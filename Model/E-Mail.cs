using System.Net.Mail;
using System.Text;

namespace GachaWebBackend.Model
{
    //https://www.cnblogs.com/xwcs/p/13508438.html
    public class E_Mail
    {
        public string From { get; set; }//发件人地址
        public string Password { get; set; }//密码
        public string[] Address { get; set; }//收件人地址
        public string[] CC { get; set; }//抄送
        public string Subject { get; set; }//主题
        public string DisplayName { get; set; }//发件人名称
        public Encoding SubjectEncoding { get; set; }//编码
        public string Body { get; set; }//邮件内容
        public Encoding BodyEncoding { get; set; }//邮件内容编码
        public bool IsBodyHtml { get; set; }//是否HTML邮件
        public MailPriority Priority { get; set; }//邮件优先级
        public bool EnableSsl { get; set; }//是否ssl
        public bool UseDefaultCredentials { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
    }
}
