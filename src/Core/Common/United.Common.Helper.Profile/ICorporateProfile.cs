using System.Collections.Generic;
using System.Threading.Tasks;
using United.CorporateDirect.Models.CustomerProfile;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Common.Helper.Profile
{
    public interface ICorporateProfile
    {
        bool IsCorporateLeisureFareSelected(List<MOBSHOPTrip> trips);
        MOBCPCorporate PopulateCorporateData(United.Services.Customer.Common.Corporate corporateData, MOBApplication application = null);
        Task<MOBCPCorporate> PopulateCorporateData(MOBCPProfileRequest request);
        Task MakeCorpFopServiceCall(MOBCPProfileRequest request);
        Task MakeCorpProfileServiecall(MOBCPProfileRequest request);
        Task<CorpMpNumberValidationResponse> CorpMpNumberValidation(string token, string sessionId, List<string> mpNumbers, int UCSID);
        InfoWarningMessages CorpMultiPaxinfoWarningMessages(List<CMSContentMessage> cMSContentMessages);
    }
}
