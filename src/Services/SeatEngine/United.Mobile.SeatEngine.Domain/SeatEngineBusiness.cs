using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.SeatEngine;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.SeatMapEngine;
using United.Services.FlightShopping.Common.Extensions;
using United.Utility.Enum;
using United.Utility.Helper;
using Session = United.Mobile.Model.Common.Session;

namespace United.Mobile.SeatEngine.Domain
{
    public class SeatEngineBusiness : ISeatEngineBusiness
    {
        private readonly ICacheLog<SeatEngineBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDPService _tokenService;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IHeaders _headers;
        private readonly ISeatMapAvailabilityService _seatMapAvailabilityService;
        private readonly IOnTimePerformanceInfoService _onTimePerformanceInfoService;
        private readonly IComplimentaryUpgradeService _complimentaryUpgradeOfferService;        
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly IFeatureSettings _featureSettings;  
        public SeatEngineBusiness(ICacheLog<SeatEngineBusiness> logger
            , IConfiguration configuration
            , IDPService tokenService
            , ISessionHelperService sessionHelperService
            , IHeaders headers
            , ISeatMapAvailabilityService seatMapAvailabilityService
            , IOnTimePerformanceInfoService onTimePerformanceInfoService
            , IComplimentaryUpgradeService complimentaryUpgradeOfferService  
            , IShoppingUtility shoppingUtility
            , IFFCShoppingcs fFCShoppingcs
            ,IFeatureSettings featureSettings
            )
        {
            _logger = logger;
            _configuration = configuration;
            _tokenService = tokenService;
            _sessionHelperService = sessionHelperService;
            _headers = headers;
            _seatMapAvailabilityService = seatMapAvailabilityService;
            _onTimePerformanceInfoService = onTimePerformanceInfoService;
            _complimentaryUpgradeOfferService = complimentaryUpgradeOfferService;
            _shoppingUtility = shoppingUtility;
            _fFCShoppingcs = fFCShoppingcs;
            _featureSettings=featureSettings;
        }

        public async Task<MOBSeatMapResponse> PreviewSeatMap(MOBSeatMapRequest request)
        {
            MOBSHOPFlight flight = null;
            MOBSeatMapResponse response = new MOBSeatMapResponse();
            try
            {
                bool isXMLtoAWSMigrationEnabled = _configuration.GetValue<bool>("EnableCSL30BookingPreviewSeatMap");
                request.DeviceId = !String.IsNullOrEmpty(request.DeviceId) ? request.DeviceId: !string.IsNullOrEmpty(request.TransactionId) ? request.TransactionId.Split('|')[0]:string.Empty ;

                _logger.LogInformation("PreviewSeatMap {@isXMLtoAWSMigrationEnabled} {@request}", isXMLtoAWSMigrationEnabled ? "PreviewSeatMap" : "PreviewSeatMapCSLRequest", JsonConvert.SerializeObject(request));

                response.FlightNumber = request.FlightNumber;
                response.SessionId = request.SessionId;
                response.LanguageCode = request.LanguageCode;
                response.TransactionId = request.TransactionId;
                response.FlightDate = request.FlightDate;
                response.OperatingCarrierCode = request.OperatingCarrierCode;
                response.SeatMapRequest = request;

                if (request.FFlight != null && request.FFlight.Flights != null && request.FFlight.Flights.Count > 0)
                {
                    flight = request.FFlight.Flights.FirstOrDefault(p => p.FlightNumber == request.FlightNumber);
                    string MarketingCarrierCode = string.IsNullOrEmpty(flight.MarketingCarrier) ? "UA" : flight.MarketingCarrier;
                    response.MarketingCarrierCode = MarketingCarrierCode;

                    if (_shoppingUtility.EnableOAMessageUpdate(request.Application.Id, request.Application.Version.Major))
                    {
                        if (_shoppingUtility.IsOAReadOnlySeatMap(request.OperatingCarrierCode))
                        {
                            throw new MOBUnitedException(await _shoppingUtility.GetSeatMapErrorMessages("PreviewSeatMapMessageForLH",request.SessionId,request.Application.Id,request.Application.Version.Major).ConfigureAwait(false));
                        }
                        else if (_shoppingUtility.IsInterLine(request.OperatingCarrierCode, MarketingCarrierCode))
                        {                         
                            throw new MOBUnitedException(await _shoppingUtility.GetSeatMapErrorMessages("PreviewSeatMapMessageForOtherAirlines", request.SessionId, request.Application.Id, request.Application.Version.Major).ConfigureAwait(false));
                        }
                        else if (!_shoppingUtility.IsOperatedByUA(request.OperatingCarrierCode, MarketingCarrierCode) &&
                            !_shoppingUtility.IsOperatedBySupportedAirlines(request.OperatingCarrierCode, MarketingCarrierCode) &&
                            !_shoppingUtility.IsLandTransport(request.EquipmentType))
                        {
                            if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && _configuration.GetValue<string>("SupportedAirlinesFareComparison").Contains(request.OperatingCarrierCode.Trim().ToUpper()))
                            {
                                throw new MOBUnitedException(await _shoppingUtility.GetSeatMapErrorMessages("OAPartnerPreviewSeatMapMessageForNonEligibleOA", request.SessionId, request.Application.Id, request.Application.Version.Major).ConfigureAwait(false));
                            }
                               throw new MOBUnitedException(await _shoppingUtility.GetSeatMapErrorMessages("PreviewSeatMapMessageForNonEligibleOA", request.SessionId, request.Application.Id, request.Application.Version.Major).ConfigureAwait(false));
                        }
                    }
                    else
                    {

                        if (IsSeatMapSupportedOa(request.OperatingCarrierCode, request.MarketingCarrierCode) && OaSeatMapSupportedVersion(request.Application.Id, request.Application.Version.Major)
                        && !EnableLufthansa(request.OperatingCarrierCode))
                        {
                            Session session = new Session();
                            session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string>() { request.SessionId, session.ObjectName });
                            if (session != null && !session.IsReshopChange)
                            {
                                throw new MOBUnitedException(_configuration.GetValue<string>("PreviewSeatMapMessageForOtherAirlines"));
                            }
                        }
                        else if (EnableLufthansaForHigherVersion(request.OperatingCarrierCode, request.Application.Id, request.Application.Version.Major))
                        {
                            Session session = new Session();

                            session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string>() { request.SessionId, session.ObjectName });
                            if (session != null && !session.IsReshopChange)
                            {
                                throw new MOBUnitedException(_configuration.GetValue<string>("PreviewSeatMapMessageForAC"));
                            }
                        }
                        else if (EnableAirCanada(request.Application.Id, request.Application.Version.Major))
                        {
                            Session session = new Session();
                            session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string>() { request.SessionId, session.ObjectName });
                            if (session != null && !session.IsReshopChange)
                            {
                                string readOnlySeatMapinBookingPathOAAirlines = string.Empty;
                                readOnlySeatMapinBookingPathOAAirlines = _configuration.GetValue<string>("ReadonlySeatMapinBookingPathOAAirlines");
                                string[] readOnlySeatMapAirlines = { };
                                if (!string.IsNullOrEmpty(readOnlySeatMapinBookingPathOAAirlines))
                                {
                                    readOnlySeatMapAirlines = readOnlySeatMapinBookingPathOAAirlines.Split(',');
                                    foreach (string airline in readOnlySeatMapAirlines)
                                    {
                                        if (request.OperatingCarrierCode.ToUpper().Equals(airline.Trim().ToUpper()))
                                        {
                                            throw new MOBUnitedException(_configuration.GetValue<string>("SeatMapUnavailableOtherAirlines"));
                                        }
                                    }
                                }

                            }
                        }
                    }

                    response.SeatMap = await GetCSL30SeatMap(request, FlowType.BOOKING_PREVIEW_SEATMAP);
                    ClearSeatsIfProductNotAvailableExceptEconomy(response.SeatMap, request.FlightNumber, request.FFlight);

                    #region check version For DAA
                    MOBSeatMapRequest req = new MOBSeatMapRequest();
                    req.Application = request.Application;
                    req.Application.Version.DisplayText = request.Application.Version.DisplayText == string.Empty ? null : request.Application.Version.DisplayText;
                    req.Application.Version.Build = request.Application.Version.Build == string.Empty ? null : request.Application.Version.Build;

                    MOBSeatMapResponse res = new MOBSeatMapResponse();
                    res.SeatMap = response.SeatMap;
                    ChangeDAAMapForUpperVersion(request.Application.Version.Major, req, res);
                    #endregion
                    if (!string.IsNullOrEmpty(request.SessionId))
                    {
                        string fltDepDt = DateTime.Parse(flight.DepartureDateTime, CultureInfo.InvariantCulture).ToString("yyyyMMdd");
                        response.OnTimePerformance = await GetOnTimePerformance(request.TransactionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, MarketingCarrierCode, request.FlightNumber, flight.Origin, flight.Destination, fltDepDt, request.SessionId);
                    }
                }
            }
            catch (MOBUnitedException uaex)
            {
                var exceptionWrapper = new MOBExceptionWrapper(uaex);

                _logger.LogWarning("PreviewSeatMap Error {@UnitedException}", JsonConvert.SerializeObject(exceptionWrapper));
                
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }

            return response;
        }
     
        public async Task<MOBSeatMapResponse> GetSeatMap(int applicationid, string appVersion, string accessCode, string transactionId, string carrierCode, string flightNumber, string flightDate, string departureAirportCode, string arrivalAirportCode, string languageCode, string deviceId, string sessionId)
        {
            MOBSeatMapRequest request = new MOBSeatMapRequest();
            MOBSeatMapResponse response = new MOBSeatMapResponse();

            try
            {
                request.AccessCode = accessCode;
                request.TransactionId = transactionId;
                request.Application = new MOBApplication();
                request.Application.Id = applicationid;
                request.LanguageCode = languageCode;
                request.FlightNumber = flightNumber;
                request.FlightDate = flightDate;
                request.DepartureAirportCode = departureAirportCode;
                request.ArrivalAirportCode = arrivalAirportCode;
                request.LanguageCode = languageCode;
                request.MarketingCarrierCode = carrierCode;
                request.OperatingCarrierCode = "UA";
                deviceId = !String.IsNullOrEmpty(deviceId) ? deviceId : !string.IsNullOrEmpty(request.TransactionId)? request.TransactionId.Split('|')[0]:string.Empty ;
                request.DeviceId = deviceId;
                request.SessionId = sessionId;
                request.Application.Version = new MOBVersion { Major = appVersion };
                response.TransactionId = request.TransactionId;
                response.LanguageCode = request.LanguageCode;
                response.FlightNumber = flightNumber;
                response.FlightDate = flightDate;
                response.SessionId = sessionId;
                request.MarketingCarrierCode = carrierCode;
                request.OperatingCarrierCode = "UA";

                response.SeatMapRequest = request;
                response.SeatMap = await GetCSL30SeatMap(request, FlowType.ERES);

                #region check version For DAA
                ChangeDAAMapForUpperVersion(appVersion, request, response);
                #endregion
                if (!string.IsNullOrEmpty(sessionId))
                {
                    response.OnTimePerformance = await GetOnTimePerformance(transactionId, applicationid, appVersion, deviceId, carrierCode, flightNumber, departureAirportCode, arrivalAirportCode, flightDate, sessionId);
                }
            }
            catch (MOBUnitedException uaex)
            {
                var exceptionWrapper = new MOBExceptionWrapper(uaex);
                _logger.LogError("EresGetSeatMap Error {exceptionstack} {ApplicationId} {Appversion} {DeviceId} and {sessionId}", JsonConvert.SerializeObject(exceptionWrapper), applicationid, appVersion, deviceId, sessionId);
                _logger.LogError("EresGetSeatMap Error {exception} and {sessionId}", uaex.Message, sessionId);
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
            }

            return response;
        }

        private async Task<MOBSHOPOnTimePerformance> GetOnTimePerformance(string transactionId, int applicationid, string appVersion, string deviceId, string carrierCode, string flightNumber, string origin, string destination, string flightDate, string sessionId)
        {
            string token = string.Empty;
            Session persistedSession = new Session();
            persistedSession = await _sessionHelperService.GetSession<Session>(sessionId, new Session().ObjectName, new List<string>() { sessionId, new Session().ObjectName });
            
            if (persistedSession != null)
            {
                token = persistedSession.Token;
            }
            else
            {
                token = await _tokenService.GetAnonymousToken(applicationid, deviceId, _configuration);
            }

            MOBSHOPOnTimePerformance objOTP = new MOBSHOPOnTimePerformance();

            string url = string.Format("GetOnTimePerformanceInfo?MarketingCarrier={0}&FlightNumber={1}&Origin={2}&Destination={3}&DepartureDateTime={4}", carrierCode, flightNumber, origin, destination, DateTime.ParseExact(flightDate, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("MM/dd/yyyy"));

            #region ShuffleVIPSBasedOnCSS_r_DPTOken
            url = IsTokenMiddleOfFlowDPDeployment() ? ModifyVIPMiddleOfFlowDPDeployment(token, url) : url;
            string result = await _onTimePerformanceInfoService.GetOnTimePerformance(token, url, transactionId, applicationid, appVersion, deviceId, carrierCode, flightNumber, origin, destination, flightDate, sessionId);
            Services.FlightShopping.Common.OnTimePerformanceInfoResponse objDOTAirlinePerformance = null;

            if (!string.IsNullOrEmpty(result))
            {
                objDOTAirlinePerformance = DataContextJsonSerializer.DeserializeUseContract<Services.FlightShopping.Common.OnTimePerformanceInfoResponse>(result);
                if (objDOTAirlinePerformance.Errors != null)
                {
                    objOTP = PopulateOnTimePerformanceSHOP(objDOTAirlinePerformance.OnTimePerformanceInformation);
                }
            }
            #endregion

            return objOTP;
        }

        private MOBSHOPOnTimePerformance PopulateOnTimePerformanceSHOP(United.Service.Presentation.ReferenceDataModel.DOTAirlinePerformance onTimePerformance)
        {
            MOBSHOPOnTimePerformance shopOnTimePerformance = null;
            if (_configuration.GetValue<bool>("ReturnOnTimePerformance"))
            {
                #region
                if (onTimePerformance != null)
                {
                    shopOnTimePerformance = new MOBSHOPOnTimePerformance();
                    shopOnTimePerformance.Source = onTimePerformance.Source;
                    shopOnTimePerformance.DOTMessages = new MOBSHOPOnTimeDOTMessages();
                    string[] dotOnTimeMessages = null;
                    if (!string.IsNullOrEmpty(shopOnTimePerformance.Source) && shopOnTimePerformance.Source.ToUpper().Equals("BR"))
                    {
                        dotOnTimeMessages = _configuration.GetValue<string>("DOTOnTimeMessagesBrazil").Split('|');
                    }
                    else
                    {
                        dotOnTimeMessages = _configuration.GetValue<string>("DOTOnTimeMessages").Split('|');
                    }
                    shopOnTimePerformance.DOTMessages.CancellationPercentageMessage = dotOnTimeMessages[0].ToString();
                    shopOnTimePerformance.DOTMessages.DelayPercentageMessage = dotOnTimeMessages[1].ToString();
                    shopOnTimePerformance.DOTMessages.DelayAndCancellationPercentageMessage = dotOnTimeMessages[2].ToString();
                    shopOnTimePerformance.DOTMessages.DOTMessagePopUpButtonCaption = dotOnTimeMessages[3].ToString();

                    shopOnTimePerformance.EffectiveDate = string.Format("{0}, {1}", onTimePerformance.Month, onTimePerformance.Year);
                    shopOnTimePerformance.PctOnTimeCancelled = onTimePerformance.CancellationRate < 0 ? "---" : onTimePerformance.CancellationRate.ToString();

                    if (!string.IsNullOrEmpty(shopOnTimePerformance.Source) && shopOnTimePerformance.Source.ToUpper().Equals("BR"))
                    {
                        int delay = onTimePerformance.ArrivalMoreThan30MinLateRate + onTimePerformance.ArrivalMoreThan60MinLateRate;
                        shopOnTimePerformance.PctOnTimeDelayed = onTimePerformance.ArrivalMoreThan30MinLateRate < 0 ? "---" : onTimePerformance.ArrivalMoreThan30MinLateRate.ToString();
                        shopOnTimePerformance.PctOnTimeMax = onTimePerformance.ArrivalMoreThan60MinLateRate < 0 ? "---" : onTimePerformance.ArrivalMoreThan60MinLateRate.ToString();
                        shopOnTimePerformance.PctOnTimeMin = onTimePerformance.ArrivalMoreThan60MinLateRate < 0 ? "---" : onTimePerformance.ArrivalMoreThan60MinLateRate.ToString();
                    }
                    else
                    {
                        int delay = -1;
                        if (!string.IsNullOrEmpty(onTimePerformance.ArrivalLateRate))
                        {
                            if (!int.TryParse(onTimePerformance.ArrivalLateRate.Replace("%", ""), out delay))
                            {
                                delay = -1;
                                onTimePerformance.ArrivalLateRate = "";
                            }
                        }

                        shopOnTimePerformance.PctOnTimeDelayed = delay < 0 ? "---" : delay.ToString();
                        int onTime = -1;
                        if (!string.IsNullOrEmpty(onTimePerformance.ArrivalOnTimeRate))
                        {
                            if (!int.TryParse(onTimePerformance.ArrivalOnTimeRate.Replace("%", ""), out onTime))
                            {
                                onTime = -1;
                                onTimePerformance.ArrivalOnTimeRate = "";
                            }
                        }

                        shopOnTimePerformance.PctOnTimeMax = onTime < 0 ? "---" : onTime.ToString();
                        shopOnTimePerformance.PctOnTimeMin = onTime < 0 ? "---" : onTime.ToString();
                    }

                    if (onTimePerformance.ArrivalMoreThan30MinLateRate <= 0 && onTimePerformance.ArrivalMoreThan60MinLateRate <= 0 && onTimePerformance.CancellationRate <= 0 && string.IsNullOrEmpty(onTimePerformance.ArrivalOnTimeRate))
                    {
                        List<string> lstOnTimeNotAvailableMessage = new List<string>(1);
                        lstOnTimeNotAvailableMessage.Add(_configuration.GetValue<string>("DOTOnTimeNotAvailableMessage"));
                        shopOnTimePerformance.OnTimeNotAvailableMessage = lstOnTimeNotAvailableMessage;
                    }
                }
                else
                {
                    shopOnTimePerformance = new MOBSHOPOnTimePerformance();
                    shopOnTimePerformance.DOTMessages = new MOBSHOPOnTimeDOTMessages();
                    string[] dotOnTimeMessages = _configuration.GetValue<string>("DOTOnTimeMessages").Split('|');
                    shopOnTimePerformance.DOTMessages.CancellationPercentageMessage = dotOnTimeMessages[0].ToString();
                    shopOnTimePerformance.DOTMessages.DelayPercentageMessage = dotOnTimeMessages[1].ToString();
                    shopOnTimePerformance.DOTMessages.DelayAndCancellationPercentageMessage = dotOnTimeMessages[2].ToString();
                    shopOnTimePerformance.DOTMessages.DOTMessagePopUpButtonCaption = dotOnTimeMessages[3].ToString();

                    List<string> lstOnTimeNotAvailableMessage = new List<string>(1);
                    lstOnTimeNotAvailableMessage.Add(_configuration.GetValue<string>("DOTOnTimeNotAvailableMessage"));
                    shopOnTimePerformance.OnTimeNotAvailableMessage = lstOnTimeNotAvailableMessage;
                }
                #endregion
            }

            return shopOnTimePerformance;
        }

        private bool IsTokenMiddleOfFlowDPDeployment()
        {
            return (_configuration.GetValue<bool>("ShuffleVIPSBasedOnCSS_r_DPTOken") && _configuration.GetValue<bool>("EnableDpToken"));
        }

        private string ModifyVIPMiddleOfFlowDPDeployment(string token, string url)
        {
            url = token.Length < 50 ? url.Replace(_configuration.GetValue<string>("DPVIPforDeployment"), _configuration.GetValue<string>("CSSVIPforDeployment")) : url;

            _logger.LogInformation("ModifyVIPMiddleOfFlowDPDeployment URL:{url}", url);

            return url;
        }

        private async Task<MOBSeatMap> GetCSL30SeatMap(MOBSeatMapRequest mRequest, FlowType flowtype)
        {
            var seatmapresponse = new MOBSeatMap { };
            string[] channelInfo = _configuration.GetValue<string>("CSL30MBEChannelInfo").Split('|');

            string authToken = await _tokenService.GetAnonymousToken(mRequest.Application.Id, mRequest.DeviceId, _configuration);
            mRequest.FlightDate = DateTime.ParseExact(mRequest.FlightDate, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
            string cslstrResponse = await _seatMapAvailabilityService.GetCSL30SeatMap(authToken, channelInfo[0], channelInfo[1], mRequest.FlightNumber, mRequest.DepartureAirportCode, mRequest.ArrivalAirportCode, mRequest.FlightDate, mRequest.MarketingCarrierCode, mRequest.OperatingCarrierCode, mRequest.SessionId, mRequest.TransactionId);

            if (!string.IsNullOrEmpty(cslstrResponse))
            {
                var cslResponse = DataContextJsonSerializer.Deserialize<United.Definition.SeatCSL30.SeatMap>(cslstrResponse);
                if (!cslResponse.IsNullOrEmpty())
                {
                    seatmapresponse = await MapCSL30SeatmapResponse(mRequest, cslResponse, true);
                }
            }
            else
            {
                throw new MOBUnitedException(@"Advance seat assignments are not available through United for this flight. Please see an airport agent during check-in to receive your seat assignment.");
            }

            return seatmapresponse;
        }

        /// <summary>
        /// To map seats/seatmapavailability/api/SeatMap/UA
        /// </summary>
        /// <param name="mRequest"></param>
        /// <param name="seatMapResponse"></param>
        /// <param name="returnPolarisLegendforSeatMap"></param>
        /// <returns></returns>
        private async Task<MOBSeatMap> MapCSL30SeatmapResponse(MOBSeatMapRequest mRequest, United.Definition.SeatCSL30.SeatMap seatMapResponse, bool returnPolarisLegendforSeatMap = false)
        {
            MOBSeatMap objMOBSeatMap = null;

            bool IsPolarisBranding = _configuration.GetValue<bool>("IsPolarisCabinBrandingON");

            if (seatMapResponse != null
                && seatMapResponse.Errors.IsNullOrEmpty()
                && !seatMapResponse.Cabins.IsNullOrEmpty()
                && !seatMapResponse.AircraftInfo.IsNullOrEmpty()
                && !seatMapResponse.FlightInfo.IsNullOrEmpty())
            {

                objMOBSeatMap = new MOBSeatMap();
                List<string> cabinBrandingDescriptions = new List<string>();
                int numberOfCabins = (seatMapResponse.Cabins.Count > 3) ? 3 : seatMapResponse.Cabins.Count;

                bool isPreferredZoneEnabled = EnablePreferredZone(mRequest.Application.Id, mRequest.Application.Version.Major); // Check Preferred Zone based on App version - Returns true or false 
                objMOBSeatMap.SeatMapAvailabe = true;
                objMOBSeatMap.FleetType = !string.IsNullOrEmpty(seatMapResponse.AircraftInfo.Icr) ? seatMapResponse.AircraftInfo.Icr : string.Empty;
                objMOBSeatMap.LegId = "";
                int cabinCount = 0;

                foreach (United.Definition.SeatCSL30.Cabin cabin in seatMapResponse.Cabins)
                {
                    ++cabinCount;
                    MOBCabin tmpCabin = new MOBCabin();
                    tmpCabin.COS = cabin.CabinBrand;
                    tmpCabin.Configuration = cabin.Layout;
                    cabinBrandingDescriptions.Add(cabin.CabinBrand);

                    foreach (United.Definition.SeatCSL30.Row row in cabin.Rows)
                    {
                        MOBRow tmpRow = new MOBRow();
                        tmpRow.Number = row.Number.ToString();
                        //3.0 check for N# 1000
                        tmpRow.Wing = row.Wing;

                        //Selected Row
                        var monumentrow = cabin.MonumentRows.FirstOrDefault(x => x.VerticalGridNumber == row.VerticalGridNumber);

                        for (int i = 1; i <= cabin.ColumnCount; i++)
                        {
                            var seat = row.Seats.FirstOrDefault(x => x.HorizontalGridNumber == i);
                            var monumentseat = (!monumentrow.IsNullOrEmpty()) ? monumentrow.Monuments.FirstOrDefault(x => x.HorizontalGridNumber == i) : null;

                            //Add seat to row
                            MOBSeatB tmpSeat = null;
                            if (!seat.IsNullOrEmpty())
                            {
                                tmpSeat = new MOBSeatB();
                                tmpSeat.Exit = seat.IsExit;
                                tmpSeat.Number = seat.Number;
                                tmpSeat.Fee = "";
                                tmpSeat.LimitedRecline = !string.IsNullOrEmpty(seat.ReclineType)
                                    && seat.ReclineType.Equals("LIMITED", StringComparison.OrdinalIgnoreCase);

                                // Need to revisit this code// checking only for united economy might includ UPP
                                if (!string.IsNullOrEmpty(seat.SeatType)
                                    && !cabin.CabinBrand.Equals("United Economy", StringComparison.OrdinalIgnoreCase))
                                {
                                    tmpSeat.Program = GetSeatPositionAccessFromCSL30SeatMap(seat.SeatType);
                                }
                                tmpSeat.IsEPlus = !string.IsNullOrEmpty(seat.SeatType)
                                    && seat.SeatType.Equals(SeatType.BLUE.ToString(), StringComparison.OrdinalIgnoreCase);

                                tmpSeat.seatvalue = GetSeatValueFromCSL30SeatMap(seat, false, false, mRequest.Application, false, false, string.Empty, false);
                            }
                            else
                            {
                                //get monumemt seat and loop based on span - build this empty seat.
                                //monumentseat.HorizontalSpan
                                tmpSeat = new MOBSeatB
                                {
                                    Number = string.Empty,
                                    Fee = "",
                                    LimitedRecline = false,
                                    seatvalue = "-",
                                    Exit = (!monumentseat.IsNullOrEmpty()) ? monumentseat.IsExit : false,
                                };
                            }
                            tmpRow.Seats.Add(tmpSeat);
                        }

                        if (row.Number < 1000)
                            tmpCabin.Rows.Add(tmpRow);
                    }

                    tmpCabin.Configuration = tmpCabin.Configuration.Replace(" ", "-");

                    if (cabinCount == 4)
                    {
                        objMOBSeatMap.Cabins.Insert(2, tmpCabin);
                        tmpCabin.COS = "Upper Deck " + tmpCabin.COS;
                    }
                    else
                    {
                        objMOBSeatMap.Cabins.Add(tmpCabin);
                    }
                }

                objMOBSeatMap.SeatLegendId = await GetPolarisSeatMapLegendId(seatMapResponse.FlightInfo.DepartureAirport, seatMapResponse.FlightInfo.ArrivalAirport,
                    numberOfCabins, cabinBrandingDescriptions, mRequest.Application.Id, mRequest.Application.Version.Major, mRequest.SessionId, mRequest.TransactionId);

                EconomySeatsForBUSService(objMOBSeatMap);
            }
            else
            {
                if (seatMapResponse != null && seatMapResponse.Errors != null && seatMapResponse.Errors.Count > 0)
                {
                    throw new MOBUnitedException("9999", "Unable to get the seat map for the flight you requested.");
                }
                else
                {
                    objMOBSeatMap = new MOBSeatMap();
                    objMOBSeatMap.SeatMapAvailabe = false;
                    throw new MOBUnitedException(@"Advance seat assignments are not available through United for this flight. Please see an airport agent during check-in to receive your seat assignment.");
                }
            }

            return objMOBSeatMap;
        }

        private void EconomySeatsForBUSService(MOBSeatMap seats, bool operated = false)
        {

            if (_configuration.GetValue<bool>("EnableProjectTOM") && seats != null && seats.FleetType.Length > 1 && seats.FleetType.Substring(0, 2).ToUpper().Equals("BU"))
            {
                string seatMapLegendEntry = (_configuration.GetValue<string>("seatMapLegendEntry") != null) ? _configuration.GetValue<string>("seatMapLegendEntry") : string.Empty;
                string seatMapLegendKey = (_configuration.GetValue<string>("seatMapLegendKey") != null) ? _configuration.GetValue<string>("seatMapLegendKey") : string.Empty;
                seats.SeatLegendId = seatMapLegendKey + "|" + seatMapLegendEntry;

                seats.IsOaSeatMap = true;
                seats.Cabins[0].COS = string.Empty;
                seats.IsReadOnlySeatMap = !operated;
                seats.OperatedByText = operated ? _configuration.GetValue<string>("ProjectTOMOperatedByText") : string.Empty;

                foreach (var cabin in seats.Cabins)
                {
                    foreach (var row in cabin.Rows)
                    {
                        row.Number = row.Number.PadLeft(2, '0');
                        row.Wing = false;
                    }
                }
            }
        }

        private async Task<string> GetPolarisSeatMapLegendId(string from, string to, int numberOfCabins, List<string> polarisCabinBrandingDescriptions, int applicationId = -1, string appVersion = "", string sessionId = "", string transactionId = "")
        {
            #region
            _logger.LogInformation("GetPolarisSeatMapLegendId parameters - DepartureAirport:{@DeptAirport}, ArrivalAirport:{@ArrAirport}, NumberofCabins:{@Cabins}, CabinBrandingDescription:{@CabinDescription}", from, to, numberOfCabins, JsonConvert.SerializeObject(polarisCabinBrandingDescriptions));
            string seatMapLegendId = string.Empty;

            //POLARIS Cabin Branding SeatMapLegend Booking Path
            string seatMapLegendEntry1 = (_configuration.GetValue<string>("seatMapLegendEntry1") != null) ? _configuration.GetValue<string>("seatMapLegendEntry1") : string.Empty;
            string seatMapLegendEntry2 = (_configuration.GetValue<string>("seatMapLegendEntry2") != null) ? _configuration.GetValue<string>("seatMapLegendEntry2") : string.Empty;
            string seatMapLegendEntry3 = (_configuration.GetValue<string>("seatMapLegendEntry3") != null) ? _configuration.GetValue<string>("seatMapLegendEntry3") : string.Empty;
            string seatMapLegendEntry4 = (_configuration.GetValue<string>("seatMapLegendEntry4") != null) ? _configuration.GetValue<string>("seatMapLegendEntry4") : string.Empty;

            string seatMapLegendEntry5 = (_configuration.GetValue<string>("seatMapLegendEntry5") != null) ? _configuration.GetValue<string>("seatMapLegendEntry5") : string.Empty;
            string seatMapLegendEntry6 = (_configuration.GetValue<string>("seatMapLegendEntry6") != null) ? _configuration.GetValue<string>("seatMapLegendEntry6") : string.Empty;
            string seatMapLegendEntry7 = (_configuration.GetValue<string>("seatMapLegendEntry7") != null) ? _configuration.GetValue<string>("seatMapLegendEntry7") : string.Empty;
            string seatMapLegendEntry8 = (_configuration.GetValue<string>("seatMapLegendEntry8") != null) ? _configuration.GetValue<string>("seatMapLegendEntry8") : string.Empty;
            string seatMapLegendEntry9 = (_configuration.GetValue<string>("seatMapLegendEntry9") != null) ? _configuration.GetValue<string>("seatMapLegendEntry9") : string.Empty;
            string seatMapLegendEntry14 = string.Empty;
            string legendForPZA = string.Empty;
            string legendForUPP = string.Empty;
            // Preferred Seat //AB-223
            bool isPreferredZoneEnabled = EnablePreferredZone(applicationId, appVersion); // Check if preferred seat
            if (isPreferredZoneEnabled)
            {
                seatMapLegendEntry14 = (_configuration.GetValue<string>("seatMapLegendEntry14") != null) ? _configuration.GetValue<string>("seatMapLegendEntry14") : "";
                legendForPZA = "_PZA";
            }

            if (IsUPPSeatMapSupportedVersion(applicationId, appVersion) && numberOfCabins == 3 && polarisCabinBrandingDescriptions != null && polarisCabinBrandingDescriptions.Any(p => p.ToUpper() == "UNITED PREMIUM PLUS"))
            {
                legendForUPP = "_UPP";
                seatMapLegendId = "seatmap_legend1" + legendForPZA + legendForUPP + "|" + polarisCabinBrandingDescriptions[0].ToString() + "|" + polarisCabinBrandingDescriptions[1].ToString() + seatMapLegendEntry6 + seatMapLegendEntry14 + seatMapLegendEntry7 + seatMapLegendEntry8 + seatMapLegendEntry9;// Sample Value Could be Ex: seatmap_legend1|United Polaris Business|United Premium Plus|Economy Plus|Preferred Seat|Economy|Occupied Seat|Exit Row or Sample Value Could be Ex: seatmap_legend2|First|Business|Economy Plus|Economy|Occupied Seat|Exit Row
            }
            else
            {

                if (_configuration.GetValue<bool>("DisableComplimentaryUpgradeOnpremSqlService"))
                {
                    if (numberOfCabins == 1)
                    {
                        seatMapLegendId = "seatmap_legend6" + legendForPZA + seatMapLegendEntry6 + seatMapLegendEntry14 + seatMapLegendEntry7 + seatMapLegendEntry8 + seatMapLegendEntry9;
                    }
                    else if (numberOfCabins == 3)
                    {
                        seatMapLegendId = "seatmap_legend1" + legendForPZA + legendForUPP + "|" + polarisCabinBrandingDescriptions[0].ToString() + "|" + polarisCabinBrandingDescriptions[1].ToString() + seatMapLegendEntry6 + seatMapLegendEntry14 + seatMapLegendEntry7 + seatMapLegendEntry8 + seatMapLegendEntry9;
                    }
                    else//If number of cabin==2 or by default assiging legend5
                    {
                        seatMapLegendId = "seatmap_legend5" + legendForPZA + legendForUPP + "|" + polarisCabinBrandingDescriptions[0].ToString() + seatMapLegendEntry6 + seatMapLegendEntry14 + seatMapLegendEntry7 + seatMapLegendEntry8 + seatMapLegendEntry9;
                    }
                }
                else
                {
                    //OnPremSQLDB service call to get brandingId's
                    List<CabinBrand> lstCabinBrand = new List<CabinBrand>();
                    try
                    {
                        lstCabinBrand = await _complimentaryUpgradeOfferService.GetComplimentaryUpgradeOfferedFlagByCabinCount(from, to, numberOfCabins, sessionId, transactionId).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("OnPremSQLService-GetComplimentaryUpgradeOfferedFlagByCabinCount Error {@Exception}", JsonConvert.SerializeObject(ex));
                    }

                    if (lstCabinBrand.Count > 0)
                    {
                        #region
                        foreach (var cb in lstCabinBrand)
                        {
                            int secondCabinBrandingId = cb.SecondCabinBrandingId;
                            int thirdCabinBrandingId = cb.ThirdCabinBrandingId;

                            //AB-223,AB-224 Adding Preferred Seat To SeatLegendID
                            //Added the code to check the flag for Preferred Zone and app version > 2.1.60                                           
                            if (thirdCabinBrandingId == 0)
                            {
                                if (secondCabinBrandingId == 1)
                                {
                                    seatMapLegendId = "seatmap_legend5" + legendForPZA + "|" + polarisCabinBrandingDescriptions[0].ToString() + seatMapLegendEntry6 + seatMapLegendEntry14 + seatMapLegendEntry7 + seatMapLegendEntry8 + seatMapLegendEntry9; // Sample Value Could be Ex: seatmap_legend5|First|Economy Plus|Preferred Seat|Economy|Occupied Seat|Exit Row
                                }
                                else if (secondCabinBrandingId == 2)
                                {
                                    seatMapLegendId = "seatmap_legend4" + legendForPZA + "|" + polarisCabinBrandingDescriptions[0].ToString() + seatMapLegendEntry6 + seatMapLegendEntry14 + seatMapLegendEntry7 + seatMapLegendEntry8 + seatMapLegendEntry9;// Sample Value Could be Ex: seatmap_legend4|Business|Economy Plus|Preferred Seat|Economy|Occupied Seat|Exit Row
                                }
                                else if (secondCabinBrandingId == 3)
                                {
                                    seatMapLegendId = "seatmap_legend3" + legendForPZA + "|" + polarisCabinBrandingDescriptions[0].ToString() + seatMapLegendEntry6 + seatMapLegendEntry14 + seatMapLegendEntry7 + seatMapLegendEntry8 + seatMapLegendEntry9;// Sample Value Could be Ex: seatmap_legend3|United Polaris Business|Economy Plus|Preferred Seat|Economy|Occupied Seat|Exit Row or seatmap_legend4|Business|Economy Plus|Economy|Occupied Seat|Exit Row
                                }
                            }
                            else if (thirdCabinBrandingId == 1)
                            {
                                seatMapLegendId = "seatmap_legend2" + legendForPZA + "|" + polarisCabinBrandingDescriptions[0].ToString() + "|" + polarisCabinBrandingDescriptions[1].ToString() + seatMapLegendEntry6 + seatMapLegendEntry14 + seatMapLegendEntry7 + seatMapLegendEntry8 + seatMapLegendEntry9;// Sample Value Could be Ex: seatmap_legend2|First|Business|Economy Plus|Preferred Seat|Economy|Occupied Seat|Exit Row or seatmap_legend1|United Polaris First|United Polaris Business|Economy Plus|Economy|Occupied Seat|Exit Row 
                            }
                            else if (thirdCabinBrandingId == 4)
                            {
                                seatMapLegendId = "seatmap_legend1" + legendForPZA + "|" + polarisCabinBrandingDescriptions[0].ToString() + "|" + polarisCabinBrandingDescriptions[1].ToString() + seatMapLegendEntry6 + seatMapLegendEntry14 + seatMapLegendEntry7 + seatMapLegendEntry8 + seatMapLegendEntry9;// Sample Value Could be Ex: seatmap_legend1|United Polaris First|United Polaris Business|Economy Plus|Preferred Seat|Economy|Occupied Seat|Exit Row or Sample Value Could be Ex: seatmap_legend2|First|Business|Economy Plus|Economy|Occupied Seat|Exit Row
                            }
                        }

                        #endregion
                    }

                    if (string.IsNullOrEmpty(seatMapLegendId))
                    {
                        #region Adding Preferred Seat Legend ID
                        //Added the code to check the flag for Preferred Zone and app version > 2.1.60
                        //Changes added on 09/24/2018                
                        //Bug 213002 mAPP: Seat Map- Blank Legend is displayed for One Cabin Flights
                        //Bug 102152
                        if (!string.IsNullOrEmpty(appVersion) &&
                            IsApplicationVersionGreater(applicationId, appVersion, "AndroidFirstCabinVersion", "iPhoneFirstCabinVersion", "", "", true)
                            && numberOfCabins == 1 && polarisCabinBrandingDescriptions != null && polarisCabinBrandingDescriptions.Count > 0 &&
                            !string.IsNullOrEmpty(polarisCabinBrandingDescriptions[0].ToString().Trim()))
                        {
                            seatMapLegendId = "seatmap_legend6" + legendForPZA + seatMapLegendEntry6 + seatMapLegendEntry14 + seatMapLegendEntry7 + seatMapLegendEntry8 + seatMapLegendEntry9;
                        }
                        else
                        {
                            seatMapLegendId = "seatmap_legend5" + legendForPZA + "|" + polarisCabinBrandingDescriptions[0].ToString() + seatMapLegendEntry6 + seatMapLegendEntry14 + seatMapLegendEntry7 + seatMapLegendEntry8 + seatMapLegendEntry9;
                        }
                        #endregion
                    }
                }
            }

            _logger.LogInformation("GetPolarisSeatMapLegendId SeatMapLegendId:{@SeatMapLegendId}", seatMapLegendId);

            return seatMapLegendId;
            #endregion
        }

        private bool IsUPPSeatMapSupportedVersion(int appId, string appVersion)
        {
            if (!string.IsNullOrEmpty(appVersion) && appId != -1)
            {
                return _configuration.GetValue<bool>("EnableUPPSeatmap")
                    && IsApplicationVersionGreater(appId, appVersion, "AndroidUPPSeatmapVersion", "iPhoneUPPSeatmapVersion", "", "", true);
            }

            return false;
        }

        private string GetSeatValueFromCSL30SeatMap(Definition.SeatCSL30.Seat seat, bool disableEplus, bool disableSeats, MOBApplication application, bool isOaSeatMapSegment, bool isOaPremiumEconomyCabin, string pcuOfferAmountForthisCabin, bool cogStop)
        {
            string seatValue = string.Empty;

            if (seat != null && !string.IsNullOrEmpty(seat.SeatType))
            {
                if (seat.IsInoperative || seat.IsPermanentBlocked || seat.IsBlocked)
                {
                    seatValue = "X";
                }
                else if (seat.SeatType.Equals(SeatType.BLUE.ToString(), StringComparison.OrdinalIgnoreCase) || isOaPremiumEconomyCabin)
                {
                    seatValue = seat.IsAvailable && (disableEplus || cogStop) ? "X" : seat.IsAvailable ? "P" : "X";
                }
                else if (seat.SeatType.Equals(SeatType.PREF.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    seatValue = seat.IsAvailable ? "PZ" : "X";
                }
                else if (seat.SeatType.Equals(SeatType.STANDARD.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    seatValue = seat.IsAvailable ? "O" : "X";
                }
                else
                {
                    seatValue = seat.IsAvailable ? "O" : "X";
                }
            }

            return string.IsNullOrEmpty(seatValue) || (!string.IsNullOrEmpty(seatValue) && disableSeats && string.IsNullOrWhiteSpace(pcuOfferAmountForthisCabin)) ? "X" : seatValue;
        }

        private string GetSeatPositionAccessFromCSL30SeatMap(string seatType)
        {
            string seatPositionProgram = string.Empty;

            if (seatType.Equals(SeatType.FBLEFT.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                seatPositionProgram = Convert.ToString(SeatPosition.FBL);
            }
            else if (seatType.Equals(SeatType.FBRIGHT.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                seatPositionProgram = Convert.ToString(SeatPosition.FBR);
            }
            else if (seatType.Equals(SeatType.FBFRONT.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                seatPositionProgram = Convert.ToString(SeatPosition.FBF);
            }
            else if (seatType.Equals(SeatType.FBBACK.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                seatPositionProgram = Convert.ToString(SeatPosition.FBB);
            }
            else if (seatType.Equals(SeatType.DAAFRONTL.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                seatPositionProgram = Convert.ToString(SeatPosition.DAFL);
            }
            else if (seatType.Equals(SeatType.DAAFRONTR.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                seatPositionProgram = Convert.ToString(SeatPosition.DAFR);
            }
            else if (seatType.Equals(SeatType.DAALEFT.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                seatPositionProgram = Convert.ToString(SeatPosition.DAL);
            }
            else if (seatType.Equals(SeatType.DAARIGHT.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                seatPositionProgram = Convert.ToString(SeatPosition.DAR);
            }
            else if (seatType.Equals(SeatType.DAAFRONTRM.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                seatPositionProgram = Convert.ToString(SeatPosition.DAFRM);
            }

            return seatPositionProgram;
        }

        /// <summary>
        /// Check version for adding preferred zone seats.
        /// Preferred Zone is available only after these specified version
        /// Returns true if this version requested has preferred zone. 
        /// Able to enable and disable based on toggle isEnablePreferredZone
        /// Added by Kiran, 09/21/2018
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="appVersion"></param>
        /// <returns></returns>
        private bool EnablePreferredZone(int appId, string appVersion)
        {
            if (!string.IsNullOrEmpty(appVersion) && appId != -1)
            {
                return _configuration.GetValue<bool>("isEnablePreferredZone")
               && IsApplicationVersionGreater(appId, appVersion, "AndroidPreferredSeatVersion", "iOSPreferredSeatVersion", "", "", true);
            }

            return false;
        }

        private void ChangeDAAMapForUpperVersion(string appVersion, MOBSeatMapRequest request, MOBSeatMapResponse response)
        {
            bool replaceDAFRMtoDAFL = _configuration.GetValue<bool>("ReplaceDAFRMtoDAFR");

            if (replaceDAFRMtoDAFL)
            {
                string iOSVersionWithNewDAASeatMap = _configuration.GetValue<string>("iOSVersionWithNewDAASeatMap"); // 2.1.22I
                string andriodVersionWithNewDAASeatMap = _configuration.GetValue<string>("andriodVersionWithNewDAASeatMap"); // 2.1.22A 
                var versionWithNewDAASeatMap = iOSVersionWithNewDAASeatMap;
                if (request.Application.Id == 2)
                {
                    versionWithNewDAASeatMap = andriodVersionWithNewDAASeatMap;
                }

                bool returnNewDAASeatMap = IsVersion1Greater(appVersion, versionWithNewDAASeatMap, true);

                if (!returnNewDAASeatMap && response.SeatMap != null && response.SeatMap.Cabins != null)
                {
                    foreach (var seat in response.SeatMap.Cabins.SelectMany(c => c.Rows.SelectMany(r => r.Seats)).Where(s => !string.IsNullOrEmpty(s.Program) && s.Program.Trim().ToUpper() == "DAFRM"))
                    {
                        seat.Program = "DAFL";
                    }
                }
            }
        }

        private bool IsVersion1Greater(string version1, string version2, bool regexAppVersion)
        {
            Regex regex = new Regex("[0-9.]");
            version1 = string.Join("", regex.Matches(version1).Cast<Match>().Select(match => match.Value).ToArray());

            return SeperatedVersionCompareCommonCode(version1, version2);
        }

        /// <summary>
        /// Disable First,Business seats from SeatMap if First,Business products SeatsRemaining is -1 
        /// </summary>
        /// <param name="seatMap"></param>
        /// <param name="flightNumber"></param>
        /// <param name="fFlight"></param>
        private void ClearSeatsIfProductNotAvailableExceptEconomy(MOBSeatMap seatMap, string flightNumber, MOBSHOPFlattenedFlight fFlight)
        {
            if (_configuration.GetValue<bool>("ClearSeatsIfProductNotAvailableExceptEconomyToggle"))
            {
                List<MOBSHOPShoppingProduct> flightProducts = null;
                MOBSHOPFlight flight = null;

                if (fFlight != null && fFlight.Flights != null)
                {
                    flight = fFlight.Flights.FirstOrDefault(p => p != null && !string.IsNullOrEmpty(p.FlightNumber) && p.FlightNumber == flightNumber);

                    if (flight != null)
                    {
                        flightProducts = fFlight.Flights.Where(f => f != null && f.ShoppingProducts != null).SelectMany(f => f.ShoppingProducts.Where(sp => !string.IsNullOrEmpty(sp.ProductId))).ToList();

                        if (flightProducts != null)
                        {
                            flightProducts = flightProducts.GroupBy(p => p.ProductId).Select(g => g.First()).ToList();

                            List<MOBCabin> cabins = null;
                            bool isEconomyExist = (flight.Messages != null && flight.Messages.Count() > 0 && flight.Messages[0].MessageCode.ToUpper().Contains("ECONOMY"));
                            if (TwoCabinCheckValidation(flightProducts, seatMap.Cabins.Count, "FIRST", isEconomyExist))
                            {
                                cabins = seatMap.Cabins.Where(p => p.COS.ToUpper().Contains("FIRST")).ToList();
                            }
                            UnAssignCabinAvaialableSeats(cabins);

                            if (TwoCabinCheckValidation(flightProducts, seatMap.Cabins.Count, "BUS", isEconomyExist))
                            {
                                cabins = seatMap.Cabins.Where(p => p.COS.ToUpper().Contains("BUS")).ToList();
                            }
                            UnAssignCabinAvaialableSeats(cabins);
                        }
                    }
                }
            }
        }

        private void UnAssignCabinAvaialableSeats(List<MOBCabin> cabins)
        {
            if (cabins != null && cabins.Count > 0)
            {
                foreach (var cabin in cabins)
                {
                    foreach (var row in cabin.Rows)
                    {
                        foreach (var seat in row.Seats)
                        {
                            if (seat.seatvalue == "O")
                                seat.seatvalue = "X";
                        }
                    }
                }
            }
        }

        private bool TwoCabinCheckValidation(List<MOBSHOPShoppingProduct> flightProducts, int cabinCount, string validateCabin, bool isEconomyExist)
        {
            bool isOnlyEconomyAvaialble = false;
            var firstProd = flightProducts.Where(prod => prod.Cabin.ToUpper().Contains("FIRST")).ToList();
            var businessProd = flightProducts.Where(prod => prod.Cabin.ToUpper().Contains("BUS")).ToList();

            if (cabinCount == 2 &&  //This block is for cabin count is 2
                 (firstProd == null || (firstProd != null && !firstProd.Exists(prod => prod.SeatsRemaining > -1 && !(prod.IsMixedCabin && isEconomyExist))) &&
                 (businessProd == null || (businessProd != null && !businessProd.Exists(prod => prod.SeatsRemaining > -1 && !(prod.IsMixedCabin && isEconomyExist))))))
            {
                isOnlyEconomyAvaialble = true;
            }
            else if (cabinCount == 3 && validateCabin == "FIRST" && firstProd != null && !firstProd.Exists(prod => prod.SeatsRemaining > -1 && !(prod.IsMixedCabin && isEconomyExist))) // prod => prod.SeatsRemaining > -1  this condition is for if product tiles are avaialble more than 1, then checking all. This is happening in award search
            {                                                                                                                                                                           /// !(prod.IsMixedCabin && isEconomyExist) this is if flight messages has economy, which is showing at flight stripe and mised cabon true for this product, then also clearing
                isOnlyEconomyAvaialble = true;                                                                                                                                          /// because in mixed cabin, we are showing first or business cabin, even though this perticular flight is Economy
            }
            else if (cabinCount == 3 && validateCabin == "BUS" && businessProd != null && !businessProd.Exists(prod => prod.SeatsRemaining > -1 && !(prod.IsMixedCabin && isEconomyExist)))
            {
                isOnlyEconomyAvaialble = true;
            }

            return isOnlyEconomyAvaialble;
        }

        private bool EnableAirCanada(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableAirCanada")
                    && IsApplicationVersionGreater(appId, appVersion, "AndroidAirCanadaVersion", "iPhoneAirCanadaVersion", "", "", true);
        }

        private bool EnableLufthansaForHigherVersion(string operatingCarrierCode, int applicationId, string appVersion)
        {
            return EnableLufthansa(operatingCarrierCode) &&
                                    IsApplicationVersionGreater(applicationId, appVersion, "Android_EnableInterlineLHRedirectLinkManageRes_AppVersion", "iPhone_EnableInterlineLHRedirectLinkManageRes_AppVersion", "", "", true);

        }

        private bool EnableLufthansa(string operatingCarrierCode)
        {
            return _configuration.GetValue<bool>("EnableInterlineLHRedirectLinkManageRes")
                                    && _configuration.GetValue<string>("InterlineLHAndParternerCode").Contains(operatingCarrierCode?.ToUpper());
        }

        private bool OaSeatMapSupportedVersion(int applicationId, string appVersion)
        {
            return IsApplicationVersionGreater(applicationId, appVersion, "AndroidOaSeatMapVersion", "iPhoneOaSeatMapVersion", "", "", true);
        }

        private bool IsApplicationVersionGreater(int applicationID, string appVersion, string androidnontfaversion,
           string iphonenontfaversion, string windowsnontfaversion, string mWebNonELFVersion, bool ValidTFAVersion)
        {
            #region Priya Code for version check

            if (!string.IsNullOrEmpty(appVersion))
            {
                string AndroidNonTFAVersion = _configuration.GetValue<string>(androidnontfaversion) ?? "";
                string iPhoneNonTFAVersion = _configuration.GetValue<string>(iphonenontfaversion) ?? "";
                string WindowsNonTFAVersion = _configuration.GetValue<string>(windowsnontfaversion) ?? "";
                string MWebNonTFAVersion = _configuration.GetValue<string>(mWebNonELFVersion) ?? "";

                Regex regex = new Regex("[0-9.]");
                appVersion = string.Join("",
                    regex.Matches(appVersion).Cast<Match>().Select(match => match.Value).ToArray());

                if (applicationID == 2 && appVersion != AndroidNonTFAVersion)
                {
                    ValidTFAVersion = IsVersion1Greater(appVersion, AndroidNonTFAVersion);
                }
            }
            #endregion

            return ValidTFAVersion;
        }

        private bool IsVersion1Greater(string version1, string version2)
        {
            return SeperatedVersionCompareCommonCode(version1, version2);
        }

        private bool SeperatedVersionCompareCommonCode(string version1, string version2)
        {
            #region
            string[] version1Arr = version1.Trim().Split('.');
            string[] version2Arr = version2.Trim().Split('.');

            if (Convert.ToInt32(version1Arr[0]) > Convert.ToInt32(version2Arr[0]))
            {
                return true;
            }
            else if (Convert.ToInt32(version1Arr[0]) == Convert.ToInt32(version2Arr[0]))
            {
                if (Convert.ToInt32(version1Arr[1]) > Convert.ToInt32(version2Arr[1]))
                {
                    return true;
                }
                else if (Convert.ToInt32(version1Arr[1]) == Convert.ToInt32(version2Arr[1]))
                {
                    if (Convert.ToInt32(version1Arr[2]) > Convert.ToInt32(version2Arr[2]))
                    {
                        return true;
                    }
                    else if (Convert.ToInt32(version1Arr[2]) == Convert.ToInt32(version2Arr[2]))
                    {
                        if (!string.IsNullOrEmpty(version1Arr[3]) && !string.IsNullOrEmpty(version2Arr[3]))
                        {
                            if (Convert.ToInt32(version1Arr[3]) > Convert.ToInt32(version2Arr[3]))
                            {
                                return true;
                            }
                        }

                    }
                }
            }
            #endregion
            return false;
        }

        private bool IsSeatMapSupportedOa(string operatingCarrier, string MarketingCarrier)
        {
            if (string.IsNullOrEmpty(operatingCarrier)) return false;
            var seatMapSupportedOa = _configuration.GetValue<string>("SeatMapSupportedOtherAirlines");
            if (string.IsNullOrEmpty(seatMapSupportedOa)) return false;

            var seatMapEnabledOa = seatMapSupportedOa.Split(',');

            if (seatMapEnabledOa.Any(s => s == operatingCarrier.ToUpper().Trim()))
                return true;
            else if (_configuration.GetValue<string>("SeatMapSupportedOtherAirlinesMarketedBy") != null)
            {
                return _configuration.GetValue<string>("SeatMapSupportedOtherAirlinesMarketedBy").Split(',').ToList().Any(m => m == MarketingCarrier + "-" + operatingCarrier);
            }

            return false;
        }
    }
}
