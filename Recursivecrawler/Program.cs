using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace Recursivecrawler
{
    internal class Program
    {
        static HttpClient _httpclient = new();
        static Regex rx = new Regex(@".*\.(jpg|png|gif)?$");
        static Mutex s = new();
        static List<string> linksToVisit = new();

        static ConcurrentQueue<string> cq = new ConcurrentQueue<string>();



        static async Task Main(string[] args)
        {

           //await ParseLinksAsync("https://www.wallpaperflare.com/");

            HttpResponseMessage resp = await _httpclient.GetAsync("https://www.wallpaperflare.com/");

            byte[] bytes = await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            string download = Encoding.ASCII.GetString(bytes);

            HashSet<string> list = new HashSet<string>();

            var doc = new HtmlDocument();
            doc.LoadHtml(download);
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//a[@href]");

            foreach (var n in nodes)
            {
                string href = n.Attributes["href"].Value;
                cq.Enqueue(GetAbsoluteUrlString("https://www.wallpaperflare.com/", href));
            }


            Action action = () =>
            {
                string dequeued;
                while (true)
                {
                    if (cq.TryDequeue(out dequeued))
                    {
                        var document = new HtmlWeb().Load(dequeued);
                        var urls = document.DocumentNode.Descendants("img").Select(e => e.GetAttributeValue("src", null)).Where(s => !String.IsNullOrEmpty(s));
                        foreach (var img in urls)
                        {
                            Console.WriteLine(img);
                        }
                        ParseLinksAsync(dequeued);

                    }
                }


            };

            Parallel.Invoke(action, action, action, action);

            Console.Read();
        }



        public static async void ParseLinksAsync(string urlToCrawl)
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
                cq.Enqueue(GetAbsoluteUrlString(urlToCrawl, href));
            }
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