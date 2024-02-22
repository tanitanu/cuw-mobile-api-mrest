using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.ReshopSelectTrip.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;

namespace United.Mobile.ReshopSelectTrip.Api.Controllers
{
    [ApiController]
    [Route("reshopselecttripservice/api")]
    public class ReshopSelectTripController : ControllerBase
    {
        private readonly ICacheLog<ReshopSelectTripController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IReshopSelectTripBusiness _reshopSelectTripBusiness;
        private readonly IHeaders _headers;

        public ReshopSelectTripController(ICacheLog<ReshopSelectTripController> logger, IConfiguration configuration, IReshopSelectTripBusiness reshopSelectTripBusiness, IHeaders headers)
        {
            _logger = logger;
            _reshopSelectTripBusiness = reshopSelectTripBusiness;
            _configuration = configuration;
            _headers = headers;
        }

        [HttpGet]
        [Route("HealthCheck")]
        public string HealthCheck()
        {
            return "Healthy";
        }

        [HttpPost("ReShopping/ReshopSelectTrip")]
        public async Task<SelectTripResponse> ReshopSelectTrip(SelectTripRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new SelectTripResponse();

            IDisposable timer = null;
            try
            {
                using (timer = _logger.BeginTimedOperation("Total time taken for ReshopSelectTrip business call", transationId: request.TransactionId))
                {
                    response = await _reshopSelectTripBusiness.SelectTrip(request);
                }
            }
            catch (UnitedException uaex)
            {
                _logger.LogWarning("ReshopSelectTrip Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), request.TransactionId);
                _logger.LogWarning("ReshopSelectTrip Error {exception} and {sessionId}", uaex.Message, request.TransactionId);

                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
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
                _logger.LogError("ReshopSelectTrip Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), request.TransactionId);
                _logger.LogError("ReshopSelectTrip Error {exception} and {sessionId}", ex.Message, request.TransactionId);

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
            _logger.LogInformation("ReshopSelectTrip {@clientResponse} {transactionId}", JsonConvert.SerializeObject(response), request.TransactionId);
            return response;
        }
    }
}
