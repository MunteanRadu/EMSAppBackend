namespace EMSApp.Application;

public sealed record UserProfileDto
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string EmergencyContact { get; set; }

    public UserProfileDto() { }
}
