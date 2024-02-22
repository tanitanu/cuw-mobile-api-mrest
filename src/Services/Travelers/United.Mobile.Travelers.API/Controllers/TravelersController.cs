using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Travelers;
using United.Mobile.Travelers.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model;

namespace United.Mobile.Travelers.API.Controllers
{
    [Route("travelersservice/api/")]
    [ApiController]
    public class TravelersController : ControllerBase
    {
        private readonly ICacheLog<TravelersController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly ITravelerBusiness _travelerBusiness;
        private readonly IValidateMPNameBusiness _validateMPNameBusiness;
        private readonly IFeatureSettings _featureSettings;

        public TravelersController(ICacheLog<TravelersController> logger, IConfiguration configuration
            , IHeaders headers
            , ITravelerBusiness travelerBusiness
            , IValidateMPNameBusiness validateMPNameBusiness
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _headers = headers;
            _travelerBusiness = travelerBusiness;
            _validateMPNameBusiness = validateMPNameBusiness;
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
        [Route("Traveler/RegisterTravelers_CFOP")]
        public async Task<MOBRegisterTravelersResponse> RegisterTravelers_CFOP(MOBRegisterTravelersRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            MOBRegisterTravelersResponse response = new MOBRegisterTravelersResponse();
            IDisposable timer = null;

            try
            {
                _logger.LogInformation("RegisterTravelers_CFOP {@ClientRequest}", JsonConvert.SerializeObject(request));
                using (timer = _logger.BeginTimedOperation("Total time taken for RegisterTravelers_CFOP business call", transationId: request.TransactionId))
                {
                    response = await _travelerBusiness.RegisterTravelers_CFOP(request);
                }
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("RegisterTravelers_CFOP Warning {@UnitedException}", JsonConvert.SerializeObject(coex));
                response.Exception = new MOBException();
                response.Exception.Message = coex.Message;
                response.Exception.Code = coex.Code;
                if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                {
                    if (coex.InnerException != null && !string.IsNullOrEmpty(coex.InnerException.Message) && coex.InnerException.Message.Trim().Equals("10050"))
                    {
                        response.Exception.Code = "10050";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("RegisterTravelers_CFOP Error {@Exception}", JsonConvert.SerializeObject(ex));

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
            _logger.LogInformation("RegisterTravelers_CFOP {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost]
        [Route("Traveler/ValidateMPNameMisMatch_CFOP")]
        public async Task<MOBMPNameMissMatchResponse> ValidateMPNameMisMatch_CFOP(MOBMPNameMissMatchRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            MOBMPNameMissMatchResponse response = new MOBMPNameMissMatchResponse();
            IDisposable timer = null;

            try
            {
                _logger.LogInformation("ValidateMPNameMisMatch_CFOP {@ClientRequest}", JsonConvert.SerializeObject(request));
                using (timer = _logger.BeginTimedOperation("Total time taken for ValidateMPNameMisMatch_CFOP business call", transationId: request.TransactionId))
                {
                     response = await _validateMPNameBusiness.ValidateMPNameMisMatch_CFOP(request);
                }
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("ValidateMPNameMisMatch_CFOP Warning {@UnitedException}", JsonConvert.SerializeObject(coex));
                response.Exception = new MOBException();
                response.Exception.Message = coex.Message;
                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = coex.Code;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("ValidateMPNameMisMatch_CFOP Error {@Exception}", JsonConvert.SerializeObject(ex));

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
            _logger.LogInformation("ValidateMPNameMisMatch_CFOP {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }
        [HttpPost]
        [Route("Traveler/ValidateWheelChairSize")]
        public async Task<MOBValidateWheelChairSizeResponse> ValidateWheelChairSize(MOBValidateWheelChairSizeRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            MOBValidateWheelChairSizeResponse response = new MOBValidateWheelChairSizeResponse();
            IDisposable timer = null;

            try
            {
                _logger.LogInformation("ValidateWheelChairSize {@ClientRequest}", JsonConvert.SerializeObject(request));
                using (timer = _logger.BeginTimedOperation("Total time taken for ValidateWheelChairSize business call", transationId: request.TransactionId))
                {
                    response = await _travelerBusiness.ValidateWheelChairSize(request);
                }
            }
            catch (MOBUnitedException uex)
            {
                _logger.LogWarning("ValidateWheelChairSize Warning {@UnitedException}", JsonConvert.SerializeObject(uex));
                response.Exception = new MOBException();
                response.Exception.Message = uex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("ValidateWheelChairSize Error {@Exception}", JsonConvert.SerializeObject(ex));

                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }
            _logger.LogInformation("ValidateWheelChairSize {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpGet("CheckGlobalization")]
        public async Task<UnitedCurrency> CheckGlobalization()
        {
            var unitedCurrency = "{'Amount':10, 'Currency':'$'}";
            try
            {
                return await Task.Run(() => { return JsonConvert.DeserializeObject<UnitedCurrency>(unitedCurrency); });

            }
            catch (Exception ex)
            {
                throw ex;
            }
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
                request.ServiceName = ServiceNames.TRAVELERS.ToString();
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
