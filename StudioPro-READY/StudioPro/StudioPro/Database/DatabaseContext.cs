using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RomanticStudio.Models;

namespace RomanticStudio.Database;

public class DatabaseContext : DbContext
{
    private static readonly string DbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RomanticStudio",
        "romantic_studio.db"
    );

    public DbSet<Contract> Contracts { get; set; } = null!;
    public DbSet<EquipmentBooking> EquipmentBookings { get; set; } = null!;
    public DbSet<EquipmentProvider> EquipmentProviders { get; set; } = null!;
    public DbSet<StaffMember> StaffMembers { get; set; } = null!;
    public DbSet<ServicePrice> ServicePrices { get; set; } = null!;
    public DbSet<PrintSizePrice> PrintSizePrices { get; set; } = null!;
    public DbSet<AppSettings> AppSettings { get; set; } = null!;
    public DbSet<BackupHistory> BackupHistories { get; set; } = null!;
    public DbSet<LicenseInfo> LicenseInfo { get; set; } = null!;

    public DatabaseContext()
    {
        // Ensure directory exists
        var directory = Path.GetDirectoryName(DbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure decimal precision for SQLite
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("TEXT");
        }

        // Seed default data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Default Service Prices
        modelBuilder.Entity<ServicePrice>().HasData(
            new ServicePrice { Id = "p1", Label = "عکاسی عروسی (VIP)", Price = 18000000 },
            new ServicePrice { Id = "p2", Label = "فیلمبرداری مراسم", Price = 9500000 },
            new ServicePrice { Id = "p3", Label = "پکیج فرمالیته شمال", Price = 35000000 },
            new ServicePrice { Id = "p4", Label = "عکاسی کودک/تولد", Price = 3500000 },
            new ServicePrice { Id = "p5", Label = "پکیج کلیپ اسپرت", Price = 15000000 }
        );

        // Default Print Prices
        modelBuilder.Entity<PrintSizePrice>().HasData(
            new PrintSizePrice { Id = "sz1", Size = "20x30", Price = 150000 },
            new PrintSizePrice { Id = "sz2", Size = "30x40", Price = 280000 },
            new PrintSizePrice { Id = "sz3", Size = "50x70 (تخته شاسی)", Price = 950000 },
            new PrintSizePrice { Id = "sz4", Size = "100x70 (بوم پلاس)", Price = 3200000 }
        );

        // Default Staff
        modelBuilder.Entity<StaffMember>().HasData(
            new StaffMember { Id = "st-1", Name = "رضا علوی", Role = "عکاس ارشد", Phone = "09121112233" },
            new StaffMember { Id = "st-2", Name = "مریم احمدی", Role = "تدوینگر", Phone = "09122223344" },
            new StaffMember { Id = "st-3", Name = "حمید کریمی", Role = "نورپرداز", Phone = "09123334455" },
            new StaffMember { Id = "st-4", Name = "سارا جهانی", Role = "روتوشر", Phone = "09124445566" },
            new StaffMember { Id = "st-5", Name = "امیر مرادی", Role = "فیلمبردار", Phone = "09125556677" }
        );

        // Default Providers
        modelBuilder.Entity<EquipmentProvider>().HasData(
            new EquipmentProvider { Id = "pr-1", Name = "نورنگار", Phone = "02177665544" },
            new EquipmentProvider { Id = "pr-2", Name = "فلاشیران", Phone = "02188990011" },
            new EquipmentProvider { Id = "pr-3", Name = "شات سنتر", Phone = "02144556677" }
        );

        // Default App Settings
        modelBuilder.Entity<AppSettings>().HasData(
            new AppSettings 
            { 
                Id = 1,
                StudioName = "استودیو رویایی",
                StudioSlogan = "لحظات زیبا را جاودانه می‌کنیم",
                EnableAutoSend = true,
                MaxBackupRetention = 10
            }
        );

        // Default License Info
        modelBuilder.Entity<LicenseInfo>().HasData(
            new LicenseInfo 
            { 
                Id = 1,
                IsActivated = false,
                HardwareId = string.Empty
            }
        );
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Simple ensure created - don't use migrations on first run
            await Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database init error: {ex.Message}");
            // Continue even if DB fails - app should still open
        }
    }
}
