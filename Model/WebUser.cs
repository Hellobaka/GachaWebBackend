using SqlSugar;
using System;
using System.Collections.Generic;

namespace GachaWebBackend.Model
{
    /// <summary>
    /// 用户说明类
    /// </summary>
    public class WebUser
    {
        /// <summary>
        /// 主键QQ
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public long QQ { get; set; }
        /// <summary>
        /// 密码(MD5之后
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// 昵称
        /// </summary>
        public string Nickname { get; set; }
        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime? RegisterDate { get; set; }
        /// <summary>
        /// 是否为开发者
        /// </summary>
        public long? Developer { get; set; }
        /// <summary>
        /// 是否已校验
        /// </summary>
        public bool CheckStatus { get; set; }
        /// <summary>
        /// 头像路径
        /// </summary>
        public string Avatar { get; set; }
        /// <summary>
        /// 收藏的卡池
        /// </summary>
        [SugarColumn(ColumnDataType = "Text", IsJson = true)]
        public List<int> SavedPools { get; set; } = new List<int>();
        /// <summary>
        /// APIKey
        /// </summary>
        public string APIKey { get; set; }    
    }
}
