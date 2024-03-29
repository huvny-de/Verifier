﻿using System;
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

namespace Verifier
{
    public class Program
    {
        public static EmailConfiguration EmailConfig { get; set; } = new EmailConfiguration();
        public static string EmailPostfix { get; set; }
        public static string ChromeLocationPath { get; set; }
        public static string ChromeDriverLocationPath { get; set; }
        public static int RunType { get; set; }
        public static string LinkFilePath { get; set; }
        public static string ApiKey { get; set; }
        public static int[] LocationArr { get; } = { 1, 4, 5, 7, 8, 10, 11 };
        private static readonly int DF_NAMELENGTH = 5;
        public static UndetectedChromeDriver _driver;

        public static void Main(string[] args)
        {
            Console.Write($"Verifier working on {DateTime.Now}");
            ReadSettingFile();
            GlobalAppInput();
            Console.WriteLine("Choose Run Type:\n1. Auto Ref And Verify\n2. Get Verify Link\n3. Verify All Link");
            RunType = Convert.ToInt32(Console.ReadLine().Trim());
            switch (RunType)
            {
                case 1:
                    AutoRefAndVerify();
                    Console.ReadKey();
                    Environment.Exit(0);
                    break;
                case 2:
                    GetAllOldLink();
                    Console.ReadKey();
                    Environment.Exit(0);
                    break;
                case 3:
                    InputMenu3();
                    string[] linkArr = File.ReadAllLines(LinkFilePath);
                    int count = 0;
                    for (int i = 0; i < linkArr.Length; i++)
                    {
                        count = i + 1;
                        Console.WriteLine($"Current Index: {i + 1}");
                        VerifyLink(linkArr[i]);
                    }
                    Console.WriteLine($"Job Completed! Total: {count} links. Time: {DateTime.Now}");
                    Console.ReadKey();
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Use your eyes!");
                    Console.ReadKey();
                    Environment.Exit(0);
                    break;
            }
        }

        private static void InputMenu3()
        {
            Console.WriteLine("Enter Verify Link Path:");
            LinkFilePath = Console.ReadLine().Trim();
        }

        private static void GlobalAppInput()
        {
            Console.WriteLine("Enter Proxy Api:");
            ApiKey = Console.ReadLine().Trim();
            Console.WriteLine("Enter Driver Path:");
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
            if (String.IsNullOrEmpty(customConfig.Email) | String.IsNullOrEmpty(customConfig.AppPassword) | String.IsNullOrEmpty(customConfig.EmailPostFix) | String.IsNullOrEmpty(customConfig.ChromeLocationPath))
            {
                Console.WriteLine($"Please Input AppSetting!\nPath: {Directory.GetCurrentDirectory().ToString()}");
                Console.ReadLine();
                return;
            }
            MappingConfig(customConfig);
            MappingAppsetting(customConfig);
        }

        public static void CreateSettingFile()
        {
            var customConfig = new CustomConfigModel();
            var json = JsonConvert.SerializeObject(customConfig);
            File.WriteAllText(Directory.GetCurrentDirectory() + @"\AppSetting.json", json);
        }

        private static void AutoRefAndVerify()
        {
            Console.WriteLine("Enter Ref Link:");
            string refLink = Console.ReadLine().Trim();

            Console.WriteLine("Enter Work Time:");
            int workTimes = Convert.ToInt32(Console.ReadLine().Trim());

            Console.Write($"Verifier working on {DateTime.Now}");
            for (int i = 0; i < workTimes; i++)
            {
                var inputModel = new CoinTeleGraphIM()
                {
                    Email = GetRandomEmail() + EmailPostfix,
                    LastName = GenerateName(DF_NAMELENGTH),
                    Wallet = GenerateWallet(),
                    FirstName = GenerateName(DF_NAMELENGTH),
                    TargetUrl = refLink
                };
                InputEmail(inputModel);
                TrackAndReadEmail();
            }
            Console.WriteLine($"AutoRefAndVerify Completed! Time: {DateTime.Now}");
        }

        private static int GetRandomlocation()
        {
            Random random = new Random();
            int locationId = random.Next(0, LocationArr.Length);
            return locationId;
        }

        private static void MappingAppsetting(CustomConfigModel customConfig)
        {
            EmailPostfix = customConfig.EmailPostFix;
            Console.WriteLine("Email Postfix: " + EmailPostfix);
            ChromeLocationPath = customConfig.ChromeLocationPath;
        }

        private static void MappingConfig(CustomConfigModel customConfig)
        {
            MappingEmailConfig(customConfig);
        }

        public static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private static void MappingEmailConfig(CustomConfigModel customConfig)
        {
            NameValueCollection emailConfigValue = (NameValueCollection)ConfigurationManager.GetSection("EmailConfiguration");
            foreach (string key in emailConfigValue.AllKeys)
            {
                switch (key)
                {
                    case nameof(EmailConfig.Email):
                        EmailConfig.Email = customConfig.Email;
                        Console.WriteLine("Receive message in: " + EmailConfig.Email);
                        break;
                    case nameof(EmailConfig.SmtpServer):
                        EmailConfig.SmtpServer = emailConfigValue[key];
                        break;
                    case nameof(EmailConfig.Password):
                        EmailConfig.Password = customConfig.AppPassword;
                        break;
                    case nameof(EmailConfig.Port):
                        EmailConfig.Port = Convert.ToInt32(emailConfigValue[key]);
                        break;
                    default:
                        break;
                }
            }
        }

        private static void TrackAndReadEmail()
        {
            try
            {
                string searchBody = "Click the button below to verify your email address and join the waitlist.";
                MailClient oClient = GetMailClient();

                oClient.GetMailInfosParam.Reset();
                oClient.GetMailInfosParam.GetMailInfosOptions = GetMailInfosOptionType.NewOnly;
                oClient.GetMailInfosParam.BodyContains = searchBody;
                var mailInfoArr = oClient.GetMailInfos();
                int waitCount = 0;
                while (mailInfoArr.Count() == 0)
                {
                    Console.WriteLine("Waiting for mail...");
                    Thread.Sleep(1000);
                    waitCount++;
                    if (waitCount > 28)
                    {
                        Console.WriteLine("Wait for email time out... Re-starting...");
                        return;
                    }
                    mailInfoArr = oClient.GetMailInfos();
                }
                MailInfo info = mailInfoArr[0];
                Mail oMail = oClient.GetMail(info);
                var url = GetVerifyLink(oMail.TextBody);
                VerifyLinkOnCurrentSession(url, ApiKey);
                if (!info.Read)
                {
                    oClient.MarkAsRead(info, true);
                }
                oClient.Quit();
            }
            catch (Exception ep)
            {
                Console.WriteLine(ep.Message);
            }
        }

        public static void MarkAsReadAllMailInfo(MailInfo[] mailInfos, MailClient mailClient)
        {
            if (mailInfos.Count() == 0)
            {
                return;
            }
            foreach (var mailInfo in mailInfos)
            {
                MarkAsReadMailInfo(mailInfo, mailClient);
            }
        }

        public static void MarkAsReadMailInfo(MailInfo mailInfo, MailClient mailClient)
        {
            if (!mailInfo.Read)
            {
                mailClient.MarkAsRead(mailInfo, true);
            }
        }

        private static MailClient GetMailClient()
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
            return oClient;
        }

        public static string GetVerifyLink(string textBody)
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

        public static void GetAllOldLink()
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
                Console.WriteLine($"Completed! Total: {count} Links. Time: {DateTime.Now}");
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

        public static void VerifyLink(string url)
        {
            try
            {
                // var extensionUrl = "chrome-extension://ehjabdnjgcjapmdngchpedkjghjfanfn/popup.html";
                var httpsProxy = GetHttpsProxy(ApiKey);
                ChromeOptions options = new ChromeOptions();
                options.AddArguments("--proxy-server=" + $"{httpsProxy[0]}" + ":" + $"{httpsProxy[1]}");
                _driver = UndetectedChromeDriver.Create(options: options, driverExecutablePath: ChromeDriverLocationPath, browserExecutablePath: ChromeLocationPath);
                _driver.GoToUrl(url);
                Thread.Sleep(45000);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                _driver.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.Sleep(5000);
            }
        }

        public static void VerifyLinkOnCurrentSession(string url, string apiKey)
        {
            try
            {
                _driver.GoToUrl(url);
                Thread.Sleep(28000);
                _driver.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (_driver != null)
                {
                    _driver.Dispose();
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
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

        public static void InputEmail(CoinTeleGraphIM inputModel)
        {
            try
            {
                Console.WriteLine($"Working on Email: {inputModel.Email} | Name: {inputModel.FirstName} | {inputModel.LastName}\n Wallet: {inputModel.Wallet} \\n");
                var httpsProxy = GetHttpsProxy(ApiKey);
                ChromeOptions options = new ChromeOptions();
                options.AddArguments("--proxy-server=" + $"{httpsProxy[0]}" + ":" + $"{httpsProxy[1]}");
                _driver = UndetectedChromeDriver.Create(options: options, driverExecutablePath: ChromeDriverLocationPath, browserExecutablePath: ChromeLocationPath);

                _driver.Navigate().GoToUrl(inputModel.TargetUrl);
                Thread.Sleep(2500);

                IWebElement ele = _driver.FindElementWait(By.ClassName("intro-content-buttons-item-text"), 10);
                ele.Click();
                IWebElement firstNameEle = _driver.FindElementWait(By.Id("form_firstName"), 10);
                firstNameEle.SendKeys(inputModel.FirstName);
                Thread.Sleep(200);

                IWebElement lastNameEle = _driver.FindElement(By.Id("form_lastname"));
                lastNameEle.SendKeys(inputModel.LastName);
                Thread.Sleep(200);

                IWebElement emailEle = _driver.FindElement(By.Id("form_email"));
                emailEle.SendKeys(inputModel.Email);
                Thread.Sleep(200);

                IWebElement ercWalletEle = _driver.FindElement(By.Id("extraField_0"));
                ercWalletEle.SendKeys(inputModel.Wallet);
                Thread.Sleep(200);

                IWebElement submitBtn = _driver.FindElement(By.Id("vl_popup_submit"));
                submitBtn.Click();
                Thread.Sleep(2000);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (_driver != null)
                {
                    _driver.Dispose();
                }
            }
        }

        public static string GenerateWallet()
        {
            EthECKey key = EthECKey.GenerateKey();
            byte[] privateKey = key.GetPrivateKeyAsBytes();
            string address = key.GetPublicAddress();
            return address;
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
