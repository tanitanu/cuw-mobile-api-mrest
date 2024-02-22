using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.CSLModels
{
    public class PartnerSSOTokenDecode
    {
        public string Version { get; set; }
        public SSOTokenInfo EncryptedToken { get; set; }
    }

    public class SSOTokenInfo
    {
        public string SSOToken { get; set; }
    }

}
