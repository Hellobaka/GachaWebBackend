using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GachaWebBackend.Interface
{
    public interface IPluginOrderModel
    {
        bool ImplementFlag { get; set; }
        string GetOrderStr();
        bool Judge(string destStr);
        //FunctionResult Progress(PluginMessage e);
    }
}
