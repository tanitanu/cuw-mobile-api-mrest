using Microsoft.AspNetCore.Http;
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
using System.Threading;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.ETC;
using United.Common.Helper.FOP;
using United.Common.Helper.FSRHandler;
using United.Common.Helper.ManageRes;
using United.Common.Helper.Merchandize;
using United.Common.Helper.PageProduct;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Definition;
using United.Definition.Shopping;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.Loyalty;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.MemberSignIn;
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
using United.Utility.Helper;
using ELFRitMetaShopMessages = United.Common.Helper.Shopping.ELFRitMetaShopMessages;
//using Country = United.Mobile.Model.Common.Country;
using FlightReservationResponse = United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse;
using FlowType = United.Utility.Enum.FlowType;
using MOBFutureFlightCredit = United.Mobile.Model.Shopping.MOBFutureFlightCredit;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.ReshopSelectTrip.Domain
{
    public class ShopTripsBusiness : IShopTripsBusiness
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IUnfinishedBooking _unfinishedBooking;
        private readonly IFlightShoppingService _flightShoppingService;
        private readonly DocumentLibraryDynamoDB _documentLibraryDynamoDB;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly IDPService _dPService;
        private readonly IPNRRetrievalService _updatePNRService;
        private readonly IReferencedataService _referencedataService;
        private readonly ILMXInfo _lmxInfo;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IPKDispenserService _pKDispenserService;
        private readonly ICMSContentService _iCMSContentService;
        private readonly IMerchandizingServices _merchandizingServices;
        private readonly ICustomerDataService _customerDataService;
        private readonly IMPSecurityQuestionsService _mPSecurityQuestionsService;
        private readonly IDataVaultService _dataVaultService;
        private readonly IUtilitiesService _utilitiesService;
        private readonly ILoyaltyUCBService _loyaltyBalanceService;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private AirportDetailsList airportsList;
        private ELFRitMetaShopMessages _eLFRitMetaShopMessages;
        private readonly ITravelerCSL _travelerCSL;
        public static string CURRENCY_TYPE_MILES = "miles";
        public static string PRICING_TYPE_CLOSE_IN_FEE = "CLOSEINFEE";
        private readonly IOmniCart _omniCart;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly IPaymentService _paymentService;
        private readonly IProductInfoHelper _productInfoHelper;
        private readonly IFormsOfPayment _formsOfPayment;
        private readonly ManageResUtility _manageResUtility;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly ITravelerUtility _travelerUtility;
        private readonly IShoppingBuyMiles _shoppingBuyMiles;



        public ShopTripsBusiness(ICacheLog<ShopTripsBusiness> logger
            , IConfiguration configuration
            , IHeaders headers
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility
            , IUnfinishedBooking unfinishedBooking
            , IFlightShoppingService flightShoppingService
            , IDynamoDBService dynamoDBService
            , IDPService dPService
            , IPNRRetrievalService updatePNRService
            , IReferencedataService referencedataService
            , ILMXInfo lmxInfo
            , IShoppingCartService shoppingCartService
            , IPKDispenserService pKDispenserService
            , ICMSContentService iCMSContentService
            , IMerchandizingServices merchandizingServices
            , ICustomerDataService customerDataService
            , IMPSecurityQuestionsService mPSecurityQuestionsService
            , IDataVaultService dataVaultService
            , IUtilitiesService utilitiesService
            , ILoyaltyUCBService loyaltyBalanceService
            , IDataPowerFactory dataPowerFactory
            , IHttpContextAccessor httpContextAccessor
            , IShoppingSessionHelper shoppingSessionHelper
            , IOmniCart omniCart
            , IFFCShoppingcs fFCShoppingcs
            , IPaymentService paymentService
            , IProductInfoHelper productInfoHelper
            , IFormsOfPayment formsOfPayment
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , ITravelerUtility travelerUtility
            , IShoppingBuyMiles shoppingBuyMiles)
        {
            _logger = logger;
            _configuration = configuration;
            _headers = headers;
            _shoppingUtility = shoppingUtility;
            _sessionHelperService = sessionHelperService;
            _unfinishedBooking = unfinishedBooking;
            _flightShoppingService = flightShoppingService;
            _dynamoDBService = dynamoDBService;
            _dPService = dPService;
            _loyaltyBalanceService = loyaltyBalanceService;
            _documentLibraryDynamoDB = new DocumentLibraryDynamoDB(_configuration, _dynamoDBService);
            _updatePNRService = updatePNRService;
            _referencedataService = referencedataService;
            _lmxInfo = lmxInfo;
            _shoppingCartService = shoppingCartService;
            _iCMSContentService = iCMSContentService;
            _merchandizingServices = merchandizingServices;
            _pKDispenserService = pKDispenserService;
            _customerDataService = customerDataService;
            _mPSecurityQuestionsService = mPSecurityQuestionsService;
            _dataVaultService = dataVaultService;
            _utilitiesService = utilitiesService;
            airportsList = null;
            _shoppingSessionHelper = shoppingSessionHelper;
            _travelerCSL = new TravelerCSL(_configuration, _sessionHelperService, _headers, _iCMSContentService);
            _omniCart = omniCart;
            _fFCShoppingcs = fFCShoppingcs;
            _paymentService = paymentService;
            _productInfoHelper = productInfoHelper;
            _formsOfPayment = formsOfPayment;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _eLFRitMetaShopMessages = new ELFRitMetaShopMessages(_configuration, _dynamoDBService,_legalDocumentsForTitlesService);
            _manageResUtility = new ManageResUtility(_configuration, _legalDocumentsForTitlesService, _dynamoDBService);
            _travelerUtility = travelerUtility;
            _shoppingBuyMiles = shoppingBuyMiles;
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

        public bool IsRequireNatAndCR(MOBSHOPReservation reservation, MOBApplication application, string sessionID, string deviceID, string token)
        {
            bool isRequireNatAndCR = false;
            List<string> NationalityResidenceCountriesList = new List<string>();

            #region Load list of countries from cache/persist
            //List<Country> lst =_sessionHelperService.GetSession<List<Country>>(_configuration.GetValue<string>("NationalityResidenceCountriesListStaticGUID").ToString(), "Booking2.0NationalityResidenceCountries");
            List<MOBSHOPCountry> lst = _sessionHelperService.GetSession<List<MOBSHOPCountry>>(Headers.ContextValues).Result;
            #endregion Load list of countries from cache/persist

            if (lst == null)
            {
                lst = GetNationalityResidenceCountries(application.Id, deviceID, application.Version.Major, null, sessionID, token);
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

        private MOBSHOPAvailability GetMetaShopBookingDetailsV2(Session session, MetaSelectTripRequest request, ref List<ReservationFlightSegment> segments, bool isUpgradedFromElf = false)
        {
            var availability = new MOBSHOPAvailability();
            United.Mobile.Model.Shopping.MOBSHOPReservation reservation;

            string action = "ShopBookingDetailsV2";
            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetMetaShopBookingDetailsV2", "Url", request.Application.Id, request.Application.Version.Major, request.DeviceId, url));


            var shopSelectRequest = BuildShopSelectRequest(request);
            shopSelectRequest.DeviceId = request.DeviceId;
            var jsonRequest = JsonConvert.SerializeObject(shopSelectRequest);
            _sessionHelperService.SaveSession<string>(jsonRequest, Headers.ContextValues.SessionId, new List<string>() { Headers.ContextValues.SessionId, typeof(Services.FlightShopping.Common.ShopSelectRequest).FullName }, typeof(Services.FlightShopping.Common.ShopSelectRequest).FullName); //, typeof(ShopSelectRequest).FullName);

            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetMetaShopBookingDetailsV2", "Request", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonRequest));

            string token = _dPService.GetAnonymousToken(Headers.ContextValues.Application.Id, Headers.ContextValues.DeviceId, _configuration).Result;
            var bookingresponse = _flightShoppingService.GetBookingDetailsV2<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(token, action, jsonRequest, session.SessionId).Result;

            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetMetaShopBookingDetailsV2", "Response", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonResponse));


            if (bookingresponse != null)
            {
                if (bookingresponse != null && bookingresponse.Status.Equals(StatusType.Success) && bookingresponse.Reservation != null)
                {
                    var response = new United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse();
                    response = RegisterFlights(bookingresponse, session, request);
                    AssignMissingPropertiesfromRegisterFlightsResponse(bookingresponse, response);

                    if (!(_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, session.IsReshopChange) && request.IsCatalogOnForTavelerTypes))
                    {
                        ThrowMessageIfAnyPaxTypeIsNotADT(response.DisplayCart);
                    }
                    session.CartId = response.CartId;
                    session.IsMeta = true;
                    _sessionHelperService.SaveSession(session, Headers.ContextValues, session.ObjectName);

                    if (response.DisplayCart.IsElf && PricesAreInverted(response))
                        return AutoUpsellElfV2(session, request, ref segments, response);

                    availability.Upsells = GetMetaShopUpsellsAndRequestedProduct(response);

                    reservation = new United.Mobile.Model.Shopping.MOBSHOPReservation();

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
                        reservation.ISInternational = new IsInternationalFlagMapper().IsInternationalFromResponse(response);
                        reservation.IsMetaSearch = true;
                        _eLFRitMetaShopMessages.AddElfRtiMetaSearchMessages(reservation);
                        AddElfUpgradeMessageForMetasearch(isUpgradedFromElf, reservation);
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
                        var fareClass = GetFareClassAtShoppingRequestFromPersist(session.SessionId);
                        var flightDepartDatesForSelectedTrip = new List<string>();
                        reservation.Trips = PopulateMetaTrips(_mOBSHOPDataCarrier, response.CartId, null, session.SessionId, request.Application.Id, request.DeviceId, request.Application.Version.Major, true, response.DisplayCart.DisplayTrips, fareClass, flightDepartDatesForSelectedTrip);
                        if (reservation.Trips != null && reservation.Trips.Count > 0)
                        {
                            flightDepartDatesForSelectedTrip.AddRange(reservation.Trips.Select(shopTrip => shopTrip.TripId + "|" + shopTrip.DepartDate));
                        }
                        if (_shoppingUtility.IsEnableTaxForAgeDiversification(session.IsReshopChange, request.Application.Id, request.Application.Version.Major))
                        {
                            reservation.Prices = _shoppingUtility.GetPrices(response.DisplayCart.DisplayPrices, false, session.SessionId, searchType: reservation.SearchType.ToString());

                        }
                        else
                        {
                            reservation.Prices = GetPrices(response.DisplayCart.DisplayPrices, false, session.SessionId);
                        }
                        if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                        {
                            reservation.Prices.AddRange(GetPrices(response.DisplayCart.DisplayFees, false, string.Empty));
                        }
                        reservation.ELFMessagesForRTI = _eLFRitMetaShopMessages.GetELFShopMessagesForRestrictions(reservation);
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
                        reservation.TravelOptions = GetTravelOptions(response.DisplayCart);

                        if (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, session.IsReshopChange) && request.IsCatalogOnForTavelerTypes)
                        {
                            string firstLOFDepDate = response.DisplayCart.DisplayTrips[0].Flights[0].DepartDateTime;

                            response.DisplayCart.DisplayTravelers.Sort((x, y) =>
                          ShopStaticUtility.GetTTypeValue(TopHelper.GetAgeByDOB(x.DateOfBirth, firstLOFDepDate), x.PaxTypeCode.ToUpper().Equals("INF")).CompareTo(ShopStaticUtility.GetTTypeValue(TopHelper.GetAgeByDOB(y.DateOfBirth, firstLOFDepDate), y.PaxTypeCode.ToUpper().Equals("INF")))
                         );
                            reservation.ShopReservationInfo2.TravelerTypes = ShopStaticUtility.GetTravelTypesFromShop(response.DisplayCart.DisplayTravelers);
                            reservation.ShopReservationInfo2.displayTravelTypes = ShopStaticUtility.GetDisplayTravelerTypes(response.DisplayCart.DisplayTravelers);
                        }
                        if (_shoppingUtility.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major,true) && !session.IsReshopChange)
                        {
                            if (reservation.ShopReservationInfo2 == null)
                                reservation.ShopReservationInfo2 = new ReservationInfo2();
                            reservation.ShopReservationInfo2.IsDisplayCart = _shoppingUtility.IsDisplayCart(session, "DisplayCartTravelTypes");
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
                            reservation.CubaTravelInfo = _travelerCSL.GetCubaTravelResons(mobMobileCMSContentRequest);
                            reservation.CubaTravelInfo.CubaTravelTitles = new MPDynamoDB(_configuration, _dynamoDBService,null).GetMPPINPWDTitleMessages(new List<string> { "CUBA_TRAVEL_CONTENT" });
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
                        //var pkDispenserPublicKey = _sessionHelperService.GetSession<string>(_shoppingUtility.GetCSSPublicKeyPersistSessionStaticGUID(Headers.ContextValues.Application.Id)).Result;
                        //if (!string.IsNullOrEmpty(pkDispenserPublicKey))
                        //{
                        //    reservation.PKDispenserPublicKey = pkDispenserPublicKey;
                        //}
                        //else
                        //{
                        //    //reservation.PKDispenserPublicKey = Authentication.GetPkDispenserPublicKey(request.Application.Id, request.DeviceId, request.Application.Version.Major, session.SessionId, logEntries, traceSwitch, session.Token);
                        //}
                        try
                        {
                            segments = CommonMethods.FlattenSegmentsForSeatDisplay(response.Reservation);
                        }
                        catch
                        {

                        }

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
                        _sessionHelperService.SaveSession(persistedShopBookingDetailsResponse, Headers.ContextValues, persistedShopBookingDetailsResponse.ObjectName);

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
                        _sessionHelperService.SaveSession(shop, Headers.ContextValues, shop.ObjectName);
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

                    throw new UnitedException("Failed to retrieve booking details.");
                }
            }
            else
            {
                throw new UnitedException("Failed to retrieve booking details.");
            }
            availability.Reservation = reservation;
            return availability;
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

                    if (isCubaFight)
                        break;

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

                            Parallel.Invoke(() =>
                            {
                                bool supressLMX = false;
                                #region //**// LMX Flag For AppID change
                                Session session = new Session();
                                session = _sessionHelperService.GetSession<Session>(Headers.ContextValues, session.ObjectName).Result;
                                supressLMX = session.SupressLMXForAppID;
                                #endregion
                                if (!supressLMX && showMileageDetails)
                                {
                                    lmxFlights = GetLmxForRTI(session.Token, cartId, GetFlightHasListForLMX(tempFlights), sessionId, appId, deviceId, appVersion);
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

        private List<United.Mobile.Model.Common.MP2015.LmxFlight> GetLmxForRTI(string token, string cartId, string hashList, string sessionId, int appId, string deviceId, string appVersion)
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

                var response = _lmxInfo.GetLmxRTIInfo<LmxQuoteResponse>(token, jsonRequest, sessionId).Result;

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

        private FareLock GetFareLockOptions(United.Service.Presentation.ProductResponseModel.ProductOffer cslFareLock, double flightPrice, string currency,
            bool isAward, double miles, int appId, string appVersion, List<MOBItem> catalogItems = null)
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
                                    flProd.FareLockProductAmountDisplayText = TopHelper.FormatAmountForDisplay(flProd.FareLockProductAmount.ToString(), ci, false);
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
                // flProd.ProductCode , flProd.ProductId ,flProd.FareLockProductAmount should not be passed anytime it will break UI
                shopFareLock.FareLockProducts.Insert(0, flProd);

            }
            return shopFareLock;
        }

        private List<Model.Shopping.TravelOption> GetTravelOptions(DisplayCart displayCart)
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
                    if (!anOption.Type.Equals("Premium Access") && !anOption.Key.Trim().ToUpper().Contains("FARELOCK") && !(addTripInsuranceInTravelOption && anOption.Key.Trim().ToUpper().Contains("TPI")))
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
                    if (!string.IsNullOrEmpty(anOption.Type))
                    {
                        travelOption.Type = anOption.Type.Equals("Premium Access") ? "Premier Access" : anOption.Type;
                    }
                    travelOptions.Add(travelOption);
                }
            }

            return travelOptions;
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
            bool isTravelerPriceDirty = false;
            string reshopTaxCodeDescription = string.Empty;
            bool isEnableOmniCartMVP2Changes = _configuration.GetValue<bool>("EnableOmniCartMVP2Changes");

            foreach (var price in prices)
            {
                if (price.SubItems != null && price.SubItems.Count > 0 &&
                    (!isReshopChange || (isReshopChange && price.Type.ToUpper() == "TRAVELERPRICE" && !isTravelerPriceDirty))
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
                        isTravelerPriceDirty = true;
                        taxsAndFees.Add(taxNfee);

                        taxTotal += taxNfee.Amount;
                    }

                    reshopTaxCodeDescription = price.Description;
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
                tnf.TaxCodeDescription = string.Format("{0} {1}: {2}{3}", numPax,
                    (!string.IsNullOrEmpty(reshopTaxCodeDescription)) ? reshopTaxCodeDescription : (numPax > 1) ? "travelers" : "traveler", tnf.DisplayAmount
                    , isEnableOmniCartMVP2Changes ? "/person" : " per person");
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

        private string GetFareClassAtShoppingRequestFromPersist(string sessionID)
        {
            #region
            string fareClass = string.Empty;
            try
            {
                ShoppingResponse shop = new ShoppingResponse();
                shop = _sessionHelperService.GetSession<ShoppingResponse>(Headers.ContextValues, shop.ObjectName).Result;
                fareClass = !string.IsNullOrEmpty(shop.Request.FareClass) ? shop.Request.FareClass : string.Empty;
            }
            catch { }
            return fareClass;
            #endregion
        }

        private void AddElfUpgradeMessageForMetasearch(bool isUpgradedFromElf, United.Mobile.Model.Shopping.MOBSHOPReservation reservation)
        {
            if (!isUpgradedFromElf) return;

            var messagesFromDb = new MPDynamoDB(_configuration, _dynamoDBService,null).GetMPPINPWDTitleMessages(new List<string> { "ELF_METASEARCH_UPGRADE_MESSAGES" });
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

        private UpsellProduct GetRequestedProduct(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse response)
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

        private List<UpsellProduct> GetMetaShopUpsellsAndRequestedProduct(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse response)
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

        private UpsellProduct GetUpsellProduct(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse response)
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

        private MOBSHOPAvailability AutoUpsellElfV2(Session session, MetaSelectTripRequest request, ref List<ReservationFlightSegment> segments, United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse response)
        {
            var newRequest = BuildNewUpsellRequest(response, request);
            return GetMetaShopBookingDetailsV2(session, newRequest, ref segments, true);
        }

        private MetaSelectTripRequest BuildNewUpsellRequest(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse response, MetaSelectTripRequest oldRequest)
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

        private bool PricesAreInverted(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse response)
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

        private void AssignMissingPropertiesfromRegisterFlightsResponse(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReserationResponse, United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse registerFlightsResponse)
        {
            if (!registerFlightsResponse.IsNullOrEmpty())
            {
                registerFlightsResponse.FareLockResponse = flightReserationResponse?.FareLockResponse;
                registerFlightsResponse.Upsells = flightReserationResponse?.Upsells;
                registerFlightsResponse.LastBBXSolutionSetId = flightReserationResponse?.LastBBXSolutionSetId;
            }
        }

        private United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse RegisterFlights(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, Session session, MOBRequest request)
        {
            string flow = session.IsNullOrEmpty() && session.IsReshopChange ? FlowType.RESHOP.ToString() : FlowType.BOOKING.ToString();
            var registerFlightRequest = BuildRegisterFlightRequest(flightReservationResponse, flow, request);
            string jsonRequest = JsonConvert.SerializeObject(registerFlightRequest);
            United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightresponse = new United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse();

            //    logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "RegisterFlights", "CSL-Request", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonRequest));

            string actionName = "RegisterFlights";

            //    logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "RegisterFlights", "CSL-Url", request.Application.Id, request.Application.Version.Major, request.DeviceId, url));


            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cSLCallDurationstopwatch1;
            cSLCallDurationstopwatch1 = new Stopwatch();
            cSLCallDurationstopwatch1.Reset();
            cSLCallDurationstopwatch1.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            string token = string.IsNullOrEmpty(session.Token
                ) ? _dPService.GetAnonymousToken(Headers.ContextValues.Application.Id, Headers.ContextValues.DeviceId, _configuration).Result : session.Token;

            var response = _shoppingCartService.GetShoppingCartInfo<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(token, actionName, jsonRequest, session.SessionId).Result;

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cSLCallDurationstopwatch1.IsRunning)
            {
                cSLCallDurationstopwatch1.Stop();
            }
            #endregion/***Get Call Duration Code - Venkat 03/17/2015*******

            //    logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "RegisterFlights", "CSS/CSL-CallDuration", request.Application.Id, request.Application.Version.Major, request.DeviceId, "CSLRegisterFlights=" + (cSLCallDurationstopwatch1.ElapsedMilliseconds / (double)1000).ToString()));
            //    logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "RegisterFlights ", "CSL-Response", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonResponse));


            if (response != null)
            {

                //logEntries.Add(LogEntry.GetLogEntry<FlightReservationResponse>(session.SessionId, "RegisterFlights", "CSL Deserialized Json Response", request.Application.Id, request.Application.Version.Major, request.DeviceId, response, true, false));

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
                if (_shoppingUtility.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major,true) && flow == FlowType.BOOKING.ToString())
                {
                    var persistShoppingCart = new MOBShoppingCart();
                    persistShoppingCart = _sessionHelperService.GetSession<MOBShoppingCart>(Headers.ContextValues).Result;//, persistShoppingCart.GetType().ToString(), false);
                    if (persistShoppingCart == null)
                    {
                        persistShoppingCart = new MOBShoppingCart();
                    }
                    persistShoppingCart.Products = _productInfoHelper.ConfirmationPageProductInfo(response, false, request.Application, null, flow);
                    double price = _shoppingUtility.GetGrandTotalPriceForShoppingCart(false, response, false, flow);
                    persistShoppingCart.TotalPrice = String.Format("{0:0.00}", price);
                    persistShoppingCart.DisplayTotalPrice = Decimal.Parse(price.ToString()).ToString("c");
                    //OmniCart omniCart = new OmniCart(_configuration, null, _sessionHelperService, _shoppingUtility);
                    persistShoppingCart.OmniCart = _omniCart.BuildOmniCart(persistShoppingCart, flightReservationResponse, flow);
                    _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, Headers.ContextValues, persistShoppingCart.GetType().ToString());

                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            return response;

        }

        private MOBShoppingCart PopulateShoppingCart(MOBShoppingCart shoppingCart, string flow, string sessionId, string CartId, MOBRequest request = null, Mobile.Model.Shopping.MOBSHOPReservation reservation = null)
        {
            shoppingCart = _sessionHelperService.GetSession<MOBShoppingCart>(Headers.ContextValues).Result; //shoppingCart.GetType().ToString());
            if (shoppingCart == null)
                shoppingCart = new MOBShoppingCart();
            shoppingCart.CartId = CartId;
            shoppingCart.Flow = flow;
            if (request?.Application != null && _shoppingUtility.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major,true))
            {
                _omniCart.BuildCartTotalPrice(shoppingCart, reservation);
            }
            _sessionHelperService.GetSession<MOBShoppingCart>(Headers.ContextValues);
            return shoppingCart;
        }

        private RegisterFlightsRequest BuildRegisterFlightRequest(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, string flow, MOBRequest mobRequest)
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

        private MOBSHOPAvailability GetMetaShopBookingDetails(Session session, MetaSelectTripRequest request, ref List<ReservationFlightSegment> segments, bool isUpgradedFromElf = false)
        {
            var availability = new MOBSHOPAvailability();
            MOBSHOPReservation reservation;

            var url = string.Format("{0}/ShopBookingDetails", _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLShopping"));

            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetMetaShopBookingDetails - Request url for ShopBookingDetails", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, url));


            var shopSelectRequest = BuildShopSelectRequest(request);
            var jsonRequest = JsonConvert.SerializeObject(shopSelectRequest);
            //_sessionHelperService.SaveSession(jsonRequest,Headers.ContextValues);

            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetMetaShopBookingDetails - Request for ShopBookingDetails", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonRequest));
            string token = string.IsNullOrEmpty(session.Token) ? _dPService.GetAnonymousToken(Headers.ContextValues.Application.Id, Headers.ContextValues.DeviceId, _configuration).Result : session.Token;
            string actionName = "ShopBookingDetails";
            var response = _flightShoppingService.GetMetaBookingDetails<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(token, actionName, jsonRequest, session.SessionId).Result;

            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetMetaShopBookingDetails - Response for ShopBookingDetails", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonResponse));

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
                    _sessionHelperService.SaveSession(session, Headers.ContextValues, session.ObjectName);

                    if (response.DisplayCart.IsElf && PricesAreInverted(response))
                        return AutoUpsellElf(session, request, ref segments, response);

                    availability.Upsells = GetMetaShopUpsellsAndRequestedProduct(response);

                    reservation = new MOBSHOPReservation();

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
                        reservation.ELFMessagesForRTI = _eLFRitMetaShopMessages.GetELFShopMessagesForRestrictions(reservation);
                        reservation.ISInternational = new IsInternationalFlagMapper().IsInternationalFromResponse(response);
                        reservation.IsMetaSearch = true;
                        _eLFRitMetaShopMessages.AddElfRtiMetaSearchMessages(reservation);
                        AddElfUpgradeMessageForMetasearch(isUpgradedFromElf, reservation);

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
                        var fareClass = GetFareClassAtShoppingRequestFromPersist(session.SessionId);
                        var flightDepartDatesForSelectedTrip = new List<string>();
                        reservation.Trips = PopulateMetaTrips(_mOBSHOPDataCarrier, response.CartId, null, session.SessionId, request.Application.Id, request.DeviceId, request.Application.Version.Major, true, response.DisplayCart.DisplayTrips, fareClass, flightDepartDatesForSelectedTrip);
                        if (reservation.Trips != null && reservation.Trips.Count > 0)
                        {
                            flightDepartDatesForSelectedTrip.AddRange(reservation.Trips.Select(shopTrip => shopTrip.TripId + "|" + shopTrip.DepartDate));
                        }
                        if (_shoppingUtility.IsEnableTaxForAgeDiversification(session.IsReshopChange, request.Application.Id, request.Application.Version.Major))
                        {
                            reservation.Prices = _shoppingUtility.GetPrices(response.DisplayCart.DisplayPrices, false, session.SessionId, searchType: reservation.SearchType.ToString());

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
                        reservation.TravelOptions = GetTravelOptions(response.DisplayCart);

                        if (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, session.IsReshopChange) && request.IsCatalogOnForTavelerTypes)
                        {
                            string firstLOFDepDate = response.DisplayCart.DisplayTrips[0].Flights[0].DepartDateTime;

                            response.DisplayCart.DisplayTravelers.Sort((x, y) =>
                          ShopStaticUtility.GetTTypeValue(TopHelper.GetAgeByDOB(x.DateOfBirth, firstLOFDepDate), x.PaxTypeCode.ToUpper().Equals("INF")).CompareTo(ShopStaticUtility.GetTTypeValue(TopHelper.GetAgeByDOB(y.DateOfBirth, firstLOFDepDate), y.PaxTypeCode.ToUpper().Equals("INF")))
                         );
                            reservation.ShopReservationInfo2.TravelerTypes = ShopStaticUtility.GetTravelTypesFromShop(response.DisplayCart.DisplayTravelers);
                            reservation.ShopReservationInfo2.displayTravelTypes = ShopStaticUtility.GetDisplayTravelerTypes(response.DisplayCart.DisplayTravelers);
                        }

                        if (_shoppingUtility.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major,true) && !session.IsReshopChange)
                        {
                            if (reservation.ShopReservationInfo2 == null)
                                reservation.ShopReservationInfo2 = new ReservationInfo2();
                            reservation.ShopReservationInfo2.IsDisplayCart = _shoppingUtility.IsDisplayCart(session, "DisplayCartTravelTypes");
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
                            availability.Reservation.CubaTravelInfo = GetCubaTravelResons(mobMobileCMSContentRequest);
                            //Profile profile = new CSL.Profile();
                            availability.Reservation.CubaTravelInfo.CubaTravelTitles = GetMPPINPWDTitleMessages(new List<string> { "CUBA_TRAVEL_CONTENT" });
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
                        _sessionHelperService.SaveSession(persistedShopBookingDetailsResponse, Headers.ContextValues);

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
                        _sessionHelperService.SaveSession(shop, Headers.ContextValues, shop.ObjectName);
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

                    throw new UnitedException("Failed to retrieve booking details.");
                }
            }
            else
            {
                throw new UnitedException("Failed to retrieve booking details.");
            }
            availability.Reservation = reservation;
            return availability;
        }

        private MOBSHOPAvailability AutoUpsellElf(Session session, MetaSelectTripRequest request, ref List<ReservationFlightSegment> segments, United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse response)
        {
            var newRequest = BuildNewUpsellRequest(response, request);
            return GetMetaShopBookingDetails(session, newRequest, ref segments, true);
        }

        private TravelSpecialNeeds GetItineraryAvailableSpecialNeeds(Session session, int appId, string appVersion, string deviceId, IEnumerable<ReservationFlightSegment> segments, string languageCode)
        {
            MultiCallResponse flightshoppingReferenceData = null;
            IEnumerable<ReservationFlightSegment> pnrOfferedMeals = null;
            var offersSSR = new TravelSpecialNeeds();

            try
            {
                Parallel.Invoke(() => flightshoppingReferenceData = GetSpecialNeedsReferenceDataFromFlightShopping(session, appId, appVersion, deviceId, languageCode),
                                () => pnrOfferedMeals = GetOfferedMealsForItineraryFromPNRManagement(session, appId, appVersion, deviceId, segments));
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

            Parallel.Invoke(() => offersSSR.SpecialMeals = GetOfferedMealsForItinerary(pnrOfferedMeals, flightshoppingReferenceData),
                            () => offersSSR.SpecialMealsMessages = GetSpecialMealsMessages(pnrOfferedMeals, flightshoppingReferenceData),
                            () => offersSSR.SpecialRequests = GetOfferedSpecialRequests(flightshoppingReferenceData),
                            () => offersSSR.SpecialMealsMessages = GetSpecialMealsMessages(pnrOfferedMeals, flightshoppingReferenceData),
                            () => offersSSR.ServiceAnimals = GetOfferedServiceAnimals(flightshoppingReferenceData, segments, appId, appVersion));

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

            AddServiceAnimalsMessageSection(offersSSR, appId, appVersion, session, deviceId);

            if (offersSSR.ServiceAnimalsMessages == null || !offersSSR.ServiceAnimalsMessages.Any())
                offersSSR.ServiceAnimalsMessages = GetServiceAnimalsMessages(offersSSR.ServiceAnimals);

            return offersSSR;
        }

        private MultiCallResponse GetSpecialNeedsReferenceDataFromFlightShopping(Session session, int appId, string appVersion, string deviceId, string languageCode)
        {
            string cslActionName = "MultiCall";

            //    logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetSpecialNeedsReferenceDataFromFlightShopping - Request url for " + cslActionName, "Trace", appId, appVersion, deviceId, url));

            string jsonRequest = JsonConvert.SerializeObject(GetFlightShoppingMulticallRequest(languageCode));

            string token = _dPService.GetAnonymousToken(Headers.ContextValues.Application.Id, Headers.ContextValues.DeviceId, _configuration).Result;


            //    logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetSpecialNeedsReferenceDataFromFlightShopping - Request for " + cslActionName, "Trace", appId, appVersion, deviceId, jsonRequest));


            var cslCallDurationstopwatch = new Stopwatch();
            cslCallDurationstopwatch.Start();

            var response = _referencedataService.GetSpecialNeedsInfo<MultiCallResponse>(cslActionName, jsonRequest, token, Headers.ContextValues.SessionId).Result;

            if (cslCallDurationstopwatch.IsRunning)
            {
                cslCallDurationstopwatch.Stop();
            }


            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "MultiCall - CSL Call Duration", "CSS/CSL-CallDuration", appId, appVersion, deviceId, "CSLMultiCall=" + (cslCallDurationstopwatch.ElapsedMilliseconds / (double)1000).ToString()));
            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetSpecialNeedsReferenceDataFromFlightShopping - Response for " + cslActionName, "Trace", appId, appVersion, deviceId, jsonResponse));


            if (response != null)
            {

                if (response == null || response.SpecialRequestResponses == null || response.ServiceAnimalResponses == null || response.SpecialMealResponses == null || response.SpecialRequestResponses == null)
                    throw new UnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

                return response;
            }
            else
            {
                throw new UnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
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

        private IEnumerable<ReservationFlightSegment> GetOfferedMealsForItineraryFromPNRManagement(Session session, int appId, string appVersion, string deviceId, IEnumerable<ReservationFlightSegment> segments)
        {
            string cslActionName = "SpecialMeals/FlightSegments";


            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetOfferedMealsForItineraryFromPNRManagement - Request url for " + cslActionName, "Trace", appId, appVersion, deviceId, url));

            string jsonRequest = JsonConvert.SerializeObject(GetOfferedMealsForItineraryFromPNRManagementRequest(segments));

            //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetOfferedMealsForItineraryFromPNRManagement - Request for " + cslActionName, "Trace", appId, appVersion, deviceId, jsonRequest));
            string token = _dPService.GetAnonymousToken(Headers.ContextValues.Application.Id, Headers.ContextValues.DeviceId, _configuration).Result;

            var cslCallDurationstopwatch = new Stopwatch();
            cslCallDurationstopwatch.Start();

            var response = _updatePNRService.GetOfferedMealsForItinerary<List<ReservationFlightSegment>>(token, cslActionName, jsonRequest, session.SessionId).Result;

            if (cslCallDurationstopwatch.IsRunning)
            {
                cslCallDurationstopwatch.Stop();
            }


            //    logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "SpecialMeals/FlightSegments - CSL Call Duration", "CSS/CSL-CallDuration", appId, appVersion, deviceId, "CSLSpecialMeals/FlightSegments=" + (cslCallDurationstopwatch.ElapsedMilliseconds / (double)1000).ToString()));
            //    logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "GetOfferedMealsForItineraryFromPNRManagement - Response for " + cslActionName, "Trace", appId, appVersion, deviceId, jsonResponse));


            if (response != null)
            {

                if (response == null)
                {
                    throw new UnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                if (response.Count > 0)
                {
                    _sessionHelperService.SaveSession<List<ReservationFlightSegment>>(response, Headers.ContextValues);//, response[0].GetType().FullName);
                }

                return response;
            }
            else
            {
                throw new UnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
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
                                             animalType.Description, animalType.Description.EndsWith("animal", StringComparison.OrdinalIgnoreCase) ? null : "Dog", TravelSpecialNeedType.ServiceAnimalType.ToString());
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
            return GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, "TravelSpecialNeedInfo_TaskTrainedServiceDog_Supported_AppVestion_Android", "TravelSpecialNeedInfo_TaskTrainedServiceDog_Supported_AppVestion_iOS");
        }
        private void AddServiceAnimalsMessageSection(TravelSpecialNeeds offersSSR, int appId, string appVersion, Session session, string deviceId)
        {
            if (IsTaskTrainedServiceDogSupportedAppVersion(appId, appVersion) && offersSSR?.ServiceAnimals != null && offersSSR.ServiceAnimals.Any())
            {
                MOBRequest request = new MOBRequest();
                request.Application = new MOBApplication();
                request.Application.Id = appId;
                request.Application.Version = new MOBVersion();
                request.Application.Version.Major = appVersion;
                request.DeviceId = deviceId;

                //string cmsCacheResponse = _sessionHelperService.GetSession<string>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID"), "MOBCSLContentMessagesResponse");
                string cmsCacheResponse = _sessionHelperService.GetSession<string>(Headers.ContextValues, "MOBCSLContentMessagesResponse").Result;
                CSLContentMessagesResponse content = new CSLContentMessagesResponse();

                if (string.IsNullOrEmpty(cmsCacheResponse))
                    content = _travelerCSL.GetBookingRTICMSContentMessages(request, session);//, LogEntries);
                else
                    content = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(cmsCacheResponse);

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
                        Messages = new List<Model.Common.MOBItem>
                        {
                            new Model.Common.MOBItem {
                                CurrentValue = emotionalSupportAssistantContent
                            }
                        }
                    });
                }
            }

            else if (_configuration.GetValue<bool>("EnableTravelSpecialNeedInfo")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, "TravelSpecialNeedInfo_Supported_AppVestion_Android", "TravelSpecialNeedInfo_Supported_AppVestion_iOS")
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
                        Messages = new List<Model.Common.MOBItem>
                        {
                            new Model.Common.MOBItem {
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

        private List<TravelSpecialNeed> GetOfferedSpecialRequests(MultiCallResponse flightshoppingReferenceData)
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

                    specialRequests.Add(sr);
                }
                else if (specialRequest.Genre.Description.Equals("WheelchairReason"))
                {
                    if (createdWheelChairItem == null)
                    {
                        createdWheelChairItem = createSpecialNeedItem(_configuration.GetValue<string>("SSRWheelChairDescription"), null, _configuration.GetValue<string>("SSRWheelChairDescription"));
                        createdWheelChairItem.SubOptionHeader = _configuration.GetValue<string>("SSR_WheelChairSubOptionHeader");
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

        private List<MOBSHOPCountry> GetNationalityResidenceCountries(int applicationId, string deviceId, string appVersion, string transactionId, string sessionID, string token)
        {
            List<MOBSHOPCountry> lstNationalityResidenceCountries = null;

            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionID, "GetNationalityResidenceCountries", "Trace", applicationId, appVersion, deviceId, null));

            try
            {
                string logAction = "/NationalityResidenceCountries";

                //logEntries.Add(LogEntry.GetLogEntry<string>(sessionID, "GetNationalityResidenceCountries - Request url", "Trace", applicationId, appVersion, deviceId, url));

                var response = _referencedataService.GetNationalityResidence<List<United.Service.Presentation.CommonModel.Characteristic>>(logAction, token, sessionID).Result;

                //logEntries.Add(LogEntry.GetLogEntry<string>(sessionID, "GetNationalityResidenceCountries - CSL Call Duration", "CSS/CSL-CallDuration", applicationId, appVersion, deviceId, "CSL_shoppingUtilityValidatePhone=" + cslCallTime));


                //logEntries.Add(LogEntry.GetLogEntry<string>(sessionID, "GetNationalityResidenceCountries - Response", "Trace", applicationId, appVersion, deviceId, jsonResponse));


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

                //logEntries.Add(LogEntry.GetLogEntry<string>(sessionID, "GetNationalityResidenceCountries", "Exception", applicationId, appVersion, deviceId, ex.Message));

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
        
        private List<MOBItem> GetMPPINPWDTitleMessages(List<string> titleList)
        {
            List<MOBItem> messages = new List<MOBItem>();
            var documentLibrary = new DocumentLibraryDynamoDB(_configuration, _dynamoDBService);
            List<United.Definition.MOBLegalDocument> docs = documentLibrary.GetNewLegalDocumentsForTitles(titleList, Headers.ContextValues.SessionId).Result;
            if (docs != null && docs.Count > 0)
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

        private List<MOBSHOPTrip> PopulateMetaTrips(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string cartId, ShoppingResponse persistShop, string sessionId, int appId, string deviceId, string appVersion, bool showMileageDetails, List<United.Services.FlightShopping.Common.DisplayCart.DisplayTrip> flightSegmentCollection, string fareClass, List<string> flightDepartDatesForSelectedTrip)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            Session session = new Session();
            session = _sessionHelperService.GetSession<Session>(Headers.ContextValues, session.ObjectName).Result;
            supressLMX = session.SupressLMXForAppID;
            #endregion
            List<MOBSHOPTrip> trips = new List<MOBSHOPTrip>();

            try
            {
                airportsList = _sessionHelperService.GetSession<AirportDetailsList>(Headers.ContextValues).Result;
                if (isGetAirportListInOneCallToggleOn() && (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0))
                {
                    airportsList = GetAllAiportsList(flightSegmentCollection);
                }
            }
            catch (Exception ex)
            {
                //logEntries.Add(LogEntry.GetLogEntry<Exception>(sessionId, "GetAllAiportsList", "PopulateMetaTrips-GetAllAiportsList", appId, appVersion, deviceId, ex, true, true));
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

                    trip.DepartDate = FormatDateFromDetails(flightSegmentCollection[i].DepartDate);
                    trip.ArrivalDate = FormatDateFromDetails(flightSegmentCollection[i].Flights[flightSegmentCollection[i].Flights.Count - 1].DestinationDateTime);
                    trip.Destination = GetAirportCode(flightSegmentCollection[i].Destination);

                    string destinationName = string.Empty;
                    if (!string.IsNullOrEmpty(trip.Destination))
                    {
                        destinationName = GetAirportNameFromSavedList(trip.Destination);
                    }
                    if (string.IsNullOrEmpty(destinationName))
                    {
                        trip.DestinationDecoded = flightSegmentCollection[i].Flights[flightSegmentCollection[i].Flights.Count - 1].DestinationDescription;
                    }
                    else
                    {
                        trip.DestinationDecoded = destinationName;
                    }

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

                            Parallel.Invoke(() =>
                            {
                                bool includeAmenities = false;
                                bool.TryParse(_configuration.GetValue<string>("IncludeAmenities"), out includeAmenities);

                                //if we are asking for amenities in the CSL call, do not make this seperate call
                                if (!includeAmenities)
                                {
                                    amenitiesResponse = GetAmenitiesForFlights(sessionId, cartId, tempFlights, appId, deviceId, appVersion);
                                    PopulateFlightAmenities(amenitiesResponse.Profiles, ref tempFlights);
                                }
                            },
                                    () =>
                                    {
                                        if (showMileageDetails && !supressLMX)
                                        {
                                            //get all flight numbers
                                            List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights = null;
                                            lmxFlights = GetLmxFlights(session.Token, cartId, GetFlightHasListForLMX(tempFlights), sessionId, appId, appVersion, deviceId);

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
                        flights = GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, flightSegmentCollection[i].Flights, string.Empty, out ci, 0.0M, trip.Columns, 0, fareClass, false);
                    }

                    trip.Origin = GetAirportCode(flightSegmentCollection[i].Origin);

                    if (showMileageDetails && !supressLMX)
                        trip.YqyrMessage = _configuration.GetValue<string>("MP2015YQYRMessage");

                    string originName = string.Empty;
                    if (!string.IsNullOrEmpty(trip.Origin))
                    {
                        originName = GetAirportNameFromSavedList(trip.Origin);
                    }
                    if (string.IsNullOrEmpty(originName))
                    {
                        trip.OriginDecoded = flightSegmentCollection[i].Flights[0].OriginDescription;
                    }
                    else
                    {
                        trip.OriginDecoded = originName;
                    }

                    if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI"))
                    {
                        //with share trip flag OriginDescription=Chicago, IL, US (ORD)
                        trip.OriginDecodedWithCountry = flightSegmentCollection[i].Flights[0].OriginDescription;

                        string destinationDecodedWithCountry = string.Empty;

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

                    if (flights != null)
                    {

                        string tripDepartDate = string.Empty;
                        foreach (string tripIDDepDate in flightDepartDatesForSelectedTrip)
                        {
                            if (tripIDDepDate.Split('|')[0].ToString().Trim() == trip.TripId)
                            {
                                tripDepartDate = tripIDDepDate.Split('|')[1].ToString().Trim();
                                break;
                            }
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

                            flight.DepartureDateTimeGMT = _shoppingUtility.GetGMTTime(flight.DepartureDateTime, flight.Origin);
                            flight.ArrivalDateTimeGMT = _shoppingUtility.GetGMTTime(flight.ArrivalDateTime, flight.Destination);

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

                                    connection.DepartureDateTimeGMT = _shoppingUtility.GetGMTTime(connection.DepartureDateTime, connection.Origin);
                                    connection.ArrivalDateTimeGMT = _shoppingUtility.GetGMTTime(connection.ArrivalDateTime, connection.Destination);

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
                                            conn.DepartureDateTimeGMT = _shoppingUtility.GetGMTTime(conn.DepartureDateTime, conn.Origin);
                                            conn.ArrivalDateTimeGMT = _shoppingUtility.GetGMTTime(conn.ArrivalDateTime, conn.Destination);
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
                                    connection.DepartureDateTimeGMT = _shoppingUtility.GetGMTTime(connection.DepartureDateTime, connection.Origin);
                                    connection.ArrivalDateTimeGMT = _shoppingUtility.GetGMTTime(connection.ArrivalDateTime, connection.Destination);
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

        private AirportDetailsList GetAllAiportsList(List<DisplayTrip> displayTrip)
        {
            string airPortCodes = GetAllAirportCodesWithCommaDelimatedFromCSLDisplayTrips(displayTrip);
            return GetAirportNamesListCollection(airPortCodes);
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

        private AirportDetailsList GetAllAiportsList(List<Flight> flights)
        {
            string airPortCodes = GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(flights);
            return GetAirportNamesListCollection(airPortCodes);
        }

        private AirportDetailsList GetAirportNamesListCollection(string airPortCodes)
        {
            AirportDetailsList retVal = null;
            if (airPortCodes != string.Empty)
            {
                airPortCodes = "'" + airPortCodes + "'";
                airPortCodes = String.Join(",", airPortCodes.Split(',').Distinct());
                airPortCodes = System.Text.RegularExpressions.Regex.Replace(airPortCodes, ",", "','");
                retVal = new AirportDetailsList();
                retVal.AirportsList = _shoppingUtility.GetAirportNamesList(airPortCodes);
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

        private string GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(List<Flight> flights)
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

        private List<Model.Shopping.MOBSHOPFlight> GetFlights(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string sessionId, string cartId, List<Flight> segments, string requestedCabin, out CultureInfo ci, decimal lowestFare, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClass, bool updateAmenities = false, bool isConnection = false, bool isELFFareDisplayAtFSR = true, MOBSHOPTrip trip = null, string appVersion = "", int appID = 0)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            Session session = new Session();
            session = _sessionHelperService.GetSession<Session>(Headers.ContextValues, session.ObjectName).Result;
            supressLMX = session.SupressLMXForAppID;
            #endregion
            try
            {
                if (isGetAirportListInOneCallToggleOn() && (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0))
                {
                    airportsList = _sessionHelperService.GetSession<AirportDetailsList>(Headers.ContextValues).Result;//, (new AirportDetailsList()).GetType().FullName);
                    if (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0)
                    {
                        airportsList = GetAllAiportsList(segments);
                    }
                }
            }
            catch (Exception ex)
            {
                //logEntries.Add(LogEntry.GetLogEntry<Exception>(sessionId, "GetAllAiportsList", "GetFlights-GetAllAiportsList", 1, string.Empty, string.Empty, ex, true, true));
                //logEntries.Add(LogEntry.GetLogEntry<List<Flight>>(sessionId, "GetAllAiportsList", "PopulateMetaTrips-Trip", 1, string.Empty, string.Empty, segments, true, true));
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

                        flight.Connections = GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, segment.Connections, requestedCabin, out ci, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR);
                    }

                    flight.ConnectTimeMinutes = segment.ConnectTimeMinutes > 0 ? GetFormattedTravelTime(segment.ConnectTimeMinutes) : string.Empty;

                    flight.DepartDate = FormatDate(segment.DepartDateTime);
                    flight.DepartTime = FormatTime(segment.DepartDateTime);
                    flight.Destination = segment.Destination;
                    flight.DestinationDate = FormatDate(segment.DestinationDateTime);
                    flight.DestinationTime = FormatTime(segment.DestinationDateTime);
                    flight.DepartureDateTime = FormatDateTime(segment.DepartDateTime);
                    flight.ArrivalDateTime = FormatDateTime(segment.DestinationDateTime);

                    string destinationName = string.Empty;
                    if (!string.IsNullOrEmpty(flight.Destination))
                    {
                        destinationName = GetAirportNameFromSavedList(flight.Destination);
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

                    if (_configuration.GetValue<string>("ReturnShopSelectTripOnTimePerformance") != null && _configuration.GetValue<bool>("ReturnShopSelectTripOnTimePerformance"))
                    {
                        flight.OnTimePerformance = PopulateOnTimePerformanceSHOP(segment.OnTimePerformanceInfo);
                    }
                    flight.OperatingCarrier = segment.OperatingCarrier;
                    if (
                            (flight.OperatingCarrier != null && flight.OperatingCarrier.ToUpper() != "UA") ||
                            (segment.OperatingCarrierDescription != null && (
                                segment.OperatingCarrierDescription.ToUpper().Contains("EXPRESS") ||
                                segment.OperatingCarrierDescription.ToUpper().Contains("AMTRAK")
                            ))
                        )
                    {
                        TextInfo ti = ci.TextInfo;
                        flight.OperatingCarrierDescription = !string.IsNullOrEmpty(segment.OperatingCarrierDescription) ? ti.ToTitleCase(segment.OperatingCarrierDescription.ToLower()) : "";
                    }

                    flight.Origin = segment.Origin;

                    string originName = string.Empty;
                    if (!string.IsNullOrEmpty(flight.Origin))
                    {
                        originName = GetAirportNameFromSavedList(flight.Origin);
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
                                    List<Flight> stops = new List<Flight>();
                                    stops.Add(stop);
                                    if (_mOBSHOPDataCarrier == null)
                                        _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                                    List<Model.Shopping.MOBSHOPFlight> stopFlights = GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, stops, requestedCabin, out ci, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR);
                                    foreach (Model.Shopping.MOBSHOPFlight sf in stopFlights)
                                    {
                                        sf.ChangeOfGauge = true;
                                    }

                                    ///245704 - PROD: Mapp - Incorrect segments info is displayed for a 2-Stop flight UA1666 on choose your travel experience screen
                                    ///Srini - 03/20/2018
                                    if (_configuration.GetValue<bool>("BugFixToggleFor18C"))
                                    {
                                        flight.Destination = stop.Origin;
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
                                        destination = GetAirportNameFromSavedList(flight.Destination);
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
                                    stopFlight.GroundTime = $"{stop.GroundTimeMinutes}";
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
                                        stopDestination = GetAirportNameFromSavedList(stopFlight.Destination);
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
                                        stopOrigin = GetAirportNameFromSavedList(stopFlight.Origin);
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

                    flight.TripId = segment.BBXSolutionSetId;

                    if (_mOBSHOPDataCarrier == null)
                        _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                    flight.ShoppingProducts = PopulateProducts(_mOBSHOPDataCarrier, segment.Products, sessionId, ref flight, requestedCabin, segment, lowestFare, columnInfo, premierStatusLevel, fareClass, isELFFareDisplayAtFSR);

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

            return flights;
        }

        private void SetSegmentInfoMessages(Model.Shopping.MOBSHOPFlight flight, Warning warn)
        {
            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("ARRIVAL_Slice")
                && !string.IsNullOrEmpty(warn.Title))
            {
                flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, ArrivesNextDaySegmentInfo(warn.Title));
            }

            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("CHANGE_OF_AIRPORT_SLICE"))
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
            if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == MOBTripPlannerType.TPSearch.ToString())
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

        private List<Model.Shopping.MOBSHOPShoppingProduct> PopulateProducts(MOBSHOPDataCarrier _mOBSHOPDataCarrier, ProductCollection products, string sessionId, ref Model.Shopping.MOBSHOPFlight flight, string cabin, Flight segment, decimal lowestAirfare, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClas, bool isELFFareDisplayAtFSR = true)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            Session session = new Session();
            session = _sessionHelperService.GetSession<Session>(Headers.ContextValues, session.ObjectName).Result;
            supressLMX = session.SupressLMXForAppID;
            #endregion
            if (_mOBSHOPDataCarrier == null)
                _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
            return PopulateProducts(_mOBSHOPDataCarrier, products, cabin, segment, lowestAirfare, columnInfo, premierStatusLevel, fareClas, supressLMX, session, isELFFareDisplayAtFSR);
        }

        private List<Model.Shopping.MOBSHOPShoppingProduct> PopulateProducts(MOBSHOPDataCarrier _mOBSHOPDataCarrier, ProductCollection products, string cabin, Flight segment, decimal lowestAirfare,
            List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClas, bool supressLMX, Session session, bool isELFFareDisplayAtFSR = true)
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
                            ref foundEconomyAward, ref foundBusinessAward, ref foundFirstAward, session, isELFFareDisplayAtFSR);

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
                        if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == MOBTripPlannerType.TPSearch.ToString())
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
                    var productSection = new ProductSection();
                    _configuration.GetSection("productSettings").Bind(productSection);

                    //var configurationProductSettings = JsonConvert.DeserializeObject<ProductSection>(configurationProductSettingsjson.Value);
                    SetShortCabin(shopProd, columnInfo, productSection);
                    //SetShortCabin(shopProd, columnInfo, configurationProductSettings);

                    if (string.IsNullOrEmpty(shopProd.Description))
                    {
                        //SetShoppingProductDescriptionBasedOnProductElementDescription
                        //(shopProd, columnInfo, configurationProductSettings);
                        SetShoppingProductDescriptionBasedOnProductElementDescription
                            (shopProd, columnInfo, productSection);
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
                newProd.ColumnID = columnInfo.First(c => c != null && !string.IsNullOrEmpty(c.Type) && c.Type.ToUpper().Equals(prod.ProductType.ToUpper())).ColumnID;
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
            ref bool foundBusinessAward, ref bool foundFirstAward, Session session, bool isELFFareDisplayAtFSR)
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

            SetProductPriceInformation(prod, ci, newProd, session);
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
            newProd.IsELF = prod.IsElf;

            if (newProd.IsELF && string.IsNullOrEmpty(newProd.ProductCode))
            {
                newProd.ProductCode = _configuration.GetValue<string>("ELFProductCode"); ;
            }

            return newProd;
        }

        private void SetProductPriceInformation(Product prod, CultureInfo ci, Model.Shopping.MOBSHOPShoppingProduct newProd, Session session)
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
            if (isTripPlannerViewEnabled && session.TravelType == MOBTripPlannerType.TPSearch.ToString())
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

        private List<United.Services.FlightShopping.Common.LMX.LmxFlight> GetLmxFlights(string token, string cartId, string hashList, string sessionId, int applicationId, string appVersion, string deviceId)
        {
            List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights = null;

            if (!string.IsNullOrEmpty(cartId))
            {
                string jsonRequest = "{\"CartId\":\"" + cartId + "\"}";

                //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetLmxFlights - URL", "Trace", applicationId, appVersion, deviceId, url));


                if (!string.IsNullOrEmpty(hashList))
                {
                    jsonRequest = "{\"CartId\":\"" + cartId + "\", \"hashList\":[" + hashList + ")}";
                }

                FlightStatus flightStatus = new FlightStatus();
                LmxQuoteResponse response = new LmxQuoteResponse();
                try
                {
                    //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetLmxFlights - Request", "Trace", applicationId, appVersion, deviceId, jsonRequest));

                    response = _lmxInfo.GetProductInfo<LmxQuoteResponse>(token, jsonRequest, sessionId).Result;

                }
                catch (System.Exception) { }

                if (response != null)
                {

                    //logEntries.Add(LogEntry.GetLogEntry<LmxQuoteResponse>(sessionId, "GetLmxFlights - Response", "Trace", applicationId, appVersion, deviceId, lmxQuoteResponse));


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
        private string FormatDateFromDetails(string dateTimeString)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(0);
            DateTime.TryParse(dateTimeString, out dateTime);
            if (dateTime.Ticks != 0)
            {
                result = string.Format("{0:MM/dd/yyyy}", dateTime);
            }

            return result;
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

        private string GetAirportNameFromSavedList(string airportCode)
        {
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
                        var airportObj = _shoppingUtility.GetAirportNamesList("'" + airportCode + "'");
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
                            airportDesc = _shoppingUtility.GetAirportName(airportCode);
                        }
                    }
                }
                else
                {
                    airportDesc = _shoppingUtility.GetAirportName(airportCode);
                }
            }
            catch (Exception ex)
            {
                airportDesc = _shoppingUtility.GetAirportName(airportCode);
            }
            return airportDesc;
        }

        private UpdateAmenitiesIndicatorsResponse GetAmenitiesForFlights(string sessionId, string cartId, List<Flight> flights, int appId, string deviceId, string appVersion, bool isClientCall = false, UpdateAmenitiesIndicatorsRequest amenitiesPersistRequest = null)
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

            string action = "UpdateAmenitiesIndicators";

            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetAmenitiesForFlight - Request url for GetAmenitiesForFlight", "Trace", appId, appVersion, deviceId, url));

            Session session = new Session();
            session = _sessionHelperService.GetSession<Session>(Headers.ContextValues, session.ObjectName).Result;
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
            string token = _dPService.GetAnonymousToken(Headers.ContextValues.Application.Id, Headers.ContextValues.DeviceId, _configuration).Result;
            var response = _flightShoppingService.GetAmenitiesInfo<UpdateAmenitiesIndicatorsResponse>(token, action, jsonRequest, sessionId).Result;

            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetAmenitiesForFlight - Response for GetAmenitiesForFlight", "Trace", appId, appVersion, deviceId, jsonResponse));

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

        //added-Kriti


        private CPCubaTravel GetCubaTravelResons(MobileCMSContentRequest request)
        {

            request.GroupName = "General:CubaTravelCerts";
            string jsonResponse = GETCSLCMSContent(request);

            CPCubaTravel mobCPCubaTravel = new CPCubaTravel();
            if (!string.IsNullOrEmpty(jsonResponse))
            {

                var response = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(jsonResponse);

                if (response != null && (Convert.ToBoolean(response.Status) && response.MessageItems != null))
                {

                    //logEntries.Add(LogEntry.GetLogEntry<MOBCSLContentMessagesResponse>(request.SessionId, "GetMobileCMSContents - DeSerialized Response", "DeSerialized Response", request.Application.Id, request.Application.Version.Major, request.DeviceId, response));


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

        private string GETCSLCMSContent(MobileCMSContentRequest request, bool isTravelAdvisory = false)
        {
            #region
            if (request == null)
            {
                throw new MOBUnitedException("GetMobileCMSContents request cannot be null.");
            }
            #region Get CSL Content request
            MOBCSLContentMessagesRequest cslContentReqeust = BuildCSLContentMessageRequest(request, isTravelAdvisory);
            #endregion

            string jsonResponse = GetCSLCMSContentMesseges(request, cslContentReqeust);
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

        private string GetCSLCMSContentMesseges(MobileCMSContentRequest request, MOBCSLContentMessagesRequest cslContentReqeust)
        {
            string jsonRequest = JsonConvert.SerializeObject(cslContentReqeust);

            //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "GetMobileCMSContents - Request", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonRequest));

            //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "GetMobileCMSContents - Request url", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, url));

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cslStopWatch;
            cslStopWatch = new Stopwatch();
            cslStopWatch.Reset();
            cslStopWatch.Start();
            #endregion****Get Call Duration Code - Venkat 03/17/2015*******
            var response = _iCMSContentService.GetMobileCMSContentMessages(token: request.Token, jsonRequest, Headers.ContextValues.SessionId).Result;
            #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cslStopWatch.IsRunning)
            {
                cslStopWatch.Stop();
            }
            string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

            //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "GetMobileCMSContents - CSL Call Duration", "CSS/CSL-CallDuration", request.Application.Id, request.Application.Version.Major, request.DeviceId, "CSLGetCMSContentMessages=" + cslCallTime));

            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******   

            //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "GetMobileCMSContents - Response", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonResponse));


            return response;
        }

        //public List<Model.Shopping.MOBSHOPPrice> GetPrices(List<DisplayPrice> prices, bool isAwardBooking, string sessionId, bool isReshopChange = false)
        //{
        //    List<Model.Shopping.MOBSHOPPrice> bookingPrices = new List<Model.Shopping.MOBSHOPPrice>();
        //    CultureInfo ci = null;
        //    foreach (var price in prices)
        //    {
        //        if (ci == null)
        //        {
        //            ci = TopHelper.GetCultureInfo(price.Currency);
        //        }

        //        Model.Shopping.MOBSHOPPrice bookingPrice = new Model.Shopping.MOBSHOPPrice();
        //        bookingPrice.CurrencyCode = price.Currency;
        //        bookingPrice.DisplayType = price.Type;
        //        bookingPrice.Status = price.Status;
        //        bookingPrice.Waived = price.Waived;
        //        bookingPrice.DisplayValue = string.Format("{0:#,0.00}", price.Amount);

        //        if (!isReshopChange)
        //        {
        //            if (!string.IsNullOrEmpty(bookingPrice.DisplayType) && bookingPrice.DisplayType.Equals("MILES") && isAwardBooking && !string.IsNullOrEmpty(sessionId))
        //            {
        //                ValidateAwardMileageBalance(sessionId, price.Amount);
        //            }
        //        }

        //        double tempDouble = 0;
        //        double.TryParse(price.Amount.ToString(), out tempDouble);
        //        bookingPrice.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);

        //        if (price.Currency.ToUpper() == "MIL")
        //        {
        //            bookingPrice.FormattedDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(price.Amount.ToString(), false);
        //        }
        //        else
        //        {
        //            if (price.Amount < 0
        //                && (string.Equals("TaxDifference", price.Type, StringComparison.OrdinalIgnoreCase)
        //                || string.Equals("FareDifference", price.Type, StringComparison.OrdinalIgnoreCase)))
        //                bookingPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(price.Amount * -1, ci, false);
        //            else
        //                bookingPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(price.Amount, ci, false);
        //        }
        //        bookingPrices.Add(bookingPrice);
        //    }

        //    return bookingPrices;
        //}

        private void ValidateAwardMileageBalance(string sessionId, decimal milesNeeded, int appId = 0, string appVersion = "")
        {
            CSLShopRequest shopRequest = new CSLShopRequest();
            shopRequest = _sessionHelperService.GetSession<CSLShopRequest>(Headers.ContextValues, shopRequest.ObjectName).Result;
            if (shopRequest != null && shopRequest.ShopRequest != null && shopRequest.ShopRequest.AwardTravel && shopRequest.ShopRequest.LoyaltyPerson != null && shopRequest.ShopRequest.LoyaltyPerson.AccountBalances != null)
            {
                if (shopRequest.ShopRequest.LoyaltyPerson.AccountBalances[0] != null && shopRequest.ShopRequest.LoyaltyPerson.AccountBalances[0].Balance < milesNeeded)
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("NoEnoughMilesForAwardBooking"));
                }
            }
        }

        //---------------------------------
        #region Select Trip

        public async Task<SelectTripResponse> SelectTrip(SelectTripRequest selectTripRequest)
        {

            SelectTripResponse response = new SelectTripResponse();
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            response.Request = selectTripRequest;
            string logAction = "SelectTrip";
            bool isDefault = false;
            Session session = null;

            var version1 = _configuration.GetValue<string>("AndriodCFOP_Booking_Reshop_PostbookingAppVersion");
            var version2 = _configuration.GetValue<string>("IphoneCFOP_Booking_Reshop_PostbookingAppVersion");
            bool isCFOPVersionCheck = GeneralHelper.IsApplicationVersionGreaterorEqual(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, version1, version2);
            try
            {
                session = _shoppingSessionHelper.GetBookingFlowSession(selectTripRequest.SessionId);
                logAction = session.IsReshopChange ? "ReShop - SelectTrip" : "SelectTrip";
                if (!session.IsReshopChange)
                {
                    Reservation bookingPathRes = new Reservation();
                    await _sessionHelperService.SaveSession<Reservation>(null, Headers.ContextValues, bookingPathRes.ObjectName);
                }

                United.Mobile.Model.Shopping.SelectTrip selectTrip = new United.Mobile.Model.Shopping.SelectTrip();
                try
                {
                    selectTrip = await _sessionHelperService.GetSession<SelectTrip>(Headers.ContextValues, selectTrip.ObjectName);
                }
                catch (System.Exception ex)
                {
                    #region
                    ExceptionWrapper exceptionWrapper = new ExceptionWrapper(ex);
                    //shopping.//logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, logAction, "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, exceptionWrapper, true, false));
                    #endregion
                }
                if (IsLatestClientAppVerWithSelectTripBackButtonFix(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                {
                    #region Verify the Select Trip Persist if its a Back Button Click on Client - Index out of Range Fix - Venkat
                    if (selectTrip != null && selectTripRequest.BackButtonClick)
                    {
                        #region
                        selectTrip.Requests = ShopStaticUtility.RebuildSelectTripRequestDictionary2(selectTrip.Requests.Count - 1, selectTrip.Requests); // Get all  Select Trip Requests of Previous Trips in Back Button Client Click
                        selectTrip.Responses = ShopStaticUtility.RebuildSelectTripResponseDictionary2(selectTrip.Responses.Count - 1, selectTrip.Responses); // Get all Select Trip Responses of Previous Trips in Back Button Client Click
                        SelectTripRequest prevSelectedTripRequest = ShopStaticUtility.GetLastSelectedTrip_TripID(selectTrip.Requests); // Get the Last Select Trip Request ID in Back Button Click
                        // Get Last Select Trip Trip Key
                        // Added By Ali as part of Tech Task 264624:Booking flow exception analysis - Get Select Trip - System.NullReference Exception
                        if ((!_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis")) || (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && prevSelectedTripRequest != null))
                        {
                            selectTrip.LastSelectTripKey = prevSelectedTripRequest.TripId;
                            session.LastSelectTripKey = prevSelectedTripRequest.TripId;
                        }
                        // Get Last Select Trip Trip Key
                        // Save to Persist the Select Trip Requets and Responses after we got only 
                        await _sessionHelperService.SaveSession<SelectTrip>(selectTrip, Headers.ContextValues, selectTrip.ObjectName);
                        await _sessionHelperService.SaveSession<Session>(session, Headers.ContextValues, session.ObjectName);
                        #endregion
                    }
                    #endregion
                }
                AwardMTThrowErrorIfCurrentTripDepartDateGreaterThanNextTripDepartDate(selectTripRequest, selectTrip, session);
                #region Call to DAL
                int nonStopFlightsCount = 0;
                //if (_configuration.GetValue<bool>("EnableNonStopFlight"))
                //{
                //    if (request.GetFlightsWithStops)
                //    {
                //        MOBSHOPAvailability nonStopsAvailability = shopping.GetLastTripAvailabilityFromPersist(1, session.SessionId);
                //        nonStopFlightsCount = nonStopsAvailability.Trip.FlightCount;
                //    }
                //}
                //if (traceSwitch.TraceInfo)
                //{
                //    shopping.//logEntries.Add(United.Logger.LogEntry.GetLogEntry<SelectTripRequest>(selectTripRequest.SessionId, logAction, "Request", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, selectTripRequest, true, false));
                //}

                response.TransactionId = selectTripRequest.TransactionId;
                response.LanguageCode = selectTripRequest.LanguageCode;

                ShoppingResponse shop = new ShoppingResponse();
                try
                {
                    shop = await _sessionHelperService.GetSession<ShoppingResponse>(Headers.ContextValues.SessionId, shop.ObjectName, new List<string>() { Headers.ContextValues.SessionId, shop.ObjectName });
                }
                catch (System.Exception ex) { }

                int totalPassengers = 1;
                if (shop != null && shop.Request != null)
                {
                    if (_shoppingUtility.EnableTravelerTypes(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, session.IsReshopChange) && shop.Request.TravelerTypes != null && shop.Request.TravelerTypes.Count > 0)
                    {
                        totalPassengers = ShopStaticUtility.GetTravelerCount(shop.Request.TravelerTypes);
                    }
                    else
                    {
                        totalPassengers = shop.Request.NumberOfAdults + shop.Request.NumberOfSeniors + shop.Request.NumberOfChildren12To17 + shop.Request.NumberOfChildren5To11 + shop.Request.NumberOfChildren2To4 + shop.Request.NumberOfInfantWithSeat + shop.Request.NumberOfInfantOnLap;
                    }
                }
                //int tripCount = 1;
                response.Availability = await SelectTrip(selectTripRequest, totalPassengers);
                response.Disclaimer = GetDisclaimerString();

                if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "YES")
                    response.CartId = response.Availability.CartId;
                #endregion

                if (IsLatestClientAppVerWithSelectTripBackButtonFix(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                {
                    #region Select Trip Persist Code
                    if (selectTrip == null)
                    {
                        selectTrip = new SelectTrip();
                    }

                    if (selectTrip.Requests == null || selectTrip.Responses == null)
                    {
                        selectTrip.Requests = new SerializableDictionary<string, SelectTripRequest>();
                        selectTrip.Responses = new SerializableDictionary<string, SelectTripResponse>();

                        //first flight so get refresh data from shop
                        response.RefreshResultsData = string.Format("{0},{1},{2}", shop.Request.Trips[0].DepartDate, "", "");
                    }
                    #region
                    //////string tripId = tripCount.ToString();
                    //if (selectTrip.Requests.ContainsKey(request.TripId))
                    //{
                    //    selectTrip.Requests[request.TripId] = request;
                    //    selectTrip.Responses[request.TripId] = response;

                    //    // get info for refresh results data
                    //    if (selectTrip.Requests.Count > 0)
                    //    {
                    //        response.RefreshResultsData = string.Format("{0},{1},{2}", shop.Request.Trips[selectTrip.Requests.Count - 1].DepartDate, request.TripId, request.ProductId);
                    //    }

                    //    //need to clear out all requests after this one as it's a reshop
                    //    selectTrip.Requests = _shoppingUtility.RebuildSelectTripRequestDictionary(request.TripId, selectTrip.Requests);
                    //    selectTrip.Responses = _shoppingUtility.RebuildSelectTripResponseDictionary(request.TripId, selectTrip.Responses);
                    //}
                    #endregion
                    if (!selectTripRequest.BackButtonClick && selectTripRequest.ISProductSelected)
                    {
                        #region Mobile EXC: Added the below if condition to check the duplicate value in selectTrip dictionary object to avoid the System.ArgumentException(An item with the same key has already been added Exception). - Srinivas
                        if (selectTrip.Requests.ContainsKey(selectTripRequest.TripId))
                        {
                            selectTrip.Requests[selectTripRequest.TripId] = selectTripRequest;
                            selectTrip.Responses[selectTripRequest.TripId] = response;
                        }
                        else
                        {
                            selectTrip.Requests.Add(selectTripRequest.TripId, selectTripRequest);
                            selectTrip.Responses.Add(selectTripRequest.TripId, response);
                        }
                        selectTrip.LastSelectTripKey = selectTripRequest.TripId;
                        session.LastSelectTripKey = selectTrip.LastSelectTripKey;
                        #endregion
                    }
                    try
                    {
                        if (selectTrip != null && selectTrip.Requests != null && selectTrip.Requests.Count > 0
                            && shop.Request != null && shop.Request.Trips != null && shop.Request.Trips.Count >= selectTrip.Requests.Count)
                        {
                            response.RefreshResultsData = string.Format("{0},{1},{2}", shop.Request.Trips[selectTrip.Requests.Count - 1].DepartDate, selectTripRequest.TripId, selectTripRequest.ProductId);
                        }
                    }
                    catch (Exception ex) { } // Why around try catch and not capturing the exception is need to check why client needs this data RefreshResultsData

                    await _sessionHelperService.SaveSession<SelectTrip>(selectTrip, Headers.ContextValues);
                    await _sessionHelperService.SaveSession<Session>(session, Headers.ContextValues);
                    //United.Persist.FilePersist.Save<United.Persist.Definition.Shopping.SelectTrip>(selectTripRequest.SessionId, selectTrip.ObjectName, selectTrip);
                    //United.Persist.FilePersist.Save<Session>(selectTripRequest.SessionId, session.ObjectName, session);
                    #endregion
                }
                else
                {
                    #region Existing Code before Index out of Range Exception
                    if (selectTrip == null)
                    {
                        selectTrip = new SelectTrip();
                    }

                    if (selectTrip.Requests == null || selectTrip.Responses == null)
                    {
                        selectTrip.Requests = new SerializableDictionary<string, SelectTripRequest>();
                        selectTrip.Responses = new SerializableDictionary<string, SelectTripResponse>();

                        //first flight so get refresh data from shop
                        response.RefreshResultsData = string.Format("{0},{1},{2}", shop.Request.Trips[0].DepartDate, "", "");
                    }
                    //string tripId = tripCount.ToString();
                    if (selectTrip.Requests.ContainsKey(selectTripRequest.TripId))
                    {
                        selectTrip.Requests[selectTripRequest.TripId] = selectTripRequest;
                        selectTrip.Responses[selectTripRequest.TripId] = response;

                        // get info for refresh results data
                        if (selectTrip.Requests.Count > 0)
                        {
                            string prevTripId = ShopStaticUtility.GetPreviousKey(selectTripRequest.TripId, selectTrip.Requests);
                            response.RefreshResultsData = string.Format("{0},{1},{2}", shop.Request.Trips[selectTrip.Requests.Count - 1].DepartDate, selectTripRequest.TripId, selectTripRequest.ProductId);
                        }

                        //need to clear out all requests after this one as it's a reshop
                        selectTrip.Requests = ShopStaticUtility.RebuildSelectTripRequestDictionary(selectTripRequest.TripId, selectTrip.Requests);
                        selectTrip.Responses = ShopStaticUtility.RebuildSelectTripResponseDictionary(selectTripRequest.TripId, selectTrip.Responses);
                    }
                    else
                    {
                        selectTrip.Requests.Add(selectTripRequest.TripId, selectTripRequest);
                        selectTrip.Responses.Add(selectTripRequest.TripId, response);

                        // get info for refresh results data
                        ////if (selectTrip.Requests.Count > 0)
                        ////{
                        ////    string prevTripId = _shoppingUtility.GetPreviousKey(request.TripId, selectTrip.Requests);
                        ////    response.RefreshResultsData = string.Format("{0},{1},{2}", shop.Request.Trips[selectTrip.Requests.Count - 1].DepartDate, request.TripId, request.ProductId);
                        ////}
                    }
                    selectTrip.LastSelectTripKey = selectTripRequest.TripId;
                    await _sessionHelperService.SaveSession<SelectTrip>(selectTrip, Headers.ContextValues);
                    //United.Persist.FilePersist.Save<United.Persist.Definition.Shopping.SelectTrip>(selectTripRequest.SessionId, selectTrip.ObjectName, selectTrip);

                    session.LastSelectTripKey = selectTrip.LastSelectTripKey;
                    await _sessionHelperService.SaveSession<Session>(session, Headers.ContextValues);
                    //United.Persist.FilePersist.Save<Session>(selectTripRequest.SessionId, session.ObjectName, session);

                    #endregion
                }

                if (response.Availability != null && response.Availability.Trip != null && response.Availability.Trip.FlattenedFlights != null)
                {
                    //if (!_configuration.GetValue<bool>("EnableNonStopFlight")
                    //|| (request.GetNonStopFlightsOnly && !response.Availability.Trip.TripHasNonStopflightsOnly) // Means First Shop Call and No Non Stop Flights Found in that Market
                    //|| (!request.GetNonStopFlightsOnly && !request.GetFlightsWithStops)) //(!request.GetNonStopFlightsOnly && !request.GetFlightsWithStops) means old Clients
                    if (!_configuration.GetValue<bool>("EnableNonStopFlight") || response.Availability.Trip.PageCount == 2) //**PageCount==2==>> for paging implementation to send only 15 flights back to client and perisit remaining flights and return remaining when customer page up
                    {
                        //TODO - null out FlightSections
                        //response.Availability.Trip.FlightSections = shopping.PopulateFlightSections(request.SessionId, response.Availability.Trip.FlattenedFlights);
                        response.Availability.Trip.FlightSections = null;
                        List<Model.Shopping.MOBSHOPFlattenedFlight> flattenedFlights = response.Availability.Trip.FlattenedFlights.Clone();
                        int organizeResultsPageSize = Convert.ToInt32(_configuration.GetValue<string>("OrgarnizeResultsRequestPageSize"));
                        if (flattenedFlights.Count > organizeResultsPageSize)
                        {
                            response.Availability.Trip.FlattenedFlights = flattenedFlights.GetRange(0, organizeResultsPageSize);
                        }
                        response.Availability.Trip.FlightCount = flattenedFlights.Count;
                        response.Availability.Trip.TotalFlightCount = flattenedFlights.Count;
                    }
                    if (_configuration.GetValue<bool>("EnableNonStopFlight") && selectTripRequest.GetFlightsWithStops)
                    {
                        response.Availability.Trip.FlightCount = response.Availability.Trip.FlightCount + nonStopFlightsCount; // To retrn total flights count both Non Stop from First Shop() call and Stops Flight Count from Second Shop Call()
                        response.Availability.Trip.TotalFlightCount = response.Availability.Trip.FlightCount;
                    }
                }
                // Fixed duplicate messages due to .net deserialization bugs
                if (response.Availability != null && response.Availability.Reservation != null && response.Availability.Reservation.Trips != null && response.Availability.Reservation.Trips.Count > 0)
                {
                    foreach (MOBSHOPTrip trip in response.Availability.Reservation.Trips)
                    {
                        if (trip.FlattenedFlights != null)
                        {
                            foreach (Model.Shopping.MOBSHOPFlattenedFlight ff in trip.FlattenedFlights)
                            {
                                if (ff.Flights != null)
                                {
                                    foreach (Model.Shopping.MOBSHOPFlight flight in ff.Flights)
                                    {
                                        if (flight.OnTimePerformance != null && flight.OnTimePerformance.OnTimeNotAvailableMessage != null && flight.OnTimePerformance.OnTimeNotAvailableMessage.Count > 1)
                                        {
                                            List<string> lstOnTimeNotAvailableMessage = new List<string>();
                                            lstOnTimeNotAvailableMessage.Add(flight.OnTimePerformance.OnTimeNotAvailableMessage[0].ToString());
                                            flight.OnTimePerformance.OnTimeNotAvailableMessage = lstOnTimeNotAvailableMessage;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                response.IsTokenAuthenticated = session.IsTokenAuthenticated;
                if (response.Availability != null && response.Availability.Trip != null && _configuration.GetValue<bool>("HideSearchFiletersAndSort"))
                {
                    if (response.Availability.Trip.SearchFiltersIn != null) { response.Availability.Trip.SearchFiltersIn = null; }
                    if (response.Availability.Trip.SearchFiltersOut != null) { response.Availability.Trip.SearchFiltersOut = null; }
                }

                if (response != null && response.Availability != null && response.Availability.Reservation != null && session != null)
                {
                    response.Availability.Reservation.IsEmp20 = string.IsNullOrEmpty(session.EmployeeId) ? false : true;
                }

                if (isCFOPVersionCheck)
                {
                    if (_configuration.GetValue<bool>("IsBookingCommonFOPEnabled") && session.IsReshopChange == false)
                    {
                        response.ShoppingCart = PopulateShoppingCart(shoppingCart, FlowType.BOOKING.ToString(), selectTripRequest.SessionId, response.Availability.CartId, selectTripRequest, response.Availability.Reservation);
                        response.Availability.Reservation.IsBookingCommonFOPEnabled = _configuration.GetValue<bool>("IsBookingCommonFOPEnabled");
                    }
                    else if (_configuration.GetValue<bool>("IsReshopCommonFOPEnabled") && session.IsReshopChange == true)
                    {
                        response.ShoppingCart = PopulateShoppingCart(shoppingCart, FlowType.RESHOP.ToString(), selectTripRequest.SessionId, response.Availability.CartId);
                        response.Availability.Reservation.IsReshopCommonFOPEnabled = _configuration.GetValue<bool>("IsReshopCommonFOPEnabled");
                        Reservation reshopPathRes = new Reservation();
                        reshopPathRes = await _sessionHelperService.GetSession<Reservation>(Headers.ContextValues, reshopPathRes.ObjectName);
                        //reshopPathRes = United.Persist.FilePersist.Load<Reservation>(selectTripRequest.SessionId, reshopPathRes.ObjectName);
                        reshopPathRes.IsReshopCommonFOPEnabled = true;
                        //United.Persist.FilePersist.Save<United.Reservation>(selectTripRequest.SessionId, reshopPathRes.ObjectName, reshopPathRes);
                        await _sessionHelperService.SaveSession<Reservation>(reshopPathRes, Headers.ContextValues, reshopPathRes.ObjectName);
                    }
                }
                if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI") && GeneralHelper.IsApplicationVersionGreater(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, "AndroidShareTripInSoftRTIVersion", "iPhoneShareTripInSoftRTIVersion", "", "", true, _configuration))
                {
                    if (response.ShoppingCart == null)
                        response.ShoppingCart = new MOBShoppingCart();

                    //response.ShoppingCart.TripShare = _shoppingUtility.GetTripShare(response, shopping.LogEntries, traceSwitch, flightShopping, "SelectTrip");
                    response.ShoppingCart.TripShare = _shoppingUtility.IsShareTripValid(response);
                }
            }
            catch (UnitedException uaex)
            {
                #region
                ExceptionWrapper uaexWrapper = new ExceptionWrapper(uaex);
                //shopping.//logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, logAction, "UnitedException", request.Application.Id, request.Application.Version.Major, request.DeviceId, uaexWrapper, true, false));

                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = uaex.Code;
                }
                if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                {
                    if (uaex.InnerException != null && !string.IsNullOrEmpty(uaex.InnerException.Message) && uaex.InnerException.Message.Trim().Equals("10050"))
                    {
                        response.Exception.Code = "10050";
                    }
                }
                #endregion
            }
            catch (System.Exception ex)
            {
                #region
                ExceptionWrapper exceptionWrapper = new ExceptionWrapper(ex);
                //    shopping.//logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, logAction, "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, exceptionWrapper, true, false));
                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    if (selectTripRequest.BackButtonClick && selectTripRequest.Application.Version.Major == "2.1.9I" && ex.Message.ToLower().Trim() == "Object reference not set to an instance of an object.".ToLower().Trim())
                    {
                        string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4iOS2.1.9ITouchIDBackButtonBug") != null ? _configuration.GetValue<string>("BookingExceptionMessage4iOS2.1.9ITouchIDBackButtonBug") : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                        response.Exception = new MOBException("9999", expMessage);
                    }
                    else if (!GeneralHelper.IsVersion1Greater(selectTripRequest.Application.Version.Major, "2.1.8", true) && ex.Message.ToLower().Trim() == "Object reference not set to an instance of an object.".ToLower().Trim())
                    {
                        #region
                        bool isProductSelected = false;

                        if (isProductSelected)
                        {
                            string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_2") != null ? _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_2") : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                            response.Exception = new MOBException("9999", expMessage);
                        }
                        else
                        {
                            string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1") != null ? _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1") : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                            response.Exception = new MOBException("9999", expMessage);
                        }
                        #endregion
                    }
                    else if (!GeneralHelper.IsVersion1Greater(selectTripRequest.Application.Version.Major, "2.1.8", true) &&
                    ex.Message.ToLower().Replace("\r\n", string.Empty).Trim() == "index was out of range. must be non-negative and less than the size of the collection.parameter name: index".ToLower().Trim())
                    {
                        string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1") != null ? _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1") : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                        response.Exception = new MOBException("9999", expMessage);
                    }
                    else
                    {
                        response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                    }
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
                #endregion
            }


            //stopwatch.Stop();
            //response.CallDuration = stopwatch.ElapsedMilliseconds;


            if (response.Availability != null && response.Availability.Reservation != null)
            {
                Reservation bookingPathReservation = new Reservation();
                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(Headers.ContextValues, bookingPathReservation.ObjectName);
                //bookingPathReservation = United.Persist.FilePersist.Load<Reservation>(request.SessionId, bookingPathReservation.ObjectName, false);
                if (bookingPathReservation != null)
                {
                    response.Availability.Reservation = _shoppingUtility.GetReservationFromPersist(response.Availability.Reservation, selectTripRequest.SessionId).Result; //##Price Break Down - Kirti
                    if (response.Availability.Reservation.NumberOfTravelers == 1)
                    {
                        bookingPathReservation.GetALLSavedTravelers = false;
                        //United.Persist.FilePersist.Save<Reservation>(request.SessionId, bookingPathReservation.ObjectName, bookingPathReservation); //commented as its saved below //FOP Options Fix Venkat 12/08

                        if (response.Availability.Reservation.IsEmp20 || (response.Availability.Reservation.ShopReservationInfo != null && response.Availability.Reservation.ShopReservationInfo.IsCorporateBooking))
                        {
                            response.Availability.Reservation.GetALLSavedTravelers = true;
                        }
                        else
                        {
                            response.Availability.Reservation.GetALLSavedTravelers = bookingPathReservation.GetALLSavedTravelers;
                        }
                    }
                    response.Availability.Reservation.FOPOptions = _shoppingUtility.GetFopOptions(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);//FOP Options Fix Venkat 12/08
                    if (session != null && session.IsReshopChange && response.Availability.Reservation.FOPOptions != null && response.Availability.Reservation.FOPOptions.Count > 0)
                    {
                        response.Availability.Reservation.FOPOptions.RemoveAll(p => p.Key.ToUpper().Contains("PAYPAL"));
                    }
                    bookingPathReservation.FOPOptions = response.Availability.Reservation.FOPOptions;//FOP Options Fix Venkat 12/08
                    bookingPathReservation.TCDAdvisoryMessages = response.Availability.Reservation.TCDAdvisoryMessages; //Nizam - 01/22/2018 - Assigning the message to object to save it in Reservation persist file.
                    bookingPathReservation.IsBookingCommonFOPEnabled = response.Availability.Reservation.IsBookingCommonFOPEnabled;
                    bookingPathReservation.IsReshopCommonFOPEnabled = response.Availability.Reservation.IsReshopCommonFOPEnabled;
                    //United.Persist.FilePersist.Save<Reservation>(request.SessionId, bookingPathReservation.ObjectName, bookingPathReservation);//FOP Options Fix Venkat 12/08
                    await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, Headers.ContextValues, bookingPathReservation.ObjectName);

                }

                if (session.IsReshopChange && response.Availability.Reservation.IsCubaTravel)
                {
                    if (_shoppingUtility.EnableReshopCubaTravelReasonVersion(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                    {
                        string cubaTravelerMsg = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage") as string;
                        response.Availability.Reservation.TravelersCSL.ForEach(x => x.Message = cubaTravelerMsg);
                        response.Availability.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL
                            = response.Availability.Reservation.TravelersCSL;
                    }
                }
            }
            if (isCFOPVersionCheck)
            {
                if ((!_configuration.GetValue<bool>("DisableSelectTripSessionNullCheckFix") && session != null) || _configuration.GetValue<bool>("DisableSelectTripSessionNullCheckFix"))
                {
                    // Added for CFOP integration in RESHOP flow
                    if (_configuration.GetValue<bool>("IsReshopCommonFOPEnabled") && session.IsReshopChange)
                    {
                        try
                        {
                            shoppingCart = response.ShoppingCart;
                            if (shoppingCart != null)
                            {
                                ReservationToShoppingCart_DataMigration(response.Availability.Reservation, ref shoppingCart, selectTripRequest);
                                response.ShoppingCart = shoppingCart;
                                await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, Headers.ContextValues, shoppingCart.GetType().FullName);
                                //United.Persist.FilePersist.Save<MOBShoppingCart>(session.SessionId, shoppingCart.GetType().ToString(), shoppingCart);
                                response.EligibleFormofPayments = GetEligibleFormofPayments(selectTripRequest, session, shoppingCart, session.CartId, "RESHOP", ref isDefault, response.Availability.Reservation);
                                response.IsDefaultPaymentOption = isDefault;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            ExceptionWrapper exceptionWrapper = new ExceptionWrapper(ex);
                            //shopping.//logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, "ShoppingCartGetProductsFailed", "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, exceptionWrapper, true, false));
                        }
                    }
                }
            }

            //_shoppingUtility.HeaderAndContentLog(selectTripRequest.SessionId, logAction, selectTripRequest.Application, selectTripRequest.DeviceId, Request);

            try
            {
                //if (_configuration.GetValue<string>("CartIdForDebug").ToString().ToUpper() == "YES" || request.DeviceId.Trim().ToUpper() == "THIS_IS_FOR_TEST_" + DateTime.Now.ToString("MMM-dd-yyyy").ToUpper().Trim())
                if (_configuration.GetValue<string>("CartIdForDebug")?.ToUpper() == "YES" || (_configuration.GetValue<string>("DeviceIDSToReturnShopCallDurations").ToUpper().Trim().Split('|').Contains(selectTripRequest.DeviceId.ToUpper().Trim())))//request.DeviceId.Trim().ToUpper() == "THIS_IS_FOR_TEST_" + DateTime.Now.ToString("MMM-dd-yyyy").ToUpper().Trim())
                {
                    //response.CartId = response.CartId + "| Call Duration = " + response.CallDuration;
                    response.CartId = response.Availability?.Trip?.CallDurationText + "|" + response.CallDuration;
                    response.Availability.Trip.FlattenedFlights[0].Flights[0].OperatingCarrierDescription = response.CartId;                    
                }
                else if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "REST3_YES")
                {
                    response.CartId = response.Availability.CartId;
                }
                else if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "REST3_YES_1")
                {
                    response.CartId = response.Availability.CartId;
                    response.Availability.Trip.FlattenedFlights[0].Flights[0].OperatingCarrierDescription = response.CartId;
                }
                if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "REST_TUNING_CALL_DURATION")
                {
                    response.CartId = response.Availability.CallDuration + "|REST Total = " + (response.CallDuration / (double)1000).ToString();
                }
                if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "REST_TUNING_CALL_DURATION_WITH_CARTID")
                {
                    response.CartId = response.Availability.CallDuration + "|REST Total = " + (response.CallDuration / (double)1000).ToString() + "|Cart ID = " + response.Availability.CartId;
                }
                if (_configuration.GetValue<string>("Log_CSL_Call_Statistics").ToUpper().Trim() == "TRUE")
                {
                    try
                    {
                        //Utility.AddCSLCallStatistics(request.Application.Id, request.Application.Version.Major, request.DeviceId, request.SessionId, response.Availability.Trip.Origin, response.Availability.Trip.Destination, response.Availability.Trip.DepartDate, request.SearchType, response.Availability.Trip.Cabin, request.FareType, request.AwardTravel, request.NumberOfAdults, request.MileagePlusAccountNumber, string.Empty);
                        //Utility.AddCSLCallStatisticsDetails(string.Empty, "REST_Shopping", response.Availability.CartId, response.Availability.CallDuration + "|REST Total = " + (response.CallDuration / (double)1000).ToString(), "SelectTrip", request.SessionId);
                        //Utility.AddCSLStatistics(request.Application.Id, request.Application.Version.Major, request.DeviceId, request.SessionId, response.Availability.Trip.Origin, response.Availability.Trip.Destination, response.Availability.Trip.DepartDate, string.Empty, response.Availability.Trip.Cabin, string.Empty, response.Availability.Reservation.AwardTravel, 0, string.Empty, string.Empty, "REST_Shopping", response.Availability.CartId, response.Availability.CallDuration + "|REST Total = " + (response.CallDuration / (double)1000).ToString(), "Shopping/SelectTrip");
                        //string callDurations = _shoppingUtility.GetCSLCallDuration(shopping.LogEntries);
                        string origin = string.Empty, dest = string.Empty, cabin = string.Empty, flightdate = string.Empty;
                        // TFS- 64834 - Added below Not null check for availability property to overcome nullreference exception in the selecttrip - Vijayan
                        if (response.Availability != null)
                        {
                            if (response.Availability.Trip != null)
                            {
                                origin = response.Availability.Trip.Origin != null ? response.Availability.Trip.Origin : string.Empty;
                                dest = response.Availability.Trip.Destination != null ? response.Availability.Trip.Destination : string.Empty;
                                cabin = response.Availability.Trip.Cabin;
                                flightdate = response.Availability.Trip.DepartDate;
                                //_shoppingUtility.AddCSLStatistics(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, selectTripRequest.SessionId, origin, dest, flightdate, string.Empty, cabin, string.Empty, response.Availability.Reservation.AwardTravel, 0, string.Empty, string.Empty, "REST_Shopping", response.Availability.CartId, "Server:" + response.MachineName + "||" + callDurations, "Shopping/SelectTrip");
                            }
                            else
                            {
                                //_shoppingUtility.AddCSLCallStatisticsDetails(string.Empty, "REST_Shopping", response.Availability.CartId, "Server:" + response.MachineName + "||" + callDurations, "Shopping/SelectTrip", selectTripRequest.SessionId);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return response;
        }

        private bool IsLatestClientAppVerWithSelectTripBackButtonFix(int applicationID, string appVersion)
        {
            #region Priya Code for version check
            bool ValidVersion = false;
            string androidVersion = _configuration.GetValue<string>("AndroidIsLatestClientAppVerWithSelectTripBackButtonFix");
            string iPhoneVersion = _configuration.GetValue<string>("iPhoneIsLatestClientAppVerWithSelectTripBackButtonFix");
            string mWebVersion = _configuration.GetValue<string>("MWebIsLatestClientAppVerWithSelectTripBackButtonFix");

            Regex regex = new Regex("[0-9.]");
            appVersion = string.Join("", regex.Matches(appVersion).Cast<Match>().Select(match => match.Value).ToArray());

            if (applicationID == 1 && appVersion != iPhoneVersion)
            {
                ValidVersion = GeneralHelper.IsVersion1Greater(appVersion, iPhoneVersion);
            }
            else if (applicationID == 2 && appVersion != androidVersion)
            {
                ValidVersion = GeneralHelper.IsVersion1Greater(appVersion, androidVersion);
            }
            else if (applicationID == 16 && appVersion != mWebVersion)
            {
                ValidVersion = GeneralHelper.IsVersion1Greater(appVersion, mWebVersion);
            }
            #endregion
            return ValidVersion;
        }

        private async Task<MOBSHOPAvailability> SelectTrip(SelectTripRequest selectRequest, int totalPassengers)
        {
            Session session = new Session();
            //session = Persist.FilePersist.Load<Session>(selectRequest.SessionId, session.ObjectName);
            session = await _sessionHelperService.GetSession<Session>(Headers.ContextValues.SessionId, (new Session()).ObjectName, new List<string>() { Headers.ContextValues.SessionId, (new Session()).ObjectName });
            string logAction = session.IsReshopChange ? "ReShopSelectTrip" : "SelectTrip";
            string cslEndpoint = GetCslEndpointForShopping(session.IsReshopChange);
            bool isShop = false;
            MOBSHOPAvailability availability = null;
            //int count = 0;
            FareWheelHelper fareWheelHelper = new FareWheelHelper(_configuration, _logger, _headers, _sessionHelperService, _shoppingUtility);
            United.Services.FlightShopping.Common.ShopSelectRequest request = await fareWheelHelper.GetShopSelectRequest(selectRequest);
            //request.FareWheelDateChange = "";

            string jsonRequest = JsonConvert.SerializeObject(request), shopCSLCallDurations = string.Empty;
            United.Services.FlightShopping.Common.ShopSelectRequest calendarRequest = fareWheelHelper.GetShopSelectFareWheelRequest(request);

            //calendarRequest.FareWheelOnly = true;
            //calendarRequest.CalendarDateChange = "";
            string calendarJsonRequest = JsonConvert.SerializeObject(calendarRequest);

            ////logEntries.Add(LogEntry.GetLogEntry<string>(selectRequest.SessionId, logAction + " - Request for ShopSelect", "Trace", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, jsonRequest));

            //string url = string.Format("{0}/{1}", cslEndpoint, session.IsReshopChange ? "ReShop/Select" : "ShopSelect");

            ////logEntries.Add(LogEntry.GetLogEntry<string>(selectRequest.SessionId, logAction + " - Request url for ShopSelect", "Trace", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, url));

            if (session == null)
            {
                throw new UnitedException("Could not find your booking session.");
            }

            Reservation persistReservation = new Reservation();
            persistReservation = await _sessionHelperService.GetSession<Reservation>(Headers.ContextValues, persistReservation.ObjectName);


            ShoppingResponse persistShop = new ShoppingResponse();
            persistShop = await _sessionHelperService.GetSession<ShoppingResponse>(Headers.ContextValues, persistShop.ObjectName);
            //persistShop = Persist.FilePersist.Load<ShoppingResponse>(selectRequest.SessionId, persistShop.ObjectName);
            if (persistShop == null)
            {
                throw new UnitedException("Could not find your booking session.");
            }
            //string response = string.Empty;
            string calendarJsonResponse = string.Empty;
            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            string callTime4Tuning = string.Empty;
            Stopwatch cslSelectTripWatch;
            cslSelectTripWatch = new Stopwatch();
            cslSelectTripWatch.Reset();
            cslSelectTripWatch.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            string token = string.IsNullOrEmpty(session.Token) ? await _dPService.GetAnonymousToken(Headers.ContextValues.Application.Id, Headers.ContextValues.DeviceId, _configuration) : session.Token;
            var response = await _flightShoppingService.SelectTrip<United.Services.FlightShopping.Common.ShopResponse>(token, Headers.ContextValues.SessionId, "ShopSelect", jsonRequest);
            //jsonResponse = HttpHelper.Post(url, "application/json; charset=utf-8", session.Token, jsonRequest, httpPostTimeOut, httpPostNumberOfRetry);

            #region// 2 = selectTripCSLCallDurationstopwatch1//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cslSelectTripWatch.IsRunning)
            {
                cslSelectTripWatch.Stop();
            }
            shopCSLCallDurations = shopCSLCallDurations + "|2=" + cslSelectTripWatch.ElapsedMilliseconds.ToString() + "|"; // 2 = selectTripCSLCallDurationstopwatch1
            string cslCallDuration = "|CSL_Select_Trip =" + (cslSelectTripWatch.ElapsedMilliseconds / (double)1000).ToString(); //CSL_Select_Trip= selectTripCSLCallDurationstopwatch1
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            ////logEntries.Add(LogEntry.GetLogEntry<string>(selectRequest.SessionId, logAction + " - Response for ShopSelect", "Trace", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, jsonResponse));
            ////logEntries.Add(LogEntry.GetLogEntry<string>(selectRequest.SessionId, logAction + " - CSL Call Duration", "CSS/CSL-CallDuration", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, "CSLShopSelect=" + (cslSelectTripWatch.ElapsedMilliseconds / (double)1000).ToString()));

            if (response != null)
            {
                //United.Services.FlightShopping.Common.ShopResponse response = JsonConvert.DeserializeObject<United.Services.FlightShopping.Common.ShopResponse>(jsonResponse);
                shopCSLCallDurations = shopCSLCallDurations + "|CSLShopSelect=" + response.CallTimeDomain;
                callTime4Tuning = "ITA = " + response.CallTimeBBX + callTime4Tuning;
                bool isLastTripSelected = false;

                CSLShopRequest shopRequest = new CSLShopRequest();
                try
                {
                    shopRequest = await _sessionHelperService.GetSession<CSLShopRequest>(Headers.ContextValues, shopRequest.ObjectName);
                    //shopRequest = United.Persist.FilePersist.Load<United.Persist.Definition.Shopping.CSLShopRequest>(selectRequest.SessionId, shopRequest.ObjectName);
                }
                catch (System.Exception) { }

                United.Services.FlightShopping.Common.ShopResponse calendarResponse = JsonConvert.DeserializeObject<United.Services.FlightShopping.Common.ShopResponse>(calendarJsonResponse);
                /// Bug 216007 : mApp: First segment FSR displayed for the third segment in the Multi Trip Booking, observed only first time in the fresh installation (Sporadic issue)
                /// Srini - 28/08/2017 CB
                bool bErrorcheckflag = true; // if any errors found in response then this flag will be false. 
                if (_configuration.GetValue<bool>("BugFixToggleFor17M"))
                {
                    if (response != null && response.Errors != null)
                    {
                        bErrorcheckflag = NoCSLExceptions(response.Errors);
                    }
                }

                if (response != null && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success) && (bErrorcheckflag))
                {
                    availability = new MOBSHOPAvailability();
                    availability.SessionId = selectRequest.SessionId;
                    availability.CartId = response.CartId;
                    if (!string.IsNullOrEmpty(session.EmployeeId))
                    {
                        availability.UaDiscount = _configuration.GetValue<string>("UADiscount");
                    }
                    availability.AwardTravel = persistShop.Request.AwardTravel;

                    availability.Reservation = new MOBSHOPReservation();

                    //-------Feature 208204--- Common class data carrier for hirarchy methds-----
                    MOBSHOPDataCarrier _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
                    _mOBSHOPDataCarrier.SearchType = persistShop.Request.SearchType;
                    if (_configuration.GetValue<bool>("Shopping - bPricingBySlice"))
                    {
                        if (!availability.AwardTravel && !session.IsReshopChange)
                        {
                            //availability.DisablePricingBySlice = _configuration.GetValue<bool>("Shopping - bPricingBySlice");
                            availability.PriceTextDescription = GetPriceTextDescription(persistShop.Request.SearchType);
                            //availability.FSRFareDescription = GetFSRFareDescription(persistShop.Request);
                            SetFSRFareDescriptionForShop(availability, persistShop.Request);
                            _mOBSHOPDataCarrier.PriceFormText = GetPriceFromText(persistShop.Request.SearchType);
                        }
                        else
                        {
                            SetSortDisclaimerForReshop(availability, persistShop.Request);
                        }
                    }
                    var isStandardRevenueSearch = IsStandardRevenueSearch(session.IsCorporateBooking, session.IsYoungAdult, availability.AwardTravel,
                                                                         session.EmployeeId, session.TravelType, session.IsReshopChange,
                                                                         persistShop.Request.FareClass, persistShop.Request.PromotionCode);
                    if (response.Trips != null && response.Trips.Count > 0)
                    {
                        bool readFromShop = false;

                        availability.Trip = null;
                        availability.Reservation.Trips = null;

                        int partiallyUsed = 0;
                        for (int i = 0; i < response.Trips.Count; i++)
                        {
                            ///95509 - iOS/Android-Farewheel price is differ from lowest price for OW /RT/MT on Business cabin search
                            ///224324 - mApp : Booking –Revenue- Multitrip – Economy/First- Fare wheel in FSR2 is showing prices based on FSR1 cabin search and not on the FSR2 cabin search
                            ///Srini - 03/20/2018
                            if (_configuration.GetValue<bool>("BugFixToggleFor18C"))
                            {
                                _mOBSHOPDataCarrier.FsrMinPrice = 0M;
                            }

                            if (response.Trips[i] != null && (!string.IsNullOrEmpty(response.Trips[i].BBXCellIdSelected) || (session.IsReshopChange && i < response.LastTripIndexRequested - 1 && response.Trips[i].ChangeType != 3)) && response.Trips[i].Flights.Count == 1)
                            {
                                if (availability.Reservation == null)
                                    availability.Reservation = new MOBSHOPReservation();
                                if (availability.Reservation.Trips == null)
                                    availability.Reservation.Trips = new List<MOBSHOPTrip>();
                                if ((_configuration.GetValue<string>("CheckCSLShopSelectFlightsNull") == null || _configuration.GetValue<bool>("CheckCSLShopSelectFlightsNull")) && (response.Trips[i].Flights == null || response.Trips[i].Flights.Count == 0))
                                {
                                    //To ByPass this Flight Null Check if have any issues after weekly releaase 10/27 just add this entry to production web config =  <add key="CheckCSLShopSelectFlightsNull" value="false"/>
                                    // To turn on check Flight Nulls delete the entry in web config or update the value to true <add key="CheckCSLShopSelectFlightsNull" value="true"/>
                                    string actionName = session.IsReshopChange ? "CSL ReShop SelectTrip – Flights Null" : "CSL Shop Select – Flights Null";
                                    if (shopRequest != null && shopRequest.ShopRequest != null && shopRequest.ShopRequest.AwardTravel)
                                    {
                                        actionName = actionName + " - Award Search";
                                    }
                                    ////logEntries.Add(LogEntry.GetLogEntry<string>(selectRequest.SessionId, actionName, "Trace", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, jsonRequest));
                                    //61048 - Bug 331484:FS (Mobile) Item 29: Flight Shopping (Mobile): Unhandled Exception ArgumentOutofRangeException - 2.1.9 - Flights Object Null
                                    throw new UnitedException(_configuration.GetValue<string>("NoAvailabilityError2.0"));
                                }
                                if (!_configuration.GetValue<bool>("EnableNonStopFlight"))
                                {
                                    availability.Reservation.Trips.Add(PopulateTrip(_mOBSHOPDataCarrier, response.CartId, response.Trips, i,
                                        persistShop.Response.Availability.Trip.SearchFiltersIn.FareFamily,
                                        selectRequest.SessionId, selectRequest.TripId, selectRequest.Application.Id,
                                        selectRequest.DeviceId, selectRequest.Application.Version.Major,
                                        persistShop.Request.ShowMileageDetails, persistShop.Request.PremierStatusLevel, isStandardRevenueSearch,
                                        availability.AwardTravel));
                                }
                                else
                                {
                                    ///208852 : Booking - FSR - PROD Basic Economy mApps Lowest Basic Economy fare is displayed. (Basic Economy switch is off) 
                                    ///Srini - 11/27/2017
                                    availability.Reservation.Trips.Add(PopulateTrip(_mOBSHOPDataCarrier, response.CartId, response.Trips, i,
                                        persistShop.Response.Availability.Trip.SearchFiltersIn.FareFamily,
                                        selectRequest.SessionId, selectRequest.TripId, selectRequest.Application.Id,
                                        selectRequest.DeviceId, selectRequest.Application.Version.Major,
                                        persistShop.Request.ShowMileageDetails, persistShop.Request.PremierStatusLevel, isStandardRevenueSearch,
                                        availability.AwardTravel, (_configuration.GetValue<bool>("BugFixToggleFor17M") ? session.IsBEFareDisplayAtFSR : true), selectRequest.GetNonStopFlightsOnly, selectRequest.GetFlightsWithStops, persistShop?.Request));
                                }
                            }
                            else if ((response.Trips[i] != null && !session.IsReshopChange) || (session.IsReshopChange && (i + 1) == response.LastTripIndexRequested))//assume this is the trips for selection
                            {
                                if (_configuration.GetValue<bool>("CheckCSLShopSelectFlightsNull") && (response.Trips[i].Flights == null || response.Trips[i].Flights.Count == 0) && response.LastTripIndexRequested == i + 1)
                                {
                                    //To ByPass this Flight Null Check if have any issues after weekly releaase 10/27 just add this entry to production web config =  <add key="CheckCSLShopSelectFlightsNull" value="false"/>
                                    // To turn on check Flight Nulls delete the entry in web config or update the value to true <add key="CheckCSLShopSelectFlightsNull" value="true"/>
                                    string actionName = session.IsReshopChange ? "CSL ReShop SelectTrip – Flights Null" : "CSL Shop Select – Flights Null";
                                    if (shopRequest != null && shopRequest.ShopRequest != null && shopRequest.ShopRequest.AwardTravel)
                                    {
                                        actionName = actionName + " - Award Search";
                                    }
                                    ////logEntries.Add(LogEntry.GetLogEntry<string>(selectRequest.SessionId, actionName, "Trace", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, jsonRequest));
                                    //61048 - Bug 331484:FS (Mobile) Item 29: Flight Shopping (Mobile): Unhandled Exception ArgumentOutofRangeException - 2.1.9 - Flights Object Null
                                    throw new UnitedException(_configuration.GetValue<string>("NoAvailabilityError2.0"));
                                }
                                if (availability.Trip == null)
                                {
                                    if (!_configuration.GetValue<bool>("EnableNonStopFlight"))
                                    {
                                        availability.Trip = PopulateTrip(_mOBSHOPDataCarrier, response.CartId, response.Trips, i,
                                            persistShop.Response.Availability.Trip.SearchFiltersIn.FareFamily,
                                            selectRequest.SessionId, selectRequest.TripId, selectRequest.Application.Id,
                                            selectRequest.DeviceId, selectRequest.Application.Version.Major,
                                            persistShop.Request.ShowMileageDetails,
                                            persistShop.Request.PremierStatusLevel, availability.AwardTravel);
                                        partiallyUsed++;
                                    }
                                    else
                                    {
                                        ///208852 : Booking - FSR - PROD Basic Economy mApps Lowest Basic Economy fare is displayed. (Basic Economy switch is off) 
                                        ///Srini - 11/27/2017
                                        availability.Reservation.Trips.Add(PopulateTrip(_mOBSHOPDataCarrier, response.CartId, response.Trips, i,
                                        persistShop.Response.Availability.Trip.SearchFiltersIn.FareFamily,
                                        selectRequest.SessionId, selectRequest.TripId, selectRequest.Application.Id,
                                        selectRequest.DeviceId, selectRequest.Application.Version.Major,
                                        persistShop.Request.ShowMileageDetails, persistShop.Request.PremierStatusLevel, isStandardRevenueSearch,
                                        availability.AwardTravel, (_configuration.GetValue<bool>("BugFixToggleFor17M") ? session.IsBEFareDisplayAtFSR : true), selectRequest.GetNonStopFlightsOnly, selectRequest.GetFlightsWithStops, persistShop?.Request));
                                    }
                                }
                                availability.AwardCalendar = PopulateAwardCalendar(response.Calendar, response.LastBBXSolutionSetId, request.BBXCellId);

                                #region Save Amenities Request to Persist
                                UpdateAmenitiesIndicatorsRequest amenitiesRequest = new UpdateAmenitiesIndicatorsRequest();
                                amenitiesRequest = GetAmenitiesRequest(response.CartId, response.Trips[i].Flights);
                                ShoppingExtend shopExtendDAL = new ShoppingExtend(_sessionHelperService);
                                await shopExtendDAL.AddAmenitiesRequestToPersist(amenitiesRequest, selectRequest.SessionId, response.LastTripIndexRequested.ToString());
                                #endregion
                            }

                            // For Reshop & partially used pnr
                            if (availability.Trip != null && session.IsReshopChange && persistReservation.Reshop.IsUsedPNR)
                            {
                                availability.Trip.Cabin = persistShop.Request.Trips[partiallyUsed].Cabin;
                                availability.Trip.LastTripIndexRequested = response.LastTripIndexRequested;
                            }
                            #region  //**NOTE**// Venkat - Nov 10,2014 For Oragainze Results
                            else if (availability.Trip != null) // Booking & Reshop
                            {
                                availability.Trip.Cabin = persistShop.Request.Trips[i].Cabin;
                                availability.Trip.LastTripIndexRequested = response.LastTripIndexRequested;
                            }
                            #endregion
                        }
                        availability.Reservation.SessionId = selectRequest.SessionId;
                        availability.Reservation.SearchType = persistShop.Request.SearchType;
                        if (_shoppingUtility.EnableTravelerTypes(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, session.IsReshopChange) && persistShop.Request.TravelerTypes != null && persistShop.Request.TravelerTypes.Count > 0)
                        {
                            availability.TravelerCount = ShopStaticUtility.GetTravelerCount(persistShop.Request.TravelerTypes);
                            availability.Reservation.NumberOfTravelers = availability.TravelerCount;

                            if (availability.Reservation.ShopReservationInfo2 == null)
                                availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();

                            availability.Reservation.ShopReservationInfo2.TravelerTypes = persistShop.Request.TravelerTypes;

                        }
                        else
                        {
                            availability.Reservation.NumberOfTravelers = persistShop.Request.NumberOfAdults + persistShop.Request.NumberOfChildren12To17 + persistShop.Request.NumberOfChildren2To4 + persistShop.Request.NumberOfChildren5To11 + persistShop.Request.NumberOfInfantOnLap + persistShop.Request.NumberOfInfantWithSeat + persistShop.Request.NumberOfSeniors;
                            availability.TravelerCount = availability.Reservation.NumberOfTravelers;
                        }


                        #region
                        SelectTrip selectTrip = new SelectTrip();
                        try
                        {
                            selectTrip = await _sessionHelperService.GetSession<SelectTrip>(Headers.ContextValues, selectTrip.ObjectName);
                            //selectTrip = United.Persist.FilePersist.Load<United.Persist.Definition.Shopping.SelectTrip>(selectRequest.SessionId, selectTrip.ObjectName, false);
                        }
                        catch (System.Exception ex) { }
                        #endregion

                        IsLastTripFSR(session.IsReshopChange, availability, response.Trips);

                        if (session.IsReshopChange &&
                            response.Trips.All(x => x.ChangeType != United.Services.FlightShopping.Common.Types.ChangeTypes.ChangeTrip) &&
                            response.Trips.Where(t => t.ChangeType == United.Services.FlightShopping.Common.Types.ChangeTypes.SelectedTrip).All(x => x.Selected))
                        {
                            isLastTripSelected = true;
                        }
                        else if (!session.IsReshopChange && IsLatestClientAppVerWithSelectTripBackButtonFix(selectRequest.Application.Id, selectRequest.Application.Version.Major))
                        {
                            if (_configuration.GetValue<bool>("EnableNonStopFlight"))
                            {
                                // Index Out Of Range Fix with Back Button and Is Product Selected flags implementation
                                if ((selectTrip != null && selectTrip.Requests.Count + 1 == session.ShopSearchTripCount && selectRequest.ISProductSelected && !selectRequest.GetFlightsWithStops) || session.ShopSearchTripCount == 1) //==> Newly Added Condition for Non Stop Flights Changes !selectRequest.GetFlightsWithStops means its second Select Trip Call.
                                {
                                    isLastTripSelected = true;
                                }
                            }
                            else
                            {
                                // Index Out Of Range Fix with Back Button and Is Product Selected flags implementation
                                if ((selectTrip != null && selectTrip.Requests.Count + 1 == session.ShopSearchTripCount && selectRequest.ISProductSelected) || session.ShopSearchTripCount == 1)
                                {
                                    isLastTripSelected = true;
                                }
                            }
                        }
                        else
                        {
                            // Old Code Before Index out of Range Exception Fix
                            if (!session.IsReshopChange && response.Trips[response.Trips.Count - 1] != null && response.Trips[response.Trips.Count - 1].BBXCellIdSelected != null && response.Trips[response.Trips.Count - 1].BBXCellIdSelected == selectRequest.ProductId)
                            {
                                isLastTripSelected = true;
                            }
                        }

                        bool isCorporateBooking = _configuration.GetValue<bool>("CorporateConcurBooking") && persistShop.Request.IsCorporateBooking;
                        if (!session.IsReshopChange && !isLastTripSelected)
                        {
                            SetTitleForFSRPage(availability, persistShop.Request);
                        }

                        if (isLastTripSelected)
                        {

                            MOBResReservation resReservation = new MOBResReservation();
                            if (_configuration.GetValue<bool>("EnableOmniChannelCartMVP1") && !session.IsReshopChange)
                            {
                                resReservation = GetShopBookingDetailsV2(_mOBSHOPDataCarrier, selectRequest.SessionId, request, ref availability, selectRequest, persistShop);
                            }
                            else
                            {
                                resReservation = GetShopBookingDetails(_mOBSHOPDataCarrier, selectRequest.SessionId, request, ref availability, selectRequest, persistShop);
                            }
                            #region


                            if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "REST_TUNING_CALL_DURATION")
                            {
                                cslCallDuration = cslCallDuration + "|CSL_Booking_Details=" + (resReservation.CallDuration / (double)1000).ToString();
                            }
                            #endregion

                            #region Save Unfinished Booking

                            if (_shoppingUtility.EnableUnfinishedBookings(selectRequest) && !_configuration.GetValue<bool>("EnableCCEServiceforGetUnfinishedBookings"))
                            {
                                try
                                {
                                    // Only save unfinished booking for regular revenue customer
                                    if (!session.IsAward && !session.IsReshopChange && !isCorporateBooking && string.IsNullOrEmpty(session.EmployeeId) && !IsYABooking(availability.Reservation))
                                    {
                                        if (session.CustomerID > 0) // for signed in customer only
                                        {
                                            System.Threading.Tasks.Task.Factory.StartNew(() => _unfinishedBooking.SaveAnUnfinishedBooking(session, selectRequest, MapUnfinishedBookingFromMOBSHOPReservation(availability.Reservation)));
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (session != null)
                                    {
                                        ////logEntries.Add(LogEntry.GetLogEntry(selectRequest.SessionId, "SelectTrip - SaveAnUnfinishedBooking", "Exception", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, new MOBExceptionWrapper(ex)));
                                    }
                                }
                            }

                            #endregion

                            #region SSR

                            if (_shoppingUtility.EnableSpecialNeeds(selectRequest.Application.Id, selectRequest.Application.Version.Major))
                            {
                                //testing, remove
                                try
                                {
                                    var persistedShopBookingDetailsResponse = new ShopBookingDetailsResponse();
                                    persistedShopBookingDetailsResponse = _sessionHelperService.GetSession<ShopBookingDetailsResponse>(Headers.ContextValues).Result;
                                    //persistedShopBookingDetailsResponse = await _sessionHelperService.GetSession<ShopBookingDetailsResponse>(Headers.ContextValues, persistedShopBookingDetailsResponse.ObjectName);
                                    //persistedShopBookingDetailsResponse = FilePersist.Load<ShopBookingDetailsResponse>(selectRequest.SessionId, persistedShopBookingDetailsResponse.GetType().FullName, logandThrowExceptionback: false);
                                    if (persistedShopBookingDetailsResponse != null)
                                    {
                                        // populate avail. special needs for the itinerary
                                        availability.Reservation.ShopReservationInfo2.SpecialNeeds = GetItineraryAvailableSpecialNeeds(session, selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, persistedShopBookingDetailsResponse.Reservation.FlightSegments, "en-US");

                                        // update persisted reservation object too
                                        var bookingPathReservation = new Reservation();
                                        bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(Headers.ContextValues, bookingPathReservation.ObjectName);
                                        //bookingPathReservation = FilePersist.Load<Reservation>(selectRequest.SessionId, bookingPathReservation.ObjectName);
                                        if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null)
                                        {
                                            bookingPathReservation.ShopReservationInfo2.SpecialNeeds = availability.Reservation.ShopReservationInfo2.SpecialNeeds;
                                            await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, Headers.ContextValues, bookingPathReservation.ObjectName);
                                            //FilePersist.Save(bookingPathReservation.SessionId, bookingPathReservation.ObjectName, bookingPathReservation);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "SelectTrip - GetItineraryAvailableSpecialNeeds", "Exception", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, new MOBExceptionWrapper(e)));
                                }
                            }

                            #endregion

                            //Build Shoppingcart products(i.e RES) only when if it is the last select trip
                            try
                            {

                                var version1 = _configuration.GetValue<string>("AndriodCFOP_Booking_Reshop_PostbookingAppVersion");
                                var version2 = _configuration.GetValue<string>("IphoneCFOP_Booking_Reshop_PostbookingAppVersion");
                                bool isCFOPVersionCheck = GeneralHelper.IsApplicationVersionGreaterorEqual(selectRequest.Application.Id, selectRequest.Application.Version.Major, version1, version2);
                                if (isCFOPVersionCheck)
                                {
                                    if (_configuration.GetValue<bool>("IsReshopCommonFOPEnabled") && session.IsReshopChange)
                                    {
                                        PopulateCFOPReshopProducts(session, selectRequest);//, LogEntries);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                                //logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(selectRequest.SessionId, "ShoppingCartGetProductsFailed", "Exception", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, exceptionWrapper, true, false));
                            }
                        }
                        if (availability.Reservation.Trips != null && availability.Reservation.Trips.Count > 0 && availability.Trip != null)
                        {
                            List<MOBSHOPTripBase> shopTrips = new List<MOBSHOPTripBase>();
                            shopTrips.Add(availability.Trip);
                            shopTrips.Add(availability.Reservation.Trips[availability.Reservation.Trips.Count - 1]);

                            if (shopRequest != null && shopRequest.ShopRequest != null && string.IsNullOrEmpty(shopRequest.ShopRequest.EmployeeDiscountId))
                            {
                                availability.FareWheel = PopulateFareWheelDates(shopTrips, "SELECTTRIP");
                            }
                        }
                        #region Corporate Booking

                        if (isCorporateBooking && persistShop.Request.mobCPCorporateDetails != null && !string.IsNullOrEmpty(persistShop.Request.mobCPCorporateDetails.CorporateCompanyName))
                        {
                            availability.CorporateDisclaimer = string.Format(persistShop.Request.mobCPCorporateDetails.CorporateCompanyName + " {0}", _configuration.GetValue<string>("CorporateDisclaimerText") ?? string.Empty);
                        }
                        #endregion

                        if (session.IsReshopChange)
                        {
                            Reservation bookingPathReservation = new Reservation();
                            //bookingPathReservation = United.Persist.FilePersist.Load<Reservation>(session.SessionId, bookingPathReservation.ObjectName, false);
                            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(Headers.ContextValues, bookingPathReservation.ObjectName);
                            bookingPathReservation.Trips = availability.Reservation.Trips;
                            bookingPathReservation.Reshop = availability.Reservation.Reshop;
                            if (bookingPathReservation.ShopReservationInfo2 == null)
                                bookingPathReservation.ShopReservationInfo2 = new ReservationInfo2();
                            bookingPathReservation.ShopReservationInfo2.NextViewName = "RTI";
                            await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, Headers.ContextValues, bookingPathReservation.ObjectName);
                            //FilePersist.Save<Reservation>(session.SessionId, bookingPathReservation.ObjectName, bookingPathReservation);
                        }

                        #region Award Dynamic Calendar

                        bool isAwardCalendarMP2017 = _configuration.GetValue<bool>("NGRPAwardCalendarMP2017Switch");
                        if (isAwardCalendarMP2017 && session.IsAward)
                        {
                            // Save ShoppingResponse in Persist
                            BuildAwardShopRequestAndSaveToPersist(response, selectRequest);

                        }
                        #endregion

                        #region Mileage Balance

                        if (_shoppingUtility.EnableMileageBalance(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major))
                        {
                            try
                            {
                                if (session.IsAward)
                                {
                                    Service.Presentation.CommonModel.Characteristic loyaltyId = response.Characteristics.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Code) && c.Code.Trim().Equals("LoyaltyId".Trim(), StringComparison.OrdinalIgnoreCase));
                                    if (loyaltyId != null && !string.IsNullOrWhiteSpace(loyaltyId.Value) && loyaltyId.Value.Equals(session.MileagPlusNumber))
                                    {
                                        Service.Presentation.CommonModel.Characteristic mileageBalance = response.Characteristics.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Code) && c.Code.Trim().Equals("MilesBalance".Trim(), StringComparison.OrdinalIgnoreCase));
                                        if (mileageBalance != null && !string.IsNullOrWhiteSpace(mileageBalance.Value))
                                        {
                                            availability.AvailableAwardMilesWithDesc = string.Format("Mileage balance: {0}", ShopStaticUtility.GetThousandPlaceCommaDelimitedNumberString(mileageBalance.Value));
                                        }
                                    }
                                    GetMilesDescWithFareDiscloser(availability, session, persistShop.Request.Experiments);
                                }
                            }
                            catch (Exception ex)
                            {
                                //logEntries.Add(LogEntry.GetLogEntry(shopRequest.SessionId, "SelectTrip - Assigning mileage plus balance", "Exception", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, new MOBExceptionWrapper(ex)));
                            }
                        }

                        #endregion

                        #region FSR Result Handler

                        if (_shoppingUtility.EnableFSRAlertMessages(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, session.TravelType))
                        {
                            try
                            {
                                if (!session.IsReshopChange)
                                {
                                    var shouldHandleFRS = false;

                                    if (_configuration.GetValue<bool>("EnableMetroCodeFixForMultiTrip"))
                                    {
                                        shouldHandleFRS = !isLastTripSelected && ("OW,RT,MD".IndexOf(persistShop.Request.SearchType.ToString()) > -1);
                                    }
                                    else
                                    {
                                        shouldHandleFRS = !isLastTripSelected && ("OW,RT".IndexOf(persistShop.Request.SearchType.ToString()) > -1);
                                    }

                                    // Corporate Check
                                    if (shouldHandleFRS)
                                    {
                                        availability.FSRAlertMessages = HandleFlightShoppingThatHasResults(response, persistShop.Request, isShop);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                //logEntries.Add(LogEntry.GetLogEntry(shopRequest.SessionId, "SelectTrip - HandleFlightShoppingThatHasResults", "Exception", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, new MOBExceptionWrapper(ex)));
                            }
                        }

                        #endregion

                        availability.FSRAlertMessages = AddMandatoryFSRAlertMessages(persistShop.Request, availability.FSRAlertMessages);
                        if (_configuration.GetValue<bool>("EnableCorporateLeisure") && session.TravelType == TravelType.CLB.ToString())
                        {
                            if (availability.FSRAlertMessages == null)
                            {
                                availability.FSRAlertMessages = new List<MOBFSRAlertMessage>();
                            }
                            var corporateLeisureOptOutAlert = GetCorporateLeisureOptOutFSRAlert(persistShop.Request, session);
                            if (corporateLeisureOptOutAlert != null)
                            {
                                availability.FSRAlertMessages.Add(corporateLeisureOptOutAlert);
                            }
                        }

                    }

                }
                else
                {
                    #region FSR No Result Handler

                    List<MOBFSRAlertMessage> alertMessages = null;

                    // Only handle this there is mp flights found but no due to CSL FS service is not downn
                    // MajorCode="20003.01"; MinorCode="10038"; Message="FLIGHTS NOT FOUND"
                    if (response != null && response.Errors != null && response.Errors.Exists(p => p.MajorCode == "20003.01" && p.MinorCode == "10038"))
                    {
                        AwardNoFlightExceptionMsg(persistShop.Request);
                        if (_shoppingUtility.EnableFSRAlertMessages(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, session.TravelType))
                        {
                            try
                            {
                                if (!session.IsReshopChange && _shoppingUtility.EnableFSRAlertMessages(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, session.TravelType))
                                {
                                    var shouldHandleFRS = false;

                                    if (_configuration.GetValue<bool>("EnableMetroCodeFixForMultiTrip"))
                                    {
                                        shouldHandleFRS = !isLastTripSelected && ("OW,RT,MD".IndexOf(persistShop.Request.SearchType.ToString()) > -1);
                                    }
                                    else
                                    {
                                        shouldHandleFRS = !isLastTripSelected && ("OW,RT".IndexOf(persistShop.Request.SearchType.ToString()) > -1);
                                    }

                                    // Corporate Check
                                    if (shouldHandleFRS)
                                    {
                                        alertMessages = HandleFlightShoppingThatHasNoResults(response, persistShop.Request, isShop);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                //logEntries.Add(LogEntry.GetLogEntry(shopRequest.SessionId, "SelectTrip - HandleFlightShoppingThatHasNoResults", "Exception", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, new MOBExceptionWrapper(ex)));
                            }
                        }
                    }

                    #endregion

                    if (!_shoppingUtility.EnableFSRAlertMessages(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, session.TravelType) || alertMessages == null || !alertMessages.Any())
                    {
                        if (response.Errors != null && response.Errors.Count > 0)
                        {
                            string errorMessage = string.Empty;
                            foreach (var error in response.Errors)
                            {
                                errorMessage = errorMessage + " " + error.Message;
                                //Bug 56109:ShopSelect: System.Exception - Object reference not set to an instance of an object - Ravitheja - Sep 14, 2016
                                if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10046"))
                                    throw new UnitedException(_configuration.GetValue<string>("BookingExceptionMessage_ServiceErrorSessionNotFound").ToString());

                                // Added By Ali as part of Task 264624 : Select Trip - The Boombox user's session has expired
                                if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10036"))
                                {
                                    throw new UnitedException(_configuration.GetValue<string>("BookingExceptionMessage_ServiceErrorSessionNotFound").ToString());
                                }

                                //  Added by Ali as part of Task 278032 : System.Exception:FLIGHTS NOT FOUND-No flight options were found for this trip.
                                if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10051"))
                                {
                                    throw new UnitedException(_configuration.GetValue<string>("NoAvailabilityError").ToString());
                                }
                            }

                            throw new System.Exception(errorMessage);
                        }
                    }
                    else
                    {
                        if (_shoppingUtility.EnableFSRAlertMessages(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, session.TravelType))
                        {
                            availability = new MOBSHOPAvailability { FSRAlertMessages = alertMessages };

                            // Offer the customer other search options due to no flights available.
                            //logEntries.Add(LogEntry.GetLogEntry(shopRequest.SessionId, "SelectTrip - HandleFlightShoppingThatHasNoResults", "UnitedException", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, "Offer the customer other search options due to no flights available."));
                        }
                    }
                }
            }
            else
            {
                throw new UnitedException("The service did not return any availability.");
            }
            if (availability.Trip != null)
            {
                AddAvailabilityToPersist(availability, selectRequest.SessionId);
            }

            try
            {
                if (_configuration.GetValue<string>("CartIdForDebug").ToString().ToUpper() == "REST_TUNING_CALL_DURATION")
                {
                    availability.CallDuration = callTime4Tuning + cslCallDuration;
                }
            }
            catch { }


            return availability;
        }

        private void AwardMTThrowErrorIfCurrentTripDepartDateGreaterThanNextTripDepartDate(SelectTripRequest request, SelectTrip selectTrip, Session session)
        {
            if (_configuration.GetValue<bool>("BugFixToggleFor17M") && selectTrip != null && session.IsAward)
            {
                CSLShopRequest cslShopRequest = new CSLShopRequest();

                cslShopRequest = _sessionHelperService.GetSession<CSLShopRequest>(Headers.ContextValues, cslShopRequest.ObjectName).Result;
                //United.Persist.FilePersist.Load<United.Persist.Definition.Shopping.CSLShopRequest>(session.SessionId, cslShopRequest.ObjectName);
                if (cslShopRequest == null)
                {
                    throw new UnitedException("Could not find your booking session.");
                }
                United.Services.FlightShopping.Common.ShopRequest shopRequest = cslShopRequest.ShopRequest;
                if (selectTrip.Responses.Count < shopRequest.Trips.Count && shopRequest.SearchTypeSelection == United.Services.FlightShopping.Common.SearchType.MultipleDestination)
                {
                    if (_configuration.GetValue<string>("AwardCalenderMessageIfSelectedDateBeyondNextTripDepartDate") != null &&
                        !string.IsNullOrEmpty(request.CalendarDateChange) &&
                        (
                         DateTime.Parse(request.CalendarDateChange) > DateTime.Parse(shopRequest.Trips[selectTrip.Responses.Count + 1].DepartDate) ||
                         (CheckPreviousSelectedTripDepartDate(request.CalendarDateChange, selectTrip))
                        ))
                    {
                        throw new UnitedException(_configuration.GetValue<string>("AwardCalenderMessageIfSelectedDateBeyondNextTripDepartDate").ToString());
                    }
                    else if ((string.IsNullOrEmpty(request.ProductId) || string.IsNullOrEmpty(request.TripId)) &&
                            selectTrip.Requests != null && selectTrip.Requests.Count > 0)
                    {
                        if (string.IsNullOrEmpty(request.ProductId))
                        {
                            request.ProductId = selectTrip.Requests[selectTrip.Requests.Keys.ElementAt(selectTrip.Requests.Count - 1)].ProductId;
                        }
                        if (string.IsNullOrEmpty(request.TripId))
                        {
                            request.TripId = selectTrip.Requests[selectTrip.Requests.Keys.ElementAt(selectTrip.Requests.Count - 1)].TripId;
                        }
                    }
                }
            }
        }

        private void ReservationToShoppingCart_DataMigration(MOBSHOPReservation reservation, ref MOBShoppingCart shoppingCart, MOBRequest request)
        {
            try
            {
                bool isETCCertificatesExistInShoppingCartPersist = (_configuration.GetValue<bool>("MTETCToggle") &&
                                                                    shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates != null &&
                                                                    shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates.Count > 0);

                if (shoppingCart == null)
                    shoppingCart = new MOBShoppingCart();
                var formOfPaymentDetails = new Model.Shopping.FormofPayment.MOBFormofPaymentDetails();
                shoppingCart.CartId = reservation.CartId;
                shoppingCart.PointofSale = reservation.PointOfSale;
                if (_configuration.GetValue<bool>("MTETCToggle"))
                    shoppingCart.IsMultipleTravelerEtcFeatureClientToggleEnabled = reservation.ShopReservationInfo2 != null ? reservation.ShopReservationInfo2.IsMultipleTravelerEtcFeatureClientToggleEnabled : false;
                formOfPaymentDetails.FormOfPaymentType = reservation.FormOfPaymentType.ToString();
                formOfPaymentDetails.PayPal = reservation.PayPal;
                formOfPaymentDetails.PayPalPayor = reservation.PayPalPayor;
                formOfPaymentDetails.MasterPassSessionDetails = reservation.MasterpassSessionDetails;
                formOfPaymentDetails.masterPass = reservation.Masterpass;
                formOfPaymentDetails.Uplift = reservation.ShopReservationInfo2?.Uplift;
                shoppingCart.SCTravelers = (reservation.TravelersCSL != null && reservation.TravelersCSL.Count() > 0) ? reservation.TravelersCSL : null;
                if (shoppingCart.SCTravelers != null && shoppingCart.SCTravelers.Any())
                {
                    shoppingCart.SCTravelers[0].SelectedSpecialNeeds = (reservation.TravelersCSL != null && reservation.TravelersCSL.Count() > 0) ? reservation.TravelersCSL[0].SelectedSpecialNeeds : null;
                    shoppingCart.SCTravelers[0].SelectedSpecialNeedMessages = (reservation.TravelersCSL != null && reservation.TravelersCSL.Count() > 0) ? reservation.TravelersCSL[0].SelectedSpecialNeedMessages : null;
                }
                if (shoppingCart.FormofPaymentDetails != null && shoppingCart.FormofPaymentDetails.SecondaryCreditCard != null)
                {
                    formOfPaymentDetails.CreditCard = shoppingCart.FormofPaymentDetails.CreditCard;
                    formOfPaymentDetails.SecondaryCreditCard = shoppingCart.FormofPaymentDetails.SecondaryCreditCard;
                }
                else
                {
                    formOfPaymentDetails.CreditCard = reservation.CreditCards.Count() > 0 ? reservation.CreditCards[0] : null;

                }
                if (_shoppingUtility.IncludeFFCResidual(request.Application.Id, request.Application.Version.Major) && shoppingCart.FormofPaymentDetails != null)
                {
                    formOfPaymentDetails.TravelFutureFlightCredit = shoppingCart.FormofPaymentDetails?.TravelFutureFlightCredit;
                    formOfPaymentDetails.FormOfPaymentType = shoppingCart.FormofPaymentDetails.FormOfPaymentType;
                }
                if (_shoppingUtility.IncludeMoneyPlusMiles(request.Application.Id, request.Application.Version.Major))
                {
                    formOfPaymentDetails.MoneyPlusMilesCredit = shoppingCart.FormofPaymentDetails?.MoneyPlusMilesCredit;
                }
                //FFCShopping fFCShopping = new FFCShopping(_configuration,null,_sessionHelperService, _sDLContentService, _shoppingUtility);
                _fFCShoppingcs.AssignFFCValues(reservation.SessionId, shoppingCart, request, formOfPaymentDetails, reservation);
                shoppingCart.FormofPaymentDetails = formOfPaymentDetails;
                shoppingCart.FormofPaymentDetails.Phone = reservation.ReservationPhone;
                shoppingCart.FormofPaymentDetails.Email = reservation.ReservationEmail;
                shoppingCart.FormofPaymentDetails.EmailAddress = reservation.ReservationEmail != null ? reservation.ReservationEmail.EmailAddress : null;
                shoppingCart.FormofPaymentDetails.BillingAddress = reservation.CreditCardsAddress.Count() > 0 ? reservation.CreditCardsAddress[0] : null;
                if (reservation.IsReshopChange)
                {

                    double changeFee = 0.0;
                    double grandTotal = 0.0;
                    if (reservation.Prices.Exists(price => price.DisplayType.ToUpper().Trim() == "CHANGEFEE"))
                        changeFee = reservation.Prices.First(price => price.DisplayType.ToUpper().Trim() == "CHANGEFEE").Value;

                    if (reservation.Prices.Exists(price => price.DisplayType.ToUpper().Trim() == "GRAND TOTAL"))
                        grandTotal = reservation.Prices.First(price => price.DisplayType.ToUpper().Trim() == "GRAND TOTAL").Value;

                    if (!reservation.AwardTravel)
                    {
                        if (grandTotal == 0.0)
                        {
                            grandTotal = (reservation.Prices != null && reservation.Prices.Count > 0) ? reservation.Prices.First(price => price.DisplayType.ToUpper().Trim() == "TOTAL").Value : grandTotal;
                        }
                    }
                    string totalDue = (grandTotal > changeFee ? (grandTotal - changeFee) : 0).ToString();
                    shoppingCart.TotalPrice = String.Format("{0:0.00}", totalDue);
                    shoppingCart.DisplayTotalPrice = totalDue;
                }

                if (_shoppingUtility.IsETCCombinabilityEnabled(request.Application.Id, request.Application.Version.Major) && shoppingCart.Flow == FlowType.BOOKING.ToString())
                {
                    _shoppingUtility.LoadandAddTravelCertificate(shoppingCart, reservation, isETCCertificatesExistInShoppingCartPersist);
                }
                else if (_configuration.GetValue<bool>("ETCToggle") && shoppingCart.Flow == FlowType.BOOKING.ToString())
                {
                    _shoppingUtility.LoadandAddTravelCertificate(shoppingCart, reservation.SessionId, reservation.Prices, isETCCertificatesExistInShoppingCartPersist, request.Application);
                }
                if (_configuration.GetValue<bool>("EnableETCBalanceAttentionMessageOnRTI") && !_shoppingUtility.IsETCCombinabilityEnabled(request.Application.Id, request.Application.Version.Major))
                {
                    _shoppingUtility.AssignBalanceAttentionInfoWarningMessage(reservation.ShopReservationInfo2, shoppingCart.FormofPaymentDetails?.TravelCertificate);
                }
                if (_configuration.GetValue<bool>("EnableCouponsforBooking") && shoppingCart.Flow == FlowType.BOOKING.ToString())
                {
                    LoadandAddPromoCode(shoppingCart, reservation.SessionId, request.Application);
                }
                reservation.CartId = null;
                reservation.PointOfSale = null;
                reservation.PayPal = null;
                reservation.PayPalPayor = null;
                reservation.MasterpassSessionDetails = null;
                reservation.Masterpass = null;
                reservation.TravelersCSL = null;
                //reservation.CreditCards2 = null;
                //reservation.ReservationPhone2 = null;
                //reservation.ReservationEmail2 = null;
                reservation.CreditCardsAddress = null;
                reservation.FOPOptions = null;
                if (_configuration.GetValue<bool>("EnableSelectDifferentFOPAtRTI"))
                {
                    if (!reservation.IsReshopChange)
                    {
                        //If ETC, ghost card, no saved cc presents and no due in reshop disable this button.
                        if (reservation.ShopReservationInfo2 != null && shoppingCart.FormofPaymentDetails != null)
                        {
                            if (((shoppingCart.FormofPaymentDetails.CreditCard != null && (reservation.ShopReservationInfo == null || !reservation.ShopReservationInfo.CanHideSelectFOPOptionsAndAddCreditCard)) ||
                                                        shoppingCart.FormofPaymentDetails.masterPass != null || shoppingCart.FormofPaymentDetails.PayPal != null || shoppingCart.FormofPaymentDetails.Uplift != null ||
                                                      (!string.IsNullOrEmpty(shoppingCart.FormofPaymentDetails.FormOfPaymentType) && shoppingCart.FormofPaymentDetails.FormOfPaymentType.ToUpper().Equals("APPLEPAY")))
                                                      && (shoppingCart.FormofPaymentDetails.TravelCertificate == null ||
                                                      shoppingCart.FormofPaymentDetails?.TravelCertificate?.Certificates == null ||
                                                      shoppingCart.FormofPaymentDetails?.TravelCertificate?.Certificates?.Count == 0))
                            {
                                reservation.ShopReservationInfo2.ShowSelectDifferentFOPAtRTI = true;
                            }
                            else
                            {
                                reservation.ShopReservationInfo2.ShowSelectDifferentFOPAtRTI = false;
                            }

                            //if (reservation.IsReshopChange)
                            //{
                            //    if (Double.TryParse(shoppingCart.TotalPrice, out double p))
                            //    {
                            //        if (p > 0 && (shoppingCart.FormofPaymentDetails.CreditCard != null ||
                            //                           (!string.IsNullOrEmpty(shoppingCart.FormofPaymentDetails.FormOfPaymentType) && shoppingCart.FormofPaymentDetails.FormOfPaymentType.ToUpper().Equals("APPLEPAY"))))
                            //        {
                            //            reservation.ShopReservationInfo2.ShowSelectDifferentFOPAtRTI = true;
                            //        }
                            //        else
                            //            reservation.ShopReservationInfo2.ShowSelectDifferentFOPAtRTI = false;
                            //    }
                            //}

                        }
                    }
                }
                _fFCShoppingcs.AssignNullToETCAndFFCCertificates(shoppingCart.FormofPaymentDetails);

                //OmniCart omniCart = new OmniCart(_configuration, null, _sessionHelperService, _shoppingUtility);
                if (_omniCart.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, reservation?.ShopReservationInfo2?.IsDisplayCart == true))
                {
                    _omniCart.BuildCartTotalPrice(shoppingCart, reservation);
                }
            }
            catch (Exception ex)
            {
                throw new UnitedException(ex.Message.ToString());
            }
        }

        private void LoadandAddPromoCode(MOBShoppingCart shoppingCart, string sessionId, MOBApplication application)
        {
            var persistedApplyPromoCodeResponse = new ApplyPromoCodeResponse();
            persistedApplyPromoCodeResponse = _sessionHelperService.GetSession<ApplyPromoCodeResponse>(Headers.ContextValues, "United.Definition.FormofPayment.MOBApplyPromoCodeResponse").Result;
            //persistedApplyPromoCodeResponse = FilePersist.Load<MOBApplyPromoCodeResponse>(sessionId, typeof(MOBApplyPromoCodeResponse).FullName, false);
            if (shoppingCart.PromoCodeDetails == null)
            {
                shoppingCart.PromoCodeDetails = new MOBPromoCodeDetails();
            }
            if (persistedApplyPromoCodeResponse != null)
            {
                UpdateShoppinCartWithCouponDetails(shoppingCart);
                persistedApplyPromoCodeResponse.ShoppingCart.PromoCodeDetails = shoppingCart.PromoCodeDetails;
                _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, Headers.ContextValues, "United.Definition.MOBShoppingCart");
                //Persist.FilePersist.Save<MOBShoppingCart>(sessionId, shoppingCart.GetType().ToString(), shoppingCart);
                _sessionHelperService.SaveSession<ApplyPromoCodeResponse>(persistedApplyPromoCodeResponse, Headers.ContextValues, "United.Definition.FormofPayment.MOBApplyPromoCodeResponse");
                //FilePersist.Save<MOBApplyPromoCodeResponse>(sessionId, typeof(MOBApplyPromoCodeResponse).FullName, persistedApplyPromoCodeResponse);
            }
            // DisablePromoOption(shoppingCart);
            IsHidePromoOption(shoppingCart);
        }

        private void UpdateShoppinCartWithCouponDetails(MOBShoppingCart persistShoppingCart)
        {
            if (persistShoppingCart != null && persistShoppingCart.Products.Any())
            {
                persistShoppingCart.PromoCodeDetails = new MOBPromoCodeDetails();
                persistShoppingCart.PromoCodeDetails.PromoCodes = new List<MOBPromoCode>();
                persistShoppingCart.Products.ForEach(product =>
                {
                    if (product.CouponDetails != null && product.CouponDetails.Any())
                    {
                        product.CouponDetails.ForEach(CouponDetail =>
                        {
                            if (_configuration.GetValue<bool>("EnableFareandAncillaryPromoCodeChanges") ? !IsDuplicatePromoCode(persistShoppingCart.PromoCodeDetails.PromoCodes, CouponDetail.PromoCode) : true)
                            {
                                persistShoppingCart.PromoCodeDetails.PromoCodes
                                .Add(new MOBPromoCode
                                {
                                    PromoCode = CouponDetail.PromoCode,
                                    AlertMessage = CouponDetail.Description,
                                    IsSuccess = true,
                                    TermsandConditions = new MOBMobileCMSContentMessages
                                    {
                                        Title = _configuration.GetValue<string>("PromoCodeTermsandConditionsTitle"),
                                        HeadLine = _configuration.GetValue<string>("PromoCodeTermsandConditionsTitle")
                                    }
                                });
                            }
                        });
                    }
                });
            }
        }

        private bool IsDuplicatePromoCode(List<MOBPromoCode> promoCodes, string promoCode)
        {

            if (promoCodes != null && promoCodes.Any() && promoCodes.Count > 0)
            {
                if (promoCodes.Exists(c => c.PromoCode.Equals(promoCode)))
                {
                    return true;
                }
            }
            return false;
        }
        private void IsHidePromoOption(MOBShoppingCart shoppingCart)
        {
            bool isTravelCertificateAdded = shoppingCart?.FormofPaymentDetails?.TravelCertificate != null && shoppingCart.FormofPaymentDetails.TravelCertificate.Certificates != null && shoppingCart.FormofPaymentDetails.TravelCertificate.Certificates.Count > 0;
            bool isCouponAdded = (shoppingCart?.PromoCodeDetails?.PromoCodes != null && shoppingCart.PromoCodeDetails.PromoCodes.Count > 0);
            if (shoppingCart?.Products != null && shoppingCart.Products.Any(p => p?.Code?.ToUpper() == "FARELOCK" || p?.Code?.ToUpper() == "FLK"))
            {
                shoppingCart.PromoCodeDetails.IsHidePromoOption = true;
                return;
            }

            if (!isCouponAdded && (_configuration.GetValue<string>("Fops_HidePromoOption").Contains(shoppingCart?.FormofPaymentDetails?.FormOfPaymentType)
                || (_configuration.GetValue<string>("Fops_HidePromoOption").Contains("ETC") && isTravelCertificateAdded)))
            {
                shoppingCart.PromoCodeDetails.IsHidePromoOption = true;
            }
            else
            {
                shoppingCart.PromoCodeDetails.IsHidePromoOption = false;
            }
        }

        private string GetCslEndpointForShopping(bool isReshop)
        {
            string cslEndpoint = string.Empty;
            if (isReshop)
            {
                cslEndpoint = _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLReShoppingService");
            }
            else
            {
                cslEndpoint = _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLShopping");
            }
            return cslEndpoint;
        }

        private string GetPriceTextDescription(string searchType)
        {
            string priceTextDescription = string.Empty;

            if (searchType == "RT")
            {
                priceTextDescription = _configuration.GetValue<string>("PriceTextDescription").Split('|')[0];//Roundtrip
            }
            else if (searchType == "MD")
            {
                priceTextDescription = _configuration.GetValue<string>("PriceTextDescription").Split('|')[1];//From
            }

            return priceTextDescription;
        }

        private string GetFSRFareDescription(MOBSHOPShopRequest request)
        {
            string FSRFareDescription = string.Empty;
            bool isExperiment = false;

            // Need to add new experiment when comparing
            if (_shoppingUtility.CheckFSRRedesignFromShopRequest(request))
            {
                FSRFareDescription = GetNewFSRFareDescriptionMessage(request.SearchType);
            }
            else
            {
                FSRFareDescription = GetFSRFareDescriptionMessage(request.SearchType);
            }

            if (_configuration.GetValue<bool>("IsExperimentEnabled") && request.Experiments != null && request.Experiments.Any() && request.Experiments.Contains(ShoppingExperiments.NoChangeFee.ToString()))
            {
                isExperiment = true;
            }

            if (request.SearchType == "RT")
            {
                if (_configuration.GetValue<bool>("ChangeFeeWaiverMessagingToggle"))
                {
                    if (isExperiment)
                    {
                        return FSRFareDescription;
                    }
                    else
                        FSRFareDescription += "\n" + _shoppingUtility.GetFeeWaiverMessage();
                }
            }
            else if (request.SearchType == "MD")
            {
                if (_configuration.GetValue<bool>("ChangeFeeWaiverMessagingToggle"))
                {
                    if (isExperiment)
                    {
                        return FSRFareDescription;
                    }
                    else
                        FSRFareDescription += "\n" + _shoppingUtility.GetFeeWaiverMessage();
                }
            }
            else if (request.SearchType == "OW" && _configuration.GetValue<bool>("ChangeFeeWaiverMessagingToggle"))
            {
                if (isExperiment)
                {
                    return FSRFareDescription;
                }
                else
                    FSRFareDescription += "\n" + _shoppingUtility.GetFeeWaiverMessage();
            }

            return FSRFareDescription;
        }

        private string GetFSRFareDescriptionMessage(string searchType)
        {
            string FSRDescription = string.Empty;
            switch (searchType)
            {
                case "RT":
                    FSRDescription = _configuration.GetValue<string>("FSRFareDescription").Split('|')[0];
                    break;
                case "MD":
                    FSRDescription = _configuration.GetValue<string>("FSRFareDescription").Split('|')[1];
                    break;
                default:
                    FSRDescription = string.Empty;
                    break;
            }

            return FSRDescription;
        }

        private string GetPriceFromText(string searchType)
        {
            string priceFromText = string.Empty;

            if (searchType == "OW")
            {
                priceFromText = _configuration.GetValue<string>("PriceFromText").Split('|')[0];//One Way -- For
            }
            else if (searchType == "RT")
            {
                priceFromText = _configuration.GetValue<string>("PriceFromText").Split('|')[1];//Round Trip -- From
            }
            else if (searchType == "MD")
            {
                priceFromText = _configuration.GetValue<string>("PriceFromText").Split('|')[2];//Multi -- From
            }

            return priceFromText;
        }

        private MOBSHOPTrip PopulateTrip(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string cartId, List<Trip> trips, int tripIndex, string requestedCabin, string sessionId, string tripKey, int appId, string deviceId, string appVersion, bool showMileageDetails, int premierStatusLevel, bool isStandardRevenueSearch, bool isAward = false, bool isELFFareDisplayAtFSR = true, bool getNonStopFlightsOnly = false, bool getFlightsWithStops = false, MOBSHOPShopRequest shopRequest = null)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            Session session = new Session();
            session = _sessionHelperService.GetSession<Session>(Headers.ContextValues, session.ObjectName).Result;
            //session = Persist.FilePersist.Load<Session>(sessionId, session.ObjectName);
            supressLMX = session.SupressLMXForAppID;
            #endregion
            MOBSHOPTrip trip = null;
            string getAmenitiesCallDurations = string.Empty;
            var showOriginDestinationForFlights = false;
            int i = 0;
            try
            {
                if (isGetAirportListInOneCallToggleOn() && (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0))
                {
                    airportsList = GetAllAiportsList(trips);
                    _sessionHelperService.SaveSession<AirportDetailsList>(airportsList, Headers.ContextValues);
                    //FilePersist.Save(sessionId, (new AirportDetailsList()).GetType().FullName, _airportsList);
                }
            }
            catch (Exception ex)
            {
                //logEntries.Add(LogEntry.GetLogEntry<Exception>(sessionId, "GetAllAiportsList", "PopulateTrip-GetAllAiportsList", appId, appVersion, deviceId, ex, true, true));
                //logEntries.Add(LogEntry.GetLogEntry<List<Trip>>(sessionId, "GetAllAiportsList", "PopulateMetaTrips-Trip", appId, appVersion, deviceId, trips, true, true));
            }

            if (trips != null && trips.Count > 0 && (tripIndex == 0 || trips.Count > tripIndex))
            {
                i = tripIndex;

                trip = new MOBSHOPTrip();
                trip.TripId = trips[i].BBXSolutionSetId;
                if (_configuration.GetValue<bool>("EnableNonStopFlight"))
                {
                    trip.FlightCount = trips[i].Flights.Count; //**nonstopchanges==> Need to work with client and fix the total flight count.
                    if (getNonStopFlightsOnly && !getFlightsWithStops)
                        trip.TripHasNonStopflightsOnly = trips[i].UseFilters; //**nonstopchanges==> Need to work to return the right bool for first shop/selectrip response & second shop/selectrip response.
                }
                else
                {
                    if (!trips[i].UseFilters)
                    {
                        trip.FlightCount = trips[i].Flights.Count;
                    }
                    else
                    {
                        if (i == 0)
                        {
                            Model.Shopping.ShoppingResponse shop = new Model.Shopping.ShoppingResponse();
                            shop = _sessionHelperService.GetSession<Model.Shopping.ShoppingResponse>(Headers.ContextValues, shop.ObjectName).Result;
                            trip.FlightCount = shop.Response.Availability.Trip.FlightCount;
                        }
                        else
                        {
                            SelectTrip selectTrip = new SelectTrip();
                            selectTrip = _sessionHelperService.GetSession<SelectTrip>(Headers.ContextValues, selectTrip.ObjectName).Result;
                            //selectTrip =
                            //    United.Persist.FilePersist.Load<United.Persist.Definition.Shopping.SelectTrip>(
                            //        sessionId, selectTrip.ObjectName);
                            trip.FlightCount = selectTrip.Responses[tripKey].Availability.Trip.FlightCount;
                        }
                    }
                }
                trip.TotalFlightCount = trip.FlightCount;
                trip.DepartDate = FormatDateFromDetails(trips[i].DepartDate);
                trip.ArrivalDate = FormatDateFromDetails(trips[i].Flights[trips[i].Flights.Count - 1].DestinationDateTime);
                trip.Destination = trips[i].Destination;

                string destinationName = string.Empty;
                if (!string.IsNullOrEmpty(trip.Destination))
                {
                    destinationName = GetAirportNameFromSavedList(trip.Destination);
                }
                if (string.IsNullOrEmpty(destinationName))
                {
                    trip.DestinationDecoded = trips[i].DestinationDecoded;
                }
                else
                {
                    trip.DestinationDecoded = destinationName;
                }

                CultureInfo ci = null;

                if (_configuration.GetValue<bool>("EnableNonStopFlight") && getFlightsWithStops)
                {
                    trip.Columns = PopulateColumns(trips[i].ColumnInformation, getFlightsWithStops, session);
                }
                else
                {
                    trip.Columns = PopulateColumns(trips[i].ColumnInformation);
                }

                List<Model.Shopping.MOBSHOPFlight> flights = null;
                if (trips[i].Flights != null && trips[i].Flights.Count > 0)
                {
                    //update amenities for all flights
                    UpdateAmenitiesIndicatorsResponse amenitiesResponse = new UpdateAmenitiesIndicatorsResponse();
                    List<Flight> tempFlights = new List<Flight>(trips[i].Flights);

                    //we do not want the search to fail if one of these fail...
                    try
                    {

                        Parallel.Invoke(() =>
                        {
                            bool includeAmenities = false;
                            bool.TryParse(_configuration.GetValue<string>("IncludeAmenities"), out includeAmenities);

                            //if we are asking for amenities in the CSL call, do not make this seperate call
                            if (!includeAmenities)
                            {
                                //****Get Call Duration Code - Venkat 03/17/2015*******
                                Stopwatch getAmenitiesCSLCallDurationstopwatch1;
                                getAmenitiesCSLCallDurationstopwatch1 = new Stopwatch();
                                getAmenitiesCSLCallDurationstopwatch1.Reset();
                                getAmenitiesCSLCallDurationstopwatch1.Start();
                                amenitiesResponse = GetAmenitiesForFlights(sessionId, cartId, tempFlights, appId, deviceId, appVersion);
                                if (getAmenitiesCSLCallDurationstopwatch1.IsRunning)
                                {
                                    getAmenitiesCSLCallDurationstopwatch1.Stop();
                                }
                                getAmenitiesCallDurations = getAmenitiesCallDurations + "|44=" + getAmenitiesCSLCallDurationstopwatch1.ElapsedMilliseconds.ToString() + "|"; // 44 Flight Amenities call
                                PopulateFlightAmenities(amenitiesResponse.Profiles, ref tempFlights);
                            }
                        },
                            () =>
                            {
                                if (showMileageDetails && !supressLMX)
                                {
                                    List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights = null;
                                    List<Flight> newFlights = null;
                                    lmxFlights = GetLmxFlights(session.Token, cartId, GetFlightHasListForLMX(tempFlights), sessionId, appId, appVersion, deviceId);

                                    if (lmxFlights != null && lmxFlights.Count > 0)
                                    {
                                        PopulateLMX(lmxFlights, ref tempFlights);
                                    }
                                }
                            }
                        );
                    }
                    catch { };

                    trips[i].Flights = new List<Flight>(tempFlights);

                    //****Get Call Duration Code - Venkat 03/17/2015*******
                    Stopwatch getFlightsStopWatch;
                    getFlightsStopWatch = new Stopwatch();
                    getFlightsStopWatch.Reset();
                    getFlightsStopWatch.Start();
                    string fareClass = GetFareClassAtShoppingRequestFromPersist(sessionId);

                    if (_mOBSHOPDataCarrier == null)
                        _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                    flights = GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, trips[i].Flights, trips[i].SearchFiltersIn.FareFamily, out ci, trips[i].SearchFiltersOut.PriceMin, trip.Columns, premierStatusLevel, fareClass, false, false, isELFFareDisplayAtFSR, trip, appVersion, appId);
                    if (getFlightsStopWatch.IsRunning)
                    {
                        getFlightsStopWatch.Stop();
                    }
                }

                trip.Origin = trips[i].Origin;

                if (showMileageDetails && !supressLMX)
                    trip.YqyrMessage = _configuration.GetValue<string>("MP2015YQYRMessage");

                string originName = string.Empty;
                if (!string.IsNullOrEmpty(trip.Origin))
                {
                    originName = GetAirportNameFromSavedList(trip.Origin);
                }
                if (string.IsNullOrEmpty(originName))
                {
                    trip.OriginDecoded = trips[i].OriginDecoded;
                }
                else
                {
                    trip.OriginDecoded = originName;
                }

                if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI"))
                {
                    //with share trip flag OriginDescription=Chicago, IL, US (ORD)
                    trip.OriginDecodedWithCountry = trips[i].OriginDecoded;
                    trip.DestinationDecodedWithCountry = trips[i].DestinationDecoded;
                }


                if (flights != null)
                {
                    trip.FlattenedFlights = new List<Model.Shopping.MOBSHOPFlattenedFlight>();

                    trip.FlattenedFlights = GetFlattendFlights(flights, trip.TripId, trips[i].BBXCellIdSelected, trip.DepartDate, shopRequest, tripIndex);

                    if (_configuration.GetValue<bool>("EnableShowOriginDestinationForFlights") && session.IsFSRRedesign && GeneralHelper.IsApplicationVersionGreaterorEqual(shopRequest.Application.Id, shopRequest.Application.Version.Major, "AndroidShowOriginDestinationForFlightsVersion", "iOSShowOriginDestinationForFlightsVersion"))
                    {
                        showOriginDestinationForFlights = trip.FlattenedFlights.Any(x => x?.Flights?.First()?.Origin != trip.Origin || x?.Flights?.Last()?.Destination != trip.Destination);
                    }
                }
                bool mixedCabinFlightExists = false;
                if (_configuration.GetValue<bool>("EnableAwardMixedCabinFiter") && isAward)
                {
                    mixedCabinFlightExists = trip.FlattenedFlights.Any(f => f.Flights.Any(s => s.ShoppingProducts.Any(p => p.IsMixedCabin)));
                }
                trip.UseFilters = trips[i].UseFilters;
                trip.SearchFiltersIn = trips[i].SearchFiltersIn != null && !string.IsNullOrEmpty(trips[i].SearchFiltersIn.AirportsOrigin) ? GetSearchFilters(trips[i].SearchFiltersIn, ci, appId, appVersion, requestedCabin, trips[i].ColumnInformation, isStandardRevenueSearch, isAward) : GetSearchFilters(trips[i].SearchFiltersOut, ci, appId, appVersion, requestedCabin, trips[i].ColumnInformation, isStandardRevenueSearch, isAward);
                trip.SearchFiltersOut = GetSearchFilters(trips[i].SearchFiltersOut, ci, appId, appVersion, requestedCabin, trips[i].ColumnInformation, isStandardRevenueSearch, isAward);
                if (_configuration.GetValue<bool>("SuppressFSREPlusColumn"))
                {
                    trip.DisableEplus = trips[i].AncillaryStatus?.DisableEplus ?? false;
                }

                if (session.IsFSRRedesign && _configuration.GetValue<bool>("IsEnabledFsrRedesignFooterSortring"))
                {
                    if (trip.Columns?.Count > 0)
                    {
                        var sortTypes = GetDefaultFsrRedesignFooterSortTypes();
                        foreach (var column in trip.Columns)
                        {
                            var value = ($"{column.LongCabin} {column.Description}").Trim();
                            sortTypes.Add(new MOBSearchFilterItem()
                            {
                                Key = column.Type,
                                Value = value,
                                DisplayValue = value
                            });
                        }
                        if (trip.SearchFiltersOut?.SortTypes?.Count() > 0)
                        {
                            trip.SearchFiltersOut.SortTypes = sortTypes;
                        }
                        if (trip.SearchFiltersIn?.SortTypes?.Count() > 0)
                        {
                            trip.SearchFiltersIn.SortTypes = sortTypes;
                        }
                    }
                }

                ///208848 : PROD Basic Economy mApps Price in footer of FSR is showing lowest BE fare when BE switch is off 
                ///Srini - 12/29/2017
                if (_configuration.GetValue<bool>("BugFixToggleFor18B") && _mOBSHOPDataCarrier.FsrMinPrice > 0 && trip.SearchFiltersIn.PriceMin < _mOBSHOPDataCarrier.FsrMinPrice && !session.IsReshopChange)
                {
                    if (isAward)
                    {
                        trip.SearchFiltersOut.PriceMinDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(Convert.ToString(_mOBSHOPDataCarrier.FsrMinPrice), true);
                    }
                    else
                    {
                        trip.SearchFiltersOut.PriceMinDisplayValue = TopHelper.FormatAmountForDisplay(_mOBSHOPDataCarrier.FsrMinPrice.ToString(), ci = TopHelper.GetCultureInfo(""), true);
                    }
                    trip.SearchFiltersOut.PriceMin = _mOBSHOPDataCarrier.FsrMinPrice;
                }
            }
            trip.CallDurationText = getAmenitiesCallDurations;
            int pageSize = Convert.ToInt32(_configuration.GetValue<string>("OrgarnizeResultsRequestPageSize").ToString());
            trip.PageCount = (trip.FlightCount / pageSize) + ((trip.FlightCount % pageSize) == 0 ? 0 : 1);

            if (Convert.ToBoolean(_configuration.GetValue<string>("ReturnAllRemainingShopFlightsWithOnly2PageCount").ToString()) && trip.PageCount > 1)
            {
                trip.PageCount = 2;   //**PageCount==2==>> for paging implementation to send only 15 flights back to client and perisit remaining flights and return remaining when customer page up
            }
            if (_configuration.GetValue<bool>("EnableNonStopFlight"))
            {
                if (getNonStopFlightsOnly && !getFlightsWithStops && trip.TripHasNonStopflightsOnly)
                    trip.PageCount = 1; //**to send pagecount 1 means not to call organize results for paging as we have before after shop call () if the market search has non stop flights.
                else if (getFlightsWithStops && !_configuration.GetValue<bool>("AllowPagingForFlightsWithStops")) //** if decided not to do paging for flights with stops resposne even they are large in number then uncomment these 2 lines or define and set the value of this "AllowPagingForFlightsWithStops" as true;
                    trip.PageCount = 1; //**to send pagecount 1 means not to call organize results for paging as we have before after shop call () if the market search has non stop flights.
            }

            if (session.IsFSRRedesign)
            {
                if (_configuration.GetValue<bool>("EnableColumnSelectionFsrRedesign"))
                {
                    string focusColumnID = trip.FlattenedFlights[0].Flights[0].ShoppingProducts.FirstOrDefault(p => p.IsSelectedCabin)?.ColumnID ?? trip.FlattenedFlights[0].Flights[0].ShoppingProducts[0].ColumnID;
                    trip.Columns.First(c => c.ColumnID.Equals(focusColumnID)).IsSelectedCabin = true;
                }

                trip.ShowOriginDestinationForFlights = showOriginDestinationForFlights;
            }

            return trip;
        }

        private AirportDetailsList GetAllAiportsList(List<United.Services.FlightShopping.Common.Trip> trips)
        {
            string airPortCodes = GetAllAirportCodesWithCommaDelimatedFromCSLTrips(trips);
            return GetAirportNamesListCollection(airPortCodes);
        }

        private List<MOBSearchFilterItem> GetDefaultFsrRedesignFooterSortTypes()
        {
            var sortTypes = new List<MOBSearchFilterItem>();
            List<string> sortTypesList = _configuration.GetValue<string>("FsrRedesignSearchFiletersSortTypes").Split('|').ToList();
            foreach (string sortType in sortTypesList)
            {
                var item = new MOBSearchFilterItem();
                item.Key = sortType.Split('~')[0].ToString();
                item.Value = sortType.Split('~')[1].ToString();
                item.DisplayValue = sortType.Split('~')[1].ToString();
                sortTypes.Add(item);
            }
            return sortTypes;
        }



        private MOBSearchFilters GetSearchFilters(SearchFilterInfo filters, CultureInfo ci, int appId, string appVersion, string requestedCabin, ColumnInformation columnInfo, bool isStandardRevenueSearch, bool isAward = false)
        {
            var filter = new MOBSearchFilters();

            if (filters != null)
            {
                if (!String.IsNullOrEmpty(filters.AircraftTypes))
                {
                    filter.AircraftTypes = filters.AircraftTypes;
                }

                if (!String.IsNullOrEmpty(filters.AirportsDestination))
                {
                    filter.AirportsDestination = processNearbyAirports(filters.AirportsDestination);
                }

                if (filters.AirportsDestinationList != null && filters.AirportsDestinationList.Count > 0)
                {
                    filter.AirportsDestinationList = new List<MOBSearchFilterItem>();
                    foreach (CodeDescPair cdp in filters.AirportsDestinationList)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(cdp.Currency);
                        }

                        var kvp = new MOBSearchFilterItem();
                        kvp.Key = cdp.Code;
                        kvp.Value = cdp.Description;
                        kvp.DisplayValue = cdp.Description + " (" + cdp.Code + ")";
                        kvp.Currency = cdp.Currency;
                        if (isAward)
                            kvp.Amount = ShopStaticUtility.FormatAwardAmountForDisplay(cdp.Amount, true);
                        else
                            kvp.Amount = TopHelper.FormatAmountForDisplay(cdp.Amount, ci);

                        kvp.IsSelected = true;
                        filter.AirportsDestinationList.Add(kvp);
                    }
                    filter.AirportsDestinationList = processNearbyAirports(filter.AirportsDestinationList);
                }

                if (!String.IsNullOrEmpty(filters.AirportsOrigin))
                {
                    filter.AirportsOrigin = processNearbyAirports(filters.AirportsOrigin);
                }

                if (filters.AirportsOriginList != null && filters.AirportsOriginList.Count > 0)
                {
                    filter.AirportsOriginList = new List<MOBSearchFilterItem>();
                    foreach (CodeDescPair cdp in filters.AirportsOriginList)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(cdp.Currency);
                        }

                        var kvp = new MOBSearchFilterItem();
                        kvp.Key = cdp.Code;
                        kvp.Value = cdp.Description;
                        kvp.DisplayValue = cdp.Description + " (" + cdp.Code + ")";
                        kvp.Currency = cdp.Currency;
                        if (isAward)
                            kvp.Amount = ShopStaticUtility.FormatAwardAmountForDisplay(cdp.Amount, true);
                        else
                            kvp.Amount = TopHelper.FormatAmountForDisplay(cdp.Amount, ci);
                        kvp.IsSelected = true;
                        filter.AirportsOriginList.Add(kvp);
                    }
                    filter.AirportsOriginList = processNearbyAirports(filter.AirportsOriginList);
                }

                if (!String.IsNullOrEmpty(filters.AirportsStop))
                {
                    filter.AirportsStop = filters.AirportsStop;
                }

                if (filters.AirportsStopList != null && filters.AirportsStopList.Count > 0)
                {
                    filter.AirportsStopList = new List<MOBSearchFilterItem>();
                    var sortedByAirportNameList = filters.AirportsStopList.OrderBy(x => x.Description);
                    foreach (CodeDescPair cdp in sortedByAirportNameList)
                    {
                        var kvp = new MOBSearchFilterItem();
                        kvp.Key = cdp.Code;
                        kvp.Value = cdp.Description;
                        kvp.DisplayValue = cdp.Description + " (" + cdp.Code + ")";
                        kvp.Currency = cdp.Currency;
                        kvp.Amount = "";
                        kvp.IsSelected = true;
                        filter.AirportsStopList.Add(kvp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.AirportsStopToAvoid))
                {
                    filter.AirportsStopToAvoid = filters.AirportsStopToAvoid;
                }

                if (filters.AirportsStopToAvoidList != null && filters.AirportsStopToAvoidList.Count > 0)
                {
                    filter.AirportsStopToAvoidList = new List<MOBSearchFilterItem>();
                    foreach (CodeDescPair cdp in filters.AirportsStopToAvoidList)
                    {
                        var kvp = new MOBSearchFilterItem();
                        kvp.Key = cdp.Code;
                        kvp.Value = cdp.Description;
                        kvp.DisplayValue = cdp.Description + " (" + cdp.Code + ")";
                        kvp.Currency = cdp.Currency;
                        kvp.Amount = "";
                        kvp.IsSelected = true;
                        filter.AirportsStopToAvoidList.Add(kvp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.BookingCodes))
                {
                    filter.BookingCodes = filters.BookingCodes;
                }

                if (!String.IsNullOrEmpty(filters.CarriersMarketing))
                {
                    filter.CarriersMarketing = filters.CarriersMarketing;
                }

                if (filters.CarriersMarketingList != null && filters.CarriersMarketingList.Count > 0)
                {
                    filter.CarriersMarketingList = new List<MOBSearchFilterItem>();
                    foreach (CodeDescPair cdp in filters.CarriersMarketingList)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(cdp.Currency);
                        }

                        var kvp = new MOBSearchFilterItem();
                        kvp.Key = cdp.Code;
                        kvp.Value = cdp.Description;
                        kvp.DisplayValue = cdp.Description;
                        kvp.Currency = cdp.Currency;
                        kvp.Amount = TopHelper.FormatAmountForDisplay(cdp.Amount, ci);
                        kvp.IsSelected = true;
                        filter.CarriersMarketingList.Add(kvp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.CarriersOperating))
                {
                    filter.CarriersOperating = filters.CarriersOperating;
                }

                if (filters.CarriersOperatingList != null && filters.CarriersOperatingList.Count > 0)
                {
                    filter.CarriersOperatingList = new List<MOBSearchFilterItem>();
                    foreach (CodeDescPair cdp in filters.CarriersOperatingList)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(cdp.Currency);
                        }

                        var kvp = new MOBSearchFilterItem();
                        kvp.Key = cdp.Code;
                        kvp.Value = cdp.Description;
                        kvp.DisplayValue = cdp.Description;
                        kvp.Currency = cdp.Currency;
                        kvp.Amount = TopHelper.FormatAmountForDisplay(cdp.Amount, ci);
                        kvp.IsSelected = true;
                        filter.CarriersOperatingList.Add(kvp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.EquipmentCodes))
                {
                    filter.EquipmentCodes = filters.EquipmentCodes;
                }

                if (filters.EquipmentList != null && filters.EquipmentList.Count > 0)
                {
                    filter.EquipmentList = new List<MOBSearchFilterItem>();
                    foreach (CodeDescPair cdp in filters.EquipmentList)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(cdp.Currency);
                        }

                        var kvp = new MOBSearchFilterItem();
                        kvp.Key = cdp.Code;
                        kvp.Value = cdp.Description;
                        kvp.DisplayValue = cdp.Description;
                        kvp.Currency = cdp.Currency;
                        kvp.Amount = TopHelper.FormatAmountForDisplay(cdp.Amount, ci);

                        filter.EquipmentList.Add(kvp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.EquipmentTypes))
                {
                    filter.EquipmentTypes = filters.EquipmentTypes;
                }

                if (filters.FareFamilies != null && filters.FareFamilies.fareFamily != null && filters.FareFamilies.fareFamily.Count() > 0)
                {
                    filter.FareFamilies = new List<MOBSHOPFareFamily>();
                    int cnt = 0;
                    foreach (fareFamilyType ff in filters.FareFamilies.fareFamily)
                    {

                        var fft = new MOBSHOPFareFamily();
                        fft.FareFamily = string.IsNullOrEmpty(ff.fareFamily) ? "" : ff.fareFamily;
                        fft.MaxMileage = ff.maxMileage;
                        if (ff.maxPrice != null)
                        {
                            fft.MaxPrice = ff.maxPrice.amount.ToString();
                        }
                        fft.MinMileage = ff.minMileage;
                        fft.MinPrice = ff.minPrice == null ? "" : ff.minPrice.amount.ToString();
                        fft.MinPriceInSummary = ff.minPriceInSummary;
                        filter.FareFamilies.Add(fft);
                        cnt++;
                    }
                }

                if (!String.IsNullOrEmpty(filters.FareFamily))
                {
                    filter.FareFamily = filters.FareFamily;
                }
                // Get the right values for arrival date filter
                if (_configuration.GetValue<bool>("EnableFixMobile14343"))
                {
                    if (!String.IsNullOrEmpty(filters.MaxArrivalDate))
                    {
                        filter.MaxArrivalDate = filters.MaxArrivalDate;
                    }

                    if (!String.IsNullOrEmpty(filters.MinArrivalDate))
                    {
                        filter.MinArrivalDate = filters.MinArrivalDate;
                    }
                }

                if (!String.IsNullOrEmpty(filters.TimeArrivalMax))
                {
                    filter.TimeArrivalMax = filters.TimeArrivalMax;
                }

                if (!String.IsNullOrEmpty(filters.TimeArrivalMin))
                {
                    filter.TimeArrivalMin = filters.TimeArrivalMin;
                }

                if (!String.IsNullOrEmpty(filters.TimeDepartMax))
                {
                    filter.TimeDepartMax = filters.TimeDepartMax;
                }

                if (!String.IsNullOrEmpty(filters.TimeDepartMin))
                {
                    filter.TimeDepartMin = filters.TimeDepartMin;
                }

                #region
                if (filters.Warnings != null && filters.Warnings.Count > 0)
                {
                    filter.WarningsFilter = new List<MOBSearchFilterItem>();
                    foreach (string warning in filters.Warnings)
                    {
                        #region // As per stogo only the below 4 types are show to client for Advisories
                        List<string> warningsList = _configuration.GetValue<string>("SearchFiletersWarnings")?.Split('|').ToList();
                        foreach (string warningType in warningsList)
                        {
                            if (warning.ToUpper().Trim() == warningType.Split('~')[0].ToString().ToUpper().Trim())
                            {
                                var item = new MOBSearchFilterItem();
                                item.Key = warningType.Split('~')[1].ToString();
                                item.Value = warningType.Split('~')[2].ToString();
                                item.DisplayValue = warningType.Split('~')[2].ToString();
                                item.IsSelected = true;
                                filter.WarningsFilter.Add(item);
                                break;
                            }
                        }
                        #endregion
                    }
                }
                #endregion
                //Value types - have to have a value.
                filter.CabinCountMax = filters.CabinCountMax;
                filter.CabinCountMin = filters.CabinCountMin;
                filter.CarrierDefault = filters.CarrierDefault;
                filter.CarrierExpress = filters.CarrierExpress;
                filter.CarrierPartners = filters.CarrierPartners;
                filter.CarrierStar = filters.CarrierStar;
                filter.DurationMax = filters.DurationMax;
                filter.DurationMin = filters.DurationMin;
                filter.DurationStopMax = filters.DurationStopMax;
                filter.DurationStopMin = filters.DurationStopMin;
                filter.PriceMax = filters.PriceMax;
                if (filters.PriceMin == -1)
                    filters.PriceMin = 0;
                filter.PriceMin = filters.PriceMin;

                if (isAward)
                {
                    filter.PriceMaxDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(filters.PriceMax.ToString(), true);
                    filter.PriceMinDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(filters.PriceMin.ToString(), true);
                }
                else
                {
                    filter.PriceMaxDisplayValue = TopHelper.FormatAmountForDisplay(filters.PriceMax, ci, true);
                    filter.PriceMinDisplayValue = TopHelper.FormatAmountForDisplay((int)filters.PriceMin, ci, true);
                }

                filter.ShowPriceFilters = Convert.ToBoolean(_configuration.GetValue<string>("ShowPriceFilter"));
                filter.ShowArrivalFilters = Convert.ToBoolean(_configuration.GetValue<string>("ShowArrivalFilters"));
                filter.ShowDepartureFilters = Convert.ToBoolean(_configuration.GetValue<string>("ShowDepartureFilters"));
                filter.ShowDurationFilters = Convert.ToBoolean(_configuration.GetValue<string>("ShowDurationFilters"));
                filter.ShowLayOverFilters = Convert.ToBoolean(_configuration.GetValue<string>("ShowLayOverFilters"));
                filter.ShowSortingandFilters = Convert.ToBoolean(_configuration.GetValue<string>("ShowSortingandFilters"));

                filter.StopCountExcl = filters.StopCountExcl;
                filter.StopCountMax = filters.StopCountMax;
                filter.StopCountMin = filters.StopCountMin;
                #region
                filter.NumberofStops = new List<MOBSearchFilterItem>();
                if (filter.StopCountMin == 0 || filter.StopCountMax == 0)
                {
                    var item = new MOBSearchFilterItem();
                    item.IsSelected = true;
                    item.Key = StopTypes.NonStop.ToString();
                    item.Value = "0,0";
                    item.DisplayValue = "Nonstop";
                    filter.NumberofStops.Add(item);
                }
                if ((filter.StopCountMin == 1 || filter.StopCountMax == 1) || ((filter.StopCountMin == 0 && filter.StopCountMax >= 2)))
                {
                    var item = new MOBSearchFilterItem();
                    item.IsSelected = true;
                    item.Key = StopTypes.OneStop.ToString();
                    item.Value = "1,1";
                    item.DisplayValue = "1+ stops";
                    filter.NumberofStops.Add(item);
                }
                if (filter.StopCountMin >= 2 || filter.StopCountMax >= 2)
                {
                    var item = new MOBSearchFilterItem();
                    item.IsSelected = true;
                    item.Key = StopTypes.MoreStops.ToString();
                    item.Value = filter.StopCountMax.ToString() + "," + filter.StopCountMax.ToString();
                    item.DisplayValue = "2+ stops";
                    filter.NumberofStops.Add(item);
                }
                #endregion
                // Refundable fares toggle feature
                if (IsEnableRefundableFaresToggle(appId, appVersion) && isStandardRevenueSearch)
                {
                    string requestedFareFamily = GetFareFamily(requestedCabin, "");

                    // Looks if the toggle should be displayed
                    if ((columnInfo?.Columns?.Count ?? 0) > 0)
                    {
                        filter.ShowRefundableFaresToggle = columnInfo.Columns.FirstOrDefault(col => col.IsFullyRefundable = true) != null;
                    }

                    // Looks for the refundable toggle fare
                    if (filter.ShowRefundableFaresToggle && (filters.FareFamilies?.fareFamily?.Length ?? 0) > 0)
                    {
                        decimal minRefundablePrice = 0;
                        minRefundablePrice = filters.FareFamilies.fareFamily.FirstOrDefault(fam => fam.fareFamily.Contains("UNRESTRICTED") && fam.fareFamily.Contains(requestedFareFamily))?.minPriceValue ?? 0;
                        filter.RefundableFaresToggle = new MOBSearchFilterItem
                        {
                            Key = "RefundableFares",
                            Value = _configuration.GetValue<string>("RefundableFaresToggleValue"),
                            DisplayValue = (minRefundablePrice > 0 ?
                                                $"{_configuration.GetValue<string>("RefundableFaresToggleDisplayWithAmt")}{string.Format("{0:0}", minRefundablePrice)}" :
                                                _configuration.GetValue<string>("RefundableFaresToggleDisplay")),
                            IsSelected = filters.ShopIndicators?.IsRefundableSelected ?? false
                        };
                    }
                }

                // Mixed Cabin toggle feature
                if (_shoppingUtility.IsMixedCabinFilerEnabled(appId, appVersion) && isAward)
                {
                    // Looks for the MixedCabin toggle 
                    if (filter.AdditionalToggles == null)
                    {
                        filter.AdditionalToggles = new List<MOBSearchFilterItem>();
                    }
                    if (filter.AdditionalToggles.Any(k => k.Key == _configuration.GetValue<string>("MixedCabinToggleKey")))
                    {
                        filter.AdditionalToggles.FirstOrDefault(k => k.Key == _configuration.GetValue<string>("MixedCabinToggleKey")).IsSelected = filters.ShopIndicators?.IsMixedToggleSelected ?? false;
                    }
                    else
                    {
                        filter.AdditionalToggles.Add(new MOBSearchFilterItem
                        {
                            Key = _configuration.GetValue<string>("MixedCabinToggleKey"),
                            Value = _configuration.GetValue<string>("MixedCabinToggle"),
                            DisplayValue = _configuration.GetValue<string>("MixedCabinToggleDisplay"),
                            IsSelected = filters.ShopIndicators?.IsMixedToggleSelected ?? false
                        });
                    }
                }
            }

            filter = _shoppingUtility.SetSearchFiltersOutDefaults(filter);
            return filter;
        }

        private bool IsEnableRefundableFaresToggle(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableRefundableFaresToggle") &&
                   GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, "AndroidRefundableFaresToggleVersion", "iPhoneRefundableFaresToggleVersion");
        }

        private string GetFareFamily(string cabinSearched, string fareType)
        {
            string cabin = "ECONOMY";

            switch (cabinSearched.Trim().ToLower())
            {
                case "econ":
                    cabin = "ECONOMY";
                    break;
                case "business":
                case "businessfirst":
                    cabin = "BUSINESS";
                    break;
                case "first":
                    cabin = "FIRST";
                    break;
                case "awardecon":
                    cabin = "AWARDECONOMY";
                    break;
                case "awardbusiness":
                case "awardbusinessfirst":
                    cabin = "AWARDBUSINESSFIRST";
                    break;
                case "awardfirst":
                    cabin = "AWARDFIRST";
                    break;
                default:
                    cabin = "ECONOMY";
                    break;
            }

            string FareType = "";

            switch (fareType.Trim().ToLower())
            {
                case "lf":
                    FareType = "";
                    break;
                case "ff":
                    FareType = "-FLEXIBLE";
                    break;
                case "urf":
                    FareType = "-UNRESTRICTED";
                    break;
                default:
                    FareType = "";
                    break;
            }

            return cabin + FareType;
        }

        private List<MOBSearchFilterItem> processNearbyAirports(List<MOBSearchFilterItem> airportList)
        {
            var newAirportList = new List<MOBSearchFilterItem>();

            Dictionary<string, decimal> minPrice = new Dictionary<string, decimal>();
            Dictionary<string, MOBSearchFilterItem> minPriceItem = new Dictionary<string, MOBSearchFilterItem>();

            foreach (MOBSearchFilterItem item in airportList)
            {
                string key = item.Key.Trim().ToUpper();

                decimal temp = decimal.MaxValue;
                decimal.TryParse(item.Amount, out temp);

                if (minPrice.ContainsKey(key))
                {
                    if (minPrice[key] < temp)
                    {
                        minPrice[key] = temp;
                        minPriceItem[key] = item;
                    }
                }
                else
                {
                    minPrice.Add(key, temp);
                    minPriceItem.Add(key, item);
                }
            }

            newAirportList = minPriceItem.Values.ToList<MOBSearchFilterItem>();

            return newAirportList.Count > 1 ? newAirportList : null;
        }

        private string processNearbyAirports(string airportList)
        {
            HashSet<string> airports = new HashSet<string>(airportList.Trim().Split(",".ToCharArray()));

            string newAirportList = string.Empty;
            foreach (string s in airports)
            {
                if (string.IsNullOrEmpty(newAirportList))
                    newAirportList = s;
                else
                    newAirportList += "," + s;
            }

            return newAirportList;
        }

        private List<Model.Shopping.MOBSHOPFlattenedFlight> GetFlattendFlights(List<Model.Shopping.MOBSHOPFlight> flights, string tripId, string productId, string tripDate, MOBSHOPShopRequest shopRequest = null, int tripIndex = -1)
        {
            #region
            List<Model.Shopping.MOBSHOPFlattenedFlight> flattendFlights = new List<Model.Shopping.MOBSHOPFlattenedFlight>();
            foreach (var flight in flights)
            {
                #region
                Model.Shopping.MOBSHOPFlattenedFlight flattenedFlight = new Model.Shopping.MOBSHOPFlattenedFlight();

                bool isUADiscount = false;

                if (flight.ShoppingProducts != null)
                {
                    foreach (var product in flight.ShoppingProducts)
                    {
                        if (_shoppingUtility.EnableIBEFull() && !flattenedFlight.IsIBE && product.IsIBE)
                        {
                            flattenedFlight.IsIBE = true;
                        }

                        if (product.IsUADiscount)
                        {
                            isUADiscount = product.IsUADiscount;
                            break;
                        }

                        if (_shoppingUtility.EnableIBELite() && !flattenedFlight.IsIBELite && product.IsIBELite) // set only once
                        {
                            flattenedFlight.IsIBELite = true;
                        }
                    }
                }

                if (_shoppingUtility.EnableIBELite() || _shoppingUtility.EnableIBEFull())
                {
                    flattenedFlight.FlightHash = flight.FlightHash;
                }
                if (shopRequest != null)
                {
                    if (_shoppingUtility.EnableFSRLabelTexts(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                    {

                        if (flattenedFlight.FlightLabelTextList == null)
                            flattenedFlight.FlightLabelTextList = new List<string>();

                        if (!_shoppingUtility.CheckFSRRedesignFromShopRequest(shopRequest))
                        {
                            if (_configuration.GetValue<bool>("ChangeFeeWaiverMessagingToggle"))
                            {
                                if (!flattenedFlight.IsChangeFeeWaiver && tripIndex == 0)
                                {
                                    if (!string.IsNullOrEmpty(_configuration.GetValue<string>("LabelTextChangeFeeWaiver")))
                                    {
                                        flattenedFlight.FlightLabelTextList.Add(_configuration.GetValue<string>("LabelTextChangeFeeWaiver"));
                                        flattenedFlight.IsChangeFeeWaiver = true;
                                    }
                                }
                            }
                        }
                        if (_shoppingUtility.EnableCovidTestFlightShopping(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                        {
                            if (!flattenedFlight.IsCovidTestFlight && (flight.IsCovidTestFlight))
                            //if (!flattenedFlight.IsCovidTestFlight)
                            {
                                if (_shoppingUtility.CheckFSRRedesignFromShopRequest(shopRequest))
                                {
                                    flattenedFlight.FlightBadges = setFlightBadgeInformation(flattenedFlight.FlightBadges, CovidTestBadge());
                                }
                                else
                                {
                                    flattenedFlight.FlightLabelTextList.Add(_configuration.GetValue<string>("LabelTextCovidTest"));
                                }
                                flattenedFlight.IsCovidTestFlight = true;
                            }
                        }
                    }
                }
                flattenedFlight.IsUADiscount = isUADiscount;

                flattenedFlight.TripId = flight.TripId;
                flattenedFlight.FlightId = flight.FlightId;
                flattenedFlight.ProductId = productId;
                flattenedFlight.Flights = new List<Model.Shopping.MOBSHOPFlight>();
                flight.TripId = tripId; // trip.TripId;
                flight.FlightDepartureDays = string.Empty;
                flight.FlightArrivalDays = GetDayDifference(flight.DepartureDateTime, flight.ArrivalDateTime);
                bool flightDateChanged = false;
                flight.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDate, flight.ArrivalDateTime, ref flightDateChanged);
                // Added by Madhavi on 22/Sep/2015
                flight.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDate, flight.DepartureDateTime, ref flightDateChanged);
                flight.FlightDateChanged = flightDateChanged;
                flattenedFlight.Flights.Add(flight);
                if (_configuration.GetValue<bool>("EnableChangeOfAirport"))
                {
                    flattenedFlight.airportChange = flight.AirportChange;
                }

                if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                {
                    // Make a copy of flight.Connections and release flight.Connections
                    List<Model.Shopping.MOBSHOPFlight> connections = flight.StopInfos.Clone();
                    flight.StopInfos = null;
                    int cnt = 0;
                    foreach (var connection in connections)
                    {
                        if (cnt == 0)
                        {
                            connection.FlightDepartureDays = GetDayDifference(flight.ArrivalDateTime,
                                connection.DepartureDateTime);
                        }
                        else
                        {
                            connection.FlightDepartureDays = GetDayDifference(connections[cnt - 1].ArrivalDateTime,
                                connection.DepartureDateTime);
                        }
                        connection.IsStopOver = true;
                        connection.FlightArrivalDays = GetDayDifference(tripDate, connection.ArrivalDateTime);
                        connection.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDate,
                            connection.ArrivalDateTime, ref flightDateChanged);
                        connection.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDate,
                            connection.DepartureDateTime, ref flightDateChanged);
                        connection.FlightDateChanged = flightDateChanged;
                        connection.TripId = tripId; //trip.TripId;

                        if (shopRequest != null)
                        {
                            if (_shoppingUtility.EnableFSRLabelTexts(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                            {

                                if (flattenedFlight.FlightLabelTextList == null)
                                    flattenedFlight.FlightLabelTextList = new List<string>();
                                if (_shoppingUtility.EnableCovidTestFlightShopping(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                                {
                                    if (!flattenedFlight.IsCovidTestFlight && (connection.IsCovidTestFlight))
                                    {
                                        if (_shoppingUtility.CheckFSRRedesignFromShopRequest(shopRequest))
                                        {
                                            flattenedFlight.FlightBadges = setFlightBadgeInformation(flattenedFlight.FlightBadges, CovidTestBadge());
                                        }
                                        else
                                        {
                                            flattenedFlight.FlightLabelTextList.Add(_configuration.GetValue<string>("LabelTextCovidTest"));
                                        }
                                        flattenedFlight.IsCovidTestFlight = true;
                                    }
                                }
                            }
                        }

                        flattenedFlight.Flights.Add(connection);
                        cnt++;
                    }
                }

                if (flight.Connections != null && flight.Connections.Count > 0)
                {
                    // Make a copy of flight.Connections and release flight.Connections
                    List<Model.Shopping.MOBSHOPFlight> connections = flight.Connections.Clone();
                    flight.Connections = null;
                    int cnt = 0;
                    foreach (var connection in connections)
                    {
                        if (cnt == 0)
                        {
                            connection.FlightDepartureDays = GetDayDifference(flight.ArrivalDateTime,
                                connection.DepartureDateTime);
                        }
                        else
                        {
                            connection.FlightDepartureDays = GetDayDifference(connections[cnt - 1].ArrivalDateTime,
                                connection.DepartureDateTime);
                        }
                        connection.FlightArrivalDays = GetDayDifference(tripDate, connection.ArrivalDateTime);
                        connection.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDate,
                            connection.ArrivalDateTime, ref flightDateChanged);
                        connection.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDate,
                            connection.DepartureDateTime, ref flightDateChanged);
                        connection.FlightDateChanged = flightDateChanged;
                        connection.TripId = tripId; //trip.TripId;

                        if (shopRequest != null)
                        {
                            if (_shoppingUtility.EnableFSRLabelTexts(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                            {

                                if (flattenedFlight.FlightLabelTextList == null)
                                    flattenedFlight.FlightLabelTextList = new List<string>();

                                if (_shoppingUtility.EnableCovidTestFlightShopping(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                                {
                                    if (!flattenedFlight.IsCovidTestFlight && (connection.IsCovidTestFlight))
                                    {
                                        if (_shoppingUtility.CheckFSRRedesignFromShopRequest(shopRequest))
                                        {
                                            flattenedFlight.FlightBadges = setFlightBadgeInformation(flattenedFlight.FlightBadges, CovidTestBadge());
                                        }
                                        else
                                        {
                                            flattenedFlight.FlightLabelTextList.Add(_configuration.GetValue<string>("LabelTextCovidTest"));
                                        }
                                        flattenedFlight.IsCovidTestFlight = true;
                                    }
                                }
                            }
                        }
                        flattenedFlight.Flights.Add(connection);
                        if (connection.StopInfos != null && connection.StopInfos.Count > 0)
                        {
                            // Make a copy of flight.Connections and release flight.Connections
                            List<Model.Shopping.MOBSHOPFlight> connectionStops = connection.StopInfos.Clone();
                            connection.StopInfos = null;
                            int csnt = 0;
                            foreach (var conn in connectionStops)
                            {
                                if (csnt == 0)
                                {
                                    conn.FlightDepartureDays = GetDayDifference(connection.ArrivalDateTime,
                                        conn.DepartureDateTime);
                                }
                                else
                                {
                                    conn.FlightDepartureDays =
                                        GetDayDifference(connectionStops[csnt - 1].ArrivalDateTime,
                                            conn.DepartureDateTime);
                                }
                                conn.IsStopOver = true;
                                conn.FlightArrivalDays = GetDayDifference(tripDate, conn.ArrivalDateTime);
                                conn.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDate, conn.ArrivalDateTime,
                                    ref flightDateChanged);
                                conn.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDate, conn.DepartureDateTime,
                                    ref flightDateChanged);
                                conn.FlightDateChanged = flightDateChanged;
                                conn.TripId = tripId; //trip.TripId;

                                if (shopRequest != null)
                                {
                                    if (_shoppingUtility.EnableFSRLabelTexts(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                                    {

                                        if (flattenedFlight.FlightLabelTextList == null)
                                            flattenedFlight.FlightLabelTextList = new List<string>();

                                        if (_shoppingUtility.EnableCovidTestFlightShopping(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                                        {
                                            if (!flattenedFlight.IsCovidTestFlight && (conn.IsCovidTestFlight))
                                            {
                                                if (_shoppingUtility.CheckFSRRedesignFromShopRequest(shopRequest))
                                                {
                                                    flattenedFlight.FlightBadges = setFlightBadgeInformation(flattenedFlight.FlightBadges, CovidTestBadge());
                                                }
                                                else
                                                {
                                                    flattenedFlight.FlightLabelTextList.Add(_configuration.GetValue<string>("LabelTextCovidTest"));
                                                }
                                                flattenedFlight.IsCovidTestFlight = true;
                                            }
                                        }
                                    }
                                }

                                flattenedFlight.Flights.Add(conn);
                                csnt++;
                            }
                        }
                        cnt++;
                    }
                }

                if (flight.ShoppingProducts != null && flight.ShoppingProducts.Count > 0)
                {
                    int idx = 0;
                    foreach (Model.Shopping.MOBSHOPShoppingProduct prod in flight.ShoppingProducts)
                    {
                        if (prod.IsMixedCabin)
                        {
                            prod.MixedCabinSegmentMessages = GetFlightMixedCabinSegments(flattenedFlight.Flights,
                                idx);
                            prod.IsSelectedCabin = GetSelectedCabinInMixedScenario(flattenedFlight.Flights, idx);

                            prod.ProductDetail.ProductCabinMessages =
                                GetProductDetailMixedCabinSegments(flattenedFlight.Flights, idx);
                        }

                        if (!string.IsNullOrEmpty(flight.AddCollectProductId))
                        {
                            if (prod.ProductId == flight.AddCollectProductId && prod.IsSelectedCabin)
                            {
                                flattenedFlight.isAddCollectWaived = flight.isAddCollectWaived;
                                flattenedFlight.AddCollectProductId = flight.AddCollectProductId;
                            }
                        }

                        idx++;
                    }
                }

                flattenedFlight.TripDays = GetDayDifference(flattenedFlight.Flights[0].DepartDate, flattenedFlight.Flights[flattenedFlight.Flights.Count - 1].DestinationDate);
                bool isNotCrashFix = Convert.ToBoolean(_configuration.GetValue<string>("ByPassBug106828Fix") ?? "False");

                if (_configuration.GetValue<bool>("EnableOntimePerformance21FFix"))
                {
                    if (flattenedFlight?.Flights?.Any(f => !string.IsNullOrEmpty(f.OperatingCarrierDescription) && !f.OperatingCarrierDescription.Equals("United Airlines")) ?? false)
                    {
                        flattenedFlight?.Flights?.Where(f => !string.IsNullOrEmpty(f.OperatingCarrierDescription) || !f.OperatingCarrierDescription.Equals("United Airlines"))?.Select(f => f?.OperatingCarrierDescription)?.ToList().Where(s => !string.IsNullOrEmpty(s))?.Distinct()?.ForEach(c => flattenedFlight.MsgFlightCarrier += c + ", ");

                        if (!string.IsNullOrEmpty(flattenedFlight.MsgFlightCarrier))
                        {
                            if (flattenedFlight?.Flights?.Any(f => f.OperatingCarrier == "UA" && (string.IsNullOrEmpty(f.OperatingCarrierDescription) || f.OperatingCarrierDescription.Equals("United Airlines"))) ?? false)
                            {
                                flattenedFlight.MsgFlightCarrier = "Includes Travel Operated By " + flattenedFlight.MsgFlightCarrier.TrimEnd(',', ' ');
                            }
                            else
                            {
                                flattenedFlight.MsgFlightCarrier = "Operated By " + flattenedFlight.MsgFlightCarrier.TrimEnd(',', ' ');
                            }
                        }
                    }
                }

                if (isNotCrashFix)
                {
                    flattendFlights.Add(flattenedFlight);
                }
                else
                {

                    GetFlattenedFlightsWithPrices(flattenedFlight, flattendFlights);
                }

                #endregion
            }

            return flattendFlights;
            #endregion
        }

        private Model.Shopping.MOBStyledText CovidTestBadge()
        {
            return new Model.Shopping.MOBStyledText()
            {
                BackgroundColor = MOBStyledColor.Yellow.GetDescription(),
                Text = _configuration.GetValue<string>("LabelTextCovidTest"),
                SortPriority = MOBFlightBadgeSortOrder.CovidTestRequired.ToString()
            };
        }

        private void GetFlattenedFlightsWithPrices(Model.Shopping.MOBSHOPFlattenedFlight flattenedFlight, List<Model.Shopping.MOBSHOPFlattenedFlight> flattendFlights)
        {
            bool isAddFlightToFlattenedFlights = flattenedFlight.Flights.All(flight => !flattenedFlight.Flights[0].AirfareDisplayValue.IsNullOrEmpty());

            if (isAddFlightToFlattenedFlights)
            {
                flattendFlights.Add(flattenedFlight);
            }
        }

        private List<Model.Shopping.MOBStyledText> setFlightBadgeInformation(List<Model.Shopping.MOBStyledText> badges, Model.Shopping.MOBStyledText badge)
        {
            if (badges == null)
                badges = new List<Model.Shopping.MOBStyledText>();

            badges.Add(badge);

            if (badges.Count > 1)
            {
                badges = badges.OrderBy(x => (int)Enum.Parse(typeof(MOBFlightBadgeSortOrder), x.SortPriority)).ToList();
            }

            return badges;
        }

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

        private List<Model.Shopping.MOBSHOPShoppingProduct> PopulateColumns(ColumnInformation columnInfo, bool getFlightsWithStops, Session session)
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


            //Shop non-stop change adding shop 1 call prod type to columns, if not exist
            if (!session.IsFSRRedesign)
            {
                if (_configuration.GetValue<bool>("EnableNonStopFlight"))
                {
                    if (getFlightsWithStops)
                    {
                        LatestShopAvailabilityResponse persistAvailability = new LatestShopAvailabilityResponse();
                        persistAvailability = _sessionHelperService.GetSession<LatestShopAvailabilityResponse>(Headers.ContextValues, persistAvailability.ObjectName).Result;
                        //persistAvailability = United.Persist.FilePersist.Load<United.Persist.Definition.Shopping.LatestShopAvailabilityResponse>(session.SessionId, persistAvailability.ObjectName);
                        if (persistAvailability != null &&
                            !persistAvailability.AvailabilityList.IsNullOrEmpty() &&
                            persistAvailability.AvailabilityList["1"].Trip != null &&
                            !persistAvailability.AvailabilityList["1"].Trip.Columns.IsNullOrEmpty() &&
                            !columns.IsNullOrEmpty()
                            )
                        {
                            foreach (var persitColumn in persistAvailability.AvailabilityList["1"].Trip.Columns)
                            {
                                if (!columns.Exists(p => p.Type == persitColumn.Type))
                                {
                                    columns.Add(persitColumn);
                                }
                            }
                        }
                    }
                }
            }
            return columns;
        }

        private bool NoCSLExceptions(List<United.Services.FlightShopping.Common.ErrorInfo> errors)
        {
            #region
            if (errors != null && errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    if (errors.Count == 1 && error.MinorCode.Trim() == "10036") //08/20/2016 - Venkat  : errors.Count == 1 to check If CSL Shop returns only one exception and its  "Request Time Out Exception" as per stogo this is like warning and we can keep Moving and ignore this error.
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
            #endregion
        }

        private SHOPAwardCalendar PopulateAwardCalendar(CalendarType calendar, string tripId, string productId)
        {
            SHOPAwardCalendar awardCalendar = new SHOPAwardCalendar();
            awardCalendar.AwardCalendarItems = new List<AwardCalendarItem>();
            awardCalendar.ProductId = productId;
            awardCalendar.TripId = tripId;

            if (calendar != null && calendar.Months != null && calendar.Months.Count > 0)
            {
                foreach (CalendarMonth month in calendar.Months)
                {
                    foreach (CalendarWeek week in month.Weeks)
                    {
                        foreach (CalendarDay day in week.Days)
                        {
                            AwardCalendarItem item = new AwardCalendarItem();
                            item.Departs = DateTime.Parse(day.DateValue);

                            foreach (CalendarDaySolution solution in day.Solutions)
                            {
                                if (solution.AwardType.ToUpper() == "BUSINESS-SURPLUS")
                                    item.HasPremiumSaver = true;

                                if (solution.AwardType.ToUpper() == "ECONOMY-SURPLUS")
                                    item.HasEconomySaver = true;

                            }

                            awardCalendar.AwardCalendarItems.Add(item);
                        }
                    }
                }
            }
            if (awardCalendar.AwardCalendarItems != null && awardCalendar.AwardCalendarItems.Count < 1)
            {
                awardCalendar = null;
            }
            return awardCalendar;
        }

        private bool IsLastTripFSR(bool isReshopChange, MOBSHOPAvailability availability, List<United.Services.FlightShopping.Common.Trip> trips)
        {
            bool isLastTripFSR = false;
            if (isReshopChange)
            {

                if (availability.Reservation == null)
                {
                    availability.Reservation = new MOBSHOPReservation();
                }

                isLastTripFSR = (trips.Where(t => (t.ChangeType == United.Services.FlightShopping.Common.Types.ChangeTypes.ChangeTrip ||
                                                   t.ChangeType == United.Services.FlightShopping.Common.Types.ChangeTypes.NewTrip)).Count() == 0);

                Reservation bookingPathReservation = new Reservation();
                bookingPathReservation = _sessionHelperService.GetSession<Reservation>(Headers.ContextValues, bookingPathReservation.ObjectName).Result;
                //bookingPathReservation = United.Persist.FilePersist.Load<Reservation>(availability.SessionId, bookingPathReservation.ObjectName, false);
                availability.Reservation.Reshop = bookingPathReservation.Reshop;
                availability.Reservation.IsReshopChange = bookingPathReservation.IsReshopChange;
                if (isLastTripFSR)
                {
                    GetOldEMailID(availability.SessionId, bookingPathReservation);
                }


                availability.Reservation.Reshop.IsLastTripFSR = isLastTripFSR;
            }
            return isLastTripFSR;
        }

        private void SetTitleForFSRPage(MOBSHOPAvailability availability, MOBSHOPShopRequest shopRequest)
        {
            if (_shoppingUtility.CheckFSRRedesignFromShopRequest(shopRequest))
            {
                if (availability != null && availability.Trip != null && !string.IsNullOrEmpty(shopRequest.SearchType) && shopRequest.Trips != null && !string.IsNullOrEmpty(availability.Trip.OriginDecoded)
                && !string.IsNullOrEmpty(availability.Trip.DestinationDecoded) && !string.IsNullOrEmpty(availability.Trip.DepartDate))
                {
                    try
                    {
                        if (shopRequest.Trips.Any(t => t.DestinationAllAirports == 1 || t.OriginAllAirports == 1 || t.SearchNearbyOriginAirports || t.SearchNearbyDestinationAirports))
                        {
                            availability.Title = _configuration.GetValue<string>("FSRRedesignTitleForNoREsults") ?? "Select flights";
                        }
                        else
                        {
                            availability.Title = availability.Trip.OriginDecoded.Split(',')[0] + " to " + availability.Trip.DestinationDecoded.Split(',')[0];
                        }

                        string taveler = availability.TravelerCount <= 1 ? Convert.ToString(availability.TravelerCount) + " traveler" : Convert.ToString(availability.TravelerCount) + " travelers";
                        string date = Convert.ToDateTime(availability.Trip.DepartDate).ToString("ddd MMM d", CultureInfo.CreateSpecificCulture("en-US"));
                        string tripCount = shopRequest.Trips.Count.ToString();
                        string isLastTripIndex = Convert.ToString(availability.Trip.LastTripIndexRequested);
                        string searchType = shopRequest.SearchType.Equals("OW", StringComparison.OrdinalIgnoreCase) ? "One-way" : shopRequest.SearchType.Equals("RT", StringComparison.OrdinalIgnoreCase) ? "RoundTrip" : "Flight " + isLastTripIndex + " of " + tripCount;
                        var joiner = $" {(char)8226} "; //" • "
                        availability.SubTitle = string.Join(joiner, searchType, taveler, date);
                    }
                    catch (Exception ex)
                    {
                        availability.Title = _configuration.GetValue<string>("FSRRedesignTitleForNoREsults") ?? "Select flights";
                        availability.SubTitle = string.Empty;
                    }
                }
            }
        }
        private void GetOldEMailID(string sessionid, Reservation persistedReservation)
        {
            MOBEmail oldEmail = new MOBEmail();
            oldEmail = _sessionHelperService.GetSession<MOBEmail>(Headers.ContextValues, "United.Definition.MOBEmail").Result;
            //oldEmail = Persist.FilePersist.Load<MOBEmail>(sessionid, oldEmail.GetType().ToString(), false);
            if (oldEmail != null)
            {
                persistedReservation.ReservationEmail = oldEmail;
            }
            if (persistedReservation.CreditCardsAddress.Count > 0 && persistedReservation.IsSignedInWithMP)
            {
                persistedReservation.Reshop.RefundAddress = new Model.Common.MOBAddress();
                persistedReservation.Reshop.RefundAddress = persistedReservation.CreditCardsAddress[0];
                if (persistedReservation.CreditCards.Count > 0)
                {
                    persistedReservation.CreditCardsAddress.Clear();
                    persistedReservation.CreditCards.Clear();
                    persistedReservation.ReservationPhone = null;
                }
            }
            else
            {
                persistedReservation.Reshop.RefundAddress = null;
                if (persistedReservation.CreditCards.Count > 0)
                {
                    persistedReservation.CreditCardsAddress.Clear();
                    persistedReservation.CreditCards.Clear();
                    persistedReservation.ReservationPhone = null;
                }
            }
            _sessionHelperService.SaveSession<Reservation>(persistedReservation, Headers.ContextValues, persistedReservation.ObjectName);
            //FilePersist.Save<Reservation>(sessionid, persistedReservation.ObjectName, persistedReservation);
        }
        private MOBResReservation GetShopBookingDetailsV2(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string sessionId, United.Services.FlightShopping.Common.ShopSelectRequest request, ref MOBSHOPAvailability availability, SelectTripRequest selectTripRequest, ShoppingResponse persistShop)
        {

            Session session = new Session();
            session = _sessionHelperService.GetSession<Session>(Headers.ContextValues, session.ObjectName).Result;
            //session = Persist.FilePersist.Load<Session>(sessionId, session.ObjectName);

            string logAction = session.IsReshopChange ? "ReShopBookingDetailsv2" : "ShopBookingDetailsv2";
            //string cslEndpoint = GetCslEndpointForShopping(session.IsReshopChange);

            MOBResReservation reservation = null;
            //string url = GetShopBookingDetailsUrl(selectRequest);

            if (session == null)
            {
                throw new UnitedException("Could not find your booking session.");
            }
            request.DeviceId = selectTripRequest.DeviceId;
            string jsonRequest = JsonConvert.SerializeObject(request);
            _sessionHelperService.SaveSession<string>(jsonRequest, Headers.ContextValues);
            //Persist.FilePersist.Save<string>(sessionId, typeof(ShopSelectRequest).FullName, jsonRequest);

            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, logAction, "Request", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, jsonRequest));

            //string url = string.Format("{0}/{1}", cslEndpoint, logAction);
            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, logAction, "URL", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, url));

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cSLCallDurationstopwatch1;
            cSLCallDurationstopwatch1 = new Stopwatch();
            cSLCallDurationstopwatch1.Reset();
            cSLCallDurationstopwatch1.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******
            string token = string.IsNullOrEmpty(session.Token) ? _dPService.GetAnonymousToken(Headers.ContextValues.Application.Id, Headers.ContextValues.DeviceId, _configuration).Result : session.Token;
            var shopBookingDetailsResponse = _flightShoppingService.ShopBookingDetails<FlightReservationResponse>(token, Headers.ContextValues.SessionId, logAction, jsonRequest).Result;
            //string jsonResponse = HttpHelper.Post(url, "application/json; charset=utf-8", session.Token, jsonRequest, httpPostTimeOut, httpPostNumberOfRetry);

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cSLCallDurationstopwatch1.IsRunning)
            {
                cSLCallDurationstopwatch1.Stop();
            }
            #endregion/***Get Call Duration Code - Venkat 03/17/2015*******
            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, logAction, "CSS/CSL-CallDuration", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, "CSLShopBookingDetails=" + (cSLCallDurationstopwatch1.ElapsedMilliseconds / (double)1000).ToString()));
            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, logAction, "Response", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, jsonResponse));

            Model.Shopping.ShoppingResponse shop = new Model.Shopping.ShoppingResponse();
            shop = _sessionHelperService.GetSession<Model.Shopping.ShoppingResponse>(Headers.ContextValues, shop.ObjectName).Result;
            //shop = United.Persist.FilePersist.Load<ShoppingResponse>(selectTripRequest.SessionId, shop.ObjectName);
            if (shop == null)
            {
                throw new UnitedException("Could not find your booking session.");
            }

            if (shopBookingDetailsResponse != null)
            {
                //FlightReservationResponse shopBookingDetailsResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(jsonResponse);
                //  United.Persist.FilePersist.Save<FlightReservationResponse>(session.SessionId, response.GetType().FullName, response);
                if (shopBookingDetailsResponse != null && shopBookingDetailsResponse.Status.Equals(StatusType.Success) && shopBookingDetailsResponse.Reservation != null)
                {
                    var response = new FlightReservationResponse();
                    response = RegisterFlights(shopBookingDetailsResponse, session, selectTripRequest);

                    #region Populate properties which are missing from RegisterFlights Response
                    AssignMissingPropertiesfromRegisterFlightsResponse(shopBookingDetailsResponse, response);
                    #endregion

                    if (session.IsAward && session.IsReshopChange)
                    {
                        if (response.DisplayCart != null 
                            && _shoppingUtility.IsBuyMilesFeatureEnabled(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                            ValidateAwardReshopMileageBalance(response.IsMileagePurchaseRequired);
                    }

                    reservation = PopulateReservation(sessionId, response.Reservation);

                    if (response.DisplayCart != null && response.DisplayCart.DisplayPrices != null)
                    {
                        if (availability.Reservation.ShopReservationInfo2 == null)
                        {
                            availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();
                        }
                        availability.Reservation.IsSSA = _shoppingUtility.EnableSSA(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);
                        availability.Reservation.IsELF = response.DisplayCart.IsElf;
                        availability.Reservation.ShopReservationInfo2.IsIBELite = _shoppingUtility.IsIBELiteFare(response.DisplayCart);
                        availability.Reservation.ShopReservationInfo2.IsIBE = _shoppingUtility.IsIBEFullFare(response.DisplayCart);
                        availability.Reservation.ShopReservationInfo2.Characteristics = ShopStaticUtility.GetCharacteristics(response.Reservation);
                        availability.Reservation.ELFMessagesForRTI = _eLFRitMetaShopMessages.GetELFShopMessagesForRestrictions(availability.Reservation);
                        availability.Reservation.IsUpgradedFromEntryLevelFare = response.DisplayCart.IsUpgradedFromEntryLevelFare && !_shoppingUtility.IsIBELiteFare(response.DisplayCart.ProductCodeBeforeUpgrade);
                        availability.Reservation.PointOfSale = shop.Request.CountryCode;
                        string fareClass = GetFareClassAtShoppingRequestFromPersist(sessionId);
                        List<string> flightDepartDatesForSelectedTrip = new List<string>();
                        if (availability.Reservation.Trips == null) { }
                        else
                        {
                            foreach (MOBSHOPTrip shopTrip in availability.Reservation.Trips)
                            {
                                flightDepartDatesForSelectedTrip.Add(shopTrip.TripId + "|" + shopTrip.DepartDate);
                            }
                        }
                        if (_mOBSHOPDataCarrier == null)
                            _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                        availability.Reservation.Trips = PopulateTrips(_mOBSHOPDataCarrier, response.CartId, persistShop, selectTripRequest.SessionId, selectTripRequest.Application.Id, selectTripRequest.DeviceId, selectTripRequest.Application.Version.Major, shop.Request.ShowMileageDetails, response.DisplayCart.DisplayTrips, fareClass, flightDepartDatesForSelectedTrip);
                        if (_shoppingUtility.IsEnableTaxForAgeDiversification(shop.Request.IsReshopChange, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                        {
                            availability.Reservation.Prices = _shoppingUtility.GetPrices(response.DisplayCart.DisplayPrices, availability.AwardTravel, session.SessionId,
                                session.IsReshopChange, availability.Reservation.SearchType, displayCart: response.DisplayCart,appId: selectTripRequest.Application.Id,
                                appVersion: selectTripRequest.Application.Version.Major, catalogItems: selectTripRequest.CatalogItems, shopBookingDetailsResponse: response,
                                displayFees: response.DisplayCart.DisplayFees);
                        }
                        else
                        {
                            availability.Reservation.Prices = GetPrices(response.DisplayCart.DisplayPrices, availability.AwardTravel, session.SessionId, session.IsReshopChange,
                                selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, catalogItems: selectTripRequest.CatalogItems, shopBookingDetailsResponse: response);

                        }
                        bool isAdvanceSearchCoupon = _shoppingUtility.EnableAdvanceSearchCouponBooking(shop.Request.Application.Id, shop.Request.Application.Version.Major);
                        AssignCorporateRate(availability.Reservation, shop.Request.IsCorporateBooking, session.IsArrangerBooking);
                        if (_shoppingUtility.EnableCovidTestFlightShopping(shop.Request.Application.Id, shop.Request.Application.Version.Major, session.IsReshopChange))
                        {
                            ShopStaticUtility.AssignCovidTestIndicator(availability.Reservation);
                        }
                        if (_configuration.GetValue<bool>("EnableIsArranger") && session.IsArrangerBooking)
                        {
                            if (availability.Reservation.ShopReservationInfo2 == null)
                                availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();

                            availability.Reservation.ShopReservationInfo2.IsArrangerBooking = true;
                        }
                        bool Is24HoursWindow = false;
                        if (_configuration.GetValue<bool>("EnableForceEPlus"))
                        {
                            if (availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT != null)
                            {
                                Is24HoursWindow = TopHelper.Is24HourWindow(Convert.ToDateTime(availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT));
                            }
                        }

                        availability.Reservation.ShopReservationInfo2.IsForceSeatMap = _shoppingUtility.IsForceSeatMapforEPlus(shop.Request.IsReshopChange, response.DisplayCart.IsElf, Is24HoursWindow, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);
                        bool isSupportedVersion = GeneralHelper.IsApplicationVersionGreater(shop.Request.Application.Id, shop.Request.Application.Version.Major, "AndroidBundleVersion", "IOSBundleVersion", "", "", true, _configuration);
                        if (isSupportedVersion)
                        {
                            if (_configuration.GetValue<bool>("IsEnableBundlesForBasicEconomyBooking"))
                            {
                                availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles") && !shop.Request.IsReshopChange;
                            }
                            else
                            {
                                availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles") && !(response.DisplayCart.IsElf || shop.Request.IsReshopChange || _shoppingUtility.IsIBEFullFare(response.DisplayCart));
                            }
                        }

                        if (shop.Request.IsReshopChange)
                        {
                            availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = false;
                            availability.Reservation.ShopReservationInfo2.ShouldHideBackButton = false;
                        }

                        #region 159514 - Added for inhibit booking message,177113 - 179536 BE Fare Inversion and stacking messages  

                        if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                        {
                            if (ShopStaticUtility.IdentifyInhibitWarning(response))
                            {
                                if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                                if (!availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.INHIBITBOOKING.ToString()))
                                {
                                    //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - Response with Inhibit warning ", "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, jsonResponse));
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();
                                    if (!_configuration.GetValue<bool>("TurnOffBookingCutoffMinsFromCSL"))
                                    {
                                        string bookingCutOffminsFromCSL = (response?.DisplayCart?.BookingCutOffMinutes > 0) ? response.DisplayCart.BookingCutOffMinutes.ToString() : string.Empty;

                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetInhibitMessage(bookingCutOffminsFromCSL));
                                        availability.Reservation.ShopReservationInfo2.BookingCutOffMinutes = bookingCutOffminsFromCSL;

                                    }
                                    else
                                    {
                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetInhibitMessage(string.Empty));
                                    }


                                    if (_shoppingUtility.EnableBoeingDisclaimer(availability.Reservation.IsReshopChange))
                                    {
                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                                    }
                                }
                            }
                        }

                        if (_shoppingUtility.EnableBoeingDisclaimer(availability.Reservation.IsReshopChange) && _shoppingUtility.IsBoeingDisclaimer(response.DisplayCart.DisplayTrips))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                            if (!availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.BOEING737WARNING.ToString()))
                            {
                                //if (traceSwitch.TraceWarning)
                                //{
                                //    //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - Response with Inhibit warning ", "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, jsonResponse));
                                //}
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetBoeingDisclaimer());

                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                            }
                        }

                        if (_shoppingUtility.IsIBELiteFare(response.DisplayCart.ProductCodeBeforeUpgrade))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetIBELiteNonCombinableMessage());
                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                        }

                        ///202150 - Getting both messages for fare inversions trying to select mixed itinerary (Reported by Andrew)
                        ///Srini - 12/26/2017
                        ///This If condition, we can remove, when we take "BugFixToggleFor17M" toggle out and directly "response.DisplayCart.IsUpgradedFromEntryLevelFare" check to next if condition
                        if (!_configuration.GetValue<bool>("BugFixToggleFor17M") || (_configuration.GetValue<bool>("BugFixToggleFor17M") && response.DisplayCart.IsUpgradedFromEntryLevelFare))
                        {
                            if (_configuration.GetValue<bool>("EnableBEFareInversion"))
                            {

                                if (ShopStaticUtility.IdentifyBEFareInversion(response))
                                {
                                    if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetBEMessage());
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                                }
                            }
                        }
                        #endregion

                        _shoppingUtility.SetELFUpgradeMsg(availability, shop.Request.Application.Id);

                        if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                        {
                            availability.Reservation.Prices.AddRange(GetPrices(response.DisplayCart.DisplayFees, false, string.Empty, appId: selectTripRequest.Application.Id,
                                appVersion: selectTripRequest.Application.Version.Major, shopBookingDetailsResponse: response));
                        }
                        //need to add close in fee to TOTAL
                        availability.Reservation.Prices = AdjustTotal(availability.Reservation.Prices);
                        if (_shoppingUtility.IsEnableTaxForAgeDiversification(shop.Request.IsReshopChange, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();

                            availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = _shoppingUtility.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices, session.IsReshopChange);

                            if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                            {
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes.Add(ShopStaticUtility.AddFeesAfterPriceChange(response.DisplayCart.DisplayFees));
                            }
                        }
                        else
                        {
                            if (_shoppingUtility.EnableReshopMixedPTC(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                            {
                                if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                                    availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();

                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = _shoppingUtility.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices, session.IsReshopChange);
                            }

                            availability.Reservation.Taxes = GetTaxAndFees(response.DisplayCart.DisplayPrices, shop.Request.NumberOfAdults, session.IsReshopChange);

                            if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                            {
                                //combine fees into taxes so that totals are correct
                                List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> tempList = new List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice>();
                                tempList.AddRange(response.DisplayCart.DisplayPrices);
                                tempList.AddRange(response.DisplayCart.DisplayFees);
                                availability.Reservation.Taxes = GetTaxAndFees(tempList, shop.Request.NumberOfAdults, session.IsReshopChange);
                            }

                            bool hasFareRefund = false;

                            if (response.DisplayCart != null && response.DisplayCart.TravelOptions != null && response.DisplayCart.TravelOptions.Any()
                                && response.DisplayCart.TravelOptions.Any(x => string.Equals(x.Status, "REFUND", StringComparison.OrdinalIgnoreCase)))
                            {
                                var traveloptionrefundobj = RefundAmountTravelOption(response.DisplayCart.TravelOptions);
                                var traveloptionrefund = traveloptionrefundobj.FirstOrDefault(x => string.Equals(x.DisplayType, "TOTALTRAVELOPTIONREFUND", StringComparison.OrdinalIgnoreCase));
                                var totalpricerefund = availability.Reservation.Prices.FirstOrDefault(x => string.Equals(x.DisplayType, "REFUNDPRICE", StringComparison.OrdinalIgnoreCase));

                                if (totalpricerefund != null)
                                {
                                    double tempDouble1 = 0;
                                    double.TryParse(totalpricerefund.DisplayValue, out tempDouble1);
                                    if (tempDouble1 > 0)
                                        hasFareRefund = true;
                                }

                                if (traveloptionrefund != null)
                                {
                                    decimal totalrefund = (traveloptionrefund != null && totalpricerefund != null) ?
                                        Convert.ToDecimal(traveloptionrefund.DisplayValue) + Convert.ToDecimal(totalpricerefund.DisplayValue) :
                                        Convert.ToDecimal(traveloptionrefund.DisplayValue);

                                    CultureInfo ci = null;
                                    double tempDouble = 0;
                                    double.TryParse(Convert.ToString(totalrefund), out tempDouble);
                                    if (ci == null) ci = TopHelper.GetCultureInfo(traveloptionrefund.CurrencyCode);

                                    if (totalpricerefund != null)
                                    {
                                        totalpricerefund.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);
                                        totalpricerefund.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalrefund, ci, false);
                                        totalpricerefund.DisplayValue = string.Format("{0:#,0.00}", totalrefund);
                                    }
                                    else
                                    {
                                        United.Mobile.Model.Shopping.MOBSHOPPrice totalPriceRefund = new United.Mobile.Model.Shopping.MOBSHOPPrice
                                        {
                                            CurrencyCode = traveloptionrefund.CurrencyCode,
                                            DisplayType = "REFUNDPRICE",
                                            Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero),
                                            FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalrefund, ci, false),
                                            DisplayValue = string.Format("{0:#,0.00}", totalrefund)

                                        };
                                        availability.Reservation.Prices.Add(totalPriceRefund);
                                    }

                                    if (traveloptionrefundobj.Any())
                                        availability.Reservation.Prices.AddRange(traveloptionrefundobj);
                                }
                            }

                            if (!hasFareRefund)
                            {
                                availability.Reservation.Prices.RemoveWhere
                                    (x => (string.Equals(x.DisplayType, "TAXDIFFERENCE", StringComparison.OrdinalIgnoreCase)));
                                availability.Reservation.Prices.RemoveWhere
                                    (x => (string.Equals(x.DisplayType, "FAREDIFFERENCE", StringComparison.OrdinalIgnoreCase)));
                            }
                        }

                        availability.Reservation.TravelOptions = GetTravelOptions(response.DisplayCart);

                        if (string.IsNullOrEmpty(session.EmployeeId))
                        {
                            if (availability.AwardTravel)
                            {
                                availability.Reservation.FareLock = GetFareLockOptions(response.FareLockResponse, GeTotalFromPrices(availability.Reservation.Prices), GetFareCurrency(response.DisplayCart.DisplayPrices), availability.AwardTravel, GetFareMiles(response.DisplayCart.DisplayPrices), selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);
                            }
                            else
                            {
                                availability.Reservation.FareLock = GetFareLockOptions(response.FareLockResponse, GetFarePrice(response.DisplayCart.DisplayPrices), GetFareCurrency(response.DisplayCart.DisplayPrices), availability.AwardTravel, GetFareMiles(response.DisplayCart.DisplayPrices), selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);
                            }
                        }
                        if (shopBookingDetailsResponse.IsMileagePurchaseRequired)
                        {
                           _shoppingBuyMiles.AddBuyMilesFeature(request, availability, selectTripRequest, session, shopBookingDetailsResponse, response);
                        }
                        //availability.PromoCodeRemoveAlertForProducts = ShopTripsBusiness.GetFareLockAdvanceSearchCouponWarningMessageWithAncillary(availability.Reservation.FareLock, isAdvanceSearchCoupon, response.DisplayCart);
                        availability.Reservation.AwardTravel = availability.AwardTravel;

                        if (!availability.AwardTravel)
                        {
                            availability.Reservation.LMXFlights = PopulateLMX(response.CartId, persistShop, selectTripRequest.SessionId, selectTripRequest.Application.Id, selectTripRequest.DeviceId, selectTripRequest.Application.Version.Major, shop.Request.ShowMileageDetails, response.DisplayCart.DisplayTrips);
                            availability.Reservation.IneligibleToEarnCreditMessage = _configuration.GetValue<string>("IneligibleToEarnCreditMessage");
                            availability.Reservation.OaIneligibleToEarnCreditMessage = _configuration.GetValue<string>("OaIneligibleToEarnCreditMessage");
                        }

                        availability.Reservation.IsCubaTravel = IsCubaTravelTrip(availability.Reservation);

                        if (availability.Reservation.IsCubaTravel)
                        {
                            MobileCMSContentRequest mobMobileCMSContentRequest = new MobileCMSContentRequest();
                            mobMobileCMSContentRequest.Application = selectTripRequest.Application;
                            mobMobileCMSContentRequest.Token = session.Token;
                            availability.Reservation.CubaTravelInfo = _travelerCSL.GetCubaTravelResons(mobMobileCMSContentRequest);
                            availability.Reservation.CubaTravelInfo.CubaTravelTitles = new MPDynamoDB(_configuration, _dynamoDBService,null).GetMPPINPWDTitleMessages(new List<string> { "CUBA_TRAVEL_CONTENT" });
                        }
                        if (_shoppingUtility.IsEnabledNationalityAndResidence(shop.Request.IsReshopChange, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();
                            availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence = IsRequireNatAndCR(availability.Reservation, selectTripRequest.Application, sessionId, selectTripRequest.DeviceId, session.Token);
                            if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence)
                            {
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.NationalityErrMsg = NationalityResidenceMsgs.NationalityErrMsg;
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ResidenceErrMsg = NationalityResidenceMsgs.ResidenceErrMsg;
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.NationalityAndResidenceErrMsg = NationalityResidenceMsgs.NationalityAndResidenceErrMsg;
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.NationalityAndResidenceHeaderMsg = NationalityResidenceMsgs.NationalityAndResidenceHeaderMsg;
                            }
                        }
                    }
                    availability.Reservation.ISFlexibleSegmentExist = IsOneFlexibleSegmentExist(availability.Reservation.Trips);
                    availability.Reservation.FlightShareMessage = GetFlightShareMessage(availability.Reservation, string.Empty);
                    availability.Reservation.IsRefundable = reservation.IsRefundable;
                    availability.Reservation.ISInternational = reservation.ISInternational;
                    //**RSA Publick Key Implmentaion*/

                    string pkDispenserPublicKey = _sessionHelperService.GetSession<string>(Headers.ContextValues, "pkDispenserPublicKey").Result;
                    //string pkDispenserPublicKey = FilePersist.Load<string>(_shoppingUtility.GetCSSPublicKeyPersistSessionStaticGUID(selectTripRequest.Application.Id), "pkDispenserPublicKey");
                    if (!string.IsNullOrEmpty(pkDispenserPublicKey))
                    {
                        availability.Reservation.PKDispenserPublicKey = pkDispenserPublicKey;
                    }
                    else
                    {
                        availability.Reservation.PKDispenserPublicKey = GetPkDispenserPublicKey(selectTripRequest.Application.Id, selectTripRequest.DeviceId, selectTripRequest.Application.Version.Major, sessionId, session.Token);
                    }
                    //**RSA Publick Key Implmentaion**//

                    //#region 214448 - Unaccompanied Minor Age
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo = new ReservationAgeBoundInfo();
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.MinimumAge = Convert.ToInt32(_configuration.GetValue<string>("umnrMinimumAge"));
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.UpBoundAge = Convert.ToInt32(_configuration.GetValue<string>("umnrUpBoundAge"));
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.ErrorMessage = _configuration.GetValue<string>("umnrErrorMessage");
                    //#endregion
                    availability.Reservation.CheckedbagChargebutton = availability.Reservation.IsReshopChange ? "" : _configuration.GetValue<string>("ViewCheckedBagChargesButton");
                    if (_shoppingUtility.EnableTravelerTypes(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, session.IsReshopChange)
                        && availability.Reservation.ShopReservationInfo2.TravelerTypes != null && availability.Reservation.ShopReservationInfo2.TravelerTypes.Count > 0)
                    {
                        availability.Reservation.ShopReservationInfo2.displayTravelTypes = ShopStaticUtility.GetDisplayTravelerTypes(response.DisplayCart.DisplayTravelers);
                    }
                    if (_shoppingUtility.EnableYoungAdult(session.IsReshopChange))
                    {
                        if (IsYATravel(response.DisplayCart.DisplayTravelers))
                        {
                            availability.Reservation.ShopReservationInfo2.IsYATravel = true;

                            if (availability.Reservation.ShopReservationInfo2.displayTravelTypes != null && availability.Reservation.ShopReservationInfo2.displayTravelTypes.Count > 0)
                                availability.Reservation.ShopReservationInfo2.displayTravelTypes[0].TravelerDescription = "Young adult (18-22)";
                        }
                    }

                    if (_configuration.GetValue<bool>("EnableCorporateLeisure"))
                    {
                        if (availability.Reservation.ShopReservationInfo2 == null)
                            availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();
                        availability.Reservation.ShopReservationInfo2.TravelType = shop.Request.TravelType;
                        if (session.TravelType == TravelType.CLB.ToString())
                        {
                            if (_shoppingUtility.IsCorporateLeisureFareSelected(availability.Reservation.Trips))
                            {
                                AddCorporateLeisureOptOutFSRAlert(availability.Reservation.ShopReservationInfo2, selectTripRequest, session);
                            }
                        }
                    }
                    #region Define Booking Path Persist Reservation and Save to session - Venkat 08/13/2014
                    var bookingPathReservation = CreateBookingPathReservation(sessionId, request, availability, shop, session, response);
                    _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, Headers.ContextValues, bookingPathReservation.ObjectName);
                    //FilePersist.Save(bookingPathReservation.SessionId, bookingPathReservation.ObjectName, bookingPathReservation);
                    #endregion
                    if (_shoppingUtility.EnableInflightContactlessPayment(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, bookingPathReservation.IsReshopChange))
                    {
                        FireForGetInFlightCLEligibilityCheck(bookingPathReservation, selectTripRequest, session);
                    }
                    var persistedShopBookingDetailsResponse = new ShopBookingDetailsResponse
                    {
                        SessionId = sessionId,
                        Reservation = response.Reservation
                    };

                    if (string.IsNullOrEmpty(session.EmployeeId))
                    {
                        persistedShopBookingDetailsResponse.FareLock = response.FareLockResponse;
                    }
                    _sessionHelperService.SaveSession<ShopBookingDetailsResponse>(persistedShopBookingDetailsResponse, Headers.ContextValues, persistedShopBookingDetailsResponse.ObjectName);

                    //FilePersist.Save(persistedShopBookingDetailsResponse.SessionId, persistedShopBookingDetailsResponse.ObjectName, persistedShopBookingDetailsResponse);
                }
                else
                {
                    if (shopBookingDetailsResponse.Errors != null && shopBookingDetailsResponse.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in shopBookingDetailsResponse.Errors)
                        {
                            errorMessage = errorMessage + " " + error.Message;

                            #region 159514 - Inhibit booking error
                            if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                            {
                                if (error != null && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10050"))
                                {
                                    //throw new UnitedException(error.Message);
                                    throw new UnitedException(error.Message, new Exception(error.MinorCode));
                                }
                            }
                            #endregion


                            //59249:Bug 323966: Flight Shopping (Mobile) - Item 9: CSL_Service\ShopBookingDetails Unhandled Exception: System.Exception - bookingDetail has no solutions being returned from ITA - Sep 22, 2016 - Vijayan
                            if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10047"))
                            {
                                throw new UnitedException(_configuration.GetValue<string>("BookingDetail_ITAError_For_CSL_10047__Error_Code").ToString());
                            }

                            //67660 Bug 345403 CSL - System.Exception - Input error for "revenueBookingDetails" (search), solutionId - Sep 30,2016 - Issuf 
                            if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10048"))
                            {
                                throw new UnitedException(_configuration.GetValue<string>("BookingDetailsErrorMessage_For_CSL_10048__Error_Code").ToString());
                            }

                            //59232 - Bug 323968: Flight Shopping (Mobile) - Item 11: CSL_Service\ShopBookingDetails Unhandled Exception: System.Exception - Ras Check Failed - Sep 12,2016 - Ravitheja G 
                            if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10045"))
                            {
                                List<string> lstFlightDetails = null;
                                if (_configuration.GetValue<string>("RasCheckFailedErrorMessageWithFlightNumber") != null && shopBookingDetailsResponse.DisplayCart != null)
                                {
                                    lstFlightDetails = new List<string>();
                                    if (shopBookingDetailsResponse.DisplayCart != null)
                                        if (shopBookingDetailsResponse.DisplayCart.DisplayTrips != null && shopBookingDetailsResponse.DisplayCart.DisplayTrips.Count > 0)
                                            foreach (var trips in shopBookingDetailsResponse.DisplayCart.DisplayTrips)
                                            {
                                                foreach (var flightDetails in trips.Flights)
                                                {
                                                    lstFlightDetails.Add(flightDetails.MarketingCarrierDescription + " flight " + flightDetails.FlightNumber);
                                                }
                                            }
                                    throw new UnitedException(string.Format(_configuration.GetValue<string>("RasCheckFailedErrorMessageWithFlightNumber").ToString(), string.Join(",", lstFlightDetails)));
                                }
                                else
                                {
                                    throw new UnitedException(_configuration.GetValue<string>("GenericRasCheckFailedErrorMessage").ToString());
                                }
                            }

                            // Added By Ali as part of Task 264624 : Select Trip - The Boombox user's session has expired
                            if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10036"))
                            {
                                throw new UnitedException(_configuration.GetValue<string>("BookingExceptionMessage_ServiceErrorSessionNotFound").ToString());
                            }

                            //  Added by Ali as part of Task 278032 : System.Exception:FLIGHTS NOT FOUND-No flight options were found for this trip.
                            if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10051"))
                            {
                                throw new UnitedException(_configuration.GetValue<string>("NoAvailabilityError").ToString());
                            }
                        }

                        throw new System.Exception(errorMessage);
                    }
                    else
                    {
                        throw new UnitedException("Failed to retrieve booking details.");
                    }
                }
            }
            else
            {
                throw new UnitedException("Failed to retrieve booking details.");
            }
            reservation.CallDuration = cSLCallDurationstopwatch1.ElapsedMilliseconds;
            return reservation;
        }

        private string GetPkDispenserPublicKey(int applicationId, string deviceId, string appVersion, string transactionId, string token)
        {
            //**RSA Publick Key Implmentaion**//
            string request = string.Empty, guidForLogEntries = transactionId, pkDispenserPublicKey = string.Empty;
            string path = string.Empty;
            #region 
            //HttpClient httpClient = new HttpClient();
            //httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["CSSPublicKeyDispenserEndPointURL"].ToString());
            ////List<Key> listKeys = new List<Key>();
            //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authToken);
            //HttpResponseMessage resp = httpClient.GetAsync(httpClient.BaseAddress + (String.Format("dispenser/key"))).Result; //6.0/security/pkdispenser
            //Assert.IsTrue(resp.IsSuccessStatusCode, "SuccessStatusCode is false");
            //United.Domain.PKDispenser.PKDispenserResponse response1 = resp.Content.ReadAsAsync<United.Domain.PKDispenser.PKDispenserResponse>().Result;
            //Assert.IsNotNull(response1.Keys, "List Key is null");
            //Assert.IsFalse(response1.Keys.Count == 0, "Key not found");
            #endregion
            #region


            //string url = string.Format("{0}/dispenser/key", ConfigurationManager.AppSettings["CSSPublicKeyDispenserEndPointURL"]);
            //logEntries.Add(LogEntry.GetLogEntry<string>(transactionId, "GetPkDispenserPublicKey - Request url ", "Trace", applicationId, appVersion, deviceId, url));

            token = string.IsNullOrEmpty(token) ? _dPService.GetAnonymousToken(Headers.ContextValues.Application.Id, Headers.ContextValues.DeviceId, _configuration).Result : token;

            var response = _pKDispenserService.GetPkDispenserPublicKey<PKDispenserResponse>(token, Headers.ContextValues.SessionId, path).Result;
            //string jsonResponse = HttpHelper.Get(url, "application/json; charset=utf-8", authToken);

            //logEntries.Add(LogEntry.GetLogEntry<string>(transactionId, "GetPkDispenserPublicKey - Response", "Trace", applicationId, appVersion, deviceId, jsonResponse));

            if (response != null)
            {
                //PKDispenserResponse response = null;

                //response = JsonConvert.DeserializeObject<PKDispenserResponse>(jsonResponse);

                if (response != null && response.Keys != null && response.Keys.Count > 0)
                {
                    //logEntries.Add(LogEntry.GetLogEntry<United.Domain.PKDispenser.PKDispenserResponse>(transactionId, "GetPkDispenserPublicKey - DeSerialized Response", "DeSerialized Response", applicationId, appVersion, deviceId, response));
                    //if (applicationId == 3)
                    //{
                    //    var obj = (from st in response.Keys
                    //               where st.CryptoTypeId == 1
                    //               select st).ToList();
                    //    pkDispenserPublicKey = obj[0].PublicKey;
                    //}
                    //else
                    //{
                    var obj = (from st in response.Keys
                               where st.CryptoTypeId == 2
                               select st).ToList();
                    pkDispenserPublicKey = obj[0].PublicKey;
                    //}
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

            //logEntries.Add(LogEntry.GetLogEntry<string>(transactionId, "GetPkDispenserPublicKey - Client Response", "to client Response", applicationId, appVersion, deviceId, pkDispenserPublicKey));
            #endregion
            //if (applicationId != 3)
            //{
            pkDispenserPublicKey = pkDispenserPublicKey.Replace("\r", "").Replace("\n", "").Replace("-----BEGIN PUBLIC KEY-----", "").Replace("-----END PUBLIC KEY-----", "").Trim();
            //}
            _sessionHelperService.SaveSession<string>(pkDispenserPublicKey, Headers.ContextValues, "pkDispenserPublicKey");
            //United.Persist.FilePersist.Save<string>(Utility.GetCSSPublicKeyPersistSessionStaticGUID(applicationId), "pkDispenserPublicKey", pkDispenserPublicKey);
            return pkDispenserPublicKey;

        }

        private bool IsYATravel(List<DisplayTraveler> displayTravelers)
        {
            if (displayTravelers == null || displayTravelers.Count == 0) return false;

            return displayTravelers.Any(t => t != null && !string.IsNullOrEmpty(t.PricingPaxType) && t.PricingPaxType.ToUpper().Equals("UAY"));
        }

        private bool IsOneFlexibleSegmentExist(List<MOBSHOPTrip> trips)
        {
            bool isFlexibleSegment = true;
            if (trips != null)
            {
                foreach (MOBSHOPTrip trip in trips)
                {
                    #region
                    if (trip.FlattenedFlights != null)
                    {
                        foreach (Model.Shopping.MOBSHOPFlattenedFlight flattenedFlight in trip.FlattenedFlights)
                        {
                            if (flattenedFlight.Flights != null && flattenedFlight.Flights.Count > 0)
                            {
                                foreach (Model.Shopping.MOBSHOPFlight flight in flattenedFlight.Flights)
                                {
                                    //TFS 53620:Booking - Certain Flights From IAH- ANC Are Displaying An Error Message
                                    if (flight.ShoppingProducts != null && flight.ShoppingProducts.Count > 0)
                                    {
                                        foreach (Model.Shopping.MOBSHOPShoppingProduct product in flight.ShoppingProducts)
                                        {
                                            if (!product.Type.ToUpper().Trim().Contains("FLEXIBLE"))
                                            {
                                                isFlexibleSegment = false;
                                                break;
                                            }

                                        }
                                    }
                                    if (!isFlexibleSegment)
                                    {
                                        break;
                                    }
                                }
                            }
                            if (!isFlexibleSegment) { break; }
                        }
                    }
                    if (!isFlexibleSegment) { break; }
                    #endregion
                }
            }
            return isFlexibleSegment;
        }

        private string GetFlightShareMessage(MOBSHOPReservation reservation, string cabinType)
        {
            #region Build Reservation Share Message 
            string flightDatesText = DateTime.Parse(reservation.Trips[0].DepartDate.Replace("\\", ""), CultureInfo.InvariantCulture).ToString("MMM dd") + (reservation.Trips.Count == 1 ? "" : (" - " + (DateTime.Parse(reservation.Trips[reservation.Trips.Count - 1].ArrivalDate.Replace("\\", ""), CultureInfo.InvariantCulture).ToString("MMM dd"))));
            string travelersText = reservation.NumberOfTravelers.ToString() + " " + (reservation.NumberOfTravelers > 1 ? "travelers" : "traveler");
            string searchType = string.Empty, flightNumbers = string.Empty, viaAirports = string.Empty;
            string initialOrigin = reservation.Trips[0].Origin.ToUpper().Trim();
            string finalDestination = reservation.Trips[reservation.Trips.Count - 1].Destination.ToUpper().Trim();

            switch (reservation.SearchType.ToUpper().Trim())
            {
                case "OW":
                    searchType = "one way";
                    break;
                case "RT":
                    searchType = "roundtrip";
                    break;
                case "MD":
                    searchType = "multiple destinations";
                    break;
                default:
                    break;
            }
            foreach (MOBSHOPTrip trip in reservation.Trips)
            {
                foreach (Model.Shopping.MOBSHOPFlattenedFlight flattenedFlight in trip.FlattenedFlights)
                {
                    if (string.IsNullOrEmpty(cabinType))
                    {
                        cabinType = flattenedFlight.Flights[0].Cabin.ToUpper().Trim() == "COACH" ? "Economy" : flattenedFlight.Flights[0].Cabin;
                    }
                    foreach (Model.Shopping.MOBSHOPFlight flight in flattenedFlight.Flights)
                    {
                        flightNumbers = flightNumbers + "," + flight.FlightNumber;
                        if (flight.Destination.ToUpper().Trim() != initialOrigin && flight.Destination.ToUpper().Trim() != finalDestination)
                        {
                            if (string.IsNullOrEmpty(viaAirports))
                            {
                                viaAirports = " via ";
                            }
                            viaAirports = viaAirports + flight.Destination + ",";
                        }
                    }
                }
            }
            if (flightNumbers.Trim(',').Split(',').Count() > 1)
            {
                flightNumbers = "Flights " + flightNumbers.Trim(',');
            }
            else
            {
                flightNumbers = "Flight " + flightNumbers.Trim(',');
            }
            string reservationShareMessage = string.Format(_configuration.GetValue<string>("Booking20ShareMessage"), flightDatesText, travelersText, searchType, cabinType, flightNumbers.Trim(','), initialOrigin, finalDestination, viaAirports.Trim(','));
            reservation.FlightShareMessage = reservationShareMessage;
            #endregion
            return reservationShareMessage;
        }


        private double GeTotalFromPrices(List<United.Mobile.Model.Shopping.MOBSHOPPrice> Prices)
        {
            double totalamount = 0.0;
            if (Prices != null)
            {
                var total = (from s in Prices
                             where s.DisplayType.Trim().ToUpper() == "TOTAL"
                             select s.DisplayValue).ToList();
                if (total.Count > 0 && !string.IsNullOrEmpty(total[0]))
                {
                    Double.TryParse(total[0], out totalamount);
                }
            }
            return totalamount;
        }

        private List<Mobile.Model.Shopping.MOBSHOPPrice> RefundAmountTravelOption(TravelOptionsCollection traveloption)
        {
            var traveloptionprices = new List<Mobile.Model.Shopping.MOBSHOPPrice>();
            var traveloptionprice = new Mobile.Model.Shopping.MOBSHOPPrice();

            CultureInfo ci = null;
            var currencyType = string.Empty;
            decimal totalAmount = 0.0m;

            traveloption.ForEach(option =>
            {
                if (option.Status.Equals("REFUND", StringComparison.OrdinalIgnoreCase))
                {
                    if (ci == null)
                        ci = TopHelper.GetCultureInfo(option.Currency);

                    totalAmount = totalAmount + option.Amount;

                    double tempDouble = 0;
                    double.TryParse(Convert.ToString(option.Amount), out tempDouble);

                    traveloptionprice = new Mobile.Model.Shopping.MOBSHOPPrice
                    {
                        Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero),
                        CurrencyCode = option.Currency,
                        DisplayValue = string.Format("{0:#,0.00}", option.Amount),
                        FormattedDisplayValue = TopHelper.FormatAmountForDisplay(option.Amount, ci, false),
                        PriceTypeDescription = option.Description,
                        Status = "To original form of payment",
                        DisplayType = "TRAVELOPTIONREFUND"
                    };
                    traveloptionprices.Add(traveloptionprice);
                }
                currencyType = option.Currency;
            });


            if (traveloptionprices.Any())
            {
                double tempTotalDouble = 0;
                double.TryParse(Convert.ToString(totalAmount), out tempTotalDouble);

                traveloptionprice = new Mobile.Model.Shopping.MOBSHOPPrice
                {
                    Value = Math.Round(tempTotalDouble, 2, MidpointRounding.AwayFromZero),
                    CurrencyCode = currencyType,
                    DisplayValue = string.Format("{0:#,0.00}", totalAmount),
                    FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalAmount, ci, false),
                    PriceTypeDescription = "Total travel option refund",
                    Status = "To original form of payment",
                    DisplayType = "TOTALTRAVELOPTIONREFUND"
                };

                traveloptionprices.Add(traveloptionprice);
            }

            return traveloptionprices;
        }

        private void AssignCorporateRate(MOBSHOPReservation reservation, bool isShopRequestCorporateBooking, bool isArranger = false)
        {
            bool isCorporateBooking = Convert.ToBoolean(_configuration.GetValue<string>("CorporateConcurBooking") ?? "false");
            string corporateFareText = _configuration.GetValue<string>("CorporateFareIndicator") ?? string.Empty;
            //bool hasMatchCorpDisc = segment.Products.Any(p => p.ProductSubtype.Contains("CORPDISC"));

            bool isCorporateFareSelected =
                reservation.Trips.Any(
                    x =>
                        x.FlattenedFlights.Any(
                            f =>
                                f.Flights.Any(
                                    fl =>
                                        fl.CorporateFareIndicator ==
                                        corporateFareText.ToString())));
            if (isCorporateBooking && isShopRequestCorporateBooking)
            {
                reservation.ShopReservationInfo = new Model.Shopping.ReservationInfo()
                {
                    IsCorporateBooking = true,
                    CorporateSuppressSavedTravelerMessage = (isArranger) ? string.Empty : _configuration.GetValue<string>("CorporateSuppressSavedTravelerMessage") ?? string.Empty

                };
                if (isCorporateFareSelected)
                {
                    reservation.ShopReservationInfo.CorporateRate =
                        _configuration.GetValue<string>("CorporateRateText") ?? string.Empty;
                }
            }
        }
        private List<MOBSHOPTrip> PopulateTrips(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string cartId, ShoppingResponse persistShop, string sessionId, int appId, string deviceId, string appVersion, bool showMileageDetails, List<United.Services.FlightShopping.Common.DisplayCart.DisplayTrip> flightSegmentCollection, string fareClass, List<string> flightDepartDatesForSelectedTrip)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            Session session = new Session();
            session = _sessionHelperService.GetSession<Session>(Headers.ContextValues, session.ObjectName).Result;
            //session = Persist.FilePersist.Load<Session>(sessionId, session.ObjectName);
            supressLMX = session.SupressLMXForAppID;
            #endregion
            List<MOBSHOPTrip> trips = new List<MOBSHOPTrip>();

            try
            {
                airportsList = _sessionHelperService.GetSession<AirportDetailsList>(Headers.ContextValues).Result;
                //_airportsList = FilePersist.Load<AirportDetailsList>(sessionId, (new AirportDetailsList()).GetType().FullName);
                if (isGetAirportListInOneCallToggleOn() && (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0))
                {
                    airportsList = GetAllAiportsList(flightSegmentCollection);
                }
            }
            catch (Exception ex)
            {
                //logEntries.Add(LogEntry.GetLogEntry<Exception>(sessionId, "GetAllAiportsList", "PopulateTrips-GetAllAiportsList", appId, appVersion, deviceId, ex, true, true));
                //logEntries.Add(LogEntry.GetLogEntry<List<United.Services.FlightShopping.Common.DisplayCart.DisplayTrip>>(sessionId, "GetAllAiportsList", "PopulateTrips-DisplayTrip", appId, appVersion, deviceId, flightSegmentCollection, true, true));
            }

            for (int i = 0; i < flightSegmentCollection.Count; i++)
            {
                MOBSHOPTrip trip = null;

                if (flightSegmentCollection != null && flightSegmentCollection.Count > 0)
                {
                    trip = new MOBSHOPTrip();
                    trip.TripId = flightSegmentCollection[i].BBXSolutionSetId;
                    trip.FlightCount = flightSegmentCollection[i].Flights.Count;


                    trip.DepartDate = FormatDateFromDetails(flightSegmentCollection[i].DepartDate);
                    trip.ArrivalDate = FormatDateFromDetails(flightSegmentCollection[i].Flights[flightSegmentCollection[i].Flights.Count - 1].DestinationDateTime);
                    trip.Destination = flightSegmentCollection[i].Destination;

                    // Fix for Partially used: To know change type
                    if (flightSegmentCollection[i].ChangeType == 0)
                        trip.ChangeType = MOBSHOPTripChangeType.ChangeFlight;
                    else if (flightSegmentCollection[i].ChangeType == 1)
                        trip.ChangeType = MOBSHOPTripChangeType.AddFlight;


                    string destinationName = string.Empty;
                    if (!string.IsNullOrEmpty(trip.Destination))
                    {
                        destinationName = GetAirportNameFromSavedList(trip.Destination);
                    }
                    if (string.IsNullOrEmpty(destinationName))
                    {
                        trip.DestinationDecoded = flightSegmentCollection[i].Flights[flightSegmentCollection[i].Flights.Count - 1].DestinationDescription;
                    }
                    else
                    {
                        trip.DestinationDecoded = destinationName;
                    }

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

                            Parallel.Invoke(() =>
                            {
                                bool includeAmenities = false;
                                bool.TryParse(_configuration.GetValue<string>("IncludeAmenities"), out includeAmenities);

                                //if we are asking for amenities in the CSL call, do not make this seperate call
                                if (!includeAmenities)
                                {
                                    amenitiesResponse = GetAmenitiesForFlights(sessionId, cartId, tempFlights, appId, deviceId, appVersion);
                                    PopulateFlightAmenities(amenitiesResponse.Profiles, ref tempFlights);
                                }
                            },
                                    () =>
                                    {
                                        if (showMileageDetails && !supressLMX)
                                        {
                                            //get all flight numbers
                                            List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights = null;
                                            lmxFlights = GetLmxFlights(session.Token, cartId, GetFlightHasListForLMX(tempFlights), sessionId, appId, appVersion, deviceId);// GetFlightHasListForLMX(tempFlights));

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

                        flights = GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, flightSegmentCollection[i].Flights, persistShop.Response.Availability.Trip.SearchFiltersIn.FareFamily, out ci, 0.0M, trip.Columns, persistShop.Request.PremierStatusLevel, fareClass, false);
                    }

                    trip.Origin = flightSegmentCollection[i].Origin;

                    if (showMileageDetails && !supressLMX)
                        trip.YqyrMessage = _configuration.GetValue<string>("MP2015YQYRMessage");

                    string originName = string.Empty;
                    if (!string.IsNullOrEmpty(trip.Origin))
                    {
                        originName = GetAirportNameFromSavedList(trip.Origin);
                    }
                    if (string.IsNullOrEmpty(originName))
                    {
                        trip.OriginDecoded = flightSegmentCollection[i].Flights[0].OriginDescription;
                    }
                    else
                    {
                        trip.OriginDecoded = originName;
                    }

                    if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI"))
                    {
                        trip.OriginDecodedWithCountry = flightSegmentCollection[i].Flights[0].OriginDescription;

                        string destinationDecodedWithCountry = string.Empty;

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


                    if (flights != null)
                    {

                        string tripDepartDate = string.Empty;
                        foreach (string tripIDDepDate in flightDepartDatesForSelectedTrip)
                        {
                            if (tripIDDepDate.Split('|')[0].ToString().Trim() == trip.TripId)
                            {
                                tripDepartDate = tripIDDepDate.Split('|')[1].ToString().Trim();
                                break;
                            }
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
                            if (_configuration.GetValue<bool>("EnableChangeOfAirport"))
                            {
                                flattenedFlight.airportChange = flight.AirportChange;
                            }
                            flight.TripId = trip.TripId;

                            flight.DepartureDateTimeGMT = _shoppingUtility.GetGMTTime(flight.DepartureDateTime, flight.Origin);
                            flight.ArrivalDateTimeGMT = _shoppingUtility.GetGMTTime(flight.ArrivalDateTime, flight.Destination);

                            #region Red Eye Flight Changes

                            flight.FlightArrivalDays = GetDayDifference(tripDepartDate, flight.ArrivalDateTime);
                            bool flightDateChanged = false;
                            flight.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDepartDate, flight.ArrivalDateTime, ref flightDateChanged);
                            flight.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDepartDate, flight.DepartureDateTime, ref flightDateChanged);
                            flight.FlightDateChanged = flightDateChanged;

                            #endregion

                            if (_shoppingUtility.EnableIBELite() || _shoppingUtility.EnableIBEFull())
                            {
                                flattenedFlight.FlightHash = flight.FlightHash;
                            }

                            flattenedFlight.Flights.Add(flight);
                            // Added By Ali as part of Bug :213198 mAPP: Award - Connection cities are showing incorrect in RTI initial & Payment screen for COG flight
                            if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                            {
                                // Make a copy of flight.Connections and release flight.Connections
                                List<Model.Shopping.MOBSHOPFlight> connections = flight.StopInfos.Clone();
                                flight.StopInfos = null;

                                foreach (var connection in connections)
                                {
                                    connection.TripId = trip.TripId;
                                    connection.IsStopOver = true;
                                    connection.DepartureDateTimeGMT = _shoppingUtility.GetGMTTime(connection.DepartureDateTime, connection.Origin);
                                    connection.ArrivalDateTimeGMT = _shoppingUtility.GetGMTTime(connection.ArrivalDateTime, connection.Destination);
                                    #region Red Eye Flight Changes

                                    connection.FlightArrivalDays = GetDayDifference(tripDepartDate, connection.ArrivalDateTime);
                                    connection.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDepartDate, connection.ArrivalDateTime, ref flightDateChanged);
                                    connection.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDepartDate, connection.DepartureDateTime, ref flightDateChanged);
                                    connection.FlightDateChanged = flightDateChanged;


                                    #endregion
                                    flattenedFlight.Flights.Add(connection);
                                }
                            }

                            if (flight.Connections != null && flight.Connections.Count > 0)
                            {
                                // Make a copy of flight.Connections and release flight.Connections
                                List<Model.Shopping.MOBSHOPFlight> connections = flight.Connections.Clone();
                                flight.Connections = null;

                                foreach (var connection in connections)
                                {
                                    connection.TripId = trip.TripId;

                                    connection.DepartureDateTimeGMT = _shoppingUtility.GetGMTTime(connection.DepartureDateTime, connection.Origin);
                                    connection.ArrivalDateTimeGMT = _shoppingUtility.GetGMTTime(connection.ArrivalDateTime, connection.Destination);

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
                                            conn.DepartureDateTimeGMT = _shoppingUtility.GetGMTTime(conn.DepartureDateTime, conn.Origin);
                                            conn.ArrivalDateTimeGMT = _shoppingUtility.GetGMTTime(conn.ArrivalDateTime, conn.Destination);
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
                            // Commented by Ali and moved this code above as part of Bug :213198 mAPP: Award - Connection cities are showing incorrect in RTI initial & Payment screen for COG flight

                            //if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                            //{
                            //    // Make a copy of flight.Connections and release flight.Connections
                            //    List<MOBSHOPFlight> connections = flight.StopInfos.Clone();
                            //    flight.StopInfos = null;

                            //    foreach (var connection in connections)
                            //    {
                            //        connection.TripId = trip.TripId;
                            //        connection.IsStopOver = true;
                            //        connection.DepartureDateTimeGMT = _shoppingUtility.GetGMTTime(connection.DepartureDateTime, connection.Origin);
                            //        connection.ArrivalDateTimeGMT = _shoppingUtility.GetGMTTime(connection.ArrivalDateTime, connection.Destination);
                            //        #region Red Eye Flight Changes

                            //        connection.FlightArrivalDays = GetDayDifference(tripDepartDate, connection.ArrivalDateTime);
                            //        connection.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDepartDate, connection.ArrivalDateTime, ref flightDateChanged);
                            //        connection.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDepartDate, connection.DepartureDateTime, ref flightDateChanged);
                            //        connection.FlightDateChanged = flightDateChanged;

                            //        #endregion
                            //        flattenedFlight.Flights.Add(connection);
                            //    }
                            //}

                            if (flight.ShoppingProducts != null && flight.ShoppingProducts.Count > 0)
                            {
                                int idx = 0;
                                foreach (Model.Shopping.MOBSHOPShoppingProduct prod in flight.ShoppingProducts)
                                {
                                    if (_shoppingUtility.EnableIBELite() && !flattenedFlight.IsIBELite && prod.IsIBELite) // set only once
                                    {
                                        flattenedFlight.IsIBELite = true;
                                    }

                                    if (_shoppingUtility.EnableIBEFull() && !flattenedFlight.IsIBE && prod.IsIBE)
                                    {
                                        flattenedFlight.IsIBE = true;
                                    }

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

        private decimal ReshopAwardAirfareDisplayValueInDecimal(List<Model.Shopping.MOBSHOPPrice> price)
        {
            decimal retCloseInFee = 0;
            decimal retChangeFee = 0;
            decimal retTaxDifference = 0;

            if (price.Exists(p => p.DisplayType.ToUpper() == "RBF"))
            {
                retCloseInFee = Convert.ToDecimal(price.First(p => p.DisplayType.ToUpper() == "RBF").DisplayValue);
            }
            if (price.Exists(p => p.DisplayType.ToUpper() == "CHANGEFEE"))
            {
                retChangeFee = Convert.ToDecimal(price.First(p => p.DisplayType.ToUpper() == "CHANGEFEE").DisplayValue);
            }
            if (price.Exists(p => p.DisplayType.ToUpper() == "TAXDIFFERENCE"))
            {
                retTaxDifference = Convert.ToDecimal(price.First(p => p.DisplayType.ToUpper() == "TAXDIFFERENCE").DisplayValue);
                if (retTaxDifference < 0)
                    retTaxDifference = 0;
            }
            return retCloseInFee + retChangeFee + retTaxDifference;
        }

        private string GetAllAirportCodesWithCommaDelimatedFromCSLTrips(List<United.Services.FlightShopping.Common.Trip> trips)
        {
            string airPortCodes = string.Empty;
            if (trips != null && trips.Count > 0)
            {
                airPortCodes = string.Join(",", trips.Where(t => t != null).Select(t => t.Origin + "," +
                                                                                        t.Destination + "," +
                                                                                        GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(t.Flights))
                                           );
            }
            airPortCodes = Regex.Replace(airPortCodes, ",{2,}", ",").Trim(',');
            airPortCodes = String.Join(",", airPortCodes.Split(',').Distinct());
            return airPortCodes;
        }

        private MOBResReservation PopulateReservation(string sessionId, Service.Presentation.ReservationModel.Reservation reservation)
        {
            MOBResReservation mobReservation = null;
            if (reservation != null)
            {
                CSLReservation persistedCSLReservation = new CSLReservation();
                persistedCSLReservation = _sessionHelperService.GetSession<CSLReservation>(Headers.ContextValues, persistedCSLReservation.ObjectName).Result;
                //persistedCSLReservation = FilePersist.Load<CSLReservation>(sessionId, persistedCSLReservation.ObjectName, false);
                if (persistedCSLReservation == null)
                {
                    persistedCSLReservation = new CSLReservation();
                    persistedCSLReservation.SessionId = sessionId;
                }
                if (persistedCSLReservation.Reservation == null)
                {
                    mobReservation = new MOBResReservation();
                    persistedCSLReservation.Reservation = mobReservation;
                }
                else
                {
                    mobReservation = persistedCSLReservation.Reservation;
                }

                mobReservation.FlightSegments = PopulateReservationFlightSegments(reservation.FlightSegments);
                mobReservation.TelephoneNumbers = PopulateReservationTelephone(reservation.TelephoneNumbers);
                mobReservation.Travelers = PopulateReservationTravelers(reservation.Travelers);

                mobReservation.IsRefundable =
                                    reservation.Characteristic.FirstOrDefault(
                                        x => x.Code.ToUpper().Trim() == "Refundable".ToUpper().Trim() && SafeConverter.ToBoolean(x.Value)) != null;

                mobReservation.ISInternational = mobReservation.FlightSegments.FirstOrDefault(item => item.FlightSegment.IsInternational.ToUpper().Trim() == "TRUE") != null;
                _sessionHelperService.SaveSession<CSLReservation>(persistedCSLReservation, Headers.ContextValues);
                //FilePersist.Save<United.Persist.Definition.Shopping.CSLReservation>(sessionId, persistedCSLReservation.ObjectName, persistedCSLReservation);
            }
            return mobReservation;
        }

        private List<MOBResTraveler> PopulateReservationTravelers(Collection<Service.Presentation.ReservationModel.Traveler> travelers)
        {
            List<MOBResTraveler> mobTravelers = null;

            if (travelers != null)
            {
                mobTravelers = new List<MOBResTraveler>();
                foreach (var traveler in travelers)
                {
                    MOBResTraveler mobTraveler = new MOBResTraveler();
                    if (traveler.Person != null)
                    {
                        mobTraveler.Person = new MOBPerPerson();
                        mobTraveler.Person.ChildIndicator = traveler.Person.ChildIndicator;
                        mobTraveler.Person.CustomerId = traveler.Person.CustomerID;
                        mobTraveler.Person.DateOfBirth = traveler.Person.DateOfBirth;
                        mobTraveler.Person.Title = traveler.Person.Title;
                        mobTraveler.Person.GivenName = traveler.Person.GivenName;
                        mobTraveler.Person.MiddleName = traveler.Person.MiddleName;
                        mobTraveler.Person.Surname = traveler.Person.Surname;
                        mobTraveler.Person.Suffix = traveler.Person.Suffix;
                        mobTraveler.Person.Suffix = traveler.Person.Sex;
                        if (traveler.Person.Documents != null)
                        {
                            mobTraveler.Person.Documents = new List<MOBPerDocument>();
                            foreach (var dcoument in traveler.Person.Documents)
                            {
                                MOBPerDocument mobDocument = new MOBPerDocument();
                                mobDocument.DocumentId = dcoument.DocumentID;
                                mobDocument.KnownTravelerNumber = dcoument.KnownTravelerNumber;
                                mobDocument.RedressNumber = dcoument.RedressNumber;
                                mobTraveler.Person.Documents.Add(mobDocument);
                            }
                        }
                    }

                    if (traveler.LoyaltyProgramProfile != null)
                    {
                        mobTraveler.LoyaltyProgramProfile = new MOBComLoyaltyProgramProfile();
                        mobTraveler.LoyaltyProgramProfile.CarrierCode = traveler.LoyaltyProgramProfile.LoyaltyProgramCarrierCode;
                        mobTraveler.LoyaltyProgramProfile.MemberId = traveler.LoyaltyProgramProfile.LoyaltyProgramMemberID;
                    }

                    mobTravelers.Add(mobTraveler);
                }
            }

            return mobTravelers;
        }


        private List<MOBComTelephone> PopulateReservationTelephone(Collection<Service.Presentation.CommonModel.Telephone> telephones)
        {
            List<MOBComTelephone> mobTelephones = null;

            if (telephones != null)
            {
                mobTelephones = new List<MOBComTelephone>();
                foreach (var telephone in telephones)
                {
                    MOBComTelephone mobTelephone = new MOBComTelephone();
                    mobTelephone.AreaCityCode = telephone.AreaCityCode;
                    mobTelephone.PhoneNumber = telephone.PhoneNumber;
                    mobTelephone.Description = telephone.Description;

                    mobTelephones.Add(mobTelephone);
                }
            }

            return mobTelephones;
        }

        private List<MOBSegReservationFlightSegment> PopulateReservationFlightSegments(System.Collections.ObjectModel.Collection<Service.Presentation.SegmentModel.ReservationFlightSegment> segments)
        {
            List<MOBSegReservationFlightSegment> reservationflightSegments = null;

            if (segments != null && segments.Count > 0)
            {
                reservationflightSegments = new List<MOBSegReservationFlightSegment>();
                foreach (var segment in segments)
                {
                    MOBSegReservationFlightSegment reservationFlightSegment = new MOBSegReservationFlightSegment();

                    reservationFlightSegment.FlightSegment = PopulateFlightSegment(segment.FlightSegment);

                    reservationflightSegments.Add(reservationFlightSegment);
                }
            }

            return reservationflightSegments;
        }

        private SegFlightSegment PopulateFlightSegment(Service.Presentation.SegmentModel.FlightSegment segment)
        {
            SegFlightSegment flightSegment = null;

            if (segment != null)
            {
                flightSegment = new SegFlightSegment();
                flightSegment.ArrivalAirport = PopulateAirport(segment.ArrivalAirport);
                flightSegment.BookingClasses = PopulateBookingClasses(segment.BookingClasses);
                flightSegment.DepartureAirport = PopulateAirport(segment.DepartureAirport);
                flightSegment.DepartureDateTime = segment.DepartureDateTime;
                flightSegment.FlightNumber = segment.FlightNumber;
                flightSegment.OperatingAirlineCode = segment.OperatingAirlineCode;
                flightSegment.OperatingAirlineName = segment.OperatingAirlineName;
                flightSegment.IsInternational = segment.IsInternational;
            }

            return flightSegment;
        }

        private MOBTMAAirport PopulateAirport(Service.Presentation.CommonModel.AirportModel.Airport airport)
        {
            MOBTMAAirport mobAirport = null;

            if (airport != null)
            {
                mobAirport = new MOBTMAAirport();
                mobAirport.IATACode = airport.IATACode;
                mobAirport.Name = airport.Name;
                mobAirport.ShortName = airport.ShortName;
            }

            return mobAirport;
        }

        private List<ComBookingClass> PopulateBookingClasses(System.Collections.ObjectModel.Collection<Service.Presentation.CommonModel.BookingClass> bookingClasses)
        {
            List<ComBookingClass> mobBookingClasses = null;

            if (bookingClasses != null && bookingClasses.Count > 0)
            {
                mobBookingClasses = new List<ComBookingClass>();
                foreach (var bookingClass in bookingClasses)
                {
                    ComBookingClass mobBookingClass = new ComBookingClass();
                    mobBookingClass.Code = bookingClass.Code;
                    mobBookingClasses.Add(mobBookingClass);
                }
            }

            return mobBookingClasses;
        }

        private void ValidateAwardReshopMileageBalance(bool IsMileagePurchaseRequired)
        {
            if (IsMileagePurchaseRequired)
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("NoEnoughMilesForAwardBooking"));
            }
        }

        private async Task<MOBMobileCMSContentMessages> GetCMSContentMessageByKey(string Key, MOBRequest request, Session session)
        {
            string cmsContentCache;
            CSLContentMessagesResponse cmsResponse = new CSLContentMessagesResponse();
            MOBMobileCMSContentMessages cmsMessage = null;
            List<CMSContentMessage> cmsMessages = null;
            try
            {
                //cmsContentCache = United.Persist.FilePersist.Load<string>(ConfigurationManager.AppSettings["BookingPathRTI_CMSContentMessagesCached_StaticGUID"].ToString(), "MOBCSLContentMessagesResponse");

                cmsContentCache = await _sessionHelperService.GetSession<string>(Headers.ContextValues, "United.Definition.Shopping.MOBSHOPSelectTripRequest");

                if (!string.IsNullOrEmpty(cmsContentCache))
                {
                    cmsResponse = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(cmsContentCache);
                }
                if (cmsResponse == null || Convert.ToBoolean(cmsResponse.Status) == false || cmsResponse.Messages == null)
                {
                    cmsResponse = _travelerCSL.GetBookingRTICMSContentMessages(request, session);
                }
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
            catch (Exception ex)
            {
                //logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "RTICMSContentMessages", "UnitedException", request.Application.Id, request.Application.Version.Major, request.DeviceId, ex.Message));
            }
            return cmsMessage;
        }

        private MOBResReservation GetShopBookingDetails(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string sessionId, United.Services.FlightShopping.Common.ShopSelectRequest request, ref MOBSHOPAvailability availability, SelectTripRequest selectTripRequest, ShoppingResponse persistShop)
        {

            Session session = new Session();
            session = _sessionHelperService.GetSession<Session>(Headers.ContextValues, session.ObjectName).Result;
            //session = Persist.FilePersist.Load<Session>(sessionId, session.ObjectName);

            string logAction = session.IsReshopChange ? "ReShopBookingDetails" : "ShopBookingDetails";
            string cslEndpoint = GetCslEndpointForShopping(session.IsReshopChange);

            MOBResReservation reservation = null;
            //string url = GetShopBookingDetailsUrl(selectRequest);

            if (session == null)
            {
                throw new UnitedException("Could not find your booking session.");
            }

            string jsonRequest = JsonConvert.SerializeObject(request);
            _sessionHelperService.SaveSession<string>(jsonRequest, Headers.ContextValues);
            //Persist.FilePersist.Save<string>(sessionId, typeof(ShopSelectRequest).FullName, jsonRequest);

            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - Request for " + logAction, "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, jsonRequest));


            //string url = string.Format("{0}/{1}", cslEndpoint, logAction);
            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - Request url for " + logAction, "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, url));

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cSLCallDurationstopwatch1;
            cSLCallDurationstopwatch1 = new Stopwatch();
            cSLCallDurationstopwatch1.Reset();
            cSLCallDurationstopwatch1.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            string token = string.IsNullOrEmpty(session.Token) ? _dPService.GetAnonymousToken(Headers.ContextValues.Application.Id, Headers.ContextValues.DeviceId, _configuration).Result : session.Token;
            var response = _flightShoppingService.ShopBookingDetails<FlightReservationResponse>(token, Headers.ContextValues.SessionId, logAction, jsonRequest).Result;
            //string jsonResponse = HttpHelper.Post(url, "application/json; charset=utf-8", session.Token, jsonRequest, httpPostTimeOut, httpPostNumberOfRetry);

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cSLCallDurationstopwatch1.IsRunning)
            {
                cSLCallDurationstopwatch1.Stop();
            }
            #endregion/***Get Call Duration Code - Venkat 03/17/2015*******
            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - CSL Call Duration", "CSS/CSL-CallDuration", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, "CSLShopBookingDetails=" + (cSLCallDurationstopwatch1.ElapsedMilliseconds / (double)1000).ToString()));
            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - Response for " + logAction, "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, jsonResponse));

            Model.Shopping.ShoppingResponse shop = new Model.Shopping.ShoppingResponse();
            shop = _sessionHelperService.GetSession<Model.Shopping.ShoppingResponse>(Headers.ContextValues, shop.ObjectName).Result;
            //shop = United.Persist.FilePersist.Load<ShoppingResponse>(selectTripRequest.SessionId, shop.ObjectName);
            if (shop == null)
            {
                throw new UnitedException("Could not find your booking session.");
            }

            if (response != null)
            {
                //FlightReservationResponse response = JsonConvert.DeserializeObject<FlightReservationResponse>(jsonResponse);
                //  United.Persist.FilePersist.Save<FlightReservationResponse>(session.SessionId, response.GetType().FullName, response);
                if (response != null && response.Status.Equals(StatusType.Success) && response.Reservation != null)
                {
                    if (session.IsAward && session.IsReshopChange)
                    {
                        if (response.DisplayCart != null)
                            ValidateAwardReshopMileageBalance(response.IsMileagePurchaseRequired);
                    }

                    reservation = PopulateReservation(sessionId, response.Reservation);

                    if (response.DisplayCart != null && response.DisplayCart.DisplayPrices != null)
                    {
                        if (availability.Reservation.ShopReservationInfo2 == null)
                        {
                            availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();
                        }
                        availability.Reservation.IsSSA = _shoppingUtility.EnableSSA(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);
                        availability.Reservation.IsELF = response.DisplayCart.IsElf;
                        availability.Reservation.ShopReservationInfo2.IsIBELite = _shoppingUtility.IsIBELiteFare(response.DisplayCart);
                        availability.Reservation.ShopReservationInfo2.IsIBE = _shoppingUtility.IsIBEFullFare(response.DisplayCart);
                        availability.Reservation.ShopReservationInfo2.Characteristics = ShopStaticUtility.GetCharacteristics(response.Reservation);
                        availability.Reservation.ELFMessagesForRTI = _eLFRitMetaShopMessages.GetELFShopMessagesForRestrictions(availability.Reservation);
                        availability.Reservation.IsUpgradedFromEntryLevelFare = response.DisplayCart.IsUpgradedFromEntryLevelFare && !_shoppingUtility.IsIBELiteFare(response.DisplayCart.ProductCodeBeforeUpgrade);
                        availability.Reservation.PointOfSale = shop.Request.CountryCode;
                        string fareClass = GetFareClassAtShoppingRequestFromPersist(sessionId);
                        List<string> flightDepartDatesForSelectedTrip = new List<string>();
                        if (availability.Reservation.Trips == null) { }
                        else
                        {
                            foreach (MOBSHOPTrip shopTrip in availability.Reservation.Trips)
                            {
                                flightDepartDatesForSelectedTrip.Add(shopTrip.TripId + "|" + shopTrip.DepartDate);
                            }
                        }
                        if (_mOBSHOPDataCarrier == null)
                            _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                        availability.Reservation.Trips = PopulateTrips(_mOBSHOPDataCarrier, response.CartId, persistShop, selectTripRequest.SessionId, selectTripRequest.Application.Id, selectTripRequest.DeviceId, selectTripRequest.Application.Version.Major, shop.Request.ShowMileageDetails, response.DisplayCart.DisplayTrips, fareClass, flightDepartDatesForSelectedTrip);
                        if (_shoppingUtility.IsEnableTaxForAgeDiversification(shop.Request.IsReshopChange, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                        {
                            availability.Reservation.Prices = _shoppingUtility.GetPrices(response.DisplayCart.DisplayPrices, availability.AwardTravel, session.SessionId, session.IsReshopChange, availability.Reservation.SearchType);
                        }
                        else
                        {
                            availability.Reservation.Prices = GetPrices(response.DisplayCart.DisplayPrices, availability.AwardTravel, session.SessionId, session.IsReshopChange);

                        }
                        AssignCorporateRate(availability.Reservation, shop.Request.IsCorporateBooking, session.IsArrangerBooking);
                        if (_shoppingUtility.EnableCovidTestFlightShopping(shop.Request.Application.Id, shop.Request.Application.Version.Major, session.IsReshopChange))
                        {
                            ShopStaticUtility.AssignCovidTestIndicator(availability.Reservation);
                        }
                        if (_configuration.GetValue<bool>("EnableIsArranger") && session.IsArrangerBooking)
                        {
                            if (availability.Reservation.ShopReservationInfo2 == null)
                                availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();

                            availability.Reservation.ShopReservationInfo2.IsArrangerBooking = true;
                        }
                        bool Is24HoursWindow = false;
                        if (_configuration.GetValue<bool>("EnableForceEPlus"))
                        {
                            if (availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT != null)
                            {
                                Is24HoursWindow = TopHelper.Is24HourWindow(Convert.ToDateTime(availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT));
                            }
                        }

                        availability.Reservation.ShopReservationInfo2.IsForceSeatMap = _shoppingUtility.IsForceSeatMapforEPlus(shop.Request.IsReshopChange, response.DisplayCart.IsElf, Is24HoursWindow, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);
                        bool isSupportedVersion = GeneralHelper.IsApplicationVersionGreater(shop.Request.Application.Id, shop.Request.Application.Version.Major, "AndroidBundleVersion", "IOSBundleVersion", "", "", true, _configuration);
                        if (isSupportedVersion)
                        {
                            if (_configuration.GetValue<bool>("IsEnableBundlesForBasicEconomyBooking"))
                            {
                                availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles") && !shop.Request.IsReshopChange;
                            }
                            else
                            {
                                availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles") && !(response.DisplayCart.IsElf || shop.Request.IsReshopChange || _shoppingUtility.IsIBEFullFare(response.DisplayCart));
                            }
                        }

                        if (shop.Request.IsReshopChange)
                        {
                            availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = false;
                            availability.Reservation.ShopReservationInfo2.ShouldHideBackButton = false;
                        }

                        #region 159514 - Added for inhibit booking message,177113 - 179536 BE Fare Inversion and stacking messages  

                        if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                        {
                            if (ShopStaticUtility.IdentifyInhibitWarning(response))
                            {
                                if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                                if (!availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.INHIBITBOOKING.ToString()))
                                {
                                    //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - Response with Inhibit warning ", "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, jsonResponse));
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                                    if (!_configuration.GetValue<bool>("TurnOffBookingCutoffMinsFromCSL"))
                                    {
                                        string bookingCutOffminsFromCSL = (response?.DisplayCart?.BookingCutOffMinutes > 0) ? response.DisplayCart.BookingCutOffMinutes.ToString() : string.Empty;

                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetInhibitMessage(bookingCutOffminsFromCSL));
                                        availability.Reservation.ShopReservationInfo2.BookingCutOffMinutes = bookingCutOffminsFromCSL;

                                    }
                                    else
                                    {
                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetInhibitMessage(string.Empty));
                                    }

                                    if (_shoppingUtility.EnableBoeingDisclaimer(availability.Reservation.IsReshopChange))
                                    {
                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                                    }
                                }
                            }
                        }

                        if (_shoppingUtility.EnableBoeingDisclaimer(availability.Reservation.IsReshopChange) && _shoppingUtility.IsBoeingDisclaimer(response.DisplayCart.DisplayTrips))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                            if (!availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.BOEING737WARNING.ToString()))
                            {
                                //if (traceSwitch.TraceWarning)
                                //{
                                //    //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - Response with Inhibit warning ", "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, jsonResponse));
                                //}
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetBoeingDisclaimer());

                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                            }
                        }

                        if (_shoppingUtility.IsIBELiteFare(response.DisplayCart.ProductCodeBeforeUpgrade))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetIBELiteNonCombinableMessage());
                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                        }

                        ///202150 - Getting both messages for fare inversions trying to select mixed itinerary (Reported by Andrew)
                        ///Srini - 12/26/2017
                        ///This If condition, we can remove, when we take "BugFixToggleFor17M" toggle out and directly "response.DisplayCart.IsUpgradedFromEntryLevelFare" check to next if condition
                        if (!_configuration.GetValue<bool>("BugFixToggleFor17M") || (_configuration.GetValue<bool>("BugFixToggleFor17M") && response.DisplayCart.IsUpgradedFromEntryLevelFare))
                        {
                            if (_configuration.GetValue<bool>("EnableBEFareInversion"))
                            {

                                if (ShopStaticUtility.IdentifyBEFareInversion(response))
                                {
                                    if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetBEMessage());
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                                }
                            }
                        }
                        #endregion

                        if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                        {
                            availability.Reservation.Prices.AddRange(GetPrices(response.DisplayCart.DisplayFees, false, string.Empty));
                        }
                        //need to add close in fee to TOTAL
                        availability.Reservation.Prices = AdjustTotal(availability.Reservation.Prices);
                        if (_shoppingUtility.IsEnableTaxForAgeDiversification(shop.Request.IsReshopChange, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();

                            availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = _shoppingUtility.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices, session.IsReshopChange);

                            if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                            {
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes.Add(ShopStaticUtility.AddFeesAfterPriceChange(response.DisplayCart.DisplayFees));
                            }
                        }
                        else
                        {
                            if (_shoppingUtility.EnableReshopMixedPTC(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                            {
                                if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                                    availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();

                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = _shoppingUtility.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices, session.IsReshopChange);
                            }

                            availability.Reservation.Taxes = GetTaxAndFees(response.DisplayCart.DisplayPrices, shop.Request.NumberOfAdults, session.IsReshopChange);

                            if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                            {
                                //combine fees into taxes so that totals are correct
                                List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> tempList = new List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice>();
                                tempList.AddRange(response.DisplayCart.DisplayPrices);
                                tempList.AddRange(response.DisplayCart.DisplayFees);
                                availability.Reservation.Taxes = GetTaxAndFees(tempList, shop.Request.NumberOfAdults, session.IsReshopChange);
                            }


                            if (response.DisplayCart != null && response.DisplayCart.TravelOptions != null && response.DisplayCart.TravelOptions.Any()
                                && response.DisplayCart.TravelOptions.Any(x => string.Equals(x.Status, "REFUND", StringComparison.OrdinalIgnoreCase)))
                            {
                                bool hasFareRefund = false;

                                var traveloptionrefundobj = RefundAmountTravelOption(response.DisplayCart.TravelOptions);
                                var traveloptionrefund = traveloptionrefundobj.FirstOrDefault(x => string.Equals(x.DisplayType, "TOTALTRAVELOPTIONREFUND", StringComparison.OrdinalIgnoreCase));
                                var totalpricerefund = availability.Reservation.Prices.FirstOrDefault(x => string.Equals(x.DisplayType, "REFUNDPRICE", StringComparison.OrdinalIgnoreCase));

                                if (totalpricerefund != null)
                                {
                                    double tempDouble1 = 0;
                                    double.TryParse(totalpricerefund.DisplayValue, out tempDouble1);
                                    if (tempDouble1 > 0)
                                        hasFareRefund = true;
                                }

                                if (traveloptionrefund != null)
                                {
                                    decimal totalrefund = (traveloptionrefund != null && totalpricerefund != null) ?
                                        Convert.ToDecimal(traveloptionrefund.DisplayValue) + Convert.ToDecimal(totalpricerefund.DisplayValue) :
                                        Convert.ToDecimal(traveloptionrefund.DisplayValue);

                                    CultureInfo ci = null;
                                    double tempDouble = 0;
                                    double.TryParse(Convert.ToString(totalrefund), out tempDouble);
                                    if (ci == null) ci = TopHelper.GetCultureInfo(traveloptionrefund.CurrencyCode);

                                    if (totalpricerefund != null)
                                    {
                                        totalpricerefund.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);
                                        totalpricerefund.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalrefund, ci, false);
                                        totalpricerefund.DisplayValue = string.Format("{0:#,0.00}", totalrefund);
                                    }
                                    else
                                    {
                                        Model.Shopping.MOBSHOPPrice totalPriceRefund = new Model.Shopping.MOBSHOPPrice
                                        {
                                            CurrencyCode = traveloptionrefund.CurrencyCode,
                                            DisplayType = "REFUNDPRICE",
                                            Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero),
                                            FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalrefund, ci, false),
                                            DisplayValue = string.Format("{0:#,0.00}", totalrefund)

                                        };
                                        availability.Reservation.Prices.Add(totalPriceRefund);
                                    }

                                    if (traveloptionrefundobj.Any())
                                        availability.Reservation.Prices.AddRange(traveloptionrefundobj);
                                }

                                if (!hasFareRefund)
                                {
                                    availability.Reservation.Prices.RemoveWhere
                                        (x => (string.Equals(x.DisplayType, "TAXDIFFERENCE", StringComparison.OrdinalIgnoreCase)));
                                    availability.Reservation.Prices.RemoveWhere
                                        (x => (string.Equals(x.DisplayType, "FAREDIFFERENCE", StringComparison.OrdinalIgnoreCase)));
                                }
                            }
                        }

                        availability.Reservation.TravelOptions = GetTravelOptions(response.DisplayCart);

                        if (string.IsNullOrEmpty(session.EmployeeId))
                        {
                            if (availability.AwardTravel)
                            {
                                availability.Reservation.FareLock = GetFareLockOptions(response.FareLockResponse, GeTotalFromPrices(availability.Reservation.Prices), GetFareCurrency(response.DisplayCart.DisplayPrices), availability.AwardTravel, GetFareMiles(response.DisplayCart.DisplayPrices), selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);
                            }
                            else
                            {
                                availability.Reservation.FareLock = GetFareLockOptions(response.FareLockResponse, GetFarePrice(response.DisplayCart.DisplayPrices), GetFareCurrency(response.DisplayCart.DisplayPrices), availability.AwardTravel, GetFareMiles(response.DisplayCart.DisplayPrices), selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);
                            }
                        }
                        availability.Reservation.AwardTravel = availability.AwardTravel;

                        if (!availability.AwardTravel)
                        {
                            availability.Reservation.LMXFlights = PopulateLMX(response.CartId, persistShop, selectTripRequest.SessionId, selectTripRequest.Application.Id, selectTripRequest.DeviceId, selectTripRequest.Application.Version.Major, shop.Request.ShowMileageDetails, response.DisplayCart.DisplayTrips);
                            availability.Reservation.IneligibleToEarnCreditMessage = _configuration.GetValue<string>("IneligibleToEarnCreditMessage");
                            availability.Reservation.OaIneligibleToEarnCreditMessage = _configuration.GetValue<string>("OaIneligibleToEarnCreditMessage");
                        }

                        availability.Reservation.IsCubaTravel = IsCubaTravelTrip(availability.Reservation);

                        if (availability.Reservation.IsCubaTravel)
                        {
                            MobileCMSContentRequest mobMobileCMSContentRequest = new MobileCMSContentRequest();
                            mobMobileCMSContentRequest.Application = selectTripRequest.Application;
                            mobMobileCMSContentRequest.Token = session.Token;
                            availability.Reservation.CubaTravelInfo = _travelerCSL.GetCubaTravelResons(mobMobileCMSContentRequest);
                            availability.Reservation.CubaTravelInfo.CubaTravelTitles = new MPDynamoDB(_configuration, _dynamoDBService,null).GetMPPINPWDTitleMessages(new List<string> { "CUBA_TRAVEL_CONTENT" });
                        }
                        if (_shoppingUtility.IsEnabledNationalityAndResidence(shop.Request.IsReshopChange, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();
                            availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence = IsRequireNatAndCR(availability.Reservation, selectTripRequest.Application, sessionId, selectTripRequest.DeviceId, session.Token);
                            if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence)
                            {
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.NationalityErrMsg = NationalityResidenceMsgs.NationalityErrMsg;
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ResidenceErrMsg = NationalityResidenceMsgs.ResidenceErrMsg;
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.NationalityAndResidenceErrMsg = NationalityResidenceMsgs.NationalityAndResidenceErrMsg;
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.NationalityAndResidenceHeaderMsg = NationalityResidenceMsgs.NationalityAndResidenceHeaderMsg;
                            }
                        }
                    }
                    availability.Reservation.ISFlexibleSegmentExist = IsOneFlexibleSegmentExist(availability.Reservation.Trips);
                    availability.Reservation.FlightShareMessage = GetFlightShareMessage(availability.Reservation, string.Empty);
                    availability.Reservation.IsRefundable = reservation.IsRefundable;
                    availability.Reservation.ISInternational = reservation.ISInternational;
                    //**RSA Publick Key Implmentaion*/

                    string pkDispenserPublicKey = _sessionHelperService.GetSession<string>(Headers.ContextValues, "pkDispenserPublicKey").Result;

                    //string pkDispenserPublicKey = FilePersist.Load<string>(_shoppingUtility.GetCSSPublicKeyPersistSessionStaticGUID(selectTripRequest.Application.Id), "pkDispenserPublicKey");
                    if (!string.IsNullOrEmpty(pkDispenserPublicKey))
                    {
                        availability.Reservation.PKDispenserPublicKey = pkDispenserPublicKey;
                    }
                    else
                    {
                        availability.Reservation.PKDispenserPublicKey = GetPkDispenserPublicKey(selectTripRequest.Application.Id, selectTripRequest.DeviceId, selectTripRequest.Application.Version.Major, sessionId, session.Token);
                    }
                    //**RSA Publick Key Implmentaion**//

                    //#region 214448 - Unaccompanied Minor Age
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo = new ReservationAgeBoundInfo();
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.MinimumAge = Convert.ToInt32(_configuration.GetValue<string>("umnrMinimumAge"));
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.UpBoundAge = Convert.ToInt32(_configuration.GetValue<string>("umnrUpBoundAge"));
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.ErrorMessage = _configuration.GetValue<string>("umnrErrorMessage");
                    //#endregion
                    availability.Reservation.CheckedbagChargebutton = availability.Reservation.IsReshopChange ? "" : _configuration.GetValue<string>("ViewCheckedBagChargesButton");
                    if (_shoppingUtility.EnableTravelerTypes(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, session.IsReshopChange)
                        && availability.Reservation.ShopReservationInfo2.TravelerTypes != null && availability.Reservation.ShopReservationInfo2.TravelerTypes.Count > 0)
                    {
                        availability.Reservation.ShopReservationInfo2.displayTravelTypes = ShopStaticUtility.GetDisplayTravelerTypes(response.DisplayCart.DisplayTravelers);
                    }
                    if (_shoppingUtility.EnableYoungAdult(session.IsReshopChange))
                    {
                        if (IsYATravel(response.DisplayCart.DisplayTravelers))
                        {
                            availability.Reservation.ShopReservationInfo2.IsYATravel = true;

                            if (availability.Reservation.ShopReservationInfo2.displayTravelTypes != null && availability.Reservation.ShopReservationInfo2.displayTravelTypes.Count > 0)
                                availability.Reservation.ShopReservationInfo2.displayTravelTypes[0].TravelerDescription = "Young adult (18-22)";

                        }
                    }
                    #region Define Booking Path Persist Reservation and Save to session - Venkat 08/13/2014

                    session.AppID = selectTripRequest.Application.Id;
                    session.VersionID = selectTripRequest.Application.Version.Major;

                    var bookingPathReservation
                        = CreateBookingPathReservation(sessionId, request, availability, shop, session, response);

                    //Adding FFCResidual data - TODO hardcoding/mapping
                    if (session.IsReshopChange
                        && _shoppingUtility.IncludeReshopFFCResidual
                        (selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major)
                        && availability?.Reservation?.Reshop != null && availability.Reservation.Reshop.IsResidualFFCRAvailable)
                    {
                        AssignFFCResidualForTravelerV2(availability, response);
                    }

                    _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, Headers.ContextValues);
                    //FilePersist.Save(bookingPathReservation.SessionId, bookingPathReservation.ObjectName, bookingPathReservation);
                    #endregion

                    var persistedShopBookingDetailsResponse = new ShopBookingDetailsResponse
                    {
                        SessionId = sessionId,
                        Reservation = response.Reservation
                    };

                    if (string.IsNullOrEmpty(session.EmployeeId))
                    {
                        persistedShopBookingDetailsResponse.FareLock = response.FareLockResponse;
                    }
                    _sessionHelperService.SaveSession<ShopBookingDetailsResponse>(persistedShopBookingDetailsResponse, Headers.ContextValues, persistedShopBookingDetailsResponse.ObjectName);
                    //FilePersist.Save(persistedShopBookingDetailsResponse.SessionId, persistedShopBookingDetailsResponse.ObjectName, persistedShopBookingDetailsResponse);
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.Message;

                            #region 159514 - Inhibit booking error
                            if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                            {
                                if (error != null && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10050"))
                                {
                                    //throw new UnitedException(error.Message);
                                    throw new UnitedException(error.Message, new Exception(error.MinorCode));
                                }
                            }
                            #endregion

                            //59249:Bug 323966: Flight Shopping (Mobile) - Item 9: CSL_Service\ShopBookingDetails Unhandled Exception: System.Exception - bookingDetail has no solutions being returned from ITA - Sep 22, 2016 - Vijayan
                            if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10047"))
                            {
                                throw new UnitedException(_configuration.GetValue<string>("BookingDetail_ITAError_For_CSL_10047__Error_Code"));
                            }

                            //67660 Bug 345403 CSL - System.Exception - Input error for "revenueBookingDetails" (search), solutionId - Sep 30,2016 - Issuf 
                            if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10048"))
                            {
                                throw new UnitedException(_configuration.GetValue<string>("BookingDetailsErrorMessage_For_CSL_10048__Error_Code"));
                            }

                            //59232 - Bug 323968: Flight Shopping (Mobile) - Item 11: CSL_Service\ShopBookingDetails Unhandled Exception: System.Exception - Ras Check Failed - Sep 12,2016 - Ravitheja G 
                            if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10045"))
                            {
                                List<string> lstFlightDetails = null;
                                if (_configuration.GetValue<string>("RasCheckFailedErrorMessageWithFlightNumber") != null && response.DisplayCart != null)
                                {
                                    lstFlightDetails = new List<string>();
                                    if (response.DisplayCart != null)
                                        if (response.DisplayCart.DisplayTrips != null && response.DisplayCart.DisplayTrips.Count > 0)
                                            foreach (var trips in response.DisplayCart.DisplayTrips)
                                            {
                                                foreach (var flightDetails in trips.Flights)
                                                {
                                                    lstFlightDetails.Add(flightDetails.MarketingCarrierDescription + " flight " + flightDetails.FlightNumber);
                                                }
                                            }
                                    throw new UnitedException(string.Format(_configuration.GetValue<string>("RasCheckFailedErrorMessageWithFlightNumber"), string.Join(",", lstFlightDetails)));
                                }
                                else
                                {
                                    throw new UnitedException(_configuration.GetValue<string>("GenericRasCheckFailedErrorMessage"));
                                }
                            }

                            // Added By Ali as part of Task 264624 : Select Trip - The Boombox user's session has expired
                            if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10036"))
                            {
                                throw new UnitedException(_configuration.GetValue<string>("BookingExceptionMessage_ServiceErrorSessionNotFound"));
                            }

                            //  Added by Ali as part of Task 278032 : System.Exception:FLIGHTS NOT FOUND-No flight options were found for this trip.
                            if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10051"))
                            {
                                throw new UnitedException(_configuration.GetValue<string>("NoAvailabilityError"));
                            }
                        }
                        throw new System.Exception(errorMessage);
                    }
                    else
                    {
                        throw new UnitedException("Failed to retrieve booking details.");
                    }
                }
            }
            else
            {
                throw new UnitedException("Failed to retrieve booking details.");
            }
            reservation.CallDuration = cSLCallDurationstopwatch1.ElapsedMilliseconds;
            return reservation;
        }
        private bool IsYABooking(MOBSHOPReservation reservation)
        {
            if (_shoppingUtility.EnableYoungAdult() && reservation != null && reservation.ShopReservationInfo2 != null && reservation.ShopReservationInfo2.IsYATravel) return true;

            return false;
        }
        private async void AwardNoFlightExceptionMsg(MOBSHOPShopRequest shopRequest)
        {
            if (_configuration.GetValue<bool>("EnableAwardShopNoFlightsExceptionMsg"))
            {
                if (shopRequest.AwardTravel)
                {
                    string cmsCacheResponse = await _sessionHelperService.GetSession<string>(Headers.ContextValues, "CSLContentMessagesResponse");
                    CSLContentMessagesResponse cmsResponse = new CSLContentMessagesResponse();
                    if (!string.IsNullOrEmpty(cmsCacheResponse))
                    {
                        cmsResponse = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(cmsCacheResponse);
                    }
                    if (cmsResponse == null || Convert.ToBoolean(cmsResponse.Status) == false || cmsResponse.Messages == null)
                    {
                        Session session = await _sessionHelperService.GetSession<Session>(Headers.ContextValues, new Session().ObjectName);

                        cmsResponse = _travelerCSL.GetBookingRTICMSContentMessages(shopRequest, session);
                    }

                    if (cmsResponse != null && (Convert.ToBoolean(cmsResponse.Status) && cmsResponse.Messages != null) && cmsResponse.Messages.Any())
                    {
                        //  logEntries.Add(LogEntry.GetLogEntry<CSLContentMessagesResponse>(shopRequest.SessionId, "AwardNoFlightExceptionMsg-Cached-Response", "Cached-Response", shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.DeviceId, cmsResponse));
                        if (cmsResponse.Messages.Any(m => m.Title.ToUpper().Equals(_configuration.GetValue<string>("AwardShopNoFlightsExceptionTitle").ToUpper())))
                        {
                            CMSContentMessage exceptionMsg = cmsResponse.Messages.First(m => m.Title.ToUpper().Equals(_configuration.GetValue<string>("AwardShopNoFlightsExceptionTitle").ToUpper()));

                            if (!string.IsNullOrEmpty(exceptionMsg.Headline))
                                throw new UnitedException(exceptionMsg.Headline + Environment.NewLine + Environment.NewLine + exceptionMsg.ContentFull);
                            else
                                throw new UnitedException(exceptionMsg.ContentFull);
                        }
                    }
                    //throw new MOBUnitedException(@"There are no award flights on the date you selected.It's possible that because of reduced flight schedules, United or our partner airlines might not fly to some destinations or may only fly on certain days of the week.You can use the calendar below to Please select another date that may have availability.");
                }
            }
        }
        private void AssignFFCResidualForTravelerV2
          (MOBSHOPAvailability availability, FlightReservationResponse response)
        {
            if (availability?.Reservation?.TravelersCSL?.Count > 0
                && response?.DisplayCart?.DisplayPrices?.Count > 0
                && response.Reservation?.Travelers != null && response.Reservation.Travelers.Any())
            {
                bool isFFCR = false;
                var title = _configuration.GetValue<string>("ReshopChangeFFCRTitle");
                var content = _configuration.GetValue<string>("ReshopChangeFFCRShortContent");

                availability.Reservation.TravelersCSL.ForEach(tvlr =>
                {
                    if (tvlr != null)
                    {
                        var scCslTraveler = response.Reservation.Travelers.Select(x => x.Person).FirstOrDefault
                        (x => string.Equals(x.Key, tvlr.Key, StringComparison.OrdinalIgnoreCase));

                        if (!string.IsNullOrEmpty(scCslTraveler?.Type) && !string.Equals(tvlr.TravelerTypeCode, "INF", StringComparison.OrdinalIgnoreCase))
                        {
                            var paxffcrprice = response.DisplayCart.DisplayPrices.FirstOrDefault
                            (x => string.Equals(x.PaxTypeCode, scCslTraveler.Type, StringComparison.OrdinalIgnoreCase) && x.ResidualAmount > 0);

                            if (paxffcrprice != null)
                            {
                                isFFCR = true;
                                var perpaxamount = Convert.ToDouble(paxffcrprice.ResidualAmount);
                                tvlr.FutureFlightCredits = new List<MOBFOPFutureFlightCredit>
                            {
                                new MOBFOPFutureFlightCredit{
                                NewValueAfterRedeem = perpaxamount,
                                DisplayNewValueAfterRedeem = $"{perpaxamount.ToString("C2", CultureInfo.CurrentCulture)}",
                                RecipientsFirstName = tvlr.FirstName,
                                RecipientsLastName = tvlr.LastName,
                                ExpiryDate = string.Empty,
                                TravelerNameIndex = tvlr.Key
                                }};
                            } //End - If

                            //FFCShopping fFCShopping = new FFCShopping(_configuration, null, _sessionHelperService, _sDLContentService, _shoppingUtility);
                            _fFCShoppingcs.AssignTravelerTotalFFCNewValueAfterReDeem(tvlr);
                        }
                    }
                }); //END - foreach

                if (isFFCR)
                {
                    availability.Reservation.Reshop.FFCMessage = new MOBFutureFlightCredit
                    {
                        Messages = new List<MOBItem>
                            {
                                new MOBItem{ Id="TITLE", CurrentValue = title },
                                new MOBItem{ Id="CONTENTSHORT", CurrentValue = content },
                            }
                    };
                } //END - If 
            }
        }

        private List<FormofPaymentOption> GetEligibleFormofPayments(SelectTripRequest request, Session session, MOBShoppingCart shoppingCart, string cartId, string flow, ref bool isDefault, MOBSHOPReservation reservation = null, List<LogEntry> logEntries = null, bool IsMilesFOPEnabled = false, SeatChangeState persistedState = null)
        {
            List<FormofPaymentOption> response = new List<FormofPaymentOption>();
            SeatChangeState state = persistedState;
            if (state == null && flow == FlowType.VIEWRES.ToString() && _shoppingUtility.IsManageResETCEnabled(request.Application.Id, request.Application.Version.Major))
            {
                state = _sessionHelperService.GetSession<SeatChangeState>(Headers.ContextValues).Result;
            }
            if (Convert.ToBoolean(_configuration.GetValue<string>("GetFoPOptionsAlongRegisterOffers")) && shoppingCart.Products != null && shoppingCart.Products.Count > 0)
            {
                FOPEligibilityRequest eligiblefopRequest = new FOPEligibilityRequest()
                {
                    TransactionId = request.TransactionId,
                    DeviceId = request.DeviceId,
                    AccessCode = request.AccessCode,
                    LanguageCode = request.LanguageCode,
                    Application = request.Application,
                    CartId = cartId,
                    SessionId = session.SessionId,
                    Flow = flow,
                    Products = _shoppingUtility.GetProductsForEligibleFopRequest(shoppingCart, state)
                };
                response = _formsOfPayment.EligibleFormOfPayments(eligiblefopRequest, session, ref isDefault, IsMilesFOPEnabled, logEntries);
                if ((reservation?.IsMetaSearch ?? false) && Convert.ToBoolean(_configuration.GetValue<string>("CreditCardFOPOnly_MetaSearch")))
                {
                    if (!_configuration.GetValue<bool>("EnableETCFopforMetaSearch"))
                    {
                        response = response.Where(x => x.Category == "CC").ToList();
                    }
                    else
                    {
                        response = response.Where(x => x.Category == "CC" || x.Category == "CERT").ToList();
                    }

                }
                var upliftFop = UpliftAsFormOfPayment(reservation, shoppingCart);
                if (upliftFop != null && response != null)
                {
                    response.Add(upliftFop);
                }

                // Added as part of Money + Miles changes: For MM user have to pay money only using CC - MOBILE-14735;  MOBILE-14833; MOBILE-14925 // MM is only for Booking
                if (_shoppingUtility.IncludeMoneyPlusMiles(request.Application.Id, request.Application.Version.Major) && flow == FlowType.BOOKING.ToString())
                {
                    if (shoppingCart.FormofPaymentDetails?.MoneyPlusMilesCredit?.SelectedMoneyPlusMiles != null) // selected miles will not be empty only after Applied Miles
                    {
                        response = response.Where(x => x.Category == "CC").ToList();
                    }
                }

                if (_shoppingUtility.IsETCchangesEnabled(request.Application.Id, request.Application.Version.Major) && flow == FlowType.BOOKING.ToString())
                    response = _shoppingUtility.BuildEligibleFormofPaymentsResponse(response, shoppingCart, session, request, reservation?.IsMetaSearch ?? false);
                else if (flow == FlowType.VIEWRES.ToString() && _shoppingUtility.IsManageResETCEnabled(request.Application.Id, request.Application.Version.Major))
                    response = _shoppingUtility.BuildEligibleFormofPaymentsResponse(response, shoppingCart, request);
                _sessionHelperService.SaveSession<List<FormofPaymentOption>>(response, Headers.ContextValues);
                //United.Persist.FilePersist.Save<List<FormofPaymentOption>>(session.SessionId, response.GetType().ToString(), response);
            }
            return response;
        }

        private FormofPaymentOption UpliftAsFormOfPayment(MOBSHOPReservation reservation, MOBShoppingCart shoppingCart)
        {
            if (_configuration.GetValue<bool>("EnableUpliftPayment"))
            {
                if (IsEligibileForUplift(reservation, shoppingCart))
                {
                    return new FormofPaymentOption
                    {
                        Category = "UPLIFT",
                        fopDescription = "Pay Monthly",
                        Code = "UPLIFT",
                        FullName = "Pay Monthly",
                        DeleteOrder = shoppingCart?.FormofPaymentDetails?.FormOfPaymentType?.ToUpper() != MOBFormofPayment.Uplift.ToString().ToUpper()
                    };
                }
            }
            return null;
        }

        private bool IsEligibileForUplift(MOBSHOPReservation reservation, MOBShoppingCart shoppingCart)
        {
            if (shoppingCart?.Flow?.ToUpper() == FlowType.VIEWRES.ToString().ToUpper())
            {
                return _shoppingUtility.HasEligibleProductsForUplift(shoppingCart.TotalPrice, shoppingCart.Products);
            }

            if (!_configuration.GetValue<bool>("EnableUpliftPayment"))
                return false;

            if (reservation == null || reservation.Prices == null || shoppingCart == null || shoppingCart?.Flow != FlowType.BOOKING.ToString())
                return false;

            if (reservation.ShopReservationInfo?.IsCorporateBooking ?? false)
                return false;

            if (shoppingCart.Products?.Any(p => p?.Code == "FLK") ?? false)
                return false;

            if (!_configuration.GetValue<bool>("DisableFixForUpliftFareLockDefect"))
            {
                if (shoppingCart.Products?.Any(p => p?.Code?.ToUpper() == "FARELOCK") ?? false)
                    return false;
            }

            if (reservation.Prices.Any(p => "TOTALPRICEFORUPLIFT".Equals(p.DisplayType, StringComparison.CurrentCultureIgnoreCase) && p.Value >= MinimumPriceForUplift && p.Value <= MaxmimumPriceForUplift) &&
               (shoppingCart?.SCTravelers?.Any(t => t?.TravelerTypeCode == "ADT" || t?.TravelerTypeCode == "SNR") ?? false))
            {
                return true;
            }
            return false;
        }
        private int MinimumPriceForUplift
        {
            get
            {
                var minimumAmountForUplift = _configuration.GetValue<string>("MinimumPriceForUplift");
                if (string.IsNullOrEmpty(minimumAmountForUplift))
                    return 300;

                int.TryParse(minimumAmountForUplift, out int upliftMinAmount);
                return upliftMinAmount;
            }
        }

        private int MaxmimumPriceForUplift
        {
            get
            {
                var maximumAmountForUplift = _configuration.GetValue<string>("MaximumPriceForUplift");
                if (string.IsNullOrEmpty(maximumAmountForUplift))
                    return 150000;

                int.TryParse(maximumAmountForUplift, out int upliftMaxAmount);
                return upliftMaxAmount;
            }
        }

        private void PopulateCFOPReshopProducts(Session session, SelectTripRequest request)//, List<LogEntry> logEntries)
        {
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            shoppingCart = _sessionHelperService.GetSession<MOBShoppingCart>(Headers.ContextValues, ObjectNames.MOBShoppingCart).Result;
            var products = _formsOfPayment.GetProductDetailsFromCartID(session, request);//, logEntries);
            if (products != null && products.Count() > 0)
            {
                shoppingCart = shoppingCart == null ? new MOBShoppingCart() : shoppingCart;
                shoppingCart.Products = new List<ProdDetail>();
                shoppingCart.Products = products;
                _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, Headers.ContextValues, shoppingCart.GetType().FullName);
            }
        }
        private List<MOBSHOPFareWheelItem> PopulateFareWheelDates(List<MOBSHOPTripBase> shopTrips, string currentCall)
        {
            List<MOBSHOPFareWheelItem> fareWheelItems = new List<MOBSHOPFareWheelItem>();
            try
            {
                if (shopTrips != null && shopTrips.Count > 0)
                {
                    #region
                    DateTime departureDate = DateTime.ParseExact(shopTrips[0].DepartDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);

                    DateTime fareWheelStart = departureDate.AddDays(-getFlexibleDaysBefore()); ;

                    DateTime fareWheelEnd = departureDate.AddDays(getFlexibleDaysAfter());

                    if (departureDate == DateTime.Today)
                    {
                        fareWheelStart = departureDate;
                    }
                    if (shopTrips.Count >= 2)
                    {
                        DateTime departureDate2 = DateTime.ParseExact(shopTrips[1].DepartDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);

                        if (!string.IsNullOrEmpty(currentCall))
                        {
                            if (currentCall == "SHOP" && (DateTime.Compare(departureDate2, fareWheelEnd)) < 0)
                            {
                                fareWheelEnd = departureDate2;
                            }

                            if (currentCall == "SELECTTRIP" && (DateTime.Compare(departureDate2, fareWheelStart)) > 0)
                            {
                                fareWheelStart = departureDate2;
                            }
                        }
                    }
                    for (DateTime fareWheelDate = fareWheelStart; fareWheelDate <= fareWheelEnd; fareWheelDate = fareWheelDate.AddDays(1))
                    {
                        MOBSHOPFareWheelItem fareWheel = new MOBSHOPFareWheelItem();

                        fareWheel.Key = (fareWheelDate.Month < 10 ? "0" + fareWheelDate.Month.ToString() : fareWheelDate.Month.ToString()) + "/" + (fareWheelDate.Day < 10 ? "0" + fareWheelDate.Day.ToString() : fareWheelDate.Day.ToString()) + "/" + fareWheelDate.Year.ToString();

                        fareWheel.DisplayValue = fareWheelDate.ToString("ddd MMM dd");

                        fareWheelItems.Add(fareWheel);
                    }
                    #endregion
                }
            }
            catch { }
            return fareWheelItems;
        }
        private int getFlexibleDaysAfter()
        {
            int flexibleFareDaysAfter = 0;
            int.TryParse(_configuration.GetValue<string>("AffinitySearchFlexibleDaysAfter"), out flexibleFareDaysAfter);

            return flexibleFareDaysAfter;
        }

        private int getFlexibleDaysBefore()
        {
            int flexibleFareDaysBefore = 0;
            int.TryParse(_configuration.GetValue<string>("AffinitySearchFlexibleDaysBefore"), out flexibleFareDaysBefore);

            return flexibleFareDaysBefore;
        }

        private void BuildAwardShopRequestAndSaveToPersist(United.Services.FlightShopping.Common.ShopResponse cslShopResponse, SelectTripRequest selectTripRequest, bool isCallFromShop = false)
        {
            #region Save CSL
            try
            {
                if (cslShopResponse != null && cslShopResponse.Trips != null && cslShopResponse.Trips.Count > 0)
                {
                    //var persistShopRequest = United.Persist.FilePersist.Load<CSLShopRequest>(selectTripRequest.SessionId, (new CSLShopRequest()).GetType().FullName);
                    var persistShopRequest = _sessionHelperService.GetSession<CSLShopRequest>(Headers.ContextValues, new CSLShopRequest().ObjectName).Result;
                    var trip = cslShopResponse.Trips.FirstOrDefault(t => t.Flights.Any(f => f.Products.Any(p => p.ProductId == selectTripRequest.ProductId.ToString())));
                    United.Services.FlightShopping.Common.Trip cslTrip = null;
                    if (trip != null)
                    {
                        if (selectTripRequest.ISProductSelected && !selectTripRequest.BackButtonClick)
                        {
                            cslTrip = persistShopRequest.ShopRequest.Trips[trip.TripIndex - 1];
                            cslTrip.Origin = trip.Origin;
                            cslTrip.Destination = trip.Destination;
                            cslTrip.BBXCellIdSelected = selectTripRequest.ProductId;
                            cslTrip.BBXSolutionSetId = selectTripRequest.TripId;
                            cslTrip.BBXSession = trip.BBXSession;
                            cslTrip.Flights = new List<Flight>();
                            cslTrip.Flights = trip.Flights.CloneDeep();
                            persistShopRequest.ShopRequest.Trips[trip.TripIndex - 1] = cslTrip;
                        }
                        else
                        {
                            //Select trip back button award calendar, sending productid as trip1, and hence tripIndex
                            cslTrip = persistShopRequest.ShopRequest.Trips[trip.TripIndex];
                            cslTrip.BBXCellIdSelected = null;
                            cslTrip.BBXSolutionSetId = null;
                            cslTrip.BBXSession = null;
                            cslTrip.Flights = null;
                            persistShopRequest.ShopRequest.Trips[trip.TripIndex] = cslTrip;
                        }

                        AssignCalendarLengthOfStay(selectTripRequest.LengthOfCalendar, persistShopRequest.ShopRequest);
                        _sessionHelperService.SaveSession<CSLShopRequest>(persistShopRequest, Headers.ContextValues);
                        //United.Persist.FilePersist.Save<United.Persist.Definition.Shopping.CSLShopRequest>(selectTripRequest.SessionId, persistShopRequest.ObjectName, persistShopRequest);
                    }
                    else
                    {
                        //logEntries.Add(LogEntry.GetLogEntry<string>(selectTripRequest.SessionId, "SelectTrip - BuildAwardShopRequestAndSaveToPersist", "United Exception", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, "Trips are null"));
                        throw new UnitedException("Trips are null");
                    }
                }
            }
            catch (System.Exception ex)
            {
                // logEntries.Add(LogEntry.GetLogEntry<string>(selectTripRequest.SessionId, "SelectTrip - BuildAwardShopRequestAndSaveToPersist", "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, ex.Message));
                throw new UnitedException("Could not find your booking session.");
            }
            #endregion
        }
        private void AssignCalendarLengthOfStay(int lengthOfCalendar, ShopRequest shopRequest)
        {
            bool isAwardCalendarMP2017 = _configuration.GetValue<bool>("NGRPAwardCalendarMP2017Switch");
            if (shopRequest.AwardTravel)
            {
                if (isAwardCalendarMP2017 && lengthOfCalendar == 3)// 3 days calendar i.e. 3 days before and 3 days later from the search date
                {
                    shopRequest.FlexibleDaysAfter = lengthOfCalendar;
                    shopRequest.FlexibleDaysBefore = lengthOfCalendar;
                }
                else if (isAwardCalendarMP2017 && lengthOfCalendar == 6)// 7 days calendar i.e. starting from the search date
                {
                    shopRequest.FlexibleDaysAfter = lengthOfCalendar;
                    shopRequest.FlexibleDaysBefore = 0;
                }
            }
        }
        private void GetMilesDescWithFareDiscloser(MOBSHOPAvailability availability, Session session, List<string> experimentList = null)
        {
            if (_shoppingUtility.EnableFareDisclouserCopyMessage(session.IsReshopChange))
            {
                bool isExperiment = false;

                if (_configuration.GetValue<bool>("IsExperimentEnabled") && experimentList != null && experimentList.Any() &&
                    experimentList.Contains(ShoppingExperiments.NoChangeFee.ToString()))
                {
                    isExperiment = true;
                }
                availability.AvailableAwardMilesWithDesc += _configuration.GetValue<string>("fareDisclosurermessage");
                //availability.AvailableAwardMilesWithDesc += "\nFares are for the entire one-way trip, per person, and include taxes and fees";
                if (_configuration.GetValue<bool>("ChangeFeeWaiverMessagingToggle") && !isExperiment)
                    availability.AvailableAwardMilesWithDesc += "\n" + _shoppingUtility.GetFeeWaiverMessage();

            }
        }
        private List<MOBFSRAlertMessage> HandleFlightShoppingThatHasResults(United.Services.FlightShopping.Common.ShopResponse cslResponse, MOBSHOPShopRequest restShopRequest, bool isShop)
        {
            United.Services.FlightShopping.Common.ShopResponse cslResponseClone = cslResponse.CloneDeep();
            MOBSHOPShopRequest restShopRequestClone = restShopRequest.CloneDeep();

            var allEnhancements = new List<IRule<MOBFSRAlertMessage>>();

            if (_shoppingUtility.EnableAirportDecodeToCityAllAirports())
            {
                if (!restShopRequest.AwardTravel || _configuration.GetValue<bool>("EnableFSRAlertMessages_Nearby_ForAwardBooking"))
                {
                    allEnhancements.Add(new FSRWithResultOriginOrDestinationWithAllAirports(cslResponseClone, restShopRequestClone, _configuration));
                }
            }

            if (restShopRequest.IsShareTripSearchAgain && _configuration.GetValue<bool>("EnableShareTripInSoftRTI") && GeneralHelper.IsApplicationVersionGreater(restShopRequest.Application.Id, restShopRequest.Application.Version.Major, "AndroidShareTripInSoftRTIVersion", "iPhoneShareTripInSoftRTIVersion", "", "", true, _configuration))
            {
                allEnhancements.Add(new FSRWithResultsShareTripSuggestedByDate(restShopRequestClone, _configuration));
            }

            if (_configuration.GetValue<bool>("FSRForceToGSTSwitch") && isShop)
            {
                allEnhancements.Add(new FSRForceToGSTbyOrigin(cslResponseClone, restShopRequestClone, _configuration));
            }

            // Add all enhancements rule for FSR with results here
            allEnhancements.Add(new FSRForceToNearbyOrgAndDest(cslResponseClone, restShopRequestClone, _sessionHelperService, _shoppingUtility, _configuration));
            allEnhancements.Add(new FSRForceToNearbyOrigin(cslResponseClone, restShopRequestClone, _sessionHelperService, _shoppingUtility, _configuration));
            allEnhancements.Add(new FSRForceToNearbyDestination(cslResponseClone, restShopRequestClone, _sessionHelperService, _shoppingUtility, _configuration));

            if (!restShopRequest.AwardTravel || _configuration.GetValue<bool>("EnableFSRAlertMessages_Nearby_ForAwardBooking"))
            {
                allEnhancements.Add(new FSRWithResultSuggestNearbyOrgAndDest(cslResponseClone, restShopRequestClone, _configuration));
                allEnhancements.Add(new FSRWithResultSuggestNearbyDestination(cslResponseClone, restShopRequestClone, _configuration));
                allEnhancements.Add(new FSRWithResultSuggestNearbyOrigin(cslResponseClone, restShopRequestClone, _configuration));
            }

            // Get the first enhancement based on given priorities
            var firstAlert = allEnhancements.FirstOrDefault(rule => rule.ShouldExecuteRule());

            if (_shoppingUtility.CheckFSRRedesignFromShopRequest(restShopRequest))
            {
                if (firstAlert != null)
                {
                    MOBFSRAlertMessage listFsrAlert = new MOBFSRAlertMessage();
                    listFsrAlert = firstAlert.Execute();

                    listFsrAlert.AlertType = MOBFSRAlertMessageType.Information.ToString();
                    return new List<MOBFSRAlertMessage>() { listFsrAlert };
                }
                else
                {
                    return null;
                }
            }
            else
                return firstAlert != null ? new List<MOBFSRAlertMessage> { firstAlert.Execute() } : null;
        }
        private List<MOBFSRAlertMessage> AddMandatoryFSRAlertMessages(MOBSHOPShopRequest restShopRequest, List<MOBFSRAlertMessage> alertMessages)
        {
            if (_configuration.GetValue<bool>("HideFSRChangeFeeWaiverMsg"))
            {
                return alertMessages;
            }
            else
            {
                if (_shoppingUtility.CheckFSRRedesignFromShopRequest(restShopRequest))
                {
                    if (_configuration.GetValue<bool>("ChangeFeeWaiverMessagingToggle"))
                    {
                        if (restShopRequest.Experiments != null && restShopRequest.Experiments.Contains(ShoppingExperiments.NoChangeFee.ToString()))
                        {
                            if (alertMessages == null)
                                alertMessages = new List<MOBFSRAlertMessage>();

                            alertMessages.Add(new MOBFSRAlertMessage()
                            {
                                headerMsg = _configuration.GetValue<string>("ChangeFeeWaiverAlertMessageHeader") ?? "No Change fees",
                                bodyMsg = _configuration.GetValue<string>("ChangeFeeWaiver_Message") ?? "Book now and change your flight with no fee. This includes Basic Economy fares",
                                MessageTypeDescription = FSRAlertMessageType.NoChangeFee,
                                AlertType = MOBFSRAlertMessageType.Information.ToString()
                            });
                        }
                    }
                }
                return alertMessages;
            }
        }
        private Reservation CreateBookingPathReservation(string sessionId, United.Services.FlightShopping.Common.ShopSelectRequest request,
            MOBSHOPAvailability availability, Model.Shopping.ShoppingResponse shop, Session session, FlightReservationResponseBase response)
        {

            Reservation bookingPathReservation = new Reservation();
            if (session.IsReshopChange && !string.IsNullOrEmpty(sessionId))
            {
                var cslReservation = _sessionHelperService.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(sessionId, new United.Service.Presentation.ReservationResponseModel.ReservationDetail().GetType().FullName, new List<string> { sessionId, new United.Service.Presentation.ReservationResponseModel.ReservationDetail().GetType().FullName }).Result;
                //var cslReservation = _sessionHelperService.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(Headers.ContextValues, new Reservation().ObjectName).Result;
                bookingPathReservation = _sessionHelperService.GetSession<Reservation>(Headers.ContextValues.SessionId, bookingPathReservation.ObjectName, new List<string>() { Headers.ContextValues.SessionId, bookingPathReservation.ObjectName }).Result;
                //bookingPathReservation = _sessionHelperService.GetSession<Reservation>(Headers.ContextValues, new Reservation().ObjectName).Result;
                //var cslReservation = United.Persist.FilePersist.Load<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(sessionId, (new United.Service.Presentation.ReservationResponseModel.ReservationDetail()).GetType().FullName);
                //bookingPathReservation = FilePersist.Load<Reservation>(sessionId, (new Reservation()).ObjectName);
                //cslReservation = JsonConvert.DeserializeObject<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(bookingPathReservation.CSLReservationJSONFormat);
                bookingPathReservation.Trips = availability.Reservation.Trips;
                bookingPathReservation = AssignTravelerSeatObjectForSelectedChangeFlight(session, bookingPathReservation, cslReservation);
                if (bookingPathReservation != null && session.IsReshopChange)
                {
                    if (bookingPathReservation.ReshopTrips != null)
                    {
                        if (bookingPathReservation.ReshopTrips.Count <= availability.Reservation.Trips.Count)
                        {

                            int countDiff = availability.Reservation.Trips.Count - bookingPathReservation.ReshopTrips.Count;
                            if (countDiff > 0)
                            {
                                for (int i = 0; i < countDiff; i++)
                                {
                                    ReshopTrip reshObj1 = new ReshopTrip();
                                    reshObj1.IsReshopTrip = true;
                                    bookingPathReservation.ReshopTrips.Add(reshObj1);
                                }
                            }

                            for (int tripIndex = 0; tripIndex < availability.Reservation.Trips.Count; tripIndex++)
                            {
                                if (bookingPathReservation.ReshopTrips[tripIndex].IsReshopTrip)
                                {
                                    bookingPathReservation.ReshopTrips[tripIndex].ChangeTrip = availability.Reservation.Trips[tripIndex];
                                }
                                else
                                {
                                    bookingPathReservation.ReshopTrips[tripIndex].ChangeTrip = null;
                                }
                            }

                        }
                        else if (Convert.ToBoolean(_configuration.GetValue<string>("ReshopChange-PartiallyUsedFix"))) // Fix for partailly used pnr
                        {
                            var newTrip = availability.Reservation.Trips.Where(p => p.ChangeType == MOBSHOPTripChangeType.ChangeFlight).ToList();
                            var reshopTrip = bookingPathReservation.ReshopTrips.Where(p => p.IsReshopTrip == true).ToList();
                            for (int tripIndex = 0; tripIndex < newTrip.Count(); tripIndex++)
                            {
                                if (newTrip[tripIndex].ChangeType == MOBSHOPTripChangeType.ChangeFlight)
                                {
                                    reshopTrip[tripIndex].ChangeTrip = newTrip[tripIndex];
                                }
                                else
                                {
                                    reshopTrip[tripIndex].ChangeTrip = null;
                                }
                            }
                        }
                    }

                    bookingPathReservation.IsReshopChange = true;
                    availability.Reservation.IsReshopChange = true;
                    availability.Reservation.Reshop = bookingPathReservation.Reshop;
                    bookingPathReservation.SeatPrices = null;
                    bookingPathReservation.ClubPassPurchaseRequest = null;
                    if (bookingPathReservation.TravelersCSL != null && bookingPathReservation.TravelersCSL.Count > 0)
                    {
                        availability.Reservation.TravelersCSL = new List<MOBCPTraveler>();
                        foreach (var travelCSL in bookingPathReservation.TravelersCSL)
                        {
                            availability.Reservation.TravelersCSL.Add(travelCSL.Value);
                        }
                    }

                    if (availability.Reservation.Reshop != null
                       && response?.DisplayCart?.DisplayPrices != null)
                    {
                        availability.Reservation.Reshop.IsResidualFFCRAvailable
                            = response.DisplayCart.DisplayPrices.Any(x => !string.IsNullOrEmpty(x.Status)
                            && string.Equals(x.Status, "FFCR", StringComparison.OrdinalIgnoreCase));
                    }

                    UpdatePricesAndTaxesForReshopChangeFlight(availability, session, cslReservation);
                }
                else
                {
                    bookingPathReservation = new Reservation();
                }
            }

            bookingPathReservation.SessionId = sessionId;
            bookingPathReservation.CartId = request.CartId;
            bookingPathReservation.PointOfSale = shop.Request.CountryCode;

            if (string.IsNullOrEmpty(session.EmployeeId))
            {
                bookingPathReservation.FareLock = availability.Reservation.FareLock;
            }
            bookingPathReservation.Trips = availability.Reservation.Trips;
            bookingPathReservation.Prices = availability.Reservation.Prices;
            bookingPathReservation.Taxes = availability.Reservation.Taxes;
            bookingPathReservation.TravelOptions = availability.Reservation.TravelOptions;
            bookingPathReservation.NumberOfTravelers = availability.Reservation.NumberOfTravelers;
            bookingPathReservation.SearchType = availability.Reservation.SearchType;
            bookingPathReservation.AwardTravel = availability.Reservation.AwardTravel;
            bookingPathReservation.FlightShareMessage = availability.Reservation.FlightShareMessage;
            bookingPathReservation.IsRefundable = availability.Reservation.IsRefundable;
            bookingPathReservation.ISInternational = availability.Reservation.ISInternational;
            bookingPathReservation.ISFlexibleSegmentExist = availability.Reservation.ISFlexibleSegmentExist;
            bookingPathReservation.PKDispenserPublicKey = availability.Reservation.PKDispenserPublicKey;
            bookingPathReservation.LMXFlights = availability.Reservation.LMXFlights;
            bookingPathReservation.IneligibleToEarnCreditMessage = availability.Reservation.IneligibleToEarnCreditMessage;
            bookingPathReservation.OaIneligibleToEarnCreditMessage = availability.Reservation.OaIneligibleToEarnCreditMessage;
            bookingPathReservation.IsELF = availability.Reservation.IsELF;
            bookingPathReservation.IsSSA = availability.Reservation.IsSSA;
            bookingPathReservation.SeatAssignmentMessage = availability.Reservation.SeatAssignmentMessage;
            bookingPathReservation.AlertMessages = availability.Reservation.AlertMessages;
            bookingPathReservation.Messages = availability.Reservation.Messages;
            bookingPathReservation.IsRedirectToSecondaryPayment = availability.Reservation.IsRedirectToSecondaryPayment;
            bookingPathReservation.IsUpgradedFromEntryLevelFare = availability.Reservation.IsUpgradedFromEntryLevelFare;
            bookingPathReservation.ELFMessagesForRTI = availability.Reservation.ELFMessagesForRTI.Clone();
            bookingPathReservation.CheckedbagChargebutton = availability.Reservation.CheckedbagChargebutton;
            bookingPathReservation.IsCubaTravel = availability.Reservation.IsCubaTravel;

            if (availability.Reservation.IsCubaTravel)
            {
                bookingPathReservation.CubaTravelInfo = availability.Reservation.CubaTravelInfo;
            }

            bookingPathReservation.FormOfPaymentType = availability.Reservation.FormOfPaymentType;
            if ((availability.Reservation.FormOfPaymentType == MOBFormofPayment.PayPal || availability.Reservation.FormOfPaymentType == MOBFormofPayment.PayPalCredit) && availability.Reservation.PayPal != null)
            {
                bookingPathReservation.PayPal = availability.Reservation.PayPal;
                bookingPathReservation.PayPalPayor = availability.Reservation.PayPalPayor;
            }
            if (availability.Reservation.FormOfPaymentType == MOBFormofPayment.Masterpass)
            {
                if (availability.Reservation.MasterpassSessionDetails != null)
                    bookingPathReservation.MasterpassSessionDetails = availability.Reservation.MasterpassSessionDetails;
                if (availability.Reservation.Masterpass != null)
                    bookingPathReservation.Masterpass = availability.Reservation.Masterpass;
            }
            try
            {
                //TODO :ASSEMBLY NOT LOADING CHECK THIS LATER
                var segments = CommonMethods.FlattenSegmentsForSeatDisplay(response.Reservation);
                bookingPathReservation.CSLReservationJSONFormat = JsonConvert.SerializeObject(segments);
                if (availability.Reservation.FOPOptions != null && availability.Reservation.FOPOptions.Count > 0) //FOP Options Fix Venkat 12/08
                {
                    bookingPathReservation.FOPOptions = availability.Reservation.FOPOptions;
                }
                if (availability.Reservation.ShopReservationInfo != null)
                {
                    bookingPathReservation.ShopReservationInfo = availability.Reservation.ShopReservationInfo;
                }
                if (availability.Reservation.ShopReservationInfo2 != null)
                {
                    bookingPathReservation.ShopReservationInfo2 = availability.Reservation.ShopReservationInfo2;
                }
            }
            catch (Exception)
            {

            }
            return bookingPathReservation;
        }

        private Reservation AssignTravelerSeatObjectForSelectedChangeFlight(Session session, Reservation reservation, United.Service.Presentation.ReservationResponseModel.ReservationDetail cslReservation)
        {

            if (session.IsReshopChange)
            {
                Model.Shopping.ShoppingResponse persistShop = new Model.Shopping.ShoppingResponse();
                persistShop = _sessionHelperService.GetSession<Model.Shopping.ShoppingResponse>(Headers.ContextValues, persistShop.ObjectName).Result;
                //persistShop = Persist.FilePersist.Load<ShoppingResponse>(session.SessionId, persistShop.ObjectName);
                if (persistShop == null)
                {
                    throw new MOBUnitedException("Could not find your booking session.");
                }
                Reshopping reshopping = new Reshopping(_configuration, _logger, _sessionHelperService, _shoppingUtility);
                reservation = reshopping.AssignPnrTravelerToReservation(persistShop.Request, reservation, cslReservation);

                foreach (var traveler in reservation.TravelersCSL)
                {
                    int tripAndFlightIndexForSeatInsert = 0;
                    foreach (var trip in reservation.Trips)
                    {
                        foreach (var flattenedFlight in trip.FlattenedFlights)
                        {
                            foreach (var flight in flattenedFlight.Flights)
                            {
                                if (traveler.Value.Seats == null || !traveler.Value.Seats.Exists(s => s.Origin == flight.Origin && s.Destination == flight.Destination && s.FlightNumber == flight.FlightNumber))
                                {
                                    if (traveler.Value.Seats == null)
                                        traveler.Value.Seats = new List<MOBSeat>();
                                    var mobseat = new MOBSeat();
                                    mobseat.TravelerSharesIndex = traveler.Key;
                                    mobseat.Destination = flight.Destination;
                                    mobseat.Origin = flight.Origin;
                                    mobseat.UAOperated = (flight.OperatingCarrier.ToUpper() == "UA");
                                    //mobseat.ProgramCode = flight.pr;
                                    traveler.Value.Seats.Insert(tripAndFlightIndexForSeatInsert, mobseat);
                                    if (_shoppingUtility.IsMilesFOPEnabled())
                                    {
                                        mobseat.OldSeatMiles = Convert.ToInt32(_configuration.GetValue<string>("milesFOP"));
                                        mobseat.DisplayOldSeatMiles = ShopStaticUtility.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false);
                                    }
                                }
                                tripAndFlightIndexForSeatInsert++;
                            }
                        }
                    }

                }
            }
            return reservation;
        }

        private void UpdatePricesAndTaxesForReshopChangeFlight(MOBSHOPAvailability availability, Session session, United.Service.Presentation.ReservationResponseModel.ReservationDetail cslReservation)
        {
            if (session.IsReshopChange)
            {
                if (session.IsAward)
                {
                    AwardUpdatePricesAndTaxesForReshopChangeFlight(availability, session, cslReservation);
                }
                else
                {
                    RevenueReshopPricesAndTaxesForReshopChangeFlight(availability, session, cslReservation);
                }
            }
        }

        private void AwardUpdatePricesAndTaxesForReshopChangeFlight(MOBSHOPAvailability availability, Session session, United.Service.Presentation.ReservationResponseModel.ReservationDetail cslReservation)
        {
            var reservation = availability.Reservation;
            availability.Trip = null;
            availability.Reservation.Reshop.IsRefundBillingAddressRequired = false;
            availability.Reservation.Reshop.AncillaryRefundFormOfPayment = "To original form of payment";
            reservation.Taxes.RemoveAll(p => p.TaxCode.ToUpper() == "CHANGEFEE" || p.TaxCode.ToUpper() == "ADDCOLLECT");

            bool isChangeFeeExistInPrices = false;
            bool isAddCollectExistInPrices = false;
            double newtaxtotal = 0;
            double originalTotal = 0;
            double totalWithChangeFee = 0;

            string waivedDesc = "";
            string changeWaivedPriceValue = "";

            List<Model.Shopping.MOBSHOPPrice> awardNewPrices = new List<Model.Shopping.MOBSHOPPrice>();

            //var cslReservation = United.Persist.FilePersist.Load<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(Convert.ToString(session.SessionId), (new United.Service.Presentation.ReservationResponseModel.ReservationDetail()).GetType().FullName);
            if (cslReservation != null && cslReservation.Detail != null && !cslReservation.Detail.Characteristic.IsNullOrEmpty())
            {
                waivedDesc = ShopStaticUtility.GetCharacteristicDescription(cslReservation.Detail.Characteristic.ToList(), "24HrFlexibleBookingPolicy");
            }

            if (!string.IsNullOrEmpty(waivedDesc))
            {
                //reservation.Prices.Add(GetPriceObject("CHANGEFEE", "Change fee", 0, "Waived"));
                changeWaivedPriceValue = "Waived";
            }

            foreach (var price in reservation.Prices)
            {
                switch (price.DisplayType.ToUpper())
                {
                    case "NEWTOTAL":
                    case "TRAVELERPRICE":
                    case "NEWTAXTOTAL":
                        var totalTravels = (reservation.TravelersCSL != null ? reservation.TravelersCSL.Count : 0);
                        var totlaTravelsString = string.Empty;
                        if (totalTravels > 1)
                        {
                            totlaTravelsString = " (" + totalTravels.ToString() + " travelers)";
                        }
                        else if (totalTravels == 1)
                        {
                            totlaTravelsString = " (" + totalTravels.ToString() + " traveler)";
                        }
                        price.PriceTypeDescription = "New trip" + totlaTravelsString;
                        if (price.DisplayType.ToUpper() == "NEWTAXTOTAL" || price.DisplayType.ToUpper() == "NEWTOTAL")
                        {
                            newtaxtotal += price.Value;
                        }
                        break;
                    case "NEWMILES":
                        Model.Shopping.MOBSHOPPrice milesPrice = new Model.Shopping.MOBSHOPPrice();
                        milesPrice.CurrencyCode = price.CurrencyCode;
                        milesPrice.DisplayType = "NEWTOTAL";
                        milesPrice.DisplayValue = price.DisplayValue;
                        milesPrice.PriceIndex = price.PriceIndex;
                        milesPrice.PriceType = price.PriceType;
                        milesPrice.Status = price.Status;
                        milesPrice.TotalBaseFare = price.TotalBaseFare;
                        milesPrice.TotalOtherTaxes = price.TotalOtherTaxes;
                        milesPrice.Value = price.Value;
                        milesPrice.Waived = price.Waived;

                        var totalMilesTravels = (reservation.TravelersCSL != null ? reservation.TravelersCSL.Count : 0);
                        var totalTravelsString = string.Empty;
                        if (totalMilesTravels > 1)
                        {
                            totalTravelsString = " (" + totalMilesTravels.ToString() + " travelers)";
                        }
                        else if (totalMilesTravels == 1)
                        {
                            totalTravelsString = " (" + totalMilesTravels.ToString() + " traveler)";
                        }
                        milesPrice.PriceTypeDescription = "New trip" + totalTravelsString;
                        milesPrice.FormattedDisplayValue = string.Format("{0:N}", price.Value).Replace(".00", string.Empty) + " miles";
                        awardNewPrices.Add(milesPrice);
                        Model.Shopping.MOBSHOPPrice newBaseMiles = new Model.Shopping.MOBSHOPPrice();
                        newBaseMiles = milesPrice.Clone();
                        newBaseMiles.DisplayType = "NEWBASETOTAL";
                        awardNewPrices.Add(newBaseMiles);
                        break;
                    case "ORIGINALTOTAL":
                        if (reservation.AwardTravel)
                        {
                            price.DisplayType = "ORIGINALTOTAL";
                            price.PriceTypeDescription = "Original trip";
                            price.FormattedDisplayValue = "- " + string.Format("{0:N}", price.Value).Replace(".00", string.Empty) + " miles";
                        }
                        else
                        {
                            price.PriceTypeDescription = "Original trip";
                            originalTotal += price.Value;
                        }
                        break;
                    case "TAXDIFFERENCE":
                        price.PriceTypeDescription = "Tax Difference";
                        break;
                    case "ORIGINALTAXTOTAL":
                        if (!reservation.AwardTravel)
                        {
                            originalTotal += price.Value;
                        }
                        break;
                    case "REFUNDPRICE":
                        if (!reservation.AwardTravel)
                        {
                            price.PriceTypeDescription = "Total refund";
                            var refundTypeMessage = GetOriginalFormOfPaymentLabelForReshopChange(price.Status, cslReservation);
                            if (refundTypeMessage != string.Empty)
                            {
                                availability.Reservation.Reshop.RefundFormOfPaymentMessage = refundTypeMessage;
                                if (price.Status != "CC")
                                {
                                    availability.Reservation.Reshop.IsRefundBillingAddressRequired = true;
                                }
                            }
                        }
                        else
                        {
                            price.DisplayType = "OLDREFUND";
                        }
                        break;
                    case "MILEAGEDIFFERENCE":
                        if (reservation.AwardTravel)
                        {
                            Model.Shopping.MOBSHOPPrice refundPrice = new Model.Shopping.MOBSHOPPrice();
                            refundPrice.CurrencyCode = price.CurrencyCode;
                            refundPrice.DisplayValue = price.DisplayValue;
                            refundPrice.PriceIndex = price.PriceIndex;
                            refundPrice.PriceType = price.PriceType;
                            refundPrice.Status = price.Status;
                            refundPrice.TotalBaseFare = price.TotalBaseFare;
                            refundPrice.TotalOtherTaxes = price.TotalOtherTaxes;
                            refundPrice.Value = price.Value;
                            refundPrice.Waived = price.Waived;

                            Model.Shopping.MOBSHOPPrice totalPrice = new Model.Shopping.MOBSHOPPrice();
                            totalPrice.CurrencyCode = price.CurrencyCode;
                            totalPrice.DisplayValue = price.DisplayValue;
                            totalPrice.PriceIndex = price.PriceIndex;
                            totalPrice.PriceType = price.PriceType;
                            totalPrice.Status = price.Status;
                            totalPrice.TotalBaseFare = price.TotalBaseFare;
                            totalPrice.TotalOtherTaxes = price.TotalOtherTaxes;
                            totalPrice.Value = price.Value;
                            totalPrice.Waived = price.Waived;

                            var refundTypeMessage = GetOriginalFormOfPaymentLabelForReshopChange(price.Status, cslReservation);
                            if (refundTypeMessage != string.Empty)
                            {
                                availability.Reservation.Reshop.RefundFormOfPaymentMessage = refundTypeMessage;
                                if (price.Status != "CC")
                                {
                                    availability.Reservation.Reshop.IsRefundBillingAddressRequired = true;
                                }
                            }

                            if (price.Value == 0)
                            {
                                refundPrice.Value = 0;
                                refundPrice.PriceTypeDescription = "Total refund";
                                refundPrice.DisplayType = "REFUNDPRICE";
                                refundPrice.FormattedDisplayValue = "0 miles";
                                awardNewPrices.Add(refundPrice);
                            }
                            if (price.Value < 0)
                            {
                                refundPrice.PriceTypeDescription = "Total refund";
                                refundPrice.DisplayType = "REFUNDPRICE";
                                refundPrice.FormattedDisplayValue = string.Format("{0:N}", price.Value).Trim('-').Replace(".00", string.Empty) + " miles";
                                awardNewPrices.Add(refundPrice);

                                totalPrice.PriceTypeDescription = "Total due";
                                totalPrice.DisplayType = "TOTAL";
                                totalPrice.DisplayValue = "0.0";
                                totalPrice.Value = 0.0;
                                totalPrice.FormattedDisplayValue = "0 miles";
                                awardNewPrices.Add(totalPrice);
                            }
                            if (price.Value >= 0)
                            {
                                refundPrice.Value = 0;
                                refundPrice.PriceTypeDescription = "Total refund";
                                refundPrice.DisplayType = "REFUNDPRICE";
                                refundPrice.DisplayValue = "0.0";
                                refundPrice.FormattedDisplayValue = "0 miles";
                                awardNewPrices.Add(refundPrice);

                                totalPrice.PriceTypeDescription = "Total due";
                                totalPrice.DisplayType = "TOTAL";
                                totalPrice.Value = 0;
                                totalPrice.DisplayValue = "0.0";
                                totalPrice.FormattedDisplayValue = string.Format("{0:N}", price.Value).Trim('-').Replace(".00", string.Empty) + " miles";
                                awardNewPrices.Add(totalPrice);
                            }
                        }
                        break;
                    case "CHANGEFEE":
                        price.PriceTypeDescription = "Change fee";
                        isChangeFeeExistInPrices = true;
                        if ((price.Waived || changeWaivedPriceValue == "Waived") && price.Value == 0)
                        {
                            price.FormattedDisplayValue = "Waived";
                            availability.Reservation.Reshop.FeeWaiverMessage = (_configuration.GetValue<string>("ReshopChange-FEEWAIVEDMESSAGE") ?? "");
                        }
                        else
                        {
                            availability.Reservation.Reshop.FeeWaiverMessage = string.Empty;
                        }
                        break;
                    case "TOTAL":
                        price.PriceTypeDescription = "Total due";
                        if (!reservation.AwardTravel)
                        {

                            isAddCollectExistInPrices = true;
                            totalWithChangeFee += price.Value;
                        }
                        else
                        {
                            price.DisplayType = "OLDTOTAL";
                        }
                        break;
                }
            }

            foreach (var newPrice in awardNewPrices)
            {
                reservation.Prices.Add(newPrice);
            }
            //AddedNewTotalDollarAmount(reservation);
            if (reservation.AwardTravel)
            {
                try
                {
                    // New total dollar amount
                    var newTotalDollar = reservation.Prices.FirstOrDefault(x => x.DisplayType == "TAX");
                    if (reservation.Prices.FirstOrDefault(x => x.DisplayType == "RBF") != null)
                        reservation.Prices.Add(GetPriceObject("NEW_TOTAL_DOLLAR_AMOUNT", string.Empty, (Convert.ToDouble(newTotalDollar.DisplayValue) + Convert.ToDouble(reservation.Prices.FirstOrDefault(x => x.DisplayType == "RBF").DisplayValue))));
                    else
                        reservation.Prices.Add(GetPriceObject("NEW_TOTAL_DOLLAR_AMOUNT", string.Empty, Convert.ToDouble(newTotalDollar.DisplayValue)));
                    // Original total dollar amount
                    var orginalTotalDollar = reservation.Prices.FirstOrDefault(x => x.DisplayType.ToUpper() == "ORIGINALTAXTOTAL");
                    reservation.Prices.Add(GetPriceObject("ORIGINAL_TOTAL_DOLLAR_AMOUNT", string.Empty, Convert.ToDouble(orginalTotalDollar.DisplayValue)));
                    var originalDollarAmount = reservation.Prices.First(p => p.DisplayType == "ORIGINAL_TOTAL_DOLLAR_AMOUNT");
                    originalDollarAmount.FormattedDisplayValue = "- " + originalDollarAmount.FormattedDisplayValue;
                    originalDollarAmount.PriceTypeDescription = "Taxes and Fees Total";
                    orginalTotalDollar.DisplayType = "OLDORIGINALTAXTOTAL";

                    // Grand total dollar amount                                            
                    double grandTotal = Convert.ToDouble(ReshopAwardAirfareDisplayValueInDecimal(reservation.Prices));
                    if (grandTotal >= 0)
                    {
                        reservation.Prices.Add(GetPriceObject("GRAND TOTAL", string.Empty, grandTotal));
                    }
                    else
                    {
                        reservation.Prices.Add(GetPriceObject("GRAND TOTAL", string.Empty, 0.0));
                    }

                    availability.Reservation.Reshop.IsRefundBillingAddressRequired = false;

                    double taxDifference = 0.0;
                    if (reservation.Prices.Exists(p => p.DisplayType.ToUpper() == "TAXDIFFERENCE"))
                    {
                        taxDifference = Convert.ToDouble(reservation.Prices.First(p => p.DisplayType.ToUpper() == "TAXDIFFERENCE").Value);
                        string status = reservation.Prices.First(p => p.DisplayType.ToUpper() == "TAXDIFFERENCE").Status;
                        availability.Reservation.Reshop.RefundFormOfPaymentMessage = string.Empty;
                        if (taxDifference < 0)
                        {
                            reservation.Prices.Add(GetPriceObject("REFUND_PRICE_DOLLAR_AMOUNT", string.Empty, taxDifference * (-1)));

                            var refundTypeMessage = GetOriginalFormOfPaymentLabelForReshopChange(status, cslReservation);
                            if (refundTypeMessage != string.Empty)
                            {
                                availability.Reservation.Reshop.RefundFormOfPaymentMessage = refundTypeMessage;
                                if (status != "CC")
                                {
                                    availability.Reservation.Reshop.IsRefundBillingAddressRequired = true;
                                }
                            }
                            availability.Reservation.Reshop.RefundFormOfPaymentMessage = "To original form of payment";
                        }
                        else
                        {
                            reservation.Prices.Add(GetPriceObject("REFUND_PRICE_DOLLAR_AMOUNT", string.Empty, 0.0));
                            availability.Reservation.Reshop.RefundFormOfPaymentMessage = string.Empty;
                        }
                    }
                    else
                    {
                        reservation.Prices.Add(GetPriceObject("REFUND_PRICE_DOLLAR_AMOUNT", string.Empty, 0.0));
                    }

                    AwardTotalTravelOptionAndRefund(ref reservation);

                    PopulateRefundIfDollarAmountIsNotZero(reservation);
                }
                catch { }
            }

            if (!isChangeFeeExistInPrices)
            {
                reservation.Prices.Add(GetPriceObject("CHANGEFEE", "Change fee", 0, (changeWaivedPriceValue == "Waived" ? "Waived" : "$0")));
                if (changeWaivedPriceValue == "Waived")
                {
                    availability.Reservation.Reshop.FeeWaiverMessage = (_configuration.GetValue<string>("ReshopChange-FEEWAIVEDMESSAGE") ?? "");
                }
                else
                {
                    availability.Reservation.Reshop.FeeWaiverMessage = string.Empty;
                }
            }


            if (!isAddCollectExistInPrices)
            {
                reservation.Prices.Add(GetPriceObject("ADDCOLLECT", "Total due", 0));
            }

            if (!reservation.AwardTravel)
            {
                var totalPrice = reservation.Prices.First(p => p.DisplayType == "TOTAL");
                AssignValueToPriceAndDisplayFormat(totalWithChangeFee, totalPrice, false);

                var originalPrice = reservation.Prices.First(p => p.DisplayType == "ORIGINALTOTAL");
                AssignValueToPriceAndDisplayFormat(originalTotal, originalPrice, true);

                var newTOtal = reservation.Prices.First(p => p.DisplayType == "NEWTOTAL");
                reservation.Prices.Add(GetPriceObject("NEWBASETOTAL", newTOtal.PriceTypeDescription, newTOtal.Value));
                AssignValueToPriceAndDisplayFormat(newtaxtotal, newTOtal, false);
            }
        }

        private void PopulateRefundIfDollarAmountIsNotZero(MOBSHOPReservation reservation)
        {
            var refundDollarAmount = reservation.Prices.First(p => p.DisplayType == "REFUND_PRICE_DOLLAR_AMOUNT");
            var refundMiles = reservation.Prices.First(p => p.DisplayType == "REFUNDPRICE");

            if (refundDollarAmount.Value == 0 && refundMiles.Value == 0)
            {
                reservation.Prices.RemoveWhere(p => p.DisplayType == "REFUND_PRICE_DOLLAR_AMOUNT");
                reservation.Prices.RemoveWhere(p => p.DisplayType == "REFUNDPRICE");
            }
        }

        private void AssignValueToPriceAndDisplayFormat(double priceValue, Model.Shopping.MOBSHOPPrice mobShopPrice, bool isFormattedDisplayValueShouldBeNegetive)
        {
            if (mobShopPrice != null)
            {
                CultureInfo ci = TopHelper.GetCultureInfo(mobShopPrice.CurrencyCode); ;
                mobShopPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(priceValue.ToString(), ci, false); ;// string.Format("{0:c}", price.Amount);
                if (isFormattedDisplayValueShouldBeNegetive)
                {
                    mobShopPrice.FormattedDisplayValue = "-" + mobShopPrice.FormattedDisplayValue;
                    priceValue = priceValue * -1;
                }
                mobShopPrice.Value = priceValue;
                mobShopPrice.DisplayValue = string.Format("{0:#,0.00}", mobShopPrice.Value);
            }
        }

        private Model.Shopping.MOBSHOPPrice GetPriceObject(string displayType, string priceTypeTitle, double value, string displayValue = "")
        {
            Model.Shopping.MOBSHOPPrice price = new Model.Shopping.MOBSHOPPrice();
            price.DisplayType = displayType;
            price.CurrencyCode = "USD";
            price.PriceTypeDescription = priceTypeTitle;
            price.Value = value;
            price.DisplayValue = string.IsNullOrEmpty(displayValue) ? value.ToString() : displayValue;
            var ci = TopHelper.GetCultureInfo(price.CurrencyCode);
            price.FormattedDisplayValue = string.IsNullOrEmpty(displayValue) ? TopHelper.FormatAmountForDisplay(price.Value.ToString(), ci, false) : displayValue;
            price.PriceType = displayType;
            return price;
        }


        private string GetOriginalFormOfPaymentLabelForReshopChange(string status, United.Service.Presentation.ReservationResponseModel.ReservationDetail cslReservation)
        {
            string msg = string.Empty;
            if (status == "PP" || status == "AP")
            {
                msg = string.Format((_configuration.GetValue<string>("ReshopChange-RTIOriginalFOPMessage") ?? ""), "PayPal");
            }
            else if (status == "ETC")
            {
                msg = string.Format(_configuration.GetValue<string>("ReshopChange-RTIElectronicCertMessage") ?? "");
            }
            else if (status == "CC")
            {
                var cslPayment = cslReservation.Detail.Payment;
                var lastFourDigits = string.Empty;
                if ((!string.IsNullOrEmpty(cslPayment.AccountNumber)))
                {
                    lastFourDigits = "**" + (cslPayment.AccountNumber.Length > 4 ? cslPayment.AccountNumber.Substring(cslPayment.AccountNumber.Length - 4) : cslPayment.AccountNumber);
                }

                if (!string.IsNullOrEmpty(lastFourDigits))
                {
                    msg = string.Format((_configuration.GetValue<string>("ReshopChange-RTIOriginalFOPMessage") ?? ""), lastFourDigits);
                }

            }

            return msg;
        }

        private void AwardTotalTravelOptionAndRefund(ref MOBSHOPReservation reservation)
        {
            // To set REFUND_PRICE_DOLLAR_AMOUNT starts here ....
            var refundPriceDollarAmount
                = reservation.Prices.FirstOrDefault(x => string.Equals(x.DisplayType, "REFUND_PRICE_DOLLAR_AMOUNT", StringComparison.OrdinalIgnoreCase));

            var totalTravelOptionRefund
                = reservation.Prices.FirstOrDefault(x => string.Equals(x.DisplayType, "TOTALTRAVELOPTIONREFUND", StringComparison.OrdinalIgnoreCase));

            if (totalTravelOptionRefund != null && totalTravelOptionRefund.Value > 0)
            {
                decimal totalrefund = (totalTravelOptionRefund != null && refundPriceDollarAmount != null) ?
                                Convert.ToDecimal(totalTravelOptionRefund.DisplayValue) + Convert.ToDecimal(refundPriceDollarAmount.DisplayValue) :
                                Convert.ToDecimal(totalTravelOptionRefund.DisplayValue);

                CultureInfo ci = null;
                double tempDouble = 0;
                double.TryParse(Convert.ToString(totalrefund), out tempDouble);
                if (ci == null) ci = TopHelper.GetCultureInfo(totalTravelOptionRefund.CurrencyCode);

                refundPriceDollarAmount.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);
                refundPriceDollarAmount.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalrefund, ci, false);
                refundPriceDollarAmount.DisplayValue = string.Format("{0:#,0.00}", totalrefund);
            }
            //REFUND_PRICE_DOLLAR_AMOUNT ends here
        }

        private void RevenueReshopPricesAndTaxesForReshopChangeFlight(MOBSHOPAvailability availability, Session session, United.Service.Presentation.ReservationResponseModel.ReservationDetail cslReservation)
        {
            var reservation = availability.Reservation;

            if (session.IsReshopChange)
            {
                availability.Trip = null;
                availability.Reservation.Reshop.IsRefundBillingAddressRequired = false;
                availability.Reservation.Reshop.AncillaryRefundFormOfPayment = "To original form of payment";
                reservation.Taxes.RemoveAll(p => p.TaxCode.ToUpper() == "CHANGEFEE" || p.TaxCode.ToUpper() == "ADDCOLLECT");

                bool isChangeFeeExistInPrices = false;
                bool isAddCollectExistInPrices = false;
                double newtaxtotal = 0;
                double originalTotal = 0;
                double totalWithChangeFee = 0;

                string waivedDesc = "";
                string changeWaivedPriceValue = "";
                bool isNonResidualCredit = false;
                //var cslReservation = United.Persist.FilePersist.Load<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(Convert.ToString(session.SessionId), (new United.Service.Presentation.ReservationResponseModel.ReservationDetail()).GetType().FullName);
                if (cslReservation != null && cslReservation.Detail != null && !cslReservation.Detail.Characteristic.IsNullOrEmpty())
                {
                    waivedDesc = ShopStaticUtility.GetCharacteristicDescription(cslReservation.Detail.Characteristic.ToList(), "24HrFlexibleBookingPolicy");
                }

                if (!string.IsNullOrEmpty(waivedDesc))
                {
                    //reservation.Prices.Add(GetPriceObject("CHANGEFEE", "Change fee", 0, "Waived"));
                    changeWaivedPriceValue = "Waived";
                }

                foreach (var price in reservation.Prices)
                {
                    switch (price.DisplayType.ToUpper())
                    {
                        case "NEWTOTAL":
                        case "TRAVELERPRICE":
                        case "NEWTAXTOTAL":
                            var totalTravels = (reservation.TravelersCSL != null ? reservation.TravelersCSL.Count : 0);
                            var totlaTravelsString = string.Empty;
                            if (totalTravels > 1)
                            {
                                totlaTravelsString = " (" + totalTravels.ToString() + " travelers)";
                            }
                            else if (totalTravels == 1)
                            {
                                totlaTravelsString = " (" + totalTravels.ToString() + " traveler)";
                            }
                            price.PriceTypeDescription = "New trip" + totlaTravelsString;
                            if (price.DisplayType.ToUpper() == "NEWTAXTOTAL" || price.DisplayType.ToUpper() == "NEWTOTAL")
                            {
                                newtaxtotal += price.Value;
                            }
                            break;
                        case "TAXDIFFERENCE":
                            price.PriceTypeDescription = "Tax Difference";
                            break;
                        case "FAREDIFFERENCE":
                            price.PriceTypeDescription = "Fare Difference";
                            break;
                        case "ORIGINALTOTAL":
                            price.PriceTypeDescription = "Original trip";
                            originalTotal += price.Value;
                            break;
                        case "ORIGINALTAXTOTAL":
                            originalTotal += price.Value;
                            break;
                        case "REFUNDPRICE":
                            price.PriceTypeDescription = "Total refund";
                            if (price.Status != string.Empty)
                            {
                                var refundTypeMessage = GetOriginalFormOfPaymentLabelForReshopChange(price.Status, cslReservation);
                                if (refundTypeMessage != string.Empty)
                                {
                                    availability.Reservation.Reshop.RefundFormOfPaymentMessage = refundTypeMessage;
                                    if (price.Status != "CC")
                                    {
                                        availability.Reservation.Reshop.IsRefundBillingAddressRequired = true;
                                    }
                                }

                                if (_shoppingUtility.IncludeReshopFFCResidual(session.AppID, session.VersionID))
                                {
                                    if (string.Equals(price.Status, "FFCR", StringComparison.OrdinalIgnoreCase))
                                    {
                                        price.Status = "RESIDUALCREDIT";
                                        price.PriceTypeDescription = "Total credit";
                                        availability.Reservation.Reshop.RefundFormOfPaymentMessage = string.Empty;
                                    }
                                }
                                else
                                {
                                    if (price.Status == "NORESIDUALCREDIT")
                                    {
                                        isNonResidualCredit = true;
                                    }
                                }
                            }
                            break;
                        case "CHANGEFEE":
                            price.PriceTypeDescription = "Change fee";
                            isChangeFeeExistInPrices = true;
                            if ((price.Waived || changeWaivedPriceValue == "Waived") && price.Value == 0)
                            {
                                price.FormattedDisplayValue = "Waived";
                                availability.Reservation.Reshop.FeeWaiverMessage = (_configuration.GetValue<string>("ReshopChange-FEEWAIVEDMESSAGE") ?? "");

                                if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                                {
                                    price.FormattedDisplayValue = "No Fee";
                                    availability.Reservation.Reshop.FeeWaiverMessage = string.Empty;
                                }
                            }
                            else
                            {
                                availability.Reservation.Reshop.FeeWaiverMessage = string.Empty;
                            }
                            break;
                        case "TOTAL":
                            price.PriceTypeDescription = "Total due";
                            isAddCollectExistInPrices = true;
                            totalWithChangeFee += price.Value;
                            break;

                    }
                }

                if (!isChangeFeeExistInPrices)
                {
                    if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                    {
                        reservation.Prices.Add(GetPriceObject("CHANGEFEE", "Change fee", 0, "No Fee"));
                    }
                    else
                    {
                        reservation.Prices.Add(GetPriceObject("CHANGEFEE", "Change fee", 0, (changeWaivedPriceValue == "Waived" ? "Waived" : "$0")));
                    }

                    if (changeWaivedPriceValue == "Waived")
                    {
                        availability.Reservation.Reshop.FeeWaiverMessage = (_configuration.GetValue<string>("ReshopChange-FEEWAIVEDMESSAGE") ?? "");

                        if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                        {
                            availability.Reservation.Reshop.FeeWaiverMessage = string.Empty;
                        }
                    }
                    else
                    {
                        availability.Reservation.Reshop.FeeWaiverMessage = string.Empty;
                    }
                }

                if (isNonResidualCredit)
                {
                    if (!_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                    {
                        string nonResidualMessageFromConfig = _configuration.GetValue<string>("Reshop_NonResidualCreditMessage");

                        availability.Reservation.Reshop.FeeWaiverMessage = availability.Reservation.Reshop.FeeWaiverMessage == string.Empty ?
                                                nonResidualMessageFromConfig :
                                                string.Format("{0}\n\n{1}", availability.Reservation.Reshop.FeeWaiverMessage, nonResidualMessageFromConfig);
                        availability.Reservation.Reshop.DisplayNonResidualCreditMessage = true;
                    }

                    var removeRefundPrice = availability.Reservation.Prices.SingleOrDefault(r => r.DisplayType == "REFUNDPRICE");
                    if (removeRefundPrice != null)
                        availability.Reservation.Prices.Remove(removeRefundPrice);
                }

                if (!isAddCollectExistInPrices)
                {
                    reservation.Prices.Add(GetPriceObject("ADDCOLLECT", "Total due", 0));
                }

                var totalPrice = reservation.Prices.First(p => p.DisplayType == "TOTAL");
                AssignValueToPriceAndDisplayFormat(totalWithChangeFee, totalPrice, false);

                var originalPrice = reservation.Prices.First(p => p.DisplayType == "ORIGINALTOTAL");
                AssignValueToPriceAndDisplayFormat(originalTotal, originalPrice, true);

                var newTOtal = reservation.Prices.First(p => p.DisplayType == "NEWTOTAL");
                reservation.Prices.Add(GetPriceObject("NEWBASETOTAL", newTOtal.PriceTypeDescription, newTOtal.Value));
                AssignValueToPriceAndDisplayFormat(newtaxtotal, newTOtal, false);

                if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                {
                    availability.Reservation.Reshop.RTIChangeCancelTxt = string.Empty;

                    if (isNonResidualCredit)
                    {
                        var removeoriginaltotal = availability.Reservation.Prices.SingleOrDefault(r => r.DisplayType == "ORIGINALTOTAL");
                        if (removeoriginaltotal != null)
                            availability.Reservation.Prices.Remove(removeoriginaltotal);

                        var updatenewtotal = availability.Reservation.Prices.SingleOrDefault(r => r.DisplayType == "NEWTOTAL");
                        if (updatenewtotal != null)
                        {
                            availability.Reservation.Reshop.RTIChangeCancelTxt
                                = string.Format(_configuration.GetValue<string>("ReshopRTIChangeCancelText"), updatenewtotal.FormattedDisplayValue);
                            updatenewtotal.FormattedDisplayValue = string.Empty;
                        }
                    }
                } //End of --EnableReshopChangeFeeElimination

            }
        }

        private void AddCorporateLeisureOptOutFSRAlert(ReservationInfo2 shopReservationInfo2, SelectTripRequest request, Session session)
        {
            var message = new MOBMobileCMSContentMessages();
            if (shopReservationInfo2 == null)
            {
                shopReservationInfo2 = new ReservationInfo2();
            }
            if (shopReservationInfo2.InfoWarningMessages == null)
            {
                shopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();
            }
            if (!shopReservationInfo2.InfoWarningMessages.Exists(x => x.Order == MOBINFOWARNINGMESSAGEORDER.RTICORPORATELEISUREOPTOUTMESSAGE.ToString()))
            {
                message = GetCMSContentMessageByKey("Shopping.CorporateDisclaimerMessage.MOB", request, session).Result;
                if (message != null)
                {
                    shopReservationInfo2.InfoWarningMessages.Add(new InfoWarningMessages
                    {
                        Order = MOBINFOWARNINGMESSAGEORDER.RTICORPORATELEISUREOPTOUTMESSAGE.ToString(),
                        IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString(),
                        Messages = new List<string>() { message.ContentFull },
                        ButtonLabel = message.ContentShort,
                        HeaderMessage = message.HeadLine,
                        IsCollapsable = true,
                        IsExpandByDefault = true
                    });
                }
                shopReservationInfo2.InfoWarningMessages = shopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
            }
        }
        private MOBFSRAlertMessage GetCorporateLeisureOptOutFSRAlert(MOBSHOPShopRequest shoprequest, Session session)
        {
            var message = new MOBMobileCMSContentMessages();
            MOBFSRAlertMessage alertMessage = null;
            message = GetCMSContentMessageByKey("Shopping.CorporateDisclaimerMessage.MOB", shoprequest, session).Result;
            if (message != null)
            {
                alertMessage = new MOBFSRAlertMessage
                {
                    bodyMsg = message.ContentFull,
                    headerMsg = message.HeadLine,
                    MessageTypeDescription = FSRAlertMessageType.CorporateLeisureOptOut,
                    MessageType = 0,
                    AlertType = MOBFSRAlertMessageType.Information.ToString(),
                    Buttons = new List<MOBFSRAlertMessageButton>()
                {
                     new MOBFSRAlertMessageButton
                     {
                         ButtonLabel =message.ContentShort,
                         UpdatedShopRequest = ConvertCorporateLeisureToRevenueRequest(shoprequest)
                     }
                }
                };
            }
            return alertMessage;
        }

        private MOBSHOPShopRequest ConvertCorporateLeisureToRevenueRequest(MOBSHOPShopRequest shopRequest)
        {
            if (shopRequest == null)
                return null;
            MOBSHOPShopRequest updatedShopRequest = shopRequest.Clone();
            updatedShopRequest.GetNonStopFlightsOnly = true;
            updatedShopRequest.GetFlightsWithStops = false;
            updatedShopRequest.SessionId = string.Empty;
            updatedShopRequest.mobCPCorporateDetails = null;
            updatedShopRequest.TravelType = string.Empty;
            return updatedShopRequest;
        }

        private List<MOBFSRAlertMessage> HandleFlightShoppingThatHasNoResults(United.Services.FlightShopping.Common.ShopResponse cslResponse, MOBSHOPShopRequest restShopRequest, bool isShop)
        {
            United.Services.FlightShopping.Common.ShopResponse cslResponseClone = cslResponse.CloneDeep();
            MOBSHOPShopRequest restShopRequestClone = restShopRequest.CloneDeep();

            var allEnhancements = new List<IRule<MOBFSRAlertMessage>>();
            if (_configuration.GetValue<bool>("FSRForceToGSTSwitch") && isShop)
            {
                allEnhancements.Add(new FSRForceToGSTbyOrigin(cslResponseClone, restShopRequestClone, _configuration));
            }


            if (_configuration.GetValue<bool>("EnableFSRAlertMessages_ForSeasonalMarket"))
            {
                allEnhancements.Add(new FSRSeasonalOrgAndDestSuggestFutureDate(cslResponseClone, restShopRequestClone, _configuration));
                allEnhancements.Add(new FSRSeasonalOriginSuggestFutureDate(cslResponseClone, restShopRequestClone, _configuration));
                allEnhancements.Add(new FSRSeasonalDestinationSuggestFutureDate(cslResponseClone, restShopRequestClone, _configuration));
            }

            if (!restShopRequest.AwardTravel || _configuration.GetValue<bool>("EnableFSRAlertMessages_Nearby_ForAwardBooking"))
            {
                allEnhancements.Add(new FSRNoResultSuggestNearbyAirports(cslResponseClone, restShopRequestClone, _configuration));
            }

            // Get the first enhancement based on given priorities
            var firstAlert = allEnhancements.FirstOrDefault(rule => rule.ShouldExecuteRule());

            if (_shoppingUtility.CheckFSRRedesignFromShopRequest(restShopRequest))
            {
                if (firstAlert != null)
                {
                    MOBFSRAlertMessage listFsrAlert = new MOBFSRAlertMessage();
                    listFsrAlert = firstAlert.Execute();

                    listFsrAlert.AlertType = MOBFSRAlertMessageType.Information.ToString();
                    return new List<MOBFSRAlertMessage>() { listFsrAlert };
                }
                else
                {
                    return null;
                }
            }
            else
                return firstAlert != null ? new List<MOBFSRAlertMessage> { firstAlert.Execute() } : null;
        }
        private void AddAvailabilityToPersist(MOBSHOPAvailability availability, string sessionID, bool isCallFromShop = false)
        {
            #region Organize Resulst Shop Filters
            LatestShopAvailabilityResponse persistAvailability = new LatestShopAvailabilityResponse();
            persistAvailability = _sessionHelperService.GetSession<LatestShopAvailabilityResponse>(Headers.ContextValues, persistAvailability.ObjectName).Result;
            //persistAvailability = United.Persist.FilePersist.Load<LatestShopAvailabilityResponse>(sessionID, persistAvailability.ObjectName, false);
            if (persistAvailability != null && persistAvailability.AvailabilityList != null)
            {
                bool isexist = false;
                foreach (string key in persistAvailability.AvailabilityKeys)
                {
                    if (key == availability.Trip.LastTripIndexRequested.ToString())
                    {
                        var persistAvailabilityCopy = persistAvailability.AvailabilityList[availability.Trip.LastTripIndexRequested.ToString()];
                        persistAvailability.AvailabilityList[availability.Trip.LastTripIndexRequested.ToString()] = availability;
                        MOBSHOPFlattenedFlightList mobSHOPFlattenedFlightList = new MOBSHOPFlattenedFlightList();
                        if (isCallFromShop && _configuration.GetValue<bool>("EnableNonStopFlight") && persistAvailabilityCopy != null)
                        {
                            mobSHOPFlattenedFlightList.FlattenedFlightList = persistAvailability.AvailabilityList[availability.Trip.LastTripIndexRequested.ToString()].Trip.FlattenedFlights.Clone();
                            foreach (var previousFlattendeFlight in persistAvailabilityCopy.Trip.FlattenedFlights)
                            {
                                if (!mobSHOPFlattenedFlightList.FlattenedFlightList.Exists(p => p.FlightId == previousFlattendeFlight.FlightId))
                                {
                                    mobSHOPFlattenedFlightList.FlattenedFlightList.AddRange(persistAvailabilityCopy.Trip.FlattenedFlights);
                                }
                            }
                            persistAvailability.AvailabilityList[availability.Trip.LastTripIndexRequested.ToString()].Trip.FlightCount = mobSHOPFlattenedFlightList.FlattenedFlightList.Count;
                        }
                        _sessionHelperService.SaveSession<MOBSHOPFlattenedFlightList>(mobSHOPFlattenedFlightList, Headers.ContextValues, mobSHOPFlattenedFlightList.ObjectName);
                        //United.Persist.FilePersist.Save<MOBSHOPFlattenedFlightList>(sessionID, (mobSHOPFlattenedFlightList).GetType().FullName, mobSHOPFlattenedFlightList);
                        isexist = true;
                    }
                }
                if (!isexist)
                {
                    persistAvailability.AvailabilityList.Add(availability.Trip.LastTripIndexRequested.ToString(), availability);
                    persistAvailability.AvailabilityKeys.Add(availability.Trip.LastTripIndexRequested.ToString());
                }
            }
            else
            {
                persistAvailability = new LatestShopAvailabilityResponse();
                persistAvailability.SessionId = sessionID;
                persistAvailability.CartId = availability.CartId;
                if (persistAvailability.AvailabilityList == null)
                {
                    persistAvailability.AvailabilityList = new SerializableDictionary<string, MOBSHOPAvailability>();
                    persistAvailability.AvailabilityKeys = new List<string>();
                }
                if (availability.Trip != null)
                {
                    persistAvailability.AvailabilityList.Add(availability.Trip.LastTripIndexRequested.ToString(), availability);
                    persistAvailability.AvailabilityKeys.Add(availability.Trip.LastTripIndexRequested.ToString());
                }
            }

            //United.Persist.FilePersist.Save<United.Persist.Definition.Shopping.LatestShopAvailabilityResponse>(sessionID, persistAvailability.ObjectName, persistAvailability);
            #endregion
        }
        private string GetNewFSRFareDescriptionMessage(string searchType)
        {
            string FSRDescription = _configuration.GetValue<string>("FSRRedesignFareDescription");
            switch (searchType)
            {
                case "OW":
                    FSRDescription = FSRDescription.Split('|')[2];
                    break;
                case "RT":
                    FSRDescription = FSRDescription.Split('|')[0];
                    break;
                case "MD":
                    FSRDescription = FSRDescription.Split('|')[1];
                    break;
                default:
                    FSRDescription = string.Empty;
                    break;
            }

            if (!string.IsNullOrEmpty(FSRDescription))
            {
                var enableNewBaggageTextOnFSRShop = _configuration.GetValue<string>("EnableNewBaggageTextOnFSRShop");
                FSRDescription = $"{FSRDescription}{enableNewBaggageTextOnFSRShop}";
            }

            return FSRDescription;
        }
        private bool CheckPreviousSelectedTripDepartDate(string calendarDate, SelectTrip selectTrip)
        {
            string departDate = string.Empty;
            if (selectTrip.Responses.Count > 0 && selectTrip.Responses[selectTrip.Responses.Keys.ElementAt(selectTrip.Responses.Count - 1)] != null)
            {
                var response = selectTrip.Responses[selectTrip.Responses.Keys.ElementAt(selectTrip.Responses.Count - 1)];
                if (response.Availability != null && response.Availability.Trip != null)
                {
                    var trip = response.Availability.Trip;
                    departDate = trip.DepartDate;
                }
            }
            return (!string.IsNullOrEmpty(departDate) && DateTime.Parse(calendarDate) < DateTime.Parse(departDate));
        }
        #endregion
        private void SetFSRFareDescriptionForShop(MOBSHOPAvailability availability, MOBSHOPShopRequest request)
        {
            availability.fSRFareDescription = GetFSRFareDescription(request);
            // MOBILE-14512
            if (_configuration.GetValue<bool>("EnableSortFilterEnhancements") &&
                _shoppingUtility.IsSortDisclaimerForNewFSR(request.Application.Id, request.Application.Version.Major))
            {
                availability.SortDisclaimerText = GetTextForSortDisclaimer(false);
            }
        }
        private string GetTextForSortDisclaimer(bool forAppend)
        {
            return $"{(forAppend ? "\n" : "")}{_configuration.GetValue<string>("AdditionalLegalDisclaimerText")}";
        }
        private void SetSortDisclaimerForReshop(MOBSHOPAvailability availability, MOBSHOPShopRequest request)
        {
            if (!availability.AwardTravel && _configuration.GetValue<bool>("EnableSortFilterEnhancements"))
            {
                // MOBILE-14512
                if (_shoppingUtility.IsSortDisclaimerForNewFSR(request.Application.Id, request.Application.Version.Major))
                {
                    availability.SortDisclaimerText = GetTextForSortDisclaimer(false);
                }
                else
                {
                    var forAppend = !string.IsNullOrEmpty(availability.CorporateDisclaimer);
                    availability.CorporateDisclaimer = $"{availability.CorporateDisclaimer}{GetTextForSortDisclaimer(forAppend)}";

                }
            }
        }
        public static bool IsStandardRevenueSearch(bool isCorporateBooking, bool isYoungAdultBooking, bool isAwardTravel,
                                                  string employeeDiscountId, string travelType, bool isReshop, string fareClass,
                                                  string promotionCode)
        {
            return !(isCorporateBooking || travelType == TravelType.CLB.ToString() || isYoungAdultBooking ||
                     isAwardTravel || !string.IsNullOrEmpty(employeeDiscountId) || isReshop ||
                     travelType == TravelType.TPSearch.ToString() || !string.IsNullOrEmpty(fareClass) ||
                     !string.IsNullOrEmpty(promotionCode));
        }
        public List<MOBSHOPPrice> GetPrices(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices, bool isAwardBooking,
                string sessionId, bool isReshopChange = false, int appId = 0, string appVersion = "", List<MOBItem> catalogItems = null, 
                FlightReservationResponse shopBookingDetailsResponse = null)
        {
            List<MOBSHOPPrice> bookingPrices = new List<MOBSHOPPrice>();
            CultureInfo ci = null;
            foreach (var price in prices)
            {
                if (ci == null)
                {
                    ci = TopHelper.GetCultureInfo(price.Currency);
                }

                MOBSHOPPrice bookingPrice = new MOBSHOPPrice();
                bookingPrice.CurrencyCode = price.Currency;
                bookingPrice.DisplayType = price.Type;
                bookingPrice.Status = price.Status;
                bookingPrice.Waived = price.Waived;
                bookingPrice.DisplayValue = string.Format("{0:#,0.00}", price.Amount);

                if (!isReshopChange)
                {
                    if (!string.IsNullOrEmpty(bookingPrice.DisplayType) && bookingPrice.DisplayType.Equals("MILES") && isAwardBooking && !string.IsNullOrEmpty(sessionId))
                    {
                        ValidateAwardMileageBalance(sessionId, price.Amount, appId, appVersion);
                    }
                }

                double tempDouble = 0;
                double.TryParse(price.Amount.ToString(), out tempDouble);
                bookingPrice.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);

                if (price.Currency.ToUpper() == "MIL")
                {
                    bookingPrice.FormattedDisplayValue = _manageResUtility.formatAwardAmountForDisplay(price.Amount.ToString(), false);
                }
                else
                {
                    if (price.Amount < 0
                        && (string.Equals("TaxDifference", price.Type, StringComparison.OrdinalIgnoreCase)
                        || string.Equals("FareDifference", price.Type, StringComparison.OrdinalIgnoreCase)))
                        bookingPrice.FormattedDisplayValue = formatAmountForDisplay((price.Amount * -1).ToString(), ci, false);
                    else
                        bookingPrice.FormattedDisplayValue = formatAmountForDisplay(price.Amount.ToString(), ci, false);
                }

                bookingPrice.PaxTypeDescription = $"{price?.Count} {price?.Description}".ToLower();
                _shoppingBuyMiles.UpdatePriceTypeDescForBuyMiles(appId, appVersion, catalogItems, shopBookingDetailsResponse, bookingPrice);
                bookingPrices.Add(bookingPrice);
            }

            return bookingPrices;
        }


        private string formatAmountForDisplay(string amt, CultureInfo ci, /*string currency,*/ bool roundup = true, bool isAward = false)
        {
            string newAmt = amt;
            decimal amount = 0;
            decimal.TryParse(amt, out amount);

            try
            {

                string currencySymbol = "";

                RegionInfo ri = new RegionInfo(ci.Name);

                switch (ri.ISOCurrencySymbol.ToUpper())
                {
                    case "JPY":
                    case "EUR":
                    case "CAD":
                    case "GBP":
                    case "CNY":
                    case "USD":
                    case "AUD":
                    default:
                        //currencySymbol = GetCurrencySymbol(currency.ToUpper());
                        //newAmt = currencySymbol + newAmt;
                        newAmt = GetCurrencySymbol(ci, amount, roundup);
                        break;
                }

            }
            catch { }

            return isAward ? "+ " + newAmt : newAmt;
        }

        private string GetCurrencySymbol(CultureInfo ci, /*string currencyCode,*/ decimal amount, bool roundup)
        {
            string result = string.Empty;

            try
            {
                if (amount > -1)
                {
                    if (roundup)
                    {
                        int newTempAmt = (int)decimal.Ceiling(amount);
                        try
                        {
                            var ri = new RegionInfo(ci.Name);
                            CultureInfo tempCi = Thread.CurrentThread.CurrentCulture;
                            Thread.CurrentThread.CurrentCulture = ci;
                            result = newTempAmt.ToString("c0");
                            Thread.CurrentThread.CurrentCulture = tempCi;
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            var ri = new RegionInfo(ci.Name);
                            CultureInfo tempCi = Thread.CurrentThread.CurrentCulture;
                            Thread.CurrentThread.CurrentCulture = ci;
                            result = amount.ToString("c");
                            Thread.CurrentThread.CurrentCulture = tempCi;
                        }
                        catch { }
                    }
                }

            }
            catch { }

            return result;
        }

        private void AddFreeBagDetailsInPrices(DisplayCart displayCart, List<MOBSHOPPrice> prices)
        {
            if (isAFSCouponApplied(displayCart))
            {
                if (displayCart.SpecialPricingInfo.MerchOfferCoupon.Product.ToUpper().Equals("BAG"))
                {
                    prices.Add(new MOBSHOPPrice
                    {
                        PriceTypeDescription = _configuration.GetValue<string>("FreeBagCouponDescription"),
                        DisplayType = "TRAVELERPRICE",
                        FormattedDisplayValue = "",
                        DisplayValue = "",
                        Value = 0
                    });
                }
            }
        }

        private bool isAFSCouponApplied(DisplayCart displayCart)
        {
            if (displayCart != null && displayCart.SpecialPricingInfo != null && displayCart.SpecialPricingInfo.MerchOfferCoupon != null && !string.IsNullOrEmpty(displayCart.SpecialPricingInfo.MerchOfferCoupon.PromoCode) && displayCart.SpecialPricingInfo.MerchOfferCoupon.IsCouponEligible.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }
    }
}

