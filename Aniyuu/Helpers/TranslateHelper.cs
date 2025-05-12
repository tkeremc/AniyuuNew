using Aniyuu.Utils;
using DeepL;
using DeepL.Model;

namespace Aniyuu.Helpers;

public class TranslateHelper
{
    public static async Task<string> Translate(string text)
    {
        var translator = new Translator(AppSettingConfig.Configuration["DeepL:AuthKey"]!);

        var translatedText = await translator.TranslateTextAsync(
            text,
            LanguageCode.English,
            LanguageCode.Turkish);
        return translatedText.Text;
    }
    
    
}