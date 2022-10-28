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
        public static int TotalRefFailed { get; set; } = 0;
        public static int TotalRefSuccess { get; set; } = 0;
        public static int TotalVerFailed { get; set; } = 0;
        public static int TotalVerSuccess { get; set; } = 0;
        public static int WaitLoadVerifyUrl { get; set; } = 0;

        public static string ApiKey { get; set; }
        public static int[] LocationArr { get; } = { 1, 4, 5, 7, 8, 10, 11 };
        private static readonly int DF_NAMELENGTH = 5;
        public static UndetectedChromeDriver _undetectedDriver;
        public static IWebDriver _webDriver;


        public static void Main(string[] args)
        {
            Console.Write($"Verifier working on {DateTime.Now}\n");
            var startTime = DateTime.Now;
            ReadSettingFile();
            GlobalAppInput();
            Console.WriteLine("Choose Run Type:\n1. Auto Ref And Verify\n2. Get Verify Link\n3. Verify All Link\n4. Auto Ref\n5. Verify Link");
            RunType = Convert.ToInt32(Console.ReadLine().Trim());
            switch (RunType)
            {
                case 1:
                    AutoRefAndVerify();
                    LogRunTime(startTime);
                    Console.ReadKey();
                    Environment.Exit(0);
                    break;
                case 2:
                    GetAllOldLink();
                    Console.WriteLine("Enter number of file you want: ");
                    int fileCount = Convert.ToInt32(Console.ReadLine().Trim());
                    SplitLinkFile(fileCount);
                    LogRunTime(startTime);
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
                    LogRunTime(startTime);
                    Console.ReadKey();
                    Environment.Exit(0);
                    break;
                case 4:
                    AutoRef();
                    LogRunTime(startTime);
                    Console.ReadKey();
                    Environment.Exit(0);
                    break;
                case 5:
                    Console.WriteLine("Enter VerifyLink Path: ");
                    string path = Console.ReadLine().Trim();
                    string[] allLinks = File.ReadAllLines(path);
                    Console.WriteLine($"Total {allLinks.Length} links.");
                    foreach (var url in allLinks)
                    {
                        VerifyLink(url);
                    }
                    LogWithColor($"Auto Verify Completed! Time: {DateTime.Now}\nTotal Verify Succeed: {TotalVerSuccess}", ConsoleColor.DarkGreen);
                    LogWithColor($"Total Verify Failed: {TotalVerFailed}", ConsoleColor.DarkRed);
                    LogRunTime(startTime);
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

        private static void SplitLinkFile(int fileCount)
        {
            var filePath = CreateOrUpdateFile();
            var totalLinks = File.ReadAllLines(filePath).ToList();
            if (totalLinks.Count < fileCount)
            {
                LogWithColor($"Number of file larger than total links.", ConsoleColor.DarkRed);
            }
            var linkPerFile = totalLinks.Count / fileCount;
            for (int i = 0; i < fileCount; i++)
            {
                string linkFilePath = CreateOrUpdateFile("VerifyLinks" + (i + 1));
                string[] content = totalLinks.GetRange(i * linkPerFile, linkPerFile).ToArray();
                if (i == fileCount - 1)
                {
                    content = totalLinks.Skip(i * linkPerFile).ToArray();
                }
                Console.WriteLine($"Content Count: {content.Length}");
                File.WriteAllLines(linkFilePath, content);
            }
        }

        private static void LogRunTime(DateTime startTime)
        {
            var runTimes = DateTime.Now.Subtract(startTime);
            LogWithColor($"Total Run Time: {runTimes.Days}d {runTimes.Hours}hrs {runTimes.Minutes}m {runTimes.Seconds}s", ConsoleColor.DarkGreen);
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

            Console.WriteLine("Enter Time Wait for browser load verify url (seconds):");
            WaitLoadVerifyUrl = Convert.ToInt32(Console.ReadLine().Trim());

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

        private static void AutoRef()
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
                InputEmailOriginDriver(inputModel);
            }
            LogWithColor($"Auto Ref Completed! Time: {DateTime.Now}\nTotal Ref Succeed: {TotalRefSuccess}", ConsoleColor.DarkGreen);
            LogWithColor($"Total Ref Failed: {TotalRefFailed}", ConsoleColor.DarkRed);
        }

        public static void LogWithColor(string value, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(value);
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
            LogWithColor(url, ConsoleColor.DarkBlue);
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

        public static string CreateOrUpdateFile(string fileName = "VerifyLinks")
        {
            string localPath = string.Format("{0}\\{1}", Directory.GetCurrentDirectory(), fileName);
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }
            string filePath = string.Format("{0}\\VerifyLinks\\{1}.txt", Directory.GetCurrentDirectory(), fileName);
            if (!File.Exists(filePath))
            {
                FileStream fileStream = File.Create(filePath);
                fileStream.Close();
            }
            Console.WriteLine($"Path: {filePath}");
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
                var httpsProxy = GetHttpsProxy(ApiKey);
                ChromeOptions options = new ChromeOptions();
                options.AddArguments("--proxy-server=" + $"{httpsProxy[0]}" + ":" + $"{httpsProxy[1]}");
                _undetectedDriver = UndetectedChromeDriver.Create(options: options, driverExecutablePath: ChromeDriverLocationPath, browserExecutablePath: ChromeLocationPath);
                _undetectedDriver.GoToUrl(url);
                Thread.Sleep(15000);
                if (_undetectedDriver.Url.Contains("cointelegraph"))
                {
                    TotalVerSuccess += 1;
                    LogWithColor($"Verify Succeed!\nTotal Verify Succeed{TotalVerSuccess}", ConsoleColor.DarkGreen);
                }
                else
                {
                    TotalVerFailed += 1;
                    LogWithColor($"Verify Failed!\nTotal Verify Failed{TotalVerFailed}", ConsoleColor.DarkRed);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                _undetectedDriver.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public static void VerifyLinkOnCurrentSession(string url, string apiKey)
        {
            try
            {
                _undetectedDriver.GoToUrl(url);
                Thread.Sleep(WaitLoadVerifyUrl * 1000);
                _undetectedDriver.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (_undetectedDriver != null)
                {
                    _undetectedDriver.Dispose();
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
                _undetectedDriver = UndetectedChromeDriver.Create(options: options, driverExecutablePath: ChromeDriverLocationPath, browserExecutablePath: ChromeLocationPath);
                _undetectedDriver.GoToUrl(inputModel.TargetUrl);
                Thread.Sleep(5200);

                IWebElement ele = _undetectedDriver.FindElement(By.ClassName("intro-content-buttons-item-text"));
                ele.Click();
                Thread.Sleep(3000);

                IWebElement firstNameEle = _undetectedDriver.FindElement(By.Id("form_firstName"));
                firstNameEle.SendKeys(inputModel.FirstName);
                Thread.Sleep(200);

                IWebElement lastNameEle = _undetectedDriver.FindElement(By.Id("form_lastname"));
                lastNameEle.SendKeys(inputModel.LastName);
                Thread.Sleep(200);

                IWebElement emailEle = _undetectedDriver.FindElement(By.Id("form_email"));
                emailEle.SendKeys(inputModel.Email);
                Thread.Sleep(200);

                IWebElement ercWalletEle = _undetectedDriver.FindElement(By.Id("extraField_0"));
                ercWalletEle.SendKeys(inputModel.Wallet);
                Thread.Sleep(200);

                IWebElement submitBtn = _undetectedDriver.FindElement(By.Id("vl_popup_submit"));
                submitBtn.Click();
                Thread.Sleep(2000);
                TotalRefSuccess += 1;
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

        public static void InputEmailOriginDriver(CoinTeleGraphIM inputModel)
        {
            try
            {
                Console.WriteLine($"Working on Email: {inputModel.Email} | Name: {inputModel.FirstName} | {inputModel.LastName}\n Wallet: {inputModel.Wallet} \\n");
                var httpsProxy = GetHttpsProxy(ApiKey);
                ChromeOptions options = new ChromeOptions();
                options.AddArguments("--proxy-server=" + $"{httpsProxy[0]}" + ":" + $"{httpsProxy[1]}");
                options.BinaryLocation = ChromeLocationPath;
                _webDriver = new ChromeDriver(ChromeDriverLocationPath, options);
                _webDriver.Navigate().GoToUrl(inputModel.TargetUrl);
                Thread.Sleep(5200);

                IWebElement ele = _webDriver.FindElement(By.ClassName("intro-content-buttons-item-text"));
                ele.Click();
                Thread.Sleep(3000);

                IWebElement firstNameEle = _webDriver.FindElement(By.Id("form_firstName"));
                firstNameEle.SendKeys(inputModel.FirstName);
                Thread.Sleep(200);

                IWebElement lastNameEle = _webDriver.FindElement(By.Id("form_lastname"));
                lastNameEle.SendKeys(inputModel.LastName);
                Thread.Sleep(200);

                IWebElement emailEle = _webDriver.FindElement(By.Id("form_email"));
                emailEle.SendKeys(inputModel.Email);
                Thread.Sleep(200);

                IWebElement ercWalletEle = _webDriver.FindElement(By.Id("extraField_0"));
                ercWalletEle.SendKeys(inputModel.Wallet);
                Thread.Sleep(200);

                IWebElement submitBtn = _webDriver.FindElement(By.Id("vl_popup_submit"));
                submitBtn.Click();
                Thread.Sleep(2000);
                TotalRefSuccess += 1;
                _webDriver.Dispose();
            }
            catch (Exception e)
            {
                LogWithColor(e.Message, ConsoleColor.DarkRed);
                TotalRefFailed += 1;
                LogWithColor($"Total Ref Failed: {TotalRefFailed}", ConsoleColor.DarkRed);
                if (_webDriver != null)
                {
                    _webDriver.Dispose();
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
