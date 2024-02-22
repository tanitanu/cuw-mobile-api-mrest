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
using United.Mobile.Model.Shopping.UnfinishedBooking;
using United.Mobile.Model.TripPlannerGetService;
using United.Mobile.Model.UnfinishedBooking;
using United.Mobile.Services.UnfinishedBooking.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;
using United.Mobile.Model.Common.FeatureSettings;
using MOBSHOPUnfinishedBookingRequestBase = United.Mobile.Model.Shopping.UnfinishedBooking.MOBSHOPUnfinishedBookingRequestBase;
using United.Mobile.Model.Common;

namespace United.Mobile.Services.UnfinishedBooking.Api.Controllers
{
    [ApiController]
    [Route("unfinishedbookingservice/api/")]
    public class UnfinishedBookingController : ControllerBase
    {

        private readonly ICacheLog<UnfinishedBookingController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUnfinishedBookingBusiness _unfinishedBookingBusiness;
        private readonly IApplicationEnricher _requestEnricher;
        private readonly IHeaders _headers;
        private readonly IFeatureSettings _featureSettings;
        public UnfinishedBookingController(ICacheLog<UnfinishedBookingController> logger
            , IConfiguration configuration
            , IUnfinishedBookingBusiness unfinishedBookingBusiness
            , IHeaders headers
            , IApplicationEnricher requestEnricher
            , IFeatureSettings featureSettings

            )
        {
            _logger = logger;
            _configuration = configuration;
            _unfinishedBookingBusiness = unfinishedBookingBusiness;
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
        [HttpPost]
        [Route("UnfinishedBooking/GetUnfinishedBookings")]
        public async Task<MOBSHOPGetUnfinishedBookingsResponse> GetUnfinishedBookings(MOBSHOPGetUnfinishedBookingsRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBSHOPGetUnfinishedBookingsResponse();
            IDisposable timer = null;
            try
            {
                _logger.LogInformation("GetUnfinishedBookings - ClientRequest {@ClientRequest}", JsonConvert.SerializeObject(request));

                using (timer = _logger.BeginTimedOperation("Total time taken for GetUnfinishedBookings business call", transationId: request.TransactionId))
                {
                    response = await _unfinishedBookingBusiness.GetUnfinishedBookings(request);
                    response.TransactionId = request.TransactionId;
                }
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("GetUnfinishedBookings Error {@UnitedException}", JsonConvert.SerializeObject(coex));

                response.Exception = new MOBException { Message = coex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError("GetUnfinishedBookings Error {@Exception}", JsonConvert.SerializeObject(ex));

                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }

            _logger.LogInformation("GetUnfinishedBookings {@ClientResponse}", JsonConvert.SerializeObject(response));

            return response;
        }

        [HttpPost]
        [Route("UnfinishedBooking/SelectUnfinishedBooking")]
        public async Task<SelectTripResponse> SelectUnfinishedBooking(MOBSHOPSelectUnfinishedBookingRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);

            var response = new SelectTripResponse();
            IDisposable timer = null;
            try
            {
                _logger.LogInformation("SelectUnfinishedBooking {@ClientRequest}", JsonConvert.SerializeObject(request));

                using (timer = _logger.BeginTimedOperation("Total time taken for SelectUnfinishedBooking business call", transationId: request.TransactionId))
                {
                    response = await _unfinishedBookingBusiness.SelectUnfinishedBooking(request, HttpContext);
                }
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("SelectUnfinishedBooking Error {@UnitedException}", JsonConvert.SerializeObject(coex));

                response.Exception = new MOBException { Message = coex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError("SelectUnfinishedBooking Error {@Exception}", JsonConvert.SerializeObject(ex));

                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", request.IsOmniCartSavedTrip ? _configuration.GetValue<string>("OmnicartExceptionMessage") : _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }

            _logger.LogInformation("SelectUnfinishedBooking {@ClientResponse}", JsonConvert.SerializeObject(response));

            return response;
        }

        [HttpPost]
        [Route("UnfinishedBooking/ClearUnfinishedBookings")]
        public async Task<MOBResponse> ClearUnfinishedBookings(MOBSHOPUnfinishedBookingRequestBase request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBResponse();
            IDisposable timer = null;
            try
            {
                _logger.LogInformation("ClearUnfinishedBookings {@ClientRequest}", JsonConvert.SerializeObject(request));

                using (timer = _logger.BeginTimedOperation("Total time taken for ClearUnfinishedBookings business call", transationId: request.TransactionId))
                {
                    response = await _unfinishedBookingBusiness.ClearUnfinishedBookings(request);
                }
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("ClearUnfinishedBookings Error {@UnitedException}", JsonConvert.SerializeObject(coex));

                response.Exception = new MOBException { Message = coex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError("ClearUnfinishedBookings Error {@Exception}", JsonConvert.SerializeObject(ex));

                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", request.IsOmniCartSavedTrip ? _configuration.GetValue<string>("OmnicartExceptionMessage") : _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }

            _logger.LogInformation("ClearUnfinishedBookings {@ClientResponse}", JsonConvert.SerializeObject(response));

            return response;
        }

        [HttpPost]
        [Route("UnfinishedBooking/SelectOmniCartSavedTrip")]
        public async Task<MOBSHOPSelectTripResponse> SelectOmniCartSavedTrip(MOBSHOPSelectUnfinishedBookingRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);

            IDisposable timer = null;
            var response = new MOBSHOPSelectTripResponse();
            var shoppingCart = new MOBShoppingCart();

            try
            {
                _logger.LogInformation("SelectOmniCartSavedTrip {@ClientRequest}", JsonConvert.SerializeObject(request));

                using (timer = _logger.BeginTimedOperation("Total time taken for SelectOmniCartSavedTrip business call", request.SessionId))
                {
                    response = await _unfinishedBookingBusiness.SelectOmniCartSavedTrip(request, HttpContext);
                }
            }
            catch (System.Net.WebException wex)
            {
                _logger.LogError("SelectOmniCartSavedTrip WebException {@WebException}", JsonConvert.SerializeObject(wex));
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("OmnicartExceptionMessage") ?? _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                response.Exception.ErrMessage = wex.Message;
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("SelectOmniCartSavedTrip UnitedException {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException();
                if (uaex != null && !string.IsNullOrEmpty(uaex.Message))
                {
                    response.Exception.Message = uaex.Message;
                }
                else
                {
                    response.Exception.Message = _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError("SelectOmniCartSavedTrip Error {@Exception}", JsonConvert.SerializeObject(ex));
                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("OmnicartExceptionMessage") ?? _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }
            response.CallDuration = 0;

            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }

            _logger.LogInformation("SelectOmniCartSavedTrip {@clientResponse}", JsonConvert.SerializeObject(response));

            return response;

        }

        [HttpPost]
        [Route("UnfinishedBooking/GetOmniCartSavedTrips")]
        public async Task<MOBGetOmniCartSavedTripsResponse> GetOmniCartSavedTrips(MOBSHOPUnfinishedBookingRequestBase request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);

            IDisposable timer = null;
            var response = new MOBGetOmniCartSavedTripsResponse();

            try
            {
                _logger.LogInformation("GetOmniCartSavedTrips response {@ClientRequest}", JsonConvert.SerializeObject(request));
                using (timer = _logger.BeginTimedOperation("Total time taken for GetOmniCartSavedTrips business call", request.SessionId))
                {
                    response = await _unfinishedBookingBusiness.GetOmniCartSavedTrips(request);
                }
            }
            catch (System.Net.WebException wex)
            {
                _logger.LogError("GetOmniCartSavedTrips WebException {@WebException}", JsonConvert.SerializeObject(wex));
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("OmnicartExceptionMessage") ?? _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                response.Exception.ErrMessage = wex.Message;
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetOmniCartSavedTrips MOBUnitedException {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }
            catch (System.Exception ex)
            {
                _logger.LogError("GetOmniCartSavedTrips Error {@Exception}", JsonConvert.SerializeObject(ex));
                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("OmnicartExceptionMessage") ?? _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }
            response.CallDuration = timer == null ? 0 : ((TimedOperation)timer).GetElapseTime();
            _logger.LogInformation("GetOmniCartSavedTrips response {@ClientResponse}", JsonConvert.SerializeObject(response));

            return response;
        }

        [HttpPost]
        [Route("UnfinishedBooking/RemoveOmniCartSavedTrip")]
        public async Task<MOBGetOmniCartSavedTripsResponse> RemoveOmniCartSavedTrip(MOBSHOPUnfinishedBookingRequestBase request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            IDisposable timer = null;

            var response = new MOBGetOmniCartSavedTripsResponse();
            try
            {
                _logger.LogInformation("RemoveOmniCartSavedTrip {@ClientRequest}", JsonConvert.SerializeObject(request));
                using (timer = _logger.BeginTimedOperation("Total time taken for RemoveOmniCartSavedTrip business call", request.SessionId))
                {
                    response = await _unfinishedBookingBusiness.RemoveOmniCartSavedTrip(request);
                }
            }
            catch (System.Net.WebException wex)
            {
                _logger.LogError("RemoveOmniCartSavedTrip WebException {@WebException}", JsonConvert.SerializeObject(wex));
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("OmnicartExceptionMessage") ?? _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                response.Exception.ErrMessage = wex.Message;
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("RemoveOmniCartSavedTrip MOBUnitedException {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;

            }
            catch (System.Exception ex)
            {
                _logger.LogError("RemoveOmniCartSavedTrip Error {@Exception}", JsonConvert.SerializeObject(ex));
                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("OmnicartExceptionMessage") ?? _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }
            response.CallDuration = timer == null ? 0 : ((TimedOperation)timer).GetElapseTime();
            _logger.LogInformation("RemoveOmniCartSavedTrip {@ClientResponse}", JsonConvert.SerializeObject(response));
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
                request.ServiceName = ServiceNames.UNFINISHEDBOOKING.ToString();
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