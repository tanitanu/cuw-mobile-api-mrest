using System.Threading.Tasks;
using United.Mobile.Model.TripPlannerService;

namespace United.Mobile.Services.TripPlannerService.Domain
{
    public interface ITripPlannerServiceBusiness
    {
        Task<MOBTripPlanVoteResponse> AddTripPlanVoting(MOBTripPlanVoteRequest request);
        Task<MOBTripPlanDeleteResponse> DeleteTripPlan(MOBTripPlanDeleteRequest request);
        Task<MOBTripPlanVoteResponse> DeleteTripPlanVoting(MOBTripPlanVoteRequest request);
    }
}
