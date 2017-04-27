using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Globalization;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace RSSCrawler
{
    public class Article
    {
        public string Title { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string Image { get; set; }
        public string Link { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
    }

    public class StringMatchingAlgorithm
    {
        public StringMatchingAlgorithm()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        public static void printArticle(Article article)
        {
            Console.WriteLine(article.Title);
            Console.WriteLine("Waktu rilis : " + article.ReleaseDate.ToString());
            Console.WriteLine("Link : " + article.Link);
            Console.WriteLine("Image : " + article.Image);
            Console.WriteLine("Summary : " + article.Summary);
            Console.WriteLine(article.Content);
            Console.WriteLine();
        }

        public static int[] computeFail(string pattern)
        {
            int[] fail = new int[pattern.Length];
            fail[0] = 0;

            int m = pattern.Length;
            int j = 0;
            int i = 1;
            while (i < m)
            {
                if (pattern[j] == pattern[i])
                {
                    fail[i] = j + 1;
                    i++;
                    j++;
                }
                else if (j > 0) { j = fail[j - 1]; }
                else { fail[i] = 0; i++; }
            }
            return fail;
        }

        public static int KMPMatch(string text, string pattern)
        {
            int n = text.Length;
            int m = pattern.Length;
            int[] fail = computeFail(pattern);

            int i = 0, j = 0;

            while (i < n)
            {
                if (pattern[j] == text[i])
                {
                    if (j == m - 1) { return i - m + 1; }
                    i++;
                    j++;
                }
                else if (j > 0) { j = fail[j - 1]; }
                else { i++; }
            }
            return -1;
        }

        public static int[] buildLast(string pattern)
        {
            int[] last = new int[128];
            for (int i = 0; i < 128; i++) { last[i] = -1; }
            for (int i = 0; i < pattern.Length; i++) { last[pattern[i]] = i; }
            return last;
        }

        public static int BMMatch(string text, string pattern)
        {
            int[] last = buildLast(pattern);
            int n = text.Length;
            int m = pattern.Length;
            int i = m - 1;

            if (i > n - 1) { return -1; }

            int j = m - 1;
            do
            {
                if (pattern[j] == text[i])
                {
                    if (j == 0) { return i; }
                    else { i--; j--; }
                }
                else
                {
                    try {
                        int lo = last[text[i]];
                        i = i + m - Math.Min(j, 1 + lo);
                        j = m - 1;
                    }
                    catch (Exception exp)
                    {
                        return -1;
                    }
                }
            } while (i <= n - 1);

            return -1;
        }

        public static int regexMatch(string text, string expr)
        {
            MatchCollection mc = Regex.Matches(text, expr);
            return mc.Count > 0 ? mc.Count : -1;
        }
    }

    public class CrawlingRSS
    {
        public Article[] xmlDoc;
        public int count;
        public static int tempCount;

        public void DataExtend(Article[] xmlDocTemp)
        {
            for (int i = count; i < count + xmlDocTemp.Length; i++)
            {
                xmlDoc[i] = new Article();
                xmlDoc[i].Title = xmlDocTemp[i - count].Title;
                xmlDoc[i].ReleaseDate = xmlDocTemp[i - count].ReleaseDate;
                xmlDoc[i].Link = xmlDocTemp[i - count].Link;
                xmlDoc[i].Image = xmlDocTemp[i - count].Image;
                xmlDoc[i].Summary = xmlDocTemp[i - count].Summary;
                xmlDoc[i].Content = xmlDocTemp[i - count].Content;
            }
            count += xmlDocTemp.Length;
        }

        public Article[] ParseRssFile(string link)
        {
            XmlDocument rssXmlDoc = new XmlDocument();
            Article[] rssContent = null;
            try
            {
                // Load the RSS file from the RSS URL
                rssXmlDoc.Load(link);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Xml Error");
                return null;
            }
            Console.WriteLine(link);
            // Parse the Items in the RSS file
            XmlNodeList rssNodeList = rssXmlDoc.SelectNodes("rss/channel/item");

            rssContent = new Article[rssNodeList.Count];
            for (int i = 0; i < rssNodeList.Count; i++)
            {
                rssContent[i] = new Article();
            }

            int idx = 0;
            string parseFormat = "ddd, dd MMM yyyy HH:mm:ss zzz";

            // Iterate through the items in the RSS file
            foreach (XmlNode rssNode in rssNodeList)
            {
                XmlNode rssSubNode;
                rssSubNode = rssNode.SelectSingleNode("title");
                rssContent[idx].Title = rssSubNode != null ? rssSubNode.InnerText : "";

                rssSubNode = rssNode.SelectSingleNode("link");
                rssContent[idx].Link = rssSubNode != null ? rssSubNode.InnerText : "";

                if (link.Equals("http://tempo.co/rss/terkini"))
                {
                    rssSubNode = rssNode.SelectSingleNode("image");
                    rssContent[idx].Image = rssSubNode != null ? rssSubNode.InnerText : "";
                    rssSubNode = rssNode.SelectSingleNode("description");
                    rssContent[idx].Summary = rssSubNode != null ? rssSubNode.InnerText : "";
                }
                else
                {
                    rssSubNode = rssNode.SelectSingleNode("description");
                    var Htmldoc = new HtmlDocument();
                    Htmldoc.LoadHtml(rssSubNode.InnerText);
                    HtmlNodeCollection nodes = Htmldoc.DocumentNode.SelectNodes("//img");
                    if (nodes != null)
                    {
                        foreach (HtmlNode nodeTemp in nodes)
                        {
                            rssContent[idx].Image = nodeTemp != null ? nodeTemp.Attributes["src"].Value : "";
                            break;
                        }
                    }
                    string desc = Regex.Replace(rssSubNode.InnerText, @"<img(.*?)>", "");
                    rssContent[idx].Summary = rssSubNode != null ? desc : "";
                }

                rssSubNode = rssNode.SelectSingleNode("pubDate");
                string dateString = rssSubNode != null ? rssSubNode.InnerText : "";
                rssContent[idx].ReleaseDate = DateTime.ParseExact(dateString, parseFormat, CultureInfo.InvariantCulture);

                try
                {
                    HtmlWeb articleWeb = new HtmlWeb();
                    HtmlDocument articleHtml = articleWeb.Load(rssContent[idx].Link);

                    if (link.Equals("http://rss.detik.com/index.php/detikcom"))
                    {
                        HtmlNode node = articleHtml.DocumentNode.SelectSingleNode("//div[@class='detail_text']");
                        if (node == null) node = articleHtml.DocumentNode.SelectSingleNode("//div[@class='text_detail']");
                        if (node == null) node = articleHtml.DocumentNode.SelectSingleNode("//div[@id='detikdetailtext']");
                        foreach (var childTag in node.SelectNodes("//comment() | //div | //script | //style "))
                            childTag.ParentNode.RemoveChild(childTag);
                        rssContent[idx].Content = node.InnerText;
                        rssContent[idx].Content = Regex.Replace(rssContent[idx].Content, @"\n", " ");
                    }
                    else if (link.Equals("http://tempo.co/rss/terkini") || link.Equals("http://rss.vivanews.com/get/all"))
                    {
                        HtmlNodeCollection nodes = articleHtml.DocumentNode.SelectNodes("//p");
                        foreach (HtmlNode node in nodes)
                        {
                            rssContent[idx].Content = string.Concat(rssContent[idx].Content, node.InnerText);
                        }
                    }
                    else if (link.Equals("http://www.antaranews.com/rss/terkini"))
                    {
                        HtmlNode node = articleHtml.DocumentNode.SelectSingleNode("//div[@id='content_news']");
                        foreach (var childTag in node.SelectNodes("//comment() | //div | //script | //style "))
                            childTag.ParentNode.RemoveChild(childTag);
                        rssContent[idx].Content = node.InnerText;
                    }

                    rssContent[idx].Content = Regex.Replace(rssContent[idx].Content, @"&nbsp;", " ");
                    rssContent[idx].Content = Regex.Replace(rssContent[idx].Content, @"&ndash;", "-");
                    rssContent[idx].Content = Regex.Replace(rssContent[idx].Content, @"&ldquo;", " ");
                    rssContent[idx].Content = Regex.Replace(rssContent[idx].Content, @"&rdquo;", " ");
                    rssContent[idx].Content = Regex.Replace(rssContent[idx].Content, @"&mdash;", " ");
                    rssContent[idx].Content = Regex.Replace(rssContent[idx].Content, @"Disclaimer :(.*?) golongan", "");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Html Error");
                    Console.WriteLine(rssContent[idx].Link);
                    rssContent[idx].Content = rssContent[idx].Content != null ? rssContent[idx].Content : "";
                }
                idx++;
            }
            tempCount += idx + 1;

            // Return the string that contain the RSS items
            return rssContent;
        }

        public CrawlingRSS()
        {
            Article[] temp1 = ParseRssFile("http://rss.detik.com/index.php/detikcom");
            Article[] temp2 = ParseRssFile("http://tempo.co/rss/terkini");
            Article[] temp3 = ParseRssFile("http://rss.vivanews.com/get/all");
            Article[] temp4 = ParseRssFile("http://www.antaranews.com/rss/terkini");

            xmlDoc = new Article[tempCount + 1];
            if (temp1 != null) DataExtend(temp1);
            if (temp2 != null) DataExtend(temp2);
            if (temp3 != null) DataExtend(temp3);
            if (temp4 != null) DataExtend(temp4);
        }
    }

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Globalization;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace RSSCrawler
{
    public class Article
    {
        public string Title { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string Image { get; set; }
        public string Link { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
    }

    public class StringMatchingAlgorithm
    {
        public StringMatchingAlgorithm()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        public static void printArticle(Article article)
        {
            Console.WriteLine(article.Title);
            Console.WriteLine("Waktu rilis : " + article.ReleaseDate.ToString());
            Console.WriteLine("Link : " + article.Link);
            Console.WriteLine("Image : " + article.Image);
            Console.WriteLine("Summary : " + article.Summary);
            Console.WriteLine(article.Content);
            Console.WriteLine();
        }

        public static int[] computeFail(string pattern)
        {
            int[] fail = new int[pattern.Length];
            fail[0] = 0;

            int m = pattern.Length;
            int j = 0;
            int i = 1;
            while (i < m)
            {
                if (pattern[j] == pattern[i])
                {
                    fail[i] = j + 1;
                    i++;
                    j++;
                }
                else if (j > 0) { j = fail[j - 1]; }
                else { fail[i] = 0; i++; }
            }
            return fail;
        }

        public static int KMPMatch(string text, string pattern)
        {
            int n = text.Length;
            int m = pattern.Length;
            int[] fail = computeFail(pattern);

            int i = 0, j = 0;

            while (i < n)
            {
                if (pattern[j] == text[i])
                {
                    if (j == m - 1) { return i - m + 1; }
                    i++;
                    j++;
                }
                else if (j > 0) { j = fail[j - 1]; }
                else { i++; }
            }
            return -1;
        }

        public static int[] buildLast(string pattern)
        {
            int[] last = new int[256];
            for (int i = 0; i < 256; i++) { last[i] = -1; }
            for (int i = 0; i < pattern.Length; i++) { last[pattern[i]] = i; }
            return last;
        }

        public static int BMMatch(string text, string pattern)
        {
            int[] last = buildLast(pattern);
            int n = text.Length;
            int m = pattern.Length;
            int skip;
            for (int i = 0; i <= n - m; i += skip)
            {
                skip = 0;
                for (int j = m - 1; j >= 0; j--)
                {
                    if (pattern[j] != text[i + j])
                    {
                        try {
                            skip = Math.Max(1, j - last[text[i + j]]);
                            break;
                        }
                        catch (Exception exp){
                            skip = 1;
                            break;
                        }     
                    }
                }
                if (skip == 0) return i;
            }
            return -1;
        }

        public static int regexMatch(string text, string expr)
        {
            MatchCollection mc = Regex.Matches(text, expr);
            return mc.Count > 0 ? mc.Count : -1;
        }
    }

    public class CrawlingRSS
    {
        public Article[] xmlDoc;
        public int count;
        public static int tempCount;

        public void DataExtend(Article[] xmlDocTemp)
        {
            for (int i = count; i < count + xmlDocTemp.Length; i++)
            {
                xmlDoc[i] = new Article();
                xmlDoc[i].Title = xmlDocTemp[i - count].Title;
                xmlDoc[i].ReleaseDate = xmlDocTemp[i - count].ReleaseDate;
                xmlDoc[i].Link = xmlDocTemp[i - count].Link;
                xmlDoc[i].Image = xmlDocTemp[i - count].Image;
                xmlDoc[i].Summary = xmlDocTemp[i - count].Summary;
                xmlDoc[i].Content = xmlDocTemp[i - count].Content;
            }
            count += xmlDocTemp.Length;
        }

        public Article[] ParseRssFile(string link)
        {
            XmlDocument rssXmlDoc = new XmlDocument();
            Article[] rssContent = null;
            try
            {
                // Load the RSS file from the RSS URL
                rssXmlDoc.Load(link);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Xml Error");
                return null;
            }
            Console.WriteLine(link);
            // Parse the Items in the RSS file
            XmlNodeList rssNodeList = rssXmlDoc.SelectNodes("rss/channel/item");

            rssContent = new Article[rssNodeList.Count];
            for (int i = 0; i < rssNodeList.Count; i++)
            {
                rssContent[i] = new Article();
            }

            int idx = 0;
            string parseFormat = "ddd, dd MMM yyyy HH:mm:ss zzz";

            // Iterate through the items in the RSS file
            foreach (XmlNode rssNode in rssNodeList)
            {
                XmlNode rssSubNode;
                rssSubNode = rssNode.SelectSingleNode("title");
                rssContent[idx].Title = rssSubNode != null ? rssSubNode.InnerText : "";

                rssSubNode = rssNode.SelectSingleNode("link");
                rssContent[idx].Link = rssSubNode != null ? rssSubNode.InnerText : "";

                if (link.Equals("http://tempo.co/rss/terkini"))
                {
                    rssSubNode = rssNode.SelectSingleNode("image");
                    rssContent[idx].Image = rssSubNode != null ? rssSubNode.InnerText : "";
                    rssSubNode = rssNode.SelectSingleNode("description");
                    rssContent[idx].Summary = rssSubNode != null ? rssSubNode.InnerText : "";
                }
                else
                {
                    rssSubNode = rssNode.SelectSingleNode("description");
                    var Htmldoc = new HtmlDocument();
                    Htmldoc.LoadHtml(rssSubNode.InnerText);
                    HtmlNodeCollection nodes = Htmldoc.DocumentNode.SelectNodes("//img");
                    if (nodes != null)
                    {
                        foreach (HtmlNode nodeTemp in nodes)
                        {
                            rssContent[idx].Image = nodeTemp != null ? nodeTemp.Attributes["src"].Value : "";
                            break;
                        }
                    }
                    string desc = Regex.Replace(rssSubNode.InnerText, @"<img(.*?)>", "");
                    rssContent[idx].Summary = rssSubNode != null ? desc : "";
                }

                rssSubNode = rssNode.SelectSingleNode("pubDate");
                string dateString = rssSubNode != null ? rssSubNode.InnerText : "";
                rssContent[idx].ReleaseDate = DateTime.ParseExact(dateString, parseFormat, CultureInfo.InvariantCulture);

                try
                {
                    HtmlWeb articleWeb = new HtmlWeb();
                    HtmlDocument articleHtml = articleWeb.Load(rssContent[idx].Link);

                    if (link.Equals("http://rss.detik.com/index.php/detikcom"))
                    {
                        HtmlNode node = articleHtml.DocumentNode.SelectSingleNode("//div[@class='detail_text']");
                        if (node == null) node = articleHtml.DocumentNode.SelectSingleNode("//div[@class='text_detail']");
                        if (node == null) node = articleHtml.DocumentNode.SelectSingleNode("//div[@id='detikdetailtext']");
                        foreach (var childTag in node.SelectNodes("//comment() | //div | //script | //style "))
                            childTag.ParentNode.RemoveChild(childTag);
                        rssContent[idx].Content = node.InnerText;
                        rssContent[idx].Content = Regex.Replace(rssContent[idx].Content, @"\n", " ");
                    }
                    else if (link.Equals("http://tempo.co/rss/terkini") || link.Equals("http://rss.vivanews.com/get/all"))
                    {
                        HtmlNodeCollection nodes = articleHtml.DocumentNode.SelectNodes("//p");
                        foreach (HtmlNode node in nodes)
                        {
                            rssContent[idx].Content = string.Concat(rssContent[idx].Content, node.InnerText);
                        }
                    }
                    else if (link.Equals("http://www.antaranews.com/rss/terkini"))
                    {
                        HtmlNode node = articleHtml.DocumentNode.SelectSingleNode("//div[@id='content_news']");
                        foreach (var childTag in node.SelectNodes("//comment() | //div | //script | //style "))
                            childTag.ParentNode.RemoveChild(childTag);
                        rssContent[idx].Content = node.InnerText;
                    }

                    rssContent[idx].Content = Regex.Replace(rssContent[idx].Content, @"&nbsp;", " ");
                    rssContent[idx].Content = Regex.Replace(rssContent[idx].Content, @"&ndash;", "-");
                    rssContent[idx].Content = Regex.Replace(rssContent[idx].Content, @"&ldquo;", " ");
                    rssContent[idx].Content = Regex.Replace(rssContent[idx].Content, @"&rdquo;", " ");
                    rssContent[idx].Content = Regex.Replace(rssContent[idx].Content, @"&mdash;", " ");
                    rssContent[idx].Content = Regex.Replace(rssContent[idx].Content, @"Disclaimer :(.*?) golongan", "");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Html Error");
                    Console.WriteLine(rssContent[idx].Link);
                    rssContent[idx].Content = rssContent[idx].Content != null ? rssContent[idx].Content : "";
                }
                idx++;
            }
            tempCount += idx + 1;

            // Return the string that contain the RSS items
            return rssContent;
        }

        public CrawlingRSS()
        {
            Article[] temp1 = ParseRssFile("http://rss.detik.com/index.php/detikcom");
            Article[] temp2 = ParseRssFile("http://tempo.co/rss/terkini");
            Article[] temp3 = ParseRssFile("http://rss.vivanews.com/get/all");
            Article[] temp4 = ParseRssFile("http://www.antaranews.com/rss/terkini");

            xmlDoc = new Article[tempCount + 1];
            if (temp1 != null) DataExtend(temp1);
            if (temp2 != null) DataExtend(temp2);
            if (temp3 != null) DataExtend(temp3);
            if (temp4 != null) DataExtend(temp4);
        }
    }

    public class NewsAggregator
    {
        public CrawlingRSS RSSArticle;

        public NewsAggregator()
        {
            RSSArticle = new CrawlingRSS();
        }

        public void KMPSearch(string pattern)
        {
            int i = -1;
            int idx = 0;
            while (i < RSSArticle.count - 1)
            {
                i++;
                if (RSSArticle.xmlDoc[i].Title != null && StringMatchingAlgorithm.KMPMatch(RSSArticle.xmlDoc[i].Title, pattern) != -1)
                {
                    idx++;
                    Console.Write(idx + " . ");
                    StringMatchingAlgorithm.printArticle(RSSArticle.xmlDoc[i]);
                }
                else if (RSSArticle.xmlDoc[i].Summary != null && StringMatchingAlgorithm.KMPMatch(RSSArticle.xmlDoc[i].Summary, pattern) != -1)
                {
                    idx++;
                    Console.Write(idx + " . ");
                    StringMatchingAlgorithm.printArticle(RSSArticle.xmlDoc[i]);
                }
                else if (RSSArticle.xmlDoc[i].Content != null && StringMatchingAlgorithm.KMPMatch(RSSArticle.xmlDoc[i].Content, pattern) != -1)
                {
                    idx++;
                    Console.Write(idx + " . ");
                    StringMatchingAlgorithm.printArticle(RSSArticle.xmlDoc[i]);
                }
            }
        }

        public void BMSearch(string pattern)
        {
            int i = -1;
            int idx = 0;
            while (i < RSSArticle.count - 1)
            {
                i++;
                if (RSSArticle.xmlDoc[i].Title != null && StringMatchingAlgorithm.BMMatch(RSSArticle.xmlDoc[i].Title, pattern) != -1)
                {
                    idx++;
                    Console.Write(idx + " . ");
                    StringMatchingAlgorithm.printArticle(RSSArticle.xmlDoc[i]);
                }
                else if (RSSArticle.xmlDoc[i].Summary != null && StringMatchingAlgorithm.BMMatch(RSSArticle.xmlDoc[i].Summary, pattern) != -1)
                {
                    idx++;
                    Console.Write(idx + " . ");
                    StringMatchingAlgorithm.printArticle(RSSArticle.xmlDoc[i]);
                }
                else if (RSSArticle.xmlDoc[i].Content != null && StringMatchingAlgorithm.BMMatch(RSSArticle.xmlDoc[i].Content, pattern) != -1)
                {
                    idx++;
                    Console.Write(idx + " . ");
                    StringMatchingAlgorithm.printArticle(RSSArticle.xmlDoc[i]);
                }
            }
        }

        public void regexSearch(string pattern)
        {
            int i = -1;
            int idx = 0;
            while (i < RSSArticle.count - 1)
            {
                i++;
                if (RSSArticle.xmlDoc[i].Title != null && StringMatchingAlgorithm.regexMatch(RSSArticle.xmlDoc[i].Title, pattern) != -1)
                {
                    idx++;
                    Console.Write(idx + " . ");
                    StringMatchingAlgorithm.printArticle(RSSArticle.xmlDoc[i]);
                }
                else if (RSSArticle.xmlDoc[i].Summary != null && StringMatchingAlgorithm.regexMatch(RSSArticle.xmlDoc[i].Summary, pattern) != -1)
                {
                    idx++;
                    Console.Write(idx + " . ");
                    StringMatchingAlgorithm.printArticle(RSSArticle.xmlDoc[i]);
                }
                else if (RSSArticle.xmlDoc[i].Content != null && StringMatchingAlgorithm.regexMatch(RSSArticle.xmlDoc[i].Content, pattern) != -1)
                {
                    idx++;
                    Console.Write(idx + " . ");
                    StringMatchingAlgorithm.printArticle(RSSArticle.xmlDoc[i]);
                }
            }
        }

        static void Main(string[] args)
        {
            NewsAggregator RAF = new NewsAggregator();
            Console.WriteLine("Crawling Done");
            for(int i=0; i < RAF.RSSArticle.count; i++)
            {
                StringMatchingAlgorithm.printArticle(RAF.RSSArticle.xmlDoc[i]);
            }
            Console.Write("Pilihan : ");
            int pil = Convert.ToInt32(Console.ReadLine());
            while (pil != 4)
            {
                Console.WriteLine("Pattern : ");
                string pattern = Console.ReadLine();
                Console.WriteLine(pil);
                Console.WriteLine(pattern);
                if (pil == 1)
                {
                    RAF.KMPSearch(pattern);
                }
                else if (pil == 2)
                {
                    RAF.BMSearch(pattern);
                }
                else if (pil == 3)
                {
                    RAF.regexSearch(pattern);
                }

                Console.WriteLine();
                Console.Write("Pilihan : ");
                pil = Convert.ToInt32(Console.ReadLine());
            }
        }
    }
}