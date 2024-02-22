using System.Threading.Tasks;
using United.Mobile.Model.BagCalculator;
using United.Mobile.Model.Common;

namespace United.Mobile.BagCalculator.Domain
{
    public interface IBagCalculatorBusiness
    {
        Task<BaggageCalculatorSearchResponse> GetBaggageCalculatorSearch(string accessCode, string transactionId, string languageCode, string appVersion, int applicationId);
        Task<MobileCMSContentResponse> GetMobileCMSContentsData(MobileCMSContentRequest request);
        Task<DOTCheckedBagCalculatorResponse> CheckedBagEstimatesForAnyFlight(DOTCheckedBagCalculatorRequest request);
        Task<DOTCheckedBagCalculatorResponse> CheckedBagEstimatesForMyFlight(DOTCheckedBagCalculatorRequest request);
        Task<PrepayForCheckedBagsResponse> PrepayForCheckedBags(PrepayForCheckedBagsRequest request);
        Task<bool> UpdateiOSTokenBaGCalcRedesign(int applicationId, string deviceId);
    }
}
