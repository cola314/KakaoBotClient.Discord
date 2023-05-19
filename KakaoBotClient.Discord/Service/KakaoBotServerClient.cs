using Grpc.Core;
using Grpc.Net.Client.Web;
using Grpc.Net.Client;
using GrpcProto;

namespace KakaoBotClient.Discord.Service;

public record MessageReceivedEventArgs(string Room, string Content);

public record Message(bool IsGroupChat, string Content, string Room, string Sender);

public class KakaoBotServerClient
{
    public event EventHandler? OnConnected;
    public event EventHandler? OnDisconnected;
    public event EventHandler<MessageReceivedEventArgs>? OnReceiveMessage;

    private string _apiKey;
    private CancellationTokenSource _cts;
    private GrpcChannel _channel;
    private KakaoClient.KakaoClientClient _client;

    public Task ConnectAsync(string address, string apiKey)
    {
        _cts = new CancellationTokenSource();
        _apiKey = apiKey;

        var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
        _channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions() {HttpHandler = httpHandler});
        _client = new KakaoClient.KakaoClientClient(_channel);

        _ = Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("[ReadPushMessage]");
                    using var call = _client.ReadPushMessage(new ReadPushMessageRequest() {ApiKey = apiKey});

                    OnConnected?.Invoke(this, EventArgs.Empty);

                    while (await call.ResponseStream.MoveNext(_cts.Token))
                    {
                        var pushMessage = call.ResponseStream.Current;
                        Console.WriteLine(
                            $"[Push Message] Room: {pushMessage.Room}, Message: {pushMessage.Message}");

                        OnReceiveMessage?.Invoke(this,
                            new MessageReceivedEventArgs(pushMessage.Room, pushMessage.Message));
                    }
                }
                catch (RpcException ex)
                {
                    if (ex.StatusCode == StatusCode.Unauthenticated)
                    {
                        Console.WriteLine("[Unauthenticated]" + ex.Message);
                        break;
                    }

                    Console.WriteLine("[ReadPushMessage RpcException]" + ex.Message);

                    await Task.Delay(1000);
                }
            }
        });
        return Task.CompletedTask;
    }

    public async Task DisconnectAsync()
    {
        _cts.Cancel();
        await _channel.ShutdownAsync();

        OnDisconnected?.Invoke(this, EventArgs.Empty);
    }

    public void SendMessage(Message message)
    {
        if (_cts.IsCancellationRequested)
            return;

        var request = new SendReceivedMessageRequest()
        {
            ApiKey = _apiKey,
            IsGroupChat = message.IsGroupChat,
            Message = message.Content,
            Room = message.Room,
            Sender = message.Sender,
        };
        try
        {
            _ = _client?.SendReceivedMessageAsync(request, cancellationToken: _cts.Token);
        }
        catch (RpcException ex)
        {
            if (ex.StatusCode == StatusCode.Unauthenticated)
            {
                Console.WriteLine("[Unauthenticated]" + ex.Message);
            }

            Console.WriteLine("[SendReceivedMessageAsync RpcException]" + ex.Message);
        }
    }
}