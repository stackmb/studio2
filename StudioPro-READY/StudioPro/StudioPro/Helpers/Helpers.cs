using System;
using System.Linq;

namespace StudioPro.Helpers;

// ==================== PERSIAN HELPER ====================

public static class PersianHelper
{
    private static readonly string[] FarsiDigits = { "۰", "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹" };
    
    public static string ToPersian(this int number)
    {
        return number.ToString().Select(c => FarsiDigits[c - '0']).Aggregate((a, b) => a + b);
    }
    
    public static string ToPersian(this long number)
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
    
    public static bool IsUrgent(DateTime eventDate, DateTime? checkDate = null)
    {
        var now = checkDate ?? DateTime.Now;
        var diff = (eventDate - now).TotalDays;
        return diff >= 0 && diff <= 2; // 48 ساعت
    }
}

// ==================== DATE HELPER ====================

public static class DateHelper
{
    public static string ToPersianDate(this DateTime date)
    {
        var persianCalendar = new System.Globalization.PersianCalendar();
        var year = persianCalendar.GetYear(date);
        var month = persianCalendar.GetMonth(date);
        var day = persianCalendar.GetDayOfMonth(date);
        return $"{year:0000}/{month:00}/{day:00}";
    }
    
    public static string GetPersianMonthName(int month)
    {
        return month switch
        {
            1 => "فروردین", 2 => "اردیبهشت", 3 => "خرداد",
            4 => "تیر", 5 => "مرداد", 6 => "شهریور",
            7 => "مهر", 8 => "آبان", 9 => "آذر",
            10 => "دی", 11 => "بهمن", 12 => "اسفند",
            _ => ""
        };
    }
    
    public static string GetPersianDayName(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Saturday => "شنبه",
            DayOfWeek.Sunday => "یکشنبه",
            DayOfWeek.Monday => "دوشنبه",
            DayOfWeek.Tuesday => "سه‌شنبه",
            DayOfWeek.Wednesday => "چهارشنبه",
            DayOfWeek.Thursday => "پنجشنبه",
            DayOfWeek.Friday => "جمعه",
            _ => ""
        };
    }
}

// ==================== FORMAT HELPER ====================

public static class FormatHelper
{
    public static string ToCurrency(this decimal amount)
    {
        return $"{amount:N0} تومان";
    }
    
    public static string ToFileSize(this long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
