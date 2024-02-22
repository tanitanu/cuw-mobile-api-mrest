using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.TripPlannerGetService;

namespace United.Mobile.Services.TripPlannerGetService.Domain
{
    public interface ITripPlannerGetServiceBusiness
    {
        Task<MOBTripPlanSummaryResponse> GetTripPlanSummary(MOBTripPlanSummaryRequest request);
        Task<MOBSHOPSelectTripResponse> SelectTripTripPlanner(MOBSHOPSelectTripRequest request, HttpContext httpContext);
        Task<MOBTripPlanBoardResponse> GetTripPlanBoard(MOBTripPlanBoardRequest request);
        Task<RefreshCacheForFlightCargoDimensionsResponse> RefreshCacheForFlightCargoDimensions();
    }
}
