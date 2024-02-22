using System;
using System.Collections.Generic;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.ManageRes
{
    [Serializable()]
    public class MOBOneClickEnrollmentResponse : MOBResponse
    {
        private string deviceId = string.Empty;
        private string sessionId = string.Empty;      
        private string recordLocator = string.Empty;
        private string lastName = string.Empty;
        private string dateCreated = string.Empty;
        private string description = string.Empty;        
        private bool isOneTraveler = false;
        private string selectTravelerHeader = string.Empty;
        private string emailAddressHint  = string.Empty;       
        private string emailInvalidError = string.Empty;
        private string emailDuplicateError = string.Empty;
        private string sendMarketEmailText = string.Empty;
        private string title = string.Empty;
        private string header = string.Empty;
        private string cancelButton = string.Empty;
        private string createAccountButton = string.Empty;
        private bool isGetPNRByRecordLocatorCall = false;
        private bool isGetPNRByRecordLocator = false;
        private string flow = string.Empty;
        private List<TravelerInfo> travelersInfo = null;
        private List<MOBKVP> benefits = null;


        public string DeviceId
        {
            get
            {
                return this.deviceId;
            }
            set
            {
                this.deviceId = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string SessionId
        {
            get
            {
                return this.sessionId;
            }
            set
            {
                this.sessionId = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string RecordLocator
        {
            get
            {
                return this.recordLocator;
            }
            set
            {
                this.recordLocator = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        public string LastName
        {
            get
            {
                return this.lastName;
            }
            set
            {
                this.lastName = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        public string DateCreated
        {
            get
            {
                return this.dateCreated;
            }
            set
            {
                this.dateCreated = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        public string Description
        {
            get
            {
                return this.description;
            }
            set
            {
                this.description = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string EmailAddressHint
        {
            get
            {
                return this.emailAddressHint;
            }
            set
            {
                this.emailAddressHint = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string EmailInvalidError
        {
            get
            {
                return this.emailInvalidError;
            }
            set
            {
                this.emailInvalidError = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        public string EmailDuplicateError
        {
            get
            {
                return this.emailDuplicateError;
            }
            set
            {
                this.emailDuplicateError = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string SelectTravelerHeader
        {
            get
            {
                return this.selectTravelerHeader;
            }
            set
            {
                this.selectTravelerHeader = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public bool IsOneTraveler
        {
            get
            {
                return this.isOneTraveler;
            }
            set
            {
                this.isOneTraveler = value;
            }
        }

        public string Header
        {
            get
            {
                return this.header;
            }
            set
            {
                this.header = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
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

        public string SendMarketEmailText
        {
            get
            {
                return this.sendMarketEmailText;
            }
            set
            {
                this.sendMarketEmailText = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        public string CancelButton
        {
            get
            {
                return this.cancelButton;
            }
            set
            {
                this.cancelButton = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        public string CreateAccountButton
        {
            get
            {
                return this.createAccountButton;
            }
            set
            {
                this.createAccountButton = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        public bool IsGetPNRByRecordLocator
        {
            get
            {
                return this.isGetPNRByRecordLocator;
            }
            set
            {
                this.isGetPNRByRecordLocator = value;
            }
        }
        public bool IsGetPNRByRecordLocatorCall
        {
            get
            {
                return this.isGetPNRByRecordLocatorCall;
            }
            set
            {
                this.isGetPNRByRecordLocatorCall = value;
            }
        }
        public string Flow
        {
            get
            {
                return this.flow;
            }
            set
            {
                this.flow = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
            }
        }

        public List<MOBKVP> Benefits 
        {
            get
            {
                return this.benefits;
            }
            set
            {
                this.benefits = value;
            }
        }
        public List<TravelerInfo>TravelersInfo
        {
            get
            {
                return this.travelersInfo;
            }
            set
            {
                this.travelersInfo = value;
            }
        }
    }
    [Serializable()]
    public class TravelerInfo
    {
        private string travelerName = string.Empty;
        private string emailAddress = string.Empty;
        private string sharesPosition = string.Empty;
        private bool showMarketingEmailCheck = false;

        public string TravelerName
        {
            get
            {
                return this.travelerName;
            }
            set
            {
                this.travelerName = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
    
        public string EmailAddress
        {
            get
            {
                return this.emailAddress;
            }
            set
            {
                this.emailAddress = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        public string SharesPosition
        {
            get
            {
                return this.sharesPosition;
            }
            set
            {
                this.sharesPosition = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public bool ShowMarketingEmailCheck
        {
            get
            {
                return this.showMarketingEmailCheck;
            }
            set
            {
                this.showMarketingEmailCheck = value;
            }
        }

    }  
}
