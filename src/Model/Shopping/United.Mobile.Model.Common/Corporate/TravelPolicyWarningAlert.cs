using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.Shopping.Common.Corporate
{
    [Serializable()]
    public class TravelPolicyWarningAlert
    {
        private TravelPolicy travelPolicy;
        private List<InfoWarningMessages> infoWarningMessages;
        public List<InfoWarningMessages> InfoWarningMessages
        {
            get { return infoWarningMessages; }
            set { infoWarningMessages = value; }
        }

        public TravelPolicy TravelPolicy
        {
            get
            {
                return travelPolicy;
            }
            set
            {
                travelPolicy = value;
            }
        }
    }
}
