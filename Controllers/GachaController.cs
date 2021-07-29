using GachaWebBackend.Helper;
using GachaWebBackend.Model;
using Microsoft.AspNetCore.Mvc;
using PublicInfos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace GachaWebBackend.Controllers
{
    public class ApiCallCount
    {
        public const int AddSecondPerCall = 10 * 60;
        public const int CallCountMax = 10;
        public int CallCount { get; set; }
        public int AddSecond { get; set; }
        public string IP { get; set; }
    }
    [ApiController]
    [Route("api/v1/[controller]")]
    public class GachaController : ControllerBase
    {
        private static Dictionary<string, ApiCallCount> CallNumber { get; set; } = new();
        [HttpGet]
        [Route("gacha")]
        public ApiResponse DoGacha(int poolID, bool multiMode)
        {
            try
            {
                string ip = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                int count = QueryCallNumber(ip);
                ApiCallCount callStatus = CallNumber[ip];
                if(count == -1)
                {
                    return WebCommonHelper.SetError($"超出限额", callStatus);
                }
                int baodiCount = 0;
                Pool destPool = SQLHelper.GetPoolByID(poolID);
                //预构建图片保存目录
                string resultPicPath = Path.Combine("Result\\img", destPool.Name);
                Directory.CreateDirectory(resultPicPath);
                List<GachaItem> ls = new();
                lock (ConfigCache.ConfigLock)
                {
                    MainSave.ApplicationConfig = ConfigCache.UserConfigs[0];
                    ls = GachaCore.DoGacha(destPool, destPool.MultiGachaNumber, ref baodiCount);
                }

                string filename = DateTime.Now.ToString("yyyyMMddHHmmss") + ".png";
                using var img = GachaCore.DrawGachaResult(ls, destPool);
                img.Save(Path.Combine(resultPicPath, filename));

                using MemoryStream ms = new();
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                byte[] b = new byte[ms.Length];
                ms.Read(b, 0, (int)ms.Length);
                WebCommonHelper.OutSuccessLog($"图片生成成功：{destPool.Name}");
                return WebCommonHelper.SetOk("ok", new { callStatus.CallCount, callStatus.AddSecond, Img = Convert.ToBase64String(b) });
            }
            catch (Exception e)
            {
                Response.StatusCode = 404;
                return WebCommonHelper.SetError(e.Message + "\n" + e.StackTrace);
            }
        }
        public int QueryCallNumber(string ip)
        {
            if(CallNumber.ContainsKey(ip) is false)
            {
                ApiCallCount newCount = new() { IP = ip, AddSecond = ApiCallCount.AddSecondPerCall, CallCount = 10 };
                CallNumber.Add(ip, newCount);
                Thread thread = new(() =>
                {
                    string thread_ip = ip;
                    while (true)
                    {
                        Thread.Sleep(1 * 1000);
                        lock(CallNumber)
                        {
                            if(CallNumber[thread_ip].AddSecond == 0)
                            {
                                CallNumber[thread_ip].AddSecond = ApiCallCount.AddSecondPerCall;
                                if(CallNumber[thread_ip].CallCount != ApiCallCount.CallCountMax)
                                    CallNumber[thread_ip].CallCount++;
                            }
                            CallNumber[thread_ip].AddSecond--;
                        }
                    }
                });thread.Start();
            }
            if (CallNumber[ip].CallCount == 0)
                return -1;
            return --CallNumber[ip].CallCount;
        }
    }
}
