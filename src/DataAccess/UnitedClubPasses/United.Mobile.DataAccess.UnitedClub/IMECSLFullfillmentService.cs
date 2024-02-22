using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.UnitedClub
{
    public interface IMECSLFullfillmentService
    {
        Task<string> GetMECSLFullfillment(string token, string requestData, string sessionId);
    }
}
