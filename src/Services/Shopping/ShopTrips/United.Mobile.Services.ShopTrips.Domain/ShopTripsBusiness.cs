using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.FSRHandler;
using United.Common.Helper.Merchandize;
using United.Common.Helper.PageProduct;
using United.Common.Helper.Shopping;
using United.Definition;
using United.Definition.Shopping;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.Shopping;
using United.Mobile.Model.Common.SSR;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Model.Shopping.Misc;
using United.Mobile.Model.Shopping.UnfinishedBooking;
using United.Service.Presentation.CommonModel;
using United.Service.Presentation.ReferenceDataRequestModel;
using United.Service.Presentation.ReferenceDataResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.Boombox;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.Extensions;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Services.FlightShopping.Common.LMX;
using United.Services.FlightShopping.Common.OrganizeResults;
using United.Utility.Enum;
using United.Utility.Helper;
using Characteristic = United.Service.Presentation.CommonModel.Characteristic;
using CreditType = United.Mobile.Model.Shopping.CreditType;
using CreditTypeColor = United.Mobile.Model.Shopping.CreditTypeColor;
using ELFRitMetaShopMessages = United.Common.Helper.Shopping.ELFRitMetaShopMessages;
using FlightReservationResponse = United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse;
using FlowType = United.Utility.Enum.FlowType;
using MOBSHOPTraveler = United.Mobile.Model.Shopping.MOBSHOPTraveler;
using PKDispenserResponse = United.Mobile.Model.Shopping.PKDispenserResponse;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using Task = System.Threading.Tasks.Task;
using Microsoft.AspNetCore.Http;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.Fluent;
using MOBRegisterTravelersResponse = United.Mobile.Model.Travelers.MOBRegisterTravelersResponse;
using United.Utility.Extensions;

namespace United.Mobile.Services.ShopTrips.Domain
{
    public class ShopTripsBusiness : IShopTripsBusiness
    {
        private readonly ICacheLog<ShopTripsBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly ISharedItinerary _sharedItinerary;
        private readonly IUnfinishedBooking _unfinishedBooking;
        private readonly IFlightShoppingService _flightShoppingService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly IDPService _dPService;
        private readonly IPNRRetrievalService _updatePNRService;
        private readonly IReferencedataService _referencedataService;
        private readonly ILMXInfo _lmxInfo;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ICMSContentService _iCMSContentService;
        private readonly IMerchandizingServices _merchandizingServices;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly IOmniCart _omniCart;
        private readonly IProductInfoHelper _productInfoHelper;
        private readonly IPKDispenserService _pKDispenserService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        public static string CURRENCY_TYPE_MILES = "miles";
        public static string PRICING_TYPE_CLOSE_IN_FEE = "CLOSEINFEE";
        private AirportDetailsList airportsList = null;
        private readonly ITravelerCSL _travelerCSL;
        private readonly ICachingService _cachingService;
        private readonly PKDispenserPublicKey _pKDispenserPublicKey;
        private readonly IGMTConversionService _gMTConversionService;
        private readonly IFFCShoppingcs _ffcShoppingcs;
        private readonly ILogger<ShopTripsBusiness> _logger1;
        private readonly IFeatureSettings _featureSettings;
        private readonly IFeatureToggles _featureToggles;
        public ShopTripsBusiness(ICacheLog<ShopTripsBusiness> logger
            , IConfiguration configuration
            , IHeaders headers
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility
            , ISharedItinerary sharedItinerary
            , IUnfinishedBooking unfinishedBooking
            , IFlightShoppingService flightShoppingService
            , IDynamoDBService dynamoDBService
            , IDPService dPService
            , IPNRRetrievalService updatePNRService
            , IReferencedataService referencedataService
            , ILMXInfo lmxInfo
            , IShoppingCartService shoppingCartService
            , ICMSContentService iCMSContentService
            , IMerchandizingServices merchandizingServices
            , IShoppingSessionHelper shoppingSessionHelper
            , IOmniCart omniCart
            , IProductInfoHelper productInfoHelper
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , ICachingService cachingService
            , IGMTConversionService gMTConversionService
            , ITravelerCSL travelerCSL
            , IPKDispenserService pKDispenserService
            , IFFCShoppingcs ffcShoppingcs
            , ILogger<ShopTripsBusiness> logger1
            , IFeatureSettings featureSettings
            , IFeatureToggles featureToggles)
        {
            _logger = logger;
            _configuration = configuration;
            _headers = headers;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
            _sharedItinerary = sharedItinerary;
            _unfinishedBooking = unfinishedBooking;
            _flightShoppingService = flightShoppingService;
            _dynamoDBService = dynamoDBService;
            _dPService = dPService;
            _updatePNRService = updatePNRService;
            _referencedataService = referencedataService;
            _lmxInfo = lmxInfo;
            _shoppingCartService = shoppingCartService;
            _iCMSContentService = iCMSContentService;
            _merchandizingServices = merchandizingServices;
            _shoppingSessionHelper = shoppingSessionHelper;
            _omniCart = omniCart;
            _productInfoHelper = productInfoHelper;
            _cachingService = cachingService;
            _gMTConversionService = gMTConversionService;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _travelerCSL = travelerCSL;
            _pKDispenserService = pKDispenserService;
            _ffcShoppingcs = ffcShoppingcs;
            _pKDispenserPublicKey = new PKDispenserPublicKey(_configuration, _cachingService, _dPService, _pKDispenserService, headers);
            _logger1 = logger1;
            _featureSettings = featureSettings;
            _featureToggles = featureToggles;
        }

        public async Task<FareRulesResponse> GetFareRulesForSelectedTrip(GetFareRulesRequest getFareRulesRequest)
        {
            FareRulesResponse response = new FareRulesResponse();
            response.SessionId = getFareRulesRequest.SessionId;
            #region
            Session session = await _shoppingSessionHelper.GetBookingFlowSession(getFareRulesRequest.SessionId);
            //if (traceSwitch.TraceInfo)
            //{
            //    profile.LogEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBSHOPGetFareRulesRequest>(request.SessionId, "GetFareRulesForSelectedTrip", "Request", request.Application.Id, request.Application.Version.Major, request.DeviceId, request));
            //}
            response.TransactionId = getFareRulesRequest.TransactionId;
            response.SessionId = getFareRulesRequest.SessionId;
            MOBSHOPReservation reservationFromPersist = new MOBSHOPReservation(_configuration, _cachingService);
            Reservation bookingPathReservation = new Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(getFareRulesRequest.SessionId, bookingPathReservation.ObjectName, new List<string> { getFareRulesRequest.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
            var fareRules = await GetPriceFareRules(getFareRulesRequest, session);

            if (_shoppingUtility.EnableAwardFareRules(getFareRulesRequest.Application.Id, getFareRulesRequest.Application.Version.Major)) // Award Fare Rules Toggle
            {
                if (session.IsAward) // Award booking
                {
                    try
                    {
                        var awardFareRules = new List<FareRuleList>();
                        if (await _featureSettings.GetFeatureSettingValue("EnableAwardFareRuleText").ConfigureAwait(false))
                            awardFareRules = await GetFareRuleTextBySDL(getFareRulesRequest, session);
                        else
                            awardFareRules = (await GetAwardPriceFareRules()).FirstOrDefault().FareRuleTextList;

                        for (int i = 0; i < fareRules.Count(); i++)
                        {
                            fareRules[i].FareRuleTextList = awardFareRules;
                        }
                    }
                    catch (Exception ex)
                    {
                        //if (traceSwitch.TraceError)
                        //{
                        //    shopping.LogEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetFareRulesForSelectedTrip - GetAwardPriceFareRules", "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, new MOBExceptionWrapper(ex)));
                        //}
                        _logger.LogError("GetFareRulesForSelectedTrip - Exception {@Exception}", JsonConvert.SerializeObject(ex));
                        throw new MOBUnitedException("Could not get Award Fare Rules.");
                    }
                }
            }

            bookingPathReservation.FareRules = fareRules;
            await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, getFareRulesRequest.SessionId, new List<string> { getFareRulesRequest.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName);
            response.Reservation = await _shoppingUtility.GetReservationFromPersist(reservationFromPersist, getFareRulesRequest.SessionId);
            #endregion
            return response;
        }

        private async Task<List<FareRules>> GetPriceFareRules(GetFareRulesRequest request, Session session)
        {
            #region
            List<FareRules> mobFareRules = new List<FareRules>();
            string cartId = session.CartId, token = session.Token;
            FareRuleRequest fareRuleRequest = new FareRuleRequest();
            fareRuleRequest.CartId = cartId;

            string jsonRequest = JsonConvert.SerializeObject(fareRuleRequest);
            //if (traceSwitch.TraceError)
            //{
            //    logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "GetPriceFareRules - Request", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonRequest));
            //}
            string actionName = "GetFareRules";
            var jsonResponse = await _flightShoppingService.FarePriceRules(token, actionName, jsonRequest, session.SessionId);
            if (jsonResponse != null)
            {
                FareRuleResponse response = JsonConvert.DeserializeObject<FareRuleResponse>(jsonResponse);
                if (response != null && response.FareRules != null && response.FareRules.Count > 0) //**// response.Status == StatusType.Success) as this service is returning 0 for all the calls even if its success sent email to stogo and chris G to take a look at it - Venkat Sep 29 ,2014
                {
                    // logEntries.Add(LogEntry.GetLogEntry<FareRuleResponse>(_headers.ContextValues, "GetPriceFareRules - DeSerialized Response", "DeSerialized Response", request.Application.Id, request.Application.Version.Major, request.DeviceId, response));

                    if (response.FareRules != null)
                    {
                        foreach (FareRule fareRule in response.FareRules)
                        {
                            #region
                            FareRules mobfareRule = new FareRules();
                            List<FareRuleList> mobFareRuleList = new List<FareRuleList>();
                            foreach (KeyValuePair<string, string> item in fareRule.RulesList)
                            {
                                FareRuleList mobFareRule = new FareRuleList();
                                mobFareRule.TextType = "Header";
                                mobFareRule.RuleText = item.Key;
                                mobFareRuleList.Add(mobFareRule);
                                int spiltTextLenght = _configuration.GetValue<string>("FareRulesTextSpiltLenght") != null ? Convert.ToInt32(_configuration.GetValue<string>("FareRulesTextSpiltLenght")) : 5000;
                                int cutOffDifference = _configuration.GetValue<string>("FareRulesTextCutOffDifference") != null ? Convert.ToInt32(_configuration.GetValue<string>("FareRulesTextCutOffDifference")) : 1500;
                                if (item.Value.Length > spiltTextLenght + cutOffDifference) // Because just want to check if the string is 7000 length no need to spilt as per james client can handle up to 7500. So for the text lenght greater than 6500 (like 8000 wil be spilit in to 2 text values one with 5000 length and other with 3000 lenght)
                                {
                                    string str = item.Value;
                                    int subStringLength = spiltTextLenght;
                                    int startIndex = 0;
                                    int count = (int)Math.Ceiling((double)item.Value.Length / subStringLength);
                                    for (int i = 1; i <= count; i++)
                                    {
                                        string textValue = string.Empty;
                                        if (i == count)
                                        {
                                            subStringLength = item.Value.Length - startIndex;
                                            textValue = str.Substring(startIndex);
                                        }
                                        else
                                        {
                                            textValue = str.Substring(startIndex, subStringLength).Substring(0, Math.Min(str.Substring(startIndex, subStringLength).Length, str.Substring(startIndex, subStringLength).LastIndexOf(" ") == -1 ? 0 : str.Substring(startIndex, subStringLength).LastIndexOf(" ")));
                                        }
                                        mobFareRule = new FareRuleList();
                                        mobFareRule.TextType = "Text";
                                        mobFareRule.RuleText = textValue;
                                        mobFareRuleList.Add(mobFareRule);
                                        startIndex = startIndex + textValue.Length;
                                    }
                                }
                                else
                                {
                                    mobFareRule = new FareRuleList();
                                    mobFareRule.TextType = "Text";
                                    mobFareRule.RuleText = item.Value;
                                    mobFareRuleList.Add(mobFareRule);
                                }
                            }
                            mobfareRule.Origin = fareRule.Origin;
                            mobfareRule.Destination = fareRule.Destination;
                            mobfareRule.FareBasisCode = fareRule.FareBasis;
                            mobfareRule.ServiceClass = fareRule.ServiceClass;
                            mobfareRule.FareRuleTextList = mobFareRuleList;
                            mobFareRules.Add(mobfareRule);
                            #endregion
                        }
                    }
                }
                else
                {
                    // logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "GetPriceFareRules - Exception", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonResponse));
                    _logger.LogError("GetPriceFareRules - Exception {@Response}", JsonConvert.SerializeObject(jsonResponse));
                    throw new MOBUnitedException("Unable to get Fare Rules."); //**//
                }
            }
            else
            {
                // logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "GetPriceFareRules - Exception", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonResponse));
                _logger.LogError("GetPriceFareRules - Exception {@Response}", JsonConvert.SerializeObject(jsonResponse));
                throw new MOBUnitedException("Unable to get Fare Rules.");//**//
            }
            return mobFareRules;
            #endregion
        }

        private async Task<List<FareRules>> GetAwardPriceFareRules()
        {
            var docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles("AwardFareRules", _headers.ContextValues.SessionId, true).ConfigureAwait(false);
            var awardFareRule = new FareRules();
            awardFareRule.FareRuleTextList = docs.Select(x => new FareRuleList { RuleText = x.LegalDocument, TextType = x.Title }).ToList();
            return new List<FareRules> { awardFareRule };
        }
        private async Task<List<FareRuleList>> GetFareRuleTextBySDL(GetFareRulesRequest getFareRulesRequest, Session session)
        {
            List<CMSContentMessage> lstMessages = await _ffcShoppingcs.GetSDLContentByGroupName(getFareRulesRequest, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
            var fareRuleSDLResponse = ShopStaticUtility.GetSDLMessageFromList(lstMessages, "AwardFareRuleTextList");//.FirstOrDefault()?.ContentFull;
            List<FareRuleList> fareRulesList = new List<FareRuleList>();
            foreach (var fareRule in fareRuleSDLResponse)
            {
                FareRuleList fareRuleListHeader = new FareRuleList();
                fareRuleListHeader.RuleText = fareRule.ContentShort.Split('|')[0].TrimEnd();
                fareRuleListHeader.TextType = fareRule.ContentShort.Split('|')[1].TrimEnd();
                FareRuleList fareRuleListBody = new FareRuleList();
                fareRuleListBody.RuleText = fareRule.ContentFull;
                fareRuleListBody.TextType = fareRule.HeadLine;
                fareRulesList.Add(fareRuleListHeader);
                fareRulesList.Add(fareRuleListBody);
            }
            return fareRulesList;


        }
        public async Task<TripShare> GetShareTrip(ShareTripRequest shareTripRequest)
        {
            TripShare response = new TripShare();

            if (!string.IsNullOrEmpty(shareTripRequest.SessionId))
            {
                //load from from persist
                SelectTrip selectTrip = new SelectTrip();
                var selectTripFromPersist = await _sessionHelperService.GetSession<SelectTrip>(shareTripRequest.SessionId, selectTrip.ObjectName, new List<string> { shareTripRequest.SessionId, selectTrip.ObjectName });
                if (selectTripFromPersist == null)
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("BookingSessionExpiredMessage") ?? _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                response = await GetTripShare(selectTripFromPersist.Responses[selectTripFromPersist.LastSelectTripKey], shareTripRequest);
            }
            return response;
        }

        private async Task<TripShare> GetTripShare(SelectTripResponse selectTripResponse, ShareTripRequest shareTripRequest)
        {
            var tripShare = new TripShare();
            try
            {
                var reservation = selectTripResponse?.Availability?.Reservation;

                if (reservation != null && (reservation.AwardTravel
                   || reservation.IsEmp20
                   || (reservation.ShopReservationInfo != null && reservation.ShopReservationInfo.IsCorporateBooking)
                   || (reservation.ShopReservationInfo2 != null && reservation.ShopReservationInfo2.IsYATravel)
                   || reservation.IsReshopChange))
                {
                    return tripShare = null;
                }
                if (selectTripResponse != null && selectTripResponse.Availability != null && reservation != null
                    && reservation.Trips.Count > 0
                    && ((reservation.FareLock != null && reservation.FareLock.FareLockProducts != null && reservation.FareLock.FareLockProducts.Count > 0))
                    || (shareTripRequest.OverrideFarelockValidation && _configuration.GetValue<bool>("EnableFinalRTIScreenshotFix")))
                {
                    var tripShareEmail = new SHOPTripShareMessage();
                    tripShare.CommonCaption = _configuration.GetValue<string>("ShareTripInSoftRTICommonCaptionText"); // should be part of url
                    tripShare.PlaceholderTitle = _shoppingUtility.BuilTripShareEmailBodyTripText(reservation.SearchType, reservation.Trips, false);

                    if (_configuration.GetValue<bool>("EnableShareTripHardCodedUrlForTesting"))
                    {
                        //unreachable code all config entry is false for "EnableShareTripHardCodedUrlForTesting"
                        var dotComHardcodedUrl = await _sessionHelperService.GetSession<string>(shareTripRequest.SessionId, "United.Persist.Definition.Shopping.TripHardCodedUrlForTesting", new List<string> { shareTripRequest.SessionId, "United.Persist.Definition.Shopping.TripHardCodedUrlForTesting" }).ConfigureAwait(false);
                        tripShare.Url = dotComHardcodedUrl;
                    }
                    else
                    {
                        //TODO after CSL imlements
                        //var cslResponse = flightShopping.GetFlightShoppingShareUrl("", selectTripResponse.Request, selectTripResponse.Request.SessionId, "GetTripShare");
                        //tripShare.Url = (cslResponse != null) ? cslResponse.VendorQueryReturnUrl : string.Empty;
                        string sharedItinerayAccessCode = await _sharedItinerary.InsertSharedItinerary(reservation, (MOBRequest)shareTripRequest, shareTripRequest.SessionId, "GetTripShare");
                        tripShare.Url = string.Format(_configuration.GetValue<string>("ShareTripDotComUrl"), sharedItinerayAccessCode);
                    }

                    //email subject 
                    tripShareEmail.Subject = _configuration.GetValue<string>("ShareTripInSoftRTIEmailSubject");

                    string priceWithCurrency = string.Empty;
                    string currencyCode = string.Empty;
                    string emailBodyTripText = string.Empty;
                    string emailBodyPriceText = string.Empty;

                    string emailBodySegmentText = string.Empty;
                    string emailBodySegmentConnectionText = string.Empty;
                    string emailBodySegmentOperatedByText = string.Empty;

                    foreach (var price in reservation.Prices)
                    {
                        if (price != null && !string.IsNullOrEmpty(price.DisplayType) && price.DisplayType.Equals("TOTAL", StringComparison.InvariantCultureIgnoreCase))
                        {
                            currencyCode = price.CurrencyCode;
                        }
                    }

                    priceWithCurrency = reservation.Trips?.LastOrDefault()?.FlattenedFlights?.LastOrDefault()?.Flights?.LastOrDefault(f => f.ShoppingProducts.Any())?.ShoppingProducts?.LastOrDefault()?.Price;

                    if (string.IsNullOrEmpty(priceWithCurrency))
                    {
                        string errorMsg = "share trip product price is null or empty";
                        // logEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(shareTripRequest.SessionId, actionName, "Exception", shareTripRequest.Application.Id, shareTripRequest.Application.Version.Major, shareTripRequest.DeviceId, errorMsg, true, false));
                        _logger.LogError("GetTripShare - Exception {@Error}", errorMsg);
                        throw new Exception(errorMsg);
                    }

                    if (reservation.SearchType == "OW" && reservation.Trips != null && reservation.Trips.Count > 0)
                    {
                        //email body with origin and dest Roundtrip from Wausau to South Bend
                        emailBodyTripText = _shoppingUtility.BuilTripShareEmailBodyTripText(reservation.SearchType, reservation.Trips, true);

                        //email body with price
                        emailBodyPriceText = _shoppingUtility.BuildTripSharePrice(priceWithCurrency, currencyCode, tripShare.Url);
                        emailBodySegmentText = _shoppingUtility.BuildTripShareSegmentText(reservation.Trips[0]);

                    }
                    else if (reservation.SearchType == "RT" && reservation.Trips != null && reservation.Trips.Count > 1)
                    {
                        //email body with origin and dest Roundtrip from Wausau to South Bend
                        emailBodyTripText = _shoppingUtility.BuilTripShareEmailBodyTripText(reservation.SearchType, reservation.Trips, true);

                        //email body with price
                        emailBodyPriceText = _shoppingUtility.BuildTripSharePrice(priceWithCurrency, currencyCode, tripShare.Url);

                        foreach (var trip in reservation.Trips)
                        {
                            emailBodySegmentText += _shoppingUtility.BuildTripShareSegmentText(trip);
                        }
                    }
                    else if (reservation.SearchType == "MD" && reservation.Trips != null && reservation.Trips.Count > 0)
                    {
                        //email body with origin and dest Roundtrip from Wausau to South Bend
                        emailBodyTripText = _shoppingUtility.BuilTripShareEmailBodyTripText(reservation.SearchType, reservation.Trips, true);

                        //email body with price
                        emailBodyPriceText = _shoppingUtility.BuildTripSharePrice(priceWithCurrency, currencyCode, tripShare.Url);

                        foreach (var trip in reservation.Trips)
                        {
                            emailBodySegmentText += _shoppingUtility.BuildTripShareSegmentText(trip);
                        }
                    }

                    tripShareEmail.Body = $"{emailBodyTripText}{emailBodyPriceText}{emailBodySegmentText}{_configuration.GetValue<string>("ShareTripInSoftRTIEmailBodyClosingHtmlTags")}";
                    tripShare.Email = tripShareEmail;
                }
            }

            catch (Exception ex)
            {
                //string[] messages = ex.Message.Split('#');
                //if (traceSwitch.TraceInfo)
                //{
                //    MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                //    exceptionWrapper.Message = messages[0];
                //    logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(shareTripRequest.SessionId, actionName, "Exception", shareTripRequest.Application.Id, shareTripRequest.Application.Version.Major, shareTripRequest.DeviceId, exceptionWrapper, true, false));
                //    //logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(shareTripRequest.SessionId, "SelectTripShare", "TripShareBuildError", shareTripRequest.Application.Id, shareTripRequest.Application.Version.Major, shareTripRequest.DeviceId, exceptionWrapper, true, false));
                //}
                _logger.LogError("SelectTripShare TripShareBuildError {@Error}", JsonConvert.SerializeObject(ex));
                throw new Exception("unable to build the Share Trips text/link.", ex);
            }
            return tripShare;
        }

        public async Task<ShoppingTripFareTypeDetailsResponse> GetTripCompareFareTypes(ShoppingTripFareTypeDetailsRequest shoppingTripFareTypeDetailsRequest)
        {
            ShoppingTripFareTypeDetailsResponse response = new ShoppingTripFareTypeDetailsResponse();
            //  shopping.LogEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBSHOPShoppingTripFareTypeDetailsRequest>(request.SessionID, actionName, "Request", request.Application.Id, request.Application.Version.Major, request.DeviceId, request));//Common Login Code
            if (!string.IsNullOrEmpty(shoppingTripFareTypeDetailsRequest.CartID))
            {
                response = await GetTripColumnInfo(shoppingTripFareTypeDetailsRequest);

            }

            return await System.Threading.Tasks.Task.FromResult(response);
        }

        private async Task<ShoppingTripFareTypeDetailsResponse> GetTripColumnInfo(ShoppingTripFareTypeDetailsRequest request)
        {
            ShoppingTripFareTypeDetailsResponse response = new ShoppingTripFareTypeDetailsResponse();

            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string> { request.SessionId, session.ObjectName }).ConfigureAwait(false);

            if (session == null)
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("BookingSessionExpiryMessageExceptionCode"), _configuration.GetValue<string>("BookingSessionExpiryMessage"));
            }
            string token = string.IsNullOrEmpty(session.Token) ? _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration).Result : session.Token;
            var jsonResponse = await _flightShoppingService.GetColumnInfo(token, request.SessionId, request.CartID, request.LanguageCode, "US");

            if (jsonResponse != null)
            {
                var responseCSLColumnInfo = JsonConvert.DeserializeObject<FareColumnContentInformationResponse>(jsonResponse);

                //logEntries.Add(LogEntry.GetLogEntry<FareColumnContentInformationResponse>(session.SessionId, "GetTripColumnInfo", "Deserialize - Response", request.Application.Id, request.Application.Version.Major, request.DeviceId, responseCSLColumnInfo, true, false));
                _logger.LogInformation("GetTripColumnInfo - Deserialize - Response SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Response {response}", session.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, responseCSLColumnInfo);

                if (responseCSLColumnInfo != null && responseCSLColumnInfo.Status == StatusType.Success && responseCSLColumnInfo.Errors == null
                    && responseCSLColumnInfo.ColumnInformation != null && responseCSLColumnInfo.ColumnInformation.Columns != null &&
                    responseCSLColumnInfo.ColumnInformation.Columns.Any())
                {
                    response.Columns = PopulateColumnsFareTypeDetails(responseCSLColumnInfo.ColumnInformation);

                    return response;
                }
                else
                {
                    if (responseCSLColumnInfo != null && responseCSLColumnInfo.Errors != null)
                    {
                        throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                    }

                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
        }

        private List<MOBSHOPShoppingProduct> PopulateColumnsFareTypeDetails(ColumnInformation columnInfo)
        {
            List<MOBSHOPShoppingProduct> columns = null;

            //if we have columns...
            if (columnInfo != null && columnInfo.Columns != null && columnInfo.Columns.Count > 0)
            {
                columns = new List<MOBSHOPShoppingProduct>();
                foreach (Column column in columnInfo.Columns)
                {
                    MOBSHOPShoppingProduct product = new MOBSHOPShoppingProduct();
                    product.LongCabin = column.DataSourceLabel;
                    product.Description = column.Description;
                    product.Type = column.Type;
                    product.SubType = column.SubType != null ? column.SubType : string.Empty;
                    product.FareContentDescription = column.FareContentDescription;
                    columns.Add(product);
                }
            }
            return columns;
        }

        public async Task<SelectTripResponse> MetaSelectTrip(MetaSelectTripRequest metaSelectTripRequest, HttpContext httpContext = null)
        {
            SelectTripResponse response = new SelectTripResponse();
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            Session session = null;
            try
            {
                new ForceUpdateVersion(_configuration).ForceUpdateForNonSupportedVersion(metaSelectTripRequest.Application.Id, metaSelectTripRequest.Application?.Version.Major, FlowType.BOOKING);

                session = await _shoppingSessionHelper.CreateShoppingSession(metaSelectTripRequest.Application.Id, metaSelectTripRequest.DeviceId, metaSelectTripRequest.Application.Version.Major, metaSelectTripRequest.TransactionId, string.Empty, string.Empty);//shopping.LogEntries, traceSwitch, );
                if (session == null)
                {
                    throw new MOBUnitedException("Unable to get booking session.");
                }

                session.CustomerID = metaSelectTripRequest.CustomerId;
                response.TransactionId = metaSelectTripRequest.TransactionId;
                response.LanguageCode = metaSelectTripRequest.LanguageCode;
                if (_configuration.GetValue<bool>("EnableMetaSearchFix") && metaSelectTripRequest.CatalogItems?.Count > 0)
                {
                    session.CatalogItems = metaSelectTripRequest.CatalogItems;
                    await _sessionHelperService.SaveSession(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);
                }
                // SoftRTI (United) deeplink
                if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI") && !string.IsNullOrEmpty(metaSelectTripRequest.TypeOfDeeplink) && metaSelectTripRequest.TypeOfDeeplink.ToUpper().Equals(DeeplinkType.TRIPSHARE.ToString()))
                {
                    var tripShareTupleResponse = await TripShareSelectTrip(session, metaSelectTripRequest, httpContext);
                    response.Availability = tripShareTupleResponse.Item1;
                    response.ShopRequest = tripShareTupleResponse.Item2;
                }
                else
                {
                    //Google flights deeplink
                    response.Availability = await MetaSelectTrip(session, metaSelectTripRequest, httpContext);
                }
                response.Disclaimer = GetDisclaimerString();

                if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "YES")
                {
                    response.CartId = response.Availability.CartId;
                }
                bool isCFOPVersionCheck = GeneralHelper.IsApplicationVersionGreaterorEqual(metaSelectTripRequest.Application.Id, metaSelectTripRequest.Application.Version.Major, _configuration.GetValue<string>("AndriodCFOP_Booking_Reshop_PostbookingAppVersion"), _configuration.GetValue<string>("IphoneCFOP_Booking_Reshop_PostbookingAppVersion"));
                if (isCFOPVersionCheck)
                {
                    if (!string.IsNullOrEmpty(_configuration.GetValue<string>("IsBookingCommonFOPEnabled")) && session.IsReshopChange == false)
                    {
                        response.ShoppingCart = await _shoppingUtility.PopulateShoppingCart(shoppingCart, FlowType.BOOKING.ToString(), _shoppingUtility.IsEnableOmniCartMVP2Changes(metaSelectTripRequest.Application.Id, metaSelectTripRequest.Application.Version.Major, true) ? response.Availability.SessionId : metaSelectTripRequest.MedaSessionId, response.Availability.CartId, metaSelectTripRequest, response.Availability.Reservation);
                    }
                    else if (!string.IsNullOrEmpty(_configuration.GetValue<string>("IsReshopCommonFOPEnabled")) && session.IsReshopChange == true)
                    {
                        response.ShoppingCart = await _shoppingUtility.PopulateShoppingCart(shoppingCart, FlowType.RESHOP.ToString(), metaSelectTripRequest.MedaSessionId, response.Availability.CartId);
                    }
                }

                if (_configuration.GetValue<bool>("IsEnableBEFeeWavierMessageForMetaTrip") && _shoppingUtility.IsFeewaiverEnabled(response.Availability.Reservation.IsReshopChange))
                {
                    if (metaSelectTripRequest.TypeOfDeeplink.ToUpper() == "TRIPSHARE"
                            && _configuration.GetValue<bool>("ShowPriceMismatchMessage")
                            && response.Availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Any(m => m.Messages.Contains(_configuration.GetValue<string>("PriceMismatchMessage"))))
                    {
                        if (response.Availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                            response.Availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                        response.Availability.Reservation.ShopReservationInfo2.InfoWarningMessages = _shoppingUtility.TripShareFeeWaiverMessage(response.Availability.Reservation.ShopReservationInfo2.InfoWarningMessages);
                    }
                    else
                    {
                        if (response.Availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                            response.Availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                        response.Availability.Reservation.ShopReservationInfo2.InfoWarningMessages = _shoppingUtility.UpdateFeeWaiverMessage(response.Availability.Reservation.ShopReservationInfo2.InfoWarningMessages);
                    }


                }
            }//turning off due to MB-7290
             //if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI") && Mobile.DAL.GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, "AndroidShareTripInSoftRTIVersion", "iPhoneShareTripInSoftRTIVersion", "", "", true))
             //{
             //    if (response.ShoppingCart == null)
             //        response.ShoppingCart = new MOBShoppingCart();

            //    //response.ShoppingCart.TripShare = _shoppingUtility.GetTripShare(response, shopping.LogEntries, traceSwitch, flightShopping,"MetaSelectTrip");
            //    response.ShoppingCart.TripShare = _shoppingUtility.IsShareTripValid(response);
            //}
            catch (MOBUnitedException uaex)
            {
                if (session != null)
                    _logger.LogWarning("MetaSelectTrip - Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException { Message = uaex.Message };
            }
            catch (Exception ex)
            {
                if (_configuration.GetValue<string>("ShareTripExpiredOrInvalidCodes").Split('|').Any(s => ex.Message.Contains(s)) && _configuration.GetValue<bool>("EnableShareTripInSoftRTI") && !string.IsNullOrEmpty(metaSelectTripRequest.TypeOfDeeplink)
                    && metaSelectTripRequest.TypeOfDeeplink.ToUpper().Equals(DeeplinkType.TRIPSHARE.ToString())
                    && GeneralHelper.isApplicationVersionGreater(metaSelectTripRequest.Application.Id, metaSelectTripRequest.Application.Version.Major, "AndroidShareTripInSoftRTIVersion", "iPhoneShareTripInSoftRTIVersion", "", "", true, _configuration))
                {
                    response.Exception = new MOBException { Message = _configuration.GetValue<string>("MetaTripExceptionMessage") };
                }
                else if (_configuration.GetValue<string>("ShareTripSoldOutErrorCodes").Split('|').Any(s => ex.Message.Contains(s)) && _configuration.GetValue<bool>("EnableShareTripInSoftRTI") && !string.IsNullOrEmpty(metaSelectTripRequest.TypeOfDeeplink)
                    && metaSelectTripRequest.TypeOfDeeplink.ToUpper().Equals(DeeplinkType.TRIPSHARE.ToString())
                    && GeneralHelper.isApplicationVersionGreater(metaSelectTripRequest.Application.Id, metaSelectTripRequest.Application.Version.Major, "AndroidShareTripInSoftRTIVersion", "iPhoneShareTripInSoftRTIVersion", "", "", true, _configuration))
                {
                    if (response != null && response.Availability == null)
                    {
                        response.Availability = new MOBSHOPAvailability();
                        response.Availability.SessionId = (session != null) ? session.SessionId : string.Empty;
                    }
                    response.Exception = new MOBException { Code = "9997", Message = _configuration.GetValue<string>("MetaTripFlightSoldOutExceptionMessage") };

                }
                else
                {
                    if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                    {
                        response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                    }
                    else
                    {
                        response.Exception = new MOBException("9999", ex.Message);
                    }
                }
            }
            return await Task.FromResult(response);
        }

        //MetSelectTrip Methods-Kriti
        private async Task<(MOBSHOPAvailability, MOBSHOPShopRequest)> TripShareSelectTrip(Session session, MetaSelectTripRequest request, HttpContext httpContext = null)
        {
            MOBSHOPAvailability availability = new MOBSHOPAvailability();
            MOBSHOPShopRequest shopRequest = null;
            bool isOAFlashError = false;
            MOBSHOPSelectUnfinishedBookingRequest tripShareBooking = await _sharedItinerary.GetSharedTripItinerary(session, request);
            if (!_configuration.GetValue<bool>("BasicEconomyRestrictionsForShareFlightsBugFixToggle") == true)
            {
                tripShareBooking.SelectedUnfinishBooking.IsSharedFlightRequest = true;
            }
            if (_configuration.GetValue<bool>("EnableOmniChannelCartMVP1"))
            {
                var tupleResponse = await _unfinishedBooking.GetShopPinDownDetailsV2(session, tripShareBooking, httpContext);
                availability = tupleResponse.Item1;
                isOAFlashError = tupleResponse.Item2;
                if ( _shoppingUtility.IsFSROAFlashSaleEnabled(session?.CatalogItems) && isOAFlashError)
                    shopRequest = MapToMOBShopRequestFromUnfinishedBooking(tripShareBooking.SelectedUnfinishBooking, request);
            }
            else
            {
                availability = await _unfinishedBooking.GetShopPinDownDetails(session, tripShareBooking);
            }
            session.CartId = availability.CartId;
            if (request.TypeOfDeeplink.ToUpper() == "TRIPSHARE")
            {
                SetPriceChangeMessage(availability, tripShareBooking.SelectedUnfinishBooking);
            }
            await _sessionHelperService.SaveSession(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);

            return (availability, shopRequest);
        }

        private MOBSHOPShopRequest MapToMOBShopRequestFromUnfinishedBooking(MOBSHOPUnfinishedBooking selectedUnfinishBooking, MetaSelectTripRequest request)
        {
            MOBSHOPShopRequest shopRequest = new MOBSHOPShopRequest();
            shopRequest.TravelerTypes = selectedUnfinishBooking.TravelerTypes;
            shopRequest.Application = request.Application;
            shopRequest.DeviceId = request.DeviceId;
            shopRequest.TransactionId = request.TransactionId;
            shopRequest.CatalogItems = request.CatalogItems;
            shopRequest.GetNonStopFlightsOnly = true;
            if (selectedUnfinishBooking?.Trips?.Count > 0)
            {
                shopRequest.Trips = new List<MOBSHOPTripBase>();
                foreach (var trip in selectedUnfinishBooking.Trips)
                {
                    MOBSHOPTripBase tripBase = new MOBSHOPTripBase();
                    tripBase.ArrivalDate = trip.ArrivalDate;
                    tripBase.DepartDate = Convert.ToDateTime(trip.DepartDate).ToString("MM/dd/yyyy");
                    tripBase.Origin = trip.Origin;
                    tripBase.Destination = trip.Destination;
                    tripBase.Cabin = _configuration.GetValue<string>("FSROAFlightDefaultCabin");// OAShareFlight is for econ flights default option for searcj
                    shopRequest.Trips.Add(tripBase);
                }
            }
            shopRequest.FareType = "lf";
            shopRequest.TravelType = TravelType.RA.ToString();
            shopRequest.Experiments = new List<string>();
            shopRequest.Experiments.Add(ShoppingExperiments.NoChangeFee.ToString());
            shopRequest.Experiments.Add(ShoppingExperiments.FSRRedesignA.ToString());
            shopRequest.SearchType = selectedUnfinishBooking.SearchType;
            return shopRequest;
        }

        private void SetPriceChangeMessage(MOBSHOPAvailability availability, MOBSHOPUnfinishedBooking selectedUnfinishBooking)
        {
            if (_configuration.GetValue<bool>("ShowPriceMismatchMessage"))
            {
                var shareAmount = selectedUnfinishBooking.Trips?.FirstOrDefault()?.Flights?.FirstOrDefault().Products?.FirstOrDefault().Prices?.FirstOrDefault().Amount ?? 0;
                var grandTotal = availability.Reservation.Prices?.FirstOrDefault()?.Value ?? 0;
                if (Convert.ToDouble(shareAmount) != grandTotal)
                {
                    if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();
                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetPriceMismatchMessage());
                }
            }
        }

        private async Task<MOBSHOPAvailability> MetaSelectTrip(Session session, MetaSelectTripRequest request, HttpContext httpContext = null)
        {
            if (!await MetaSyncUserSession(session, request))
                return null;
            List<CMSContentMessage> lstMessages = await _ffcShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
            List<ReservationFlightSegment> segments = null;
            var availability = new MOBSHOPAvailability();
            if (_configuration.GetValue<bool>("EnableOmniChannelCartMVP1") && !session.IsReshopChange)
            {
                var tupleResponse = await GetMetaShopBookingDetailsV2(session, request, segments);
                availability = tupleResponse.availability;
                segments = tupleResponse.segments;
            }
            else
            {
                var tupleResponse = await GetMetaShopBookingDetails(session, request, segments);
                availability = tupleResponse.availability;
                segments = tupleResponse.segments;
            }
            availability.SessionId = session.SessionId;
            availability.CartId = request.CartId;

            if (availability.Reservation == null)
                return availability;

            var selectTrip = new SelectTrip
            {
                SessionId = session.SessionId,
                CartId = request.CartId,
                LastSelectTripKey = "0",
                Responses = new SerializableDictionary<string, SelectTripResponse>()
            };
            var selectTripResponse = new SelectTripResponse
            {
                Availability = new MOBSHOPAvailability { Reservation = availability.Reservation }
            };
            selectTrip.Responses.Add(selectTrip.LastSelectTripKey, selectTripResponse);
            await _sessionHelperService.SaveSession(selectTrip, availability.SessionId, new List<string> { availability.SessionId, selectTrip.ObjectName }, selectTrip.ObjectName).ConfigureAwait(false);

            bool Is24HoursWindow = false;
            if (_configuration.GetValue<bool>("EnableForceEPlus"))
            {
                if (availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT != null)
                {
                    Is24HoursWindow = TopHelper.Is24HourWindow(Convert.ToDateTime(availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT));
                }
            }
            if (availability.Reservation.ShopReservationInfo2 == null)
            {
                availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();
            }
            availability.Reservation.ShopReservationInfo2.IsForceSeatMap = _shoppingUtility.IsForceSeatMapforEPlus(false, availability.Reservation.IsELF, Is24HoursWindow, request.Application.Id, request.Application.Version.Major);

            if (_shoppingUtility.IsEnabledNationalityAndResidence(false, request.Application.Id, request.Application.Version.Major))
            {
                if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                    availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();
                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence = await IsRequireNatAndCR(availability.Reservation, request.Application, session.SessionId, request.DeviceId, session.Token);
                if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence)
                {
                    availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.NationalityErrMsg = NationalityResidenceMsgs.NationalityErrMsg;
                    availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ResidenceErrMsg = NationalityResidenceMsgs.ResidenceErrMsg;
                    availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.NationalityAndResidenceErrMsg = NationalityResidenceMsgs.NationalityAndResidenceErrMsg;
                    availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.NationalityAndResidenceHeaderMsg = NationalityResidenceMsgs.NationalityAndResidenceHeaderMsg;
                }
            }

            //#region 214448 - Unaccompanied Minor Age
            availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo = new ReservationAgeBoundInfo();
            availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.MinimumAge = Convert.ToInt32(_configuration.GetValue<string>("umnrMinimumAge"));
            availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.UpBoundAge = Convert.ToInt32(_configuration.GetValue<string>("umnrUpBoundAge"));
            availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.ErrorMessage = _configuration.GetValue<string>("umnrErrorMessage");
            //#endregion
            bool isCFOPVersionCheck = GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("AndriodCFOP_Booking_Reshop_PostbookingAppVersion"), _configuration.GetValue<string>("IphoneCFOP_Booking_Reshop_PostbookingAppVersion"));
            if (isCFOPVersionCheck)
            {
                if (!string.IsNullOrEmpty(_configuration.GetValue<string>("IsBookingCommonFOPEnabled")))
                {
                    availability.Reservation.IsBookingCommonFOPEnabled = Convert.ToBoolean(_configuration.GetValue<string>("IsBookingCommonFOPEnabled"));
                }
            }

            #region Special Needs

            if (_shoppingUtility.EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
            {
                //testing, remove
                try
                {
                    var persistedShopBookingDetailsResponse = new ShopBookingDetailsResponse();
                    persistedShopBookingDetailsResponse = await _sessionHelperService.GetSession<ShopBookingDetailsResponse>(session.SessionId, persistedShopBookingDetailsResponse.ObjectName, new List<string> { session.SessionId, persistedShopBookingDetailsResponse.ObjectName }).ConfigureAwait(false);
                    if (persistedShopBookingDetailsResponse != null)
                    {
                        if (_configuration.GetValue<bool>("EnableServiceAnimalEnhancements"))
                        {
                            var selectTripRequest = new SelectTripRequest { CatalogItems = request.CatalogItems, Application = request.Application, DeviceId = request.DeviceId };
                            // populate avail. special needs for the itinerary
                            availability.Reservation.ShopReservationInfo2.SpecialNeeds = await GetItineraryAvailableSpecialNeeds(session, request.Application.Id, request.Application.Version.Major, request.DeviceId, persistedShopBookingDetailsResponse.Reservation.FlightSegments, "en-US", availability.Reservation, selectTripRequest);
                        }
                        else
                        {
                            availability.Reservation.ShopReservationInfo2.SpecialNeeds = await GetItineraryAvailableSpecialNeeds(session, request.Application.Id, request.Application.Version.Major, request.DeviceId, persistedShopBookingDetailsResponse.Reservation.FlightSegments, "en-US");
                        }


                        // update persisted reservation object too
                        var bookingPathReservation = new Reservation();
                        bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(session.SessionId, bookingPathReservation.ObjectName, new List<string> { session.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                        if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null)
                        {
                            bookingPathReservation.ShopReservationInfo2.SpecialNeeds = availability.Reservation.ShopReservationInfo2.SpecialNeeds;
                            await _sessionHelperService.SaveSession(bookingPathReservation, session.SessionId, new List<string> { session.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("MetaSelectTrip - GetItineraryAvailableSpecialNeeds {@Exception}", JsonConvert.SerializeObject(e));

                }
            }

            #endregion

            #region WheelChair Sizer changes
            if (await _shoppingUtility.IsEnableWheelChairSizerChanges(request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false)
                && !session.IsReshopChange)
            {
                try
                {
                    if (availability.Reservation != null && availability.Reservation.ShopReservationInfo2 != null)
                    {
                        availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo = new WheelChairSizerInfo();
                        availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo.ImageUrl1 = _shoppingUtility.GetFormatedUrl(httpContext.Request.Host.Value,
                                         httpContext.Request.Scheme, _configuration.GetValue<string>("WheelChairImageUrl"), true);
                        availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo.ImageUrl2 = _shoppingUtility.GetFormatedUrl(httpContext.Request.Host.Value,
                                            httpContext.Request.Scheme, _configuration.GetValue<string>("WheelChairFoldedImageUrl"), true);

                        var sdlKeyForWheelchairSizerContent = _configuration.GetValue<string>("SDLKeyForWheelChairSizerContent");
                        var message = !string.IsNullOrEmpty(sdlKeyForWheelchairSizerContent) ? await GetCMSContentMessageByKey(sdlKeyForWheelchairSizerContent, request, session).ConfigureAwait(false) : null;
                        if (message != null)
                        {
                            availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo.WcHeaderMsg = message?.HeadLine;
                            availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo.WcBodyMsg = message?.ContentFull;
                        }
                        _shoppingUtility.BuildWheelChairSizerOAMsgs(availability.Reservation);
                        var bookingPathReservation = new Reservation();
                        bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(session.SessionId, bookingPathReservation.ObjectName, new List<string> { session.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                        if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null)
                        {
                            bookingPathReservation.ShopReservationInfo2.WheelChairSizerInfo = availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo;
                            await _sessionHelperService.SaveSession(bookingPathReservation, session.SessionId, new List<string> { session.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger1.LogError("MetaSelectTrip - WheelChairSizerContent {@message} {@stackTrace}", ex.Message, ex.StackTrace);
                }
            }
            #endregion

            #region TaxID Collection
            if (await _featureToggles.IsEnableTaxIdCollectionForLATIDCountries(request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false)
        && !session.IsReshopChange && _shoppingUtility.IsTaxIdCountryEnable(availability.Reservation?.Trips, lstMessages))
            {
                try
                {
                    await _shoppingUtility.BuildTaxIdInformationForLatidCountries(availability.Reservation, request, session, lstMessages).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ILoggerError("MetaSelectTrip - TaxID Collection {@message} {@stackTrace}", ex.Message, ex.StackTrace);
                }
            }
            #endregion
            var reservation = ReservationToPersistReservation(availability);
            reservation.CSLReservationJSONFormat = JsonConvert.SerializeObject(segments);
            await _sessionHelperService.SaveSession(reservation, session.SessionId, new List<string> { session.SessionId, reservation.ObjectName }, reservation.ObjectName).ConfigureAwait(false);
            if (_shoppingUtility.EnableInflightContactlessPayment(request.Application.Id, request.Application.Version.Major, reservation.IsReshopChange))
            {
                FireForGetInFlightCLEligibilityCheck(reservation, request, session);
            }
            if (!_shoppingUtility.AllowElfMetaSearchUpsell(request.Application.Id, request.Application.Version.Major))
                return availability;

            availability.OfferMetaSearchElfUpsell = availability.Reservation.IsELF || availability.Reservation.ShopReservationInfo2.IsIBELite || availability.Reservation.ShopReservationInfo2.IsIBE;

            if (availability.Reservation.IsELF)
            {
                await SetAvailabilityELFProperties(availability, availability.Reservation.NumberOfTravelers > 1, _shoppingUtility.EnableSSA(request.Application.Id, request.Application.Version.Major));
            }
            else if (availability.Reservation.ShopReservationInfo2.IsIBELite || availability.Reservation.ShopReservationInfo2.IsIBE)
            {
                try
                {
                    await SetAvailabilityBeLiteProperties(availability, session, request);
                }
                catch
                {
                    availability.OfferMetaSearchElfUpsell = false;
                }
            }

            #region Save Unfinished Booking

            if (EnableMetaPathSavedBooking() && _shoppingUtility.EnableUnfinishedBookings(request))
            {
                try
                {
                    // Only save unfinished booking for regular revenue customer
                    //if (!session.IsAward && !session.IsReshopChange && !isCorporateBooking && string.IsNullOrEmpty(session.EmployeeId))
                    //{
                    if (session.CustomerID > 0) // for signed in customer only
                    {
                        System.Threading.Tasks.Task.Factory.StartNew(() => _unfinishedBooking.SaveAnUnfinishedBooking(session, request, MapUnfinishedBookingFromMOBSHOPReservation(availability.Reservation)));
                    }
                    //}
                }
                catch (Exception ex)
                {

                    //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "MetaSelectTrip - SaveAnUnfinishedBooking", "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, new MOBExceptionWrapper(ex)));
                    _logger.LogError("MetaSelectTrip - SaveAnUnfinishedBooking Error {@Exception}", JsonConvert.SerializeObject(ex));

                }
            }

            #endregion

            return availability;
        }

        private List<string> GetDisclaimerString()
        {
            List<string> disclaimer = new List<string>();

            if (_configuration.GetValue<string>("MakeReservationDisclaimer") != null)
            {
                disclaimer.Add(_configuration.GetValue<string>("MakeReservationDisclaimer"));
            }
            else
            {
                disclaimer.Add("*Miles shown are the actual miles flown for this segment.Mileage accrued will vary depending on the terms and conditions of your frequent flyer program.");
            }
            return disclaimer;
        }

        public async Task<bool> IsRequireNatAndCR(MOBSHOPReservation reservation, MOBApplication application, string sessionID, string deviceID, string token)
        {
            bool isRequireNatAndCR = false;
            List<string> NationalityResidenceCountriesList = new List<string>();

            #region Load list of countries from cache/persist
            var list = await _cachingService.GetCache<List<MOBSHOPCountry>>(_configuration.GetValue<string>("NationalityResidenceCountriesListStaticGUID") + "Booking2.0NationalityResidenceCountries", _headers.ContextValues.TransactionId);
            var lst = JsonConvert.DeserializeObject<List<MOBSHOPCountry>>(list);
            #endregion Load list of countries from cache/persist

            if (lst == null)
            {
                lst = await GetNationalityResidenceCountries(application.Id, deviceID, application.Version.Major, null, sessionID, token);
                try
                {
                    await _cachingService.SaveCache<List<MOBSHOPCountry>>(_configuration.GetValue<string>("NationalityResidenceCountriesListStaticGUID") + "Booking2.0NationalityResidenceCountries", lst, _headers.ContextValues.TransactionId, new TimeSpan(1, 30, 0));
                }
                catch (Exception e)
                {
                    _logger.LogError("ShopTripBusiness -MetaSelectTrip IsRequireNatAndCR SaveCache Failed - Exception {error} and SessionId {guid}", e.Message, sessionID);
                }
            }
            if (lst != null && lst.Count > 0)
                NationalityResidenceCountriesList = lst.Select(c => c.CountryCode).ToList();
            else if (lst == null)
            {
                string dList = _configuration.GetValue<string>("TaxPriceChangeCountries") as string; // If any issue with CSL loading country list from Web.Config
                if (!string.IsNullOrEmpty(dList))
                {
                    foreach (string s in dList.Split(',').ToList())
                    {
                        NationalityResidenceCountriesList.Add(s);
                    }
                }
            }

            if (reservation != null && reservation.Trips != null && NationalityResidenceCountriesList != null && NationalityResidenceCountriesList.Count > 1)
            {
                foreach (MOBSHOPTrip trip in reservation.Trips)
                {
                    if (isRequireNatAndCR)
                        break;
                    foreach (var flight in trip.FlattenedFlights)
                    {
                        foreach (var stopFlights in flight.Flights)
                        {
                            isRequireNatAndCR = IsNatAndCRExists(stopFlights.OriginCountryCode, stopFlights.DestinationCountryCode, NationalityResidenceCountriesList);

                            if (!isRequireNatAndCR && stopFlights.StopInfos != null)
                            {
                                foreach (var stop in stopFlights.StopInfos)
                                {
                                    isRequireNatAndCR = IsNatAndCRExists(stop.OriginCountryCode, stop.DestinationCountryCode, NationalityResidenceCountriesList);
                                    isRequireNatAndCR = IsNatAndCRExists(stop.OriginCountryCode, stop.DestinationCountryCode, NationalityResidenceCountriesList);
                                }
                                if (isRequireNatAndCR)
                                    break;
                            }
                            if (isRequireNatAndCR)
                                break;
                        }

                        if (isRequireNatAndCR)
                            break;
                    }
                }
            }
            return isRequireNatAndCR;
        }

        private async Task<bool> MetaSyncUserSession(Session session, MetaSelectTripRequest request)
        {
            bool ok = false;

            string action = "UserSessionSync";
            //logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "MetaSyncUserSession - Request url for MetaSyncUserSession", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, url));
            _logger.LogInformation("MetaSyncUserSession - Request url for MetaSyncUserSession SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Url {action}", session.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, action);

            MetaUserSessionSyncRequest metaUserSessionSync = new MetaUserSessionSyncRequest();
            metaUserSessionSync.AuthTokenId = request.MedaSessionId;
            metaUserSessionSync.CartId = request.CartId;
            string jsonRequest = JsonConvert.SerializeObject(metaUserSessionSync);

            //logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "MetaSyncUserSession - Request for MetaSyncUserSession", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonRequest));
            _logger.LogInformation("MetaSyncUserSession - Request for MetaSyncUserSession SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Request {request}", session.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonRequest);
            string token = string.IsNullOrEmpty(session.Token) ? _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration).Result : session.Token;
            var response = await _flightShoppingService.GetUserSession<MetaUserSessionSyncResponse>(token, action, jsonRequest, _headers.ContextValues.SessionId);

            //logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "MetaSyncUserSession - Response for UserSessionSync", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonResponse));
            _logger.LogInformation("MetaSyncUserSession - Response for UserSessionSync SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Response {response}", session.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, response);
            if (response.Status.Equals("1"))
            {
                ok = true;
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("MetaTripExceptionMessage"));
            }


            return ok;
        }

        private United.Services.FlightShopping.Common.ShopSelectRequest BuildShopSelectRequest(MetaSelectTripRequest request)
        {
            United.Services.FlightShopping.Common.ShopSelectRequest shopSelectRequest = new United.Services.FlightShopping.Common.ShopSelectRequest();
            shopSelectRequest.AccessCode = "1A7370E9-A532-4376-BD39-41795F01321C";
            shopSelectRequest.CartId = request.CartId;
            shopSelectRequest.ChannelType = "MOBILE";
            shopSelectRequest.CountryCode = "US";
            shopSelectRequest.VendorQuery = !request.RequeryForUpsell;
            shopSelectRequest.RequeryForUpsell = request.RequeryForUpsell;

            if (request.RequeryForUpsell)
            {
                shopSelectRequest.BBXSolutionSetId = request.BbxSolutionId;
                shopSelectRequest.BBXCellId = request.BbxCellId;
            }
            return shopSelectRequest;
        }

        private async Task<(MOBSHOPAvailability availability, List<ReservationFlightSegment> segments)> GetMetaShopBookingDetailsV2(Session session, MetaSelectTripRequest request, List<ReservationFlightSegment> segments, bool isUpgradedFromElf = false)
        {
            var availability = new MOBSHOPAvailability();
            United.Mobile.Model.Shopping.MOBSHOPReservation reservation;
            List<CMSContentMessage> lstMessages = await _ffcShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
            string action = "ShopBookingDetailsV2";

            var shopSelectRequest = BuildShopSelectRequest(request);
            shopSelectRequest.DeviceId = request.DeviceId;
            var jsonRequest = JsonConvert.SerializeObject(shopSelectRequest);
            await _sessionHelperService.SaveSession<string>(jsonRequest, session.SessionId, new List<string> { session.SessionId, typeof(United.Services.FlightShopping.Common.ShopSelectRequest).FullName }, typeof(United.Services.FlightShopping.Common.ShopSelectRequest).FullName).ConfigureAwait(false);
            //#TakeTokenFromSession
            // string token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration);
            var bookingresponse = await _flightShoppingService.GetBookingDetailsV2<FlightReservationResponse>(session.Token, action, jsonRequest, session.SessionId);
            await _sessionHelperService.SaveSession(bookingresponse, session.SessionId, new List<string> { session.SessionId, new FlightReservationResponse().GetType().FullName }, new FlightReservationResponse().GetType().FullName).ConfigureAwait(false);
            if (bookingresponse != null)
            {
                if (bookingresponse != null && bookingresponse.Status.Equals(StatusType.Success) && bookingresponse.Reservation != null)
                {
                    var response = new FlightReservationResponse();
                    response = await RegisterFlights(bookingresponse, session, request);
                    AssignMissingPropertiesfromRegisterFlightsResponse(bookingresponse, response);

                    if (!(_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, session.IsReshopChange) && request.IsCatalogOnForTavelerTypes))
                    {
                        ThrowMessageIfAnyPaxTypeIsNotADT(response.DisplayCart);
                    }
                    session.CartId = response.CartId;
                    session.IsMeta = true;
                    await _sessionHelperService.SaveSession(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);

                    if (response.DisplayCart.IsElf && PricesAreInverted(response))
                        return await AutoUpsellElfV2(session, request, segments, response);

                    availability.Upsells = GetMetaShopUpsellsAndRequestedProduct(response);

                    reservation = new MOBSHOPReservation(_configuration, _cachingService);

                    //-------Feature 208204--- Common class data carrier for hirarchy methds-----
                    MOBSHOPDataCarrier _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
                    //_mOBSHOPDataCarrier.SearchType = request.;
                    //_mOBSHOPDataCarrier.AwardTravel = request.AwardTravel;
                    //if (_configuration.GetValue<bool>("Shopping - bPricingBySlice"))
                    //{
                    //    // One time decide to assign text for all the products in the Flights. Will be using in BE & Compare Screens
                    //    _mOBSHOPDataCarrier.PriceFormText = GetPriceFromText(shopRequest.SearchType);
                    //}
                    //-----------

                    if (response.DisplayCart != null && response.DisplayCart.DisplayPrices != null && response.Reservation != null)
                    {
                        if (reservation.ShopReservationInfo2 == null)
                            reservation.ShopReservationInfo2 = new ReservationInfo2();
                        reservation.CheckedbagChargebutton = _configuration.GetValue<string>("ViewCheckedBagChargesButton");
                        reservation.IsELF = response.DisplayCart.IsElf;
                        reservation.IsSSA = _shoppingUtility.EnableSSA(request.Application.Id, request.Application.Version.Major);
                        reservation.ShopReservationInfo2.Characteristics = ShopStaticUtility.GetCharacteristics(response.Reservation);
                        reservation.ShopReservationInfo2.IsIBELite = _shoppingUtility.IsIBELiteFare(response.DisplayCart);
                        reservation.ShopReservationInfo2.IsIBE = _shoppingUtility.IsIBEFullFare(response.DisplayCart);
                        reservation.ShopReservationInfo2.IsNonRefundableNonChangable = _shoppingUtility.IsNonRefundableNonChangable(response.DisplayCart);
                        reservation.ShopReservationInfo2.FareRestrictionsMessage = reservation.ShopReservationInfo2.IsIBELite
                                                                                   ? _configuration.GetValue<string>("IBELiteRestrictionsMessageMetaRTI") : null; //needed only in metapath
                        reservation.ISInternational = new IsInternationalFlagMapper().IsInternationalFromResponse(response);
                        reservation.IsMetaSearch = true;
                        await new ELFRitMetaShopMessages(_configuration, _dynamoDBService, _legalDocumentsForTitlesService, _headers).AddElfRtiMetaSearchMessages(reservation);
                        await AddElfUpgradeMessageForMetasearch(isUpgradedFromElf, reservation);
                        bool isSupportedVersion = GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, "AndroidBundleVersion", "IOSBundleVersion", "", "", true, _configuration);
                        if (isSupportedVersion)
                        {
                            if (_configuration.GetValue<bool>("IsEnableBundlesForBasicEconomyBooking"))
                            {
                                reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles");
                            }
                            else
                            {
                                reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles") && !(response.DisplayCart.IsElf || _shoppingUtility.IsIBEFullFare(response.DisplayCart));
                            }
                        }
                        reservation.SessionId = session.SessionId;
                        reservation.CartId = request.CartId;
                        reservation.MetaSessionId = request.MedaSessionId;
                        reservation.PointOfSale = response.Reservation.PointOfSale.Country.CountryCode;
                        reservation.NumberOfTravelers = response.Reservation.NumberInParty;
                        switch (response.DisplayCart.SearchType)
                        {
                            case SearchType.OneWay:
                                reservation.SearchType = "OW";
                                break;
                            case SearchType.RoundTrip:
                                reservation.SearchType = "RT";
                                break;
                            case SearchType.MultipleDestination:
                                reservation.SearchType = "MD";
                                break;
                            default:
                                reservation.SearchType = string.Empty;
                                break;
                        }
                        var fareClass = await GetFareClassAtShoppingRequestFromPersist(session.SessionId);
                        var flightDepartDatesForSelectedTrip = new List<string>();
                        reservation.Trips = await PopulateMetaTrips(_mOBSHOPDataCarrier, response.CartId, null, session.SessionId, request.Application.Id, request.DeviceId, request.Application.Version.Major, true, response.DisplayCart.DisplayTrips, fareClass, flightDepartDatesForSelectedTrip);
                        if (reservation.Trips != null && reservation.Trips.Count > 0)
                        {
                            flightDepartDatesForSelectedTrip.AddRange(reservation.Trips.Select(shopTrip => shopTrip.TripId + "|" + shopTrip.DepartDate));
                        }
                        if (_shoppingUtility.IsEnableTaxForAgeDiversification(session.IsReshopChange, request.Application.Id, request.Application.Version.Major))
                        {
                            reservation.Prices = _shoppingUtility.GetPrices(response.DisplayCart.DisplayPrices, false, session.SessionId, searchType: reservation.SearchType.ToString(), session: session);

                        }
                        else
                        {
                            reservation.Prices = GetPrices(response.DisplayCart.DisplayPrices, false, session.SessionId);
                        }
                        if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                        {
                            reservation.Prices.AddRange(GetPrices(response.DisplayCart.DisplayFees, false, string.Empty));
                        }
                        reservation.ELFMessagesForRTI = await new ELFRitMetaShopMessages(_configuration, _dynamoDBService, _legalDocumentsForTitlesService, _headers).GetELFShopMessagesForRestrictions(reservation, request.Application.Id);
                        //need to add close in fee to TOTAL
                        reservation.Prices = AdjustTotal(reservation.Prices);
                        if (_shoppingUtility.IsEnableTaxForAgeDiversification(session.IsReshopChange, request.Application.Id, request.Application.Version.Major))
                        {
                            if (reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                                reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();

                            reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = _shoppingUtility.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices);

                            if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                            {
                                reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes.Add(ShopStaticUtility.AddFeesAfterPriceChange(response.DisplayCart.DisplayFees));
                            }
                        }
                        else
                        {
                            reservation.Taxes = GetTaxAndFees(response.DisplayCart.DisplayPrices, response.Reservation.NumberInParty);
                            if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                            {
                                //combine fees into taxes so that totals are correct
                                var tempList = new List<DisplayPrice>();
                                tempList.AddRange(response.DisplayCart.DisplayPrices);
                                tempList.AddRange(response.DisplayCart.DisplayFees);
                                reservation.Taxes = GetTaxAndFees(tempList, response.Reservation.NumberInParty);
                            }
                        }
                        reservation.TravelOptions = GetTravelOptions(response.DisplayCart, session.IsReshopChange, request.Application.Id, request.Application.Version.Major);

                        if (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, session.IsReshopChange) && request.IsCatalogOnForTavelerTypes)
                        {
                            string firstLOFDepDate = response.DisplayCart.DisplayTrips[0].Flights[0].DepartDateTime;

                            response.DisplayCart.DisplayTravelers.Sort((x, y) =>
                          ShopStaticUtility.GetTTypeValue(TopHelper.GetAgeByDOB(x.DateOfBirth, firstLOFDepDate), x.PaxTypeCode.ToUpper().Equals("INF")).CompareTo(ShopStaticUtility.GetTTypeValue(TopHelper.GetAgeByDOB(y.DateOfBirth, firstLOFDepDate), y.PaxTypeCode.ToUpper().Equals("INF")))
                         );
                            reservation.ShopReservationInfo2.TravelerTypes = ShopStaticUtility.GetTravelTypesFromShop(response.DisplayCart.DisplayTravelers);
                            reservation.ShopReservationInfo2.displayTravelTypes = ShopStaticUtility.GetDisplayTravelerTypes(response.DisplayCart.DisplayTravelers);
                        }
                        if (_shoppingUtility.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, true) && !session.IsReshopChange)
                        {
                            if (reservation.ShopReservationInfo2 == null)
                                reservation.ShopReservationInfo2 = new ReservationInfo2();
                            reservation.ShopReservationInfo2.IsDisplayCart = _shoppingUtility.IsDisplayCart(session, GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("Android_EnableAwardLiveCart_AppVersion"), _configuration.GetValue<string>("iPhone_EnableAwardLiveCart_AppVersion"))
                                                                                                                        ? "DisplayCartTravelTypesWithAward"
                                                                                                                        : "DisplayCartTravelTypes");
                        }

                        if (string.IsNullOrEmpty(session.EmployeeId))
                        {
                            reservation.FareLock = GetFareLockOptions(response.FareLockResponse, GetFarePrice(response.DisplayCart.DisplayPrices), GetFareCurrency(response.DisplayCart.DisplayPrices), false, GetFareMiles(response.DisplayCart.DisplayPrices), request.Application.Id, request.Application.Version.Major, catalogItems: request.CatalogItems);
                        }

                        reservation.GetALLSavedTravelers = false;

                        if (string.IsNullOrEmpty(session.EmployeeId))
                        {
                            reservation.FareLock = GetFareLockOptions(response.FareLockResponse, GetFarePrice(response.DisplayCart.DisplayPrices), GetFareCurrency(response.DisplayCart.DisplayPrices), false, GetFareMiles(response.DisplayCart.DisplayPrices), request.Application.Id, request.Application.Version.Major, catalogItems: request.CatalogItems);
                        }
                        reservation.AwardTravel = false;

                        if (!reservation.AwardTravel)
                        {
                            reservation.LMXFlights = PopulateLMX(response.CartId, null, session.SessionId, request.Application.Id, request.DeviceId, request.Application.Version.Major, true, response.DisplayCart.DisplayTrips);
                            reservation.IneligibleToEarnCreditMessage = _configuration.GetValue<string>("IneligibleToEarnCreditMessage");
                            reservation.OaIneligibleToEarnCreditMessage = _configuration.GetValue<string>("OaIneligibleToEarnCreditMessage");
                        }
                        reservation.IsCubaTravel = IsCubaTravelTrip(reservation);
                        if (reservation.IsCubaTravel)
                        {
                            MobileCMSContentRequest mobMobileCMSContentRequest = new MobileCMSContentRequest();
                            mobMobileCMSContentRequest.Application = request.Application;
                            mobMobileCMSContentRequest.Token = session.Token;
                            reservation.CubaTravelInfo = await _travelerCSL.GetCubaTravelResons(mobMobileCMSContentRequest);
                            reservation.CubaTravelInfo.CubaTravelTitles = await (new MPDynamoDB(_configuration, _dynamoDBService, _legalDocumentsForTitlesService, _headers)).GetMPPINPWDTitleMessages(new List<string> { "CUBA_TRAVEL_CONTENT" });
                        }
                        if (_shoppingUtility.EnableBoeingDisclaimer(session.IsReshopChange) && _shoppingUtility.IsBoeingDisclaimer(response.DisplayCart.DisplayTrips))
                        {
                            if (reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                            if (!reservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.BOEING737WARNING.ToString()))
                            {
                                reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetBoeingDisclaimer());

                                reservation.ShopReservationInfo2.InfoWarningMessages = reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                            }
                        }

                        reservation.PKDispenserPublicKey = await _pKDispenserPublicKey.GetCachedOrNewpkDispenserPublicKey(request.Application.Id, request.Application.Version.Major, request.DeviceId, session.SessionId, session.Token, session.CatalogItems);

                        segments = CommonMethods.FlattenSegmentsForSeatDisplay(response.Reservation);

                        if (_shoppingUtility.EnableCovidTestFlightShopping(request.Application.Id, request.Application.Version.Major, session.IsReshopChange))
                        {
                            ShopStaticUtility.AssignCovidTestIndicator(reservation);
                        }

                        var persistedShopBookingDetailsResponse = new ShopBookingDetailsResponse
                        {
                            SessionId = session.SessionId,
                            CartId = request.CartId,
                            Reservation = response.Reservation
                        };
                        if (string.IsNullOrEmpty(session.EmployeeId))
                        {
                            persistedShopBookingDetailsResponse.FareLock = response.FareLockResponse;
                        }
                        await _sessionHelperService.SaveSession(persistedShopBookingDetailsResponse, session.SessionId, new List<string> { session.SessionId, persistedShopBookingDetailsResponse.ObjectName }, persistedShopBookingDetailsResponse.ObjectName).ConfigureAwait(false);

                        var shop = new Model.Shopping.ShoppingResponse
                        {
                            SessionId = session.SessionId,
                            CartId = request.CartId,
                            Request = new MOBSHOPShopRequest
                            {
                                AccessCode = "1A7370E9-A532-4376-BD39-41795F01321C",
                                SessionId = session.SessionId,
                                CountryCode = "US",
                                NumberOfAdults = reservation.NumberOfTravelers,
                                PremierStatusLevel = request.PremierStatusLevel,
                                MileagePlusAccountNumber = request.MileagePlusAccountNumber
                            }
                        };
                        await _sessionHelperService.SaveSession(shop, session.SessionId, new List<string> { session.SessionId, shop.ObjectName }, shop.ObjectName).ConfigureAwait(false);
                    }
                    if (await _featureToggles.IsEnableCustomerFacingCartId(request.Application.Id, request.Application.Version.Major, session.CatalogItems).ConfigureAwait(false) && response?.CartRefId > 0)
                    {
                        try
                        {
                            _shoppingUtility.GetCartRefId(response.CartRefId, reservation?.ShopReservationInfo2, lstMessages);
                        }
                        catch { }
                    }
                }
                else
                {
                    if (bookingresponse != null && bookingresponse.Errors != null && bookingresponse.Errors.Count > 0)
                    {
                        var errorMessage = string.Empty;
                        foreach (var error in bookingresponse.Errors)
                        {
                            if (!string.IsNullOrEmpty(error.Message))
                            {
                                if (error.Message.Equals("United Airlines The fare or flight option you have selected is no longer available."))
                                {
                                    errorMessage = errorMessage + _configuration.GetValue<string>("MetaTripExceptionMessage");
                                }
                                else
                                {
                                    errorMessage = errorMessage + " " + error.Message;
                                }
                            }
                        }

                        throw new Exception(errorMessage);
                    }

                    throw new MOBUnitedException("Failed to retrieve booking details.");
                }
            }
            else
            {
                throw new MOBUnitedException("Failed to retrieve booking details.");
            }
            availability.Reservation = reservation;
            return (availability, segments);
        }

        private bool isCubaAirportCodeExist(string origin, string destination, List<string> CubaAirports)
        {
            bool isCubaFight = false;
            if (CubaAirports != null && (CubaAirports.Exists(p => p == origin) || CubaAirports.Exists(p => p == destination)))
            {
                isCubaFight = true;
            }
            return isCubaFight;
        }

        private bool IsCubaTravelTrip(United.Mobile.Model.Shopping.MOBSHOPReservation reservation)
        {
            bool isCubaFight = false;
            string CubaAirports = _configuration.GetValue<string>("CubaAirports");
            List<string> CubaAirportList = CubaAirports.Split('|').ToList();

            if (reservation != null && reservation.Trips != null)
            {
                foreach (MOBSHOPTrip trip in reservation.Trips)
                {
                    isCubaFight = isCubaAirportCodeExist(trip.Origin, trip.Destination, CubaAirportList);

                    foreach (var flight in trip.FlattenedFlights)
                    {
                        foreach (var stopFlights in flight.Flights)
                        {
                            isCubaFight = isCubaAirportCodeExist(stopFlights.Origin, stopFlights.Destination, CubaAirportList);
                            if (!isCubaFight && stopFlights.StopInfos != null)
                            {
                                foreach (var stop in stopFlights.StopInfos)
                                {
                                    isCubaFight = isCubaAirportCodeExist(stop.Origin, stop.Destination, CubaAirportList);
                                }
                                if (isCubaFight)
                                    break;
                            }
                            if (isCubaFight)
                                break;
                        }

                        if (isCubaFight)
                            break;
                    }
                }
            }
            return isCubaFight;
        }

        private List<United.Mobile.Model.Common.MP2015.LmxFlight> PopulateLMX(string cartId, ShoppingResponse persistShop, string sessionId, int appId, string deviceId, string appVersion, bool showMileageDetails, List<United.Services.FlightShopping.Common.DisplayCart.DisplayTrip> flightSegmentCollection)
        {
            List<United.Mobile.Model.Common.MP2015.LmxFlight> lmxFlights = null;

            for (int i = 0; i < flightSegmentCollection.Count; i++)
            {
                if (flightSegmentCollection != null && flightSegmentCollection.Count > 0)
                {
                    if (flightSegmentCollection[i].Flights != null && flightSegmentCollection[i].Flights.Count > 0)
                    {
                        //update amenities for all flights
                        UpdateAmenitiesIndicatorsResponse amenitiesResponse = new UpdateAmenitiesIndicatorsResponse();
                        List<Flight> tempFlights = new List<Flight>(flightSegmentCollection[i].Flights);

                        //we do not want the search to fail if one of these fail...
                        try
                        {

                            Parallel.Invoke(async () =>
                            {
                                bool supressLMX = false;
                                #region //**// LMX Flag For AppID change
                                Session session = new Session();
                                session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);
                                supressLMX = session.SupressLMXForAppID;
                                #endregion
                                if (!supressLMX && showMileageDetails)
                                {
                                    lmxFlights = await GetLmxForRTI(session.Token, cartId, GetFlightHasListForLMX(tempFlights), sessionId, appId, deviceId, appVersion);
                                }

                            }
                                );
                        }
                        catch { };
                    }
                }
            }

            return lmxFlights;
        }

        private async Task<List<United.Mobile.Model.Common.MP2015.LmxFlight>> GetLmxForRTI(string token, string cartId, string hashList, string sessionId, int appId, string deviceId, string appVersion)
        {
            List<United.Mobile.Model.Common.MP2015.LmxFlight> lmxFlights = null;

            if (!string.IsNullOrEmpty(cartId))
            {

                string jsonRequest = "{\"CartId\":\"" + cartId + "\"}";

                if (!string.IsNullOrEmpty(hashList))
                {
                    jsonRequest = "{\"CartId\":\"" + cartId + "\", \"hashList\":[" + hashList + ")}";
                }

                FlightStatus flightStatus = new FlightStatus();



                #region//****Get Call Duration Code - Venkat 03/17/2015*******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

                var response = await _lmxInfo.GetLmxRTIInfo<LmxQuoteResponse>(token, jsonRequest, sessionId);

                #region// 2 = cslStopWatch/***Get Call Duration Code - Venkat 03/17/2015*******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

                //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetLmxForRTI - CSL Call url", "Trace", appId, appVersion, deviceId, url));
                //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetLmxForRTI - CSL Call Request", "Trace", appId, appVersion, deviceId, jsonRequest));
                //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetLmxForRTI - CSL Call Duration", "CSS/CSL-CallDuration", appId, appVersion, deviceId, "CSLGetLmxQuote=" + cslCallTime));
                //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetLmxForRTI - CSL Call Response", "Trace", appId, appVersion, deviceId, jsonResponse));

                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******
                _logger.LogInformation("GetLmxForRTI - CSL Call SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Response {response}", sessionId, appId, appVersion, deviceId, response);

                if (response != null)
                {
                    if (response != null && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success))
                    {
                        if (response.Flights != null && response.Flights.Count > 0)
                        {
                            if (lmxFlights == null)
                            {
                                lmxFlights = new List<United.Mobile.Model.Common.MP2015.LmxFlight>();
                            }
                            CultureInfo ci = null;
                            foreach (var flight in response.Flights)
                            {
                                United.Mobile.Model.Common.MP2015.LmxFlight lmxFlight = new United.Mobile.Model.Common.MP2015.LmxFlight();
                                //lmxFlight.Arrival = new MOBAirport();
                                //lmxFlight.Arrival.Code = flight.Destination;
                                //lmxFlight.Departure = new MOBAirport();
                                //lmxFlight.Departure.Code = flight.Origin;
                                //lmxFlight.FlightNumber = flight.FlightNumber;
                                //lmxFlight.MarketingCarrier = new MOBAirline();
                                lmxFlight.MarketingCarrier.Code = flight.MarketingCarrier;
                                lmxFlight.ScheduledDepartureDateTime = flight.DepartDateTime;


                                if (_configuration.GetValue<string>("LMXPartners").IndexOf(flight.MarketingCarrier) == -1)
                                {
                                    lmxFlight.NonPartnerFlight = true;
                                }

                                if (flight.Products != null && flight.Products.Count > 0)
                                {
                                    lmxFlight.Products = new List<United.Mobile.Model.Common.MP2015.LmxProduct>();
                                    foreach (var product in flight.Products)
                                    {
                                        United.Mobile.Model.Common.MP2015.LmxProduct lmxProduct = new United.Mobile.Model.Common.MP2015.LmxProduct();
                                        lmxProduct.ProductType = product.ProductType;
                                        if (product.LmxLoyaltyTiers != null && product.LmxLoyaltyTiers.Count > 0)
                                        {
                                            lmxProduct.LmxLoyaltyTiers = new List<United.Mobile.Model.Common.MP2015.LmxLoyaltyTier>();
                                            foreach (var loyaltyTier in product.LmxLoyaltyTiers)
                                            {
                                                if (string.IsNullOrEmpty(loyaltyTier.ErrorCode))
                                                {
                                                    United.Mobile.Model.Common.MP2015.LmxLoyaltyTier lmxLoyaltyTier = new United.Mobile.Model.Common.MP2015.LmxLoyaltyTier();
                                                    lmxLoyaltyTier.Description = loyaltyTier.Descr;
                                                    lmxLoyaltyTier.Key = loyaltyTier.Key;
                                                    lmxLoyaltyTier.Level = loyaltyTier.Level;
                                                    if (loyaltyTier.LmxQuotes != null && loyaltyTier.LmxQuotes.Count > 0)
                                                    {
                                                        lmxLoyaltyTier.LmxQuotes = new List<United.Mobile.Model.Common.MP2015.LmxQuote>();
                                                        foreach (var quote in loyaltyTier.LmxQuotes)
                                                        {
                                                            if (ci == null)
                                                                TopHelper.GetCultureInfo(quote.Currency);
                                                            United.Mobile.Model.Common.MP2015.LmxQuote lmxQuote = new United.Mobile.Model.Common.MP2015.LmxQuote();
                                                            lmxQuote.Amount = quote.Amount;
                                                            lmxQuote.Currency = quote.Currency;
                                                            lmxQuote.Description = quote.Descr;
                                                            lmxQuote.Type = quote.Type;
                                                            lmxQuote.DblAmount = Double.Parse(quote.Amount);
                                                            lmxQuote.Currency = _shoppingUtility.GetCurrencySymbol(ci);
                                                            lmxLoyaltyTier.LmxQuotes.Add(lmxQuote);
                                                        }
                                                    }
                                                    lmxProduct.LmxLoyaltyTiers.Add(lmxLoyaltyTier);
                                                }
                                            }
                                        }
                                        lmxFlight.Products.Add(lmxProduct);
                                    }
                                }

                                lmxFlights.Add(lmxFlight);
                            }


                            //logEntries.Add(LogEntry.GetLogEntry<List<MOBLmxFlight>>(sessionId, "GetLmxForRTI - ClientResponse for GetLmxQuote", "Trace", appId, appVersion, deviceId, lmxFlights));

                        }
                    }
                }
            }

            return lmxFlights;
        }

        private double GetFareMiles(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices)
        {
            double bookingPrice = 0;
            foreach (United.Services.FlightShopping.Common.DisplayCart.DisplayPrice price in prices)
            {
                if (price.Type.ToUpper() == "MILES")
                {
                    bookingPrice = (double)price.Amount;
                    break;
                }
            }
            return bookingPrice;
        }

        private string GetFareCurrency(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices)
        {
            string bookingCurrency = "";
            foreach (United.Services.FlightShopping.Common.DisplayCart.DisplayPrice price in prices)
            {
                if (price.Type.ToUpper() == "TOTAL")
                {
                    bookingCurrency = price.Currency;
                    break;
                }
            }
            return bookingCurrency;
        }

        private double GetFarePrice(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices)
        {
            double bookingPrice = 0;
            foreach (United.Services.FlightShopping.Common.DisplayCart.DisplayPrice price in prices)
            {
                if (price.Type.ToUpper() == "TOTAL")
                {
                    bookingPrice = (double)price.Amount;
                    break;
                }
            }
            return bookingPrice;
        }

        private FareLock GetFareLockOptions(United.Service.Presentation.ProductResponseModel.ProductOffer cslFareLock, double flightPrice, string currency, bool isAward, double miles, int appId, string appVersion, List<MOBItem> catalogItems = null)
        {
            FareLock shopFareLock = new FareLock();

            if (cslFareLock != null && cslFareLock.Offers != null && cslFareLock.Offers.Count > 0)
            {
                /// This code added to farelock with implementation of 14-day option to support old clients
                bool isEnableFarelockForOldClientsWithTwoOptions = _configuration.GetValue<bool>("IsEnableFarelocVersionForOldClients");
                bool isEnableFarelockOption = isEnableFarelockForOldClientsWithTwoOptions && !appId.IsNullOrEmpty() && !string.IsNullOrEmpty(appVersion) ? GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidFarelockOldClientVersion", "iPhoneFarelockOldClientVersion", "", "", true, _configuration) : false;
                string farelockCodeFor7Day = isEnableFarelockForOldClientsWithTwoOptions ? _configuration.GetValue<string>("FarelockCodeFor7DayOption") : string.Empty;
                CultureInfo ci = null;
                double lowest = 999999.9;
                string prodAmountDisplay = string.Empty;

                foreach (United.Service.Presentation.ProductResponseModel.Offer offer in cslFareLock.Offers)
                {
                    if (offer.ProductInformation != null && offer.ProductInformation.ProductDetails != null && offer.ProductInformation.ProductDetails.Count > 0)
                    {
                        shopFareLock.FareLockProducts = new List<FareLockProduct>();
                        foreach (United.Service.Presentation.ProductResponseModel.ProductDetail prodDetail in offer.ProductInformation.ProductDetails)
                        {
                            foreach (United.Service.Presentation.ProductModel.SubProduct subProduct in prodDetail.Product.SubProducts)
                            {
                                FareLockProduct flProd = new FareLockProduct();
                                foreach (United.Service.Presentation.ProductModel.ProductPriceOption prodPrice in subProduct.Prices)
                                {
                                    if (ci == null)
                                    {
                                        ci = TopHelper.GetCultureInfo(prodPrice.PaymentOptions[0].PriceComponents[0].Price.Totals[0].Currency.Code);
                                    }

                                    flProd.FareLockProductAmount = prodPrice.PaymentOptions[0].PriceComponents[0].Price.Totals[0].Amount;
                                    if (_shoppingUtility.IsEnableFareLockAmoutDisplayPerPerson(appId, appVersion, catalogItems))
                                    {
                                        prodAmountDisplay = (prodPrice.PaymentOptions[0].PriceComponents[0].Price?.Adjustments == null)
                                                ? flProd.FareLockProductAmount.ToString() : prodPrice.PaymentOptions[0].PriceComponents[0].Price?.Adjustments?.Where(a => a.Designator == _configuration.GetValue<string>("FarelockDesignatorKey"))?.FirstOrDefault()?.Result.ToString();
                                        flProd.FareLockProductAmountDisplayText = (TopHelper.FormatAmountForDisplay(prodAmountDisplay, ci, false) + _configuration.GetValue<string>("FareLockPerPersonText"));
                                    }
                                    else
                                    {
                                        flProd.FareLockProductAmountDisplayText = TopHelper.FormatAmountForDisplay(flProd.FareLockProductAmount.ToString(), ci, false);
                                    }
                                    //Retrieving the ProductId and ProductCode inorder for the client to send it back to us when calling the RegisterFairLock.
                                    //Note: Since we are using "Shopping/cart/RegisterOffer" Instead of "flightShopping/RegisterFairlock".Old CSL doesn't require productCode.But "Shopping/Cart/RegisterOffer" productId/productCode is mandate.
                                    flProd.ProductId = prodPrice.ID;

                                    if (lowest == -1 || flProd.FareLockProductAmount < lowest)
                                        lowest = flProd.FareLockProductAmount;
                                }
                                flProd.ProductCode = prodDetail.Product.Code;
                                flProd.FareLockProductCode = subProduct.Code;
                                if (_shoppingUtility.IsAllFareLockOptionEnabled(appId, appVersion, catalogItems))
                                    flProd.FareLockProductTitle = "Hold for " + subProduct.Features[0].Value + " " + subProduct.Features[0].Name;
                                else
                                    flProd.FareLockProductTitle = subProduct.Features[0].Value + " " + subProduct.Features[0].Name;

                                if (isEnableFarelockForOldClientsWithTwoOptions)
                                {
                                    if (!isEnableFarelockOption && prodDetail.Product.SubProducts.Count > 2)
                                    {
                                        if (!subProduct.Code.Equals(farelockCodeFor7Day))
                                            shopFareLock.FareLockProducts.Add(flProd);
                                    }
                                    else
                                    {
                                        shopFareLock.FareLockProducts.Add(flProd);
                                    }
                                }
                                else
                                {
                                    shopFareLock.FareLockProducts.Add(flProd);
                                }
                            }
                        }

                        shopFareLock.FareLockDescriptionText = offer.ProductInformation.ProductDetails[0].Product.Description != null ? offer.ProductInformation.ProductDetails[0].Product.Description : "Farelock";
                        shopFareLock.FareLockHoldButtonText = _configuration.GetValue<string>("FareLockHoldButtonText"); //"Hold fare";
                        shopFareLock.FareLockTextTop = _configuration.GetValue<string>("FarelockTextTop");
                        shopFareLock.FareLockTextBottom = _configuration.GetValue<string>("FarelockTextBottom");
                        shopFareLock.FareLockMinAmount = lowest;
                        shopFareLock.FareLockDisplayMinAmount = TopHelper.FormatAmountForDisplay(lowest.ToString(), ci, true);
                        shopFareLock.FareLockTermsAndConditions = new List<string>();
                        shopFareLock.FareLockPurchaseButtonAmount = flightPrice;

                        if (isAward)
                        {
                            shopFareLock.FareLockPurchaseButtonAmountDisplayText = ShopStaticUtility.FormatAwardAmountForDisplay(miles.ToString(), true) + " + " + TopHelper.FormatAmountForDisplay(flightPrice.ToString(), ci, false);
                        }
                        else
                        {
                            shopFareLock.FareLockPurchaseButtonAmountDisplayText = TopHelper.FormatAmountForDisplay(flightPrice.ToString(), ci, false);
                        }
                        shopFareLock.FareLockPurchaseButtonText = _configuration.GetValue<string>("FareLockPurchaseButtonText");//"Purchase now";
                        shopFareLock.FareLockTitleText = _configuration.GetValue<string>("FareLockTitleText");//"FareLock";
                    }
                }
            }
            if (_shoppingUtility.IsAllFareLockOptionEnabled(appId, appVersion, catalogItems) && shopFareLock?.FareLockProducts?.Count > 0)
            {
                FareLockProduct flProd = new FareLockProduct();
                flProd.FareLockProductTitle = _configuration.GetValue<string>("FareLockTextContinueWithOutFareLock");
                //  flProd.ProductCode , flProd.ProductId ,flProd.FareLockProductAmount should not be passed anytime it will break UI
                shopFareLock.FareLockProducts.Insert(0, flProd);
            }

            // }
            return shopFareLock;
        }

        private List<Model.Shopping.TravelOption> GetTravelOptions(DisplayCart displayCart, bool isReshop, int appID, string appVersion)
        {
            List<Model.Shopping.TravelOption> travelOptions = null;
            if (displayCart != null && displayCart.TravelOptions != null && displayCart.TravelOptions.Count > 0)
            {
                CultureInfo ci = null;
                travelOptions = new List<Model.Shopping.TravelOption>();
                bool addTripInsuranceInTravelOption =
                    !_configuration.GetValue<bool>("ShowTripInsuranceBookingSwitch")
                    && Convert.ToBoolean(_configuration.GetValue<string>("ShowTripInsuranceSwitch") ?? "false");
                foreach (var anOption in displayCart.TravelOptions)
                {
                    //wade - added check for farelock as we were bypassing it
                    if (!anOption.Type.Equals("Premium Access") && !anOption.Key.Trim().ToUpper().Contains("FARELOCK") && !(addTripInsuranceInTravelOption && anOption.Key.Trim().ToUpper().Contains("TPI"))
                    && !(_shoppingUtility.EnableEPlusAncillary(appID, appVersion, isReshop) && anOption.Key.Trim().ToUpper().Contains("EFS")))
                    {
                        continue;
                    }
                    if (ci == null)
                    {
                        ci = TopHelper.GetCultureInfo(anOption.Currency);
                    }

                    Model.Shopping.TravelOption travelOption = new Model.Shopping.TravelOption();
                    travelOption.Amount = (double)anOption.Amount;

                    travelOption.DisplayAmount = TopHelper.FormatAmountForDisplay(anOption.Amount, ci, false);

                    //??
                    if (anOption.Key.Trim().ToUpper().Contains("FARELOCK") || (addTripInsuranceInTravelOption && anOption.Key.Trim().ToUpper().Contains("TPI")))
                        travelOption.DisplayButtonAmount = TopHelper.FormatAmountForDisplay(anOption.Amount, ci, false);
                    else
                        travelOption.DisplayButtonAmount = TopHelper.FormatAmountForDisplay(anOption.Amount, ci, true);

                    travelOption.CurrencyCode = anOption.Currency;
                    travelOption.Deleted = anOption.Deleted;
                    travelOption.Description = anOption.Description;
                    travelOption.Key = anOption.Key;
                    travelOption.ProductId = anOption.ProductId;
                    travelOption.SubItems = GetTravelOptionSubItems(anOption.SubItems);
                    if (_shoppingUtility.EnableEPlusAncillary(appID, appVersion, isReshop) && anOption.SubItems != null && anOption.SubItems.Count > 0)
                    {
                        travelOption.BundleCode = GetTravelOptionEplusAncillary(anOption.SubItems, travelOption.BundleCode);
                        GetTravelOptionAncillaryDescription(anOption.SubItems, travelOption, displayCart);
                    }
                    if (!string.IsNullOrEmpty(anOption.Type))
                    {
                        travelOption.Type = anOption.Type.Equals("Premium Access") ? "Premier Access" : anOption.Type;
                    }
                    travelOptions.Add(travelOption);
                }
            }

            return travelOptions;
        }
        private void GetTravelOptionAncillaryDescription(SubitemsCollection subitemsCollection, Model.Shopping.TravelOption travelOption, DisplayCart displayCart)
        {
            List<AncillaryDescriptionItem> ancillaryDesciptionItems = new List<AncillaryDescriptionItem>();
            CultureInfo ci = null;

            if (subitemsCollection.Any(t => t?.Type?.Trim().ToUpper() == "EFS"))
            {
                var trips = subitemsCollection.GroupBy(x => x.TripIndex);
                foreach (var trip in trips)
                {
                    if (trip != null)
                    {
                        decimal ancillaryAmount = 0;
                        foreach (var item in trip)
                        {
                            ancillaryAmount += item.Amount;
                            if (ci == null)
                            {
                                ci = TopHelper.GetCultureInfo(item.Currency);
                            }
                        }

                        AncillaryDescriptionItem objeplus = new AncillaryDescriptionItem();
                        objeplus.DisplayValue = TopHelper.FormatAmountForDisplay(ancillaryAmount, ci, false);
                        objeplus.SubTitle = displayCart.DisplayTravelers?.Count.ToString() + (displayCart.DisplayTravelers?.Count > 1 ? " travelers" : " traveler");
                        var displayTrip = displayCart.DisplayTrips?.FirstOrDefault(s => s.Index == Convert.ToInt32(trip.FirstOrDefault().TripIndex));
                        if (displayTrip != null)
                        {
                            objeplus.Title = displayTrip.Origin + " - " + displayTrip.Destination;
                        }
                        ancillaryDesciptionItems.Add(objeplus);
                    }
                }

                travelOption.BundleOfferTitle = "Economy Plus®";
                travelOption.BundleOfferSubtitle = "Included with your fare";
                travelOption.AncillaryDescriptionItems = ancillaryDesciptionItems;
            }
        }

        private List<ShopBundleEplus> GetTravelOptionEplusAncillary(SubitemsCollection subitemsCollection, List<ShopBundleEplus> bundlecode)
        {
            if (bundlecode == null || bundlecode.Count == 0)
            {
                bundlecode = new List<ShopBundleEplus>();
            }

            foreach (var item in subitemsCollection)
            {
                if (item?.Type?.Trim().ToUpper() == "EFS")
                {
                    ShopBundleEplus objeplus = new ShopBundleEplus();
                    objeplus.ProductKey = item.Type;
                    objeplus.AssociatedTripIndex = Convert.ToInt32(item.TripIndex);
                    bundlecode.Add(objeplus);
                }
            }

            return bundlecode;
        }

        private List<TravelOptionSubItem> GetTravelOptionSubItems(SubitemsCollection subitemsCollection)
        {
            List<TravelOptionSubItem> subItems = null;

            if (subitemsCollection != null && subitemsCollection.Count > 0)
            {
                CultureInfo ci = null;
                subItems = new List<TravelOptionSubItem>();

                foreach (var item in subitemsCollection)
                {
                    if (ci == null)
                    {
                        ci = TopHelper.GetCultureInfo(item.Currency);
                    }

                    TravelOptionSubItem subItem = new TravelOptionSubItem();
                    subItem.Amount = (double)item.Amount;
                    subItem.DisplayAmount = TopHelper.FormatAmountForDisplay(item.Amount, ci, false);
                    subItem.CurrencyCode = item.Currency;
                    subItem.Description = item.Description;
                    subItem.Key = item.Key;
                    subItem.ProductId = item.Type;
                    subItem.Value = item.Value;


                    //    subItem.BundleCode = new List<MobShopBundleEplus>();
                    //    foreach (var v in item.Product.SubProducts)
                    //    {

                    //      MobShopBundleEplus objeplus = new MobShopBundleEplus();
                    //       if (v.Code == "EPU")
                    //        {
                    //            objeplus.ProductKey = item.Product.ProductType;
                    //            objeplus.SegmentName = item.Product.PromoDescription;
                    //        }
                    //        subItem.BundleCode.Add(objeplus);
                    //    }
                    subItems.Add(subItem);
                }

            }

            return subItems;
        }

        private List<Model.Shopping.MOBSHOPTax> GetTaxAndFees(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices, int numPax, bool isReshopChange = false)
        {
            List<Model.Shopping.MOBSHOPTax> taxsAndFees = new List<Model.Shopping.MOBSHOPTax>();
            CultureInfo ci = null;
            decimal taxTotal = 0.0M;
            bool isEnableOmniCartMVP2Changes = _configuration.GetValue<bool>("EnableOmniCartMVP2Changes");

            foreach (var price in prices)
            {
                if (price.SubItems != null && price.SubItems.Count > 0
                    && price.Type.Trim().ToUpper() != "RBF" // Added by Hasnan - # 167553 - 10/04/2017
                   )
                {
                    foreach (var subItem in price.SubItems)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(subItem.Currency);
                        }

                        Model.Shopping.MOBSHOPTax taxNfee = new Model.Shopping.MOBSHOPTax();
                        taxNfee.CurrencyCode = subItem.Currency;
                        taxNfee.Amount = subItem.Amount;
                        taxNfee.DisplayAmount = TopHelper.FormatAmountForDisplay(taxNfee.Amount, ci, false);
                        taxNfee.TaxCode = subItem.Type;
                        taxNfee.TaxCodeDescription = subItem.Description;
                        taxsAndFees.Add(taxNfee);

                        taxTotal += taxNfee.Amount;
                    }

                }
                else if (price.Type.Trim().ToUpper() == "RBF") //Reward Booking Fee
                {
                    if (ci == null)
                    {
                        ci = TopHelper.GetCultureInfo(price.Currency);
                    }
                    Model.Shopping.MOBSHOPTax taxNfee = new Model.Shopping.MOBSHOPTax();
                    taxNfee.CurrencyCode = price.Currency;
                    taxNfee.Amount = price.Amount / numPax;
                    taxNfee.DisplayAmount = TopHelper.FormatAmountForDisplay(taxNfee.Amount, ci, false);
                    taxNfee.TaxCode = price.Type;
                    taxNfee.TaxCodeDescription = price.Description;
                    taxsAndFees.Add(taxNfee);

                    taxTotal += taxNfee.Amount;
                }
            }

            if (taxsAndFees != null && taxsAndFees.Count > 0)
            {
                //add new label as first item for UI
                Model.Shopping.MOBSHOPTax tnf = new Model.Shopping.MOBSHOPTax();
                tnf.CurrencyCode = taxsAndFees[0].CurrencyCode;
                tnf.Amount = taxTotal;
                tnf.DisplayAmount = TopHelper.FormatAmountForDisplay(tnf.Amount, ci, false);
                tnf.TaxCode = "PERPERSONTAX";
                tnf.TaxCodeDescription = string.Format("{0} adult{1}: {2}{3}", numPax, numPax > 1 ? "s" : "", tnf.DisplayAmount, isEnableOmniCartMVP2Changes ? "/person" : " per person");
                taxsAndFees.Insert(0, tnf);

                //add grand total for all taxes
                Model.Shopping.MOBSHOPTax tnfTotal = new Model.Shopping.MOBSHOPTax();
                tnfTotal.CurrencyCode = taxsAndFees[0].CurrencyCode;
                tnfTotal.Amount = taxTotal * numPax;
                tnfTotal.DisplayAmount = TopHelper.FormatAmountForDisplay(tnfTotal.Amount, ci, false);
                tnfTotal.TaxCode = "TOTALTAX";
                tnfTotal.TaxCodeDescription = "Taxes and Fees Total";
                taxsAndFees.Add(tnfTotal);

            }

            return taxsAndFees;
        }

        public async Task<string> GetFareClassAtShoppingRequestFromPersist(string sessionID)
        {
            #region
            string fareClass = string.Empty;
            try
            {
                ShoppingResponse shop = new ShoppingResponse();
                shop = await _sessionHelperService.GetSession<ShoppingResponse>(sessionID, shop.ObjectName, new List<string> { sessionID, shop.ObjectName }).ConfigureAwait(false);
                fareClass = !string.IsNullOrEmpty(shop.Request.FareClass) ? shop.Request.FareClass : string.Empty;
            }
            catch { }
            return fareClass;
            #endregion
        }

        private async System.Threading.Tasks.Task AddElfUpgradeMessageForMetasearch(bool isUpgradedFromElf, United.Mobile.Model.Shopping.MOBSHOPReservation reservation)
        {
            if (!isUpgradedFromElf) return;

            var messagesFromDb = await (new MPDynamoDB(_configuration, _dynamoDBService, null, _headers)).GetMPPINPWDTitleMessages(new List<string> { "ELF_METASEARCH_UPGRADE_MESSAGES" });
            reservation.ElfUpgradeMessagesForMetaSearch = messagesFromDb[0].CurrentValue;
        }

        private List<Model.Shopping.MOBSHOPPrice> AdjustTotal(List<Model.Shopping.MOBSHOPPrice> prices)
        {
            CultureInfo ci = null;

            List<Model.Shopping.MOBSHOPPrice> newPrices = prices;
            double fee = 0;
            foreach (Model.Shopping.MOBSHOPPrice p in newPrices)
            {
                if (ci == null)
                {
                    ci = TopHelper.GetCultureInfo(p.CurrencyCode);
                }

                if (fee == 0)
                {
                    foreach (Model.Shopping.MOBSHOPPrice q in newPrices)
                    {
                        if (q.DisplayType.Trim().ToUpper() == "RBF")
                        {
                            fee = q.Value;
                            break;
                        }
                    }
                }
                if (p.DisplayType.Trim().ToUpper() == "REFUNDPRICE" && p.Value < 0)
                {
                    p.Value *= -1;
                }
                if ((fee > 0 && p.DisplayType.Trim().ToUpper() == "TOTAL") || p.DisplayType.Trim().ToUpper() == "REFUNDPRICE")
                {
                    //update total
                    p.Value += fee;
                    p.DisplayValue = string.Format("{0:#,0.00}", p.Value);
                    p.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(p.Value.ToString(), ci, false); ;// string.Format("{0:c}", price.Amount);
                }
            }
            return newPrices;
        }

        public List<MOBSHOPPrice> GetPrices(List<DisplayPrice> prices, bool isAwardBooking, string sessionId, bool isReshopChange = false,
            int appId = 0, string appVersion = "", List<MOBItem> catalogItems = null, FlightReservationResponse shopBookingDetailsResponse = null)
        {
            List<Model.Shopping.MOBSHOPPrice> bookingPrices = new List<Model.Shopping.MOBSHOPPrice>();
            bool isEnableOmniCartMVP2Changes = _configuration.GetValue<bool>("EnableOmniCartMVP2Changes");
            CultureInfo ci = null;
            foreach (var price in prices)
            {
                if (ci == null)
                {
                    ci = TopHelper.GetCultureInfo(price.Currency);
                }

                Model.Shopping.MOBSHOPPrice bookingPrice = new Model.Shopping.MOBSHOPPrice();
                bookingPrice.CurrencyCode = price.Currency;
                bookingPrice.DisplayType = price.Type;
                bookingPrice.Status = price.Status;
                bookingPrice.Waived = price.Waived;
                bookingPrice.DisplayValue = string.Format("{0:#,0.00}", price.Amount);

                if (!isReshopChange)
                {
                    if (!string.IsNullOrEmpty(bookingPrice.DisplayType) && bookingPrice.DisplayType.Equals("MILES") && isAwardBooking && !string.IsNullOrEmpty(sessionId))
                    {
                        ValidateAwardMileageBalance(sessionId, price.Amount, appId, appVersion, catalogItems);
                    }
                }

                double tempDouble = 0;
                double.TryParse(price.Amount.ToString(), out tempDouble);
                bookingPrice.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);

                if (price.Currency.ToUpper() == "MIL")
                {
                    bookingPrice.FormattedDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(price.Amount.ToString(), false);
                }
                else
                {
                    if (price.Amount < 0
                        && (string.Equals("TaxDifference", price.Type, StringComparison.OrdinalIgnoreCase)
                        || string.Equals("FareDifference", price.Type, StringComparison.OrdinalIgnoreCase)))
                        bookingPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(price.Amount * -1, ci, false);
                    else
                        bookingPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(price.Amount, ci, false);
                }
                if (_shoppingUtility.EnableYADesc(isReshopChange) && price.PricingPaxType != null && price.PricingPaxType.ToUpper().Equals("UAY"))   //If Young adult account
                {
                    bookingPrice.PaxTypeDescription = $"{price.Count} {"young adult (18-23)"}".ToLower(); //string.Format("{0} {1}: {2} per person", price?.Count, "young adult (18-23)", price?.Amount);
                    if (isEnableOmniCartMVP2Changes)
                        bookingPrice.PaxTypeDescription = bookingPrice?.PaxTypeDescription.Replace(" per ", "/");
                }
                else
                    bookingPrice.PaxTypeDescription = $"{price.Count} {price.Description}".ToLower();

                UpdatePriceTypeDescForBuyMiles(appId, appVersion, catalogItems, shopBookingDetailsResponse, bookingPrice);
                bookingPrices.Add(bookingPrice);
            }

            return bookingPrices;
        }

        public void UpdatePriceTypeDescForBuyMiles(int appId, string appVersion, List<MOBItem> catalogItems, FlightReservationResponse shopBookingDetailsResponse, MOBSHOPPrice bookingPrice)
        {
            // if BUY MILES flow and PRice type is MPF change the description for UI display
            if (_shoppingUtility.IsBuyMilesFeatureEnabled(appId, appVersion, isNotSelectTripCall: true))
            {
                string additionalMiles = "Additional {0} miles";
                string formattedMiles = String.Format("{0:n0}", shopBookingDetailsResponse?.DisplayCart?.DisplayFees?.FirstOrDefault()?.SubItems?.Where(a => a.Key == "PurchaseMiles")?.FirstOrDefault()?.Value);
                if (bookingPrice?.DisplayType == "MPF")
                    bookingPrice.PriceTypeDescription = string.Format(additionalMiles, formattedMiles);
            }
        }

        private async System.Threading.Tasks.Task ValidateAwardMileageBalance(string sessionId, decimal milesNeeded, int appId = 0, string appVersion = "", List<MOBItem> catalogItems = null)
        {
            CSLShopRequest shopRequest = new CSLShopRequest();
            shopRequest = await _sessionHelperService.GetSession<CSLShopRequest>(sessionId, shopRequest.ObjectName, new List<string> { sessionId, shopRequest.ObjectName });
            if (shopRequest != null && shopRequest.ShopRequest != null && shopRequest.ShopRequest.AwardTravel && shopRequest.ShopRequest.LoyaltyPerson != null && shopRequest.ShopRequest.LoyaltyPerson.AccountBalances != null)
            {
                if (shopRequest.ShopRequest.LoyaltyPerson.AccountBalances[0] != null && shopRequest.ShopRequest.LoyaltyPerson.AccountBalances[0].Balance < milesNeeded)
                {
                    if (_shoppingUtility.IsBuyMilesFeatureEnabled(appId, appVersion, catalogItems) == false)
                        throw new MOBUnitedException(_configuration.GetValue<string>("NoEnoughMilesForAwardBooking"));
                }
            }
        }

        private UpsellProduct GetRequestedProduct(FlightReservationResponse response)
        {
            if (response == null) return null;

            var product = response.DisplayCart.DisplayTrips[0].Flights[0].Products.FirstOrDefault();
            var totalPriceItem = response.DisplayCart.DisplayPrices.FirstOrDefault(p => p.Type.ToUpper().Equals("TOTAL"));
            var totalPrice = totalPriceItem != null ? totalPriceItem.Amount : 0;

            if (product == null)
                return null;

            var isBeLite = _shoppingUtility.IsIBeLiteFare(product);
            var iBeLiteLongCabinText = _configuration.GetValue<string>("IBELiteShortProductName");
            var requestedProduct = new UpsellProduct
            {
                BookingCode = product.BookingCode,
                SolutionId = product.SolutionId,
                CabinType = product.CabinType,
                LongCabin = isBeLite ? iBeLiteLongCabinText : "Basic Economy\n(most restricted)",
                LastSolutionId = response.LastBBXSolutionSetId,
                ProductSubtype = "requestedProduct",
                ProductType = isBeLite ? iBeLiteLongCabinText : "Basic Economy",
                TotalPrice = totalPrice
            };

            if (totalPrice != 0)
                requestedProduct.Prices.Add(totalPrice.ToString("C2", CultureInfo.CurrentCulture));

            return requestedProduct;
        }

        private List<UpsellProduct> GetMetaShopUpsellsAndRequestedProduct(FlightReservationResponse response)
        {
            var upsellList = new List<UpsellProduct>();
            if (null != response && null != response.Upsells && null != response.DisplayCart)
            {
                var upsellProduct = GetUpsellProduct(response);
                var requestedProduct = GetRequestedProduct(response);

                if (requestedProduct != null) upsellList.Add(requestedProduct);
                if (upsellProduct != null) upsellList.Add(upsellProduct);
            }
            return upsellList;
        }

        private UpsellProduct GetUpsellProduct(FlightReservationResponse response)
        {
            var product = GetProductWithLowestPrice(response.Upsells);

            if (product == null)
                return null;
            var expectedPricingTypes = new[] { "TOTAL", "FARE" };
            var totalPriceItem = product.Prices.FirstOrDefault(p => expectedPricingTypes.Contains(p.PricingType.ToUpper()));
            var totalPrice = totalPriceItem != null ? totalPriceItem.Amount : 0;

            var upsellProduct = new UpsellProduct
            {
                BookingCode = product.BookingCode,
                SolutionId = product.SolutionId,
                CabinType = product.CabinType,
                LongCabin = "Economy",
                LastSolutionId = response.LastBBXSolutionSetId,
                ProductSubtype = product.ProductSubtype,
                ProductType = "Economy",
                TotalPrice = totalPrice
            };

            foreach (var price in product.Prices)
            {
                upsellProduct.Prices.Add(price.Amount.ToString("C2", CultureInfo.CurrentCulture));
            }
            return upsellProduct;
        }
        private async Task<string> GetGMTTime(string localTime, string airportCode, string sessionId)
        {
            var gmtTime = await _gMTConversionService.GETGMTTime(localTime, airportCode, sessionId);
            if (!String.IsNullOrEmpty(gmtTime))
            {
                DateTime getDateTime;
                DateTime.TryParse(gmtTime, out getDateTime);
                return getDateTime.ToString("MM/dd/yyyy hh:mm tt");
            }
            return localTime;
        }
        private Product GetProductWithLowestPrice(IEnumerable<Product> products)
        {
            if (products == null)
                return null;

            var productsAndPrices = products
                .Where(product => product.CabinType == "COACH")
                .SelectMany(
                    product => product.Prices,
                    (product, pricingItem) => new { Product = product, Prices = pricingItem });

            var productWithLowestPrice = productsAndPrices.OrderBy(arg => arg.Prices.Amount).FirstOrDefault();

            return productWithLowestPrice == null ? null : productWithLowestPrice.Product;
        }

        private async Task<(MOBSHOPAvailability availability, List<ReservationFlightSegment> segments)> AutoUpsellElfV2(Session session, MetaSelectTripRequest request, List<ReservationFlightSegment> segments, FlightReservationResponse response)
        {
            var newRequest = BuildNewUpsellRequest(response, request);
            return await GetMetaShopBookingDetailsV2(session, newRequest, segments, true);
        }

        private MetaSelectTripRequest BuildNewUpsellRequest(FlightReservationResponse response, MetaSelectTripRequest oldRequest)
        {
            var upsellProduct = GetUpsellProduct(response);
            var newRequest = oldRequest.CloneDeep();

            newRequest.MedaSessionId = response.SessionId;
            newRequest.CartId = response.CartId;
            newRequest.RequeryForUpsell = true;
            newRequest.BbxCellId = upsellProduct.SolutionId;
            newRequest.BbxSolutionId = upsellProduct.LastSolutionId;

            return newRequest;
        }

        private bool PricesAreInverted(FlightReservationResponse response)
        {
            var selectedProduct = GetRequestedProduct(response);
            var upsellProduct = GetUpsellProduct(response);
            return selectedProduct != null && upsellProduct != null && selectedProduct.TotalPrice >= upsellProduct.TotalPrice;
        }

        private void ThrowMessageIfAnyPaxTypeIsNotADT(DisplayCart displayCart)
        {
            var message = _configuration.GetValue<string>("MetaPathMessageToRestrictifAllPaxtypeIsNotADT");

            if (string.IsNullOrEmpty(message))
                return;

            if (displayCart != null && displayCart.DisplayTravelers != null && displayCart.DisplayTravelers.Count > 0 && displayCart.DisplayTravelers.Any(t => (t != null && t.PaxTypeCode != null && t.PaxTypeCode.ToUpper().Trim() != "ADT")))
                throw new MOBUnitedException(message);
        }

        private void AssignMissingPropertiesfromRegisterFlightsResponse(FlightReservationResponse flightReserationResponse, FlightReservationResponse registerFlightsResponse)
        {
            if (!registerFlightsResponse.IsNullOrEmpty())
            {
                registerFlightsResponse.FareLockResponse = flightReserationResponse?.FareLockResponse;
                registerFlightsResponse.Upsells = flightReserationResponse?.Upsells;
                registerFlightsResponse.LastBBXSolutionSetId = flightReserationResponse?.LastBBXSolutionSetId;
            }
        }

        private async Task<FlightReservationResponse> RegisterFlights(FlightReservationResponse flightReservationResponse, Session session, MOBRequest request)
        {
            string flow = session.IsNullOrEmpty() && session.IsReshopChange ? FlowType.RESHOP.ToString() : FlowType.BOOKING.ToString();
            var registerFlightRequest = BuildRegisterFlightRequest(flightReservationResponse, flow, request);
            if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == TravelType.TPBooking.ToString())
            {
                var cslShopRequest = _sessionHelperService.GetSession<CSLShopRequest>(session.SessionId, new CSLShopRequest().ObjectName, new List<string> { session.SessionId, new CSLShopRequest().ObjectName })?.Result.ShopRequest;

                registerFlightRequest.TravelPlanId = cslShopRequest?.TravelPlanId;
                registerFlightRequest.TravelPlanCartId = cslShopRequest?.TravelPlanCartId;
            }
            string jsonRequest = JsonConvert.SerializeObject(registerFlightRequest);
            FlightReservationResponse flightresponse = new FlightReservationResponse();
            string actionName = "RegisterFlights";

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cSLCallDurationstopwatch1;
            cSLCallDurationstopwatch1 = new Stopwatch();
            cSLCallDurationstopwatch1.Reset();
            cSLCallDurationstopwatch1.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            string token = string.IsNullOrEmpty(session.Token
                ) ? _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration).Result : session.Token;

            var response = await _shoppingCartService.GetShoppingCartInfo<FlightReservationResponse>(token, actionName, jsonRequest, session.SessionId);

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cSLCallDurationstopwatch1.IsRunning)
            {
                cSLCallDurationstopwatch1.Stop();
            }
            #endregion/***Get Call Duration Code - Venkat 03/17/2015*******

            if (response != null)
            {

                if (!(response != null && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success) && response.DisplayCart != null && response.Reservation != null))
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.Message;
                        }

                        throw new System.Exception(errorMessage);
                    }
                }
                if ((_shoppingUtility.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, true)
                     || EnableAdvanceSearchCouponBooking(request.Application.Id, request.Application.Version.Major))
                     && flow == FlowType.BOOKING.ToString())
                {
                    var persistShoppingCart = new MOBShoppingCart();
                    persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(session.SessionId, persistShoppingCart.ObjectName, new List<string> { session.SessionId, persistShoppingCart.ObjectName });
                    if (persistShoppingCart == null)
                    {
                        persistShoppingCart = new MOBShoppingCart();
                    }
                    persistShoppingCart.Products = await _productInfoHelper.ConfirmationPageProductInfo(response, false, request.Application, null, flow);
                    double price = _shoppingUtility.GetGrandTotalPriceForShoppingCart(false, response, false, flow);
                    persistShoppingCart.TotalPrice = String.Format("{0:0.00}", price);
                    persistShoppingCart.DisplayTotalPrice = Decimal.Parse(price.ToString().Trim()).ToString("c", CultureInfo.CreateSpecificCulture("en-us"));
                    persistShoppingCart.PromoCodeDetails = AddAFSPromoCodeDetails(response.DisplayCart);
                    await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, session.SessionId, new List<string> { session.SessionId, persistShoppingCart.ObjectName }, persistShoppingCart.ObjectName).ConfigureAwait(false);

                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            return response;

        }
        public MOBPromoCodeDetails AddAFSPromoCodeDetails(DisplayCart displayCart)
        {
            MOBPromoCodeDetails promoDetails = new MOBPromoCodeDetails();
            promoDetails.PromoCodes = new List<MOBPromoCode>();
            if (isAFSCouponApplied(displayCart))
            {
                var promoOffer = displayCart.SpecialPricingInfo.MerchOfferCoupon;
                promoDetails.PromoCodes.Add(new MOBPromoCode
                {
                    PromoCode = !_configuration.GetValue<bool>("DisableHandlingCaseSenstivity") ? promoOffer.PromoCode.ToUpper().Trim() : promoOffer.PromoCode.Trim(),
                    AlertMessage = promoOffer.Description,
                    IsSuccess = true,
                    TermsandConditions = new MOBMobileCMSContentMessages
                    {
                        HeadLine = _configuration.GetValue<string>("PromoCodeTermsandConditionsTitle"),
                        Title = _configuration.GetValue<string>("PromoCodeTermsandConditionsTitle")
                    },
                    Product = promoOffer.Product
                });
            }
            return promoDetails;
        }

        private RegisterFlightsRequest BuildRegisterFlightRequest(FlightReservationResponse flightReservationResponse, string flow, MOBRequest mobRequest)
        {
            RegisterFlightsRequest request = new RegisterFlightsRequest();
            request.CartId = flightReservationResponse.CartId;
            request.CartInfo = flightReservationResponse.DisplayCart;
            request.CountryCode = flightReservationResponse.DisplayCart.CountryCode;//TODO:Check this is populated all the time.
            request.Reservation = flightReservationResponse.Reservation;
            request.DeviceID = mobRequest.DeviceId;
            request.Upsells = flightReservationResponse.Upsells;
            request.MerchOffers = flightReservationResponse.MerchOffers;
            request.LoyaltyUpgradeOffers = flightReservationResponse.LoyaltyUpgradeOffers;
            request.WorkFlowType = _shoppingUtility.GetWorkFlowType(flow);
            return request;
        }

        private async Task<(MOBSHOPAvailability availability, List<ReservationFlightSegment> segments)> GetMetaShopBookingDetails(Session session, MetaSelectTripRequest request, List<ReservationFlightSegment> segments, bool isUpgradedFromElf = false)
        {
            var availability = new MOBSHOPAvailability();
            MOBSHOPReservation reservation;
            var url = string.Format("{0}/ShopBookingDetails", _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLShopping"));

            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetMetaShopBookingDetails - Request url for ShopBookingDetails", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, url));


            var shopSelectRequest = BuildShopSelectRequest(request);
            var jsonRequest = JsonConvert.SerializeObject(shopSelectRequest);
            await _sessionHelperService.SaveSession(jsonRequest, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);

            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetMetaShopBookingDetails - Request for ShopBookingDetails", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonRequest));
            _logger.LogInformation("GetMetaShopBookingDetails - Request for ShopBookingDetails  SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Request {request}", session.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonRequest);

            string token = string.IsNullOrEmpty(session.Token) ? _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration).Result : session.Token;
            string actionName = "ShopBookingDetails";
            var response = await _flightShoppingService.GetMetaBookingDetails<FlightReservationResponse>(token, actionName, jsonRequest, session.SessionId);
            await _sessionHelperService.SaveSession(response, session.SessionId, new List<string> { session.SessionId, new FlightReservationResponse().GetType().FullName }, new FlightReservationResponse().GetType().FullName).ConfigureAwait(false);
            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetMetaShopBookingDetails - Response for ShopBookingDetails", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonResponse));
            _logger.LogInformation("GetMetaShopBookingDetails - Response for ShopBookingDetails  SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Response {response}", session.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, response);

            if (response != null)
            {
                if (response != null && response.Status.Equals(StatusType.Success) && response.Reservation != null)
                {
                    if (!(_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, session.IsReshopChange) && request.IsCatalogOnForTavelerTypes))
                    {
                        ThrowMessageIfAnyPaxTypeIsNotADT(response.DisplayCart);
                    }

                    session.CartId = response.CartId;
                    session.IsMeta = true;
                    await _sessionHelperService.SaveSession(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);

                    if (response.DisplayCart.IsElf && PricesAreInverted(response))
                        return await AutoUpsellElf(session, request, segments, response);

                    availability.Upsells = GetMetaShopUpsellsAndRequestedProduct(response);

                    reservation = new MOBSHOPReservation(_configuration, _cachingService);

                    //-------Feature 208204--- Common class data carrier for hirarchy methds-----
                    MOBSHOPDataCarrier _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
                    //_mOBSHOPDataCarrier.SearchType = request.;
                    //_mOBSHOPDataCarrier.AwardTravel = request.AwardTravel;
                    //if (_configuration.GetValue<bool>("Shopping - bPricingBySlice"))
                    //{
                    //    // One time decide to assign text for all the products in the Flights. Will be using in BE & Compare Screens
                    //    _mOBSHOPDataCarrier.PriceFormText = GetPriceFromText(shopRequest.SearchType);
                    //}
                    //-----------

                    if (response.DisplayCart != null && response.DisplayCart.DisplayPrices != null && response.Reservation != null)
                    {
                        if (reservation.ShopReservationInfo2 == null)
                            reservation.ShopReservationInfo2 = new ReservationInfo2();
                        reservation.CheckedbagChargebutton = _configuration.GetValue<string>("ViewCheckedBagChargesButton");
                        reservation.IsELF = response.DisplayCart.IsElf;
                        reservation.IsSSA = _shoppingUtility.EnableSSA(request.Application.Id, request.Application.Version.Major);
                        reservation.ShopReservationInfo2.Characteristics = ShopStaticUtility.GetCharacteristics(response.Reservation);
                        reservation.ShopReservationInfo2.IsIBELite = _shoppingUtility.IsIBELiteFare(response.DisplayCart);
                        reservation.ShopReservationInfo2.IsIBE = _shoppingUtility.IsIBEFullFare(response.DisplayCart);
                        reservation.ShopReservationInfo2.FareRestrictionsMessage = reservation.ShopReservationInfo2.IsIBELite
                                                                                   ? _configuration.GetValue<string>("IBELiteRestrictionsMessageMetaRTI") : null; //needed only in metapath
                        reservation.ELFMessagesForRTI = await new ELFRitMetaShopMessages(_configuration, _dynamoDBService, _legalDocumentsForTitlesService, _headers).GetELFShopMessagesForRestrictions(reservation, request.Application.Id);
                        reservation.ISInternational = new IsInternationalFlagMapper().IsInternationalFromResponse(response);
                        reservation.IsMetaSearch = true;
                        await new ELFRitMetaShopMessages(_configuration, _dynamoDBService, _legalDocumentsForTitlesService, _headers).AddElfRtiMetaSearchMessages(reservation);
                        await AddElfUpgradeMessageForMetasearch(isUpgradedFromElf, reservation);

                        bool isSupportedVersion = GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, "AndroidBundleVersion", "IOSBundleVersion", "", "", true, _configuration);
                        if (isSupportedVersion)
                        {
                            if (_configuration.GetValue<bool>("IsEnableBundlesForBasicEconomyBooking"))
                            {
                                reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles");
                            }
                            else
                            {
                                reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles") && !(response.DisplayCart.IsElf || _shoppingUtility.IsIBEFullFare(response.DisplayCart));
                            }
                        }
                        reservation.SessionId = session.SessionId;
                        reservation.CartId = request.CartId;
                        reservation.MetaSessionId = request.MedaSessionId;
                        reservation.PointOfSale = response.Reservation.PointOfSale.Country.CountryCode;
                        reservation.NumberOfTravelers = response.Reservation.NumberInParty;
                        switch (response.DisplayCart.SearchType)
                        {
                            case SearchType.OneWay:
                                reservation.SearchType = "OW";
                                break;
                            case SearchType.RoundTrip:
                                reservation.SearchType = "RT";
                                break;
                            case SearchType.MultipleDestination:
                                reservation.SearchType = "MD";
                                break;
                            default:
                                reservation.SearchType = string.Empty;
                                break;
                        }
                        var fareClass = await GetFareClassAtShoppingRequestFromPersist(session.SessionId);
                        var flightDepartDatesForSelectedTrip = new List<string>();
                        reservation.Trips = await PopulateMetaTrips(_mOBSHOPDataCarrier, response.CartId, null, session.SessionId, request.Application.Id, request.DeviceId, request.Application.Version.Major, true, response.DisplayCart.DisplayTrips, fareClass, flightDepartDatesForSelectedTrip);
                        if (reservation.Trips != null && reservation.Trips.Count > 0)
                        {
                            flightDepartDatesForSelectedTrip.AddRange(reservation.Trips.Select(shopTrip => shopTrip.TripId + "|" + shopTrip.DepartDate));
                        }
                        if (_shoppingUtility.IsEnableTaxForAgeDiversification(session.IsReshopChange, request.Application.Id, request.Application.Version.Major))
                        {
                            reservation.Prices = _shoppingUtility.GetPrices(response.DisplayCart.DisplayPrices, false, session.SessionId, searchType: reservation.SearchType.ToString(), session: session);

                        }
                        else
                        {
                            reservation.Prices = GetPrices(response.DisplayCart.DisplayPrices, false, session.SessionId);
                        }
                        if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                        {
                            reservation.Prices.AddRange(GetPrices(response.DisplayCart.DisplayFees, false, string.Empty));
                        }
                        //need to add close in fee to TOTAL
                        reservation.Prices = AdjustTotal(reservation.Prices);
                        if (_shoppingUtility.IsEnableTaxForAgeDiversification(session.IsReshopChange, request.Application.Id, request.Application.Version.Major))
                        {
                            if (reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                                reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();

                            reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = _shoppingUtility.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices);

                            if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                            {
                                reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes.Add(ShopStaticUtility.AddFeesAfterPriceChange(response.DisplayCart.DisplayFees));
                            }
                        }
                        else
                        {
                            reservation.Taxes = GetTaxAndFees(response.DisplayCart.DisplayPrices, response.Reservation.NumberInParty);
                            if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                            {
                                //combine fees into taxes so that totals are correct
                                var tempList = new List<DisplayPrice>();
                                tempList.AddRange(response.DisplayCart.DisplayPrices);
                                tempList.AddRange(response.DisplayCart.DisplayFees);
                                reservation.Taxes = GetTaxAndFees(tempList, response.Reservation.NumberInParty);
                            }
                        }
                        reservation.TravelOptions = GetTravelOptions(response.DisplayCart, session.IsReshopChange, request.Application.Id, request.Application.Version.Major);

                        if (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, session.IsReshopChange) && request.IsCatalogOnForTavelerTypes)
                        {
                            string firstLOFDepDate = response.DisplayCart.DisplayTrips[0].Flights[0].DepartDateTime;

                            response.DisplayCart.DisplayTravelers.Sort((x, y) =>
                          ShopStaticUtility.GetTTypeValue(TopHelper.GetAgeByDOB(x.DateOfBirth, firstLOFDepDate), x.PaxTypeCode.ToUpper().Equals("INF")).CompareTo(ShopStaticUtility.GetTTypeValue(TopHelper.GetAgeByDOB(y.DateOfBirth, firstLOFDepDate), y.PaxTypeCode.ToUpper().Equals("INF")))
                         );
                            reservation.ShopReservationInfo2.TravelerTypes = ShopStaticUtility.GetTravelTypesFromShop(response.DisplayCart.DisplayTravelers);
                            reservation.ShopReservationInfo2.displayTravelTypes = ShopStaticUtility.GetDisplayTravelerTypes(response.DisplayCart.DisplayTravelers);
                        }

                        if (_shoppingUtility.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, true) && !session.IsReshopChange)
                        {
                            if (reservation.ShopReservationInfo2 == null)
                                reservation.ShopReservationInfo2 = new ReservationInfo2();
                            reservation.ShopReservationInfo2.IsDisplayCart = _shoppingUtility.IsDisplayCart(session, GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("Android_EnableAwardLiveCart_AppVersion"), _configuration.GetValue<string>("iPhone_EnableAwardLiveCart_AppVersion"))
                                                                                                                        ? "DisplayCartTravelTypesWithAward"
                                                                                                                        : "DisplayCartTravelTypes");
                        }

                        if (string.IsNullOrEmpty(session.EmployeeId))
                        {
                            reservation.FareLock = GetFareLockOptions(response.FareLockResponse, GetFarePrice(response.DisplayCart.DisplayPrices), GetFareCurrency(response.DisplayCart.DisplayPrices), false, GetFareMiles(response.DisplayCart.DisplayPrices), request.Application.Id, request.Application.Version.Major);
                        }

                        reservation.GetALLSavedTravelers = false;

                        if (string.IsNullOrEmpty(session.EmployeeId))
                        {
                            reservation.FareLock = GetFareLockOptions(response.FareLockResponse, GetFarePrice(response.DisplayCart.DisplayPrices), GetFareCurrency(response.DisplayCart.DisplayPrices), false, GetFareMiles(response.DisplayCart.DisplayPrices), request.Application.Id, request.Application.Version.Major);
                        }
                        reservation.AwardTravel = false;

                        if (!reservation.AwardTravel)
                        {
                            reservation.LMXFlights = PopulateLMX(response.CartId, null, session.SessionId, request.Application.Id, request.DeviceId, request.Application.Version.Major, true, response.DisplayCart.DisplayTrips);
                            reservation.IneligibleToEarnCreditMessage = _configuration.GetValue<string>("IneligibleToEarnCreditMessage");
                            reservation.OaIneligibleToEarnCreditMessage = _configuration.GetValue<string>("OaIneligibleToEarnCreditMessage");
                        }
                        reservation.IsCubaTravel = IsCubaTravelTrip(reservation);
                        if (reservation.IsCubaTravel)
                        {
                            MobileCMSContentRequest mobMobileCMSContentRequest = new MobileCMSContentRequest();
                            mobMobileCMSContentRequest.Application = request.Application;
                            mobMobileCMSContentRequest.Token = session.Token;
                            availability.Reservation.CubaTravelInfo = await GetCubaTravelResons(mobMobileCMSContentRequest);
                            //Profile profile = new CSL.Profile();
                            availability.Reservation.CubaTravelInfo.CubaTravelTitles = await GetMPPINPWDTitleMessages("CUBA_TRAVEL_CONTENT");
                        }
                        if (_shoppingUtility.EnableBoeingDisclaimer(session.IsReshopChange) && _shoppingUtility.IsBoeingDisclaimer(response.DisplayCart.DisplayTrips))
                        {
                            if (reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                            if (!reservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.BOEING737WARNING.ToString()))
                            {
                                reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetBoeingDisclaimer());

                                reservation.ShopReservationInfo2.InfoWarningMessages = reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                            }
                        }
                        //var pkDispenserPublicKey = _sessionHelperService.GetSession<string>(_shoppingUtility.GetCSSPublicKeyPersistSessionStaticGUID(request.Application.Id), "pkDispenserPublicKey");
                        //if (!string.IsNullOrEmpty(pkDispenserPublicKey))
                        //{
                        //    reservation.PKDispenserPublicKey = pkDispenserPublicKey;
                        //}
                        //else
                        //{
                        //    reservation.PKDispenserPublicKey = Authentication.GetPkDispenserPublicKey(request.Application.Id, request.DeviceId, request.Application.Version.Major, session.SessionId, logEntries, traceSwitch, session.Token);
                        //}

                        segments = CommonMethods.FlattenSegmentsForSeatDisplay(response.Reservation);

                        if (_shoppingUtility.EnableCovidTestFlightShopping(request.Application.Id, request.Application.Version.Major, session.IsReshopChange))
                        {
                            ShopStaticUtility.AssignCovidTestIndicator(reservation);
                        }

                        var persistedShopBookingDetailsResponse = new ShopBookingDetailsResponse
                        {
                            SessionId = session.SessionId,
                            CartId = request.CartId,
                            Reservation = response.Reservation
                        };
                        if (string.IsNullOrEmpty(session.EmployeeId))
                        {
                            persistedShopBookingDetailsResponse.FareLock = response.FareLockResponse;
                        }
                        await _sessionHelperService.SaveSession(persistedShopBookingDetailsResponse, session.SessionId, new List<string> { session.SessionId, persistedShopBookingDetailsResponse.ObjectName }, persistedShopBookingDetailsResponse.ObjectName).ConfigureAwait(false);

                        var shop = new Model.Shopping.ShoppingResponse
                        {
                            SessionId = session.SessionId,
                            CartId = request.CartId,
                            Request = new MOBSHOPShopRequest
                            {
                                AccessCode = "1A7370E9-A532-4376-BD39-41795F01321C",
                                SessionId = session.SessionId,
                                CountryCode = "US",
                                NumberOfAdults = reservation.NumberOfTravelers,
                                PremierStatusLevel = request.PremierStatusLevel,
                                MileagePlusAccountNumber = request.MileagePlusAccountNumber
                            }
                        };
                        await _sessionHelperService.SaveSession(shop, session.SessionId, new List<string> { session.SessionId, shop.ObjectName }, shop.ObjectName).ConfigureAwait(false);
                    }
                }
                else
                {
                    if (response != null && response.Errors != null && response.Errors.Count > 0)
                    {
                        var errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            if (!string.IsNullOrEmpty(error.Message))
                            {
                                if (error.Message.Equals("United Airlines The fare or flight option you have selected is no longer available."))
                                {
                                    errorMessage = errorMessage + _configuration.GetValue<string>("MetaTripExceptionMessage");
                                }
                                else
                                {
                                    errorMessage = errorMessage + " " + error.Message;
                                }
                            }
                        }

                        throw new Exception(errorMessage);
                    }

                    throw new MOBUnitedException("Failed to retrieve booking details.");
                }
            }
            else
            {
                throw new MOBUnitedException("Failed to retrieve booking details.");
            }
            availability.Reservation = reservation;
            return (availability, segments);
        }

        private async Task<(MOBSHOPAvailability availability, List<ReservationFlightSegment> segments)> AutoUpsellElf(Session session, MetaSelectTripRequest request, List<ReservationFlightSegment> segments, FlightReservationResponse response)
        {
            var newRequest = BuildNewUpsellRequest(response, request);
            return await GetMetaShopBookingDetails(session, newRequest, segments, true);
        }

        public async Task<TravelSpecialNeeds> GetItineraryAvailableSpecialNeeds(Session session, int appId, string appVersion, string deviceId, IEnumerable<ReservationFlightSegment> segments, string languageCode,
            MOBSHOPReservation reservation = null, SelectTripRequest selectRequest = null)
        {
            MultiCallResponse flightshoppingReferenceData = null;
            IEnumerable<ReservationFlightSegment> pnrOfferedMeals = null;
            var offersSSR = new TravelSpecialNeeds();

            try
            {
                flightshoppingReferenceData = await GetSpecialNeedsReferenceDataFromFlightShopping(session, appId, appVersion, deviceId, languageCode);
                pnrOfferedMeals = await GetOfferedMealsForItineraryFromPNRManagement(session, appId, appVersion, deviceId, segments);
            }
            catch (Exception) // 'System.ArgumentException' is thrown when any action in the actions array throws an exception.
            {
                if (flightshoppingReferenceData == null) // unable to get reference data, POPULATE DEFAULT SPECIAL REQUESTS
                {
                    offersSSR.ServiceAnimalsMessages = new List<Model.Common.MOBItem> { new Model.Common.MOBItem { CurrentValue = _configuration.GetValue<string>("SSR_RefDataServiceFailure_ServiceAnimalMassage") } };

                    flightshoppingReferenceData = GetMultiCallResponseWithDefaultSpecialRequests();
                }
                else if (pnrOfferedMeals == null) // unable to get market restriction meals, POPULATE DEFAULT MEALS
                {
                    pnrOfferedMeals = PopulateSegmentsWithDefaultMeals(segments);
                }
            }

            offersSSR.SpecialMeals = GetOfferedMealsForItinerary(pnrOfferedMeals, flightshoppingReferenceData);
            offersSSR.SpecialMealsMessages = GetSpecialMealsMessages(pnrOfferedMeals, flightshoppingReferenceData);
            offersSSR.SpecialRequests = await GetOfferedSpecialRequests(flightshoppingReferenceData, reservation, selectRequest, session);
            offersSSR.SpecialMealsMessages = GetSpecialMealsMessages(pnrOfferedMeals, flightshoppingReferenceData);
            offersSSR.ServiceAnimals = GetOfferedServiceAnimals(flightshoppingReferenceData, segments, appId, appVersion);
            offersSSR.SpecialNeedsAlertMessages = GetPartnerAirlinesSpecialTravelNeedsMessage(session, segments);

            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("RemoveEmotionalSupportServiceAnimalOption_EffectiveDateTime"))
                && Convert.ToDateTime(_configuration.GetValue<string>("RemoveEmotionalSupportServiceAnimalOption_EffectiveDateTime")) <= DateTime.Now
                && offersSSR.ServiceAnimals != null && offersSSR.ServiceAnimals.Any())
            {
                offersSSR.ServiceAnimals.Remove(offersSSR.ServiceAnimals.FirstOrDefault(x => x.Code == "ESAN" && x.Value == "6"));
            }
            if (IsTaskTrainedServiceDogSupportedAppVersion(appId, appVersion)
                 && offersSSR?.ServiceAnimals != null && offersSSR.ServiceAnimals.Any(x => x.Code == "ESAN" && x.Value == "5"))
            {
                offersSSR.ServiceAnimals.Remove(offersSSR.ServiceAnimals.FirstOrDefault(x => x.Code == "ESAN" && x.Value == "5"));
            }

            await AddServiceAnimalsMessageSection(offersSSR, appId, appVersion, session, deviceId);

            if (offersSSR.ServiceAnimalsMessages == null || !offersSSR.ServiceAnimalsMessages.Any())
                offersSSR.ServiceAnimalsMessages = GetServiceAnimalsMessages(offersSSR.ServiceAnimals);
            if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && session.CatalogItems != null && session.CatalogItems.Count > 0 &&
                  session.CatalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableNewPartnerAirlines).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableNewPartnerAirlines).ToString())?.CurrentValue == "1"
                 && reservation.Trips?.FirstOrDefault()?.FlattenedFlights?.FirstOrDefault()?.Flights?.FirstOrDefault().OperatingCarrier != null
                   && _configuration.GetValue<string>("SupportedAirlinesFareComparison").Contains(reservation.Trips?.FirstOrDefault()?.FlattenedFlights?.FirstOrDefault()?.Flights?.FirstOrDefault().OperatingCarrier.ToUpper())
                  )
            {

                offersSSR.SpecialNeedsAlertMessages = new Mobile.Model.Common.MOBAlertMessages
                {
                    HeaderMessage = _configuration.GetValue<string>("PartnerAirlinesSpecialTravelNeedsHeader"),
                    IsDefaultOption = true,
                    MessageType = MOBINFOWARNINGMESSAGEICON.WARNING.ToString(),
                    AlertMessages = new List<MOBSection>
                    {
                        new MOBSection
                        {
                            MessageType = MOBINFOWARNINGMESSAGEICON.WARNING.ToString(),
                            Text1 = _configuration.GetValue<string>("PartnerAirlinesSpecialTravelNeedsMessage"),
                            Order = "1"
                        }
                    }
                };
            }

            return offersSSR;
        }

        private async Task<MultiCallResponse> GetSpecialNeedsReferenceDataFromFlightShopping(Session session, int appId, string appVersion, string deviceId, string languageCode)
        {
            string cslActionName = "MultiCall";

            //    logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetSpecialNeedsReferenceDataFromFlightShopping - Request url for " + cslActionName, "Trace", appId, appVersion, deviceId, url));

            string jsonRequest = JsonConvert.SerializeObject(GetFlightShoppingMulticallRequest(languageCode));
            //#TakeTokenFromSession
            // string token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration);

            var cslCallDurationstopwatch = new Stopwatch();
            cslCallDurationstopwatch.Start();

            var response = await _referencedataService.GetSpecialNeedsInfo<MultiCallResponse>(cslActionName, jsonRequest, session.Token, _headers.ContextValues.SessionId);

            if (cslCallDurationstopwatch.IsRunning)
            {
                cslCallDurationstopwatch.Stop();
            }

            if (response != null)
            {

                if (response == null || response.SpecialRequestResponses == null || response.ServiceAnimalResponses == null || response.SpecialMealResponses == null || response.SpecialRequestResponses == null)
                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

                return response;
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
        }

        private MultiCallRequest GetFlightShoppingMulticallRequest(string languageCode)
        {
            var request = new MultiCallRequest
            {
                ServiceAnimalRequests = new Collection<ServiceAnimalRequest> { new ServiceAnimalRequest { LanguageCode = languageCode } },
                ServiceAnimalTypeRequests = new Collection<ServiceAnimalTypeRequest> { new ServiceAnimalTypeRequest { LanguageCode = languageCode } },
                SpecialMealRequests = new Collection<SpecialMealRequest> { new SpecialMealRequest { LanguageCode = languageCode } },
                SpecialRequestRequests = new Collection<SpecialRequestRequest> { new SpecialRequestRequest { LanguageCode = languageCode/*, Channel = ConfigurationManager.AppSettings["Shopping - ChannelType")*/ } },
            };

            return request;
        }

        private async Task<IEnumerable<ReservationFlightSegment>> GetOfferedMealsForItineraryFromPNRManagement(Session session, int appId, string appVersion, string deviceId, IEnumerable<ReservationFlightSegment> segments)
        {
            string cslActionName = "SpecialMeals/FlightSegments";

            string jsonRequest = JsonConvert.SerializeObject(GetOfferedMealsForItineraryFromPNRManagementRequest(segments));
            //#TakeTokenFromSession
            // string token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration);

            var cslCallDurationstopwatch = new Stopwatch();
            cslCallDurationstopwatch.Start();

            var response = await _updatePNRService.GetOfferedMealsForItinerary<List<ReservationFlightSegment>>(session.Token, cslActionName, jsonRequest, session.SessionId);

            if (cslCallDurationstopwatch.IsRunning)
            {
                cslCallDurationstopwatch.Stop();
            }

            if (response != null)
            {

                if (response == null)
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                if (response.Count > 0)
                {
                    await _sessionHelperService.SaveSession<List<ReservationFlightSegment>>(response, session.SessionId, new List<string> { session.SessionId, new ReservationFlightSegment().GetType().FullName }, new ReservationFlightSegment().GetType().FullName).ConfigureAwait(false);//, response[0].GetType().FullName);
                }

                return response;
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
        }

        private MultiCallResponse GetMultiCallResponseWithDefaultSpecialRequests()
        {
            try
            {
                return new MultiCallResponse
                {
                    SpecialRequestResponses = new Collection<SpecialRequestResponse>
                    {
                        new SpecialRequestResponse
                        {
                            SpecialRequests = new Collection<United.Service.Presentation.CommonModel.Characteristic> (_configuration.GetValue<string>("SSR_DefaultSpecialRequests")
                                                                                                .Split('|')
                                                                                                .Select(request => request.Split('^'))
                                                                                                .Select(request => new United.Service.Presentation.CommonModel.Characteristic
                                                                                                {
                                                                                                    Code = request[0],
                                                                                                    Description = request[1],
                                                                                                    Genre = new United.Service.Presentation.CommonModel.Genre { Description = request[2]},
                                                                                                    Value = request[3]
                                                                                                })
                                                                                                .ToList())
                        }
                    }
                };
            }
            catch
            {
                return null;
            }
        }

        private List<Model.Common.MOBItem> GetSpecialMealsMessages(IEnumerable<ReservationFlightSegment> allSegmentsWithMeals, MultiCallResponse flightshoppingReferenceData)
        {
            Func<List<Model.Common.MOBItem>> GetMealUnavailableMsg = () => new List<Model.Common.MOBItem> { new Model.Common.MOBItem { CurrentValue = string.Format(_configuration.GetValue<string>("SSRItinerarySpecialMealsNotAvailableMsg"), "") } };

            if (allSegmentsWithMeals == null || !allSegmentsWithMeals.Any()
                || flightshoppingReferenceData == null || flightshoppingReferenceData.SpecialMealResponses == null || !flightshoppingReferenceData.SpecialMealResponses.Any()
                || flightshoppingReferenceData.SpecialMealResponses[0].SpecialMeals == null || !flightshoppingReferenceData.SpecialMealResponses[0].SpecialMeals.Any())
            {
                return GetMealUnavailableMsg();
            }

            // all meals from reference data
            var allRefMeals = flightshoppingReferenceData.SpecialMealResponses[0].SpecialMeals.Select(x => x.Type.Key);
            if (allRefMeals == null || !allRefMeals.Any())
                return GetMealUnavailableMsg();

            var segmentsHaveMeals = allSegmentsWithMeals.Where(seg => seg != null && seg.FlightSegment != null && seg.FlightSegment.Characteristic != null && seg.FlightSegment.Characteristic.Any()
                                                   && seg.FlightSegment.Characteristic[0] != null
                                                   && seg.FlightSegment.Characteristic.Exists(x => x.Code.Equals("SPML", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.Value)))
                                                   .Select(seg => new
                                                   {
                                                       segment = string.Join(" - ", seg.FlightSegment.DepartureAirport.IATACode, seg.FlightSegment.ArrivalAirport.IATACode),
                                                       meals = string.IsNullOrWhiteSpace(seg.FlightSegment.Characteristic[0].Value) ? new HashSet<string>() : new HashSet<string>(seg.FlightSegment.Characteristic[0].Value.Split('|', ' ').Intersect(allRefMeals))
                                                   })
                                                   .Where(seg => seg.meals != null && seg.meals.Any())
                                                   .Select(seg => seg.segment)
                                                   .ToList();

            if (segmentsHaveMeals == null || !segmentsHaveMeals.Any())
            {
                return GetMealUnavailableMsg();
            }

            if (segmentsHaveMeals.Count < allSegmentsWithMeals.Count())
            {
                var segments = segmentsHaveMeals.Count > 1 ? string.Join(", ", segmentsHaveMeals.Take(segmentsHaveMeals.Count - 1)) + " and " + segmentsHaveMeals.Last() : segmentsHaveMeals.First();
                return new List<Model.Common.MOBItem> { new Model.Common.MOBItem { CurrentValue = string.Format(_configuration.GetValue<string>("SSR_MarketMealRestrictionMessage"), segments) } };
            }

            return null;
        }

        private List<TravelSpecialNeed> GetOfferedMealsForItinerary(IEnumerable<ReservationFlightSegment> allSegmentsWithMeals, MultiCallResponse flightshoppingReferenceData)
        {
            if (allSegmentsWithMeals == null || !allSegmentsWithMeals.Any()
                || flightshoppingReferenceData == null || flightshoppingReferenceData.SpecialMealResponses == null || !flightshoppingReferenceData.SpecialMealResponses.Any()
                || flightshoppingReferenceData.SpecialMealResponses[0].SpecialMeals == null || !flightshoppingReferenceData.SpecialMealResponses[0].SpecialMeals.Any())
                return null;

            Func<IEnumerable<string>, List<Model.Common.MOBItem>> generateMsg = flightSegments =>
            {
                var segments = flightSegments.Count() > 1 ? string.Join(", ", flightSegments.Take(flightSegments.Count() - 1)) + " and " + flightSegments.Last() : flightSegments.First();
                return new List<Model.Common.MOBItem> { new Model.Common.MOBItem { CurrentValue = string.Format(_configuration.GetValue<string>("SSR_MealRestrictionMessage"), segments) } };
            };

            // all meals from reference data
            var allRefMeals = flightshoppingReferenceData.SpecialMealResponses[0].SpecialMeals.ToDictionary(x => x.Type.Key, x => string.Join("^", x.Value[0], x.Description));
            if (allRefMeals == null || !allRefMeals.Any())
                return null;

            // Dictionary whose keys are segments (orig - dest) and values are list of all meals that are available for each segment
            // These contain only the segments that offer meals
            // These meals also need to exist in reference data table 
            var segmentAndMealsMap = allSegmentsWithMeals.Where(seg => seg != null && seg.FlightSegment != null && seg.FlightSegment.Characteristic != null && seg.FlightSegment.Characteristic.Any()
                                                   && seg.FlightSegment.Characteristic[0] != null
                                                   && seg.FlightSegment.Characteristic.Exists(x => x.Code.Equals("SPML", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.Value))) // get all segments that offer meals
                                                   .Select(seg => new // project them
                                                   {
                                                       segment = string.Join(" - ", seg.FlightSegment.DepartureAirport.IATACode, seg.FlightSegment.ArrivalAirport.IATACode), // IAH - NRT if going from IAH to NRT 
                                                       meals = string.IsNullOrWhiteSpace(seg.FlightSegment.Characteristic[0].Value) ? null : new HashSet<string>(seg.FlightSegment.Characteristic[0].Value.Split('|', ' ').Intersect(allRefMeals.Keys)) // List of all meal codes that offer on the segment
                                                   })
                                                   .Where(segment => segment.meals != null && segment.meals.Any()) // filter out the segments that don't offer meals
                                                   .GroupBy(seg => seg.segment) // handle same market exist twice for MD
                                                   .Select(grp => grp.First()) // handle same market exist twice for MD 
                                                   .ToDictionary(seg => seg.segment, seg => seg.meals); // tranform them to dictionary of segment and meals

            if (segmentAndMealsMap == null || !segmentAndMealsMap.Any())
                return null;

            // Get common meals that offers on all segments after filtering out all segments that don't offer meals
            var mealsThatAvailableOnAllSegments = segmentAndMealsMap.Values.Skip(1)
                                                                            .Aggregate(new HashSet<string>(segmentAndMealsMap.Values.First()), (current, next) => { current.IntersectWith(next); return current; });

            // Filter out the common meals
            if (mealsThatAvailableOnAllSegments != null && mealsThatAvailableOnAllSegments.Any())
            {
                segmentAndMealsMap.Values.ToList().ForEach(x => x.RemoveWhere(item => mealsThatAvailableOnAllSegments.Contains(item)));
            }

            // Add the non-common meals, these will have message
            var results = segmentAndMealsMap.Where(kv => kv.Value != null && kv.Value.Any())
                                .SelectMany(item => item.Value.Select(x => new { mealCode = x, segment = item.Key }))
                                .GroupBy(x => x.mealCode, x => x.segment)
                                .ToDictionary(x => x.Key, x => x.ToList())
                                .Select(kv => new TravelSpecialNeed
                                {
                                    Code = kv.Key,
                                    Value = allRefMeals[kv.Key].Split('^')[0],
                                    DisplayDescription = allRefMeals[kv.Key].Split('^')[1],
                                    RegisterServiceDescription = allRefMeals[kv.Key].Split('^')[1],
                                    Type = TravelSpecialNeedType.SpecialMeal.ToString(),
                                    Messages = mealsThatAvailableOnAllSegments.Any() ? generateMsg(kv.Value) : null
                                })
                                .ToList();

            // Add the common meals, these don't have messages
            if (mealsThatAvailableOnAllSegments.Any())
            {
                results.AddRange(mealsThatAvailableOnAllSegments.Select(m => new TravelSpecialNeed
                {
                    Code = m,
                    Value = allRefMeals[m].Split('^')[0],
                    DisplayDescription = allRefMeals[m].Split('^')[1],
                    RegisterServiceDescription = allRefMeals[m].Split('^')[1],
                    Type = TravelSpecialNeedType.SpecialMeal.ToString()
                }));
            }

            return results == null || !results.Any() ? null : results; // return null if empty
        }

        private List<TravelSpecialNeed> GetOfferedServiceAnimals(MultiCallResponse flightshoppingReferenceData, IEnumerable<ReservationFlightSegment> segments, int appId, string appVersion)
        {
            if (!IsTaskTrainedServiceDogSupportedAppVersion(appId, appVersion) &&
               !_configuration.GetValue<bool>("ShowServiceAnimalInTravelNeeds"))
                return null;

            if (segments == null || !segments.Any()
                || flightshoppingReferenceData == null || flightshoppingReferenceData.ServiceAnimalResponses == null || !flightshoppingReferenceData.ServiceAnimalResponses.Any()
                || flightshoppingReferenceData.ServiceAnimalResponses[0].Animals == null || !flightshoppingReferenceData.ServiceAnimalResponses[0].Animals.Any()
                || flightshoppingReferenceData.ServiceAnimalTypeResponses == null || !flightshoppingReferenceData.ServiceAnimalTypeResponses.Any()
                || flightshoppingReferenceData.ServiceAnimalTypeResponses[0].Types == null || !flightshoppingReferenceData.ServiceAnimalTypeResponses[0].Types.Any())
                return null;

            if (!DoesItineraryHaveServiceAnimal(segments))
                return null;

            var SSRAnimalValueCodeDesc = _configuration.GetValue<string>("SSRAnimalValueCodeDesc").Split('|').ToDictionary(x => x.Split('^')[0], x => x.Split('^')[1]);
            var SSRAnimalTypeValueCodeDesc = _configuration.GetValue<string>("SSRAnimalTypeValueCodeDesc").Split('|').ToDictionary(x => x.Split('^')[0], x => x.Split('^')[1]);

            Func<string, string, string, string, string, TravelSpecialNeed> createSpecialNeed = (code, value, desc, RegisterServiceDesc, type)
                => new TravelSpecialNeed { Code = code, Value = value, DisplayDescription = desc, RegisterServiceDescription = RegisterServiceDesc, Type = type };

            List<TravelSpecialNeed> animals = flightshoppingReferenceData.ServiceAnimalResponses[0].Animals
                                                .Where(x => !string.IsNullOrWhiteSpace(x.Description))
                                                .Select(x => createSpecialNeed(SSRAnimalValueCodeDesc[x.Value], x.Value, x.Description, x.Description, TravelSpecialNeedType.ServiceAnimal.ToString())).ToList();

            Func<United.Service.Presentation.CommonModel.Characteristic, TravelSpecialNeed> createServiceAnimalTypeItem = animalType =>
            {
                var type = createSpecialNeed(SSRAnimalTypeValueCodeDesc[animalType.Value], animalType.Value,
                                             animalType.Description, animalType.Description.EndsWith("animal", StringComparison.OrdinalIgnoreCase) ? null : !_configuration.GetValue<bool>("DisableTaskServiceAnimalDescriptionFix") ? animalType.Description : "Dog", TravelSpecialNeedType.ServiceAnimalType.ToString());
                type.SubOptions = animalType.Description.EndsWith("animal", StringComparison.OrdinalIgnoreCase) ? animals : null;
                return type;
            };

            return flightshoppingReferenceData.ServiceAnimalTypeResponses[0].Types.Where(x => !string.IsNullOrWhiteSpace(x.Description))
                                                                                  .Select(createServiceAnimalTypeItem).ToList();
        }

        private bool DoesItineraryHaveServiceAnimal(IEnumerable<ReservationFlightSegment> segments)
        {
            var statesDoNotAllowServiceAnimal = new HashSet<string>(_configuration.GetValue<string>("SSRStatesDoNotAllowServiceAnimal").Split('|'));
            foreach (var segment in segments)
            {
                if (segment == null || segment.FlightSegment == null || segment.FlightSegment.ArrivalAirport == null || segment.FlightSegment.DepartureAirport == null
                    || segment.FlightSegment.ArrivalAirport.IATACountryCode == null || segment.FlightSegment.DepartureAirport.IATACountryCode == null
                    || string.IsNullOrWhiteSpace(segment.FlightSegment.ArrivalAirport.IATACountryCode.CountryCode) || string.IsNullOrWhiteSpace(segment.FlightSegment.DepartureAirport.IATACountryCode.CountryCode)
                    || segment.FlightSegment.ArrivalAirport.StateProvince == null || segment.FlightSegment.DepartureAirport.StateProvince == null
                    || string.IsNullOrWhiteSpace(segment.FlightSegment.ArrivalAirport.StateProvince.StateProvinceCode) || string.IsNullOrWhiteSpace(segment.FlightSegment.DepartureAirport.StateProvince.StateProvinceCode)

                    || !segment.FlightSegment.ArrivalAirport.IATACountryCode.CountryCode.Equals("US") || !segment.FlightSegment.DepartureAirport.IATACountryCode.CountryCode.Equals("US") // is international
                    || statesDoNotAllowServiceAnimal.Contains(segment.FlightSegment.ArrivalAirport.StateProvince.StateProvinceCode) // touches states that not allow service animal
                    || statesDoNotAllowServiceAnimal.Contains(segment.FlightSegment.DepartureAirport.StateProvince.StateProvinceCode)) // touches states that not allow service animal
                    return false;
            }

            return true;
        }

        private bool IsTaskTrainedServiceDogSupportedAppVersion(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("TravelSpecialNeedInfo_TaskTrainedServiceDog_Supported_AppVestion_Android"), _configuration.GetValue<string>("TravelSpecialNeedInfo_TaskTrainedServiceDog_Supported_AppVestion_iOS"));
        }
        private async System.Threading.Tasks.Task AddServiceAnimalsMessageSection(TravelSpecialNeeds offersSSR, int appId, string appVersion, Session session, string deviceId)
        {
            if (IsTaskTrainedServiceDogSupportedAppVersion(appId, appVersion))
            {
                if (offersSSR?.ServiceAnimals == null) offersSSR.ServiceAnimals = new List<TravelSpecialNeed>();
                MOBRequest request = new MOBRequest();
                request.Application = new MOBApplication();
                request.Application.Id = appId;
                request.Application.Version = new MOBVersion();
                request.Application.Version.Major = appVersion;
                request.DeviceId = deviceId;
                request.TransactionId = _headers.ContextValues.TransactionId;
                CSLContentMessagesResponse content = new CSLContentMessagesResponse();

                var cmsCacheResponse = await _cachingService.GetCache<string>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID") + "MOBCSLContentMessagesResponse", _headers.ContextValues.TransactionId).ConfigureAwait(false);

                try
                {
                    if (!string.IsNullOrEmpty(cmsCacheResponse))
                        content = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(cmsCacheResponse);
                }

                catch { cmsCacheResponse = null; }

                if (string.IsNullOrEmpty(cmsCacheResponse))
                    content = await _travelerCSL.GetBookingRTICMSContentMessages(request, session);//, LogEntries);

                string emotionalSupportAssistantContent = (content?.Messages?.FirstOrDefault(m => !string.IsNullOrEmpty(m.Title) && m.Title.Equals("TravelNeeds_TaskTrainedDog_Screen_Content_MOB"))?.ContentFull) ?? "";
                string emotionalSupportAssistantCodeVale = _configuration.GetValue<string>("TravelSpecialNeedInfoCodeValue");

                if (!string.IsNullOrEmpty(emotionalSupportAssistantContent) && !string.IsNullOrEmpty(emotionalSupportAssistantCodeVale))
                {
                    var codeValue = emotionalSupportAssistantCodeVale.Split('#');
                    offersSSR.ServiceAnimals.Add(new TravelSpecialNeed
                    {
                        Code = codeValue[0],
                        Value = codeValue[1],
                        DisplayDescription = "",
                        Type = TravelSpecialNeedType.TravelSpecialNeedInfo.ToString(),
                        Messages = new List<MOBItem>
                        {
                            new MOBItem {
                                CurrentValue = emotionalSupportAssistantContent
                            }
                        }
                    });
                }
            }

            else if (_configuration.GetValue<bool>("EnableTravelSpecialNeedInfo")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("TravelSpecialNeedInfo_Supported_AppVestion_Android"), _configuration.GetValue<string>("TravelSpecialNeedInfo_Supported_AppVestion_iOS"))
                && offersSSR.ServiceAnimals != null && offersSSR.ServiceAnimals.Any())
            {
                string emotionalSupportAssistantHeading = _configuration.GetValue<string>("TravelSpecialNeedInfoHeading");
                string emotionalSupportAssistantContent = _configuration.GetValue<string>("TravelSpecialNeedInfoContent");
                string emotionalSupportAssistantCodeVale = _configuration.GetValue<string>("TravelSpecialNeedInfoCodeValue");

                if (!string.IsNullOrEmpty(emotionalSupportAssistantHeading) &&
                    !string.IsNullOrEmpty(emotionalSupportAssistantContent) &&
                    !string.IsNullOrEmpty(emotionalSupportAssistantCodeVale))
                {
                    var codeValue = emotionalSupportAssistantCodeVale.Split('#');

                    offersSSR.ServiceAnimals.Add(new TravelSpecialNeed
                    {
                        Code = codeValue[0],
                        Value = codeValue[1],
                        DisplayDescription = emotionalSupportAssistantHeading,
                        Type = TravelSpecialNeedType.TravelSpecialNeedInfo.ToString(),
                        Messages = new List<MOBItem>
                        {
                            new MOBItem {
                                CurrentValue = emotionalSupportAssistantContent
                            }
                        }
                    });
                }
            }
        }

        private List<Model.Common.MOBItem> GetServiceAnimalsMessages(List<TravelSpecialNeed> serviceAnimals)
        {
            if (serviceAnimals != null && serviceAnimals.Any())
                return null;

            return new List<Model.Common.MOBItem> { new Model.Common.MOBItem { CurrentValue = _configuration.GetValue<string>("SSRItineraryServiceAnimalNotAvailableMsg") } };
        }

        private IEnumerable<ReservationFlightSegment> PopulateSegmentsWithDefaultMeals(IEnumerable<ReservationFlightSegment> segments)
        {
            var pnrOfferedMeals = GetOfferedMealsForItineraryFromPNRManagementRequest(segments);
            pnrOfferedMeals.Where(x => x.FlightSegment != null && x.FlightSegment.IsInternational.Equals("True", StringComparison.OrdinalIgnoreCase))
                           .ToList()
                           .ForEach(x => x.FlightSegment.Characteristic = new Collection<Service.Presentation.CommonModel.Characteristic> { new Service.Presentation.CommonModel.Characteristic {
                                       Code = "SPML",
                                       Description = "Default meals when service is down",
                                       Value = _configuration.GetValue<string>("SSR_DefaultMealCodes")
                                   } });

            return pnrOfferedMeals;
        }

        private IEnumerable<ReservationFlightSegment> GetOfferedMealsForItineraryFromPNRManagementRequest(IEnumerable<ReservationFlightSegment> segments)
        {
            if (segments == null || !segments.Any())
                return new List<ReservationFlightSegment>();

            return segments.Select(segment => new ReservationFlightSegment
            {
                FlightSegment = new Service.Presentation.SegmentModel.FlightSegment
                {
                    ArrivalAirport = new Service.Presentation.CommonModel.AirportModel.Airport { IATACode = segment.FlightSegment.ArrivalAirport.IATACode },
                    DepartureAirport = new Service.Presentation.CommonModel.AirportModel.Airport { IATACode = segment.FlightSegment.DepartureAirport.IATACode },
                    DepartureDateTime = segment.FlightSegment.DepartureDateTime,
                    FlightNumber = segment.FlightSegment.FlightNumber,
                    InstantUpgradable = false,
                    IsInternational = segment.FlightSegment.IsInternational,
                    OperatingAirlineCode = segment.FlightSegment.OperatingAirlineCode,
                    UpgradeEligibilityStatus = Service.Presentation.CommonEnumModel.UpgradeEligibilityStatus.Unknown,
                    UpgradeVisibilityType = Service.Presentation.CommonEnumModel.UpgradeVisibilityType.None,
                    BookingClasses = new Collection<BookingClass>(segment.FlightSegment.BookingClasses.Where(y => y != null && y.Cabin != null).Select(y => new BookingClass { Cabin = new Service.Presentation.CommonModel.AircraftModel.Cabin { Name = y.Cabin.Name }, Code = y.Code }).ToList())
                }
            }).ToList();
        }

        private async Task<MOBMobileCMSContentMessages> GetCMSContentMessageByKey(string Key, MOBRequest request, Session session)
        {

            CSLContentMessagesResponse cmsResponse = new CSLContentMessagesResponse();
            MOBMobileCMSContentMessages cmsMessage = null;
            List<CMSContentMessage> cmsMessages = null;
            try
            {
                var cmsContentCache = await _cachingService.GetCache<string>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID") + "MOBCSLContentMessagesResponse", request.TransactionId);
                try
                {
                    if (!string.IsNullOrEmpty(cmsContentCache))
                        cmsResponse = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(cmsContentCache);
                }
                catch { cmsContentCache = null; }

                if (string.IsNullOrEmpty(cmsContentCache) || Convert.ToBoolean(cmsResponse.Status) == false || cmsResponse.Messages == null)
                    cmsResponse = await _travelerCSL.GetBookingRTICMSContentMessages(request, session);

                cmsMessages = (cmsResponse != null && cmsResponse.Messages != null && cmsResponse.Messages.Count > 0) ? cmsResponse.Messages : null;
                if (cmsMessages != null)
                {
                    var message = cmsMessages.Find(m => m.Title.Equals(Key));
                    if (message != null)
                    {
                        cmsMessage = new MOBMobileCMSContentMessages()
                        {
                            HeadLine = message.Headline,
                            ContentFull = message.ContentFull,
                            ContentShort = message.ContentShort
                        };
                    }
                }
            }
            catch (Exception)
            { }
            return cmsMessage;
        }

        public bool IsEnableWheelchairLinkUpdate(Session session)
        {
            return _configuration.GetValue<bool>("EnableWheelchairLinkUpdate") &&
                   session.CatalogItems != null &&
                   session.CatalogItems.Count > 0 &&
                   session.CatalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableWheelchairLinkUpdate).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableWheelchairLinkUpdate).ToString())?.CurrentValue == "1";
        }

        private async Task<List<TravelSpecialNeed>> GetOfferedSpecialRequests(MultiCallResponse flightshoppingReferenceData, MOBSHOPReservation reservation = null, SelectTripRequest selectRequest = null, Session session = null)
        {
            if (flightshoppingReferenceData == null || flightshoppingReferenceData.SpecialRequestResponses == null || !flightshoppingReferenceData.SpecialRequestResponses.Any()
                || flightshoppingReferenceData.SpecialRequestResponses[0].SpecialRequests == null || !flightshoppingReferenceData.SpecialRequestResponses[0].SpecialRequests.Any())
                return null;

            var specialRequests = new List<TravelSpecialNeed>();
            var specialNeedType = TravelSpecialNeedType.SpecialRequest.ToString();
            TravelSpecialNeed createdWheelChairItem = null;
            Func<string, string, string, TravelSpecialNeed> createSpecialNeedItem = (code, value, desc)
                => new TravelSpecialNeed { Code = code, Value = value, DisplayDescription = desc, RegisterServiceDescription = desc, Type = specialNeedType };


            foreach (var specialRequest in flightshoppingReferenceData.SpecialRequestResponses[0].SpecialRequests.Where(x => x.Genre != null && !string.IsNullOrWhiteSpace(x.Genre.Description) && !string.IsNullOrWhiteSpace(x.Code)))
            {
                if (specialRequest.Genre.Description.Equals("General"))
                {
                    var sr = createSpecialNeedItem(specialRequest.Code, specialRequest.Value, specialRequest.Description);

                    if (specialRequest.Code.StartsWith("DPNA", StringComparison.OrdinalIgnoreCase)) // add info message for DPNA_1, and DPNA_2 request
                        sr.Messages = new List<Model.Common.MOBItem> { new Model.Common.MOBItem { CurrentValue = _configuration.GetValue<string>("SSR_DPNA_Message") } };

                    await SetTaskTrainedServiceAnimalMessage(specialRequest, sr, reservation, selectRequest, null, selectRequest, session);

                    if (sr.Code != "OTHS")
                        specialRequests.Add(sr);
                }
                else if (specialRequest.Genre.Description.Equals("WheelchairReason"))
                {
                    if (createdWheelChairItem == null)
                    {
                        createdWheelChairItem = createSpecialNeedItem(_configuration.GetValue<string>("SSRWheelChairDescription"), null, _configuration.GetValue<string>("SSRWheelChairDescription"));
                        createdWheelChairItem.SubOptionHeader = _configuration.GetValue<string>("SSR_WheelChairSubOptionHeader");

                        // MOBILE-23726
                        if (IsEnableWheelchairLinkUpdate(session))
                        {
                            var sdlKeyForWheelchairLink = _configuration.GetValue<string>("FSRSpecialTravelNeedsWheelchairLinkKey");
                            MOBMobileCMSContentMessages message = null;
                            if (!string.IsNullOrEmpty(sdlKeyForWheelchairLink))
                            {
                                message = await GetCMSContentMessageByKey(sdlKeyForWheelchairLink, selectRequest, session);
                            }
                            createdWheelChairItem.InformationLink = message?.ContentFull ?? (_configuration.GetValue<string>("WheelchairLinkUpdateFallback") ?? "");
                        }

                        specialRequests.Add(createdWheelChairItem);
                    }

                    var wheelChairSubItem = createSpecialNeedItem(specialRequest.Code, specialRequest.Value, specialRequest.Description);

                    if (createdWheelChairItem.SubOptions == null)
                    {
                        createdWheelChairItem.SubOptions = new List<TravelSpecialNeed> { wheelChairSubItem };
                    }
                    else
                    {
                        createdWheelChairItem.SubOptions.Add(wheelChairSubItem);
                    }
                }
                else if (specialRequest.Genre.Description.Equals("WheelchairType"))
                {
                    specialRequests.Add(createSpecialNeedItem(specialRequest.Code, specialRequest.Value, specialRequest.Description));
                }
            }

            return specialRequests;
        }

        private async Task SetTaskTrainedServiceAnimalMessage(Characteristic specialRequest, TravelSpecialNeed sr, MOBSHOPReservation reservation = null, SelectTripRequest selectRequest = null,
          TraceSwitch _traceSwitch = null, MOBRequest request = null, Session session = null)
        {
            if (selectRequest != null && reservation != null && _shoppingUtility.IsServiceAnimalEnhancementEnabled(selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.CatalogItems) && specialRequest.Code.StartsWith(_configuration.GetValue<string>("TasktrainedServiceAnimalCODE"), StringComparison.OrdinalIgnoreCase)) // add info message for Task-trained service animal
            {

                List<CMSContentMessage> lstMessages = await _ffcShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");

                //First 2 lines only if International , HNL or GUM 
                if (reservation?.ISInternational == true
                    || (!string.IsNullOrWhiteSpace(reservation?.Trips?.FirstOrDefault()?.Origin) && _configuration.GetValue<string>("DisableServiceAnimalAirportCodes")?.Contains(reservation?.Trips?.FirstOrDefault()?.Origin) == true)
                    || (!string.IsNullOrWhiteSpace(reservation?.Trips?.FirstOrDefault()?.Destination) && _configuration.GetValue<string>("DisableServiceAnimalAirportCodes")?.Contains(reservation?.Trips?.FirstOrDefault()?.Destination) == true))
                {
                    sr.IsDisabled = true;
                    sr.Messages = new List<MOBItem> { new MOBItem {
                                Id = "ESAN_SUBTITLE",
                                CurrentValue =_shoppingUtility.GetSDLStringMessageFromList(lstMessages, "TravelNeeds_TaskTrainedDog_Screen_Content_INT_MOB")  } };
                }
                sr.SubOptions = new List<TravelSpecialNeed>();

                sr.SubOptions.Add(new TravelSpecialNeed
                {
                    Value = sr.Value,
                    Code = "SVAN",
                    Type = TravelSpecialNeedType.ServiceAnimalType.ToString(),
                    DisplayDescription = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "TravelNeeds_TaskTrainedDog_Screen_Content2_MOB"),
                    RegisterServiceDescription = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "TravelNeeds_TaskTrainedDog_Screen_Title_MOB")
                });
                if (sr.Messages == null) sr.Messages = new List<MOBItem>();
                ServiceAnimalDetailsScreenMessages(sr.Messages);

                //TODO, ONCE FS changes are ready we do not need below
                sr.Code = "SVAN";
                sr.DisplayDescription = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "TravelNeeds_TaskTrainedDog_Screen_Title_MOB");
                sr.RegisterServiceDescription = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "TravelNeeds_TaskTrainedDog_Screen_Title_MOB");
            }
        }

        private void ServiceAnimalDetailsScreenMessages(List<MOBItem> messages)
        {

            messages.Add(new MOBItem
            {
                Id = "ESAN_HDR",
                CurrentValue = "Additional step required before traveling with a service dog. \n <br /> <br /> Please complete the service dog request form located in Trip Details before arriving at the airport.",
            });
            messages.Add(new MOBItem
            {
                Id = "ESAN_FTR",
                CurrentValue = "\n<br /><br />We no longer accept emotional support animals due to new Department of Transportation regulations.\n<br /><br /><a href=\"https://www.united.com/ual/en/US/fly/travel/special-needs/disabilities/assistance-animals.html\">Review our service animal policy</a>",
            });
        }

        public Reservation ReservationToPersistReservation(MOBSHOPAvailability availability)
        {
            var reservation = new Reservation
            {
                IsSSA = availability.Reservation.IsSSA,
                IsELF = availability.Reservation.IsELF,
                IsMetaSearch = availability.Reservation.IsMetaSearch,
                AwardTravel = availability.Reservation.AwardTravel,
                CartId = availability.Reservation.CartId,
                ClubPassPurchaseRequest = availability.Reservation.ClubPassPurchaseRequest,
                CreditCards = availability.Reservation.CreditCards,
                CreditCardsAddress = availability.Reservation.CreditCardsAddress,
                FareLock = availability.Reservation.FareLock,
                FareRules = availability.Reservation.FareRules,
                FlightShareMessage = availability.Reservation.FlightShareMessage,
                GetALLSavedTravelers = availability.Reservation.GetALLSavedTravelers,
                IneligibleToEarnCreditMessage = availability.Reservation.IneligibleToEarnCreditMessage,
                ISFlexibleSegmentExist = availability.Reservation.ISFlexibleSegmentExist,
                ISInternational = availability.Reservation.ISInternational,
                IsRefundable = availability.Reservation.IsRefundable,
                IsSignedInWithMP = availability.Reservation.IsSignedInWithMP,
                LMXFlights = availability.Reservation.LMXFlights,
                LMXTravelers = availability.Reservation.lmxtravelers,
                NumberOfTravelers = availability.Reservation.NumberOfTravelers,
                PKDispenserPublicKey = availability.Reservation.PKDispenserPublicKey,
                PointOfSale = availability.Reservation.PointOfSale,
                Prices = availability.Reservation.Prices,
                ReservationEmail = availability.Reservation.ReservationEmail,
                ReservationPhone = availability.Reservation.ReservationPhone,
                SearchType = availability.Reservation.SearchType,
                SeatPrices = availability.Reservation.SeatPrices,
                SessionId = availability.Reservation.SessionId,
                MetaSessionId = availability.Reservation.MetaSessionId,
                ELFMessagesForRTI = availability.Reservation.ELFMessagesForRTI,
                Taxes = availability.Reservation.Taxes,
                TCDAdvisoryMessages = availability.Reservation.TCDAdvisoryMessages,
                SeatAssignmentMessage = availability.Reservation.SeatAssignmentMessage,
                AlertMessages = availability.Reservation.AlertMessages,
                ShopReservationInfo = availability.Reservation.ShopReservationInfo,
                ShopReservationInfo2 = availability.Reservation.ShopReservationInfo2,
                CheckedbagChargebutton = availability.Reservation.CheckedbagChargebutton,
                IsBookingCommonFOPEnabled = availability.Reservation.IsBookingCommonFOPEnabled,
                IsReshopCommonFOPEnabled = availability.Reservation.IsReshopCommonFOPEnabled,
                IsCubaTravel = availability.Reservation.IsCubaTravel,
                CubaTravelInfo = availability.Reservation.CubaTravelInfo,
                HasJSXSegment = availability.Reservation.HasJSXSegment
            };
            if (availability.Reservation.Travelers != null && availability.Reservation.Travelers.Count > 0)
            {
                reservation.Travelers = new SerializableDictionary<string, MOBSHOPTraveler>();
                foreach (var traveler in availability.Reservation.Travelers)
                {
                    reservation.Travelers.Add(traveler.Key, traveler);
                }
            }
            if (availability.Reservation.TravelersCSL != null && availability.Reservation.TravelersCSL.Count > 0)
            {
                reservation.TravelersCSL = new SerializableDictionary<string, MOBCPTraveler>();
                foreach (var travelersCSL in availability.Reservation.TravelersCSL)
                {
                    reservation.TravelersCSL.Add(travelersCSL.Key, travelersCSL);
                }
            }
            reservation.TravelersRegistered = availability.Reservation.TravelersRegistered;
            reservation.TravelOptions = availability.Reservation.TravelOptions;
            reservation.Trips = availability.Reservation.Trips;
            reservation.UnregisterFareLock = availability.Reservation.UnregisterFareLock;
            if (!string.IsNullOrEmpty(availability.Reservation.RecordLocator))
            {
                if (availability.Reservation.TravelersCSL != null && availability.Reservation.TravelersCSL.Count > 0)
                {
                    reservation.TravelerKeys = new List<string>() { };
                    foreach (var travelersCSL in availability.Reservation.TravelersCSL)
                    {
                        reservation.TravelerKeys.Add(travelersCSL.Key);
                    }
                }
                reservation.IsRedirectToSecondaryPayment = availability.Reservation.IsRedirectToSecondaryPayment;
                reservation.RecordLocator = availability.Reservation.RecordLocator;
                reservation.Messages = availability.Reservation.Messages;
            }

            reservation.TripInsuranceFile = new TripInsuranceFile() { TripInsuranceBookingInfo = availability.Reservation.TripInsuranceInfoBookingPath, TripInsuranceInfo = availability.Reservation.TripInsuranceInfo };
            return reservation;
        }
        private void FireForGetInFlightCLEligibilityCheck(Reservation reservation, MOBRequest request, Session session)
        {
            if (!reservation.IsReshopChange)
            {
                System.Threading.Tasks.Task.Factory.StartNew(() =>
                {
                    _merchandizingServices.IsEligibleInflightContactlessPayment(reservation, request, session);
                });
            }
        }

        private async System.Threading.Tasks.Task SetAvailabilityELFProperties(MOBSHOPAvailability availability, bool isMultiTravelers, bool isSSA)
        {
            if (availability != null)
            {
                availability.ELFShopMessages = await SetElfShopMessage(isMultiTravelers, isSSA);
                availability.ELFShopOptions = await ParseELFShopOptions(isSSA);
            }
        }


        private async System.Threading.Tasks.Task SetAvailabilityBeLiteProperties(MOBSHOPAvailability availability, Session session, MOBRequest mobRequest)
        {
            var getProductInfoForFsrdRequest = new MOBSHOPGetProductInfoForFSRDRequest
            {
                Application = mobRequest.Application,
                DeviceId = mobRequest.DeviceId,
                LanguageCode = mobRequest.LanguageCode,
                CountryCode = availability.Reservation.PointOfSale,
                FlightHash = availability.Reservation.Trips.Last().FlattenedFlights.Last().Flights.First().FlightHash,
                SearchType = availability.Reservation.SearchType,
                NumberOfAdults = availability.Reservation.NumberOfTravelers,
                SessionId = availability.Reservation.SessionId,
                IsIBE = availability.Reservation.ShopReservationInfo2.IsIBE
            };

            var productInfoForFsrd = await GetProductInfoForFSRD(session, getProductInfoForFsrdRequest, GetProductCode(availability));
            availability.ELFShopMessages = productInfoForFsrd.IBELiteShopMessages;
            availability.ELFShopOptions = productInfoForFsrd.IBELiteShopOptions;
        }
        private bool EnableMetaPathSavedBooking()
        {
            return _configuration.GetValue<bool>("EnableMetaPathSavedBooking");
        }

        private MOBSHOPUnfinishedBooking MapUnfinishedBookingFromMOBSHOPReservation(MOBSHOPReservation reservation)
        {
            var result = new MOBSHOPUnfinishedBooking
            {
                IsELF = reservation.IsELF,
                CountryCode = reservation.PointOfSale,
                SearchType = reservation.SearchType,
                NumberOfAdults = reservation.NumberOfTravelers,
                TravelerTypes = reservation.ShopReservationInfo2.TravelerTypes,
                SearchExecutionDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time")).ToString("G")
            };
            result.Trips = reservation.Trips.Select(MapToUnfinishedBookingTripFromMOBSHOPTrip).ToList();

            return result;
        }

        private async Task<List<MOBSHOPCountry>> GetNationalityResidenceCountries(int applicationId, string deviceId, string appVersion, string transactionId, string sessionID, string token)
        {
            List<MOBSHOPCountry> lstNationalityResidenceCountries = null;


            try
            {
                string logAction = "NationalityResidenceCountries";


                var response = await _referencedataService.GetNationalityResidence<List<United.Service.Presentation.CommonModel.Characteristic>>(logAction, token, sessionID);


                lstNationalityResidenceCountries = new List<MOBSHOPCountry>();
                List<United.Service.Presentation.CommonModel.Characteristic> lst = response;

                if (lst != null && lst.Count > 0)
                {
                    foreach (var l in lst)
                    {
                        lstNationalityResidenceCountries.Add(new MOBSHOPCountry() { CountryCode = l.Code, Name = l.Description });
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogInformation("GetNationalityResidenceCountries Exception {sessionId} and {Message}", sessionID, ex.Message);

            }
            if (deviceId.ToUpper().Trim() != "SCHEDULED_PublicKey_UPDADE_JOB".ToUpper().Trim())
            {
                //System.Threading.Tasks.Task.Factory.StartNew(() => Authentication.Write(logEntries));
            }

            return lstNationalityResidenceCountries;
        }

        private bool IsNatAndCRExists(string origin, string destination, List<string> NatAndCRList)
        {
            bool isNatAndCRExists = false;
            if (!string.IsNullOrEmpty(origin) && !string.IsNullOrEmpty(destination) && NatAndCRList != null && NatAndCRList.Count > 0)
            {
                if (NatAndCRList != null && (NatAndCRList.Exists(p => p == origin) || NatAndCRList.Exists(p => p == destination)))
                {
                    isNatAndCRExists = true;
                }
            }
            return isNatAndCRExists;
        }

        private MOBSHOPUnfinishedBookingTrip MapToUnfinishedBookingTripFromMOBSHOPTrip(MOBSHOPTrip trip)
        {
            var ubTrip = new MOBSHOPUnfinishedBookingTrip
            {
                DepartDate = trip.FlattenedFlights.First().Flights.First().DepartDate,
                DepartTime = trip.FlattenedFlights.First().Flights.First().DepartTime,
                ArrivalDate = trip.FlattenedFlights.First().Flights.First().DestinationDate,
                ArrivalTime = trip.FlattenedFlights.First().Flights.First().DestinationTime,
                Destination = trip.Destination,
                Origin = trip.Origin,
                Flights = trip.FlattenedFlights.First().Flights.Select(MapToUnfinishedBookingFlightFromMOBSHOPFlight).ToList(),
            };

            return ubTrip;
        }

        private MOBSHOPUnfinishedBookingFlight MapToUnfinishedBookingFlightFromMOBSHOPFlight(MOBSHOPFlight shopFlight)
        {
            var ubFlight = new MOBSHOPUnfinishedBookingFlight
            {
                BookingCode = shopFlight.ServiceClass,
                DepartDateTime = shopFlight.DepartureDateTime,
                Origin = shopFlight.Origin,
                Destination = shopFlight.Destination,
                FlightNumber = shopFlight.FlightNumber,
                MarketingCarrier = shopFlight.MarketingCarrier,
                ProductType = shopFlight.ShoppingProducts.First().Type,
            };

            if (shopFlight.Connections != null)
                ubFlight.Connections = shopFlight.Connections.Select(MapToUnfinishedBookingFlightFromMOBSHOPFlight).ToList();

            return ubFlight;
        }

        private string ValidateMOBSHOPGetProductInfoForFSRDRequest(MOBSHOPGetProductInfoForFSRDRequest request)
        {
            string errorMsg = null;
            var kv = new Dictionary<string, string>
            {
                { "SessionId", request.SessionId },
                { "CountryCode", request.CountryCode },
                { "FlightHash", request.FlightHash },
                { "SearchType", request.SearchType }
            };

            var invalidValue = kv.FirstOrDefault(pair => string.IsNullOrWhiteSpace(pair.Value));
            if (!invalidValue.Equals(default(KeyValuePair<string, string>)))
                errorMsg = string.Format("Request's {0} is empty.", invalidValue.Key);
            else if ("RT,OW,MD".IndexOf(request.SearchType.Trim().ToUpper()) < 0)
                errorMsg = string.Format("Request's SearchType is invalid.");

            return errorMsg;
        }

        private async Task<MOBSHOPGetProductInfoForFSRDResponse> GetProductInfoForFSRD(Session session, MOBSHOPGetProductInfoForFSRDRequest request, string shoppingProductCode = "")
        {
            string requestValidationErrors = ValidateMOBSHOPGetProductInfoForFSRDRequest(request);
            if (!string.IsNullOrWhiteSpace(requestValidationErrors))
                throw new MOBUnitedException(requestValidationErrors);

            var response = new MOBSHOPGetProductInfoForFSRDResponse();
            BasicEconomyEntitlementResponse ibeEntitlementsResponse = null;
            var ibeBagFee = _configuration.GetValue<string>("IBELiteProdutDefaultPrice");
            var ibeBagFeeCurrency = string.Empty;
            var ecoBagFee = string.Empty;
            try
            {
                ibeEntitlementsResponse = await GetBasicEconomyEntitlements(session, request);
                var success = ValidateBasicEconomyEntitlementResponse(ibeEntitlementsResponse);

                ibeBagFee = GetIBELitePBagFeeAmount(ibeEntitlementsResponse);
                ibeBagFeeCurrency = GetIBELitePBagFeeCurrecy(ibeEntitlementsResponse);
                if (!request.IsIBE)
                {
                    ecoBagFee = GetIBELiteEconomyBagFeeAmount(ibeEntitlementsResponse);
                }
            }
            catch (System.Net.WebException wex)
            {
                //var errorResponse = new StreamReader(wex.Response.GetResponseStream()).ReadToEnd();

                //if (this.traceSwitch.TraceWarning)
                //{
                //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(request.SessionId, "GetProductInfoForFSRD", "MOBUnitedException", request.Application.Id, request.Application.Version.Major, request.DeviceId, errorResponse));
                //}
            }
            catch (MOBUnitedException ex)
            {
                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry(request.SessionId, "GetProductInfoForFSRD", "MOBUnitedException", request.Application.Id, request.Application.Version.Major, request.DeviceId, new MOBExceptionWrapper(ex)));
                //}
            }
            catch (Exception ex)
            {
                //if (traceSwitch.TraceError)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry(request.SessionId, "GetProductInfoForFSRD", "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, new MOBExceptionWrapper(ex)));
                //}
                _logger.LogError("GetProductInfoForFSRD Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), request.SessionId);
                _logger.LogError("GetProductInfoForFSRD Error {exception} and {sessionId}", ex.Message, request.SessionId);

            }

            var currencySymbol = !string.IsNullOrEmpty(ibeBagFeeCurrency) ? _shoppingUtility.GetCurrencySymbol(TopHelper.GetCultureInfo(ibeBagFeeCurrency)) : "$";

            var ibeBagFeeOnConfirmFare = !string.IsNullOrEmpty(ibeBagFeeCurrency) ? string.Format("{0}{1}", currencySymbol, ibeBagFee) : _configuration.GetValue<string>("IBELiteProdutDefaultPrice");
            var ibeBagFeeOnCompareFares = !string.IsNullOrEmpty(ibeBagFeeCurrency) ? string.Format("{0}{1} ", currencySymbol, ibeBagFee) : string.Empty;

            if (_shoppingUtility.EnableIBEFull() && request.IsIBE)
            {
                if (!_shoppingUtility.EnablePBE())
                {
                    response.IBELiteShopOptions = await GetIBEFullShoppingOptions();
                    response.IBELiteShopMessages = await GetIBEFullShoppingMessages(request);
                    if (_configuration.GetValue<bool>("BasicEconomyContentChange"))
                    {
                        ShoppingResponse shop = new ShoppingResponse();
                        try
                        {
                            shop = await _sessionHelperService.GetSession<ShoppingResponse>(session.SessionId, shop.ObjectName, new List<string> { session.SessionId, shop.ObjectName }).ConfigureAwait(false);
                            var differentYearcount = shop.Request.Trips.Select(d => Convert.ToDateTime(d.DepartDate).Year).Distinct().Count();
                            if (shop != null && shop.Request.Trips != null && differentYearcount > 1 && DateTime.Now.Year == 2019)
                            {
                                var presentYear = shop.Request.Trips.Where(d => Convert.ToDateTime(d.DepartDate).Year == 2019).FirstOrDefault().DepartDate;
                                var futureYear = shop.Request.Trips.Where(d => Convert.ToDateTime(d.DepartDate).Year == 2020).FirstOrDefault().DepartDate;
                                if (!string.IsNullOrEmpty(presentYear) && !string.IsNullOrEmpty(futureYear))
                                {
                                    if (response.IBELiteShopMessages.Any(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_19_20"))
                                    {
                                        response.IBELiteShopMessages.RemoveAll(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter");
                                        response.IBELiteShopMessages.RemoveAll(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_2020");
                                        (response.IBELiteShopMessages.Where(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_19_20").FirstOrDefault().Id) = "ELFConfirmFareTypeFooter";
                                    }
                                }
                            }
                            else
                            {
                                if (shop != null && shop.Request.Trips != null && Convert.ToDateTime(shop.Request.Trips[0].DepartDate).Year == 2019)
                                {
                                    response.IBELiteShopMessages.RemoveAll(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_2020");
                                    response.IBELiteShopMessages.RemoveAll(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_19_20");
                                }
                                else if (shop != null && shop.Request.Trips != null && Convert.ToDateTime(shop.Request.Trips[0].DepartDate).Year >= 2020)
                                {
                                    if (response.IBELiteShopMessages.Any(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_2020"))
                                    {
                                        response.IBELiteShopMessages.RemoveAll(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter");
                                        response.IBELiteShopMessages.RemoveAll(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_19_20");
                                        (response.IBELiteShopMessages.Where(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_2020").FirstOrDefault().Id) = "ELFConfirmFareTypeFooter";
                                    }
                                }
                            }
                        }
                        catch (System.Exception) { }
                    }
                }
            }
            else
            {
                var ecoBagFeeFull = string.Format("{0}{1}", currencySymbol, !string.IsNullOrWhiteSpace(ecoBagFee) ? ecoBagFee : _configuration.GetValue<string>("IBELiteEconomyBagPrice"));

                response.IBELiteShopOptions = await GetIBELiteShoppingOptions(ibeBagFeeOnConfirmFare, string.IsNullOrEmpty(ibeBagFeeCurrency) ? string.Empty : GetIBELiteTripTypeDescription(request.SearchType), ecoBagFeeFull);
                response.IBELiteShopMessages = await GetIBELiteShoppingMessages(request);
            }
            response.ShoppingProducts = await GetIBELiteShoppingProducts(request, ibeBagFeeOnCompareFares, GetIBELiteTripTypeDescription(request.SearchType));

            if (_shoppingUtility.EnablePBE())
            {
                string productCode = string.IsNullOrEmpty(shoppingProductCode)
                                         ? response?.ShoppingProducts?.First(p => p != null && p.IsIBE).ProductCode
                                         : shoppingProductCode;
                response.IBELiteShopOptions = await GetIBEFullShoppingOptions(productCode);
                if (_shoppingUtility.EnableOptoutScreenHyperlinkSupportedContent(request.Application.Id, request.Application.Version.Major))
                {
                    response.IBELiteShopMessages = await GetIBEFullShoppingMessages(request, productCode, productCode + "_CONFIRMATION_PAGE_HEADER_FOOTER_V1");
                }
                else
                {
                    response.IBELiteShopMessages = await GetIBEFullShoppingMessages(request, productCode);
                }
            }

            return response;
        }

        private async Task<List<MOBItem>> GetIBEFullShoppingMessages(MOBSHOPGetProductInfoForFSRDRequest request, string productCode)
        {
            var msgs = (await GetIBEFullConfirmPageHeaderFooterDocs(productCode))
                          .Select(doc => new MOBItem { Id = doc.Title, CurrentValue = doc.LegalDocument })
                          .ToList();

            if (request.NumberOfAdults == 1)
            {
                var elfConfirmFareTypeTitle = msgs.FirstOrDefault(msg => msg != null && msg.Id.Equals("ELFConfirmFareTypeTitle", StringComparison.OrdinalIgnoreCase));

                if (elfConfirmFareTypeTitle != null)
                    elfConfirmFareTypeTitle.CurrentValue = string.Empty;
            }

            return msgs;
        }
        private async Task<List<MOBLegalDocument>> GetIBEFullConfirmPageHeaderFooterDocs(string productCode)
        {
            return await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(productCode, _headers.ContextValues.SessionId, true);
        }

        private async Task<List<Option>> GetIBEFullShoppingOptions(string productCode)
        {
            return (await GetIBEFullConfirmFarePageDocs(productCode))
                            .Where(o => o != null)
                            .OrderBy(o => Convert.ToInt32(o.Title))
                            .Select(o => o.LegalDocument.Split('|'))
                            .Select(doc => new Option
                            {
                                OptionDescription = doc[0],
                                AvailableInElf = Convert.ToBoolean(doc[1]),
                                AvailableInEconomy = Convert.ToBoolean(doc[2]),
                                fareSubDescriptionELF = null,
                                fareSubDescriptionEconomy = null,
                                OptionIcon = doc[5]
                            })
                            .ToList();
        }

        private async Task<List<MOBLegalDocument>> GetIBEFullConfirmFarePageDocs(string productCode)
        {
            return await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(productCode + "_CONFIRMATION_PAGE_OPTIONS", _headers.ContextValues.TransactionId, true).ConfigureAwait(false);
        }

        private async Task<List<MOBItem>> GetIBEFullShoppingMessages(MOBSHOPGetProductInfoForFSRDRequest request, string productCode, string title)
        {
            var msgs = (await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(title, _headers.ContextValues.SessionId, true).ConfigureAwait(false))
                          .Select(doc => new MOBItem { Id = doc.Title, CurrentValue = doc.LegalDocument })
                          .ToList();

            if (request.NumberOfAdults == 1)
            {
                var elfConfirmFareTypeTitle = msgs.FirstOrDefault(msg => msg != null && msg.Id.Equals("ELFConfirmFareTypeTitle", StringComparison.OrdinalIgnoreCase));

                if (elfConfirmFareTypeTitle != null)
                    elfConfirmFareTypeTitle.CurrentValue = string.Empty;
            }

            return msgs;
        }

        private async Task<List<Model.Shopping.MOBSHOPShoppingProduct>> GetIBELiteShoppingProducts(MOBSHOPGetProductInfoForFSRDRequest request, string ibeBagFee, string ibeTripTypeDesc)
        {
            if (string.IsNullOrEmpty(request.TripId))
                return null;

            var persistedAvail = await GetLastTripAvailabilityFromPersist(request.TripId, request.SessionId);

            List<Model.Shopping.MOBSHOPFlattenedFlight> shopFlattenedFlightList = null;
            if (persistedAvail == null || persistedAvail.Trip.LastTripIndexRequested == 1)
            {
                var persistShopFlattenedFlightList = new Model.Shopping.MOBSHOPFlattenedFlightList();
                persistShopFlattenedFlightList = await _sessionHelperService.GetSession<MOBSHOPFlattenedFlightList>(request.SessionId, persistShopFlattenedFlightList.ObjectName, new List<string> { request.SessionId, persistShopFlattenedFlightList.ObjectName }).ConfigureAwait(false);//, persistShopFlattenedFlightList.GetType().FullName, false);

                if (persistShopFlattenedFlightList != null)
                    shopFlattenedFlightList = persistShopFlattenedFlightList.FlattenedFlightList;

                if (shopFlattenedFlightList == null || shopFlattenedFlightList.Count == 0)
                {
                    shopFlattenedFlightList = await GetFlattendFlightsFromAvailability(request.SessionId);
                }
            }

            if (shopFlattenedFlightList == null && persistedAvail == null)
                return null;

            if (shopFlattenedFlightList == null)
                shopFlattenedFlightList = persistedAvail.Trip.FlattenedFlights;

            var products = shopFlattenedFlightList.First(f => f.FlightHash.Equals(request.FlightHash, StringComparison.OrdinalIgnoreCase))
                            .Flights
                            .First()
                            .ShoppingProducts;
            if (_shoppingUtility.EnableIBEFull() && request.IsIBE)
            {
                var ibeProducts = string.Format(_configuration.GetValue<string>("IBEFulldetails"), ibeBagFee, ibeTripTypeDesc);
                products.First().ProductDetail.ProductDetails = ibeProducts.Split('|').ToList();
            }
            else
            {
                var ibeProducts = string.Format(string.Join("^", products.First().ProductDetail.ProductDetails), ibeBagFee, ibeTripTypeDesc);
                products.First().ProductDetail.ProductDetails = ibeProducts.Split('^').ToList();
            }

            return products;
        }

        private async Task<List<MOBSHOPFlattenedFlight>> GetFlattendFlightsFromAvailability(string sessionID)
        {
            LatestShopAvailabilityResponse persistAvailability = new LatestShopAvailabilityResponse();
            persistAvailability = await _sessionHelperService.GetSession<LatestShopAvailabilityResponse>(sessionID, persistAvailability.ObjectName, new List<string> { sessionID, persistAvailability.ObjectName }).ConfigureAwait(false);
            var persistFFlights = persistAvailability.AvailabilityList[persistAvailability.AvailabilityList.Count.ToString()].Trip.FlattenedFlights;

            return persistFFlights;
        }

        private async Task<MOBSHOPAvailability> GetLastTripAvailabilityFromPersist(string tripID, string sessionID)
        {
            MOBSHOPAvailability lastTripAvailability = null;

            if (string.IsNullOrWhiteSpace(tripID))
                return lastTripAvailability;

            var persistAvailability = new LatestShopAvailabilityResponse();
            persistAvailability = await _sessionHelperService.GetSession<LatestShopAvailabilityResponse>(sessionID, persistAvailability.ObjectName, new List<string> { sessionID, persistAvailability.ObjectName }).ConfigureAwait(false);//, false);

            if (persistAvailability != null && persistAvailability.AvailabilityList != null && persistAvailability.AvailabilityList.Any())
            {
                lastTripAvailability = persistAvailability.AvailabilityList.Values.FirstOrDefault(x => x.Trip.TripId.Equals(tripID, StringComparison.OrdinalIgnoreCase));
            }

            return lastTripAvailability;
        }

        private async Task<List<MOBItem>> GetIBELiteShoppingMessages(MOBSHOPGetProductInfoForFSRDRequest request)
        {
            var msgs = (await GetIBELiteConfirmPageHeaderFooterDocs())
                          .Select(doc => new MOBItem { Id = doc.Title, CurrentValue = doc.LegalDocument })
                          .ToList();

            if (request.NumberOfAdults == 1)
            {
                var elfConfirmFareTypeTitle = msgs.FirstOrDefault(msg => msg != null && msg.Id.Equals("ELFConfirmFareTypeTitle", StringComparison.OrdinalIgnoreCase));

                if (elfConfirmFareTypeTitle != null)
                    elfConfirmFareTypeTitle.CurrentValue = string.Empty;
            }

            return msgs;
        }

        private async Task<List<MOBLegalDocument>> GetIBELiteConfirmPageHeaderFooterDocs()
        {
            return await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles("IBELITE_CONFIRMATION_PAGE_HEADER_FOOTER", _headers.ContextValues.SessionId, true);
        }

        private string GetIBELiteTripTypeDescription(string searchType)
        {
            if (string.IsNullOrEmpty(searchType))
                return string.Empty;

            string tripTypeDesc = string.Empty;
            switch (searchType.ToUpper().Trim())
            {
                case "RT":
                    tripTypeDesc = "(roundtrip)";
                    break;
                case "OW":
                    tripTypeDesc = "(one way)";
                    break;
                case "MD":
                    tripTypeDesc = "(each way)";
                    break;
                default:
                    break;
            }

            return tripTypeDesc;
        }

        private async Task<List<Option>> GetIBELiteShoppingOptions(string ibeBagFee, string ibeTripTypeDesc, string ecoBagFee)
        {
            return (await GetIBELiteConfirmFarePageDocs())
                            .Where(o => o != null)
                            .OrderBy(o => Convert.ToInt32(o.Title))
                            .Select(o => o.LegalDocument.Split('|'))
                            .Select(doc => new Option
                            {
                                OptionDescription = doc[0],
                                AvailableInElf = Convert.ToBoolean(doc[1]),
                                AvailableInEconomy = Convert.ToBoolean(doc[2]),
                                fareSubDescriptionELF = string.IsNullOrWhiteSpace(doc[3]) ? null : string.Format("{0} {1}", ibeBagFee, ibeTripTypeDesc),
                                fareSubDescriptionEconomy = string.IsNullOrWhiteSpace(doc[4]) ? null : ecoBagFee,
                                OptionIcon = doc[5]
                            })
                            .ToList();
        }

        private async Task<List<MOBLegalDocument>> GetIBELiteConfirmFarePageDocs()
        {
            return await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles("IBELITE_CONFIRMATION_PAGE_OPTIONS", _headers.ContextValues.SessionId, true);
        }

        private async Task<List<MOBItem>> GetIBEFullShoppingMessages(MOBSHOPGetProductInfoForFSRDRequest request)
        {
            var msgs = (await GetIBEFullConfirmPageHeaderFooterDocs())
                          .Select(doc => new MOBItem { Id = doc.Title, CurrentValue = doc.LegalDocument })
                          .ToList();

            if (request.NumberOfAdults == 1)
            {
                var elfConfirmFareTypeTitle = msgs.FirstOrDefault(msg => msg != null && msg.Id.Equals("ELFConfirmFareTypeTitle", StringComparison.OrdinalIgnoreCase));

                if (elfConfirmFareTypeTitle != null)
                    elfConfirmFareTypeTitle.CurrentValue = string.Empty;
            }

            return msgs;
        }

        private async Task<List<MOBLegalDocument>> GetIBEFullConfirmPageHeaderFooterDocs()
        {
            return await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles("IBEFULL_CONFIRMATION_PAGE_HEADER_FOOTER", _headers.ContextValues.SessionId, true);
        }

        private async Task<List<Option>> GetIBEFullShoppingOptions()
        {
            return (await GetIBEFullConfirmFarePageDocs())
                            .Where(o => o != null)
                            .OrderBy(o => Convert.ToInt32(o.Title))
                            .Select(o => o.LegalDocument.Split('|'))
                            .Select(doc => new Option
                            {
                                OptionDescription = doc[0],
                                AvailableInElf = Convert.ToBoolean(doc[1]),
                                AvailableInEconomy = Convert.ToBoolean(doc[2]),
                                fareSubDescriptionELF = null,
                                fareSubDescriptionEconomy = null,
                                OptionIcon = doc[5]
                            })
                            .ToList();
        }

        private async Task<List<MOBLegalDocument>> GetIBEFullConfirmFarePageDocs()
        {
            return await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles("IBEFULL_CONFIRMATION_PAGE_OPTIONS", _headers.ContextValues.SessionId, true);
        }

        private string GetIBELiteEconomyBagFeeAmount(BasicEconomyEntitlementResponse cslRepsonse)
        {
            if (cslRepsonse.Status != StatusType.Success)
                throw new MOBUnitedException("Bad Response");

            return cslRepsonse.ProductOffer.Offers.First().ProductInformation.ProductDetails.First().Product.SubProducts.First().Extension.Bag.Bags.First().RegularPrice.First().Price.Totals.First().Amount.ToString();
        }

        private string GetIBELitePBagFeeCurrecy(BasicEconomyEntitlementResponse cslRepsonse)
        {
            if (cslRepsonse.Status != StatusType.Success)
                throw new MOBUnitedException("Bad Response");

            return cslRepsonse.ProductOffer.Offers.First().ProductInformation.ProductDetails.First().Product.SubProducts.First().Prices.First().PaymentOptions.First().PriceComponents.First().Price.Totals.First().Currency.Code;
        }

        private string GetIBELitePBagFeeAmount(BasicEconomyEntitlementResponse cslRepsonse)
        {
            if (cslRepsonse.Status != StatusType.Success)
                throw new MOBUnitedException("Bad Response");

            return cslRepsonse.ProductOffer.Offers.First().ProductInformation.ProductDetails.First().Product.SubProducts.First().Prices.First().PaymentOptions.First().PriceComponents.First().Price.Totals.First().Amount.ToString();
        }

        private bool ValidateBasicEconomyEntitlementResponse(BasicEconomyEntitlementResponse ibeEntitlementsResponse)
        {
            if (ibeEntitlementsResponse.Status == StatusType.Success)
                return true;

            if (ibeEntitlementsResponse.Errors == null || !ibeEntitlementsResponse.Errors.Any())
                throw new MOBUnitedException("Unable to get bag fee.");

            throw new MOBUnitedException(string.Join(", ", ibeEntitlementsResponse.Errors.Select(x => x.Message)));
        }

        private string GetProductCode(MOBSHOPAvailability availability)
        {
            if (_configuration.GetValue<bool>("EnablePBE")
                && availability != null
                && availability.Reservation != null)
            {
                return availability
                       .Reservation
                        .Trips
                         .First()
                          .FlattenedFlights
                           .First()
                            .Flights
                             .First()
                              .ShoppingProducts
                               .First(p => p != null && p.IsIBE)
                                .ProductCode;
            }
            return string.Empty;
        }

        internal virtual async Task<BasicEconomyEntitlementResponse> GetBasicEconomyEntitlements(Session session, MOBSHOPGetProductInfoForFSRDRequest request)
        {
            string cslActionName = "GetBasicEconomyEntitlements";

            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetProductInfoForFSRD - Request url for " + cslActionName, "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, url));

            var cslRequest = GetBasicEconomyEntitlementRequest(session, request);
            string cslRequestJson = JsonConvert.SerializeObject(cslRequest);

            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetProductInfoForFSRD - Request for " + cslActionName, "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, cslRequestJson));
            _logger.LogInformation("GetProductInfoForFSRD - Request for {cslActionName}  SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Request {request}", cslActionName, session.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, cslRequestJson);
            //#TakeTokenFromSession
            //  string token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration);

            var cslCallDurationstopwatch = new Stopwatch();
            cslCallDurationstopwatch.Start();

            var response = await _flightShoppingService.GetBasicEconomyEntitlement<BasicEconomyEntitlementResponse>(session.Token, cslActionName, cslRequestJson, session.SessionId);

            if (cslCallDurationstopwatch.IsRunning)
            {
                cslCallDurationstopwatch.Stop();
            }

            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, string.Format("{0} - CSL Call Duration", cslActionName), "CSS/CSL-CallDuration", request.Application.Id, request.Application.Version.Major, request.DeviceId, cslActionName + (cslCallDurationstopwatch.ElapsedMilliseconds / (double)1000).ToString()));

            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetProductInfoForFSRD - Response for " + cslActionName, "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonResponse));

            _logger.LogInformation("GetProductInfoForFSRD - Response for {cslactionname} SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Response {response}", cslActionName, session.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, response);

            if (response != null)
            {
                if (response != null && !response.Status.Equals(StatusType.Success))
                {
                    if (response != null && response.Errors != null && response.Errors.Count > 0)
                    {
                        var builtErrMsg = string.Join(", ", response.Errors.Select(x => x.Message));

                        throw new Exception(builtErrMsg);
                    }

                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                return response;
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
        }

        private BasicEconomyEntitlementRequest GetBasicEconomyEntitlementRequest(Session session, MOBSHOPGetProductInfoForFSRDRequest request)
        {
            return new BasicEconomyEntitlementRequest
            {
                CartId = session.CartId,
                CountryCode = request.CountryCode,
                FlightHash = request.FlightHash,
                LangCode = request.LanguageCode,
                ChannelType = _configuration.GetValue<string>("Shopping - ChannelType")
            };
        }

        private async Task<List<MOBItem>> SetElfShopMessage(bool isMultiTravelers, bool isSSA)
        {
            var list = isSSA ? await GetELFShopMessages("SSA_ELF_CONFIRMATION_PAGE_HEADER_FOOTER") :
                               await GetELFShopMessages("ELF_CONFIRMATION_PAGE_HEADER_FOOTER");
            if (list != null && list.Count > 0)
            {
                var multiTravelerTitle = list.Find(p => p != null && p.Id == "ELFConfirmFareTypeTitle");
                if (!isMultiTravelers && multiTravelerTitle != null)
                {
                    multiTravelerTitle.CurrentValue = "";
                }
            }

            return list;
        }

        private async Task<List<MOBItem>> GetELFShopMessages(string elfDocumentLibraryTableKey)
        {
            List<MOBItem> elfShopMessageList = await GetMPPINPWDTitleMessages(elfDocumentLibraryTableKey);
            return elfShopMessageList;
        }

        private async Task<List<MOBItem>> GetMPPINPWDTitleMessages(string titleList)
        {

            List<MOBItem> messages = new List<MOBItem>();
            var docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(titleList, _headers.ContextValues.TransactionId, true);
            if (!_configuration.GetValue<bool>("DisableCubaTravelContentOrderMismatchFix") && !string.IsNullOrEmpty(titleList) && titleList == "CUBA_TRAVEL_CONTENT" && docs != null && docs.Count > 0)
            {
                _shoppingUtility.AddMobLegalDocumentItem(docs, messages, "CubaPage1Title");

                _shoppingUtility.AddMobLegalDocumentItem(docs, messages, "CubaPage1Description");

                _shoppingUtility.AddMobLegalDocumentItem(docs, messages, "CubaPage1ReasonListButton");
            }
            else if (docs != null && docs.Count > 0)
            {
                foreach (var doc in docs)
                {
                    MOBItem item = new MOBItem();
                    item.Id = doc.Title;
                    item.CurrentValue = doc.LegalDocument;
                    messages.Add(item);
                }
            }
            return messages;
        }

        private async Task<List<Option>> ParseELFShopOptions(bool isSSA)
        {
            List<MOBItem> list = isSSA ? await GetELFShopMessages("SSA_ELF_CONFIRMATION_PAGE_OPTIONS") :
                                         await GetELFShopMessages("ELF_CONFIRMATION_PAGE_OPTIONS");
            List<Option> elfOptions = new List<Option>();
            if (list != null && list.Count > 0)
            {
                var orderedList = list.Where(o => o != null).OrderBy(o => Convert.ToInt32(o.Id)).ToList();
                foreach (var mobItem in orderedList)
                {
                    if (mobItem.CurrentValue != string.Empty)
                    {
                        string[] mobShopOptionValueCollection = mobItem.CurrentValue.Split('|');
                        if (mobShopOptionValueCollection.Length == 4)
                        {
                            elfOptions.Add(new Option()
                            {
                                OptionDescription = mobShopOptionValueCollection[0],
                                AvailableInElf = Convert.ToBoolean(mobShopOptionValueCollection[1]),
                                AvailableInEconomy = Convert.ToBoolean(mobShopOptionValueCollection[2]),
                                OptionIcon = mobShopOptionValueCollection[3]
                            });
                        }
                    }
                }
            }
            return elfOptions;
        }

        private async Task<List<MOBSHOPTrip>> PopulateMetaTrips(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string cartId, ShoppingResponse persistShop, string sessionId, int appId, string deviceId, string appVersion, bool showMileageDetails, List<United.Services.FlightShopping.Common.DisplayCart.DisplayTrip> flightSegmentCollection, string fareClass, List<string> flightDepartDatesForSelectedTrip)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, new Session().ObjectName, new List<string> { sessionId, new Session().ObjectName }).ConfigureAwait(false);
            supressLMX = session.SupressLMXForAppID;
            #endregion
            List<MOBSHOPTrip> trips = new List<MOBSHOPTrip>();
            try
            {
                if (_configuration.GetValue<bool>("EnableFecthAirportNameFromCSL"))
                {
                    airportsList = null;
                }
                else
                {
                    airportsList = await _sessionHelperService.GetSession<AirportDetailsList>(sessionId, new AirportDetailsList().ObjectName, new List<string> { sessionId, new AirportDetailsList().ObjectName }).ConfigureAwait(false);
                }

                if (isGetAirportListInOneCallToggleOn() && (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0))
                {
                    airportsList = await GetAllAiportsList(flightSegmentCollection);
                }
            }
            catch (Exception ex)
            {
                //logEntries.Add(LogEntry.GetLogEntry<Exception>(sessionId, "GetAllAiportsList", "PopulateMetaTrips-GetAllAiportsList", appId, appVersion, deviceId, ex, true, true));
                _logger.LogError("PopulateMetaTrips-GetAllAiportsList Error {@Exception}", JsonConvert.SerializeObject(ex));
                //logEntries.Add(LogEntry.GetLogEntry<List<United.Services.FlightShopping.Common.DisplayCart.DisplayTrip>>(sessionId, "GetAllAiportsList", "PopulateMetaTrips-DisplayTrip", appId, appVersion, deviceId, flightSegmentCollection, true, true));
            }

            for (int i = 0; i < flightSegmentCollection.Count; i++)
            {
                MOBSHOPTrip trip = null;

                if (flightSegmentCollection != null && flightSegmentCollection.Count > 0)
                {
                    //i = tripIndex;

                    trip = new MOBSHOPTrip();
                    trip.TripId = flightSegmentCollection[i].BBXSolutionSetId;
                    trip.FlightCount = flightSegmentCollection[i].Flights.Count;
                    //trip.Columns = PopulateColumns(flightSegmentCollection[i].ColumnInformation);

                    trip.DepartDate = GeneralHelper.FormatDateFromDetails(flightSegmentCollection[i].DepartDate);
                    trip.ArrivalDate = GeneralHelper.FormatDateFromDetails(flightSegmentCollection[i].Flights[flightSegmentCollection[i].Flights.Count - 1].DestinationDateTime);
                    trip.Destination = GetAirportCode(flightSegmentCollection[i].Destination);

                    CultureInfo ci = null;

                    List<Model.Shopping.MOBSHOPFlight> flights = null;
                    if (flightSegmentCollection[i].Flights != null && flightSegmentCollection[i].Flights.Count > 0)
                    {
                        //update amenities for all flights
                        UpdateAmenitiesIndicatorsResponse amenitiesResponse = new UpdateAmenitiesIndicatorsResponse();
                        List<Flight> tempFlights = new List<Flight>(flightSegmentCollection[i].Flights);

                        //we do not want the search to fail if one of these fail...
                        try
                        {

                            Parallel.Invoke(async () =>
                            {
                                bool includeAmenities = false;
                                bool.TryParse(_configuration.GetValue<string>("IncludeAmenities"), out includeAmenities);

                                //if we are asking for amenities in the CSL call, do not make this seperate call
                                if (!includeAmenities)
                                {
                                    amenitiesResponse = await GetAmenitiesForFlights(sessionId, cartId, tempFlights, appId, deviceId, appVersion);
                                    PopulateFlightAmenities(amenitiesResponse.Profiles, ref tempFlights);
                                }
                            },
                                    async () =>
                                    {
                                        if (showMileageDetails && !supressLMX)
                                        {
                                            //get all flight numbers
                                            List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights = null;
                                            lmxFlights = await GetLmxFlights(session.Token, cartId, GetFlightHasListForLMX(tempFlights), sessionId, appId, appVersion, deviceId);

                                            if (lmxFlights != null && lmxFlights.Count > 0)
                                                PopulateLMX(lmxFlights, ref tempFlights);//tempFlights = lmxFlights;
                                        }

                                    }
                                );
                        }
                        catch { };

                        flightSegmentCollection[i].Flights = new List<Flight>(tempFlights);
                        if (_mOBSHOPDataCarrier == null)
                            _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
                        var tupleRes = await GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, flightSegmentCollection[i].Flights, string.Empty, ci, 0.0M, trip.Columns, 0, fareClass, false, false, true, null, appVersion, appId);
                        flights = tupleRes.shopflight;
                        ci = tupleRes.ci;
                    }

                    trip.Origin = GetAirportCode(flightSegmentCollection[i].Origin);

                    if (showMileageDetails && !supressLMX)
                        trip.YqyrMessage = _configuration.GetValue<string>("MP2015YQYRMessage");

                    string originName = string.Empty;
                    if (!string.IsNullOrEmpty(trip.Origin))
                    {
                        originName = await GetAirportNameFromSavedList(trip.Origin);
                    }
                    if (string.IsNullOrEmpty(originName))
                    {
                        trip.OriginDecoded = flightSegmentCollection[i].Flights[0].OriginDescription;
                    }
                    else
                    {
                        trip.OriginDecoded = originName;
                    }


                    string destinationDecodedWithCountry = string.Empty;

                    if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI"))
                    {
                        //with share trip flag OriginDescription=Chicago, IL, US (ORD)
                        trip.OriginDecodedWithCountry = flightSegmentCollection[i].Flights[0].OriginDescription;
                        foreach (var flight in flightSegmentCollection[i].Flights)
                        {
                            if (flight.Connections != null && flight.Connections.Count > 0)
                            {
                                foreach (var conn in flight.Connections)
                                {
                                    if (conn.Destination.Equals(flightSegmentCollection[i].Destination))
                                    {
                                        destinationDecodedWithCountry = conn.DestinationDescription;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (flight.Destination.Equals(flightSegmentCollection[i].Destination))
                                {
                                    destinationDecodedWithCountry = flight.DestinationDescription;
                                    break;
                                }
                            }
                        }
                        trip.DestinationDecodedWithCountry = destinationDecodedWithCountry;

                    }

                    string destinationName = string.Empty;
                    if (!string.IsNullOrEmpty(trip.Destination))
                    {
                        destinationName = await GetAirportNameFromSavedList(trip.Destination);
                    }
                    if (string.IsNullOrEmpty(destinationName))
                    {
                        if (_configuration.GetValue<bool>("EnableFecthAirportNameFromCSL"))
                        {
                            trip.DestinationDecoded = destinationDecodedWithCountry;
                        }
                        else
                        {
                            trip.DestinationDecoded = flightSegmentCollection[i].Flights[flightSegmentCollection[i].Flights.Count - 1].DestinationDescription;
                        }
                    }
                    else
                    {
                        trip.DestinationDecoded = destinationName;
                    }

                    if (flights != null)
                    {
                        var isEnabledGMTConversionUsingCslData = _configuration.GetValue<bool>("EnableGMTConversionUsingCslData");
                        string tripDepartDate = string.Empty;
                        foreach (string tripIDDepDate in flightDepartDatesForSelectedTrip)
                        {
                            if (tripIDDepDate.Split('|')[0].ToString().Trim() == trip.TripId)
                            {
                                tripDepartDate = tripIDDepDate.Split('|')[1].ToString().Trim();
                                break;
                            }
                        }
                        if (string.IsNullOrEmpty(tripDepartDate))
                        {
                            tripDepartDate = trip.DepartDate;
                        }

                        trip.FlattenedFlights = new List<Model.Shopping.MOBSHOPFlattenedFlight>();
                        foreach (var flight in flights)
                        {
                            Model.Shopping.MOBSHOPFlattenedFlight flattenedFlight = new Model.Shopping.MOBSHOPFlattenedFlight();
                            //need to overwrite trip id otherwise it's the previous trip's id
                            trip.TripId = flight.TripId;
                            flattenedFlight.TripId = flight.TripId;
                            flattenedFlight.FlightId = flight.FlightId;
                            flattenedFlight.ProductId = flight.ProductId;
                            flattenedFlight.Flights = new List<Model.Shopping.MOBSHOPFlight>();
                            flight.TripId = trip.TripId;

                            if (isEnabledGMTConversionUsingCslData)
                            {
                                flight.DepartureDateTimeGMT = await _shoppingUtility.GetGMTTimeFromOffset(flight.DepartureDateTime, flight.OriginTimezoneOffset);
                                flight.ArrivalDateTimeGMT = await _shoppingUtility.GetGMTTimeFromOffset(flight.ArrivalDateTime, flight.DestinationTimezoneOffset);
                            }
                            else
                            {
                                flight.DepartureDateTimeGMT = await GetGMTTime(flight.DepartureDateTime, flight.Origin, sessionId);
                                flight.ArrivalDateTimeGMT = await GetGMTTime(flight.ArrivalDateTime, flight.Destination, sessionId);
                            }

                            #region Red Eye Flight Changes

                            flight.FlightArrivalDays = GetDayDifference(tripDepartDate, flight.ArrivalDateTime);
                            bool flightDateChanged = false;
                            flight.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDepartDate, flight.ArrivalDateTime, ref flightDateChanged);
                            flight.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDepartDate, flight.DepartureDateTime, ref flightDateChanged);
                            flight.FlightDateChanged = flightDateChanged;

                            #endregion


                            flattenedFlight.Flights.Add(flight);

                            if (flight.Connections != null && flight.Connections.Count > 0)
                            {
                                // Make a copy of flight.Connections and release flight.Connections
                                List<Model.Shopping.MOBSHOPFlight> connections = flight.Connections.Clone();
                                flight.Connections = null;

                                foreach (var connection in connections)
                                {
                                    connection.TripId = trip.TripId;

                                    connection.DepartureDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(connection.DepartureDateTime, connection.OriginTimezoneOffset) : await GetGMTTime(connection.DepartureDateTime, connection.Origin, sessionId);
                                    connection.ArrivalDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(connection.ArrivalDateTime, connection.DestinationTimezoneOffset) : await GetGMTTime(connection.ArrivalDateTime, connection.Destination, sessionId);

                                    #region Red Eye Flight Changes

                                    connection.FlightArrivalDays = GetDayDifference(tripDepartDate, connection.ArrivalDateTime);
                                    connection.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDepartDate, connection.ArrivalDateTime, ref flightDateChanged);
                                    connection.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDepartDate, connection.DepartureDateTime, ref flightDateChanged);
                                    connection.FlightDateChanged = flightDateChanged;

                                    #endregion

                                    flattenedFlight.Flights.Add(connection);
                                    if (connection.StopInfos != null && connection.StopInfos.Count > 0)
                                    {
                                        // Make a copy of flight.Connections and release flight.Connections
                                        List<Model.Shopping.MOBSHOPFlight> connStops = connection.StopInfos.Clone();
                                        connection.StopInfos = null;

                                        foreach (var conn in connStops)
                                        {
                                            conn.TripId = trip.TripId;
                                            conn.IsStopOver = true;
                                            conn.DepartureDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(conn.DepartureDateTime, conn.OriginTimezoneOffset) : await GetGMTTime(conn.DepartureDateTime, conn.Origin, sessionId);
                                            conn.ArrivalDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(conn.ArrivalDateTime, conn.DestinationTimezoneOffset) : await GetGMTTime(conn.ArrivalDateTime, conn.Destination, sessionId);
                                            #region Red Eye Flight Changes

                                            conn.FlightArrivalDays = GetDayDifference(tripDepartDate, conn.ArrivalDateTime);
                                            conn.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDepartDate, conn.ArrivalDateTime, ref flightDateChanged);
                                            conn.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDepartDate, conn.DepartureDateTime, ref flightDateChanged);
                                            conn.FlightDateChanged = flightDateChanged;

                                            #endregion

                                            flattenedFlight.Flights.Add(conn);
                                        }
                                    }
                                }
                            }

                            if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                            {
                                // Make a copy of flight.Connections and release flight.Connections
                                List<Model.Shopping.MOBSHOPFlight> connections = flight.StopInfos.Clone();
                                flight.StopInfos = null;

                                foreach (var connection in connections)
                                {
                                    connection.TripId = trip.TripId;
                                    connection.IsStopOver = true;
                                    connection.DepartureDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(connection.DepartureDateTime, connection.OriginTimezoneOffset) : await GetGMTTime(connection.DepartureDateTime, connection.Origin, sessionId);
                                    connection.ArrivalDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(connection.ArrivalDateTime, connection.DestinationTimezoneOffset) : await GetGMTTime(connection.ArrivalDateTime, connection.Destination, sessionId);
                                    #region Red Eye Flight Changes

                                    connection.FlightArrivalDays = GetDayDifference(tripDepartDate, connection.ArrivalDateTime);
                                    connection.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDepartDate, connection.ArrivalDateTime, ref flightDateChanged);
                                    connection.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDepartDate, connection.DepartureDateTime, ref flightDateChanged);
                                    connection.FlightDateChanged = flightDateChanged;

                                    #endregion
                                    flattenedFlight.Flights.Add(connection);
                                }
                            }

                            if (flight.ShoppingProducts != null && flight.ShoppingProducts.Count > 0)
                            {
                                int idx = 0;
                                foreach (var prod in flight.ShoppingProducts)
                                {
                                    if (prod.IsMixedCabin)
                                    {
                                        prod.MixedCabinSegmentMessages = GetFlightMixedCabinSegments(flattenedFlight.Flights, idx);
                                        prod.IsSelectedCabin = GetSelectedCabinInMixedScenario(flattenedFlight.Flights, idx);

                                        prod.ProductDetail.ProductCabinMessages = GetProductDetailMixedCabinSegments(flattenedFlight.Flights, idx);
                                        //break;
                                    }
                                    idx++;
                                }
                            }

                            trip.FlattenedFlights.Add(flattenedFlight);
                        }
                    }
                    trip.UseFilters = false;
                    trip.SearchFiltersIn = null;
                    trip.SearchFiltersOut = null;
                }
                trips.Add(trip);
            }

            return trips;
        }
        //added-Kriti

        private async Task<AirportDetailsList> GetAllAiportsList(List<DisplayTrip> displayTrip)
        {
            string airPortCodes = GetAllAirportCodesWithCommaDelimatedFromCSLDisplayTrips(displayTrip);
            return await GetAirportNamesListCollection(airPortCodes);
        }

        private string GetDayDifference(String flightDepart, String flightArrive)
        {
            try
            {

                DateTime depart = DateTime.MinValue;
                DateTime arrive = DateTime.MinValue;

                DateTime.TryParse(flightDepart, out depart);
                DateTime.TryParse(flightArrive, out arrive);

                int days = (arrive.Date - depart.Date).Days;

                if (days == 0)
                {
                    return string.Empty;
                }
                else if (days > 0 && days < 2)
                {
                    return "+" + days.ToString() + " day";
                }
                else if (days > 0 && days > 1)
                {
                    return "+" + days.ToString() + " days";
                }
                else if (days < 0 && days > -2)
                {
                    return days.ToString() + " day";
                }
                else if (days < 0 && days < -1)
                {
                    return days.ToString() + " days";
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }

        private bool isGetAirportListInOneCallToggleOn()
        {
            return _configuration.GetValue<bool>("GetAirportNameInOneCallToggle");
        }

        private async Task<AirportDetailsList> GetAllAiportsList(List<Flight> flights)
        {
            string airPortCodes = GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(flights);
            return await GetAirportNamesListCollection(airPortCodes);
        }

        private async Task<AirportDetailsList> GetAirportNamesListCollection(string airPortCodes)
        {
            AirportDetailsList retVal = null;
            if (airPortCodes != string.Empty)
            {
                airPortCodes = "'" + airPortCodes + "'";
                airPortCodes = String.Join(",", airPortCodes.Split(',').Distinct());
                airPortCodes = System.Text.RegularExpressions.Regex.Replace(airPortCodes, ",", "','");
                retVal = new AirportDetailsList();
                retVal.AirportsList = await _shoppingUtility.GetAirportNamesList(airPortCodes);
            }
            return retVal;
        }

        private string GetAllAirportCodesWithCommaDelimatedFromCSLDisplayTrips(List<DisplayTrip> displayTrip)
        {
            string airPortCodes = string.Empty;
            if (displayTrip != null && displayTrip.Count > 0)
            {
                airPortCodes = string.Join(",", displayTrip.Where(t => t != null).Select(t => t.Origin + "," +
                                                                                              t.Destination + "," +
                                                                                              GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(t.Flights))
                                          );
            }
            airPortCodes = System.Text.RegularExpressions.Regex.Replace(airPortCodes, ",{2,}", ",").Trim(',');
            airPortCodes = String.Join(",", airPortCodes.Split(',').Distinct());
            return airPortCodes;
        }

        private List<MOBSHOPShoppingProductDetailCabinMessage> GetProductDetailMixedCabinSegments(List<Model.Shopping.MOBSHOPFlight> flights, int index)
        {
            //group the mixed cabin messages                
            List<Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage> tempMsgs = new List<Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage>();
            foreach (var flt in flights)
            {
                if (flt.ShoppingProducts != null && flt.ShoppingProducts.Count > index)
                {
                    if (flt.ShoppingProducts[index].ProductDetail.ProductCabinMessages != null && flt.ShoppingProducts[index].ProductDetail.ProductCabinMessages.Count > 0)
                        tempMsgs.AddRange(flt.ShoppingProducts[index].ProductDetail.ProductCabinMessages);
                }
            }

            return tempMsgs;
        }

        private List<string> GetFlightMixedCabinSegments(List<Model.Shopping.MOBSHOPFlight> flights, int index)
        {
            //group the mixed cabin messages                
            List<string> tempMsgs = new List<string>();
            foreach (var flt in flights)
            {
                if (flt.ShoppingProducts != null && flt.ShoppingProducts.Count > index)
                {
                    if (flt.ShoppingProducts[index].MixedCabinSegmentMessages != null && flt.ShoppingProducts[index].MixedCabinSegmentMessages.Count > 0)
                        tempMsgs.AddRange(flt.ShoppingProducts[index].MixedCabinSegmentMessages);
                }
            }

            return tempMsgs;
        }

        private static string GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(List<Flight> flights)
        {
            string airPortCodes = string.Empty;
            if (flights != null && flights.Count > 0)
            {
                airPortCodes = string.Join(",", flights.Where(f => f != null).Select(flight => flight.Origin + "," +
                                                                                               flight.Destination + "," +
                                                                                               string.Join(",", flight.Connections.Where(c => c != null).Select(connection => connection.Origin + "," + connection.Destination)) + "," +
                                                                                               string.Join(",", flight.StopInfos.Where(s => s != null).Select(stop => stop.Origin + "," + stop.Destination))
                                                                                          )
                                                                    );
            }
            airPortCodes = System.Text.RegularExpressions.Regex.Replace(airPortCodes, ",{2,}", ",").Trim(',');
            airPortCodes = String.Join(",", airPortCodes.Split(',').Distinct());
            return airPortCodes;
        }

        private bool GetSelectedCabinInMixedScenario(List<Model.Shopping.MOBSHOPFlight> flights, int index)
        {
            //group the mixed cabin messages                
            bool selected = false;
            foreach (var flt in flights)
            {
                if (flt.ShoppingProducts != null && flt.ShoppingProducts.Count > index)
                {
                    if (flt.ShoppingProducts[index].IsSelectedCabin)
                        selected = flt.ShoppingProducts[index].IsSelectedCabin;
                }
            }

            return selected;
        }

        private string GetRedEyeDepartureDate(String tripDate, String flightDepartureDate, ref bool flightDateChanged)
        {
            try
            {
                DateTime trip = DateTime.MinValue;
                DateTime departure = DateTime.MinValue;

                DateTime.TryParse(tripDate, out trip);
                DateTime.TryParse(flightDepartureDate, out departure);

                int days = (departure.Date - trip.Date).Days;

                if (days > 0)
                {
                    flightDateChanged = true; // Venkat - Showing Flight Date Change message is only for Departure date is different than Flight Search Date.
                    return departure.ToString("ddd. MMM dd"); // Wed. May 20                    
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }

        private string GetRedEyeFlightArrDate(String flightDepart, String flightArrive, ref bool flightDateChanged)
        {
            try
            {

                DateTime depart = DateTime.MinValue;
                DateTime arrive = DateTime.MinValue;

                DateTime.TryParse(flightDepart, out depart);
                DateTime.TryParse(flightArrive, out arrive);

                int days = (arrive.Date - depart.Date).Days;

                if (days == 0)
                {
                    return string.Empty;
                }
                else if (days > 0)
                {
                    return arrive.ToString("ddd. MMM dd"); // Wed. May 20
                }
                else
                {
                    if (_configuration.GetValue<bool>("EnableFlightDateChangeAlertFix"))
                    {
                        var daysText = "day";
                        if (days < -1)
                        {
                            daysText = $"{daysText}s";
                        }
                        return $"{days} {daysText} arrival";
                    }
                    return string.Empty;
                }
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }

        private async Task<(List<Model.Shopping.MOBSHOPFlight> shopflight, CultureInfo ci)> GetFlights(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string sessionId, string cartId, List<Flight> segments, string requestedCabin, CultureInfo ci, decimal lowestFare, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClass, bool updateAmenities = false, bool isConnection = false, bool isELFFareDisplayAtFSR = true, MOBSHOPTrip trip = null, string appVersion = "", int appID = 0)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);
            supressLMX = session.SupressLMXForAppID;
            #endregion

            try
            {
                if (isGetAirportListInOneCallToggleOn() && (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0))
                {
                    airportsList = await _sessionHelperService.GetSession<AirportDetailsList>(sessionId, new AirportDetailsList().ObjectName, new List<string> { sessionId, new AirportDetailsList().ObjectName }).ConfigureAwait(false);//, (new AirportDetailsList()).GetType().FullName);
                    if (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0)
                    {
                        airportsList = await GetAllAiportsList(segments);
                    }
                }
            }
            catch (Exception ex)
            {
                //logEntries.Add(LogEntry.GetLogEntry<Exception>(sessionId, "GetAllAiportsList", "GetFlights-GetAllAiportsList", 1, string.Empty, string.Empty, ex, true, true));
                //logEntries.Add(LogEntry.GetLogEntry<List<Flight>>(sessionId, "GetAllAiportsList", "PopulateMetaTrips-Trip", 1, string.Empty, string.Empty, segments, true, true));
                _logger.LogError("GetFlights-GetAllAiportsList Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), sessionId);
                _logger.LogError("GetFlights-GetAllAiportsList Error {exception} and {sessionId}", ex.Message, sessionId);

            }

            List<Model.Shopping.MOBSHOPFlight> flights = null;
            ci = null;
            if (segments != null && segments.Count > 0)
            {
                flights = new List<Model.Shopping.MOBSHOPFlight>();

                foreach (Flight segment in segments)
                {
                    #region

                    Model.Shopping.MOBSHOPFlight flight = new Model.Shopping.MOBSHOPFlight();
                    flight.Messages = new List<United.Mobile.Model.Shopping.MOBSHOPMessage>();
                    string AddCollectProductID = string.Empty;
                    Product displayProductForStopInfo = null;
                    bool selectedForStopInfo = false;
                    string bestProductType = null;

                    // #633226 Reshop SDC Add coller waiver status
                    if (session.IsReshopChange)
                    {
                        flight.isAddCollectWaived = GetAddCollectWaiverStatus(segment, out AddCollectProductID);
                        flight.AddCollectProductId = AddCollectProductID;
                    }

                    if (!string.IsNullOrEmpty(segment.BBXHash))
                    {
                        flight.FlightId = segment.BBXHash;
                        flight.ProductId = segment.BBXCellId;
                        flight.GovtMessage = segment.SubjectToGovernmentApproval ? _configuration.GetValue<string>("SubjectToGovernmentApprovalMessage") : string.Empty;
                    }

                    flight.TripIndex = segment.TripIndex;
                    flight.FlightHash = segment.Hash;
                    flight.IsCovidTestFlight = segment.IsCovidTestingRequired;
                    if (!_configuration.GetValue<bool>("DisableOperatingCarrierFlightNumberChanges"))
                    {
                        flight.OperatingCarrierFlightNumber = segment.OriginalFlightNumber;
                    }

                    if (session.IsExpertModeEnabled && !session.IsAward)
                    {
                        if (!string.IsNullOrEmpty(segment.BookingClassAvailability))
                        {
                            flight.BookingClassAvailability = segment.BookingClassAvailability.Replace('|', ' ');
                        }
                    }

                    #region //NEED LOGIC TO DETERMINE SELECTED PRODUCT HERE TO GET PRICING
                    if (segment.Products != null && segment.Products.Count > 0/* && segment.Products[0] != null && segment.Products[0].Prices != null && segment.Products[0].Prices.Count > 0*/)
                    {
                        bool selected;
                        int seatsRemaining = 0;
                        bool mixedCabin;
                        string description = string.Empty;

                        AssignCorporateFareIndicator(segment, flight, session.TravelType);

                        Product displayProduct = GetMatrixDisplayProduct(segment.Products, requestedCabin, columnInfo, out ci, out selected, out description, out mixedCabin, out seatsRemaining, fareClass, isConnection, isELFFareDisplayAtFSR);
                        displayProductForStopInfo = displayProduct;
                        selectedForStopInfo = selected;
                        if (_shoppingUtility.EnableYoungAdult(session.IsReshopChange))
                        {
                            if (IsYoungAdultProduct(segment.Products))
                            {
                                flight.YaDiscount = "Discounted";
                            }
                        }
                        GetBestProductTypeTripPlanner(session, displayProduct, selected, ref bestProductType);
                        if (displayProduct != null && isConnection || (displayProduct.Prices != null && displayProduct.Prices.Count > 0))
                        {
                            if (displayProduct != null && !isConnection && displayProduct.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES)
                            {
                                //WADE-adding logic to add in close in award fee if present
                                decimal closeInFee = 0;
                                if (displayProduct.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES)
                                {
                                    foreach (PricingItem p in displayProduct.Prices)
                                    {
                                        if (p.PricingType.Trim().ToUpper() == PRICING_TYPE_CLOSE_IN_FEE)
                                        {
                                            closeInFee = p.Amount;
                                            break;
                                        }
                                    }

                                }
                                flight.Airfare = displayProduct.Prices[0].Amount;
                                ///208848 : PROD Basic Economy mApps Price in footer of FSR is showing lowest BE fare when BE switch is off 
                                ///Srini - 12/29/2017
                                if (_configuration.GetValue<bool>("BugFixToggleFor18B") &&
                                      (_mOBSHOPDataCarrier.FsrMinPrice > displayProduct.Prices[0].Amount ||
                                      (_mOBSHOPDataCarrier.FsrMinPrice == 0 && displayProduct.Prices[0].Amount > 0)) &&
                                      !mixedCabin &&
                                      (
                                        requestedCabin.Contains("ECONOMY") ||
                                        (requestedCabin.Contains("BUS") && (displayProduct.ProductType.Contains("BUS") || displayProduct.ProductType.Contains("FIRST"))) ||
                                        (requestedCabin.Contains("FIRST") && displayProduct.ProductType.Contains("FIRST"))
                                      )
                                   )
                                {
                                    _mOBSHOPDataCarrier.FsrMinPrice = displayProduct.Prices[0].Amount;
                                }
                                //TFS WI 68391
                                //Do_Not_Allow_Miles_Zero_AwardSearch we using this as a Flag if its Null means allow if its not Null even the value is true or false do not allow Zero Miles Award Booking.
                                if (displayProduct.Prices[0].Amount > 0 || _configuration.GetValue<string>("Do_Not_Allow_Miles_Zero_AwardSearch") == null)
                                {
                                    if (!_configuration.GetValue<bool>("NGRPAwardCalendarMP2017NewUISwitch"))
                                    {
                                        flight.AirfareDisplayValue = TopHelper.FormatAmountForDisplay(displayProduct.Prices[1].Amount + closeInFee, ci, true, true);
                                    }
                                    else
                                    {
                                        flight.AirfareDisplayValue = TopHelper.FormatAmountForDisplay(displayProduct.Prices[1].Amount + closeInFee, ci, false, true);
                                    }
                                    flight.MilesDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(flight.Airfare.ToString(), true);
                                }
                                else
                                {
                                    flight.AirfareDisplayValue = "N/A";
                                    flight.MilesDisplayValue = "N/A";
                                    if (_configuration.GetValue<bool>("ReturnExceptionForAnyZeroDollarAirFareValue"))
                                    {
                                        throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                                    }
                                }
                                flight.MilesDisplayValue += !selected || mixedCabin ? "*" : "";
                                flight.IsAwardSaver = displayProduct.AwardType.Trim().ToLower().Contains("saver") && !displayProduct.ProductType.ToUpper().Contains("ECONOMY") ? true : false;
                            }
                            else if (!isConnection)
                            {
                                ///208848 : PROD Basic Economy mApps Price in footer of FSR is showing lowest BE fare when BE switch is off 
                                ///Srini - 12/29/2017
                                if (_configuration.GetValue<bool>("BugFixToggleFor18B") &&
                                      (_mOBSHOPDataCarrier.FsrMinPrice > displayProduct.Prices[0].Amount ||
                                      (_mOBSHOPDataCarrier.FsrMinPrice == 0 && displayProduct.Prices[0].Amount > 0)) &&
                                      !mixedCabin &&
                                      (
                                        requestedCabin.Contains("ECONOMY") ||
                                        (requestedCabin.Contains("BUS") && (displayProduct.ProductType.Contains("BUS") || displayProduct.ProductType.Contains("FIRST"))) ||
                                        (requestedCabin.Contains("FIRST") && displayProduct.ProductType.Contains("FIRST"))
                                      )
                                   )
                                {
                                    _mOBSHOPDataCarrier.FsrMinPrice = displayProduct.Prices[0].Amount;
                                }
                                flight.Airfare = displayProduct.Prices[0].Amount;
                                if (session.IsReshopChange)
                                {
                                    if (session.IsAward)
                                    {
                                        //if(trip.LastTripIndexRequested == 1)
                                        //    flight.AirfareDisplayValue = ReShopAirfareDisplayValue(ci, displayProduct.Prices, session.IsAward, true);
                                        //else
                                        flight.AirfareDisplayValue = ReShopAirfareDisplayValue(ci, displayProduct.Prices, session.IsAward, true);
                                        var milesAmountToDisplay = ReshopAwardPrice(displayProduct.Prices);

                                        if (milesAmountToDisplay == null)
                                            flight.MilesDisplayValue = "NA";
                                        else
                                        {
                                            flight.MilesDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(Convert.ToString(milesAmountToDisplay.Amount), true);
                                            flight.MilesDisplayValue += !selected || mixedCabin ? "*" : "";
                                        }
                                    }
                                    else
                                    {
                                        flight.AirfareDisplayValue = ReShopAirfareDisplayValue(ci, displayProduct.Prices);

                                        if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                                        {
                                            flight.ReshopFees = ReshopAirfareDisplayText(displayProduct.Prices);
                                        }

                                        if (_shoppingUtility.EnableReShopAirfareCreditDisplay(appID, appVersion))
                                        {
                                            flight = ReShopAirfareCreditDisplayFSR(ci, displayProduct, flight);
                                        }
                                    }
                                }
                                else if (displayProduct.Prices[0].Amount > 0)
                                {
                                    string displayPrice = TopHelper.FormatAmountForDisplay(displayProduct.Prices[0].Amount, ci, true);
                                    flight.AirfareDisplayValue = string.IsNullOrEmpty(displayPrice) ? "Not available" : displayPrice;
                                }
                                else
                                {
                                    flight.AirfareDisplayValue = "N/A";
                                    if (_configuration.GetValue<bool>("ReturnExceptionForAnyZeroDollarAirFareValue"))
                                    {
                                        // Added as part of Bug 180337:mApp: "Sorry something went wrong... " Error message is displayed when selected cabin for second segment in the multi trip
                                        // throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                                        throw new MOBUnitedException(_configuration.GetValue<string>("BookingDetail_ITAError_For_CSL_10047__Error_Code"));
                                    }
                                }

                                if (!session.IsFSRRedesign)
                                {
                                    if (!session.IsAward)
                                        flight.AirfareDisplayValue += !selected || mixedCabin ? "*" : "";
                                }
                            }

                            flight.SeatsRemaining = seatsRemaining;
                            flight.Selected = selected;
                            flight.ServiceClassDescription = description;

                            if (string.IsNullOrEmpty(flight.Meal))
                            {
                                flight.Meal = !string.IsNullOrEmpty(displayProduct.MealDescription) ? displayProduct.MealDescription : "None";
                                flight.ServiceClass = displayProduct.BookingCode;

                                flight.Messages = new List<Model.Shopping.MOBSHOPMessage>();

                                Model.Shopping.MOBSHOPMessage msg = new Model.Shopping.MOBSHOPMessage();
                                msg.FlightNumberField = flight.FlightNumber;
                                msg.TripId = flight.TripId;
                                msg.MessageCode = displayProduct.Description + " (" + displayProduct.BookingCode + ")";
                                if (selected && _shoppingUtility.IsIBeLiteFare(displayProduct)) // bug 277549: update the message for IBE Lite only when customer switch ON the 'Show Basic Economy fares'
                                {
                                    msg.MessageCode = msg.MessageCode + " " + displayProduct.CabinTypeText; // EX: United Economy (K) (first bag charge/no changes allowed)
                                }
                                flight.Messages.Add(msg);

                                msg = new Model.Shopping.MOBSHOPMessage();
                                msg.FlightNumberField = flight.FlightNumber;
                                msg.TripId = flight.TripId;
                                msg.MessageCode = !string.IsNullOrEmpty(displayProduct.MealDescription) ? displayProduct.MealDescription : "None";
                                flight.Messages.Add(msg);

                                flight.MatchServiceClassRequested = selected;
                                if (session.IsFSRRedesign)
                                {
                                    if (flight.Messages != null && flight.Messages.Count > 0)
                                    {
                                        if (mixedCabin)
                                        {
                                            var mixedCabinText = _configuration.GetValue<string>("MixedCabinProductBadgeText");
                                            var firstMessage = flight.Messages.First().MessageCode;
                                            var mixedCabinColorCode = _configuration.GetValue<string>("MixedCabinTextColorCode");
                                            if (await _featureSettings.GetFeatureSettingValue("EnableNewBackGroundColor").ConfigureAwait(false) && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, _configuration.GetValue<string>("Android_Atmos2_New_MixedCabinTextColorCode_AppVersion"), _configuration.GetValue<string>("Iphone_Atmos2_New_MixedCabinTextColorCode_AppVersion")))
                                            {
                                                mixedCabinColorCode = _configuration.GetValue<string>("Atmos2_New_MixedCabinTextColorCode");
                                            }
                                            var message1 = string.Format("<span style='color:{0}'>{1} {2}</span>", mixedCabinColorCode, mixedCabinText, firstMessage);
                                            var message2 = flight.Messages.Last().MessageCode;
                                            flight.LineOfFlightMessage = string.Join(" / ", message1, message2);
                                        }
                                        else
                                        {
                                            flight.LineOfFlightMessage = string.Join(" / ", flight.Messages.Select(x => x.MessageCode));
                                        }
                                    }
                                }
                            }

                            if (!supressLMX && displayProduct.LmxLoyaltyTiers != null && displayProduct.LmxLoyaltyTiers.Count > 0)
                            {
                                //flight.YqyrMessage = _configuration.GetValue<string>("MP2015YQYRMessage");

                                foreach (var tier in displayProduct.LmxLoyaltyTiers)
                                {
                                    if (tier != null && string.IsNullOrEmpty(tier.ErrorCode))
                                    {
                                        int tempStatus = premierStatusLevel;
                                        if (premierStatusLevel > 4)//GS gets same LMX as 1K
                                            tempStatus = 4;

                                        if (tier.Level == tempStatus)
                                        {
                                            if (tier.LmxQuotes != null && tier.LmxQuotes.Count > 0)
                                            {
                                                foreach (var quote in tier.LmxQuotes)
                                                {
                                                    switch (quote.Type.Trim().ToUpper())
                                                    {
                                                        case "RDM":
                                                            flight.RdmText = string.Format("{0:#,##0}", quote.Amount);
                                                            break;
                                                        case "PQM":
                                                            flight.PqmText = string.Format("{0:#,##0}", quote.Amount);
                                                            break;
                                                        case "PQD":
                                                            flight.PqdText = TopHelper.FormatAmountForDisplay(quote.Amount, ci, true, false);
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            //Add label to response for seats remaining if AppId = 16.
                            //Make # seats to show and AppIds configurable.
                            //Reuse existing SeatsRemaingVerbiage and logic.
                            int intSeatsRemainingLimit = 0;
                            string strAppIDs = String.Empty;

                            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("mWebSeatsRemainingLimit")))
                            {
                                intSeatsRemainingLimit = Convert.ToInt32(_configuration.GetValue<string>("mWebSeatsRemainingLimit"));
                            }
                            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("SeatsRemainingAppIDs")))
                            {
                                strAppIDs = _configuration.GetValue<string>("SeatsRemainingAppIDs");
                            }

                            if ((!selected || (_configuration.GetValue<bool>("EnableUPPCabinTextDisplay") && selected && displayProduct.ProductType.ToUpper().Contains("ECO-PREMIUM"))) && string.IsNullOrEmpty(flight.CabinDisclaimer) && !string.IsNullOrEmpty(description))
                            {
                                if (!string.IsNullOrEmpty(requestedCabin))
                                {
                                    if (string.IsNullOrEmpty(flight.YaDiscount))
                                    {
                                        if (requestedCabin.Trim().ToUpper().Contains("BUS") && !displayProduct.ProductType.ToUpper().Contains("BUS"))
                                        {
                                            flight.CabinDisclaimer = GetCabinNameFromColumn(displayProduct.ProductType, columnInfo, displayProduct.Description);

                                        }
                                        else if (requestedCabin.Trim().ToUpper().Contains("FIRST") && !displayProduct.ProductType.ToUpper().Contains("FIRST"))
                                        {
                                            flight.CabinDisclaimer = GetCabinNameFromColumn(displayProduct.ProductType, columnInfo, displayProduct.Description);

                                        }
                                        else if (requestedCabin.Trim().ToUpper().Contains("ECONOMY") && !displayProduct.ProductType.ToUpper().Contains("ECONOMY"))
                                        {
                                            flight.CabinDisclaimer = GetCabinNameFromColumn(displayProduct.ProductType, columnInfo, displayProduct.Description);
                                        }

                                        // *** ALM #23244 fixed booking cabin disclamier - Victoria July 9. 2015

                                        if (flight.CabinDisclaimer != "Economy")
                                        {
                                            if (mixedCabin)
                                            {
                                                flight.CabinDisclaimer = "Mixed cabin";
                                            }
                                            else if (flight.CabinDisclaimer.IndexOf("Business") != -1)
                                            {
                                                flight.CabinDisclaimer = "Business";
                                            }
                                            else if (flight.CabinDisclaimer.IndexOf("First") != -1)
                                            {
                                                flight.CabinDisclaimer = "First";
                                            }
                                        }
                                    }
                                    if (requestedCabin.Trim().ToUpper().Contains("BUS"))
                                    {
                                        flight.PreferredCabinName = "Business";
                                    }
                                    else if (requestedCabin.Trim().ToUpper().Contains("FIRST"))
                                    {
                                        flight.PreferredCabinName = "First";
                                    }
                                    else
                                    {
                                        flight.PreferredCabinName = "Economy";
                                    }
                                    flight.PreferredCabinMessage = "not available";
                                }
                            }
                            else if (string.IsNullOrEmpty(flight.CabinDisclaimer) && mixedCabin && string.IsNullOrEmpty(flight.YaDiscount))
                            {
                                flight.CabinDisclaimer = GetMixedCabinTextForFlight(segment);
                            }
                            //Modified this to check if it's a "Seats Remaining app and if so, don't set this value - JD
                            else if (string.IsNullOrEmpty(flight.CabinDisclaimer) && seatsRemaining < 9 && seatsRemaining > 0 && !strAppIDs.Contains("|" + session.AppID.ToString() + "|"))
                            {
                                if (appID != 0 && _shoppingUtility.FeatureVersionCheck(appID, appVersion, "EnableVerbiage", "AndroidEnableVerbiageVersion", "iPhoneEnableVerbiageVersion"))
                                {
                                    if (!displayProduct.ProductType.ToUpper().Contains("ECO-BASIC") && _configuration.GetValue<bool>("HideBasicEconomyVerbiage"))
                                    {
                                        flight.AvailSeatsDisclaimer = seatsRemaining == 1 ? (seatsRemaining.ToString() + _configuration.GetValue<string>("TicketsRemainingVerbiage")).Replace("tickets", "ticket") : seatsRemaining.ToString() + _configuration.GetValue<string>("TicketsRemainingVerbiage");
                                        if (session.IsFSRRedesign)
                                        {
                                            flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, TicketsLeftSegmentInfo(flight.AvailSeatsDisclaimer));
                                        }
                                    }
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(flight.YaDiscount))
                                    {
                                        flight.CabinDisclaimer = seatsRemaining == 1 ? (seatsRemaining.ToString() + _configuration.GetValue<string>("SeatsRemaingVerbiage")).Replace("seats", "seat") : seatsRemaining.ToString() + _configuration.GetValue<string>("SeatsRemaingVerbiage");
                                    }
                                }
                            }
                            //Added this check if it's a "Seats Remaining app set the new label value - JD
                            if (strAppIDs.Contains("|" + session.AppID.ToString() + "|"))
                            {
                                if (seatsRemaining < 9 && seatsRemaining > 0)
                                {
                                    flight.AvailSeatsDisclaimer = seatsRemaining == 1 ? (seatsRemaining.ToString() + _configuration.GetValue<string>("SeatsRemaingVerbiage")).Replace("seats", "seat") : seatsRemaining.ToString() + _configuration.GetValue<string>("SeatsRemaingVerbiage");
                                }
                            }
                            if (appID != 0 && _shoppingUtility.FeatureVersionCheck(appID, appVersion, "EnableVerbiage", "AndroidEnableVerbiageVersion", "iPhoneEnableVerbiageVersion") && string.IsNullOrEmpty(flight.AvailSeatsDisclaimer))
                            {
                                if (seatsRemaining < 9 && seatsRemaining > 0)
                                {
                                    if (!displayProduct.ProductType.ToUpper().Contains("ECO-BASIC") && _configuration.GetValue<bool>("HideBasicEconomyVerbiage"))
                                    {
                                        flight.AvailSeatsDisclaimer = seatsRemaining == 1 ? (seatsRemaining.ToString() + _configuration.GetValue<string>("TicketsRemainingVerbiage")).Replace("tickets", "ticket") : seatsRemaining.ToString() + _configuration.GetValue<string>("TicketsRemainingVerbiage");

                                        if (session.IsFSRRedesign)
                                        {
                                            flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, TicketsLeftSegmentInfo(flight.AvailSeatsDisclaimer));
                                        }
                                    }
                                }
                            }

                            //if (!string.IsNullOrEmpty(flight.YaDiscount))
                            //{
                            //    flight.CabinDisclaimer = null; // Young Adult discount trump the cabin mismatch & mixed cabin message.
                            //}
                        }
                    }
                    #endregion
                    if (ci == null)
                    {
                        ci = TopHelper.GetCultureInfo("");
                    }

                    #region
                    if (segment.Amenities != null && segment.Amenities.Count > 0)
                    {
                        foreach (var amenity in segment.Amenities)
                        {
                            switch (amenity.Key.ToLower())
                            {
                                case "audiovideoondemand":
                                    flight.HasAVOnDemand = amenity.IsOffered;
                                    break;
                                case "beverages":
                                    flight.HasBeverageService = amenity.IsOffered;
                                    break;
                                case "directv":
                                    flight.HasDirecTV = amenity.IsOffered;
                                    break;
                                case "economylieflatseating":
                                    flight.HasEconomyLieFlatSeating = amenity.IsOffered;
                                    break;
                                case "economymeal":
                                    flight.HasEconomyMeal = amenity.IsOffered;
                                    break;
                                case "firstclasslieflatseating":
                                    flight.HasFirstClassLieFlatSeating = amenity.IsOffered;
                                    break;
                                case "firstclassmeal":
                                    flight.HasFirstClassMeal = amenity.IsOffered;
                                    break;
                                case "inseatpower":
                                    flight.HasInSeatPower = amenity.IsOffered;
                                    break;
                                case "wifi":
                                    flight.HasWifi = amenity.IsOffered;
                                    break;
                            }
                        }
                    }
                    #endregion
                    //flight.Cabin = string.IsNullOrEmpty(segment.CabinType) ? "" : segment.CabinType.Trim();

                    flight.Cabin = GetCabinDescription(flight.ServiceClassDescription);

                    flight.ChangeOfGauge = segment.ChangeOfPlane;
                    if (segment.Connections != null && segment.Connections.Count > 0)
                    {
                        if (_mOBSHOPDataCarrier == null)
                            _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                        var tupleflightRes = await GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, segment.Connections, requestedCabin, ci, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR);
                        flight.Connections = tupleflightRes.shopflight;
                        ci = tupleflightRes.ci;
                    }

                    flight.ConnectTimeMinutes = segment.ConnectTimeMinutes > 0 ? GetFormattedTravelTime(segment.ConnectTimeMinutes) : string.Empty;

                    flight.DepartDate = FormatDate(segment.DepartDateTime);
                    flight.DepartTime = FormatTime(segment.DepartDateTime);
                    flight.Destination = segment.Destination;
                    flight.DestinationDate = FormatDate(segment.DestinationDateTime);
                    flight.DestinationTime = FormatTime(segment.DestinationDateTime);
                    flight.DepartureDateTime = FormatDateTime(segment.DepartDateTime);
                    if (IsTripPlanSearch(session.TravelType))
                    {
                        flight.DepartureDateFormated = FormatDateTimeTripPlan(segment.DepartDateTime);
                    }
                    flight.ArrivalDateTime = FormatDateTime(segment.DestinationDateTime);

                    string destinationName = string.Empty;
                    if (!string.IsNullOrEmpty(flight.Destination))
                    {
                        destinationName = await GetAirportNameFromSavedList(flight.Destination);
                    }
                    if (string.IsNullOrEmpty(destinationName))
                    {
                        flight.DestinationDescription = segment.DestinationDescription;
                    }
                    else
                    {
                        flight.DestinationDescription = destinationName;
                    }
                    flight.DestinationCountryCode = segment.DestinationCountryCode;
                    flight.OriginCountryCode = segment.OriginCountryCode;
                    if (_configuration.GetValue<bool>("AdvisoryMsgUpdateEnable"))
                    {
                        flight.DestinationStateCode = !string.IsNullOrEmpty(segment.DestinationStateCode) ? segment.DestinationStateCode : string.Empty;
                        flight.OriginStateCode = !string.IsNullOrEmpty(segment.OriginStateCode) ? segment.OriginStateCode : string.Empty;
                    }
                    flight.EquipmentDisclosures = GetEquipmentDisclosures(segment.EquipmentDisclosures);
                    flight.FareBasisCode = segment.FareBasisCode;
                    flight.FlightNumber = segment.FlightNumber;
                    flight.GroundTime = segment.GroundTimeMinutes.ToString();
                    flight.InternationalCity = segment.InternationalCity;
                    flight.IsConnection = segment.IsConnection;
                    flight.MarketingCarrier = segment.MarketingCarrier;
                    flight.MarketingCarrierDescription = segment.MarketingCarrierDescription;
                    flight.Miles = segment.MileageActual.ToString();

                    if (_configuration.GetValue<string>("ReturnShopSelectTripOnTimePerformance") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnShopSelectTripOnTimePerformance").ToString()))
                    {
                        flight.OnTimePerformance = PopulateOnTimePerformanceSHOP(segment.OnTimePerformanceInfo);
                    }
                    flight.OperatingCarrier = segment.OperatingCarrier;
                    if (_configuration.GetValue<bool>("EnableOperatingCarrierShortForDisclosureText"))
                    {
                        if (
                            (flight.OperatingCarrier != null && flight.OperatingCarrier.ToUpper() != "UA") ||
                            (segment.OperatingCarrierShort != null && (
                                segment.OperatingCarrierShort.ToUpper().Contains("EXPRESS") ||
                                segment.OperatingCarrierShort.ToUpper().Contains("AMTRAK")
                            ))
                        )
                        {
                            flight.OperatingCarrierDescription = !string.IsNullOrEmpty(segment.OperatingCarrierShort) ? segment.OperatingCarrierShort : "";
                        }
                    }
                    else
                    {
                        if (
                            (flight.OperatingCarrier != null && flight.OperatingCarrier.ToUpper() != "UA") ||
                            (segment.OperatingCarrierDescription != null && (
                                segment.OperatingCarrierDescription.ToUpper().Contains("EXPRESS") ||
                                segment.OperatingCarrierDescription.ToUpper().Contains("AMTRAK")
                            ))
                        )
                        {
                            if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && flight.OperatingCarrier.ToUpper() == "XE")
                            {
                                flight.OperatingCarrierDescription = !string.IsNullOrEmpty(segment.OperatingCarrierDescription) ? segment.OperatingCarrierDescription : "";
                            }
                            else
                            {
                                TextInfo ti = ci.TextInfo;
                                flight.OperatingCarrierDescription = !string.IsNullOrEmpty(segment.OperatingCarrierDescription) ? ti.ToTitleCase(segment.OperatingCarrierDescription.ToLower()) : "";
                            }
                        }
                    }

                    flight.Origin = segment.Origin;

                    string originName = string.Empty;
                    if (!string.IsNullOrEmpty(flight.Origin))
                    {
                        originName = await GetAirportNameFromSavedList(flight.Origin);
                    }
                    if (string.IsNullOrEmpty(originName))
                    {
                        flight.OriginDescription = segment.OriginDescription;
                    }
                    else
                    {
                        flight.OriginDescription = originName;
                    }

                    if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI"))
                    {
                        //with share trip flag OriginDescription=Chicago, IL, US (ORD)
                        flight.OriginDecodedWithCountry = segment.OriginDescription;
                        flight.DestinationDecodedWithCountry = segment.DestinationDescription;
                    }

                    //Warnings
                    if (segment.Warnings != null && segment.Warnings.Count > 0)
                    {
                        foreach (Warning warn in segment.Warnings)
                        {
                            if (warn.Key.Trim().ToUpper() == "OVERNIGHTCONN")
                            {
                                flight.OvernightConnection = string.IsNullOrEmpty(_configuration.GetValue<string>("OvernightConnectionMessage")) ? _configuration.GetValue<string>("OvernightConnectionMessage") : warn.Title;
                            }

                            if (_configuration.GetValue<bool>("EnableChangeOfAirport") && warn != null && !string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == "CHANGE_OF_AIRPORT_SLICE" && !session.IsReshopChange)
                            {
                                flight.AirportChange = !string.IsNullOrEmpty(_configuration.GetValue<string>("CHANGE_OF_AIRPORT_SLICE")) ? _configuration.GetValue<string>("CHANGE_OF_AIRPORT_SLICE") : warn.Key;
                            }

                            if (session.IsFSRRedesign)
                            {
                                SetSegmentInfoMessages(flight, warn);
                            }
                        }
                    }
                    flight.StopDestination = segment.StopDestination;
                    bool changeOfGauge = false;
                    if (segment.StopInfos != null && segment.StopInfos.Count > 0)
                    {
                        flight.StopInfos = new List<Model.Shopping.MOBSHOPFlight>();
                        flight.ShowSeatMap = true;
                        bool isFlightDestionUpdated = false;
                        int travelMinutes = segment.TravelMinutes;

                        foreach (Flight stop in segment.StopInfos)
                        {
                            if (segment.EquipmentDisclosures != null && !string.IsNullOrEmpty(segment.EquipmentDisclosures.EquipmentType) && stop.EquipmentDisclosures != null && !string.IsNullOrEmpty(stop.EquipmentDisclosures.EquipmentType))
                            {
                                if (segment.EquipmentDisclosures.EquipmentType.Trim() == stop.EquipmentDisclosures.EquipmentType.Trim())
                                {
                                    flight.ChangeOfGauge = true;
                                    flight.ChangeOfPlane = segment.ChangeOfPlane;
                                    flight.IsThroughFlight = !segment.ChangeOfPlane;
                                    List<Flight> stops = new List<Flight>();
                                    stops.Add(stop);
                                    if (_mOBSHOPDataCarrier == null)
                                        _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                                    var tupleflightRes = await GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, stops, requestedCabin, ci, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR);
                                    List<Model.Shopping.MOBSHOPFlight> stopFlights = tupleflightRes.shopflight;
                                    ci = tupleflightRes.ci;
                                    foreach (Model.Shopping.MOBSHOPFlight sf in stopFlights)
                                    {
                                        sf.ChangeOfGauge = true;
                                        if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes"))
                                        {
                                            sf.IsThroughFlight = !stop.ChangeOfPlane;
                                            sf.ChangeOfPlane = stop.ChangeOfPlane;
                                            sf.GroundTime = GetFormattedGroundTime(sf.GroundTime, sf.Origin, segment.Warnings, stop.ChangeOfPlane);
                                        }
                                    }


                                    ///245704 - PROD: Mapp - Incorrect segments info is displayed for a 2-Stop flight UA1666 on choose your travel experience screen
                                    ///Srini - 03/20/2018
                                    if (_configuration.GetValue<bool>("BugFixToggleFor18C"))
                                    {
                                        flight.Destination = stop.Origin;
                                        if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes") && !isFlightDestionUpdated)
                                        {
                                            string destination = string.Empty;
                                            if (!string.IsNullOrEmpty(flight.Destination))
                                            {
                                                destination = await GetAirportNameFromSavedList(flight.Destination);
                                            }
                                            if (string.IsNullOrEmpty(destination))
                                            {
                                                flight.DestinationDescription = stop.OriginDescription;
                                            }
                                            else
                                            {
                                                flight.DestinationDescription = destination;
                                            }
                                            flight.DestinationDecodedWithCountry = stop.OriginDescription;
                                            flight.DestinationStateCode = stop.OriginStateCode;
                                            flight.DestinationCountryCode = stop.OriginCountryCode;
                                        }
                                        isFlightDestionUpdated = true;
                                    }

                                    flight.StopInfos.AddRange(stopFlights);
                                }
                                else
                                {
                                    changeOfGauge = true;
                                    ///245704 - PROD: Mapp - Incorrect segments info is displayed for a 2-Stop flight UA1666 on choose your travel experience screen
                                    ///Srini - 03/20/2018
                                    if (!_configuration.GetValue<bool>("BugFixToggleFor18C") || (_configuration.GetValue<bool>("BugFixToggleFor18C") && !isFlightDestionUpdated))
                                    {
                                        flight.Destination = stop.Origin;
                                        isFlightDestionUpdated = true;
                                    }

                                    string destination = string.Empty;
                                    if (!string.IsNullOrEmpty(flight.Destination))
                                    {
                                        destination = await GetAirportNameFromSavedList(flight.Destination);
                                    }
                                    if (string.IsNullOrEmpty(destination))
                                    {
                                        flight.DestinationDescription = stop.OriginDescription;
                                    }
                                    else
                                    {
                                        flight.DestinationDescription = destination;
                                    }

                                    flight.DestinationDate = FormatDate(Convert.ToDateTime(stop.DepartDateTime).AddMinutes(-stop.GroundTimeMinutes).ToString());
                                    flight.DestinationTime = FormatTime(Convert.ToDateTime(stop.DepartDateTime).AddMinutes(-stop.GroundTimeMinutes).ToString());
                                    flight.ArrivalDateTime = FormatDateTime(Convert.ToDateTime(stop.DepartDateTime).AddMinutes(-stop.GroundTimeMinutes).ToString());
                                    flight.TravelTime = segment.TravelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes > 0 ? GetFormattedTravelTime(segment.TravelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes) : string.Empty;
                                    flight.ChangeOfPlane = segment.ChangeOfPlane;
                                    Model.Shopping.MOBSHOPFlight stopFlight = new Model.Shopping.MOBSHOPFlight();

                                    stopFlight.EquipmentDisclosures = GetEquipmentDisclosures(stop.EquipmentDisclosures);
                                    stopFlight.FlightNumber = stop.FlightNumber;
                                    stopFlight.ChangeOfGauge = stop.ChangeOfPlane;
                                    stopFlight.ShowSeatMap = true;
                                    stopFlight.DepartDate = FormatDate(stop.DepartDateTime);
                                    stopFlight.DepartTime = FormatTime(stop.DepartDateTime);
                                    stopFlight.Origin = stop.Origin;
                                    stopFlight.Destination = stop.Destination;
                                    stopFlight.DestinationDate = FormatDate(stop.DestinationDateTime);
                                    stopFlight.DestinationTime = FormatTime(stop.DestinationDateTime);
                                    stopFlight.DepartureDateTime = FormatDateTime(stop.DepartDateTime);
                                    stopFlight.ArrivalDateTime = FormatDateTime(stop.DestinationDateTime);
                                    stopFlight.IsCovidTestFlight = stop.IsCovidTestingRequired;
                                    stopFlight.GroundTime = stop.GroundTimeMinutes > 0 ? GetFormattedTravelTime(stop.GroundTimeMinutes) : String.Empty;
                                    stopFlight.ChangeOfPlane = stop.ChangeOfPlane;
                                    if (!_configuration.GetValue<bool>("DisableOperatingCarrierFlightNumberChanges"))
                                    {
                                        stopFlight.OperatingCarrierFlightNumber = stop.OriginalFlightNumber;
                                    }

                                    ///57783 - BUG 390826 CSL:  Class of service information is not included for certain segments on Mobile
                                    ///Srini - 02/20/2018
                                    if (_configuration.GetValue<bool>("BugFixToggleFor18B") && displayProductForStopInfo != null)
                                    {
                                        stopFlight.Meal = !string.IsNullOrEmpty(displayProductForStopInfo.MealDescription) ? displayProductForStopInfo.MealDescription : "None";
                                        stopFlight.ServiceClass = displayProductForStopInfo.BookingCode;

                                        stopFlight.Messages = new List<Model.Shopping.MOBSHOPMessage>();

                                        Model.Shopping.MOBSHOPMessage msg = new Model.Shopping.MOBSHOPMessage();
                                        msg.FlightNumberField = flight.FlightNumber;
                                        msg.TripId = flight.TripId;
                                        msg.MessageCode = displayProductForStopInfo.Description + " (" + displayProductForStopInfo.BookingCode + ")";
                                        stopFlight.Messages.Add(msg);

                                        msg = new Model.Shopping.MOBSHOPMessage();
                                        msg.FlightNumberField = flight.FlightNumber;
                                        msg.TripId = flight.TripId;
                                        msg.MessageCode = !string.IsNullOrEmpty(displayProductForStopInfo.MealDescription) ? displayProductForStopInfo.MealDescription : "None";
                                        stopFlight.Messages.Add(msg);

                                        flight.MatchServiceClassRequested = selectedForStopInfo;
                                    }
                                    if (session.IsFSRRedesign)
                                    {
                                        if (stopFlight.Messages != null && stopFlight.Messages.Count > 0)
                                        {
                                            if (stopFlight.ShoppingProducts != null && stopFlight.ShoppingProducts.Count > 0 && stopFlight.ShoppingProducts.Any(p => p.IsMixedCabin))
                                            {
                                                var mixedCabinText = _configuration.GetValue<string>("MixedCabinProductBadgeText");
                                                var firstMessage = stopFlight.Messages.First().MessageCode;
                                                var mixedCabinColorCode = _configuration.GetValue<string>("MixedCabinTextColorCode");
                                                if (await _featureSettings.GetFeatureSettingValue("EnableNewBackGroundColor").ConfigureAwait(false) && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, _configuration.GetValue<string>("Android_Atmos2_New_MixedCabinTextColorCode_AppVersion"), _configuration.GetValue<string>("Iphone_Atmos2_New_MixedCabinTextColorCode_AppVersion")))
                                                {
                                                    mixedCabinColorCode = _configuration.GetValue<string>("Atmos2_New_MixedCabinTextColorCode");
                                                }
                                                var message1 = string.Format("<span style='color:{0}'>{1} {2}</span>", mixedCabinColorCode, mixedCabinText, firstMessage);
                                                var message2 = stopFlight.Messages.Last().MessageCode;
                                                stopFlight.LineOfFlightMessage = string.Join(" / ", message1, message2);
                                            }
                                            else
                                            {
                                                stopFlight.LineOfFlightMessage = string.Join(" / ", stopFlight.Messages.Select(x => x.MessageCode));
                                            }
                                        }
                                    }
                                    //Added Carrier code for the bug 218201 by Niveditha.Didn't add Marketing Carrier description as per suggestion by Jada sreenivas.
                                    stopFlight.MarketingCarrier = flight.MarketingCarrier;

                                    string stopDestination = string.Empty;
                                    if (!string.IsNullOrEmpty(stopFlight.Destination))
                                    {
                                        stopDestination = await GetAirportNameFromSavedList(stopFlight.Destination);
                                    }
                                    if (string.IsNullOrEmpty(stopDestination))
                                    {
                                        stopFlight.DestinationDescription = stop.DestinationDescription;
                                    }
                                    else
                                    {
                                        stopFlight.DestinationDescription = stopDestination;
                                    }

                                    string stopOrigin = string.Empty;
                                    if (!string.IsNullOrEmpty(stopFlight.Origin))
                                    {
                                        stopOrigin = await GetAirportNameFromSavedList(stopFlight.Origin);
                                    }
                                    if (string.IsNullOrEmpty(stopOrigin))
                                    {
                                        stopFlight.OriginDescription = stop.OriginDescription;
                                    }
                                    else
                                    {
                                        stopFlight.OriginDescription = stopOrigin;
                                    }

                                    if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI"))
                                    {
                                        //with share trip flag OriginDescription=Chicago, IL, US (ORD)
                                        stopFlight.OriginDecodedWithCountry = stop.OriginDescription;
                                        stopFlight.DestinationDecodedWithCountry = stop.DestinationDescription;
                                    }

                                    stopFlight.TravelTime = stop.TravelMinutes > 0 ? GetFormattedTravelTime(stop.TravelMinutes) : string.Empty;

                                    if (session.IsFSRRedesign)
                                    {
                                        if (stop.Warnings != null && stop.Warnings.Count > 0)
                                        {
                                            foreach (Warning warn in stop.Warnings)
                                            {
                                                SetSegmentInfoMessages(stopFlight, warn);
                                            }
                                        }
                                    }

                                    flight.StopInfos.Add(stopFlight);
                                }
                            }
                            if (_configuration.GetValue<bool>("DoubleDisclosureFix"))
                            {
                                travelMinutes = travelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes > 0 ? travelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes : 0;
                            }
                        }
                        if (_configuration.GetValue<bool>("DoubleDisclosureFix"))
                        {
                            flight.Destination = segment.StopInfos[0].Origin;
                            flight.DestinationDate = FormatDate(Convert.ToDateTime(segment.StopInfos[0].DepartDateTime).AddMinutes(-segment.StopInfos[0].GroundTimeMinutes).ToString());
                            flight.DestinationTime = FormatTime(Convert.ToDateTime(segment.StopInfos[0].DepartDateTime).AddMinutes(-segment.StopInfos[0].GroundTimeMinutes).ToString());
                            flight.TravelTime = travelMinutes > 0 ? GetFormattedTravelTime(travelMinutes) : string.Empty;
                        }
                    }

                    flight.Stops = segment.StopInfos != null ? segment.StopInfos.Count : 0;

                    if (_configuration.GetValue<bool>("DoubleDisclosureFix"))
                    {
                        if (!changeOfGauge && string.IsNullOrEmpty(flight.TravelTime))
                            flight.TravelTime = segment.TravelMinutes > 0 ? GetFormattedTravelTime(segment.TravelMinutes) : string.Empty;
                    }
                    else
                    {
                        if (!changeOfGauge)
                        {
                            flight.TravelTime = segment.TravelMinutes > 0 ? GetFormattedTravelTime(segment.TravelMinutes) : string.Empty;
                        }
                    }
                    flight.TotalTravelTime = segment.TravelMinutesTotal > 0 ? GetFormattedTravelTime(segment.TravelMinutesTotal) : string.Empty;
                    flight.TravelTimeInMinutes = segment.TravelMinutes;
                    flight.TripId = segment.BBXSolutionSetId;

                    if (_mOBSHOPDataCarrier == null)
                        _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                    var tupleProdRes = await PopulateProducts(_mOBSHOPDataCarrier, segment.Products, sessionId, flight, requestedCabin, segment, lowestFare, columnInfo, premierStatusLevel, fareClass, isELFFareDisplayAtFSR, appVersion);
                    flight = tupleProdRes.flight;
                    flight.ShoppingProducts = tupleProdRes.shoppingProducts;
                    SetAutoFocusIfMissed(session, isELFFareDisplayAtFSR, flight.ShoppingProducts, bestProductType);
                    #endregion
                    if (isConnection)
                    {
                        flights.Add(flight);
                    }
                    else
                    {
                        if (flight.ShoppingProducts != null && flight.ShoppingProducts.Count > 0)
                            flights.Add(flight);
                        #region REST SHOP and Select Trip Tuning Changes - Venkat Apirl 20, 2015
                        if (_configuration.GetValue<string>("HandlePagingAtRESTSide") != null && Convert.ToBoolean(_configuration.GetValue<string>("HandlePagingAtRESTSide").ToString()) && flights.Count == Convert.ToInt32(_configuration.GetValue<string>("OrgarnizeResultsRequestPageSize").ToString()))
                        {
                            break;
                        }
                        #endregion
                    }
                }
            }

            return (flights, ci);
        }

        private string GetFormattedGroundTime(string groundTime, string origin, List<Warning> warnings, bool isChangeOfPlane)
        {
            if (int.TryParse(groundTime, out int minutes))
            {
                if (minutes == 0)
                    return string.Empty;

                if (string.IsNullOrEmpty(origin))
                    return GetFormattedTravelTime(minutes);

                if (isChangeOfPlane)
                    return GetFormattedTravelTime(minutes);

                var groundTimeWarningEntry = warnings?.FirstOrDefault(w => string.Equals(w.Key, "stop", StringComparison.OrdinalIgnoreCase))
                                                                                               ?.Stops?.FirstOrDefault(s => string.Equals(s.AdvisoryAirportCode, origin, StringComparison.OrdinalIgnoreCase));
                if (groundTimeWarningEntry == null)
                    return GetFormattedTravelTime(minutes);

                return groundTimeWarningEntry.Duration;
            }
            else
                return groundTime;
        }

        private string FormatDateTimeTripPlan(string dateTimeString)
        {
            string result = string.Empty;
            DateTime dateTime = new DateTime(0);
            DateTime.TryParse(dateTimeString, out dateTime);
            result = string.Format("{0:ddd, MMM dd}", dateTime);

            return result;
        }
        private bool IsTripPlanSearch(string travelType)
        {
            var isTripPlannerViewEnabled = _configuration.GetValue<bool>("EnableTripPlannerView");
            return isTripPlannerViewEnabled && (travelType == MOBTripPlannerType.TPSearch.ToString() || travelType == MOBTripPlannerType.TPEdit.ToString());
        }

        private MOBSHOPFlight ReShopAirfareCreditDisplayFSR(CultureInfo ci, Product product, MOBSHOPFlight flight)
        {
            var price = product.Prices;

            if (price != null && price.Any())
            {
                decimal displayprice = ReshopAirfareDisplayValueInDecimal(price);

                if (displayprice.CompareTo(decimal.Zero) == 0)
                {
                    decimal displayCredit = ReshopAirfareCreditDisplayInDecimal(price, "refundPrice");

                    if (displayCredit.CompareTo(decimal.Zero) < 0)
                    {
                        displayCredit = displayCredit * -1;
                    }

                    string strDisplayCredit
                        = TopHelper.FormatAmountForDisplay(displayCredit, ci, true);

                    flight.ReshopCreditColor = CreditTypeColor.GREEN;

                    //displayPrice = string.Concat("+", displayPrice);
                    //AirfareDisplayValue
                    if (product.CreditType == CreditTypes.Refund)
                    {
                        flight.ReshopFees = CreditType.REFUND.GetDisplayName();
                        flight.IsReshopCredit = true;
                    }
                    else if (product.CreditType == CreditTypes.FlightCredit)
                    {
                        flight.ReshopFees = CreditType.FLIGHTCREDIT.GetDisplayName();
                        flight.IsReshopCredit = true;
                    }

                    flight.AirfareDisplayValue = strDisplayCredit;
                }
            }
            return flight;
        }

        private decimal ReshopAirfareCreditDisplayInDecimal(List<PricingItem> price, string priceType)
        {
            decimal retVal = 0;
            if (price.Exists(p => string.Equals(p.PricingType, priceType, StringComparison.OrdinalIgnoreCase)))
                retVal = price.FirstOrDefault(p => string.Equals(p.PricingType, priceType, StringComparison.OrdinalIgnoreCase)).Amount;
            return retVal;
        }

        private void SetSegmentInfoMessages(Model.Shopping.MOBSHOPFlight flight, Warning warn)
        {
            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("ARRIVAL_Slice")
                && !string.IsNullOrEmpty(warn.Title))
            {
                flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, ArrivesNextDaySegmentInfo(warn.Title));
            }

            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("CHANGE_OF_AIRPORT_SLICE") || (_configuration.GetValue<bool>("EnableAwardFSRChanges") && !string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("CHANGE_OF_AIRPORT_SLICE_KEY")))
            {
                if (!string.IsNullOrEmpty(warn.Title))
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, AirportChangeSegmentInfo(warn.Title));
                }
                else
                if (warn.Messages != null && warn.Messages.Count > 0)
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, AirportChangeSegmentInfo(warn.Messages[0]));
                }
            }

            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("SubjectToReceiptOfGovtAuthority_SLICE"))
            {
                if (!string.IsNullOrEmpty(warn.Title))
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, SubjectOfReceiptOfGovtAuthSegmentInfo(warn.Title));
            }

            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("LONG_LAYOVER_SLICE"))
            {
                if (!string.IsNullOrEmpty(warn.Title))
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, LonglayoverSegmentInfo(warn.Title));
                }
                else
                if (warn.Messages != null && warn.Messages.Count > 0)
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, LonglayoverSegmentInfo(warn.Messages[0]));
                }
            }

            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("Red-eyeFlight_Slice"))
            {
                if (!string.IsNullOrEmpty(warn.Title))
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, RedEyeFlightSegmentInfo(warn.Title));
                }
                else
                if (warn.Messages != null && warn.Messages.Count > 0)
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, RedEyeFlightSegmentInfo(warn.Messages[0]));
                }
            }

            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("RISKYCONNECTION_SLICE"))
            {
                if (!string.IsNullOrEmpty(warn.Title))
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, RiskyConnectionSegmentInfo(warn.Title));
                }
                else
                if (warn.Messages != null && warn.Messages.Count > 0)
                {
                    //if (!string.IsNullOrEmpty(warn.Title))
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, RiskyConnectionSegmentInfo(warn.Messages[0]));
                }
            }
            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("TerminalChange_SLICE"))
            {
                if (!string.IsNullOrEmpty(warn.Title))
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, TerminalChangeSegmentInfo(warn.Title));
                }
                else
                if (warn.Messages != null && warn.Messages.Count > 0)
                {
                    //if (!string.IsNullOrEmpty(warn.Title))
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, TerminalChangeSegmentInfo(warn.Messages[0]));
                }
            }
        }

        private Model.Shopping.SHOPOnTimePerformance PopulateOnTimePerformanceSHOP(United.Service.Presentation.ReferenceDataModel.DOTAirlinePerformance onTimePerformance)
        {
            Model.Shopping.SHOPOnTimePerformance shopOnTimePerformance = null;
            if (_configuration.GetValue<string>("ReturnOnTimePerformance") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnOnTimePerformance")))
            {
                #region
                if (onTimePerformance != null)
                {
                    shopOnTimePerformance = new Model.Shopping.SHOPOnTimePerformance();
                    shopOnTimePerformance.Source = onTimePerformance.Source;
                    shopOnTimePerformance.DOTMessages = new SHOPOnTimeDOTMessages();
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
                        if (!int.TryParse(onTimePerformance.ArrivalLateRate.Replace("%", ""), out delay))
                        {
                            delay = -1;
                            onTimePerformance.ArrivalLateRate = "";
                        }
                        shopOnTimePerformance.PctOnTimeDelayed = delay < 0 ? "---" : delay.ToString();
                        int onTime = -1;
                        if (!int.TryParse(onTimePerformance.ArrivalOnTimeRate.Replace("%", ""), out onTime))
                        {
                            onTime = -1;
                            onTimePerformance.ArrivalOnTimeRate = "";
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
                    shopOnTimePerformance = new Model.Shopping.SHOPOnTimePerformance();
                    shopOnTimePerformance.DOTMessages = new SHOPOnTimeDOTMessages();
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

        private void SetAutoFocusIfMissed(Session session, bool isELFFareDisplayAtFSR, List<Model.Shopping.MOBSHOPShoppingProduct> shopProds, string productTypeBestMatched)
        {
            if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == MOBTripPlannerType.TPSearch.ToString() ||
                session.TravelType == MOBTripPlannerType.TPEdit.ToString())
            {
                if (!shopProds.Any(p => p.IsSelectedCabin))
                {
                    if (!string.IsNullOrEmpty(productTypeBestMatched) && shopProds.Any(p => p.Type == productTypeBestMatched))
                    {
                        shopProds.First(p => p.Type == productTypeBestMatched).IsSelectedCabin = true;
                    }
                    else
                    {
                        var priorityProduct = shopProds.FirstOrDefault(p => (!isELFFareDisplayAtFSR) ? p?.Type?.ToUpper() != "ECO-BASIC" : true && p.PriceAmount > 0);

                        if (priorityProduct != null)
                            priorityProduct.IsSelectedCabin = true;
                    }
                }
            }
        }

        private Product GetMatrixDisplayProduct(ProductCollection products, string fareSelected, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, out CultureInfo ci, out bool isSelectedFareFamily, out string serviceClassDesc, out bool isMixedCabin, out int seatsRemaining, string fareClass, bool isConnectionOrStopover = false, bool isELFFareDisplayAtFSR = true)
        {
            var bestMatch = new Product();
            isSelectedFareFamily = false;
            isMixedCabin = false;
            serviceClassDesc = "";
            seatsRemaining = 0;
            const int minimumSeatsRemaining = 4;
            var isSelectedCabin = false;
            ci = null;

            var productsOrderedByPrice = products.Where(p => (p.Prices != null && p.Prices.Any())).OrderBy(p => p.Prices.First().Amount);

            foreach (var prod in productsOrderedByPrice)
            {
                if (prod.IsBundleProduct || prod.ProductId == "NAP")
                {
                    continue;
                }

                if ((isConnectionOrStopover && !string.IsNullOrEmpty(prod.ProductId)) ||
                    (prod.Prices != null && prod.Prices.Any()))
                {
                    if (!isConnectionOrStopover)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(prod.Prices[0].Currency);
                        }
                    }

                    isSelectedCabin = MatchServiceClassRequested(fareSelected, fareClass, prod.ProductType, columnInfo, isELFFareDisplayAtFSR);

                    if (isSelectedCabin)
                    {
                        bestMatch = prod;
                        isMixedCabin = !string.IsNullOrEmpty(prod.CrossCabinMessaging);
                        isSelectedFareFamily = true;
                        serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                        seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                        break;
                    }
                }

            }

            if (!isSelectedCabin)
            {
                foreach (Product prod in products)
                {
                    if (prod.IsBundleProduct || prod.ProductId == "NAP")
                    {
                        continue;
                    }

                    if ((isConnectionOrStopover && !string.IsNullOrEmpty(prod.ProductId)) || (prod.Prices != null && prod.Prices.Any()))
                    {
                        if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("FLEXIBLE"))
                        {
                            if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("BUSINESS"))
                            {
                                //if (lowestAirfare == -1M || tempProdPrice < lowestAirfare)
                                //{
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                // }
                            }
                            else if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("FIRST"))
                            {
                                //if (lowestAirfare == -1M || tempProdPrice < lowestAirfare)
                                //{
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                //}
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                // }
                            }
                        }
                        else if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("UNRESTRICTED"))
                        {
                            if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("BUSINESS"))
                            {
                                //if (lowestAirfare == -1M || tempProdPrice < lowestAirfare)
                                //{
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                // }
                            }
                            else if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("FIRST"))
                            {
                                //if (lowestAirfare == -1M || tempProdPrice < lowestAirfare)
                                //{
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                // }
                            }
                        }
                        else if (Convert.ToBoolean(_configuration.GetValue<string>("SwithAwardSelectedCabinMilesDisplay") ?? "false") && !string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("AWARD"))
                        {
                            bestMatch = prod;
                            serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                            seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                            isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                            if ((prod.ProductType.Contains("ECONOMY") && fareSelected.ToUpper().Contains("ECONOMY")) ||
                                (prod.ProductType.Contains("BUS") && (fareSelected.ToUpper().Contains("ECONOMY") || fareSelected.ToUpper().Contains("BUS"))) ||
                                (prod.ProductType.Contains("FIRST") && fareSelected.ToUpper().Contains("FIRST")))
                                break;
                        }
                        else //lowest
                        {
                            if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("BUSINESS"))
                            {
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                // }
                            }
                            else if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("FIRST"))
                            {
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                //}
                            }
                        }
                    }
                }
            }

            return bestMatch;
        }

        private bool MatchServiceClassRequested(string requestedCabin, string fareClass, string prodType, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, bool isELFFareDisplayAtFSR = true)
        {
            var match = false;
            if (!string.IsNullOrEmpty(requestedCabin))
            {
                requestedCabin = requestedCabin.Trim().ToUpper();
            }

            if (!string.IsNullOrEmpty(fareClass))
            {
                fareClass = fareClass.Trim().ToUpper();
            }

            if (!string.IsNullOrEmpty(fareClass) && prodType.ToUpper().Contains("SPECIFIED"))
            {
                match = true;
            }
            else
            {
                if (string.IsNullOrEmpty(fareClass))
                {
                    switch (requestedCabin)
                    {
                        case "ECON":
                        case "ECONOMY":
                            //Removed FLEXIBLE & UNRESTRICTED as it is not taking ECO-FLEXIBLE as selected when Economy is not available.
                            match = (prodType.ToUpper().Contains("ECON") || (isELFFareDisplayAtFSR && prodType.ToUpper().Contains("ECO-BASIC")) || prodType.ToUpper().Contains("ECO-PREMIUM")) && !prodType.ToUpper().Contains("FLEXIBLE") && !prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "ECONOMY-FLEXIBLE":
                            match = (prodType.ToUpper().Contains("ECON") || prodType.ToUpper().Contains("ECO-PREMIUM")) && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "ECONOMY-UNRESTRICTED":
                            match = (prodType.ToUpper().Contains("ECON") || prodType.ToUpper().Contains("ECO-PREMIUM")) && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "BUSINESS":
                        case "BUSINESSFIRST":
                            match = prodType.ToUpper().Contains("BUS") && !prodType.ToUpper().Contains("FLEXIBLE") && !prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "BUSINESS-FLEXIBLE":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "BUSINESS-UNRESTRICTED":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "BUSINESSFIRST-FLEXIBLE":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "BUSINESSFIRST-UNRESTRICTED":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "FIRST":
                            match = prodType.ToUpper().Contains("FIRST") && !prodType.ToUpper().Contains("FLEXIBLE") && !prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "FIRST-FLEXIBLE":
                            match = prodType.ToUpper().Contains("FIRST") && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "FIRST-UNRESTRICTED":
                            match = prodType.ToUpper().Contains("FIRST") && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "AWARDECONOMY":
                            match = prodType.ToUpper().Contains("ECON") && !prodType.ToUpper().Contains("FLEXIBLE") && !prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "AWARDECONOMY-FLEXIBLE":
                            match = prodType.ToUpper().Contains("ECON") && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "AWARDECONOMY-UNRESTRICTED":
                            match = prodType.ToUpper().Contains("ECON") && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "AWARDBUSINESS":
                        case "AWARDBUSINESSFIRST":
                            {
                                var cabinName = GetCabinNameFromColumn(prodType, columnInfo, string.Empty);
                                match = cabinName.ToUpper().Contains("BUSINESS");
                                break;
                            }
                        case "AWARDBUSINESS-FLEXIBLE":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "AWARDBUSINESS-UNRESTRICTED":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "AWARDBUSINESSFIRST-FLEXIBLE":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "AWARDBUSINESSFIRST-UNRESTRICTED":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "AWARDFIRST":
                            {
                                var cabinName = GetCabinNameFromColumn(prodType, columnInfo, string.Empty);
                                match = cabinName.ToUpper().Contains("FIRST");
                                break;
                            }
                        case "AWARDFIRST-FLEXIBLE":
                            match = prodType.ToUpper().Contains("FIRST") && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "AWARDFIRST-UNRESTRICTED":
                            match = prodType.ToUpper().Contains("FIRST") && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        default:
                            break;
                    }
                }
            }

            return match;
        }

        private async Task<(List<Model.Shopping.MOBSHOPShoppingProduct> shoppingProducts, Model.Shopping.MOBSHOPFlight flight)> PopulateProducts(MOBSHOPDataCarrier _mOBSHOPDataCarrier, ProductCollection products, string sessionId, Model.Shopping.MOBSHOPFlight flight, string cabin, Flight segment, decimal lowestAirfare, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClas, bool isELFFareDisplayAtFSR = true, string appVersion = "")
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);
            supressLMX = session.SupressLMXForAppID;
            #endregion
            if (_mOBSHOPDataCarrier == null)
                _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
            return (PopulateProducts(_mOBSHOPDataCarrier, products, cabin, segment, lowestAirfare, columnInfo, premierStatusLevel, fareClas, supressLMX, session, isELFFareDisplayAtFSR, appVersion, flight.CorporateFareIndicator, flight.YaDiscount), flight);
        }

        private List<Model.Shopping.MOBSHOPShoppingProduct> PopulateProducts(MOBSHOPDataCarrier _mOBSHOPDataCarrier, ProductCollection products, string cabin, Flight segment, decimal lowestAirfare,
            List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClas, bool supressLMX, Session session, bool isELFFareDisplayAtFSR = true, string appVersion = "", string corporateFareIndicator = "", string yaDiscount = "")
        {
            var shopProds = new List<Model.Shopping.MOBSHOPShoppingProduct>();
            if (_mOBSHOPDataCarrier == null)
                _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
            CultureInfo ci = null;

            var foundCabinSelected = false;
            var foundFirstAward = false;
            var foundBusinessAward = false;
            var foundEconomyAward = false;

            var fareClass = fareClas;

            var productIndex = -1;
            try
            {
                foreach (var prod in products)
                {
                    var isUaDiscount = !string.IsNullOrEmpty(prod.PromoDescription) &&
                                        prod.PromoDescription.Trim().ToUpper().Equals("EMPLOYEE FARE");
                    productIndex = productIndex + 1;

                    if ((prod.Prices != null && prod.Prices.Count > 0) &&
                        ((session.IsReshopChange && prod.ProductId != "NAP") || !session.IsReshopChange))
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(prod.Prices[0].Currency);
                        }

                        var newProd = TransformProductWithPriceToNewProduct(cabin, segment, lowestAirfare, columnInfo,
                            premierStatusLevel, isUaDiscount, prod, supressLMX, ci, fareClass, productIndex, ref foundCabinSelected,
                            ref foundEconomyAward, ref foundBusinessAward, ref foundFirstAward, session, isELFFareDisplayAtFSR, appVersion, corporateFareIndicator, yaDiscount);

                        if (_shoppingUtility.EnableIBELite() && !string.IsNullOrWhiteSpace(prod.ProductCode))
                        {
                            newProd.IsIBELite = _configuration.GetValue<string>("IBELiteShoppingProductCodes").IndexOf(prod.ProductCode.Trim().ToUpper()) > -1;

                            if (newProd.IsIBELite) // per clients' request when implementing IBELite
                                newProd.ShortProductName = _configuration.GetValue<string>("IBELiteShortProductName");
                        }

                        if (_shoppingUtility.EnableIBEFull() && !string.IsNullOrWhiteSpace(prod.ProductCode))
                        {
                            newProd.IsIBE = _configuration.GetValue<string>("IBEFULLShoppingProductCodes").IndexOf(prod.ProductCode.Trim().ToUpper()) > -1;

                            if (newProd.IsIBE)
                                newProd.ShortProductName = _configuration.GetValue<string>("IBEFULLShortProductName");
                        }
                        if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == MOBTripPlannerType.TPSearch.ToString() || session.TravelType == MOBTripPlannerType.TPEdit.ToString())
                        {
                            newProd.PriceApplyLabelText = GetPriceApplyLabelText(_mOBSHOPDataCarrier.SearchType);
                        }
                        shopProds.Add(newProd);
                    }
                    else
                    {
                        var newProd = TransformProductWithoutPriceToNewProduct(cabin, columnInfo, isUaDiscount, prod,
                            foundEconomyAward, foundBusinessAward, foundFirstAward, session);

                        shopProds.Add(newProd);
                    }
                }
            }
            catch (Exception ex)
            {
            }
            HighlightProductMatchingSelectedCabin(shopProds, cabin, columnInfo, fareClass, (session == null ? false : session.IsReshopChange), isELFFareDisplayAtFSR);

            //loop thru if award to finalize the award pricing blocks to all cabin option
            int econAwardCount = 0;
            int busAwardCount = 0;
            int firstAwardCount = 0;
            CalculateAwardCounts(shopProds, ref econAwardCount, ref busAwardCount, ref firstAwardCount);

            if (econAwardCount > 1 || busAwardCount > 1 || firstAwardCount > 1)
            {
                ClearMileageButtonAndAllCabinButtonText(shopProds, econAwardCount, busAwardCount, firstAwardCount);
            }

            if (shopProds != null && shopProds.Count > 0)
            {
                foreach (var shopProd in shopProds)
                {
                    var configurationProductSettings = _configuration.GetSection("productSettings").Get<ProductSection>() as ProductSection;

                    SetShortCabin(shopProd, columnInfo, configurationProductSettings);

                    if (string.IsNullOrEmpty(shopProd.Description))
                    {
                        SetShoppingProductDescriptionBasedOnProductElementDescription
                            (shopProd, columnInfo, configurationProductSettings);
                    }
                    else
                    {
                        if (shopProd.LongCabin.Equals("Economy (lowest)") && !string.IsNullOrEmpty(shopProd.AwardType))
                        {
                            shopProd.Description = string.Empty;
                        }
                    }
                }
            }

            #region awardType=saver

            List<Model.Shopping.MOBSHOPShoppingProduct> updatedShopProducts = new List<Model.Shopping.MOBSHOPShoppingProduct>();
            foreach (var mobShopProduct in shopProds)
            {
                SetIsPremierCabinSaverIfApplicable(mobShopProduct);
                updatedShopProducts.Add(mobShopProduct);
                if (_configuration.GetValue<bool>("Shopping - bPricingBySlice"))
                {
                    if (_mOBSHOPDataCarrier != null)
                        mobShopProduct.PriceFromText = _mOBSHOPDataCarrier.PriceFormText;// SetProductPriceFromText();
                }

            }

            #endregion

            return updatedShopProducts;
        }

        private void ClearMileageButtonAndAllCabinButtonText(List<Model.Shopping.MOBSHOPShoppingProduct> shopProds, int econAwardCount, int busAwardCount,
           int firstAwardCount)
        {
            int econClassCount = 0;
            int busClassCount = 0;
            int firstClassCount = 0;

            int econIdx = -1;
            int busIdx = -1;
            int firstIdx = -1;
            int econIdx2 = -1;
            int busIdx2 = -1;
            int firstIdx2 = -1;

            for (int k = 0; k < shopProds.Count; k++)
            {
                if (econAwardCount > 1)
                {
                    if (shopProds[k].LongCabin.Trim().ToUpper().Contains("ECONOMY"))
                    {
                        if (econIdx < 0)
                            econIdx = k;
                        else
                            econIdx2 = k;

                        econClassCount++;
                        if (econClassCount > 1 && econIdx2 >= 0 && shopProds[econIdx].MilesDisplayAmount > 0)
                        {
                            shopProds[econIdx2].MileageButton = -1;
                            shopProds[econIdx2].AllCabinButtonText = "";
                        }
                        else if (econClassCount > 1 && econIdx2 >= 0 && shopProds[econIdx2].MilesDisplayAmount > 0)
                        {
                            shopProds[econIdx].MileageButton = -1;
                            shopProds[econIdx].AllCabinButtonText = "";
                        }
                        else if (econClassCount > 1 && econIdx2 >= 0)
                        {
                            shopProds[econIdx2].MileageButton = -1;
                            shopProds[econIdx2].AllCabinButtonText = "";
                        }
                    }
                }
                else if (busAwardCount > 1)
                {
                    if (shopProds[k].LongCabin.Trim().ToUpper().Contains("BUS"))
                    {
                        if (busIdx < 0)
                            busIdx = k;
                        else
                            busIdx2 = k;

                        busClassCount++;
                        if (busClassCount > 1 && busIdx2 >= 0 && shopProds[busIdx].MilesDisplayAmount > 0)
                        {
                            shopProds[busIdx2].MileageButton = -1;
                            shopProds[busIdx2].AllCabinButtonText = "";
                        }
                        else if (busClassCount > 1 && busIdx2 >= 0 && shopProds[busIdx2].MilesDisplayAmount > 0)
                        {
                            shopProds[busIdx].MileageButton = -1;
                            shopProds[busIdx].AllCabinButtonText = "";
                        }
                        else if (busClassCount > 1 && busIdx2 >= 0)
                        {
                            shopProds[busIdx2].MileageButton = -1;
                            shopProds[busIdx2].AllCabinButtonText = "";
                        }
                    }
                }
                else if (firstAwardCount > 1)
                {
                    if (shopProds[k].LongCabin.Trim().ToUpper().Contains("FIRST"))
                    {
                        if (firstIdx < 0)
                            firstIdx = k;
                        else
                            firstIdx2 = k;

                        firstClassCount++;
                        if (firstClassCount > 1 && firstIdx2 >= 0 && shopProds[firstIdx].MilesDisplayAmount > 0)
                        {
                            shopProds[firstIdx2].MileageButton = -1;
                            shopProds[firstIdx2].AllCabinButtonText = "";
                        }
                        else if (firstClassCount > 1 && firstIdx2 >= 0 && shopProds[firstIdx2].MilesDisplayAmount > 0)
                        {
                            shopProds[firstIdx].MileageButton = -1;
                            shopProds[firstIdx].AllCabinButtonText = "";
                        }
                        else if (firstClassCount > 1 && firstIdx2 >= 0)
                        {
                            shopProds[firstIdx2].MileageButton = -1;
                            shopProds[firstIdx2].AllCabinButtonText = "";
                        }
                    }
                }
            }
        }

        private void SetIsPremierCabinSaverIfApplicable(Model.Shopping.MOBSHOPShoppingProduct mobShopProduct)
        {
            if (mobShopProduct.AwardType.Trim().ToUpper().Contains("SAVER") &&
                !mobShopProduct.LongCabin.Trim().ToUpper().Contains("ECON"))
            {
                mobShopProduct.ISPremierCabinSaver = true;
            }
        }

        private void SetShoppingProductDescriptionBasedOnProductElementDescription
            (Model.Shopping.MOBSHOPShoppingProduct shopProd, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, IProductSection configurationProductSettings)
        {
            if (configurationProductSettings != null && configurationProductSettings.ProductElementCollection != null && configurationProductSettings.ProductElementCollection.Count > 0 &&
                columnInfo != null && columnInfo.Count > 0)
            {
                foreach (ProductElement productElement in configurationProductSettings.ProductElementCollection)
                {
                    if (productElement.Description.Equals(shopProd.LongCabin))
                    {
                        shopProd.Description = productElement.Header;
                        break;
                    }
                }
            }
        }

        private void SetShortCabin(Model.Shopping.MOBSHOPShoppingProduct shopProd, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, ProductSection configurationProductSettings)
        {
            foreach (ProductElement configElement in configurationProductSettings.ProductElementCollection)
            {
                if (shopProd.Type == configElement.Key)
                {
                    if (configElement.ShouldShowShortCabinName && columnInfo != null &&
                        columnInfo.First(column => column.Type == shopProd.Type) != null)
                    {
                        shopProd.Cabin = columnInfo.First(column => column.Type == shopProd.Type).LongCabin;
                    }
                    else
                    {
                        shopProd.Cabin = shopProd.LongCabin;
                    }
                }
            }
        }

        private void CalculateAwardCounts(List<Model.Shopping.MOBSHOPShoppingProduct> shopProds, ref int econAwardCount, ref int busAwardCount,
            ref int firstAwardCount)
        {
            foreach (var prod in shopProds)
            {
                if (prod.MileageButton > -1)
                {
                    if (prod.LongCabin.Trim().ToUpper().Contains("ECONOMY"))
                    {
                        econAwardCount++;
                    }
                    else if (prod.LongCabin.Trim().ToUpper().Contains("BUS"))
                    {
                        busAwardCount++;
                    }
                    else if (prod.LongCabin.Trim().ToUpper().Contains("FIRST"))
                    {
                        firstAwardCount++;
                    }
                }
            }
        }

        private void HighlightProductMatchingSelectedCabin(List<Model.Shopping.MOBSHOPShoppingProduct> shopProds, string requestedCabin, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, string fareClass, bool isReshopChange, bool isELFFareDisplayAtFSR = true)
        {
            IOrderedEnumerable<Model.Shopping.MOBSHOPShoppingProduct> productsSortedByPrice = null;
            if (isReshopChange)
                productsSortedByPrice = shopProds.Where(p => p.PriceAmount >= 0 && !string.IsNullOrEmpty(p.ProductId)).OrderBy(p => p.PriceAmount);
            else
                productsSortedByPrice = shopProds.Where(p => p.PriceAmount > 0).OrderBy(p => p.PriceAmount);

            foreach (var product in productsSortedByPrice)
            {
                var productMatchesClassRequested = MatchServiceClassRequested(requestedCabin, fareClass, product.Type, columnInfo, isELFFareDisplayAtFSR);
                if (productMatchesClassRequested)
                {
                    product.IsSelectedCabin = true;
                    break;
                }

            }
        }

        private Model.Shopping.MOBSHOPShoppingProduct TransformProductWithoutPriceToNewProduct(string cabin, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, bool isUADiscount,
            Product prod, bool foundEconomyAward, bool foundBusinessAward, bool foundFirstAward, Session session)
        {
            Model.Shopping.MOBSHOPShoppingProduct newProd = new Model.Shopping.MOBSHOPShoppingProduct();
            newProd.ProductDetail = new MOBSHOPShoppingProductDetail();

            newProd.IsUADiscount = isUADiscount;

            string cabinType = string.IsNullOrEmpty(prod.ProductType) ? "" : prod.ProductType.Trim().ToUpper();

            newProd.LongCabin = GetCabinDescriptionFromColumn(prod.ProductType, columnInfo);
            if (session.IsFSRRedesign)
            {
                if (columnInfo != null && columnInfo.Count > 0 && !string.IsNullOrEmpty(prod.ProductType))
                {
                    newProd.ColumnID = columnInfo.First(c => c != null && !string.IsNullOrEmpty(c.Type) && c.Type.ToUpper().Equals(prod.ProductType.ToUpper())).ColumnID;
                }
            }
            newProd.Description = GetDescriptionFromColumn(prod.ProductType, columnInfo);
            newProd.Type = prod.ProductType;
            newProd.Price = "Not available";
            newProd.ProductId = string.Empty;
            newProd.MilesDisplayAmount = 0;
            newProd.MilesDisplayValue = string.Empty;
            newProd.IsELF = prod.IsElf;
            newProd.AllCabinButtonText = _shoppingUtility.formatAllCabinAwardAmountForDisplay(newProd.MilesDisplayAmount.ToString(),
                newProd.LongCabin, true);


            switch (cabinType)
            {
                case "MIN-ECONOMY-SURP-OR-DISP": //award
                    {
                        newProd.MileageButton = -1;
                        if (!newProd.IsSelectedCabin && !foundEconomyAward)
                        {
                            newProd.MileageButton = GetMileageButtonIndex(cabin, newProd.LongCabin);
                        }
                    }
                    break;
                case "BUSINESS-SURPLUS": //award
                case "BUSINESS-DISPLACEMENT": //award
                    {
                        newProd.MileageButton = -1;
                        if (!newProd.IsSelectedCabin && !foundBusinessAward)
                        {
                            newProd.MileageButton = GetMileageButtonIndex(cabin, newProd.LongCabin);
                        }
                    }
                    break;
                case "FIRST-SURPLUS": //award
                case "FIRST-DISPLACEMENT": //award
                    {
                        newProd.MileageButton = -1;
                        if (!newProd.IsSelectedCabin && !foundFirstAward)
                        {
                            newProd.MileageButton = GetMileageButtonIndex(cabin, newProd.LongCabin);
                        }
                    }
                    break;
            }

            if (newProd.IsELF && string.IsNullOrEmpty(newProd.ProductCode))
            {
                newProd.ProductCode = _configuration.GetValue<string>("ELFProductCode"); ;
            }
            if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes"))
            {
                newProd.CabinDescription = prod.Description;
                newProd.BookingCode = prod.BookingCode;
            }
            return newProd;
        }

        private int GetMileageButtonIndex(string requestedCabin, string awardCabin)
        {
            int buttonIndex = -1;

            if (requestedCabin.Trim().ToUpper().Contains("ECON") && awardCabin.Trim().ToUpper().Contains("BUS"))
            {
                buttonIndex = 0;
            }
            else if (requestedCabin.Trim().ToUpper().Contains("ECON") && awardCabin.Trim().ToUpper().Contains("FIRST"))
            {
                buttonIndex = 1;
            }
            else if (requestedCabin.Trim().ToUpper().Contains("BUS") && awardCabin.Trim().ToUpper().Contains("ECONOMY"))
            {
                buttonIndex = 0;
            }
            else if (requestedCabin.Trim().ToUpper().Contains("BUS") && awardCabin.Trim().ToUpper().Contains("FIRST"))
            {
                buttonIndex = 1;
            }
            else if (requestedCabin.Trim().ToUpper().Contains("FIRST") && awardCabin.Trim().ToUpper().Contains("ECONOMY"))
            {
                buttonIndex = 0;
            }
            else if (requestedCabin.Trim().ToUpper().Contains("FIRST") && awardCabin.Trim().ToUpper().Contains("BUS"))
            {
                buttonIndex = 1;
            }

            return buttonIndex;
        }

        private string GetDescriptionFromColumn(string productType, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo)
        {
            string description = string.Empty;
            ProductSection section = _configuration.GetSection("productSettings") as ProductSection;
            if (section != null && section.ProductElementCollection != null && section.ProductElementCollection.Count > 0 && columnInfo != null && columnInfo.Count > 0)
            {
                foreach (var ci in columnInfo)
                {
                    foreach (ProductElement productElement in section.ProductElementCollection)
                    {
                        if (productElement.Key.Equals(productType) && productElement.Title.Equals(ci.LongCabin))
                        {
                            description = productElement.Header;
                            return description;
                        }
                    }
                }
            }

            return description;
        }

        private string GetCabinDescriptionFromColumn(string type, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo)
        {
            type = type.IsNullOrEmpty() ? string.Empty : type.ToUpper().Trim();

            string cabin = string.Empty;
            if (columnInfo != null && columnInfo.Count > 0)
            {
                foreach (Model.Shopping.MOBSHOPShoppingProduct prod in columnInfo)
                {
                    if (!prod.Type.IsNullOrEmpty() && type == prod.Type.ToUpper().Trim())
                    {
                        cabin = (prod.LongCabin + " " + prod.Description).Trim();
                        break;
                    }
                }
            }
            return cabin;
        }

        private string GetPriceApplyLabelText(string searchType)
        {
            String PriceFromTextTripPlanner = _configuration.GetValue<string>("PriceApplyLabelTextTripPlanner") ?? "";

            if (searchType == "OW")
            {
                return PriceFromTextTripPlanner.Split('|')[0];//One Way -- For
            }
            else if (searchType == "RT")
            {
                return PriceFromTextTripPlanner.Split('|')[1];//Roundtrip from
            }
            else if (searchType == "MD")
            {
                return PriceFromTextTripPlanner.Split('|')[2];//Multitrip from
            }
            return "";
        }

        private Model.Shopping.MOBSHOPShoppingProduct TransformProductWithPriceToNewProduct
            (string cabin, Flight segment, decimal lowestAirfare,
            List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, bool isUADiscount, Product prod, bool supressLMX, CultureInfo ci,
            string fareClass, int productIndex, ref bool foundCabinSelected, ref bool foundEconomyAward,
            ref bool foundBusinessAward, ref bool foundFirstAward, Session session, bool isELFFareDisplayAtFSR, string appVersion = "", string corporateFareIndicator = "", string yaDiscount = "")
        {
            Model.Shopping.MOBSHOPShoppingProduct newProd = new Model.Shopping.MOBSHOPShoppingProduct();
            newProd.ProductDetail = new MOBSHOPShoppingProductDetail();
            newProd.ProductDetail.ProductCabinMessages = new List<Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage>();

            newProd.IsUADiscount = isUADiscount;

            //get cabin data from column object
            newProd.LongCabin = GetCabinDescriptionFromColumn(prod.ProductType, columnInfo);
            newProd.CabinType = prod.CabinType;
            newProd.Description = GetDescriptionFromColumn(prod.ProductType, columnInfo);

            newProd.Type = prod.ProductType;
            newProd.SeatsRemaining = prod.BookingCount;

            newProd.ProductCode = prod.ProductCode;

            if (session.IsFSRRedesign)
            {
                if (columnInfo != null && columnInfo.Count > 0 && !string.IsNullOrEmpty(prod.ProductType))
                {
                    newProd.ColumnID = columnInfo.First(c => c != null && !string.IsNullOrEmpty(c.Type) && c.Type.ToUpper().Equals(prod.ProductType.ToUpper())).ColumnID;
                }
            }

            SetLmxLoyaltyInformation(premierStatusLevel, prod, supressLMX, ci, newProd);

            SetProductDetails(segment, columnInfo, prod, productIndex, newProd);

            string cabinType = string.IsNullOrEmpty(prod.ProductType) ? "" : prod.ProductType.Trim().ToUpper();
            SetMileageButtonAndAwardFound(cabin, prod, ref foundEconomyAward, ref foundBusinessAward, ref foundFirstAward, cabinType, newProd);

            SetProductPriceInformation(prod, ci, newProd, session, appVersion);
            newProd.Meal = string.IsNullOrEmpty(prod.MealDescription) ? "None" : prod.MealDescription;


            newProd.ProductId = prod.ProductId;

            newProd.IsMixedCabin = !string.IsNullOrEmpty(prod.CrossCabinMessaging);

            if (newProd.IsMixedCabin)
            {
                SetProductMixedCabinInformation(segment, prod, newProd);
                if (session.IsFSRRedesign)
                {
                    newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, MixedCabinBadge());
                }
            }

            if (session.IsFSRRedesign && _configuration.GetValue<bool>("EnableAwardFSRChanges") && newProd?.AwardType?.ToUpper() == MOBFlightProductAwardType.Saver.ToString().ToUpper())
                newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, SaverAwardBadge());
            newProd.IsELF = prod.IsElf;

            if (_configuration.GetValue<bool>("EnableAdvanceSearchCouponBooking") && session.IsFSRRedesign && !string.IsNullOrEmpty(prod.PromoDescription) && !isUADiscount) //&& prod.PromoDescription.Equals("Special offer")
            {
                newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, PromoTextBadge(prod.PromoDescription));
            }
            if (session.IsFSRRedesign)
            {
                if (isUADiscount)
                {
                    newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, UADiscountBadge());
                }
                else if (!string.IsNullOrEmpty(yaDiscount))
                {
                    newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, YADiscountBadge(yaDiscount));
                }
                else if (!string.IsNullOrEmpty(corporateFareIndicator))
                {
                    newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, CorporateFareIndicatorBadge(corporateFareIndicator));
                }
            }
            if (newProd.IsELF && string.IsNullOrEmpty(newProd.ProductCode))
            {
                newProd.ProductCode = _configuration.GetValue<string>("ELFProductCode"); ;
            }

            if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes"))
            {
                newProd.CabinDescription = prod.Description;
                newProd.BookingCode = prod.BookingCode;
            }

            return newProd;
        }

        private MOBStyledText PromoTextBadge(string promoText)
        {
            if (promoText.Contains("United PassPlus Secure&#8480;"))
            {
                return new MOBStyledText() { };
            }
            else
            {
                return new MOBStyledText()
                {
                    Text = promoText,
                    SortPriority = MOBFlightProductBadgeSortOrder.Specialoffer.ToString(),
                    TextColor = _configuration.GetValue<string>("SpecialOfferColorCode")
                };
            }
        }

        private MOBStyledText SaverAwardBadge()
        {
            return new MOBStyledText()
            {
                Text = _configuration.GetValue<string>("SaverAwardProductBadgeText"),
                SortPriority = MOBFlightProductBadgeSortOrder.SaverAward.ToString(),
                TextColor = _configuration.GetValue<string>("SaverAwardColorCode")

            };
        }
        private MOBStyledText UADiscountBadge()
        {
            return new MOBStyledText()
            {
                Text = _configuration.GetValue<string>("UADiscountProductBadgeText"),
                SortPriority = MOBFlightProductBadgeSortOrder.MixedCabin.ToString(),
                TextColor = _configuration.GetValue<string>("UADiscountColorCode")
            };
        }
        private MOBStyledText YADiscountBadge(string yaDiscount = "")
        {
            return new MOBStyledText()
            {
                Text = yaDiscount,
                SortPriority = MOBFlightProductBadgeSortOrder.YADiscounted.ToString(),
                TextColor = _configuration.GetValue<string>("YADiscountColorCode")

            };
        }
        private MOBStyledText CorporateFareIndicatorBadge(string corporateFareIndicator = "")
        {
            return new MOBStyledText()
            {
                Text = corporateFareIndicator,
                SortPriority = corporateFareIndicator == _configuration.GetValue<string>("CorporateFareIndicator") ? MOBFlightProductBadgeSortOrder.CorporateDiscounted.ToString() : MOBFlightProductBadgeSortOrder.BreakFromBusiness.ToString(),
                TextColor = corporateFareIndicator == _configuration.GetValue<string>("CorporateFareIndicator") ? _configuration.GetValue<string>("CorporateFareColorCode") : _configuration.GetValue<string>("BreakFromBusinessFareColorCode")
            };
        }

        private void SetProductPriceInformation(Product prod, CultureInfo ci, Model.Shopping.MOBSHOPShoppingProduct newProd, Session session, string appVersion = "")
        {
            var closeInFee = CalculateCloseInAwardFee(prod);
            decimal totalAmount = 0;
            var totalAmountDisplay = string.Empty;
            if (session != null && session.IsReshopChange)
            {
                if (session.IsAward)
                {
                    totalAmount = ReshopAwardAirfareDisplayValueInDecimal(prod.Prices, true);
                    if (ReshopAwardPrice(prod.Prices) == null)
                        newProd.MilesDisplayValue = "NA";
                    else
                        newProd.MilesDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(ReshopAwardPrice(prod.Prices).Amount.ToString(), true);
                    newProd.Price = "+ " + TopHelper.FormatAmountForDisplay(totalAmount, ci, false);
                    newProd.MilesDisplayAmount = totalAmount;
                    if (_configuration.GetValue<bool>("EnableAwardPricesForAllProducts"))
                    {
                        newProd.AllCabinButtonText = ReshopAwardPrice(prod.Prices).Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                       ? _shoppingUtility.formatAllCabinAwardAmountForDisplay(ReshopAwardPrice(prod.Prices).Amount.ToString(), newProd.LongCabin, true, newProd.Price)
                       : string.Empty;
                    }
                    else
                    {
                        newProd.AllCabinButtonText = ReshopAwardPrice(prod.Prices).Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                       ? _shoppingUtility.formatAllCabinAwardAmountForDisplay(ReshopAwardPrice(prod.Prices).Amount.ToString(), newProd.LongCabin, true)
                       : string.Empty;
                    }

                    newProd.PriceAmount = totalAmount;
                    newProd.ProductId = prod.ProductId;

                }
                else
                {
                    totalAmount = ReshopAirfareDisplayValueInDecimal(prod.Prices);
                    newProd.Price = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                            ? TopHelper.FormatAmountForDisplay(totalAmount + closeInFee, ci, true, true)
                            : TopHelper.FormatAmountForDisplay(totalAmount, ci);
                    newProd.MilesDisplayAmount = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES ? totalAmount : 0;
                    newProd.MilesDisplayValue = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                        ? ShopStaticUtility.FormatAwardAmountForDisplay(totalAmount.ToString(), true)
                        : string.Empty;
                    newProd.AllCabinButtonText = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                        ? _shoppingUtility.formatAllCabinAwardAmountForDisplay(totalAmount.ToString(), newProd.LongCabin, true)
                        : string.Empty;
                    newProd.PriceAmount = totalAmount;

                    if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                    {
                        if (totalAmount.CompareTo(decimal.Zero) > 0)
                        {
                            newProd.Price = string.Concat("+", newProd.Price);
                        }
                        newProd.ReshopFees = ReshopAirfareDisplayText(prod.Prices);
                    }

                    if (_shoppingUtility.EnableReShopAirfareCreditDisplay(session.AppID, appVersion))
                    {
                        newProd = ReShopAirfareCreditDisplayFSRD(ci, prod, newProd);
                    }
                }
            }
            else
            {
                newProd.Price = prod.Prices[0].Currency.Trim().ToLower() ==
                    CURRENCY_TYPE_MILES && !_configuration.GetValue<bool>("NGRPAwardCalendarMP2017NewUISwitch") ?
                    TopHelper.FormatAmountForDisplay(prod.Prices[1].Amount + closeInFee, ci, true, true) :
                    prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES &&
                    _configuration.GetValue<bool>("NGRPAwardCalendarMP2017NewUISwitch") ?
                    TopHelper.FormatAmountForDisplay(prod.Prices[1].Amount + closeInFee, ci, false, true) :
                    TopHelper.FormatAmountForDisplay(prod.Prices[0].Amount, ci);
                newProd.MilesDisplayAmount = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES ? prod.Prices[0].Amount : 0;
                newProd.MilesDisplayValue = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                    ? ShopStaticUtility.FormatAwardAmountForDisplay(prod.Prices[0].Amount.ToString(), true)
                    : string.Empty;
                if (_configuration.GetValue<bool>("EnableAwardPricesForAllProducts"))
                {
                    newProd.AllCabinButtonText = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                    ? _shoppingUtility.formatAllCabinAwardAmountForDisplay(prod.Prices[0].Amount.ToString(), newProd.LongCabin, true, newProd.Price)
                    : string.Empty;
                }
                else
                {
                    newProd.AllCabinButtonText = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                    ? _shoppingUtility.formatAllCabinAwardAmountForDisplay(prod.Prices[0].Amount.ToString(), newProd.LongCabin, true)
                    : string.Empty;
                }
                newProd.PriceAmount = prod.Prices[0].Amount;
            }
        }

        private MOBSHOPShoppingProduct ReShopAirfareCreditDisplayFSRD(CultureInfo ci, Product product, MOBSHOPShoppingProduct shoppingProduct)
        {
            var price = product.Prices;

            if (price != null && price.Any())
            {
                decimal displayprice = ReshopAirfareDisplayValueInDecimal(price);

                if (displayprice.CompareTo(decimal.Zero) == 0)
                {
                    decimal displayCredit = ReshopAirfareCreditDisplayInDecimal(price, "refundPrice");

                    if (displayCredit.CompareTo(decimal.Zero) < 0)
                    {
                        displayCredit = displayCredit * -1;
                    }

                    string strDisplayCredit
                        = TopHelper.FormatAmountForDisplay(displayCredit, ci, true);

                    shoppingProduct.ReshopCreditColor = CreditTypeColor.GREEN;

                    //displayPrice = string.Concat("+", displayPrice);
                    //AirfareDisplayValue
                    if (product.CreditType == CreditTypes.Refund)
                    {
                        shoppingProduct.ReshopFees = CreditType.REFUND.GetDisplayName();
                        shoppingProduct.IsReshopCredit = true;
                    }
                    else if (product.CreditType == CreditTypes.FlightCredit)
                    {
                        shoppingProduct.ReshopFees = CreditType.FLIGHTCREDIT.GetDisplayName();
                        shoppingProduct.IsReshopCredit = true;
                    }

                    shoppingProduct.Price = strDisplayCredit;
                }
            }
            return shoppingProduct;
        }


        private void SetProductDetails(Flight segment, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, Product prod, int productIndex,
          Model.Shopping.MOBSHOPShoppingProduct newProd)
        {
            if (productIndex >= 2 && segment.Connections != null && segment.Connections.Count > 0)
            {
                if (!string.IsNullOrEmpty(prod.ProductType) &&
                    (prod.ProductType.Contains("FIRST") || prod.ProductType.Contains("BUSINESS")) &&
                    !string.IsNullOrEmpty(prod.Description) && prod.Description.Contains("Economy"))
                {
                    newProd.LongCabin = GetCabinDescriptionFromColumn(columnInfo[productIndex].Type, columnInfo);
                    newProd.Description = GetDescriptionFromColumn(columnInfo[productIndex].Type, columnInfo);

                    ProductSection section = _configuration.GetSection("productSettings") as ProductSection;
                    if (section != null && section.ProductElementCollection != null &&
                        section.ProductElementCollection.Count > 0 && columnInfo != null && columnInfo.Count > 0)
                    {
                        foreach (var cInfo in columnInfo)
                        {
                            foreach (ProductElement productElement in section.ProductElementCollection)
                            {
                                if (productElement.Key.Equals(columnInfo[productIndex].Type) &&
                                    productElement.Title.Equals(cInfo.LongCabin) && productElement.CabinCount.Equals("0"))
                                {
                                    newProd.ProductDetail.Body = productElement.Body;
                                    newProd.ProductDetail.Title = cInfo.LongCabin + " " + cInfo.Description;
                                    newProd.ProductDetail.Header = productElement.Header;
                                    newProd.ProductDetail.ProductDetails = productElement.Details.Split('|').ToList();
                                    return;
                                }

                                if (productElement.Key.Equals(columnInfo[productIndex].Type) &&
                                    productElement.Title.Equals(cInfo.LongCabin) &&
                                    productElement.CabinCount.Equals(segment.CabinCount.ToString()))
                                {
                                    newProd.ProductDetail.Body = productElement.Body;
                                    newProd.ProductDetail.Title = cInfo.LongCabin + " " + cInfo.Description;
                                    newProd.ProductDetail.Header = productElement.Header;
                                    newProd.ProductDetail.ProductDetails = productElement.Details.Split('|').ToList();
                                    return;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(prod.AwardType))
                    {
                        ProductSection section = _configuration.GetSection("productSettings") as ProductSection;
                        if (section != null && section.ProductElementCollection != null &&
                            section.ProductElementCollection.Count > 0 && columnInfo != null && columnInfo.Count > 0)
                        {
                            foreach (var cInfo in columnInfo)
                            {
                                foreach (ProductElement productElement in section.ProductElementCollection)
                                {
                                    if (productElement.Key.Equals(prod.ProductType) && productElement.Title.Equals(cInfo.LongCabin) &&
                                        productElement.CabinCount.Equals("0"))
                                    {
                                        newProd.ProductDetail.Body = productElement.Body;
                                        newProd.ProductDetail.Title = cInfo.LongCabin + " " + cInfo.Description;
                                        newProd.ProductDetail.Header = productElement.Header;
                                        newProd.ProductDetail.ProductDetails = productElement.Details.Split('|').ToList();
                                        return;
                                    }

                                    if (productElement.Key.Equals(prod.ProductType) && productElement.Title.Equals(cInfo.LongCabin) &&
                                        productElement.CabinCount.Equals(segment.CabinCount.ToString()))
                                    {
                                        newProd.ProductDetail.Body = productElement.Body;
                                        newProd.ProductDetail.Title = cInfo.LongCabin + " " + cInfo.Description;
                                        newProd.ProductDetail.Header = productElement.Header;
                                        newProd.ProductDetail.ProductDetails = productElement.Details.Split('|').ToList();
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(prod.AwardType))
                {
                    ProductSection section = _configuration.GetSection("productSettings") as ProductSection;
                    if (section != null && section.ProductElementCollection != null &&
                        section.ProductElementCollection.Count > 0 && columnInfo != null && columnInfo.Count > 0)
                    {
                        foreach (var cInfo in columnInfo)
                        {
                            foreach (ProductElement productElement in section.ProductElementCollection)
                            {
                                if (productElement.Key.Equals(prod.ProductType) && productElement.Title.Equals(cInfo.LongCabin) &&
                                    productElement.CabinCount.Equals("0"))
                                {
                                    newProd.ProductDetail.Body = productElement.Body;
                                    newProd.ProductDetail.Title = cInfo.LongCabin + " " + cInfo.Description;
                                    newProd.ProductDetail.Header = productElement.Header;
                                    newProd.ProductDetail.ProductDetails = productElement.Details.Split('|').ToList();
                                    return;
                                }

                                if (productElement.Key.Equals(prod.ProductType) && productElement.Title.Equals(cInfo.LongCabin) &&
                                    productElement.CabinCount.Equals(segment.CabinCount.ToString()))
                                {
                                    newProd.ProductDetail.Body = productElement.Body;
                                    newProd.ProductDetail.Title = cInfo.LongCabin + " " + cInfo.Description;
                                    newProd.ProductDetail.Header = productElement.Header;
                                    newProd.ProductDetail.ProductDetails = productElement.Details.Split('|').ToList();
                                    return;
                                }

                                ///179440 : Booking FSR mApp: First lowest desciption is empty in the Compare screens in the Multi Trip booking flow
                                ///Srini - 11/27/2017
                                if (_configuration.GetValue<bool>("BugFixToggleFor17M"))
                                {
                                    if (
                                        (
                                            (productElement.Key.Equals((prod.CabinType ?? string.Empty).ToUpper()) && productElement.Title.Equals(cInfo.LongCabin)) ||
                                            ///238434 - mApp: Booking - FSR - First lowest description is empty in the Compare screens for specific markets
                                            ///Srini - 03/21/2018
                                            (_configuration.GetValue<bool>("BugFixToggleFor18C") && productElement.Key.Equals((cInfo.LongCabin ?? string.Empty).ToUpper()) && cInfo.Type.Equals(prod.ProductType))
                                        ) &&

                                        (newProd.ProductDetail.ProductDetails == null || newProd.ProductDetail.ProductDetails.Count == 0) &&
                                        (productElement.CabinCount.Equals("0") || productElement.CabinCount.Equals(segment.CabinCount.ToString())))
                                    {
                                        newProd.ProductDetail.Body = productElement.Body;
                                        newProd.ProductDetail.Title = cInfo.LongCabin + " " + cInfo.Description;
                                        newProd.ProductDetail.Header = productElement.Header;
                                        newProd.ProductDetail.ProductDetails = productElement.Details.Split('|').ToList();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private Model.Shopping.MOBStyledText MixedCabinBadge()
        {
            return new Model.Shopping.MOBStyledText()
            {
                Text = _configuration.GetValue<string>("MixedCabinProductBadgeText"),
                SortPriority = MOBFlightProductBadgeSortOrder.MixedCabin.ToString()
            };
        }

        private List<Model.Shopping.MOBStyledText> SetProductBadgeInformation(List<Model.Shopping.MOBStyledText> badges, Model.Shopping.MOBStyledText badge)
        {
            if (badges == null)
                badges = new List<Model.Shopping.MOBStyledText>();

            badges.Add(badge);

            if (badges.Count > 1)
            {
                badges = badges.OrderBy(x => (int)Enum.Parse(typeof(MOBFlightProductBadgeSortOrder), x.SortPriority)).ToList();
            }

            return badges;
        }

        private void SetProductMixedCabinInformation(Flight segment, Product prod, Model.Shopping.MOBSHOPShoppingProduct newProd)
        {
            Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage detailCabinMessage =
                new Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage();
            if (prod.CrossCabinMessaging.ToUpper().Contains("ECONOMY") ||
                prod.CrossCabinMessaging.ToUpper().Contains("COACH"))
            {
                if (newProd.LongCabin.ToUpper().Contains("BUS") || newProd.LongCabin.ToUpper().Contains("FIRST") || newProd.LongCabin.ToUpper().Contains("PREMIUM ECONOMY"))
                {
                    if (prod.Description.ToUpper().Contains("ECONOMY") || prod.Description.ToUpper().Contains("COACH"))
                    {
                        newProd.MixedCabinSegmentMessages = new List<string>();
                        newProd.MixedCabinSegmentMessages.Add(String.Format("{0}-{1} {2}", segment.Origin,
                            segment.Destination, prod.Description + " (" + prod.BookingCode + ")"));
                        newProd.IsSelectedCabin = false;
                        detailCabinMessage.IsMixedCabin = true;
                    }
                    else
                    {
                        detailCabinMessage.IsMixedCabin = false;
                    }

                    detailCabinMessage.Cabin = prod.Description + " (" + prod.BookingCode + ")";
                    detailCabinMessage.Segments = String.Format("{0} - {1}", segment.Origin, segment.Destination);
                }
            }
            else
            {
                if (newProd.LongCabin.ToUpper().Contains("ECONOMY") || newProd.LongCabin.ToUpper().Contains("COACH"))
                {
                    if (prod.Description.ToUpper().Contains("BUS") || prod.Description.ToUpper().Contains("FIRST") || newProd.LongCabin.ToUpper().Contains("PREMIUM ECONOMY"))
                    {
                        newProd.MixedCabinSegmentMessages = new List<string>();
                        newProd.MixedCabinSegmentMessages.Add(String.Format("{0}-{1} {2}", segment.Origin,
                            segment.Destination, prod.Description + " (" + prod.BookingCode + ")"));
                        newProd.IsSelectedCabin = false;
                        detailCabinMessage.IsMixedCabin = true;
                    }
                    else
                    {
                        detailCabinMessage.IsMixedCabin = false;
                    }

                    detailCabinMessage.Cabin = prod.Description + " (" + prod.BookingCode + ")";
                    detailCabinMessage.Segments = String.Format("{0} - {1}", segment.Origin, segment.Destination);
                }
            }

            newProd.ProductDetail.ProductCabinMessages.Add(detailCabinMessage);
        }

        private decimal CalculateCloseInAwardFee(Product prod)
        {
            decimal closeInFee = 0;
            if (prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES)
            {
                foreach (PricingItem p in prod.Prices)
                {
                    if (p.PricingType.Trim().ToUpper() == PRICING_TYPE_CLOSE_IN_FEE)
                    {
                        closeInFee = p.Amount;
                        break;
                    }
                }
            }
            return closeInFee;
        }

        private void SetMileageButtonAndAwardFound(string cabin, Product prod, ref bool foundEconomyAward,
            ref bool foundBusinessAward, ref bool foundFirstAward, string cabinType, Model.Shopping.MOBSHOPShoppingProduct newProd)
        {
            switch (cabinType)
            {
                case "MIN-ECONOMY-SURP-OR-DISP": //award
                    {
                        newProd.AwardType = prod.AwardType.ToLower();

                        newProd.MileageButton = -1;
                        if (!newProd.IsSelectedCabin && !foundEconomyAward)
                        {
                            newProd.MileageButton = GetMileageButtonIndex(cabin, newProd.LongCabin);
                            foundEconomyAward = true;
                        }
                    }
                    break;
                case "BUSINESS-SURPLUS": //award
                case "BUSINESS-DISPLACEMENT": //award
                    {
                        newProd.AwardType = prod.AwardType.ToLower();

                        newProd.MileageButton = -1;
                        if (!newProd.IsSelectedCabin && !foundBusinessAward)
                        {
                            newProd.MileageButton = GetMileageButtonIndex(cabin, newProd.LongCabin);
                            foundBusinessAward = true;
                        }
                    }
                    break;
                case "FIRST-SURPLUS": //award
                case "FIRST-DISPLACEMENT": //award
                    {
                        newProd.AwardType = prod.AwardType.ToLower();

                        newProd.MileageButton = -1;
                        if (!newProd.IsSelectedCabin && !foundFirstAward)
                        {
                            newProd.MileageButton = GetMileageButtonIndex(cabin, newProd.LongCabin);
                            foundFirstAward = true;
                        }
                    }
                    break;
            }
        }

        private void SetLmxLoyaltyInformation(int premierStatusLevel, Product prod, bool supressLMX, CultureInfo ci,
            Model.Shopping.MOBSHOPShoppingProduct newProd)
        {
            if (!supressLMX && prod.LmxLoyaltyTiers != null && prod.LmxLoyaltyTiers.Count > 0)
            {
                foreach (LmxLoyaltyTier tier in prod.LmxLoyaltyTiers)
                {
                    if (tier != null)
                    {
                        int tempStatus = premierStatusLevel;
                        if (premierStatusLevel > 4) //GS gets same LMX as 1K
                            tempStatus = 4;

                        if (tier.Level == tempStatus)
                        {
                            if (tier.LmxQuotes != null && tier.LmxQuotes.Count > 0)
                            {
                                foreach (LmxQuote quote in tier.LmxQuotes)
                                {
                                    switch (quote.Type.Trim().ToUpper())
                                    {
                                        case "RDM":
                                            newProd.RdmText = string.Format("{0:#,##0}", quote.Amount);
                                            break;
                                        case "PQM":
                                            newProd.PqmText = string.Format("{0:#,##0}", quote.Amount);
                                            break;
                                        case "PQD":
                                            newProd.PqdText = TopHelper.FormatAmountForDisplay(quote.Amount, ci, true, false);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private SegmentInfoAlerts TerminalChangeSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                SortOrder = SegmentInfoAlertsOrder.TerminalChange.ToString()
            };
        }

        private SegmentInfoAlerts RiskyConnectionSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                SortOrder = SegmentInfoAlertsOrder.RiskyConnection.ToString()
            };
        }

        private SegmentInfoAlerts RedEyeFlightSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                SortOrder = SegmentInfoAlertsOrder.RedEyeFlight.ToString()
            };
        }

        private SegmentInfoAlerts LonglayoverSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                SortOrder = SegmentInfoAlertsOrder.LongLayover.ToString()
            };
        }

        private SegmentInfoAlerts SubjectOfReceiptOfGovtAuthSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                SortOrder = SegmentInfoAlertsOrder.GovAuthority.ToString()
            };
        }

        private SegmentInfoAlerts AirportChangeSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Warning.ToString(),
                SortOrder = SegmentInfoAlertsOrder.AirportChange.ToString()
            };
        }

        private SegmentInfoAlerts ArrivesNextDaySegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                //ConfigurationManager.AppSettings["NextDayArrivalSegmentText"),
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                SortOrder = SegmentInfoAlertsOrder.ArrivesNextDay.ToString()
            };
        }

        private Model.Shopping.SHOPEquipmentDisclosure GetEquipmentDisclosures(United.Services.FlightShopping.Common.EquipmentDisclosure equipmentDisclosure)
        {
            Model.Shopping.SHOPEquipmentDisclosure bkEquipmentDisclosure = null;
            if (equipmentDisclosure != null)
            {
                bkEquipmentDisclosure = new Model.Shopping.SHOPEquipmentDisclosure();
                bkEquipmentDisclosure.EquipmentType = equipmentDisclosure.EquipmentType;
                bkEquipmentDisclosure.EquipmentDescription = equipmentDisclosure.EquipmentDescription;
                bkEquipmentDisclosure.IsSingleCabin = equipmentDisclosure.IsSingleCabin;
                bkEquipmentDisclosure.NoBoardingAssistance = equipmentDisclosure.NoBoardingAssistance;
                bkEquipmentDisclosure.NonJetEquipment = equipmentDisclosure.NonJetEquipment;
                bkEquipmentDisclosure.WheelchairsNotAllowed = equipmentDisclosure.WheelchairsNotAllowed;
            }
            return bkEquipmentDisclosure;
        }

        private string FormatDateTime(string dateTimeString)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(0);
            DateTime.TryParse(dateTimeString, out dateTime);
            result = string.Format("{0:MM/dd/yyyy hh:mm tt}", dateTime);
            result = result.Contains("-") ? result.Replace("-", "/") : result;

            return result;
        }

        private string FormatTime(string dateTimeString)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(0);
            DateTime.TryParse(dateTimeString, out dateTime);
            if (dateTime.Ticks != 0)
            {
                result = dateTime.ToString("h:mmtt").ToLower();
            }

            return result;
        }

        private string FormatDate(string dateTimeString)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(0);
            DateTime.TryParse(dateTimeString, out dateTime);
            if (dateTime.Ticks != 0)
            {
                if (dateTime.ToString("MMMM").Length > 3)
                {
                    result = string.Format("{0:ddd., MMM. d, yyyy}", dateTime);
                }
                else
                {
                    result = string.Format("{0:ddd., MMM d, yyyy}", dateTime);
                }
            }

            return result;
        }

        private string GetFormattedTravelTime(int minutes)
        {
            if (minutes < 60)
            {
                return string.Format("{0}m", minutes);
            }
            else
            {
                return string.Format("{0}h {1}m", minutes / 60, minutes % 60);
            }
        }

        private SegmentInfoAlerts TicketsLeftSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Warning.ToString(),
                SortOrder = SegmentInfoAlertsOrder.TicketsLeft.ToString()
            };
        }

        private string GetCabinDescription(string cos)
        {
            string cabin = string.Empty;

            if (!string.IsNullOrEmpty(cos))
            {
                switch (cos.ToLower())
                {
                    case "united economy":
                        cabin = "Coach";
                        break;
                    case "economy":
                        cabin = "Coach";
                        break;
                    case "united business":
                        cabin = "Business";
                        break;
                    case "business":
                        cabin = "Business";
                        break;
                    case "united businessfirst":
                        cabin = "BusinessFirst";
                        break;
                    case "businessfirst":
                        cabin = "BusinessFirst";
                        break;
                    case "united global first":
                        cabin = "First";
                        break;
                    case "united first":
                        cabin = "First";
                        break;
                    case "first":
                        cabin = "First";
                        break;
                }
            }
            return cabin;
        }

        private List<SegmentInfoAlerts> SetFlightInformationMessage(List<SegmentInfoAlerts> flightSegmentAlerts, SegmentInfoAlerts alert)
        {
            if (flightSegmentAlerts == null)
                flightSegmentAlerts = new List<SegmentInfoAlerts>();

            //alert.AlignLeft = flightSegmentAlerts == null || (flightSegmentAlerts.Count % 2 > 0);
            //alert.AlignRight = flightSegmentAlerts != null && flightSegmentAlerts.Count % 2 == 0;
            flightSegmentAlerts.Add(alert);

            if (flightSegmentAlerts.Count > 1)
                flightSegmentAlerts = flightSegmentAlerts.OrderBy(x => (int)Enum.Parse(typeof(SegmentInfoAlertsOrder), x.SortOrder)).ToList();

            int i = 1;
            foreach (var item in flightSegmentAlerts)
            {
                if (i % 2 > 0)
                {
                    item.AlignLeft = true;
                    item.AlignRight = false;
                }
                else
                {
                    item.AlignRight = true;
                    item.AlignLeft = false;
                }

                i++;
            }

            return flightSegmentAlerts;
        }

        private string GetMixedCabinTextForFlight(Flight flt)
        {
            //group the mixed cabin messages                
            string tempMsgs = "";
            if (flt.Products != null && flt.Products.Count > 0)
            {
                foreach (Product prod in flt.Products)
                {
                    if (!string.IsNullOrEmpty(prod.CrossCabinMessaging))
                        tempMsgs = "Mixed cabin";
                }
            }

            return tempMsgs;
        }

        private string GetCabinNameFromColumn(string type, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, string defaultCabin)
        {
            type = type.IsNullOrEmpty() ? string.Empty : type.ToUpper().Trim();

            string cabin = "Economy";
            if (columnInfo != null && columnInfo.Count > 0)
            {
                foreach (var prod in columnInfo)
                {
                    if (!prod.Type.IsNullOrEmpty() && type == prod.Type.ToUpper().Trim())
                    {
                        cabin = prod.LongCabin;
                        break;
                    }
                }
            }
            else
            {
                cabin = defaultCabin;
            }
            return cabin;
        }

        private string ReshopAirfareDisplayText(List<PricingItem> price)
        {
            bool isAddCollect = (price.Exists(p => p.PricingType == "AddCollect"))
                ? price.FirstOrDefault(p => p.PricingType == "AddCollect")?.Amount > 0 : false;

            bool isChangeFee = (price.Exists(p => p.PricingType == "ChangeFee"))
                ? price.FirstOrDefault(p => p.PricingType == "ChangeFee")?.Amount > 0 : false;

            return (isAddCollect && isChangeFee)
                ? "Price difference and change fee" : (isAddCollect) ? "Price difference"
                : (isChangeFee) ? "change fee" : string.Empty;
        }

        private PricingItem ReshopAwardPrice(List<PricingItem> price)
        {
            if (price.Exists(p => p.PricingType == "Award"))
                return price.FirstOrDefault(p => p.PricingType == "Award");

            return null;
        }

        private void GetBestProductTypeTripPlanner(Session session, Product displayProduct, bool isSelected, ref string bestProductType)
        {
            bool isTripPlannerViewEnabled = _configuration.GetValue<bool>("EnableTripPlannerView");
            if (isTripPlannerViewEnabled && (session.TravelType == MOBTripPlannerType.TPSearch.ToString() || session.TravelType == MOBTripPlannerType.TPEdit.ToString()))

            {
                if (string.IsNullOrEmpty(bestProductType) && !isSelected)
                {
                    bestProductType = displayProduct?.ProductType;
                }
            }
        }

        private string ReShopAirfareDisplayValue(CultureInfo ci, List<PricingItem> price, bool isAward = false, bool isChangeFee = false)
        {
            string displayPrice = string.Empty;
            if (price != null && price.Count > 0)
            {
                if (!isAward)
                {
                    decimal tempdisplayprice = ReshopAirfareDisplayValueInDecimal(price);

                    displayPrice = TopHelper.FormatAmountForDisplay(tempdisplayprice, ci, true);

                    if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                    {
                        if (tempdisplayprice.CompareTo(decimal.Zero) > 0)
                        {
                            displayPrice = string.Concat("+", displayPrice);
                        }
                    }
                }
                else
                    displayPrice = TopHelper.FormatAmountForDisplay(ReshopAwardAirfareDisplayValueInDecimal(price, isChangeFee), ci, false, isAward);
            }
            return string.IsNullOrEmpty(displayPrice) ? "Not available" : displayPrice;
        }

        private decimal ReshopAwardAirfareDisplayValueInDecimal(List<PricingItem> price, bool isChangeFee = false)
        {
            decimal retCloseInFee = 0;
            decimal retChangeFee = 0;
            decimal retTax = 0;

            if (price.Exists(p => p.PricingType.ToUpper() == "CLOSEINFEE") && isChangeFee)
            {
                retCloseInFee = price.First(p => p.PricingType.ToUpper() == "CLOSEINFEE").Amount;
            }
            if (price.Exists(p => p.PricingType.ToUpper() == "CHANGEFEE") && isChangeFee)
            {
                retChangeFee = price.First(p => p.PricingType.ToUpper() == "CHANGEFEE").Amount;
            }
            if (price.Exists(p => p.PricingType.ToUpper() == "SALETAXTOTAL"))
            {
                retTax = price.First(p => p.PricingType.ToUpper() == "SALETAXTOTAL").Amount;
            }

            return retCloseInFee + retChangeFee + retTax;
        }

        private decimal ReshopAirfareDisplayValueInDecimal(List<PricingItem> price)
        {
            decimal retVal = 0;
            if (price.Exists(p => p.PricingType == "AddCollect"))
                retVal = price.First(p => p.PricingType == "AddCollect").Amount;
            if (price.Exists(p => p.PricingType == "ChangeFee"))
                retVal += price.First(p => p.PricingType == "ChangeFee").Amount;

            return retVal;
        }

        private bool IsYoungAdultProduct(ProductCollection pc)
        {
            return pc != null && pc.Count > 0 && pc.Any(p => p.ProductSubtype.ToUpper().Equals("YOUNGADULTDISCOUNTEDFARE"));
        }

        private void AssignCorporateFareIndicator(Flight segment, Model.Shopping.MOBSHOPFlight flight, string travelType = "")
        {
            bool isCorporateBooking = _configuration.GetValue<bool>("CorporateConcurBooking");
            bool isCorpLeisureBooking = _configuration.GetValue<bool>("EnableCorporateLeisure") && travelType == TravelType.CLB.ToString();
            if (isCorporateBooking || isCorpLeisureBooking)
            {
                if (segment.Products.Exists(p => p.ProductSubtype != null))
                {
                    bool hasMatchCorpDisc = segment.Products.Any(p => p.ProductSubtype.Contains("CORPDISC"));
                    flight.CorporateFareIndicator = hasMatchCorpDisc ?
                                                      isCorpLeisureBooking
                                                      ? _configuration.GetValue<string>("FSRLabelForCorporateLeisure")
                                                      : _configuration.GetValue<string>("CorporateFareIndicator") ?? string.Empty
                                                   : string.Empty;
                }
            }
        }

        private bool GetAddCollectWaiverStatus(Flight flight, out string addcollectwaiver)
        {
            addcollectwaiver = string.Empty;

            if (flight.Products == null) return false;

            foreach (var product in flight.Products)
            {
                if (product.ProductId == "NAP")
                    continue;
                if (product.Prices != null)
                {
                    foreach (var price in product.Prices)
                    {
                        if (price.PricingDetails != null)
                        {
                            if (price.PricingDetails.Exists(p => p.DetailDescription.Contains("-NOAC")))
                            {
                                addcollectwaiver = product.ProductId;
                                return true;
                            }

                            if (price.PricingDetails.Exists(p => p.DetailDescription.Contains("VALID FOR SAME DAY CHANGE")))
                            {
                                var priceDetails = price.PricingDetails.First(p => p.DetailDescription.Contains("VALID FOR SAME DAY CHANGE"));
                                if (priceDetails != null)
                                {
                                    if (priceDetails.PriceType == "AddCollect" && priceDetails.PriceSubtype == "Waiver")
                                    {
                                        addcollectwaiver = product.ProductId;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private void PopulateLMX(List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights, ref List<Flight> flights)
        {
            if (lmxFlights != null && lmxFlights.Count > 0)
            {
                try
                {
                    for (int i = 0; i < flights.Count; i++)
                    {
                        Flight tempFlight = flights[i];
                        GetLMXForFlight(lmxFlights, ref tempFlight);
                        flights[i].Products = tempFlight.Products;

                        if (flights[i].Connections != null && flights[i].Connections.Count > 0)
                        {
                            List<Flight> tempFlights = flights[i].Connections;
                            PopulateLMX(lmxFlights, ref tempFlights);
                            flights[i].Connections = tempFlights;
                        }
                        if (flights[i].StopInfos != null && flights[i].StopInfos.Count > 0)
                        {
                            List<Flight> tempFlights = flights[i].StopInfos;
                            PopulateLMX(lmxFlights, ref tempFlights);
                            flights[i].StopInfos = tempFlights;
                        }
                    }
                }
                catch { }
            }
        }

        private void GetLMXForFlight(List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights, ref Flight flight)
        {
            foreach (United.Services.FlightShopping.Common.LMX.LmxFlight lmxFlight in lmxFlights)
            {
                if (flight.Hash == lmxFlight.Hash)
                {
                    //overwrite the products with new LMX versions
                    for (int i = 0; i < flight.Products.Count; i++)
                    {
                        Product tempProduct = flight.Products[i];
                        GetLMXForProduct(lmxFlight.Products, ref tempProduct);
                        flight.Products[i] = tempProduct;
                    }
                    return;
                }
            }
        }

        private void GetLMXForProduct(List<LmxProduct> productCollection, ref Product tempProduct)
        {
            foreach (LmxProduct p in productCollection)
            {
                if (p.ProductId == tempProduct.ProductId)
                {
                    tempProduct.LmxLoyaltyTiers = p.LmxLoyaltyTiers;
                }
            }
        }

        private string GetFlightHasListForLMX(List<Flight> flights)
        {
            List<string> flightNumbers = new List<string>();
            string flightHash = string.Empty;
            if (flights != null)
            {
                try
                {
                    foreach (Flight flight in flights)
                    {
                        if (!flightNumbers.Contains(flight.Hash))
                        {
                            flightNumbers.Add(flight.Hash);
                        }
                        if (flight.Connections != null && flight.Connections.Count > 0)
                        {
                            foreach (Flight connection in flight.Connections)
                            {
                                if (!flightNumbers.Contains(connection.Hash))
                                {
                                    flightNumbers.Add(connection.Hash);
                                }
                            }
                        }
                        if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                        {
                            foreach (Flight stop in flight.StopInfos)
                            {
                                if (!flightNumbers.Contains(stop.Hash))
                                {
                                    flightNumbers.Add(stop.Hash);
                                }
                            }
                        }
                    }

                    foreach (string str in flightNumbers)
                    {
                        if (flightHash == string.Empty)
                            flightHash += "\"" + str + "\"";
                        else
                            flightHash += "," + "\"" + str + "\"";
                    }
                }
                catch { }
            }
            return flightHash;
        }

        private async Task<List<United.Services.FlightShopping.Common.LMX.LmxFlight>> GetLmxFlights(string token, string cartId, string hashList, string sessionId, int applicationId, string appVersion, string deviceId)
        {
            List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights = null;

            if (!string.IsNullOrEmpty(cartId))
            {
                string jsonRequest = "{\"CartId\":\"" + cartId + "\"}";


                if (!string.IsNullOrEmpty(hashList))
                {
                    jsonRequest = "{\"CartId\":\"" + cartId + "\", \"hashList\":[" + hashList + ")}";
                }

                FlightStatus flightStatus = new FlightStatus();
                LmxQuoteResponse response = new LmxQuoteResponse();
                try
                {
                    //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetLmxFlights - Request", "Trace", applicationId, appVersion, deviceId, jsonRequest));
                    _logger.LogInformation("GetLmxFlights - Request SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Request {request}", sessionId, applicationId, appVersion, deviceId, jsonRequest);
                    response = await _lmxInfo.GetProductInfo<LmxQuoteResponse>(token, jsonRequest, sessionId);

                }
                catch (System.Exception) { }

                if (response != null)
                {

                    //logEntries.Add(LogEntry.GetLogEntry<LmxQuoteResponse>(sessionId, "GetLmxFlights - Response", "Trace", applicationId, appVersion, deviceId, lmxQuoteResponse));
                    _logger.LogInformation("GetLmxFlights - Response SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Response {response}", sessionId, applicationId, appVersion, deviceId, response);

                    if (response != null && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success))
                    {
                        if (response.Flights != null && response.Flights.Count > 0)
                        {
                            lmxFlights = response.Flights;
                        }
                    }
                }
            }

            return lmxFlights;
        }

        private void PopulateFlightAmenities(Collection<AmenitiesProfile> amenityFlights, ref List<Flight> flights)
        {
            if (amenityFlights != null && amenityFlights.Count > 0)
            {
                try
                {
                    foreach (Flight flight in flights)
                    {
                        Flight tempFlight = flight;
                        GetAmenitiesForFlight(amenityFlights, ref tempFlight);
                        flight.Amenities = tempFlight.Amenities;

                        if (flight.Connections != null && flight.Connections.Count > 0)
                        {
                            List<Flight> tempFlights = flight.Connections;
                            PopulateFlightAmenities(amenityFlights, ref tempFlights);
                            flight.Connections = tempFlights;
                        }
                        if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                        {
                            List<Flight> tempFlights = flight.StopInfos;
                            PopulateFlightAmenities(amenityFlights, ref tempFlights);
                            flight.StopInfos = tempFlights;
                        }
                    }
                }
                catch { }
            }
        }
        private void GetAmenitiesForFlight(Collection<AmenitiesProfile> amenityFlights, ref Flight flight)
        {
            foreach (var amenityFlight in amenityFlights)
            {
                if (flight.FlightNumber == amenityFlight.Key)
                {
                    //update flight amenities
                    flight.Amenities = amenityFlight.Amenities;
                    return;
                }
            }
        }

        private string GetAirportCode(string airportName)
        {
            string airportCode = string.Empty;
            if (!string.IsNullOrEmpty(airportName))
            {
                if (airportName.Length == 3)
                {
                    airportCode = airportName;
                }
                else
                {
                    int pos = airportName.IndexOf("(") + 1;
                    if (pos != -1 && pos + 4 <= airportName.Length)
                    {
                        airportCode = airportName.Substring(pos, 3);
                    }
                    else
                    {
                        airportCode = airportName;
                    }
                }
            }

            return airportCode;
        }

        private async Task<string> GetAirportNameFromSavedList(string airportCode)
        {
            if (_configuration.GetValue<bool>("EnableFecthAirportNameFromCSL"))
            {
                return string.Empty;
            }
            string airportDesc = string.Empty;
            try
            {
                if (isGetAirportListInOneCallToggleOn())
                {
                    if (airportsList != null && airportsList.AirportsList != null && airportsList.AirportsList.Exists(p => p.AirportCode == airportCode))
                    {
                        var airPort = airportsList.AirportsList.First(p => p.AirportCode == airportCode);
                        airportDesc = airPort.AirportNameMobile;
                    }
                    else
                    {
                        var airportObj = await _shoppingUtility.GetAirportNamesList("'" + airportCode + "'");
                        if (airportObj != null && airportObj.Exists(p => p.AirportCode == airportCode))
                        {
                            if (airportsList == null)
                                airportsList = new AirportDetailsList();
                            if (airportsList.AirportsList == null)
                                airportsList.AirportsList = new List<MOBDisplayBagTrackAirportDetails>();

                            var airPort = airportObj.First(p => p.AirportCode == airportCode);
                            airportsList.AirportsList.Add(airPort);   //.Add(new Definition.Bag.MOBDisplayBagTrackAirportDetails() { AirportCode = airportCode, AirportCityName = airPort.AirportCityName });
                            airportDesc = airPort.AirportNameMobile;
                        }
                        else
                        {
                            airportDesc = await _shoppingUtility.GetAirportName(airportCode);
                        }
                    }
                }
                else
                {
                    airportDesc = await _shoppingUtility.GetAirportName(airportCode);
                }
            }
            catch (Exception ex)
            {
                airportDesc = await _shoppingUtility.GetAirportName(airportCode);
            }
            return airportDesc;
        }

        private async Task<UpdateAmenitiesIndicatorsResponse> GetAmenitiesForFlights(string sessionId, string cartId, List<Flight> flights, int appId, string deviceId, string appVersion, bool isClientCall = false, UpdateAmenitiesIndicatorsRequest amenitiesPersistRequest = null)
        {
            UpdateAmenitiesIndicatorsRequest amenitiesRequest = new UpdateAmenitiesIndicatorsRequest();
            if (isClientCall)
            {
                amenitiesRequest = amenitiesPersistRequest;
            }
            else
            {
                amenitiesRequest = GetAmenitiesRequest(cartId, flights);
            }

            string jsonRequest = JsonConvert.SerializeObject(amenitiesRequest);

            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetAmenitiesForFlight - Request GetAmenitiesForFlight", "Trace", appId, appVersion, deviceId, jsonRequest));
            _logger.LogInformation("GetAmenitiesForFlight - Request for GetAmenitiesForFlight SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Request {request}", sessionId, appId, appVersion, deviceId, jsonRequest);

            string action = "UpdateAmenitiesIndicators";

            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);
            if (session == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }
            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cslStopWatch;
            cslStopWatch = new Stopwatch();
            cslStopWatch.Reset();
            cslStopWatch.Start();
            #endregion
            #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cslStopWatch.IsRunning)
            {
                cslStopWatch.Stop();
            }
            string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetAmenitiesForFlight - CSL Call Duration", "CSS/CSL-CallDuration", appId, appVersion, deviceId, "CSLUpdateAmenitiesIndicators=" + cslCallTime));

            #endregion
            //#TakeTokenFromSession
            // string token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration);
            var response = await _flightShoppingService.GetAmenitiesInfo<UpdateAmenitiesIndicatorsResponse>(session.Token, action, jsonRequest, sessionId);

            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetAmenitiesForFlight - Response for GetAmenitiesForFlight", "Trace", appId, appVersion, deviceId, jsonResponse));
            _logger.LogInformation("GetAmenitiesForFlight - Response for GetAmenitiesForFlight SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Response {response}", sessionId, appId, appVersion, deviceId, response);

            if (response != null)
            {
                //we do not want to throw an errors and stop bookings if this fails
                if (response != null && (response.Errors == null || response.Errors.Count < 1) && response.Profiles != null && response.Profiles.Count > 0)
                {
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


                        //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetAmenitiesForFlight - Response for GetAmenitiesForFlight", "Error", jsonResponse));
                        _logger.LogInformation("GetAmenitiesForFlight - Response for GetAmenitiesForFlight SessionId {sessionid}, ErrorMessage {error} and Response {response}", sessionId, errorMessage, response);
                    }
                    else
                    {

                        //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetAmenitiesForFlight - Response for GetAmenitiesForFlight", "Error", jsonResponse));
                    }
                }
            }
            else
            {
                throw new MOBUnitedException("Failed to retrieve booking details.");
            }

            return response;
        }

        private UpdateAmenitiesIndicatorsRequest GetAmenitiesRequest(string cartId, List<Flight> flights)
        {
            UpdateAmenitiesIndicatorsRequest request = new UpdateAmenitiesIndicatorsRequest();

            request.CartId = cartId;
            request.CollectionType = UpdateAmenitiesIndicatorsCollectionType.FlightNumbers;
            request.FlightNumbers = new Collection<string>();

            if (flights != null)
            {
                try
                {
                    foreach (Flight flight in flights)
                    {
                        if (!request.FlightNumbers.Contains(flight.FlightNumber))
                        {
                            request.FlightNumbers.Add(flight.FlightNumber);
                        }
                        if (flight.Connections != null && flight.Connections.Count > 0)
                        {
                            foreach (Flight connection in flight.Connections)
                            {
                                if (!request.FlightNumbers.Contains(connection.FlightNumber))
                                {
                                    request.FlightNumbers.Add(connection.FlightNumber);
                                }
                            }
                        }
                        if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                        {
                            foreach (Flight stop in flight.StopInfos)
                            {
                                if (!request.FlightNumbers.Contains(stop.FlightNumber))
                                {
                                    request.FlightNumbers.Add(stop.FlightNumber);
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            return request;
        }

        private async Task<CPCubaTravel> GetCubaTravelResons(MobileCMSContentRequest request)
        {

            request.GroupName = "General:CubaTravelCerts";
            string jsonResponse = await GETCSLCMSContent(request);

            CPCubaTravel mobCPCubaTravel = new CPCubaTravel();
            if (!string.IsNullOrEmpty(jsonResponse))
            {

                var response = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(jsonResponse);

                if (response != null && (Convert.ToBoolean(response.Status) && response.MessageItems != null))
                {

                    //logEntries.Add(LogEntry.GetLogEntry<MOBCSLContentMessagesResponse>(request.SessionId, "GetMobileCMSContents - DeSerialized Response", "DeSerialized Response", request.Application.Id, request.Application.Version.Major, request.DeviceId, response));
                    _logger.LogInformation("GetMobileCMSContents - DeSerialized Response SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Response {response}", request.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, response);

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
                        string exceptionMessage = _configuration.GetValue<string>("UnableToRegisterTravelerErrorMessage").ToString();
                        if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting").ToString()))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL RegisterTravelers(MOBRegisterTravelersRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToRegisterTravelerErrorMessage").ToString();
                if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL RegisterTravelers(MOBRegisterTravelersRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }


            //logEntries.Add(LogEntry.GetLogEntry<List<MOBMobileCMSContentMessages>>(request.SessionId, "GetMobileCMSContents - Client Response", "to client Response", request.Application.Id, request.Application.Version.Major, request.DeviceId, sortedCMSContentMessages));

            return mobCPCubaTravel;
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

        private async Task<string> GETCSLCMSContent(MobileCMSContentRequest request, bool isTravelAdvisory = false)
        {
            #region
            if (request == null)
            {
                throw new MOBUnitedException("GetMobileCMSContents request cannot be null.");
            }
            #region Get CSL Content request
            MOBCSLContentMessagesRequest cslContentReqeust = BuildCSLContentMessageRequest(request, isTravelAdvisory);
            #endregion

            string jsonResponse = await GetCSLCMSContentMesseges(request, cslContentReqeust);
            #endregion
            return jsonResponse;

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
                if (_configuration.GetValue<string>("CheckCMSContentsLocationCodes").ToUpper().Trim().Split('|').ToList().Contains(request.GroupName.ToUpper().Trim()))
                {
                    cslContentReqeust.LocationCodes = new List<string>();
                    cslContentReqeust.LocationCodes.Add(request.GroupName);
                }
                else
                {
                    cslContentReqeust.Groupname = request.GroupName;
                }
                if (_configuration.GetValue<string>("DonotUsecache4CSMContents") == null || (_configuration.GetValue<string>("DonotUsecache4CSMContents") != null && !_configuration.GetValue<bool>("DonotUsecache4CSMContents")))
                {
                    if (!istravelAdvisory)
                        cslContentReqeust.Usecache = true;
                }
            }

            return cslContentReqeust;
        }

        private async Task<string> GetCSLCMSContentMesseges(MobileCMSContentRequest request, MOBCSLContentMessagesRequest cslContentReqeust)
        {
            string jsonRequest = JsonConvert.SerializeObject(cslContentReqeust);

            //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "GetMobileCMSContents - Request", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonRequest));
            _logger.LogInformation("GetMobileCMSContents - Request SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Request {request}", request.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonRequest);
            //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "GetMobileCMSContents - Request url", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, url));

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cslStopWatch;
            cslStopWatch = new Stopwatch();
            cslStopWatch.Reset();
            cslStopWatch.Start();
            #endregion****Get Call Duration Code - Venkat 03/17/2015*******
            var response = await _iCMSContentService.GetMobileCMSContentMessages(token: request.Token, jsonRequest, _headers.ContextValues.SessionId);
            #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cslStopWatch.IsRunning)
            {
                cslStopWatch.Stop();
            }
            string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

            //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "GetMobileCMSContents - CSL Call Duration", "CSS/CSL-CallDuration", request.Application.Id, request.Application.Version.Major, request.DeviceId, "CSLGetCMSContentMessages=" + cslCallTime));

            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******   

            //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "GetMobileCMSContents - Response", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonResponse));
            _logger.LogInformation("GetMobileCMSContents - Response SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Response {response}", request.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, response);


            return response;
        }

        #region 
        private List<Model.Shopping.MOBSHOPShoppingProduct> PopulateColumns(ColumnInformation columnInfo)
        {
            List<Model.Shopping.MOBSHOPShoppingProduct> columns = null;

            //if we have columns...
            if (columnInfo != null && columnInfo.Columns != null && columnInfo.Columns.Count > 0)
            {
                columns = new List<Model.Shopping.MOBSHOPShoppingProduct>();
                foreach (Column column in columnInfo.Columns)
                {
                    Model.Shopping.MOBSHOPShoppingProduct product = new Model.Shopping.MOBSHOPShoppingProduct();
                    product.LongCabin = column.DataSourceLabel;
                    product.Description = column.Description;
                    product.Type = column.Type;
                    product.SubType = column.SubType != null ? column.SubType : string.Empty;
                    product.ColumnID = column.DescriptionId;
                    columns.Add(product);
                }
            }
            return columns;
        }

        private bool EnableAdvanceSearchCouponBooking(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableAdvanceSearchCouponBooking") && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("AndroidAdvanceSearchCouponBookingVersion"), _configuration.GetValue<string>("iPhoneAdvanceSearchCouponBookingVersion"));
        }


        private bool isAFSCouponApplied(DisplayCart displayCart)
        {
            if (displayCart != null && displayCart.SpecialPricingInfo != null && displayCart.SpecialPricingInfo.MerchOfferCoupon != null && !string.IsNullOrEmpty(displayCart.SpecialPricingInfo.MerchOfferCoupon.PromoCode) && displayCart.SpecialPricingInfo.MerchOfferCoupon.IsCouponEligible.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }
        private United.Mobile.Model.Common.MOBAlertMessages GetPartnerAirlinesSpecialTravelNeedsMessage(Session session, IEnumerable<ReservationFlightSegment> segments)
        {
            if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && session.CatalogItems != null && session.CatalogItems.Count > 0 &&
                  session.CatalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableNewPartnerAirlines).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableNewPartnerAirlines).ToString())?.CurrentValue == "1"
                 && segments?.Any(s => s.FlightSegment.OperatingAirlineCode != null) == true
                   && _configuration.GetValue<string>("SupportedAirlinesFareComparison").Contains(segments?.FirstOrDefault()?.FlightSegment?.OperatingAirlineCode.ToUpper())
                  )
            {

                United.Mobile.Model.Common.MOBAlertMessages specialNeedsAlertMessages = new United.Mobile.Model.Common.MOBAlertMessages
                {
                    HeaderMessage = _configuration.GetValue<string>("PartnerAirlinesSpecialTravelNeedsHeader"),
                    IsDefaultOption = true,
                    MessageType = MOBFSRAlertMessageType.Caution.ToString(),
                    AlertMessages = new List<MOBSection>
                        {
                            new MOBSection
                            {
                                MessageType = MOBFSRAlertMessageType.Caution.ToString(),
                                Text2 = _configuration.GetValue<string>("PartnerAirlinesSpecialTravelNeedsMessage"),
                                Order = "1"
                            }
                        }
                };
                return specialNeedsAlertMessages;
            }
            return null;
        }
        #endregion
        public async Task<MOBRegisterTravelersResponse> RepriceForAddTravelers(MOBAddTravelersRequest request, HttpContext httpContext = null)
        {
            MOBRegisterTravelersResponse addTravelerResponse = new MOBRegisterTravelersResponse();
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            Session session = await _shoppingSessionHelper.GetBookingFlowSession(request.SessionId);
            try
            {
                if (session != null)
                {
                    var (availability, isOAFlashError) = await _unfinishedBooking.GetShopPinDownDetailsV2(session, null, httpContext, request, true);
                    addTravelerResponse.Reservation = availability.Reservation;
                    addTravelerResponse.ShoppingCart = await _shoppingUtility.PopulateShoppingCart(shoppingCart, FlowType.BOOKING.ToString(), session.SessionId, availability.CartId, request, availability.Reservation, true);
                    
                }
            }
            catch (MOBUnitedException ex)
            {
                var contentMessages = await _ffcShoppingcs.GetSDLContentByGroupName(request, request.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");

                switch (ex.Message.Trim())
                {
                    case "10051":
                    case "10056":
                    case "10129":
                    case "10045":
                        addTravelerResponse.Reservation = new MOBSHOPReservation();
                        addTravelerResponse.Reservation.OnScreenAlert = AddActionsOnNoEnoughSeatsInFlight(contentMessages);
                        addTravelerResponse.Exception = new MOBException("9999", _configuration.GetValue<string>("AddTravelerNotEnoughSeatsGenericErrorMessage"));
                        break; 
                    case "10130":
                        addTravelerResponse.Reservation = new MOBSHOPReservation();
                        addTravelerResponse.Reservation.OnScreenAlert = AddActionsOnNoEnoughMilesForMoneyPlusMilesOption(contentMessages);
                        addTravelerResponse.Exception = new MOBException("9999", _configuration.GetValue<string>("AddTravelerMoneyPlusMilesLowMilesMessage"));
                        break;
                    default:
                        throw ex;
                }
            }
            return addTravelerResponse;
        }

        private MOBOnScreenAlert AddActionsOnNoEnoughMilesForMoneyPlusMilesOption(List<CMSContentMessage> contentMessages)
        {
            return new MOBOnScreenAlert()
            {
                AlertType = MOBOnScreenAlertType.OTHER,
                Title = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.NoEnoughMilesForMoneyPlusMilesOption.AlertTitle") ?? "Not enough miles",
                Message = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.MoneyPlusMilesLowMiles.AlertMessage") ?? _configuration.GetValue<string>("AddTravelerMoneyPlusMilesLowMilesMessage"),
                Actions = new List<MOBOnScreenActions>()
                {
                   new MOBOnScreenActions
                   {
                     ActionTitle = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.UpdateTravelers.UpdateTravelersText") ?? "Update travelers",
                     ActionType = MOBOnScreenAlertActionType.DISMISS_ALERT,
                   },
                   new MOBOnScreenActions
                   {
                     ActionTitle = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.SelectDifferentFlightText") ?? "Select a different flight",
                     ActionType = MOBOnScreenAlertActionType.NAVIGATE_TO_FSR,
                   }
                }
            };
        }

        private MOBOnScreenAlert AddActionsOnNoEnoughSeatsInFlight(List<CMSContentMessage> contentMessages)
        {
            return new MOBOnScreenAlert()
            {
                AlertType = MOBOnScreenAlertType.OTHER,
                Title = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.NoSeatsAvailable.AlertTitle") ?? "Not enough seats",
                Message = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.NoSeatsAvailable.AlertMessage") ?? _configuration.GetValue<string>("AddTraveler.NoSeatsAvailable.AlertMessage"),
                Actions = new List<MOBOnScreenActions>()
                {
                   new MOBOnScreenActions
                   {
                     ActionTitle = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.UpdateTravelers.UpdateTravelersText") ?? "Update travelers",
                     ActionType = MOBOnScreenAlertActionType.DISMISS_ALERT,
                   },
                   new MOBOnScreenActions
                   {
                     ActionTitle = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.SelectDifferentFlightText") ?? "Select a different flight",
                     ActionType = MOBOnScreenAlertActionType.NAVIGATE_TO_FSR,
                   }
                }
            };
        }
    }
}

