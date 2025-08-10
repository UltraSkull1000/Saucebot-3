using Discord.WebSocket;
using Timer = System.Timers.Timer;

namespace Saucebot;

public class StatusHandler{
    private static Random rand = new Random();
    private DiscordSocketClient? _client;
    private string[] statuses = [];
    public StatusHandler(DiscordSocketClient _client, List<string> statuses){
        this._client = _client;
        this.statuses = statuses.ToArray();
        _client.Ready += OnReady;
    }

    public async Task OnReady(){
        await Program.Print($"Client is Ready, Preparing Status Handler...");
        if(_client == null){
            throw new NullReferenceException(nameof(_client));
        }
        _client.Ready -= OnReady;
        await _client.SetCustomStatusAsync($"Saucebot Successfully Started! {DateTime.Now.ToShortTimeString()}");
        var timer = new Timer(600000);
        timer.Elapsed += async (s, e) => await _client.SetCustomStatusAsync(statuses[rand.Next(0,statuses.Length)]);
        timer.Start();
        await Program.Print("Finished Preparing Status Handler.");
    }
}