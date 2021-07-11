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
                JwtHelper.TokenModelJwt tokenModel = new() { Uid = 1, Role = userRole.Developer == 1 ? "Developer" : "User" };
                jwtStr = JwtHelper.IssueJwt(tokenModel);//登录，获取到一定规则的 Token 令牌
                return WebCommonHelper.SetOk("ok", jwtStr);
            }
            else
            {
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
                Console.WriteLine($"[+] 用户注册成功：QQ: {user.QQ} Email: {user.Email}");
                return WebCommonHelper.SetOk(); 
            }
            else
            {
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
                return WebCommonHelper.SetOk();
            else
                return WebCommonHelper.SetOk("此邮箱已被使用");
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
                return WebCommonHelper.SetOk();
            else
                return WebCommonHelper.SetOk("此QQ已被使用");
        }
        [HttpGet]
        [Route("verifycaptcha")]
        public ApiResponse VerifyCaptcha(string randstr, string ticket, string ip)
        {
            try
            {
                JObject secret = JObject.Parse(System.IO.File.ReadAllText(@"E:\编程\Asp.net\txcloudSecret.txt"));
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
                req.CaptchaAppId = 2038093986;
                req.AppSecretKey = "0nKys00xpbAVpQZJ8GbWX3Q**";
                DescribeCaptchaResultResponse resp = client.DescribeCaptchaResultSync(req);
                return WebCommonHelper.SetOk("ok", resp);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return WebCommonHelper.SetError();
            }
        }
    }
}
