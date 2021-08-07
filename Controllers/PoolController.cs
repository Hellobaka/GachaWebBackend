using GachaWebBackend.AuthHelper;
using GachaWebBackend.Helper;
using GachaWebBackend.Model;
using Microsoft.AspNetCore.Mvc;
using PublicInfos;
using System;
using System.Linq;

namespace GachaWebBackend.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PoolController : ControllerBase
    {
        /// <summary>
        /// 返回数据
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("getAll")]
        public ApiResponse GetAll()
        {
            try
            {
                return WebCommonHelper.SetOk("查询成功", WebCommonHelper.WriteGzip(SQLHelper.GetAllPools().ToJson()));
            }
            catch (Exception e)
            {
                Response.StatusCode = 404;
                return WebCommonHelper.SetError(e.Message + "\n" + e.StackTrace);
            }
        }
        [HttpGet]
        [Route("setLike")]
        public ApiResponse SetLike(int poolID)
        {
            try
            {
                long QQ = JwtHelper.SerializeJwt(Request.Headers["X-Token"].ToString()).Uid;
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
                long QQ = JwtHelper.SerializeJwt(Request.Headers["X-Token"].ToString()).Uid;
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
                long QQ = JwtHelper.SerializeJwt(Request.Headers["X-Token"].ToString()).Uid;
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
