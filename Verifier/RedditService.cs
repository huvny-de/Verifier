using System;
using System.Threading;
using OpenQA.Selenium;
using System.IO;
using Verifier.Models;
using SeleniumUndetectedChromeDriver;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Verifier.Extensions;
using ClosedXML.Excel;

namespace Verifier
{
    public class RedditService
    {
        private static string ChromeLocationPath { get; set; }
        private static string ChromeDriverLocationPath { get; set; }
        private static string ApiKey { get; set; }

        private static int TotalRefSucceed { get; set; } = 0;
        private static int TotalRefFailed { get; set; } = 0;
        private const int SaveColumn = 14;

        private static string DEFAULT_IRS_OPTION = "MD";


        private static UndetectedChromeDriver _undetectedDriver;

        public async Task StartSerivce()
        {
            Console.Write($"Auto IRS working on {DateTime.Now}\n");
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
                    "1. Auto Ref\n"
                    );
                runType = Convert.ToInt32(Console.ReadLine().Trim());
                switch (runType)
                {
                    case 1:
                        Console.WriteLine("Input Excel Location Path:");
                        var excelPath = Console.ReadLine();

                        using (var workbook = new XLWorkbook(excelPath))
                        {
                            var worksheet = workbook.Worksheet(1); // Assuming the data is in the first worksheet

                            var range = worksheet.RangeUsed();

                            foreach (var row in range.Rows())
                            {
                                var entity = row.MapToEntity();
                                var saveData = await TestIRS(entity);
                                var saveColumn = SaveColumn;
                                var targetCell = worksheet.Cell(row.RowNumber(), saveColumn);
                                targetCell.Value = saveData;
                            }

                            workbook.Save();
                        }

                        startTime.LogRunTime();
                        break;
                }
            } while (runType != 3);
        }

        public async Task<string> TestIRS(IrsEntity entity)
        {
            var registerUrl = "https://sa.www4.irs.gov/modiein/individual/index.jsp";
            try
            {
                var ssnArr = entity.SSN.Split(' ');

                Console.WriteLine($"Working on: {entity.Id} | {entity.FirstName} {entity.LastName}");

                //var httpsProxy = GetNewProxyOnly(ApiKey);
                //ChromeOptions options = new ChromeOptions();
                //options.AddArguments("--proxy-server=" + $"{httpsProxy[0]}" + ":" + $"{httpsProxy[1]}");
                _undetectedDriver = UndetectedChromeDriver.Create(/*options: options,*/ driverExecutablePath: ChromeDriverLocationPath + @"\chromedriver.exe", browserExecutablePath: ChromeLocationPath);
                _undetectedDriver.Navigate().GoToUrl(registerUrl);
                Thread.Sleep(500);

                IWebElement beginBtn = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"topcontent\"]/form[1]/div[5]/input[1]"));
                beginBtn.Click();

                IWebElement soleSelection = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#sole"));
                soleSelection.Click();

                IWebElement continueBtn = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[16]/input[1]"));
                continueBtn.Click();

                IWebElement soleSelection2 = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#sole"));
                soleSelection2.Click();

                IWebElement continueBtn2 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[6]/input[1]"));
                continueBtn2.Click();

                IWebElement continueBtn3 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[2]/input[1]"));
                continueBtn3.Click();

                IWebElement newbiz = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#newbiz"));
                newbiz.Click();

                IWebElement continueBtn4 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[11]/input[1]"));
                continueBtn4.Click();


                IWebElement firstNameTxt = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantFirstName\"]"));
                firstNameTxt.SendKeys(entity.FirstName);

                //IWebElement middleNameTxt = GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantMiddleName\"]"));
                //middleNameTxt.SendKeys("mid");

                IWebElement lastNameTxt = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantLastName\"]"));
                lastNameTxt.SendKeys(entity.LastName);

                ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("document.getElementById('applicantSuffix').value = 'MD';");
                Thread.Sleep(500);

                IWebElement ssnText1 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantSSN3\"]"));
                ssnText1.SendKeys(ssnArr[0]);

                IWebElement ssnText2 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantSSN2\"]"));
                ssnText2.SendKeys(ssnArr[1]);

                IWebElement ssnText3 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantSSN4\"]"));
                ssnText3.SendKeys(ssnArr[2]);

                IWebElement iamsole = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#iamsole"));
                iamsole.Click();

                IWebElement continueBtn5 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[5]/input[1]"));
                continueBtn5.Click();


                $"Success: {entity.Id} | {entity.FirstName} {entity.LastName}".LogWithColor(ConsoleColor.DarkGreen);
                TotalRefSucceed++;

                var saveData = "Save";
                return saveData;

            }
            catch (Exception)
            {
                $"Failed: {entity.Id} | {entity.FirstName} {entity.LastName}".LogWithColor(ConsoleColor.Red);
                TotalRefFailed++;
                throw;
            }
            finally
            {
                _undetectedDriver?.Dispose();
            }
        }

        private void GlobalAppInput()
        {
            Console.WriteLine("Enter Proxy Api:");
            ApiKey = Console.ReadLine().Trim();
            Console.WriteLine("Enter Chrome Driver Path:");
            ChromeDriverLocationPath = Console.ReadLine().Trim();
        }

        private void MappingAppsetting(CustomConfigModel customConfig)
        {
            ChromeLocationPath = customConfig.ChromeLocationPath;
        }

        private void ReadSettingFile()
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
                CommonExtension.CreateSettingFile();
                Console.ReadLine();
                return;
            }
            var customConfig = JsonConvert.DeserializeObject<CustomConfigModel>(rs);

            foreach (var property in customConfig.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    string value = (string)property.GetValue(customConfig);
                    if (string.IsNullOrEmpty(value))
                    {
                        Console.WriteLine($"Please Input AppSetting!\nPath: {Directory.GetCurrentDirectory()}");
                        Console.ReadLine();
                        return;
                    }
                }
            }

            MappingAppsetting(customConfig);
        }
    }
}
