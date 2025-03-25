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
            if (arg.Data.CustomId == "r34menu:") // Tags from an image, contains 25 at a time. TODO: Expand Functionality
            {
                var tagslist = arg.Data.Values;
                string tags = "";
                foreach (var t in tagslist)
                {
                    tags += $"{t} ";
                }
                tags += " -ai_generated -incest -scat"; // Include the default filters.
                var image = await BooruService.GetImage(BooruService.Site.Rule34, tags); // Gets new random image from taglist
                var builder = await ComponentService.GetPostComponents(image, tags); // Gets Post Buttons
                await arg.RespondAsync(image.file_url, components: builder.Build()); //Responds to SelectMenu
            }
        }

        private async Task HandleButton(SocketMessageComponent arg) // Handles Button components from Interactions
        {
            var cid = arg.Data.CustomId; // Gets CustomID from the button
            if (cid.StartsWith("r34:")) // Get another image from Rule34
            {
                string tags = cid.Substring(4); //trims the leading prefix and extracts the searchable tags
                var t = tags.Split(' '); //array of individual tags
                var image = await BooruService.GetImage(BooruService.Site.Rule34, tags); //fetches the next image from the booru service
                if (image == null) //no more images left in the tag, or image fetching failed.
                {
                    await arg.RespondAsync($"No more images with those tags!", ephemeral: true);
                    return;
                }
                var builder = await ComponentService.GetPostComponents(image, tags); //fetches the button components from the component service
                if (arg.IsDMInteraction) //user is executing this command within a dm, so threads dont exist
                {
                    await arg.RespondAsync(image.file_url, components: builder.Build());
                    return;
                }
                if ((arg.Channel as IThreadChannel) == null) //we arent in a dm, and we arent in a thread
                {
                    if (arg.Message.Thread == null)
                    {
                        await arg.RespondAsync($"Creating thread...", ephemeral: true);
                        var newThread = await (arg.Channel as ITextChannel).CreateThreadAsync($"{tags.Split(' ')[0]}", ThreadType.PublicThread, ThreadArchiveDuration.OneHour, arg.Message);
                        await newThread.SendMessageAsync($"Tags: `{string.Join("`, `", t.Take(t.Count() - 3))}`");
                        await newThread.SendMessageAsync(image.file_url, components: builder.Build());
                    }
                    else
                    {
                        await arg.DeferAsync();
                        await arg.Message.Thread.SendMessageAsync(image.file_url, components: builder.Build());
                    }
                }
                else await arg.RespondAsync(image.file_url, components: builder.Build()); //we are in a thread
            }
            if (cid.StartsWith("tags:"))
            {
                var payload = cid.Substring(5).Split('|');
                string id = payload[0];
                int page = int.Parse(payload[1]);
                var image = await BooruService.GetPostById(id);
                var tags = image.tags.Split(' ');
                string output = "";
                foreach (var t in tags.Skip(page * 15).Take(15))
                {
                    output += $"[{t}](https://rule34.xxx/index.php?page=post&s=list&tags={t}) \t";
                }
                if (output == "")
                    return;
                await arg.RespondAsync($"{output}", ephemeral: true, components: ComponentService.GetTagComponents(id, page, tags.Length/15).Build());
            }
            if (cid.StartsWith("st:") || cid.StartsWith("nexttags:") || cid.StartsWith("prevtags:")) // Gets a page of 25 tags and bakes it into a SelectMenu. 
            {
                var payload = arg.Data.CustomId.Substring(9).Split('|'); // Payload is in the format of {id}|{pn}
                string id = payload[0];
                int page = int.Parse(payload[1]);
                var image = await BooruService.GetPostById(id); // Gets the R34Post object associated with the id of the image
                var tags = image.tags.Split(' '); // Gets the split up list of tags
                var builder = ComponentService.GetTaglistComponents(image, page); // Gets the associated taglist search components

                if (cid.StartsWith("nexttags:") || cid.StartsWith("prevtags:")) //If its from a taglist component, modify rather than remake
                {
                    await arg.DeferAsync();
                    await arg.UpdateAsync(p =>
                    {
                        p.Content = $"*Showing Tags {1 + (25 * page)}-{25 + (25 * page)} of {tags.Length}*";
                        p.Components = builder.Build();
                    });
                }
                else await arg.RespondAsync($"*Showing Tags {1 + (25 * page)}-{25 + (25 * page)} of {tags.Length}*", components: builder.Build(), ephemeral: true);
            }
            if (cid.StartsWith("save:"))
            {
                string id = cid.Substring(5);
                var image = await BooruService.GetPostById(id);
                var tags = image.tags.Split(' ');
                var builder = ComponentService.GetSaveComponents(image);
                await arg.DeferAsync();
                await arg.User.SendMessageAsync($"[Direct Link]({image.file_url})\n`{string.Join("`, `", tags)}`", components: builder.Build());
            }
            if (cid.StartsWith("delete:"))
            {
                await arg.Message.DeleteAsync();
                await arg.DeferAsync();
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
            .AddSingleton(typeof(TagService))
            .BuildServiceProvider();
    }
}
