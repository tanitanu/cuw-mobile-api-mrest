using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.DataAccess.Common;
using United.Mobile.EligibleCheck.Domain;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ReShop;
using United.Utility.Serilog;

namespace United.Mobile.EligibleCheck.Api.Controllers
{
    [ApiController]
    [Route("eligiblecheckservice/api")]
    public class EligibleCheckController : ControllerBase
    {
        private readonly ICacheLog<EligibleCheckController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEligibleCheckBusiness _eligibleCheckBusiness;
        private readonly IHeaders _headers;


        public EligibleCheckController(ICacheLog<EligibleCheckController> logger
            , IConfiguration configuration
            , IEligibleCheckBusiness eligibleCheckBusiness
            , IHeaders headers)
        {
            _logger = logger;
            _eligibleCheckBusiness = eligibleCheckBusiness;
            _configuration = configuration;
            _headers = headers;
        }

        [HttpGet]
        [Route("HealthCheck")]
        public string HealthCheck()
        {
            return "Healthy";
        }

        [HttpPost("ReShopping/ChangeEligibleCheckAndReshop")]
        public async Task<MOBRESHOPChangeEligibilityResponse> ChangeEligibleCheckAndReshop(MOBRESHOPChangeEligibilityRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBRESHOPChangeEligibilityResponse();
            IDisposable timer = null;
            try
            {
                timer = _logger.BeginTimedOperation("Total time taken for ChangeEligibleCheckAndReshop business call", transationId: request.TransactionId);
                {
                    response = await _eligibleCheckBusiness.ChangeEligibleCheckAndReshop(request);
                }
            }
            catch (UnitedException uaex)
            {
                _logger.LogWarning("ChangeEligibleCheckAndReshop Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), request.SessionId);
                _logger.LogWarning("ChangeEligibleCheckAndReshop Error {exception} and {sessionId}", uaex.Message, request.SessionId);
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
                _logger.LogError("ChangeEligibleCheckAndReshop Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), request.SessionId);
                _logger.LogError("ChangeEligibleCheckAndReshop Error {exception} and {sessionId}", ex.Message, request.SessionId);
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            response.CallDuration = 0;
            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }
            _logger.LogInformation("ChangeEligibleCheckAndReshop {@clientResponse} and {SessionId}", JsonConvert.SerializeObject(response), request.SessionId);
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
                timer = _logger.BeginTimedOperation("Total time taken for ChangeEligibleCheck business call", transationId: request.TransactionId);
                {
                    response = await _eligibleCheckBusiness.ChangeEligibleCheck(request);
                }
            }
            catch (UnitedException uaex)
            {
                _logger.LogWarning("ChangeEligibleCheck Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), request.SessionId);
                _logger.LogWarning("ChangeEligibleCheck Error {exception} and {sessionId}", uaex.Message, request.SessionId);

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
                _logger.LogError("ChangeEligibleCheck Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), request.SessionId);
                _logger.LogError("ChangeEligibleCheck Error {exception} and {sessionId}", ex.Message, request.SessionId);

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            response.CallDuration = 0;
            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }
            _logger.LogInformation("ChangeEligibleCheck {@clientResponse} and {sessionId}", JsonConvert.SerializeObject(response), request.SessionId);
            return response;
        }
    }
}
