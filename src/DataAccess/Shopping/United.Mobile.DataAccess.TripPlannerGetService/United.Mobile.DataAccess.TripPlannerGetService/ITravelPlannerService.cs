using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.TripPlannerGetService
{
    public interface ITravelPlannerService
    {
        Task<string> TripPlan<T>(string token, string sessionId, string action, string shopRequest);
        Task<string> ShopTripPlanner(string token, string sessionId, string action, string request);
    }
}
