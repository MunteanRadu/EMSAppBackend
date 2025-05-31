using EMSApp.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace EMSApp.Domain.Entities;

public class UserProfile
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public int Age { get; private set; }
    public string Phone { get; private set; }
    public string Address { get; private set; }
    public string EmergencyContact { get; private set; }

    public UserProfile() { }

    public UserProfile(string name, int age, string phone, string address, string emergencyContact) 
    {
        ValidateUserProfile(name, age, phone, address, emergencyContact);

        Id = Guid.NewGuid().ToString();
        Name = name;
        Age = age;
        Phone = phone;
        Address = address;
        EmergencyContact = emergencyContact;
    }

    public void Update(string newName, int newAge, string newPhone, string newAddress, string newEmergencyContact)
    {
        ValidateUserProfile(newName, newAge, newPhone, newAddress, newEmergencyContact);

        Name = newName;
        Age = newAge;
        Phone = newPhone;
        Address = newAddress;
        EmergencyContact = newEmergencyContact;
    }

    public bool IsEmpty()
        => string.IsNullOrWhiteSpace(Name)
        && string.IsNullOrWhiteSpace(Phone)
        && string.IsNullOrWhiteSpace(Address)
        && string.IsNullOrWhiteSpace(EmergencyContact);

    private void ValidateUserProfile(string name, int age, string phone, string address, string emergencyContact)
    {
        // Name validation
        if (string.IsNullOrWhiteSpace(name) || name.Length < 3 || name.Length > 50)
            throw new DomainException("Name length must be between 3-50 characters");
        if (!Regex.IsMatch(name, @"^[a-zA-Z\s]+$"))
            throw new DomainException("Name must only contain letters");

        // Age validation
        if (age > 65 || age < 18)
            throw new DomainException("Age must be between 18 and 65");

        // Phone validation
        if (string.IsNullOrWhiteSpace(phone) || !Regex.IsMatch(phone, @"^(07\d{8}|\+407\d{8}|00407\d{8})$"))
            throw new DomainException("Phone number must be in valid format");

        // Address validation
        if (string.IsNullOrWhiteSpace(address) || address.Length < 5)
            throw new DomainException("Address too short");

        // Phone validation
        if (string.IsNullOrWhiteSpace(emergencyContact) || !Regex.IsMatch(emergencyContact, @"^(07\d{8}|\+407\d{8}|00407\d{8})$"))
            throw new DomainException("Emergency contact phone number must be in valid format");
    }
}
