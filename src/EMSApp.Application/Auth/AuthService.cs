using EMSApp.Application;
using EMSApp.Domain.Entities;
using EMSApp.Domain;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using EMSApp.Application.Auth;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;

namespace EMSApp.Infrastructure;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<User> _hasher;

    public AuthService(IUserRepository userRepository, IConfiguration configuration, IPasswordHasher<User> hasher)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _hasher = hasher;

    }

    public async Task<string?> LoginAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null) return null;

        bool ok;
        if (IsIdentityHash(user.PasswordHash))
        {
            ok = _hasher.VerifyHashedPassword(user, user.PasswordHash, password)
                     is PasswordVerificationResult.Success;
        }
        else
        {
            ok = user.PasswordHash == password;
            if (ok)
            {
                user.UpdatePassword(_hasher.HashPassword(user, password));
                await _userRepository.UpdateAsync(user, false);
            }
        }

        return ok ? GenerateJwtToken(user) : null;
    }

    public async Task<string> RegisterAsync(string username, string email, string password, string role = "employee")
    {
        if (await _userRepository.GetByUsernameAsync(username) is not null)
            throw new InvalidOperationException("Username already exists.");

        var user = new User(email, username, password, Enum.Parse<UserRole>(role, ignoreCase: true));
        await _userRepository.CreateAsync(user);
        return GenerateJwtToken(user);
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString().ToLowerInvariant()),
            new Claim("departmentId", user.DepartmentId ?? string.Empty)
        };

        var key = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                          double.Parse(jwtSettings["ExpirationMinutes"] ?? "15")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool IsIdentityHash(string h) => h.StartsWith("AQAAAA");
}
