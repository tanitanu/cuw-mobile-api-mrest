using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.ManageRes;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Common.HelperSeatEngine;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.SeatEngine;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.Shopping;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.SeatMap;
using United.Mobile.Model.SeatMapEngine;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Booking;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Model.Shopping.Misc;
using United.Mobile.Model.Shopping.Pcu;
using United.Service.Presentation.CommonModel;
using United.Service.Presentation.ProductModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common.Extensions;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Utility.Helper;
using Characteristic = United.Service.Presentation.CommonModel.Characteristic;
using FlowType = United.Utility.Enum.FlowType;
using MOBErrorCodes = United.Utility.Enum.MOBErrorCodes;
using MOBPromoCodeDetails = United.Mobile.Model.Shopping.FormofPayment.MOBPromoCodeDetails;
using MOBSeatMap = United.Mobile.Model.Common.MOBSeatMap;
using RegisterOfferRequest = United.Services.FlightShopping.Common.FlightReservation.RegisterOfferRequest;
using Session = United.Mobile.Model.Common.Session;
using TripSegment = United.Mobile.Model.Shopping.TripSegment;

namespace United.Mobile.SeatMap.Domain
{
    public class SeatMapBusiness : ISeatMapBusiness
    {
        private readonly ICacheLog<SeatMapBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICachingService _cachingService;
        private readonly IDPService _dPService;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly ISeatMapAvailabilityService _seatMapAvailabilityService;
        private readonly IProductInfoHelper _productInfoHelper;
        private readonly ISeatEnginePostService _seatEnginePostService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IManageReservation _manageReservation;
        private readonly IFormsOfPayment _formsOfPayment;
        private readonly ISeatMapCSL30 _seatMapCSL30;
        private readonly IRegisterCFOP _registerCFOP;
        private readonly IProductOffers _shopping;
        private readonly IPKDispenserService _pKDispenserService;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly ISeatEngine _seatEngine;
        private readonly IPaymentUtility _paymentUtility;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly IPaymentService _paymentService;
        private readonly IHeaders _headers;

        public SeatMapBusiness(ICacheLog<SeatMapBusiness> logger
            , IConfiguration configuration
            , ICachingService cachingService
            , IDPService dPService
            , ISessionHelperService sessionHelperService
            , ISeatMapAvailabilityService seatMapAvailabilityService
            , IProductInfoHelper productInfoHelper
            , ISeatEnginePostService seatEnginePostService
            , IShoppingCartService shoppingCartService
            , IManageReservation manageReservation
            , ISeatMapCSL30 seatMapCSL30
            , IFormsOfPayment formsOfPayment
            , IRegisterCFOP regiserCFOP
            , IProductOffers shopping
            , IPKDispenserService pKDispenserService
            , IShoppingSessionHelper shoppingSessionHelper
            , ISeatEngine seatEngine
            , IPaymentUtility paymentUtility
            , IDynamoDBService dynamoDBService
            , IPaymentService paymentService
            , IHeaders headers
            )
        {
            _logger = logger;
            _configuration = configuration;
            _cachingService = cachingService;
            _dPService = dPService;
            _sessionHelperService = sessionHelperService;
            _seatMapAvailabilityService = seatMapAvailabilityService;
            _productInfoHelper = productInfoHelper;
            _seatEnginePostService = seatEnginePostService;
            _shoppingCartService = shoppingCartService;
            _manageReservation = manageReservation;
            _seatMapCSL30 = seatMapCSL30;
            _formsOfPayment = formsOfPayment;
            _registerCFOP = regiserCFOP;
            _shopping = shopping;
            _pKDispenserService = pKDispenserService;
            _shoppingSessionHelper = shoppingSessionHelper;
            _seatEngine = seatEngine;
            _paymentUtility = paymentUtility;
            _dynamoDBService = dynamoDBService;
            _paymentService = paymentService;
            _headers = headers;

            ConfigUtility.UtilityInitialize(_configuration);
        }

        public async Task<MOBSeatMapResponse> GetSeatMap(int applicationid, string appVersion, string accessCode, string transactionId, string carrierCode, string flightNumber, string flightDate, string departureAirportCode, string arrivalAirportCode, string languageCode, string scheduledDepartureAirportCode = null, string scheduledArrivalAirportCode = null)
        {
            MOBSeatMapRequest request = new MOBSeatMapRequest();
            MOBSeatMapResponse response = new MOBSeatMapResponse();

            //Diverted flight changes
            if (!string.IsNullOrEmpty(scheduledDepartureAirportCode))
                departureAirportCode = scheduledDepartureAirportCode;
            if (!string.IsNullOrEmpty(scheduledArrivalAirportCode))
                arrivalAirportCode = scheduledArrivalAirportCode;

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
            request.MarketingCarrierCode = "UA";
            request.Application.Version = new MOBVersion { Major = appVersion };
            request.DeviceId = !String.IsNullOrEmpty(request.TransactionId) ? request.TransactionId.Split('|')[0] : request.TransactionId;

            if (carrierCode.ToUpper().ToString() == _configuration.GetValue<string>("CarrierCodeTOM")?.ToUpper().ToString())
            {
                request.OperatingCarrierCode = "UA";
            }
            else
            {
                request.OperatingCarrierCode = carrierCode;
            }

            _logger.LogInformation("FlightStatusGetSeatMap:GetSeatMapRequest {Request} {ApplicationId} {ApplicationVersion} {DeviceId} and {TransactionId}", JsonConvert.SerializeObject(request), request.Application.Id, request.Application.Version.Major, request.DeviceId, request.TransactionId);


            response.TransactionId = request.TransactionId;
            response.LanguageCode = request.LanguageCode;
            response.FlightNumber = flightNumber;
            response.FlightDate = flightDate;
            response.MarketingCarrierCode = request.MarketingCarrierCode;
            response.OperatingCarrierCode = request.OperatingCarrierCode;
            string fltDate = DateTime.ParseExact(flightDate, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("MM/dd/yyyy");

            #region

            int flightNum = 0;
            Int32.TryParse(flightNumber, out flightNum);

            try
            {
                response.SeatMap = GetCSL30SeatMap(request, FlowType.FLIGHTSTATUS_SEATMAP).Result;

                #region check version For DAA
                ChangeDAAMapForUpperVersion(appVersion, request, response);

                _logger.LogInformation("FlightStatusGetSeatMap-GetSeatMapResponse {Response} {ApplicationId} {ApplicationVersion} {DeviceId} and {TransactionId}", response, applicationid, request.Application.Version.Major, request.DeviceId, transactionId);
                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError("GetSeatMap Error {exceptionstack} and {transactionId}", JsonConvert.SerializeObject(ex), transactionId);
                _logger.LogError("GetSeatMap Error {exception} and {transactionId}", ex.Message, transactionId);

                if (ex.InnerException?.Message != null)
                {
                    response.Exception = new MOBException("", ex.InnerException.Message);
                }
                else
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("GenericExceptionMessage"));
                }
            }

            #endregion
            return await System.Threading.Tasks.Task.FromResult(response);
        }

        public async Task<MOBRegisterSeatsResponse> RegisterSeats(MOBRegisterSeatsRequest request)
        {
            Session session = null;
            MOBRegisterSeatsResponse response = new MOBRegisterSeatsResponse();

            _logger.LogInformation("RegisterSeats_CFOP {Request} {SessionId} {ApplicationId} {ApplicationVersion} {DeviceId}", JsonConvert.SerializeObject(request), request.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId);


            if (!string.IsNullOrEmpty(request.SessionId))
            {
                session = await _shoppingSessionHelper.GetValidateSession(request.SessionId, false, true);
                session.Flow = request.Flow;
            }
            else
            {
                _logger.LogInformation("RegisterSeats_CFOP {CFOP - Session Null} {Request} {SessionId} {ApplicationId} {ApplicationVersion} {DeviceId}", JsonConvert.SerializeObject(request), request.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId);
                if (ConfigUtility.VersionCheck_NullSession_AfterAppUpgradation(request))
                    throw new MOBUnitedException(((int)MOBErrorCodes.ViewResCFOP_NullSession_AfterAppUpgradation).ToString(), _configuration.GetValue<string>("CFOPViewRes_ReloadAppWhileUpdateMessage"));
                else
                    throw new MOBUnitedException(((int)MOBErrorCodes.ViewResCFOPSessionExpire).ToString(), _configuration.GetValue<string>("CFOPViewRes_ReloadAppWhileUpdateMessage"));
            }

            response.SessionId = request.SessionId;
            response.Flow = request.Flow;
            response.Request = request;
            response.PriceBreakDownTitle = "Price details";

            response.Request.Application.Version.Build = null;
            response.Request.Application.Version.DisplayText = null;

            if (GeneralHelper.ValidateAccessCode(request.AccessCode))
            {
                SeatChangeState state = new SeatChangeState();
                state = _sessionHelperService.GetSession<SeatChangeState>(request.SessionId, state.ObjectName, new List<string> { request.SessionId, state.ObjectName }).Result;
                if (state == null)
                {
                    throw new MOBUnitedException("Unable to retrieve information needed for seat change.");
                }

                response.SelectedTrips = state.Trips;

                AddSeats(ref state, request.Origin, request.Destination, request.FlightNumber, request.FlightDate, request.PaxIndex, request.SeatAssignment);


                try
                {
                    #region 
                    List<MOBSeatMap> MOBSeatMapList = _sessionHelperService.GetSession<List<MOBSeatMap>>(request.SessionId, ObjectNames.MOBSeatMapListFullName, new List<string> { request.SessionId, ObjectNames.MOBSeatMapListFullName }).Result;
                    if (MOBSeatMapList != null && MOBSeatMapList.Count > 0 && state != null && state.Seats != null)
                    {
                        _logger.LogInformation("RegisterSeats_CFOP {SeatMPCache2ValidatPriceManuplate} {SessionId} {ApplicationId} {ApplicationVersion} and {Transactionid}", MOBSeatMapList, request.SessionId, request.Application.Id, request.Application.Version.DisplayText, request.TransactionId);

                        List<Seat> unavailableSeats = new List<Seat>();
                        bool seatChangeToggle = _configuration.GetValue<bool>("SeatUpgradeForUnavailableSeatCheck");

                        if (_configuration.GetValue<bool>("FixSeatNotFoundManageResObjRefExceptionInRegisterSeatsAction"))
                        {
                            #region Fix for RegisterSeats_CFOP action for SeatNotFound ManageRes ObjRefException(JIRA MOBILE - 6584) - by Shashank
                            foreach (Seat seat in state.Seats)
                            {
                                if (!string.IsNullOrEmpty(seat.SeatAssignment) && seat.Origin == request.Origin && seat.Destination == request.Destination && seat.FlightNumber == request.FlightNumber && Convert.ToDateTime(seat.DepartureDate).ToString("MMM., dd, yyyy") == Convert.ToDateTime(request.FlightDate).ToString("MMM., dd, yyyy"))
                                {
                                    List<MOBSeatB> mobSeatB = (from list in MOBSeatMapList
                                                               from cabin in list.Cabins
                                                               from row in cabin.Rows
                                                               from se in row.Seats
                                                               where se.Number.ToUpper().Trim() == seat.SeatAssignment.ToUpper().Trim()
                                                               select se).ToList();
                                    if (mobSeatB[0].ServicesAndFees != null && mobSeatB[0].ServicesAndFees.Count > 0)
                                    {
                                        seat.Price = Convert.ToDecimal(mobSeatB[0].ServicesAndFees[0].TotalFee);
                                        seat.PcuOfferOptionId = mobSeatB[0].PcuOfferOptionId;
                                        if (_configuration.GetValue<bool>("EnableMilesAsPayment"))
                                        {
                                            if (_configuration.GetValue<bool>("EnableMilesAsPayment") && seat.Price > 0)
                                            {
                                                seat.Miles = _configuration.GetValue<int>("milesFOP");
                                                seat.DisplayMiles = UtilityHelper.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false);
                                            }

                                        }
                                    }
                                    if (seatChangeToggle && mobSeatB[0].seatvalue == "X")
                                    {
                                        unavailableSeats.Add(seat);
                                    }
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            foreach (Seat seat in state.Seats)
                            {
                                if (seat.SeatAssignment != string.Empty && seat.Origin == request.Origin && seat.Destination == request.Destination)
                                {
                                    List<MOBSeatB> mobSeatB = (from list in MOBSeatMapList
                                                               from cabin in list.Cabins
                                                               from row in cabin.Rows
                                                               from se in row.Seats
                                                               where se.Number.ToUpper().Trim() == seat.SeatAssignment.ToUpper().Trim()
                                                               select se).ToList();
                                    if (mobSeatB[0].ServicesAndFees != null && mobSeatB[0].ServicesAndFees.Count > 0)
                                    {
                                        seat.Price = Convert.ToDecimal(mobSeatB[0].ServicesAndFees[0].TotalFee);
                                        seat.PcuOfferOptionId = mobSeatB[0].PcuOfferOptionId;
                                        if (_configuration.GetValue<bool>("EnableMilesAsPayment"))
                                        {
                                            if (_configuration.GetValue<bool>("EnableMilesAsPayment") && seat.Price > 0)
                                            {
                                                seat.Miles = _configuration.GetValue<int>("milesFOP");
                                                seat.DisplayMiles = UtilityHelper.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false);
                                            }

                                        }
                                    }
                                    if (seatChangeToggle && mobSeatB[0].seatvalue == "X")
                                    {
                                        unavailableSeats.Add(seat);
                                    }
                                }
                            }
                        }

                        if (seatChangeToggle && unavailableSeats.Count > 0)
                        {
                            foreach (var seat in unavailableSeats)
                            {
                                state.Seats.Remove(seat);
                            }
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    if (_configuration.GetValue<string>("SeatNotFoundAtCompleteSeatsSelection") != null)
                    {
                        string exMessage = ex.Message != null ? ex.Message : "";
                        throw new Exception(_configuration.GetValue<string>("SeatNotFoundAtCompleteSeatsSelection") + " - " + exMessage);
                    }
                }

                bool isCSLSeatMap = ConfigUtility.IsEnableXmlToCslSeatMapMigration(request.Application.Id, request.Application.Version.Major);

                bool isCSL30SeatMap = _configuration.GetValue<bool>("EnableCSL30ManageResSelectSeatMap");
                if (isCSL30SeatMap)
                {
                    state.Seats = ApplyTravelerCompanionRulesCSl30(request.SessionId, state.Seats, state.RecordLocator, state.PNRCreationDate, state.BookingTravelerInfo, request.LanguageCode, request.Application.Id, request.Application.Version.Major, state.Segments, request.DeviceId);
                }
                else
                {
                    state.Seats = ApplyTravelerCompanionRulesCSL(request.SessionId, state.Seats, state.RecordLocator, state.PNRCreationDate, state.BookingTravelerInfo, request.LanguageCode, request.Application.Id, request.Application.Version.Major, state.Segments, request.DeviceId);
                }

                response.Seats = state.Seats;

                foreach (MOBBKTraveler traveler in state.BookingTravelerInfo)
                {
                    IEnumerable<Seat> seatList = from s in state.Seats
                                                 where s.TravelerSharesIndex == traveler.SHARESPosition
                                                 select s;
                    if (seatList.Count() > 0)
                    {
                        List<Seat> travelerSeats = new List<Seat>();
                        travelerSeats = seatList.ToList();
                        travelerSeats = travelerSeats.OrderBy(s => s.Key).ToList();
                        traveler.Seats = travelerSeats;
                    }
                }
                response.BookingTravlerInfo = state.BookingTravelerInfo;

                state.SeatPrices = ComputeSeatPrices(state.Seats);

                response.SeatPrices = state.SeatPrices;
                if (ShouldCreateNewCart(request))
                {
                    request.CartId = await _shopping.CreateCart(request, session);

                    MOBShoppingCart persistShoppingCart = new MOBShoppingCart();
                    await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, session.SessionId, new List<string> { session.SessionId, persistShoppingCart.ObjectName }, persistShoppingCart.ObjectName);
                }

                response.ShoppingCart = await RegisterSeats(request, request.CartId, session.Token, state, request.Flow);

                if (response.ShoppingCart == null && request.Flow == FlowType.VIEWRES_BUNDLES_SEATMAP.ToString())
                {
                    MOBShoppingCart persistShoppingCart = new MOBShoppingCart();
                    response.ShoppingCart = _sessionHelperService.GetSession<MOBShoppingCart>(session.SessionId, persistShoppingCart.ObjectName, new List<string> { session.SessionId, persistShoppingCart.ObjectName }).Result;
                }

                decimal.TryParse(response?.ShoppingCart?.TotalPrice, out decimal totalPrice);

                if (response.SeatPrices == null && totalPrice == 0)
                {
                    if (response.ShoppingCart != null)
                    {
                        CheckOutRequest checkOutRequest = new CheckOutRequest()
                        {
                            SessionId = request.SessionId,
                            CartId = request.CartId,
                            Application = request.Application,
                            AccessCode = request.AccessCode,
                            LanguageCode = request.LanguageCode,
                            FormofPaymentDetails = new MOBFormofPaymentDetails
                            {
                                FormOfPaymentType = null
                            },
                            Flow = request.Flow
                        };

                        CheckOutResponse checkOutResponse = new CheckOutResponse();
                        checkOutResponse = await _registerCFOP.RegisterFormsOfPayments_CFOP(checkOutRequest);
                        if (checkOutResponse.ShoppingCart != null && checkOutResponse.ShoppingCart.AlertMessages != null && checkOutResponse.ShoppingCart.AlertMessages.Any())
                        {
                            response.seatAssignMessages = new List<string> { _configuration.GetValue<string>("SeatsUnAssignedMessage").Trim() };
                        }
                        else
                        {
                            response.seatAssignMessages = checkOutResponse.SeatAssignMessages;
                        }
                    }

                    MOBPNRByRecordLocatorRequest pnrByRecordLocatorRequest = new MOBPNRByRecordLocatorRequest
                    {
                        Application = request.Application,
                        AccessCode = request.AccessCode,
                        SessionId = request.SessionId,
                        LanguageCode = request.LanguageCode,
                        RecordLocator = state.RecordLocator,
                        LastName = state.LastName,
                        DeviceId = request.DeviceId,
                        TransactionId = request.TransactionId,
                        MileagePlusNumber = request.MileagePlusNumber,
                        Flow = FlowType.VIEWRES.ToString()
                    };

                    var reservationResponse = await _manageReservation.GetPNRByRecordLocatorCommonMethod(pnrByRecordLocatorRequest);
                    response.PNR = reservationResponse.PNR;
                    response.TripInsuranceInfo = reservationResponse.TripInsuranceInfo;
                    response.PremierAccess = reservationResponse.PremierAccess;
                    response.ShowPremierAccess = response.PremierAccess != null;
                    response.DotBaggageInformation = reservationResponse.DotBaggageInformation;
                    response.Ancillary = reservationResponse.Ancillary;
                    response.Flow = reservationResponse.Flow;
                    if (!_configuration.GetValue<bool>("DisableRegisterSeatResponseFlowMissingInFreeSeatFlow") && string.IsNullOrEmpty(response.Flow))
                    {
                        response.Flow = request.Flow;
                    }
                    response.PNR.IsEnableEditTraveler = !reservationResponse.PNR.isgroup;
                    response.ShowSeatChange = ShowSeatChangeButton(response.PNR);
                    response.RewardPrograms = reservationResponse.RewardPrograms;
                    response.SpecialNeeds = reservationResponse.SpecialNeeds;
                }
                else
                {
                    if (response.ShoppingCart != null)
                    {
                        if (_configuration.GetValue<bool>("GetFoPOptionsAlongRegisterOffers"))
                        {
                            bool isDefault = false;
                            if (ConfigUtility.IsManageResETCEnabled(request.Application.Id, request.Application.Version.Major))
                            {
                                var tupleRes = await _formsOfPayment.GetEligibleFormofPayments(request, session, response.ShoppingCart, request.CartId, request.Flow, isDefault, null);
                                response.EligibleFormofPayments = tupleRes.response;
                            }
                            else
                            {
                                FOPEligibilityRequest eligiblefopRequest = new FOPEligibilityRequest()
                                {
                                    TransactionId = request.TransactionId,
                                    DeviceId = request.DeviceId,
                                    AccessCode = request.AccessCode,
                                    LanguageCode = request.LanguageCode,
                                    Application = request.Application,
                                    CartId = request.CartId,
                                    SessionId = request.SessionId,
                                    Flow = request.Flow,
                                    Products = ConfigUtility.GetProductsForEligibleFopRequest(response.ShoppingCart, state)
                                };
                                var tupleResponse = await _formsOfPayment.EligibleFormOfPayments(eligiblefopRequest, session, isDefault, _paymentUtility.GetIsMilesFOPEnabled(response.ShoppingCart));
                                response.EligibleFormofPayments = tupleResponse.response;
                                var upliftFop = _paymentUtility.UpliftAsFormOfPayment(null, response.ShoppingCart);
                                if (upliftFop != null && response.EligibleFormofPayments != null)
                                {
                                    response.EligibleFormofPayments.Add(upliftFop);
                                }
                            }
                            response.IsDefaultPaymentOption = isDefault;
                            await _sessionHelperService.SaveSession<List<FormofPaymentOption>>(response.EligibleFormofPayments, request.SessionId, new List<string> { request.SessionId, new FormofPaymentOption().GetType().FullName }, new FormofPaymentOption().GetType().FullName);
                            
                            //MOBILE-32683 - FOP have to be saved with object name
                            if (!_configuration.GetValue<bool>("DisableFOPObjectName"))
                                await _sessionHelperService.SaveSession<List<FormofPaymentOption>>(response.EligibleFormofPayments, request.SessionId, new List<string> { request.SessionId, new FormofPaymentOption().ObjectName }, new FormofPaymentOption().ObjectName).ConfigureAwait(false);//change session

                        }
                    }
                    response.Flow = request.Flow == FlowType.VIEWRES_BUNDLES_SEATMAP.ToString() ? FlowType.VIEWRES.ToString() : request.Flow;
                    var pKDispenserPublicKey = new PKDispenserPublicKey(_configuration, _cachingService, _dPService, _pKDispenserService, _headers);

                    response.PkDispenserPublicKey = await pKDispenserPublicKey.GetCachedOrNewpkDispenserPublicKey(request.Application.Id, request.Application.Version.Major, request.DeviceId, request.SessionId, session.Token, session?.CatalogItems);
                }

                await _sessionHelperService.SaveSession<SeatChangeState>(state, state.SessionId, new List<string> { state.SessionId, state.ObjectName }, state.ObjectName);
            }
            else
            {
                throw new MOBUnitedException("The access code you provided is not valid.");
            }

            return response;
        }

        private async Task<MOBSeatMap> GetCSL30SeatMap(MOBSeatMapRequest mRequest, FlowType flowtype)
        {
            var seatmapresponse = new MOBSeatMap { };
            mRequest.FlightDate = DateTime.ParseExact(mRequest.FlightDate, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
            string[] channelInfo = _configuration.GetValue<string>("CSL30MBEChannelInfo").Split('|');
            string authToken = await _dPService.GetAnonymousToken(mRequest.Application.Id, mRequest.DeviceId, _configuration);
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
            seatmapresponse.EnabledVerticalSeatMap = _configuration.GetValue<bool>("EnabledVerticalSeatMap");
            return seatmapresponse;
        }

        private async Task<MOBSeatMap> MapCSL30SeatmapResponse(MOBSeatMapRequest mRequest, Definition.SeatCSL30.SeatMap seatMapResponse, bool returnPolarisLegendforSeatMap = false)
        {
            MOBSeatMap objMOBSeatMap = null;

            bool enable3DSeatMap = _configuration.GetValue<bool>("3DSeatMap") && GeneralHelper.IsApplicationVersionGreaterorEqual(mRequest.Application.Id, mRequest.Application.Version.Major, _configuration.GetValue<string>("iPhone3DSeatMapSupportedVersion"), _configuration.GetValue<string>("Android3DSeatMapSupportedVersion"));

            bool IsPolarisBranding = _configuration.GetValue<bool>("IsPolarisCabinBrandingON");
            bool enableSeatMapMonument = _configuration.GetValue<bool>("SeatMapMonument") && GeneralHelper.IsApplicationVersionGreaterorEqual(mRequest.Application.Id, mRequest.Application.Version.Major, _configuration.GetValue<string>("iPhoneSeatMapMonumentSupportedVersion"), _configuration.GetValue<string>("AndroidSeatMapMonumentSupportedVersion"));

            if (seatMapResponse != null
                && seatMapResponse.Errors.IsNullOrEmpty()
                && !seatMapResponse.Cabins.IsNullOrEmpty()
                && !seatMapResponse.AircraftInfo.IsNullOrEmpty()
                && !seatMapResponse.FlightInfo.IsNullOrEmpty())
            {

                objMOBSeatMap = new MOBSeatMap();
                List<string> cabinBrandingDescriptions = new List<string>();
                int numberOfCabins = (seatMapResponse.Cabins.Count > 3) ? 3 : seatMapResponse.Cabins.Count;

                bool isPreferredZoneEnabled = ConfigUtility.EnablePreferredZone(mRequest.Application.Id, mRequest.Application.Version.Major);
                objMOBSeatMap.SeatMapAvailabe = true;

                if (enable3DSeatMap && !string.IsNullOrEmpty(seatMapResponse.AircraftInfo.Icr))
                {
                    objMOBSeatMap.View3DUrl = Get3DSeatMapUrl(seatMapResponse.AircraftInfo.Icr);
                }

                objMOBSeatMap.FleetType = !string.IsNullOrEmpty(seatMapResponse.AircraftInfo.Icr) ? seatMapResponse.AircraftInfo.Icr : string.Empty;
                objMOBSeatMap.LegId = "";
                int cabinCount = 0;
                int prevRowNumber = 0;
                foreach (United.Definition.SeatCSL30.Cabin cabin in seatMapResponse.Cabins)
                {
                    ++cabinCount;
                    MOBCabin tmpCabin = new MOBCabin();
                    tmpCabin.COS = cabin.CabinBrand;
                    tmpCabin.Configuration = cabin.Layout;
                    cabinBrandingDescriptions.Add(cabin.CabinBrand);


                    MOBRow tmpRow = null;

                    tmpCabin.FrontMonuments = GetMonuments(cabin, 0, 999, enableSeatMapMonument);
                    tmpCabin.BackMonuments = GetMonuments(cabin, 3000, 4000, enableSeatMapMonument);


                    foreach (United.Definition.SeatCSL30.Row row in cabin.Rows)
                    {
                        LoadMonuments(row, prevRowNumber, cabin, ref tmpCabin, enableSeatMapMonument);
                        tmpRow = new MOBRow();
                        tmpRow.Number = row.Number.ToString();
                        tmpRow.Wing = row.Wing;

                        var monumentrow = enableSeatMapMonument ? cabin.MonumentRows.FirstOrDefault(x => x.VerticalGridNumber == row.VerticalGridNumber) : null;

                        for (int i = 1; i <= cabin.ColumnCount; i++)
                        {
                            var seat = row.Seats.FirstOrDefault(x => x.HorizontalGridNumber == i);
                            var monumentseat = (!monumentrow.IsNullOrEmpty()) ? monumentrow.Monuments.FirstOrDefault(x => x.HorizontalGridNumber == i) : null;

                            MOBSeatB tmpSeat = null;
                            if (!seat.IsNullOrEmpty())
                            {
                                tmpSeat = new MOBSeatB();
                                tmpSeat.Exit = seat.IsExit;
                                tmpSeat.Number = seat.Number;
                                tmpSeat.Fee = "";
                                tmpSeat.LimitedRecline = !string.IsNullOrEmpty(seat.ReclineType)
                                    && seat.ReclineType.Equals("LIMITED", StringComparison.OrdinalIgnoreCase);

                                if (!string.IsNullOrEmpty(seat.SeatType)
                                    && !cabin.CabinBrand.Equals("United Economy", StringComparison.OrdinalIgnoreCase))
                                {
                                    tmpSeat.Program = _seatMapCSL30.GetSeatPositionAccessFromCSL30SeatMap(seat.SeatType);
                                }
                                tmpSeat.IsEPlus = !string.IsNullOrEmpty(seat.SeatType)
                                    && seat.SeatType.Equals(SeatType.BLUE.ToString(), StringComparison.OrdinalIgnoreCase);

                                tmpSeat.seatvalue = _seatMapCSL30.GetSeatValueFromCSL30SeatMap(seat, false, false, mRequest.Application, false, false, string.Empty, false);
                                if (enableSeatMapMonument)
                                {
                                    tmpSeat.DoorExit = seat.IsDoorExit;
                                    tmpSeat.MonumentType = seat.ItemType;
                                    tmpSeat.HorizontalSpan = "1";
                                }
                                tmpRow.Seats.Add(tmpSeat);
                            }
                            else
                            {
                                if (!enableSeatMapMonument)
                                {
                                    tmpSeat = new MOBSeatB
                                    {
                                        Number = string.Empty,
                                        Fee = "",
                                        LimitedRecline = false,
                                        seatvalue = "-",
                                        Exit = (!monumentseat.IsNullOrEmpty()) ? monumentseat.IsExit : false,
                                    };
                                    tmpRow.Seats.Add(tmpSeat);
                                }
                                else
                                {
                                    if (!monumentseat.IsNullOrEmpty() && !string.IsNullOrEmpty(monumentseat.ItemType))
                                    {
                                        tmpSeat = new MOBSeatB
                                        {
                                            Number = string.Empty,
                                            Fee = "",
                                            LimitedRecline = false,
                                            seatvalue = "-",
                                            Exit = (!monumentseat.IsNullOrEmpty()) ? monumentseat.IsExit : false,
                                            MonumentType = (!monumentseat.IsNullOrEmpty()) ? monumentseat.ItemType : string.Empty,
                                            HorizontalSpan = (!monumentseat.IsNullOrEmpty()) ? Convert.ToString(monumentseat.HorizontalSpan) : string.Empty,
                                            DoorExit = (!monumentseat.IsNullOrEmpty()) ? monumentseat.IsDoorExit : false
                                        };
                                    }
                                    if (!tmpSeat.IsNullOrEmpty() && !string.IsNullOrEmpty(tmpSeat.MonumentType))
                                        tmpRow.Seats.Add(tmpSeat);

                                    if (!tmpSeat.IsNullOrEmpty() && Convert.ToInt32(tmpSeat.HorizontalSpan) > 1)
                                    {
                                        int hrzlSpan = Convert.ToInt32(tmpSeat.HorizontalSpan);
                                        for (int count = 1; count < hrzlSpan; count++)
                                        {
                                            tmpSeat = new MOBSeatB
                                            {
                                                Number = string.Empty,
                                                Fee = "",
                                                LimitedRecline = false,
                                                seatvalue = "-",
                                                Exit = false,
                                            };
                                            tmpRow.Seats.Add(tmpSeat);
                                        }


                                    }

                                }

                            }

                        }

                        if (row.Number < 1000)
                            tmpCabin.Rows.Add(tmpRow);
                        prevRowNumber = row.VerticalGridNumber;
                    }
                    tmpCabin.Configuration = tmpCabin.Configuration.Replace(" ", "-");
                    if (enableSeatMapMonument)
                    {
                        List<Definition.SeatCSL30.MonumentRow> monumentsAtCabinEnd = cabin.MonumentRows.Where(x => x.VerticalGridNumber > prevRowNumber).ToList();
                        if (!monumentsAtCabinEnd.IsNull() && monumentsAtCabinEnd.Count > 0)
                        {
                            List<MOBRow> monumentsRowsToAdd = GetMonuments(cabin, monumentsAtCabinEnd[0].VerticalGridNumber, monumentsAtCabinEnd[monumentsAtCabinEnd.Count - 1].VerticalGridNumber, enableSeatMapMonument);
                            tmpCabin.Rows.AddRange(monumentsRowsToAdd);
                        }
                    }
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


                objMOBSeatMap.SeatLegendId = await _seatEngine.GetPolarisSeatMapLegendId(seatMapResponse.FlightInfo.DepartureAirport, seatMapResponse.FlightInfo.ArrivalAirport,
                    numberOfCabins, cabinBrandingDescriptions, mRequest.Application.Id, mRequest.Application.Version.Major);

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

        private List<MOBRow> GetMonuments(United.Definition.SeatCSL30.Cabin cabin, int startVal, int endVal, bool enableSeatMapMonument)
        {
            List<MOBRow> monumentRows = new List<MOBRow>();
            if (!enableSeatMapMonument || cabin.IsNullOrEmpty() || cabin.MonumentRows.IsNullOrEmpty()) return monumentRows;
            var selMonumentRows = cabin.MonumentRows.Where(x => x.VerticalGridNumber >= startVal && x.VerticalGridNumber <= endVal);
            foreach (United.Definition.SeatCSL30.MonumentRow monumentRow in selMonumentRows)
            {
                MOBRow tmpRow = null;

                if (monumentRow != null)
                {
                    tmpRow = new MOBRow();
                    tmpRow.Number = string.Empty;
                    tmpRow.Wing = false;

                    for (int i = 1; i <= cabin.ColumnCount; i++)
                    {
                        var monumentseat = (!monumentRow.IsNullOrEmpty()) ? monumentRow.Monuments.FirstOrDefault(x => x.HorizontalGridNumber == i) : null;
                        MOBSeatB tmpMonumentSeat = new MOBSeatB
                        {
                            Number = string.Empty,
                            Fee = "",
                            LimitedRecline = false,
                            seatvalue = "-",
                            Exit = (!monumentseat.IsNullOrEmpty()) ? monumentseat.IsExit : false,
                            MonumentType = (!monumentseat.IsNullOrEmpty()) ? monumentseat.ItemType : string.Empty,
                            HorizontalSpan = (!monumentseat.IsNullOrEmpty()) ? Convert.ToString(monumentseat.HorizontalSpan) : string.Empty
                        };
                        if (!tmpMonumentSeat.IsNullOrEmpty())
                            tmpRow.Seats.Add(tmpMonumentSeat);
                    }
                    monumentRows.Add(tmpRow);


                }
            }
            return monumentRows;
        }

        private void LoadMonuments(United.Definition.SeatCSL30.Row row, int prevRowNumber, United.Definition.SeatCSL30.Cabin cabin, ref MOBCabin mobCabin, bool enableSeatMapMonument)
        {
            int missingCount = 0;
            MOBRow tmpRow = null;
            if (!enableSeatMapMonument || cabin.IsNullOrEmpty() || cabin.MonumentRows.IsNullOrEmpty()) return;
            if (prevRowNumber == 0) prevRowNumber = row.VerticalGridNumber + 1;
            missingCount = row.VerticalGridNumber - prevRowNumber;
            if (missingCount > 1)
            {
                prevRowNumber++;
                for (int missingNumber = prevRowNumber; missingNumber < row.VerticalGridNumber; missingNumber++)
                {
                    var monumentMissingRow = cabin.MonumentRows.FirstOrDefault(x => x.VerticalGridNumber == missingNumber);
                    if (monumentMissingRow != null)
                    {
                        tmpRow = new MOBRow();
                        tmpRow.Number = string.Empty;
                        tmpRow.Wing = false;

                        for (int i = 1; i <= cabin.ColumnCount; i++)
                        {
                            var monumentseat = (!monumentMissingRow.IsNullOrEmpty()) ? monumentMissingRow.Monuments.FirstOrDefault(x => x.HorizontalGridNumber == i) : null;
                            MOBSeatB tmpMonumentSeat = new MOBSeatB
                            {
                                Number = string.Empty,
                                Fee = "",
                                LimitedRecline = false,
                                seatvalue = "-",
                                Exit = (!monumentseat.IsNullOrEmpty()) ? monumentseat.IsExit : false,
                                MonumentType = (!monumentseat.IsNullOrEmpty()) ? monumentseat.ItemType : string.Empty,
                                HorizontalSpan = (!monumentseat.IsNullOrEmpty()) ? Convert.ToString(monumentseat.HorizontalSpan) : string.Empty
                            };
                            tmpRow.Seats.Add(tmpMonumentSeat);
                        }
                        if (row.Number < 1000)
                            mobCabin.Rows.Add(tmpRow);
                    }

                }

            }
        }


        private string Get3DSeatMapUrl(string aircraftIcr)
        {
            string view3DUrl = null;
            List<string> ICRsWith3DSeatMap = null;

            if (_configuration.GetValue<string>("ICRsWith3DSeatMap") != null)
            {
                ICRsWith3DSeatMap = _configuration.GetValue<string>("ICRsWith3DSeatMap").Split(',').ToList();
            }

            if (!ICRsWith3DSeatMap.IsNullOrEmpty()
                && ICRsWith3DSeatMap.Contains(aircraftIcr))
            {
                var baseUrl = _configuration.GetValue<string>("3DSeatMapBaseUrl");

                switch (aircraftIcr)
                {
                    case "B6H":
                        view3DUrl = string.Format(baseUrl, "C6K");
                        break;
                    case "B7I":
                        view3DUrl = string.Format(baseUrl, "B7J");
                        break;
                    case "B8E":
                        view3DUrl = string.Format(baseUrl, "B8G");
                        break;
                    case "B5L":
                    case "E2D":
                    case "D2F":
                    case "C5G":
                    case "B5M":
                        view3DUrl = string.Format(baseUrl, "D2E");
                        break;
                    case "B5K":
                    case "C5D":
                    case "B5J":
                    case "B5P":
                    case "C5F":
                        view3DUrl = string.Format(baseUrl, "B5I");
                        break;
                    case "B5N":
                        view3DUrl = string.Format(baseUrl, "B5G");
                        break;
                    case "B6U":
                    case "C6P":
                    case "C6Q":
                        view3DUrl = string.Format(baseUrl, "B6T");
                        break;
                    case "B6R":
                    case "C6N":
                    case "C6O":
                        view3DUrl = string.Format(baseUrl, "B6S");
                        break;
                    default:
                        view3DUrl = string.Format(baseUrl, aircraftIcr);
                        break;
                }
            }

            return view3DUrl;
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

        private void AddSeats(ref SeatChangeState state, string origin, string destination, string flightNumber, string flightDate, string paxIndex, string seatAssignment)
        {
            string[] paxIndexArray = paxIndex.Split(',');
            string[] seatAssignmentArray = seatAssignment.Split(',');

            if (state.Seats != null)
            {
                for (int i = 0; i < seatAssignmentArray.Length; i++)
                {
                    IEnumerable<Seat> seatList = from s in state.Seats
                                                 where s.Origin == origin.Trim().ToUpper()
                                                 && s.Destination == destination.Trim().ToUpper()
                                                 && s.TravelerSharesIndex == paxIndexArray[i]
                                                 && Convert.ToDateTime(s.DepartureDate).ToString("MMddyyy") == Convert.ToDateTime(flightDate).ToString("MMddyyy")
                                                 && s.FlightNumber == flightNumber
                                                 select s;
                    if (seatList.Count() > 0)
                    {
                        List<Seat> seats = new List<Seat>();
                        seats = seatList.ToList();
                        if (seats.Count() == 1)
                        {
                            Seat aSeat = new Seat();
                            aSeat.Destination = destination;
                            aSeat.Origin = origin;
                            aSeat.FlightNumber = flightNumber;
                            aSeat.DepartureDate = flightDate.ToUpper();
                            aSeat.TravelerSharesIndex = seats[0].TravelerSharesIndex;
                            aSeat.Key = seats[0].Key;
                            aSeat.OldSeatAssignment = seats[0].OldSeatAssignment;
                            aSeat.OldSeatCurrency = seats[0].OldSeatCurrency;
                            aSeat.OldSeatPrice = seats[0].OldSeatPrice;
                            aSeat.OldSeatType = seats[0].OldSeatType;
                            aSeat.OldSeatProgramCode = seats[0].OldSeatProgramCode;

                            string[] assignments = seatAssignmentArray[i].Split('|');
                            if (assignments.Length == 5)
                            {
                                aSeat.SeatAssignment = assignments[0];
                                aSeat.Price = Convert.ToDecimal(assignments[1]);
                                aSeat.PriceAfterTravelerCompanionRules = aSeat.Price;
                                aSeat.Currency = assignments[2];
                                aSeat.ProgramCode = assignments[3];
                                aSeat.SeatType = assignments[4];
                                aSeat.LimitedRecline = (aSeat.ProgramCode == "PSL");
                            }
                            else
                            {
                                aSeat.SeatAssignment = seatAssignmentArray[i];
                            }

                            if (_configuration.GetValue<bool>("FixArgumentOutOfRangeExceptionInRegisterSeatsAction"))
                            {
                                state.Seats[state.Seats.IndexOf(seats[0])] = aSeat;
                            }
                            else
                            {
                                state.Seats[seats[0].Key] = aSeat;
                            }
                        }
                    }
                    else
                    {
                        Seat aSeat = new Seat();
                        aSeat.Destination = destination;
                        aSeat.Origin = origin;
                        aSeat.FlightNumber = flightNumber;
                        aSeat.DepartureDate = flightDate;

                        string[] assignments = seatAssignmentArray[i].Split('|');
                        if (assignments.Length == 5)
                        {
                            aSeat.SeatAssignment = assignments[0];
                            aSeat.Price = Convert.ToDecimal(assignments[1]);
                            aSeat.PriceAfterTravelerCompanionRules = aSeat.Price;
                            aSeat.Currency = assignments[2];
                            aSeat.ProgramCode = assignments[3];
                            aSeat.SeatType = assignments[4];
                            aSeat.LimitedRecline = (aSeat.ProgramCode == "PSL");
                        }
                        else
                        {
                            aSeat.SeatAssignment = seatAssignmentArray[i];
                        }

                        aSeat.TravelerSharesIndex = paxIndexArray[i];
                        aSeat.Key = state.Seats.Count;
                        state.Seats.Add(aSeat);
                    }
                }
            }
            else
            {
                for (int i = 0; i < seatAssignment.Split(',').Length; i++)
                {
                    Seat aSeat = new Seat();
                    aSeat.Destination = destination;
                    aSeat.Origin = origin;
                    aSeat.FlightNumber = flightNumber;
                    aSeat.DepartureDate = flightDate;

                    string[] assignments = seatAssignmentArray[i].Split('|');
                    if (assignments.Length == 5)
                    {
                        aSeat.SeatAssignment = assignments[0];
                        aSeat.Price = Convert.ToDecimal(assignments[1]);
                        aSeat.PriceAfterTravelerCompanionRules = aSeat.Price;
                        aSeat.Currency = assignments[2];
                        aSeat.ProgramCode = assignments[3];
                        aSeat.SeatType = assignments[4];
                        aSeat.LimitedRecline = (aSeat.ProgramCode == "PSL");
                    }
                    else
                    {
                        aSeat.SeatAssignment = seatAssignmentArray[i];
                    }

                    aSeat.TravelerSharesIndex = paxIndexArray[i];
                    aSeat.Key = state.Seats.Count;
                    state.Seats.Add(aSeat);
                }
            }
            if (state.Seats != null)
            {
                foreach (MOBBKTraveler traveler in state.BookingTravelerInfo)
                {
                    IEnumerable<Seat> seatList = from s in state.Seats
                                                 where s.TravelerSharesIndex == traveler.SHARESPosition
                                                 select s;
                    if (seatList.Count() > 0)
                    {
                        List<Seat> travelerSeats = new List<Seat>();
                        travelerSeats = seatList.ToList();
                        travelerSeats = travelerSeats.OrderBy(s => s.Key).ToList();
                        traveler.Seats = travelerSeats;
                    }
                }
            }
        }

        public List<Seat> ApplyTravelerCompanionRulesCSl30(string sessionId, List<Seat> seats, string recordLocator, string pnrCreationDate, List<MOBBKTraveler> travelers, string languageCode, int applicationId, string appVersion, List<TripSegment> tripSegments, string deviceId)
        {
            var persistedCSL30SeatMaps = new List<Model.ShopSeats.SeatMapCSL30>();
            persistedCSL30SeatMaps = _sessionHelperService.GetSession<List<Model.ShopSeats.SeatMapCSL30>>(sessionId, new Model.ShopSeats.SeatMapCSL30().ObjectName, 0, new List<string> { sessionId, new Model.ShopSeats.SeatMapCSL30().ObjectName }).Result;

            if (persistedCSL30SeatMaps == null)
            {
                if (!(seats?.Any(s => !string.IsNullOrEmpty(s?.SeatAssignment) && s?.SeatAssignment != s?.OldSeatAssignment) ?? false))
                {
                    return seats;
                }

                throw new System.Exception(_configuration.GetValue<string>("SeatNotFoundAtCompleteSeatsSelection"));
            }

            if (!persistedCSL30SeatMaps.IsNullOrEmpty() && seats != null && seats.Count > 0 && travelers != null && travelers.Count > 0)
            {
                foreach (Model.ShopSeats.SeatMapCSL30 pCSL30SeatMap in persistedCSL30SeatMaps)
                {
                    foreach (Model.Shopping.Misc.Seat seat in seats)
                    {
                        if (!seat.IsNullOrEmpty() && seat.FlightNumber == pCSL30SeatMap.FlightNumber.ToString() && seat.Origin == pCSL30SeatMap.DepartureCode && seat.Destination == pCSL30SeatMap.ArrivalCode)
                        {
                            var seatDetail = pCSL30SeatMap.Seat.FirstOrDefault(x => !x.IsNullOrEmpty() && x.Number == seat.SeatAssignment && x.IsAvailable);
                            if (!seatDetail.IsNullOrEmpty() && string.IsNullOrEmpty(seat.PcuOfferOptionId))
                            {
                                seat.PriceAfterTravelerCompanionRules = seat.Price > 0 && seat.Price > seat.OldSeatPrice ? seatDetail.Pricing.FirstOrDefault(x => !x.IsNullOrEmpty() && x.TravelerIndex == seat.TravelerSharesIndex).TotalPrice : 0;
                                seat.SeatFeature = seatDetail.DisplaySeatCategory;
                            }
                            seat.SeatType = !seatDetail.IsNullOrEmpty() && !seatDetail.DisplaySeatCategory.IsNullOrEmpty() ? seatDetail.DisplaySeatCategory : seat.SeatType;
                        }
                    }
                }
            }

            _logger.LogInformation("ApplyTravelerCompanionRulesCSL30 {Response} {SessionId} {ApplicationId} {ApplicationVersion} {DeviceId}", seats, sessionId, applicationId, appVersion, deviceId);

            return seats;
        }

        public List<Seat> ApplyTravelerCompanionRulesCSL(string sessionId, List<Seat> seats, string recordLocator, string pnrCreationDate, List<MOBBKTraveler> travelers, string languageCode, int applicationId, string appVersion, List<TripSegment> tripSegments, string deviceId)
        {
            #region request
            United.Service.Presentation.FlightRequestModel.SeatMap request = new Service.Presentation.FlightRequestModel.SeatMap();

            if (travelers != null && seats != null && travelers.Count > 0 && seats.Count > 0)
            {
                request.Requestor = new United.Service.Presentation.CommonModel.Requestor();
                string channelId = _configuration.GetValue<string>("AssignChannelID");
                request.Requestor.ChannelID = channelId.IsNullOrEmpty() ? "MOBILE" : channelId;
                request.LanguageCode = languageCode;
                request.ConfirmationID = recordLocator;
                request.ReservationCreateDate = pnrCreationDate;
                request.Rules = BuildSeatMapRequestWithSegmentsCSL(tripSegments);

                request.Travelers = new Collection<ProductTraveler>();
                foreach (var traveler in travelers)
                {
                    ProductTraveler travelerInformation = new ProductTraveler();
                    travelerInformation.GivenName = traveler.Person.GivenName;
                    travelerInformation.Surname = traveler.Person.Surname;
                    travelerInformation.TravelerNameIndex = traveler.SHARESPosition;

                    List<Model.Shopping.Misc.Seat> paxSeats = new List<Model.Shopping.Misc.Seat>();
                    foreach (Model.Shopping.Misc.Seat seat in seats)
                    {
                        if (seat.TravelerSharesIndex == traveler.SHARESPosition)
                        {
                            paxSeats.Add(seat);
                        }
                    }

                    travelerInformation.Seats = new Collection<SeatDetail>();
                    foreach (var paxSeat in paxSeats)
                    {
                        SeatDetail Seat = new SeatDetail();
                        Seat.DepartureAirport = new Service.Presentation.CommonModel.AirportModel.Airport();
                        Seat.DepartureAirport.IATACode = paxSeat.Origin;
                        Seat.ArrivalAirport = new Service.Presentation.CommonModel.AirportModel.Airport();
                        Seat.ArrivalAirport.IATACode = paxSeat.Destination;
                        Seat.FlightNumber = paxSeat.FlightNumber;
                        Seat.Seat = new United.Service.Presentation.CommonModel.AircraftModel.Seat();

                        if (paxSeat.SeatAssignment != null && paxSeat.SeatAssignment.Trim() != string.Empty)
                        {
                            Seat.Seat.Identifier = paxSeat.SeatAssignment;

                        }
                        else if (paxSeat.OldSeatAssignment != null && paxSeat.OldSeatAssignment.Trim() != string.Empty)
                        {
                            Seat.Seat.Identifier = paxSeat.OldSeatAssignment;
                        }
                        else
                        {
                            Seat.Seat = null;
                        }
                        Seat.DepartureDate = Convert.ToDateTime(paxSeat.DepartureDate).ToShortDateString();
                        travelerInformation.Seats.Add(Seat);
                    }

                    request.Travelers.Add(travelerInformation);
                }


                string jsonRequest = JsonConvert.SerializeObject(request);

                #endregion request
                Session session = new Session();
                session = _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).Result;


                var url = ConfigUtility.EnableSSA(applicationId, appVersion) ? string.Format("{0}GetFinalTravelerSeatPricesWithAllFares", _configuration.GetValue<string>("CSLService-ViewReservationChangeseats"))
                                                                       : string.Format("{0}GetFinalTravelerSeatPrices", _configuration.GetValue<string>("CSLService-ViewReservationChangeseats"));


                _logger.LogInformation("ApplyTravelerCompanionRulesCSL {SessionId} {ApplicationId} {ApplicationVersion} {DeviceId} and {Url}", sessionId, applicationId, appVersion, deviceId, url);
                _logger.LogInformation("ApplyTravelerCompanionRulesCSL {SessionId} {ApplicationId} {ApplicationVersion} {DeviceId} and {CSL Request}", sessionId, applicationId, appVersion, deviceId, jsonRequest);
                _logger.LogInformation("ApplyTravelerCompanionRulesCSL {SessionId} {ApplicationId} {ApplicationVersion} {DeviceId} and {CSL Token}", sessionId, applicationId, appVersion, deviceId, session.Token);

                var jsonResponse = _seatEnginePostService.SeatEnginePostNew(sessionId, url, "application/xml;", session.Token, jsonRequest).Result;

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    _logger.LogInformation("ApplyTravelerCompanionRulesCSL {SessionId} {ApplicationId} {ApplicationVersion} {DeviceId} and {CSL Response}", sessionId, applicationId, appVersion, deviceId, jsonResponse);

                    var response = JsonConvert.DeserializeObject<United.Service.Presentation.ProductModel.FlightSeatMapDetail>(jsonResponse);

                    if (response != null && response.Travelers != null && response.Travelers.Any() && seats != null && seats.Any())
                    {
                        foreach (var traveler in response.Travelers)
                        {
                            if (traveler.Seats != null && traveler.Seats.Count > 0)
                            {
                                foreach (var seatInfo in traveler.Seats)
                                {
                                    foreach (Model.Shopping.Misc.Seat seat in seats)
                                    {
                                        if (seat.SeatAssignment != null && seat.SeatAssignment.Equals(seatInfo.Seat.Identifier) && seat.Origin.Equals(seatInfo.DepartureAirport.IATACode) && seat.Destination.Equals(seatInfo.ArrivalAirport.IATACode))
                                        {
                                            if (seatInfo.Seat != null && seatInfo.Seat.Price != null && seatInfo.Seat.Price.Totals != null && seatInfo.Seat.Price.Totals.Count > 0)
                                            {
                                                seat.PriceAfterTravelerCompanionRules = Convert.ToDecimal(seatInfo.Seat.Price.Totals[0].Amount);
                                                seat.SeatFeature = seatInfo.Seat.Price.PromotionCode;
                                            }
                                            seat.SeatType = seatInfo.Seat.SeatType.ToString();
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return seats;
        }

        private Collection<SeatRuleParameter> BuildSeatMapRequestWithSegmentsCSL(List<TripSegment> segments)
        {
            if (segments == null)
                return null;

            var rules = new Collection<SeatRuleParameter>();
            foreach (var segment in segments)
            {
                var seatRule = new SeatRuleParameter();
                seatRule.Flight = new FlightProfile();
                seatRule.Flight.FlightNumber = segment.FlightNumber;
                seatRule.Flight.OperatingCarrierCode = segment.OperatingCarrier;
                seatRule.ProductCode = segment.ProductCode;
                seatRule.FareBasisCode = segment.FareBasisCode;
                seatRule.Segment = segment.Departure.Code + segment.Arrival.Code;
                seatRule.Flight.DepartureAirport = segment.Departure.Code;
                seatRule.Flight.ArrivalAirport = segment.Arrival.Code;
                rules.Add(seatRule);
            }

            return rules;
        }

        private List<MOBSeatPrice> ComputeSeatPrices(List<Seat> seats)
        {
            List<MOBSeatPrice> seatPrices = null;

            bool allSeatsFree = true;
            List<MOBSeatPrice> tempSeatPrices = new List<MOBSeatPrice>();

            if (seats != null)
            {
                foreach (Model.Shopping.Misc.Seat seat in seats)
                {
                    if (!IsExistingSeat(seat.OldSeatAssignment, seat.SeatAssignment) && seat.Price > 0 && seat.Price > seat.OldSeatPrice)
                    {
                        if (seat.PriceAfterTravelerCompanionRules > 0)
                        {
                            allSeatsFree = false;
                        }

                        MOBSeatPrice seatPrice = BuildSeatPrice(seat);
                        if (!seatPrice.IsNullOrEmpty())
                            tempSeatPrices.Add(seatPrice);
                    }
                }
            }

            if (!allSeatsFree && tempSeatPrices.Count > 0)
            {
                seatPrices = new List<MOBSeatPrice>();
                string origin = "";
                string destination = "";

                foreach (MOBSeatPrice seatPrice in tempSeatPrices)
                {
                    if (seatPrice.Origin.Equals(origin) && seatPrice.Destination.Equals(destination))
                    {
                        var item = seatPrices.Find(s => s.Origin == seatPrice.Origin
                                                     && s.Destination == seatPrice.Destination
                                                     && s.SeatMessage == seatPrice.SeatMessage);

                        if (item != null)
                        {
                            var ci = TopHelper.GetCultureInfo(seatPrice.CurrencyCode ?? "USD");
                            item.NumberOftravelers = item.NumberOftravelers + 1;
                            item.TotalPrice = item.TotalPrice + seatPrice.TotalPrice;
                            item.TotalPriceDisplayValue = TopHelper.FormatAmountForDisplay(item.TotalPrice, ci, false);
                            item.DiscountedTotalPrice = item.DiscountedTotalPrice + seatPrice.DiscountedTotalPrice;
                            item.DiscountedTotalPriceDisplayValue = TopHelper.FormatAmountForDisplay(item.DiscountedTotalPrice, ci, false);
                            if (item.SeatNumbers == null)
                                item.SeatNumbers = new List<string>();
                            if (!seatPrice.SeatNumbers.IsNullOrEmpty())
                                item.SeatNumbers.AddRange(seatPrice.SeatNumbers);
                        }
                        else
                        {
                            MOBSeatPrice sp = BuildSeatPricesWithDiscountedPrice(seatPrice);
                            if (!sp.IsNullOrEmpty())
                            {
                                seatPrices.Add(sp);
                            }
                        }
                    }
                    else
                    {
                        origin = seatPrice.Origin;
                        destination = seatPrice.Destination;

                        MOBSeatPrice sp = BuildSeatPricesWithDiscountedPrice(seatPrice);
                        if (!sp.IsNullOrEmpty())
                        {
                            seatPrices.Add(sp);
                        }
                    }
                }
                if (seatPrices != null)
                {

                    seatPrices = OrderSeatPriceItemsBySeatType(seatPrices);

                    seatPrices.FindAll(s => s.SeatMessage != null && s.NumberOftravelers > 1)
                                            .ForEach(q =>
                                            {
                                                q.SeatMessage = q.SeatMessage.Replace("Economy Plus Seat", "Economy Plus Seats");
                                                q.SeatMessage = q.SeatMessage.Replace("Advance Seat Assignment", "Advance Seat Assignments");
                                                q.SeatMessage = q.SeatMessage.Replace("Preferred seat", "Preferred seats");
                                            });

                }
            }

            return seatPrices;
        }

        private bool IsExistingSeat(string oldSeatAssignment, string currentSeatAssignment)
        {
            if (string.IsNullOrEmpty(oldSeatAssignment) || string.IsNullOrEmpty(currentSeatAssignment))
                return false;

            return oldSeatAssignment.Equals(currentSeatAssignment, StringComparison.OrdinalIgnoreCase);
        }

        private bool ShouldCreateNewCart(MOBRegisterSeatsRequest request)
        {
            if (!string.IsNullOrEmpty(request.CartId) && request.Flow == FlowType.VIEWRES_BUNDLES_SEATMAP.ToString())
                return false;

            return string.IsNullOrEmpty(request.CartId);
        }

        private async Task<MOBShoppingCart> RegisterSeats(MOBRequest request, string cartId, string token, SeatChangeState state, string flow = "")
        {
            RegisterSeatsRequest registerSeatsRequest = BuildRegisterSeatRequest(cartId, state, flow);

            if (registerSeatsRequest.SeatAssignments == null || registerSeatsRequest.SeatAssignments.Count == 0)
                return null;

            string jsonRequest = DataContextJsonSerializer.Serialize<RegisterSeatsRequest>(registerSeatsRequest);
            string actionName = "/registerseats";
            var jsonResponse = _shoppingCartService.GetRegisterSeats<FlightReservationResponse>(token, actionName, state.SessionId, jsonRequest).Result;

            if (jsonResponse.response != null)
            {
                if (jsonResponse.response != null && jsonResponse.response.Errors.Count() == 0)
                {
                    MOBShoppingCart persistShoppingCart = new MOBShoppingCart();
                    persistShoppingCart = _sessionHelperService.GetSession<MOBShoppingCart>(state.SessionId, persistShoppingCart.ObjectName, new List<string> { state.SessionId, persistShoppingCart.ObjectName }).Result;

                    var reservationDetail = new United.Service.Presentation.ReservationResponseModel.ReservationDetail();
                    reservationDetail = _sessionHelperService.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(state.SessionId, new Service.Presentation.ReservationResponseModel.ReservationDetail().GetType().FullName, new List<string> { state.SessionId, new Service.Presentation.ReservationResponseModel.ReservationDetail().GetType().FullName }).Result;

                    if (persistShoppingCart == null)
                        persistShoppingCart = new MOBShoppingCart();
                    persistShoppingCart.Flow = GetFlow(persistShoppingCart, flow);
                    persistShoppingCart.Products = await _productInfoHelper.ConfirmationPageProductInfo(jsonResponse.response, false, request.Application, state);
                    persistShoppingCart.CartId = cartId;
                    var grandtotal = _configuration.GetValue<bool>("EnableTravelOptionsBundleInViewRes")
                         ? persistShoppingCart?.Products?.Sum(p => string.IsNullOrEmpty(p.ProdTotalPrice) ? 0 : Convert.ToDecimal(p.ProdTotalPrice)) ?? 0
                         : jsonResponse.response.DisplayCart.DisplaySeats.Select(x => x.SeatPrice).ToList().Sum();
                    persistShoppingCart.TotalPrice = String.Format("{0:0.00}", grandtotal);
                    persistShoppingCart.DisplayTotalPrice = Decimal.Parse(grandtotal.ToString()).ToString("c");
                    if (ConfigUtility.IsManageResETCEnabled(request.Application.Id, request.Application.Version.Major))
                    {
                        persistShoppingCart.Prices = _paymentUtility.AddGrandTotalIfNotExist(persistShoppingCart.Prices, Convert.ToDouble(persistShoppingCart.TotalPrice), persistShoppingCart.Flow);
                    }
                    persistShoppingCart.TermsAndConditions = await _productInfoHelper.GetProductBasedTermAndConditions(state.SessionId, jsonResponse.response, false);
                    persistShoppingCart.PaymentTarget = _formsOfPayment.GetPaymentTargetForRegisterFop(jsonResponse.response);
                    persistShoppingCart.DisplayMessage = reservationDetail != null && reservationDetail.Detail != null ? await _paymentUtility.GetPaymentMessagesForWLPNRViewRes(jsonResponse.response, reservationDetail.Detail.FlightSegments, flow) : null;
                    persistShoppingCart.TripInfoForUplift = _paymentUtility.GetUpliftTripInfo(reservationDetail, persistShoppingCart.TotalPrice, persistShoppingCart.Products);

                    if (persistShoppingCart.Captions.IsNullOrEmpty())
                    {
                        persistShoppingCart.Captions = await _productInfoHelper.GetCaptions("PaymentPage_ViewRes_Captions");
                    }
                    if (!persistShoppingCart.Products.IsNullOrEmpty())
                    {
                        persistShoppingCart.IsCouponEligibleProduct = !_configuration.GetValue<bool>("IsEnableManageResCoupon") && IsCouponEligibleProduct(persistShoppingCart.Products);
                        var androidAndroidIsEnableManageResMerchCouponAppVersion = _configuration.GetValue<string>("Android_IsEnableManageResMerchCoupon_AppVersion");
                        var iphoneIsEnableManageResMerchCouponAppVersion = _configuration.GetValue<string>("iPhone_IsEnableManageResMerchCoupon_AppVersion");
                        persistShoppingCart.PromoCodeDetails = _configuration.GetValue<bool>("IsEnableManageResCoupon") && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, androidAndroidIsEnableManageResMerchCouponAppVersion, iphoneIsEnableManageResMerchCouponAppVersion) ? new MOBPromoCodeDetails() : null;
                    }
                    if (_configuration.GetValue<bool>("EnableMilesAsPayment"))
                    {
                        if (persistShoppingCart.Products.SelectMany(p => p.Segments).ToList().SelectMany(s => s.SubSegmentDetails).Where(m => m.Miles == 0).ToList().Count > 0)
                        {
                            foreach (ProductSubSegmentDetail sSegment in persistShoppingCart.Products.SelectMany(p => p.Segments).ToList().SelectMany(s => s.SubSegmentDetails))
                            {
                                sSegment.Miles = 0;
                                sSegment.DisplayMiles = string.Empty;
                            }
                            persistShoppingCart.TotalMiles = "0";
                            persistShoppingCart.DisplayTotalMiles = string.Empty;
                        }
                        else
                        {
                            persistShoppingCart.TotalMiles = (_configuration.GetValue<int>("milesFOP") * persistShoppingCart.Products.SelectMany(p => p.Segments).ToList().SelectMany(s => s.SubSegmentDetails).Count()).ToString();
                            persistShoppingCart.DisplayTotalMiles = UtilityHelper.FormatAwardAmountForDisplay(persistShoppingCart.TotalMiles, false);
                        }
                    }
                    //Billing International address
                    if (ConfigUtility.IsInternationalBillingAddress_CheckinFlowEnabled(request.Application)
                        && persistShoppingCart.FormofPaymentDetails?.InternationalBilling?.BillingAddressProperties == null && flow?.ToLower() == FlowType.CHECKIN.ToString().ToLower())
                    {
                        persistShoppingCart.FormofPaymentDetails = GetBillingAddressProperties(persistShoppingCart.FormofPaymentDetails);
                    }
                    await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, state.SessionId, new List<string> { state.SessionId, persistShoppingCart.ObjectName }, persistShoppingCart.ObjectName);
                    return persistShoppingCart;
                }
            }
            return null;
        }

        private MOBFormofPaymentDetails GetBillingAddressProperties(MOBFormofPaymentDetails formofPaymentDetails)
        {
            if (formofPaymentDetails == null)
            {
                formofPaymentDetails = new MOBFormofPaymentDetails();
            }
            formofPaymentDetails.InternationalBilling = new MOBInternationalBilling();
            var billingCountries = GetCachedBillingAddressCountries();

            if (billingCountries == null || !billingCountries.Any())
            {
                billingCountries = new List<MOBCPBillingCountry>();

                billingCountries.Add(new MOBCPBillingCountry
                {
                    CountryCode = "US",
                    CountryName = "United States",
                    Id = "1",
                    IsStateRequired = true,
                    IsZipRequired = true
                });
            }
            formofPaymentDetails.InternationalBilling.BillingAddressProperties = (billingCountries == null || !billingCountries.Any()) ? null : billingCountries;
            return formofPaymentDetails;
        }

        private List<MOBCPBillingCountry> GetCachedBillingAddressCountries()
        {
            try
            {
                var seatMapDynamoDB = new SeatMapDynamoDB(_configuration, _dynamoDBService);
                var billingCountriesList = _cachingService.GetCache<List<MOBCPBillingCountry>>(_configuration.GetValue<string>("INFLIGHTPURCHASE_BILLINGCOUNTRIES_CACHED_STATICGUID") + "MOBCPBillingCountry", _headers.ContextValues.TransactionId).Result;
                var billingCountries = JsonConvert.DeserializeObject<List<MOBCPBillingCountry>>(billingCountriesList);
                var billCountries = new MOBCPBillingCountry();

                if (billingCountries == null || !billingCountries.Any())
                {
                    billingCountries = new List<MOBCPBillingCountry>();
                    billCountries = new MOBCPBillingCountry()
                    {
                        CountryName = billCountries.CountryName,
                        CountryCode = billCountries.CountryCode,
                        Id = billCountries.Id,
                        IsStateRequired = billCountries.IsStateRequired,
                        IsZipRequired = billCountries.IsZipRequired

                    };
                    billingCountries = seatMapDynamoDB?.GetBillingAddressCountries<List<MOBCPBillingCountry>>(billCountries.Id, _headers.ContextValues.SessionId).Result;

                    #region
                    //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
                    //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("uasp_Select_BillingCountryList");
                    //using (IDataReader dataReader = database.ExecuteReader(dbCommand))
                    //{
                    //    while (dataReader.Read())
                    //    {
                    //        billingCountries.Add(new MOBCPBillingCountry
                    //        {
                    //            CountryName = Convert.ToString(dataReader["CountryName"]),
                    //            CountryCode = Convert.ToString(dataReader["CountryCode"]),
                    //            Id = Convert.ToString(dataReader["BillingCountryOrder"]),
                    //            IsStateRequired = Convert.ToBoolean(dataReader["IsStateRequired"]),
                    //            IsZipRequired = Convert.ToBoolean(dataReader["IsZipPostalRequired"]),
                    //        });
                    //    }
                    //}
                    #endregion

                    _cachingService.SaveCache<List<MOBCPBillingCountry>>(_configuration.GetValue<string>("INFLIGHTPURCHASE_BILLINGCOUNTRIES_CACHED_STATICGUID") + "MOBCPBillingCountry", billingCountries, _headers.ContextValues.TransactionId, new TimeSpan(1, 30, 0));
                }
                return billingCountries;
            }
            catch { }
            return null;
        }

        private List<MOBSeatPrice> OrderSeatPriceItemsBySeatType(List<MOBSeatPrice> seatPrices)
        {
            if (seatPrices == null || !seatPrices.Any())
                return seatPrices;
            return seatPrices.GroupBy(g => g.Origin + " - " + g.Destination)
                             .SelectMany(odSeatPriceGrp => GetOrderredSeatPricesForOriginDestination(odSeatPriceGrp)).ToList();
        }

        private IEnumerable<MOBSeatPrice> GetOrderredSeatPricesForOriginDestination(IGrouping<string, MOBSeatPrice> groupOfSeatPrices)
        {
            if (groupOfSeatPrices == null || !groupOfSeatPrices.Any())
                return groupOfSeatPrices;
            var orderOfSeatPriceItems = UtilityHelper.GetSeatPriceOrder();
            var orderredSeatPrices = groupOfSeatPrices.OrderBy(g => orderOfSeatPriceItems[g.SeatMessage]);
            orderredSeatPrices.FirstOrDefault().SeatPricesHeaderandTotal = new MOBItem { Id = groupOfSeatPrices.Key };
            return orderredSeatPrices;
        }

        private string GetFlow(MOBShoppingCart persistShoppingCart, string flow)
        {
            if (persistShoppingCart?.Flow == FlowType.VIEWRES_BUNDLES_SEATMAP.ToString() || flow == FlowType.VIEWRES_BUNDLES_SEATMAP.ToString())
            {
                return FlowType.VIEWRES.ToString();
            }
            return string.IsNullOrEmpty(persistShoppingCart?.Flow) ? flow : persistShoppingCart.Flow;
        }
        private RegisterSeatsRequest BuildRegisterSeatRequest(string cartId, SeatChangeState state, string flow)
        {
            RegisterSeatsRequest registerSeatsRequest = new RegisterSeatsRequest();

            var reservationDetail = new United.Service.Presentation.ReservationResponseModel.ReservationDetail();
            reservationDetail = _sessionHelperService.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(state.SessionId, new Service.Presentation.ReservationResponseModel.ReservationDetail().GetType().FullName, new List<string> { state.SessionId, new Service.Presentation.ReservationResponseModel.ReservationDetail().GetType().FullName }).Result;

            registerSeatsRequest.CartId = cartId;
            registerSeatsRequest.Characteristics = new Collection<Characteristic>() {
                    new Characteristic() { Code = "ManageRes", Value = "true" }};
            registerSeatsRequest.Reservation = reservationDetail.Detail;
            if (registerSeatsRequest.Reservation.FlightSegments != null && registerSeatsRequest.Reservation.FlightSegments.Count > 0)
            {
                registerSeatsRequest.Reservation.FlightSegments.ForEach(s => s.FlightSegment.OperatingAirlineCode =
                _configuration.GetValue<string>("SeatMapForACSubsidiary").Contains(s.FlightSegment.OperatingAirlineCode) ? "AC" : s.FlightSegment.OperatingAirlineCode);
            }
            registerSeatsRequest.IsPNRFirst = false;
            registerSeatsRequest.RecordLocator = reservationDetail.Detail.ConfirmationID;
            registerSeatsRequest.OfferRequest = BuildRegisterPcuSeatOffer(state, cartId);
            List<SeatAssignment> seatAssignments = new List<SeatAssignment>();
            registerSeatsRequest.SeatAssignments = state.BookingTravelerInfo.SelectMany(x => x.Seats).Where(x => x.SeatAssignment != null && x.SeatAssignment != x.OldSeatAssignment).Select(x => new SeatAssignment
            {
                Adjustments = x.IsCouponApplied ? null : (!x.Adjustments.IsNullOrEmpty() ? x.Adjustments.ToCollection() : null),
                Currency = x.Currency,
                FlattenedSeatIndex = Convert.ToInt32(state.Segments.Where(y => y.Arrival.Code == x.Destination && y.Departure.Code == x.Origin && y.FlightNumber == x.FlightNumber).Select(y => y.SegmentIndex).FirstOrDefault()) - 1,
                OptOut = false,
                OriginalPrice = x.OldSeatPrice,
                ProductCode = _configuration.GetValue<bool>("DisableFixForIncorrectEdocSeats_MRM_1759") ? x.OldSeatProgramCode : null,
                PCUSeat = !string.IsNullOrEmpty(x.PcuOfferOptionId),
                Seat = x.SeatAssignment,
                SeatPrice = x.IsCouponApplied ? x.PriceAfterTravelerCompanionRules : (!x.Adjustments.IsNullOrEmpty() ? x.PriceAfterCouponApplied : x.PriceAfterTravelerCompanionRules),
                SeatPromotionCode = x.ProgramCode,
                SeatType = x.SeatType,
                TravelerIndex = Convert.ToInt32(x.TravelerSharesIndex.Split('.')[0]) - 1,
                OriginalSegmentIndex = Convert.ToInt32(state.Segments.Where(y => y.Arrival.Code == x.Destination && y.Departure.Code == x.Origin && y.FlightNumber == x.FlightNumber).Select(y => y.OriginalSegmentNumber).FirstOrDefault()),
                LegIndex = Convert.ToInt32(state.Segments.Where(y => y.Arrival.Code == x.Destination && y.Departure.Code == x.Origin && y.FlightNumber == x.FlightNumber).Select(y => y.LegIndex).FirstOrDefault()),
                PersonIndex = x.TravelerSharesIndex.ToString(),
                ArrivalAirportCode = x.Destination.ToString(),
                DepartureAirportCode = x.Origin.ToString(),
                FlightNumber = x.FlightNumber.ToString()
            }).ToList();
            if (_configuration.GetValue<bool>("EnableCSLCloudMigrationToggle"))
            {
                registerSeatsRequest.WorkFlowType = ConfigUtility.GetWorkFlowType(flow);
            }

            return registerSeatsRequest;
        }

        private RegisterOfferRequest BuildRegisterPcuSeatOffer(SeatChangeState seatState, string cartId)
        {
            if (seatState == null || seatState.BookingTravelerInfo == null)
                return null;

            var seats = seatState.BookingTravelerInfo.SelectMany(x => x.Seats);
            if (seats == null || !seats.Any())
                return null;

            var optionIds = seats.Where(s => s != null && !string.IsNullOrEmpty(s.PcuOfferOptionId)).Select(s => s.PcuOfferOptionId);

            if (optionIds == null || !optionIds.Any())
                return null;

            var pcuState = new PcuState();
            pcuState = _sessionHelperService.GetSession<PcuState>(seatState.SessionId, pcuState.ObjectName, new List<string> { seatState.SessionId, pcuState.ObjectName }).Result;
            var productIds = pcuState.EligibleSegments.SelectMany(e => e.UpgradeOptions.Where(s => optionIds.Contains(s.OptionId))).SelectMany(u => u.ProductIds).ToList();

            var productOffer = new GetOffers();
            productOffer = _sessionHelperService.GetSession<GetOffers>(seatState.SessionId, productOffer.ObjectName, new List<string> { seatState.SessionId, productOffer.ObjectName }).Result;
            productOffer.Offers[0].ProductInformation.ProductDetails.RemoveWhere(p => p.Product.Code != "PCU");

            return _shopping.GetRegisterOffersRequest(cartId, null, null, null, "PCU", null, productIds, null, false, productOffer, null);
        }

        #region Utility
        private MOBSeatPrice BuildSeatPrice(Model.Shopping.Misc.Seat seat)
        {
            if (seat.IsNullOrEmpty())
                return null;
            var ci = TopHelper.GetCultureInfo(seat.Currency);
            var seatMessage = GetSeatMessage(seat.SeatType, seat.SeatFeature, seat.ProgramCode, seat.LimitedRecline);
            return new MOBSeatPrice
            {
                Origin = seat.Origin,
                Destination = seat.Destination,
                SeatMessage = seatMessage,
                NumberOftravelers = 1,
                TotalPrice = seat.Price,
                TotalPriceDisplayValue = TopHelper.FormatAmountForDisplay(seat.Price, ci, false),
                DiscountedTotalPrice = seat.PriceAfterTravelerCompanionRules,
                DiscountedTotalPriceDisplayValue = TopHelper.FormatAmountForDisplay(seat.PriceAfterTravelerCompanionRules, ci, false),
                CurrencyCode = seat.Currency,
                SeatNumbers = new List<string> { seat.SeatAssignment }
            };
        }

        private string GetSeatMessage(string seatType, string seatFeature, string programCode, bool limitedRecline)
        {
            if (ConfigUtility.IsEMinusSeat(programCode))
                return "Advance Seat Assignment";

            if (_configuration.GetValue<bool>("isEnablePreferredZone") && (_seatEngine.IsPreferredSeat(seatType, programCode) || _seatEngine.IsPreferredSeat(seatFeature, programCode)))
                return _configuration.GetValue<string>("PreferedSeat_PriceBreakdownTitle");

            return limitedRecline ? "Economy Plus Seat (limited recline)"
                                  : "Economy Plus Seat";
        }

        private MOBSeatPrice BuildSeatPricesWithDiscountedPrice(MOBSeatPrice seatPrice)
        {
            if (seatPrice.IsNullOrEmpty())
                return null;

            CultureInfo ci = TopHelper.GetCultureInfo(seatPrice.CurrencyCode);
            return new MOBSeatPrice
            {
                Origin = seatPrice.Origin,
                Destination = seatPrice.Destination,
                SeatMessage = seatPrice.SeatMessage,
                NumberOftravelers = 1,
                TotalPrice = seatPrice.TotalPrice,
                TotalPriceDisplayValue = TopHelper.FormatAmountForDisplay(seatPrice.TotalPrice, ci, false),
                DiscountedTotalPrice = seatPrice.DiscountedTotalPrice,
                DiscountedTotalPriceDisplayValue = TopHelper.FormatAmountForDisplay(seatPrice.DiscountedTotalPrice, ci, false),
                CurrencyCode = seatPrice.CurrencyCode,
                SeatNumbers = seatPrice.SeatNumbers
            };
        }

        private bool IsCouponEligibleProduct(List<ProdDetail> proddetail)
        {
            string couponProductCode = _configuration.GetValue<string>("IsCouponEligibleProduct") ?? string.Empty;

            if (!couponProductCode.IsNullOrEmpty())
            {
                var couponProductCodeList = couponProductCode.Split('|');
                foreach (var c in proddetail)
                {

                    if (couponProductCodeList.Any(t => t.Equals(c.Code)))
                    {
                        return true;
                    }

                }
            }

            return false;
        }

        private void ChangeDAAMapForUpperVersion(string appVersion, MOBSeatMapRequest request, MOBSeatMapResponse response)
        {
            bool replaceDAFRMtoDAFL = _configuration.GetValue<bool>("ReplaceDAFRMtoDAFR");
            if (replaceDAFRMtoDAFL)
            {
                string iOSVersionWithNewDAASeatMap = _configuration.GetValue<string>("iOSVersionWithNewDAASeatMap");
                string andriodVersionWithNewDAASeatMap = _configuration.GetValue<string>("andriodVersionWithNewDAASeatMap");
                var versionWithNewDAASeatMap = iOSVersionWithNewDAASeatMap;
                if (request.Application.Id == 2)
                {
                    versionWithNewDAASeatMap = andriodVersionWithNewDAASeatMap;
                }
                bool returnNewDAASeatMap = GeneralHelper.IsVersion1Greater(appVersion, versionWithNewDAASeatMap, true);
                if (!returnNewDAASeatMap && response.SeatMap != null && response.SeatMap.Cabins != null)
                {
                    foreach (var seat in response.SeatMap.Cabins.SelectMany(c => c.Rows.SelectMany(r => r.Seats)).Where(s => !string.IsNullOrEmpty(s.Program) && s.Program.Trim().ToUpper() == "DAFRM"))
                    {
                        seat.Program = "DAFL";
                    }
                }
            }

        }

        private bool ShowSeatChangeButton(MOBPNR pnr)
        {
            if (pnr.Passengers != null && pnr.Passengers.Count > 9 || pnr.isgroup == true)
            {
                return false;
            }

            return pnr.IsEligibleToSeatChange && _configuration.GetValue<bool>("ShowSeatChange");
        }

        #endregion
    }
}
