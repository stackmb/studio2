using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using RomanticStudio.Services;
using RomanticStudio.Views;
using RomanticStudio.Database;

namespace RomanticStudio;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static Window MainWindow { get; private set; } = null!;

    public App()
    {
        this.InitializeComponent();
        
        // Configure Services
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Database
        services.AddSingleton<DatabaseContext>();
        
        // Services
        services.AddSingleton<LicenseService>();
        services.AddSingleton<TelegramService>();
        services.AddSingleton<EmailService>();
        services.AddSingleton<PdfService>();
        services.AddSingleton<BackupService>();
        services.AddSingleton<HardwareIdService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ContractService>();
        services.AddSingleton<EquipmentService>();
        services.AddSingleton<NotificationService>();
        
        // Views
        services.AddTransient<MainWindow>();
        services.AddTransient<LicenseActivationWindow>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Show window immediately - no async/await to prevent hanging
        MainWindow = new LicenseActivationWindow();
        MainWindow.Activate();
        
        // Initialize in background (fire and forget)
        _ = InitializeAppAsync();
    }

    private async Task InitializeAppAsync()
    {
        try
        {
            // Initialize Database in background
            var dbContext = Services.GetRequiredService<DatabaseContext>();
            await dbContext.InitializeAsync();
            
            System.Diagnostics.Debug.WriteLine("Database initialized successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Background init error: {ex.Message}");
        }
    }
}
