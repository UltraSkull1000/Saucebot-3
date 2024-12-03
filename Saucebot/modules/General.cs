using Discord.Interactions;

namespace Saucebot.Modules; // All modules should be contained within this folder, and thus should be under the .Modules namespace!

public class General() : InteractionModuleBase // Modules with SlashCommands should be 1) Public for dependency injection and 2) Inherit InteractionModuleBase
{
    [SlashCommand("uptime", "Checks the uptime of the current shard")] // Example command. Shows how long the bot has been running in a hh:mm:ss format.
    public async Task Uptime()
    {
        await RespondAsync($"The current shard has been up for {(DateTime.Now - Saucebot.startTime).ToString()}.", ephemeral: true);
    }
    
    [SlashCommand("ping", "Checks the ping to the current shard")] // Example command. Shows the difference between interaction creation time and handling time. 
    public async Task Ping()
    {
        await RespondAsync($"*Pong! {(DateTime.Now - Context.Interaction.CreatedAt).ToString("fff")}ms*", ephemeral: true);
    }

    [SlashCommand("r34", "Gets images from rule34.xxx!")]
    public async Task Rule34(string query = "");

}