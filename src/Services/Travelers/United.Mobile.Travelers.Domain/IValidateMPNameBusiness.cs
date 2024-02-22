using System.Threading.Tasks;
using United.Mobile.Model.Travelers;

namespace United.Mobile.Travelers.Domain
{
    public interface IValidateMPNameBusiness
    {
        Task<MOBMPNameMissMatchResponse> ValidateMPNameMisMatch_CFOP(MOBMPNameMissMatchRequest request);
    }
}
