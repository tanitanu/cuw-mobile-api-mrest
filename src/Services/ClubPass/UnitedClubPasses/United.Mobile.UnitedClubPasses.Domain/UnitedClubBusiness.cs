using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Ebs.Logging.Enrichers;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.UnitedClub;
using United.Mobile.Model.Internal.AccountManagement;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.UnitedClubPasses;
using United.Utility.Helper;

namespace United.Mobile.UnitedClubPasses.Domain
{
    public class UnitedClubBusiness : IUnitedClubBusiness
    {
        private readonly ICacheLog<UnitedClubBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDPService _tokenService;
        private readonly IPKDispenserPublicKeyService _iPKDispenserPublicKeyService;
        private readonly IUnitedClubMembershipV2Service _unitedClubMembershipV2Service;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ICachingService _cachingService;
        private readonly IUnitedClubSQLDBService _unitedClubSQLDBService;
        private Stopwatch stopwatch;
        private readonly Utility _utility;
        private readonly ICSLStatisticsService _cSLStatisticsService;
        private CSLStatistics _cSLStatistics;
        private bool IsMileageplusDeviceWithAppIdDataPresentInSQLDB { get; set; } = false;

        public UnitedClubBusiness(ICacheLog<UnitedClubBusiness> logger, IConfiguration configuration
            , IDPService tokenService, IPKDispenserPublicKeyService iPKDispenserPublicKeyService
            , IApplicationEnricher applicationEnricher
            , IUnitedClubMembershipV2Service unitedClubMembershipV2Service
            , IDynamoDBService dynamoDBService
            , ICachingService cachingService
            , IUnitedClubSQLDBService unitedClubSQLDBService
            , ICSLStatisticsService cSLStatisticsService)
        {
            _logger = logger;
            _configuration = configuration;
            stopwatch = new Stopwatch();
            _tokenService = tokenService;
            _iPKDispenserPublicKeyService = iPKDispenserPublicKeyService;
            _unitedClubMembershipV2Service = unitedClubMembershipV2Service;
            _dynamoDBService = dynamoDBService;
            _utility = new Utility(_configuration);
            _cSLStatisticsService = cSLStatisticsService;
            _cSLStatistics = new CSLStatistics(_logger, _configuration, _dynamoDBService, _cSLStatisticsService);
            _cachingService = cachingService;
            _unitedClubSQLDBService = unitedClubSQLDBService;
        }
        public async Task<ClubPKDispenserPublicKeyResponse> GetPKDispenserPublicKey(ClubPKDispenserPublicKeyRequest keyRequest)
        {
            ClubPKDispenserPublicKeyResponse response = new ClubPKDispenserPublicKeyResponse();
            this.stopwatch.Reset();
            this.stopwatch.Start();
            response.SessionId = Guid.NewGuid().ToString().ToUpper().Replace("-", "");

            //**RSA Public Key Implementation**//
            string transId = string.IsNullOrEmpty(keyRequest.TransactionId) ? "trans0" : keyRequest.TransactionId;
            string key = string.Format(_configuration.GetValue<string>("PKDispenserKeyTokenKeyFormat"), keyRequest.Application.Id);
            var cacheResponse = _cachingService.GetCache<United.Service.Presentation.SecurityResponseModel.PKDispenserKey>(key, transId).Result;
            var obj = JsonConvert.DeserializeObject<United.Service.Presentation.SecurityResponseModel.PKDispenserKey>(cacheResponse);

            string pkDispenserPublicKey = obj == null ? null : obj.PublicKey;

            if (!string.IsNullOrEmpty(pkDispenserPublicKey))
            {
                response.PkDispenserPublicKey = pkDispenserPublicKey;
            }
            else
            {
                string authToken = await _tokenService.GetAnonymousToken(keyRequest.Application.Id, keyRequest.DeviceId, _configuration);
                var CSLresponse = await _iPKDispenserPublicKeyService.GetPKDispenserPublicKeyServices(authToken, keyRequest.TransactionId);
                response.PkDispenserPublicKey = GetPublicKeyFromtheResponse(CSLresponse, keyRequest.Application.Id, keyRequest.TransactionId);
            }

            if (_configuration.GetValue<bool>("Log_CSL_Call_Statistics"))
            {
                string callDurations = "|REST Total =" + (stopwatch.ElapsedMilliseconds / (double)1000).ToString();
                await _cSLStatistics.AddCSLCallStatisticsDetails(keyRequest.MPNumber, "GetPKDispenserPublicKey", string.Empty, callDurations, "UnitedClubController/GetPKDispenserPublicKey", response.SessionId);
            }
            return response;
        }

        public async Task<ClubPKDispenserPublicKeyResponse> GetPublicKey(ClubPKDispenserPublicKeyRequest keyRequest)
        {
            ClubPKDispenserPublicKeyResponse response = new ClubPKDispenserPublicKeyResponse();
            this.stopwatch.Reset();
            this.stopwatch.Start();

            string staticSessionID = keyRequest.DeviceId;

            staticSessionID = keyRequest.DeviceId + "_" + DateTime.Now.ToShortDateString() + "_" + DateTime.Now.ToLongTimeString();
            response.SessionId = staticSessionID;

            //**RSA Publick Key Implmentaion**//
            string pkDispenserPublicKey = string.Empty;

            string transId = string.IsNullOrEmpty(keyRequest.TransactionId) ? "trans0" : keyRequest.TransactionId;
            string key = string.Format(_configuration.GetValue<string>("PKDispenserKeyTokenKeyFormat"), keyRequest.Application.Id);
            var cacheResponse = _cachingService.GetCache<United.Service.Presentation.SecurityResponseModel.PKDispenserKey>(key, transId).Result;
            var obj = JsonConvert.DeserializeObject<United.Service.Presentation.SecurityResponseModel.PKDispenserKey>(cacheResponse);

            pkDispenserPublicKey = obj == null ? null : obj.PublicKey;


            if (!string.IsNullOrEmpty(pkDispenserPublicKey))
            {
                response.PkDispenserPublicKey = pkDispenserPublicKey;
            }
            else
            {
                var token = await _tokenService.GetAnonymousToken(keyRequest.Application.Id, keyRequest.DeviceId, _configuration);
                var CSLresponse = await _iPKDispenserPublicKeyService.GetPKDispenserPublicKeyServices(token, keyRequest.TransactionId);
                //Headers.ContextValues.SessionId = staticSessionID;
                response.PkDispenserPublicKey = GetPublicKeyFromtheResponse(CSLresponse, keyRequest.Application.Id,keyRequest.TransactionId);
            }

            if (_configuration.GetValue<bool>("Log_CSL_Call_Statistics"))
            {
                string callDurations = "|REST Total =" + (stopwatch.ElapsedMilliseconds / (double)1000).ToString();
                await _cSLStatistics.AddCSLCallStatisticsDetails(keyRequest.MPNumber, "GetPKDispenserPublicKey", string.Empty, callDurations, "UnitedClubController/GetPKDispenserPublicKey", response.SessionId);
            }
            return response;
        }

        public async Task<ClubMembershipResponse> GetUnitedClubMembershipV2(UnitedClubMembershipRequest request)
        {
            string transactionIdAfterSplit = request.TransactionId;
            string deviceId = string.Empty;
            string transactionId = string.Empty;
            bool validWalletRequest = IsValidDeviceRequest(request.TransactionId, request.Application.Id, request.MPNumber, ref transactionId, ref deviceId);
            request.TransactionId = transactionIdAfterSplit;

            ClubMembershipResponse response = new ClubMembershipResponse
            {
                TransactionId = request.TransactionId,
                LanguageCode = "en-US"
            };

            if (validWalletRequest)
            {
                if (GeneralHelper.ValidateAccessCode(request.AccessCode))
                {
                    if (!await ValidateAccountFromCache(request.MPNumber, request.HashPinCode, request.Application.Id,request.DeviceId,request.TransactionId, request.Application.Version.Major))
                    {
                        throw new MOBUnitedException(string.Format("Invalid Account Number or Pin."));
                    }
                    response.Membership = await GetClubMembershipV3(request);
                }
                else
                {
                    throw new MOBUnitedException(string.Format("Access code '{0}' is invalid", request.AccessCode));
                }
            }
            else
            {
                _logger.LogInformation("GetUnitedClubMembershipV2 - Unauthorized {DeviceId} {ApplicationId} {MPNumber} and {HaspinCode}", request.DeviceId, request.Application.Id.ToString(), request.MPNumber, request.HashPinCode);
            }
            return response;
        }

        private async Task<ClubMembership> GetClubMembershipV3(UnitedClubMembershipRequest ucmRequest)
        {
            ClubMembership membership = new ClubMembership();

            if (ucmRequest != null && !string.IsNullOrEmpty(ucmRequest.MPNumber))
            {
                membership.MPNumber = ucmRequest.MPNumber;
                membership.FirstName = string.Empty;
                membership.MiddleName = string.Empty;
                membership.LastName = string.Empty;

                ClubMembership currentMembershipInfo = null;

                var jsonResponse = await _unitedClubMembershipV2Service.GetCurrentMembershipInfo(ucmRequest.MPNumber, ucmRequest.TransactionId);

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    List<UClubMembershipInfo> uClubMembershipInfoList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UClubMembershipInfo>>(jsonResponse);
                    if (uClubMembershipInfoList != null && uClubMembershipInfoList.Count > 0)
                    {
                        foreach (UClubMembershipInfo uClubMembershipInfo in uClubMembershipInfoList)
                        {
                            if (uClubMembershipInfo.DiscontinueDate.DateTime >= DateTime.Now && string.IsNullOrEmpty(uClubMembershipInfo.ClubStatusCode))
                            {
                                currentMembershipInfo = new ClubMembership
                                {
                                    CompanionMPNumber = string.IsNullOrEmpty(uClubMembershipInfo.CompanionMpNumber) ? string.Empty : uClubMembershipInfo.CompanionMpNumber,
                                    EffectiveDate = uClubMembershipInfo.EffectiveDate.DateTime.ToString("MM/dd/yyyy"),
                                    ExpirationDate = uClubMembershipInfo.DiscontinueDate.DateTime.ToString("MM/dd/yyyy"),
                                    IsPrimary = string.IsNullOrEmpty(uClubMembershipInfo.PrimaryOrCompanion),
                                    MembershipTypeCode = string.IsNullOrEmpty(uClubMembershipInfo.MemberTypeCode) ? string.Empty : uClubMembershipInfo.MemberTypeCode,
                                    MembershipTypeDescription = string.IsNullOrEmpty(uClubMembershipInfo.MemberTypeDescription) ? string.Empty : uClubMembershipInfo.MemberTypeDescription
                                };
                            }
                        }
                    }
                }
                if (currentMembershipInfo != null)
                {
                    membership.MembershipTypeCode = currentMembershipInfo.MembershipTypeCode;
                    membership.MembershipTypeDescription = currentMembershipInfo.MembershipTypeDescription;
                    membership.EffectiveDate = currentMembershipInfo.EffectiveDate;
                    membership.ExpirationDate = currentMembershipInfo.ExpirationDate;
                    membership.CompanionMPNumber = currentMembershipInfo.CompanionMPNumber;
                    membership.IsPrimary = currentMembershipInfo.IsPrimary;
                    string barCodeData = string.Format("M1XXXXXXX/XXXXXXXXX   XX--XX1 XXXXXXUA 0000 242J008C 136 15C>3180 O3242BUA              0000000000000000 UA UA {0}            *000      00  UAG", membership.MPNumber);
                    membership.BarCode = _utility.GetBarCode(barCodeData);
                    membership.BarCodeString = barCodeData;
                }
                else
                {
                    membership = null;
                }
            }
            return membership;
        }

        private string GetPublicKeyFromtheResponse(string Clsresponse, int applicationId,string transactionId)
        {
            string pkDispenserPublicKey = string.Empty;
            if (!string.IsNullOrEmpty(Clsresponse))
            {
                United.Service.Presentation.SecurityResponseModel.PKDispenserResponse response = null;
                response = DataContextJsonSerializer.NewtonSoftDeserialize<United.Service.Presentation.SecurityResponseModel.PKDispenserResponse>(Clsresponse);
                if (response != null && response.Keys != null && response.Keys.Count > 0)
                {
                    var obj = (from st in response.Keys
                               where st.CryptoTypeID.Trim().Equals("2")
                               select st).ToList();
                    obj[0].PublicKey = obj[0].PublicKey.Replace("\r", "").Replace("\n", "").Replace("-----BEGIN PUBLIC KEY-----", "").Replace("-----END PUBLIC KEY-----", "").Trim();
                    pkDispenserPublicKey = obj[0].PublicKey;

                    string transId = string.IsNullOrEmpty(transactionId) ? "trans0" : transactionId;
                    string key = string.Format(_configuration.GetValue<string>("PKDispenserKeyTokenKeyFormat"), applicationId);
                    _cachingService.SaveCache<United.Service.Presentation.SecurityResponseModel.PKDispenserKey>(key, obj[0], transId, new TimeSpan(1, 30, 0));

                    PKDispenserPublicKeyCacheSessionResponse cPKDispObj = new PKDispenserPublicKeyCacheSessionResponse()
                    {
                        PkDispenserPublicKey = pkDispenserPublicKey,
                        Kid = obj[0].Kid,
                        CryptoTypeID = obj[0].CryptoTypeID
                    };

                }
                else
                {
                    string exceptionMessage = _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                    if (!String.IsNullOrEmpty(_configuration.GetValue<string>("UnableToGetPkDispenserPublicKeyErrorMessage")))
                    {
                        exceptionMessage = _configuration.GetValue<string>("UnableToGetPkDispenserPublicKeyErrorMessage");
                    }
                    throw new MOBUnitedException(exceptionMessage);
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                if (!String.IsNullOrEmpty(_configuration.GetValue<string>("UnableToGetPkDispenserPublicKeyErrorMessage")))
                {
                    exceptionMessage = _configuration.GetValue<string>("UnableToGetPkDispenserPublicKeyErrorMessage");
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            return pkDispenserPublicKey;
        }

        public async Task<bool> ValidateAccountFromCache(string accountNumber, string pinCode, int appId, string deviceId,string transactionId, string appVersion)
        {
            bool ok = false;
            var mDb = new MileagePlusDynamoDB(_configuration, _dynamoDBService, _logger);
            string response = await mDb.ValidateAccount(accountNumber, appId, deviceId, pinCode, transactionId);
            if (response != null)
            {
                var ucMemberShipV2 = Newtonsoft.Json.JsonConvert.DeserializeObject<MileagePlus>(response);
                if (ucMemberShipV2 != null && ucMemberShipV2.HashPincode.Equals(pinCode) && ucMemberShipV2.MileagePlusNumber.Equals(accountNumber))
                    ok = true;
            }
            if (!ok)
            {
                //OnPremSQLDB service call - if record does not present in the dynamodb
                try
                {
                    ok = await _unitedClubSQLDBService.ValidateAccountFromCache(accountNumber, appId,
                                                                                appVersion,
                                                                                deviceId, pinCode,
                                                                                transactionId);
                    IsMileageplusDeviceWithAppIdDataPresentInSQLDB = ok;
                }
                catch (Exception ex)
                {
                    _logger.LogError("OnPremSQLService-ValidateAccountFromCache Error {message} {exceptionStackTrace} and {transactionId}", ex.Message, ex.StackTrace, transactionId);
                }
            }
            if (IsMileageplusDeviceWithAppIdDataPresentInSQLDB)
            {
                var mpItem = new Model.Internal.AccountManagement.MileagePlus()
                {
                    MileagePlusNumber = accountNumber,
                    DeviceID = deviceId,
                    ApplicationID = appId.ToString(),
                    HashPincode = pinCode
                };
                await mDb.SaveRecords<string>(accountNumber, deviceId, appId.ToString(), JsonConvert.SerializeObject(mpItem), transactionId);
            }

            return ok;
        }

        private bool IsValidDeviceRequest(string transactionIdAfterSplit, int applicationId, string mpNumber, ref string transactionId, ref string deviceId)
        {
            string deviceIdLocal = transactionIdAfterSplit.Trim('|'); // We see iOS is sending only DeviceId as transaction ID without a Pipe symbol so to address this scenario we assigned the transaction ID as Device ID
            transactionId = transactionIdAfterSplit.Trim('|') + "|" + mpNumber + "_" + DateTime.Now.ToShortDateString() + "_" + DateTime.Now.ToLongTimeString(); // Same above Scenario And creating a new Transaction ID as DeviceID | GUID.

            if (transactionIdAfterSplit.IndexOf('|') > -1)
            {
                transactionId = transactionIdAfterSplit.Split('|')[1];
                deviceId = transactionIdAfterSplit.Split('|')[0];
                deviceIdLocal = deviceId;
            }
            bool isValidDeviceRequest = IsValidDeviceRequest(deviceIdLocal, applicationId, mpNumber,transactionId).Result;
            return isValidDeviceRequest;
        }

        private async Task<bool> IsValidDeviceRequest(string deviceId, int applicationId, string mpNumber, string transactionId)
        {
            bool validWalletRequest = true; // This flag will set to true either if the wallet call request is a valid one (checking the MP device table withthe device Id and MP passed in the request)
                                            //if (!string.IsNullOrEmpty(request.PushToken) && !string.IsNullOrEmpty(request.MPNumber))
            if (!string.IsNullOrEmpty(mpNumber))
            {
                if (_configuration.GetValue<bool>("ValidateWalletRequest"))
                {
                    var mDb = new MileagePlusDynamoDB(_configuration, _dynamoDBService, _logger);
                    bool verifyMileagePlusWithDeviceIAPPID = false;
                    string response = await mDb.VerifyMileagePlusWithDeviceAPPID(deviceId, applicationId, mpNumber, transactionId);
                    if (response != null)
                        return verifyMileagePlusWithDeviceIAPPID = true;
                    else
                    {
                        //OnPrem SQLDB Service call if record does not present in the dynamodb.
                        try
                        {
                            bool ok = await _unitedClubSQLDBService.VerifyMileagePlusWithDeviceAPPID(deviceId, applicationId, mpNumber,transactionId);
                            if (ok)
                            {
                                IsMileageplusDeviceWithAppIdDataPresentInSQLDB = true;
                                verifyMileagePlusWithDeviceIAPPID = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("OnPremSQLService-VerifyMileagePlusWithDeviceAPPID Error {message} {exceptionStackTrace} and {transactionId}", ex.Message, ex.StackTrace, transactionId);
                        }

                    }
                    if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(mpNumber) || !verifyMileagePlusWithDeviceIAPPID)
                    {
                        validWalletRequest = false;
                    }
                }
                else
                {
                    validWalletRequest = true; // here we set to true to have this work as existing production wiht out checking the MP DeviceId and MP Number validation
                }
            }
            return validWalletRequest;
        }

    }
}
