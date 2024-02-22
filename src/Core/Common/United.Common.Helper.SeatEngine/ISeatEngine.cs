using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Booking;
using Seat = United.Mobile.Model.Shopping.Misc.Seat;

namespace United.Common.HelperSeatEngine
{
    public interface ISeatEngine
    {
        List<string> GetPolarisCabinBranding(string authenticationToken, string flightNumber, string departureAirportCode, string flightDate, string arrivalAirportCode, string cabinCount, string languageCode, string sessionId, string operatingCarrier = "UA", string marketingCarrier = "UA");
        Task<List<MOBSeatMap>> GetSeatMapDetail(int applicationId, string transactionId, string recordLocator, FlightAvailabilitySegment segment, List<BookingTravelerInfo> booingTravelerInfo, string lastName, string destination, string origin, bool returnPolarisLegendforSeatMap = false);
        Task<List<MOBSeatMap>> GetSeatMapDetail(string sessionId, string destination, string origin, int applicationId, string appVersion, string deviceId, bool returnPolarisLegendforSeatMap);
        Task<MOBSeatChangeInitializeResponse> SeatChangeInitialize(MOBSeatChangeInitializeRequest request);
        Task<bool> IsEnablePCUSeatMapPurchaseManageRes(int appId, string appVersion, int numberOfTravelers);
        void EconomySeatsForBUSService(MOBSeatMap seats, bool operated = false);
        Task<string> ShowNoFreeSeatsAvailableMessage(int noOfTravelersWithoutSeat, int noOfFreeEplusEligibleRemaining, int noOfFreeSeats, int noOfPricedSeats, bool isBasicEconomy);
        bool IsPreferredSeatProgramCode(string program);
        bool ShowSeatMapForCarriers(string operatingCarrier);
        bool IsInChecKInWindow(string departTimeString);
        Task<(string jsonResponse, string token)> GetPnrDetailsFromCSL(string transactionId, string recordLocator, string lastName, int applicationId, string appVersion, string actionName, string token, bool usedRecall = false);
        Task<List<MOBSeatMap>> GetSeatMapForRecordLocatorWithLastNameCSL(string sessionId,
                  string recordLocator, int segmentIndex, string languageCode, string bookingCabin,
                  bool cogStop, string origin, string flightnumber, string MarketingCarrier, string OperatingCarrier,
                  string flightdate, string destination, string appVersion, bool isOaSeatMapSegment, bool isBasicEconomy,
                  int noOfTravelersWithNoSeat1, int noOfFreeEplusEligibleRemaining, List<TripSegment> tripSegments, string deviceId, int applicationId = -1, bool returnPolarisLegendforSeatMap = false);
        List<MOBSeatMap> GetSeatMapWithPreAssignedSeats(List<MOBSeatMap> seatMap, List<Seat> existingSeats, bool isPreferredZoneEnabled);
        string GetOperatedByText(string marketingCarrier, string flightNumber, string operatingCarrierDescription);
        Task<Mobile.Model.MPRewards.SeatEngine> GetFlightReservationCSL_CFOP(MOBSeatChangeInitializeRequest request, Mobile.Model.MPRewards.SeatEngine seatEngine);
        int GetNoOfFreeEplusEligibleRemaining(List<MOBBKTraveler> travelers, string orgin, string destination, int totalEplusEligible, bool isElf, out int noOfTravelersWithNoSeat);
        void CheckSegmentToRaiseExceptionForElf(List<TripSegment> segments);
        Task<(int, MOBSeatChangeInitializeResponse response, int ePlusSubscriberCount)> PopulateEPlusSubscriberSeatMessage(MOBSeatChangeInitializeResponse response, int applicationID, string sessionID, int ePlusSubscriberCount, bool isEnablePreferredZoneSubscriptionMessages = false);
        int GetNoFreeSeatCompanionCount(List<MOBBKTraveler> travelers, List<MOBBKTrip> trips);
        Task<(int, MOBSeatChangeInitializeResponse response, int ePlusSubscriberCount, bool hasEliteAboveGold, bool doNotShowEPlusSubscriptionMessage, bool showEPUSubscriptionMessage)> PopulateEPlusSubscriberAndMPMemeberSeatMessage(MOBSeatChangeInitializeResponse response, int applicationID, string sessionID, int ePlusSubscriberCount, bool hasEliteAboveGold, bool doNotShowEPlusSubscriptionMessage, bool showEPUSubscriptionMessage);
        void PopulateEPAEPlusSeatMessage(ref MOBSeatChangeInitializeResponse response, int noFreeSeatCompanionCount, ref bool doNotShowEPlusSubscriptionMessage);
        bool IsMatchedFlight(TripSegment segment, OfferRequestData flightDetails, List<TripSegment> segments);
        bool HasEconomySegment(List<MOBBKTrip> trips);
        bool ValidateResponse(MOBSeatChangeInitializeResponse response);
        Task<string> GetPolarisSeatMapLegendId(string from, string to, int numberOfCabins, List<string> polarisCabinBrandingDescriptions, int applicationId = -1, string appVersion = "", bool isFamilySeatingIconEnabled = false);
        bool IsPreferredSeat(string DisplaySeatType, string program);
        bool IsMatchedFlight(TripSegment segment, MOBSeatFocus seatFocusSegment, List<TripSegment> segments);
        Task<string> ShowNoFreeSeatsAvailableMessage(Reservation persistedReservation, int noOfFreeSeats, int noOfPricedSeats, bool isInCheckInWindow, bool isFirstSegment);
    }
}
