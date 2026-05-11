using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface ILocalizationService
{
    List<LocaleInfo> GetSupportedLocales();
    Dictionary<string, string> GetTranslations(string locale);
}

public class LocalizationService : ILocalizationService
{
    private static readonly List<LocaleInfo> SupportedLocales = new()
    {
        new() { Code = "en", Name = "English", NativeName = "English", Direction = "ltr" },
        new() { Code = "hi", Name = "Hindi", NativeName = "हिन्दी", Direction = "ltr" },
        new() { Code = "es", Name = "Spanish", NativeName = "Español", Direction = "ltr" },
        new() { Code = "pt", Name = "Portuguese", NativeName = "Português", Direction = "ltr" },
        new() { Code = "ar", Name = "Arabic", NativeName = "العربية", Direction = "rtl" },
        new() { Code = "zh", Name = "Chinese", NativeName = "中文", Direction = "ltr" },
        new() { Code = "ja", Name = "Japanese", NativeName = "日本語", Direction = "ltr" },
        new() { Code = "ko", Name = "Korean", NativeName = "한국어", Direction = "ltr" },
        new() { Code = "fr", Name = "French", NativeName = "Français", Direction = "ltr" },
        new() { Code = "de", Name = "German", NativeName = "Deutsch", Direction = "ltr" },
        new() { Code = "vi", Name = "Vietnamese", NativeName = "Tiếng Việt", Direction = "ltr" },
        new() { Code = "th", Name = "Thai", NativeName = "ไทย", Direction = "ltr" },
        new() { Code = "id", Name = "Indonesian", NativeName = "Bahasa Indonesia", Direction = "ltr" },
        new() { Code = "tr", Name = "Turkish", NativeName = "Türkçe", Direction = "ltr" },
        new() { Code = "ru", Name = "Russian", NativeName = "Русский", Direction = "ltr" },
        new() { Code = "bn", Name = "Bengali", NativeName = "বাংলা", Direction = "ltr" }
    };

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["en"] = new()
        {
            ["nav.explore"] = "Explore", ["nav.markets"] = "Markets", ["nav.watchlist"] = "Watchlist",
            ["nav.charts"] = "Charts", ["nav.compare"] = "Compare", ["nav.news"] = "News",
            ["nav.backtest"] = "Backtest", ["nav.plans"] = "Plans", ["nav.notifications"] = "Notifications",
            ["nav.chat"] = "AI Chat", ["nav.signals"] = "Signals", ["nav.signout"] = "Sign Out",
            ["plan.free"] = "Free", ["plan.pro"] = "Pro", ["plan.premium"] = "Premium",
            ["plan.enterprise"] = "Enterprise", ["plan.upgrade"] = "Upgrade",
            ["trial.expires"] = "Trial expires in {0} days", ["trial.expired"] = "Trial expired. Please upgrade.",
            ["common.loading"] = "Loading...", ["common.error"] = "Error", ["common.save"] = "Save",
            ["common.cancel"] = "Cancel", ["common.delete"] = "Delete", ["common.search"] = "Search"
        },
        ["hi"] = new()
        {
            ["nav.explore"] = "खोजें", ["nav.markets"] = "बाज़ार", ["nav.watchlist"] = "वॉचलिस्ट",
            ["nav.charts"] = "चार्ट", ["nav.compare"] = "तुलना", ["nav.news"] = "समाचार",
            ["nav.backtest"] = "बैकटेस्ट", ["nav.plans"] = "योजनाएं", ["nav.notifications"] = "सूचनाएं",
            ["nav.chat"] = "AI चैट", ["nav.signals"] = "सिग्नल", ["nav.signout"] = "साइन आउट",
            ["plan.free"] = "मुफ़्त", ["plan.pro"] = "प्रो", ["plan.premium"] = "प्रीमियम",
            ["plan.enterprise"] = "एंटरप्राइज़", ["plan.upgrade"] = "अपग्रेड करें",
            ["trial.expires"] = "{0} दिनों में ट्रायल समाप्त होता है", ["trial.expired"] = "ट्रायल समाप्त। कृपया अपग्रेड करें।",
            ["common.loading"] = "लोड हो रहा है...", ["common.error"] = "त्रुटि", ["common.save"] = "सहेजें",
            ["common.cancel"] = "रद्द करें", ["common.delete"] = "हटाएं", ["common.search"] = "खोजें"
        },
        ["es"] = new()
        {
            ["nav.explore"] = "Explorar", ["nav.markets"] = "Mercados", ["nav.watchlist"] = "Lista de seguimiento",
            ["nav.charts"] = "Gráficos", ["nav.compare"] = "Comparar", ["nav.news"] = "Noticias",
            ["nav.backtest"] = "Backtest", ["nav.plans"] = "Planes", ["nav.notifications"] = "Notificaciones",
            ["nav.chat"] = "Chat IA", ["nav.signals"] = "Señales", ["nav.signout"] = "Cerrar sesión",
            ["plan.free"] = "Gratis", ["plan.pro"] = "Pro", ["plan.premium"] = "Premium",
            ["plan.enterprise"] = "Empresa", ["plan.upgrade"] = "Actualizar",
            ["trial.expires"] = "Prueba expira en {0} días", ["trial.expired"] = "Prueba expirada. Por favor actualice.",
            ["common.loading"] = "Cargando...", ["common.error"] = "Error", ["common.save"] = "Guardar",
            ["common.cancel"] = "Cancelar", ["common.delete"] = "Eliminar", ["common.search"] = "Buscar"
        },
        ["ar"] = new()
        {
            ["nav.explore"] = "استكشاف", ["nav.markets"] = "الأسواق", ["nav.watchlist"] = "قائمة المراقبة",
            ["nav.charts"] = "الرسوم البيانية", ["nav.compare"] = "مقارنة", ["nav.news"] = "أخبار",
            ["nav.backtest"] = "اختبار رجعي", ["nav.plans"] = "الخطط", ["nav.notifications"] = "إشعارات",
            ["nav.chat"] = "دردشة AI", ["nav.signals"] = "إشارات", ["nav.signout"] = "تسجيل خروج",
            ["plan.free"] = "مجاني", ["plan.pro"] = "برو", ["plan.premium"] = "بريميوم",
            ["plan.enterprise"] = "مؤسسة", ["plan.upgrade"] = "ترقية",
            ["trial.expires"] = "تنتهي النسخة التجريبية خلال {0} أيام", ["trial.expired"] = "انتهت النسخة التجريبية. يرجى الترقية.",
            ["common.loading"] = "جار التحميل...", ["common.error"] = "خطأ", ["common.save"] = "حفظ",
            ["common.cancel"] = "إلغاء", ["common.delete"] = "حذف", ["common.search"] = "بحث"
        }
    };

    public List<LocaleInfo> GetSupportedLocales() => SupportedLocales;

    public Dictionary<string, string> GetTranslations(string locale)
    {
        return Translations.GetValueOrDefault(locale, Translations["en"])!;
    }
}
