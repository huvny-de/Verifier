using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Nethereum.Signer;
using System.Linq;
using System.IO;
using Verifier.Models;
using SeleniumUndetectedChromeDriver;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Verifier.Extensions;
using System.Text.RegularExpressions;
using OpenQA.Selenium.DevTools.V105.DOM;
using Verifier.Serivces;
using EAGetMail;
using OpenQA.Selenium.DevTools.V105.Network;
using Org.BouncyCastle.Crypto;

namespace Verifier
{
    public class RedditService
    {
        public static string ChromeLocationPath { get; set; }
        public static string ChromeDriverLocationPath { get; set; }
        public static string HostEmailPassword { get; set; }
        public static string ApiKey { get; set; }
        public static string CaptChaApi { get; set; }
        public static int[] LocationArr { get; } = { 1, 4, 5, 7, 8, 10, 11 };

        public static int TotalRefSucceed { get; set; } = 0;
        public static int TotalRefFailed { get; set; } = 0;
        public static string HostWallet { get; set; }


        public static readonly string FailedLinksFileName = "LinksFailed" + GetRandomEmail() + GenerateName(8);

        public static string HostEmail { get; set; }
        public static int DF_NameLength { get; } = 5;
        public static List<string> MintFail { get; set; } = new List<string>();
        public static List<string> LoginFail { get; set; } = new List<string>();


        public static UndetectedChromeDriver _undetectedDriver;
        public static IWebDriver _webDriver;

        public async Task StartSerivce()
        {
            Console.Write($"Verifier working on {DateTime.Now}\n");
            var startTime = DateTime.Now;
            ReadSettingFile();
            GlobalAppInput();
            await Menu(startTime);
            Environment.Exit(0);
        }

        public async Task Menu(DateTime startTime)
        {
            int runType = 0;
            do
            {
                Console.WriteLine("Choose Run Type:\n" +
                    "1. Auto Ref\n" +
                    "2. Auto Transfer\n" +
                    "3. Create Reddit Account\n" +
                    "4. Mint\n" +
                    "5. Ref rainbow"
                    );
                runType = Convert.ToInt32(Console.ReadLine().Trim());
                switch (runType)
                {
                    case 1:
                        Console.WriteLine("Please input email file path");
                        var filePath = Console.ReadLine();
                        var emailList = File.ReadAllLines(filePath);
                        LogWithColor($"Total {emailList.Length} emails", ConsoleColor.DarkBlue);

                        foreach (var email in emailList)
                        {
                            await AutoRef(email);

                        }
                        LogRunTime(startTime);
                        break;
                    case 2:


                        break;
                }
            } while (runType != 3);
        }

        private Mail GetEmail(string currentEmail, int timeout = 10)
        {
            var imap = new IMAPService();
            var mailClient = imap.CreateClient(HostEmail, HostEmailPassword);
            mailClient.WaitNewEmail(2);
            var mail = imap.GetEmail(mailClient, currentEmail);
            try
            {
                int count = 0;
                while (mail == null && count < timeout)
                {
                    LogWithColor($"waiting for new email {count + 1}", ConsoleColor.DarkBlue);
                    mailClient.WaitNewEmail(2);
                    mail = imap.GetEmail(mailClient, currentEmail);
                    count++;
                }
                if (mail == null)
                {
                    LogWithColor("not found email", ConsoleColor.DarkYellow);
                    imap.Disconnect(mailClient);
                    return null;
                }
                imap.Disconnect(mailClient);
                return mail;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                imap.Disconnect(mailClient);
                return null;
            }
        }

        private string GetVerifyLink(string text)
        {
            string[] lines = text.Split('\n');

            // Find the line that contains the link.
            string linkLine = null;
            foreach (string line in lines)
            {
                if (line.Contains("<https://auth.magic.link/confirm"))
                {
                    linkLine = line;
                    break;
                }
            }

            // Extract the link from the line.
            int startIndex = linkLine.IndexOf('<') + 1;
            int endIndex = linkLine.IndexOf('>', startIndex);
            string link = linkLine.Substring(startIndex, endIndex - startIndex);

            return link;
        }


        public async Task RefRainbow(string[] account, string refLink)
        {
            string saveInfo = $"{account[0]}|{account[1]}";
            Console.WriteLine($"Working on Username: {account[0]}\n");

            _undetectedDriver = UndetectedChromeDriver.Create(driverExecutablePath: ChromeDriverLocationPath + @"\chromedriver.exe", browserExecutablePath: ChromeLocationPath);
            _undetectedDriver.Navigate().GoToUrl(refLink);
            Thread.Sleep(5000);

            var emailEle = GetWebElementUntilSuccess(By.Id("email"));
            emailEle.SendKeys(account[0]);
            var submit = GetWebElementUntilSuccess(By.CssSelector(".framer-pdw0va"));
            submit.Click();
            SaveUrl(saveInfo, "Rainbow", "Ref");
        }

        public async Task AutoMint()
        {
            //  var regex = new Regex(@"^0x[a-fA-F0-9]{40}$");
            Console.WriteLine("Enter account path:");
            string accPath = Console.ReadLine().Trim();
            //while (!regex.IsMatch(wallet))
            //{
            //    Console.WriteLine("Wallet is not correct, try again:");
            //    wallet = Console.ReadLine().Trim();
            //}
            var accountList = File.ReadAllLines(accPath);
            Console.WriteLine($"Total {accountList.Length} accounts.");
            string fileName = "RedditMinted_" + DateTime.Now.ToString("yyyy_MM_dd hh_mm_ss tt");
            string fileNameFailed = "RedditMintFailed_" + DateTime.Now.ToString("yyyy_MM_dd hh_mm_ss tt");

            foreach (var acc in accountList)
            {
                var account = acc.Split('|');
                AutoMintDriver(account, fileName);
            }
            int countFail = 1;
            while (MintFail.Count > 0)
            {
                foreach (var acc in MintFail)
                {
                    LogWithColor($"Re-mint failed. Time: {countFail}", ConsoleColor.Red);
                    var failedAcc = acc.Split('|');
                    AutoMintDriver(failedAcc, fileName);
                }
                countFail++;
            }
            var allSucess = File.ReadAllLines(fileName);
            var mintFailed = accountList.Except(allSucess).ToList();
            File.WriteAllLines(fileNameFailed, mintFailed);
        }

        public void AutoMintDriver(string[] account, string fileName)
        {

            var loginUrl = "https://www.reddit.com/login/?dest=https%3A%2F%2Fwww.reddit.com%2F";
            try
            {
                string info = $"{account[0]}|{account[1]}";
                _undetectedDriver = UndetectedChromeDriver.Create(driverExecutablePath: ChromeDriverLocationPath + @"\chromedriver.exe", browserExecutablePath: ChromeLocationPath);
                _undetectedDriver.Navigate().GoToUrl(loginUrl);
                Thread.Sleep(5000);

                IWebElement usernameEle = GetWebElementUntilSuccess(By.Id("loginUsername"));
                usernameEle.SendKeys(account[0]);
                Thread.Sleep(1000);

                IWebElement passwordEle = GetWebElementUntilSuccess(By.Id("loginPassword"));
                passwordEle.SendKeys(HostEmailPassword);
                Thread.Sleep(1000);

                var maxLogin = 0;
                var loginBtn = GetWebElementsUntilSuccess(By.CssSelector(".AnimatedForm__submitButton"));
                while (loginBtn.Count != 3 && maxLogin < 3)
                {
                    loginBtn = GetWebElementsUntilSuccess(By.CssSelector(".AnimatedForm__submitButton"));
                    maxLogin++;
                }
                ClickUntilSuccess(loginBtn[0]);
                var messageEle = GetWebElementsUntilSuccess(By.CssSelector(".AnimatedForm__errorMessage")).ToList();
                var errMsg = messageEle.FirstOrDefault()?.Text;
                if (!string.IsNullOrEmpty(errMsg))
                {
                    if (errMsg == "Incorrect username or password")
                    {
                        LogWithColor(errMsg, ConsoleColor.DarkRed);
                        SaveUrl(info, "Reddit", "LoginFailed");
                        _undetectedDriver.Dispose();
                        return;
                    }
                }
                Thread.Sleep(8000);
                //var maxGotIt = 0;
                //List<IWebElement> gotIt = GetWebElementsUntilSuccess(By.CssSelector("._34mIRHpFtnJ0Sk97S2Z3D9"));
                //while (gotIt.Count != 4 && maxGotIt < 3)
                //{
                //    gotIt = GetWebElementsUntilSuccess(By.CssSelector("._34mIRHpFtnJ0Sk97S2Z3D9"));
                //    maxGotIt++;
                //}
                //if (gotIt.Count == 4)
                //{
                //    ClickUntilSuccess(gotIt[3]);
                //    Thread.Sleep(2000);

                //    var maxDivEmail = 0;
                //    IWebElement divEmail = GetWebElementUntilSuccess(By.CssSelector("._1AaXuuXcppN6z3lyjemnkL"));
                //    while (divEmail == null && maxDivEmail < 3)
                //    {
                //        divEmail = GetWebElementUntilSuccess(By.CssSelector("._1AaXuuXcppN6z3lyjemnkL"));
                //        maxDivEmail++;
                //    }
                //    Thread.Sleep(3000);
                //    ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("document.querySelectorAll('._34mIRHpFtnJ0Sk97S2Z3D9').forEach(x=>{if(x.textContent === 'Got it'){x.click();}})");
                //    Thread.Sleep(1000);
                //}
                IWebElement userDropdown = GetWebElementUntilSuccess(By.Id("USER_DROPDOWN_ID"));
                ClickUntilSuccess(userDropdown);
                Thread.Sleep(2000);

                ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("document.querySelector('._6opQAE7SUXi-Fy7P3vItL').click()");
                Thread.Sleep(12000);


                var next = GetWebElementsUntilSuccess(By.CssSelector("._button_3pioz_15")).ToList();
                while (next.Count != 7)
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
                Thread.Sleep(1000);
                var toast = GetWebElementUntilSuccess(By.CssSelector("._toastWrapper_1ojai_1"));
                var textContent = toast?.Text;
                Console.WriteLine(textContent);
                var message = GetWebElementUntilSuccess(By.CssSelector("._content_fu7hf_81"));
                var msg = message?.Text;
                Console.WriteLine(msg);
                if (String.IsNullOrEmpty(textContent) && String.IsNullOrEmpty(msg))
                {
                    Thread.Sleep(500);

                    IWebElement vaultPass = GetWebElementUntilSuccess(By.Id("passwordField"));
                    vaultPass.SendKeys(HostEmailPassword);
                    Thread.Sleep(200);

                    IWebElement vaultPassConfirm = GetWebElementUntilSuccess(By.Id("confirmationPasswordField"));
                    vaultPassConfirm.SendKeys(HostEmailPassword);
                    Thread.Sleep(200);

                    var secureBtn = GetWebElementUntilSuccess(By.CssSelector("._button_1wfm6_149._button_1wfm6_149"));
                    ClickUntilSuccess(secureBtn);
                    Thread.Sleep(6000);

                    var maxDivTest = 0;
                    var divTest = GetWebElementUntilSuccess(By.CssSelector("._itemDescription_7kbcu_189"));
                    while (divTest == null && maxDivTest < 3)
                    {
                        divTest = GetWebElementUntilSuccess(By.CssSelector("._itemDescription_7kbcu_189"));
                        maxDivTest++;
                    }
                    var continue4 = GetWebElementUntilSuccess(By.CssSelector("._button_koles_42"));
                    while (continue4.Text != "Continue")
                    {
                        continue4 = GetWebElementUntilSuccess(By.CssSelector("._button_koles_42"));
                    }
                 ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("document.querySelectorAll('._button_koles_42').forEach(x=>{if(x.textContent === 'Continue'){x.click();}})");
                    Thread.Sleep(10000);

                    var fullItem = GetWebElementsUntilSuccess(By.CssSelector("._ctaButton_y0x52_1"));
                    while (fullItem == null)
                    {
                        fullItem = GetWebElementsUntilSuccess(By.CssSelector("._ctaButton_y0x52_1"));
                    }
                ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("document.querySelectorAll('._ctaButton_y0x52_1').forEach(x=>{if(x.textContent === 'Get the Full Look'){x.click();}})");
                    Thread.Sleep(6000);
                    ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("document.querySelectorAll('._ctaButton_y0x52_1').forEach(x=>{if(x.textContent === 'Save'){x.click();}})");
                    Thread.Sleep(6000);
                }
                SaveUrl(info, "RedditMinted", fileName);
                TotalRefSucceed += 1;
                if (MintFail.Contains(info))
                {
                    LogWithColor($"Removed {info} from fail list", ConsoleColor.DarkGreen);
                    MintFail.Remove(info);
                }
                LogWithColor($"Total Mint Succeed: {TotalRefSucceed}", ConsoleColor.DarkGreen);
                _undetectedDriver.Dispose();
            }
            catch (Exception e)
            {
                LogWithColor(e.Message, ConsoleColor.DarkRed);
                TotalRefFailed += 1;
                LogWithColor($"Total Mint Failed: {TotalRefFailed}", ConsoleColor.DarkRed);
                string info = $"{account[0]}|{account[1]}";
                MintFail.Add(info);
                if (_undetectedDriver != null)
                {
                    _undetectedDriver.Dispose();
                }
            }
        }

        public void LogRunTime(DateTime startTime)
        {
            var runTimes = DateTime.Now.Subtract(startTime);
            LogWithColor($"Total Run Time: {runTimes.Days}d {runTimes.Hours}hrs {runTimes.Minutes}m {runTimes.Seconds}s", ConsoleColor.DarkGreen);
        }

        public void GlobalAppInput()
        {
            //Console.WriteLine("Enter Proxy Api:");
            //ApiKey = Console.ReadLine().Trim();
            Console.WriteLine("Enter Chrome Driver Path:");
            ChromeDriverLocationPath = Console.ReadLine().Trim();
        }

        public void ReadSettingFile()
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
                Console.WriteLine($"Please Input AppSetting!\nPath: {Directory.GetCurrentDirectory()}");
                CreateSettingFile();
                Console.ReadLine();
                return;
            }
            var customConfig = JsonConvert.DeserializeObject<CustomConfigModel>(rs);
            if (String.IsNullOrEmpty(customConfig.HostEmail) | String.IsNullOrEmpty(customConfig.HostEmailPassword) | string.IsNullOrEmpty(customConfig.ChromeLocationPath))
            {
                Console.WriteLine($"Please Input AppSetting!\nPath: {Directory.GetCurrentDirectory()}");
                Console.ReadLine();
                return;
            }
            MappingAppsetting(customConfig);
        }

        public void CreateSettingFile()
        {
            var customConfig = new CustomConfigModel();
            var json = JsonConvert.SerializeObject(customConfig);
            File.WriteAllText(Directory.GetCurrentDirectory() + @"\AppSetting.json", json);
        }

        public async Task AutoRef(string info)
        {
            var spliter = info.Split('|');
            var inputModel = new RedditInputModel()
            {
                Email = spliter.FirstOrDefault(),
                Password = HostEmailPassword,
                Username = GetRandomEmail() + GenerateName(DF_NameLength),
                FirstName = GenerateName(DF_NameLength),
                LastName = GenerateName(DF_NameLength),

            };
            await InputEmailOriginDriver(inputModel);
        }

        public async Task CreateAccounts()
        {
            Console.WriteLine("Enter Work Time:");
            int workTimes = Convert.ToInt32(Console.ReadLine().Trim());
            Console.WriteLine("Enter Sleep Time:");
            int sleepTime = Convert.ToInt32(Console.ReadLine().Trim());
            string fileName = "RedditAccountNotMint" + DateTime.Now.ToString("yyyy_MM_dd hh_mm_ss tt");
            for (int i = 0; i < workTimes; i++)
            {
                var inputModel = new RedditInputModel()
                {
                    Email = GetRandomEmail() + HostEmail,
                    Password = HostEmailPassword,
                    Username = GetRandomEmail() + GenerateName(DF_NameLength)
                };
                await CreateAccountDriver(inputModel, fileName, sleepTime);
            }
        }

        public async Task AutoTransfer()
        {
            Console.WriteLine("Enter Account Path:");
            string path = Console.ReadLine().Trim();
            var regex = new Regex(@"^0x[a-fA-F0-9]{40}$");
            Console.WriteLine("Enter wallet:");
            string wallet = Console.ReadLine().Trim();
            while (!regex.IsMatch(wallet))
            {
                Console.WriteLine("Wallet is not correct, try again:");
                wallet = Console.ReadLine().Trim();
            }
            string[] accounts = File.ReadAllLines(path);
            foreach (var account in accounts)
            {
                await AutoTransferDriver(account.Split('|'), wallet);
            }
        }

        public async Task AutoTransferDriver(string[] account, string wallet)
        {
            var loginUrl = "https://www.reddit.com/login/?dest=https%3A%2F%2Fwww.reddit.com%2F";
            try
            {
                string saveInfo = $"{account[0]}|{account[1]}";
                Console.WriteLine($"Working on Username: {account[0]}\n");

                _undetectedDriver = UndetectedChromeDriver.Create(driverExecutablePath: ChromeDriverLocationPath + @"\chromedriver.exe", browserExecutablePath: ChromeLocationPath);
                _undetectedDriver.Navigate().GoToUrl(loginUrl);
                Thread.Sleep(5000);

                IWebElement usernameEle = GetWebElementUntilSuccess(By.Id("loginUsername"));
                usernameEle.SendKeys(account[0]);
                Thread.Sleep(1000);

                IWebElement passwordEle = GetWebElementUntilSuccess(By.Id("loginPassword"));
                passwordEle.SendKeys(HostEmailPassword);
                Thread.Sleep(1000);

                var maxLogin = 0;
                var loginBtn = GetWebElementsUntilSuccess(By.CssSelector(".AnimatedForm__submitButton"));
                while (loginBtn.Count != 3 && maxLogin < 3)
                {
                    loginBtn = GetWebElementsUntilSuccess(By.CssSelector(".AnimatedForm__submitButton"));
                    maxLogin++;
                }
                ClickUntilSuccess(loginBtn[0]);
                Thread.Sleep(10000);

                IWebElement userDropdown = GetWebElementUntilSuccess(By.Id("USER_DROPDOWN_ID"));
                ClickUntilSuccess(userDropdown);
                Thread.Sleep(2000);

                ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("document.querySelector('._6opQAE7SUXi-Fy7P3vItL').click()");
                Thread.Sleep(12000);

                var maxYou = 0;
                var you = GetWebElementsUntilSuccess(By.CssSelector("._pillOption_1such_54"));
                var isValid = you.Any(x => x.Text == "you");
                while (you.Count != 4 && !isValid && maxYou < 3)
                {
                    you = GetWebElementsUntilSuccess(By.CssSelector("._pillOption_1such_54"));
                    isValid = you.Any(x => x.Text == "you");
                    maxYou++;
                }
                ClickUntilSuccess(you[3]);
                Thread.Sleep(3000);

                var yourStuff = GetWebElementUntilSuccess(By.CssSelector("._banner_wez9n_1"));
                if (yourStuff != null)
                {
                    ClickUntilSuccess(yourStuff);
                    Thread.Sleep(500);

                    var maxoutFit = 0;
                    var outFit = GetWebElementsUntilSuccess(By.CssSelector("._card_b43y4_1"));
                    while (outFit == null && maxoutFit < 3)
                    {
                        outFit = GetWebElementsUntilSuccess(By.CssSelector("._button_3pioz_15"));
                        maxoutFit++;
                    }
                    ClickUntilSuccess(outFit[0]);
                    Thread.Sleep(1000);

                    ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("document.querySelector('._sectionDescription_eet7b_47').click()");
                    Thread.Sleep(2000);

                    var transfer = GetWebElementsUntilSuccess(By.CssSelector("._button_koles_42"));
                    ClickUntilSuccess(transfer[0]);
                    Thread.Sleep(1000);


                    IWebElement toField = GetWebElementUntilSuccess(By.Id("toField"));
                    toField.SendKeys(wallet);
                    Thread.Sleep(200);

                    IWebElement passwordField = GetWebElementUntilSuccess(By.Id("passwordField"));
                    passwordField.SendKeys(HostEmailPassword);
                    Thread.Sleep(200);

                    var transfer2 = GetWebElementsUntilSuccess(By.CssSelector("._button_koles_42"));
                    ClickUntilSuccess(transfer2[1]);
                    Thread.Sleep(1000);

                    IWebElement transferingnow = GetWebElementUntilSuccess(By.CssSelector("._messageTitle_tzkjg_146"));
                    LogWithColor(transferingnow?.Text, ConsoleColor.DarkGreen);
                }
                string fileName = "transfered";
                SaveUrl(saveInfo, "Transfered", fileName);
                TotalRefSucceed += 1;
                LogWithColor($"Total Transfer Succeed: {TotalRefSucceed}", ConsoleColor.DarkGreen);
                _undetectedDriver.Dispose();
            }
            catch (Exception e)
            {
                LogWithColor(e.Message, ConsoleColor.DarkRed);
                TotalRefFailed += 1;
                LogWithColor($"Total Transfer Failed: {TotalRefFailed}", ConsoleColor.DarkRed);
                if (_undetectedDriver != null)
                {
                    _undetectedDriver.Dispose();
                }
            }
        }

        public NewProxyModel GetCurrentProxy(string apiKey)
        {
            var proxy = TMAPIHelper.GetCurrentProxy(apiKey);
            NewProxyModel proxyModel = JsonConvert.DeserializeObject<NewProxyModel>(proxy);
            return proxyModel;
        }

        public string[] GetHttpsProxy(string apiKey)
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

        public string[] GetNewProxyOnly(string apiKey)
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

        public NewProxyModel GetProxyModel(string apiKey)
        {
            var locationId = GetRandomlocation();
            var proxy = TMAPIHelper.GetNewProxy(apiKey, apiKey, locationId);
            NewProxyModel proxyModel = JsonConvert.DeserializeObject<NewProxyModel>(proxy);
            return proxyModel;
        }

        public int GetRandomlocation()
        {
            Random random = new Random();
            int locationId = random.Next(0, LocationArr.Length);
            return locationId;
        }

        public async Task InputEmailOriginDriver(RedditInputModel inputModel)
        {
            var registerUrl = "https://niftys.com/";
            try
            {
                Console.WriteLine($"Working on Email: {inputModel.Email} | Username: {inputModel.Username}\n");
                // var httpsProxy = GetNewProxyOnly(ApiKey);
                ChromeOptions options = new ChromeOptions();
                //options.AddArguments("--proxy-server=" + $"{httpsProxy[0]}" + ":" + $"{httpsProxy[1]}");
                options.AddArguments("--disable-popup-blocking");
                _undetectedDriver = UndetectedChromeDriver.Create(options: options, driverExecutablePath: ChromeDriverLocationPath + @"\chromedriver.exe", browserExecutablePath: ChromeLocationPath);
                _undetectedDriver.Navigate().GoToUrl(registerUrl);
                Thread.Sleep(5000);

                var loginbtn = GetWebElementUntilSuccess(By.XPath("//*[@id=\"app_container\"]/div[2]/div[2]/div[1]/div[4]/button[1]"));
                loginbtn.Click();

                var inputEmail = GetWebElementUntilSuccess(By.CssSelector("html > body > div:nth-of-type(4) > div > div > div > div:nth-of-type(2) > div > form > div > div > div > div:nth-of-type(3) > input"));
                inputEmail.SendKeys(inputModel.Email);

                var loginBtnPopup = GetWebElementUntilSuccess(By.CssSelector("html > body > div:nth-of-type(4) > div > div > div > div:nth-of-type(2) > div > form > div:nth-of-type(2) > button"));
                loginBtnPopup.Click();

                Thread.Sleep(3000);

                var mail = GetEmail(inputModel.Email);
                var link = GetVerifyLink(mail?.TextBody);

                ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("window.open();");
                string currentWindowHandle = _undetectedDriver.CurrentWindowHandle;
                string newWindowHandle = string.Empty;
                foreach (string handle in _undetectedDriver.WindowHandles)
                {
                    if (handle != currentWindowHandle)
                    {
                        newWindowHandle = handle;
                        break;
                    }
                }
                _undetectedDriver.SwitchTo().Window(newWindowHandle);
                _undetectedDriver.Navigate().GoToUrl(link);
                Thread.Sleep(3000);

                _undetectedDriver.Close();
                _undetectedDriver.SwitchTo().Window(_undetectedDriver.WindowHandles.Last());

                var nameInput = GetWebElementUntilSuccess(By.XPath("//*[@id=\"app_container\"]/div[2]/main[1]/div[1]/section[1]/div[2]/form[1]/div[1]/div[1]/div[1]/div[1]/div[3]/input[1]"), 30);
                nameInput.SendKeys(inputModel.FirstName + " " + inputModel.LastName);

                var usernameInput = GetWebElementUntilSuccess(By.XPath("//*[@id=\"app_container\"]/div[2]/main[1]/div[1]/section[1]/div[2]/form[1]/div[1]/div[2]/div[1]/div[2]/div[1]/div[3]/input[1]"));
                usernameInput.SendKeys(inputModel.Username);

                var argreebtn = GetWebElementUntilSuccess(By.XPath("//*[@id=\"app_container\"]/div[2]/main[1]/div[1]/section[1]/div[2]/form[1]/div[1]/div[4]/div[1]/label[1]/div[1]"));
                argreebtn.Click();

                var letgoBtn = GetWebElementUntilSuccess(By.XPath("//*[@id=\"app_container\"]/div[2]/main[1]/div[1]/section[1]/div[2]/form[1]/div[2]/button[1]"));
                letgoBtn.Click();

                string fileName = "niftys";
                var info = $"{inputModel.Email}|{inputModel.Username}";
                SaveUrl(info, "niftys", fileName);

                Thread.Sleep(2000);

                var gleamUrl = "https://gleam.io/2OdsZ/game-of-thrones-build-your-realm-allowlist-twitter-quiz";
                _undetectedDriver.Navigate().GoToUrl(gleamUrl);
                Thread.Sleep(5000);

                var gleamUserName = GetWebElementUntilSuccess(By.CssSelector("html > body > div > form > div > div:nth-of-type(2) > div > div > input"));
                gleamUserName.SendKeys(inputModel.Username);

                var emailAddress = GetWebElementUntilSuccess(By.CssSelector("html > body > div > form > div > div:nth-of-type(2) > div > div:nth-of-type(2) > input"));
                emailAddress.SendKeys(inputModel.Email);

                var submitBtn = GetWebElementUntilSuccess(By.CssSelector("html > body > div > form > div > div:nth-of-type(2) > div:nth-of-type(2) > div > input"));
                submitBtn.Click();

                TotalRefSucceed += 1;
                LogWithColor($"Total Ref Succeed: {TotalRefSucceed}", ConsoleColor.DarkGreen);
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
                Thread.Sleep(1000);
            }
        }

        public async Task CreateAccountDriver(RedditInputModel inputModel, string fileName, int sleepTime = 30000)
        {
            var registerUrl = "https://www.reddit.com/register/";
            try
            {
                Console.WriteLine($"Working on Email: {inputModel.Email} | Username: {inputModel.Username}\n");
                var httpsProxy = GetNewProxyOnly(ApiKey);
                ChromeOptions options = new ChromeOptions();
                options.AddArguments("--proxy-server=" + $"{httpsProxy[0]}" + ":" + $"{httpsProxy[1]}");
                _undetectedDriver = UndetectedChromeDriver.Create(options: options, driverExecutablePath: ChromeDriverLocationPath + @"\chromedriver.exe", browserExecutablePath: ChromeLocationPath);
                _undetectedDriver.Navigate().GoToUrl(registerUrl);
                Thread.Sleep(5000);

                IWebElement firstNameEle = GetWebElementUntilSuccess(By.Id("regEmail"));
                firstNameEle.SendKeys(inputModel.Email);
                Thread.Sleep(1000);

                ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("document.querySelectorAll('.AnimatedForm__submitButton').forEach(x => {if(x.textContent == 'Continue'){ x.click();}})");
                Thread.Sleep(1000);

                var suggestUsername = GetWebElementsUntilSuccess(By.CssSelector(".Onboarding__usernameSuggestion"));
                Thread.Sleep(200);
                IWebElement regUsername = GetWebElementUntilSuccess(By.Id("regUsername"));
                var takeUsername = suggestUsername[0].Text;
                regUsername.SendKeys(takeUsername);

                IWebElement regPassword = GetWebElementUntilSuccess(By.Id("regPassword"));
                regPassword.SendKeys(HostEmailPassword);
                Thread.Sleep(4000);

                CaptChaExtension captChaExtension = new CaptChaExtension();
                TwoCaptcha.Models.TwoCaptcha token = await captChaExtension.ReCaptchaV2Async(CaptChaApi);
                Thread.Sleep(5000);

                IWebElement innerToken = GetWebElementUntilSuccess(By.CssSelector(".g-recaptcha-response"), 20);
                Console.WriteLine(innerToken.TagName);
                ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript($"document.getElementById('g-recaptcha-response').innerHTML='{token.Request}'");
                // ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript($"document.getElementById('recaptcha-demo-form').submit()");

                Thread.Sleep(2000);
                var submitBtn = GetWebElementsUntilSuccess(By.CssSelector(".AnimatedForm__submitButton"));
                ClickUntilSuccess(submitBtn[1]);
                Thread.Sleep(10000);
                string folderName = "RedditAccountNotMint";
                string info = $"{takeUsername}|{inputModel.Email}";
                SaveUrl(info, folderName, fileName);
                LogWithColor($"Account: {info} have been saved!", ConsoleColor.DarkGreen);

                IWebElement skipBtn = GetWebElementUntilSuccess(By.CssSelector("._22ChQI9alXTuxk7yqwRt8l"));
                ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("document.querySelectorAll('._22ChQI9alXTuxk7yqwRt8l').forEach(x=>{if(x.textContent === 'Skip'){x.click();}})");
                Thread.Sleep(1000);

                var emojiBtnDiv = GetWebElementUntilSuccess(By.CssSelector("._2Bejocqb-InO8686E2ehf"));
                ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("document.querySelectorAll('._3oCL2oMbe3H81mue3bR1CQ ')[0].click()");
                ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("document.querySelectorAll('._3oCL2oMbe3H81mue3bR1CQ ')[1].click()");
                ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("document.querySelectorAll('._3oCL2oMbe3H81mue3bR1CQ ')[2].click()");
                Thread.Sleep(1000);

                ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("document.querySelector('.dK60vCQAai2JPR7mVZ4ir').click()");
                Thread.Sleep(1000);

                var fistJoin = GetWebElementsUntilSuccess(By.CssSelector("._2h_rraB53rhUmsB6cnedRY")).ToList();
                while (fistJoin.Count < 2)
                {
                    fistJoin = GetWebElementsUntilSuccess(By.CssSelector("._2h_rraB53rhUmsB6cnedRY")).ToList();
                }
                ClickUntilSuccess(fistJoin[1]);
                TotalRefSucceed += 1;
                LogWithColor($"Total Create Succeed: {TotalRefSucceed}", ConsoleColor.DarkGreen);
                _undetectedDriver.Dispose();
                Thread.Sleep(sleepTime);
            }
            catch (Exception e)
            {
                LogWithColor(e.Message, ConsoleColor.DarkRed);
                TotalRefFailed += 1;
                LogWithColor($"Total Create Failed: {TotalRefFailed}", ConsoleColor.DarkRed);
                if (_undetectedDriver != null)
                {
                    _undetectedDriver.Dispose();
                }
                Thread.Sleep(sleepTime);
            }
        }

        public bool TryClickButton(IWebElement ele)
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

        public void ClickUntilSuccess(IWebElement ele)
        {
            var maxTime = 0;
            var rs = TryClickButton(ele);
            while (rs == false && maxTime < 10)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"Trying click element {maxTime + 1} | {ele?.Text}");
                rs = TryClickButton(ele);
                maxTime++;
            }
        }

        public IWebElement GetWebElement(By by)
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

        public IWebElement GetWebElementUntilSuccess(By by, int maxWork = 10)
        {
            var workTime = 0;
            var ele = GetWebElement(by);
            while (ele == null && workTime < maxWork)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"Trying get element {workTime + 1}");
                ele = GetWebElement(by);
                workTime++;
            }
            return ele;
        }

        public List<IWebElement> GetWebElementsUntilSuccess(By by)
        {
            var maxTime = 0;
            var ele = GetWebElements(by);
            while (ele == null && maxTime < 10)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"Trying get list element {maxTime + 1}");
                ele = GetWebElements(by);
                maxTime++;
            }
            return ele;
        }


        public List<IWebElement> GetWebElements(By by)
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

        public void LogWithColor(string value, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(value);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public int GetRandomFC()
        {
            int[] randomFB = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 };
            Random random = new Random();
            int locationId = random.Next(0, randomFB.Length);
            return locationId;
        }

        public void MappingAppsetting(CustomConfigModel customConfig)
        {
            HostEmail = customConfig.HostEmail;
            Console.WriteLine("Host Email: " + HostEmail);
            HostEmailPassword = customConfig.HostEmailPassword;
            Console.WriteLine("Host Email Password: " + HostEmailPassword);
            ChromeLocationPath = customConfig.ChromeLocationPath;
            //HostWallet = customConfig.Wallet;
            //Console.WriteLine("HostWallet: " + HostWallet);
            //CaptChaApi = customConfig.CaptChaApi;
            //Console.WriteLine("CaptChaApi: " + CaptChaApi);
        }

        public void DeleteFile(string fileName = "VerifyLinks")
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

        public string CreateOrUpdateFile(string folderName, string fileName = "VerifyLinks", bool shoudNew = false)
        {
            string localPath = string.Format("{0}\\{1}", Directory.GetCurrentDirectory(), folderName);
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }
            string filePath = string.Format("{0}\\{1}\\{2}.txt", Directory.GetCurrentDirectory(), folderName, fileName);
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

        public string SaveUrl(string url, string folderName, string fileName)
        {
            var filePath = CreateOrUpdateFile(folderName, fileName);
            using (StreamWriter writer = new StreamWriter(filePath, append: true))
            {
                writer.WriteLine(url, 0, url.Length);
                writer.Close();
            }
            return filePath;
        }

        public bool Confirm(string title)
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

        private List<string> GenerateWalletAndprivateKeyAsync(int total)
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

        public (string, string) GenerateWalletAndpublicKeyAsync()
        {
            EthECKey key = EthECKey.GenerateKey();
            string publicKey = key.GetPrivateKey();
            string address = key.GetPublicAddress();
            return (publicKey, address);
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
