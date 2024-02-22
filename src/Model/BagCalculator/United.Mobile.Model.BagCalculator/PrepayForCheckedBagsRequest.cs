using System;
using System.Collections.Generic;
using System.Text;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.BagCalculator
{
    public class PrepayForCheckedBagsRequest: MOBRequest
    {
        public string CorrelationId { get; set; }
        public string Flow { get; set; }
        public string SessionId { get; set; }
        public string MileagePlusNumber { get; set; }
        public string PartnerRPCIds { get; set; }
    }
}
