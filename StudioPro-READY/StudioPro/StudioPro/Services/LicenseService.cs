using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RomanticStudio.Database;
using RomanticStudio.Models;

namespace RomanticStudio.Services;

public class LicenseService
{
    private readonly DatabaseContext _db;
    private readonly HardwareIdService _hardwareIdService;
    private const string SALT = "ROMANTIC_V3_SECURE_2025";
    private const string MAGIC_SIGNATURE = "RMT-APP-VERIFIED";

    public LicenseService(DatabaseContext db, HardwareIdService hardwareIdService)
    {
        _db = db;
        _hardwareIdService = hardwareIdService;
    }

    public async Task<bool> CheckLicenseAsync()
    {
        try
        {
            var licenseInfo = await _db.LicenseInfo.FirstOrDefaultAsync();
            if (licenseInfo == null || !licenseInfo.IsActivated || string.IsNullOrEmpty(licenseInfo.LicenseKey))
                return false;

            // Check time manipulation
            if (licenseInfo.LastTimeCheck.HasValue)
            {
                if (DateTime.Now < licenseInfo.LastTimeCheck.Value.AddMinutes(-1))
                {
                    // Time was rolled back
                    licenseInfo.IsActivated = false;
                    await _db.SaveChangesAsync();
                    return false;
                }
            }

            licenseInfo.LastTimeCheck = DateTime.Now;
            await _db.SaveChangesAsync();

            // Validate license
            var result = ValidateLicense(licenseInfo.LicenseKey);
            if (!result.IsValid || DateTime.Now > result.ExpiryDate)
            {
                licenseInfo.IsActivated = false;
                await _db.SaveChangesAsync();
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, string Message)> ActivateLicenseAsync(string licenseKey)
    {
        try
        {
            var result = ValidateLicense(licenseKey);
            if (!result.IsValid)
                return (false, "کلید لایسنس نامعتبر است.");

            if (DateTime.Now > result.ExpiryDate)
                return (false, "لایسنس منقضی شده است.");

            var licenseInfo = await _db.LicenseInfo.FirstOrDefaultAsync();
            if (licenseInfo == null)
            {
                licenseInfo = new LicenseInfo { Id = 1 };
                _db.LicenseInfo.Add(licenseInfo);
            }

            licenseInfo.LicenseKey = licenseKey;
            licenseInfo.ExpiryDate = result.ExpiryDate;
            licenseInfo.IsActivated = true;
            licenseInfo.HardwareId = _hardwareIdService.GetHardwareId();
            licenseInfo.LastTimeCheck = DateTime.Now;

            await _db.SaveChangesAsync();

            return (true, $"فعال‌سازی موفق! لایسنس تا {result.ExpiryDate:yyyy/MM/dd} اعتبار دارد.");
        }
        catch (Exception ex)
        {
            return (false, $"خطا در فعال‌سازی: {ex.Message}");
        }
    }

    public string GetHardwareId()
    {
        return _hardwareIdService.GetHardwareId();
    }

    public async Task<string?> GetRemainingTimeAsync()
    {
        var licenseInfo = await _db.LicenseInfo.FirstOrDefaultAsync();
        if (licenseInfo == null || !licenseInfo.ExpiryDate.HasValue)
            return null;

        var remaining = licenseInfo.ExpiryDate.Value - DateTime.Now;
        if (remaining.TotalSeconds <= 0)
            return "منقضی شده";

        if (remaining.TotalDays >= 1)
            return $"{(int)remaining.TotalDays} روز";
        
        if (remaining.TotalHours >= 1)
            return $"{(int)remaining.TotalHours} ساعت";
        
        return $"{(int)remaining.TotalMinutes} دقیقه";
    }

    private (bool IsValid, DateTime ExpiryDate) ValidateLicense(string licenseKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(licenseKey) || licenseKey.Length < 30)
                return (false, DateTime.MinValue);

            string hardwareId = _hardwareIdService.GetHardwareId();
            
            // Decrypt license
            byte[] encryptedBytes = Convert.FromBase64String(licenseKey);
            
            // Extract IV (first 16 bytes)
            byte[] iv = new byte[16];
            Array.Copy(encryptedBytes, 0, iv, 0, 16);
            
            // Extract ciphertext
            byte[] ciphertext = new byte[encryptedBytes.Length - 16];
            Array.Copy(encryptedBytes, 16, ciphertext, 0, ciphertext.Length);
            
            // Generate key from SALT
            byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(SALT));
            
            // Decrypt
            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            using var decryptor = aes.CreateDecryptor();
            byte[] decryptedBytes = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
            string jsonStr = Encoding.UTF8.GetString(decryptedBytes);
            
            // Parse JSON
            var data = JsonConvert.DeserializeObject<LicenseData>(jsonStr);
            if (data == null || data.Sid != hardwareId || data.Sig != MAGIC_SIGNATURE)
                return (false, DateTime.MinValue);
            
            DateTime expiryDate = DateTimeOffset.FromUnixTimeSeconds(data.Expiry).DateTime;
            return (true, expiryDate);
        }
        catch
        {
            return (false, DateTime.MinValue);
        }
    }

    private class LicenseData
    {
        [JsonProperty("sid")]
        public string Sid { get; set; } = string.Empty;
        
        [JsonProperty("sig")]
        public string Sig { get; set; } = string.Empty;
        
        [JsonProperty("expiry")]
        public long Expiry { get; set; }
    }
}
