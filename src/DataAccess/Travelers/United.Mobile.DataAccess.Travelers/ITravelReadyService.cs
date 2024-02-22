using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.Travelers
{
    public interface ITravelReadyService
    {
        Task<(T response, long callDuration)> GetCovidLite<T>(string token, string action, string request, string sessionId);
    }
}
