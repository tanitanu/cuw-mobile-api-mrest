using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.Travelers
{
    public interface IPlacePassService
    {
        Task<(T response, long callDuration)> GetPlacePass<T>(string token, string url, string sessionId);
    }
}
