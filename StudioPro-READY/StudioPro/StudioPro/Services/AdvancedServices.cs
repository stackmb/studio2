using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using StudioPro.Database;
using StudioPro.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace StudioPro.Services;

// ==================== TELEGRAM SERVICE ====================

public class TelegramService
{
    private TelegramBotClient? _botClient;
    private string? _chatId;

    public void Configure(string botToken, string chatId)
    {
        _botClient = new TelegramBotClient(botToken);
        _chatId = chatId;
    }

    public async Task<bool> SendMessageAsync(string message, bool useHtml = true)
    {
        if (_botClient == null || string.IsNullOrEmpty(_chatId)) return false;

        try
        {
            await _botClient.SendTextMessageAsync(
                chatId: _chatId,
                text: message,
                parseMode: useHtml ? Telegram.Bot.Types.Enums.ParseMode.Html : null
            );
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> SendFileAsync(string filePath, string? caption = null)
    {
        if (_botClient == null || string.IsNullOrEmpty(_chatId)) return false;

        try
        {
            await using var stream = System.IO.File.OpenRead(filePath);
            var inputFile = InputFile.FromStream(stream, Path.GetFileName(filePath));
            
            await _botClient.SendDocumentAsync(
                chatId: _chatId,
                document: inputFile,
                caption: caption
            );
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> SendPhotoAsync(string filePath, string? caption = null)
    {
        if (_botClient == null || string.IsNullOrEmpty(_chatId)) return false;

        try
        {
            await using var stream = System.IO.File.OpenRead(filePath);
            var inputFile = InputFile.FromStream(stream, Path.GetFileName(filePath));
            
            await _botClient.SendPhotoAsync(
                chatId: _chatId,
                photo: inputFile,
                caption: caption
            );
            return true;
        }
        catch { return false; }
    }
}

// ==================== PDF SERVICE ====================

public class PdfService
{
    public async Task<string> GenerateContractAsync(Contract contract, AppSettings settings)
    {
        return await Task.Run(() =>
        {
            var fileName = $"Contract_{contract.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            var filePath = Path.Combine(Path.GetTempPath(), fileName);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(container => Header(container, contract, settings));
                    page.Content().Element(container => Content(container, contract));
                    page.Footer().Element(Footer);
                });
            }).GeneratePdf(filePath);

            return filePath;
        });
    }

    private void Header(IContainer container, Contract contract, AppSettings settings)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(settings.StudioName).FontSize(20).Bold();
                column.Item().Text(settings.StudioSlogan).FontSize(10);
            });

            row.ConstantItem(150).Column(column =>
            {
                column.Item().Text($"شماره: {contract.Id}");
                column.Item().Text($"تاریخ: {contract.ContractDate:yyyy/MM/dd}");
            });
        });
    }

    private void Content(IContainer container, Contract contract)
    {
        container.Column(column =>
        {
            column.Item().Text("مشخصات مشتری").FontSize(14).Bold();
            column.Item().Text($"نام: {contract.ClientName}");
            column.Item().Text($"تلفن: {contract.PhoneNumber}");
            column.Item().Text($"تاریخ مراسم: {contract.EventDate:yyyy/MM/dd}");
            
            column.Item().PaddingTop(20);
            column.Item().Text("مبالغ").FontSize(14).Bold();
            column.Item().Text($"مبلغ کل: {contract.TotalAmount:N0} تومان");
            column.Item().Text($"بیعانه: {contract.Deposit:N0} تومان");
            column.Item().Text($"باقیمانده: {contract.RemainingAmount:N0} تومان");
        });
    }

    private void Footer(IContainer container)
    {
        container.AlignBottom().Row(row =>
        {
            row.RelativeItem().Text("امضای استودیو").AlignCenter();
            row.RelativeItem().Text("امضای مشتری").AlignCenter();
        });
    }

    public async Task<string> GenerateReceiptAsync(Contract contract, AppSettings settings, decimal amount)
    {
        return await Task.Run(() =>
        {
            var fileName = $"Receipt_{contract.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            var filePath = Path.Combine(Path.GetTempPath(), fileName);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(148, 210, Unit.Millimetre);
                    page.Margin(1, Unit.Centimetre);
                    page.Content().Column(column =>
                    {
                        column.Item().Text("رسید").FontSize(16).Bold().AlignCenter();
                        column.Item().Text(settings.StudioName).AlignCenter();
                        column.Item().PaddingTop(10);
                        column.Item().Text($"مشتری: {contract.ClientName}");
                        column.Item().Text($"مبلغ: {amount:N0} تومان");
                        column.Item().Text($"تاریخ: {DateTime.Now:yyyy/MM/dd}");
                    });
                });
            }).GeneratePdf(filePath);

            return filePath;
        });
    }
}

// ==================== BACKUP SERVICE ====================

public class BackupService
{
    private readonly StudioDbContext _context;
    private readonly TelegramService _telegram;

    public BackupService(StudioDbContext context, TelegramService telegram)
    {
        _context = context;
        _telegram = telegram;
    }

    public async Task<(bool success, string filePath)> CreateBackupAsync()
    {
        try
        {
            var backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "StudioPro",
                "Backups"
            );
            Directory.CreateDirectory(backupDir);

            var fileName = $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var backupPath = Path.Combine(backupDir, fileName);

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "StudioPro",
                "studiopro.db"
            );

            System.IO.File.Copy(dbPath, backupPath, true);

            // ذخیره در تاریخچه
            var history = new BackupHistory
            {
                Date = DateTime.Now,
                Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                FilePath = backupPath,
                FileSize = new FileInfo(backupPath).Length,
                Method = "Local"
            };
            _context.BackupHistories.Add(history);
            await _context.SaveChangesAsync();

            return (true, backupPath);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    public async Task<bool> RestoreBackupAsync(string filePath)
    {
        try
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "StudioPro",
                "studiopro.db"
            );

            System.IO.File.Copy(filePath, dbPath, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SendBackupToTelegramAsync(string filePath)
    {
        return await _telegram.SendFileAsync(filePath, "🗄️ Backup Database");
    }
}

// ==================== LICENSE SERVICE ====================

public class LicenseService
{
    private readonly StudioDbContext _context;
    private readonly HardwareIdService _hardware;
    private const string SALT = "STUDIOPRO_2026_SECURE";

    public LicenseService(StudioDbContext context, HardwareIdService hardware)
    {
        _context = context;
        _hardware = hardware;
    }

    public async Task<bool> CheckLicenseAsync()
    {
        var license = await _context.Licenses.FirstOrDefaultAsync();
        if (license == null || !license.IsActivated) return false;

        // بررسی Hardware ID
        var currentHwId = _hardware.GetHardwareId();
        if (license.HardwareId != currentHwId) return false;

        // بررسی تاریخ انقضا
        if (license.ExpiryDate.HasValue && license.ExpiryDate < DateTime.Now)
            return false;

        // بررسی دستکاری زمان
        if (license.LastValidationDate.HasValue && license.LastValidationDate > DateTime.Now)
            return false;

        license.LastValidationDate = DateTime.Now;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ActivateLicenseAsync(string licenseKey)
    {
        try
        {
            var decrypted = DecryptLicense(licenseKey);
            if (string.IsNullOrEmpty(decrypted)) return false;

            var parts = decrypted.Split('|');
            if (parts.Length != 3) return false;

            var hwId = parts[0];
            var expiryStr = parts[1];
            var signature = parts[2];

            // بررسی Hardware ID
            if (hwId != _hardware.GetHardwareId()) return false;

            // بررسی امضا
            var expectedSig = ComputeSignature(hwId + expiryStr);
            if (signature != expectedSig) return false;

            var license = await _context.Licenses.FirstOrDefaultAsync();
            if (license == null)
            {
                license = new LicenseInfo();
                _context.Licenses.Add(license);
            }

            license.LicenseKey = licenseKey;
            license.HardwareId = hwId;
            license.ActivationDate = DateTime.Now;
            license.ExpiryDate = DateTime.Parse(expiryStr);
            license.IsActivated = true;
            license.LastValidationDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string DecryptLicense(string encrypted)
    {
        try
        {
            var key = SHA256.HashData(Encoding.UTF8.GetBytes(SALT));
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = new byte[16];
            
            var bytes = Convert.FromBase64String(encrypted);
            using var decryptor = aes.CreateDecryptor();
            var decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return string.Empty;
        }
    }

    private string ComputeSignature(string data)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(data + SALT));
        return Convert.ToBase64String(bytes).Substring(0, 12);
    }
}

// ==================== HARDWARE ID SERVICE ====================

public class HardwareIdService
{
    public string GetHardwareId()
    {
        try
        {
            var cpu = GetCpuId();
            var mb = GetMotherboardSerial();
            var combined = $"{cpu}-{mb}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
            return "HW-" + Convert.ToBase64String(hash).Substring(0, 12).Replace("+", "").Replace("/", "");
        }
        catch
        {
            return "HW-" + Environment.MachineName.GetHashCode().ToString("X");
        }
    }

    private string GetCpuId()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                return obj["ProcessorId"]?.ToString() ?? "";
            }
        }
        catch { }
        return "";
    }

    private string GetMotherboardSerial()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
            foreach (var obj in searcher.Get())
            {
                return obj["SerialNumber"]?.ToString() ?? "";
            }
        }
        catch { }
        return "";
    }
}

// ==================== NOTIFICATION SERVICE ====================

public class NotificationService
{
    private readonly TelegramService _telegram;

    public NotificationService(TelegramService telegram)
    {
        _telegram = telegram;
    }

    public async Task<bool> SendDebtWarningAsync(List<Contract> contracts)
    {
        var message = "🔔 *هشدار بدهی*\n\n";
        foreach (var c in contracts)
        {
            message += $"• {c.ClientName}\n";
            message += $"  💰 {c.RemainingAmount:N0} تومان\n";
            message += $"  📞 {c.PhoneNumber}\n\n";
        }
        return await _telegram.SendMessageAsync(message);
    }

    public async Task<bool> SendDailyReportAsync()
    {
        var message = $"📊 *گزارش روزانه*\n\n";
        message += $"تاریخ: {DateTime.Now:yyyy/MM/dd}\n";
        return await _telegram.SendMessageAsync(message);
    }
}
