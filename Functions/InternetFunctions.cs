using ExCSS;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace chatClient.Functions
{
    public class InternetFunctions
    {
        private const string BingTemplate = "https://cn.bing.com/search?q={0}";

        /// <summary>
        /// 搜索用户提出的问题
        /// </summary>
        [KernelFunction, Description("搜索用户提出的问题")]
        public async Task<string> GetAsync(string value)
        {
            var http = new HttpClient(); //httpClientFactory.CreateClient(nameof(HttpClientFunction));

            var html = await http.GetStringAsync(string.Format(BingTemplate, value)).ConfigureAwait(false);

            var scriptRegex = new Regex(@"<script[^>]*>[\s\S]*?</script>");
            var styleRegex = new Regex(@"<style[^>]*>[\s\S]*?</style>");
            var commentRegex = new Regex(@"<!--[\s\S]*?-->");
            var headRegex = new Regex(@"<head[^>]*>[\s\S]*?</head>");
            var tagAttributesRegex = new Regex(@"<(\w+)(?:\s+[^>]*)?>");
            var emptyTagsRegex = new Regex(@"<(\w+)(?:\s+[^>]*)?>\s*</\1>");

            html = scriptRegex.Replace(html, "");
            html = styleRegex.Replace(html, "");
            html = commentRegex.Replace(html, "");
            html = headRegex.Replace(html, "");
            html = tagAttributesRegex.Replace(html, "<$1>");
            html = emptyTagsRegex.Replace(html, "");

            return html;
        }
    }
}