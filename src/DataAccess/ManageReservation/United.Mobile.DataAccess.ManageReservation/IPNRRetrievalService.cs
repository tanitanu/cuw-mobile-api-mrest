using System.Threading.Tasks;

namespace United.Mobile.DataAccess.ManageReservation
{
    public interface IPNRRetrievalService
    {
        Task<string> PNRRetrieval(string token, string requestPayload, string sessionId, string path = "");
        Task<T> GetOfferedMealsForItinerary<T>(string token, string action, string request, string sessionId);
        Task<string> UpdateTravelerInfo(string token, string requestData, string path, string sessionId);
    }
}
