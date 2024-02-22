using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.Model.Shopping
{
    [Serializable]
    public class MOBOnScreenActions
    {
        private string actionTitle = string.Empty;
        private MOBOnScreenAlertActionType actionType;
        private string actionURL = string.Empty;
        private string webSessionShareUrl = string.Empty;
        private string webShareToken = string.Empty;

        public string ActionTitle
        {
            get
            {
                return this.actionTitle;
            }
            set
            {
                this.actionTitle = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        public string WebSessionShareUrl
        {
            get
            {
                return this.webSessionShareUrl;
            }
            set
            {
                this.webSessionShareUrl = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string WebShareToken
        {
            get
            {
                return this.webShareToken;
            }
            set
            {
                this.webShareToken = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        public MOBOnScreenAlertActionType ActionType
        {
            get { return actionType; }
            set { actionType = value; }
        }

        public string ActionURL
        {
            get { return actionURL; }
            set { actionURL = value; }
        }
    }

    public enum MOBOnScreenAlertActionType
    {
        NAVIGATE_TO_EXTERNAL = 0,
        DISMISS_ALERT = 1,
        NAVIGATE_TO_FARELOCK = 2,
        ADD_MILES = 3,
        NAVIGATE_BACK = 4,
        CANCEL = 5,
        NAVIGATE_TO_FSR = 6,
        BUY_A_SEAT = 7,
        ADD_INFANT_IN_LAP = 8,
        CONTINUE_TO_SELECTTRIP = 9,
        CALL_US =10
    }
}
