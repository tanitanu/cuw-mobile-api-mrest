using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using United.Mobile.Model;
using United.Mobile.Model.Catalog;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.Shopping;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Bundles;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Model.Shopping.Misc;
using United.Mobile.Model.Shopping.UnfinishedBooking;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.FlightReservation;
using MOBBKTraveler = United.Mobile.Model.Shopping.Booking.MOBBKTraveler;
using MOBSHOPReservation = United.Mobile.Model.Shopping.MOBSHOPReservation;
using MOBSHOPTax = United.Mobile.Model.Shopping.MOBSHOPTax;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using Trip = United.Services.FlightShopping.Common.Trip;
using United.Service.Presentation.CommonModel;
using Characteristic = United.Service.Presentation.CommonModel.Characteristic;
using ShopResponse = United.Services.FlightShopping.Common.ShopResponse;
using United.Mobile.Model.ManageRes;
using United.Definition;
using United.Mobile.Model.Common.SSR;
using United.Utility.Enum;
using Task = System.Threading.Tasks.Task;
using ErrorInfo = United.Services.FlightShopping.Common.ErrorInfo;

namespace United.Common.Helper.Shopping
{
    public interface IShoppingUtility
    {
        bool IsSeatMapSupportedOa(string operatingCarrier, string MarketingCarrier);
        bool EnablePreferredZone(int appId, string appVersion);
        bool IsIBE(Reservation persistedReservation);
        bool IsUPPSeatMapSupportedVersion(int appId, string appVersion);
        bool OaSeatMapExceptionVersion(int applicationId, string appVersion);
        bool IsEMinusSeat(string programCode);
        bool EnableUnfinishedBookings(BookingBundlesRequest request);
        bool OaSeatMapSupportedVersion(int applicationId, string appVersion, string carrierCode, string MarketingCarrier = "");
        bool EnableAirCanada(int appId, string appVersion);
        bool EnableTravelerTypes(int appId, string appVersion, bool reshop = false);
        bool ShopTimeOutCheckforAppVersion(int appID, string appVersion);

        bool IsPosRedirectInShopEnabled(MOBSHOPShopRequest request);
        Task<Session> ValidateFSRRedesign(MOBSHOPShopRequest request, Session session);
        Task<bool> ValidateHashPinAndGetAuthToken(string accountNumber, string hashPinCode, int applicationId, string deviceId, string appVersion);
        Task<(bool returnValue, string validAuthToken)> ValidateHashPinAndGetAuthToken(string accountNumber, string hashPinCode, int applicationId, string deviceId, string appVersion, string validAuthToken);
        //Session CreateShoppingSession(int applicationId, string deviceId, string appVersion, string transactionId, string mileagPlusNumber, string employeeId, bool isBEFareDisplayAtFSR = false, bool isReshop = false, bool isAward = false, string travelType = "");
        System.Threading.Tasks.Task ValidateAndGetSignSignOn(MOBSHOPShopRequest request, Mobile.Model.Shopping.ShopResponse response);
        string BuildDotComRedirectUrl(MOBSHOPShopRequest request);
        bool EnableRoundTripPricing(int appId, string appVersion);

        bool EnableAirportDecodeToCityAllAirports(bool isReshop = false);
        bool EnableYoungAdult(bool isReshop = false);
        Task<bool> EnableFSRETCTravelCreditsFeature();
        bool EnableSSA(int id, string major);
        bool EnableMileageBalance(int id, string major);
        bool EnableFSRAlertMessages(int id, string major, string travelType);
        bool CheckFSRRedesignFromShopRequest(MOBSHOPShopRequest shopRequest);
        bool CheckFSRRedesignFromShop(MOBSHOPShopRequest request);
        bool CheckSkipElfProperties(MOBSHOPShopRequest request);
        bool IsETCchangesEnabled(int id, string major);
        bool EnableFareDisclouserCopyMessage(bool isReshopChange);
        string GetFeeWaiverMessage();
        MOBSearchFilters SetSearchFiltersOutDefaults(MOBSearchFilters filter);
        bool EnableIBEFull();
        bool EnableIBELite();
        bool EnableFSRLabelTexts(int id, string major, bool v);
        bool EnableCovidTestFlightShopping(int id, string major, bool v);
        void CheckTripsDepartDateAndAssignPreviousTripDateIfItLesser(Services.FlightShopping.Common.ShopRequest request);
        void GetAirportCityName(string airportCode, ref string airportName, ref string cityName);
        string GetCurrencySymbol(CultureInfo ci);
        bool EnableOptoutScreenHyperlinkSupportedContent(int appID, string version);
        bool IsIBeLiteFare(Product displayProduct);
        bool FeatureVersionCheck(int appID, string appVersion, string v1, string v2, string v3);
        Task<bool> EnableBookingWheelchairDisclaimer(int applicationId, string appVersion);
        Task<List<MOBDisplayBagTrackAirportDetails>> GetAirportNamesList(string codes);
        Task<string> GetAirportName(string airportCode);
        string formatAllCabinAwardAmountForDisplay(string amt, string cabinType, bool truncate = true, string price = null);
        string GetConfigEntriesWithDefault(string configKey, string defaultReturnValue);
        bool IsEnableOmniCartMVP2Changes(int applicationId, string appVersion, bool isDisplayCart);
        List<InfoWarningMessages> TripShareFeeWaiverMessage(List<InfoWarningMessages> infoWarningMessages);
        bool IsFeewaiverEnabled(bool isReshop);
        List<InfoWarningMessages> UpdateFeeWaiverMessage(List<InfoWarningMessages> infoWarningMessages);
        bool EnableAwardFareRules(int appId, string appVersion);
        bool IsForceSeatMapforEPlus(bool isReShop, bool isElf, bool is24HoursWindow, int appid, string appversion);
        bool IsEnabledNationalityAndResidence(bool isReShop, int appid, string appversion);
        bool EnableNationalityResidence(int appId, string appVersion);
        bool EnableSpecialNeeds(int appId, string appVersion);
        bool EnableInflightContactlessPayment(int appID, string appVersion, bool isReshop = false);
        bool AllowElfMetaSearchUpsell(int appId, string version);
        bool EnableUnfinishedBookings(MOBRequest request);
        MOBSHOPUnfinishedBookingTrip MapToMOBSHOPUnfinishedBookingTrip(United.Mobile.Model.ShopTrips.Trip csTrip);
        MOBSHOPUnfinishedBookingFlight MapToMOBSHOPUnfinishedBookingFlight(Mobile.Model.ShopTrips.Flight cslFlight);
        public bool EnableSavedTripShowChannelTypes(int appId, string appVersion);
        List<MOBTypeOption> GetFopOptions(int applicationID, string appVersion);
        List<MOBTypeOption> GetAppsFOPOptions(string appVersion, string[] fopTypesByLatestVersion);
        MOBTypeOption GetAvailableFopOptions(string fopType);
        bool IsDisplayCart(Session session, string travelTypeConfigKey = "DisplayCartTravelTypes");
        string GetCSSPublicKeyPersistSessionStaticGUID(int applicationId);
        List<List<MOBSHOPTax>> GetTaxAndFeesAfterPriceChange(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices, bool isReshopChange = false, int appId = 0, string appVersion = "", string travelType = null);
        bool EnableYADesc(bool isReshop = false);
        bool IsEnableTaxForAgeDiversification(bool isReShop, int appid, string appversion);
        bool EnableTaxForAgeDiversification(int appId, string appVersion);
        InfoWarningMessages BuildUpgradeFromELFInfoMessage(int ID);
        System.Threading.Tasks.Task SetELFUpgradeMsg(MOBSHOPAvailability availability, string productCode, MOBRequest request, Session session);
        InfoWarningMessages GetBEMessage();
        InfoWarningMessages GetBoeingDisclaimer();
        bool IsBoeingDisclaimer(List<DisplayTrip> trips);
        bool IsMaxBoeing(string boeingType);
        bool IsConBoeingDisclaimer(Flight flight);
        bool EnableBoeingDisclaimer(bool isReshop = false);
        InfoWarningMessages GetInhibitMessage(string bookingCutOffMinutes);
        bool IsIBEFullFare(DisplayCart displayCart);
        bool IsIBEFullFare(string productCode);
        bool IsIBELiteFare(DisplayCart displayCart);
        bool IsIBELiteFare(string productCode);
        bool EnablePBE();
        List<Mobile.Model.Shopping.MOBSHOPPrice> GetPrices(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice>
            prices, bool isAwardBooking, string sessionId, bool isReshopChange = false, string searchType = null,
            bool isFareLockViewRes = false, bool isCorporateFare = false, DisplayCart displayCart = null,
            int appId = 0, string appVersion = "", List<MOBItem> catalogItems = null, bool isNotSelectTripCall = false,
            FlightReservationResponse shopBookingDetailsResponse = null
             , List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> displayFees = null, bool isRegisterOffersFlow = false, Session session = null);
        System.Threading.Tasks.Task ValidateAwardMileageBalance(string sessionId, decimal milesNeeded);

        Task<MOBSHOPReservation> GetReservationFromPersist(MOBSHOPReservation reservation, string sessionID);
        bool IsBuyMilesFeatureEnabled(int appId, string version, List<MOBItem> catalogItems = null, bool isNotSelectTripCall = false);


        #region

        TripShare IsShareTripValid(SelectTripResponse selectTripResponse);
        bool EnableReshopCubaTravelReasonVersion(int appId, string appVersion);
        Task<MOBShoppingCart> ReservationToShoppingCart_DataMigration(MOBSHOPReservation reservation, MOBShoppingCart shoppingCart, MOBRequest request, Session session = null);
        //List<FormofPaymentOption> GetEligibleFormofPayments(MOBRequest request, Session session, MOBShoppingCart shoppingCart, string cartId, string flow, ref bool isDefault, MOBSHOPReservation reservation = null, List<LogEntry> logEntries = null, bool IsMilesFOPEnabled = false, SeatChangeState persistedState = null);
        bool IsETCCombinabilityEnabled(int applicationId, string appVersion);
        string GetGMTTime(string localTime, string airportCode);
        bool IncludeMOBILE12570ResidualFix(int appId, string appVersion);
        bool IsManageResETCEnabled(int applicationId, string appVersion);

        WorkFlowType GetWorkFlowType(string flow, string productCode = "");
        bool IsMilesFOPEnabled();
        Collection<FOPProduct> GetProductsForEligibleFopRequest(MOBShoppingCart shoppingCart, SeatChangeState state = null);
        bool IncludeMoneyPlusMiles(int appId, string appVersion);
        bool HasEligibleProductsForUplift(string totalPrice, List<ProdDetail> products);
        bool IncludeFFCResidual(int appId, string appVersion);
        System.Threading.Tasks.Task LoadandAddTravelCertificate(MOBShoppingCart shoppingCart, MOBSHOPReservation reservation, bool isETCCertificatesExistInShoppingCartPersist, MOBApplication application);
        System.Threading.Tasks.Task AssignBalanceAttentionInfoWarningMessage(ReservationInfo2 shopReservationInfo2, MOBFOPTravelCertificate travelCertificate);
        System.Threading.Tasks.Task LoadandAddTravelCertificate(MOBShoppingCart shoppingCart, string sessionId, List<Mobile.Model.Shopping.MOBSHOPPrice> prices, bool isETCCertificatesExistInShoppingCartPersist, Mobile.Model.MOBApplication application);
        InfoWarningMessages GetIBELiteNonCombinableMessage();
        bool EnableReshopMixedPTC(int appId, string appVersion);
        bool IncludeReshopFFCResidual(int appId, string appVersion);
        bool IsCorporateLeisureFareSelected(List<MOBSHOPTrip> trips);
        List<FormofPaymentOption> BuildEligibleFormofPaymentsResponse(List<FormofPaymentOption> response, MOBShoppingCart shoppingCart, Session session, MOBRequest request, bool isMetaSearch = false);
        List<FormofPaymentOption> BuildEligibleFormofPaymentsResponse(List<FormofPaymentOption> response, MOBShoppingCart shoppingCart, MOBRequest request);
        #endregion



        //added-Kriti
        void GetFlattedFlightsForCOGorThruFlights(Trip trip);
        string BuilTripShareEmailBodyTripText(string tripType, List<MOBSHOPTrip> trips, bool isHtml);
        string BuildTripSharePrice(string priceWithCurrency, string currencyCode, string redirectUrl);
        string BuildTripShareSegmentText(MOBSHOPTrip trip);
        Task<List<ProdDetail>> ConfirmationPageProductInfo(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, bool isPost, MOBApplication application, SeatChangeState state = null, string flow = "VIEWRES", string sessionId = "");
        double GetGrandTotalPriceForShoppingCart(bool isCompleteFarelockPurchase, Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, bool isPost, string flow = "VIEWRES");

        ProdDetail BuildProdDetailsForSeats(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, SeatChangeState state, string flow, MOBApplication application);
        ProdDetail BuildProdDetailsForSeats(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, SeatChangeState state, bool isPost, string flow);
        Task<List<ProdDetail>> BuildProductDetailsForInflightMeals(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, string productCode, string sessionId, bool isPost);
        string BuildSegmentInfo(string productCode, Collection<ReservationFlightSegment> flightSegments, IGrouping<string, SubItem> x);
        bool IsOriginalPriceExists(ProdDetail prodDetail);
        void AddCouponDetails(List<ProdDetail> prodDetails, Services.FlightShopping.Common.FlightReservation.FlightReservationResponse cslFlightReservationResponse, bool isPost, string flow, MOBApplication application);
        void AddPromoDetailsInSegments(ProdDetail prodDetail);
        string GetFormattedCabinName(string cabinName);
        List<ProductSegmentDetail> GetProductSegmentForInFlightMeals(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse,
  List<MOBInFlightMealsRefreshmentsResponse> savedResponse, Collection<Services.FlightShopping.Common.DisplayCart.TravelOption> travelOptions, Service.Presentation.InteractionModel.ShoppingCart flightReservationResponseShoppingCart);
        bool CheckSeatAssignMessage(string seatAssignMessage, bool isPost);
        List<ProductSegmentDetail> BuildProductSegmentsForSeats(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, List<Seat> seats, List<MOBBKTraveler> BookingTravelerInfo, bool isPost);

        Task<List<string>> GetProductDetailDescrption(IGrouping<String, SubItem> subItem, string productCode, String sessionId, bool isBundleProduct);
        string GetSeatTypeBasedonCode(string seatCode, int travelerCount);
        List<ProductSegmentDetail> BuildCheckinSegmentDetail(IEnumerable<IGrouping<string, SeatAssignment>> seatAssignmentGroup);
        List<ProductSubSegmentDetail> BuildSubsegmentDetails(List<SeatAssignment> seatAssignments);
        ProductSubSegmentDetail PopulateSubsegmentDetails(string seatType, decimal originalPrice, decimal seatPrice, int count);
        string GetSeatTypeBasedonCode(string seatCode, int travelerCount, bool isCheckinPath = false);
        Task<MOBShoppingCart> InitialiseShoppingCartAndDevfaultValuesForETC(MOBShoppingCart shoppingcart, List<ProdDetail> products, string flow);
        #region SeatMap
        bool VersionCheck_NullSession_AfterAppUpgradation(MOBRequest request);
        bool EnableUMNRInformation(int appId, string appVersion);
        bool EnableNewChangeSeatCheckinWindowMsg(int appId, string appVersion);
        bool IsEnableXmlToCslSeatMapMigration(int appId, string appVersion);
        bool EnableLufthansaForHigherVersion(string operatingCarrierCode, int applicationId, string appVersion);
        bool EnableLufthansa(string operatingCarrierCode);
        string BuildInterlineRedirectLink(MOBRequest mobRequest, string recordLocator, string lastname, string pointOfSale, string operatingCarrierCode);
        bool EnablePcuDeepLinkInSeatMap(int appId, string appVersion);
        bool IsTokenMiddleOfFlowDPDeployment();
        string ModifyVIPMiddleOfFlowDPDeployment(string token, string url);

        #endregion
        bool CheckEPlusSeatCode(string program);
        string SpecialcharacterFilterInPNRLastname(string stringTofilter);
        bool EnableActiveFutureFlightCreditPNR(int appId, string appVersion);
        string GetCurrencyCode(string code);
        bool EnableFareLockPurchaseViewRes(int appId, string appVersion);
        void GetCheckInEligibilityStatusFromCSLPnrReservation(System.Collections.ObjectModel.Collection<United.Service.Presentation.CommonEnumModel.CheckinStatus> checkinEligibilityList, ref MOBPNR pnr);
        bool IsELFFare(string productCode);
        string[] SplitConcatenatedConfigValue(string configkey, string splitchar);
        bool IncludeTRCAdvisory(MOBPNR pnr, int appId, string appVersion);
        MOBPNRAdvisory PopulateTRCAdvisoryContent(string displaycontent);
        bool EnablePetInformation(int appId, string appVersion);
        bool CheckMax737WaiverFlight
            (United.Service.Presentation.ReservationResponseModel.PNRChangeEligibilityResponse changeEligibilityResponse);
        void OneTimeSCChangeCancelAlert(MOBPNR pnr);

        string GetCurrencyAmount(double value = 0, string code = "USD", int decimalPlace = 2, string languageCode = "");
        bool CheckIfTicketedByUA(ReservationDetail response);
        bool EnableConsolidatedAdvisoryMessage(int appId, string appVersion);
        void AssignCertificateTravelers(MOBShoppingCart shoppingCart, FOPTravelerCertificateResponse persistedTravelCertifcateResponse, List<MOBSHOPPrice> prices, MOBApplication application);
        void UpdateCertificateAmountInTotalPrices(List<MOBSHOPPrice> prices, List<ProdDetail> scProducts, double certificateTotalAmount, bool isShoppingCartProductsGotRefresh = false);
        void AssignIsOtherFOPRequired(MOBFormofPaymentDetails formofPaymentDetails, List<MOBSHOPPrice> prices, bool IsSecondaryFOP = false, bool isRemoveAll = false);
        void UpdateSavedCertificate(MOBShoppingCart shoppingcart);
        bool IsETCEnabledforMultiTraveler(int applicationId, string appVersion);
        bool IsEligibileForUplift(MOBSHOPReservation reservation, MOBShoppingCart shoppingCart);
        bool EnableRtiMandateContentsToDisplayByMarket(int appID, string appVersion, bool isReshop);
        double GetAlowedETCAmount(List<ProdDetail> products, string flow);
        MOBSHOPPrice UpdateCertificatePrice(MOBSHOPPrice certificatePrice, double totalAmount);
        bool IsPOMOffer(string productCode);
        string BuildProductDescription(Collection<Services.FlightShopping.Common.DisplayCart.TravelOption> travelOptions, IGrouping<string, SubItem> t, string productCode);
        void UpdateCertificateAmountInTotalPrices(List<MOBSHOPPrice> prices, double certificateTotalAmount);
        string CreateLufthansaDeeplink(string recordLocator, string lastName, string countryCode, string languageCode);
        TripPriceBreakDown GetPriceBreakDown(Reservation reservation);
        Task<MOBSHOPReservation> MakeReservationFromPersistReservation(MOBSHOPReservation reservation, Reservation bookingPathReservation,
           Session session);
        bool DisableFSRAlertMessageTripPlan(int appId, string appVersion, string travelType);
        Task<List<MOBOptimizelyQMData>> ValidateAwardFSR(MOBSHOPShopRequest request);
        bool EnableAdvanceSearchCouponBooking(MOBSHOPShopRequest request);
        Task<bool> Authorize(string sessionId, int applicationId, string applicationVersion, string deviceId, string mileagePlusNumber, string hash);
        bool IsAwardFSRRedesignEnabled(int appId, string appVersion);
        bool IsSortDisclaimerForNewFSR(int appId, string appVersion);
        bool EnableReShopAirfareCreditDisplay(int appId, string appVersion);
        Task<List<MOBMobileCMSContentMessages>> GetProductBasedTermAndConditions(string sessionId, FlightReservationResponse flightReservationResponse, bool isPost);
        string GetBookingPaymentTargetForRegisterFop(FlightReservationResponse flightReservationResponse);
        string GetPaymentTargetForRegisterFop(TravelOptionsCollection travelOptions, bool isCompleteFarelockPurchase = false);
        InfoWarningMessages GetPriceMismatchMessage();
        List<MOBSHOPPrice> UpdatePricesForEFS(MOBSHOPReservation reservation, int appID, string appVersion, bool isReshop);
        bool IsMixedCabinFilerEnabled(int id, string version);
        bool EnableEPlusAncillary(int appID, string appVersion, bool isReshop = false);
        Task<MOBShoppingCart> PopulateShoppingCart(MOBShoppingCart shoppingCart, string flow, string sessionId, string CartId, MOBRequest request = null, Mobile.Model.Shopping.MOBSHOPReservation reservation = null, bool isAddTravelerFlow = false);
        bool IsAllFareLockOptionEnabled(int id, string version, List<MOBItem> catalgItems = null);
        bool? GetBooleanFromCharacteristics(Collection<Characteristic> characteristic, string key);
        string GetCharactersticValue(Collection<Characteristic> characteristics, string code);
        string GetSDLStringMessageFromList(List<CMSContentMessage> list, string title);
        bool IsCheckedIn(ReservationFlightSegment cslSegment);
        bool IsCheckInEligible(ReservationFlightSegment cslSegment);
        bool IsAllPaxCheckedIn(ReservationFlightSegment cslSegment);
        bool IsEnableMostPopularBundle(int appId, string version);
        bool EnableAdvanceSearchCouponBooking(int appId, string appVersion);

        bool IsAllPaxCheckedIn(ReservationDetail reservation, ReservationFlightSegment cslSegment, bool isCheckedIn);
        bool IsNonRefundableNonChangable(string productCode);
        Task<United.Mobile.Model.Shopping.InfoWarningMessages> GetNonRefundableNonChangableInversionMessage(MOBRequest request, Session session);
        bool IsNonRefundableNonChangable(DisplayCart displayCart);
        bool IsFSRNearByAirportAlertEnabled(int id, string version);
        List<MOBMobileCMSContentMessages> GetSDLMessageFromList(List<CMSContentMessage> list, string title);
        string BuildStrikeThroughDescription();
        bool IsEnableBulkheadNoUnderSeatStorage(int appId, string version);
        bool EnableEditForAllCabinPOM(int appId, string appVersion, List<MOBItem> catalog);
        bool HasNearByAirport(ShopResponse _cslShopResponse);
        bool CheckClientCatalogForEnablingFeature(string catalogFeature, List<MOBItem> clientCatalog);
        bool EnableEditForAllCabinPOM(int appId, string appVersion);
        Task<string> ValidateAndGetSingleSignOnWebShareToken(MOBRequest request, string mileagePlusAccountNumber, string passwordHash, string sessionId);
        bool IsInterLine(string operatingCarrier, string MarketingCarrier);
        bool IsOperatedBySupportedAirlines(string operatingCarrier, string MarketingCarrier);
        bool IsOperatedByUA(string operatingCarrier, string MarketingCarrier);
        bool IsLandTransport(string equipmentType);
        bool EnableOAMessageUpdate(int applicationId, string appVersion);
        bool EnableOAMsgUpdateFixViewRes(int applicationId, string appVersion);
        bool IsOAReadOnlySeatMap(string operatingCarrier);
        bool IsOperatedByOtherAirlines(string operatingCarrier, string MarketingCarrier, string equipmentType);
        bool EnableShoppingcartPhase2ChangesWithVersionCheck(int appId, string appVersion);
        string BuildPaxTypeDescription(string paxTypeCode, string paxDescription, int paxCount);

        bool IsServiceAnimalEnhancementEnabled(int id, string version, List<MOBItem> catalogItems);
        bool EnableBagCalcSelfRedirect(int id, string version);
        void UpdateShoppinCartWithCouponDetails(MOBShoppingCart persistShoppingCart);
        void IsHidePromoOption(MOBShoppingCart shoppingCart);

        List<ProductSegmentDetail> BuildProductSegmentsForSeats(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, List<Seat> seats, MOBApplication application, string flow);

        bool IsEnableEditSearchOnFSRHeaderBooking(int applicationId, string appVersion, List<MOBItem> catalogItems = null);
        bool EnableEditSearchOnFSRHeaderBooking(MOBSHOPShopRequest request, bool isEnableMultiCityEditSearchOnFSRBooking);
        bool EnableEditSearchOnFSRHeaderBooking(SelectTripRequest request, Session session);
        bool IsEnableU4BCorporateBooking(int applicationId, string appVersion, List<MOBItem> catalogItems = null);
        bool IsEnableU4BCorporateBooking(int applicationId, string appVersion);

        bool EnableAwardNonStop(int applicationId, string appVersion, bool isReshopChange, bool isAward = false);
        bool EnableAwardNonStop(int applicationId, string appVersion);

        bool GetFlightsByCountryCode(List<MOBSHOPTrip> trips, string countryCode);

        bool IsCanadaTravelNumberEnabled(int appId, string version);

        void SetCanadaTravelNumberDetails(MOBSHOPAvailability availability, MOBRequest request, Session session);

        void SetCarbonEmissionDetailsForConnections(MOBCarbonEmissionsResponse carbonEmissionData, MOBSHOPFlight flight);

        Task<MOBCarbonEmissionsResponse> LoadCarbonEmissionsDataFromPersist(Session session);

        bool IsNoFlightsSeasonalityFeatureEnabled(int id, string version, List<MOBItem> catalogItems);
        bool IsEnableCarbonEmissionsFeature(int id, string version, List<MOBItem> catalogItems);

        bool IsEnableFareLockAmoutDisplayPerPerson(int id, string version, List<MOBItem> catalogItems);

        Task<string> GetGMTTimeFromOffset(string strDate, double offsetInHours);

        bool IsReverseSearchByMapOrderForNoFlightsFound(int id, string version, List<MOBItem> catalogItems);

        bool IsEnableSelectTripResponseRequestFromBackend(int id, string version, List<MOBItem> catalogItems);

        bool IsEnableFarelockforOmniCart(int id, string version, bool TravelersRegistered, bool isOmniCartHomeScreenChanges);

        void AddMobLegalDocumentItem(List<MOBLegalDocument> docs, List<MOBItem> messages, string key);

        bool IsEnableGetFSRDInfoFromCSL(int appId, string appVersion);

        bool IsBookingSeatMapNotSupportedOtherAirlines(string operatingCarrier);

        Task<bool> IsEnableU4BTravelAddONPolicy(bool isCorporate, int applicationId, string appVersion, List<MOBItem> catalogItems = null);
        Task<bool> IsEnableU4BTravelAddONPolicy(int applicationId, string appVersion);
        Task<Mobile.Model.Shopping.TravelPolicy> GetTravelPolicy(United.CorporateDirect.Models.CustomerProfile.CorpPolicyResponse response, Session session, MOBRequest request, string corporateCompanyName, bool isCorporateNamePersonalized);
        Task<List<CMSContentMessage>> GetCorporateSDLMessages(Session session, MOBRequest request);
        Task<List<InfoWarningMessages>> BuildTravelPolicyAlert(United.CorporateDirect.Models.CustomerProfile.CorpPolicyResponse travelPolicies, MOBRequest request, FlightReservationResponse response, Session session, bool isCorporateNamePersonalized);
        Task<CorporateDirect.Models.CustomerProfile.CorpPolicyResponse> GetCorporateTravelPolicyResponse(string deviceId, string mileagePlusNumber, string sessionId);
        bool HasPolicyWarningMessage(List<Services.FlightShopping.Common.ErrorInfo> response);
        string GetCabinNameFromCorpTravelPolicy(CorporateDirect.Models.CustomerProfile.CorporateTravelCabinRestriction travelCabinRestrictions);
        bool IsEbulkPNRReshopEnabled(int id, string version, ReservationDetail cslReservation=null);
        Task<bool> IsDynamicLegendsEnabled(int id, string appVersion);
        Task<List<MOBDimensions>> GetFlightDimensionsList(string transId);
        double ConvertToInches(double dimensionInCm);
        FlightCombinationType GetFlightCombinationType(List<MOBSHOPTrip> trips);
        void BuildWheelChairSizerOAMsgs(MOBSHOPReservation reservation);
        Task<bool> IsEnableWheelChairSizerChanges(int applicationId, string appVersion, List<MOBItem> catalogItems = null);
        string GetFormatedUrl(string url, string scheme, string relativePath, bool ensureSSL = false);
        Task<bool> IsEnableU4BCorporateTravelSessionFix();
        bool AreAllFlightsBusinessOrFirstCabin(List<MOBSHOPTrip> trips);
        bool IsExtraSeatFeatureEnabled(int appId, string appVersion, List<MOBItem> catalogItems);
        bool IsExtraSeatExcluded(Session session, List<MOBSHOPTrip> trips, List<DisplayTravelType> displayTravelTypes, MOBShoppingCart response = null, bool? isUnfinishedBooking = false);
        Task<bool> IsEnabledExpressCheckoutFlow(int applicationId, string appVersion, List<MOBItem> catalogItems = null);
        Task<bool> IsEnableGuatemalaTaxIdCollectionChanges(int applicationId, string appVersion, List<MOBItem> catalogItems = null);
        System.Threading.Tasks.Task GetTaxIdContentFromSDL(MOBTaxIdInformation taxIdInfo, MOBRequest request, Session session);
        bool IsGuatemalaOriginatingTrip(List<MOBSHOPTrip> trips);
        System.Threading.Tasks.Task BuildTaxIdInformation(MOBSHOPReservation reservation, MOBRequest request, Session session);
        MOBOffer GetAFSOfferDetails(DisplayCart displayCart);
        Task<bool> IsEnableAdvanceSearchOfferCode(int applicationId, string appVersion, List<MOBItem> catalogItems = null);
        bool IsEnableMoneyPlusMilesFeature(int applicationId = 0, string appVersion = "", List<MOBItem> catalogItems = null);
        bool IsEnableMoneyPlusMilesOnScreenAlert(int applicationId = 0, string appVersion = "", List<MOBItem> catalogItems = null);
        bool IsEnableFSRETCTravelCreditsOnScreenAlert(int applicationId = 0, string appVersion = "", List<MOBItem> catalogItems = null);
        bool IsIncludeMoneyMilesInRTI(List<FormofPaymentOption> eligibleFormsOfPayment);
        bool IsEnablePartnerProvision(List<MOBItem> catalogItems, string flow, int applicationId, string appVersion);
        double UpdatePromoValueForFSRMoneyMiles(List<DisplayPrice> prices,Session session, double promoValue);
        Task<bool> IsMoneyPlusmilesEligible(ShopResponse shopResponse, MOBApplication application, List<MOBItem> catalogItems);
        Tuple<bool, string> IsNoDiscountedFaresAvailable(ShopResponse shopResponse);
        MOBFSRAlertMessage GetnoDiscoutedFareAlertMessage(United.Services.FlightShopping.Common.ShopResponse response, List<CMSContentMessage> lstMessages);
        List<MOBPaxDetails> GetPaxDetails(IGrouping<string, SeatAssignment> t, FlightReservationResponse response, string flow, MOBApplication application);
        string PaxName(United.Service.Presentation.ReservationModel.Traveler traveler, bool isExtraSeatEnabled, List<string> extraSeatPassengerIndex);
        string GetGivenNameForExtraSeat(string givenName);
        bool IsExtraSeatFeatureEnabled(int appId, string appVersion);
        Task<bool> IsLoadTCOrTB(MOBShoppingCart shoppingCart);
        List<MOBErrorInfo> AddWarnings(United.Services.FlightShopping.Common.ShopResponse response);
        Task<bool> IsNoDiscountFareAvailbleinShop1(string sessionId);
        Task<bool> IsEnableHideEditProfileNavigationForExtraSeatInOmniCart();
        Task<bool> IsExtraSeatFeatureEnabledForOmniCart(int appId, string appVersion, List<MOBItem> catalogItems);    
        bool IsTaxIdCountryEnable(List<MOBSHOPTrip> trips, List<CMSContentMessage> sdllstMessages = null);
        System.Threading.Tasks.Task BuildTaxIdInformationForLatidCountries(MOBSHOPReservation reservation, MOBRequest request, Session session, List<CMSContentMessage> sdllstMessages = null);
        Task<string> GetSeatMapErrorMessages(string key, string sessionId, int appId, string version);
        Task<bool> IsEnableExtraSeatReasonFixInOmniCartFlow();
        Task<bool> IsEnablePassingTourCodeToFSInCorporateFlow();
        Task<bool> EnableGUIDAndUCSIDToFlightShoppingInCorporateFlow();
        void GetCartRefId(long cartRefid, ReservationInfo2 shopReservationInfo2, List<CMSContentMessage> lstMessages);
        System.Threading.Tasks.Task UpdateFSRMoneyPlusMilesOptionsBasedOnEFOP(MOBRequest request, Session session, MOBShoppingCart shoppingCart, MOBSHOPReservation reservation, List<FormofPaymentOption> response);

        bool IsFSROAFlashSaleEnabled(List<MOBItem> catalogItems);
        bool IsFSROAFlashSaleEnabledInReShop(List<MOBItem> catalogItems);

        Task<bool> IsEnableSuppressingCompanyNameForBusiness(bool isCorporateNamePersonalized);

        Task<bool> IsEnableCorporateNameChange();

        Task<bool> IsEnablePassingPersonalizedFlagToClient();

        string GetCorporateDisclaimerText(MOBSHOPShopRequest shopRequest, bool isU4BCorporateBookingEnabled);

        MOBSHOPAvailability AddFSROAFalsSaleAlerts(MOBSHOPAvailability availability, List<CMSContentMessage> lstMessages,
           ErrorInfo errorInfo);
        MOBSHOPAvailability AddFSROAFalsSaleAlertsInReshop(MOBSHOPAvailability availability, List<CMSContentMessage> lstMessages, ErrorInfo errorInfo);
        void BuildWheelChairFilterContent(WheelChairSizerInfo WheelChairfilter, List<CMSContentMessage> lstMessages);
        void PrepopulateDimensionInfo(MOBSearchFilters searchFilters, ReservationInfo2 shopReservationInfo2, string sessionId);
        Task RemoveSavedDimensionInfo(MOBSearchFilters searchFilters, string sessionId);
        void AssignWheelChairSpecialNeedForSinglePax(List<MOBCPTraveler> travelers, Reservation bookingPathReservation);
        bool isWheelChairFilterAppliedAtFSR(ReservationInfo2 shopReservationInfo2);
        void UpdateAllEligibleTravelersCslWithWheelChairSpecialNeed(List<MOBCPTraveler> allEligibleTravelersCSL, MOBCPTraveler traveler);
        void AddWheelChairAlertMsgWhenNoneFits(MOBSHOPAvailability availability, List<CMSContentMessage> lstMessages);
        Task ValidateWheelChairFilterSize(MOBDimensions dimensions, string sessionId, MOBSHOPAvailability availability, List<CMSContentMessage> lstMessages);
        Task DisplayBuyMiles(MOBSHOPReservation reservation, FlightReservationResponse response, Session session,
           MOBAddTravelersRequest mobAddTravlerRequest, List<CMSContentMessage> contentMessages);
        string GetCorporateDisclaimerText(MOBSHOPShopRequest shopRequest, bool isU4BCorporateBookingEnabled, bool isEnableSuppressingCompanyNameForBusiness, bool isReshopChange);
        Task SetEligibilityforETCTravelCredit(MOBSHOPReservation reservation, Session session, Reservation bookingPathReservation);
        Task<List<MOBFSRAlertMessage>> SetFSRTravelTypeAlertMessage(Session session, List<CMSContentMessage> lstMessages = null);
    }
}
