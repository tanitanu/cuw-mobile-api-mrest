using System.Threading.Tasks;

namespace United.Mobile.DataAccess.Customer
{
    public interface ICustomerTravelerService
    {
        Task<T> InsertTraveler<T>(string token, string sessionId, string loyaltyId, string jsonRequest);
        Task<T> UpdateTravelerBase<T>(string token, string sessionId, string customerId, string jsonRequest);
    }
}
