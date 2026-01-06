using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPG_Launcher.Model.Data
{
    public class TokenResponse
    {
        public string RefreshToken { get; set; }    = string.Empty;
        public string AccessToken { get; set; }     = string.Empty;
    }
}
