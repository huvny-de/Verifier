﻿using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TwoCaptcha.Models;
using Flurl;
using Flurl.Http;

namespace Verifier
{
    public class CaptChaExtension
    {
        static string siteKey = "6LeTnxkTAAAAAN9QEuDZRpn90WwKk_R1TRW_g-JC";
        static string redditUrl = @"https://www.reddit.com/register/";
        static string url = "http://2captcha.com";

        /// <summary>
        /// How to obtain the parameters? https://2captcha.com/2captcha-api#solving_recaptchav2_new
        /// </summary>
        public async Task<TwoCaptcha.Models.TwoCaptcha> ReCaptchaV2Async(string apikey, int softId = 5287317)
        {
            var client = new HttpClient();

            var twoCaptcha = await url
                .AppendPathSegment("in.php")
                .SetQueryParam("key", apikey)
                .SetQueryParam("method", "userrecaptcha")
                .SetQueryParam("json", "1")
                .SetQueryParam("soft_id", softId)
                .SetQueryParam("googlekey", siteKey)
                .SetQueryParam("pageurl", redditUrl)
                .SetQueryParam("here", "now")
                .GetJsonAsync<TwoCaptcha.Models.TwoCaptcha>();

            var idRequest = twoCaptcha.Request;

            if (twoCaptcha.Status == 1)
            {
                do
                {
                    // Wait time before checking if the CAPTCHA has been resolved 
                    await Task.Delay(5000);

                    twoCaptcha = await url
                        .AppendPathSegment("res.php")
                        .SetQueryParam("key", apikey)
                        .SetQueryParam("action", "get")
                        .SetQueryParam("json", "1")
                        .SetQueryParam("id", idRequest)
                        .GetJsonAsync<TwoCaptcha.Models.TwoCaptcha>();

                    if (twoCaptcha.Request == "ERROR_CAPTCHA_UNSOLVABLE")
                    {
                        break;
                    }

                } while (twoCaptcha.Request == "CAPCHA_NOT_READY");
            }

            twoCaptcha.IdRequest = idRequest;
            return twoCaptcha;
        }

        public async Task<TwoCaptcha.Models.TwoCaptcha> NormalCaptchaAsync(string key2Captcha, string imgCaptchaBase64,
            NumericEnum numeric = NumericEnum.NotSpecified, byte minLength = 0, byte maxLength = 0, RegSenseEnum regSense = RegSenseEnum.CaptchaInNotCaseSensitive,
            PhraseEnum phrase = PhraseEnum.CaptchaContainsOneWord, CalcEnum calc = CalcEnum.NotSpecified, LanguageEnum language = LanguageEnum.NotSpecified,
            string textInstructions = null, int softId = 5287317)
        {
            var client = new HttpClient();

            var fields = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key", key2Captcha),
                new KeyValuePair<string, string>("json", "1"),
                new KeyValuePair<string, string>("numeric", ((int)numeric).ToString()),
                new KeyValuePair<string, string>("min_len", minLength.ToString()),
                new KeyValuePair<string, string>("max_len", maxLength.ToString()),
                new KeyValuePair<string, string>("regsense", ((int)regSense).ToString()),
                new KeyValuePair<string, string>("phrase", ((int)phrase).ToString()),
                new KeyValuePair<string, string>("calc", ((int)calc).ToString()),
                new KeyValuePair<string, string>("language", ((int)calc).ToString()),
                new KeyValuePair<string, string>("method", "base64"),
                new KeyValuePair<string, string>("body", imgCaptchaBase64),
                new KeyValuePair<string, string>("soft_id", softId.ToString()),
            };

            if (textInstructions != null)
            {
                fields.Add(new KeyValuePair<string, string>("textinstructions", ""));
            }

            var form = new FormUrlEncodedContent(fields);
            var response = await client.PostAsync("http://2captcha.com/in.php", form);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var twoCaptcha = TwoCaptcha.Models.TwoCaptcha.FromJson(json);
            var idRequest = twoCaptcha.Request;

            if (twoCaptcha.Status == 1)
            {
                do
                {
                    // Wait time before checking if the CAPTCHA has been resolved 
                    await Task.Delay(5000);

                    json = await client.GetStringAsync($"http://2captcha.com/res.php?key={key2Captcha}&action=get&id={idRequest}&json=1");
                    twoCaptcha = TwoCaptcha.Models.TwoCaptcha.FromJson(json);

                    if (twoCaptcha.Request == "ERROR_CAPTCHA_UNSOLVABLE")
                    {
                        break;
                    }

                } while (twoCaptcha.Request == "CAPCHA_NOT_READY");
            }

            return twoCaptcha;
        }

        public async Task<string> ReportGoodAsync(string twoCaptchaKey, string idRequest)
        {
            return await url
                .AppendPathSegment("res.php")
                .SetQueryParam("key", twoCaptchaKey)
                .SetQueryParam("action", "reportgood")
                .SetQueryParam("json", "1")
                .SetQueryParam("id", idRequest)
                .GetStringAsync();
        }

        public async Task<string> ReportBadAsync(string twoCaptchaKey, string idRequest)
        {
            return await url
                .AppendPathSegment("res.php")
                .SetQueryParam("key", twoCaptchaKey)
                .SetQueryParam("action", "reportbad")
                .SetQueryParam("json", "1")
                .SetQueryParam("id", idRequest)
                .GetStringAsync();
        }
    }

}
