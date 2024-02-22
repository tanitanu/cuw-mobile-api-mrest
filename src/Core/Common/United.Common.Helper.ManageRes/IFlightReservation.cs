using System.Collections.ObjectModel;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Fitbit;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.MPRewards;
using United.Service.Presentation.ReservationRequestModel;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using Reservation = United.Service.Presentation.ReservationModel.Reservation;


namespace United.Common.Helper.ManageRes
{
    public interface IFlightReservation
    {
        Task<(MOBPNR pnr, ReservationDetail response)> GetPNRByRecordLocatorFromCSL(string transactionId, string deviceId, string recordLocator, string lastName, string languageCode, int applicationId, string appVersion, bool forWallet, Session session, ReservationDetail response, bool isOTFConversion = false);
        Task<SeatOffer> GetSeatOffer_CFOP(MOBPNR pnr, MOBRequest request, Reservation cslReservation, string token, string flowType, string sessionId);
        Task<MOBIRROPSChange> ValidateIRROPSStatus(MOBPNRByRecordLocatorRequest request, MOBPNRByRecordLocatorResponse response,
           ReservationDetail cslReservationDetail, Session session);
        Task<MOBPNR> GetPNRByRecordLocator(string transactionId, string recordLocator, string lastName, string languageCode, int applicationId, string appVersion, bool forWallet);
        Task<MOBPNR> GetPNRByRecordLocatorFromCSL(string transactionId, string deviceId, string recordLocator, string lastName, string languageCode, int applicationId, string appVersion, bool forWallet, Session session = null);
        Task<MOBMPPNRs> GetPNRsByMileagePlusNumberCSL(int applicationId, string transactionId, string mileagePlusNumber, MOBReservationType reservationType, string languageCode, bool includeFarelockInfo, string appVersion, bool forWallet, bool getEmployeeIdFromCSLCustomerData);
        Task<MOBMPPNRs> GetPNRsByMileagePlusNumber(string transactionId, string mileagePlusNumber, MOBReservationType reservationType, string languageCode, bool includeFarelockInfo, string appVersion, bool forWallet, int applicationId, bool getEmployeeIdFromCSLCustomerData);
        MOBMPPNRs GetPNRsByMileagePlusNumberCSL(string transactionId, string mileagePlusNumber, MOBReservationType reservationType, string languageCode, bool includeFarelockInfo, string appVersion, bool forWallet, int applicationId, bool getEmployeeIdFromCSLCustomerData);

        string GetTravelType(Collection<ReservationFlightSegment> flightSegments);

        Task<string> RetrievePnrDetailsFromCsl(int applicationId, string TransactionId, RetrievePNRSummaryRequest request);
        string GetCharactersticValue(System.Collections.ObjectModel.Collection<Service.Presentation.CommonModel.Characteristic> Characteristic, string code);
        Task<MOBPNRSegment> GetPnrSegment(string languageCode, string appVersion, int applicationId, ReservationFlightSegment segment, int lowestEliteLevel);
        void GetPassengerDetails
         (MOBPNR pnr, ReservationDetail response, ref bool isSpaceAvailblePassRider, ref bool isPositiveSpace, int applicationId = 0, string appVersion = "");

        MOBPNRAdvisory PopulateConfigContent(string displaycontent, string splitchar);
        Task<MOBFutureFlightCredit> GetFutureFlightCreditMessages(int applicationId, string appVersion);
        //bool AddPNRRemark(MOBPNRRemarkRequest request, System.Collections.Generic.List<Logger> logEntries);

        Task<bool> AddPNRRemark(MOBPNRRemarkRequest request);
    }
}
