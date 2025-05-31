namespace EMSApp.Api;

public class UpdateUserProfileRequest
{
    public string? Name { get; init; }
    public int? Age { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? EmergencyContact { get; init; }
}
