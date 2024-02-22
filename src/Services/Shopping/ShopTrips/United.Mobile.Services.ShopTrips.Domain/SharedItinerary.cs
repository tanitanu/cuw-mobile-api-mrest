using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.Profile;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.UnfinishedBooking;
using United.Services.FlightShopping.Common;
using United.Services.Loyalty.Preferences.Common;
using United.Utility.Helper;

namespace United.Mobile.Services.ShopTrips.Domain
{
    public class SharedItinerary : ISharedItinerary
    {
        private readonly ICacheLog<SharedItinerary> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly ICustomerPreferencesService _customerPreferencesService;
        private readonly IDPService _dPService;
        private readonly ISessionHelperService _sessionHelperService;
        public SharedItinerary(ICacheLog<SharedItinerary> logger
            , IConfiguration configuration
            , IHeaders headers
            , IShoppingUtility shoppingUtility
            , ICustomerPreferencesService customerPreferencesService
            , IDPService dPService
            , ISessionHelperService sessionHelperService)
        {
            _logger = logger;
            _configuration = configuration;
            _headers = headers;
            _shoppingUtility = shoppingUtility;
            _customerPreferencesService = customerPreferencesService;
            _dPService = dPService;
            _sessionHelperService= sessionHelperService;
        }

        public async Task<MOBSHOPSelectUnfinishedBookingRequest> GetSharedTripItinerary(Session session, MetaSelectTripRequest request)
        {
            MOBSHOPSelectUnfinishedBookingRequest tripShareBooking = new MOBSHOPSelectUnfinishedBookingRequest();
            MOBSHOPUnfinishedBooking unfinishedBooking = new MOBSHOPUnfinishedBooking();

            Model.ShopTrips.GetSharedItineraryDataModel sharedTrip = await GetSharedTrip(session, request);
            //response.SharedItineraryList[0].SharedItinerary.PaxInfoList = new List<SerializableShareItinerary.PaxInfo> { new SerializableShareItinerary.PaxInfo() { PaxType = SerializableShareItinerary.PaxType.Adult, DateOfBirth = "7/23/1995",PaxTypeCode="1" } };

            unfinishedBooking = MapToMOBSHOPUnfinishedBooking(sharedTrip, request);

            tripShareBooking.SelectedUnfinishBooking = unfinishedBooking;
            tripShareBooking.MileagePlusAccountNumber = request.MileagePlusAccountNumber;
            tripShareBooking.PremierStatusLevel = request.PremierStatusLevel;
            tripShareBooking.CustomerId = request.CustomerId;
            tripShareBooking.Application = request.Application;
            tripShareBooking.SelectedUnfinishBooking.CatalogItems = request.CatalogItems;
            if (_configuration.GetValue<bool>("EnableOmniChannelCartMVP1"))
            {
                tripShareBooking.DeviceId = request.DeviceId;
            }
            return tripShareBooking;

        }


        public async Task<Model.ShopTrips.GetSharedItineraryDataModel> GetSharedTrip(Session session, MetaSelectTripRequest request)
        {
            //#TakeTokenFromSession
            // string token = await _dPService.GetAnonymousToken(request.Application.Id, request.DeviceId, _configuration);
            var response = await _customerPreferencesService.GetSharedTrip<Model.ShopTrips.SharedItineraryDataModel>(session.Token, request.SharedTripId, session.SessionId).ConfigureAwait(false);

            if (response != null)
            {

                if (response != null && response.Status.Equals(United.Services.Loyalty.Common.Common.Constants.StatusType.Success))
                {
                    if (response.SharedItineraryList != null && response.SharedItineraryList.Count > 0)
                    {
                        return response.SharedItineraryList[0];
                    }
                    else
                    {
                        throw new MOBUnitedException(_configuration.GetValue<string>("MetaTripExceptionMessage"));
                    }

                }
                else
                {
                    if (response != null && response.Errors != null && response.Errors.Count > 0)
                    {
                        throw new Exception(string.Join(" ", response.Errors.Select(err => err.UserFriendlyMessage)));
                    }

                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

        }

        private MOBSHOPUnfinishedBooking MapToMOBSHOPUnfinishedBooking(Model.ShopTrips.GetSharedItineraryDataModel getSharedItineraryDataModel, MetaSelectTripRequest request)
        {
            var ub = getSharedItineraryDataModel.SharedItinerary;
            MOBSHOPUnfinishedBooking mobEntry = new MOBSHOPUnfinishedBooking();

            mobEntry.SearchExecutionDate = new[] { getSharedItineraryDataModel.InsertTimestamp, getSharedItineraryDataModel.UpdateTimestamp }.FirstOrDefault(x => !string.IsNullOrEmpty(x));
            mobEntry.TravelerTypes = new List<MOBTravelerType>();
            ShopStaticUtility.GetTravelTypesInfoFromShop(ub, mobEntry);
            mobEntry.CountryCode = ub.CountryCode;
            mobEntry.SearchType = ShopStaticUtility.GetSeachTypeSelection((SearchType)ub.SearchTypeSelection);
            mobEntry.Trips = ub.Trips.Select(_shoppingUtility.MapToMOBSHOPUnfinishedBookingTrip).ToList();
            mobEntry.Id = ub.AccessCode;
            if (_shoppingUtility.EnableSavedTripShowChannelTypes(request.Application.Id, request.Application.Version.Major)) // Map channel
                mobEntry.ChannelType = ub.ChannelType;

            return mobEntry;

        }
        public async Task<string> InsertSharedItinerary(MOBSHOPReservation reservation, MOBRequest mobRequest, string sessionId, string logAction)
        {
            string sharedItinerayAccessCode = string.Empty;
            //build request
            InsertSharedItineraryData sharedItineraryRequest = new InsertSharedItineraryData();
            var cslRequest = new InsertSharedItineraryData
            {
                InsertID = mobRequest.DeviceId,
                InsertSharedItinerary = MapToSerializableSavedItineraryTtypes(MapUnfinishedBookingFromMOBSHOPReservation(reservation), mobRequest.LanguageCode, reservation)
            };
            //Call CSL
            string actioName = "/SharedItinerary";
            //#TakeTokenFromSession
            // string token = await _dPService.GetAnonymousToken(mobRequest.Application.Id, mobRequest.DeviceId, _configuration);
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName });
            var jsonRequest = JsonConvert.SerializeObject(cslRequest);
            var response = await _customerPreferencesService.GetSharedItinerary(session.Token, actioName, jsonRequest, sessionId).ConfigureAwait(false);         
            var cslResponse = string.IsNullOrEmpty(response) ? null
                : JsonConvert.DeserializeObject<SharedItineraryResponseData>(response);
            if (cslResponse != null && cslResponse.Status.Equals(United.Services.Loyalty.Common.Common.Constants.StatusType.Success) && !string.IsNullOrEmpty(cslResponse.AccessCode))
            {
                //    response
                sharedItinerayAccessCode = cslResponse.AccessCode;
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            return sharedItinerayAccessCode;
        }
        private SerializableSharedItinerary MapToSerializableSavedItineraryTtypes(MOBSHOPUnfinishedBooking ub, string languageCode, MOBSHOPReservation reservation)
        {
            decimal fligtPrice = decimal.Zero;
            var cslUB = new SerializableSharedItinerary
            {
                AccessCode = ub.Id,
                AwardTravel = false,
                ChannelType = _configuration.GetValue<string>("Shopping - ChannelType"),
                CountryCode = ub.CountryCode,
                InitialShop = true,
                LangCode = languageCode,
                //LoyaltyId = mpNumber,
                NGRP = false,
                SearchTypeSelection = (SerializableShareItinerary.SearchType)GetSearchType(ub.SearchType),
                Trips = MapToListOfCSLUnfinishedBookingTrips(ub.Trips),
                TrueAvailability = true
            };

            foreach (var price in reservation?.Prices)
            {
                if (price != null && !string.IsNullOrEmpty(price.DisplayType) && price.DisplayType.Equals("TOTAL", StringComparison.OrdinalIgnoreCase))
                {
                    decimal.TryParse(price?.DisplayValue, out fligtPrice);
                    break;
                }
            }

            if (cslUB?.Trips?.FirstOrDefault()?.Flights?.Count > 0 && fligtPrice > decimal.Zero)
            {
                 cslUB.Trips.FirstOrDefault().Flights.FirstOrDefault().Price = fligtPrice;
            }

            Func<United.Services.FlightShopping.Common.PaxType, int, SerializableShareItinerary.PaxInfo> getPax = (type, subtractYear) => new SerializableShareItinerary.PaxInfo { PaxType = (SerializableShareItinerary.PaxType)type, DateOfBirth = DateTime.Today.AddYears(subtractYear).ToShortDateString() };
            cslUB.PaxInfoList = new List<SerializableShareItinerary.PaxInfo>();
            GetPaxInfoUnfinishBooking(ub, cslUB, getPax);

            return cslUB;
        }
        private List<SerializableShareItinerary.Trip> MapToListOfCSLUnfinishedBookingTrips(List<MOBSHOPUnfinishedBookingTrip> trips)
        {
            if (trips == null)
                return null;

            var clsTrips = new List<SerializableShareItinerary.Trip>();
            if (!trips.Any())
                return clsTrips;

            foreach (var mobTrip in trips)
            {
                var trip = new SerializableShareItinerary.Trip
                {
                    DepartDate = mobTrip.DepartDate,
                    DepartTime = mobTrip.DepartTime,
                    Destination = mobTrip.Destination,
                    Origin = mobTrip.Origin,
                    FlightCount = mobTrip.Flights.Count(),
                    Flights = mobTrip.Flights.Select(MapToCSLUnfinishedBookingFlight).ToList(),
                };
                GetFlattedFlightsForCOGorThruFlights(trip);
                clsTrips.Add(trip);
            }

            return clsTrips;
        }
        private SerializableShareItinerary.Flight MapToCSLUnfinishedBookingFlight(MOBSHOPUnfinishedBookingFlight ubFlight)
        {
            var flight = new SerializableShareItinerary.Flight
            {
                BookingCode = ubFlight.BookingCode,
                DepartDateTime = ubFlight.DepartDateTime,
                Destination = ubFlight.Destination,
                Origin = ubFlight.Origin,
                FlightNumber = ubFlight.FlightNumber,
                MarketingCarrier = ubFlight.MarketingCarrier,
                ProductType = ubFlight.ProductType
            };

            if (ubFlight.Products?.FirstOrDefault()?.Prices?.FirstOrDefault()?.Amount != 0)
            {
                flight.Price = ubFlight.Products?.FirstOrDefault()?.Prices?.FirstOrDefault()?.Amount;
            }

            if (ubFlight.Connections == null)
                return flight;

            ubFlight.Connections.ForEach(x => flight.Connections.Add(MapToCSLUnfinishedBookingFlight(x)));

            return flight;
        }
        private void GetFlattedFlightsForCOGorThruFlights(SerializableShareItinerary.Trip trip)
        {
            if (_configuration.GetValue<bool>("SavedTripThruOrCOGFlightBugFix")
              && trip.Flights.Any()
              && trip.Flights.GroupBy(x => x.FlightNumber).Any(g => g.Count() > 1))
            {
                for (int i = 0; i < trip.Flights.Count - 1; i++)
                {
                    if (trip.Flights[i].FlightNumber == trip.Flights[i + 1].FlightNumber)
                    {
                        trip.Flights[i].Destination = trip.Flights[i + 1].Destination;
                        trip.Flights.RemoveAt(i + 1);
                        i = -1;
                    }
                }
            }
        }
        private SerializableShareItinerary.SearchType GetSearchType(string searchTypeSelection)
        {
            SerializableShareItinerary.SearchType searchType = SerializableShareItinerary.SearchType.ValueNotSet;

            if (string.IsNullOrEmpty(searchTypeSelection))
                return searchType;

            switch (searchTypeSelection.Trim().ToUpper())
            {
                case "OW":
                    searchType = SerializableShareItinerary.SearchType.OneWay;
                    break;
                case "RT":
                    searchType = SerializableShareItinerary.SearchType.RoundTrip;
                    break;
                case "MD":
                    searchType = SerializableShareItinerary.SearchType.MultipleDestination;
                    break;
                default:
                    searchType = SerializableShareItinerary.SearchType.ValueNotSet;
                    break;
            }

            return searchType;
        }
        private static void GetPaxInfoUnfinishBooking(MOBSHOPUnfinishedBooking ub, SerializableSharedItinerary cslUB, Func<United.Services.FlightShopping.Common.PaxType, int, SerializableShareItinerary.PaxInfo> getPax)
        {
            if (ub.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Adult.ToString()) && ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Adult.ToString()).Count > 0)
                cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Adult.ToString()).Count).Select(x => getPax(PaxType.Adult, -20)));

            if (ub.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Senior.ToString()) && ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Senior.ToString()).Count > 0)
                cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Senior.ToString()).Count).Select(x => getPax(PaxType.Senior, -67)));

            if (ub.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child2To4.ToString()) && ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child2To4.ToString()).Count > 0)
                cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child2To4.ToString()).Count).Select(x => getPax(PaxType.Child01, -3)));

            if (ub.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child5To11.ToString()) && ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child5To11.ToString()).Count > 0)
                cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child5To11.ToString()).Count).Select(x => getPax(PaxType.Child02, -8)));

            if (ub.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child12To17.ToString()) && ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child12To17.ToString()).Count > 0)
                cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child12To17.ToString()).Count).Select(x => getPax(PaxType.Child03, -15)));

            if (ub.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child12To14.ToString()) && ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child12To14.ToString()).Count > 0)
                cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child12To14.ToString()).Count).Select(x => getPax(PaxType.Child04, -13)));

            if (ub.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child15To17.ToString()) && ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child15To17.ToString()).Count > 0)
                cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child15To17.ToString()).Count).Select(x => getPax(PaxType.Child05, -16)));

            if (ub.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.InfantLap.ToString()) && ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.InfantLap.ToString()).Count > 0)
                cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.InfantLap.ToString()).Count).Select(x => getPax(PaxType.InfantLap, -1)));

            if (ub.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.InfantSeat.ToString()) && ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.InfantSeat.ToString()).Count > 0)
                cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.TravelerTypes.First(t => t.TravelerType == PAXTYPE.InfantSeat.ToString()).Count).Select(x => getPax(PaxType.InfantSeat, -1)));
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
            };
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    result.SearchExecutionDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time")).ToString("G");
                }
                else
                {
                    result.SearchExecutionDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Chicago")).ToString("G");
                }
                result.Trips = reservation.Trips.Select(MapToUnfinishedBookingTripFromMOBSHOPTrip).ToList();
            }
            catch (TimeZoneNotFoundException ex)
            {
                _logger.LogError("GetShareTrip - The registry does not define the Central Standard Time zone.- Error {exceptionstack}, {Message}, {sessionId} ", JsonConvert.SerializeObject(ex), ex.Message, reservation?.SessionId);
            }
            catch (InvalidTimeZoneException ex)
            {
                _logger.LogError("GetShareTrip - Registry data on the Central Standard Time zone has been corrupted.- Error {exceptionstack}, {Message}, {sessionId} ", JsonConvert.SerializeObject(ex), ex.Message, reservation?.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetShareTrip Error {exceptionstack}, {Message}, {sessionId} ", JsonConvert.SerializeObject(ex), ex.Message,reservation?.SessionId);
            }

            return result;
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
                ProductType = shopFlight.ShoppingProducts.Any() ? shopFlight.ShoppingProducts.First().Type : string.Empty,
            };

            if (shopFlight.Connections != null)
                ubFlight.Connections = shopFlight.Connections.Select(MapToUnfinishedBookingFlightFromMOBSHOPFlight).ToList();

            return ubFlight;
        }
    }
}
