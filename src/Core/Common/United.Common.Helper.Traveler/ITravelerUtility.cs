using System.Collections.Generic;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.SSR;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Service.Presentation.CommonEnumModel;
using United.Service.Presentation.PaymentModel;
using MOBFOPTravelCredit = United.Mobile.Model.Shopping.FormofPayment.MOBFOPTravelCredit;
using United.Services.FlightShopping.Common.DisplayCart;
using ProfileResponse = United.Mobile.Model.Shopping.ProfileResponse;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using United.Mobile.Model;
using System.Threading.Tasks;
using United.Mobile.Model.Travelers;
using System.Collections.ObjectModel;

namespace United.Common.Helper.Traveler
{
    public interface ITravelerUtility
    {
        List<MOBCPTraveler> AssignInfantWithSeat(Reservation bookingPathReservation, List<MOBCPTraveler> travelers);
        List<MOBCPTraveler> AssignInfantInLap(Reservation bookingPathReservation, List<MOBCPTraveler> travelers);
        bool EnableYoungAdultValidation(bool isReshop = false);
        int GetChildInLapCount(MOBCPTraveler traveler, List<int> travelerAges, int childInLapCount, string firstLOFDepDate);
        void ValidateTravelerAges(List<int> travelerAges, int inLapChildCount = 0);
        bool ShowUpliftTpiMessage(Reservation reservation, string formOfPaymentType);
        void ValidateTravelersForCubaReason(List<MOBCPTraveler> travelersCSL, bool isCuba);
        bool EnableTPI(int appId, string appVersion, int path);
        bool IsEnableNavigation(bool isReshop = false);
        bool IsFFCTravelerChanged(List<MOBCPTraveler> travelers, SerializableDictionary<string, MOBCPTraveler> travelerCSLBeforeRegister);
        bool IsETCTravelerChanged(List<MOBCPTraveler> travelers, SerializableDictionary<string, MOBCPTraveler> travelerCSLBeforeRegister, List<MOBFOPCertificate> certificates);
        T ObjectToObjectCasting<T, R>(R request);
        Task<ProfileResponse> GetCSLProfileResponseInSession(string sessionId);

        string GetPriceAfterChaseCredit(decimal price);
        string GetPriceAfterChaseCredit(decimal price, string chaseCrediAmount);
        void AddGrandTotalIfNotExistInPrices(List<MOBSHOPPrice> prices);
        Task<LoadReservationAndDisplayCartResponse> GetCartInformation(string sessionId, Mobile.Model.MOBApplication application, string device, string cartId, string token, WorkFlowType workFlowType = WorkFlowType.InitialBooking);
        Task<TPIInfo> GetTPIDetails(Service.Presentation.ProductResponseModel.ProductOffer productOffer, string sessionId, bool isShoppingCall, bool isBookingPath = false, int appid = -1, string appVersion = "");
        bool IsEnableUKChildrenTaxReprice(bool isReShop, int appid, string appversion);
        Task<List<TravelSpecialNeed>> ValidateSpecialNeedsAgaintsMasterList(List<TravelSpecialNeed> specialNeeds, TravelSpecialNeeds masterList, int appId = 0, string appVersion = "", List<MOBItem> catalogItems = null);
        string GetTypeCodeByAge(int age, bool iSChildInLap = false);
        string GetPaxDescriptionByDOB(string date, string deptDateFLOF);
        bool EnableServicePlacePassBooking(int appId, string appVersion);
        Task<PlacePass> GetGenericPlacePass(string destinationAiportCode, string tripType, string sessionId, int appID, string appVersion, string deviceId, string logAction, string utm_campain);
        bool EnablePlacePassBooking(int appId, string appVersion);
        Task<PlacePass> GetEligiblityPlacePass(string destinationAiportCode, string tripType, string sessionId, int appID, string appVersion, string deviceId, string logAction);
        void RemoveelfMessagesForRTI(ref Reservation bookingPathReservation);
        void ValidateTravelersCSLForCubaReason(MOBSHOPReservation reservation);
        InfoWarningMessages GetPriceChangeMessage();
        void UpdateInhibitMessage(ref Reservation reservation);
        bool EnableConcurrCardPolicy(bool isReshop = false);
        InfoWarningMessages GetConcurrCardPolicyMessage(bool isPayment = false);
        MOBCCAdStatement BuildChasePromo(string adType);
        Task<List<MOBMobileCMSContentMessages>> GetProductBasedTermAndConditions(string sessionId, United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, bool isPost);
        string GetBookingPaymentTargetForRegisterFop(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse);
        string GetPaymentTargetForRegisterFop(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, bool isCompleteFarelockPurchase = false);
        bool EnableTravelerTypes(int appId, string appVersion, bool reshop = false);
        bool EnableReshopCubaTravelReasonVersion(int appId, string appVersion);
        bool IsETCEnabledforMultiTraveler(int applicationId, string appVersion);
        bool IsETCCombinabilityEnabled(int applicationId, string appVersion);
        bool IncludeMoneyPlusMiles(int appId, string appVersion);
        bool EnableSpecialNeeds(int appId, string appVersion);
        bool IncludeFFCResidual(int appId, string appVersion);
        List<string[]> LoadCountries();
        string GetAccessCode(string inputCountryCode);

        Task<(FOPResponse, Reservation bookingPathReservation)> LoadBasicFOPResponse(Session session, Reservation bookingPathReservation);
        bool IncludeTravelCredit(int appId, string appVersion);
        bool IncludeTravelBankFOP(int appId, string appVersion);
        bool EnableEPlusAncillary(int appID, string appVersion, bool isReshop = false);
        List<MOBSHOPPrice> UpdatePricesForBundles(MOBSHOPReservation reservation, Mobile.Model.Shopping.RegisterOfferRequest request, int appID, string appVersion, bool isReshop, string productId = "");
        Task<double> GetTravelBankBalance(string sessionId);
        void AddETCToTC(List<MOBFOPTravelCredit> travelCredits, ETCCertificates etc, bool islookUp);
        void AddFFCandFFCR(List<MOBCPTraveler> travelers, List<MOBFOPTravelCredit> travelCredits, FFCRCertificates ffcr, List<MOBMobileCMSContentMessages> lookUpMessages, bool isOtfFFC, bool islookUp);
        Task<MOBCPTraveler> GetProfileOwnerTravelerCSL(string sessionID);
        void UpdateAllEligibleTravelersList(Reservation bookingPathReservation);
        void AddFreeBagDetailsInPrices(DisplayCart displayCart, List<MOBSHOPPrice> prices);
        List<ShopBundleEplus> GetTravelOptionEplusAncillary(Services.FlightShopping.Common.DisplayCart.SubitemsCollection subitemsCollection, List<ShopBundleEplus> bundlecode);
        void GetTravelOptionAncillaryDescription(Services.FlightShopping.Common.DisplayCart.SubitemsCollection subitemsCollection, Mobile.Model.Shopping.TravelOption travelOption, Services.FlightShopping.Common.DisplayCart.DisplayCart displayCart);

        List<MOBSHOPTax> AddFeesAfterPriceChange(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices);
        bool EnableChaseOfferRTI(int appID, string appVersion);
        void FormatChaseCreditStatemnet(MOBCCAdStatement chaseCreditStatement);
        Task<TPIInfoInBookingPath> GetBookingPathTPIInfo(string sessionId, string languageCode, MOBApplication application, string deviceId, string cartId, string token, bool isRequote, bool isRegisterTraveler, bool isReshop);
        List<MOBSHOPPrice> UpdatePriceForUnRegisterTPI(List<MOBSHOPPrice> persistedPrices);
        List<Mobile.Model.Shopping.TravelOption> GetTravelOptions(DisplayCart displayCart, bool isReshop, int appID, string appVersion);

        bool IsEnableFeature(string feature, int appId, string appVersion);

        bool IsExtraSeatFeatureEnabled(int appId, string appVersion, List<MOBItem> catalogItems);

        bool IsExtraSeatEligibleForInfantOnLapAndInfantOnSeat(string travelerTypeCode, bool isExtraSeat);

        Task ExtraSeatHandling(MOBCPTraveler traveler, MOBRequest mobRequest, Reservation bookingPathReservation, Session session, string mpNumber = "", bool isUpdateTraveler = false);

        bool IsExtraSeatExcluded(Session session, List<MOBSHOPTrip> trips, List<DisplayTravelType> displayTravelTypes, MOBCPProfileResponse response = null, bool? isUnfinishedBooking = false);

        void ExtraSeatHandlingForProfile(MOBRequest request, MOBCPProfileResponse response, Session session);

        string SpecialServiceRequestCode(string ssrCode);

        Task SetNextViewNameForEliteCustomer(United.Mobile.Model.Common.ProfileResponse profilePersist, United.Mobile.Model.Shopping.Reservation bookingPathReservation);

        string GetTravelerDisplayNameForExtraSeat(string firstName, string middleName, string lastName, string suffix);

        string RemoveExtraSeatCodeFromGivenName(string name);

        string GivenNameForExtraSeat(string firstName, string middleName, string lastName, string suffix, bool isExtraSeatEnabled);

        List<string> GetTravelerNameIndexForExtraSeat(bool isExtraSeatEnabled, Collection<Service.Presentation.CommonModel.Service> services);

        string GetExtraSeatReason(string travelerNameIndex, bool isExtraSeatEnabled, Collection<Service.Presentation.CommonModel.Service> services);
        
        string GetExtraSeatReasonDescription(string ssrCode);
        Task DuplicateTravelerCheck(string sessionId, MOBCPTraveler request, bool isExtraSeatFeatureEnabled);
        corpMultiPaxInfo corpMultiPaxInfo(bool isCorpBooking, bool isArrangerBooking, bool isMultipaxAllowed, List<RewardProgram> rewardPrograms);
    }
}
