using System;
using System.Threading;
using OpenQA.Selenium;
using System.IO;
using Verifier.Models;
using SeleniumUndetectedChromeDriver;
using Newtonsoft.Json;
using Verifier.Extensions;
using ClosedXML.Excel;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Linq;
using DocumentFormat.OpenXml.Bibliography;
using System.Collections.Generic;
using System.Net;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Jpeg;
using Directory = System.IO.Directory;
using System.Drawing.Imaging;
using System.Drawing;
using System.Globalization;

namespace Verifier
{
    public class RedditService
    {
        private string _chromeLocationPath;
        private string _chromeDriverLocationPath;
        private DateTime _startTime;

        private const string EXTENSION_FOLDER = "autoAuthProxy";


        private int _totalRefSucceed = 0;
        private int _totalRefFailed = 0;

        private readonly string _downloadPath;
        private readonly string _excelPath;

        private readonly string _fileLink;

        private List<string> _linkSuccess = new List<string>();
        private List<string> _linkFailed = new List<string>();



        private const int DELAYPERSTEP = 500;

        private const int SaveColumn = 14;

        private const string DEFAULT_IRS_OPTION = "MD";

        private readonly UndetectedChromeDriver _undetectedDriver;
        private readonly ChromeDriver _driver;

        public RedditService(/*string downloadPath, string excelPath,*/ string chromeDriverPath, string fileLink)
        {
            ReadSettingFile();
            _startTime = DateTime.Now;
            _fileLink = fileLink;
            //_downloadPath = downloadPath;
            //_excelPath = excelPath;
            _chromeDriverLocationPath = chromeDriverPath;
            _undetectedDriver = CreateUndetectedChromeDriver();
            //_driver = CreateChromeDriver();
        }

        private UndetectedChromeDriver CreateUndetectedChromeDriver()
        {
            ChromeOptions options = new ChromeOptions();

            //string proxy = "922s5.proxys5.net:6300";
            //var directory = $"{Environment.CurrentDirectory}\\{EXTENSION_FOLDER}";
            //options.AddArguments($"--load-extension={directory}");
            //options.AddArguments("--proxy-server=" + proxy);

            options.AddArgument("--headless");
            return UndetectedChromeDriver.Create(/*options: options,*/ driverExecutablePath: _chromeDriverLocationPath + @"\chromedriver.exe", browserExecutablePath: _chromeLocationPath);
        }

        private ChromeDriver CreateChromeDriver()
        {
            var options = new ChromeOptions();
            options.AddArguments("--no-sandbox"); // Optional: Disable sandbox
            //options.AddUserProfilePreference("download.default_directory", _downloadPath);
            //options.AddUserProfilePreference("profile.default_content_settings.popups", 0);
            //options.AddUserProfilePreference("plugins.always_open_pdf_externally", true);
            // Create the ChromeDriver with options
            return new ChromeDriver(options);
        }

        public void StartSerivce()
        {
            Console.Write($"Auto IRS working on {_startTime}\n");
            //GlobalAppInput();
            var urls = File.ReadAllLines(_fileLink);
            foreach (var url in urls)
            {
                try
                {
                    ShopeeCraw(url);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An exception occurred for URL '{url}': {ex.Message}");
                    // You can choose to log the exception or take other actions if needed.
                }
            }

            long currentTick = DateTime.Now.Ticks;

            // Create a folder based on the current datetime tick
            string folderName = $"{currentTick}_results";
            string folderPath = Path.Combine(Environment.CurrentDirectory, folderName);

            // Create the folder if it doesn't exist
            Directory.CreateDirectory(folderPath);

            // Create paths for success and failure files
            string successFileName = Path.Combine(folderPath, "success.txt");
            string failureFileName = Path.Combine(folderPath, "failure.txt");

            File.WriteAllLines(successFileName, _linkSuccess);

            File.WriteAllLines(failureFileName, _linkFailed);
            Environment.Exit(0);

        }

        public void Menu()
        {
            //ShopeeCraw();

            var proxyUsername = "89692936-zone-custom";
            var proxyPassword = "OnNeKxA7";
            var js = $"document.getElementById('login').value = '{proxyUsername}'; document.getElementById('password').value = '{proxyPassword}'; document.getElementById('save').click();\r\n";

            ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript(js);

            _undetectedDriver.Navigate().GoToUrl("https://ipinfo.io");
            var saveColumn = SaveColumn;

            using (var workbook = new XLWorkbook(_excelPath))
            {
                var worksheet = workbook.Worksheet(1); // Assuming the data is in the first worksheet

                var range = worksheet.RangeUsed();

                foreach (var row in range.Rows().Skip(4))
                {
                    var rowNumber = row.RowNumber();

                    try
                    {
                        var entity = row.MapToEntity();
                        //var saveData = TestIRS(entity, _downloadPath);
                        var saveData = TestIRSDriver(entity, _downloadPath);


                        var eincodeCell = worksheet.Cell(rowNumber, saveColumn);
                        eincodeCell.Value = saveData.EinCode;

                        var filenameCell = worksheet.Cell(rowNumber, saveColumn + 1);
                        filenameCell.Value = saveData.FileName;
                    }
                    catch (Exception ex)
                    {
                        // Handle the exception, log, and decide whether to continue or break the loop
                        // Example: Log the exception and continue processing the next row
                        continue;
                    }
                }

                // Save changes after processing all rows in the using block
                workbook.Save();
            }

            LogInformation();
        }

        private void ShopeeCraw(string url)
        {
            try
            {
                _undetectedDriver.Navigate().GoToUrl(url);
                Thread.Sleep(2000);
                var titleEle = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"main\"]/div[1]/div[2]/div[1]/div[1]/div[1]/div[1]/section[1]/section[2]/div[1]/div[1]/span[1]"));
                var title = titleEle.Text;

                string folderPath = Path.Combine(Environment.CurrentDirectory, title);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    Console.WriteLine($"Folder '{titleEle}' created.");
                }
                else
                {
                    Console.WriteLine($"Folder '{titleEle}' already exists.");
                }


                var imageSection = _undetectedDriver.GetWebElementUntilSuccess(By.ClassName("XjROLg"));

                // Find all the image elements within the section
                IReadOnlyCollection<IWebElement> imageElements = imageSection.FindElements(By.TagName("img"));

                int imageCount = 1;
                foreach (IWebElement imgElement in imageElements)
                {
                    string imageUrl = imgElement.GetAttribute("src");

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        // Download the image
                        using (WebClient client = new WebClient())
                        {
                            string fileName = $"image_{imageCount}.jpg"; // You can change the file format if needed
                            string filePath = Path.Combine(folderPath, fileName);
                            client.DownloadFile(imageUrl, filePath);

                            using (Bitmap cloneImage = new Bitmap(filePath))
                            {
                                // Remove metadata (EXIF data) from the clone
                                foreach (PropertyItem property in cloneImage.PropertyItems)
                                {
                                    cloneImage.RemovePropertyItem(property.Id);
                                }

                                // Save the clone as the original image without metadata (overwriting)
                                cloneImage.Save(fileName, ImageFormat.Jpeg);
                            }
                        }
                    }
                }

                var descriptionEle = _undetectedDriver.GetWebElementUntilSuccess(By.ClassName("irIKAp"));
                var description = descriptionEle.Text;
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                title = textInfo.ToTitleCase(title);
                description = textInfo.ToTitleCase(description);

                string txtFileName = "title_description.txt";
                string txtPath = Path.Combine(folderPath, txtFileName);
                using (StreamWriter writer = new StreamWriter(txtPath))
                {
                    // Write title
                    writer.WriteLine(title);
                    writer.WriteLine("-------------");
                    writer.WriteLine(description);
                }

                _linkSuccess.Add(url);
            }
            catch (Exception)
            {
                _linkFailed.Add(url);
                throw;
            }
        }

        public void TestPdf()
        {

            var setting = "chrome://settings/content/pdfDocuments";
            var registerUrl = "https://www.africau.edu/images/default/sample.pdf";
            _driver.Navigate().GoToUrl(registerUrl);

            Thread.Sleep(DELAYPERSTEP);
            Console.ReadKey();
        }


        public SaveDataIrs TestIRS(IrsEntity entity, string downloadPath)
        {
            var registerUrl = "https://sa.www4.irs.gov/modiein/individual/index.jsp";
            try
            {
                var ssnArr = entity.SSN.Split(' ');
                Console.WriteLine($"Working on: {entity.Id} | {entity.FirstName} {entity.LastName}");

                _undetectedDriver.Navigate().GoToUrl(registerUrl);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement warningTxt = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("div#topcontent > h2"));
                if (warningTxt != null && warningTxt.Text == "Our online assistant is currently unavailable.")
                {
                    warningTxt.Text.LogWithColor(ConsoleColor.DarkRed);
                    LogInformation();
                    Environment.Exit(0);
                }

                IWebElement beginBtn = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"topcontent\"]/form[1]/div[5]/input[1]"));
                beginBtn.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement soleSelection = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#sole"));
                soleSelection.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("div#individual-leftcontent > form > div:nth-of-type(16) > input"));
                continueBtn.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement soleSelection2 = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#sole"));
                soleSelection2.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn2 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[6]/input[1]"));
                continueBtn2.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn3 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[2]/input[1]"));
                continueBtn3.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement newbiz = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#newbiz"));
                newbiz.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn4 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[11]/input[1]"));
                continueBtn4.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement firstNameTxt = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantFirstName\"]"));
                firstNameTxt.SendKeys(entity.FirstName);
                Thread.Sleep(DELAYPERSTEP);

                //IWebElement middleNameTxt = GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantMiddleName\"]"));
                //middleNameTxt.SendKeys("mid");

                IWebElement lastNameTxt = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantLastName\"]"));
                lastNameTxt.SendKeys(entity.LastName);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement suffixDropdown = _undetectedDriver.GetWebElementUntilSuccess(By.Id("applicantSuffix"));
                SelectElement selectElement = new SelectElement(suffixDropdown);
                selectElement.SelectByValue(DEFAULT_IRS_OPTION);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement ssnText1 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantSSN3\"]"));
                ssnText1.SendKeys(ssnArr[0]);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement ssnText2 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantSSN2\"]"));
                ssnText2.SendKeys(ssnArr[1]);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement ssnText3 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantSSN4\"]"));
                ssnText3.SendKeys(ssnArr[2]);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement iamsole = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#iamsole"));
                iamsole.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn5 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[5]/input[1]"));
                continueBtn5.Click();
                Thread.Sleep(DELAYPERSTEP);

                #region step3

                IWebElement streetTxt = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#physicalAddressStreet"));
                streetTxt.SendKeys(entity.Street);
                Thread.Sleep(DELAYPERSTEP);


                IWebElement cityTxt = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#physicalAddressCity"));
                cityTxt.SendKeys(entity.City);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement stateDropdown = _undetectedDriver.GetWebElementUntilSuccess(By.Id("physicalAddressState"));
                SelectElement selectState = new SelectElement(stateDropdown);
                selectState.SelectByValue(entity.State);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement zipCodeTxt = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#physicalAddressZipCode"));
                zipCodeTxt.SendKeys(entity.ZipCode);
                Thread.Sleep(DELAYPERSTEP);

                var phoneArr = entity.Phone.Split(' ');

                IWebElement phone1 = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#phoneFirst3"));
                phone1.SendKeys(phoneArr[0]);
                Thread.Sleep(DELAYPERSTEP);


                IWebElement phone2 = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#phoneMiddle3"));
                phone2.SendKeys(phoneArr[1]);
                Thread.Sleep(DELAYPERSTEP);


                IWebElement phone3 = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#phoneLast4"));
                phone3.SendKeys(phoneArr[2]);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn6 = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("div#individual-leftcontent > form > div:nth-of-type(3) > input"));
                continueBtn6.Click();
                Thread.Sleep(DELAYPERSTEP);

                #endregion


                IWebElement monthDropdown = _undetectedDriver.GetWebElementUntilSuccess(By.Id("BUSINESS_OPERATIONAL_MONTH_ID"));
                SelectElement selectMonth = new SelectElement(monthDropdown);
                selectMonth.SelectByValue("3");
                Thread.Sleep(DELAYPERSTEP);

                IWebElement soleYear = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#BUSINESS_OPERATIONAL_YEAR_ID"));
                soleYear.SendKeys("2023");
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn7 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[4]/input[1]"));
                continueBtn7.Click();
                Thread.Sleep(DELAYPERSTEP);

                ((IJavaScriptExecutor)_undetectedDriver).ExecuteScript("document.querySelectorAll('input[type=\"radio\"]').forEach(function(checkbox) { if (checkbox.value === \"false\") { checkbox.click();}});");
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn8 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[4]/input[1]"));
                continueBtn8.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement retailSelection = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#retail"));
                retailSelection.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn9 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[31]/input[1]"));
                continueBtn9.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement sellingGoods = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#selling"));
                sellingGoods.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn10 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[7]/input[1]"));
                continueBtn10.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement receiveonline = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("input#receiveonline"));
                receiveonline.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn11 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"space\"]/input[1]"));
                continueBtn11.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement submit = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/table[1]/tbody[1]/tr[1]/td[3]/input[1]"));
                submit.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement eincodeTxt = _undetectedDriver.GetWebElementUntilSuccess(By.CssSelector("div#confirmation-table > table > tbody > tr > td:nth-of-type(2) > b"));
                var saveData = eincodeTxt.Text;

                IWebElement fileHref = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"confirmation-leftcontent\"]/a[1]/b[1]"));
                fileHref.Click();
                Thread.Sleep(DELAYPERSTEP);

                string originalWindowHandle = _undetectedDriver.CurrentWindowHandle;
                foreach (string windowHandle in _undetectedDriver.WindowHandles)
                {
                    if (windowHandle != originalWindowHandle)
                    {
                        _undetectedDriver.SwitchTo().Window(windowHandle);
                        break;
                    }
                }

                string newWindowUrl = _undetectedDriver.Url;

                //ChromeOptions chromeOptions = new ChromeOptions();
                //chromeOptions.AddUserProfilePreference("download.default_directory", _downloadPath);
                //chromeOptions.AddUserProfilePreference("profile.default_content_settings.popups", 0);
                //chromeOptions.AddUserProfilePreference("plugins.always_open_pdf_externally", true);

                //IWebDriver driver = new ChromeDriver(chromeOptions);
                //driver.Navigate().GoToUrl(newWindowUrl);

                string extractedFilename = System.IO.Path.GetFileName(new Uri(newWindowUrl).LocalPath);
                string downloadedFilePath = System.IO.Path.Combine(downloadPath, extractedFilename);

                IWebElement continueBtn12 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[1]/div[5]/input[1]"));
                continueBtn12.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn13 = _undetectedDriver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[4]/input[1]"));
                continueBtn13.Click();
                Thread.Sleep(DELAYPERSTEP);


                $"Success: {entity.Id} | {entity.FirstName} {entity.LastName}".LogWithColor(ConsoleColor.DarkGreen);
                _totalRefSucceed++;

                return new SaveDataIrs { EinCode = saveData, FileName = extractedFilename };

            }
            catch (Exception)
            {
                $"Failed: {entity.Id} | {entity.FirstName} {entity.LastName}".LogWithColor(ConsoleColor.Red);
                _totalRefFailed++;
                throw;
            }
            //finally
            //{
            //    _undetectedDriver?.Dispose();
            //}
        }

        public SaveDataIrs TestIRSDriver(IrsEntity entity, string downloadPath)
        {
            var registerUrl = "https://sa.www4.irs.gov/modiein/individual/index.jsp";
            try
            {
                var ssnArr = entity.SSN.Split(' ');
                Console.WriteLine($"Working on: {entity.Id} | {entity.FirstName} {entity.LastName}");

                _driver.Navigate().GoToUrl(registerUrl);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement warningTxt = _driver.GetWebElementUntilSuccess(By.CssSelector("div#topcontent > h2"));
                if (warningTxt != null && warningTxt.Text == "Our online assistant is currently unavailable.")
                {
                    warningTxt.Text.LogWithColor(ConsoleColor.DarkRed);
                    LogInformation();
                    Environment.Exit(0);
                }

                IWebElement beginBtn = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"topcontent\"]/form[1]/div[5]/input[1]"));
                beginBtn.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement soleSelection = _driver.GetWebElementUntilSuccess(By.CssSelector("input#sole"));
                soleSelection.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn = _driver.GetWebElementUntilSuccess(By.CssSelector("div#individual-leftcontent > form > div:nth-of-type(16) > input"));
                continueBtn.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement soleSelection2 = _driver.GetWebElementUntilSuccess(By.CssSelector("input#sole"));
                soleSelection2.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn2 = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[6]/input[1]"));
                continueBtn2.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn3 = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[2]/input[1]"));
                continueBtn3.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement newbiz = _driver.GetWebElementUntilSuccess(By.CssSelector("input#newbiz"));
                newbiz.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn4 = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[11]/input[1]"));
                continueBtn4.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement firstNameTxt = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantFirstName\"]"));
                firstNameTxt.SendKeys(entity.FirstName);
                Thread.Sleep(DELAYPERSTEP);

                //IWebElement middleNameTxt = GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantMiddleName\"]"));
                //middleNameTxt.SendKeys("mid");

                IWebElement lastNameTxt = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantLastName\"]"));
                lastNameTxt.SendKeys(entity.LastName);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement suffixDropdown = _driver.GetWebElementUntilSuccess(By.Id("applicantSuffix"));
                SelectElement selectElement = new SelectElement(suffixDropdown);
                selectElement.SelectByValue(DEFAULT_IRS_OPTION);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement ssnText1 = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantSSN3\"]"));
                ssnText1.SendKeys(ssnArr[0]);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement ssnText2 = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantSSN2\"]"));
                ssnText2.SendKeys(ssnArr[1]);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement ssnText3 = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"applicantSSN4\"]"));
                ssnText3.SendKeys(ssnArr[2]);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement iamsole = _driver.GetWebElementUntilSuccess(By.CssSelector("input#iamsole"));
                iamsole.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn5 = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[5]/input[1]"));
                continueBtn5.Click();
                Thread.Sleep(DELAYPERSTEP);

                #region step3

                IWebElement streetTxt = _driver.GetWebElementUntilSuccess(By.CssSelector("input#physicalAddressStreet"));
                streetTxt.SendKeys(entity.Street);
                Thread.Sleep(DELAYPERSTEP);


                IWebElement cityTxt = _driver.GetWebElementUntilSuccess(By.CssSelector("input#physicalAddressCity"));
                cityTxt.SendKeys(entity.City);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement stateDropdown = _driver.GetWebElementUntilSuccess(By.Id("physicalAddressState"));
                SelectElement selectState = new SelectElement(stateDropdown);
                selectState.SelectByValue(entity.State);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement zipCodeTxt = _driver.GetWebElementUntilSuccess(By.CssSelector("input#physicalAddressZipCode"));
                zipCodeTxt.SendKeys(entity.ZipCode);
                Thread.Sleep(DELAYPERSTEP);

                var phoneArr = entity.Phone.Split(' ');

                IWebElement phone1 = _driver.GetWebElementUntilSuccess(By.CssSelector("input#phoneFirst3"));
                phone1.SendKeys(phoneArr[0]);
                Thread.Sleep(DELAYPERSTEP);


                IWebElement phone2 = _driver.GetWebElementUntilSuccess(By.CssSelector("input#phoneMiddle3"));
                phone2.SendKeys(phoneArr[1]);
                Thread.Sleep(DELAYPERSTEP);


                IWebElement phone3 = _driver.GetWebElementUntilSuccess(By.CssSelector("input#phoneLast4"));
                phone3.SendKeys(phoneArr[2]);
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn6 = _driver.GetWebElementUntilSuccess(By.CssSelector("div#individual-leftcontent > form > div:nth-of-type(3) > input"));
                continueBtn6.Click();
                Thread.Sleep(DELAYPERSTEP);

                #endregion


                IWebElement monthDropdown = _driver.GetWebElementUntilSuccess(By.Id("BUSINESS_OPERATIONAL_MONTH_ID"));
                SelectElement selectMonth = new SelectElement(monthDropdown);
                selectMonth.SelectByValue("3");
                Thread.Sleep(DELAYPERSTEP);

                IWebElement soleYear = _driver.GetWebElementUntilSuccess(By.CssSelector("input#BUSINESS_OPERATIONAL_YEAR_ID"));
                soleYear.SendKeys("2023");
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn7 = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[4]/input[1]"));
                continueBtn7.Click();
                Thread.Sleep(DELAYPERSTEP);

                ((IJavaScriptExecutor)_driver).ExecuteScript("document.querySelectorAll('input[type=\"radio\"]').forEach(function(checkbox) { if (checkbox.value === \"false\") { checkbox.click();}});");
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn8 = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[4]/input[1]"));
                continueBtn8.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement retailSelection = _driver.GetWebElementUntilSuccess(By.CssSelector("input#retail"));
                retailSelection.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn9 = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[31]/input[1]"));
                continueBtn9.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement sellingGoods = _driver.GetWebElementUntilSuccess(By.CssSelector("input#selling"));
                sellingGoods.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn10 = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[7]/input[1]"));
                continueBtn10.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement receiveonline = _driver.GetWebElementUntilSuccess(By.CssSelector("input#receiveonline"));
                receiveonline.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn11 = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"space\"]/input[1]"));
                continueBtn11.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement submit = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/table[1]/tbody[1]/tr[1]/td[3]/input[1]"));
                submit.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement eincodeTxt = _driver.GetWebElementUntilSuccess(By.CssSelector("div#confirmation-table > table > tbody > tr > td:nth-of-type(2) > b"));
                var saveData = eincodeTxt.Text;

                IWebElement fileHref = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"confirmation-leftcontent\"]/a[1]/b[1]"));
                fileHref.Click();
                Thread.Sleep(DELAYPERSTEP);

                //string originalWindowHandle = _driver.CurrentWindowHandle;
                //foreach (string windowHandle in _driver.WindowHandles)
                //{
                //    if (windowHandle != originalWindowHandle)
                //    {
                //        _driver.SwitchTo().Window(windowHandle);
                //        break;
                //    }
                //}

                string newWindowUrl = _driver.Url;

                //ChromeOptions chromeOptions = new ChromeOptions();
                //chromeOptions.AddUserProfilePreference("download.default_directory", _downloadPath);
                //chromeOptions.AddUserProfilePreference("profile.default_content_settings.popups", 0);
                //chromeOptions.AddUserProfilePreference("plugins.always_open_pdf_externally", true);

                //IWebDriver driver = new ChromeDriver(chromeOptions);
                //driver.Navigate().GoToUrl(newWindowUrl);

                string extractedFilename = System.IO.Path.GetFileName(new Uri(newWindowUrl).LocalPath);
                string downloadedFilePath = System.IO.Path.Combine(downloadPath, extractedFilename);

                IWebElement continueBtn12 = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[1]/div[5]/input[1]"));
                continueBtn12.Click();
                Thread.Sleep(DELAYPERSTEP);

                IWebElement continueBtn13 = _driver.GetWebElementUntilSuccess(By.XPath("//*[@id=\"individual-leftcontent\"]/form[1]/div[4]/input[1]"));
                continueBtn13.Click();
                Thread.Sleep(DELAYPERSTEP);


                $"Success: {entity.Id} | {entity.FirstName} {entity.LastName}".LogWithColor(ConsoleColor.DarkGreen);
                _totalRefSucceed++;

                return new SaveDataIrs { EinCode = saveData, FileName = extractedFilename };

            }
            catch (Exception)
            {
                $"Failed: {entity.Id} | {entity.FirstName} {entity.LastName}".LogWithColor(ConsoleColor.Red);
                _totalRefFailed++;
                throw;
            }
            //finally
            //{
            //    _driver?.Dispose();
            //}
        }

        private void LogInformation()
        {
            $"Total success: {_totalRefSucceed}".LogWithColor(ConsoleColor.DarkGreen);
            $"Total failed: {_totalRefFailed}".LogWithColor(ConsoleColor.DarkRed);
            _startTime.LogRunTime();
        }

        //private void GlobalAppInput()
        //{
        //    //Console.WriteLine("Enter Proxy Api:");
        //    //ApiKey = Console.ReadLine().Trim();
        //    Console.WriteLine("Enter Chrome Driver Path:");
        //    ChromeDriverLocationPath = Console.ReadLine().Trim();
        //}

        private void MappingAppsetting(CustomConfigModel customConfig)
        {
            _chromeLocationPath = customConfig.ChromeLocationPath;
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
