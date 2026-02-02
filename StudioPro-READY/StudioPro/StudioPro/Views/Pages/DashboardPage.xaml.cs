using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using StudioPro.Services;
using StudioPro.Models;
using StudioPro.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI;

namespace StudioPro.Views.Pages;

public sealed partial class DashboardPage : Page
{
    private readonly ContractService _contractService;

    public ObservableCollection<ContractViewModel> AllContracts { get; } = new();
    public ObservableCollection<ContractViewModel> FilteredContracts { get; } = new();

    public DashboardPage()
    {
        this.InitializeComponent();
        
        _contractService = App.Services.GetRequiredService<ContractService>();
        
        this.Loaded += Dashboard_Loaded;
    }

    private async void Dashboard_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadDataAsync();
    }

    private async System.Threading.Tasks.Task LoadDataAsync()
    {
        try
        {
            var contracts = await _contractService.GetAllAsync();
            
            AllContracts.Clear();
            foreach (var contract in contracts)
            {
                var vm = new ContractViewModel(contract);
                AllContracts.Add(vm);
            }

            UpdateStats();
            ApplyFilters();
            CheckUrgentContracts();
            UpdateEmptyState();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"خطا در بارگذاری: {ex.Message}");
        }
    }

    private void UpdateStats()
    {
        var activeContracts = AllContracts.Where(c => c.Status == ContractStatus.Active).ToList();
        var totalRevenue = activeContracts.Sum(c => c.Deposit);
        var pendingRevenue = activeContracts.Sum(c => c.RemainingAmount);

        ActiveCountText.Text = activeContracts.Count.ToString();
        TotalRevenueText.Text = $"{totalRevenue:N0}";
        PendingRevenueText.Text = $"{pendingRevenue:N0}";
        TotalClientsText.Text = AllContracts.Count.ToString();
    }

    private void ApplyFilters()
    {
        FilteredContracts.Clear();
        
        var filtered = AllContracts.AsEnumerable();

        // جستجو
        if (!string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            var search = SearchBox.Text.ToLower();
            filtered = filtered.Where(c =>
                c.ClientName.ToLower().Contains(search) ||
                c.PhoneNumber.Contains(search) ||
                c.Id.ToLower().Contains(search) ||
                c.Description.ToLower().Contains(search)
            );
        }

        // مرتب‌سازی پیش‌فرض: جدیدترین اول
        filtered = filtered.OrderByDescending(c => c.EventDate);

        foreach (var contract in filtered)
        {
            FilteredContracts.Add(contract);
        }

        UpdateEmptyState();
    }

    private void CheckUrgentContracts()
    {
        var urgentCount = AllContracts.Count(c =>
            c.Status == ContractStatus.Active &&
            c.RemainingAmount > 0 &&
            PersianHelper.IsUrgent(c.EventDate)
        );

        if (urgentCount > 0)
        {
            UrgentBanner.Visibility = Visibility.Visible;
            UrgentCountText.Text = urgentCount.ToString();
        }
        else
        {
            UrgentBanner.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateEmptyState()
    {
        if (FilteredContracts.Count == 0 && AllContracts.Count == 0)
        {
            EmptyState.Visibility = Visibility.Visible;
            ContractsListView.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyState.Visibility = Visibility.Collapsed;
            ContractsListView.Visibility = Visibility.Visible;
        }
    }

    private void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void NewContract_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(ContractsPage));
    }
}

// ==================== VIEW MODEL ====================

public class ContractViewModel
{
    private readonly Contract _contract;

    public ContractViewModel(Contract contract)
    {
        _contract = contract;
    }

    public string Id => _contract.Id;
    public string ClientName => _contract.ClientName;
    public string PhoneNumber => _contract.PhoneNumber;
    public string Description => _contract.Description;
    public DateTime EventDate => _contract.EventDate;
    public decimal TotalAmount => _contract.TotalAmount;
    public decimal Deposit => _contract.Deposit;
    public decimal RemainingAmount => _contract.RemainingAmount;
    public ContractStatus Status => _contract.Status;

    public string TotalAmountFormatted => $"{TotalAmount:N0}";
    public string DepositFormatted => $"{Deposit:N0}";
    public string RemainingFormatted => $"{RemainingAmount:N0}";
    public string EventDateFormatted => EventDate.ToPersianDate();

    public string StatusText => Status switch
    {
        ContractStatus.Active => "فعال",
        ContractStatus.Completed => "تکمیل",
        ContractStatus.Canceled => "لغو شده",
        _ => "نامشخص"
    };

    public SolidColorBrush StatusColor => Status switch
    {
        ContractStatus.Active => new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)),     // #4CAF50
        ContractStatus.Completed => new SolidColorBrush(Color.FromArgb(255, 33, 150, 243)), // #2196F3
        ContractStatus.Canceled => new SolidColorBrush(Color.FromArgb(255, 244, 67, 54)),   // #F44336
        _ => new SolidColorBrush(Color.FromArgb(255, 158, 158, 158))
    };

    public bool HasDebt => RemainingAmount > 0 && Status == ContractStatus.Active;
}
