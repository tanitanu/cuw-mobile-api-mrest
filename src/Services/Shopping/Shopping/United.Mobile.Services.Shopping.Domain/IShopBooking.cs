using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Common;
using United.Mobile.Model.Shopping.Common.TripPlanner;
using United.Services.FlightShopping.Common;

namespace United.Mobile.Services.Shopping.Domain
{
    public interface IShopBooking
    {
        Task<MOBSHOPAvailability> GetAvailability(string token, MOBSHOPShopRequest shopRequest, bool isFirstShopCall,HttpContext httpContext);

        Task<MOBSHOPAvailability> GetAvailabilityTripPlan(string token, MOBTripPlanShopHelper shopRequest, MOBSHOPTripPlanRequest sHOPTripPlanRequest, HttpContext httpContext = null);

        Task<MOBSHOPAvailability> GetLastTripAvailabilityFromPersist(int v, string sessionId);
        MOBSHOPShopRequest ConvertCorporateLeisureToRevenueRequest(MOBSHOPShopRequest request);
        Task<MOBSHOPAvailability> FilterShopSearchResults(ShopOrganizeResultsReqeust organizeResultsRequest, Session session, bool isRemoveAppliedWheelChairFilter = false);
        Task<(bool, MOBPromoAlertMessage couponAlertMessages)> ShopValidate(string token, MOBSHOPShopRequest shopRequest, MOBPromoAlertMessage couponAlertMessages);
        Task AddAvailabilityToPersist(MOBSHOPAvailability availability, string sessionID, bool isCallFromShop = false);
        Task<List<MOBFSRAlertMessage>> HandleFlightShoppingThatHasNoResults(United.Services.FlightShopping.Common.ShopResponse cslResponse, MOBSHOPShopRequest restShopRequest, bool isShop);
        System.Threading.Tasks.Task AwardNoFlightExceptionMsg(MOBSHOPShopRequest shopRequest);
        Task<MOBFSRAlertMessage> GetCorporateLeisureOptOutFSRAlert(MOBSHOPShopRequest shoprequest, Session session);
        List<MOBFSRAlertMessage> AddMandatoryFSRAlertMessages(MOBSHOPShopRequest restShopRequest, List<MOBFSRAlertMessage> alertMessages);
        Task<List<MOBFSRAlertMessage>> HandleFlightShoppingThatHasResults(United.Services.FlightShopping.Common.ShopResponse cslResponse, MOBSHOPShopRequest restShopRequest, bool isShop);
        void GetMilesDescWithFareDiscloser(MOBSHOPAvailability availability, Session session, bool isMobileRedirect = false, List<string> experimentList = null, bool isNewAwardFSR = false);
        void AssignCalendarLengthOfStay(int lengthOfCalendar, United.Services.FlightShopping.Common.ShopRequest shopRequest);
        List<MOBSHOPFareWheelItem> PopulateFareWheelDates(List<United.Mobile.Model.Shopping.MOBSHOPTripBase> shopTrips, string currentCall);
        bool NoCSLExceptions(List<United.Services.FlightShopping.Common.ErrorInfo> errors);
        string GetPriceFromText(string searchType);
        string GetPriceFromText(MOBSHOPShopRequest shopRequest);
        string GetPriceTextDescription(string searchType);
        void SetFSRFareDescriptionForShop(MOBSHOPAvailability availability, MOBSHOPShopRequest request);
        void SetSortDisclaimerForReshop(MOBSHOPAvailability availability, MOBSHOPShopRequest request);
        bool IsStandardRevenueSearch(bool isCorporateBooking, bool isYoungAdultBooking, bool isAwardTravel,
                                                  string employeeDiscountId, string travelType, bool isReshop, string fareClass,
                                                  string promotionCode);
        Task<MOBSHOPTrip> PopulateTrip(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string cartId, List<United.Services.FlightShopping.Common.Trip> trips, int tripIndex, string requestedCabin, string sessionId, int appId, string deviceId, string appVersion, bool showMileageDetails, int premierStatusLevel, bool isStandardRevenueSearch, bool isAward = false, bool isELFFareDisplayAtFSR = true, bool getNonStopFlightsOnly = false, bool getFlightsWithStops = false, MOBSHOPShopRequest shopRequest = null, MOBAdditionalItems additionalItems = null, List<CMSContentMessage> lstMessages = null, HttpContext httpContext = null);
        Task<MOBSHOPTrip> PopulateTrip(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string cartId, List<United.Services.FlightShopping.Common.Trip> trips, int tripIndex, string requestedCabin, string sessionId, string tripKey, int appId, string deviceId, string appVersion, bool showMileageDetails, int premierStatusLevel, bool isStandardRevenueSearch, bool isAward = false, bool isELFFareDisplayAtFSR = true, bool getNonStopFlightsOnly = false, bool getFlightsWithStops = false, MOBSHOPShopRequest shopRequest = null, Session session = null, MOBAdditionalItems additionalItems = null, List<CMSContentMessage> lstMessages = null, HttpContext httpContext= null);
        SHOPAwardCalendar PopulateAwardCalendar(United.Services.FlightShopping.Common.CalendarType calendar, string tripId, string productId);
        UpdateAmenitiesIndicatorsRequest GetAmenitiesRequest(string cartId, List<Flight> flights);
        Task<bool> IsLastTripFSR(bool isReshopChange, MOBSHOPAvailability availability, List<United.Services.FlightShopping.Common.Trip> trips);
        void SetTitleForFSRPage(MOBSHOPAvailability availability, MOBSHOPShopRequest shopRequest);
        bool EnableAdvanceSearchCouponBooking(int appId, string appVersion);
        Task<string> GetFareClassAtShoppingRequestFromPersist(string sessionID);
        Task<(List<Model.Shopping.MOBSHOPFlight>, CultureInfo ci)> GetFlights(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string sessionId, string cartId, List<Flight> segments, string requestedCabin, CultureInfo ci, decimal lowestFare, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClass, bool updateAmenities = false, bool isConnection = false, bool isELFFareDisplayAtFSR = true, MOBSHOPTrip trip = null, string appVersion = "", int appID = 0, Session session = null, MOBAdditionalItems additionalItems = null, List<CMSContentMessage> lstMessages = null, MOBSearchFilters searchFilters = null);
        Task<List<MOBSHOPFlight>> GetFlightsAsync(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string sessionId, string cartId, List<Flight> segments, string requestedCabin, decimal lowestFare, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClass, bool updateAmenities = false, bool isConnection = false, bool isELFFareDisplayAtFSR = true, MOBSHOPTrip trip = null, string appVersion = "", int appID = 0, Session session = null, MOBAdditionalItems additionalItems = null, List<CMSContentMessage> lstMessages = null, MOBSearchFilters searchFilters = null);
        void PopulateFlightAmenities(Collection<AmenitiesProfile> amenityFlights, ref List<Flight> flights);
        Task<List<United.Services.FlightShopping.Common.LMX.LmxFlight>> GetLmxFlights(string token, string cartId, string hashList, string sessionId, int applicationId, string appVersion, string deviceId);
        Task<UpdateAmenitiesIndicatorsResponse> GetAmenitiesForFlights(string sessionId, string cartId, List<Flight> flights, int appId, string deviceId, string appVersion, bool isClientCall = false, UpdateAmenitiesIndicatorsRequest amenitiesPersistRequest = null);
        List<Model.Shopping.MOBSHOPShoppingProduct> PopulateColumns(ColumnInformation columnInfo);
        void SetCurrentSession(Session session);
        Task StrikeThroughContentMessages(MOBSHOPAvailability availability, MOBAdditionalItems additionalItems, Session session, MOBRequest mOBRequest);
    }
}