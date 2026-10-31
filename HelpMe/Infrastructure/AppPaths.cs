// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: HelpMe
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

namespace HelpMe.Infrastructure;

/// <summary>Resolves the local storage, logs, settings, and database paths.</summary>
public sealed record AppPaths(
    string DataDirectory,
    string DatabasePath,
    string LogsDirectory,
    string LogFilePattern,
    string PromptDirectory)
{

    #region Methods

    /// <summary>Resolves and creates the portable local data, log, database, and packaged-prompt paths.</summary>
    public static AppPaths Create()
    {
        var configuredDataDirectory = Environment.GetEnvironmentVariable("HELPME_DATA_DIR");
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDirectory = string.IsNullOrWhiteSpace(configuredDataDirectory)
            ? Path.Combine(localApplicationData, "HelpMe")
            : Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredDataDirectory.Trim()));

        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new InvalidOperationException("A writable HelpMe data directory could not be resolved.");
        }

        var logsDirectory = Path.Combine(dataDirectory, "logs");
        Directory.CreateDirectory(dataDirectory);
        Directory.CreateDirectory(logsDirectory);

        return new AppPaths(
            dataDirectory,
            Path.Combine(dataDirectory, "helpme.db"),
            logsDirectory,
            Path.Combine(logsDirectory, "helpme-.log"),
            Path.Combine(AppContext.BaseDirectory, "prompt"));
    }

    #endregion

}
