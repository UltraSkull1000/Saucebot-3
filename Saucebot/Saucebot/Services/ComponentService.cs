using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Saucebot.Services
{
    public class ComponentService
    {
        static Random r = new Random();

        static IEmote last = new Emoji("💖");
        static readonly IEmote[] emotes = { new Emoji("💖"), new Emoji("💓"), new Emoji("🍆"), new Emoji("😍"), new Emoji("🥵"), new Emoji("🔥"), new Emoji("😩") };
        static IEmote PickNext()
        {
            last = emotes.Where(x => !x.Equals(last)).ToArray()[r.Next(emotes.Length - 1)];
            return last;
        }

        public static async Task<ComponentBuilder> GetPostComponents(R34Post image, string tags)
        {
            var paid = image.parent_id.ToString();

            if (image.id.ToString().Length != image.parent_id.ToString().Length)
            {
                paid = $"{image.id.ToString()[0]}{image.parent_id}";
            }

            var row1 = new ActionRowBuilder()
                .WithButton("Another!~", $"r34:{tags}", ButtonStyle.Success, PickNext())
                .WithButton("Rule34", url: $"https://rule34.xxx/index.php?page=post&s=view&id={image.id}", style: ButtonStyle.Link);
            var row2 = new ActionRowBuilder()
                .WithButton("Tags...", $"tags:{image.id}|0")
                .WithButton("Hide", "delete:", ButtonStyle.Danger);
            var builder = new ComponentBuilder()
                .AddRow(row1)
                .AddRow(row2);
            return builder;
        }

        public static ComponentBuilder GetSaveComponents(R34Post image)
        {
            var builder = new ComponentBuilder();
            if(image.source.Length != 0)
                builder.WithButton("Source", url: image.source, style: ButtonStyle.Link);
            builder.WithButton("Rule34", url: $"https://rule34.xxx/index.php?page=post&s=view&id={image.id}", style: ButtonStyle.Link);
            builder.WithButton("Delete", "delete:", ButtonStyle.Danger);
            return builder;
        }

        public static ComponentBuilder GetTaglistComponents(R34Post image, int page)
        {
            var tagoptions = new List<SelectMenuOptionBuilder>();
            var tags = image.tags.Split(' ');
            foreach (var t in tags.Skip(25 * page).Take(25))
            {
                tagoptions.Add(new SelectMenuOptionBuilder()
                    .WithLabel(t)
                    .WithValue(t)
                    .WithDefault(t.Contains("(artist)"))
                    );
            }
            var selectMenu = new SelectMenuBuilder()
                .WithType(ComponentType.SelectMenu)
                .WithCustomId("r34menu:")
                .WithPlaceholder("Select Tags to Search...")
                .WithMinValues(1)
                .WithMaxValues(tagoptions.Count())
                .WithOptions(tagoptions);
            var smrow = new ActionRowBuilder()
                .WithSelectMenu(selectMenu);
            var brow = new ActionRowBuilder()
                .WithButton("Prev", $"prevtags:{image.id}|{page - 1}", ButtonStyle.Danger, disabled: page <= 0)
                .WithButton("Rule34", url: $"https://rule34.xxx/index.php?page=post&s=view&id={image.id}", style: ButtonStyle.Link)
                .WithButton("Next", $"nexttags:{image.id}|{page + 1}", ButtonStyle.Success, disabled: page > (tags.Length / 25) - 1);
            var builder = new ComponentBuilder()
                .AddRow(smrow)
                .AddRow(brow);
            return builder;
        }

        public static ComponentBuilder GetTagComponents(string id, int page, int maxpages)
        {
            var builder = new ComponentBuilder();
            if (page != 0)
                builder.WithButton("Prev", $"tags:{id}|{page - 1}", disabled: page == 0);
            if(page != maxpages)
                builder.WithButton("Next", $"tags:{id}|{page + 1}", disabled: page == maxpages);
            return builder;
        }
    }
}
