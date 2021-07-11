using GachaWebBackend.Helper;
using GachaWebBackend.Model;
using Microsoft.AspNetCore.Mvc;
using PublicInfos;
using System;
using System.Collections.Generic;
using System.IO;

namespace GachaWebBackend.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class GachaController : ControllerBase
    {
        [HttpGet]
        [Route("gacha")]
        public ApiResponse DoGacha(int poolID, bool multiMode)
        {
            try
            {
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
                var img = GachaCore.DrawGachaResult(ls, destPool);
                img.Save(Path.Combine(resultPicPath, filename));

                using MemoryStream ms = new();
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                byte[] b = new byte[ms.Length];
                ms.Read(b, 0, (int)ms.Length);
                return WebCommonHelper.SetOk("ok", Convert.ToBase64String(b));
            }
            catch (Exception e)
            {
                Response.StatusCode = 404;
                return WebCommonHelper.SetError(e.Message + "\n" + e.StackTrace);
            }
        }
    }
}
