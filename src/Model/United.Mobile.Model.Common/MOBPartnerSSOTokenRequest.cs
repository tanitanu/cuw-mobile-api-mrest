using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.Common
{
    public class MOBPartnerSSOTokenRequest:MOBRequest
    {
        public string MileagePlusNumber { get; set; }

        public int CustomerId { get; set; }

        public string HashPinCode { get; set; }
    }
}
