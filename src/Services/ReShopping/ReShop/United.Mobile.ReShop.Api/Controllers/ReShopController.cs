using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.EligibleCheck.Domain;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.ReShop;
using United.Mobile.Model.Shopping;
using United.Mobile.ReShop.Domain;
using United.Mobile.ReShop.Domain.GetProducts_CFOP;
using United.Mobile.ReshopSelectTrip.Domain;
using United.Mobile.ScheduleChange.Domain;
using United.Mobile.UpdateProfile.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model;

namespace United.Mobile.ReShop.Api.Controllers
{

    [Route("reshoppingservice/api")]
    [ApiController]
    public class ReShopController : ControllerBase
    {
        private readonly ICacheLog<ReShopController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly IReShoppingBusiness _reShoppingBusiness;
        private readonly IEligibleCheckBusiness _eligibleCheckBusiness;
        private readonly IReshopSelectTripBusiness _reshopSelectTripBusiness;
        private readonly IScheduleChangeBusiness _scheduleChangeBusiness;
        private readonly IUpdateProfileBusiness _updateProfileBusiness;
        private readonly IGetProducts_CFOPBusiness _getProducts_CFOPBusiness;
        private readonly IFeatureSettings _featureSettings;

        public ReShopController(ICacheLog<ReShopController> logger
            , IConfiguration configuration
            , IHeaders headers
            , IReShoppingBusiness reShoppingBusiness
            , IEligibleCheckBusiness eligibleCheckBusiness
            , IReshopSelectTripBusiness reshopSelectTripBusiness
            , IScheduleChangeBusiness scheduleChangeBusiness
            , IUpdateProfileBusiness updateProfileBusiness
            , IGetProducts_CFOPBusiness getProducts_CFOPBusiness
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _headers = headers;
            _reShoppingBusiness = reShoppingBusiness;
            _eligibleCheckBusiness = eligibleCheckBusiness;
            _reshopSelectTripBusiness = reshopSelectTripBusiness;
            _scheduleChangeBusiness = scheduleChangeBusiness;
            _updateProfileBusiness = updateProfileBusiness;
            _getProducts_CFOPBusiness = getProducts_CFOPBusiness;
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
        [HttpGet]
        [Route("EnumCheck")]
        public EnumCommon EnumCheck()
        {
            return new EnumCommon()
            {
                Title = "Testing Enum values",
                Genre = DisplayGendre.FEMALE
            };
        }

        [HttpPost("ReShopping/Reshop")]
        public async Task<ShopResponse> Reshop(MOBSHOPShopRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new ShopResponse();
            IDisposable timer = null;
            try
            {
                _logger.LogInformation("Reshop {@ClientRequest}", JsonConvert.SerializeObject(request));
                using (timer = _logger.BeginTimedOperation("Total time taken for Reshop business call", transationId: request.TransactionId))
                {
                    response = await _reShoppingBusiness.ReShop(request, HttpContext);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("Reshop Warning {@UnitedException}", JsonConvert.SerializeObject(uaex));

                //Calling regular shop call, if non-stop returned notflight found error code
                if (_configuration.GetValue<bool>("EnableNonStopFlight") && request.GetNonStopFlightsOnly && !request.GetFlightsWithStops && uaex.Code == "10038")
                {
                    request.GetNonStopFlightsOnly = false;
                    request.GetFlightsWithStops = false;
                    return await Reshop(request);
                }

                response.Exception = new MOBException
                {
                    Message = uaex.Message
                };
                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = uaex.Code;
                }

                //TODO-add uaex.Code=="500"
                //assigning right exception, if stop flights call returned no flights found in 2'nd shop call
                if (_configuration.GetValue<bool>("EnableNonStopFlight") && request.GetFlightsWithStops && uaex.Code == "10038")
                {
                    response.NoFlightsWithStops = true;
                    response.Exception.Message = _configuration.GetValue<string>("NoAvailabilityError2.0");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Reshop Error {@Exception}", JsonConvert.SerializeObject(ex));

                string[] messages = ex.Message.Split('#');

                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    if ((_configuration.GetValue<string>("Environment - ReShoppingPNRCall") == "STAGE") && messages.ToList().Count > 1)
                    {
                        response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage") + "CartId " + messages[1].ToString());
                    }
                    else
                    {
                        response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                    }
                }
                else
                {
                    response.Exception = new MOBException("9999", messages[0]);
                }
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("Reshop {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost("ReShopping/ReshopSelectTrip")]
        public async Task<SelectTripResponse> ReshopSelectTrip(SelectTripRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new SelectTripResponse();

            IDisposable timer = null;
            try
            {
                _logger.LogInformation("ReshopSelectTrip {@ClientRequest}", JsonConvert.SerializeObject(request));
                using (timer = _logger.BeginTimedOperation("Total time taken for ReshopSelectTrip business call", transationId: request.TransactionId))
                {
                    response = await _reshopSelectTripBusiness.SelectTrip(request, HttpContext);
                }

            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("ReshopSelectTrip Warning {@UnitedException}", JsonConvert.SerializeObject(uaex));

                response.Exception = new MOBException
                {
                    Message = uaex.Message
                };
                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = uaex.Code;
                }
                if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                {
                    if (uaex.InnerException != null && !string.IsNullOrEmpty(uaex.InnerException.Message) && uaex.InnerException.Message.Trim().Equals("10050"))
                    {
                        response.Exception.Code = "10050";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("ReshopSelectTrip Error {@Exception}", JsonConvert.SerializeObject(ex));

                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    if (request.BackButtonClick && request.Application.Version.Major == "2.1.9I" && ex.Message.ToLower().Trim() == "Object reference not set to an instance of an object.".ToLower().Trim())
                    {
                        string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4iOS2.1.9ITouchIDBackButtonBug") != null ? _configuration.GetValue<string>("BookingExceptionMessage4iOS2.1.9ITouchIDBackButtonBug").ToString() : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                        response.Exception = new MOBException("9999", expMessage);
                    }
                    else if (!GeneralHelper.IsVersion1Greater(request.Application.Version.Major, "2.1.8", true) && ex.Message.ToLower().Trim() == "Object reference not set to an instance of an object.".ToLower().Trim())
                    {
                        #region
                        bool isProductSelected = false;
                        //foreach (LogEntry logEntry in shopping.LogEntries)
                        //{
                        //    if (!string.IsNullOrEmpty(logEntry.Action) && (logEntry.Action.ToUpper().Trim().Equals("GetShopBookingDetails - Response for ShopBookingDetails".ToUpper().Trim())))
                        //    {
                        //        isProductSelected = true;
                        //        break;
                        //    }
                        //}
                        if (isProductSelected)
                        {
                            string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_2") != null ? _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_2").ToString() : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                            response.Exception = new MOBException("9999", expMessage);
                        }
                        else
                        {
                            string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1") != null ? _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1").ToString() : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                            response.Exception = new MOBException("9999", expMessage);
                        }
                        #endregion
                    }
                    else if (!GeneralHelper.IsVersion1Greater(request.Application.Version.Major, "2.1.8", true) &&
                    ex.Message.ToLower().Replace("\r\n", string.Empty).Trim() == "index was out of range. must be non-negative and less than the size of the collection.parameter name: index".ToLower().Trim())
                    {
                        string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1") != null ? _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1").ToString() : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                        response.Exception = new MOBException("9999", expMessage);
                    }
                    else
                    {
                        response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                    }
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("ReshopSelectTrip {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost("ReShopping/ChangeEligibleCheckAndReshop")]
        public async Task<MOBRESHOPChangeEligibilityResponse> ChangeEligibleCheckAndReshop(MOBRESHOPChangeEligibilityRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBRESHOPChangeEligibilityResponse();
            IDisposable timer = null;
            try
            {
                _logger.LogInformation("ChangeEligibleCheckAndReshop {@ClientRequest}", JsonConvert.SerializeObject(request));
                using (timer = _logger.BeginTimedOperation("Total time taken for ChangeEligibleCheckAndReshop business call", transationId: request.TransactionId))
                {
                    response = await _eligibleCheckBusiness.ChangeEligibleCheckAndReshop(request);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("ChangeEligibleCheckAndReshop Warning {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);

                if (uaex.Message == "USEDNOLOF")
                {
                    response.Exception = null;
                    response.PathEligible = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("ChangeEligibleCheckAndReshop Error {@Exception}", JsonConvert.SerializeObject(ex));
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            response.CallDuration = 0;
            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }
            _logger.LogInformation("ChangeEligibleCheckAndReshop {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost("ReShopping/ChangeEligibleCheck")]
        public async Task<MOBRESHOPChangeEligibilityResponse> ChangeEligibleCheck(MOBRESHOPChangeEligibilityRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBRESHOPChangeEligibilityResponse();

            IDisposable timer = null;
            try
            {
                _logger.LogInformation("ChangeEligibleCheck {@ClientRequest}", JsonConvert.SerializeObject(request));
                using (timer = _logger.BeginTimedOperation("Total time taken for ChangeEligibleCheck business call", transationId: request.TransactionId))
                {
                    response = await _eligibleCheckBusiness.ChangeEligibleCheck(request);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("ChangeEligibleCheck Warning {@UnitedException}", JsonConvert.SerializeObject(uaex));
                //_logger.LogWarning("ChangeEligibleCheck Error {@UnitedException} and {sessionId}", uaex.Message, request.SessionId);

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);

                if (uaex.Message == "USEDNOLOF")
                {
                    response.Exception = null;
                    response.PathEligible = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("ChangeEligibleCheck Error {@Exception}", JsonConvert.SerializeObject(ex));
                //_logger.LogError("ChangeEligibleCheck Error {exception} and {sessionId}", ex.Message, request.SessionId);

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            response.CallDuration = 0;
            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }
            _logger.LogInformation("ChangeEligibleCheck {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost("ReShopping/ConfirmScheduleChange")]
        public async Task<MOBConfirmScheduleChangeResponse> ConfirmScheduleChange(MOBConfirmScheduleChangeRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBConfirmScheduleChangeResponse(); 

            IDisposable timer = null;
            try
            {
                using (timer = _logger.BeginTimedOperation("Total time taken for ConfirmScheduleChange business call", transationId: request.TransactionId))
                {
                    _logger.LogInformation("ConfirmScheduleChange {@ConfirmScheduleChangeRequest}", JsonConvert.SerializeObject(request));
                    response = await _scheduleChangeBusiness.ConfirmScheduleChange(request);
                }
                    
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("ConfirmScheduleChange Warning {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), request.SessionId);
                _logger.LogWarning("ConfirmScheduleChange Error {@UnitedException} and {sessionId}", uaex.Message, request.SessionId);

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("PNRConfmScheduleChangeExcMessage"));
            }
            catch (Exception ex)
            {
                _logger.LogError("ConfirmScheduleChange Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), request.SessionId);
                _logger.LogError("ConfirmScheduleChange Error {exception} and {sessionId}", ex.Message, request.SessionId);

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("ConfirmScheduleChange {@clientResponse} and {SessionId}", JsonConvert.SerializeObject(response), request.SessionId);
            return response;
        }

        [HttpPost("ReShopping/ReshopSaveEmail_CFOP")]
        public async Task<MOBChangeEmailResponse> ReshopSaveEmail_CFOP(MOBChangeEmailRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBChangeEmailResponse();

            IDisposable timer = null;
            try
            {
                _logger.LogInformation("ReshopSaveEmail_CFOP {@ClientRequest}", JsonConvert.SerializeObject(request));
                using (timer = _logger.BeginTimedOperation("Total time taken for ReshopSaveEmail_CFOP business call", transationId: request.TransactionId))
                {
                    response = await _updateProfileBusiness.ReshopSaveEmail_CFOP(request);
                }
            }
            catch (MOBUnitedException uaex)
            {
                MOBException exception = new MOBException();
                if (uaex != null && !string.IsNullOrEmpty(uaex.Message))
                {
                    exception = new MOBException
                    {
                        Code = (string.IsNullOrEmpty(uaex.Code)) ? "9999" : uaex.Code.ToString(),
                        Message = uaex.Message.Trim()
                    };
                }
                else
                {
                    exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                response.Exception = exception;
                _logger.LogWarning("ReshopSaveEmail_CFOP Warning {@UnitedException}", JsonConvert.SerializeObject(uaex));
            }
            catch (Exception ex)
            {
                MOBException exception = new MOBException();
                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    exception = new MOBException("9999", ex.Message);
                }

                response.Exception = exception;
                _logger.LogError("ReshopSaveEmail_CFOP Error {@Exception}", JsonConvert.SerializeObject(ex));
            }

            response.CallDuration = 0;
            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }
            _logger.LogInformation("ReshopSaveEmail_CFOP {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost("ReShopping/ReshopSaveAddress_CFOP")]
        public async Task<MOBSHOPReservationResponse> ReshopSaveAddress_CFOP(MOBChangeAddressRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBSHOPReservationResponse();

            IDisposable timer = null;
            try
            {
                timer = _logger.BeginTimedOperation("Total time taken for ReshopSaveAddress_CFOP business call", transationId: request.TransactionId);
                {
                    _logger.LogInformation("ReshopSaveAddress_CFOP {@clientRequest} and {SessionId}", JsonConvert.SerializeObject(request), request.SessionId);
                    response = await _updateProfileBusiness.ReshopSaveAddress_CFOP(request);
                }
            }
            catch (MOBUnitedException uaex)
            {
                MOBException exception = new MOBException();
                if (uaex != null && !string.IsNullOrEmpty(uaex.Message))
                {
                    exception = new MOBException
                    {
                        Code = (string.IsNullOrEmpty(uaex.Code)) ? "9999" : uaex.Code.ToString(),
                        Message = uaex.Message.Trim()
                    };
                }
                else
                {
                    exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                response.Exception = exception;
                _logger.LogWarning("ReshopSaveAddress_CFOP Warning {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex.StackTrace), request.SessionId);
                _logger.LogWarning("ReshopSaveAddress_CFOP Error {@UnitedException} and {sessionId}", exception.Message, request.SessionId);
            }
            catch (Exception ex)
            {
                MOBException exception = new MOBException();
                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    exception = new MOBException("9999", ex.Message);
                }

                response.Exception = exception;
                _logger.LogError("ReshopSaveAddress_CFOP Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), request.SessionId);
                _logger.LogError("ReshopSaveAddress_CFOP Error {exception} and {sessionId}", ex.Message, request.SessionId);
            }

            response.CallDuration = 0;
            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }
            _logger.LogInformation("ReshopSaveAddress_CFOP {@clientResponse} and {SessionId}", JsonConvert.SerializeObject(response), request.SessionId);
            return response;
        }

        [HttpPost]
        public async Task<MOBRESHOPChangeEligibilityResponse> RetrievePNRandChangeEligibleCheck(MOBPNRByRecordLocatorRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBRESHOPChangeEligibilityResponse();

            return response;
        }


        [HttpPost("Shopping/GetProducts_CFOP")]
        public async Task<MOBSHOPProductSearchResponse> GetProducts_CFOP(MOBSHOPProductSearchRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBSHOPProductSearchResponse();

            IDisposable timer = null;
            try
            {
                _logger.LogInformation("GetProducts_CFOP {@ClientRequest}", JsonConvert.SerializeObject(request));
                using (timer = _logger.BeginTimedOperation("Total time taken for GetProducts_CFOP business call", transationId: request.TransactionId))
                {
                    response = await _getProducts_CFOPBusiness.GetProducts_CFOP(request);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetProducts_CFOP Warning {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;

            }
            catch (Exception ex)
            {
                _logger.LogError("GetProducts_CFOP Error {@Exception}", JsonConvert.SerializeObject(ex));

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                if (_configuration.GetValue<bool>("BugFixToggleFor17M") && _getProducts_CFOPBusiness.ValidateEPlusVersion(request.Application.Id, request.Application.Version.Major).Result)
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("EPlusGetproductsExceptionMessage"));
                }
                else if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message + " :: " + ex.StackTrace);
                }
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetProducts_CFOP {@ClientResponse}", JsonConvert.SerializeObject(response));
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
                request.ServiceName = ServiceNames.RESHOP.ToString();
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