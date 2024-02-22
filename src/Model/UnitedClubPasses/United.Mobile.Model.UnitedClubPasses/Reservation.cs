using System;
using System.Collections.Generic;
using United.Definition;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class Reservation 
    {
        #region IPersist Members

        private string objectName = "United.Persist.Definition.Shopping.Reservation";
        public string ObjectName
        {
            get
            {
                return this.objectName;
            }
            set
            {
                this.objectName = value;
            }
        }

        #endregion

        public string SessionId { get; set; }
        public string CartId { get; set; }
        public string MetaSessionId { get; set; }
        public bool IsSignedInWithMP { get; set; }
        public List<SHOPTrip> Trips { get; set; }
        //public List<SHOPPrice> Prices { get; set; }
        public List<SHOPTax> Taxes { get; set; }
        public List<string> TravelerKeys { get; set; }
        //public SerializableDictionary<string, SHOPTraveler> Travelers { get; set; }
        //public SerializableDictionary<string, CPTraveler> TravelersCSL { get; set; }
        //public List<SeatPrice> SeatPrices { get; set; }
        public List<CreditCard> CreditCards { get; set; }
        public string CSLReservationJSONFormat { get; set; }
        //public SHOPRegisterOfferRequest SHOPRegisterOfferRequest { get; set; }
        //public List<SHOPTravelOption> TravelOptions { get; set; }
        //public SHOPClubPassPurchaseRequest ClubPassPurchaseRequest { get; set; }
        public int NumberOfTravelers { get; set; }
        public bool TravelersRegistered { get; set; }
        public string SearchType = string.Empty;
        public List<MOBAddress> CreditCardsAddress { get; set; }
        //public List<SHOPFareRules> FareRules { get; set; }

        //public CPPhone ReservationPhone { get; set; }
        public Email ReservationEmail { get; set; }

        private string epaMessageTitle = string.Empty;
        private string epaMessage = string.Empty;
        private Boolean override24HrFlex;

        public Boolean Override24HrFlex { get { return this.override24HrFlex; } set { this.override24HrFlex = value; } }
        public string EPAMessageTitle { get; set; }
        public string EPAMessage { get; set; }

        //private List<LMXTraveler> lmxtravelers;
        //public List<LMXTraveler> LMXTravelers
        //{
        //    get
        //    {
        //        return this.lmxtravelers;
        //    }
        //    set
        //    {
        //        this.lmxtravelers = value;
        //    }
        //}

        private string pointOfSale = string.Empty;
        public string PointOfSale
        {
            get { return this.pointOfSale; }
            set { this.pointOfSale = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); }
        }

        //private SHOPFareLock fareLock;
        //public SHOPFareLock FareLock
        //{
        //    get { return this.fareLock; }
        //    set { this.fareLock = value; }
        //}

        private bool unregisterFareLock;
        public bool UnregisterFareLock
        {
            get { return this.unregisterFareLock; }
            set { this.unregisterFareLock = value; }
        }

        private bool awardTravel = false;
        public bool AwardTravel
        {
            get
            {
                return this.awardTravel;
            }
            set
            {
                this.awardTravel = value;
            }
        }

        public string FlightShareMessage = string.Empty;
        public string PKDispenserPublicKey = string.Empty;

        //List<LmxFlight> lmxFlights;
        //public List<LmxFlight> LMXFlights
        //{
        //    get
        //    {
        //        return this.lmxFlights;
        //    }
        //    set
        //    {
        //        this.lmxFlights = value;
        //    }
        //}

        
        public List<MOBItem> TCDAdvisoryMessages { get; set; }
        

        private bool isRefundable;
        public bool IsRefundable
        {
            get
            {
                return this.isRefundable;
            }
            set
            {
                this.isRefundable = value;
            }
        }

        private bool isInternational;
        public bool ISInternational
        {
            get
            {
                return this.isInternational;
            }
            set
            {
                this.isInternational = value;
            }
        }


        private string ineligibleToEarnCreditMessage = string.Empty;
        public string IneligibleToEarnCreditMessage
        {
            get
            {
                return this.ineligibleToEarnCreditMessage;
            }
            set
            {
                this.ineligibleToEarnCreditMessage = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        private string oaIneligibleToEarnCreditMessage = string.Empty;
        public string OaIneligibleToEarnCreditMessage
        {
            get
            {
                return this.oaIneligibleToEarnCreditMessage;
            }
            set
            {
                this.oaIneligibleToEarnCreditMessage = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        private bool isFlexibleSegmentExist;
        public bool ISFlexibleSegmentExist
        {
            get
            {
                return this.isFlexibleSegmentExist;
            }
            set
            {
                this.isFlexibleSegmentExist = value;
            }
        }
        private bool getALLSavedTravelers = true;
        public bool GetALLSavedTravelers
        {
            get
            {
                return this.getALLSavedTravelers;
            }
            set
            {
                this.getALLSavedTravelers = value;
            }
        }
        private bool isGetProfileCalledForProfileOwner = false;

        public bool IsELF { get; set; }
        public bool IsMetaSearch { get; set; }

        public string ElfUpgradeMessagesForMetaSearch { get; set; }

        public bool IsUpgradedFromEntryLevelFare { get; set; }


        public bool IsGetProfileCalledForProfileOwner
        {
            get
            {
                return this.isGetProfileCalledForProfileOwner;
            }
            set
            {
                this.isGetProfileCalledForProfileOwner = value;
            }
        }

        private bool isCubaTravel;

        public bool IsCubaTravel
        {
            get { return isCubaTravel; }
            set { isCubaTravel = value; }
        }

        //private CPCubaTravel cubaTravelInfo;

        //public CPCubaTravel CubaTravelInfo
        //{
        //    get { return cubaTravelInfo; }
        //    set { cubaTravelInfo = value; }
        //}

        private FormofPayment formOfPaymentType;

        public FormofPayment FormOfPaymentType
        {
            get { return formOfPaymentType; }
            set { formOfPaymentType = value; }
        }

        private PayPal payPal;

        public PayPal PayPal
        {
            get { return payPal; }
            set { payPal = value; }
        }

        private PayPalPayor payPalPayor;
        public PayPalPayor PayPalPayor
        {
            get { return payPalPayor; }
            set { payPalPayor = value; }
        }

        //private List<TypeOption> fopOptions;
        //public List<TypeOption> FOPOptions
        //{
        //    get
        //    {
        //        return this.fopOptions;
        //    }
        //    set { this.fopOptions = value; }
        //}

       
        public List<MOBItem> ELFMessagesForRTI { get; set; }




        //public static implicit operator Service.Presentation.ReservationModel.Reservation(Reservation v)
        //{
        //    throw new NotImplementedException();
        //}

        private MasterpassSessionDetails masterpassSessionDetails;
        public MasterpassSessionDetails MasterpassSessionDetails
        {
            get { return masterpassSessionDetails; }
            set { masterpassSessionDetails = value; }
        }

        private Masterpass masterpass;
        public Masterpass Masterpass
        {
            get { return masterpass; }
            set { masterpass = value; }
        }

        private bool isReshopChange;
        public bool IsReshopChange
        {
            get { return isReshopChange; }
            set { isReshopChange = value; }
        }

        //private List<SHOPReshopTrip> rehopTrips;
        //public List<SHOPReshopTrip> ReshopTrips
        //{
        //    get { return rehopTrips; }
        //    set { rehopTrips = value; }
        //}

        //private List<SHOPReshopTrip> reshopUsedTrips;
        //public List<SHOPReshopTrip> ReshopUsedTrips
        //{
        //    get { return reshopUsedTrips; }
        //    set { reshopUsedTrips = value; }
        //}

        //private SHOPReshop reshop;
        //public SHOPReshop Reshop
        //{
        //    get { return reshop; }
        //    set { reshop = value; }
        //}

        private bool isSSA;

        public bool IsSSA
        {
            get { return isSSA; }
            set { isSSA = value; }
        }

       

        public List<MOBItem> SeatAssignmentMessage { get; set; }
        
        //private SHOPReservationInfo shopReservationInfo;
        //public SHOPReservationInfo ShopReservationInfo
        //{
        //    get { return shopReservationInfo; }
        //    set { shopReservationInfo = value; }
        //}

        private int aboveGoldMembers;

        public int AboveGoldMembers
        {
            get { return aboveGoldMembers; }
            set { aboveGoldMembers = value; }
        }

        private int goldMembers;

        public int GoldMembers
        {
            get { return goldMembers; }
            set { goldMembers = value; }
        }
        private int silverMembers;

        public int SilverMembers
        {
            get { return silverMembers; }
            set { silverMembers = value; }
        }

        private int noOfFreeEplusWithSubscriptions;

        public int NoOfFreeEplusWithSubscriptions
        {
            get { return noOfFreeEplusWithSubscriptions; }
            set { noOfFreeEplusWithSubscriptions = value; }
        }

        //private TripInsuranceFile tripInsuranceFile;
        //public TripInsuranceFile TripInsuranceFile
        //{
        //    get { return this.tripInsuranceFile; }
        //    set { this.tripInsuranceFile = value; }
        //}
        //private SHOPReservationInfo2 shopReservationInfo2;
        //public SHOPReservationInfo2 ShopReservationInfo2
        //{
        //    get { return shopReservationInfo2; }
        //    set { shopReservationInfo2 = value; }
        //}
        //private List<Section> alertMessages;
        //public List<Section> AlertMessages
        //{
        //    get { return this.alertMessages; }
        //    set { this.alertMessages = value; }
        //}
        private bool isRedirectToSecondaryPayment = false;
        public bool IsRedirectToSecondaryPayment
        {
            get
            {
                return this.isRedirectToSecondaryPayment;
            }
            set
            {
                this.isRedirectToSecondaryPayment = value;
            }
        }

        private string recordLocator = string.Empty;
        public string RecordLocator
        {
            get
            {
                return this.recordLocator;
            }
            set
            {
                this.recordLocator = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
            }
        }

        private List<string> messages;
        public List<string> Messages
        {
            get
            {
                return this.messages;
            }
            set
            {
                this.messages = value;
            }
        }

        private string checkedbagChargebutton;

        public string CheckedbagChargebutton
        {
            get { return checkedbagChargebutton; }
            set { checkedbagChargebutton = value; }
        }

        private bool isBookingCommonFOPEnabled;

        public bool IsBookingCommonFOPEnabled
        {
            get { return isBookingCommonFOPEnabled; }
            set { isBookingCommonFOPEnabled = value; }
        }

        private bool isReshopCommonFOPEnabled;

        public bool IsReshopCommonFOPEnabled
        {
            get { return isReshopCommonFOPEnabled; }
            set { isReshopCommonFOPEnabled = value; }
        }
        private bool isPostBookingCommonFOPEnabled;

        public bool IsPostBookingCommonFOPEnabled
        {
            get { return isPostBookingCommonFOPEnabled; }
            set { isPostBookingCommonFOPEnabled = value; }
        }
    }
}
