using ExCSS;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace JIm.ChatClient.Core.Functions
{
    public class WeatherFunctions
    {

        [KernelFunction, Description("获取城市天气情况")]
        public async Task<string> GetWeather(
            [Description("城市名称")] string cityName,
            [Description("查询时段，值可以是[今天,明天,后天]")] string dayPart = "今天")
        {
            // 知心天气
            var url = $"https://api.seniverse.com/v3/weather/daily.json?key=S935bhXYNu5PKDchV&location={cityName}&language=zh-Hans&unit=c&start=0&days=5";
            // 高德天气
            //var url = $"https://restapi.amap.com/v3/weather/weatherInfo?key=cc1cefd83af78b72ece7f58ece2eef4e&city={cityName}&extensions=all";

            var client = new HttpClient();
            var response = await client.GetAsync(url);

            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<object>(content);

            return content;
        }

        [KernelFunction, Description("获取当前日期或时间")]
        public string GetCurrentTime()
        {
            return DateTime.Now.ToString();
        }

    }
}