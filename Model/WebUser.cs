using SqlSugar;
using System;

namespace GachaWebBackend.Model
{
    public class WebUser
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long QQ { get; set; }

        public string Password { get; set; }

        public string Email { get; set; }

        public string Nickname { get; set; }

        public DateTime? RegisterDate { get; set; }

        public long? Developer { get; set; }
        public bool CheckStatus { get; set; }
    }
}
