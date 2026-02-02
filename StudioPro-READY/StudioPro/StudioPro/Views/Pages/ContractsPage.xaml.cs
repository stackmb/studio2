using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using StudioPro.Services;
using StudioPro.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace StudioPro.Views.Pages;

public sealed partial class ContractsPage : Page
{
    private readonly ContractService _contractService;
    private readonly SettingsService _settingsService;

    public ObservableCollection<ServiceViewModel> Services { get; } = new();
    public ObservableCollection<PrintSizeViewModel> PrintSizes { get; } = new();

    private decimal _totalAmount = 0;
    private decimal _depositAmount = 0;

    public ContractsPage()
    {
        this.InitializeComponent();
        
        _contractService = App.Services.GetRequiredService<ContractService>();
        _settingsService = App.Services.GetRequiredService<SettingsService>();

        LoadServicesAsync();
    }

    private async void LoadServicesAsync()
    {
        try
        {
            var services = await _settingsService.GetServicePricesAsync();
            var printSizes = await _settingsService.GetPrintPricesAsync();

            Services.Clear();
            foreach (var service in services)
            {
                Services.Add(new ServiceViewModel
                {
                    Id = service.Id,
                    Label = service.Label,
                    Price = service.Price,
                    IsSelected = false
                });
            }

            PrintSizes.Clear();
            foreach (var size in printSizes)
            {
                PrintSizes.Add(new PrintSizeViewModel
                {
                    Id = size.Id,
                    Size = size.Size,
                    Price = size.Price,
                    Quantity = 0,
                    IsSelected = false
                });
            }

            ServicesListView.ItemsSource = Services;
            PrintSizesListView.ItemsSource = PrintSizes;
        }
        catch (Exception ex)
        {
            ShowError($"خطا در بارگذاری: {ex.Message}");
        }
    }

    private void Service_CheckChanged(object sender, RoutedEventArgs e)
    {
        CalculateTotal();
    }

    private void Print_CheckChanged(object sender, RoutedEventArgs e)
    {
        CalculateTotal();
    }

    private void PrintQuantity_Changed(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        CalculateTotal();
    }

    private void Deposit_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (decimal.TryParse(DepositBox.Text, out var deposit))
        {
            _depositAmount = deposit;
        }
        else
        {
            _depositAmount = 0;
        }
        UpdateRemaining();
    }

    private void CalculateTotal()
    {
        // محاسبه خدمات
        var servicesTotal = Services.Where(s => s.IsSelected).Sum(s => s.Price);

        // محاسبه چاپ
        var printsTotal = PrintSizes.Where(p => p.IsSelected && p.Quantity > 0)
            .Sum(p => p.Price * p.Quantity);

        _totalAmount = servicesTotal + printsTotal;

        // به‌روزرسانی UI
        TotalServicesText.Text = $"{_totalAmount:N0} تومان";
        
        // به‌روزرسانی قیمت کل هر آیتم چاپ
        foreach (var print in PrintSizes)
        {
            print.TotalPrice = print.Price * print.Quantity;
        }

        UpdateRemaining();
    }

    private void UpdateRemaining()
    {
        var remaining = _totalAmount - _depositAmount;
        RemainingText.Text = $"{remaining:N0} تومان";
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(GroomNameBox.Text))
            {
                ShowError("لطفاً نام داماد را وارد کنید");
                return;
            }

            if (string.IsNullOrWhiteSpace(GroomPhoneBox.Text))
            {
                ShowError("لطفاً شماره تلفن داماد را وارد کنید");
                return;
            }

            if (!EventDatePicker.Date.HasValue)
            {
                ShowError("لطفاً تاریخ مراسم را انتخاب کنید");
                return;
            }

            // ساخت قرارداد
            var contract = new Contract
            {
                ClientName = GroomNameBox.Text.Trim(),
                PhoneNumber = GroomPhoneBox.Text.Trim(),
                BrideName = BrideNameBox.Text.Trim(),
                BridePhoneNumber = BridePhoneBox.Text.Trim(),
                NationalId = NationalIdBox.Text.Trim(),
                ClientAddress = AddressBox.Text.Trim(),
                Description = DescriptionBox.Text.Trim(),
                GiftDescription = GiftBox.Text.Trim(),
                
                ContractDate = ContractDatePicker.Date?.DateTime ?? DateTime.Now,
                EventDate = EventDatePicker.Date.Value.DateTime,
                
                TotalAmount = _totalAmount,
                Deposit = _depositAmount,
                RemainingAmount = _totalAmount - _depositAmount,
                
                PaymentMethod = GetPaymentMethod(),
                Status = ContractStatus.Active,
                
                SelectedServices = System.Text.Json.JsonSerializer.Serialize(
                    Services.Where(s => s.IsSelected).Select(s => s.Id).ToList()
                ),
                
                SelectedPrints = System.Text.Json.JsonSerializer.Serialize(
                    PrintSizes.Where(p => p.IsSelected && p.Quantity > 0).Select(p => new
                    {
                        p.Id,
                        p.Size,
                        p.Quantity,
                        p.Price,
                        TotalPrice = p.Price * p.Quantity
                    }).ToList()
                )
            };

            // ذخیره در دیتابیس
            var success = await _contractService.AddAsync(contract);

            if (success)
            {
                await ShowSuccessDialog("قرارداد با موفقیت ثبت شد!");
                ClearForm();
                
                // بازگشت به داشبورد
                Frame.Navigate(typeof(DashboardPage));
            }
            else
            {
                ShowError("خطا در ثبت قرارداد");
            }
        }
        catch (Exception ex)
        {
            ShowError($"خطا: {ex.Message}");
        }
    }

    private PaymentMethod GetPaymentMethod()
    {
        if (CardRadio.IsChecked == true) return PaymentMethod.Card;
        if (PosRadio.IsChecked == true) return PaymentMethod.POS;
        return PaymentMethod.Cash;
    }

    private void ClearForm()
    {
        GroomNameBox.Text = "";
        GroomPhoneBox.Text = "";
        BrideNameBox.Text = "";
        BridePhoneBox.Text = "";
        NationalIdBox.Text = "";
        AddressBox.Text = "";
        DescriptionBox.Text = "";
        GiftBox.Text = "";
        DepositBox.Text = "";
        
        ContractDatePicker.Date = null;
        EventDatePicker.Date = null;

        foreach (var service in Services)
        {
            service.IsSelected = false;
        }

        foreach (var print in PrintSizes)
        {
            print.IsSelected = false;
            print.Quantity = 0;
        }

        CalculateTotal();
    }

    private void ShowError(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "خطا",
            Content = message,
            CloseButtonText = "باشه",
            XamlRoot = this.XamlRoot
        };
        _ = dialog.ShowAsync();
    }

    private async Task ShowSuccessDialog(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "موفق",
            Content = message,
            CloseButtonText = "باشه",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}

// ==================== VIEW MODELS ====================

public class ServiceViewModel : Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public decimal Price { get; set; }
    
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string PriceFormatted => $"{Price:N0} تومان";
}

public class PrintSizeViewModel : Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject
{
    public string Id { get; set; } = "";
    public string Size { get; set; } = "";
    public decimal Price { get; set; }
    
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    private int _quantity;
    public int Quantity
    {
        get => _quantity;
        set
        {
            SetProperty(ref _quantity, value);
            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(TotalPriceFormatted));
        }
    }

    private decimal _totalPrice;
    public decimal TotalPrice
    {
        get => Price * Quantity;
        set
        {
            SetProperty(ref _totalPrice, value);
            OnPropertyChanged(nameof(TotalPriceFormatted));
        }
    }

    public string TotalPriceFormatted => $"{TotalPrice:N0} تومان";
}
