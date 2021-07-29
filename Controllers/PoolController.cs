﻿using GachaWebBackend.Helper;
using GachaWebBackend.Model;
using Microsoft.AspNetCore.Mvc;
using PublicInfos;
using System;

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
            catch(Exception e)
            {
                Response.StatusCode = 404;
                return WebCommonHelper.SetError(e.Message + "\n" + e.StackTrace);
            }
        }
    }
}
