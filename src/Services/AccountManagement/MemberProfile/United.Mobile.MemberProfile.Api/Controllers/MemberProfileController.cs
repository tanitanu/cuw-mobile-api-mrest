using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.MemberProfile.Domain;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Utility.Helper;
using United.Utility.Serilog;
using United.Mobile.Model.Common.FeatureSettings;
using United.Mobile.Model;

namespace United.Mobile.MemberProfile.Api.Controllers
{
    [Route("memberprofileservice/api/")]
    [ApiController]
    public class MemberProfileController : ControllerBase
    {
        private readonly ICacheLog<MemberProfileController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly IMemberProfileBusiness _memberProfileBusiness;
        private readonly IFeatureSettings _featureSettings;

        public MemberProfileController(ICacheLog<MemberProfileController> logger
            , IConfiguration configuration
            , IHeaders headers
            , IMemberProfileBusiness memberProfileBusiness
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _headers = headers;
            _memberProfileBusiness = memberProfileBusiness;
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
        //Moved to Signin
        [HttpPost]
        [Route("MileagePlus/GetContactUsDetails")]
        public async Task<MOBContactUsResponse> GetContactUsDetails(MOBContactUsRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, string.Empty);
            var response = new MOBContactUsResponse();
            IDisposable timer = null;

            try
            {
                _logger.LogInformation("GetContactUsDetails {@clientRequest} {DeviceId} {SessionId} {TransactionId}", JsonConvert.SerializeObject(request), request.DeviceId, "", request.TransactionId);

                timer = _logger.BeginTimedOperation("Total time taken for GetContactUsDetails business call", transationId: request.TransactionId);

                response = await _memberProfileBusiness.GetContactUsDetails(request);
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetContactUsDetails Warning {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(uaex), request.TransactionId);
                _logger.LogWarning("GetContactUsDetails Error {@UnitedException} and {transactionId}", uaex.Message, request.TransactionId);
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetContactUsDetails Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), request.TransactionId);
                _logger.LogError("GetContactUsDetails {exception} and {transactionId}", ex.Message, request.TransactionId);
                response.Exception = new MOBException("99999", _configuration.GetValue<string>("GenericExceptionMessage"));
            }

            response.CallDuration = 0;

            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }
            _logger.LogInformation("GetContactUsDetails {@clientResponse} {transactionId}", JsonConvert.SerializeObject(response), request.TransactionId);
            return response;
        }
        //Moved to Signin
        [HttpPost]
        [Route("CustomerProfile/RetrieveCustomerPreferences")]
        public async Task<MOBCustomerPreferencesResponse> RetrieveCustomerPreferences(MOBCustomerPreferencesRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBCustomerPreferencesResponse();
            IDisposable timer = null;

            try
            {
                _logger.LogInformation("RetrieveCustomerPreferences {@clientRequest} {DeviceId} {SessionId} {TransactionId}", JsonConvert.SerializeObject(request), request.DeviceId, request.SessionId, request.TransactionId);

                timer = _logger.BeginTimedOperation("Total time taken for RetrieveCustomerPreferences business call", transationId: request.TransactionId);

                response = await _memberProfileBusiness.RetrieveCustomerPreferences(request);
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("RetrieveCustomerPreferences Warning {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), request.SessionId);
                _logger.LogWarning("RetrieveCustomerPreferences Error {@UnitedException} and {sessionId}", uaex.Message, request.SessionId);
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("RetrieveCustomerPreferences Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), request.SessionId);
                _logger.LogError("RetrieveCustomerPreferences {exception} and {sessionId}", ex.Message, request.SessionId);
                response.Exception = new MOBException("99999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            response.CallDuration = 0;

            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }
            _logger.LogInformation("RetrieveCustomerPreferences {@clientResponse} {transactionId}", JsonConvert.SerializeObject(response), request.TransactionId);
            return response;
        }

        [HttpPost]
        [Route("CustomerProfile/GetProfileOwner")]
        public async Task<MOBCustomerProfileResponse> GetProfileOwner(MOBCustomerProfileRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBCustomerProfileResponse();

            try
            {
                _logger.LogInformation("GetProfileOwner {@clientRequest} {DeviceId} {SessionId} {TransactionId}", JsonConvert.SerializeObject(request), request.DeviceId, request.SessionId, request.TransactionId);

                response = await _memberProfileBusiness.GetProfileOwner(request);
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetProfileOwner Warning {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }

            catch (Exception ex)
            {
                _logger.LogError("GetProfileOwner Error {exception}", JsonConvert.SerializeObject(ex));
                response.Exception = new MOBException("99999", _configuration.GetValue<string>("GenericExceptionMessage"));
            }
            _logger.LogInformation("GetProfileOwner {@clientResponse} {transactionId}", JsonConvert.SerializeObject(response), request.TransactionId);
            return response;
        }
        //Moved to mpSignin
        [HttpPost]
        [Route("MileagePlus/GetAccountSummaryWithMemberCardPremierActivity")]
        public async Task<MPAccountSummaryResponse> GetAccountSummaryWithMemberCardPremierActivity(MPAccountValidationRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application.Id.ToString(), request.Application.Version.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MPAccountSummaryResponse();
            IDisposable timer = null;

            try
            {
                _logger.LogInformation("GetAccountSummaryWithMemberCardPremierActivity {@clientRequest} {DeviceId} {SessionId} {TransactionId}", JsonConvert.SerializeObject(request), request.DeviceId, request.SessionId, request.TransactionId);

                timer = _logger.BeginTimedOperation("Total time taken for GetAccountSummaryWithMemberCardPremierActivity business call", transationId: request.TransactionId);
                response = await _memberProfileBusiness.GetAccountSummaryWithMemberCardPremierActivity(request);
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetAccountSummaryWithMemberCardPremierActivity Warning {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), request.SessionId);
                _logger.LogWarning("GetAccountSummaryWithMemberCardPremierActivity Error {@UnitedException} and {sessionId}", uaex.Message, request.SessionId);
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAccountSummaryWithMemberCardPremierActivity Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), request.SessionId);
                _logger.LogError("GetAccountSummaryWithMemberCardPremierActivity {exception} and {sessionId}", ex.Message, request.SessionId);
                response.Exception = new MOBException("99999", _configuration.GetValue<string>("GenericExceptionMessage"));
            }
            response.CallDuration = 0;

            if (timer != null)
            {
                response.CallDuration = ((TimedOperation)timer).GetElapseTime();
                timer.Dispose();
            }
            _logger.LogInformation("GetAccountSummaryWithMemberCardPremierActivity {@clientResponse} {transactionId}", JsonConvert.SerializeObject(response), request.TransactionId);
            return response;
        }

        [HttpPost]
        [Route("ProfileCSL/GetProfileCSL_CFOP")]
        public async Task<MOBCPProfileResponse> GetProfileCSL_CFOP(MOBCPProfileRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application?.Id.ToString(), request.Application?.Version?.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBCPProfileResponse();

            try
            {
                _logger.LogInformation("GetProfileCSL_CFOP {@ClientRequest}", JsonConvert.SerializeObject(request));
                response = await _memberProfileBusiness.GetProfileCSL_CFOP(request);
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetProfileCSL_CFOP Error {@UnitedException}", JsonConvert.SerializeObject(uaex));

                #region Inhibit Booking
                if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                {
                    if (uaex.InnerException != null && !string.IsNullOrEmpty(uaex.InnerException.Message) && uaex.InnerException.Message.Trim().Equals("10050"))
                    {
                        response.Exception = new MOBException("10050", uaex.Message);
                    }
                }
                #endregion

                if (uaex.Message.Trim() == _configuration.GetValue<string>("bugBountySessionExpiredMsg").Trim())
                {
                    response.Exception = new MOBException();
                    response.Exception.Message = uaex.Message;
                }
                else if (uaex.Message.Trim() == _configuration.GetValue<string>("BookingSessionExpiryMessage").Trim())
                {
                    response.Exception = new MOBException();
                    response.Exception.Message = uaex.Message;
                    if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                    {
                        response.Exception.Code = uaex.Code;
                    }
                }
                else
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProfileCSL_CFOP Error {@Exception}", JsonConvert.SerializeObject(ex));

                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            _logger.LogInformation("GetProfileCSL_CFOP {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost]
        [Route("ProfileCSL/GetPartnerSSOToken")]
        public async Task<MOBPartnerSSOTokenResponse> GetPartnerSSOToken(MOBPartnerSSOTokenRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application?.Id.ToString(), request.Application?.Version?.Major, request.TransactionId, request.LanguageCode, null);
            var response = new MOBPartnerSSOTokenResponse();
            IDisposable timer = null;

            try
            {
                _logger.LogInformation("GetPartnerSSOToken {@ClientRequest}", JsonConvert.SerializeObject(request));

                using (timer = _logger.BeginTimedOperation("Total time taken for GetPartnerSSOToken business call", transationId: request.TransactionId))
                {
                    response = await _memberProfileBusiness.GetPartnerSSOToken(request);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetPartnerSSOToken Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPartnerSSOToken Error {@Exception}", JsonConvert.SerializeObject(ex));
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            response.CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
            _logger.LogInformation("GetPartnerSSOToken {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost]
        [Route("ProfileCSL/GetEmpProfileCSL_CFOP")]
        public async Task<MOBCPProfileResponse> GetEmpProfileCSL_CFOP(MOBCPProfileRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application?.Id.ToString(), request.Application?.Version?.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBCPProfileResponse();
            IDisposable timer = null;

            try
            {
                _logger.LogInformation("GetEmpProfileCSL_CFOP {@ClientRequest}", JsonConvert.SerializeObject(request));

                using (timer = _logger.BeginTimedOperation("Total time taken for GetEmpProfileCSL_CFOP business call", transationId: request.TransactionId))
                {
                    response = await _memberProfileBusiness.GetEmpProfileCSL_CFOP(request);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetEmpProfileCSL_CFOP Error {@UnitedException}", JsonConvert.SerializeObject(uaex));

                #region Inhibit Booking
                if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                {
                    if (uaex.InnerException != null && !string.IsNullOrEmpty(uaex.InnerException.Message) && uaex.InnerException.Message.Trim().Equals("10050"))
                    {
                        response.Exception = new MOBException("10050", uaex.Message);
                    }
                }
                #endregion
                if (uaex.Message.Trim() == _configuration.GetValue<string>("BookingSessionExpiryMessage").ToString().Trim())
                {
                    response.Exception = new MOBException();
                    response.Exception.Message = uaex.Message;
                }
                else
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            catch (Exception ex)
            {
                _logger.LogError("GetEmpProfileCSL_CFOP Error {@Exception}", JsonConvert.SerializeObject(ex));

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
            _logger.LogInformation("GetEmpProfileCSL_CFOP {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost]
        [Route("ProfileCSL/MPSignedInInsertUpdateTraveler_CFOP")]
        public async Task<MOBCPProfileResponse> MPSignedInInsertUpdateTraveler_CFOP(MOBUpdateTravelerRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application?.Id.ToString(), request.Application?.Version?.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBCPProfileResponse();

            try
            {
                _logger.LogInformation("MPSignedInInsertUpdateTraveler_CFOP {@ClientRequest}", JsonConvert.SerializeObject(request));
                response = await _memberProfileBusiness.MPSignedInInsertUpdateTraveler_CFOP(request);
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("MPSignedInInsertUpdateTraveler_CFOP Error {@UnitedException}", JsonConvert.SerializeObject(uaex));

                #region

                if (uaex != null && !string.IsNullOrEmpty(uaex.Message.Trim()))
                {
                    response.Exception = new MOBException();
                    response.Exception.Message = uaex.Message;
                }
                else
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError("MPSignedInInsertUpdateTraveler_CFOP Error {@Exception}", JsonConvert.SerializeObject(ex));
                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }
            _logger.LogInformation("MPSignedInInsertUpdateTraveler_CFOP {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpGet]
        [Route("ProfileCSL/GetLatestFrequentFlyerRewardProgramList")]
        public async Task<FrequentFlyerRewardProgramsResponse> GetLatestFrequentFlyerRewardProgramList(int applicationId, string appVersion, string accessCode, string transactionId, string languageCode)
        {
            await _headers.SetHttpHeader(string.Empty, applicationId.ToString(), appVersion, transactionId, languageCode, string.Empty);
            var response = new FrequentFlyerRewardProgramsResponse();
            try
            {
                _logger.LogInformation("GetLatestFrequentFlyerRewardProgramList {@AccessCode} {@LanguageCode}", accessCode, languageCode);
                response = await _memberProfileBusiness.GetLatestFrequentFlyerRewardProgramList(applicationId, appVersion, accessCode, transactionId, languageCode);
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetLatestFrequentFlyerRewardProgramList {@UnitedException}", JsonConvert.SerializeObject(uaex));

                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }
            catch (System.Exception ex)
            {
                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                _logger.LogError("GetLatestFrequentFlyerRewardProgramList {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));

                response.Exception = new MOBException("9999", _configuration.GetValue<string>("GenericExceptionMessage"));
            }

            _logger.LogInformation("GetLatestFrequentFlyerRewardProgramList {@ClientResponse}", JsonConvert.SerializeObject(response));
            return response;
        }

        [HttpPost]
        [Route("ProfileCSL/UpdateTravelerCCPromo_CFOP")]
        public async Task<MOBCPProfileResponse> UpdateTravelerCCPromo_CFOP(MOBUpdateTravelerRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application?.Id.ToString(), request.Application?.Version?.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBCPProfileResponse();

            try
            {
                _logger.LogInformation("UpdateTravelerCCPromo_CFOP {@ClientRequest}", JsonConvert.SerializeObject(request));
                response = await _memberProfileBusiness.UpdateTravelerCCPromo_CFOP(request);
            }
            catch (MOBUnitedException uaex)
            {
                #region

                MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);
                _logger.LogWarning("UpdateTravelerCCPromo_CFOP {@UnitedException}", JsonConvert.SerializeObject(uaexWrapper));

                if (uaex != null && !string.IsNullOrEmpty(uaex.Message.Trim()))
                {
                    response.Exception = new MOBException();
                    response.Exception.Message = uaex.Message;
                }
                else
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                #endregion
            }
            catch (System.Exception ex)
            {
                #region

                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);

                _logger.LogError("UpdateTravelerCCPromo_CFOP {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));

                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
                #endregion
            }

            _logger.LogInformation("UpdateTravelerCCPromo_CFOP {@ClientResponse}", JsonConvert.SerializeObject(response));

            return response;
        }

        [HttpPost]
        [Route("ProfileCSL/UpdateTravelersInformation")]
        public async Task<MOBUpdateTravelerInfoResponse> UpdateTravelersInformation(MOBUpdateTravelerInfoRequest request)
        {
            await _headers.SetHttpHeader(request.DeviceId, request.Application?.Id.ToString(), request.Application?.Version?.Major, request.TransactionId, request.LanguageCode, request.SessionId);
            var response = new MOBUpdateTravelerInfoResponse();

            try
            {
                _logger.LogInformation("UpdateTravelersInformation {@clientRequest} {DeviceId} {SessionId} {TransactionId}", JsonConvert.SerializeObject(request), request.DeviceId, request.SessionId, request.TransactionId);
                response = await _memberProfileBusiness.UpdateTravelersInformation(request);
            }
            catch (MOBUnitedException uaex)
            {
                MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);
                _logger.LogWarning("UpdateTravelersInformation {MOBUnitedException} and {SessionId}", uaexWrapper, request.SessionId);

                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception = new MOBException();
                    response.Exception.Message = uaex.Message;
                    response.Exception.Code = uaex.Code;
                }
                else
                {

                    response.Exception = new MOBException();
                    response.Exception.Message = _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                }
            }
            catch (System.Exception ex)
            {
                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                _logger.LogError("UpdateTravelersInformation {Exception} and {SessionId}", exceptionWrapper, request.SessionId);

                if ((request.TravelersInfo[0].MileagePlus != null && !string.IsNullOrEmpty(request.TravelersInfo[0].MileagePlus.MileagePlusId)) ||
                    (request.TravelersInfo[0].OaRewardPrograms != null && !string.IsNullOrEmpty(request.TravelersInfo[0].OaRewardPrograms[0].ProgramMemberId)))
                {
                    response.Exception = new MOBException
                        ("9999", _configuration.GetValue<string>("ValidateMPNameMisMatchErrorMessage"));
                }
                else
                    response.Exception = new MOBException
                        ("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            _logger.LogInformation("UpdateTravelersInformation {Response}", JsonConvert.SerializeObject(response));

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
                request.ServiceName = ServiceNames.MEMBERPROFILE.ToString();
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