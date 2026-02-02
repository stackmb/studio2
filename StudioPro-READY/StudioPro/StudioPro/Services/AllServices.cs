using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MailKit.Net.Smtp;
using MimeKit;
using RomanticStudio.Database;
using RomanticStudio.Models;

namespace RomanticStudio.Services;

// ==================== EMAIL SERVICE ====================
public class EmailService
{
    public async Task<bool> SendPasswordRecoveryAsync(string toEmail, string password)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Romantic Studio", "noreply@romanticstudio.com"));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "بازیابی رمز عبور - Romantic Studio";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <div dir='rtl' style='font-family: Tahoma; padding: 20px;'>
                        <h2>بازیابی رمز عبور</h2>
                        <p>رمز عبور شما:</p>
                        <h3 style='background: #f0f0f0; padding: 10px; border-radius: 5px;'>{password}</h3>
                        <p>لطفاً پس از ورود، رمز عبور خود را تغییر دهید.</p>
                        <hr/>
                        <small>این ایمیل به صورت خودکار ارسال شده است.</small>
                    </div>"
            };

            // NOTE: در نسخه نهایی باید از SMTP واقعی استفاده شود
            // این فقط یک نمونه است
            using var client = new SmtpClient();
            // await client.ConnectAsync("smtp.gmail.com", 587, false);
            // await client.AuthenticateAsync("your-email@gmail.com", "your-password");
            // await client.SendAsync(message);
            // await client.DisconnectAsync(true);

            return true;
        }
        catch
        {
            return false;
        }
    }
}

// ==================== PDF SERVICE ====================
public class PdfService
{
    public async Task<string> GenerateContractPdfAsync(Contract contract, AppSettings settings)
    {
        // این بخش با QuestPDF پیاده‌سازی می‌شود
        // برای مختصر بودن، فقط یک نمونه ساده است
        
        var fileName = $"Contract_{contract.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);
        
        // TODO: Implement full PDF generation with QuestPDF
        // This is a placeholder
        await Task.CompletedTask;
        
        return filePath;
    }

    public async Task<string> GenerateReceiptPdfAsync(Contract contract, AppSettings settings, decimal amount)
    {
        var fileName = $"Receipt_{contract.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);
        
        // TODO: Implement receipt PDF generation
        await Task.CompletedTask;
        
        return filePath;
    }
}

// ==================== BACKUP SERVICE ====================
public class BackupService
{
    private readonly DatabaseContext _db;
    private readonly TelegramService _telegram;

    public BackupService(DatabaseContext db, TelegramService telegram)
    {
        _db = db;
        _telegram = telegram;
    }

    public async Task<(bool Success, string FilePath)> CreateBackupAsync()
    {
        try
        {
            var backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "RomanticStudio",
                "Backups"
            );

            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            var fileName = $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var filePath = Path.Combine(backupDir, fileName);

            // Get current database path
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RomanticStudio",
                "romantic_studio.db"
            );

            if (System.IO.File.Exists(dbPath))
            {
                System.IO.File.Copy(dbPath, filePath, true);

                // Save backup history
                var history = new BackupHistory
                {
                    BackupDate = DateTime.Now,
                    FileName = fileName,
                    FileSize = new FileInfo(filePath).Length,
                    Method = BackupMethod.Local,
                    IsSuccessful = true
                };

                _db.BackupHistories.Add(history);
                await _db.SaveChangesAsync();

                return (true, filePath);
            }

            return (false, string.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Backup error: {ex.Message}");
            return (false, string.Empty);
        }
    }

    public async Task<bool> RestoreBackupAsync(string backupFilePath)
    {
        try
        {
            if (!System.IO.File.Exists(backupFilePath))
                return false;

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RomanticStudio",
                "romantic_studio.db"
            );

            // Close database connections
            await _db.Database.CloseConnectionAsync();

            // Copy backup file
            System.IO.File.Copy(backupFilePath, dbPath, true);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SendBackupToTelegramAsync(string filePath)
    {
        try
        {
            var caption = $"📦 بک‌آپ خودکار - {DateTime.Now:yyyy/MM/dd HH:mm}";
            return await _telegram.SendFileAsync(filePath, caption);
        }
        catch
        {
            return false;
        }
    }
}

// ==================== SETTINGS SERVICE ====================
public class SettingsService
{
    private readonly DatabaseContext _db;

    public SettingsService(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        var settings = await _db.AppSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new AppSettings { Id = 1 };
            _db.AppSettings.Add(settings);
            await _db.SaveChangesAsync();
        }
        return settings;
    }

    public async Task<bool> SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            _db.AppSettings.Update(settings);
            await _db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> VerifyFinancialPasswordAsync(string password)
    {
        var settings = await GetSettingsAsync();
        return settings.FinancialPassword == password;
    }

    public async Task<bool> VerifySettingsPasswordAsync(string password)
    {
        var settings = await GetSettingsAsync();
        return settings.SettingsPassword == password;
    }
}

// ==================== CONTRACT SERVICE ====================
public class ContractService
{
    private readonly DatabaseContext _db;

    public ContractService(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<List<Contract>> GetAllContractsAsync()
    {
        return await _db.Contracts.OrderByDescending(c => c.ContractDate).ToListAsync();
    }

    public async Task<Contract?> GetContractByIdAsync(string id)
    {
        return await _db.Contracts.FindAsync(id);
    }

    public async Task<bool> AddContractAsync(Contract contract)
    {
        try
        {
            _db.Contracts.Add(contract);
            await _db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateContractAsync(Contract contract)
    {
        try
        {
            contract.UpdatedAt = DateTime.Now;
            _db.Contracts.Update(contract);
            await _db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteContractAsync(string id)
    {
        try
        {
            var contract = await _db.Contracts.FindAsync(id);
            if (contract != null)
            {
                _db.Contracts.Remove(contract);
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<Contract>> SearchContractsAsync(string searchTerm)
    {
        return await _db.Contracts
            .Where(c => c.ClientName.Contains(searchTerm) || 
                       c.PhoneNumber.Contains(searchTerm) ||
                       c.Description.Contains(searchTerm))
            .ToListAsync();
    }
}

// ==================== EQUIPMENT SERVICE ====================
public class EquipmentService
{
    private readonly DatabaseContext _db;

    public EquipmentService(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<List<EquipmentBooking>> GetAllBookingsAsync()
    {
        return await _db.EquipmentBookings.OrderByDescending(e => e.StartDate).ToListAsync();
    }

    public async Task<bool> AddBookingAsync(EquipmentBooking booking)
    {
        try
        {
            _db.EquipmentBookings.Add(booking);
            await _db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateBookingAsync(EquipmentBooking booking)
    {
        try
        {
            _db.EquipmentBookings.Update(booking);
            await _db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<EquipmentProvider>> GetProvidersAsync()
    {
        return await _db.EquipmentProviders.ToListAsync();
    }

    public async Task<List<StaffMember>> GetStaffAsync()
    {
        return await _db.StaffMembers.ToListAsync();
    }
}

// ==================== NOTIFICATION SERVICE ====================
public class NotificationService
{
    private readonly TelegramService _telegram;
    private readonly ContractService _contractService;
    private readonly SettingsService _settingsService;

    public NotificationService(
        TelegramService telegram, 
        ContractService contractService,
        SettingsService settingsService)
    {
        _telegram = telegram;
        _contractService = contractService;
        _settingsService = settingsService;
    }

    public async Task SendDebtWarningAsync(Contract contract)
    {
        if (contract.RemainingAmount <= 0)
            return;

        var settings = await _settingsService.GetSettingsAsync();
        if (!settings.EnableAutoSend)
            return;

        string message = $@"
⚠️ <b>هشدار بدهی</b>

👤 مشتری: {contract.ClientName}
📞 تلفن: {contract.PhoneNumber}
📅 تاریخ رویداد: {contract.EventDate:yyyy/MM/dd}

💰 مبلغ باقی‌مانده: {contract.RemainingAmount:N0} تومان

لطفاً پیگیری لازم را انجام دهید.
";

        await _telegram.SendMessageAsync(message);
    }

    public async Task SendDailyReportAsync()
    {
        var contracts = await _contractService.GetAllContractsAsync();
        var activeContracts = contracts.Where(c => c.Status == ContractStatus.Active).ToList();
        var totalDebt = activeContracts.Sum(c => c.RemainingAmount);

        string message = $@"
📊 <b>گزارش روزانه</b>

📅 تاریخ: {DateTime.Now:yyyy/MM/dd}

📝 قراردادهای فعال: {activeContracts.Count}
💰 مجموع بدهی: {totalDebt:N0} تومان

✅ گزارش ارسال شد.
";

        await _telegram.SendMessageAsync(message);
    }
}
