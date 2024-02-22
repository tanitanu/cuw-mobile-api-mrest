using System.Threading.Tasks;
using United.Mobile.Model.ReShop;

namespace United.Mobile.EligibleCheck.Domain
{
    public interface IEligibleCheckBusiness
    {
        Task<MOBRESHOPChangeEligibilityResponse> ChangeEligibleCheckAndReshop(MOBRESHOPChangeEligibilityRequest request);

        Task<MOBRESHOPChangeEligibilityResponse> ChangeEligibleCheck(MOBRESHOPChangeEligibilityRequest request);
    }
}