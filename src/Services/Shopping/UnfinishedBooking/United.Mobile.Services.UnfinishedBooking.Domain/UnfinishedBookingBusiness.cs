using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.Merchandize;
using United.Common.Helper.PageProduct;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Definition;
using United.Definition.Shopping;
using United.Ebs.Logging.Enrichers;
using United.Foundations.Practices.Framework.Security.DataPower;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.Loyalty;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.MerchandizeService;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.ShopBundles;
using United.Mobile.DataAccess.ShopProducts;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.DataAccess.Travelers;
using United.Mobile.DataAccess.UnfinishedBooking;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.SSR;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Bundles;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Model.Shopping.Misc;
using United.Mobile.Model.Shopping.PriceBreakDown;
using United.Mobile.Model.Shopping.UnfinishedBooking;
using United.Mobile.Model.ShopTrips;
using United.Mobile.Model.Travelers;
using United.Mobile.Model.TripPlannerGetService;
using United.Mobile.Model.UnfinishedBooking;
using United.Service.Presentation.CommonModel;
using United.Service.Presentation.PaymentModel;
using United.Service.Presentation.PaymentRequestModel;
using United.Service.Presentation.PaymentResponseModel;
using United.Service.Presentation.PersonalizationModel;
using United.Service.Presentation.PersonalizationRequestModel;
using United.Service.Presentation.PersonalizationResponseModel;
using United.Service.Presentation.ProductModel;
using United.Services.Customer.Common;
using United.Services.Customer.Preferences.Common;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.Cart;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.Extensions;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Services.FlightShopping.Common.LMX;
using United.Utility.Enum;
using United.Utility.Helper;
using static United.Common.Helper.Shopping.UnfinishedBooking;
using static United.Services.Customer.Common.Constants;
using Characteristic = United.Service.Presentation.CommonModel.Characteristic;
using CslDataVaultResponse = United.Service.Presentation.PaymentResponseModel.DataVault<United.Service.Presentation.PaymentModel.Payment>;
using JsonSerializer = United.Utility.Helper.DataContextJsonSerializer;
using MOBSHOPGetUnfinishedBookingsRequest = United.Mobile.Model.Shopping.UnfinishedBooking.MOBSHOPGetUnfinishedBookingsRequest;
using MOBShoppingCart = United.Mobile.Model.Shopping.MOBShoppingCart;
using MOBSHOPUnfinishedBooking = United.Mobile.Model.Shopping.UnfinishedBooking.MOBSHOPUnfinishedBooking;
using MOBSHOPUnfinishedBookingFlight = United.Mobile.Model.Shopping.UnfinishedBooking.MOBSHOPUnfinishedBookingFlight;
using MOBSHOPUnfinishedBookingRequestBase = United.Mobile.Model.Shopping.UnfinishedBooking.MOBSHOPUnfinishedBookingRequestBase;
using MOBSHOPUnfinishedBookingTrip = United.Mobile.Model.Shopping.UnfinishedBooking.MOBSHOPUnfinishedBookingTrip;
using MOBTravelerType = United.Mobile.Model.Shopping.MOBTravelerType;

namespace United.Mobile.Services.UnfinishedBooking.Domain
{
    public class UnfinishedBookingBusiness : IUnfinishedBookingBusiness
    {
        private readonly ICacheLog<UnfinishedBookingBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly IBundleOfferService _bundleOfferService;
        private readonly IUnfinishedBooking _unfinishedBooking;
        private readonly IOmniCart _omniCart;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly ICustomerPreferencesService _customerPreferencesService;
        private readonly IOmniChannelCartService _omniChannelCartService;
        private readonly IValidateHashPinService _validateHashPinService;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly IFormsOfPayment _formsOfPayment;
        private readonly ITravelerUtility _travelerUtility;
        private readonly IFlightShoppingProductsService _flightShoppingProductsService;
        private readonly IELFRitMetaShopMessages _eLFRitMetaShopMessages;
        private readonly ITraveler _traveler;
        private readonly IReferencedataService _referencedataService;
        private readonly ICachingService _cachingService;
        private readonly ITravelerCSL _travelerCSL;
        private readonly IShoppingBuyMiles _shoppingBuyMiles;
        private readonly ICustomerDataService _customerDataService;
        private readonly IDataVaultService _dataVaultService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly IDPService _dPService;
        private readonly IPKDispenserService _pKDispenserService;
        private readonly IProfileService _profileService;
        private readonly ILMXInfo _lmxInfo;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IShoppingCcePromoService _shoppingCcePromoService;
        private readonly ILoyaltyUCBService _loyaltyBalanceServices;
        private readonly IPurchaseMerchandizingService _purchaseMerchandizingService;
        private readonly IFlightShoppingService _flightShoppingService;
        private readonly ICMSContentService _iCMSContentService;
        private readonly IPaymentService _paymentService;
        private readonly IGMTConversionService _gMTConversionService;
        private readonly IMileagePlusCSSTokenService _mileagePlusCSSTokenService;
        private readonly IMileagePlusTFACSL _mileagePlusTFACSL;
        private readonly IMPSecurityQuestionsService _mPSecurityQuestionsService;

        private readonly IPaymentUtility _paymentUtility;
        private readonly IProductOffers _shopping;
        private readonly IRegisterCFOP _regiserCFOP;
        private readonly IProductInfoHelper _productInfoHelper;
        private readonly IProfileCreditCard _profileCreditCard;
        private readonly ICustomerProfile _customerProfile;
        private readonly PKDispenserPublicKey _pKDispenserPublicKey;
        private AirportDetailsList airportsList = null;
        public string CURRENCY_TYPE_MILES = "miles";
        public string PRICING_TYPE_CLOSE_IN_FEE = "CLOSEINFEE";
        private bool IsCorpBookingPath = false;
        private bool IsArrangerBooking = false;
        private string _deviceId = string.Empty;
        private MOBApplication _application = new MOBApplication() { Version = new MOBVersion() };
        private readonly DocumentLibraryDynamoDB _documentLibraryDynamoDB;
        //private readonly IDataPowerGateway _dataPowerGateway;
        private IDataPowerGateway _dataPowerGateway = null;
        private readonly IApplicationEnricher _requestEnricher;
        private readonly IHeaders _headers;
        private readonly IFeatureSettings _featureSettings;
        private readonly IShopBundleService _shopBundleService;
        private readonly ILogger<UnfinishedBookingBusiness> _logger1;
        private readonly IFeatureToggles _featureToggles;
        private readonly IFFCShoppingcs _ffcShoppingcs;

        public UnfinishedBookingBusiness(ICacheLog<UnfinishedBookingBusiness> logger, IConfiguration configuration
            , IShoppingSessionHelper shoppingSessionHelper
            , ISessionHelperService sessionHelperService
            , IDynamoDBService dynamoDBService
            , IBundleOfferService bundleOfferService
            , IUnfinishedBooking unfinishedBooking,
              IOmniCart omniCart
            , IShoppingUtility shoppingUtility
            , ICustomerPreferencesService customerPreferencesService
            , IOmniChannelCartService omniChannelCartService
            , IValidateHashPinService validateHashPinService,
              IFFCShoppingcs fFCShoppingcs,
              IFormsOfPayment formsOfPayment,
              ITravelerUtility travelerUtility,
              IFlightShoppingProductsService flightShoppingProductsService,
              IELFRitMetaShopMessages eLFRitMetaShopMessages,
              ITraveler traveler,
              IReferencedataService referencedataService,
              ICachingService cachingService,
              ITravelerCSL travelerCSL,
              IShoppingBuyMiles shoppingBuyMiles,
              ICustomerDataService customerDataService,
              IDataVaultService dataVaultService,
              IGMTConversionService gMTConversionService,
              IMileagePlusCSSTokenService mileagePlusCSSTokenService,
              ILegalDocumentsForTitlesService legalDocumentsForTitlesService,
              IDPService dPService,
              IPKDispenserService pKDispenserService,
              IProfileService profileService,
              ILMXInfo lmxInfo,
              IShoppingCartService shoppingCartService,
              IShoppingCcePromoService shoppingCcePromoService,
              ILoyaltyUCBService loyaltyBalanceServices,
              IPurchaseMerchandizingService purchaseMerchandizingService,
              IFlightShoppingService flightShoppingService,
              ICMSContentService iCMSContentService,
              IPaymentService paymentService,
              //IDataPowerGateway dataPowerGateway,
              IMileagePlusTFACSL mileagePlusTFACSL,
              IMPSecurityQuestionsService mPSecurityQuestionsService
            , IPaymentUtility paymentUtility
             , IProductOffers shopping
            , IRegisterCFOP regiserCFOP
            , IProductInfoHelper productInfoHelper
            , IProfileCreditCard profileCreditCard
            , IApplicationEnricher requestEnricher
            , IHeaders headers
            , ICustomerProfile customerProfile
            , IFeatureSettings featureSettings
            , IShopBundleService shopBundleService
            , ILogger<UnfinishedBookingBusiness> logger1
            , IFeatureToggles featureToggles
            , IFFCShoppingcs ffcShoppingcs)
        {
            _logger = logger;
            _configuration = configuration;
            _shoppingSessionHelper = shoppingSessionHelper;
            _sessionHelperService = sessionHelperService;
            _dynamoDBService = dynamoDBService;
            _bundleOfferService = bundleOfferService;
            _unfinishedBooking = unfinishedBooking;
            _omniCart = omniCart;
            _shoppingUtility = shoppingUtility;
            _customerPreferencesService = customerPreferencesService;
            _omniChannelCartService = omniChannelCartService;
            _validateHashPinService = validateHashPinService;
            _fFCShoppingcs = fFCShoppingcs;
            _formsOfPayment = formsOfPayment;
            _travelerUtility = travelerUtility;
            _flightShoppingProductsService = flightShoppingProductsService;
            _eLFRitMetaShopMessages = eLFRitMetaShopMessages;
            _traveler = traveler;
            _referencedataService = referencedataService;
            _cachingService = cachingService;
            _travelerCSL = travelerCSL;
            _shoppingBuyMiles = shoppingBuyMiles;
            _customerDataService = customerDataService;
            _dataVaultService = dataVaultService;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _dPService = dPService;
            _pKDispenserService = pKDispenserService;
            _profileService = profileService;
            _lmxInfo = lmxInfo;
            _shoppingCartService = shoppingCartService;
            _shoppingCcePromoService = shoppingCcePromoService;
            _loyaltyBalanceServices = loyaltyBalanceServices;
            _purchaseMerchandizingService = purchaseMerchandizingService;
            _flightShoppingService = flightShoppingService;
            _iCMSContentService = iCMSContentService;
            _paymentService = paymentService;
            _gMTConversionService = gMTConversionService;
            _mileagePlusTFACSL = mileagePlusTFACSL;
            _mPSecurityQuestionsService = mPSecurityQuestionsService;
            _mileagePlusCSSTokenService = mileagePlusCSSTokenService;
            new ConfigUtility(_configuration);
            _documentLibraryDynamoDB = new DocumentLibraryDynamoDB(_configuration, _dynamoDBService);
            _dataPowerGateway = new DataPowerGateway(_configuration.GetValue<string>("DPDiscoveryDocumentEndPoint"));
            _paymentUtility = paymentUtility;
            _shopping = shopping;
            _regiserCFOP = regiserCFOP;
            _productInfoHelper = productInfoHelper;
            _profileCreditCard = profileCreditCard;
            _headers = headers;
            _requestEnricher = requestEnricher;
            _customerProfile = customerProfile;
            _pKDispenserPublicKey = new PKDispenserPublicKey(_configuration, _cachingService, _dPService, _pKDispenserService, headers);
            _featureSettings = featureSettings;
            _shopBundleService = shopBundleService;
            _logger1 = logger1;
            _featureToggles = featureToggles;
            _ffcShoppingcs = ffcShoppingcs;
        }

        public async Task<MOBSHOPGetUnfinishedBookingsResponse> GetUnfinishedBookings(MOBSHOPGetUnfinishedBookingsRequest request)
        {
            var response = new MOBSHOPGetUnfinishedBookingsResponse();
            Session session = null;

            session = await _shoppingSessionHelper.CreateShoppingSession(request.Application.Id, request.DeviceId, request.Application.Version.Major, request.TransactionId, request.MileagePlusAccountNumber, string.Empty);
            if (session == null)
            {
                throw new MOBUnitedException(_configuration.GetValue<string>(("Booking2OGenericExceptionMessage")));
            }
            await ValidateVulnerability(request.MileagePlusAccountNumber, request.PasswordHash, request.Application.Id, request.DeviceId
                                  , request.Application.Version.Major, request.IsOmniCartSavedTrip ? _configuration.GetValue<string>("OmnicartExceptionMessage") : _configuration.GetValue<string>("bugBountySessionExpiredMsg"));

            new ForceUpdateVersion(_configuration).ForceUpdateForNonSupportedVersion(request.Application.Id, request.Application.Version.Major, FlowType.BOOKING);

            bool IsEnableCCEForGetUnfinishedBookings = _configuration.GetValue<bool>("EnableCCEServiceforGetUnfinishedBookings") && (session?.CustomerID > 0 || _configuration.GetValue<bool>("EnableCCEServicetoGetSavedTripsForGuest"));
            response.PricedUnfinishedBookingList = IsEnableCCEForGetUnfinishedBookings ? await GetUnfinishedBookingsV2(session, request) : await GetUnfinishedBookings(session, request);
            return response;
        }
        public async Task<MOBGetOmniCartSavedTripsResponse> RemoveOmniCartSavedTrip(MOBSHOPUnfinishedBookingRequestBase request)
        {
            Session session = null;

            session = await _shoppingSessionHelper.GetValidateSession(request.SessionId, true, false);

            await ValidateVulnerability(request.MileagePlusAccountNumber, request.PasswordHash, request.Application.Id, request.DeviceId
                                    , request.Application.Version.Major, _configuration.GetValue<string>("OmnicartExceptionMessage"));


            return await RemoveOmniCartSavedTrip(session, request);
        }
        public async Task<MOBGetOmniCartSavedTripsResponse> GetOmniCartSavedTrips(MOBSHOPUnfinishedBookingRequestBase request)
        {
            Session session = await _shoppingSessionHelper.CreateShoppingSession(request.Application.Id, request.DeviceId, request.Application.Version.Major, request.TransactionId, request.MileagePlusAccountNumber, string.Empty);

            if (session == null)
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("OmnicartExceptionMessage"));
            }
            await ValidateVulnerability(request.MileagePlusAccountNumber, request.PasswordHash, request.Application.Id, request.DeviceId
                                    , request.Application.Version.Major, _configuration.GetValue<string>("OmnicartExceptionMessage"));


            if (request.CatalogItems?.Count > 0)
            {
                session.CatalogItems = request.CatalogItems;
                //United.Persist.FilePersist.Save<United.Persist.Definition.Shopping.Session>(session.SessionId, session.ObjectName, session);
                await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName);

            }

            return await GetOmniCartSavedTrips(session, request);
        }

        public async Task<SelectTripResponse> SelectUnfinishedBooking(MOBSHOPSelectUnfinishedBookingRequest request, HttpContext httpContext = null)
        {
            var actionName = request.IsOmniCartSavedTrip ? "SelectUnfinishedBooking_OmniCart" : "SelectUnfinishedBooking";
            var response = new SelectTripResponse();
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            Session session = null;
            await ValidateVulnerability(request.MileagePlusAccountNumber, request.PasswordHash, request.Application.Id, request.DeviceId
                                        , request.Application.Version.Major, request.IsOmniCartSavedTrip ? _configuration.GetValue<string>("OmnicartExceptionMessage") : _configuration.GetValue<string>("bugBountySessionExpiredMsg"));

            if (request.IsOmniCartSavedTrip && _omniCart.IsEnableOmniCartHomeScreenChanges(request.Application.Id, request.Application.Version.Major))
            {
                session = await _sessionHelperService.GetSession<Session>(request.SessionId, new Session().ObjectName, new List<string> { request.SessionId, new Session().ObjectName });
            }
            else
            {
                session = await _shoppingSessionHelper.CreateShoppingSession(request.Application.Id, request.DeviceId, request.Application.Version.Major, request.TransactionId, request.MileagePlusAccountNumber, string.Empty);
            }

            if (session == null)
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            if (!String.IsNullOrEmpty(session.SessionId))
            {
                _headers.ContextValues.SessionId = session.SessionId;
                _requestEnricher.Add(United.Mobile.Model.Constants.SessionId, session.SessionId);
            }

            if (request.SelectedUnfinishBooking?.CatalogItems.Count > 0)
            {
                session.CatalogItems = request.SelectedUnfinishBooking.CatalogItems;
            }

            new ForceUpdateVersion(_configuration).ForceUpdateForNonSupportedVersion(request.Application.Id, request.Application.Version.Major, FlowType.BOOKING);
          

            response.TransactionId = request.TransactionId;
            response.LanguageCode = request.LanguageCode;
            if (_configuration.GetValue<bool>("EnableOmniChannelCartMVP1"))
            {
                var tupleResponse = await _unfinishedBooking.GetShopPinDownDetailsV2(session, request, httpContext);
                response.Availability = tupleResponse.Item1;
            }
            else
            {
                response.Availability = await _unfinishedBooking.GetShopPinDownDetails(session, request);
            }
            response.Disclaimer = new List<string>();
            if (_configuration.GetValue<string>("MakeReservationDisclaimer") != null)
            {
                response.Disclaimer.Add(_configuration.GetValue<string>("MakeReservationDisclaimer"));
            }
            else
            {
                response.Disclaimer.Add("*Miles shown are the actual miles flown for this segment.Mileage accrued will vary depending on the terms and conditions of your frequent flyer program.");
            }

            if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "YES")
            {
                response.CartId = response.Availability.CartId;
            }

            if (response.Availability != null)
            {
                session.CartId = response.Availability.CartId;
                await _sessionHelperService.SaveSession(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName);
            }
            bool isCFOPVersionCheck = GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("AndriodCFOP_Booking_Reshop_PostbookingAppVersion"), _configuration.GetValue<string>("IphoneCFOP_Booking_Reshop_PostbookingAppVersion"));
            if (isCFOPVersionCheck)
            {
                if (!string.IsNullOrEmpty(_configuration.GetValue<string>("IsBookingCommonFOPEnabled")) && session.IsReshopChange == false)
                {
                    response.ShoppingCart = await _shoppingUtility.PopulateShoppingCart(shoppingCart, FlowType.BOOKING.ToString(), session.SessionId, response.Availability.CartId, request, response.Availability.Reservation);
                }
            }

            return response;
        }

        public async Task<MOBSHOPSelectTripResponse> SelectOmniCartSavedTrip(MOBSHOPSelectUnfinishedBookingRequest request, HttpContext httpContext = null)
        {
            bool isDefault = false;
            Session session = null;
            var response = new MOBSHOPSelectTripResponse();
            var shoppingCart = new MOBShoppingCart();
            Stopwatch _stopwatch = new Stopwatch();
            await ValidateVulnerability(request.MileagePlusAccountNumber, request.PasswordHash, request.Application.Id, request.DeviceId
                                 , request.Application.Version.Major, _configuration.GetValue<string>("OmnicartExceptionMessage"));
            if (_omniCart.IsEnableOmniCartRetargetingChanges(request.Application.Id, request.Application.Version.Major, request?.CatalogItems) && request.IsDeeplink)
            {
                session = await _shoppingSessionHelper.CreateShoppingSession(request.Application.Id, request.DeviceId, request.Application.Version.Major, request.TransactionId, request.MileagePlusAccountNumber, string.Empty);

                if (session == null)
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("OmnicartExceptionMessage"));
                }
                if (request.CatalogItems?.Count > 0)
                {
                    session.CatalogItems = request.CatalogItems;
                    await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName);
                }
                request.SessionId = session.SessionId;
                MOBSHOPSelectTripResponse apiResponse = await BuildCCEFlightReservationResponseByCartId(request, session?.Token, session?.SessionId);
                if (apiResponse !=null)
                {
                    return apiResponse;
                }
            }
            else
            {
                session = await _shoppingSessionHelper.GetValidateSession(request.SessionId, true, false);
                request.CatalogItems = session?.CatalogItems.Count > 0 ? session.CatalogItems : request.CatalogItems;
            }
            if ( _omniCart.IsEnableOmniCartReleaseCandidate4CChanges(request.Application.Id, request.Application.Version.Major, session?.CatalogItems)
                && await _omniCart.IsNonUSPointOfSale(session.SessionId, request.CartId, response))   //If Point of Sale's Country Code is Not US, then return the control with the alert message property populated.
            {
                response.NonUSPOSAlertMessage.Actions.First().WebShareToken = _dPService.GetSSOTokenString(request.Application.Id, request.MileagePlusAccountNumber, _configuration);

                if (await _featureSettings.GetFeatureSettingValue("EnableRedirectURLUpdate").ConfigureAwait(false))
                {
                    var action = response.NonUSPOSAlertMessage.Actions.First();
                    response.NonUSPOSAlertMessage.Actions.First().ActionURL = $"{_configuration.GetValue<string>("NewDotcomSSOUrl")}?type=sso&token={action.WebShareToken}&landingUrl={action.ActionURL}";
                    response.NonUSPOSAlertMessage.Actions.First().WebSessionShareUrl = response.NonUSPOSAlertMessage.Actions.First().WebShareToken = string.Empty;
                }
                return response;
            }

            response = await SelectOmniCartSavedTrip(session, request, _stopwatch, httpContext);

            shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, shoppingCart.ObjectName, new List<string> { request.SessionId, shoppingCart.ObjectName }).ConfigureAwait(false);

            //shoppingCart = FilePersist.Load<MOBShoppingCart>(request.SessionId, shoppingCart.ObjectName);

            shoppingCart.Flow = FlowType.BOOKING.ToString();
            shoppingCart= await _shoppingUtility.ReservationToShoppingCart_DataMigration(response.Availability.Reservation, shoppingCart, request, session);
            response.ShoppingCart = shoppingCart;

            await _sessionHelperService.SaveSession<MOBShoppingCart>(response.ShoppingCart, request.SessionId, new List<string> { request.SessionId, response.ShoppingCart.ObjectName }, response.ShoppingCart.ObjectName);

            //Persist.FilePersist.Save<MOBShoppingCart>(request.SessionId, response.shoppingCart.ObjectName, response.ShoppingCart);

            if (IsCallEligibleFOP(response.ShoppingCart))
            {
                var tupleRes = await _formsOfPayment.GetEligibleFormofPayments(request, session, shoppingCart, request.CartId, FlowType.BOOKING.ToString(), isDefault, response.Availability.Reservation);
                response.EligibleFormofPayments = tupleRes.response;
                isDefault = tupleRes.isDefault;
            }
            response.IsDefaultPaymentOption = isDefault;
            response.CartId = shoppingCart.CartId;
            response.Disclaimer = new List<string>();

            if (_configuration.GetValue<string>("MakeReservationDisclaimer") != null)
            {
                response.Disclaimer.Add(_configuration.GetValue<string>("MakeReservationDisclaimer").ToString());
            }
            else
            {
                response.Disclaimer.Add("*Miles shown are the actual miles flown for this segment.Mileage accrued will vary depending on the terms and conditions of your frequent flyer program.");
            }
            if (response.Availability != null)
            {
                session.CartId = response.Availability.CartId;
                await _sessionHelperService.SaveSession(session, request.SessionId, new List<string> { request.SessionId, session.ObjectName }, session.ObjectName);

                //FilePersist.Save(session.SessionId, session.ObjectName, session);
            }
            //Covid-19 Emergency WHO TPI content
            if (_configuration.GetValue<bool>("ToggleCovidEmergencytextTPI") == true)
            {
                bool return_TPICOVID_19WHOMessage_For_BackwardBuilds = GeneralHelper.IsApplicationVersionGreater2(request.Application.Id, request.Application.Version.Major, "Android_Return_TPICOVID_19WHOMessage__For_BackwardBuilds", "iPhone_Return_TPICOVID_19WHOMessage_For_BackwardBuilds", string.Empty, string.Empty, _configuration);
                if (!return_TPICOVID_19WHOMessage_For_BackwardBuilds && response.Availability.Reservation != null
                    && response.Availability.Reservation.TripInsuranceInfoBookingPath != null && response.Availability.Reservation.TripInsuranceInfoBookingPath.tpiAIGReturnedMessageContentList != null
                    && response.Availability.Reservation.TripInsuranceInfoBookingPath.tpiAIGReturnedMessageContentList.Count > 0)
                {
                    MOBItem tpiCOVID19EmergencyAlertBookingPath = response.Availability.Reservation.TripInsuranceInfoBookingPath.tpiAIGReturnedMessageContentList.Find(p => p.Id.ToUpper().Trim() == "COVID19EmergencyAlert".ToUpper().Trim());
                    if (tpiCOVID19EmergencyAlertBookingPath != null)
                    {
                        response.Availability.Reservation.TripInsuranceInfoBookingPath.Tnc = response.Availability.Reservation.TripInsuranceInfoBookingPath.Tnc +
                            "<br><br>" + tpiCOVID19EmergencyAlertBookingPath.CurrentValue;
                    }
                }
            }
            return response;
        }

        public async Task<MOBResponse> ClearUnfinishedBookings(MOBSHOPUnfinishedBookingRequestBase request)
        {
            var actionName = request.IsOmniCartSavedTrip ? "ClearUnfinishedBookings_OmniCart" : "ClearUnfinishedBookings";
            var response = new MOBResponse();
            Session session = null;

            await ValidateVulnerability(request.MileagePlusAccountNumber, request.PasswordHash, request.Application.Id, request.DeviceId
                                       , request.Application.Version.Major, request.IsOmniCartSavedTrip ? _configuration.GetValue<string>("OmnicartExceptionMessage") : _configuration.GetValue<string>("bugBountySessionExpiredMsg"));

            session = await _shoppingSessionHelper.CreateShoppingSession(request.Application.Id, request.DeviceId, request.Application.Version.Major, request.TransactionId, request.MileagePlusAccountNumber, string.Empty);

            if (session == null)
            {
                throw new MOBUnitedException("Unable to get booking session.");
            }

            bool IsEnableOmniChannelCartServiceForClearUnfinishedBookings = _configuration.GetValue<bool>("EnableOmniChannelCartServiceForClearUnfinishedBookings") && (!string.IsNullOrEmpty(request.MileagePlusAccountNumber) || _configuration.GetValue<bool>("EnableOmniChannelCartServiceForClearUnfinishedBookingsForGuest"));
            if (IsEnableOmniChannelCartServiceForClearUnfinishedBookings)
            {
                await PurgeUnfinishedBookingsV2(session, request);
            }
            else
            {
                await PurgeUnfinishedBookings(session, request);
            }

            response.TransactionId = request.TransactionId;
            response.LanguageCode = request.LanguageCode;

            return await System.Threading.Tasks.Task.FromResult(response);
        }

        public async Task<List<MOBSHOPUnfinishedBooking>> GetUnfinishedBookings(Session session, MOBSHOPGetUnfinishedBookingsRequest request)
        {
            var ubs = new List<MOBSHOPUnfinishedBooking>();

            if (ConfigUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major) && request.IsCatalogOnForTavelerTypes)
            {
                foreach (var ub in request.UnfinishedBookings)
                {
                    if (ub.NumberOfAdults > 0 && (ub.TravelerTypes == null || ub.TravelerTypes.Count == 0))
                    {
                        ub.TravelerTypes = new List<MOBTravelerType> { new MOBTravelerType() { TravelerType = PAXTYPE.Adult.ToString(), Count = ub.NumberOfAdults } };
                    }
                }
            }

            if (session.CustomerID > 0)
            {
                var savedUBs = await _unfinishedBooking.GetSavedUnfinishedBookingEntries(session, request, request.IsCatalogOnForTavelerTypes);
                if (savedUBs != null)
                    ubs.AddRange(savedUBs);
            }

            if (request.UnfinishedBookings != null)
            {
                if (ConfigUtility.EnableSavedTripShowChannelTypes(request.Application.Id, request.Application.Version.Major))
                {
                    // Ensure the ubs that were sent from client has correct channel
                    foreach (var ub in request.UnfinishedBookings)
                    {
                        if (string.IsNullOrWhiteSpace(ub.ChannelType))
                            ub.ChannelType = _configuration.GetValue<string>("Shopping - ChannelType");
                    }
                }

                ubs.AddRange(request.UnfinishedBookings);
            }
            MOBSHOPUnfinishedBookingComparer comparer = new MOBSHOPUnfinishedBookingComparer();
            // remove duplicate here
            ubs = ubs.Distinct(comparer).ToList();

            var result = new List<MOBSHOPUnfinishedBooking>();
            Parallel.ForEach(PickTopFiveMostRecentUnfinisedBookingsBasedOnExecutionDateCST(session, request, ubs),
                () => false,
                (ub, loopState, skipped) =>
                {
                    try
                    {


                       var tupleResponse = _unfinishedBooking.GetShopPinDown(session, request.Application.Version.Major, (ConfigUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major) && request.IsCatalogOnForTavelerTypes) ?
                           _unfinishedBooking.BuildShopPinDownRequest(ub, request.MileagePlusAccountNumber, request.LanguageCode, request.Application.Id, request.Application.Version.Major, request.IsCatalogOnForTavelerTypes) :
                           _unfinishedBooking.BuildShopPinDownRequest(ub, request.MileagePlusAccountNumber, request.LanguageCode)).Result;
                        var pindown = tupleResponse.Item1;

                        ub.IsELF = pindown.DisplayCart.IsElf;
                        ub.IsIBE = ConfigUtility.IsIBEFullFare(pindown.DisplayCart);
                        ub.DisplayPrice = string.Empty; // clear out the price in case client sending with prices

                        var total = pindown.DisplayCart.DisplayPrices.FirstOrDefault(p => p.Type.Equals("Total"));
                        if (total != null)
                        {
                            var roundedUpPrice = Math.Round(total.Amount, 0, MidpointRounding.AwayFromZero);
                            ub.DisplayPrice = string.Format("{0}{1}", ConfigUtility.GetCurrencySymbol(TopHelper.GetCultureInfo(pindown.DisplayCart.CountryCode)), ShopStaticUtility.GetThousandPlaceCommaDelimitedNumberString(roundedUpPrice.ToString()));
                        }

                        for (int i = 0; i < ub.Trips.Count(); i++)
                        {
                            var tripLastFlight = pindown.DisplayCart.DisplayTrips[i].Flights.Last();
                            var destDateTime = DateTime.Parse((tripLastFlight.Connections != null && tripLastFlight.Connections.Any() ? tripLastFlight.Connections.Last().DestinationDateTime : tripLastFlight.DestinationDateTime));
                            ub.Trips[i].ArrivalDate = destDateTime.ToString("MM/dd/yyyy");
                            ub.Trips[i].ArrivalTime = destDateTime.ToString("h:mm") + destDateTime.ToString("tt").ToLower();
                            var departDateTime = DateTime.Parse(pindown.DisplayCart.DisplayTrips[i].DepartDate);
                            ub.Trips[i].DepartDate = departDateTime.ToString("MM/dd/yyyy");
                            ub.Trips[i].DepartTime = departDateTime.ToString("h:mm") + departDateTime.ToString("tt").ToLower();
                            ub.Trips[i].DepartDateTimeGMT = pindown.DisplayCart.bookingDetails.solution[0].slice[i].segment[0].leg[0].departure;
                        }
                    }
                    catch (Exception ex)
                    {
                        //var exludeList = new[] { shopPinDownFareNotFoundMajorAndMinorCode, shopPinDownDepartedErrorMajorAndMinorCode };
                        //var majorAndMinorErrorList = ex.Message.Split(new[] { shopPinDownErrorSeparator }, StringSplitOptions.RemoveEmptyEntries);

                        //skipped = majorAndMinorErrorList.Except(exludeList).Any();

                        skipped = true; // always skip the ub that we cannot retrieve info from CSL FL service
                    }

                    if (!skipped)
                        result.Add(ub);
                    return skipped;
                },
                (skipped) => { });

            return SortUnfinishedBookingsByDepartureDateGMT(session, request, result);
        }
        private List<MOBSHOPUnfinishedBooking> SortUnfinishedBookingsByDepartureDateGMT(Session session, MOBRequest request, List<MOBSHOPUnfinishedBooking> inputList)
        {
            if (inputList == null || !inputList.Any())
                return inputList;

            try
            {
                return inputList.OrderBy(x => DateTimeOffset.Parse(x.Trips[0].DepartDateTimeGMT).UtcDateTime).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError("SortUnfinishedBookingsByDepartureDateGMT {@Exception}", new MOBExceptionWrapper(ex));
                return inputList; // returns original list if unable to sort it due to an expected error
            }
        }

        private bool IsCallEligibleFOP(MOBShoppingCart shoppingCart)
        {
            if (shoppingCart?.OmniCart != null)
            {
                return !shoppingCart.OmniCart.IsCallRegisterOffers;
            }
            return true;
        }
        private async Task<List<MOBSHOPUnfinishedBooking>> GetUnfinishedBookingsV2(Session session, MOBSHOPGetUnfinishedBookingsRequest request)
        {
            string cslEndpoint = _configuration.GetValue<string>("ServiceEndPointBaseUrl - CCEPromo");
            var ccePersonalizationRequest = BuildccePersonalizationRequest(request);
            string jsonRequest = JsonConvert.SerializeObject(ccePersonalizationRequest);
            ContextualCommResponse response = new ContextualCommResponse();

            string url = string.Format("{0}", "mobile/messages");
            var jsonResponse = await _bundleOfferService.UnfinishedBookings<ContextualCommResponse>(session.Token, session.SessionId, url, jsonRequest);

            List<MOBSHOPUnfinishedBooking> unfinishedBookings = new List<MOBSHOPUnfinishedBooking>();
            if (jsonResponse.response != null)
            {
                response = jsonResponse.response; // JsonSerializer.Deserialize<ContextualCommResponse>(jsonResponse);
                if (response.Components != null && response.Components.Any())
                {
                    var cceComponent = response.Components.FirstOrDefault(x => x.Name.ToUpper().Contains("SAVEDTRIPS"));
                    if (cceComponent != null && cceComponent.ContextualElements != null)
                    {
                        foreach (var item in cceComponent.ContextualElements)
                        {
                            var contextualItemValueJson = Newtonsoft.Json.JsonConvert.SerializeObject(item.Value);
                            Value contextualItemValue = Newtonsoft.Json.JsonConvert.DeserializeObject<Value>(contextualItemValueJson);
                            if (contextualItemValue.Itinerary != null && contextualItemValue.Itinerary.SavedItinerary != null)
                            {
                                MOBSHOPUnfinishedBooking unfinishedBooking = new MOBSHOPUnfinishedBooking();
                                unfinishedBooking = MapToMOBSHOPUnfinishedBookingTtypes(contextualItemValue.Itinerary, request);
                                unfinishedBookings.Add(unfinishedBooking);
                            }
                        }
                    }
                    //Log CCE Response if it is partial failure 
                    if (IsAnyErrorExistsInCCEResponse(response))
                    {
                        _logger.LogWarning("GetUnfinishedBookings {@CCEPartialFailure}", JsonConvert.SerializeObject(response));
                    }
                }
                else
                {
                    if (response.Error != null && response.Error.Count > 0)
                    {
                        bool isUCDErrorExists = response.Error.ToList().Exists(e => e.ErrorType == "MPNumUcdResponse" && e.Text.Contains("404"));
                        if (!isUCDErrorExists)
                        {
                            throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                        }
                    }
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            return unfinishedBookings;
        }

        private List<MOBSHOPUnfinishedBooking> PickTopFiveMostRecentUnfinisedBookingsBasedOnExecutionDateCST(Session session, MOBRequest request, List<MOBSHOPUnfinishedBooking> inputList)
        {
            if (inputList == null || !inputList.Any())
                return inputList;

            try
            {
                return inputList.OrderByDescending(x => DateTime.Parse(ConvertEpochTimeToDateTime(x.SearchExecutionDate, request.Application))).Take(5).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError("PickTopFiveMostRecentUnfinisedBookingsBasedOnExecutionDateCST {@Exception}", new MOBExceptionWrapper(ex));

                return inputList; // returns original list if unable to pick top 5 due to an expected error
            }
        }
        private string ConvertEpochTimeToDateTime(string date, MOBApplication application)
        {
            if (!_configuration.GetValue<bool>("EnableDateFixForGetUnFinishedBookings") && application != null && !String.IsNullOrEmpty(date) && GeneralHelper.IsApplicationVersionGreaterorEqual(application.Id, application.Version.Major.ToString(), _configuration.GetValue<string>("Android_EnableDateCheckForUnfinishedBookings"), _configuration.GetValue<string>("iPhone_EnableDateCheckForUnfinishedBookings")) && !date.Contains("/"))
            {
                long.TryParse(date, out long milliseconds);
                return (new DateTime(1970, 1, 1).AddMilliseconds(milliseconds)).ToString();
            }
            return date;
        }
        private bool IsAnyErrorExistsInCCEResponse(ContextualCommResponse response)
        {
            if (!_configuration.GetValue<bool>("DisableCCEErrorLogging"))
            {
                try
                {
                    if (response.Error != null && response.Error.Count > 0)
                    {
                        return true;
                    }
                }
                catch
                {

                }
            }
            return false;
        }
        private MOBSHOPUnfinishedBooking MapToMOBSHOPUnfinishedBookingTtypes(CCEItinerary cslEntry, MOBRequest request)
        {
            var ub = cslEntry.SavedItinerary;
            MOBSHOPUnfinishedBooking mobEntry = new MOBSHOPUnfinishedBooking();
            mobEntry.SearchExecutionDate = new[] { cslEntry.InsertTimestamp, cslEntry.UpdateTimestamp }.FirstOrDefault(x => !string.IsNullOrEmpty(x));
            mobEntry.DisplayPrice = cslEntry.ItineraryDisplayPrice;
            mobEntry.IsELF = cslEntry.IsELF;
            mobEntry.IsIBE = cslEntry.IsIBE;
            mobEntry.TravelerTypes = new List<MOBTravelerType>();
            GetTravelTypesFromShop(ub, mobEntry);
            mobEntry.CountryCode = ub.CountryCode;
            mobEntry.SearchType = GetSeachTypeSelection((United.Services.FlightShopping.Common.SearchType)ub.SearchTypeSelection);
            mobEntry.Trips = ub.Trips.Select(MapToMOBSHOPUnfinishedBookingTrip).ToList();
            mobEntry.Id = ub.AccessCode;
            mobEntry.ChannelType = ub.ChannelType;
            return mobEntry;
        }
        private string GetSeachTypeSelection(United.Services.FlightShopping.Common.SearchType searchType)
        {
            var result = string.Empty;
            try
            {
                return new Dictionary<United.Services.FlightShopping.Common.SearchType, string>
                {
                    {United.Services.FlightShopping.Common.SearchType.OneWay, "OW"},
                    {United.Services.FlightShopping.Common.SearchType.RoundTrip, "RT"},
                    {United.Services.FlightShopping.Common.SearchType.MultipleDestination, "MD"},
                    {United.Services.FlightShopping.Common.SearchType.ValueNotSet, string.Empty},
                }[searchType];
            }
            catch { return result; }
        }
        private static void GetTravelTypesFromShop(CCESavedItinerary ub, MOBSHOPUnfinishedBooking mobEntry)
        {
            foreach (var t in ub.PaxInfoList.GroupBy(p => p.PaxType))
            {
                switch ((int)t.Key)
                {
                    case (int)United.Services.FlightShopping.Common.PaxType.Adult:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Adult.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.Senior:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Senior.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.Child01:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child2To4.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.Child02:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child5To11.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.Child03:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child12To17.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.Child04:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child12To14.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.Child05:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child15To17.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.InfantSeat:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.InfantSeat.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.InfantLap:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.InfantLap.ToString() });
                        break;
                    default:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Adult.ToString() });
                        break;
                }
            }
        }
        private MOBSHOPUnfinishedBookingTrip MapToMOBSHOPUnfinishedBookingTrip(CCESavedItinerary.Trip csTrip)
        {
            return new MOBSHOPUnfinishedBookingTrip
            {
                DepartDate = csTrip.DepartDate,
                DepartTime = csTrip.DepartTime,
                ArrivalTime = csTrip.ArrivalTime,
                ArrivalDate = csTrip.ArrivalDate,
                Destination = csTrip.Destination,
                Origin = csTrip.Origin,
                Flights = csTrip.Flights.Select(MapToMOBSHOPUnfinishedBookingFlight).ToList()
            };
        }


        private bool IsInclueWithThisToggle(int appId, string appVersion, string configToggleKey, string androidVersion, string iosVersion)
        {
            return _configuration.GetValue<bool>(configToggleKey) &&
                   GeneralHelper.IsApplicationVersionGreater(appId, appVersion, androidVersion, iosVersion, "", "", true, _configuration);
        }

        private double GetAllowedFFCAmount(List<ProdDetail> products, bool isAncillaryFFCEnable = false)
        {
            string allowedFFCProducts = string.Empty;

            allowedFFCProducts = _configuration.GetValue<string>("FFCEligibleProductCodes");
            double allowedFFCAmount = products == null ? 0 : products.Where(p => (allowedFFCProducts.IndexOf(p.Code) > -1) && !string.IsNullOrEmpty(p.ProdTotalPrice)).Sum(a => Convert.ToDouble(a.ProdTotalPrice));
            allowedFFCAmount += GetAncillaryAmount(products, isAncillaryFFCEnable);
            allowedFFCAmount = Math.Round(allowedFFCAmount, 2, MidpointRounding.AwayFromZero);
            return allowedFFCAmount;
        }

        private double GetAncillaryAmount(List<ProdDetail> products, bool isAncillaryFFCEnable = false)
        {
            double allowedFFCAmount = 0;
            if (isAncillaryFFCEnable)
            {
                string allowedFFCProducts = _configuration.GetValue<string>("TravelCreditEligibleProducts");
                allowedFFCAmount += products == null ? 0 : products.Where(p => (allowedFFCProducts.IndexOf(p.Code) > -1) && !string.IsNullOrEmpty(p.ProdTotalPrice)).Sum(a => Convert.ToDouble(a.ProdTotalPrice));
                allowedFFCAmount += GetBundlesAmount(products);
                allowedFFCAmount = Math.Round(allowedFFCAmount, 2, MidpointRounding.AwayFromZero);
            }
            return allowedFFCAmount;
        }

        private double GetBundlesAmount(List<ProdDetail> products)
        {
            string nonBundleProductCode = _configuration.GetValue<string>("NonBundleProductCode");
            double bundleAmount = products == null ? 0 : products.Where(p => (nonBundleProductCode.IndexOf(p.Code) == -1) && !string.IsNullOrEmpty(p.ProdTotalPrice)).Sum(a => Convert.ToDouble(a.ProdTotalPrice));
            return bundleAmount;
        }

        private void ApplyFFCToAncillary(List<ProdDetail> products, MOBApplication application, MOBFormofPaymentDetails mobFormofPaymentDetails, List<MOBSHOPPrice> prices)
        {
            bool isAncillaryFFCEnable = IsInclueWithThisToggle(application.Id, application.Version.Major, "EnableTravelCreditAncillary", "AndroidTravelCreditVersionAncillary", "iPhoneTravelCreditVersionAncillary");
            var futureFlightCredits = mobFormofPaymentDetails.TravelFutureFlightCredit?.FutureFlightCredits;
            if (isAncillaryFFCEnable && futureFlightCredits?.Count > 0)
            {
                mobFormofPaymentDetails.TravelFutureFlightCredit.AllowedFFCAmount = GetAllowedFFCAmount(products, isAncillaryFFCEnable);
                mobFormofPaymentDetails.TravelFutureFlightCredit.totalAllowedAncillaryAmount = GetAncillaryAmount(products, isAncillaryFFCEnable);


                var travelCredits = mobFormofPaymentDetails.TravelCreditDetails.TravelCredits.Where(tc => futureFlightCredits.Exists(ffc => ffc.PinCode == tc.PinCode)).ToList();
                //var newValueAfterRedeem = travelCredits.Sum(tc => tc.NewValueAfterRedeem);
                int index = 0;
                foreach (var travelCredit in travelCredits)
                {

                    double ffcAppliedToAncillary = 0;
                    ffcAppliedToAncillary = futureFlightCredits.Where(ffc => ffc.TravelerNameIndex == "ANCILLARY").Sum(t => t.RedeemAmount);
                    ffcAppliedToAncillary = Math.Round(ffcAppliedToAncillary, 2, MidpointRounding.AwayFromZero);
                    var existedFFC = futureFlightCredits.FirstOrDefault(f => f.TravelerNameIndex == "ANCILLARY" && f.PinCode == travelCredit.PinCode);
                    double alreadyAppliedAmount = futureFlightCredits.Where(f => f.PinCode == travelCredit.PinCode).Sum(p => p.RedeemAmount);
                    var balanceAfterAppliedToRESAndAncillary = travelCredit.CurrentValue - alreadyAppliedAmount;

                    if (balanceAfterAppliedToRESAndAncillary > 0 &&
                        ffcAppliedToAncillary < mobFormofPaymentDetails.TravelFutureFlightCredit?.totalAllowedAncillaryAmount &&
                        existedFFC == null)
                    {
                        index++;
                        var mobFFC = new MOBFOPFutureFlightCredit();
                        mobFFC.CreditAmount = travelCredit.CreditAmount;
                        mobFFC.ExpiryDate = Convert.ToDateTime(travelCredit.ExpiryDate).ToString("MMMMM dd, yyyy");
                        mobFFC.IsCertificateApplied = true;
                        mobFFC.InitialValue = travelCredit.InitialValue;
                        mobFFC.Index = index;
                        mobFFC.PinCode = travelCredit.PinCode;
                        mobFFC.PromoCode = travelCredit.PromoCode;
                        mobFFC.RecordLocator = travelCredit.RecordLocator;
                        mobFFC.TravelerNameIndex = "ANCILLARY";
                        double remainingBalanceAfterAppliedFFC = mobFormofPaymentDetails.TravelFutureFlightCredit.totalAllowedAncillaryAmount - ffcAppliedToAncillary;
                        mobFFC.RedeemAmount = remainingBalanceAfterAppliedFFC > balanceAfterAppliedToRESAndAncillary ? balanceAfterAppliedToRESAndAncillary : remainingBalanceAfterAppliedFFC;
                        mobFFC.RedeemAmount = Math.Round(mobFFC.RedeemAmount, 2, MidpointRounding.AwayFromZero);
                        mobFFC.DisplayRedeemAmount = (mobFFC.RedeemAmount).ToString("C2", CultureInfo.CurrentCulture);
                        mobFFC.NewValueAfterRedeem = travelCredit.CurrentValue - (mobFFC.RedeemAmount + alreadyAppliedAmount);
                        mobFFC.NewValueAfterRedeem = Math.Round(mobFFC.NewValueAfterRedeem, 2, MidpointRounding.AwayFromZero);
                        mobFFC.DisplayNewValueAfterRedeem = (mobFFC.NewValueAfterRedeem).ToString("C2", CultureInfo.CurrentCulture);
                        mobFFC.IsCertificateApplied = true;
                        mobFFC.CurrentValue = travelCredit.CurrentValue;
                        futureFlightCredits.Add(mobFFC);
                    }
                    else if (existedFFC != null)
                    {
                        double remainingBalanceAfterAppliedFFC = (mobFormofPaymentDetails.TravelFutureFlightCredit.totalAllowedAncillaryAmount - ffcAppliedToAncillary) + existedFFC.RedeemAmount;
                        existedFFC.NewValueAfterRedeem += existedFFC.RedeemAmount;
                        existedFFC.RedeemAmount = 0;
                        //double remainingBalanceAfterAppliedFFC = mobFormofPaymentDetails.TravelFutureFlightCredit.AllowedFFCAmount - ffcAppliedToAncillary;
                        existedFFC.RedeemAmount = remainingBalanceAfterAppliedFFC > existedFFC.NewValueAfterRedeem ? existedFFC.NewValueAfterRedeem : remainingBalanceAfterAppliedFFC;
                        existedFFC.RedeemAmount = Math.Round(existedFFC.RedeemAmount, 2, MidpointRounding.AwayFromZero);
                        existedFFC.DisplayRedeemAmount = (existedFFC.RedeemAmount).ToString("C2", CultureInfo.CurrentCulture);
                        existedFFC.NewValueAfterRedeem -= existedFFC.RedeemAmount;
                        existedFFC.NewValueAfterRedeem = Math.Round(existedFFC.NewValueAfterRedeem, 2, MidpointRounding.AwayFromZero);
                        existedFFC.DisplayNewValueAfterRedeem = (existedFFC.NewValueAfterRedeem).ToString("C2", CultureInfo.CurrentCulture);
                    }

                }

                futureFlightCredits.RemoveAll(f => f.RedeemAmount <= 0);
                _fFCShoppingcs.UpdatePricesInReservation(mobFormofPaymentDetails.TravelFutureFlightCredit, prices);
                _fFCShoppingcs.AssignIsOtherFOPRequired(mobFormofPaymentDetails, prices);
                _fFCShoppingcs.AssignFormOfPaymentType(mobFormofPaymentDetails, prices, false);
            }
        }

        private void UpdateTravelCreditRedeemAmount(United.Mobile.Model.Shopping.FormofPayment.MOBFOPTravelCredit travelCredit, double redeemAmount)
        {
            travelCredit.RedeemAmount = redeemAmount;
            travelCredit.RedeemAmount = Math.Round(travelCredit.RedeemAmount, 2, MidpointRounding.AwayFromZero);
            travelCredit.DisplayRedeemAmount = (redeemAmount).ToString("C2", CultureInfo.CurrentCulture);
            travelCredit.NewValueAfterRedeem = travelCredit.CurrentValue - redeemAmount;
            travelCredit.NewValueAfterRedeem = Math.Round(travelCredit.NewValueAfterRedeem, 2, MidpointRounding.AwayFromZero);
            travelCredit.DisplayNewValueAfterRedeem = (travelCredit.NewValueAfterRedeem).ToString("C2", CultureInfo.CurrentCulture);
            travelCredit.IsApplied = redeemAmount > 0;
        }
        private void UpdateTravelCreditAmountWithSelectedETCOrFFC(MOBFormofPaymentDetails formofPaymentDetails, List<MOBSHOPPrice> prices, List<MOBCPTraveler> travelers)
        {
            if (formofPaymentDetails?.TravelCreditDetails?.TravelCredits?.Count > 0)
            {
                bool isETC = (formofPaymentDetails?.TravelCertificate?.Certificates?.Count > 0);
                bool isFFC = (formofPaymentDetails?.TravelFutureFlightCredit?.FutureFlightCredits?.Count > 0);

                bool isTravelerFFC_Check = !_configuration.GetValue<bool>("DisableMPSignedInInsertUpdateTraveler");

                foreach (var travelCredit in formofPaymentDetails.TravelCreditDetails.TravelCredits)
                {
                    double redeemAmount = 0;
                    if (isETC)
                    {
                        var cert = formofPaymentDetails.TravelCertificate.Certificates.Where(c => c.PinCode == travelCredit.PinCode).ToList();
                        if (cert != null)
                        {
                            redeemAmount = cert.Sum(c => c.RedeemAmount);
                        }
                    }

                    if (isFFC)
                    {
                        var ffcs = formofPaymentDetails.TravelFutureFlightCredit.FutureFlightCredits.Where(c => c.PinCode == travelCredit.PinCode).ToList();
                        if (ffcs != null)
                        {
                            redeemAmount = ffcs.Sum(c => c.RedeemAmount);
                        }
                    }
                    UpdateTravelCreditRedeemAmount(travelCredit, redeemAmount);
                }
                if (isFFC)
                {
                    IEnumerable<United.Mobile.Model.Shopping.FormofPayment.MOBFOPTravelCredit> ancillaryTCs = formofPaymentDetails.TravelCreditDetails.TravelCredits.Where(tc => tc.IsApplied);
                    foreach (var ancillaryTC in ancillaryTCs)
                    {
                        foreach (var scTraveler in isTravelerFFC_Check ? travelers.Where(trav => trav.FutureFlightCredits != null).Where(tc => tc.FutureFlightCredits.Exists(f => f.PinCode == ancillaryTC.PinCode)) : travelers.Where(trav => trav.FutureFlightCredits.Exists(f => f.PinCode == ancillaryTC.PinCode)))
                        {
                            var travelerFFC = scTraveler.FutureFlightCredits.FirstOrDefault(ffc => ffc.PinCode == ancillaryTC.PinCode);
                            if (travelerFFC != null)
                            {
                                travelerFFC.NewValueAfterRedeem = 0;
                                travelerFFC.NewValueAfterRedeem = Math.Round(travelerFFC.NewValueAfterRedeem, 2, MidpointRounding.AwayFromZero);
                                travelerFFC.DisplayNewValueAfterRedeem = (travelerFFC.NewValueAfterRedeem).ToString("C2", CultureInfo.CurrentCulture);
                                _fFCShoppingcs.AssignTravelerTotalFFCNewValueAfterReDeem(scTraveler);
                            }
                        }
                        foreach (var tnIndex in ancillaryTC.EligibleTravelers)
                        {
                            MOBCPTraveler traveler = isTravelerFFC_Check ? travelers.Where(tc => tc.FutureFlightCredits != null).FirstOrDefault(trav => trav.TravelerNameIndex == tnIndex) : travelers.FirstOrDefault(trav => trav.TravelerNameIndex == tnIndex);
                            var travelerFFC = traveler?.FutureFlightCredits.FirstOrDefault(ffc => ffc.PinCode == ancillaryTC.PinCode);
                            if (travelerFFC != null)
                            {
                                travelerFFC.NewValueAfterRedeem = ancillaryTC.NewValueAfterRedeem / ancillaryTC.EligibleTravelers.Count;
                                travelerFFC.NewValueAfterRedeem = Math.Round(travelerFFC.NewValueAfterRedeem, 2, MidpointRounding.AwayFromZero);
                                travelerFFC.DisplayNewValueAfterRedeem = (travelerFFC.NewValueAfterRedeem).ToString("C2", CultureInfo.CurrentCulture);
                                _fFCShoppingcs.AssignTravelerTotalFFCNewValueAfterReDeem(traveler);
                            }
                        }
                    }
                    var newRefundValueAfterReeedeem = ancillaryTCs.Sum(tcAmount => tcAmount.NewValueAfterRedeem);
                    var refundPrice = prices.FirstOrDefault(p => p.PriceType == "REFUNDPRICE");
                    if (refundPrice != null)
                    {
                        if (newRefundValueAfterReeedeem > 0)
                        {
                            _fFCShoppingcs.UpdateCertificatePrice(refundPrice, newRefundValueAfterReeedeem, "REFUNDPRICE", "Total Credit", "RESIDUALCREDIT");
                        }
                        else
                        {
                            prices.RemoveAll(p => p.PriceType == "REFUNDPRICE");
                        }
                    }
                }
            }
        }
        private bool isApplicationVersionGreaterForBundles(int applicationID, string appVersion, string androidnontfaversion,
                    string iphonenontfaversion, string windowsnontfaversion, string mWebNonELFVersion)
        {
            #region Prasad Code for version check
            bool ValidTFAVersion = false;
            if (!string.IsNullOrEmpty(appVersion))
            {
                string AndroidNonTFAVersion = _configuration.GetValue<string>(androidnontfaversion) ?? "";
                string iPhoneNonTFAVersion = _configuration.GetValue<string>(iphonenontfaversion) ?? "";
                string WindowsNonTFAVersion = _configuration.GetValue<string>(windowsnontfaversion) ?? "";
                string MWebNonTFAVersion = _configuration.GetValue<string>(mWebNonELFVersion) ?? "";

                Regex regex = new Regex("[0-9.]");
                appVersion = string.Join("",
                    regex.Matches(appVersion).Cast<Match>().Select(match => match.Value).ToArray());
                if (applicationID == 1 && appVersion != iPhoneNonTFAVersion)
                {
                    ValidTFAVersion = isVersion1Greater(appVersion, iPhoneNonTFAVersion);
                }
                else if (applicationID == 2 && appVersion != AndroidNonTFAVersion)
                {
                    ValidTFAVersion = isVersion1Greater(appVersion, AndroidNonTFAVersion);
                }
                else if (applicationID == 6 && appVersion != WindowsNonTFAVersion)
                {
                    ValidTFAVersion = isVersion1Greater(appVersion, WindowsNonTFAVersion);
                }
                else if (applicationID == 16 && appVersion != MWebNonTFAVersion)
                {
                    ValidTFAVersion = isVersion1Greater(appVersion, MWebNonTFAVersion);
                }
            }
            #endregion

            return ValidTFAVersion;
        }

        private bool isVersion1Greater(string version1, string version2)
        {
            return SeperatedVersionCompareCommonCode(version1, version2);
        }

        private bool SeperatedVersionCompareCommonCode(string version1, string version2)
        {
            try
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
            }
            catch
            {
            }
            return false;
        }
        public RegisterFlightsRequest BuildRegisterFlightRequest(FlightReservationResponse flightReservationResponse, string flow, MOBRequest mobRequest)
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
            request.WorkFlowType = ConfigUtility.GetWorkFlowType(flow);
            return request;
        }
        private async Task<FlightReservationResponse> RegisterFlights(FlightReservationResponse flightReservationResponse, Session session, MOBRequest request)
        {
            //string cslEndpoint = _configuration.GetValue<string>("ServiceEndPointBaseUrl - ShoppingCartService");
            string flow = session.IsNullOrEmpty() && session.IsReshopChange ? FlowType.RESHOP.ToString() : FlowType.BOOKING.ToString();
            var registerFlightRequest = BuildRegisterFlightRequest(flightReservationResponse, flow, request);

            if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == TravelType.TPBooking.ToString())
            {
                //var cslShopRequest = FilePersist.Load<CSLShopRequest>(session.SessionId, new CSLShopRequest().ObjectName)?.ShopRequest;
                var cslShopRequest = await _sessionHelperService.GetSession<Model.Shopping.CSLShopRequest>(session.SessionId, new Model.Shopping.CSLShopRequest().ObjectName, new List<string> { session.SessionId, new Model.Shopping.CSLShopRequest().ObjectName }).ConfigureAwait(false);

                registerFlightRequest.TravelPlanId = cslShopRequest?.ShopRequest?.TravelPlanId;
                registerFlightRequest.TravelPlanCartId = cslShopRequest?.ShopRequest?.TravelPlanCartId;
            }
            string jsonRequest = JsonConvert.SerializeObject(registerFlightRequest);
            FlightReservationResponse response = new FlightReservationResponse();


            //string url = string.Format("{0}/{1}", cslEndpoint, "RegisterFlights");


            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cSLCallDurationstopwatch1;
            cSLCallDurationstopwatch1 = new Stopwatch();
            cSLCallDurationstopwatch1.Reset();
            cSLCallDurationstopwatch1.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******
            string action = "RegisterFlights";
            response = await _shoppingCartService.RegisterFlights<FlightReservationResponse>(session.Token, action, jsonRequest, session.SessionId);
            //HttpHelper.Post(url, "application/json; charset=utf-8", session.Token, jsonRequest, httpPostTimeOut, httpPostNumberOfRetry);

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cSLCallDurationstopwatch1.IsRunning)
            {
                cSLCallDurationstopwatch1.Stop();
            }
            #endregion/***Get Call Duration Code - Venkat 03/17/2015*******


            if (response != null)
            {
                //response = JsonConvert.DeserializeObject<FlightReservationResponse>(jsonResponse);

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
                if ((_omniCart.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, true)
                    || _shoppingUtility.EnableAdvanceSearchCouponBooking(request.Application.Id, request.Application.Version.Major))
                    && flow == FlowType.BOOKING.ToString())
                {
                    var persistShoppingCart = new MOBShoppingCart();
                    //persistShoppingCart = FilePersist.Load<MOBShoppingCart>(session.SessionId, persistShoppingCart.ObjectName, false);
                    persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(session.SessionId, persistShoppingCart.ObjectName, new List<string> { session.SessionId, persistShoppingCart.ObjectName }).ConfigureAwait(false);


                    if (persistShoppingCart == null)
                    {
                        persistShoppingCart = new MOBShoppingCart();
                    }
                    persistShoppingCart.Products = await _shoppingUtility.ConfirmationPageProductInfo(response, false, request.Application, null, flow, sessionId: session?.SessionId);
                    double price = _shoppingUtility.GetGrandTotalPriceForShoppingCart(false, response, false, flow);
                    persistShoppingCart.TotalPrice = String.Format("{0:0.00}", price);
                    persistShoppingCart.DisplayTotalPrice = Decimal.Parse(price.ToString()).ToString("c");
                    persistShoppingCart.PromoCodeDetails = AddAFSPromoCodeDetails(response.DisplayCart);


                    await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, session.SessionId, new List<string> { session.SessionId, persistShoppingCart.ObjectName }, persistShoppingCart.ObjectName).ConfigureAwait(false);
                    //Persist.FilePersist.Save<MOBShoppingCart>(session.SessionId, persistShoppingCart.ObjectName, persistShoppingCart);
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            return response;
        }

        private MOBPromoCodeDetails AddAFSPromoCodeDetails(DisplayCart displayCart)
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

        private bool isAFSCouponApplied(DisplayCart displayCart)
        {
            if (displayCart != null && displayCart.SpecialPricingInfo != null && displayCart.SpecialPricingInfo.MerchOfferCoupon != null && !string.IsNullOrEmpty(displayCart.SpecialPricingInfo.MerchOfferCoupon.PromoCode) && displayCart.SpecialPricingInfo.MerchOfferCoupon.IsCouponEligible.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private async Task<List<MOBItem>> GetELFShopMessagesForRestrictions(MOBSHOPReservation reservation, int appId = 0)
        {
            if (reservation == null) return null;

            var databaseKey = string.Empty;

            if (reservation.IsELF)
            {
                databaseKey = reservation.IsSSA ?
                              "SSA_ELF_RESTRICTIONS_APPLY_MESSAGES" :
                              "ELF_RESTRICTIONS_APPLY_MESSAGES";
            }
            else if (reservation.ShopReservationInfo2 != null && reservation.ShopReservationInfo2.IsIBELite)
            {
                databaseKey = "IBELite_RESTRICTIONS_APPLY_MESSAGES";
            }
            else if (reservation.ShopReservationInfo2 != null && reservation.ShopReservationInfo2.IsIBE)
            {
                if (ConfigUtility.EnablePBE())
                {
                    string productCode = reservation?.Trips[0]?.FlattenedFlights?[0]?.Flights?[0].ShoppingProducts.First(p => p != null && p.IsIBE).ProductCode;
                    databaseKey = productCode + "_RESTRICTIONS_APPLY_MESSAGES";
                }
                else
                {
                    databaseKey = "IBE_RESTRICTIONS_APPLY_MESSAGES";
                }
            }
            
            var messages = !string.IsNullOrEmpty(databaseKey) ?
                   await GetMPPINPWDTitleMessages(new List<string> { databaseKey }) : null;

            if (!_configuration.GetValue<bool>("DisableRestrictionsForiOS"))
            {
                if (messages != null && appId == 1)
                {
                    try
                    {
                        var footNote = messages.Where(x => x.Id == _configuration.GetValue<string>("RestrictionsLimitationsFootNotes")).FirstOrDefault();
                        if (footNote != null && footNote?.CurrentValue != null)
                        {
                            if (footNote.CurrentValue.StartsWith("<p>"))
                            {
                                footNote.CurrentValue = footNote.CurrentValue.Replace("<p>", "").Replace("</p>", "").Replace("<br/><br/>", "\n\n");
                            }
                        }
                    }
                    catch (Exception ex) { }
                }
            }

            return messages;
        }

        private async Task<MOBSHOPAvailability> GetShopPinDownDetailsV2(Session session, MOBSHOPSelectUnfinishedBookingRequest request, HttpContext httpContext = null)
        {
            var availability = new MOBSHOPAvailability { SessionId = session.SessionId };
            string cartid = string.Empty;
            var response = default(FlightReservationResponse);
            var isOmniCartHomeScreenChanges = request.IsOmniCartSavedTrip && _omniCart.IsEnableOmniCartHomeScreenChanges(request.Application.Id, request.Application.Version.Major);
            if (isOmniCartHomeScreenChanges)
            {
                response = await _omniCart.GetFlightReservationResponseByCartId(session.SessionId, request.CartId);
                if (await _featureSettings.GetFeatureSettingValue("EnableAddTravelers").ConfigureAwait(false))
                {
                    await _sessionHelperService.SaveSession(response, session.SessionId, new List<string> { session.SessionId, new FlightReservationResponse().GetType().FullName }, new FlightReservationResponse().GetType().FullName).ConfigureAwait(false);
                }
                if (response == null)
                {
                    throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage")); // omni cart persist file not found hence rasing exception forcefully
                }
                if (_omniCart.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, true) || !_configuration.GetValue<bool>("Disable_NoBundleOffer_NavigationFix"))
                {
                    await _omniCart.BuildShoppingCart(request, response, FlowType.BOOKING.ToString(), request.CartId, request.SessionId);
                }
            }
            else
            {
                Guid cartId = new Guid(session.SessionId);
                cartid = cartId.ToString().ToUpper();
                ShopRequest shopPindownRequest = BuildShopPinDownDetailsRequest(request, cartid);
                //FilePersist.Save(session.SessionId, typeof(ShopRequest).FullName, shopPindownRequest);// SAVING THIS SHOP PIN DOWN REQUEST TO USE LATER ON FOR GET BUNDLES CALL
                await _sessionHelperService.SaveSession(shopPindownRequest, session.SessionId, new List<string> { session.SessionId, typeof(ShopRequest).FullName, }, typeof(ShopRequest).FullName).ConfigureAwait(false);// SAVING THIS SHOP PIN DOWN REQUEST TO USE LATER ON FOR GET BUNDLES CALL

               var tupleResponse = await  _unfinishedBooking.GetShopPinDown(session, request.Application.Version.Major, shopPindownRequest);
                var shoppindownResponse = tupleResponse.Item1;
                if ((await _featureSettings.GetFeatureSettingValue("EnableAddTravelers").ConfigureAwait(false)))
                {
                    await _sessionHelperService.SaveSession(shoppindownResponse, session.SessionId, new List<string> { session.SessionId, new FlightReservationResponse().GetType().FullName }, new FlightReservationResponse().GetType().FullName).ConfigureAwait(false);
                }
                if (shoppindownResponse != null && shoppindownResponse.Status.Equals(Css.SecureProfile.Types.StatusType.Success) && shoppindownResponse.Reservation != null)
                {
                    try
                    {
                        response = await RegisterFlights(shoppindownResponse, session, request);
                    }
                    catch (System.Net.WebException wex)
                    {
                        throw wex;
                    }
                    catch (MOBUnitedException uaex)
                    {
                        throw uaex;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    #region Populate properties which are missing from RegisterFlights Response
                    _unfinishedBooking.AssignMissingPropertiesfromRegisterFlightsResponse(shoppindownResponse, response);
                    #endregion
                }
            }
            if (response != null)
            {
                List<CMSContentMessage> lstMessages = await _ffcShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                var mobResRev = await _unfinishedBooking.PopulateReservation(session, response.Reservation);

                if (response.DisplayCart != null && response.DisplayCart.DisplayPrices != null && response.Reservation != null)
                {
                    availability.CartId = response.CartId;
                    availability.Reservation = new MOBSHOPReservation(_configuration, _cachingService);

                    if (!_configuration.GetValue<bool>("BasicEconomyRestrictionsForShareFlightsBugFixToggle") == true && request.SelectedUnfinishBooking != null && request.SelectedUnfinishBooking.IsSharedFlightRequest)
                    {
                        availability.Reservation.IsMetaSearch = true;
                    }

                    availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();
                    availability.Reservation.ShopReservationInfo2.Characteristics = UtilityHelper.GetCharacteristics(response.Reservation);
                    availability.Reservation.ShopReservationInfo2.IsIBELite = ConfigUtility.IsIBELiteFare(response.DisplayCart);
                    availability.Reservation.ShopReservationInfo2.IsIBE = ConfigUtility.IsIBEFullFare(response.DisplayCart);
                    availability.Reservation.ShopReservationInfo2.IsNonRefundableNonChangable = _shoppingUtility.IsNonRefundableNonChangable(response.DisplayCart);
                    availability.Reservation.ShopReservationInfo2.AllowAdditionalTravelers = !session.IsCorporateBooking;
                    availability.Reservation.ShopReservationInfo2.IsUnfinihedBookingPath = true;

                    if (_configuration.GetValue<bool>("EnableOmniCartReleaseCandidateTwoChanges_Bundles"))
                    {
                        availability.Reservation.ShopReservationInfo2.IsOmniCartSavedTripFlow = request.IsOmniCartSavedTrip;
                    }
                    availability.Reservation.IsSSA = _shoppingUtility.EnableSSA(request.Application.Id, request.Application.Version.Major);
                    availability.Reservation.IsELF = response.DisplayCart.IsElf;

                    availability.Reservation.IsUpgradedFromEntryLevelFare = response.DisplayCart.IsUpgradedFromEntryLevelFare && !ConfigUtility.IsIBELiteFare(response.DisplayCart.ProductCodeBeforeUpgrade);
                    availability.Reservation.PointOfSale = response.Reservation.PointOfSale.Country.CountryCode;
                    availability.Reservation.CartId = response.CartId;
                    availability.Reservation.SessionId = session.SessionId;

                    availability.Reservation.NumberOfTravelers = response.Reservation.NumberInParty;
                    availability.Reservation.Trips = await PopulateMetaTrips(new MOBSHOPDataCarrier(), response.CartId, null, session.SessionId, request.Application.Id, request.DeviceId, request.Application.Version.Major, true, response.DisplayCart.DisplayTrips, "", new List<string>());
                    availability.Reservation.ELFMessagesForRTI = await GetELFShopMessagesForRestrictions(availability.Reservation, request.Application.Id);
                    switch (response.DisplayCart.SearchType)
                    {
                        case United.Services.FlightShopping.Common.SearchType.OneWay:
                            availability.Reservation.SearchType = "OW";
                            break;
                        case United.Services.FlightShopping.Common.SearchType.RoundTrip:
                            availability.Reservation.SearchType = "RT";
                            break;
                        case United.Services.FlightShopping.Common.SearchType.MultipleDestination:
                            availability.Reservation.SearchType = "MD";
                            break;
                        default:
                            availability.Reservation.SearchType = string.Empty;
                            break;
                    }
                    if (_shoppingUtility.IsEnableTaxForAgeDiversification(session.IsReshopChange, request.Application.Id, request.Application.Version.Major))
                    {
                        availability.Reservation.Prices = _shoppingUtility.GetPrices(response.DisplayCart.DisplayPrices, availability.AwardTravel, session.SessionId, session.IsReshopChange, availability.Reservation.SearchType, session: session);
                    }
                    else
                    {
                        availability.Reservation.Prices = _unfinishedBooking.GetPrices(response.DisplayCart.DisplayPrices);
                    }
                    bool Is24HoursWindow = false;

                    if (_configuration.GetValue<bool>("EnableForceEPlus"))
                    {
                        if (availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT != null)
                        {
                            Is24HoursWindow = TopHelper.Is24HourWindow(Convert.ToDateTime(availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT));
                        }
                    }

                    availability.Reservation.ShopReservationInfo2.IsForceSeatMap = ConfigUtility.IsForceSeatMapforEPlus(false, response.DisplayCart.IsElf, Is24HoursWindow, request.Application.Id, request.Application.Version.Major);
                    bool isSupportedVersion = isApplicationVersionGreaterForBundles(request.Application.Id, request.Application.Version.Major, "AndroidBundleVersion", "IOSBundleVersion", "", "");
                    if (isSupportedVersion)
                    {
                        if (_configuration.GetValue<bool>("IsEnableBundlesForBasicEconomyBooking"))
                        {
                            availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles");
                        }
                        else
                        {
                            availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles") && !(response.DisplayCart.IsElf || ConfigUtility.IsIBEFullFare(response.DisplayCart));
                        }
                    }

                    #region 159514 - Added for inhibit booking message,177113 - 179536 BE Fare Inversion and stacking messages  

                    if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                    {
                        if (UtilityHelper.IdentifyInhibitWarning(response))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                            if (!availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.INHIBITBOOKING.ToString()))
                            {

                                _logger.LogWarning("GetShopPinDownDetails - Response with Inhibit warning {@Response}", JsonConvert.SerializeObject(response));


                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                                if (!_configuration.GetValue<bool>("TurnOffBookingCutoffMinsFromCSL"))
                                {
                                    string bookingCutOffminsFromCSL = (response?.DisplayCart?.BookingCutOffMinutes > 0) ? response.DisplayCart.BookingCutOffMinutes.ToString() : string.Empty;

                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(ConfigUtility.GetInhibitMessage(bookingCutOffminsFromCSL));
                                    availability.Reservation.ShopReservationInfo2.BookingCutOffMinutes = bookingCutOffminsFromCSL;

                                }
                                else
                                {
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(ConfigUtility.GetInhibitMessage(string.Empty));
                                }

                                if (ConfigUtility.EnableBoeingDisclaimer())
                                {
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                                }
                            }
                        }
                    }

                    if (ConfigUtility.EnableBoeingDisclaimer() && ConfigUtility.IsBoeingDisclaimer(response.DisplayCart.DisplayTrips))
                    {
                        if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                        if (!availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.BOEING737WARNING.ToString()))
                        {

                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();
                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(ConfigUtility.GetBoeingDisclaimer());

                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                        }
                    }

                    ///202150 - Getting both messages for fare inversions trying to select mixed itinerary (Reported by Andrew)
                    ///Srini - 12/26/2017
                    ///This If condition, we can remove, when we take "BugFixToggleFor17M" toggle out and directly "response.DisplayCart.IsUpgradedFromEntryLevelFare" check to next if condition
                    if (!_configuration.GetValue<bool>("BugFixToggleFor17M") || (_configuration.GetValue<bool>("BugFixToggleFor17M") && response.DisplayCart.IsUpgradedFromEntryLevelFare))
                    {
                        if (_configuration.GetValue<bool>("EnableBEFareInversion"))
                        {
                            if (UtilityHelper.IdentifyBEFareInversion(response))
                            {
                                if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                                if (_shoppingUtility.IsNonRefundableNonChangable(response.DisplayCart.ProductCodeBeforeUpgrade))
                                {
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(await GetNonRefundableNonChangableInversionMessage(request, session));
                                }
                                else
                                {
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(ConfigUtility.GetBEMessage());
                                }
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                            }
                        }
                    }

                    #endregion

                    await SetELFUpgradeMsg(availability, response?.DisplayCart?.ProductCodeBeforeUpgrade, request, session);

                    if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Any())
                    {
                        availability.Reservation.Prices.AddRange(_unfinishedBooking.GetPrices(response.DisplayCart.DisplayFees));
                    }

                    availability.Reservation.Prices = _traveler.AdjustTotal(availability.Reservation.Prices);

                    if (ConfigUtility.IsEnableTaxForAgeDiversification(session.IsReshopChange, request.Application.Id, request.Application.Version.Major))
                    {
                        availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();
                        availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = ConfigUtility.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices);
                        if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                        {
                            availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes.Add(UtilityHelper.AddFeesAfterPriceChange(response.DisplayCart.DisplayFees));
                        }
                    }
                    else
                    {
                        availability.Reservation.Taxes = _unfinishedBooking.GetTaxAndFees(response.DisplayCart.DisplayPrices, response.Reservation.NumberInParty);

                        if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                        {
                            //combine fees into taxes so that totals are correct
                            List<DisplayPrice> tempList = new List<DisplayPrice>();
                            tempList.AddRange(response.DisplayCart.DisplayPrices);
                            tempList.AddRange(response.DisplayCart.DisplayFees);
                            availability.Reservation.Taxes = _unfinishedBooking.GetTaxAndFees(tempList, response.Reservation.NumberInParty);
                        }
                    }
                    availability.Reservation.TravelOptions = _travelerUtility.GetTravelOptions(response.DisplayCart, session.IsReshopChange, request.Application.Id, request.Application.Version.Major);

                    availability.Reservation.Prices = _shoppingUtility.UpdatePricesForEFS(availability.Reservation, request.Application.Id, request.Application.Version.Major, false);

                    if (isOmniCartHomeScreenChanges)
                    {
                        if (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, session.IsReshopChange))
                        {
                            availability.TravelerCount = _omniCart.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, true) && response?.Reservation?.Travelers != null ? response.Reservation.Travelers.Count() : response.DisplayCart.DisplayTravelers.Sum(t => t.TravelerCount);
                            availability.Reservation.NumberOfTravelers = availability.TravelerCount;
                            availability.Reservation.ShopReservationInfo2.TravelerTypes = _omniCart.GetMOBTravelerTypes(response.DisplayCart.DisplayTravelers);
                            availability.Reservation.ShopReservationInfo2.displayTravelTypes = UtilityHelper.GetDisplayTravelerTypes(response.DisplayCart.DisplayTravelers);
                        }
                    }
                    else
                    {
                        if (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, session.IsReshopChange) && request.SelectedUnfinishBooking.TravelerTypes != null && request.SelectedUnfinishBooking.TravelerTypes.Count > 0)
                        {
                            availability.TravelerCount = request.SelectedUnfinishBooking.TravelerTypes.Where(t => t.TravelerType != null && t.Count > 0).Select(t => t.Count).Sum();
                            availability.Reservation.NumberOfTravelers = availability.TravelerCount;

                            availability.Reservation.ShopReservationInfo2.TravelerTypes = request.SelectedUnfinishBooking.TravelerTypes;

                            availability.Reservation.ShopReservationInfo2.displayTravelTypes = UtilityHelper.GetDisplayTravelerTypes(response.DisplayCart.DisplayTravelers);
                        }
                    }

                    if (string.IsNullOrEmpty(session.EmployeeId))
                    {                                               
                        availability.Reservation.FareLock = GetFareLockOptions(response.FareLockResponse, response.DisplayCart.DisplayPrices, request.SelectedUnfinishBooking?.CatalogItems, request.Application.Id, request.Application.Version.Major);                        
                    }

                    //availability.Reservation.LMXFlights = PopulateLMX(response.CartId, selectTripRequest.SessionId, selectTripRequest.Application.Id, selectTripRequest.DeviceId, selectTripRequest.Application.Version.Major, shop.Request.ShowMileageDetails, response.DisplayCart.DisplayTrips);

                    availability.Reservation.IneligibleToEarnCreditMessage = _configuration.GetValue<string>("IneligibleToEarnCreditMessage");
                    availability.Reservation.OaIneligibleToEarnCreditMessage = _configuration.GetValue<string>("OaIneligibleToEarnCreditMessage");

                    availability.Reservation.IsCubaTravel = IsCubaTravelTrip(availability.Reservation);
                    if (availability.Reservation.IsCubaTravel)
                    {
                        MobileCMSContentRequest mobMobileCMSContentRequest = new MobileCMSContentRequest();
                        mobMobileCMSContentRequest.Application = request.Application;
                        mobMobileCMSContentRequest.Token = session.Token;
                        availability.Reservation.CubaTravelInfo = await _travelerCSL.GetCubaTravelResons(mobMobileCMSContentRequest);

                        availability.Reservation.CubaTravelInfo.CubaTravelTitles = await GetMPPINPWDTitleMessages(new List<string> { "CUBA_TRAVEL_CONTENT" });
                    }

                    if (ConfigUtility.IsEnabledNationalityAndResidence(false, request.Application.Id, request.Application.Version.Major))
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
                }
                if (await _featureToggles.IsEnableCustomerFacingCartId(request.Application.Id, request.Application.Version.Major, session.CatalogItems).ConfigureAwait(false) && response?.CartRefId > 0)
                {
                    try
                    {
                        _shoppingUtility.GetCartRefId(response.CartRefId, availability?.Reservation?.ShopReservationInfo2, lstMessages);
                    }
                    catch { }
                }
                availability.Reservation.ISFlexibleSegmentExist = _unfinishedBooking.IsOneFlexibleSegmentExist(availability.Reservation.Trips);
                availability.Reservation.FlightShareMessage = _unfinishedBooking.GetFlightShareMessage(availability.Reservation, string.Empty);
                availability.Reservation.IsRefundable = mobResRev.IsRefundable;
                availability.Reservation.ISInternational = mobResRev.ISInternational;

                //**RSA Publick Key Implmentaion**//

                //string pkDispenserPublicKey = FilePersist.Load<string>(Utility.GetCSSPublicKeyPersistSessionStaticGUID(request.Application.Id), "pkDispenserPublicKey");
               
                availability.Reservation.PKDispenserPublicKey = await _pKDispenserPublicKey.GetCachedOrNewpkDispenserPublicKey(request.Application.Id, request.Application.Version.Major, request.DeviceId, session.SessionId, session.Token, session.CatalogItems);

                //**RSA Publick Key Implmentaion**//

                //#region 214448 - Unaccompanied Minor Age
                availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo = new ReservationAgeBoundInfo();
                availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.MinimumAge = Convert.ToInt32(_configuration.GetValue<string>("umnrMinimumAge"));
                availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.UpBoundAge = Convert.ToInt32(_configuration.GetValue<string>("umnrUpBoundAge"));
                availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.ErrorMessage = _configuration.GetValue<string>("umnrErrorMessage");
                //#endregion
                availability.Reservation.CheckedbagChargebutton = _configuration.GetValue<string>("ViewCheckedBagChargesButton");

                #region Special Needs

                if (_shoppingUtility.EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
                {
                    try
                    {
                        SelectTripRequest selectTripRequest = null;
                        if (_configuration.GetValue<bool>("EnableServiceAnimalEnhancements"))
                        {
                            selectTripRequest = new SelectTripRequest { CatalogItems = request.CatalogItems != null ? request.CatalogItems : request.SelectedUnfinishBooking?.CatalogItems, Application = request.Application, DeviceId = request.DeviceId };
                        }
                        // populate avail. special needs for the itinerary
                        availability.Reservation.ShopReservationInfo2.SpecialNeeds = await _unfinishedBooking.GetItineraryAvailableSpecialNeeds(session, request.Application.Id, request.Application.Version.Major, request.DeviceId, response.Reservation.FlightSegments, "en-US", availability.Reservation, selectRequest: selectTripRequest);

                        // update persisted reservation object too
                        var bookingPathReservation = new Model.Shopping.Reservation();
                        bookingPathReservation = await _sessionHelperService.GetSession<Model.Shopping.Reservation>(session.SessionId, bookingPathReservation.ObjectName, new List<string> { session.SessionId, bookingPathReservation.ObjectName });

                        //bookingPathReservation = FilePersist.Load<Reservation>(session.SessionId, bookingPathReservation.ObjectName);

                        if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null)
                        {
                            bookingPathReservation.ShopReservationInfo2.SpecialNeeds = availability.Reservation.ShopReservationInfo2.SpecialNeeds;
                            //FilePersist.Save(bookingPathReservation.SessionId, bookingPathReservation.ObjectName, bookingPathReservation);
                            await _sessionHelperService.SaveSession(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);

                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("GetShopPinDownDetailsV2 - GetItineraryAvailableSpecialNeeds {@Exception}", JsonConvert.SerializeObject(e));
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
                            var message = !string.IsNullOrEmpty(sdlKeyForWheelchairSizerContent) ? await _unfinishedBooking.GetCMSContentMessageByKey(sdlKeyForWheelchairSizerContent, request, session).ConfigureAwait(false) : null;
                            if (message != null)
                            {
                                availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo.WcHeaderMsg = message?.HeadLine;
                                availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo.WcBodyMsg = message?.ContentFull;
                            }
                            _shoppingUtility.BuildWheelChairSizerOAMsgs(availability.Reservation);
                            var bookingPathReservation = new Model.Shopping.Reservation();
                            bookingPathReservation = await _sessionHelperService.GetSession<Model.Shopping.Reservation>(session.SessionId, bookingPathReservation.ObjectName, new List<string> { session.SessionId, bookingPathReservation.ObjectName });
                            if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null)
                            {
                                bookingPathReservation.ShopReservationInfo2.WheelChairSizerInfo = availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo;
                                await _sessionHelperService.SaveSession(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger1.LogError("GetShopPinDownDetailsV2 - WheelChairSizerContent {@message} {@stackTrace}", ex.Message, ex.StackTrace);
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
                        _logger.ILoggerError("GetShopPinDownDetailsV2 - TaxID Collection {@message} {@stackTrace}", ex.Message, ex.StackTrace);
                    }
                }
                #endregion

                if (_shoppingUtility.EnableCovidTestFlightShopping(request.Application.Id, request.Application.Version.Major, session.IsReshopChange))
                {
                    ShopStaticUtility.AssignCovidTestIndicator(availability.Reservation);
                }
                if (_shoppingUtility.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, true) && !session.IsReshopChange)
                {
                    if (availability.Reservation.ShopReservationInfo2 == null)
                        availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();
                    availability.Reservation.ShopReservationInfo2.IsDisplayCart = _shoppingUtility.IsDisplayCart(session, GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, "Android_EnableAwardLiveCart_AppVersion", "iPhone_EnableAwardLiveCart_AppVersion")
                                                                                                                        ? "DisplayCartTravelTypesWithAward"
                                                                                                                        : "DisplayCartTravelTypes");
                }

                var persistedShopBookingDetailsResponse = new ShopBookingDetailsResponse
                {
                    SessionId = session.SessionId,
                    Reservation = response.Reservation,
                    CartId = response.CartId
                };

                if (string.IsNullOrEmpty(session.EmployeeId))
                {
                    persistedShopBookingDetailsResponse.FareLock = response.FareLockResponse;
                }
                //FilePersist.Save(persistedShopBookingDetailsResponse.SessionId, persistedShopBookingDetailsResponse.ObjectName, persistedShopBookingDetailsResponse);
                await _sessionHelperService.SaveSession(persistedShopBookingDetailsResponse, persistedShopBookingDetailsResponse.SessionId, new List<string> { persistedShopBookingDetailsResponse.SessionId, persistedShopBookingDetailsResponse.ObjectName }, persistedShopBookingDetailsResponse.ObjectName).ConfigureAwait(false);

                var shop = new ShoppingResponse
                {
                    SessionId = session.SessionId,
                    CartId = response.CartId,
                    Request = new MOBSHOPShopRequest
                    {
                        AccessCode = _configuration.GetValue<string>("AccessCode - CSLShopping"),
                        CountryCode = availability.Reservation.PointOfSale,
                        NumberOfAdults = availability.Reservation.NumberOfTravelers,
                        TravelerTypes = availability.Reservation.ShopReservationInfo2.TravelerTypes,
                        PremierStatusLevel = request.PremierStatusLevel,
                        MileagePlusAccountNumber = request.MileagePlusAccountNumber,
                        SearchType = availability?.Reservation?.SearchType
                    }
                };

                //FilePersist.Save(session.SessionId, shop.ObjectName, shop);
                await _sessionHelperService.SaveSession(shop, session.SessionId, new List<string> { session.SessionId, shop.ObjectName }, shop.ObjectName).ConfigureAwait(false);

                var selectTrip = new SelectTrip
                {
                    SessionId = session.SessionId,
                    CartId = response.CartId,
                    LastSelectTripKey = "0",
                    Responses = new SerializableDictionary<string, SelectTripResponse>()
                };
                var selectTripResponse = new SelectTripResponse
                {
                    Availability = new MOBSHOPAvailability { Reservation = availability.Reservation }
                };
                selectTrip.Responses.Add(selectTrip.LastSelectTripKey, selectTripResponse);
                //FilePersist.Save(session.SessionId, selectTrip.ObjectName, selectTrip);
                await _sessionHelperService.SaveSession(selectTrip, session.SessionId, new List<string> { session.SessionId, selectTrip.ObjectName }, selectTrip.ObjectName).ConfigureAwait(false);

                bool isCFOPVersionCheck = GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("AndriodCFOP_Booking_Reshop_PostbookingAppVersion"), _configuration.GetValue<string>("IphoneCFOP_Booking_Reshop_PostbookingAppVersion"));
                if (isCFOPVersionCheck)
                {
                    if (!string.IsNullOrEmpty(_configuration.GetValue<string>("IsBookingCommonFOPEnabled")) && session.IsReshopChange == false)
                    {
                        availability.Reservation.IsBookingCommonFOPEnabled = Convert.ToBoolean(_configuration.GetValue<string>("IsBookingCommonFOPEnabled"));

                    }
                }
                var reservation = ReservationToPersistReservation(availability);
                reservation.CSLReservationJSONFormat = JsonConvert.SerializeObject(CommonMethods.FlattenSegmentsForSeatDisplay(response.Reservation));
                reservation.FOPOptions = _shoppingUtility.GetFopOptions(request.Application.Id, request.Application.Version.Major);//FOP Options Fix Venkat 12/08

                //FilePersist.Save(session.SessionId, reservation.ObjectName, reservation);
                await _sessionHelperService.SaveSession(reservation, session.SessionId, new List<string> { session.SessionId, reservation.ObjectName }, reservation.ObjectName).ConfigureAwait(false);

                if (ConfigUtility.EnableInflightContactlessPayment(request.Application.Id, request.Application.Version.Major, false))
                {
                    await FireForGetInFlightCLEligibilityCheck(reservation, request, session);
                }

                    await SetAvailabilityELFProperties(availability, availability.Reservation.NumberOfTravelers > 1, _shoppingUtility.EnableSSA(request.Application.Id, request.Application.Version.Major));
                if (_configuration.GetValue<bool>("EnableSessionForceSavePersistInCloud"))
                {
                    //TODO: complete migration (gopi)
                    //FilePersist.ForceSaveToCloud<United.Persist.Definition.Shopping.Session>(session.SessionId, session.ObjectName, session);
                    await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);

                }
            }

            return availability;
        }
        private async System.Threading.Tasks.Task SetAvailabilityELFProperties(MOBSHOPAvailability availability, bool isMultiTravelers, bool isSSA)
        {
            if (availability != null)
            {
                availability.ELFShopMessages = await SetElfShopMessage(isMultiTravelers, isSSA);
                availability.ELFShopOptions = await ParseELFShopOptions(isSSA);
            }
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

        
        public virtual async Task<List<MOBItem>> SetElfShopMessage(bool isMultiTravelers, bool isSSA)
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
        public async Task<List<MOBItem>> GetMPPINPWDTitleMessages(List<string> titleList)
        {
            List<MOBItem> messages = new List<MOBItem>();
            List<United.Definition.MOBLegalDocument> docs;
            if (titleList.Count == 1)
            {
                docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(titleList[0], _headers.ContextValues.TransactionId, true);
            }
            else
            {
                docs = await _documentLibraryDynamoDB.GetNewLegalDocumentsForTitlesData(titleList, true);
            }
            //GetNewLegalDocumentsForTitles(titleList, true);
            if (!_configuration.GetValue<bool>("DisableCubaTravelContentOrderMismatchFix") && titleList.Count == 1 && !string.IsNullOrEmpty(titleList[0]) && titleList[0] == "CUBA_TRAVEL_CONTENT" && docs != null && docs.Count > 0)
            {
                _shoppingUtility.AddMobLegalDocumentItem(docs, messages, "CubaPage1Title");

                _shoppingUtility.AddMobLegalDocumentItem(docs, messages, "CubaPage1Description");

                _shoppingUtility.AddMobLegalDocumentItem(docs, messages, "CubaPage1ReasonListButton");
            }
            else if (docs != null && docs.Count > 0)
            {
                foreach (MOBLegalDocument doc in docs)
                {
                    MOBItem item = new MOBItem();
                    item.Id = doc.Title;
                    item.CurrentValue = doc.LegalDocument;
                    messages.Add(item);
                }
            }
            return messages;
        }
        private async Task<List<MOBItem>> GetELFShopMessages(string elfDocumentLibraryTableKey)
        {
            List<MOBItem> elfShopMessageList = await GetMPPINPWDTitleMessages(new List<string>() { elfDocumentLibraryTableKey });
            return elfShopMessageList;
        }

        private System.Collections.ObjectModel.Collection<United.Service.Presentation.ProductModel.ProductSegment> GetInflightPurchaseEligibility(Model.Shopping.Reservation reservation, MOBRequest request, Session session)
        {
            United.Service.Presentation.ProductResponseModel.ProductEligibilityResponse eligibilityResponse = null;

            //United.Logger.Database.SqlServerLoggerProvider logger = new United.Logger.Database.SqlServerLoggerProvider();
            try
            {
                //string url = $"{_configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLMerchandizingservice")}/GetProductEligibility";

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
                eligibilityRequest.FlightSegments = new Collection<United.Service.Presentation.ProductModel.ProductSegment>();


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


                #region //**** Get Call Duration Code *******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion //**** Get Call Duration Code *******

                eligibilityResponse = (_purchaseMerchandizingService.GetInflightPurchaseEligibility<United.Service.Presentation.ProductResponseModel.ProductEligibilityResponse>
                    (session.Token, jsonRequest, session.SessionId).Result).response;
                //HttpHelper.Post(url, "application/json; charset=utf-8", session.Token, jsonRequest);

                #region // 2 = cslStopWatch //**** Get Call Duration Code *******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

                #endregion //**** Get Call Duration Code *******   


                if (eligibilityResponse == null)
                {
                    // logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "GetInflightPurchaseEligibility", "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, "CSL response is empty or null"));
                    _logger.LogError("GetInflightPurchaseEligibility Exception with (CSL response is empty or null)");

                    return null;
                }
                //= JsonSerializer.Deserialize<United.Service.Presentation.ProductResponseModel.ProductEligibilityResponse>(jsonResponse);


                if (eligibilityResponse?.FlightSegments?.Count == 0)
                {
                    //logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "GetInflightPurchaseEligibility", "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, "Failed to deserialize CSL response"));
                    _logger.LogError("GetInflightPurchaseEligibility Exception with (Failed to deserialize CSL response)");

                    return null;
                }

                if (eligibilityResponse.Response.Error?.Count > 0)
                {
                    string errorMsg = String.Join(", ", eligibilityResponse.Response.Error.Select(x => x.Text));
                    _logger.LogError("GetInflightPurchaseEligibility UnitedException {@ErrorMsg}", errorMsg);
                    //logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "GetInflightPurchaseEligibility", "UnitedException", request.Application.Id, request.Application.Version.Major, request.DeviceId, errorMsg));
                    return null;
                }

                return eligibilityResponse.FlightSegments;
            }
            catch (System.Net.WebException wex)
            {
                if (wex.Response != null)
                {
                    var errorResponse = new StreamReader(wex.Response.GetResponseStream()).ReadToEnd();

                    _logger.LogError("GetInflightPurchaseEligibility - Exception {@Message}", wex.Message);

                    //    if (levelSwitch.TraceInfo)
                    //        LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(session.SessionId, "GetInflightPurchaseEligibility", "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, errorResponse));
                }
            }
            catch (Exception ex)
            {
                //logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "GetInflightPurchaseEligibility", "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, ex.Message));
                _logger.LogError("GetInflightPurchaseEligibility Exception {@Message}", JsonConvert.SerializeObject(ex));
            }
            return null;
        }

        private async Task<MOBSHOPInflightContactlessPaymentEligibility> IsEligibleInflightContactlessPayment(Model.Shopping.Reservation reservation, MOBRequest request, Session session)
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

                await _sessionHelperService.SaveSession<MOBSHOPInflightContactlessPaymentEligibility>(eligibility, session.SessionId, new List<string> { session.SessionId, eligibility.ObjectName }, eligibility.ObjectName);
            }
            catch { }
            return eligibility;
        }
        private async System.Threading.Tasks.Task FireForGetInFlightCLEligibilityCheck(Model.Shopping.Reservation reservation, MOBRequest request, Session session)
        {
            if (!reservation.IsReshopChange)
            {
                await IsEligibleInflightContactlessPayment(reservation, request, session);
            }
        }
        private Model.Shopping.Reservation ReservationToPersistReservation(MOBSHOPAvailability availability)
        {
            var reservation = new Model.Shopping.Reservation
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
                CubaTravelInfo = availability.Reservation.CubaTravelInfo
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
        private bool isGetAirportListInOneCallToggleOn()
        {
            return Convert.ToBoolean(_configuration.GetValue<string>("GetAirportNameInOneCallToggle") ?? "False");
        }

        private async Task<AirportDetailsList> GetAllAiportsList(List<DisplayTrip> trips)
        {
            string airPortCodes = GetAllAirportCodesWithCommaDelimatedFromCSLTrips(trips);
            return await GetAirportNamesListCollection(airPortCodes);
        }

        private async Task<AirportDetailsList> GetAirportNamesListCollection(string airPortCodes)
        {
            AirportDetailsList retVal = null;
            if (airPortCodes != string.Empty)
            {
                airPortCodes = "'" + airPortCodes + "'";
                airPortCodes = String.Join(",", airPortCodes.Split(',').Distinct());
                airPortCodes = Regex.Replace(airPortCodes, ",", "','");
                retVal = new AirportDetailsList();
                retVal.AirportsList = await _shoppingUtility.GetAirportNamesList(airPortCodes);
            }
            return retVal;
        }

        private string GetAllAirportCodesWithCommaDelimatedFromCSLTrips(List<DisplayTrip> trips)
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
        private string GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(List<United.Services.FlightShopping.Common.Flight> flights)
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
                        if (airportDesc.IsNullOrEmpty())
                        {
                            airportDesc = airPort.AirportInfo;
                        }
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
                    airportDesc = await  _shoppingUtility.GetAirportName(airportCode);
                }
            }
            catch (Exception ex)
            {
                airportDesc = await  _shoppingUtility.GetAirportName(airportCode);
            }
            return airportDesc;
        }

        private UpdateAmenitiesIndicatorsRequest GetAmenitiesRequest(string cartId, List<United.Services.FlightShopping.Common.Flight> flights)
        {
            UpdateAmenitiesIndicatorsRequest request = new UpdateAmenitiesIndicatorsRequest();

            request.CartId = cartId;
            request.CollectionType = UpdateAmenitiesIndicatorsCollectionType.FlightNumbers;
            request.FlightNumbers = new Collection<string>();

            if (flights != null)
            {
                try
                {
                    foreach (United.Services.FlightShopping.Common.Flight flight in flights)
                    {
                        if (!request.FlightNumbers.Contains(flight.FlightNumber))
                        {
                            request.FlightNumbers.Add(flight.FlightNumber);
                        }
                        if (flight.Connections != null && flight.Connections.Count > 0)
                        {
                            foreach (United.Services.FlightShopping.Common.Flight connection in flight.Connections)
                            {
                                if (!request.FlightNumbers.Contains(connection.FlightNumber))
                                {
                                    request.FlightNumbers.Add(connection.FlightNumber);
                                }
                            }
                        }
                        if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                        {
                            foreach (United.Services.FlightShopping.Common.Flight stop in flight.StopInfos)
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

        private async Task<UpdateAmenitiesIndicatorsResponse> GetAmenitiesForFlights(string sessionId, string cartId, List<United.Services.FlightShopping.Common.Flight> flights, int appId, string deviceId, string appVersion, bool isClientCall = false, UpdateAmenitiesIndicatorsRequest amenitiesPersistRequest = null)
        {
            UpdateAmenitiesIndicatorsRequest amenitiesRequest = new UpdateAmenitiesIndicatorsRequest();
            UpdateAmenitiesIndicatorsResponse response = null;
            if (isClientCall)
            {
                amenitiesRequest = amenitiesPersistRequest;
            }
            else
            {
                amenitiesRequest = GetAmenitiesRequest(cartId, flights);
            }

            string jsonRequest = JsonConvert.SerializeObject(amenitiesRequest);

            string url = "";// string.Format("{0}/UpdateAmenitiesIndicators", ConfigurationManager.AppSettings["ServiceEndPointBaseUrl - CSLShopping"]);

            Session session = new Session();
            //session = Persist.FilePersist.Load<Persist.Definition.Shopping.Session>(sessionId, session.ObjectName);
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string>() { sessionId, session.ObjectName }).ConfigureAwait(false);

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

            response = (_flightShoppingService.GetAmenitiesForFlights<UpdateAmenitiesIndicatorsResponse>(session.Token, sessionId, jsonRequest).Result).response;
            //HttpHelper.Post(url, "application/json; charset=utf-8", session.Token, jsonRequest, httpPostTimeOut, httpPostNumberOfRetry);
            #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cslStopWatch.IsRunning)
            {
                cslStopWatch.Stop();
            }
            string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

            #endregion


            if (response != null)
            {
                //response = JsonSerializer.DeserializeUseContract<UpdateAmenitiesIndicatorsResponse>(jsonResponse);

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

                    }
                    else
                    {
                        //if (traceSwitch.TraceError)
                        //{
                        //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetAmenitiesForFlight - Response for GetAmenitiesForFlight", "Error", jsonResponse));
                        //}
                    }
                    _logger.LogError("GetAmenitiesForFlight GetAmenitiesForFlight - Response for GetAmenitiesForFlight - Response {@Response}", JsonConvert.SerializeObject(response));

                }
            }
            else
            {
                throw new MOBUnitedException("Failed to retrieve booking details.");
            }

            return response;
        }
        private async Task<List<United.Services.FlightShopping.Common.LMX.LmxFlight>> GetLmxFlights(string token, string cartId, string hashList, string sessionId, int applicationId, string appVersion, string deviceId)
        {
            List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights = null;

            if (!string.IsNullOrEmpty(cartId))
            {
                //string url = _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLBookingProducts");
                string jsonRequest = "{\"CartId\":\"" + cartId + "\"}";



                if (!string.IsNullOrEmpty(hashList))
                {
                    jsonRequest = "{\"CartId\":\"" + cartId + "\", \"hashList\":[" + hashList + "]}";
                }
                LmxQuoteResponse lmxQuoteResponse = new LmxQuoteResponse();

                try
                {
                    lmxQuoteResponse = await _lmxInfo.GetLmxFlight<LmxQuoteResponse>(token, jsonRequest, _headers.ContextValues.SessionId);

                    //HttpHelper.Post(url, "application/json; charset=utf-8", token, jsonRequest, httpPostTimeOut, httpPostNumberOfRetry);

                }
                catch (System.Exception) { }

                if (lmxQuoteResponse != null)
                {
                    //LmxQuoteResponse lmxQuoteResponse = JsonSerializer.NewtonSoftDeserialize<LmxQuoteResponse>(jsonResponse);


                    if (lmxQuoteResponse != null && lmxQuoteResponse.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success))
                    {
                        if (lmxQuoteResponse.Flights != null && lmxQuoteResponse.Flights.Count > 0)
                        {
                            lmxFlights = lmxQuoteResponse.Flights;
                        }
                    }
                }
            }

            return lmxFlights;
        }

        private async Task<List<MOBSHOPTrip>> PopulateMetaTrips(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string cartId, ShoppingResponse persistShop, string sessionId, int appId, string deviceId, string appVersion, bool showMileageDetails, List<DisplayTrip> flightSegmentCollection, string fareClass, List<string> flightDepartDatesForSelectedTrip)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            Session session = new Session();
            //session = Persist.FilePersist.Load<Persist.Definition.Shopping.Session>(sessionId, session.ObjectName);
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string>() { sessionId, session.ObjectName }).ConfigureAwait(false);

            supressLMX = session.SupressLMXForAppID;
            #endregion
            List<MOBSHOPTrip> trips = new List<MOBSHOPTrip>();          
            try
            {
                //var airportsList = FilePersist.Load<AirportDetailsList>(sessionId, (new AirportDetailsList()).GetType().FullName);
                if (_configuration.GetValue<bool>("EnableFecthAirportNameFromCSL"))
                {
                    airportsList = null;
                }
                else
                {
                    airportsList = await _sessionHelperService.GetSession<AirportDetailsList>(sessionId, (new AirportDetailsList()).GetType().FullName, new List<string>() { sessionId, (new AirportDetailsList()).GetType().FullName }).ConfigureAwait(false);
                }
                if (isGetAirportListInOneCallToggleOn() && (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0))
                {
                    airportsList = await GetAllAiportsList(flightSegmentCollection);
                }
            }
            catch (Exception ex)
            {
                //logEntries.Add(LogEntry.GetLogEntry<Exception>(sessionId, "GetAllAiportsList", "PopulateMetaTrips-GetAllAiportsList", appId, appVersion, deviceId, ex, true, true));
                //logEntries.Add(LogEntry.GetLogEntry<List<United.Services.FlightShopping.Common.DisplayCart.DisplayTrip>>(sessionId, "GetAllAiportsList", "PopulateMetaTrips-DisplayTrip", appId, appVersion, deviceId, flightSegmentCollection, true, true));
                _logger.LogError("GetSDLContentByGroupName - Exception {error} , SessionId {sessionId} and PopulateMetaTrips-DisplayTrip {flightSegmentCollection}", ex, sessionId, flightSegmentCollection);
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

                    CultureInfo ci = null;

                    List<MOBSHOPFlight> flights = null;
                    if (flightSegmentCollection[i].Flights != null && flightSegmentCollection[i].Flights.Count > 0)
                    {
                        //update amenities for all flights
                        UpdateAmenitiesIndicatorsResponse amenitiesResponse = new UpdateAmenitiesIndicatorsResponse();
                        List<United.Services.FlightShopping.Common.Flight> tempFlights = new List<United.Services.FlightShopping.Common.Flight>(flightSegmentCollection[i].Flights);

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

                        flightSegmentCollection[i].Flights = new List<United.Services.FlightShopping.Common.Flight>(tempFlights);
                        if (_mOBSHOPDataCarrier == null)
                            _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                        var tupleRes = await GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, flightSegmentCollection[i].Flights, string.Empty, ci, 0.0M, trip.Columns, 0, fareClass, false, false, true, null, appVersion, appId);
                        ci = tupleRes.ci;
                        flights = tupleRes.mOBSHOPFlights;
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

                        trip.FlattenedFlights = new List<MOBSHOPFlattenedFlight>();
                        foreach (var flight in flights)
                        {
                            MOBSHOPFlattenedFlight flattenedFlight = new MOBSHOPFlattenedFlight();
                            //need to overwrite trip id otherwise it's the previous trip's id
                            trip.TripId = flight.TripId;
                            flattenedFlight.TripId = flight.TripId;
                            flattenedFlight.FlightId = flight.FlightId;
                            flattenedFlight.ProductId = flight.ProductId;
                            flattenedFlight.Flights = new List<MOBSHOPFlight>();
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

                            flight.FlightArrivalDays = GeneralHelper.GetDayDifference(tripDepartDate, flight.ArrivalDateTime);
                            bool flightDateChanged = false;
                            flight.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDepartDate, flight.ArrivalDateTime, ref flightDateChanged);
                            flight.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDepartDate, flight.DepartureDateTime, ref flightDateChanged);
                            flight.FlightDateChanged = flightDateChanged;

                            #endregion


                            flattenedFlight.Flights.Add(flight);

                            if (flight.Connections != null && flight.Connections.Count > 0)
                            {
                                // Make a copy of flight.Connections and release flight.Connections
                                List<MOBSHOPFlight> connections = flight.Connections.Clone();
                                flight.Connections = null;

                                foreach (var connection in connections)
                                {
                                    connection.TripId = trip.TripId;

                                    connection.DepartureDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(connection.DepartureDateTime, connection.OriginTimezoneOffset) : await GetGMTTime(connection.DepartureDateTime, connection.Origin, sessionId);
                                    connection.ArrivalDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(connection.ArrivalDateTime, connection.DestinationTimezoneOffset) : await GetGMTTime(connection.ArrivalDateTime, connection.Destination, sessionId);

                                    #region Red Eye Flight Changes

                                    connection.FlightArrivalDays = GeneralHelper.GetDayDifference(tripDepartDate, connection.ArrivalDateTime);
                                    connection.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDepartDate, connection.ArrivalDateTime, ref flightDateChanged);
                                    connection.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDepartDate, connection.DepartureDateTime, ref flightDateChanged);
                                    connection.FlightDateChanged = flightDateChanged;

                                    #endregion

                                    flattenedFlight.Flights.Add(connection);
                                    if (connection.StopInfos != null && connection.StopInfos.Count > 0)
                                    {
                                        // Make a copy of flight.Connections and release flight.Connections
                                        List<MOBSHOPFlight> connStops = connection.StopInfos.Clone();
                                        connection.StopInfos = null;

                                        foreach (var conn in connStops)
                                        {
                                            conn.TripId = trip.TripId;
                                            conn.IsStopOver = true;

                                            conn.DepartureDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(conn.DepartureDateTime, conn.OriginTimezoneOffset) : await GetGMTTime(conn.DepartureDateTime, conn.Origin, sessionId);
                                            conn.ArrivalDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(conn.ArrivalDateTime, conn.DestinationTimezoneOffset) : await GetGMTTime(conn.ArrivalDateTime, conn.Destination, sessionId);

                                            #region Red Eye Flight Changes

                                            conn.FlightArrivalDays = GeneralHelper.GetDayDifference(tripDepartDate, conn.ArrivalDateTime);
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
                                List<MOBSHOPFlight> connections = flight.StopInfos.Clone();
                                flight.StopInfos = null;

                                foreach (var connection in connections)
                                {
                                    connection.TripId = trip.TripId;
                                    connection.IsStopOver = true;

                                    connection.DepartureDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(connection.DepartureDateTime, connection.OriginTimezoneOffset) : await GetGMTTime(connection.DepartureDateTime, connection.Origin, sessionId);
                                    connection.ArrivalDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(connection.ArrivalDateTime, connection.DestinationTimezoneOffset) : await GetGMTTime(connection.ArrivalDateTime, connection.Destination, sessionId);
                                    #region Red Eye Flight Changes

                                    connection.FlightArrivalDays = GeneralHelper.GetDayDifference(tripDepartDate, connection.ArrivalDateTime);
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
                                foreach (MOBSHOPShoppingProduct prod in flight.ShoppingProducts)
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
                            if (_omniCart.IsEnableOmniCartMVP2Changes(appId, appVersion, true))
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
                                            flattenedFlight.MsgFlightCarrier = "Operated by " + flattenedFlight.MsgFlightCarrier.TrimEnd(',', ' ');
                                        }
                                    }
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
        //public string GetGMTTime(string localTime, string airportCode, string sessionId)
        //{

        //    string gmtTime = localTime;

        //    DateTime dateTime = new DateTime(0);
        //    if (DateTime.TryParse(localTime, out dateTime) && airportCode != null && airportCode.Trim().Length == 3)
        //    {

        //        long dateTime1 = 0L;
        //        long dateTime2 = 0L;
        //        long dateTime3 = 0L;
        //        try
        //        {
        //            var fitBitDynanmoDB = new FitbitDynamoDB(_configuration, _dynamoDBService);
        //            var gmtTimeValue = fitBitDynanmoDB.GetGMTTime<United.Mobile.Model.Internal.Common.GMTTime>(dateTime.Year, airportCode.Trim().ToUpper(), sessionId);

        //            //IDataReader dataReader = null;
        //            //using (dataReader = database.ExecuteReader(dbCommand))
        //            //{
        //            //    while (dataReader.Read())
        //            //    {
        //            //        dateTime1 = Convert.ToInt64(dataReader["DateTime_1"]);
        //            //        dateTime2 = Convert.ToInt64(dataReader["DateTime_2"]);
        //            //        dateTime3 = Convert.ToInt64(dataReader["DateTime_3"]);
        //            //    }
        //            //}

        //            long time = Convert.ToInt64(dateTime.Year.ToString() + dateTime.Month.ToString("00") + dateTime.Day.ToString("00") + dateTime.Hour.ToString("00") + dateTime.Minute.ToString("00"));
        //            bool dayLightSavingTime = false;
        //            if (time >= dateTime2 && time <= dateTime3)
        //            {
        //                dayLightSavingTime = true;
        //            }

        //            int offsetMunite = 0;
        //            //database = DatabaseFactory.CreateDatabase("ConnectionString - GMTConversion");
        //            //dbCommand = (DbCommand)database.GetStoredProcCommand("sp_GMT_City");

        //            //database.AddInParameter(dbCommand, "@StationCode", DbType.String, airportCode.Trim().ToUpper());
        //            //database.AddInParameter(dbCommand, "@Carrier", DbType.String, "CO");

        //            offsetMunite = fitBitDynanmoDB.GetGMTCity<int>(airportCode.Trim().ToUpper(), sessionId);

        //            //using (dataReader = database.ExecuteReader(dbCommand))
        //            //{
        //            //    while (dataReader.Read())
        //            //    {
        //            //        if (dayLightSavingTime)
        //            //        {
        //            //            offsetMunite = Convert.ToInt32(dataReader["DaySavTime"]);
        //            //        }
        //            //        else
        //            //        {
        //            //            offsetMunite = Convert.ToInt32(dataReader["StandardTime"]);
        //            //        }
        //            //    }
        //            //}

        //            dateTime = dateTime.AddMinutes(-offsetMunite);

        //            gmtTime = dateTime.ToString("MM/dd/yyyy hh:mm tt");

        //        }
        //        catch (System.Exception) { }
        //    }
        //    return gmtTime;
        //}
        private async Task<AirportDetailsList> GetAllAiportsList(List<United.Services.FlightShopping.Common.Flight> flights)
        {
            string airPortCodes = GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(flights);
            return await GetAirportNamesListCollection(airPortCodes);
        }

        private bool GetAddCollectWaiverStatus(United.Services.FlightShopping.Common.Flight flight, out string addcollectwaiver)
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

        private async Task<(List<MOBSHOPFlight> mOBSHOPFlights, CultureInfo ci)> GetFlights(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string sessionId, string cartId, List<United.Services.FlightShopping.Common.Flight> segments, string requestedCabin,  CultureInfo ci, decimal lowestFare, List<MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClass, bool updateAmenities = false, bool isConnection = false, bool isELFFareDisplayAtFSR = true, MOBSHOPTrip trip = null, string appVersion = "", int appID = 0, MOBAdditionalItems additionalItems = null, MOBCarbonEmissionsResponse carbonEmissionData = null)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            Session session = new Session();
            //session = Persist.FilePersist.Load<Persist.Definition.Shopping.Session>(sessionId, session.ObjectName);
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);

            supressLMX = session.SupressLMXForAppID;
            #endregion
            try
            {
                if (isGetAirportListInOneCallToggleOn() && (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0))
                {
                    //airportsList = FilePersist.Load<AirportDetailsList>(sessionId, (new AirportDetailsList()).GetType().FullName);
                    airportsList = await _sessionHelperService.GetSession<AirportDetailsList>(sessionId, (new AirportDetailsList()).GetType().FullName, new List<string> { sessionId, (new AirportDetailsList()).GetType().FullName }).ConfigureAwait(false);
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
            }

            List<MOBSHOPFlight> flights = null;
            ci = null;
            if (segments != null && segments.Count > 0)
            {
                flights = new List<MOBSHOPFlight>();

                foreach (United.Services.FlightShopping.Common.Flight segment in segments)
                {
                    #region

                    MOBSHOPFlight flight = new MOBSHOPFlight();
                    flight.Messages = new List<MOBSHOPMessage>();
                    string AddCollectProductID = string.Empty;
                    United.Services.FlightShopping.Common.Product displayProductForStopInfo = null;
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

                        United.Services.FlightShopping.Common.Product displayProduct = GetMatrixDisplayProduct(segment.Products, requestedCabin, columnInfo, out ci, out selected, out description, out mixedCabin, out seatsRemaining, fareClass, isConnection, isELFFareDisplayAtFSR);
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
                                    flight.MilesDisplayValue = formatAwardAmountForDisplay(flight.Airfare.ToString(), true);
                                }
                                else
                                {
                                    flight.AirfareDisplayValue = "N/A";
                                    flight.MilesDisplayValue = "N/A";
                                    if (_configuration.GetValue<string>("ReturnExceptionForAnyZeroDollarAirFareValue") != null && Convert.ToBoolean(_configuration.GetValue<bool>("ReturnExceptionForAnyZeroDollarAirFareValue").ToString()))
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
                                            flight.MilesDisplayValue = formatAwardAmountForDisplay(Convert.ToString(milesAmountToDisplay.Amount), true);
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

                                    if (_configuration.GetValue<string>("ReturnExceptionForAnyZeroDollarAirFareValue") != null && Convert.ToBoolean(_configuration.GetValue<bool>("ReturnExceptionForAnyZeroDollarAirFareValue").ToString()))
                                    {
                                        // Added as part of Bug 180337:mApp: "Sorry something went wrong... " Error message is displayed when selected cabin for second segment in the multi trip
                                        // throw new MOBUnitedException(ConfigurationManager.AppSettings["Booking2OGenericExceptionMessage"]);

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

                                flight.Messages = new List<MOBSHOPMessage>();

                                MOBSHOPMessage msg = new MOBSHOPMessage();
                                msg.FlightNumberField = flight.FlightNumber;
                                msg.TripId = flight.TripId;
                                msg.MessageCode = displayProduct.Description + " (" + displayProduct.BookingCode + ")";
                                if (selected && _shoppingUtility.IsIBeLiteFare(displayProduct)) // bug 277549: update the message for IBE Lite only when customer switch ON the 'Show Basic Economy fares'
                                {
                                    msg.MessageCode = msg.MessageCode + " " + displayProduct.CabinTypeText; // EX: United Economy (K) (first bag charge/no changes allowed)
                                }
                                flight.Messages.Add(msg);

                                msg = new MOBSHOPMessage();
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
                                //flight.YqyrMessage = ConfigurationManager.AppSettings["MP2015YQYRMessage"];

                                foreach (United.Services.FlightShopping.Common.LMX.LmxLoyaltyTier tier in displayProduct.LmxLoyaltyTiers)
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
                                                foreach (United.Services.FlightShopping.Common.LMX.LmxQuote quote in tier.LmxQuotes)
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
                        foreach (United.Services.FlightShopping.Common.Amenity amenity in segment.Amenities)
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

                        var tupleFlightRes =  await GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, segment.Connections, requestedCabin, ci, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR, null, appVersion, appID, additionalItems, carbonEmissionData);
                        ci = tupleFlightRes.ci;
                        flight.Connections = tupleFlightRes.mOBSHOPFlights;
                    }

                    flight.ConnectTimeMinutes = segment.ConnectTimeMinutes > 0 ? GetFormattedTravelTime(segment.ConnectTimeMinutes) : string.Empty;

                    flight.DepartDate = GeneralHelper.FormatDate(segment.DepartDateTime);
                    flight.DepartTime = FormatTime(segment.DepartDateTime);
                    flight.Destination = segment.Destination;
                    flight.DestinationDate = GeneralHelper.FormatDate(segment.DestinationDateTime);
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
                        foreach (United.Services.FlightShopping.Common.Warning warn in segment.Warnings)
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
                                if (!IsFSRNearByAirportAlertEnabled(appID, appVersion) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("WARNING_NEARBYAIRPORT"))
                                {
                                    continue;
                                }
                                SetSegmentInfoMessages(flight, warn);
                            }
                        }
                    }
                    flight.StopDestination = segment.StopDestination;
                    bool changeOfGauge = false;
                    if (segment.StopInfos != null && segment.StopInfos.Count > 0)
                    {
                        flight.StopInfos = new List<MOBSHOPFlight>();
                        flight.ShowSeatMap = true;
                        bool isFlightDestionUpdated = false;
                        int travelMinutes = segment.TravelMinutes;

                        foreach (United.Services.FlightShopping.Common.Flight stop in segment.StopInfos)
                        {
                            if (segment.EquipmentDisclosures != null && !string.IsNullOrEmpty(segment.EquipmentDisclosures.EquipmentType) && stop.EquipmentDisclosures != null && !string.IsNullOrEmpty(stop.EquipmentDisclosures.EquipmentType))
                            {
                                if (segment.EquipmentDisclosures.EquipmentType.Trim() == stop.EquipmentDisclosures.EquipmentType.Trim())
                                {
                                    flight.ChangeOfGauge = true;
                                    flight.ChangeOfPlane = segment.ChangeOfPlane;
                                    flight.IsThroughFlight = !segment.ChangeOfPlane;
                                    List<United.Services.FlightShopping.Common.Flight> stops = new List<United.Services.FlightShopping.Common.Flight>();
                                    stops.Add(stop);
                                    if (_mOBSHOPDataCarrier == null)
                                        _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                                    var tupleResponse = await GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, stops, requestedCabin, ci, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR, null, appVersion, appID, additionalItems, carbonEmissionData: carbonEmissionData);
                                    ci = tupleResponse.ci;
                                    List<MOBSHOPFlight> stopFlights = tupleResponse.mOBSHOPFlights;
                                    foreach (MOBSHOPFlight sf in stopFlights)
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
                                    ///
                                    if (_configuration.GetValue<bool>("BugFixToggleFor18C"))
                                    {
                                        flight.Destination = stop.Origin;

                                        if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes") && !isFlightDestionUpdated)
                                        {
                                            var isConnectThroughFlightFix = _configuration.GetValue<bool>("OmnicartConnectionThroughFlightFix")

                                                                             ? _configuration.GetValue<bool>("OmnicartConnectionThroughFlightFix")
                                                                                : flight.IsThroughFlight && flight.IsConnection;

                                            flight.Destination = isConnectThroughFlightFix ? segment.StopInfos.Last()?.Destination : stop.Origin;
                                            string destination = string.Empty;
                                            if (!string.IsNullOrEmpty(flight.Destination))
                                            {
                                                destination = await GetAirportNameFromSavedList(flight.Destination);
                                            }
                                            if (string.IsNullOrEmpty(destination))
                                            {
                                                flight.DestinationDescription = isConnectThroughFlightFix ? segment.StopInfos.Last()?.DestinationDescription : stop.OriginDescription;
                                            }
                                            else
                                            {
                                                flight.DestinationDescription = destination;
                                            }
                                            flight.DestinationDecodedWithCountry = isConnectThroughFlightFix ? segment.StopInfos.Last()?.DestinationDescription : stop.OriginDescription;
                                            flight.DestinationStateCode = isConnectThroughFlightFix ? segment.StopInfos.Last()?.DestinationStateCode : stop.OriginStateCode;
                                            flight.DestinationCountryCode = isConnectThroughFlightFix ? segment.StopInfos.Last()?.DestinationCountryCode : stop.OriginCountryCode;
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

                                    flight.DestinationDate = GeneralHelper.FormatDate(Convert.ToDateTime(stop.DepartDateTime).AddMinutes(-stop.GroundTimeMinutes).ToString());
                                    flight.DestinationTime = FormatTime(Convert.ToDateTime(stop.DepartDateTime).AddMinutes(-stop.GroundTimeMinutes).ToString());
                                    flight.ArrivalDateTime = FormatDateTime(Convert.ToDateTime(stop.DepartDateTime).AddMinutes(-stop.GroundTimeMinutes).ToString());
                                    flight.TravelTime = segment.TravelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes > 0 ? GetFormattedTravelTime(segment.TravelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes) : string.Empty;
                                    flight.ChangeOfPlane = segment.ChangeOfPlane;
                                    MOBSHOPFlight stopFlight = new MOBSHOPFlight();

                                    stopFlight.EquipmentDisclosures = GetEquipmentDisclosures(stop.EquipmentDisclosures);
                                    stopFlight.FlightNumber = stop.FlightNumber;
                                    stopFlight.ChangeOfGauge = stop.ChangeOfPlane;
                                    stopFlight.ShowSeatMap = true;
                                    stopFlight.DepartDate = GeneralHelper.FormatDate(stop.DepartDateTime);
                                    stopFlight.DepartTime = FormatTime(stop.DepartDateTime);
                                    stopFlight.Origin = stop.Origin;
                                    stopFlight.Destination = stop.Destination;
                                    stopFlight.DestinationDate = GeneralHelper.FormatDate(stop.DestinationDateTime);
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

                                        stopFlight.Messages = new List<MOBSHOPMessage>();

                                        MOBSHOPMessage msg = new MOBSHOPMessage();
                                        msg.FlightNumberField = flight.FlightNumber;
                                        msg.TripId = flight.TripId;
                                        msg.MessageCode = displayProductForStopInfo.Description + " (" + displayProductForStopInfo.BookingCode + ")";
                                        stopFlight.Messages.Add(msg);

                                        msg = new MOBSHOPMessage();
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
                                            foreach (United.Services.FlightShopping.Common.Warning warn in stop.Warnings)
                                            {
                                                if (!IsFSRNearByAirportAlertEnabled(appID, appVersion) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("WARNING_NEARBYAIRPORT"))
                                                {
                                                    continue;
                                                }
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
                            flight.Destination = flight.IsThroughFlight && flight.IsConnection ? segment.StopInfos?.Last().Destination : segment.StopInfos[0].Origin;
                            flight.DestinationDate = GeneralHelper.FormatDate(Convert.ToDateTime(segment.StopInfos[0].DepartDateTime).AddMinutes(-segment.StopInfos[0].GroundTimeMinutes).ToString());
                            flight.DestinationTime = FormatTime(Convert.ToDateTime(segment.StopInfos[0].DepartDateTime).AddMinutes(-segment.StopInfos[0].GroundTimeMinutes).ToString());
                            flight.TravelTime = travelMinutes > 0 ? GetFormattedTravelTime(travelMinutes) : string.Empty;
                        }
                        if (flight.IsConnection && flight.IsThroughFlight)
                        {
                            flight.TravelTime = segment.TravelMinutes > 0 ? GetFormattedTravelTime(segment.TravelMinutes) : string.Empty;
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

                    var tupleRes = await PopulateProducts(_mOBSHOPDataCarrier, segment.Products, sessionId, flight, requestedCabin, segment, lowestFare, columnInfo, premierStatusLevel, fareClass, isELFFareDisplayAtFSR, appVersion, additionalItems);
                    flight.ShoppingProducts = tupleRes.shoppingProducts;
                    flight = tupleRes.flight;
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

            return (flights,ci);
        }
        private string GetMixedCabinTextForFlight(United.Services.FlightShopping.Common.Flight flt)
        {
            //group the mixed cabin messages                
            string tempMsgs = "";
            if (flt.Products != null && flt.Products.Count > 0)
            {
                foreach (United.Services.FlightShopping.Common.Product prod in flt.Products)
                {
                    if (!string.IsNullOrEmpty(prod.CrossCabinMessaging))
                        tempMsgs = "Mixed cabin";
                }
            }

            return tempMsgs;
        }

        private async Task<(List<MOBSHOPShoppingProduct> shoppingProducts, MOBSHOPFlight flight)> PopulateProducts(MOBSHOPDataCarrier _mOBSHOPDataCarrier, ProductCollection products, string sessionId, MOBSHOPFlight flight, string cabin, United.Services.FlightShopping.Common.Flight segment, decimal lowestAirfare, List<MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClas, bool isELFFareDisplayAtFSR = true, string appVersion = "", MOBAdditionalItems additionalItems = null)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            Session session = new Session();
            //session = Persist.FilePersist.Load<Session>(sessionId, session.ObjectName);
            session = await _sessionHelperService.GetSession<Session>(sessionId, new Session().ObjectName, new List<string> { sessionId, new Session().ObjectName }).ConfigureAwait(false);
            supressLMX = session.SupressLMXForAppID;
            #endregion
            if (_mOBSHOPDataCarrier == null)
                _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
            return (PopulateProducts(_mOBSHOPDataCarrier, products, cabin, segment, lowestAirfare, columnInfo, premierStatusLevel, fareClas, supressLMX, session, isELFFareDisplayAtFSR, appVersion, additionalItems),flight);
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
        private string GetDescriptionFromColumn(string productType, List<MOBSHOPShoppingProduct> columnInfo)
        {
            string description = string.Empty;

            ProductSection section = _configuration.GetValue<ProductSection>("productSettings") as ProductSection;
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
        private string GetCabinDescriptionFromColumn(string type, List<MOBSHOPShoppingProduct> columnInfo)
        {
            type = type.IsNullOrEmpty() ? string.Empty : type.ToUpper().Trim();

            string cabin = string.Empty;
            if (columnInfo != null && columnInfo.Count > 0)
            {
                foreach (MOBSHOPShoppingProduct prod in columnInfo)
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
        private MOBSHOPShoppingProduct TransformProductWithoutPriceToNewProduct(string cabin, List<MOBSHOPShoppingProduct> columnInfo, bool isUADiscount,
           United.Services.FlightShopping.Common.Product prod, bool foundEconomyAward, bool foundBusinessAward, bool foundFirstAward, Session session)
        {
            MOBSHOPShoppingProduct newProd = new MOBSHOPShoppingProduct();
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
                newProd.ProductCode = _configuration.GetValue<string>("ELFProductCode");
            }

            if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes"))
            {
                newProd.CabinDescription = prod.Description;
                newProd.BookingCode = prod.BookingCode;
            }
            return newProd;
        }

        private void SetLmxLoyaltyInformation(int premierStatusLevel, United.Services.FlightShopping.Common.Product prod, bool supressLMX, CultureInfo ci,
    MOBSHOPShoppingProduct newProd)
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

        private void SetProductDetails(United.Services.FlightShopping.Common.Flight segment, List<MOBSHOPShoppingProduct> columnInfo, United.Services.FlightShopping.Common.Product prod, int productIndex,
   MOBSHOPShoppingProduct newProd)
        {
            if (productIndex >= 2 && segment.Connections != null && segment.Connections.Count > 0)
            {
                if (!string.IsNullOrEmpty(prod.ProductType) &&
                    (prod.ProductType.Contains("FIRST") || prod.ProductType.Contains("BUSINESS")) &&
                    !string.IsNullOrEmpty(prod.Description) && prod.Description.Contains("Economy"))
                {
                    newProd.LongCabin = GetCabinDescriptionFromColumn(columnInfo[productIndex].Type, columnInfo);
                    newProd.Description = GetDescriptionFromColumn(columnInfo[productIndex].Type, columnInfo);

                    ProductSection section = _configuration.GetValue<ProductSection>("productSettings") as ProductSection;
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
                        ProductSection section = _configuration.GetValue<ProductSection>("productSettings") as ProductSection;
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
                    ProductSection section = _configuration.GetValue<ProductSection>("productSettings") as ProductSection;
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

        private void SetMileageButtonAndAwardFound(string cabin, United.Services.FlightShopping.Common.Product prod, ref bool foundEconomyAward,
          ref bool foundBusinessAward, ref bool foundFirstAward, string cabinType, MOBSHOPShoppingProduct newProd)
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

        private decimal CalculateCloseInAwardFee(United.Services.FlightShopping.Common.Product prod)
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
        private MOBSHOPShoppingProduct ReShopAirfareCreditDisplayFSRD
    (CultureInfo ci, United.Services.FlightShopping.Common.Product product, MOBSHOPShoppingProduct shoppingProduct)
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

                    shoppingProduct.ReshopCreditColor = Model.Shopping.CreditTypeColor.GREEN;

                    //displayPrice = string.Concat("+", displayPrice);
                    //AirfareDisplayValue
                    if (product.CreditType == CreditTypes.Refund)
                    {
                        shoppingProduct.ReshopFees = Model.Common.CreditType.REFUND.GetDisplayName();
                        shoppingProduct.IsReshopCredit = true;
                    }
                    else if (product.CreditType == CreditTypes.FlightCredit)
                    {
                        shoppingProduct.ReshopFees = Model.Common.CreditType.FLIGHTCREDIT.GetDisplayName();
                        shoppingProduct.IsReshopCredit = true;
                    }

                    shoppingProduct.Price = strDisplayCredit;
                }
            }
            return shoppingProduct;
        }
        private void SetProductPriceInformation(United.Services.FlightShopping.Common.Product prod, CultureInfo ci, MOBSHOPShoppingProduct newProd, Session session, string appVersion = "", MOBAdditionalItems additionalItems = null)
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
                        newProd.MilesDisplayValue = formatAwardAmountForDisplay(ReshopAwardPrice(prod.Prices).Amount.ToString(), true);
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
                        ? formatAwardAmountForDisplay(totalAmount.ToString(), true)
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
                    ? formatAwardAmountForDisplay(prod.Prices[0].Amount.ToString(), true)
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
                if (_configuration.GetValue<bool>("EnableAwardStrikeThroughPricing") && session.IsAward && session.CatalogItems != null && session.CatalogItems.Count > 0 &&
         session.CatalogItems.FirstOrDefault(a => a.Id == ((int)United.Common.Helper.Profile.IOSCatalogEnum.AwardStrikeThroughPricing).ToString() || a.Id == ((int)United.Common.Helper.Profile.AndroidCatalogEnum.AwardStrikeThroughPricing).ToString())?.CurrentValue == "1"
         && prod.Context?.StrikeThroughPricing != null && prod.Context?.StrikeThroughPricing?.PaxPrice != null && prod.Prices?.Any(x => x.PricingType == "Award") == true
         && (int)prod.Context?.StrikeThroughPricing?.PaxPrice?.Miles != (int)prod.Prices?.FirstOrDefault(x => x.PricingType == "Award").Amount
         )
                {
                    newProd.StrikeThroughDisplayValue = formatAwardAmountForDisplay(prod.Context?.StrikeThroughPricing?.PaxPrice?.Miles.ToString(), true);
                    if (additionalItems == null)
                    {
                        additionalItems = new MOBAdditionalItems();
                    }
                    if (additionalItems.StrikeThroughPricing == false)
                    {
                        additionalItems.StrikeThroughPricing = true;
                    }
                }
                //if (("EnableAwardNGRPPricing") && session.IsAward && session.CatalogItems != null && session.CatalogItems.Count > 0 &&
                //        session.CatalogItems.FirstOrDefault(a => a.Id == ((int)United.Common.Helper.Profile.IOSCatalogEnum.AwardStrikeThroughPricing).ToString() || a.Id == ((int)United.Common.Helper.Profile.AndroidCatalogEnum.AwardStrikeThroughPricing).ToString())?.CurrentValue == "1")
                //{

                //    newProd.IsNGRPPricing = true;
                //    newProd.GeneralMemberMilesDisplayValue = formatAwardAmountForDisplay("25000", true);
                //}
                newProd.PriceAmount = prod.Prices[0].Amount;
            }
        }

        private void SetProductMixedCabinInformation(United.Services.FlightShopping.Common.Flight segment, United.Services.FlightShopping.Common.Product prod, MOBSHOPShoppingProduct newProd)
        {
            MOBSHOPShoppingProductDetailCabinMessage detailCabinMessage = new MOBSHOPShoppingProductDetailCabinMessage();
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

        private List<MOBStyledText> SetProductBadgeInformation(List<MOBStyledText> badges, MOBStyledText badge)
        {
            if (badges == null)
                badges = new List<MOBStyledText>();

            badges.Add(badge);

            if (badges.Count > 1)
            {
                badges = badges.OrderBy(x => (int)Enum.Parse(typeof(MOBFlightProductBadgeSortOrder), x.SortPriority)).ToList();
            }

            return badges;
        }

        private MOBStyledText MixedCabinBadge()
        {
            return new MOBStyledText()
            {
                Text = _configuration.GetValue<string>("MixedCabinProductBadgeText"),
                SortPriority = MOBFlightProductBadgeSortOrder.MixedCabin.ToString()
            };
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

        private  MOBStyledText PromoTextBadge(string promoText)
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

        private MOBSHOPShoppingProduct TransformProductWithPriceToNewProduct
    (string cabin, United.Services.FlightShopping.Common.Flight segment, decimal lowestAirfare,
    List<MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, bool isUADiscount, United.Services.FlightShopping.Common.Product prod, bool supressLMX, CultureInfo ci,
    string fareClass, int productIndex, ref bool foundCabinSelected, ref bool foundEconomyAward,
    ref bool foundBusinessAward, ref bool foundFirstAward, Session session, bool isELFFareDisplayAtFSR, string appVersion = "", MOBAdditionalItems additionalItems = null, string corporateFareIndicator = "", string yaDiscount = "")
        {
            MOBSHOPShoppingProduct newProd = new MOBSHOPShoppingProduct();
            newProd.ProductDetail = new MOBSHOPShoppingProductDetail();
            newProd.ProductDetail.ProductCabinMessages = new List<MOBSHOPShoppingProductDetailCabinMessage>();

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

            SetProductPriceInformation(prod, ci, newProd, session, appVersion, additionalItems);
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

            // Added for AFS coupon story - MOBILE-10165
            if (_configuration.GetValue<bool>("EnableAdvanceSearchCouponBooking") && session.IsFSRRedesign && !string.IsNullOrEmpty(prod.PromoDescription)) //&& prod.PromoDescription.Equals("Special offer")
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
                newProd.ProductCode = _configuration.GetValue<string>("ELFProductCode");
            }

            if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes"))
            {
                newProd.CabinDescription = prod.Description;
                newProd.BookingCode = prod.BookingCode;
            }

            return newProd;
        }


        private List<MOBSHOPShoppingProduct> PopulateProducts(MOBSHOPDataCarrier _mOBSHOPDataCarrier, ProductCollection products, string cabin, United.Services.FlightShopping.Common.Flight segment, decimal lowestAirfare,
       List<MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClas, bool supressLMX, Session session, bool isELFFareDisplayAtFSR = true, string appVersion = ""
            , MOBAdditionalItems additionalItems = null, string corporateFareIndicator = "", string yaDiscount = "")
        {
            var shopProds = new List<MOBSHOPShoppingProduct>();
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
                            ref foundEconomyAward, ref foundBusinessAward, ref foundFirstAward, session, isELFFareDisplayAtFSR, appVersion, additionalItems, corporateFareIndicator, yaDiscount);

                        if (ConfigUtility.EnableIBELite() && !string.IsNullOrWhiteSpace(prod.ProductCode))
                        {
                            newProd.IsIBELite = _configuration.GetValue<string>("IBELiteShoppingProductCodes").IndexOf(prod.ProductCode.Trim().ToUpper()) > -1;

                            if (newProd.IsIBELite) // per clients' request when implementing IBELite
                                newProd.ShortProductName = _configuration.GetValue<string>("IBELiteShortProductName");
                        }

                        if (ConfigUtility.EnableIBEFull() && !string.IsNullOrWhiteSpace(prod.ProductCode))
                        {
                            newProd.IsIBE = _configuration.GetValue<string>("IBEFULLShoppingProductCodes").IndexOf(prod.ProductCode.Trim().ToUpper()) > -1;

                            if (newProd.IsIBE)
                                newProd.ShortProductName = _configuration.GetValue<string>("IBEFULLShortProductName");
                        }
                        if (_configuration.GetValue<bool>("EnableTripPlannerView") && (session.TravelType == MOBTripPlannerType.TPSearch.ToString() || session.TravelType == MOBTripPlannerType.TPEdit.ToString()))
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

            List<MOBSHOPShoppingProduct> updatedShopProducts = new List<MOBSHOPShoppingProduct>();
            foreach (MOBSHOPShoppingProduct mobShopProduct in shopProds)
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
        private void HighlightProductMatchingSelectedCabin(List<MOBSHOPShoppingProduct> shopProds, string requestedCabin, List<MOBSHOPShoppingProduct> columnInfo, string fareClass, bool isReshopChange, bool isELFFareDisplayAtFSR = true)
        {
            IOrderedEnumerable<MOBSHOPShoppingProduct> productsSortedByPrice = null;
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
        private void CalculateAwardCounts(List<MOBSHOPShoppingProduct> shopProds, ref int econAwardCount, ref int busAwardCount,
    ref int firstAwardCount)
        {
            foreach (MOBSHOPShoppingProduct prod in shopProds)
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

        private void ClearMileageButtonAndAllCabinButtonText(List<MOBSHOPShoppingProduct> shopProds, int econAwardCount, int busAwardCount,
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

        private void SetIsPremierCabinSaverIfApplicable(MOBSHOPShoppingProduct mobShopProduct)
        {
            if (mobShopProduct.AwardType.Trim().ToUpper().Contains("SAVER") &&
                !mobShopProduct.LongCabin.Trim().ToUpper().Contains("ECON"))
            {
                mobShopProduct.ISPremierCabinSaver = true;
            }
        }
        private void SetShortCabin(MOBSHOPShoppingProduct shopProd, List<MOBSHOPShoppingProduct> columnInfo, ProductSection configurationProductSettings)
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
        private void SetShoppingProductDescriptionBasedOnProductElementDescription
    (MOBSHOPShoppingProduct shopProd, List<MOBSHOPShoppingProduct> columnInfo, IProductSection configurationProductSettings)
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
        private SHOPOnTimePerformance PopulateOnTimePerformanceSHOP(Service.Presentation.ReferenceDataModel.DOTAirlinePerformance onTimePerformance)
        {
            SHOPOnTimePerformance shopOnTimePerformance = null;

            if (_configuration.GetValue<string>("ReturnOnTimePerformance") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnOnTimePerformance").ToString()))
            {
                #region
                if (onTimePerformance != null)
                {
                    shopOnTimePerformance = new SHOPOnTimePerformance();
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
                    shopOnTimePerformance = new SHOPOnTimePerformance();
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

        private string FormatDateTimeTripPlan(string dateTimeString)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(0);
            DateTime.TryParse(dateTimeString, out dateTime);
            result = string.Format("{0:ddd, MMM dd}", dateTime);

            return result;
        }

        public static PricingItem ReshopAwardPrice(List<PricingItem> price)
        {
            if (price.Exists(p => p.PricingType == "Award"))
                return price.FirstOrDefault(p => p.PricingType == "Award");

            return null;
        }
        private decimal ReshopAirfareCreditDisplayInDecimal(List<PricingItem> price, string priceType)
        {
            decimal retVal = 0;
            if (price.Exists(p => string.Equals(p.PricingType, priceType, StringComparison.OrdinalIgnoreCase)))
                retVal = price.FirstOrDefault(p => string.Equals(p.PricingType, priceType, StringComparison.OrdinalIgnoreCase)).Amount;
            return retVal;
        }
        private MOBSHOPFlight ReShopAirfareCreditDisplayFSR(CultureInfo ci, United.Services.FlightShopping.Common.Product product, MOBSHOPFlight flight)
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

                    flight.ReshopCreditColor = Model.Shopping.CreditTypeColor.GREEN;

                    //displayPrice = string.Concat("+", displayPrice);
                    //AirfareDisplayValue
                    if (product.CreditType == CreditTypes.Refund)
                    {
                        flight.ReshopFees = Model.Common.CreditType.REFUND.GetDisplayName();
                        flight.IsReshopCredit = true;
                    }
                    else if (product.CreditType == CreditTypes.FlightCredit)
                    {
                        flight.ReshopFees = Model.Common.CreditType.FLIGHTCREDIT.GetDisplayName();
                        flight.IsReshopCredit = true;
                    }

                    flight.AirfareDisplayValue = strDisplayCredit;
                }
            }
            return flight;
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

        private void GetBestProductTypeTripPlanner(Session session, United.Services.FlightShopping.Common.Product displayProduct, bool isSelected, ref string bestProductType)
        {
            if (IsTripPlanSearch(session.TravelType))
            {
                if (string.IsNullOrEmpty(bestProductType) && !isSelected)
                {
                    bestProductType = displayProduct?.ProductType;
                }
            }
        }

        private static string ReshopAirfareDisplayText(List<PricingItem> price)
        {
            bool isAddCollect = (price.Exists(p => p.PricingType == "AddCollect"))
                ? price.FirstOrDefault(p => p.PricingType == "AddCollect")?.Amount > 0 : false;

            bool isChangeFee = (price.Exists(p => p.PricingType == "ChangeFee"))
                ? price.FirstOrDefault(p => p.PricingType == "ChangeFee")?.Amount > 0 : false;

            return (isAddCollect && isChangeFee)
                ? "Price difference and change fee" : (isAddCollect) ? "Price difference"
                : (isChangeFee) ? "change fee" : string.Empty;
        }

        private SegmentInfoAlerts TicketsLeftSegmentInfo(string msg)
        {
            SegmentInfoAlerts segmentInfoAlerts = new SegmentInfoAlerts();
            segmentInfoAlerts.AlertMessage = msg;
            segmentInfoAlerts.AlertType = MOBFSRAlertMessageType.Warning.ToString();
            segmentInfoAlerts.Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString();
            segmentInfoAlerts.SortOrder = MOBSHOPSegmentInfoAlertsOrder.TicketsLeft.ToString();
            return segmentInfoAlerts;
        }

        private bool IsTripPlanSearch(string travelType) =>
            _configuration.GetValue<bool>("EnableTripPlannerView") && (travelType == MOBTripPlannerType.TPSearch.ToString() || travelType == MOBTripPlannerType.TPEdit.ToString());


        private bool IsYoungAdultProduct(ProductCollection pc)
        {
            return pc != null && pc.Count > 0 && pc.Any(p => p.ProductSubtype.ToUpper().Equals("YOUNGADULTDISCOUNTEDFARE"));
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
        private void SetAutoFocusIfMissed(Session session, bool isELFFareDisplayAtFSR, List<MOBSHOPShoppingProduct> shopProds, string productTypeBestMatched)
        {
            if (_configuration.GetValue<bool>("EnableTripPlannerView") && (session.TravelType == MOBTripPlannerType.TPSearch.ToString() ||
                session.TravelType == MOBTripPlannerType.TPEdit.ToString()))
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
        private string GetCabinNameFromColumn(string type, List<MOBSHOPShoppingProduct> columnInfo, string defaultCabin)
        {
            type = type.IsNullOrEmpty() ? string.Empty : type.ToUpper().Trim();

            string cabin = "Economy";
            if (columnInfo != null && columnInfo.Count > 0)
            {
                foreach (MOBSHOPShoppingProduct prod in columnInfo)
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
        private United.Services.FlightShopping.Common.Product GetMatrixDisplayProduct(ProductCollection products, string fareSelected, List<MOBSHOPShoppingProduct> columnInfo, out CultureInfo ci, out bool isSelectedFareFamily, out string serviceClassDesc, out bool isMixedCabin, out int seatsRemaining, string fareClass, bool isConnectionOrStopover = false, bool isELFFareDisplayAtFSR = true)
        {
            var bestMatch = new United.Services.FlightShopping.Common.Product();
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
                foreach (United.Services.FlightShopping.Common.Product prod in products)
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

        private void AssignCorporateFareIndicator(United.Services.FlightShopping.Common.Flight segment, MOBSHOPFlight flight, string travelType = "")
        {
            bool isCorporateBooking = Convert.ToBoolean(_configuration.GetValue<string>("CorporateConcurBooking") ?? "false");

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
        private void PopulateSeats(ref List<MOBCPTraveler> bookedTravelers, Service.Presentation.FlightResponseModel.AssignTravelerSeat assignedSeats)
        {
            foreach (MOBCPTraveler t in bookedTravelers)
            {
                t.Seats = new List<MOBSeat>();
            }
            if (assignedSeats.Travelers != null && assignedSeats.Travelers.Count > 0)
            {
                foreach (Service.Presentation.ProductModel.ProductTraveler traveler in assignedSeats.Travelers)
                {
                    foreach (MOBCPTraveler t in bookedTravelers)
                    {
                        if (traveler.Seats != null && traveler.Seats.Count > 0 && traveler.GivenName.ToUpper() == t.FirstName.ToUpper() && traveler.Surname.ToUpper() == t.LastName.ToUpper())
                        {
                            foreach (Service.Presentation.SegmentModel.SeatDetail seat in traveler.Seats)
                            {
                                if (Convert.ToBoolean(seat.Status))
                                {
                                    MOBSeat assignedSeat = new MOBSeat();
                                    assignedSeat.SeatAssignment = seat.Seat.Identifier;
                                    assignedSeat.TravelerSharesIndex = traveler.TravelerNameIndex;
                                    assignedSeat.Origin = seat.DepartureAirport.IATACode;
                                    assignedSeat.Destination = seat.ArrivalAirport.IATACode;
                                    if (seat.Seat.Price != null)
                                    {
                                        assignedSeat.Price = Convert.ToDecimal(seat.Seat.Price.Totals[0].Amount);
                                        assignedSeat.Currency = seat.Seat.Price.Totals[0].Currency.Code;
                                        if (ConfigUtility.IsMilesFOPEnabled())
                                        {
                                            assignedSeat.OldSeatMiles = Convert.ToInt32(_configuration.GetValue<string>("milesFOP"));

                                            assignedSeat.DisplayOldSeatMiles = formatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false);
                                        }
                                    }

                                    assignedSeat.ProgramCode = seat.ProgramCode;
                                    assignedSeat.SeatType = seat.Seat.SeatType;

                                    t.Seats.Add(assignedSeat);
                                }
                            }
                        }
                    }
                }
            }
        }

        private string formatAwardAmountForDisplay(string amt, bool truncate = true)
        {
            string newAmt = amt;
            decimal amount = 0;
            decimal.TryParse(amt, out amount);

            try
            {
                if (amount > -1)
                {
                    if (truncate)
                    {
                        //int newTempAmt = (int)decimal.Ceiling(amount);
                        try
                        {
                            if (amount > 999)
                            {
                                amount = amount / 1000;
                                if (amount % 1 > 0)
                                    newAmt = string.Format("{0:n1}", amount) + "K miles";
                                else
                                    newAmt = string.Format("{0:n0}", amount) + "K miles";
                            }
                            else
                            {
                                newAmt = string.Format("{0:n0}", amount) + " miles";
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            newAmt = string.Format("{0:n0}", amount) + " miles";
                        }
                        catch { }
                    }
                }

            }
            catch { }

            return newAmt;
        }

        private bool IsAnySeatChoosen(MOBSHOPReservation reservation)
        {
            if (_configuration.GetValue<bool>("DisableSSA_SeatAssignmentFailure"))
                return false;

            if (reservation.IsNullOrEmpty() || reservation.TravelersCSL.IsNullOrEmpty())
                return false;

            return reservation.TravelersCSL.Where(t => !t.Seats.IsNullOrEmpty())
                                            .SelectMany(t => t.Seats)
                                            .Any(s => !s.SeatAssignment.IsNullOrEmpty());
        }
        private SHOPEquipmentDisclosure GetEquipmentDisclosures(EquipmentDisclosure equipmentDisclosure)
        {
            SHOPEquipmentDisclosure bkEquipmentDisclosure = null;
            if (equipmentDisclosure != null)
            {
                bkEquipmentDisclosure = new SHOPEquipmentDisclosure();
                bkEquipmentDisclosure.EquipmentType = equipmentDisclosure.EquipmentType;
                bkEquipmentDisclosure.EquipmentDescription = equipmentDisclosure.EquipmentDescription;
                bkEquipmentDisclosure.IsSingleCabin = equipmentDisclosure.IsSingleCabin;
                bkEquipmentDisclosure.NoBoardingAssistance = equipmentDisclosure.NoBoardingAssistance;
                bkEquipmentDisclosure.NonJetEquipment = equipmentDisclosure.NonJetEquipment;
                bkEquipmentDisclosure.WheelchairsNotAllowed = equipmentDisclosure.WheelchairsNotAllowed;
            }
            return bkEquipmentDisclosure;
        }
        private string GetFormattedGroundTime(string groundTime, string origin, List<United.Services.FlightShopping.Common.Warning> warnings, bool isChangeOfPlane)
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

                if (string.IsNullOrWhiteSpace(groundTimeWarningEntry.Duration))
                    return GetFormattedTravelTime(minutes);


                return groundTimeWarningEntry.Duration;
            }
            else
                return groundTime;
        }

        private bool IsFSRNearByAirportAlertEnabled(int id, string version)
        {
            if (!_configuration.GetValue<bool>("EnableFSRNearByAirportAlertFeature")) return false;
            return GeneralHelper.IsApplicationVersionGreaterorEqual(id, version, _configuration.GetValue<string>("Android_EnableFSRNearByAirportAlertFeature_AppVersion"), _configuration.GetValue<string>("IPhone_EnableFSRNearByAirportAlertFeature_AppVersion"));
        }
        private List<SegmentInfoAlerts> SetFlightInformationMessage(List<SegmentInfoAlerts> flightSegmentAlerts, SegmentInfoAlerts alert)
        {
            if (flightSegmentAlerts == null)
                flightSegmentAlerts = new List<SegmentInfoAlerts>();

            //alert.AlignLeft = flightSegmentAlerts == null || (flightSegmentAlerts.Count % 2 > 0);
            //alert.AlignRight = flightSegmentAlerts != null && flightSegmentAlerts.Count % 2 == 0;
            flightSegmentAlerts.Add(alert);

            if (flightSegmentAlerts.Count > 1)
                flightSegmentAlerts = flightSegmentAlerts.OrderBy(x => (int)Enum.Parse(typeof(MOBSHOPSegmentInfoAlertsOrder), x.SortOrder)).ToList();

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
        private SegmentInfoAlerts ArrivesNextDaySegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                //ConfigurationManager.AppSettings["NextDayArrivalSegmentText"],
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.ArrivesNextDay.ToString()
            };
        }
        private SegmentInfoAlerts AirportChangeSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Warning.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.AirportChange.ToString()
            };
        }

        private SegmentInfoAlerts SubjectOfReceiptOfGovtAuthSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.GovAuthority.ToString()
            };
        }
        private SegmentInfoAlerts LonglayoverSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.LongLayover.ToString()
            };
        }

        private SegmentInfoAlerts RedEyeFlightSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.RedEyeFlight.ToString()
            };
        }
        private SegmentInfoAlerts RiskyConnectionSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.RiskyConnection.ToString()
            };
        }
        private SegmentInfoAlerts TerminalChangeSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.TerminalChange.ToString()
            };
        }
        private SegmentInfoAlerts NearByAirportSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.NearByAirport.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRCollapsed.ToString()
            };
        }
        private void SetSegmentInfoMessages(MOBSHOPFlight flight, United.Services.FlightShopping.Common.Warning warn)
        {

            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("ARRIVAL_Slice")
                && !string.IsNullOrEmpty(warn.Title))
            {
                flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, ArrivesNextDaySegmentInfo(warn.Title));
            }
            if (!string.IsNullOrEmpty(warn.Key) && (warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("CHANGE_OF_AIRPORT_SLICE"))
                || (_configuration.GetValue<bool>("EnableAwardFSRChanges") && !string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("CHANGE_OF_AIRPORT_SLICE_KEY")))
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
            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("WARNING_NEARBYAIRPORT"))
            {
                if (warn.Messages != null && warn.Messages.Count > 0)
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, NearByAirportSegmentInfo(warn.Messages[0]));
                }
                else if (!string.IsNullOrEmpty(warn.Title))
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, NearByAirportSegmentInfo(warn.Title));
                }
            }
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
        private string FormatDateTime(string dateTimeString)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(0);
            DateTime.TryParse(dateTimeString, out dateTime);
            result = string.Format("{0:MM/dd/yyyy hh:mm tt}", dateTime);

            return result;
        }

        private List<MOBSHOPShoppingProductDetailCabinMessage> GetProductDetailMixedCabinSegments(List<MOBSHOPFlight> flights, int index)
        {
            //group the mixed cabin messages                
            List<MOBSHOPShoppingProductDetailCabinMessage> tempMsgs = new List<MOBSHOPShoppingProductDetailCabinMessage>();
            foreach (MOBSHOPFlight flt in flights)
            {
                if (flt.ShoppingProducts != null && flt.ShoppingProducts.Count > index)
                {
                    if (flt.ShoppingProducts[index].ProductDetail.ProductCabinMessages != null && flt.ShoppingProducts[index].ProductDetail.ProductCabinMessages.Count > 0)
                        tempMsgs.AddRange(flt.ShoppingProducts[index].ProductDetail.ProductCabinMessages);
                }
            }

            return tempMsgs;
        }

        private bool GetSelectedCabinInMixedScenario(List<MOBSHOPFlight> flights, int index)
        {
            //group the mixed cabin messages                
            bool selected = false;
            foreach (MOBSHOPFlight flt in flights)
            {
                if (flt.ShoppingProducts != null && flt.ShoppingProducts.Count > index)
                {
                    if (flt.ShoppingProducts[index].IsSelectedCabin)
                        selected = flt.ShoppingProducts[index].IsSelectedCabin;
                }
            }

            return selected;
        }

        private List<string> GetFlightMixedCabinSegments(List<MOBSHOPFlight> flights, int index)
        {
            //group the mixed cabin messages                
            List<string> tempMsgs = new List<string>();
            foreach (MOBSHOPFlight flt in flights)
            {
                if (flt.ShoppingProducts != null && flt.ShoppingProducts.Count > index)
                {
                    if (flt.ShoppingProducts[index].MixedCabinSegmentMessages != null && flt.ShoppingProducts[index].MixedCabinSegmentMessages.Count > 0)
                        tempMsgs.AddRange(flt.ShoppingProducts[index].MixedCabinSegmentMessages);
                }
            }

            return tempMsgs;
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

        private void GetAmenitiesForFlight(Collection<AmenitiesProfile> amenityFlights, ref United.Services.FlightShopping.Common.Flight flight)
        {
            foreach (AmenitiesProfile amenityFlight in amenityFlights)
            {
                if (flight.FlightNumber == amenityFlight.Key)
                {
                    //update flight amenities
                    flight.Amenities = amenityFlight.Amenities;
                    return;
                }
            }
        }
        private void PopulateFlightAmenities(Collection<AmenitiesProfile> amenityFlights, ref List<United.Services.FlightShopping.Common.Flight> flights)
        {
            if (amenityFlights != null && amenityFlights.Count > 0)
            {
                try
                {
                    foreach (United.Services.FlightShopping.Common.Flight flight in flights)
                    {
                        United.Services.FlightShopping.Common.Flight tempFlight = flight;
                        GetAmenitiesForFlight(amenityFlights, ref tempFlight);
                        flight.Amenities = tempFlight.Amenities;

                        if (flight.Connections != null && flight.Connections.Count > 0)
                        {
                            List<United.Services.FlightShopping.Common.Flight> tempFlights = flight.Connections;
                            PopulateFlightAmenities(amenityFlights, ref tempFlights);
                            flight.Connections = tempFlights;
                        }
                        if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                        {
                            List<United.Services.FlightShopping.Common.Flight> tempFlights = flight.StopInfos;
                            PopulateFlightAmenities(amenityFlights, ref tempFlights);
                            flight.StopInfos = tempFlights;
                        }
                    }
                }
                catch { }
            }
        }

        private string GetFlightHasListForLMX(List<United.Services.FlightShopping.Common.Flight> flights)
        {
            List<string> flightNumbers = new List<string>();
            string flightHash = string.Empty;
            if (flights != null)
            {
                try
                {
                    foreach (United.Services.FlightShopping.Common.Flight flight in flights)
                    {
                        if (!flightNumbers.Contains(flight.Hash))
                        {
                            flightNumbers.Add(flight.Hash);
                        }
                        if (flight.Connections != null && flight.Connections.Count > 0)
                        {
                            foreach (United.Services.FlightShopping.Common.Flight connection in flight.Connections)
                            {
                                if (!flightNumbers.Contains(connection.Hash))
                                {
                                    flightNumbers.Add(connection.Hash);
                                }
                            }
                        }
                        if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                        {
                            foreach (United.Services.FlightShopping.Common.Flight stop in flight.StopInfos)
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
        private void GetLMXForFlight(List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights, ref United.Services.FlightShopping.Common.Flight flight)
        {
            foreach (United.Services.FlightShopping.Common.LMX.LmxFlight lmxFlight in lmxFlights)
            {
                if (flight.Hash == lmxFlight.Hash)
                {
                    //overwrite the products with new LMX versions
                    for (int i = 0; i < flight.Products.Count; i++)
                    {
                        United.Services.FlightShopping.Common.Product tempProduct = flight.Products[i];
                        GetLMXForProduct(lmxFlight.Products, ref tempProduct);
                        flight.Products[i] = tempProduct;
                    }
                    return;
                }
            }
        }
        private void GetLMXForProduct(List<United.Services.FlightShopping.Common.LMX.LmxProduct> productCollection, ref United.Services.FlightShopping.Common.Product tempProduct)
        {
            foreach (United.Services.FlightShopping.Common.LMX.LmxProduct p in productCollection)
            {
                if (p.ProductId == tempProduct.ProductId)
                {
                    tempProduct.LmxLoyaltyTiers = p.LmxLoyaltyTiers;
                }
            }
        }
        private void PopulateLMX(List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights, ref List<United.Services.FlightShopping.Common.Flight> flights)
        {
            if (lmxFlights != null && lmxFlights.Count > 0)
            {
                try
                {
                    for (int i = 0; i < flights.Count; i++)
                    {
                        United.Services.FlightShopping.Common.Flight tempFlight = flights[i];
                        GetLMXForFlight(lmxFlights, ref tempFlight);
                        flights[i].Products = tempFlight.Products;

                        if (flights[i].Connections != null && flights[i].Connections.Count > 0)
                        {
                            List<United.Services.FlightShopping.Common.Flight> tempFlights = flights[i].Connections;
                            PopulateLMX(lmxFlights, ref tempFlights);
                            flights[i].Connections = tempFlights;
                        }
                        if (flights[i].StopInfos != null && flights[i].StopInfos.Count > 0)
                        {
                            List<United.Services.FlightShopping.Common.Flight> tempFlights = flights[i].StopInfos;
                            PopulateLMX(lmxFlights, ref tempFlights);
                            flights[i].StopInfos = tempFlights;
                        }
                    }
                }
                catch { }
            }
        }

        public bool IsCubaAirportCodeExist(string origin, string destination, List<string> CubaAirports)
        {
            bool isCubaFight = false;
            if (CubaAirports != null && (CubaAirports.Exists(p => p == origin) || CubaAirports.Exists(p => p == destination)))
            {
                isCubaFight = true;
            }
            return isCubaFight;
        }

        private bool IsCubaTravelTrip(MOBSHOPReservation reservation)
        {
            bool isCubaFight = false;

            string CubaAirports = _configuration.GetValue<string>("CubaAirports");
            List<string> CubaAirportList = CubaAirports.Split('|').ToList();

            if (reservation != null && reservation.Trips != null)
            {
                foreach (MOBSHOPTrip trip in reservation.Trips)
                {
                    isCubaFight = IsCubaAirportCodeExist(trip.Origin, trip.Destination, CubaAirportList);

                    foreach (var flight in trip.FlattenedFlights)
                    {
                        foreach (var stopFlights in flight.Flights)
                        {
                            isCubaFight = IsCubaAirportCodeExist(stopFlights.Origin, stopFlights.Destination, CubaAirportList);
                            if (!isCubaFight && stopFlights.StopInfos != null)
                            {
                                foreach (var stop in stopFlights.StopInfos)
                                {
                                    isCubaFight = IsCubaAirportCodeExist(stop.Origin, stop.Destination, CubaAirportList);
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
                _logger.LogError("GetNationalityResidenceCountries Exception {sessionId} and {Message}", sessionID, ex.Message);

            }
            if (deviceId.ToUpper().Trim() != "SCHEDULED_PublicKey_UPDADE_JOB".ToUpper().Trim())
            {
                //TODO: Complete
                //System.Threading.Tasks.Task.Factory.StartNew(() => Authentication.Write(logEntries));
                //System.Threading.Tasks.Task.Factory.StartNew(() => Write(logEntries));

            }

            return lstNationalityResidenceCountries;
        }

        private async Task<bool> IsRequireNatAndCR(MOBSHOPReservation reservation, MOBApplication application, string sessionID, string deviceID, string token)
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
                catch(Exception e)
                {
                    _logger.LogError("UnfinishedBookingBusiness -SelectOmniCartSavedTrip IsRequireNatAndCR SaveCache Failed - Exception {error} and SessionId {guid}", e.Message, sessionID);
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

        private FareLock GetFareLockOptions(Service.Presentation.ProductResponseModel.ProductOffer cslFareLock, List<DisplayPrice> prices, List<MOBItem> catalogItems = null, int appId = 0, string appVersion = "")
        {
            FareLock shopFareLock = new FareLock();
            var total = prices.FirstOrDefault(p => p.Type.Equals("Total", StringComparison.OrdinalIgnoreCase));
            double flightPrice = total != null ? (double)total.Amount : 0;
            string currency = total != null ? total.Currency : string.Empty;
            if (cslFareLock != null && cslFareLock.Offers != null && cslFareLock.Offers.Count > 0)
            {
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
                                shopFareLock.FareLockProducts.Add(flProd);
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
                        shopFareLock.FareLockPurchaseButtonAmountDisplayText = TopHelper.FormatAmountForDisplay(flightPrice.ToString(), ci, false);

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

        private async System.Threading.Tasks.Task SetELFUpgradeMsg(MOBSHOPAvailability availability, string productCode, MOBRequest request, Session session)
        {
            if (_configuration.GetValue<bool>("ByPassSetUpUpgradedFromELFMessages"))
            {
                if (availability?.Reservation?.IsUpgradedFromEntryLevelFare ?? false)
                {
                    if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                    if (_shoppingUtility.IsNonRefundableNonChangable(productCode))
                    {
                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(await BuildUpgradeFromNonRefuNonChanInfoMessage(request, session));
                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                    }
                    else
                    {
                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(ConfigUtility.BuildUpgradeFromELFInfoMessage(request.Application.Id));
                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                    }
                }

            }
        }
        public static List<MOBMobileCMSContentMessages> GetSDLMessageFromList(List<CMSContentMessage> list, string title)
        {
            List<MOBMobileCMSContentMessages> listOfMessages = new List<MOBMobileCMSContentMessages>();
            list?.Where(l => l.Title.ToUpper().Equals(title.ToUpper()))?.ForEach(i => listOfMessages.Add(new MOBMobileCMSContentMessages()
            {
                Title = i.Title,
                ContentFull = i.ContentFull,
                HeadLine = i.Headline,
                ContentShort = i.ContentShort,
                LocationCode = i.LocationCode
            }));

            return listOfMessages;
        }

        private async Task<InfoWarningMessages> BuildUpgradeFromNonRefuNonChanInfoMessage(MOBRequest request, Session session)
        {
            List<CMSContentMessage> lstMessages = await GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");

            var message = GetSDLMessageFromList(lstMessages, _configuration.GetValue<string>("UpgradedFromNonRefuNonChanTextWithHtml")).FirstOrDefault();
            var infoWarningMessages = new InfoWarningMessages
            {
                Order = MOBINFOWARNINGMESSAGEORDER.INHIBITBOOKING.ToString(), // Using existing order for sorting. 
                IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString(),
                HeaderMessage = (request.Application.Id == 1) ? message.HeadLine : string.Empty,
                Messages = new List<string>
                {
                   (request.Application.Id==1)? message.ContentShort : message.ContentFull
                }
            };
            return infoWarningMessages;
        }

        private async Task<InfoWarningMessages> GetNonRefundableNonChangableInversionMessage(MOBRequest request, Session session)
        {
            List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
            var message = GetSDLStringMessageFromList(lstMessages, _configuration.GetValue<string>("NonRefundableNonChangableFareInversionMessage"));
            return BuildInfoWarningMessages(message);
        }

        private async Task<List<CMSContentMessage>> GetSDLContentByGroupName(MOBRequest request, string guid, string token, string groupName, string docNameConfigEntry, bool useCache = false)
        {
            CSLContentMessagesResponse response = null;

            //string s = United.Persist.FilePersist.Load<string>(GetConfigEntries(docNameConfigEntry), "MOBCSLContentMessagesResponse");
            string s = await _sessionHelperService.GetSession<string>(_configuration.GetValue<string>("docNameConfigEntry"), ObjectNames.MOBCSLContentMessagesResponseFullName, new List<string> { _configuration.GetValue<string>("docNameConfigEntry"), ObjectNames.MOBCSLContentMessagesResponseFullName }).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(s))
            {
                response = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(s);
            }

            if (response != null && response.Messages != null) { return response.Messages; }

            MOBCSLContentMessagesRequest sdlReqeust = new MOBCSLContentMessagesRequest
            {
                Lang = "en",
                Pos = "us",
                Channel = "mobileapp",
                Listname = new List<string>(),
                LocationCodes = new List<string>(),
                Groupname = groupName,
                Usecache = useCache
            };

            string jsonRequest = JsonConvert.SerializeObject(sdlReqeust);
            //string url = string.Format("{0}/message", _configuration.GetValue<string>("ServiceEndPointBaseUrl - CMSContentMessages"));


            #region//****Get Call Duration Code*******
            Stopwatch cslStopWatch = new Stopwatch();
            cslStopWatch.Reset();
            cslStopWatch.Start();
            #endregion//****Get Call Duration Code*******
            string action = "message";
            response = await _iCMSContentService.GetSDLContentByGroupName<CSLContentMessagesResponse>(token, action, jsonRequest, guid);
            //HttpHelper.Post(url, "application/json; charset=utf-8", token, jsonRequest);
            #region// 2 = cslStopWatch//****Get Call Duration Code*******
            if (cslStopWatch.IsRunning)
            {
                cslStopWatch.Stop();
            }
            string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

            #endregion//****Get Call Duration Code*******   

            if (response == null)
            {
                //logContext?.LogEntries?.Add(LogEntry.GetLogEntry<string>(guid, "GetSDLContentByGroupName", "CSL-CallError", request.Application.Id, request.Application.Version.Major, request.DeviceId, "Failed to deserialize CSL response"));
                _logger.LogError("GetSDLContentByGroupName - CSL-CallErro Exception {error} and SessionId {guid}", "Failed to deserialize CSL response", guid);

                return null;
            }

            if (response.Errors.Count > 0)
            {
                string errorMsg = String.Join(" ", response.Errors.Select(x => x.Message));
                //logContext?.LogEntries?.Add(LogEntry.GetLogEntry<string>(guid, "GetSDLContentByGroupName", "CSL-CallError", request.Application.Id, request.Application.Version.Major, request.DeviceId, errorMsg));
                _logger.LogError("GetSDLContentByGroupName - Exception {error} and SessionId {guid}", errorMsg, guid);
                return null;
            }

            if (response != null && (Convert.ToBoolean(response.Status) && response.Messages != null))
            {
                //FilePersist.Save<string>(GetConfigEntries(docNameConfigEntry), "MOBCSLContentMessagesResponse", jsonResponse);
                await _sessionHelperService.SaveSession<string>(JsonConvert.SerializeObject(response), GetConfigEntries(docNameConfigEntry), new List<string> { GetConfigEntries(docNameConfigEntry), ObjectNames.MOBCSLContentMessagesResponseFullName }, ObjectNames.MOBCSLContentMessagesResponseFullName).ConfigureAwait(false);
                //logContext?.LogEntries?.Add(LogEntry.GetLogEntry<MOBCSLContentMessagesResponse>(guid, "GetSDLContentByGroupName", "SDLResponse", request.Application.Id, request.Application.Version.Major, request.DeviceId, response));
                _logger.LogInformation("GetSDLContentByGroupName {Trace} and {guid}", response, guid);
            }

            return response.Messages;
        }
        private string GetConfigEntries(string configKey)
        {
            var configString = _configuration.GetValue<string>(configKey) ?? string.Empty;
            if (!string.IsNullOrEmpty(configString))
            {
                return configString;
            }
            return configString;
        }
        private static InfoWarningMessages BuildInfoWarningMessages(string message)
        {
            var infoWarningMessages = new InfoWarningMessages
            {
                Order = MOBINFOWARNINGMESSAGEORDER.BEFAREINVERSION.ToString(),
                IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString(),
                Messages = new List<string>
                {
                    message
                }
            };
            return infoWarningMessages;
        }

        private string GetSDLStringMessageFromList(List<CMSContentMessage> list, string title)
        {
            return list?.Where(x => x.Title.Equals(title))?.FirstOrDefault()?.ContentFull?.Trim();
        }
        private ShopRequest BuildShopPinDownDetailsRequest(MOBSHOPSelectUnfinishedBookingRequest request, string cartId = "")
        {

            var shopRequest = (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major) && request.SelectedUnfinishBooking.TravelerTypes != null) ?
                            _unfinishedBooking.BuildShopPinDownRequest(request.SelectedUnfinishBooking, request.MileagePlusAccountNumber, request.LanguageCode, request.Application.Id, request.Application.Version.Major, true) :
                            _unfinishedBooking.BuildShopPinDownRequest(request.SelectedUnfinishBooking, request.MileagePlusAccountNumber, request.LanguageCode);


            shopRequest.DisablePricingBySlice = _shoppingUtility.EnableRoundTripPricing(request.Application.Id, request.Application.Version.Major);
            shopRequest.DecodesOnTimePerfRequested = _configuration.GetValue<bool>("DecodesOnTimePerformance");
            shopRequest.DecodesRequested = _configuration.GetValue<bool>("DecodesRequested");
            shopRequest.IncludeAmenities = _configuration.GetValue<bool>("IncludeAmenities");
            shopRequest.CartId = cartId;
            if (_configuration.GetValue<bool>("EnableOmniChannelCartMVP1"))
            {
                shopRequest.DeviceId = request.DeviceId;
                shopRequest.LoyaltyPerson = new Service.Presentation.PersonModel.LoyaltyPerson();
                shopRequest.LoyaltyPerson.LoyaltyProgramMemberID = request.MileagePlusAccountNumber;
            }
            return shopRequest;
        }
        private void ValidateTravelersForCubaReason(List<MOBCPTraveler> travelersCSL, bool isCuba)
        {
            if (isCuba)
            {
                //var selectedTravelers = travelersCSL.Where(p => p.IsPaxSelected).ToList();
                //if (selectedTravelers == null || selectedTravelers.Count() == 0)
                //{
                ValidateTravelersCSLForCubaReason(travelersCSL);
                //}
                //else
                //{
                //    ValidateTravelersCSLForCubaReason(selectedTravelers);
                //    travelersCSL.Where(p => p != null && !p.IsPaxSelected).ToList().ForEach(p => p.Message = string.Empty);
                //}
            }
        }
        private bool IsCubaTravelerHasReason(MOBCPTraveler traveler)
        {
            return (traveler.CubaTravelReason != null && !string.IsNullOrEmpty(traveler.CubaTravelReason.Vanity));
        }
        private void ValidateTravelersCSLForCubaReason(List<MOBCPTraveler> travelersCSL)
        {
            if (travelersCSL != null && travelersCSL.Count > 0)
            {
                foreach (MOBCPTraveler traveler in travelersCSL)
                {
                    if (!IsCubaTravelerHasReason(traveler))
                    {
                        traveler.Message = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage");
                    }
                    else if (!string.IsNullOrEmpty(traveler.Message))
                    {
                        if (!string.IsNullOrEmpty(traveler.FirstName) && !string.IsNullOrEmpty(traveler.LastName) && !string.IsNullOrEmpty(traveler.GenderCode) && !string.IsNullOrEmpty(traveler.BirthDate))
                        {
                            traveler.Message = null;
                        }
                    }
                }
            }
        }
        private async System.Threading.Tasks.Task BuildResponseForRegisterTraveler(MOBRegisterTravelersRequest request, MOBSHOPReservation reservation, Session session, FlightReservationResponse flightReservationResponse)
        {
            Model.Shopping.Reservation bookingPathReservation = new Model.Shopping.Reservation();
            reservation.SessionId = request.SessionId;
            #region udpate alleligibletravelers to persist

            //bookingPathReservation = United.Persist.FilePersist.Load<United.Persist.Definition.Shopping.Reservation>(request.SessionId, bookingPathReservation.ObjectName);
            bookingPathReservation = await _sessionHelperService.GetSession<Model.Shopping.Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
            _travelerUtility.UpdateAllEligibleTravelersList(bookingPathReservation);
            ValidateTravelersForCubaReason(bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL, bookingPathReservation.IsCubaTravel);
            bookingPathReservation.ShopReservationInfo2.IsOmniCartSavedTrip = true;
            //FilePersist.Save<Model.Shopping.Reservation >(bookingPathReservation.SessionId, bookingPathReservation.ObjectName, bookingPathReservation);

            await _sessionHelperService.SaveSession<Model.Shopping.Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
            #endregion
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            shoppingCart.Flow = request.Flow;
            //shoppingCart = United.Persist.FilePersist.Load<MOBShoppingCart>(request.SessionId, shoppingCart.GetType().ToString());
            shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, shoppingCart.ObjectName, new List<string> { request.SessionId, shoppingCart.ObjectName }).ConfigureAwait(false);

            if (shoppingCart?.FormofPaymentDetails == null)
            {
                shoppingCart.FormofPaymentDetails = new MOBFormofPaymentDetails();
            }
            shoppingCart.FormofPaymentDetails.FormOfPaymentType = bookingPathReservation.FormOfPaymentType.ToString();
            if (shoppingCart.OmniCart == null)
                shoppingCart.OmniCart = new Model.Shopping.Cart();
            
                shoppingCart.OmniCart.NavigateToScreen = MOBNavigationToScreen.TRAVELOPTIONS.ToString();                       
                    
            if (ConfigUtility.IncludeTravelCredit(request.Application.Id, request.Application.Version.Major) && request.Flow == FlowType.BOOKING.ToString()
                    && !session.IsAward && (ConfigUtility.IsEnableFeature("EnableU4BCorporateBookingFFC", request.Application.Id, request.Application.Version.Major) ? true : !session.IsCorporateBooking))
            {
                await PreLoadTravelCredit(session.SessionId, shoppingCart, request, false, flightReservationResponse);
            }
            //FilePersist.Save<MOBShoppingCart>(request.SessionId, shoppingCart.GetType().ToString(), shoppingCart);
            await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, request.SessionId, new List<string> { request.SessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName).ConfigureAwait(false);

        }
        private List<MOBMobileCMSContentMessages> GetSDLContentMessages(List<CMSContentMessage> lstMessages, string title)
        {
            List<MOBMobileCMSContentMessages> messages = new List<MOBMobileCMSContentMessages>();
            messages.AddRange(ShopStaticUtility.GetSDLMessageFromList(lstMessages, title));
            return messages;
        }

        private async Task<TCLookupByFreqFlyerNumWithEligibleResponse> GetCSLTravelCredits(string sessionId, MOBRequest mobRequest, FOPResponse response, bool isLoadFromCSL, FlightReservationResponse flightReservationResponse = null)
        {
            TCLookupByFreqFlyerNumWithEligibleResponse lookupResponse = new TCLookupByFreqFlyerNumWithEligibleResponse();
            string url = "/ECD/TCLookupByFreqFlyerNumWithEligible";
            TCLookupByFreqFlyerNumWithEligibleRequest cslRequest = new TCLookupByFreqFlyerNumWithEligibleRequest();
            cslRequest.FreqFlyerNum = response?.Profiles?[0].Travelers.Find(item => item.IsProfileOwner).MileagePlus.MileagePlusId;
            if (String.IsNullOrEmpty(cslRequest.FreqFlyerNum))
                cslRequest.FreqFlyerNum = response.Reservation?.TravelersCSL?.FirstOrDefault(v => v.IsProfileOwner)?.MileagePlus?.MileagePlusId;
            // In the guest flow we will be able to show the travel credits based on the lastname/DOB 
            if (!_configuration.GetValue<bool>("EnablePreLoadForTCNonMember"))
            {
                if (String.IsNullOrEmpty(cslRequest.FreqFlyerNum))
                {
                    return lookupResponse;
                }
            }
            else
            {
                cslRequest.IsLoadFFCRWithCustomSearch = true;
                cslRequest.IsLoadFFCWithCustomSearch = true;
            }

            cslRequest.IsLoadETC = true;
            cslRequest.IsLoadFFC = true;
            cslRequest.IsLoadFFCR = true;

            Session session =await  _sessionHelperService.GetSession<Session>(sessionId, new Session().ObjectName, new List<string> { sessionId, new Session().ObjectName });
            var reservation = new Service.Presentation.ReservationModel.Reservation();
            reservation = await _sessionHelperService.GetSession<Service.Presentation.ReservationModel.Reservation>(sessionId, reservation.GetType().FullName, new List<string> { sessionId, reservation.GetType().FullName }).ConfigureAwait(false);
            if (reservation == null || (!_configuration.GetValue<bool>("DisableMMOptionsReloadInBackButtonFixToggle") && isLoadFromCSL))
            {
                if (flightReservationResponse == null)
                {
                    var cartInfo = await _travelerUtility.GetCartInformation(sessionId, mobRequest.Application, mobRequest.DeviceId, session.CartId, session.Token);
                    reservation = cartInfo.Reservation;
                }
                else
                {
                    reservation = flightReservationResponse.Reservation;
                }
                await _sessionHelperService.SaveSession<Service.Presentation.ReservationModel.Reservation>(reservation, session.SessionId, new List<string> { session.SessionId, reservation.GetType().FullName }, reservation.GetType().FullName).ConfigureAwait(false);
            }
            cslRequest.Reservation = reservation;
            cslRequest.CallingService = new ServiceClient();
            cslRequest.CallingService.Requestor = new Requestor();
            cslRequest.CallingService.AccessCode = _configuration.GetValue<string>("TravelCreditAccessCode").ToString();
            cslRequest.CallingService.Requestor.AgentAAA = "HQS";
            cslRequest.CallingService.Requestor.AgentSine = "UA";
            cslRequest.CallingService.Requestor.ApplicationSource = "Mobile";
            cslRequest.CallingService.Requestor.Device = new Service.Presentation.CommonModel.Device();
            cslRequest.CallingService.Requestor.Device.LNIATA = "Mobile";
            cslRequest.CallingService.Requestor.DutyCode = "SU";
            string jsonRequest = JsonConvert.SerializeObject(cslRequest);
            string jsonResponse = await PostAndLog(sessionId, url, jsonRequest, mobRequest, "GetCSLTravelCredits", "TCLookupByFreqFlyerNumWithEligible");

            lookupResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<TCLookupByFreqFlyerNumWithEligibleResponse>(jsonResponse);
            await _sessionHelperService.SaveSession(lookupResponse, session.SessionId, new List<string> { session.SessionId, lookupResponse.GetType().FullName }, lookupResponse.GetType().FullName).ConfigureAwait(false);
            return lookupResponse;
        }
        private async Task<string> GetSessionToken(string sessionId)
        {
            var session = new Session();
            //session = FilePersist.Load<Session>(sessionId, session.ObjectName);
            session = await _sessionHelperService.GetSession<Session>(sessionId, new Session().ObjectName, new List<string> { sessionId, new Session().ObjectName }).ConfigureAwait(false);
            return session.Token;
        }
        private async Task<string> PostAndLog(string sessionId, string path, string jsonRequest, MOBRequest mobRequest, string logAction, string cslAction)
        {
            cslAction = cslAction ?? string.Empty;
            logAction = logAction ?? string.Empty;
            logAction = cslAction + " - " + logAction;
            var token = await GetSessionToken(sessionId);

            var jsonResponse = await _paymentService.GetEligibleFormOfPayments(token, path, jsonRequest, sessionId);

            //HttpHelper.Post(url, "application/json; charset=utf-8", token, jsonRequest);


            if (string.IsNullOrEmpty(jsonResponse))
                throw new Exception("Service did not return any reponse");

            return jsonResponse;
        }
        private List<MOBMobileCMSContentMessages> SwapTitleAndLocation(List<MOBMobileCMSContentMessages> cmsList)
        {
            foreach (var item in cmsList)
            {
                string location = item.LocationCode;
                item.LocationCode = item.Title;
                item.Title = location;
            }
            return cmsList;
        }

        private List<Model.Shopping.FormofPayment.MOBFOPTravelCredit> LoadCSLResponse(TCLookupByFreqFlyerNumWithEligibleResponse cslResponse, FOPResponse response, List<MOBMobileCMSContentMessages> lookUpMessages)
        {
            List<Model.Shopping.FormofPayment.MOBFOPTravelCredit> travelCredits = new List<Model.Shopping.FormofPayment.MOBFOPTravelCredit>();

            if (cslResponse == null)
            {
                return travelCredits;
            }
            var etc = cslResponse.ETCCertificates;
            var ffc = cslResponse.FFCCertificates;
            var ffcr = cslResponse.FFCRCertificates;

            _travelerUtility.AddETCToTC(travelCredits, etc, false);
            _travelerUtility.AddFFCandFFCR(response.Reservation.TravelersCSL, travelCredits, ffc, lookUpMessages, true, false);
            _travelerUtility.AddFFCandFFCR(response.Reservation.TravelersCSL, travelCredits, ffcr, lookUpMessages, false, false);
            return travelCredits;
        }

        private async System.Threading.Tasks.Task PreLoadTravelCredit(string sessionId, MOBShoppingCart shoppingCart, MOBRequest request, bool isLoadFromCSL = false, FlightReservationResponse flightReservationResponse = null)
        {
            try
            {
                //Session session = FilePersist.Load<Session>(sessionId, new Session().ObjectName);
                Session session = await _sessionHelperService.GetSession<Session>(sessionId, new Session().ObjectName, new List<string> { sessionId, new Session().ObjectName });

                var bookingPathReservation = new Mobile.Model.Shopping.Reservation();
                var trupleRes = await _travelerUtility.LoadBasicFOPResponse(session, bookingPathReservation);
                var response = trupleRes.Item1;
                bookingPathReservation = trupleRes.bookingPathReservation;

                if (sessionId == null)
                {
                    throw new Exception("empty session");
                }
                if (response?.ShoppingCart?.FormofPaymentDetails == null)
                {
                    response.ShoppingCart.FormofPaymentDetails = new MOBFormofPaymentDetails();
                }

                var travelCreditDetails = new Model.Shopping.FormofPayment.MOBFOPTravelCreditDetails();

                TravelCredit travelCredit = new TravelCredit();

                List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, sessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                travelCreditDetails.LookUpMessages = GetSDLContentMessages(lstMessages, "RTI.TravelCertificate.LookUpTravelCredits");
                TCLookupByFreqFlyerNumWithEligibleResponse cslres = new TCLookupByFreqFlyerNumWithEligibleResponse();
                cslres = await GetCSLTravelCredits(sessionId, request, response, isLoadFromCSL, flightReservationResponse);
                travelCreditDetails.ReviewMessages = GetSDLContentMessages(lstMessages, "RTI.TravelCertificate.ReviewTravelCredits");
                travelCreditDetails.ReviewMessages.AddRange(GetSDLContentMessages(lstMessages, "RTI.TravelCertificate.AlertTravelCredits"));
                SwapTitleAndLocation(travelCreditDetails.ReviewMessages);
                SwapTitleAndLocation(travelCreditDetails.LookUpMessages);
                travelCreditDetails.ReviewMessages.AddRange(travelCreditDetails.LookUpMessages);
                travelCreditDetails.TravelCredits = LoadCSLResponse(cslres, response, travelCreditDetails.LookUpMessages);
                if (response.ShoppingCart.FormofPaymentDetails.TravelCreditDetails?.TravelCredits?.Count > 0)
                {
                    travelCreditDetails.TravelCredits.ForEach(tc => tc.IsApplied = (response.ShoppingCart.FormofPaymentDetails.TravelCreditDetails.TravelCredits.Exists(existingTC => existingTC.IsApplied && existingTC.PinCode == tc.PinCode)));
                }
                travelCreditDetails.TravelCredits = travelCreditDetails.TravelCredits.OrderBy(x => DateTime.Parse(x.ExpiryDate)).ToList();
                var nameWaiverMatchMessage = ShopStaticUtility.GetSDLMessageFromList(lstMessages, "RTI.TravelCertificate.LookUpTravelCredits.Alert.NameMatchWaiver");
                travelCreditDetails.NameWaiverMatchMessage = nameWaiverMatchMessage.Count() > 0 ? nameWaiverMatchMessage?[0].ContentFull.ToString() : null;

                travelCreditDetails.TravelCreditSummary = string.Empty;

                if (_configuration.GetValue<bool>("EnableTravelCreditSummary"))
                {
                    var travelCreditCount = travelCreditDetails?.TravelCredits?.Count ?? 0;

                    if (travelCreditCount > 0)
                    {
                        var travelCreditSummary = _configuration.GetValue<string>("TravelCreditSummary");
                        var pluralChar = travelCreditCount > 1 ? "s" : string.Empty;

                        travelCreditDetails.TravelCreditSummary = response.ShoppingCart?.Products?.FirstOrDefault().Code != "FLK" ? string.Format(travelCreditSummary, travelCreditCount, pluralChar) : string.Empty;
                    }
                }

                shoppingCart.FormofPaymentDetails.TravelCreditDetails = travelCreditDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError("PreLoadTravelCredits {@Exception}", JsonConvert.SerializeObject(ex));
            }
        }
        private async System.Threading.Tasks.Task MapCslTravelersToMOBTravelers(MOBRegisterTravelersRequest travelerRequest, FlightReservationResponse flightReservationResponse, MOBSHOPSelectUnfinishedBookingRequest request, MOBSHOPReservation mobReservation, Session session)
        {
            travelerRequest.Flow = FlowType.BOOKING.ToString();
            travelerRequest.Travelers = new List<MOBCPTraveler>();
            int paxIndex = 0;
            travelerRequest.Application = request.Application;
            travelerRequest.DeviceId = request.DeviceId;
            travelerRequest.SessionId = request.SessionId;
            travelerRequest.TransactionId = request.TransactionId;
            travelerRequest.CartId = request.CartId;
            travelerRequest.IsOmniCartSavedTripFlow = true;

            bool isExtraSeatFeatureEnabledForOmniCart = await _shoppingUtility.IsExtraSeatFeatureEnabledForOmniCart(travelerRequest.Application.Id, travelerRequest.Application?.Version?.Major, session.CatalogItems).ConfigureAwait(false);
            var extraSeatPassengerIndex = _travelerUtility.GetTravelerNameIndexForExtraSeat(isExtraSeatFeatureEnabledForOmniCart, flightReservationResponse.Reservation.Services);

            foreach (var cslTraveler in flightReservationResponse.Reservation.Travelers)
            {
                MOBCPTraveler traveler = new MOBCPTraveler();

                if (isExtraSeatFeatureEnabledForOmniCart && extraSeatPassengerIndex?.Count > 0 && !string.IsNullOrEmpty(cslTraveler?.Person?.Key) && extraSeatPassengerIndex.Contains(cslTraveler.Person.Key))
                {
                    string travelerMiddleInitial = !string.IsNullOrEmpty(cslTraveler.Person?.MiddleName) ? " " + cslTraveler.Person.MiddleName.Substring(0, 1) : string.Empty;
                    string travelerSuffix = !string.IsNullOrEmpty(cslTraveler.Person?.Suffix) ? " " + cslTraveler.Person.Suffix : string.Empty;

                    traveler.FirstName = _configuration.GetValue<string>("ExtraSeatName");
                    traveler.LastName = " (" + _travelerUtility.RemoveExtraSeatCodeFromGivenName(cslTraveler.Person?.GivenName) + travelerMiddleInitial + " " + cslTraveler.Person?.Surname + travelerSuffix + ")";
                    traveler.IsExtraSeat = true;

                    MOBExtraSeat extraSeatData = new MOBExtraSeat();
                    extraSeatData.Purpose = _travelerUtility.GetExtraSeatReason(cslTraveler.Person.Key, isExtraSeatFeatureEnabledForOmniCart, flightReservationResponse.Reservation.Services);
                    traveler.ExtraSeatData = extraSeatData;
                }
                else
                {
                    traveler.FirstName = cslTraveler?.Person?.GivenName;
                    traveler.LastName = cslTraveler?.Person?.Surname;
                }
                traveler.BirthDate = cslTraveler?.Person?.DateOfBirth;
                traveler.TravelerTypeCode = cslTraveler?.Person?.Type;
                traveler.TravelerNameIndex = cslTraveler?.Person?.Key;
                traveler.MiddleName = cslTraveler?.Person?.MiddleName;
                traveler.CountryOfResidence = cslTraveler?.Person?.CountryOfResidence?.CountryCode;
                traveler.Nationality = cslTraveler?.Person?.Nationality != null && cslTraveler?.Person?.Nationality.Count > 0 ? cslTraveler?.Person?.Nationality.First().CountryCode : "";
                if (cslTraveler?.Person?.Documents != null)
                {
                    traveler.GenderCode = cslTraveler?.Person?.Documents?.First().Sex;
                    traveler.RedressNumber = cslTraveler?.Person?.Documents?.First().RedressNumber;
                    traveler.KnownTravelerNumber = cslTraveler?.Person?.Documents?.First().KnownTravelerNumber;
                }

                traveler.Suffix = cslTraveler?.Person?.Suffix;
                if (cslTraveler?.Person?.Contact?.PhoneNumbers != null && cslTraveler?.Person?.Contact?.PhoneNumbers.Count > 0)
                {
                    traveler.Phones = new List<MOBCPPhone>();
                    traveler.Phones.Add(new MOBCPPhone
                    {
                        AreaNumber = cslTraveler?.Person?.Contact?.PhoneNumbers.First().AreaCityCode,
                        CountryCode = cslTraveler?.Person?.Contact?.PhoneNumbers.First().CountryAccessCode,
                        PhoneNumber = cslTraveler?.Person?.Contact?.PhoneNumbers.First().PhoneNumber
                    });
                }
                if (cslTraveler?.Person?.Contact?.Emails != null && cslTraveler?.Person?.Contact?.Emails.Count > 0)
                {
                    traveler.EmailAddresses = new List<MOBEmail>();
                    traveler.EmailAddresses.Add(new MOBEmail
                    {
                        EmailAddress = cslTraveler?.Person?.Contact?.Emails.First().Address
                    });
                }
                traveler.PaxIndex = paxIndex;
                traveler.PaxID = traveler.PaxIndex + 1;
                await AssignSpecialRequests(mobReservation, flightReservationResponse, cslTraveler, traveler).ConfigureAwait(false);
                AssignSpecialMeals(mobReservation, flightReservationResponse, cslTraveler, traveler);
                AssignCubaSpecialReasons(mobReservation, flightReservationResponse, cslTraveler, traveler);
                AssignFrequentFlyerNumber(mobReservation, flightReservationResponse, cslTraveler, traveler);
                AssignServiceAnimal(mobReservation, flightReservationResponse, cslTraveler, traveler);
                traveler.PTCDescription = GeneralHelper.GetPaxDescriptionByDOB(traveler.BirthDate, mobReservation.Trips[0].FlattenedFlights[0].Flights[0].DepartDate);
                await RegisterNewTravelerValidateMPMisMatch(traveler, travelerRequest, travelerRequest.SessionId, travelerRequest.CartId, session.Token);
                paxIndex++;
                //TODO: Special meals request
                travelerRequest.Travelers.Add(traveler);
            }
        }


        public async Task<string> GetDPAnonymousToken(int applicationID, string deviceId, string transactionId, string appVersion, string WebConfigSessionID, Session persistToken, bool SaveToPersist = true, bool SaveToDataBase = false, string mpNumber = "", string customerID = "")
        {
            string token = string.Empty;
            if (applicationID != 0)
            {
                var DpReqObj = _mileagePlusTFACSL.GetDPRequestObject(applicationID, deviceId);

                Foundations.Practices.Framework.Security.DataPower.Models.DpToken dpTokenResponse = null;
                List<LogEntry> _dpTokenlogEntries = new List<LogEntry>();
                string request = string.Empty;
                string applicationSessionId = "";
                try
                {
                    applicationSessionId = string.IsNullOrWhiteSpace(WebConfigSessionID) ? persistToken.SessionId : WebConfigSessionID;
                }
                catch { }


                try
                {
                    #region Log Request
                    try
                    {

                        _dpTokenlogEntries.Add(new LogEntry { Guid = applicationSessionId, Action = "Get_Anonymous_DP_Token", MessageType = "Reqeust", ApplicationID = applicationID, AppVersion = appVersion, DeviceID = deviceId, Message = JsonConvert.SerializeObject(DpReqObj) });

                        _logger.LogInformation("Get_Anonymous_DP_Token Reqeust {DpReqObj} , ApplicationID {applicationID} , AppVersion {appVersion}, DeviceId {deviceId}", JsonConvert.SerializeObject(DpReqObj), applicationID, appVersion, deviceId);

                    }
                    catch { }
                    #endregion

                    // Acquire anonymous token

                    dpTokenResponse = _dataPowerGateway.AcquireAnonymousToken(DpReqObj.ClientId, DpReqObj.ClientSecret, DpReqObj.Scope, DpReqObj.UserType, null, null, DpReqObj.EndUserAgentIP, DpReqObj.EndUserAgentId);
                    if (dpTokenResponse != null)
                    {
                        token = dpTokenResponse.TokenType + " " + dpTokenResponse.AccessToken;
                        if (SaveToPersist)
                        {
                            if (!string.IsNullOrWhiteSpace(WebConfigSessionID))// Flight Status FlightStatus.CSLToken
                            {
                                United.Persist.Definition.FlightStatus.CSLToken CSLpersistToken = null;
                                CSLpersistToken = new Persist.Definition.FlightStatus.CSLToken();
                                CSLpersistToken.InsertedDateTime = DateTime.Now;

                                CSLpersistToken.Duration = Convert.ToInt32(dpTokenResponse.ExpiresIn) - Convert.ToInt32(_configuration.GetValue<string>("CSSTokenExpireLimitValueToMinusFromActualDuration").ToString());
                                CSLpersistToken.ExpirationTime = DateTime.Now.AddSeconds(CSLpersistToken.Duration);
                                CSLpersistToken.Token = token;

                                await _sessionHelperService.SaveSession<United.Persist.Definition.FlightStatus.CSLToken>(CSLpersistToken, WebConfigSessionID, new List<string> { WebConfigSessionID, CSLpersistToken.ObjectName }, CSLpersistToken.ObjectName).ConfigureAwait(false);

                            }
                            if (persistToken != null)// For Shopping.Session
                            {
                                persistToken.Token = token;
                                persistToken.IsTokenExpired = false;
                                persistToken.IsTokenAuthenticated = false; // As this toke in annonymous token
                                persistToken.TokenExpirationValueInSeconds = Convert.ToDouble(dpTokenResponse.ExpiresIn);
                                persistToken.TokenExpireDateTime = DateTime.Now.AddSeconds(Convert.ToDouble(dpTokenResponse.ExpiresIn));

                                await _sessionHelperService.SaveSession<Session>(persistToken, persistToken.SessionId, new List<string> { persistToken.SessionId, persistToken.ObjectName }, persistToken.ObjectName).ConfigureAwait(false);

                            }
                        }
                        if (SaveToDataBase)// Added for Promotions Hot Fix
                        {
                            //Utility.UpdateMileagePlusCSSToken(mpNumber, deviceId, applicationID, appVersion, token,
                            //    true, DateTime.Now.AddSeconds(Convert.ToDouble(dpTokenResponse.ExpiresIn)),
                            //    Convert.ToDouble(dpTokenResponse.ExpiresIn), false, Convert.ToInt64(customerID.Trim()));

                            await _mileagePlusCSSTokenService.UpdateMileagePlusCSSToken(persistToken?.SessionId, transactionId, mpNumber, deviceId, applicationID, appVersion, token,
                                true, DateTime.Now.AddSeconds(Convert.ToDouble(dpTokenResponse.ExpiresIn)),
                                Convert.ToDouble(dpTokenResponse.ExpiresIn), false, Convert.ToInt64(customerID.Trim()));
                        }
                    }
                    else
                    {
                        _logger.LogError("Get_Anonymous_DP_Token Data Power call returned Null! SessionId {applicationSessionId} , ApplicationID {applicationID} , AppVersion {appVersion}, DeviceId {deviceId}", applicationSessionId, applicationID, appVersion, deviceId);

                        //if (string.IsNullOrEmpty(_configuration.GetValue<string>("LogAnonymousDPToken")) && Convert.ToBoolean(_configuration.GetValue<string>("LogAnonymousDPToken")))
                        //{
                        //    // System.Threading.Tasks.Task.Factory.StartNew(() => Authentication.Write(_dpTokenlogEntries));

                        //    //System.Threading.Tasks.Task.Factory.StartNew(() => Write(_dpTokenlogEntries));
                        //}
                    }

                    #region Log Response
                    try
                    {
                        _dpTokenlogEntries.Add(new LogEntry { Guid = applicationSessionId, Action = "Get_Anonymous_DP_Token", MessageType = "Exception", ApplicationID = applicationID, AppVersion = appVersion, DeviceID = deviceId, Message = "Data Power call returned Null!" });

                        _logger.LogInformation("Get_Anonymous_DP_Token Response {dpTokenResponse}, SessionId {applicationSessionId} , ApplicationID {applicationID} , AppVersion {appVersion}, DeviceId {deviceId}", JsonConvert.SerializeObject(dpTokenResponse), applicationSessionId, applicationID, appVersion, deviceId);

                        //if (!string.IsNullOrEmpty(_configuration.GetValue<string>("LogAnonymousDPToken")) && Convert.ToBoolean(_configuration.GetValue<string>("LogAnonymousDPToken")))
                        //{
                        //    //TODO: Complete Migration

                        //    // System.Threading.Tasks.Task.Factory.StartNew(() => Authentication.Write(_dpTokenlogEntries));
                        //    System.Threading.Tasks.Task.Factory.StartNew(() => Write(_dpTokenlogEntries));

                        //}
                    }
                    catch { }
                    #endregion

                }
                catch (Exception ex)
                {
                    #region Log Exception


                    _dpTokenlogEntries.Add(new LogEntry { Guid = applicationSessionId, Action = "Get_Anonymous_DP_Token", MessageType = "Exception", ApplicationID = applicationID, AppVersion = appVersion, DeviceID = deviceId, Message = ex.Message.ToString() + " :: " + ex.StackTrace.ToString() });
                    //  _logger.LogError("Get_Anonymous_DP_Token Exception {" + ex.Message.ToString() + " :: " + ex.StackTrace.ToString() + "} , ApplicationID {applicationID} , AppVersion {appVersion}, DeviceId {deviceId}", applicationID, appVersion, deviceId);


                    //if (string.IsNullOrEmpty(_configuration.GetValue<string>("LogAnonymousDPToken")) && Convert.ToBoolean(_configuration.GetValue<string>("LogAnonymousDPToken")))
                    //{
                    //    //TODO: Complete Migration

                    //    //System.Threading.Tasks.Task.Factory.StartNew(() => Authentication.Write(_dpTokenlogEntries));
                    //    System.Threading.Tasks.Task.Factory.StartNew(() => Write(_dpTokenlogEntries));

                    //}
                    throw ex;
                    #endregion

                }

            }
            return token;
        }
    

        public void LogNoPersistentTokenInCSLResponseForVormetricPayment(string sessionId, string Message = "Unable to retieve PersistentToken")
        {

            //_logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, _action, "PERSISTENTTOKENNOTFOUND", _application.Id, _application.Version.Major, _deviceId, Message, false, true));
            _logger.LogWarning("PERSISTENTTOKENNOTFOUND {Message} and {sessionId}", Message, sessionId);

            //No need to block the flow as we are calling DataVault for Persistent Token during the final payment
            //throw new System.Exception(ConfigurationManager.AppSettings["VormetricExceptionMessage"]);
        }
         
        private async Task<MOBCPTraveler> RegisterNewTravelerValidateMPMisMatch(MOBCPTraveler registerTraveler, MOBRequest mobRequest, string sessionID, string cartId, string token)
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
                //Add a toggle mention about bug number.

                if (_configuration.GetValue<bool>("EnableValidateNewMPNumberIssueFix16788"))
                {
                    savedTravelerProfileRequest.SessionId = sessionID;
                    //+ "_GetProfileOwner_" + registerTraveler.AirRewardPrograms[0].MemberId;
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

                    registerTraveler.Message = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage").ToString();

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
                    if (Regex.IsMatch(registerTraveler.AirRewardPrograms[0].MemberId.ToString(), @"^\d$"))
                    {
                        throw new MOBUnitedException(_configuration.GetValue<string>("ValidateLoyalityNumberErrorMessage").ToString());
                    }
                }
            }
            #endregion
            return registerTraveler;
        }

        private void AssignServiceAnimal(MOBSHOPReservation mobReservation, FlightReservationResponse flightReservationResponse, Service.Presentation.ReservationModel.Traveler cslTraveler, MOBCPTraveler mobTraveler)
        {
            if (flightReservationResponse.Reservation != null
                && flightReservationResponse.Reservation.Services != null
                && mobReservation?.ShopReservationInfo2?.SpecialNeeds != null
                && mobReservation?.ShopReservationInfo2?.SpecialNeeds?.ServiceAnimals != null
               )
            {
                if (mobTraveler.SelectedSpecialNeeds == null)
                {
                    mobTraveler.SelectedSpecialNeeds = new List<United.Mobile.Model.Common.SSR.TravelSpecialNeed>();
                }
                var specialNeedsList = flightReservationResponse.Reservation.Services.Where(splNeeds => splNeeds.TravelerNameIndex == cslTraveler?.Person?.Key && splNeeds.Key == "SSR");
                if (specialNeedsList != null)
                {
                    foreach (var specialNeed in specialNeedsList)
                    {
                        var serviceAnimals = mobReservation?.ShopReservationInfo2?.SpecialNeeds?.ServiceAnimals.Where(sn => sn.Code.Equals(specialNeed.Code) && sn.DisplayDescription.Equals(specialNeed.Description));
                        if (serviceAnimals != null)
                        {
                            mobTraveler.SelectedSpecialNeeds.AddRange(serviceAnimals.ToList());
                        }
                    }
                }
            }
        }
        private void AssignFrequentFlyerNumber(MOBSHOPReservation mobReservation, FlightReservationResponse flightReservationResponse, Service.Presentation.ReservationModel.Traveler cslTraveler, MOBCPTraveler mobTraveler)
        {
            if (flightReservationResponse.Reservation != null
                && flightReservationResponse.Reservation.Travelers != null
                && mobReservation?.RewardPrograms != null)
            {
                var frequentFlyTraveler = flightReservationResponse.Reservation.Travelers.FirstOrDefault(traveler => traveler?.Person?.Key == cslTraveler?.Person?.Key);
                if (!string.IsNullOrEmpty(frequentFlyTraveler?.LoyaltyProgramProfile?.LoyaltyProgramCarrierCode))
                {
                    var selectedAirrewardProgram = mobReservation.RewardPrograms.FirstOrDefault(rp => rp.Type == frequentFlyTraveler.LoyaltyProgramProfile.LoyaltyProgramCarrierCode);
                    mobTraveler.AirRewardPrograms = new List<United.Mobile.Model.Common.MOBBKLoyaltyProgramProfile>();
                    mobTraveler.AirRewardPrograms.Add(new United.Mobile.Model.Common.MOBBKLoyaltyProgramProfile
                    {
                        CarrierCode = selectedAirrewardProgram?.Type,
                        ProgramId = selectedAirrewardProgram?.ProgramID,
                        ProgramName = selectedAirrewardProgram?.Description,
                        MemberId = frequentFlyTraveler.LoyaltyProgramProfile.LoyaltyProgramMemberID
                    });
                }
            }
        }
        private void AssignCubaSpecialReasons(MOBSHOPReservation mobReservation, FlightReservationResponse flightReservationResponse, Service.Presentation.ReservationModel.Traveler cslTraveler, MOBCPTraveler mobTraveler)
        {
            if (flightReservationResponse.Reservation != null
                && flightReservationResponse.Reservation.Services != null
                && mobReservation?.CubaTravelInfo?.TravelReasons != null)
            {
                var cubaSpecialReasons = flightReservationResponse.Reservation.Services.Where(splNeeds => splNeeds.TravelerNameIndex == cslTraveler?.Person?.Key && splNeeds.Key == "SSR" && splNeeds.Code == "RFTV");
                if (cubaSpecialReasons != null)
                {
                    var cubaSpecialReasonCode = cubaSpecialReasons.Select(sn => sn.Description).First();
                    mobTraveler.CubaTravelReason = new MOBCPCubaSSR
                    {
                        Vanity = cubaSpecialReasonCode
                    };
                }
            }
        }
        private void AssignSpecialMeals(MOBSHOPReservation mobReservation, FlightReservationResponse flightReservationResponse, Service.Presentation.ReservationModel.Traveler cslTraveler, MOBCPTraveler mobTraveler)
        {
            if (flightReservationResponse.Reservation != null
                && flightReservationResponse.Reservation.Services != null
                && mobReservation?.ShopReservationInfo2?.SpecialNeeds != null
                && mobReservation?.ShopReservationInfo2?.SpecialNeeds?.SpecialMeals != null)
            {
                if (mobTraveler.SelectedSpecialNeeds == null)
                {
                    mobTraveler.SelectedSpecialNeeds = new List<United.Mobile.Model.Common.SSR.TravelSpecialNeed>();
                }
                var specialNeedsList = flightReservationResponse.Reservation.Services.Where(splNeeds => splNeeds.TravelerNameIndex == cslTraveler?.Person?.Key).Select(sn => sn.Code).ToList();
                var specialMeals = mobReservation?.ShopReservationInfo2?.SpecialNeeds?.SpecialMeals.Where(sn => specialNeedsList.Contains(sn.Code));
                if (specialMeals != null)
                {
                    mobTraveler.SelectedSpecialNeeds.AddRange(specialMeals.ToList());
                }
            }
        }
        private async System.Threading.Tasks.Task AssignSpecialRequests(MOBSHOPReservation mobReservation, FlightReservationResponse flightReservationResponse, Service.Presentation.ReservationModel.Traveler cslTraveler, MOBCPTraveler mobTraveler)
        {
            if (!_configuration.GetValue<bool>("DisabledNotAllowTravelerTypeInfantSSR") && _configuration.GetValue<string>("NotAllowTravelerTypeInfantSSR").Contains(cslTraveler?.Person?.Type))
            {
                return;
            }

            if (flightReservationResponse.Reservation != null
                && flightReservationResponse.Reservation.Services != null
                && mobReservation?.ShopReservationInfo2?.SpecialNeeds != null
                && mobReservation?.ShopReservationInfo2?.SpecialNeeds?.SpecialRequests != null
               )
            {
                if (mobTraveler.SelectedSpecialNeeds == null)
                {
                    mobTraveler.SelectedSpecialNeeds = new List<United.Mobile.Model.Common.SSR.TravelSpecialNeed>();
                }
                var wheelChairCodes = new HashSet<string> { "WCMP", "WCLB", "WCBW", "WCBD" };
                var specialNeedsList = flightReservationResponse.Reservation.Services.Where(splNeeds => splNeeds.TravelerNameIndex == cslTraveler?.Person?.Key).Select(sn => sn.Code).ToList();
                if (_configuration.GetValue<bool>("EnableFeatureSettingsChanges")
                           ? await _featureSettings.GetFeatureSettingValue("EnableOmincartWCFix").ConfigureAwait(false)
                           : !_configuration.GetValue<bool>("DisableOmincartWCFix") && specialNeedsList != null)
                { 
                    foreach (var specialNeedCode in specialNeedsList)
                    {
                        if (wheelChairCodes.Contains(specialNeedCode))
                        {
                            var wheelChairSpecialNeed = mobReservation?.ShopReservationInfo2?.SpecialNeeds?.SpecialRequests?.FirstOrDefault(sn => sn.Code == _configuration.GetValue<string>("SSRWheelChairDescription"));
                            if (wheelChairSpecialNeed != null && wheelChairSpecialNeed.SubOptions != null)
                            {
                                wheelChairSpecialNeed.SubOptions = wheelChairSpecialNeed?.SubOptions?.Where(s => s.Code == specialNeedCode).ToList();
                                mobTraveler.SelectedSpecialNeeds.Add(wheelChairSpecialNeed);
                            }
                        }
                    }
                }
                var specialNeeds = mobReservation?.ShopReservationInfo2?.SpecialNeeds?.SpecialRequests.Where(sn => specialNeedsList.Contains(sn.Code));
                if (specialNeeds != null && specialNeedsList != null)
                {
                    mobTraveler.SelectedSpecialNeeds.AddRange(specialNeeds.ToList());
                }

            }
        }

        private bool SpecialNeedThatNeedRegisterDescription(string specialNeedCode)
        {
            if (string.IsNullOrWhiteSpace(specialNeedCode))
                return false;

            // we don't put a safe guard around this because we want this to fail if the list doesn't exist

            var listOfSpecialNeedsThatNeedRegisterDesc = new HashSet<string>(_configuration.GetValue<string>("SpecialNeedsThatNeedRegisterDescriptions").Split('|'));

            return listOfSpecialNeedsThatNeedRegisterDesc != null && listOfSpecialNeedsThatNeedRegisterDesc.Any() && listOfSpecialNeedsThatNeedRegisterDesc.Contains(specialNeedCode);
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
        private async Task<(RegisterTravelersRequest regTravelersRequest, MOBRegisterTravelersRequest request)> GetRegisterTravelerRequest(MOBRegisterTravelersRequest request, bool isRequireNationalityAndResidence, Model.Shopping.Reservation bookingPathReservation)
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
                    //session = FilePersist.Load<Session>(request.SessionId, session.ObjectName);
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

                    #region Special Needs

                    if (ConfigUtility.EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
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
                                    Code = sr.Code,
                                    TravelerNameIndex = flightTraveler.TravelerNameIndex,
                                    NumberInParty = 1,
                                    Description = SpecialNeedThatNeedRegisterDescription(sr.Code) ? sr.RegisterServiceDescription : null // Per PNR management team, only a few of special needs will need to be passed in with description
                                };

                                flightTraveler.SpecialServiceRequests.Add(specialRequest);
                            }
                        }
                    }

                    #endregion

                    ////Commented and Added By Santosh as part of Bug 104740:Error message is displayed during first time check-in for the Guest pax travelling to Cuba. -Santosh
                    //flightTraveler.Traveler.Person.Surname = IsAlphabets(traveler.LastName);
                    flightTraveler.Traveler.Person.Surname = traveler.LastName;
                    //flightTraveler.Traveler.Person.GivenName = IsAlphabets(traveler.FirstName);
                    flightTraveler.Traveler.Person.GivenName = traveler.FirstName;
                    //flightTraveler.Traveler.Person.MiddleName = IsAlphabets(traveler.MiddleName);
                    flightTraveler.Traveler.Person.MiddleName = traveler.MiddleName;
                    flightTraveler.Traveler.Person.Suffix = IsAlphabets(traveler.Suffix); //traveler.Suffix != null?traveler.Suffix.Replace(".", ""):"";
                    //Commenting Title as per Mahi if we pass title it is appending to name and failing when trying to refund EPU when purchasing PCU
                    //flightTraveler.Traveler.Person.Title = IsAlphabets(traveler.Title);   //traveler.Title != null?traveler.Title.Replace(".", ""):""; // As per Moni RegisterFormsOfPayment Call failing when passing dot with title value.
                    flightTraveler.Traveler.Person.Sex = traveler.GenderCode;

                    #region Nationality And Country Of Residence - Rajesh Settipalli
                    if (((ConfigUtility.EnableUnfinishedBookings(request) && isRequireNationalityAndResidence) ||
                               ConfigUtility.EnableUnfinishedBookings(request) == false
                          )
                        && ConfigUtility.IsEnabledNationalityAndResidence(false, request.Application.Id, request.Application.Version.Major))
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
                    if (ConfigUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange) && bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.TravelerTypes != null
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
                registerTravelerRequest.Characteristics = new Collection<Characteristic>();
                registerTravelerRequest.Characteristics.Add(new Characteristic { Code = "OMNICHANNELCART", Value = "true" });

                registerTravelerRequest.DeviceID = request.DeviceId;
            }
            return (registerTravelerRequest,request)  ;
        }

        private bool IsTravelReasonExist(List<Service.Presentation.CommonModel.Service> specialServiceRequest, string vanity)
        {
            return (specialServiceRequest != null && specialServiceRequest.Exists(p => p.Description == vanity && p.Code == "RFTV" && p.Key == "SSR"));
        }

        public async System.Threading.Tasks.Task AssignCSLCubaTravelReasonToSpecialRequest(FlightTraveler flightTraveler, MOBCPCubaSSR cubaTravelReason, string sessionid)
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
                Model.Shopping.Reservation bookingPathReservation = new Model.Shopping.Reservation();
                //bookingPathReservation = United.Persist.FilePersist.Load<United.Persist.Definition.Shopping.Reservation>(sessionid, bookingPathReservation.ObjectName, false);
                bookingPathReservation = await _sessionHelperService.GetSession<Model.Shopping.Reservation>(sessionid, bookingPathReservation.ObjectName, new List<string> { sessionid, bookingPathReservation.ObjectName }).ConfigureAwait(false);

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
                                isCubaFight = IsCubaAirportCodeExist(stopFlights.Origin, stopFlights.Destination, CubaAirportList);
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


        private async Task<List<MOBCPTraveler>> GetMPDetailsForSavedTravelers(MOBRegisterTravelersRequest request)
        {
            var travelers = request.Travelers;
            United.Services.Customer.Common.MileagePlusesResponse response = new MileagePlusesResponse();
            List<MOBBKLoyaltyProgramProfile> airRewardProgramList = new List<MOBBKLoyaltyProgramProfile>();

            //Build CSL MP List request
            United.Services.Customer.Common.MileagePlusesRequest mpRequest = new United.Services.Customer.Common.MileagePlusesRequest();

            mpRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices").ToString();
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
                                                    where (program.CarrierCode != null && program.CarrierCode.ToUpper().Trim() == "UA") || (!string.IsNullOrEmpty(program.ProgramId) && Convert.ToInt32(program.ProgramId) == 7)
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
            //session = Persist.FilePersist.Load<Persist.Definition.Shopping.Session>(request.SessionId, session.ObjectName);
            session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string>() { request.SessionId, session.ObjectName }).ConfigureAwait(false);

            supressLMX = session.SupressLMXForAppID;
            #endregion
            if (mpRequest.LoyaltyIds != null && mpRequest.LoyaltyIds.Count > 0 && !supressLMX) // Fix to check if Selected Saved travler has MP. - Venkat 06/17/2015
            {
                #region Get MileagePlus list from CSL
                //call CSL for MP list

                string jsonRequest = JsonConvert.SerializeObject(mpRequest);


                //string url = string.Format("{0}/GetMileagePluses", _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLProfile"));

                //if (ConfigurationManager.AppSettings["ForTestingGetttingXMLGetProfileTime"] != null && Convert.ToBoolean(ConfigurationManager.AppSettings["ForTestingGetttingXMLGetProfileTime"].ToString()))
                //{
                //    GetXMLGetProfileTimeForTesting(request);
                //}
                #region//****Get Call Duration Code - Venkat 03/17/2015*******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******


                response = (_customerDataService.GetMileagePluses<MileagePlusesResponse>(session.Token, session.SessionId, jsonRequest).Result).response;

                //HttpHelper.Post(url, "application/json; charset=utf-8", request.Token, jsonRequest);
                #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

                if (_configuration.GetValue<string>("ForTestingGetttingXMLGetProfileTime") != null && Convert.ToBoolean(_configuration.GetValue<string>("ForTestingGetttingXMLGetProfileTime").ToString()))
                {
                    // logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, url, "CSS/CSL-CallDuration", request.Application.Id, request.Application.Version.Major, request.DeviceId + "_" + request.MileagePlusNumber, "CSLGetMileagePluses=" + cslCallTime));
                    _logger.LogInformation("CSS/CSLCallDuration CSLRegisterTraveler = {cslCallTime} and {sessionId}", cslCallTime, request.SessionId);

                }

                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******



                if (response != null)
                {

                    if (response != null && response.Status.Equals(United.Services.Customer.Common.Constants.StatusType.Success) && response.MileagePlusList != null)
                    {


                        var mileagePluses = new List<MOBCPMileagePlus>();
                        mileagePluses = GetMpListFromCSLResponse(response);

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
                        if (response.Errors != null && response.Errors.Count > 0)
                        {
                            string errorMessage = string.Empty;
                            foreach (var error in response.Errors)
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

        private async Task<List<Mobile.Model.Common.MP2015.LmxFlight>> GetLmxForRTI(string token, string cartId)
        {
            List<Mobile.Model.Common.MP2015.LmxFlight> lmxFlights = null;
            LmxQuoteResponse lmxQuoteResponse = null;

            if (!string.IsNullOrEmpty(cartId))
            {
                //string url = _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLBookingProducts");

                string jsonRequest = "{\"CartId\":\"" + cartId + "\",\"Type\":\"RES\",\"LoadAllTiers\":1}";



                //FlightStatus flightStatus = new FlightStatus();
                try
                {
                    //token = flightStatus.GetFLIFOSecurityTokenCSSCall(applicationId, deviceId, transactionId);

                    lmxQuoteResponse = await _lmxInfo.GetLmxRTIInfo<LmxQuoteResponse>(token, jsonRequest, _headers.ContextValues.SessionId);

                    //HttpHelper.Post(url, "application/json; charset=utf-8", token, jsonRequest, httpPostTimeOut, httpPostNumberOfRetry);

                }
                catch (System.Exception) { }

                if (lmxQuoteResponse != null)
                {
                    if (lmxQuoteResponse != null && lmxQuoteResponse.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success))
                    {
                        if (lmxQuoteResponse.Flights != null && lmxQuoteResponse.Flights.Count > 0)
                        {
                            if (lmxFlights == null)
                            {
                                lmxFlights = new List<Mobile.Model.Common.MP2015.LmxFlight>();
                            }
                            CultureInfo ci = null;
                            foreach (var flight in lmxQuoteResponse.Flights)
                            {
                                Mobile.Model.Common.MP2015.LmxFlight lmxFlight = new Mobile.Model.Common.MP2015.LmxFlight();
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
                                        Mobile.Model.Common.MP2015.LmxProduct lmxProduct = new Mobile.Model.Common.MP2015.LmxProduct();
                                        //lmxProduct.BookingCode = product.BookingCode;
                                        //lmxProduct.Description = product.Description;
                                        lmxProduct.ProductType = product.ProductType;
                                        if (product.LmxLoyaltyTiers != null && product.LmxLoyaltyTiers.Count > 0)
                                        {
                                            lmxProduct.LmxLoyaltyTiers = new List<Mobile.Model.Common.MP2015.LmxLoyaltyTier>();
                                            foreach (var loyaltyTier in product.LmxLoyaltyTiers)
                                            {
                                                if (string.IsNullOrEmpty(loyaltyTier.ErrorCode))
                                                {
                                                    Mobile.Model.Common.MP2015.LmxLoyaltyTier lmxLoyaltyTier = new Mobile.Model.Common.MP2015.LmxLoyaltyTier();
                                                    lmxLoyaltyTier.Description = loyaltyTier.Descr;
                                                    lmxLoyaltyTier.Key = loyaltyTier.Key;
                                                    lmxLoyaltyTier.Level = loyaltyTier.Level;
                                                    if (loyaltyTier.LmxQuotes != null && loyaltyTier.LmxQuotes.Count > 0)
                                                    {
                                                        lmxLoyaltyTier.LmxQuotes = new List<Mobile.Model.Common.MP2015.LmxQuote>();
                                                        foreach (var quote in loyaltyTier.LmxQuotes)
                                                        {
                                                            if (ci == null)
                                                                TopHelper.GetCultureInfo(quote.Currency);
                                                            Mobile.Model.Common.MP2015.LmxQuote lmxQuote = new Mobile.Model.Common.MP2015.LmxQuote();
                                                            lmxQuote.Amount = quote.Amount;
                                                            lmxQuote.Currency = quote.Currency;
                                                            lmxQuote.Description = quote.Descr;
                                                            lmxQuote.Type = quote.Type;
                                                            lmxQuote.DblAmount = Double.Parse(quote.Amount);
                                                            lmxQuote.Currency = ConfigUtility.GetCurrencySymbol(ci);
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


        private async Task<MOBSHOPReservation> RegisterTravelers_CFOP(MOBRegisterTravelersRequest request, bool isRegisterOffersCall = false)
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

            Model.Shopping.Reservation bookingPathReservation = new Model.Shopping.Reservation();
            // bookingPathReservation = United.Persist.FilePersist.Load<United.Persist.Definition.Shopping.Reservation>(request.SessionId, bookingPathReservation.ObjectName, false);
            bookingPathReservation = await _sessionHelperService.GetSession<Model.Shopping.Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

            // Traveler selector validation
            if (_configuration.GetValue<bool>("EnableTravelerSelectorCountValidation")&&(await _featureSettings.GetFeatureSettingValue("EnableTravelerSelectorCountValidation").ConfigureAwait(false)))
            {
                if (bookingPathReservation?.NumberOfTravelers > 0 && request?.Travelers?.Count > 0)
                {
                    if (bookingPathReservation.NumberOfTravelers != request.Travelers.Count)
                    {
                        throw new MOBUnitedException((bookingPathReservation.NumberOfTravelers > request.Travelers.Count) ? _configuration.GetValue<string>("FewTravelersErrorMessage") : _configuration.GetValue<string>("ManyTravelersErrorMessage"));
                    }
                }
            }

            string nextViewName = _omniCart.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, bookingPathReservation?.ShopReservationInfo2?.IsDisplayCart == true) ? _traveler.GetNextViewName(request, bookingPathReservation) : "";
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
               await System.Threading.Tasks.Task.Factory.StartNew(async() =>
                {
                    bookingPathReservation.ShopReservationInfo2.PlacePass = await _traveler.GetPlacePass(destinationcode, bookingPathReservation.SearchType, request.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, "RegisterTravelerGetPlacePass", placepasscampain);
                });

            }

            if (_travelerUtility.EnablePlacePassBooking(request.Application.Id, request.Application.Version.Major)
                && bookingPathReservation.ShopReservationInfo2 != null)
            {
                string destinationcode = bookingPathReservation.Trips[0].Destination.ToString();
                bookingPathReservation.ShopReservationInfo2.PlacePass = await _travelerUtility.GetEligiblityPlacePass(destinationcode, bookingPathReservation.SearchType, request.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, "RegisterTravelerGetPlacePass");
            }
            if (ConfigUtility.EnableIBEFull())
            {
                _travelerUtility.RemoveelfMessagesForRTI(ref bookingPathReservation);
            }
            string jsonRequest = string.Empty, jsonResponse = string.Empty;
            if (!request.IsOmniCartSavedTripFlow)
            {
                var tupleRes = await GetRegisterTravelerRequest(request, isRequireNationalityAndResidence, bookingPathReservation);
                RegisterTravelersRequest registerTravelerRequest = tupleRes.regTravelersRequest;
                request = tupleRes.request;
                jsonRequest = JsonConvert.SerializeObject(registerTravelerRequest);

                //string url = string.Format("{0}/RegisterTravelers", ConfigurationManager.AppSettings["ServiceEndPointBaseUrl - ShoppingCartService"]);

                #region//****Get Call Duration Code - Venkat 03/17/2015*******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

                jsonResponse = await _shoppingCartService.GetRegisterTravelers<string>(request.Token, request.SessionId, jsonRequest);
                //HttpHelper.Post(url, "application/json; charset=utf-8", request.Token, jsonRequest);
                #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

                if (_configuration.GetValue<string>("ForTestingGetttingXMLGetProfileTime") != null && Convert.ToBoolean(_configuration.GetValue<string>("ForTestingGetttingXMLGetProfileTime").ToString()))
                {
                    //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, url, "CSS/CSL-CallDuration", request.Application.Id, request.Application.Version.Major, request.DeviceId + "_" + request.MileagePlusNumber, "CSLRegisterTraveler=" + cslCallTime));
                    _logger.LogInformation("CSS/CSLCallDuration CSLRegisterTraveler = {cslCallTime} and {sessionId}", cslCallTime, request.SessionId);

                }
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******    

            }

            if (!string.IsNullOrEmpty(jsonResponse) || request.IsOmniCartSavedTripFlow)
            {
                FlightReservationResponse response = null;

                response = request.IsOmniCartSavedTripFlow ? await _omniCart.GetFlightReservationResponseByCartId(request.SessionId, request.CartId) : JsonConvert.DeserializeObject<FlightReservationResponse>(jsonResponse);

                if (response != null && (response.Status == United.Services.FlightShopping.Common.StatusType.Success) && response.Reservation != null)
                {
                    reservation.TravelersRegistered = true;
                    bookingPathReservation.TravelersRegistered = true;
                    //##Kirti ALM 23973 - Booking 2.1 - REST - LMX - use new CSL Profile Service, GetSavedTraverlerMplist from CSL 
                    request.Travelers = await GetMPDetailsForSavedTravelers(request);

                    if (ConfigUtility.IsETCEnabledforMultiTraveler(request.Application.Id, request.Application.Version.Major.ToString()))
                    {
                        AssignTravelerIndividualTotalAmount(request.Travelers, response.DisplayCart.DisplayPrices, response.Reservation?.Travelers.ToList(), response.Reservation?.Prices?.ToList());
                    }
                    _traveler.AssignFFCsToUnChangedTravelers(request.Travelers, bookingPathReservation.TravelersCSL, request.Application, request.ContinueToChangeTravelers);

                    if ((ConfigUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange) && bookingPathReservation.ShopReservationInfo2 != null &&
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

                    Session session = new Session();
                    //session = Persist.FilePersist.Load<Session>(request.SessionId, session.ObjectName);
                    session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string>() { request.SessionId, session.ObjectName }).ConfigureAwait(false);

                    //added by wade to get details LMX info
                    try
                    {
                        #region //**// LMX Flag For AppID change
                        bool supressLMX = false;
                        supressLMX = session.SupressLMXForAppID;
                        #endregion
                        ShoppingResponse shop = new ShoppingResponse();
                        //shop = United.Persist.FilePersist.Load<ShoppingResponse>(request.SessionId, shop.ObjectName);
                        shop = await _sessionHelperService.GetSession<ShoppingResponse>(request.SessionId, shop.ObjectName, new List<string>() { request.SessionId, shop.ObjectName }).ConfigureAwait(false);

                        bookingPathReservation.LMXFlights = null; // need to default to null to remove LMX from reservation if service call fails.

                        if (!supressLMX && shop != null && shop.Request.ShowMileageDetails)
                            bookingPathReservation.LMXFlights = await GetLmxForRTI(request.Token, request.CartId);
                    }
                    catch { }

                    string mpNumbers = string.Empty;
                    foreach (var traveler in request.Travelers)
                    {
                        if (ConfigUtility.IsEnabledNationalityAndResidence(bookingPathReservation.IsReshopChange, request.Application.Id, request.Application.Version.Major))
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
                        string savedTravelerMPNameMismatch = _configuration.GetValue<string>("SavedTravelerMPNameMismatch").ToString();
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
                            var tupleResponse = await _traveler.GetProfileOwnerCreditCardList(request.SessionId, creditCardAddresses, mpPhone, mpEmail, string.Empty);
                            bookingPathReservation.CreditCards = tupleResponse.savedProfileOwnerCCList;
                            creditCardAddresses = tupleResponse.creditCardAddresses;
                            mpPhone = tupleResponse.mpPhone;
                            mpEmail = tupleResponse.mpEmail;
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
                    bool enableEplus = ConfigUtility.EnableEPlusAncillary(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange);


                    //response.DisplayCart.PricingChange = true;
                    //Bug 241287 : added below null check, as it is failing in preprod - 7th Feb 2018 j.srinivas
                    if (enableNat || enableUKtax || enableTravelerTypes || enableEplus)
                    {
                        if (response.DisplayCart.PricingChange || (enableTravelerTypes && !_traveler.comapreTtypesList(bookingPathReservation, response.DisplayCart))
                            || (enableEplus && bookingPathReservation.TravelOptions != null && bookingPathReservation.TravelOptions.Any(t => t?.Key.Trim().ToUpper() == "EFS"))
                            || (_omniCart.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, bookingPathReservation?.ShopReservationInfo2?.IsDisplayCart == true) && !string.IsNullOrEmpty(nextViewName) && nextViewName != "RTI")//MOBILE-20204
                            )
                        {
                            bookingPathReservation.Prices.Clear();
                            if (enableUKtax)
                            {
                                bookingPathReservation.Prices = _shoppingUtility.GetPrices(response.DisplayCart.DisplayPrices, bookingPathReservation.AwardTravel, bookingPathReservation.SessionId, bookingPathReservation.IsReshopChange,
                                    bookingPathReservation.SearchType, appId: request.Application.Id, appVersion: request.Application.Version.Major, isNotSelectTripCall: true, shopBookingDetailsResponse: response, isRegisterOffersFlow: isRegisterOffersCall, session: session);
                            }
                            else
                            {
                                bookingPathReservation.Prices = _traveler.GetPricesAfterPriceChange(response.DisplayCart.DisplayPrices, bookingPathReservation.AwardTravel, bookingPathReservation.SessionId, bookingPathReservation.IsReshopChange, response);
                            }

                            if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                            {
                                bookingPathReservation.Prices.AddRange(await GetPrices(response.DisplayCart.DisplayFees, false, string.Empty));

                            }
                            //need to add close in fee to TOTAL
                            bookingPathReservation.Prices = AdjustTotal(bookingPathReservation.Prices);
                            if (enableUKtax)
                            {
                                bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = ConfigUtility.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices, bookingPathReservation.IsReshopChange, appId: request.Application.Id, appVersion: request.Application.Version.Major, travelType: session.TravelType);
                                if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                                {
                                    bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes.Add(AddFeesAfterPriceChange(response.DisplayCart.DisplayFees));
                                }
                            }
                            else
                            {
                                bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices, bookingPathReservation.NumberOfTravelers, bookingPathReservation.IsReshopChange);
                                if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                                {
                                    bookingPathReservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes.Add(AddFeesAfterPriceChange1(response.DisplayCart.DisplayFees));
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
                                    bookingPathReservation.ShopReservationInfo2.InfoWarningMessages = new List<Model.Shopping.InfoWarningMessages>();

                                if (!bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.PRICECHANGE.ToString()))
                                {
                                    bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Add(_travelerUtility.GetPriceChangeMessage());
                                    bookingPathReservation.ShopReservationInfo2.InfoWarningMessages = bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                                }

                                if (bookingPathReservation.ShopReservationInfo2.IsUnfinihedBookingPath)
                                {
                                    //ShopRequest persistedShopPindownRequest = FilePersist.Load<ShopRequest>(request.SessionId, typeof(United.Services.FlightShopping.Common.ShopRequest).FullName);
                                    ShopRequest persistedShopPindownRequest = await _sessionHelperService.GetSession<ShopRequest>(request.SessionId, typeof(United.Services.FlightShopping.Common.ShopRequest).FullName, new List<string>() { request.SessionId, typeof(United.Services.FlightShopping.Common.ShopRequest).FullName }).ConfigureAwait(false);


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

                                        //FilePersist.Save<United.Services.FlightShopping.Common.ShopRequest>(request.SessionId, typeof(United.Services.FlightShopping.Common.ShopRequest).FullName, persistedShopPindownRequest);
                                        await _sessionHelperService.SaveSession<United.Services.FlightShopping.Common.ShopRequest>(persistedShopPindownRequest, request.SessionId, new List<string> { request.SessionId, typeof(United.Services.FlightShopping.Common.ShopRequest).FullName }, typeof(United.Services.FlightShopping.Common.ShopRequest).FullName).ConfigureAwait(false);

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
                        bookingPathReservation.ShopReservationInfo2.displayTravelTypes = UtilityHelper.GetDisplayTravelerTypes(response.DisplayCart.DisplayTravelers);
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

                    if (_configuration.GetValue<bool>("EnableInhibitBooking") && UtilityHelper.IdentifyInhibitWarning(response))
                    {
                        //if (traceSwitch.TraceWarning)
                        //{
                        //    logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "RegisterTravelers - Response with Inhibit warning", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonResponse, true, false));
                        //}
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
                            bookingPathReservation.ShopReservationInfo2.InfoWarningMessages = bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();

                            if (bookingPathReservation.ShopReservationInfo2.InfoWarningMessagesPayment == null)
                                bookingPathReservation.ShopReservationInfo2.InfoWarningMessagesPayment = new List<InfoWarningMessages>();

                            bookingPathReservation.ShopReservationInfo2.InfoWarningMessagesPayment.Add(_travelerUtility.GetConcurrCardPolicyMessage(true));
                        }
                    }
                    #region 1127 - Chase Offer (Booking)
                    if (EnableChaseOfferRTI(request.Application.Id, request.Application.Version.Major))
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
                                FormatChaseCreditStatemnet(bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement);
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
                                // bookingPathReservation.ShopReservationInfo2.IsMultipleTravelerEtcFeatureClientToggleEnabled = Utility.IsClientCatalogEnabled(request.Application.Id, ConfigurationManager.AppSettings["MultipleTravelerETCClientToggleIds"].Split('|'));

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
                        if (bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Exists(x => x.Order == MOBINFOWARNINGMESSAGEORDER.RTICORPORATELEISUREOPTOUTMESSAGE.ToString()) && _traveler.IsFareLockAvailable(bookingPathReservation))
                        {
                            bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Remove(bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.Find(x => x.Order == MOBINFOWARNINGMESSAGEORDER.RTICORPORATELEISUREOPTOUTMESSAGE.ToString()));
                        }
                    }
                    #endregion

                    if (_shoppingUtility.IncludeMoneyPlusMiles(request.Application.Id, request.Application.Version.Major))
                    {
                        bookingPathReservation.GetALLSavedTravelers = true;
                    }
                    if (_omniCart.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, bookingPathReservation?.ShopReservationInfo2?.IsDisplayCart == true))
                    {
                        if (nextViewName != "RTI" && request.IsRegisterTravelerFromRTI)
                        {
                            bookingPathReservation.ShopReservationInfo2.HideBackButtonOnSelectTraveler = true;
                        }
                        else
                        {
                            bookingPathReservation.ShopReservationInfo2.HideBackButtonOnSelectTraveler = false;
                        }
                    }

                    // Feature TravelInsuranceOptimization : MOBILE-21191, MOBILE-21193, MOBILE-21195, MOBILE-21197

                    if (_configuration.GetValue<bool>("EnableTravelInsuranceOptimization"))
                    {
                        #region TPI in booking path
                        if (_travelerUtility.EnableTPI(request.Application.Id, request.Application.Version.Major, 3) && !bookingPathReservation.IsReshopChange)
                        {
                            // call TPI 
                            try
                            {
                                string token = session.Token;
                                TPIInfoInBookingPath tPIInfo = await GetBookingPathTPIInfo(request.SessionId, request.LanguageCode, request.Application, request.DeviceId, response.Reservation.CartId, token, true, true, false);
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
                        bookingPathReservation.Prices = UpdatePriceForUnRegisterTPI(bookingPathReservation.Prices);

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
                    //FilePersist.Save<United.Persist.Definition.Shopping.Reservation>(bookingPathReservation.SessionId, bookingPathReservation.ObjectName, bookingPathReservation);
                    await _sessionHelperService.SaveSession<Model.Shopping.Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);

                    #endregion

                    MOBShoppingCart persistShoppingCart = new MOBShoppingCart();
                    //persistShoppingCart = FilePersist.Load<MOBShoppingCart>(request.SessionId, persistShoppingCart.ObjectName);
                    persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, persistShoppingCart.ObjectName, new List<string>() { request.SessionId, persistShoppingCart.ObjectName }).ConfigureAwait(false);

                    if (persistShoppingCart == null)
                        persistShoppingCart = new MOBShoppingCart();
                    await _traveler.InFlightCLPaymentEligibility(request, bookingPathReservation, session, persistShoppingCart);
                    persistShoppingCart.Products = await _shoppingUtility.ConfirmationPageProductInfo(response, false, request.Application, null, request.Flow, sessionId: session?.SessionId);
                    persistShoppingCart.CartId = request.CartId;
                    double priceTotal = ConfigUtility.GetGrandTotalPriceForShoppingCart(false, response, false, request.Flow);
                    persistShoppingCart.TotalPrice = String.Format("{0:0.00}", priceTotal);
                    persistShoppingCart.DisplayTotalPrice = Decimal.Parse(priceTotal.ToString()).ToString("c");
                    persistShoppingCart.TermsAndConditions = await _travelerUtility.GetProductBasedTermAndConditions(null, response, false);
                    persistShoppingCart.PaymentTarget = (request.Flow == FlowType.BOOKING.ToString()) ? _travelerUtility.GetBookingPaymentTargetForRegisterFop(response) : _travelerUtility.GetPaymentTargetForRegisterFop(response);
                    //FilePersist.Save<MOBShoppingCart>(request.SessionId, persistShoppingCart.ObjectName, persistShoppingCart);

                    await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, request.SessionId, new List<string> { request.SessionId, persistShoppingCart.ObjectName }, persistShoppingCart.ObjectName).ConfigureAwait(false);

                    if (ConfigUtility.EnableRtiMandateContentsToDisplayByMarket(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange))
                    {
                        if (bookingPathReservation.ShopReservationInfo2.RTIMandateContentsToDisplayByMarket == null || bookingPathReservation.ShopReservationInfo2.RTIMandateContentsToDisplayByMarket.Count == 0)
                        {
                            try
                            {
                                await _traveler.UpdateCovidTestInfo(request, bookingPathReservation, session);
                            }
                            catch (WebException ex)
                            {
                                //if (traceSwitch.TraceWarning)
                                //    logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "UpdateCovidTestInfo", "UnitedException", request.Application.Id, request.Application.Version.Major, request.DeviceId, ex.Message, true, false));

                                throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                            }
                            catch (Exception ex)
                            {
                                //if (traceSwitch.TraceWarning)
                                //    logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "UpdateCovidTestInfo", "UnitedException", request.Application.Id, request.Application.Version.Major, request.DeviceId, ex.Message, true, false));

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
                    //MOBILE-20204
                    //Note: Implementing this for supporting the Booking ShoppingCart changes..as we are not updating the shoppingcart and updating the reservation.prices right now in prod not able to show the correct prices.
                    // Scenario:Changing the traveler on RTI after registering ancillary offers
                    #region UnRegister If any ancillary offers registered
                    if (_omniCart.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, true) && !string.IsNullOrEmpty(nextViewName) && nextViewName != "RTI" && !request.IsOmniCartSavedTripFlow)
                    {
                        await _traveler.UnregisterAncillaryOffer(persistShoppingCart, response, request, request.SessionId, request.CartId);
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
                        if (response.Errors.Any(e => e.MinorCode.Trim().Equals("10036")))
                        {
                            throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                        }
                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToRegisterTravelerErrorMessage").ToString();

                        if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting").ToString()))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL RegisterTravelers_CFOP(MOBRegisterTravelersRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToRegisterTravelerErrorMessage").ToString();

                if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting").ToString()))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL RegisterTravelers_CFOP(MOBRegisterTravelersRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }

            #endregion

            return reservation;
        }

        public void FormatChaseCreditStatemnet(MOBCCAdStatement chaseCreditStatement)
        {
            if (_configuration.GetValue<bool>("UpdateChaseColor16788"))
            {
                chaseCreditStatement.styledInitialDisplayPrice = string.IsNullOrWhiteSpace(chaseCreditStatement.initialDisplayPrice) ? "" : HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginning"))
                    + chaseCreditStatement.initialDisplayPrice + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
                chaseCreditStatement.styledInitialDisplayText = HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginning")) + _configuration.GetValue<string>("InitialDisplayText") + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
                chaseCreditStatement.styledStatementCreditDisplayPrice = string.IsNullOrWhiteSpace(chaseCreditStatement.statementCreditDisplayPrice) ? "" : HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginningWithColor")) + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextStrongBeginning"))
                    + _travelerUtility.GetPriceAfterChaseCredit(0, chaseCreditStatement.statementCreditDisplayPrice) + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextStrongEnding")) + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
                chaseCreditStatement.styledStatementCreditDisplayText = HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginningWithColor")) + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextStrongBeginning"))
                    + _configuration.GetValue<string>("StatementCreditDisplayText") + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextStrongEnding")) + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
                chaseCreditStatement.styledFinalAfterStatementDisplayPrice = string.IsNullOrWhiteSpace(chaseCreditStatement.finalAfterStatementDisplayPrice) ? "" : HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginning")) + chaseCreditStatement.finalAfterStatementDisplayPrice + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
                chaseCreditStatement.styledFinalAfterStatementDisplayText = HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginning")) + _configuration.GetValue<string>("FinalAfterStatementDisplayText") + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
            }
        }


        public bool EnableChaseOfferRTI(int appID, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableChaseOfferRTI") && (!_configuration.GetValue<bool>("EnableChaseOfferRTIVersionCheck") ||
                (_configuration.GetValue<bool>("EnableChaseOfferRTIVersionCheck") && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, _configuration.GetValue<string>("AndroidEnableChaseOfferRTIVersion"), _configuration.GetValue<string>("iPhoneEnableChaseOfferRTIVersion"))));
        }

        private List<List<MOBSHOPTax>> GetTaxAndFeesAfterPriceChange(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices, int numPax, bool isReshopChange = false)
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
                        taxNfee.DisplayAmount = TopHelper.FormatAmountForDisplay(taxNfee.Amount, ci, false);
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
                    tnf.TaxCodeDescription = string.Format("{0} adult{1}: {2}{3}", price.Count, price.Count > 1 ? "s" : "", tnf.DisplayAmount,
                        isEnableOmniCartMVP2Changes ? "/person" : " per person");
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
                        await ValidateAwardMileageBalance(sessionId, price.Amount, appId, appVersion, catalogItems);
                    }
                }

                double tempDouble = 0;
                double.TryParse(price.Amount.ToString(), out tempDouble);
                bookingPrice.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);

                if (price.Currency.ToUpper() == "MIL")
                {
                    bookingPrice.FormattedDisplayValue = formatAwardAmountForDisplay(price.Amount.ToString(), false);
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

                if (ConfigUtility.EnableYADesc(isReshopChange) && price.PricingPaxType != null && price.PricingPaxType.ToUpper().Equals("UAY"))   //If Young adult account
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
            if (_traveler.IsBuyMilesFeatureEnabled(appId, appVersion, isNotSelectTripCall: true))
            {
                string additionalMiles = "Additional {0} miles";
                string formattedMiles = String.Format("{0:n0}", shopBookingDetailsResponse?.DisplayCart?.DisplayFees?.FirstOrDefault()?.SubItems?.Where(a => a.Key == "PurchaseMiles")?.FirstOrDefault()?.Value);
                if (bookingPrice?.DisplayType == "MPF")
                    bookingPrice.PriceTypeDescription = string.Format(additionalMiles, formattedMiles);
            }
        }


        public async System.Threading.Tasks.Task ValidateAwardMileageBalance(string sessionId, decimal milesNeeded, int appId = 0, string appVersion = "", List<MOBItem> catalogItems = null)
        {
            Model.Shopping.CSLShopRequest shopRequest = new Model.Shopping.CSLShopRequest();
            //shopRequest = Persist.FilePersist.Load<United.Persist.Definition.Shopping.CSLShopRequest>(sessionId, shopRequest.ObjectName);
            shopRequest = await _sessionHelperService.GetSession<Model.Shopping.CSLShopRequest>(sessionId, shopRequest.ObjectName, new List<string> { sessionId, shopRequest.ObjectName }).ConfigureAwait(false);

            if (shopRequest != null && shopRequest.ShopRequest != null && shopRequest.ShopRequest.AwardTravel && shopRequest.ShopRequest.LoyaltyPerson != null && shopRequest.ShopRequest.LoyaltyPerson.AccountBalances != null)
            {
                if (shopRequest.ShopRequest.LoyaltyPerson.AccountBalances[0] != null && shopRequest.ShopRequest.LoyaltyPerson.AccountBalances[0].Balance < milesNeeded)
                {
                    if (_traveler.IsBuyMilesFeatureEnabled(appId, appVersion, catalogItems) == false)
                        throw new MOBUnitedException(_configuration.GetValue<string>("NoEnoughMilesForAwardBooking"));
                }
            }
        }

        private async Task<MOBSHOPSelectTripResponse> SelectOmniCartSavedTrip(Session session, MOBSHOPSelectUnfinishedBookingRequest request, Stopwatch _stopwatch, HttpContext httpContext = null)
        {
            MOBSHOPSelectTripResponse response = new MOBSHOPSelectTripResponse();
            Model.Shopping.Reservation bookingPathReservation = new Model.Shopping.Reservation();
            var isExpressCheckoutFlow = false; 

            #region Building the SoftRti Response
            response.Availability = new MOBSHOPAvailability();
            response.Availability = await GetShopPinDownDetailsV2(session, request, httpContext);
            #endregion Building the SoftRti Response
            FlightReservationResponse flightReservationResponse = await _omniCart.GetFlightReservationResponseByCartId(session.SessionId, request.CartId);
            #region Building traveler Logic(From Flightreservationresponse if we have travelers..Build traveler Details)
            if (flightReservationResponse?.Reservation?.Travelers?.Any(traveler => !String.IsNullOrEmpty(traveler?.Person?.GivenName) && !String.IsNullOrEmpty(traveler?.Person?.Surname)) == true)
            {
                MOBRegisterTravelersRequest travelersRequest = new MOBRegisterTravelersRequest();
                await MapCslTravelersToMOBTravelers(travelersRequest, flightReservationResponse, request, response.Availability.Reservation, session);
                response.Availability.Reservation = await RegisterTravelers_CFOP(travelersRequest);
                response.Availability.Reservation.TravelersRegistered = true;
                await BuildResponseForRegisterTraveler(travelersRequest, response.Availability.Reservation, session, flightReservationResponse);
            }
            #endregion Building traveler Logic           
            #region  OmnicartRelease Candidate 1.2 retaining the Travel Options    

            if (_omniCart.IsEnableOmniCartReleaseCandidateTwoChanges_Bundles(request.Application.Id, request.Application.Version.Major)
                && (_configuration.GetValue<bool>("Disable_NoBundleOffer_NavigationFix") ? true : response?.Availability?.Reservation?.TravelersRegistered == true))//MOBILE-21207 bug fix
            {
                MOBShoppingCart shoppingCart = new MOBShoppingCart();
                //shoppingCart = United.Persist.FilePersist.Load<MOBShoppingCart>(request.SessionId, shoppingCart.GetType().ToString()) ?? new MOBShoppingCart();
                shoppingCart = _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, shoppingCart.ObjectName,
                    new List<string> { request.SessionId, shoppingCart.ObjectName }).Result ?? new MOBShoppingCart();



                if (IsBundleOrSFFRegistered(flightReservationResponse))
                {
                    AssignCallRegisterOffersFlag(shoppingCart);
                    //  shoppingCart.OmniCart.NavigateToScreen = OmniCart.IsBundlesSelectedForAlltrips(flightReservationResponse) ? MOBNavigationToScreen.SEATS.ToString() : MOBNavigationToScreen.TRAVELOPTIONS.ToString();

                    #region Get bundle Offer
                    if (!_omniCart.IsEnableOmniCartReleaseCandidateThreeChanges_Seats(request.Application.Id, request.Application.Version.Major))
                        GetBundleOffer(request, request.IsOmniCartSavedTrip);
                    #endregion
                }
                #region  OmnicartRelease Candidate 1.3 retaining Seats 
                if (_omniCart.IsEnableOmniCartReleaseCandidateThreeChanges_Seats(request.Application.Id, request.Application.Version.Major))
                {
                    if (IsSeatsRegistered(flightReservationResponse))
                    {
                        AssignCallRegisterOffersFlag(shoppingCart);
                        shoppingCart.OmniCart.NavigateToSeatmapSegmentNumber = AssignNavigateToSeatmapSegmentNumber(flightReservationResponse);
                    }
                }
                #endregion  OmnicartRelease Candidate 1.3 retaining Seats 
                await CheckBundlesOffered(request, flightReservationResponse, shoppingCart);
                AssignOmnicartRepricingInfo(shoppingCart, flightReservationResponse);
                shoppingCart.OmniCart.NavigateToScreen = GetNavigateToScreenValue(shoppingCart, flightReservationResponse);
                LogInternlineSeatRepriceFailures(shoppingCart, response?.Availability?.Reservation?.Trips, request, request.SessionId);

                //FilePersist.Save<MOBShoppingCart>(request.SessionId, shoppingCart.GetType().ToString(), shoppingCart);
                await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, request.SessionId, new List<string> { request.SessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName).ConfigureAwait(false);

            }
            #endregion
            //bookingPathReservation = United.Persist.FilePersist.Load<United.Persist.Definition.Shopping.Reservation>(request.SessionId, bookingPathReservation.ObjectName);
            bookingPathReservation = await _sessionHelperService.GetSession<Model.Shopping.Reservation>(request.SessionId, bookingPathReservation.ObjectName,
                new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

            if (response.Availability.Reservation.TravelersRegistered)
            {
                #region Get Trip Insurance

                await CheckFareLockExistsandGetTPI(request, session, bookingPathReservation, request.CartId);
                //FilePersist.Save<Model.Shopping.Reservation>(bookingPathReservation.SessionId, bookingPathReservation.ObjectName, bookingPathReservation);                

                #endregion Get Trip Insurance
            }

            #region ExpressCheckout flow
            if (await _shoppingUtility.IsEnabledExpressCheckoutFlow(request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false)
                && flightReservationResponse != null
                && request != null
                && !string.IsNullOrEmpty(request.MileagePlusAccountNumber)
                && flightReservationResponse.IsExpressCheckout)
            {
                try
                {
                    isExpressCheckoutFlow = await GetNavigateToScreenValueForExpressCheckout(flightReservationResponse, request).ConfigureAwait(false);
                    if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null)
                    {
                        bookingPathReservation.ShopReservationInfo2.IsExpressCheckoutPath = isExpressCheckoutFlow;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("SelectOmniCartSavedTrip - ExpressCheckout Exception {error} and SessionId {sessionId}", ex.Message, request.SessionId);
                }
            }
            #endregion
            if (bookingPathReservation != null) 
            {
                await _sessionHelperService.SaveSession<Model.Shopping.Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
            }            

            if (!isExpressCheckoutFlow && _shoppingUtility.IsEnableFarelockforOmniCart(request.Application.Id, request.Application.Version.Major, response.Availability.Reservation.TravelersRegistered, request.IsOmniCartSavedTrip))
            {

                DisplayCartResponse displayCartResponse = new DisplayCartResponse();
                displayCartResponse = await GetFarelockDetails(request.SessionId, request.LanguageCode, request.Application, request.DeviceId, request.CartId, session.Token);

                Service.Presentation.ProductResponseModel.ProductOffer FareLockResponse = new Service.Presentation.ProductResponseModel.ProductOffer();
                FareLockResponse.Characteristics = displayCartResponse.Reservation.Characteristic;
                FareLockResponse.FlightSegments = displayCartResponse.MerchProductOffer.FlightSegments;
                FareLockResponse.Offers = displayCartResponse.MerchProductOffer.Offers;
                FareLockResponse.Requester = displayCartResponse.MerchProductOffer.Requester;
                FareLockResponse.Travelers = displayCartResponse.MerchProductOffer.Travelers;
                FareLockResponse.ODOptions = displayCartResponse.MerchProductOffer.ODOptions;
                FareLockResponse.Response = displayCartResponse.MerchProductOffer.Response;
                FareLockResponse.Solutions = displayCartResponse.MerchProductOffer.Solutions;

                bookingPathReservation = await _sessionHelperService.GetSession<Model.Shopping.Reservation>(request.SessionId, bookingPathReservation.ObjectName,
                    new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                bookingPathReservation.FareLock = GetFareLockOptions(FareLockResponse, flightReservationResponse.DisplayCart.DisplayPrices, request?.CatalogItems, request.Application.Id, request.Application.Version.Major);
                await _sessionHelperService.SaveSession<Model.Shopping.Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);

                var persistedShopBookingDetailsResponse = new ShopBookingDetailsResponse
                {
                    SessionId = session.SessionId,
                    Reservation = flightReservationResponse.Reservation,
                    CartId = response.CartId
                };
                
                if (string.IsNullOrEmpty(session.EmployeeId))
                {
                    persistedShopBookingDetailsResponse.FareLock = FareLockResponse;
                }                
                await _sessionHelperService.SaveSession(persistedShopBookingDetailsResponse, persistedShopBookingDetailsResponse.SessionId, new List<string> { persistedShopBookingDetailsResponse.SessionId, persistedShopBookingDetailsResponse.ObjectName }, persistedShopBookingDetailsResponse.ObjectName).ConfigureAwait(false);                              
            }
            response.Availability.Reservation = await GetReservationFromPersist(response.Availability.Reservation, session);
            return response;
        }

        private async Task<bool> GetNavigateToScreenValueForExpressCheckout(FlightReservationResponse flightReservationResponse, MOBSHOPSelectUnfinishedBookingRequest request)
        {
            var isExpressCheckout = false;
            try
            {                
                // If not bundles and not seats registered change the navigatetoscreen property to seats
                if (!IsBundleOrSFFRegistered(flightReservationResponse) && !IsSeatsRegistered(flightReservationResponse))
                {
                    MOBShoppingCart shoppingCart = new MOBShoppingCart();
                    shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, shoppingCart.ObjectName, new List<string> { request.SessionId, shoppingCart.ObjectName }).ConfigureAwait(false);

                    if (shoppingCart != null && shoppingCart.OmniCart != null
                        && shoppingCart.OmniCart.OmniCartPricingInfos != null && shoppingCart.OmniCart.OmniCartPricingInfos.Count() == 0
                        && (shoppingCart.OmniCart.NavigateToScreen == MOBNavigationToScreen.TRAVELOPTIONS.ToString() || string.IsNullOrEmpty(shoppingCart.OmniCart.NavigateToScreen)))
                    {
                        shoppingCart.OmniCart.NavigateToScreen = MOBNavigationToScreen.SEATS.ToString();
                        await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, request.SessionId, new List<string> { request.SessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName).ConfigureAwait(false);
                        isExpressCheckout = true;
                    }                   
                }
            }
            catch (Exception ex) 
            {
                _logger.ILoggerError("UpdateExpressCheckoutFlowAndNavigateTo - Error making updates for the express checkout flow {exception}", ex.Message);
                isExpressCheckout = false;
            }
            return isExpressCheckout;
        }

        private async Task<DisplayCartResponse> GetFarelockDetails(string SessionId, string LanguageCode, MOBApplication Application, string DeviceId, string CartId, string Token)
        {
            ProductSearchRequest getFLKRequest = new ProductSearchRequest();
            getFLKRequest.SessionId = SessionId;
            getFLKRequest.LanguageCode = LanguageCode;
            getFLKRequest.Application = Application;
            getFLKRequest.DeviceId = DeviceId;
            getFLKRequest.CartId = CartId;
            getFLKRequest.PointOfSale = "US";
            getFLKRequest.CartKey = "FLK";
            getFLKRequest.ProductCodes = new List<string>() { "FLK" };
            DisplayCartRequest displayCartRequest = await GetDisplayCartRequest(getFLKRequest).ConfigureAwait(false);
            string jsonRequest = JsonConvert.SerializeObject(displayCartRequest);
            string jsonRes = await _flightShoppingProductsService.GetProducts(Token, _headers.ContextValues.SessionId, jsonRequest).ConfigureAwait(false);
            DisplayCartResponse displayCartResponse = new DisplayCartResponse();
            displayCartResponse = JsonConvert.DeserializeObject<DisplayCartResponse>(jsonRes);
            return displayCartResponse;
        }
        public List<MOBSHOPTax> AddFeesAfterPriceChange(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices)
        {

            List<MOBSHOPTax> taxsAndFees = new List<MOBSHOPTax>();
            CultureInfo ci = null;

            foreach (var price in prices)
            {
                if (ci == null)
                {
                    ci = TopHelper.GetCultureInfo(price.Currency);
                }
                MOBSHOPTax taxNfee = new MOBSHOPTax();
                taxNfee.CurrencyCode = price.Currency;
                taxNfee.Amount = price.Amount;
                taxNfee.DisplayAmount = TopHelper.FormatAmountForDisplay(taxNfee.Amount, ci, false);
                taxNfee.TaxCode = price.Type;
                taxNfee.TaxCodeDescription = price.Description;
                if (taxNfee.TaxCode != "MPF")
                    taxsAndFees.Add(taxNfee);
            }
            return taxsAndFees;
        }
        private List<MOBSHOPTax> AddFeesAfterPriceChange1(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices)
        {

            List<MOBSHOPTax> taxsAndFees = new List<MOBSHOPTax>();
            CultureInfo ci = null;

            foreach (var price in prices)
            {
                if (ci == null)
                {
                    ci = TopHelper.GetCultureInfo(price.Currency);
                }
                MOBSHOPTax taxNfee = new MOBSHOPTax();
                taxNfee.CurrencyCode = price.Currency;
                taxNfee.Amount = price.Amount;
                taxNfee.DisplayAmount = TopHelper.FormatAmountForDisplay(taxNfee.Amount, ci, false);
                taxNfee.TaxCode = price.Type;
                taxNfee.TaxCodeDescription = price.Description;
                taxsAndFees.Add(taxNfee);
            }
            return taxsAndFees;
        }

        private async Task<MOBSHOPReservation> GetReservationFromPersist(MOBSHOPReservation reservation, Session session)
        {
            #region
            //United.Persist.Definition.Shopping.Session session = GetShoppingSession(sessionID);
            Model.Shopping.Reservation bookingPathReservation = new Model.Shopping.Reservation();
            //bookingPathReservation = United.Persist.FilePersist.Load<United.Persist.Definition.Shopping.Reservation>(session.SessionId, bookingPathReservation.ObjectName);
            bookingPathReservation = await _sessionHelperService.GetSession<Model.Shopping.Reservation>(session.SessionId, bookingPathReservation.ObjectName, new List<string> { session.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

            return MakeReservationFromPersistReservation(reservation, bookingPathReservation, session);

            #endregion
        }
        public MOBSHOPReservation MakeReservationFromPersistReservation(MOBSHOPReservation reservation, Model.Shopping.Reservation bookingPathReservation,
         Session session)
        {
            reservation.CartId = bookingPathReservation.CartId;
            reservation.PointOfSale = bookingPathReservation.PointOfSale;
            reservation.ClubPassPurchaseRequest = bookingPathReservation.ClubPassPurchaseRequest;
            reservation.CreditCards = bookingPathReservation.CreditCards;
            reservation.CreditCardsAddress = bookingPathReservation.CreditCardsAddress;
            reservation.FareLock = bookingPathReservation.FareLock;
            reservation.FareRules = bookingPathReservation.FareRules;
            reservation.IsSignedInWithMP = bookingPathReservation.IsSignedInWithMP;
            reservation.NumberOfTravelers = bookingPathReservation.NumberOfTravelers;
            reservation.Prices = bookingPathReservation.Prices;
            reservation.SearchType = bookingPathReservation.SearchType;
            reservation.SeatPrices = bookingPathReservation.SeatPrices;
            reservation.SessionId = session.SessionId;
            reservation.Taxes = bookingPathReservation.Taxes;
            reservation.UnregisterFareLock = bookingPathReservation.UnregisterFareLock;
            reservation.AwardTravel = bookingPathReservation.AwardTravel;
            reservation.LMXFlights = bookingPathReservation.LMXFlights;
            reservation.IneligibleToEarnCreditMessage = bookingPathReservation.IneligibleToEarnCreditMessage;
            reservation.OaIneligibleToEarnCreditMessage = bookingPathReservation.OaIneligibleToEarnCreditMessage;
            reservation.SeatPrices = bookingPathReservation.SeatPrices;
            reservation.IsBookingCommonFOPEnabled = bookingPathReservation.IsBookingCommonFOPEnabled;
            reservation.IsReshopCommonFOPEnabled = bookingPathReservation.IsReshopCommonFOPEnabled;
            if (bookingPathReservation.TravelersCSL != null && bookingPathReservation.TravelerKeys != null)
            {
                List<MOBCPTraveler> lstTravelers = new List<MOBCPTraveler>();
                foreach (string travelerKey in bookingPathReservation.TravelerKeys)
                {
                    lstTravelers.Add(bookingPathReservation.TravelersCSL[travelerKey]);
                }
                reservation.TravelersCSL = lstTravelers;
            }
            reservation.TravelersRegistered = bookingPathReservation.TravelersRegistered;
            reservation.TravelOptions = bookingPathReservation.TravelOptions;
            reservation.Trips = bookingPathReservation.Trips;
            reservation.ReservationPhone = bookingPathReservation.ReservationPhone;
            reservation.ReservationEmail = bookingPathReservation.ReservationEmail;
            reservation.FlightShareMessage = bookingPathReservation.FlightShareMessage;
            reservation.PKDispenserPublicKey = bookingPathReservation.PKDispenserPublicKey;
            reservation.IsRefundable = bookingPathReservation.IsRefundable;
            reservation.ISInternational = bookingPathReservation.ISInternational;
            reservation.ISFlexibleSegmentExist = bookingPathReservation.ISFlexibleSegmentExist;
            reservation.ClubPassPurchaseRequest = bookingPathReservation.ClubPassPurchaseRequest;
            reservation.GetALLSavedTravelers = bookingPathReservation.GetALLSavedTravelers;
            reservation.IsELF = bookingPathReservation.IsELF;
            reservation.IsSSA = bookingPathReservation.IsSSA;
            reservation.IsMetaSearch = bookingPathReservation.IsMetaSearch;
            reservation.MetaSessionId = bookingPathReservation.MetaSessionId;
            reservation.IsUpgradedFromEntryLevelFare = bookingPathReservation.IsUpgradedFromEntryLevelFare;
            reservation.SeatAssignmentMessage = bookingPathReservation.SeatAssignmentMessage;
            if (bookingPathReservation.TCDAdvisoryMessages != null && bookingPathReservation.TCDAdvisoryMessages.Count > 0)
            {
                reservation.TCDAdvisoryMessages = bookingPathReservation.TCDAdvisoryMessages;
            }
            //##Price Break Down - Kirti
            if (_configuration.GetValue<string>("EnableShopPriceBreakDown") != null &&
                Convert.ToBoolean(_configuration.GetValue<string>("EnableShopPriceBreakDown").ToString()))
            {
                reservation.ShopPriceBreakDown = GetPriceBreakDown(bookingPathReservation);
            }

            if (session != null && !string.IsNullOrEmpty(session.EmployeeId) && reservation != null)
            {
                reservation.IsEmp20 = true;
            }
            if (bookingPathReservation.IsCubaTravel)
            {
                reservation.IsCubaTravel = bookingPathReservation.IsCubaTravel;
                reservation.CubaTravelInfo = bookingPathReservation.CubaTravelInfo;
            }
            reservation.FormOfPaymentType = bookingPathReservation.FormOfPaymentType;
            if (bookingPathReservation.FormOfPaymentType == MOBFormofPayment.PayPal || bookingPathReservation.FormOfPaymentType == MOBFormofPayment.PayPalCredit)
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
            if (bookingPathReservation.FOPOptions != null && bookingPathReservation.FOPOptions.Count > 0) //FOP Options Fix Venkat 12/08
            {
                reservation.FOPOptions = bookingPathReservation.FOPOptions;
            }

            if (bookingPathReservation.IsReshopChange)
            {
                reservation.ReshopTrips = bookingPathReservation.ReshopTrips;
                reservation.Reshop = bookingPathReservation.Reshop;
                reservation.IsReshopChange = true;
            }
            reservation.ELFMessagesForRTI = bookingPathReservation.ELFMessagesForRTI;
            if (bookingPathReservation.ShopReservationInfo != null)
            {
                reservation.ShopReservationInfo = bookingPathReservation.ShopReservationInfo;
            }
            if (bookingPathReservation.ShopReservationInfo2 != null)
            {
                reservation.ShopReservationInfo2 = bookingPathReservation.ShopReservationInfo2;
                reservation.ShopReservationInfo2.AllowAdditionalTravelers = !session.IsCorporateBooking;
            }

            if (bookingPathReservation.ReservationEmail != null)
            {
                reservation.ReservationEmail = bookingPathReservation.ReservationEmail;
            }

            if (bookingPathReservation.TripInsuranceFile != null && bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo != null)
            {
                reservation.TripInsuranceInfoBookingPath = bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo;
            }
            else
            {
                reservation.TripInsuranceInfoBookingPath = null;
            }
            reservation.AlertMessages = bookingPathReservation.AlertMessages;
            reservation.IsRedirectToSecondaryPayment = bookingPathReservation.IsRedirectToSecondaryPayment;
            reservation.RecordLocator = bookingPathReservation.RecordLocator;
            reservation.Messages = bookingPathReservation.Messages;
            reservation.CheckedbagChargebutton = bookingPathReservation.CheckedbagChargebutton;
            reservation.IsCanadaTravel = _shoppingUtility.GetFlightsByCountryCode(bookingPathReservation?.Trips, "CA");
            _shoppingUtility.SetEligibilityforETCTravelCredit(reservation, session, bookingPathReservation);
            return reservation;
        }

        private TripPriceBreakDown GetPriceBreakDown(Model.Shopping.Reservation reservation)
        {
            //##Price Break Down - Kirti
            var priceBreakDownObj = new TripPriceBreakDown();
            bool hasAward = false;
            string awardPrice = string.Empty;
            string basePrice = string.Empty;
            string totalPrice = string.Empty;
            bool hasOneTimePass = false;
            string oneTimePassCost = string.Empty;
            bool hasFareLock = false;
            double awardPriceValue = 0;
            double basePriceValue = 0;

            if (reservation != null)
            {

                priceBreakDownObj.PriceBreakDownDetails = new PriceBreakDownDetails();
                priceBreakDownObj.PriceBreakDownSummary = new PriceBreakDownSummary();

                foreach (var travelOption in reservation.TravelOptions)
                {
                    if (travelOption.Key.Equals("FareLock"))
                    {
                        hasFareLock = true;

                        priceBreakDownObj.PriceBreakDownDetails.FareLock = new List<PriceBreakDown2Items>();
                        priceBreakDownObj.PriceBreakDownSummary.FareLock = new PriceBreakDown2Items();
                        var fareLockAmount = new PriceBreakDown2Items();
                        foreach (var subItem in travelOption.SubItems)
                        {
                            if (subItem.Key.Equals("FareLockHoldDays"))
                            {

                                fareLockAmount.Text1 = string.Format("{0} {1}", subItem.Amount, "Day FareLock");
                            }
                        }
                        //Row 0 Column 0
                        fareLockAmount.Price1 = travelOption.DisplayAmount;
                        priceBreakDownObj.PriceBreakDownDetails.FareLock.Add(fareLockAmount);
                        priceBreakDownObj.PriceBreakDownSummary.FareLock = fareLockAmount;


                        priceBreakDownObj.PriceBreakDownDetails.FareLock.Add(new PriceBreakDown2Items() { Text1 = "Total due now" });
                        //Row 1 Column 0
                    }
                }

                StringBuilder tripType = new StringBuilder();
                if (reservation.SearchType.Equals("OW"))
                {
                    tripType.Append("Oneway");
                }
                else if (reservation.SearchType.Equals("RT"))
                {
                    tripType.Append("Roundtrip");
                }
                else
                {
                    tripType.Append("MultipleTrip");
                }
                tripType.Append(" (");
                tripType.Append(reservation.NumberOfTravelers);
                tripType.Append(reservation.NumberOfTravelers > 1 ? " travelers)" : " traveler)");

                //row 2 coulum 0

                foreach (var price in reservation.Prices)
                {
                    switch (price.DisplayType)
                    {
                        case "MILES":
                            hasAward = true;
                            awardPrice = price.FormattedDisplayValue;
                            awardPriceValue = price.Value;
                            break;

                        case "TRAVELERPRICE":
                            basePrice = price.FormattedDisplayValue;
                            basePriceValue = price.Value;
                            break;

                        case "TOTAL":
                            totalPrice = price.FormattedDisplayValue;
                            break;

                        case "ONE-TIME PASS":
                            hasOneTimePass = true;
                            oneTimePassCost = price.FormattedDisplayValue;
                            break;

                        case "GRAND TOTAL":
                            if (!hasFareLock)
                                totalPrice = price.FormattedDisplayValue;
                            break;
                    }
                }

                string travelPrice = string.Empty;
                double travelPriceValue = 0;
                //row 2 column 1
                if (hasAward)
                {
                    travelPrice = awardPrice;
                    travelPriceValue = awardPriceValue;
                }
                else
                {
                    travelPrice = basePrice;
                    travelPriceValue = basePriceValue;
                }

                priceBreakDownObj.PriceBreakDownDetails.Trip = new PriceBreakDown2Items() { Text1 = tripType.ToString(), Price1 = travelPrice };

                priceBreakDownObj.PriceBreakDownSummary.TravelOptions = new List<PriceBreakDown2Items>();

                decimal taxNfeesTotal = 0;
                UtilityHelper.BuildTaxesAndFees(reservation, priceBreakDownObj, out taxNfeesTotal);




                if (((reservation.SeatPrices != null && reservation.SeatPrices.Count > 0) ||
                    reservation.TravelOptions != null && reservation.TravelOptions.Count > 0 || hasOneTimePass) && !hasFareLock)
                {
                    priceBreakDownObj.PriceBreakDownDetails.AdditionalServices = new PriceBreakDownAddServices();

                    // Row n+ 5 column 0
                    // Row n+ 5 column 1

                    priceBreakDownObj.PriceBreakDownDetails.AdditionalServices.Seats = new List<PriceBreakDown4Items>();
                    priceBreakDownObj.PriceBreakDownDetails.AdditionalServices.Seats.Add(new PriceBreakDown4Items() { Text1 = "Additional services:" });


                    UtilityHelper.BuildSeatPrices(reservation, priceBreakDownObj);

                    //build travel options
                    UtilityHelper.BuildTravelOptions(reservation, priceBreakDownObj);
                }

                if (hasOneTimePass)
                {
                    priceBreakDownObj.PriceBreakDownDetails.AdditionalServices.OneTimePass = new List<PriceBreakDown2Items>();
                    priceBreakDownObj.PriceBreakDownDetails.AdditionalServices.OneTimePass.Add(new PriceBreakDown2Items() { Text1 = "One-Time Pass", Price1 = oneTimePassCost });

                    priceBreakDownObj.PriceBreakDownSummary.TravelOptions.Add(new PriceBreakDown2Items() { Text1 = "One-Time Pass", Price1 = oneTimePassCost });
                }

                var finalPriceSummary = new PriceBreakDown2Items();

                priceBreakDownObj.PriceBreakDownDetails.Total = new List<PriceBreakDown2Items>();
                priceBreakDownObj.PriceBreakDownSummary.Total = new List<PriceBreakDown2Items>();
                if (hasFareLock)
                {
                    //column 0
                    finalPriceSummary.Text1 = "Total price (held)";
                }
                else
                {

                    //  buildDottedLine(); column 1
                    finalPriceSummary.Text1 = "Total price";
                }
                if (hasAward)
                {
                    //colum 1
                    finalPriceSummary.Price1 = awardPrice;
                    priceBreakDownObj.PriceBreakDownDetails.Total.Add(finalPriceSummary);

                    priceBreakDownObj.PriceBreakDownSummary.Total.Add(new PriceBreakDown2Items() { Price1 = string.Format("+{0}", totalPrice) });

                    priceBreakDownObj.PriceBreakDownSummary.Trip = new List<PriceBreakDown2Items>()
                             {
                                 new PriceBreakDown2Items()
                                 {
                                    Text1 = tripType.ToString(), Price1 = string.Format("${0}", taxNfeesTotal.ToString("F"))
                                 }

                             };

                }
                else
                {
                    //column 1
                    finalPriceSummary.Price1 = totalPrice;
                    priceBreakDownObj.PriceBreakDownDetails.Total.Add(new PriceBreakDown2Items() { Text1 = totalPrice });

                    priceBreakDownObj.PriceBreakDownSummary.Trip = new List<PriceBreakDown2Items>()
                             {
                                new PriceBreakDown2Items()
                                {
                                  Text1 = tripType.ToString(), Price1 = string.Format("${0}", (travelPriceValue + Convert.ToDouble(taxNfeesTotal)).ToString("F"))
                                }

                             };
                }


                priceBreakDownObj.PriceBreakDownSummary.Total.Add(finalPriceSummary);

            }
            return priceBreakDownObj;
        }

        private void LogInternlineSeatRepriceFailures(MOBShoppingCart shoppingCart, List<MOBSHOPTrip> trips, MOBRequest request, string sessionId)
        {
            try
            {
                if (shoppingCart?.OmniCart?.OmniCartPricingInfos?.Any(omnicartRepriceInfo => omnicartRepriceInfo.Product == "SEATS") == true)
                {
                    var allSupportedOAFlights = trips?.SelectMany(trip => trip.FlattenedFlights.FirstOrDefault()?.Flights).Where(flight => ConfigUtility.IsSeatMapSupportedOa(flight.OperatingCarrier, flight.MarketingCarrier));
                    if (allSupportedOAFlights != null && allSupportedOAFlights.Count() > 0)
                    {
                        //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "SelectOmniCartSavedTrips", "InterlineSeatRepricingFailure", request.Application.Id, request.Application.Version.Major, request.DeviceId, , true, true));
                        _logger.LogInformation("SelectOmniCartSavedTrips {Trace} and {sessionId}", String.Join(",", allSupportedOAFlights.Select(oaFlight => oaFlight.OperatingCarrier + '-' + oaFlight.MarketingCarrier)), sessionId);

                    }
                }
            }
            catch
            {

            }

        }
        private bool IsRepricingFailedByProduct(FlightReservationResponse flightReservation, string product)
        {
            return flightReservation?
                               .DisplayCart?
                               .OmniCartRepricingInfos?
                               .Any(OmniCartPricingInfo => OmniCartPricingInfo.Product.ToUpper() == product && (!string.IsNullOrEmpty(OmniCartPricingInfo.Status) && OmniCartPricingInfo.Status.ToUpper() == "FAILED" || OmniCartPricingInfo.Status.ToUpper() == "PARTIAL")) == true;
        }
        private bool IsBundlesSelectedForAlltrips(FlightReservationResponse flightReservationResponse)
        {
            if (flightReservationResponse.DisplayCart?.TravelOptions != null &&
                flightReservationResponse.DisplayCart.DisplayTrips != null)
            {
                var bundleSelectedTripdIds = flightReservationResponse.DisplayCart?.TravelOptions
                                                                                   .Where(to => to.Type == "BE")
                                                                                   .SelectMany(x => x.SubItems) //get all the subitems for different traveloption with type as "BE"
                                                                                   .Where(si => si.Amount > 0 && !string.IsNullOrEmpty(si.SegmentNumber) && !string.IsNullOrEmpty(si.TripIndex))//get only subitems with amount greater than 0 and segmentnumber and trip inde is not null
                                                                                   .Select(si => si.TripIndex) //get all the trip ids
                                                                                   .Distinct() //get distinct trip ids
                                                                                   .ToList();
                var tripids = flightReservationResponse.DisplayCart.DisplayTrips.Select(trips => trips.Index.ToString()).ToList();

                return !_configuration.GetValue<bool>("DisableBundleOAFix") ? bundleSelectedTripdIds.Count().Equals(BundleEligibleTripsCount(flightReservationResponse)) : bundleSelectedTripdIds.Count().Equals(tripids.Count());
            }
            return false;
        }
        private int BundleEligibleTripsCount(FlightReservationResponse flightReservationResponse)
        {
            int tripCount = 0;

            var unitedCarrierCodes = _configuration.GetValue<string>("UnitedCarrierCodes").Split('|');
            foreach (var segments in flightReservationResponse.Reservation.FlightSegments.GroupBy(fs => fs.TripNumber))
            {
                if (segments.All(segment => unitedCarrierCodes.Contains(segment.FlightSegment?.OperatingAirlineCode)))
                {
                    tripCount++;
                }
            }
            return tripCount;
        }
        private string GetNavigateToScreenValue(MOBShoppingCart shoppingCart, FlightReservationResponse flightReservationResponse)
        {
            string navigateToScreenValue = shoppingCart?.OmniCart?.NavigateToScreen;

            if (IsRepricingFailedByProduct(flightReservationResponse, "BUNDLE")) //If Bundle Repricing failed we always need to go to Traveloptions page as we need to show the bundle repricing failure message
            {
                return MOBNavigationToScreen.TRAVELOPTIONS.ToString();
            }

            if (IsRepricingFailedByProduct(flightReservationResponse, "SEATS")) //If SEATS Repricing failed we always need to go to Seats page as we need to show the Seats repricing failure message
            {
                return MOBNavigationToScreen.SEATS.ToString();
            }

            if (IsBundleRegistered(flightReservationResponse)) //If Bundles are registered Check if the Bundles are assigned for all the trips
            {
                navigateToScreenValue = IsBundlesSelectedForAlltrips(flightReservationResponse)
                                                                                                  ? MOBNavigationToScreen.SEATS.ToString()
                                                                                                  : MOBNavigationToScreen.TRAVELOPTIONS.ToString();
            }

            if (IsSeatsRegistered(flightReservationResponse)) //If Seats Registered Need to check 
            {
                if (shoppingCart.OmniCart.NavigateToSeatmapSegmentNumber > 0) //Partial seatfailed we will go to particular segment  (This would always have value where we navigate to while finding this value it will skip the OA flight segments one)
                {
                    navigateToScreenValue = MOBNavigationToScreen.SEATS.ToString();
                }
                else
                {
                    navigateToScreenValue = MOBNavigationToScreen.FINALRTI.ToString();
                }
            }

            return navigateToScreenValue;
        }
        private void AssignOmnicartRepricingInfo(MOBShoppingCart shoppingCart, FlightReservationResponse flightReservationResponse)
        {

            if (flightReservationResponse?.DisplayCart?.OmniCartRepricingInfos != null)
            {
                shoppingCart.OmniCart = shoppingCart.OmniCart ?? new Model.Shopping.Cart();
                List<MOBOmniCartRepricingInfo> repricingInfos = new List<MOBOmniCartRepricingInfo>();
                foreach (var warning in flightReservationResponse?.DisplayCart?.OmniCartRepricingInfos.Where(repriceItem => !string.IsNullOrEmpty(repriceItem.Status) && (repriceItem.Status.ToUpper() == "FAILED" || repriceItem.Status.ToUpper() == "PARTIAL")))
                {
                    switch (warning.Product.ToUpper())
                    {
                        case "BUNDLE":
                            repricingInfos.Add(new MOBOmniCartRepricingInfo
                            {
                                Product = warning.Product.ToUpper(),
                                RepriceAlertMessage = new MOBSection
                                {
                                    Text1 = _configuration.GetValue<string>("OmnicartRepriceInfoHeadertext"),

                                    Text2 = _configuration.GetValue<string>("OmnicartRepriceInfomessageForBundles"),
                                    MessageType = MESSAGETYPES.INFORMATION.ToString()
                                }
                            });
                            break;
                        case "SEATS":
                            AssignFailedSegmentInfo(warning, repricingInfos);
                            break;
                        default:
                            break;
                    }
                }
                shoppingCart.OmniCart.OmniCartPricingInfos = repricingInfos;
            }
        }
        public async Task<MOBGetOmniCartSavedTripsResponse> GetOmniCartSavedTrips(Session session, MOBSHOPUnfinishedBookingRequestBase request)
        {
            var ccePersonalizationRequest = BuildccePersonalizationRequest(request);
            ContextualCommResponse response = new ContextualCommResponse();

            string jsonRequest = JsonConvert.SerializeObject(ccePersonalizationRequest);
            string jsonResponse = await _shoppingCcePromoService.ChasePromoFromCCE(session.Token, jsonRequest, session.SessionId);
            //utility.MakeHTTPPost(session.SessionId, request.DeviceId, "CCEPersonalisation", request.Application, session.Token, url, jsonRequest);

            MOBGetOmniCartSavedTripsResponse savedTripResponse = new MOBGetOmniCartSavedTripsResponse();
            List<CCEFlightReservationResponseByCartId> cceFlightReservationResponse = new List<CCEFlightReservationResponseByCartId>();
            if (!string.IsNullOrEmpty(jsonResponse))
            {

                response = JsonSerializer.Deserialize<ContextualCommResponse>(jsonResponse);

                if (response.Components != null && response.Components.Any())
                {
                    var cceComponent = response.Components;

                    if (cceComponent != null)
                    {
                        foreach (var component in cceComponent)
                        {
                            string componentName = component.Name;
                            BuildContent(savedTripResponse, component, request.Application.Id, request.Application.Version.Major);
                            if (component != null && component.ContextualElements != null)
                            {
                                foreach (var item in component.ContextualElements)
                                {
                                    var contextualItemValueJson = Newtonsoft.Json.JsonConvert.SerializeObject(item.Value);
                                    FlightReservationContextualMessage contextualItemValue = Newtonsoft.Json.JsonConvert.DeserializeObject<FlightReservationContextualMessage>(contextualItemValueJson);

                                    if (componentName == _configuration.GetValue<string>("CCEOmnicartGetSavedTripsComponet"))
                                        BuildOmniCartSavedTrips(savedTripResponse, cceFlightReservationResponse, contextualItemValue);

                                    if (IsEnableOmniCartReleaseCandidate4BChanges(request.Application.Id, request.Application.Version.Major) && componentName == _configuration.GetValue<string>("CCEOmnicartGetSavedTripsComponetV2"))
                                        BuildOmniCartSavedTripsV2(savedTripResponse, cceFlightReservationResponse, contextualItemValue);

                                    if (componentName == _configuration.GetValue<string>("CCEOmnicartSavedTripsAlertComponent"))
                                        BuildWarningMessages(savedTripResponse, contextualItemValue);

                                    if (componentName == _configuration.GetValue<string>("CCEOmnicartOmniCartIndicatorComponent"))
                                        BuildOmniCartIndicator(savedTripResponse, contextualItemValue);
                                }
                            }
                            else
                            {
                                if (response.Error != null && response.Error.Count > 0)
                                {
                                    throw new Exception(_configuration.GetValue<string>("OmnicartExceptionMessage"));
                                }
                            }
                        }

                        if (IsAnyErrorExistsInCCEResponse(response))
                        {
                            _logger.LogError("GetOmniCartSavedTrips - CCE-PartialFailure with response {@Response}", JsonConvert.SerializeObject(response));
                        }
                        if (cceFlightReservationResponse != null && cceFlightReservationResponse.Count > 0)
                        {
                            await _sessionHelperService.SaveSession<List<CCEFlightReservationResponseByCartId>>(cceFlightReservationResponse, session.SessionId, new List<string> { session.SessionId, new CCEFlightReservationResponseByCartId().ObjectName }, new CCEFlightReservationResponseByCartId().ObjectName).ConfigureAwait(false);
                        }
                    }

                }
                else
                {
                    throw new Exception(_configuration.GetValue<string>("OmnicartExceptionMessage"));
                }
                savedTripResponse.SessionId = session.SessionId;
            }
            return savedTripResponse;
        }
        public void BuildOmniCartSavedTrips(MOBGetOmniCartSavedTripsResponse savedTripResponse, List<CCEFlightReservationResponseByCartId> cceFlightReservationResponse, FlightReservationContextualMessage contextualItemValue)
        {
            if (savedTripResponse.OmniCartSavedTrips == null)
                savedTripResponse.OmniCartSavedTrips = new List<MOBOmniCartSavedTrip>();
            MOBOmniCartSavedTrip savedTrip = new MOBOmniCartSavedTrip();
            if (contextualItemValue?.Content?.Links != null)
            {
                savedTrip.Links = BuildLinks(contextualItemValue.Content?.Links);
            }
            #region Building the Trip Details
            if (contextualItemValue?.Content?.SubContents != null)
            {
                var flightReservationResponseJson = JsonConvert.SerializeObject(contextualItemValue.FlightReservationData);
                FlightReservationResponse cslFlightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponseJson);
                savedTrip.CartId = cslFlightReservationResponse?.CartId;
                savedTrip.Trips = BuildSavedTripDetails(contextualItemValue.Content?.SubContents);
                cceFlightReservationResponse.Add(new CCEFlightReservationResponseByCartId
                {
                    CartId = cslFlightReservationResponse?.CartId,
                    CslFlightReservationResponse = cslFlightReservationResponse
                });
            }
            #endregion Building the Trip Details
            if (savedTrip.Links != null && savedTrip.Trips != null)
            {
                savedTripResponse.OmniCartSavedTrips.Add(savedTrip);
            }
        }
        public static List<MOBOmniCartSavedTripDetails> BuildSavedTripDetails(Collection<United.Service.Presentation.PersonalizationModel.SubContent> subContents)
        {
            if (subContents != null)
            {
                List<MOBOmniCartSavedTripDetails> savedTripDetails = new List<MOBOmniCartSavedTripDetails>();
                subContents.ForEach(subContent =>
                {
                    savedTripDetails.Add(new MOBOmniCartSavedTripDetails
                    {
                        Title = subContent.Title,
                        SubTitle = subContent.SubTitle
                    });
                });
                return savedTripDetails;
            }
            return null;
        }
        public bool IsEnableOmniCartReleaseCandidate4BChanges(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableOmniCartReleaseCandidate4BChanges") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableOmniCartReleaseCandidate4BChanges_AppVersion"), _configuration.GetValue<string>("iPhone_EnableOmniCartReleaseCandidate4BChanges_AppVersion"));
        }
        public void BuildOmniCartSavedTripsV2(MOBGetOmniCartSavedTripsResponse savedTripResponse, List<CCEFlightReservationResponseByCartId> cceFlightReservationResponse, FlightReservationContextualMessage contextualItemValue)
        {
            if (savedTripResponse.OmniCartSavedTrips == null)
                savedTripResponse.OmniCartSavedTrips = new List<MOBOmniCartSavedTrip>();
            MOBOmniCartSavedTrip savedTrip = new MOBOmniCartSavedTrip();
            if (contextualItemValue?.Content?.Links != null)
            {
                savedTrip.Links = BuildLinks(contextualItemValue.Content?.Links);
            }
            #region Building the Trip Details
            if (contextualItemValue?.Content?.SubContents != null)
            {
                var flightReservationResponseJson = JsonConvert.SerializeObject(contextualItemValue.FlightReservationData);
                FlightReservationResponse cslFlightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponseJson);
                savedTrip.CartId = cslFlightReservationResponse?.CartId;
                savedTrip.TripInfo = BuildShowDetailsInfo(contextualItemValue.Content);
                cceFlightReservationResponse.Add(new CCEFlightReservationResponseByCartId
                {
                    CartId = cslFlightReservationResponse?.CartId,
                    CslFlightReservationResponse = cslFlightReservationResponse
                });
            }
            #endregion Building the Trip Details
            if (savedTrip.Links != null && savedTrip.TripInfo?.FlightDetails != null)
            {
                savedTripResponse.OmniCartSavedTrips.Add(savedTrip);
            }
        }
        public async Task<MOBSHOPSelectTripResponse> BuildCCEFlightReservationResponseByCartId(MOBSHOPSelectUnfinishedBookingRequest request, string token, string sessionId )
        {
            MOBSHOPSelectTripResponse apiResponse = null;
            try
            {
                var jsonResponse = await _omniChannelCartService.GetFlightReservationData(request?.CartId, true, token, request?.TransactionId, sessionId);
                United.OmniChannelCart.Model.OmniChannelCartServiceResponse response = new United.OmniChannelCart.Model.OmniChannelCartServiceResponse();
                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    response = JsonConvert.DeserializeObject<United.OmniChannelCart.Model.OmniChannelCartServiceResponse>(jsonResponse);
                    #region Omnicart Retargeting happy path
                    if ((response != null && response.FlightReservationData != null && (response.FlightReservationData[0].Errors == null || response.FlightReservationData[0].Errors.Count == 0 || !response.FlightReservationData[0].Errors.Any()) && !string.IsNullOrEmpty(response.Status) && response.Status.ToUpper() == "SUCCESSFUL"))
                    {
                        var flightReservationResponseJson = JsonConvert.SerializeObject(response.FlightReservationData);
                        Collection<FlightReservationResponse> cslFlightReservationResponse = JsonConvert.DeserializeObject<Collection<FlightReservationResponse>>(flightReservationResponseJson);
                        List<CCEFlightReservationResponseByCartId> cceFlightReservationResponse = new List<CCEFlightReservationResponseByCartId>();
                        cceFlightReservationResponse.Add(new CCEFlightReservationResponseByCartId
                        {
                            CartId = cslFlightReservationResponse[0]?.CartId,
                            CslFlightReservationResponse = cslFlightReservationResponse[0]
                        });

                        await _sessionHelperService.SaveSession<List<CCEFlightReservationResponseByCartId>>(cceFlightReservationResponse, sessionId, new List<string> { sessionId, new CCEFlightReservationResponseByCartId().ObjectName }, new CCEFlightReservationResponseByCartId().ObjectName);
                    }
                    #endregion
                    #region Omnicart Retargeting Negative scenarios handling
                    else
                    {
                        return await OmnicartRetargetingNegativeFlow(request, token, response);
                    }
                    #endregion
                }
                else
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("OmnicartExceptionMessage"));
                }
            }
            catch (MOBUnitedException uex)
            {
                throw uex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return apiResponse;
        }
        public async Task<MOBSHOPSelectTripResponse> OmnicartRetargetingNegativeFlow(MOBSHOPSelectUnfinishedBookingRequest request, string token, United.OmniChannelCart.Model.OmniChannelCartServiceResponse response)
        {
            MOBSHOPSelectTripResponse apiResponse = new MOBSHOPSelectTripResponse();
            apiResponse.ShoppingCart = new MOBShoppingCart();
            apiResponse.ShoppingCart.OmniCart = new Model.Shopping.Cart();
            if (response.Meta?.FirstOrDefault(m => m.Key == "GetOmniCartFromDynamoDb")?.Value == "Not found")//For invalid cartId or flight departed or purchased cartId scenarios,if cartId is not found,should land on booking main
            {
                apiResponse.ShoppingCart.OmniCart.NavigateToScreen = MOBNavigationToScreen.BOOKINGMAIN.ToString();
                _logger.LogWarning("Omnicart-Deeplink cartId&Reprice call failed-cartId not found");
            }
            else
            {
                var cslJsonResponse = await _omniChannelCartService.GetCartIdInformation(request?.CartId, token, request?.TransactionId, request?.SessionId);
                MOBSHOPShopRequest shopRequest = new MOBSHOPShopRequest();
                United.OmniChannelCart.Model.OmniChannelCartServiceResponse cslResponse = new United.OmniChannelCart.Model.OmniChannelCartServiceResponse();
                cslResponse = JsonConvert.DeserializeObject<United.OmniChannelCart.Model.OmniChannelCartServiceResponse>(cslJsonResponse);
                if (cslResponse != null && cslResponse.Data != null && cslResponse.Data?.Count > 0 && cslResponse.Data[0]?.ShopRequests != null && cslResponse.Data[0]?.ShopRequests?.Count > 0)
                {
                    ShopRequest cslShopRequest = cslResponse.Data[0].ShopRequests[0];
                    shopRequest = ShopStaticUtility.BuildMobShopRequest(cslShopRequest);//Forming shop request from csl cartId response to land on FSR
                    shopRequest.Flow = FlowType.OMNICARTDEEPLINK.ToString();
                    apiResponse.ShopRequest = shopRequest;
                    apiResponse.ShoppingCart.OmniCart.NavigateToScreen = MOBNavigationToScreen.BOOKINGFSR.ToString();
                    _logger.LogWarning("Omnicart-Deeplink cartId&Reprice call failed-FSR Landing");
                }
                else
                {
                    apiResponse.ShoppingCart.OmniCart.NavigateToScreen = MOBNavigationToScreen.BOOKINGMAIN.ToString();
                    _logger.LogWarning("Omnicart-Deeplink cartId&Reprice, cartId calls failed");
                }
            }
            return apiResponse;
        }
        public MOBOmniCartSavedTripDetails BuildShowDetailsInfo(ContextualContent content)
        {
            if (content?.SubContents != null)
            {
                MOBOmniCartSavedTripDetails showDetailsInfo = new MOBOmniCartSavedTripDetails();
                showDetailsInfo.Title = content.Title;
                showDetailsInfo.SubTitle = content.Title2;
                showDetailsInfo.SubTitle2 = content.SubTitle;
                content?.SubContents.ForEach(subContent =>
                {
                    if (showDetailsInfo.FlightDetails == null)
                    {
                        showDetailsInfo.FlightDetails = new List<MOBOmniCartSavedTripDetails>();
                    }
                    showDetailsInfo.FlightDetails.Add(new MOBOmniCartSavedTripDetails
                    {
                        Title = subContent.Title,
                        SubTitle = subContent.SubTitle,
                        SelectedAncillaries = BuildSelectedAncillaries(subContent),
                        ConnectionTime = GetConnectionTime(subContent)
                    });
                });
                return showDetailsInfo;
            }
            return null;
        }
        public List<string> BuildSelectedAncillaries(SubContent subContent)
        {

            if (subContent.SubContents != null)
            {
                var selectedAncillaries = new List<string>();
                subContent.SubContents.ForEach(sbContent =>
                {
                    selectedAncillaries.Add(sbContent.Title);
                });
                return selectedAncillaries;
            }
            return null;
        }
        public string GetConnectionTime(SubContent subContent)
        {
            if (subContent.Params != null
                && subContent.Params.Any(param => param.Key.ToUpper().Equals("CONNECTIONTIME")))
            {
                var connectionTime = subContent.Params.FirstOrDefault(param => param.Key.ToUpper().Equals("CONNECTIONTIME")).Value;

                if (!String.IsNullOrEmpty(connectionTime))
                    return $"{_configuration.GetValue<string>("ConnectionText")}{subContent.Params.First(param => param.Key.ToUpper().Equals("CONNECTIONTIME")).Value}";
            }
            return string.Empty;
        }
        public void BuildOmniCartIndicator(MOBGetOmniCartSavedTripsResponse savedTripResponse, ContextualMessage contextualItemValue)
        {
            if (contextualItemValue.Params != null && contextualItemValue.Params.Any(p => p.Key == "ENABLECART"))
            {
                savedTripResponse.ShowOmniCartIndicator = Convert.ToBoolean(contextualItemValue.Params.First(p => p.Key == "ENABLECART").Value);
            }
        }
        public void BuildWarningMessages(MOBGetOmniCartSavedTripsResponse savedTripResponse, ContextualMessage contextualItemValue)
        {
            if (contextualItemValue?.Content != null)
            {
                savedTripResponse.WarningMessages = new MOBSection
                {
                    Text2 = contextualItemValue.Content.BodyText,
                    MessageType = GetAlertMessageType(contextualItemValue.Params)
                };
            }
        }
        public string GetAlertMessageType(Dictionary<string, string> Params)
        {
            if (Params != null && Params.Any(param => param.Key.ToUpper().Equals("ALERTTYPE")))
            {
                return Params.First(param => param.Key.ToUpper().Equals("ALERTTYPE")).Value.ToUpper();
            }
            return "";
        }
        public void BuildContent(MOBGetOmniCartSavedTripsResponse savedTripResponse, PersonalizationComponent component,int appId, string appVersion)
        {
            #region Content 
            if (component.Content != null)
            {
                savedTripResponse.CartTitle = component.Content.Title;
                savedTripResponse.Header = component.Content.SubTitle;
                savedTripResponse.Footer = component.Content.BodyText;
                if (component.Content?.Links != null)
                {
                    savedTripResponse.Links = BuildLinks(component.Content?.Links);
                }
            }
            if (!_configuration.GetValue<bool>("DisableSNSLinkInOmnicartFix") && !GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("AndroidSNSLinkInOmnicartFixVersion"), _configuration.GetValue<string>("iPhoneSNSLinkInOmnicartFixVersion"))
                && !string.IsNullOrEmpty(component.Name) && (component.Name == _configuration.GetValue<string>("CCEOmnicartGetSavedTripsComponetV2") || component.Name == _configuration.GetValue<string>("CCEOmnicartEmptySavedTripsComponent")))
            {
                if (component.Content != null && component.Content?.Links != null && component.Content?.Links.Any(l =>  l.LinkStyle == "SNS_LINK" && l.LinkText == "Start new search") == false)
                {
                    MOBLink startNewSearchLink = new MOBLink
                    {
                        ComponentTitle = "SNS_LINK",
                        LinkText = "Start new search"
                    };
                    if(savedTripResponse.Links ==null)
                    {
                        savedTripResponse.Links = new List<MOBLink>();
                    }
                    savedTripResponse.Links?.Add(startNewSearchLink);
                }
            }
            #endregion Content 
        }
        public List<MOBLink> BuildLinks(Collection<ContextualLinkType> cceLinks)
        {
            if (cceLinks != null)
            {
                List<MOBLink> links = new List<MOBLink>();
                cceLinks.ForEach(link =>
                {
                    links.Add(new MOBLink
                    {
                        ComponentTitle = link.LinkStyle, //GetComponentTitle(link.LinkStyle),
                        LinkText = link.LinkText
                    });
                });
                return links;
            }
            return null;
        }
        private void AssignFailedSegmentInfo(OmniCartRepricingInfo omnicartRepricingInfo, List<MOBOmniCartRepricingInfo> repricingInfos)
        {
            repricingInfos.Add(new MOBOmniCartRepricingInfo
            {
                Product = "SEATS",
                RepriceAlertMessage = new MOBSection
                {
                    Text1 = _configuration.GetValue<string>("OmnicartRepriceInfoHeadertext"),
                    Text2 = _configuration.GetValue<string>("OmnicartRepriceInfomessageForSeats"),
                    MessageType = MESSAGETYPES.INFORMATION.ToString()
                }
            });
            if (omnicartRepricingInfo?.Segments != null)
            {
                if (omnicartRepricingInfo.Segments.Any(segment => segment.Status == "FAILED"))
                {
                    repricingInfos.First(repriceinfo => repriceinfo.Product == "SEATS").Segments = AssignFailedSegmentInfo(omnicartRepricingInfo.Segments.Where(segment => segment.Status?.ToUpper() == "FAILED")?.ToList());
                }
            }
        }
        private List<MOBOmniCartRepricingSegmentInfo> AssignFailedSegmentInfo(IEnumerable<United.Services.FlightShopping.Common.DisplayCart.Segment> segments)
        {
            List<MOBOmniCartRepricingSegmentInfo> segmentsList = new List<MOBOmniCartRepricingSegmentInfo>();
            foreach (var segment in segments)
            {
                segmentsList.Add(new MOBOmniCartRepricingSegmentInfo
                {
                    SegmentNumber = Convert.ToInt32(segment.SegmentNumber)

                });
            }
            return segmentsList;
        }
        private int AssignNavigateToSeatmapSegmentNumber(FlightReservationResponse flightReservationResponse)
        {
            int travelerCount = flightReservationResponse.Reservation.Travelers.Where(traveler => traveler.Person?.Type != "INF").Count();
            foreach (var flightsegment in flightReservationResponse.Reservation.FlightSegments)
            {
                //Skip the segment if not supported or readonly seatmap Segment 
                if (IsOANotSupportedOrReadonlySeatMap(flightsegment))
                {
                    continue;
                }
                var selectedSeatsForSegment = flightReservationResponse.DisplayCart.DisplaySeats.Where(displaySeat => !string.IsNullOrEmpty(displaySeat.Seat) && displaySeat.OriginalSegmentIndex == flightsegment.SegmentNumber);
                if (selectedSeatsForSegment == null
                    || (selectedSeatsForSegment != null && selectedSeatsForSegment.Count() != travelerCount)) //return the segment if seat not selected for all the passengers
                {
                    return flightsegment.SegmentNumber;
                }
            }
            return 0;
        }
        private bool IsOANotSupportedOrReadonlySeatMap(Service.Presentation.SegmentModel.ReservationFlightSegment flightsegment)
        {
            if (IsUAOperatedFlight(flightsegment?.FlightSegment?.OperatingAirlineCode))
            {
                return false;
            }
            else
            {
                bool isSeatMapSupportedOa = ConfigUtility.IsSeatMapSupportedOa(flightsegment?.FlightSegment?.OperatingAirlineCode, flightsegment?.FlightSegment?.MarketedFlightSegment?.FirstOrDefault()?.MarketingAirlineCode);
                if (IsReadOnlySeatMap(flightsegment.FlightSegment.OperatingAirlineCode) || !isSeatMapSupportedOa)
                {
                    return true;
                }
            }
            return false;
        }
        private bool IsUAOperatedFlight(string operatingCarrier)
        {
            var unitedCarrierCodes = _configuration.GetValue<string>("UnitedCarrierCodes").Split('|');
            return unitedCarrierCodes.Contains(operatingCarrier);
        }
        private bool IsReadOnlySeatMap(string operatingCarrier)
        {
            var readOnlySeatMapAirlines = _configuration.GetValue<string>("ReadonlySeatMapinBookingPathOAAirlines")?.Split(',');
            return readOnlySeatMapAirlines?.Contains(operatingCarrier) == true;
        }
        private BookingBundlesRequest BuildBundleOfferRequest(MOBSHOPSelectUnfinishedBookingRequest request)
        {
            var bundleOfferRequest = new BookingBundlesRequest
            {
                Application = request.Application,
                SessionId = request.SessionId,
                CartId = request.CartId,
                DeviceId = request.DeviceId,
                Flow = FlowType.BOOKING.ToString(),
                TransactionId = request.TransactionId
            };
            return bundleOfferRequest;
        }
        private void GetBundleOffer(MOBSHOPSelectUnfinishedBookingRequest request, bool isOmniCartSaveTripFlow)
        {

            System.Threading.Tasks.Task.Factory.StartNew(async() =>
            {
                try
                {
                    if (!_configuration.GetValue<bool>("DisableBundlesMethodsInCommonPlace"))
                    {
                        await _shopBundleService.GetBundleOffer(BuildBundleOfferRequest(request), throwExceptionWhenSaveOmniCartFlow: isOmniCartSaveTripFlow);
                    }
                    else
                    {
                        await GetBundleOffer(BuildBundleOfferRequest(request), throwExceptionWhenSaveOmniCartFlow: isOmniCartSaveTripFlow);
                    }                    
                }
                catch (System.Net.WebException wex)
                {

                    throw wex;
                }
                catch (MOBUnitedException uaex)
                {
                    throw uaex;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    //if (_shopping.LogEntries != null & _shopping.LogEntries.Count > 0)
                    //{
                    //    LogEntries.AddRange(_shopping.LogEntries);
                    //}
                }
            });
        }
        private static DynamicOfferDetailRequest BuildDynamicOfferRequestForBundles(BookingBundlesRequest bundleRequest, Session session)
        {
            var isAward = false;
            if (ConfigUtility.IsEnableSAFFeature(session))
            {
                isAward = session?.IsAward ?? false;
            }
            var characteristics = new Collection<Characteristic>() {
                new Characteristic(){ Code ="TKT_PRICE" },
                new Characteristic(){ Code ="MILES_NEEDED", Value="N" },
                new Characteristic(){ Code ="CartId", Value=bundleRequest.CartId },
                new Characteristic(){ Code ="Context", Value="TO" },
                new Characteristic(){ Code ="New", Value="True" },
                new Characteristic(){ Code ="REVENUE", Value= isAward ? "False" : "True" },
                new Characteristic(){ Code ="IsEnabledThroughCCE", Value="True" }
            };

            var dynamicOfferRequest = new DynamicOfferDetailRequest()
            {
                Characteristics = characteristics,
                CountryCode = "US",
                CurrencyCode = "USD",
                IsAwardReservation = isAward ? "True" : "False",
                TicketingCountryCode = "US",
                Requester = new ServiceClient()
                {
                    Requestor = new Requestor()
                    {
                        ChannelName = "MBE",
                        LanguageCode = "en"
                    }
                }
            };
            return dynamicOfferRequest;
        }

        private void PopulateBundleTnCForSAF(BookingBundlesResponse response, SDLBody[] sDLBodies)
        {
            var sdlBodyForSAF = sDLBodies.FirstOrDefault(body => string.Equals(body.name, "sfc", StringComparison.OrdinalIgnoreCase));
            var bundleTnCContent = sdlBodyForSAF?.content?.FirstOrDefault(content => string.Equals(content.name, "sfc-tnc", StringComparison.OrdinalIgnoreCase));
            MOBMobileCMSContentMessages objtermsandconditions = new MOBMobileCMSContentMessages();
            response.AdditionalTermsAndCondition = new MOBMobileCMSContentMessages();
            response.AdditionalTermsAndCondition.Title = "Terms and Conditions";
            response.AdditionalTermsAndCondition.LocationCode = response.AdditionalTermsAndCondition.ContentShort = "";
            response.AdditionalTermsAndCondition.HeadLine = sdlBodyForSAF?.metadata.nav_title;
            if (!string.IsNullOrEmpty(bundleTnCContent?.content?.body_text))
            {
                response.AdditionalTermsAndCondition.ContentFull = bundleTnCContent.content.body_text;

            }
        }
        private string GetTripId(Service.Presentation.ProductModel.SubProduct subProduct, Collection<Service.Presentation.ProductModel.ProductOriginDestinationOption> ODOptions)
        {
            var odOption = ODOptions.First(od => od.ID == subProduct.Association.ODMappings.First().RefID);
            return odOption.FlightSegments.First().TripIndicator;
        }
        private string GetOriginDestinationDesc(Service.Presentation.ProductModel.SubProduct subProduct, Collection<Service.Presentation.ProductModel.ProductOriginDestinationOption> ODOptions, int price)
        {
            var odOption = ODOptions.First(od => od.ID == subProduct.Association.ODMappings.First().RefID);
            string odDesc = string.Concat(odOption.FlightSegments.First().DepartureAirport.IATACode, " - ", odOption.FlightSegments.Last().ArrivalAirport.IATACode);
            if (subProduct.Prices?.Count > 0)
            {
                return string.Concat(odDesc, " | ", "$", price, "/person");
            }
            return String.Concat(odDesc, " | ", "Not available");

        }
        private async Task<BookingBundlesResponse> GetBundleOffer(BookingBundlesRequest bundleRequest, bool throwExceptionWhenSaveOmniCartFlow = false)

        {
            BookingBundlesResponse response = new BookingBundlesResponse(_configuration);
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(bundleRequest.SessionId, session.ObjectName, new List<string>() { bundleRequest.SessionId, session.ObjectName }).ConfigureAwait(false);
            string logAction = "dynamicOfferdetail";
            if (session == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }

            DynamicOfferDetailRequest dynamicOfferRequest = BuildDynamicOfferRequestForBundles(bundleRequest , session);
            string jsonRequest = JsonConvert.SerializeObject(dynamicOfferRequest);
            var jsonResponse = await _bundleOfferService.DynamicOfferdetail<DynamicOfferDetailResponse>(session.Token, session.SessionId, logAction, jsonRequest).ConfigureAwait(false);

            if (!jsonResponse.IsNullOrEmpty())
            {
                List<BundleProduct> objbundleproduct = new List<BundleProduct>();

                //_logger.LogInformation("GetBundles_CFOP {response} and {@sessionId}", jsonResponse, bundleRequest.SessionId);

                var productDetails = jsonResponse.response.Offers?.Where(offer => offer.ProductInformation != null)
                                                    .SelectMany(offer => offer.ProductInformation.ProductDetails);
                if (jsonResponse.response.ResponseData == null && throwExceptionWhenSaveOmniCartFlow)
                {
                    throw new Exception("CCEResponse data is empty or not loaded");
                }
                if (jsonResponse.response.ResponseData != null && productDetails.Any())
                {
                    var sdlResponseDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(jsonResponse.response.ResponseData);
                    SDLContentResponseData sdlResponseData = Newtonsoft.Json.JsonConvert.DeserializeObject<SDLContentResponseData>(sdlResponseDataJson);
                    var sdlBundleData = sdlResponseData?.Results;
                    // MOBILE-25395: SAF
                    var safCode = _configuration.GetValue<string>("SAFCode");

                    //TnC
                    var responseUndleTnc = PopulateBundleTnC(response, sdlResponseData.Body);

                    response.Products = new List<BundleProduct>();
                    for (var index = 0; index < sdlBundleData.Length; index++)
                    {
                        var bundleData = sdlBundleData.ElementAt(index);
                        var productDetail = productDetails.FirstOrDefault(x => x?.Product?.Code == bundleData.Code);
                        // MOBILE-25395: SAF
                        if (bundleData.Code != safCode)
                        {
                            var bundleProduct = new BundleProduct();
                            bundleProduct.ProductIndex = index;
                            bundleProduct.ProductCode = productDetail.Product?.Code;
                            bundleProduct.ProductID = productDetail.Product?.SubProducts?.FirstOrDefault(sp => sp.InEligibleReason == null)?.Prices?.FirstOrDefault()?.ID;
                            bundleProduct.Detail = new BundleDetail();
                            bundleProduct.Detail.OfferTitle = $"Bundle offer {index + 1} includes the following additions to your trip:";
                            bundleProduct.Detail.OfferTrips = new List<BundleOfferTrip>();
                            var bundlePrice = productDetail.Product?
                                                           .SubProducts?.FirstOrDefault(sp => sp.InEligibleReason == null)?
                                                           .Prices?.FirstOrDefault()?
                                                           .PaymentOptions?.FirstOrDefault()?
                                                           .PriceComponents?.FirstOrDefault()?
                                                           .Price?
                                                           .Totals?.FirstOrDefault()?
                                                           .Amount;
                            bundleProduct.Tile = new BundleTile();
                            bundleProduct.Tile.OfferTitle = $"Bundle offer {index + 1}";
                            bundleProduct.Tile.OfferPrice = string.Concat("$", bundlePrice);
                            bundleProduct.Tile.PriceText = string.Concat("+$", bundlePrice, "/person");
                            // if CCE returns Type?.Description then it is considered  
                            if (_shoppingUtility.IsEnableMostPopularBundle(bundleRequest.Application.Id, bundleRequest.Application.Version.Major) && productDetail?.Product?.Type?.Description?.ToUpper().Trim() == "PPLBNDL")
                                bundleProduct.Tile.BundleBadgeText = _configuration.GetValue<string>("MostPopularBundleText");

                            bundleProduct.Detail.OfferDetails = new List<BundleOfferDetail>();
                            bundleProduct.Tile.OfferDescription = new List<string>();
                            bundleProduct.BundleProductCodes = new List<string>();
                            bundleData.Products.ForEach(product =>
                            {
                                bundleProduct.Tile.OfferDescription.Add(product.Name);
                                bundleProduct.BundleProductCodes.Add(product.Code);
                            });


                            #region Build Segment Selection screen for bundles details (Trip Details)
                            var nonNullSubProducts = productDetail.Product != null && productDetail.Product.SubProducts?.Count > 0 ? productDetail.Product.SubProducts.Where(sp => sp != null) : new Collection<Service.Presentation.ProductModel.SubProduct>();
                            foreach (var subProduct in nonNullSubProducts)
                            {

                                BundleOfferTrip offerTrip = new BundleOfferTrip();
                                if (subProduct.Prices?.Count > 0)
                                {
                                    offerTrip.TripProductID = String.Join(",", subProduct.Prices.Select(p => p.ID).ToList()); //As we are getting multiple price items for multipax                     
                                    offerTrip.TripProductIDs = subProduct.Prices.Select(p => p.ID).ToList();
                                }
                                offerTrip.Price = Convert.ToInt32(subProduct
                                                                  .Prices?.FirstOrDefault()?
                                                                  .PaymentOptions?.FirstOrDefault()?
                                                                  .PriceComponents?.FirstOrDefault()?
                                                                  .Price?
                                                                  .Totals?.FirstOrDefault()?
                                                                  .Amount);
                                offerTrip.TripId = GetTripId(subProduct, jsonResponse.response.ODOptions);
                                offerTrip.OriginDestination = GetOriginDestinationDesc(subProduct, jsonResponse.response.ODOptions, offerTrip.Price);
                                bundleProduct.Detail.OfferTrips.Add(offerTrip);

                                PopulateBundleOfferDetails(subProduct, bundleProduct.Detail.OfferDetails, bundleData.Products);
                            }
                            #endregion
                            response.Products.Add(bundleProduct);
                        }
                        else if (ConfigUtility.IsEnableSAFFeature(session))
                        {
                            // MOBILE-25395: SAF
                            //TnC
                            PopulateBundleTnCForSAF(response, sdlResponseData.Body);

                            var bundleProduct = new BundleProduct();
                            bundleProduct.ProductIndex = index;
                            bundleProduct.ProductCode = productDetail.Product?.Code;
                            bundleProduct.ProductName = bundleData.Products?.FirstOrDefault()?.Name ??
                                                        productDetail.Product?.DisplayName;
                            bundleProduct.ProductID = productDetail.Product?.SubProducts?.FirstOrDefault(sp => sp.InEligibleReason == null)?.Prices?.FirstOrDefault()?.ID;
                            bundleProduct.Detail = new BundleDetail();
                            bundleProduct.Detail.OfferTitle = bundleData.Products?.FirstOrDefault()?.OfferTile ??
                                                              productDetail.Product?.DisplayName;
                            bundleProduct.Detail.OfferTrips = new List<BundleOfferTrip>();
                            var bundlePrice = productDetail.Product?
                                                           .SubProducts?.FirstOrDefault(sp => sp.InEligibleReason == null)?
                                                           .Prices?.FirstOrDefault()?
                                                           .PaymentOptions?.FirstOrDefault()?
                                                           .PriceComponents?.FirstOrDefault()?
                                                           .Price?
                                                           .Totals?.FirstOrDefault()?
                                                           .Amount;
                            bundleProduct.Tile = new BundleTile();
                            bundleProduct.Tile.BackGroundColor = MOBStyledColor.Green.GetDescription();
                            if (await _featureSettings.GetFeatureSettingValue("EnableNewBackGroundColor").ConfigureAwait(false) && Utility.Helper.GeneralHelper.IsApplicationVersionGreaterorEqual(bundleRequest.Application.Id, bundleRequest.Application.Version.Major.ToString(), _configuration.GetValue<string>("Android_Atmos2_New_BackGroundColor_AppVersion"), _configuration.GetValue<string>("iPhone_Atmos2_New_BackGroundColor_AppVersion")))
                            {
                                bundleProduct.Tile.BackGroundColor = MOBStyledColor.AlertGreen.GetDescription();
                            }
                            bundleProduct.Tile.OfferTitle = bundleData.Name ??
                                                            productDetail.Product?.DisplayName;
                            bundleProduct.Tile.OfferPrice = _configuration.GetValue<bool>("IsEnableTilePriceForSAF") ? string.Concat("$", bundlePrice) : _configuration.GetValue<string>("TravelOptionsPageSAFOfferPrice");
                            bundleProduct.Tile.PriceText = bundleProduct.Tile.OfferPrice;
                            bundleProduct.Tile.OfferDescription = new List<string>();
                            bundleProduct.Detail.OfferDetails = new List<BundleOfferDetail>
                            {
                                new BundleOfferDetail() {
                                    OfferDetailDescription = bundleData.Products?.FirstOrDefault()?.ConfigDetails ?? ""
                                }
                            };
                            bundleProduct.BundleProductCodes = new List<string>();
                            bundleData.Products.ForEach(product =>
                            {
                                bundleProduct.Tile.OfferDescription.Add(product.Description);
                                bundleProduct.BundleProductCodes.Add(product.Code);
                            });
                            if (!_configuration.GetValue<bool>("DisableSAFSliderChanges"))
                            {
                                bundleProduct.Detail.IncrementSliderValue = Convert.ToDouble(_configuration.GetValue<string>("SAFIncrementSliderValue"));
                                bundleProduct.Tile.FromText = _configuration.GetValue<string>("TravelOptionsPageFromText");
                            }


                            #region Build SAF Selection screen for bundles details (Trip Details)

                            var nonNullSubProducts = productDetail.Product != null && productDetail.Product.SubProducts?.Count > 0 ? productDetail.Product.SubProducts.Where(sp => sp != null) : new Collection<Service.Presentation.ProductModel.SubProduct>();
                            foreach (var subProduct in nonNullSubProducts)
                            {

                                BundleOfferTrip offerTrip = new BundleOfferTrip();
                                if (subProduct.Prices?.Count > 0)
                                {
                                    offerTrip.TripProductID = String.Join(",", subProduct.Prices.Select(p => p.ID).ToList()); //As we are getting multiple price items for multipax                     
                                    offerTrip.TripProductIDs = subProduct.Prices.Select(p => p.ID).ToList();
                                }
                                var price = subProduct.Prices?.FirstOrDefault()?
                                                                 .PaymentOptions?.FirstOrDefault()?
                                                                 .PriceComponents?.FirstOrDefault()?
                                                                 .Price?
                                                                 .Totals?.FirstOrDefault()?
                                                                 .Amount;
                                offerTrip.Price = Convert.ToInt32(price);
                                if (!_configuration.GetValue<bool>("DisableSAFSliderChanges"))
                                {
                                    offerTrip.Amount = Convert.ToDouble(price);
                                    if (bundleProduct.Detail.OfferTrips.Count() == 1)
                                        offerTrip.IsDefault = true;
                                }
                                offerTrip.TripId = "10" + subProduct.ID;
                                if (_configuration.GetValue<bool>("ShowDecimalValueForSAF"))
                                {
                                    offerTrip.OriginDestination = string.Concat("$", string.Format("{0:#.00}", offerTrip.Amount));

                                }
                                else
                                {
                                    offerTrip.OriginDestination = string.Concat("$", offerTrip.Price);
                                }
                                bundleProduct.Detail.OfferTrips.Add(offerTrip);

                            }

                            #endregion

                            response.Products.Add(bundleProduct);

                        }

                    }

                    //Persisting the offer as we need to send this while registering the offer
                    var productOffer = new GetOffers();
                    productOffer = _travelerUtility.ObjectToObjectCasting<GetOffers, DynamicOfferDetailResponse>(jsonResponse.response);
                    await PersistAncillaryProducts(bundleRequest.SessionId, productOffer, true, "Bundle").ConfigureAwait(true);
                }
            }
            else
            {
                if (throwExceptionWhenSaveOmniCartFlow)
                    throw new Exception("CCEResponse is not loaded");
            }
            await _sessionHelperService.SaveSession<BookingBundlesResponse>(response, bundleRequest.SessionId, new List<string>() { bundleRequest.SessionId, response.ObjectName }, response.ObjectName).ConfigureAwait(false);
            return response;

        }
        private async System.Threading.Tasks.Task PersistAncillaryProducts(string sessionId, GetOffers productOffer, bool IsCCEDynamicOffer = false, String product = "")
        {

            await System.Threading.Tasks.Task.Run(async () =>
            {
                var persistedProductOffers = new GetOffers();
                persistedProductOffers = await _sessionHelperService.GetSession<GetOffers>(sessionId, persistedProductOffers.ObjectName, new List<string>() { sessionId, persistedProductOffers.ObjectName }).ConfigureAwait(false);
                if (_configuration.GetValue<bool>("EnableOmniCartReleaseCandidateTwoChanges_Bundles") && !string.IsNullOrEmpty(product))
                {
                    //Remove the Existing offer if we are making the dynamicOffer call multiple times with the same session
                    _omniCart.RemoveProductOfferIfAlreadyExists(persistedProductOffers, product);
                }

                if (persistedProductOffers != null && persistedProductOffers.Offers.Count > 0)
                {
                    if (!_configuration.GetValue<bool>("DisablePostBookingPurchaseFailureFix"))//Flightsegments need to be updated when ever we get an offer for the product.
                    {
                        persistedProductOffers.FlightSegments = productOffer.FlightSegments;
                        persistedProductOffers.Travelers = productOffer.Travelers;
                        persistedProductOffers.Solutions = productOffer.Solutions;
                        persistedProductOffers.Response = productOffer.Response;
                        persistedProductOffers.Requester = productOffer.Requester;
                    }

                    if (_configuration.GetValue<bool>("EnableOmniCartReleaseCandidateTwoChanges_Bundles") && !string.IsNullOrEmpty(product) && productOffer.Offers.FirstOrDefault().ProductInformation.ProductDetails != null && productOffer.Offers.FirstOrDefault().ProductInformation.ProductDetails.Count > 0)
                    {
                        foreach (var productDetails in productOffer.Offers.FirstOrDefault().ProductInformation.ProductDetails)
                        {
                            persistedProductOffers.Offers.FirstOrDefault().ProductInformation.ProductDetails.Add(productDetails);
                        }
                    }
                    else
                    {
                        persistedProductOffers.Offers.FirstOrDefault().ProductInformation.ProductDetails.Add(productOffer.Offers.FirstOrDefault().ProductInformation.ProductDetails.FirstOrDefault());
                    }
                }
                else
                {
                    persistedProductOffers = productOffer;
                }
                if (_configuration.GetValue<bool>("EnableOmniCartReleaseCandidateTwoChanges_Bundles"))
                {
                    if (IsCCEDynamicOffer)
                    {
                        if (productOffer.Characteristics == null)
                        {
                            productOffer.Characteristics = new Collection<Characteristic>();
                        }
                        if (persistedProductOffers != null && !persistedProductOffers.Characteristics.Any(characteristic => characteristic.Code == "IsEnabledThroughCCE"))
                        {
                            productOffer.Characteristics.Add(new Characteristic { Code = "IsEnabledThroughCCE", Value = "True" });
                        }
                    }
                    else// Need to remove this characteristics when IsCCEDynamicOffer==false ,As this is the same method we use for saving the postbooking products (PBS and PCU) at that we shouldnt send this characteristis as we are going to flightshoppig.
                    {
                        if (persistedProductOffers != null && persistedProductOffers.Characteristics?.Any(characteristic => characteristic.Code == "IsEnabledThroughCCE") == true)
                        {
                            persistedProductOffers.Characteristics.Remove(persistedProductOffers.Characteristics.First(characteristic => characteristic.Code == "IsEnabledThroughCCE"));
                        }
                    }
                }
                await _sessionHelperService.SaveSession<GetOffers>(persistedProductOffers, sessionId, new List<string>() { sessionId, persistedProductOffers.ObjectName }, persistedProductOffers.ObjectName).ConfigureAwait(false);
            });
        }
        private void PopulateBundleOfferDetails(Service.Presentation.ProductModel.SubProduct subProduct, List<BundleOfferDetail> bundleOfferDetails, Model.MPRewards.SDLProduct[] sDLProducts)
        {
            bundleOfferDetails = bundleOfferDetails ?? new List<BundleOfferDetail>();

            foreach (var product in sDLProducts)
            {
                var bundleOfferDetail = new BundleOfferDetail();
                bundleOfferDetail.OfferDetailDescription = product.Description.Replace("\n", String.Empty);
                bundleOfferDetail.OfferDetailHeader = product.Name;
                if (subProduct?.Extension?.Bundle?.Products.Count > 0)
                {
                    var notAvailable = new List<string>();
                    subProduct.Extension.Bundle.Products.ForEach(sp =>
                    {
                        var additionalExtensions = sp.SubProducts.Where(sub => sub.Extension != null
                                                  && sub.Code == product.Code
                                                  && sub.Extension.AdditionalExtensions != null).Select(sub => sub.Extension.AdditionalExtensions);

                        foreach (var additionalExtension in additionalExtensions)
                        {
                            additionalExtension.ForEach(ae =>
                            {
                                ae.Characteristics.Where(characteristic => string.Equals(characteristic.Value, "false", StringComparison.OrdinalIgnoreCase))
                                                   .ForEach(c => notAvailable.Add($"Not available at {c.Code}"));
                            });
                        }
                    });
                    bundleOfferDetail.OfferDetailWarningMessage = String.Join("\n", notAvailable.Distinct());
                }

                if (!bundleOfferDetails.Any(bod => string.Equals(bod.OfferDetailHeader, bundleOfferDetail.OfferDetailHeader, StringComparison.OrdinalIgnoreCase)))
                {
                    bundleOfferDetails.Add(bundleOfferDetail);
                }
                else
                {
                    var offerdetail = bundleOfferDetails.Find(bod => string.Equals(bod.OfferDetailHeader, bundleOfferDetail.OfferDetailHeader, StringComparison.OrdinalIgnoreCase));
                    offerdetail.OfferDetailWarningMessage = String.Join("\n", (new[] { offerdetail.OfferDetailWarningMessage?.Trim('\n')
                                                                                        , bundleOfferDetail.OfferDetailWarningMessage?.Trim('\n')
                                                                                }).Where(s => !string.IsNullOrEmpty(s)).Distinct());
                }
            }

        }
        private async System.Threading.Tasks.Task PopulateBundleTnC(BookingBundlesResponse response, United.Mobile.Model.MPRewards.SDLBody[] sDLBodies)
        {
            var bundleTnCContent = sDLBodies.FirstOrDefault(body => string.Equals(body.name, "bookingbundle", StringComparison.OrdinalIgnoreCase))?
                                                .content.FirstOrDefault(content => string.Equals(content.name, "booking-tc", StringComparison.OrdinalIgnoreCase))?
                                                .content;
            MOBMobileCMSContentMessages objtermsandconditions = new MOBMobileCMSContentMessages();
            response.TermsAndCondition = response.TermsAndCondition ?? new MOBMobileCMSContentMessages();
            response.TermsAndCondition.Title = "Terms and Conditions";
            response.TermsAndCondition.LocationCode = response.TermsAndCondition.ContentShort = "";
            response.TermsAndCondition.HeadLine = "Travel Options bundle terms and conditions";
            if (!string.IsNullOrEmpty(bundleTnCContent?.body))
            {
                response.TermsAndCondition.ContentFull = bundleTnCContent.body;

            }
            else //fallback, if sdl not giving tnc then we're getting from document library
            {
                response.TermsAndCondition.ContentFull = (await GetBundleTermsandConditons("bundlesTermsandConditons")).Replace('?', '℠');
            }
        }
        private async Task<string> GetBundleTermsandConditons(string databaseKeys)
        {
            string docTitles = "bundlesTermsandConditons" ;
            var docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(databaseKeys, _headers.ContextValues.TransactionId, true);

            string message = string.Empty;
            if (docs != null && docs.Count > 0)
            {
                foreach (MOBLegalDocument doc in docs)
                {
                    message = doc.LegalDocument;
                }
            }
            return message;
        }
        private bool IsSeatsRegistered(FlightReservationResponse flightReservationResponse)
        {
            return flightReservationResponse.DisplayCart?.DisplaySeats != null &&
                       flightReservationResponse.DisplayCart.DisplaySeats.Any(displaySeat => !string.IsNullOrEmpty(displaySeat.Seat));
        }
        private async Task<bool> GetBundleOfferV2(MOBSHOPSelectUnfinishedBookingRequest request)
        {
            bool isBundleOffersFailed = false;

            try
            {
                if (!_configuration.GetValue<bool>("DisableBundlesMethodsInCommonPlace"))
                {
                    await _shopBundleService.GetBundleOffer(BuildBundleOfferRequest(request));
                }
                else
                {
                    await GetBundleOffer(BuildBundleOfferRequest(request));
                }
                
            }
            catch (System.Net.WebException wex)
            {
                isBundleOffersFailed = true;
                MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(wex);
                //logEntries.Add(LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, "SelectOmniCartSavedTrips", "CCE-DynamicOffersForBundlesFailed", request.Application.Id, request.Application.Version.Major, request.DeviceId, uaexWrapper, true, true));
                _logger.LogError("SelectOmniCartSavedTrips - Exception {error} and SessionId {sessionId}", JsonConvert.SerializeObject(uaexWrapper), request.SessionId);
            }
            catch (MOBUnitedException uaex)
            {
                isBundleOffersFailed = true;
                MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);
                //logEntries.Add(LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, "SelectOmniCartSavedTrips", "CCE-DynamicOffersForBundlesFailed", request.Application.Id, request.Application.Version.Major, request.DeviceId, uaexWrapper, true, true));
                _logger.LogError("SelectOmniCartSavedTrips - Exception {error} and SessionId {sessionId}", JsonConvert.SerializeObject(uaexWrapper), request.SessionId);

            }
            catch (Exception ex)
            {
                isBundleOffersFailed = true;
                MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(ex);
                //logEntries.Add(LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, "SelectOmniCartSavedTrips", "CCE-DynamicOffersForBundlesFailed", request.Application.Id, request.Application.Version.Major, request.DeviceId, uaexWrapper, true, true));
                _logger.LogError("SelectOmniCartSavedTrips - Exception {error} and SessionId {sessionId}", JsonConvert.SerializeObject(uaexWrapper), request.SessionId);

            }
            finally
            {

                //if (_shopping.LogEntries != null & _shopping.LogEntries.Count > 0)
                //{
                //    logEntries.AddRange(_shopping.LogEntries);
                //}
            }

            return isBundleOffersFailed;
        }
        private bool IsBundleRegistered(FlightReservationResponse flightReservationResponse)
        {
            return flightReservationResponse?.ShoppingCart?.Items != null
                   && flightReservationResponse.ShoppingCart.Items
                                                           .SelectMany(i => i.Product)
                                                           .Any(p => p.Characteristics != null
                                                                  && p.Characteristics
                                                                  .Any(c => !string.IsNullOrEmpty(c.Code)
                                                                         && (c.Code.ToUpper() == "ISBUNDLE")
                                                                         && !string.IsNullOrEmpty(c.Value)
                                                                         && c.Value.ToUpper() == "TRUE"));
        }

        private bool IsBundleOrSFFRegistered(FlightReservationResponse flightReservationResponse)
        {
            var enableSFFTravelOption = System.Threading.Tasks.Task.Run(async () =>
            {
                return await _featureSettings.GetFeatureSettingValue("EnableFixForSFFTravelOption_MOBILE39209").ConfigureAwait(false);
            }).Result;
            return flightReservationResponse?.ShoppingCart?.Items != null
                   && flightReservationResponse.ShoppingCart.Items
                                                           .SelectMany(i => i.Product)
                                                           .Any(p => p.Characteristics != null
                                                                  && p.Characteristics
                                                                  .Any(c => !string.IsNullOrEmpty(c.Code)
                                                                         && (c.Code.ToUpper() == "ISBUNDLE")
                                                                         && !string.IsNullOrEmpty(c.Value)
                                                                         && c.Value.ToUpper() == "TRUE")
                                                                  || (enableSFFTravelOption && !string.IsNullOrEmpty(p.Code) && p.Code.ToUpper() == "SFC"));
        }

        private async System.Threading.Tasks.Task AssignHideTraveloptionsOnRTIFlagandNavigateToScreen(string sessionid, MOBShoppingCart shoppingCart)
        {
            shoppingCart.OmniCart = shoppingCart.OmniCart ?? new United.Mobile.Model.Shopping.Cart();
            shoppingCart.OmniCart.NavigateToScreen = MOBNavigationToScreen.SEATS.ToString();
            Model.Shopping.Reservation bookingPathReservation = new Model.Shopping.Reservation();
            //bookingPathReservation = United.Persist.FilePersist.Load<United.Persist.Definition.Shopping.Reservation>(sessionid, bookingPathReservation.ObjectName);
            bookingPathReservation = await _sessionHelperService.GetSession<Model.Shopping.Reservation>(sessionid, bookingPathReservation.ObjectName, new List<string> { sessionid, bookingPathReservation.ObjectName }).ConfigureAwait(false) ;


            bookingPathReservation.ShopReservationInfo2 = bookingPathReservation.ShopReservationInfo2 ?? new ReservationInfo2();
            bookingPathReservation.ShopReservationInfo2.HideTravelOptionsOnRTI = true;

            await _sessionHelperService.SaveSession<Model.Shopping.Reservation>(bookingPathReservation, sessionid, new List<string> { sessionid, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);

            //United.Persist.FilePersist.Save<United.Persist.Definition.Shopping.Reservation>(sessionid, bookingPathReservation.ObjectName, bookingPathReservation);
        }
        private async System.Threading.Tasks.Task CheckBundlesOffered(MOBSHOPSelectUnfinishedBookingRequest request, FlightReservationResponse flightReservationResponse, MOBShoppingCart shoppingCart)
        {
            if (_omniCart.IsEnableOmniCartReleaseCandidateThreeChanges_Seats(request.Application.Id, request.Application.Version.Major))
            {
                bool isBundleOffersFailed = await GetBundleOfferV2(request);
                if (isBundleOffersFailed)//Check if got any error from bundle 
                {
                    if (IsBundleRegistered(flightReservationResponse))//If we got error and bundles registered then we need to throw exception
                    {
                        throw new Exception(_configuration.GetValue<string>("OmnicartExceptionMessage"));
                    }
                    else
                    {
                        await AssignHideTraveloptionsOnRTIFlagandNavigateToScreen(request.SessionId, shoppingCart);
                    }
                }
                else
                {
                    MOBBookingBundlesResponse bundleResponse = new MOBBookingBundlesResponse(_configuration);
                    bundleResponse = await _sessionHelperService.GetSession<MOBBookingBundlesResponse>(request.SessionId, bundleResponse.ObjectName, new List<string> { request.SessionId, bundleResponse.ObjectName }).ConfigureAwait(false);

                    if (bundleResponse?.Products == null || bundleResponse?.Products?.Count == 0)
                    {
                        await AssignHideTraveloptionsOnRTIFlagandNavigateToScreen(request.SessionId, shoppingCart);
                    }
                }
            }
        }

        private void AssignCallRegisterOffersFlag(MOBShoppingCart shoppingCart)
        {
            shoppingCart.OmniCart = shoppingCart.OmniCart ?? new United.Mobile.Model.Shopping.Cart();
            shoppingCart.OmniCart.IsCallRegisterOffers = true;
        }

        public bool TravelOptionsContainsFareLock(List<Model.Shopping.TravelOption> options)
        {
            bool containsFareLock = false;

            if (options != null && options.Count > 0)
            {
                foreach (Model.Shopping.TravelOption option in options)
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
        private async System.Threading.Tasks.Task CheckFareLockExistsandGetTPI(MOBRequest request, Session session, Model.Shopping.Reservation bookingPathReservation, string cartId)
        {
            var isFarelockExist = TravelOptionsContainsFareLock(bookingPathReservation.TravelOptions);
            if (isFarelockExist)
            {
                List<Model.Shopping.TravelOption> travelOptions = new List<Model.Shopping.TravelOption>();
                foreach (Model.Shopping.TravelOption option in bookingPathReservation.TravelOptions)
                {
                    if (option != null && !string.IsNullOrEmpty(option.Key) && option.Key.ToUpper() == "FARELOCK")
                    {
                        travelOptions.Add(option);
                        break;
                    }
                }
                bookingPathReservation.TravelOptions = travelOptions;
            }
            else
            {
                #region TPI in booking path
                await GetTPIandUpdatePrices(request, cartId, session, bookingPathReservation);
                #endregion
            }
        }

        private async Task<bool> IsValidForTPI(Model.Shopping.Reservation bookingPathReservation)
        {
            bool isValid = true;
            if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo != null && bookingPathReservation.ShopReservationInfo.IsCorporateBooking && !bookingPathReservation.ShopReservationInfo.IsGhostCardValidForTPIPurchase)
            {
                isValid = false;
            }
            return isValid;
        }
        private async Task<TPIInfo> GetTripInsuranceDetails(Service.Presentation.ProductResponseModel.ProductOffer offer, int applicationId, string applicationVersion, string deviceID, string sessionId, bool isBookingPath = false)
        {
            TPIInfo tripInsuranceInfo = new TPIInfo();
            try
            {
                tripInsuranceInfo = await _travelerUtility.GetTPIDetails(offer, sessionId, true, isBookingPath, applicationId, applicationVersion);
            }
            catch (System.Exception ex)
            {
                tripInsuranceInfo = null;
                _logger.LogError("GetTripInsuranceInfo - Exception {error} and SessionId {sessionId}", ex.Message.ToString(), sessionId);
            }
            if (tripInsuranceInfo == null && (_configuration.GetValue<bool>("Log_TI_Offer_If_AIG_NotOffered_At_BookingPath")))
            {
                var ex = "AIG Not Offered Trip Insuracne in Booking Path";
                _logger.LogError("GetTripInsuranceInfo - UnitedException {error} and SessionId {sessionId}", ex.ToString(), sessionId);
            }
            return tripInsuranceInfo;
        }
        private async Task<DisplayCartRequest> GetDisplayCartRequest(ProductSearchRequest request)
        {
            DisplayCartRequest displayCartRequest = null;
            if (request != null && !string.IsNullOrEmpty(request.CartId) && request.ProductCodes != null && request.ProductCodes.Count > 0)
            {
                displayCartRequest = new DisplayCartRequest();
                displayCartRequest.CartId = request.CartId;
                displayCartRequest.CountryCode = request.PointOfSale;
                displayCartRequest.LangCode = request.LanguageCode;
                displayCartRequest.ProductCodes = new List<string>();
                displayCartRequest.CartKey = request.CartKey;
                foreach (var productCode in request.ProductCodes)
                {
                    displayCartRequest.ProductCodes.Add(productCode);
                }

                #region TripInsuranceV2
                // session
                var session = new Session();
                session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string> { request.SessionId, session.ObjectName }).ConfigureAwait(false);
                if (session != null &&
                    await _featureToggles.IsEnabledTripInsuranceV2(request.Application.Id, request.Application.Version.Major, session.CatalogItems).ConfigureAwait(false) &&
                    !session.IsReshopChange &&
                    displayCartRequest.ProductCodes != null &&
                    displayCartRequest.ProductCodes.Count > 0 &&
                    displayCartRequest.ProductCodes.Contains("TPI"))
                {
                    displayCartRequest.Characteristics = new Collection<Service.Presentation.CommonModel.Characteristic>();
                    // new characteristic to get the right information depending on the version
                    var newTPIITem = new Service.Presentation.CommonModel.Characteristic
                    {
                        Code = "TripOfferVersionId",
                        Value = "3"
                    };
                    displayCartRequest.Characteristics.Add(newTPIITem);
                }
                #endregion
            }
            return displayCartRequest;
        }

        private async Task<TPIInfo> GetTripInsuranceInfo(ProductSearchRequest request, string token, bool isBookingPath = false)
        {
            TPIInfo tripInsuranceInfo = new TPIInfo();
            bool isPostBookingCall = (_configuration.GetValue<bool>("ShowTripInsuranceSwitch"));
            // ==>> Venkat and Elise change only one below line of code to offer TPI postbooking when inline TPI is off for new clients 2.1.36 and above
            isPostBookingCall = _travelerUtility.EnableTPI(request.Application.Id, request.Application.Version.Major, 1);
            if (isPostBookingCall ||
                (_configuration.GetValue<bool>("ShowTripInsuranceBookingSwitch")
                && isBookingPath)
                )
            {
                string logAction = isBookingPath ? "GetTripInsuranceInfoInBookingPath" : "GetTripInsuranceInfo";
                try
                {
                    DisplayCartRequest displayCartRequest = await GetDisplayCartRequest(request).ConfigureAwait(false);
                    string jsonRequest = JsonConvert.SerializeObject(displayCartRequest);

                    string jsonResponse = await _flightShoppingProductsService.GetProducts(token, _headers.ContextValues.SessionId, jsonRequest).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(jsonResponse))
                    {
                        DisplayCartResponse response = JsonConvert.DeserializeObject<DisplayCartResponse>(jsonResponse);
                        var productVendorOffer = new GetVendorOffers();
                        if (!_configuration.GetValue<bool>("DisableMerchProductOfferNullCheck"))
                            productVendorOffer = response.MerchProductOffer != null ? productVendorOffer = ObjectToObjectCasting<GetVendorOffers, United.Service.Presentation.ProductResponseModel.ProductOffer>(response.MerchProductOffer) : productVendorOffer;
                        else
                            productVendorOffer = ObjectToObjectCasting<GetVendorOffers, United.Service.Presentation.ProductResponseModel.ProductOffer>(response.MerchProductOffer);

                        await _sessionHelperService.SaveSession<GetVendorOffers>(productVendorOffer, request.SessionId, new List<string> { request.SessionId, productVendorOffer.ObjectName }, productVendorOffer.ObjectName).ConfigureAwait(false);
                        if (response != null && (response.Errors == null || response.Errors.Count == 0) && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success) && response.MerchProductOffer != null)
                        {
                            tripInsuranceInfo = await GetTripInsuranceDetails(response.MerchProductOffer, request.Application.Id, request.Application.Version.Major, request.DeviceId, request.SessionId, isBookingPath);
                        }
                        else
                        {
                            tripInsuranceInfo = null;
                        }
                    }

                    if (!isBookingPath)
                    {
                        _logger.LogInformation("GetTripInsuranceInfo - ClientResponse for GetTripInsuranceInfo {Trace} and {sessionId}", tripInsuranceInfo, request.SessionId);

                        // add presist file 
                        Model.Shopping.Reservation bookingPathReservation = new Model.Shopping.Reservation();
                        bookingPathReservation = await _sessionHelperService.GetSession<Model.Shopping.Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                        if (bookingPathReservation.TripInsuranceFile == null)
                        {
                            bookingPathReservation.TripInsuranceFile = new TripInsuranceFile() { };
                        }
                        bookingPathReservation.TripInsuranceFile.TripInsuranceInfo = tripInsuranceInfo;
                        await _sessionHelperService.SaveSession<Model.Shopping.Reservation>(bookingPathReservation, request.SessionId, new List<string> { request.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                    }
                }
                catch (System.Net.WebException wex)
                {
                    //var errorResponse = new StreamReader(wex.Response.GetResponseStream()).ReadToEnd();
                    _logger.LogError("GetTripInsuranceInfo - UnitedException {error} and SessionId {sessionId}", wex.Message.ToString(), request.SessionId);
                }
                catch (System.Exception ex)
                {
                    tripInsuranceInfo = null;
                    MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                }
            }
            return tripInsuranceInfo;
        }

        private T ObjectToObjectCasting<T, R>(R request)
        {
            var typeInstance = Activator.CreateInstance(typeof(T));

            foreach (var propReq in request.GetType().GetProperties())
            {
                var propRes = typeInstance.GetType().GetProperty(propReq.Name);
                if (propRes != null)
                {
                    propRes.SetValue(typeInstance, propReq.GetValue(request));
                }
            }

            return (T)typeInstance;
        }

        private async Task<TPIInfoInBookingPath> GetBookingPathTPIInfo(string sessionId, string languageCode, MOBApplication application, string deviceId, string cartId, string token, bool isRequote, bool isRegisterTraveler, bool isReshop)
        {
            TPIInfoInBookingPath tPIInfo = new TPIInfoInBookingPath();
            TPIInfo tripInsuranceInfo = new TPIInfo();
            ProductSearchRequest getTripInsuranceRequest = new ProductSearchRequest();
            getTripInsuranceRequest.SessionId = sessionId;
            getTripInsuranceRequest.LanguageCode = languageCode;
            getTripInsuranceRequest.Application = application;
            getTripInsuranceRequest.DeviceId = deviceId;
            getTripInsuranceRequest.CartId = cartId;
            getTripInsuranceRequest.PointOfSale = "US";
            getTripInsuranceRequest.CartKey = "TPI";
            getTripInsuranceRequest.ProductCodes = new List<string>() { "TPI" };
            Model.Shopping.Reservation bookingPathReservation = new Model.Shopping.Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Model.Shopping.Reservation>(sessionId, bookingPathReservation.ObjectName, new List<string> { sessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
            // if it is concur account with non valid fop for ghost card, we dont show TPI to them
            if (await IsValidForTPI(bookingPathReservation))
            {
                TPIInfoInBookingPath persistregisteredTPIInfo = new TPIInfoInBookingPath() { };
                if (bookingPathReservation != null && bookingPathReservation.TripInsuranceFile != null && bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo != null && bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo.IsRegistered)
                {
                    persistregisteredTPIInfo = bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo;
                }
                else
                {
                    persistregisteredTPIInfo = null;
                }
                getTripInsuranceRequest.PointOfSale = !string.IsNullOrEmpty(bookingPathReservation.PointOfSale) ? bookingPathReservation.PointOfSale : "US";
                tripInsuranceInfo = await GetTripInsuranceInfo(getTripInsuranceRequest, token, true);

                bookingPathReservation = await _sessionHelperService.GetSession<Model.Shopping.Reservation>(sessionId, bookingPathReservation.ObjectName, new
                    List<string> { sessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                if (bookingPathReservation.TripInsuranceFile == null || tripInsuranceInfo == null)
                {
                    bookingPathReservation.TripInsuranceFile = new TripInsuranceFile() { };
                    tPIInfo = null;
                }
                else
                {
                    tPIInfo = AssignBookingPathTPIInfo(tripInsuranceInfo, bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo);
                }
                string cartKey = "TPI";
                string productCode = "TPI";
                string productId = string.Empty;
                List<string> productIds = new List<string>() { };
                string subProductCode = string.Empty;
                bool delete = true;
                if (persistregisteredTPIInfo != null && persistregisteredTPIInfo.IsRegistered && isRequote)
                {
                    //requote
                    if (isRegisterTraveler || (tPIInfo == null || (tPIInfo != null && tPIInfo.Amount == 0)))
                    {
                        // unregister old TPI, update price
                        productId = persistregisteredTPIInfo.QuoteId;
                        delete = true;
                        var travelOptions = RegisterOffer(sessionId, cartId, cartKey, languageCode, getTripInsuranceRequest.PointOfSale, productCode, productId, productIds, subProductCode, delete, application.Id, deviceId, application.Version.Major);
                        //bookingPathReservation.Prices = UpdatePriceForUnRegisterTPI(bookingPathReservation.Prices);
                    }
                    else
                    {
                        // ancillary change
                        if (tPIInfo != null)
                        {
                            // unregister the old one and register new one
                            if (persistregisteredTPIInfo.Amount == tPIInfo.Amount)
                            {
                                productId = persistregisteredTPIInfo.QuoteId;
                                delete = true;
                                var travelOptions = RegisterOffer(sessionId, cartId, cartKey, languageCode, getTripInsuranceRequest.PointOfSale, productCode, productId, productIds, subProductCode, delete, application.Id, deviceId, application.Version.Major);
                                productId = tPIInfo.QuoteId;
                                delete = false;
                                travelOptions = RegisterOffer(sessionId, cartId, cartKey, languageCode, getTripInsuranceRequest.PointOfSale, productCode, productId, productIds, subProductCode, delete, application.Id, deviceId, application.Version.Major);
                                tPIInfo.ButtonTextInProdPage = _configuration.GetValue<string>("TPIinfo_BookingPath_PRODBtnText_AfterRegister");
                                tPIInfo.CoverCostStatus = _configuration.GetValue<string>("TPIinfo_BookingPath_CoverCostStatus");
                                tPIInfo.IsRegistered = true;
                            }
                            // send pop up message
                            else
                            {
                                tPIInfo.OldAmount = persistregisteredTPIInfo.Amount;
                                tPIInfo.OldQuoteId = persistregisteredTPIInfo.QuoteId;
                                CultureInfo ci = TopHelper.GetCultureInfo("en-US");
                                string oldPrice = TopHelper.FormatAmountForDisplay(string.Format("{0:#,0.00}", tPIInfo.OldAmount), ci, false);
                                string newPrice = TopHelper.FormatAmountForDisplay(string.Format("{0:#,0.00}", tPIInfo.Amount), ci, false);
                                tPIInfo.PopUpMessage = string.Format(_configuration.GetValue<string>("TPIinfo_BookingPath_PopUpMessage"), oldPrice, newPrice);
                                // in the background of RTI page, trip insurance considered as added. Dont remove TPI from prices list and keep the isRegistered equal to true until user make any choices. 
                                tPIInfo.IsRegistered = true;
                            }
                        }
                        else
                        {
                            // if trip insurance not shown after requote, remove price from prices list
                            bookingPathReservation.Prices = UpdatePriceForUnRegisterTPI(bookingPathReservation.Prices);
                        }
                    }
                }
                bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo = tPIInfo;
            }
            else
            {
                bookingPathReservation.TripInsuranceFile = null;
            }

            await _sessionHelperService.SaveSession<Model.Shopping.Reservation>(bookingPathReservation, sessionId, new List<string> { sessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);

            return tPIInfo;
        }
        private United.Services.FlightShopping.Common.FlightReservation.RegisterOfferRequest GetRegisterOfferRequest(string cartId, string cartKey, string languageCode, string pointOfSale, string productCode, string productId, List<string> productIds, string subProductCode, bool delete)
        {
            United.Services.FlightShopping.Common.FlightReservation.RegisterOfferRequest registerOfferRequest = new United.Services.FlightShopping.Common.FlightReservation.RegisterOfferRequest();
            registerOfferRequest.AutoTicket = false;
            registerOfferRequest.CartId = cartId;
            registerOfferRequest.CartKey = cartKey;
            registerOfferRequest.CountryCode = pointOfSale;
            registerOfferRequest.Delete = delete;
            registerOfferRequest.LangCode = languageCode;
            registerOfferRequest.ProductCode = productCode;
            registerOfferRequest.ProductId = productId;
            registerOfferRequest.ProductIds = productIds;
            registerOfferRequest.SubProductCode = subProductCode;

            return registerOfferRequest;
        }

        private async Task<List<Model.Shopping.TravelOption>> RegisterOffer(string sessionId, string cartId, string cartKey, string languageCode, string pointOfSale, string productCode, string productId, List<string> productIds, string subProductCode, bool delete, int applicationId, string deviceId, string appVersion)
        {
            List<Model.Shopping.TravelOption> travelOptions = null;

            string logAction = delete ? "UnRegisterOffer" : "RegisterOffer";
            if (productCode == "TPI")
            {
                logAction = delete ? "UnRegisterOfferForTPI" : "RegisterOfferForTPI";
            }

            United.Services.FlightShopping.Common.FlightReservation.RegisterOfferRequest registerOfferRequest = GetRegisterOfferRequest(cartId, cartKey, languageCode, pointOfSale, productCode, productId, productIds, subProductCode, delete);
            if (registerOfferRequest != null)
            {
                string jsonRequest = JsonConvert.SerializeObject(registerOfferRequest);
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
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

                string jsonResponse = await _flightShoppingProductsService.RegisterOffer(session.Token, logAction, jsonRequest, sessionId);
                #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******

                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******            
                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    FlightReservationResponse response = JsonConvert.DeserializeObject<FlightReservationResponse>(jsonResponse);

                    if (response != null && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success) && response.DisplayCart != null)
                    {
                        if (productCode != "TPI" && productCode != "PBS")
                        {
                            travelOptions = GetTravelOptions(response.DisplayCart);
                        }
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

                            throw new System.Exception(errorMessage);
                        }
                        else
                        {
                            throw new MOBUnitedException("Unable to get shopping cart contents.");
                        }
                    }
                }
                else
                {
                    throw new MOBUnitedException("Unable to get shopping cart contents.");
                }
            }

            return travelOptions;
        }

        public async Task<MOBGetOmniCartSavedTripsResponse> RemoveOmniCartSavedTrip(Session session, MOBSHOPUnfinishedBookingRequestBase request)
        {
            string cslActionName = "CartId/" + request.CartId;
            await PurgeUnfinshedBookings(session, cslActionName, request);
            return await GetOmniCartSavedTrips(session, request);
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
                    && (_configuration.GetValue<bool>("ShowTripInsuranceSwitch"));
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


        private TPIInfoInBookingPath AssignBookingPathTPIInfo(TPIInfo tripInsuranceInfo, TPIInfoInBookingPath tripInsuranceBookingInfo)
        {
            TPIInfoInBookingPath tPIInfo = new TPIInfoInBookingPath() { };
            tPIInfo.Amount = tripInsuranceInfo.Amount;
            tPIInfo.ButtonTextInProdPage = (_configuration.GetValue<string>("TPIinfo_BookingPath_PRODBtnText_BeforeRegister") ?? "") + tripInsuranceInfo.DisplayAmount;
            //tPIInfo.ButtonTextInRTIPage = ConfigurationManager.AppSettings["TPIinfo_BookingPath_RTIBtnText_BeforeRegister"] ??  "";
            tPIInfo.Title = tripInsuranceInfo.PageTitle;
            tPIInfo.CoverCostText = _configuration.GetValue<string>("TPIinfo_BookingPath_CoverCostTest") ?? "";
            tPIInfo.CoverCost = (_configuration.GetValue<string>("TPIinfo_BookingPath_CoverCost") ?? "") + "<b>" + tripInsuranceInfo.CoverCost + "</b>";
            tPIInfo.Img = tripInsuranceInfo.Image;
            tPIInfo.IsRegistered = false;
            tPIInfo.QuoteId = tripInsuranceInfo.ProductId;
            tPIInfo.Tnc = tripInsuranceInfo.Body3;
            tPIInfo.Content = tripInsuranceBookingInfo?.Content;
            tPIInfo.Header = tripInsuranceBookingInfo?.Header;
            tPIInfo.LegalInformation = tripInsuranceBookingInfo?.LegalInformation;
            tPIInfo.LegalInformationText = tripInsuranceInfo.TNC;
            tPIInfo.TncSecondaryFOPPage = tripInsuranceBookingInfo?.TncSecondaryFOPPage;
            tPIInfo.DisplayAmount = tripInsuranceInfo.DisplayAmount;
            tPIInfo.ConfirmationMsg = tripInsuranceBookingInfo?.ConfirmationMsg;
            if (_configuration.GetValue<bool>("EnableTravelInsuranceOptimization"))
            {
                tPIInfo.TileTitle1 = tripInsuranceInfo.TileTitle1;
                tPIInfo.TileTitle2 = tripInsuranceInfo.TileTitle2;
                tPIInfo.TileImage = tripInsuranceInfo.TileImage;
                tPIInfo.TileLinkText = tripInsuranceInfo.TileLinkText;
            }


            //Covid-19 Emergency WHO TPI content
            if (_configuration.GetValue<bool>("ToggleCovidEmergencytextTPI") == true)
            {
                tPIInfo.tpiAIGReturnedMessageContentList = tripInsuranceInfo.TPIAIGReturnedMessageContentList;
            }

            if (!string.IsNullOrEmpty(tripInsuranceInfo.HtmlContentV2))
            {
                tPIInfo.HtmlContentV2 = tripInsuranceInfo.HtmlContentV2;
            }
            return tPIInfo;
        }

        private async System.Threading.Tasks.Task GetTPIandUpdatePrices(MOBRequest request, string cartId, Session session, Model.Shopping.Reservation bookingPathReservation)
        {
            #region TPI in booking path
            if (_travelerUtility.EnableTPI(request.Application.Id, request.Application.Version.Major, 3) && !bookingPathReservation.IsReshopChange)
            {
                // call TPI 
                try
                {
                    string token = session.Token;
                    TPIInfoInBookingPath tPIInfo = await GetBookingPathTPIInfo(session.SessionId, request.LanguageCode, request.Application, request.DeviceId, cartId, token, true, true, false);
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
            bookingPathReservation.Prices = UpdatePriceForUnRegisterTPI(bookingPathReservation.Prices);
            #endregion
        }
        private List<MOBSHOPPrice> UpdatePriceForUnRegisterTPI(List<MOBSHOPPrice> persistedPrices)
        {
            List<MOBSHOPPrice> prices = null;
            if (persistedPrices != null && persistedPrices.Count > 0)
            {
                double travelOptionSubTotal = 0.0;
                foreach (var price in persistedPrices)
                {
                    // Add TPI here 
                    if (price.PriceType.ToUpper().Equals("TRAVEL INSURANCE"))
                    {
                        travelOptionSubTotal = travelOptionSubTotal + Convert.ToDouble(price.DisplayValue);
                    }
                    else if (!price.PriceType.ToUpper().Equals("GRAND TOTAL"))
                    {
                        if (prices == null)
                        {
                            prices = new List<MOBSHOPPrice>();
                        }
                        prices.Add(price);
                    }
                }

                foreach (var price in persistedPrices)
                {
                    if (price.PriceType.ToUpper().Equals("GRAND TOTAL"))
                    {
                        double grandTotal = Convert.ToDouble(price.DisplayValue);
                        price.DisplayValue = string.Format("{0:#,0.00}", grandTotal - travelOptionSubTotal);
                        price.FormattedDisplayValue = string.Format("${0:c}", price.DisplayValue);
                        double tempDouble1 = 0;
                        double.TryParse(price.DisplayValue.ToString(), out tempDouble1);
                        price.Value = Math.Round(tempDouble1, 2, MidpointRounding.AwayFromZero);
                        if (prices == null)
                        {
                            prices = new List<MOBSHOPPrice>();
                        }
                        prices.Add(price);
                    }
                }
            }

            return prices;
        }

        private async System.Threading.Tasks.Task UpdateTCPriceAndFOPType(List<MOBSHOPPrice> prices, MOBFormofPaymentDetails formofPaymentDetails, MOBApplication application, List<ProdDetail> products, List<MOBCPTraveler> travelers)
        {
            if (ConfigUtility.IncludeTravelCredit(application.Id, application.Version.Major))
            {
                ApplyFFCToAncillary(products, application, formofPaymentDetails, prices);
                var price = prices.FirstOrDefault(p => p.DisplayType.ToUpper() == "CERTIFICATE" || p.DisplayType.ToUpper() == "FFC");
                if (price != null)
                {
                    formofPaymentDetails.TravelCreditDetails.AlertMessages = (formofPaymentDetails.TravelFutureFlightCredit?.FutureFlightCredits?.Count > 0 ?
                                                                              formofPaymentDetails.TravelFutureFlightCredit.ReviewFFCMessages :
                                                                              formofPaymentDetails.TravelCertificate.ReviewETCMessages.Where(m => m.HeadLine != "TravelCertificate_Combinability_ReviewETCAlertMsgs_OtherFopRequiredMessage").ToList());
                }
                else if (formofPaymentDetails.TravelCreditDetails != null)
                {
                    formofPaymentDetails.TravelCreditDetails.AlertMessages = null;
                }

                UpdateTravelCreditAmountWithSelectedETCOrFFC(formofPaymentDetails, prices, travelers);
                try
                {
                    CSLContentMessagesResponse lstMessages = null;

                    //string s = United.Persist.FilePersist.Load<string>(ConfigurationManager.AppSettings["BookingPathRTI_CMSContentMessagesCached_StaticGUID"].ToString(), "MOBCSLContentMessagesResponse");

                    string s = await _sessionHelperService.GetSession<string>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID"), ObjectNames.MOBSeatMapListFullName, new List<string> { _configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID"), ObjectNames.MOBSeatMapListFullName }).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(s))
                    {
                        lstMessages = DataContextJsonSerializer.NewtonSoftDeserialize<CSLContentMessagesResponse>(s);
                        formofPaymentDetails.TravelCreditDetails.AlertMessages = _fFCShoppingcs.BuildReviewFFCHeaderMessage(formofPaymentDetails?.TravelFutureFlightCredit, travelers, lstMessages.Messages);
                    }
                }
                catch { }
                if (formofPaymentDetails?.FormOfPaymentType == "ETC" ||
                   formofPaymentDetails?.FormOfPaymentType == "FFC")
                    formofPaymentDetails.FormOfPaymentType = "TC";

            }
        }

        private void UpdateGrandTotal(MOBSHOPReservation reservation, bool isCommonMethod = false)
        {
            var grandTotalIndex = reservation.Prices.FindIndex(a => a.PriceType == "GRAND TOTAL");
            if (grandTotalIndex >= 0)
            {
                double extraMilePurchaseAmount = (reservation?.Prices?.Where(a => a.DisplayType == "MPF")?.FirstOrDefault()?.Value != null) ?
                                         Convert.ToDouble(reservation?.Prices?.Where(a => a.DisplayType == "MPF")?.FirstOrDefault()?.Value) : 0;
                string priceTypeDescription = reservation?.Prices?.Where(a => a.DisplayType == "MPF")?.FirstOrDefault()?.PriceTypeDescription;
                if (extraMilePurchaseAmount > 0 && (priceTypeDescription == null || priceTypeDescription?.Contains("Additional") == false))
                {
                    reservation.Prices[grandTotalIndex].Value += extraMilePurchaseAmount;
                    CultureInfo ci = null;
                    ci = TopHelper.GetCultureInfo(reservation?.Prices[grandTotalIndex].CurrencyCode);
                    reservation.Prices[grandTotalIndex].DisplayValue = reservation.Prices[grandTotalIndex].Value.ToString();
                    reservation.Prices[grandTotalIndex].FormattedDisplayValue = TopHelper.FormatAmountForDisplay(reservation?.Prices[grandTotalIndex].Value.ToString(), ci, false);
                }
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


        public async Task<bool> IsNonUSPointOfSale(string sessionId, string cartId, MOBSHOPSelectTripResponse response)
        {
            FlightReservationResponse flightReservationResponse = await _omniCart.GetFlightReservationResponseByCartId(sessionId, cartId);

            if (String.CompareOrdinal(flightReservationResponse?.Reservation?.PointOfSale?.Country?.CountryCode, "US") != 0)
            {

                response.NonUSPOSAlertMessage = new MOBOnScreenAlert()
                {
                    Title = _configuration.GetValue<string>("POSAlertMessageTitle")?.ToString(),
                    Message = _configuration.GetValue<string>("POSAlertMessageBodyText")?.ToString(),
                    Actions = new List<MOBOnScreenActions> {
                        new MOBOnScreenActions
                        {
                            ActionTitle = _configuration.GetValue<string>("POSContinueButtonText")?.ToString(),
                            ActionURL = flightReservationResponse.VendorQueryReturnUrl,

                            WebSessionShareUrl= _configuration.GetValue<string>("DotcomSSOUrl")
                        },
                        new MOBOnScreenActions
                        {
                            ActionTitle = _configuration.GetValue<string>("POSStartNewSearchButtonText")?.ToString()
                        },
                    }
                }; 
                return true;
            }
            return false;
        }

        private MOBSHOPUnfinishedBookingFlight MapToMOBSHOPUnfinishedBookingFlight(CCESavedItinerary.Flight cslFlight)
        {
            var ubMOBFlight = new MOBSHOPUnfinishedBookingFlight
            {
                BookingCode = cslFlight.BookingCode,
                DepartDateTime = cslFlight.DepartDateTime,
                Origin = cslFlight.Origin,
                Destination = cslFlight.Destination,
                FlightNumber = cslFlight.FlightNumber,
                MarketingCarrier = cslFlight.MarketingCarrier,
                ProductType = cslFlight.ProductType,
            };
            if (cslFlight.Products?.Count > 0)
            {
                foreach (var product in cslFlight.Products)
                {
                    if (ubMOBFlight.Products == null)
                        ubMOBFlight.Products = new List<MOBSHOPUnfinishedBookingFlightProduct>();
                    ubMOBFlight.Products.Add(MapToMOBSHOPUnfinishedBookingFlightProduct(product));
                }
            }
            if (cslFlight.Connections == null || cslFlight.Connections.Count == 0)
                return ubMOBFlight;

            foreach (var conn in cslFlight.Connections)
            {
                if (ubMOBFlight.Connections == null)
                    ubMOBFlight.Connections = new List<MOBSHOPUnfinishedBookingFlight>();
                ubMOBFlight.Connections.Add(MapToMOBSHOPUnfinishedBookingFlight(conn));
            }

            return ubMOBFlight;
        }
        private MOBSHOPUnfinishedBookingFlightProduct MapToMOBSHOPUnfinishedBookingFlightProduct(CCESavedItinerary.Product product)
        {
            MOBSHOPUnfinishedBookingFlightProduct unfinishedBookingFlightProduct = null;
            if (product != null)
            {
                unfinishedBookingFlightProduct = new MOBSHOPUnfinishedBookingFlightProduct
                {
                    BookingCode = product.BookingCode,
                    ProductType = product.ProductType,
                    TripIndex = product.TripIndex
                };

                if (product.Prices != null)
                {
                    foreach (var price in product.Prices)
                    {
                        if (unfinishedBookingFlightProduct.Prices == null)
                            unfinishedBookingFlightProduct.Prices = new List<MOBSHOPUnfinishedBookingProductPrice>();
                        unfinishedBookingFlightProduct.Prices.Add(MapToMOBSHOPUnfinishedBookingFlightProductPrice(price));
                    }
                }
            }
            return unfinishedBookingFlightProduct;
        }
        private MOBSHOPUnfinishedBookingProductPrice MapToMOBSHOPUnfinishedBookingFlightProductPrice(CCESavedItinerary.Price productPrice)
        {
            MOBSHOPUnfinishedBookingProductPrice unfinishedBookingProductPrice = null;
            if (productPrice != null)
            {
                unfinishedBookingProductPrice = new MOBSHOPUnfinishedBookingProductPrice
                {
                    Amount = productPrice.Amount,
                    AmountAllPax = productPrice.AmountAllPax,
                    AmountBase = productPrice.AmountBase,
                    Currency = productPrice.Currency,
                    CurrencyAllPax = productPrice.CurrencyAllPax,
                    OfferID = productPrice.OfferID,
                    PricingType = productPrice.PricingType,
                    Selected = productPrice.Selected,
                    MerchPriceDetail = new MOBSHOPUnfinishedBookingProductPriceDetail { ProductCode = productPrice.MerchPriceDetail?.ProductCode, EddCode = productPrice.MerchPriceDetail?.EddCode },
                };

                if (productPrice.SegmentMappings != null)
                {
                    foreach (var segmentMapping in productPrice.SegmentMappings)
                    {
                        if (unfinishedBookingProductPrice.SegmentMappings == null)
                            unfinishedBookingProductPrice.SegmentMappings = new List<MOBSHOPUnfinishedBookingProductSegmentMapping>();
                        unfinishedBookingProductPrice.SegmentMappings.Add(MapToMOBSHOPUnfinishedBookingFlightProductSegmentMapping(segmentMapping));
                    }
                }
            }

            return unfinishedBookingProductPrice;
        }
        private MOBSHOPUnfinishedBookingProductSegmentMapping MapToMOBSHOPUnfinishedBookingFlightProductSegmentMapping(CCESavedItinerary.SegmentMapping segmentMapping)
        {
            MOBSHOPUnfinishedBookingProductSegmentMapping unfinishedBookingProductSegmentMapping = null;
            if (segmentMapping != null)
            {
                unfinishedBookingProductSegmentMapping = new MOBSHOPUnfinishedBookingProductSegmentMapping
                {
                    Origin = segmentMapping.Origin,
                    Destination = segmentMapping.Destination,
                    BBxHash = segmentMapping.BBxHash,
                    UpgradeStatus = segmentMapping.UpgradeStatus,
                    UpgradeTo = segmentMapping.UpgradeTo,
                    UpgradeType = segmentMapping.UpgradeType,
                    FlightNumber = segmentMapping.FlightNumber,
                    CabinDescription = segmentMapping.CabinDescription,
                    SegmentRefID = segmentMapping.SegmentRefID,
                };
            }

            return unfinishedBookingProductSegmentMapping;
        }
        public ContextualCommRequest BuildccePersonalizationRequest(MOBSHOPUnfinishedBookingRequestBase request)
        {
            ContextualCommRequest cceRequest = new ContextualCommRequest();
            cceRequest.IPCountry = "US";
            cceRequest.ChannelType = "MOB";
            cceRequest.ComponentsToLoad = new Collection<string>();

            cceRequest.ComponentsToLoad.Add(_configuration.GetValue<string>("CCEOmnicartEmptySavedTripsComponent"));


            if (IsEnableOmniCartReleaseCandidate4BChanges(request.Application.Id, request.Application.Version.Major))
                cceRequest.ComponentsToLoad.Add(_configuration.GetValue<string>("CCEOmnicartGetSavedTripsComponetV2"));
            else
                cceRequest.ComponentsToLoad.Add(_configuration.GetValue<string>("CCEOmnicartGetSavedTripsComponet"));

            cceRequest.ComponentsToLoad.Add(_configuration.GetValue<string>("CCEOmnicartSavedTripsAlertComponent"));

            cceRequest.ComponentsToLoad.Add(_configuration.GetValue<string>("CCEOmnicartOmniCartIndicatorComponent"));
            cceRequest.LangCode = "en";
            cceRequest.MileagePlusId = request.MileagePlusAccountNumber;

            cceRequest.PageToLoad = _configuration.GetValue<string>("CCEOmnicartSavedTripsPageToLoad");
            cceRequest.DeviceId = request.DeviceId;
            cceRequest.Browser = request.Application.Id == 1 ? "iOS" : "Android";
            cceRequest.BrowserVersion = System.Text.RegularExpressions.Regex.Replace(request.Application.Version.Major, "[^0-9.]", "");
            cceRequest.Requestor = new Requestor();
            cceRequest.Requestor.Characteristic = new Collection<Characteristic>();
            if (_omniCart.IsEnableOmniCartReleaseCandidateTwoChanges_Bundles(request.Application.Id, request.Application.Version.Major))
            {
                cceRequest.Requestor.Characteristic.Add(new Characteristic { Code = "OmniCartRepricing", Value = _omniCart.GetCCEOmnicartRepricingCharacteristicValue(request.Application.Id, request.Application.Version.Major) });
            }
            if (_omniCart.IsEnableOmniCartReleaseCandidate4CChanges(request.Application.Id, request.Application.Version.Major, request.CatalogItems))
            {
                cceRequest.Requestor.Characteristic.Add(new Characteristic { Code = "IncludeIPOSTrips", Value = "true" });
            }
            return cceRequest;
        }
        private ContextualCommRequest BuildccePersonalizationRequest(MOBSHOPGetUnfinishedBookingsRequest request)
        {
            ContextualCommRequest cceRequest = new ContextualCommRequest();
            cceRequest.IPCountry = "US";
            cceRequest.ChannelType = "MOB";
            cceRequest.ComponentsToLoad = new System.Collections.ObjectModel.Collection<string>();
            cceRequest.ComponentsToLoad.Add("SavedTrips");
            cceRequest.LangCode = "en";
            cceRequest.MileagePlusId = request.MileagePlusAccountNumber;
            cceRequest.PageToLoad = "Booking";
            cceRequest.DeviceId = request.DeviceId;
            cceRequest.Browser = request.Application.Id == 1 ? "iOS" : "Android";
            cceRequest.BrowserVersion = System.Text.RegularExpressions.Regex.Replace(request.Application.Version.Major, "[^0-9.]", "");
            cceRequest.Requestor = new Requestor();
            cceRequest.Requestor.Characteristic = new Collection<Characteristic>();
            if (_omniCart.IsEnableOmniCartReleaseCandidateTwoChanges_Bundles(request.Application.Id, request.Application.Version.Major))
            {
                cceRequest.Requestor.Characteristic.Add(new Characteristic { Code = "OmniCartRepricing", Value = _omniCart.GetCCEOmnicartRepricingCharacteristicValue(request.Application.Id, request.Application.Version.Major) });
            }
            if (_omniCart.IsEnableOmniCartReleaseCandidate4CChanges(request.Application.Id, request.Application.Version.Major, request.CatalogItems))
            {
                cceRequest.Requestor.Characteristic.Add(new Characteristic { Code = "IncludeIPOSTrips", Value = "true" });
            }
            return cceRequest;
        }
        private async System.Threading.Tasks.Task ValidateVulnerability(string mpNumber, string hashPinCode, int applicationId, string deviceId, string appMajorVersion, string exceptionMessage)
        {
            if (!string.IsNullOrEmpty(mpNumber) &&
                (_configuration.GetValue<bool>("EnableVulnerabilityFixForUnfinishedBooking")
                || GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appMajorVersion, _configuration.GetValue<string>("AndroidVulnerabilityFixVersion"), _configuration.GetValue<string>("iPhoneVulnerabilityFixVersion"))))
            {
                bool validOmniCartRequest = false;
                string authToken = string.Empty;
                try
                {
                    var mpResponse = await new HashPin(_logger, _configuration, _validateHashPinService, _dynamoDBService,_headers, _featureSettings).ValidateHashPinAndGetAuthTokenDynamoDB(mpNumber, hashPinCode, applicationId, deviceId, appMajorVersion);
                    if (mpResponse != null && mpResponse.HashPincode == hashPinCode)
                    {
                        validOmniCartRequest = true;
                        authToken = mpResponse.AuthenticatedToken?.ToString();
                    }

                }
                catch (Exception ex) { string msg = ex.Message; }

                if (!validOmniCartRequest)
                    throw new MOBUnitedException(exceptionMessage);
            }

        }
        private async System.Threading.Tasks.Task<bool> PurgeUnfinishedBookings(Session session, MOBRequest request)
        {
            var savedUBs = await GetSavedUnfinishedBookingEntries(session, request);

            if (savedUBs == null)
                return true;

            var count = 0;
            var errorMsgs = new List<string>();
            foreach (var savedUB in savedUBs)
            {
                try
                {
                    await PurgeAnUnfinishedBooking(session, request, savedUB.Id);
                    count++;
                }
                catch (Exception ex) { errorMsgs.Add(ex.Message); }
            }

            if (savedUBs.Count != 0 && errorMsgs.Count == savedUBs.Count)
                throw new Exception(string.Join(" ", errorMsgs));

            return savedUBs.Count == 0 || count > 0; // consider success if able to delete at least one
        }

        private async Task<List<MOBSHOPUnfinishedBooking>> GetSavedUnfinishedBookingEntries(Session session, MOBRequest request, bool isCatalogOnForTravelerTypes = false)
        {
            if (session.CustomerID <= 0)
                return new List<MOBSHOPUnfinishedBooking>();

            string cslActionName = "SavedItinerary(Get)";
            string savedUnfinishedBookingActionName = "SavedItinerary";
            string savedUnfinishedBookingAugumentName = "CustomerId";

            var response = await _customerPreferencesService.GetCustomerPrefernce<SavedItineraryDataModel>(session.Token, cslActionName, savedUnfinishedBookingActionName, savedUnfinishedBookingAugumentName, session.CustomerID, session.SessionId);

            if (response != null)
            {
                if (response != null && response.Status.Equals(PreferencesConstants.StatusType.Success))
                {
                    List<MOBSHOPUnfinishedBooking> unfinishedBookings = new List<MOBSHOPUnfinishedBooking>();
                    if (response.SavedItineraryList != null)
                    {
                        unfinishedBookings = response.SavedItineraryList.Select(x => (ConfigUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major) && isCatalogOnForTravelerTypes) ? MapToMOBSHOPUnfinishedBookingTtypes(x, request)
                        : MapToMOBSHOPUnfinishedBooking(x, request)).ToList();
                    }
                    return unfinishedBookings;
                }
                else
                {
                    if (response != null && response.Errors != null && response.Errors.Count > 0)
                    {
                        throw new Exception(string.Join(" ", response.Errors.Select(err => err.Message)));
                    }

                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
        }
        private bool IsEnableOmniCartReleaseCandidate4CChanges(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableOmniCartReleaseCandidate4CChanges") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, "Android_EnableOmniCartReleaseCandidate4CChanges_AppVersion", "iPhone_EnableOmniCartReleaseCandidate4CChanges_AppVersion");
        }

        private async Task<bool> PurgeAnUnfinishedBooking(Session session, MOBRequest request, string ubId)
        {
            string savedUnfinishedBookingAugumentName = "CustomerId";
            string savedUnfinishedBookingActionName = "SavedItinerary";

            string url = string.Format("/{0}/{1}/{2}/{3}/{4}", savedUnfinishedBookingActionName, savedUnfinishedBookingAugumentName, session.CustomerID, "accesscode", ubId);

            var jsonResponse = await _customerPreferencesService.PurgeAnUnfinishedBooking(session.Token, url, session.SessionId);

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                var response = JsonConvert.DeserializeObject<SubCommonData>(jsonResponse);
                if (response != null && !response.Status.Equals(PreferencesConstants.StatusType.Success))
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        throw new Exception(string.Join(" ", response.Errors.Select(err => err.Message)));
                    }

                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            return true;
        }

        private MOBSHOPUnfinishedBooking MapToMOBSHOPUnfinishedBooking(GetSavedItineraryDataModel cslEntry, MOBRequest request)
        {
            var ub = cslEntry.SavedItinerary;
            var mobEntry = new MOBSHOPUnfinishedBooking
            {
                SearchExecutionDate = new[] { cslEntry.InsertTimestamp, cslEntry.UpdateTimestamp }.FirstOrDefault(x => !string.IsNullOrEmpty(x)),
                NumberOfAdults = ub.PaxInfoList.Count(px => (United.Services.FlightShopping.Common.PaxType)px.PaxType == United.Services.FlightShopping.Common.PaxType.Adult),
                NumberOfSeniors = ub.PaxInfoList.Count(px => (United.Services.FlightShopping.Common.PaxType)px.PaxType == United.Services.FlightShopping.Common.PaxType.Senior),
                NumberOfChildren2To4 = ub.PaxInfoList.Count(px => (United.Services.FlightShopping.Common.PaxType)px.PaxType == United.Services.FlightShopping.Common.PaxType.Child01),
                NumberOfChildren5To11 = ub.PaxInfoList.Count(px => (United.Services.FlightShopping.Common.PaxType)px.PaxType == United.Services.FlightShopping.Common.PaxType.Child02),
                NumberOfChildren12To17 = ub.PaxInfoList.Count(px => (United.Services.FlightShopping.Common.PaxType)px.PaxType == United.Services.FlightShopping.Common.PaxType.Child03),
                NumberOfInfantOnLap = ub.PaxInfoList.Count(px => (United.Services.FlightShopping.Common.PaxType)px.PaxType == United.Services.FlightShopping.Common.PaxType.InfantLap),
                NumberOfInfantWithSeat = ub.PaxInfoList.Count(px => (United.Services.FlightShopping.Common.PaxType)px.PaxType == United.Services.FlightShopping.Common.PaxType.InfantSeat),
                CountryCode = ub.CountryCode,
                SearchType = GetSeachTypeSelection((United.Services.FlightShopping.Common.SearchType)ub.SearchTypeSelection),
                Trips = ub.Trips.Select(MapToMOBSHOPUnfinishedBookingTrip).ToList(),
                Id = ub.AccessCode
            };

            if (ConfigUtility.EnableSavedTripShowChannelTypes(request.Application.Id, request.Application.Version.Major)) // Map channel
                mobEntry.ChannelType = ub.ChannelType;

            return mobEntry;
        }

        private async System.Threading.Tasks.Task PurgeUnfinishedBookingsV2(Session session, MOBSHOPUnfinishedBookingRequestBase request)
        {
            bool isClearAllSavedTrips = _omniCart.IsClearAllSavedTrips(request.IsOmniCartSavedTrip, request.Application.Id, request.Application.Version.Major, request.IsRemoveAll);
            if (isClearAllSavedTrips)
            {
                string cslActionName = "DeviceID/" + request.DeviceId;
                await PurgeUnfinshedBookings(session, cslActionName, request);
                if (!string.IsNullOrEmpty(request.MileagePlusAccountNumber))
                {
                    cslActionName = "LoyaltyID/" + request.MileagePlusAccountNumber;
                    //await PurgeUnfinishedBookings(session, request);//This to delete the Loyalty trips.
                    await PurgeUnfinshedBookings(session, cslActionName, request);
                }
            }
        }

        private async System.Threading.Tasks.Task PurgeUnfinshedBookings(Session session, String cslActionName, MOBRequest request)
        {
            string url = string.Format("{0}", cslActionName);

            var jsonResponse = await _omniChannelCartService.PurgeUnfinshedBookings(session.Token, cslActionName, session.SessionId);

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                var response = JsonConvert.DeserializeObject<United.OmniChannelCart.Model.OmniChannelCartServiceResponse>(jsonResponse);

                if (!(response != null && !string.IsNullOrEmpty(response.Status) && response.Status.ToUpper() == "SUCCESSFUL"))
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("UnfinishedBooking_UnableToClearMsgs"));
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("UnfinishedBooking_UnableToClearMsgs"));
            }
        }

        private MOBSHOPUnfinishedBooking MapToMOBSHOPUnfinishedBookingTtypes(GetSavedItineraryDataModel cslEntry, MOBRequest request)
        {
            var ub = cslEntry.SavedItinerary;
            MOBSHOPUnfinishedBooking mobEntry = new MOBSHOPUnfinishedBooking();

            mobEntry.SearchExecutionDate = new[] { cslEntry.InsertTimestamp, cslEntry.UpdateTimestamp }.FirstOrDefault(x => !string.IsNullOrEmpty(x));
            mobEntry.TravelerTypes = new List<MOBTravelerType>();
            GetTravelTypesFromShop(ub, mobEntry);
            mobEntry.CountryCode = ub.CountryCode;
            mobEntry.SearchType = GetSeachTypeSelection((United.Services.FlightShopping.Common.SearchType)ub.SearchTypeSelection);
            mobEntry.Trips = ub.Trips.Select(MapToMOBSHOPUnfinishedBookingTrip).ToList();
            mobEntry.Id = ub.AccessCode;
            if (ConfigUtility.EnableSavedTripShowChannelTypes(request.Application.Id, request.Application.Version.Major)) // Map channel
                mobEntry.ChannelType = ub.ChannelType;

            return mobEntry;
        }

        private static void GetTravelTypesFromShop(SerializableSavedItinerary ub, MOBSHOPUnfinishedBooking mobEntry)
        {
            foreach (var t in ub.PaxInfoList.GroupBy(p => p.PaxType))
            {
                switch ((int)t.Key)
                {
                    case (int)United.Services.FlightShopping.Common.PaxType.Adult:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Adult.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.Senior:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Senior.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.Child01:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child2To4.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.Child02:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child5To11.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.Child03:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child12To17.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.Child04:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child12To14.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.Child05:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child15To17.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.InfantSeat:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.InfantSeat.ToString() });
                        break;

                    case (int)United.Services.FlightShopping.Common.PaxType.InfantLap:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.InfantLap.ToString() });
                        break;
                    default:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Adult.ToString() });
                        break;
                }
            }
        }

        private MOBSHOPUnfinishedBookingTrip MapToMOBSHOPUnfinishedBookingTrip(SerializableSavedItinerary.Trip csTrip)
        {
            return new MOBSHOPUnfinishedBookingTrip
            {
                DepartDate = csTrip.DepartDate,
                DepartTime = csTrip.DepartTime,
                Destination = csTrip.Destination,
                Origin = csTrip.Origin,
                Flights = csTrip.Flights.Select(MapToMOBSHOPUnfinishedBookingFlight).ToList()
            };
        }

        private MOBSHOPUnfinishedBookingFlight MapToMOBSHOPUnfinishedBookingFlight(SerializableSavedItinerary.Flight cslFlight)
        {
            var ubMOBFlight = new MOBSHOPUnfinishedBookingFlight
            {
                BookingCode = cslFlight.BookingCode,
                DepartDateTime = cslFlight.DepartDateTime,
                Origin = cslFlight.Origin,
                Destination = cslFlight.Destination,
                FlightNumber = cslFlight.FlightNumber,
                MarketingCarrier = cslFlight.MarketingCarrier,
                ProductType = cslFlight.ProductType,
            };

            if (ubMOBFlight.Connections == null)
                return ubMOBFlight;

            cslFlight.Connections.ForEach(x => ubMOBFlight.Connections.Add(MapToMOBSHOPUnfinishedBookingFlight(x)));

            return ubMOBFlight;
        }
    }
}
