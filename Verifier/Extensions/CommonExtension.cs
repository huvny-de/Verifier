using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verifier.Models;

namespace Verifier.Extensions
{
    public static class CommonExtension
    {

        public static void CreateSettingFile()
        {
            var customConfig = new CustomConfigModel();
            var json = JsonConvert.SerializeObject(customConfig);
            File.WriteAllText(Directory.GetCurrentDirectory() + @"\AppSetting.json", json);
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
        public static string CreateOrUpdateFile(string folderName, string fileName = "VerifyLinks", bool shoudNew = false)
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

        public static int GetRandomFC()
        {
            int[] randomFB = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 };
            Random random = new Random();
            int locationId = random.Next(0, randomFB.Length);
            return locationId;
        }

        

        public static string SaveUrl(string url, string folderName, string fileName)
        {
            var filePath = CreateOrUpdateFile(folderName, fileName);
            using (StreamWriter writer = new StreamWriter(filePath, append: true))
            {
                writer.WriteLine(url, 0, url.Length);
                writer.Close();
            }
            return filePath;
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
