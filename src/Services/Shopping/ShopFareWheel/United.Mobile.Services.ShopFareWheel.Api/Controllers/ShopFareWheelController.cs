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
using United.Mobile.Model.Shopping;
using United.Mobile.Services.ShopFareWheel.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model.Common;

namespace United.Mobile.Services.ShopFareWheel.Api.Controllers
{
    [Route("shopfarewheelservice/api")]
    [ApiController]
    public class ShopFareWheelController : ControllerBase
    {
        private readonly ICacheLog<ShopFareWheelController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IShopFareWheelBusiness _shopFareWheelBusiness;
        private readonly IHeaders _headers;
        private readonly IApplicationEnricher _requestEnricher;
        private readonly IFeatureSettings _featureSettings;

        public ShopFareWheelController(ICacheLog<ShopFareWheelController> logger, IConfiguration configuration, IShopFareWheelBusiness shopFareWheelBusiness, IHeaders headers, IApplicationEnricher requestEnricher, IFeatureSettings featureSettings)
        {
            _logger = logger;
            _shopFareWheelBusiness = shopFareWheelBusiness;
            _configuration = configuration;            
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
        [HttpPost("Shopping/GetShopFareWheelList")]
        public async Task<FareWheelResponse> GetShopFareWheelList(MOBSHOPShopRequest shopRequest)
        {
            var response = new FareWheelResponse();
            try
            {
                await _headers.SetHttpHeader(shopRequest.DeviceId, shopRequest.Application?.Id.ToString(), shopRequest.Application?.Version?.Major, shopRequest.TransactionId, shopRequest.LanguageCode, shopRequest.SessionId);
                
                _logger.LogInformation("GetShopFareWheelList {@ClientRequest}", JsonConvert.SerializeObject(shopRequest));

                    response = await _shopFareWheelBusiness.GetShopFareWheelListResponse(shopRequest);
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetShopFareWheelList Error {MOBUnitedException}", JsonConvert.SerializeObject(uaex));
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
                _logger.LogError("GetShopFareWheelList Error {@Exception}", JsonConvert.SerializeObject(ex));
                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }
            _logger.LogInformation("GetShopFareWheelList {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost("Shopping/GetFareWheelList")]
        public async Task<FareWheelResponse> GetFareWheelList(SelectTripRequest selectTripRequest)
        {
            var response = new FareWheelResponse();
            try
            {
                await _headers.SetHttpHeader(selectTripRequest.DeviceId, selectTripRequest.Application?.Id.ToString(), selectTripRequest.Application?.Version?.Major, selectTripRequest.TransactionId, selectTripRequest.LanguageCode, selectTripRequest.SessionId);
               
                _logger.LogInformation("GetFareWheelList {@ClientRequest}", JsonConvert.SerializeObject(selectTripRequest));

                    response = await _shopFareWheelBusiness.GetFareWheelListResponse(selectTripRequest);
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetFareWheelList Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException
                {
                    Message = uaex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("GetFareWheelList Error {@Exception}", JsonConvert.SerializeObject(ex));

                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            _logger.LogInformation("GetFareWheelList {@ClientResponse}", JsonConvert.SerializeObject(response));
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
                request.ServiceName = ServiceNames.SHOPFAREWHEEL.ToString();
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
