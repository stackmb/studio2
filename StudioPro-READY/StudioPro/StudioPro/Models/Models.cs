using System;
using System.ComponentModel.DataAnnotations;

namespace RomanticStudio.Models;

public class EquipmentBooking
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string EquipmentName { get; set; } = string.Empty;
    
    [Required]
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderPhone { get; set; } = string.Empty;
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public decimal RentalPrice { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DepositAmount { get; set; }
    
    public BookingStatus Status { get; set; } = BookingStatus.Reserved;
    public string? Notes { get; set; }
    public string? LinkedContractId { get; set; }
    public string AssignedStaffIds { get; set; } = "[]"; // JSON array
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public enum BookingStatus
{
    Reserved,
    InUse,
    Returned,
    Canceled
}

public class EquipmentProvider
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string Phone { get; set; } = string.Empty;
}

public class StaffMember
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string Role { get; set; } = string.Empty;
    
    [Required]
    public string Phone { get; set; } = string.Empty;
}

public class AppSettings
{
    [Key]
    public int Id { get; set; } = 1;
    
    public string StudioName { get; set; } = string.Empty;
    public string StudioSlogan { get; set; } = string.Empty;
    public string WatermarkText { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ContractTerms { get; set; } = string.Empty;
    
    public string? ManagerWhatsapp { get; set; }
    public string? ManagerEmail { get; set; }
    public string? ManagerCardNumber { get; set; }
    public string? ManagerName { get; set; }
    
    public string? FinancialPassword { get; set; }
    public string? SettingsPassword { get; set; }
    
    public string? TelegramBotToken { get; set; }
    public string? TelegramChatId { get; set; }
    
    public bool EnableAutoSend { get; set; }
    public BackupFrequency BackupFrequency { get; set; } = BackupFrequency.Disabled;
    public BackupMethod BackupMethod { get; set; } = BackupMethod.Local;
    public int MaxBackupRetention { get; set; } = 10;
    public DateTime? LastBackupDate { get; set; }
    
    public string? SecurityQuestion { get; set; }
    public string? SecurityAnswer { get; set; }
    
    public DateTime? LastAutoReportDate { get; set; }
}

public enum BackupFrequency
{
    Disabled,
    Daily,
    Weekly,
    Monthly
}

public enum BackupMethod
{
    Local,
    Telegram
}

public class BackupHistory
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public DateTime BackupDate { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public BackupMethod Method { get; set; }
    public bool IsSuccessful { get; set; }
}

public class LicenseInfo
{
    [Key]
    public int Id { get; set; } = 1;
    
    public string? LicenseKey { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActivated { get; set; }
    public string HardwareId { get; set; } = string.Empty;
    public DateTime? LastTimeCheck { get; set; }
}
