using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPG_Launcher.Model.Responses
{
    public class ConnectTokenResponseModel
    {
        public string ConnectToken { get; set; } = string.Empty;
        public DateTime ConnectTokenExpiration { get; set; }
    }
}
