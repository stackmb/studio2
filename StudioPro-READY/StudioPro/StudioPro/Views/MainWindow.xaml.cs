using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Extensions.DependencyInjection;
using RomanticStudio.Services;
using RomanticStudio.Views.Pages;
using System;

namespace RomanticStudio.Views;

public sealed partial class MainWindow : Window
{
    private readonly LicenseService _licenseService;
    private readonly SettingsService _settingsService;
    private readonly BackupService _backupService;

    public MainWindow()
    {
        this.InitializeComponent();
        
        _licenseService = App.Services.GetRequiredService<LicenseService>();
        _settingsService = App.Services.GetRequiredService<SettingsService>();
        _backupService = App.Services.GetRequiredService<BackupService>();

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        // Load studio name
        var settings = await _settingsService.GetSettingsAsync();
        if (!string.IsNullOrEmpty(settings.StudioName))
        {
            StudioNameText.Text = settings.StudioName;
            this.Title = settings.StudioName;
        }

        // Update license info
        var remainingTime = await _licenseService.GetRemainingTimeAsync();
        if (!string.IsNullOrEmpty(remainingTime))
        {
            LicenseExpiryText.Text = $"{remainingTime} باقی‌مانده";
        }

        // Navigate to dashboard
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tag)
        {
            Type? pageType = tag switch
            {
                "Dashboard" => typeof(DashboardPage),
                "Contracts" => typeof(ContractsPage),
                "Calendar" => typeof(CalendarPage),
                "Equipment" => typeof(EquipmentPage),
                "Financial" => typeof(FinancialPage),
                _ => null
            };

            if (pageType != null)
            {
                ContentFrame.Navigate(pageType);
            }
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(SettingsPage));
    }

    private async void BackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "بک‌آپ گیری",
            Content = "آیا می‌خواهید از اطلاعات پشتیبان تهیه کنید؟",
            PrimaryButtonText = "بله",
            CloseButtonText = "خیر",
            XamlRoot = this.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var (success, filePath) = await _backupService.CreateBackupAsync();
            
            var resultDialog = new ContentDialog
            {
                Title = success ? "✅ موفق" : "❌ خطا",
                Content = success ? $"بک‌آپ با موفقیت ذخیره شد:\n{filePath}" : "خطا در تهیه بک‌آپ",
                CloseButtonText = "باشه",
                XamlRoot = this.Content.XamlRoot
            };
            
            await resultDialog.ShowAsync();
        }
    }
}
