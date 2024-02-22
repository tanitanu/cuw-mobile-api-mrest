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
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.AwardCalendar;
using United.Mobile.Services.ShopAward.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;
using Constants = United.Mobile.Model.Constants;


namespace United.Mobile.Services.ShopAward.Api.Controllers
{
    [Route("shopawardservice/api")]
    [ApiController]
    public class ShopAwardController : ControllerBase
    {
        private readonly ICacheLog<ShopAwardController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IShopAwardBusiness _shopAwardBusiness;
        private readonly IHeaders _headers;
        private readonly IApplicationEnricher _requestEnricher;
        private readonly IFeatureSettings _featureSettings;
        public ShopAwardController(ICacheLog<ShopAwardController> logger, IConfiguration configuration, IShopAwardBusiness shopAwardBusiness, IHeaders headers, IApplicationEnricher requestEnricher, IFeatureSettings featureSettings)
        {
            _logger = logger;
            _shopAwardBusiness = shopAwardBusiness;
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
        [HttpPost("Shopping/RevenueLowestPriceForAwardSearch")]
        public async Task<RevenueLowestPriceForAwardSearchResponse> RevenueLowestPriceForAwardSearch(MOBSHOPShopRequest shopRequest)
        {
            var response = new RevenueLowestPriceForAwardSearchResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(shopRequest.DeviceId, shopRequest.Application?.Id.ToString(), shopRequest.Application?.Version?.Major, shopRequest.TransactionId, shopRequest.LanguageCode, shopRequest.SessionId);
                using (timer = _logger.BeginTimedOperation("Total time taken for RevenueLowestPriceForAwardSearch business call", transationId: shopRequest.TransactionId))
                {
                    response = await _shopAwardBusiness.RevenueLowestPriceForAwardSearch(shopRequest);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("RevenueLowestPriceForAwardSearch Warning {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), shopRequest.SessionId);
                _logger.LogWarning("RevenueLowestPriceForAwardSearch Error {@UnitedException} and {sessionId}", uaex.Message, shopRequest.SessionId);
                //return TopHelper.ErrorHandling<ShopResponse>(response, errorCode, uaex.Message);
                //TODO
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("RevenueLowestPriceForAwardSearch Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), shopRequest.SessionId);
                _logger.LogError("RevenueLowestPriceForAwardSearch Error {exception} and {sessionId}", ex.Message, shopRequest.SessionId);
                //return TopHelper.ExceptionResponse<RevenueLowestPriceForAwardSearchResponse>(response, HttpContext, _configuration);
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("RevenueLowestPriceForAwardSearch {Response} and {SessionID}", JsonConvert.SerializeObject(response), shopRequest.SessionId);
            return response;
        }

        [HttpPost("Shopping/GetSelectTripAwardCalendar")]
        public async Task<AwardCalendarResponse> GetSelectTripAwardCalendar(SelectTripRequest selectTripRequest)
        {
            var response = new AwardCalendarResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(selectTripRequest.DeviceId, selectTripRequest.Application?.Id.ToString(), selectTripRequest.Application?.Version?.Major, selectTripRequest.TransactionId, selectTripRequest.LanguageCode, selectTripRequest.SessionId);
                
                _logger.LogInformation("GetSelectTripAwardCalendar {@ClientRequest}", JsonConvert.SerializeObject(selectTripRequest));
                using (timer = _logger.BeginTimedOperation("Total time taken for GetSelectTripAwardCalendar business call", transationId: selectTripRequest.TransactionId))
                {
                    response = await _shopAwardBusiness.GetSelectTripAwardCalendar(selectTripRequest);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetSelectTripAwardCalendar Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetSelectTripAwardCalendar Error {@Exception}",JsonConvert.SerializeObject(ex));

                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetSelectTripAwardCalendar {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost("Shopping/GetShopAwardCalendar")]
        public async Task<AwardCalendarResponse> GetShopAwardCalendar(MOBSHOPShopRequest shopRequest)
        {
            var response = new AwardCalendarResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(shopRequest.DeviceId, shopRequest.Application?.Id.ToString(), shopRequest.Application?.Version?.Major, shopRequest.TransactionId, shopRequest.LanguageCode, shopRequest.SessionId);

                _logger.LogInformation("GetShopAwardCalendar {@ClientRequest}", JsonConvert.SerializeObject(shopRequest));
                using (timer = _logger.BeginTimedOperation("Total time taken for GetShopAwardCalendar business call", transationId: shopRequest.TransactionId))
                {
                    response = await _shopAwardBusiness.GetShopAwardCalendar(shopRequest);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetShopAwardCalendar Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = await _shopAwardBusiness.GetAwardCalendarExceptionMessage(uaex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetShopAwardCalendar Error {@Exception}",JsonConvert.SerializeObject(ex));

                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetShopAwardCalendar {@ClientResponse}", JsonConvert.SerializeObject(response));
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
                request.ServiceName = ServiceNames.SHOPAWARD.ToString();
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
