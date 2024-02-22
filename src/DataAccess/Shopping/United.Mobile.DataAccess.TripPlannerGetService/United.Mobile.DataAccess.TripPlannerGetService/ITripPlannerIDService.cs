using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.TripPlanGetService
{
    public interface ITripPlannerIDService
    {
        Task<string> GetTripPlanID<T>(string token, string sessionId, string action, string request);
    }
}
