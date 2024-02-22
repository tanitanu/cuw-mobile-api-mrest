using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.Common
{
    
   public class MOBPartnerSSOTokenResponse:MOBResponse
    {
        public string Token  { get; set; }
        public string MileagePlusNumber { get; set; }
    }
}
