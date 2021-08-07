using System;
using System.Collections.Generic;
using System.IO;
using PublicInfos;

namespace GachaWebBackend.Helper
{
    public class GachaHelper
    {
        /// <summary>
        /// 生成抽卡图片
        /// </summary>
        /// <param name="poolID">卡池ID</param>
        /// <param name="multiMode">单抽或多抽</param>
        /// <returns>文件路径</returns>
        public static string GenerateGachaPic(int poolID, bool multiMode)
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
                    ls = GachaCore.DoGacha(destPool, multiMode? destPool.MultiGachaNumber : 1, ref baodiCount);
                }

                string filename = Path.Combine(resultPicPath, DateTime.Now.ToString("yyyyMMddHHmmss") + ".png");
                using var img = GachaCore.DrawGachaResult(ls, destPool);
                img.Save(filename);
                WebCommonHelper.OutSuccessLog($"图片生成成功：{destPool.Name}");
                return filename;
            }
            catch (Exception e)
            {
                WebCommonHelper.OutErrorLog($"图片生成失败，原因：{e.Message}\n{e.StackTrace}");
                return string.Empty;
            }
        }
    }
}