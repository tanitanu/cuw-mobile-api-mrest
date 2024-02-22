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
using United.Mobile.Model.UnitedClubPasses;
using United.Mobile.UnitedClubPasses.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;
using United.Mobile.Model.Common.FeatureSettings;


namespace United.Mobile.UnitedClub.Api.Controllers
{
    [Route("unitedclubservice/api/")]
    [ApiController]
    public class UnitedClubController : ControllerBase
    {
        private readonly ICacheLog<UnitedClubController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUnitedClubBusiness _clubBusiness;
        private readonly IPurchaseOTPPassesBusiness _purchaseOTPPassesBusiness;
        private readonly IHeaders _headers;
        private readonly IApplicationEnricher _requestEnricher;
        private readonly IFeatureSettings _featureSettings;

        public UnitedClubController(ICacheLog<UnitedClubController> logger,
            IConfiguration configuration,
            IUnitedClubBusiness clubBusiness,
            IHeaders headers,
            IPurchaseOTPPassesBusiness purchaseOTPPassesBusiness,
            IApplicationEnricher requestEnricher
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _clubBusiness = clubBusiness;
            _configuration = configuration;
            _purchaseOTPPassesBusiness = purchaseOTPPassesBusiness;
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
        [HttpPost]
        [Route("GetPKDispenserPublicKey")]
        public async Task<ClubPKDispenserPublicKeyResponse> GetPKDispenserPublicKey([FromBody] ClubPKDispenserPublicKeyRequest request)
        {
            ClubPKDispenserPublicKeyResponse response = new ClubPKDispenserPublicKeyResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.TransactionId);
                // Add Request Enrichers that would be propagated to all the logs within the same service
                _requestEnricher.Add(Constants.TransactionIdText, request.TransactionId);
                _requestEnricher.Add(Constants.ApplicationIdText, request.Application.Id);
                _requestEnricher.Add(Constants.ApplicationVersionText, request.Application.Version.Major);
                _requestEnricher.Add(Constants.DeviceIdText, request.DeviceId);
                _logger.LogInformation("GetPKDispenserPublicKey {clientRequest} and {transactionId}", JsonConvert.SerializeObject(request), request.TransactionId);
                using (timer = _logger.BeginTimedOperation("Total time taken for GetPKDispenserPublicKey business call", transationId: request.TransactionId))
                {
                    response = await _clubBusiness.GetPKDispenserPublicKey(request);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogError("GetPKDispenserPublicKey Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), request.TransactionId);
                _logger.LogError("GetPKDispenserPublicKey Error {exception} and {sessionId}", uaex.Message, request.TransactionId);
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }
            catch (System.Exception ex)
            {
                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                _logger.LogError("GetPKDispenserPublicKey Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(exceptionWrapper), request.TransactionId);
                _logger.LogError("GetPKDispenserPublicKey Error {exception} and {sessionId}", ex.Message, request.TransactionId);
                response.Exception = !_configuration.GetValue<bool>("SurfaceErrorToClient") ? new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage")) : new MOBException("9999", ex.Message);
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetPKDispenserPublicKey {clientResponse} and {transactionId}", response, request.TransactionId);
            return response;
        }
        [HttpPost]
        [Route("GetPublicKey")]
        public async Task<ClubPKDispenserPublicKeyResponse> GetPublicKey([FromBody] ClubPKDispenserPublicKeyRequest request)
        {
            ClubPKDispenserPublicKeyResponse response = new ClubPKDispenserPublicKeyResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.TransactionId);
                // Add Request Enrichers that would be propagated to all the logs within the same service
                _requestEnricher.Add(Constants.TransactionIdText, request.TransactionId);
                _requestEnricher.Add(Constants.ApplicationIdText, request.Application.Id);
                _requestEnricher.Add(Constants.ApplicationVersionText, request.Application.Version.Major);
                _requestEnricher.Add(Constants.DeviceIdText, request.DeviceId);
                _logger.LogInformation("GetPublicKey {clientRequest} and {transactionId}", JsonConvert.SerializeObject(request), request.TransactionId);
                using (timer = _logger.BeginTimedOperation("Total time taken for GetPublicKey business call", transationId: request.TransactionId))
                {
                    response = await _clubBusiness.GetPublicKey(request);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogError("GetPublicKey Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), request.TransactionId);
                _logger.LogError("GetPublicKey Error {exception} and {sessionId}", uaex.Message, request.TransactionId);
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }
            catch (System.Exception ex)
            {
                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                _logger.LogError("GetPublicKey Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(exceptionWrapper), request.TransactionId);
                _logger.LogError("GetPublicKey Error {exception} and {sessionId}", ex.Message, request.TransactionId);
                response.Exception = !_configuration.GetValue<bool>("SurfaceErrorToClient") ? new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage")) : new MOBException("9999", ex.Message);
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetPublicKey {clientResponse}  and {transactionId}", response, request.TransactionId);
            return response;
        }

        [HttpPost]
        [Route("PurchaseOTPPasses")]
        public async Task<OTPPurchaseResponse> PurchaseOTPPasses([FromBody] OTPPurchaseRequest request)
        {
            OTPPurchaseResponse response = new OTPPurchaseResponse();
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
                // Add Request Enrichers that would be propagated to all the logs within the same service
                _requestEnricher.Add(Constants.TransactionIdText, request.TransactionId);
                _requestEnricher.Add(Constants.ApplicationIdText, request.Application.Id);
                _requestEnricher.Add(Constants.ApplicationVersionText, request.Application.Version.Major);
                _requestEnricher.Add(Constants.DeviceIdText, request.DeviceId);
                _requestEnricher.Add(Constants.SessionId, request.SessionId);
                _requestEnricher.Add(Constants.Credit_CardTypeDescription, request.CreditCard.CardTypeDescription);
                _requestEnricher.Add(Constants.Credit_cCName, request.CreditCard.CCName);

                _logger.LogInformation("PurchaseOTPPasses {@ClientRequest}", JsonConvert.SerializeObject(request));

                using (timer = _logger.BeginTimedOperation("Total time taken for PurchaseOTPPasses business call", transationId: request.SessionId))
                {
                    response = await _purchaseOTPPassesBusiness.PurchaseOTPPasses(request);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("PurchaseOTPPasses Warning {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), request.SessionId ?? response.sessionId);
                _logger.LogWarning("PurchaseOTPPasses Warning {exception} and {sessionId}", uaex.Message, request.SessionId ?? response.sessionId);
                response.Exception = new MOBException(uaex.Message, _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            catch (System.Exception ex)
            {
                _logger.LogError("PurchaseOTPPasses Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), request.SessionId ?? response.sessionId);
                _logger.LogError("PurchaseOTPPasses Error {exception} and {sessionId}", ex.Message, request.SessionId ?? response.sessionId);
                response.Exception = !_configuration.GetValue<bool>("SurfaceErrorToClient") ? new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage")) : new MOBException("9999", ex.Message);
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;

            _logger.LogInformation("PurchaseOTPPasses {ClientResponse} and {sessionId}", JsonConvert.SerializeObject(response), request.SessionId ?? response.sessionId);

            return response;
        }


        [HttpPost]
        [Route("GetUnitedClubMembershipV2")]
        public async Task<ClubMembershipResponse> GetUnitedClubMembershipV2([FromBody] UnitedClubMembershipRequest request)
        {
            ClubMembershipResponse response = new ClubMembershipResponse();
            return response;
            IDisposable timer = null;
            try
            {
                await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, string.Empty);
                // Add Request Enrichers that would be propagated to all the logs within the same service
                _requestEnricher.Add(Constants.TransactionIdText, request.TransactionId);
                _requestEnricher.Add(Constants.ApplicationIdText, request.Application.Id);
                _requestEnricher.Add(Constants.ApplicationVersionText, request.Application.Version.Major);
                _requestEnricher.Add(Constants.DeviceIdText, request.DeviceId);
                _requestEnricher.Add(Constants.MileagePlusNumber, request.MPNumber);
                _requestEnricher.Add(Constants.HashPinCode, request.HashPinCode);
                _logger.LogInformation("GetUnitedClubMembershipV2 {clientRequest} and {transactionId}", JsonConvert.SerializeObject(request), request.TransactionId);
                using (timer = _logger.BeginTimedOperation("Total time taken for GetUnitedClubMembershipV2 business call", transationId: request.TransactionId))
                {
                    response = await _clubBusiness.GetUnitedClubMembershipV2(request);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogError("GetUnitedClubMembershipV2 Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), request.TransactionId);
                _logger.LogError("GetUnitedClubMembershipV2 Error {exception} and {sessionId}", uaex.Message, request.TransactionId);
                response.Exception = new MOBException("GetUnitedClubMembershipV2", uaex.Message);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("GetUnitedClubMembershipV2 Error {exceptionstack} and {sessionId}", ex.StackTrace, request.TransactionId);
                _logger.LogError("GetUnitedClubMembershipV2 Error {exception} and {sessionId}", ex.Message, request.TransactionId);
                response.Exception = new MOBException("10000", _configuration.GetValue<string>("GenericExceptionMessage"));
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetUnitedClubMembershipV2 {clientResponse} and {transactionId}", response, request.TransactionId);
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
                request.ServiceName = United.Mobile.Model.Common.ServiceNames.UNITEDCLUBPASSES.ToString();
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
