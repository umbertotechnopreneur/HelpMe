// SPDX-License-Identifier: MIT
using System.Text.RegularExpressions;

namespace HelpMe.Services;

/// <summary>Restricts Bash automatic execution to complete, literal inspection commands.</summary>
internal static partial class BashCommandRisk
{
    /// <summary>Classifies destructive operations and treats evaluation or unrecognized syntax as high risk.</summary>
    internal static int Score(string command)
    {
        if (Destructive().IsMatch(command)) return 95;
        // Quotes, expansion, redirection, assignments and compound commands need manual review.
        return Inspection().IsMatch(command) ? 15 : 75;
    }

    /// <summary>Recognizes system destruction and process/session termination even inside compound input.</summary>
    [GeneratedRegex(@"(?:^|[\s/;&|()])(?:mkfs(?:\.[a-z0-9]+)?|wipefs|dd|reboot|poweroff|halt|shutdown|kill|killall|pkill)(?:\s|$)", RegexOptions.CultureInvariant)]
    private static partial Regex Destructive();

    /// <summary>Allows a small set of complete read-only shapes without shell metacharacters or arbitrary options.</summary>
    [GeneratedRegex(@"\A[ \t]*(?:pwd|whoami|hostname|date|uname(?:[ \t]+-[amsnrpv]+)?|ls(?:[ \t]+-[alh]+)?|git[ \t]+(?:status(?:[ \t]+--short)?|branch[ \t]+--show-current|log[ \t]+--oneline))[ \t]*\z", RegexOptions.CultureInvariant)]
    private static partial Regex Inspection();
}
