using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GachaWebBackend.AuthHelper
{
    public class JwtTokenAuth
    {
        // 中间件一定要有一个next，将管道可以正常的走下去
        private readonly RequestDelegate _next;
        public JwtTokenAuth(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
            //检测是否包含'Authorization'请求头
            if (!httpContext.Request.Headers.ContainsKey("x-token"))
            {
                httpContext.Abort();
                return _next(httpContext);//不包含授权信息头的http信道
            }
            var tokenHeader = httpContext.Request.Headers["x-token"].ToString().Replace("Bearer ", "");

            try
            {
                if (tokenHeader.Length >= 128)
                {
                    JwtHelper.TokenModelJwt tm = JwtHelper.SerializeJwt(tokenHeader);

                    //授权 Claim 关键
                    var claimList = new List<Claim>();
                    var claim = new Claim(ClaimTypes.Role, tm.Role);
                    claimList.Add(claim);
                    var identity = new ClaimsIdentity(claimList);
                    var principal = new ClaimsPrincipal(identity);
                    httpContext.User = principal;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now} middleware wrong:{e.Message}");
            }
            return _next(httpContext);
        }

    }
    // 这里定义一个中间件Helper，主要作用就是给当前模块的中间件取一个别名
    public static class MiddlewareHelpers
    {
        public static IApplicationBuilder UseJwtTokenAuth(this IApplicationBuilder app)
        {
            return app.UseMiddleware<JwtTokenAuth>();
        }
    }
}
