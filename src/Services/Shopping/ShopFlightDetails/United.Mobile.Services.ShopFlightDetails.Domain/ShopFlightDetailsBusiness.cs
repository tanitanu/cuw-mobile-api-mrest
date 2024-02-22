using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Services.FlightShopping.Common;
using United.Utility.Helper;

namespace United.Mobile.Services.ShopFlightDetails.Domain
{
    public class ShopFlightDetailsBusiness : IShopFlightDetailsBusiness
    {
        private readonly ICacheLog<ShopFlightDetailsBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IFlightShoppingService _flightShoppingService;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly ICMSContentService _cMSContentService;
        private readonly ICachingService _cachingService;
        private readonly IDPService _dPService;

        public ShopFlightDetailsBusiness(ICacheLog<ShopFlightDetailsBusiness> logger
            , IConfiguration configuration
            , IHeaders headers
            , ISessionHelperService sessionHelperService
            , IFlightShoppingService flightShoppingService
            , IShoppingSessionHelper shoppingSessionHelper, ICMSContentService cMSContentService
            , ICachingService cachingService
            , IDPService dPService)
        {
            _logger = logger;
            _headers = headers;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _flightShoppingService = flightShoppingService;
            _shoppingSessionHelper = shoppingSessionHelper;
            _cMSContentService = cMSContentService;
            _cachingService = cachingService;
            _dPService = dPService;
        }

        public async Task<OnTimePerformanceResponse> GetONTimePerformence(OnTimePerformanceRequest onTimePerformanceRequest)
        {
            OnTimePerformanceResponse response = new OnTimePerformanceResponse();
            await _shoppingSessionHelper.GetBookingFlowSession(onTimePerformanceRequest.SessionId);
            if (!string.IsNullOrEmpty(onTimePerformanceRequest.SessionId))
            {
                response.OnTimePerformance = await GetOnTimePerformanceFsrRedesign(onTimePerformanceRequest.TransactionId
                    , onTimePerformanceRequest.Application.Id
                    , onTimePerformanceRequest.Application.Version.Major
                    , onTimePerformanceRequest.DeviceId
                    , onTimePerformanceRequest.MarketingCarrierCode
                    , onTimePerformanceRequest.FlightNumber
                    , onTimePerformanceRequest.DepartureAirportCode
                    , onTimePerformanceRequest.ArrivalAirportCode
                    , onTimePerformanceRequest.FlightDate
                    , onTimePerformanceRequest.SessionId);
                response.SessionId = onTimePerformanceRequest.SessionId;
            }
            response.TransactionId = onTimePerformanceRequest.TransactionId;
            return await Task.FromResult(response);
        }
        private async Task<OnTimePerformanceInformation> GetOnTimePerformanceFsrRedesign(string transactionId, int applicationid, string appVersion, string deviceId, string carrierCode, string flightNumber, string origin, string destination, string flightDate, string sessionId)
        {

            string token = string.Empty;
            Session persistedSession = await _sessionHelperService.GetSession<Session>(sessionId, new Session().ObjectName, new List<string> { sessionId, new Session().ObjectName }).ConfigureAwait(false);
            if (persistedSession != null)
            {
                token = persistedSession.Token;
            }


            OnTimePerformanceInformation objOTP = new OnTimePerformanceInformation();

            var objDOTAirlinePerformance = await _flightShoppingService.GetOnTimePerformanceInfo<OnTimePerformanceInfoResponse>(token, carrierCode, flightNumber, origin, destination, flightDate, sessionId);


            if (objDOTAirlinePerformance?.Errors != null)
            {
                if (_configuration.GetValue<bool>("EnableOntimePerformanceFixFsrRedesign"))
                {
                    objOTP = PopulateOnTimePerformanceSHOPFsrRedesign1(objDOTAirlinePerformance.OnTimePerformanceInformation);
                }
                else
                    objOTP = PopulateOnTimePerformanceSHOPFsrRedesign(objDOTAirlinePerformance.OnTimePerformanceInformation);
            }

            return objOTP;

        }

        private OnTimePerformanceInformation PopulateOnTimePerformanceSHOPFsrRedesign1(United.Service.Presentation.ReferenceDataModel.DOTAirlinePerformance onTimePerformance)
        {
            OnTimePerformanceInformation shopOnTimePerformance = null;
            if (_configuration.GetValue<bool>("ReturnOnTimePerformance"))
            {
                shopOnTimePerformance = new OnTimePerformanceInformation();
                string dOTOnTimeNotAvailableMessage = _configuration.GetValue<string>("DOTOnTimeNotAvailableMessage");
                string dotOnTimeMessages = HttpUtility.HtmlDecode(_configuration.GetValue<string>("DOTOnTimeMessagesFsrRedesign"));
                #region
                if (onTimePerformance != null)
                {
                    shopOnTimePerformance.DotMessagesHtml = string.Empty;
                    var source = onTimePerformance.Source;
                    string brDotOnTimeMessages;

                    var onTimePerformanceItems = new List<MOBTypeOption>();
                    var boldValue = _configuration.GetValue<int>("OnTimePerformanceBoldPercentage");
                    boldValue = boldValue == 0 ? 50 : boldValue;

                    if ((!string.IsNullOrEmpty(source) && source.ToUpper().Equals("BR")) || onTimePerformance.ArrivalMoreThan30MinLateRate > 0 || onTimePerformance.ArrivalMoreThan60MinLateRate > 0)
                    {
                        if (!string.IsNullOrEmpty(source) && source.ToUpper().Equals("BR"))
                        {
                            brDotOnTimeMessages = _configuration.GetValue<string>("DOTOnTimeMessagesBrazilFsrRedesign");
                            dotOnTimeMessages += brDotOnTimeMessages;
                        }

                        onTimePerformanceItems.Add(GetOnTimePerformanceItem("Delayed 30 min. or more", onTimePerformance.ArrivalMoreThan30MinLateRate, true, boldValue));
                        onTimePerformanceItems.Add(GetOnTimePerformanceItem("Delayed 60 min. or more", onTimePerformance.ArrivalMoreThan60MinLateRate, true, boldValue));
                    }
                    else
                    {
                        if (!int.TryParse(onTimePerformance.ArrivalLateRate?.Replace("%", ""), out int delay))
                        {
                            delay = -1;
                            onTimePerformance.ArrivalLateRate = "";
                        }
                        if (!int.TryParse(onTimePerformance.ArrivalOnTimeRate?.Replace("%", ""), out int onTime))
                        {
                            onTime = -1;
                            onTimePerformance.ArrivalOnTimeRate = "";
                        }
                        onTimePerformanceItems.Add(GetOnTimePerformanceItem("On-time", onTime));
                        onTimePerformanceItems.Add(GetOnTimePerformanceItem("Late", delay));
                    }
                    shopOnTimePerformance.DotMessagesHtml = dotOnTimeMessages;
                    if (_configuration.GetValue<bool>("EnableOntimePerformance21FFix"))
                    {
                        if ((!string.IsNullOrEmpty(source) && source.ToUpper().Equals("BR")))
                        {
                            onTimePerformanceItems.Add(GetOnTimePerformanceItem("Canceled*", onTimePerformance.CancellationRate));
                        }
                        else
                        {
                            onTimePerformanceItems.Add(GetOnTimePerformanceItem("Canceled", onTimePerformance.CancellationRate));
                        }

                        shopOnTimePerformance.TimePeriod = (!string.IsNullOrEmpty(onTimePerformance.Month) && !string.IsNullOrEmpty(onTimePerformance.Year)) ? onTimePerformance.Month + " " + onTimePerformance.Year : string.Empty;
                    }
                    else
                    {
                        onTimePerformanceItems.Add(GetOnTimePerformanceItem("Canceled", onTimePerformance.CancellationRate));
                    }
                    shopOnTimePerformance.OnTimePerformanceItems = onTimePerformanceItems;
                    if (onTimePerformance.ArrivalMoreThan30MinLateRate <= 0 && onTimePerformance.ArrivalMoreThan60MinLateRate <= 0 && onTimePerformance.CancellationRate <= 0 && string.IsNullOrEmpty(onTimePerformance.ArrivalOnTimeRate))
                    {
                        shopOnTimePerformance.OnTimeNotAvailableMessage = dOTOnTimeNotAvailableMessage;
                    }
                }
                else
                {
                    if (_configuration.GetValue<bool>("EnableOntimePerformance21FFix"))
                    {
                        shopOnTimePerformance.DotMessagesHtml = dotOnTimeMessages;
                    }
                    shopOnTimePerformance.OnTimeNotAvailableMessage = dOTOnTimeNotAvailableMessage;
                }
                #endregion
            }
            return shopOnTimePerformance;
        }

        private OnTimePerformanceInformation PopulateOnTimePerformanceSHOPFsrRedesign(United.Service.Presentation.ReferenceDataModel.DOTAirlinePerformance onTimePerformance)
        {
            OnTimePerformanceInformation shopOnTimePerformance = null;
            if (_configuration.GetValue<bool>("ReturnOnTimePerformance"))
            {
                shopOnTimePerformance = new OnTimePerformanceInformation();
                string dOTOnTimeNotAvailableMessage = _configuration.GetValue<string>("DOTOnTimeNotAvailableMessage");
                #region
                if (onTimePerformance != null)
                {
                    shopOnTimePerformance.DotMessagesHtml = string.Empty;
                    var source = onTimePerformance.Source;
                    string[] dotOnTimeMessages;
                    string[] brDotOnTimeMessages;
                    dotOnTimeMessages = _configuration.GetValue<string>("DOTOnTimeMessages").Split('|');
                    var dotMessageBuilder = new StringBuilder();
                    dotMessageBuilder.Append(dotOnTimeMessages[0].Replace("On time:", "<strong>On-time:</strong>"));
                    dotMessageBuilder.Append("<br/><br/>");
                    dotMessageBuilder.Append(dotOnTimeMessages[1].Replace("Late:", "<strong>Late:</strong>"));
                    dotMessageBuilder.Append("<br/><br/>");
                    dotMessageBuilder.Append(dotOnTimeMessages[2].Replace("Canceled:", "<strong>Canceled:</strong>"));
                    var onTimePerformanceItems = new List<MOBTypeOption>();
                    var boldValue = _configuration.GetValue<int>("OnTimePerformanceBoldPercentage");
                    boldValue = boldValue == 0 ? 50 : boldValue;

                    if (!string.IsNullOrEmpty(source) && source.ToUpper().Equals("BR"))
                    {
                        brDotOnTimeMessages = _configuration.GetValue<string>("DOTOnTimeMessagesBrazil").Split('|');
                        dotMessageBuilder.Append("<br/><br/>");
                        dotMessageBuilder.Append("* " + brDotOnTimeMessages[0]);
                        dotMessageBuilder.Append("<br/><br/>");
                        dotMessageBuilder.Append("<strong>Delayed:</strong> " + brDotOnTimeMessages[1]);
                        dotMessageBuilder.Append("<br/><br/>");
                        dotMessageBuilder.Append(brDotOnTimeMessages[2]);
                        if (!string.IsNullOrEmpty(onTimePerformance.IsArrivalOnTimeLessThan50Percent) &&
                            onTimePerformance.IsArrivalOnTimeLessThan50Percent.ToUpper().Equals("TRUE"))
                        {
                            onTimePerformanceItems.Add(GetOnTimePerformanceItem("Delayed 30 min. or more", onTimePerformance.ArrivalMoreThan30MinLateRate, true, boldValue));
                            onTimePerformanceItems.Add(GetOnTimePerformanceItem("Delayed 60 min. or more", onTimePerformance.ArrivalMoreThan60MinLateRate, true, boldValue));
                        }
                        else
                        {
                            if (!int.TryParse(onTimePerformance.ArrivalLateRate?.Replace("%", ""), out int delay))
                            {
                                delay = -1;
                                onTimePerformance.ArrivalLateRate = "";
                            }
                            if (!int.TryParse(onTimePerformance.ArrivalOnTimeRate?.Replace("%", ""), out int onTime))
                            {
                                onTime = -1;
                                onTimePerformance.ArrivalOnTimeRate = "";
                            }

                            onTimePerformanceItems.Add(GetOnTimePerformanceItem("On-time", onTime));
                            onTimePerformanceItems.Add(GetOnTimePerformanceItem("Late", delay));
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(onTimePerformance.IsArrivalOnTimeLessThan50Percent) &&
                            onTimePerformance.IsArrivalOnTimeLessThan50Percent.ToUpper().Equals("TRUE"))
                        {
                            onTimePerformanceItems.Add(GetOnTimePerformanceItem("Delayed 30 min. or more", onTimePerformance.ArrivalMoreThan30MinLateRate, true, boldValue));
                            onTimePerformanceItems.Add(GetOnTimePerformanceItem("Delayed 60 min. or more", onTimePerformance.ArrivalMoreThan60MinLateRate, true, boldValue));
                        }
                        else
                        {
                            if (!int.TryParse(onTimePerformance.ArrivalLateRate?.Replace("%", ""), out int delay))
                            {
                                delay = -1;
                                onTimePerformance.ArrivalLateRate = "";
                            }
                            if (!int.TryParse(onTimePerformance.ArrivalOnTimeRate?.Replace("%", ""), out int onTime))
                            {
                                onTime = -1;
                                onTimePerformance.ArrivalOnTimeRate = "";
                            }
                            onTimePerformanceItems.Add(GetOnTimePerformanceItem("On-time", onTime));
                            onTimePerformanceItems.Add(GetOnTimePerformanceItem("Late", delay));
                        }
                    }
                    shopOnTimePerformance.DotMessagesHtml = dotMessageBuilder.ToString();

                    onTimePerformanceItems.Add(GetOnTimePerformanceItem("Cancelled", onTimePerformance.CancellationRate));
                    shopOnTimePerformance.OnTimePerformanceItems = onTimePerformanceItems;
                    if (onTimePerformance.ArrivalMoreThan30MinLateRate <= 0 && onTimePerformance.ArrivalMoreThan60MinLateRate <= 0 && onTimePerformance.CancellationRate <= 0 && string.IsNullOrEmpty(onTimePerformance.ArrivalOnTimeRate))
                    {
                        shopOnTimePerformance.DotMessagesHtml = null;
                        shopOnTimePerformance.OnTimeNotAvailableMessage = dOTOnTimeNotAvailableMessage;
                    }
                }
                else
                {
                    shopOnTimePerformance.OnTimeNotAvailableMessage = dOTOnTimeNotAvailableMessage;
                }
                #endregion
            }
            return shopOnTimePerformance;
        }
        private MOBTypeOption GetOnTimePerformanceItem(string key, int value, bool considerBold = false, int boldPercent = 50)
        {
            string strValue = string.Empty;
            if (considerBold)
            {
                strValue = value > boldPercent ? $"<strong>{value}%</strong>" : $"{value}%";
            }
            else
            {
                strValue = value < 0 ? "---" : $"{value}%";
            }

            return new MOBTypeOption() { Key = key, Value = strValue };
        }
        public async Task<RefreshCacheForSDLContentResponse> RefreshCacheForSDLContent(string groupName, string cacheKey)
        {
            if (!(IsValidSDLContentGroupNameCacheKeyCombination(groupName, cacheKey)))
            {
                throw new Exception("Invalid GroupName,CacheKey combination");
            }
            bool isCacheUpdated = false;
            RefreshCacheForSDLContentResponse response = new RefreshCacheForSDLContentResponse();
            CSLContentMessagesResponse sdlResponse = null;
            string deviceId = "RefreshCacheForSDLContent_"+ DateTime.Now.ToString("MMM_dd_yyyy_HH:mm:ss"); //RefreshCacheForSDLContent_Jan_20_2023_21:20:06
            string transactionId = "Trans01";
            try
            {
                string token = await _dPService.GetAnonymousTokenV2(1, deviceId, _configuration, "dpTokenRequest", false);

                MOBCSLContentMessagesRequest sdlReqeust = new MOBCSLContentMessagesRequest
                {
                    Lang = "en",
                    Pos = "us",
                    Channel = "mobileapp",
                    Listname = new List<string>(),
                    LocationCodes = new List<string>(),
                    Groupname = groupName,
                    Usecache = false
                };

                string jsonRequest = JsonConvert.SerializeObject(sdlReqeust);

                sdlResponse = await _cMSContentService.GetSDLContentByGroupName<CSLContentMessagesResponse>(token, "message", jsonRequest,"").ConfigureAwait(false);

                if (sdlResponse == null)
                {
                    _logger.LogError("GetSDLContentByGroupName Failed to deserialize CSL response");
                    throw new MOBUnitedException("Failed to deserialize CSL response");
                }

                if (sdlResponse.Errors.Count > 0)
                {
                    string errorMsg = String.Join(" ", sdlResponse.Errors.Select(x => x.Message));
                    _logger.LogError("GetSDLContentByGroupName {@CSLCallError}", errorMsg);
                    throw new MOBUnitedException("CSL service returned errors");
                }

                if (sdlResponse != null && (sdlResponse.Errors == null || (sdlResponse.Errors != null && sdlResponse.Errors.Count == 0)) && (Convert.ToBoolean(sdlResponse.Status) && sdlResponse.Messages != null))
                {
                    if (!_configuration.GetValue<bool>("DisableSDLEmptyTitleFix"))
                    {
                        sdlResponse.Messages = sdlResponse.Messages.Where(l => l.Title != null)?.ToList();
                    }
                    isCacheUpdated = await _cachingService.SaveCache<CSLContentMessagesResponse>(cacheKey, sdlResponse, transactionId, new TimeSpan(1, 30, 0)).ConfigureAwait(false);
                }


                if (isCacheUpdated)
                {
                    var getSDL = await _cachingService.GetCache<string>(cacheKey, transactionId).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(getSDL))
                    {
                        sdlResponse = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(getSDL);
                        if (sdlResponse != null && sdlResponse.Messages != null)
                        {
                            response.SDLContentMessageResponse = sdlResponse;
                            return response;
                        }
                    }
                    else
                    {
                        throw new Exception("Couldn't retrieve SDL content from Cache");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return response;
        }

        public bool IsValidSDLContentGroupNameCacheKeyCombination(string groupName, string cacheKey)
        {
            string docName = "MOBCSLContentMessagesResponse";
            Dictionary<string, string> GroupNameLookUp = new Dictionary<string, string>()
            {
                {_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID")+ docName, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages") },
                {_configuration.GetValue<string>("U4BCorporateContentMessageCache")+ docName, _configuration.GetValue<string>("U4BCorporateContentMessageGroupName") },
                {_configuration.GetValue<string>("MANAGERES_CMSContentMessagesCached_StaticGUID")+ docName,_configuration.GetValue<string>("CMSContentMessages_GroupName_MANAGERES_Messages") },
                {_configuration.GetValue<string>("MANAGERES_CMSContentMessagesCached_DestImg")+ docName,"MANAGERES:VIEWRES" },
                {_configuration.GetValue<string>("ManageReservation_Offers_CMSContentMessagesCached_StaticGUID")+ docName,"ManageReservation:Offers" },
                {_configuration.GetValue<string>("CMSContentMessages_GroupName_Baggage_Messages")+ docName,_configuration.GetValue<string>("CMSContentMessages_GroupName_Baggage_Messages") }
            };
            if (GroupNameLookUp.ContainsKey(cacheKey))
            {
                return GroupNameLookUp[cacheKey].ToUpper().Trim() == groupName.ToUpper().Trim();
            }
            return default;
        }
    }
}
