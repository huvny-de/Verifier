using OpenQA.Selenium;
using SeleniumUndetectedChromeDriver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Verifier.Extensions
{
    public static class WebDriverExtensions
    {
        public static IWebElement GetWebElement(this UndetectedChromeDriver undetectedDriver, By by)
        {
            try
            {
                return undetectedDriver.FindElement(by);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static IWebElement GetWebElementUntilSuccess(this UndetectedChromeDriver undetectedDriver, By by, int maxWork = 10)
        {
            var workTime = 0;
            var ele = undetectedDriver.GetWebElement(by);
            while (ele == null && workTime < maxWork)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"Trying get element {workTime + 1}");
                ele = undetectedDriver.GetWebElement(by);
                workTime++;
            }
            return ele;
        }

        public static List<IWebElement> GetWebElements(this UndetectedChromeDriver undetectedDriver, By by)
        {
            try
            {
                return undetectedDriver.FindElements(by).ToList();

            }
            catch (Exception)
            {
                return null;
            }
        }

        public static List<IWebElement> GetWebElementsUntilSuccess(this UndetectedChromeDriver undetectedDriver, By by)
        {
            var maxTime = 0;
            var ele = undetectedDriver.GetWebElements(by);
            while (ele == null && maxTime < 10)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"Trying get list element {maxTime + 1}");
                ele = undetectedDriver.GetWebElements(by);
                maxTime++;
            }
            return ele;
        }

        public static bool TryClickButton(IWebElement ele)
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

        public static void ClickUntilSuccess(IWebElement ele)
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
    }
}
