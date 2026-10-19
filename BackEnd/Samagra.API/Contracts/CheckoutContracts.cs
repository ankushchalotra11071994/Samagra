// Samagra.API/Contracts/CheckoutContracts.cs
//
// DataAnnotations yahan zaroori hain — global ModelValidation filter inhi se
// 400 return karta hai.
//
// Dhyan: attribute seedha constructor parameter par lagta hai, [property: ...]
// par NAHI. Record ke saath [property:] compile ho jaata hai par runtime par
// "validation metadata must be associated with the constructor parameter"
// bolkar 500 de deta hai. BulkImportContracts bhi isi tarah likha hai.

using System.ComponentModel.DataAnnotations;

namespace Samagra.API.Contracts;

public record SaveAddressRequest(
    [Required, StringLength(50)]  string Label,
    [Required, StringLength(150)] string FullName,
    [Required, StringLength(20), Phone] string Phone,
    [Required, StringLength(200)] string Line1,
    [StringLength(200)]           string? Line2,
    [Required, StringLength(100)] string City,
    [Required, StringLength(100)] string State,
    [Required, StringLength(20)]  string PostalCode,
    [Required, StringLength(100)] string Country,
    bool IsDefault = false);

/// <summary>
/// The card number arrives here, is validated, and is then dropped: only the
/// brand and last four digits reach the database. There is deliberately no
/// CVV field — nothing in this app can use one.
/// </summary>
public record SaveCardRequest(
    [Required, StringLength(25, MinimumLength = 12)] string Number,
    [Required, StringLength(150)] string CardholderName,
    [Range(1, 12)]      int ExpiryMonth,
    [Range(2000, 2100)] int ExpiryYear,
    bool IsDefault = false);
