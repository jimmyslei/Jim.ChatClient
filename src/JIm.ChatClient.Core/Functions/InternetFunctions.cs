using ExCSS;
using HtmlAgilityPack;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace JIm.ChatClient.Core.Functions
{
    public class SearchResult
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
    }
    public class InternetFunctions
    {
        //private const string BingTemplate = "https://cn.bing.com/search?q={0}";

        ///// <summary>
        ///// 搜索用户提出的问题
        ///// </summary>
        ////[KernelFunction, Description("搜索用户提出的问题")]
        //public async Task<string> OnlineSearchAsync(string query)
        //{
        //    var http = new HttpClient();

        //    var html = await http.GetStringAsync(string.Format(BingTemplate, HttpUtility.UrlEncode(query)));//.ConfigureAwait(false);

        //    // 使用 HtmlAgilityPack 解析 HTML
        //    var doc = new HtmlDocument();
        //    doc.LoadHtml(html);

        //    // 初始化搜索结果列表
        //    var results = new List<object>();

        //    // 选择搜索结果节点（Bing 的搜索结果通常在 <li class="b_algo"> 元素中）
        //    var nodes = doc.DocumentNode.SelectNodes("//li[@class='b_algo']");
        //    if (nodes != null)
        //    {
        //        foreach (var node in nodes)
        //        {
        //            // 提取标题（在 <h2><a>...</a></h2> 中）
        //            var titleNode = node.SelectSingleNode(".//h2/a");
        //            // 提取 URL（从标题链接的 href 属性中获取）
        //            var urlNode = titleNode?.Attributes["href"]?.Value;
        //            // 提取摘要（在 <p> 标签中）
        //            var snippetNode = node.SelectSingleNode(".//p");

        //            // 确保提取的字段有效
        //            if (titleNode != null && urlNode != null && snippetNode != null)
        //            {
        //                // 将urlNode转移
        //                urlNode = HttpUtility.HtmlDecode(urlNode);
        //                results.Add(new
        //                {
        //                    Title = titleNode.InnerText,
        //                    Url = urlNode,
        //                    Snippet = snippetNode.InnerText
        //                });
        //            }
        //        }
        //    }

        //    return JsonConvert.SerializeObject(results);

        //    //var scriptRegex = new Regex(@"<script[^>]*>[\s\S]*?</script>");
        //    //var styleRegex = new Regex(@"<style[^>]*>[\s\S]*?</style>");
        //    //var commentRegex = new Regex(@"<!--[\s\S]*?-->");
        //    //var headRegex = new Regex(@"<head[^>]*>[\s\S]*?</head>");
        //    //var tagAttributesRegex = new Regex(@"<(\w+)(?:\s+[^>]*)?>");
        //    //var emptyTagsRegex = new Regex(@"<(\w+)(?:\s+[^>]*)?>\s*</\1>");

        //    //html = scriptRegex.Replace(html, "");
        //    //html = styleRegex.Replace(html, "");
        //    //html = commentRegex.Replace(html, "");
        //    //html = headRegex.Replace(html, "");
        //    //html = tagAttributesRegex.Replace(html, "<$1>");
        //    //html = emptyTagsRegex.Replace(html, "");

        //    //return html;
        //}


        private readonly HttpClient _client;

        public InternetFunctions()
        {
            _client = new HttpClient();
            // 设置请求头，模拟浏览器行为
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36");
            _client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml");
            _client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        }

        public async Task<List<SearchResult>> ScrapeSearchResults(string query)
        {
            var results = new List<SearchResult>();

            try
            {
                // URL编码搜索查询
                string encodedQuery = HttpUtility.UrlEncode(query); //Uri.EscapeDataString(query);
                string url = $"https://cn.bing.com/search?q={encodedQuery}&ensearch=1&FORM=BESBTB";

                // 发送GET请求
                HttpResponseMessage response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // 获取响应内容
                string htmlContent = await response.Content.ReadAsStringAsync();

                // 使用HtmlAgilityPack解析HTML
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);

                // 提取搜索结果
                var searchResults = doc.DocumentNode.SelectNodes("//li[@class='b_algo']");

                if (searchResults != null)
                {
                    foreach (var result in searchResults)
                    {
                        var titleNode = result.SelectSingleNode(".//h2/a");
                        var descriptionNode = result.SelectSingleNode(".//div[@class='b_caption']/p");
                        var urlNode = titleNode;

                        if (titleNode != null)
                        {
                            var searchResult = new SearchResult
                            {
                                Title = titleNode.InnerText,
                                Url = urlNode?.GetAttributeValue("href", ""),
                                Description = descriptionNode?.InnerText ?? ""
                            };

                            results.Add(searchResult);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scraping Bing: {ex.Message}");
            }

            return results;
        }

        // 将搜索结果转换为大模型可用的格式
        public string FormatForLLM(List<SearchResult> results)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("搜索结果:");

            foreach (var result in results)
            {
                sb.AppendLine($"标题: {result.Title}");
                sb.AppendLine($"描述: {result.Description}");
                sb.AppendLine($"链接: {result.Url}");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}