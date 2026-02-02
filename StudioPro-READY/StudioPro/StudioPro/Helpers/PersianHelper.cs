using System;
using System.Linq;

namespace RomanticStudio.Helpers;

public static class PersianHelper
{
    private static readonly string[] FarsiDigits = { "۰", "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹" };
    
    public static string ToPersian(this int number)
    {
        return number.ToString().Select(c => FarsiDigits[c - '0']).Aggregate((a, b) => a + b);
    }
    
    public static string ToEnglish(this string persianNumber)
    {
        if (string.IsNullOrEmpty(persianNumber)) return persianNumber;
        for (int i = 0; i < 10; i++)
            persianNumber = persianNumber.Replace(FarsiDigits[i], i.ToString());
        return persianNumber;
    }
}
