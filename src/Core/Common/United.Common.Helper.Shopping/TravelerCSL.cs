﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Utility.Helper;

namespace United.Common.Helper.Shopping
{
    public class TravelerCSL : ITravelerCSL
    {
        private readonly IConfiguration _configuration;
        private readonly ICMSContentService _iCMSContentService;
        private readonly ICachingService _cachingService;
        private readonly ICacheLog<TravelerCSL> _logger;

        public TravelerCSL(IConfiguration configuration
            , ICMSContentService iCMSContentService
            , ICachingService cachingService
            , ICacheLog<TravelerCSL> logger)
        {
            _configuration = configuration;
            _iCMSContentService = iCMSContentService;
            _cachingService = cachingService;
            _logger = logger;
        }

        public async Task<CPCubaTravel> GetCubaTravelResons(MobileCMSContentRequest request)
        {
            request.GroupName = "General:CubaTravelCerts";
            string jsonResponse = await GETCSLCMSContent(request);

            CPCubaTravel mobCPCubaTravel = new CPCubaTravel();
            if (!string.IsNullOrEmpty(jsonResponse))
            {

                var response = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(jsonResponse);

                if (response != null && (Convert.ToBoolean(response.Status) && response.MessageItems != null))
                {
                    //if (traceSwitch.TraceError)
                    //{
                    //    logEntries.Add(LogEntry.GetLogEntry<CSLContentMessagesResponse>(request.SessionId, "GetMobileCMSContents - DeSerialized Response", "DeSerialized Response", request.Application.Id, request.Application.Version.Major, request.DeviceId, response));
                    //}

                    mobCPCubaTravel.TravelReasons = GetMOBCPCubaTravelReasons(response.MessageItems);


                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.Message;
                        }
                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToRegisterTravelerErrorMessage");
                        if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL RegisterTravelers(MOBRegisterTravelersRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToRegisterTravelerErrorMessage");
                if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL RegisterTravelers(MOBRegisterTravelersRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }


            //logEntries.Add(LogEntry.GetLogEntry<List<MOBMobileCMSContentMessages>>(request.SessionId, "GetMobileCMSContents - Client Response", "to client Response", request.Application.Id, request.Application.Version.Major, request.DeviceId, sortedCMSContentMessages));

            return mobCPCubaTravel;
        }

        public async Task<string> GETCSLCMSContent(MobileCMSContentRequest request, bool isTravelAdvisory = false)
        {
            #region
            if (request == null)
            {
                throw new MOBUnitedException("GetMobileCMSContents request cannot be null.");
            }
            #region Get CSL Content request
            MOBCSLContentMessagesRequest cslContentReqeust = BuildCSLContentMessageRequest(request, isTravelAdvisory);
            #endregion

            string jsonResponse =await GetCSLCMSContentMesseges(request, cslContentReqeust);
            #endregion
            return jsonResponse;

        }

        private async Task<string> GetCSLCMSContentMesseges(MobileCMSContentRequest request, MOBCSLContentMessagesRequest cslContentReqeust)
        {
            string jsonRequest = JsonConvert.SerializeObject(cslContentReqeust);

            return await _iCMSContentService.GetMobileCMSContentMessages(token: request.Token, jsonRequest, request.SessionId).ConfigureAwait(false);
        }

        private MOBCSLContentMessagesRequest BuildCSLContentMessageRequest(MobileCMSContentRequest request, bool istravelAdvisory = false)
        {
            MOBCSLContentMessagesRequest cslContentReqeust = new MOBCSLContentMessagesRequest();
            if (request != null)
            {
                cslContentReqeust.Lang = "en";
                cslContentReqeust.Pos = "us";
                cslContentReqeust.Channel = "mobileapp";
                cslContentReqeust.Listname = new List<string>();
                foreach (string strItem in request.ListNames)
                {
                    cslContentReqeust.Listname.Add(strItem);
                }
                if (_configuration.GetValue<string>("CheckCMSContentsLocationCodes").ToUpper().Trim().Split('|').ToList().Contains(request.GroupName?.ToUpper().Trim()))
                {
                    cslContentReqeust.LocationCodes = new List<string>();
                    cslContentReqeust.LocationCodes.Add(request.GroupName);
                }
                else
                {
                    cslContentReqeust.Groupname = request.GroupName;
                }
                if (_configuration.GetValue<bool>("DonotUsecache4CSMContents"))
                {
                    if (!istravelAdvisory)
                        cslContentReqeust.Usecache = true;
                }
            }

            return cslContentReqeust;
        }
        private List<MOBCPCubaTravelReason> GetMOBCPCubaTravelReasons(List<CMSContentMessageitem> messageItems)
        {
            List<MOBCPCubaTravelReason> mobCPCubaTravelReason = null;
            if (messageItems != null)
            {
                string messageItemsJson = JsonConvert.SerializeObject(messageItems);
                mobCPCubaTravelReason = JsonConvert.DeserializeObject<List<MOBCPCubaTravelReason>>(messageItemsJson);
                mobCPCubaTravelReason.Where(p => p.Vanity == "LICEN").ToList().ForEach(p => p.IsInputRequired = true);
                mobCPCubaTravelReason.ForEach(p => p.ContentFull = p.ContentFull.Replace("<p>", "").Replace("</p>", ""));
            }
            return mobCPCubaTravelReason;
        }
        public async Task<CSLContentMessagesResponse> GetBookingRTICMSContentMessages(MOBRequest request, Session session)//, List<LogEntry> logEntries)
        {
            #region
            MobileCMSContentRequest mobMobileCMSContentRequest = new MobileCMSContentRequest();
            mobMobileCMSContentRequest.Token = session.Token;
            mobMobileCMSContentRequest.Application = request.Application;
            mobMobileCMSContentRequest.GroupName = _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages");
            string jsonResponse = await GETCSLCMSContent(mobMobileCMSContentRequest, true);
            CSLContentMessagesResponse response = new CSLContentMessagesResponse();
            if (!string.IsNullOrEmpty(jsonResponse))
            {
                response = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(jsonResponse);
                if (response != null && (Convert.ToBoolean(response.Status) && response.Messages != null))
                {
                    if (!_configuration.GetValue<bool>("DisableSDLEmptyTitleFix"))
                    {
                        response.Messages = response.Messages.Where(l => l.Title != null)?.ToList();
                    }
                    await _cachingService.SaveCache<string>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID") + ObjectNames.MOBCSLContentMessagesResponseFullName, jsonResponse, request.TransactionId, new TimeSpan(1, 30, 0)).ConfigureAwait(false);
                   
                }
                else
                {
                    _logger.LogWarning("GetBookingRTICMSContentMessages response does not contain expected messages {@CMSResponse}", JsonConvert.SerializeObject(response));
                }
            }
            else
            {
                _logger.LogWarning("GetBookingRTICMSContentMessages-EmptyJSONReturnedBYCMSService {@CMSResponse}", JsonConvert.SerializeObject(response));
            }
            return response;
            #endregion
        }

    }
}
