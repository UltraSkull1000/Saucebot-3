using Discord; // InteractionType
using Discord.Interactions; // InteractionService
using Discord.WebSocket; // DiscordSocketClient
using Saucebot.Modules; // Classes under the folder ./modules

using Microsoft.Extensions.DependencyInjection; // ServiceProvider

using System.Reflection; // Assembly

namespace Saucebot; 
public class CommandHandler // Handles interactions, mostly commands. 
{
    private DiscordSocketClient _client; // Refers to the static client held in Program.cs
    private InteractionService _interactionService; // Provides the framework for building and registering Application Commands
    private IServiceProvider _serviceProvider; // Provides custom support for accessing our modules and services.
    
    public CommandHandler(DiscordSocketClient _client) // Should only be created once.
    {
        this._client = _client; 
        _interactionService = new InteractionService(_client, new InteractionServiceConfig(){ // Init Interaction Service
            DefaultRunMode = Discord.Interactions.RunMode.Async, // By default, commands should run Asynchronously, to serve as many clients as possible.
            UseCompiledLambda = true // Compiles registered commands to Lambda, speeding up many processes.
        });
        _serviceProvider = SetupServices(); // Registers Modules and Services for later use.
        _client.Ready += OnReady; // Wait for the client to be ready for more.

        Saucebot.Print("Initialized Command Handler!"); // Finished Initialization
    }
    public async Task OnReady(){ // Client is ready to serve users.
        Saucebot.Print($"Client is Ready, Preparing Services...");
        _client.Ready -= OnReady; // Remove event listener, as it is no longer needed.
        await _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider); // Connects the interactionService with the Modules and Services specified in the ServiceProvider.
        await _interactionService.RegisterCommandsGloballyAsync(); // Registers all new commands to Discord. Also, removes missing commands.
        _client.InteractionCreated += HandleInteraction; // Create a new listener to handle passed interactions.
        Saucebot.Print("Finished Preparing Services.");
    }
    private async Task HandleInteraction(SocketInteraction interaction){
        var ctx = new SocketInteractionContext(_client, interaction); // Create the context needed to populate information about the request.
        var result = await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider); // Execute the command.
        if(!result.IsSuccess) // Log errors
        {
            Saucebot.Print(result.ErrorReason, ConsoleColor.Red);
        }
        else if(interaction.Type == InteractionType.ApplicationCommand) // Log that a command was executed
            Saucebot.Print($"{interaction.User.Username} >> Executed Application Command", ConsoleColor.Blue);
    }
    private IServiceProvider SetupServices() // Be sure to add any modules or services you create as a singleton to this.
    => new ServiceCollection()
    .AddSingleton(_interactionService)
    .AddSingleton(typeof(General))
    .BuildServiceProvider();
}