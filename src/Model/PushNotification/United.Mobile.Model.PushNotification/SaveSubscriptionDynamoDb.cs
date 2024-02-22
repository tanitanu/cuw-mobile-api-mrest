using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.PushNotification
{
    [Serializable()]
    public class SaveSubscriptionDynamoDb
    {
        public int ApplicationId { get; set; }
        public string DeviceId { get; set; }
        public string PushToken { get; set; }
        public int SubscriptionSetting { get; set; }
        public bool IsSubscribed { get; set; }
        public bool PushEnabled { get; set; }
        public bool IsBoardingNowSubscribed { get; set; }
    }
}
