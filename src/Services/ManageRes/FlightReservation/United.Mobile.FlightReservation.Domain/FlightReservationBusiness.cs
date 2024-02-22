using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.ManageRes;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightReservation;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.Model.Common;
using United.Mobile.Model.Fitbit;
using United.Mobile.Model.FlightReservation;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ManageRes;
using United.Utility.Enum;
using United.Utility.Helper;

namespace United.Mobile.FlightReservation.Domain
{
    public class FlightReservationBusiness : IFlightReservationBusiness
    {
        private readonly ICacheLog<FlightReservationBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly IFlightReservation _flightReservation;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly IRequestReceiptByEmailService _requestReceiptByEmailService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly ManageResUtility _manageResUtility;
        private readonly IHeaders _headers;

        public FlightReservationBusiness(ICacheLog<FlightReservationBusiness> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , IShoppingSessionHelper shoppingSessionHelper
            , IFlightReservation flightReservation
            , IDynamoDBService dynamoDBService
          //  , IRequestReceiptByEmailService requestReceiptByEmailService
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , IHeaders headers)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _shoppingSessionHelper = shoppingSessionHelper;
            _flightReservation = flightReservation;
            _dynamoDBService = dynamoDBService;
            //_requestReceiptByEmailService = requestReceiptByEmailService;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _headers= headers;
            _manageResUtility = new ManageResUtility(_configuration, _legalDocumentsForTitlesService, _dynamoDBService, _headers, _logger);
        }

        public async Task<MOBPNRByMileagePlusResponse> GetPNRsByMileagePlusNumber(int applicationId, string appVersion, string accessCode, string transactionId, string mileagePlusNumber, string pinCode, string reservationType, string languageCode, bool includeFarelockInfo = false)
        {
            MOBPNRByMileagePlusRequest request = new MOBPNRByMileagePlusRequest();
            request.AccessCode = accessCode;
            request.TransactionId = transactionId;
            request.MileagePlusNumber = mileagePlusNumber;

            request.ReservationType = (MOBReservationType)Enum.Parse(typeof(MOBReservationType), reservationType);
            request.LanguageCode = languageCode;

            _logger.LogInformation("GetPNRsByMileagePlusNumberRequest {Request} and {SessionId}", _headers.ContextValues.SessionId, request.TransactionId);//Common Login Code


            MOBPNRByMileagePlusResponse response = new MOBPNRByMileagePlusResponse();
            response.TransactionId = request.TransactionId;

            #region ALM 24989  - Dover Release - deviceid validation - Modified by Srini 12/29/2015

            string transactionIdAfterSplit = transactionId;
            string deviceId = string.Empty;
            bool validWalletRequest = _manageResUtility.isValidDeviceRequest(transactionId, applicationId, mileagePlusNumber, ref transactionIdAfterSplit, ref deviceId);
            request.TransactionId = transactionIdAfterSplit;
            request.DeviceId = deviceId;

            #endregion

            if (validWalletRequest)
            {
                if (GeneralHelper.ValidateAccessCode(accessCode))
                {
                    if (!_manageResUtility.ValidateAccountFromCache(request.MileagePlusNumber, pinCode))
                    {
                        throw new MOBUnitedException(string.Format("Invalid Account Number or Pin."));
                    }
                    bool getEmployeeIdFromCSLCustomerData = !string.IsNullOrEmpty(_configuration.GetValue<string>("GetEmployeeIDFromGetProfileCustomerData")) && Convert.ToBoolean(_configuration.GetValue<string>("GetEmployeeIDFromGetProfileCustomerData"));

                    if (!_configuration.GetValue<bool>("ReservationMigrateToCSLService"))
                    {
                        response.PNRs = await _flightReservation.GetPNRsByMileagePlusNumber(transactionId, request.MileagePlusNumber, request.ReservationType, request.LanguageCode, includeFarelockInfo, appVersion, false, applicationId, getEmployeeIdFromCSLCustomerData);
                    }
                    else
                    {
                        response.PNRs = _flightReservation.GetPNRsByMileagePlusNumberCSL(transactionId, request.MileagePlusNumber, request.ReservationType, request.LanguageCode, includeFarelockInfo, appVersion, false, applicationId, getEmployeeIdFromCSLCustomerData);
                    }
                }
                else
                {
                    throw new MOBUnitedException("Invalid access code");
                }
            }
            else
            {
                response.Exception = new MOBException();
                response.Exception.Message = _configuration.GetValue<string>("GenericExceptionMessage");
            }

            return await Task.FromResult(response);
        }
        public async Task<MOBReceiptByEmailResponse> RequestReceiptByEmail(MOBReceiptByEmailRequest request)
        {

            //stopwatch.Reset();
            //stopwatch.Start();
            //infFltRes.LogEntries.Clear();

            //if (this.levelSwitch.TraceInfo)
            //{
            //infFltRes.LogEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBReceiptByEmailRequest>(request.TransactionId, "RequestReceiptByEmail", "Request", request));//Common Login Code
            _logger.LogInformation("RequestReceiptByEmailRequest {Request} and {SessionId}", _headers.ContextValues.SessionId, request.TransactionId);//Common Login Code
            //}


            MOBReceiptByEmailResponse response = new MOBReceiptByEmailResponse();
            response.TransactionId = request.TransactionId;
            //try
            //{
            if (GeneralHelper.ValidateAccessCode(request.AccessCode))
            {

                CommonDef commonDef = new CommonDef();
                CommonDef presistedCommonDef = null;
                if (_configuration.GetValue<bool>("DeviceIDPNRSessionGUIDCaseSensitiveFix"))
                {
                    //presistedCommonDef = United.Persist.FilePersist.Load<United.Persist.Definition.Common.CommonDef>
                    //    ((UtilityNew.GetDeviceIdFromTransactionId(request.TransactionId) + request.RecordLocator).Replace("|", "").Replace("-", "").ToUpper().Trim(), commonDef.ObjectName);
                    presistedCommonDef = await _sessionHelperService.GetSession<CommonDef>
                        ((GetDeviceIdFromTransactionId(request.TransactionId) + request.RecordLocator).Replace("|", "").Replace("-", "").ToUpper().Trim(), commonDef.ObjectName).ConfigureAwait(false);
                }
                else
                {
                    presistedCommonDef =await _sessionHelperService.GetSession<CommonDef>
                        ((GetDeviceIdFromTransactionId(request.TransactionId) + request.RecordLocator).Replace("|", "").Replace("-", ""), commonDef.ObjectName).ConfigureAwait(false);
                }

                MOBPNRByRecordLocatorResponse mobpnrbyrecordlocatorresponse;
                if (presistedCommonDef != null)
                {
                    mobpnrbyrecordlocatorresponse = JsonConvert.DeserializeObject<MOBPNRByRecordLocatorResponse>(presistedCommonDef.SampleJsonResponse);
                    if (mobpnrbyrecordlocatorresponse == null)
                    {
                        throw new MOBUnitedException(_configuration.GetValue<string>("InvalidPNRLastName-ExceptionMessage").ToString());
                    }
                }
                else
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("InvalidPNRLastName-ExceptionMessage").ToString());
                }

                if (request.RecordLocator == null || request.RecordLocator.Trim().Length != 6)
                {
                    throw new MOBUnitedException("Confirmation number must be 6 alphanumeric in length");
                }

                if (!IsValidEmail(request.EMailAddress))
                {
                    throw new MOBUnitedException("You have entered an invalid e-mail address");
                }

                response.RecordLocator = request.RecordLocator;
                response.EMailAdress = request.EMailAddress;
                response.CreationDate = request.CreationDate;
                request.DeviceId = string.IsNullOrEmpty(request.DeviceId) ? mobpnrbyrecordlocatorresponse.DeviceId : request.DeviceId;

                //TO DO
                if (await RequestReceiptByEmailViaCSL(request))
                {
                    response.Message = string.Format("Ticket receipt for confirmation number {0} is sent to {1}", response.RecordLocator, response.EMailAdress);
                }
            }
            else
            {
                throw new MOBUnitedException("Invalid access code");
            }
            //}
            //catch (MOBUnitedException coex)
            //{
            //    if (levelSwitch.TraceInfo)
            //    {
            //        infFltRes.LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>
            //            (request.TransactionId, "RequestReceiptByEmail", "MOBUnitedException", coex.Message));
            //    }
            //    response.Exception = new MOBException();
            //    response.Exception.Message = coex.Message;
            //}
            //catch (System.Exception ex)
            //{
            //    if (levelSwitch.TraceInfo)
            //    {
            //        MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
            //        infFltRes.LogEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>
            //            (request.TransactionId, "RequestReceiptByEmail", "Exception", exceptionWrapper));
            //    }
            //    response.Exception = new MOBException("10000", ConfigurationManager.AppSettings["GenericExceptionMessage"]);
            //}

            //stopwatch.Stop();
            //response.CallDuration = stopwatch.ElapsedMilliseconds;

            //if (this.levelSwitch.TraceInfo)
            //{
            //    infFltRes.LogEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBReceiptByEmailResponse>
            //        (request.TransactionId, "RequestReceiptByEmail", "Response", response));
            //}
            //logger.Write(infFltRes.LogEntries);
            return await Task.FromResult(response);

        }
        private string GetDeviceIdFromTransactionId(string transactionId)
        {
            string retDeviceId = transactionId;
            if (!string.IsNullOrEmpty(retDeviceId) && retDeviceId.IndexOf('|') > -1)
            {
                retDeviceId = retDeviceId.Split('|')[0];
            }
            return retDeviceId;
        }
        private bool IsValidEmail(string email)
        {
            string strRegex = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                  @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                  @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
            Regex re = new Regex(strRegex);
            if (re.IsMatch(email))
                return (true);
            else
                return (false);
        }

        private async Task<bool> RequestReceiptByEmailViaCSL(MOBReceiptByEmailRequest request)
        {
            bool ok = false;
            var session = new Session();

            Collection<United.Service.Presentation.CommonModel.EmailAddress> cslRequest = new Collection<United.Service.Presentation.CommonModel.EmailAddress>();
            cslRequest.Add(
                new Service.Presentation.CommonModel.EmailAddress
                {
                    Address = request.EMailAddress
                });

            //if (this.levelSwitch.TraceError)
            //{
            //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<Collection<Service.Presentation.CommonModel.EmailAddress>>
            //        (request.TransactionId, "RequestReceiptByEmail request", "Request", cslRequest));
            //}
            _logger.LogInformation("RequestReceiptByEmailViaCSL Request {SessionId} and {cslRequest}", _headers.ContextValues.SessionId, cslRequest);

            string token = _shoppingSessionHelper.CheckIsCSSTokenValid(request.Application.Id, request.DeviceId, request.Application.Version.Major, request.TransactionId, session, string.Empty).Result.ToString();
            string jsonRequest = JsonConvert.SerializeObject(cslRequest);
            //string url = string.Format("{0}?ConfirmationID={1}&PutInReceiptQueue=true",
             //   _configuration.GetValue<string>("RequestReceiptByEmailViaCSLURL"), request.RecordLocator);
            string url = string.Format("{0}?ConfirmationID={1}&PutInReceiptQueue=true");

            var cslResponse =await _requestReceiptByEmailService.PostReceiptByEmailViaCSL(token, jsonRequest, _headers.ContextValues.SessionId, url).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(cslResponse))
            {
                List<United.Service.Presentation.CommonModel.Message>
                   msgResponse = JsonConvert.DeserializeObject<List<United.Service.Presentation.CommonModel.Message>>(cslResponse);

                //if (this.levelSwitch.TraceError)
                //{
                //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<List<United.Service.Presentation.CommonModel.Message>>
                //           (request.TransactionId, "RequestReceiptByEmail response", "Response", msgResponse));
                //}
                _logger.LogInformation("RequestReceiptByEmailViaCSL Response {TransactionId} and {msgResponse}", request.TransactionId, msgResponse);

                if (msgResponse != null && msgResponse.Any()
                    && string.Equals(msgResponse[0].Status, "SHARE_RESPONSE", StringComparison.OrdinalIgnoreCase)
                    && msgResponse[0].Text.IndexOf("EMAIL RQSTD") >= 0)
                {
                    ok = true;
                }

                //if (this.levelSwitch.TraceError)
                //{
                //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<bool>(request.TransactionId, "RequestReceiptByEmail", "Response", ok));
                //}
                _logger.LogInformation("RequestReceiptByEmailViaCSL Response {TransactionId} , {ok}", request.TransactionId, ok);
            }
            return ok;
        }

        public async Task<MOBPNRRemarkResponse> AddPNRRemark(MOBPNRRemarkRequest request)
        {
            MOBPNRRemarkResponse response = new MOBPNRRemarkResponse();
            response.TransactionId = request.TransactionId;

            response.RecordLocator = request.RecordLocator;
            response.TransactionId = request.TransactionId;

                if (request.Flow == Convert.ToString(FlowType.VIEWRES))
                {
                    request.RemarkDescription =_configuration.GetValue<String>("ViewRes_ShareAppSharesRemark");
                }

                Session session = null;
                if (!string.IsNullOrEmpty(request.SessionId))
                {
                    session = await _sessionHelperService.GetSession<Session>(request.SessionId, (new Session()).ObjectName).ConfigureAwait(false);

                    session.Flow = request.Flow;
                }
                await _flightReservation.AddPNRRemark(request);      


            return response;
        }

    }
}
