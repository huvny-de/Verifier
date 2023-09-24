using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Verifier;

public class Program
{
    private static void Main(string[] args)
    {
        //var chromeDriverLocationPath = @"C:\Users\SE140364\Downloads\chromedriver-win64\chromedriver-win64";
        //var excelPath = @"C:\Users\SE140364\Downloads\Telegram Desktop\4154056404009026144new_microsoft_excel_worksheet.xlsx";
        //string downloadPath = @"C:\Users\SE140364\Desktop\IRS\FileDown";


        Console.WriteLine("Enter Chrome Driver Path:");
        var chromeDriverLocationPath = Console.ReadLine().Trim();

        Console.WriteLine("Enter file link");
        var fileLinkPath = Console.ReadLine().Trim();

        //Console.WriteLine("Input Excel Location Path:");
        //var excelPath = Console.ReadLine();

        //Console.WriteLine("Enter the download path: ");
        //string downloadPath = Console.ReadLine();



        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<BgSerivce>();
                services.AddScoped(provider => new RedditService(/*downloadPath, excelPath,*/ chromeDriverLocationPath, fileLinkPath));
            })
            .Build();

        host.Run();
    }
}