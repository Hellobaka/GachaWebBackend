using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GachaWebBackend.Model
{
    public class CaptchaSave
    {
        public bool Pass { get; set; } = false;
        public bool CanReGet { get; set; } = false;
        public string Email { get; set; }
        public int Code { get; set; }
    }
}
