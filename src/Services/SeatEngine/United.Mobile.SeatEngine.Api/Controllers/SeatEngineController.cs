using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Ebs.Logging.Enrichers;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.SeatEngine.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;
using Constants = United.Mobile.Model.Constants;
using United.Mobile.Model.Common.FeatureSettings;

namespace United.Mobile.SeatEngine.Api.Controllers
{
    [Route("seatengineservice/api/")]
    [ApiController]
    public class SeatEngineController : ControllerBase
    {
        private readonly ICacheLog<SeatEngineController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly IApplicationEnricher _requestEnricher;
        private readonly ISeatEngineBusiness _seatEngineBusiness;
        private readonly IFeatureSettings _featureSettings;

        public SeatEngineController(ICacheLog<SeatEngineController> logger
            , IConfiguration configuration
            , IHeaders headers
            , IApplicationEnricher requestEnricher
            , ISeatEngineBusiness seatEngineBusiness
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _headers = headers;
            _requestEnricher = requestEnricher;
            _seatEngineBusiness = seatEngineBusiness;
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
        [Route("PreviewSeatMap")]
        public async Task<MOBSeatMapResponse> PreviewSeatMap([FromBody] MOBSeatMapRequest request)
        {
            MOBSeatMapResponse response = new MOBSeatMapResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
                
                _logger.LogInformation("PreviewSeatMap {clientRequest}", JsonConvert.SerializeObject(request));

                using (timer = _logger.BeginTimedOperation("Total time taken for PreviewSeatMap business call", transationId: _headers.ContextValues.TransactionId))
                {
                    response = await _seatEngineBusiness.PreviewSeatMap(request);
                }
                    
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("PreviewSeatMap Error {exception} {exceptionstack} and {transactionId}", coex.Message, JsonConvert.SerializeObject(coex), request.TransactionId);

                response.Exception = new MOBException();
                response.Exception.Message = coex.Message;
            }
            catch (Exception ex)
            {
                var exceptionWrapper = new MOBExceptionWrapper(ex);

                _logger.LogError("PreviewSeatMap Error {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));
                
                response.Exception = !_configuration.GetValue<bool>("SurfaceErrorToClient") ? new MOBException("9999", @"Advance seat assignments are not available through United for this flight. Please see an airport agent during check-in to receive your seat assignment") : new MOBException("9999", ex.Message);
            }

            response.CallDuration = 0;

            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }

            _logger.LogInformation("PreviewSeatMap {clientResponse}", JsonConvert.SerializeObject(response));

            return response;
        }

        [HttpGet]
        [Route("GetSeatMap")]
        public async Task<MOBSeatMapResponse> GetSeatMap(int applicationid, string appVersion, string accessCode, string transactionId, string carrierCode, string flightNumber, string flightDate, string departureAirportCode, string arrivalAirportCode, string languageCode, string deviceId, string sessionId)
        {
            MOBSeatMapResponse response = new MOBSeatMapResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(deviceId, applicationid.ToString(), appVersion, transactionId, DateTime.UtcNow.ToString(), sessionId);
                
                if (!string.IsNullOrEmpty(sessionId))
                {
                    _logger.LogInformation("EresGetSeatMap {ApplicationId} {Appversion} {DeviceId} and {sessionId}", _headers.ContextValues.Application.Id, _headers.ContextValues.Application.Version.Major, _headers.ContextValues.DeviceId, sessionId);
                }
                else
                {
                    _logger.LogInformation("EresGetSeatMap {ApplicationId} {Appversion} {DeviceId} and {TransactionId}", _headers.ContextValues.Application.Id, _headers.ContextValues.Application.Version.Major, _headers.ContextValues.DeviceId, _headers.ContextValues.TransactionId);
                }

                timer = _logger.BeginTimedOperation("Total time taken for GetSeatMap business call", transationId: _headers.ContextValues.TransactionId);

                response = await _seatEngineBusiness.GetSeatMap(applicationid, appVersion, accessCode, transactionId, carrierCode, flightNumber, flightDate, departureAirportCode, arrivalAirportCode, languageCode, deviceId, sessionId);
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("GetSeatMap Error {exception} {exceptionstack} and {transactionId}", coex.Message, JsonConvert.SerializeObject(coex), transactionId);

                response.Exception = new MOBException();
                response.Exception.Message = coex.Message;
            }
            catch (Exception ex)
            {
                var exceptionWrapper = new MOBExceptionWrapper(ex);
                _logger.LogError("GetSeatMap Error {exceptionstack} {ApplicationId} {Appversion} {DeviceId} and {sessionId}", JsonConvert.SerializeObject(exceptionWrapper), _headers.ContextValues.Application.Id, _headers.ContextValues.Application.Version.Major, _headers.ContextValues.DeviceId, sessionId);
                _logger.LogError("GetSeatMap Error {exception} and {sessionId}", ex.Message, sessionId);
                response.Exception = !_configuration.GetValue<bool>("SurfaceErrorToClient") ? new MOBException("9999", @"Advance seat assignments are not available through United for this flight. Please see an airport agent during check-in to receive your seat assignment") : new MOBException("9999", ex.Message);
            }
            response.CallDuration = 0;
            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }
            _logger.LogInformation("GetSeatMap {clientResponse} and {sessionId}", JsonConvert.SerializeObject(response), sessionId);

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
                request.ServiceName = ServiceNames.SEATMAP.ToString();
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
