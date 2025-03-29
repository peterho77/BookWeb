using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Book.Utility
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        public EmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }
        public async Task SendEmailAsync(string email, string subject, string HtmlMessage)
        {
            //logic send email
            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));

            var emailList = email.Split(',');
            foreach(var e in emailList)
            {
                emailMessage.To.Add(new MailboxAddress("", e));
            } 
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = HtmlMessage };

            using (var client = new SmtpClient())
            { 
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, true);
                await client.AuthenticateAsync(_emailSettings.FromEmail, _emailSettings.Password);
                await client.SendAsync(emailMessage);

                await client.DisconnectAsync(true);
            }
        }
    }
}
