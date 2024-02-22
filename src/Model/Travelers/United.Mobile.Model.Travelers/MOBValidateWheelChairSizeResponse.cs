using System;
using MOBAlertMessages = United.Mobile.Model.Common.MOBAlertMessages;
using United.Mobile.Model.Common.SSR;

namespace United.Mobile.Model.Travelers
{
    [Serializable()]
    public class MOBValidateWheelChairSizeResponse : MOBResponse
    {
        private string sessionId = string.Empty;
        private MOBDimensions wheelChairDimensionInfo;
        private MOBAlertMessages wheelChairErrorMessages;
        public MOBAlertMessages WheelChairErrorMessages
        {
            get { return wheelChairErrorMessages; }
            set { wheelChairErrorMessages = value; }
        }
        public string SessionId
        {
            get
            {
                return sessionId;
            }
            set
            {
                this.sessionId = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        public MOBDimensions WheelChairDimensionInfo
        {
            get { return wheelChairDimensionInfo; }
            set { wheelChairDimensionInfo = value; }
        }
    }
}
