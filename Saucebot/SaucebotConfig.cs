using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Saucebot
{
    public class SaucebotConfig
    {
        [JsonInclude]
        private string Token { get; set; } = "";
        [JsonInclude]
        private List<string> Statuses { get; set; } = new List<string>();
        public enum Site
        {
            Rule34,
            E621
        }
        [JsonInclude]
        private Dictionary<Site, string> BooruTokens { get; set; } = new Dictionary<Site, string>();
        public static SaucebotConfig GetConfig()
        {
            if (File.Exists("config.json"))
            {
                using (var stream = File.OpenRead("config.json"))
                {
                    var config = JsonSerializer.Deserialize<SaucebotConfig>(stream);
                    if (config != null)
                    {
                        if (config.GetToken() != "")
                            return config;
                    }
                }
            }

            // No config returned, lets get that data.
            var newConfig = new SaucebotConfig();
            newConfig.Token = Program.Prompt($"Please enter your bot's token: ");
            bool statusEdit = Program.YNPrompt($"Would you like to add statuses now? (y/n): ");
            while (statusEdit)
            {
                var status = Program.Prompt($"Please enter a status: ");
                newConfig.Statuses.Add(status);
                statusEdit = Program.YNPrompt($"Add another? (y/n): ");
            }
            foreach (Site value in Enum.GetValues(typeof(Site)))
            {
                var token = Program.Prompt($"Please enter a token for site {value}: ", true);
                newConfig.BooruTokens.Add(value, token);
            }
            Program.Print($"Config Filled, Thank you!").GetAwaiter().GetResult();

            File.WriteAllText($"config.json", JsonSerializer.Serialize<SaucebotConfig>(newConfig));
            return newConfig;
        }

        public string GetToken()
        {
            return Token;
        }

        public List<string> GetStatuses()
        {
            return Statuses;
        }

        public string GetBooruToken(Site site)
        {
            return BooruTokens[site];
        }
    }
}