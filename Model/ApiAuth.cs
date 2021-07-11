using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GachaWebBackend.Model
{
    public class ApiAuth
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long ApiID { get; set; }
        public string ApiKey { get; set; }
    }
}
