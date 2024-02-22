using System;
using System.Collections.Generic;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.SSR;

namespace United.Mobile.Model.Travelers
{
    [Serializable()]
    public class MOBValidateWheelChairSizeRequest : MOBRequest
    {
        private string sessionId = string.Empty;
        private MOBDimensions wheelChairDimensionInfo;
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
