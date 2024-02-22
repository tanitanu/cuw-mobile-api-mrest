using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ReShop;
using United.Mobile.ScheduleChange.Domain;
using United.Utility.Serilog;

namespace United.Mobile.ScheduleChange.Api.Controllers
{
    [ApiController]
    [Route("schedulechangeservice/api")]
    public class ScheduleChangeController : ControllerBase
    {
        private readonly ICacheLog<ScheduleChangeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IScheduleChangeBusiness _scheduleChangeBusiness;
        private readonly IHeaders _headers;

        public ScheduleChangeController(ICacheLog<ScheduleChangeController> logger
            , IConfiguration configuration
            , IScheduleChangeBusiness scheduleChangeBusiness
            , IHeaders headers)
        {
            _logger = logger;
            _scheduleChangeBusiness = scheduleChangeBusiness;
            _configuration = configuration;
            _headers = headers;
        }

        [HttpGet]
        [Route("HealthCheck")]
        public string HealthCheck()
        {
            return "Healthy";
        }

        [HttpPost("ReShopping/ConfirmScheduleChange")]
        public async Task<MOBConfirmScheduleChangeResponse> ConfirmScheduleChange(MOBConfirmScheduleChangeRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBConfirmScheduleChangeResponse();

            IDisposable timer = null;
            try
            {
                timer = _logger.BeginTimedOperation("Total time taken for ConfirmScheduleChange business call", transationId: request.TransactionId);
                response = await _scheduleChangeBusiness.ConfirmScheduleChange(request);
            }
            catch (UnitedException uaex)
            {
                _logger.LogWarning("ConfirmScheduleChange Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), request.SessionId);
                _logger.LogWarning("ConfirmScheduleChange Error {exception} and {sessionId}", uaex.Message, request.SessionId);

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("PNRConfmScheduleChangeExcMessage"));
            }
            catch (Exception ex)
            {
                _logger.LogError("ConfirmScheduleChange Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), request.SessionId);
                _logger.LogError("ConfirmScheduleChange Error {exception} and {sessionId}", ex.Message, request.SessionId);

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            response.CallDuration = 0;
            if (timer != null) response.CallDuration = ((TimedOperation)timer).GetElapseTime();
            _logger.LogInformation("ConfirmScheduleChange {@clientResponse} and {SessionId}", JsonConvert.SerializeObject(response), request.SessionId);
            return response;
        }
    }
}
