using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Saucebot.Modules;
using Saucebot.Services;
using System.Reflection;

namespace Saucebot
{
    public class CommandHandler
    {
        DiscordSocketClient _client; // Client for interacting with Discord. Passed from Program.cs
        InteractionService _interactionService; // Service for handling incoming interactions
        IServiceProvider _serviceProvider; // Service Provider for Dependency Injection

        public CommandHandler(DiscordSocketClient _client)
        {
            this._client = _client; // Passing client from Program.cs

            _interactionService = new InteractionService(_client); // Initialize Interaction Service
            _serviceProvider = SetupServices(); // Get Services needed by the InteractionService.

            _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider).GetAwaiter().GetResult();
            _interactionService.RegisterCommandsGloballyAsync(true); // Registers commands to Discord, May take some time to update for clients.

            // Assigning Command Listeners
            _client.InteractionCreated += HandleInteraction;
            _client.ButtonExecuted += HandleButton;
            _client.SelectMenuExecuted += HandleSelectMenu;
        }

        private async Task HandleSelectMenu(SocketMessageComponent arg) // Handles Select menu components from Interactions
        {
            var cid = arg.Data.CustomId;
            Program.Print($"{arg.User.Username} -> {cid}", ConsoleColor.DarkGray);
            if (cid.StartsWith("manytags:"))
            {
                cid = arg.Data.Values.First();
                var payload = cid.Substring(9).Split('|');
                string id = payload[0];
                int page = int.Parse(payload[1]);
                var image = await BooruService.GetPostById(id);
                if (image.tags == null)
                {
                    throw new NullReferenceException("Image Tags are Blank.");
                }
                var tags = image.tags.Split(' ');
                string output = "";
                foreach (var t in tags.Skip(page * 15).Take(15))
                {
                    output += $"[{t}](https://rule34.xxx/index.php?page=post&s=list&tags={t}) \t";
                }
                if (output == "")
                    return;
                await arg.RespondAsync($"{output}", ephemeral: true, components: ComponentService.GetTagComponents(id, page, tags.Length / 15).Build());
            }
        }

        private async Task HandleButton(SocketMessageComponent arg) // Handles Button components from Interactions
        {
            var cid = arg.Data.CustomId; // Gets CustomID from the button
            if (cid.StartsWith("r34:") || cid.StartsWith("may:")) // Get another image/images from Rule34
            {
                int count = 1;
                if (cid.StartsWith("may:"))
                {
                    count = 5;
                }
                string tags = cid.Substring(4); //trims the leading prefix and extracts the searchable tags
                var t = tags.Split(' '); //array of individual tags
                List<R34Post> images = new List<R34Post>();
                for (int i = 0; i < count; i++)
                {
                    var image = await BooruService.GetImage(tags); //fetches the next image from the booru service
                    if (image == null && i == 0) //no more images left in the tag, or image fetching failed.
                    {
                        await arg.RespondAsync($"No more images with those tags!", ephemeral: true);
                        return;
                    }
                    else if (image == null)
                    {
                        break;
                    }
                    images.Add(image);
                }
                var builder = await ComponentService.GetPostComponents(images, tags); //fetches the button components from the component service
                string content = images.Count() > 1 ? string.Join(" ", images.Select(x => $"[{images.IndexOf(x) + 1}]({x.file_url})")) : $"[Link]({images.First().file_url})"; // Generates Message Content inline. 
                if (arg.IsDMInteraction) //user is executing this command within a dm, so threads dont exist
                {
                    await arg.RespondAsync(content, components: builder.Build());
                    return;
                }
                if ((arg.Channel as IThreadChannel) == null) //we arent in a dm, and we arent in a thread
                {
                    var channel = arg.Channel as ITextChannel;
                    if (channel == null)
                        return;
                    if (arg.Message.Thread == null)
                    {
                        await arg.RespondAsync($"Creating thread...", ephemeral: true);
                        var newThread = await channel.CreateThreadAsync($"{tags.Split(' ')[0]}", ThreadType.PublicThread, ThreadArchiveDuration.OneHour, arg.Message);
                        await newThread.SendMessageAsync($"Tags: `{string.Join("`, `", t.Take(t.Count() - 3))}`");
                        await newThread.SendMessageAsync(content, components: builder.Build());
                    }
                    else
                    {
                        await arg.DeferAsync();
                        await arg.Message.Thread.SendMessageAsync(content, components: builder.Build());
                    }
                }
                else await arg.RespondAsync(content, components: builder.Build()); //we are in a thread
            }
            if (cid.StartsWith("tags:"))
            {
                var payload = cid.Substring(5).Split('|');
                string id = payload[0];
                int page = int.Parse(payload[1]);
                var image = await BooruService.GetPostById(id);
                if (image.tags == null)
                {
                    throw new NullReferenceException("Image Tags are Blank.");
                }
                var tags = image.tags.Split(' ');
                string output = "";
                foreach (var t in tags.Skip(page * 15).Take(15))
                {
                    output += $"[{t}](https://rule34.xxx/index.php?page=post&s=list&tags={t}) \t";
                }
                if (output == "")
                    return;
                await arg.RespondAsync($"{output}", ephemeral: true, components: ComponentService.GetTagComponents(id, page, tags.Length / 15).Build());
            }
            if (cid.StartsWith("delete:"))
            {
                await arg.Message.DeleteAsync();
                await arg.DeferAsync();
            }
            if (cid.StartsWith("save:"))
            {
                try
                {
                    var message = arg.Message.Content;
                    if(cid.Substring(5) != "")
                        message = cid.Substring(5);
                    var builder = await ComponentService.GetDMComponents();
                    await arg.User.SendMessageAsync(message, components: builder.Build());
                }
                catch (Exception e)
                {
                    await arg.RespondAsync("Can't send you a message!");
                    Program.Print(e.Message, ConsoleColor.DarkGray);
                }
            }
            if(cid.StartsWith("details:")){
                var payload = cid.Substring(8).Split('|');
                int page = int.Parse(payload.Last());
                string[] ids = payload.First().Split('-');
                EmbedBuilder embed = ComponentService.GetDetailsEmbed(ids, page, out var components, out var url);
                if(arg.Message.Content.StartsWith("details:")) {
                    await arg.DeferAsync();
                    await arg.Message.ModifyAsync(x => {
                        x.Content = $"details:{ids[page]}\n{$"[Link]({url})"}";
                        x.Embed = embed.Build();
                        x.Components = components.Build();
                    });
                }
                else
                    await arg.RespondAsync($"details:{ids[page]}\n{$"[Link]({url})"}", embed: embed.Build(), components: components.Build());
            }
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            var ctx = new SocketInteractionContext(_client, arg);
            await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
        }

        private IServiceProvider SetupServices()
            => new ServiceCollection()
            .AddSingleton(this)
            .AddSingleton(_client)
            .AddSingleton(_interactionService)
            .AddSingleton(typeof(General))
            .BuildServiceProvider();
    }
}
