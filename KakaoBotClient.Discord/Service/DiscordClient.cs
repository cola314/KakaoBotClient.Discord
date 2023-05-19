using Discord;
using Discord.WebSocket;
using KakaoBotClient.Discord.Service;

public class DiscordClient
{
    private readonly KakaoBotServerClient _botServerClient;
    private readonly DiscordSocketClient _client;
    private readonly string _token;

    public DiscordClient(string token, KakaoBotServerClient botServerClient)
    {
        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        };
        _client = new DiscordSocketClient(config);
        _token = token;
        _botServerClient = botServerClient;
    }

    public async Task Start()
    {
        _botServerClient.OnReceiveMessage += BotServerClientOnOnReceiveMessage; 
        _client.Log += OnLog;
        _client.MessageReceived += OnMessageReceived;

        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
    }

    private async void BotServerClientOnOnReceiveMessage(object? sender, MessageReceivedEventArgs e)
    {
        try
        {
            var channel = await _client.GetChannelAsync(ulong.Parse(e.Room)) as ISocketMessageChannel;
            channel?.SendMessageAsync(e.Content);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[BotServerClientOnOnReceiveMessage]" + ex.Message);
        }
    }

    private async Task OnMessageReceived(SocketMessage message)
    {
        if (message.Author.IsBot) return;

        Console.WriteLine("[OnMessageReceived]" + message.Content);

        _botServerClient.SendMessage(new Message(
            IsGroupChat: true,
            Content: message.Content,
            Room: message.Channel.Id.ToString(),
            Sender: message.Author.Username));
    }

    private Task OnLog(LogMessage message)
    {
        Console.WriteLine(message.ToString());
        return Task.CompletedTask;
    }
}