using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Verifier.Models;

namespace Verifier.Extensions
{
    public static class TMAPIHelper
    {
        static string url = "https://tmproxy.com/api/proxy";
        public static int[] LocationArr { get; } = { 1, 4, 5, 7, 8, 10, 11 };


        public static string Stats(string key)
        {
            var typedTemp = new
            {
                api_key = key
            };

            string param = JsonConvert.SerializeObject(typedTemp);
            string result = PostDataJson(new HttpClient(), url + "/stats", param);
            Console.WriteLine(result);
            return result;
        }

        public static string GetCurrentProxy(string key)
        {
            var typedTemp = new
            {
                api_key = key
            };

            string param = JsonConvert.SerializeObject(typedTemp);
            string result = PostDataJson(new HttpClient(), url + "/get-current-proxy", param);
            Console.WriteLine(result);
            return result;
        }

        public static string GetNewProxy(string key, string sign, int locationId)
        {
            var typedTemp = new
            {
                api_key = key,
                sign,
                id_location = locationId
            };

            string param = JsonConvert.SerializeObject(typedTemp);
            string result = PostDataJson(new HttpClient(), url + "/get-new-proxy", param);
            Console.WriteLine(result);
            return result;
        }

        public static string PostDataJson(HttpClient httpClient, string url, string data = null)
        {
            string html = "";
            HttpContent c = new StringContent(data, Encoding.UTF8, "application/json");
            var t = Task.Run(() => PostURI(new Uri(url), c));
            t.Wait();
            html = t.Result;

            return html;
        }

        static async Task<string> PostURI(Uri u, HttpContent c)
        {
            var response = string.Empty;
            using (var client = new HttpClient())
            {
                HttpResponseMessage result = await client.PostAsync(u, c);
                if (result.IsSuccessStatusCode)
                {
                    response = await result.Content.ReadAsStringAsync();

                }
            }
            return response;
        }

        public static NewProxyModel GetProxyModel(string apiKey)
        {
            var locationId = GetRandomlocation();
            var proxy = TMAPIHelper.GetNewProxy(apiKey, apiKey, locationId);
            NewProxyModel proxyModel = JsonConvert.DeserializeObject<NewProxyModel>(proxy);
            return proxyModel;
        }

        public static string[] GetNewProxyOnly(this string apiKey)
        {
            string[] httpsProxy;
            NewProxyModel proxyModel = GetProxyModel(apiKey);
            while (!(proxyModel.code == 0))
            {
                Thread.Sleep(1000);
                proxyModel = GetProxyModel(apiKey);
            }
            httpsProxy = proxyModel.data.https.Split(':');
            return httpsProxy;
        }

        private static int GetRandomlocation()
        {
            Random random = new Random();
            int locationId = random.Next(0, LocationArr.Length);
            return locationId;
        }

        public static string[] GetHttpsProxy(string apiKey)
        {
            string[] httpsProxy;
            NewProxyModel proxyModel = GetProxyModel(apiKey);
            if (proxyModel.code == 0 && proxyModel.data.next_request > 0)
            {
                httpsProxy = proxyModel.data.https.Split(':');
                return httpsProxy;
            }
            proxyModel = GetProxyModel(apiKey);
            while (!(proxyModel.code == 0))
            {
                Thread.Sleep(1000);
                proxyModel = GetProxyModel(apiKey);
            }
            httpsProxy = proxyModel.data.https.Split(':');
            return httpsProxy;
        }

        public static NewProxyModel GetCurrentProxyModel(string apiKey)
        {
            var proxy = GetCurrentProxy(apiKey);
            NewProxyModel proxyModel = JsonConvert.DeserializeObject<NewProxyModel>(proxy);
            return proxyModel;
        }
    }
}
