using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.FlightShopping
{
    public interface ILMXInfo
    {
        Task<T> GetProductInfo<T>(string token, string request, string sessionId);
        Task<T> GetLmxRTIInfo<T>(string token, string request, string sessionId);
        Task<T> GetLmxFlight<T>(string token, string request, string sessionId);
    }
}
