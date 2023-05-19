using KakaoBotClient.Discord.Service;

var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")!;
var serverAddress = Environment.GetEnvironmentVariable("API_SERVER")!;
var apiKey = Environment.GetEnvironmentVariable("API_KEY")!;

var kakaoServerClient = new KakaoBotServerClient();
kakaoServerClient.ConnectAsync(serverAddress, apiKey);

new DiscordClient(token, kakaoServerClient).Start();
