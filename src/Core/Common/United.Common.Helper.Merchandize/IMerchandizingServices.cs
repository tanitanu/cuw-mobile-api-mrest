using MerchandizingServices;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Fitbit;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.MPSignIn;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Pcu;
using United.Service.Presentation.PersonalizationModel;
using United.Service.Presentation.PersonalizationResponseModel;
using United.Service.Presentation.PriceModel;
using United.Service.Presentation.ProductModel;
using United.Service.Presentation.ProductRequestModel;
using United.Service.Presentation.SegmentModel;
using MOBBKTrip = United.Mobile.Model.Shopping.Booking.MOBBKTrip;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Common.Helper.Merchandize
{
    public interface IMerchandizingServices
    {
        Task<MOBUASubscriptions> GetEPlusSubscriptions(string mpAccountNumber, int applicationID, string sessionID);
        void SetMerchandizeChannelValues(string merchChannel, ref string channelId, ref string channelName);
        Task<MOBSHOPInflightContactlessPaymentEligibility> IsEligibleInflightContactlessPayment(Reservation reservation, MOBRequest request, Session session);
        Task<MOBUASubscriptions> GetUASubscriptions(string mpAccountNumber, int applicationID, string sessionID, string channelId, string channelName);
        Task<(bool showEPAMsg, List<United.Mobile.Model.Shopping.Booking.MOBBKTrip> trips)> GetEPlusSubscriptionsForBookingSelectedTravelers(List<FlightSegmentType> flightSegmentTypeList, List<TravelerType> travelTypeList, List<MOBBKTrip> trips);
        Task<Service.Presentation.ProductResponseModel.ProductOffer> GetMerchOffersDetails(Session session, United.Service.Presentation.ReservationModel.Reservation cslReservation, MOBRequest mobRequest, string flow, MOBPNR pnrResponse);
        Task<DynamicOfferDetailResponse> GetMerchOffersDetailsFromCCE(Session session, United.Service.Presentation.ReservationModel.Reservation cslReservation, MOBRequest mobRequest, string flow, MOBPNR pnrResponse, string productCode = "");

        MOBTravelOptionsBundle TravelOptionsBundleOffer(DynamicOfferDetailResponse productOffers, MOBRequest request, string sessionId);

        MOBBasicEconomyBuyOut BasicEconomyBuyOutOffer(DynamicOfferDetailResponse productOffers, MOBRequest request, string sessionId, MOBPNR pnrResponse);
        Task<DOTBaggageInfoResponse> GetDOTBaggageInfoWithPNR(string accessCode, string transactionId, string languageCode, string messageFormat, int applicationId, string recordLocator, string ticketingDate, string channelId, string channelName, MOBSHOPReservation reservation = null, United.Service.Presentation.ReservationModel.Reservation cslReservation = null);
        string GetPnrCreatedDate(Service.Presentation.ReservationModel.Reservation cslReservation);
        Task<(Mobile.Model.MPRewards.MOBPriorityBoarding priorityBoarding, MOBPremierAccess premierAccess)> PBAndPADetailAssignment_CFOP(string sessionId, MOBApplication application, string recordLocator, string lastname, string pnrCreateDate, Mobile.Model.MPRewards.MOBPriorityBoarding priorityBoarding, MOBPremierAccess premierAccess, string jsonRequest, string jsonResponse);
        void PBAndPAAssignment(string transactionId, ref MOBAncillary ancillary, MOBApplication application, string deviceId, Mobile.Model.MPRewards.MOBPriorityBoarding priorityBoarding, string logAction, ref MOBPremierAccess premierAccess, ref bool showPremierAccess);
        Task<MOBBundlesMerchandizingResponse> GetBundleInfoWithPNR(MOBBundlesMerchangdizingRequest request, string channelId, string channelName);
        Task<MOBPremiumCabinUpgrade> GetPremiumCabinUpgrade_CFOP(Service.Presentation.ProductResponseModel.ProductOffer productOffers, MOBPNRByRecordLocatorRequest request, string sessionId, Service.Presentation.ReservationModel.Reservation cslReservation);
        Task<MOBAccelerators> GetMileageAndStatusOptions(Service.Presentation.ProductResponseModel.ProductOffer productOffers, MOBRequest request, string sessionId);
        Task<MOBTPIInfo> GetTPIINfoDetails_CFOP(bool isTPIIncluded, bool isFareLockOrNRSA, MOBPNRByRecordLocatorRequest request, Session session);
        string GetBundlesCommonDescription(string bundleCode);
        Collection<ProductTraveler> ProductTravelers(Collection<United.Service.Presentation.ReservationModel.Traveler> travelers);
        Collection<ProductFlightSegment> ProductFlightSegments(Collection<ReservationFlightSegment> flightSegments, Collection<Price> prices, string productCode = "");
        Collection<Solution> Solutions(Collection<ReservationFlightSegment> flightSegments);
        Collection<ODOption> ProductOriginDestinationOptionsForMircoSite(Collection<ReservationFlightSegment> flightSegments);
        Task<MOBPremiumCabinUpgrade> GetPremiumCabinUpgrade(string recordLocator, string sessionId, string cartId, MOBRequest mobRequest, United.Service.Presentation.ReservationModel.Reservation cslReservation);
    }
}
