using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.FlightReservation.Domain;
using United.Mobile.Model.Common;
using United.Mobile.Model.FlightReservation;
using United.Mobile.Model.Internal.Exception;
using United.Utility.Helper;
using United.Utility.Serilog;

namespace United.Mobile.FlightReservation.Api.Controllers
{
    [Route("flightreservationservice/api")]
    [ApiController]
    public class FlightReservationController : ControllerBase
    {
        private readonly ICacheLog<FlightReservationController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly IFlightReservationBusiness _flightReservationBusiness;

        public FlightReservationController(ICacheLog<FlightReservationController> logger
            , IConfiguration configuration
            , IHeaders headers
            , IFlightReservationBusiness flightReservationBusiness)
        {
            _logger = logger;
            _configuration = configuration;
            _headers = headers;
            _flightReservationBusiness = flightReservationBusiness;
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
        [Route("GetPNRsByMileagePlusNumber")]
        public async Task<MOBPNRByMileagePlusResponse> GetPNRsByMileagePlusNumber(int applicationId, string appVersion, string accessCode, string transactionId, string mileagePlusNumber, string pinCode, string reservationType, string languageCode, bool includeFarelockInfo = false)
        {
            await _headers.SetHttpHeader(string.Empty, applicationId.ToString(), appVersion, transactionId, languageCode, string.Empty);
            var response = new MOBPNRByMileagePlusResponse();
            IDisposable timer = null;

            try
            {
                using (timer = _logger.BeginTimedOperation("Total time taken for GetPNRsByMileagePlusNumber business call", transationId: transactionId))
                {
                    response = await _flightReservationBusiness.GetPNRsByMileagePlusNumber(applicationId, appVersion, accessCode, transactionId, mileagePlusNumber, pinCode, reservationType, languageCode, includeFarelockInfo);
                }
            }

            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("GetPNRsByMileagePlusNumber Warning {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(coex), transactionId);
                _logger.LogWarning("GetPNRsByMileagePlusNumber Warning {exception} and {transactionId}", coex.Message, transactionId);
                response.Exception = new MOBException();
                response.Exception.Message = coex.Message;
            }

            catch (Exception ex)
            {
                _logger.LogError("GetPNRsByMileagePlusNumber Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), transactionId);
                _logger.LogError("GetPNRsByMileagePlusNumber Error {exception} and {transactionId}", ex.Message, transactionId);

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("GenericExceptionMessage"));
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetPNRsByMileagePlusNumber {@clientResponse} {transactionId}", JsonConvert.SerializeObject(response), transactionId);

            return response;
        }
        [HttpPost]
        [Route("RequestReceiptByEmail")]
        public async Task<MOBReceiptByEmailResponse> RequestReceiptByEmail(MOBReceiptByEmailRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, string.Empty);
            var response = new MOBReceiptByEmailResponse();
            IDisposable timer = null;

            try
            {
                using (timer = _logger.BeginTimedOperation("Total time taken for RequestReceiptByEmail business call", transationId: request.TransactionId))
                {
                    response = await _flightReservationBusiness.RequestReceiptByEmail(request);
                }
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("RequestReceiptByEmail Warning {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(coex), request.TransactionId);
                _logger.LogWarning("RequestReceiptByEmail Warning {exception} and {transactionId}", coex.Message, request.TransactionId);
                response.Exception = new MOBException();
                response.Exception.Message = coex.Message;
            }

            catch (Exception ex)
            {
                _logger.LogError("RequestReceiptByEmail Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), request.TransactionId);
                _logger.LogError("RequestReceiptByEmail Error {exception} and {transactionId}", ex.Message, request.TransactionId);

                response.Exception = new MOBException("10000", _configuration.GetValue<string>("GenericExceptionMessage"));
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("RequestReceiptByEmail {@clientResponse} {transactionId}", JsonConvert.SerializeObject(response), request.TransactionId);

            return response;
        }

        [HttpPost]
        [Route("AddPNRRemark")]
        public async Task<MOBPNRRemarkResponse> AddPNRRemark(MOBPNRRemarkRequest request)
        {

            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, string.Empty);
            var response = new MOBPNRRemarkResponse();
            IDisposable timer = null;

            try
            {
                using (timer = _logger.BeginTimedOperation("Total time taken for AddPNRRemark business call", transationId: request.TransactionId))
                {
                    response = await _flightReservationBusiness.AddPNRRemark(request);
                }
            }
            catch (MOBUnitedException coex)
            {
                _logger.LogWarning("AddPNRRemark Warning {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(coex), request.TransactionId);
                _logger.LogWarning("AddPNRRemark Warning {exception} and {transactionId}", coex.Message, request.TransactionId);
                response.Exception = new MOBException();
                response.Exception.Message = coex.Message;
            }

            catch (Exception ex)
            {
                _logger.LogError("AddPNRRemark Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), request.TransactionId);
                _logger.LogError("AddPNRRemark Error {exception} and {transactionId}", ex.Message, request.TransactionId);

                response.Exception = new MOBException("10000", _configuration.GetValue<string>("GenericExceptionMessage"));
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("AddPNRRemark {@clientResponse} {transactionId}", JsonConvert.SerializeObject(response), request.TransactionId);

            return response;
        }
    }
}
