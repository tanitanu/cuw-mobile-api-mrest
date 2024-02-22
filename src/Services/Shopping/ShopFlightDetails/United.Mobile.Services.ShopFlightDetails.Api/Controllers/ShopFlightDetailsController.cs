using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Ebs.Logging.Enrichers;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.TeaserPage;
using United.Mobile.Services.ShopFlightDetails.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;

namespace United.Mobile.Services.ShopFlightDetails.Api.Controllers
{
    [Route("shopflightdetailsservice/api")]
    [ApiController]
    public class ShopFlightDetailsController : ControllerBase
    {
        private readonly ICacheLog<ShopFlightDetailsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IShopFlightDetailsBusiness _shopFlightDetailsBusiness;
        private readonly IHeaders _headers;
        private readonly IApplicationEnricher _requestEnricher;
        private readonly ITeaserPageBusiness _teaserpageBusiness;
        private readonly IFeatureSettings _featureSettings;
        public ShopFlightDetailsController(ICacheLog<ShopFlightDetailsController> logger, IConfiguration configuration, IHeaders headers, IApplicationEnricher requestEnricher, IShopFlightDetailsBusiness shopFlightDetailsBusiness,
            ITeaserPageBusiness teaserpageBusiness,IFeatureSettings featureSettings)
        {
            _logger = logger;
            _shopFlightDetailsBusiness = shopFlightDetailsBusiness;
            _headers = headers;
            _configuration = configuration;
            _requestEnricher = requestEnricher;
            _teaserpageBusiness = teaserpageBusiness;
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
        [HttpPost("Shopping/GetONTimePerformence")]
        public async Task<OnTimePerformanceResponse> GetONTimePerformence(OnTimePerformanceRequest onTimePerformanceRequest)
        {
            await _headers.SetHttpHeader(onTimePerformanceRequest.DeviceId, onTimePerformanceRequest.Application.Id.ToString(), onTimePerformanceRequest.Application.Version.Major, onTimePerformanceRequest.TransactionId, onTimePerformanceRequest.LanguageCode, onTimePerformanceRequest.SessionId);
            var response = new OnTimePerformanceResponse();
            IDisposable timer = null;
            try
            {
                _logger.LogInformation("GetONTimePerformence {@ClientRequest}", JsonConvert.SerializeObject(onTimePerformanceRequest));
                using (timer = _logger.BeginTimedOperation("Total time taken for GetONTimePerformence business call", transationId: onTimePerformanceRequest.TransactionId))
                {
                    response = await _shopFlightDetailsBusiness.GetONTimePerformence(onTimePerformanceRequest);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetONTimePerformence Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetONTimePerformence Error {@Exception}", JsonConvert.SerializeObject(ex));

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetONTimePerformence {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost]
        [Route("Shopping/GetTeaserPage")]
        public async Task<MOBSHOPShoppingTeaserPageResponse> GetTeaserPage(MOBSHOPShoppingTeaserPageRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionID);
            MOBSHOPShoppingTeaserPageResponse response = new MOBSHOPShoppingTeaserPageResponse();
            IDisposable timer = null;
            try
            {
                _logger.LogInformation("GetTeaserPage {@ClientRequest}", JsonConvert.SerializeObject(request));

                using (timer = _logger.BeginTimedOperation("Total time taken for GetTeaserPage business call", transationId: request.TransactionId))
                {
                    response = await _teaserpageBusiness.GetTeaserPage(request, HttpContext);
                }
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("GetTeaserPage Error {@UnitedException}", JsonConvert.SerializeObject(coex));

                response.Exception = new MOBException { Message = coex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError("GetTeaserPage Error {@Exception}", JsonConvert.SerializeObject(ex));

                response.Exception = new MOBException("9999", ex.Message);

            }
            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }
            _logger.LogInformation("GetTeaserPage {@ClientResponse}", JsonConvert.SerializeObject(response));

            return response;

        }
        [HttpGet]
        [Route("Shopping/RefreshCacheForSDLContent")]
        public async Task<RefreshCacheForSDLContentResponse> RefreshCacheForSDLContent(string groupName , string cacheKey)
        {
            RefreshCacheForSDLContentResponse response = new RefreshCacheForSDLContentResponse();
            try
            {
                _logger.LogInformation("RefreshCacheForSDLContent {@groupName} {@cacheKey}", groupName, cacheKey);
                if (!string.IsNullOrEmpty(groupName) && !string.IsNullOrEmpty(cacheKey))
                {
                    response = await _shopFlightDetailsBusiness.RefreshCacheForSDLContent(groupName,cacheKey);
                }
                else
                {
                    throw new Exception("RefreshCacheForSDLContent url is missing one of the properties- GroupName/CacheKey");
                }
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("RefreshCacheForSDLContent Error {@UnitedException}", JsonConvert.SerializeObject(coex));

                response.Exception = new MOBException { Message = coex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError("RefreshCacheForSDLContent Error {@Exception}", JsonConvert.SerializeObject(ex));

                response.Exception = new MOBException("9999", ex.Message);

            }
            _logger.LogInformation("RefreshCacheForSDLContent {@ClientResponse}", JsonConvert.SerializeObject(response));

            return response;
        }
        #region FeatureSettings related methods
        [HttpPost("RefreshAllContainerFeatureSettingsCache")]
        public async Task<MOBGetContainerIPAddressesByServiceResponse> RefreshAllContainerFeatureSettingsCache(MOBFeatureSettingsCacheRequest request)
        {
            MOBGetContainerIPAddressesByServiceResponse response = new MOBGetContainerIPAddressesByServiceResponse();
            try
            {
                response = await _featureSettings.RefreshAllContainerFeatureSettingsCache(ServiceNames.SHOPFLIGHTDETAILS.ToString(), "shopflightdetailsservice", request);
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("RefreshRetrieveAllFeatureSettings warning {@UnitedException}", JsonConvert.SerializeObject(coex));
                response.Exception = new MOBException { Message = coex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError("RefreshRetrieveAllFeatureSettings Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), "RefreshRetrieveAllFeatureSettings_TransId");
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            return response;
        }
        [HttpPost("RefreshFeatureSettingCache")]
        public async Task<MOBResponse> RefreshFeatureSettingCache(MOBFeatureSettingsCacheRequest request)
        {
            MOBResponse response = new MOBResponse();
            try
            {
                request.ServiceName = ServiceNames.SHOPFLIGHTDETAILS.ToString();
                await _featureSettings.RefreshFeatureSettingCache(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("RefreshFeatureSettingCache Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), "RefreshRetrieveAllFeatureSettings_TransId");
                response.Exception = new MOBException("9999", JsonConvert.SerializeObject(ex));
            }
            return response;
        }
        [HttpPost("GetAllContainerFeatureSettings")]
        public async Task<MOBGetAllContainerFeatureSettingsResponse> GetAllContainerFeatureSettings(MOBFeatureSettingsCacheRequest request)
        {
            MOBGetAllContainerFeatureSettingsResponse response = new MOBGetAllContainerFeatureSettingsResponse();
            try
            {
                response = await _featureSettings.GetAllContainerFeatureSettings(request, Model.Common.ServiceNames.SHOPFLIGHTDETAILS.ToString(), "shoppingservice");
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllContainerFeatureSettings Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), "GetAllContainerFeatureSettings_TransId");
                response.Exception = new MOBException("9999", JsonConvert.SerializeObject(ex));
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
        #endregion
    }
}
