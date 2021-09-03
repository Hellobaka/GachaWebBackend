using GachaWebBackend.Helper;
using GachaWebBackend.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace GachaWebBackend.Controllers
{
    /// <summary>
    /// 试用生成限额 单ip默认10次 每600秒加一次
    /// </summary>
    public class ApiCallCount
    {
        /// <summary>
        /// 增加额度的时长
        /// </summary>
        public const int AddSecondPerCall = 10 * 60;
        /// <summary>
        /// 调用限额
        /// </summary>
        public const int CallCountMax = 10;
        /// <summary>
        /// 剩余调用额度
        /// </summary>
        public int CallCount { get; set; }
        /// <summary>
        /// 增加额度的进度
        /// </summary>
        public int AddSecond { get; set; }
        /// <summary>
        /// 调用者ip
        /// </summary>
        public string IP { get; set; }
    }

    /// <summary>
    /// 抽卡控制器 用于控制各种抽卡api
    /// </summary>
    [ApiController]
    [Authorize(Policy = "All")]
    [Route("api/v1/[controller]")]
    public class GachaController : ControllerBase
    {
        /// <summary>
        /// 对试用抽卡额度的记录
        /// </summary>
        private static Dictionary<string, ApiCallCount> CallNumber { get; set; } = new();
        /// <summary>
        /// 试用抽卡
        /// </summary>
        /// <param name="poolID">调用的卡池ID</param>
        /// <param name="multiMode">单抽或多抽</param>
        /// <returns></returns>
        [HttpGet]
        [Route("testGacha")]
        public ApiResponse TestGacha(int poolID, bool multiMode)
        {
            string ip = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrWhiteSpace(ip))
            {
                return WebCommonHelper.SetError($"捕获IP失败，请确认未使用代理服务器绕过限额检测");
            }
            int count = HandleCallNumber(ip);
            ApiCallCount callStatus = CallNumber[ip];
            if (count == -1)
            {
                return WebCommonHelper.SetError($"超出限额", callStatus);
            }

            string picPath = GachaHelper.GenerateGachaPic(poolID, multiMode);
            if (string.IsNullOrWhiteSpace(picPath))
            {
                return WebCommonHelper.SetError("图片生成失败，详情参照控制台");
            }

            using var img = Image.FromFile(picPath);
            return WebCommonHelper.SetOk("ok",
                new {callStatus.CallCount, callStatus.AddSecond, Img = WebCommonHelper.Image2Base64(img)});
        }
        
        /// <summary>
        /// 处理试用限额
        /// </summary>
        /// <param name="ip">调用者ip</param>
        /// <returns>此ip当前调用限额</returns>
        public int HandleCallNumber(string ip)
        {
            if (CallNumber.ContainsKey(ip) is false)
            {
                ApiCallCount newCount = new() {IP = ip, AddSecond = ApiCallCount.AddSecondPerCall, CallCount = 10};
                CallNumber.Add(ip, newCount);
                Thread thread = new(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(1 * 1000);
                        lock (CallNumber)
                        {
                            if (CallNumber[ip].AddSecond == 0)
                            {
                                CallNumber[ip].AddSecond = ApiCallCount.AddSecondPerCall;
                                if (CallNumber[ip].CallCount != ApiCallCount.CallCountMax)
                                    CallNumber[ip].CallCount++;
                            }

                            CallNumber[ip].AddSecond--;
                        }
                    }
                });
                thread.Start();
            }

            if (CallNumber[ip].CallCount == 0)
                return -1;
            return --CallNumber[ip].CallCount;
        }
    }
}