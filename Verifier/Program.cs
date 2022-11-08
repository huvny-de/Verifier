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
        public static string RefSource = "&refSource=";
        public static string[] RefSouceKey { get; } = { "copy", "twitter", "facebook", "reddit", "email" };
        public static string RefPrefix = "https://cointelegraph.com/historical?referral=";
        private static readonly int DF_NAMELENGTH = 5;
        public static readonly string FailedLinksFileName = "LinksFailed" + GetRandomEmail() + GenerateName(8);
        public static readonly string VerifyLinksFileName = "VerifyLinks";
        public static UndetectedChromeDriver _undetectedDriver;
        public static IWebDriver _webDriver;

        public static async Task Main(string[] args)
        {
            Console.Write($"Verifier working on {DateTime.Now}\n");
            var startTime = DateTime.Now;
            ReadSettingFile();
            GlobalAppInput();
            await Menu(startTime);
            Environment.Exit(0);
        }

        private static Task Menu(DateTime startTime)
        {
            do
            {
                Console.WriteLine("Choose Run Type:\n" +
                    "1. Auto Ref And Verify\n" +
                    "2. Get Verify Link\n" +
                    "3. Verify All Link\n" +
                    "4. Auto Register Main Account\n" +
                    "5. Verify Link\n" +
                    "6. Auto Ref Multiples Link\n" +
                    "7. Exit\n" +
                    "8. Reg new main acc\n" +
                    "9. Track Rank\n" +
                    "10. Gen Pattern Wallet\n" +
                    "11. Gen Multi Pattern Wallet\n" +
                    "");
                RunType = Convert.ToInt32(Console.ReadLine().Trim());
                switch (RunType)
                {
                    case 1:
                        AutoRefAndVerify();
                        LogRunTime(startTime);
                        break;
                    case 2:
                        // await GetAllOldLinkAsync();
                        GetAllOldLink();
                        Console.WriteLine("Enter number of file you want: ");
                        int fileCount = Convert.ToInt32(Console.ReadLine().Trim());
                        SplitLinkFile(fileCount);
                        LogRunTime(startTime);
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
                        break;
                    case 4:
                        Console.WriteLine("Enter Ref Link:");
                        string refLink = Console.ReadLine().Trim();
                        Console.WriteLine("Enter Email:Wallet List Path:");
                        string emailWalletListPath = Console.ReadLine().Trim();
                        string[] emailWalletList = File.ReadAllLines(emailWalletListPath);
                        Console.WriteLine("WebDriver or Undetect?");
                        int driverType = Convert.ToInt32(Console.ReadLine().Trim());
                        AutoRegisterMainAccount(refLink, emailWalletList.ToList(), driverType);
                        LogRunTime(startTime);
                        break;
                    case 5:
                        startTime = DateTime.Now;
                        Console.WriteLine("Enter VerifyLink Path: ");
                        string path = Console.ReadLine().Trim();
                        VerifyAllSetup(startTime, path);
                        string pathFailed = CreateOrUpdateFile(FailedLinksFileName);
                        VerifyAllSetup(DateTime.Now, pathFailed, true);
                        File.Delete(pathFailed);
                        break;
                    case 6:
                        startTime = DateTime.Now;
                        Console.WriteLine("Enter RefLinkList Path:");
                        string refLinkListPath = Console.ReadLine().Trim();
                        string[] allRefLink = File.ReadAllLines(refLinkListPath);
                        Console.WriteLine("WebDriver or Undetect?");
                        int driverType2 = Convert.ToInt32(Console.ReadLine().Trim());
                        if (allRefLink.Length == 0)
                        {
                            LogWithColor("File empty", ConsoleColor.DarkRed);
                        }
                        Console.WriteLine("Enter work time per link:");
                        int workPerLink = Convert.ToInt32(Console.ReadLine().Trim());
                        int count2 = 1;
                        foreach (var link in allRefLink)
                        {
                            LogWithColor($"Current Link Index: {count2}", ConsoleColor.DarkBlue);
                            if (link.Contains("https"))
                            {
                                AutoRef(allRefLink.Length, startTime, link + RefSource + GetRandomRefSource(), workPerLink, driverType2);
                            }
                            else
                            {
                                AutoRef(allRefLink.Length, startTime, RefPrefix + link + RefSource + GetRandomRefSource(), workPerLink, driverType2);
                            }
                            count2++;
                        }
                        LogWithColor("----------------------------------------------------", ConsoleColor.DarkGreen);
                        LogWithColor($"Auto Ref Completed! Time: {DateTime.Now}\nTotal Ref Succeed: {TotalRefSuccess}", ConsoleColor.DarkGreen);
                        LogWithColor($"Total Ref Failed: {TotalRefFailed}", ConsoleColor.DarkRed);
                        LogRunTime(startTime);
                        LogWithColor("----------------------------------------------------", ConsoleColor.DarkGreen);
                        break;
                    case 8:
                        Console.WriteLine("Enter number of wallet");
                        int number = Convert.ToInt32(Console.ReadLine().Trim());
                        List<string> values = GenerateWalletAndPrivateKeyAsync(number);
                        string p = CreateOrUpdateFile("walletAddress");
                        File.AppendAllLines(p, values);
                        break;
                    case 9:
                        Console.WriteLine("Enter RefCode Path:");
                        string refCodePath = Console.ReadLine().Trim();
                        var refCodeList = File.ReadAllLines(refCodePath).ToList();
                        TrackRank(refCodeList);
                        break;
                    case 10:
                        var genStartTime = DateTime.Now;
                        Console.WriteLine("Input number of postfix:");
                        int numOfPost = Convert.ToInt32(Console.ReadLine().Trim());
                        Console.WriteLine("Input number pattern:");
                        string numberPattern = Console.ReadLine().Trim();
                        while (!(numberPattern.Length == numOfPost))
                        {
                            Console.WriteLine("number pattern length must equal number of postfix:");
                            numberPattern = Console.ReadLine().Trim();
                        }
                        GenPatternWallet(numOfPost, numberPattern);
                        LogRunTime(genStartTime);
                        break;
                    case 11:
                        var genMStartTime = DateTime.Now;
                        Console.WriteLine("Enter  number of wallet:");
                        int numOfGenWallet = Convert.ToInt32(Console.ReadLine().Trim());
                        Console.WriteLine("Input number of postfix:");
                        int numOfPostM = Convert.ToInt32(Console.ReadLine().Trim());
                        Console.WriteLine("Input number pattern:");
                        string numberPatternM = Console.ReadLine().Trim();
                        List<string> takeWallet = new List<string>();
                        while (!(numberPatternM.Length == numOfPostM))
                        {
                            Console.WriteLine("number pattern length must equal number of postfix:");
                            numberPatternM = Console.ReadLine().Trim();
                        }
                        var stringB = new StringBuilder();
                        while (takeWallet.Count < numOfGenWallet)
                        {
                            var notMatch = true;
                            var privateKey = "";
                            var address = "";
                            int countGen = 1;
                            double gps = 0;
                            while (notMatch)
                            {
                                Stopwatch st = new Stopwatch();
                                st.Start();
                                (privateKey, address) = GenerateWalletAndPrivateKeyAsync();
                                st.Stop();
                                var totalMs = st.ElapsedMilliseconds;
                                gps = 1000 / totalMs;
                                var takePattern = address.Substring(address.Length - numOfPostM, numOfPostM);
                                notMatch = !takePattern.Equals(numberPatternM);
                                Console.WriteLine(countGen + " | " + gps + "wallet/s");
                                countGen++;
                            }
                            string saveInfo = $"{address}|{privateKey}";
                            takeWallet.Add(saveInfo);
                        }
                        string pathGen = CreateOrUpdateFile("GenMultiWallet");
                        File.AppendAllLines(pathGen, takeWallet);
                        LogRunTime(genMStartTime);
                        break;
                    default:
                        break;
                }
            } while (RunType != 7);
            return Task.CompletedTask;
        }

        private static void GenPatternWallet(int numOfPost, string numberPattern)
        {
            var notMatch = true;
            var privateKey = "";
            var address = "";
            int countGen = 1;
            double gps = 0;
            while (notMatch)
            {
                Stopwatch st = new Stopwatch();
                st.Start();
                (privateKey, address) = GenerateWalletAndPrivateKeyAsync();
                st.Stop();
                var totalMs = st.ElapsedMilliseconds;
                gps = 1000 / totalMs;
                var takePattern = address.Substring(address.Length - numOfPost, numOfPost);
                notMatch = !takePattern.Equals(numberPattern);
                Console.WriteLine(countGen + " | " + gps + "wallet/s");
                countGen++;
            }
            Console.WriteLine("Bingo");
            string saveInfo = $"{address}|{privateKey}";
            SaveUrl(saveInfo, address);
        }

        private static void VerifyAllSetup(DateTime startTime, string path, bool isReVer = false)
        {
            string[] allLinks = File.ReadAllLines(path);
            TotalVerSuccess = 0;
            TotalVerFailed = 0;
            VerifyAllLink(startTime, allLinks, isReVer);
            LogWithColor("----------------------------------------------------", ConsoleColor.DarkGreen);
            LogWithColor($"Auto Verify Completed! Time: {DateTime.Now}\nTotal Verify Succeed: {TotalVerSuccess}", ConsoleColor.DarkGreen);
            LogWithColor($"Total Verify Failed: {TotalVerFailed}", ConsoleColor.DarkRed);
            LogRunTime(startTime);
            LogWithColor("----------------------------------------------------", ConsoleColor.DarkGreen);
        }

        public static void VerifyAllLink(DateTime startTime, string[] linkList, bool reVer = false)
        {
            if (reVer)
            {
                Console.WriteLine($"Re-Verifing Failed Links");
            }
            Console.WriteLine($"Total {linkList.Length} links.");
            for (int i = 0; i < linkList.Length; i++)
            {
                Stopwatch st = new Stopwatch();
                VerifyLink(linkList[i]);
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
                string linkFilePath = CreateOrUpdateFile(VerifyLinksFileName + (i + 1), true);
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

        private static void AutoRef(int totalShift, DateTime startTime, string refLink, int workTimes, int driverType = 1)
        {
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
                if (driverType == 1)
                {
                    InputEmailOriginDriver(inputModel);
                }
                else
                {
                    InputEmailUndetecDriver(inputModel);
                }
            }
        }

        private static void AutoRegisterMainAccount(string refLink, List<string> emailWalletList, int driverType = 1)
        {
            Console.Write($"Verifier working on {DateTime.Now}");
            foreach (var email in emailWalletList)
            {
                string[] info = email.Split(':');
                var inputModel = new CoinTeleGraphIM()
                {
                    Email = info[0],
                    LastName = GenerateName(DF_NAMELENGTH),
                    Wallet = info[1],
                    FirstName = GenerateName(DF_NAMELENGTH),
                    TargetUrl = refLink
                };
                if (driverType == 1)
                {
                    InputEmailOriginDriver(inputModel);
                }
                else
                {
                    InputEmailUndetecDriver(inputModel);
                }
            }
            LogWithColor($"Auto Ref Completed! Time: {DateTime.Now}\nTotal Ref Succeed: {TotalRefSuccess}", ConsoleColor.DarkGreen);
            LogWithColor($"Total Ref Failed: {TotalRefFailed}", ConsoleColor.DarkRed);
        }

        public static void LogWithColor(string value, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(value);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static int GetRandomlocation()
        {
            Random random = new Random();
            int locationId = random.Next(0, LocationArr.Length);
            return locationId;
        }

        private static string GetRandomRefSource()
        {
            Random random = new Random();
            int num = random.Next(0, RefSouceKey.Length);
            string refSource = RefSouceKey[num];
            return refSource;
        }

        private static void MappingAppsetting(CustomConfigModel customConfig)
        {
            EmailPostfix = customConfig.EmailPostFix;
            Console.WriteLine("Email Postfix: " + EmailPostfix);
            ChromeLocationPath = customConfig.ChromeLocationPath;
            //  ChromeDriverLocationPath = Directory.GetCurrentDirectory();
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

        public static void GetAllOldLink()
        {
            string title = ("Do you want to delete old VerifyLinks.txt file?");
            var isYes = Confirm(title);
            if (isYes)
            {
                DeleteFile(VerifyLinksFileName);
            }
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
                string searchKey = "Verify your email address";
                MailClient oClient = new MailClient("TryIt");
                oClient.Connect(oServer);
                oClient.GetMailInfosParam.Reset();
                oClient.GetMailInfosParam.SubjectOrBodyContains = searchKey;
                MailInfo[] infos = oClient.GetMailInfos();
                Console.WriteLine("Total {0} all email(s)\r\n", infos.Length);
                int count = 0;
                for (int j = 0; j < infos.Length; j++)
                {
                    MailInfo info = infos[j];
                    Console.WriteLine("Index: {0}; Size: {1}; UIDL: {2}",
                        info.Index, info.Size, info.UIDL);
                    Mail oMail = oClient.GetMail(info);
                    if (oMail.Subject.Contains(searchKey))
                    {
                        var url = GetVerifyLink(oMail.TextBody);
                        SaveUrl(url, VerifyLinksFileName);
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

        public static async Task GetAllOldLinkAsync()
        {
            string title = ("Do you want to delete old VerifyLinks.txt file?");
            var isYes = Confirm(title);
            if (isYes)
            {
                DeleteFile(VerifyLinksFileName);
            }
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
                string searchKey = "Verify your email address";
                MailClient oClient = new MailClient("TryIt");
                oClient.Connect(oServer);
                oClient.GetMailInfosParam.Reset();
                oClient.GetMailInfosParam.SubjectOrBodyContains = searchKey;
                MailInfo[] infos = oClient.GetMailInfos();
                Console.WriteLine("Total {0} all email(s)\r\n", infos.Length);
                int count = 0;
                for (int j = 0; j < infos.Length; j++)
                {
                    count = await ReadMailAsync(searchKey, oClient, infos, count, j);
                }
                oClient.Quit();
                Console.WriteLine($"Completed! Total: {count} Links. Time: {DateTime.Now}");
            }
            catch (Exception ep)
            {
                Console.WriteLine(ep.Message);
            }
        }

        private static async Task<int> ReadMailAsync(string searchKey, MailClient oClient, MailInfo[] infos, int count, int j)
        {
            try
            {
                MailInfo info = infos[j];
                Console.WriteLine("Index: {0}; Size: {1}; UIDL: {2}",
                    info.Index, info.Size, info.UIDL);

                Mail oMail = await Task.Run(() => oClient.GetMail(info));
                var url = await Task.Run(() => GetVerifyLink(oMail.TextBody));
                await Task.Run(() => SaveUrl(url, VerifyLinksFileName));
                count++;
                Console.WriteLine("To: {0}", oMail.To.ToString());
                if (!info.Read)
                {
                    await Task.Run(() => oClient.MarkAsRead(info, true));
                }
                return count;
            }
            catch (Exception e)
            {
                LogWithColor(e.ToString(), ConsoleColor.DarkRed);
                throw;
            }
        }

        public static void VerifyLink(string url)
        {
            try
            {
                var httpsProxy = GetHttpsProxy(ApiKey);
                ChromeOptions options = new ChromeOptions();
                options.AddArguments("--proxy-server=" + $"{httpsProxy[0]}" + ":" + $"{httpsProxy[1]}");
                _undetectedDriver = UndetectedChromeDriver.Create(options: options, driverExecutablePath: ChromeDriverLocationPath + @"\chromedriver.exe", browserExecutablePath: ChromeLocationPath);
                _undetectedDriver.GoToUrl(url);
                Thread.Sleep(20000);
                if (_undetectedDriver.Url.Contains("cointelegraph"))
                {
                    TotalVerSuccess += 1;
                    LogWithColor($"Verify Succeed!\nTotal Verify Succeed: {TotalVerSuccess}", ConsoleColor.DarkGreen);
                }
                else
                {
                    TotalVerFailed += 1;
                    LogWithColor($"Verify Failed!\nTotal Verify Failed: {TotalVerFailed}", ConsoleColor.DarkRed);
                    SaveUrl(url, FailedLinksFileName);
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
                _undetectedDriver = UndetectedChromeDriver.Create(options: options, driverExecutablePath: ChromeDriverLocationPath + @"\chromedriver.exe", browserExecutablePath: ChromeLocationPath);
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
                Console.WriteLine($"Working on Email: {inputModel.Email} | Name: {inputModel.FirstName} | {inputModel.LastName}\nWallet: {inputModel.Wallet}\n");
                var httpsProxy = GetHttpsProxy(ApiKey);
                ChromeOptions options = new ChromeOptions();
                options.AddArguments("--proxy-server=" + $"{httpsProxy[0]}" + ":" + $"{httpsProxy[1]}");
                options.BinaryLocation = ChromeLocationPath;
                _webDriver = new ChromeDriver(ChromeDriverLocationPath, options);
                _webDriver.Navigate().GoToUrl(inputModel.TargetUrl);
                Thread.Sleep(5000);
                ((IJavaScriptExecutor)_webDriver).ExecuteScript("VL.openModal()");

                Thread.Sleep(3000);
                IWebElement firstNameEle = _webDriver.FindElement(By.Id("form_firstName"));
                firstNameEle.SendKeys(inputModel.FirstName);

                IWebElement lastNameEle = _webDriver.FindElement(By.Id("form_lastname"));
                lastNameEle.SendKeys(inputModel.LastName);

                IWebElement emailEle = _webDriver.FindElement(By.Id("form_email"));
                emailEle.SendKeys(inputModel.Email);

                IWebElement ercWalletEle = _webDriver.FindElement(By.Id("extraField_0"));
                ercWalletEle.SendKeys(inputModel.Wallet);

                IWebElement submitBtn = _webDriver.FindElement(By.Id("vl_popup_submit"));
                submitBtn.Click();
                Thread.Sleep(1000);
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

        public static void TrackRank(List<string> listCode)
        {
            var fileName = "RefCode_Rank_" + DateTime.Now.ToString("yyyy_MM_dd hh_mm_ss tt");
            try
            {
                var url = "https://cointelegraph.com/historical?";
                ChromeOptions options = new ChromeOptions
                {
                    BinaryLocation = ChromeLocationPath
                };
                _webDriver = new ChromeDriver(ChromeDriverLocationPath, options);
                _webDriver.Navigate().GoToUrl(url);
                Thread.Sleep(5000);
                List<CheckRankModel> listChecked = new List<CheckRankModel>();
                foreach (var codeLine in listCode)
                {
                    string code = codeLine.Split(' ')[0];
                    if (!string.IsNullOrEmpty(code))
                    {
                        IJavaScriptExecutor js = (IJavaScriptExecutor)_webDriver;
                        var curCode = (String)js.ExecuteScript("return localStorage.getItem('vl_refCode_VSx8VFOsBXAmtdG2wyFoy380cp0')");
                        var trackModel = new CheckRankModel();
                        while (curCode != code)
                        {
                            try
                            {
                                js.ExecuteScript($"window.localStorage.setItem('vl_refCode_VSx8VFOsBXAmtdG2wyFoy380cp0', '{code}');");
                                _webDriver.Navigate().Refresh();
                                Thread.Sleep(3000);
                                ((IJavaScriptExecutor)_webDriver).ExecuteScript("VL.openModal()");
                                Thread.Sleep(2500);
                                List<IWebElement> eles = _webDriver.FindElements(By.CssSelector("#vl_popup.vlns.vl-new-version .vl-modal-dialog .vl-metric .vl-metric-value")).ToList();
                                var rank = eles[0].Text;
                                var refCount = eles[1].Text;
                                var rankNum = rank.Remove(0, 1);
                                var saveInfo = $"{code} {rank} {refCount}";
                                if (Convert.ToInt32(rankNum) <= 500)
                                {
                                    LogWithColor(saveInfo, ConsoleColor.DarkGreen);
                                }
                                else
                                {
                                    LogWithColor(saveInfo, ConsoleColor.DarkRed);
                                }
                                listChecked.Add(new CheckRankModel() { Code = code, Rank = rank, Ref = Convert.ToInt32(refCount) });
                                curCode = (String)js.ExecuteScript("return localStorage.getItem('vl_refCode_VSx8VFOsBXAmtdG2wyFoy380cp0')");
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }
                }
                listChecked.Distinct().ToList().OrderByDescending(x => x.Ref).ToList().ForEach(item =>
                {
                    var saveInfo = $"{item.Code} {item.Rank} {item.Ref}";
                    SaveUrl(saveInfo, fileName);
                });
                _webDriver.Dispose();
            }
            catch (Exception e)
            {
                if (_webDriver != null)
                {
                    _webDriver.Dispose();
                }
            }
        }

        public static void InputEmailUndetecDriver(CoinTeleGraphIM inputModel)
        {
            try
            {
                Console.WriteLine($"Working on Email: {inputModel.Email} | Name: {inputModel.FirstName} | {inputModel.LastName}\nWallet: {inputModel.Wallet}\n");
                var httpsProxy = GetHttpsProxy(ApiKey);
                ChromeOptions options = new ChromeOptions();
                options.AddArguments("--proxy-server=" + $"{httpsProxy[0]}" + ":" + $"{httpsProxy[1]}");
                options.BinaryLocation = ChromeLocationPath;
                _undetectedDriver = UndetectedChromeDriver.Create(options: options, driverExecutablePath: ChromeDriverLocationPath + @"\chromedriver.exe", browserExecutablePath: ChromeLocationPath);
                _undetectedDriver.Navigate().GoToUrl(inputModel.TargetUrl);
                Thread.Sleep(5000);
                ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("VL.openModal()");

                Thread.Sleep(3000);
                IWebElement firstNameEle = _undetectedDriver.FindElement(By.Id("form_firstName"));
                firstNameEle.SendKeys(inputModel.FirstName);

                IWebElement lastNameEle = _undetectedDriver.FindElement(By.Id("form_lastname"));
                lastNameEle.SendKeys(inputModel.LastName);

                IWebElement emailEle = _undetectedDriver.FindElement(By.Id("form_email"));
                emailEle.SendKeys(inputModel.Email);

                IWebElement ercWalletEle = _undetectedDriver.FindElement(By.Id("extraField_0"));
                ercWalletEle.SendKeys(inputModel.Wallet);

                IWebElement submitBtn = _undetectedDriver.FindElement(By.Id("vl_popup_submit"));
                submitBtn.Click();
                Thread.Sleep(1000);
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
