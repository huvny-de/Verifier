using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Nethereum.Signer;
using EAGetMail;
using System.Linq;
using System.IO;
using System.Configuration;
using System.Collections.Specialized;
using Verifier.Models;
using System.Security.Principal;
using Verifier.InputModels;
using SeleniumUndetectedChromeDriver;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Verifier.Extensions;
using System.Diagnostics;
using System.Text;

namespace Verifier
{
    public class Program
    {
        public static string ChromeLocationPath { get; set; }
        public static string ChromeDriverLocationPath { get; set; }
        public static string VaultPassword { get; set; }
        public static string ApiKey { get; set; }

        public static int[] LocationArr { get; } = { 1, 4, 5, 7, 8, 10, 11 };

        public static int TotalRefSucceed { get; set; } = 0;
        public static int TotalRefFailed { get; set; } = 0;
        public static string HostWallet { get; set; }


        public static readonly string FailedLinksFileName = "LinksFailed" + GetRandomEmail() + GenerateName(8);

        public static string EmailPostfix { get; set; }
        public static int DF_NameLength { get; } = 5;

        public static UndetectedChromeDriver _undetectedDriver;
        public static IWebDriver _webDriver;

        public static async Task Main(string[] args)
        {
            Console.Write($"Verifier working on {DateTime.Now}\n");
            var startTime = DateTime.Now;
            ReadSettingFile();
            GlobalAppInput();
            Menu(startTime);
            Environment.Exit(0);
        }

        private static void Menu(DateTime startTime)
        {
            int runType = 0;
            do
            {
                Console.WriteLine("Choose Run Type:\n" +
                    "1. Auto Ref");
                runType = Convert.ToInt32(Console.ReadLine().Trim());
                switch (runType)
                {
                    case 1:
                        AutoRef();
                        LogRunTime(startTime);
                        break;
                }
            } while (runType != 3);
        }

        private static void LogRunTime(DateTime startTime)
        {
            var runTimes = DateTime.Now.Subtract(startTime);
            LogWithColor($"Total Run Time: {runTimes.Days}d {runTimes.Hours}hrs {runTimes.Minutes}m {runTimes.Seconds}s", ConsoleColor.DarkGreen);
        }

        private static void GlobalAppInput()
        {
            Console.WriteLine("Enter Proxy Api:");
            ApiKey = Console.ReadLine().Trim();
            Console.WriteLine("Enter Chrome Driver Path:");
            ChromeDriverLocationPath = Console.ReadLine().Trim();
        }

        public static void ReadSettingFile()
        {
            string rs = "";
            try
            {
                rs = File.ReadAllText(Directory.GetCurrentDirectory() + @"\AppSetting.json");
            }
            catch (Exception)
            {
            }
            if (string.IsNullOrEmpty(rs))
            {
                Console.WriteLine($"Please Input AppSetting!\nPath: {Directory.GetCurrentDirectory().ToString()}");
                CreateSettingFile();
                Console.ReadLine();
                return;
            }
            var customConfig = JsonConvert.DeserializeObject<CustomConfigModel>(rs);
            if (String.IsNullOrEmpty(customConfig.VaultPassword) | String.IsNullOrEmpty(customConfig.Wallet) | String.IsNullOrEmpty(customConfig.EmailPostFix) | String.IsNullOrEmpty(customConfig.ChromeLocationPath))
            {
                Console.WriteLine($"Please Input AppSetting!\nPath: {Directory.GetCurrentDirectory()}");
                Console.ReadLine();
                return;
            }
            MappingAppsetting(customConfig);
        }

        public static void CreateSettingFile()
        {
            var customConfig = new CustomConfigModel();
            var json = JsonConvert.SerializeObject(customConfig);
            File.WriteAllText(Directory.GetCurrentDirectory() + @"\AppSetting.json", json);
        }

        private static void AutoRef()
        {
            Console.WriteLine("Enter Work Time:");
            int workTimes = Convert.ToInt32(Console.ReadLine().Trim());
            for (int i = 0; i < workTimes; i++)
            {
                var inputModel = new RedditInputModel()
                {
                    Email = GetRandomEmail() + EmailPostfix,
                    Password = VaultPassword,
                    Username = GetRandomEmail() + GenerateName(DF_NameLength)
                };
                InputEmailOriginDriver(inputModel);
            }
        }

        public static NewProxyModel GetCurrentProxy(string apiKey)
        {
            var proxy = TMAPIHelper.GetCurrentProxy(apiKey);
            NewProxyModel proxyModel = JsonConvert.DeserializeObject<NewProxyModel>(proxy);
            return proxyModel;
        }

        public static string[] GetHttpsProxy(string apiKey)
        {
            string[] httpsProxy;
            NewProxyModel proxyModel = GetCurrentProxy(apiKey);
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

        public static NewProxyModel GetProxyModel(string apiKey)
        {
            var locationId = GetRandomlocation();
            var proxy = TMAPIHelper.GetNewProxy(apiKey, apiKey, locationId);
            NewProxyModel proxyModel = JsonConvert.DeserializeObject<NewProxyModel>(proxy);
            return proxyModel;
        }

        private static int GetRandomlocation()
        {
            Random random = new Random();
            int locationId = random.Next(0, LocationArr.Length);
            return locationId;
        }

        public static void InputEmailOriginDriver(RedditInputModel inputModel)
        {
            var registerUrl = "https://www.reddit.com/register/";
            try
            {
                Console.WriteLine($"Working on Email: {inputModel.Email} | Username: {inputModel.Username}\n");
                var httpsProxy = GetHttpsProxy(ApiKey);
                ChromeOptions options = new ChromeOptions();
                options.AddArguments("--proxy-server=" + $"{httpsProxy[0]}" + ":" + $"{httpsProxy[1]}");
                _undetectedDriver = UndetectedChromeDriver.Create(options: options, driverExecutablePath: ChromeDriverLocationPath + @"\chromedriver.exe", browserExecutablePath: ChromeLocationPath);
                _undetectedDriver.Navigate().GoToUrl(registerUrl);
                Thread.Sleep(3000);

                IWebElement firstNameEle = GetWebElementUntilSuccess(By.Id("regEmail"));
                firstNameEle.SendKeys(inputModel.Email);
                Thread.Sleep(200);

                IWebElement btnContinue = GetWebElementUntilSuccess(By.CssSelector(".AnimatedForm__submitButton.m-full-width"));
                ClickUntilSuccess(btnContinue);
                Thread.Sleep(1000);

                var suggestUsername = GetWebElementsUntilSuccess(By.CssSelector(".Onboarding__usernameSuggestion"));
                Thread.Sleep(200);
                IWebElement regUsername = GetWebElementUntilSuccess(By.Id("regUsername"));
                var takeUsername = suggestUsername[0].Text;
                regUsername.SendKeys(takeUsername);

                IWebElement regPassword = GetWebElementUntilSuccess(By.Id("regPassword"));
                regPassword.SendKeys(VaultPassword);

                LogWithColor("Press enter after verified captcha!", ConsoleColor.DarkGreen);
                var key = Console.ReadKey();
                while (key.KeyChar != (char)13)
                {
                    LogWithColor("Press enter after verified captcha!", ConsoleColor.DarkGreen);
                    key = Console.ReadKey();
                }
                var submitBtn = GetWebElementsUntilSuccess(By.CssSelector(".AnimatedForm__submitButton"));
                submitBtn[1].Click();
                Thread.Sleep(2000);

                IWebElement skipBtn = GetWebElementUntilSuccess(By.CssSelector("._22ChQI9alXTuxk7yqwRt8l"));
                skipBtn.Click();
                Thread.Sleep(1000);

                List<IWebElement> emojiBtn = GetWebElementsUntilSuccess(By.CssSelector("._3miLvWoAksppOIKDbHmCpH ._3oCL2oMbe3H81mue3bR1CQ"));
                while (emojiBtn.Count < 3)
                {
                    emojiBtn = GetWebElementsUntilSuccess(By.CssSelector("._3miLvWoAksppOIKDbHmCpH ._3oCL2oMbe3H81mue3bR1CQ"));
                }
                ClickUntilSuccess(emojiBtn[0]);
                ClickUntilSuccess(emojiBtn[1]);
                ClickUntilSuccess(emojiBtn[2]);


                IWebElement continueBtn = GetWebElementUntilSuccess(By.CssSelector(".dK60vCQAai2JPR7mVZ4ir"));
                ClickUntilSuccess(continueBtn);
                Thread.Sleep(1000);


                var fistJoin = GetWebElementsUntilSuccess(By.CssSelector("._2h_rraB53rhUmsB6cnedRY")).ToList();
                ClickUntilSuccess(fistJoin[1]);
                Thread.Sleep(100);
                var continueBtn3 = GetWebElementUntilSuccess(By.CssSelector(".dK60vCQAai2JPR7mVZ4ir"));
                ClickUntilSuccess(continueBtn3);
                Thread.Sleep(5000);

                IWebElement continue2 = GetWebElementUntilSuccess(By.CssSelector(".dK60vCQAai2JPR7mVZ4ir"));
                ClickUntilSuccess(continue2);
                Thread.Sleep(3000);

                List<IWebElement> gotIt = GetWebElementsUntilSuccess(By.CssSelector("._34mIRHpFtnJ0Sk97S2Z3D9"));
                while (gotIt.Count != 4)
                {
                    gotIt = GetWebElementsUntilSuccess(By.CssSelector("._34mIRHpFtnJ0Sk97S2Z3D9"));
                }
                ClickUntilSuccess(gotIt[3]);
                Thread.Sleep(1000);

                List<IWebElement> gotIt2 = GetWebElementsUntilSuccess(By.CssSelector("._2iuoyPiKHN3kfOoeIQalDT"));
                while (gotIt2.Count != 37)
                {
                    gotIt2 = GetWebElementsUntilSuccess(By.CssSelector("._2iuoyPiKHN3kfOoeIQalDT"));
                }
                ClickUntilSuccess(gotIt2[36]);
                Thread.Sleep(1000);

                //IWebElement xBtn = GetWebElementUntilSuccess(By.CssSelector("._1DK52RbaamLOWw5UPaht_S _199HcTqT2ANvw-1B0onPUa _1acwN_tUhJ8w-n7oCp-Aw3"));
                //xBtn.Click();

                IWebElement userDropdown = GetWebElementUntilSuccess(By.Id("USER_DROPDOWN_ID"));
                ClickUntilSuccess(userDropdown);
                Thread.Sleep(2000);

                IWebElement createAvtBtn = GetWebElementUntilSuccess(By.CssSelector("._6opQAE7SUXi-Fy7P3vItL._6opQAE7SUXi-Fy7P3vItL"));
                ClickUntilSuccess(createAvtBtn);
                Thread.Sleep(4000);


                var next = GetWebElementsUntilSuccess(By.CssSelector("._button_3pioz_15")).ToList();
                while (next.Count != 6)
                {
                    next = GetWebElementsUntilSuccess(By.CssSelector("._button_3pioz_15")).ToList();
                }
                for (int i = 0; i < 14; i++)
                {
                    ClickUntilSuccess(next[0]);
                    Thread.Sleep(200);
                }

                var randomFC = GetRandomFC();
                var listFC = GetWebElementsUntilSuccess(By.CssSelector("._outfitImage_1trdi_30")).ToList();
                while (listFC.Count != 33)
                {
                    listFC = GetWebElementsUntilSuccess(By.CssSelector("._outfitImage_1trdi_30")).ToList();
                }
                ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript($"document.querySelectorAll('._outfitImage_1trdi_30')[{randomFC}].click()");
                Thread.Sleep(2000);

                var choseItemBtn = GetWebElementUntilSuccess(By.CssSelector("._button_koles_42"));
                ClickUntilSuccess(choseItemBtn);
                Thread.Sleep(500);

                IWebElement vaultPass = GetWebElementUntilSuccess(By.Id("passwordField"));
                vaultPass.SendKeys(VaultPassword);
                Thread.Sleep(200);

                IWebElement vaultPassConfirm = GetWebElementUntilSuccess(By.Id("confirmationPasswordField"));
                vaultPassConfirm.SendKeys(VaultPassword);
                Thread.Sleep(200);

                var secureBtn = GetWebElementUntilSuccess(By.CssSelector("._button_1wfm6_149._button_1wfm6_149"));
                ClickUntilSuccess(secureBtn);
                Thread.Sleep(3000);

                var continue4 = GetWebElementUntilSuccess(By.CssSelector("._button_koles_42"));
                ClickUntilSuccess(continue4);
                Thread.Sleep(2000);

                var fullItem = GetWebElementsUntilSuccess(By.CssSelector("._ctaButton_y0x52_1"));
                while (fullItem.Count != 2)
                {
                    fullItem = GetWebElementsUntilSuccess(By.CssSelector("._ctaButton_y0x52_1"));
                }
                ClickUntilSuccess(fullItem[0]);
                Thread.Sleep(500);

                ClickUntilSuccess(fullItem[1]);
                Thread.Sleep(500);

                //var viewDetail = GetWebElementUntilSuccess(By.CssSelector("._sheet_eet7b_1._sectionDescription_eet7b_47._clickable_eet7b_70"));
                //viewDetail.Click();
                //Thread.Sleep(500);

                //var transfer = GetWebElementsUntilSuccess(By.CssSelector("._productDetails_7kbcu_53._defaultButtonsContainer_7kbcu_182._ctaButton_7kbcu_140"));
                //transfer[0].Click();
                //Thread.Sleep(500);


                //IWebElement toField = GetWebElementUntilSuccess(By.Id("toField"));
                //toField.SendKeys(HostWallet);
                //Thread.Sleep(200);

                //IWebElement passwordField = GetWebElementUntilSuccess(By.Id("passwordField"));
                //passwordField.SendKeys(VaultPassword);
                //Thread.Sleep(200);

                //var transfer2 = GetWebElementUntilSuccess(By.CssSelector("._blueTheme_koles_103"));
                //transfer2.Click();

                string info = $"{takeUsername}|{inputModel.Email}";
                string fileName = "RegReddit";
                SaveUrl(info, fileName);

                TotalRefSucceed += 1;
                _undetectedDriver.Dispose();
            }
            catch (Exception e)
            {
                LogWithColor(e.Message, ConsoleColor.DarkRed);
                TotalRefFailed += 1;
                LogWithColor($"Total Ref Failed: {TotalRefFailed}", ConsoleColor.DarkRed);
                if (_undetectedDriver != null)
                {
                    _undetectedDriver.Dispose();
                }
            }
        }

        private static bool TryClickButton(IWebElement ele)
        {
            try
            {
                ele.Click();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ClickUntilSuccess(IWebElement ele)
        {
            var rs = TryClickButton(ele);
            while (rs == false)
            {
                rs = TryClickButton(ele);
            }
        }

        private static IWebElement GetXButton()
        {
            try
            {
                return GetWebElementUntilSuccess(By.CssSelector("._2lPBwpVCWIEI428aTPAwZx"));

            }
            catch (Exception)
            {
                return null;
            }
        }

        private static IWebElement GetWebElement(By by)
        {
            try
            {
                return _undetectedDriver.FindElement(by);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static IWebElement GetWebElementUntilSuccess(By by)
        {
            var ele = GetWebElement(by);
            while (ele == null)
            {
                Thread.Sleep(1000);
                ele = GetWebElement(by);
            }
            return ele;
        }

        private static List<IWebElement> GetWebElementsUntilSuccess(By by)
        {
            var ele = GetWebElements(by);
            while (ele == null)
            {
                Thread.Sleep(1000);
                ele = GetWebElements(by);
            }
            return ele;
        }


        private static List<IWebElement> GetWebElements(By by)
        {
            try
            {
                return _undetectedDriver.FindElements(by).ToList();

            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void LogWithColor(string value, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(value);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static int GetRandomFC()
        {
            int[] randomFB = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 };
            Random random = new Random();
            int locationId = random.Next(0, randomFB.Length);
            return locationId;
        }

        private static void MappingAppsetting(CustomConfigModel customConfig)
        {
            EmailPostfix = customConfig.EmailPostFix;
            Console.WriteLine("Email Postfix: " + EmailPostfix);
            VaultPassword = customConfig.VaultPassword;
            Console.WriteLine("VaultPassword: " + VaultPassword);
            HostWallet = customConfig.Wallet;
            Console.WriteLine("HostWallet: " + HostWallet);
            ChromeLocationPath = customConfig.ChromeLocationPath;
        }

        public static void DeleteFile(string fileName = "VerifyLinks")
        {
            string localPath = string.Format("{0}\\{1}", Directory.GetCurrentDirectory(), fileName);
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }
            string filePath = string.Format("{0}\\VerifyLinks\\{1}.txt", Directory.GetCurrentDirectory(), fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public static string CreateOrUpdateFile(string fileName = "VerifyLinks", bool shoudNew = false)
        {
            string localPath = string.Format("{0}\\{1}", Directory.GetCurrentDirectory(), fileName);
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }
            string filePath = string.Format("{0}\\VerifyLinks\\{1}.txt", Directory.GetCurrentDirectory(), fileName);
            if (File.Exists(filePath) && shoudNew)
            {
                File.Delete(filePath);
            }
            if (!File.Exists(filePath))
            {
                FileStream fileStream = File.Create(filePath);
                fileStream.Close();
            }
            Console.WriteLine($"Path: {filePath}");
            return filePath;
        }

        public static string SaveUrl(string url, string fileName)
        {
            var filePath = CreateOrUpdateFile(fileName);
            using (StreamWriter writer = new StreamWriter(filePath, append: true))
            {
                writer.WriteLine(url, 0, url.Length);
                writer.Close();
            }
            return filePath;
        }

        public static bool Confirm(string title)
        {
            ConsoleKey response;
            do
            {
                Console.Write($"{title} [y/n] ");
                response = Console.ReadKey(false).Key;
                if (response != ConsoleKey.Enter)
                {
                    Console.WriteLine();
                }
            } while (response != ConsoleKey.Y && response != ConsoleKey.N);

            return (response == ConsoleKey.Y);
        }

        public static string GetRandomEmail()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[8];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }
            var finalString = new String(stringChars);
            return finalString;
        }

        public static string GenerateWallet()
        {
            EthECKey key = EthECKey.GenerateKey();
            string address = key.GetPublicAddress();
            return address;
        }

        public static List<string> GenerateWalletAndPrivateKeyAsync(int total)
        {
            List<string> value = new List<string>();
            for (int i = 0; i < total; i++)
            {

                EthECKey key = EthECKey.GenerateKey();
                string privateKey = key.GetPrivateKey();
                string address = key.GetPublicAddress();
                value.Add(address + ":" + privateKey);
            }
            return value;
        }

        public static (string, string) GenerateWalletAndPrivateKeyAsync()
        {
            EthECKey key = EthECKey.GenerateKey();
            string privateKey = key.GetPrivateKey();
            string address = key.GetPublicAddress();
            return (privateKey, address);
        }

        public static string GenerateName(int len)
        {
            Random r = new Random();
            string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
            string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
            string Name = "";
            Name += consonants[r.Next(consonants.Length)].ToUpper();
            Name += vowels[r.Next(vowels.Length)];
            int b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
            while (b < len)
            {
                Name += consonants[r.Next(consonants.Length)];
                b++;
                Name += vowels[r.Next(vowels.Length)];
                b++;
            }
            return Name;
        }
    }
}
