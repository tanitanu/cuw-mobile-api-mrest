using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.TripPlannerService;
using United.Mobile.Services.TripPlannerService.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model;
using United.Mobile.Model.Common;

namespace United.Mobile.Services.TripPlannerService.Api.Controllers
{
    [ApiController]
    [Route("tripplannerservice/api")]
    public class TripPlannerServiceController : ControllerBase
    {
        private readonly ICacheLog<TripPlannerServiceController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ITripPlannerServiceBusiness _tripPlannerServiceBusiness;
        private readonly IHeaders _headers;
        private readonly IFeatureSettings _featureSettings;
        public TripPlannerServiceController(ICacheLog<TripPlannerServiceController> logger
            , IConfiguration configuration
            , ITripPlannerServiceBusiness tripPlannerServiceBusiness
            , IHeaders headers
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _tripPlannerServiceBusiness = tripPlannerServiceBusiness;
            _headers = headers;
            _featureSettings = featureSettings;
        }

        [HttpGet]
        [Route("HealthCheck")]
        public string HealthCheck()
        {
            return "Healthy";
        }

        /// <summary>/// HTTP GET/// Returns a version number of the service./// </summary>/// <returns>/// Version Number of the Service./// </returns>
        [HttpGet]
        [Route("version")]
        public string Version()
        {
            string serviceVersionNumber = null;

            try
            {
                serviceVersionNumber = Environment.GetEnvironmentVariable("SERVICE_VERSION_NUMBER");
            }
            catch
            {
                // Suppress any exceptions
            }
            finally
            {
                serviceVersionNumber = (null == serviceVersionNumber) ? "0.0.0" : serviceVersionNumber;
            }

            return serviceVersionNumber;
        }
        [HttpGet]
        [Route("environment")]
        public virtual string ApiEnvironment()
        {
            try
            {
                return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            }
            catch
            {
            }
            return "Unknown";
        }
        [HttpPost]
        [Route("Shopping/AddTripPlanVoting")]
        public async Task<MOBTripPlanVoteResponse> AddTripPlanVoting(MOBTripPlanVoteRequest request)
        {

            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            MOBTripPlanVoteResponse response = new MOBTripPlanVoteResponse();

            IDisposable timer = null;
            try
            {
                _logger.LogInformation("AddTripPlanVoting {@ClientRequest}", JsonConvert.SerializeObject(request));

                using (timer = _logger.BeginTimedOperation("Total time taken for AddTripPlanVoting business call", transationId: request.TransactionId))
                {
                    response = await _tripPlannerServiceBusiness.AddTripPlanVoting(request);
                }
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("AddTripPlanVoting Error {@UnitedException}", JsonConvert.SerializeObject(coex));

                response.Exception = new MOBException { Message = ("Booking2OGenericExceptionMessage") };

                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = coex.Code;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("AddTripPlanVoting Error {@Exception}", JsonConvert.SerializeObject(ex));

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

            }

            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }

            _logger.LogInformation("AddTripPlanVoting {@ClientResponse}", JsonConvert.SerializeObject(response));

            return response;
        }

        [HttpPost]
        [Route("Shopping/DeleteTripPlan")]
        public async Task<MOBTripPlanDeleteResponse> DeleteTripPlan(MOBTripPlanDeleteRequest request)
        {

            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            MOBTripPlanDeleteResponse response = new MOBTripPlanDeleteResponse();

            IDisposable timer = null;
            try
            {
                _logger.LogInformation("DeleteTripPlan {@ClientRequest}", JsonConvert.SerializeObject(request));

                timer = _logger.BeginTimedOperation("Total time taken for DeleteTripPlan business call", transationId: request.TransactionId);
                response = await _tripPlannerServiceBusiness.DeleteTripPlan(request);
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("DeleteTripPlan UnitedException {@UnitedException}", JsonConvert.SerializeObject(coex));

                response.Exception = new MOBException { Message = ("Booking2OGenericExceptionMessage") };

                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = coex.Code;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteTripPlan Error {@Exception}", JsonConvert.SerializeObject(ex));

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

            }

            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }

            _logger.LogInformation("DeleteTripPlan {@ClientResponse}", JsonConvert.SerializeObject(response));

            return response;
        }

        [HttpPost]
        [Route("Shopping/DeleteTripPlanVoting")]
        public async Task<MOBTripPlanVoteResponse> DeleteTripPlanVoting(MOBTripPlanVoteRequest request)
        {

            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            MOBTripPlanVoteResponse response = new MOBTripPlanVoteResponse();

            IDisposable timer = null;
            try
            {
                _logger.LogInformation("DeleteTripPlanVoting {@ClientRequest}", JsonConvert.SerializeObject(request));

                using (timer = _logger.BeginTimedOperation("Total time taken for DeleteTripPlan business call", transationId: request.TransactionId))
                {
                    response = await _tripPlannerServiceBusiness.DeleteTripPlanVoting(request);
                }
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("DeleteTripPlanVoting UnitedException {@UnitedException}", JsonConvert.SerializeObject(coex));

                response.Exception = new MOBException { Message = ("Booking2OGenericExceptionMessage") };

                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = coex.Code;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteTripPlanVoting Error {@Exception}", JsonConvert.SerializeObject(ex));

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

            }

            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }

            _logger.LogInformation("DeleteTripPlanVoting {@ClientResponse}", JsonConvert.SerializeObject(response));

            return response;
        }
        [HttpGet("GetFeatureSettings")]
        public GetFeatureSettingsResponse GetFeatureSettings()
        {
            GetFeatureSettingsResponse response = new GetFeatureSettingsResponse();
            try
            {
                response = _featureSettings.GetFeatureSettings();
            }
            catch (Exception ex)
            {
                _logger.LogError("GetFeatureSettings Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), "GetFeatureSettings_TransId");
                response.Exception = new MOBException("9999", JsonConvert.SerializeObject(ex));
            }
            return response;
        }
        [HttpPost("RefreshFeatureSettingCache")]
        public async Task<MOBResponse> RefreshFeatureSettingCache(MOBFeatureSettingsCacheRequest request)
        {
            MOBResponse response = new MOBResponse();
            try
            {
                request.ServiceName = ServiceNames.TRIPPLANNER.ToString();
                await _featureSettings.RefreshFeatureSettingCache(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("RefreshFeatureSettingCache Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), "RefreshRetrieveAllFeatureSettings_TransId");
                response.Exception = new MOBException("9999", JsonConvert.SerializeObject(ex));
            }
            return response;
        }
        [HttpPost("ServicesContainerRefreshStatus")]
        public async Task<GetFeatureSettingsResponse> ServicesContainerRefreshStatus(MOBFeatureSettingsCacheRequest request)
        {
            GetFeatureSettingsResponse response = new GetFeatureSettingsResponse();
            try
            {
                response=await _featureSettings.ServicesContainerRefreshStatus(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("ServicesContainerRefreshStatus Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), "ServicesContainerRefreshStatus_TransId");
                response.Exception = new MOBException("9999", JsonConvert.SerializeObject(ex));
            }
            return response;
        }
        [HttpPost("UpdateRefreshToggleToFalse")]
        public async Task<MOBResponse> UpdateRefreshToggleToFalse(MOBFeatureSettingsCacheRequest request)
        {
            MOBResponse response = new MOBResponse();
            try
            {
                await _featureSettings.UpdateRefreshToggleToFalse(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateRefreshToggleToFalse Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), "ServicesContainerRefreshStatus_TransId");
                response.Exception = new MOBException("9999", JsonConvert.SerializeObject(ex));
            }
            return response;
        }
    }
}
