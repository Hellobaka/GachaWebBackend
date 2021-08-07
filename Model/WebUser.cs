using SqlSugar;
using System;
using System.Collections.Generic;

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
        public string Avatar { get; set; }
        [SugarColumn(ColumnDataType = "Text", IsJson = true)]
        public List<int> SavedPools { get; set; } = new List<int>();


    }
}
