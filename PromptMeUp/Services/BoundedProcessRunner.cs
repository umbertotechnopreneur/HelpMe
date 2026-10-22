// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: PromptMeUp
 * Repository: https://github.com/umbertotechnopreneur/PromptMeUp
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; added this header on 2026-10-10.
 * Human guidance: Umberto Giacobbi; requested VibeWare branding.
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT - see LICENSE
 *
 * VBWR E */

using System.Diagnostics;
using System.Text;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

/// <summary>Runs a child process with bounded output, cancellation, and timeout handling.</summary>
internal static class BoundedProcessRunner
{
    #region Methods

    /// <summary>Runs a caller-approved process with one deadline for input, exit, and bounded output collection.</summary>
    /// <param name="startInfo">The process and argument list to launch.</param>
    /// <param name="displayCommand">The command retained in the result.</param>
    /// <param name="timeout">Total deadline, or InfiniteTimeSpan for foreground execution.</param>
    /// <param name="cancellationToken">Cancels the application.</param>
    /// <param name="standardInput">Optional approved source transported through stdin.</param>
    /// <param name="output">Receives serialized, normalized live updates.</param>
    /// <param name="stopToken">Interrupts this command without cancelling the application.</param>
    /// <exception cref="ArgumentOutOfRangeException">The deadline is not positive or infinite.</exception>
    /// <exception cref="InvalidOperationException">The process could not be started.</exception>
    internal static async Task<CommandExecutionResult> RunAsync(
        ProcessStartInfo startInfo, string displayCommand, TimeSpan timeout, CancellationToken cancellationToken,
        string? standardInput = null, Action<CommandOutputUpdate>? output = null, CancellationToken stopToken = default)
    {
        var hasFiniteTimeout = timeout != Timeout.InfiniteTimeSpan;
        var isTimeoutOutOfRange = timeout <= TimeSpan.Zero || timeout.TotalMilliseconds > uint.MaxValue - 1;
        if (hasFiniteTimeout && isTimeoutOutOfRange)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        cancellationToken.ThrowIfCancellationRequested();
        stopToken.ThrowIfCancellationRequested();

        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.StandardInputEncoding = new UTF8Encoding(false);
        startInfo.StandardOutputEncoding = Encoding.UTF8;
        startInfo.StandardErrorEncoding = Encoding.UTF8;

        using var process = new Process { StartInfo = startInfo };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stopToken);
        deadline.CancelAfter(timeout);
        var stopwatch = Stopwatch.StartNew();

        if (!process.Start())
        {
            throw new InvalidOperationException("The requested process could not be started.");
        }

        var outputGate = new object();
        Action<CommandOutputUpdate>? publish = output is null ? null : update =>
        {
            lock (outputGate)
            {
                output(update);
            }
        };

        // Drain both pipes concurrently so a full stderr buffer cannot block stdout or process exit.
        var outputTask = ReadBoundedAsync(process.StandardOutput.BaseStream, false, publish, deadline);
        var errorTask = ReadBoundedAsync(process.StandardError.BaseStream, true, publish, deadline);
        var inputTask = WriteInputAsync(process.StandardInput, standardInput, deadline);
        var timedOut = false;

        try
        {
            await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
            // A descendant retaining a pipe must not keep the UI waiting after the launched process exits.
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                deadline.CancelAfter(TimeSpan.FromSeconds(2));
            }

            await Task.WhenAll(inputTask, outputTask, errorTask).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            timedOut = deadline.IsCancellationRequested && !stopToken.IsCancellationRequested;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = !stopToken.IsCancellationRequested;
        }
        catch
        {
            await deadline.CancelAsync().ConfigureAwait(false);
            TryKill(process);
            await ObserveCompletionAsync(inputTask, outputTask, errorTask).ConfigureAwait(false);
            throw;
        }
        finally
        {
            if (deadline.IsCancellationRequested)
            {
                TryKill(process);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
        }

        await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
        var capturedOutput = await outputTask.ConfigureAwait(false);
        var error = await errorTask.ConfigureAwait(false);
        try
        {
            await inputTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            // Cancellation already determines the result; input shutdown adds no separate failure.
        }

        cancellationToken.ThrowIfCancellationRequested();
        var cancelled = stopToken.IsCancellationRequested;
        int? exitCode = timedOut || cancelled ? null : process.ExitCode;
        var outputTruncated = capturedOutput.Truncated || error.Truncated;

        return new CommandExecutionResult(displayCommand, exitCode,
            capturedOutput.Text, error.Text, timedOut, outputTruncated, stopwatch.ElapsedMilliseconds)
        {
            Cancelled = cancelled,
            OutputWasStreamed = publish is not null
        };
    }

    #endregion

    #region Implementation

    /// <summary>Transmits large approved source without command-line length limits or temporary source files.</summary>
    /// <param name="writer">The child process input stream.</param>
    /// <param name="input">Optional approved source to transmit.</param>
    /// <param name="deadline">The shared lifetime cancelled if input transmission fails.</param>
    private static async Task WriteInputAsync(StreamWriter writer, string? input, CancellationTokenSource deadline)
    {
        try
        {
            if (input is not null)
            {
                await writer.BaseStream.WriteAsync(Encoding.UTF8.GetBytes(input), deadline.Token).ConfigureAwait(false);
            }
        }
        catch
        {
            await deadline.CancelAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            writer.Close();
        }
    }

    /// <summary>Publishes each available UTF-8 byte block while retaining bounded normalized first/last evidence.</summary>
    /// <param name="stream">The redirected byte stream to drain without text-reader read-ahead.</param>
    /// <param name="isError">Whether this is stderr.</param>
    /// <param name="publish">The optional serialized display callback.</param>
    /// <param name="deadline">The shared deadline cancelled if reading or rendering fails.</param>
    private static async Task<CapturedStream> ReadBoundedAsync(Stream stream, bool isError,
        Action<CommandOutputUpdate>? publish, CancellationTokenSource deadline)
    {
        var capture = new SimpleCommandOutput(isError, publish);
        var buffer = new byte[4096];
        var characters = new char[Encoding.UTF8.GetMaxCharCount(buffer.Length)];
        var decoder = Encoding.UTF8.GetDecoder();
        var firstCharacters = true;
        var truncated = false;
        try
        {
            while (true)
            {
                // Read the pipe directly so an exact text-buffer boundary cannot delay already available output.
                var read = await stream.ReadAsync(buffer.AsMemory(), deadline.Token).ConfigureAwait(false);
                var count = decoder.GetChars(buffer.AsSpan(0, read), characters.AsSpan(), flush: read == 0);
                var start = 0;
                if (firstCharacters && count > 0)
                {
                    firstCharacters = false;
                    if (characters[0] == '\uFEFF')
                    {
                        start = 1;
                    }
                }

                if (count > start)
                {
                    capture.Append(characters.AsSpan(start, count - start));
                }

                if (read == 0)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            truncated = true;
        }
        catch
        {
            await deadline.CancelAsync().ConfigureAwait(false);
            throw;
        }
        var text = capture.Complete();
        return new CapturedStream(text, truncated || capture.Truncated);
    }

    /// <summary>Observes cancelled I/O tasks after teardown so they cannot outlive the execution scope.</summary>
    /// <param name="tasks">Outstanding input/output operations.</param>
    private static async Task ObserveCompletionAsync(params Task[] tasks)
    {
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected after the execution deadline or application shutdown.
        }
        catch (IOException)
        {
            // Closing a terminated process's pipes may fail after the original error was captured.
        }
    }

    /// <summary>Stops the started process tree when a deadline, cancellation, or I/O failure interrupts execution.</summary>
    /// <param name="process">The process whose descendants belong to this execution.</param>
    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // A process can exit between the state check and the kill request.
        }
    }

    #endregion

    #region Types

    /// <summary>Contains captured stream text and whether bounded retention omitted any output.</summary>
    private sealed record CapturedStream(string Text, bool Truncated);

    #endregion
}
