using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.Travelers
{
    public interface IFlightShoppingProductsService
    {
       Task<string> GetProducts(string token,  string sessionId, string request);
       // Task<string> RegisterOffer(string token, string sessionId, string request);
        Task<T> MilesAndMoneyOption<T>(string token, string action, string request, string sessionId);
        Task<string> ApplyCSLMilesPlusMoneyOptions(string token, string action, string request, string sessionId);
        Task<string> GetCSLMilesPlusMoneyOptions(string token, string action, string request, string sessionId);

        Task<string> GetTripInsuranceInfo(string token, string action, string request, string sessionId);
        Task<string> RegisterFareLocks(string token, string action, string request, string sessionId);
        Task<string> RegisterOffer(string token, string action, string request, string sessionId);


    }
}
