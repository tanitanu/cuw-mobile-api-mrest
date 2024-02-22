using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.Payment;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Service.Presentation.CommonModel;
using United.Service.Presentation.InteractionModel;
using United.Service.Presentation.PaymentModel;
using United.Service.Presentation.PaymentRequestModel;
using United.Service.Presentation.PaymentResponseModel;
using United.Service.Presentation.PersonalizationResponseModel;
using United.Service.Presentation.ProductModel;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.Extensions;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Utility.Enum;
using United.Utility.Helper;
using Address = United.Service.Presentation.CommonModel.Address;
using CslDataVaultResponse = United.Service.Presentation.PaymentResponseModel.DataVault<United.Service.Presentation.PaymentModel.Payment>;
using FormofPaymentOption = United.Mobile.Model.Common.Shopping.FormofPaymentOption;
//using FormofPaymentOption = United.Mobile.Model.Common.Shopping;
using Genre = United.Service.Presentation.CommonModel.Genre;
using MOBFormofPaymentDetails = United.Mobile.Model.Shopping.FormofPayment.MOBFormofPaymentDetails;
using MOBShoppingCart = United.Mobile.Model.Shopping.MOBShoppingCart;
using Product = United.Service.Presentation.ProductModel.Product;
using ProfileFOPCreditCardResponse = United.Mobile.Model.Shopping.ProfileFOPCreditCardResponse;
using ProfileResponse = United.Mobile.Model.Shopping.ProfileResponse;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Common.Helper.FOP
{
    public class FormsOfPayment : IFormsOfPayment
    {
        private readonly ICacheLog<FormsOfPayment> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentUtility _paymentUtility;
        private readonly IProductOffers _shopping;
        private readonly IReferencedataService _referencedataService;
        private readonly IDataVaultService _dataVaultService;
        private readonly IPKDispenserService _pKDispenserService;
        private readonly ICachingService _cachingService;
        private readonly IDPService _dPService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly IProductInfoHelper _productInfoHelper;
        private readonly IProfileCreditCard _profileCreditCard;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly PKDispenserPublicKey _pKDispenserPublicKey;
        private MOBApplication _application = new MOBApplication() { Version = new MOBVersion() };
        private string _deviceId = string.Empty;
        private readonly IHeaders _headers;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IFeatureSettings _featureSettings;
        public FormsOfPayment(ICacheLog<FormsOfPayment> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , IShoppingCartService shoppingCartService
            , IPaymentService paymentService
            , IPaymentUtility paymentUtility
            , IProductOffers shopping
            , IReferencedataService referencedataService
            , IDataVaultService dataVaultService
            , ICachingService cachingService
            , IDPService dPService
            , IPKDispenserService pKDispenserService
            , IDynamoDBService dynamoDBService
            , IProfileCreditCard profileCreditCard
            , IProductInfoHelper productInfoHelper
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , IHeaders headers,
            IShoppingUtility shoppingUtility
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _shoppingCartService = shoppingCartService;
            _paymentService = paymentService;
            _paymentUtility = paymentUtility;
            _shopping = shopping;
            _referencedataService = referencedataService;
            _dataVaultService = dataVaultService;
            _cachingService = cachingService;
            _dPService = dPService;
            _pKDispenserService = pKDispenserService;
            _dynamoDBService = dynamoDBService;
            _profileCreditCard = profileCreditCard;
            _productInfoHelper = productInfoHelper;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _headers = headers;
            _pKDispenserPublicKey = new PKDispenserPublicKey(_configuration, _cachingService, _dPService, _pKDispenserService, headers);
            _shoppingUtility = shoppingUtility;
            _featureSettings = featureSettings;
            ConfigUtility.UtilityInitialize(_configuration);
        }

        public async Task<(List<FormofPaymentOption> response, bool isDefault)> EligibleFormOfPayments(FOPEligibilityRequest request, Session session, bool isDefault, bool IsMilesFOPEnabled = false, List<LogEntry> eligibleFoplogEntries = null)
        {
            var response = new List<FormofPaymentOption>();

            try
            {
                string requestXml = string.Empty;
                if (!string.IsNullOrEmpty(request.Flow) && request.Flow == "RESHOP")
                {
                    request.Flow = "EXCHANGE";
                }
                if (await _featureSettings.GetFeatureSettingValue("EnableFixForEligibleFOPsForViewResSeatMapFlow").ConfigureAwait(false) && request.Flow == FlowType.VIEWRES_SEATMAP.ToString())
                {
                    request.Flow = FlowType.VIEWRES.ToString();
                }
                ShoppingCart shoppingCart = await BuildEligibleFormOfPaymentsRequest(request, session);
                bool isMigrateToJSONService = _configuration.GetValue<bool>("EligibleFopMigrateToJSonService");
                var xmlRequest = isMigrateToJSONService
                                 ? JsonConvert.SerializeObject(shoppingCart)
                                 : XmlSerializerHelper.Serialize<ShoppingCart>(shoppingCart);

                string path = "/FormOfPayment/EligibleFormOfPaymentByShoppingCart";
                string token = session.Token;

                if (ConfigUtility.IsEnableFeature("EnableU4BCorporateBookingFFC", request.Application.Id, request.Application.Version.Major) && session.IsCorporateBooking)
                {
                    var buildEligibleFOPRequest = BuildEligibleFOPRequest(request.CartId, session.IsCorporateBooking.ToString(), shoppingCart.PointOfSale.Country.CountryCode, shoppingCart);
                    path = "/FormOfPayment/EligibleFOP";
                    xmlRequest = JsonConvert.SerializeObject(buildEligibleFOPRequest);


                }
                string xmlResponse = await _paymentService.GetEligibleFormOfPayments(token, path, xmlRequest, _headers.ContextValues.SessionId).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(xmlResponse))
                {
                    Service.Presentation.PaymentResponseModel.EligibleFormOfPayment eligibleFormOfPayment = isMigrateToJSONService
                                                                                                            ? DataContextJsonSerializer.DeserializeUseContract<Service.Presentation.PaymentResponseModel.EligibleFormOfPayment>(xmlResponse)
                                                                                                            : XmlSerializerHelper.Deserialize<Service.Presentation.PaymentResponseModel.EligibleFormOfPayment>(xmlResponse);

                    if ((request.Products != null) && (request.Products.Count == 1) && (ConfigUtility.IsETCchangesEnabled(request.Application.Id, request.Application.Version.Major) ? request.Flow != FlowType.BOOKING.ToString() : true))
                    {
                        response = eligibleFormOfPayment.ProductFormOfPayment[0].FormsOfPayment.GroupBy(p => p.Payment.Type.Key).Select(x => x.FirstOrDefault()).Select(x => new FormofPaymentOption { Category = x.Payment.Category, FullName = (x.Payment.Category == "CC") ? "Credit Card" : x.Payment.Type.Description, Code = x.Payment.Type.Key, FoPDescription = x.Payment.Type.Description }).ToList();
                    }
                    else
                    {
                        response = eligibleFormOfPayment.FormsOfPayment.GroupBy(p => p.Payment.Type.Key).Select(x => x.FirstOrDefault()).Select(x => new FormofPaymentOption { Category = x.Payment.Category, FullName = (x.Payment.Category == "CC") ? "Credit Card" : x.Payment.Type.Description, Code = x.Payment.Type.Key, FoPDescription = x.Payment.Type.Description }).ToList();
                    }

                    isDefault = false;
                    if (IsMilesFOPEnabled && ConfigUtility.IsMilesFOPEnabled())
                    {
                        response.Insert(1, new FormofPaymentOption { Category = "MILES", FullName = "Use miles", Code = null, FoPDescription = null });
                    }
                    if (IsMilesFOPEnabled)
                    {
                        var MilesFOP = response.FirstOrDefault(fop => fop.Code == "MILE");
                        if (MilesFOP != null)
                        {
                            MilesFOP.Category = "USEMILES";
                            MilesFOP.FoPDescription = "Use miles";
                            MilesFOP.FullName = "Use miles";
                        }
                    }
                    else
                    {
                        response = response.Where(x => x.Code != "MILE").ToList();
                    }
                    //If Payment service enables ETC fop for SEATS and PCU it will automatically shows ETC as FOP .So,disabling etc for lower versions .
                    if (request.Flow == FlowType.VIEWRES.ToString() && !GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("Android_EnableETCManageRes_AppVersion"), _configuration.GetValue<string>("iPhone_EnableETCManageRes_AppVersion"))
                    || (_configuration.GetValue<bool>("LoadVIewResVormetricForVIEWRES_SEATMAPFlowToggle") && request.Flow == FlowType.VIEWRES_SEATMAP.ToString()))
                    {
                        if (response.Exists(x => x.Category == "CERT"))
                        {
                            response = response.Where(x => x.Category != "CERT").ToList();
                        }
                    }

                    if ((request.Flow == FlowType.BOOKING.ToString() && !ConfigUtility.IncludeMoneyPlusMiles(request.Application.Id, request.Application.Version.Major)) || session.IsAward || session.IsCorporateBooking
                        || (!_configuration.GetValue<bool>("DisableFixForMMCATripTipSeatChange_MOBILE17339") && request.Flow == FlowType.VIEWRES_SEATMAP.ToString()))
                    {
                        if (response.Exists(x => x.Category == "MILES"))
                        {
                            response = response.Where(x => x.Category != "MILES").ToList();
                        }
                    }
                    if (!_paymentUtility.IncludeTravelBankFOP(request.Application.Id, request.Application.Version.Major) ||
                        request.Flow != FlowType.BOOKING.ToString() ||
                        session.IsAward ||
                        (await _paymentUtility.GetTravelBankBalance(session.SessionId)) == 0.00)
                    {
                        if (response.Exists(x => x.Category == "TRAVELBANK"))
                        {
                            response = response.Where(x => x.Category != "TRAVELBANK").ToList();
                        }
                    }

                    if (request.Flow == FlowType.BOOKING.ToString()
                        && _paymentUtility.IncludeTravelCredit(request.Application.Id, request.Application.Version.Major)
                        && !session.IsAward && (ConfigUtility.IsEnableFeature("EnableU4BCorporateBookingFFC", request.Application.Id, request.Application.Version.Major) ? true : !session.IsCorporateBooking)
                        && response.Exists(x => x.Category == "CERT"))
                    {
                        response.Add(
                            new FormofPaymentOption
                            {
                                Category = "TRAVELCREDIT",
                                Code = "TC",
                                DeleteOrder = false,
                                FoPDescription = "Travel Credit",
                                FullName = "Travel Credit"
                            });
                        response.RemoveAll(x => x.Category == "CERT");
                    }

                    if (request.Flow == FlowType.BOOKING.ToString()
                        && _paymentUtility.IncludeTravelCredit(request.Application.Id, request.Application.Version.Major)
                        && !session.IsAward && !ConfigUtility.IsEnableFeature("EnableU4BCorporateBookingFFC", request.Application.Id, request.Application.Version.Major)
                  && session.IsCorporateBooking)
                    {
                        response.RemoveAll(x => x.Category == "CERT");
                    }
                }
            }
            catch (System.Net.WebException wex)
            {
                if (wex.Response != null)
                {
                    _logger.LogError("EligibleFormOfPayments - Exception {ErrorMessageResponse} and {SessionId}", wex.Response, request.SessionId);

                    response.Add(new FormofPaymentOption { Category = "CC", FullName = "Credit Card", Code = null, FoPDescription = null });
                    response.Add(new FormofPaymentOption { Category = "PP", FullName = "PayPal", Code = null, FoPDescription = null });
                    response.Add(new FormofPaymentOption { Category = "PPC", FullName = "PayPal Credit", Code = null, FoPDescription = null });
                    response.Add(new FormofPaymentOption { Category = "MPS", FullName = "Masterpass", Code = null, FoPDescription = null });
                    response.Add(new FormofPaymentOption { Category = "APP", FullName = "Apple Pay", Code = null, FoPDescription = null });
                    isDefault = true;
                    if (IsMilesFOPEnabled && ConfigUtility.IsMilesFOPEnabled())
                    {
                        response.Insert(1, new FormofPaymentOption { Category = "MILES", FullName = "Use miles", Code = null, FoPDescription = null });
                    }
                }
            }
            catch (MOBUnitedException coex)
            {
                response = null;
                _logger.LogWarning("EligibleFormOfPayments - Exception {@UnitedException}", JsonConvert.SerializeObject(coex));
            }
            catch (System.Exception ex)
            {
                response = null;
                ExceptionWrapper exceptionWrapper = new ExceptionWrapper(ex);
                _logger.LogError("EligibleFormOfPayments - Exception {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));
            }

            return (response, isDefault);
        }

        private EligibleFOPRequest BuildEligibleFOPRequest(string cartId, string isCorporateBooking, string countryCode, ShoppingCart shoppingCart)
        {
            EligibleFOPRequest eligibleFOPRequest = new EligibleFOPRequest();
            eligibleFOPRequest.CartID = cartId;
            eligibleFOPRequest.IsCorporateBooking = isCorporateBooking.ToString();
            eligibleFOPRequest.PointOfSale = shoppingCart.PointOfSale.Country.CountryCode;
            eligibleFOPRequest.ShoppingCart = shoppingCart;

            return eligibleFOPRequest;
        }

        public async Task<MOBPersistFormofPaymentResponse> GetCreditCardToken(MOBPersistFormofPaymentRequest request, Session session)
        {
            MOBPersistFormofPaymentResponse response = new MOBPersistFormofPaymentResponse();

            var persistShoppingCart = new MOBShoppingCart();
            persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(_headers.ContextValues.SessionId, persistShoppingCart.ObjectName, new List<string> { _headers.ContextValues.SessionId, persistShoppingCart.ObjectName }).ConfigureAwait(false);
            if (persistShoppingCart == null)
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            var bookingPathReservation = new Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(_headers.ContextValues.SessionId, bookingPathReservation.ObjectName, new List<string> { _headers.ContextValues.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

            //Build formofpaymentdetails
            var formofPaymentDetails = new MOBFormofPaymentDetails();
            if (ConfigUtility.IsManageResETCEnabled(request.Application.Id, request.Application.Version.Major) && request.Flow == FlowType.VIEWRES.ToString()
                && persistShoppingCart?.FormofPaymentDetails?.TravelCertificate != null)
            {
                formofPaymentDetails.TravelCertificate = persistShoppingCart.FormofPaymentDetails.TravelCertificate;
                formofPaymentDetails.IsOtherFOPRequired = persistShoppingCart.FormofPaymentDetails != null ? persistShoppingCart.FormofPaymentDetails.IsOtherFOPRequired : true;
            }
            if (!(persistShoppingCart.Flow == FlowType.BOOKING.ToString() && bookingPathReservation.IsRedirectToSecondaryPayment))
            {
                formofPaymentDetails.CreditCard = await GetCreditCardWithToken(request.FormofPaymentDetails.CreditCard, session, request);
            }
            else
            {
                formofPaymentDetails.CreditCard = persistShoppingCart.FormofPaymentDetails.CreditCard;
                formofPaymentDetails.SecondaryCreditCard = await GetCreditCardWithToken(request.FormofPaymentDetails.CreditCard, session, request);
            }
            formofPaymentDetails.FormOfPaymentType = MOBFormofPayment.CreditCard.ToString();
            formofPaymentDetails.EmailAddress = request.FormofPaymentDetails.EmailAddress;
            formofPaymentDetails.BillingAddress = await GetBillingAddressWithValidStateCode(request, session);
            formofPaymentDetails.Phone = request.FormofPaymentDetails.Phone;

            persistShoppingCart.FormofPaymentDetails = formofPaymentDetails;
            persistShoppingCart.CartId = request.CartId;
            persistShoppingCart.TotalPrice = String.Format("{0:0.00}", request.Amount);

            if (ConfigUtility.EnableInflightContactlessPayment(request.Application.Id, request.Application.Version.Major, persistShoppingCart.Flow != FlowType.BOOKING.ToString()))
            {
                if (persistShoppingCart?.InFlightContactlessPaymentEligibility?.IsEligibleInflightCLPayment ?? false)
                {
                    persistShoppingCart.InFlightContactlessPaymentEligibility.IsCCSelectedForContactless = request.IsCCSelectedForContactless;
                }
            }

            await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, persistShoppingCart.ObjectName }, persistShoppingCart.ObjectName).ConfigureAwait(false);
            response.ShoppingCart = persistShoppingCart;
            List<FormofPaymentOption> persistedEligibleFormofPayments = new List<FormofPaymentOption>();
            persistedEligibleFormofPayments = await _sessionHelperService.GetSession<List<FormofPaymentOption>>(request.SessionId, persistedEligibleFormofPayments.GetType().FullName, new List<string> { request.SessionId, persistedEligibleFormofPayments.GetType().FullName }).ConfigureAwait(false);
            response.EligibleFormofPayments = persistedEligibleFormofPayments;
            response.PkDispenserPublicKey = await _pKDispenserPublicKey.GetCachedOrNewpkDispenserPublicKey(request.Application.Id, request.Application.Version.Major, request.DeviceId, session.SessionId, session.Token, session?.CatalogItems);

            return response;
        }

        private async Task<LoadReservationAndDisplayCartResponse> GetCartInformation(string sessionId, Mobile.Model.MOBApplication application, string device, string cartId, string token, WorkFlowType workFlowType = WorkFlowType.InitialBooking)
        {
            LoadReservationAndDisplayCartRequest loadReservationAndDisplayCartRequest = new LoadReservationAndDisplayCartRequest();
            LoadReservationAndDisplayCartResponse loadReservationAndDisplayResponse = new LoadReservationAndDisplayCartResponse();
            loadReservationAndDisplayCartRequest.CartId = cartId;
            loadReservationAndDisplayCartRequest.WorkFlowType = workFlowType;
            string jsonRequest = JsonConvert.SerializeObject(loadReservationAndDisplayCartRequest);

            loadReservationAndDisplayResponse = await _shoppingCartService.GetCartInformation<LoadReservationAndDisplayCartResponse>(token, "LoadReservationAndDisplayCart", jsonRequest, _headers.ContextValues.SessionId).ConfigureAwait(false);

            return loadReservationAndDisplayResponse;
        }

        private async Task<ShoppingCart> BuildEligibleFormOfPaymentsRequest(FOPEligibilityRequest request, Session session)
        {
            string channel = (request.Application.Id == 1 ? "MOBILE-IOS" : "MOBILE-Android");
            string productContext = string.Format
                (@" <Reservation xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/United.Service.Presentation.ReservationModel"">            <Channel>{0}</Channel>            <Type xmlns:d2p1=""http://schemas.datacontract.org/2004/07/United.Service.Presentation.CommonModel"">            <d2p1:Genre>            <d2p1:Key>{1}</d2p1:Key>            </d2p1:Genre>            </Type>            </Reservation> ", channel, request.Flow);
            ShoppingCart shoppingCart = new ShoppingCart
            {
                ID = new UniqueIdentifier
                {
                    ID = (request.CartId != null) ? request.CartId : Guid.NewGuid().ToString()
                },
                Items = new Collection<ShoppingCartItem>()
            };
            var shoppingCartItem = new ShoppingCartItem
            {
                Product = new Collection<Product>()
            };

            foreach (var product in request.Products)
            {
                Product productObj = new Product
                {
                    Code = (_configuration.GetValue<bool>("EnableFareLockPurchaseViewRes") && product.Code != null && product.Code.Equals("FLK_VIEWRES", StringComparison.OrdinalIgnoreCase)) ? "FLK" : product.Code,
                    Description = product.ProductDescription
                };
                shoppingCartItem.Product.Add(productObj);
            }

            if (ConfigUtility.IsETCchangesEnabled(request.Application.Id, request.Application.Version.Major) && request.Flow == FlowType.BOOKING.ToString()
                || (request.Flow == FlowType.VIEWRES.ToString() || request.Flow == FlowType.BAGGAGECALCULATOR.ToString()) && ConfigUtility.IsManageResETCEnabled(request.Application.Id, request.Application.Version.Major))
            {
                channel = (request.Application.Id == 1 ? _configuration.GetValue<string>("eligibleFopMobileioschannelname") : _configuration.GetValue<string>("eligibleFopMobileandriodchannelname"));
                string newProductContext = string.Empty;
                newProductContext = await BuildProductContextForEligibleFoprequest(request, channel, session);
                productContext = string.IsNullOrEmpty(newProductContext) ? productContext : newProductContext;
                //FareLock code should be FLK Instead of FareLock
                if (shoppingCartItem.Product.Exists(x => x.Code == "FareLock"))
                {
                    shoppingCartItem.Product.Where(x => x.Code == "FareLock").FirstOrDefault().Code = "FLK";
                }
            }
            shoppingCartItem.ProductContext = new Collection<string>
            {
                productContext
            };
            shoppingCart.Items.Add(shoppingCartItem);
            shoppingCart.PointOfSale = new Service.Presentation.CommonModel.PointOfSale
            {
                Country = new Service.Presentation.CommonModel.Country
                {
                    CountryCode = "US"
                }
            };

            return shoppingCart;
        }

        private string GetMobilePathforEligibleFop(string flow)
        {
            string mobilePath = string.Empty;
            switch (flow)
            {
                case "BOOKING":
                    mobilePath = "INITIAL";
                    break;
                case "RESHOP":
                    mobilePath = "EXCHANGE";
                    break;
                case "BAGGAGECALCULATOR":
                    mobilePath = "VIEWRES";
                    break;
                default:
                    return flow;
            }
            return mobilePath;
        }

        private async Task<string> BuildProductContextForEligibleFoprequest(FOPEligibilityRequest request, string channel, Session session)
        {
            var reservation = new Service.Presentation.ReservationModel.Reservation();
            string mobilePath = GetMobilePathforEligibleFop(request.Flow);
            reservation.Channel = channel;
            reservation.Type = new Collection<Genre>
            {
                new Genre { Key = mobilePath }
            };
            switch (request.Flow)
            {
                case "BOOKING":
                    Reservation persistedReservation = new Reservation();
                    persistedReservation = await _sessionHelperService.GetSession<Reservation>(_headers.ContextValues.SessionId, persistedReservation.ObjectName, new List<string> { _headers.ContextValues.SessionId, persistedReservation.ObjectName }).ConfigureAwait(false);
                    List<ReservationFlightSegment> segments = DataContextJsonSerializer.DeserializeUseContract<List<ReservationFlightSegment>>(persistedReservation.CSLReservationJSONFormat);
                    if (persistedReservation != null)
                    {
                        reservation.FlightSegments = segments.ToCollection();//ETC is offered for only united operated flights.Need to send the segment details with operating carrier code.
                        if (ConfigUtility.IncludeMoneyPlusMiles(request.Application.Id, request.Application.Version.Major))
                        {
                            reservation = await _sessionHelperService.GetSession<Service.Presentation.ReservationModel.Reservation>(_headers.ContextValues.SessionId, reservation.GetType().FullName, new List<string> { _headers.ContextValues.SessionId, reservation.GetType().FullName }).ConfigureAwait(false);

                            if (reservation == null)
                            {
                                var cartInfo = await GetCartInformation(request.SessionId, request.Application, request.DeviceId, request.CartId, session.Token);
                                reservation = cartInfo.Reservation;
                                await _sessionHelperService.SaveSession<Service.Presentation.ReservationModel.Reservation>(cartInfo.Reservation, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, typeof(Service.Presentation.ReservationModel.Reservation).FullName }, typeof(Service.Presentation.ReservationModel.Reservation).FullName).ConfigureAwait(false);
                            }

                            reservation.Channel = channel;
                            reservation.Type = new Collection<Genre>
                            {
                                new Genre { Key = mobilePath }
                            };
                        }
                    }
                    return ProviderHelper.SerializeXml(reservation);
                case "VIEWRES":
                case "BAGGAGECALCULATOR":
                    ReservationDetail reservationDetail = new ReservationDetail();
                    reservationDetail = await _sessionHelperService.GetSession<ReservationDetail>(_headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName, new List<string>() { _headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName }).ConfigureAwait(false);

                    if (reservationDetail != null)
                    {

                        reservation.FlightSegments = reservationDetail.Detail.FlightSegments;//ETC is offered for only united operated flights.Need to send the segment details with operating carrier code.
                    }
                    return ProviderHelper.SerializeXml(reservation);
                default: return string.Empty;
            }
        }

        //v788383: 11/02/2018 - GetPaymentToken with Generic request and responsGetProductDetailsFromCartIDe
        public async Task<List<ProdDetail>> GetProductDetailsFromCartID(Session session, SelectTripRequest request)
        {
            ShoppingCart flightReservationResponseShoppingCart = new ShoppingCart();
            List<ProdDetail> shoppingCartProducts = new List<ProdDetail>();
            List<string> productCodes = new List<string>();
            string token = session.Token;

            flightReservationResponseShoppingCart = await _shoppingCartService.GetProductDetailsFromCartID<United.Service.Presentation.InteractionModel.ShoppingCart>(token, session.CartId, _headers.ContextValues.SessionId).ConfigureAwait(false);

            if (flightReservationResponseShoppingCart != null)
            {
                productCodes = flightReservationResponseShoppingCart.Items.Select(x => x.Product.FirstOrDefault().Code).ToList();
                foreach (string productCode in productCodes)
                {
                    var prodDetail = new ProdDetail()
                    {
                        Code = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Code).FirstOrDefault().ToString(),
                        ProdDescription = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Description).FirstOrDefault().ToString(),
                        ProdTotalPrice = String.Format("{0:0.00}", flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).SelectMany(x => x.Price.Totals).Where(x => (x.Name != null ? x.Name.ToUpper() != "TAXTOTAL" : true)).Select(x => x.Amount).ToList().Sum()),
                        ProdDisplayTotalPrice = Decimal.Parse(flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).SelectMany(x => x.Price.Totals).Where(x => (x.Name != null ? x.Name.ToUpper() != "TAXTOTAL" : true)).Select(x => x.Amount).ToList().Sum().ToString()).ToString("c"),
                    };
                    shoppingCartProducts.Add(prodDetail);
                }
            }
            else
            {
                _logger.LogInformation("ShoppingCartReturningEmptyProducts {Exception} and {SessionId}", "Shopping Cart is returning Empty Products. We just logged at Exception to get it inserted into uatb_log table but the response for this is Success.", request.SessionId);
            }
            return shoppingCartProducts;
        }

        public async Task<MOBFOPAcquirePaymentTokenResponse> GetPayPalToken(MOBFOPAcquirePaymentTokenRequest request, Session session)
        {
            var response = new MOBFOPAcquirePaymentTokenResponse();
            string token = session.Token == null ? (await _dPService.GetAnonymousToken(request.Application.Id, request.DeviceId, _configuration).ConfigureAwait(false)) : session.Token;

            string path = string.Format("{0}{1}", "/PayPal/GetPayPalUrl/", request.CartId);
            var requestPayPal = new PayPal();
            PayPal responsePayPal = new PayPal(); ;
            requestPayPal.Amount = Convert.ToDouble(request.Amount);
            requestPayPal.BillingAddress = new Address
            {
                Country = new Service.Presentation.CommonModel.Country() { CountryCode = request.CountryCode }
            };
            requestPayPal.ReturnURL = string.Format(_configuration.GetValue<string>("AcquirePayPalToken - ReturnURL"));
            requestPayPal.CancelURL = string.Format(_configuration.GetValue<string>("AcquirePayPalToken - CancelURL"));
            requestPayPal.Type = new Genre() { Key = request.FormofPaymentCode };
            string jsonRequest = JsonConvert.SerializeObject(requestPayPal);

            string response1 = await _paymentService.GetEligibleFormOfPayments(token, path, jsonRequest, _headers.ContextValues.SessionId);

            if (!string.IsNullOrEmpty(response1))
            {
                responsePayPal = JsonConvert.DeserializeObject<PayPal>(response1);
                response.Token = responsePayPal.TokenID;
            }
            return response;
        }

        public async Task<MOBFOPAcquirePaymentTokenResponse> GetMasterpassToken(MOBFOPAcquirePaymentTokenRequest request, Session session)
        {
            string token = session.Token;

            string path = "/MasterPassWallet/CreateMasterPassOpenWalletSession";
            OpenWalletSessionRequest openWalletRequest = BuildOpenWalletSessionRequest(Convert.ToDouble(request.Amount), session.CartId);
            string jsonRequest = JsonConvert.SerializeObject(openWalletRequest);

            var jsonResponse = await _paymentService.GetEligibleFormOfPayments(token, path, jsonRequest, _headers.ContextValues.SessionId);
            return DeserialiseAndBuildMOBFOPAcquirePaymentTokenResponse(jsonResponse);
        }


        private MOBFOPAcquirePaymentTokenResponse DeserialiseAndBuildMOBFOPAcquirePaymentTokenResponse(string jsonResponse)
        {
            MOBFOPAcquirePaymentTokenResponse response = new MOBFOPAcquirePaymentTokenResponse();

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                var openWalletResponse = JsonConvert.DeserializeObject<OpenWalletSessionResponse>(jsonResponse);
                if (openWalletResponse != null && openWalletResponse.ServiceStatus != null && openWalletResponse.ServiceStatus.StatusType.ToUpper().Equals("SUCCESS"))
                {
                    response.Token = string.Format(_configuration.GetValue<string>("MasterpassURL"),
                                                                    openWalletResponse.AllowedCardTypes,
                                                                    _configuration.GetValue<string>("AcquireMasterpassToken-ShippingCountris"),
                                                                    _configuration.GetValue<string>("AcquireMasterpassToken-CallbackURL"),
                                                                    openWalletResponse.MerchantCheckoutID,
                                                                    openWalletResponse.RequestToken);
                    response.CslSessionId = openWalletResponse.SessionID;
                }
                else
                {
                    if (openWalletResponse != null)
                    {
                        ThrowExceptionAsPerErrorResponse(openWalletResponse.Error);
                    }
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            return response;
        }

        private void ThrowExceptionAsPerErrorResponse(Collection<Service.Presentation.CommonModel.ExceptionModel.Error> errors)
        {
            if (errors != null && errors.Count > 0)
            {
                string errorMessage = string.Empty;
                foreach (var error in errors)
                {
                    errorMessage = errorMessage + " " + (error.Description ?? error.Text);
                }
                throw new System.Exception(errorMessage.Trim());
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
        }

        private OpenWalletSessionRequest BuildOpenWalletSessionRequest(double amount, string cartId)
        {
            OpenWalletSessionRequest openWalletRequest = new OpenWalletSessionRequest
            {
                Amount = Convert.ToString(amount),
                Currency = _configuration.GetValue<string>("AcquireMasterpassToken-CurrencyCode"),
                OriginCallingURL =
                _configuration.GetValue<string>("AcquireMasterpassToken-OriginCallingURL"),
                OriginURL =
               _configuration.GetValue<string>("AcquireMasterpassToken-OriginURL"),
                CartID = cartId,
                PointOfSale = _configuration.GetValue<string>("AcquireMasterPassToken-PointOfSale")
            };
            return openWalletRequest;
        }

        public async Task<MOBPersistFormofPaymentResponse> PersistFormofPaymentDetails(MOBPersistFormofPaymentRequest request, Session session)
        {
            MOBPersistFormofPaymentResponse response = new MOBPersistFormofPaymentResponse();
            response = await BuildPersistFormofPaymentDetailsresponse(request, session);

            ProfileResponse persistedProfileResponse = new ProfileResponse();
            persistedProfileResponse = await _sessionHelperService.GetSession<ProfileResponse>(session.SessionId, persistedProfileResponse.ObjectName, new List<string> { session.SessionId, persistedProfileResponse.ObjectName });
            response.Profiles = persistedProfileResponse != null ? persistedProfileResponse.Response.Profiles : null;
            return response;
        }

        private async Task<MOBPersistFormofPaymentResponse> BuildPersistFormofPaymentDetailsresponse(MOBPersistFormofPaymentRequest request, Session session)
        {
            MOBPersistFormofPaymentResponse response = new MOBPersistFormofPaymentResponse();
            var formofPaymentDetails = new MOBFormofPaymentDetails();
            MOBShoppingCart persistShoppingCart = new MOBShoppingCart();
            persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(session.SessionId, persistShoppingCart.ObjectName, new List<string> { session.SessionId, persistShoppingCart.ObjectName });
            if (request.FormofPaymentDetails.FormOfPaymentType.ToUpper() == MOBFormofPayment.PayPal.ToString().ToUpper() || request.FormofPaymentDetails.FormOfPaymentType.ToUpper() == MOBFormofPayment.PayPalCredit.ToString().ToUpper() || request.FormofPaymentDetails.PayPal != null)
            {
                formofPaymentDetails = await PersistPayPalDetails(request, session);
                formofPaymentDetails.FormOfPaymentType = request.FormofPaymentDetails.FormOfPaymentType.ToString();
            }
            else if (request.FormofPaymentDetails.FormOfPaymentType.ToUpper() == MOBFormofPayment.Masterpass.ToString().ToUpper() || request.FormofPaymentDetails.masterPass != null)
            {
                var acquireMasterpassTokenResponse = new MOBFOPAcquirePaymentTokenResponse();
                acquireMasterpassTokenResponse = await _sessionHelperService.GetSession<MOBFOPAcquirePaymentTokenResponse>(request.SessionId, acquireMasterpassTokenResponse.ObjectName, new List<string> { request.SessionId, acquireMasterpassTokenResponse.ObjectName });
                request.FormofPaymentDetails.masterPass.CslSessionId = acquireMasterpassTokenResponse.CslSessionId;
                formofPaymentDetails = await PersistMasterPassDetails(request, session);
                formofPaymentDetails.FormOfPaymentType = MOBFormofPayment.Masterpass.ToString();
            }
            else if (request.FormofPaymentDetails.FormOfPaymentType.ToUpper() == MOBFormofPayment.ApplePay.ToString().ToUpper())
            {
                formofPaymentDetails.FormOfPaymentType = MOBFormofPayment.ApplePay.ToString();
            }
            else if (request.FormofPaymentDetails.FormOfPaymentType.ToUpper() == MOBFormofPayment.Uplift.ToString().ToUpper())
            {
                formofPaymentDetails.FormOfPaymentType = MOBFormofPayment.Uplift.ToString();
                formofPaymentDetails.Uplift = await GetCreditCardWithToken(request.FormofPaymentDetails.Uplift, session, request);
                formofPaymentDetails.EmailAddress = request.FormofPaymentDetails.EmailAddress;
                formofPaymentDetails.BillingAddress = await GetBillingAddressWithValidStateCode(request, session);
                formofPaymentDetails.Phone = request.FormofPaymentDetails.Phone;
            }
            else if (request.FormofPaymentDetails.FormOfPaymentType.ToUpper() == MOBFormofPayment.MilesFormOfPayment.ToString().ToUpper() && ConfigUtility.IsMilesFOPEnabled())
            {
                var profilePersist = new ProfileFOPCreditCardResponse();
                profilePersist = await _sessionHelperService.GetSession<ProfileFOPCreditCardResponse>(request.SessionId, profilePersist.ObjectName, new List<string> { request.SessionId, profilePersist.ObjectName }).ConfigureAwait(false);
                if (profilePersist != null && profilePersist.Response != null && profilePersist.Response.Profiles != null && profilePersist.Response.Profiles.Count > 0 && profilePersist.Response.Profiles[0].Travelers != null &&
                    profilePersist.Response.Profiles[0].Travelers.Count > 0 && profilePersist.Response.Profiles[0].Travelers[0].MileagePlus != null)
                {
                    var MilagePlus = profilePersist.Response.Profiles[0].Travelers[0].MileagePlus;
                    var mileFOP = new MilesFOP
                    {
                        Name = new MOBName()
                        {
                            First = profilePersist.Response.Profiles[0].Travelers[0].FirstName,
                            Last = profilePersist.Response.Profiles[0].Travelers[0].LastName,
                            Middle = profilePersist.Response.Profiles[0].Travelers[0].MiddleName,
                            Suffix = profilePersist.Response.Profiles[0].Travelers[0].Suffix,
                            Title = profilePersist.Response.Profiles[0].Travelers[0].Title
                        },
                        ProfileOwnerMPAccountNumber = profilePersist.Response.MileagePlusNumber,
                        CustomerId = profilePersist.Response.Profiles[0].ProfileId,
                        RequiredMiles = Convert.ToInt32(persistShoppingCart.TotalMiles),
                        AvailableMiles = MilagePlus.AccountBalance
                    };
                    mileFOP.HasEnoughMiles = mileFOP.AvailableMiles >= mileFOP.RequiredMiles ? true : false;
                    mileFOP.DisplayRequiredMiles = UtilityHelper.FormatAwardAmountForDisplay(mileFOP.RequiredMiles.ToString(), false);
                    mileFOP.DisplayAvailableMiles = UtilityHelper.FormatAwardAmountForDisplay(mileFOP.AvailableMiles.ToString(), false);
                    if (!mileFOP.HasEnoughMiles)
                    {
                        persistShoppingCart.AlertMessages = new List<Section>
                        {
                            new Section()
                            {
                                Text1 = "Insufficient miles for purchase",
                                Text2 = "You don't have enough miles to complete this purchase. Please select a different payment method and try again."
                            }
                        };
                    }
                    formofPaymentDetails.MilesFOP = mileFOP;
                    formofPaymentDetails.FormOfPaymentType = MOBFormofPayment.MilesFormOfPayment.ToString();
                }
            }

            if (persistShoppingCart == null)
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            persistShoppingCart.FormofPaymentDetails = formofPaymentDetails;
            persistShoppingCart.CartId = request.CartId;
            persistShoppingCart.TotalPrice = String.Format("{0:0.00}", request.Amount);
            if (!persistShoppingCart.Products.Any(x => x.Code == "BAG"))//needed for bags since we have currency code as well
            {
                persistShoppingCart.DisplayTotalPrice = Decimal.Parse(request.Amount).ToString("c");
            }

            await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, persistShoppingCart.ObjectName }, persistShoppingCart.ObjectName).ConfigureAwait(false);
            response.ShoppingCart = persistShoppingCart;
            return response;
        }

        private async Task<MOBFormofPaymentDetails> PersistPayPalDetails(MOBPersistFormofPaymentRequest request, Session session)
        {
            MOBFormofPaymentDetails response = new MOBFormofPaymentDetails();
            var payPalResponse = new Service.Presentation.PaymentModel.PayPal();

            var payPalRequest = new Service.Presentation.PaymentModel.PayPal
            {
                Amount = Convert.ToDouble(request.Amount),
                BillingAddress = new Service.Presentation.CommonModel.Address
                {
                    Country = new Service.Presentation.CommonModel.Country() { CountryCode = request.FormofPaymentDetails.PayPal.BillingAddressCountryCode } // Make sure the country code here for AcquirePayPalCreditCard is the same from Billing address counrty code or some thing differenct.
                },
                PayerID = request.FormofPaymentDetails.PayPal.PayerID,
                TokenID = request.FormofPaymentDetails.PayPal.PayPalTokenID
            };
            #region
            string path = string.Format("{0}{1}", "/PayPal/GetPayPalCustomerDetails/", request.CartId);
            string jsonRequest = JsonConvert.SerializeObject(payPalRequest);
            string jsonResponse = await _paymentService.GetEligibleFormOfPayments(session.Token, path, jsonRequest, session.SessionId);

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                #region
                payPalResponse = JsonConvert.DeserializeObject<PayPal>(jsonResponse);

                if (payPalResponse != null)
                {
                    #region Populate values from paypal creditcard response
                    response.PayPalPayor = new MOBPayPalPayor
                    {
                        PayPalCustomerID = payPalResponse.Payor.CustomerID,
                        PayPalGivenName = payPalResponse.Payor.GivenName,
                        PayPalStatus = payPalResponse.Payor.Status.Description,
                        PayPalContactPhoneNumber = GetKeyValueFromAddressCharacteristicCollection(payPalResponse.BillingAddress, "PHONE"),
                        PayPalBillingAddress = ConvertCslBillingAddressToMOBAddress(payPalResponse.BillingAddress, MOBFormofPayment.PayPal),
                        PayPalContactEmailAddress = payPalResponse.Payor.Contact.Emails.Select(x => x.Address).FirstOrDefault()
                    };
                    response.PayPal = request.FormofPaymentDetails.PayPal;
                    #endregion 
                }
                else
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                #endregion
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            #endregion

            return response;
        }

        private async Task<MOBFormofPaymentDetails> PersistMasterPassDetails(MOBPersistFormofPaymentRequest request, Session session)
        {
            MOBFormofPaymentDetails response = new MOBFormofPaymentDetails();
            string token = session.Token;

            string path = "/MasterPassWallet/GetMasterPassSessionDetails";
            United.Service.Presentation.PaymentModel.Wallet wallet = BuildWalletObject(request.FormofPaymentDetails.masterPass, request.CartId);

            string jsonRequest = JsonConvert.SerializeObject(wallet);

            var jsonResponse = await _paymentService.GetEligibleFormOfPayments(token, path, jsonRequest, _headers.ContextValues.SessionId);

            response.MasterPassSessionDetails = await DeserialiseAndBuildMOBMasterpassSessionDetailsFOP(jsonResponse);
            response.masterPass = request.FormofPaymentDetails.masterPass;
            return response;
        }

        private async Task<MasterpassSessionDetails> DeserialiseAndBuildMOBMasterpassSessionDetailsFOP(string jsonResponse)
        {
            MasterpassSessionDetails response = new MasterpassSessionDetails();

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                var walletSessionResponse = JsonConvert.DeserializeObject<WalletSessionResponse>(jsonResponse);
                if (walletSessionResponse != null && walletSessionResponse.Response != null && walletSessionResponse.Response.Message != null && walletSessionResponse.Response.Message[0].Text.ToUpper().Equals("SUCCESSFUL"))
                {
                    var payment = walletSessionResponse.Payment;
                    CreditCard cc = (CreditCard)walletSessionResponse.Payment;
                    response.AccountNumber = payment.AccountNumber;
                    response.AccountNumberEncrypted = payment.AccountNumberEncrypted;
                    response.AccountNumberHMAC = payment.AccountNumberHMAC;
                    response.AccountNumberLastFourDigits = payment.AccountNumberLastFourDigits;
                    response.AccountNumberMasked = payment.AccountNumberMasked;
                    response.AccountNumberToken = payment.AccountNumberToken;
                    response.ExpirationDate = cc.ExpirationDate;
                    response.Amount = payment.Amount;
                    response.OperationID = payment.OperationID;
                    response.GivenName = payment.Payor.GivenName;
                    response.SurName = payment.Payor.Surname;
                    response.Code = cc.Code;
                    response.CreditCardTypeCode = Convert.ToInt32(cc.CreditCardTypeCode);
                    response.Description = Convert.ToInt32(cc.Description);
                    response.Name = cc.Name;
                    bool masterPassCheckCountryNameToggle = Convert.ToBoolean(_configuration.GetValue<string>("MasterPassCheckCountryNameToggle") ?? "false");
                    if (masterPassCheckCountryNameToggle &&
                        payment.BillingAddress != null &&
                        payment.BillingAddress.Country != null)
                    {
                        payment.BillingAddress.Country.CountryCode = string.Empty;
                    }
                    response.BillingAddress = ConvertCslBillingAddressToMOBAddress(payment.BillingAddress, MOBFormofPayment.Masterpass);
                    response.ContactPhoneNumber =
                        GetKeyValueFromAddressCharacteristicCollection(payment.BillingAddress, "PHONE");
                    response.ContactEmailAddress =
                        GetKeyValueFromAddressCharacteristicCollection(payment.BillingAddress, "EMAIL");
                    response.MasterpassType = new MasterpassType
                    {
                        DefaultIndicator = payment.Type.DefaultIndicator,
                        Description = payment.Type.Description,
                        Key = payment.Type.Key,
                        Val = payment.Type.Value
                    };
                    //MOBILE-1683/MOBILE-1669/MOBILE-1671: PA,PB,PCU- Masterpass : Richa
                    MOBVormetricKeys vormetricKeys = await AssignPersistentTokenToCC(payment.AccountNumberToken, payment.PersistentToken, string.Empty, response.Code, walletSessionResponse.Response.TransactionID, "DeserialiseAndBuildMOBMasterpassSessionDetailsFOP", 0, "");
                    if (!string.IsNullOrEmpty(vormetricKeys.PersistentToken))
                    {
                        response.PersistentToken = vormetricKeys.PersistentToken;
                    }
                    if (!string.IsNullOrEmpty(vormetricKeys.CardType) && string.IsNullOrEmpty(response.Code))
                    {
                        response.Code = vormetricKeys.CardType;
                    }
                }
                else
                {
                    if (walletSessionResponse != null)
                    {
                        ThrowExceptionAsPerErrorResponse(walletSessionResponse.Response.Error);
                    }
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            return response;
        }

        private async Task<MOBCreditCard> GetCreditCardWithToken(MOBCreditCard creditCard, Session session, MOBRequest mobRequest)
        {
            if (!string.IsNullOrEmpty(creditCard.EncryptedCardNumber))
            {
                #region
                string ccDataVaultToken = string.Empty;
                bool isPersistAssigned = await AssignDataVaultAndPersistTokenToCC(session.SessionId, session.Token, creditCard);

                if (!isPersistAssigned)
                {
                    var tupleRes = await _profileCreditCard.GenerateCCTokenWithDataVault(creditCard, session.SessionId, session.Token, mobRequest.Application, mobRequest.DeviceId, ccDataVaultToken);
                    ccDataVaultToken = tupleRes.ccDataVaultToken;
                    if (tupleRes.Item1)
                    {
                        creditCard.AccountNumberToken = ccDataVaultToken;
                        if (creditCard.UnencryptedCardNumber != null)
                        {
                            creditCard.UnencryptedCardNumber = "XXXXXXXXXXXX" + creditCard.UnencryptedCardNumber.Substring((creditCard.UnencryptedCardNumber.Length - 4), 4);
                        }
                    }
                }

                #endregion
                if (creditCard != null)
                {
                    if (creditCard.CardTypeDescription != null)
                    {
                        switch (creditCard.CardTypeDescription.ToLower())
                        {
                            case "diners club":
                                creditCard.CardTypeDescription = "Diners Club Card";
                                break;
                            case "uatp (formerly air travel card)":
                                creditCard.CardTypeDescription = "UATP";
                                break;
                        }
                    }
                }
            }

            if (_configuration.GetValue<string>("MakeItEmptyCreditCardInformationNeededMessage") != null && Convert.ToBoolean(_configuration.GetValue<string>("MakeItEmptyCreditCardInformationNeededMessage").ToString()))
            {
                creditCard.Message = string.Empty;
            }

            creditCard.IsPrimary = true;
            creditCard.IsValidForTPIPurchase = _paymentUtility.IsValidFOPForTPIpayment(creditCard.CardType);
            return creditCard;
        }

        private United.Service.Presentation.PaymentModel.Wallet BuildWalletObject(MOBMasterpass masterpass, string cartId)
        {
            United.Service.Presentation.PaymentModel.Wallet wallet = new Service.Presentation.PaymentModel.Wallet
            {
                CartID = cartId,
                CheckoutURL = masterpass.CheckoutResourceURL,
                OathToken = masterpass.OauthToken,
                OathVerifier = masterpass.Oauth_verifier,
                PointOfSale = _configuration.GetValue<string>("AcquireMasterPassToken-PointOfSale"),
                WalletType = "MPS",
                SessionID = masterpass.CslSessionId,
                Version = "1.0"
            };
            return wallet;
        }

        private async Task<MOBAddress> GetBillingAddressWithValidStateCode(MOBPersistFormofPaymentRequest request, Session session)
        {
            if (request != null && request.FormofPaymentDetails != null && request.FormofPaymentDetails.BillingAddress != null && request.FormofPaymentDetails.BillingAddress != null && request.FormofPaymentDetails.BillingAddress.State != null && !string.IsNullOrEmpty(request.FormofPaymentDetails.BillingAddress.State.Code.Trim()))
            {
                string stateCode = string.Empty;
                var response = await GetAndValidateStateCode_CFOP(request, session, stateCode);
                if (response.returnVal)
                {
                    request.FormofPaymentDetails.BillingAddress.State.Code = response.stateCode;
                }

                return request.FormofPaymentDetails.BillingAddress;
            }

            return request?.FormofPaymentDetails?.BillingAddress;
        }

        private async Task<(bool returnVal, string stateCode)> GetAndValidateStateCode_CFOP(MOBPersistFormofPaymentRequest request, Session session, string stateCode)
        {
            bool validStateCode = false;
            #region
            //http://unitedservicesstage.ual.com/5.0/referencedata/StatesFilter?State=tex&CountryCode=US&Language=en-US
            // string url = string.Format("{0}/StatesFilter?State={1}&CountryCode={2}&Language={3}", _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLReferencedata"], request.FormofPaymentDetails.BillingAddress.State.Code, request.FormofPaymentDetails.BillingAddress.Country.Code, request.LanguageCode);
            string path = string.Format("/StatesFilter?State={0}&CountryCode={1}&Language={2}", request.FormofPaymentDetails.BillingAddress.State.Code, request.FormofPaymentDetails.BillingAddress.Country.Code, request.LanguageCode);

            var response = await _referencedataService.GetDataGetAsync<List<StateProvince>>(path, session.Token, session.SessionId);
            if (response != null)
            {
                if (response != null && response.Count == 1 && !string.IsNullOrEmpty(response[0].StateProvinceCode))
                {
                    stateCode = response[0].StateProvinceCode;
                    validStateCode = true;
                }
                else
                {
                    string exceptionMessage = _configuration.GetValue<string>("UnableToGetAndValidateStateCode").ToString();
                    throw new MOBUnitedException(exceptionMessage);
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToGetAndValidateStateCode");
                if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting").ToString()))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  GetCommonUsedDataList()";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion
            return (validStateCode, stateCode);
        }

        private string GetKeyValueFromAddressCharacteristicCollection(Address address, string key)
        {
            string phoneNumber = string.Empty;
            if (address != null &&
                address.Characteristic != null &&
                address.Characteristic.Count > 0)
            {
                foreach (var characterstic in address.Characteristic)
                {
                    if (characterstic != null)
                    {
                        if (((characterstic.Description ?? "").ToUpper() == key || (characterstic.Code ?? "").ToUpper() == key) &&
                             key == "PHONE" &&
                             !string.IsNullOrEmpty(characterstic.Value))
                        {
                            phoneNumber = characterstic.Value.Replace("-", "");
                        }
                        else if ((characterstic.Description ?? "").ToUpper() == key || (characterstic.Code ?? "").ToUpper() == key)
                        {
                            phoneNumber = characterstic.Value;
                        }
                    }
                }
            }
            return phoneNumber;
        }

        private MOBAddress ConvertCslBillingAddressToMOBAddress(United.Service.Presentation.CommonModel.Address cslAddress, MOBFormofPayment fop)
        {
            MOBAddress mobAddress = new MOBAddress();
            if (cslAddress != null)
            {
                foreach (string addressLines in cslAddress.AddressLines)
                {
                    if (string.IsNullOrEmpty(mobAddress.Line1))
                    {
                        mobAddress.Line1 = addressLines;
                    }
                    else if (string.IsNullOrEmpty(mobAddress.Line2))
                    {
                        mobAddress.Line2 = addressLines;

                    }
                    else if (string.IsNullOrEmpty(mobAddress.Line3))
                    {
                        mobAddress.Line3 = addressLines;

                    }
                    else
                    {
                        mobAddress.Line3 = mobAddress.Line3 + addressLines;
                    }
                }

                mobAddress.Country = new MOBCountry
                {
                    Code = cslAddress.Country.CountryCode,
                    Name = cslAddress.Country.Name
                };
                mobAddress.City = cslAddress.City;
                mobAddress.PostalCode = cslAddress.PostalCode;
                mobAddress.State = new State();

                if (fop == MOBFormofPayment.Masterpass)
                {
                    mobAddress.State.Code = !string.IsNullOrEmpty(cslAddress.StateProvince.Name) ? cslAddress.StateProvince.Name.ToUpper().Replace("US-", "") : (cslAddress.StateProvince.Name ?? "");
                    mobAddress.State.Name = (cslAddress.StateProvince.Name ?? "").ToUpper();
                }
                else
                {
                    mobAddress.State.Code = cslAddress.StateProvince.ShortName;
                    mobAddress.State.Name = cslAddress.StateProvince.StateProvinceCode;
                }

                bool payPalBillingCountryNotUsaMessageToggle = Convert.ToBoolean(_configuration.GetValue<string>("PayPalBillingCountryNotUSAMessageToggle") ?? "false");
                if (payPalBillingCountryNotUsaMessageToggle && !_paymentUtility.IsUSACountryAddress(mobAddress.Country))
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("PayPalBillingCountryNotUSAMessage"));
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            return mobAddress;
        }

        private async Task<bool> AssignDataVaultAndPersistTokenToCC(string sessionId, string sessionToken, MOBCreditCard creditCard)
        {
            bool isPersistAssigned = _configuration.GetValue<bool>("VormetricTokenMigration");
            if (isPersistAssigned)
            {
                if (await _profileCreditCard.GenerateCCTokenWithDataVault(creditCard, sessionId, sessionToken, _application, _deviceId))
                {
                    if (!string.IsNullOrEmpty(creditCard.PersistentToken))
                    {
                        if (creditCard.UnencryptedCardNumber != null && creditCard.UnencryptedCardNumber.Length > 4)
                        {
                            creditCard.UnencryptedCardNumber = "XXXXXXXXXXXX" + creditCard.UnencryptedCardNumber.Substring((creditCard.UnencryptedCardNumber.Length - 4), 4);
                        }
                        else
                        {
                            creditCard.UnencryptedCardNumber = "XXXXXXXXXXXX";
                        }
                    }
                    else if (String.IsNullOrEmpty(creditCard.AccountNumberToken) && !string.IsNullOrEmpty(sessionToken) && !string.IsNullOrEmpty(_headers.ContextValues.SessionId))
                    {
                        MOBVormetricKeys vormetricKeys = await GetPersistentTokenUsingAccountNumberToken(creditCard.AccountNumberToken, _headers.ContextValues.SessionId, sessionToken);
                        creditCard.PersistentToken = vormetricKeys.PersistentToken;
                        creditCard.SecurityCodeToken = vormetricKeys.SecurityCodeToken;
                        creditCard.CardType = vormetricKeys.CardType;
                    }
                    else
                    {
                        LogNoPersistentTokenInCSLResponseForVormetricPayment(sessionId);
                    }
                }
            }
            return isPersistAssigned;
        }

        private void LogNoPersistentTokenInCSLResponseForVormetricPayment(string sessionId, string Message = "Unable to retieve PersistentToken")
        {
            _logger.LogError("LogNoPersistentTokenInCSLResponseForVormetricPayment-PERSISTENTTOKENNOTFOUND Error {exception} {sessionid}", Message, sessionId);
            //No need to block the flow as we are calling DataVault for Persistent Token during the final payment
        }

        private async Task<MOBVormetricKeys> GetPersistentTokenUsingAccountNumberToken(string accountNumberToke, string sessionId, string token)
        {
            //string url = string.Format("{0}/{1}/RSA", _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLDataVault"), accountNumberToke);
            string url = string.Format("{0}/RSA", accountNumberToke);
            var cslResponse = await MakeHTTPCallAndLogIt(
                                        sessionId,
                                        _headers.ContextValues.DeviceId,
                                        "GetPersistentTokenUsingAccountNumberToken",
                                        _application,
                                        token,
                                        url,
                                        string.Empty,
                                        true,
                                        false);

            return GetPersistentTokenFromCSLDatavaultResponse(cslResponse, sessionId);
        }

        private async Task<string> MakeHTTPCallAndLogIt(
                                            string sessionId, string deviceId, string action, MOBApplication application, string token, string url, string jsonRequest, bool isGetCall, bool isXMLRequest = false)
        {
            string jsonResponse = string.Empty;
            if (isGetCall)
            {
                jsonResponse = await _dataVaultService.GetPersistentToken(token, jsonRequest, url, sessionId);
            }
            else
            {
                jsonResponse = await _dataVaultService.PersistentToken(token, jsonRequest, url, sessionId);
            }

            return jsonResponse;
        }

        private MOBVormetricKeys GetPersistentTokenFromCSLDatavaultResponse(string jsonResponseFromCSL, string sessionID)
        {
            MOBVormetricKeys vormetricKeys = new MOBVormetricKeys();
            if (!string.IsNullOrEmpty(jsonResponseFromCSL))
            {
                CslDataVaultResponse response = JsonConvert.DeserializeObject<CslDataVaultResponse>(jsonResponseFromCSL);
                if (response != null && response.Responses != null && response.Responses[0].Error == null && response.Responses[0].Message != null && response.Responses[0].Message.Count > 0 && response.Responses[0].Message[0].Code.Trim() == "0")
                {
                    var creditCard = response.Items[0] as Service.Presentation.PaymentModel.CreditCard;
                    vormetricKeys.PersistentToken = creditCard.PersistentToken;
                    vormetricKeys.SecurityCodeToken = creditCard.SecurityCodeToken;
                    vormetricKeys.CardType = creditCard.Code;
                }
                else
                {
                    if (response.Responses[0].Error != null && response.Responses[0].Error.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Responses[0].Error)
                        {
                            errorMessage = errorMessage + " " + error.Text;
                        }

                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToInsertCreditCardToProfileErrorMessage");
                        if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && _configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL InsertTravelerCreditCard(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }

            return vormetricKeys;
        }

        private async Task<MOBVormetricKeys> AssignPersistentTokenToCC(string accountNumberToken, string persistentToken, string securityCodeToken, string cardType, string sessionId, string action, int appId, string deviceID)
        {
            MOBVormetricKeys vormetricKeys = new MOBVormetricKeys();
            if (_configuration.GetValue<bool>("VormetricTokenMigration"))
            {
                if ((string.IsNullOrEmpty(persistentToken) || string.IsNullOrEmpty(cardType)) && !string.IsNullOrEmpty(accountNumberToken) && !string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(accountNumberToken))
                {
                    vormetricKeys = await GetPersistentTokenUsingAccountNumberToken(accountNumberToken, sessionId, accountNumberToken);
                    persistentToken = vormetricKeys.PersistentToken;
                }

                if (!string.IsNullOrEmpty(persistentToken))
                {
                    vormetricKeys.PersistentToken = persistentToken;
                    vormetricKeys.SecurityCodeToken = securityCodeToken;
                    vormetricKeys.CardType = cardType;
                }
                else
                {
                    LogNoPersistentTokenInCSLResponseForVormetricPayment(sessionId);
                }
            }
            else
            {
                persistentToken = string.Empty;
            }

            return vormetricKeys;
        }

        public async Task<MOBRegisterOfferResponse> RegisterOffers(MOBRegisterOfferRequest request, Session session)
        {
            MOBRegisterOfferResponse response = new MOBRegisterOfferResponse
            {
                SessionId = request.SessionId,
                TransactionId = request.TransactionId,
                Flow = request.Flow
            };

            if (request.MerchandizingOfferDetails.Count() == 0)
            {
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                return response;
            }

            var productOffer = new GetOffers();
            productOffer = await _sessionHelperService.GetSession<GetOffers>(session.SessionId, productOffer.ObjectName, new List<string> { request.SessionId, productOffer.ObjectName });

            var productVendorOffer = new GetVendorOffers();
            productVendorOffer = await _sessionHelperService.GetSession<GetVendorOffers>(session.SessionId, productVendorOffer.ObjectName, new List<string> { session.SessionId, productVendorOffer.ObjectName });

            var pomOffer = await _sessionHelperService.GetSession<DynamicOfferDetailResponse>(session.SessionId, new DynamicOfferDetailResponse().GetType().FullName, new List<string> { session.SessionId, new DynamicOfferDetailResponse().GetType().FullName });

            var reservationDetail = new ReservationDetail();
            reservationDetail = await _sessionHelperService.GetSession<ReservationDetail>(request.SessionId, reservationDetail.GetType().FullName, new List<string> { request.SessionId, reservationDetail.GetType().FullName });

            var isCompleteFarelockPurchase = _configuration.GetValue<bool>("EnableFareLockPurchaseViewRes") && request.MerchandizingOfferDetails.Any(o => (o.ProductCode != null && o.ProductCode.Equals("FLK_VIEWRES", StringComparison.OrdinalIgnoreCase)));

            var flightReservationResponse = isCompleteFarelockPurchase ? await _shopping.RegisterFareLockReservation(request, reservationDetail, session)
                                                                       : await _shopping.RegisterOffers(request, productOffer, productVendorOffer, pomOffer, reservationDetail, session);

            var flow = GetFlow(request.Flow, flightReservationResponse?.ShoppingCart?.Items);
            response.Flow = flow;
            response.ShoppingCart.Flow = flow;

            response.ShoppingCart.Products = await _productInfoHelper.ConfirmationPageProductInfo(flightReservationResponse, false, request.Application, null, "VIEWRES", request.SessionId);
            response.ShoppingCart.TermsAndConditions = await _productInfoHelper.GetProductBasedTermAndConditions(request.SessionId, flightReservationResponse, false);
            response.ShoppingCart.PaymentTarget = GetPaymentTargetForRegisterFop(flightReservationResponse, isCompleteFarelockPurchase);
            double price = GetGrandTotalPriceForShoppingCart(isCompleteFarelockPurchase, flightReservationResponse);
            response.ShoppingCart.TotalPrice = String.Format("{0:0.00}", price);
            response.ShoppingCart.DisplayTotalPrice = Decimal.Parse(price.ToString()).ToString("c");
            response.ShoppingCart.CartId = flightReservationResponse.CartId.ToString();
            response.ShoppingCart.DisplayMessage = await _paymentUtility.GetPaymentMessagesForWLPNRViewRes(flightReservationResponse, reservationDetail.Detail.FlightSegments, request.Flow);
            response.ShoppingCart.TripInfoForUplift = _paymentUtility.GetUpliftTripInfo(reservationDetail, response.ShoppingCart.TotalPrice, response.ShoppingCart.Products);
            response.PkDispenserPublicKey = await _pKDispenserPublicKey.GetCachedOrNewpkDispenserPublicKey(request.Application.Id, request.Application.Version.Major, request.DeviceId, session.SessionId, session.Token, session?.CatalogItems);
            response.ShoppingCart.PromoCodeDetails = _configuration.GetValue<bool>("EnablePromoCodeForAncillaryOffersManageRes") && _paymentUtility.IsEligibleAncillaryProductForPromoCode(request.SessionId, flightReservationResponse, false) ? new MOBPromoCodeDetails() : null;

            if (isCompleteFarelockPurchase)
            {
                response.ShoppingCart.Trips = await GetTrips(reservationDetail.Detail.FlightSegments);
                response.ShoppingCart.SCTravelers = GetTravelerCSLDetails(flightReservationResponse.Reservation, response.ShoppingCart.Trips, session.SessionId);
                //Prices & Taxes
                if (!flightReservationResponse.DisplayCart.IsNullOrEmpty() && !flightReservationResponse.DisplayCart.DisplayPrices.IsNullOrEmpty() && flightReservationResponse.DisplayCart.DisplayPrices.Any())
                {
                    var productPrice = new ProductPrice(_configuration, _sessionHelperService);
                    // Journey Type will be "OW", "RT", "MD"
                    var JourneyType = UtilityHelper.GetJourneyTypeDescription(flightReservationResponse.Reservation);
                    bool isCorporateFare = _paymentUtility.IsCorporateTraveler(flightReservationResponse.Reservation.Characteristic);
                    response.ShoppingCart.Prices = await productPrice.GetPrices(flightReservationResponse.DisplayCart.DisplayPrices, false, null, false, JourneyType, isCompleteFarelockPurchase, isCorporateFare);
                    response.ShoppingCart.Taxes = ConfigUtility.GetTaxAndFeesAfterPriceChange(flightReservationResponse.DisplayCart.DisplayPrices, false);
                    response.ShoppingCart.Captions = GetFareLockCaptions(flightReservationResponse.Reservation, JourneyType, isCorporateFare);

                    response.ShoppingCart.ELFLimitations = await (new ELFRitMetaShopMessages(_configuration, _dynamoDBService, _legalDocumentsForTitlesService, _headers)).GetELFLimitationsViewRes(response.ShoppingCart.Trips, reservationDetail.Detail.FlightSegments, request.Application.Id);
                }
            }
            if (response.ShoppingCart.Captions.IsNullOrEmpty())
            {
                response.ShoppingCart.Captions = await _productInfoHelper.GetCaptions("PaymentPage_ViewRes_Captions");
            }
            if (ConfigUtility.IsManageResETCEnabled(request.Application.Id, request.Application.Version.Major))
            {
                response.ShoppingCart.Prices = _paymentUtility.AddGrandTotalIfNotExist(response.ShoppingCart.Prices, Convert.ToDouble(response.ShoppingCart.TotalPrice), response.ShoppingCart.Flow);
            }

            await _sessionHelperService.SaveSession<MOBShoppingCart>(response.ShoppingCart, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, response.ShoppingCart.ObjectName }, response.ShoppingCart.ObjectName).ConfigureAwait(false);

            return response;
        }

        private double GetGrandTotalPriceForShoppingCart(bool isCompleteFarelockPurchase, FlightReservationResponse flightReservationResponse)
        {
            return isCompleteFarelockPurchase ? UtilityHelper.GetGrandTotalPriceFareLockShoppingCart(flightReservationResponse)
                                             : flightReservationResponse.ShoppingCart.Items.Where(x => x.Product.FirstOrDefault().Code != "RES").Select(x => x.Product?.FirstOrDefault().Price?.Totals?.FirstOrDefault()?.Amount ?? 0).ToList().Sum();
        }

        private List<MOBCPTraveler> GetTravelerCSLDetails(United.Service.Presentation.ReservationModel.Reservation reservation, List<MOBSHOPTrip> shoptrips, string sessionId)
        {
            if (reservation == null || shoptrips == null || !reservation.Travelers.Any() || !reservation.FlightSegments.Any() || !shoptrips.Any())
            {
                return null;
            }

            var trips = GetTripBase(shoptrips);
            if (trips == null || !trips.Any())
            {
                return null;
            }

            int paxIndex = 0;
            string departDate = trips.FirstOrDefault(k => !k.IsNullOrEmpty()).DepartDate;

            List<MOBCPTraveler> travelerCSL = new List<MOBCPTraveler>();
            reservation.Travelers.ForEach(p =>
            {
                if (p != null && p.Person != null)
                {

                    MOBCPTraveler mobCPTraveler = MapTravelerModel.MapCslPersonToMOBCPTravel(p, paxIndex, reservation.FlightSegments, trips);
                    mobCPTraveler.Phones = _paymentUtility.GetMobCpPhones(p.Person.Contact);
                    mobCPTraveler.FirstName = _paymentUtility.FirstLetterToUpperCase(mobCPTraveler.FirstName);
                    mobCPTraveler.LastName = _paymentUtility.FirstLetterToUpperCase(mobCPTraveler.LastName);
                    if (!mobCPTraveler.BirthDate.IsNullOrEmpty() && !departDate.IsNullOrEmpty())
                    {
                        mobCPTraveler.PTCDescription = GeneralHelper.GetPaxDescriptionByDOB(mobCPTraveler.BirthDate.ToString(), departDate);
                    }
                    travelerCSL.Add(mobCPTraveler);
                    paxIndex++;
                }
            });

            return travelerCSL;
        }

        private bool HasEPUSubproduct(BundleExtension bundle)
        {
            return bundle?.Products?.Any(p => "EPU".Equals(p.Code)) ?? false;
        }

        private List<MOBSHOPTripBase> GetTripBase(List<MOBSHOPTrip> ShopTrip)
        {
            if (ShopTrip == null || !ShopTrip.Any())
            {
                return null;
            }

            List<MOBSHOPTripBase> trips = new List<MOBSHOPTripBase>();
            foreach (var trip in ShopTrip)
            {
                var tripBase = new MOBSHOPTripBase();
                tripBase = trip;
                tripBase.DepartDate = trip.DepartDate;
                trips.Add(tripBase);
            }
            return trips;
        }

        private List<MOBItem> GetFareLockCaptions(United.Service.Presentation.ReservationModel.Reservation reservation, string journeyType, bool isCorporateFareLock)
        {
            if (reservation.IsNullOrEmpty() || journeyType.IsNullOrEmpty())
            {
                return null;
            }

            var title = GetPaymentTitleCaptionFareLock(reservation, journeyType);
            var tripType = _paymentUtility.GetSegmentDescriptionPageSubTitle(reservation);

            List<MOBItem> captions = new List<MOBItem>();
            if (!title.IsNullOrEmpty() && !tripType.IsNullOrEmpty())
            {
                captions.Add(_paymentUtility.GetFareLockViewResPaymentCaptions("PaymentPage_Title", title));
                captions.Add(_paymentUtility.GetFareLockViewResPaymentCaptions("PaymentPage_SubTitle", tripType));
                captions.Add(_paymentUtility.GetFareLockViewResPaymentCaptions("PaymentPage_Book24Hr_Policy", "Book without worry"));
                captions.Add(_paymentUtility.GetFareLockViewResPaymentCaptions("PaymentPage_ProductCode", "FLK_ViewRes"));
            }

            // To find if PNR is corporate travel
            if (isCorporateFareLock)
            {
                var priceText = _configuration.GetValue<string>("CorporateRateText");
                if (!priceText.IsNullOrEmpty())
                {
                    captions.Add(_paymentUtility.GetFareLockViewResPaymentCaptions("Corporate_PriceBreakDownText", priceText));
                }
            }

            return captions;
        }

        private string GetPaymentTitleCaptionFareLock(Service.Presentation.ReservationModel.Reservation reservation, string journeyType)
        {
            if (reservation.IsNullOrEmpty() || journeyType.IsNullOrEmpty())
            {
                return string.Empty;
            }

            string fareLockHeaderDate = _paymentUtility.GetFareLockTitleViewResPaymentRTI(reservation, journeyType);
            string fareLockHeaderTitle = GetFareLockTitle(reservation, journeyType);

            if (!fareLockHeaderDate.IsNullOrEmpty() && !fareLockHeaderTitle.IsNullOrEmpty())
            {
                string title = journeyType.Equals("Multicity") ? "Starting" + " " + fareLockHeaderDate + " " + fareLockHeaderTitle
                                                        : fareLockHeaderDate + " " + fareLockHeaderTitle;
                if (journeyType.Equals("Multicity") && title.Length > 26)
                {
                    int endIndex = title.IndexOf("/") + 1;
                    title = title.Substring(0, endIndex) + "....";
                }
                return title;
            }
            return string.Empty;
        }

        private string GetFareLockTitle(United.Service.Presentation.ReservationModel.Reservation reservation, string journeyType)
        {
            if (reservation.IsNullOrEmpty() || journeyType.IsNullOrEmpty())
            {
                return string.Empty;
            }

            string tripBuild = string.Empty;
            var arrivalAirportCode = reservation.FlightSegments.LastOrDefault(k => !k.IsNullOrEmpty() && !k.FlightSegment.IsNullOrEmpty()).FlightSegment;
            reservation.FlightSegments.ForEach(p =>
            {
                if (p != null && !p.FlightSegment.IsNullOrEmpty() && !p.FlightSegment.DepartureAirport.IsNullOrEmpty() && !p.FlightSegment.ArrivalAirport.IsNullOrEmpty())
                {
                    if (journeyType.Equals("Multicity"))
                    {
                        tripBuild = tripBuild.IsNullOrEmpty() ? p.FlightSegment.DepartureAirport.IATACode + " to " + p.FlightSegment.ArrivalAirport.IATACode
                                                              : tripBuild + "/" + p.FlightSegment.DepartureAirport.IATACode + " to " + p.FlightSegment.ArrivalAirport.IATACode;
                    }
                    else if (journeyType.Equals("Roundtrip"))
                    {
                        if (tripBuild.IsNullOrEmpty())
                        {
                            tripBuild = p.FlightSegment.DepartureAirport.IATACode + " - " + p.FlightSegment.ArrivalAirport.IATACode;
                        }
                    }
                    else
                    {
                        if (tripBuild.IsNullOrEmpty())
                        {
                            string arrivalAirport = !arrivalAirportCode.IsNullOrEmpty() && !arrivalAirportCode.ArrivalAirport.IsNullOrEmpty() ? arrivalAirportCode.ArrivalAirport.IATACode : p.FlightSegment.ArrivalAirport.IATACode;
                            tripBuild = p.FlightSegment.DepartureAirport.IATACode + " - " + arrivalAirport;
                        }
                    }
                }
            });
            return tripBuild;
        }

        private string GetFlow(string flow, Collection<ShoppingCartItem> items)
        {
            if (!_configuration.GetValue<bool>("EnableTravelOptionsBundleInViewRes"))
            {
                return flow;
            }

            if (!FlowType.VIEWRES.ToString().Equals(flow) || items.IsNullOrEmpty())
            {
                return flow;
            }

            var hasSeatBundleProduct = items.Any(i => i?.Product?.Any(p => p?.SubProducts?.Any(sp => "BE".Equals(sp?.GroupCode) && HasEPUSubproduct(sp?.Extension?.Bundle)) ?? false) ?? false);
            return hasSeatBundleProduct ? FlowType.VIEWRES_BUNDLES_SEATMAP.ToString() : flow;
        }
        private async Task<List<MOBSHOPTrip>> GetTrips(Collection<ReservationFlightSegment> FlightSegments)
        {
            if (FlightSegments == null || !FlightSegments.Any())
            {
                return null;
            }

            List<MOBSHOPTrip> trips = new List<MOBSHOPTrip>();
            var currenttripnum = string.Empty;
            foreach (var cslsegment in FlightSegments)
            {
                if (!cslsegment.TripNumber.IsNullOrEmpty() && !currenttripnum.Equals(cslsegment.TripNumber))
                {
                    currenttripnum = cslsegment.TripNumber;
                    var tripAllSegments = FlightSegments.Where(p => p != null && p.TripNumber != null && p.TripNumber == cslsegment.TripNumber).ToList();
                    var tripsegment = await _shopping.ConvertPNRSegmentToShopTripWithTripNumber(tripAllSegments);
                    trips.Add(tripsegment);
                }
            }
            return trips;
        }

        public string GetPaymentTargetForRegisterFop(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, bool isCompleteFarelockPurchase = false)
        {
            if (string.IsNullOrEmpty(_configuration.GetValue<string>("EnablePCUSelectedSeatPurchaseViewRes")))
            {
                return string.Empty;
            }

            if (isCompleteFarelockPurchase)
            {
                return "RES";
            }

            if (flightReservationResponse == null || flightReservationResponse.ShoppingCart == null || flightReservationResponse.ShoppingCart.Items == null)
            {
                return string.Empty;
            }

            var productCodes = flightReservationResponse.ShoppingCart.Items.Where(x => x.Product.FirstOrDefault().Code != "RES").Select(x => x.Product.FirstOrDefault().Code).ToList();
            if (productCodes == null || !productCodes.Any())
            {
                return string.Empty;
            }

            return string.Join(",", productCodes.Distinct());
        }
        public async Task<(List<FormofPaymentOption> response, bool isDefault)> GetEligibleFormofPayments(MOBRequest request, Session session, MOBShoppingCart shoppingCart, string cartId, string flow, bool isDefault, MOBSHOPReservation reservation = null, bool IsMilesFOPEnabled = false, SeatChangeState persistedState = null)
        {
            List<FormofPaymentOption> response = new List<FormofPaymentOption>();
            SeatChangeState state = persistedState;

            if (state == null && flow == FlowType.VIEWRES.ToString() && ConfigUtility.IsManageResETCEnabled(request.Application.Id, request.Application.Version.Major))
            {
                state = await _sessionHelperService.GetSession<SeatChangeState>(_headers.ContextValues.SessionId, new SeatChangeState().ObjectName, new List<string> { _headers.ContextValues.SessionId, new SeatChangeState().ObjectName }).ConfigureAwait(false);
            }
            if (_configuration.GetValue<bool>("GetFoPOptionsAlongRegisterOffers") && shoppingCart.Products != null && shoppingCart.Products.Count > 0)
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
                    Products = ConfigUtility.GetProductsForEligibleFopRequest(shoppingCart, state)
                };

                var tupleResponse = await EligibleFormOfPayments(eligiblefopRequest, session, isDefault, IsMilesFOPEnabled);
                response = tupleResponse.response;
                isDefault = tupleResponse.isDefault;

                if ((reservation?.IsMetaSearch ?? false) && _configuration.GetValue<bool>("CreditCardFOPOnly_MetaSearch"))
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

                var upliftFop = _paymentUtility.UpliftAsFormOfPayment(reservation, shoppingCart);

                if (upliftFop != null && response != null)
                {
                    response.Add(upliftFop);
                }

                // Added as part of Money + Miles changes: For MM user have to pay money only using CC - MOBILE-14735;  MOBILE-14833; MOBILE-14925 // MM is only for Booking
                if (ConfigUtility.IncludeMoneyPlusMiles(request.Application.Id, request.Application.Version.Major) && flow == FlowType.BOOKING.ToString())
                {
                    if (response.Exists(x => x.Category == "MILES") && !_configuration.GetValue<bool>("DisableMMFixForSemiLogin_MOBILE17070")
                       && !reservation.IsSignedInWithMP)
                    {
                        response = response.Where(x => x.Category != "MILES").ToList();
                    }
                    if (shoppingCart.FormofPaymentDetails?.MoneyPlusMilesCredit?.SelectedMoneyPlusMiles != null) // selected miles will not be empty only after Applied Miles
                    {
                        response = response.Where(x => x.Category == "CC").ToList();
                    }
                }

                response = TravelBankEFOPFilter(request, shoppingCart?.FormofPaymentDetails?.TravelBankDetails, flow, response);

                if (ConfigUtility.IsETCchangesEnabled(request.Application.Id, request.Application.Version.Major) && flow == FlowType.BOOKING.ToString())
                {
                    response = ConfigUtility.BuildEligibleFormofPaymentsResponse(response, shoppingCart, session, request, reservation?.IsMetaSearch ?? false);
                }
                else if ((flow == FlowType.VIEWRES.ToString() || flow == FlowType.VIEWRES_SEATMAP.ToString())&& ConfigUtility.IsManageResETCEnabled(request.Application.Id, request.Application.Version.Major))
                {
                    response = ConfigUtility.BuildEligibleFormofPaymentsResponse(response, shoppingCart, request);
                }
                if (!_configuration.GetValue<bool>("DisableTravelCreditBannerCheckForUpdateContact"))
                {
                    var isMandatory = shoppingCart?.FormofPaymentDetails?.CreditCard != null ? shoppingCart.FormofPaymentDetails.CreditCard.IsMandatory : false;
                    if ((shoppingCart.FormofPaymentDetails?.MoneyPlusMilesCredit?.SelectedMoneyPlusMiles != null ||
                        shoppingCart.FormofPaymentDetails?.TravelBankDetails?.TBApplied > 0 ||
                        shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.ApplePay.ToString() ||
                        shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.PayPal.ToString() ||
                        shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.PayPalCredit.ToString() ||
                        shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.Masterpass.ToString() ||
                        shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.Uplift.ToString() ||
                        isMandatory ||
                        (!_configuration.GetValue<bool>("DisableTravelCreditBannerCheckForUpdateContact") && !response.Exists(x => x.Category == "TRAVELCREDIT")))
                        && !string.IsNullOrEmpty(shoppingCart?.FormofPaymentDetails?.TravelCreditDetails?.TravelCreditSummary)
                        && flow?.ToLower() == FlowType.BOOKING.ToString()?.ToLower())
                    {
                        shoppingCart.FormofPaymentDetails.TravelCreditDetails.TravelCreditSummary = string.Empty;
                    }
                    //Apple Pay to remove              
                    if (isMandatory)
                    {
                        response = response.Where(x => x.Category.ToUpper() != "APP").ToList();
                    }
                }

                await _sessionHelperService.SaveSession<List<FormofPaymentOption>>(response, session.SessionId, new List<string> { session.SessionId, response.GetType().FullName }, response.GetType().FullName).ConfigureAwait(false);//change session

                //MOBILE-32683 - FOP have to be saved with object name
                if (!_configuration.GetValue<bool>("DisableFOPObjectName"))
                    await _sessionHelperService.SaveSession<List<FormofPaymentOption>>(response, session.SessionId, new List<string> { session.SessionId, new FormofPaymentOption().ObjectName }, new FormofPaymentOption().ObjectName).ConfigureAwait(false);//change session

                if (!_configuration.GetValue<bool>("DisableFixForGuestFlowNullEFOP_ACM2362") && flow == FlowType.RESHOP.ToString())
                {
                    //await _sessionHelperService.SaveSession<List<FormofPaymentOption>>(response, session.SessionId, new List<string> { session.SessionId, new FormofPaymentOption().ObjectName }, new FormofPaymentOption().ObjectName).ConfigureAwait(false); // Have to save session with ObjectName through out the application After removing below uncomment this line of code
                    await _sessionHelperService.SaveSession<List<FormofPaymentOption>>(response, session.SessionId, new List<string> { session.SessionId, "System.Collections.Generic.List`1[United.Definition.FormofPaymentOption]" }, "System.Collections.Generic.List`1[United.Definition.FormofPaymentOption]").ConfigureAwait(false); // temp fix till next MSC release. 22Y                                        
                }

                await _shoppingUtility.UpdateFSRMoneyPlusMilesOptionsBasedOnEFOP(request, session, shoppingCart, reservation, response).ConfigureAwait(false);
            }

            return (response, isDefault);
        }

       

        private List<FormofPaymentOption> TravelBankEFOPFilter(MOBRequest request, Mobile.Model.Shopping.FormofPayment.MOBFOPTravelBankDetails travelBankDet, string flow, List<FormofPaymentOption> response)
        {
            if (travelBankDet?.TBApplied > 0 && _paymentUtility.IncludeTravelBankFOP(request.Application.Id, request.Application.Version.Major) && flow == FlowType.BOOKING.ToString())
            {
                response = response.Where(x => x.Category == "CC").ToList();
            }

            return response;
        }
    }
}
