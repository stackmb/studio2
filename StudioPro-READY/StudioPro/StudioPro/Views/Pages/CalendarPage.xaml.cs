using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using RomanticStudio.Services;
using RomanticStudio.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Globalization;

namespace RomanticStudio.Views.Pages;

public sealed partial class CalendarPage : Page
{
    private readonly ContractService _contractService;
    public ObservableCollection<CalendarEventViewModel> Events { get; } = new();
    public ObservableCollection<DayViewModel> CalendarDays { get; } = new();
    
    private DateTime _currentMonth = DateTime.Now;
    private PersianCalendar _persianCalendar = new PersianCalendar();

    public CalendarPage()
    {
        this.InitializeComponent();
        _contractService = App.Services.GetRequiredService<ContractService>();
        LoadCalendarAsync();
    }

    private async void LoadCalendarAsync()
    {
        var contracts = await _contractService.GetAllContractsAsync();
        
        Events.Clear();
        foreach (var contract in contracts.Where(c => c.Status == ContractStatus.Active))
        {
            Events.Add(new CalendarEventViewModel
            {
                Title = contract.ClientName,
                Date = contract.EventDate,
                Description = contract.Description,
                PhoneNumber = contract.PhoneNumber
            });
        }

        GenerateCalendarDays();
    }

    private void GenerateCalendarDays()
    {
        CalendarDays.Clear();
        
        int year = _persianCalendar.GetYear(_currentMonth);
        int month = _persianCalendar.GetMonth(_currentMonth);
        int daysInMonth = _persianCalendar.GetDaysInMonth(year, month);
        
        DateTime firstDay = new DateTime(year, month, 1, _persianCalendar);
        int startDayOfWeek = (int)firstDay.DayOfWeek;
        
        // Add empty days
        for (int i = 0; i < startDayOfWeek; i++)
        {
            CalendarDays.Add(new DayViewModel { Day = 0 });
        }
        
        // Add actual days
        for (int day = 1; day <= daysInMonth; day++)
        {
            DateTime date = new DateTime(year, month, day, _persianCalendar);
            var dayEvents = Events.Where(e => e.Date.Date == date.Date).ToList();
            
            CalendarDays.Add(new DayViewModel
            {
                Day = day,
                Date = date,
                HasEvents = dayEvents.Any(),
                EventsCount = dayEvents.Count,
                IsToday = date.Date == DateTime.Now.Date
            });
        }

        MonthYearText.Text = $"{GetPersianMonthName(month)} {year}";
    }

    private string GetPersianMonthName(int month)
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

    private void PreviousMonthButton_Click(object sender, RoutedEventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(-1);
        GenerateCalendarDays();
    }

    private void NextMonthButton_Click(object sender, RoutedEventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(1);
        GenerateCalendarDays();
    }

    private void TodayButton_Click(object sender, RoutedEventArgs e)
    {
        _currentMonth = DateTime.Now;
        GenerateCalendarDays();
    }
}

public class CalendarEventViewModel
{
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DateFormatted => Date.ToString("yyyy/MM/dd");
}

public class DayViewModel
{
    public int Day { get; set; }
    public DateTime Date { get; set; }
    public bool HasEvents { get; set; }
    public int EventsCount { get; set; }
    public bool IsToday { get; set; }
    
    public Visibility DayVisibility => Day > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EventBadgeVisibility => HasEvents ? Visibility.Visible : Visibility.Collapsed;
    public string DayText => Day > 0 ? Day.ToString() : "";
    public string EventCountText => EventsCount.ToString();
}
