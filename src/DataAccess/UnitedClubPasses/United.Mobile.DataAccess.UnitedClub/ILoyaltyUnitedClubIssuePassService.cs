using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.UnitedClub
{
    public interface ILoyaltyUnitedClubIssuePassService
    {
        Task<string> GetLoyaltyUnitedClubIssuePass(string token, string requestData, string sessionId);
    }
}
