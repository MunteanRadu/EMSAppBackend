namespace EMSApp.Api;

public sealed record UpdateUserProfileRequest(
    string? Name,
    int? Age,
    string? Phone,
    string? Address,
    string? EmergencyContact
);
    
