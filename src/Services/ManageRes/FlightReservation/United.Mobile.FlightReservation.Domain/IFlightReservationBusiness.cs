using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.FlightReservation;

namespace United.Mobile.FlightReservation.Domain
{
    public interface IFlightReservationBusiness
    {
        Task<MOBPNRByMileagePlusResponse> GetPNRsByMileagePlusNumber(int applicationId, string appVersion, string accessCode, string transactionId, string mileagePlusNumber, string pinCode, string reservationType, string languageCode, bool includeFarelockInfo = false);
        Task<MOBReceiptByEmailResponse> RequestReceiptByEmail(MOBReceiptByEmailRequest request);
        Task<MOBPNRRemarkResponse> AddPNRRemark(MOBPNRRemarkRequest request);
    }
}
