using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.MerchandizeService;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.DataAccess.Travelers;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.SSR;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Model.Travelers;
using United.Service.Presentation.ProductModel;
using United.Service.Presentation.ProductResponseModel;
using United.Services.Customer.Common;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Services.FlightShopping.Common.LMX;
using United.Utility.Enum;
using United.Utility.Helper;
using ProfileResponse = United.Mobile.Model.Shopping.ProfileResponse;
using RegisterOfferRequest = United.Mobile.Model.Shopping.FormofPayment.RegisterOfferRequest;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using TravelOption = United.Mobile.Model.Shopping.TravelOption;

namespace United.Common.Helper.Traveler
{
    public class Traveler : ITraveler
    {
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly ICachingService _cachingService;
        private readonly ICMSContentService _cMSContentService;
        private readonly ICacheLog<Traveler> _logger;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly ITravelerUtility _travelerUtility;
        private readonly ICustomerProfile _customerProfile;
        private readonly ILMXInfo _lmxInfo;
        private readonly IDPService _dPService;
        private readonly IPurchaseMerchandizingService _purchaseMerchandizingService;
        private readonly ITravelReadyService _travelReadyService;
        private readonly ICustomerDataService _customerDataService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IPlacePassService _placePassService;
        private readonly IProductInfoHelper _productInfoHelper;
        private readonly IShoppingBuyMiles _shoppingBuyMiles;
        private readonly IMobileShoppingCart _mobileShoppingCart;
        private readonly IHeaders _headers;
        private readonly IFeatureSettings _featureSettings;
        private readonly IFeatureToggles _featureToggles;
        public Traveler(ICacheLog<Traveler> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , ICachingService cachingService
            , ICMSContentService cMSContentService
            , IShoppingUtility shoppingUtility
            , ITravelerUtility travelerUtility
            , ICustomerProfile customerProfile
            , ILMXInfo lmxInfo
            , IDPService dPService
            , IPurchaseMerchandizingService purchaseMerchandizingService
            , ITravelReadyService travelReadyService
            , ICustomerDataService customerDataService
            , IShoppingCartService shoppingCartService
            , IPlacePassService placePassService
            , IProductInfoHelper productInfoHelper
            , IMobileShoppingCart mobileShoppingCart
            , IShoppingBuyMiles shoppingBuyMiles
            , IHeaders headers
            , IFeatureSettings featureSettings
            , IFeatureToggles featureToggles)
        {
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _cachingService = cachingService;
            _cMSContentService = cMSContentService;
            _logger = logger;
            _shoppingUtility = shoppingUtility;
            _travelerUtility = travelerUtility;
            _customerProfile = customerProfile;
            _lmxInfo = lmxInfo;
            _dPService = dPService;
            _purchaseMerchandizingService = purchaseMerchandizingService;
            _travelReadyService = travelReadyService;
            _customerDataService = customerDataService;
            _shoppingCartService = shoppingCartService;
            _placePassService = placePassService;
            _productInfoHelper = productInfoHelper;
            _mobileShoppingCart = mobileShoppingCart;
            _shoppingBuyMiles = shoppingBuyMiles;
            _headers = headers;
            _featureSettings = featureSettings;
            _featureToggles = featureToggles;
        }

        public async Task<MOBSHOPReservation> RegisterTravelers_CFOP(MOBRegisterTravelersRequest request, bool isRegisterOffersCall = false)
        {
            MOBSHOPReservation reservation = new MOBSHOPReservation(_configuration, _cachingService);
            #region
            if (request == null)
            {
                throw new MOBUnitedException("Register Travelers request cannot be null.");
            }
            if (request.ProfileOwner == null)
            {
                request = await GetPopulateProfileOwnerData(request);
            }

            Reservation bookingPathReservation = new Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

            // Traveler selector validation
            if (_configuration.GetValue<bool>("EnableTravelerSelectorCountValidation") && (await _featureSettings.GetFeatureSettingValue("EnableTravelerSelectorCountValidation").ConfigureAwait(false)))
            {
                if (bookingPathReservation?.NumberOfTravelers > 0 && request?.Travelers?.Count > 0)
                {
                    if (bookingPathReservation.NumberOfTravelers != request.Travelers.Count)
                    {
                        throw new MOBUnitedException((bookingPathReservation.NumberOfTravelers > request.Travelers.Count) ? _configuration.GetValue<string>("FewTravelersErrorMessage") : _configuration.GetValue<string>("ManyTravelersErrorMessage"));
                    }
                }
            }
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string> { request.SessionId, session.ObjectName }).ConfigureAwait(false);
            if (await _featureToggles.IsEnableWheelchairFilterOnFSR(request.Application.Id, request.Application.Version.Major, session.CatalogItems).ConfigureAwait(false))
            {
                try
                {
                    _shoppingUtility.AssignWheelChairSpecialNeedForSinglePax(request.Travelers, bookingPathReservation);
                }
                catch (Exception ex)
                {
                    _logger.LogError("WheelChairSSR-RegisterTravelers tieing to singlepax error {Exception} and sessionId {sessionid} ", JsonConvert.SerializeObject(ex), request.SessionId);
                }
            }
            string nextViewName = _productInfoHelper.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, bookingPathReservation?.ShopReservationInfo2?.IsDisplayCart == true) ? GetNextViewName(request, bookingPathReservation) : "";
            bool isRequireNationalityAndResidence = false;
            if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence != null)
            {
                isRequireNationalityAndResidence = bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence;
            }

            if (_travelerUtility.EnableServicePlacePassBooking(request.Application.Id, request.Application.Version.Major) && bookingPathReservation.ShopReservationInfo2 != null)
            {
                string destinationcode = bookingPathReservation.Trips[0].FlattenedFlights[0].Flights.Select(p => p.Destination).LastOrDefault();
                string destinationcode1 = bookingPathReservation.Trips[0].Destination.ToString();
                string placepasscampain = "utm_Campaign=Confirmation_Mobile";
                await System.Threading.Tasks.Task.Factory.StartNew(async () =>
                {
                    bookingPathReservation.ShopReservationInfo2.PlacePass = await GetPlacePass(destinationcode, bookingPathReservation.SearchType, request.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, "RegisterTravelerGetPlacePass", placepasscampain);
                });

            }

            if (_travelerUtility.EnablePlacePassBooking(request.Application.Id, request.Application.Version.Major)
                && bookingPathReservation.ShopReservationInfo2 != null)
            {
                string destinationcode = bookingPathReservation.Trips[0].Destination.ToString();
                bookingPathReservation.ShopReservationInfo2.PlacePass = await _travelerUtility.GetEligiblityPlacePass(destinationcode, bookingPathReservation.SearchType, request.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, "RegisterTravelerGetPlacePass");
            }
            if (_shoppingUtility.EnableIBEFull())
            {
                _travelerUtility.RemoveelfMessagesForRTI(ref bookingPathReservation);
            }

            var tupleResponse = await GetRegisterTravelerRequest(request, isRequireNationalityAndResidence, bookingPathReservation);
            RegisterTravelersRequest registerTravelerRequest = tupleResponse.Item1;
            request = tupleResponse.request;

            string jsonRequest = JsonConvert.SerializeObject(registerTravelerRequest);

            var response = await _shoppingCartService.GetRegisterTravelers<FlightReservationResponse>(request.Token, request.SessionId, jsonRequest).ConfigureAwait(false);

            if (_configuration.GetValue<string>("ForTestingGetttingXMLGetProfileTime") != null && _configuration.GetValue<bool>("ForTestingGetttingXMLGetProfileTime"))
            {
                // logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, url, "CSS/CSL-CallDuration", request.Application.Id, request.Application.Version.Major, request.DeviceId + "_" + request.MileagePlusNumber, "CSLRegisterTraveler=" + cslCallTime));
            }
            if (response != null)
            {
                // FlightReservationResponse response = null;

                if (response != null && (response.Status == Services.FlightShopping.Common.StatusType.Success) && response.Reservation != null)
                {
                    reservation.TravelersRegistered = true;
                    bookingPathReservation.TravelersRegistered = true;
                    //##Kirti ALM 23973 - Booking 2.1 - REST - LMX - use new CSL Profile Service, GetSavedTraverlerMplist from CSL 
                    request.Travelers = await GetMPDetailsForSavedTravelers(request);

                    if (_shoppingUtility.IsETCEnabledforMultiTraveler(request.Application.Id, request.Application.Version.Major.ToString()))
                    {
                        AssignTravelerIndividualTotalAmount(request.Travelers, response.DisplayCart.DisplayPrices, response.Reservation?.Travelers.ToList(), response.Reservation?.Prices?.ToList());
                    }
                    AssignFFCsToUnChangedTravelers(request.Travelers, bookingPathReservation.TravelersCSL, request.Application, request.ContinueToChangeTravelers);

                    if ((_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange) && bookingPathReservation.ShopReservationInfo2 != null &&
                        bookingPathReservation.ShopReservationInfo2.TravelerTypes != null && bookingPathReservation.ShopReservationInfo2.TravelerTypes.Count > 0) || IsArranger(bookingPathReservation))
                    {
                        string DeptDateOfFLOF = bookingPathReservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTime;
                        //making this flag true if want to remove traveler from list in seatmap.

                        foreach (var t in request.Travelers)
                        {
                            if (!string.IsNullOrEmpty(t.TravelerTypeCode) && t.TravelerTypeCode.ToUpper().Equals("INF") && !string.IsNullOrEmpty(t.BirthDate) && TopHelper.GetAgeByDOB(t.BirthDate, DeptDateOfFLOF) < 2)
                            {
                                t.IsEligibleForSeatSelection = false;
                            }
                            else
                            {
                                t.IsEligibleForSeatSelection = true;
                            }
                        }
                    }

                    reservation.TravelersCSL = request.Travelers;
                    #region Define Booking Path Persist Reservation and Save to session
                    bookingPathReservation.TravelersCSL = new SerializableDictionary<string, MOBCPTraveler>();
                    bookingPathReservation.TravelerKeys = new List<string>();

                    //added by wade to get details LMX info
                    try
                    {
                        #region //**// LMX Flag For AppID change
                        bool supressLMX = false;
                        supressLMX = session.SupressLMXForAppID;
                        #endregion
                        ShoppingResponse shop = new ShoppingResponse();
                        shop = await _sessionHelperService.GetSession<ShoppingResponse>(request.SessionId, shop.ObjectName, new List<string> { request.SessionId, shop.ObjectName }).ConfigureAwait(false);
                        bookingPathReservation.LMXFlights = null; // need to default to null to remove LMX from reservation if service call fails.

                        if (!supressLMX && shop != null && shop.Request.ShowMileageDetails)
                            bookingPathReservation.LMXFlights = await GetLmxForRTI(request.Token, request.CartId);
                    }
                    catch { }

                    string mpNumbers = string.Empty;
                    foreach (var traveler in request.Travelers)
                    {
                        if (_shoppingUtility.IsEnabledNationalityAndResidence(bookingPathReservation.IsReshopChange, request.Application.Id, request.Application.Version.Major))
                        {
                            if (bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence != null &&
                                bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence)
                            {
                                if (string.IsNullOrEmpty(traveler.CountryOfResidence) || string.IsNullOrEmpty(traveler.Nationality))
                                {
                                    traveler.Message = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage") as string;
                                }
                            }
                        }
                        #region
                        if (bookingPathReservation.TravelersCSL.ContainsKey(traveler.PaxIndex.ToString()))
                        {
                            bookingPathReservation.TravelersCSL[traveler.PaxIndex.ToString()] = traveler;
                        }
                        else
                        {
                            bookingPathReservation.TravelersCSL.Add(traveler.PaxIndex.ToString(), traveler);
                            bookingPathReservation.TravelerKeys.Add(traveler.PaxIndex.ToString());
                        }
                        #endregion
                        #region Get Multiple Saved Travelers MP Name Miss Match
                        if (traveler.isMPNameMisMatch)
                        {
                            MOBBKLoyaltyProgramProfile frequentFlyerProgram = traveler.AirRewardPrograms.Find(itm => itm.CarrierCode.ToUpper().Trim() == "UA");
                            if (frequentFlyerProgram != null)
                            {
                                mpNumbers = mpNumbers + "," + frequentFlyerProgram.MemberId;
                            }
                        }
                        #endregion

                    }
                    #region Get Multiple Saved Travelers MP Name Miss Match messages
                    if (!string.IsNullOrEmpty(mpNumbers))
                    {
                        #region
                        string savedTravelerMPNameMismatch = _configuration.GetValue<string>("SavedTravelerMPNameMismatch");
                        MOBItem item = new MOBItem();
                        mpNumbers = mpNumbers.Trim(',').ToUpper().Trim();
                        if (mpNumbers.Split(',').Length > 1)
                        {
                            string firstMP = mpNumbers.Split(',')[0].ToString();
                            mpNumbers = mpNumbers.Replace(firstMP, "") + " and " + firstMP;
                            mpNumbers = mpNumbers.Trim(',');
                            item.CurrentValue = string.Format(savedTravelerMPNameMismatch, "accounts", mpNumbers, "travelers");
                        }
                        else
                        {
                            item.CurrentValue = string.Format(savedTravelerMPNameMismatch, "account", mpNumbers, "this traveler");
                        }
                        item.Id = "SavedTravelerMPNameMismatch";
                        item.SaveToPersist = true;
                        if (bookingPathReservation.TCDAdvisoryMessages != null && bookingPathReservation.TCDAdvisoryMessages.Count >= 1 && bookingPathReservation.TCDAdvisoryMessages.FindIndex(itm => itm.Id.ToUpper().Trim() == "SavedTravelerMPNameMismatch".ToUpper().Trim()) >= 0)
                        {
                            bookingPathReservation.TCDAdvisoryMessages.Find(itm => itm.Id.ToUpper().Trim() == "SavedTravelerMPNameMismatch".ToUpper().Trim()).CurrentValue = item.CurrentValue;
                        }
                        else
                        {
                            bookingPathReservation.TCDAdvisoryMessages = new List<MOBItem>();
                            bookingPathReservation.TCDAdvisoryMessages.Add(item);
                        }
                        #endregion
                    }
                    #endregion
                    if (bookingPathReservation.IsSignedInWithMP)
                    {
                        List<MOBAddress> creditCardAddresses = new List<MOBAddress>();
                        MOBCPPhone mpPhone = new MOBCPPhone();
                        MOBEmail mpEmail = new MOBEmail();
                        if (bookingPathReservation.CreditCards == null || bookingPathReservation.CreditCards.Count == 0)
                        {

                            var tupleRes = await GetProfileOwnerCreditCardList(request.SessionId, creditCardAddresses, mpPhone, mpEmail, string.Empty);
                            bookingPathReservation.CreditCards = tupleRes.savedProfileOwnerCCList;
                            creditCardAddresses = tupleRes.creditCardAddresses;
                            mpPhone = tupleRes.mpPhone;
                            mpEmail = tupleRes.mpEmail;
                            bookingPathReservation.CreditCardsAddress = creditCardAddresses;
                            bookingPathReservation.ReservationPhone = mpPhone;
                            bookingPathReservation.ReservationEmail = mpEmail;
                        }

                        reservation.ReservationPhone = mpPhone;
                        reservation.ReservationEmail = mpEmail;
                        reservation.CreditCards = bookingPathReservation.CreditCards;
                        reservation.CreditCardsAddress = bookingPathReservation.CreditCardsAddress;
                    }

                    //bookingPathReservation.CSLReservationJSONFormat 
                    //    = United.Json.Serialization.JsonSerializer.Serialize<United.Service.Presentation.ReservationModel.Reservation>(response.Reservation);
                    //United.Service.Presentation.ReservationModel.Reservation cslReservation = JsonSerializer.DeserializeUseContract<United.Service.Presentation.ReservationModel.Reservation>(session.CSLReservation);

                    reservation.PointOfSale = bookingPathReservation.PointOfSale;
                    reservation.Trips = bookingPathReservation.Trips;
                    reservation.Prices = bookingPathReservation.Prices;
                    reservation.Taxes = bookingPathReservation.Taxes;
                    reservation.NumberOfTravelers = bookingPathReservation.NumberOfTravelers;
                    reservation.IsSignedInWithMP = bookingPathReservation.IsSignedInWithMP;
                    reservation.CartId = bookingPathReservation.CartId;
                    reservation.SearchType = bookingPathReservation.SearchType;
                    reservation.TravelOptions = bookingPathReservation.TravelOptions;
                    reservation.LMXFlights = bookingPathReservation.LMXFlights;
                    reservation.lmxtravelers = GetLMXTravelersFromFlights(reservation);
                    reservation.IneligibleToEarnCreditMessage = bookingPathReservation.IneligibleToEarnCreditMessage;
                    reservation.OaIneligibleToEarnCreditMessage = bookingPathReservation.OaIneligibleToEarnCreditMessage;
                    if (bookingPathReservation.IsCubaTravel)
                    {
                        reservation.IsCubaTravel = bookingPathReservation.IsCubaTravel;
                        reservation.CubaTravelInfo = bookingPathReservation.CubaTravelInfo;
                    }
                    reservation.FormOfPaymentType = bookingPathReservation.FormOfPaymentType;
                    if ((bookingPathReservation.FormOfPaymentType == MOBFormofPayment.PayPal || bookingPathReservation.FormOfPaymentType == MOBFormofPayment.PayPalCredit) && bookingPathReservation.PayPal != null)
                    {
                        reservation.PayPal = bookingPathReservation.PayPal;
                        reservation.PayPalPayor = bookingPathReservation.PayPalPayor;
                    }
                    if (bookingPathReservation.FormOfPaymentType == MOBFormofPayment.Masterpass)
                    {
                        if (bookingPathReservation.MasterpassSessionDetails != null)
                            reservation.MasterpassSessionDetails = bookingPathReservation.MasterpassSessionDetails;
                        if (bookingPathReservation.Masterpass != null)
                            reservation.Masterpass = bookingPathReservation.Masterpass;
                    }
                    if (bookingPathReservation.IsReshopChange)
                    {
                        reservation.ReshopTrips = bookingPathReservation.ReshopTrips;
                        reservation.Reshop = bookingPathReservation.Reshop;
                        reservation.IsReshopChange = true;
                    }
                    if (reservation.IsCubaTravel)
                    {
                        _travelerUtility.ValidateTravelersCSLForCubaReason(reservation);
                    }
                    bool enableUKtax = _travelerUtility.IsEnableUKChildrenTaxReprice(bookingPathReservation.IsReshopChange, request.Application.Id, request.Application.Version.Major);
                    bool enableNat = IsNatAndResEnabled(request, bookingPathReservation);
                    bool enableTravelerTypes = GetEnableTravelerTypes(request, bookingPathReservation);
                    bool enableEplus = _travelerUtility.EnableEPlusAncillary(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange);

                    //Bug 241287 : added below null check, as it is failing in preprod - 7th Feb 2018 j.srinivas
                    if (enableNat || enableUKtax || enableTravelerTypes || enableEplus)
                    {
                        if (response.DisplayCart.PricingChange || (enableTravelerTypes && !comapreTtypesList(bookingPathReservation, response.DisplayCart))
                            || (await _featureSettings.GetFeatureSettingValue("EnableEPlusNotRemovedFix").ConfigureAwait(false) && enableEplus && bookingPathReservation.TravelOptions != null && bookingPathReservation.TravelOptions.Any(t => t?.Key.Trim().ToUpper() == "EFS")))
                        {
                            bookingPathReservation.Prices.Clear();
                            if (enableUKtax)
                            {
                                bookingPathReservation.Prices = _shoppingUtility.GetPrices(response.DisplayCart.DisplayPrices, bookingPathReservation.AwardTravel, bookingPathReservation.SessionId,
                                    bookingPathReservation.IsReshopChange, bookingPathReservation.SearchType, appId: request.Application.Id, appVersion: request.Application.Version.Major,
                                    isNotSelectTripCall: true, shopBookingDetailsResponse: response, isRegisterOffersFlow: isRegisterOffersCall, session: session);
                            }
                            else
                            {
                                bookingPathReservation.Prices = GetPricesAfterPriceChange(response.DisplayCart.DisplayPrices, bookingPathReservation.AwardTravel, bookingPathReservation.SessionId, bookingPathReservation.IsReshopChange);
                            }

                            if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                            {
                                bookingPathReservation.Prices.AddRange(await GetPrices(response.DisplayCart.DisplayFees, false, string.Empty));

                            }
                            //need to add close in fee to TOTAL
                            bookingPathReservation.Prices = AdjustTotal(bookingPathReservation.Prices);
                            if (enableUKtax)
                            {
                                bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = _shoppingUtility.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices, bookingPathReservation.IsReshopChange, appId: request.Application.Id, appVersion: request.Application.Version.Major, travelType: session.TravelType);
                                if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                                {
                                    bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes.Add(ShopStaticUtility.AddFeesAfterPriceChange(response.DisplayCart.DisplayFees));
                                }
                            }
                            else
                            {
                                bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices, bookingPathReservation.NumberOfTravelers, bookingPathReservation.IsReshopChange, appId: request.Application.Id, appVersion: request.Application.Version.Major, travelType: session.TravelType);
                                if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                                {
                                    bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes.Add(ShopStaticUtility.AddFeesAfterPriceChange(response.DisplayCart.DisplayFees));
                                }
                            }

                            if (reservation.ShopReservationInfo2 == null)
                                reservation.ShopReservationInfo2 = new ReservationInfo2();

                            if (reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                                reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();

                            reservation.Prices = bookingPathReservation.Prices;
                            reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes;
                            if (enableEplus && response.DisplayCart.ChangeOfferPriceMessages != null && response.DisplayCart.ChangeOfferPriceMessages.Count > 0)
                            {
                                bookingPathReservation.TravelOptions = _travelerUtility.GetTravelOptions(response.DisplayCart, bookingPathReservation.IsReshopChange, request.Application.Id, request.Application.Version.Major);
                                reservation.TravelOptions = bookingPathReservation.TravelOptions;
                            }

                            if (enableEplus && _configuration.GetValue<bool>("EnableEplusCodeRefactor"))
                            {
                                bookingPathReservation.Prices = _shoppingUtility.UpdatePricesForEFS(reservation, request.Application.Id, request.Application.Version.Major, session.IsReshopChange);
                                reservation.Prices = bookingPathReservation.Prices;
                            }
                            if (response.DisplayCart.PricingChange)
                            {
                                if (bookingPathReservation.ShopReservationInfo2.InfoWarningMessages == null)
                                    bookingPathReservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                                if (!bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.PRICECHANGE.ToString()))
                                {
                                    bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Add(_travelerUtility.GetPriceChangeMessage());
                                    bookingPathReservation.ShopReservationInfo2.InfoWarningMessages = bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)System.Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                                }

                                if (bookingPathReservation.ShopReservationInfo2.IsUnfinihedBookingPath)
                                {
                                    United.Services.FlightShopping.Common.ShopRequest persistedShopPindownRequest = await _sessionHelperService.GetSession<United.Services.FlightShopping.Common.ShopRequest>(_headers.ContextValues.SessionId, typeof(United.Services.FlightShopping.Common.ShopRequest).FullName, new List<string> { _headers.ContextValues.SessionId, typeof(United.Services.FlightShopping.Common.ShopRequest).FullName }).ConfigureAwait(false);
                                    int i = 0;
                                    if (persistedShopPindownRequest != null && persistedShopPindownRequest.PaxInfoList != null && response.Reservation.Travelers.Count == persistedShopPindownRequest.PaxInfoList.Count)
                                    {
                                        foreach (var traveler in response.Reservation.Travelers)
                                        {
                                            if (traveler.Person.Nationality != null && traveler.Person.Nationality.Count > 0)
                                            {
                                                persistedShopPindownRequest.PaxInfoList[i].Nationality = traveler.Person.Nationality[0].CountryCode;
                                                persistedShopPindownRequest.PaxInfoList[i].DateOfBirth = traveler.Person.DateOfBirth;
                                            }

                                            if (traveler.Person.CountryOfResidence != null)
                                                persistedShopPindownRequest.PaxInfoList[i].Residency = traveler.Person.CountryOfResidence.CountryCode;

                                            i++;
                                        }

                                        await _sessionHelperService.SaveSession<United.Services.FlightShopping.Common.ShopRequest>(persistedShopPindownRequest, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, typeof(United.Services.FlightShopping.Common.ShopRequest).FullName }, typeof(United.Services.FlightShopping.Common.ShopRequest).FullName).ConfigureAwait(false);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.InfoWarningMessages != null && bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.PRICECHANGE.ToString()))
                            {
                                bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Remove(bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Single(m => m.Order == MOBINFOWARNINGMESSAGEORDER.PRICECHANGE.ToString()));
                            }
                        }
                    }

                    if (IsBuyMilesFeatureEnabled(request.Application.Id, request.Application.Version.Major, isNotSelectTripCall: true) && response?.DisplayCart?.DisplayFees?.Where(a => a.Type == "MPF") != null)
                    {
                        _shoppingBuyMiles.ApplyPriceChangesForBuyMiles(response, null, bookingPathReservation: bookingPathReservation);
                    }

                    if (GetEnableTravelerTypes(request, bookingPathReservation))
                    {
                        bookingPathReservation.ShopReservationInfo2.displayTravelTypes = ShopStaticUtility.GetDisplayTravelerTypes(response.DisplayCart.DisplayTravelers);
                        bookingPathReservation.ShopReservationInfo2.TravelOptionEligibleTravelersCount = response.DisplayCart.DisplayTravelers.Where(t => !t.PaxTypeCode.ToUpper().Equals("INF")).Count();
                    }

                    if (session.IsCorporateBooking || !string.IsNullOrEmpty(session.EmployeeId))
                    {
                        bookingPathReservation.ShopReservationInfo2.TravelOptionEligibleTravelersCount = bookingPathReservation.NumberOfTravelers;
                    }

                    bookingPathReservation.LMXTravelers = reservation.lmxtravelers;

                    if (bookingPathReservation.FOPOptions != null && bookingPathReservation.FOPOptions.Count > 0) //FOP Options Fix Venkat 12/08
                    {
                        reservation.FOPOptions = bookingPathReservation.FOPOptions;
                    }
                    #region 159514 - Inhibit booking 

                    if (_configuration.GetValue<bool>("EnableInhibitBooking") && ShopStaticUtility.IdentifyInhibitWarning(response))
                    {
                        _logger.LogWarning("RegisterTravelers - Response with Inhibit warning {@Response}", JsonConvert.SerializeObject(response));
                        _travelerUtility.UpdateInhibitMessage(ref bookingPathReservation);
                    }
                    #endregion

                    if (_travelerUtility.EnableConcurrCardPolicy(bookingPathReservation.IsReshopChange))
                    {
                        if (session.IsCorporateBooking && ValidateCorporateMsg(bookingPathReservation.ShopReservationInfo2.InfoWarningMessages))
                        {
                            if (bookingPathReservation.ShopReservationInfo2.InfoWarningMessages == null)
                                bookingPathReservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                            bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Add(_travelerUtility.GetConcurrCardPolicyMessage());
                            bookingPathReservation.ShopReservationInfo2.InfoWarningMessages = bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)System.Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();

                            if (bookingPathReservation.ShopReservationInfo2.InfoWarningMessagesPayment == null)
                                bookingPathReservation.ShopReservationInfo2.InfoWarningMessagesPayment = new List<InfoWarningMessages>();

                            bookingPathReservation.ShopReservationInfo2.InfoWarningMessagesPayment.Add(_travelerUtility.GetConcurrCardPolicyMessage(true));
                        }
                    }
                    #region 1127 - Chase Offer (Booking)
                    if (_configuration.GetValue<bool>("EnableChaseOfferRTI"))
                    {
                        if (bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement != null)
                        {
                            var objPrice = bookingPathReservation.Prices.FirstOrDefault(p => p.PriceType.ToUpper().Equals("GRAND TOTAL"));
                            if (objPrice != null)
                            {
                                decimal price = Convert.ToDecimal(objPrice.Value);
                                if (_configuration.GetValue<bool>("TurnOffChaseBugMOBILE-11134"))
                                {
                                    bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement.finalAfterStatementDisplayPrice = _travelerUtility.GetPriceAfterChaseCredit(price);
                                }
                                else
                                {
                                    bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement.finalAfterStatementDisplayPrice = _travelerUtility.GetPriceAfterChaseCredit(price, bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement.statementCreditDisplayPrice);
                                }
                                bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement.initialDisplayPrice = price.ToString("C2", CultureInfo.CurrentCulture);
                                _travelerUtility.FormatChaseCreditStatemnet(bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement);
                            }
                        }

                        if (_configuration.GetValue<bool>("EnableChaseBannerFromCCEForGuestFlow"))
                        {
                            if (string.IsNullOrEmpty(request.MileagePlusNumber))
                            {
                                if (bookingPathReservation.ShopReservationInfo2 == null)
                                    bookingPathReservation.ShopReservationInfo2 = new ReservationInfo2();

                                bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement = _travelerUtility.BuildChasePromo(CHASEADTYPE.NONPREMIER.ToString());

                                // FilePersist.Save<United.Persist.Definition.Shopping.Reservation>(request.SessionId, bookingPathReservation.ObjectName, bookingPathReservation);
                            }
                        }
                    }
                    #endregion 1127 - Chase Offer (Booking)
                    #region  //==>>Need to Test below lines of Code for any use case which will have either bookingPathReservation.ShopReservationInfo2 = null OR  reservation.ShopReservationInfo2 = null

                    #endregion
                    if (!ShowViewCheckedBagsAtRti(bookingPathReservation))
                        bookingPathReservation.CheckedbagChargebutton = string.Empty;
                    reservation.CheckedbagChargebutton = bookingPathReservation.CheckedbagChargebutton;

                    #region Get client catalog values for multiple traveler etc
                    if (_configuration.GetValue<bool>("MTETCToggle"))
                    {
                        try
                        {
                            if (bookingPathReservation != null)
                            {
                                if (bookingPathReservation.ShopReservationInfo2 == null)
                                    bookingPathReservation.ShopReservationInfo2 = new ReservationInfo2();
                                // bookingPathReservation.ShopReservationInfo2.IsMultipleTravelerEtcFeatureClientToggleEnabled = Utility.IsClientCatalogEnabled(request.Application.Id,  _configuration.GetValue<string>("MultipleTravelerETCClientToggleIds"].Split('|'));
                                bookingPathReservation.ShopReservationInfo2.IsMultipleTravelerEtcFeatureClientToggleEnabled = _configuration.GetValue<bool>("MTETCToggle");
                            }
                        }
                        catch
                        { }

                    }
                    #endregion

                    #region Add Corporate Disclaimer message
                    if (_configuration.GetValue<bool>("EnableCouponsforBooking") && bookingPathReservation?.ShopReservationInfo2?.TravelType == TravelType.CLB.ToString())
                    {

                        if (bookingPathReservation.ShopReservationInfo2.InfoWarningMessages != null && bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Exists(x => x.Order == MOBINFOWARNINGMESSAGEORDER.RTICORPORATELEISUREOPTOUTMESSAGE.ToString()) && IsFareLockAvailable(bookingPathReservation))
                        {
                            bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Remove(bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Find(x => x.Order == MOBINFOWARNINGMESSAGEORDER.RTICORPORATELEISUREOPTOUTMESSAGE.ToString()));
                        }
                    }
                    #endregion
                    if (_travelerUtility.IncludeMoneyPlusMiles(request.Application.Id, request.Application.Version.Major))
                    {
                        bookingPathReservation.GetALLSavedTravelers = true;
                    }
                    if (_productInfoHelper.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, bookingPathReservation?.ShopReservationInfo2?.IsDisplayCart == true))
                    {
                        if (nextViewName != "RTI" && request.IsRegisterTravelerFromRTI || (await _featureSettings.GetFeatureSettingValue("EnableAddTravelers").ConfigureAwait(false) && bookingPathReservation.ShopReservationInfo2.HideBackButtonOnSelectTraveler))
                        {
                            bookingPathReservation.ShopReservationInfo2.HideBackButtonOnSelectTraveler = true;
                        }
                        else
                        {
                            bookingPathReservation.ShopReservationInfo2.HideBackButtonOnSelectTraveler = false;
                        }
                    }

                    // Feature TravelInsuranceOptimization : MOBILE-21191, MOBILE-21193, MOBILE-21195, MOBILE-21197
                    if (_configuration.GetValue<bool>("EnableTravelInsuranceOptimization") && !await _featureSettings.GetFeatureSettingValue("EnableTravelInsuranceRemovalFix").ConfigureAwait(false))
                    {
                        #region TPI in booking path                       
                        if (_travelerUtility.EnableTPI(request.Application.Id, request.Application.Version.Major, 3) && !bookingPathReservation.IsReshopChange)
                        {
                            // call TPI 
                            try
                            {
                                string token = session.Token;
                                TPIInfoInBookingPath tPIInfo = await _travelerUtility.GetBookingPathTPIInfo(request.SessionId, request.LanguageCode, request.Application, request.DeviceId, response.Reservation.CartId, token, true, true, false);
                                bookingPathReservation.TripInsuranceFile = new TripInsuranceFile() { };
                                bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo = tPIInfo;
                            }
                            catch
                            {
                                bookingPathReservation.TripInsuranceFile = null;
                            }
                        }
                        else
                        {
                            // register traveler should handle the reset TPI.  
                            bookingPathReservation.TripInsuranceFile = null;
                        }
                        bookingPathReservation.Prices = _travelerUtility.UpdatePriceForUnRegisterTPI(bookingPathReservation.Prices);

                        #endregion
                    }
                    #region Guatemala TaxID Collection
                    if (await _shoppingUtility.IsEnableGuatemalaTaxIdCollectionChanges(request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false)
                     && !await _featureToggles.IsEnableTaxIdCollectionForLATIDCountries(request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false)
                     && !session.IsReshopChange && _shoppingUtility.IsGuatemalaOriginatingTrip(reservation?.Trips))
                    {
                        await _shoppingUtility.BuildTaxIdInformation(reservation, request, session).ConfigureAwait(false);
                        bookingPathReservation.ShopReservationInfo2.TaxIdInformation = reservation.ShopReservationInfo2.TaxIdInformation;
                    }
                    #endregion Guatemala TaxID Collection
                    if (await _featureToggles.IsEnableU4BForMultipax(request.Application.Id, request?.Application?.Version?.Major, session?.CatalogItems))
                    {
                        bool isMultipaxAllowed = bookingPathReservation?.ShopReservationInfo2?.IsMultiPaxAllowed ?? false;
                        bookingPathReservation.ShopReservationInfo2.CorpMultiPaxInfo = _travelerUtility.corpMultiPaxInfo(session.IsCorporateBooking, session.IsArrangerBooking, isMultipaxAllowed, reservation?.RewardPrograms);
                        bookingPathReservation.ShopReservationInfo2.IsShowAddNewTraveler = session.IsArrangerBooking || (session.IsCorporateBooking && isMultipaxAllowed);
                    }
                    await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                    #endregion

                    MOBShoppingCart persistShoppingCart = new MOBShoppingCart();
                    persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(_headers.ContextValues.SessionId, persistShoppingCart.ObjectName, new List<string> { _headers.ContextValues.SessionId, persistShoppingCart.ObjectName }).ConfigureAwait(false);

                    if (persistShoppingCart == null)
                        persistShoppingCart = new MOBShoppingCart();
                    await InFlightCLPaymentEligibility(request, bookingPathReservation, session, persistShoppingCart);
                    persistShoppingCart.Products = await _productInfoHelper.ConfirmationPageProductInfo(response, false, request.Application, null, request.Flow, sessionId: session?.SessionId);
                    persistShoppingCart.CartId = request.CartId;
                    double priceTotal = _shoppingUtility.GetGrandTotalPriceForShoppingCart(false, response, false, request.Flow);
                    persistShoppingCart.TotalPrice = String.Format("{0:0.00}", priceTotal);
                    persistShoppingCart.DisplayTotalPrice = Decimal.Parse(priceTotal.ToString().Trim()).ToString("c", new CultureInfo("en-us"));
                    persistShoppingCart.TermsAndConditions = await _travelerUtility.GetProductBasedTermAndConditions(null, response, false);
                    persistShoppingCart.PaymentTarget = (request.Flow == FlowType.BOOKING.ToString()) ? _travelerUtility.GetBookingPaymentTargetForRegisterFop(response) : _travelerUtility.GetPaymentTargetForRegisterFop(response);
                    //Mahesh To Review
                    if (await _featureSettings.GetFeatureSettingValue("EnableFSRETCTravelCreditsFeature").ConfigureAwait(false) && response.DisplayCart?.SpecialPricingInfo != null && response.DisplayCart.SpecialPricingInfo.TravelCreditDetails?.TravelCertificates?.Count() > 0 && persistShoppingCart?.FormofPaymentDetails != null)
                    {
                        foreach (var displayCartAppliedTravelCredits in response.DisplayCart.SpecialPricingInfo.TravelCreditDetails?.TravelCertificates)
                        {
                            foreach (var persistSCTRavelCredits in persistShoppingCart?.FormofPaymentDetails?.TravelCreditDetails?.TravelCredits)
                            {
                                if (displayCartAppliedTravelCredits.CertPin.Equals(persistSCTRavelCredits.PinCode))
                                    persistSCTRavelCredits.IsApplied = true;
                            }
                        }
                    }                   
                    if (session.IsCorporateBooking && (await _shoppingUtility.IsEnableU4BTravelAddONPolicy(request.Application.Id, request.Application.Version.Major).ConfigureAwait(false)) && _shoppingUtility.HasPolicyWarningMessage(response?.Warnings))
                    {
                        persistShoppingCart.TravelPolicyWarningAlert = new Mobile.Model.Shopping.Common.Corporate.TravelPolicyWarningAlert();
                        CorporateDirect.Models.CustomerProfile.CorpPolicyResponse _corpPolicyResponse = await _shoppingUtility.GetCorporateTravelPolicyResponse(request.DeviceId, session.MileagPlusNumber,session.SessionId);
                        if (_corpPolicyResponse != null && _corpPolicyResponse.TravelPolicies != null && _corpPolicyResponse.TravelPolicies.Count > 0)
                        {
                            var corporateData = response?.Reservation?.Travelers?.FirstOrDefault()?.CorporateData;
                            string corporateCompanyName = corporateData != null ? corporateData.CompanyName : string.Empty;
                            var isCorporateBusinessNamePersonalized =  bookingPathReservation.ShopReservationInfo2.IsCorporateBusinessNamePersonalized;
                            persistShoppingCart.TravelPolicyWarningAlert.TravelPolicy = await _shoppingUtility.GetTravelPolicy(_corpPolicyResponse, session, request, corporateCompanyName, isCorporateBusinessNamePersonalized);
                            persistShoppingCart.TravelPolicyWarningAlert.InfoWarningMessages = new List<InfoWarningMessages>();
                            persistShoppingCart.TravelPolicyWarningAlert.InfoWarningMessages = await _shoppingUtility.BuildTravelPolicyAlert(_corpPolicyResponse, request, response, session, isCorporateBusinessNamePersonalized);
                        }
                    }
                    await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, persistShoppingCart.ObjectName }, persistShoppingCart.ObjectName).ConfigureAwait(false);
                    if (_shoppingUtility.EnableRtiMandateContentsToDisplayByMarket(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange))
                    {
                        if (bookingPathReservation.ShopReservationInfo2.RTIMandateContentsToDisplayByMarket == null || bookingPathReservation.ShopReservationInfo2.RTIMandateContentsToDisplayByMarket.Count == 0)
                        {
                            try
                            {
                                await UpdateCovidTestInfo(request, bookingPathReservation, session);
                            }
                            catch (WebException ex)
                            {
                                _logger.LogError("UpdateCovidTestInfo {@WebException}", JsonConvert.SerializeObject(ex));

                                throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                            }
                            catch (Exception ex)
                            {

                                _logger.LogError("UpdateCovidTestInfo {@Exception}", JsonConvert.SerializeObject(ex));
                                throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                            }
                        }
                    }
                    #region
                    if (_configuration.GetValue<bool>("enableBookingPathRTI_CMSContentMessages"))
                    {
                        try
                        {
                            if (bookingPathReservation != null && bookingPathReservation.Trips != null && bookingPathReservation.Trips.Any())
                            {
                                if (_configuration.GetValue<bool>("AdvisoryMsgUpdateEnable"))
                                {
                                    await GetTravelAdvisoryMessagesBookingRTI_V1(request, bookingPathReservation, session);
                                }
                                else
                                    await GetTravelAdvisoryMessagesBookingRTI(request, bookingPathReservation, session);
                            }
                        }
                        catch { }
                    }
                    #endregion

                    #region UnRegister If any ancillary offers registered
                    if (_productInfoHelper.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, true) && !string.IsNullOrEmpty(nextViewName) && nextViewName != "RTI" && !request.IsOmniCartSavedTripFlow)
                    {
                        await UnregisterAncillaryOffer(persistShoppingCart, response, request, request.SessionId, request.CartId);
                        if (await _featureSettings.GetFeatureSettingValue("EnableTravelInsuranceRemovalFix").ConfigureAwait(false))
                        {
                            if (_travelerUtility.EnableTPI(request.Application.Id, request.Application.Version.Major, 3) && !bookingPathReservation.IsReshopChange)
                            {
                                // call TPI 
                                try
                                {
                                    string token = session.Token;
                                    TPIInfoInBookingPath tPIInfo = await _travelerUtility.GetBookingPathTPIInfo(request.SessionId, request.LanguageCode, request.Application, request.DeviceId, response.Reservation.CartId, token, false, true, false);

                                }
                                catch
                                {
                                    bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                                    bookingPathReservation.TripInsuranceFile = null;
                                    await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;

                        #region 159514 - Added for inhibit booking error message
                        if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                        {
                            if (response.Errors.Exists(error => error != null && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10050")))
                            {
                                var inhibitErrorCsl = response.Errors.FirstOrDefault(error => error != null && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10050"));
                                //throw new MOBUnitedException(inhibitErrorCsl.Message);
                                throw new MOBUnitedException(inhibitErrorCsl.Message, new Exception(inhibitErrorCsl.MinorCode));
                            }
                        }
                        #endregion
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.Message;
                        }
                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToRegisterTravelerErrorMessage");
                        if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && _configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL RegisterTravelers_CFOP(MOBRegisterTravelersRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToRegisterTravelerErrorMessage");
                if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && _configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL RegisterTravelers_CFOP(MOBRegisterTravelersRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }

            //    logEntries.Add(LogEntry.GetLogEntry<MOBSHOPReservation>(request.SessionId, "RegisterTravelers_CFOP -Client Response", "to client Response", request.Application.Id, request.Application.Version.Major, request.DeviceId, reservation, true, false));

            #endregion
            return reservation;

        }

        public string GetNextViewName(MOBRegisterTravelersRequest request, Reservation bookingPathReservation)
        {
            var isTravelerChanged = ChangeTravelerValidation(request.Travelers, bookingPathReservation.TravelersCSL);
            var isFarelockExist = TravelOptionsContainsFareLock(bookingPathReservation.TravelOptions);
            return GetNavigationPageCode(request, bookingPathReservation, isFarelockExist, isTravelerChanged);
        }
        public bool ChangeTravelerValidation(List<MOBCPTraveler> travelers, SerializableDictionary<string, MOBCPTraveler> travelersCSL)
        {
            var isChanged = ((travelers != null && travelersCSL == null) || (travelers == null && travelersCSL != null));
            if (!isChanged && travelers != null && travelersCSL != null && travelersCSL.Count() == travelers.Count())
            {
                foreach (var trv in travelersCSL)
                {
                    var currentTravelrFromRequestedTravelers = travelers.FirstOrDefault(t => t.PaxID == trv.Value.PaxID);
                    isChanged = currentTravelrFromRequestedTravelers == null || CheckMileagePlus(currentTravelrFromRequestedTravelers.MileagePlus, trv.Value.MileagePlus);
                    if (isChanged)
                    {
                        break;
                    }
                }
            }
            return isChanged;
        }
        public bool CheckMileagePlus(MOBCPMileagePlus mileagePlus1, MOBCPMileagePlus mileagePlus2)
        {
            bool itsChanged = false;
            if (
                (mileagePlus1 != null && mileagePlus2 == null) ||
                (mileagePlus1 == null && mileagePlus2 != null) ||
                (mileagePlus1 != null && mileagePlus2 != null && mileagePlus1.MileagePlusId != mileagePlus2.MileagePlusId)
               )
            {
                itsChanged = true;
            }

            return itsChanged;
        }
        private string GetNavigationPageCode(MOBRegisterTravelersRequest request, Reservation bookingPathReservation1, bool isFarelockExist, bool isTravelerChanged)
        {
            var navigateTo = string.Empty;
            List<MOBCPTraveler> travelers = request.Travelers;
            if (_travelerUtility.IsEnableNavigation(bookingPathReservation1.IsReshopChange))
            {
                if ((bookingPathReservation1.ShopReservationInfo2.NextViewName == "TRAVELERADDED" ||
                    bookingPathReservation1.ShopReservationInfo2.NextViewName == "" ||
                 bookingPathReservation1.ShopReservationInfo2.AllEligibleTravelersCSL == null ||
                 bookingPathReservation1.ShopReservationInfo2.NextViewName == "TravelOption" || bookingPathReservation1.ShopReservationInfo2.NextViewName == "TravelList" ||
                 isTravelerChanged ||
            bookingPathReservation1.ShopReservationInfo2.AllEligibleTravelersCSL.Count() == 0) && !isFarelockExist)
                {
                    ///216190 : Eplus: Eplus flow is observed for the pax after navigating to seat map with no changes done
                    ///Srini - 11/22/2-17
                    if (_configuration.GetValue<bool>("BugFixToggleFor17M") &&
                        !isTravelerChanged &&
                        bookingPathReservation1.ShopReservationInfo2.NextViewName != "TravelOption" &&
                        bookingPathReservation1.ShopReservationInfo2.NextViewName != "" && bookingPathReservation1.ShopReservationInfo2.NextViewName != "TravelList")
                        navigateTo = "RTI";
                    else
                        navigateTo = "TravelOption";
                }
                else
                {
                    navigateTo = "RTI";
                    if (!isFarelockExist)
                    {
                        foreach (var traverkey in travelers)
                        {
                            if (!bookingPathReservation1.ShopReservationInfo2.AllEligibleTravelersCSL.Exists(t => t.IsPaxSelected == traverkey.IsPaxSelected && t.PaxID == traverkey.PaxID))
                            {
                                navigateTo = "TravelOption";
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                if ((bookingPathReservation1.ShopReservationInfo2.NextViewName == "TRAVELERADDED" ||
                        bookingPathReservation1.ShopReservationInfo2.NextViewName == "" ||
                     bookingPathReservation1.ShopReservationInfo2.AllEligibleTravelersCSL == null ||
                     bookingPathReservation1.ShopReservationInfo2.NextViewName == "TravelOption" ||
                     isTravelerChanged ||
                bookingPathReservation1.ShopReservationInfo2.AllEligibleTravelersCSL.Count() == 0) && !isFarelockExist)
                {
                    ///216190 : Eplus: Eplus flow is observed for the pax after navigating to seat map with no changes done
                    ///Srini - 11/22/2-17
                    if (_configuration.GetValue<bool>("BugFixToggleFor17M") &&
                        !isTravelerChanged &&
                        bookingPathReservation1.ShopReservationInfo2.NextViewName != "TravelOption" &&
                        bookingPathReservation1.ShopReservationInfo2.NextViewName != "")
                        navigateTo = "RTI";
                    else
                        navigateTo = "TravelOption";
                }
                else
                {
                    navigateTo = "RTI";
                    if (!isFarelockExist)
                    {
                        foreach (var traverkey in travelers)
                        {
                            if (!bookingPathReservation1.ShopReservationInfo2.AllEligibleTravelersCSL.Exists(t => t.IsPaxSelected == traverkey.IsPaxSelected && t.PaxID == traverkey.PaxID))
                            {
                                navigateTo = "TravelOption";
                                break;
                            }
                        }
                    }
                }
            }

            bookingPathReservation1.ShopReservationInfo2.NextViewName = navigateTo;
            bookingPathReservation1.ShopReservationInfo2.IsForceSeatMapInRTI = (navigateTo == "TravelOption");
            return navigateTo;
        }
        private bool TravelOptionsContainsFareLock(List<TravelOption> options)
        {
            bool containsFareLock = false;

            if (options != null && options.Count > 0)
            {
                foreach (TravelOption option in options)
                {
                    if (option != null && !string.IsNullOrEmpty(option.Key) && option.Key.ToUpper() == "FARELOCK")
                    {
                        containsFareLock = true;
                        break;
                    }
                }
            }

            return containsFareLock;
        }
        public async Task GetTravelAdvisoryMessagesBookingRTI(MOBRequest request, Reservation persistedReservation, Session session)
        {
            #region
            //<add key="enableBookingPathRTI_CMSContentMessages" value="true" />
            //<add key="iPhoneAppLatestAppVersionSupportHTMLContentBookingRTIAlertMessage" value="3.0.36" />
            //<add key="AndroidAppLatestAppVersionSupportHTMLContentBookingRTIAlertMessage" value="3.0.36" /> 
            //<add key = "BookingPathRTI_TravelAdvisoryTitlesForV1" value = "RTI.Destinations.State.HI.MOB.V1|RTI.Destinations.Country.GB.MOB.V1" />
            //<add key = "BookingPathRTI_TravelAdvisoryTitlesForV2" value = "RTI.Destinations.State.HI.MOB.V2|RTI.Destinations.Country.GB.MOB.V2" />
            //<add key = "BookingPathRTI_TravelAdvisoryAlert_HAWAII_Destination_Airport_Codes" value = "OGG|KOA|HNL|ITO|LIH|LNY|MKK|JHM" />
            //<add key = "BookingPathRTI_TravelAdvisoryAlert_UK_Destination_Airport_Codes" value = "EDI|LHR|MAN|ABZ" />
            #endregion
            #region

            if (persistedReservation != null && persistedReservation.ShopReservationInfo2 != null && persistedReservation.ShopReservationInfo2.InfoWarningMessages != null)
            {
                foreach (InfoWarningMessages warningMessage in persistedReservation.ShopReservationInfo2.InfoWarningMessages)
                {
                    if (warningMessage.Order == MOBINFOWARNINGMESSAGEORDER.RTITRAVELADVISORYBROKENANDRIODVERSION.ToString() || warningMessage.Order == MOBINFOWARNINGMESSAGEORDER.RTITRAVELADVISORY.ToString())
                    {
                        return;
                    }
                }
            }
            bool isHTMLContentSupportAppVersion = GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, "AndroidAppLatestAppVersionSupportHTMLContentBookingRTIAlertMessage", "iPhoneAppLatestAppVersionSupportHTMLContentBookingRTIAlertMessage", "", "", false, _configuration);
            string bookingPathRTI_TravelAdvisoryTitles = "BookingPathRTI_TravelAdvisoryTitlesForV1";
            if (isHTMLContentSupportAppVersion)
            {
                bookingPathRTI_TravelAdvisoryTitles = "BookingPathRTI_TravelAdvisoryTitlesForV2";
            }
            string travelAdvisoryTitles = GetDepatureAirportCodesFromReservation(persistedReservation, bookingPathRTI_TravelAdvisoryTitles); // "HI|UK"/"HI"/"UK"

            if (travelAdvisoryTitles != null && travelAdvisoryTitles.Trim() != "")
            {
                #region
                string jsonResponse = await _cachingService.GetCache<string>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID") + "MOBCSLContentMessagesResponse", "TID1").ConfigureAwait(false);
                CSLContentMessagesResponse response = new CSLContentMessagesResponse();
                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    response = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(jsonResponse);
                }
                if (response == null || Convert.ToBoolean(response.Status) == false || response.Messages == null)
                {
                    response = await GetBookingRTICMSContentMessages(request, session);
                }

                if (response != null && (Convert.ToBoolean(response.Status) && response.Messages != null))
                {
                    #region
                    List<InfoWarningMessages> infomessages = new List<InfoWarningMessages>();
                    foreach (CMSContentMessage contentMessage in response.Messages)
                    {
                        #region
                        if (travelAdvisoryTitles.Trim() != "" && travelAdvisoryTitles.Trim().Split('|').ToList().Contains(contentMessage.Title) &&
                            contentMessage.ContentFull != null && contentMessage.ContentFull.Trim() != "")
                        {
                            InfoWarningMessages info = new InfoWarningMessages();
                            if (!isHTMLContentSupportAppVersion && request.Application.Id == 2)
                            {
                                info.Order = MOBINFOWARNINGMESSAGEORDER.RTITRAVELADVISORYBROKENANDRIODVERSION.ToString();
                            }
                            else
                            {
                                info.Order = MOBINFOWARNINGMESSAGEORDER.RTITRAVELADVISORY.ToString();
                            }

                            info.IconType = MOBINFOWARNINGMESSAGEICON.WARNING.ToString();
                            info.Messages = new List<string>();
                            info.Messages.Add(contentMessage.ContentFull);
                            infomessages.Add(info);
                        }
                        #endregion
                    }

                    //Saving to persist
                    if (persistedReservation.ShopReservationInfo2.InfoWarningMessages == null)
                    {
                        persistedReservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();
                    }
                    persistedReservation.ShopReservationInfo2.InfoWarningMessages.AddRange(infomessages);
                    persistedReservation.ShopReservationInfo2.InfoWarningMessages = persistedReservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)System.Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                    await _sessionHelperService.SaveSession<Reservation>(persistedReservation, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, persistedReservation.ObjectName }, persistedReservation.ObjectName).ConfigureAwait(false);

                    #endregion
                }
                else
                {
                    _logger.LogError("GetTravelAdvisoryMessagesBookingRTI-Cached-Response is empty or does not have message expected {@Response}", JsonConvert.SerializeObject(response));
                }
                #endregion
            }
            #endregion
        }
        public string GetDepatureAirportCodesFromReservation(Reservation persistedReservation, string bookingPathRTI_TravelAdvisoryTitles)
        {
            List<string> destinationAirportCodes = new List<string>();
            if (persistedReservation != null && persistedReservation.Trips != null && persistedReservation.Trips.Any())
            {
                foreach (MOBSHOPTrip trip in persistedReservation.Trips)
                {
                    if (trip != null && trip.FlattenedFlights != null && trip.FlattenedFlights.Any())
                    {
                        foreach (MOBSHOPFlattenedFlight flattenedFlights in trip.FlattenedFlights)
                        {
                            if (flattenedFlights != null && flattenedFlights.Flights != null && flattenedFlights.Flights.Any())
                            {
                                foreach (MOBSHOPFlight flight in flattenedFlights.Flights)
                                {
                                    if (flight != null && !string.IsNullOrEmpty(flight.Destination))
                                    {
                                        destinationAirportCodes.Add(flight.Destination);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            string hawaiiAirports = _configuration.GetValue<string>("BookingPathRTI_TravelAdvisoryAlert_HAWAII_Destination_Airport_Codes");
            string uKAirports = _configuration.GetValue<string>("BookingPathRTI_TravelAdvisoryAlert_UK_Destination_Airport_Codes");
            //<add key = "BookingPathRTI_TravelAdvisoryAlert_HAWAII_Destination_Airport_Codes" value = "OGG|KOA|HNL|ITO|LIH|LNY|MKK|JHM" />
            //<add key = "BookingPathRTI_TravelAdvisoryAlert_UK_Destination_Airport_Codes" value = "EDI|LHR|MAN|ABZ" />
            //Check if airport code exists
            string travelAdvisoryTitles = string.Empty;
            bool isHawaiiAirortExist = false;
            bool isUKAirportExist = false;
            foreach (string airportcode in destinationAirportCodes)
            {
                if (!string.IsNullOrEmpty(hawaiiAirports) && hawaiiAirports.Trim().Split('|').ToList().Contains(airportcode))
                {
                    isHawaiiAirortExist = true;

                }
                if (!string.IsNullOrEmpty(uKAirports) && uKAirports.Trim().Split('|').ToList().Contains(airportcode))
                {
                    isUKAirportExist = true;
                }
            }
            //<add key = "BookingPathRTI_TravelAdvisoryTitlesForV1" value = "RTI.Destinations.State.HI.MOB.V1|RTI.Destinations.Country.GB.MOB.V1" />
            //<add key = "BookingPathRTI_TravelAdvisoryTitlesForV2" value = "RTI.Destinations.State.HI.MOB.V2|RTI.Destinations.Country.GB.MOB.V2" />
            if (isHawaiiAirortExist)
            {
                travelAdvisoryTitles = _configuration.GetValue<string>(bookingPathRTI_TravelAdvisoryTitles).Split('|')[0].ToString();
            }
            else if (isUKAirportExist)
            {
                travelAdvisoryTitles = _configuration.GetValue<string>(bookingPathRTI_TravelAdvisoryTitles).Split('|')[1].ToString();
            }
            return travelAdvisoryTitles;
        }

        public async Task GetTravelAdvisoryMessagesBookingRTI_V1(MOBRequest request, Reservation persistedReservation, Session session)
        {
            #region
            //<add key="enableBookingPathRTI_CMSContentMessages" value="true" />
            //<add key="iPhoneAppLatestAppVersionSupportHTMLContentBookingRTIAlertMessage" value="3.0.36" />
            //<add key="AndroidAppLatestAppVersionSupportHTMLContentBookingRTIAlertMessage" value="3.0.36" /> 
            //<add key = "BookingPathRTI_TravelAdvisoryTitlesForV1" value = "RTI.Destinations.State.HI.MOB.V1|RTI.Destinations.Country.GB.MOB.V1" />
            //<add key = "BookingPathRTI_TravelAdvisoryTitlesForV2" value = "RTI.Destinations.State.HI.MOB.V2|RTI.Destinations.Country.GB.MOB.V2" />
            //<add key = "BookingPathRTI_TravelAdvisoryAlert_HAWAII_Destination_Airport_Codes" value = "OGG|KOA|HNL|ITO|LIH|LNY|MKK|JHM" />
            //<add key = "BookingPathRTI_TravelAdvisoryAlert_UK_Destination_Airport_Codes" value = "EDI|LHR|MAN|ABZ" />
            #endregion
            #region

            if (persistedReservation != null && persistedReservation.ShopReservationInfo2 != null && persistedReservation.ShopReservationInfo2.InfoWarningMessages != null)
            {
                foreach (InfoWarningMessages warningMessage in persistedReservation.ShopReservationInfo2.InfoWarningMessages)
                {
                    if (warningMessage.Order == MOBINFOWARNINGMESSAGEORDER.RTITRAVELADVISORYBROKENANDRIODVERSION.ToString() || warningMessage.Order == MOBINFOWARNINGMESSAGEORDER.RTITRAVELADVISORY.ToString())
                    {
                        return;
                    }
                }
            }
            bool isHTMLContentSupportAppVersion = GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, "AndroidAppLatestAppVersionSupportHTMLContentBookingRTIAlertMessage", "iPhoneAppLatestAppVersionSupportHTMLContentBookingRTIAlertMessage", "", "", false, _configuration);



            #region
            string jsonResponse = await _cachingService.GetCache<string>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID") + "MOBCSLContentMessagesResponse", _headers.ContextValues.TransactionId).ConfigureAwait(false);
            CSLContentMessagesResponse response = new CSLContentMessagesResponse();
            if (!string.IsNullOrEmpty(jsonResponse))
            {
                response = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(jsonResponse);
            }
            if (response == null || Convert.ToBoolean(response.Status) == false || response.Messages == null)
            {
                response = await GetBookingRTICMSContentMessages(request, session);
            }

            if (response != null && (Convert.ToBoolean(response.Status) && response.Messages != null) && response.Messages.Any())
            {
                #region
                List<InfoWarningMessages> infomessages = new List<InfoWarningMessages>();
                List<string> titles = new List<string>();
                List<Section> confirmationMessages = new List<Section>();


                foreach (var trip in persistedReservation.Trips.Where(t => t != null))
                {
                    foreach (var fFlight in trip.FlattenedFlights.Where(ff => ff != null && ff.Flights != null && ff.Flights.Any()))
                    {
                        List<string> tmpTitles = GetListOfAdvisoryMsgs(response.Messages, fFlight.Flights, isHTMLContentSupportAppVersion, request.Application.Id, ref confirmationMessages);

                        if (tmpTitles.Any())
                        {
                            titles.AddRange(tmpTitles);
                        }
                    }
                }

                if (titles.Any())
                {
                    titles = titles.Distinct().ToList();
                    infomessages = AddCMSContentMessages(titles, isHTMLContentSupportAppVersion, request.Application.Id);
                }
                //    return contentMessages;

                if (_configuration.GetValue<bool>("IsEnableIndiaRepatriationFlightMessaging"))
                {
                    if (persistedReservation.ShopReservationInfo2.AlertMessages == null)
                        persistedReservation.ShopReservationInfo2.AlertMessages = new List<Section>();

                    if (confirmationMessages.Any())
                    {
                        persistedReservation.ShopReservationInfo2.AlertMessages.AddRange(confirmationMessages);
                    }

                    if (response.Messages.Any(m => m.Title.ToUpper().Equals(_configuration.GetValue<string>("ConfirmationFaceMaskAdvisoryTitle").ToUpper())))
                    {
                        var msg = response.Messages.First(m => m.Title.ToUpper().Equals(_configuration.GetValue<string>("ConfirmationFaceMaskAdvisoryTitle").ToUpper()));
                        persistedReservation.ShopReservationInfo2.AlertMessages.Add(new Section() { Text1 = msg.Headline, Text2 = msg.ContentFull, Order = MOBCONFIRMATIONALERTMESSAGEORDER.FACECOVERING.ToString() });
                    }

                    if (persistedReservation.ShopReservationInfo2.AlertMessages.Any())
                    {
                        persistedReservation.ShopReservationInfo2.AlertMessages = persistedReservation.ShopReservationInfo2.AlertMessages.GroupBy(x => x.Text2).Select(y => y.First()).ToList<Section>();
                    }
                }

                if (infomessages != null && infomessages.Any())
                {
                    //Saving to persist
                    if (persistedReservation.ShopReservationInfo2.InfoWarningMessages == null)
                    {
                        persistedReservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();
                    }
                    persistedReservation.ShopReservationInfo2.InfoWarningMessages.AddRange(infomessages);
                    persistedReservation.ShopReservationInfo2.InfoWarningMessages = persistedReservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)System.Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();

                    #endregion

                }
                if ((infomessages != null && infomessages.Any()) || persistedReservation.ShopReservationInfo2.AlertMessages.Any())
                {
                    await _sessionHelperService.SaveSession<Reservation>(persistedReservation, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, persistedReservation.ObjectName }, persistedReservation.ObjectName).ConfigureAwait(false);
                }
            }
            else
            {
                _logger.LogError("GetTravelAdvisoryMessagesBookingRTI-Cached-Response is empty or does not have message expected {@Response}", JsonConvert.SerializeObject(response));
            }
            #endregion

            #endregion
        }
        private List<InfoWarningMessages> AddCMSContentMessages(List<string> contentFull, bool isHTMLContentSupportAppVersion, int appId)
        {
            List<InfoWarningMessages> infomessages = new List<InfoWarningMessages>();
            foreach (var message in contentFull)
            {
                InfoWarningMessages info = new InfoWarningMessages();
                if (!isHTMLContentSupportAppVersion && appId == 2)
                {
                    info.Order = MOBINFOWARNINGMESSAGEORDER.RTITRAVELADVISORYBROKENANDRIODVERSION.ToString();
                }
                else
                {
                    info.Order = MOBINFOWARNINGMESSAGEORDER.RTITRAVELADVISORY.ToString();
                }

                info.IconType = MOBINFOWARNINGMESSAGEICON.WARNING.ToString();
                info.Messages = new List<string>();
                info.Messages.Add(message);
                infomessages.Add(info);
            }
            return infomessages;
        }

        private List<string> GetListOfAdvisoryMsgs(List<CMSContentMessage> messages, List<MOBSHOPFlight> shopFlight, bool isHTMLContentSupportAppVersion, int appId, ref List<Section> confirmationList)
        {
            List<string> titleList = new List<string>();
            if (confirmationList == null) confirmationList = new List<Section>();
            List<InfoWarningMessages> contentMessages = new List<InfoWarningMessages>();

            foreach (var flight in shopFlight.Where(f => f != null))
            {
                string title, confirmationTitles = string.Empty;
                CMSContentMessage msg = null;
                CMSContentMessage destMsg = null;

                if (!string.IsNullOrEmpty(flight.OriginCountryCode))
                {
                    title = string.Format(_configuration.GetValue<string>("BookingPathRTI_CountryTravelAdvisoryTitles_Origin"), flight.OriginCountryCode, (isHTMLContentSupportAppVersion) ? "V2" : "V1");

                    //var msg =messages.Where(m => m.Title.ToUpper().Equals(title)).FirstOrDefault()?.ContentFull;
                    msg = messages.FirstOrDefault(m => !string.IsNullOrEmpty(m.Title) && m.Title.ToUpper().Equals(title.ToUpper()));
                    if (msg != null && !string.IsNullOrEmpty(msg.ContentFull))
                    {
                        titleList.Add(msg.ContentFull);
                    }
                    if (_configuration.GetValue<bool>("IsEnableCubaConfirmPageMessaging"))
                    {
                        confirmationTitles = string.Format(_configuration.GetValue<string>("BookingPathConfirmation_CountryTravelAdvisoryTitles_Origin"), flight.OriginCountryCode);

                        if (!string.IsNullOrEmpty(confirmationTitles) && messages.Any(m => m.Title.ToUpper().Equals(confirmationTitles.ToUpper())))
                        {
                            CMSContentMessage confMessage = messages.First(m => m.Title.ToUpper().Equals(confirmationTitles.ToUpper()));
                            confirmationList.Add(new Section() { Text1 = confMessage.Headline, Text2 = confMessage.ContentFull, Order = MOBCONFIRMATIONALERTMESSAGEORDER.TRIPADVISORY.ToString() });
                        }
                    }
                }

                if (msg == null)
                {
                    if (!string.IsNullOrEmpty(flight.OriginStateCode))
                    {
                        title = string.Format(_configuration.GetValue<string>("BookingPathRTI_StateTravelAdvisoryTitles_Origin"), flight.OriginStateCode, (isHTMLContentSupportAppVersion) ? "V2" : "V1");
                        msg = messages.FirstOrDefault(m => !string.IsNullOrEmpty(m.Title) && m.Title.ToUpper().Equals(title.ToUpper()));
                        if (msg != null && !string.IsNullOrEmpty(msg.ContentFull))
                        {
                            titleList.Add(msg.ContentFull);
                        }
                    }
                    if (msg == null)
                    {
                        if (!string.IsNullOrEmpty(flight.Origin))
                        {
                            title = string.Format(_configuration.GetValue<string>("BookingPathRTI_AirportTravelAdvisoryTitles_Origin"), flight.Origin, (isHTMLContentSupportAppVersion) ? "V2" : "V1");
                            msg = messages.FirstOrDefault(m => !string.IsNullOrEmpty(m.Title) && m.Title.ToUpper().Equals(title.ToUpper()));
                            if (msg != null && !string.IsNullOrEmpty(msg.ContentFull))
                            {
                                titleList.Add(msg.ContentFull);
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(flight.DestinationCountryCode))
                {
                    title = string.Format(_configuration.GetValue<string>("BookingPathRTI_CountryTravelAdvisoryTitles_Destination"), flight.DestinationCountryCode, (isHTMLContentSupportAppVersion) ? "V2" : "V1");
                    destMsg = messages.FirstOrDefault(m => !string.IsNullOrEmpty(m.Title) && m.Title.ToUpper().Equals(title.ToUpper()));
                    if (destMsg != null && !string.IsNullOrEmpty(destMsg.ContentFull))
                    {
                        titleList.Add(destMsg.ContentFull);
                    }

                    if (_configuration.GetValue<bool>("IsEnableIndiaRepatriationFlightMessaging"))
                    {
                        confirmationTitles = string.Format(_configuration.GetValue<string>("BookingPathConfirmation_CountryTravelAdvisoryTitles_Destination"), flight.DestinationCountryCode);
                        //confirmationTitles = string.Format(Utility.GetConfigEntries("BookingPathConfirmation_CountryTravelAdvisoryTitles_Destination"), "IN");
                        if (!string.IsNullOrEmpty(confirmationTitles) && messages.Any(m => m.Title.ToUpper().Equals(confirmationTitles.ToUpper())))
                        {
                            CMSContentMessage confMessage = messages.First(m => m.Title.ToUpper().Equals(confirmationTitles.ToUpper()));
                            confirmationList.Add(new Section() { Text1 = confMessage.Headline, Text2 = confMessage.ContentFull, Order = MOBCONFIRMATIONALERTMESSAGEORDER.TRIPADVISORY.ToString() });
                        }
                    }
                }
                if (destMsg == null)
                {

                    if (!string.IsNullOrEmpty(flight.DestinationStateCode))
                    {
                        title = string.Format(_configuration.GetValue<string>("BookingPathRTI_StateTravelAdvisoryTitles_Destination"), flight.DestinationStateCode, (isHTMLContentSupportAppVersion) ? "V2" : "V1");
                        destMsg = messages.FirstOrDefault(m => !string.IsNullOrEmpty(m.Title) && m.Title.ToUpper().Equals(title.ToUpper()));
                        if (destMsg != null && !string.IsNullOrEmpty(destMsg.ContentFull))
                        {
                            titleList.Add(destMsg.ContentFull);
                        }
                    }
                    if (destMsg == null)
                    {
                        if (!string.IsNullOrEmpty(flight.Destination))
                        {
                            title = string.Format(_configuration.GetValue<string>("BookingPathRTI_AirportTravelAdvisoryTitles_Destination"), flight.Destination, (isHTMLContentSupportAppVersion) ? "V2" : "V1");
                            destMsg = messages.FirstOrDefault(m => !string.IsNullOrEmpty(m.Title) && m.Title.ToUpper().Equals(title.ToUpper()));
                            if (destMsg != null && !string.IsNullOrEmpty(destMsg.ContentFull))
                            {
                                titleList.Add(destMsg.ContentFull);
                            }
                        }
                    }
                }

                if (flight.Connections != null && flight.Connections.Any())
                {
                    GetListOfAdvisoryMsgs(messages, flight.Connections, isHTMLContentSupportAppVersion, appId, ref confirmationList);
                }
            }
            titleList = titleList.Distinct().ToList();

            return titleList;
        }

        public async Task InFlightCLPaymentEligibility(MOBRegisterTravelersRequest request, Reservation bookingPathReservation, Session session, MOBShoppingCart persistShoppingCart)
        {
            try
            {
                if (_shoppingUtility.EnableInflightContactlessPayment(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange) &&
                    !(bookingPathReservation?.TravelOptions?.Any(t => t != null && !string.IsNullOrEmpty(t.Type) && t.Type.ToUpper().Equals("FARELOCK")) ?? false))
                {
                    MOBSHOPInflightContactlessPaymentEligibility eligibility = await _sessionHelperService.GetSession<MOBSHOPInflightContactlessPaymentEligibility>(request.SessionId, ObjectNames.MOBSHOPInflightContactlessPaymentEligibility, new List<string> { request.SessionId, ObjectNames.MOBSHOPInflightContactlessPaymentEligibility }).ConfigureAwait(false);
                    if (eligibility == null)
                    {
                        eligibility = await IsEligibleInflightContactlessPayment(bookingPathReservation, request, session);
                    }
                    persistShoppingCart.InFlightContactlessPaymentEligibility = eligibility;
                }
            }
            catch { }
        }
        private async Task<MOBSHOPInflightContactlessPaymentEligibility> IsEligibleInflightContactlessPayment(United.Mobile.Model.Shopping.Reservation reservation, MOBRequest request, Session session)
        {
            MOBSHOPInflightContactlessPaymentEligibility eligibility = new MOBSHOPInflightContactlessPaymentEligibility(false, null, null);
            try
            {
                Collection<United.Service.Presentation.ProductModel.ProductSegment> list = GetInflightPurchaseEligibility(reservation, request, session);
                if (list?.Any(l => l != null && !string.IsNullOrEmpty(l.IsRulesEligible) && l.IsRulesEligible.ToLower().Equals("true")) ?? false)
                {
                    if (_configuration.GetValue<bool>("EnableCreditCardSelectedForPartialEligibilityContactless") && (list.Count != list.Where(l => l != null && !string.IsNullOrEmpty(l.IsRulesEligible) && l.IsRulesEligible.ToLower().Equals("true")).Count()))
                    {
                        eligibility = new MOBSHOPInflightContactlessPaymentEligibility(true, _configuration.GetValue<string>("CreditCardSelectedForPartialEligibilityContactlessTitle"), _configuration.GetValue<string>("CreditCardSelectedForPartialEligibilityContactlessMessage"));
                    }
                    else
                    {
                        eligibility = new MOBSHOPInflightContactlessPaymentEligibility(true, _configuration.GetValue<string>("CreditCardSelectedForContactlessTitle"), _configuration.GetValue<string>("CreditCardSelectedForContactlessMessage"));
                    }
                }

                await _sessionHelperService.SaveSession<MOBSHOPInflightContactlessPaymentEligibility>(eligibility, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, eligibility.ObjectName }, eligibility.ObjectName).ConfigureAwait(false);
            }
            catch { }
            return eligibility;
        }
        private Collection<ProductSegment> GetInflightPurchaseEligibility(United.Mobile.Model.Shopping.Reservation reservation, MOBRequest request, Session session)
        {
            //United.Logger.Database.SqlServerLoggerProvider logger = new United.Logger.Database.SqlServerLoggerProvider();
            try
            {
                //string url = $"{Utility.GetConfigEntries("ServiceEndPointBaseUrl - CSLMerchandizingservice")}/GetProductEligibility";

                United.Service.Presentation.ProductRequestModel.ProductEligibilityRequest eligibilityRequest = new United.Service.Presentation.ProductRequestModel.ProductEligibilityRequest();
                eligibilityRequest.Filters = new System.Collections.ObjectModel.Collection<United.Service.Presentation.ProductRequestModel.ProductFilter>()
            {
                new United.Service.Presentation.ProductRequestModel.ProductFilter()
                {
                    ProductCode = "PEC"
                }
            };
                eligibilityRequest.Requestor = new United.Service.Presentation.CommonModel.Requestor()
                {
                    ChannelID = _configuration.GetValue<string>("MerchandizeOffersCSLServiceChannelID"),
                    ChannelName = _configuration.GetValue<string>("MerchandizeOffersCSLServiceChannelName")
                };

                int segNum = 0;
                string departureDateTime(string date)
                {
                    DateTime dt;
                    DateTime.TryParse(date, out dt);
                    return dt != null ? dt.ToString() : date;
                }
                eligibilityRequest.FlightSegments = new Collection<ProductSegment>();


                reservation?.Trips?.ForEach(
                    t => t?.FlattenedFlights?.ForEach(
                        ff => ff?.Flights?.ForEach(
                            f => eligibilityRequest?.FlightSegments?.Add(new ProductSegment()
                            {
                                SegmentNumber = ++segNum,
                                ClassOfService = f.ServiceClass,
                                OperatingAirlineCode = f.OperatingCarrier,
                                DepartureDateTime = departureDateTime(f.DepartureDateTime),
                                ArrivalDateTime = f.ArrivalDateTime,
                                DepartureAirport = new United.Service.Presentation.CommonModel.AirportModel.Airport() { IATACode = f.Origin },
                                ArrivalAirport = new United.Service.Presentation.CommonModel.AirportModel.Airport() { IATACode = f.Destination },
                                Characteristic = new System.Collections.ObjectModel.Collection<United.Service.Presentation.CommonModel.Characteristic>()
                                                 {
                                                 new Service.Presentation.CommonModel.Characteristic() { Code="Program", Value="Contactless" }
                                                 }
                            })
                            )
                        )
                    );
                string jsonRequest = JsonConvert.SerializeObject(eligibilityRequest);


                var response = (_purchaseMerchandizingService.GetInflightPurchaseInfo<ProductEligibilityResponse>(session.Token, "GetProductEligibility", jsonRequest, _headers.ContextValues.SessionId).Result).response;

                if (string.IsNullOrEmpty(response.Response.ToString()))
                {
                    return null;
                }

                if (response?.FlightSegments?.Count == 0)
                {
                    _logger.LogError("GetInflightPurchaseEligibility Failed to deserialize CSL response");
                    return null;
                }

                if (response.Response.Error?.Count > 0)
                {
                    string errorMsg = String.Join(", ", response.Response.Error.Select(x => x.Text));
                    _logger.LogWarning("GetInflightPurchaseEligibility {@UnitedException}", errorMsg);
                    return null;
                }

                return response.FlightSegments;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetInflightPurchaseEligibility {@Exception}", JsonConvert.SerializeObject(ex));
            }
            return null;
        }

        public bool ShowViewCheckedBagsAtRti(Reservation reservation)
        {
            if (reservation == null)
                return false;

            if (reservation.IsMetaSearch)
                return false;

            return !IsFareLockAvailable(reservation);
        }

        public bool IsFareLockAvailable(Reservation reservation)
        {
            return reservation != null &&
                   reservation.FareLock != null &&
                   reservation.FareLock.FareLockProducts != null &&
                   reservation.FareLock.FareLockProducts.Any();
        }
        public bool IsNatAndResEnabled(MOBRegisterTravelersRequest request, Reservation bookingPathReservation)
        {
            return bookingPathReservation != null && _shoppingUtility.IsEnabledNationalityAndResidence(bookingPathReservation.IsReshopChange, request.Application.Id, request.Application.Version.Major)
        && bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence != null &&
                                (bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence);
        }
        public async Task<List<MOBCPTraveler>> GetMPDetailsForSavedTravelers(MOBRegisterTravelersRequest request)
        {
            var travelers = request.Travelers;

            List<MOBBKLoyaltyProgramProfile> airRewardProgramList = new List<MOBBKLoyaltyProgramProfile>();

            //Build CSL MP List request
            United.Services.Customer.Common.MileagePlusesRequest mpRequest = new Services.Customer.Common.MileagePlusesRequest();
            mpRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices");
            mpRequest.LangCode = request.LanguageCode;
            mpRequest.LoyaltyIds = new List<string>();


            //Get the MPList 
            if (request.Travelers != null)
            {
                foreach (var mobTraveler in request.Travelers)
                {
                    if (!mobTraveler.IsProfileOwner)
                    {
                        if (mobTraveler.AirRewardPrograms != null && mobTraveler.AirRewardPrograms.Count > 0)
                        {
                            airRewardProgramList = (from program in mobTraveler.AirRewardPrograms
                                                    where (program.CarrierCode != null && program.CarrierCode.ToUpper().Trim() == "UA") || (program.ProgramId != null && Convert.ToInt32(program.ProgramId) == 7)
                                                    select program).ToList();

                            if (airRewardProgramList != null && airRewardProgramList.Count > 0)
                            {
                                mpRequest.LoyaltyIds.Add(airRewardProgramList[0].MemberId);
                            }
                        }
                    }
                }
            }
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string> { request.SessionId, session.ObjectName }).ConfigureAwait(false);
            supressLMX = session.SupressLMXForAppID;
            #endregion
            if (mpRequest.LoyaltyIds != null && mpRequest.LoyaltyIds.Count > 0 && !supressLMX) // Fix to check if Selected Saved travler has MP. - Venkat 06/17/2015
            {
                #region Get MileagePlus list from CSL
                //call CSL for MP list

                string jsonRequest = JsonConvert.SerializeObject(mpRequest);

                var response = await _customerDataService.GetMileagePluses<MileagePlusesResponse>(session.Token, session.SessionId, jsonRequest).ConfigureAwait(false);

                if (response.response != null)
                {
                    if (response.response != null && response.response.Status.Equals(United.Services.Customer.Common.Constants.StatusType.Success) && response.response.MileagePlusList != null)
                    {
                        var mileagePluses = new List<MOBCPMileagePlus>();
                        mileagePluses = GetMpListFromCSLResponse(response.response);

                        foreach (var traveler in travelers)
                        {
                            if (!traveler.IsProfileOwner)
                            {
                                if (traveler.AirRewardPrograms != null && traveler.AirRewardPrograms.Count > 0)
                                {

                                    if (airRewardProgramList != null && airRewardProgramList.Count > 0)
                                    {

                                        foreach (var mileagePlus in mileagePluses)
                                        {
                                            if (airRewardProgramList[0].MemberId.ToUpper().Trim().Equals(mileagePlus.MileagePlusId.ToUpper().Trim()))
                                            {
                                                traveler.MileagePlus = mileagePlus;
                                            }

                                        }
                                    }
                                }
                            }

                        }
                    }
                    else
                    {
                        if (response.response.Errors != null && response.response.Errors.Count > 0)
                        {
                            string errorMessage = string.Empty;
                            foreach (var error in response.response.Errors)
                            {
                                if (!string.IsNullOrEmpty(error.UserFriendlyMessage))
                                {
                                    errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(error.Message) && error.Message.ToUpper().Trim().Contains("INVALID"))
                                    {
                                        errorMessage = errorMessage + " " + "Invalid MileagePlusId " + request.MileagePlusNumber;
                                    }
                                    else
                                    {
                                        errorMessage = errorMessage + " " + (error.MinorDescription != null ? error.MinorDescription : string.Empty);
                                    }
                                }
                                _logger.LogError("RegisterTravelers - Response for get MileagePluses {@Exception}, {@Response}", errorMessage, JsonConvert.SerializeObject(response.response));
                            }

                            throw new MOBUnitedException(errorMessage);
                        }
                        else
                        {
                            throw new MOBUnitedException("Unable to get MileagePlus List.");
                        }
                    }
                }
                else
                {
                    throw new MOBUnitedException("Unable to get MileagePlus List.");
                }
                #endregion
            }

            if (travelers != null && travelers.Count > 0)
            {
                foreach (var traveler in travelers)
                {
                    //TFS 53507: "Regression: ""More information needed"" text is not displayed when DOB and gender field is blank for MP account"
                    //if (!(string.IsNullOrEmpty(traveler.FirstName) && string.IsNullOrEmpty(traveler.LastName) && string.IsNullOrEmpty(traveler.BirthDate) && string.IsNullOrEmpty(traveler.GenderCode)))
                    if (!traveler.IsTSAFlagON && !string.IsNullOrEmpty(traveler.FirstName) && !string.IsNullOrEmpty(traveler.LastName) && !string.IsNullOrEmpty(traveler.GenderCode) && !string.IsNullOrEmpty(traveler.BirthDate))
                    {
                        traveler.Message = string.Empty;
                    }
                    // 4
                    //if (traveler.Phones != null && traveler.Phones.Count > 0)
                    //{
                    //    foreach (MOBCPPhone mobcoPhone in traveler.Phones)
                    //    {
                    //        mobcoPhone.CountryCode = profile.GetCountryCode(mobcoPhone.CountryCode.Substring(2));
                    //    }
                    //}
                }
            }

            return travelers;
        }
        public async Task UpdateCovidTestInfo(MOBRegisterTravelersRequest request, Reservation bookingPathReservation, Session session)
        {
            string cmsContentCache;
            CSLContentMessagesResponse cmsResponse = new CSLContentMessagesResponse();
            List<CMSContentMessage> cmsMessages = null;
            try
            {
                cmsContentCache = await _cachingService.GetCache<string>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID") + "MOBCSLContentMessagesResponse", _headers.ContextValues.TransactionId).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(cmsContentCache))
                {
                    cmsResponse = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(cmsContentCache);
                }
                if (cmsResponse == null || Convert.ToBoolean(cmsResponse.Status) == false || cmsResponse.Messages == null)
                {
                    cmsResponse = await GetBookingRTICMSContentMessages(request, session);
                }
                cmsMessages = (cmsResponse != null && cmsResponse.Messages != null && cmsResponse.Messages.Count > 0) ? cmsResponse.Messages : null;
            }
            catch (Exception ex)
            {
                _logger.LogError("RTICMSContentMessages {@Exception}", JsonConvert.SerializeObject(ex));
            }

            if ((IsCovidTestFlagFromReservation(request.Application.Id, request.Application.Version.Major, bookingPathReservation)) ? bookingPathReservation.ShopReservationInfo2.IsCovidTestFlight : VerifyTAPServiceMKTEligibleForCovidTest(bookingPathReservation.Trips, cmsMessages))
            //if (VerifyTAPServiceMKTEligibleForCovidTest(bookingPathReservation.Trips, cmsMessages))
            {
                await CovidTestInfo(request, bookingPathReservation, session, cmsMessages);

                if (bookingPathReservation.ShopReservationInfo2.RTIMandateContentsToDisplayByMarket != null && bookingPathReservation.ShopReservationInfo2.RTIMandateContentsToDisplayByMarket.Count > 0)
                {

                    if (!string.IsNullOrEmpty(bookingPathReservation.ShopReservationInfo2.RTIMandateContentsToDisplayByMarket[0].MandateContentDetail?.NavigatePageBody))
                    {
                        if (bookingPathReservation.ShopReservationInfo2.AlertMessages == null)
                            bookingPathReservation.ShopReservationInfo2.AlertMessages = new List<Section>();

                        if (bookingPathReservation.ShopReservationInfo2.AlertMessages.Count == 0 || !bookingPathReservation.ShopReservationInfo2.AlertMessages.Any(m => !string.IsNullOrEmpty(m.Text2) && m.Text2.Equals(bookingPathReservation.ShopReservationInfo2.RTIMandateContentsToDisplayByMarket[0].MandateContentDetail.NavigatePageBody)))
                        {
                            Section confirmationCovidTestInfo = new Section()
                            {
                                Text1 = (cmsMessages?.FirstOrDefault(m => !string.IsNullOrEmpty(m.Title) && m.Title.ToUpper().Equals("CONFIRMATION_COVIDTESTINFOTITLE_MOB"))?.ContentFull) ??
                                _configuration.GetValue<string>("ConfirmationCovidTestInfoTitle"),
                                Text2 = bookingPathReservation?.ShopReservationInfo2?.RTIMandateContentsToDisplayByMarket[0]?.MandateContentDetail?.NavigatePageBody,
                                Order = MOBCONFIRMATIONALERTMESSAGEORDER.COVIDTESTINFO.ToString()
                            };

                            bookingPathReservation.ShopReservationInfo2.AlertMessages.Add(confirmationCovidTestInfo);

                            await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                        }
                    }

                }

            }

        }
        private bool VerifyTAPServiceMKTEligibleForCovidTest(List<MOBSHOPTrip> trips, List<CMSContentMessage> messages)
        {
            //ToggleToCheckandCallTAPServiceOnlyIfItineraryHasAirportsListedAsCOVIDTestMandatory
            if (_configuration.GetValue<bool>("ToggleToCallTAPServiceIfItineraryHasAirportsListedAsCOVIDTestMandatory")) // This Check is to avoid unneccessary calls for COVID TEST Mandatory so we not sending all our Shop Calls traffic to TAP Service.
            {
                string aiportListForCOVIDTestMandatory = _configuration.GetValue<string>("AiportListForCOVIDTestMandatory"); // ArrivalAiportListForCOVIDTestMandatory = 'LHR|DEL|BOM|SYD|AKL';
                if (_configuration.GetValue<bool>("GetAiportListForCOVIDTestMandatoryFromSDL")) // This toggle is to get the COVID TEST mandatory airports from SDL
                {
                    aiportListForCOVIDTestMandatory = (messages?.FirstOrDefault(m => !string.IsNullOrEmpty(m.Title) && m.Title.ToUpper().Equals("RTI_AIPORTLISTCOVIDTESTREQUIRED_MOB"))?.ContentFull) ?? _configuration.GetValue<string>("AiportListForCOVIDTestMandatory");  //Add this same ArrivalAiportListForCOVIDTestMandatory = 'LHR|DEL|BOM|SYD|AKL'; to SDL
                }

                List<string> airportList = new List<string>();
                List<string> airportListCovidTest = new List<string>();


                foreach (var t in trips)
                {
                    foreach (var ff in t.FlattenedFlights.Where(ff => ff != null))
                    {
                        foreach (var f in ff.Flights.Where(f => f != null))
                        {
                            if (!airportList.Contains(f.Destination.ToUpper()))
                            {
                                airportList.Add(f.Destination.ToUpper());
                            }
                            if (!airportList.Contains(f.Origin.ToUpper()))
                            {
                                airportList.Add(f.Origin.ToUpper());
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(aiportListForCOVIDTestMandatory))
                {
                    aiportListForCOVIDTestMandatory = aiportListForCOVIDTestMandatory.ToUpper();
                    airportListCovidTest = aiportListForCOVIDTestMandatory.Split('|').ToList();
                }


                if (airportList.Count > 0 && airportListCovidTest.Count > 0)
                {
                    if (airportList.Intersect(airportListCovidTest).ToList().Count > 0)
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
            return true;
        }

        private async Task CovidTestInfo(MOBRequest request, Reservation persistedReservation, Session session, List<CMSContentMessage> messages)
        {
            if (persistedReservation.ShopReservationInfo2 == null)
                persistedReservation.ShopReservationInfo2 = new ReservationInfo2();


            var flights = GetFlightsCovidLite(persistedReservation.Trips);

            if (flights != null && flights.Count > 0)
            {
                CovidLiteRequest covidLiteReq = new CovidLiteRequest()
                {
                    Flights = flights,
                    TestFlightOnly = true
                };
                var covidLiteResponse = await GetCovidLiteInfo(session, covidLiteReq, request);
                CovidLite covidTestInfo = CovidInfoFlight(covidLiteResponse);

                if (IsCovidTestFlagFromReservation(request.Application.Id, request.Application.Version.Major, persistedReservation))
                {
                    if (persistedReservation.ShopReservationInfo2.IsCovidTestFlight)
                    {
                        string msg = "TAP Service not returning IsTestFlight true.FS specifying trip has covid test.";
                        _logger.LogError("CovidTestInfo {@UnitedException}", msg);
                        if (covidTestInfo == null || string.IsNullOrEmpty(covidTestInfo.Information))
                            throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                    }
                }
                if (covidTestInfo != null)
                {
                    RTIMandateContentToDisplayByMarket mandateContent = new RTIMandateContentToDisplayByMarket();
                    CMSContentMessage msg = null;

                    msg = messages?.FirstOrDefault(m => !string.IsNullOrEmpty(m.Title) && m.Title.ToUpper().Equals("RTI_COVIDTESTINFOMANDATECONTENTBODYMSG_MOB"));

                    mandateContent.HeaderMsg = (msg != null) ? msg.Headline : _configuration.GetValue<string>("RTIMandateContentHeaderMsg");
                    mandateContent.BodyMsg = (msg != null) ? msg.ContentFull : HttpUtility.HtmlDecode(_configuration.GetValue<string>("RTIMandateContentBodyMsg"));
                    mandateContent.MandateContentDetail = new RTIMandateContentDetail();
                    //mandateContent.MandateContentDetail.NavigateToLinkLabel = ConfigurationManager.AppSettings["RTIMandateContentNavigateToLinkLabel"];
                    mandateContent.MandateContentDetail.NavigateToLinkLabel = (messages?.FirstOrDefault(m => !string.IsNullOrEmpty(m.Title) && m.Title.ToUpper().Equals("RTI_COVIDTESTINFOMANDATECONTENTNAVIGATETOLINKLABEL_MOB"))?.ContentFull) ??
                        _configuration.GetValue<string>("RTIMandateContentNavigateToLinkLabel");
                    //mandateContent.MandateContentDetail.NavigatePageTitle = ConfigurationManager.AppSettings["RTIMandateContentNavigatePageTitle"];
                    mandateContent.MandateContentDetail.NavigatePageTitle = (messages?.FirstOrDefault(m => !string.IsNullOrEmpty(m.Title) && m.Title.ToUpper().Equals("RTI_COVIDTESTINFOMANDATECONTENTNAVIGATEPAGETITLE_MOB"))?.ContentFull) ??
                       _configuration.GetValue<string>("RTIMandateContentNavigatePageTitle");
                    mandateContent.MandateContentDetail.NavigatePageBody = covidTestInfo.Information;
                    persistedReservation.ShopReservationInfo2.RTIMandateContentsToDisplayByMarket = new List<RTIMandateContentToDisplayByMarket>(){
                                mandateContent };
                }
            }
        }
        private async Task<CovidLiteResponse> GetCovidLiteInfo(Session session, CovidLiteRequest request, MOBRequest mobReq)
        {
            // string url = $"{ "ServiceEndPointBaseUrl - TAPService"}/{"GetCovidLite"}";
            //https://checkindev.ual.com/tcd/travelReady/GetCovidLite
            string action = "GetCovidLite";
            string jsonRequest = JsonConvert.SerializeObject(request);

            var response = await _travelReadyService.GetCovidLite<CovidLiteResponse>(session.Token, action, jsonRequest, session.SessionId).ConfigureAwait(false);

            if (response.response != null)
            {
                if (response.response != null && response.response.ErrCode == 0)
                {
                    return response.response;
                }
                else
                {
                    if (response.response != null && response.response.ErrMsg != null)
                    {
                        throw new Exception(response.response.ErrMsg);
                    }

                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
        }
        private CovidLite CovidInfoFlight(CovidLiteResponse covidLiteResponse)
        {
            return (covidLiteResponse?.CovidLites != null && covidLiteResponse.CovidLites.Count > 0 &&
                covidLiteResponse.CovidLites.Any(c => c.IsTestFlight)) ? covidLiteResponse.CovidLites.First(c => c.IsTestFlight) : null;
        }
        private Collection<Flight> GetFlightsCovidLite(List<MOBSHOPTrip> trips)
        {
            Collection<Flight> covidLiteRequestFlights = new Collection<Flight>();
            if (trips != null)
            {
                foreach (var trip in trips)
                {
                    foreach (var flights in trip.FlattenedFlights)
                    {
                        foreach (var flight in flights.Flights)
                        {
                            covidLiteRequestFlights.Add(MapToCovidLiteFlights(flight));
                        }
                    }
                }
            }

            return covidLiteRequestFlights;
        }
        private Flight MapToCovidLiteFlights(MOBSHOPFlight flight)
        {
            var clMOBFlight = new Flight
            {
                Origin = flight.Origin,
                Destination = flight.Destination,
                OperatorFlightNumber = _configuration.GetValue<bool>("DisableOperatingCarrierFlightNumberChanges")
                                      ? Convert.ToInt32(flight.FlightNumber)
                                      : Convert.ToInt32(String.IsNullOrEmpty(flight.OperatingCarrierFlightNumber)
                                         ? flight.FlightNumber
                                         : flight.OperatingCarrierFlightNumber),
                OperatorCarrierCode = flight.OperatingCarrier,
                //ArrivalDateTimeUTC = flight.ArrivalDateTime,
                //DepartureDateTimeUTC = flight.DepartureDateTime,
                ArrivalDateTimeUTC = flight.ArrivalDateTimeGMT,
                DepartureDateTimeUTC = flight.DepartureDateTimeGMT
            };


            return clMOBFlight;
        }

        public async Task<CSLContentMessagesResponse> GetBookingRTICMSContentMessages(MOBRequest request, Session session)
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
                    await _cachingService.SaveCache<string>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID") + "MOBCSLContentMessagesResponse", jsonResponse, _headers.ContextValues.TransactionId, new TimeSpan(1, 30, 0)).ConfigureAwait(false);

                    //_logger.LogInformation("GetBookingRTICMSContentMessages {@CMSResponse}", JsonConvert.SerializeObject(response));
                }
                else
                {
                    _logger.LogWarning("GetBookingRTICMSContentMessages {@Response}", JsonConvert.SerializeObject(response));
                }
            }
            else
            {
                //logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "GetBookingRTICMSContentMessages-EmptyJSONReturnedBYCMSService", "MOBUnitedException", request.Application.Id, request.Application.Version.Major, request.DeviceId, string.Empty));
                _logger.LogWarning("EmptyJSONReturnedBYCMSService response is empty");
            }
            return response;
            #endregion
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
            string jsonRequest = System.Text.Json.JsonSerializer.Serialize(cslContentReqeust);
            string jsonResponse = await _cMSContentService.GetMobileCMSContentDetail(token: request.Token, JsonConvert.SerializeObject(request), request.SessionId);
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
                if (!_configuration.GetValue<bool>("DonotUsecache4CSMContents"))
                {
                    if (!istravelAdvisory)
                        cslContentReqeust.Usecache = true;
                }
            }
            return cslContentReqeust;
        }


        private bool IsCovidTestFlagFromReservation(int id, string version, Reservation bookingPathReservation)
        {
            return _shoppingUtility.EnableCovidTestFlightShopping(id, version, bookingPathReservation.IsReshopChange) && _configuration.GetValue<bool>("EnableCovidTestFlightShoppingForMetaAndPindown");
        }
        public bool ValidateCorporateMsg(List<InfoWarningMessages> infoWarningMessages)
        {
            if (infoWarningMessages == null || infoWarningMessages.Count == 0) return true;

            if (infoWarningMessages.Count > 0)
            {
                return !infoWarningMessages.Any(m => !string.IsNullOrEmpty(m.Order) && m.Order.Equals("CONCURRCARDPOLICY"));
            }

            return false;
        }

        public List<List<MOBSHOPTax>> GetTaxAndFeesAfterPriceChange(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices, int numPax, bool isReshopChange = false, int appId = 0, string appVersion = "", string travelType = null)
        {
            List<List<MOBSHOPTax>> taxsAndFees = new List<List<MOBSHOPTax>>();
            CultureInfo ci = null;
            decimal taxTotal = 0.0M;
            decimal subTaxTotal = 0.0M;
            bool isTravelerPriceDirty = false;
            bool isEnableOmniCartMVP2Changes = _configuration.GetValue<bool>("EnableOmniCartMVP2Changes");

            foreach (var price in prices)
            {
                List<MOBSHOPTax> tmpTaxsAndFees = new List<MOBSHOPTax>();

                subTaxTotal = 0;

                if (price.SubItems != null && price.SubItems.Count > 0 && (!isReshopChange || (isReshopChange && price.Type.ToUpper() == "TRAVELERPRICE" && !isTravelerPriceDirty))) // Added by Hasnan - # 167553 - 10/04/2017
                {
                    foreach (var subItem in price.SubItems)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(subItem.Currency);
                        }
                        MOBSHOPTax taxNfee = new MOBSHOPTax();
                        taxNfee = new MOBSHOPTax();
                        taxNfee.CurrencyCode = subItem.Currency;
                        taxNfee.Amount = subItem.Amount;
                        taxNfee.DisplayAmount = TopHelper.FormatAmountForDisplay(taxNfee.Amount, new CultureInfo("en-US"), false);
                        taxNfee.TaxCode = subItem.Type;
                        taxNfee.TaxCodeDescription = subItem.Description;
                        isTravelerPriceDirty = true;
                        tmpTaxsAndFees.Add(taxNfee);

                        subTaxTotal += taxNfee.Amount;
                    }
                }

                if (tmpTaxsAndFees != null && tmpTaxsAndFees.Count > 0)
                {
                    //add new label as first item for UI
                    MOBSHOPTax tnf = new MOBSHOPTax();
                    tnf.CurrencyCode = tmpTaxsAndFees[0].CurrencyCode;
                    tnf.Amount = subTaxTotal;
                    tnf.DisplayAmount = TopHelper.FormatAmountForDisplay(tnf.Amount, ci, false);
                    tnf.TaxCode = "PERPERSONTAX";
                    if (EnableYADesc(isReshopChange) && price.PricingPaxType != null && price.PricingPaxType.ToUpper().Equals("UAY"))
                    {
                        tnf.TaxCodeDescription = string.Format("{0} {1}: {2} per person", price.Count, "young adult (18-23)", tnf.DisplayAmount);
                    }
                    else
                    {
                        string description = price?.Description;
                        if (EnableShoppingcartPhase2ChangesWithVersionCheck(appId, appVersion) && !isReshopChange && !string.IsNullOrEmpty(travelType) && (travelType == TravelType.CB.ToString() || travelType == TravelType.CLB.ToString()))
                        {
                            description = BuildPaxTypeDescription(price?.PaxTypeCode, price?.Description, price.Count);
                        }
                        tnf.TaxCodeDescription = string.Format("{0} {1}: {2} per person", price.Count, description.ToLower(), tnf.DisplayAmount);
                    }
                    if (isEnableOmniCartMVP2Changes)
                    {
                        tnf.TaxCodeDescription = tnf.TaxCodeDescription.Replace(" per ", "/");
                    }
                    tmpTaxsAndFees.Insert(0, tnf);
                }
                taxTotal += subTaxTotal * price.Count;
                if (tmpTaxsAndFees.Count > 0)
                {
                    taxsAndFees.Add(tmpTaxsAndFees);
                }

            }
            if (taxsAndFees != null && taxsAndFees.Count > 0)
            {
                //add grand total for all taxes
                List<MOBSHOPTax> lstTnfTotal = new List<MOBSHOPTax>();
                MOBSHOPTax tnfTotal = new MOBSHOPTax();
                tnfTotal.CurrencyCode = taxsAndFees[0][0].CurrencyCode;
                tnfTotal.Amount += taxTotal;
                tnfTotal.DisplayAmount = TopHelper.FormatAmountForDisplay(tnfTotal.Amount, ci, false);
                tnfTotal.TaxCode = "TOTALTAX";
                tnfTotal.TaxCodeDescription = "Taxes and fees total";
                lstTnfTotal.Add(tnfTotal);
                taxsAndFees.Add(lstTnfTotal);
            }

            return taxsAndFees;
        }

        public List<MOBSHOPPrice> GetPricesAfterPriceChange(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices, bool isAwardBooking, string sessionId, bool isReshopChange = false, FlightReservationResponse flightReservationResponse = null)
        {
            List<MOBSHOPPrice> bookingPrices = new List<MOBSHOPPrice>();
            CultureInfo ci = null;
            foreach (var price in prices.Where(p => p != null && p.Type != null && !(p.Type.ToUpper().Equals("TRAVELERPRICE"))).ToList())
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
                        _shoppingUtility.ValidateAwardMileageBalance(sessionId, price.Amount);
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
                    bookingPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(price.Amount, new CultureInfo("en-US"), false);
                }

                bookingPrices.Add(bookingPrice);
            }
            decimal subTotal = 0.0M;
            subTotal = prices.Where(p => !string.IsNullOrEmpty(p.Type) && p.Type.ToUpper().Equals("TRAVELERPRICE")).ToList().Sum(p => p.Amount);
            DisplayPrice travelerPrice = prices.Where(p => !string.IsNullOrEmpty(p.Type) && p.Type.ToUpper().Equals("TRAVELERPRICE")).ToList()[0];

            if (ci == null)
            {
                ci = TopHelper.GetCultureInfo(travelerPrice.Currency);
            }

            MOBSHOPPrice bookingPrice1 = new MOBSHOPPrice();
            bookingPrice1.CurrencyCode = travelerPrice.Currency;
            bookingPrice1.DisplayType = travelerPrice.Type;
            bookingPrice1.Status = travelerPrice.Status;
            bookingPrice1.Waived = travelerPrice.Waived;
            bookingPrice1.DisplayValue = string.Format("{0:#,0.00}", subTotal);

            double tempDouble1 = 0;
            double.TryParse(subTotal.ToString(), out tempDouble1);
            bookingPrice1.Value = Math.Round(tempDouble1, 2, MidpointRounding.AwayFromZero);

            if (travelerPrice.Currency.ToUpper() == "MIL")
            {
                bookingPrice1.FormattedDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(subTotal.ToString(), false);
            }
            else
            {
                bookingPrice1.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(subTotal, ci, false);
            }
            bookingPrices.Add(bookingPrice1);

            return bookingPrices;
        }
        public List<MOBSHOPPrice> AdjustTotal(List<MOBSHOPPrice> prices)
        {
            CultureInfo ci = null;

            List<MOBSHOPPrice> newPrices = prices;
            double fee = 0;
            foreach (MOBSHOPPrice p in newPrices)
            {
                if (ci == null)
                {
                    ci = TopHelper.GetCultureInfo(p.CurrencyCode);
                }

                if (fee == 0)
                {
                    foreach (MOBSHOPPrice q in newPrices)
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

        public bool comapreTtypesList(Reservation bookingPathReservation, DisplayCart displayCart)
        {
            List<string> lstReservation = bookingPathReservation.ShopReservationInfo2.displayTravelTypes.Select(t => t.PaxType).ToList();
            List<string> lstResponse = displayCart.DisplayTravelers.Select(t => t.PaxTypeCode).ToList();


            if (lstReservation.Count == lstResponse.Count && lstReservation.Except(lstResponse).Count() == 0 && lstResponse.Except(lstReservation).Count() == 0)
                return true;

            return false;
        }
        public bool GetEnableTravelerTypes(MOBRegisterTravelersRequest request, Reservation bookingPathReservation)
        {
            return _shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange) && bookingPathReservation.ShopReservationInfo2 != null
                && bookingPathReservation.ShopReservationInfo2.TravelerTypes != null && bookingPathReservation.ShopReservationInfo2.TravelerTypes.Count > 0;
        }
        public bool IsArranger(Reservation bookingPathReservation)
        {
            return _configuration.GetValue<bool>("EnableIsArranger") && bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.IsArrangerBooking;
        }
        public void AssignFFCsToUnChangedTravelers(List<MOBCPTraveler> requestedTravelers, SerializableDictionary<string, MOBCPTraveler> persistedTravelers, MOBApplication application, bool continueToChangeTravelers)
        {
            if (_shoppingUtility.IncludeFFCResidual(application.Id, application.Version.Major) &&
                persistedTravelers?.Count > 0 &&
                requestedTravelers?.Count > 0)
            {
                if (
                       ((!_configuration.GetValue<bool>("disable21GFFCToggle") && !continueToChangeTravelers))
                       || _configuration.GetValue<bool>("disable21GFFCToggle")
                 )
                {
                    var ffcTravelers = persistedTravelers.Where(persistedTraveler => persistedTraveler.Value?.FutureFlightCredits?.Count() > 0);
                    if (ffcTravelers?.Count() > 0)
                    {
                        foreach (var trv in ffcTravelers)
                        {
                            var currentTravelrFromRequestedTravelers = requestedTravelers.FirstOrDefault(t => t.PaxID == trv.Value.PaxID);
                            if (currentTravelrFromRequestedTravelers != null && trv.Value.FutureFlightCredits?.Count() > 0 &&
                                !ShopStaticUtility.IsValuesChangedForSameTraveler(currentTravelrFromRequestedTravelers, trv.Value))
                            {
                                currentTravelrFromRequestedTravelers.FutureFlightCredits = trv.Value.FutureFlightCredits;
                                if (!_configuration.GetValue<bool>("disable21GFFCToggle"))
                                {
                                    AssignTravelerTotalFFCNewValueAfterReDeem(currentTravelrFromRequestedTravelers);
                                }
                            }
                        }
                    }
                }
            }
        }
        public List<MOBLMXTraveler> GetLMXTravelersFromFlights(MOBSHOPReservation reservation)
        {
            List<MOBLMXTraveler> lmxTravelers = new List<MOBLMXTraveler>();
            MOBLMXTraveler lmxTraveler;
            if (reservation.LMXFlights != null)
            {
                foreach (MOBCPTraveler traveler in reservation.TravelersCSL)
                {
                    lmxTraveler = new MOBLMXTraveler();
                    lmxTraveler.FirstName = traveler.FirstName;
                    lmxTraveler.LastName = traveler.LastName;
                    if (traveler.MileagePlus != null)
                    {
                        lmxTraveler.IsMPMember = true;
                        lmxTraveler.HasIneligibleSegment = false;
                        lmxTraveler.MPEliteLevelDescription = traveler.MileagePlus.CurrentEliteLevelDescription;
                        List<MOBLMXRow> lmxRows = new List<MOBLMXRow>();
                        double pqdTotal = 0;
                        double pqsTotal = 0;
                        double rdmTotal = 0;
                        double pqmTotal = 0;

                        foreach (United.Mobile.Model.Common.MP2015.LmxFlight flight in reservation.LMXFlights)
                        {
                            bool addedItemAtLoyaltyLevel = false;
                            foreach (United.Mobile.Model.Common.MP2015.LmxLoyaltyTier tier in flight.Products[0].LmxLoyaltyTiers)
                            {
                                if (tier.Level == traveler.MileagePlus.CurrentEliteLevel && !addedItemAtLoyaltyLevel)
                                {
                                    MOBLMXRow row = new MOBLMXRow();
                                    foreach (United.Mobile.Model.Common.MP2015.LmxQuote quote in tier.LmxQuotes)
                                    {
                                        row.Segment = flight.Departure.Code + " - " + flight.Arrival.Code;
                                        switch (quote.Type)
                                        {
                                            case "PQS":
                                                pqsTotal += quote.DblAmount;
                                                row.PQS = quote.DblAmount.ToString();
                                                break;
                                            case "PQD":
                                                pqdTotal += quote.DblAmount;
                                                row.PQD = quote.DblAmount.ToString("C0", CultureInfo.CurrentCulture).Replace(",", "");
                                                break;
                                            case "RDM":
                                                rdmTotal += quote.DblAmount;
                                                //row.AwardMiles = string.Format("{0:#,##0}", quote.DblAmount);
                                                row.AwardMiles = quote.DblAmount.ToString();
                                                break;
                                            case "PQM":
                                                pqmTotal += quote.DblAmount;
                                                row.PQM = quote.DblAmount.ToString();
                                                break;
                                        }
                                    }

                                    if (flight.MarketingCarrier.Code != "UA")
                                    {
                                        row.OperatingCarrierDescription = "Operated by " + flight.MarketingCarrier.Name;
                                    }

                                    if (flight.NonPartnerFlight)
                                    {
                                        lmxTraveler.HasIneligibleSegment = true;
                                        row.IsEligibleSegment = false;
                                        row.IneligibleSegmentMessage = reservation.OaIneligibleToEarnCreditMessage;
                                        //row.OperatingCarrierDescription = "Operated by " + flight.MarketingCarrier.Name;
                                    }
                                    else
                                    {
                                        row.IsEligibleSegment = true;

                                    }
                                    addedItemAtLoyaltyLevel = true;
                                    lmxRows.Add(row);
                                }
                            }
                        }

                        if (lmxRows.Count == 0)
                        {
                            MOBLMXRow row = new MOBLMXRow();
                            lmxTraveler.HasIneligibleSegment = true;
                            row.IsEligibleSegment = false;
                            row.IneligibleSegmentMessage = "Could not retrieve any earning data";
                            lmxRows.Add(row);
                        }
                        lmxTraveler.LMXRows = lmxRows;
                        lmxTraveler.FormattedAwardMileTotal = rdmTotal.ToString() + " miles";
                        lmxTraveler.FormattedPQMTotal = pqmTotal.ToString() + " miles";
                        //lmxTraveler.FormattedPQDTotal = pqdTotal.ToString("C2", CultureInfo.CurrentCulture);
                        lmxTraveler.FormattedPQDTotal = pqdTotal.ToString("C0", CultureInfo.CurrentCulture).Replace(",", "");
                        lmxTraveler.FormattedPQSTotal = pqsTotal.ToString();
                        //lmxTraveler.AwardMileTotal = string.Format("{0:#,##0}", rdmTotal);
                        lmxTraveler.AwardMileTotal = rdmTotal.ToString();
                        lmxTraveler.PQMTotal = pqmTotal.ToString();
                        //lmxTraveler.PQDTotal = string.Format("{0:#,0.00}", pqdTotal);
                        lmxTraveler.PQDTotal = pqdTotal.ToString();
                        lmxTraveler.PQSTotal = pqsTotal.ToString();
                    }
                    else
                    {
                        lmxTraveler.IsMPMember = false;
                        string memberProgram = string.Empty;
                        bool allianceProgramFound = false;
                        if (traveler.AirRewardPrograms != null && traveler.AirRewardPrograms.Count > 0)
                        {
                            foreach (MOBBKLoyaltyProgramProfile program in traveler.AirRewardPrograms)
                            {
                                if (program != null && !string.IsNullOrEmpty(program.ProgramName) && program.CarrierCode != "UA")
                                {
                                    memberProgram = program.ProgramName;
                                    break;
                                }
                            }

                            if (!string.IsNullOrEmpty(memberProgram))
                            {
                                lmxTraveler.MPEliteLevelDescription = memberProgram;
                                lmxTraveler.NonMPMemberMessage = string.Concat("This traveler is earning miles with ", memberProgram);
                                allianceProgramFound = true;
                            }
                        }
                        if (!allianceProgramFound)
                        {
                            lmxTraveler.MPEliteLevelDescription = "Non-member";
                            lmxTraveler.NonMPMemberMessage = _configuration.GetValue<string>("NonMPMemberMessage");

                        }
                    }
                    lmxTravelers.Add(lmxTraveler);
                }
            }
            return lmxTravelers;
        }

        public async Task<(List<MOBCreditCard> savedProfileOwnerCCList, List<MOBAddress> creditCardAddresses, MOBCPPhone mpPhone, MOBEmail mpEmail)> GetProfileOwnerCreditCardList(string sessionID, List<MOBAddress> creditCardAddresses, MOBCPPhone mpPhone, MOBEmail mpEmail, string updatedCCKey)
        {
            #region
            //List<MOBAddress> nonPrimaryCCAddresses = new List<MOBAddress>();
            creditCardAddresses = new List<MOBAddress>();
            List<MOBCreditCard> savedProfileOwnerCCList = null;
            ProfileResponse profilePersist = new ProfileResponse();
            profilePersist = await _travelerUtility.GetCSLProfileResponseInSession(sessionID);
            if (profilePersist.Response != null && profilePersist.Response.Profiles != null)
            {
                #region
                foreach (var traveler in profilePersist.Response.Profiles[0].Travelers)
                {
                    if (traveler.IsProfileOwner)
                    {
                        #region
                        if (traveler.CreditCards != null && traveler.CreditCards.Count > 0)
                        {
                            savedProfileOwnerCCList = new List<MOBCreditCard>();
                            //List<MOBCreditCard> nonPrimaryCC = new List<MOBCreditCard>();
                            foreach (var creditCard in traveler.CreditCards)
                            {
                                if (creditCard.IsPrimary || creditCard.Key == updatedCCKey)
                                {
                                    savedProfileOwnerCCList = new List<MOBCreditCard>();
                                    savedProfileOwnerCCList.Add(creditCard);
                                    if (!String.IsNullOrEmpty(creditCard.AddressKey))
                                    {
                                        foreach (var address in traveler.Addresses)
                                        {
                                            if (address.Key.ToUpper().Trim() == creditCard.AddressKey.ToUpper().Trim())
                                            {
                                                creditCardAddresses = new List<MOBAddress>();
                                                creditCardAddresses.Add(address);
                                                break;
                                            }
                                        }
                                    }
                                    if (creditCard.Key == updatedCCKey || string.IsNullOrEmpty(updatedCCKey))
                                    {
                                        break;
                                    }
                                }
                            }
                            if (savedProfileOwnerCCList == null || savedProfileOwnerCCList.Count == 0) // To check if there is not Primary CC then get the top 1 CC from the List.
                            {
                                savedProfileOwnerCCList = new List<MOBCreditCard>();
                                savedProfileOwnerCCList.Add(traveler.CreditCards[0]);
                                if (traveler.Addresses != null && traveler.Addresses.Count > 0)
                                {
                                    creditCardAddresses = new List<MOBAddress>();
                                    foreach (var address in traveler.Addresses)
                                    {
                                        if (address.Key.ToUpper().Trim() == traveler.CreditCards[0].AddressKey.ToUpper().Trim())
                                        {
                                            creditCardAddresses.Add(address);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        #endregion
                        if (traveler.ReservationPhones != null && traveler.ReservationPhones.Count > 0)
                        {
                            mpPhone = traveler.ReservationPhones[0];
                        }
                        if (traveler.ReservationEmailAddresses != null && traveler.ReservationEmailAddresses.Count > 0)
                        {
                            mpEmail = traveler.ReservationEmailAddresses[0];
                        }
                        break;
                    }
                }
                #endregion
            }
            return (savedProfileOwnerCCList, creditCardAddresses, mpPhone, mpEmail);
            #endregion
        }
        private void AssignTravelerTotalFFCNewValueAfterReDeem(MOBCPTraveler traveler)
        {
            if (traveler.FutureFlightCredits?.Count > 0)
            {
                var sumOfNewValueAfterRedeem = traveler.FutureFlightCredits.Sum(ffc => ffc.NewValueAfterRedeem);
                if (sumOfNewValueAfterRedeem > 0)
                {
                    sumOfNewValueAfterRedeem = Math.Round(sumOfNewValueAfterRedeem, 2, MidpointRounding.AwayFromZero);
                    traveler.TotalFFCNewValueAfterRedeem = (sumOfNewValueAfterRedeem).ToString("C2", CultureInfo.CurrentCulture);
                }
                else
                {
                    traveler.TotalFFCNewValueAfterRedeem = "";
                }
            }
            else
            {
                traveler.TotalFFCNewValueAfterRedeem = "";
            }
        }

        private void AssignTravelerIndividualTotalAmount(List<MOBCPTraveler> travelers, List<DisplayPrice> displayPrices, List<Service.Presentation.ReservationModel.Traveler> cslReservationTravelers, List<Service.Presentation.PriceModel.Price> cslReservationPrices)
        {
            if (travelers?.Count > 0 && displayPrices?.Count > 0)
            {
                foreach (var traveler in travelers)
                {
                    var cslReservationTraveler = cslReservationTravelers.Find(crt => crt.Person.Key == traveler.TravelerNameIndex);
                    if (cslReservationTraveler == null && traveler.TravelerTypeCode == "INF")
                    {
                        cslReservationTraveler = cslReservationTravelers.Find(crt => crt.Person.Type == "INF");
                    }
                    DisplayPrice dPrice = null;
                    if (cslReservationTraveler == null)
                    {
                        dPrice = displayPrices.Find(dp => dp.PaxTypeCode == traveler.TravelerTypeCode);
                    }
                    else
                    {
                        var MultiplePriceTypeExist = displayPrices.Where(dp => (dp.PaxTypeCode == cslReservationTraveler.Person.Type) && (_configuration.GetValue<bool>("EnableCouponsforBooking")
                        ? !string.IsNullOrEmpty(dp.Type) && !dp.Type.ToUpper().Contains("NONDISCOUNTPRICE")
                        : true));
                        if (MultiplePriceTypeExist.Count() > 1)
                        {
                            var cslReservationPrice = cslReservationPrices.Find(crp => crp.PassengerIDs?.Key.IndexOf(traveler.TravelerNameIndex) > -1);
                            traveler.CslReservationPaxTypeCode = cslReservationPrice.PassengerTypeCode;
                            traveler.IndividualTotalAmount = cslReservationPrice.Totals.ToList().Find(t => t.Name.ToUpper() == "GRANDTOTALFORCURRENCY" && t.Currency.Code == "USD").Amount;
                        }
                        else
                        {
                            dPrice = displayPrices.Find(dp => (dp.PaxTypeCode == cslReservationTraveler.Person.Type));
                        }
                        traveler.CslReservationPaxTypeCode = cslReservationTraveler.Person.Type;
                    }
                    if (dPrice != null && dPrice.Amount > 0 && (_configuration.GetValue<bool>("EnableCouponsforBooking") ? true : traveler.IndividualTotalAmount == 0))
                    {
                        var amount = Math.Round((dPrice.Amount / Convert.ToDecimal(dPrice.Count)), 2, MidpointRounding.AwayFromZero);
                        traveler.IndividualTotalAmount = Convert.ToDouble(amount);
                        if (dPrice.SubItems != null)
                        {
                            foreach (var sp in dPrice.SubItems)
                            {
                                traveler.IndividualTotalAmount += Math.Round(Convert.ToDouble(sp.Amount), 2, MidpointRounding.AwayFromZero);
                            }
                        }
                    }
                }
            }
        }
        public async Task<List<United.Mobile.Model.Common.MP2015.LmxFlight>> GetLmxForRTI(string token, string cartId)
        {
            List<United.Mobile.Model.Common.MP2015.LmxFlight> lmxFlights = null;

            if (!string.IsNullOrEmpty(cartId))
            {
                string jsonRequest = "{\"CartId\":\"" + cartId + "\",\"Type\":\"RES\",\"LoadAllTiers\":1}";


                //FlightStatus flightStatus = new FlightStatus();

                //token = flightStatus.GetFLIFOSecurityTokenCSSCall(applicationId, deviceId, transactionId);
                var response = await _lmxInfo.GetLmxRTIInfo<LmxQuoteResponse>(token, jsonRequest, _headers.ContextValues.SessionId).ConfigureAwait(false);

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
                                lmxFlight.Arrival = new MOBAirport();
                                lmxFlight.Arrival.Code = flight.Destination;
                                lmxFlight.Departure = new MOBAirport();
                                lmxFlight.Departure.Code = flight.Origin;
                                lmxFlight.FlightNumber = flight.FlightNumber;
                                lmxFlight.MarketingCarrier = new MOBAirline();
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
                                        //lmxProduct.BookingCode = product.BookingCode;
                                        //lmxProduct.Description = product.Description;
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
                        }
                    }
                    //else
                    //{
                    //    if (lmxQuoteResponse != null && lmxQuoteResponse.Errors.Count > 0)
                    //    {
                    //        throw new Exception(string.Join(" , ", lmxQuoteResponse.Errors.Select(o => o.Message)));
                    //    }
                    //    else
                    //    {
                    //        throw new Exception(jsonResponse);
                    //    }
                    //}
                }
            }

            return lmxFlights;
        }

        private List<MOBCPMileagePlus> GetMpListFromCSLResponse(United.Services.Customer.Common.MileagePlusesResponse mpResponse)
        {
            var mpList = new List<MOBCPMileagePlus>();

            if (mpResponse != null && mpResponse.MileagePlusList != null)
            {
                foreach (var mileagePlus in mpResponse.MileagePlusList)
                {
                    var mp = new MOBCPMileagePlus();
                    // mp.MpCustomerId = mileagePlus.cu
                    mp.AccountBalance = mileagePlus.AccountBalance;
                    mp.CurrentEliteLevel = mileagePlus.CurrentEliteLevel;
                    mp.CurrentEliteLevelDescription = mileagePlus.CurrentEliteLevelDescription;
                    mp.ActiveStatusCode = mileagePlus.ActiveStatusCode;
                    mp.ActiveStatusDescription = mileagePlus.ActiveStatusDescription;
                    mp.AllianceEliteLevel = mileagePlus.AllianceEliteLevel;
                    mp.ClosedStatusCode = mileagePlus.ClosedStatusCode;
                    mp.ClosedStatusDescription = mileagePlus.ClosedStatusDescription;
                    mp.CurrentYearMoneySpent = mileagePlus.CurrentYearMoneySpent;
                    mp.EliteMileageBalance = mileagePlus.EliteMileageBalance;
                    mp.EliteSegmentBalance = (int)mileagePlus.EliteSegmentBalance;
                    //mp.EliteSegmentDecimalPlaceValue = mileagePlus.elites
                    mp.FutureEliteDescription = mileagePlus.FutureEliteLevelDescription;
                    mp.FutureEliteLevel = mileagePlus.FutureEliteLevel;
                    mp.InstantEliteExpirationDate = mileagePlus.InstantEliteExpDate.ToString();
                    mp.IsCEO = mileagePlus.IsCEO;
                    mp.IsClosedPermanently = mileagePlus.IsClosedPermanently;
                    mp.IsClosedTemporarily = mileagePlus.IsClosedTemporarily;
                    mp.IsCurrentTrialEliteMember = mileagePlus.IsCurrentTrialEliteMember;
                    mp.IsFlexPqm = mileagePlus.IsFlexPQM;
                    mp.IsInfiniteElite = mileagePlus.IsInfiniteElite;
                    mp.IsLifetimeCompanion = mileagePlus.IsLifetimeCompanion;
                    mp.IsLockedOut = mileagePlus.IsMergePending;
                    mp.IsMergePending = mileagePlus.IsMergePending;
                    mp.IsPresidentialPlus = mileagePlus.IsPresidentialPlus;
                    // mp.IsUnitedClubMember = mileagePlus.IsPClubMember;
                    mp.LastActivityDate = mileagePlus.LastActivityDate;
                    mp.LastExpiredMile = mileagePlus.LastExpiredMile;
                    mp.LastFlightDate = mileagePlus.LastFlightDate;
                    mp.LastStatementBalance = mileagePlus.LastStatementBalance;
                    mp.LastStatementDate = mileagePlus.LastStatementDate.ToString();
                    mp.LifetimeEliteLevel = mileagePlus.LifetimeEliteLevel;
                    mp.LifetimeEliteMileageBalance = mileagePlus.LifetimeEliteMileageBalance;
                    mp.MileageExpirationDate = mileagePlus.MileageExpirationDate;
                    mp.MileagePlusPin = mileagePlus.MileagePlusPIN;
                    mp.NextYearEliteLevel = mileagePlus.NextYearEliteLevel;
                    mp.NextYearEliteLevelDescription = mileagePlus.NextYearEliteLevelDescription;
                    mp.PriorUnitedAccountNumber = mileagePlus.PriorUnitedAccountNumber;
                    // mp.StarAllianceEliteLevel = mileagePlus.SkyTeamEliteLevelCode;



                    mp.MileagePlusId = mileagePlus.MileagePlusId;

                    mpList.Add(mp);
                }
            }
            return mpList;
        }

        public async Task<PlacePass> GetPlacePass(string destinationcode, string tripType, string sessionId, int appID, string appVersion, string deviceId, string logAction, string placepasscampain)
        {
            PlacePass placepass = new PlacePass();
            string utm_source = "utm_source=United";
            string utm_medium = "utm_medium=Web";
            string utm_campain = placepasscampain;
            string utm = utm_source + "&" + utm_medium + "&" + utm_campain;
            if (!(tripType.Equals("MD") || tripType.Equals("MULTI_CITY")))
            {
                try
                {
                    string subkey = !string.IsNullOrEmpty(_configuration.GetValue<string>("PlacePassSubKey")) ? _configuration.GetValue<string>("PlacePassSubKey") : string.Empty;
                    string partnerid = !string.IsNullOrEmpty(_configuration.GetValue<string>("PlacePassPartnerId")) ? _configuration.GetValue<string>("PlacePassPartnerId") : string.Empty;
                    string env = !string.IsNullOrEmpty(_configuration.GetValue<string>("PlacePassEnv")) ? _configuration.GetValue<string>("PlacePassEnv") : string.Empty;
                    string path = destinationcode;

                    string token = await _dPService.GetAnonymousToken(appID, deviceId, _configuration);
                    var response = await _placePassService.GetPlacePass<AZUREPlacePass>(token, path, sessionId).ConfigureAwait(false);

                    // AZUREPlacePass placepassresponse = JsonConvert.DeserializeObject<AZUREPlacePass>(response);
                    if (response.response != null)
                    {
                        if (response.response != null && !string.IsNullOrEmpty(response.response.NearestLocation.city) && response.response.ValidLocation)
                        {
                            //&& !string.IsNullOrEmpty(placepassresponse.NearestLocation.city) && !string.IsNullOrEmpty(placepassresponse.MobileImageUrl)

                            placepass.PlacePassImageSrc = !string.IsNullOrEmpty(response.response.MobileImageUrl) ? response.response.MobileImageUrl.ToString() : (!string.IsNullOrEmpty(_configuration.GetValue<string>("PlacepassGenericImageUrl")) ? _configuration.GetValue<string>("PlacepassGenericImageUrl") : "");
                            placepass.PlacePassUrl = !string.IsNullOrEmpty(response.response.PropertyDomainUrl) ? response.response.PropertyDomainUrl.ToString() + "?" + utm : "https://united.placepass.com" + "?" + utm;
                            //placepass.OfferDescription = (!string.IsNullOrEmpty(_configuration.GetValue<string>("PlacePassCitySpecificOfferDescription"]) ? _configuration.GetValue<string>("PlacePassCitySpecificOfferDescription"].ToString() + " " + placepassresponse.NearestLocation.city.ToString() : "Find top things to do in your destination");
                            placepass.TxtPoweredBy = !string.IsNullOrEmpty(_configuration.GetValue<string>("PlacePassPoweredBy")) ? _configuration.GetValue<string>("PlacePassPoweredBy") : "Powered by";
                            placepass.TxtPlacepass = !string.IsNullOrEmpty(_configuration.GetValue<string>("PlacePassFooter")) ? _configuration.GetValue<string>("PlacePassFooter") : "PLACEPASS";
                            if (string.IsNullOrEmpty(response.response.MobileImageUrl))
                            {
                                placepass.OfferDescription = !string.IsNullOrEmpty(_configuration.GetValue<string>("PlacePassGenericDescription")) ? _configuration.GetValue<string>("PlacePassGenericDescription") : string.Empty;
                            }
                            else if (!string.IsNullOrEmpty(response.response.MobileImageUrl))
                            {
                                placepass.OfferDescription = (!string.IsNullOrEmpty(_configuration.GetValue<string>("PlacePassCitySpecificOfferDescription")) ? _configuration.GetValue<string>("PlacePassCitySpecificOfferDescription") + " " + response.response.NearestLocation.city.ToString() : _configuration.GetValue<string>("PlacePassGenericDescription"));

                            }
                        }
                        else
                        {
                            placepass = await _travelerUtility.GetGenericPlacePass(destinationcode, tripType, sessionId, appID, appVersion, deviceId, "GetGenericPlacePass", utm_campain);
                        }
                    }
                    else
                    {
                        placepass = await _travelerUtility.GetGenericPlacePass(destinationcode, tripType, sessionId, appID, appVersion, deviceId, "GetGenericPlacePass", utm_campain);
                    }
                }
                catch (System.Net.WebException exx)
                {
                    var errorResponse = new StreamReader(exx.Response.GetResponseStream()).ReadToEnd();
                    //  logEntries.Add(LogEntry.GetLogEntry(sessionId, logAction, "Exception", appID, appVersion, deviceId, errorResponse));
                    _logger.LogError("GetPlacePass {@WebException}", exx.Response.ToString());
                    placepass = new PlacePass();
                    placepass = await _travelerUtility.GetGenericPlacePass(destinationcode, tripType, sessionId, appID, appVersion, deviceId, logAction, utm_campain);

                    //}
                }
                catch (Exception ex)
                {
                    MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                    //logEntries.Add(LogEntry.GetLogEntry(sessionId, logAction, "Exception", appID, appVersion, deviceId, exceptionWrapper));
                    _logger.LogError("GetPlacePass {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));

                    placepass = new PlacePass();
                    placepass = await _travelerUtility.GetGenericPlacePass(destinationcode, tripType, sessionId, appID, appVersion, deviceId, logAction, utm_campain);

                    //}
                }
            }
            else
            {
                placepass = await _travelerUtility.GetGenericPlacePass(destinationcode, tripType, sessionId, appID, appVersion, deviceId, "GetGenericPlacePass", utm_campain);

                _logger.LogInformation("logAction PlacepassGenericResponseFromService {@Response}", JsonConvert.SerializeObject(placepass));
            }
            return placepass;
        }

        public async Task<(RegisterTravelersRequest registerTravelerRequest, MOBRegisterTravelersRequest request)> GetRegisterTravelerRequest(MOBRegisterTravelersRequest request, bool isRequireNationalityAndResidence, Reservation bookingPathReservation)
        {
            RegisterTravelersRequest registerTravelerRequest = new RegisterTravelersRequest();
            registerTravelerRequest.CartId = request.CartId;
            if (!_configuration.GetValue<bool>("DisablePassingWorkFlowType"))
            {
                registerTravelerRequest.WorkFlowType = _shoppingUtility.GetWorkFlowType(request.Flow);
            }
            if (request.ProfileOwner != null)
            {
                registerTravelerRequest.loyaltyPerson = new Service.Presentation.PersonModel.LoyaltyPerson();
                registerTravelerRequest.loyaltyPerson.Surname = request.ProfileOwner.LastName;
                registerTravelerRequest.loyaltyPerson.GivenName = request.ProfileOwner.FirstName;
                registerTravelerRequest.loyaltyPerson.MiddleName = request.ProfileOwner.MiddleName;
                registerTravelerRequest.loyaltyPerson.LoyaltyProgramMemberID = request.ProfileOwner.MileagePlus.MileagePlusId;
                registerTravelerRequest.loyaltyPerson.LoyaltyProgramCarrierCode = "UA";
                #region
                if (request.ProfileOwner.MileagePlus.CurrentEliteLevel == 8) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                {
                    registerTravelerRequest.loyaltyPerson.LoyaltyProgramMemberTierLevel = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.StarSilver;
                }
                else if (request.ProfileOwner.MileagePlus.CurrentEliteLevel == 7) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                {
                    registerTravelerRequest.loyaltyPerson.LoyaltyProgramMemberTierLevel = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.StarGold;
                }
                else if (request.ProfileOwner.MileagePlus.CurrentEliteLevel == 6) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                {
                    registerTravelerRequest.loyaltyPerson.LoyaltyProgramMemberTierLevel = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.ChairmansCircle;
                }
                else if (request.ProfileOwner.MileagePlus.CurrentEliteLevel == 5) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                {
                    registerTravelerRequest.loyaltyPerson.LoyaltyProgramMemberTierLevel = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.GlobalServices;
                }
                else if (request.ProfileOwner.MileagePlus.CurrentEliteLevel == 4) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                {
                    registerTravelerRequest.loyaltyPerson.LoyaltyProgramMemberTierLevel = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.Premier1K;
                }
                else if (request.ProfileOwner.MileagePlus.CurrentEliteLevel == 3) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                {
                    registerTravelerRequest.loyaltyPerson.LoyaltyProgramMemberTierLevel = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.PremierPlatinum;
                }
                else if (request.ProfileOwner.MileagePlus.CurrentEliteLevel == 2) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                {
                    registerTravelerRequest.loyaltyPerson.LoyaltyProgramMemberTierLevel = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.PremierGold;
                }
                else if (request.ProfileOwner.MileagePlus.CurrentEliteLevel == 1) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                {
                    registerTravelerRequest.loyaltyPerson.LoyaltyProgramMemberTierLevel = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.PremierSilver;
                }
                else if (request.ProfileOwner.MileagePlus.CurrentEliteLevel == 0) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                {
                    registerTravelerRequest.loyaltyPerson.LoyaltyProgramMemberTierLevel = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.GeneralMember;
                }
                #endregion
                registerTravelerRequest.loyaltyPerson.AccountBalances = new System.Collections.ObjectModel.Collection<Service.Presentation.CommonModel.LoyaltyAccountBalance>();
                Service.Presentation.CommonModel.LoyaltyAccountBalance loyaltyBalance = new Service.Presentation.CommonModel.LoyaltyAccountBalance();
                loyaltyBalance.Balance = request.ProfileOwner.MileagePlus.AccountBalance;
                loyaltyBalance.BalanceType = Service.Presentation.CommonEnumModel.LoyaltyAccountBalanceType.MilesBalance;
                registerTravelerRequest.loyaltyPerson.AccountBalances.Add(loyaltyBalance);
            }
            if (request.Travelers != null)
            {
                registerTravelerRequest.FlightTravelers = new List<FlightTraveler>();
                int travelerKeyIndex = 0;
                List<MOBCPTraveler> cloneTravelerList = new List<MOBCPTraveler>();

                //2
                List<string[]> Countries = _travelerUtility.LoadCountries();

                foreach (MOBCPTraveler traveler in request.Travelers)
                {
                    #region
                    FlightTraveler flightTraveler = new FlightTraveler();
                    #region Get flightTraveler.Traveler.Person details
                    travelerKeyIndex++;

                    flightTraveler.Traveler = new Service.Presentation.ReservationModel.Traveler();

                    //Emp20 booking needs empId
                    Session session = new Session();
                    session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string> { request.SessionId, session.ObjectName }).ConfigureAwait(false);
                    if (session != null && !string.IsNullOrEmpty(session.EmployeeId))
                    {
                        flightTraveler.Traveler.EmployeeProfile = new Service.Presentation.CommonModel.EmployeeProfile();
                        flightTraveler.Traveler.EmployeeProfile.EmployeeID = session.EmployeeId;
                    }

                    flightTraveler.Traveler.Person = new Service.Presentation.PersonModel.Person();
                    flightTraveler.Traveler.Person.Key = travelerKeyIndex.ToString() + ".1";
                    flightTraveler.TravelerNameIndex = travelerKeyIndex.ToString() + ".1";

                    traveler.TravelerNameIndex = flightTraveler.TravelerNameIndex;

                    bool isExtraSeatFeatureEnabled = _travelerUtility.IsExtraSeatFeatureEnabled(request.Application.Id, request.Application.Version.Major, session.CatalogItems);

                    #region Special Needs

                    if (_shoppingUtility.EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
                    {
                        if (traveler.SelectedSpecialNeeds != null && traveler.SelectedSpecialNeeds.Any())
                        {
                            flightTraveler.SpecialServiceRequests = new List<Service.Presentation.CommonModel.Service>();
                            foreach (var specialNeed in traveler.SelectedSpecialNeeds.Where(x => x != null))
                            {
                                var sr = (specialNeed.SubOptions != null && specialNeed.SubOptions.Any()) ? specialNeed.SubOptions[0] : specialNeed;

                                var specialRequest = new Service.Presentation.CommonModel.Service
                                {
                                    Key = "SSR",
                                    Code = isExtraSeatFeatureEnabled ? _travelerUtility.SpecialServiceRequestCode(sr.Code) : sr.Code,
                                    TravelerNameIndex = flightTraveler.TravelerNameIndex,
                                    NumberInParty = 1,
                                    Description = SpecialNeedThatNeedRegisterDescription(sr.Code) ? sr.RegisterServiceDescription : null // Per PNR management team, only a few of special needs will need to be passed in with description
                                };

                                flightTraveler.SpecialServiceRequests.Add(specialRequest);
                            }
                        }
                    }

                    #endregion

                    MOBCPTraveler extraSeatNames = new MOBCPTraveler();
                    string ssrCode = string.Empty;
                    bool isExtraSeatEnabled = isExtraSeatFeatureEnabled && traveler.IsExtraSeat;
                    if (isExtraSeatEnabled && traveler?.ExtraSeatData?.SelectedPaxId != null)
                    {
                        extraSeatNames = request.Travelers.FirstOrDefault(a => a?.PaxID != null && a?.PaxID == traveler?.ExtraSeatData?.SelectedPaxId);

                        ssrCode = (traveler.SelectedSpecialNeeds != null && traveler.SelectedSpecialNeeds.Count() > 0 && !string.IsNullOrEmpty(traveler.SelectedSpecialNeeds[0].Code))
                                        ? traveler.SelectedSpecialNeeds[0].Code
                                        : string.Empty;
                    }

                    ////Commented and Added By Santosh as part of Bug 104740:Error message is displayed during first time check-in for the Guest pax travelling to Cuba. -Santosh
                    //flightTraveler.Traveler.Person.Surname = IsAlphabets(traveler.LastName);
                    flightTraveler.Traveler.Person.Surname = isExtraSeatEnabled ? extraSeatNames?.LastName : traveler.LastName;
                    //flightTraveler.Traveler.Person.GivenName = IsAlphabets(traveler.FirstName);
                    flightTraveler.Traveler.Person.GivenName = isExtraSeatEnabled ? ssrCode + extraSeatNames?.FirstName : traveler.FirstName;
                    //flightTraveler.Traveler.Person.MiddleName = IsAlphabets(traveler.MiddleName);
                    flightTraveler.Traveler.Person.MiddleName = traveler.MiddleName;
                    flightTraveler.Traveler.Person.Suffix = IsAlphabets(traveler.Suffix); //traveler.Suffix != null?traveler.Suffix.Replace(".", ""):"";
                                                                                          //Commenting Title as per Mahi if we pass title it is appending to name and failing when trying to refund EPU when purchasing PCU
                                                                                          //flightTraveler.Traveler.Person.Title = IsAlphabets(traveler.Title);   //traveler.Title != null?traveler.Title.Replace(".", ""):""; // As per Moni RegisterFormsOfPayment Call failing when passing dot with title value.
                    flightTraveler.Traveler.Person.Sex = traveler.GenderCode;

                    #region Nationality And Country Of Residence - Rajesh Settipalli
                    if (((_shoppingUtility.EnableUnfinishedBookings(request) && isRequireNationalityAndResidence) ||
                               _shoppingUtility.EnableUnfinishedBookings(request) == false
                          )
                        && _shoppingUtility.IsEnabledNationalityAndResidence(false, request.Application.Id, request.Application.Version.Major))
                    {
                        if (flightTraveler.Traveler.Person != null && flightTraveler.Traveler.Person.CountryOfResidence == null)
                        {
                            flightTraveler.Traveler.Person.CountryOfResidence = new United.Service.Presentation.CommonModel.Country();
                        }
                        flightTraveler.Traveler.Person.CountryOfResidence.CountryCode = traveler.CountryOfResidence;
                        if (flightTraveler.Traveler.Person.Nationality == null)
                        {
                            flightTraveler.Traveler.Person.Nationality = new Collection<United.Service.Presentation.CommonModel.Country>();
                        }

                        flightTraveler.Traveler.Person.Nationality.Add(new United.Service.Presentation.CommonModel.Country() { CountryCode = traveler.Nationality });
                    }
                    #endregion Nationality And Country Of Residence

                    if (!String.IsNullOrEmpty(traveler.BirthDate))
                    {
                        flightTraveler.Traveler.Person.DateOfBirth = traveler.BirthDate;
                    }
                    else if (traveler.SecureTravelers != null && traveler.SecureTravelers.Count > 0)
                    {
                        flightTraveler.Traveler.Person.DateOfBirth = traveler.SecureTravelers[0].BirthDate;
                    }
                    flightTraveler.Traveler.Person.Type = traveler.TravelerTypeCode;
                    flightTraveler.Traveler.Person.InfantIndicator = "false";  //**//--> need to follow up how this value should be set?
                    if (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange) && bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.TravelerTypes != null
                        && bookingPathReservation.ShopReservationInfo2.TravelerTypes.Count > 0)
                    {
                        string firstLOFDepartDate = bookingPathReservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTime;
                        if (!string.IsNullOrEmpty(traveler.BirthDate))
                        {
                            if (TopHelper.GetAgeByDOB(traveler.BirthDate, firstLOFDepartDate) < 2)
                            {
                                if (traveler.TravelerTypeCode.ToUpper().Equals("INF"))
                                {
                                    flightTraveler.Traveler.Person.InfantIndicator = "lapinfant";//not to get auto seat assignment at check out for infant in lap after registertravelers call
                                }
                                else
                                {
                                    flightTraveler.Traveler.Person.InfantIndicator = "Infant";
                                }
                            }
                        }
                    }
                    #endregion
                    #region Get flightTraveler.Traveler.Person.Documents details
                    //**//--> check what the person.document details should be are these same as person details or different
                    flightTraveler.Traveler.Person.Documents = new System.Collections.ObjectModel.Collection<Service.Presentation.PersonModel.Document>();
                    Service.Presentation.PersonModel.Document personDocument = new Service.Presentation.PersonModel.Document();
                    personDocument.DateOfBirth = flightTraveler.Traveler.Person.DateOfBirth;
                    //Commented and Added By Santosh as part of Bug 104740:Error message is displayed during first time check-in for the Guest pax travelling to Cuba. -Santosh
                    //personDocument.GivenName = IsAlphabets(traveler.FirstName);
                    //personDocument.Surname = IsAlphabets(traveler.LastName);
                    personDocument.GivenName = traveler.FirstName;
                    personDocument.Surname = traveler.LastName;

                    personDocument.Suffix = IsAlphabets(traveler.Suffix);//traveler.Suffix != null ? traveler.Suffix.Replace(".", "") : "";
                    personDocument.Sex = traveler.GenderCode;
                    personDocument.Type = Service.Presentation.CommonEnumModel.DocumentType.Reserved; //**//--> As per Babu email reply dated 8/4/2014 its a default value reserved.
                                                                                                      //Commented and Added By Santosh as part of Bug 104740:Error message is displayed during first time check-in for the Guest pax travelling to Cuba. -Santosh
                                                                                                      //personDocument.MiddleName = IsAlphabets(traveler.MiddleName);
                    personDocument.MiddleName = traveler.MiddleName;
                    personDocument.KnownTravelerNumber = traveler.KnownTravelerNumber;
                    personDocument.RedressNumber = traveler.RedressNumber;
                    personDocument.CanadianTravelNumber = traveler.CanadianTravelerNumber;
                    flightTraveler.Traveler.Person.Documents.Add(personDocument);
                    #endregion
                    flightTraveler.Traveler.Person.Contact = new Service.Presentation.PersonModel.Contact();
                    if (traveler.EmailAddresses != null)
                    {
                        #region Get flightTraveler.Traveler.Person.Contact.Emails 
                        flightTraveler.Traveler.Person.Contact.Emails = new System.Collections.ObjectModel.Collection<Service.Presentation.CommonModel.EmailAddress>();
                        foreach (MOBEmail mobEmail in traveler.EmailAddresses)
                        {
                            Service.Presentation.CommonModel.EmailAddress emailAddress = new Service.Presentation.CommonModel.EmailAddress();
                            emailAddress.Address = mobEmail.EmailAddress;
                            flightTraveler.Traveler.Person.Contact.Emails.Add(emailAddress);
                            ////if (mobEmail.IsDayOfTravel) // As at get profile we populate only one email if its day of travel contact and when its a guest traveler we need to populate the email entered at edit traveler so no need to check this condition as alway this email list will have one email.
                            ////{
                            //    flightTraveler.Traveler.Person.Contact.Emails = new System.Collections.ObjectModel.Collection<Service.Presentation.CommonModel.EmailAddress>();
                            //    flightTraveler.Traveler.Person.Contact.Emails.Add(emailAddress);
                            //    //break;
                            ////}
                        }
                        #endregion
                    }
                    if (traveler.Phones != null && _configuration.GetValue<string>("DonotSendPhonestoRegisterTravelerToCSL") == null)
                    {
                        #region Get flightTraveler.Traveler.Person.Contact.PhoneNumbers
                        flightTraveler.Traveler.Person.Contact.PhoneNumbers = new System.Collections.ObjectModel.Collection<Service.Presentation.CommonModel.Telephone>();
                        foreach (MOBCPPhone mobcoPhone in traveler.Phones)
                        {
                            Service.Presentation.CommonModel.Telephone telephone = new Service.Presentation.CommonModel.Telephone();
                            telephone.Description = mobcoPhone.ChannelTypeCode;
                            // 3                      
                            telephone.CountryAccessCode = Regex.Replace(_travelerUtility.GetAccessCode(mobcoPhone.CountryCode), @"\s", "");
                            telephone.AreaCityCode = mobcoPhone.AreaNumber;
                            telephone.PhoneNumber = mobcoPhone.PhoneNumber;
                            if (mobcoPhone.CountryCode != telephone.CountryAccessCode)
                            {
                                telephone.PhoneNumber = telephone.CountryAccessCode + mobcoPhone.PhoneNumber;
                            }
                            telephone.DisplaySequence = mobcoPhone.ChannelTypeSeqNumber; //**//-->  Need to check what value should be display sequence?
                            flightTraveler.Traveler.Person.Contact.PhoneNumbers.Add(telephone);
                            ////if (mobcoPhone.IsDayOfTravel)// As at get profile we populate only one phone if its day of travel contact and when its a guest traveler we need to populate the phone entered at edit traveler so no need to check this condition as alway this phone list will have one phone.
                            ////{
                            //    flightTraveler.Traveler.Person.Contact.PhoneNumbers = new System.Collections.ObjectModel.Collection<Service.Presentation.CommonModel.Telephone>();
                            //    flightTraveler.Traveler.Person.Contact.PhoneNumbers.Add(telephone);
                            //    //break;
                            ////}
                        }
                        #endregion
                    }
                    flightTraveler.Traveler.LoyaltyProgramProfile = new Service.Presentation.CommonModel.LoyaltyProgramProfile();
                    if (!traveler.isMPNameMisMatch && traveler.MileagePlus != null)
                    {
                        #region Get flightTraveler.Traveler.LoyaltyProgramProfile
                        flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramCarrierCode = "UA";
                        flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberID = traveler.MileagePlus.MileagePlusId;
                        flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierLevel = traveler.MileagePlus.CurrentEliteLevel.ToString();
                        if (traveler.MileagePlus.CurrentEliteLevel == 8) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                        {
                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.StarSilver;
                        }
                        else if (traveler.MileagePlus.CurrentEliteLevel == 7) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                        {
                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.StarGold;
                        }
                        else if (traveler.MileagePlus.CurrentEliteLevel == 6) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                        {
                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.ChairmansCircle;
                        }
                        else if (traveler.MileagePlus.CurrentEliteLevel == 5) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                        {
                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.GlobalServices;
                        }
                        else if (traveler.MileagePlus.CurrentEliteLevel == 4) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                        {
                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.Premier1K;
                        }
                        else if (traveler.MileagePlus.CurrentEliteLevel == 3) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                        {
                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.PremierPlatinum;
                        }
                        else if (traveler.MileagePlus.CurrentEliteLevel == 2) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                        {
                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.PremierGold;
                        }
                        else if (traveler.MileagePlus.CurrentEliteLevel == 1) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                        {
                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.PremierSilver;
                        }
                        else if (traveler.MileagePlus.CurrentEliteLevel == 0) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                        {
                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.GeneralMember;
                        }
                        if (traveler.MileagePlus.StarAllianceEliteLevel == 1)//**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                        {
                            flightTraveler.Traveler.LoyaltyProgramProfile.StarEliteLevelDescription = Service.Presentation.CommonEnumModel.StarEliteTierLevel.StarSilver;
                        }
                        else if (traveler.MileagePlus.StarAllianceEliteLevel == 0)//**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                        {
                            flightTraveler.Traveler.LoyaltyProgramProfile.StarEliteLevelDescription = Service.Presentation.CommonEnumModel.StarEliteTierLevel.StarGold;
                        }
                        flightTraveler.Traveler.LoyaltyProgramProfile.MilesBalance = traveler.MileagePlus.AccountBalance;
                        #endregion
                    }
                    else if (traveler.AirRewardPrograms != null)
                    {
                        foreach (MOBBKLoyaltyProgramProfile airRewardProgram in traveler.AirRewardPrograms)
                        {
                            //if (airRewardProgram.CarrierCode.ToUpper().Trim() == "UA")
                            if (airRewardProgram != null && airRewardProgram.ProgramId == "7")
                            {
                                if (!traveler.isMPNameMisMatch)
                                {
                                    #region Get flightTraveler.Traveler.LoyaltyProgramProfile
                                    flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramCarrierCode = "UA";
                                    flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberID = airRewardProgram.MemberId;
                                    flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierLevel = airRewardProgram.MPEliteLevel.ToString();
                                    if (airRewardProgram.MPEliteLevel != null)
                                    {
                                        if (airRewardProgram.MPEliteLevel == 8) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                                        {
                                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.StarSilver;
                                        }
                                        else if (airRewardProgram.MPEliteLevel == 7) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                                        {
                                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.StarGold;
                                        }
                                        else if (airRewardProgram.MPEliteLevel == 6) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                                        {
                                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.ChairmansCircle;
                                        }
                                        else if (airRewardProgram.MPEliteLevel == 5) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                                        {
                                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.GlobalServices;
                                        }
                                        else if (airRewardProgram.MPEliteLevel == 4) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                                        {
                                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.Premier1K;
                                        }
                                        else if (airRewardProgram.MPEliteLevel == 3) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                                        {
                                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.PremierPlatinum;
                                        }
                                        else if (airRewardProgram.MPEliteLevel == 2) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                                        {
                                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.PremierGold;
                                        }
                                        else if (airRewardProgram.MPEliteLevel == 1) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                                        {
                                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.PremierSilver;
                                        }
                                        else if (airRewardProgram.MPEliteLevel == 0) //**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                                        {
                                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.GeneralMember;
                                        }
                                    }
                                    else
                                    {
                                        flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = new Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel();
                                        flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription = Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel.GeneralMember;
                                    }
                                    if (airRewardProgram.StarEliteLevel != null)
                                    {
                                        if (airRewardProgram.StarEliteLevel.Trim() == "1")//**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                                        {
                                            flightTraveler.Traveler.LoyaltyProgramProfile.StarEliteLevelDescription = Service.Presentation.CommonEnumModel.StarEliteTierLevel.StarSilver;
                                        }
                                        else if (airRewardProgram.StarEliteLevel.Trim() == "0")//**//--> Looks like I got it from Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel defintion but have to verify with CSL profile services and register traveler guys
                                        {
                                            flightTraveler.Traveler.LoyaltyProgramProfile.StarEliteLevelDescription = Service.Presentation.CommonEnumModel.StarEliteTierLevel.StarGold;
                                        }
                                    }
                                    #endregion
                                }
                                break;
                            }
                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramCarrierCode = airRewardProgram.CarrierCode;
                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberID = airRewardProgram.MemberId;
                            flightTraveler.Traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierLevel = airRewardProgram.MPEliteLevel.ToString();
                        }
                    }
                    await AssignCSLCubaTravelReasonToSpecialRequest(flightTraveler, traveler.CubaTravelReason, request.SessionId);
                    registerTravelerRequest.FlightTravelers.Add(flightTraveler);
                    #endregion
                    cloneTravelerList.Add(traveler);
                }
                request.Travelers = cloneTravelerList;
                registerTravelerRequest.Reservation = new Service.Presentation.ReservationModel.Reservation();
                registerTravelerRequest.Reservation.NumberInParty = travelerKeyIndex;
            }
            if (_configuration.GetValue<bool>("EnableOmniChannelCartMVP1"))
            {
                registerTravelerRequest.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                registerTravelerRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic { Code = "OMNICHANNELCART", Value = "true" });

                registerTravelerRequest.DeviceID = request.DeviceId;
            }
            return (registerTravelerRequest, request);
        }
        public async Task AssignCSLCubaTravelReasonToSpecialRequest(FlightTraveler flightTraveler, MOBCPCubaSSR cubaTravelReason, string sessionid)
        {
            if (cubaTravelReason != null &&
                !string.IsNullOrEmpty(cubaTravelReason.Vanity))
            {
                string vanity = cubaTravelReason.Vanity + (string.IsNullOrEmpty(cubaTravelReason.InputValue) ? "" : "/" + cubaTravelReason.InputValue);

                if (flightTraveler != null &&
                !IsTravelReasonExist(flightTraveler.SpecialServiceRequests, cubaTravelReason.Vanity))
                {
                    if (flightTraveler.SpecialServiceRequests == null)
                        flightTraveler.SpecialServiceRequests = new List<Service.Presentation.CommonModel.Service>();

                    var service = new Service.Presentation.CommonModel.Service();
                    service.Key = "SSR";
                    service.Code = "RFTV";
                    service.Description = vanity;
                    service.TravelerNameIndex = flightTraveler.TravelerNameIndex;
                    if (_configuration.GetValue<bool>("BugFixToggleFor17M"))
                    {
                        service.SegmentNumber = await GetCubaSegmentNumbersFromPersistReservation(sessionid);
                    }
                    flightTraveler.SpecialServiceRequests.Add(service);
                }
            }
        }
        private async Task<Collection<int>> GetCubaSegmentNumbersFromPersistReservation(string sessionid)
        {
            Collection<int> segmentNumbers = null;
            if (_configuration.GetValue<bool>("BugFixToggleFor17M") && !string.IsNullOrEmpty(sessionid))
            {
                Reservation bookingPathReservation = new Reservation();
                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionid, bookingPathReservation.ObjectName, new List<string> { sessionid, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                bool isCubaFight = false;
                string CubaAirports = _configuration.GetValue<string>("CubaAirports");
                List<string> CubaAirportList = CubaAirports.Split('|').ToList();

                if (bookingPathReservation != null && bookingPathReservation.Trips != null)
                {
                    segmentNumbers = new Collection<int>();
                    int segmentNumber = 0;
                    foreach (MOBSHOPTrip trip in bookingPathReservation.Trips)
                    {
                        foreach (var flight in trip.FlattenedFlights)
                        {
                            foreach (var stopFlights in flight.Flights)
                            {
                                segmentNumber++;
                                isCubaFight = isCubaAirportCodeExist(stopFlights.Origin, stopFlights.Destination, CubaAirportList);
                                if (isCubaFight)
                                {
                                    segmentNumbers.Add(segmentNumber);
                                }
                            }
                        }
                    }
                }
            }
            return segmentNumbers;
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
        private bool IsTravelReasonExist(List<Service.Presentation.CommonModel.Service> specialServiceRequest, string vanity)
        {
            return (specialServiceRequest != null && specialServiceRequest.Exists(p => p.Description == vanity && p.Code == "RFTV" && p.Key == "SSR"));
        }
        private string IsAlphabets(string inputString)
        {
            if (inputString != null)
            {
                System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex("^[a-zA-Z]+$");
                if (r.IsMatch(inputString))
                {
                    return inputString;
                }
                else
                {
                    string str = System.Text.RegularExpressions.Regex.Replace(inputString, "[^a-zA-Z]", "");
                    return str;
                }
            }
            return string.Empty;
        }
        private bool SpecialNeedThatNeedRegisterDescription(string specialNeedCode)
        {
            if (string.IsNullOrWhiteSpace(specialNeedCode))
                return false;

            // we don't put a safe guard around this because we want this to fail if the list doesn't exist
            var listOfSpecialNeedsThatNeedRegisterDesc = new HashSet<string>(_configuration.GetValue<string>("SpecialNeedsThatNeedRegisterDescriptions").Split('|'));

            return listOfSpecialNeedsThatNeedRegisterDesc != null && listOfSpecialNeedsThatNeedRegisterDesc.Any() && listOfSpecialNeedsThatNeedRegisterDesc.Contains(specialNeedCode);
        }
        public async Task<MOBRegisterTravelersRequest> GetPopulateProfileOwnerData(MOBRegisterTravelersRequest request)
        {
            ProfileResponse profilePersist = new ProfileResponse();
            profilePersist = await _travelerUtility.GetCSLProfileResponseInSession(request.SessionId);
            if (profilePersist != null && profilePersist.Response != null && profilePersist.Response.Profiles != null)
            {
                CPProfileResponse profileResponse = profilePersist.Response;
                request.ProfileId = profilePersist.Response.Profiles[0].ProfileId;
                request.ProfileKey = profilePersist.Response.Profiles[0].ProfileOwnerKey;
                request.ProfileOwnerId = profilePersist.Response.Profiles[0].ProfileOwnerId;
                request.ProfileOwnerKey = profilePersist.Response.Profiles[0].ProfileOwnerKey;
                foreach (MOBCPTraveler mobCPTraveler in profilePersist.Response.Profiles[0].Travelers)
                {
                    if (mobCPTraveler.IsProfileOwner)
                    {
                        request.ProfileOwner = mobCPTraveler;
                        break;
                    }
                }
            }
            return request;
        }

        public async Task<MOBCPTraveler> RegisterNewTravelerValidateMPMisMatch(MOBCPTraveler registerTraveler, MOBRequest mobRequest, string sessionID, string cartId, string token)
        {

            //if (request.ValidateMPNameMissMatch && request.Travelers != null && request.Travelers.Count == 1 && request.Travelers[0].AirRewardPrograms != null && request.Travelers[0].AirRewardPrograms.Count > 0)
            #region This scenario is to validate Not Signed In Add Travelers or a Add New Traveler not saved to Profile -> Validate MP Traveler Name Miss Match per traveler.
            string mpNumbers = string.Empty;
            //if (registerTraveler.CustomerId == 0 && registerTraveler.AirRewardPrograms != null && registerTraveler.AirRewardPrograms[0].ProgramId == "7") //Program ID = 7 means United Mileage Plus Account
            if (registerTraveler.AirRewardPrograms != null && registerTraveler.AirRewardPrograms[0].ProgramId == "7") //Program ID = 7 means United Mileage Plus Account
            {
                #region Get Newly Added Traveler Not Saved to Profile MP Name Miss Match
                MOBCPProfileRequest savedTravelerProfileRequest = new MOBCPProfileRequest();
                #region
                savedTravelerProfileRequest.Application = mobRequest.Application;
                savedTravelerProfileRequest.DeviceId = mobRequest.DeviceId;
                savedTravelerProfileRequest.CartId = cartId;
                savedTravelerProfileRequest.AccessCode = mobRequest.AccessCode;
                savedTravelerProfileRequest.LanguageCode = mobRequest.LanguageCode;
                savedTravelerProfileRequest.ProfileOwnerOnly = true; // This is to call getprofile() to return only profile owner details to check the saved traveler FN, MN , LN, DOB and Gender to validate they match with saved traveler MP details and if not matched return a mismatch message to client if matched then get the Mileagplus details to get the elite level for LMX changes for client.
                savedTravelerProfileRequest.Token = token;
                savedTravelerProfileRequest.TransactionId = mobRequest.TransactionId;
                savedTravelerProfileRequest.IncludeAllTravelerData = false;
                savedTravelerProfileRequest.IncludeAddresses = false;
                savedTravelerProfileRequest.IncludeEmailAddresses = false;
                savedTravelerProfileRequest.IncludePhones = false;
                savedTravelerProfileRequest.IncludeCreditCards = false;
                savedTravelerProfileRequest.IncludeSubscriptions = false;
                savedTravelerProfileRequest.IncludeTravelMarkets = false;
                savedTravelerProfileRequest.IncludeCustomerProfitScore = false;
                savedTravelerProfileRequest.IncludePets = false;
                savedTravelerProfileRequest.IncludeCarPreferences = false;
                savedTravelerProfileRequest.IncludeDisplayPreferences = false;
                savedTravelerProfileRequest.IncludeHotelPreferences = false;
                savedTravelerProfileRequest.IncludeAirPreferences = false;
                savedTravelerProfileRequest.IncludeContacts = false;
                savedTravelerProfileRequest.IncludePassports = false;
                savedTravelerProfileRequest.IncludeSecureTravelers = false;
                savedTravelerProfileRequest.IncludeFlexEQM = false;
                savedTravelerProfileRequest.IncludeServiceAnimals = false;
                savedTravelerProfileRequest.IncludeSpecialRequests = false;
                savedTravelerProfileRequest.IncludePosCountyCode = false;
                #endregion
                savedTravelerProfileRequest.MileagePlusNumber = registerTraveler.AirRewardPrograms[0].MemberId;
                if (_configuration.GetValue<bool>("EnableValidateNewMPNumberIssueFix16788"))
                {
                    savedTravelerProfileRequest.SessionId = sessionID;
                }
                else
                {
                    savedTravelerProfileRequest.SessionId = sessionID + "_GetProfileOwner_" + registerTraveler.AirRewardPrograms[0].MemberId;
                }
                List<MOBCPProfile> travelerProfileList = await _customerProfile.GetProfile(savedTravelerProfileRequest);
                if (travelerProfileList != null && travelerProfileList.Count > 0 && travelerProfileList[0].Travelers != null && travelerProfileList[0].Travelers.Count > 0 && registerTraveler.FirstName.ToUpper().Trim() == travelerProfileList[0].Travelers[0].FirstName.ToUpper().Trim() && registerTraveler.LastName.ToUpper().Trim() == travelerProfileList[0].Travelers[0].LastName.ToUpper().Trim())
                {
                    if (travelerProfileList[0].Travelers[0].MileagePlus != null)
                    {
                        registerTraveler.MileagePlus = travelerProfileList[0].Travelers[0].MileagePlus;
                    }
                }
                else
                {
                    mpNumbers = mpNumbers + "," + registerTraveler.AirRewardPrograms[0].MemberId;
                    registerTraveler.Message = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage");
                    registerTraveler.MPNameNotMatchMessage = _configuration.GetValue<string>("ValidateMPNameMisMatchErrorMessage");
                    registerTraveler.isMPNameMisMatch = true;
                }
                #endregion
            }
            //53606 - For Travel Program Other Than Mileage Plus Loyalty Information Field Accepts O and 1 Digit Numbers - Manoj
            else if (registerTraveler.AirRewardPrograms != null && registerTraveler.AirRewardPrograms[0].ProgramId != "7")
            {
                if (!string.IsNullOrEmpty(registerTraveler.AirRewardPrograms[0].MemberId.ToString()))
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(registerTraveler.AirRewardPrograms[0].MemberId.ToString(), @"^\d$"))
                    {
                        throw new MOBUnitedException(_configuration.GetValue<string>("ValidateLoyalityNumberErrorMessage"));
                    }
                }
            }
            if (_shoppingUtility.IsCanadaTravelNumberEnabled(mobRequest.Application.Id, mobRequest.Application.Version.Major) && string.IsNullOrEmpty(registerTraveler.CanadianTravelerNumber) == false)
            {
                registerTraveler.CanadianTravelerNumber = registerTraveler.CanadianTravelerNumber.ToUpper();
                if (registerTraveler.CanadianTravelerNumber?.StartsWith("CAN") == false)
                    throw new MOBUnitedException(_configuration.GetValue<string>("CanadaTravelNumberFormatErrorMessage"));
                else if (registerTraveler.CanadianTravelerNumber?.Length != _configuration.GetValue<int>("CanadaTravelNumberLength"))
                    throw new MOBUnitedException(_configuration.GetValue<string>("CanadaTravelLengthMessage"));
            }
            #endregion
            return registerTraveler;
        }

        public async Task<List<MOBSHOPPrice>> GetPrices(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices, bool isAwardBooking,
   string sessionId, bool isReshopChange = false, int appId = 0, string appVersion = "", List<MOBItem> catalogItems = null, FlightReservationResponse shopBookingDetailsResponse = null)
        {
            List<MOBSHOPPrice> bookingPrices = new List<MOBSHOPPrice>();
            bool isEnableOmniCartMVP2Changes = _configuration.GetValue<bool>("EnableOmniCartMVP2Changes");
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
                        await ValidateAwardMileageBalance(sessionId, price.Amount, appId, appVersion);
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
                {
                    bookingPrice.PaxTypeDescription = $"{price.Count} {price.Description}".ToLower();
                }

                _shoppingBuyMiles.UpdatePriceTypeDescForBuyMiles(appId, appVersion, catalogItems, shopBookingDetailsResponse, bookingPrice);
                bookingPrices.Add(bookingPrice);
            }

            return bookingPrices;
        }
        private async Task ValidateAwardMileageBalance(string sessionId, decimal milesNeeded, int appId = 0, string appVersion = "")
        {
            CSLShopRequest shopRequest = new CSLShopRequest();
            shopRequest = await _sessionHelperService.GetSession<CSLShopRequest>(sessionId, shopRequest.ObjectName, new List<string> { sessionId, shopRequest.ObjectName }).ConfigureAwait(false);
            if (shopRequest != null && shopRequest.ShopRequest != null && shopRequest.ShopRequest.AwardTravel && shopRequest.ShopRequest.LoyaltyPerson != null && shopRequest.ShopRequest.LoyaltyPerson.AccountBalances != null)
            {
                if (shopRequest.ShopRequest.LoyaltyPerson.AccountBalances[0] != null && shopRequest.ShopRequest.LoyaltyPerson.AccountBalances[0].Balance < milesNeeded)
                {
                    if (IsBuyMilesFeatureEnabled(appId, appVersion) == false)
                        throw new MOBUnitedException(_configuration.GetValue<string>("NoEnoughMilesForAwardBooking"));
                }
            }
        }
        public bool IsBuyMilesFeatureEnabled(int appId, string version, List<MOBItem> catalgItems = null, bool isNotSelectTripCall = false)
        {
            if (!_configuration.GetValue<bool>("EnableBuyMilesFeature")) return false;
            if ((catalgItems != null && catalgItems.Count > 0 &&
                   catalgItems.FirstOrDefault(a => a.Id == _configuration.GetValue<string>("Android_EnableBuyMilesFeatureCatalogID") || a.Id == _configuration.GetValue<string>("iOS_EnableBuyMilesFeatureCatalogID"))?.CurrentValue == "1")
                   || isNotSelectTripCall)
                return GeneralHelper.IsApplicationVersionGreaterorEqual(appId, version, _configuration.GetValue<string>("Android_BuyMilesFeatureSupported_AppVersion"), _configuration.GetValue<string>("IPhone_BuyMilesFeatureSupported_AppVersion"));
            else
                return false;
        }
        public async Task UnregisterAncillaryOffer(MOBShoppingCart shoppingCart, FlightReservationResponse cslFlightReservationResponse, MOBRequest mobRequest, string sessionId, string cartId)
        {
            if (IsAncillaryOffersRegistered(cslFlightReservationResponse))
            {
                try
                {
                    var mobRegisterOfferRequest = BuildUnregisterAncillaryOfferRequest(shoppingCart, mobRequest, sessionId, cartId);
                    MOBBookingRegisterOfferResponse registerOffersResponse = new MOBBookingRegisterOfferResponse();
                    //string url = string.Format("{0}{1}", _configuration.GetValue<string>("MobileShoppingCartApi_BaseUrl"), "/Product/UnRegisterAncillaryOffersForBooking");
                    string path = "/Product/UnRegisterAncillaryOffersForBooking";
                    string jsonRequest = JsonConvert.SerializeObject(mobRegisterOfferRequest);
                    string jsonResponse = await MakeHTTPPost(sessionId, mobRequest.DeviceId, "UnregisterAncillaryOffer", mobRequest.Application, "", path, jsonRequest);
                    if (!string.IsNullOrEmpty(jsonResponse))
                    {
                        registerOffersResponse = JsonConvert.DeserializeObject<MOBBookingRegisterOfferResponse>(jsonResponse);
                        if (registerOffersResponse.Exception != null)
                        {
                            throw new Exception(registerOffersResponse.Exception.ErrMessage);
                        }
                    }
                }
                catch (System.Net.WebException webException)
                {
                    _logger.LogError("UnregisterAncillaryOffer {@WebException}", JsonConvert.SerializeObject(webException));
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(webException.InnerException ?? webException).Throw();
                }
                catch (System.Exception ex)
                {
                    _logger.LogError("UnregisterAncillaryOffer {@Exception}", JsonConvert.SerializeObject(ex));
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
                }
            }
        }
        public async Task<string> MakeHTTPPost(string sessionId, string deviceId, string action, MOBApplication application, string token, string path, string jsonRequest, bool isXMLRequest = false)
        {
            string jsonResponse = string.Empty;
            string applicationRequestType = isXMLRequest ? "xml" : "json";
            jsonResponse = await _mobileShoppingCart.UnRegisterAncillaryOffersForBooking(token, path, jsonRequest, sessionId).ConfigureAwait(false);

            return jsonResponse;
        }

        public bool IsAncillaryOffersRegistered(FlightReservationResponse cslFlightReservationResponse)
        {
            var shoppingCartItems = cslFlightReservationResponse?.ShoppingCart?.Items;
            var productCodes = _configuration.GetValue<string>("EligibletoUnregisterOffer_ProductCodes");
            if (shoppingCartItems != null && !String.IsNullOrEmpty(productCodes))
            {
                return shoppingCartItems.Any(item => productCodes.Split(',').Contains(item.Product.First().Code)
                                                                    || (
                                                                         _configuration.GetValue<string>("EligibletoUnregisterOffer_ProductCodes").Split(',').Contains("BE") &&
                                                                         (item.Product.First().Characteristics != null
                                                                                            &&
                                                                                               item.Product.First().Characteristics
                                                                                               .Any(c => !string.IsNullOrEmpty(c.Code)
                                                                                               && c.Code.ToUpper() == "ISBUNDLE"
                                                                                               && !string.IsNullOrEmpty(c.Value)
                                                                                               && c.Value.ToUpper() == "TRUE"))
                                                                        )
                                              );
            }
            return false;
        }
        public RegisterOfferRequest BuildUnregisterAncillaryOfferRequest(MOBShoppingCart shoppingCart, MOBRequest mobRequest, string sessionId, string cartId)
        {
            RegisterOfferRequest request = new RegisterOfferRequest
            {
                Application = mobRequest.Application,
                SessionId = sessionId,
                CartId = cartId,
                DeviceId = mobRequest.DeviceId,
                Flow = FlowType.BOOKING.ToString()
            };
            var productCodes = _configuration.GetValue<string>("EligibletoUnregisterOffer_ProductCodes");
            if (shoppingCart?.Products != null && !String.IsNullOrEmpty(productCodes))
            {
                foreach (var product in shoppingCart.Products)
                {
                    if (productCodes.Split(',').Contains(product.Code) || productCodes.Split(',').Contains(product.ProdDescription))
                    {
                        request.MerchandizingOfferDetails = request.MerchandizingOfferDetails ?? new Collection<MerchandizingOfferDetails>();
                        if (product.Code == "SEATASSIGNMENTS")
                        {
                            if (Convert.ToDouble(product.ProdTotalPrice) > 0)
                            {
                                request.MerchandizingOfferDetails.Add(new MerchandizingOfferDetails
                                {
                                    ProductCode = product.Code
                                });
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            request.MerchandizingOfferDetails.Add(new MerchandizingOfferDetails
                            {
                                ProductCode = product?.Code,
                                TripIds = product?.Segments?.Select(y => y?.TripId)?.SelectMany(y => y?.Split(',').ToList()).ToList(),
                                SelectedTripProductIDs = product?.Segments?.Select(y => y?.ProductId)?.ToList(),
                                ProductIds = product?.Segments.SelectMany(y => y?.ProductIds).ToList(),
                                IsOfferRegistered = true,
                            });
                        }

                    }
                }
            }
            return request;
        }

        public bool EnableYADesc(bool isReshop = false)
        {
            return _configuration.GetValue<bool>("EnableYoungAdultBooking") && _configuration.GetValue<bool>("EnableYADesc") && !isReshop;
        }

        public bool EnableShoppingcartPhase2ChangesWithVersionCheck(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableShoppingCartPhase2Changes")
                 && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "Android_EnableShoppingCartPhase2Changes_AppVersion", "iPhone_EnableShoppingCartPhase2Changes_AppVersion", "", "", true, _configuration);
        }

        public string BuildPaxTypeDescription(string paxTypeCode, string paxDescription, int paxCount)
        {
            string description = paxDescription;
            if (!string.IsNullOrEmpty(paxTypeCode))
            {
                switch (paxTypeCode.ToUpper())
                {
                    case "ADT":
                        description = $"{((paxCount == 1) ? "adult (18-64)" : "adults (18-64)")} ";
                        break;
                    case "SNR":
                        description = $"{((paxCount == 1) ? "senior (65+)" : "seniors (65+)")} ";
                        break;
                    case "C17":
                        description = $"{((paxCount == 1) ? "child (15-17)" : "children (15-17)")} ";
                        break;
                    case "C14":
                        description = $"{((paxCount == 1) ? "child (12-14)" : "children (12-14)")} ";
                        break;
                    case "C11":
                        description = $"{((paxCount == 1) ? "child (5-11)" : "children (5-11)")} ";
                        break;
                    case "C04":
                        description = $"{((paxCount == 1) ? "child (2-4)" : "children (2-4)")} ";
                        break;
                    case "INS":
                        description = $"{((paxCount == 1) ? "infant(under 2) - seat" : "infants(under 2) - seat")} ";
                        break;
                    case "INF":
                        description = $"{((paxCount == 1) ? "infant (under 2) - lap" : "infants (under 2) - lap")} ";
                        break;
                    default:
                        description = paxDescription;
                        break;
                }
            }
            return description;
        }
    }

}

