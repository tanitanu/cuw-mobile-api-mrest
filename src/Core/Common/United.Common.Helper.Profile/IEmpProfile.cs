using System.Collections.Generic;
using System.Threading.Tasks;
using United.Mobile.Model.Common;

namespace United.Common.Helper.Profile
{
    public interface IEmpProfile
    {
        Task<List<MOBCPProfile>> GetEmpProfile(MOBCPProfileRequest request, bool getEmployeeIdFromCSLCustomerData = false);
        Task<string> GetOnlyEmpIDForWalletCall(MOBCPProfileRequest request, bool getEmployeeIdFromCSLCustomerData = false);
    }
}
