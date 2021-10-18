using GachaWebBackend.AuthHelper;
using GachaWebBackend.Helper;
using GachaWebBackend.Model;
using Microsoft.AspNetCore.Mvc;
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
        string _requestIP = "";
        string RequestIP
        {
            get
            {
                _requestIP = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                return _requestIP;
            }
            set
            {
                _requestIP = value;
            }
        }
        /// <summary>
        /// 邮箱验证码校验
        /// </summary>
        static Dictionary<string, CaptchaSave> emailCaptcha = new();
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
                WebCommonHelper.AddActionSuccessRecord(RequestIP, userRole.QQ, "登录", "");
                return WebCommonHelper.SetOk("ok", jwtStr);
            }
            else
            {
                WebCommonHelper.OutErrorLog($"登录失败 用户名或密码错误: 用户名: {name} 密码: {pass}");
                WebCommonHelper.AddActionFailRecord(RequestIP, userRole.QQ, "登录", "用户名或密码错误");
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
            try
            {
                if (emailCaptcha.Any(x=>x.Value.Email==user.Email && x.Value.Pass) is false || emailCaptcha[user.Email].Code==0)
                    throw new Exception("请先使用邮箱验证码或验证码已过期");
                var flag = SqlHelper.Register(user);
                if (flag)
                {
                    WebCommonHelper.OutSuccessLog($"用户注册成功: QQ: {user.QQ} Email: {user.Email}");
                    WebCommonHelper.AddActionSuccessRecord(RequestIP, user.QQ, "注册", "");
                    return WebCommonHelper.SetOk();
                }
                else
                    throw new Exception("QQ或邮箱重复注册");
            }
            catch (Exception ex)
            {
                WebCommonHelper.OutErrorLog($"注册失败: QQ: {user.QQ} Email: {user.Email}");
                WebCommonHelper.AddActionFailRecord(RequestIP, user.QQ, "注册", ex.Message);
                return WebCommonHelper.SetError(ex.Message);
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
                WebCommonHelper.AddActionSuccessRecord(RequestIP, "", "查询邮箱", "未被使用");
                return WebCommonHelper.SetOk(); 
            }
            else
            {
                WebCommonHelper.OutErrorLog($"注册邮箱重复使用: Email: {email}");
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "查询邮箱", "已被使用");
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
                WebCommonHelper.AddActionSuccessRecord(RequestIP, "", "查询QQ", "未被使用");
                return WebCommonHelper.SetOk();
            }
            else
            {
                WebCommonHelper.OutErrorLog($"注册QQ重复使用: QQ: {QQ}");
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "查询QQ", "已被使用");
                return WebCommonHelper.SetOk("此QQ已被使用"); 
            }
        }
        /// <summary>
        /// 校验腾讯图形验证码
        /// </summary>
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
                WebCommonHelper.AddActionSuccessRecord(RequestIP, "", "邮箱验证码校验", "");
                return WebCommonHelper.SetOk("ok", resp);
            }
            catch (Exception e)
            {
                WebCommonHelper.OutErrorLog($"腾讯云图形验证码校验失败: {e.Message}");
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "邮箱验证码校验", e.Message);
                return WebCommonHelper.SetError();
            }
        }
        /// <summary>
        /// 获取邮箱验证码
        /// </summary>
        /// <param name="address">邮箱地址</param>
        [HttpGet]
        [Route("getemailcaptcha")]
        public ApiResponse EmailCaptcha(string address)
        {
            //列表存在这个邮箱而且不在限时中
            var flag = emailCaptcha.Any(x => x.Value.Email == address && !x.Value.CanReGet) || SqlHelper.VerifyEmail(address);//是否邮箱已使用过
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
                    Thread.Sleep(1 * 60 * 1000);
                    emailCaptcha[sessionID].CanReGet = true;
                    Thread.Sleep(4 * 60 * 1000);
                    WebCommonHelper.OutSuccessLog($"验证码销毁成功，用户: {address} SessionID: {sessionID} 验证码: {code}");
                    WebCommonHelper.AddActionSuccessRecord("self", "", "邮箱验证码", $"已销毁, 用户: {address} SessionID: {sessionID} 验证码: {code}");
                    emailCaptcha.Remove(sessionID);
                });
                thread.Start();
                WebCommonHelper.OutSuccessLog($"验证码获取成功，用户: {address} SessionID: {sessionID} 验证码: {code}");
                WebCommonHelper.AddActionSuccessRecord(RequestIP, "", "邮箱验证码", $"验证码获取成功，用户: {address} SessionID: {sessionID} 验证码: {code}");
                return WebCommonHelper.SetOk("ok", sessionID);
            }
            if(emailCaptcha.Any(x => x.Value.Email == address && !x.Value.CanReGet))
            {
                WebCommonHelper.OutErrorLog($"邮箱验证码，邮箱验证码仍在等待队列中: {address}");
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "邮箱验证码", $"邮箱验证码仍在等待队列中，用户: {address}");
                return WebCommonHelper.SetError("邮箱验证码仍在等待队列中");
            }
            else
            {
                WebCommonHelper.OutErrorLog($"邮箱验证码，邮箱已注册: {address}");
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "邮箱验证码", $"邮箱未注册，用户: {address}");
                return WebCommonHelper.SetError("邮箱已注册");
            }
        }
        /// <summary>
        /// 校验邮件验证码
        /// </summary>
        /// <param name="code">邮箱验证码</param>
        /// <param name="sessionID">sessionID</param>
        [HttpGet]
        [Route("verifyemailcaptcha")]
        public ApiResponse VerifyEmailCaptcha(int code, string sessionID)
        {
            string msg;
            if (string.IsNullOrWhiteSpace(sessionID) || code < 100000)
            {
                msg = $"参数无效，sessionID: {sessionID} code: {code}";
                WebCommonHelper.OutErrorLog(msg);
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "验证邮箱验证码", msg);
                return WebCommonHelper.SetError("参数无效"); 
            }
            if(emailCaptcha.ContainsKey(sessionID) && emailCaptcha[sessionID].Code == code)
            {
                msg = $"验证码验证成功，用户: {emailCaptcha[sessionID].Email}";
                WebCommonHelper.OutSuccessLog(msg);
                WebCommonHelper.AddActionSuccessRecord(RequestIP, "", "验证邮箱验证码", msg);
                emailCaptcha[sessionID].Pass = true;
                return WebCommonHelper.SetOk();
            }
            else
            {
                WebCommonHelper.OutErrorLog($"验证码验证失败");
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "验证邮箱验证码", "无效的验证码");
                return WebCommonHelper.SetError("无效的验证码，若确认无误尝试重新获取");
            }
        }
        /// <summary>
        /// 重置密码
        /// </summary>
        /// <param name="sessionID"></param>
        /// <param name="newpwd"></param>
        [HttpGet]
        [Route("resetpwd")]
        public ApiResponse ResetPassword(string sessionID, string newpwd)
        {
            string msg;
            if (string.IsNullOrWhiteSpace(sessionID) || string.IsNullOrWhiteSpace(newpwd))
            {
                msg = $"重置密码参数无效, sessionID: {sessionID} newpwd: {newpwd}";
                WebCommonHelper.OutErrorLog(msg);
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "重置密码", msg);
                return WebCommonHelper.SetError($"参数无效"); 
            }
            if(emailCaptcha.ContainsKey(sessionID) is false)
            {
                msg = $"Session无效, sessionID: {sessionID} newpwd: {newpwd}";
                WebCommonHelper.OutErrorLog(msg);
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "重置密码", msg);
                return WebCommonHelper.SetError("Session无效，请刷新页面重新获取验证码");
            }
            string email = emailCaptcha[sessionID].Email;
            try
            {
                SqlHelper.ResetPassword(email, WebCommonHelper.MD5Encrypt(newpwd));
                WebCommonHelper.OutSuccessLog($"重置密码成功: 用户: {email} 新密码: {newpwd}");
                WebCommonHelper.AddActionSuccessRecord(RequestIP, "", "重置密码", "");
                return WebCommonHelper.SetOk();
            }
            catch (Exception e)
            {
                WebCommonHelper.OutErrorLog($"重置密码失败: {e.Message}");
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "重置密码", e.Message);
                return WebCommonHelper.SetError("重置失败，请重试");
            }
        }
        /// <summary>
        /// 使用Token获取用户信息
        /// </summary>
        /// <param name="token"></param>
        [HttpGet]
        [Route("getuserinfo")]
        public ApiResponse GetUserInfo(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                WebCommonHelper.OutErrorLog($"获取用户信息 无效Token: token: {token}");
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "获取用户信息", "无效Token");
                return WebCommonHelper.SetError("无效参数");
            }
            var c = JwtHelper.SerializeJwt(token);
            WebUser user = SqlHelper.GetUserByID(c.Uid);
            user.Password = "***";
            WebCommonHelper.OutSuccessLog($"用户已获取信息: QQ: {user.QQ}");
            WebCommonHelper.AddActionSuccessRecord(RequestIP, "", "获取用户信息", $"QQ: {user.QQ}");
            return WebCommonHelper.SetOk("ok", user);
        }
        /// <summary>
        /// 登出
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("logout")]
        public ApiResponse Logout(JObject json) 
        {
            return WebCommonHelper.SetOk();
        }
        [HttpGet]
        [Route("GetActionRecords")]
        public ApiResponse GetActionRecords() 
        {
            var user = WebCommonHelper.GetQQFromJwt(Request.Headers);
            try
            {
                var actions = SqlHelper.GetRecordsByQQ(user.ToString());
                actions = actions.Where(x => x.Time > x.Time.AddMonths(-1) && string.IsNullOrEmpty(x.APIKey) is false).OrderByDescending(x=>x.Time).ToList();
                List<object> list = new List<object>();
                foreach (var action in actions)
                    list.Add(new { RequestIP=action.IP, action.Action, action.Time, action.Info });
                WebCommonHelper.OutSuccessLog($"获取操作日志, QQ: {user}");
                WebCommonHelper.AddActionSuccessRecord(RequestIP, user, "获取操作日志", "");
                return WebCommonHelper.SetOk("ok", list);
            }
            catch(Exception ex)
            {
                WebCommonHelper.OutErrorLog($"获取操作日志失败, QQ: {user}");
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "获取操作日志", ex.Message);
                return WebCommonHelper.SetError("error", ex.Message);
            }
        }
    }
}
