using OpenAI.Chat;

namespace EMSApp.Application;

public interface IChatBotService
{
    Task<string> GetChatResponseAsync(string prompt, CancellationToken ct = default);
}
