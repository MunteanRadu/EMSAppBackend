using EMSApp.Application;
using System;
using OpenAI;
using OpenAI.Chat;

namespace EMSApp.Infrastructure;

public class ChatBotService : IChatBotService
{
    private readonly ChatClient _client;

    public ChatBotService(ChatClient client)
    {
        _client = client;
    }

    public async Task<string> GetChatResponseAsync(string prompt, CancellationToken ct = default)
    {
        ChatCompletion completion = await _client.CompleteChatAsync(prompt);

        return completion.Content[0].Text;
    }
}
