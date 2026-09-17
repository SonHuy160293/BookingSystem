namespace BookingSystem.Identity.Application.Features.Tenants;

internal static class TenantRequestValidation
{
    public static List<string> ValidateDetails(
        string? name,
        string? legalName,
        string? taxCode,
        string? contactEmail,
        string? contactPhone,
        string? address)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Name is required.");
        }

        ValidateLength(name, 250, "Name", errors);
        ValidateLength(legalName, 250, "LegalName", errors);
        ValidateLength(taxCode, 50, "TaxCode", errors);
        ValidateLength(contactEmail, 256, "ContactEmail", errors);
        ValidateLength(contactPhone, 32, "ContactPhone", errors);
        ValidateLength(address, 500, "Address", errors);

        return errors;
    }

    private static void ValidateLength(string? value, int maximumLength, string propertyName, List<string> errors)
    {
        if (value?.Trim().Length > maximumLength)
        {
            errors.Add($"{propertyName} must not exceed {maximumLength} characters.");
        }
    }
}
