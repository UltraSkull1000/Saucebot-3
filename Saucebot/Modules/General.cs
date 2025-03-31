using Discord;
using Discord.Interactions;
using Saucebot.Services;


namespace Saucebot.Modules
{
    public class General : InteractionModuleBase<SocketInteractionContext>
    {
        public class TagAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {   
                string[] data = ((string)autocompleteInteraction.Data.Current.Value).Split(' ');
                string existing = string.Join(' ', data.Take(data.Count() - 1));
                string search = data.Last(); // Retrieves user's entered content from data.
                string?[]? tags = await TagService.GetTags(search); // Fetches autocomplete results for the user's input
                if(tags == null) // We tried to connect to rule34, but were unable to get any results.
                    return AutocompletionResult.FromError(new NullReferenceException("Cannot Reach Rule34.xxx for Autocomplete."));
                List<AutocompleteResult> results = new List<AutocompleteResult>();
                foreach (string? tag in tags){
                    if(tag != null)
                        results.Add(new AutocompleteResult($"{existing} {tag}", $"{existing} {tag}"));
                }
                return AutocompletionResult.FromSuccess(results.Take(25));
            }
        }

        [NsfwCommand(true)]
        [SlashCommand("r34", "Gets an Image from Rule34", runMode: RunMode.Async)]
        public async Task Rule34([Summary("tags"), Autocomplete(typeof(TagAutocompleteHandler))] string tags = "")
        {
            tags += " -ai_generated";
            var image = await BooruService.GetImage(tags);
            if(image == null){
                await RespondAsync("No images with those tags!", ephemeral:true);
                return;
            }
            var builder = await ComponentService.GetPostComponents(image, tags);
            await RespondAsync($"[Link]({image.file_url})", components:builder.Build());
        }

        [SlashCommand("clear", "Clears messages from Saucebot from the channel", runMode:RunMode.Async)]
        public async Task Clear(int amount = 50, bool all = false)
        {
            var messages = await Context.Channel.GetMessagesAsync().FlattenAsync();
            var toDelete = messages.Where(x => x.Author.Id == Context.Client.CurrentUser.Id).Take(amount);
            if(!all)
                await RespondAsync($"Deleting last chain!", ephemeral:true);
            else
                await RespondAsync($"Deleting last {toDelete.Count()} messages!", ephemeral: true);
            foreach (IUserMessage m in toDelete)
            {
                Thread.Sleep(500);
                bool br = false;
                var muid = m.InteractionMetadata;
                if (muid != null)
                    br = true;
                await Context.Channel.DeleteMessageAsync(m);
                if (br && !all)
                    break;
            }
        }
    }
}
