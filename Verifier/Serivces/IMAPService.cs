using EAGetMail;
using Nethereum.JsonRpc.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verifier.Models;

namespace Verifier.Serivces
{
    public class IMAPService
    {
        private readonly string Server = "outlook.office365.com";
        private readonly int Port = 993;


        public MailClient CreateClient(string email, string password)
        {
            MailServer oServer = new MailServer(Server,
                              email,
                               password,
                               ServerProtocol.Imap4)
            {
                SSLConnection = true,
                Port = Port
            };
            MailClient oClient = new MailClient("TryIt");
            oClient.Connect(oServer);
            return oClient;
        }


        public Mail GetEmail(MailClient mailClient, string currentEmail)
        {
            try
            {
                string subjectSearch = "Log in to Nifty's";
                mailClient.GetMailInfosParam.Reset();
                mailClient.GetMailInfosParam.SubjectContains = subjectSearch;
                MailInfo[] infos = mailClient.GetMailInfos();
                Console.WriteLine("Total {0} all email(s)\r\n", infos.Length);
                foreach (MailInfo info in infos)
                {
                    Mail mail = mailClient.GetMail(info);
                    if (mail.TextBody.Contains(currentEmail))
                    {
                        Console.WriteLine("Subject: " + mail.Subject);
                        Console.WriteLine("From: " + mail.From);
                        Console.WriteLine("Body: " + mail.TextBody);
                        if (!info.Read)
                        {
                            mailClient.MarkAsRead(info, true);
                        }
                        return mail;
                    }
                }
                return null;
            }
            catch (Exception ep)
            {
                Console.WriteLine(ep.Message);
                return null;
            }
        }

        public void Disconnect(MailClient mailClient)
        {
            mailClient?.Quit();
        }
    }
}
