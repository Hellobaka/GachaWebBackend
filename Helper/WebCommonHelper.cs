using GachaWebBackend.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace GachaWebBackend.Helper
{
    public static class WebCommonHelper
    {
        private const string _SALT = "33ae826c28108ccfd6890ff6d942be89";
        public static ApiResponse SetOk(string msg = "ok", object data = null)
        {
            return new ApiResponse { code = 200, msg = msg, data = data };
        }
        public static ApiResponse SetError(string msg = "error", object data = null)
        {
            return new ApiResponse { code = 404, msg = msg, data = data };
        }
        public static bool CompareKeyProp(this WebUser user, WebUser target)
        {
            return user.Email == target.Email || user.QQ == target.QQ;
        }
        public static void OutSuccessLog(string msg)
        {
            Console.WriteLine($"[+] [{GetLogTime()}] {msg}");
        }
        public static void OutErrorLog(string msg)
        {
            Console.WriteLine($"[-] [{GetLogTime()}] {msg}");
        }
        public static string GetLogTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        public static string MD5Encrypt(string str)
        {
            str = _SALT + str;
            byte[] result = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(Encoding.UTF8.GetBytes(str));
            StringBuilder output = new(16);
            for (int i = 0; i < result.Length; i++)
            {
                output.Append(result[i].ToString("x2"));
            }
            return output.ToString();
        }
        //https://www.cnblogs.com/xwcs/p/13508438.html
        public static void SendEmail(E_Mail M)
        {
            try
            {
                MailMessage myMail = new MailMessage();//发送电子邮件类

                foreach (string item in M.Address)//添加收件人
                {
                    myMail.To.Add(item);
                }
                foreach (string item in M.CC)//添加抄送
                {
                    myMail.CC.Add(item);
                }

                myMail.Subject = M.Subject;//邮件主题
                myMail.SubjectEncoding = M.SubjectEncoding;//邮件标题编码

                myMail.From = new MailAddress(M.From, M.DisplayName, M.SubjectEncoding);//发件信息


                myMail.Body = M.Body;//邮件内容
                myMail.BodyEncoding = M.BodyEncoding;//邮件内容编码
                myMail.IsBodyHtml = M.IsBodyHtml;//是否是HTML邮件
                myMail.Priority = M.Priority;//邮件优先级

                SmtpClient smtp = new SmtpClient();//SMTP协议

                smtp.EnableSsl = M.EnableSsl;//是否使用SSL安全加密 使用QQ邮箱必选
                smtp.UseDefaultCredentials = M.UseDefaultCredentials;

                smtp.Host = M.Host;//主机
                smtp.Port = M.Port;
                smtp.Credentials = new NetworkCredential(M.From, M.Password);//验证发件人信息

                smtp.Send(myMail);//发送

            }
            catch (Exception e)
            {
                Console.WriteLine($"[-] 发送邮件发生错误：{e.Message}");
            }
        }
        public static E_Mail GetTemplateMail(string subject, string body, string[] address)
        {
            JObject secret = JObject.Parse(System.IO.File.ReadAllText(@"E:\编程\Asp.net\txcloudSecret.json"));
            return new E_Mail() 
            {
                Address = address,
                Body = body,
                DisplayName = "屑平台",
                UseDefaultCredentials = false,
                SubjectEncoding = Encoding.UTF8,
                EnableSsl = true,
                Host = secret["Smtp_Host"].ToString(),
                Port = secret["Smtp_Port"].ToObject<int>(),
                IsBodyHtml = false,
                BodyEncoding = Encoding.UTF8,
                From = secret["Smtp_Account"].ToString(),
                Password = secret["Smtp_Password"].ToString(),
                Priority = MailPriority.Normal,
                CC = Array.Empty<string>(),
                Subject = subject
            };            
        }

        /// <summary>
        /// 使用RNGCryptoServiceProvider生成种子
        /// </summary>
        /// <returns>按此格式 new Random(GetRandomSeed()) 使用随机数种子</returns>
        public static int GetRandomSeed()
        {
            byte[] bytes = new byte[new Random().Next(0, 10000000)];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}
