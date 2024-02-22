using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using United.Common.Helper.PageProduct;
using United.Definition;
using United.Definition.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.MerchandizeService;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.SSR;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Model.Shopping.Misc;
using United.Mobile.Model.Shopping.UnfinishedBooking;
using United.Service.Presentation.CommonModel;
using United.Service.Presentation.ProductModel;
using United.Service.Presentation.ProductResponseModel;
using United.Service.Presentation.ReferenceDataRequestModel;
using United.Service.Presentation.ReferenceDataResponseModel;
using United.Service.Presentation.ReservationModel;
using United.Service.Presentation.SecurityResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.Customer.Preferences.Common;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.Extensions;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Services.FlightShopping.Common.LMX;
using United.Utility.Enum;
using United.Utility.Helper;
using Aircraft = United.Services.FlightShopping.Common.Aircraft;
using Amenity = United.Services.FlightShopping.Common.Amenity;
using Characteristic = United.Service.Presentation.CommonModel.Characteristic;
using CreditType = United.Mobile.Model.Shopping.CreditType;
using CreditTypeColor = United.Mobile.Model.Shopping.CreditTypeColor;
using EquipmentDisclosure = United.Services.FlightShopping.Common.EquipmentDisclosure;
using GetSavedItineraryDataModel = United.Services.Customer.Preferences.Common.GetSavedItineraryDataModel;
using InsertSavedItineraryData = United.Services.Customer.Preferences.Common.InsertSavedItineraryData;
using MOBSHOPFlattenedFlight = United.Mobile.Model.Shopping.MOBSHOPFlattenedFlight;
using MOBSHOPTax = United.Mobile.Model.Shopping.MOBSHOPTax;
using Product = United.Services.FlightShopping.Common.Product;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using SavedItineraryDataModel = United.Services.Customer.Preferences.Common.SavedItineraryDataModel;
using SerializableSavedItinerary = United.Services.Customer.Preferences.Common.SerializableSavedItinerary;
using SubCommonData = United.Services.Customer.Preferences.Common.SubCommonData;
using TravelOption = United.Mobile.Model.Shopping.TravelOption;
using Trip = United.Services.FlightShopping.Common.Trip;
using Microsoft.AspNetCore.Http;
using United.Service.Presentation.ReferenceDataModel;
using MOBRegisterTravelersResponse = United.Mobile.Model.Travelers.MOBRegisterTravelersResponse;
using United.Mobile.Model.CSLModels;
namespace United.Common.Helper.Shopping
{
    public class UnfinishedBooking : IUnfinishedBooking
    {
        private readonly ICacheLog<UnfinishedBooking> _logger;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IConfiguration _configuration;
        private readonly IFlightShoppingService _flightShoppingService;
        private readonly ICustomerPreferencesService _customerPreferencesService;
        private readonly IDPService _dPService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOmniCart _omniCart;
        private readonly ITravelerCSL _travelerCSL;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly IReferencedataService _referencedataService;
        private readonly IPurchaseMerchandizingService _purchaseMerchandizingService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ICachingService _cachingService;
        private readonly PKDispenserPublicKey _pKDispenserPublicKey;
        private AirportDetailsList airportsList;
        private MOBSHOPUnfinishedBookingComparer comparer = new MOBSHOPUnfinishedBookingComparer();
        private string shopPinDownActionName = "shoppindown";
        private string shopPinDownErrorSeparator = ",";
        private string shopPinDownErrorMajorAndMinorCodesSeparator = "^";
        private string savedUnfinishedBookingActionName = "SavedItinerary";
        private string savedUnfinishedBookingAugumentName = "CustomerId";
        private readonly IPNRRetrievalService _pNRRetrievalService;
        private readonly IPKDispenserService _pKDispenserService;
        private readonly IGMTConversionService _gMTConversionService;
        private string CURRENCY_TYPE_MILES = "miles";
        private string PRICING_TYPE_CLOSE_IN_FEE = "CLOSEINFEE";
        private readonly IFFCShoppingcs _ffcShoppingcs;
        private readonly IHeaders _headers;
        private readonly ILogger<UnfinishedBooking> _logger1;
        private readonly IFeatureSettings _featureSettings;
        private readonly IFeatureToggles _featureToggles;
        public UnfinishedBooking(ICacheLog<UnfinishedBooking> logger
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility
            , IConfiguration configuration
            , IFlightShoppingService flightShoppingService
            , ICustomerPreferencesService customerPreferencesService
            , IDPService dPService
            , IShoppingCartService shoppingCartService
            , IOmniCart omniCart
            , ITravelerCSL travelerCSL
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , IReferencedataService referencedataService
            , IPurchaseMerchandizingService purchaseMerchandizingService
            , IPNRRetrievalService pNRRetrievalService
            , IPKDispenserService pKDispenserService
            , IDynamoDBService dynamoDBService
            , ICachingService cachingService
            , IGMTConversionService gMTConversionService
            , IFFCShoppingcs ffcShoppingcs
            , IHeaders headers
            , ILogger<UnfinishedBooking> logger1
            , IFeatureSettings featureSettings
            , IFeatureToggles featureToggles)
        {
            _logger = logger;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
            _configuration = configuration;
            _flightShoppingService = flightShoppingService;
            _customerPreferencesService = customerPreferencesService;
            _dPService = dPService;
            _shoppingCartService = shoppingCartService;
            _omniCart = omniCart;
            _travelerCSL = travelerCSL;
            airportsList = null;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _referencedataService = referencedataService;
            _purchaseMerchandizingService = purchaseMerchandizingService;
            _pNRRetrievalService = pNRRetrievalService;
            _pKDispenserService = pKDispenserService;
            _dynamoDBService = dynamoDBService;
            _cachingService = cachingService;
            _gMTConversionService = gMTConversionService;
            _ffcShoppingcs = ffcShoppingcs;
            _headers = headers;
            _pKDispenserPublicKey = new PKDispenserPublicKey(_configuration, _cachingService, _dPService, _pKDispenserService, headers);
            _logger1 = logger1;
            _featureSettings = featureSettings;
            _featureToggles = featureToggles;
        }

        public string GetFlightShareMessage(MOBSHOPReservation reservation, string cabinType)
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
                foreach (MOBSHOPFlattenedFlight flattenedFlight in trip.FlattenedFlights)
                {
                    if (string.IsNullOrEmpty(cabinType))
                    {
                        cabinType = flattenedFlight.Flights[0].Cabin.ToUpper().Trim() == "COACH" ? "Economy" : flattenedFlight.Flights[0].Cabin;
                    }
                    foreach (MOBSHOPFlight flight in flattenedFlight.Flights)
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
        public bool IsCubaAirportCodeExist(string origin, string destination, List<string> CubaAirports)
        {
            bool isCubaFight = false;
            if (CubaAirports != null && (CubaAirports.Exists(p => p == origin) || CubaAirports.Exists(p => p == destination)))
            {
                isCubaFight = true;
            }
            return isCubaFight;
        }

        public bool IsOneFlexibleSegmentExist(List<MOBSHOPTrip> trips)
        {
            bool isFlexibleSegment = true;
            if (trips != null)
            {
                foreach (MOBSHOPTrip trip in trips)
                {
                    #region
                    if (trip.FlattenedFlights != null)
                    {
                        foreach (MOBSHOPFlattenedFlight flattenedFlight in trip.FlattenedFlights)
                        {
                            if (flattenedFlight.Flights != null && flattenedFlight.Flights.Count > 0)
                            {
                                foreach (MOBSHOPFlight flight in flattenedFlight.Flights)
                                {
                                    //TFS 53620:Booking - Certain Flights From IAH- ANC Are Displaying An Error Message
                                    if (flight.ShoppingProducts != null && flight.ShoppingProducts.Count > 0)
                                    {
                                        foreach (MOBSHOPShoppingProduct product in flight.ShoppingProducts)
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

        public async Task<MOBResReservation> PopulateReservation(Session session, Service.Presentation.ReservationModel.Reservation reservation)
        {
            if (reservation == null)
                return null;

            var mobReservation = new MOBResReservation
            {
                FlightSegments = PopulateReservationFlightSegments(reservation.FlightSegments),
                Travelers = PopulateReservationTravelers(reservation.Travelers),
                IsRefundable = reservation.Characteristic.Any(c => c.Code.ToUpper().Trim() == "REFUNDABLE" && United.Utility.Helper.SafeConverter.ToBoolean(c.Value)),
            };
            mobReservation.ISInternational = mobReservation.FlightSegments.Any(item => item.FlightSegment.IsInternational.ToUpper().Trim() == "TRUE");

            var persistedCSLReservation = new CSLReservation(_configuration, _cachingService) { SessionId = session.SessionId, Reservation = mobReservation };
            await _sessionHelperService.SaveSession(persistedCSLReservation, session.SessionId, new List<string> { session.SessionId, persistedCSLReservation.ObjectName }, persistedCSLReservation.ObjectName).ConfigureAwait(false);

            return mobReservation;
        }

        private List<MOBResTraveler> PopulateReservationTravelers(IEnumerable<Service.Presentation.ReservationModel.Traveler> travelers)
        {
            if (travelers == null)
                return null;

            var mobTravelers = new List<MOBResTraveler>();
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

            return mobTravelers;
        }

        private List<MOBSegReservationFlightSegment> PopulateReservationFlightSegments(IEnumerable<ReservationFlightSegment> flightSegments)
        {
            if (flightSegments == null)
                return null;

            return flightSegments.Select(s => new MOBSegReservationFlightSegment { FlightSegment = PopulateFlightSegment(s.FlightSegment) }).ToList();
        }

        private SegFlightSegment PopulateFlightSegment(Service.Presentation.SegmentModel.FlightSegment segment)
        {
            if (segment == null)
                return null;

            return new SegFlightSegment
            {
                ArrivalAirport = PopulateAirport(segment.ArrivalAirport),
                BookingClasses = PopulateBookingClasses(segment.BookingClasses),
                DepartureAirport = PopulateAirport(segment.DepartureAirport),
                DepartureDateTime = segment.DepartureDateTime,
                FlightNumber = segment.FlightNumber,
                OperatingAirlineCode = segment.OperatingAirlineCode,
                OperatingAirlineName = segment.OperatingAirlineName,
                IsInternational = segment.IsInternational
            };
        }

        private List<ComBookingClass> PopulateBookingClasses(IEnumerable<BookingClass> bookingClasses)
        {
            if (bookingClasses == null || !bookingClasses.Any())
                return null;

            return bookingClasses.Select(c => new ComBookingClass { Code = c.Code }).ToList();
        }

        private MOBTMAAirport PopulateAirport(Service.Presentation.CommonModel.AirportModel.Airport arrivalAirport)
        {
            if (arrivalAirport == null)
                return null;

            return new MOBTMAAirport { IATACode = arrivalAirport.IATACode, Name = arrivalAirport.Name, ShortName = arrivalAirport.ShortName };
        }

        public void AssignMissingPropertiesfromRegisterFlightsResponse(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse registerFlightsResponse)
        {
            if (!registerFlightsResponse.IsNullOrEmpty())
            {
                registerFlightsResponse.FareLockResponse = flightReservationResponse?.FareLockResponse;
                registerFlightsResponse.Upsells = flightReservationResponse?.Upsells;
                registerFlightsResponse.LastBBXSolutionSetId = flightReservationResponse?.LastBBXSolutionSetId;
            }
        }

        private List<MOBSHOPPrice> AdjustTotal(List<MOBSHOPPrice> prices)
        {
            CultureInfo ci = null;

            List<MOBSHOPPrice> newPrices = prices;
            double fee = 0;
            foreach (var p in newPrices)
            {
                if (ci == null)
                {
                    ci = TopHelper.GetCultureInfo(p.CurrencyCode);
                }

                if (fee == 0)
                {
                    foreach (var q in newPrices)
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

        public List<MOBSHOPTax> GetTaxAndFees(List<DisplayPrice> prices, int numPax, bool isReshopChange = false)
        {
            List<MOBSHOPTax> taxsAndFees = new List<MOBSHOPTax>();
            CultureInfo ci = null;
            decimal taxTotal = 0.0M;
            bool isEnableOmniCartMVP2Changes = _configuration.GetValue<bool>("EnableOmniCartMVP2Changes");

            foreach (var price in prices)
            {
                if (price.SubItems != null && price.SubItems.Count > 0
                    && price.Type.Trim().ToUpper() != "RBF" // Added by Hasnan - # 167553 - 10/04/2017
                   )
                {
                    foreach (var subItem in price.SubItems)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(subItem.Currency);
                        }

                        MOBSHOPTax taxNfee = new MOBSHOPTax();
                        taxNfee.CurrencyCode = subItem.Currency;
                        taxNfee.Amount = subItem.Amount;
                        taxNfee.DisplayAmount = TopHelper.FormatAmountForDisplay(taxNfee.Amount, ci, false);
                        taxNfee.TaxCode = subItem.Type;
                        taxNfee.TaxCodeDescription = subItem.Description;
                        taxsAndFees.Add(taxNfee);

                        taxTotal += taxNfee.Amount;
                    }
                }
                else if (price.Type.Trim().ToUpper() == "RBF") //Reward Booking Fee
                {
                    if (ci == null)
                    {
                        ci = TopHelper.GetCultureInfo(price.Currency);
                    }
                    MOBSHOPTax taxNfee = new MOBSHOPTax();
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
                MOBSHOPTax tnf = new MOBSHOPTax();
                tnf.CurrencyCode = taxsAndFees[0].CurrencyCode;
                tnf.Amount = taxTotal;
                tnf.DisplayAmount = TopHelper.FormatAmountForDisplay(tnf.Amount, ci, false);
                tnf.TaxCode = "PERPERSONTAX";
                tnf.TaxCodeDescription = string.Format("{0} adult{1}: {2}{3}", numPax, numPax > 1 ? "s" : "", tnf.DisplayAmount, isEnableOmniCartMVP2Changes ? "/person" : " per person");
                taxsAndFees.Insert(0, tnf);

                //add grand total for all taxes
                MOBSHOPTax tnfTotal = new MOBSHOPTax();
                tnfTotal.CurrencyCode = taxsAndFees[0].CurrencyCode;
                tnfTotal.Amount = taxTotal * numPax;
                tnfTotal.DisplayAmount = TopHelper.FormatAmountForDisplay(tnfTotal.Amount, ci, false);
                tnfTotal.TaxCode = "TOTALTAX";
                tnfTotal.TaxCodeDescription = "Taxes and Fees Total";
                taxsAndFees.Add(tnfTotal);

            }

            return taxsAndFees;
        }

        public List<Mobile.Model.Shopping.TravelOption> GetTravelOptions(DisplayCart displayCart)
        {
            List<Mobile.Model.Shopping.TravelOption> travelOptions = null;
            if (displayCart != null && displayCart.TravelOptions != null && displayCart.TravelOptions.Count > 0)
            {
                CultureInfo ci = null;
                travelOptions = new List<Mobile.Model.Shopping.TravelOption>();
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

                    Mobile.Model.Shopping.TravelOption travelOption = new Mobile.Model.Shopping.TravelOption();
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
                    subItems.Add(subItem);
                }
            }

            return subItems;
        }

        public FareLock GetFareLockOptions(Service.Presentation.ProductResponseModel.ProductOffer cslFareLock, List<DisplayPrice> prices, List<MOBItem> catalogItems = null, int appId = 0, string appVersion = "")
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
                                    }                                    //Retrieving the ProductId and ProductCode inorder for the client to send it back to us when calling the RegisterFairLock.
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

        public async Task<FlightReservationResponse> RegisterFlights(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, Session session, MOBRequest request)
        {
            string flow = !session.IsNullOrEmpty() && session.IsReshopChange ? FlowType.RESHOP.ToString() : FlowType.BOOKING.ToString();

            var registerFlightRequest = BuildRegisterFlightRequest(flightReservationResponse, flow, request);
            if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == TravelType.TPBooking.ToString())
            {
                var cslShopRequest = _sessionHelperService.GetSession<CSLShopRequest>(session.SessionId, new CSLShopRequest().ObjectName, new List<string> { session.SessionId, new CSLShopRequest().ObjectName })?.Result?.ShopRequest;

                registerFlightRequest.TravelPlanId = cslShopRequest?.TravelPlanId;
                registerFlightRequest.TravelPlanCartId = cslShopRequest?.TravelPlanCartId;
            }
            if (session.IsCorporateBooking && _shoppingUtility.IsEnableU4BCorporateBooking(request.Application.Id, request.Application.Version.Major))
            {
                try
                {
                    bool isEnableU4BTravelAddONPolicy = await _shoppingUtility.IsEnableU4BTravelAddONPolicy(session.IsCorporateBooking, request.Application.Id, request.Application.Version.Major, session.CatalogItems).ConfigureAwait(false);


                    Collection<United.Service.Presentation.ReservationModel.CorporateTravelPolicy> corpPolicy = new Collection<Service.Presentation.ReservationModel.CorporateTravelPolicy>();
                    United.Service.Presentation.ReservationModel.CorporateData corpData = new Service.Presentation.ReservationModel.CorporateData();

                    if (!isEnableU4BTravelAddONPolicy)
                    {
                        var _corpPolicyResponse = await _sessionHelperService.GetSession<United.CorporateDirect.Models.CustomerProfile.CorpPolicyResponse>(request.DeviceId + session.MileagPlusNumber, ObjectNames.CSLCorporatePolicyResponse, new List<string> { request.DeviceId + session.MileagPlusNumber, ObjectNames.CSLCorporatePolicyResponse }).ConfigureAwait(false);
                        corpPolicy = GetCorporateTravelPolicy(session.SessionId, "CSLCorporatePolicyResponse", _corpPolicyResponse);
                    }

                    var _corprofileResponse = await _sessionHelperService.GetSession<United.CorporateDirect.Models.CustomerProfile.CorpProfileResponse>(request.DeviceId + session.MileagPlusNumber, ObjectNames.CSLCorpProfileResponse, new List<string> { request.DeviceId + session.MileagPlusNumber, ObjectNames.CSLCorpProfileResponse }).ConfigureAwait(false);
                    corpData = GetCorporateProfileResponse(session.SessionId, "CSLCorpProfileResponse", _corprofileResponse);

                    foreach (var traveler in registerFlightRequest?.Reservation?.Travelers)
                    {
                        if (!isEnableU4BTravelAddONPolicy)
                        {
                            traveler.TravelPolicies = corpPolicy != null && corpPolicy.Count > 0 ? corpPolicy : null;
                        }
                        traveler.CorporateData = corpData != null && !string.IsNullOrEmpty(corpData.CompanyName) ? corpData : null;
                    }
                }
                catch (Exception ex)
                {

                }
            }
            string jsonRequest = JsonConvert.SerializeObject(registerFlightRequest);
            United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightresponse = new United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse();

            string actionName = "RegisterFlights";


            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cSLCallDurationstopwatch1;
            cSLCallDurationstopwatch1 = new Stopwatch();
            cSLCallDurationstopwatch1.Reset();
            cSLCallDurationstopwatch1.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            // string token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration).ConfigureAwait(false);
            var response = await _shoppingCartService.GetShoppingCartInfo<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(session.Token, actionName, jsonRequest, session.SessionId).ConfigureAwait(false);


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
                if ((_omniCart.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, true)
                    || EnableAdvanceSearchCouponBooking(request.Application.Id, request.Application.Version.Major))
                    && flow == FlowType.BOOKING.ToString())
                {
                    var persistShoppingCart = new MOBShoppingCart();
                    persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(session.SessionId, persistShoppingCart.ObjectName, new List<string> { session.SessionId, persistShoppingCart.ObjectName }).ConfigureAwait(false);
                    if (persistShoppingCart == null)
                    {
                        persistShoppingCart = new MOBShoppingCart();
                    }
                    persistShoppingCart.Products = await _shoppingUtility.ConfirmationPageProductInfo(response, false, request.Application, null, flow);
                    double price = _shoppingUtility.GetGrandTotalPriceForShoppingCart(false, response, false, flow);
                    persistShoppingCart.TotalPrice = String.Format("{0:0.00}", price);
                    persistShoppingCart.DisplayTotalPrice = Decimal.Parse(price.ToString().Trim()).ToString("c", new CultureInfo("en-us"));
                    persistShoppingCart.PromoCodeDetails = AddAFSPromoCodeDetails(response.DisplayCart);
                    if(await _shoppingUtility.IsEnableAdvanceSearchOfferCode(request.Application.Id, request.Application.Version.Major,session?.CatalogItems).ConfigureAwait(false))
                        persistShoppingCart.Offers = _shoppingUtility.GetAFSOfferDetails(response.DisplayCart);
                    await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, session.SessionId, new List<string> { session.SessionId, persistShoppingCart.ObjectName }, persistShoppingCart.ObjectName).ConfigureAwait(false);
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            return response;

        }

        private CorporateData GetCorporateProfileResponse(string sessionId, string objectName, United.CorporateDirect.Models.CustomerProfile.CorpProfileResponse _corprofileResponse)
        {
            United.Service.Presentation.ReservationModel.CorporateData corpData = new Service.Presentation.ReservationModel.CorporateData();

            var corporateData = _corprofileResponse != null ? _corprofileResponse?.Profiles?.FirstOrDefault()?.CorporateData : null;

            if (corporateData != null && !string.IsNullOrEmpty(corporateData.CompanyName))
            {
                corpData.CompanyName = corporateData.CompanyName;
                corpData.VendorId = corporateData.VendorId;
                corpData.VendorName = corporateData.VendorName;
                corpData.DiscountCode = corporateData.DiscountCode;
                corpData.IsValid = corporateData.IsValid;
                corpData.LeisureCode = corporateData.LeisureCode;
                corpData.UserId = corporateData.UserId;
                corpData.UCSID = corporateData.UCSID;
                corpData.CompanyOrgId = corporateData.CompanyOrgId;
                corpData.IdFromPartner = corporateData.IdFromPartner;
                corpData.VendorRefId = corporateData.VendorRefId;
                corpData.IsArranger = corporateData.IsArranger;
            }

            return corpData;
        }
        private Collection<CorporateTravelPolicy> GetCorporateTravelPolicy(string sessionId, string objectName, United.CorporateDirect.Models.CustomerProfile.CorpPolicyResponse _corpPolicyResponse)
        {
            Collection<United.Service.Presentation.ReservationModel.CorporateTravelPolicy> corpPolicy = new Collection<Service.Presentation.ReservationModel.CorporateTravelPolicy>();

            var corporateTravelPolicy = _corpPolicyResponse != null ? _corpPolicyResponse?.TravelPolicies?.FirstOrDefault() : null;

            if (corporateTravelPolicy != null && corporateTravelPolicy?.TravelCabinRestrictions?.Count > 0)
            {
                United.Service.Presentation.ReservationModel.CorporateTravelPolicy corpTravelPolicy = new Service.Presentation.ReservationModel.CorporateTravelPolicy();
                corpTravelPolicy.CountryCode = corporateTravelPolicy.CountryCode;
                corpTravelPolicy.CurrencyCode = corporateTravelPolicy.CurrencyCode;
                corpTravelPolicy.IsBasicEconomyAllowed = corporateTravelPolicy.IsBasicEconomyAllowed;
                corpTravelPolicy.MaximumBudget = corporateTravelPolicy.MaximumBudget;
                United.CorporateDirect.Models.CustomerProfile.CorporateTravelCabinRestriction corporateTravelCabinRestriction = corporateTravelPolicy.TravelCabinRestrictions.FirstOrDefault();
                corpTravelPolicy.TravelCabinRestrictions = new Collection<Service.Presentation.ReservationModel.CorporateTravelCabinRestriction>();
                corpTravelPolicy.TravelCabinRestrictions.Add(new Service.Presentation.ReservationModel.CorporateTravelCabinRestriction
                {
                    Duration = corporateTravelCabinRestriction.Duration,
                    TripTypeCode = corporateTravelCabinRestriction.TripTypeCode,
                    IsEconomyAllowed = corporateTravelCabinRestriction.IsEconomyAllowed,
                    IsPremiumEconomyAllowed = corporateTravelCabinRestriction.IsPremiumEconomyAllowed,
                    IsBusinessFirstAllowed = corporateTravelCabinRestriction.IsBusinessFirstAllowed,
                    UpdatedDate = corporateTravelCabinRestriction.UpdatedDate.ToString()
                });
                corpPolicy.Add(corpTravelPolicy);
            }
            return corpPolicy;
        }
        public bool EnableAdvanceSearchCouponBooking(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableAdvanceSearchCouponBooking") && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("AndroidAdvanceSearchCouponBookingVersion"), _configuration.GetValue<string>("iPhoneAdvanceSearchCouponBookingVersion"));
        }
        public MOBPromoCodeDetails AddAFSPromoCodeDetails(DisplayCart displayCart)
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

        public List<MOBSHOPPrice> GetPrices(List<DisplayPrice> displayPrices)
        {
            if (displayPrices == null)
                return null;

            var bookingPrices = new List<MOBSHOPPrice>();
            CultureInfo ci = null;
            foreach (var price in displayPrices)
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

                double tempDouble = 0;
                double.TryParse(price.Amount.ToString(), out tempDouble);
                bookingPrice.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);
                bookingPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(price.Amount, ci, false);
                UpdatePriceTypeDescForBuyMiles(price, bookingPrice);
                bookingPrices.Add(bookingPrice);
            }

            return bookingPrices;
        }
        public void UpdatePriceTypeDescForBuyMiles(DisplayPrice price, MOBSHOPPrice bookingPrice)
        {
            if (price?.SubItems?.Any(a => a?.Key == "PurchaseMiles") ?? false)
            {
                // if BUY MILES flow and PRice type is MPF change the description for UI display
                string additionalMiles = "Additional {0} miles";
                string formattedMiles = String.Format("{0:n0}", price.SubItems?.Where(a => a.Key == "PurchaseMiles")?.FirstOrDefault()?.Value);
                if (bookingPrice?.DisplayType == "MPF")
                    bookingPrice.PriceTypeDescription = string.Format(additionalMiles, formattedMiles);
            }
        }
        public async Task<(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse,bool)> GetShopPinDown(Session session, string appVersion, ShopRequest shopRequest, bool addTravelerFlow = false)
        {
            if (_configuration.GetValue<bool>("EnableOmniChannelCartMVP1"))
            {
                shopPinDownActionName = "ShopPinDownV2";
            }
            if (addTravelerFlow) shopPinDownActionName = "ModifyReservation";
            string jsonRequest = JsonConvert.SerializeObject(shopRequest);
            var jsonResponse = await _flightShoppingService.GetShopPinDown(session.Token, shopPinDownActionName, jsonRequest, _headers.ContextValues.TransactionId).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                var response = JsonConvert.DeserializeObject<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(jsonResponse);
                if (response != null && !response.Status.Equals(StatusType.Success))
                {
                    if (response != null && response.Errors != null && response.Errors.Count > 0)
                    {
                        if (response.Errors.Any(x => x?.MinorCode?.Trim() == "10051" || x?.MinorCode?.Trim() == "10056" || x?.MinorCode?.Trim() == "10130" || x?.MinorCode?.Trim() == "10129" || x?.MinorCode?.Trim() == "10045") && addTravelerFlow)
                        {
                            _logger.LogError("GetShopPinDown - ModifyReservation error {response}", JsonConvert.SerializeObject(response));
                            throw new MOBUnitedException(response?.Errors?.FirstOrDefault(x => x?.MinorCode?.Trim() is "10051" or "10056" or "10130" or "10129" or "10045")?.MinorCode);
                        }
                        var builtErrMsg = string.Join(shopPinDownErrorSeparator, response.Errors.Select(err => string.Format("{0}{1}{2}", err.MajorCode, shopPinDownErrorMajorAndMinorCodesSeparator, err.MinorCode)));
                        if (_shoppingUtility.IsFSROAFlashSaleEnabled(session?.CatalogItems) &&
                               response.Errors.Any(a => a.MinorCode == "10129"))
                            return (response, true);
                        else
                            throw new Exception(builtErrMsg);
                        throw new Exception(builtErrMsg);
                    }

                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                return (response,false);
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

        }

        public ShopRequest BuildShopPinDownDetailsRequest(MOBSHOPSelectUnfinishedBookingRequest request, string cartId = "")
        {

            var shopRequest = (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major) && request.SelectedUnfinishBooking.TravelerTypes != null) ?
                            BuildShopPinDownRequest(request.SelectedUnfinishBooking, request.MileagePlusAccountNumber, request.LanguageCode, request.Application.Id, request.Application.Version.Major, true) :
                            BuildShopPinDownRequest(request.SelectedUnfinishBooking, request.MileagePlusAccountNumber, request.LanguageCode);


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

        public ShopRequest BuildShopPinDownRequest(MOBSHOPUnfinishedBooking unfinishedBooking, string mpNumber, string languageCode, int appID = -1, string appVer = "", bool isCatalogOnForTravelerTypes = false)
        {
            var shopRequest = new ShopRequest
            {
                AccessCode = _configuration.GetValue<string>("AccessCode - CSLShopping"),
                ChannelType = _configuration.GetValue<string>("Shopping - ChannelType"),
                CountryCode = unfinishedBooking.CountryCode,
                InclCancelledFlights = false,
                InclOAMain = true,
                InclStarMain = true,
                InclUACodeshares = true,
                InclUAMain = true,
                InclUARegionals = true,
                InitialShop = true,
                LangCode = languageCode,
                StopsInclusive = true,
                LoyaltyId = mpNumber,
                RememberedLoyaltyId = mpNumber,
                TrueAvailability = true,
                UpgradeComplimentaryRequested = true,
                SearchTypeSelection = GetSearchType(unfinishedBooking.SearchType),
                Trips = GetTripsForShopPinDown(unfinishedBooking),
            };

            Func<PaxType, int, PaxInfo> getPax = (type, subtractYear) => new PaxInfo { PaxType = type, DateOfBirth = DateTime.Today.AddYears(subtractYear).ToShortDateString() };
            shopRequest.PaxInfoList = new List<PaxInfo>();

            if (_shoppingUtility.EnableTravelerTypes(appID, appVer) && isCatalogOnForTravelerTypes)
            {
                if (unfinishedBooking.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Adult.ToString()) && unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Adult.ToString()).Count > 0)
                    shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Adult.ToString()).Count).Select(x => getPax(PaxType.Adult, -20)));

                if (unfinishedBooking.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Senior.ToString()) && unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Senior.ToString()).Count > 0)
                    shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Senior.ToString()).Count).Select(x => getPax(PaxType.Senior, -67)));

                if (unfinishedBooking.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child15To17.ToString()) && unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child15To17.ToString()).Count > 0)
                    shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child15To17.ToString()).Count).Select(x => getPax(PaxType.Child05, -16)));

                if (unfinishedBooking.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child12To14.ToString()) && unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child12To14.ToString()).Count > 0)
                    shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child12To14.ToString()).Count).Select(x => getPax(PaxType.Child04, -13)));

                if (unfinishedBooking.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child5To11.ToString()) && unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child5To11.ToString()).Count > 0)
                    shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child5To11.ToString()).Count).Select(x => getPax(PaxType.Child02, -8)));

                if (unfinishedBooking.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child2To4.ToString()) && unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child2To4.ToString()).Count > 0)
                    shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child2To4.ToString()).Count).Select(x => getPax(PaxType.Child01, -3)));

                /*
                 * commented as we are not using the below code
                 * if (unfinishedBooking.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child12To17.ToString()) && unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child12To17.ToString()).Count > 0)
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child12To17.ToString()).Count).Select(x => getPax(PaxType.Child03, -15))); */

                if (unfinishedBooking.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.InfantSeat.ToString()) && unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.InfantSeat.ToString()).Count > 0)
                    shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.InfantSeat.ToString()).Count).Select(x => getPax(PaxType.InfantSeat, -1)));

                if (unfinishedBooking.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.InfantLap.ToString()) && unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.InfantLap.ToString()).Count > 0)
                    shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.InfantLap.ToString()).Count).Select(x => getPax(PaxType.InfantLap, -1)));
            }
            else
            {
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.NumberOfAdults).Select(x => getPax(PaxType.Adult, -20)));
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.NumberOfSeniors).Select(x => getPax(PaxType.Senior, -67)));
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.NumberOfChildren2To4).Select(x => getPax(PaxType.Child01, -3)));
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.NumberOfChildren5To11).Select(x => getPax(PaxType.Child02, -8)));
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.NumberOfChildren12To17).Select(x => getPax(PaxType.Child03, -15)));
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.NumberOfInfantOnLap).Select(x => getPax(PaxType.InfantLap, -1)));
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.NumberOfInfantWithSeat).Select(x => getPax(PaxType.InfantSeat, -1)));
            }

            return shopRequest;
        }

        private List<Trip> GetTripsForShopPinDown(MOBSHOPUnfinishedBooking unfinishedBooking)
        {
            List<Trip> trips = new List<Trip>();

            foreach (var mobTrip in unfinishedBooking.Trips)
            {
                var trip = new Trip
                {
                    DepartDate = mobTrip.DepartDate,
                    DepartTime = mobTrip.DepartTime,
                    Destination = mobTrip.Destination,
                    Origin = mobTrip.Origin,
                    FlightCount = mobTrip.Flights.Count(),
                    Flights = mobTrip.Flights.Select(MapToFlightShoppingFlight).ToList(),
                    SearchFiltersIn = new SearchFilterInfo(),
                    SearchFiltersOut = new SearchFilterInfo(),
                };

                // MB-3052 Combine COG flights for price variance in saved trips
                _shoppingUtility.GetFlattedFlightsForCOGorThruFlights(trip);
                trips.Add(trip);
            }
            return trips;
        }
        private Flight MapToFlightShoppingFlight(MOBSHOPUnfinishedBookingFlight ubFlight)
        {
            var flight = new Flight
            {
                Aircraft = new Aircraft(),
                EquipmentDisclosures = new EquipmentDisclosure(),
                FlightInfo = new FlightInfo(),
                BookingCode = ubFlight.BookingCode,
                DepartDateTime = ubFlight.DepartDateTime,
                Destination = ubFlight.Destination,
                Origin = ubFlight.Origin,
                FlightNumber = ubFlight.FlightNumber,
                MarketingCarrier = ubFlight.MarketingCarrier,
            };
            if (ubFlight.Products != null && ubFlight.Products.Count > 0)
            {
                flight.Products = new ProductCollection();

                foreach (var product in ubFlight.Products)
                {
                    if (product != null && !String.IsNullOrEmpty(product.ProductType))
                    {
                        flight.Products.Add(MapToFlightShoppingFlightProduct(product));
                    }
                }
            }

            if (ubFlight.Connections == null)
                return flight;

            ubFlight.Connections.ForEach(x => flight.Connections.Add(MapToFlightShoppingFlight(x)));

            return flight;
        }

        private United.Services.FlightShopping.Common.Product MapToFlightShoppingFlightProduct(MOBSHOPUnfinishedBookingFlightProduct ubFlightProduct)
        {
            if (ubFlightProduct != null)
            {
                var product = new United.Services.FlightShopping.Common.Product
                {
                    BookingCode = ubFlightProduct.BookingCode,
                    ProductType = ubFlightProduct.ProductType,
                    TripIndex = ubFlightProduct.TripIndex,
                };
                if (ubFlightProduct.Prices?.Count > 0)
                {
                    product.Prices = new List<PricingItem>();
                    foreach (var price in ubFlightProduct.Prices)
                    {
                        if (price != null && !String.IsNullOrEmpty(price.PricingType))
                        {
                            product.Prices.Add(MapToFlightShoppingFlightProductPrice(price));
                        }
                    }
                }
                return product;
            }
            return null;
        }

        private PricingItem MapToFlightShoppingFlightProductPrice(MOBSHOPUnfinishedBookingProductPrice ubProductPrice)
        {
            if (ubProductPrice != null)
            {
                PricingItem price = new PricingItem
                {
                    Amount = ubProductPrice.Amount,
                    AmountAllPax = ubProductPrice.AmountAllPax,
                    AmountBase = ubProductPrice.AmountBase,
                    Currency = ubProductPrice.Currency,
                    CurrencyAllPax = ubProductPrice.CurrencyAllPax,
                    OfferID = ubProductPrice.OfferID,
                    PricingType = ubProductPrice.PricingType,
                    Selected = ubProductPrice.Selected,
                    MerchPriceDetail = new MerchPriceDetail { EDDCode = ubProductPrice.MerchPriceDetail?.EddCode, ProductCode = ubProductPrice.MerchPriceDetail?.ProductCode }
                };
                if (ubProductPrice.SegmentMappings?.Count > 0)
                {
                    price.SegmentMappings = new List<SegmentMapping>();
                    foreach (var segmentMapping in ubProductPrice.SegmentMappings)
                    {
                        if (String.IsNullOrEmpty(segmentMapping?.SegmentRefID))
                        {
                            price.SegmentMappings.Add(MapToFlightShoppingProductSegmentMapping(segmentMapping));
                        }
                    }
                }
                return price;
            }
            return null;
        }

        private SegmentMapping MapToFlightShoppingProductSegmentMapping(MOBSHOPUnfinishedBookingProductSegmentMapping ubSegmentMapping)
        {
            if (ubSegmentMapping != null && !String.IsNullOrEmpty(ubSegmentMapping.SegmentRefID))
            {
                SegmentMapping segmentMapping = new SegmentMapping
                {
                    Origin = ubSegmentMapping.Origin,
                    Destination = ubSegmentMapping.Destination,
                    BBxHash = ubSegmentMapping.BBxHash,
                    UpgradeStatus = ubSegmentMapping.UpgradeStatus,
                    UpgradeTo = ubSegmentMapping.UpgradeTo,
                    FlightNumber = ubSegmentMapping.FlightNumber,
                    CabinDescription = ubSegmentMapping.CabinDescription,
                    SegmentRefID = ubSegmentMapping.SegmentRefID
                };
                return segmentMapping;
            }
            return null;
        }

        private SearchType GetSearchType(string searchTypeSelection)
        {
            SearchType searchType = SearchType.ValueNotSet;

            if (string.IsNullOrEmpty(searchTypeSelection))
                return searchType;

            switch (searchTypeSelection.Trim().ToUpper())
            {
                case "OW":
                    searchType = SearchType.OneWay;
                    break;
                case "RT":
                    searchType = SearchType.RoundTrip;
                    break;
                case "MD":
                    searchType = SearchType.MultipleDestination;
                    break;
                default:
                    searchType = SearchType.ValueNotSet;
                    break;
            }

            return searchType;
        }

        //added
        public async Task<bool> SaveAnUnfinishedBooking(Session session, MOBRequest request, MOBSHOPUnfinishedBooking ub)
        {
            var savedUBs = await GetSavedUnfinishedBookingEntries(session, request, ub.TravelerTypes != null);
            MOBSHOPUnfinishedBooking foundUB = null;
            if (savedUBs != null && savedUBs.Any() && (foundUB = savedUBs.FirstOrDefault(x => comparer.Equals(x, ub))) != null)
            {
                ub.Id = foundUB.Id;
                return await UpdateAnUnfinishedBooking(session, request, ub);
            }

            return await InsertAnUnfinishedBooking(session, request, ub);
        }

        public async Task<List<MOBSHOPUnfinishedBooking>> GetSavedUnfinishedBookingEntries(Session session, MOBRequest request, bool isCatalogOnForTravelerTypes = false)
        {
            if (session.CustomerID <= 0)
                return new List<MOBSHOPUnfinishedBooking>();

            string cslActionName = "SavedItinerary(Get)";

            string token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration);
            var csLCallDurationstopwatch = new Stopwatch();
            csLCallDurationstopwatch.Start();

            var response = await _customerPreferencesService.GetCustomerPrefernce<SavedItineraryDataModel>(token, cslActionName, savedUnfinishedBookingActionName, savedUnfinishedBookingAugumentName, session.CustomerID, session.SessionId);

            if (csLCallDurationstopwatch.IsRunning)
            {
                csLCallDurationstopwatch.Stop();
            }

            if (response != null)
            {

                if (response != null && response.Status.Equals(PreferencesConstants.StatusType.Success))
                {
                    List<MOBSHOPUnfinishedBooking> unfinishedBookings = new List<MOBSHOPUnfinishedBooking>();
                    if (response.SavedItineraryList != null)
                    {
                        unfinishedBookings = response.SavedItineraryList.Select(x => (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major) && isCatalogOnForTravelerTypes) ? MapToMOBSHOPUnfinishedBookingTtypes(x, request)
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

        private MOBSHOPUnfinishedBooking MapToMOBSHOPUnfinishedBooking(GetSavedItineraryDataModel cslEntry, MOBRequest request)
        {
            var ub = cslEntry.SavedItinerary;
            var mobEntry = new MOBSHOPUnfinishedBooking
            {
                SearchExecutionDate = new[] { cslEntry.InsertTimestamp, cslEntry.UpdateTimestamp }.FirstOrDefault(x => !string.IsNullOrEmpty(x)),
                NumberOfAdults = ub.PaxInfoList.Count(px => (PaxType)px.PaxType == PaxType.Adult),
                NumberOfSeniors = ub.PaxInfoList.Count(px => (PaxType)px.PaxType == PaxType.Senior),
                NumberOfChildren2To4 = ub.PaxInfoList.Count(px => (PaxType)px.PaxType == PaxType.Child01),
                NumberOfChildren5To11 = ub.PaxInfoList.Count(px => (PaxType)px.PaxType == PaxType.Child02),
                NumberOfChildren12To17 = ub.PaxInfoList.Count(px => (PaxType)px.PaxType == PaxType.Child03),
                NumberOfInfantOnLap = ub.PaxInfoList.Count(px => (PaxType)px.PaxType == PaxType.InfantLap),
                NumberOfInfantWithSeat = ub.PaxInfoList.Count(px => (PaxType)px.PaxType == PaxType.InfantSeat),
                CountryCode = ub.CountryCode,
                SearchType = GetSeachTypeSelection((SearchType)ub.SearchTypeSelection),
                Trips = ub.Trips.Select(MapToMOBSHOPUnfinishedBookingTrip).ToList(),
                Id = ub.AccessCode
            };

            if (_shoppingUtility.EnableSavedTripShowChannelTypes(request.Application.Id, request.Application.Version.Major)) // Map channel
                mobEntry.ChannelType = ub.ChannelType;

            return mobEntry;
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

        private string GetSeachTypeSelection(SearchType searchType)
        {
            var result = string.Empty;
            try
            {
                return new Dictionary<SearchType, string>
                {
                    {SearchType.OneWay, "OW"},
                    {SearchType.RoundTrip, "RT"},
                    {SearchType.MultipleDestination, "MD"},
                    {SearchType.ValueNotSet, string.Empty},
                }[searchType];
            }
            catch { return result; }
        }

        private MOBSHOPUnfinishedBooking MapToMOBSHOPUnfinishedBookingTtypes(United.Services.Customer.Preferences.Common.GetSavedItineraryDataModel cslEntry, MOBRequest request)
        {
            var ub = cslEntry.SavedItinerary;
            MOBSHOPUnfinishedBooking mobEntry = new MOBSHOPUnfinishedBooking();

            mobEntry.SearchExecutionDate = new[] { cslEntry.InsertTimestamp, cslEntry.UpdateTimestamp }.FirstOrDefault(x => !string.IsNullOrEmpty(x));
            mobEntry.TravelerTypes = new List<MOBTravelerType>();
            GetTravelTypesFromShop(ub, mobEntry);
            mobEntry.CountryCode = ub.CountryCode;
            mobEntry.SearchType = GetSeachTypeSelection((SearchType)ub.SearchTypeSelection);
            mobEntry.Trips = ub.Trips.Select(MapToMOBSHOPUnfinishedBookingTrip).ToList();
            mobEntry.Id = ub.AccessCode;
            if (_shoppingUtility.EnableSavedTripShowChannelTypes(request.Application.Id, request.Application.Version.Major)) // Map channel
                mobEntry.ChannelType = ub.ChannelType;

            return mobEntry;
        }

        private static void GetTravelTypesFromShop(SerializableSavedItinerary ub, MOBSHOPUnfinishedBooking mobEntry)
        {
            foreach (var t in ub.PaxInfoList.GroupBy(p => p.PaxType))
            {
                switch ((int)t.Key)
                {
                    case (int)PaxType.Adult:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Adult.ToString() });
                        break;

                    case (int)PaxType.Senior:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Senior.ToString() });
                        break;

                    case (int)PaxType.Child01:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child2To4.ToString() });
                        break;

                    case (int)PaxType.Child02:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child5To11.ToString() });
                        break;

                    case (int)PaxType.Child03:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child12To17.ToString() });
                        break;

                    case (int)PaxType.Child04:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child12To14.ToString() });
                        break;

                    case (int)PaxType.Child05:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child15To17.ToString() });
                        break;

                    case (int)PaxType.InfantSeat:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.InfantSeat.ToString() });
                        break;

                    case (int)PaxType.InfantLap:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.InfantLap.ToString() });
                        break;
                    default:
                        mobEntry.TravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Adult.ToString() });
                        break;
                }
            }
        }

        public async Task<bool> UpdateAnUnfinishedBooking(Session session, MOBRequest request, MOBSHOPUnfinishedBooking ubTobeUpdated)
        {
            string cslActionName = "SavedItinerary(Put)";

            var cslRequest = new United.Services.Customer.Preferences.Common.UpdateSavedItineraryData
            {
                UpdateID = session.DeviceID,
                UpdateSavedItinerary = (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major) && ubTobeUpdated.TravelerTypes != null) ?
                MapToSerializableSavedItineraryTtypes(ubTobeUpdated, request.LanguageCode, session.MileagPlusNumber) : MapToSerializableSavedItinerary(ubTobeUpdated, request.LanguageCode, session.MileagPlusNumber)
            };

            string jsonRequest = JsonConvert.SerializeObject(cslRequest);

            string token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration).ConfigureAwait(false);

            var cSLCallDurationstopwatch = new Stopwatch();
            cSLCallDurationstopwatch.Start();

            var response = await _customerPreferencesService.GetUnfinishedCustomerPrefernce<SubCommonData>(token, cslActionName, savedUnfinishedBookingActionName, savedUnfinishedBookingAugumentName, session.CustomerID, session.SessionId, jsonRequest).ConfigureAwait(false);
            if (cSLCallDurationstopwatch.IsRunning)
            {
                cSLCallDurationstopwatch.Stop();
            }

            if (response != null)
            {

                if (response != null && !response.Status.Equals(PreferencesConstants.StatusType.Success))
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        throw new Exception(string.Join(" ", response.Errors.Select(err => err.Message)));
                    }

                    throw new MOBUnitedException("Failed to update an unfinished booking.");
                }
            }
            else
            {
                throw new MOBUnitedException("Failed to update an unfinished booking.");
            }

            return true;

        }

        private async Task<bool> InsertAnUnfinishedBooking(Session session, MOBRequest request, MOBSHOPUnfinishedBooking ubTobeInserted)
        {
            string cslActionName = "SavedItinerary(Post)";

            string token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration).ConfigureAwait(false);


            var cslRequest = new InsertSavedItineraryData
            {
                InsertID = session.DeviceID,
                InsertSavedItinerary = (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major) && ubTobeInserted.TravelerTypes != null) ?
                MapToSerializableSavedItineraryTtypes(ubTobeInserted, request.LanguageCode, session.MileagPlusNumber) : MapToSerializableSavedItinerary(ubTobeInserted, request.LanguageCode, session.MileagPlusNumber)
            };

            //// Ensure access code is empty for the new one to be inserted. Only the existing one has access code
            //cslRequest.InsertSavedItinerary.AccessCode = null;

            string jsonRequest = JsonConvert.SerializeObject(cslRequest);

            var cSLCallDurationstopwatch = new Stopwatch();
            cSLCallDurationstopwatch.Start();

            var response = await _customerPreferencesService.GetUnfinishedCustomerPrefernce<SubCommonData>(token, cslActionName, savedUnfinishedBookingActionName, savedUnfinishedBookingAugumentName, session.CustomerID, session.SessionId, jsonRequest).ConfigureAwait(false);

            if (cSLCallDurationstopwatch.IsRunning)
            {
                cSLCallDurationstopwatch.Stop();
            }

            if (response != null)
            {

                if (response != null && !response.Status.Equals(PreferencesConstants.StatusType.Success))
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        throw new Exception(string.Join(" ", response.Errors.Select(err => err.Message)));
                    }

                    throw new MOBUnitedException("Failed to insert an unfinished booking.");
                }
            }
            else
            {
                throw new MOBUnitedException("Failed to insert an unfinished booking.");
            }

            return true;

        }

        private SerializableSavedItinerary MapToSerializableSavedItineraryTtypes(MOBSHOPUnfinishedBooking ub, string languageCode, string mpNumber)
        {
            var cslUB = new SerializableSavedItinerary
            {
                AccessCode = ub.Id,
                AwardTravel = false,
                ChannelType = _configuration.GetValue<string>("Shopping - ChannelType"),
                CountryCode = ub.CountryCode,
                InitialShop = true,
                LangCode = languageCode,
                LoyaltyId = mpNumber,
                NGRP = false,
                SearchTypeSelection = (SerializableSavedItinerary.SearchType)GetSearchType(ub.SearchType),
                Trips = MapToListOfCSLUnfinishedBookingTrips(ub.Trips),
                TrueAvailability = true
            };

            Func<PaxType, int, SerializableSavedItinerary.PaxInfo> getPax = (type, subtractYear) => new SerializableSavedItinerary.PaxInfo { PaxType = (SerializableSavedItinerary.PaxType)type, DateOfBirth = DateTime.Today.AddYears(subtractYear).ToShortDateString() };
            cslUB.PaxInfoList = new List<SerializableSavedItinerary.PaxInfo>();
            GetPaxInfoUnfinishBooking(ub, cslUB, getPax);

            return cslUB;
        }

        private List<SerializableSavedItinerary.Trip> MapToListOfCSLUnfinishedBookingTrips(List<MOBSHOPUnfinishedBookingTrip> trips)
        {
            if (trips == null)
                return null;

            var clsTrips = new List<SerializableSavedItinerary.Trip>();
            if (!trips.Any())
                return clsTrips;

            foreach (var mobTrip in trips)
            {
                var trip = new SerializableSavedItinerary.Trip
                {
                    DepartDate = mobTrip.DepartDate,
                    DepartTime = mobTrip.DepartTime,
                    Destination = mobTrip.Destination,
                    Origin = mobTrip.Origin,
                    FlightCount = mobTrip.Flights.Count(),
                    Flights = mobTrip.Flights.Select(MapToCSLUnfinishedBookingFlight).ToList(),
                };

                clsTrips.Add(trip);
            }

            return clsTrips;
        }

        private SerializableSavedItinerary.Flight MapToCSLUnfinishedBookingFlight(MOBSHOPUnfinishedBookingFlight ubFlight)
        {
            var flight = new SerializableSavedItinerary.Flight
            {
                BookingCode = ubFlight.BookingCode,
                DepartDateTime = ubFlight.DepartDateTime,
                Destination = ubFlight.Destination,
                Origin = ubFlight.Origin,
                FlightNumber = ubFlight.FlightNumber,
                MarketingCarrier = ubFlight.MarketingCarrier,
                ProductType = ubFlight.ProductType
            };

            if (ubFlight.Connections == null)
                return flight;

            ubFlight.Connections.ForEach(x => flight.Connections.Add(MapToCSLUnfinishedBookingFlight(x)));

            return flight;
        }

        private SerializableSavedItinerary MapToSerializableSavedItinerary(MOBSHOPUnfinishedBooking ub, string languageCode, string mpNumber)
        {
            var cslUB = new SerializableSavedItinerary
            {
                AccessCode = ub.Id,
                AwardTravel = false,
                ChannelType = _configuration.GetValue<string>("Shopping - ChannelType"),
                CountryCode = ub.CountryCode,
                InitialShop = true,
                LangCode = languageCode,
                LoyaltyId = mpNumber,
                NGRP = false,
                SearchTypeSelection = (SerializableSavedItinerary.SearchType)GetSearchType(ub.SearchType),
                Trips = MapToListOfCSLUnfinishedBookingTrips(ub.Trips),
                TrueAvailability = true
            };

            Func<PaxType, int, SerializableSavedItinerary.PaxInfo> getPax = (type, subtractYear) => new SerializableSavedItinerary.PaxInfo { PaxType = (SerializableSavedItinerary.PaxType)type, DateOfBirth = DateTime.Today.AddYears(subtractYear).ToShortDateString() };
            cslUB.PaxInfoList = new List<SerializableSavedItinerary.PaxInfo>();
            cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.NumberOfAdults).Select(x => getPax(PaxType.Adult, -20)));
            cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.NumberOfSeniors).Select(x => getPax(PaxType.Senior, -67)));
            cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.NumberOfChildren2To4).Select(x => getPax(PaxType.Child01, -3)));
            cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.NumberOfChildren5To11).Select(x => getPax(PaxType.Child02, -8)));
            cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.NumberOfChildren12To17).Select(x => getPax(PaxType.Child03, -15)));
            cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.NumberOfInfantOnLap).Select(x => getPax(PaxType.InfantLap, -1)));
            cslUB.PaxInfoList.AddRange(Enumerable.Range(1, ub.NumberOfInfantWithSeat).Select(x => getPax(PaxType.InfantSeat, -1)));

            return cslUB;
        }

        private void GetPaxInfoUnfinishBooking(MOBSHOPUnfinishedBooking ub, SerializableSavedItinerary cslUB, Func<PaxType, int, SerializableSavedItinerary.PaxInfo> getPax)
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
       
        public List<MOBTravelerType> UpdateRequestForInfants(Reservation bookingPathReservation, List<MOBTravelerType> travelers)
        {
            int infantCount = travelers.Where(t => t.TravelerType == PAXTYPE.InfantSeat.ToString() || t.TravelerType == PAXTYPE.InfantLap.ToString()).Sum(x => x.Count);
            if (infantCount == 0) return travelers;

            int searchInfantinLapCount = bookingPathReservation.ShopReservationInfo2.TravelerTypes.FirstOrDefault(t => t.TravelerType == PAXTYPE.InfantLap.ToString())?.Count ?? 0;
            int adultSeniorCount = travelers.Where(t => t.TravelerType == PAXTYPE.Adult.ToString() || t.TravelerType == PAXTYPE.Senior.ToString()).Sum(x => x.Count);
            int infantInLapCount = 0;
            int infantWithSeat = 0;

            if (infantCount <= searchInfantinLapCount)
            {
                infantInLapCount = infantCount;
                infantWithSeat = 0;
            }
            else 
            {
                infantInLapCount =  infantCount <= searchInfantinLapCount ? infantCount : searchInfantinLapCount;
                infantWithSeat = infantCount - infantInLapCount;
            }

            if (adultSeniorCount < infantInLapCount || (infantWithSeat > 0 && adultSeniorCount == 0)) throw new MOBUnitedException("9999", _configuration.GetValue<string>("UnaccompaniedDisclaimerMessage"));

            travelers.First(t => t.TravelerType == PAXTYPE.InfantLap.ToString()).Count = infantInLapCount;
            travelers.First(t => t.TravelerType == PaxType.InfantSeat.ToString()).Count = infantWithSeat;

            return travelers;
        }

        #region UnfinishedBooking methods
        public async Task<(MOBSHOPAvailability, bool)> GetShopPinDownDetailsV2(Session session, MOBSHOPSelectUnfinishedBookingRequest request = null, HttpContext httpContext = null, MOBAddTravelersRequest addTravelersRequest = null, bool addTravelerFlow = false)
        {
            var availability = new MOBSHOPAvailability { SessionId = session.SessionId };
            string cartid = string.Empty;
            bool isOAFlashError = false;
            var response = default(FlightReservationResponse);
            FlightReservationResponse shoppindownResponse = null;
            string sessionId = request?.SessionId ?? session.SessionId;
            var selectedUnfinishBooking = request?.SelectedUnfinishBooking;
            var bookingpathReservation = new Reservation();
            bool isUnfinishedBookingPath = true;
            List<MOBCPTraveler> allEligibleTravelersCSL = null;

            bool enableAddTraveler = await _featureSettings.GetFeatureSettingValue("EnableAddTravelers").ConfigureAwait(false);
            if (addTravelerFlow)
            {
                //load bookingpathReservation to check whether user selected HoldFareLock
                bookingpathReservation = await _sessionHelperService.GetSession<Reservation>(addTravelersRequest.SessionId, new Reservation().ObjectName, new List<string> { addTravelersRequest.SessionId, new Reservation().ObjectName }).ConfigureAwait(false);
                isUnfinishedBookingPath = bookingpathReservation?.ShopReservationInfo2?.IsUnfinihedBookingPath ?? false;
                bookingpathReservation?.ShopReservationInfo2?.AllEligibleTravelersCSL?.ForEach(x => x.IsInfantTravelerTypeConfirmed = false);
                allEligibleTravelersCSL = bookingpathReservation?.ShopReservationInfo2?.AllEligibleTravelersCSL;
            }

            if (request == null)
            {
                request = new MOBSHOPSelectUnfinishedBookingRequest();
                request.CatalogItems = session?.CatalogItems;
                request.Application = addTravelersRequest.Application;
                request.DeviceId = addTravelersRequest.DeviceId;
            }
            var isOmniCartHomeScreenChanges = false;
            bool isOmniCartSavedTrip = request?.IsOmniCartSavedTrip ?? false;
            isOmniCartHomeScreenChanges = request.IsOmniCartSavedTrip && _omniCart.IsEnableOmniCartHomeScreenChanges(request.Application.Id, request.Application.Version.Major);
            if (!addTravelerFlow && isOmniCartHomeScreenChanges)
            {
                response = await _omniCart.GetFlightReservationResponseByCartId(session.SessionId, request.CartId);
                if (enableAddTraveler)
                {
                    await _sessionHelperService.SaveSession(response, session.SessionId, new List<string> { session.SessionId, new FlightReservationResponse().GetType().FullName }, new FlightReservationResponse().GetType().FullName).ConfigureAwait(false);
                }
                if (response == null)
                {
                    throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage")); // omni cart persist file not found hence rasing exception forcefully
                }
                if (_omniCart.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, true))
                {
                    await _omniCart.BuildShoppingCart(request, response, FlowType.BOOKING.ToString(), request.CartId, sessionId);
                }
            }
            else
            {
                ShopRequest shopPindownRequest = null;
                if (!addTravelerFlow)
                {
                    Guid cartId = new Guid(session.SessionId);
                    cartid = cartId.ToString().ToUpper();
                }

                if (addTravelerFlow)
                {
                    sessionId = session.SessionId;
                    cartid = addTravelersRequest.CartId;

                    if (addTravelersRequest.CartId.IsNullOrEmpty())
                    {
                        addTravelersRequest.CartId = !session.CartId.IsNullOrEmpty() ? session.CartId : new Guid(session.SessionId).ToString().ToUpper();
                        cartid = addTravelersRequest.CartId;
                    }

                    CSLShopRequest cslShopRequest = new CSLShopRequest();
                    cslShopRequest = await _sessionHelperService.GetSession<CSLShopRequest>(addTravelersRequest.SessionId, cslShopRequest.ObjectName, new List<string> { addTravelersRequest.SessionId, cslShopRequest.ObjectName });

                    var loadFlightReservationResponse = await _sessionHelperService.GetSession<FlightReservationResponse>(addTravelersRequest.SessionId, new FlightReservationResponse().GetType().FullName, new List<string> { addTravelersRequest.SessionId, new FlightReservationResponse().GetType().FullName }).ConfigureAwait(false);

                    if (loadFlightReservationResponse != null)
                    {
                        shopPindownRequest = await BuildShopPinDownDetailsRequest(addTravelersRequest, loadFlightReservationResponse, cslShopRequest, bookingpathReservation, session);

                        _logger.LogInformation("GetShopPinDownDetailsV2 - Request for ModifyReservation SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Request {request}", session.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, JsonConvert.SerializeObject(shopPindownRequest));
                    }
                    else throw new Exception("Session expired, please try again");
                }
                else
                {
                    shopPindownRequest = BuildShopPinDownDetailsRequest(request, cartid);
                }
                await _sessionHelperService.SaveSession(shopPindownRequest, session.SessionId, new List<string> { session.SessionId, typeof(ShopRequest).FullName, }, typeof(ShopRequest).FullName).ConfigureAwait(false);// SAVING THIS SHOP PIN DOWN REQUEST TO USE LATER ON FOR GET BUNDLES CALL

                var tupleResponse = await GetShopPinDown(session, request.Application.Version.Major, shopPindownRequest, addTravelerFlow);
                shoppindownResponse = tupleResponse.Item1;
                isOAFlashError = tupleResponse.Item2;
                await _sessionHelperService.SaveSession(shoppindownResponse, session.SessionId, new List<string> { session.SessionId, new FlightReservationResponse().GetType().FullName }, new FlightReservationResponse().GetType().FullName).ConfigureAwait(false);

                if (addTravelerFlow)
                {
                    _logger.LogInformation("GetShopPinDownDetailsV2 - Response for ModifyReservation  SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Response {response}", session.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, JsonConvert.SerializeObject(shoppindownResponse));
                }

                if (shoppindownResponse != null && shoppindownResponse.Status.Equals(StatusType.Success) && shoppindownResponse.Reservation != null)
                {
                    var shopping = new SHOPShopping();
                    try
                    {
                        response = await RegisterFlights(shoppindownResponse, session, addTravelerFlow ? addTravelersRequest : request);
                        if (addTravelerFlow)
                        {   
                            if (bookingpathReservation?.TravelOptions?.Count > 0 && TravelOptionsContainsFareLock(bookingpathReservation.TravelOptions))
                            {
                                if (shoppindownResponse.FareLockResponse != null)
                                    //register updated farelock offer after reprice based on user selection to all the selected travelers
                                    response = await RegisterFareLockOffer(session, request, addTravelersRequest, sessionId, cartid, shoppindownResponse, bookingpathReservation).ConfigureAwait(false);
                            }
                           
                            session.IsMoneyPlusMilesSelected = (!shoppindownResponse?.DisplayCart?.MoneyMilesOptionId?.IsNullOrEmpty()) ?? false; // to update the cart based on the cslresponse
                            response.IsMileagePurchaseRequired = shoppindownResponse.IsMileagePurchaseRequired;
                            response.IsPurchaseIneligible = shoppindownResponse.IsPurchaseIneligible;
                            response.MessageReasonCodes = shoppindownResponse.MessageReasonCodes;

                            _logger.LogInformation("GetShopPinDownDetailsV2 - Response for RegisterFlights  SessionId {sessionid}, ApplicationId {applicationid}, ApplicationVersion {applicationversion}, DeviceId {deviceid} and Response {response}", session.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, JsonConvert.SerializeObject(response));
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
                    #region Populate properties which are missing from RegisterFlights Response
                    AssignMissingPropertiesfromRegisterFlightsResponse(shoppindownResponse, response);
                    #endregion
                }
            }
            if (response != null)
            {
                List<CMSContentMessage> lstMessages = await _ffcShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                var mobResRev = await PopulateReservation(session, response.Reservation);
                if (response.DisplayCart != null && response.DisplayCart.DisplayPrices != null && response.Reservation != null)
                {
                    availability.CartId = response.CartId;
                    availability.Reservation = new MOBSHOPReservation(_configuration, _cachingService);   
                    if (!_configuration.GetValue<bool>("BasicEconomyRestrictionsForShareFlightsBugFixToggle") == true && (selectedUnfinishBooking?.IsSharedFlightRequest ?? false))
                    {
                        availability.Reservation.IsMetaSearch = true;
                    }
                    availability.Reservation.AwardTravel = addTravelerFlow && (addTravelersRequest?.IsAward ?? false);
                    availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();
                    availability.Reservation.ShopReservationInfo2.Characteristics = ShopStaticUtility.GetCharacteristics(response.Reservation);
                    availability.Reservation.ShopReservationInfo2.IsIBELite = _shoppingUtility.IsIBELiteFare(response.DisplayCart);
                    availability.Reservation.ShopReservationInfo2.IsIBE = _shoppingUtility.IsIBEFullFare(response.DisplayCart);
                    availability.Reservation.ShopReservationInfo2.IsNonRefundableNonChangable = _shoppingUtility.IsNonRefundableNonChangable(response.DisplayCart);
                    availability.Reservation.ShopReservationInfo2.AllowAdditionalTravelers = !session.IsCorporateBooking;
                    availability.Reservation.ShopReservationInfo2.HideBackButtonOnSelectTraveler = addTravelerFlow;   
                    availability.Reservation.ShopReservationInfo2.IsUnfinihedBookingPath = isUnfinishedBookingPath;
                    if (_configuration.GetValue<bool>("EnableOmniCartReleaseCandidateTwoChanges_Bundles") && !addTravelerFlow)
                    {
                        availability.Reservation.ShopReservationInfo2.IsOmniCartSavedTripFlow = isOmniCartSavedTrip;
                    }
                    availability.Reservation.IsSSA = _shoppingUtility.EnableSSA(request.Application.Id, request.Application.Version.Major);
                    availability.Reservation.IsELF = response.DisplayCart.IsElf;

                    availability.Reservation.IsUpgradedFromEntryLevelFare = response.DisplayCart.IsUpgradedFromEntryLevelFare && !_shoppingUtility.IsIBELiteFare(response.DisplayCart.ProductCodeBeforeUpgrade);
                    availability.Reservation.PointOfSale = response.Reservation.PointOfSale.Country.CountryCode;
                    availability.Reservation.CartId = response.CartId;
                    availability.Reservation.SessionId = session.SessionId;

                    availability.Reservation.NumberOfTravelers = response.Reservation.NumberInParty;
                    availability.Reservation.Trips = await PopulateMetaTrips(new MOBSHOPDataCarrier(), response.CartId, null, session.SessionId, request.Application.Id, request.DeviceId, request.Application.Version.Major, true, response.DisplayCart.DisplayTrips, "", new List<string>());
                    if (addTravelerFlow)
                    {
                        availability.Reservation?.Trips?.ForEach(trip =>
                           trip.FlattenedFlights?.ForEach(flattendFlight =>
                           flattendFlight.Flights?.ForEach(flight =>
                           {
                               flight.CarbonEmissionData = bookingpathReservation?.Trips?.Where(x => x.FlattenedFlights.Any(x => x.FlightHash == flattendFlight.FlightHash))?.Select(x => x.FlattenedFlights?.FirstOrDefault()?.Flights?.FirstOrDefault(x => x.FlightNumber == flight.FlightNumber && x.CarbonEmissionData != null)?.CarbonEmissionData ?? null)?.FirstOrDefault();
                           })));
                    }

                    var eLFRitMetaShopMessages = new ELFRitMetaShopMessages(_configuration, _dynamoDBService, _legalDocumentsForTitlesService, _headers);
                    availability.Reservation.ELFMessagesForRTI = await eLFRitMetaShopMessages.GetELFShopMessagesForRestrictions(availability.Reservation, request.Application.Id);
                    switch (response.DisplayCart.SearchType)
                    {
                        case SearchType.OneWay:
                            availability.Reservation.SearchType = "OW";
                            break;
                        case SearchType.RoundTrip:
                            availability.Reservation.SearchType = "RT";
                            break;
                        case SearchType.MultipleDestination:
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
                        availability.Reservation.Prices = GetPrices(response.DisplayCart.DisplayPrices);
                    }
                    _shoppingUtility.SetCanadaTravelNumberDetails(availability, request, session);
                    bool Is24HoursWindow = false;
                    if (_configuration.GetValue<bool>("EnableForceEPlus"))
                    {
                        if (availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT != null)
                        {
                            Is24HoursWindow = TopHelper.Is24HourWindow(Convert.ToDateTime(availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT));
                        }
                    }

                    availability.Reservation.ShopReservationInfo2.IsForceSeatMap = _shoppingUtility.IsForceSeatMapforEPlus(false, response.DisplayCart.IsElf, Is24HoursWindow, request.Application.Id, request.Application.Version.Major);
                    bool isSupportedVersion = GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, "AndroidBundleVersion", "IOSBundleVersion", "", "", true, _configuration);
                    if (isSupportedVersion)
                    {
                        if (_configuration.GetValue<bool>("IsEnableBundlesForBasicEconomyBooking"))
                        {
                            availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles");
                        }
                        else
                        {
                            availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles") && !(response.DisplayCart.IsElf || _shoppingUtility.IsIBEFullFare(response.DisplayCart));
                        }
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
                                _logger.LogWarning("GetShopPinDownDetails - Response with Inhibit warning {@Response}", JsonConvert.SerializeObject(response));

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

                                if (_shoppingUtility.EnableBoeingDisclaimer())
                                {
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                                }
                            }
                        }
                    }

                    if (_shoppingUtility.EnableBoeingDisclaimer() && _shoppingUtility.IsBoeingDisclaimer(response.DisplayCart.DisplayTrips))
                    {
                        if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                        if (!availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.BOEING737WARNING.ToString()))
                        {
                            _logger.LogWarning("GetShopBookingDetails - Response with Inhibit warning {@Response}", JsonConvert.SerializeObject(response));

                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();
                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetBoeingDisclaimer());

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

                            if (ShopStaticUtility.IdentifyBEFareInversion(response))
                            {
                                if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();


                                if (_shoppingUtility.IsNonRefundableNonChangable(response.DisplayCart.ProductCodeBeforeUpgrade))
                                {
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(await _shoppingUtility.GetNonRefundableNonChangableInversionMessage(request, session));
                                }
                                else
                                {
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetBEMessage());
                                }
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                            }
                        }
                    }

                    #endregion

                    await _shoppingUtility.SetELFUpgradeMsg(availability, response?.DisplayCart?.ProductCodeBeforeUpgrade, request, session);

                    if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Any())
                    {
                        availability.Reservation.Prices.AddRange(GetPrices(response.DisplayCart.DisplayFees));
                    }

                    availability.Reservation.Prices = AdjustTotal(availability.Reservation.Prices);

                    if (_shoppingUtility.IsEnableTaxForAgeDiversification(session.IsReshopChange, request.Application.Id, request.Application.Version.Major))
                    {
                        availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();
                        availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = _shoppingUtility.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices);
                        if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                        {
                            availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes.Add(ShopStaticUtility.AddFeesAfterPriceChange(response.DisplayCart.DisplayFees));
                        }
                    }
                    else
                    {
                        availability.Reservation.Taxes = GetTaxAndFees(response.DisplayCart.DisplayPrices, response.Reservation.NumberInParty);

                        if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                        {
                            //combine fees into taxes so that totals are correct
                            List<DisplayPrice> tempList = new List<DisplayPrice>();
                            tempList.AddRange(response.DisplayCart.DisplayPrices);
                            tempList.AddRange(response.DisplayCart.DisplayFees);
                            availability.Reservation.Taxes = GetTaxAndFees(tempList, response.Reservation.NumberInParty);
                        }
                    }
                    availability.Reservation.TravelOptions = GetTravelOptions(response.DisplayCart, session.IsReshopChange, request.Application.Id, request.Application.Version.Major);

                    availability.Reservation.Prices = _shoppingUtility.UpdatePricesForEFS(availability.Reservation, request.Application.Id, request.Application.Version.Major, false);

                    if (isOmniCartHomeScreenChanges)
                    {
                        if (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, session.IsReshopChange))
                        {
                            availability.TravelerCount = _omniCart.IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, true) && response?.Reservation?.Travelers != null ? response.Reservation.Travelers.Count() : response.DisplayCart.DisplayTravelers.Sum(t => t.TravelerCount);
                            availability.Reservation.NumberOfTravelers = availability.TravelerCount;
                            availability.Reservation.ShopReservationInfo2.TravelerTypes = _omniCart.GetMOBTravelerTypes(response.DisplayCart.DisplayTravelers);
                            availability.Reservation.ShopReservationInfo2.displayTravelTypes = ShopStaticUtility.GetDisplayTravelerTypes(response.DisplayCart.DisplayTravelers);
                        }
                    }
                    else
                    {
                        if (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, session.IsReshopChange) && (addTravelerFlow ? (addTravelersRequest.TravelerTypes != null && addTravelersRequest.TravelerTypes.Count > 0) : (request.SelectedUnfinishBooking.TravelerTypes != null && request.SelectedUnfinishBooking.TravelerTypes.Count > 0)))
                        {
                            availability.TravelerCount = addTravelerFlow ? addTravelersRequest.TravelerTypes.Where(t => t.TravelerType != null && t.Count > 0).Sum(t => t.Count) : request.SelectedUnfinishBooking.TravelerTypes.Where(t => t.TravelerType != null && t.Count > 0).Sum(t => t.Count);
                            availability.Reservation.NumberOfTravelers = availability.TravelerCount;

                            availability.Reservation.ShopReservationInfo2.TravelerTypes = addTravelerFlow ? addTravelersRequest.TravelerTypes : request.SelectedUnfinishBooking.TravelerTypes;

                            availability.Reservation.ShopReservationInfo2.displayTravelTypes = ShopStaticUtility.GetDisplayTravelerTypes(response.DisplayCart.DisplayTravelers);
                        }
                    }

                    if (addTravelerFlow)
                    {
                        if ((bookingpathReservation.FormOfPaymentType == MOBFormofPayment.PayPal || bookingpathReservation.FormOfPaymentType == MOBFormofPayment.PayPalCredit) && bookingpathReservation.PayPal != null)
                        {
                            availability.Reservation.FormOfPaymentType = bookingpathReservation.FormOfPaymentType;
                            availability.Reservation.PayPal = bookingpathReservation.PayPal;
                            availability.Reservation.PayPalPayor = bookingpathReservation.PayPalPayor;
                        }
                        availability.Reservation.ReservationEmail = bookingpathReservation?.ReservationEmail;
                        availability.Reservation.ReservationPhone = bookingpathReservation?.ReservationPhone;
                        availability.Reservation.CreditCards = bookingpathReservation?.CreditCards;
                        availability.Reservation.CreditCardsAddress = bookingpathReservation?.CreditCardsAddress;
                        availability.Reservation.TravelersRegistered = false;
                        availability.Reservation.IsSignedInWithMP = bookingpathReservation?.IsSignedInWithMP ?? false;
                        availability.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL = allEligibleTravelersCSL;
                        availability.Reservation.ShopReservationInfo2.TravelType = bookingpathReservation?.ShopReservationInfo2?.TravelType ?? string.Empty;
                        availability.Reservation.ShopReservationInfo2.AllowExtraSeatSelection = _shoppingUtility.IsExtraSeatFeatureEnabled(request.Application.Id, request.Application.Version.Major, session?.CatalogItems) && _shoppingUtility.IsExtraSeatExcluded(session, availability?.Reservation.Trips, availability?.Reservation?.ShopReservationInfo2?.displayTravelTypes, null, isUnfinishedBookingPath);
                    }
                    if (string.IsNullOrEmpty(session.EmployeeId))
                    {
                        availability.Reservation.FareLock = GetFareLockOptions(response.FareLockResponse, response.DisplayCart.DisplayPrices, request?.SelectedUnfinishBooking?.CatalogItems, request.Application.Id, request.Application.Version.Major);
                    }
                    if (addTravelerFlow && (response.IsMileagePurchaseRequired || response.IsPurchaseIneligible))
                    {
                        await _shoppingUtility.DisplayBuyMiles(availability.Reservation, response, session, addTravelersRequest, lstMessages);
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
                        //Profile profile = new CSL.Profile();
                        availability.Reservation.CubaTravelInfo.CubaTravelTitles = await GetMPPINPWDTitleMessages(new List<string> { "CUBA_TRAVEL_CONTENT" });
                    }

                    if (_shoppingUtility.IsEnabledNationalityAndResidence(false, request.Application.Id, request.Application.Version.Major))
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
                availability.Reservation.ISFlexibleSegmentExist = IsOneFlexibleSegmentExist(availability.Reservation.Trips);
                availability.Reservation.FlightShareMessage = GetFlightShareMessage(availability.Reservation, string.Empty);
                availability.Reservation.IsRefundable = mobResRev.IsRefundable;
                availability.Reservation.ISInternational = mobResRev.ISInternational;

                //**RSA Publick Key Implmentaion**//

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

                        if (_configuration.GetValue<bool>("EnableServiceAnimalEnhancements"))
                        {
                            var selectTripRequest = new SelectTripRequest { CatalogItems = request.CatalogItems != null ? request.CatalogItems : request.SelectedUnfinishBooking?.CatalogItems, Application = request.Application, DeviceId = request.DeviceId };
                            // populate avail. special needs for the itinerary
                            availability.Reservation.ShopReservationInfo2.SpecialNeeds = await GetItineraryAvailableSpecialNeeds(session, request.Application.Id, request.Application.Version.Major, request.DeviceId, response.Reservation.FlightSegments, "en-US", availability.Reservation, selectTripRequest);
                        }
                        else
                        {
                            // populate avail. special needs for the itinerary
                            availability.Reservation.ShopReservationInfo2.SpecialNeeds = await GetItineraryAvailableSpecialNeeds(session, request.Application.Id, request.Application.Version.Major, request.DeviceId, response.Reservation.FlightSegments, "en-US");
                        }
                        // update persisted reservation object too
                        var bookingPathReservation = new Reservation();
                        bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(session.SessionId, bookingPathReservation.ObjectName, new List<string> { session.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                        if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null)
                        {
                            bookingPathReservation.ShopReservationInfo2.SpecialNeeds = availability.Reservation.ShopReservationInfo2.SpecialNeeds;
                            await _sessionHelperService.SaveSession(bookingPathReservation, session.SessionId, new List<string> { session.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("GetShopPinDownDetails - GetItineraryAvailableSpecialNeeds {@ExceptionMessage}", JsonConvert.SerializeObject(e));
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
                            var message = !string.IsNullOrEmpty(sdlKeyForWheelchairSizerContent) ? await GetCMSContentMessageByKey(sdlKeyForWheelchairSizerContent, request, session).ConfigureAwait(false) : null;
                            if (message != null)
                            {
                                availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo.WcHeaderMsg = message?.HeadLine;
                                availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo.WcBodyMsg = message?.ContentFull;
                            }
                            _shoppingUtility.BuildWheelChairSizerOAMsgs(availability.Reservation);
                            try
                            {
                                if (await _featureToggles.IsEnableWheelchairFilterOnFSR(request.Application.Id, request.Application.Version.Major, session.CatalogItems).ConfigureAwait(false))
                                {
                                    MOBSearchFilters searchFilters = new MOBSearchFilters();
                                    searchFilters = await _sessionHelperService.GetSession<MOBSearchFilters>(session.SessionId, searchFilters.GetType().FullName, new List<string> { session.SessionId, searchFilters.GetType().FullName });
                                    _shoppingUtility.PrepopulateDimensionInfo(searchFilters, availability.Reservation.ShopReservationInfo2, session.SessionId);
                                }                                
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("WheelChairFilter-Dimension Info Prepopulating error-{@Exception},SessionId-{sessionId}", JsonConvert.SerializeObject(ex), request.SessionId);
                            }
                            var bookingPathReservation = new Reservation();
                            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(session.SessionId, bookingPathReservation.ObjectName, new List<string> { session.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                            if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null)
                            {
                                bookingPathReservation.ShopReservationInfo2.WheelChairSizerInfo = availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo;
                                await _sessionHelperService.SaveSession(bookingPathReservation, session.SessionId, new List<string> { session.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger1.LogError("GetShopPinDownDetails - WheelChairSizerContent {@message} {@stackTrace}", ex.Message, ex.StackTrace);
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
                    availability.Reservation.ShopReservationInfo2.IsDisplayCart = _shoppingUtility.IsDisplayCart(session, GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("Android_EnableAwardLiveCart_AppVersion"), _configuration.GetValue<string>("iPhone_EnableAwardLiveCart_AppVersion"))
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
                await _sessionHelperService.SaveSession(persistedShopBookingDetailsResponse, session.SessionId, new List<string> { session.SessionId, persistedShopBookingDetailsResponse.ObjectName }, persistedShopBookingDetailsResponse.ObjectName).ConfigureAwait(false);
                // OA Flash sale todo : need to update the messages
                if ( _shoppingUtility.IsFSROAFlashSaleEnabled(session?.CatalogItems)
                     && shoppindownResponse.Errors != null && shoppindownResponse.Errors.Count > 0
                     && (shoppindownResponse.Errors.Where(a => a.MinorCode.Trim().Equals("10129"))?.Count() > 0)
                    )
                {
                    availability = _shoppingUtility.AddFSROAFalsSaleAlerts(availability, lstMessages, shoppindownResponse.Errors.Where(a => a.MinorCode.Trim().Equals("10129"))?.FirstOrDefault());
                }
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
                        PremierStatusLevel = addTravelerFlow ? addTravelersRequest.PremierStatusLevel : request.PremierStatusLevel,
                        MileagePlusAccountNumber = addTravelerFlow ? addTravelersRequest.MileagePlusNumber : request.MileagePlusAccountNumber,
                        SearchType = availability?.Reservation?.SearchType
                    }
                };
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
                await _sessionHelperService.SaveSession(selectTrip, session.SessionId, new List<string> { session.SessionId, selectTrip.ObjectName }, selectTrip.ObjectName).ConfigureAwait(false);
                bool isCFOPVersionCheck = GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("AndriodCFOP_Booking_Reshop_PostbookingAppVersion"), _configuration.GetValue<string>("IphoneCFOP_Booking_Reshop_PostbookingAppVersion"));
                if (isCFOPVersionCheck)
                {

                    if (!string.IsNullOrEmpty(_configuration.GetValue<string>("IsBookingCommonFOPEnabled")) && session.IsReshopChange == false)
                    {
                        availability.Reservation.IsBookingCommonFOPEnabled = Convert.ToBoolean(_configuration.GetValue<string>("IsBookingCommonFOPEnabled"));

                    }
                }
                var reservation = ReservationToPersistReservation(availability, addTravelerFlow);

                reservation.CSLReservationJSONFormat = JsonConvert.SerializeObject(CommonMethods.FlattenSegmentsForSeatDisplay(response.Reservation));
                reservation.FOPOptions = _shoppingUtility.GetFopOptions(request.Application.Id, request.Application.Version.Major);//FOP Options Fix Venkat 12/08

                await _sessionHelperService.SaveSession(reservation, session.SessionId, new List<string> { session.SessionId, reservation.ObjectName }, reservation.ObjectName).ConfigureAwait(false);
                if (_shoppingUtility.EnableInflightContactlessPayment(request.Application.Id, request.Application.Version.Major, false))
                {
                    FireForGetInFlightCLEligibilityCheck(reservation, request, session);
                }
                await SetAvailabilityELFProperties(availability, availability.Reservation.NumberOfTravelers > 1, _shoppingUtility.EnableSSA(request.Application.Id, request.Application.Version.Major));
            }

            return (availability, isOAFlashError);
        }

       

        private void CheckTotalInfantsCount(MOBAddTravelersRequest addTravelersRequest)
        {
            int adultSeniorCount = addTravelersRequest.TravelerTypes.Where(t => t.TravelerType == PAXTYPE.Adult.ToString() || t.TravelerType == PAXTYPE.Senior.ToString()).Sum(x => x.Count);
            if (adultSeniorCount < addTravelersRequest.TravelerTypes.FirstOrDefault(t => t.TravelerType == PAXTYPE.InfantLap.ToString()).Count || (addTravelersRequest.TravelerTypes.FirstOrDefault(t => t.TravelerType == PAXTYPE.InfantSeat.ToString()).Count > 0 && adultSeniorCount == 0)) throw new MOBUnitedException("9999", _configuration.GetValue<string>("UnaccompaniedDisclaimerMessage"));
        }

        private async System.Threading.Tasks.Task<FlightReservationResponse> RegisterFareLockOffer(Session session, MOBRequest request, MOBAddTravelersRequest addTravelersRequest, string sessionId, string cartid, FlightReservationResponse shoppindownResponse, Reservation reservation)
        {
            var response = new FlightReservationResponse();
            bool delete = false;
            string cartKey = "FLK";
            string productCode = "FLK";
            List<string> productIds = new List<string>() { };
            var pointOfSale = !string.IsNullOrEmpty(reservation.PointOfSale) ? reservation.PointOfSale : "US";

            var fareLockProduct = reservation.TravelOptions.FirstOrDefault(x => x.Key.Equals("FareLock", StringComparison.InvariantCultureIgnoreCase));
            double fareLockHoldDays = fareLockProduct.SubItems.Where(x => x.Key.Contains("FareLockHoldDays")).Select(x => x.Amount).FirstOrDefault();

            var selectedFareLock = shoppindownResponse.FareLockResponse.Offers.FirstOrDefault().ProductInformation.ProductDetails.FirstOrDefault(x => x.Product.Code == "FLK").Product.SubProducts.
                FirstOrDefault(x => Convert.ToDouble(x.Name) == fareLockHoldDays);

            if (shoppindownResponse.FareLockResponse != null)
            {
                 response = await RegisterOffer(sessionId, cartid, cartKey, addTravelersRequest.LanguageCode, pointOfSale, productCode, selectedFareLock.ID, productIds, selectedFareLock.SubGroupCode, delete, request.Application.Id, request.DeviceId, request.Application.Version.Major, session.IsReshopChange, shoppindownResponse.FareLockResponse, reservation, response);
                
            }
            return response;
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

        private async Task<ShopRequest> BuildShopPinDownDetailsRequest(MOBAddTravelersRequest request, FlightReservationResponse flightReservationResponse, CSLShopRequest cslShopRequest, Reservation bookingpathReservation, Session session)
        {
            var shopRequest = new ShopRequest
            {
                AwardTravel = request.IsAward,
                NGRP = request.IsAward,
                AccessCode = _configuration.GetValue<string>("AccessCode - CSLShopping"),
                ChannelType = _configuration.GetValue<string>("Shopping - ChannelType"),
                CountryCode = cslShopRequest?.ShopRequest?.CountryCode ?? flightReservationResponse?.DisplayCart?.CountryCode ?? "US",
                InclCancelledFlights = false,
                InclOAMain = true,
                InclStarMain = true,
                InclUACodeshares = true,
                InclUAMain = true,
                InclUARegionals = true,
                InitialShop = true,
                EmployeeDiscountId = session?.EmployeeId,
                LangCode = request.LanguageCode,
                StopsInclusive = true,
                LoyaltyId = request.MileagePlusNumber,
                RememberedLoyaltyId = request.MileagePlusNumber,
                TrueAvailability = true,
                UpgradeComplimentaryRequested = true,
                EliteLevel = cslShopRequest?.ShopRequest?.EliteLevel ?? 0,
                SortType = cslShopRequest?.ShopRequest?.SortType ?? string.Empty,
                BookingCodesSpecified = cslShopRequest?.ShopRequest?.BookingCodesSpecified ?? string.Empty,
                DisableMostRestrictive = cslShopRequest?.ShopRequest?.DisableMostRestrictive ?? false,
                Stops = cslShopRequest?.ShopRequest?.Stops ?? 0,
                SearchTypeSelection = GetSearchType(flightReservationResponse.Reservation.Characteristic.FirstOrDefault(x => x.Code == "TRIP_TYPE").Value),
                Trips = GetTripsForShopPinDown(flightReservationResponse, request.TravelerTypes, cslShopRequest),
            };
            shopRequest.DisablePricingBySlice = _shoppingUtility.EnableRoundTripPricing(request.Application.Id, request.Application.Version.Major);
            shopRequest.DecodesOnTimePerfRequested = _configuration.GetValue<bool>("DecodesOnTimePerformance");
            shopRequest.DecodesRequested = _configuration.GetValue<bool>("DecodesRequested");
            shopRequest.IncludeAmenities = _configuration.GetValue<bool>("IncludeAmenities");
            shopRequest.CartId = request.CartId;
            shopRequest.DeviceId = request.DeviceId;

            if (!string.IsNullOrWhiteSpace(request.MileagePlusNumber))
            {
                OwnerResponseModel profileOwnerResponse = new OwnerResponseModel();
                var balance = 0;
                if (cslShopRequest?.ShopRequest?.LoyaltyPerson == null)
                {
                    profileOwnerResponse = await _sessionHelperService.GetSession<OwnerResponseModel>(request.SessionId, ObjectNames.CSLGetProfileOwnerResponse, new List<string> { request.SessionId, ObjectNames.CSLGetProfileOwnerResponse }).ConfigureAwait(false);
                    if (profileOwnerResponse != null && profileOwnerResponse.MileagePlus != null && profileOwnerResponse.MileagePlus.Data != null)
                    {
                        var bal = profileOwnerResponse.MileagePlus.Data.Balances?.FirstOrDefault(balnc => (int)balnc.Currency == 5)?.Amount ?? 0;
                        balance = Convert.ToInt32(bal);
                    }
                }

                shopRequest.LoyaltyPerson = new Service.Presentation.PersonModel.LoyaltyPerson();
                shopRequest.LoyaltyPerson.LoyaltyProgramMemberID = cslShopRequest?.ShopRequest?.LoyaltyPerson?.LoyaltyProgramMemberID ?? profileOwnerResponse?.MileagePlus?.Data?.MileageplusId;
                shopRequest.LoyaltyPerson.LoyaltyProgramMemberTierLevel = cslShopRequest?.ShopRequest?.LoyaltyPerson?.LoyaltyProgramMemberTierLevel ?? (Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel) profileOwnerResponse?.MileagePlus?.Data?.MPTierLevel;
                balance = cslShopRequest?.ShopRequest?.LoyaltyPerson?.AccountBalances?.FirstOrDefault()?.Balance ?? balance;

                shopRequest.LoyaltyPerson.AccountBalances = new Collection<Service.Presentation.CommonModel.LoyaltyAccountBalance>()
                {
                        new Service.Presentation.CommonModel.LoyaltyAccountBalance
                        {
                            Balance = balance,
                            BalanceType = Service.Presentation.CommonEnumModel.LoyaltyAccountBalanceType.MilesBalance
                        }
                };
            }
            List<string> ptcList = null;
            if (request.IsAward)
            {
                ptcList = cslShopRequest?.ShopRequest?.PaxInfoList?.FirstOrDefault(x => x?.PtcList != null)?.PtcList ?? null;
            }
            request.TravelerTypes = UpdateRequestForInfants(bookingpathReservation, request.TravelerTypes);

            Func<PaxType, int, PaxInfo> getPax = (type, subtractYear) => new PaxInfo { PaxType = type, DateOfBirth = DateTime.Today.AddYears(subtractYear).ToShortDateString(), PtcList = ptcList };
            shopRequest.PaxInfoList = new List<PaxInfo>();

            if (request.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Adult.ToString()) && request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Adult.ToString()).Count > 0)
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Adult.ToString()).Count).Select(x => getPax(PaxType.Adult, -20)));

            if (request.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Senior.ToString()) && request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Senior.ToString()).Count > 0)
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Senior.ToString()).Count).Select(x => getPax(PaxType.Senior, -67)));

            if (request.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child15To17.ToString()) && request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child15To17.ToString()).Count > 0)
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child15To17.ToString()).Count).Select(x => getPax(PaxType.Child05, -16)));

            if (request.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child12To14.ToString()) && request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child12To14.ToString()).Count > 0)
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child12To14.ToString()).Count).Select(x => getPax(PaxType.Child04, -13)));

            if (request.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child5To11.ToString()) && request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child5To11.ToString()).Count > 0)
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child5To11.ToString()).Count).Select(x => getPax(PaxType.Child02, -8)));

            if (request.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child2To4.ToString()) && request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child2To4.ToString()).Count > 0)
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child2To4.ToString()).Count).Select(x => getPax(PaxType.Child01, -3)));

            /*
             * commented as we are not using the below code
             * if (unfinishedBooking.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child12To17.ToString()) && unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child12To17.ToString()).Count > 0)
            shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, unfinishedBooking.TravelerTypes.First(t => t.TravelerType == PAXTYPE.Child12To17.ToString()).Count).Select(x => getPax(PaxType.Child03, -15))); */

            if (request.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.InfantSeat.ToString()) && request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.InfantSeat.ToString()).Count > 0)
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.InfantSeat.ToString()).Count).Select(x => getPax(PaxType.InfantSeat, -1)));

            if (request.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.InfantLap.ToString()) && request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.InfantLap.ToString()).Count > 0)
                shopRequest.PaxInfoList.AddRange(Enumerable.Range(1, request.TravelerTypes.First(t => t.TravelerType == PAXTYPE.InfantLap.ToString()).Count).Select(x => getPax(PaxType.InfantLap, -1)));

            return shopRequest;
        }

        private List<Trip> GetTripsForShopPinDown(FlightReservationResponse flightReservationResponse, List<MOBTravelerType> travelerTypes, CSLShopRequest cslShopRequest = null)
        {
            List<Trip> trips = new List<Trip>();
            foreach (var cslTrip in flightReservationResponse.DisplayCart.DisplayTrips)
            {
                var trip = new Trip
                {
                    DepartDate = cslTrip.DepartDate,
                    DepartTime = cslTrip.Flights[0].DepartDateTime,
                    OriginAllAirports = cslShopRequest?.ShopRequest.Trips.FirstOrDefault().OriginAllAirports ?? false,
                    Destination = cslTrip.Destination,
                    FlightCount = cslTrip.Flights.Count,
                    Origin = cslTrip.Origin,
                    Flights = cslTrip.Flights.Select(x => MapToFlightShoppingFlight(x, flightReservationResponse, travelerTypes)).ToList(),
                    SearchFiltersIn = new SearchFilterInfo()
                    {
                        FareFamily = cslShopRequest?.ShopRequest?.Trips?.FirstOrDefault(x => x?.SearchFiltersIn != null)?.SearchFiltersIn?.FareFamily ?? string.Empty
                    },
                    SearchFiltersOut = new SearchFilterInfo()
                    {
                        ShopIndicators = new ShopIndicators { IsBESelected = cslShopRequest?.ShopRequest?.Trips?.FirstOrDefault(x => x.SearchFiltersOut != null)?.SearchFiltersOut?.ShopIndicators?.IsBESelected ?? false }
                    }
                };
                if (trip.Flights.Any() && trip.Flights.GroupBy(x => x.FlightNumber).Any(g => g.Count() > 1))
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
                trips.Add(trip);
            }
            return trips;
        }

        private Flight MapToFlightShoppingFlight(Flight cslFlight, FlightReservationResponse flightReservationResponse, List<MOBTravelerType> travelerTypes)
        {
            var flight = new Flight
            {
                Aircraft = new Aircraft(),
                EquipmentDisclosures = new EquipmentDisclosure(),
                FlightInfo = new FlightInfo(),
                BookingCode = cslFlight.BookingCode,
                DepartDateTime = cslFlight.DepartDateTime,
                Destination = cslFlight.Destination,
                Origin = cslFlight.Origin,
                FlightNumber = cslFlight.FlightNumber,
                MarketingCarrier = cslFlight.MarketingCarrier,
            };
            if (cslFlight.Products != null && cslFlight.Products.Count > 0)
            {
                flight.Products = new ProductCollection();

                foreach (var product in cslFlight.Products)
                {
                    if (product != null && !string.IsNullOrEmpty(product.ProductType))
                    {
                        flight.Products.Add(MapToFlightShoppingFlightProduct(product, flightReservationResponse, travelerTypes, cslFlight));
                    }
                }
            }

            if (cslFlight.Connections == null)
                return flight;

            cslFlight.Connections.ForEach(x => flight.Connections.Add(MapToFlightShoppingFlight(x, flightReservationResponse, travelerTypes)));

            return flight;
        }
        private United.Services.FlightShopping.Common.Product MapToFlightShoppingFlightProduct(Product flightProduct, FlightReservationResponse flightReservationResponse, List<MOBTravelerType> travelerTypes, Flight cslFlight)
        {
            if (flightProduct != null)
            {
                var product = new United.Services.FlightShopping.Common.Product
                {
                    BookingCode = flightProduct.BookingCode,
                    ProductType = flightProduct.ProductType,
                    TripIndex = cslFlight.TripIndex - 1,
                    PromoDescription = flightProduct?.PromoDescription
                };
                if (flightProduct.Prices?.Count > 0)
                {
                    product.Prices = new List<PricingItem>();
                    foreach (var price in flightProduct.Prices)
                    {
                        if (price != null && !string.IsNullOrEmpty(price.PricingType))
                        {
                            for (var i = 0; i < travelerTypes.Where(x => x.Count > 0).Select(x => x.Count).Sum(); i++)
                            {
                                product.Prices.Add(MapToFlightShoppingFlightProductPrice(price));
                            }
                        }
                    }
                }
                return product;
            }
            return null;
        }

        private PricingItem MapToFlightShoppingFlightProductPrice(PricingItem productPrice)
        {
            if (productPrice != null)
            {
                PricingItem price = new PricingItem
                {
                    Amount = productPrice.Amount,
                    AmountAllPax = productPrice.AmountAllPax,
                    AmountBase = productPrice.AmountBase,
                    Currency = productPrice.Currency,
                    CurrencyAllPax = productPrice.CurrencyAllPax,
                    OfferID = productPrice.OfferID,
                    PricingType = productPrice.PricingType,
                    Selected = productPrice.Selected,
                    MerchPriceDetail = new MerchPriceDetail { EDDCode = productPrice.MerchPriceDetail?.EDDCode, ProductCode = productPrice.MerchPriceDetail?.ProductCode }
                };
                if (productPrice.SegmentMappings?.Count > 0)
                {
                    price.SegmentMappings = new List<SegmentMapping>();
                    foreach (var segmentMapping in productPrice.SegmentMappings)
                    {
                        if (string.IsNullOrEmpty(segmentMapping?.SegmentRefID))
                        {
                            price.SegmentMappings.Add(MapToFlightShoppingProductSegmentMapping(segmentMapping));
                        }
                    }
                }
                return price;
            }
            return null;
        }
        private SegmentMapping MapToFlightShoppingProductSegmentMapping(SegmentMapping flightSegmentMapping)
        {
            if (flightSegmentMapping != null && !string.IsNullOrEmpty(flightSegmentMapping.SegmentRefID))
            {
                SegmentMapping segmentMapping = new SegmentMapping
                {
                    Origin = flightSegmentMapping.Origin,
                    Destination = flightSegmentMapping.Destination,
                    BBxHash = flightSegmentMapping.BBxHash,
                    UpgradeStatus = flightSegmentMapping.UpgradeStatus,
                    UpgradeTo = flightSegmentMapping.UpgradeTo,
                    FlightNumber = flightSegmentMapping.FlightNumber,
                    CabinDescription = flightSegmentMapping.CabinDescription,
                    SegmentRefID = flightSegmentMapping.SegmentRefID
                };
                return segmentMapping;
            }
            return null;
        }

        public string GetCSSPublicKeyPersistSessionStaticGUID(int applicationId)
        {
            #region Get Aplication and Profile Ids
            string[] cSSPublicKeyPersistSessionStaticGUIDs = _configuration.GetValue<string>("CSSPublicKeyPersistSessionStaticGUID").Split('|');
            List<string> applicationDeviceTokenSessionIDList = new List<string>();
            foreach (string applicationSessionGUID in cSSPublicKeyPersistSessionStaticGUIDs)
            {
                #region
                if (Convert.ToInt32(applicationSessionGUID.Split('~')[0].ToString().ToUpper().Trim()) == applicationId)
                {
                    return applicationSessionGUID.Split('~')[1].ToString().Trim();
                }
                #endregion
            }
            return "1CSSPublicKeyPersistStatSesion4IphoneApp";
            #endregion
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

        private async Task<List<MOBItem>> SetElfShopMessage(bool isMultiTravelers, bool isSSA)
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

        private async Task<List<MOBItem>> GetELFShopMessages(string elfDocumentLibraryTableKey)
        {
            List<MOBItem> elfShopMessageList = await GetMPPINPWDTitleMessages(new List<string>() { elfDocumentLibraryTableKey });
            return elfShopMessageList;
        }


        private void FireForGetInFlightCLEligibilityCheck(Reservation reservation, MOBRequest request, Session session)
        {
            if (!reservation.IsReshopChange)
            {
                System.Threading.Tasks.Task.Factory.StartNew(async () =>
                {
                    await IsEligibleInflightContactlessPayment(reservation, request, session);
                });
            }
        }
        private async Task<MOBSHOPInflightContactlessPaymentEligibility> IsEligibleInflightContactlessPayment(United.Mobile.Model.Shopping.Reservation reservation, MOBRequest request, Session session)
        {
            MOBSHOPInflightContactlessPaymentEligibility eligibility = new MOBSHOPInflightContactlessPaymentEligibility(false, null, null);
            try
            {
                Collection<United.Service.Presentation.ProductModel.ProductSegment> list = await GetInflightPurchaseEligibility(reservation, request, session);
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
        private async Task<Collection<ProductSegment>> GetInflightPurchaseEligibility(United.Mobile.Model.Shopping.Reservation reservation, MOBRequest request, Session session)
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
                //#TakeTokenFromSession
                //string token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration).ConfigureAwait(false);
                var response = (await _purchaseMerchandizingService.GetInflightPurchaseInfo<ProductEligibilityResponse>(session.Token, "GetProductEligibility", jsonRequest, _headers.ContextValues.SessionId).ConfigureAwait(false)).response;

                if (response == null)
                {
                    return null;
                }

                if (response?.FlightSegments?.Count == 0)
                {
                    _logger.LogError("GetInflightPurchaseEligibility Failed to deserialize CSL response {@Response}", JsonConvert.SerializeObject(response));
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
                _logger.LogError("GetInflightPurchaseEligibility {@Exception}", ex.Message);
            }
            return null;
        }

        public Reservation ReservationToPersistReservation(MOBSHOPAvailability availability, bool addTravelerFlow = false)
        {
            var reservation = new Reservation
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
            if (addTravelerFlow && (availability?.Reservation?.FormOfPaymentType == MOBFormofPayment.PayPal || availability?.Reservation?.FormOfPaymentType == MOBFormofPayment.PayPalCredit) && availability?.Reservation?.PayPal != null)
            {
                reservation.PayPal = availability.Reservation.PayPal;
                reservation.PayPalPayor = availability.Reservation.PayPalPayor;
                reservation.FormOfPaymentType = availability.Reservation.FormOfPaymentType;
            }
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

        //private string GetPkDispenserPublicKey(int applicationId, string deviceId, string appVersion, string transactionId, string token)
        //{
        //    //**RSA Publick Key Implmentaion**//
        //    string request = string.Empty, guidForLogEntries = transactionId, pkDispenserPublicKey = string.Empty;
        //    string path = string.Empty;
        //    #region

        //    token = string.IsNullOrEmpty(token) ? _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration).Result : token;

        //    var response = _pKDispenserService.GetPkDispenserPublicKey<PKDispenserResponse>(token, _headers.ContextValues.SessionId, path);
        //    if (response != null)
        //    {
        //        if (response != null && response.Keys != null && response.Keys.Count > 0)
        //        {

        //            var obj = (from st in response.Keys
        //                       where st.CryptoTypeId == 2
        //                       select st).ToList();
        //            pkDispenserPublicKey = obj[0].PublicKey;

        //        }
        //        else
        //        {
        //            string exceptionMessage = _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
        //            if (!String.IsNullOrEmpty(_configuration.GetValue<string>("UnableToGetPkDispenserPublicKeyErrorMessage")))
        //            {
        //                exceptionMessage = _configuration.GetValue<string>("UnableToGetPkDispenserPublicKeyErrorMessage");
        //            }
        //            throw new MOBUnitedException(exceptionMessage);
        //        }
        //    }
        //    else
        //    {
        //        string exceptionMessage = _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
        //        if (!String.IsNullOrEmpty(_configuration.GetValue<string>("UnableToGetPkDispenserPublicKeyErrorMessage")))
        //        {
        //            exceptionMessage = _configuration.GetValue<string>("UnableToGetPkDispenserPublicKeyErrorMessage");
        //        }
        //        throw new MOBUnitedException(exceptionMessage);
        //    }

        //    _logger.LogInformation("GetPkDispenserPublicKey - Client Response {to client Response} and {TransactionId}", pkDispenserPublicKey, transactionId);
        //    #endregion
        //    //if (applicationId != 3)
        //    //{
        //    pkDispenserPublicKey = pkDispenserPublicKey.Replace("\r", "").Replace("\n", "").Replace("-----BEGIN PUBLIC KEY-----", "").Replace("-----END PUBLIC KEY-----", "").Trim();
        //    //}
        //    _sessionHelperService.SaveSession<string>(pkDispenserPublicKey, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, "pkDispenserPublicKey" }, "pkDispenserPublicKey");
        //    //United.Persist.FilePersist.Save<string>(Utility.GetCSSPublicKeyPersistSessionStaticGUID(applicationId), "pkDispenserPublicKey", pkDispenserPublicKey);
        //    return pkDispenserPublicKey;

        //}

        private async Task<List<MOBSHOPTrip>> PopulateMetaTrips(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string cartId, ShoppingResponse persistShop, string sessionId, int appId, string deviceId, string appVersion, bool showMileageDetails, List<United.Services.FlightShopping.Common.DisplayCart.DisplayTrip> flightSegmentCollection, string fareClass, List<string> flightDepartDatesForSelectedTrip)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);
            supressLMX = session.SupressLMXForAppID;
            #endregion
            List<MOBSHOPTrip> trips = new List<MOBSHOPTrip>();
            try
            {
                airportsList = await _sessionHelperService.GetSession<AirportDetailsList>(sessionId, new AirportDetailsList().ObjectName, new List<string> { sessionId, new AirportDetailsList().ObjectName }).ConfigureAwait(false);
                if (_configuration.GetValue<bool>("GetAirportNameInOneCallToggle") && (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0))
                {
                    airportsList = await GetAllAiportsList(flightSegmentCollection);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllAiportsList {@PopulateMetaTrips-GetAllAiportsList}", JsonConvert.SerializeObject(ex));
                _logger.LogError("GetAllAiportsList {@PopulateMetaTrips - DisplayTrip}", JsonConvert.SerializeObject(flightSegmentCollection));
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

                    trip.DepartDate = GeneralHelper.FormatDateFromDetails(flightSegmentCollection[i].DepartDate);
                    trip.ArrivalDate = GeneralHelper.FormatDateFromDetails(flightSegmentCollection[i].Flights[flightSegmentCollection[i].Flights.Count - 1].DestinationDateTime);
                    trip.Destination = GetAirportCode(flightSegmentCollection[i].Destination);
                    trip.ShareMessage = string.IsNullOrEmpty(trip.ShareMessage) ? string.Empty : trip.ShareMessage;

                    if (_omniCart.IsEnableOmniCartMVP2Changes(appId, appVersion, true) && !_configuration.GetValue<bool>("DisablePopulatingCabinFix"))
                    {
                        trip.Columns = PopulateColumns(flightSegmentCollection[i].ColumnInformation);
                    }
                    CultureInfo ci = null;

                    List<MOBSHOPFlight> flights = null;
                    if (flightSegmentCollection[i].Flights != null && flightSegmentCollection[i].Flights.Count > 0)
                    {
                        //update amenities for all flights
                        UpdateAmenitiesIndicatorsResponse amenitiesResponse = new UpdateAmenitiesIndicatorsResponse();
                        List<Flight> tempFlights = new List<Flight>(flightSegmentCollection[i].Flights);

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
                                    amenitiesResponse = await GetAmenitiesForFlights(sessionId, cartId, tempFlights, appId, deviceId, appVersion).ConfigureAwait(false);
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

                        flightSegmentCollection[i].Flights = new List<Flight>(tempFlights);
                        if (_mOBSHOPDataCarrier == null)
                            _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
                        var tupleRes = await GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, flightSegmentCollection[i].Flights, string.Empty,  0.0M, trip.Columns, 0, fareClass, false);
                        flights = tupleRes.flights;
                        ci = tupleRes.ci;
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
                            flattenedFlight.FlightHash = flight.FlightHash;
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

                                    connection.DepartureDateTimeGMT = isEnabledGMTConversionUsingCslData? await _shoppingUtility.GetGMTTimeFromOffset(connection.DepartureDateTime, connection.OriginTimezoneOffset): await GetGMTTime(connection.DepartureDateTime, connection.Origin, sessionId);
                                    connection.ArrivalDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(connection.ArrivalDateTime, connection.DestinationTimezoneOffset) :await GetGMTTime(connection.ArrivalDateTime, connection.Destination, sessionId);

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
        private async Task<string> GetGMTTime(string localTime, string airportCode, string sessionId)
        {
            var gmtTime = await _gMTConversionService.GETGMTTime(localTime, airportCode, sessionId).ConfigureAwait(false);
            if (!String.IsNullOrEmpty(gmtTime))
            {
                DateTime getDateTime;
                DateTime.TryParse(gmtTime, out getDateTime);
                return getDateTime.ToString("MM/dd/yyyy hh:mm tt");
            }
            return localTime;
        }
        private List<TravelOption> GetTravelOptions(DisplayCart displayCart, bool isReshop, int appID, string appVersion)
        {
            List<TravelOption> travelOptions = null;
            if (displayCart != null && displayCart.TravelOptions != null && displayCart.TravelOptions.Count > 0)
            {
                CultureInfo ci = null;
                travelOptions = new List<TravelOption>();
                bool addTripInsuranceInTravelOption =
                    !_configuration.GetValue<bool>("ShowTripInsuranceBookingSwitch")
                    && Convert.ToBoolean(_configuration.GetValue<string>("ShowTripInsuranceSwitch") ?? "false");
                foreach (var anOption in displayCart.TravelOptions)
                {
                    //wade - added check for farelock as we were bypassing it
                    if (!anOption.Type.Equals("Premium Access") && !anOption.Key.Trim().ToUpper().Contains("FARELOCK") && !(addTripInsuranceInTravelOption && anOption.Key.Trim().ToUpper().Contains("TPI"))
                    && !(_shoppingUtility.EnableEPlusAncillary(appID, appVersion, isReshop) && anOption.Key.Trim().ToUpper().Contains("EFS")))
                    {
                        continue;
                    }
                    if (ci == null)
                    {
                        ci = TopHelper.GetCultureInfo(anOption.Currency);
                    }

                    TravelOption travelOption = new TravelOption();
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
                    if (_shoppingUtility.EnableEPlusAncillary(appID, appVersion, isReshop) && anOption.SubItems != null && anOption.SubItems.Count > 0)
                    {
                        travelOption.BundleCode = GetTravelOptionEplusAncillary(anOption.SubItems, travelOption.BundleCode);
                        GetTravelOptionAncillaryDescription(anOption.SubItems, travelOption, displayCart);
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

        public async Task<MOBSHOPAvailability> GetShopPinDownDetails(Session session, MOBSHOPSelectUnfinishedBookingRequest request)
        {
            var availability = new MOBSHOPAvailability { SessionId = session.SessionId };
            ShopRequest shopPindownRequest = BuildShopPinDownDetailsRequest(request);
            await _sessionHelperService.SaveSession(shopPindownRequest, session.SessionId, new List<string> { session.SessionId, typeof(ShopRequest).FullName }, typeof(ShopRequest).FullName).ConfigureAwait(false);// SAVING THIS SHOP PIN DOWN REQUEST TO USE LATER ON FOR GET BUNDLES CALL

            var tupleResponse = await GetShopPinDown(session, request.Application.Version.Major, shopPindownRequest);
            var response = tupleResponse.Item1;
            if (await _featureSettings.GetFeatureSettingValue("EnableAddTravelers").ConfigureAwait(false))
            {
                await _sessionHelperService.SaveSession(response, session.SessionId, new List<string> { session.SessionId, new FlightReservationResponse().GetType().FullName }, new FlightReservationResponse().GetType().FullName).ConfigureAwait(false);
            }
            if (response != null && response.Status.Equals(StatusType.Success) && response.Reservation != null)
            {
                var mobResRev = await PopulateReservation(session, response.Reservation);

                if (response.DisplayCart != null && response.DisplayCart.DisplayPrices != null && response.Reservation != null)
                {
                    availability.CartId = response.CartId;
                    availability.Reservation = new MOBSHOPReservation(_configuration, _cachingService);
                    if (!_configuration.GetValue<bool>("BasicEconomyRestrictionsForShareFlightsBugFixToggle") == true && request.SelectedUnfinishBooking.IsSharedFlightRequest)
                    {
                        availability.Reservation.IsMetaSearch = true;
                    }
                    availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();
                    availability.Reservation.ShopReservationInfo2.Characteristics = ShopStaticUtility.GetCharacteristics(response.Reservation);
                    availability.Reservation.ShopReservationInfo2.IsIBELite = _shoppingUtility.IsIBELiteFare(response.DisplayCart);
                    availability.Reservation.ShopReservationInfo2.IsIBE = _shoppingUtility.IsIBEFullFare(response.DisplayCart);
                    availability.Reservation.ShopReservationInfo2.IsNonRefundableNonChangable = _shoppingUtility.IsNonRefundableNonChangable(response.DisplayCart);
                    availability.Reservation.ShopReservationInfo2.AllowAdditionalTravelers = !session.IsCorporateBooking;
                    availability.Reservation.ShopReservationInfo2.IsUnfinihedBookingPath = true;
                    availability.Reservation.IsSSA = _shoppingUtility.EnableSSA(request.Application.Id, request.Application.Version.Major);
                    availability.Reservation.IsELF = response.DisplayCart.IsElf;
                    var eLFRitMetaShopMessages = new ELFRitMetaShopMessages(_configuration, _dynamoDBService, _legalDocumentsForTitlesService, _headers);
                    availability.Reservation.ELFMessagesForRTI = await eLFRitMetaShopMessages.GetELFShopMessagesForRestrictions(availability.Reservation, request.Application.Id);
                    availability.Reservation.IsUpgradedFromEntryLevelFare = response.DisplayCart.IsUpgradedFromEntryLevelFare && !_shoppingUtility.IsIBELiteFare(response.DisplayCart.ProductCodeBeforeUpgrade);
                    availability.Reservation.PointOfSale = response.Reservation.PointOfSale.Country.CountryCode;
                    availability.Reservation.CartId = response.CartId;
                    availability.Reservation.SessionId = session.SessionId;

                    availability.Reservation.NumberOfTravelers = response.Reservation.NumberInParty;
                    availability.Reservation.Trips = await PopulateMetaTrips(new MOBSHOPDataCarrier(), response.CartId, null, session.SessionId, request.Application.Id, request.DeviceId, request.Application.Version.Major, true, response.DisplayCart.DisplayTrips, "", new List<string>());
                    switch (response.DisplayCart.SearchType)
                    {
                        case SearchType.OneWay:
                            availability.Reservation.SearchType = "OW";
                            break;
                        case SearchType.RoundTrip:
                            availability.Reservation.SearchType = "RT";
                            break;
                        case SearchType.MultipleDestination:
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
                        availability.Reservation.Prices = GetPrices(response.DisplayCart.DisplayPrices);
                    }
                    bool Is24HoursWindow = false;
                    if (_configuration.GetValue<bool>("EnableForceEPlus"))
                    {
                        if (availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT != null)
                        {
                            Is24HoursWindow = TopHelper.Is24HourWindow(Convert.ToDateTime(availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT));
                        }
                    }

                    availability.Reservation.ShopReservationInfo2.IsForceSeatMap = _shoppingUtility.IsForceSeatMapforEPlus(false, response.DisplayCart.IsElf, Is24HoursWindow, request.Application.Id, request.Application.Version.Major);
                    bool isSupportedVersion = GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, "AndroidBundleVersion", "IOSBundleVersion", "", "", true, _configuration);
                    if (isSupportedVersion)
                    {
                        if (_configuration.GetValue<bool>("IsEnableBundlesForBasicEconomyBooking"))
                        {
                            availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles");
                        }
                        else
                        {
                            availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles") && !(response.DisplayCart.IsElf || _shoppingUtility.IsIBEFullFare(response.DisplayCart));
                        }
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
                                _logger.LogWarning("GetShopPinDownDetails - Response with Inhibit warning {@Response}", JsonConvert.SerializeObject(response));

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
                                if (_shoppingUtility.EnableBoeingDisclaimer())
                                {
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                                }
                            }
                        }
                    }

                    if (_shoppingUtility.EnableBoeingDisclaimer() && _shoppingUtility.IsBoeingDisclaimer(response.DisplayCart.DisplayTrips))
                    {
                        if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                        if (!availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.BOEING737WARNING.ToString()))
                        {
                            _logger.LogWarning("GetShopBookingDetails - Response with Inhibit warning {@Response}", JsonConvert.SerializeObject(response));
                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();
                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetBoeingDisclaimer());

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

                            if (ShopStaticUtility.IdentifyBEFareInversion(response))
                            {
                                if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                                if (_shoppingUtility.IsNonRefundableNonChangable(response.DisplayCart.ProductCodeBeforeUpgrade))
                                {
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(await _shoppingUtility.GetNonRefundableNonChangableInversionMessage(request, session));
                                }
                                else
                                {
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetBEMessage());
                                }
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                            }
                        }
                    }

                    #endregion

                    await _shoppingUtility.SetELFUpgradeMsg(availability, response?.DisplayCart?.ProductCodeBeforeUpgrade, request, session);

                    if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Any())
                    {
                        availability.Reservation.Prices.AddRange(GetPrices(response.DisplayCart.DisplayFees));
                    }

                    availability.Reservation.Prices = AdjustTotal(availability.Reservation.Prices);

                    if (_shoppingUtility.IsEnableTaxForAgeDiversification(session.IsReshopChange, request.Application.Id, request.Application.Version.Major))
                    {
                        availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();
                        availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = _shoppingUtility.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices);
                        if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                        {
                            availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes.Add(ShopStaticUtility.AddFeesAfterPriceChange(response.DisplayCart.DisplayFees));
                        }
                    }
                    else
                    {
                        availability.Reservation.Taxes = GetTaxAndFees(response.DisplayCart.DisplayPrices, response.Reservation.NumberInParty);

                        if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                        {
                            //combine fees into taxes so that totals are correct
                            List<DisplayPrice> tempList = new List<DisplayPrice>();
                            tempList.AddRange(response.DisplayCart.DisplayPrices);
                            tempList.AddRange(response.DisplayCart.DisplayFees);
                            availability.Reservation.Taxes = GetTaxAndFees(tempList, response.Reservation.NumberInParty);
                        }
                    }
                    availability.Reservation.TravelOptions = GetTravelOptions(response.DisplayCart, session.IsReshopChange, request.Application.Id, request.Application.Version.Major);

                    if (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, session.IsReshopChange) && request.SelectedUnfinishBooking.TravelerTypes != null && request.SelectedUnfinishBooking.TravelerTypes.Count > 0)
                    {
                        availability.TravelerCount = request.SelectedUnfinishBooking.TravelerTypes.Where(t => t.TravelerType != null && t.Count > 0).Select(t => t.Count).Sum();
                        availability.Reservation.NumberOfTravelers = availability.TravelerCount;

                        availability.Reservation.ShopReservationInfo2.TravelerTypes = request.SelectedUnfinishBooking.TravelerTypes;

                        availability.Reservation.ShopReservationInfo2.displayTravelTypes = ShopStaticUtility.GetDisplayTravelerTypes(response.DisplayCart.DisplayTravelers);
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
                        //Profile profile = new CSL.Profile();
                        availability.Reservation.CubaTravelInfo.CubaTravelTitles = await new MPDynamoDB(_configuration, _dynamoDBService, null, _headers).GetMPPINPWDTitleMessages(new List<string> { "CUBA_TRAVEL_CONTENT" });
                    }

                    if (_shoppingUtility.IsEnabledNationalityAndResidence(false, request.Application.Id, request.Application.Version.Major))
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

                availability.Reservation.ISFlexibleSegmentExist = IsOneFlexibleSegmentExist(availability.Reservation.Trips);
                availability.Reservation.FlightShareMessage = GetFlightShareMessage(availability.Reservation, string.Empty);
                availability.Reservation.IsRefundable = mobResRev.IsRefundable;
                availability.Reservation.ISInternational = mobResRev.ISInternational;

                //**RSA Publick Key Implmentaion**//

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
                        // populate avail. special needs for the itinerary
                        availability.Reservation.ShopReservationInfo2.SpecialNeeds = await GetItineraryAvailableSpecialNeeds(session, request.Application.Id, request.Application.Version.Major, request.DeviceId, response.Reservation.FlightSegments, "en-US");

                        // update persisted reservation object too
                        var bookingPathReservation = new Reservation();
                        bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(session.SessionId, bookingPathReservation.ObjectName, new List<string> { session.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                        if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null)
                        {
                            bookingPathReservation.ShopReservationInfo2.SpecialNeeds = availability.Reservation.ShopReservationInfo2.SpecialNeeds;
                            await _sessionHelperService.SaveSession(bookingPathReservation, session.SessionId, new List<string> { session.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("GetShopPinDownDetails - GetItineraryAvailableSpecialNeeds {@Exception}", JsonConvert.SerializeObject(ex));
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
                    availability.Reservation.ShopReservationInfo2.IsDisplayCart = _shoppingUtility.IsDisplayCart(session, GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("Android_EnableAwardLiveCart_AppVersion"), _configuration.GetValue<string>("iPhone_EnableAwardLiveCart_AppVersion"))
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
                await _sessionHelperService.SaveSession(persistedShopBookingDetailsResponse, session.SessionId, new List<string> { session.SessionId, persistedShopBookingDetailsResponse.ObjectName }, persistedShopBookingDetailsResponse.ObjectName).ConfigureAwait(false);

                var shop = new Mobile.Model.Shopping.ShoppingResponse
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
                        MileagePlusAccountNumber = request.MileagePlusAccountNumber
                    }
                };
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

                await _sessionHelperService.SaveSession(reservation, availability.SessionId, new List<string> { availability.SessionId, reservation.ObjectName }, reservation.ObjectName).ConfigureAwait(false);

                await SetAvailabilityELFProperties(availability, availability.Reservation.NumberOfTravelers > 1, _shoppingUtility.EnableSSA(request.Application.Id, request.Application.Version.Major));
            }
            return availability;
        }
        private async Task<List<MOBItem>> GetMPPINPWDTitleMessages(List<string> titleList)
        {
            bool isTermsnConditions = false;
            StringBuilder stringBuilder = new StringBuilder();
            if (!isTermsnConditions)
            {
                foreach (var title in titleList)
                {
                    stringBuilder.Append("'");
                    stringBuilder.Append(title);
                    stringBuilder.Append("'");
                    stringBuilder.Append(",");
                }
            }
            else
            {
                stringBuilder.Append(titleList[0]);
            }

            string reqTitles = stringBuilder.ToString().Trim(',');
            var docs = new List<MOBLegalDocument>();
            try
            {
                docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(reqTitles, _headers.ContextValues.TransactionId, true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
            }
            List<MOBItem> messages = new List<MOBItem>();
            if (!_configuration.GetValue<bool>("DisableCubaTravelContentOrderMismatchFix") && !string.IsNullOrEmpty(reqTitles) && reqTitles == "'CUBA_TRAVEL_CONTENT'" && docs != null && docs.Count > 0)
            {
                _shoppingUtility.AddMobLegalDocumentItem(docs, messages, "CubaPage1Title");

                _shoppingUtility.AddMobLegalDocumentItem(docs, messages, "CubaPage1Description");

                _shoppingUtility.AddMobLegalDocumentItem(docs, messages, "CubaPage1ReasonListButton");
            }
            else if (docs != null && docs.Count > 0)
            {
                foreach (MOBLegalDocument doc in docs)
                {
                    MOBItem item = new MOBItem
                    {
                        Id = doc.Title,
                        CurrentValue = doc.LegalDocument
                    };
                    messages.Add(item);
                }
            }
            return messages;
        }
        private async Task<bool> IsRequireNatAndCR(MOBSHOPReservation reservation, MOBApplication application, string sessionID, string deviceID, string token)
        {
            bool isRequireNatAndCR = false;
            List<string> NationalityResidenceCountriesList = new List<string>();

            #region Load list of countries from cache/persist
            //List<Country> lst =_sessionHelperService.GetSession<List<Country>>(_configuration.GetValue<string>("NationalityResidenceCountriesListStaticGUID").ToString(), "Booking2.0NationalityResidenceCountries");
            List<MOBSHOPCountry> lst = await _sessionHelperService.GetSession<List<MOBSHOPCountry>>(sessionID, typeof(List<MOBSHOPCountry>).FullName, new List<string> { sessionID, typeof(List<MOBSHOPCountry>).FullName }).ConfigureAwait(false); //change session
            #endregion Load list of countries from cache/persist

            if (lst == null)
            {
                lst = await GetNationalityResidenceCountries(application.Id, deviceID, application.Version.Major, null, sessionID, token);
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
        private async Task<List<MOBSHOPCountry>> GetNationalityResidenceCountries(int applicationId, string deviceId, string appVersion, string transactionId, string sessionID, string token)
        {
            List<MOBSHOPCountry> lstNationalityResidenceCountries = null;

            try
            {
                string logAction = "NationalityResidenceCountries";

                var response = await _referencedataService.GetNationalityResidence<List<United.Service.Presentation.CommonModel.Characteristic>>(logAction, token, sessionID).ConfigureAwait(false);

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

                _logger.LogError("GetNationalityResidenceCountries {@Exception}", JsonConvert.SerializeObject(ex));

            }
            if (deviceId.ToUpper().Trim() != "SCHEDULED_PublicKey_UPDADE_JOB".ToUpper().Trim())
            {
                //System.Threading.Tasks.Task.Factory.StartNew(() => Authentication.Write(logEntries));
            }

            return lstNationalityResidenceCountries;
        }
        public async Task<TravelSpecialNeeds> GetItineraryAvailableSpecialNeeds(Session session, int appId, string appVersion, string deviceId, IEnumerable<ReservationFlightSegment> segments, string languageCode
            , MOBSHOPReservation reservation = null, SelectTripRequest selectRequest = null)
        {
            MultiCallResponse flightshoppingReferenceData = null;
            IEnumerable<ReservationFlightSegment> pnrOfferedMeals = null;
            var offersSSR = new TravelSpecialNeeds();

            try
            {
                //Parallel.Invoke(() => flightshoppingReferenceData = GetSpecialNeedsReferenceDataFromFlightShopping(session, appId, appVersion, deviceId, languageCode),
                //                () => pnrOfferedMeals = GetOfferedMealsForItineraryFromPNRManagement(session, appId, appVersion, deviceId, segments));
                flightshoppingReferenceData = await GetSpecialNeedsReferenceDataFromFlightShopping(session, appId, appVersion, deviceId, languageCode);
                pnrOfferedMeals = await GetOfferedMealsForItineraryFromPNRManagement(session, appId, appVersion, deviceId, segments);
            }
            catch (Exception) // 'System.ArgumentException' is thrown when any action in the actions array throws an exception.
            {
                if (flightshoppingReferenceData == null) // unable to get reference data, POPULATE DEFAULT SPECIAL REQUESTS
                {
                    offersSSR.ServiceAnimalsMessages = new List<MOBItem> { new MOBItem { CurrentValue = _configuration.GetValue<string>("SSR_RefDataServiceFailure_ServiceAnimalMassage") } };

                    flightshoppingReferenceData = GetMultiCallResponseWithDefaultSpecialRequests();
                }
                else if (pnrOfferedMeals == null) // unable to get market restriction meals, POPULATE DEFAULT MEALS
                {
                    pnrOfferedMeals = PopulateSegmentsWithDefaultMeals(segments);
                }
            }

            //Parallel.Invoke(() => offersSSR.SpecialMeals = GetOfferedMealsForItinerary(pnrOfferedMeals, flightshoppingReferenceData),
            //                () => offersSSR.SpecialMealsMessages = GetSpecialMealsMessages(pnrOfferedMeals, flightshoppingReferenceData),
            //                () => offersSSR.SpecialRequests = GetOfferedSpecialRequests(flightshoppingReferenceData),
            //                () => offersSSR.SpecialMealsMessages = GetSpecialMealsMessages(pnrOfferedMeals, flightshoppingReferenceData),
            //                () => offersSSR.ServiceAnimals = GetOfferedServiceAnimals(flightshoppingReferenceData, segments, appId, appVersion));

            offersSSR.SpecialMeals = GetOfferedMealsForItinerary(pnrOfferedMeals, flightshoppingReferenceData);
            offersSSR.SpecialMealsMessages = GetSpecialMealsMessages(pnrOfferedMeals, flightshoppingReferenceData);
            offersSSR.SpecialRequests = await GetOfferedSpecialRequests(flightshoppingReferenceData, reservation, selectRequest, session);
            offersSSR.SpecialMealsMessages = GetSpecialMealsMessages(pnrOfferedMeals, flightshoppingReferenceData);
            offersSSR.ServiceAnimals = GetOfferedServiceAnimals(flightshoppingReferenceData, segments, appId, appVersion);
            offersSSR.SpecialNeedsAlertMessages = GetPartnerAirlinesSpecialTravelNeedsMessage(session, segments);
            _logger.LogInformation("{@SpecialMeals}", JsonConvert.SerializeObject(offersSSR.SpecialMeals));

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

            await AddServiceAnimalsMessageSection(offersSSR, appId, appVersion, session, deviceId);

            if (offersSSR.ServiceAnimalsMessages == null || !offersSSR.ServiceAnimalsMessages.Any())
                offersSSR.ServiceAnimalsMessages = GetServiceAnimalsMessages(offersSSR.ServiceAnimals);


            return offersSSR;
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
                            SpecialRequests = new Collection<Service.Presentation.CommonModel.Characteristic> (_configuration.GetValue<string>("SSR_DefaultSpecialRequests")
                                                                                                .Split('|')
                                                                                                .Select(request => request.Split('^'))
                                                                                                .Select(request => new Service.Presentation.CommonModel.Characteristic
                                                                                                {
                                                                                                    Code = request[0],
                                                                                                    Description = request[1],
                                                                                                    Genre = new Service.Presentation.CommonModel.Genre { Description = request[2]},
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

        private async Task<IEnumerable<ReservationFlightSegment>> GetOfferedMealsForItineraryFromPNRManagement(Session session, int appId, string appVersion, string deviceId, IEnumerable<ReservationFlightSegment> segments)
        {
            string cslActionName = "SpecialMeals/FlightSegments";

            string jsonRequest = JsonConvert.SerializeObject(GetOfferedMealsForItineraryFromPNRManagementRequest(segments));
            //#TakeTokenFromSession
            //  string token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration).ConfigureAwait(false);

            var cslCallDurationstopwatch = new Stopwatch();
            cslCallDurationstopwatch.Start();

            var response = await _pNRRetrievalService.GetOfferedMealsForItinerary<List<ReservationFlightSegment>>(session.Token, cslActionName, jsonRequest, session.SessionId).ConfigureAwait(false);

            if (cslCallDurationstopwatch.IsRunning)
            {
                cslCallDurationstopwatch.Stop();
            }

            if (response != null)
            {
                if (response == null)
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                if (response.Count > 0)
                {
                    await _sessionHelperService.SaveSession<List<ReservationFlightSegment>>(response, session.SessionId, new List<string> { session.SessionId, new ReservationFlightSegment().GetType().FullName }, new ReservationFlightSegment().GetType().FullName).ConfigureAwait(false);//, response[0].GetType().FullName);
                }

                return response;
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
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
        public async Task<MOBMobileCMSContentMessages> GetCMSContentMessageByKey(string Key, MOBRequest request, Session session)
        {

            CSLContentMessagesResponse cmsResponse = new CSLContentMessagesResponse();
            MOBMobileCMSContentMessages cmsMessage = null;
            List<CMSContentMessage> cmsMessages = null;
            try
            {
                var cmsContentCache = await _cachingService.GetCache<string>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID") + "MOBCSLContentMessagesResponse", request.TransactionId);
                try
                {
                    if (!string.IsNullOrEmpty(cmsContentCache))
                        cmsResponse = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(cmsContentCache);
                }
                catch { cmsContentCache = null; }

                if (string.IsNullOrEmpty(cmsContentCache) || Convert.ToBoolean(cmsResponse.Status) == false || cmsResponse.Messages == null)
                    cmsResponse = await _travelerCSL.GetBookingRTICMSContentMessages(request, session);

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
            catch (Exception)
            { }
            return cmsMessage;
        }

        public bool IsEnableWheelchairLinkUpdate(Session session)
        {
            return _configuration.GetValue<bool>("EnableWheelchairLinkUpdate") &&
                   session.CatalogItems != null &&
                   session.CatalogItems.Count > 0 &&
                   session.CatalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableWheelchairLinkUpdate).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableWheelchairLinkUpdate).ToString())?.CurrentValue == "1";
        }

        private async Task<List<TravelSpecialNeed>> GetOfferedSpecialRequests(MultiCallResponse flightshoppingReferenceData, MOBSHOPReservation reservation = null, SelectTripRequest selectRequest = null, Session session = null)
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
                        sr.Messages = new List<MOBItem> { new MOBItem { CurrentValue = _configuration.GetValue<string>("SSR_DPNA_Message") } };
                    await SetTaskTrainedServiceAnimalMessage(specialRequest, sr, reservation, selectRequest, null, selectRequest, session);
                    if (sr.Code != "OTHS")
                        specialRequests.Add(sr);
                }
                else if (specialRequest.Genre.Description.Equals("WheelchairReason"))
                {
                    if (createdWheelChairItem == null)
                    {
                        createdWheelChairItem = createSpecialNeedItem(_configuration.GetValue<string>("SSRWheelChairDescription"), null, _configuration.GetValue<string>("SSRWheelChairDescription"));
                        createdWheelChairItem.SubOptionHeader = _configuration.GetValue<string>("SSR_WheelChairSubOptionHeader");
                        // MOBILE-23726
                        if (IsEnableWheelchairLinkUpdate(session))
                        {
                            var sdlKeyForWheelchairLink = _configuration.GetValue<string>("FSRSpecialTravelNeedsWheelchairLinkKey");
                            MOBMobileCMSContentMessages message = null;
                            if (!string.IsNullOrEmpty(sdlKeyForWheelchairLink))
                            {
                                message = await GetCMSContentMessageByKey(sdlKeyForWheelchairLink, selectRequest, session);
                            }
                            createdWheelChairItem.InformationLink = message?.ContentFull ?? (_configuration.GetValue<string>("WheelchairLinkUpdateFallback") ?? "");
                        }

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

        private async System.Threading.Tasks.Task SetTaskTrainedServiceAnimalMessage(Characteristic specialRequest, TravelSpecialNeed sr, MOBSHOPReservation reservation = null, SelectTripRequest selectRequest = null,
           TraceSwitch _traceSwitch = null, MOBRequest request = null, Session session = null)
        {
            if (selectRequest != null && reservation != null && _shoppingUtility.IsServiceAnimalEnhancementEnabled(selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.CatalogItems) && specialRequest.Code.StartsWith(_configuration.GetValue<string>("TasktrainedServiceAnimalCODE"), StringComparison.OrdinalIgnoreCase)) // add info message for Task-trained service animal
            {

                List<CMSContentMessage> lstMessages = await _ffcShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");

                //First 2 lines only if International , HNL or GUM 
                if (reservation?.ISInternational == true
                    || (!string.IsNullOrWhiteSpace(reservation?.Trips?.FirstOrDefault()?.Origin) && _configuration.GetValue<string>("DisableServiceAnimalAirportCodes")?.Contains(reservation?.Trips?.FirstOrDefault()?.Origin) == true)
                    || (!string.IsNullOrWhiteSpace(reservation?.Trips?.FirstOrDefault()?.Destination) && _configuration.GetValue<string>("DisableServiceAnimalAirportCodes")?.Contains(reservation?.Trips?.FirstOrDefault()?.Destination) == true))
                {
                    sr.IsDisabled = true;
                    sr.Messages = new List<MOBItem> { new MOBItem {
                                Id = "ESAN_SUBTITLE",
                                CurrentValue =_shoppingUtility.GetSDLStringMessageFromList(lstMessages, "TravelNeeds_TaskTrainedDog_Screen_Content_INT_MOB")  } };
                }
                sr.SubOptions = new List<TravelSpecialNeed>();

                sr.SubOptions.Add(new TravelSpecialNeed
                {
                    Value = sr.Value,
                    Code = "SVAN",
                    Type = TravelSpecialNeedType.ServiceAnimalType.ToString(),
                    DisplayDescription = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "TravelNeeds_TaskTrainedDog_Screen_Content2_MOB"),
                    RegisterServiceDescription = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "TravelNeeds_TaskTrainedDog_Screen_Title_MOB")
                });
                if (sr.Messages == null) sr.Messages = new List<MOBItem>();
                ServiceAnimalDetailsScreenMessages(sr.Messages);

                //TODO, ONCE FS changes are ready we do not need below
                sr.Code = "SVAN";
                sr.DisplayDescription = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "TravelNeeds_TaskTrainedDog_Screen_Title_MOB");
                sr.RegisterServiceDescription = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "TravelNeeds_TaskTrainedDog_Screen_Title_MOB");
            }
        }


        private void ServiceAnimalDetailsScreenMessages(List<MOBItem> messages)
        {

            messages.Add(new MOBItem
            {
                Id = "ESAN_HDR",
                CurrentValue = "Additional step required before traveling with a service dog. \n <br /> <br /> Please complete the service dog request form located in Trip Details before arriving at the airport.",
            });
            messages.Add(new MOBItem
            {
                Id = "ESAN_FTR",
                CurrentValue = "\n<br /><br />We no longer accept emotional support animals due to new Department of Transportation regulations.\n<br /><br /><a href=\"https://www.united.com/ual/en/US/fly/travel/special-needs/disabilities/assistance-animals.html\">Review our service animal policy</a>",
            });
        }

        private List<MOBItem> GetSpecialMealsMessages(IEnumerable<ReservationFlightSegment> allSegmentsWithMeals, MultiCallResponse flightshoppingReferenceData)
        {
            Func<List<MOBItem>> GetMealUnavailableMsg = () => new List<MOBItem> { new MOBItem { CurrentValue = string.Format(_configuration.GetValue<string>("SSRItinerarySpecialMealsNotAvailableMsg"), "") } };

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
                return new List<MOBItem> { new MOBItem { CurrentValue = string.Format(_configuration.GetValue<string>("SSR_MarketMealRestrictionMessage"), segments) } };
            }

            return null;
        }

        private List<TravelSpecialNeed> GetOfferedMealsForItinerary(IEnumerable<ReservationFlightSegment> allSegmentsWithMeals, MultiCallResponse flightshoppingReferenceData)
        {
            if (allSegmentsWithMeals == null || !allSegmentsWithMeals.Any()
                || flightshoppingReferenceData == null || flightshoppingReferenceData.SpecialMealResponses == null || !flightshoppingReferenceData.SpecialMealResponses.Any()
                || flightshoppingReferenceData.SpecialMealResponses[0].SpecialMeals == null || !flightshoppingReferenceData.SpecialMealResponses[0].SpecialMeals.Any())
                return null;

            Func<IEnumerable<string>, List<MOBItem>> generateMsg = flightSegments =>
            {
                var segments = flightSegments.Count() > 1 ? string.Join(", ", flightSegments.Take(flightSegments.Count() - 1)) + " and " + flightSegments.Last() : flightSegments.First();
                return new List<MOBItem> { new MOBItem { CurrentValue = string.Format(_configuration.GetValue<string>("SSR_MealRestrictionMessage"), segments) } };
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
            if (!_configuration.GetValue<bool>("EnableServiceAnimalEnhancements") ||
                (!IsTaskTrainedServiceDogSupportedAppVersion(appId, appVersion) &&
               !_configuration.GetValue<bool>("ShowServiceAnimalInTravelNeeds")))
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
                                             animalType.Description, animalType.Description.EndsWith("animal", StringComparison.OrdinalIgnoreCase) ? null : !_configuration.GetValue<bool>("DisableTaskServiceAnimalDescriptionFix") ? animalType.Description : "Dog", TravelSpecialNeedType.ServiceAnimalType.ToString());
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

        private async System.Threading.Tasks.Task AddServiceAnimalsMessageSection(TravelSpecialNeeds offersSSR, int appId, string appVersion, Session session, string deviceId)
        {
            if (IsTaskTrainedServiceDogSupportedAppVersion(appId, appVersion))
            {
                if (offersSSR?.ServiceAnimals == null) offersSSR.ServiceAnimals = new List<TravelSpecialNeed>();
                MOBRequest request = new MOBRequest();
                request.Application = new MOBApplication();
                request.Application.Id = appId;
                request.Application.Version = new MOBVersion();
                request.Application.Version.Major = appVersion;
                request.DeviceId = deviceId;
                request.TransactionId = _headers.ContextValues.TransactionId;
                CSLContentMessagesResponse content = new CSLContentMessagesResponse();

                var cmsCacheResponse = await _cachingService.GetCache<string>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID") + "MOBCSLContentMessagesResponse", _headers.ContextValues.TransactionId).ConfigureAwait(false);

                try
                {
                    if (!string.IsNullOrEmpty(cmsCacheResponse))
                        content = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(cmsCacheResponse);
                }

                catch { cmsCacheResponse = null; }

                if (string.IsNullOrEmpty(cmsCacheResponse))
                    content = await _travelerCSL.GetBookingRTICMSContentMessages(request, session);//, LogEntries);

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
                        Messages = new List<MOBItem>
                        {
                            new MOBItem {
                                CurrentValue = emotionalSupportAssistantContent
                            }
                        }
                    });
                }
            }

            else if (_configuration.GetValue<bool>("EnableTravelSpecialNeedInfo")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("TravelSpecialNeedInfo_Supported_AppVestion_Android"), _configuration.GetValue<string>("TravelSpecialNeedInfo_Supported_AppVestion_iOS"))
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
                        Messages = new List<MOBItem>
                        {
                            new MOBItem {
                                CurrentValue = emotionalSupportAssistantContent
                            }
                        }
                    });
                }
            }
        }
        private bool IsTaskTrainedServiceDogSupportedAppVersion(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("TravelSpecialNeedInfo_TaskTrainedServiceDog_Supported_AppVestion_Android"), _configuration.GetValue<string>("TravelSpecialNeedInfo_TaskTrainedServiceDog_Supported_AppVestion_iOS"));
        }
        private List<MOBItem> GetServiceAnimalsMessages(List<TravelSpecialNeed> serviceAnimals)
        {
            if (serviceAnimals != null && serviceAnimals.Any())
                return null;

            return new List<MOBItem> { new MOBItem { CurrentValue = _configuration.GetValue<string>("SSRItineraryServiceAnimalNotAvailableMsg") } };
        }
        public async Task<MultiCallResponse> GetSpecialNeedsReferenceDataFromFlightShopping(Session session, int appId, string appVersion, string deviceId, string languageCode)
        {
            string cslActionName = "MultiCall";

            string jsonRequest = JsonConvert.SerializeObject(GetFlightShoppingMulticallRequest(languageCode));
            //#TakeTokenFromSession
            // string token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration).ConfigureAwait(false);

            var cslCallDurationstopwatch = new Stopwatch();
            cslCallDurationstopwatch.Start();

            var response = await _referencedataService.GetSpecialNeedsInfo<MultiCallResponse>(cslActionName, jsonRequest, session.Token, session.SessionId).ConfigureAwait(false);


            if (cslCallDurationstopwatch.IsRunning)
            {
                cslCallDurationstopwatch.Stop();
            }

            if (response != null)
            {
                if (response == null || response.SpecialRequestResponses == null || response.ServiceAnimalResponses == null || response.SpecialMealResponses == null || response.SpecialRequestResponses == null)
                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

                return response;
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
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
        internal virtual United.Mobile.Model.Common.MOBAlertMessages GetPartnerAirlinesSpecialTravelNeedsMessage(Session session, IEnumerable<ReservationFlightSegment> segments)
        {
            if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && session.CatalogItems != null && session.CatalogItems.Count > 0 &&
                  session.CatalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableNewPartnerAirlines).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableNewPartnerAirlines).ToString())?.CurrentValue == "1"
                 && segments?.Any(s => s.FlightSegment.OperatingAirlineCode != null) == true
                   && _configuration.GetValue<string>("SupportedAirlinesFareComparison").Contains(segments?.FirstOrDefault()?.FlightSegment?.OperatingAirlineCode.ToUpper())
                  )
            {

                United.Mobile.Model.Common.MOBAlertMessages specialNeedsAlertMessages = new United.Mobile.Model.Common.MOBAlertMessages
                {
                    HeaderMessage = _configuration.GetValue<string>("PartnerAirlinesSpecialTravelNeedsHeader"),
                    IsDefaultOption = true,
                    MessageType = MOBFSRAlertMessageType.Caution.ToString(),
                    AlertMessages = new List<MOBSection>
                        {
                            new MOBSection
                            {
                                MessageType = MOBFSRAlertMessageType.Caution.ToString(),
                                Text2 = _configuration.GetValue<string>("PartnerAirlinesSpecialTravelNeedsMessage"),
                                Order = "1"
                            }
                        }
                };
                return specialNeedsAlertMessages;
            }
            return null;
        }

        #endregion

        #region-HelperClass
        public class MOBSHOPUnfinishedBookingComparer : IEqualityComparer<MOBSHOPUnfinishedBooking>
        {
            private bool _compareArrivalDateTime;
            private bool _compareIsElf;

            public MOBSHOPUnfinishedBookingComparer(bool compareArrivalDateTime = false, bool compareIsElf = false)
            {
                _compareIsElf = compareIsElf;
                _compareArrivalDateTime = compareArrivalDateTime;
            }
            public bool Equals(MOBSHOPUnfinishedBooking x, MOBSHOPUnfinishedBooking y)
            {
                if (x == null && y == null)
                {
                    return true;
                }
                else if (x == null || y == null)
                {
                    return false;
                }

                if (x.TravelerTypes == null && y.TravelerTypes == null)
                {
                    if (x.SearchType != y.SearchType
                        || x.NumberOfAdults != y.NumberOfAdults
                        || x.NumberOfSeniors != y.NumberOfSeniors
                        || x.NumberOfChildren2To4 != y.NumberOfChildren2To4
                        || x.NumberOfChildren5To11 != y.NumberOfChildren5To11
                        || x.NumberOfChildren12To17 != y.NumberOfChildren12To17
                        || x.NumberOfInfantOnLap != y.NumberOfInfantOnLap
                        || x.NumberOfInfantWithSeat != y.NumberOfInfantWithSeat
                        || x.CountryCode != y.CountryCode)
                    {
                        return false;
                    }
                }

                if (x.SearchType != y.SearchType)
                    return false;

                if (x.TravelerTypes != null && y.TravelerTypes != null)
                {
                    if (x.TravelerTypes.Where(t => t.Count > 0).ToList().Count != y.TravelerTypes.Where(t => t.Count > 0).ToList().Count
                        || x.TravelerTypes.Where(t => t.Count > 0).ToList().Except(y.TravelerTypes.Where(t => t.Count > 0).ToList(), new MOBTravelerTypeComparer()).Count() != 0
                        || y.TravelerTypes.Where(t => t.Count > 0).ToList().Except(x.TravelerTypes.Where(t => t.Count > 0).ToList(), new MOBTravelerTypeComparer()).Count() != 0
                        || x.CountryCode != y.CountryCode)
                    {
                        return false;
                    }
                }

                if (x.TravelerTypes == null && (x.NumberOfAdults > 0 || x.NumberOfSeniors > 0) && y.TravelerTypes != null)
                {
                    //if (y.TravelerTypes.Any(t => t.TravelerType.ToUpper() != PAXTYPE.Adult.ToString().ToUpper() && t.Count > 0))
                    //    return false;

                    if (x.NumberOfAdults > 0 && (!y.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Adult.ToString()) || x.NumberOfAdults != y.TravelerTypes.Where(t => t.TravelerType == PAXTYPE.Adult.ToString()).First().Count))
                        return false;

                    if (x.NumberOfChildren2To4 > 0 && (!y.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child2To4.ToString()) || x.NumberOfChildren2To4 != y.TravelerTypes.Where(t => t.TravelerType == PAXTYPE.Child2To4.ToString()).First().Count))
                        return false;

                    if (x.NumberOfChildren5To11 > 0 && (!y.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child5To11.ToString()) || x.NumberOfChildren5To11 != y.TravelerTypes.Where(t => t.TravelerType == PAXTYPE.Child5To11.ToString()).First().Count))
                        return false;

                    if (x.NumberOfChildren12To17 > 0 && (!y.TravelerTypes.Any(p => p.TravelerType == PAXTYPE.Child12To14.ToString() || p.TravelerType == PAXTYPE.Child15To17.ToString()) ||
                        x.NumberOfChildren12To17 != y.TravelerTypes.Where(t => t.TravelerType == PAXTYPE.Child12To14.ToString() || t.TravelerType == PAXTYPE.Child15To17.ToString()).Sum(t => t.Count)))
                        return false;

                    if (x.NumberOfInfantWithSeat > 0 && (!y.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.InfantSeat.ToString()) || x.NumberOfInfantWithSeat != y.TravelerTypes.Where(t => t.TravelerType == PAXTYPE.InfantSeat.ToString()).First().Count))
                        return false;

                    if (x.NumberOfInfantOnLap > 0 && (!y.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.InfantLap.ToString()) || x.NumberOfInfantOnLap != y.TravelerTypes.Where(t => t.TravelerType == PAXTYPE.InfantLap.ToString()).First().Count))
                        return false;

                    if (x.NumberOfSeniors > 0 && (!y.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Senior.ToString()) && x.NumberOfSeniors != y.TravelerTypes.Where(t => t.TravelerType == PAXTYPE.Senior.ToString()).First().Count))
                        return false;
                }

                if (x.TravelerTypes != null && y.TravelerTypes == null && (y.NumberOfAdults > 0 || y.NumberOfSeniors > 0))
                {
                    //if (x.TravelerTypes.Any(t => t.TravelerType.ToUpper() != PAXTYPE.Adult.ToString().ToUpper() && t.Count > 0))
                    //    return false;

                    if (y.NumberOfAdults > 0 && (!x.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Adult.ToString()) || y.NumberOfAdults != x.TravelerTypes.Where(t => t.TravelerType == PAXTYPE.Adult.ToString()).First().Count))
                        return false;

                    if (y.NumberOfChildren2To4 > 0 && (!x.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child2To4.ToString()) || y.NumberOfChildren2To4 != x.TravelerTypes.Where(t => t.TravelerType == PAXTYPE.Child2To4.ToString()).First().Count))
                        return false;

                    if (y.NumberOfChildren5To11 > 0 && (!x.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child5To11.ToString()) || y.NumberOfChildren5To11 != x.TravelerTypes.Where(t => t.TravelerType == PAXTYPE.Child5To11.ToString()).First().Count))
                        return false;

                    if (y.NumberOfChildren12To17 > 0 && (!x.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Child12To14.ToString() || t.TravelerType == PAXTYPE.Child15To17.ToString()) || y.NumberOfChildren12To17 != x.TravelerTypes.Where(t => t.TravelerType == PAXTYPE.Child12To14.ToString() || t.TravelerType == PAXTYPE.Child15To17.ToString()).Sum(t => t.Count)))
                        return false;

                    if (y.NumberOfInfantWithSeat > 0 && (!x.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.InfantSeat.ToString()) || y.NumberOfInfantWithSeat != x.TravelerTypes.Where(t => t.TravelerType == PAXTYPE.InfantSeat.ToString()).First().Count))
                        return false;

                    if (y.NumberOfInfantOnLap > 0 && (!x.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.InfantLap.ToString()) || y.NumberOfInfantOnLap != x.TravelerTypes.Where(t => t.TravelerType == PAXTYPE.InfantLap.ToString()).First().Count))
                        return false;

                    if (y.NumberOfSeniors > 0 && (!x.TravelerTypes.Any(t => t.TravelerType == PAXTYPE.Senior.ToString()) || y.NumberOfSeniors != x.TravelerTypes.Where(t => t.TravelerType == PAXTYPE.Senior.ToString()).First().Count))
                        return false;
                }

                if (_compareIsElf && (x.IsELF != y.IsELF))
                {
                    return false;
                }

                if (x.Trips == null && y.Trips == null)
                    return true;

                if (x.Trips == null)
                    return !y.Trips.Any();

                if (y.Trips == null)
                    return !x.Trips.Any();

                //if (_configuration.GetValue<bool>("GetUnfinishedBookingsArgumentOutOfRangeExceptionFix"))
                //{
                //    if (x.Trips.Count() != y.Trips.Count())
                //        return false;
                //}
                //else
                //{
                //    if (x.Trips.Count() != x.Trips.Count)
                //        return false;
                //}

                for (int i = 0; i < x.Trips.Count; i++)
                {
                    var xtrip = x.Trips[i];
                    var ytrip = y.Trips[i];
                    if (xtrip == null && ytrip == null)
                        return true;

                    if (xtrip.Destination != ytrip.Destination
                        || xtrip.Origin != ytrip.Origin
                        || !SameDate(xtrip.DepartDate, ytrip.DepartDate)
                        || !SameTime(xtrip.DepartTime, ytrip.DepartTime)
                        || !SameDate(xtrip.DepartDateTimeGMT, ytrip.DepartDateTimeGMT)
                        || xtrip != null && ytrip == null
                        || xtrip == null && ytrip != null)
                    {
                        return false;
                    }

                    if (_compareArrivalDateTime && (!SameDate(xtrip.ArrivalDate, ytrip.ArrivalDate) || !SameTime(xtrip.ArrivalTime, ytrip.ArrivalTime)))
                    {
                        return false;
                    }

                    if (xtrip.Flights == null && ytrip.Flights == null)
                        return true;

                    if (xtrip.Flights == null)
                        return !ytrip.Flights.Any();

                    if (ytrip.Flights == null)
                        return !xtrip.Flights.Any();

                    if (xtrip.Flights.Count() != ytrip.Flights.Count)
                        return false;

                    for (int j = 0; j < xtrip.Flights.Count(); j++)
                    {
                        if (!AreSameFlights(xtrip.Flights[j], ytrip.Flights[j]))
                            return false;
                    }
                }

                return true;
            }

            private bool AreSameFlights(MOBSHOPUnfinishedBookingFlight f1, MOBSHOPUnfinishedBookingFlight f2)
            {
                if (f1 == null && f2 == null)
                    return true;

                if (f1.BookingCode != f2.BookingCode
                    || !SameDate(f1.DepartDateTime, f2.DepartDateTime)
                    || f1.Origin != f2.Origin
                    || f1.Destination != f2.Destination
                    || f1.FlightNumber != f2.FlightNumber
                    || f1.MarketingCarrier != f2.MarketingCarrier
                    || f1.ProductType != f2.ProductType)
                {
                    return false;
                }

                if (f1.Connections == null && f2.Connections == null)
                    return true;

                if (f1.Connections == null)
                    return !f2.Connections.Any();

                if (f2.Connections == null)
                    return !f1.Connections.Any();

                if (f1.Connections.Count() != f2.Connections.Count)
                    return false;

                for (int i = 0; i < f1.Connections.Count(); i++)
                {
                    if (!AreSameFlights(f1.Connections[i], f2.Connections[i]))
                        return false;
                }

                return true;
            }

            private bool SameDate(string date1, string date2)
            {
                if (string.IsNullOrEmpty(date1) && string.IsNullOrEmpty(date2))
                    return true;

                if (!string.IsNullOrEmpty(date1) && string.IsNullOrEmpty(date2) || string.IsNullOrEmpty(date1) && !string.IsNullOrEmpty(date2))
                    return false;

                if (date1 == date2)
                    return true;

                DateTime d1;
                DateTime d2;
                var d1Parsed = DateTime.TryParse(date1, out d1);
                var d2Parsed = DateTime.TryParse(date2, out d2);


                if (!(d1Parsed && d2Parsed))
                    return false;

                return d1 == d2;
            }

            private bool SameTime(string time1, string time2)
            {
                if (string.IsNullOrEmpty(time1) && string.IsNullOrEmpty(time2))
                    return true;

                if (!string.IsNullOrEmpty(time1) && string.IsNullOrEmpty(time2) || string.IsNullOrEmpty(time1) && !string.IsNullOrEmpty(time2))
                    return false;

                if (time1 == time2)
                    return true;

                DateTime d1;
                DateTime d2;

                var now = DateTime.Now.ToShortDateString();
                var d1Parsed = DateTime.TryParse(now + " " + time1, out d1);
                var d2Parsed = DateTime.TryParse(now + " " + time2, out d2);

                if (!(d1Parsed && d2Parsed))
                    return false;

                return d1 == d2;
            }

            public int GetHashCode(MOBSHOPUnfinishedBooking obj)
            {
                return -32000;
            }
        }
        public class MOBTravelerTypeComparer : IEqualityComparer<MOBTravelerType>
        {
            public bool Equals(MOBTravelerType x, MOBTravelerType y)
            {
                if (Object.ReferenceEquals(x, y)) return true;

                if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                    return false;

                return x.TravelerType.Equals(y.TravelerType) && x.Count == y.Count;
            }

            public int GetHashCode(MOBTravelerType tType)
            {

                if (Object.ReferenceEquals(tType, null)) return 0;

                return tType.Count ^ tType.TravelerType.GetHashCode();
            }
        }

        private async Task<AirportDetailsList> GetAllAiportsList(List<Flight> flights)
        {
            string airPortCodes = GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(flights);
            return await GetAirportNamesListCollection(airPortCodes);
        }

        private async Task<AirportDetailsList> GetAllAiportsList(List<DisplayTrip> displayTrip)
        {
            string airPortCodes = GetAllAirportCodesWithCommaDelimatedFromCSLDisplayTrips(displayTrip);
            return await GetAirportNamesListCollection(airPortCodes);
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

        private static string GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(List<Flight> flights)
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

        private async Task<AirportDetailsList> GetAirportNamesListCollection(string airPortCodes)
        {
            AirportDetailsList retVal = null;
            if (airPortCodes != string.Empty)
            {
                airPortCodes = "'" + airPortCodes + "'";
                airPortCodes = String.Join(",", airPortCodes.Split(',').Distinct());
                airPortCodes = System.Text.RegularExpressions.Regex.Replace(airPortCodes, ",", "','");
                retVal = new AirportDetailsList();
                retVal.AirportsList = await _shoppingUtility.GetAirportNamesList(airPortCodes);
            }
            return retVal;
        }
        public string GetRedEyeFlightArrDate(String flightDepart, String flightArrive, ref bool flightDateChanged)
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
        public string GetRedEyeDepartureDate(String tripDate, String flightDepartureDate, ref bool flightDateChanged)
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
        private List<string> GetFlightMixedCabinSegments(List<Mobile.Model.Shopping.MOBSHOPFlight> flights, int index)
        {
            //group the mixed cabin messages                
            List<string> tempMsgs = new List<string>();
            foreach (Mobile.Model.Shopping.MOBSHOPFlight flt in flights)
            {
                if (flt.ShoppingProducts != null && flt.ShoppingProducts.Count > index)
                {
                    if (flt.ShoppingProducts[index].MixedCabinSegmentMessages != null && flt.ShoppingProducts[index].MixedCabinSegmentMessages.Count > 0)
                        tempMsgs.AddRange(flt.ShoppingProducts[index].MixedCabinSegmentMessages);
                }
            }

            return tempMsgs;
        }
        private bool GetSelectedCabinInMixedScenario(List<Mobile.Model.Shopping.MOBSHOPFlight> flights, int index)
        {
            //group the mixed cabin messages                
            bool selected = false;
            foreach (Mobile.Model.Shopping.MOBSHOPFlight flt in flights)
            {
                if (flt.ShoppingProducts != null && flt.ShoppingProducts.Count > index)
                {
                    if (flt.ShoppingProducts[index].IsSelectedCabin)
                        selected = flt.ShoppingProducts[index].IsSelectedCabin;
                }
            }

            return selected;
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
                if (_configuration.GetValue<bool>("GetAirportNameInOneCallToggle"))
                {
                    if (airportsList != null && airportsList.AirportsList != null && airportsList.AirportsList.Exists(p => p.AirportCode == airportCode))
                    {
                        var airPort = airportsList.AirportsList.First(p => p.AirportCode == airportCode);
                        airportDesc = airPort.AirportNameMobile;
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
                    airportDesc = await _shoppingUtility.GetAirportName(airportCode);
                }
            }
            catch (Exception ex)
            {
                airportDesc = await _shoppingUtility.GetAirportName(airportCode);
            }
            return airportDesc;
        }

        private List<Mobile.Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage> GetProductDetailMixedCabinSegments(List<Mobile.Model.Shopping.MOBSHOPFlight> flights, int index)
        {
            //group the mixed cabin messages                
            List<Mobile.Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage> tempMsgs = new List<Mobile.Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage>();
            foreach (Mobile.Model.Shopping.MOBSHOPFlight flt in flights)
            {
                if (flt.ShoppingProducts != null && flt.ShoppingProducts.Count > index)
                {
                    if (flt.ShoppingProducts[index].ProductDetail.ProductCabinMessages != null && flt.ShoppingProducts[index].ProductDetail.ProductCabinMessages.Count > 0)
                        tempMsgs.AddRange(flt.ShoppingProducts[index].ProductDetail.ProductCabinMessages);
                }
            }
            return tempMsgs;
        }

        private List<ShopBundleEplus> GetTravelOptionEplusAncillary(SubitemsCollection subitemsCollection, List<ShopBundleEplus> bundlecode)
        {
            if (bundlecode == null || bundlecode.Count == 0)
            {
                bundlecode = new List<ShopBundleEplus>();
            }

            foreach (var item in subitemsCollection)
            {
                if (item?.Type?.Trim().ToUpper() == "EFS")
                {
                    ShopBundleEplus objeplus = new ShopBundleEplus();
                    objeplus.ProductKey = item.Type;
                    objeplus.AssociatedTripIndex = Convert.ToInt32(item.TripIndex);
                    bundlecode.Add(objeplus);
                }
            }

            return bundlecode;
        }
        private List<Mobile.Model.Shopping.MOBSHOPShoppingProduct> PopulateColumns(ColumnInformation columnInfo)
        {
            List<Mobile.Model.Shopping.MOBSHOPShoppingProduct> columns = null;

            //if we have columns...
            if (columnInfo != null && columnInfo.Columns != null && columnInfo.Columns.Count > 0)
            {
                columns = new List<Mobile.Model.Shopping.MOBSHOPShoppingProduct>();
                foreach (Column column in columnInfo.Columns)
                {
                    Mobile.Model.Shopping.MOBSHOPShoppingProduct product = new Mobile.Model.Shopping.MOBSHOPShoppingProduct();
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

        private async Task<UpdateAmenitiesIndicatorsResponse> GetAmenitiesForFlights(string sessionId, string cartId, List<Flight> flights, int appId, string deviceId, string appVersion, bool isClientCall = false, UpdateAmenitiesIndicatorsRequest amenitiesPersistRequest = null)
        {
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName });
            if (session == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }

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
            var returnValue = await _flightShoppingService.UpdateAmenitiesIndicators<UpdateAmenitiesIndicatorsResponse>(session.Token, sessionId, jsonRequest).ConfigureAwait(false);
            response = returnValue.response;
            string cslCallTime = (returnValue.callDuration / (double)1000).ToString();
            //we do not want to throw an errors and stop bookings if this fails
            if (response != null && (response.Errors == null || response.Errors.Count < 1) && response.Profiles != null && response.Profiles.Count > 0)
            {
            }
            else
            {
                if (response?.Errors?.Count > 0)
                {
                    string errorMessage = string.Empty;
                    foreach (var error in response.Errors)
                    {
                        errorMessage = errorMessage + " " + error.Message;
                    }
                    _logger.LogError("GetAmenitiesForFlight - {@Response} for GetAmenitiesForFlight {@Error}", JsonConvert.SerializeObject(response), errorMessage);
                }
                else
                {
                    _logger.LogError("GetAmenitiesForFlight - {@Response} for GetAmenitiesForFlight error", JsonConvert.SerializeObject(response));
                }
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

        private void GetTravelOptionAncillaryDescription(SubitemsCollection subitemsCollection, Mobile.Model.Shopping.TravelOption travelOption, DisplayCart displayCart)
        {
            List<AncillaryDescriptionItem> ancillaryDesciptionItems = new List<AncillaryDescriptionItem>();
            CultureInfo ci = null;

            if (subitemsCollection.Any(t => t?.Type?.Trim().ToUpper() == "EFS"))
            {
                var trips = subitemsCollection.GroupBy(x => x.TripIndex);
                foreach (var trip in trips)
                {
                    if (trip != null)
                    {
                        decimal ancillaryAmount = 0;
                        foreach (var item in trip)
                        {
                            ancillaryAmount += item.Amount;
                            if (ci == null)
                            {
                                ci = TopHelper.GetCultureInfo(item.Currency);
                            }
                        }

                        AncillaryDescriptionItem objeplus = new AncillaryDescriptionItem();
                        objeplus.DisplayValue = TopHelper.FormatAmountForDisplay(ancillaryAmount, ci, false);
                        objeplus.SubTitle = displayCart.DisplayTravelers?.Count.ToString() + (displayCart.DisplayTravelers?.Count > 1 ? " travelers" : " traveler");
                        var displayTrip = displayCart.DisplayTrips?.FirstOrDefault(s => s.Index == Convert.ToInt32(trip.FirstOrDefault().TripIndex));
                        if (displayTrip != null)
                        {
                            objeplus.Title = displayTrip.Origin + " - " + displayTrip.Destination;
                        }
                        ancillaryDesciptionItems.Add(objeplus);
                    }
                }

                travelOption.BundleOfferTitle = "Economy Plus®";
                travelOption.BundleOfferSubtitle = "Included with your fare";
                travelOption.AncillaryDescriptionItems = ancillaryDesciptionItems;
            }
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

        private async Task<List<United.Services.FlightShopping.Common.LMX.LmxFlight>> GetLmxFlights(string token, string cartId, string hashList, string sessionId, int applicationId, string appVersion, string deviceId)
        {
            List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights = null;

            if (!string.IsNullOrEmpty(cartId))
            {
                var returnValue = await _flightShoppingService.GetLmxQuote<LmxQuoteResponse>(token, sessionId, cartId, hashList).ConfigureAwait(false);
                var lmxQuoteResponse = returnValue.response;
                string cslCallTime = (returnValue.callDuration / (double)1000).ToString();

                FlightStatus flightStatus = new FlightStatus();

                if (lmxQuoteResponse != null && lmxQuoteResponse.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success))
                {
                    if (lmxQuoteResponse.Flights != null && lmxQuoteResponse.Flights.Count > 0)
                    {
                        lmxFlights = lmxQuoteResponse.Flights;
                    }
                }
            }
            return lmxFlights;
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

        private async Task<(List<Mobile.Model.Shopping.MOBSHOPFlight> flights, CultureInfo ci)> GetFlights(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string sessionId, string cartId, List<Flight> segments, string requestedCabin, decimal lowestFare, List<Mobile.Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClass, bool updateAmenities = false, bool isConnection = false, bool isELFFareDisplayAtFSR = true, MOBSHOPTrip trip = null, string appVersion = "", int appID = 0, Session session = null, MOBCarbonEmissionsResponse carbonEmissionData = null)
        {
            #region //**// LMX Flag For AppID change
            CultureInfo ci;
            bool supressLMX = false;
            session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);

            supressLMX = session.SupressLMXForAppID;
            #endregion
            try
            {
                if (_configuration.GetValue<bool>("GetAirportNameInOneCallToggle") && (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0))
                {
                    airportsList = await _sessionHelperService.GetSession<AirportDetailsList>(sessionId, new AirportDetailsList().ObjectName, new List<string> { sessionId, new AirportDetailsList().ObjectName }).ConfigureAwait(false);

                    if (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0)
                    {
                        airportsList = await GetAllAiportsList(segments);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllAiportsList {@GetFlights-GetAllAiportsList}", JsonConvert.SerializeObject(ex));
                _logger.LogError("GetAllAiportsList {@PopulateMetaTrips-Trip}", JsonConvert.SerializeObject(segments));

            }

            List<Mobile.Model.Shopping.MOBSHOPFlight> flights = null;
            ci = null;
            if (segments != null && segments.Count > 0)
            {
                flights = new List<Mobile.Model.Shopping.MOBSHOPFlight>();

                foreach (Flight segment in segments)
                {
                    #region

                    Mobile.Model.Shopping.MOBSHOPFlight flight = new Mobile.Model.Shopping.MOBSHOPFlight();
                    flight.Messages = new List<Mobile.Model.Shopping.MOBSHOPMessage>();
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
                        flight.ProductId = segment.BBXCellId ?? string.Empty;
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
                                flight.IsAwardSaver = displayProduct?.AwardType?.Trim().ToLower().Contains("saver") ?? false && !displayProduct.ProductType.ToUpper().Contains("ECONOMY") ? true : false;
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
                                //flight.YqyrMessage = _configuration.GetValue<string>("MP2015YQYRMessage");

                                foreach (LmxLoyaltyTier tier in displayProduct.LmxLoyaltyTiers)
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
                                                foreach (LmxQuote quote in tier.LmxQuotes)
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
                                intSeatsRemainingLimit = _configuration.GetValue<int>("mWebSeatsRemainingLimit");
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
                        foreach (Amenity amenity in segment.Amenities)
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
                        var tupleRes= await GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, segment.Connections, requestedCabin, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR, trip, appVersion, appID, session);
                        flight.Connections = tupleRes.flights;
                        ci = tupleRes.ci;
                    }

                    flight.ConnectTimeMinutes = segment.ConnectTimeMinutes > 0 ? GetFormattedTravelTime(segment.ConnectTimeMinutes) : string.Empty;

                    flight.DepartDate = GeneralHelper.FormatDate(segment.DepartDateTime);
                    flight.DepartTime = FormatTime(segment.DepartDateTime);
                    flight.Destination = segment.Destination;
                    flight.DestinationDate = GeneralHelper.FormatDate(segment.DestinationDateTime);
                    flight.DestinationTime = FormatTime(segment.DestinationDateTime);
                    flight.DepartureDateTime = GeneralHelper.FormatDateTime(segment.DepartDateTime);
                    flight.ArrivalDateTime = GeneralHelper.FormatDateTime(segment.DestinationDateTime);

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
                    flight.InternationalCity = segment.InternationalCity ?? string.Empty;
                    flight.IsConnection = segment.IsConnection;
                    flight.MarketingCarrier = segment.MarketingCarrier;
                    flight.MarketingCarrierDescription = segment.MarketingCarrierDescription;
                    flight.Miles = segment.MileageActual.ToString();

                    if (_configuration.GetValue<bool>("ReturnShopSelectTripOnTimePerformance"))
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
                        flight.StopInfos = new List<Mobile.Model.Shopping.MOBSHOPFlight>();
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
                                    var tupleRes= await GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, stops, requestedCabin, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR, trip, appVersion, appID, session);
                                    List<Mobile.Model.Shopping.MOBSHOPFlight> stopFlights = tupleRes.flights;
                                    ci = tupleRes.ci;

                                    foreach (Mobile.Model.Shopping.MOBSHOPFlight sf in stopFlights)
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
                                    flight.ArrivalDateTime = GeneralHelper.FormatDateTime(Convert.ToDateTime(stop.DepartDateTime).AddMinutes(-stop.GroundTimeMinutes).ToString());
                                    flight.TravelTime = segment.TravelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes > 0 ? GetFormattedTravelTime(segment.TravelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes) : string.Empty;

                                    Mobile.Model.Shopping.MOBSHOPFlight stopFlight = new Mobile.Model.Shopping.MOBSHOPFlight();

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
                                    stopFlight.DepartureDateTime = GeneralHelper.FormatDateTime(stop.DepartDateTime);
                                    stopFlight.ArrivalDateTime = GeneralHelper.FormatDateTime(stop.DestinationDateTime);
                                    stopFlight.IsCovidTestFlight = stop.IsCovidTestingRequired;
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
                            flight.DestinationDate = GeneralHelper.FormatDate(Convert.ToDateTime(segment.StopInfos[0].DepartDateTime).AddMinutes(-segment.StopInfos[0].GroundTimeMinutes).ToString());
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
                    flight.TravelTimeInMinutes = segment.TravelMinutes;
                    flight.TripId = segment.BBXSolutionSetId;

                    if (_mOBSHOPDataCarrier == null)
                        _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                    var tupleResponse = await PopulateProducts(_mOBSHOPDataCarrier, segment.Products, sessionId, flight, requestedCabin, segment, lowestFare, columnInfo, premierStatusLevel, fareClass, isELFFareDisplayAtFSR, appVersion, session);
                    flight.ShoppingProducts = tupleResponse.Item1;
                    flight = tupleResponse.flight;


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
                        if (_configuration.GetValue<bool>("HandlePagingAtRESTSide") && flights.Count == _configuration.GetValue<int>("OrgarnizeResultsRequestPageSize"))
                        {
                            break;
                        }
                        #endregion
                    }
                }
            }

            return (flights, ci);
        }
        public string FormatTime(string dateTimeString)
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
        private Mobile.Model.Shopping.SHOPOnTimePerformance PopulateOnTimePerformanceSHOP(Service.Presentation.ReferenceDataModel.DOTAirlinePerformance onTimePerformance)
        {

            Mobile.Model.Shopping.SHOPOnTimePerformance shopOnTimePerformance = null;
            if (_configuration.GetValue<bool>("ReturnOnTimePerformance"))
            {
                #region
                if (onTimePerformance != null)
                {
                    shopOnTimePerformance = new Mobile.Model.Shopping.SHOPOnTimePerformance();
                    shopOnTimePerformance.Source = onTimePerformance.Source;
                    shopOnTimePerformance.DOTMessages = new Mobile.Model.Shopping.SHOPOnTimeDOTMessages();
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
                    shopOnTimePerformance = new Mobile.Model.Shopping.SHOPOnTimePerformance();
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

        private async Task<(List<Mobile.Model.Shopping.MOBSHOPShoppingProduct>, Mobile.Model.Shopping.MOBSHOPFlight flight)> PopulateProducts(MOBSHOPDataCarrier _mOBSHOPDataCarrier, ProductCollection products, string sessionId, Mobile.Model.Shopping.MOBSHOPFlight flight, string cabin, Flight segment, decimal lowestAirfare, List<Mobile.Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClas, bool isELFFareDisplayAtFSR = true, string appVersion = "", Session session = null)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);
            supressLMX = session.SupressLMXForAppID;
            #endregion
            if (_mOBSHOPDataCarrier == null)
                _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
            return (PopulateProducts(_mOBSHOPDataCarrier, products, cabin, segment, lowestAirfare, columnInfo, premierStatusLevel, fareClas, supressLMX, session, isELFFareDisplayAtFSR, appVersion),flight);
        }


        private List<Mobile.Model.Shopping.MOBSHOPShoppingProduct> PopulateProducts(MOBSHOPDataCarrier _mOBSHOPDataCarrier, ProductCollection products, string cabin, Flight segment, decimal lowestAirfare,
          List<Mobile.Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClas, bool supressLMX, Session session, bool isELFFareDisplayAtFSR = true, string appVersion = "")
        {
            var shopProds = new List<Mobile.Model.Shopping.MOBSHOPShoppingProduct>();
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
                            ref foundEconomyAward, ref foundBusinessAward, ref foundFirstAward, session, isELFFareDisplayAtFSR, appVersion);

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
                    var configurationProductSettings = _configuration.GetSection("productSettings").Get<ProductSection>() as ProductSection;

                    SetShortCabin(shopProd, columnInfo, configurationProductSettings);

                    if (string.IsNullOrEmpty(shopProd.Description))
                    {
                        SetShoppingProductDescriptionBasedOnProductElementDescription(shopProd, columnInfo, configurationProductSettings);
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

            List<Mobile.Model.Shopping.MOBSHOPShoppingProduct> updatedShopProducts = new List<Mobile.Model.Shopping.MOBSHOPShoppingProduct>();
            foreach (Mobile.Model.Shopping.MOBSHOPShoppingProduct mobShopProduct in shopProds)
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

        private void CalculateAwardCounts(List<Mobile.Model.Shopping.MOBSHOPShoppingProduct> shopProds, ref int econAwardCount, ref int busAwardCount, ref int firstAwardCount)
        {
            foreach (Mobile.Model.Shopping.MOBSHOPShoppingProduct prod in shopProds)
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

        private void HighlightProductMatchingSelectedCabin(List<Mobile.Model.Shopping.MOBSHOPShoppingProduct> shopProds, string requestedCabin, List<Mobile.Model.Shopping.MOBSHOPShoppingProduct> columnInfo, string fareClass, bool isReshopChange, bool isELFFareDisplayAtFSR = true)
        {
            IOrderedEnumerable<Mobile.Model.Shopping.MOBSHOPShoppingProduct> productsSortedByPrice = null;
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

        public static bool MatchServiceClassRequested(string requestedCabin, string fareClass, string prodType, List<Mobile.Model.Shopping.MOBSHOPShoppingProduct> columnInfo, bool isELFFareDisplayAtFSR = true)
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


        private MOBSHOPShoppingProduct TransformProductWithPriceToNewProduct
            (string cabin, Flight segment, decimal lowestAirfare,
            List<MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, bool isUADiscount, Product prod, bool supressLMX, CultureInfo ci,
            string fareClass, int productIndex, ref bool foundCabinSelected, ref bool foundEconomyAward,
            ref bool foundBusinessAward, ref bool foundFirstAward, Session session, bool isELFFareDisplayAtFSR, string appVersion = "", string corporateFareIndicator = "", string yaDiscount = "")
        {
            MOBSHOPShoppingProduct newProd = new MOBSHOPShoppingProduct();
            newProd.ProductDetail = new MOBSHOPShoppingProductDetail();
            newProd.ProductDetail.ProductCabinMessages = new List<Mobile.Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage>();

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


            if (_shoppingUtility.IsFSROAFlashSaleEnabled(session?.CatalogItems) && prod.PenaltyBoxIndicator == true) // if penaltybox indicator is trye not set the product id which makes the product non-clickbale in UI
            {
                newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, NoLongerAvailableBadge());
            }
            else
            {
                newProd.ProductId = prod.ProductId;
            }

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

            if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes"))
            {
                newProd.CabinDescription = prod.Description;
                newProd.BookingCode = prod.BookingCode;
            }
            return newProd;
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
        private Mobile.Model.Shopping.MOBSHOPShoppingProduct TransformProductWithoutPriceToNewProduct(string cabin, List<MOBSHOPShoppingProduct> columnInfo, bool isUADiscount,
            Product prod, bool foundEconomyAward, bool foundBusinessAward, bool foundFirstAward, Session session)
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
                newProd.ProductCode = _configuration.GetValue<string>("ELFProductCode"); ;
            }
            if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes"))
            {
                newProd.CabinDescription = prod.Description;
                newProd.BookingCode = prod.BookingCode;
            }
            return newProd;
        }

        private Mobile.Model.Shopping.MOBStyledText MixedCabinBadge()
        {
            return new Mobile.Model.Shopping.MOBStyledText()
            {
                Text = _configuration.GetValue<string>("MixedCabinProductBadgeText"),
                SortPriority = MOBFlightProductBadgeSortOrder.MixedCabin.ToString()
            };
        }

        private MOBSHOPFlight ReShopAirfareCreditDisplayFSR(CultureInfo ci, Product product, MOBSHOPFlight flight)
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

                    flight.ReshopCreditColor = CreditTypeColor.GREEN;

                    //displayPrice = string.Concat("+", displayPrice);
                    //AirfareDisplayValue
                    if (product.CreditType == CreditTypes.Refund)
                    {
                        flight.ReshopFees = CreditType.REFUND.GetDisplayName();
                        flight.IsReshopCredit = true;
                    }
                    else if (product.CreditType == CreditTypes.FlightCredit)
                    {
                        flight.ReshopFees = CreditType.FLIGHTCREDIT.GetDisplayName();
                        flight.IsReshopCredit = true;
                    }

                    flight.AirfareDisplayValue = strDisplayCredit;
                }
            }
            return flight;
        }

        private void SetSegmentInfoMessages(Mobile.Model.Shopping.MOBSHOPFlight flight, Warning warn)
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

        private SegmentInfoAlerts TerminalChangeSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                SortOrder = SegmentInfoAlertsOrder.TerminalChange.ToString()
            };
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
                //_configuration.GetValue<string>("NextDayArrivalSegmentText"),
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                SortOrder = SegmentInfoAlertsOrder.ArrivesNextDay.ToString()
            };
        }

        private void SetIsPremierCabinSaverIfApplicable(Mobile.Model.Shopping.MOBSHOPShoppingProduct mobShopProduct)
        {
            if (mobShopProduct.AwardType.Trim().ToUpper().Contains("SAVER") &&
               !mobShopProduct.LongCabin.Trim().ToUpper().Contains("ECON"))
            {
                mobShopProduct.ISPremierCabinSaver = true;
            }
        }

        private void SetMileageButtonAndAwardFound(string cabin, Product prod, ref bool foundEconomyAward,
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

        private List<Mobile.Model.Shopping.MOBStyledText> SetProductBadgeInformation(List<Mobile.Model.Shopping.MOBStyledText> badges, Mobile.Model.Shopping.MOBStyledText badge)
        {
            if (badges == null)
                badges = new List<Mobile.Model.Shopping.MOBStyledText>();

            badges.Add(badge);

            if (badges.Count > 1)
            {
                badges = badges.OrderBy(x => (int)Enum.Parse(typeof(MOBFlightProductBadgeSortOrder), x.SortPriority)).ToList();
            }

            return badges;
        }

        private void SetProductMixedCabinInformation(Flight segment, Product prod, MOBSHOPShoppingProduct newProd)
        {
            Mobile.Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage detailCabinMessage =
              new Mobile.Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage();
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

        private void SetProductPriceInformation(Product prod, CultureInfo ci, MOBSHOPShoppingProduct newProd, Session session, string appVersion = "")
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
                    if (prod.PenaltyBoxIndicator == false)
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

        private Mobile.Model.Shopping.MOBStyledText NoLongerAvailableBadge()
        {
            return new Mobile.Model.Shopping.MOBStyledText()
            {
                Text = _configuration.GetValue<string>("NoLongerAvailableProductBadgeText"),
                SortPriority = MOBFlightProductBadgeSortOrder.NoLongerAvailable.ToString()
            };
        }

        public string ReShopAirfareDisplayValue(CultureInfo ci, List<PricingItem> price, bool isAward = false, bool isChangeFee = false)
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

                    shoppingProduct.ReshopCreditColor = CreditTypeColor.GREEN;

                    //displayPrice = string.Concat("+", displayPrice);
                    //AirfareDisplayValue
                    if (product.CreditType == CreditTypes.Refund)
                    {
                        shoppingProduct.ReshopFees = CreditType.REFUND.GetDisplayName();
                        shoppingProduct.IsReshopCredit = true;
                    }
                    else if (product.CreditType == CreditTypes.FlightCredit)
                    {
                        shoppingProduct.ReshopFees = CreditType.FLIGHTCREDIT.GetDisplayName();
                        shoppingProduct.IsReshopCredit = true;
                    }

                    shoppingProduct.Price = strDisplayCredit;
                }
            }
            return shoppingProduct;
        }
        private decimal ReshopAirfareCreditDisplayInDecimal(List<PricingItem> price, string priceType)
        {
            decimal retVal = 0;
            if (price.Exists(p => string.Equals(p.PricingType, priceType, StringComparison.OrdinalIgnoreCase)))
                retVal = price.FirstOrDefault(p => string.Equals(p.PricingType, priceType, StringComparison.OrdinalIgnoreCase)).Amount;
            return retVal;
        }

        private PricingItem ReshopAwardPrice(List<PricingItem> price)
        {
            if (price.Exists(p => p.PricingType == "Award"))
                return price.FirstOrDefault(p => p.PricingType == "Award");

            return null;
        }

        private void SetLmxLoyaltyInformation(int premierStatusLevel, Product prod, bool supressLMX, CultureInfo ci,
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

        private void SetProductDetails(Flight segment, List<MOBSHOPShoppingProduct> columnInfo, Product prod, int productIndex,
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

        private string GetDescriptionFromColumn(string productType, List<MOBSHOPShoppingProduct> columnInfo)
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


        private static string GetCabinNameFromColumn(string type, List<MOBSHOPShoppingProduct> columnInfo, string defaultCabin)
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

        private void SetShoppingProductDescriptionBasedOnProductElementDescription(Mobile.Model.Shopping.MOBSHOPShoppingProduct shopProd, List<Mobile.Model.Shopping.MOBSHOPShoppingProduct> columnInfo, IProductSection configurationProductSettings)
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

        private Mobile.Model.Shopping.SHOPEquipmentDisclosure GetEquipmentDisclosures(United.Services.FlightShopping.Common.EquipmentDisclosure equipmentDisclosure)
        {
            Mobile.Model.Shopping.SHOPEquipmentDisclosure bkEquipmentDisclosure = null;
            if (equipmentDisclosure != null)
            {
                bkEquipmentDisclosure = new Mobile.Model.Shopping.SHOPEquipmentDisclosure();
                bkEquipmentDisclosure.EquipmentType = equipmentDisclosure.EquipmentType;
                bkEquipmentDisclosure.EquipmentDescription = equipmentDisclosure.EquipmentDescription;
                bkEquipmentDisclosure.IsSingleCabin = equipmentDisclosure.IsSingleCabin;
                bkEquipmentDisclosure.NoBoardingAssistance = equipmentDisclosure.NoBoardingAssistance;
                bkEquipmentDisclosure.NonJetEquipment = equipmentDisclosure.NonJetEquipment;
                bkEquipmentDisclosure.WheelchairsNotAllowed = equipmentDisclosure.WheelchairsNotAllowed;
            }
            return bkEquipmentDisclosure;
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

        public void AddGrandTotalIfNotExistInPrices(List<MOBSHOPPrice> prices)
        {
            var grandTotalPrice = prices.FirstOrDefault(p => p.DisplayType.ToUpper().Equals("GRAND TOTAL"));
            if (grandTotalPrice == null)
            {
                var totalPrice = prices.FirstOrDefault(p => p.DisplayType.ToUpper().Equals("TOTAL"));
                grandTotalPrice = ShopStaticUtility.BuildGrandTotalPriceForReservation(totalPrice.Value);
                prices.Add(grandTotalPrice);
            }
        }
        public void AddFreeBagDetailsInPrices(DisplayCart displayCart, List<MOBSHOPPrice> prices)
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
        private void ClearMileageButtonAndAllCabinButtonText(List<Mobile.Model.Shopping.MOBSHOPShoppingProduct> shopProds, int econAwardCount, int busAwardCount, int firstAwardCount)
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

        public void AssignCorporateFareIndicator(Flight segment, Mobile.Model.Shopping.MOBSHOPFlight flight, string travelType = "")
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


        public Product GetMatrixDisplayProduct(ProductCollection products, string fareSelected, List<MOBSHOPShoppingProduct> columnInfo, out CultureInfo ci, out bool isSelectedFareFamily, out string serviceClassDesc, out bool isMixedCabin, out int seatsRemaining, string fareClass, bool isConnectionOrStopover = false, bool isELFFareDisplayAtFSR = true)
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
                        else if (_configuration.GetValue<bool>("SwithAwardSelectedCabinMilesDisplay") && !string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("AWARD"))
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
        private bool IsYoungAdultProduct(ProductCollection pc)
        {
            return pc != null && pc.Count > 0 && pc.Any(p => p.ProductSubtype.ToUpper().Equals("YOUNGADULTDISCOUNTEDFARE"));

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

        private bool IsTripPlanSearch(string travelType)
        {
            return _configuration.GetValue<bool>("EnableTripPlannerView") && travelType == MOBTripPlannerType.TPSearch.ToString() || travelType == MOBTripPlannerType.TPEdit.ToString();
        }
        private United.Services.FlightShopping.Common.FlightReservation.RegisterOfferRequest GetRegisterOfferRequest(string cartId, string cartKey, string languageCode, string pointOfSale, string productCode, string productId, List<string> productIds, string subProductCode, bool delete, Service.Presentation.ProductResponseModel.ProductOffer productOffer)
        {
            United.Services.FlightShopping.Common.FlightReservation.RegisterOfferRequest registerOfferRequest = new United.Services.FlightShopping.Common.FlightReservation.RegisterOfferRequest();
            registerOfferRequest.AutoTicket = false;
            registerOfferRequest.CartId = cartId;
            registerOfferRequest.CartKey = cartKey;
            registerOfferRequest.CountryCode = pointOfSale;
            registerOfferRequest.Delete = delete;
            registerOfferRequest.Offer = productOffer;
            registerOfferRequest.LangCode = languageCode;
            registerOfferRequest.ProductCode = productCode;
            registerOfferRequest.ProductId = productId;
            registerOfferRequest.ProductIds = productIds;
            registerOfferRequest.SubProductCode = subProductCode;

            return registerOfferRequest;
        }
        private async Task<FlightReservationResponse> RegisterOffer(string sessionId, string cartId, string cartKey, string languageCode, string pointOfSale, string productCode, string productId, List<string> productIds, string subProductCode, bool delete, int applicationId, string deviceId, string appVersion, bool isReshopChange, Service.Presentation.ProductResponseModel.ProductOffer productOffer, Reservation reservation, FlightReservationResponse response)
        {
            List<Mobile.Model.Shopping.TravelOption> travelOptions = null;
            string logAction = delete ? "UnRegisterOffer" : "RegisterOffer";

            United.Services.FlightShopping.Common.FlightReservation.RegisterOfferRequest registerOfferRequest = GetRegisterOfferRequest(cartId, cartKey, languageCode, pointOfSale, productCode, productId, productIds, subProductCode, delete, productOffer);
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
                string action = "RegisterOffer";
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******
                response = await _shoppingCartService.RegisterOffers<FlightReservationResponse>(session.Token, action, jsonRequest, sessionId).ConfigureAwait(false);
                #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******            
                if (response != null)
                {
                    if (response != null && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success) && response.DisplayCart != null)
                    {
                        if (productCode != "TPI" && productCode != "PBS")
                        {
                            travelOptions = GetTravelOptions(response.DisplayCart, isReshopChange, applicationId, appVersion);
                            if (_omniCart.IsEnableOmniCartMVP2Changes(applicationId, appVersion, true))
                            {
                                var mobRequest = new MOBRequest
                                {
                                    Application = new MOBApplication
                                    {
                                        Id = applicationId,
                                        Version = new MOBVersion
                                        {
                                            Major = appVersion
                                        }
                                    },
                                    DeviceId = deviceId
                                };
                                await _omniCart.BuildShoppingCart(mobRequest, response, FlowType.BOOKING.ToString(), cartId, sessionId);
                            }
                            reservation.TravelOptions = travelOptions;
                            await _sessionHelperService.SaveSession<Reservation>(reservation, session.SessionId, new List<string> { session.SessionId, reservation.ObjectName }, reservation.ObjectName).ConfigureAwait(false);
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

            return response;
        }
        #endregion
    }
}
