using Microsoft.EntityFrameworkCore;
using StudioPro.Database;
using StudioPro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudioPro.Services;

// ==================== CONTRACT SERVICE ====================

public class ContractService
{
    private readonly StudioDbContext _context;

    public ContractService(StudioDbContext context)
    {
        _context = context;
    }

    public async Task<List<Contract>> GetAllAsync()
    {
        return await _context.Contracts.OrderByDescending(c => c.ContractDate).ToListAsync();
    }

    public async Task<Contract?> GetByIdAsync(string id)
    {
        return await _context.Contracts.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> AddAsync(Contract contract)
    {
        try
        {
            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> UpdateAsync(Contract contract)
    {
        try
        {
            _context.Contracts.Update(contract);
            await _context.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            var contract = await GetByIdAsync(id);
            if (contract != null)
            {
                _context.Contracts.Remove(contract);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        catch { return false; }
    }

    public async Task<List<Contract>> SearchAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return await GetAllAsync();

        term = term.ToLower();
        return await _context.Contracts
            .Where(c => c.ClientName.ToLower().Contains(term) ||
                       c.Description.ToLower().Contains(term) ||
                       c.Id.ToLower().Contains(term) ||
                       c.PhoneNumber.Contains(term))
            .ToListAsync();
    }
}

// ==================== EQUIPMENT SERVICE ====================

public class EquipmentService
{
    private readonly StudioDbContext _context;

    public EquipmentService(StudioDbContext context)
    {
        _context = context;
    }

    public async Task<List<EquipmentBooking>> GetAllBookingsAsync()
    {
        return await _context.EquipmentBookings.OrderByDescending(e => e.StartDate).ToListAsync();
    }

    public async Task<bool> AddBookingAsync(EquipmentBooking booking)
    {
        try
        {
            _context.EquipmentBookings.Add(booking);
            await _context.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> UpdateBookingAsync(EquipmentBooking booking)
    {
        try
        {
            _context.EquipmentBookings.Update(booking);
            await _context.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<List<EquipmentProvider>> GetProvidersAsync()
    {
        return await _context.EquipmentProviders.ToListAsync();
    }

    public async Task<List<StaffMember>> GetStaffAsync()
    {
        return await _context.StaffMembers.ToListAsync();
    }

    public async Task<List<EquipmentBooking>> GetByContractIdAsync(string contractId)
    {
        return await _context.EquipmentBookings
            .Where(e => e.LinkedContractId == contractId)
            .ToListAsync();
    }
}

// ==================== APPOINTMENT SERVICE ====================

public class AppointmentService
{
    private readonly StudioDbContext _context;

    public AppointmentService(StudioDbContext context)
    {
        _context = context;
    }

    public async Task<List<Appointment>> GetAllAsync()
    {
        return await _context.Appointments.OrderBy(a => a.Date).ThenBy(a => a.Time).ToListAsync();
    }

    public async Task<bool> AddAsync(Appointment appointment)
    {
        try
        {
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> UpdateStatusAsync(string id, AppointmentStatus status)
    {
        try
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = status;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        catch { return false; }
    }
}

// ==================== PHOTO SERVICE ====================

public class PhotoService
{
    private readonly StudioDbContext _context;

    public PhotoService(StudioDbContext context)
    {
        _context = context;
    }

    public async Task<List<PhotoItem>> GetAllAsync()
    {
        return await _context.Photos.OrderByDescending(p => p.Date).ToListAsync();
    }

    public async Task<bool> AddPhotoAsync(string filePath, string title, string category)
    {
        try
        {
            var photo = new PhotoItem
            {
                FilePath = filePath,
                Title = title,
                Category = category,
                Date = DateTime.Now
            };
            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            var photo = await _context.Photos.FindAsync(id);
            if (photo != null)
            {
                _context.Photos.Remove(photo);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        catch { return false; }
    }
}

// ==================== SETTINGS SERVICE ====================

public class SettingsService
{
    private readonly StudioDbContext _context;

    public SettingsService(StudioDbContext context)
    {
        _context = context;
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        var settings = await _context.Settings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new AppSettings();
            _context.Settings.Add(settings);
            await _context.SaveChangesAsync();
        }
        return settings;
    }

    public async Task<bool> SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            _context.Settings.Update(settings);
            await _context.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> VerifyPasswordAsync(string type, string password)
    {
        var settings = await GetSettingsAsync();
        return type switch
        {
            "financial" => settings.FinancialPassword == password,
            "settings" => settings.SettingsPassword == password,
            _ => false
        };
    }

    public async Task<List<ServicePrice>> GetServicePricesAsync()
    {
        return await _context.ServicePrices.ToListAsync();
    }

    public async Task<List<PrintSizePrice>> GetPrintPricesAsync()
    {
        return await _context.PrintSizePrices.ToListAsync();
    }
}
