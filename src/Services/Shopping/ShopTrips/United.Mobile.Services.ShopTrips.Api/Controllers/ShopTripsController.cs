using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Ebs.Logging.Enrichers;
using United.Mobile.Model;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Services.ShopTrips.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model.Common;
using United.Mobile.Model.Travelers;

namespace United.Mobile.Services.ShopTrips.Api.Controllers
{
    [Route("shoptripsservice/api")]
    [ApiController]
    public class ShopTripsController : ControllerBase
    {
        private readonly ICacheLog<ShopTripsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IShopTripsBusiness _shopTripsBusiness;
        private readonly IHeaders _headers;
        private readonly IApplicationEnricher _requestEnricher;
        private readonly IFeatureSettings _featureSettings;
        public ShopTripsController(ICacheLog<ShopTripsController> logger, IConfiguration configuration, IShopTripsBusiness shopTripsBusiness, IHeaders headers, IApplicationEnricher requestEnricher, IFeatureSettings featureSettings)
        {
            _logger = logger;
            _shopTripsBusiness = shopTripsBusiness;
            _configuration = configuration;
            _headers = headers;
            _requestEnricher = requestEnricher;
            _featureSettings = featureSettings;
        }

        [HttpGet("HealthCheck")]
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
        [HttpPost("Shopping/GetTripCompareFareTypes")]
        public async Task<ShoppingTripFareTypeDetailsResponse> GetTripCompareFareTypes(ShoppingTripFareTypeDetailsRequest shoppingTripFareTypeDetailsRequest)
        {
            var response = new ShoppingTripFareTypeDetailsResponse();
            IDisposable timer = null;

            try
            {
                await _headers.SetHttpHeader(shoppingTripFareTypeDetailsRequest.DeviceId, shoppingTripFareTypeDetailsRequest.Application?.Id.ToString(), shoppingTripFareTypeDetailsRequest.Application?.Version?.Major, shoppingTripFareTypeDetailsRequest.TransactionId, shoppingTripFareTypeDetailsRequest.LanguageCode, shoppingTripFareTypeDetailsRequest.SessionId);

                using (timer = _logger.BeginTimedOperation("Total time taken for GetTripCompareFareTypes business call", transationId: shoppingTripFareTypeDetailsRequest.TransactionId))
                {
                    response = await _shopTripsBusiness.GetTripCompareFareTypes(shoppingTripFareTypeDetailsRequest);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetTripCompareFareTypes Warning {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), shoppingTripFareTypeDetailsRequest.SessionId);
                _logger.LogWarning("GetTripCompareFareTypes Error {@UnitedException} and {sessionId}", uaex.Message, shoppingTripFareTypeDetailsRequest.SessionId);
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetTripCompareFareTypes Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), shoppingTripFareTypeDetailsRequest.SessionId);
                _logger.LogError("GetTripCompareFareTypes Error {exception} and {sessionId}", ex.Message, shoppingTripFareTypeDetailsRequest.SessionId);

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetTripCompareFareTypes {@clientResponse} {sessionId}", JsonConvert.SerializeObject(response), shoppingTripFareTypeDetailsRequest.SessionId);
            return response;
        }

        [HttpPost("Shopping/GetShareTrip")]
        public async Task<TripShare> GetShareTrip(ShareTripRequest shareTripRequest)
        {
            var response = new TripShare();
            IDisposable timer = null;

            try
            {
                await _headers.SetHttpHeader(shareTripRequest.DeviceId, shareTripRequest.Application?.Id.ToString(), shareTripRequest.Application?.Version?.Major, shareTripRequest.TransactionId, shareTripRequest.LanguageCode, shareTripRequest.SessionId);               

                _logger.LogInformation("GetShareTrip {@ClientRequest}", JsonConvert.SerializeObject(shareTripRequest));

                using (timer = _logger.BeginTimedOperation("Total time taken for GetShareTrip business call", transationId: shareTripRequest.TransactionId))
                {
                    response = await _shopTripsBusiness.GetShareTrip(shareTripRequest);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetShareTrip Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetShareTrip Error {@Exception}", JsonConvert.SerializeObject(ex));
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            response.TransactionId = shareTripRequest.TransactionId;
            _logger.LogInformation("GetShareTrip {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost("Shopping/MetaSelectTrip")]
        public async Task<SelectTripResponse> MetaSelectTrip(MetaSelectTripRequest metaSelectTripRequest)
        {
            var response = new SelectTripResponse();
            IDisposable timer = null;

            try
            {
                await _headers.SetHttpHeader(metaSelectTripRequest.DeviceId, metaSelectTripRequest.Application?.Id.ToString(), metaSelectTripRequest.Application?.Version.Major, metaSelectTripRequest.TransactionId, metaSelectTripRequest.LanguageCode, metaSelectTripRequest.SessionId);

                _logger.LogInformation("MetaSelectTrip {@ClientRequest}", JsonConvert.SerializeObject(metaSelectTripRequest));
                using (timer = _logger.BeginTimedOperation("Total time taken for MetaSelectTrip business call", transationId: metaSelectTripRequest.TransactionId))
                {
                    response = await _shopTripsBusiness.MetaSelectTrip(metaSelectTripRequest, HttpContext);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("MetaSelectTrip Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException { Message = uaex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError("MetaSelectTrip Error {@Exception}", JsonConvert.SerializeObject(ex));
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("MetaSelectTrip {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost("Shopping/GetFareRulesForSelectedTrip")]
        public async Task<FareRulesResponse> GetFareRulesForSelectedTrip(GetFareRulesRequest getFareRulesRequest)
        {
            var response = new FareRulesResponse();
            IDisposable timer = null;

            try
            {
                await _headers.SetHttpHeader(getFareRulesRequest.DeviceId, getFareRulesRequest.Application?.Id.ToString(), getFareRulesRequest.Application?.Version.Major, getFareRulesRequest.TransactionId, getFareRulesRequest.LanguageCode, getFareRulesRequest.SessionId);

                using (timer = _logger.BeginTimedOperation("Total time taken for GetFareRulesForSelectedTrip business call", transationId: getFareRulesRequest.TransactionId))
                {
                    response = await _shopTripsBusiness.GetFareRulesForSelectedTrip(getFareRulesRequest);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetFareRulesForSelectedTrip Error {@UnitedException}", JsonConvert.SerializeObject(uaex));

                if (uaex != null && !string.IsNullOrEmpty(uaex.Message.Trim()))
                {
                    response.Exception = new MOBException();
                    response.Exception.Message = uaex.Message;
                    if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                    {
                        response.Exception.Code = uaex.Code;
                    }
                }
                else
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetFareRulesForSelectedTrip Error {@Exception}", JsonConvert.SerializeObject(ex));
                if (_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetFareRulesForSelectedTrip {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }
        [HttpPost]
        [Route("Shopping/RepriceForAddTravelers")]
        public async Task<MOBRegisterTravelersResponse> RepriceForAddTravelers(MOBAddTravelersRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            MOBRegisterTravelersResponse response = new MOBRegisterTravelersResponse();
            IDisposable timer = null;

            try
            {
                _logger.LogInformation("RepriceForAddTravelers {@ClientRequest}", JsonConvert.SerializeObject(request));
                using (timer = _logger.BeginTimedOperation("Total time taken for AddTravelers_Reprice business call", transationId: request.TransactionId))
                {
                    response = await _shopTripsBusiness.RepriceForAddTravelers(request, HttpContext);
                }
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("RepriceForAddTravelers Warning {@UnitedException}", JsonConvert.SerializeObject(coex));
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
                _logger.LogError("RepriceForAddTravelers Error {@Exception}", JsonConvert.SerializeObject(ex));

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
            _logger.LogInformation("RepriceForAddTravelers {@ClientResponse}", JsonConvert.SerializeObject(response));
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
                request.ServiceName = ServiceNames.SHOPTRIPS.ToString();
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