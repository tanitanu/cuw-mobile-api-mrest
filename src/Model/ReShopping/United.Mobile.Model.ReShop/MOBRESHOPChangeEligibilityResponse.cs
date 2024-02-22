using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Service.Presentation.ReservationResponseModel;
using MOBSHOPResponseStatusItem = United.Mobile.Model.Common.MOBSHOPResponseStatusItem;

namespace United.Mobile.Model.ReShop
{
    [Serializable]
    public class MOBRESHOPChangeEligibilityResponse : MOBResponse
    {
        private ReservationDetail reservationDetail;
        public ReservationDetail ReservationDetail
        {
            get { return reservationDetail; }
            set { reservationDetail = value; }
        }
        private bool pathEligible;
        public bool PathEligible
        {
            get { return pathEligible; }
            set { pathEligible = value; }
        }

        private string failedRule;

        public string FailedRule
        {
            get { return failedRule; }
            set { failedRule = value; }
        }

        private string sessionId;
        public string SessionId
        {
            get { return sessionId; }
            set { sessionId = value; }
        }

        private string redirectURL = string.Empty;
        public string RedirectURL
        {
            get { return redirectURL; }
            set { redirectURL = value; }
        }

        private bool awardTravel = false;
        public bool AwardTravel
        {
            get { return awardTravel; }
            set { awardTravel = value; }
        }

        private string sponsorMileagePlus = string.Empty;
        public string SponsorMileagePlus
        {
            get { return sponsorMileagePlus; }
            set { sponsorMileagePlus = value; }
        }

        private string webShareToken = string.Empty;
        private string webSessionShareUrl = string.Empty;
        private Collection<Characteristic> characteristics;

        public Collection<Characteristic> Characteristics { get { return this.characteristics; } set { this.characteristics = value; } }
        public string WebShareToken { get { return this.webShareToken; } set { this.webShareToken = value; } }
        public string WebSessionShareUrl { get { return this.webSessionShareUrl; } set { this.webSessionShareUrl = value; } }

        private List<MOBShuttleOffer> shuttleOffer;
        public List<MOBShuttleOffer> ShuttleOffer { get { return this.shuttleOffer; } set { this.shuttleOffer = value; } }

        private MOBSHOPReservation reservation;
        public MOBSHOPReservation Reservation
        {
            get
            {
                return this.reservation;
            }
            set
            {
                this.reservation = value;
            }
        }

        private MOBSHOPResponseStatusItem responseStatusItem;

        public MOBSHOPResponseStatusItem ResponseStatusItem
        {
            get { return responseStatusItem; }
            set { responseStatusItem = value; }
        }

        private ShopResponse shopResponse;

        public ShopResponse ShopResponse
        {
            get { return shopResponse; }
            set { shopResponse = value; }
        }
        private List<MOBPNRPassenger> pnrTravelers = null;

        public List<MOBPNRPassenger> PnrTravelers
        {
            get { return pnrTravelers; }
            set { pnrTravelers = value; }
        }

        private List<MOBPNRSegment> pnrSegment = null;

        [JsonPropertyName("pnrSegment")]
        public List<MOBPNRSegment> PnrSegment
        {
            get { return pnrSegment; }
            set { pnrSegment = value; }
        }

        private bool exceptionPolicyEligible;
        public bool ExceptionPolicyEligible
        {
            get { return exceptionPolicyEligible; }
            set { exceptionPolicyEligible = value; }
        }

        private bool sameDayChangeEligible;
        public bool SameDayChangeEligible
        {
            get { return sameDayChangeEligible; }
            set { sameDayChangeEligible = value; }
        }

        private List<MOBPNRAdvisory> advisoryInfo;
        public List<MOBPNRAdvisory> AdvisoryInfo { get { return this.advisoryInfo; } set { this.advisoryInfo = value; } }

        private OnTheFlyEligibility onTheFlyEligibility;
        public OnTheFlyEligibility OnTheFlyEligibility { get { return this.onTheFlyEligibility; } set { this.onTheFlyEligibility = value; } }


        //Book With Travel Credit Session
        private string bwcsessionId = string.Empty;
        [JsonProperty("bwcsessionId")]
        public string BWCSessionId
        {
            get
            {
                return this.bwcsessionId;
            }
            set
            {
                this.bwcsessionId = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        //private MOBSHOPResponseStatusItem inEligibilityReasons1;
        //public MOBSHOPResponseStatusItem InEligibilityReasons1
        //{
        //    get { return inEligibilityReasons1; }
        //    set { inEligibilityReasons1 = value; }
        //}
        private bool isShowChangeEligible = false;
        public bool IsShowChangeEligible
        {
            get { return isShowChangeEligible; }
            set { isShowChangeEligible = value; }
        }

        private EligibilityReasonsCode inEligibilityReasons;
        public EligibilityReasonsCode InEligibilityReasons
        {
            get { return inEligibilityReasons; }
            set { inEligibilityReasons = value; }
        }
    }
    public class OnTheFlyEligibility
    {
        private bool offerEligible;
        [JsonProperty(PropertyName = "OfferEligible")]
        [JsonPropertyName("OfferEligible")]
        public bool OfferEligible
        {
            get { return offerEligible; }
            set { offerEligible = value; }
        }
    }

    public class EligibilityReasonsCode
    {
        private string title;
        public string Title
        {
            get
            {
                return this.title;
            }
            set
            {
                this.title = value;
            }
        }
        private string message;
        public string Message
        {
            get
            {
                return this.message;
            }
            set
            {
                this.message = value;
            }
        }

        private List<ActionDetails> actionButtons;
        public List<ActionDetails> ActionButtons
        {
            get { return actionButtons; }
            set { actionButtons = value; }
        }
    }

    public class ActionDetails
    {
        private string actionText;
        public string ActionText
        {
            get
            {
                return this.actionText;
            }
            set
            {
                this.actionText = value;
            }
        }
        private string actionURL;
        public string ActionURL
        {
            get
            {
                return this.actionURL;
            }
            set
            {
                this.actionURL = value;
            }
        }
    }
}
