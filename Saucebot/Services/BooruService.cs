using Newtonsoft.Json;

namespace Saucebot.Services
{
    public static class BooruService
    {
        public enum Site
        {
            Rule34,
            Gelbooru
        }

        public static Dictionary<string, Queue<R34Post>> cache = new Dictionary<string, Queue<R34Post>>();
        public static Dictionary<string, int> pagenum = new Dictionary<string, int>();

        public static Random rand = new Random();

        public static async Task<R34Post> GetImage(Site site, string tags)
        {
            if (cache.ContainsKey($"r34:{tags}"))
            {
                Queue<R34Post>? cached = GetCached(site, tags);
                if (cached != null)
                {
                    R34Post random = cached.Dequeue();
                    return random;
                }
                else throw new NullReferenceException($"Unable to fetch cached images.");
            }

            string uri = $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&tags={tags.Replace(' ', '+')}";

            Queue<R34Post> page = await GetNewPage(uri, site, tags);
            R34Post returned = page.Dequeue();
            cache.Add($"r34:{tags}", page);
            return returned;
        }

        static Queue<R34Post>? GetCached(Site site, string tags)
        {
            var c34 = cache[$"r34:{tags}"];
            if (c34.Count() < 1)
            {
                string uri = $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&tags={tags.Replace(' ', '+')}";
                var q = GetNewPage(uri, site, tags).GetAwaiter().GetResult();
                cache[$"r34:{tags}"] = q;
                return q;
            }
            else return c34;
        }

        static async Task<Queue<R34Post>> GetNewPage(string uri, Site site, string tags = "")
        {
            try
            {
                int page = 0;
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
                    throw new NullReferenceException("Unable to convert json into proper list.");
                }
                var r34q = new Queue<R34Post>(r34R.OrderBy(x => rand.Next()));
                pagenum.Add(uri, 0);
                return r34q;
            }
            catch
            {
                return new Queue<R34Post>();
            }
        }

        public static async Task<R34Post> GetPostById(string id)
        {
            var uri = $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&id={id}";

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
            if(tags == null)
                throw new NullReferenceException("Tag list returned empty.");
            return tags;
        }

        public static async Task<Queue<R34Post>> GetRelated(int parentid)
        {
            if (cache.ContainsKey($"collection:{parentid}"))
                return cache[$"collection:{parentid}"];
            var uri = $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&tags=parent:{parentid}";

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
#pragma warning disable CS8618 // Variables within this range are generated from a json request and thus cannot be null without exception, ignoring for now
    public class R34Post
    {
        public string preview_url { get; set; }
        public string sample_url { get; set; }
        public string file_url { get; set; }
        public int directory { get; set; }
        public string hash { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int id { get; set; }
        public string image { get; set; }
        public int change { get; set; }
        public string owner { get; set; }
        public int parent_id { get; set; }
        public string rating { get; set; }
        public bool sample { get; set; }
        public int sample_height { get; set; }
        public int sample_width { get; set; }
        public int score { get; set; }
        public string tags { get; set; }
        public string source { get; set; }
        public string status { get; set; }
        public bool has_notes { get; set; }
        public int comment_count { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}

