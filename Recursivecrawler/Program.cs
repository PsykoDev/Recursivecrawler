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
        static Regex rx = new Regex(@".*\.(jpg|png|gif|jpeg|avif|webp)?$");
        static Mutex s = new();
        static List<string> alreadysee = new();
        static ConcurrentQueue<string> cq = new ConcurrentQueue<string>();



        static async Task Main(string[] args)
        {

            for (int i = 0; i < 15; i++)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { process(); }), null);
            }

            Console.Read();
        }


        static async void process()
        {

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
                alreadysee.Add(GetAbsoluteUrlString("https://www.wallpaperflare.com/", href));
            }


            Action action = () =>
            {
                string dequeued;
                Console.WriteLine("Trying");
                while (true)
                {
                    if (cq.TryDequeue(out dequeued))
                    {
                        Console.WriteLine("processing queue");
                        var document = new HtmlWeb().Load(dequeued);
                        var urls = document.DocumentNode.Descendants("img").Select(e => e.GetAttributeValue("src", null)).Where(s => !String.IsNullOrEmpty(s));
                        foreach (var img in urls)
                        {
                            if (rx.IsMatch(img))
                            {

                                Console.WriteLine(img + "\n");
                            }
                            Console.WriteLine(cq.Count);
                        }
                        ParseLinksAsync(dequeued);

                    }
                }
            };

            for (int i = 0; i < 15; i++)
                Parallel.Invoke(action);
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
                //cq.Enqueue(GetAbsoluteUrlString(urlToCrawl, href));
                alreadysee.Add(GetAbsoluteUrlString(urlToCrawl, href));
                RemoveDuplicatesSet(alreadysee);
                cq.Clear();
                for(int i = 0; i<alreadysee.Count; i++)
                {
                    cq.Enqueue(alreadysee[i]);
                }
                
            }
        }

        static string GetAbsoluteUrlString(string baseUrl, string url)
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
                uri = new Uri(new Uri(baseUrl), uri);
            return uri.ToString();
        }

        public static List<string> RemoveDuplicatesSet(List<string> items)
        {
            var result = new List<string>();
            var set = new HashSet<string>();
            for (int i = 0; i < items.Count; i++)
            {
                if (!set.Contains(items[i]))
                {
                    result.Add(items[i]);
                    set.Add(items[i]);
                }
            }
            return result;
        }
    }
}