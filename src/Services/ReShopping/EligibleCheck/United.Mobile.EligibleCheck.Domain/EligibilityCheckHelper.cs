using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Web;
using United.Common.Helper;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.ReShop;
using United.Mobile.Model.Shopping;
using United.Service.Presentation.CommonModel;
using United.Service.Presentation.ReservationResponseModel;
using United.Services.FlightShopping.Common.Extensions;
using United.Utility.Helper;
using Characteristic = United.Service.Presentation.CommonModel.Characteristic;
using Genre = United.Service.Presentation.CommonModel.Genre;

namespace United.Mobile.EligibleCheck.Domain
{
    public class EligibilityCheckHelper
    {
        private static IConfiguration _configuration { get; set; }
        public EligibilityCheckHelper(IConfiguration configuration)
        {
            EligibilityCheckHelper._configuration = configuration;
        }

        public static void UtilityInitialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static bool CheckCheckinEligible(MOBRESHOPChangeEligibilityRequest request)
        {
            return request.PaxIndexes.IsNullOrEmpty();
        }

        public static bool IsOTFOfferEligible(MOBRESHOPChangeEligibilityResponse response)
        {
            if (!_configuration.GetValue<bool>("EnableFFCOTFRedirectToShopping")) return false;
            if (response?.OnTheFlyEligibility?.OfferEligible == null) return false;
            return response.OnTheFlyEligibility.OfferEligible;
        }

        public static string GetSDCRedirect3dot0Url(MOBRESHOPChangeEligibilityRequest request,
            MOBRESHOPChangeEligibilityResponse response = null)
        {
            bool isAward = false;
            string trips = string.Empty;
            string travelers = string.Empty;
            string ddate = string.Empty;
            int idx = 0;

            if (request.ReshopRequest != null)
            {
                isAward = request.ReshopRequest.AwardTravel;

                if (!request.PaxIndexes.IsNullOrEmpty() && request.PaxIndexes.Any())
                {
                    travelers = string.Join(",", request.PaxIndexes.Select(x => x));
                }
                else if (response.PnrTravelers != null && response.PnrTravelers.Any())
                {
                    travelers = string.Join(",", response.PnrTravelers.Select(x => x.SHARESPosition));
                }

                if (request.ReshopRequest.Trips != null
                   && request.ReshopRequest.Trips.Any())
                {
                    //trips = string.Join(",", request.ReshopRequest.Trips.Where
                    //    (y => y.ChangeType == MOBSHOPTripChangeType.ChangeFlight)?.Select(x => x.Index));

                    var selectedtrip = request.ReshopRequest.Trips.Where
                        (y => y.ChangeType == MOBSHOPTripChangeType.ChangeFlight)?.FirstOrDefault();

                    if (selectedtrip != null)
                    {
                        trips = selectedtrip.Index.ToString();

                        if (DateTime.TryParse(selectedtrip.DepartDate, out DateTime tempddate))
                        {
                            ddate = tempddate.ToString("MM-dd-yyyy");
                        }
                    }
                }


            }
            return GetTripDetailRedirect3dot0Url(request.RecordLocator, request.LastName, "CSDC",
                 channel: "mobile", languagecode: "en/US", trips: trips, travelers: travelers,
                 ddate: ddate, guid: request.CheckinSessionKey, isAward: isAward);
        }

        public static string GetTripDetailRedirect3dot0Url
            (string cn, string ln, string ac, int timestampvalidity = 0, string channel = "mobile",
            string languagecode = "en/US", string trips = "", string travelers = "", string ddate = "",
            string guid = "", bool isAward = false)
        {
            new EligibilityCheckHelper(_configuration);
            var retUrl = string.Empty;
            //REF:{0}/{1}/manageres/tripdetails/{2}/{3}?{4}
            //{env}/{en/US}/manageres/tripdetails/{encryptedStuff}/mobile?changepath=true
            var baseUrl = _configuration.GetValue<string>("TripDetailRedirect3dot0BaseUrl");

            var urlPattern = _configuration.GetValue<string>("TripDetailRedirect3dot0UrlPattern");
            var urlPatternFSR = _configuration.GetValue<string>("ReshopFSRRedirect3dot0UrlPattern");


            DateTime timestamp
                = (timestampvalidity > 0) ? DateTime.Now.ToUniversalTime().AddMinutes(timestampvalidity) : DateTime.Now.ToUniversalTime();

            var encryptedstring = string.Empty;
            if (_configuration.GetValue<bool>("EnableRedirect3dot0UrlWithSlashRemoved"))
            {
                encryptedstring = EncryptString
                (string.Format("RecordLocator={0};LastName={1};TimeStamp={2};", cn, ln, timestamp)).Replace("/", "~~");
            }
            else
            {
                encryptedstring = EncryptString
                (string.Format("RecordLocator={0};LastName={1};TimeStamp={2};", cn, ln, timestamp));
            }

            var encodedstring = HttpUtility.UrlEncode(encryptedstring);
            string encodedpnr = HttpUtility.UrlEncode(EncryptString(cn));
            string from = "mobilecheckinsdc";

            if (string.Equals(ac, "EX", StringComparison.OrdinalIgnoreCase))
            {
                return string.Format
                    (urlPattern, baseUrl, languagecode, encodedstring, channel, "changepath=true");
            }
            else if (string.Equals(ac, "CA", StringComparison.OrdinalIgnoreCase))
            {
                return string.Format
                    (urlPattern, baseUrl, languagecode, encodedstring, channel, "cancelpath=true");
            }
            else if (string.Equals(ac, "CSDC", StringComparison.OrdinalIgnoreCase))
            {
                //&td1=01-29-2021&idx=1
                string inputdatapattern = "pnr={0}&trips={1}&travelers={2}&from={3}&guid={4}&td1={5}{6}";
                return string.Format(urlPatternFSR, baseUrl, languagecode, isAward ? "awd" : "rev",
                    string.Format(inputdatapattern, encodedpnr, trips, travelers, from, guid,
                    ddate, isAward ? string.Empty : "&TYPE=rev"));
            }
            else
            {
                return string.Format
                    (urlPattern, baseUrl, languagecode, encodedstring, channel, string.Empty).TrimEnd('?');
            }
        }

        public static string EncryptString(string data)
        {
            return United.ECommerce.Framework.Utilities.SecureData.EncryptString(data);
        }

        public static string GetHighestCabin
            (IEnumerable<United.Service.Presentation.SegmentModel.ReservationFlightSegment> totalTripSegments, bool isAward)
        {
            try
            {
                bool isFirst = totalTripSegments.Any
                    (x => x.BookingClass?.Cabin?.Name?.IndexOf("first", StringComparison.OrdinalIgnoreCase) > 0);
                bool isBusiness = totalTripSegments.Any
                    (x => x.BookingClass?.Cabin?.Name?.IndexOf("business", StringComparison.OrdinalIgnoreCase) > 0);

                return (isFirst) ? ((isAward) ? "awardFirst" : "first")
                    : (isBusiness) ? ((isAward) ? "awardBusinessFirst" : "businessFirst") : ((isAward) ? "awardEcon" : "econ");
            }
            catch { return string.Empty; }
        }


        public static void MapPersistedReservationToMOBSHOPReservation(MOBSHOPReservation reservation,
            United.Mobile.Model.Shopping.Reservation persistedReservation)
        {
            reservation.TravelersCSL = new List<MOBCPTraveler>();

            if (persistedReservation == null) return;

            if (persistedReservation?.TravelerKeys != null && persistedReservation.TravelerKeys.Any())
            {
                foreach (var travelerKey in persistedReservation.TravelerKeys)
                {
                    var traveler = persistedReservation.TravelersCSL[travelerKey];
                    reservation.TravelersCSL.Add(traveler);
                }
            }
            reservation.NumberOfTravelers = persistedReservation.NumberOfTravelers;
            reservation.IsSignedInWithMP = persistedReservation.IsSignedInWithMP;
            reservation.SessionId = persistedReservation.SessionId;
            reservation.SearchType = persistedReservation.SearchType;
            reservation.TravelersRegistered = persistedReservation.TravelersRegistered;
            reservation.Trips = persistedReservation.Trips;
            reservation.Prices = persistedReservation.Prices;
            reservation.CartId = persistedReservation.CartId;
            reservation.CreditCards = persistedReservation.CreditCards.Clone();
            reservation.ReservationEmail = persistedReservation.ReservationEmail.Clone();
            reservation.ReservationPhone = persistedReservation.ReservationPhone.Clone();
            reservation.CreditCardsAddress = persistedReservation.CreditCardsAddress.Clone();
            reservation.PointOfSale = persistedReservation.PointOfSale;
            reservation.ClubPassPurchaseRequest = persistedReservation.ClubPassPurchaseRequest;
            reservation.FareLock = persistedReservation.FareLock;
            reservation.FareRules = persistedReservation.FareRules;
            reservation.SeatPrices = persistedReservation.SeatPrices;
            reservation.Taxes = persistedReservation.Taxes;
            reservation.UnregisterFareLock = persistedReservation.UnregisterFareLock;
            reservation.AwardTravel = persistedReservation.AwardTravel;
            reservation.FlightShareMessage = persistedReservation.FlightShareMessage;
            reservation.PKDispenserPublicKey = persistedReservation.PKDispenserPublicKey;
            reservation.TCDAdvisoryMessages = persistedReservation.TCDAdvisoryMessages;
            reservation.IsRefundable = persistedReservation.IsRefundable;
            reservation.ISInternational = persistedReservation.ISInternational;
            reservation.ISFlexibleSegmentExist = persistedReservation.ISFlexibleSegmentExist;
            reservation.SeatAssignmentMessage = persistedReservation.SeatAssignmentMessage;
            reservation.IsELF = persistedReservation.IsELF;
            reservation.IsSSA = persistedReservation.IsSSA;
            if (persistedReservation.TravelOptions != null && persistedReservation.TravelOptions.Count > 0)
            {
                reservation.TravelOptions = persistedReservation.TravelOptions;
            }
            //FOP Options Fix Venkat 12/08
            reservation.FormOfPaymentType = persistedReservation.FormOfPaymentType;
            if (persistedReservation.FormOfPaymentType == MOBFormofPayment.PayPal || persistedReservation.FormOfPaymentType == MOBFormofPayment.PayPalCredit)
            {
                if (persistedReservation.PayPal != null)
                {
                    reservation.PayPal = persistedReservation.PayPal;
                }
                if (persistedReservation.PayPalPayor != null)
                {
                    reservation.PayPalPayor = persistedReservation.PayPalPayor;
                }
            }
            if (persistedReservation.FormOfPaymentType == MOBFormofPayment.Masterpass)
            {
                if (persistedReservation.MasterpassSessionDetails != null)
                    reservation.MasterpassSessionDetails = persistedReservation.MasterpassSessionDetails;
                if (persistedReservation.Masterpass != null)
                    reservation.Masterpass = persistedReservation.Masterpass;
            }
            //FOP Options Fix Venkat 12/08
            if (persistedReservation.FOPOptions != null && persistedReservation.FOPOptions.Count > 0)
            {
                reservation.FOPOptions = persistedReservation.FOPOptions;
            }

            if (persistedReservation.IsReshopChange)
            {
                reservation.ReshopTrips = persistedReservation.ReshopTrips;
                reservation.IsReshopChange = true;
                reservation.Reshop = persistedReservation.Reshop;
            }
            if (persistedReservation.ShopReservationInfo != null)
            {
                reservation.ShopReservationInfo = persistedReservation.ShopReservationInfo;
            }
            if (persistedReservation.ShopReservationInfo2 != null)
            {
                reservation.ShopReservationInfo2 = persistedReservation.ShopReservationInfo2;
            }
            if (persistedReservation.TripInsuranceFile != null && persistedReservation.TripInsuranceFile.TripInsuranceBookingInfo != null)
            {
                reservation.TripInsuranceInfoBookingPath = persistedReservation.TripInsuranceFile.TripInsuranceBookingInfo;
            }
            else
            {
                reservation.TripInsuranceInfoBookingPath = null;
            }

            reservation.AlertMessages = persistedReservation.AlertMessages;
            reservation.IsRedirectToSecondaryPayment = persistedReservation.IsRedirectToSecondaryPayment;
            reservation.RecordLocator = persistedReservation.RecordLocator;
            reservation.Messages = persistedReservation.Messages;
            reservation.CheckedbagChargebutton = persistedReservation.CheckedbagChargebutton;
            reservation.IsPostBookingCommonFOPEnabled = persistedReservation.IsPostBookingCommonFOPEnabled;
        }

        public static bool EnableChangeWebSSO(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreater(appId, appVersion, _configuration.GetValue<string>("AndroidVersionChangeWebSSO"), _configuration.GetValue<string>("iPhoneVersionChangeWebSSO"), "", "", true, _configuration);
        }


        public static List<MOBPNRPassenger> GetAllPassangers
                    (MOBRESHOPChangeEligibilityRequest request,
                    Service.Presentation.ReservationResponseModel.ReservationDetail cslresponse)
        {
            //TODO null check
            List<MOBPNRPassenger> passengers = new List<MOBPNRPassenger>();

            var travelers = cslresponse.Detail.Travelers;

            if (travelers == null || !travelers.Any()) return null;

            travelers.ToList().ForEach(pax =>
            {

                if (pax != null && pax.Person != null)
                {
                    MOBPNRPassenger passenger = new MOBPNRPassenger
                    {
                        SHARESPosition = pax.Person.Key,
                        TravelerTypeCode = pax.Person.Type,
                        SSRDisplaySequence = pax.Person.Key,
                        PricingPaxType = pax.Person.PricingPaxType
                    };

                    passenger.PNRCustomerID = pax.Person.CustomerID;
                    passenger.BirthDate = pax.Person.DateOfBirth;
                    passenger.SharesGivenName = pax.Person.GivenName;
                    passenger.PassengerName = new MOBName
                    {
                        First = pax.Person.GivenName,
                        Last = pax.Person.Surname,
                    };
                    passengers.Add(passenger);
                }
            });

            return passengers;
        }

        public static bool CheckForAward
                    (MOBRESHOPChangeEligibilityRequest request,
                    Service.Presentation.ReservationResponseModel.ReservationDetail cslresponse)
        {
            Genre iTinType = cslresponse.Detail.Type?.FirstOrDefault
                (x => x != null
                && x.Description.Equals("ITIN_TYPE")
                && !string.IsNullOrEmpty(x.Key));

            if (iTinType != null
                && iTinType.Key.Equals("AWARD", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            return false;
        }

        public static bool CheckUnaccompaniedMinorAvailable(United.Service.Presentation.ReservationResponseModel.ReservationDetail details)
        {
            bool IsUnaccompaniedMinor = false;

            try
            {
                if (details != null && details.Detail != null)
                {
                    if (details.Detail.Travelers != null && details.Detail.Travelers.Any())
                    {
                        details.Detail.Travelers.ToList().ForEach(traveler =>
                        {
                            if (string.Equals(traveler.IsUnaccompaniedMinor, "true", StringComparison.OrdinalIgnoreCase))
                            {
                                IsUnaccompaniedMinor = true;
                                return;
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            { return IsUnaccompaniedMinor; }
            return IsUnaccompaniedMinor;
        }

        public static string GetCharacteristicValue(List<Characteristic> characteristics, string code)
        {
            string keyValue = string.Empty;
            if (characteristics.Exists(p => p.Code == code))
            {
                keyValue = characteristics.First(p => p.Code == code).Value;
            }
            return keyValue;
        }

        public static string GetOriginalFormOfPaymentLabelForReshopChange
            (ReservationDetail cslReservation, Model.Shopping.Reservation bookingPathRes, string waivedDesc)
        {
            var isRefundable = false;
            var cslPayment = cslReservation.Detail.Payment;
            var prices = cslReservation.Detail.Prices?.ToList();


            if (waivedDesc != string.Empty && waivedDesc.ToLower() == "true")
            {
                isRefundable = true;
            }
            else if (prices != null && prices.Count > 0)
            {
                isRefundable = prices.Exists(p => p.Rules != null && p.Rules.ToList().Exists(r => r.Description.ToUpper().Contains("REFUNDABLE")));
            }

            string msg = string.Empty;
            var lastFourDigits = string.Empty;
            if (cslPayment.Type != null && cslPayment.Type.Value.ToUpper() == "PAYPAL")
            {
                lastFourDigits = "PayPal";//cslPayment.Type.Value.ToUpper();
            }
            else if ((!string.IsNullOrEmpty(cslPayment.AccountNumber)))
            {
                lastFourDigits = "**" + (cslPayment.AccountNumber.Length > 4 ? cslPayment.AccountNumber.Substring(cslPayment.AccountNumber.Length - 4) : cslPayment.AccountNumber);
            }
            if (isRefundable && !string.IsNullOrEmpty(lastFourDigits))
            {
                msg = string.Format((_configuration.GetValue<string>("ReshopChange-RTIOriginalFOPMessage") ?? ""), lastFourDigits);
            }
            else
            {
                msg = string.Format(_configuration.GetValue<string>("ReshopChange-RTIElectronicCertMessage") ?? "");
            }
            return msg;
        }


        public static List<MOBEmail> GetMobEmails(Collection<EmailAddress> pnrEmailAddress)
        {
            var mobEmails = new List<MOBEmail>();
            if (pnrEmailAddress != null)
            {
                foreach (var pnremail in pnrEmailAddress)
                {
                    var mobEmail = new MOBEmail();
                    mobEmail.EmailAddress = pnremail.Address.ToLower();
                    mobEmails.Add(mobEmail);
                }
            }
            return mobEmails;
        }

        public static MOBAddress GetMobFFCRAddress(Collection<Service.Presentation.ReservationModel.Traveler> travelers)
        {
            MOBAddress address = null;
            //start
            try
            {
                var allpaymentaddress = travelers?.FirstOrDefault()?.Tickets?.Where(x => x.Payments != null)
                    .SelectMany(x => x.Payments.Where(y => y.BillingAddress != null).Select(z => z.BillingAddress));

                if (allpaymentaddress == null || !allpaymentaddress.Any()) return null;

                var billingaddress = allpaymentaddress.LastOrDefault(x => !string.IsNullOrEmpty(x.AddressLines?.FirstOrDefault(y => !string.IsNullOrEmpty(y)))
                                      && !string.IsNullOrEmpty(x.Country.CountryCode));

                if (billingaddress == null) return null;

                address = new MOBAddress
                {
                    Country = new MOBCountry { Code = billingaddress.Country.CountryCode },
                    PostalCode = billingaddress.PostalCode,
                    City = billingaddress.City
                };

                if (!string.IsNullOrEmpty(billingaddress.StateProvince?.StateProvinceCode))
                {
                    address.State = new State { Code = billingaddress.StateProvince.StateProvinceCode };
                }

                foreach (string line in billingaddress.AddressLines)
                {
                    if (string.IsNullOrEmpty(address.Line1)) address.Line1 = line;
                    else if (string.IsNullOrEmpty(address.Line2)) address.Line2 = line;
                    else if (string.IsNullOrEmpty(address.Line3)) address.Line3 = line;
                }
                return address;
            }
            catch { return null; }
        }

        public static bool IncludeReshopFFCResidual(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableReshopFFCResidual")
                && GeneralHelper.IsApplicationVersionGreater
                (appId, appVersion, "AndroidFFCResidualVersion", "iPhoneFFCResidualVersion", "", "", true, _configuration);
        }

        internal static bool IsIbe(Collection<Characteristic> characteristics)
        {
            return _configuration.GetValue<bool>("EnableIBE") &&
                    characteristics != null &&
                    characteristics.Any(c => c != null &&
                                            "PRODUCTCODE".Equals(c.Code, StringComparison.OrdinalIgnoreCase) &&
                                            "IBE".Equals(c.Value, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool IsElf(Collection<Characteristic> characteristics)
        {
            return _configuration.GetValue<bool>("EnableIBE") &&
                    characteristics != null &&
                    characteristics.Any(c => c != null &&
                                            "PRODUCTCODE".Equals(c.Code, StringComparison.OrdinalIgnoreCase) &&
                                            "ELF".Equals(c.Value, StringComparison.OrdinalIgnoreCase));
        }

        public static string GetTravelTime(string appVersion, TimeSpan journeyduration)
        {
            string flighttime = string.Empty;

            if (journeyduration.Hours > 0)
            {
                if (!string.IsNullOrEmpty(appVersion) && appVersion.ToUpper().Equals("2.1.8I"))
                {
                    flighttime = "0 HR " + journeyduration.Hours;
                }
                else if (journeyduration.Hours > 0)
                {
                    flighttime = journeyduration.Hours + " HR";
                }
            }
            if (journeyduration.Minutes > 0)
            {
                if (!string.IsNullOrEmpty(appVersion) && appVersion.ToUpper().Equals("2.1.8I"))
                {
                    flighttime = flighttime + " " + journeyduration.Minutes + " 0 MN";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(flighttime))
                    {
                        flighttime = journeyduration.Minutes + " MN";
                    }
                    else
                    {
                        flighttime = flighttime + " " + journeyduration.Minutes + " MN";
                    }
                }
            }
            return flighttime;
        }

        public static string GetRedEyeFlightArrDate(String flightDepart, String flightArrive, ref bool flightDateChanged)
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

        public static string GetRedEyeDepartureDate(String tripDate, String flightDepartureDate, ref bool flightDateChanged)
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

        public static string GetServiceClassDescriptionFromCslReservationFlightBookingClasses(Collection<BookingClass> bookingClasses)
        {
            string serviceClassDescription = string.Empty;
            if (bookingClasses != null && bookingClasses.Count > 0 && bookingClasses[0].Cabin != null)
            {
                serviceClassDescription = string.Format("{0} ({1})", bookingClasses[0].Cabin.Name, bookingClasses[0].Code);
            }
            return serviceClassDescription;
        }

        public static SHOPEquipmentDisclosure ConvertPNRSegmentEquipmentToMobShopEquipmentDisclousures(Service.Presentation.CommonModel.AircraftModel.Aircraft airCraft)
        {
            SHOPEquipmentDisclosure mobShopEquipmentDisclosure = null;
            if (airCraft != null && airCraft.Model != null)
            {
                mobShopEquipmentDisclosure = new SHOPEquipmentDisclosure();
                mobShopEquipmentDisclosure.EquipmentType = airCraft.Model.Fleet;
                mobShopEquipmentDisclosure.EquipmentDescription = airCraft.Model.Description;
                mobShopEquipmentDisclosure.WheelchairsNotAllowed = !string.IsNullOrEmpty(airCraft.Model.IsWheelchairAllowed);
                mobShopEquipmentDisclosure.NonJetEquipment = !string.IsNullOrEmpty(airCraft.Model.IsJetEquipment);
                mobShopEquipmentDisclosure.NoBoardingAssistance = !string.IsNullOrEmpty(airCraft.Model.HasBoardingAssistance);
                mobShopEquipmentDisclosure.IsSingleCabin = !string.IsNullOrEmpty(airCraft.Model.IsSingleCabin);
            }
            return mobShopEquipmentDisclosure;
        }

        public static MOBException SetCustomMobExceptionMessage(string message, string code)
        {
            var customexceptionmsg = new MOBException();
            customexceptionmsg.Code = string.IsNullOrEmpty(code) ? "9999" : code;
            customexceptionmsg.Message = message;
            return customexceptionmsg;
        }

        public static bool AllowAgencyToChangeCancel(string agencyname, string requesttype)
        {
            if (!_configuration.GetValue<bool>("AllowSelectedAgencyChangeCancelPath")) return false;
            var agencynamearray
                = ConfigUtility.GetListFrmPipelineSeptdConfigString("ReshopChangeCancelEligibleAgencyNames");
            return (agencynamearray != null && agencynamearray.Any() && agencynamearray.Contains(agencyname)) ? true : false;
        }

        public static bool EnableAgencyChangeMessage(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreater(appId, appVersion, _configuration.GetValue<string>("AndroidAgencyChangeMessageVersion"), _configuration.GetValue<string>("iPhoneAgencyChangeMessageVersion"), "", "", true, _configuration);
        }

        public static string GetEnumValue(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(Convert.ToString(value));
            var attribute = (System.Runtime.Serialization.EnumMemberAttribute)fi.GetCustomAttribute
                (typeof(System.Runtime.Serialization.EnumMemberAttribute));
            return attribute.Value;
        }

        static public bool IsVersionAllowAward(int applicationId, MOBVersion appVersion, string androidVersion, string iOSVersion)
        {
            bool award = false;
            if (applicationId == 1)
            {
                appVersion.Major = appVersion.Major.Replace("I", string.Empty);
                award = GeneralHelper.SeperatedVersionCompareCommonCode(appVersion.Major, iOSVersion);
            }
            else if (applicationId == 2)
            {
                appVersion.Major = appVersion.Major.Replace("A", string.Empty);
                award = GeneralHelper.SeperatedVersionCompareCommonCode(appVersion.Major, androidVersion);
            }

            return award;
        }

        public static string GetPNRRedirectUrl(string recordLocator, string lastlName, string reqType)
        {
            string retUrl = string.Empty;

            if (string.Equals(reqType, "EX", StringComparison.OrdinalIgnoreCase))
            {
                retUrl = string.Format("https://{0}/ual/en/US/flight-search/change-a-flight/changeflight/changeflight/rev?PNR={1}&RiskFreePolicy=&TYPE=rev&source=MOBILE",
                 _configuration.GetValue<string>("DotComChangeResBaseUrl"), HttpUtility.UrlEncode(EncryptString(recordLocator)));
            }
            else
            {
                if (string.Equals(reqType, "AWARD_CA", StringComparison.OrdinalIgnoreCase))
                {
                    retUrl = string.Format("http://{0}/{1}?TY=F&CN={2}&FLN={3}&source=MOBILE",
                    _configuration.GetValue<string>("DotComOneCancelURL"),
                    _configuration.GetValue<string>("ReShopRedirectPath"),
                    EncryptString(recordLocator),
                    EncryptString(lastlName)
                   );
                }
                else
                {
                    retUrl = string.Format("http://{0}/{1}?TY=F&AC={2}&CN={3}&FLN={4}&source=MOBILE",
                    _configuration.GetValue<string>("DotComOneCancelURL"),
                    _configuration.GetValue<string>("ReShopRedirectPath"),
                    reqType,
                    EncryptString(recordLocator),
                    EncryptString(lastlName)
                   );
                }
            }
            return retUrl;
        }

        public static bool IsChildTravelerAvailable(List<MOBPNRPassenger> passangers)
        {
            string adtTravelerTypeCode = "ADT";
            string snrTravelerTypeCode = "SNR";
            try
            {
                if (passangers != null && passangers.Any())
                {
                    var isChildTraveler = passangers.FirstOrDefault(x =>
                    (!string.Equals(adtTravelerTypeCode, x.TravelerTypeCode, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(snrTravelerTypeCode, x.TravelerTypeCode, StringComparison.OrdinalIgnoreCase)));
                    if (isChildTraveler != null)
                    {
                        return true;
                    }
                }
            }
            catch { return false; }
            return false;
        }

        public static MOBPNRAdvisory PopulateReshopAdvisoryContent(string displaycontent)
        {
            try
            {
                string strnewtrip = GetConfigEntries(displaycontent);

                string[] splitpattern = { "||" };
                string[] splititems = strnewtrip.Split(splitpattern, System.StringSplitOptions.RemoveEmptyEntries);

                if (splititems == null || !splititems.Any()) return null;

                MOBPNRAdvisory advisorycontent = new MOBPNRAdvisory
                {
                    ContentType = ContentType.RESHOPNEWTRIP,
                    AdvisoryType = AdvisoryType.INFORMATION,
                    ShouldExpand = true,
                    IsBodyAsHtml = true,
                };

                splititems.ForEach(splititem =>
                {
                    if (!string.IsNullOrEmpty(splititem))
                    {
                        string[] item = splititem.Split('|');

                        if (!string.IsNullOrEmpty(item[0])
                          && !string.IsNullOrEmpty(item[1]))
                        {
                            switch (item[0])
                            {
                                case "Header":
                                    advisorycontent.Header = item[1];
                                    break;
                                case "Body":
                                    advisorycontent.Body = HttpUtility.HtmlDecode(item[1]);
                                    break;
                                case "SubTitle":
                                    advisorycontent.SubTitle = item[1];
                                    break;
                            }
                        }
                    }
                });

                return advisorycontent;
            }
            catch { return null; }
        }

        public static string GetConfigEntries(string configKey)
        {
            try
            {
                var configString = _configuration.GetValue<string>(configKey) ?? string.Empty;
                return configString = (configString.IsNullOrEmpty()) ? string.Empty : configString;
            }
            catch { return string.Empty; }
        }

        public static bool EnableInfantInLapVersion(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreater(appId, appVersion, _configuration.GetValue<string>("AndroidInfantInLapVersion"), _configuration.GetValue<string>("iPhoneInfantInLapVersion"), "", "", true, _configuration);
        }

        public static bool SupressPopupWhenBEIBEWithExceptionPolicy(EligibilityResponse eligibility)
        {
            if (_configuration.GetValue<bool>("EnableCheckWhenBEIBEWith24hrAndExceptionPolicy"))
            {
                if (eligibility.IsElf || eligibility.IsIBE || eligibility.IsIBELite)
                { return true; }
            }
            return false;
        }

        public static bool EnableReshopCheckinEligibleMessage(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreater(appId, appVersion, _configuration.GetValue<string>("AndroidReshopCheckinEligibleMsgVersion"), _configuration.GetValue<string>("iPhoneReshopCheckinEligibleMsgVersion"), "", "", true, _configuration);
        }
    }
}
