using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.SeatMap;

namespace United.Mobile.SeatMap.Domain
{
    public interface ISeatMapBusiness
    {
        Task<MOBSeatMapResponse> GetSeatMap(int applicationid, string appVersion, string accessCode, string transactionId, string carrierCode, string flightNumber, string flightDate, string departureAirportCode, string arrivalAirportCode, string languageCode, string scheduledDepartureAirportCode, string scheduledArrivalAirportCode);
        Task<MOBRegisterSeatsResponse> RegisterSeats(MOBRegisterSeatsRequest request);
    }
}
