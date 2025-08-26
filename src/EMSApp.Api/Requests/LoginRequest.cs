using System.ComponentModel.DataAnnotations;

namespace EMSApp.Api;

public sealed record LoginRequest(
    [Required] string Username,
    [Required] string Password
);
