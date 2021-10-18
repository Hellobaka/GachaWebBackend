using GachaWebBackend.Helper;
using GachaWebBackend.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
    [Route("api/v1/[controller]")]
    public class GachaController : ControllerBase
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
        [Authorize(Policy = "All")]
        [Route("testGacha")]
        public ApiResponse TestGacha(int poolID, bool multiMode)
        {
            try
            {
                string ip = RequestIP;
                if (string.IsNullOrWhiteSpace(ip))
                    throw new Exception($"捕获IP失败，请确认未使用代理服务器绕过限额检测");
                int count = HandleCallNumber(ip);
                ApiCallCount callStatus = CallNumber[ip];
                if (count == -1)
                {
                    WebCommonHelper.AddActionFailRecord(RequestIP, "", "测试图片生成", "超出限额");
                    return WebCommonHelper.SetError($"超出限额", callStatus);
                }

                string picPath = GachaHelper.GenerateGachaPic(poolID, multiMode);
                if (string.IsNullOrWhiteSpace(picPath))
                    throw new Exception("图片生成失败，详情参照控制台");

                using var img = Image.FromFile(picPath);
                WebCommonHelper.AddActionSuccessRecord(ip, "", "测试图片生成", "");
                return WebCommonHelper.SetOk("ok",
                    new { callStatus.CallCount, callStatus.AddSecond, Img = WebCommonHelper.Image2Base64(img) });
            }
            catch(Exception ex)
            {
                WebCommonHelper.OutSuccessLog($"测试图片生成失败，{ex.Message}");
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "测试图片生成", ex.Message);
                return WebCommonHelper.SetError(ex.Message);
            }            
        }
        /// <summary>
        /// 使用APIKey限定进行抽卡
        /// </summary>
        /// <param name="APIKey">平台分发的APIKey</param>
        /// <param name="poolID">需要抽取卡池的ID</param>
        /// <param name="multiMode">单抽或多抽</param>
        [HttpGet]
        [Route("doGacha")]
        public ApiResponse GetGachaResult(string APIKey, int poolID, bool multiMode)
        {
            var user = WebCommonHelper.GetRoleFromAPIKey(APIKey);
            try
            {
                if (user == null)
                    throw new Exception("无关联APIKey");
                if (PoolController.PoolsCache.Any(x => x.PoolID == poolID) is false || user.SavedPools.Any(x => x == poolID) is false)
                    throw new Exception($"poolID={poolID} multiMode={multiMode} msg=未找到相关卡池，可能需要先在卡池预览中收藏该卡池");

                string picPath = GachaHelper.GenerateGachaPic(poolID, multiMode);
                if (string.IsNullOrWhiteSpace(picPath))
                    throw new Exception($"poolID={poolID} multiMode={multiMode} msg=图片生成失败，详情参照控制台");

                using var img = Image.FromFile(picPath);
                WebCommonHelper.AddActionSuccessRecord(RequestIP, user.QQ.ToString(), "抽卡图片生成", $"poolID={poolID} multiMode={multiMode}", APIKey);
                return WebCommonHelper.SetOk("ok",
                    new { Img = WebCommonHelper.Image2Base64(img) });
            }
            catch (Exception ex)
            {
                string o = user == null ? APIKey : user.QQ.ToString();
                WebCommonHelper.OutErrorLog($"抽卡图片生成失败，Error：{ex.Message}");
                WebCommonHelper.AddActionFailRecord(RequestIP, o, "抽卡图片生成", ex.Message, APIKey);
                return WebCommonHelper.SetError(ex.Message);
            }            
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