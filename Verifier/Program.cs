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
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;

namespace Verifier
{

    public class Program
    {
        public static EmailConfiguration EmailConfig { get; set; } = new EmailConfiguration();
        public static string EmailPostfix { get; set; }
        public static string ChromFullPath { get; set; }
        public static string ChromeDriverPath { get; set; }
        public static string ExtensionCrxPath { get; set; }
        public static int RunType { get; set; }
        public static string LinkFilePath { get; set; }

        public static int[] LocationArr { get; } = { 1, 4, 5, 7, 8, 10, 11 };

        private static readonly int DF_NAMELENGTH = 5;

        public static void Main(string[] args)
        {
            MappingConfig();
            MappingAppsetting();
            //if (!IsAdministrator())
            //{
            //    Console.WriteLine("Require Admin!");
            //    Console.WriteLine("\npress any key to exit the process...");
            //    Console.ReadKey();
            //    Environment.Exit(0);
            //}
            Console.WriteLine("Choose Run Type:\n1. Auto ref\n2. Get Verify Link\n3. Verify All Link");
            RunType = Convert.ToInt32(Console.ReadLine().Trim());
            switch (RunType)
            {
                case 1:
                    AutoRef();
                    Environment.Exit(0);
                    break;
                case 2:
                    Console.Write($"Verifier working on {DateTime.Now}");
                    var program2 = new Program();
                    program2.GetAllOldLink();
                    Environment.Exit(0);
                    break;
                case 3:
                    Console.Write($"Verifier working on {DateTime.Now}\n");
                    Console.WriteLine("Enter Proxy Api:");
                    string api = Console.ReadLine().Trim();
                    Console.WriteLine("Enter Verify Link Path:");
                    LinkFilePath = Console.ReadLine().Trim();
                    var program3 = new Program();
                    string[] linkArr = File.ReadAllLines(LinkFilePath);
                    foreach (var item in linkArr)
                    {
                        program3.VerifyLink(item, api);
                    }
                    Environment.Exit(0);
                    break;
                default:
                    break;
            }
        }

        private static void AutoRef()
        {
            Console.WriteLine("Enter Ref Link:");
            string refLink = Console.ReadLine().Trim();
            Console.WriteLine("Enter Proxy Api:");
            string apiKey = Console.ReadLine().Trim();
            Console.WriteLine("Enter Work Time:");
            int work = Convert.ToInt32(Console.ReadLine().Trim());
            Console.Write($"Verifier working on {DateTime.Now}");
            for (int i = 0; i < work; i++)
            {
                var program = new Program();
                var program2 = new Program();
                var inputModel = new CoinTeleGraphIM()
                {
                    Email = program.GetRandomEmail() + EmailPostfix,
                    LastName = program.GenerateName(DF_NAMELENGTH),
                    Wallet = program.GenerateWallet(),
                    FirstName = program2.GenerateName(DF_NAMELENGTH),
                    ApiKey = apiKey,
                    TargetUrl = refLink
                };
                program.InputEmail(inputModel);
                //program.Verify(email, pass, gmailUrl);
                //TrackAndReadEmail(program);
            }
        }

        private static int GetRandomlocation()
        {
            Random random = new Random();
            int locationId = random.Next(0, LocationArr.Length);
            return locationId;
        }

        private static void MappingAppsetting()
        {
            EmailPostfix = ConfigurationManager.AppSettings.Get("EmailPostfix");
            Console.WriteLine("Email Postfix: " + EmailPostfix);
            ChromFullPath = ConfigurationManager.AppSettings.Get("ChromFullPath");
            ExtensionCrxPath = ConfigurationManager.AppSettings.Get("ExtensionCrxPath");
            ChromeDriverPath = ConfigurationManager.AppSettings.Get("ChromeDriverPath");
        }

        private static void MappingConfig()
        {
            MappingEmailConfig();
        }

        public static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private static void MappingEmailConfig()
        {
            NameValueCollection emailConfigValue = (NameValueCollection)ConfigurationManager.GetSection("EmailConfiguration");
            foreach (string key in emailConfigValue.AllKeys)
            {
                switch (key)
                {
                    case nameof(EmailConfig.Email):
                        EmailConfig.Email = emailConfigValue[key];
                        Console.WriteLine("Receive message in: " + EmailConfig.Email);
                        break;
                    case nameof(EmailConfig.SmtpServer):
                        EmailConfig.SmtpServer = emailConfigValue[key];
                        break;
                    case nameof(EmailConfig.Password):
                        EmailConfig.Password = emailConfigValue[key];
                        break;
                    case nameof(EmailConfig.Port):
                        EmailConfig.Port = Convert.ToInt32(emailConfigValue[key]);
                        break;
                    default:
                        break;
                }
            }
        }

        private static void TrackAndReadEmail(Program program)
        {
            try
            {
                MailServer oServer = new MailServer(EmailConfig.SmtpServer,
                               EmailConfig.Email,
                               EmailConfig.Password,
                               ServerProtocol.Imap4)
                {
                    SSLConnection = true,
                    Port = EmailConfig.Port
                };
                MailClient oClient = new MailClient("TryIt");
                oClient.Connect(oServer);
                oClient.GetMailInfosParam.Reset();
                oClient.GetMailInfosParam.GetMailInfosOptions = GetMailInfosOptionType.NewOnly;
                MailInfo[] infos = oClient.GetMailInfos();
                Console.WriteLine("Total {0} unread email(s)\r\n", infos.Length);
                string param = "Verify your email address (Trial Version)";
                for (int j = 0; j < infos.Length; j++)
                {
                    MailInfo info = infos[j];
                    Console.WriteLine("Index: {0}; Size: {1}; UIDL: {2}",
                        info.Index, info.Size, info.UIDL);
                    Mail oMail = oClient.GetMail(info);
                    if (oMail.Subject.Contains(param))
                    {
                        var url = program.GetVerifyLink(oMail.TextBody);
                        // program.VerifyLink(url);

                    }
                    Console.WriteLine("To: {0}", oMail.To.ToString());
                    if (!info.Read)
                    {
                        oClient.MarkAsRead(info, true);
                    }
                }
                oClient.Quit();
                Console.WriteLine("Completed!");
            }
            catch (Exception ep)
            {
                Console.WriteLine(ep.Message);
            }
        }

        public string GetVerifyLink(string textBody)
        {
            string url = "";
            if (string.IsNullOrEmpty(textBody))
            {
                Console.WriteLine("Empty Mail TextBody");
                return url;
            }
            Char prefix = '<';
            Char postfix = '>';
            var newText = textBody.Split(prefix)[1];
            url = newText.Split(postfix).First();
            return url;
        }

        public static string[] GetAllVerifyLink()
        {
            string localPath = string.Format("{0}\\VerifyLinks", Directory.GetCurrentDirectory());
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }
            string filePath = string.Format("{0}\\VerifyLinks\\VerifyLinks.txt", Directory.GetCurrentDirectory());
            string[] rs = { "" };
            if (File.Exists(filePath))
            {
                rs = File.ReadAllLines(filePath);
            }
            return rs;
        }

        public static string CreateOrUpdateFile()
        {
            string localPath = string.Format("{0}\\VerifyLinks", Directory.GetCurrentDirectory());
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }
            string filePath = string.Format("{0}\\VerifyLinks\\VerifyLinks.txt", Directory.GetCurrentDirectory());
            if (!File.Exists(filePath))
            {
                FileStream fileStream = File.Create(filePath);
                fileStream.Close();
            }
            return filePath;
        }

        public void GetAllOldLink()
        {
            try
            {
                MailServer oServer = new MailServer(EmailConfig.SmtpServer,
                                EmailConfig.Email,
                                EmailConfig.Password,
                                ServerProtocol.Imap4)
                {
                    SSLConnection = true,
                    Port = EmailConfig.Port
                };
                MailClient oClient = new MailClient("TryIt");
                oClient.Connect(oServer);
                oClient.GetMailInfosParam.Reset();
                oClient.GetMailInfosParam.GetMailInfosOptions = GetMailInfosOptionType.NewOnly;
                //oClient.GetMailInfosParam.GetMailInfosOptions = GetMailInfosOptionType.OrderByDateTime;
                //oClient.GetMailInfosParam.DateRange.SINCE = System.DateTime.Now.AddHours(-3.5);
                //oClient.GetMailInfosParam.DateRange.BEFORE = System.DateTime.Now;
                MailInfo[] infos = oClient.GetMailInfos();
                string param = "Verify your email address (Trial Version)";
                Console.WriteLine("Total {0} all email(s)\r\n", infos.Length);
                int count = 0;
                for (int j = 0; j < infos.Length; j++)
                {
                    MailInfo info = infos[j];
                    Console.WriteLine("Index: {0}; Size: {1}; UIDL: {2}",
                        info.Index, info.Size, info.UIDL);
                    Mail oMail = oClient.GetMail(info);
                    if (oMail.Subject.Contains(param))
                    {
                        var url = GetVerifyLink(oMail.TextBody);
                        WriteLink(url);
                        count++;
                        // program.VerifyLink(url);

                    }
                    Console.WriteLine("To: {0}", oMail.To.ToString());
                    if (!info.Read)
                    {
                        oClient.MarkAsRead(info, true);
                    }
                }
                oClient.Quit();
                Console.WriteLine($"Completed! Total: {count} Links");
            }
            catch (Exception ep)
            {
                Console.WriteLine(ep.Message);
            }
        }

        public static string WriteLink(string url)
        {
            var filePath = CreateOrUpdateFile();
            using (StreamWriter writer = new StreamWriter(filePath, append: true))
            {
                writer.WriteLine(url, 0, url.Length);
                writer.Close();
            }
            return filePath;
        }

        public void VerifyLink(string url, string apiKey)
        {
            // var extensionUrl = "chrome-extension://ehjabdnjgcjapmdngchpedkjghjfanfn/popup.html";
            var httpsProxy = GetHttpsProxy(apiKey);
            ChromeOptions options = new ChromeOptions();
            options.AddArguments("--no-sandbox");
            options.AddArguments("--proxy-server=" + $"{httpsProxy[0]}" + ":" + $"{httpsProxy[1]}");
            var driver = UndetectedChromeDriver.Create(options: options, driverExecutablePath: @"C:\Users\neopi\Desktop\chromedriver.exe", browserExecutablePath: @"C:\Program Files\Google\Chrome\Application\chrome.exe");
            try
            {
                driver.GoToUrl(url);
                Thread.Sleep(10000);
                driver.Close();
                Thread.Sleep(20000);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                driver.Close();
            }

        }

        public static NewProxyModel GetProxyModel(string apiKey)
        {
            var locationId = GetRandomlocation();
            var proxy = TMAPIHelper.GetNewProxy(apiKey, apiKey, locationId);
            NewProxyModel proxyModel = JsonConvert.DeserializeObject<NewProxyModel>(proxy);
            return proxyModel;
        }

        public static string[] GetHttpsProxy(string apiKey)
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


        public string GetRandomEmail()
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

        public NewProxyModel GetNewProxy(string apiKey, int location)
        {
            string url = "https://tmproxy.com/api/proxy/get-new-proxy";
            var httpclient = new System.Net.Http.HttpClient();
            MultipartFormDataContent content = new MultipartFormDataContent();
            content.Add(new StringContent(apiKey), "api_key");
            content.Add(new StringContent(apiKey), "sign");
            content.Add(new StringContent(location.ToString()), "id_location");
            try
            {
                var response = httpclient.PostAsync(url, content).Result;
                var rsString = response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<NewProxyModel>(rsString.Result);
            }
            catch (Exception e)
            {
                return new NewProxyModel();
            }
        }

        public void InputEmail(CoinTeleGraphIM inputModel)
        {
            Console.WriteLine($"Working on Email: {inputModel.Email} | Name: {inputModel.FirstName} | {inputModel.LastName}\n Wallet: {inputModel.Wallet} \\n");
            var extensionUrl = "chrome-extension://pmdlifofgdjcolhfjjfkojibiimoahlc/popup.html";
            ChromeOptions options = new ChromeOptions();
            options.AddExtensions(ExtensionCrxPath);
            options.AddArgument("no-sandbox");
            //options.BinaryLocation = ChromFullPath;
            //IWebDriver driver = new ChromeDriver(ChromeDriverPath, options);
            IWebDriver driver = new ChromeDriver(options);
            try
            {
                driver.Navigate().GoToUrl(extensionUrl);
                Thread.Sleep(2500);

                IWebElement apiId = driver.FindElement(By.Id("API-input"));
                apiId.SendKeys(inputModel.ApiKey);
                Thread.Sleep(200);

                IWebElement conBtn = driver.FindElement(By.Id("connect-button"));
                conBtn.Click();
                Thread.Sleep(200);

                IWebElement changeBtn = driver.FindElement(By.CssSelector(".ui.fade.animated.button .hidden.content"));
                changeBtn.Click();
                Thread.Sleep(2000);

                driver.Navigate().GoToUrl(inputModel.TargetUrl);
                Thread.Sleep(2500);

                IWebElement ele = driver.FindElement(By.ClassName("intro-content-buttons-item-text"));
                ele.Click();
                Thread.Sleep(2500);

                IWebElement firstNameEle = driver.FindElement(By.Id("form_firstName"));
                firstNameEle.SendKeys(inputModel.FirstName);
                Thread.Sleep(200);

                IWebElement lastNameEle = driver.FindElement(By.Id("form_lastname"));
                lastNameEle.SendKeys(inputModel.LastName);
                Thread.Sleep(200);

                IWebElement emailEle = driver.FindElement(By.Id("form_email"));
                emailEle.SendKeys(inputModel.Email);
                Thread.Sleep(200);

                IWebElement ercWalletEle = driver.FindElement(By.Id("extraField_0"));
                ercWalletEle.SendKeys(inputModel.Wallet);
                Thread.Sleep(200);

                IWebElement submitBtn = driver.FindElement(By.Id("vl_popup_submit"));
                submitBtn.Click();
                Thread.Sleep(3000);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                driver.Close();
                Console.WriteLine("Slept!");
                Thread.Sleep(46300);
            }

        }

        public string GenerateWallet()
        {
            EthECKey key = EthECKey.GenerateKey();
            byte[] privateKey = key.GetPrivateKeyAsBytes();
            string address = key.GetPublicAddress();
            return address;
        }

        public string GenerateName(int len)
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
