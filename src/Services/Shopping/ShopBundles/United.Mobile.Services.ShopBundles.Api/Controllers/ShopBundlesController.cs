using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.Model;
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping.Bundles;
using United.Mobile.Services.ShopBundles.Domain;
using United.Utility.Helper;

namespace United.Mobile.Services.ShopBundles.Api.Controllers
{
    [Route("shopbundlesservice/api")]
    [ApiController]
    public class ShopBundlesController : ControllerBase
    {
        private readonly ICacheLog<ShopBundlesController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IShopBundlesBusiness _shopBundlesBusiness;
        private readonly IHeaders _headers;
        private readonly IFeatureSettings _featureSettings;

        public ShopBundlesController(ICacheLog<ShopBundlesController> logger, IConfiguration configuration, IHeaders headers, IShopBundlesBusiness shopBundlesBusiness, IFeatureSettings featureSettings)
        {
            _logger = logger;
            _shopBundlesBusiness = shopBundlesBusiness;
            _configuration = configuration;
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
        [HttpPost("Shopping/GetBundles_CFOP")]
        public async Task<BookingBundlesResponse> GetBundles_CFOP(BookingBundlesRequest bookingBundlesRequest)
        {
            var response = new BookingBundlesResponse(_configuration);           
            try
            {
                await _headers.SetHttpHeader(bookingBundlesRequest.DeviceId, bookingBundlesRequest.Application.Id.ToString(), bookingBundlesRequest.Application.Version.Major, bookingBundlesRequest.TransactionId, bookingBundlesRequest.LanguageCode, bookingBundlesRequest.SessionId);

                response.TransactionId = bookingBundlesRequest.TransactionId;
                response.LanguageCode = bookingBundlesRequest.LanguageCode;
                _logger.LogInformation("GetBundles_CFOP {@ClientRequest}", JsonConvert.SerializeObject(bookingBundlesRequest));

                using (_logger.BeginTimedOperation("Total time taken for Shop Bundles business call", transationId: bookingBundlesRequest.TransactionId))
                {
                    response = await _shopBundlesBusiness.GetBundles_CFOP(bookingBundlesRequest);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetBundles_CFOP Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;

                if (!_configuration.GetValue<bool>("DisableSetBundleResponseSessionIdWhenEmptyChanges") && string.IsNullOrEmpty(response.SessionId))
                {
                    response.SessionId = bookingBundlesRequest.SessionId;
                }
            }
            catch (Exception ex)
            {
                if(!_configuration.GetValue<bool>("DisableByPassThrowingClientError"))
                {
                    MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                    _logger.LogError("GetBundles_CFOP Error {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));
                }           
                else if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message + " :: " + ex.StackTrace);
                }
                if (!_configuration.GetValue<bool>("DisableSetBundleResponseSessionIdWhenEmptyChanges") && string.IsNullOrEmpty(response.SessionId))
                {
                    response.SessionId = bookingBundlesRequest.SessionId;
                }

            }

            _logger.LogInformation("GetBundles_CFOP {@ClientResponse}", JsonConvert.SerializeObject(response));
           
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
                request.ServiceName = United.Mobile.Model.Common.ServiceNames.SHOPBUNDLES.ToString();
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
