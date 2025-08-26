namespace EMSApp.Api;

public sealed record CreateUserProfileRequest(
    string Name,
    int Age,
    string Phone,
    string Address,
    string EmergencyContact
);
