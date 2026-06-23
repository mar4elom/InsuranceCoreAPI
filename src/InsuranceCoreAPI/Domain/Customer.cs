namespace InsuranceCoreAPI.Domain;

/// <summary>
/// Represents an insurance customer.
/// </summary>
public sealed class Customer
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Full legal name of the customer.
    /// </summary>
    public required string FullName { get; set; }
}
