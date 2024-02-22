using System.Collections.Generic;
using System.Threading.Tasks;
using United.Mobile.Model.Common;

namespace United.Common.Helper.Traveler
{
    public interface ICMSContentHelper
    {
        Task<List<MobileCMSContentMessages>> GetMobileCMSContents(MobileCMSContentRequest request);
        Task<List<MobileCMSContentMessages>> GetMobileTermsAndConditions(MobileCMSContentRequest request);

        Task<string> GETCSLCMSContent(MobileCMSContentRequest request, bool isTravelAdvisory = false);
    }
}
