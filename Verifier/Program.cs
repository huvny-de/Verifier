using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Nethereum.Signer;
using EAGetMail;
using System.Linq;
using System.IO;

namespace Verifier
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var emailPost = "@vunimail.com";

            Console.WriteLine("Enter Ref Link::");
            string refLink = Console.ReadLine();
            Console.WriteLine("Enter Proxy Api:");
            string apiKey = Console.ReadLine();
            Console.WriteLine("Enter Work Time:");

            int work = Convert.ToInt32(Console.ReadLine());
            Console.Write($"Verifier working on {DateTime.Now}");
            //var program = new Program();
            //program.GetAllOldLink();
            for (int i = 0; i < work; i++)
            {
                var program = new Program();
                var email = program.GetRandomEmail();
                var lastName = program.GenerateName(5);
                var ethWallet = program.GenerateWallet();
                var program2 = new Program();
                var firstName = program2.GenerateName(5);
                program.InputEmail(email + emailPost, refLink, firstName, lastName, ethWallet, apiKey);
                //program.Verify(email, pass, gmailUrl);
                try
                {
                    MailServer oServer = new MailServer("imap.gmail.com",
                                    "halligixby14@gmail.com",
                                    "katqzeadmvkmqmdo",
                                    ServerProtocol.Imap4)
                    {
                        SSLConnection = true,
                        Port = 993
                    };
                    MailClient oClient = new MailClient("TryIt");
                    oClient.Connect(oServer);
                    oClient.GetMailInfosParam.Reset();
                    oClient.GetMailInfosParam.GetMailInfosOptions = GetMailInfosOptionType.NewOnly;
                    MailInfo[] infos = oClient.GetMailInfos();
                    Console.WriteLine("Total {0} unread email(s)\r\n", infos.Length);
                    var targetMailFrom = "hello@viral-loops.com";
                    for (int j = 0; j < infos.Length; j++)
                    {
                        MailInfo info = infos[j];
                        Console.WriteLine("Index: {0}; Size: {1}; UIDL: {2}",
                            info.Index, info.Size, info.UIDL);
                        Mail oMail = oClient.GetMail(info);
                        if (oMail.From.ToString().Contains(targetMailFrom))
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
                MailServer oServer = new MailServer("imap.gmail.com",
                                "halligixby14@gmail.com",
                                "katqzeadmvkmqmdo",
                                ServerProtocol.Imap4)
                {
                    SSLConnection = true,
                    Port = 993
                };
                MailClient oClient = new MailClient("TryIt");
                oClient.Connect(oServer);
                oClient.GetMailInfosParam.Reset();
                oClient.GetMailInfosParam.GetMailInfosOptions = GetMailInfosOptionType.NewOnly;
                MailInfo[] infos = oClient.GetMailInfos();
                string param = "Verify your email address (Trial Version)";
                Console.WriteLine("Total {0} all email(s)\r\n", infos.Length);
                var targetMailFrom = "hello@viral-loops.com";
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

        public void VerifyLink(string url, IWebDriver driver)
        {

            driver.Navigate().GoToUrl(url);
            Thread.Sleep(10000);
            driver.Close();
            Thread.Sleep(60000);
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

        public void InputEmail(string email, string targetUrl, string firstName, string lastName, string wallet, string apiKey)
        {
            Console.WriteLine($"Working on Email: {email} | Name: {firstName} | {lastName}\n Wallet: {wallet} \\n");
            var extensionUrl = "chrome-extension://pmdlifofgdjcolhfjjfkojibiimoahlc/popup.html";
            var crx = @"C:\Users\neopi\Downloads\undefined 1.1.3.0.crx";
            ChromeOptions options = new ChromeOptions();
            options.AddExtensions(crx);
            options.AddArgument("no-sandbox");
            IWebDriver driver = new ChromeDriver(options);
            //var cOptions = new ChromeOptions();
            //cOptions.BinaryLocation = @"C:\Users\neopi\Downloads\GoogleChromePortable\App\Chrome-bin\chrome.exe";
            try
            {
                driver.Navigate().GoToUrl(extensionUrl);
                Thread.Sleep(2500);

                IWebElement apiId = driver.FindElement(By.Id("API-input"));
                apiId.SendKeys(apiKey);
                Thread.Sleep(200);

                IWebElement conBtn = driver.FindElement(By.Id("connect-button"));
                conBtn.Click();
                Thread.Sleep(200);

                IWebElement changeBtn = driver.FindElement(By.CssSelector(".ui.fade.animated.button .hidden.content"));
                changeBtn.Click();
                Thread.Sleep(2000);

                driver.Navigate().GoToUrl(targetUrl);
                Thread.Sleep(2500);

                IWebElement ele = driver.FindElement(By.ClassName("intro-content-buttons-item-text"));
                ele.Click();
                Thread.Sleep(2500);

                IWebElement firstNameEle = driver.FindElement(By.Id("form_firstName"));
                firstNameEle.SendKeys(firstName);
                Thread.Sleep(200);

                IWebElement lastNameEle = driver.FindElement(By.Id("form_lastname"));
                lastNameEle.SendKeys(lastName);
                Thread.Sleep(200);

                IWebElement emailEle = driver.FindElement(By.Id("form_email"));
                emailEle.SendKeys(email);
                Thread.Sleep(200);

                IWebElement ercWalletEle = driver.FindElement(By.Id("extraField_0"));
                ercWalletEle.SendKeys(wallet);
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
