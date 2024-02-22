using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.SeatMap;
using United.Mobile.SeatMap.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model;

namespace United.Mobile.SeatMap.Api.Controllers
{
    [Route("seatmapservice/api")]
    [ApiController]
    public class SeatMapController : ControllerBase
    {
        private readonly ICacheLog<SeatMapController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly ISeatMapBusiness _seatMapBusiness;
        private readonly IFeatureSettings _featureSettings;

        public SeatMapController(ICacheLog<SeatMapController> logger, IConfiguration configuration
            , IHeaders headers
            , ISeatMapBusiness seatMapBusiness
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _headers = headers;
            _seatMapBusiness = seatMapBusiness;
            _featureSettings = featureSettings;
        }

        [HttpGet]
        [Route("HealthCheck")]
        public string HealthCheck()
        {
            return "Healthy";
        }

        [HttpGet]
        [Route("Version")]
        public string Version()
        {
            string serviceVersionNumber = null;

            try
            {
                serviceVersionNumber = System.Environment.GetEnvironmentVariable("SERVICE_VERSION_NUMBER");
            }
            catch
            {
                // Suppress any exceptions
            }
            finally
            {
                serviceVersionNumber = (null == serviceVersionNumber) ? "Unable to retrieve the version number" : serviceVersionNumber;
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
        [HttpGet]
        [Route("GetSeatMap")]
        public async Task<MOBSeatMapResponse> GetSeatMap(int applicationid, string appVersion, string accessCode, string transactionId, string carrierCode, string flightNumber, string flightDate, string departureAirportCode, string arrivalAirportCode, string languageCode, string scheduledDepartureAirportCode = "", string scheduledArrivalAirportCode = "")
        {
            await _headers.SetHttpHeader(string.Empty, applicationid.ToString(), appVersion, transactionId, languageCode, string.Empty);
            var response = new MOBSeatMapResponse();
            IDisposable timer = null;
            try
            {
                _logger.LogInformation("GetSeatMap {ApplicationId} {AppVersion} {AccessCode} {transactionId} {CarrierCode} {FlightNumber} {FlightDate} {DepartureCode} {ArrivalCode} {LanguageCode}", applicationid, appVersion, accessCode, transactionId, carrierCode, flightNumber, flightDate, departureAirportCode, arrivalAirportCode, languageCode);

                timer = _logger.BeginTimedOperation("Total time taken for GetSeatMap business call", transationId: transactionId);
                response = await _seatMapBusiness.GetSeatMap(applicationid, appVersion, accessCode, transactionId, carrierCode, flightNumber, flightDate, departureAirportCode, arrivalAirportCode, languageCode, scheduledDepartureAirportCode, scheduledArrivalAirportCode);
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("GetSeatMap Error {exception} {exceptionstack} and {transactionId}", coex.Message, JsonConvert.SerializeObject(coex), transactionId);

                response.Exception = new MOBException();
                response.Exception.Message = coex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetSeatMap Error {exception} {exceptionstack} and {transactionId}", ex.Message, JsonConvert.SerializeObject(ex), transactionId);

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("GenericExceptionMessage"));
            }

            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }

            _logger.LogInformation("GetSeatMap {@clientResponse} {transactionId}", JsonConvert.SerializeObject(response), transactionId);

            return response;
        }

        [HttpPost]
        [Route("RegisterSeats")]
        public async Task<MOBRegisterSeatsResponse> RegisterSeats(MOBRegisterSeatsRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBRegisterSeatsResponse();
            IDisposable timer = null;
            try
            {
                _logger.LogInformation("RegisterSeats {@clientRequest} {DeviceId} {SessionId} {TransactionId}", JsonConvert.SerializeObject(request), request.DeviceId, request.SessionId, request.TransactionId);

                timer = _logger.BeginTimedOperation("Total time taken for RegisterSeats business call", transationId: request.TransactionId);
                response = await _seatMapBusiness.RegisterSeats(request);
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("RegisterSeats Error {exception} {exceptionstack} {SessionId} and {TransactionId}", coex.Message, JsonConvert.SerializeObject(coex), request.SessionId, request.TransactionId);

                response.Exception = new MOBException();
                response.Exception.Message = coex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("RegisterSeats Error {exception} {exceptionstack} {SessionId} and {TransactionId}", ex.Message, JsonConvert.SerializeObject(ex), request.SessionId, request.TransactionId);

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("GenericExceptionMessage"));
            }

            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }

            _logger.LogInformation("RegisterSeats {@clientResponse} {SessionId} {transactionId}", JsonConvert.SerializeObject(response), request.SessionId, request.TransactionId);

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
