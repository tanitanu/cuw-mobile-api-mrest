using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ReShop;
using United.Mobile.UpdateProfile.Domain;
using United.Utility.Serilog;

namespace United.Mobile.UpdateProfile.Api.Controllers
{
    [ApiController]
    [Route("updateprofileservice/api")]
    public class UpdateProfileController : ControllerBase
    {
        private readonly ICacheLog<UpdateProfileController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUpdateProfileBusiness _updateProfileBusiness;
        private readonly IHeaders _headers;

        public UpdateProfileController(ICacheLog<UpdateProfileController> logger
            , IConfiguration configuration
            , IUpdateProfileBusiness updateProfileBusiness
            , IHeaders headers)
        {
            _logger = logger;
            _updateProfileBusiness = updateProfileBusiness;
            _configuration = configuration;
            _headers = headers;
        }

        [HttpGet]
        [Route("HealthCheck")]
        public string HealthCheck()
        {
            return "Healthy";
        }

        [HttpPost("ReShopping/ReshopSaveEmail_CFOP")]
        public async Task<MOBChangeEmailResponse> ReshopSaveEmail_CFOP(MOBChangeEmailRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBChangeEmailResponse();

            IDisposable timer = null;
            try
            {
                timer = _logger.BeginTimedOperation("Total time taken for ReshopSaveEmail_CFOP business call", transationId: request.TransactionId);
                {
                    _logger.LogInformation("ReshopSaveEmail_CFOP {@clientRequest} and {SessionId}", JsonConvert.SerializeObject(request), request.SessionId);
                    response = await _updateProfileBusiness.ReshopSaveEmail_CFOP(request);
                }
            }
            catch (UnitedException uaex)
            {
                MOBException exception = new MOBException();
                if (uaex != null && !string.IsNullOrEmpty(uaex.Message))
                {
                    exception = new MOBException();
                    exception.Code = (string.IsNullOrEmpty(uaex.Code)) ? "9999" : uaex.Code.ToString();
                    exception.Message = uaex.Message.Trim();
                }
                else
                {
                    exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                response.Exception = exception;
                _logger.LogWarning("ReshopSaveEmail_CFOP Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex.StackTrace), request.SessionId);
                _logger.LogWarning("ReshopSaveEmail_CFOP Error {exception} and {sessionId}", exception.Message, request.SessionId);
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
                _logger.LogError("ReshopSaveEmail_CFOP Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), request.SessionId);
                _logger.LogError("ReshopSaveEmail_CFOP Error {exception} and {sessionId}", ex.Message, request.SessionId);
            }

            response.CallDuration = 0;
            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }
            _logger.LogInformation("ReshopSaveEmail_CFOP {@clientResponse} and {SessionId}", JsonConvert.SerializeObject(response), request.SessionId);
            return response;
        }
    }
}
