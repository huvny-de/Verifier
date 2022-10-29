using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;

namespace Verifier.Extensions
{
    public static class WebDriverExtensions
    {
        public static IWebElement FindElementWait(this IWebDriver driver, By by, int timeoutInSeconds)
        {
            if (timeoutInSeconds > 0)
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
                return wait.Until(drv => drv.FindElement(by));
            }
            return driver.FindElement(by);
        }
    }
}
