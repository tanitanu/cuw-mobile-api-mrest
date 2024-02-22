using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Travelers;
using United.Service.Presentation.ReferenceDataModel;
using United.Utility.Helper;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.Travelers.Domain
{
    public class ValidateMPNameBusiness : IValidateMPNameBusiness
    {
        private readonly ICacheLog<ValidateMPNameBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly ITravelerUtility _travelerUtility;
        private readonly ITraveler _traveler;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly ICachingService _cachingService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ICSLStatisticsService _cSLStatisticsService;
        private readonly IFeatureSettings _featureSettings;

        public ValidateMPNameBusiness(ICacheLog<ValidateMPNameBusiness> logger, IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility
            , ITravelerUtility travelerUtility
            , ITraveler traveler
            , IShoppingSessionHelper shoppingSessionHelper
            , ICachingService cachingService, IDynamoDBService dynamoDBService, ICSLStatisticsService cSLStatisticsService
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
            _travelerUtility = travelerUtility;
            _traveler = traveler;
            _shoppingSessionHelper = shoppingSessionHelper;
            _cachingService = cachingService;
            _dynamoDBService = dynamoDBService;
            _cSLStatisticsService = cSLStatisticsService;
            _featureSettings = featureSettings;
        }

        public async Task<MOBMPNameMissMatchResponse> ValidateMPNameMisMatch_CFOP(MOBMPNameMissMatchRequest request)
        {
            MOBMPNameMissMatchResponse response = new MOBMPNameMissMatchResponse();
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            Session session = await _shoppingSessionHelper.GetBookingFlowSession(request.SessionId);
            request.Token = session.Token;
            request.CartId = session.CartId;

            response.CartId = request.CartId;
            response.SessionId = request.SessionId;
            response.Token = session.Token;
            response.MileagePlusNumber = request.MileagePlusNumber;
            response.TransactionId = request.TransactionId;
            MOBRequest mobRequest = new MOBRequest();
            mobRequest.Application = request.Application;
            mobRequest.AccessCode = request.AccessCode;
            mobRequest.DeviceId = request.DeviceId;
            mobRequest.LanguageCode = request.LanguageCode;
            mobRequest.TransactionId = request.TransactionId;

            bool isExtraSeatFeatureEnabled = _travelerUtility.IsExtraSeatFeatureEnabled(request.Application.Id, request.Application.Version.Major, session.CatalogItems);

            if (_travelerUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major))
            {
                var bookingReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, (new Reservation()).ObjectName, new List<String> { request.SessionId, (new Reservation()).ObjectName }).ConfigureAwait(false);
                if (bookingReservation.ShopReservationInfo2 != null && bookingReservation.ShopReservationInfo2.TravelerTypes != null && bookingReservation.ShopReservationInfo2.TravelerTypes.Count > 0)
                {
                    if (!string.IsNullOrEmpty(request.Traveler.BirthDate))
                    {
                        int age = TopHelper.GetAgeByDOB(request.Traveler.BirthDate, bookingReservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTime);
                        request.Traveler.TravelerTypeCode = _travelerUtility.GetTypeCodeByAge(age, (!string.IsNullOrEmpty(request.Traveler.TravelerTypeCode)) ? request.Traveler.TravelerTypeCode.ToUpper().Equals("INF") : false);
                        request.Traveler.TravelerTypeDescription = _travelerUtility.GetPaxDescriptionByDOB(request.Traveler.BirthDate, bookingReservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTime);
                        request.Traveler.PTCDescription = request.Traveler.TravelerTypeDescription;
                    }
                }
            }

            // MB-3148(MBCS-41) & MB-3189 fix for guest flow - Traveler Consolidation screen is looping when the same name given for multi pax
            //if (_configuration.GetValue<bool>("DuplicateTravelerCheck"))
            if (await _featureSettings.GetFeatureSettingValue("EnableFixForDuplicateTraveler_MOBILE35239").ConfigureAwait(false))
            {
                await _travelerUtility.DuplicateTravelerCheck(request.SessionId, request.Traveler, isExtraSeatFeatureEnabled);
            }

            response.Traveler = await _traveler.RegisterNewTravelerValidateMPMisMatch(request.Traveler, mobRequest, request.SessionId, request.CartId, request.Token);
            response.isMPNameMisMatch = response.Traveler.isMPNameMisMatch;

            if (response.isMPNameMisMatch)
            {
                response.Exception = new MOBException("1111", _configuration.GetValue<string>("ValidateMPNameMisMatchErrorMessage"));
            }
            else
            {
                #region
                var bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, (new Reservation()).ObjectName, new List<string> { request.SessionId, (new Reservation()).ObjectName }).ConfigureAwait(false);
                if (bookingPathReservation.ShopReservationInfo2 == null)
                {
                    bookingPathReservation.ShopReservationInfo2 = new ReservationInfo2();
                    bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL = new List<MOBCPTraveler>();
                }
                else
                {
                    if (bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL == null)
                    {
                        bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL = new List<MOBCPTraveler>();
                    }
                }
                MOBCPTraveler editingPax = null;
                //adding pax
                if (request.Traveler != null)
                {
                    if (request.Traveler.PaxID == 0)
                    {

                        editingPax = GetMatchedTraverler(bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL, request.Traveler);
                    }
                    else
                    {
                        editingPax = bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL.FirstOrDefault(p => p.PaxID == request.Traveler.PaxID);
                    }

                    await _travelerUtility.ExtraSeatHandling(request.Traveler, mobRequest, bookingPathReservation, session, "", editingPax != null);

                    if (isExtraSeatFeatureEnabled)
                    {
                        request.Traveler.IsEligibleForExtraSeatSelection = _travelerUtility.IsExtraSeatEligibleForInfantOnLapAndInfantOnSeat(request.Traveler.TravelerTypeCode, request.Traveler.IsExtraSeat);
                        response.Traveler.IsEligibleForExtraSeatSelection = request.Traveler.IsEligibleForExtraSeatSelection;
                    }

                    if (editingPax == null)
                    {
                        #region Special Needs

                        if (_travelerUtility.EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
                        {
                            var finalSpecialNeeds = await _travelerUtility.ValidateSpecialNeedsAgaintsMasterList(request.Traveler.SelectedSpecialNeeds, bookingPathReservation.ShopReservationInfo2.SpecialNeeds, request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false);
                            request.Traveler.SelectedSpecialNeeds = finalSpecialNeeds;
                        }

                        #endregion

                        request.Traveler.PaxID = bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count + 1;
                        request.Traveler.PaxIndex = bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count;
                        request.Traveler.IsPaxSelected = false;
                        var stringfyCPTraveler = JsonConvert.SerializeObject(request.Traveler);
                        MOBCPTraveler copyCPTraveler = JsonConvert.DeserializeObject<MOBCPTraveler>(stringfyCPTraveler);
                        bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Add(copyCPTraveler);
                        if (request.AlreadySelectedPAXIDs == null)
                            request.AlreadySelectedPAXIDs = new List<int>();
                        request.AlreadySelectedPAXIDs.Add(request.Traveler.PaxID);
                        bookingPathReservation.ShopReservationInfo2.NextViewName = "TRAVELERADDED";
                    }
                    else
                    {
                        #region Special Needs

                        if (_travelerUtility.EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
                        {
                            var finalSpecialNeeds = await _travelerUtility.ValidateSpecialNeedsAgaintsMasterList(request.Traveler.SelectedSpecialNeeds, bookingPathReservation.ShopReservationInfo2.SpecialNeeds, request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false);
                            request.Traveler.SelectedSpecialNeeds = finalSpecialNeeds;
                        }

                        #endregion

                        if (request.Traveler.PaxID == 0 && request.AlreadySelectedPAXIDs != null && !request.AlreadySelectedPAXIDs.Contains(editingPax.PaxID))
                        {
                            request.AlreadySelectedPAXIDs.Add(editingPax.PaxID);
                        }
                        request.Traveler.PaxIndex = editingPax.PaxIndex;
                        request.Traveler.PaxID = editingPax.PaxID;
                        int travelrLocationInCollection = bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL.IndexOf(editingPax);
                        request.Traveler.IsPaxSelected = editingPax.IsPaxSelected;
                        var stringfyCPTraveler = JsonConvert.SerializeObject(request.Traveler);
                        MOBCPTraveler copyCPTraveler = JsonConvert.DeserializeObject<MOBCPTraveler>(stringfyCPTraveler);
                        bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL[travelrLocationInCollection] = copyCPTraveler;
                        if (_travelerUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major) && bookingPathReservation.ShopReservationInfo2.TravelerTypes != null
                            && bookingPathReservation.ShopReservationInfo2.TravelerTypes.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(bookingPathReservation.ShopReservationInfo2.NextViewName) && bookingPathReservation.ShopReservationInfo2.NextViewName.ToUpper().Equals("RTI"))
                            {
                                if (_configuration.GetValue<bool>("EnableNavigatingToRTIFix") && (request.Traveler.FirstName == editingPax.FirstName) && (request.Traveler.LastName == editingPax.LastName)
                                    && (request.Traveler.KnownTravelerNumber == editingPax.KnownTravelerNumber) && (request.Traveler.BirthDate == editingPax.BirthDate) && (request.Traveler.GenderCode == editingPax.GenderCode))
                                {
                                    bookingPathReservation.ShopReservationInfo2.NextViewName = "RTI";
                                }
                                else
                                {
                                    bookingPathReservation.ShopReservationInfo2.NextViewName = "TravelOption";
                                }
                            }
                        }

                        if (isExtraSeatFeatureEnabled && !request.Traveler.IsExtraSeat)
                        {
                            foreach (var traveler in bookingPathReservation?.ShopReservationInfo2?.AllEligibleTravelersCSL)
                            {
                                if (traveler != null && traveler.IsExtraSeat && traveler.ExtraSeatData?.SelectedPaxId == request.Traveler.PaxID)
                                {
                                    traveler.LastName = _travelerUtility.GetTravelerDisplayNameForExtraSeat(request.Traveler.FirstName, request.Traveler.MiddleName, request.Traveler.LastName, request.Traveler.Suffix);
                                    traveler.MiddleName = request.Traveler.MiddleName;
                                    traveler.Suffix = request.Traveler.Suffix;
                                    traveler.BirthDate = request.Traveler.BirthDate;
                                    traveler.GenderCode = request.Traveler.GenderCode;
                                    traveler.TravelerTypeCode = request.Traveler.TravelerTypeCode;
                                    traveler.TravelerTypeDescription = request.Traveler.TravelerTypeDescription;
                                    traveler.Nationality = request.Traveler.Nationality;
                                    traveler.CountryOfResidence = request.Traveler.CountryOfResidence;
                                    traveler.CubaTravelReason = request.Traveler.CubaTravelReason;
                                }
                            }
                        }
                    }

                    if (bookingPathReservation.IsCubaTravel)
                    {
                        _travelerUtility.ValidateTravelersForCubaReason(bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL, bookingPathReservation.IsCubaTravel);
                    }
                }
                await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, request.SessionId, new List<string> { request.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName);

                response.Reservation = new MOBSHOPReservation(_configuration, _cachingService);
                response.Reservation =await _shoppingUtility.GetReservationFromPersist(response.Reservation, request.SessionId).ConfigureAwait(false);

                if (response.Reservation != null && response.Reservation.ShopReservationInfo2 != null &&
                    response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL != null &&
                    response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL.Any())
                {
                    var eligibleTraveler = response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL.FirstOrDefault(p => p.PaxID == request.Traveler.PaxID);
                    eligibleTraveler.IsPaxSelected = true;
                    response.Reservation.TravelersCSL = new List<MOBCPTraveler>();
                    bookingPathReservation.TravelersCSL = new SerializableDictionary<string, MOBCPTraveler>();
                    string cubaTravelerMsg = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage") as string;

                    foreach (var traveler in response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL)
                    {
                        traveler.IsPaxSelected = request.AlreadySelectedPAXIDs != null && request.AlreadySelectedPAXIDs.Exists(id => id == traveler.PaxID);

                        if (session.IsReshopChange && bookingPathReservation.IsCubaTravel)
                        {
                            if (_travelerUtility.EnableReshopCubaTravelReasonVersion(request.Application.Id, request.Application.Version.Major))
                            {
                                if ((request.Traveler.CubaTravelReason != null
                                    && !string.IsNullOrEmpty(request.Traveler.CubaTravelReason.Vanity))
                                    && traveler.PaxID == request.Traveler.PaxID)
                                {
                                    traveler.CubaTravelReason = request.Traveler.CubaTravelReason;
                                    traveler.Message = string.Empty;
                                }
                                else if (traveler.CubaTravelReason == null
                                    || (traveler.CubaTravelReason != null
                                    && string.IsNullOrEmpty(traveler.CubaTravelReason.Vanity)))
                                {
                                    traveler.Message = cubaTravelerMsg;
                                }
                                else
                                {
                                    traveler.Message = string.Empty;
                                }

                                bookingPathReservation.TravelersCSL.Add(Convert.ToString(traveler.PaxIndex), traveler);
                                response.Reservation.TravelersCSL.Add(traveler);

                                await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, request.SessionId, new List<string> { request.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName);
                            }
                        }
                        else
                        {
                            if (traveler.IsPaxSelected || traveler.PaxID == 0)
                            {
                                response.Reservation.TravelersCSL.Add(traveler);
                            }
                        }
                    }
                }
                response.Reservation.IsBookingCommonFOPEnabled = bookingPathReservation.IsBookingCommonFOPEnabled;
                response.Reservation.IsReshopCommonFOPEnabled = bookingPathReservation.IsReshopCommonFOPEnabled;
                shoppingCart= await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, shoppingCart.ObjectName, new List<string> { request.SessionId, shoppingCart.ObjectName }).ConfigureAwait(false);
               shoppingCart = await _shoppingUtility.ReservationToShoppingCart_DataMigration(response.Reservation, shoppingCart, request, session);
                shoppingCart.SCTravelers = (shoppingCart.SCTravelers == null ? new List<MOBCPTraveler>() : shoppingCart.SCTravelers);
                if (shoppingCart?.OmniCart?.FOPDetails != null && !shoppingCart.OmniCart.FOPDetails.Any())
                    shoppingCart.OmniCart.FOPDetails = null;

                shoppingCart.Flow = request.Flow;
                await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, request.SessionId, new List<string> { request.SessionId, shoppingCart.ObjectName}, shoppingCart.ObjectName);
                response.ShoppingCart = shoppingCart;

                #endregion

            }
            if (_configuration.GetValue<bool>("Log_CSL_Call_Statistics"))
            {
                try
                {
                    CSLStatistics _cslStatistics = new CSLStatistics(_logger, _configuration, _dynamoDBService, _cSLStatisticsService);
                    string callDurations = string.Empty;
                    await _cslStatistics.AddCSLCallStatisticsDetails(string.Empty, "REST_Shopping", response.CartId, callDurations, "Traveler/ValidateMPNameMismatch_CFOP", request.SessionId);

                }
                catch { }
            }
            return response;
        }

        private MOBCPTraveler GetMatchedTraverler(List<MOBCPTraveler> allEligibleTravelersCSL, MOBCPTraveler traveler)
        {
            MOBCPTraveler matchedTraveler = null;
            foreach (var eligibleTraveler in allEligibleTravelersCSL)
            {
                if (CompareTravelerProperties(traveler, eligibleTraveler))
                {
                    matchedTraveler = eligibleTraveler;
                    break;
                }
            }
            return matchedTraveler;
        }
        private bool CompareTravelerProperties(MOBCPTraveler currentTravelrFromRequestedTravelers, MOBCPTraveler travelerCSL)
        {
            if (currentTravelrFromRequestedTravelers.IsExtraSeat == true) return false;
            bool itsMatched = false;
            itsMatched = (
                            (currentTravelrFromRequestedTravelers.FirstName ?? "") == (travelerCSL.FirstName ?? "") &&
                            (currentTravelrFromRequestedTravelers.LastName ?? "") == (travelerCSL.LastName ?? "") &&
                            (currentTravelrFromRequestedTravelers.MiddleName ?? "") == (travelerCSL.MiddleName ?? "") &&
                            (currentTravelrFromRequestedTravelers.Suffix ?? "") == (travelerCSL.Suffix ?? "")
                         );

            return itsMatched;
        }

    }

}
