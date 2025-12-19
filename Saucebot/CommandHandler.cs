using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Saucebot.Modules;
using Saucebot.Services;
using System.Net.Sockets;
using System.Reflection;

namespace Saucebot
{
    public class CommandHandler
    {
        DiscordSocketClient _client; // Client for interacting with Discord. Passed from Program.cs
        InteractionService _interactionService; // Service for handling incoming interactions
        IServiceProvider _serviceProvider; // Service Provider for Dependency Injection
        private IServiceProvider SetupServices()
            => new ServiceCollection()
            .AddSingleton(this)
            .AddSingleton(_client)
            .AddSingleton(_interactionService)
            .AddSingleton(typeof(General))
            .BuildServiceProvider();

        public CommandHandler(DiscordSocketClient _client)
        {
            this._client = _client; // Passing client from Program.cs

            _interactionService = new InteractionService(_client); // Initialize Interaction Service
            _serviceProvider = SetupServices(); // Get Services needed by the InteractionService.

            _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider).GetAwaiter().GetResult();
            _interactionService.RegisterCommandsGloballyAsync(true).GetAwaiter().GetResult(); // Registers commands to Discord, May take some time to update for clients

            // Assigning Command Listeners
            _client.InteractionCreated += HandleInteraction;
            _client.ButtonExecuted += HandleButton;
            _client.SelectMenuExecuted += HandleSelectMenu;
        }
        private async Task HandleInteraction(SocketInteraction arg)
        {
            var ctx = new SocketInteractionContext(_client, arg);
            await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
        }

        private async Task HandleButton(SocketMessageComponent button) // Handles Button components from Interactions
        {
            await Task.Run(async () =>
            {
                bool isDM = button.IsDMInteraction;
                switch (button.Data.CustomId)
                {
                    default:
                        await Program.Print($"Client recieved an unknown button interaction. CID: {button.Data.CustomId}");
                        break;
                    case var customID when customID.StartsWith("delete:"):
                        await button.DeferAsync(); // Acknowledge interaction
                        var thread = button.Message.Thread;
                        if (thread != null)
                        {
                            await thread.DeleteAsync();
                        }
                        await button.Message.DeleteAsync(); // Delete source message
                        break;
                    case var customID when customID.StartsWith("details:"):
                        await button.DeferAsync(); // Acknowledge interaction
                        var payload = customID.Substring(8).Split('|'); // Payload is split into two components, the ids and the page.
                        int page = int.Parse(payload[1]); // page is a single-digit int less than five.
                        string[] ids = payload[0].Split('-'); // ids contains up to five elements.

                        EmbedBuilder embed = ComponentService.GetDetailsEmbed(ids, page, out var components, out var url, button.IsDMInteraction);
                        var messageContent = $"[Details]({url})";

                        await button.Message.ModifyAsync(x =>
                        {
                            x.Content = messageContent;
                            x.Embed = embed.Build();
                            x.Components = components.Build();
                        });

                        break;
                    case var customID when customID.StartsWith("r34:") || customID.StartsWith("may:"): // Get another image from rule34.
                        int count = 1; // Return 1 image
                        if (customID.StartsWith("may:"))
                            count = 5; // Return 5 images, because we are requesting many.

                        string tags = customID.Substring(4); //trims the leading prefix and extracts the searchable tags
                        var tagArray = tags.Split(' ').Where(x => x != ""); //array of individual tags

                        List<R34Post> images = new List<R34Post>(); // List of images to process
                        for (int i = 0; i < count; i++) // Grab Images
                        {
                            await button.DeferLoadingAsync(); // Acknowledge interaction
                            var image = await R34Service.GetImage(tags); //fetches the next image from the booru service
                            if (image == null && i == 0) //no more images left in the tag, or image fetching failed.
                            {
                                await button.FollowupAsync($"No more images with those tags!", ephemeral: true);
                                return;
                            }
                            else if (image == null)
                                break;
                            images.Add(image);
                        }

                        var builder = await ComponentService.GetPostComponents(images, tags, isDM); //fetches the button components from the component service
                        string content = images.Count() > 1 ? string.Join(" ", images.Select(x => $"[{images.IndexOf(x) + 1}]({x.file_url})")) : $"[Link]({images.First().file_url})"; // Generates Message Content inline.

                        if ((button.Channel as IThreadChannel) == null && !button.IsDMInteraction) // We are in a primary text channel.
                        {
                            var channel = button.Channel as ITextChannel;
                            if (channel == null) // The bot can no longer access the channel, or is improperly scoped.
                                return;
                            if (button.Message.Thread == null) // The message does not have a pre-existing thread, create one. 
                            {
                                var tagsFormatted = tagArray.Count() <= 1 ? $"Rule34.xxx" : (tagArray.Count() == 2 ? tagArray.First() : string.Join(", ", tagArray.Take(tagArray.Count() - 1)));
                                await button.FollowupAsync($"Creating thread `{tagsFormatted}`...", ephemeral: true);
                                var newThread = await channel.CreateThreadAsync(tagsFormatted == null ? "Rule34.xxx" : tagsFormatted, ThreadType.PublicThread, ThreadArchiveDuration.OneHour, button.Message);
                                await newThread.SendMessageAsync(content, components: builder.Build());
                            }
                            else // The message does have a pre-existing thread, so send the image there.
                            {
                                try
                                {
                                    await button.Message.Thread.SendMessageAsync(content, components: builder.Build());
                                }
                                catch // We were not permitted to send a message in that thread.
                                {
                                    await button.Channel.SendMessageAsync($"Cannot send messages in that thread!", flags: MessageFlags.Ephemeral);
                                    await button.FollowupAsync(content, components: builder.Build());
                                }
                            }
                        }
                        else await button.FollowupAsync(content, components: builder.Build()); // We are in a thread or dm
                        break;
                    case var customID when customID.StartsWith("save:"): // We are sending the image to the user's dms.
                        await button.DeferAsync(); // acknowledge interaction
                        var id = customID.Substring(5); // grab image id
                        var message = button.Message.Content; // usually a link to the image in a markdown format.
                        var dmBuilder = await ComponentService.GetDMComponents(id);
                        try
                        {
                            await button.User.SendMessageAsync(message, components: dmBuilder.Build());
                        }
                        catch // User has their dms turned off.
                        {
                            await button.FollowupAsync("Can't send you a message!", ephemeral: true);
                        }
                        break;
                    case var customID when customID.StartsWith("tags:"): // Fetch the tags for the associated image.
                        await HandleTags(button, customID);
                        break;
                    case var customID when customID.StartsWith("edit:"):
                        await button.DeferAsync(); // acknowledge interaction
                        var img = customID.Substring(5); // grab image id
                        var post = await R34Service.GetPostById(img);
                        var dmB = await ComponentService.GetDMComponents(img);
                        await button.Message.ModifyAsync(x =>
                        {
                           x.Content = $"[Image]({post.file_url})";
                           x.Components = dmB.Build();
                           x.Embeds = null;
                        });
                        break;
                }
            });
        }

        private async Task HandleSelectMenu(SocketMessageComponent selectMenu) // Handles Select menu components from Interactions
        {
            switch (selectMenu.Data.CustomId)
            {
                default:
                    await Program.Print($"Client recieved an unknown select menu interaction. CID: {selectMenu.Data.CustomId}");
                    break;
                case var customID when customID.StartsWith("manytags:"): // This select menu selects between one of 5 images, and produces a list of their tags.
                    customID = selectMenu.Data.Values.First(); // Switches from the custom id of the select menu to that of the equivalent option.
                    await HandleTags(selectMenu, customID); // Handles Tags.
                    break;
            }
        }

        private async Task HandleTags(SocketInteraction interaction, string data)
        {
            var payload = data.Substring(5).Split('|');
            string id = payload[0];
            int page = int.Parse(payload[1]);

            var image = await R34Service.GetPostById(id);
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
            await interaction.RespondAsync($"{output}", ephemeral: true, components: ComponentService.GetTagComponents(id, page, tags.Length / 15).Build());
        }
    }
}
