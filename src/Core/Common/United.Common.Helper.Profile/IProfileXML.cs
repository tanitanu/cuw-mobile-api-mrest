using System.Threading.Tasks;
using United.Mobile.Model.Common;

namespace United.Common.Helper.Profile
{
    public interface IProfileXML
    {
        Task<MOBProfile> GetOwnerProfileForMP2014(string mileagePlusAccountNumber);
        Task<MOBProfile> GetProfile(MOBProfileRequest request);

    }
}
