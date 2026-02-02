using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace RomanticStudio.Services;

public class HardwareIdService
{
    private string? _cachedHardwareId;

    public string GetHardwareId()
    {
        if (!string.IsNullOrEmpty(_cachedHardwareId))
            return _cachedHardwareId;

        try
        {
            // Get CPU ID
            string cpuId = GetCpuId();
            
            // Get Motherboard Serial
            string motherboardSerial = GetMotherboardSerial();
            
            // Get BIOS Serial
            string biosSerial = GetBiosSerial();
            
            // Combine and hash
            string combined = $"{cpuId}-{motherboardSerial}-{biosSerial}";
            string hashed = ComputeSha256Hash(combined);
            
            _cachedHardwareId = $"HW-{hashed.Substring(0, 12).ToUpper()}";
            return _cachedHardwareId;
        }
        catch
        {
            // Fallback: use machine name hash
            string fallback = Environment.MachineName + Environment.UserName;
            string hashed = ComputeSha256Hash(fallback);
            _cachedHardwareId = $"HW-{hashed.Substring(0, 12).ToUpper()}";
            return _cachedHardwareId;
        }
    }

    private string GetCpuId()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["ProcessorId"]?.ToString() ?? string.Empty;
            }
        }
        catch { }
        return string.Empty;
    }

    private string GetMotherboardSerial()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["SerialNumber"]?.ToString() ?? string.Empty;
            }
        }
        catch { }
        return string.Empty;
    }

    private string GetBiosSerial()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["SerialNumber"]?.ToString() ?? string.Empty;
            }
        }
        catch { }
        return string.Empty;
    }

    private string ComputeSha256Hash(string rawData)
    {
        using SHA256 sha256Hash = SHA256.Create();
        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return string.Concat(bytes.Select(b => b.ToString("x2")));
    }
}
