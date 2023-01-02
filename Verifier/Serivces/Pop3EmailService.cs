using MailKit.Net.Pop3;
using MimeKit;
using System;

namespace Verifier.Serivces
{
    public class Pop3EmailService
    {
        string host = "pop3.live.com";
        int port = 995;
        bool useSsl = true;

        public void ReadEmail()
        {
            string username = "bookulrasyidk@hotmail.com";
            string password = "EHE3v445";
            Pop3Client client = new Pop3Client();
            try
            {
                client.Connect(host, port, useSsl);
                client.Authenticate(username, password);

                int messageCount = client.GetMessageCount();
                for (int i = 0; i < messageCount; i++)
                {
                    MimeMessage message = client.GetMessage(i);
                    Console.WriteLine("Subject: " + message.Subject);
                    Console.WriteLine("From: " + message.From);
                    Console.WriteLine("Body: " + message.TextBody);
                }

                client.Disconnect(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to read email from Hotmail account: " + ex.Message);
            }
        }
    }
}
