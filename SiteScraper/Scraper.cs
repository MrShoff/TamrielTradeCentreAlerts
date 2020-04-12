using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SiteScraper
{
    public class Scraper
    {
        public static List<KeyValuePair<string, string>> Scrape(Uri uri, IEnumerable<string> nodes)
        {
            List<KeyValuePair<string, string>> returnValues = new List<KeyValuePair<string, string>>();

            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(uri);

            string threadTitle = string.Empty;
            List<HtmlNode> postsNodes = new List<HtmlNode>();
            try
            {
                threadTitle = htmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[5]/h1[1]/span[1]").InnerText;
                postsNodes = htmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[7]/ol[1]").ChildNodes.Where(n => n.Id.StartsWith("post_")).ToList<HtmlNode>();
            }
            catch
            {

            }
            try
            {
                if (threadTitle == string.Empty) // thread is a poll
                {
                    threadTitle = htmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[6]/h1[1]/span[1]").InnerText;
                    postsNodes = htmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[8]/ol[1]").ChildNodes.Where(n => n.Id.StartsWith("post_")).ToList<HtmlNode>();
                }
            }
            catch
            {
                // thread is deleted/archived/unavailable
                return null;
            }

            threadTitle = threadTitle.Replace("&#8217;", "'").Replace("&quot;", "\"").Replace("&gt;", ">").Replace("&lt;", "<");

            string today = DateTime.Now.Subtract(TimeSpan.FromHours(2)).Date.ToString().Replace(" 12:00:00 AM", "");
            string yesterday = DateTime.Now.Subtract(TimeSpan.FromHours(26)).Date.ToString().Replace(" 12:00:00 AM", "");

            UserTextFeedback.ConsoleOut("Processing thread: " + threadTitle);

            foreach (HtmlNode postNode in postsNodes)
            {
                Post curPost = new Post();

                // parse post and thread info
                uint postId;
                uint.TryParse(postNode.Id.Substring(5), out postId);
                curPost.PostId = postId;
                curPost.ThreadId = threadId;
                curPost.ThreadUrl = url;
                curPost.ThreadTitle = threadTitle;

                // parse message info
                HtmlNode messageNode = postNode.SelectSingleNode(postNode.XPath + "/div[2]/div[2]/div[1]/div[1]/div[1]/blockquote[1]");
                string quotedText = "";
                try
                {
                    quotedText = messageNode.SelectSingleNode(messageNode.XPath + "/div[1]/div[1]").InnerText.Trim();
                    curPost.MessageText = messageNode.InnerText.Replace(quotedText, "");
                }
                catch
                {

                }
                if (quotedText == string.Empty)
                    curPost.MessageText = messageNode.InnerText;
                curPost.MessageText = curPost.MessageText.Replace("&#8217;", "'").Replace("&quot;", "\"").Replace("&gt;", ">").Replace("&lt;", "<")
                    .Replace("Quotes (6) disabled for this post", "").Replace("Quotes (7) disabled for this post", "").Replace("Quotes (8) disabled for this post", "").Replace("Quotes (9) disabled for this post", "").Replace("Quotes (10) disabled for this post", "").Replace("Quotes (11) disabled for this post", "")
                    .Replace("Sent from my iPhone using Tapatalk", "").Trim();

                // parse user info
                HtmlNode userinfo = postNode.SelectSingleNode(postNode.XPath + "/div[2]/div[1]/div[1]/div[1]/a[1]");
                curPost.Username = userinfo.InnerText.Trim();
                string userIdText = userinfo.GetAttributeValue("href", "").Replace("http://www.postcount.net/forum/member.php?", "");
                userIdText = userIdText.Substring(0, userIdText.IndexOf("-"));
                uint userId;
                uint.TryParse(userIdText, out userId);
                curPost.UserId = userId;

                // parse thread date
                string date = postNode.SelectSingleNode(postNode.XPath + "/div[1]/span[1]/span[1]").InnerText;
                date = date.Replace(",&nbsp;", " ").Replace("Today", today).Replace("Yesterday", yesterday);
                DateTime postDate = new DateTime();
                DateTime.TryParse(date, out postDate);
                postDate = postDate.Add(TimeSpan.FromHours(2));
                curPost.PostDate = postDate;

                if (DoReporting)
                {
                    UserTextFeedback.ConsoleOut(curPost.Username + " " + curPost.PostDate + ": ");
                    UserTextFeedback.ConsoleOut(curPost.MessageText);
                }

                postsInThread.Add(curPost);
            }

            try // try to find a next page
            {
                foreach (HtmlNode navNode in htmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[8]/div[1]/form[1]").ChildNodes)
                {
                    if (navNode.GetAttributeValue("class", "") == "prev_next")
                    {
                        string nextPageUrl = navNode.FirstChild.GetAttributeValue("href", "");
                        if (navNode.FirstChild.GetAttributeValue("rel", "") == "next")
                            postsInThread.AddRange(Scrape(nextPageUrl, threadId));
                    }
                }
            }
            catch
            {

            }

            if (DoReporting)
            {
                UserTextFeedback.ConsoleOut("Total number of posts in thread: " + postsInThread.Count);
                UserTextFeedback.ConsoleOut("Total time elapsed: " + Math.Round(DateTime.Now.Subtract(startedProcessing).TotalMilliseconds, 0) + "ms");
                UserTextFeedback.ConsoleOut("Done");
            }

            return postsInThread;
        }
    }
}
