using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using United.Common.Helper;
using United.Mobile.BagCalculator.Domain;
using United.Mobile.Model;
using United.Mobile.Model.BagCalculator;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model.Internal.Exception;
using United.Utility.Helper;
using United.Utility.Serilog;

namespace United.Mobile.BagCalculator.Api.Controllers
{
    [Route("bagcalculatorservice/api/")]
    [ApiController]
    public class BagCalculatorController : ControllerBase
    {
        private readonly ICacheLog<BagCalculatorController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IBagCalculatorBusiness _bagCalculatorBusiness;
        private readonly IHeaders _headers;
        private readonly IFeatureSettings _featureSettings;
        public BagCalculatorController(ICacheLog<BagCalculatorController> logger, IConfiguration configuration, IBagCalculatorBusiness bagCalculatorBusiness, IHeaders headers, IFeatureSettings featureSettings)
        {          
            _logger = logger;        
            _configuration = configuration;           
            _bagCalculatorBusiness = bagCalculatorBusiness;
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
        [HttpGet]
        [Route("MerchandizingServices/GetBaggageCalculatorSearchValues")]
        public async Task<BaggageCalculatorSearchResponse> GetBaggageCalculatorSearchValues(string accessCode, string transactionId, string languageCode, string appVersion, int applicationId)
        {          
            var response = new BaggageCalculatorSearchResponse();

            try
            {
                var deviceId = transactionId?.Split('|')?[0] ?? string.Empty;
                await _headers.SetHttpHeader(deviceId, applicationId.ToString(), appVersion, transactionId, languageCode, string.Empty);
                response = await _bagCalculatorBusiness.GetBaggageCalculatorSearch(accessCode, transactionId, languageCode, appVersion, applicationId);
            }
            catch (MOBUnitedException uaex)
            {              
               _logger.LogWarning("GetBaggageCalculatorSearchValues MOBUnitedException {UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException() {Message=uaex.Message };
            }
            catch (Exception ex)
            {                
                _logger.LogError("GetBaggageCalculatorSearchValues Error {exception}", JsonConvert.SerializeObject(ex));  
                response.Exception = new MOBException("99999",_configuration.GetValue<string>("GenericExceptionMessage"));
            }            

            _logger.LogInformation("GetBaggageCalculatorSearchValues {@clientResponse}", JsonConvert.SerializeObject(response));
            
            return response;
        }

        [HttpPost]
        [Route("CheckedBaggageEstimatesAnyFlight")]
        public async Task<DOTCheckedBagCalculatorResponse> CheckedBaggageEstimatesAnyFlight(DOTCheckedBagCalculatorRequest request)
        {
            
            var response = new DOTCheckedBagCalculatorResponse();
            try
            {
                if (string.IsNullOrEmpty(request.DeviceId)) request.DeviceId = request.TransactionId?.Split('|')?[0];
                await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, string.Empty);

                _logger.LogInformation("CheckedBaggageEstimatesAnyFlight {@clientRequest}", JsonConvert.SerializeObject(request));

                response = await _bagCalculatorBusiness.CheckedBagEstimatesForAnyFlight(request);
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("CheckedBaggageEstimatesAnyFlight MOBUnitedException {UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException("99999", _configuration.GetValue<string>("CheckBagInfo_GenericException"));
            }
            catch (Exception ex)
            {
                _logger.LogError("CheckedBaggageEstimatesAnyFlight Error {exception}", JsonConvert.SerializeObject(ex));
                response.Exception = new MOBException("99999", _configuration.GetValue<string>("CheckBagInfo_GenericException"));
            }

            _logger.LogInformation("CheckedBaggageEstimatesAnyFlight {@clientResponse}", JsonConvert.SerializeObject(response,Formatting.None));

            return response;            
        }

        [HttpPost]
        [Route("CheckedBaggageEstimatesMyFlight")]
        public async Task<DOTCheckedBagCalculatorResponse> CheckedBaggageEstimatesMyFlight(DOTCheckedBagCalculatorRequest request)
        {

            var response = new DOTCheckedBagCalculatorResponse();
            try
            {
                if (string.IsNullOrEmpty(request.DeviceId)) request.DeviceId = request.TransactionId?.Split('|')?[0];
                await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId ?? string.Empty);
                _logger.LogInformation("CheckedBaggageEstimatesMyFlight {@clientRequest} {TransactionId} {sessionId}", JsonConvert.SerializeObject(request), request.TransactionId, request.SessionId);
                response = await _bagCalculatorBusiness.CheckedBagEstimatesForMyFlight(request);
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("CheckedBaggageEstimatesMyFlight MOBUnitedException {UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException("99999", string.IsNullOrEmpty(uaex.Message) ? _configuration.GetValue<string>("CheckBagInfo_GenericException") : uaex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError("CheckedBaggageEstimatesMyFlight Error {exception} ", JsonConvert.SerializeObject(ex));
                response.Exception = new MOBException("99999", _configuration.GetValue<string>("CheckBagInfo_GenericException"));
            }

            response.CallDuration = 0;
            _logger.LogInformation("CheckedBaggageEstimatesMyFlight {@clientResponse}", JsonConvert.SerializeObject(response, Formatting.None));

            return response;
        }


        [HttpPost]
        [Route("PrepayForCheckedBags")]
        public async Task<PrepayForCheckedBagsResponse> PrepayForCheckedBags(PrepayForCheckedBagsRequest request)
        {
            var response = new PrepayForCheckedBagsResponse();

            try
            {
                await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId ?? string.Empty);
                _logger.LogInformation("PrepayForCheckedBags {@clientRequest}", JsonConvert.SerializeObject(request));
                response = await _bagCalculatorBusiness.PrepayForCheckedBags(request);
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("PrepayForCheckedBags MOBUnitedException {UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException("99999", string.IsNullOrEmpty(uaex.Message) ? _configuration.GetValue<string>("CheckBagInfo_GenericException") : uaex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError("PrepayForCheckedBags Error {Exception}", JsonConvert.SerializeObject(ex));

                response.Exception = new MOBException("99999", _configuration.GetValue<string>("CheckBagInfo_GenericException"));
            }

            _logger.LogInformation("PrepayForCheckedBags {@clientResponse}", JsonConvert.SerializeObject(response, Formatting.None));

            return response;
        }

        [HttpPost]
        [Route("Traveler/GetMobileCMSContents")]
        public async Task<MobileCMSContentResponse> GetMobileCMSContents(MobileCMSContentRequest request)
        {            
            var response = new MobileCMSContentResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
                _logger.LogInformation("GetMobileCMSContents {@ClientRequest}", JsonConvert.SerializeObject(request));
                using (timer = _logger.BeginTimedOperation("Total time taken for GetMobileCMSContents business call", transationId: request.TransactionId))
                {
                    response = await _bagCalculatorBusiness.GetMobileCMSContentsData(request);
                }
            }
            catch (MOBUnitedException uaex)
            {
                MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);
                _logger.LogWarning("GetMobileCMSContents MOBUnitedException {@UnitedException}", JsonConvert.SerializeObject(uaex));

                response.Exception = _configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain") ? 
                new MOBException() {Code = uaex.Code,Message = uaex.Message } : new MOBException() { Message = uaex.Message };
            }
            catch (System.Exception ex)
            {
                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                _logger.LogError("GetMobileCMSContents Error {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));

                response.Exception = (!Convert.ToBoolean(_configuration.GetValue<String>("SurfaceErrorToClient"))) ?
                new MOBException("9999", _configuration.GetValue<String>("Booking2OGenericExceptionMessage")) :
                new MOBException("9999", ex.Message);
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetMobileCMSContents {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpGet]
        [Route("UpdateiOSTokenBaGCalcRedesign")]
        public async Task<MOBResponse> UpdateiOSTokenBaGCalcRedesign(string accessCode, string transactionId, string languageCode, string appVersion, int applicationId, string deviceId)
        {
            MOBResponse response = new MOBResponse();
            try
            {
                if (accessCode.ToUpper().Trim() == "B6E173B3E4834FF2ADE18CEC9218A5BD".ToUpper().Trim())
                {
                    await _headers.SetHttpHeader(deviceId, applicationId.ToString(), appVersion, transactionId, languageCode, string.Empty);
                    bool isSuccess = await _bagCalculatorBusiness.UpdateiOSTokenBaGCalcRedesign(applicationId, deviceId);

                    if (isSuccess == false)
                    {
                        response.Exception = new MOBException("99999", _configuration.GetValue<string>("GenericExceptionMessage"));
                    }
                    else
                    {
                        response.Exception = new MOBException("Success", "Succesfully refreshed the token for iOS in cache.");
                    }
                }
                else
                {
                    response.Exception = new MOBException("99999", _configuration.GetValue<string>("GenericExceptionMessage"));
                }
            }
            catch (Exception ex)
            {
                response.Exception = new MOBException("99999", _configuration.GetValue<string>("GenericExceptionMessage"));
            }
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
                request.ServiceName = ServiceNames.BAGCALCULATOR.ToString();
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