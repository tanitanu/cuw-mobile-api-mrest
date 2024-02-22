﻿using System.Collections.Generic;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.SeatMap;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Booking;
using MOBSeatMap = United.Mobile.Model.Shopping.MOBSeatMap;
using MOBPromoCodeDetails = United.Mobile.Model.Shopping.FormofPayment.MOBPromoCodeDetails;
using System.Threading.Tasks;

namespace United.Common.HelperSeatEngine
{
    public interface ISeatMapCSL30
    {
        Task<(List<MOBSeatMap>, bool isEplusNA)> GetCSL30SeatMapDetail(string flow, string sessionId, string destination, string origin, int applicationId, string appVersion, string deviceId, bool returnPolarisLegendforSeatMap, Section promoCodeRemovalAlertMessage, MOBPromoCodeDetails promoCodeDetails, bool isVerticalSeatMapEnabled= false, int ePlusSubscriberCount =0);

        Task<List<MOBSeatMap>> GetCSL30SeatMapForRecordLocatorWithLastName(string sessionId, string recordLocator, int segmentIndex, string languageCode, string bookingCabin, string lastName, bool cogStop, string origin, string destination, int applicationId, string appVersion, bool isELF, bool isIBE, int noOfTravelersWithNoSeat1, int noOfFreeEplusEligibleRemaining, bool isOaSeatMapSegment, List<TripSegment> tripSegments, string operatingCarrierCode, string deviceId, List<MOBBKTraveler> BookingTravelerInfo, string flow, bool isOneofTheSegmentSeatMapShownForMultiTripPNRMRes = false, bool isSeatFocusEnabled = false, List<MOBItem> catalog = null);
        Task<(decimal lowestEplusPrice, decimal lowestEMinusPrice, string currencyCode)> GetSeatPriceForOfferTile(MOBPNR pnr, United.Service.Presentation.ReservationModel.Reservation cslReservation, string token, MOBRequest mobRequest, string sessionId);
        Task<MOBSeatMap> BuildSeatMapCSL30(United.Definition.SeatCSL30.SeatMap seatMapResponse, int numberOfTravelers, string bookingCabin, bool cogStop, string sessionId, bool isELF, bool isIBE, int noOfTravelersWithNoSeat, int noOfFreeEplusEligibleRemaining, bool isOaSeatMapSegment, int segmentIndex, string flow, string token = "", int appId = -1, string appVersion = "", bool isOneofTheSegmentSeatMapShownForMultiTripPNRMRes = false, string operatingCarrierCode = "", List<MOBItem> catalog = null);
        string GetSeatPositionAccessFromCSL30SeatMap(string seatType);
        string GetSeatValueFromCSL30SeatMap(Definition.SeatCSL30.Seat seat, bool disableEplus, bool disableSeats, MOBApplication application, bool isOaSeatMapSegment, bool isOaPremiumEconomyCabin, string pcuOfferAmountForthisCabin, bool cogStop);
        InterLineDeepLink GetInterlineRedirectLink(List<MOBBKTraveler> bookingTravelerInfo, string pointOfSale, MOBSeatChangeSelectRequest request, string recordLocator, string lastName, List<MOBItem> catalog, string operatingCarrier, string origin, string destination, string departDate);
        void GetInterlineRedirectLink(List<MOBBKTraveler> bookingTravelerInfo, List<TripSegment> segments, string pointOfSale, MOBRequest mobRequest, string recordLocator, string lastname, List<MOBItem> catalog);
        void GetInterlineRedirectLink(List<TripSegment> segments, string pointOfSale, MOBRequest mobRequest, string recordLocator, string lastname, List<MOBItem> catalog);
        bool EnableAdvanceSearchCouponBooking(int appId, string appVersion);
        void GetOANoSeatAvailableMessage(List<TripSegment> segments);
        InterLineDeepLink GetOANoSeatAvailableMessage(string origin, string destination, string departDate);
    }

}
