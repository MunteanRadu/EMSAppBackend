namespace EMSApp.Api;

public record CreateUserProfileRequest(
    string Name,
    int Age,
    string Phone,
    string Address,
    string EmergencyContact
);
