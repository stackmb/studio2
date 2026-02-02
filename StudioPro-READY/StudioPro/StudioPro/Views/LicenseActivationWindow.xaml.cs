using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using RomanticStudio.Services;
using Windows.ApplicationModel.DataTransfer;
using System;

namespace RomanticStudio.Views;

public sealed partial class LicenseActivationWindow : Window
{
    private readonly LicenseService _licenseService;
    private readonly TelegramService _telegramService;
    private readonly HardwareIdService _hardwareIdService;

    public LicenseActivationWindow()
    {
        this.InitializeComponent();
        
        _licenseService = App.Services.GetRequiredService<LicenseService>();
        _telegramService = App.Services.GetRequiredService<TelegramService>();
        _hardwareIdService = App.Services.GetRequiredService<HardwareIdService>();

        // Display hardware ID
        string hwid = _hardwareIdService.GetHardwareId();
        HardwareIdText.Text = hwid;
    }

    private void CopyHwIdButton_Click(object sender, RoutedEventArgs e)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(HardwareIdText.Text);
        Clipboard.SetContent(dataPackage);

        ShowInfoBar("✅ کپی شد", "شناسه سخت‌افزار در کلیپ‌بورد کپی شد");
    }

    private async void ActivateButton_Click(object sender, RoutedEventArgs e)
    {
        string licenseKey = LicenseKeyInput.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            ShowInfoBar("⚠️ خطا", "لطفاً کلید لایسنس را وارد کنید");
            return;
        }

        LoadingRing.IsActive = true;
        LoadingRing.Visibility = Visibility.Visible;
        ActivateButton.IsEnabled = false;

        var result = await _licenseService.ActivateLicenseAsync(licenseKey);

        LoadingRing.IsActive = false;
        LoadingRing.Visibility = Visibility.Collapsed;
        ActivateButton.IsEnabled = true;

        if (result.Success)
        {
            var dialog = new ContentDialog
            {
                Title = "✅ فعال‌سازی موفق",
                Content = result.Message,
                CloseButtonText = "ورود به برنامه",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();

            // Open main window
            var mainWindow = App.Services.GetRequiredService<MainWindow>();
            mainWindow.Activate();
            this.Close();
        }
        else
        {
            ShowInfoBar("❌ خطا", result.Message);
        }
    }

    private async void RequestLicenseButton_Click(object sender, RoutedEventArgs e)
    {
        string userName = UserNameInput.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(userName))
        {
            ShowInfoBar("⚠️ خطا", "لطفاً نام خود را وارد کنید");
            return;
        }

        LoadingRing.IsActive = true;
        LoadingRing.Visibility = Visibility.Visible;
        RequestLicenseButton.IsEnabled = false;

        string hwid = _hardwareIdService.GetHardwareId();
        bool success = await _telegramService.SendLicenseRequestAsync(hwid, userName);

        LoadingRing.IsActive = false;
        LoadingRing.Visibility = Visibility.Collapsed;
        RequestLicenseButton.IsEnabled = true;

        if (success)
        {
            ShowInfoBar("✅ موفق", "درخواست شما به پشتیبانی ارسال شد. لطفاً منتظر دریافت لایسنس باشید.");
        }
        else
        {
            ShowInfoBar("❌ خطا", "خطا در ارسال درخواست. لطفاً اتصال اینترنت خود را بررسی کنید.");
        }
    }

    private async void ShowInfoBar(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "باشه",
            XamlRoot = this.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }
}
