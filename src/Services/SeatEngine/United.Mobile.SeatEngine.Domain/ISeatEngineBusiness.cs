using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.SeatMapEngine;

namespace United.Mobile.SeatEngine.Domain
{
    public interface ISeatEngineBusiness
    {
        Task<MOBSeatMapResponse> PreviewSeatMap(MOBSeatMapRequest request);
        Task<MOBSeatMapResponse> GetSeatMap(int applicationid, string appVersion, string accessCode, string transactionId, string carrierCode, string flightNumber, string flightDate, string departureAirportCode, string arrivalAirportCode, string languageCode, string deviceId, string sessionId);
    }
}
