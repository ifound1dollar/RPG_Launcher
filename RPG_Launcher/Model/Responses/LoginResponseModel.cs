using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPG_Launcher.Model.Responses
{
    public class LoginResponseModel
    {
        public string Username { get; set; } = string.Empty;
        public string PrimaryEmail { get; set; } = string.Empty;
        public string SecondaryEmail { get; set; } = string.Empty;
        public int LoginStatusCode { get; set; } = -1;
        public string RefreshToken { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiration { get; set; }
    }

    /// LOGIN STATUS CODES:
    /// 0  : full access (complete login)
    /// 1  : partial login, awaiting MFA code submission
    /// 10 : primary email not yet verified
    /// 20 : password needs reset
    /// 30 : MFA not yet set up
    /// 40 : account locked while awaiting MFA hard reset
}
