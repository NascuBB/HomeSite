using System.Net.Mail;
using System.Net;

namespace HomeSite.Managers
{
    public class EmailManager
    {
        public static async Task SendCodeEmailAsync(string toEmail, int code)
        {
            var fromAddress = new MailAddress("noreply@" + ConfigManager.Domain, "Just1x");
            var toAddress = new MailAddress(toEmail);
            const string subject = "Подтверждение почты";
            string body = "Здравствуйте!\r\n\r\n" +
                "Для подтверждения вашей электронной почты, пожалуйста, введите следующий код на сайте:\r\n\r\n" +
                code +"\r\n\r\nКод действителен в течение 10 минут.\r\n\r\nЕсли вы не регистрировались на этом сайте, просто проигнорируйте это письмо.\r\n\r\nС уважением,\r\nЖастикс";

            string realFromEmail = ConfigManager.RealEmail!;
            string fromPassword = ConfigManager.SMTPKey!;

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(realFromEmail, fromPassword)
            };

            using var message = new MailMessage
            {
                From = fromAddress, // псевдодомен
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(toAddress);

            await smtp.SendMailAsync(message);
        }

        public static async Task SendPassRestoreEmailAsync(string toEmail, string code)
        {
            var fromAddress = new MailAddress("noreply@" + ConfigManager.Domain, "Just1x");
            var toAddress = new MailAddress(toEmail);
            const string subject = "Восстановление пароля";
            string body = "Здравствуйте!\r\n\r\n" +
                "Вы запросили восстановление пароля. Пожалуйста, перейдите по ссылке ниже, чтобы установить новый пароль:\r\n\r\n" +
                $"{ConfigManager.Domain}/account/resetpassword/{code}\r\n\r\n" +
                "Ссылка будет активна в течение 1 часа с момента запроса.\r\n\r\n" +
                "Если вы не запрашивали восстановление пароля, просто проигнорируйте это письмо — никаких действий предпринимать не нужно.\r\n\r\n" +
                "С уважением,\r\n" +
                "Жастикс\r\n\r\n";

            string realFromEmail = ConfigManager.RealEmail!;
            string fromPassword = ConfigManager.SMTPKey!;

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(realFromEmail, fromPassword)
            };

            using var message = new MailMessage
            {
                From = fromAddress, // псевдодомен
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(toAddress);

            await smtp.SendMailAsync(message);
        }

        public static async Task SendFileDeletionEmailAsync(string toEmail)
        {
            var fromAddress = new MailAddress("noreply@" + ConfigManager.Domain, "Just1x");
            var toAddress = new MailAddress(toEmail);
            const string subject = "Ваши файлы будут удалены через 7 дней — требуется вход в аккаунт";
            string body = "Здравствуйте!\r\n\r\n" +
                "В соответствии с нашей политикой, уведомляем вас о том, что файлы, хранимые на вашем аккаунте, будут удалены через 7 дней из-за отсутствия входа на сайт в течение последних 60 дней.\r\n\r\n" +
                "Если вам необходимо сохранить эти данные, пожалуйста, зайдите на сайт и выполните вход в свой аккаунт. Это позволит нам подтвердить актуальность вашего аккаунта и сохранить ваши файлы.\r\n\r\n" +
                "\r\n\r\nС уважением," +
                "Жастикс";

            string realFromEmail = ConfigManager.RealEmail!;
            string fromPassword = ConfigManager.SMTPKey!;

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(realFromEmail, fromPassword)
            };

            using var message = new MailMessage
            {
                From = fromAddress, // псевдодомен
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(toAddress);

            await smtp.SendMailAsync(message);
        }
    }
}
