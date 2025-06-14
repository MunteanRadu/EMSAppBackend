using System.ComponentModel.DataAnnotations;

namespace EMSApp.Api;

public sealed record RegisterRequest
(
    [Required]
    string Username,
    [Required, EmailAddress] 
    string Email,
    [Required, MinLength(6)] 
    string Password
);
