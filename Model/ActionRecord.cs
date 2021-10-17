using System;

namespace GachaWebBackend.Model
{
    /// <summary>
    /// 操作记录
    /// </summary>
    public class ActionRecord
    {
        /// <summary>
        /// 来源IP
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// 操作者QQ
        /// </summary>
        public string Operator { get;set; }
        /// <summary>
        /// 操作名称
        /// </summary>
        public string Action { get; set; }
        /// <summary>
        /// 操作状态
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// 操作备注
        /// </summary>
        public string Info { get; set; }
        /// <summary>
        /// 操作发生时间
        /// </summary>
        public DateTime Time { get; set; }
    }
}
