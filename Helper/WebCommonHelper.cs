using GachaWebBackend.AuthHelper;
using GachaWebBackend.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace GachaWebBackend.Helper
{
    /// <summary>
    /// 工具类
    /// </summary>
    public static class WebCommonHelper
    {
        /// <summary>
        /// MD5盐值
        /// </summary>
        private const string _SALT = "33ae826c28108ccfd6890ff6d942be89";
        /// <summary>
        /// 返回模板正常对象
        /// </summary>
        /// <param name="msg">正常消息 默认为 ok</param>
        /// <param name="data">附带正常对象 默认为 null</param>

        public static ApiResponse SetOk(string msg = "ok", object data = null)
        {
            return new() { code = 200, msg = msg, data = data };
        }
        /// <summary>
        /// 返回模板错误对象
        /// </summary>
        /// <param name="msg">错误消息 默认为 error</param>
        /// <param name="data">附带错误对象 默认为 null</param>
        public static ApiResponse SetError(string msg = "error", object data = null)
        {
            return new() { code = 404, msg = msg, data = data };
        }
        /// <summary>
        /// 比较关键属性
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool CompareKeyProp(this WebUser user, WebUser target)
        {
            return user.Email == target.Email || user.QQ == target.QQ;
        }
        /// <summary>
        /// 模板正常日志
        /// </summary>
        /// <param name="msg">日志文本</param>
        public static void OutSuccessLog(string msg)
        {
            Console.WriteLine($"[+] [{GetLogTime()}] {msg}");
            //TODO: Database
        }
        /// <summary>
        /// 模板错误日志
        /// </summary>
        /// <param name="msg">日志文本</param>
        public static void OutErrorLog(string msg)
        {
            Console.WriteLine($"[-] [{GetLogTime()}] {msg}");
            //TODO: Database
        }
        /// <summary>
        /// 获取长时间
        /// </summary>
        public static string GetLogTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="str">待加密字符串</param>
        /// <returns>16位小写md5</returns>
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
        /// <summary>
        /// 从头的JWT授权字段中获取用户信息主键
        /// </summary>
        /// <param name="header">Request.Headers</param>
        public static long GetQQFromJwt(Microsoft.AspNetCore.Http.IHeaderDictionary header)
        {
            return JwtHelper.SerializeJwt(header["Authorization"].ToString().Replace("Bearer ", "")).Uid;
        }
        /// <summary>
        /// 使用APIKey查询用户信息
        /// </summary>
        public static WebUser GetRoleFromAPIKey(string APIKey)
        {
            return SqlHelper.GetUserByAPIKey(APIKey);
        }
        /// <summary>
        /// 重载
        /// </summary>
        public static void AddActionSuccessRecord(string IP, long QQ, string actionName, string actionInfo, string APIKey="")
        {
            AddActionSuccessRecord(IP, QQ.ToString(), actionName, actionInfo, APIKey);
        }
        /// <summary>
        /// 添加操作成功日志，时间默认是触发时间
        /// </summary>
        /// <param name="IP">请求来源IP</param>
        /// <param name="QQ">操作者QQ</param>
        /// <param name="actionName">操作名称</param>
        /// <param name="actionInfo">操作备注</param>
        /// <param name="APIKey">使用的APIKey</param>
        public static void AddActionSuccessRecord(string IP, string QQ, string actionName, string actionInfo, string APIKey="")
        {
            SqlHelper.AddRecordAsync(IP, QQ, APIKey, actionName, "成功", actionInfo, DateTime.Now);
        }
        /// <summary>
        /// 重载
        /// </summary>
        public static void AddActionFailRecord(string IP, long QQ, string actionName, string actionInfo, string APIKey = "")
        {
            AddActionFailRecord(IP, QQ.ToString(), actionName, actionInfo, APIKey);
        }
        /// <summary>
        /// 添加操作失败日志，时间默认是触发时间
        /// </summary>
        /// <param name="IP">请求来源IP</param>
        /// <param name="QQ">操作者QQ</param>
        /// <param name="actionName">操作名称</param>
        /// <param name="actionInfo">操作备注</param>
        /// <param name="APIKey">使用的APIKey</param>
        public static void AddActionFailRecord(string IP, string QQ, string actionName, string actionInfo, string APIKey="")
        {
            SqlHelper.AddRecordAsync(IP, QQ,APIKey, actionName, "失败", actionInfo, DateTime.Now);
        }
        //https://www.cnblogs.com/xwcs/p/13508438.html
        /// <summary>
        /// SMTP发送邮件
        /// </summary>
        /// <param name="M">邮件对象</param>
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
        /// <summary>
        /// 初始化邮箱发送模板
        /// </summary>
        /// <param name="subject">标题</param>
        /// <param name="body">内容</param>
        /// <param name="address">发送地址</param>
        /// <returns></returns>
        public static E_Mail GetTemplateMail(string subject, string body, string[] address)
        {
            JObject secret = JObject.Parse(File.ReadAllText(Appsettings.app(new[] { "SecretConfig" })));
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
        /// <summary>
        /// 将字符串压缩成Gzip格式的byte数组
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] WriteGzip(string str)
        {
            byte[] rawData = Encoding.UTF8.GetBytes(str);
            using MemoryStream ms = new();
            GZipStream compressedzipStream = new(ms, CompressionMode.Compress, true);
            compressedzipStream.Write(rawData, 0, rawData.Length);
            compressedzipStream.Close();
            return ms.ToArray();
        }
        /// <summary>
        /// 扩展方法_JsonConvert.SerializeObject的简化封装
        /// </summary>
        /// <param name="obj">待转json的简单对象</param>
        /// <returns>json文本</returns>
        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
        /// <summary>
        /// 图片转base64
        /// </summary>
        /// <param name="img">待处理图片</param>
        /// <returns>base64</returns>
        public static string Image2Base64(Image img)
        {
            using MemoryStream ms = new();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            byte[] b = new byte[ms.Length];
            ms.Read(b, 0, (int)ms.Length);
            return Convert.ToBase64String(b);
        }
    }
}
