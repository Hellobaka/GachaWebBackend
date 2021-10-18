using GachaWebBackend.Helper;
using GachaWebBackend.Model;
using Microsoft.AspNetCore.Mvc;
using System;

namespace GachaWebBackend.Controllers
{
    /// <summary>
    /// 管理APIKey的控制器
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class APIKeyController : ControllerBase
    {
        string _requestIP = "";
        string RequestIP { 
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
        /// 生成
        /// </summary>
        [HttpGet]
        [Route("GenerateKey")]
        public ApiResponse GenerateKey(bool reGen = false)
        {
            var user = SqlHelper.GetUserByID(WebCommonHelper.GetQQFromJwt(Request.Headers));
            try
            {
                if (user != null)
                {
                    if(!reGen && !string.IsNullOrWhiteSpace(user.APIKey))
                        throw new Exception("已存在APIKey, 如需覆盖请指定reGen字段");
                    user.APIKey = Guid.NewGuid().ToString().Replace("-", "");
                    WebCommonHelper.OutSuccessLog($"APIKey生成, QQ: {user.QQ} APIKey: {user.APIKey}");
                    SqlHelper.UpdateUser(user);
                    WebCommonHelper.AddActionSuccessRecord(RequestIP, user.QQ, "APIKey生成", $"reGen={reGen} APIKey={user.APIKey}");
                    return WebCommonHelper.SetOk("ok", new { APIKey = user.APIKey });
                }
            }
            catch (Exception ex)
            {
                WebCommonHelper.OutErrorLog($"APIKey生成, QQ: {user.QQ}, Error: {ex.Message}");
                WebCommonHelper.AddActionFailRecord(RequestIP, user.QQ, "APIKey生成", $"reGen={reGen} Error={ex.Message}");
                Response.StatusCode = 400;
                return WebCommonHelper.SetError(ex.Message);
            }
            return WebCommonHelper.SetError("未知错误");
        }
    }
}