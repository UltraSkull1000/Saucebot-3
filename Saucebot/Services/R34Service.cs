using Newtonsoft.Json;
namespace Saucebot.Services
{
    public static class R34Service
    {
        public static Dictionary<string, Queue<R34Post>> cache = new Dictionary<string, Queue<R34Post>>();
        public static Dictionary<string, int> pagenum = new Dictionary<string, int>();
        public static Random rand = new Random();
        private static SaucebotConfig config = SaucebotConfig.GetConfig();
        private static string api_key = config.GetBooruToken(SaucebotConfig.Site.Rule34);
        public static async Task<R34Post?> GetImage(string tags)
        {
            if (cache.ContainsKey($"r34:{tags}")) // We have cached a page for this tag set.
            {
                Queue<R34Post>? cached = GetCached(tags); // Attempt to retrieve the cached page. Will fetch a new page if the cached page was empty.
                if (cached != null) // The cached page had entries!
                {
                    R34Post random = cached.Dequeue(); // The order was randomized on fetch, so we just pull from the top of the queue. 
                    return random;
                }
                else throw new NullReferenceException($"Unable to fetch cached images."); // Usually this means that you've pulled all of the images that exist in the tag.
            }

            // We don't have a cached page for this, start a new one.
            string uri = $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&tags={tags.Replace(' ', '+')}&&api_key={api_key}&user_id=5584446"; // Generate API address

            Queue<R34Post> page = await GetNewPage(uri, tags); // Fetches a page with the specified tags.
            if (page.Count() == 0)
            {
                return null;
            }
            R34Post returned = page.Dequeue(); // Pulls an image
            cache.Add($"r34:{tags}", page); // Caches the page that we pulled.
            return returned;
        }

        static Queue<R34Post>? GetCached(string tags)
        {
            if (!cache.ContainsKey($"r34:{tags}")) // Double-checks whether the page exists within the cache. Should never be an issue, but its here for safety anyway.
                throw new NullReferenceException("Page does not exist within cache!");
            var c34 = cache[$"r34:{tags}"]; // Grabs the cached page.
            if (c34.Count() < 1) // There are no images on this page.
            {
                string uri = $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&tags={tags.Replace(' ', '+')}&api_key={api_key}&user_id=5584446"; // Regenerate URI
                var q = GetNewPage(uri, tags).GetAwaiter().GetResult(); // Fetch a new page from the site
                cache[$"r34:{tags}"] = q; // Cache the page.
                return cache[$"r34:{tags}"];
            }
            else return c34; // The cache still contained images, so obviously just return it
        }

        static async Task<Queue<R34Post>> GetNewPage(string uri, string tags = "")
        {
            int page = 0;
            try
            {
                if (pagenum.TryGetValue(uri, out page))
                {
                    pagenum[uri] = page + 1;
                    uri += $"&pid={++page}";
                }

                HttpClient client = new HttpClient();
                HttpResponseMessage resp = await client.GetAsync(uri);
                resp.EnsureSuccessStatusCode();

                string json = await resp.Content.ReadAsStringAsync();

                var r34R = JsonConvert.DeserializeObject<List<R34Post>>(json);
                if (r34R == null)
                {
                    return new Queue<R34Post>();
                }
                var r34q = new Queue<R34Post>(r34R.OrderBy(x => rand.Next()));
                pagenum.Add(uri, 0);
                return r34q;
            }
            catch
            {
                if (page == 0) // There are no images associated with the tag set, return null.
                    return new Queue<R34Post>();
                // There are no images left on this tagset, set page to 0 and recurse
                pagenum[uri] = 0;
                return await GetNewPage(uri, tags);
            }
        }

        public static async Task<R34Post> GetPostById(string id)
        {
            var uri = $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&id={id}&api_key={api_key}&user_id=5584446";

            HttpClient client = new HttpClient();
            HttpResponseMessage resp = await client.GetAsync(uri);
            resp.EnsureSuccessStatusCode();

            string json = await resp.Content.ReadAsStringAsync();

            var r34R = JsonConvert.DeserializeObject<List<R34Post>>(json);
            if (r34R == null)
            {
                throw new NullReferenceException("Unable to convert json into proper list.");
            }
            return r34R.First();
        }

        public static string?[]? GetTags(string search)
        {
            var tags = TagService.GetTags(search).GetAwaiter().GetResult();
            if (tags == null)
                throw new NullReferenceException("Tag list returned empty.");
            return tags;
        }

        public static async Task<Queue<R34Post>> GetRelated(int parentid)
        {
            if (cache.ContainsKey($"collection:{parentid}"))
                return cache[$"collection:{parentid}"];
            var uri = $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&tags=parent:{parentid}&api_key={api_key}&user_id=5584446";

            HttpClient client = new HttpClient();
            HttpResponseMessage resp = await client.GetAsync(uri);
            resp.EnsureSuccessStatusCode();

            string json = await resp.Content.ReadAsStringAsync();

            var r34R = JsonConvert.DeserializeObject<List<R34Post>>(json);
            if (r34R == null)
            {
                throw new NullReferenceException("Unable to convert json into proper list.");
            }
            var r34Q = new Queue<R34Post>(r34R);
            cache.Add($"collection:{parentid}", r34Q);
            return r34Q;
        }
    }
    public class R34Post
    {
        public string? preview_url { get; set; }
        public string? sample_url { get; set; }
        public string? file_url { get; set; }
        public int directory { get; set; }
        public string? hash { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int id { get; set; }
        public string? image { get; set; }
        public int change { get; set; }
        public string? owner { get; set; }
        public int parent_id { get; set; }
        public string? rating { get; set; }
        public bool sample { get; set; }
        public int sample_height { get; set; }
        public int sample_width { get; set; }
        public int score { get; set; }
        public string? tags { get; set; }
        public string? source { get; set; }
        public string? status { get; set; }
        public bool has_notes { get; set; }
        public int comment_count { get; set; }
    }
}

