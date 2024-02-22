using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.DataAccess.Travelers;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.SSR;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Model.Shopping.Misc;
using United.Mobile.Model.Travelers;
using United.Service.Presentation.ProductModel;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.Extensions;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Utility.Enum;
using United.Utility.Helper;
using ProfileResponse = United.Mobile.Model.Shopping.ProfileResponse;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using TravelOption = United.Mobile.Model.Shopping.TravelOption;

namespace United.Mobile.Travelers.Domain
{
    public class TravelerBusiness : ITravelerBusiness
    {
        private readonly ICacheLog<TravelerBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly ITravelerUtility _travelerUtility;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly ITraveler _traveler;
        private readonly IFlightShoppingProductsService _getProductsService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly IPKDispenserService _pKDispenserService;
        private readonly ICachingService _cachingService;
        private readonly IDPService _dPService;
        private readonly IProductInfoHelper _productInfoHelper;
        private readonly PKDispenserPublicKey _pKDispenserPublicKey;
        private readonly IFormsOfPayment _formsOfPayment;
        private readonly IPaymentService _paymentService;
        private readonly IShoppingBuyMiles _shoppingBuyMiles;
        private readonly IOmniCart _omniCart;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ICSLStatisticsService _cSLStatisticsService;
        private readonly IHeaders _headers;
        private readonly IPaymentUtility _paymentUtility;
        private readonly IProductOffers _shopping;
        private readonly ILogger<TravelerBusiness> _logger1;
        private IFeatureSettings _featureSettings;
        private readonly IFeatureToggles _featureToggles;

        public TravelerBusiness(ICacheLog<TravelerBusiness> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility
            , ITravelerUtility travelerUtility
            , IFFCShoppingcs fFCShoppingcs
            , ITraveler traveler
            , IFlightShoppingProductsService getProductsService
            , IShoppingCartService shoppingCartService
            , IShoppingSessionHelper shoppingSessionHelper
            , ICachingService cachingService
            , IDPService dPService
            , IPKDispenserService pKDispenserService
            , IProductInfoHelper productInfoHelper
            , IFormsOfPayment formsOfPayment
            , IPaymentService paymentService
            , IShoppingBuyMiles shoppingBuyMiles
            , IOmniCart omniCart
            , IDynamoDBService dynamoDBService
            , ICSLStatisticsService cSLStatisticsService
            , IPaymentUtility paymentUtility
            , IProductOffers shopping
            , IHeaders headers
            , ILogger<TravelerBusiness> logger1
            , IFeatureSettings featureSettings
            , IFeatureToggles featureToggles)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
            _travelerUtility = travelerUtility;
            _fFCShoppingcs = fFCShoppingcs;
            _traveler = traveler;
            _getProductsService = getProductsService;
            _shoppingCartService = shoppingCartService;
            _shoppingSessionHelper = shoppingSessionHelper;
            _cachingService = cachingService;
            _dPService = dPService;
            _pKDispenserService = pKDispenserService;
            _productInfoHelper = productInfoHelper;
            _formsOfPayment = formsOfPayment;
            _paymentService = paymentService;
            _headers = headers;
            _pKDispenserPublicKey = new PKDispenserPublicKey(_configuration, _cachingService, _dPService, _pKDispenserService, _headers);
            _shoppingBuyMiles = shoppingBuyMiles;
            _omniCart = omniCart;
            _dynamoDBService = dynamoDBService;
            _cSLStatisticsService = cSLStatisticsService;
            _paymentUtility = paymentUtility;
            _shopping = shopping;
            _logger1 = logger1;
            _featureSettings = featureSettings;
            _featureToggles = featureToggles;
        }

        public async Task<MOBRegisterTravelersResponse> RegisterTravelers_CFOP(MOBRegisterTravelersRequest request)
        {
            MOBRegisterTravelersResponse response = new MOBRegisterTravelersResponse();
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            bool isDefault = false;

            Session session = await _shoppingSessionHelper.GetBookingFlowSession(request.SessionId);
            request.Token = session.Token;
            request.CartId = session.CartId;

            if (_configuration.GetValue<bool>("DisableDOBFix") == false
                    && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("Android_EnableDOBOldVersionFix_AppVersion"), _configuration.GetValue<string>("IPhone_EnableDOBOldVersionFix_AppVersion")) == false
                    && request.Travelers != null && request.Travelers.Count > 0)
            {
                foreach (var traveler in request.Travelers)
                {
                    traveler.GenderCode = UtilityHelper.GetGenderShortDesciption(traveler.GenderCode);
                }
            }

            List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, request.SessionId, session?.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");

            try
            {
                var enableBookingTravelerAlert = await _featureSettings.GetFeatureSettingValue("EnableBookingSameNameAlert").ConfigureAwait(false)
                     && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("Android_EnableBookingSameNameAlert_AppVersion"), _configuration.GetValue<string>("IPhone_EnableBookingSameNameAlert_AppVersion"));

                if (enableBookingTravelerAlert)
                {
                    var duplicatesTravelers = request?.Travelers?
                        .Where(s => !s.IsExtraSeat)
                        .GroupBy(s => s, new EqualityComparer())
                        .Where(x => x.Count() > 1)
                        .Select(s => s.Key);
                    if (duplicatesTravelers != null && duplicatesTravelers.Any())
                    {
                            var alertTitle = lstMessages != null && lstMessages.Count > 0
                                ? ShopStaticUtility.GetSDLMessageFromList(lstMessages, "BookingSameNameTravelerAlertTitle")[0].ContentFull
                                : string.Empty;
                            var alertDescription = lstMessages != null && lstMessages.Count > 0
                                ? ShopStaticUtility.GetSDLMessageFromList(lstMessages, "BookingSameNameTravelerAlertDescription")[0].ContentFull
                                : string.Empty;

                            var reservation = new MOBSHOPReservation();
                            reservation.AlertMessages = new List<Section>{
                            new Section
                            {
                                Text1 = alertTitle,
                                Text2 = alertDescription,
                                MessageType = MOBFSRAlertMessageType.Warning.ToString()
                            }};
                            response.Reservation = reservation;
                            throw new MOBUnitedException("9001", _configuration.GetValue<string>("DuplicateTravelerMessage").ToString());
                    }
                }
            }
            catch (MOBUnitedException uex)
            {
                if (uex.Code == "9001")
                {
                    response.Exception = new MOBException(uex.Code, uex.Message);
                    _logger.LogWarning("RegisterTravelers_CFOP {Response} and {SessionId}", JsonConvert.SerializeObject(response), request.SessionId);
                    return response;
                }
            }

            response.CartId = request.CartId;
            response.SessionId = request.SessionId;
            response.Token = session.Token;
            response.MileagePlusNumber = request.MileagePlusNumber;
            response.ProfileId = request.ProfileId;
            response.ProfileKey = request.ProfileKey;
            response.ProfileOwnerId = request.ProfileOwnerId;
            response.ProfileOwnerKey = request.ProfileOwnerKey;
            response.TransactionId = request.TransactionId;
            Reservation bookingPathReservation = new Reservation();

            ///We are loading this for check travelerCSL before register, so that we can know travers were selected or not for deciding the navigation page at client
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

            // Traveler selector validation
            if (_configuration.GetValue<bool>("EnableTravelerSelectorCountValidation"))
            {
                if (bookingPathReservation?.NumberOfTravelers > 0 && request?.Travelers?.Count > 0)
                {
                    if (bookingPathReservation.NumberOfTravelers != request.Travelers.Count)
                    {
                        throw new MOBUnitedException((bookingPathReservation.NumberOfTravelers > request.Travelers.Count) ? _configuration.GetValue<string>("FewTravelersErrorMessage") : _configuration.GetValue<string>("ManyTravelersErrorMessage"));
                    }
                }
            }

            var travelerCSLBeforeRegister = bookingPathReservation.TravelersCSL;
            List<int> travelerAges = new List<int>();
            int childInLapCount = 0;

            string firstLOFDepDate = string.Empty;
            if (await _featureSettings.GetFeatureSettingValue("EnableAddTravelers").ConfigureAwait(false) && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("Android_EnableAddTraveler_AppVersion"), _configuration.GetValue<string>("iPhone_EnableAddTraveler_AppVersion")))
            {
                firstLOFDepDate = bookingPathReservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTime;
                bookingPathReservation?.ShopReservationInfo2?.AllEligibleTravelersCSL?.Where(y => (!request.Travelers.Any(x => x.PaxID == y.PaxID)) && y.IsInfantTravelerTypeConfirmed)?.ForEach(y =>
                { y.IsInfantTravelerTypeConfirmed = false; });

                if (request.Travelers.Any(x => (x.TravelerTypeCode.Equals("INF") || x.TravelerTypeCode.Equals("INS"))))
                {
                    var (mobReservation, mobShoppingCart) = await CheckInfantsInRequest(request, session, bookingPathReservation, lstMessages);
                    if (request.Travelers.Any(x => (x.TravelerTypeCode.Equals("INF") || x.TravelerTypeCode.Equals("INS")) && !x.IsInfantTravelerTypeConfirmed))
                    {
                        response.Reservation = mobReservation;
                        response.ShoppingCart = mobShoppingCart;

                        return response;
                    }
                }
                else await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, request.SessionId, new List<string> { request.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);

                SortTravelersInRequest(request, firstLOFDepDate);
            }
            else if (_travelerUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange) && bookingPathReservation.ShopReservationInfo2 != null
                && bookingPathReservation.ShopReservationInfo2.TravelerTypes != null && bookingPathReservation.ShopReservationInfo2.TravelerTypes.Count > 0)
            {
                firstLOFDepDate = bookingPathReservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTime;
                if (bookingPathReservation.IsSignedInWithMP)
                {
                    request.Travelers = _travelerUtility.AssignInfantWithSeat(bookingPathReservation, request.Travelers);
                }
                else
                {
                    request.Travelers = _travelerUtility.AssignInfantInLap(bookingPathReservation, request.Travelers);
                }

                //CSL register travelers expecting order as like initial search.
                SortTravelersInRequest(request, firstLOFDepDate);
            }


            if (!string.IsNullOrEmpty(bookingPathReservation.ShopReservationInfo2.NextViewName) && bookingPathReservation.ShopReservationInfo2.NextViewName.ToUpper().Equals("TRAVELOPTION"))
            {
                if (travelerCSLBeforeRegister != null)
                {
                    travelerCSLBeforeRegister.ToList().ForEach(t => t.Value.Seats = null);
                }
            }
            if (_travelerUtility.IsExtraSeatFeatureEnabled(request.Application.Id, request.Application.Version.Major, session.CatalogItems))
            {
                var extraSeats = request?.Travelers?.Where(a => a.IsExtraSeat == true);
                if (extraSeats?.Count() > 0)
                {
                    if (extraSeats.Count() >= bookingPathReservation?.ShopReservationInfo2?.displayTravelTypes?.Count())
                    {
                        throw new MOBUnitedException("4590", _configuration.GetValue<string>("ExtraSeatRegularTravelerValidationMessage"));
                    }
                    var regularTravelers = request.Travelers.Where(a => a.IsExtraSeat == false);
                    if (extraSeats.Any(t2 => !regularTravelers?.Any(t1 => t2.ExtraSeatData.SelectedPaxId == (t1.PaxID)) == true))
                        throw new MOBUnitedException("4590", _configuration.GetValue<string>("ExtraSeatTravelerMatchValidationMessage"));
                }
            }

            for (int co = 0; co < request.Travelers.Count; co++)
            {
                #region
                request.Travelers[co].Seats = null;
                //Fare Quotes are not being set due to missing TravelerTypeCode - JD - 5/17/2016
                //This allows us to force a type code so that the Fare Quotes will be filed. - JD - 5/17/2016
                //We default to ADT/Adult.  We do not handle infants.  And we will not default to SEN/Senior - JD - 5/17/2016
                /*
                 ******************* added code out 5/17/2016 **********************
                if (string.IsNullOrEmpty(request.Travelers[co].TravelerTypeCode))
                {
                    request.Travelers[co].TravelerTypeCode = "ADT";
                }
                 ******************* commenting code out 5/24/2016 **********************
                */
                //Modifying this to default ALL travelers to ADT. - JD - 5/24/2016
                if (_travelerUtility.EnableYoungAdultValidation(session.IsReshopChange))
                {
                    if (bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.IsYATravel)
                    {
                        if (!bookingPathReservation.IsSignedInWithMP)
                            throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage") ?? "Sorry, something went wrong. Please try again.");

                        ValidateYATravel(bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL, request.Travelers);
                    }
                }
                if (_travelerUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, session.IsReshopChange) && bookingPathReservation.ShopReservationInfo2 != null
                    && bookingPathReservation.ShopReservationInfo2.TravelerTypes != null && bookingPathReservation.ShopReservationInfo2.TravelerTypes.Count > 0)
                {
                    childInLapCount = _travelerUtility.GetChildInLapCount(request.Travelers[co], travelerAges, childInLapCount, firstLOFDepDate);
                }
                else
                {
                    request.Travelers[co].TravelerTypeCode = "ADT";
                }
                if (_travelerUtility.IsExtraSeatFeatureEnabled(request.Application.Id, request.Application.Version.Major, session.CatalogItems))
                {
                    var extraSeatCountPerTraveler = request.Travelers.Where(a => a.IsExtraSeat == true && a.FirstName == request.Travelers[co].FirstName
                                                                           && a.LastName == request.Travelers[co].LastName).Count();
                    if (extraSeatCountPerTraveler > _configuration.GetValue<int>("ExtraSeatMaxAllowedLimit"))
                    {
                        throw new MOBUnitedException("4590", _configuration.GetValue<string>("ExtraSeatMaxMessage"));
                    }
                }
                #endregion
            }
            if (_travelerUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, session.IsReshopChange) && bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.TravelerTypes != null)
            {
                _travelerUtility.ValidateTravelerAges(travelerAges, childInLapCount);
            }
            if (_configuration.GetValue<bool>("EnableCouponsforBooking") && request.ContinueToChangeTravelers)
            {
                if (!await AddOrRemovePromo(request, session, true, request.Flow))
                {
                    throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }

            var changeInTravelersAlert = await GetChangeInTravelerMessageWhenETCOrUpliftareFop(request, bookingPathReservation, session);
            if (changeInTravelersAlert != null)
            {
                response.ChangeInTravelersAlert = changeInTravelersAlert;

                return response;
            }
            var isEnablePaymentMoneyMilesFix = await _featureSettings.GetFeatureSettingValue("EnableEditTravelerPaymentMoneyMilesFix").ConfigureAwait(false);
            if (isEnablePaymentMoneyMilesFix && request.ContinueToChangeTravelers) // after M+M 100% remove this block of code
            {
                shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, shoppingCart.ObjectName, new List<string> { request.SessionId, shoppingCart.ObjectName }).ConfigureAwait(false);
            }

            if (_shoppingUtility.IsEnableMoneyPlusMilesFeature(request.Application.Id, request.Application.Version.Major, session?.CatalogItems) && request.ContinueToChangeTravelers
                && (session.IsMoneyPlusMilesSelected || (isEnablePaymentMoneyMilesFix && shoppingCart?.FormofPaymentDetails?.MoneyPlusMilesCredit?.SelectedMoneyPlusMiles != null)))
            {
                ApplyMoneyPlusMilesOptionRequest milesRequest = new ApplyMoneyPlusMilesOptionRequest();
                milesRequest.OptionId = null; // for undo moneymiles send optionid as null
                milesRequest.Application = request.Application;
                milesRequest.CartId = request.CartId;
                milesRequest.AccessCode = request.AccessCode;
                //milesRequest.CartKey = request.CartKey;
                milesRequest.DeviceId = request.DeviceId;
                milesRequest.SessionId = request.SessionId;
                milesRequest.TransactionId = request.TransactionId;
                milesRequest.Flow = request.Flow;
                //milesRequest.PointOfSale = request.PointOfSale;
                var milesResponse = await ApplyMilesPlusMoneyOption(session, milesRequest);
                session.IsMoneyPlusMilesSelected = false;
                await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);
            }


            response.Reservation = await RegisterTravelersCSL(request, isRegisterOffersCall: true);
            response.Reservation.SessionId = request.SessionId;
            #region udpate prices at persist reservation object
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

            var isTravelerChanged = ChangeTravelerValidation(request.Travelers, travelerCSLBeforeRegister);

            bool isE20TravelersChanges = false;
            if (!_configuration.GetValue<bool>("DisableEMPCreditLoadInRegisterTraveler"))
            {
                isE20TravelersChanges = session.TravelType == "E20" &&
                                         (isTravelerChanged ||
                                                            (request.Travelers != null &&
                                                            travelerCSLBeforeRegister != null &&
                                                            travelerCSLBeforeRegister.Count() != request.Travelers.Count()
                                                            ));
            }

            if (!isTravelerChanged)
            {
                foreach (var trv in travelerCSLBeforeRegister)
                {
                    var currentTravelrFromRequestedTravelers = bookingPathReservation.TravelersCSL.FirstOrDefault(t => t.Value.PaxID == trv.Value.PaxID);
                    if (currentTravelrFromRequestedTravelers.Value != null)
                    {
                        currentTravelrFromRequestedTravelers.Value.Seats = trv.Value.Seats;
                    }
                }
            }

            var isFarelockExist = TravelOptionsContainsFareLock(bookingPathReservation.TravelOptions);
            bool IsShowBackbutton = false;

            //  Reverting this code to normal as productoin..next iteration28 it will be fixed /The below logic is to check User Navigate from RTI when new traveler added or Traveler Changed
            if (bookingPathReservation.ShopReservationInfo2.NextViewName == "RTI")
            {
                IsShowBackbutton = true;
            }
            else
            {
                bookingPathReservation.SeatPrices = null;
            }

            string navigateTo = GetNavigationPageCode(request, bookingPathReservation, isFarelockExist, isTravelerChanged);

            //remove uplift and reset the flags
            if (request.ContinueToChangeTravelers && (navigateTo?.ToUpper().Trim() == "TRAVELOPTION" || IsTravelerEdited(request.Travelers, travelerCSLBeforeRegister)))
            {
                if (bookingPathReservation.FormOfPaymentType == MOBFormofPayment.Uplift)
                {
                    bookingPathReservation.FormOfPaymentType = MOBFormofPayment.CreditCard;
                }
                if (bookingPathReservation.ShopReservationInfo2 != null)
                {
                    bookingPathReservation.ShopReservationInfo2.HideSelectSeatsOnRTI = false;
                    bookingPathReservation.ShopReservationInfo2.HideTravelOptionsOnRTI = false;
                    bookingPathReservation.ShopReservationInfo2.Uplift = null;
                }
            }

            if (_configuration.GetValue<bool>("EnableUpliftPayment"))
            {
                //some time, even when there is no change in travelers, tpi gets dropped - so we have to remove TPI message in that case.
                var showUpliftTpiMessage = _travelerUtility.ShowUpliftTpiMessage(bookingPathReservation, bookingPathReservation?.FormOfPaymentType.ToString());
                if (!showUpliftTpiMessage && (bookingPathReservation.ShopReservationInfo2?.InfoWarningMessages?.Any() ?? false))
                {
                    bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.RemoveAll(i => i.Order == MOBINFOWARNINGMESSAGEORDER.UPLIFTTPISECONDARYPAYMENT.ToString());
                }
            }

            if (bookingPathReservation.IsReshopChange)
            {
                if (_travelerUtility.EnableReshopCubaTravelReasonVersion(request.Application.Id, request.Application.Version.Major))
                {
                    navigateTo = string.Empty;
                }
            }
            if (_configuration.GetValue<bool>("EnableTravelerRefactoringChanges"))
            {
                _travelerUtility.UpdateAllEligibleTravelersList(bookingPathReservation);
            }
            else
            {
                if (bookingPathReservation.ShopReservationInfo2 == null || bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL == null || bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count() == 0)
                {
                    if (bookingPathReservation.ShopReservationInfo2 == null)
                        bookingPathReservation.ShopReservationInfo2 = new ReservationInfo2();
                    if (bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL == null)
                        bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL = new List<MOBCPTraveler>();

                    foreach (var travelerkey in bookingPathReservation.TravelersCSL)
                    {
                        travelerkey.Value.IsPaxSelected = true;
                        bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Add(travelerkey.Value);
                    }
                }
                else
                {
                    foreach (var travelerkey in bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL)
                    {
                        travelerkey.IsPaxSelected = bookingPathReservation.TravelersCSL != null && bookingPathReservation.TravelersCSL.Any(t => t.Value.PaxID == travelerkey.PaxID);
                    }
                }
            }
            _travelerUtility.ValidateTravelersForCubaReason(bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL, bookingPathReservation.IsCubaTravel);
            var isEplusAncillary = false;
            if (isFarelockExist)
            {
                List<TravelOption> travelOptions = new List<TravelOption>();
                foreach (TravelOption option in bookingPathReservation.TravelOptions)
                {
                    if (option != null && !string.IsNullOrEmpty(option.Key) && option.Key.ToUpper() == "FARELOCK")
                    {
                        travelOptions.Add(option);
                        break;
                    }
                }
                if (_travelerUtility.EnableEPlusAncillary(request.Application.Id, request.Application.Version.Major, session.IsReshopChange))
                {
                    var travelOption = bookingPathReservation.TravelOptions?.FirstOrDefault(t => t?.Key?.Trim().ToUpper() == "EFS");
                    if (travelOption != null && !string.IsNullOrEmpty(travelOption.Key))
                    {
                        travelOptions.Add(travelOption);
                        isEplusAncillary = true;
                    }
                }
                bookingPathReservation.TravelOptions = travelOptions;
            }
            else
            {
                if (navigateTo == "TravelOption")
                {
                    if (_travelerUtility.EnableEPlusAncillary(request.Application.Id, request.Application.Version.Major, session.IsReshopChange) && bookingPathReservation.TravelOptions != null)
                    {
                        var travelOption = bookingPathReservation.TravelOptions?.FirstOrDefault(t => t?.Key?.Trim().ToUpper() == "EFS");
                        if (travelOption != null && !string.IsNullOrEmpty(travelOption.Key))
                        {
                            bookingPathReservation.TravelOptions = new List<TravelOption>() { travelOption };
                            isEplusAncillary = true;
                        }
                        else
                        {
                            bookingPathReservation.TravelOptions = null;
                        }
                    }
                    else
                    {
                        bookingPathReservation.TravelOptions = null;
                    }
                }
                //This logic to hide the back button when Traveler changed from RTI
                if (navigateTo == "TravelOption" && IsShowBackbutton)
                {
                    bookingPathReservation.ShopReservationInfo2.ShouldHideBackButton = true;
                }
                if (_configuration.GetValue<bool>("EnableTravelerRefactoringChanges"))
                {
                    await GetTPIandUpdatePrices(request, response.Reservation.CartId, session, bookingPathReservation);
                }
                else
                {

                    // Feature TravelInsuranceOptimization : MOBILE-21191, MOBILE-21193, MOBILE-21195, MOBILE-21197
                    if (!_configuration.GetValue<bool>("EnableTravelInsuranceOptimization"))
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

                }
            }
            //bookingPathReservation.ShopReservationInfo2.NextViewName = navigateTo;
            SelectTrip persistSelectTripObj = await _sessionHelperService.GetSession<SelectTrip>(request.SessionId, new SelectTrip().ObjectName, new List<string> { request.SessionId, new SelectTrip().ObjectName }).ConfigureAwait(false);
            if (persistSelectTripObj != null && (bookingPathReservation.Prices == null || bookingPathReservation.Prices.Count() == 0))
            {
                bookingPathReservation.Prices = persistSelectTripObj.Responses[persistSelectTripObj.LastSelectTripKey].Availability.Reservation.Prices;
            }

            if (!_configuration.GetValue<bool>("EnableEplusCodeRefactor") && _travelerUtility.EnableEPlusAncillary(request.Application.Id, request.Application.Version.Major, session.IsReshopChange) && isEplusAncillary)
            {
                var reservation = persistSelectTripObj.Responses[persistSelectTripObj.LastSelectTripKey].Availability.Reservation;
                reservation.Prices = bookingPathReservation.Prices;
                bookingPathReservation.Prices = _travelerUtility.UpdatePricesForBundles(reservation, null, request.Application.Id, request.Application.Version.Major, session.IsReshopChange);

            }

            // ExpressCheckout flow
            if (await _shoppingUtility.IsEnabledExpressCheckoutFlow(request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false)
                && bookingPathReservation != null
                && bookingPathReservation.ShopReservationInfo2 != null
                && bookingPathReservation.ShopReservationInfo2.IsExpressCheckoutPath
                && request != null
                && string.IsNullOrEmpty(request.MileagePlusNumber))
            {
                try
                {
                    // Make the property false in guess flow
                    bookingPathReservation.ShopReservationInfo2.IsExpressCheckoutPath = false;
                }
                catch (Exception ex)
                {
                    _logger.LogError("RegisterTravelers_CFOP - ExpressCheckout Exception {error} and SessionId {sessionId}", ex.Message, request.SessionId);
                }
            }

            await UpdateFFCInSession(bookingPathReservation, null, request.ContinueToChangeTravelers, request.Application, request.SessionId, null);
            await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, request.SessionId, new List<string> { request.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName);
            #endregion
            //**// A common place to get all the saved reservation data at persist. 
            response.Reservation = await _shoppingUtility.GetReservationFromPersist(response.Reservation, request.SessionId).ConfigureAwait(false);
            //**//
            response.Reservation.ShopReservationInfo2.NextViewName = navigateTo;

            if (response.Reservation != null && session != null && !string.IsNullOrEmpty(session.EmployeeId))
            {
                response.Reservation.IsEmp20 = true;
            }

            ProfileResponse profilePersist = new ProfileResponse();
            profilePersist = await _travelerUtility.GetCSLProfileResponseInSession(request.SessionId);
            if (profilePersist != null && profilePersist.Response != null)
            {
                CPProfileResponse profileResponse = profilePersist.Response;
                response.Profile = profileResponse.Profiles[0];
            }

            response.Reservation.IsBookingCommonFOPEnabled = bookingPathReservation.IsBookingCommonFOPEnabled;
            shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, shoppingCart.ObjectName, new List<string> { request.SessionId, shoppingCart.ObjectName }).ConfigureAwait(false);
            await UpdateFFCInSession(null, shoppingCart, request.ContinueToChangeTravelers, request.Application, request.SessionId, response.Reservation.Prices);
            if (_travelerUtility.IsETCEnabledforMultiTraveler(request.Application.Id, request.Application.Version.Major) && response.Reservation.ShopReservationInfo2.IsMultipleTravelerEtcFeatureClientToggleEnabled &&
                !_travelerUtility.IsETCCombinabilityEnabled(request.Application.Id, request.Application.Version.Major))
            {
                FOPTravelerCertificateResponse persistedTravelCertifcateResponse = null;
                shoppingCart.SCTravelers = (response.Reservation.TravelersCSL != null && response.Reservation.TravelersCSL.Count() > 0) ? response.Reservation.TravelersCSL : null;
                if (response.Reservation.Prices.Exists(price => price.DisplayType.ToUpper().Trim() == "CERTIFICATE") ||
                    (shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates != null &&
                    shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates.Count > 0))
                {
                    persistedTravelCertifcateResponse = await _sessionHelperService.GetSession<FOPTravelerCertificateResponse>(request.SessionId, persistedTravelCertifcateResponse.ObjectName, new List<string> { request.SessionId, persistedTravelCertifcateResponse.ObjectName }).ConfigureAwait(false);
                }
                _shoppingUtility.AssignCertificateTravelers(shoppingCart, persistedTravelCertifcateResponse, bookingPathReservation.Prices, request.Application);
                if ((shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates != null &&
                    shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates.Count > 0))
                {
                    ShopStaticUtility.AddGrandTotalIfNotExistInPricesAndUpdateCertificateValue(bookingPathReservation.Prices, shoppingCart.FormofPaymentDetails);
                    _shoppingUtility.UpdateCertificateAmountInTotalPrices(bookingPathReservation.Prices, shoppingCart.Products, persistedTravelCertifcateResponse.ShoppingCart.FormofPaymentDetails.TravelCertificate.TotalRedeemAmount);
                    _shoppingUtility.AssignIsOtherFOPRequired(shoppingCart.FormofPaymentDetails, bookingPathReservation.Prices, shoppingCart.FormofPaymentDetails?.SecondaryCreditCard != null);
                    persistedTravelCertifcateResponse.ShoppingCart.Products = shoppingCart.Products;
                }

                if (_configuration.GetValue<bool>("SavedETCToggle"))
                {
                    _shoppingUtility.UpdateSavedCertificate(shoppingCart);
                    if (persistedTravelCertifcateResponse?.ShoppingCart != null)
                    {
                        persistedTravelCertifcateResponse.ShoppingCart.ProfileTravelerCertificates = shoppingCart.ProfileTravelerCertificates;
                    }
                }
                response.Reservation.Prices = bookingPathReservation.Prices;
                if (persistedTravelCertifcateResponse != null)
                    await _sessionHelperService.SaveSession<FOPTravelerCertificateResponse>(persistedTravelCertifcateResponse, request.SessionId, new List<string> { request.SessionId, persistedTravelCertifcateResponse.ObjectName }, persistedTravelCertifcateResponse.ObjectName);
            }
            bool isTC = _travelerUtility.IncludeTravelCredit(request.Application.Id, request.Application.Version.Major);
            if (_shoppingUtility.IsEnableMoneyPlusMilesFeature(request.Application.Id, request.Application.Version.Major, session?.CatalogItems))
            {
                //Reset MM if Edit Traveler is true and ContinueToChangeTravelers is true - Toggle check is
                shoppingCart = await UpdateMMInSession(shoppingCart, request, request.Application, session);
            }
            shoppingCart = await _shoppingUtility.ReservationToShoppingCart_DataMigration(response.Reservation, shoppingCart, request, session);

            shoppingCart.Flow = request.Flow;

            //remove Uplift from shopping cart
            if (request.ContinueToChangeTravelers && (navigateTo?.ToUpper().Trim() == "TRAVELOPTION" || IsTravelerEdited(request.Travelers, travelerCSLBeforeRegister)))
            {
                if (shoppingCart.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.Uplift.ToString())
                {
                    shoppingCart.FormofPaymentDetails.FormOfPaymentType = null;
                    shoppingCart.FormofPaymentDetails.Uplift = null;
                }
            }
            if (_shoppingUtility.IsEnableMoneyPlusMilesFeature(request.Application.Id, request.Application.Version.Major, session?.CatalogItems) == false)
            {
                //Reset MM if Edit Traveler is true and ContinueToChangeTravelers is true - Toggle check is
                shoppingCart = await UpdateMMInSession(shoppingCart, request, request.Application, session);
            }

            //await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, request.SessionId, new List<string> { request.SessionId, ObjectNames.MOBShoppingCart }, ObjectNames.MOBShoppingCart);
            response.ShoppingCart = shoppingCart;

            if (_travelerUtility.IncludeMoneyPlusMiles(request.Application.Id, request.Application.Version.Major))
            {
                var cartInfo = await _travelerUtility.GetCartInformation(request.SessionId, request.Application, request.DeviceId, request.CartId, session.Token);
                if (cartInfo != null)
                {
                    var reservation = cartInfo.Reservation;
                    await _sessionHelperService.SaveSession<Service.Presentation.ReservationModel.Reservation>(cartInfo.Reservation, request.SessionId, new List<string> { request.SessionId, typeof(Service.Presentation.ReservationModel.Reservation).FullName }, typeof(Service.Presentation.ReservationModel.Reservation).FullName);
                }
            }
            if (await _shoppingUtility.IsLoadTCOrTB(shoppingCart).ConfigureAwait(false))
            {
                var preLoadTravelCredit = new PreLoadTravelCredits(_logger, _configuration, _sessionHelperService, _travelerUtility, _paymentService, _fFCShoppingcs, _featureSettings);
                var travelcredit = shoppingCart?.FormofPaymentDetails?.TravelCreditDetails?.TravelCredits;
                var DisableTravelCreditCheckForTravelerUpdate = _configuration.GetValue<bool>("DisableTravelCreditCheckFortravelerUpdate");
                if (isTC && request.Flow == FlowType.BOOKING.ToString()
                        && !session.IsAward && (ConfigUtility.IsEnableFeature("EnableU4BCorporateBookingFFC", request.Application.Id, request.Application.Version.Major) ? true : !session.IsCorporateBooking) && DisableTravelCreditCheckForTravelerUpdate)
                {
                    await preLoadTravelCredit.PreLoadTravelCredit(session.SessionId, shoppingCart, request, shoppingCart.IsCorporateBusinessNamePersonalized);
                }
                if (isE20TravelersChanges)
                {
                    if (isTC && request.Flow == FlowType.BOOKING.ToString() && !session.IsAward)
                    {
                        await preLoadTravelCredit.PreLoadTravelCredit(session.SessionId, shoppingCart, request, shoppingCart.IsCorporateBusinessNamePersonalized);
                    }
                }
                else
                {
                    if (isTC && request.Flow == FlowType.BOOKING.ToString()
                            && !session.IsAward && (ConfigUtility.IsEnableFeature("EnableU4BCorporateBookingFFC", request.Application.Id, request.Application.Version.Major) ? true : !session.IsCorporateBooking) && isTravelerChanged && !DisableTravelCreditCheckForTravelerUpdate)
                    {
                        await preLoadTravelCredit.PreLoadTravelCredit(session.SessionId, shoppingCart, request);
                    }
                }
                await UpdateTravelBankPrice(request, shoppingCart, session, response.Reservation);
            }
            await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, request.SessionId, new List<string> { request.SessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName);
            response.ShoppingCart = shoppingCart;

            //if (response.ShoppingCart.FormofPaymentDetails.TravelCreditDetails.ReviewTravelCreditMessages != null && !response.ShoppingCart.FormofPaymentDetails.TravelCreditDetails.ReviewTravelCreditMessages.Any())
            //    response.ShoppingCart.FormofPaymentDetails.TravelCreditDetails.ReviewTravelCreditMessages = null;
            var tupleRes = await _formsOfPayment.GetEligibleFormofPayments(request, session, shoppingCart, request.CartId, request.Flow, isDefault, response.Reservation);

            response.EligibleFormofPayments = tupleRes.response;
            response.IsDefaultPaymentOption = tupleRes.isDefault;
            //Covid-19 Emergency WHO TPI content
            if (_configuration.GetValue<bool>("ToggleCovidEmergencytextTPI") == true)
            {
                bool return_TPICOVID_19WHOMessage_For_BackwardBuilds = GeneralHelper.IsApplicationVersionGreater2(request.Application.Id, request.Application.Version.Major, "Android_Return_TPICOVID_19WHOMessage__For_BackwardBuilds", "iPhone_Return_TPICOVID_19WHOMessage_For_BackwardBuilds", string.Empty, string.Empty, _configuration);
                if (!return_TPICOVID_19WHOMessage_For_BackwardBuilds && response.Reservation != null
                    && response.Reservation.TripInsuranceInfoBookingPath != null && response.Reservation.TripInsuranceInfoBookingPath.tpiAIGReturnedMessageContentList != null
                    && response.Reservation.TripInsuranceInfoBookingPath.tpiAIGReturnedMessageContentList.Count > 0)
                {
                    MOBItem tpiCOVID19EmergencyAlertBookingPath = response.Reservation.TripInsuranceInfoBookingPath.tpiAIGReturnedMessageContentList.Find(p => p.Id.ToUpper().Trim() == "COVID19EmergencyAlert".ToUpper().Trim());
                    if (tpiCOVID19EmergencyAlertBookingPath != null)
                    {
                        response.Reservation.TripInsuranceInfoBookingPath.Tnc = response.Reservation.TripInsuranceInfoBookingPath.Tnc +
                            "<br><br>" + tpiCOVID19EmergencyAlertBookingPath.CurrentValue;
                    }
                }
            }


            if (_configuration.GetValue<bool>("Log_CSL_Call_Statistics"))
            {
                try
                {
                    CSLStatistics _cslStatistics = new CSLStatistics(_logger, _configuration, _dynamoDBService, _cSLStatisticsService);
                    string callDurations = string.Empty;
                    await _cslStatistics.AddCSLCallStatisticsDetails(string.Empty, "REST_Shopping", response.CartId, callDurations, "RegisterTraveler_CFOP", request.SessionId);

                }
                catch { }
            }
            _logger.LogInformation("RegisterTravelers_CFOP {Response} and {SessionId}", JsonConvert.SerializeObject(response), request.SessionId);
            return response;

        }
        private static void SortTravelersInRequest(MOBRegisterTravelersRequest request, string firstLOFDepDate)
        {
            request.Travelers.Sort((x, y) =>
             ShopStaticUtility.GetTTypeValue(TopHelper.GetAgeByDOB(x.BirthDate, firstLOFDepDate), x.TravelerTypeCode.ToUpper().Equals("INF")).CompareTo(ShopStaticUtility.GetTTypeValue(TopHelper.GetAgeByDOB(y.BirthDate, firstLOFDepDate), y.TravelerTypeCode.ToUpper().Equals("INF")))
            );

            for (int i = 0; i < request.Travelers.Count; i++)
            {
                request.Travelers[i].PaxIndex = i;
            }
        }
        private async Task<(MOBSHOPReservation mobReservation, MOBShoppingCart mobShoppingCart)> CheckInfantsInRequest(MOBRegisterTravelersRequest request, Session session, Reservation reservation, List<CMSContentMessage> lstMessages)
        {
            int adultSeniorCount = 0;
            int infantInLapCount = 0;

            adultSeniorCount = request.Travelers.Where(x => x.TravelerTypeCode.Equals("ADT") || x.TravelerTypeCode.Equals("SNR")).Count();
            infantInLapCount = request.Travelers.Where(x => x.TravelerTypeCode.Equals("INF") && x.IsInfantTravelerTypeConfirmed).Count();

            if (adultSeniorCount < infantInLapCount || adultSeniorCount == 0)
            {
                throw new MOBUnitedException("UnaccompaniedDisclaimerMessage", _configuration.GetValue<string>("UnaccompaniedDisclaimerMessage"));
            }

            request.Travelers.Where(x => x.TravelerTypeCode.Equals("INF") || x.TravelerTypeCode.Equals("INS")).ForEach(req =>
            reservation?.ShopReservationInfo2?.AllEligibleTravelersCSL?.Where(y => y.PaxID == req.PaxID)?.ForEach(y => { y.IsInfantTravelerTypeConfirmed = req.IsInfantTravelerTypeConfirmed; y.TravelerTypeCode = req.TravelerTypeCode; }));

            if (request.Travelers.Any(x => (x.TravelerTypeCode.Equals("INF") || x.TravelerTypeCode.Equals("INS")) && !x.IsInfantTravelerTypeConfirmed))
            {
                var mobReservation = new MOBSHOPReservation();
                mobReservation = await _shoppingUtility.MakeReservationFromPersistReservation(mobReservation, reservation, session);
                MOBShoppingCart shoppingCart = new MOBShoppingCart();
                shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, shoppingCart.ObjectName, new List<string> { request.SessionId, shoppingCart.ObjectName }).ConfigureAwait(false);

                var paxId = request.Travelers.FirstOrDefault(x => (x.TravelerTypeCode.Equals("INS") || x.TravelerTypeCode.Equals("INF")) && !x.IsInfantTravelerTypeConfirmed).PaxID;
                var (age, isInfantTurningTwo) = CheckIsInfantTurningTwo(request.Travelers.FirstOrDefault(x => x.PaxID == paxId).BirthDate, reservation.Trips.LastOrDefault().DepartDate);

                if (isInfantTurningTwo)
                    mobReservation.OnScreenAlert = BuildOnScreenAlertForChildTurningTwo(paxId, request, lstMessages);
                else if (age < 2 && adultSeniorCount > infantInLapCount)
                    mobReservation.OnScreenAlert = BuildOnScreenAlertForInfantInLap(paxId, request, lstMessages);
                else
                    mobReservation.OnScreenAlert = BuildOnScreenAlertForInfantWithSeat(paxId, request, lstMessages);

                await _sessionHelperService.SaveSession<Reservation>(reservation, request.SessionId, new List<string> { request.SessionId, reservation.ObjectName }, reservation.ObjectName).ConfigureAwait(false);
                return (mobReservation, shoppingCart);
            }
            else
            {
                await _sessionHelperService.SaveSession<Reservation>(reservation, request.SessionId, new List<string> { request.SessionId, reservation.ObjectName }, reservation.ObjectName).ConfigureAwait(false);
                return default;
            }
        }

        private (int age, bool isInfantTurningTwo) CheckIsInfantTurningTwo(string birthDate, string lastLOFDepDate)
        {
            var travelDate = DateTime.Parse(lastLOFDepDate);

            var birthDate1 = DateTime.Parse(birthDate);
            // Calculate the age.
            var age = travelDate.Year - birthDate1.Year;
            // Go back to the year the person was born in case of a leap year
            if (birthDate1 > travelDate.AddYears(-age)) age--;

            bool isInfantTurningTwo = age <= 2 && birthDate1.AddYears(2) < travelDate;  // age validation for infant turning two years of age before the trip

            return (age, isInfantTurningTwo);
        }

        private MOBOnScreenAlert BuildOnScreenAlertForChildTurningTwo(int paxId, MOBRegisterTravelersRequest request, List<CMSContentMessage> contentMessages)
        {
            string travelerFirstName = request.Travelers.FirstOrDefault(x => x.PaxID == paxId).FirstName.ToLower().ToPascalCase();
            return new MOBOnScreenAlert()
            {
                AlertType = MOBOnScreenAlertType.CONFIRMINFANTTRAVELTYPE,
                Title = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.InfantSeatPurchase.AlertTitle") ?? "Seat purchase required",
                Message = string.Format(_shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.InfantTurningTwoSeatPurchase.AlertMessage") ?? "Infants who turn two before the end of a trip cannot sit in a lap. {0} will need their own seat for this reservation.", travelerFirstName),
                PaxId = paxId,
                Actions = new List<MOBOnScreenActions>()
                {
                    new MOBOnScreenActions
                    {
                        ActionTitle = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.InfantBuySeat.BuySeatText") ?? "Buy a seat",
                        ActionType = MOBOnScreenAlertActionType.BUY_A_SEAT,
                    },
                    new MOBOnScreenActions
                    {
                        ActionTitle = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.UpdateTravelers.UpdateTravelersText") ?? "Update travelers",
                        ActionType = MOBOnScreenAlertActionType.DISMISS_ALERT,
                    }
                }
            };
        }

        private MOBOnScreenAlert BuildOnScreenAlertForInfantWithSeat(int paxId, MOBRegisterTravelersRequest request, List<CMSContentMessage> contentMessages)
        {
            string travelerFirstName = request.Travelers.FirstOrDefault(x => x.PaxID == paxId).FirstName.ToLower().ToPascalCase();
            return new MOBOnScreenAlert()
            {
                AlertType = MOBOnScreenAlertType.CONFIRMINFANTTRAVELTYPE,
                Title = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.InfantSeatPurchase.AlertTitle") ?? "Seat purchase required",
                Message = string.Format(_shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.InfantSeatPurchase.AlertMessage") ?? "There can only be one infant on lap per adult on your reservation. In order to continue, {0} will need a seat.", travelerFirstName),
                PaxId = paxId,
                Actions = new List<MOBOnScreenActions>()
                { 
                    new MOBOnScreenActions
                    {
                       ActionTitle = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.InfantBuySeat.BuySeatText") ?? "Buy a seat",
                       ActionType = MOBOnScreenAlertActionType.BUY_A_SEAT,
                    },
                    new MOBOnScreenActions
                    {
                       ActionTitle = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.UpdateTravelers.UpdateTravelersText") ?? "Update travelers",
                       ActionType = MOBOnScreenAlertActionType.DISMISS_ALERT,
                    }
                }
            };
                
        }

        private MOBOnScreenAlert BuildOnScreenAlertForInfantInLap(int paxId, MOBRegisterTravelersRequest request, List<CMSContentMessage> contentMessages)
        {
            string travelerFullName = string.Join(" ", request.Travelers.FirstOrDefault(x => x.PaxID == paxId).FirstName.ToLower().ToPascalCase(), request.Travelers.FirstOrDefault(x => x.PaxID == paxId).LastName.ToLower().ToPascalCase());
            return new MOBOnScreenAlert()
            {
                AlertType = MOBOnScreenAlertType.CONFIRMINFANTTRAVELTYPE,
                Title = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.InfantTraveler.AlertTitle") ?? "Infant traveler",
                Message = string.Format(_shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.InfantTraveler.AlertMessage") ?? "{0} is younger than two years old. Please let us know if they will be in a lap or need a seat.", travelerFullName),
                PaxId = paxId,
                Actions = new List<MOBOnScreenActions>()
                {
                    new MOBOnScreenActions
                    {
                       ActionTitle = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.InfanInLap.AddInfantInLapText") ?? "Select infant in lap",
                       ActionType = MOBOnScreenAlertActionType.ADD_INFANT_IN_LAP,
                    },
                    new MOBOnScreenActions
                    {
                       ActionTitle = _shoppingUtility.GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.InfantBuySeat.BuySeatText") ?? "Buy a seat",
                       ActionType = MOBOnScreenAlertActionType.BUY_A_SEAT,
                    }
                }
            };
        }

        private void CheckTotalInfantsCount(MOBRegisterTravelersRequest request)
        {
            int adultSeniorCount = request.Travelers.Where(x => x.TravelerTypeCode.Equals("ADT") || x.TravelerTypeCode.Equals("SNR")).Count();
            if (adultSeniorCount < request.Travelers.Where(x => x.TravelerTypeCode.Equals("INF")).Count() || (request.Travelers.Any(x => x.TravelerTypeCode.Equals("INS"))
                                                                                                                                && adultSeniorCount == 0))
                throw new MOBUnitedException("9999", _configuration.GetValue<string>("UnaccompaniedDisclaimerMessage"));
        }

        private async Task<MOBShoppingCart> UpdateMMInSession(MOBShoppingCart shoppingCart, MOBRegisterTravelersRequest request, MOBApplication application, Session session)
        {
            if (_travelerUtility.IncludeMoneyPlusMiles(application.Id, application.Version.Major) && request.ContinueToChangeTravelers)
            {
                if (shoppingCart != null && shoppingCart.FormofPaymentDetails?.MoneyPlusMilesCredit?.SelectedMoneyPlusMiles != null)
                {
                    ApplyMoneyPlusMilesOptionRequest milesRequest = new ApplyMoneyPlusMilesOptionRequest();
                    milesRequest.OptionId = null; // for undo moneymiles send optionid as null
                    milesRequest.Application = request.Application;
                    milesRequest.CartId = request.CartId;
                    milesRequest.AccessCode = request.AccessCode;
                    //milesRequest.CartKey = request.CartKey;
                    milesRequest.DeviceId = request.DeviceId;
                    milesRequest.SessionId = request.SessionId;
                    milesRequest.TransactionId = request.TransactionId;
                    milesRequest.Flow = request.Flow;
                    //milesRequest.PointOfSale = request.PointOfSale;
                    var milesResponse = await ApplyMilesPlusMoneyOption(session, milesRequest);
                    shoppingCart.FormofPaymentDetails.MoneyPlusMilesCredit.SelectedMoneyPlusMiles = null;
                    if (_shoppingUtility.IsEnableMoneyPlusMilesFeature(request.Application.Id, request.Application.Version.Major, session?.CatalogItems))
                    {
                        session.IsMoneyPlusMilesSelected = false;
                        await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);
                    }
                }
            }
            return shoppingCart;
        }
        private async Task<FOPResponse> ApplyMilesPlusMoneyOption(Session session, ApplyMoneyPlusMilesOptionRequest request)
        {
            string milesMoneyErrorMessage = string.Empty;
            FOPResponse milesPlusMoneyCreditResponse = new FOPResponse();
            try
            {
                // MOBILE-15202 Error Message 
                List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                milesMoneyErrorMessage = ShopStaticUtility.GetSDLMessageFromList(lstMessages, "RTI.MoneyPlusMilesCredits.ReviewMMCMessage.ErrorMsg").FirstOrDefault()?.ContentFull;
                var response = await ApplyCSLMilesPlusMoneyOptions(session, request, request.OptionId, milesMoneyErrorMessage);
                var shoppingCart = new MOBShoppingCart();
                shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, shoppingCart.ObjectName, new List<string> { request.SessionId, shoppingCart.ObjectName }).ConfigureAwait(false);
                await AssignUpdatedPricesToReservation(session.SessionId, response, request.OptionId, shoppingCart.Products);
                await LoadSessionValuesToResponse(session, milesPlusMoneyCreditResponse, null, request.OptionId, shoppingCart, lstMessages);
                milesPlusMoneyCreditResponse.PkDispenserPublicKey = await _pKDispenserPublicKey.GetCachedOrNewpkDispenserPublicKey(request.Application.Id, request.Application.Version.Major, request.DeviceId, session.SessionId, session.Token, session.CatalogItems);
            }
            catch
            {
                throw new MOBUnitedException(milesMoneyErrorMessage != null ? milesMoneyErrorMessage : _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            return milesPlusMoneyCreditResponse;
        }
        private async Task LoadSessionValuesToResponse(Session session, FOPResponse response, FOPMoneyPlusMilesCredit moneyPlusMilesCredit = null, string optionId = "", MOBShoppingCart shoppingCart = null, List<CMSContentMessage> lstMessages = null)
        {
            response.ShoppingCart = shoppingCart ?? await _sessionHelperService.GetSession<MOBShoppingCart>(session.SessionId, new MOBShoppingCart().ObjectName, new List<string> { session.SessionId, new MOBShoppingCart().ObjectName }).ConfigureAwait(false);
            if (response.ShoppingCart == null)
            {
                response.ShoppingCart = new MOBShoppingCart();
            }
            if (response.ShoppingCart?.FormofPaymentDetails == null)
            {
                response.ShoppingCart.FormofPaymentDetails = new MOBFormofPaymentDetails();
            }

            if (moneyPlusMilesCredit != null)
            {
                response.ShoppingCart.FormofPaymentDetails.MoneyPlusMilesCredit = moneyPlusMilesCredit;
            }
            else
            {
                if (!string.IsNullOrEmpty(optionId))
                {
                    response.ShoppingCart.FormofPaymentDetails.MoneyPlusMilesCredit.SelectedMoneyPlusMiles = response.ShoppingCart.FormofPaymentDetails.MoneyPlusMilesCredit.MilesPlusMoneyOptions.FirstOrDefault(mm => mm.OptionId == optionId);
                    var changeInTravelerMessage = string.Empty;
                    changeInTravelerMessage = ShopStaticUtility.GetSDLMessageFromList(lstMessages, "RTI.MoneyPlusMilesCredits.MMCMessage.RemoveTraveler")[0].ContentFull;
                    response.ShoppingCart.FormofPaymentDetails.MoneyPlusMilesCredit.PromoCodeMoneyMilesAlertMessage = string.IsNullOrWhiteSpace(changeInTravelerMessage) ? null : new Section
                    {
                        Text1 = changeInTravelerMessage,
                        Text2 = "Cancel",
                        Text3 = "Continue"
                    };
                }
                else
                {
                    response.ShoppingCart.FormofPaymentDetails.MoneyPlusMilesCredit = null;
                }
            }
            await _sessionHelperService.SaveSession(response.ShoppingCart, session.SessionId, new List<string> { session.SessionId, response.ShoppingCart.ObjectName }, response.ShoppingCart.ObjectName).ConfigureAwait(false);

            response.SessionId = session.SessionId;
            response.Flow = string.IsNullOrEmpty(session.Flow) ? response.ShoppingCart.Flow : session.Flow;
            response.Reservation = new MOBSHOPReservation(_configuration, _cachingService);
            response.Reservation = await _shoppingUtility.GetReservationFromPersist(response.Reservation, session.SessionId).ConfigureAwait(false);


            ProfileResponse persistedProfileResponse = new ProfileResponse();
            persistedProfileResponse = await _sessionHelperService.GetSession<ProfileResponse>(session.SessionId, persistedProfileResponse.ObjectName, new List<string> { session.SessionId, persistedProfileResponse.ObjectName }).ConfigureAwait(false);
            response.Profiles = persistedProfileResponse != null ? persistedProfileResponse.Response.Profiles : null;
        }

        private async Task AssignUpdatedPricesToReservation(string sessionId, FlightReservationResponse cslReservation, string optionId, List<ProdDetail> products)
        {
            Reservation bookingPathReservation = new Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionId, bookingPathReservation.ObjectName, new List<string> { sessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

            List<DisplayPrice> displayPrices = cslReservation?.DisplayCart?.DisplayPrices;

            if (displayPrices != null)
            {
                foreach (var price in displayPrices)
                {
                    if (string.IsNullOrEmpty(price.Description))
                    {
                        price.Description = price.Type;
                    }
                }
            }

            BuildMoneyPlusMilesPrice(bookingPathReservation.Prices, displayPrices, string.IsNullOrEmpty(optionId), products);
            _travelerUtility.AddGrandTotalIfNotExistInPrices(bookingPathReservation.Prices);
            await _sessionHelperService.SaveSession(bookingPathReservation, sessionId, new List<string> { sessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
        }
        private bool BuildMoneyPlusMilesPrice(List<MOBSHOPPrice> prices, List<DisplayPrice> displayPrices, bool isRemove, List<ProdDetail> products)
        {
            bool isDirty = false;
            double mmValue = 0;
            if (isRemove)
            {
                var mmPrice = prices.FirstOrDefault(p => p.DisplayType == "MILESANDMONEY");
                if (mmPrice != null)
                {
                    mmValue = mmPrice.Value;
                    prices.Remove(mmPrice);
                    isDirty = true;
                }

                if (_configuration.GetValue<bool>("EnableFSRMoneyPlusMilesFeature"))
                {
                    var mplusmPrice = prices.FirstOrDefault(p => p.DisplayType == "MONEYPLUSMILES");
                    if (mplusmPrice != null)
                    {
                        prices.Remove(mplusmPrice);
                        isDirty = true;
                    }
                }

                // response.Reservation.Prices.Remove(moneyPlusMiles)
            }
            else
            {
                var mmPrice = displayPrices.FirstOrDefault(p => p.Type.Equals("MILESANDMONEY", StringComparison.OrdinalIgnoreCase));
                if (mmPrice != null && mmPrice.Amount > 0)
                {
                    MOBSHOPPrice bookingPrice = new MOBSHOPPrice();
                    bookingPrice.CurrencyCode = mmPrice.Currency;
                    bookingPrice.DisplayType = mmPrice.Type;
                    bookingPrice.Status = mmPrice.Status;
                    bookingPrice.Waived = mmPrice.Waived;
                    bookingPrice.DisplayValue = string.Format("{0:#,0.00}", mmPrice.Amount);
                    bookingPrice.PriceTypeDescription = "Miles applied";

                    double tempDouble = 0;
                    double.TryParse(mmPrice.Amount.ToString(), out tempDouble);
                    bookingPrice.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);
                    CultureInfo ci = TopHelper.GetCultureInfo(mmPrice.Currency);
                    bookingPrice.FormattedDisplayValue = "-" + TopHelper.FormatAmountForDisplay(mmPrice.Amount.ToString(), ci, false); // Money and Miles have to be displayed in -ve format as per MOBILE-14807
                    prices.Add(bookingPrice);
                    mmValue = bookingPrice.Value;
                    isDirty = true;
                }
            }

            if (isDirty)
            {
                var price = prices.FirstOrDefault(p => p.DisplayType == "RES");
                ShopStaticUtility.UpdateCertificateRedeemAmountFromTotalInReserationPrices(price, mmValue, !isRemove);
                price = prices.FirstOrDefault(p => p.DisplayType == "TOTAL");
                ShopStaticUtility.UpdateCertificateRedeemAmountFromTotalInReserationPrices(price, mmValue, !isRemove);
                price = prices.FirstOrDefault(p => p.DisplayType.ToUpper().Equals("GRAND TOTAL"));
                ShopStaticUtility.UpdateCertificateRedeemAmountFromTotalInReserationPrices(price, mmValue, !isRemove);
                var scRESProduct = products.Find(p => p.Code == "RES");
                ShopStaticUtility.UpdateCertificateRedeemAmountInSCProductPrices(scRESProduct, mmValue, !isRemove);
            }
            return isDirty;

        }

        private async Task<FlightReservationResponse> ApplyCSLMilesPlusMoneyOptions(Session session, MOBRequest mobRequest, string optionId, string milesMoneyErrorMessage)
        {
            FlightReservationResponse response = new FlightReservationResponse();
            string actionName = string.IsNullOrEmpty(optionId) ? "UndoMilesAndMoneyOption" : "ApplyMilesAndMoneyOption";
            ApplyMilesAndMoneyOptionRequest cslRequest = new ApplyMilesAndMoneyOptionRequest();
            cslRequest.CartId = session.CartId;
            var cartInfo = await _travelerUtility.GetCartInformation(session.SessionId, mobRequest.Application, session.DeviceID, session.CartId, session.Token);
            cslRequest.Reservation = cartInfo.Reservation;
            cslRequest.DisplayCart = cartInfo.DisplayCart;
            cslRequest.OptionId = optionId;

            string jsonRequest = JsonConvert.SerializeObject(cslRequest);
            response = await _getProductsService.MilesAndMoneyOption<FlightReservationResponse>(session.Token, actionName, jsonRequest, session.SessionId).ConfigureAwait(false);
            await RegisterFlights(response, session, mobRequest, milesMoneyErrorMessage);
            return response;
        }
        private async Task<FlightReservationResponse> RegisterFlights(FlightReservationResponse flightReservationResponse, Session session, MOBRequest request, string milesMoneyErrorMessage)
        {
            //string cslEndpoint = _configuration.GetValue<string>("ServiceEndPointBaseUrl - ShoppingCartService");
            string flow = session.Flow;
            var registerFlightRequest = BuildRegisterFlightRequest(flightReservationResponse, flow, request);
            string jsonRequest = JsonConvert.SerializeObject(registerFlightRequest);
            FlightReservationResponse response = new FlightReservationResponse();

            string actionName = "RegisterFlights";

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cSLCallDurationstopwatch1;
            cSLCallDurationstopwatch1 = new Stopwatch();
            cSLCallDurationstopwatch1.Reset();
            cSLCallDurationstopwatch1.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            response = await _shoppingCartService.RegisterFlights<FlightReservationResponse>(session.Token, actionName, jsonRequest, session.SessionId).ConfigureAwait(false);

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
            }
            else
            {
                throw new MOBUnitedException(!string.IsNullOrEmpty(milesMoneyErrorMessage) ? milesMoneyErrorMessage : _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            return response;
        }
        private RegisterFlightsRequest BuildRegisterFlightRequest(FlightReservationResponse flightReservationResponse, string flow, MOBRequest mobRequest)
        {
            RegisterFlightsRequest request = new RegisterFlightsRequest();
            request.CartId = flightReservationResponse.DisplayCart.CartId;
            request.CartInfo = flightReservationResponse.DisplayCart;
            request.CountryCode = flightReservationResponse.DisplayCart.CountryCode;
            request.Reservation = flightReservationResponse.Reservation;
            request.DeviceID = mobRequest.DeviceId;
            request.Upsells = flightReservationResponse.Upsells;
            request.MerchOffers = flightReservationResponse.MerchOffers;
            request.LoyaltyUpgradeOffers = flightReservationResponse.LoyaltyUpgradeOffers;
            request.WorkFlowType = _shoppingUtility.GetWorkFlowType(flow);
            request.AppliedMilesAndMoney = true;
            return request;
        }

        private async Task UpdateFFCInSession(Reservation bookingPathReservation, MOBShoppingCart shoppingCart, bool continueToChangeTravelers, MOBApplication application, string sessionid, List<MOBSHOPPrice> prices)
        {
            if (_travelerUtility.IncludeFFCResidual(application.Id, application.Version.Major) && continueToChangeTravelers)
            {
                if (bookingPathReservation != null)
                {
                    var shoppingCartLocal = await _sessionHelperService.GetSession<MOBShoppingCart>(sessionid, new MOBShoppingCart().ObjectName, new List<string> { sessionid, new MOBShoppingCart().ObjectName }).ConfigureAwait(false);
                    if (shoppingCartLocal?.FormofPaymentDetails?.TravelFutureFlightCredit?.FutureFlightCredits?.Count() > 0)
                    {
                        shoppingCartLocal.FormofPaymentDetails.TravelFutureFlightCredit.FutureFlightCredits = null;
                        UpdatePricesInReservation(shoppingCartLocal.FormofPaymentDetails.TravelFutureFlightCredit, bookingPathReservation.Prices);
                    }
                }

                if (shoppingCart != null && shoppingCart.FormofPaymentDetails?.TravelFutureFlightCredit?.FutureFlightCredits?.Count > 0)
                {
                    shoppingCart.FormofPaymentDetails.TravelFutureFlightCredit.FutureFlightCredits = null;
                    AssignIsOtherFOPRequired(shoppingCart.FormofPaymentDetails, prices);
                    _fFCShoppingcs.AssignFormOfPaymentType(shoppingCart.FormofPaymentDetails, prices, shoppingCart.FormofPaymentDetails?.SecondaryCreditCard != null, true);
                }
            }
        }


        private void AssignIsOtherFOPRequired(MOBFormofPaymentDetails formofPaymentDetails, List<MOBSHOPPrice> prices)
        {
            var grandTotalPrice = prices.FirstOrDefault(p => p.DisplayType.ToUpper().Equals("GRAND TOTAL"));
            if (grandTotalPrice != null)
                formofPaymentDetails.IsOtherFOPRequired = (grandTotalPrice.Value > 0);
        }
        private void UpdatePricesInReservation(FOPTravelFutureFlightCredit travelFutureFlightCredit, List<MOBSHOPPrice> prices)
        {

            var ffcPrice = prices.FirstOrDefault(p => p.DisplayType.ToUpper() == "FFC");
            var totalCreditFFC = prices.FirstOrDefault(p => p.DisplayType.ToUpper() == "REFUNDPRICE");
            //var scRESProduct = response.ShoppingCart.Products.Find(p => p.Code == "RES");
            var grandtotal = prices.FirstOrDefault(p => p.DisplayType.ToUpper() == "GRAND TOTAL");

            if (ffcPrice == null && travelFutureFlightCredit.TotalRedeemAmount > 0)
            {
                ffcPrice = new MOBSHOPPrice();
                prices.Add(ffcPrice);
            }
            else if (ffcPrice != null)
            {
                //ETCUtility.UpdateCertificateRedeemAmountInSCProductPrices(scRESProduct, ffcPrice.Value, false);
                ShopStaticUtility.UpdateCertificateRedeemAmountFromTotalInReserationPrices(grandtotal, ffcPrice.Value, false);
            }

            if (totalCreditFFC != null)
                prices.Remove(totalCreditFFC);

            if (travelFutureFlightCredit.TotalRedeemAmount > 0)
            {
                UpdateCertificatePrice(ffcPrice, travelFutureFlightCredit.TotalRedeemAmount, "FFC", "Future Flight Credit", isAddNegative: true);
                //Build Total Credit item
                double totalCreditValue = travelFutureFlightCredit.FutureFlightCredits.Sum(ffc => ffc.NewValueAfterRedeem);
                if (totalCreditValue > 0)
                {
                    totalCreditFFC = new MOBSHOPPrice();
                    prices.Add(totalCreditFFC);
                    UpdateCertificatePrice(totalCreditFFC, totalCreditValue, "REFUNDPRICE", "Total Credit", "RESIDUALCREDIT");
                }
                //ETCUtility.UpdateCertificateRedeemAmountInSCProductPrices(scRESProduct, travelFutureFlightCredit.TotalRedeemAmount);
                ShopStaticUtility.UpdateCertificateRedeemAmountFromTotalInReserationPrices(grandtotal, travelFutureFlightCredit.TotalRedeemAmount);
            }
            else
            {
                prices.Remove(ffcPrice);
            }
        }
        private MOBSHOPPrice UpdateCertificatePrice(MOBSHOPPrice ffc, double totalAmount, string priceType, string priceTypeDescription, string status = "", bool isAddNegative = false)
        {
            ffc.CurrencyCode = "USD";
            ffc.DisplayType = priceType;
            ffc.PriceType = priceType;
            ffc.Status = status;
            ffc.PriceTypeDescription = priceTypeDescription;
            ffc.Value = totalAmount;
            ffc.Value = Math.Round(ffc.Value, 2, MidpointRounding.AwayFromZero);
            ffc.FormattedDisplayValue = (isAddNegative ? "-" : "") + (ffc.Value).ToString("C2", CultureInfo.CurrentCulture);
            ffc.DisplayValue = string.Format("{0:#,0.00}", ffc.Value);
            return ffc;
        }
        private void ValidateYATravel(List<MOBCPTraveler> travelerCSLBeforeRegister, List<MOBCPTraveler> travelers)
        {
            if (travelers.Count > 1)
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage") ?? "Sorry, something went wrong. Please try again.");
            if (travelerCSLBeforeRegister != null && travelerCSLBeforeRegister.Count > 0)
            {
                foreach (var t in travelerCSLBeforeRegister)
                {
                    if (t.IsProfileOwner)
                    {
                        if (!t.FirstName.ToUpper().Equals(travelers[0].FirstName.ToUpper()) || !t.LastName.ToUpper().Equals(travelers[0].LastName.ToUpper()) ||
                             !t.GenderCode.Equals(travelers[0].GenderCode))
                            throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage") ?? "Sorry, something went wrong. Please try again.");

                        //if (!t.Value.MileagePlus.MileagePlusId.ToUpper().Equals(travelers[0].MileagePlus.MileagePlusId.ToUpper()) )
                        //    new MOBUnitedException(ConfigurationManager.AppSettings["Booking2OGenericExceptionMessage"] ?? "Sorry, something went wrong. Please try again.");

                        break;
                    }
                }
            }
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
        private async Task<bool> AddOrRemovePromo(MOBRequest request, Session session, bool isRemove, string flow)
        {
            try
            {

                MOBShoppingCart persistedShoppingCart = new MOBShoppingCart();
                persistedShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(session.SessionId, persistedShoppingCart.ObjectName, new List<string> { session.SessionId, persistedShoppingCart.ObjectName }).ConfigureAwait(false);
                List<MOBPromoCode> promoCodes = new List<MOBPromoCode>();
                bool isAdvanceSearchCoupon = _shoppingUtility.EnableAdvanceSearchCouponBooking(request.Application.Id, request.Application.Version.Major);

                var persistedApplyPromoCodeResponse = new ApplyPromoCodeResponse();
                persistedApplyPromoCodeResponse = await _sessionHelperService.GetSession<ApplyPromoCodeResponse>(session.SessionId, persistedApplyPromoCodeResponse.ObjectName, new List<string> { session.SessionId, persistedApplyPromoCodeResponse.ObjectName }).ConfigureAwait(false);


                if (persistedApplyPromoCodeResponse == null
                   || (persistedApplyPromoCodeResponse?.ShoppingCart?.PromoCodeDetails?.PromoCodes != null
                    && persistedApplyPromoCodeResponse?.ShoppingCart?.PromoCodeDetails?.PromoCodes.Count == 0)) //Do nothing if there is no coupon.
                {
                    if (isAdvanceSearchCoupon && persistedShoppingCart?.PromoCodeDetails != null && persistedShoppingCart?.PromoCodeDetails?.PromoCodes != null && persistedShoppingCart?.PromoCodeDetails?.PromoCodes.Count > 0)
                    {
                        promoCodes = persistedShoppingCart?.PromoCodeDetails?.PromoCodes;
                    }
                    else
                        return true;
                }
                ApplyPromoCodeRequest promoCodeRequest = new ApplyPromoCodeRequest();
                if (isAdvanceSearchCoupon)
                {
                    if (promoCodes == null || promoCodes?.Count == 0)
                        promoCodes = persistedApplyPromoCodeResponse.ShoppingCart?.PromoCodeDetails?.PromoCodes;
                }
                else
                    promoCodes = persistedApplyPromoCodeResponse.ShoppingCart?.PromoCodeDetails?.PromoCodes;

                promoCodeRequest = BuildApplyPromoCodeRequest(request, session, isRemove, promoCodes, flow);
                var tupleResponse = await ApplyPromoCode(promoCodeRequest, session, true);
                if (tupleResponse.inEligibleReason != null && !string.IsNullOrEmpty(tupleResponse.inEligibleReason.CurrentValue))
                    return false;
            }
            catch (System.Net.WebException wex)
            {
                throw wex;
            }
            catch (MOBUnitedException uaex)
            {
                throw uaex;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
            finally
            {
                //if (LogEntries != null && LogEntries.Count > 0)
                //{
                //    logEntries.AddRange(LogEntries);
                //}
            }
            return true;
        }
        private async Task<(ApplyPromoCodeResponse response, MOBItem inEligibleReason)> ApplyPromoCode(ApplyPromoCodeRequest request, Session session, bool isByPassValidations = false)
        {
            ApplyPromoCodeResponse response = new ApplyPromoCodeResponse();
            var persistShoppingCart = new MOBShoppingCart();
            Reservation bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, (new Reservation()).ObjectName, new List<string> { request.SessionId, (new Reservation()).ObjectName }).ConfigureAwait(false);
            persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, persistShoppingCart.ObjectName, new List<string> { request.SessionId, persistShoppingCart.ObjectName }).ConfigureAwait(false);
            var inEligibleReason = new MOBItem();
            FlightReservationResponse cslFlightReservationResponse = new FlightReservationResponse();
            try
            {
                #region coupon Service Call        

                if (request.PromoCodes.Any(p => p.IsRemove))
                {
                    if (IsUpliftFopAdded(persistShoppingCart) && !isByPassValidations)
                    {
                        throw new MOBUnitedException(_configuration.GetValue<string>("RemovePromo_UpliftAddedMessage"));
                    }
                    cslFlightReservationResponse = await RegisterOrRemoveCoupon(request, session, true);
                    if (_shoppingUtility.EnableAdvanceSearchCouponBooking(request.Application.Id, request.Application.Version.Major))
                    {
                        persistShoppingCart.PromoCodeDetails = new MOBPromoCodeDetails
                        {
                            PromoCodes = null,
                            IsHidePromoOption = false,
                            IsDisablePromoOption = false
                        };
                    }
                }
                if (request.PromoCodes.Any(p => !p.IsRemove))
                {
                    if (IsMaxPromoCountReached(persistShoppingCart))
                    {
                        response.MaxCountMessage = _configuration.GetValue<string>("MaxPromoCodeMessage");
                        response.ShoppingCart = persistShoppingCart;
                        response.Reservation = new MOBSHOPReservation(_configuration, _cachingService);
                        response.Reservation = await _shoppingUtility.GetReservationFromPersist(response.Reservation, session.SessionId).ConfigureAwait(false);
                        return (response, inEligibleReason);
                    }
                    cslFlightReservationResponse = await RegisterOrRemoveCoupon(request, session, false);
                }
                #endregion coupon Service Call
                #region Update Prices with CouponDetails    
                var persistedSCproducts = persistShoppingCart.Products.Clone();
                persistShoppingCart.Products = await _productInfoHelper.ConfirmationPageProductInfo(cslFlightReservationResponse, false, request.Application, null, request.Flow);
                UpdatePricesWithPromo(cslFlightReservationResponse, bookingPathReservation.Prices.Clone(), persistedSCproducts, persistShoppingCart.Products, bookingPathReservation, session);
                AddFreeBagDetails(persistShoppingCart, bookingPathReservation);
                #endregion Update Prices with CouponDetails  
                #region TPI in booking path
                if (_travelerUtility.EnableTPI(request.Application.Id, request.Application.Version.Major, 3) && !bookingPathReservation.IsReshopChange)
                {
                    // call TPI 
                    try
                    {
                        string token = session.Token;
                        TPIInfoInBookingPath tPIInfo = await _travelerUtility.GetBookingPathTPIInfo(request.SessionId, request.LanguageCode, request.Application, request.DeviceId, request.CartId, token, true, false, false);
                        bookingPathReservation.TripInsuranceFile = new TripInsuranceFile();
                        bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo = tPIInfo;
                    }
                    catch (Exception ex)
                    {
                        bookingPathReservation.TripInsuranceFile = null;
                    }
                }
                #endregion
                #region Save Reservation & ShoppingCart              
                double price = _shoppingUtility.GetGrandTotalPriceForShoppingCart(false, cslFlightReservationResponse, false, request.Flow);
                persistShoppingCart.TotalPrice = String.Format("{0:0.00}", price);
                persistShoppingCart.DisplayTotalPrice = Decimal.Parse(price.ToString()).ToString("c");
                AssignWarningMessage(bookingPathReservation.ShopReservationInfo2, cslFlightReservationResponse.Warnings);
                #region Update the chase banner price total
                if (_configuration.GetValue<bool>("EnableChaseOfferRTI") && (!_configuration.GetValue<bool>("EnableChaseOfferRTIVersionCheck") ||
                (_configuration.GetValue<bool>("EnableChaseOfferRTIVersionCheck") && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("AndroidEnableChaseOfferRTIVersion"), _configuration.GetValue<string>("iPhoneEnableChaseOfferRTIVersion")))))
                {
                    if (bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement != null)
                    {
                        var objPrice = bookingPathReservation.Prices.FirstOrDefault(p => p.PriceType.ToUpper().Equals("GRAND TOTAL"));
                        if (objPrice != null)
                        {
                            decimal grandTotalPrice = Convert.ToDecimal(objPrice.Value);
                            if (_configuration.GetValue<bool>("TurnOffChaseBugMOBILE-11134"))
                            {
                                bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement.finalAfterStatementDisplayPrice = _travelerUtility.GetPriceAfterChaseCredit(grandTotalPrice);
                            }
                            else
                            {
                                bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement.finalAfterStatementDisplayPrice = _travelerUtility.GetPriceAfterChaseCredit(grandTotalPrice, bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement.statementCreditDisplayPrice);
                            }
                            bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement.initialDisplayPrice = price.ToString("c", new CultureInfo("en-us"));
                        }
                    }
                }
                #endregion Update the chase banner price Total
                await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, session.SessionId, new List<string> { session.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, session.SessionId, new List<string> { session.SessionId, persistShoppingCart.ObjectName }, persistShoppingCart.ObjectName).ConfigureAwait(false);
                #endregion Save Reservation & ShoppingCart          

            }
            catch (System.Net.WebException wex)
            {
                throw wex;
            }
            catch (MOBUnitedException uaex)
            {
                //If it is a soft failure from service it should be thrown as united exception .
                //But, no need to rethrow back to contrller since client shows Soft failures from service as inline instead of popup
                if (!string.IsNullOrEmpty(uaex.Code) && (uaex.Code == "2000" || uaex.Code == "3000"))
                {
                    inEligibleReason.Id = uaex.Code;
                    inEligibleReason.CurrentValue = uaex.Message;
                    MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);
                    _logger.LogError("ApplyPromoCode - MOBUnitedException {error} and SessionId {sessionId}", uaexWrapper, request.SessionId);
                }
                else
                {
                    throw uaex;
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
            response.ShoppingCart = persistShoppingCart;
            response.Reservation = new MOBSHOPReservation(_configuration, _cachingService);
            response.Reservation = await _shoppingUtility.GetReservationFromPersist(response.Reservation, session.SessionId).ConfigureAwait(false);
            #region Update individual amount in traveler
            UpdateTravelerIndividualAmount(response.Reservation, response.ShoppingCart, cslFlightReservationResponse);
            #endregion Update individual amount in traveler
            return (response, inEligibleReason);
        }
        private void AssignWarningMessage(ReservationInfo2 shopReservationInfo2, List<United.Services.FlightShopping.Common.ErrorInfo> Warnings)
        {
            if (shopReservationInfo2 == null)
            {
                shopReservationInfo2 = new ReservationInfo2();
            }
            if (shopReservationInfo2.InfoWarningMessages == null)
            {
                shopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();
            }
            InfoWarningMessages selectSeatWarningMessage = new InfoWarningMessages();

            if (shopReservationInfo2.InfoWarningMessages.Exists(x => x.Order == MOBINFOWARNINGMESSAGEORDER.RTIPROMOSELECTSEAT.ToString()))
            {
                shopReservationInfo2.InfoWarningMessages.Remove(shopReservationInfo2.InfoWarningMessages.Find(x => x.Order == MOBINFOWARNINGMESSAGEORDER.RTIPROMOSELECTSEAT.ToString()));
            }
            if (Warnings != null && Warnings.Any() && Warnings.Count > 0)
            {
                selectSeatWarningMessage = new InfoWarningMessages();
                selectSeatWarningMessage.Order = MOBINFOWARNINGMESSAGEORDER.RTIPROMOSELECTSEAT.ToString();
                selectSeatWarningMessage.IconType = MOBINFOWARNINGMESSAGEICON.WARNING.ToString();
                selectSeatWarningMessage.Messages = new List<string>();
                selectSeatWarningMessage.Messages.Add(Warnings.FirstOrDefault().Message);
                if (selectSeatWarningMessage != null)
                {
                    shopReservationInfo2.InfoWarningMessages.Add(selectSeatWarningMessage);
                    shopReservationInfo2.InfoWarningMessages = shopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                }
            }
        }
        private void AddFreeBagDetails(MOBShoppingCart shoppingCart, Reservation bookingPathReservation)
        {
            if (_configuration.GetValue<bool>("EnableCouponMVP2Changes"))
            {
                //Fare+Bag Coupon check for display type in coupondetails ,FreeBag Coupon check SC->Product has Bag product
                if (shoppingCart?.Products != null
                    && (shoppingCart.Products.Any(p => p.Code == "BAG")
                    || (shoppingCart.Products.Find(p => p.Code == "RES").CouponDetails != null && shoppingCart.Products.Find(p => p.Code == "RES").CouponDetails.Any()
                        && shoppingCart.Products.Find(p => p.Code == "RES").CouponDetails.First().Product == "BAG")))
                {
                    if (bookingPathReservation?.Prices != null)
                    {
                        bookingPathReservation.Prices.Add(new MOBSHOPPrice
                        {
                            PriceTypeDescription = _configuration.GetValue<string>("FreeBagCouponDescription"),
                            DisplayType = "TRAVELERPRICE",
                            FormattedDisplayValue = "",
                            DisplayValue = "",
                            Value = 0
                        });
                    }
                }
                else
                {
                    if (bookingPathReservation?.Prices != null)
                    {
                        var bagPriceItem = bookingPathReservation.Prices.Find(p => p.DisplayType == "TRAVELERPRICE"
                                                                                   && p.PriceTypeDescription.Equals(_configuration.GetValue<string>("FreeBagCouponDescription")));
                        if (bagPriceItem != null)
                        {
                            bookingPathReservation.Prices.Remove(bagPriceItem);
                        }
                    }
                }
            }
        }
        private void UpdateTravelerIndividualAmount(MOBSHOPReservation reservation, MOBShoppingCart ShoppingCart, FlightReservationResponse cslFlightReservationResponse)
        {
            if (reservation != null
                && ShoppingCart != null
                && cslFlightReservationResponse != null
                && ShoppingCart?.Products != null
                && ShoppingCart.Products.Any(p => p.Code == "RES" && p.CouponDetails != null)
                && cslFlightReservationResponse?.DisplayCart?.DisplayPrices != null)
            {

                //  ETCUtility etc = new ETCUtility(logEntries);
                AssignTravelerIndividualTotalAmount(reservation.TravelersCSL, cslFlightReservationResponse.DisplayCart.DisplayPrices, cslFlightReservationResponse.Reservation?.Travelers.ToList(), cslFlightReservationResponse.Reservation?.Prices?.ToList());
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
                            if (cslReservationPrice != null)
                            {
                                traveler.CslReservationPaxTypeCode = cslReservationPrice.PassengerTypeCode;
                                traveler.IndividualTotalAmount = cslReservationPrice.Totals.ToList().Find(t => t.Name.ToUpper() == "GRANDTOTALFORCURRENCY" && t.Currency.Code == "USD").Amount;
                            }
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
        private void UpdatePricesWithPromo(FlightReservationResponse cslFlightReservationResponse, List<MOBSHOPPrice> peristedPrices, List<ProdDetail> persistedSCproducts, List<ProdDetail> updatedSCproducts, Reservation bookingPathReservation, Session session)
        {

            UpdateFlightPriceWithPromo(cslFlightReservationResponse, bookingPathReservation.Prices, persistedSCproducts, updatedSCproducts, bookingPathReservation, session);
            UpdateSeatsPriceWithPromo(cslFlightReservationResponse, bookingPathReservation.Prices, bookingPathReservation.SeatPrices, persistedSCproducts, updatedSCproducts);
            UpdateGrandTotal(bookingPathReservation.Prices, peristedPrices);
        }
        private void UpdateGrandTotal(List<MOBSHOPPrice> prices, List<MOBSHOPPrice> persistedPrices)
        {
            var grandTotalPrice = prices.Where(p => p.DisplayType.ToUpper().Equals("GRAND TOTAL")).First();
            #region Find previous total promo and add it to GrandTotal
            double persistedPromoValue = 0;
            persistedPrices.ForEach(p =>
            {
                if (p.PromoDetails != null && (!(p.DisplayType.ToUpper().Equals("GRAND TOTAL") || p.DisplayType.ToUpper().Equals("TRAVELERPRICE"))))
                {
                    persistedPromoValue += p.PromoDetails.PromoValue;
                }
            });
            //Update grand total by adding the previous promovalue 
            grandTotalPrice.DisplayValue = string.Format("{0:#,0.00}", grandTotalPrice.Value + persistedPromoValue);
            grandTotalPrice.FormattedDisplayValue = string.Format("${0:c}", grandTotalPrice.DisplayValue);
            grandTotalPrice.Value = Math.Round(grandTotalPrice.Value + persistedPromoValue, 2, MidpointRounding.AwayFromZero);
            #endregion
            #region Update GrandTotal with new promo value
            double totalPromoVlaue = 0;
            prices.ForEach(p =>
            {
                if (p.PromoDetails != null && (!(p.DisplayType.ToUpper().Equals("GRAND TOTAL") || p.DisplayType.ToUpper().Equals("TRAVELERPRICE"))))
                {
                    totalPromoVlaue += p.PromoDetails != null ? p.PromoDetails.PromoValue : 0;
                }
            });
            grandTotalPrice.DisplayValue = string.Format("{0:#,0.00}", grandTotalPrice.Value - totalPromoVlaue);
            grandTotalPrice.FormattedDisplayValue = string.Format("{0:c}", grandTotalPrice.DisplayValue);
            grandTotalPrice.Value = Math.Round(grandTotalPrice.Value - totalPromoVlaue, 2, MidpointRounding.AwayFromZero);
            grandTotalPrice.PromoDetails = totalPromoVlaue > 0 ? new MOBPromoCode
            {
                PriceTypeDescription = _configuration.GetValue<string>("PromoSavedText"),
                PromoValue = totalPromoVlaue,
                FormattedPromoDisplayValue = totalPromoVlaue.ToString("C2", CultureInfo.CurrentCulture)
            } : null;
            #endregion update GrandTotal with new promo value
        }
        private void UpdateSeatsPriceWithPromo(FlightReservationResponse cslFlightReservationResponse, List<MOBSHOPPrice> prices, List<MOBSeatPrice> seatPrices, List<ProdDetail> persistedSCproducts, List<ProdDetail> updatedSCproducts)
        {
            double totalEplusPromoValue = 0,
                totalAdvanceSeatpromoValue = 0,
                totalPreferredSeatPromoValue = 0, promoValue = 0;

            if (prices != null
                && prices.Any(x => x.DisplayType.ToUpper().Contains("SEAT"))
                && IsPromoDetailsUpdatedByProduct(persistedSCproducts, updatedSCproducts, "SEATASSIGNMENTS"))
            {
                if (cslFlightReservationResponse.DisplayCart.DisplaySeats != null
                    && cslFlightReservationResponse.DisplayCart.DisplaySeats.Any())
                {
                    #region Get Promotion Value applied for Seat Category
                    foreach (var seat in cslFlightReservationResponse.DisplayCart.DisplaySeats)
                    {
                        promoValue = Convert.ToDouble(seat.OriginalPrice - seat.SeatPrice);
                        switch (ShopStaticUtility.GetCommonSeatCode(seat.SeatPromotionCode))
                        {
                            case "EPU":
                            case "PSL":
                                totalEplusPromoValue += promoValue;
                                break;
                            case "PZA":
                                totalPreferredSeatPromoValue += promoValue;
                                break;
                            case "ASA":
                                totalAdvanceSeatpromoValue += promoValue;
                                break;
                        }

                    }
                    #endregion Get Promotion Value applied for Seat Category
                    #region Update Reservation Prices(RTI)/SeatPrice for Price Breakdown
                    foreach (var price in prices)
                    {
                        if (price.DisplayType.ToUpper() == "ECONOMYPLUS SEATS")
                        {
                            price.PromoDetails = totalEplusPromoValue > 0 ? new MOBPromoCode
                            {
                                PromoValue = totalEplusPromoValue,
                                FormattedPromoDisplayValue = "-" + totalEplusPromoValue.ToString("C2", CultureInfo.CurrentCulture),
                                PriceTypeDescription = _configuration.GetValue<string>("PromoCodeAppliedText")
                            } : null;
                            if (seatPrices != null && seatPrices
                                .Exists(p => p.SeatMessage.ToUpper().Contains("ECONOMY PLUS") || p.SeatMessage.ToUpper().Contains("LIMITED RECLINE")))
                            {
                                seatPrices
                                .Where(p => p.SeatMessage.ToUpper().Contains("ECONOMY PLUS") || p.SeatMessage.ToUpper().Contains("LIMITED RECLINE"))
                                .First().SeatPromoDetails = price.PromoDetails;
                            }
                        }
                        if (price.DisplayType.ToUpper() == _configuration.GetValue<string>("PreferedSeat_PriceBreakdownTitle").ToUpper())
                        {
                            price.PromoDetails = totalPreferredSeatPromoValue > 0 ? new MOBPromoCode
                            {
                                PromoValue = totalPreferredSeatPromoValue,
                                FormattedPromoDisplayValue = "-" + totalPreferredSeatPromoValue.ToString("C2", CultureInfo.CurrentCulture),
                                PriceTypeDescription = _configuration.GetValue<string>("PromoCodeAppliedText")
                            } : null;

                            if (seatPrices != null && seatPrices
                                    .Exists(p => p.SeatMessage.ToUpper().Contains(_configuration.GetValue<string>("PreferedSeat_PriceBreakdownTitle").ToUpper())))
                            {
                                seatPrices
                                    .Where(p => p.SeatMessage.ToUpper().Contains(_configuration.GetValue<string>("PreferedSeat_PriceBreakdownTitle").ToUpper()))
                                    .First()
                                    .SeatPromoDetails = price.PromoDetails;
                            }
                        }
                        if (price.DisplayType.ToUpper() == "ADVANCE SEAT ASSIGNMENT")
                        {
                            price.PromoDetails = totalAdvanceSeatpromoValue > 0 ? new MOBPromoCode
                            {
                                PromoValue = totalAdvanceSeatpromoValue,
                                FormattedPromoDisplayValue = "-" + totalAdvanceSeatpromoValue.ToString("C2", CultureInfo.CurrentCulture),
                                PriceTypeDescription = _configuration.GetValue<string>("PromoCodeAppliedText")
                            } : null;
                            if (seatPrices != null && seatPrices.Exists(p => p.SeatMessage.ToUpper().Contains("ADVANCE SEAT ASSIGNMENT")))
                            {
                                seatPrices.Where(p => p.SeatMessage.ToUpper().Contains("ADVANCE SEAT ASSIGNMENT")).First().SeatPromoDetails = price.PromoDetails;
                            }
                        }
                    }
                    #endregion  Update Reservation Prices(RTI)/SeatPrice for Price Breakdown
                }
            }
        }
        private void UpdateFlightPriceWithPromo(FlightReservationResponse cslFlightReservationResponse, List<MOBSHOPPrice> prices, List<ProdDetail> persistedSCproducts, List<ProdDetail> updatedSCproducts, Reservation bookingPathReservation, Session session)
        {
            double promoValue = 0;
            if (prices != null && prices.Any(x => x.DisplayType.ToUpper().Contains("TRAVELERPRICE"))
                                && IsPromoDetailsUpdatedByProduct(persistedSCproducts, updatedSCproducts, "RES"))
            {
                var displayCartPrices = cslFlightReservationResponse.DisplayCart.DisplayPrices;
                if (displayCartPrices != null && displayCartPrices.Any())
                {
                    #region Update Reservation Prices(RTI) for Price breakdown
                    foreach (var price in prices)
                    {
                        if (displayCartPrices.Any(dp => dp.PaxTypeCode == price.PaxTypeCode && (dp.Type.ToUpper().Equals("TRAVELERPRICE") || dp.Type.ToUpper().Equals("TOTAL")))
                            && (price.DisplayType.Equals("TRAVELERPRICE") || (price.DisplayType.Equals("TOTAL"))))
                        {
                            if (price.DisplayType.Equals("TRAVELERPRICE"))
                            {
                                var nonDiscountedPrice = displayCartPrices.Find(dp => dp.PaxTypeCode == price.PaxTypeCode && dp.Type.ToUpper().Equals("NONDISCOUNTPRICE-TRAVELERPRICE"));
                                var discountedPrice = displayCartPrices.Find(dp => dp.PaxTypeCode == price.PaxTypeCode && dp.Type.ToUpper().Equals("TRAVELERPRICE"));
                                if (discountedPrice != null && nonDiscountedPrice != null)
                                {
                                    promoValue = Math.Round(Convert.ToDouble(nonDiscountedPrice.Amount)
                                                 - Convert.ToDouble(discountedPrice.Amount), 2, MidpointRounding.AwayFromZero);
                                }
                                else
                                {
                                    promoValue = 0;
                                }
                            }
                            if (price.DisplayType.Equals("TOTAL"))
                            {
                                var nonDiscountedTotalPrice = displayCartPrices.Find(dp => dp.PaxTypeCode == price.PaxTypeCode && dp.Type.ToUpper().Equals("NONDISCOUNTPRICE-TOTAL"));
                                var discountedTotalPrice = displayCartPrices.Find(dp => dp.PaxTypeCode == price.PaxTypeCode && dp.Type.ToUpper().Equals("TOTAL"));
                                if (discountedTotalPrice != null && nonDiscountedTotalPrice != null)
                                {
                                    promoValue = Math.Round(Convert.ToDouble(nonDiscountedTotalPrice.Amount)
                                                - Convert.ToDouble(discountedTotalPrice.Amount), 2, MidpointRounding.AwayFromZero);
                                    promoValue = _shoppingUtility.UpdatePromoValueForFSRMoneyMiles(displayCartPrices, session, promoValue);
                                }
                                else
                                {
                                    promoValue = 0;
                                }
                            }
                            price.PromoDetails = promoValue > 0 ? new MOBPromoCode
                            {
                                PriceTypeDescription = _configuration.GetValue<string>("PromoCodeAppliedText"),
                                PromoValue = Math.Round(promoValue, 2, MidpointRounding.AwayFromZero),
                                FormattedPromoDisplayValue = "-" + promoValue.ToString("C2", CultureInfo.CurrentCulture)
                            } : null;

                        }
                    }
                    #endregion  Update Reservation Prices(RTI)
                }
                #region update taxes               
                bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = _shoppingUtility.GetTaxAndFeesAfterPriceChange(cslFlightReservationResponse.DisplayCart.DisplayPrices, bookingPathReservation.IsReshopChange);
                #endregion update taxes
            }
        }
        private bool IsPromoDetailsUpdatedByProduct(List<ProdDetail> persistedSCproducts, List<ProdDetail> updatedSCproducts, string productCode)
        {
            if (persistedSCproducts.Any(p => p.Code == productCode && p.CouponDetails != null && p.CouponDetails.Count > 0)
                || updatedSCproducts.Any(p => p.Code == productCode && p.CouponDetails != null && p.CouponDetails.Count > 0))
            {
                return true;
            }
            return false;
        }
        private bool IsMaxPromoCountReached(MOBShoppingCart persistShoppingCart)
        {
            if (persistShoppingCart?.PromoCodeDetails?.PromoCodes != null
                && persistShoppingCart.PromoCodeDetails.PromoCodes.Any()
                && persistShoppingCart.PromoCodeDetails.PromoCodes.Count > 0)
            {
                return true;
            }
            return false;
        }
        private async Task<FlightReservationResponse> RegisterOrRemoveCoupon(ApplyPromoCodeRequest request, Session session, bool isRemove)
        {
            //string cslEndpoint = Utility.GetConfigEntries("ServiceEndPointBaseUrl - ShoppingCartService");
            var registerCouponRequest = BuildRegisterOrRemoveCouponRequest(request, isRemove);
            string jsonRequest = JsonConvert.SerializeObject(registerCouponRequest);
            //var response = new FlightReservationResponse();
            string cslActionName = isRemove ? "RemoveCoupon" : "RegisterCoupon";

            #region//****Get Call Duration Code*******
            Stopwatch cSLCallDurationstopwatch1;
            cSLCallDurationstopwatch1 = new Stopwatch();
            cSLCallDurationstopwatch1.Reset();
            cSLCallDurationstopwatch1.Start();
            #endregion//****Get Call Duration Code  03/17/2015*******

            var response = await _shoppingCartService.RegisterOrRemove<FlightReservationResponse>(session.Token, cslActionName, jsonRequest, session.SessionId).ConfigureAwait(false);

            #region//****Get Call Duration Code *******
            if (cSLCallDurationstopwatch1.IsRunning)
            {
                cSLCallDurationstopwatch1.Stop();
            }
            #endregion/***Get Call Duration Code*******

            if (response.response != null)
            {
                if (!(response.response != null && response.response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success) && response.response.DisplayCart != null && response.response.Reservation != null))
                {
                    if (response.response.Errors != null && response.response.Errors.Count > 0)
                    {
                        if (response.response.Errors.Any(error => (error.MajorCode == "2000" || error.MajorCode == "3000")))
                        {
                            var errors = response.response.Errors.Where(error => (error.MajorCode == "2000" || error.MajorCode == "3000")).FirstOrDefault();
                            throw new MOBUnitedException(errors.MajorCode, errors.Message);
                        }
                        else
                        {
                            string errorMessage = string.Empty;
                            foreach (var error in response.response.Errors)
                            {
                                errorMessage = errorMessage + " " + error.Message;
                            }
                            throw new System.Exception(errorMessage);
                        }
                    }
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            return response.response;
        }
        private RegisterCouponRequest BuildRegisterOrRemoveCouponRequest(ApplyPromoCodeRequest request, bool isRemove)
        {
            RegisterCouponRequest cslCouponRequest = new RegisterCouponRequest();
            cslCouponRequest.CartId = request.CartId;
            cslCouponRequest.Channel = _configuration.GetValue<string>("Shopping - ChannelType");
            cslCouponRequest.Requestor = new Service.Presentation.CommonModel.Requestor();
            cslCouponRequest.Requestor.ChannelID = _configuration.GetValue<string>("MerchandizeOffersCSLServiceChannelID");
            cslCouponRequest.Requestor.ChannelName = _configuration.GetValue<string>("MerchandizeOffersCSLServiceChannelName");
            cslCouponRequest.Requestor.ChannelSource = _configuration.GetValue<string>("RegisterCouponServiceChannelSource");
            cslCouponRequest.Coupons = new System.Collections.ObjectModel.Collection<Service.Presentation.ProductModel.ProductPromotion>();
            cslCouponRequest.Requestor.CountryCode = "US";
            cslCouponRequest.Requestor.LanguageCode = request.LanguageCode;
            request.PromoCodes.ForEach(x =>
            {
                if (isRemove)
                {
                    if (x.IsRemove)
                    {
                        cslCouponRequest.Coupons.Add(new ProductPromotion
                        {
                            Code = !_configuration.GetValue<bool>("DisableHandlingCaseSenstivity") ? x.PromoCode.ToUpper().Trim() : x.PromoCode.Trim()
                        });
                    }
                }
                else
                {
                    if (!x.IsRemove)
                    {
                        cslCouponRequest.Coupons.Add(new ProductPromotion
                        {
                            Code = !_configuration.GetValue<bool>("DisableHandlingCaseSenstivity") ? x.PromoCode.ToUpper().Trim() : x.PromoCode.Trim()
                        });
                    }
                }
            });
            cslCouponRequest.Requestor.LanguageCode = request.LanguageCode;
            cslCouponRequest.WorkFlowType = _shoppingUtility.GetWorkFlowType(request.Flow);
            return cslCouponRequest;
        }
        private bool IsUpliftFopAdded(MOBShoppingCart shoppingCart)
        {
            if (shoppingCart?.FormofPaymentDetails != null && shoppingCart.FormofPaymentDetails.FormOfPaymentType.ToUpper() == MOBFormofPayment.Uplift.ToString().ToUpper())
            {
                return true;
            }
            return false;
        }

        private ApplyPromoCodeRequest BuildApplyPromoCodeRequest(MOBRequest request, Session session, bool isRemove, List<MOBPromoCode> promoCodes, string flow)
        {
            ApplyPromoCodeRequest applyPromoCodeRequest = new ApplyPromoCodeRequest();
            applyPromoCodeRequest.CartId = session.CartId;
            applyPromoCodeRequest.SessionId = session.SessionId;
            applyPromoCodeRequest.TransactionId = request.TransactionId;
            applyPromoCodeRequest.DeviceId = request.DeviceId;
            applyPromoCodeRequest.Application = request.Application;
            applyPromoCodeRequest.LanguageCode = request.LanguageCode;
            applyPromoCodeRequest.Flow = flow;
            applyPromoCodeRequest.PromoCodes = new List<MOBPromoCode>();

            promoCodes.ForEach(x =>
            {
                if (isRemove)
                {
                    applyPromoCodeRequest.PromoCodes.Add(new MOBPromoCode
                    {
                        PromoCode = !_configuration.GetValue<bool>("DisableHandlingCaseSenstivity") ? x.PromoCode.ToUpper().Trim() : x.PromoCode.Trim(),
                        IsRemove = isRemove
                    });
                }
            });
            return applyPromoCodeRequest;
        }



        public async Task<MOBSHOPReservation> RegisterTravelersCSL(MOBRegisterTravelersRequest request, bool isRegisterOffersCall = false)
        {
            MOBSHOPReservation reservation = new MOBSHOPReservation(_configuration, _cachingService);
            #region
            if (request == null)
            {
                throw new MOBUnitedException("Register Travelers request cannot be null.");
            }
            if (request.ProfileOwner == null)
            {
                request = await _traveler.GetPopulateProfileOwnerData(request);
            }

            Reservation bookingPathReservation = new Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
            string nextViewName = _productInfoHelper.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, bookingPathReservation?.ShopReservationInfo2?.IsDisplayCart == true) ? _traveler.GetNextViewName(request, bookingPathReservation) : "";
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
                    bookingPathReservation.ShopReservationInfo2.PlacePass = await _traveler.GetPlacePass(destinationcode, bookingPathReservation.SearchType, request.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, "RegisterTravelerGetPlacePass", placepasscampain);
                });

            }
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string> { request.SessionId, session.ObjectName }).ConfigureAwait(false);
            if (await _featureToggles.IsEnableWheelchairFilterOnFSR(request.Application.Id, request.Application.Version.Major, session.CatalogItems).ConfigureAwait(false))
            {
                try
                {
                    _shoppingUtility.AssignWheelChairSpecialNeedForSinglePax(request.Travelers, bookingPathReservation);
                }
                catch(Exception ex)
                {
                    _logger.LogError("WheelChairSSR-RegisterTravelers tieing to singlepax error {Exception} and sessionId {sessionid} ", JsonConvert.SerializeObject(ex),request.SessionId);
                }
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
            string jsonRequest = string.Empty; FlightReservationResponse response = null;
            if (!request.IsOmniCartSavedTripFlow)
            {
                var tupleres = await _traveler.GetRegisterTravelerRequest(request, isRequireNationalityAndResidence, bookingPathReservation).ConfigureAwait(false);
                RegisterTravelersRequest registerTravelerRequest = tupleres.registerTravelerRequest;
                request = tupleres.request;
                jsonRequest = JsonConvert.SerializeObject(registerTravelerRequest);

                response = await _shoppingCartService.GetRegisterTravelers<FlightReservationResponse>(request.Token, request.SessionId, jsonRequest).ConfigureAwait(false);
            }

            if (response != null || request.IsOmniCartSavedTripFlow)
            {
                if (request.IsOmniCartSavedTripFlow)
                {
                    response = await _omniCart.GetFlightReservationResponseByCartId(request.SessionId, request.CartId);
                }

                if (response != null && (response.Status == Services.FlightShopping.Common.StatusType.Success) && response.Reservation != null)
                {
                    reservation.TravelersRegistered = true;
                    bookingPathReservation.TravelersRegistered = true;
                    //##Kirti ALM 23973 - Booking 2.1 - REST - LMX - use new CSL Profile Service, GetSavedTraverlerMplist from CSL 
                    request.Travelers = await _traveler.GetMPDetailsForSavedTravelers(request);

                    if (_shoppingUtility.IsETCEnabledforMultiTraveler(request.Application.Id, request.Application.Version.Major.ToString()))
                    {
                        AssignTravelerIndividualTotalAmount(request.Travelers, response.DisplayCart.DisplayPrices, response.Reservation?.Travelers.ToList(), response.Reservation?.Prices?.ToList());
                    }
                    _traveler.AssignFFCsToUnChangedTravelers(request.Travelers, bookingPathReservation.TravelersCSL, request.Application, request.ContinueToChangeTravelers);

                    if ((_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange) && bookingPathReservation.ShopReservationInfo2 != null &&
                        bookingPathReservation.ShopReservationInfo2.TravelerTypes != null && bookingPathReservation.ShopReservationInfo2.TravelerTypes.Count > 0) || _traveler.IsArranger(bookingPathReservation))
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
                            bookingPathReservation.LMXFlights = await _traveler.GetLmxForRTI(request.Token, request.CartId);
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

                            var tupleRes = await _traveler.GetProfileOwnerCreditCardList(request.SessionId, creditCardAddresses, mpPhone, mpEmail, string.Empty);
                            bookingPathReservation.CreditCards = tupleRes.savedProfileOwnerCCList;
                            mpPhone = tupleRes.mpPhone;
                            mpEmail = tupleRes.mpEmail;
                            var DisableCreditCardAddressCheck = _configuration.GetValue<bool>("DisableCreditCardAddressCheck");
                            if (!DisableCreditCardAddressCheck)
                            {
                                if (creditCardAddresses != null && creditCardAddresses.Count != 0)
                                {
                                    bookingPathReservation.CreditCardsAddress = creditCardAddresses;
                                }
                            }
                            else
                            {
                                bookingPathReservation.CreditCardsAddress = creditCardAddresses;
                            }
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
                    reservation.lmxtravelers = _traveler.GetLMXTravelersFromFlights(reservation);
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
                    bool enableNat = _traveler.IsNatAndResEnabled(request, bookingPathReservation);
                    bool enableTravelerTypes = _traveler.GetEnableTravelerTypes(request, bookingPathReservation);
                    bool enableEplus = _travelerUtility.EnableEPlusAncillary(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange);

                    //Bug 241287 : added below null check, as it is failing in preprod - 7th Feb 2018 j.srinivas
                    if (enableNat || enableUKtax || enableTravelerTypes || enableEplus)
                    {
                        if (response.DisplayCart.PricingChange || (enableTravelerTypes && !_traveler.comapreTtypesList(bookingPathReservation, response.DisplayCart)) || (enableEplus && bookingPathReservation.TravelOptions != null && bookingPathReservation.TravelOptions.Any(t => t?.Key.Trim().ToUpper() == "EFS"))
                            || (_omniCart.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, bookingPathReservation?.ShopReservationInfo2?.IsDisplayCart == true) && !string.IsNullOrEmpty(nextViewName) && nextViewName != "RTI"))//MOBILE-20204
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
                                bookingPathReservation.Prices = _traveler.GetPricesAfterPriceChange(response.DisplayCart.DisplayPrices, bookingPathReservation.AwardTravel, bookingPathReservation.SessionId, bookingPathReservation.IsReshopChange, response);
                            }

                            if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                            {
                                bookingPathReservation.Prices.AddRange(await _traveler.GetPrices(response.DisplayCart.DisplayFees, false, string.Empty));

                            }
                            //need to add close in fee to TOTAL
                            bookingPathReservation.Prices = _traveler.AdjustTotal(bookingPathReservation.Prices);
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
                                bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = _traveler.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices, bookingPathReservation.NumberOfTravelers, bookingPathReservation.IsReshopChange);
                                if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                                {
                                    bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes.Add(_travelerUtility.AddFeesAfterPriceChange(response.DisplayCart.DisplayFees));
                                }
                            }

                            if (reservation.ShopReservationInfo2 == null)
                            {
                                reservation.ShopReservationInfo2 = new ReservationInfo2();
                                reservation.ShopReservationInfo2.AllowAdditionalTravelers = !session.IsCorporateBooking;
                            }

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
                                    United.Services.FlightShopping.Common.ShopRequest persistedShopPindownRequest = await _sessionHelperService.GetSession<United.Services.FlightShopping.Common.ShopRequest>(request.SessionId, typeof(United.Services.FlightShopping.Common.ShopRequest).FullName, new List<string> { request.SessionId, typeof(United.Services.FlightShopping.Common.ShopRequest).FullName }).ConfigureAwait(false);
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

                                        await _sessionHelperService.SaveSession<United.Services.FlightShopping.Common.ShopRequest>(persistedShopPindownRequest, request.SessionId, new List<string> { request.SessionId, new United.Services.FlightShopping.Common.ShopRequest().GetType().FullName }, new United.Services.FlightShopping.Common.ShopRequest().GetType().FullName).ConfigureAwait(false);
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

                    if (_traveler.IsBuyMilesFeatureEnabled(request.Application.Id, request.Application.Version.Major, isNotSelectTripCall: true) && response?.DisplayCart?.DisplayFees?.Where(a => a.Type == "MPF") != null)
                    {
                        _shoppingBuyMiles.ApplyPriceChangesForBuyMiles(response, null, bookingPathReservation: bookingPathReservation);
                    }

                    if (_traveler.GetEnableTravelerTypes(request, bookingPathReservation))
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
                        if (session.IsCorporateBooking && _traveler.ValidateCorporateMsg(bookingPathReservation.ShopReservationInfo2.InfoWarningMessages))
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
                    if (_travelerUtility.EnableChaseOfferRTI(request.Application.Id, request.Application.Version.Major))
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
                    if (!_traveler.ShowViewCheckedBagsAtRti(bookingPathReservation))
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
                        if (bookingPathReservation.ShopReservationInfo2.InfoWarningMessages != null && bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Count > 0 && bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Exists(x => x.Order == MOBINFOWARNINGMESSAGEORDER.RTICORPORATELEISUREOPTOUTMESSAGE.ToString()) && _traveler.IsFareLockAvailable(bookingPathReservation))
                        {
                            bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Remove(bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Find(x => x.Order == MOBINFOWARNINGMESSAGEORDER.RTICORPORATELEISUREOPTOUTMESSAGE.ToString()));
                        }
                    }
                    #endregion
                    if (_travelerUtility.IncludeMoneyPlusMiles(request.Application.Id, request.Application.Version.Major))
                    {
                        bookingPathReservation.GetALLSavedTravelers = true;
                    }
                    if (_omniCart.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, bookingPathReservation?.ShopReservationInfo2?.IsDisplayCart == true))
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

                    if (_configuration.GetValue<bool>("EnableTravelInsuranceOptimization") && !await _featureSettings.GetFeatureSettingValue("EnableTravelInsuranceRemovalFix").ConfigureAwait(false))
                    {
                        #region TPI in booking path
                        //var shopping = new Shopping();
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

                    await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, request.SessionId, new List<string> { request.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                    #endregion
                    MOBShoppingCart persistShoppingCart = new MOBShoppingCart();
                    persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, persistShoppingCart.ObjectName, new List<string> { request.SessionId, persistShoppingCart.ObjectName }).ConfigureAwait(false);

                    if (persistShoppingCart == null)
                        persistShoppingCart = new MOBShoppingCart();
                    await _traveler.InFlightCLPaymentEligibility(request, bookingPathReservation, session, persistShoppingCart);
                    persistShoppingCart.Products = await _productInfoHelper.ConfirmationPageProductInfo(response, false, request.Application, null, request.Flow, sessionId: session?.SessionId);
                    persistShoppingCart.CartId = request.CartId;
                    double priceTotal = _shoppingUtility.GetGrandTotalPriceForShoppingCart(false, response, false, request.Flow);
                    persistShoppingCart.TotalPrice = String.Format("{0:0.00}", priceTotal);
                    persistShoppingCart.DisplayTotalPrice = Decimal.Parse(priceTotal.ToString().Trim()).ToString("c");
                    persistShoppingCart.TermsAndConditions = await _travelerUtility.GetProductBasedTermAndConditions(null, response, false);
                    persistShoppingCart.PaymentTarget = (request.Flow == FlowType.BOOKING.ToString()) ? _travelerUtility.GetBookingPaymentTargetForRegisterFop(response) : _travelerUtility.GetPaymentTargetForRegisterFop(response);
                    if (session.IsCorporateBooking && (await _shoppingUtility.IsEnableU4BTravelAddONPolicy(request.Application.Id, request.Application.Version.Major).ConfigureAwait(false)) && _shoppingUtility.HasPolicyWarningMessage(response?.Warnings))
                    {
                        persistShoppingCart.TravelPolicyWarningAlert = new Mobile.Model.Shopping.Common.Corporate.TravelPolicyWarningAlert();
                        CorporateDirect.Models.CustomerProfile.CorpPolicyResponse _corpPolicyResponse = await _shoppingUtility.GetCorporateTravelPolicyResponse(request.DeviceId, session.MileagPlusNumber, session.SessionId);
                        if (_corpPolicyResponse != null && _corpPolicyResponse.TravelPolicies != null && _corpPolicyResponse.TravelPolicies.Count > 0 && bookingPathReservation.ShopReservationInfo2 != null)
                        {
                            var corporateData = response?.Reservation?.Travelers?.FirstOrDefault()?.CorporateData;
                            string corporateCompanyName = corporateData != null ? corporateData.CompanyName : string.Empty;
                            var isCorporateBusinessNamePersonalized = bookingPathReservation.ShopReservationInfo2.IsCorporateBusinessNamePersonalized;
                            persistShoppingCart.TravelPolicyWarningAlert.TravelPolicy = await _shoppingUtility.GetTravelPolicy(_corpPolicyResponse, session, request, corporateCompanyName, isCorporateBusinessNamePersonalized);
                            persistShoppingCart.TravelPolicyWarningAlert.InfoWarningMessages = new List<InfoWarningMessages>();
                            persistShoppingCart.TravelPolicyWarningAlert.InfoWarningMessages = await _shoppingUtility.BuildTravelPolicyAlert(_corpPolicyResponse, request, response, session, isCorporateBusinessNamePersonalized);
                        }
                    }
                    await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, request.SessionId, new List<string> { request.SessionId, persistShoppingCart.ObjectName }, persistShoppingCart.ObjectName).ConfigureAwait(false);
                    if (_shoppingUtility.EnableRtiMandateContentsToDisplayByMarket(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange))
                    {
                        if (bookingPathReservation.ShopReservationInfo2.RTIMandateContentsToDisplayByMarket == null || bookingPathReservation.ShopReservationInfo2.RTIMandateContentsToDisplayByMarket.Count == 0)
                        {
                            try
                            {
                                await _traveler.UpdateCovidTestInfo(request, bookingPathReservation, session);
                            }
                            catch (System.Net.WebException ex)
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
                                    await _traveler.GetTravelAdvisoryMessagesBookingRTI_V1(request, bookingPathReservation, session);
                                }
                                else
                                    await _traveler.GetTravelAdvisoryMessagesBookingRTI(request, bookingPathReservation, session);
                            }
                        }
                        catch { }
                    }
                    #endregion

                    #region UnRegister If any ancillary offers registered
                    if (_productInfoHelper.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, true) && !string.IsNullOrEmpty(nextViewName) && nextViewName != "RTI" && !request.IsOmniCartSavedTripFlow)
                    {
                        await _traveler.UnregisterAncillaryOffer(persistShoppingCart, response, request, request.SessionId, request.CartId);
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
                        List<CMSContentMessage> lstMessages = new List<CMSContentMessage>();
                        if (request != null && !string.IsNullOrEmpty(request.Token))
                        {
                            lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, request.SessionId, request.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                        }

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
                        if (response.Errors.Any(e => e.MinorCode != null && e.MinorCode.Trim().Equals("10036")))
                        {
                            throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                        }
                        if (await _featureSettings.GetFeatureSettingValue("EnableInfantOnLapWarning").ConfigureAwait(false)) 
                        {
                            if (response.Errors.Any(e => e.MinorCode != null && e.MinorCode.Trim().Equals("22300")))
                            {
                                if (lstMessages != null && lstMessages.Count > 0) 
                                {
                                    var travelerMessage = ShopStaticUtility.GetSDLMessageFromList(lstMessages, "TRAVELERS_INFANTONLAP_ALERT_MESSAGE")[0].ContentFull;
                                    if (!string.IsNullOrEmpty(travelerMessage))
                                    {
                                        throw new MOBUnitedException("22300", travelerMessage);
                                    }
                                }
                                throw new MOBUnitedException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

                            }
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

            #endregion
            return reservation;

        }
        private List<TravelOption> GetTravelOptions(Services.FlightShopping.Common.DisplayCart.DisplayCart displayCart, bool isReshop, int appID, string appVersion)
        {
            List<TravelOption> travelOptions = null;
            if (displayCart != null && displayCart.TravelOptions != null && displayCart.TravelOptions.Count > 0)
            {
                CultureInfo ci = null;
                travelOptions = new List<TravelOption>();
                bool addTripInsuranceInTravelOption =
                    !_configuration.GetValue<bool>("ShowTripInsuranceBookingSwitch")
                    && (_configuration.GetValue<bool>("ShowTripInsuranceSwitch"));
                foreach (var anOption in displayCart.TravelOptions)
                {
                    //wade - added check for farelock as we were bypassing it
                    if (!anOption.Type.Equals("Premium Access") && !anOption.Key.Trim().ToUpper().Contains("FARELOCK") && !(addTripInsuranceInTravelOption && anOption.Key.Trim().ToUpper().Contains("TPI")) && !(_shoppingUtility.EnableEPlusAncillary(appID, appVersion, isReshop) && anOption.Key.Trim().ToUpper().Contains("EFS")))
                    {
                        continue;
                    }
                    if (ci == null)
                    {
                        ci = TopHelper.GetCultureInfo(anOption.Currency);
                    }

                    TravelOption travelOption = new TravelOption();
                    travelOption.Amount = (double)anOption.Amount;

                    travelOption.DisplayAmount = TopHelper.FormatAmountForDisplay(anOption.Amount.ToString(), ci, false);

                    //??
                    if (anOption.Key.Trim().ToUpper().Contains("FARELOCK") || (addTripInsuranceInTravelOption && anOption.Key.Trim().ToUpper().Contains("TPI")))
                        travelOption.DisplayButtonAmount = TopHelper.FormatAmountForDisplay(anOption.Amount.ToString(), ci, false);
                    else
                        travelOption.DisplayButtonAmount = TopHelper.FormatAmountForDisplay(anOption.Amount.ToString(), ci, true);

                    travelOption.CurrencyCode = anOption.Currency;
                    travelOption.Deleted = anOption.Deleted;
                    travelOption.Description = anOption.Description;
                    travelOption.Key = anOption.Key;
                    travelOption.ProductId = anOption.ProductId;
                    travelOption.SubItems = GetTravelOptionSubItems(anOption.SubItems);
                    if (_shoppingUtility.EnableEPlusAncillary(appID, appVersion, isReshop) && anOption.SubItems != null && anOption.SubItems.Count > 0)
                    {
                        travelOption.BundleCode = _travelerUtility.GetTravelOptionEplusAncillary(anOption.SubItems, travelOption.BundleCode);
                        _travelerUtility.GetTravelOptionAncillaryDescription(anOption.SubItems, travelOption, displayCart);
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
        private List<TravelOptionSubItem> GetTravelOptionSubItems(Services.FlightShopping.Common.DisplayCart.SubitemsCollection subitemsCollection)
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
                    subItem.DisplayAmount = TopHelper.FormatAmountForDisplay(item.Amount.ToString(), ci, false);
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

        private async Task<MOBSection> GetChangeInTravelerMessageWhenETCOrUpliftareFop(MOBRegisterTravelersRequest request, Reservation bookingPathReservation, Session session)
        {
            if (request.ContinueToChangeTravelers)
                return null;

            if (!(_travelerUtility.IsETCEnabledforMultiTraveler(request.Application.Id, request.Application.Version.Major.ToString()) || _configuration.GetValue<bool>("EnableUpliftPayment")))
                return null;

            var isTravelerChanged = ChangeTravelerValidation(request.Travelers, bookingPathReservation.TravelersCSL);
            var isFarelockExist = TravelOptionsContainsFareLock(bookingPathReservation.TravelOptions);
            var viewNameToCheckCoupon = !string.IsNullOrEmpty(bookingPathReservation?.ShopReservationInfo2?.NextViewName) ? bookingPathReservation?.ShopReservationInfo2?.NextViewName : string.Empty;
            var viewName = GetNavigationPageCode(request, bookingPathReservation, isFarelockExist, isTravelerChanged);

            if (!(viewName?.ToUpper().Trim() == "TRAVELOPTION" ||
                 IsTravelerEdited(request.Travelers, bookingPathReservation.TravelersCSL)))
                return null;

            var shoppingCart = new MOBShoppingCart();
            shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, shoppingCart.ObjectName, new List<string> { request.SessionId, shoppingCart.ObjectName }).ConfigureAwait(false);
            if (shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.Uplift.ToString())
            {
                var changeInTravelerMessage = _configuration.GetValue<string>("UpliftChangeInTravelerMessage");
                return string.IsNullOrWhiteSpace(changeInTravelerMessage) ? null : new MOBSection
                {
                    Text1 = changeInTravelerMessage,
                    Text2 = "Cancel",
                    Text3 = "Continue"
                };
            }

            if (!_travelerUtility.IsETCCombinabilityEnabled(request.Application.Id, request.Application.Version.Major) && _travelerUtility.IsETCEnabledforMultiTraveler(request.Application.Id, request.Application.Version.Major.ToString()) && shoppingCart.SCTravelers.Count > 1 && shoppingCart.IsMultipleTravelerEtcFeatureClientToggleEnabled &&
                shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates?.Count() > 0 &&
                _travelerUtility.IsETCTravelerChanged(request.Travelers, bookingPathReservation.TravelersCSL, shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates))
            {
                //if(etc specificTravelerChanged){
                var changeInTravelerMessage = _configuration.GetValue<string>("ETCChangeInTravelerMessage");
                return string.IsNullOrWhiteSpace(changeInTravelerMessage) ? null : new MOBSection
                {
                    Text1 = changeInTravelerMessage,
                    Text2 = "Cancel",
                    Text3 = "Continue"
                };
                //}
            }

            if (_travelerUtility.IncludeFFCResidual(request.Application.Id, request.Application.Version.Major) &&
                shoppingCart?.FormofPaymentDetails?.TravelFutureFlightCredit?.FutureFlightCredits?.Count > 0 &&
                _travelerUtility.IsFFCTravelerChanged(request.Travelers, bookingPathReservation.TravelersCSL))
            {
                List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, request.SessionId, session?.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                var changeInTravelerMessage = string.Empty;
                var isEnableTravelCredit = _travelerUtility.IncludeTravelCredit(request.Application.Id, request.Application.Version.Major);
                if (_configuration.GetValue<bool>("EnableCouponsforBooking") && shoppingCart?.Products != null && shoppingCart.Products.Any(p => p.CouponDetails != null && p.CouponDetails.Count > 0))
                {
                    changeInTravelerMessage = ShopStaticUtility.GetSDLMessageFromList(lstMessages, "RTI.FutureFlightCreditsAndCoupons.RemoveTraveler")[0].ContentFull;
                    if (isEnableTravelCredit)
                    {
                        var lookUpMessages = ShopStaticUtility.GetSDLMessageFromList(lstMessages, "RTI.TravelCertificate.AlertTravelCredits");
                        var appliedFFCNPromo = lookUpMessages?.FirstOrDefault
                        (x => string.Equals(x.LocationCode, "RTI.TravelCertificate.LookUpTravelCredits.Alert.AppliedFFCNPromo", StringComparison.OrdinalIgnoreCase));
                        changeInTravelerMessage = appliedFFCNPromo.ContentFull;
                    }
                }
                else
                {
                    changeInTravelerMessage = ShopStaticUtility.GetSDLMessageFromList(lstMessages, "RTI.FutureFlightCredits.RemoveTraveler")[0].ContentFull;
                    if (isEnableTravelCredit)
                    {
                        var lookUpMessages = ShopStaticUtility.GetSDLMessageFromList(lstMessages, "RTI.TravelCertificate.AlertTravelCredits");
                        var appliedFFCNPromo = lookUpMessages?.FirstOrDefault
                        (x => string.Equals(x.LocationCode, "RTI.TravelCertificate.LookUpTravelCredits.Alert.AlreadyAppliedFFC", StringComparison.OrdinalIgnoreCase));
                        changeInTravelerMessage = appliedFFCNPromo.ContentFull;
                    }
                }

                return string.IsNullOrWhiteSpace(changeInTravelerMessage) ? null : new MOBSection
                {
                    Text1 = changeInTravelerMessage,
                    Text2 = "Cancel",
                    Text3 = "Continue"
                };
            }
            if (_configuration.GetValue<bool>("EnableCouponsforBooking") && shoppingCart?.Products != null && shoppingCart.Products.Any(p => p.CouponDetails != null && p.CouponDetails.Count > 0) && (_shoppingUtility.EnableAdvanceSearchCouponBooking(request.Application.Id, request.Application.Version.Major) ? request.IsRegisterTravelerFromRTI : true))
            {
                var changeInTravelerMessage = _configuration.GetValue<string>("PromoCodeRemovalMessage");
                return string.IsNullOrWhiteSpace(changeInTravelerMessage) ? null : new MOBSection
                {
                    Text1 = changeInTravelerMessage,
                    Text2 = "Cancel",
                    Text3 = "Continue"
                };
            }

            //Popup for MM to reset if change in Traveler
            if (_travelerUtility.IncludeMoneyPlusMiles(request.Application.Id, request.Application.Version.Major) && shoppingCart?.FormofPaymentDetails?.MoneyPlusMilesCredit?.SelectedMoneyPlusMiles != null && bookingPathReservation.TravelersRegistered)
            {
                //Update message as per Mockup
                List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                var changeInTravelerMessage = string.Empty;
                changeInTravelerMessage = ShopStaticUtility.GetSDLMessageFromList(lstMessages, "RTI.MoneyPlusMilesCredits.MMCMessage.RemoveTraveler")[0]?.ContentFull;
                return string.IsNullOrWhiteSpace(changeInTravelerMessage) ? null : new MOBSection
                {
                    Text1 = changeInTravelerMessage,
                    Text2 = "Cancel",
                    Text3 = "Continue"
                };
            }
            return null;
        }

        private bool ChangeTravelerValidation(List<MOBCPTraveler> travelers, SerializableDictionary<string, MOBCPTraveler> travelersCSL)
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
        private bool CheckMileagePlus(MOBCPMileagePlus mileagePlus1, MOBCPMileagePlus mileagePlus2)
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
        private bool IsTravelerEdited(List<MOBCPTraveler> travelers, SerializableDictionary<string, MOBCPTraveler> travelersCSL)
        {
            if (travelersCSL == null || !travelersCSL.Any() || travelers == null || !travelers.Any())
                return false;

            foreach (var travelerCSL in travelersCSL)
            {
                var currentTraveler = travelers.FirstOrDefault(t => t.PaxID == travelerCSL.Value?.PaxID);
                if (currentTraveler != null)
                {
                    if (ShopStaticUtility.IsValuesChangedForSameTraveler(currentTraveler, travelerCSL.Value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private async Task UpdateTravelBankPrice(MOBRegisterTravelersRequest request, MOBShoppingCart shoppingCart, Session session, MOBSHOPReservation reservation)
        {
            if (_travelerUtility.IncludeTravelBankFOP(request.Application.Id, request.Application.Version.Major))
            {
                var travelBank = new United.Common.Helper.Traveler.TravelBank(_configuration, _sessionHelperService, _travelerUtility, _fFCShoppingcs, _shoppingUtility, _cachingService, _dPService, _pKDispenserService, _headers);
                var scRESProduct = shoppingCart.Products.Find(p => p.Code == "RES");
                double prodValue = Convert.ToDouble(scRESProduct?.ProdTotalPrice);
                prodValue = Math.Round(prodValue, 2, MidpointRounding.AwayFromZero);
                if (shoppingCart?.FormofPaymentDetails?.TravelBankDetails == null)
                {
                    shoppingCart.FormofPaymentDetails.TravelBankDetails = await travelBank.PopulateTravelBankData(session, reservation, request);
                }
                else if (shoppingCart?.FormofPaymentDetails?.TravelBankDetails?.TBApplied > 0 && (shoppingCart?.FormofPaymentDetails?.TravelBankDetails?.TBApplied != prodValue || !reservation.Prices.Exists(p => p.PriceType == "TB")))
                {
                    var tbRequest = new MOBFOPTravelerBankRequest();
                    tbRequest.Application = request.Application;
                    tbRequest.SessionId = request.SessionId;

                    tbRequest.AppliedAmount = prodValue < shoppingCart.FormofPaymentDetails.TravelBankDetails.TBApplied ? prodValue : shoppingCart.FormofPaymentDetails.TravelBankDetails.TBApplied;
                    tbRequest.IsRemove = false;
                    var tBresponse = await travelBank.TravelBankCredit(session, tbRequest, false);
                    reservation.Prices = tBresponse.Reservation.Prices;
                }
            }
        }

        private async Task GetTPIandUpdatePrices(MOBRequest request, string cartId, Session session, Reservation bookingPathReservation)
        {
            #region TPI in booking path
            if (_travelerUtility.EnableTPI(request.Application.Id, request.Application.Version.Major, 3) && !bookingPathReservation.IsReshopChange)
            {
                // call TPI 
                try
                {
                    string token = session.Token;
                    TPIInfoInBookingPath tPIInfo = await _travelerUtility.GetBookingPathTPIInfo(session.SessionId, request.LanguageCode, request.Application, request.DeviceId, cartId, token, true, true, false);
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
        public async Task<MOBValidateWheelChairSizeResponse> ValidateWheelChairSize(MOBValidateWheelChairSizeRequest request)
        {
            MOBValidateWheelChairSizeResponse response = new MOBValidateWheelChairSizeResponse();

            if (request.WheelChairDimensionInfo.Width > 0
                && request.WheelChairDimensionInfo.Height > 0 && !string.IsNullOrEmpty(request.WheelChairDimensionInfo.Units))
            {
                if (request.WheelChairDimensionInfo.Units.ToUpper().Trim() != "INCHES")
                {
                    request.WheelChairDimensionInfo.Width = _shoppingUtility.ConvertToInches(request.WheelChairDimensionInfo.Width);
                    request.WheelChairDimensionInfo.Height = _shoppingUtility.ConvertToInches(request.WheelChairDimensionInfo.Height);
                }
                var equipmentListInDB = await _shoppingUtility.GetFlightDimensionsList(request.TransactionId).ConfigureAwait(false);
                var bookingPathReservation = new Reservation();
                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName });
                if (bookingPathReservation == null)
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("BookingSessionExpiryMessage"));
                }
                #region Getting UA/UAX flight segments from reservation
                var unitedCarriers = _configuration.GetValue<string>("UnitedCarriers");
                var UAFlightSegments = bookingPathReservation?.Trips?.SelectMany(a => a.FlattenedFlights)?.SelectMany(b => b.Flights)
                    .Where(f => unitedCarriers.Contains(f.OperatingCarrier?.ToUpper().Trim())).ToList();
                List<string> UAflightEquipmentList = new List<string>();
                if (UAFlightSegments != null && UAFlightSegments.Any() && UAFlightSegments.Count > 0)
                {
                    UAflightEquipmentList = UAFlightSegments?.Select(c => c.EquipmentDisclosures)?.Select(d => d.EquipmentDescription)?.ToList();
                }
                #endregion
                List<MOBDimensions> masterUAEquipList = new List<MOBDimensions>();
                #region Logic for handling UA/UAX and UA+OA flights scenario
                if (UAflightEquipmentList != null && UAflightEquipmentList.Any()
                                    && UAflightEquipmentList.Count > 0 && equipmentListInDB != null
                                    && equipmentListInDB.Count > 0)//indicates some/all the flights are UA/UAX
                {
                    foreach (var flightDesc in UAflightEquipmentList)
                    {
                        var equip = equipmentListInDB?.FirstOrDefault(e => e.FlightEquipmentDescription?.ToUpper() == flightDesc?.ToUpper());
                        if (equip != null && equip.Height > 0 && equip.Width > 0)
                        {
                            masterUAEquipList?.Add(equip);//adding only UA/UAX flights that has dimension data available
                        }
                    }
                    if (masterUAEquipList != null && masterUAEquipList?.Count > 0 && UAflightEquipmentList.Count == masterUAEquipList.Count)//indicates all the UA/UAX flights in reservation has dimension data available in db
                    {
                        var maxWidthAllowed = masterUAEquipList?.Min(e => e.Width);
                        var maxHeightAllowed = masterUAEquipList?.Min(e => e.Height);
                        if (request.WheelChairDimensionInfo.Width > maxWidthAllowed || request.WheelChairDimensionInfo.Height > maxHeightAllowed)//Customer's WheelChair size exceeded cargo dimensions
                        {
                            response.WheelChairErrorMessages = new United.Mobile.Model.Common.MOBAlertMessages();
                            response.WheelChairErrorMessages.HeaderMessage = _configuration.GetValue<string>("WheelChairSizeWillNotFitHeaderMsg");
                            response.WheelChairErrorMessages.AlertMessages = new List<MOBSection>();
                            response.WheelChairErrorMessages.AlertMessages.Add(new MOBSection()
                            {
                                Text1 = _configuration.GetValue<string>("WheelChairSizeWillNotFitBodyMsg")
                            });
                            _logger1.LogError("ValidateWheelChairSize - Customer's WheelChair size exceeded cargo dimensions.");
                        }
                        #region WheelChair fits successfully in all UA/UAX flights
                        else
                        {
                            response.WheelChairDimensionInfo = new MOBDimensions();
                            response.WheelChairDimensionInfo.Dimensions = request.WheelChairDimensionInfo.Dimensions;
                            response.WheelChairDimensionInfo.WcFitConfirmationMsg = _configuration.GetValue<string>("WheelChairSizeSuccessMsg");//success message
                        }
                        #endregion
                    }
                    #region Dimension data unavailable in db for one of the UA/UAX flights
                    else
                    {
                        response.WheelChairErrorMessages = new United.Mobile.Model.Common.MOBAlertMessages();
                        response.WheelChairErrorMessages.HeaderMessage = _configuration.GetValue<string>("WheelChairSizeNoDataHeaderMsg");
                        response.WheelChairErrorMessages.AlertMessages = new List<MOBSection>();
                        response.WheelChairErrorMessages.AlertMessages.Add(new MOBSection()
                        {
                            Text1 = _configuration.GetValue<string>("WheelChairSizeNoDataBodyMsg")
                        });
                        _logger1.LogError("ValidateWheelChairSize - Dimension data unavailable in db for one of the UA/UAX flights.");
                    }
                    #endregion
                }
                #endregion
                #region when all flights are OA
                else
                {
                    response.WheelChairErrorMessages = new United.Mobile.Model.Common.MOBAlertMessages();
                    response.WheelChairErrorMessages.HeaderMessage = _configuration.GetValue<string>("WheelChairSizeNoDataHeaderMsg");
                    response.WheelChairErrorMessages.AlertMessages = new List<MOBSection>();
                    response.WheelChairErrorMessages.AlertMessages.Add(new MOBSection()
                    {
                        Text1 = _configuration.GetValue<string>("WheelChairSizeNoDataBodyMsg")
                    });
                }
                #endregion
            }
            else
            {
                throw new MOBUnitedException("Please enter a valid width and height.");
            }

            return response;
        }
    }
}
