using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.ReShop;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.CloudDynamoDB;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.ReShop;
using United.Mobile.Model.Shopping;
using United.Mobile.ReShop.Domain;
using United.Mobile.Services.Shopping.Domain;
using United.Service.Presentation.ReservationModel;
using United.Service.Presentation.ReservationRequestModel;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common.Extensions;
using United.Utility.Enum;
using United.Utility.Extensions;
using United.Utility.Helper;
using MOBSHOPResponseStatusItem = United.Mobile.Model.Common.MOBSHOPResponseStatusItem;
using Reservation = United.Mobile.Model.Shopping.Reservation;


namespace United.Mobile.EligibleCheck.Domain
{
    public class EligibleCheckBusiness : IEligibleCheckBusiness
    {
        private readonly ICacheLog<EligibleCheckBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IValidateHashPinService _validateHashPinService;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly IDPService _dPService;
        private readonly IPNRRetrievalService _pNRRetrievalService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly AirportDynamoDB _airportDynamoDB;
        private readonly IRefundService _refundService;
        private readonly IShoppingBusiness _shopBusiness;
        private readonly ICachingService _cachingService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private HttpContext _httpContext;
        private readonly IHeaders _headers;
        private readonly IFeatureSettings _featureSettings;
        private readonly ICMSContentService _iCMSContentService;

        public EligibleCheckBusiness(ICacheLog<EligibleCheckBusiness> logger
            , IConfiguration configuration
            , IValidateHashPinService validateHashPinService
            , ISessionHelperService sessionHelperService
            , IShoppingSessionHelper shoppingSessionHelper
            , IDPService dPService
            , IPNRRetrievalService pNRRetrievalService
            , IDynamoDBService dynamoDBService
            , IRefundService refundService
            , IShoppingBusiness shopBusiness
            , ICachingService cachingService
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService 
            , IHeaders headers
            , IFeatureSettings featureSettings
            , ICMSContentService iCMSContentService
           )
        {
            _logger = logger;
            _configuration = configuration;
            _validateHashPinService = validateHashPinService;
            _sessionHelperService = sessionHelperService;
            _shoppingSessionHelper = shoppingSessionHelper;
            _dPService = dPService;
            _pNRRetrievalService = pNRRetrievalService;
            _dynamoDBService = dynamoDBService;
            _headers = headers;
            _airportDynamoDB = new AirportDynamoDB(_configuration, _dynamoDBService);
            _refundService = refundService;
            _shopBusiness = shopBusiness;
            _cachingService = cachingService;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _featureSettings = featureSettings;
            ConfigUtility.UtilityInitialize(_configuration);
            EligibilityCheckHelper.UtilityInitialize(_configuration);
            _iCMSContentService = iCMSContentService;
        }

        public async Task<MOBRESHOPChangeEligibilityResponse> ChangeEligibleCheckAndReshop(MOBRESHOPChangeEligibilityRequest request)
        {
            var response = new MOBRESHOPChangeEligibilityResponse();
            var persistedReservation = new Reservation();
            Session session = null;
            Service.Presentation.ReservationResponseModel.ReservationDetail cslReservation;

            if (!string.IsNullOrEmpty(request.SessionId))
            {
                session = await _sessionHelperService.GetSession<Session>(request.SessionId, new Session().ObjectName, new List<string>() { request.SessionId, new Session().ObjectName }).ConfigureAwait(false);
            }

            if (session == null)
            {
                session = await _shoppingSessionHelper.CreateShoppingSession
                    (request.Application.Id, request.DeviceId, request.Application.Version.Major, request.TransactionId,
                    request.MileagePlusNumber, string.Empty);
            }

            if (session != null)
            {
                request.SessionId = session.SessionId;
                session.IsReshopChange = true;

                _headers.ContextValues.SessionId = session.SessionId;

                if (request?.ReshopRequest != null)
                {
                    session.IsAward = request.ReshopRequest.AwardTravel;
                }

                session.Flow = Convert.ToString(FlowType.CHECKINSDC);
                request.FlowType = Convert.ToString(FlowType.CHECKINSDC);

                await _sessionHelperService.SaveSession<Session>(session, _headers.ContextValues.SessionId, new List<string>() { _headers.ContextValues.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);
            }

            _logger.LogInformation("ChangeEligibleCheckAndReshop {clientResquest} and {sessionId}", request, session);

            //Eligibility Call
            response = await ReshoppingChangeEligibleCheck(request, isAllowAward: true, isSameDayChangeVersion: true, isExceptionPolicyVersion: true);

            cslReservation = await _sessionHelperService.GetSession<ReservationDetail>
              (_headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName, new List<string>() { _headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName }).ConfigureAwait(false);


            bool isCheckinEligible = EligibilityCheckHelper.CheckCheckinEligible(request);

            ////Request Mapping  
            var reshoprequest = new MOBSHOPShopRequest();
            reshoprequest
              = await CreateReshopMOBSHOPShopRequest(request, response, cslReservation);

            bool enableCheckinReshop30Redirect = await _featureSettings.GetFeatureSettingValue("EnableCheckinReshop30Redirect");
            response.RedirectURL = EligibilityCheckHelper.GetSDCRedirect3dot0Url(request, response, enableCheckinReshop30Redirect);
            response.WebSessionShareUrl = _configuration.GetValue<string>("DotcomSSOUrl");

            await BuildReservationObjectForReshopChange
                   (request, cslReservation, isCanceledWithFutureFlightCredit: false, mresponse: response);

            persistedReservation = await _sessionHelperService.GetSession<Reservation>(_headers.ContextValues.SessionId, new Reservation().ObjectName, new List<string>() { _headers.ContextValues.SessionId, new Reservation().ObjectName }).ConfigureAwait(false);

            response.Reservation = new MOBSHOPReservation(_configuration, _cachingService);

            await EligibilityCheckHelper.MapPersistedReservationToMOBSHOPReservation(response.Reservation, persistedReservation);

            if (response.PathEligible && isCheckinEligible)
            {
                response.ShopResponse = await _shopBusiness.GetShop(reshoprequest,_httpContext);
                if (response.ShopResponse.ShopRequest?.Trips != null && response.ShopResponse.ShopRequest.Trips.Any() && request.FlowType == "CHECKINSDC")
                {
                    response.ShopResponse.ShopRequest?.Trips.ForEach(s => { s.OriginAllAirports = s.OriginAllAirports == 0 ? -1 : s.OriginAllAirports; s.DestinationAllAirports = s.DestinationAllAirports == 0 ? -1 : s.DestinationAllAirports; });
                }
            }
            else
            {
                response.PathEligible = false;
                //IN-ELIGIBLE code starts 
                //SET SSO 
                if (EligibilityCheckHelper.EnableChangeWebSSO(request.Application.Id, request.Application.Version.Major))
                {
                    if (!string.IsNullOrEmpty(request.MileagePlusNumber) && !string.IsNullOrEmpty(request.HashPinCode))
                    {
                        if (await _featureSettings.GetFeatureSettingValue("CSDCMPSignedInAuthTokenFix").ConfigureAwait(false))
                        {
                            MileagePlusDetails validWalletRequest;

                            HashPin hashPin = new HashPin(_logger, _configuration, _validateHashPinService, _dynamoDBService, _headers, _featureSettings);

                            validWalletRequest = await hashPin.ValidateHashPinAndGetAuthTokenDynamoDB
                                (request.MileagePlusNumber, request.HashPinCode, request.Application.Id, request.DeviceId, request.Application.Version.Major);

                            if (validWalletRequest!=null && !validWalletRequest.IsTokenValid.ToBoolean())
                                throw new MOBUnitedException(_configuration.GetValue<string>("bugBountySessionExpiredMsg"));

                            if (validWalletRequest != null && validWalletRequest.IsTokenValid.ToBoolean())
                            {
                                response.TransactionId = request.TransactionId;
                                response.LanguageCode = request.LanguageCode;
                                response.WebShareToken = _dPService.GetSSOTokenString(request.Application.Id, request.MileagePlusNumber, _configuration)?.ToString();
                                if (await _featureSettings.GetFeatureSettingValue("EnableRedirectURLUpdate").ConfigureAwait(false))
                                {
                                    response.RedirectURL = $"{_configuration.GetValue<string>("NewDotcomSSOUrl")}?type=sso&token={response.WebShareToken}&landingUrl={response.RedirectURL}";
                                    response.WebSessionShareUrl = response.WebShareToken = string.Empty;
                                }
                            }
                        }
                        else {
                            bool validWalletRequest = false;

                            HashPin hashPin = new HashPin(_logger, _configuration, _validateHashPinService, _dynamoDBService, _headers, _featureSettings);

                            validWalletRequest = await hashPin.ValidateHashPinAndGetAuthToken
                                (request.MileagePlusNumber, request.HashPinCode, request.Application.Id, request.DeviceId, request.Application.Version.Major);

                            if (!validWalletRequest)
                                throw new MOBUnitedException(_configuration.GetValue<string>("bugBountySessionExpiredMsg"));

                            if (validWalletRequest)
                            {
                                response.TransactionId = request.TransactionId;
                                response.LanguageCode = request.LanguageCode;
                                response.WebShareToken = _dPService.GetSSOTokenString(request.Application.Id, request.MileagePlusNumber, _configuration)?.ToString();
                                if (await _featureSettings.GetFeatureSettingValue("EnableRedirectURLUpdate").ConfigureAwait(false))
                                {
                                    response.RedirectURL = $"{_configuration.GetValue<string>("NewDotcomSSOUrl")}?type=sso&token={response.WebShareToken}&landingUrl={response.RedirectURL}";
                                    response.WebSessionShareUrl = response.WebShareToken = string.Empty;
                                }
                            }
                        }
                    }
                }
                //end                    
            }

            if (cslReservation?.Detail?.Sponsor != null)
            {
                response.SponsorMileagePlus
                    = cslReservation.Detail.Sponsor?.LoyaltyProgramMemberID;
            }
            else
            {
                if (request?.ReshopRequest != null && request.ReshopRequest.IsCorporateBooking)
                {
                    response.SponsorMileagePlus = cslReservation.Detail.Travelers?
                        .FirstOrDefault()?.LoyaltyProgramProfile?.LoyaltyProgramMemberID;
                }
            }
            if(response?.Reservation != null)
            {
                response.Reservation.Messages = (response.Reservation.Messages == null)? new List<string>() : response.Reservation.Messages;
                response.Reservation.FareRules = (response.Reservation.FareRules == null)? new List<FareRules>() : response.Reservation.FareRules;
                response.Reservation.Prices = (response.Reservation.Prices == null)? new List<MOBSHOPPrice>() : response.Reservation.Prices;
                response.Reservation.AlertMessages = (response.Reservation.AlertMessages == null)? new List<Section>() : response.Reservation.AlertMessages;
            }
            _logger.LogInformation("ChangeEligibleCheckAndReshop {clientResponse} and {sessionId}", response, session);

            //Default values
            if (response?.Reservation?.Reshop != null)
            {
                response.Reservation.Reshop.ReviewChangeBackBtnText
                    = (string.IsNullOrEmpty(request.CheckinSessionKey)) ? "Back to Reservation" : "Back to Checkin";

                response.Reservation.Reshop.CheckinSessionKey = request.CheckinSessionKey;
            }

            if (response?.ShopResponse?.Availability?.Reservation?.Reshop != null)
            {
                response.ShopResponse.Availability.Reservation.Reshop.ReviewChangeBackBtnText
                    = (string.IsNullOrEmpty(request.CheckinSessionKey)) ? "Back to Reservation" : "Back to Check-in";

                response.ShopResponse.Availability.Reservation.Reshop.CheckinSessionKey = request.CheckinSessionKey;
            }

            response.TransactionId = request.TransactionId;
            response.SessionId = request.SessionId;

            return response;
        }

        private async Task<Reservation> BuildReservationObjectForReshopChange
            (MOBRESHOPChangeEligibilityRequest request, ReservationDetail cslReservation,
            bool isCanceledWithFutureFlightCredit = false, MOBRESHOPChangeEligibilityResponse mresponse = null)
        {
            Reservation bookingPathRes = new Reservation();
            bookingPathRes.Reshop = new Reshop();
            bookingPathRes.Reshop.RecordLocator = request.RecordLocator;
            bookingPathRes.Reshop.LastName = request.LastName;
            bookingPathRes.Reshop.CheckinSessionKey = request.CheckinSessionKey;
            bookingPathRes.Reshop.FsrTitle = (_configuration.GetValue<string>("ReshopChange-FSRTitle") ?? "");
            bookingPathRes.Reshop.FsrChangeFeeTxt = (_configuration.GetValue<string>("ReshopChange-FSRHeader") ?? "");
            if (IsEbulkPNRReshopEnabledV2(request.IsEbulkCatalogOn, cslReservation)) {
                bookingPathRes.Reshop.FsrChangeFeeTxt = null;
            }
            bookingPathRes.Override24HrFlex = request.Override24HrFlex;
            bookingPathRes.Reshop.IsReshopWithFutureFlightCredit = isCanceledWithFutureFlightCredit;
            bookingPathRes.Reshop.ChangeTripTitle = HttpUtility.HtmlDecode(_configuration.GetValue<string>("ReshopChange-RTIFlightBlockTitle") ?? "");
            bookingPathRes.Reshop.ChangeFlightHeaderText = HttpUtility.HtmlDecode(_configuration.GetValue<string>("ReshopChange-CHANGEFLIGHTTEXT") ?? "");
            bookingPathRes.Reshop.ReviewChangeBackBtnText
                    = (string.IsNullOrEmpty(request.CheckinSessionKey)) ? "Back to Reservation" : "Back to Check-in";

            if (mresponse?.PnrTravelers != null && mresponse.PnrTravelers.Any())
            {
                bookingPathRes.NumberOfTravelers = mresponse.PnrTravelers.Count();
            }

            if (_configuration.GetValue<bool>("enableMANAGERESJSXChanges")
            && cslReservation?.Detail?.Characteristic != null)
            {
                bookingPathRes.HasJSXSegment
                    = (string.IsNullOrEmpty(ConfigUtility.GetCharactersticValue(cslReservation.Detail.Characteristic, "XE"))) ? false : true;
            }

            if (isCanceledWithFutureFlightCredit)
            {
                bookingPathRes.Reshop.ChangeTripTitle = string.Empty;
                bookingPathRes.Reshop.ChangeFlightHeaderText = string.Empty;
            }

            bookingPathRes.Reshop.FlightHeaderText = (_configuration.GetValue<string>("ReshopChange-NEWTRIP") ?? "");

            Reservation persistReservation = new Reservation();
            persistReservation = await _sessionHelperService.GetSession<Reservation>(_headers.ContextValues.SessionId, persistReservation.ObjectName, new List<string>() { _headers.ContextValues.SessionId, persistReservation.ObjectName }).ConfigureAwait(false);

            if (persistReservation != null)
            {
                if (persistReservation.Reshop.IsUsedPNR)
                {
                    bookingPathRes.Reshop.FlightHeaderText = persistReservation.Reshop.FlightHeaderText;
                    bookingPathRes.Reshop.IsUsedPNR = true;
                }
            }

            bookingPathRes.IsReshopChange = true;
            //bookingPathRes.Reshop.FeeWaiverMessage = (_configuration.GetValue<string>("ReshopChange-FEEWAIVEDMESSAGE") ?? "");
            if (cslReservation != null && cslReservation.Detail != null && cslReservation.Detail.Payment != null)
            {
                var waivedDesc = EligibilityCheckHelper.GetCharacteristicValue(cslReservation.Detail.Characteristic.ToList(), "24HrFlexibleBookingPolicy");
                bookingPathRes.Reshop.RefundFormOfPaymentMessage
                    = EligibilityCheckHelper.GetOriginalFormOfPaymentLabelForReshopChange(cslReservation, bookingPathRes, waivedDesc);
            }

            //var mobCPPhones = ReShopping.GetMobCpPhones(cslReservation.Detail.TelephoneNumbers);
            //if (mobCPPhones != null && mobCPPhones.Any())
            //{
            //    bookingPathRes.ReservationPhone = mobCPPhones.FirstOrDefault(x => x.PhoneNumber != null);
            //}
            var mobEmails = EligibilityCheckHelper.GetMobEmails(cslReservation.Detail.EmailAddress);
            if (mobEmails != null && mobEmails.Any())
            {
                bookingPathRes.ReservationEmail = mobEmails.FirstOrDefault(x => x.EmailAddress != null);
            }

            if (EligibilityCheckHelper.IncludeReshopFFCResidual(request.Application.Id, request.Application.Version.Major))
            {
                var mobffcrAddress = EligibilityCheckHelper.GetMobFFCRAddress(cslReservation.Detail.Travelers);
                if (mobffcrAddress != null)
                {
                    bookingPathRes.Reshop.FFCRAddress = mobffcrAddress;
                }
            }

            bookingPathRes.ReshopTrips = new List<ReshopTrip>();
            bookingPathRes.ReshopUsedTrips = new List<ReshopTrip>();

            if (await _featureSettings.GetFeatureSettingValue("EnablePartialylFlownTripNumberChanges") && Convert.ToBoolean(mresponse?.Characteristics?.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.Code) && c.Code.Equals("IsPartiallyUsedTicket", StringComparison.OrdinalIgnoreCase))?.Value))
            {

                bookingPathRes.IsPartiallyFlown = true;
            }
            mresponse.Characteristics = null;

            int tripNumber = 0;
            int remainingFlightsCount = 0;
            bool usedFlag = false;
            bool enableLOFChangesForReshop = await _featureSettings.GetFeatureSettingValue("EnableLOFChangesForReshop") && !bookingPathRes.IsPartiallyFlown;

            foreach (var cslsegment in cslReservation.Detail.FlightSegments)
            {
                if (!cslReservation.Detail.FlightSegments.Exists(p => p.TripNumber == "1"))
                {
                    usedFlag = true;
                    bookingPathRes.Reshop.IsUsedPNR = true;
                }

                bool flightUsedSegmentStatus = false;
                if (cslsegment.FlightSegment?.FlightStatuses?.Count() > 0)
                {
                    flightUsedSegmentStatus = cslsegment.FlightSegment.FlightStatuses[0].StatusType.Equals("USED");
                }
                // Reshop: Remove lof with used segement #
                if (flightUsedSegmentStatus && tripNumber == Convert.ToInt32(cslsegment.TripNumber) - 1)
                {
                    tripNumber++;
                    bookingPathRes.Reshop.IsUsedPNR = true;
                    var tripAllSegments = cslReservation.Detail.FlightSegments.Where(p => p.TripNumber == cslsegment.TripNumber).ToList();
                    var mOBSHOPReShopTrip = new ReshopTrip() { OriginalTrip = await ConvertPNRSegmentToShopTrip(tripAllSegments) };
                    bookingPathRes.ReshopUsedTrips.Add(mOBSHOPReShopTrip);
                    continue;
                }

                bool enableLOFChangesForReshopV2 = await _featureSettings.GetFeatureSettingValue("EnableLOFChangesForReshopV2") && !bookingPathRes.IsPartiallyFlown;

                if (tripNumber != (enableLOFChangesForReshop ? Convert.ToInt32(cslsegment.LOFNumber) : Convert.ToInt32(cslsegment.TripNumber)) && !flightUsedSegmentStatus)
                {
                    var tripAllSegments = enableLOFChangesForReshop ? cslReservation.Detail.FlightSegments.Where(p => p.LOFNumber == cslsegment.LOFNumber).ToList()
                        : cslReservation.Detail.FlightSegments.Where(p => p.TripNumber == cslsegment.TripNumber).ToList();
                    var mOBSHOPReShopTrip = new ReshopTrip() { OriginalTrip = await ConvertPNRSegmentToShopTrip(tripAllSegments, enableLOFChangesForReshop), OriginalUsedIndex = remainingFlightsCount };

                    bookingPathRes.ReshopTrips.Add(mOBSHOPReShopTrip);
                    tripNumber = enableLOFChangesForReshopV2 ? cslsegment.LOFNumber : cslsegment.TripNumber.ToInteger(0);
                    remainingFlightsCount++;
                }
            }
            if (remainingFlightsCount == 0)
            {
                throw new MOBUnitedException((_configuration.GetValue<string>("ReshopChange-USEDNOLOF") ?? ""));
            }

            if (bookingPathRes.Reshop.IsUsedPNR)
            {
                if (remainingFlightsCount <= 1)
                {
                    bookingPathRes.Reshop.FlightHeaderText = (_configuration.GetValue<string>("ReshopChange-REMAININGFLIGHT") ?? "");
                }
                else
                {
                    bookingPathRes.Reshop.FlightHeaderText = (_configuration.GetValue<string>("ReshopChange-REMAININGFLIGHTS") ?? "");
                }
            }
            else
            {
                bookingPathRes.Reshop.FlightHeaderText = (_configuration.GetValue<string>("ReshopChange-NEWTRIP") ?? "");
            }

            await _sessionHelperService.SaveSession<Reservation>(bookingPathRes, _headers.ContextValues.SessionId, new List<string>() { _headers.ContextValues.SessionId, bookingPathRes.ObjectName }, bookingPathRes.ObjectName).ConfigureAwait(false);

            return bookingPathRes;
        }

        private async Task<MOBSHOPTrip> ConvertPNRSegmentToShopTrip(List<ReservationFlightSegment> pnrFlightSegment,bool enableLOFChangesForReshop = false)
        {
            MOBSHOPTrip trip = null;
            if (pnrFlightSegment != null && pnrFlightSegment.Count > 0)
            {
                var pnrLastFlightSegment = enableLOFChangesForReshop ? pnrFlightSegment.Where(p => p.LOFNumber == pnrFlightSegment[0].LOFNumber).OrderByDescending(r => Convert.ToInt32(r.SegmentNumber)).FirstOrDefault()
                    : pnrFlightSegment.Where(p => p.TripNumber == pnrFlightSegment[0].TripNumber).OrderByDescending(r => Convert.ToInt32(r.SegmentNumber)).FirstOrDefault();
                trip = new MOBSHOPTrip();
                trip.Origin = pnrFlightSegment[0].FlightSegment.DepartureAirport.IATACode;
                trip.OriginDecoded = await _airportDynamoDB.GetAirportName(trip.Origin, _headers.ContextValues.SessionId); // pnrFlightSegment[0].FlightSegment.DepartureAirport.Name;
                trip.Destination = pnrLastFlightSegment.FlightSegment.ArrivalAirport.IATACode;
                trip.DestinationDecoded = await _airportDynamoDB.GetAirportName(trip.Destination, _headers.ContextValues.SessionId); //pnrLastFlightSegment.FlightSegment.ArrivalAirport.Name;
                trip.DepartDate = GeneralHelper.FormatDateFromDetails(pnrFlightSegment[0].FlightSegment.DepartureDateTime);


                trip.FlattenedFlights = new List<MOBSHOPFlattenedFlight>();
                MOBSHOPFlattenedFlight mobShopFlattenedFlight = new MOBSHOPFlattenedFlight();
                mobShopFlattenedFlight.Flights = new List<MOBSHOPFlight>();
                int currentFlightIndex = 0;
                foreach (var reservationFlightSegment in pnrFlightSegment)
                {
                    MOBSHOPFlight flight = new MOBSHOPFlight();
                    flight.Origin = reservationFlightSegment.FlightSegment.DepartureAirport.IATACode;
                    flight.Destination = reservationFlightSegment.FlightSegment.ArrivalAirport.IATACode;
                    flight.OriginDescription = await _airportDynamoDB.GetAirportName(flight.Origin, _headers.ContextValues.SessionId);
                    flight.DestinationDescription = await _airportDynamoDB.GetAirportName(flight.Destination, _headers.ContextValues.SessionId);

                    if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI"))
                    {
                        //with share trip flag OriginDescription=Chicago, IL, US (ORD)
                        flight.OriginDecodedWithCountry = reservationFlightSegment.FlightSegment.DepartureAirport.Name;
                        flight.DestinationDecodedWithCountry = reservationFlightSegment.FlightSegment.ArrivalAirport.Name;
                    }

                    //flight.TripId = trip.TripId;
                    //flight.IsStopOver = true;
                    flight.DepartureDateTimeGMT = reservationFlightSegment.FlightSegment.DepartureUTCDateTime;
                    flight.ArrivalDateTimeGMT = reservationFlightSegment.FlightSegment.ArrivalUTCDateTime;

                    bool flightDateChanged = false;
                    flight.FlightArrivalDays = GeneralHelper.GetDayDifference(pnrFlightSegment[0].FlightSegment.DepartureDateTime, reservationFlightSegment.FlightSegment.ArrivalDateTime);
                    flight.NextDayFlightArrDate = EligibilityCheckHelper.GetRedEyeFlightArrDate(pnrFlightSegment[0].FlightSegment.DepartureDateTime, reservationFlightSegment.FlightSegment.ArrivalDateTime, ref flightDateChanged);
                    flight.RedEyeFlightDepDate = EligibilityCheckHelper.GetRedEyeDepartureDate(pnrFlightSegment[0].FlightSegment.DepartureDateTime, reservationFlightSegment.FlightSegment.DepartureDateTime, ref flightDateChanged);
                    flight.FlightDateChanged = flightDateChanged;
                    //flight.DepartureDateTimeGMT = GetGMTTime(reservationFlightSegment.FlightSegment.DepartureDateTime, flight.Origin);
                    //flight.ArrivalDateTimeGMT = GetGMTTime(reservationFlightSegment.FlightSegment.ArrivalDateTime, flight.Destination);
                    if (reservationFlightSegment.BookingClass != null && reservationFlightSegment.BookingClass.Cabin != null)
                        flight.Cabin = reservationFlightSegment.BookingClass.Cabin.Name;
                    else
                        flight.Cabin = string.Empty;

                    flight.DepartDate = GeneralHelper.FormatDate(reservationFlightSegment.FlightSegment.DepartureDateTime);
                    flight.DepartTime = FormatTime(reservationFlightSegment.FlightSegment.DepartureDateTime);
                    flight.DestinationDate = GeneralHelper.FormatDate(reservationFlightSegment.FlightSegment.ArrivalDateTime);
                    flight.DestinationTime = FormatTime(reservationFlightSegment.FlightSegment.ArrivalDateTime);
                    flight.ArrivalDateTime = reservationFlightSegment.FlightSegment.ArrivalDateTime;
                    flight.DepartureDateTime = reservationFlightSegment.FlightSegment.DepartureDateTime;
                    flight.FlightNumber = reservationFlightSegment.FlightSegment.FlightNumber;

                    DateTime depatureDateTime = Convert.ToDateTime(reservationFlightSegment.FlightSegment.DepartureUTCDateTime);
                    DateTime arrivalDateTime = Convert.ToDateTime(reservationFlightSegment.FlightSegment.ArrivalUTCDateTime);
                    TimeSpan timeSpan = (arrivalDateTime - depatureDateTime);
                    if (((timeSpan.Days * 24) + timeSpan.Hours) > 0)
                    {
                        flight.TravelTime = string.Format("{0}h {1}m", ((timeSpan.Days * 24) + timeSpan.Hours), timeSpan.Minutes);
                    }
                    else
                    {
                        flight.TravelTime = string.Format("{0}m", timeSpan.Minutes);
                    }

                    if (currentFlightIndex > 0)
                    {
                        DateTime previousFlightArrivalTime = Convert.ToDateTime(pnrFlightSegment[currentFlightIndex - 1].FlightSegment.ArrivalUTCDateTime);
                        timeSpan = (depatureDateTime - previousFlightArrivalTime);
                        if (((timeSpan.Days * 24) + timeSpan.Hours) > 0)
                        {
                            flight.ConnectTimeMinutes = string.Format("{0}h {1}m", ((timeSpan.Days * 24) + timeSpan.Hours), timeSpan.Minutes);
                        }
                        else
                        {
                            flight.ConnectTimeMinutes = string.Format("{0}m", timeSpan.Minutes);
                        }
                    }


                    if (reservationFlightSegment.FlightSegment.MarketedFlightSegment != null && reservationFlightSegment.FlightSegment.MarketedFlightSegment.Count > 0)
                    {
                        flight.MarketingCarrier = reservationFlightSegment.FlightSegment.MarketedFlightSegment[0].MarketingAirlineCode;
                        flight.MarketingCarrierDescription = reservationFlightSegment.FlightSegment.MarketedFlightSegment[0].MarketingAirlineCode;
                    }
                    flight.OperatingCarrier = reservationFlightSegment.FlightSegment.OperatingAirlineCode;
                    flight.ServiceClassDescription = EligibilityCheckHelper.GetServiceClassDescriptionFromCslReservationFlightBookingClasses(reservationFlightSegment.FlightSegment.BookingClasses);
                    flight.EquipmentDisclosures = EligibilityCheckHelper.ConvertPNRSegmentEquipmentToMobShopEquipmentDisclousures(reservationFlightSegment.FlightSegment.Equipment);
                    flight.Meal = EligibilityCheckHelper.GetCharacteristicValue(reservationFlightSegment.Characteristic.ToList(), "MealDescription");
                    flight.Miles = reservationFlightSegment.FlightSegment.Distance.ToString();
                    flight.Messages = new List<MOBSHOPMessage>();

                    MOBSHOPMessage msg = new MOBSHOPMessage();
                    msg.MessageCode = flight.ServiceClassDescription;
                    flight.Messages.Add(msg);
                    msg = new MOBSHOPMessage();
                    msg.MessageCode = flight.Meal;
                    flight.Messages.Add(msg);
                    msg = new MOBSHOPMessage();
                    msg.MessageCode = "None";
                    flight.Messages.Add(msg);
                    if (reservationFlightSegment.FlightSegment.OperatingAirlineCode != "UA")
                    {
                        flight.OperatingCarrierDescription = reservationFlightSegment.FlightSegment.OperatingAirlineName;
                    }

                    mobShopFlattenedFlight.Flights.Add(flight);
                    if (reservationFlightSegment != null && reservationFlightSegment.FlightSegment != null && reservationFlightSegment.FlightSegment.Characteristic != null)
                    {
                        mobShopFlattenedFlight.IsIBE = EligibilityCheckHelper.IsIbe(reservationFlightSegment.FlightSegment.Characteristic);
                        mobShopFlattenedFlight.IsElf = EligibilityCheckHelper.IsElf(reservationFlightSegment.FlightSegment.Characteristic);
                    }
                    currentFlightIndex++;
                }
                trip.FlattenedFlights.Add(mobShopFlattenedFlight);
            }
            return trip;
        }

        private async Task<MOBRESHOPChangeEligibilityResponse> ReshoppingChangeEligibleCheck
           (MOBRESHOPChangeEligibilityRequest request, bool isAllowAward, bool isSameDayChangeVersion, bool isExceptionPolicyVersion)
        {
            string validToken = await _shoppingSessionHelper.GetSessionWithValidToken(request);

            var session = await _sessionHelperService.GetSession<Session>(_headers.ContextValues.SessionId, (new Session()).ObjectName, new List<string>() { _headers.ContextValues.SessionId, (new Session()).ObjectName }).ConfigureAwait(false);

            session.Token = validToken;

            if (!_configuration.GetValue<bool>("EnableBookWithCredit") && !string.IsNullOrEmpty(session.SessionId))
            {
                session.BWCSessionId = session.SessionId;
            }

            await _sessionHelperService.SaveSession<Session>(session, _headers.ContextValues.SessionId, new List<string>() { _headers.ContextValues.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);

            return await ChangeEligibleCheck(request, session.Token, isAllowAward, isSameDayChangeVersion, isExceptionPolicyVersion, session.IsAward);
        }

        private async Task<MOBRESHOPChangeEligibilityResponse> ChangeEligibleCheck(MOBRESHOPChangeEligibilityRequest changeEligibleCheckRequest, string cslToken, bool isAllowAward, bool isSameDayChangeVersion, bool isExceptionPolicyVersion, bool isAwardTravel)
        {
            MOBRESHOPChangeEligibilityResponse response = new MOBRESHOPChangeEligibilityResponse();

            // _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLRefundService"),

            string path = string.Format("/Eligible?recordLocator={0}&channel={1}&path=change&AllowAward={2}&allowFutureFlightCredit={3}",
                              changeEligibleCheckRequest.RecordLocator
                              , changeEligibleCheckRequest.Application.Id.ToString()
                                , isAllowAward
                                , _configuration.GetValue<bool>("AllowActiveFutureFlightCreditInChangePath"));

            RefundReservation refundReservation = new RefundReservation();

            //var url = await _refundService.EligibleCheck<MOBRESHOPChangeEligibilityResponse>(cslContext.Token
            //    , changeEligibleCheckRequest.SessionId
            //    , changeEligibleCheckRequest.RecordLocator
            //    , changeEligibleCheckRequest.Application.Id.ToString()
            //    , isAllowAward
            //    , _configuration.GetValue<bool>("AllowActiveFutureFlightCreditInChangePath"));

            var cslResponse = await MakeHTTPCallAndLogIt(
                                        changeEligibleCheckRequest.SessionId,
                                        changeEligibleCheckRequest.DeviceId,
                                        "CSL-ChangeEligibleCheck",
                                        changeEligibleCheckRequest.Application,
                                        cslToken,
                                        path,
                                        string.Empty,
                                        true,
                                        false);

            if (cslResponse != null)
            {
                bool IsEnableChangeEligibleResponseDeserialize = await _featureSettings.GetFeatureSettingValue("EnableChangeEligibleResponseDeserialize").ConfigureAwait(false);

                if (IsEnableChangeEligibleResponseDeserialize)
                {
                    byte[] jsonBytes = Encoding.UTF8.GetBytes(cslResponse);
                    using (var stream = new MemoryStream(jsonBytes))
                    {
                        response = Deserialize<MOBRESHOPChangeEligibilityResponse>(stream);
                    }
                }
                else
                {
                    response
                        = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityResponse>(cslResponse);
                }


                bool isUnaccompaniedMinor = false;

                var reservationdetails
                    = new United.Service.Presentation.ReservationResponseModel.ReservationDetail();

                if (changeEligibleCheckRequest.FlowType == Convert.ToString(FlowType.CHECKINSDC))
                {
                    try
                    {
                        //United.Refunds.Common.Models.Response.EligibilityPath
                        dynamic dyncslresponseobj = JsonConvert.DeserializeObject(cslResponse);
                        reservationdetails = JsonConvert.DeserializeObject
                            <United.Service.Presentation.ReservationResponseModel.ReservationDetail>
                            (Newtonsoft.Json.JsonConvert.SerializeObject(dyncslresponseobj["ReservationDetail"]));

                        if (reservationdetails != null)
                        {
                            //reservationdetails.Detail?.FlightSegments?.ForEach(seg =>
                            //{
                            //    if (seg != null)
                            //    {
                            //        seg.EstimatedArrivalTime
                            //        = string.IsNullOrEmpty(seg.EstimatedArrivalTime)
                            //        ? DateTime.Now.ToString() : seg.EstimatedArrivalTime;

                            //        if (seg.FlightSegment != null)
                            //        {
                            //            seg.FlightSegment.ArrivalDateTime
                            //            = string.IsNullOrEmpty(seg.FlightSegment.ArrivalDateTime)
                            //            ? DateTime.Now.ToString() : seg.FlightSegment.ArrivalDateTime;

                            //        }
                            //    }
                            //    seg.TripNumber = Convert.ToString(seg.LOFNumber);
                            //});


                            await _sessionHelperService.SaveSession<ReservationDetail>(reservationdetails, _headers.ContextValues.SessionId, new List<string>() { _headers.ContextValues.SessionId, reservationdetails.GetType().FullName }, reservationdetails.GetType().FullName).ConfigureAwait(false);

                            var retrieveRequestData = new RetrieveRequestData(_configuration, _dynamoDBService, _headers, _featureSettings);
                            response.PnrSegment =await retrieveRequestData.GetAllSegments(changeEligibleCheckRequest, reservationdetails);
                            response.PnrTravelers = EligibilityCheckHelper.GetAllPassangers(changeEligibleCheckRequest, reservationdetails);
                            response.AwardTravel = EligibilityCheckHelper.CheckForAward(changeEligibleCheckRequest, reservationdetails);
                        }
                    }
                    catch { }
                }
                else
                {
                    //TODO : To BYPASS ATRE Check when miles & money - Lower env only 
                    if (changeEligibleCheckRequest.OverrideATREEligible)
                    {
                        if (!response.PathEligible
                            && string.Equals(response.FailedRule, "ATRE", StringComparison.OrdinalIgnoreCase))
                        {
                            response.PathEligible = true;
                            response.FailedRule = string.Empty;
                        }
                    }

                    if (EligibilityCheckHelper.IsOTFOfferEligible(response))
                    {
                        response.PathEligible = false;
                    }
                    else
                    {
                        response.OnTheFlyEligibility = new OnTheFlyEligibility { OfferEligible = false };
                    }

                    if (response.PathEligible)
                    {
                        List<LogEntry> logEntries = new List<LogEntry>();
                        string token = cslToken;
                        var tupleRes= await GetPnrDetailsFromCSL
                            (changeEligibleCheckRequest.TransactionId, changeEligibleCheckRequest.RecordLocator,
                            changeEligibleCheckRequest.LastName, changeEligibleCheckRequest.Application.Id,
                            changeEligibleCheckRequest.Application.Version.Major, "CSLPNRRetrivel - ChangeEligibileCheck",
                             token, true);
                        string pnrRetrival = tupleRes.jsonResponse;
                        token = tupleRes.token;

                        reservationdetails = DataContextJsonSerializer.DeserializeUseContract<ReservationDetail>(pnrRetrival);

                        if (!_configuration.GetValue<bool>("DisableInfantAwardFlowFix") && IsAward(response?.ReservationDetail?.Detail?.Type))
                        {
                            var travelers = response?.ReservationDetail?.Detail?.Travelers;
                            bool isInfantExists = false;
                            if (travelers?.Count > 0)
                            {
                                foreach (var traveler in travelers)
                                {
                                    if (traveler.Person.InfantIndicator.Equals("True", StringComparison.OrdinalIgnoreCase)
                                        && traveler.Person.Type.Equals("INF", StringComparison.OrdinalIgnoreCase))
                                    {
                                        isInfantExists = true;
                                    }
                                }
                            }

                            if (isInfantExists)
                            {
                                if (response.ReservationDetail.Detail.Prices != null && response.ReservationDetail.Detail.Prices.Any())
                                {
                                    reservationdetails.Detail.Prices = new System.Collections.ObjectModel.Collection<Service.Presentation.PriceModel.Price>();
                                    reservationdetails.Detail.Prices = response.ReservationDetail.Detail.Prices;
                                }
                            }
                        }

                        await _sessionHelperService.SaveSession<ReservationDetail>
                            (reservationdetails, _headers.ContextValues.SessionId, new List<string>() { _headers.ContextValues.SessionId, reservationdetails.GetType().FullName }, reservationdetails.GetType().FullName).ConfigureAwait(false);
                    }
                }
                if (await _featureSettings.GetFeatureSettingValue("RemoveRemarksVDPVulnerability").ConfigureAwait(false))
                {
                    if (response?.ReservationDetail?.Detail?.Remarks != null)
                    {
                        response.ReservationDetail.Detail.Remarks = null; //VDP-214
                    }
                }
                if (response.PathEligible)
                {
                    if (_configuration.GetValue<bool>("PreventUNMRinChangePath"))
                    {
                        isUnaccompaniedMinor = EligibilityCheckHelper.CheckUnaccompaniedMinorAvailable(reservationdetails);
                        if (isUnaccompaniedMinor) response.FailedRule = "ATRE";
                    }
                }

                if ((response.SameDayChangeEligible && !isSameDayChangeVersion) || (response.ExceptionPolicyEligible && !isExceptionPolicyVersion))
                {
                    response.PathEligible = false;
                }

                if (_configuration.GetValue<bool>("ChangeEligibilityByPassFlag"))
                {
                    response.PathEligible = true;
                }

                if (response.PathEligible && !EBulkMAppReshopVersionCheckV2(changeEligibleCheckRequest.IsEbulkCatalogOn, reservationdetails))
                {
                    response.PathEligible = false;
                    response.FailedRule = "IsNotExecutiveBulkTicket";
                    response.ReservationDetail.PNRChangeEligibility.IsPolicyEligible = "True";
                }

                if (changeEligibleCheckRequest.FlowType != Convert.ToString(FlowType.CHECKINSDC))
                {
                    SetupRedirectURI(response,reservationdetails, changeEligibleCheckRequest);
                }

                bool eligibleCatalogValues = changeEligibleCheckRequest.CatalogValues != null && changeEligibleCheckRequest.CatalogValues.Count > 0 && IsEnableSaveTriptoMyAccount(changeEligibleCheckRequest?.CatalogValues);

                if (eligibleCatalogValues)
                {
                    var cslReservationResponse = await _sessionHelperService.GetSession<ReservationDetail>
                 (_headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName, new List<string>() { _headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName }).ConfigureAwait(false);
                    if (response.FailedRule == "ATRE")
                    {
                        throw new MOBUnitedException(JsonConvert.SerializeObject(await GetEligibilityReasonsCode(cslReservationResponse, cslToken)), response.FailedRule);
                    }
                    else if (response.FailedRule == "IsCurrencyUsd,IsUSPointOfSale")
                    {
                        response.InEligibilityReasons = await GetEligibilityReasonsCode(cslReservationResponse, cslToken);
                        if (response.InEligibilityReasons != null && response.InEligibilityReasons.Message != null)
                            response.IsShowChangeEligible = true;
                    }
                }
                else
                {
                    if (response.FailedRule == "ATRE")
                    {
                        throw new MOBUnitedException(response.FailedRule);
                    }
                }

                //Failed rule empty and partially flown = add your own failed rule
                if (await _featureSettings.GetFeatureSettingValue("EnablePartiallyFlownReshopChanges").ConfigureAwait(false)) {
                    bool partiallyFlown = Convert.ToBoolean(response?.Characteristics?.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.Code) && c.Code.Equals("IsPartiallyUsedTicket", StringComparison.OrdinalIgnoreCase))?.Value);
                    bool isCheckedIn = Convert.ToBoolean(response?.Characteristics?.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.Code) && c.Code.Equals("IsCheckedIn", StringComparison.OrdinalIgnoreCase))?.Value);

                    if (partiallyFlown && string.IsNullOrEmpty(response.FailedRule))
                    {
                        if (isCheckedIn)
                        {
                            response.FailedRule = _configuration.GetValue<string>("PartiallyFlownFailedRuleMobileCheckedIn");
                        }
                        else {
                            response.FailedRule = _configuration.GetValue<string>("PartiallyFlownFailedRuleMobile");
                        }
                    }
                }
                if (await _featureSettings.GetFeatureSettingValue("EbulkFailedRuleReshopLogging")) {
                    bool isEbulkPnr = Convert.ToBoolean(response?.Characteristics?.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.Code) && c.Code.Equals("IsExecutiveBulkTicket", StringComparison.OrdinalIgnoreCase))?.Value);
                    if (isEbulkPnr && string.IsNullOrEmpty(response.FailedRule)) {
                        response.FailedRule = _configuration.GetValue<string>("EbulkFailedRule");
                    }
                }
                if (await _featureSettings.GetFeatureSettingValue("IsBookingSourceValidReshopLogging")) {
                    bool isOnlineTravelAgencyBooking = Convert.ToBoolean(response?.Characteristics?.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.Code) && c.Code.Equals("IsOnlineTravelAgency", StringComparison.OrdinalIgnoreCase))?.Value);
                    bool isAgencyBooking = Convert.ToBoolean(response?.Characteristics?.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.Code) && c.Code.Equals("IsAgencyBooking", StringComparison.OrdinalIgnoreCase))?.Value);
                    bool isGDSPnr = Convert.ToBoolean(response?.Characteristics?.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.Code) && c.Code.Equals("IsGDSCorporatePNR", StringComparison.OrdinalIgnoreCase))?.Value);
                    bool isNDCPnr = Convert.ToBoolean(response?.Characteristics?.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.Code) && c.Code.Equals("IsNDCBooking", StringComparison.OrdinalIgnoreCase))?.Value);

                    if (string.IsNullOrEmpty(response.FailedRule)) {
                        if (isGDSPnr) {
                            response.FailedRule = _configuration.GetValue<string>("GDSFailedRule");
                        }
                        else if (isNDCPnr)
                        {
                            response.FailedRule = _configuration.GetValue<string>("NDCFailedRule");
                        }
                        else if (isAgencyBooking && !isOnlineTravelAgencyBooking)
                        {
                            response.FailedRule = _configuration.GetValue<string>("NonOTAFailedRule");
                            
                        }
                        else if (isAgencyBooking && isOnlineTravelAgencyBooking)
                        {
                            response.FailedRule = _configuration.GetValue<string>("OTAFailedRule");
                        }
                        if (await _featureSettings.GetFeatureSettingValue("NonOTAMessageReshop") && isAgencyBooking && !isOnlineTravelAgencyBooking)
                        {
                            string[] nonOTAReshopBanner = GeneralHelper.SplitConcatenatedConfigValue(_configuration, "NonOTAReshopBanner", "||");
                            MOBPNRAdvisory nonOTAAdvisoryMessage = new MOBPNRAdvisory
                            {
                                Header = nonOTAReshopBanner[0],
                                Body = nonOTAReshopBanner[1],
                                ContentType = ContentType.FFCRRESIDUAL,
                                AdvisoryType = AdvisoryType.INFORMATION,
                                IsBodyAsHtml = true,
                                IsDefaultOpen = false,
                            };
                            (response.AdvisoryInfo ??= new List<MOBPNRAdvisory>()).Add(nonOTAAdvisoryMessage);
                            await _sessionHelperService.SaveSession<List<MOBPNRAdvisory>>(response?.AdvisoryInfo, _headers?.ContextValues?.SessionId, new List<string> { _headers?.ContextValues?.SessionId, response?.AdvisoryInfo?.GetType()?.FullName }, response?.AdvisoryInfo?.GetType()?.FullName).ConfigureAwait(false);
                        }
                    }                    
                }
                return response;
            }

            return response;
        }

        public async Task<EligibilityReasonsCode> GetEligibilityReasonsCode(ReservationDetail response, string cslToken)
        {
            EligibilityReasonsCode eligibilityReasonsCode = new EligibilityReasonsCode();

            if (!string.IsNullOrEmpty(response?.PNRChangeEligibility?.IsChangeEligible) && !Convert.ToBoolean(response?.PNRChangeEligibility?.IsChangeEligible))
            {
                string eligibiltyCode = response?.PNRChangeEligibility?.InEligibilityReasons != null ? response?.PNRChangeEligibility?.InEligibilityReasons[0]?.Code : "";
                string responceSDLContent = await GetSDLCMSContentMesseges(cslToken, _headers.ContextValues.SessionId);
                var content = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(responceSDLContent);
                eligibilityReasonsCode = GetSDLMessageFromList(content?.Messages, eligibiltyCode);
            }
            return eligibilityReasonsCode;
        }

        public EligibilityReasonsCode GetSDLMessageFromList(List<CMSContentMessage> list, string eligibiltyCode)
        {
            EligibilityReasonsCode listOfMessages = new EligibilityReasonsCode();
            string title = "";
            if (!string.IsNullOrEmpty(eligibiltyCode) && Enum.IsDefined(typeof(InEligibilityReasonsCode), eligibiltyCode))
                title = _configuration.GetValue<string>("EligibilityReasonsSDLTitle") + eligibiltyCode;
            else
                title = _configuration.GetValue<string>("EligibilityReasonsSDLTitle") + "GENERIC";

            list?.Where(l => l.Title.ToUpper().Equals(title.ToUpper()))?.ForEach(i =>
            {
                listOfMessages.Title = i.Headline;
                listOfMessages.Message = i.ContentFull;
                listOfMessages.ActionButtons = new List<ActionDetails>();
                listOfMessages.ActionButtons.Add(GetActionDetails(eligibiltyCode));
            });

            return listOfMessages;
        }

        public ActionDetails GetActionDetails(string eligibiltyCode)
        {
            ActionDetails obj = new ActionDetails();

            switch (((!string.IsNullOrEmpty(eligibiltyCode) && Enum.IsDefined(typeof(InEligibilityReasonsCode), eligibiltyCode)) ? Enum.Parse(typeof(InEligibilityReasonsCode), eligibiltyCode) : 0))
            {
                case InEligibilityReasonsCode.OUT_OF_SYNC_PNR:
                case InEligibilityReasonsCode.ECD_PROMO:
                case InEligibilityReasonsCode.PET_PNR:
                case InEligibilityReasonsCode.HAS_INTL_INF:
                case InEligibilityReasonsCode.UNSUPRT_CURNCY:
                case InEligibilityReasonsCode.FLTSEG_NOT_HK:
                    obj = new ActionDetails() { ActionText = "Contact us", ActionURL = "" };
                    break;
                case InEligibilityReasonsCode.BE_PRICE_PNR:
                case InEligibilityReasonsCode.MM_24FLEX_BE:
                    obj = new ActionDetails() { ActionText = "Cancel your trip", ActionURL = "" };
                    break;
                case InEligibilityReasonsCode.GROUP_PNR_PARTIAL:
                    obj = new ActionDetails() { ActionText = "Contact us", ActionURL = _configuration.GetValue<string>("EligibilityGROUP_PNR_PARTIAL") };
                    break;
                case InEligibilityReasonsCode.GROUP_PNR:
                case InEligibilityReasonsCode.BULK_PNR:
                case InEligibilityReasonsCode.TKT_NOT_016:
                    obj = new ActionDetails() { ActionText = "", ActionURL = "" };
                    break;
                default:
                    obj = new ActionDetails() { ActionText = "Contact us", ActionURL = "" };
                    break;
            }

            return obj;
        }

        public static T Deserialize<T>(Stream stream)
        {
            if (stream == null || stream.CanRead == false)
            {
                return default;
            }
            using var sr = new StreamReader(stream);
            using var jr = new JsonTextReader(sr);
            var ntSerializer = new Newtonsoft.Json.JsonSerializer();
            ntSerializer.Converters.Add(new TimeSpanConverter());
            var response = ntSerializer.Deserialize<T>(jr);
            return response;
        }
        private async Task<string> MakeHTTPCallAndLogIt(string sessionId, string deviceId, string action, MOBApplication application, string token, string path, string jsonRequest, bool isGetCall, bool isXMLRequest = false)
        {
            var jsonResponse = string.Empty;

            if (isGetCall)
            {
                jsonResponse = await _refundService.GetEligibleCheck<MOBRESHOPChangeEligibilityResponse>(token, sessionId, path).ConfigureAwait(false);
            }
            else
            {
                jsonResponse = await _refundService.PostEligibleCheck<MOBRESHOPChangeEligibilityResponse>(token, sessionId, path, jsonRequest).ConfigureAwait(false);
            }
            return jsonResponse;

        }

        private void SetupRedirectURI(MOBRESHOPChangeEligibilityResponse refundReservationResponse,ReservationDetail cslReservation, MOBRESHOPChangeEligibilityRequest request)
        {
            if (!refundReservationResponse.PathEligible)
            {
                refundReservationResponse.Exception = new MOBException("8999", _configuration.GetValue<string>("Change-IneligibleMessage"));
            }
        }

        private async Task<MOBSHOPShopRequest> CreateReshopMOBSHOPShopRequest(
           MOBRESHOPChangeEligibilityRequest request,
           MOBRESHOPChangeEligibilityResponse response,
           ReservationDetail reservationDetail)
        {
            var reshoprequest = new MOBSHOPShopRequest
            {
                RecordLocator = request.RecordLocator,
                LastName = request.LastName,
                Application = request.Application,
                LanguageCode = request.LanguageCode,
                SessionId = request.SessionId,
                TransactionId = request.TransactionId,
                IsReshop = true,
                IsReshopChange = true,
                DeviceId = request.DeviceId,
                MaxNumberOfStops = 2,
                MaxNumberOfTrips = 25,
                FareType = "lf",
                IsELFFareDisplayAtFSR = false,
                IsExpertModeEnabled = true,
                PageIndex = 1,
                PageSize = 25,
            };

            reshoprequest.HashPinCode = request.HashPinCode;
            reshoprequest.Application = request.Application;

            //Add Trips
            var reshopTrips = new List<MOBSHOPTripBase>();
            bool isAward = (request?.ReshopRequest != null) ? request.ReshopRequest.AwardTravel : false;
            var retrieveRequestData = new RetrieveRequestData(_configuration, _dynamoDBService, _headers, _featureSettings);
            List<MOBTrip> alltrips = await retrieveRequestData.GetAllTrips(reservationDetail, isAward);
            bool setChangeFlag = true;

            //New try

            if (alltrips != null && alltrips.Any())  //List<MOBTrip> trips = alltrips.Where(x => x.Index == 1).ToList();
            {
                alltrips.ForEach(trip =>
                {
                    var reshopTrip = new MOBSHOPTripBase
                    {
                        DepartDate = trip.DepartureTime,
                        Destination = trip.Destination,
                        //ArrivalDate = trip.ArrivalTime,
                        Origin = trip.Origin,
                        Index = trip.Index,
                        Cabin = trip.CabinType,
                        UseFilters = false,
                        SearchNearbyDestinationAirports = false,
                        SearchNearbyOriginAirports = false,
                        //OriginAllAirports = 0,
                        //DestinationAllAirports = 0,
                    };


                    MOBSHOPTripBase requestedtrip = null;
                    if (request?.ReshopRequest?.Trips != null && request.ReshopRequest.Trips.Any())
                    {
                        requestedtrip = request.ReshopRequest.Trips.FirstOrDefault
                        (x => x.Origin == trip.Origin && x.Destination == trip.Destination);
                    }
                    if (requestedtrip != null)
                    {
                        if (!string.IsNullOrEmpty(requestedtrip.DepartDate))
                        {
                            reshopTrip.DepartDate = requestedtrip.DepartDate;
                        }
                        reshopTrip.ChangeType = MOBSHOPTripChangeType.ChangeFlight;
                        setChangeFlag = false;
                    }
                    else
                    {
                        if (setChangeFlag)
                        {
                            reshopTrip.ChangeType = MOBSHOPTripChangeType.ChangeFlight;
                            setChangeFlag = false;
                        }
                        else
                        {
                            reshopTrip.ChangeType = MOBSHOPTripChangeType.NoChange;
                        }
                    }

                    reshoprequest.Trips
                   = (reshoprequest.Trips == null) ? new List<MOBSHOPTripBase>() : reshoprequest.Trips;
                    reshoprequest.Trips.Add(reshopTrip);
                });
            }

            request.ReshopRequest = (request.ReshopRequest == null)
               ? new MOBSHOPShopRequest() : request.ReshopRequest;

            request.ReshopRequest.Trips = (request.ReshopRequest.Trips == null)
                ? new List<MOBSHOPTripBase>() : request.ReshopRequest.Trips;

            request.ReshopRequest.Trips = reshoprequest.Trips;

            //Set all segments.
            reshoprequest.ReshopSegments
                = (reshoprequest.ReshopSegments == null) ? new List<MOBPNRSegment>() : reshoprequest.ReshopSegments;

            reshoprequest.ReshopSegments = response.PnrSegment;

            //Set all Pax
            reshoprequest.ReshopTravelers
                = (reshoprequest.ReshopTravelers == null) ? new List<MOBPNRPassenger>() : reshoprequest.ReshopTravelers;

            reshoprequest.ReshopTravelers = response.PnrTravelers;

            if (reshoprequest.ReshopTravelers != null && reshoprequest.ReshopTravelers.Any())
            {
                reshoprequest.NumberOfAdults = reshoprequest.ReshopTravelers.Count();

                #region "traveler type / Pax pricing type" - commented

                //string[] paxtype = { "ADT", "INS", "C04", "C11", "C14", "C17", "SNR", "INF" };

                //reshoprequest.NumberOfAdults = reshoprequest.ReshopTravelers.Where
                //    (item => item.PricingPaxType == "ADT").Count();

                //reshoprequest.NumberOfSeniors = reshoprequest.ReshopTravelers.Where
                //    (item => item.PricingPaxType == "SNR").Count();

                //reshoprequest.NumberOfChildren2To4 = reshoprequest.ReshopTravelers.Where
                //    (item => item.PricingPaxType == "C04").Count();

                //reshoprequest.NumberOfChildren5To11 = reshoprequest.ReshopTravelers.Where
                //    (item => item.PricingPaxType == "C11").Count();

                //reshoprequest.NumberOfChildren12To17
                //    = reshoprequest.ReshopTravelers.Where(item => item.PricingPaxType == "C14").Count()
                //    + reshoprequest.ReshopTravelers.Where(item => item.PricingPaxType == "C17").Count();

                //reshoprequest.NumberOfInfantOnLap
                //    = reshoprequest.ReshopTravelers.Where(item => item.PricingPaxType == "INF").Count();

                //reshoprequest.NumberOfInfantWithSeat = reshoprequest.ReshopTravelers.Where
                //    (item => item.PricingPaxType == "INS").Count();

                //var differentpaxtype = reshoprequest.ReshopTravelers.Where
                //    (item => !paxtype.Contains(item.PricingPaxType));

                //if (differentpaxtype != null && differentpaxtype.Any())
                //{
                //    reshoprequest.NumberOfAdults = reshoprequest.NumberOfAdults 
                //        + differentpaxtype.Where(item => item.TravelerTypeCode == "ADT").Count();                    

                //    reshoprequest.NumberOfSeniors = reshoprequest.NumberOfSeniors 
                //        + differentpaxtype.Where(item => item.TravelerTypeCode == "SNR").Count();

                //    reshoprequest.NumberOfChildren2To4 = reshoprequest.NumberOfChildren2To4 
                //        + differentpaxtype.Where(item => item.TravelerTypeCode == "C04").Count();

                //    reshoprequest.NumberOfChildren5To11 = reshoprequest.NumberOfChildren5To11 
                //         + differentpaxtype.Where(item => item.TravelerTypeCode == "C11").Count();

                //    reshoprequest.NumberOfChildren12To17 = reshoprequest.NumberOfChildren12To17
                //        + differentpaxtype.Where(item => item.TravelerTypeCode == "C14").Count()
                //        + differentpaxtype.Where(item => item.TravelerTypeCode == "C17").Count();

                //    reshoprequest.NumberOfInfantOnLap = reshoprequest.NumberOfInfantOnLap
                //        + differentpaxtype.Where(item => item.TravelerTypeCode == "INF").Count();

                //    reshoprequest.NumberOfInfantWithSeat = reshoprequest.NumberOfInfantWithSeat 
                //        + differentpaxtype.Where(item => item.TravelerTypeCode == "INS").Count();
                //}

                #endregion

                reshoprequest.EmployeeDiscountId
                    = (reshoprequest.ReshopTravelers.Any(x => x.PricingPaxType == "EMP")) ? "EMP" : String.Empty;
            }

            if (response.AwardTravel)
            {
                reshoprequest.AwardTravel = response.AwardTravel;
                reshoprequest.MileagePlusAccountNumber = request.MileagePlusNumber;
                //reshoprequest.MileagePlusAccountNumber = response.SponsorMileagePlus;
            }

            if (request.ReshopRequest != null && request.ReshopRequest.IsCorporateBooking)
            {
                reshoprequest.IsCorporateBooking = request.ReshopRequest.IsCorporateBooking;

                if (request.ReshopRequest.MOBCPCorporateDetails != null)
                {
                    reshoprequest.MOBCPCorporateDetails = new MOBCorporateDetails
                    {
                        CorporateCompanyName = request.ReshopRequest.MOBCPCorporateDetails.CorporateCompanyName,
                        CorporateTravelProvider = request.ReshopRequest.MOBCPCorporateDetails.CorporateTravelProvider,
                        DiscountCode = request.ReshopRequest.MOBCPCorporateDetails.DiscountCode,
                        NoOfTravelers = request.ReshopRequest.MOBCPCorporateDetails.NoOfTravelers,
                    };
                }
            }
            return reshoprequest;
        }

        #region FLIGHT RESERVAtion
        private async Task<(string jsonResponse, string token)> GetPnrDetailsFromCSL(string transactionId, string recordLocator, string lastName, int applicationId, string appVersion, string actionName, string token, bool usedRecall = false)
        {
            var request = new RetrievePNRSummaryRequest();

            if (!usedRecall)
            {
                request.Channel = _configuration.GetValue<string>("ChannelName");
                request.IsIncludeETicketSDS = _configuration.GetValue<string>("IsIncludeETicketSDS");
                request.IsIncludeFlightRange = _configuration.GetValue<string>("IsIncludeFlightRange");
                request.IsIncludeFlightStatus = _configuration.GetValue<string>("IsIncludeFlightStatus");
                request.IncludeManageResDetails = _configuration.GetValue<string>("IncludeManageResDetails");
                request.IsUpgradeDetails = _configuration.GetValue<string>("IsUpgradeDetails");
                if (_configuration.GetValue<bool>("EnablePCUWaitListPNRManageRes"))
                {
                    request.IsUpgradeDetailsWithEMD = _configuration.GetValue<string>("IsUpgradeDetailsWithEMD");
                }
                request.IsIncludePNRChangeEligibility = _configuration.GetValue<string>("IsIncludePNRChangeEligibility");
                request.IsIncludeLMX = _configuration.GetValue<string>("IsIncludeLMX");
                request.IsIncludePNRDB = _configuration.GetValue<string>("IsIncludePNRDB");
                request.IsIncludeSegmentDuration = _configuration.GetValue<string>("IsIncludeSegmentDuration");
                request.ConfirmationID = recordLocator.ToUpper();
                request.LastName = lastName;
                request.PNRType = string.Empty;
                request.FilterHours = _configuration.GetValue<string>("FilterHours");
                request.IsIncludeChangeFee = _configuration.GetValue<bool>("IsIncludeChangeFee");
                if (_configuration.GetValue<bool>("IsIncludeTravelWaiverDetail"))
                {
                    request.IsIncludeTravelWaiverDetail = _configuration.GetValue<bool>("IsIncludeTravelWaiverDetail");
                }
            }
            else
            {
                request.Channel = _configuration.GetValue<string>("ChannelName");
                request.IsIncludeETicketSDS = _configuration.GetValue<string>("IsIncludeETicketSDS");
                request.IsIncludeFlightRange = _configuration.GetValue<string>("IsIncludeFlightRange");
                request.IsIncludeFlightStatus = _configuration.GetValue<string>("IsIncludeFlightStatus");
                request.IsIncludeLMX = _configuration.GetValue<string>("IsIncludeLMX");
                request.IsIncludePNRDB = _configuration.GetValue<string>("IsIncludePNRDB");
                request.IsIncludeSegmentDuration = _configuration.GetValue<string>("IsIncludeSegmentDuration");
                request.ConfirmationID = recordLocator.ToUpper();
                request.LastName = lastName;
                request.PNRType = string.Empty;
            }

            var jsonResponse =await RetrievePnrDetailsFromCsl(applicationId, transactionId, request).ConfigureAwait(false);
            return (jsonResponse,token);
        }

        private async Task<string> RetrievePnrDetailsFromCsl(int applicationId, string transactionId, RetrievePNRSummaryRequest request)
        {
            var jsonRequest = System.Text.Json.JsonSerializer.Serialize<RetrievePNRSummaryRequest>(request);

            string token = await _dPService.GetAnonymousToken(applicationId, _headers.ContextValues.DeviceId, _configuration);

            var jsonResponse = string.Empty;
            jsonResponse = await _pNRRetrievalService.PNRRetrieval(token, jsonRequest, transactionId, "PNRRetrieval").ConfigureAwait(false);
            return jsonResponse;
        }

        #endregion


        public async Task<MOBRESHOPChangeEligibilityResponse> ChangeEligibleCheck(MOBRESHOPChangeEligibilityRequest request)
        {
            var response = new MOBRESHOPChangeEligibilityResponse();
            Reservation persistedReservation = null;
            EligibilityResponse eligibility = null;
            Session session = null;

            try
            {
                if (_configuration.GetValue<bool>("EnableReshopChangeFeeForceUpdate"))
                {
                    new ForceUpdateVersion(_configuration).ForceUpdateForNonSupportedVersion
                            (request.Application.Id, request.Application.Version.Major, FlowType.RESHOP);
                }

                eligibility = new EligibilityResponse();

                eligibility = await _sessionHelperService.GetSession<EligibilityResponse>(_headers.ContextValues.SessionId, (new EligibilityResponse()).ObjectName, new List<string>() { _headers.ContextValues.SessionId, (new EligibilityResponse()).ObjectName }).ConfigureAwait(false);

                if (eligibility != null)
                {
                    if (!request.OverrideATREEligible)
                    {
                        bool enableIsChangeEligibleWith24hrs = (request.CatalogValues != null && request.CatalogValues.Count > 0 && IsEnableSaveTriptoMyAccount(request?.CatalogValues)) ? eligibility.Is24HrFlexibleBookingPolicy : true;
                        if (_configuration.GetValue<bool>("EnableReshopMilesMoney")
                            && eligibility.IsMilesAndMoney && (await _featureSettings.GetFeatureSettingValue("EnableIsChangeEligible").ConfigureAwait(false) ? !eligibility.IsChangeEligible : !eligibility.IsATREEligible)
                            && !eligibility.IsIBE && !eligibility.IsIBELite && !eligibility.IsElf && enableIsChangeEligibleWith24hrs)
                        {
                            string[] stringarray = GeneralHelper.SplitConcatenatedConfigValue(_configuration, "MilesMoneyReshopEligibility", "||");
                            if (stringarray != null && stringarray.Length >= 2)
                            {
                                response.PathEligible = true;
                                response.Exception = new MOBException();
                                response.Exception.Code = "9999";
                                response.Exception.Message = stringarray[1];
                                return response;
                            }
                        }
                    }

                    if (!_configuration.GetValue<bool>("Disable_RESHOP_JSENONCHANGEABLEFARE"))
                    {
                        if (eligibility.IsJSENonChangeableFare)
                        {
                            response.PathEligible = true;
                            response.Exception = new MOBException("9999", _configuration.GetValue<string>("RESHOP_JSE_NONCHANGEABLE_ERRMSG"));
                            return response;
                        }
                    }

                    if (eligibility.IsCorporateBooking && _configuration.GetValue<bool>("DisableCorporateReshopChange"))
                    {
                        var vendorNameArray = ConfigUtility.GetListFrmPipelineSeptdConfigString("ReshopChangeIneligibleVendorNames");
                        if (vendorNameArray != null && vendorNameArray.Any())
                        {
                            if (vendorNameArray.Contains(eligibility.CorporateVendorName))
                            {
                                response.PathEligible = true;
                                response.Exception = EligibilityCheckHelper.SetCustomMobExceptionMessage
                                    (string.Format(_configuration.GetValue<string>("CorporateReshopChangeWarningMsg"), Environment.NewLine), "9999");
                                return response;
                            }
                        }
                    }//IsCorporateBooking

                    if (eligibility.IsAgencyBooking)
                    {
                        if (!EligibilityCheckHelper.AllowAgencyToChangeCancel(eligibility.AgencyName, requesttype: "CHANGE"))
                        {
                            if (_configuration.GetValue<bool>("EnableAgencyChangeMessage"))
                            {
                                //string msgtypekey = (!eligibility.IsCheckinEligible) ? "changeAgencyCheckInIneligible" : "changeAgencyCheckInEligible";
                                string msgtypekey = "changeAgencyCheckInEligible";
                                var agencymsginfo = await new ShuttleOfferInfo(_configuration, _dynamoDBService,_legalDocumentsForTitlesService, _headers).GetDBDisplayContent("Reshop_ChangeCancelMsg_Content");
                                var msg = (agencymsginfo != null) ? agencymsginfo.FirstOrDefault
                                    (x => string.Equals(x.Id, msgtypekey, StringComparison.OrdinalIgnoreCase)) : null;
                                if (msg != null)
                                {
                                    if (eligibility.IsCheckinEligible &&
                                        EligibilityCheckHelper.EnableAgencyChangeMessage
                                        (request.Application.Id, request.Application.Version.Major))
                                    {
                                        string errorCode = EligibilityCheckHelper.GetEnumValue(MOBSHOPResponseStatus.ReshopAgencyCheckinEligible);
                                        response.PathEligible = false;
                                        response.ResponseStatusItem = new MOBSHOPResponseStatusItem
                                        {
                                            Status = MOBSHOPResponseStatus.ReshopAgencyCheckinEligible,
                                            StatusMessages = new List<MOBItem> { new MOBItem { Id = errorCode, CurrentValue = msg.CurrentValue } }
                                        };
                                        return response;
                                    }
                                    else
                                    {
                                        response.PathEligible = true;
                                        response.Exception = EligibilityCheckHelper.SetCustomMobExceptionMessage(msg.CurrentValue, string.Empty);
                                        return response;
                                    }//Version Check                        
                                }
                            }

                        }//Exclude NDC
                    } // IsAgencyBooking
                }


                if (!string.IsNullOrEmpty(request.RecordLocator))
                {
                    var regex = new Regex(@"\b^[a-zA-Z][a-zA-Z0-9]{5}\b*$");
                    if (!regex.IsMatch(request.RecordLocator))
                    {
                        response.Exception = new MOBException();
                        response.Exception.Code = "9999";
                        response.Exception.Message = _configuration.GetValue<string>("MPValidationErrorMessage").ToString();
                        return response;
                    }
                }

                //Book With Credit 
                if (!_configuration.GetValue<bool>("EnableBookWithCredit") && !string.IsNullOrEmpty(request.SessionId))
                {
                    MOBFOPLookUpTravelCreditRequest mobLookupRequest = new MOBFOPLookUpTravelCreditRequest();
                    mobLookupRequest.LastName = request.LastName;
                    mobLookupRequest.PinOrPnr = request.RecordLocator;
                    mobLookupRequest.MileagePlusNumber = request.MileagePlusNumber;
                    mobLookupRequest.HashPin = request.HashPinCode;
                    mobLookupRequest.Application = request.Application;
                    mobLookupRequest.DeviceId = request.DeviceId;

                    await _sessionHelperService.SaveSession<MOBFOPLookUpTravelCreditRequest>(mobLookupRequest, _headers.ContextValues.SessionId, new List<string>() { request.SessionId, (new MOBFOPLookUpTravelCreditRequest()).ObjectName }, (new MOBFOPLookUpTravelCreditRequest()).ObjectName).ConfigureAwait(false);
                }

                session = await _shoppingSessionHelper.GetBookingFlowSession(request.SessionId, false);

                if (ConfigUtility.EnableUMNRInformation(request.Application.Id, request.Application.Version.Major))
                {
                    var pnrSession = await _sessionHelperService.GetSession<MOBPNR>(_headers.ContextValues.SessionId, (new MOBPNR()).ObjectName, new List<string>() { _headers.ContextValues.SessionId, (new MOBPNR()).ObjectName }).ConfigureAwait(false);
                    if (pnrSession != null && pnrSession.IsUnaccompaniedMinor)
                    {
                        response.Exception = new MOBException();
                        response.ResponseStatusItem = new MOBSHOPResponseStatusItem();
                        response.Exception.Message += " " + request.RecordLocator;
                        response.ResponseStatusItem.Status = MOBSHOPResponseStatus.ReshopUnableToChange;
                        response.ResponseStatusItem.StatusMessages =await new MPDynamoDB(_configuration, _dynamoDBService, _legalDocumentsForTitlesService, _headers).GetMPPINPWDTitleMessages(new List<string> { "RTI_CHANGE_ELIGIBLE_UNABLE_COMPLETE_REQUEST" });
                        return response;
                    }
                }

                bool awardVersionCheck = EligibilityCheckHelper.IsVersionAllowAward(request.Application.Id,
                                                request.Application.Version,
                                                _configuration.GetValue<string>("AndroidAwardVersion"),
                                                _configuration.GetValue<string>("iPhoneAwardVersion"));

                bool sameDayChangeVersionCheck = EligibilityCheckHelper.IsVersionAllowAward(request.Application.Id,
                                                        request.Application.Version,
                                                        _configuration.GetValue<string>("AndroidReshopSameDayChangeVersion"),
                                                        _configuration.GetValue<string>("iPhoneReshopSameDayChangeVersion"));
                bool exceptionPolicyVersionCheck = EligibilityCheckHelper.IsVersionAllowAward(request.Application.Id,
                                                        request.Application.Version,
                                                        _configuration.GetValue<string>("AndroidExceptionPolicyVersion"),
                                                        _configuration.GetValue<string>("iPhoneExceptionPolicyVersion"));


                response = await ReshoppingChangeEligibleCheck(request, awardVersionCheck, sameDayChangeVersionCheck, exceptionPolicyVersionCheck);

                //Book With Credit 
                if (!_configuration.GetValue<bool>("EnableBookWithCredit") && !string.IsNullOrEmpty(request.SessionId))
                {
                    response.BWCSessionId = request.SessionId; // Update the BWCSession ID to the response
                }

                if (eligibility != null && eligibility.IsReshopWithFutureFlightCredit
                   && response?.OnTheFlyEligibility != null && response.OnTheFlyEligibility.OfferEligible)
                {
                    response.ResponseStatusItem = new MOBSHOPResponseStatusItem
                    {
                        Status = MOBSHOPResponseStatus.ReshopOTFShopEligible,
                        StatusMessages = new List<MOBItem> { new MOBItem { Id = "RESHOP_REDIRECT_SHOPPING", CurrentValue = "OTF Eligible - Redirect to Shopping path" } }
                    };
                    response.SessionId = request.SessionId;
                    //response.Exception = new MOBException { Code = "7999", Message = "RESHOP_REDIRECT_SHOPPING", ErrMessage = "OTF - Redirect to Shopping path" };                    
                    return response;
                }
                var cslReservation = await _sessionHelperService.GetSession<ReservationDetail>
                  (_headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName, new List<string>() { _headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName }).ConfigureAwait(false);


                if (_configuration.GetValue<bool>("EnableEncryptedRedirectUrl"))
                {
                    response.RedirectURL
                                = (_configuration.GetValue<bool>("EnableTripDetailChangeRedirect3dot0Url"))
                                ? EligibilityCheckHelper.GetTripDetailRedirect3dot0Url(request.RecordLocator, request.LastName, ac: "EX", channel: "mobile", languagecode: "en/US")
                                : EligibilityCheckHelper.GetPNRRedirectUrl(request.RecordLocator, request.LastName, reqType: "EX");
                }
                else
                {
                    response.RedirectURL = "http://" + _configuration.GetValue<string>("DotComOneCancelURL") + "/web/en-US/apps/reservation/import.aspx?OP=1&CN=" +
                        request.RecordLocator +
                        "&LN=" +
                        request.LastName +
                        "&T=F&MobileOff=1";
                }

                if (_configuration.GetValue<bool>("ReshopEligiblityDontLoadReservationWhenExceptionInCSLResponse"))
                {
                    response.Reservation = new MOBSHOPReservation(_configuration,_cachingService);
                    persistedReservation = new Reservation();
                    if (response.PathEligible)
                    {
                        await BuildReservationObjectForReshopChange(request, cslReservation, eligibility.IsReshopWithFutureFlightCredit,response);
                        persistedReservation = await _sessionHelperService.GetSession<Reservation>(_headers.ContextValues.SessionId, persistedReservation.ObjectName, new List<string>() { _headers.ContextValues.SessionId, persistedReservation.ObjectName }).ConfigureAwait(false);
                    }
                }
                else
                {
                    if (response.PathEligible)
                        await BuildReservationObjectForReshopChange(request, cslReservation, eligibility.IsReshopWithFutureFlightCredit,response);
                    response.Reservation = new MOBSHOPReservation(_configuration,_cachingService);
                    persistedReservation = new Reservation();
                    persistedReservation = await _sessionHelperService.GetSession<Reservation>(_headers.ContextValues.SessionId, persistedReservation.ObjectName, new List<string>() { _headers.ContextValues.SessionId, persistedReservation.ObjectName }).ConfigureAwait(false);
                }
                persistedReservation.IsReshopChange = true;
                response.Reservation.IsReshopChange = true;
                response.AwardTravel = session.IsAward;


                if (cslReservation?.Detail?.Sponsor != null)
                {
                    response.SponsorMileagePlus
                        = cslReservation.Detail.Sponsor?.LoyaltyProgramMemberID;
                }
                else
                {
                    if (eligibility.IsCorporateBooking)
                    {
                        response.SponsorMileagePlus = cslReservation.Detail.Travelers?
                            .FirstOrDefault()?.LoyaltyProgramProfile?.LoyaltyProgramMemberID;
                    }
                }


                if (!response.PathEligible)
                {
                    if (EligibilityCheckHelper.EnableChangeWebSSO(request.Application.Id, request.Application.Version.Major))
                    {
                        if (!string.IsNullOrEmpty(request.MileagePlusNumber) && !string.IsNullOrEmpty(request.HashPinCode))
                        {
                            bool validWalletRequest = false;
                            string authToken = string.Empty;

                            var tupleRes= await new HashPin(_logger, _configuration, _validateHashPinService, _dynamoDBService, _headers, _featureSettings).ValidateHashPinAndGetAuthToken
                                (request.MileagePlusNumber, request.HashPinCode, request.Application.Id, request.DeviceId, request.Application.Version.Major, authToken);
                            validWalletRequest = tupleRes.returnValue;
                            authToken = tupleRes.validAuthToken;

                            if (!validWalletRequest)
                                throw new MOBUnitedException(_configuration.GetValue<string>("bugBountySessionExpiredMsg"));

                            if (validWalletRequest)
                            {
                                response.TransactionId = request.TransactionId;
                                response.LanguageCode = request.LanguageCode;

                                response.WebShareToken = _dPService.GetSSOTokenString(request.Application.Id, request.MileagePlusNumber, _configuration)?.ToString();
                                if (!String.IsNullOrEmpty(response.WebShareToken))
                                {
                                    response.WebSessionShareUrl = _configuration.GetValue<string>("DotcomSSOUrl");
                                    if (await _featureSettings.GetFeatureSettingValue("EnableRedirectURLUpdate").ConfigureAwait(false))
                                    {
                                        response.RedirectURL = $"{_configuration.GetValue<string>("NewDotcomSSOUrl")}?type=sso&token={response.WebShareToken}&landingUrl={response.RedirectURL}";
                                        response.WebSessionShareUrl = response.WebShareToken = string.Empty;
                                    }
                                }
                            }
                        }
                    }
                }

                response.Reservation.SessionId = request.SessionId;
                response.PnrTravelers = eligibility.Passengers;

                if (_configuration.GetValue<bool>("EnablePNRShuttleOfferAvailable"))
                {
                    var ewrOffer =await new ShuttleOfferInfo(_configuration, _dynamoDBService,_legalDocumentsForTitlesService, _headers).GetEWRShuttleOfferInformation();
                    if (ewrOffer != null)
                    {
                        if (response.ShuttleOffer == null) { response.ShuttleOffer = new List<MOBShuttleOffer>(); }
                        if (request.ShuttleOffer != null && request.ShuttleOffer.Any()) { ewrOffer.IsSelected = true; }
                        if (!EligibilityCheckHelper.IsChildTravelerAvailable(response.PnrTravelers))
                        {
                            ewrOffer.ChildCutoffAge = string.Empty;
                            ewrOffer.ChildWarningMessage = string.Empty;
                        }
                        response.ShuttleOffer.Add(ewrOffer);
                    }
                }

                if (EligibilityCheckHelper.EnableInfantInLapVersion(request.Application.Id, request.Application.Version.Major))
                {
                    if (eligibility.InfantInLaps != null && eligibility.InfantInLaps.Any())
                        response.PnrTravelers.AddRange(eligibility.InfantInLaps);
                }

                if (response.PathEligible && eligibility.IsCheckinEligible)
                {
                    if (!EligibilityCheckHelper.SupressPopupWhenBEIBEWithExceptionPolicy(eligibility))
                    {
                        if (_configuration.GetValue<bool>("EnableReshopCheckinEligibleMessage"))
                        {
                            if (EligibilityCheckHelper.EnableReshopCheckinEligibleMessage
                                (request.Application.Id, request.Application.Version.Major))
                            {
                                string msgtypekey = "changeCheckIneligible";
                                string errorCode = EligibilityCheckHelper.GetEnumValue(MOBSHOPResponseStatus.ReshopCheckinEligible);
                                var checkineligiblemsg = await new ShuttleOfferInfo(_configuration, _dynamoDBService,_legalDocumentsForTitlesService, _headers).GetDBDisplayContent("Reshop_ChangeCancelMsg_Content");
                                var msg = (checkineligiblemsg != null) ? checkineligiblemsg.FirstOrDefault
                                    (x => string.Equals(x.Id, msgtypekey, StringComparison.OrdinalIgnoreCase)) : null;
                                if (msg != null)
                                {
                                    response.ResponseStatusItem = new MOBSHOPResponseStatusItem
                                    {
                                        Status = MOBSHOPResponseStatus.ReshopCheckinEligible,
                                        StatusMessages = new List<MOBItem> { new MOBItem { Id = errorCode, CurrentValue = msg.CurrentValue } }
                                    };
                                }
                            }//Version Check
                        }
                    }
                }

                if (_configuration.GetValue<bool>("EnableReshopAdvisoryInfo"))
                {
                    MOBPNRAdvisory reshopnewtrip = null;
                    if (eligibility.IsSCChangeEligible)
                    {
                        reshopnewtrip = EligibilityCheckHelper.PopulateReshopAdvisoryContent("ReshopScheduleChangeNewTripAlert");

                        if (_configuration.GetValue<bool>("EnableReshopNewTripResidualAlert"))
                        {
                            var appendnewtripcontent = EligibilityCheckHelper.PopulateReshopAdvisoryContent("ReshopAboutNewTripAlert")?.Body;
                            if (reshopnewtrip != null
                                && !string.IsNullOrEmpty(appendnewtripcontent))
                            {
                                reshopnewtrip.Body = HttpUtility.HtmlDecode($"{reshopnewtrip.Body}{appendnewtripcontent}");
                            }
                        }
                        else
                        {
                            reshopnewtrip.Body = HttpUtility.HtmlDecode($"{reshopnewtrip.Body}");
                        }
                    }
                    else
                    {
                        if (_configuration.GetValue<bool>("EnableReshopNewTripResidualAlert"))
                        {
                            reshopnewtrip = EligibilityCheckHelper.PopulateReshopAdvisoryContent("ReshopAboutNewTripAlert");
                        }
                    }
                    if (reshopnewtrip != null)
                    {
                        response.AdvisoryInfo = (response.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : response.AdvisoryInfo;
                        response.AdvisoryInfo.Add(reshopnewtrip);
                    }
                }

                response.PnrSegment = eligibility.Segments;
                response.SessionId = request.SessionId;
                response.Reservation.UpdateRewards(_configuration, _cachingService);
                await EligibilityCheckHelper.MapPersistedReservationToMOBSHOPReservation(response.Reservation, persistedReservation);
                if (response.Reservation != null)
                {
                    if(response.Reservation.ReshopTrips!=null && response.Reservation.ReshopTrips.Any())
                    {
                        response.Reservation.ReshopTrips.ForEach(p =>p.OriginalTrip.ArrivalDate = (p.OriginalTrip?.ArrivalDate == null) ? string.Empty : p.OriginalTrip?.ArrivalDate);
                        response.Reservation.ReshopTrips.ForEach(p =>p.OriginalTrip.FlattenedFlights.ForEach(ff=>ff.Flights.ForEach(f=>f.AirportChange=(f.AirportChange==null)? string.Empty: f.AirportChange)));
                        response.Reservation.ReshopTrips.ForEach(p =>p.OriginalTrip.ShareMessage=(p.OriginalTrip.ShareMessage ==null)? string.Empty : p.OriginalTrip.ShareMessage);
                    }
                }
            }
            catch (MOBUnitedException uaex)
            {
                response.Exception
                        = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

                _logger.LogWarning("ChangeEligibleCheck Error {@UnitedException}", JsonConvert.SerializeObject(uaex));

                if (!string.IsNullOrEmpty(uaex.Message) &&
                    uaex.Message.Equals(_configuration.GetValue<string>("NonELFVersionMessage")
                   , StringComparison.OrdinalIgnoreCase))
                {
                    response.PathEligible = true;
                    response.Exception
                        = new MOBException("9999", _configuration.GetValue<string>("NonELFVersionMessage"));
                }
                else if (uaex.Message == "ATRE")
                {
                    eligibility = new EligibilityResponse();
                    eligibility = await _sessionHelperService.GetSession<EligibilityResponse>(_headers.ContextValues.SessionId, eligibility.ObjectName, new List<string>() { _headers.ContextValues.SessionId, eligibility.ObjectName }).ConfigureAwait(false);
                    response.Exception.Message = "There was a problem completing your reservation";
                    response.Exception.Code = "RESHOPCHANGEERROR";
                    response.ResponseStatusItem = new MOBSHOPResponseStatusItem();

                    if (request.CatalogValues != null && request.CatalogValues.Count > 0 && IsEnableSaveTriptoMyAccount(request?.CatalogValues))
                    {
                        if (!string.IsNullOrEmpty(uaex.Code))
                        {
                            response.InEligibilityReasons = new EligibilityReasonsCode();
                            response.InEligibilityReasons = uaex.Code.Contains("Message") ? JsonConvert.DeserializeObject<EligibilityReasonsCode>(uaex.Code) : null;
                            if (response.InEligibilityReasons != null && response.InEligibilityReasons.Message != null)
                            {
                                response.IsShowChangeEligible = true;
                            }
                        }
                    }

                    if (await IsBulkTicketType(request.SessionId))
                    {
                        response.Exception.Message = _configuration.GetValue<string>("ChangeEligiblieBulkAlertMessage");
                        response.PathEligible = true;
                    }
                    else if (eligibility!=null && ( eligibility.IsElf || eligibility.IsIBE || eligibility.IsIBELite || eligibility.IsCBE))
                    {
                        response.Exception
                                 = new MOBException("9999", _configuration.GetValue<string>
                                 (eligibility.IsIBELite ? "IBELiteNotChangeEligible" : eligibility.IsBEChangeEligible
                                 ? "BENotChangeEligible_24FlexibleBooking" : "BENotChangeEligible"));
                        response.PathEligible = true;
                    }
                    else
                    {
                        response.Exception.Message += " " + request.RecordLocator;
                        response.ResponseStatusItem.Status = MOBSHOPResponseStatus.ReshopUnableToChange;
                        response.ResponseStatusItem.StatusMessages = await new MPDynamoDB(_configuration, _dynamoDBService, _legalDocumentsForTitlesService, _headers).GetMPPINPWDTitleMessages(new List<string> { "RTI_CHANGE_ELIGIBLE_UNABLE_COMPLETE_REQUEST" });
                    }
                }
                else if (uaex.Message == "USEDNOLOF")
                {
                    response.ResponseStatusItem = new MOBSHOPResponseStatusItem();
                    response.ResponseStatusItem.Status = MOBSHOPResponseStatus.ReshopUnableToChange;
                    response.ResponseStatusItem.StatusMessages = await new MPDynamoDB(_configuration, _dynamoDBService, _legalDocumentsForTitlesService, _headers).GetMPPINPWDTitleMessages(new List<string> { "RTI_CHANGE_ELIGIBLE_USEDNOLOF" });
                    response.Exception.Message += " " + request.RecordLocator;
                    response.Exception.Code = "RESHOPCHANGEERROR";
                    response.PathEligible = false;
                }
                else if (uaex.Message.Trim() == _configuration.GetValue<string>("GeneralSessionExpiryMessage").ToString().Trim())
                {
                    response.Exception.Message = uaex.Message;
                    if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                        response.Exception.Code = uaex.Code;
                    else
                        response.Exception.Code = "9999";
                    response.PathEligible = true;
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError("ChangeEligibleCheck Error {@Exception}", JsonConvert.SerializeObject(ex));
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
           
            return response;
        }

        private async Task<string> GetSDLCMSContentMesseges(string token,string sessionId)
        {
            MOBCSLContentMessagesRequest cslContentReqeust = new MOBCSLContentMessagesRequest();

            cslContentReqeust.Lang = "en";
            cslContentReqeust.Pos = "us";
            cslContentReqeust.Channel = "mobileapp";
            cslContentReqeust.Listname = new List<string>();
            cslContentReqeust.Usecache = false;
            cslContentReqeust.LocationCodes = new List<string>();
            cslContentReqeust.LocationCodes.Add("RTI_CHANGE_ELIGIBLE_UNABLE_COMPLETE_REQUEST");
            cslContentReqeust.Groupname = "Booking:RTI";
            cslContentReqeust.Listname = new List<string>();
            cslContentReqeust.Listname.Add("Messages");

            string jsonRequest = JsonConvert.SerializeObject(cslContentReqeust);

            var response = await _iCMSContentService.GetMobileCMSContentMessages(token: token, jsonRequest, sessionId).ConfigureAwait(false);

            return response;
        }

        public bool IsEnableSaveTriptoMyAccount(List<MOBItem> catalogItems)
        {
            return catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableIsEligibilityReasons).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableIsEligibilityReasons).ToString())?.CurrentValue == "1";
        }

        async Task<bool> IsBulkTicketType(string sessionId)
        {
            var cslReservation =await _sessionHelperService.GetSession<ReservationDetail>(_headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName, new List<string>() { _headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName }).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(sessionId) && cslReservation != null && cslReservation.Detail != null
                && cslReservation.Detail.Type != null && cslReservation.Detail.Type.Count() > 0)
            {
                return cslReservation.Detail.Type.Exists(p => p.Value == "BULK TICKET");
            }
            return false;
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
        private bool IsAward(Service.Presentation.CommonModel.Genre type)
        {
            return type.Description != null && type.Key != null && type.Description.ToUpper().Trim().Equals("ITIN_TYPE") && type.Key.ToUpper().Trim().Equals("AWARD");
        }
        private bool IsAward(Collection<Service.Presentation.CommonModel.Genre> types)
        {
            return types != null && types.Any(IsAward);
        }
        private bool IsEbulkPNRReshopEnabledV2(bool isEbulkCatalog, ReservationDetail cslReservation = null)
        {
            if (cslReservation?.Detail?.BookingIndicators.IsBulk == false) return false;
            return isEbulkCatalog;
        }
        private bool EBulkMAppReshopVersionCheckV2(bool isEbulkCatalog, ReservationDetail cslReservation = null)
        {
            if (cslReservation != null && cslReservation?.Detail?.BookingIndicators.IsBulk == false) return true;
            return isEbulkCatalog;
        }
    }
}
