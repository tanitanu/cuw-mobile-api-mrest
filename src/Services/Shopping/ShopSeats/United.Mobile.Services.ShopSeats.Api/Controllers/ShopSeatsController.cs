using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Services.ShopSeats.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;

namespace United.Mobile.Services.ShopSeats.Api.Controllers
{
    [Route("shopseatsservice/api")]
    [ApiController]

    public class ShopSeatsController : ControllerBase
    {

        private readonly ICacheLog<ShopSeatsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IShopSeatsBusiness _shopSeatsBusiness;
        private readonly IHeaders _headers;
        private readonly IFeatureSettings _featureSettings;
        public ShopSeatsController(ICacheLog<ShopSeatsController> logger, IConfiguration configuration, IShopSeatsBusiness shopSeatsBusiness, IHeaders headers, IFeatureSettings featureSettings)
        {
            _logger = logger;
            _shopSeatsBusiness = shopSeatsBusiness;
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
        [HttpPost("Shopping/SelectSeats")]
        public async Task<SelectSeatsResponse> SelectSeats(SelectSeatsRequest selectSeatsRequest)
        {
            var response = new SelectSeatsResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(selectSeatsRequest.DeviceId, selectSeatsRequest.Application.Id.ToString(), selectSeatsRequest.Application.Version.Major, selectSeatsRequest.TransactionId, selectSeatsRequest.LanguageCode, selectSeatsRequest.SessionId);
                _logger.LogInformation("SelectSeats {@ClientRequest}", JsonConvert.SerializeObject(selectSeatsRequest));
                using (timer= _logger.BeginTimedOperation("Total time taken for SelectSeats business call", transationId: selectSeatsRequest.TransactionId))
                {
                    response = await _shopSeatsBusiness.SelectSeats(selectSeatsRequest);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("SelectSeats Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("SelectSeats Error {@Exception}", JsonConvert.SerializeObject(ex));
                ///Bug 215283 : Eplus:mApp:Scenario2: incorrect "Load Seats Exception "content is displayed
                if (_configuration.GetValue<bool>("BugFixToggleFor17M") && _shopSeatsBusiness.ValidateEPlusVersion(selectSeatsRequest.Application.Id, selectSeatsRequest.Application.Version.Major).Result)
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("EPlusSelectSeatsErrormsg"));
                }
                else if (_configuration.GetValue<string>("Booking2OGenericExceptionMessage") != null)
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("SelectSeats {@ClientResponse}", JsonConvert.SerializeObject(response));
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
                request.ServiceName = ServiceNames.SHOPSEATS.ToString();
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
