using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.Model.Common;
using United.Mobile.Model.FeedBack;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.UpdateMemberProfile;
using United.Mobile.UpdateMemberProfile.Domain;
using United.Utility.Helper;
using United.Utility.Serilog;
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model;

namespace United.Mobile.UpdateMemberProfile.Api.Controllers
{
    [Route("updatememberprofileservice/api/")]
    [ApiController]
    public class UpdateMemberProfileController : ControllerBase
    {
        private readonly ICacheLog<UpdateMemberProfileController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUpdateMemberProfileBusiness _updateMemberProfileBusiness;
        private readonly IHeaders _headers;
        private readonly IFeatureSettings _featureSettings;
        public UpdateMemberProfileController(ICacheLog<UpdateMemberProfileController> logger
            , IConfiguration configuration
            , IUpdateMemberProfileBusiness updateMemberProfileBusiness
            , IHeaders headers
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _updateMemberProfileBusiness = updateMemberProfileBusiness;
            _headers = headers;
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
        [Route("CustomerProfile/UpdateProfileOwner")]
        public async Task<MOBCustomerProfileResponse> UpdateProfileOwner(MOBUpdateCustomerFOPRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBCustomerProfileResponse();
            IDisposable timer = null;
            try
            {
                _logger.LogInformation("UpdateProfileOwner {@ClientRequest}", JsonConvert.SerializeObject(request));
                using (timer = _logger.BeginTimedOperation("Total time taken for UpdateProfileOwner business call", transationId: request.TransactionId))
                {
                    response = await _updateMemberProfileBusiness.UpdateProfileOwner(request);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("UpdateProfileOwner Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                if (_updateMemberProfileBusiness.IsViewResPath("viewrespath"))
                {
                    response.Exception = new MOBException("9999", "please verify the credit card information entered is correct or try another form of payment.");
                }
                else
                {
                    if (uaex != null && !string.IsNullOrEmpty(uaex.Message.Trim()))
                    {
                        response.Exception = new MOBException();
                        response.Exception.Message = uaex.Message;
                    }
                    else
                    {
                        response.Exception = new MOBException("9999", _configuration.GetValue<string>("UpdateProfileCCException") as string);
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError("UpdateProfileOwner Error {@Exception}", JsonConvert.SerializeObject(ex));
                response.Exception = (!Convert.ToBoolean(_configuration.GetValue<String>("SurfaceErrorToClient"))) ?
                new MOBException("9999", _configuration.GetValue<String>("Booking2OGenericExceptionMessage")) :
                new MOBException("9999", ex.Message);
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("UpdateProfileOwnerr {@ClientResponse}", JsonConvert.SerializeObject(response), request.TransactionId);
            return response;
        }

        [HttpPost]
        [Route("CustomerProfile/UpdateProfileOwnerCardInfo")]
        public async Task<MOBUpdateProfileOwnerFOPResponse> UpdateProfileOwnerCardInfo(MOBUpdateProfileOwnerFOPRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBUpdateProfileOwnerFOPResponse();
            IDisposable timer = null;
            try
            {
                #region Remove Encrypted CardNumber from request while logging (Change from Security Complaince team)
                MOBUpdateProfileOwnerFOPRequest clonedRequest = request.Clone();
                ConfigUtility.RemoveEncyptedCreditcardNumberfromRequest(clonedRequest?.Traveler?.CreditCards);
                #endregion
                _logger.LogInformation("UpdateProfileOwnerCardInfo {@clientRequest} {sessionId}", JsonConvert.SerializeObject(clonedRequest), request.SessionId);
                using (timer = _logger.BeginTimedOperation("Total time taken for UpdateProfileOwnerCardInfo business call", transationId: request.TransactionId))
                {
                    response = await _updateMemberProfileBusiness.UpdateProfileOwnerCardInfo(request);
                }
            }
            catch (MOBUnitedException uaex)
            {

                //_logger.LogWarning("UpdateProfileOwnerCardInfo Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), request.SessionId);
                _logger.LogWarning("UpdateProfileOwnerCardInfo Error {exception} and {sessionId}", JsonConvert.SerializeObject(uaex), request.SessionId);

                if (_updateMemberProfileBusiness.IsViewResPath("viewrespath"))
                {
                    response.Exception = new MOBException("9999", "please verify the credit card information entered is correct or try another form of payment.");
                }
                else
                {
                    if (uaex != null && !string.IsNullOrEmpty(uaex.Message.Trim()))
                    {
                        response.Exception = new MOBException();
                        response.Exception.Message = uaex.Message;
                    }
                    else
                    {
                        response.Exception = new MOBException("9999", _configuration.GetValue<string>("UpdateProfileCCException") as string);
                    }
                }

                if (request.IsPartnerProvision || request.IsExistCreditCard)
                {
                    await _updateMemberProfileBusiness.UpdateProvisionStatus(request);
                }
            }
            catch (System.Exception ex)
            {
                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                _logger.LogError("UpdateProfileOwnerCardInfo Error {Exception} and {sessionId}", JsonConvert.SerializeObject(exceptionWrapper), request.SessionId);
                // _logger.LogError("UpdateProfileOwnerCardInfo Error {Exception} and {sessionId}", ex.Message, request.SessionId);

                response.Exception = (!Convert.ToBoolean(_configuration.GetValue<String>("SurfaceErrorToClient"))) ?
                new MOBException("9999", _configuration.GetValue<String>("Booking2OGenericExceptionMessage")) :
                new MOBException("9999", ex.Message);

                if (request.IsPartnerProvision || request.IsExistCreditCard)
                {
                    await _updateMemberProfileBusiness.UpdateProvisionStatus(request);
                }
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("UpdateProfileOwnerCardInfo {@clientResponse} {sessionId}", JsonConvert.SerializeObject(response), request.SessionId);
            return response;
        }

        [HttpPost]
        [Route("FeedBack/PromoFeedback")]
        public async Task<MOBPromoFeedbackResponse> PromoFeedback(MOBPromoFeedbackRequest promofeedbackrequest)
        {
            await _headers.SetHttpHeader(promofeedbackrequest.DeviceId, promofeedbackrequest.Application.Id.ToString(), promofeedbackrequest.Application.Version.Major, promofeedbackrequest.TransactionId, promofeedbackrequest.LanguageCode, promofeedbackrequest.SessionId);
            IDisposable timer = null;
            var response = new MOBPromoFeedbackResponse();
            try
            {
                using (timer = _logger.BeginTimedOperation("Total time taken for PromoFeedback business call", transationId: promofeedbackrequest.TransactionId))
                {
                    response = await _updateMemberProfileBusiness.PromoFeedback(promofeedbackrequest);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("PromoFeedback UnitedException {@UnitedException}", JsonConvert.SerializeObject(uaex));
                if (uaex != null && !string.IsNullOrEmpty(uaex.Message.Trim()))
                {
                    response.Exception = new MOBException();
                    response.Exception.Message = uaex.Message;
                }
                else
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("PromoFeedback Error {@Exception}", JsonConvert.SerializeObject(ex));
                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;

            _logger.LogInformation("PromoFeedback {@ClientResponse}", JsonConvert.SerializeObject(response));

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
                request.ServiceName = ServiceNames.UPDATEMEMBERPROFILE.ToString();
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
