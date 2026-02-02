using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using RomanticStudio.Services;
using RomanticStudio.Models;
using System;
using Windows.Storage.Pickers;
using Windows.Storage;

namespace RomanticStudio.Views.Pages;

public sealed partial class SettingsPage : Page
{
    private readonly SettingsService _settingsService;
    private readonly TelegramService _telegramService;
    private readonly BackupService _backupService;
    private readonly HardwareIdService _hardwareIdService;
    private AppSettings? _settings;

    public SettingsPage()
    {
        this.InitializeComponent();
        
        _settingsService = App.Services.GetRequiredService<SettingsService>();
        _telegramService = App.Services.GetRequiredService<TelegramService>();
        _backupService = App.Services.GetRequiredService<BackupService>();
        _hardwareIdService = App.Services.GetRequiredService<HardwareIdService>();

        LoadSettingsAsync();
    }

    private async void LoadSettingsAsync()
    {
        _settings = await _settingsService.GetSettingsAsync();
        
        StudioNameBox.Text = _settings.StudioName;
        PhoneBox.Text = _settings.Phone;
        AddressBox.Text = _settings.Address;
        TelegramTokenBox.Text = _settings.TelegramBotToken ?? "";
        TelegramChatIdBox.Text = _settings.TelegramChatId ?? "";
        AutoSendCheckBox.IsChecked = _settings.EnableAutoSend;
    }

    private async void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_settings == null) return;

        _settings.StudioName = StudioNameBox.Text.Trim();
        _settings.Phone = PhoneBox.Text.Trim();
        _settings.Address = AddressBox.Text.Trim();
        _settings.TelegramBotToken = TelegramTokenBox.Text.Trim();
        _settings.TelegramChatId = TelegramChatIdBox.Text.Trim();
        _settings.EnableAutoSend = AutoSendCheckBox.IsChecked ?? false;

        bool success = await _settingsService.SaveSettingsAsync(_settings);
        
        if (success)
        {
            // Configure Telegram with new settings
            if (!string.IsNullOrEmpty(_settings.TelegramBotToken) && 
                !string.IsNullOrEmpty(_settings.TelegramChatId))
            {
                _telegramService.Configure(_settings.TelegramBotToken, _settings.TelegramChatId);
            }

            ShowNotification("✅ تنظیمات ذخیره شد");
        }
        else
        {
            ShowNotification("❌ خطا در ذخیره تنظیمات");
        }
    }

    private async void TestTelegramButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(TelegramTokenBox.Text) || 
            string.IsNullOrEmpty(TelegramChatIdBox.Text))
        {
            ShowNotification("⚠️ لطفاً ابتدا توکن و Chat ID را وارد کنید");
            return;
        }

        _telegramService.Configure(TelegramTokenBox.Text, TelegramChatIdBox.Text);
        
        bool success = await _telegramService.SendMessageAsync(
            "🧪 <b>تست اتصال</b>\n\nاتصال به تلگرام با موفقیت برقرار شد! ✅"
        );

        if (success)
        {
            ShowNotification("✅ پیام آزمایشی ارسال شد! تلگرام خود را بررسی کنید.");
        }
        else
        {
            ShowNotification("❌ خطا در ارسال پیام. لطفاً توکن و Chat ID را بررسی کنید.");
        }
    }

    private async void CreateBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var (success, filePath) = await _backupService.CreateBackupAsync();
        
        if (success)
        {
            ShowNotification($"✅ بک‌آپ با موفقیت ذخیره شد:\n{filePath}");
        }
        else
        {
            ShowNotification("❌ خطا در تهیه بک‌آپ");
        }
    }

    private async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".db");
        
        // Get window handle - use static property
        var window = App.MainWindow;
        if (window != null)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }
        
        StorageFile file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "⚠️ بازیابی بک‌آپ",
                Content = "تمام اطلاعات فعلی جایگزین می‌شود. ادامه می‌دهید؟",
                PrimaryButtonText = "بله",
                CloseButtonText = "خیر",
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                bool success = await _backupService.RestoreBackupAsync(file.Path);
                
                if (success)
                {
                    ShowNotification("✅ بازیابی انجام شد. لطفاً برنامه را مجدداً اجرا کنید.");
                }
                else
                {
                    ShowNotification("❌ خطا در بازیابی");
                }
            }
        }
    }

    private async void SendBackupToTelegramButton_Click(object sender, RoutedEventArgs e)
    {
        var (success, filePath) = await _backupService.CreateBackupAsync();
        
        if (!success)
        {
            ShowNotification("❌ خطا در تهیه بک‌آپ");
            return;
        }

        bool sent = await _backupService.SendBackupToTelegramAsync(filePath);
        
        if (sent)
        {
            ShowNotification("✅ بک‌آپ به تلگرام ارسال شد");
        }
        else
        {
            ShowNotification("❌ خطا در ارسال به تلگرام");
        }
    }

    private async void SendTicketButton_Click(object sender, RoutedEventArgs e)
    {
        string message = TicketMessageBox.Text.Trim();
        
        if (string.IsNullOrEmpty(message))
        {
            ShowNotification("⚠️ لطفاً پیام خود را وارد کنید");
            return;
        }

        string hwid = _hardwareIdService.GetHardwareId();
        string userName = _settings?.ManagerName ?? "کاربر";
        
        bool success = await _telegramService.SendSupportTicketAsync(userName, hwid, message);
        
        if (success)
        {
            TicketMessageBox.Text = "";
            ShowNotification("✅ تیکت شما ارسال شد. پشتیبانی به زودی پاسخ خواهد داد.");
        }
        else
        {
            ShowNotification("❌ خطا در ارسال تیکت");
        }
    }

    private async void ShowNotification(string message)
    {
        var dialog = new ContentDialog
        {
            Content = message,
            CloseButtonText = "باشه",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
