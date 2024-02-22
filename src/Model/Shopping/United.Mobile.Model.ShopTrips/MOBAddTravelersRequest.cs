using System.Collections.Generic;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.Shopping
{
    public class MOBAddTravelersRequest : MOBRequest
    {
        public bool IsAward { get; set; }
        public int PremierStatusLevel { get; set; }
        public string MileagePlusNumber { get; set; }
        public List<MOBTravelerType> TravelerTypes { get; set; }
        public string CartId { get; set; }
        public string Flow { get; set; }
        public string SessionId { get; set; }
    }
}