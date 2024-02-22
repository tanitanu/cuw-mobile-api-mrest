using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.PushNotification
{
    [Serializable()]
    public class MOBSubscriptionSettingsResponse : MOBResponse
    {
        private List<MOBSubscriptionSetting> settings;

        public List<MOBSubscriptionSetting> Settings
        {
            get { return this.settings; }
            set { this.settings = value; }
        }
    }
}
