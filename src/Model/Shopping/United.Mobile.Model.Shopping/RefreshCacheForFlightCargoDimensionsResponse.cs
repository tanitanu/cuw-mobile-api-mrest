using System;
using System.Collections.Generic;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.SSR;

namespace United.Mobile.Model.Shopping
{

    [Serializable]
    public class RefreshCacheForFlightCargoDimensionsResponse : MOBResponse
    {
        public List<MOBDimensions> FlightCargoDimensions { get; set; }
    }
}
