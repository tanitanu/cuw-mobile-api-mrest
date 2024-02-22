using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.FeedBack;
using United.Mobile.Model.UpdateMemberProfile;

namespace United.Mobile.UpdateMemberProfile.Domain
{
    public interface IUpdateMemberProfileBusiness
    {
        Task<MOBCustomerProfileResponse> UpdateProfileOwner(MOBUpdateCustomerFOPRequest request);
        Task<MOBUpdateProfileOwnerFOPResponse> UpdateProfileOwnerCardInfo(MOBUpdateProfileOwnerFOPRequest request);
        bool IsViewResPath(string viewrespath);
        Task<MOBPromoFeedbackResponse> PromoFeedback(Model.FeedBack.MOBPromoFeedbackRequest promoFeedbackRequest);
        Task UpdateProvisionStatus(MOBUpdateProfileOwnerFOPRequest request);
    }
}
