using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HtmlAgilityPack;
using System.Text;
using System.IO;
using HtmlAgilityPackProject.Models;

namespace HtmlAgilityPackProject.Controllers
{
    public class HomeController : Controller
    {
        #region 私有变量

        private HtmlWeb webClient = new HtmlWeb();
        private const string FILE_PATH = @"J:\软件项目\HtmlAgilityPackProject\";
        private const string ANTI_URL = @"http://mp3.sogou.com/antispider/";

        #endregion

        public ActionResult Index()
        {
            ViewBag.Message = "Welcome to ASP.NET MVC!";
            return View("Index");
        }

        /// <summary>
        /// 测试蜘蛛爬虫
        /// </summary>
        /// <returns></returns>
        public JsonResult TestSpider()
        {
            string keyword = Request.Form["keyword"] == null ? string.Empty : Request.Form["keyword"].ToString().Trim();
            int searchEngine = Request.Form["searchEngine"] == null ? (int)SearchEngineEnum.Baidu : Convert.ToInt32(Request.Form["searchEngine"].ToString().Trim());

            string message = string.Empty;
            switch (searchEngine)
            {
                case (int)SearchEngineEnum.Baidu: message = BaiduSpiderSearch(keyword) ? "<span style=\"color:Green;\">爬虫成功</span>" : "<span style=\"color:Red;\">爬虫失败</span>"; break;
                case (int)SearchEngineEnum.Sogou: message = SogouSpiderSearch(keyword) ? "<span style=\"color:Green;\">爬虫成功</span>" : "<span style=\"color:Red;\">爬虫失败</span>"; break;
                case (int)SearchEngineEnum.Qihu: message = QihuSpiderSearch(keyword) ? "<span style=\"color:Green;\">爬虫成功</span>" : "<span style=\"color:Red;\">爬虫失败</span>"; break;
                default:
                    message = QihuSpiderSearch(keyword) ? "<span style=\"color:Green;\">爬虫成功</span>" : "<span style=\"color:Red;\">爬虫失败</span>"; break;
            }

            return Json(new { isSuccess = message }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 百度搜索结果
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public bool BaiduSpiderSearch(string keyword)
        {
            List<KeywordRank> keywordRankLs = new List<KeywordRank>();
            string url = string.Format("http://www.baidu.com/s?wd={0}", HttpUtility.UrlEncode(keyword, Encoding.UTF8));
            HtmlDocument htmlDoc = webClient.Load(url);
            string docHtml = htmlDoc.DocumentNode.InnerHtml;
            HtmlNodeCollection leftLinkNodes = htmlDoc.DocumentNode.SelectNodes(".//div[@id='content_left']/table[contains(@class,'ec_pp_f')]");
            int rank = 1;
            HtmlNode titleNode, descNode, urlNode;
            if (leftLinkNodes != null)
            {
                foreach (HtmlNode item in leftLinkNodes)
                {
                    KeywordRank keywordRank = new KeywordRank();
                    keywordRank.Position = PositionEnum.Left;
                    titleNode = item.SelectSingleNode(".//tr/td[contains(@class,'EC_header')]/a[contains(@class,'EC_title')]/span");
                    if (titleNode != null)
                        keywordRank.CreativeTitle = titleNode.InnerHtml.Replace("<font color=\"#CC0000\">", "{").Replace("</font>", "}");
                    descNode = item.SelectSingleNode(".//tr/td[contains(@class,'EC_PP')]/a[contains(@class,'EC_desc')]");
                    if (descNode != null)
                        keywordRank.CreativeDesc = descNode.InnerHtml.Replace("<font color=\"#CC0000\">", "{").Replace("</font>", "}");
                    else
                    {
                        descNode = item.SelectSingleNode(".//tr/td[contains(@class,'EC_PP')]/a[contains(@class,'EC_zpdes')]");
                        keywordRank.CreativeDesc = descNode == null ? string.Empty : descNode.InnerHtml.Replace("<font color=\"#CC0000\">", "{").Replace("</font>", "}");
                    }
                    urlNode = item.SelectSingleNode(".//tr/td[contains(@class,'EC_header')]/a[contains(@class,'EC_url')]/span");
                    if (urlNode != null)
                        keywordRank.DisplayUrl = urlNode.InnerText.Trim();

                    keywordRank.Rank = rank;
                    if (keywordRankLs.Where(n => n.DisplayUrl == keywordRank.DisplayUrl && n.CreativeTitle == keywordRank.CreativeTitle && n.CreativeDesc == keywordRank.CreativeDesc).Count() <= 0)
                        keywordRankLs.Add(keywordRank);
                    rank++;
                }
            }
            try
            {
                string baiduSpiderFile = string.Format("{0}{1}.txt", FILE_PATH, "baidu_spider");
                SaveSpiderFile(baiduSpiderFile, keywordRankLs);

                return true;
            }
            catch (IOException ex)
            {
                return false;
                throw ex;
            }
        }

        /// <summary>
        /// 奇虎360搜索结果
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        private bool QihuSpiderSearch(string keyword)
        {
            List<KeywordRank> keywordRankLs = new List<KeywordRank>();

            string url = string.Format("http://www.so.com/s?ie=utf-8&src=360sou_home&q={0}", HttpUtility.UrlEncode(keyword, Encoding.UTF8));
            HtmlDocument htmlDoc = webClient.Load(url);
            string docHtml = htmlDoc.DocumentNode.InnerHtml;
            HtmlNodeCollection leftLinkNodes = htmlDoc.DocumentNode.SelectNodes(".//div[@class='spread']/ul[contains(@id,'djbox')]/li");
            int rank = 1;
            HtmlNode titleNode, descNode, urlNode;
            if (leftLinkNodes != null)
            {
                foreach (HtmlNode item in leftLinkNodes)
                {
                    KeywordRank keywordRank = new KeywordRank();
                    keywordRank.Position = PositionEnum.Left;
                    titleNode = item.SelectSingleNode("./h3/a[@_cs]");
                    if (titleNode != null)
                        keywordRank.CreativeTitle = titleNode.InnerHtml.Replace("<em color=\"red\">", "{").Replace("</em>", "}");
                    descNode = item.SelectSingleNode("./p");
                    if (descNode != null)
                        keywordRank.CreativeDesc = descNode.InnerHtml.Replace("<em color=\"red\">", "{").Replace("</em>", "}");
                    urlNode = item.SelectSingleNode("./p/cite");
                    if (urlNode != null)
                        keywordRank.DisplayUrl = urlNode.InnerText.Trim();
                    keywordRank.Rank = rank;
                    if (keywordRankLs.Where(n => n.DisplayUrl == keywordRank.DisplayUrl && n.CreativeTitle == keywordRank.CreativeTitle && n.CreativeDesc == keywordRank.CreativeDesc).Count() <= 0)
                        keywordRankLs.Add(keywordRank);
                    rank++;
                }
            }
            try
            {
                string qihuSpiderFile = string.Format("{0}{1}.txt", FILE_PATH, "qihu_spider");
                SaveSpiderFile(qihuSpiderFile, keywordRankLs);

                return true;
            }
            catch (IOException ex)
            {
                return false;
                throw ex;
            }
        }

        /// <summary>
        /// 搜狗搜素
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        private bool SogouSpiderSearch(string keyword)
        {
            List<KeywordRank> keywordRankLs = new List<KeywordRank>();

            string url = string.Format("http://www.sogou.com/web?query={0}", HttpUtility.UrlEncode(keyword, Encoding.GetEncoding("gb2312")));

            HtmlDocument htmlDoc = webClient.Load(url);
            string docHtml = htmlDoc.DocumentNode.InnerHtml;
            HtmlNodeCollection leftLinkNodes = htmlDoc.DocumentNode.SelectNodes(".//div[@class='spread']/ul[contains(@id,'djbox')]/li");
            int rank = 1;
            HtmlNode titleNode, descNode, urlNode;
            if (leftLinkNodes != null)
            {
                foreach (HtmlNode item in leftLinkNodes)
                {
                    KeywordRank keywordRank = new KeywordRank();
                    keywordRank.Position = PositionEnum.Left;
                    titleNode = item.SelectSingleNode("./h3/a[@_cs]");
                    if (titleNode != null)
                        keywordRank.CreativeTitle = titleNode.InnerHtml.Replace("<em color=\"red\">", "{").Replace("</em>", "}");
                    descNode = item.SelectSingleNode("./p");
                    if (descNode != null)
                        keywordRank.CreativeDesc = descNode.InnerHtml.Replace("<em color=\"red\">", "{").Replace("</em>", "}");
                    urlNode = item.SelectSingleNode("./p/cite");
                    if (urlNode != null)
                        keywordRank.DisplayUrl = urlNode.InnerText.Trim();
                    keywordRank.Rank = rank;
                    if (keywordRankLs.Where(n => n.DisplayUrl == keywordRank.DisplayUrl && n.CreativeTitle == keywordRank.CreativeTitle && n.CreativeDesc == keywordRank.CreativeDesc).Count() <= 0)
                        keywordRankLs.Add(keywordRank);
                    rank++;
                }
            }
            try
            {
                string qihuSpiderFile = string.Format("{0}{1}.txt", FILE_PATH, "qihu_spider");
                SaveSpiderFile(qihuSpiderFile, keywordRankLs);

                return true;
            }
            catch (IOException ex)
            {
                return false;
                throw ex;
            }
        }

        /// <summary>
        /// 保存蜘蛛爬虫结果
        /// </summary>
        /// <param name="fileName"></param>
        private void SaveSpiderFile(string fileName, List<KeywordRank> keywordRankLs)
        {
            if (System.IO.File.Exists(fileName))
                System.IO.File.Delete(fileName);

            using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                byte[] beginBytes = Encoding.UTF8.GetBytes("百度搜索排名：\r\n");
                fs.Seek(0, SeekOrigin.Begin);
                fs.Write(beginBytes, 0, beginBytes.Length);

                foreach (var item in keywordRankLs)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("--------------------------");
                    sb.AppendLine(string.Format("排名：{0}", item.Rank));
                    sb.AppendLine(string.Format("位置：{0}", item.Position == PositionEnum.Left ? "左边" : "右边"));
                    sb.AppendLine(string.Format("创意名称：{0}", item.CreativeTitle));
                    sb.AppendLine(string.Format("创意描述：{0}", item.CreativeDesc));
                    sb.AppendLine(string.Format("显示URL：{0}", item.DisplayUrl));
                    byte[] dataBuffers = Encoding.UTF8.GetBytes(sb.ToString());
                    fs.Write(dataBuffers, 0, dataBuffers.Length);
                    fs.Flush();
                }
            }
        }

        public ActionResult About()
        {
            return View("About");
        }
    }
}
