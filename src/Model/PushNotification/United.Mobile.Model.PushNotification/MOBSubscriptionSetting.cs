using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.PushNotification
{
    [Serializable()]
    public class MOBSubscriptionSetting
    {
        private int setting;
        private bool isSubscribed;
        private bool updateSucceed;

        public int Setting
        {
            get
            {
                return this.setting;
            }
            set
            {
                this.setting = value;
            }
        }

        public bool IsSubscribed
        {
            get
            {
                return this.isSubscribed;
            }
            set
            {
                this.isSubscribed = value;
            }
        }

        public bool UpdateSucceed
        {
            get
            {
                return this.updateSucceed;
            }
            set
            {
                this.updateSucceed = value;
            }
        }
    }
}
