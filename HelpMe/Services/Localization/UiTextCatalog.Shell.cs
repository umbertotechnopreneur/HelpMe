// SPDX-License-Identifier: MIT
namespace HelpMe.Services;

/// <summary>Provides shell selection and preview text in all supported languages.</summary>
internal static partial class UiTextCatalog
{
    /// <summary>Adds shell usage, ambiguity, and preview labels.</summary>
    private static void AddShellEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Shell.Setting", new("Command shell", "Shell dei comandi", "Shell des commandes", "Befehls-Shell", "Shell de comandos", "Shell chạy lệnh"));
        entries.Add("Shell.PathUnavailable", new(
            "Run PATH integration directly from Bash, PowerShell, Zsh, or Fish so HelpMe can identify the profile to preview.",
            "Avvia l’integrazione PATH direttamente da Bash, PowerShell, Zsh o Fish: HelpMe potrà mostrarti il profilo da modificare.",
            "Lancez l’intégration PATH directement depuis Bash, PowerShell, Zsh ou Fish pour identifier le profil à prévisualiser.",
            "Starte die PATH-Integration direkt aus Bash, PowerShell, Zsh oder Fish, damit HelpMe das Profil anzeigen kann.",
            "Inicia la integración PATH directamente desde Bash, PowerShell, Zsh o Fish para identificar el perfil que se mostrará.",
            "Chạy tích hợp PATH trực tiếp từ Bash, PowerShell, Zsh hoặc Fish để HelpMe xác định hồ sơ cần xem trước."));
        entries.Add("Shell.Auto", new("Auto", "Automatica", "Automatique", "Automatisch", "Automática", "Tự động"));
        entries.Add("Shell.PowerShell", new("PowerShell", "PowerShell", "PowerShell", "PowerShell", "PowerShell", "PowerShell"));
        entries.Add("Shell.Bash", new("Bash", "Bash", "Bash", "Bash", "Bash", "Bash"));
        entries.Add("Shell.SettingHelp", new(
            "Auto detects the Linux calling shell; Windows and macOS use PowerShell. --shell overrides this preference for one invocation. Script language is separate; script, file, and skill wrappers require PowerShell.",
            "Automatica rileva la shell chiamante su Linux; Windows e macOS usano PowerShell. --shell prevale per un avvio. Il linguaggio degli script è separato; script, file e skill richiedono PowerShell per i comandi generati.",
            "Automatique détecte la shell appelante sous Linux ; Windows et macOS utilisent PowerShell. --shell remplace ce choix pour un lancement. Le langage des scripts est distinct ; les commandes générées pour scripts, fichiers et skills nécessitent PowerShell.",
            "Automatisch erkennt unter Linux die aufrufende Shell; Windows und macOS verwenden PowerShell. --shell überschreibt die Wahl für einen Aufruf. Die Skriptsprache ist separat; generierte Skript-, Datei- und Skill-Befehle benötigen PowerShell.",
            "Automática detecta la shell llamante en Linux; Windows y macOS usan PowerShell. --shell cambia la elección para un inicio. El lenguaje de scripts es independiente; los comandos generados para scripts, archivos y skills necesitan PowerShell.",
            "Tự động nhận diện shell gọi trên Linux; Windows và macOS dùng PowerShell. --shell ghi đè cho một lần chạy. Ngôn ngữ script là riêng biệt; lệnh tạo cho script, tệp và skill cần PowerShell."));
        entries.Add("Shell.Help", new(
            "Choose auto, pwsh, or bash for this invocation, overriding Setup. Linux Auto detects the calling shell; ambiguous detection requires an explicit choice. Bash compound commands require manual approval; old plans without shell metadata must be recreated.",
            "Scegli auto, pwsh o bash per questo avvio, prevalendo sul Setup. Automatica rileva la shell chiamante Linux; se è ambigua serve una scelta esplicita. I comandi Bash composti richiedono conferma manuale; ricrea i vecchi piani senza shell salvata.",
            "Choisissez auto, pwsh ou bash pour ce lancement, en priorité sur Réglages. Auto détecte la shell appelante Linux ; un résultat ambigu exige un choix explicite. Les commandes Bash composées exigent une confirmation ; recréez les anciens plans sans shell enregistrée.",
            "Wähle auto, pwsh oder bash für diesen Aufruf statt der Einstellung. Auto erkennt die aufrufende Linux-Shell; bei Unklarheit ist eine explizite Wahl erforderlich. Zusammengesetzte Bash-Befehle benötigen Bestätigung; alte Pläne ohne gespeicherte Shell müssen neu erstellt werden.",
            "Elige auto, pwsh o bash para este inicio, con prioridad sobre Ajustes. Auto detecta la shell llamante en Linux; si es ambigua, elige explícitamente. Los comandos Bash compuestos requieren confirmación; recrea los planes antiguos sin shell guardada.",
            "Chọn auto, pwsh hoặc bash cho lần chạy này, ưu tiên hơn Cài đặt. Auto nhận diện shell gọi trên Linux; nếu không rõ, phải chọn cụ thể. Lệnh Bash phức hợp cần xác nhận; tạo lại kế hoạch cũ chưa lưu shell."));
        entries.Add("Shell.Usage", new(
            "Use --shell auto, --shell pwsh, or --shell bash once per invocation.",
            "Usa --shell auto, --shell pwsh o --shell bash una sola volta per avvio.",
            "Utilisez --shell auto, --shell pwsh ou --shell bash une fois par lancement.",
            "Verwende --shell auto, --shell pwsh oder --shell bash einmal pro Aufruf.",
            "Usa --shell auto, --shell pwsh o --shell bash una vez por ejecución.",
            "Dùng --shell auto, --shell pwsh hoặc --shell bash một lần mỗi lượt khởi chạy."));
        entries.Add("Shell.Choose", new(
            "Choose the command shell explicitly: restart with --shell pwsh or --shell bash. Bash execution is supported on Linux and macOS; Windows uses pwsh.",
            "Scegli la shell dei comandi: riavvia hm con --shell pwsh o --shell bash. Bash è disponibile su Linux e macOS; su Windows usa pwsh.",
            "Choisissez la shell : relancez hm avec --shell pwsh ou --shell bash. Bash est disponible sous Linux et macOS ; sous Windows, utilisez pwsh.",
            "Wähle die Befehls-Shell: Starte hm mit --shell pwsh oder --shell bash neu. Bash ist unter Linux und macOS verfügbar; verwende unter Windows pwsh.",
            "Elige la shell: vuelve a iniciar hm con --shell pwsh o --shell bash. Bash está disponible en Linux y macOS; en Windows usa pwsh.",
            "Chọn shell: chạy lại hm với --shell pwsh hoặc --shell bash. Bash có trên Linux và macOS; trên Windows dùng pwsh."));
        entries.Add("Shell.Preview", new("Command shell: {0}", "Shell del comando: {0}",
            "Shell de la commande : {0}", "Befehls-Shell: {0}", "Shell del comando: {0}", "Shell của lệnh: {0}"));
    }
}
