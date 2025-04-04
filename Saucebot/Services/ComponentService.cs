﻿using Discord;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        static IEmote[] Emotes
        {
            get
            {
                return new List<string> { "💖", "💓", "🍆", "😍", "🥵", "🔥", "😩", "💯", "🥴", "🤤", "🍑", "✨" }.Select(x => (IEmote)new Emoji(x)).ToArray();
            }
        }
        static IEmote PickNext()
        {
            last = Emotes.Where(x => !x.Equals(last)).ToArray()[r.Next(Emotes.Length - 1)];
            return last;
        }

        public static async Task<ComponentBuilder> GetPostComponents(R34Post image, string tags) => await GetPostComponents(new List<R34Post> { image }, tags);

        public static async Task<ComponentBuilder> GetPostComponents(List<R34Post> images, string tags)
        {
            if (images.Count() == 1)
            {
                var image = images.First();
                var row1 = new ActionRowBuilder()
                    .WithButton("Another!~", $"r34:{tags}", ButtonStyle.Success, PickNext())
                    .WithButton("Link", url: $"https://rule34.xxx/index.php?page=post&s=view&id={image.id}", style: ButtonStyle.Link)
                    .WithButton("Info...", $"details:{image.id}|0", ButtonStyle.Primary)
                    .WithButton("x5!~", $"may:{tags}", ButtonStyle.Success, PickNext());
                var row2 = new ActionRowBuilder()
                    .WithButton("＋", $"save:", ButtonStyle.Success)
                    .WithButton("Tags...", $"tags:{image.id}|0")
                    .WithButton("Rule34", url: $"https://rule34.xxx/index.php?page=post$s=list&tags={tags.Replace(' ', '+')}", style: ButtonStyle.Link)
                    .WithButton("Hide", "delete:", ButtonStyle.Danger);
                var builder = new ComponentBuilder()
                    .AddRow(row1)
                    .AddRow(row2);
                return builder;
            }
            else
            {
                return await Task.Run(() =>
                {
                    var row1 = new ActionRowBuilder()
                        .WithButton("Another!~", $"r34:{tags}", ButtonStyle.Success, PickNext())
                        .WithButton("Rule34", url: $"https://rule34.xxx/index.php?page=post$s=list&tags={tags.Replace(' ', '+')}", style: ButtonStyle.Link)
                        .WithButton("x5!", $"may:{tags}", ButtonStyle.Success, PickNext());
                    var imageoptions = images.Select(
                        x => new SelectMenuOptionBuilder()
                        .WithLabel((images.IndexOf(x) + 1).ToString())
                        .WithValue($"tags:{x.id}|0")
                    ).ToList();
                    var selectMenu = new SelectMenuBuilder()
                        .WithType(ComponentType.SelectMenu)
                        .WithCustomId("manytags:")
                        .WithPlaceholder("Tags...")
                        .WithMinValues(1)
                        .WithMaxValues(1)
                        .WithOptions(imageoptions);
                    var row2 = new ActionRowBuilder()
                        .WithSelectMenu(selectMenu);
                    var row3 = new ActionRowBuilder()
                        .WithButton("More Details...", $"details:{string.Join("-", images.Select(x => x.id))}|0")
                        .WithButton("＋ Save to Dms...", $"save:", ButtonStyle.Success)
                        .WithButton("Hide", "delete:", ButtonStyle.Danger);
                    var builder = new ComponentBuilder()
                        .AddRow(row1)
                        .AddRow(row2)
                        .AddRow(row3);
                    return builder;
                });
            }

        }

        public static async Task<ComponentBuilder> GetDMComponents()
        {
            return await Task.Run(() =>
            {
                var row = new ActionRowBuilder()
                    .WithButton("Delete", "delete:", ButtonStyle.Danger);
                return new ComponentBuilder().AddRow(row);
            });
        }
        public static ComponentBuilder GetTagComponents(string id, int page, int maxpages)
        {
            var builder = new ComponentBuilder();
            if (page != 0)
                builder.WithButton("Prev", $"tags:{id}|{page - 1}", disabled: page == 0);
            if (page != maxpages)
                builder.WithButton("Next", $"tags:{id}|{page + 1}", disabled: page == maxpages);
            return builder;
        }

        public static EmbedBuilder GetDetailsEmbed(string[] ids, int page, out ComponentBuilder builder, out string url)
        {
            R34Post current = BooruService.GetPostById(ids[page]).GetAwaiter().GetResult();
            url = current.file_url != null ? current.file_url : "";

            builder = new ComponentBuilder();
            if (ids.Count() > 1)
            {
                builder.WithButton("Prev", $"details:{string.Join("-", ids)}|{page - 1}", ButtonStyle.Danger, disabled: page == 0);
                builder.WithButton("＋ Save to Dms...", $"save:{$"[Link]({current.file_url})"}", ButtonStyle.Success);
                builder.WithButton("Hide", "delete:", ButtonStyle.Danger);
                builder.WithButton("Next", $"details:{string.Join("-", ids)}|{page + 1}", ButtonStyle.Primary, disabled: page + 1 == ids.Count());
            }
            else{
                builder.WithButton("＋ Save to Dms...", $"save:{$"[Link]({current.file_url})"}", ButtonStyle.Success);
                builder.WithButton("Delete", "delete:", ButtonStyle.Danger);
            }


            EmbedBuilder embedBuilder = new EmbedBuilder()
            {
                Title = "Rule34.xxx",
                ImageUrl = (url.EndsWith(".png") || url.EndsWith(".jpg") || url.EndsWith(".jpeg") || url.EndsWith(".webp") || url.EndsWith(".gif")) ? url : (current.preview_url != null ? current.preview_url : ""),
                Color = Color.Teal,
                Description = $"```\n{current.tags}\n```"
            };
            embedBuilder.AddField("Link", $"https://rule34.xxx/index.php?page=post&s=view&id={current.id}", true);
            embedBuilder.AddField("Score", current.score, true);
            embedBuilder.AddField("Size", $"{current.width}x{current.height}px", true);
            return embedBuilder;
        }
    }
}
