﻿using Discord;
using Discord.WebSocket;
using ConnectionState = Discord.ConnectionState;

//Created by McElroy Ruman https://github.com/UltraSkull1000
//Link to Repository: https://github.com/UltraSkull1000/Saucebot-3

namespace Saucebot
{
    public class Program
    {
        public static DiscordSocketClient? _client; // Client for interacting with Discord
        public static CommandHandler? _commandHandler; // Class for accepting events from the Client and executing commands and interactions.
        private static string currentLog = "";

        public static void Main() => MainAsync().GetAwaiter().GetResult();
        public static async Task MainAsync()
        {
            // Client Logging Init
            if (!Directory.Exists("logs")) // Create logs folder, if it does not exist.
                Directory.CreateDirectory("logs");
            currentLog = $"client_{DateTime.Now.ToFileTimeUtc()}.txt"; // Name and Create current log file. Opens and closes in order to preserve integrity
            File.Create($"logs/{currentLog}").Close();

            // Client Token Init
            if (!File.Exists("token.txt")) // Checking if the token has been saved for the bot yet
            {
                Console.Write("Please Enter your Bot's Token: ");
                string? t = Console.ReadLine(); // Accepts the token
                File.WriteAllText("token.txt", t); // Creates and writes the token to the token.txt 
                Console.WriteLine();
            }

            // Client Init
            _client = new DiscordSocketClient(); // Initializes the client for this instance. 

            // Assigning listeners for primary events. 
            _client.Log += LogClientMessage;
            _client.JoinedGuild += LogGuildJoin;
            _client.LeftGuild += LogGuildLeave;

            await _client.LoginAsync(TokenType.Bot, File.ReadAllText("token.txt")); // Set the token
            await _client.StartAsync(); // Begin establishing connection

            while (_client.ConnectionState != ConnectionState.Connected) // Wait until the connection is established
            {
                Thread.Sleep(500);
            }

            // Client Connected.
            await Print($"Bot is Online! Use the following link to invite it to a test server: https://discord.com/oauth2/authorize?client_id={_client.CurrentUser.Id}&scope=bot&permissions=8", ConsoleColor.Green, false);
            await Print($"Do Not Use the link above for public invites! Find out exactly what permissions the bot needs in order for it to function, and generate a link here: https://discordapi.com/permissions.html#0", ConsoleColor.Red, false);

            _commandHandler = new CommandHandler(_client); // Initialize Command Handler

            await Task.Delay(-1); // Keep Running Indefinitely.
        }

        private static Task LogGuildLeave(SocketGuild arg) => Print($"Left Guild {arg.Name}", ConsoleColor.Red);
        private static Task LogGuildJoin(SocketGuild arg) => Print($"Joined Guild {arg.Name}", ConsoleColor.Blue);
        private static async Task LogClientMessage(LogMessage arg) // Logs general client messages. 
        {
            switch (arg.Severity)
            {
                case LogSeverity.Verbose:
                    await Print($"{arg.Message}", ConsoleColor.Gray);
                    break;
                case LogSeverity.Info:
                    await Print(arg.Message);
                    break;
                case LogSeverity.Warning:
                    await Print(arg.Message, ConsoleColor.Yellow);
                    break;
                case LogSeverity.Error:
                case LogSeverity.Critical:
                    await Print($"{arg.Message}\n\t{arg.Exception}\n\n\t{arg.Source}", ConsoleColor.Red);
                    break;
            }
        }

        public static async Task Print(string message, ConsoleColor color = ConsoleColor.White, bool includeTimestamp = true) // Extension and formatting for console printing. 
        {
            await Task.Run(() =>
            {
                if (includeTimestamp)
                    Console.Write($"{DateTime.Now.ToLocalTime()} >> ");
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
                using (var Writer = File.AppendText($"logs/{currentLog}"))
                {
                    Writer.WriteLine($"{DateTime.Now.ToLocalTime()} >> {message}");
                }
            });
        }
    }
}