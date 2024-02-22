using System;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.MPRewards
{
    [Serializable()]
    public class MOBGetActionDetailsForOffersRequest : MOBRequest
    {
        private string data;
        private string mileagePlusNumber;

        public string MileagePlusNumber
        {
            get { return mileagePlusNumber; }
            set { mileagePlusNumber = value; }
        }
        
        public string Data
        {
            get { return data; }
            set { data = value; }
        }
    }
}
