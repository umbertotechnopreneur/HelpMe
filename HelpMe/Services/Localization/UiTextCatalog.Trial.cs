// SPDX-License-Identifier: MIT
namespace HelpMe.Services;

/// <summary>Provides localized terminal messages in all six supported languages.</summary>
internal static partial class UiTextCatalog
{
    #region Implementation

    /// <summary>Adds trial countdown, expiration, and Store actions in all six supported languages.</summary>
    private static void AddTrialEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Trial.OneDay", new(
            English: "Trial: 1 day remaining",
            Italian: "Trial: manca 1 giorno",
            French: "Essai : il reste 1 jour",
            German: "Testversion: noch 1 Tag",
            Spanish: "Prueba: queda 1 día",
            Vietnamese: "Dùng thử: còn 1 ngày"));
        entries.Add("Trial.Days", new(
            English: "Trial: {0} days remaining",
            Italian: "Trial: mancano {0} giorni",
            French: "Essai : il reste {0} jours",
            German: "Testversion: noch {0} Tage",
            Spanish: "Prueba: quedan {0} días",
            Vietnamese: "Dùng thử: còn {0} ngày"));
        entries.Add("Trial.Expired", new(
            English: "Your trial has expired",
            Italian: "La trial è scaduta",
            French: "Votre période d’essai a expiré",
            German: "Deine Testversion ist abgelaufen",
            Spanish: "Tu período de prueba ha caducado",
            Vietnamese: "Thời gian dùng thử đã hết"));
        entries.Add("Trial.Unlicensed", new(
            English: "No active Microsoft Store license",
            Italian: "Nessuna licenza Microsoft Store attiva",
            French: "Aucune licence Microsoft Store active",
            German: "Keine aktive Microsoft Store-Lizenz",
            Spanish: "No hay una licencia activa de Microsoft Store",
            Vietnamese: "Không có giấy phép Microsoft Store đang hoạt động"));
        entries.Add("Trial.Unavailable", new(
            English: "Microsoft Store license could not be verified. Try again or open the Store.",
            Italian: "Impossibile verificare la licenza Microsoft Store. Riprova o apri lo Store.",
            French: "Impossible de vérifier la licence Microsoft Store. Réessayez ou ouvrez le Store.",
            German: "Die Microsoft Store-Lizenz konnte nicht geprüft werden. Versuche es erneut oder öffne den Store.",
            Spanish: "No se pudo verificar la licencia de Microsoft Store. Inténtalo de nuevo o abre la Store.",
            Vietnamese: "Không thể xác minh giấy phép Microsoft Store. Hãy thử lại hoặc mở Store."));
        entries.Add("Trial.Store", new(
            English: "Microsoft Store", Italian: "Microsoft Store", French: "Microsoft Store",
            German: "Microsoft Store", Spanish: "Microsoft Store", Vietnamese: "Microsoft Store"));
        entries.Add("Trial.Purchase", new(
            English: "Purchase", Italian: "Acquista", French: "Acheter",
            German: "Kaufen", Spanish: "Comprar", Vietnamese: "Mua"));
        entries.Add("Trial.Explanation", new(
            English: "All features are available during the 30-day trial. After it expires, only --about and --help remain available until you purchase the app in Microsoft Store.",
            Italian: "Tutte le funzioni sono disponibili durante la trial di 30 giorni. Alla scadenza restano disponibili solo --about e --help, fino all’acquisto dell’app nel Microsoft Store.",
            French: "Toutes les fonctions sont disponibles pendant l’essai de 30 jours. À l’expiration, seuls --about et --help restent disponibles jusqu’à l’achat de l’application dans le Microsoft Store.",
            German: "Während der 30-tägigen Testphase sind alle Funktionen verfügbar. Danach bleiben nur --about und --help verfügbar, bis du die App im Microsoft Store kaufst.",
            Spanish: "Todas las funciones están disponibles durante la prueba de 30 días. Al caducar, solo quedan disponibles --about y --help hasta que compres la aplicación en Microsoft Store.",
            Vietnamese: "Tất cả tính năng đều khả dụng trong 30 ngày dùng thử. Sau khi hết hạn, chỉ --about và --help còn khả dụng cho đến khi bạn mua ứng dụng trên Microsoft Store."));
        entries.Add("Trial.Blocked", new(
            English: "Purchase HelpMe in Microsoft Store to continue. Only --about and --help are available.",
            Italian: "Acquista HelpMe nel Microsoft Store per continuare. Sono disponibili solo --about e --help.",
            French: "Achetez HelpMe dans le Microsoft Store pour continuer. Seuls --about et --help sont disponibles.",
            German: "Kaufe HelpMe im Microsoft Store, um fortzufahren. Nur --about und --help sind verfügbar.",
            Spanish: "Compra HelpMe en Microsoft Store para continuar. Solo están disponibles --about y --help.",
            Vietnamese: "Mua HelpMe trên Microsoft Store để tiếp tục. Chỉ --about và --help khả dụng."));
    }

    #endregion
}
