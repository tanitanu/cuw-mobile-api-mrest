using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.PostBooking;
using United.Mobile.PostBooking.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model;

namespace United.Mobile.PostBooking.Api.Controllers
{
    [Route("postbookingservice/api")]
    [ApiController]
    public class PostBookingController : ControllerBase
    {
        private readonly ICacheLog<PostBookingController> _logger;
        private readonly IHeaders _headers;
        private readonly IConfiguration _configuration;
        private readonly IPostBookingBusiness _postBookingBusiness;
        private readonly IFeatureSettings _featureSettings;

        public PostBookingController(ICacheLog<PostBookingController> logger
            , IHeaders headers
            , IConfiguration configuration
            , IPostBookingBusiness postBookingBusiness
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _headers = headers;
            _configuration = configuration;
            _postBookingBusiness = postBookingBusiness;
            _featureSettings = featureSettings;
        }

        [HttpGet]
        [Route("HealthCheck")]
        public string HealthCheck()
        {
            return "Healthy";
        }

        [HttpGet]
        [Route("version")]
        public virtual string Version()
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
        [Route("PostBooking/GetOffers")]
        public async Task<MOBSHOPGetOffersResponse> GetOffers(MOBSHOPGetOffersRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId,request.Application.Id.ToString(),request.Application.Version.Major,request.TransactionId,request.LanguageCode,request.SessionId);
            var response = new MOBSHOPGetOffersResponse();
            IDisposable timer = null;

            try
            {
                using (timer = _logger.BeginTimedOperation("Total time taken for GetOffers business call", transationId: request.TransactionId))
                {
                    response = await _postBookingBusiness.GetOffers(request);
                }
            }
            catch (MOBUnitedException uex)
            {
                _logger.LogWarning("GetOffers Warning {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(uex), request.TransactionId);
                _logger.LogWarning("GetOffers Warning {exception} and {transactionId}", uex.Message, request.TransactionId);
                response.Exception = new MOBException();
                response.Exception.Message = uex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetOffers Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), request.TransactionId);
                _logger.LogError("GetOffers Error {exception} and {transactionId}", ex.Message, request.TransactionId);

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("GenericExceptionMessage"));
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetOffers {@clientResponse} {transactionId}", JsonConvert.SerializeObject(response), request.TransactionId);

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
                request.ServiceName = ServiceNames.POSTBOOKING.ToString();
                await _featureSettings.RefreshFeatureSettingCache(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("RefreshFeatureSettingCache Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), "RefreshRetrieveAllFeatureSettings_TransId");
                response.Exception = new MOBException("9999", JsonConvert.SerializeObject(ex));
            }
            return response;
        }
    }
}
