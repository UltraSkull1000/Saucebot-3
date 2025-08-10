using Newtonsoft.Json;
namespace Saucebot.Services
{
    public class E621Service
    {
        public static Dictionary<string, Queue<E621Post>> cache = new Dictionary<string, Queue<E621Post>>();
        public static Dictionary<string, int> pagenum = new Dictionary<string, int>();
        public static Random rand = new Random();
        public static async Task<E621Post?> GetImage(string tags)
        {
            if (cache.ContainsKey($"e621:{tags}")) // We have cached a page for this tag set.
            {
                Queue<E621Post>? cached = GetCached(tags); // Attempt to retrieve the cached page. Will fetch a new page if the cached page was empty.
                if (cached != null) // The cached page had entries!
                {
                    E621Post random = cached.Dequeue(); // The order was randomized on fetch, so we just pull from the top of the queue. 
                    return random;
                }
                else throw new NullReferenceException($"Unable to fetch cached images."); // Usually this means that you've pulled all of the images that exist in the tag.
            }

            // We don't have a cached page for this, start a new one.
            string uri = $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&tags={tags.Replace(' ', '+')}"; // Generate API address

            Queue<E621Post> page = await GetNewPage(uri, tags); // Fetches a page with the specified tags.
            if (page.Count() == 0)
            {
                return null;
            }
            E621Post returned = page.Dequeue(); // Pulls an image
            cache.Add($"e621:{tags}", page); // Caches the page that we pulled.
            return returned;
        }

        static Queue<E621Post>? GetCached(string tags)
        {
            if (!cache.ContainsKey($"e621:{tags}")) // Double-checks whether the page exists within the cache. Should never be an issue, but its here for safety anyway.
                throw new NullReferenceException("Page does not exist within cache!");
            var c34 = cache[$"e621:{tags}"]; // Grabs the cached page.
            if (c34.Count() < 1) // There are no images on this page.
            {
                string uri = $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&tags={tags.Replace(' ', '+')}"; // Regenerate URI
                var q = GetNewPage(uri, tags).GetAwaiter().GetResult(); // Fetch a new page from the site
                cache[$"e621:{tags}"] = q; // Cache the page.
                return cache[$"e621:{tags}"];
            }
            else return c34; // The cache still contained images, so obviously just return it
        }

        static async Task<Queue<E621Post>> GetNewPage(string uri, string tags = "")
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

                var e621R = JsonConvert.DeserializeObject<List<E621Post>>(json);
                if (e621R == null)
                {
                    return new Queue<E621Post>();
                }
                var e621q = new Queue<E621Post>(e621R.OrderBy(x => rand.Next()));
                pagenum.Add(uri, 0);
                return e621q;
            }
            catch
            {
                if (page == 0) // There are no images associated with the tag set, return null.
                    return new Queue<E621Post>();
                // There are no images left on this tagset, set page to 0 and recurse
                pagenum[uri] = 0;
                return await GetNewPage(uri, tags);
            }
        }

        public static async Task<E621Post> GetPostById(string id)
        {
            var uri = $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&id={id}";

            HttpClient client = new HttpClient();
            HttpResponseMessage resp = await client.GetAsync(uri);
            resp.EnsureSuccessStatusCode();

            string json = await resp.Content.ReadAsStringAsync();

            var e621R = JsonConvert.DeserializeObject<List<E621Post>>(json);
            if (e621R == null)
            {
                throw new NullReferenceException("Unable to convert json into proper list.");
            }
            return e621R.First();
        }

        public static string?[]? GetTags(string search)
        {
            var tags = TagService.GetTags(search).GetAwaiter().GetResult();
            if (tags == null)
                throw new NullReferenceException("Tag list returned empty.");
            return tags;
        }

        public static async Task<Queue<E621Post>> GetRelated(int parentid)
        {
            if (cache.ContainsKey($"collection:{parentid}"))
                return cache[$"collection:{parentid}"];
            var uri = $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&tags=parent:{parentid}";

            HttpClient client = new HttpClient();
            HttpResponseMessage resp = await client.GetAsync(uri);
            resp.EnsureSuccessStatusCode();

            string json = await resp.Content.ReadAsStringAsync();

            var e621R = JsonConvert.DeserializeObject<List<E621Post>>(json);
            if (e621R == null)
            {
                throw new NullReferenceException("Unable to convert json into proper list.");
            }
            var e621Q = new Queue<E621Post>(e621R);
            cache.Add($"collection:{parentid}", e621Q);
            return e621Q;
        }
    }

    public class E621Post{

    }
}
