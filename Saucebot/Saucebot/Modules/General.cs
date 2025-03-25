using Discord;
using Discord.Interactions;
using Saucebot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saucebot.Modules
{
    public class General : InteractionModuleBase<SocketInteractionContext>
    {
        public class TagAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                var data = ((string)autocompleteInteraction.Data.Current.Value).Split(' ');
                string search = data.Last();
                string[] tags = await TagService.GetTags(search);
                List<AutocompleteResult> results = new List<AutocompleteResult>();
                foreach (string tag in tags){
                    results.Add(new AutocompleteResult(tag,tag));
                }
                return AutocompletionResult.FromSuccess(results);
            }
        }

        [SlashCommand("r34", "Gets an Image from Rule34", runMode: RunMode.Async)]
        public async Task Rule34([Summary("tags"), Autocomplete(typeof(TagAutocompleteHandler))] string tags = "", bool useDefaultFilters = true)
        {
            if(useDefaultFilters)
                tags += " rating:explicit -ai_generated -incest -scat";
            var image = await BooruService.GetImage(BooruService.Site.Rule34, tags);
            var builder = await ComponentService.GetPostComponents(image, tags);
            await RespondAsync(image.file_url, components:builder.Build());
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
            foreach (var m in toDelete)
            {
                Thread.Sleep(500);
                bool br = false;
                var muid = m.Interaction;
                if (muid != null)
                    br = true;
                await Context.Channel.DeleteMessageAsync(m);
                if (br && !all)
                    break;
            }
        }
    }
}
