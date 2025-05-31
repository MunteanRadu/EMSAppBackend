namespace EMSApp.Application.Auth;

public interface IAuthService
{
    Task<string?> LoginAsync(string email, string password);
    Task<string> RegisterAsync(string username, string email, string password,
                                string role = "employee");
}
