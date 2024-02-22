using System.Threading.Tasks;
using United.Mobile.Model.ReShop;

namespace United.Mobile.ScheduleChange.Domain
{
    public interface IScheduleChangeBusiness
    {
        Task<MOBConfirmScheduleChangeResponse> ConfirmScheduleChange(MOBConfirmScheduleChangeRequest request);
        Task<MOBConfirmScheduleChangeResponse> ConfirmScheduleChangeCSL(MOBConfirmScheduleChangeRequest schedulechangerequest);
       }
}