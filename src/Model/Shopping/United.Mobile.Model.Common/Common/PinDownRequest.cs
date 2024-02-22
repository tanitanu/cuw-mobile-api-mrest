﻿using System;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.Shopping
{
    [Serializable]
    public class PinDownRequest : MOBRequest
    {
        public string SessionId { get; set; } = string.Empty;

        public string MileagePlusAccountNumber { get; set; } = string.Empty;

        public PinDownAvailability Availability { get; set; }

        public int PremierStatusLevel { get; set; }
    }
}
