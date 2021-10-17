using GachaWebBackend.Helper;
using GachaWebBackend.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicInfos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GachaWebBackend.Controllers
{
    [ApiController]
    [Authorize(Policy = "All")]
    [Route("api/v1/[controller]")]
    public class PoolController : ControllerBase
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
        /// 卡池储存
        /// </summary>
        public static List<Pool> PoolsCache;
        /// <summary>
        /// 返回数据
        /// </summary>
        [HttpGet]
        [Route("getAll")]
        public ApiResponse GetAll(bool updateCache = false)
        {
            try
            {
                if(updateCache)
                    PoolsCache = SQLHelper.GetAllPools();
                WebCommonHelper.AddActionSuccessRecord(RequestIP, "", "卡池获取", "");
                return WebCommonHelper.SetOk("查询成功", WebCommonHelper.WriteGzip(PoolsCache.ToJson()));
            }
            catch (Exception e)
            {
                Response.StatusCode = 404;
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "卡池获取", e.Message);
                return WebCommonHelper.SetError(e.Message + "\n" + e.StackTrace);
            }
        }
        /// <summary>
        /// 设置收藏卡池
        /// </summary>
        /// <param name="poolID">卡池ID</param>
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
                    WebCommonHelper.AddActionSuccessRecord(RequestIP, "", "收藏卡池", "");
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
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "收藏卡池", e.Message);
                return WebCommonHelper.SetError(e.Message);
            }
        }
        /// <summary>
        /// 设置取消收藏卡池
        /// </summary>
        /// <param name="poolID"></param>
        /// <returns></returns>
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
                    WebCommonHelper.AddActionSuccessRecord(RequestIP, "", "取消收藏卡池", "");
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
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "取消收藏卡池", e.Message);
                return WebCommonHelper.SetError(e.Message);
            }
        }
        /// <summary>
        /// 获取收藏状态
        /// </summary>
        /// <param name="poolID"></param>
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
                    WebCommonHelper.AddActionSuccessRecord(RequestIP, "", "获取收藏状态", "");
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
                WebCommonHelper.AddActionFailRecord(RequestIP, "", "获取收藏状态", e.Message);
                return WebCommonHelper.SetError(e.Message);
            }
        }
    }
}
