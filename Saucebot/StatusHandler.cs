using Discord.WebSocket;
using Timer = System.Timers.Timer;

namespace Saucebot;

public class StatusHandler{ // Handles the statuses attached to the current user. 
    private DiscordSocketClient _client; // Refers to the static client declared in Program.cs
    private string[] statuses = []; 
    public StatusHandler(DiscordSocketClient _client){
        this._client = _client;
        if(!File.Exists("status.txt")) // If we don't have a status.txt, then we dont have any statuses to show!
            return;
        statuses = File.ReadAllLines("status.txt"); // Dump status.txt to statuses[]
        _client.Ready += OnReady; // Wait for client to be ready.
    }

    public async Task OnReady(){
        Saucebot.Print($"Client is Ready, Preparing Status Handler...");
        _client.Ready -= OnReady; // Rmove listener, as it is no longer needed.
        await _client.SetCustomStatusAsync($"{Saucebot.name} Successfully Started! {DateTime.Now.ToShortTimeString()}");
        var timer = new Timer(600000); // 600000ms = 10 minutes
        timer.Elapsed += async (s, e) => await _client.SetCustomStatusAsync(statuses[new Random().Next(0, statuses.Length)]); // Set status to a random status in statuses[]
        timer.Start();
        Saucebot.Print("Finished Preparing Status Handler.");
    }
}