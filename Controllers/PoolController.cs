﻿using GachaWebBackend.AuthHelper;
using GachaWebBackend.Helper;
using GachaWebBackend.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicInfos;
using System;
using System.Linq;
using System.Threading;

namespace GachaWebBackend.Controllers
{
    [ApiController]
    [Authorize(Policy = "All")]
    [Route("api/v1/[controller]")]
    public class PoolController : ControllerBase
    {
        static string PoolsCache = "";
        /// <summary>
        /// 返回数据
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("getAll")]
        public ApiResponse GetAll(bool updateCache = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(PoolsCache) || updateCache)
                    CallCache();
                return WebCommonHelper.SetOk("查询成功", WebCommonHelper.WriteGzip(PoolsCache));
            }
            catch (Exception e)
            {
                Response.StatusCode = 404;
                return WebCommonHelper.SetError(e.Message + "\n" + e.StackTrace);
            }
        }
        /// <summary>
        /// 卡池缓存
        /// </summary>
        /// <param name="cacheTimeOut">缓存过期时长</param>
        public void CallCache(int cacheTimeOut = 12 * 60 * 60 * 1000)
        {
            PoolsCache = SQLHelper.GetAllPools().ToJson();
            WebCommonHelper.OutSuccessLog($"卡池缓存成功，在{cacheTimeOut / 1000}秒之后缓存失效");

            Thread thread = new(() =>
            {
                Thread.Sleep(cacheTimeOut);
                PoolsCache = string.Empty;
            });
            thread.Start();
        }
        [HttpGet]
        [Route("setLike")]
        public ApiResponse SetLike(int poolID)
        {
            try
            {
                long QQ = WebCommonHelper.GetQQFromJwt(Request.Headers);
                if (QQ != 0)
                {
                    WebUser user = SqlHelper.GetUserByID(QQ);
                    if (user == null)
                    {
                        throw new Exception("QQ无效");
                    }
                    if (!user.SavedPools.Any(x => x == poolID))
                    {
                        user.SavedPools.Add(poolID);
                        user.UpdateUser();
                    }
                    return WebCommonHelper.SetOk();
                }
                else
                {
                    throw new Exception("请登录");
                }
            }
            catch (Exception e)
            {
                Response.StatusCode = 404;
                return WebCommonHelper.SetError(e.Message);
            }

        }
        [HttpGet]
        [Route("setUnLike")]
        public ApiResponse SetUnLike(int poolID)
        {
            try
            {
                long QQ = WebCommonHelper.GetQQFromJwt(Request.Headers);
                if (QQ != 0)
                {
                    WebUser user = SqlHelper.GetUserByID(QQ);
                    if (user == null)
                    {
                        throw new Exception("QQ无效");
                    }
                    if (user.SavedPools.Any(x => x == poolID))
                    {
                        user.SavedPools.Remove(poolID);
                        user.UpdateUser();
                    }
                    return WebCommonHelper.SetOk();
                }
                else
                {
                    throw new Exception("请登录");
                }
            }
            catch (Exception e)
            {
                Response.StatusCode = 404;
                return WebCommonHelper.SetError(e.Message);
            }
        }
        [HttpGet]
        [Route("checkLike")]
        public ApiResponse CheckLikeStatus(int poolID)
        {
            try
            {
                long QQ = WebCommonHelper.GetQQFromJwt(Request.Headers);
                if (QQ != 0)
                {
                    WebUser user = SqlHelper.GetUserByID(QQ);
                    if (user == null)
                    {
                        throw new Exception("QQ无效");
                    }
                    return WebCommonHelper.SetOk("ok", new { status = user.SavedPools.Any(x => x == poolID) });
                }
                else
                {
                    throw new Exception("请登录");
                }
            }
            catch (Exception e)
            {
                Response.StatusCode = 404;
                return WebCommonHelper.SetError(e.Message);
            }
        }
    }
}
