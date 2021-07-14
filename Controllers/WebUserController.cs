using GachaWebBackend.AuthHelper;
using GachaWebBackend.Helper;
using GachaWebBackend.Model;
using CustomGacha.SDK.Tool.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using TencentCloud.Captcha.V20190722;
using TencentCloud.Captcha.V20190722.Models;
using TencentCloud.Common;
using TencentCloud.Common.Profile;
using System.Linq;
using System.Threading;

namespace GachaWebBackend.Controllers
{
    /// <summary>
    /// 用户管理模块
    /// </summary>
    [ApiController]
    [Route("api/v1/user")]
    public class WebUserController : ControllerBase
    {
        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="json">Json文本，格式: {"name":"xxx", "password":"xxx"}</param>
        /// <returns>data中是Jwt授权Token</returns>
        [HttpPost]
        [Route("login")]
        public ApiResponse Login(JObject json)
        {
            string name = json["username"].ToString();
            string pass = WebCommonHelper.MD5Encrypt(json["password"].ToString());
            string jwtStr = "";
            var userRole = SqlHelper.Login(name, pass);
            if (userRole != null)
            {
                // 将用户id和角色名，作为单独的自定义变量封装进 token 字符串中。
                JwtHelper.TokenModelJwt tokenModel = new() { Uid = userRole.QQ, Role = userRole.Developer == 1 ? "Developer" : "User" };
                jwtStr = JwtHelper.IssueJwt(tokenModel);//登录，获取到一定规则的 Token 令牌
                WebCommonHelper.OutSuccessLog($"登录成功: 用户名: {name} 密码: {pass} Token: {jwtStr}");
                return WebCommonHelper.SetOk("ok", jwtStr);
            }
            else
            {
                WebCommonHelper.OutErrorLog($"登录失败 用户名或密码错误: 用户名: {name} 密码: {pass}");
                return WebCommonHelper.SetError("用户名或密码错误");
            }
        }
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="user">Json文本, 请按照WebUser类的格式填写</param>
        [HttpPost]
        [Route("register")]
        public ApiResponse Register(WebUser user)
        {
            var flag = SqlHelper.Register(user);
            if (flag) 
            {
                WebCommonHelper.OutSuccessLog($"用户注册成功: QQ: {user.QQ} Email: {user.Email}");
                return WebCommonHelper.SetOk(); 
            }
            else
            {
                WebCommonHelper.OutErrorLog($"注册失败: QQ: {user.QQ} Email: {user.Email}");
                return WebCommonHelper.SetError("QQ或邮箱重复注册");
            }
        }
        /// <summary>
        /// 验证邮箱状态
        /// </summary>
        /// <param name="email">校验邮箱是否已经被使用</param>
        [HttpGet]
        [Route("verifyemail")]
        public ApiResponse VerifyEmail(string email)
        {
            var flag = SqlHelper.VerifyEmail(email);
            if (flag)
            {
                WebCommonHelper.OutSuccessLog($"注册邮箱未被使用: Email: {email}");
                return WebCommonHelper.SetOk(); 
            }
            else
            {
                WebCommonHelper.OutErrorLog($"注册邮箱重复使用: Email: {email}");
                return WebCommonHelper.SetOk("此邮箱已被使用");
            }
        }
        /// <summary>
        /// 验证QQ状态
        /// </summary>
        /// <param name="QQ">校验QQ是否已被注册</param>
        [HttpGet]
        [Route("verifyqq")]
        public ApiResponse VerifyQQ(long QQ)
        {
            var flag = SqlHelper.VerifyQQ(QQ);
            if (flag)
            {
                WebCommonHelper.OutSuccessLog($"注册QQ未被使用: QQ: {QQ}");
                return WebCommonHelper.SetOk();
            }
            else
            {
                WebCommonHelper.OutErrorLog($"注册QQ重复使用: QQ: {QQ}");
                return WebCommonHelper.SetOk("此QQ已被使用"); 
            }
        }
        [HttpGet]
        [Route("verifycaptcha")]
        public ApiResponse VerifyCaptcha(string randstr, string ticket, string ip)
        {
            try
            {
                JObject secret = JObject.Parse(System.IO.File.ReadAllText(Appsettings.app(new string[] { "SecretConfig" })));
                Credential cred = new()
                {
                    SecretId = secret["SecretId"].ToString(),
                    SecretKey = secret["SecretKey"].ToString()
                };
                ClientProfile clientProfile = new();
                HttpProfile httpProfile = new();
                httpProfile.Endpoint = ("captcha.tencentcloudapi.com");
                clientProfile.HttpProfile = httpProfile;

                CaptchaClient client = new(cred, "", clientProfile);
                DescribeCaptchaResultRequest req = new();
                req.CaptchaType = 9;
                req.Ticket = ticket;
                req.UserIp = ip;
                req.Randstr = randstr;
                req.CaptchaAppId = secret["CaptchaAppId"].ToObject<ulong>();
                req.AppSecretKey = secret["CaptchaAppSecretKey"].ToString();
                DescribeCaptchaResultResponse resp = client.DescribeCaptchaResultSync(req);
                WebCommonHelper.OutSuccessLog($"腾讯云图形验证码校验成功, randstr: {randstr} ticket: {ticket} ip: {ip}");
                return WebCommonHelper.SetOk("ok", resp);
            }
            catch (Exception e)
            {
                WebCommonHelper.OutErrorLog($"腾讯云图形验证码校验失败: {e.Message}");
                return WebCommonHelper.SetError();
            }
        }

        static Dictionary<string, CaptchaSave> emailCaptcha = new();
        [HttpGet]
        [Route("getemailcaptcha")]
        public ApiResponse EmailCaptcha(string address)
        {
            var flag = SqlHelper.VerifyEmail(address);
            if (!flag)
            {
                int code;
                do
                {
                    code = new Random(WebCommonHelper.GetRandomSeed()).Next(100000, 999999);
                } while (emailCaptcha.Any(x => x.Value.Code == code));
                string sessionID = Guid.NewGuid().ToString();
                emailCaptcha.Add(sessionID, new CaptchaSave { Code = code, Email = address });
                WebCommonHelper.SendEmail(WebCommonHelper.GetTemplateMail("屑平台邮箱验证", $"验证码:{code}，有效期5分钟", new string[] { address }));
                Thread thread = new(()=> 
                {
                    Thread.Sleep(5 * 60 * 1000);
                    WebCommonHelper.OutSuccessLog($"验证码销毁成功，用户: {address} SessionID: {sessionID} 验证码: {code}");
                    emailCaptcha.Remove(sessionID);
                });
                thread.Start();
                WebCommonHelper.OutSuccessLog($"验证码获取成功，用户: {address} SessionID: {sessionID} 验证码: {code}");
                return WebCommonHelper.SetOk("ok", sessionID);
            }
            WebCommonHelper.OutErrorLog($"邮箱未注册: {address}");
            return WebCommonHelper.SetError("无效的邮箱");
        }
        [HttpGet]
        [Route("verifyemailcaptcha")]
        public ApiResponse VerifyEmailCaptcha(int code, string sessionID)
        {
            if (string.IsNullOrWhiteSpace(sessionID) || code < 100000)
            {
                WebCommonHelper.OutErrorLog($"参数无效，sessionID: {sessionID} code: {code}");
                return WebCommonHelper.SetError("参数无效"); 
            }
            if(emailCaptcha.ContainsKey(sessionID) && emailCaptcha[sessionID].Code == code)
            {
                WebCommonHelper.OutSuccessLog($"验证码验证成功，用户: {emailCaptcha[sessionID].Email}");
                return WebCommonHelper.SetOk();
            }
            else
            {
                WebCommonHelper.OutErrorLog($"验证码验证失败");
                return WebCommonHelper.SetError("无效的验证码，若确认无误尝试重新获取");
            }
        }
        [HttpGet]
        [Route("resetpwd")]
        public ApiResponse ResetPassword(string sessionID, string newpwd)
        {
            if (string.IsNullOrWhiteSpace(sessionID) || string.IsNullOrWhiteSpace(newpwd))
            {
                WebCommonHelper.OutErrorLog($"重置密码参数无效, sessionID: {sessionID} newpwd: {newpwd}");
                return WebCommonHelper.SetError($"参数无效"); 
            }
            if(emailCaptcha.ContainsKey(sessionID) is false)
            {
                WebCommonHelper.OutErrorLog($"Session无效, sessionID: {sessionID} newpwd: {newpwd}");
                return WebCommonHelper.SetError("Session无效，请刷新页面重新获取验证码");
            }
            string email = emailCaptcha[sessionID].Email;
            try
            {
                SqlHelper.ResetPassword(email, WebCommonHelper.MD5Encrypt(newpwd));
                WebCommonHelper.OutSuccessLog($"重置密码成功: 用户: {email} 新密码: {newpwd}");
                return WebCommonHelper.SetOk();
            }
            catch (Exception e)
            {
                WebCommonHelper.OutErrorLog($"重置密码失败: {e.Message}");
                return WebCommonHelper.SetError("重置失败，请重试");
            }
        }
        [HttpGet]
        [Route("getuserinfo")]
        public ApiResponse GetUserInfo(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                WebCommonHelper.OutErrorLog($"获取用户信息 无效Token: token: {token}");
                return WebCommonHelper.SetError("无效参数");
            }
            var c = JwtHelper.SerializeJwt(token);
            WebUser user = SqlHelper.GetUserByID(c.Uid);
            user.Password = "***";
            WebCommonHelper.OutSuccessLog($"用户已获取信息: QQ: {user.QQ}");
            return WebCommonHelper.SetOk("ok", user);
        }
        [HttpPost]
        [Route("logout")]
        public ApiResponse Logout(JObject json) 
        {
            return WebCommonHelper.SetOk();
        }
    }
}
