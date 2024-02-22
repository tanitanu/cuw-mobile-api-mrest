using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using United.Common.Helper;
using United.Common.Helper.ManageRes;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.EligibleCheck;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.ReShop;
using United.Mobile.Model.Shopping;
using United.Mobile.ReShop.Domain;
using United.Service.Presentation.ReservationModel;
using United.Service.Presentation.ReservationRequestModel;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common.Extensions;
using United.Utility.Enum;
using United.Utility.Helper;
using MOBSHOPResponseStatusItem = United.Mobile.Model.Common.MOBSHOPResponseStatusItem;
using Reservation = United.Mobile.Model.Shopping.Reservation;


namespace United.Mobile.EligibleCheck.Domain
{
    public class EligibleCheckBusiness : IEligibleCheckBusiness
    {
        private readonly ICacheLog<EligibleCheckBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly IDPService _dPService;
        private readonly IPNRRetrievalService _pNRRetrievalService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly AirportDynamoDB _airportDynamoDB;
        private readonly IRefundService _refundService;
        private readonly IShoppingBusiness _shopBusiness;
        private readonly ICachingService _cachingService;

        public EligibleCheckBusiness(ICacheLog<EligibleCheckBusiness> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , IShoppingSessionHelper shoppingSessionHelper
            , IDPService dPService
            , IPNRRetrievalService pNRRetrievalService
            , IDynamoDBService dynamoDBService
            , IRefundService refundService
            , IShoppingBusiness shopBusiness
            , ICachingService cachingService
           )
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _shoppingSessionHelper = shoppingSessionHelper;
            _dPService = dPService;
            _pNRRetrievalService = pNRRetrievalService;
            _dynamoDBService = dynamoDBService;
            _airportDynamoDB = new AirportDynamoDB(_configuration, _dynamoDBService);
            _refundService = refundService;
            _shopBusiness = shopBusiness;
            _cachingService = cachingService;
            ConfigUtility.UtilityInitialize(_configuration);
            EligibilityCheckHelper.UtilityInitialize(_configuration);
        }

        public async Task<MOBRESHOPChangeEligibilityResponse> ChangeEligibleCheckAndReshop(MOBRESHOPChangeEligibilityRequest request)
        {
            var response = new MOBRESHOPChangeEligibilityResponse();
            var persistedReservation = new Reservation();
            Session session = null;
            Service.Presentation.ReservationResponseModel.ReservationDetail cslReservation;

            _logger.LogInformation("ChangeEligibleCheckAndReshop {clientRequest} and {sessionId}", request, request.SessionId);

            try
            {
                if (!string.IsNullOrEmpty(request.SessionId))
                {
                    session = await _sessionHelperService.GetSession<Session>(Headers.ContextValues.SessionId, new Session().ObjectName, new List<string>() { Headers.ContextValues.SessionId, new Session().ObjectName });
                }

                if (session == null)
                {
                    session = _shoppingSessionHelper.CreateShoppingSession
                        (request.Application.Id, request.DeviceId, request.Application.Version.Major, request.TransactionId,
                        request.MileagePlusNumber, string.Empty);
                }

                if (session != null)
                {
                    request.SessionId = session.SessionId;
                    session.IsReshopChange = true;

                    Headers.ContextValues.SessionId = session.SessionId;

                    if (request?.ReshopRequest != null)
                    {
                        session.IsAward = request.ReshopRequest.AwardTravel;
                    }

                    session.Flow = Convert.ToString(FlowType.CHECKINSDC);
                    request.FlowType = Convert.ToString(FlowType.CHECKINSDC);

                    await _sessionHelperService.SaveSession<Session>(session, Headers.ContextValues.SessionId, new List<string>() { Headers.ContextValues.SessionId, (new Session()).ObjectName }, (new Session()).ObjectName);
                }

                _logger.LogInformation("ChangeEligibleCheckAndReshop {clientResquest} and {sessionId}", request, session);

                //Eligibility Call
                response = await ReshoppingChangeEligibleCheck(request, isAllowAward: true, isSameDayChangeVersion: true, isExceptionPolicyVersion: true);

                cslReservation = await _sessionHelperService.GetSession<ReservationDetail>
                  (Headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName, new List<string>() { Headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName });


                bool isCheckinEligible = EligibilityCheckHelper.CheckCheckinEligible(request);

                ////Request Mapping  
                var reshoprequest = new MOBSHOPShopRequest();
                reshoprequest
                  = CreateReshopMOBSHOPShopRequest(request, response, cslReservation);


                response.RedirectURL = EligibilityCheckHelper.GetSDCRedirect3dot0Url(request, response);
                response.WebSessionShareUrl = _configuration.GetValue<string>("DotcomSSOUrl");

                BuildReservationObjectForReshopChange
                       (request, cslReservation, isCanceledWithFutureFlightCredit: false, mresponse: response);

                persistedReservation = _sessionHelperService.GetSession<Reservation>(Headers.ContextValues.SessionId, new Reservation().ObjectName, new List<string>() { Headers.ContextValues.SessionId, new Reservation().ObjectName }).Result;

                response.Reservation = new MOBSHOPReservation();

                EligibilityCheckHelper.MapPersistedReservationToMOBSHOPReservation(response.Reservation, persistedReservation);

                if (response.PathEligible && isCheckinEligible)
                {
                    response.ShopResponse = await _shopBusiness.Shop(reshoprequest);
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
                            bool validWalletRequest = false;

                            HashPin hashPin = new HashPin(null,_configuration, null, _dynamoDBService);
                            validWalletRequest = hashPin.ValidateHashPinAndGetAuthToken
                                (request.MileagePlusNumber, request.HashPinCode, request.Application.Id, request.DeviceId, request.Application.Version.Major);

                            if (!validWalletRequest)
                                throw new UnitedException(_configuration.GetValue<string>("bugBountySessionExpiredMsg"));

                            if (validWalletRequest)
                            {
                                response.TransactionId = request.TransactionId;
                                response.LanguageCode = request.LanguageCode;
                                response.WebShareToken = ShopStaticUtility.GetSSOToken(request.Application.Id, request.DeviceId, request.Application.Version.Major, request.TransactionId, null, request.SessionId, request.MileagePlusNumber);
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
            }
            catch (UnitedException uaex)
            {
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                _logger.LogWarning("ChangeEligibleCheckAndReshop Error {exception} ,{stackTrace} and {sessionId}", uaex, uaex.StackTrace, session);

                if (uaex.Message == "USEDNOLOF")
                {
                    response.Exception = null;
                    response.PathEligible = false;
                }

            }
            catch (System.Exception ex)
            {
                _logger.LogError("ChangeEligibleCheckAndReshop Error {exception} ,{stackTrace} and {sessionId}", ex, ex.StackTrace, session);
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
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

            if (isCanceledWithFutureFlightCredit)
            {
                bookingPathRes.Reshop.ChangeTripTitle = string.Empty;
                bookingPathRes.Reshop.ChangeFlightHeaderText = string.Empty;
            }

            bookingPathRes.Reshop.FlightHeaderText = (_configuration.GetValue<string>("ReshopChange-NEWTRIP") ?? "");

            Reservation persistReservation = new Reservation();
            persistReservation = await _sessionHelperService.GetSession<Reservation>(Headers.ContextValues.SessionId, persistReservation.ObjectName, new List<string>() { Headers.ContextValues.SessionId, persistReservation.ObjectName });

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

            int tripNumber = 0;
            int remainingFlightsCount = 0;
            bool usedFlag = false;

            foreach (var cslsegment in cslReservation.Detail.FlightSegments)
            {
                if (!cslReservation.Detail.FlightSegments.Exists(p => p.TripNumber == "1"))
                {
                    usedFlag = true;
                    bookingPathRes.Reshop.IsUsedPNR = true;
                }

                bool flightUsedSegmentStatus = false;
                if (cslsegment.FlightSegment.FlightStatuses.Count() > 0)
                {
                    flightUsedSegmentStatus = cslsegment.FlightSegment.FlightStatuses[0].StatusType.Equals("USED");
                }
                // Reshop: Remove lof with used segement #
                if (flightUsedSegmentStatus && tripNumber == Convert.ToInt32(cslsegment.TripNumber) - 1)
                {
                    tripNumber++;
                    bookingPathRes.Reshop.IsUsedPNR = true;
                    var tripAllSegments = cslReservation.Detail.FlightSegments.Where(p => p.TripNumber == cslsegment.TripNumber).ToList();
                    var mOBSHOPReShopTrip = new ReshopTrip() { OriginalTrip = ConvertPNRSegmentToShopTrip(tripAllSegments) };
                    bookingPathRes.ReshopUsedTrips.Add(mOBSHOPReShopTrip);
                    continue;
                }

                if (tripNumber != Convert.ToInt32(cslsegment.TripNumber) && !flightUsedSegmentStatus)
                {
                    var tripAllSegments = cslReservation.Detail.FlightSegments.Where(p => p.TripNumber == cslsegment.TripNumber).ToList();
                    var mOBSHOPReShopTrip = new ReshopTrip() { OriginalTrip = ConvertPNRSegmentToShopTrip(tripAllSegments), OriginalUsedIndex = remainingFlightsCount };

                    bookingPathRes.ReshopTrips.Add(mOBSHOPReShopTrip);
                    tripNumber = cslsegment.TripNumber.ToInteger(0);
                    remainingFlightsCount++;
                }
            }
            if (remainingFlightsCount == 0)
            {
                throw new UnitedException((_configuration.GetValue<string>("ReshopChange-USEDNOLOF") ?? ""));
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

            _sessionHelperService.SaveSession<Reservation>(bookingPathRes, Headers.ContextValues.SessionId, new List<string>() { Headers.ContextValues.SessionId, bookingPathRes.ObjectName }, bookingPathRes.ObjectName);

            return bookingPathRes;
        }

        private MOBSHOPTrip ConvertPNRSegmentToShopTrip(List<ReservationFlightSegment> pnrFlightSegment)
        {
            MOBSHOPTrip trip = null;
            if (pnrFlightSegment != null && pnrFlightSegment.Count > 0)
            {
                var pnrLastFlightSegment = pnrFlightSegment.Where(p => p.TripNumber == pnrFlightSegment[0].TripNumber).OrderByDescending(r => Convert.ToInt32(r.SegmentNumber)).FirstOrDefault();
                trip = new MOBSHOPTrip();
                trip.Origin = pnrFlightSegment[0].FlightSegment.DepartureAirport.IATACode;
                trip.OriginDecoded = _airportDynamoDB.GetAirportName(trip.Origin, Headers.ContextValues.SessionId); // pnrFlightSegment[0].FlightSegment.DepartureAirport.Name;
                trip.Destination = pnrLastFlightSegment.FlightSegment.ArrivalAirport.IATACode;
                trip.DestinationDecoded = _airportDynamoDB.GetAirportName(trip.Destination, Headers.ContextValues.SessionId); //pnrLastFlightSegment.FlightSegment.ArrivalAirport.Name;
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
                    flight.OriginDescription = _airportDynamoDB.GetAirportName(flight.Origin, Headers.ContextValues.SessionId);
                    flight.DestinationDescription = _airportDynamoDB.GetAirportName(flight.Destination, Headers.ContextValues.SessionId);

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
            using (ICslContext cslContext = new CslContext(request, _sessionHelperService, _dPService, _configuration))
            {
                var session = await _sessionHelperService.GetSession<Session>(Headers.ContextValues.SessionId, (new Session()).ObjectName, new List<string>() { Headers.ContextValues.SessionId, (new Session()).ObjectName });

                session.Token = cslContext.Token;

                await _sessionHelperService.SaveSession<Session>(session, Headers.ContextValues.SessionId, new List<string>() { Headers.ContextValues.SessionId, (new Session()).ObjectName }, (new Session()).ObjectName);

                return await ChangeEligibleCheck(request, cslContext, isAllowAward, isSameDayChangeVersion, isExceptionPolicyVersion, session.IsAward);
            }
        }

        private async Task<MOBRESHOPChangeEligibilityResponse> ChangeEligibleCheck(MOBRESHOPChangeEligibilityRequest changeEligibleCheckRequest, ICslContext cslContext, bool isAllowAward, bool isSameDayChangeVersion, bool isExceptionPolicyVersion, bool isAwardTravel)
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

            var cslResponse = MakeHTTPCallAndLogIt(
                                        changeEligibleCheckRequest.SessionId,
                                        changeEligibleCheckRequest.DeviceId,
                                        "CSL-ChangeEligibleCheck",
                                        changeEligibleCheckRequest.Application,
                                        cslContext.Token,
                                        path,
                                        string.Empty,
                                        true,
                                        false);

            if (cslResponse != null)
            {
                response
                    = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityResponse>(cslResponse);

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


                            await _sessionHelperService.SaveSession<ReservationDetail>(reservationdetails, Headers.ContextValues.SessionId, new List<string>() { Headers.ContextValues.SessionId, reservationdetails.GetType().FullName }, reservationdetails.GetType().FullName);

                            var retrieveRequestData = new RetrieveRequestData(_configuration, _dynamoDBService);
                            response.PnrSegments = retrieveRequestData.GetAllSegments(changeEligibleCheckRequest, reservationdetails);
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
                        string token = cslContext.Token;

                        string pnrRetrival = GetPnrDetailsFromCSL
                            (changeEligibleCheckRequest.TransactionId, changeEligibleCheckRequest.RecordLocator,
                            changeEligibleCheckRequest.LastName, changeEligibleCheckRequest.Application.Id,
                            changeEligibleCheckRequest.Application.Version.Major, "CSLPNRRetrivel - ChangeEligibileCheck",
                            ref token, true);

                        reservationdetails = DataContextJsonSerializer.Deserialize<ReservationDetail>(pnrRetrival);

                        await _sessionHelperService.SaveSession<ReservationDetail>
                            (reservationdetails, Headers.ContextValues.SessionId, new List<string>() { Headers.ContextValues.SessionId, reservationdetails.GetType().FullName }, reservationdetails.GetType().FullName);
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

                if (changeEligibleCheckRequest.FlowType != Convert.ToString(FlowType.CHECKINSDC))
                {
                    SetupRedirectURI(response);
                }

                if (response.FailedRule == "ATRE")
                {
                    throw new MOBUnitedException(response.FailedRule);
                }
                return response;
            }

            return response;
        }

        private string MakeHTTPCallAndLogIt(string sessionId, string deviceId, string action, MOBApplication application, string token, string path, string jsonRequest, bool isGetCall, bool isXMLRequest = false)
        {
            var jsonResponse = string.Empty;

            if (isGetCall)
            {
                jsonResponse = _refundService.GetEligibleCheck<MOBRESHOPChangeEligibilityResponse>(token, sessionId, path).Result;
            }
            else
            {
                jsonResponse = _refundService.PostEligibleCheck<MOBRESHOPChangeEligibilityResponse>(token, sessionId, path, jsonRequest).Result;
            }
            return jsonResponse;

        }

        private void SetupRedirectURI(MOBRESHOPChangeEligibilityResponse refundReservationResponse)
        {
            if (!refundReservationResponse.PathEligible)
            {
                refundReservationResponse.Exception = new MOBException("8999", _configuration.GetValue<string>("Change-IneligibleMessage"));
            }
        }

        private MOBSHOPShopRequest CreateReshopMOBSHOPShopRequest(
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
            var retrieveRequestData = new RetrieveRequestData(_configuration, _dynamoDBService);
            List<MOBTrip> alltrips = retrieveRequestData.GetAllTrips(reservationDetail, isAward);
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

            reshoprequest.ReshopSegments = response.PnrSegments;

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

                if (request.ReshopRequest.mobCPCorporateDetails != null)
                {
                    reshoprequest.mobCPCorporateDetails = new MOBCorporateDetails
                    {
                        CorporateCompanyName = request.ReshopRequest.mobCPCorporateDetails.CorporateCompanyName,
                        CorporateTravelProvider = request.ReshopRequest.mobCPCorporateDetails.CorporateTravelProvider,
                        DiscountCode = request.ReshopRequest.mobCPCorporateDetails.DiscountCode,
                        NoOfTravelers = request.ReshopRequest.mobCPCorporateDetails.NoOfTravelers,
                    };
                }
            }
            return reshoprequest;
        }

        #region FLIGHT RESERVAtion
        private string GetPnrDetailsFromCSL(string transactionId, string recordLocator, string lastName, int applicationId, string appVersion, string actionName, ref string token, bool usedRecall = false)
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

            var jsonResponse = RetrievePnrDetailsFromCsl(applicationId, transactionId, request).Result;
            return jsonResponse;
        }

        private async Task<string> RetrievePnrDetailsFromCsl(int applicationId, string transactionId, RetrievePNRSummaryRequest request)
        {
            var jsonRequest = System.Text.Json.JsonSerializer.Serialize<RetrievePNRSummaryRequest>(request);

            string token = await _dPService.GetAnonymousToken(applicationId, Headers.ContextValues.DeviceId, _configuration);

            var jsonResponse = string.Empty;
            jsonResponse = await _pNRRetrievalService.PNRRetrieval(token, jsonRequest, transactionId);

            return jsonResponse;
        }

        #endregion


        public async Task<MOBRESHOPChangeEligibilityResponse> ChangeEligibleCheck(MOBRESHOPChangeEligibilityRequest request)
        {
            var response = new MOBRESHOPChangeEligibilityResponse();
            Reservation persistedReservation = null;
            EligibilityResponse eligibility = null;
            Session session = null;

            _logger.LogInformation("ChangeEligibleCheck {clientRequest} and {sessionId}", request, request.SessionId);

            try
            {
                if (_configuration.GetValue<bool>("EnableReshopChangeFeeForceUpdate"))
                {
                    new ForceUpdateVersion(_configuration).ForceUpdateForNonSupportedVersion
                            (request.Application.Id, request.Application.Version.Major, FlowType.RESHOP);
                }

                eligibility = new EligibilityResponse();

                eligibility = await _sessionHelperService.GetSession<EligibilityResponse>(Headers.ContextValues.SessionId, (new EligibilityResponse()).ObjectName, new List<string>() { Headers.ContextValues.SessionId, (new EligibilityResponse()).ObjectName });

                if (eligibility != null)
                {
                    if (_configuration.GetValue<bool>("EnableReshopMilesMoney")
                            && eligibility.IsMilesAndMoney && !eligibility.IsATREEligible
                            && !eligibility.IsIBE && !eligibility.IsIBELite && !eligibility.IsElf)
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
                                var agencymsginfo = new ShuttleOfferInfo(_configuration, _dynamoDBService).GetDBDisplayContent("Reshop_ChangeCancelMsg_Content");
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

                _logger.LogInformation("MOBRESHOPChangeEligibilityRequest {request} and {sessionId}", request, request.SessionId);

                session = _shoppingSessionHelper.GetBookingFlowSession(request.SessionId, false);

                if (ConfigUtility.EnableUMNRInformation(request.Application.Id, request.Application.Version.Major))
                {
                    var pnrSession = _sessionHelperService.GetSession<MOBPNR>(Headers.ContextValues.SessionId, ObjectNames.MOBPNR, new List<string>() { Headers.ContextValues.SessionId, ObjectNames.MOBPNR }).Result;
                    if (pnrSession != null && pnrSession.IsUnaccompaniedMinor)
                    {
                        response.Exception = new MOBException();
                        response.ResponseStatusItem = new MOBSHOPResponseStatusItem();
                        response.Exception.Message += " " + request.RecordLocator;
                        response.ResponseStatusItem.Status = MOBSHOPResponseStatus.ReshopUnableToChange;
                        response.ResponseStatusItem.StatusMessages = new MPDynamoDB(_configuration, _dynamoDBService, null).GetMPPINPWDTitleMessages(new List<string> { "RTI_CHANGE_ELIGIBLE_UNABLE_COMPLETE_REQUEST" });
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


                response = ReshoppingChangeEligibleCheck(request, awardVersionCheck, sameDayChangeVersionCheck, exceptionPolicyVersionCheck).Result;

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
                  (Headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName, new List<string>() { Headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName });


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
                    response.Reservation = new MOBSHOPReservation();
                    persistedReservation = new Reservation();
                    if (response.PathEligible)
                    {
                        await BuildReservationObjectForReshopChange(request, cslReservation, eligibility.IsReshopWithFutureFlightCredit);
                        persistedReservation = await _sessionHelperService.GetSession<Reservation>(Headers.ContextValues.SessionId, persistedReservation.ObjectName, new List<string>() { Headers.ContextValues.SessionId, persistedReservation.ObjectName });
                    }
                }
                else
                {
                    if (response.PathEligible)
                        await BuildReservationObjectForReshopChange(request, cslReservation, eligibility.IsReshopWithFutureFlightCredit);
                    response.Reservation = new MOBSHOPReservation();
                    persistedReservation = new Reservation();
                    persistedReservation = await _sessionHelperService.GetSession<Reservation>(Headers.ContextValues.SessionId, persistedReservation.ObjectName, new List<string>() { Headers.ContextValues.SessionId, persistedReservation.ObjectName });
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

                            validWalletRequest = new HashPin(null, _configuration, null, _dynamoDBService).ValidateHashPinAndGetAuthToken
                                (request.MileagePlusNumber, request.HashPinCode, request.Application.Id, request.DeviceId, request.Application.Version.Major, ref authToken);

                            if (!validWalletRequest)
                                throw new MOBUnitedException(_configuration.GetValue<string>("bugBountySessionExpiredMsg"));

                            if (validWalletRequest)
                            {
                                response.TransactionId = request.TransactionId;
                                response.LanguageCode = request.LanguageCode;

                                response.WebShareToken = ShopStaticUtility.GetSSOToken
                                    (request.Application.Id, request.DeviceId, request.Application.Version.Major, request.TransactionId, null, request.SessionId, request.MileagePlusNumber);
                                if (!String.IsNullOrEmpty(response.WebShareToken))
                                {
                                    response.WebSessionShareUrl = _configuration.GetValue<string>("DotcomSSOUrl");
                                }
                            }
                        }
                    }
                }

                response.Reservation.SessionId = request.SessionId;
                response.PnrTravelers = eligibility.Passengers;

                if (_configuration.GetValue<bool>("EnablePNRShuttleOfferAvailable"))
                {
                    var ewrOffer = new ShuttleOfferInfo(_configuration, _dynamoDBService).GetEWRShuttleOfferInformation();
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
                                var checkineligiblemsg = new ShuttleOfferInfo(_configuration, _dynamoDBService).GetDBDisplayContent("Reshop_ChangeCancelMsg_Content");
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

                response.PnrSegments = eligibility.Segments;
                response.SessionId = request.SessionId;
                response.Reservation.UpdateRewards(_configuration, _cachingService);
                EligibilityCheckHelper.MapPersistedReservationToMOBSHOPReservation(response.Reservation, persistedReservation);
            }
            catch (UnitedException uaex)
            {
                response.Exception
                        = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

                _logger.LogWarning("ChangeEligibleCheck Error {exception} ,{stackTrace} and {sessionId}", uaex, uaex.StackTrace, session);

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
                    eligibility = await _sessionHelperService.GetSession<EligibilityResponse>(Headers.ContextValues.SessionId, eligibility.ObjectName, new List<string>() { Headers.ContextValues.SessionId, eligibility.ObjectName });
                    response.Exception.Message = "There was a problem completing your reservation";
                    response.Exception.Code = "RESHOPCHANGEERROR";
                    response.ResponseStatusItem = new MOBSHOPResponseStatusItem();

                    if (IsBulkTicketType(request.SessionId))
                    {
                        response.Exception.Message = _configuration.GetValue<string>("ChangeEligiblieBulkAlertMessage");
                        response.PathEligible = true;
                    }
                    else if (eligibility.IsElf || eligibility.IsIBE || eligibility.IsIBELite)
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
                        response.ResponseStatusItem.StatusMessages = new MPDynamoDB(_configuration, _dynamoDBService, null).GetMPPINPWDTitleMessages(new List<string> { "RTI_CHANGE_ELIGIBLE_UNABLE_COMPLETE_REQUEST" });
                    }
                }
                else if (uaex.Message == "USEDNOLOF")
                {
                    response.ResponseStatusItem = new MOBSHOPResponseStatusItem();
                    response.ResponseStatusItem.Status = MOBSHOPResponseStatus.ReshopUnableToChange;
                    response.ResponseStatusItem.StatusMessages = new MPDynamoDB(_configuration, _dynamoDBService, null).GetMPPINPWDTitleMessages(new List<string> { "RTI_CHANGE_ELIGIBLE_USEDNOLOF" });
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
                _logger.LogError("ChangeEligibleCheck Error {exception} ,{stackTrace} and {sessionId}", ex, ex.StackTrace, session);
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            return response;
        }

        bool IsBulkTicketType(string sessionId)
        {
            var cslReservation = _sessionHelperService.GetSession<ReservationDetail>(Headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName, new List<string>() { Headers.ContextValues.SessionId, (new ReservationDetail()).GetType().FullName }).Result;
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
    }
}
