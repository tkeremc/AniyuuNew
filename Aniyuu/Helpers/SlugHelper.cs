using System.Globalization;
using System.Text.RegularExpressions;

namespace Aniyuu.Helpers;

public class SlugHelper
{
    public static string FormatString(string inputText)
    {
        if (string.IsNullOrEmpty(inputText))
        {
            return string.Empty; 
        }

        var result = inputText.ToLower(new CultureInfo("tr-TR"));

        result = result.Replace('ö', 'o');
        result = result.Replace('ç', 'c');
        result = result.Replace('ş', 's');
        result = result.Replace('ı', 'i');
        result = result.Replace('ğ', 'g');
        result = result.Replace('ü', 'u');
        
        result = result.Replace(' ', '-');
        
        result = Regex.Replace(result, @"[^a-z0-9-]", string.Empty);
        
        result = Regex.Replace(result, @"-{2,}", "-");
        
        result = result.Trim('-');

        return result;
    }
}