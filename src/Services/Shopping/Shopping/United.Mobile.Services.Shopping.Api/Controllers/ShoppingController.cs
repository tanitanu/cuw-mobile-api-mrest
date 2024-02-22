using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Ebs.Logging.Enrichers;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Common;
using United.Mobile.Model.Shopping.Common.MoneyPlusMiles;
using United.Mobile.Services.Shopping.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;
using Constants = United.Mobile.Model.Constants;

namespace United.Mobile.Services.Shopping.Api.Controllers
{
    [Route("shoppingservice/api")]
    [ApiController]
    public class ShoppingController : ControllerBase
    {
        private readonly ICacheLog<ShoppingController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IShoppingBusiness _shoppingBusiness;
        private readonly IShopProductsBusiness _shopProductsBusiness;
        private readonly IShopMileagePricingBusiness _shopMileagePricingBusiness;
        private readonly IHeaders _headers;
        private readonly IApplicationEnricher _requestEnricher;
        private readonly IFeatureSettings _featureSettings;


        public ShoppingController(ICacheLog<ShoppingController> logger
            , IConfiguration configuration
            , IShoppingBusiness shoppingBusiness
            , IShopProductsBusiness shopProductsBusiness
            , IHeaders headers
            , IApplicationEnricher requestEnricher
            , IFeatureSettings featureSettings
            , IShopMileagePricingBusiness shopMileagePricingBusiness
            )
        {
            _logger = logger;
            _shoppingBusiness = shoppingBusiness;
            _shopProductsBusiness = shopProductsBusiness;
            _configuration = configuration;
            _headers = headers;
            _requestEnricher = requestEnricher;
            _featureSettings = featureSettings;
            _shopMileagePricingBusiness = shopMileagePricingBusiness;
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

        [HttpPost("Shopping/shop")]
        public async Task<ShopResponse> Shop(MOBSHOPShopRequest request)
        {
            var response = new ShopResponse();

            try
            {
                await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);

                // Add Request Enrichers that would be propagated to all the logs within the same service

                _logger.LogInformation("Shop {@ClientRequest}", JsonConvert.SerializeObject(request));

                response = await _shoppingBusiness.GetShop(request, HttpContext);
            }

            #region 
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("Shop Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                //_cacheLog.LogWarning("Shop Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                //Calling regular shop call, if non-stop returned notflight found error code
                if (_configuration.GetValue<bool>("EnableNonStopFlight") && request.GetNonStopFlightsOnly && !request.GetFlightsWithStops && uaex.Message == "10038")
                {
                    request.GetNonStopFlightsOnly = false;
                    request.GetFlightsWithStops = false;
                    return await Shop(request);
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
                _logger.LogError("Shop Error {@Exception}", JsonConvert.SerializeObject(ex));
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
            #endregion

            _logger.LogInformation("Shop {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost("Shopping/ShopCLBOptOut")]
        public async Task<ShopResponse> ShopCLBOptOut(CLBOptOutRequest cLBOptOutRequest)
        {
            var response = new ShopResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(cLBOptOutRequest.DeviceId, cLBOptOutRequest.Application.Id.ToString(), cLBOptOutRequest.Application.Version.Major, cLBOptOutRequest.TransactionId, cLBOptOutRequest.LanguageCode, cLBOptOutRequest.SessionId);

                _logger.LogInformation("ShopCLBOptOut {@ClientRequest}", JsonConvert.SerializeObject(cLBOptOutRequest));

                using (timer = _logger.BeginTimedOperation("Total time taken for Shopping business call", transationId: cLBOptOutRequest?.TransactionId))
                {
                    response = await _shoppingBusiness.ShopCLBOptOut(cLBOptOutRequest);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("ShopCLBOptOut Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                int errorCode = string.IsNullOrEmpty(uaex.Code) ? 400 : Convert.ToInt32(uaex.Code);
                // return TopHelper.ErrorHandling<ShopResponse>(response, errorCode, uaex.Message);//TODO
            }
            catch (Exception ex)
            {
                _logger.LogError("ShopCLBOptOut Error {@Exception}", JsonConvert.SerializeObject(ex));
                return TopHelper.ExceptionResponse<ShopResponse>(response, HttpContext, _configuration);
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("ShopCLBOptOut {@clientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost("Shopping/OrganizeShopResults")]
        public async Task<ShopOrganizeResultsResponse> OrganizeShopResults(ShopOrganizeResultsReqeust organizeResultsReqeust)
        {
            var response = new ShopOrganizeResultsResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(organizeResultsReqeust.DeviceId, organizeResultsReqeust.Application.Id.ToString(), organizeResultsReqeust.Application.Version.Major, organizeResultsReqeust.TransactionId, organizeResultsReqeust.LanguageCode, organizeResultsReqeust.SessionId);

                _logger.LogInformation("OrganizeShopResults {@ClientRequest}", JsonConvert.SerializeObject(organizeResultsReqeust));
                using (timer = _logger.BeginTimedOperation("Total time taken for OrganizeShopResults business call", transationId: organizeResultsReqeust.SessionId))
                {
                    response = await _shoppingBusiness.OrganizeShopResults(organizeResultsReqeust);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("OrganizeShopResults Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException { Message = uaex.Message };
                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = uaex.Code;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("OrganizeShopResults Error {@Exception}", JsonConvert.SerializeObject(ex));
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
            _logger.LogInformation("OrganizeShopResults {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost("Shopping/GetShopRequest")]
        public async Task<TripShareV2Response> GetShopRequest(ShareTripRequest shareTripRequest)
        {
            var response = new TripShareV2Response();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(shareTripRequest.DeviceId, shareTripRequest.Application.Id.ToString(), shareTripRequest.Application.Version.Major, shareTripRequest.TransactionId, shareTripRequest.LanguageCode, shareTripRequest.SessionId);

                // Add Request Enrichers that would be propagated to all the logs within the same service
                _requestEnricher.Add(Constants.SessionId, shareTripRequest.SessionId);
                _requestEnricher.Add(Constants.ApplicationIdText, shareTripRequest.Application.Id);
                _requestEnricher.Add(Constants.ApplicationVersionText, shareTripRequest.Application.Version.Major);
                _requestEnricher.Add(Constants.DeviceIdText, shareTripRequest.DeviceId);
                _logger.LogInformation("GetShopRequest {@ClientRequest}", JsonConvert.SerializeObject(shareTripRequest));
                using (timer = _logger.BeginTimedOperation("Total time taken for GetShopRequest business call", transationId: shareTripRequest.TransactionId))
                {
                    response = await _shoppingBusiness.GetShopRequest(shareTripRequest);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetShopRequest Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException
                {
                    Message = uaex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("GetShopRequest Error {@Exception}", JsonConvert.SerializeObject(ex));
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetShopRequest {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost("Shopping/SelectTrip")]
        public async Task<SelectTripResponse> SelectTrip(SelectTripRequest selectTripRequest)
        {
            var response = new SelectTripResponse();

            try
            {
                await _headers.SetHttpHeader(selectTripRequest.DeviceId, selectTripRequest.Application?.Id.ToString(), selectTripRequest.Application?.Version?.Major, selectTripRequest.TransactionId, selectTripRequest.LanguageCode, selectTripRequest.SessionId);

                _logger.LogInformation("SelectTrip {@ClientRequest}", JsonConvert.SerializeObject(selectTripRequest));
                response = await _shoppingBusiness.SelectTrip(selectTripRequest, HttpContext);
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("SelectTrip Error {@UnitedException}", JsonConvert.SerializeObject(uaex));

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
                if (_configuration.GetValue<bool>("DisableFSRAlertRefresh") == false
                   && uaex.Message.Equals(_configuration.GetValue<string>("NoAvailabilityError2.0")))
                {
                    // Refresh the FSR screen based on this code when this error occurs
                    response.Exception.Code = _configuration.GetValue<string>("ErrorCodeForFSRRefresh");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SelectTrip Error {@Exception}", JsonConvert.SerializeObject(ex));

                #region 
                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    if (selectTripRequest.BackButtonClick && selectTripRequest.Application.Version.Major == "2.1.9I" && ex.Message.ToLower().Trim() == "Object reference not set to an instance of an object.".ToLower().Trim())
                    {
                        string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4iOS2.1.9ITouchIDBackButtonBug") != null ? _configuration.GetValue<string>("BookingExceptionMessage4iOS2.1.9ITouchIDBackButtonBug") : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                        response.Exception = new MOBException("9999", expMessage);
                    }
                    else if (!GeneralHelper.IsVersion1Greater(selectTripRequest.Application.Version.Major, "2.1.8", true) && ex.Message.ToLower().Trim() == "Object reference not set to an instance of an object.".ToLower().Trim())
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
                            string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_2") != null ? _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_2") : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                            response.Exception = new MOBException("9999", expMessage);
                        }
                        else
                        {
                            string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1") != null ? _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1") : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                            response.Exception = new MOBException("9999", expMessage);
                        }
                        #endregion
                    }
                    else if (!GeneralHelper.IsVersion1Greater(selectTripRequest.Application.Version.Major, "2.1.8", true) &&
                    ex.Message.ToLower().Replace("\r\n", string.Empty).Trim() == "index was out of range. must be non-negative and less than the size of the collection.parameter name: index".ToLower().Trim())
                    {
                        string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1") != null ? _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1") : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
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
                #endregion
            }
            _logger.LogInformation("SelectTrip {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;

        }

        [HttpPost("Shopping/ChasePromoRTIRedirect")]
        public async Task<ChasePromoRedirectResponse> ChasePromoRTIRedirect(ChasePromoRedirectRequest chasePromoRedirectRequest)
        {
            var response = new ChasePromoRedirectResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(chasePromoRedirectRequest.DeviceId, chasePromoRedirectRequest.Application.Id.ToString(), chasePromoRedirectRequest.Application.Version.Major, chasePromoRedirectRequest.TransactionId, chasePromoRedirectRequest.LanguageCode, chasePromoRedirectRequest.SessionId);

                _logger.LogInformation("ChasePromoRTIRedirect {@ClientResquest}", JsonConvert.SerializeObject(chasePromoRedirectRequest));
                using (timer = _logger.BeginTimedOperation("Total time taken for ChasePromoRTIRedirect business call", transationId: chasePromoRedirectRequest.TransactionId))
                {
                    response = await _shopProductsBusiness.ChasePromoRTIRedirect(chasePromoRedirectRequest);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("ChasePromoRTIRedirect Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException
                {
                    Message = uaex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("ChasePromoRTIRedirect Error {@Exception}", JsonConvert.SerializeObject(ex));
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
            _logger.LogInformation("ChasePromoRTIRedirect {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost("Shopping/GetProductInfoForFSRD")]
        public async Task<GetProductInfoForFSRDResponse> GetProductInfoForFSRD(GetProductInfoForFSRDRequest getProductInfoForFSRDRequest)
        {
            var response = new GetProductInfoForFSRDResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(getProductInfoForFSRDRequest.DeviceId, getProductInfoForFSRDRequest.Application.Id.ToString(), getProductInfoForFSRDRequest.Application.Version.Major, String.IsNullOrEmpty(getProductInfoForFSRDRequest.TransactionId) ? getProductInfoForFSRDRequest.SessionId : getProductInfoForFSRDRequest.TransactionId, getProductInfoForFSRDRequest.LanguageCode, getProductInfoForFSRDRequest.SessionId);

                _logger.LogInformation("GetProductInfoForFSRD {@ClientRequest}", JsonConvert.SerializeObject(getProductInfoForFSRDRequest));
                using (timer = _logger.BeginTimedOperation("Total time taken for GetProductInfoForFSRD business call", transationId: getProductInfoForFSRDRequest.TransactionId))
                {
                    response = await _shopProductsBusiness.GetProductInfoForFSRD(getProductInfoForFSRDRequest);
                }
            }

            catch (System.Net.WebException wex)
            {
                _logger.LogError("GetProductInfoForFSRD Error {@WebException}", JsonConvert.SerializeObject(wex));

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetProductInfoForFSRD Error {@UnitedException}", JsonConvert.SerializeObject(uaex));

                if (uaex != null && !string.IsNullOrEmpty(uaex.Message))
                {
                    response.Exception = new MOBException
                    {
                        Code = (string.IsNullOrEmpty(uaex.Code)) ? "9999" : uaex.Code.ToString(),
                        Message = uaex.Message.Trim()
                    };
                }
                else
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductInfoForFSRD Error {@Exception}", JsonConvert.SerializeObject(ex));
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
            _logger.LogInformation("GetProductInfoForFSRD {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost("Shopping/ShopTripPlan")]
        public async Task<ShopResponse> ShopTripPlan(MOBSHOPTripPlanRequest request)
        {
            var response = new ShopResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);

                _logger.LogInformation("ShopTripPlan {@ClientRequest}", JsonConvert.SerializeObject(request));

                string timedOperationMessage = string.Format("Total time taken for {0} business call", "Shop Trip Plan");

                using (timer = _logger.BeginTimedOperation(timedOperationMessage, transationId: request.TransactionId))
                {
                    response = await _shoppingBusiness.GetShopTripPlan(request, HttpContext);
                }
            }

            #region 
            catch (MOBUnitedException uaex)
            {
                #region
                //if (traceSwitch.TraceInfo)
                //{
                MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);
                //    shopping.LogEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, logAction, "UnitedException", request.Application.Id, request.Application.Version.Major, request.DeviceId, uaexWrapper, true, false));
                //}
                _logger.LogWarning("ShopTripPlan Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = uaex.Code;
                }

                //assigning right exception, if stop flights call returned no flights found in 2'nd shop call
                if (uaex.Message == "10038")
                {
                    response.Exception.Message = _configuration.GetValue<string>("NoAvailabilityError2.0TripPlan");
                }
                #endregion
            }
            catch (System.Exception ex)
            {
                string[] messages = ex.Message.Split('#');

                //if (traceSwitch.TraceInfo)
                //{
                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                exceptionWrapper.Message = messages[0];
                //    shopping.LogEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, logAction, "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, exceptionWrapper, true, false));
                //}
                //response.Exception = new MOBException("9999", ex.Message + "|" + ex.StackTrace);
                _logger.LogError("ShopTripPlan Error {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));

                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", messages[0]);
                }
            }
            #endregion

            try
            {
                string callDuration = string.Empty;
                if (response != null && response.Availability != null && response.Availability.Trip != null &&
                    !string.IsNullOrEmpty(response.Availability.Trip.CallDurationText))
                {
                    callDuration = response.Availability.Trip.CallDurationText;
                }

                if (_configuration.GetValue<string>("CartIdForDebug").ToString().ToUpper() == "YES" || (_configuration.GetValue<string>("DeviceIDSToReturnShopCallDurations") != null && _configuration.GetValue<string>("DeviceIDSToReturnShopCallDurations").ToUpper().Trim().Split('|').Contains(request.DeviceId.ToUpper().Trim())))//request.DeviceId.Trim().ToUpper() == "THIS_IS_FOR_TEST_" + DateTime.Now.ToString("MMM-dd-yyyy").ToUpper().Trim())
                {
                    response.CartId = response.CartId;// + cssCallDuration + callDuration + "|" + response.CallDuration;
                }
                if (_configuration.GetValue<string>("CartIdForDebug").ToString().ToUpper() == "REST_TUNING_CALL_DURATION")
                {
                    response.CartId = "CSS = " + callDuration + "|REST Total = " + (response.CallDuration / (double)1000).ToString();
                }
                if (_configuration.GetValue<string>("CartIdForDebug").ToString().ToUpper() == "REST_TUNING_CALL_DURATION_WITH_CARTID")
                {
                    response.CartId = "CSS = " + callDuration + "|REST Total = " + (response.CallDuration / (double)1000).ToString() + "|Cart ID = " + response.Availability.CartId;
                }
                if (_configuration.GetValue<bool>("Log_CSL_Call_Statistics"))
                {
                    if (response?.Availability?.Trip != null && response.ShopRequest != null)
                    {
                        try
                        {
                            string mileagePlusAccountNumber = request.MileagePlusAccountNumber;
                            if (!_configuration.GetValue<bool>("DisableAppendingCartIdWithMileaguePlusNumber"))
                            {

                                mileagePlusAccountNumber = mileagePlusAccountNumber + "_" + response.Availability.CartId;
                            }
                            int noOfTravelers = response.ShopRequest.TravelerTypes.Sum(t => (t?.Count ?? 0));
                            //Utility.AddCSLStatistics(request.Application.Id, request.Application.Version.Major, request.DeviceId, response.ShopRequest.SessionId, response.Availability.Trip.Origin, response.Availability.Trip.Destination, response.Availability.Trip.DepartDate, response.ShopRequest.SearchType, response.Availability.Trip.Cabin, response.ShopRequest.FareType, false, noOfTravelers, mileagePlusAccountNumber, null, "REST_Shopping", response.Availability.CartId, "Server:" + response.MachineName + "||CSS = " + cssCallDuration2 + callDuration + "|REST Total = " + (response.CallDuration / (double)1000).ToString(), "Shopping/ShopTripPlan");
                        }
                        catch { }
                    }
                }
            }
            catch { }
            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            var stringCount = JsonConvert.SerializeObject(response).Length;
            _logger.LogInformation("Shop {@ClientResponse}", JsonConvert.SerializeObject(response));

            return response;
        }


        [HttpPost("Shopping/GetCarbonEmissionDetails")]
        public async Task<MOBCarbonEmissionsResponse> GetCarbonEmissionDetails(MOBCarbonEmissionsRequest request)
        {
            var response = new MOBCarbonEmissionsResponse();
            try
            {
                await _headers.SetHttpHeader(request.DeviceId, request.Application?.Id.ToString(), request.Application?.Version?.Major, request.TransactionId, request.LanguageCode, request.SessionId);
                _logger.LogInformation("GetCarbonEmissionDetails {@ClientRequest}", JsonConvert.SerializeObject(request));
                response = await _shoppingBusiness.GetCarbonEmissionDetails(request);
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetCarbonEmissionDetails Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
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
                if (_configuration.GetValue<bool>("DisableFSRAlertRefresh") == false
                   && uaex.Message.Equals(_configuration.GetValue<string>("NoAvailabilityError2.0")))
                {
                    // Refresh the FSR screen based on this code when this error occurs
                    response.Exception.Code = _configuration.GetValue<string>("ErrorCodeForFSRRefresh");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("GetCarbonEmissionDetails Error {@Exception}", JsonConvert.SerializeObject(ex));
            }
            _logger.LogInformation("GetCarbonEmissionDetails {@ClientResponse}", JsonConvert.SerializeObject(response));
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
                request.ServiceName = ServiceNames.SHOPPING.ToString();
                await _featureSettings.RefreshFeatureSettingCache(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("RefreshFeatureSettingCache Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), "RefreshRetrieveAllFeatureSettings_TransId");
                response.Exception = new MOBException("9999", JsonConvert.SerializeObject(ex));
            }
            return response;
        }
        [HttpPost("Shopping/getmoneyplusmilesoptions")]
        public async Task<MOBMoneyPlusMilesOptionsResponse> GetMoneyPlusMilesOptions(MOBMoneyPlusMilesOptionsRequest request)
        {

            MOBMoneyPlusMilesOptionsResponse response = new MOBMoneyPlusMilesOptionsResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);

                _logger.LogInformation("GetMoneyPlusMilesOptions {@ClientRequest}", JsonConvert.SerializeObject(request));

                string timedOperationMessage = string.Format("Total time taken for {0} business call", "Money Plus Miles Options");

                using (timer = _logger.BeginTimedOperation(timedOperationMessage, transationId: request.TransactionId))
                {
                    response = await _shopMileagePricingBusiness.GetMoneyPlusMilesOptions(request, HttpContext);
                }
            }

            #region 
            catch (MOBUnitedException uaex)
            {
                #region
                 MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);
                _logger.LogWarning("GetMoneyPlusMilesOptions Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = uaex.Code;
                }

                #endregion
            }
            catch (System.Exception ex)
            {
                string[] messages = ex.Message.Split('#');

                 MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                exceptionWrapper.Message = messages[0];
               _logger.LogError("GetMoneyPlusMilesOptions Error {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));

                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", messages[0]);
                }
            }
            #endregion

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            var stringCount = JsonConvert.SerializeObject(response).Length;
            _logger.LogInformation("GetMoneyPlusMilesOptions {@ClientResponse}", JsonConvert.SerializeObject(response));

            return response;


        }
        [HttpPost("Shopping/applymileagepricing")]
        public async Task<MOBFSRMileagePricingResponse> ApplyMileagePricing(MOBFSRMileagePricingRequest request)
        {

            MOBFSRMileagePricingResponse response = new MOBFSRMileagePricingResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);

                _logger.LogInformation("ApplyMileagePricing {@ClientRequest}", JsonConvert.SerializeObject(request));

                string timedOperationMessage = string.Format("Total time taken for {0} business call", "Money Plus Miles Options");

                using (timer = _logger.BeginTimedOperation(timedOperationMessage, transationId: request.TransactionId))
                {
                    response = await _shopMileagePricingBusiness.ApplyMileagePricing(request, HttpContext);
                }
            }

            #region 
            catch (MOBUnitedException uaex)
            {
                #region
                MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);
                _logger.LogWarning("GetMoneyPlusMilesOptions Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = uaex.Code;
                }

                #endregion
            }
            catch (System.Exception ex)
            {
                string[] messages = ex.Message.Split('#');

                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                exceptionWrapper.Message = messages[0];
                _logger.LogError("GetMoneyPlusMilesOptions Error {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));

                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", messages[0]);
                }
            }
            #endregion

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            var stringCount = JsonConvert.SerializeObject(response).Length;
            _logger.LogInformation("ApplyMileagePricing {@ClientResponse}", JsonConvert.SerializeObject(response));

            return response;


        }
    }
    }
