using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RomanticStudio.Models;

public class Contract
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public DateTime ContractDate { get; set; } = DateTime.Now;
    public DateTime EventDate { get; set; }
    
    [Required]
    public string ClientName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    public string? ClientAddress { get; set; }
    public string? LocationUrl { get; set; }
    
    public string? BrideName { get; set; }
    public string? BridePhoneNumber { get; set; }
    
    public string Description { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
    public string? GiftDescription { get; set; }
    
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    
    public string SelectedServices { get; set; } = "[]"; // JSON array
    public string SelectedPrints { get; set; } = "[]"; // JSON array
    
    public decimal TotalAmount { get; set; }
    public decimal Deposit { get; set; }
    public decimal RemainingAmount { get; set; }
    
    public ContractStatus Status { get; set; } = ContractStatus.Active;
    public bool WarningAcknowledged { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}

public enum PaymentMethod
{
    Cash,
    Card,
    POS
}

public enum ContractStatus
{
    Active,
    Completed,
    Canceled
}

public class ServicePrice
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Label { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class PrintSizePrice
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Size { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class SelectedPrintItem
{
    public string SizeId { get; set; } = string.Empty;
    public string SizeLabel { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
