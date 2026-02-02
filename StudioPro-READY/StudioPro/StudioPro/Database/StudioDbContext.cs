using Microsoft.EntityFrameworkCore;
using StudioPro.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace StudioPro.Database;

public class StudioDbContext : DbContext
{
    private static readonly string DbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StudioPro",
        "studiopro.db"
    );

    public DbSet<Contract> Contracts { get; set; }
    public DbSet<EquipmentBooking> EquipmentBookings { get; set; }
    public DbSet<EquipmentProvider> EquipmentProviders { get; set; }
    public DbSet<StaffMember> StaffMembers { get; set; }
    public DbSet<ServicePrice> ServicePrices { get; set; }
    public DbSet<PrintSizePrice> PrintSizePrices { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<AppSettings> Settings { get; set; }
    public DbSet<BackupHistory> BackupHistories { get; set; }
    public DbSet<LicenseInfo> Licenses { get; set; }
    public DbSet<PhotoItem> Photos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var directory = Path.GetDirectoryName(DbPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory!);

        optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed Data
        SeedServicePrices(modelBuilder);
        SeedPrintSizes(modelBuilder);
        SeedStaffMembers(modelBuilder);
        SeedEquipmentProviders(modelBuilder);
        SeedDefaultSettings(modelBuilder);
        SeedDefaultLicense(modelBuilder);
    }

    private void SeedServicePrices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServicePrice>().HasData(
            new ServicePrice { Id = "SRV001", Label = "فیلمبرداری", Price = 5000000 },
            new ServicePrice { Id = "SRV002", Label = "عکاسی", Price = 3000000 },
            new ServicePrice { Id = "SRV003", Label = "آلبوم دیجیتال", Price = 2000000 },
            new ServicePrice { Id = "SRV004", Label = "تدوین فیلم", Price = 4000000 },
            new ServicePrice { Id = "SRV005", Label = "عکس‌های ویژه", Price = 1500000 },
            new ServicePrice { Id = "SRV006", Label = "پهپاد (Drone)", Price = 2500000 },
            new ServicePrice { Id = "SRV007", Label = "عکاسی فضای باز", Price = 3500000 },
            new ServicePrice { Id = "SRV008", Label = "لایت روم", Price = 1000000 }
        );
    }

    private void SeedPrintSizes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrintSizePrice>().HasData(
            new PrintSizePrice { Id = "PRT001", Size = "10x15", Price = 5000 },
            new PrintSizePrice { Id = "PRT002", Size = "13x18", Price = 8000 },
            new PrintSizePrice { Id = "PRT003", Size = "15x20", Price = 12000 },
            new PrintSizePrice { Id = "PRT004", Size = "20x30", Price = 25000 },
            new PrintSizePrice { Id = "PRT005", Size = "30x40", Price = 50000 },
            new PrintSizePrice { Id = "PRT006", Size = "40x60", Price = 100000 },
            new PrintSizePrice { Id = "PRT007", Size = "50x70", Price = 150000 },
            new PrintSizePrice { Id = "PRT008", Size = "آلبوم کلاسیک", Price = 500000 },
            new PrintSizePrice { Id = "PRT009", Size = "آلبوم دیجیتال", Price = 300000 }
        );
    }

    private void SeedStaffMembers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StaffMember>().HasData(
            new StaffMember { Id = "STF001", Name = "علی احمدی", Role = "مدیر", Phone = "09121234567" },
            new StaffMember { Id = "STF002", Name = "رضا محمدی", Role = "فیلمبردار", Phone = "09127654321" },
            new StaffMember { Id = "STF003", Name = "سارا کریمی", Role = "عکاس", Phone = "09131112222" },
            new StaffMember { Id = "STF004", Name = "مهدی رضایی", Role = "تدوینگر", Phone = "09141234567" },
            new StaffMember { Id = "STF005", Name = "فاطمه حسینی", Role = "آرایشگر", Phone = "09151237890" }
        );
    }

    private void SeedEquipmentProviders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EquipmentProvider>().HasData(
            new EquipmentProvider { Id = "PRV001", Name = "تجهیزات نوری", Phone = "02188112233" },
            new EquipmentProvider { Id = "PRV002", Name = "رنت دوربین حرفه‌ای", Phone = "02177665544" },
            new EquipmentProvider { Id = "PRV003", Name = "پهپاد پرواز", Phone = "09351234567" }
        );
    }

    private void SeedDefaultSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSettings>().HasData(
            new AppSettings
            {
                Id = 1,
                StudioName = "استودیو رویایی",
                StudioSlogan = "ثبت لحظات ماندگار شما",
                WatermarkText = "ROMANTIC STUDIO",
                Address = "تهران، خیابان ولیعصر، پلاک ۱۲۳",
                Phone = "021-88990011",
                ContractTerms = "شرایط و ضوابط قرارداد:\n۱. پرداخت بیعانه غیرقابل استرداد است.\n۲. تحویل فایل‌ها حداکثر ۳۰ روز کاری.\n۳. هرگونه تغییر در زمان نیاز به هماهنگی قبلی دارد.",
                ManagerName = "مدیر",
                MaxBackupRetention = 10,
                BackupFrequency = BackupFrequency.Weekly,
                BackupMethod = BackupMethod.Local,
                EnableAutoSend = false
            }
        );
    }

    private void SeedDefaultLicense(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LicenseInfo>().HasData(
            new LicenseInfo
            {
                Id = 1,
                IsActivated = false
            }
        );
    }

    public async Task InitializeAsync()
    {
        try
        {
            await Database.EnsureCreatedAsync();
            System.Diagnostics.Debug.WriteLine($"✅ Database initialized at: {DbPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Database init error: {ex.Message}");
        }
    }
}
