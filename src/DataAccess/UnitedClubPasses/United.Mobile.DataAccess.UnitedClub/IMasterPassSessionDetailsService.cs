using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.UnitedClub
{
    public interface IMasterPassSessionDetailsService
    {
        Task<string> GetMasterPassSessionDetails(string token, string requestData, string sessionId);

    }
}
