using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Ebs.Logging.Enrichers;
using United.Mobile.Model;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.TripPlannerGetService;
using United.Mobile.Services.TripPlannerGetService.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Common;
using United.Mobile.Model.TripPlannerService;

namespace United.Mobile.Services.TripPlannerGetService.Api.Controllers
{
    [ApiController]
    [Route("tripplannergetservice/api/")]
    public class TripPlannerGetServiceController : ControllerBase
    {
        private readonly ICacheLog<TripPlannerGetServiceController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ITripPlannerGetServiceBusiness _tripPlannerGetServiceBusiness;
        private readonly ITripPlannerServiceBusiness _tripPlannerServiceBusiness;
        private readonly IHeaders _headers;
        private readonly IApplicationEnricher _requestEnricher;
        private readonly IFeatureSettings _featureSettings;

        public TripPlannerGetServiceController(ICacheLog<TripPlannerGetServiceController> logger
            , IConfiguration configuration
            , ITripPlannerGetServiceBusiness tripPlannerGetServiceBusiness
            , ITripPlannerServiceBusiness tripPlannerServiceBusiness
            , IHeaders headers
            , IApplicationEnricher requestEnricher, IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _tripPlannerGetServiceBusiness = tripPlannerGetServiceBusiness;
            _tripPlannerServiceBusiness = tripPlannerServiceBusiness;
            _headers = headers;
            _requestEnricher = requestEnricher;
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
        [Route("Shopping/GetTripPlanSummary")]
        public async Task<MOBTripPlanSummaryResponse> GetTripPlanSummary(MOBTripPlanSummaryRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            MOBTripPlanSummaryResponse response = new MOBTripPlanSummaryResponse();

            IDisposable timer = null;
            try
            {
                using (timer = _logger.BeginTimedOperation("Total time taken for GetTripPlanSummary business call", transationId: request.TransactionId))
                {
                    _logger.LogInformation("GetTripPlanSummary {@ClientRequest}", JsonConvert.SerializeObject(request));
                    response = await _tripPlannerGetServiceBusiness.GetTripPlanSummary(request);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetTripPlanSummary Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException
                {
                    Message = uaex.Message
                };
                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = uaex.Code;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetTripPlanSummary Error {@Exception}", JsonConvert.SerializeObject(ex));

                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetTripPlanSummary {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost]
        [Route("Shopping/SelectTripTripPlanner")]
        public async Task<MOBSHOPSelectTripResponse> SelectTripTripPlanner(MOBSHOPSelectTripRequest request)
        {

            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            MOBSHOPSelectTripResponse response = new MOBSHOPSelectTripResponse();
            IDisposable timer = null;
            try
            {
                using (timer = _logger.BeginTimedOperation("Total time taken for SelectTripTripPlanner business call", transationId: request.TransactionId))
                {
                    _logger.LogInformation("SelectTripTripPlanner {@ClientRequest}", JsonConvert.SerializeObject(request));
                    response = await _tripPlannerGetServiceBusiness.SelectTripTripPlanner(request, HttpContext);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("SelectTripTripPlanner Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException
                {
                    Message = uaex.Message
                };
                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = uaex.Code;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SelectTripTripPlanner Error {@Exception}", JsonConvert.SerializeObject(ex));

                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("SelectTripTripPlanner {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost]
        [Route("Shopping/GetTripPlanBoard")]
        public async Task<MOBTripPlanBoardResponse> GetTripPlanBoard(MOBTripPlanBoardRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBTripPlanBoardResponse();
            IDisposable timer = null;
            try
            {
                using (timer = _logger.BeginTimedOperation("Total time taken for GetTripPlanBoard business call", transationId: request.TransactionId))
                {
                    _logger.LogInformation("GetTripPlanBoard {@ClientRequest}", JsonConvert.SerializeObject(request));
                    response = await _tripPlannerGetServiceBusiness.GetTripPlanBoard(request);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetTripPlanBoard Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException
                {
                    Message = uaex.Message
                };
                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = uaex.Code;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetTripPlanBoard Error {@Exception}", JsonConvert.SerializeObject(ex));

                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetTripPlanBoard {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
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
                request.ServiceName = ServiceNames.TRIPPLANNERGETSERVICE.ToString();
                await _featureSettings.RefreshFeatureSettingCache(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("RefreshFeatureSettingCache Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), "RefreshRetrieveAllFeatureSettings_TransId");
                response.Exception = new MOBException("9999", JsonConvert.SerializeObject(ex));
            }
            return response;
        }

        [HttpGet]
        [Route("Shopping/RefreshCacheForFlightCargoDimensions")]
        public async Task<RefreshCacheForFlightCargoDimensionsResponse> RefreshCacheForFlightCargoDimensions()
        {
            RefreshCacheForFlightCargoDimensionsResponse response = new RefreshCacheForFlightCargoDimensionsResponse();
            try
            {
                response = await _tripPlannerGetServiceBusiness.RefreshCacheForFlightCargoDimensions();
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("RefreshCacheForFlightCargoDimensions Error {@UnitedException}", JsonConvert.SerializeObject(coex));

                response.Exception = new MOBException { Message = coex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError("RefreshCacheForFlightCargoDimensions Error {@Exception}", JsonConvert.SerializeObject(ex));

                response.Exception = new MOBException("9999", ex.Message);

            }
            _logger.LogInformation("RefreshCacheForFlightCargoDimensions {@ClientResponse}", JsonConvert.SerializeObject(response));

            return response;
        }
    }
}






