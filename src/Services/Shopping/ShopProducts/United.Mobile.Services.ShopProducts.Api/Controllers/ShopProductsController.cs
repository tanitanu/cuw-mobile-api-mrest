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
using United.Mobile.Services.ShopProducts.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;

namespace United.Mobile.Services.ShopProducts.Api.Controllers
{
    [Route("shopproductsservice/api")]
    [ApiController]
    public class ShopProductsController : ControllerBase
    {
        private readonly ICacheLog<ShopProductsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IShopProductsBusiness _shopProductsBusiness;
        private readonly IHeaders _headers;
        private readonly IApplicationEnricher _requestEnricher;

        public ShopProductsController(ICacheLog<ShopProductsController> logger, IConfiguration configuration, IShopProductsBusiness shopProductsBusiness, IHeaders headers, IApplicationEnricher requestEnricher)
        {
            _logger = logger;
            _shopProductsBusiness = shopProductsBusiness;
            _configuration = configuration;
            _headers = headers;
            _requestEnricher = requestEnricher;
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

        [HttpPost("Shopping/ChasePromoRTIRedirect")]
        public async Task<ChasePromoRedirectResponse> ChasePromoRTIRedirect(ChasePromoRedirectRequest chasePromoRedirectRequest)
        {
            var response = new ChasePromoRedirectResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(chasePromoRedirectRequest.DeviceId, chasePromoRedirectRequest.Application.Id.ToString(), chasePromoRedirectRequest.Application.Version.Major, chasePromoRedirectRequest.TransactionId, chasePromoRedirectRequest.LanguageCode, chasePromoRedirectRequest.SessionId);
                _logger.LogInformation("ChasePromoRTIRedirect {ClientResquest} and {sessionId}", JsonConvert.SerializeObject(chasePromoRedirectRequest), chasePromoRedirectRequest.SessionId);
                using (timer = _logger.BeginTimedOperation("Total time taken for ChasePromoRTIRedirect business call", transationId: chasePromoRedirectRequest.TransactionId))
                {
                    response = await _shopProductsBusiness.ChasePromoRTIRedirect(chasePromoRedirectRequest);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("ChasePromoRTIRedirect Warning {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), chasePromoRedirectRequest.SessionId);
                _logger.LogWarning("ChasePromoRTIRedirect Error {@UnitedException} and {sessionId}", uaex.Message, chasePromoRedirectRequest.SessionId);
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("ChasePromoRTIRedirect Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), chasePromoRedirectRequest.SessionId);
                _logger.LogError("ChasePromoRTIRedirect Error {exception} and {sessionId}", ex.Message, chasePromoRedirectRequest.SessionId);
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
            _logger.LogInformation("ChasePromoRTIRedirect {ClientResponse} and {sessionId}", JsonConvert.SerializeObject(response), chasePromoRedirectRequest.SessionId);
            return response;
        }

        [HttpPost("Shopping/GetProductInfoForFSRD")]
        public async Task<GetProductInfoForFSRDResponse> GetProductInfoForFSRD(GetProductInfoForFSRDRequest getProductInfoForFSRDRequest)
        {
            var response = new GetProductInfoForFSRDResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(getProductInfoForFSRDRequest.DeviceId, getProductInfoForFSRDRequest.Application.Id.ToString(), getProductInfoForFSRDRequest.Application.Version.Major, getProductInfoForFSRDRequest.TransactionId, getProductInfoForFSRDRequest.LanguageCode, getProductInfoForFSRDRequest.SessionId);
            
                _logger.LogInformation("GetProductInfoForFSRD {ClientRequest} and {sessionId}", JsonConvert.SerializeObject(getProductInfoForFSRDRequest), getProductInfoForFSRDRequest.SessionId);
                using (_logger.BeginTimedOperation("Total time taken for GetProductInfoForFSRD business call", transationId: getProductInfoForFSRDRequest.TransactionId))
                {
                    response = await _shopProductsBusiness.GetProductInfoForFSRD(getProductInfoForFSRDRequest);
                }
            }
            catch (System.Net.WebException wex)
            {
                _logger.LogError("GetProductInfoForFSRD Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(wex), getProductInfoForFSRDRequest.SessionId);
                _logger.LogError("GetProductInfoForFSRD Error {exception} and {sessionId}", wex.Message, getProductInfoForFSRDRequest.SessionId);

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetProductInfoForFSRD Warning {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), getProductInfoForFSRDRequest.SessionId);
                _logger.LogWarning("GetProductInfoForFSRD Error {@UnitedException} and {sessionId}", uaex.Message, getProductInfoForFSRDRequest.SessionId);

                if (uaex != null && !string.IsNullOrEmpty(uaex.Message))
                {
                    response.Exception = new MOBException();
                    response.Exception.Code = (string.IsNullOrEmpty(uaex.Code)) ? "9999" : uaex.Code.ToString();
                    response.Exception.Message = uaex.Message.Trim();
                }
                else
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductInfoForFSRD Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), getProductInfoForFSRDRequest.SessionId);
                _logger.LogError("GetProductInfoForFSRD Error {exception} and {sessionId}", ex.Message, getProductInfoForFSRDRequest.SessionId);
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
            _logger.LogInformation("GetProductInfoForFSRD {ClientResponse} and {sessionId}", JsonConvert.SerializeObject(response), getProductInfoForFSRDRequest.SessionId);
            return response;
        }
    }
}