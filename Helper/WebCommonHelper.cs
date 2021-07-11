using GachaWebBackend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GachaWebBackend.Helper
{
    public static class WebCommonHelper
    {
        private const string _SALT = "33ae826c28108ccfd6890ff6d942be89";
        public static ApiResponse SetOk(string msg = "ok", object data = null)
        {
            return new ApiResponse { code = 200, msg = msg, data = data };
        }
        public static ApiResponse SetError(string msg = "error", object data = null)
        {
            return new ApiResponse { code = 404, msg = msg, data = data };
        }
        public static bool CompareKeyProp(this WebUser user, WebUser target)
        {
            return user.Email == target.Email || user.QQ == target.QQ;
        }
        public static string MD5Encrypt(string str)
        {
            str = _SALT + str;
            byte[] result = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(Encoding.UTF8.GetBytes(str));
            StringBuilder output = new(16);
            for (int i = 0; i < result.Length; i++)
            {
                output.Append(result[i].ToString("x2"));
            }
            return output.ToString();
        }
    }
}
