using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using United.Common.Helper;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Shopping;
using United.Common.HelperSeatEngine;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Misc;
using United.Services.FlightShopping.Common.Extensions;
using United.Utility.Helper;
using MOBSeatMap = United.Mobile.Model.Shopping.MOBSeatMap;
using MOBSHOPReservation = United.Mobile.Model.Shopping.MOBSHOPReservation;
using MOBSHOPTrip = United.Mobile.Model.Shopping.MOBSHOPTrip;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.Services.ShopSeats.Domain
{
    public class ShopSeatsBusiness : IShopSeatsBusiness
    {
        private readonly ICacheLog<ShopSeatsBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly ISeatEngine _seatEngine;
        private readonly ISeatMapCSL30 _seatMapCSL30;
        private readonly IMerchandizingServices _merchandizingServices;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ICSLStatisticsService _cSLStatisticsService;
        private readonly IFeatureToggles _featureToggles;

        public ShopSeatsBusiness(ICacheLog<ShopSeatsBusiness> logger
            , IConfiguration configuration
            , IHeaders headers
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility
            , ISeatEngine seatEngine
            , ISeatMapCSL30 seatMapCSL30
            , IMerchandizingServices merchandizingServices
            , IDynamoDBService dynamoDBService
            , ICSLStatisticsService cSLStatisticsService
            , IFeatureToggles featureToggles)
        {
            _logger = logger;
            _configuration = configuration;
            _headers = headers;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
            _seatEngine = seatEngine;
            _seatMapCSL30 = seatMapCSL30;
            _merchandizingServices = merchandizingServices;
            _dynamoDBService = dynamoDBService;
            _cSLStatisticsService = cSLStatisticsService;
            _featureToggles = featureToggles;
        }
        ///BUG 215322 : Eplus Exception handling: Incorrect error message pop-up is displayed, when getProduct call fails
        public async Task<bool> ValidateEPlusVersion(int applicationID, string appVersion)
        {
            return await Task.FromResult(GeneralHelper.IsApplicationVersionGreater(applicationID, appVersion, "AndroidEPlusVersion", "iPhoneEPlusVersion", "", "", true, _configuration));
        }

        public async Task<SelectSeatsResponse> SelectSeats(SelectSeatsRequest selectSeatsRequest)
        {
            SelectSeatsResponse response = new SelectSeatsResponse();
            await ThrowExceptionDependsOnDepartuReDate(selectSeatsRequest.SessionId, "EPlusSelectSeatsErrorDate");
            if (!string.IsNullOrEmpty(selectSeatsRequest.SeatAssignment))
            {
                selectSeatsRequest.SeatAssignment = await ValidateSeatPrice(selectSeatsRequest.SeatAssignment, selectSeatsRequest.SessionId, selectSeatsRequest.Origin, selectSeatsRequest.Destination);
            }

            if (selectSeatsRequest.SeatAssignment == null)
                selectSeatsRequest.SeatAssignment = "";
            string iOSVersionWithNewSeatMapLegend = _configuration.GetValue<string>("iOSAppVersionWithNewSeatMapLegendForPolaris"); // 2.1.13I
            string andriodVersionWithNewSeatMapLegend = _configuration.GetValue<string>("AndriodAppVersionWithNewSeatMapLegendForPolaris"); // 2.1.12A 
            string mWebVersionWithNewSeatMapLegend = _configuration.GetValue<string>("MWebWithNewSeatMapLegendForPolaris"); // 2.0.0 
            var versionWithNewSeatMapLegend = iOSVersionWithNewSeatMapLegend;
            if (selectSeatsRequest.Application.Id == 2)
            {
                versionWithNewSeatMapLegend = andriodVersionWithNewSeatMapLegend;
            }
            else if (selectSeatsRequest.Application.Id == 16)
            {
                versionWithNewSeatMapLegend = mWebVersionWithNewSeatMapLegend;
            }
            bool returnPolarisLegendforSeatMap = GeneralHelper.IsVersion1Greater(selectSeatsRequest.Application.Version.Major, versionWithNewSeatMapLegend, true);
            response = await SelectSeats(selectSeatsRequest, returnPolarisLegendforSeatMap);

            #region check version For DAA
            ChangeDAAMapForUpperVersion(selectSeatsRequest, response);
            #endregion
            response.FlightTravelerSeatsRequest = selectSeatsRequest;
            response.LanguageCode = selectSeatsRequest.LanguageCode;
            response.TransactionId = selectSeatsRequest.TransactionId;
            response.SessionId = selectSeatsRequest.SessionId;
            response.ExitAdvisory = GetExitAdvisory();
            response.CartId = selectSeatsRequest.CartId;
            response.Flow = selectSeatsRequest.Flow;

            var selectSeats = new SelectSeats();
            try
            {
                selectSeats = await _sessionHelperService.GetSession<SelectSeats>(selectSeatsRequest.SessionId, selectSeats.ObjectName, new List<string> { selectSeatsRequest.SessionId, selectSeats.ObjectName });
            }
            catch (System.Exception ex) { }

            if (selectSeats == null)
            {
                selectSeats = new SelectSeats();
            }

            if (selectSeats.Requests == null || selectSeats.Responses == null)
            {
                selectSeats.Requests = new SerializableDictionary<string, SelectSeatsRequest>();
                selectSeats.Responses = new SerializableDictionary<string, SelectSeatsResponse>();
            }

            if (selectSeats.Requests.ContainsKey(selectSeatsRequest.NextOrigin + selectSeatsRequest.NextDestination))
            {
                selectSeats.Requests[selectSeatsRequest.NextOrigin + selectSeatsRequest.NextDestination] = selectSeatsRequest;
                selectSeats.Responses[selectSeatsRequest.NextOrigin + selectSeatsRequest.NextDestination] = response;
            }
            else
            {
                selectSeats.Requests.Add(selectSeatsRequest.NextOrigin + selectSeatsRequest.NextDestination, selectSeatsRequest);
                selectSeats.Responses.Add(selectSeatsRequest.NextOrigin + selectSeatsRequest.NextDestination, response);
            }

            await _sessionHelperService.SaveSession<SelectSeats>(selectSeats, selectSeatsRequest.SessionId, new List<string> { selectSeatsRequest.SessionId, selectSeats.ObjectName }, selectSeats.ObjectName);
            if (_configuration.GetValue<string>("Log_CSL_Call_Statistics") != null && _configuration.GetValue<string>("Log_CSL_Call_Statistics").ToString().ToUpper().Trim() == "TRUE")
            {
                try
                {
                    CSLStatistics cslStatistics = new CSLStatistics(_logger, _configuration, _dynamoDBService, _cSLStatisticsService);
                    string callDurations = response.CallDuration.ToString();
                    await cslStatistics.AddCSLCallStatisticsDetails(string.Empty, "REST_Shopping", string.Empty, callDurations, "Shopping/SelectSeats", selectSeatsRequest.SessionId);
                }
                catch { }
            }


            return response;
        }

        private List<MOBTypeOption> GetExitAdvisory()
        {
            List<MOBTypeOption> exitAdvisory = new List<MOBTypeOption>();

            exitAdvisory.Add(new MOBTypeOption("1", "Be at least 15 years of age and able to perform all of the functions listed below without assistance."));
            exitAdvisory.Add(new MOBTypeOption("2", "Not be traveling with a customer who requires special care, such as a small child, that would prevent you from performing all of the required functions listed."));
            exitAdvisory.Add(new MOBTypeOption("3", "Read and speak well enough to understand the instructions, provided by United in printed or graphic form, for opening exits and other emergency procedures and give information during an emergency."));
            exitAdvisory.Add(new MOBTypeOption("4", "See well enough to perform the emergency exit functions. Persons may wear glasses or contact lenses"));
            exitAdvisory.Add(new MOBTypeOption("5", "Understand English well enough and hear well enough to understand crewmembers commands. Persons may wear a hearing aid."));
            exitAdvisory.Add(new MOBTypeOption("6", "Be able to reach the emergency exit quickly and assist customers off an escape slide."));
            exitAdvisory.Add(new MOBTypeOption("7", "Be able to use both hands, both arms and both legs, as well as maintain balance and be strong and flexible enough to operate the exit and any slide mechanism; open the exit and go quickly through it; stabilize the escape slide; assist others in getting off an escape slide; and clear the exit row of obstructions including window hatches as required. Some window hatches that must be lifted can weigh as much as 58 lbs. (26 kgs)."));
            exitAdvisory.Add(new MOBTypeOption("8", "By selecting Accept, I understand and acknowledge the terms and conditions above."));

            return exitAdvisory;
        }

        private async Task ThrowExceptionDependsOnDepartuReDate(string sessionId, string v)
        {
            ///Bug 215283 : Eplus:mApp:Scenario2: incorrect "Load Seats Exception "content is displayed
            ///BUG 215322 : Eplus Exception handling: Incorrect error message pop-up is displayed, when getProduct call fails
            ///Srini
            var reservation =await _sessionHelperService.GetSession<Reservation>(sessionId, (new Reservation()).ObjectName, new List<string>() { sessionId, new Reservation().ObjectName }).ConfigureAwait(false);
            if (_configuration.GetValue<bool>("BugFixToggleFor17M") && reservation != null && !string.IsNullOrEmpty(_configuration.GetValue<string>(v)))
            {
                foreach (var route in _configuration.GetValue<string>(v).Split(';'))
                {
                    string[] strCollection = route.Split('|');
                    if (reservation.Trips[0].Origin == strCollection[0] &&
                       reservation.Trips[0].Destination == strCollection[1] &&
                       reservation.Trips[0].DepartDate == strCollection[2])
                        throw new NotImplementedException();
                }
            }
        }

        private async Task<string> ValidateSeatPrice(string seatAssignment, string sessionId, string origin, string destination)
        {
            string validAssignnment = string.Empty;
            bool isLoop = true;
            if (!string.IsNullOrEmpty(seatAssignment) && seatAssignment.Trim(',').Trim() != "")
            {
                try
                {
                    var persistSelectSeatsResponse =await _sessionHelperService.GetSession<SelectSeats>(sessionId, new SelectSeats().ObjectName, new List<string> { sessionId, new SelectSeats().ObjectName }).ConfigureAwait(false);

                    string[] multipleSeats = seatAssignment.Split(',');

                    for (int i = 0; i < multipleSeats.Count(); i++)
                    {
                        isLoop = true;
                        string[] SeatValues = multipleSeats[i].Split('|');

                        if (SeatValues[0] != null && SeatValues[0].ToUpper().Trim() != "")
                        {
                            #region
                            if (persistSelectSeatsResponse != null)
                            {
                                SelectSeatsResponse shopSelectSeatsResponse = persistSelectSeatsResponse.Responses[origin + destination];

                                foreach (Cabin cabin in shopSelectSeatsResponse.SeatMap[0].Cabins)
                                {
                                    foreach (Row mobRows in cabin.Rows)
                                    {
                                        foreach (SeatB eachSeat in mobRows.Seats)
                                        {
                                            if (eachSeat.ServicesAndFees?.Count > 0 && eachSeat.ServicesAndFees[0]?.SeatNumber?.ToUpper() == SeatValues[0]?.ToUpper())
                                            {
                                                if (eachSeat.ServicesAndFees[0]?.TotalFee != Convert.ToDecimal(SeatValues[1]))
                                                {
                                                    multipleSeats[i] = SeatValues[0] + "|" + eachSeat.ServicesAndFees[0].TotalFee + "|" + SeatValues[2] + "|" + eachSeat.ServicesAndFees[0].Program?.ToUpper() + "|" + eachSeat.ServicesAndFees[0].SeatFeature?.ToUpper() + "|" + SeatValues[5];
                                                }
                                                isLoop = false;
                                                break;
                                            }
                                        }
                                        if (!isLoop)
                                        {
                                            break;
                                        }
                                    }
                                    if (!isLoop)
                                    {
                                        break;
                                    }
                                }
                            }
                            #endregion
                        }
                        validAssignnment = validAssignnment + multipleSeats[i] + ",";
                    }
                }

                catch (Exception ex)
                {
                    validAssignnment = string.Empty;
                }
            }
            else
            {
                // Scenario for SelectSeatsCall first time for multi pax if  seatAssignment = ',' we need to return the same ',' string back with out Trim(',')
                return seatAssignment;
            }
            //return validAssignnment.Substring(0, validAssignnment.Length - 1);
            return validAssignnment.Trim(',');
        }

        private void ChangeDAAMapForUpperVersion(SelectSeatsRequest request, SelectSeatsResponse response)
        {
            try
            {
                bool replaceDAFRMtoDAFL = (_configuration.GetValue<bool>("ReplaceDAFRMtoDAFR"));
                if (replaceDAFRMtoDAFL)
                {
                    string iOSVersionWithNewDAASeatMap = _configuration.GetValue<string>("iOSVersionWithNewDAASeatMap"); // 2.1.22I
                    string andriodVersionWithNewDAASeatMap = _configuration.GetValue<string>("andriodVersionWithNewDAASeatMap"); // 2.1.22A 
                    var versionWithNewDAASeatMap = iOSVersionWithNewDAASeatMap;
                    if (request.Application.Id == 2)
                    {
                        versionWithNewDAASeatMap = andriodVersionWithNewDAASeatMap;
                    }
                    bool returnNewDAASeatMap = GeneralHelper.IsVersion1Greater(request.Application.Version.Major, versionWithNewDAASeatMap, true);
                    if (!returnNewDAASeatMap && response.SeatMap != null)
                    {
                        foreach (var mobSeatB in response.SeatMap.SelectMany(sm => sm.Cabins.SelectMany(c => c.Rows.SelectMany(r => r.Seats.Where(s => !string.IsNullOrEmpty(s.Program) && s.Program.Trim().ToUpper() == "DAFRM")))))
                        {
                            mobSeatB.Program = "DAFL";
                        }
                    }
                }
            }
            catch
            {
                // trying to return exsisting seat map icon DAFR for old clints, and DAFRM for new clients, to have better customer ecperience. 
            }
        }

        private async Task<SelectSeatsResponse> SelectSeats(SelectSeatsRequest request, bool returnPolarisLegendforSeatMap = false)
        {
            SelectSeatsResponse response = new SelectSeatsResponse();
            var persistShoppingCart = new MOBShoppingCart();
            persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, persistShoppingCart.ObjectName, new List<string> { request.SessionId, persistShoppingCart.ObjectName }).ConfigureAwait(false);
            if (_configuration.GetValue<bool>("EnableCouponsforBooking"))
            {
                if (_shoppingUtility.EnableAdvanceSearchCouponBooking(request.Application.Id, request?.Application?.Version?.Major))
                {
                    if (persistShoppingCart?.PromoCodeDetails?.PromoCodes != null
                                                                 && persistShoppingCart.PromoCodeDetails.PromoCodes.Count > 0)
                    {
                        Reservation persistedReservation2 =await _sessionHelperService.GetSession<Reservation>(request.SessionId, (new Reservation()).ObjectName, new List<string> { request.SessionId, (new Reservation()).ObjectName }).ConfigureAwait(false);
                        if (persistedReservation2.ShopReservationInfo2.NextViewName == "RTI")
                        {
                            persistedReservation2.ShopReservationInfo2.IsSelectSeatsFromRTI = true;
                        }
                        await _sessionHelperService.SaveSession<Reservation>(persistedReservation2, request.SessionId, new List<string> { request.SessionId, (new Reservation()).ObjectName }, (new Reservation()).ObjectName);
                        if (persistShoppingCart?.Products != null && persistShoppingCart.Products.Any(p => p.CouponDetails != null && p.CouponDetails.Count > 0))
                        {
                            response.PromoCodeRemovalAlertMessage = _shoppingUtility.EnableAdvanceSearchCouponBooking(request.Application.Id, request?.Application?.Version?.Major) && (persistedReservation2?.ShopReservationInfo2?.IsSelectSeatsFromRTI == true) ? GetPromoCodeRemovalMessage() : null;
                        }
                    }
                }
                else
                {
                    if (persistShoppingCart?.Products != null && persistShoppingCart.Products.Any(p => p.CouponDetails != null && p.CouponDetails.Count > 0))
                    {
                        response.PromoCodeRemovalAlertMessage = GetPromoCodeRemovalMessage();
                    }
                }
            }
            response.Seats = await FlightTravelerSeats(request.SessionId, request.SeatAssignment, request.Origin, request.Destination, request.PaxIndex, request.NextOrigin, request.NextDestination);
            Reservation persistedReservation =await _sessionHelperService.GetSession<Reservation>(request.SessionId, new Reservation().ObjectName, new List<string> { request.SessionId, new Reservation().ObjectName }).ConfigureAwait(false);
            int ePlusSubscriberCount = 0;
            if (persistedReservation.ShopReservationInfo2 != null)
            {
                if (!persistedReservation.IsReshopChange)
                {
                    persistedReservation.ShopReservationInfo2.NextViewName = "TravelOption";
                }
                else
                {
                    persistedReservation.ShopReservationInfo2.NextViewName = "RTI";
                }
                await _sessionHelperService.SaveSession<Reservation>(persistedReservation, request.SessionId, new List<string> { request.SessionId, persistedReservation.ObjectName }, persistedReservation.ObjectName);
            }
            if (!(persistedReservation.IsELF || _shoppingUtility.IsIBE(persistedReservation)))
            {
                if (string.IsNullOrEmpty(request.SeatAssignment.Split(',')[0]) && request.Origin == request.NextOrigin && request.Destination == request.NextDestination)
                {
                    if (HasEconomySegment(persistedReservation.Trips))
                    {
                        var tupleResponse = await PopulateEPlusSubscriberAndMPMemeberSeatMessage( persistedReservation, request.Application.Id, request.SessionId);
                        persistedReservation = tupleResponse.Item1;
                        ePlusSubscriberCount = tupleResponse.ePlusSubscriberCount;
                    }
                }

                if (ShowEPAMessage(persistedReservation.Trips, request.NextOrigin, request.NextDestination))
                {
                    response.EPAMessageTitle = persistedReservation.EPAMessageTitle;
                    response.EPAMessage = persistedReservation.EPAMessage;
                }
            }
            Session session = new Session();
            //session = await _sessionHelperService.GetSession<Session>
            //    (request.SessionId, new Session().ObjectName, new List<string> { request.SessionId, new Session().ObjectName }).ConfigureAwait(false);
            if (await _featureToggles.IsEnableVerticalSeatMapInBooking(request.Application.Id, request.Application.Version.Major).ConfigureAwait(false))
            {
                response.IsVerticalSeatMapEnabled = true;
            }
            var isEplusNA = false;
            if (!string.IsNullOrEmpty(request.NextDestination) || !string.IsNullOrEmpty(request.NextOrigin))
            {

                if (_configuration.GetValue<bool>("EnableCSL30BookingReshopSelectSeatMap"))
                {
                    var tupleResponse = await _seatMapCSL30.GetCSL30SeatMapDetail
                        (request.Flow, request.SessionId, request.NextDestination, request.NextOrigin, request.Application.Id, request.Application.Version.Major, request.DeviceId, returnPolarisLegendforSeatMap, response.PromoCodeRemovalAlertMessage, persistShoppingCart?.PromoCodeDetails, response.IsVerticalSeatMapEnabled, ePlusSubscriberCount);

                    response.SeatMap = tupleResponse.Item1;
                    isEplusNA = tupleResponse.isEplusNA;

                }
                else
                {
                    response.SeatMap = await _seatEngine.GetSeatMapDetail(request.SessionId, request.NextDestination, request.NextOrigin, request.Application.Id, request.Application.Version.Major, request.DeviceId, returnPolarisLegendforSeatMap);
                }
                foreach (MOBSeatMap seatMap in response.SeatMap)
                {
                    #region //**// LMX Flag For AppID change
                    session = await _sessionHelperService.GetSession<Session>(request.SessionId, new Session().ObjectName, new List<string> { request.SessionId, new Session().ObjectName }).ConfigureAwait(false);
                    seatMap.SuppressLMX = session.SupressLMXForAppID;
                    #endregion
                }
            }
            if (_configuration.GetValue<bool>("EnableCouponsforBooking"))
            {
                if (response.PromoCodeRemovalAlertMessage != null && response.SeatMap != null && response.SeatMap.Any() && !string.IsNullOrEmpty(response.SeatMap.FirstOrDefault().ShowInfoMessageOnSeatMap))
                {
                    response.SeatMap.FirstOrDefault().ShowInfoMessageOnSeatMap = string.Empty;
                }
            }
            if (_shoppingUtility.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, persistedReservation?.ShopReservationInfo2?.IsDisplayCart == true))
            {
                if (!request.IsBackNavigationFromRTI)
                {
                    response.ClearOption = CartClearOption.ClearSeats.ToString().ToUpper();
                }
                else
                {
                    response.ClearOption = String.Empty;
                }
            }
            if (_shoppingUtility.EnableEPlusAncillary(request.Application.Id, request.Application.Version.Major, persistedReservation.IsReshopChange) && isEplusNA)
            {
                if (_configuration.GetValue<bool>("EnableEplusCodeRefactor"))
                {
                    var messages = new List<MOBItem>();
                    try
                    {
                        messages = await GetMessagesFromDb("EPlusSeatsNAMsg");
                    }
                    catch { }

                    MOBSection alertMsg = new MOBSection() { };
                    if (messages != null && messages.Count > 0)
                    {
                        alertMsg = AssignAlertMessage(messages);
                        var ePlusNAMsg = new MOBFSRAlertMessage()
                        {
                            HeaderMessage = alertMsg.Text1,
                            BodyMessage = alertMsg.Text2,
                            AlertType = MOBFSRAlertMessageType.Information.ToString(),
                            MessageTypeDescription = FSRAlertMessageType.NoSeatsAvailable
                        };
                        response.SeatmapMessaging = new List<MOBFSRAlertMessage>();
                        response.SeatmapMessaging.Add(ePlusNAMsg);
                    }
                    else
                    {
                        string headerMsg = _configuration.GetValue<string>("EPlusSeatsNAHeaderMsg");
                        string bodyMsg = _configuration.GetValue<string>("EPlusSeatsNABodyMsg");
                        var ePlusNAMsg = new MOBFSRAlertMessage()
                        {
                            HeaderMessage = headerMsg,
                            BodyMessage = bodyMsg,
                            AlertType = MOBFSRAlertMessageType.Information.ToString(),
                            MessageTypeDescription = FSRAlertMessageType.NoSeatsAvailable
                        };
                        response.SeatmapMessaging = new List<MOBFSRAlertMessage>();
                        response.SeatmapMessaging.Add(ePlusNAMsg);
                    }
                }
                else
                {
                    string headerMsg = _configuration.GetValue<string>("EPlusSeatsNAHeaderMsg");
                    string bodyMsg = _configuration.GetValue<string>("EPlusSeatsNABodyMsg");
                    var ePlusNAMsg = new MOBFSRAlertMessage()
                    {
                        HeaderMessage = headerMsg,
                        BodyMessage = bodyMsg,
                        AlertType = MOBFSRAlertMessageType.Information.ToString(),
                        MessageTypeDescription = FSRAlertMessageType.NoSeatsAvailable
                    };
                    response.SeatmapMessaging = new List<MOBFSRAlertMessage>();
                    response.SeatmapMessaging.Add(ePlusNAMsg);
                }
            }
            return response;
        }

        private async Task<List<MOBSeat>> FlightTravelerSeats(string sessionID, string seatAssignment,
      string origin, string destination, string paxIndex, string nextOrigin, string nextDestination)
        {
            var lstSeats = new List<MOBSeat>();
            List<MOBSeat> allSeats = new List<MOBSeat>();

            Reservation persistedReservation = await _sessionHelperService.GetSession<Reservation>(sessionID, new Reservation().ObjectName, new List<string> { sessionID, new Reservation().ObjectName }).ConfigureAwait(false);

            string[] arrSeatAssignments = seatAssignment.Split(',');
            string[] arrPax = paxIndex.Split(',');


            if (persistedReservation != null && persistedReservation.TravelersCSL != null)
            {
                for (int i = 0; i < arrPax.Length; i++)
                {
                    if (persistedReservation.IsReshopChange)
                    {
                        ResetPopulatedSeats(persistedReservation.TravelersCSL[arrPax[i]].Seats, origin, destination);
                    }

                    lstSeats = persistedReservation.TravelersCSL[arrPax[i]].Seats;
                    if (lstSeats != null && lstSeats.Count > 0)
                    {
                        IEnumerable<MOBSeat> seat = from s in lstSeats
                                                    where s.Origin == origin.ToUpper().Trim()
                                                    && s.Destination == destination.ToUpper().Trim()
                                                    && s.TravelerSharesIndex == arrPax[i]
                                                    select s;
                        if (seat != null && seat.Count() > 0)
                        {
                            var _seat = new List<MOBSeat>();
                            _seat = seat.ToList();
                            if (_seat.Count() == 1)
                            {
                                var tmpSeat = new MOBSeat();

                                tmpSeat.Destination = destination;
                                tmpSeat.Origin = origin;
                                tmpSeat.TravelerSharesIndex = _seat[0].TravelerSharesIndex;
                                tmpSeat.Key = _seat[0].Key;

                                string[] assignments = arrSeatAssignments[i].Split('|');

                                if (assignments.Length == 6)
                                {
                                    //Added as part of the changes for the exception 284001:Select seatsFormatException
                                    if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis"))
                                    {
                                        decimal price;
                                        tmpSeat.SeatAssignment = assignments[0];
                                        price = string.IsNullOrEmpty(assignments[1]) ? 0 : Convert.ToDecimal(assignments[1]);
                                        tmpSeat.Price = price;
                                        tmpSeat.PriceAfterTravelerCompanionRules = price;
                                        tmpSeat.Currency = assignments[2];
                                        tmpSeat.ProgramCode = assignments[3];
                                        tmpSeat.SeatType = assignments[4];
                                        tmpSeat.LimitedRecline = string.IsNullOrEmpty(assignments[5]) ? false : Convert.ToBoolean(assignments[5]);
                                        if (_configuration.GetValue<bool>("EnableMilesAsPayment"))
                                        {
                                            tmpSeat.OldSeatMiles = _configuration.GetValue<int>("milesFOP");
                                            tmpSeat.DisplayOldSeatMiles = ShopStaticUtility.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false);
                                        }

                                    }
                                    else
                                    {
                                        tmpSeat.SeatAssignment = assignments[0];
                                        tmpSeat.Price = Convert.ToDecimal(assignments[1]);
                                        tmpSeat.PriceAfterTravelerCompanionRules = Convert.ToDecimal(assignments[1]);
                                        tmpSeat.Currency = assignments[2];
                                        tmpSeat.ProgramCode = assignments[3];
                                        tmpSeat.SeatType = assignments[4];
                                        tmpSeat.LimitedRecline = Convert.ToBoolean(assignments[5]);
                                        if (_configuration.GetValue<bool>("EnableMilesAsPayment"))
                                        {
                                            tmpSeat.OldSeatMiles = _configuration.GetValue<int>("milesFOP");
                                            tmpSeat.DisplayOldSeatMiles = ShopStaticUtility.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false);
                                        }
                                    }
                                }
                                else
                                {
                                    ///Bug fix for numbers 77203,203206 by Ranjit
                                    ///If SeatAssignment is null from the request, SeatAssignment value saving as empty so issue reproducible.
                                    ///Now at this time Seat details are fetching from Persist file and assigning to SeatAssignment in tmpSeat.
                                    if (string.IsNullOrEmpty(arrSeatAssignments[i]))
                                    {
                                        tmpSeat.Price = _seat[0].Price;
                                        tmpSeat.PriceAfterTravelerCompanionRules = _seat[0].PriceAfterTravelerCompanionRules;
                                        tmpSeat.Currency = _seat[0].Currency;
                                        tmpSeat.ProgramCode = _seat[0].ProgramCode;
                                        tmpSeat.SeatType = _seat[0].SeatType;
                                        tmpSeat.LimitedRecline = _seat[0].LimitedRecline;
                                        tmpSeat.SeatAssignment = _seat[0].SeatAssignment;
                                        if (_configuration.GetValue<bool>("EnableMilesAsPayment"))
                                        {
                                            tmpSeat.OldSeatMiles = _configuration.GetValue<int>("milesFOP");
                                            tmpSeat.DisplayOldSeatMiles = ShopStaticUtility.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false);
                                        }
                                    }
                                    else
                                    {
                                        tmpSeat.SeatAssignment = arrSeatAssignments[i];
                                    }
                                }

                                lstSeats[_seat[0].Key] = tmpSeat;
                            }
                        }
                        else
                        {
                            if(lstSeats == null)
                            {
                                lstSeats = new List<MOBSeat>();
                            }
                            var tmpSeat = new MOBSeat();
                            tmpSeat.Destination = destination;
                            tmpSeat.Origin = origin;

                            string[] assignments = arrSeatAssignments[i].Split('|');
                            if (assignments.Length == 6)
                            {
                                tmpSeat.SeatAssignment = assignments[0];
                                tmpSeat.Price = Convert.ToDecimal(assignments[1]);
                                tmpSeat.PriceAfterTravelerCompanionRules = Convert.ToDecimal(assignments[1]);
                                tmpSeat.Currency = assignments[2];
                                tmpSeat.ProgramCode = assignments[3];
                                tmpSeat.SeatType = assignments[4];
                                tmpSeat.LimitedRecline = Convert.ToBoolean(assignments[5]);
                                if (_configuration.GetValue<bool>("EnableMilesAsPayment"))
                                {
                                    tmpSeat.OldSeatMiles = _configuration.GetValue<int>("milesFOP");
                                    tmpSeat.DisplayOldSeatMiles = ShopStaticUtility.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false);
                                }
                            }
                            else
                            {
                                //if (arrSeatAssignments[i] != null && arrSeatAssignments[i] != string.Empty)
                                //{
                                tmpSeat.SeatAssignment = arrSeatAssignments[i];
                                //}
                            }

                            tmpSeat.TravelerSharesIndex = arrPax[i];
                            tmpSeat.Key =  lstSeats.Count;
                            lstSeats.Add(tmpSeat);
                        }

                    }
                    else
                    {
                        var tmpSeat = new MOBSeat();
                        tmpSeat.Destination = destination;
                        tmpSeat.Origin = origin;

                        string[] assignments = arrSeatAssignments[i].Split('|');
                        if (assignments.Length == 6)
                        {
                            //Added as part of the changes for the exception 284001:Select seatsFormatException
                            if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis"))
                            {
                                decimal price;
                                price = string.IsNullOrEmpty(assignments[1]) ? 0 : Convert.ToDecimal(assignments[1]);
                                tmpSeat.SeatAssignment = assignments[0];
                                tmpSeat.Price = price;
                                tmpSeat.PriceAfterTravelerCompanionRules = price;
                                tmpSeat.Currency = assignments[2];
                                tmpSeat.ProgramCode = assignments[3];
                                tmpSeat.SeatType = assignments[4];
                                tmpSeat.LimitedRecline = string.IsNullOrEmpty(assignments[5]) ? false : Convert.ToBoolean(assignments[5]);
                                if (_configuration.GetValue<bool>("EnableMilesAsPayment"))
                                {
                                    tmpSeat.OldSeatMiles = Convert.ToInt32(_configuration.GetValue<string>("milesFOP"));
                                    tmpSeat.DisplayOldSeatMiles = ShopStaticUtility.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false);
                                }
                            }
                            else
                            {
                                tmpSeat.SeatAssignment = assignments[0];
                                tmpSeat.Price = Convert.ToDecimal(assignments[1]);
                                tmpSeat.PriceAfterTravelerCompanionRules = Convert.ToDecimal(assignments[1]);
                                tmpSeat.Currency = assignments[2];
                                tmpSeat.ProgramCode = assignments[3];
                                tmpSeat.SeatType = assignments[4];
                                tmpSeat.LimitedRecline = Convert.ToBoolean(assignments[5]);
                                if (_configuration.GetValue<bool>("EnableMilesAsPayment"))
                                {
                                    tmpSeat.OldSeatMiles = Convert.ToInt32(_configuration.GetValue<string>("milesFOP"));
                                    tmpSeat.DisplayOldSeatMiles = ShopStaticUtility.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false);
                                }
                            }
                        }
                        else
                        {
                            tmpSeat.SeatAssignment = arrSeatAssignments[i];
                        }
                        tmpSeat.TravelerSharesIndex = arrPax[i];
                        if (lstSeats == null)
                        {
                            lstSeats = new List<MOBSeat>();
                        }
                        tmpSeat.Key = lstSeats.Count;
                        lstSeats.Add(tmpSeat);
                    }
                    lstSeats = lstSeats?.OrderBy(s => s.Key)?.ToList();

                    AssignSeatUAOperatedFlag(lstSeats, allSeats, persistedReservation);

                    persistedReservation.TravelersCSL[arrPax[i]].Seats = lstSeats;
                }
            }



           await _sessionHelperService.SaveSession<Reservation>(persistedReservation, sessionID, new List<string> { sessionID, persistedReservation.ObjectName }, persistedReservation.ObjectName).ConfigureAwait(false);


            IEnumerable<MOBSeat> retSeat = from s in lstSeats
                                           where s.Origin == nextOrigin.ToUpper().Trim()
                                           && s.Destination == nextDestination.ToUpper().Trim()
                                           select s;
            if (retSeat.Count() > 0)
            {
                lstSeats = retSeat.ToList();
            }
            else if (!string.IsNullOrEmpty(nextDestination))
                allSeats = null;

            return allSeats;

        }

        private bool HasEconomySegment(List<MOBSHOPTrip> trips)
        {
            bool result = false;

            if (trips != null)
            {
                foreach (var trip in trips)
                {
                    if (trip.FlattenedFlights != null)
                    {
                        foreach (var ff in trip.FlattenedFlights)
                        {
                            if (ff.Flights != null)
                            {
                                foreach (var flight in ff.Flights)
                                {
                                    if (!string.IsNullOrEmpty(flight.ServiceClassDescription) &&
                                        (flight.ServiceClassDescription.ToUpper().Trim().Equals("ECONOMY") ||
                                         flight.ServiceClassDescription.ToUpper().Trim().Equals("UNITED ECONOMY") ||
                                         flight.ServiceClassDescription.ToUpper().Trim().Equals("COACH")
                                        ))
                                    {
                                        result = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private async Task<(Reservation, int ePlusSubscriberCount)> PopulateEPlusSubscriberAndMPMemeberSeatMessage( Reservation persistedReservation, int applicationID, string sessionID)
        {
            // Booking and View Res only Care about Gold Level Status for Free EPlus Seats because silver will get only on Check In path.
            int ePlusSubscriberCount = 0;
            int goldMemberCount = 0;
            int silverMemberCount = 0;
            int platinumMemberCount = 0;
            int oneKMemberCount = 0;
            int aboveGoldMemberCount = 0;
            int globalServiceAndAboveCount = 0;
            int subscribeCompanionCount = 0;
            int totalEPlusCompanionsEligible = 0;
            string regionType = string.Empty;
            int totalSubscribeCompanionCount = 0;
            string ePlusSubscriptionMessage = string.Empty;
            List<string> ePlusSubscriberNames = new List<string>();
            bool isOneOfIsGlobalSubscriber = false;
            string ePlusMsgTitle = string.Empty;
            string ePlusMsg = string.Empty;
            int eplusSeatsCount = 0;
            Dictionary<string, int> subscribeRegions = new Dictionary<string, int>();
            bool isEnablePreferredZoneSubscriptionMessages = _configuration.GetValue<bool>("isEnablePreferredZoneSubscriptionMessages");

            if (persistedReservation.TravelersCSL != null && persistedReservation.TravelersCSL.Count > 0)
            {
                #region
                StringBuilder names = new StringBuilder();
                foreach (var traveler in persistedReservation.TravelersCSL.Values)
                {
                    #region
                    if (traveler.MileagePlus != null)
                    {

                        if (traveler.MileagePlus.CurrentEliteLevel == 1)
                        {
                            silverMemberCount++;
                        }
                        else if (traveler.MileagePlus.CurrentEliteLevel == 2)
                        {
                            if (goldMemberCount == 0)
                            {
                                names.Append(traveler.FirstName + " " + traveler.LastName);
                            }
                            ++goldMemberCount;
                        }
                        else if (traveler.MileagePlus.CurrentEliteLevel > 2)
                        {
                            if (isEnablePreferredZoneSubscriptionMessages)
                            {
                                if (traveler.MileagePlus.CurrentEliteLevel == 3)
                                {
                                    if (platinumMemberCount == 0)
                                    {
                                        names.Append(traveler.FirstName + " " + traveler.LastName);
                                    }
                                    ++platinumMemberCount;
                                }
                                else if (traveler.MileagePlus.CurrentEliteLevel == 4)
                                {
                                    if (oneKMemberCount == 0)
                                    {
                                        names.Append(traveler.FirstName + " " + traveler.LastName);
                                    }
                                    ++oneKMemberCount;
                                }
                                else
                                {
                                    if (globalServiceAndAboveCount == 0)
                                    {
                                        names.Append(traveler.FirstName + " " + traveler.LastName);
                                    }
                                    globalServiceAndAboveCount++;
                                }
                            }
                            if (aboveGoldMemberCount == 0)
                            {
                                names.Append(traveler.FirstName + " " + traveler.LastName);
                            }
                            aboveGoldMemberCount++;
                        }

                        #region
                        Model.Common.MOBUASubscriptions objUASubscriptions = null;
                        bool isEPlusSubscriber = false;
                        var tupleResponse= await GetEPlusSubscriptionMessage(traveler.MileagePlus.MileagePlusId,
                                                       applicationID, sessionID,
                                                       traveler.FirstName + " " + traveler.LastName,
                                                        objUASubscriptions, isEPlusSubscriber,
                                                        isOneOfIsGlobalSubscriber,  subscribeCompanionCount,
                                                        regionType, isEnablePreferredZoneSubscriptionMessages);
                        objUASubscriptions = tupleResponse.objUASubscriptions;
                        isEPlusSubscriber = tupleResponse.isEPlusSubscriber;
                        isOneOfIsGlobalSubscriber = tupleResponse.isOneOfIsGlobalSubscriber;
                        subscribeCompanionCount = tupleResponse.subscribeCompanionCount;
                        regionType = tupleResponse.regionType;
                        ePlusSubscriptionMessage = ePlusSubscriptionMessage + " " +
                                                  tupleResponse.ePLUSMessage.Trim();
                        traveler.Subscriptions = objUASubscriptions;

                        if (isEPlusSubscriber)
                        {
                            //Calculate total number of EPlus Companions eligible to get free Eplus Seats
                            totalEPlusCompanionsEligible = totalEPlusCompanionsEligible + 1;
                            totalEPlusCompanionsEligible = totalEPlusCompanionsEligible + subscribeCompanionCount;

                            ePlusSubscriberNames.Add(traveler.FirstName + " " + traveler.LastName);
                            ePlusSubscriberCount = ePlusSubscriberCount + 1;

                            totalSubscribeCompanionCount += subscribeCompanionCount;

                            if (subscribeRegions.Count == 0)
                            {
                                subscribeRegions.Add(regionType.ToUpper(), subscribeCompanionCount);
                            }
                            else
                            {
                                if (subscribeRegions.ContainsKey(regionType.ToUpper()))
                                {
                                    subscribeRegions[regionType.ToUpper()] += subscribeCompanionCount;
                                }
                                else subscribeRegions.Add(regionType.ToUpper(), subscribeCompanionCount);
                            }
                        }
                        #endregion
                    }
                    #endregion
                }

                #region Updating subscription messages for preferred zone [AB-693, AB-694, AB-1091, AB-1092 updating the alert message for E+ and Premier silver members]
                // Added this new loop to add new subscription messages to preferred zone.
                // Else has the same behaviour before the preferred zone is added to messages
                // This loop changes the behaviour of region global but not the selected region types.
                // isEnablePreferredZoneSubscriptionMessages and isEnablePreferredZone both needs to be turned on for consistency in production.
                // New config entries were taken for the preferred seat.
                // Sending Message title as empty.
                if (isEnablePreferredZoneSubscriptionMessages)
                {
                    if (globalServiceAndAboveCount > 0)
                    {
                        ePlusMsg = _configuration.GetValue<string>("NEWEPAEPlusSeatMessageForHigherEliteLevel");
                    }
                    else if (oneKMemberCount > 0)
                    {
                        ePlusMsg = _configuration.GetValue<string>("NEWEPAEPlusSeatMessageOneK");
                    }
                    else if (platinumMemberCount > 0)
                    {
                        ePlusMsg = _configuration.GetValue<string>("NEWEPAEPlusSeatMessagePlatinum");
                    }
                    else if (goldMemberCount > 0 && ePlusSubscriberCount > 0 && totalSubscribeCompanionCount > 1)
                    {
                        ePlusMsg = _configuration.GetValue<string>("NEWEPAEPlusSeatMessageGoldEplusWithMultipleCompanions");
                    }
                    else if (goldMemberCount > 0)
                    {
                        ePlusMsg = string.Format(HttpUtility.HtmlDecode(_configuration.GetValue<string>("NEWEPAEPlusSeatMessageGold")), (goldMemberCount + 1));
                    }
                    else if (silverMemberCount > 0)
                    {
                        if ((subscribeRegions != null && subscribeRegions.Any() && subscribeRegions.Keys.ToList()[0].ToUpper() != "GLOBAL"))
                        {
                            eplusSeatsCount = getTotalEPlusSeats(ePlusSubscriberCount, totalSubscribeCompanionCount);
                            if (eplusSeatsCount == 9)
                            {
                                ePlusMsg = subscribeRegions.Count > 1 ? string.Format(_configuration.GetValue<string>("NEWEPARegionalMessageWithMultipleRegionMax"), "(for flights in eligible region)")
                                                                      : string.Format(_configuration.GetValue<string>("NEWEPARegionalMessageWithMultipleRegionMax"), "(" + subscribeRegions.Keys.ToList()[0].Replace("AND", "or") + ")");
                            }
                            else if (eplusSeatsCount > 1)
                            {
                                ePlusMsg = subscribeRegions.Count > 1 ? string.Format(_configuration.GetValue<string>("NEWEPARegionalMessageSilverWithMultipleRegionCompanions"), eplusSeatsCount, "(for flights in eligible region)")
                                                                      : string.Format(_configuration.GetValue<string>("NEWEPARegionalMessageSilverWithMultipleRegionCompanions"), eplusSeatsCount, "(" + subscribeRegions.Keys.ToList()[0].Replace("AND", "or") + ")");
                            }
                            else
                            {
                                ePlusMsg = subscribeRegions.Count > 1 ? string.Format(_configuration.GetValue<string>("NEWEPARegionalMessageSilverWithMultipleRegions"), "(for flights in eligible region)")
                                                                      : string.Format(_configuration.GetValue<string>("NEWEPARegionalMessageSilverWithMultipleRegions"), "(" + subscribeRegions.Keys.ToList()[0].Replace("AND", "or") + ")");
                            }
                        }
                        else if (ePlusSubscriberCount > 0 && totalSubscribeCompanionCount > 1)
                        {
                            ePlusMsg = _configuration.GetValue<string>("NEWEPAEPlusSeatMessageSilverEplusWithMultipleCompanions");
                        }
                        else if (ePlusSubscriberCount > 0 && totalSubscribeCompanionCount == 1)
                        {
                            ePlusMsg = _configuration.GetValue<string>("NEWEPAEPlusSeatMessageSilverEplusWithOneCompanion");
                        }
                        else if (ePlusSubscriberCount > 0 && totalSubscribeCompanionCount == 0)
                        {
                            ePlusMsg = _configuration.GetValue<string>("NEWEPAEPlusSeatMessageSilverEplusWithZeroCompanion");
                        }
                        else
                        {
                            ePlusMsg = _configuration.GetValue<string>("NEWEPAEPlusSeatMessageSilver");
                        }
                    }
                    else if (ePlusSubscriberCount > 1)
                    {
                        if ((subscribeRegions != null && subscribeRegions.Any() && subscribeRegions.Keys.ToList()[0].ToUpper() != "GLOBAL"))
                        {
                            if (totalSubscribeCompanionCount > 1)
                            {
                                ePlusMsg = subscribeRegions.Count > 1 ? string.Format(_configuration.GetValue<string>("NEWEPARegionalMessageWithMultipleRegionMax"), "(for flights in eligible region)")
                                                                      : string.Format(_configuration.GetValue<string>("NEWEPARegionalMessageWithMultipleRegionMax"), "(" + subscribeRegions.Keys.ToList()[0].Replace("AND", "or") + ")");
                            }
                            else
                            {
                                eplusSeatsCount = ePlusSubscriberCount + totalSubscribeCompanionCount > 9 ? 9 : ePlusSubscriberCount + totalSubscribeCompanionCount;
                                eplusSeatsCount = eplusSeatsCount - 1;
                                ePlusMsg = subscribeRegions.Count > 1 ? string.Format(_configuration.GetValue<string>("NEWEPARegionalMessageGeneralWithCompanions"), (eplusSeatsCount.ToString() + " companions"), "(for flights in eligible region)")
                                                                     : string.Format(_configuration.GetValue<string>("NEWEPARegionalMessageGeneralWithCompanions"), (eplusSeatsCount.ToString() + " companions"), "(" + subscribeRegions.Keys.ToList()[0].Replace("AND", "or") + ")");
                            }
                        }
                    }
                    else if (ePlusSubscriberCount == 1)
                    {
                        ePlusMsg = ePlusSubscriptionMessage.Trim();
                    }
                }
                #endregion
                #region This region is needed until the preferred zone is completely live
                // If not preferred zone will have the same old behaviour
                // If preferred zone was working and toggle isEnablePreferredZoneSubscriptionMessages is completly on, this else region will never execute 
                else
                {
                    if (aboveGoldMemberCount > 1)
                    {
                        ePlusMsgTitle = _configuration.GetValue<string>("NEWEPAEPlusSeatMessageTitle5");
                        ePlusMsg = string.Format(_configuration.GetValue<string>("NEWEPAEPlusSeatMessage4"), "8 companions");
                    }
                    else if (aboveGoldMemberCount > 0)
                    {
                        ePlusMsgTitle = _configuration.GetValue<string>("NEWEPAEPlusSeatMessageTitle5");
                        ePlusMsg = string.Format(_configuration.GetValue<string>("NEWEPAEPlusSeatMessage3"), "8 companions");
                    }
                    else if (goldMemberCount > 0 && ePlusSubscriberCount > 0)
                    {
                        #region
                        ePlusMsg = string.Format(_configuration.GetValue<string>("NEWEPlusMessageForGLOBALGoldMemberAndSubscriberPLUS"), (totalSubscribeCompanionCount + goldMemberCount * 2) + " companions");
                        #endregion
                    }
                    else if (ePlusSubscriberCount > 1 && goldMemberCount == 0)
                    {
                        #region
                        if (subscribeRegions.Count > 1)
                        {
                            ePlusMsgTitle = _configuration.GetValue<string>("NEWEPlusMultiSubscriberMessageTitle");
                            ePlusMsg = string.Format(_configuration.GetValue<string>("NEWEPlusMessageForMultipleSubscriberRegionPLUS"), totalSubscribeCompanionCount > 1 ? totalSubscribeCompanionCount + " companions" : "1 companion");
                        }
                        else
                        {
                            ePlusMsgTitle = _configuration.GetValue<string>("NEWEPlusMultiSubscriberMessageTitle");
                            ePlusMsg = string.Format(_configuration.GetValue<string>("NEWEPlusMessageForMultipleSubscriberPLUS"), totalSubscribeCompanionCount > 1 ? totalSubscribeCompanionCount + " companions" : "1 companion", subscribeRegions.Keys.ToList()[0]);
                        }
                        #endregion
                    }
                    else if (ePlusSubscriberCount == 1 && goldMemberCount == 0)
                    {
                        if (subscribeRegions.Keys.ToList()[0].ToUpper() == "GLOBAL")
                        {
                            ePlusMsgTitle = _configuration.GetValue<string>("NEWEPlusGlobalSubscriberMessageTitle").Trim();
                        }
                        else
                        {
                            ePlusMsgTitle = _configuration.GetValue<string>("NEWEPlusSubscriberMessageTitle").Trim();
                        }
                        ePlusMsg = ePlusSubscriptionMessage.Trim();
                    }
                    else if (ePlusSubscriberCount == 0 && goldMemberCount > 0)
                    {
                        if (goldMemberCount == 1)
                        {
                            ePlusMsgTitle = _configuration.GetValue<string>("NEWEPAEPlusSeatMessageTitle3");
                            ePlusMsg = string.Format(_configuration.GetValue<string>("NEWEPAEPlusSeatMessage3"), "1 companion");
                        }
                        else
                        {
                            ePlusMsgTitle = _configuration.GetValue<string>("NEWEPAEPlusSeatMessageTitle4");
                            ePlusMsg = string.Format(_configuration.GetValue<string>("NEWEPAEPlusSeatMessage4"), goldMemberCount + " companions");
                        }
                    }
                }
                #endregion
                #endregion
               
            }

            persistedReservation.AboveGoldMembers = aboveGoldMemberCount;
            persistedReservation.GoldMembers = goldMemberCount;
            persistedReservation.SilverMembers = silverMemberCount;
            persistedReservation.NoOfFreeEplusWithSubscriptions = totalEPlusCompanionsEligible;

            persistedReservation.EPAMessageTitle = ePlusMsgTitle;
            persistedReservation.EPAMessage = ePlusMsg;


            await _sessionHelperService.SaveSession<Reservation>(persistedReservation, sessionID, new List<string> { sessionID, persistedReservation.ObjectName }, persistedReservation.ObjectName).ConfigureAwait(false);
            return (persistedReservation, ePlusSubscriberCount);
        }

        private bool ShowEPAMessage(List<MOBSHOPTrip> trips, string origin, string destination)
        {
            bool result = false;

            if (trips != null)
            {
                foreach (var trip in trips)
                {
                    if (trip.FlattenedFlights != null)
                    {
                        foreach (var ff in trip.FlattenedFlights)
                        {
                            if (ff.Flights != null)
                            {
                                foreach (var flight in ff.Flights)
                                {
                                    if (flight.Origin == origin && flight.Destination == destination && !string.IsNullOrEmpty(flight.ServiceClassDescription) &&
                                        (flight.ServiceClassDescription.ToUpper().Trim().Equals("ECONOMY") ||
                                         flight.ServiceClassDescription.ToUpper().Trim().Equals("UNITED ECONOMY") ||
                                         flight.ServiceClassDescription.ToUpper().Trim().Equals("COACH")
                                        ))
                                    {
                                        if (!_shoppingUtility.IsSeatMapSupportedOa(flight.OperatingCarrier, flight.MarketingCarrier))
                                            result = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }



        private void ResetPopulatedSeats(List<MOBSeat> seats, string origin, string destination)
        {
            int index = 0;
            foreach (var seat in seats)
            {
                if (seat.Destination == destination && seat.Origin == origin)
                {
                    seats.RemoveAt(index);
                    break;
                }
                index++;
            }
        }

        private void AssignSeatUAOperatedFlag(List<MOBSeat> lstSeats, List<MOBSeat> allSeats, Reservation persistedReservation)
        {
            if (lstSeats != null)
            {
                foreach (var seat in lstSeats)
                {
                    if (persistedReservation != null)
                    {
                        foreach (MOBSHOPTrip trip in persistedReservation.Trips)
                        {
                            foreach (Model.Shopping.MOBSHOPFlattenedFlight flight in trip.FlattenedFlights)
                            {
                                foreach (Model.Shopping.MOBSHOPFlight shopFlight in flight.Flights)
                                {
                                    if (seat.Origin.Trim().ToUpper() == shopFlight.Origin.Trim().ToUpper()
                                        && seat.Destination.Trim().ToUpper() == shopFlight.Destination.Trim().ToUpper()
                                        && shopFlight.OperatingCarrier.Trim().ToUpper() == "UA")
                                    {
                                        seat.UAOperated = true;
                                    }
                                }
                            }
                        }
                    }

                    allSeats.Add(seat);
                }
            }
        }

        public Section GetPromoCodeRemovalMessage()
        {
            var changeInTravelerMessage = _configuration.GetValue<string>("PromoCodeRemovalMessage");
            return string.IsNullOrWhiteSpace(changeInTravelerMessage) ? null : new Section
            {
                Text1 = changeInTravelerMessage,
                Text2 = "Cancel",
                Text3 = "Continue"
            };
        }

        private async Task<(string ePLUSMessage, Model.Common.MOBUASubscriptions objUASubscriptions,  bool isEPlusSubscriber,  bool isOneOfIsGlobalSubscriber,  int subscribeCompanionCount, string regionType)> GetEPlusSubscriptionMessage(string mpAccountNumber, int applicationID, string sessionID, string travelerName,  Model.Common.MOBUASubscriptions objUASubscriptions,  bool isEPlusSubscriber,  bool isOneOfIsGlobalSubscriber,  int subscribeCompanionCount,  string regionType, bool isEnablePreferredZoneSubscriptionMessages)
        {
            objUASubscriptions = await _merchandizingServices.GetEPlusSubscriptions(mpAccountNumber, applicationID, sessionID).ConfigureAwait(false);
            string ePLUSMessage = string.Empty;
            isEPlusSubscriber = false;
            if (objUASubscriptions != null && objUASubscriptions.SubscriptionTypes != null && objUASubscriptions.SubscriptionTypes.Count() > 0)
            {
                #region
                isEPlusSubscriber = true;
                foreach (MOBUASubscription objUASubscription in objUASubscriptions.SubscriptionTypes)
                {
                    foreach (var item in objUASubscription.Items)
                    {
                        #region
                        if (item.Id.Trim().ToUpper() == "EplusSubscribeRegion".ToUpper())
                        {
                            regionType = item.CurrentValue.Trim();
                        }
                        else if (item.Id.Trim().ToUpper() == "EPlusSubscribeCompanionCount".ToUpper())
                        {
                            subscribeCompanionCount = Convert.ToInt32(item.CurrentValue.Trim());
                        }
                        #endregion
                    }
                }
                #region Updating subscription messages for preferred zone [AB-693, AB-694, AB-1091, AB-1092 updating the alert message for E+ and Premier silver members]
                // Added this new loop to add new subscription messages to preferred zone.
                // Else has the same behaviour before the preferred zone is added to messages
                // This loop changes the behaviour of region global but not the selected region types.
                // isEnablePreferredZoneSubscriptionMessages and isEnablePreferredZone both needs to be turned on for consistency in production.
                // New config entries were taken for the preferred seat.
                // Sending Message title as empty.
                if (isEnablePreferredZoneSubscriptionMessages)
                {
                    if (regionType.Trim().ToUpper() == "GLOBAL")
                    {
                        isOneOfIsGlobalSubscriber = true;
                        if (subscribeCompanionCount > 1)
                        {
                            ePLUSMessage = _configuration.GetValue<string>("NEWEPAEPlusSeatMessageEplusWithMultipleCompanions");
                        }
                        else if (subscribeCompanionCount == 1)
                        {
                            ePLUSMessage = _configuration.GetValue<string>("NEWEPAEPlusSeatMessageEplusWithOneCompanion");
                        }
                        else
                        {
                            ePLUSMessage = _configuration.GetValue<string>("NEWEPAEPlusSeatMessageEplus");
                        }
                    }
                    else
                    {
                        string regionMessage = string.IsNullOrEmpty(regionType) ? string.Empty : "(" + regionType.Replace("and", "or") + ")";

                        ePLUSMessage = subscribeCompanionCount > 0 ?
                                                                   string.Format(_configuration.GetValue<string>("NEWEPARegionalMessageGeneralWithCompanions"), subscribeCompanionCount.ToString() + (subscribeCompanionCount == 1 ? " companion" : " companions"), regionMessage)
                                                                   : string.Format(_configuration.GetValue<string>("NEWEPARegionalMessageGeneral"), regionMessage);
                    }
                }
                #endregion
                else
                {
                    if (regionType.Trim().ToUpper() == "GLOBAL" && subscribeCompanionCount == 0)
                    {
                        isOneOfIsGlobalSubscriber = true;
                        if (_configuration.GetValue<string>("NEWEPlusMessageForIndividualGLOBALSubscriber") != null)
                        {
                            ePLUSMessage = _configuration.GetValue<string>("NEWEPlusMessageForIndividualGLOBALSubscriber");
                        }
                    }
                    else if (regionType.Trim().ToUpper() == "GLOBAL" && subscribeCompanionCount > 0)
                    {
                        isOneOfIsGlobalSubscriber = true;
                        if (_configuration.GetValue<string>("NEWEPlusMessageForGLOBALSubscriberPLUS") != null)
                        {
                            ePLUSMessage = String.Format(_configuration.GetValue<string>("NEWEPlusMessageForGLOBALSubscriberPLUS"), subscribeCompanionCount.ToString() + (subscribeCompanionCount == 1 ? " companion" : " companions"));
                        }
                    }
                    else
                    {
                        if (subscribeCompanionCount == 0)
                        {
                            if (_configuration.GetValue<string>("NEWEPlusMessageForIndividualSubscriber") != null)
                            {
                                ePLUSMessage = String.Format(_configuration.GetValue<string>("NEWEPlusMessageForIndividualSubscriber"), regionType);
                            }
                        }
                        else if (subscribeCompanionCount > 0)
                        {
                            if (_configuration.GetValue<string>("NEWEPlusMessageForSubscriberPLUS") != null)
                            {
                                ePLUSMessage = String.Format(_configuration.GetValue<string>("NEWEPlusMessageForSubscriberPLUS"), subscribeCompanionCount.ToString() + (subscribeCompanionCount == 1 ? " companion" : " companions"), regionType);
                            }
                        }
                    }
                }
                #endregion
            }
            return (ePLUSMessage,objUASubscriptions,isEPlusSubscriber,isOneOfIsGlobalSubscriber,subscribeCompanionCount,regionType);
        }

        private int getTotalEPlusSeats(int ePlusSubscriberCount, int totalSubscribeCompanionCount)
        {
            int eplusSeats = 0;
            if (ePlusSubscriberCount > 0 && totalSubscribeCompanionCount > 1)
            {
                eplusSeats = ePlusSubscriberCount + totalSubscribeCompanionCount > 9 ? 9 : ePlusSubscriberCount + totalSubscribeCompanionCount;
            }
            else if (ePlusSubscriberCount > 0 && totalSubscribeCompanionCount == 1)
            {
                eplusSeats = ePlusSubscriberCount + totalSubscribeCompanionCount;
            }
            else if (ePlusSubscriberCount > 0 && totalSubscribeCompanionCount == 0)
            {
                eplusSeats = ePlusSubscriberCount;
            }
            return eplusSeats;
        }

        private async Task<List<MOBItem>> GetMessagesFromDb(string seatMessageKey)
        {
            return seatMessageKey.IsNullOrEmpty()
                    ? null
                    : await new MPDynamoDB(_configuration, _dynamoDBService, null, _headers).GetMPPINPWDTitleMessages(new List<string> { seatMessageKey });
        }

        public MOBSection AssignAlertMessage(List<MOBItem> seatAssignmentMessage)
        {
            MOBSection alertMsg = new MOBSection() { };
            if (seatAssignmentMessage != null && seatAssignmentMessage.Count > 0)
            {
                foreach (var msg in seatAssignmentMessage)
                {
                    if (msg != null)
                    {
                        switch (msg.Id.ToUpper())
                        {
                            case "HEADER":
                                alertMsg.Text1 = msg.CurrentValue.Trim();
                                break;
                            case "BODY":
                                alertMsg.Text2 = msg.CurrentValue.Trim();
                                break;
                            case "FOOTER":
                                alertMsg.Text3 = msg.CurrentValue.Trim();
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            return alertMsg;
        }

    }

}
