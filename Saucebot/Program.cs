/*
    Discord.Net Bot Template/Example
    Written by McElroy Ruman
    Abstracted from UltraSkull1000/Malaco-5
    ---------------------------------------
    Subject to the MIT License included in 
    license.txt
*/

using Discord;
using Discord.WebSocket;

namespace Saucebot; // All elements of the program should be under this namespace. Feel free to rename.

public class Saucebot() // I recommend changing the name of this class to match the namespace above. 
{
    public static DiscordSocketClient? _client; // Static reference to the default client. In this case, it is not sharded. 
    public static CommandHandler? commandHandler; // Static reference to the CommandHandler, an instance of the class contained in CommandHandler.cs
    public static StatusHandler? statusHandler; // Static reference to the StatusHandler, an instance of the class contained in StatusHandler.cs
    public static DateTime startTime; // Denotes when the program finished initialization. 

    public static string name = "Discord Bot"; // Self-referential name of the bot. Will not affect client-side username. 

    public static void Main(string[] args) // Static and Synchronous entry point.
    {
        Print($"Booting {name}...");
        while (!File.Exists("token.txt")) // If we don't have a saved token, accept that from the user.
        {
            Print("Token Not Found, Please Enter Token: ", ConsoleColor.White, ConsoleColor.Black, false);
            string? t = Console.ReadLine();
            File.WriteAllText("token.txt", t);
        }
        Print("Starting Asyncronous Operation..."); 
        MainAsync().GetAwaiter().GetResult(); // Starts asynchronous operation.
    }

    public static async Task MainAsync() // Start of Asynchronous operation
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig(){ // Init Static Client
            UseInteractionSnowflakeDate = false // Fixes a bug where some interactions won't respond because they believe they are outside the appropriate window.
        });

        _client.Log += LogClientMessage; // General Logging
        _client.JoinedGuild += LogGuildJoin;
        _client.LeftGuild += LogGuildLeave;

        commandHandler = new CommandHandler(_client); // Init CommandHandler.
        statusHandler = new StatusHandler(_client); // Init StatusHandler. Will quit unless status.txt is provided in the same folder as executable.

        await _client.LoginAsync(TokenType.Bot, File.ReadAllText("token.txt")); // Logs in with the supplied token.
        await _client.StartAsync(); // Begins client operations.
        
        while (_client.ConnectionState != ConnectionState.Connected)
        {
            Thread.Sleep(100); // Wait for the client to establish a connection to discord.
        }
 
        startTime = DateTime.Now; // We've officially started operations!
        await Task.Delay(-1); // Stops the program from exiting prematurely. To close the program, use a system interrupt.
    }

    private static Task LogClientMessage(LogMessage arg) // General information supplied by the DiscordSocketClient.
    {
        switch (arg.Severity)
        {
            default: return Task.CompletedTask;
            case LogSeverity.Info:
                Print(arg.Message);
                break;
            case LogSeverity.Warning:
                Print(arg.Message, ConsoleColor.Yellow);
                break;
            case LogSeverity.Error:
            case LogSeverity.Critical:
                Print($"{arg.Message}\n\t{arg.Exception}\n\n\t{arg.Source}", ConsoleColor.Red);
                break;
        }
        return Task.CompletedTask;
    }

    private static Task LogGuildJoin(SocketGuild arg) // Log when the bot is added to a guild. 
    {
        Print($"Joined Guild {arg.Name} <{arg.Id}> owned by {arg.Owner.Username} <@{arg.OwnerId}>", ConsoleColor.Blue);
        return Task.CompletedTask;
    }

    private static Task LogGuildLeave(SocketGuild arg) // Log when the bot is removed from a guild.
    {
        Print($"Left Guild {arg.Name} <{arg.Id}>", ConsoleColor.Red);
        return Task.CompletedTask;
    }

    public static void Print(string text, ConsoleColor fg = ConsoleColor.Gray, ConsoleColor bg = ConsoleColor.Black, bool showTimestamp = true) // Standardized Console Print Function. For Aesthetics!
    {
        Console.ForegroundColor = fg;
        Console.BackgroundColor = bg;
        Console.WriteLine(showTimestamp ? $"{DateTime.Now.ToShortTimeString()} > {text}" : text);
        Console.ResetColor();
    }
}