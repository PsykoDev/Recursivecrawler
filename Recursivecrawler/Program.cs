using HtmlAgilityPack;
using System.Text;

namespace Recursivecrawler
{
    internal class Program
    {
        static HttpClient _httpclient = new();

        static List<string> linksToVisit = new();
        static void Main(string[] args)
        {


            linksToVisit.AddRange(ParseLinksAsync("https://www.wallpaperflare.com/").Result);

            for (int i = 0; i < linksToVisit.Count; i++)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { GetImage(); }), null);
            }

            Console.Read();
        }

        static void GetImage()
        {
            for (int i = 0; i < linksToVisit.Count; i++)
            {
                var document = new HtmlWeb().Load(linksToVisit[i]);
                var urls = document.DocumentNode.Descendants("img").Select(e => e.GetAttributeValue("src", null)).Where(s => !String.IsNullOrEmpty(s));
                foreach (var img in urls)
                {
                    Console.WriteLine(img);
                }

                linksToVisit.AddRange(ParseLinksAsync(linksToVisit[i]).Result);
            }
        }

        public static async Task<List<string>> ParseLinksAsync(string urlToCrawl)
        {

            HttpResponseMessage resp = await _httpclient.GetAsync(urlToCrawl);

            byte[] bytes = await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            string download = Encoding.ASCII.GetString(bytes);

            HashSet<string> list = new HashSet<string>();

            var doc = new HtmlDocument();
            doc.LoadHtml(download);
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//a[@href]");

            foreach (var n in nodes)
            {
                string href = n.Attributes["href"].Value;
                list.Add(GetAbsoluteUrlString(urlToCrawl, href));
            }
            return list.ToList();
        }

        static string GetAbsoluteUrlString(string baseUrl, string url)
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
                uri = new Uri(new Uri(baseUrl), uri);
            return uri.ToString();
        }
    }
}