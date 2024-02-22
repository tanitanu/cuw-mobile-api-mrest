using System.Collections.Generic;

namespace United.Mobile.Model.Shopping
{
    public class MOBOnScreenAlert
    {
        private string title = string.Empty;
        private string message = string.Empty;
        private List<MOBOnScreenActions> actions;
        private MOBOnScreenAlertType alertType;
        private int paxId;

        public int PaxId
        {
            get { return paxId; }
            set { paxId = value; }
        }


        public MOBOnScreenAlertType AlertType
        {
            get { return alertType == 0 ? MOBOnScreenAlertType.OTHER : alertType; }
            set { alertType = value; }
        }

        public string Title
        {
            get
            {
                return this.title;
            }
            set
            {
                this.title = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string Message
        {
            get
            {
                return this.message;
            }
            set
            {
                this.message = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public List<MOBOnScreenActions> Actions
        {
            get { return actions; }
            set { actions = value; }
        }
    }
    public enum MOBOnScreenAlertType
    {
        OTHER = 0,
        FSRMONEYPLUSMILES = 1,
        BUYMILES = 2,
        OAFLASHSALE = 3,
        RASCHECK = 4,
        FSRMILEAGEPRICINGMONEYPLUSMILES = 5, // CurrentPricingType : ETC
        FSRMONEYPLUSMILESMILEAGEPRICING = 6, // CurrentPricingType : MPM
        CONFIRMINFANTTRAVELTYPE = 7,
        WHEELCHAIRFITS =8
    }
}
