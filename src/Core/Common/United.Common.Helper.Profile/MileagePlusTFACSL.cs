using Css.SecureProfile;
using Css.SecureProfile.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using United.Foundations.Practices.Framework.Security.DataPower.Models;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Service.Presentation.SecurityRequestModel;
using United.Service.Presentation.SecurityResponseModel;
using United.Services.Customer.Common;
using United.Utility.Helper;
using Genre = United.Service.Presentation.CommonModel.Genre;

namespace United.Common.Helper.Profile
{
    public class MileagePlusTFACSL : IMileagePlusTFACSL
    {
        private readonly IConfiguration _configuration;
        private readonly ICustomerDataService _customerDataService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMPSecurityQuestionsService _mPSecurityQuestionsService;
        private readonly ICacheLog<MileagePlusTFACSL> _logger;
        private readonly IHeaders _headers;
        public MileagePlusTFACSL(ICacheLog<MileagePlusTFACSL> logger,
            IConfiguration configuration,
            IMPSecurityQuestionsService mPSecurityQuestionsService,
            ICustomerDataService customerDataService
            , IHttpContextAccessor httpContextAccessor
            , IHeaders headers)
        {
            _logger = logger;
            _configuration = configuration;
            _mPSecurityQuestionsService = mPSecurityQuestionsService;
            _customerDataService = customerDataService;
            _httpContextAccessor = httpContextAccessor;
            _headers = headers;
            
        }
        public async Task<bool> GetTfaWrongAnswersFlag(string sessionid, string token, int customerId, string mileagePlusNumber, bool answeredQuestionsIncorrectly, string languageCode)
        {

            if (string.IsNullOrEmpty(mileagePlusNumber))
            {
                throw new MOBUnitedException("MPNumber request cannot be null.");
            }

            //string url = string.Format("{0}/GetTfaWrongAnswersFlag", _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLProfile"));
            string path = string.Format("/GetTfaWrongAnswersFlag");
            //if (traceSwitch.TraceError)
            //{
            //    string request = "customerId:" + customerId + ", mileagePlusNumber:" + mileagePlusNumber + ",answeredQuestionsIncorrectly:" + answeredQuestionsIncorrectly.ToString() + ",languageCode:" + languageCode;
            //    //Logging equest
            //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionid, "GetTfaWrongAnswersFlag", "Request", request));
            //    //URL Logging
            //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionid, "GetTfaWrongAnswersFlag", "Request url for Is Device Valid", url));
            //}

            United.Services.Customer.Common.UpdateWrongAnswersFlagRequest updatewronganswersflagrequest = new Services.Customer.Common.UpdateWrongAnswersFlagRequest();
            updatewronganswersflagrequest.AnsweredQuestionsIncorrectly = answeredQuestionsIncorrectly;
            updatewronganswersflagrequest.CustomerId = customerId;
            updatewronganswersflagrequest.LoyaltyId = mileagePlusNumber;
            updatewronganswersflagrequest.LangCode = languageCode;
            updatewronganswersflagrequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices");

            string jsonRequest = JsonConvert.SerializeObject(updatewronganswersflagrequest);

            #region//****GetTfaWrongAnswersFlag Call Duration Code*******
            Stopwatch cslStopWatch;
            cslStopWatch = new Stopwatch();
            cslStopWatch.Reset();
            cslStopWatch.Start();
            #endregion//****GetTfaWrongAnswersFlag Call Duration Code - Srini 02/19/2016*******

            //string jsonResponse = HttpHelper.Post(url, "application/json; charset=utf-8", token, jsonRequest);
            var response =await _customerDataService.InsertMPEnrollment<SearchResponse>(token, jsonRequest, _headers.ContextValues.SessionId, path).ConfigureAwait(false);

            #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cslStopWatch.IsRunning)
            {
                cslStopWatch.Stop();
            }
            string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
            //if (traceSwitch.TraceError)
            //{
            //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionid, "GetTfaWrongAnswersFlag", "CSS/CSL-CallDuration", "GetTfaWrongAnswersFlag=" + cslCallTime));
            //}
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            bool retValue = false;

            if (response != null)
            {
                retValue = response.Result;

                //    if (response != null && (response.Errors == null || response.Errors.Count() == 0))
                //    {
                //        logEntries.Add(LogEntry.GetLogEntry<SearchResponse>(sessionid, "GetTfaWrongAnswersFlag", "Response", response));
                //    }
                //    else
                //    {
                //        string errorMessage = string.Empty;
                //        foreach (var error in response.Errors)
                //        {
                //            if (!string.IsNullOrEmpty(error.UserFriendlyMessage))
                //            {
                //                errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                //            }
                //            else
                //            {
                //                errorMessage = errorMessage + " " + error.Message;
                //            }
                //        }

                //        string exceptionmessage = !string.IsNullOrEmpty(errorMessage) ? errorMessage : "Unable to Get TFA Wrong Answers Flag.";

                //        logEntries.Add(LogEntry.GetLogEntry<string>(sessionid, "GetTfaWrongAnswersFlag", "Exception", exceptionmessage));
                //        throw new MOBUnitedException(exceptionmessage);
                //    }

            }

            return retValue; // response;
        }
        private String GetClientIPAddress()
        {
            string clientIP = string.Empty;

            if (_configuration.GetValue<bool>("CSSDP_Get_ClientIP"))
            {
                //checking network availability for getting client host ipAddress.
                if (!string.IsNullOrEmpty(_httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString()))
                {
                    clientIP = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
                }
                else
                {   //Assigning empty client ip address, if there is no network
                    clientIP = _configuration.GetValue<string>("RequestAttribute_ClientIP");
                }
            }
            else
            {
                clientIP = _configuration.GetValue<String>("RequestAttribute_ClientIP");
            }

            return clientIP;
        }
        public DpRequest GetDPRequestObject(int applicationID, string deviceId)
        {
            DpRequest dpRequest = null;
            string _deviceId = ValidDeviceIDforDP(deviceId, isGuestType: true);
            switch (applicationID)
            {
                case 1:
                    dpRequest = new DpRequest
                    {

                        GrantType = "client_credentials",
                        ClientId = _configuration.GetValue<string>("iOS_ClientId_DP").ToString(),
                        ClientSecret = _configuration.GetValue<string>("iOS_ClientSecret_DP").ToString(),
                        Scope = _configuration.GetValue<string>("iOS_Scope_DP").ToString(),
                        UserType = "guest",
                        EndUserAgentId = _deviceId,
                        EndUserAgentIP = GetClientIPAddress()
                        //AccessToken="",
                        //UserName = "",
                        //Password = "",
                        //RefreshToken ="",
                        //TokenTypeHint="",
                        //Nonce = ""
                    };
                    break;

                case 2:
                    dpRequest = new DpRequest
                    {
                        GrantType = "client_credentials",
                        ClientId = _configuration.GetValue<string>("Android_ClientId_DP").ToString(),
                        ClientSecret = _configuration.GetValue<string>("Android_ClientSecret_DP").ToString(),
                        Scope = _configuration.GetValue<string>("Android_Scope_DP").ToString(),
                        UserType = "guest",
                        EndUserAgentId = _deviceId,
                        EndUserAgentIP = GetClientIPAddress()
                        //AccessToken = "",
                        //UserName = "",
                        //Password = "",
                        //RefreshToken = "",
                        //TokenTypeHint = "",
                        //Nonce = ""
                    };
                    break;
                case 3:
                    dpRequest = new DpRequest
                    {
                        GrantType = "client_credentials",
                        ClientId = _configuration.GetValue<string>("Windows_ClientId_DP").ToString(),
                        ClientSecret = _configuration.GetValue<string>("Windows_ClientSecret_DP").ToString(),
                        Scope = _configuration.GetValue<string>("Windows_Scope_DP").ToString(),
                        UserType = "guest",
                        EndUserAgentId = _deviceId,
                        EndUserAgentIP = GetClientIPAddress()
                        //AccessToken = "",
                        //UserName = "",
                        //Password = "",
                        //RefreshToken = "",
                        //TokenTypeHint = "",
                        //Nonce = ""
                    };
                    break;
                case 6:
                    dpRequest = new DpRequest
                    {
                        GrantType = "client_credentials",
                        ClientId = _configuration.GetValue<string>("Mobile_ClientId_DP").ToString(),
                        ClientSecret = _configuration.GetValue<string>("Mobile_ClientSecret_DP").ToString(),
                        Scope = _configuration.GetValue<string>("Mobile_Scope_DP").ToString(),
                        UserType = "guest",
                        EndUserAgentId = _deviceId,
                        EndUserAgentIP = GetClientIPAddress()
                        //AccessToken = "",
                        //UserName = "",
                        //Password = "",
                        //RefreshToken = "",
                        //TokenTypeHint = "",
                        //Nonce = ""
                    };
                    break;
                case 32:
                    dpRequest = new DpRequest
                    {

                        GrantType = "client_credentials",
                        ClientId = _configuration.GetValue<string>("iOS_ClientId_DP").ToString(),
                        ClientSecret = _configuration.GetValue<string>("iOS_ClientSecret_DP").ToString(),
                        Scope = _configuration.GetValue<string>("iOS_Scope_DP").ToString(),
                        UserType = "guest",
                        EndUserAgentId = _deviceId,
                        EndUserAgentIP = GetClientIPAddress()
                        //AccessToken="",
                        //UserName = "",
                        //Password = "",
                        //RefreshToken ="",
                        //TokenTypeHint="",
                        //Nonce = ""
                    };
                    break;
                default:
                    break;
            }
            return dpRequest;
        }
        public string ValidDeviceIDforDP(string deviceId, bool isGuestType = false)
        {
            string _deviceId = "";

            if ((_configuration.GetValue<string>("SendDeviceIDatDPTokenGenerationRequest") != null && Convert.ToBoolean(_configuration.GetValue<string>("SendDeviceIDatDPTokenGenerationRequest").ToString()))
                || (isGuestType && _configuration.GetValue<bool>("EnableEndUserAgentIdInDPTokenRequestForGuestUser")))
            {
                _deviceId = !string.IsNullOrWhiteSpace(deviceId) ? deviceId : "";
                if (!string.IsNullOrWhiteSpace(_deviceId))
                {
                    string[] invalidDeviceIDList = null;
                    if (_configuration.GetValue<string>("DPInvalidDeviceIDList") != null)
                    {
                        invalidDeviceIDList = _configuration.GetValue<string>("DPInvalidDeviceIDList").ToString().Split(',');
                    }
                    if ((invalidDeviceIDList != null && invalidDeviceIDList.Contains(_deviceId, StringComparer.OrdinalIgnoreCase)) || (_deviceId.Length != 36))
                    {
                        _deviceId = "";
                    }
                }
            }
            return _deviceId;
        }
        public async Task<bool> ValidateDevice(Session session, string appVersion, string languageCode)
        {
            bool StatusFlag = false;

            if (_configuration.GetValue<bool>("EnableDPToken"))
            {

                if (string.IsNullOrEmpty(session.MileagPlusNumber) || string.IsNullOrEmpty(session.DeviceID))
                {
                    //logEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(session.SessionId, "ValidateDevice", "MOBUnitedException", session.AppID, appVersion, session.DeviceID, "MPNumber or DeviceID in sesion is null."));
                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage").ToString());
                }


                Collection<Genre> dbTokens = new Collection<Genre> { new Genre { Key = "DBEnvironment", Value = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices").ToString() }, new Genre { Key = "LangCode", Value = languageCode } };
                Service.Presentation.SecurityRequestModel.ValidateDeviceRequest _validateDeviceRequest = new Service.Presentation.SecurityRequestModel.ValidateDeviceRequest
                {
                    ApplicationId = GetDPRequestObject(session.AppID, session.DeviceID).ClientId,
                    CustomerId = session.CustomerID,
                    DeviceId = session.DeviceID,
                    MileagePlusId = session.MileagPlusNumber,
                    Tokens = dbTokens
                };

                string jsonRequest = JsonConvert.SerializeObject(_validateDeviceRequest);
                //string url = string.Format("{0}/ValidateDevice", _configuration.GetValue<string>("CslSecureProfileURL"));
                string path = string.Format("/ValidateDevice");
                //if (traceSwitch.TraceError)
                //{
                //    //Logging equest
                //    logEntries.Add(LogEntry.GetLogEntry<Persist.Definition.Shopping.Session>(session.SessionId, "ValidateDevice", "Request", session.AppID, appVersion, session.DeviceID, session));
                //    //URL Logging
                //    logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "ValidateDevice", "Request url for Is Device Valid", session.AppID, appVersion, session.DeviceID, url));
                //}

                #region//****ValidateDevice Call Duration Code*******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****ValidateDevice Call Duration Code - Srini 02/19/2016*******


                //string jsonResponse = HttpHelper.Post(url, "application/json; charset=utf-8", session.Token, jsonRequest);
                var response = await _mPSecurityQuestionsService.ValidateSecurityAnswer<ValidateDeviceResponse>(session.Token, jsonRequest, session.SessionId, path).ConfigureAwait(false);


                #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "ValidateDevice", "CSS/CSL-CallDuration", session.AppID, appVersion, session.DeviceID, "ValidateDevice=" + cslCallTime));
                //}
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

                if (response != null)
                {

                    //if (traceSwitch.TraceError)
                    //{
                    //    if (response != null && string.IsNullOrEmpty(response.ExceptionCode))
                    //    {
                    //        logEntries.Add(LogEntry.GetLogEntry<United.Service.Presentation.SecurityResponseModel.ValidateDeviceResponse>(session.SessionId, "ValidateDevice", "DeSerialized Response", session.AppID, appVersion, session.DeviceID, response));
                    //    }
                    //    else
                    //    {
                    //        string exceptionmessage = string.IsNullOrEmpty(response.ExceptionCode) ? response.ExceptionCode : "Unable to Validate Device.";
                    //        logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "ValidateDevice", "Exception", session.AppID, appVersion, session.DeviceID, jsonResponse));
                    //        throw new MOBUnitedException(exceptionmessage);
                    //    }
                    //}

                    StatusFlag = response.IsAuthenticated;

                }

            }
            else
            {
                if (string.IsNullOrEmpty(session.MileagPlusNumber) || string.IsNullOrEmpty(session.DeviceID))
                {
                    //logEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(session.SessionId, "ValidateDevice", "MOBUnitedException", session.AppID, appVersion, session.DeviceID, "MPNumber or DeviceID in sesion is null."));
                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage").ToString());
                }

                System.Guid appID = GetApplicationID(session.AppID, session.SessionId, "ValidateDevice");

                string url = string.Format("{0}", _configuration.GetValue<string>("CssSecureProfileURL"));
                //if (traceSwitch.TraceError)
                //{
                //    //Logging equest
                //    logEntries.Add(LogEntry.GetLogEntry<Persist.Definition.Shopping.Session>(session.SessionId, "ValidateDevice", "Request", session.AppID, appVersion, session.DeviceID, session));
                //    //URL Logging
                //    logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "ValidateDevice", "Request url for Is Device Valid", session.AppID, appVersion, session.DeviceID, url));
                //}

                #region//****ValidateDevice Call Duration Code*******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****ValidateDevice Call Duration Code - Srini 02/19/2016*******


                List<Metadata> dbTokens = new List<Metadata> { new Metadata("DBEnvironment", _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices").ToString()), new Metadata("LangCode", languageCode) };
                SecureProfileClient proxy = new SecureProfileClient(url, session.Token);
                ValidateDeviceCallWrapper response = proxy.ValidateDevice(session.CustomerID, session.MileagPlusNumber, appID.ToString(), session.DeviceID, dbTokens);

                #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "ValidateDevice", "CSS/CSL-CallDuration", session.AppID, appVersion, session.DeviceID, "ValidateDevice=" + cslCallTime));
                //}
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

                //if (traceSwitch.TraceError)
                //{
                //    if (response != null && string.IsNullOrEmpty(response.ExceptionCode))
                //    {
                //        logEntries.Add(LogEntry.GetLogEntry<ValidateDeviceCallWrapper>(session.SessionId, "ValidateDevice", "DeSerialized Response", session.AppID, appVersion, session.DeviceID, response));
                //    }
                //    else
                //    {
                //        string exceptionmessage = string.IsNullOrEmpty(response.ExceptionCode) ? response.ExceptionCode : "Unable to Validate Device.";
                //        logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "ValidateDevice", "Exception", session.AppID, appVersion, session.DeviceID, exceptionmessage));
                //        throw new MOBUnitedException(exceptionmessage);
                //    }
                //}

                StatusFlag = response.IsAuthenticated;

            }
            return StatusFlag;
        }
        /// <summary>
        /// Validating DeviceID is Valid Or Not
        /// </summary>
        /// <returns>ValidateDeviceCallWrapper with isauthenticated or not with eorr message and code</returns>
        public async Task<bool> AddDeviceAuthentication(Session session, string appVersion, string languageCode)
        {
            bool StatusFlag = false;

            if (_configuration.GetValue<bool>("EnableDPToken"))
            {

                if (string.IsNullOrEmpty(session.MileagPlusNumber) || string.IsNullOrEmpty(session.DeviceID))
                {
                    //logEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(session.SessionId, "AddDeviceAuthentication", "MOBUnitedException", session.AppID, appVersion, session.DeviceID, "MPNumber or DeviceID in sesion is null."));

                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                string originatingInfo = "::1", authenticationMethod = "sign-in", insertId = "";

                Collection<Genre> dbTokens = new Collection<Genre> { new Genre { Key = "DBEnvironment", Value = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices") }, new Genre { Key = "LangCode", Value = languageCode } };
                insertId = session.CustomerID.ToString();
                if (_configuration.GetValue<string>("PassDeviceIDForOriginatingInfo") != null
                    &&
                    Convert.ToBoolean(_configuration.GetValue<string>("PassDeviceIDForOriginatingInfo")))
                {
                    originatingInfo = session.DeviceID;
                }

                DeviceAuthenticationRequest _DeviceAuthRequest = new DeviceAuthenticationRequest
                {
                    ApplicationId = GetDPRequestObject(session.AppID, session.DeviceID).ClientId,
                    AuthenticationMethod = authenticationMethod,
                    CustomerId = session.CustomerID,
                    DeviceId = session.DeviceID,
                    InsertId = insertId,
                    MileagePlusId = session.MileagPlusNumber,
                    OriginatingInfo = originatingInfo,
                    Tokens = dbTokens
                };
                string jsonRequest = Newtonsoft.Json.JsonConvert.SerializeObject(_DeviceAuthRequest);
                string path = "/AddDeviceAuthentication";
                //TO DO
                //if (traceSwitch.TraceError)
                //{
                //    //Logging equest
                //    logEntries.Add(LogEntry.GetLogEntry<United.Service.Presentation.SecurityRequestModel.DeviceAuthenticationRequest>(session.SessionId, "AddDeviceAuthentication", "Request", session.AppID, appVersion, session.DeviceID, _DeviceAuthRequest));
                //    //URL Logging
                //    logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "AddDeviceAuthentication", "Request url for Is Device Valid", session.AppID, appVersion, session.DeviceID, url));
                //}

                #region//****AddDeviceAuthentication Call Duration Code*******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****AddDeviceAuthentication Call Duration Code - Srini 02/19/2016*******

                //string jsonResponse = HttpHelper.Post(url, "application/json; charset=utf-8", session.Token, jsonRequest);
                string jsonResponse = await _mPSecurityQuestionsService.AddDeviceAuthentication(session.Token, jsonRequest, session.SessionId, path);

                #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "AddDeviceAuthentication", "CSS/CSL-CallDuration", session.AppID, appVersion, session.DeviceID, "AddDeviceAuthentication=" + cslCallTime));
                //}
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    var response = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceAuthenticationResponse>(jsonResponse);
                    //TO DO
                    //if (traceSwitch.TraceError)
                    //{
                    //    if (response != null && string.IsNullOrEmpty(response.ExceptionCode))
                    //    {
                    //        logEntries.Add(LogEntry.GetLogEntry<DeviceAuthenticationResponse>(session.SessionId, "AddDeviceAuthentication", "DeSerialized Response", response));
                    //    }
                    //    else
                    //    {
                    //        string exceptionmessage = string.IsNullOrEmpty(response.ExceptionCode) ? response.ExceptionCode : "Unable to Add Device.";
                    //        logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "AddDeviceAuthentication", "Exception", session.AppID, appVersion, session.DeviceID, jsonResponse));
                    //        throw new MOBUnitedException(exceptionmessage);
                    //    }
                    //}
                    StatusFlag = response.Success;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(session.MileagPlusNumber) || string.IsNullOrEmpty(session.DeviceID))
                {
                    //logEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(session.SessionId, "AddDeviceAuthentication", "MOBUnitedException", session.AppID, appVersion, session.DeviceID, "MPNumber or DeviceID in sesion is null."));

                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                System.Guid cssApplicatinID = GetApplicationID(session.AppID, session.SessionId, "AddDeviceAuthentication");

                string url = string.Format("{0}", _configuration.GetValue<string>("CssSecureProfileURL"));
                //if (traceSwitch.TraceError)
                //{
                //    //Logging equest
                //    logEntries.Add(LogEntry.GetLogEntry<Persist.Definition.Shopping.Session>(session.SessionId, "AddDeviceAuthentication", "Request", session.AppID, appVersion, session.DeviceID, session));
                //    //URL Logging
                //    logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "AddDeviceAuthentication", "Request url for Is Device Valid", session.AppID, appVersion, session.DeviceID, url));
                //}

                string originatingInfo = "::1", authenticationMethod = "sign-in", insertId = "";

                #region//****AddDeviceAuthentication Call Duration Code*******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****AddDeviceAuthentication Call Duration Code - Srini 02/19/2016*******


                List<Metadata> dbTokens = new List<Metadata> { new Metadata("DBEnvironment", _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices")), new Metadata("LangCode", languageCode) };
                SecureProfileClient proxy = new SecureProfileClient(url, session.Token);
                if (_configuration.GetValue<string>("PassDeviceIDForOriginatingInfo") != null
                    &&
                    Convert.ToBoolean(_configuration.GetValue<string>("PassDeviceIDForOriginatingInfo")))
                {
                    originatingInfo = session.DeviceID;
                }
                insertId = session.CustomerID.ToString();
                AddDeviceAuthenticationCallWrapper response = proxy.AddDeviceAuthentication(session.CustomerID, session.MileagPlusNumber, cssApplicatinID.ToString(), session.DeviceID, authenticationMethod, originatingInfo, insertId, dbTokens);

                #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "AddDeviceAuthentication", "CSS/CSL-CallDuration", session.AppID, appVersion, session.DeviceID, "AddDeviceAuthentication=" + cslCallTime));
                //}
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******
                //to do
                //if (traceSwitch.TraceError)
                //{
                //    if (response != null && string.IsNullOrEmpty(response.ExceptionCode))
                //    {
                //        logEntries.Add(LogEntry.GetLogEntry<AddDeviceAuthenticationCallWrapper>(session.SessionId, "AddDeviceAuthentication", "DeSerialized Response", response));
                //    }
                //    else
                //    {
                //        string exceptionmessage = string.IsNullOrEmpty(response.ExceptionCode) ? response.ExceptionCode : "Unable to Add Device.";
                //        logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "AddDeviceAuthentication", "Exception", session.AppID, appVersion, session.DeviceID, exceptionmessage));
                //        throw new MOBUnitedException(exceptionmessage);
                //    }
                //}

                StatusFlag = response.Success;
            }
            return StatusFlag;
        }
        //public List<Securityquestion> GetMPPinPwdSavedSecurityQuestions(string token, int customerId, string mileagePlusNumber, string sessionId, int appId, string appVersion, string deviceId)
        //{
        //    List<Securityquestion> securityQuestions = null;

        //    if (_configuration.GetValue<bool>("EnableDPToken"))
        //    {
        //        //string url = string.Format("{0}/GetSavedSecurityQuestions", _configuration.GetValue<string>("CslSecureProfileURL"));
        //        string path = string.Format("/GetSavedSecurityQuestions");

        //        //if (traceSwitch.TraceError)
        //        //{
        //        //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetMPPinPwdSavedSecurityQuestions - URL for GetSavedSecurityQuestions", "Trace", appId, appVersion, deviceId, url));
        //        //}

        //        Collection<Genre> dbTokens = new Collection<Genre> { new Genre { Key = "DBEnvironment", Value = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices").ToString() }, new Genre { Key = "LangCode", Value = "en-US" } };

        //        Service.Presentation.SecurityRequestModel.SavedSecurityQuestionsRequest _SavedSecurityQuestionsRequest = new Service.Presentation.SecurityRequestModel.SavedSecurityQuestionsRequest
        //        {
        //            CustomerId = customerId,
        //            MileagePlusId = mileagePlusNumber,
        //            Tokens = dbTokens
        //        };

        //        string jsonRequest = DataContextJsonSerializer.Serialize<Service.Presentation.SecurityRequestModel.SavedSecurityQuestionsRequest>(_SavedSecurityQuestionsRequest);

        //        //if (traceSwitch.TraceError)
        //        //{
        //        //    logEntries.Add(LogEntry.GetLogEntry<Collection<Genre>>(sessionId, "GetMPPinPwdSavedSecurityQuestions - Request for GetSavedSecurityQuestions", "Trace", appId, appVersion, deviceId, dbTokens));
        //        //}

        //        #region//****Get Call Duration Code - *******
        //        Stopwatch cslStopWatch;
        //        cslStopWatch = new Stopwatch();
        //        cslStopWatch.Reset();
        //        cslStopWatch.Start();
        //        #endregion//****Get Call Duration Code *******

        //        #region// 2 = cslStopWatch//****Get Call Duration Code *******
        //        if (cslStopWatch.IsRunning)
        //        {
        //            cslStopWatch.Stop();
        //        }
        //        string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
        //        //if (traceSwitch.TraceError)
        //        //{
        //        //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetMPPinPwdSavedSecurityQuestions - CSL Call Duration", "CSS/CSL-CallDuration", appId, appVersion, deviceId, "CSL GetMPPinPwdSavedSecurityQuestions=" + cslCallTime));
        //        //}

        //        #endregion//****Get Call Duration Code - *******
        //        ///
        //        //string jsonResponse = HttpHelper.Post(url, "application/json; charset=utf-8", token, jsonRequest);
        //        string jsonResponse = _mPSecurityQuestionsService.ValidateSecurityAnswer(token, jsonRequest, sessionId, path);


        //        if (!string.IsNullOrEmpty(jsonResponse))
        //        {
        //            SavedSecurityQuestionsResponse response = DataContextJsonSerializer.NewtonSoftDeserialize<SavedSecurityQuestionsResponse>(jsonResponse);

        //            //if (traceSwitch.TraceError)
        //            //{
        //            //    if (response != null && string.IsNullOrEmpty(response.ExceptionCode))
        //            //    {
        //            //        logEntries.Add(LogEntry.GetLogEntry<United.Service.Presentation.SecurityResponseModel.SavedSecurityQuestionsResponse>(sessionId, "GetMPPinPwdSavedSecurityQuestions - Response for GetSavedSecurityQuestions", "DeSerialized Response", appId, appVersion, deviceId, response));
        //            //    }
        //            //    else
        //            //    {
        //            //        string exceptionmessage = string.IsNullOrEmpty(response.ExceptionCode) ? response.ExceptionCode : "The account information you entered is incorrect.";
        //            //        exceptionmessage = ConfigurationManager.AppSettings["ValidateMPSignInGetSavedSecurityQuestionsErrorMessage"] != null && ConfigurationManager.AppSettings["ValidateMPSignInGetSavedSecurityQuestionsErrorMessage"] != "" ? ConfigurationManager.AppSettings["ValidateMPSignInGetSavedSecurityQuestionsErrorMessage"].ToString() : "The account information you entered is incorrect.";
        //            //        //string exceptionmessage = string.IsNullOrEmpty(response.ExceptionCode) ? response.ExceptionCode : "Unable to Validate Device.";
        //            //        logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetMPPinPwdSavedSecurityQuestions - Response for GetSavedSecurityQuestions", "Exception", appId, appVersion, deviceId, jsonResponse));
        //            //        throw new MOBUnitedException(exceptionmessage);
        //            //    }
        //            //}
        //            securityQuestions = ConvertToCSSSecurityList(response.Questions);
        //        }
        //    }

        //    else
        //    {
        //        string url = _configuration.GetValue<string>("CssSecureProfileURL").ToString();

        //        //if (traceSwitch.TraceError)
        //        //{
        //        //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetMPPinPwdSavedSecurityQuestions - URL for GetSavedSecurityQuestions", "Trace", appId, appVersion, deviceId, url));
        //        //}
        //        List<Metadata> dbTokens = new List<Metadata> { new Metadata("DBEnvironment", _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices").ToString()), new Metadata("LangCode", "en-US") };

        //        //if (traceSwitch.TraceError)
        //        //{
        //        //    logEntries.Add(LogEntry.GetLogEntry<List<Metadata>>(sessionId, "GetMPPinPwdSavedSecurityQuestions - Request for GetSavedSecurityQuestions", "Trace", appId, appVersion, deviceId, dbTokens));
        //        //}

        //        #region//****Get Call Duration Code - *******
        //        Stopwatch cslStopWatch;
        //        cslStopWatch = new Stopwatch();
        //        cslStopWatch.Reset();
        //        cslStopWatch.Start();
        //        #endregion//****Get Call Duration Code *******

        //        SecureProfileClient proxy = new SecureProfileClient(url, token);
        //        GetSavedSecurityQuestionsCallWrapper savedQuestions = proxy.GetSavedSecurityQuestions(customerId, mileagePlusNumber.ToUpper(), dbTokens);

        //        #region// 2 = cslStopWatch//****Get Call Duration Code *******
        //        if (cslStopWatch.IsRunning)
        //        {
        //            cslStopWatch.Stop();
        //        }
        //        string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
        //        //if (traceSwitch.TraceError)
        //        //{
        //        //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetMPPinPwdSavedSecurityQuestions - CSL Call Duration", "CSS/CSL-CallDuration", appId, appVersion, deviceId, "CSL GetMPPinPwdSavedSecurityQuestions=" + cslCallTime));
        //        //}

        //        #endregion//****Get Call Duration Code - *******


        //        if (string.IsNullOrEmpty(savedQuestions.ExceptionMessage))
        //        {
        //            securityQuestions = DataContextJsonSerializer.NewtonSoftDeserialize<List<Securityquestion>>(DataContextJsonSerializer.NewtonSoftSerializeToJson<List<Question>>(savedQuestions.Questions));

        //            //if (traceSwitch.TraceError)
        //            //{
        //            //    logEntries.Add(LogEntry.GetLogEntry<List<Question>>(sessionId, "GetMPPinPwdSavedSecurityQuestions - Client Response for get SavedSecurityQuestions", "Trace", appId, appVersion, deviceId, savedQuestions.Questions));
        //            //}
        //        }
        //        else
        //        {
        //            throw new MOBUnitedException(savedQuestions.ExceptionMessage);
        //        }
        //    }
        //    return securityQuestions;
        //}
        public async Task<List<Securityquestion>> GetMPPINPWDSecurityQuestions(string token, string sessionId, int appId, string appVersion, string deviceId)
        {
            List<Securityquestion> securityQuestions = new List<Securityquestion>();

            if (_configuration.GetValue<bool>("EnableDPToken"))
            {
                #region
                // string url = string.Format("{0}/GetAllSecurityQuestions", ConfigurationManager.AppSettings["CslSecureProfileURL"]); ;
                //< add key = "CslSecureProfileURL" value = "https://csmc.stage.api.united.com/8.0/security/SecureProfile/api" />

                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetMPPINPWDSecurityQuestions - URL for GetAllSecurityQuestions", "Trace", appId, appVersion, deviceId, url));
                //}

                Collection<Genre> dbTokens = new Collection<Genre> { new Genre { Key = "DBEnvironment", Value = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices") }, new Genre { Key = "LangCode", Value = "en-US" } };

                Service.Presentation.SecurityRequestModel.SecurityQuestionsRequest _SecurityQuestionsRequest = new Service.Presentation.SecurityRequestModel.SecurityQuestionsRequest
                {
                    Tokens = dbTokens,
                };
                string jsonRequest = DataContextJsonSerializer.Serialize<Service.Presentation.SecurityRequestModel.SecurityQuestionsRequest>(_SecurityQuestionsRequest);

                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<Collection<Genre>>(sessionId, "GetMPPINPWDSecurityQuestions - Request for GetAllSecurityQuestions", "Trace", appId, appVersion, deviceId, dbTokens));
                //}

                #region//****Get Call Duration Code - *******
                //Stopwatch cslStopWatch;
                //cslStopWatch = new Stopwatch();
                //cslStopWatch.Reset();
                //cslStopWatch.Start();
                #endregion//****Get Call Duration Code *******
                #region// 2 = cslStopWatch//****Get Call Duration Code *******
                //if (cslStopWatch.IsRunning)
                //{
                //    cslStopWatch.Stop();
                //}
                //string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetMPPINPWDSecurityQuestions - CSL Call Duration", "CSS/CSL-CallDuration", appId, appVersion, deviceId, "CSL GetMPPinPwdSavedSecurityQuestions=" + cslCallTime));
                //}

                #endregion//****Get Call Duration Code - *******
                ///
                //string jsonResponse = HttpHelper.Post(url, "application/json; charset=utf-8", token, jsonRequest);

                string jsonResponse = await _mPSecurityQuestionsService.GetMPPINPWDSecurityQuestions(token, jsonRequest, sessionId);

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    SecurityQuestionsResponse response = Newtonsoft.Json.JsonConvert.DeserializeObject<SecurityQuestionsResponse>(jsonResponse);

                    //if (traceSwitch.TraceError)
                    //{
                    if (response != null && string.IsNullOrEmpty(response.ExceptionCode))
                    {
                        _logger.LogInformation("GetMPPINPWDSecurityQuestions - Response for GetAllSecurityQuestions {Response} {transactionId}", jsonResponse, _headers.ContextValues.TransactionId);
                        //logEntries.Add(LogEntry.GetLogEntry<United.Service.Presentation.SecurityResponseModel.SecurityQuestionsResponse>(sessionId, "GetMPPINPWDSecurityQuestions - Response for GetAllSecurityQuestions", "DeSerialized Response", appId, appVersion, deviceId, response));
                    }
                    else
                    {
                        string exceptionmessage = string.IsNullOrEmpty(response.ExceptionCode) ? response.ExceptionCode : "Unable to Validate Device.";
                        //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetMPPINPWDSecurityQuestions - Response for GetAllSecurityQuestions", "Exception", appId, appVersion, deviceId, jsonResponse));
                        //throw new MOBUnitedException(exceptionmessage);

                        _logger.LogError("GetMPPINPWDSecurityQuestions - Response for GetAllSecurityQuestions {exception} {transactionId}", Newtonsoft.Json.JsonConvert.SerializeObject(exceptionmessage), _headers.ContextValues.TransactionId);
                        throw new MOBUnitedException(exceptionmessage);
                    }
                    //}
                    securityQuestions = ConvertToCSSSecurityList(response.SecurityQuestions);
                }
                #endregion
            }
            else
            {
                #region
                string url = _configuration.GetValue<string>("CslSecureProfileURL");

                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetMPPINPWDSecurityQuestions - URL for get Security Questions", "Trace", appId, appVersion, deviceId, url));
                //}
                _logger.LogInformation("GetMPPINPWDSecurityQuestions - URL for get Security Questions {url} {transactionId}", url, _headers.ContextValues.TransactionId);
                List<Metadata> dbTokens = new List<Metadata> { new Metadata("DBEnvironment", _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices")), new Metadata("LangCode", "en-US") };
                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<List<Metadata>>(sessionId, "GetMPPINPWDSecurityQuestions - Request for get Security Questions", "Trace", appId, appVersion, deviceId, dbTokens));
                //}
                _logger.LogInformation("GetMPPINPWDSecurityQuestions - Request for get Security Questions {dbTokens} {transactionId}", dbTokens, _headers.ContextValues.TransactionId);

                #region//****Get Call Duration Code - *******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****Get Call Duration Code *******

                SecureProfileClient proxy = new SecureProfileClient(url, token);
                GetAllSecurityQuestionsCallWrapper wrapper = proxy.GetAllSecurityQuestions(dbTokens);

                #region// 2 = cslStopWatch//****Get Call Duration Code *******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetMPPINPWDSecurityQuestions - CSL Call Duration", "CSS/CSL-CallDuration", appId, appVersion, deviceId, "CSL GetMPPINPWDSecurityQuestions=" + cslCallTime));
                //}
                #endregion//****Get Call Duration Code - *******


                if (string.IsNullOrEmpty(wrapper.ExceptionMessage))
                {
                    //if (traceSwitch.TraceError)
                    //{
                    //    logEntries.Add(LogEntry.GetLogEntry<List<Securityquestion>>(sessionId, "GetMPPINPWDSecurityQuestions - Client Response for get Security Questions", "Trace", appId, appVersion, deviceId, securityQuestions));
                    //}
                    _logger.LogInformation("GetMPPINPWDSecurityQuestions - Client Response for get Security Questions {securityQuestions} {transactionId}", securityQuestions, _headers.ContextValues.TransactionId);

                    securityQuestions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Securityquestion>>(Newtonsoft.Json.JsonConvert.SerializeObject(wrapper.SecurityQuestions));

                }
                else
                {
                    throw new MOBUnitedException(wrapper.ExceptionMessage);
                }
                #endregion
            }
            return securityQuestions;

        }
        private static List<Securityquestion> ConvertToCSSSecurityList(Collection<Service.Presentation.SecurityModel.Question> _CSLQuestionsCollection)
        {
            List<Securityquestion> _CSSQuestionsList = new List<Securityquestion>();
            List<Mobile.Model.Common.Answer> _CSSAnsList = new List<Mobile.Model.Common.Answer>();
            //int QuestionID = 1;
            //int AnsID = 1;
            foreach (var _CSLQst in _CSLQuestionsCollection)
            {
                if (_CSLQst != null && _CSLQst.Answers != null && _CSLQst.Answers.Count > 0)
                {
                    _CSSAnsList = new List<Mobile.Model.Common.Answer>();
                    //AnsID = 1;
                    foreach (var _CSLAns in _CSLQst.Answers)
                    {
                        Mobile.Model.Common.Answer _CSSAns = new Mobile.Model.Common.Answer
                        {
                            //AnswerId = AnsID,
                            AnswerKey = _CSLAns.AnswerKey,
                            AnswerText = _CSLAns.AnswerText,
                            QuestionKey = _CSLAns.QuestionKey,
                            //QuestionId = QuestionID
                        };
                        //++AnsID;
                        _CSSAnsList.Add(_CSSAns);
                    }
                    Securityquestion _CSSQtn = new Securityquestion
                    {
                        //QuestionId = QuestionID,
                        QuestionKey = _CSLQst.QuestionKey,
                        QuestionText = _CSLQst.QuestionText,
                        Used = _CSLQst.IsUsed,
                        Answers = _CSSAnsList
                    };
                    _CSSQuestionsList.Add(_CSSQtn);
                    //++QuestionID;
                }
            }

            return _CSSQuestionsList;
        }
        private System.Guid GetApplicationID(int appId, string sessionid, string actionName)
        {
            #region Get Aplication Id

            System.Guid appID = new Guid("643e1e47-1242-4b6c-ab7e-64024e4bc84c"); // default App Id
            try
            {
                string[] cSSAuthenticationTokenServiceApplicationIDs = _configuration.GetValue<string>("CSSAuthenticationTokenServiceApplicationIDs").Split('|');
                foreach (string applicationID in cSSAuthenticationTokenServiceApplicationIDs)
                {
                    if (Convert.ToInt32(applicationID.Split('~')[0].ToString().ToUpper().Trim()) == appId)
                    {
                        appID = new Guid(applicationID.Split('~')[1].ToString().Trim());
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                //logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(sessionid, actionName, "Get AppID Exception", exceptionWrapper));
            }
            #endregion

            return appID;
        }
        public async Task<SaveResponse> SendForgotPasswordEmail(string sessionid, string token, string mileagePlusNumber, string emailAddress, string languageCode)
        {
            if (string.IsNullOrEmpty(mileagePlusNumber))
            {
                throw new MOBUnitedException("MPNumber request cannot be null.");
            }
            string path = string.Format("/SendForgotPasswordEmail");

            United.Services.Customer.Common.SendForgotPasswordEmailRequest sendforgotpasswordemailrequest = new SendForgotPasswordEmailRequest();
            sendforgotpasswordemailrequest.EmailAddress = emailAddress;
            sendforgotpasswordemailrequest.MileagePlusId = mileagePlusNumber;
            sendforgotpasswordemailrequest.LangCode = languageCode;
            sendforgotpasswordemailrequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices").ToString();

            string jsonRequest = DataContextJsonSerializer.Serialize<SendForgotPasswordEmailRequest>(sendforgotpasswordemailrequest);

            var response = await _customerDataService.InsertMPEnrollment<SaveResponse>(token, jsonRequest, _headers.ContextValues.SessionId, path).ConfigureAwait(false);

            if (response != null)
            {
                if (response != null && (response.Errors == null || response.Errors.Count() == 0))
                {
                    _logger.LogInformation("SendForgotPasswordEmail {Response} {SessionId} ", JsonConvert.SerializeObject(response), sessionid);
                }
                else
                {
                    string errorMessage = string.Empty;
                    foreach (var error in response.Errors)
                    {
                        if (!string.IsNullOrEmpty(error.UserFriendlyMessage))
                        {
                            errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                        }
                        else
                        {
                            errorMessage = errorMessage + " " + error.Message;
                        }
                    }

                    string exceptionmessage = !string.IsNullOrEmpty(errorMessage) ? errorMessage : "Unable to Send Reset Account Email.";
                    _logger.LogError("SendForgotPasswordEmail Exception {exception} {SessionId} ", exceptionmessage, sessionid);
                    throw new MOBUnitedException(exceptionmessage);
                }

            }

            return response;
        }
        public async Task<SaveResponse> SendResetAccountEmail(string sessionid, string token, int customerId, string mileagePlusNumber, string emailAddress, string languageCode)
        {
            if (string.IsNullOrEmpty(mileagePlusNumber))
            {
                throw new MOBUnitedException("MPNumber request cannot be null.");
            }
            string path = "/SendResetAccountEmail";

            United.Services.Customer.Common.SendResetAccountEmailRequest SendResetAccountEmailRequest = new Services.Customer.Common.SendResetAccountEmailRequest();
            SendResetAccountEmailRequest.EmailAddress = emailAddress;
            SendResetAccountEmailRequest.MileagePlusId = mileagePlusNumber;
            SendResetAccountEmailRequest.LangCode = languageCode;
            SendResetAccountEmailRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices").ToString();

            string jsonRequest = DataContextJsonSerializer.Serialize<SendResetAccountEmailRequest>(SendResetAccountEmailRequest);
            var response = await _customerDataService.InsertMPEnrollment<SaveResponse>(token, jsonRequest, _headers.ContextValues.SessionId, path).ConfigureAwait(false);

            if (response != null)
            {
                string errorMessage = string.Empty;
                foreach (var error in response?.Errors)
                {
                    if (!string.IsNullOrEmpty(error.UserFriendlyMessage))
                    {
                        errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                    }
                    else
                    {
                        errorMessage = errorMessage + " " + error.Message;
                    }
                }
                string exceptionmessage = !string.IsNullOrEmpty(errorMessage) ? errorMessage : "Unable to Send Reset Account Email.";
                _logger.LogError("SendResetAccountEmails error {sessionid}", exceptionmessage, sessionid);
                throw new MOBUnitedException(exceptionmessage);
            }

            return response;
        }
        public bool SignOutSession(string sessionid, string token, int appId)
        {

            bool signOutSuccess = false;
            if (_configuration.GetValue<bool>("EnableDPToken"))
            {
                //signOutSuccess = DataPower.DPAccessTokenFactory.RevokeDPToken(token, appId, logEntries, traceSwitch); ////**==>> WHY DO WE NEED TO SIGN OUT IF THE MP SIGN IN ON THAT DEVICE FIRST TIME AS ANY WAY WE ASK SECURITY QUESTIONS TO ALLOW CUSTOMER TO CONTINUE ( AS CUSTOMER MAY NOT MAY NOT SELECT REMEMBER ME) AS THIS dp REVOKE TOKEN CREATING LOTS OF ISSUES TO FUTHURE CONTINUE WITHSAME TOKEN WHICH WE SIGNED THE CUSTOMER. 
            }
            else
            {
                System.Guid appID = GetApplicationID(appId, sessionid, "SignOutSession");
                string url = string.Format("{0}", _configuration.GetValue<string>("CSSAuthenticationTokenGeneratorURL"));
                //if (traceSwitch.TraceError)
                //{
                //    string request = "token:" + token;
                //    //Logging equest
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionid, "SignOutSession", "Request", request));
                //    //URL Logging
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionid, "SignOutSession", "Request url for Is Device Valid", url));
                //}

                #region//****SignOutSession Call Duration Code*******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****SignOutSession Call Duration Code - Srini 02/19/2016*******


                var client = new Css.ChannelProxy.Client(url);
                Css.Types.SignOutSessionCallWrapper response = client.SignOutSession(appID, new Guid(token));

                if (response.SignOutSessionOperationResult == Css.Types.SignOutSessionResult.Success &&
                    response.CallAuthenticationOperationResult == Css.Types.CallAuthenticationResult.Success &&
                    response.CallAuthorizationOperationResult == Css.Types.CallAuthorizationResult.Success &&
                    response.UseTokenValidationResult == Css.Types.UseTokenValidationResult.Valid)
                {
                    signOutSuccess = true; //**==>> Do this return true and default return false
                }

                #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionid, "SignOutSession", "CSS/CSL-CallDuration", "SignOutSession=" + cslCallTime));
                //}
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

                //if (traceSwitch.TraceError)
                //{
                //    if (response != null && string.IsNullOrEmpty(response.ExceptionMessage))
                //    {
                //        logEntries.Add(LogEntry.GetLogEntry<Css.Types.SignOutSessionCallWrapper>(sessionid, "SignOutSession", "DeSerialized Response", response));
                //    }
                //    else
                //    {
                //        string exceptionmessage = string.IsNullOrEmpty(response.ExceptionMessage) ? response.ExceptionMessage : "Unable to Signout Session.";
                //        logEntries.Add(LogEntry.GetLogEntry<string>(sessionid, "SignOutSession", "Exception", exceptionmessage));
                //        //throw new MOBUnitedException(exceptionmessage); //**==>> do not throw exception this is just to sign out if any exception no need to throw exception 
                //    }
                //}
            }

            return signOutSuccess;
        }
        public async Task<bool> ShuffleSavedSecurityQuestions(string token, string sessionId, int appId, string appVersion, string deviceId, string MileagePlusID, int customerID = 0, string loyaltyId = null)
        {
            bool StatusFlag = false;
            if (_configuration.GetValue<bool>("EnableDPToken"))
            {

                //string url = string.Format("{0}/ShuffleSavedSecurityQuestions", _configuration.GetValue<string>("CslSecureProfileURL"));
                string path = string.Format("/ShuffleSavedSecurityQuestions");
                Collection<Genre> dbTokens = new Collection<Genre> { new Genre { Key = "DBEnvironment", Value = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices").ToString() }, new Genre { Key = "LangCode", Value = "en-US" } };

                Service.Presentation.SecurityRequestModel.ShuffleSavedSecurityQuestionsRequest request = new Service.Presentation.SecurityRequestModel.ShuffleSavedSecurityQuestionsRequest
                {
                    CustomerId = customerID,
                    MileagePlusId = MileagePlusID,
                    Tokens = dbTokens

                };
                //if (levelSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "ShuffleSavedSecurityQuestions - URL for ShuffleSavedSecurityQuestions", "Trace", appId, appVersion, deviceId, url));
                //}
                string jsonRequest = DataContextJsonSerializer.Serialize<Service.Presentation.SecurityRequestModel.ShuffleSavedSecurityQuestionsRequest>(request);
                //if (levelSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "ShuffleSavedSecurityQuestions - Request for ShuffleSavedSecurityQuestions", "Trace", appId, appVersion, deviceId, jsonRequest));
                //}

                #region//****Get Call Duration Code - *******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****Get Call Duration Code *******

                //string jsonResponse = HttpHelper.Post(url, "application/json; charset=utf-8", token, jsonRequest);
                var response = await _mPSecurityQuestionsService.ValidateSecurityAnswer<ShuffleSavedSecurityQuestionsResponse>(token, jsonRequest, _headers.ContextValues.SessionId, path).ConfigureAwait(false);
                #region// 2 = cslStopWatch//****Get Call Duration Code *******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
                //if (levelSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "ShuffleSavedSecurityQuestions - CSL Call Duration", "CSS/CSL-CallDuration", appId, appVersion, deviceId, "CSL ShuffleSavedSecurityQuestions=" + cslCallTime));
                //}

                #endregion//****Get Call Duration Code - *******

                if (response != null)
                {

                    if (!string.IsNullOrEmpty(response.ExceptionMessage))
                    {
                        //if (levelSwitch.TraceError)
                        //{
                        //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "ShuffleSavedSecurityQuestions - Response from ShuffleSavedSecurityQuestions", "Trace", appId, appVersion, deviceId, response.ExceptionMessage));
                        //}
                        //United.Services.Customer.Common.SearchResponse response = JsonSerializer.Deserialize<United.Services.Customer.Common.SearchResponse>(jsonResponse);
                    }
                    StatusFlag = response.Success;
                }

            }
            else
            {

                string url = _configuration.GetValue<string>("CssSecureProfileURL").ToString();

                List<Metadata> dbTokens = new List<Metadata> { new Metadata("DBEnvironment", _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices").ToString()), new Metadata("LangCode", "en-US") };

                Css.SecureProfile.Types.ShuffleSavedSecurityQuestionsRequest request = new Css.SecureProfile.Types.ShuffleSavedSecurityQuestionsRequest();


                request.CustomerId = customerID;
                request.MileagePlusId = MileagePlusID;
                request.Tokens = dbTokens;

                //if (levelSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "ShuffleSavedSecurityQuestions - URL for ShuffleSavedSecurityQuestions", "Trace", appId, appVersion, deviceId, url));
                //}
                string jsonRequest = DataContextJsonSerializer.Serialize<Css.SecureProfile.Types.ShuffleSavedSecurityQuestionsRequest>(request);
                //if (levelSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "ShuffleSavedSecurityQuestions - Request for ShuffleSavedSecurityQuestions", "Trace", appId, appVersion, deviceId, jsonRequest));
                //}

                #region//****Get Call Duration Code - *******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****Get Call Duration Code *******

                SecureProfileClient proxy = new SecureProfileClient(url, token);

                ShuffleSavedSecurityQuestionsCallWrapper wrapper = proxy.ShuffleSavedSecurityQuestions(customerID, MileagePlusID, dbTokens);

                //string jsonResponse = HttpHelper.Post(url, "application/json; charset=utf-8", token, jsonRequest);

                #region// 2 = cslStopWatch//****Get Call Duration Code *******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
                //if (levelSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "ShuffleSavedSecurityQuestions - CSL Call Duration", "CSS/CSL-CallDuration", appId, appVersion, deviceId, "CSL ShuffleSavedSecurityQuestions=" + cslCallTime));
                //}

                #endregion//****Get Call Duration Code - *******

                //if (levelSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "ShuffleSavedSecurityQuestions - CSL Call Duration", "CSS/CSL-CallDuration", appId, appVersion, deviceId, "CSL ShuffleSavedSecurityQuestions=" + wrapper.ToString()));
                //}


                if (!string.IsNullOrEmpty(wrapper.ExceptionMessage))
                {
                    //if (levelSwitch.TraceError)
                    //{
                    //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "ShuffleSavedSecurityQuestions - Response from ShuffleSavedSecurityQuestions", "Trace", appId, appVersion, deviceId, wrapper.ExceptionMessage));
                    //}
                    //United.Services.Customer.Common.SearchResponse response = DataContextJsonSerializer.Deserialize<United.Services.Customer.Common.SearchResponse>(jsonResponse);
                }
                StatusFlag = wrapper.Success;
            }

            return StatusFlag;
        }
        public async Task<List<Securityquestion>> GetMPPinPwdSavedSecurityQuestions(string token, int customerId, string mileagePlusNumber, string sessionId, int appId, string appVersion, string deviceId)
        {
            List<Securityquestion> securityQuestions = null;

            if (_configuration.GetValue<bool>("EnableDPToken"))
            {
                //string url = string.Format("{0}/GetSavedSecurityQuestions", _configuration.GetValue<string>("CslSecureProfileURL"));
                string path = string.Format("/GetSavedSecurityQuestions");

                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetMPPinPwdSavedSecurityQuestions - URL for GetSavedSecurityQuestions", "Trace", appId, appVersion, deviceId, url));
                //}

                Collection<Genre> dbTokens = new Collection<Genre> { new Genre { Key = "DBEnvironment", Value = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices").ToString() }, new Genre { Key = "LangCode", Value = "en-US" } };

                Service.Presentation.SecurityRequestModel.SavedSecurityQuestionsRequest _SavedSecurityQuestionsRequest = new Service.Presentation.SecurityRequestModel.SavedSecurityQuestionsRequest
                {
                    CustomerId = customerId,
                    MileagePlusId = mileagePlusNumber,
                    Tokens = dbTokens
                };

                string jsonRequest = DataContextJsonSerializer.Serialize<Service.Presentation.SecurityRequestModel.SavedSecurityQuestionsRequest>(_SavedSecurityQuestionsRequest);

                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<Collection<Genre>>(sessionId, "GetMPPinPwdSavedSecurityQuestions - Request for GetSavedSecurityQuestions", "Trace", appId, appVersion, deviceId, dbTokens));
                //}

                #region//****Get Call Duration Code - *******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****Get Call Duration Code *******

                #region// 2 = cslStopWatch//****Get Call Duration Code *******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetMPPinPwdSavedSecurityQuestions - CSL Call Duration", "CSS/CSL-CallDuration", appId, appVersion, deviceId, "CSL GetMPPinPwdSavedSecurityQuestions=" + cslCallTime));
                //}

                #endregion//****Get Call Duration Code - *******
                ///
                //string jsonResponse = HttpHelper.Post(url, "application/json; charset=utf-8", token, jsonRequest);
                var response = await _mPSecurityQuestionsService.ValidateSecurityAnswer<SavedSecurityQuestionsResponse>(token, jsonRequest, sessionId, path).ConfigureAwait(false);

                if (response != null)
                {

                    if (!string.IsNullOrEmpty(response.ExceptionMessage))
                    {
                        //if (levelSwitch.TraceError)
                        //{
                        //    logEntries.Add(LogEntry.GetLogEntry<United.Service.Presentation.SecurityResponseModel.SavedSecurityQuestionsResponse>(sessionId, "GetMPPinPwdSavedSecurityQuestions - Response for GetSavedSecurityQuestions", "DeSerialized Response", appId, appVersion, deviceId, response));
                        //}
                    }
                    securityQuestions = ConvertToCSSSecurityList(response.Questions);
                }
                //string jsonResponse = _mPSecurityQuestionsService.ValidateSecurityAnswer<string>(token, jsonRequest, sessionId, path);

                //if (!string.IsNullOrEmpty(jsonResponse))
                //{
                //    SavedSecurityQuestionsResponse response = DataContextJsonSerializer.NewtonSoftDeserialize<SavedSecurityQuestionsResponse>(jsonResponse);

                //    //if (traceSwitch.TraceError)
                //    //{
                //    //    if (response != null && string.IsNullOrEmpty(response.ExceptionCode))
                //    //    {
                //    //        logEntries.Add(LogEntry.GetLogEntry<United.Service.Presentation.SecurityResponseModel.SavedSecurityQuestionsResponse>(sessionId, "GetMPPinPwdSavedSecurityQuestions - Response for GetSavedSecurityQuestions", "DeSerialized Response", appId, appVersion, deviceId, response));
                //    //    }
                //    //    else
                //    //    {
                //    //        string exceptionmessage = string.IsNullOrEmpty(response.ExceptionCode) ? response.ExceptionCode : "The account information you entered is incorrect.";
                //    //        exceptionmessage = ConfigurationManager.AppSettings["ValidateMPSignInGetSavedSecurityQuestionsErrorMessage"] != null && ConfigurationManager.AppSettings["ValidateMPSignInGetSavedSecurityQuestionsErrorMessage"] != "" ? ConfigurationManager.AppSettings["ValidateMPSignInGetSavedSecurityQuestionsErrorMessage"].ToString() : "The account information you entered is incorrect.";
                //    //        //string exceptionmessage = string.IsNullOrEmpty(response.ExceptionCode) ? response.ExceptionCode : "Unable to Validate Device.";
                //    //        logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetMPPinPwdSavedSecurityQuestions - Response for GetSavedSecurityQuestions", "Exception", appId, appVersion, deviceId, jsonResponse));
                //    //        throw new MOBUnitedException(exceptionmessage);
                //    //    }
                //    //}
                //    securityQuestions = ConvertToCSSSecurityList(response.Questions);
                //}


            }

            else
            {
                string url = _configuration.GetValue<string>("CssSecureProfileURL").ToString();

                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetMPPinPwdSavedSecurityQuestions - URL for GetSavedSecurityQuestions", "Trace", appId, appVersion, deviceId, url));
                //}
                List<Metadata> dbTokens = new List<Metadata> { new Metadata("DBEnvironment", _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices").ToString()), new Metadata("LangCode", "en-US") };

                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<List<Metadata>>(sessionId, "GetMPPinPwdSavedSecurityQuestions - Request for GetSavedSecurityQuestions", "Trace", appId, appVersion, deviceId, dbTokens));
                //}

                #region//****Get Call Duration Code - *******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****Get Call Duration Code *******

                SecureProfileClient proxy = new SecureProfileClient(url, token);
                GetSavedSecurityQuestionsCallWrapper savedQuestions = proxy.GetSavedSecurityQuestions(customerId, mileagePlusNumber.ToUpper(), dbTokens);

                #region// 2 = cslStopWatch//****Get Call Duration Code *******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetMPPinPwdSavedSecurityQuestions - CSL Call Duration", "CSS/CSL-CallDuration", appId, appVersion, deviceId, "CSL GetMPPinPwdSavedSecurityQuestions=" + cslCallTime));
                //}

                #endregion//****Get Call Duration Code - *******


                if (string.IsNullOrEmpty(savedQuestions.ExceptionMessage))
                {
                    securityQuestions = DataContextJsonSerializer.NewtonSoftDeserialize<List<Securityquestion>>(DataContextJsonSerializer.NewtonSoftSerializeToJson<List<Question>>(savedQuestions.Questions));

                    //if (traceSwitch.TraceError)
                    //{
                    //    logEntries.Add(LogEntry.GetLogEntry<List<Question>>(sessionId, "GetMPPinPwdSavedSecurityQuestions - Client Response for get SavedSecurityQuestions", "Trace", appId, appVersion, deviceId, savedQuestions.Questions));
                    //}
                }
                else
                {
                    throw new MOBUnitedException(savedQuestions.ExceptionMessage);
                }
            }
            return securityQuestions;
        }
    }
}
