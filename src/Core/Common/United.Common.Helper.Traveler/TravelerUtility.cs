using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using United.Common.Helper.Shopping;
using United.Definition;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.DataAccess.Travelers;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.Shopping;
using United.Mobile.Model.Common.SSR;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Service.Presentation.PaymentModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.Cart;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.Extensions;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Utility.Enum;
using United.Utility.Helper;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using Image = United.Mobile.Model.Shopping.Image;
using MOBFOPTravelCredit = United.Mobile.Model.Shopping.FormofPayment.MOBFOPTravelCredit;
using ProfileFOPCreditCardResponse = United.Mobile.Model.Shopping.ProfileFOPCreditCardResponse;
using ProfileResponse = United.Mobile.Model.Shopping.ProfileResponse;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using WorkFlowType = United.Service.Presentation.CommonEnumModel.WorkFlowType;
using System.Threading.Tasks;
using United.Mobile.Model.Travelers;
using United.Service.Presentation.ReferenceDataModel;
using System.Text.RegularExpressions;

namespace United.Common.Helper.Traveler
{
    public class TravelerUtility : ITravelerUtility
    {
        private readonly ICacheLog<TravelerUtility> _logger;
        private readonly IConfiguration _configuration;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ICachingService _cachingService;
        private readonly IShoppingBuyMiles _shoppingBuyMiles;
        private readonly DocumentLibraryDynamoDB _documentLibraryDynamoDB;
        private readonly IFlightShoppingProductsService _getProductsService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly IOmniCart _omniCart;
        private string PATH_COUNTRIES_XML;
        private List<string[]> Countries = new List<string[]>();
        private static readonly string _MSG1 = "MSG1";
        private static readonly string _ERR1 = "ERR1";
        private static readonly string _ERR2 = "ERR2";
        private static readonly string _ERR3 = "ERR3";
        private static readonly string _ERR4 = "Err4";
        private static readonly string _ERR5 = "ERR5";
        private readonly IHeaders _headers;
        private readonly ILogger<TravelerUtility> _logger1;
        private readonly IFeatureToggles _featureToggles;
        public TravelerUtility(ICacheLog<TravelerUtility> logger
            , IConfiguration configuration
            , IShoppingUtility shoppingUtility
            , ISessionHelperService sessionHelperService
            , IFFCShoppingcs fFCShoppingcs
            , IShoppingCartService shoppingCartService
            , IDynamoDBService dynamoDBService
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , ICachingService cachingService
            , IShoppingBuyMiles shoppingBuyMiles
            , IOmniCart omniCart
            , IFlightShoppingProductsService getProductsService
            , IHeaders headers
            , ILogger<TravelerUtility> logger1
            , IFeatureToggles featureToggles)
            
        {
            _logger = logger;
            _configuration = configuration;
            _shoppingUtility = shoppingUtility;
            _sessionHelperService = sessionHelperService;
            _fFCShoppingcs = fFCShoppingcs;
            _shoppingCartService = shoppingCartService;
            _dynamoDBService = dynamoDBService;
            _cachingService = cachingService;
            _shoppingBuyMiles = shoppingBuyMiles;
            _headers = headers;
            _documentLibraryDynamoDB = new DocumentLibraryDynamoDB(_configuration, _dynamoDBService);
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _getProductsService = getProductsService;
            _omniCart = omniCart;
            _logger1 = logger1;
            _featureToggles = featureToggles;
        }
        public List<MOBCPTraveler> AssignInfantWithSeat(Reservation bookingPathReservation, List<MOBCPTraveler> travelers)
        {
            //int searchInfantwithSeatCount = bookingPathReservation.ShopReservationInfo2.DisplayTravelTypes.Where(t => t.TravelerType == MOBPAXTYPE.InfantSeat).Count();
            //int infantWithSeatCount = travelers.Where(t => t.TravelerTypeCode.Equals("INS")).Count();

            //if (infantWithSeatCount < searchInfantwithSeatCount)
            //{
            //    travelers.Where(t => t.TravelerTypeCode.Equals("INF")).Take(searchInfantwithSeatCount - infantWithSeatCount).ToList().ForEach(t => t.TravelerTypeCode = "INS");
            //}
            int searchInfantinLapCount = 0;
            if (_configuration.GetValue<bool>("INFFixWithRepriceEnabled"))
            {
                if (bookingPathReservation.ShopReservationInfo2.TravelerTypes != null && bookingPathReservation.ShopReservationInfo2.TravelerTypes.Any(t => t.TravelerType.ToUpper().Equals(PAXTYPE.InfantLap.ToString().ToUpper())))
                    searchInfantinLapCount = bookingPathReservation.ShopReservationInfo2.TravelerTypes.First(t => t.TravelerType.ToUpper().Equals(PAXTYPE.InfantLap.ToString().ToUpper())).Count;
            }
            else
            {
                searchInfantinLapCount = bookingPathReservation.ShopReservationInfo2.displayTravelTypes.Where(t => t.TravelerType == PAXTYPE.InfantLap).Count();
            }
            int infantInLapCount = travelers.Where(t => t.TravelerTypeCode != null && t.TravelerTypeCode.ToUpper().Equals("INF")).Count();

            if (infantInLapCount > searchInfantinLapCount)
            {
                travelers.Where(t => t.TravelerTypeCode.ToUpper().Equals("INF")).Skip(searchInfantinLapCount).ToList().ForEach(t => t.TravelerTypeCode = "INS");
            }

            return travelers;
        }
        public List<MOBCPTraveler> AssignInfantInLap(Reservation bookingPathReservation, List<MOBCPTraveler> travelers)
        {
            if (!comapreTtypesList(bookingPathReservation, travelers))
            {
                int searchInfantinLapCount = bookingPathReservation.ShopReservationInfo2.displayTravelTypes.Where(t => t.TravelerType == PAXTYPE.InfantLap).Count();
                int infantInLapCount = travelers.Where(t => t.TravelerTypeCode.ToUpper().Equals("INF")).Count();

                if (infantInLapCount < searchInfantinLapCount)
                {
                    travelers.Where(t => t.TravelerTypeCode.ToUpper().Equals("INS")).Take(searchInfantinLapCount - infantInLapCount).ToList().ForEach(t => t.TravelerTypeCode = "INF");
                }
            }

            return travelers;
        }
        private bool comapreTtypesList(Reservation bookingPathReservation, List<MOBCPTraveler> travelers)
        {
            if (!travelers.Any(t => t.TravelerTypeCode.ToUpper().Equals("INF")))
                return false;

            List<string> lstReservation = bookingPathReservation.ShopReservationInfo2.displayTravelTypes.Select(t => t.PaxType).ToList();
            List<string> lstRequest = travelers.Select(t => t.TravelerTypeCode).ToList();


            if (lstReservation.Count == lstRequest.Count && lstReservation.Except(lstRequest).Count() == 0 && lstRequest.Except(lstReservation).Count() == 0)
                return true;

            return false;

        }
        public bool EnableYoungAdultValidation(bool isReshop = false)
        {
            return _configuration.GetValue<bool>("EnableYoungAdultValidation") && _configuration.GetValue<bool>("EnableYoungAdultBooking") && !isReshop;
        }
        public int GetChildInLapCount(MOBCPTraveler traveler, List<int> travelerAges, int childInLapCount, string firstLOFDepDate)
        {
            if (string.IsNullOrEmpty(traveler.BirthDate))
                throw new MOBUnitedException("Please ensure these fields are valid:first name, last name, date of birth, gender");

            int age = TopHelper.GetAgeByDOB(traveler.BirthDate, firstLOFDepDate);

            if (!string.IsNullOrEmpty(traveler.TravelerTypeCode))
            {
                if (age < 2 && traveler.TravelerTypeCode.ToUpper().Equals("INF"))
                    childInLapCount++;
            }

            travelerAges.Add(age);
            return childInLapCount;
        }
        private int GetAgeByDOB(string birthDate, string firstLOFDepDate)
        {
            if (!string.IsNullOrEmpty(birthDate))
            {
                var travelDate = DateTime.Parse(firstLOFDepDate);

                var birthDate1 = DateTime.Parse(birthDate);
                // Calculate the age.
                var age = travelDate.Year - birthDate1.Year;
                // Go back to the year the person was born in case of a leap year
                if (birthDate1 > travelDate.AddYears(-age)) age--;

                return age;
            }
            else
                return 20;
        }
        public void ValidateTravelerAges(List<int> travelerAges, int inLapChildCount = 0)
        {

            if (travelerAges == null || travelerAges.Count < 1)
                return;

            // Replace the value 14 to 17 for fixing the Bug #MOBILE-19043
            if (!travelerAges.Any(i => i > 17))
                throw new MOBUnitedException("UnaccompaniedDisclaimerMessage", _configuration.GetValue<string>("UnaccompaniedDisclaimerMessage"));

            if (inLapChildCount < 1)
                return;

            if (travelerAges.Where(age => age > 17).Count() < inLapChildCount)
                throw new MOBUnitedException("InfantDisclaimerMessage", _configuration.GetValue<string>("InfantDisclaimerMessage"));
        }
        public bool ShowUpliftTpiMessage(Reservation reservation, string formOfPaymentType)
        {
            return (reservation?.TripInsuranceFile?.TripInsuranceBookingInfo?.IsRegistered ?? false) &&
                   (formOfPaymentType == MOBFormofPayment.Uplift.ToString());
        }
        public void ValidateTravelersForCubaReason(List<MOBCPTraveler> travelersCSL, bool isCuba)
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
        public  async Task<List<TravelSpecialNeed>> ValidateSpecialNeedsAgaintsMasterList(List<TravelSpecialNeed> specialNeeds, TravelSpecialNeeds masterList, int appId=0, string appVersion ="", List<MOBItem> catalogItems =null)
        {
            if (specialNeeds == null || !specialNeeds.Any() || masterList == null)
                return null;

            Action<string, MOBCPTraveler> addSSRMsgToTraveler = (desc, traveler) =>
            {
                var travelSSRInfoMsg = new MOBItem { CurrentValue = desc };

                if (traveler.SelectedSpecialNeedMessages == null)
                {
                    traveler.SelectedSpecialNeedMessages = new List<MOBItem> { travelSSRInfoMsg };
                }
                else
                {
                    traveler.SelectedSpecialNeedMessages.Add(travelSSRInfoMsg);
                }
            };

            var results = new List<TravelSpecialNeed>();
            var referenceData = masterList.CloneDeep();

            // meal
            if (referenceData.SpecialMeals != null && referenceData.SpecialMeals?.Count > 0)
            {
                var specialMealsMasterList = referenceData.SpecialMeals.Where(x => !string.IsNullOrWhiteSpace(x.Code));
                var meals = specialNeeds.Where(x => !string.IsNullOrWhiteSpace(x.Type) && x.Type.Trim().Equals(TravelSpecialNeedType.SpecialMeal.ToString().Trim()) && !string.IsNullOrWhiteSpace(x.Code)).ToList();
                if (meals != null && meals.Any() && specialMealsMasterList != null && specialMealsMasterList.Any())
                {
                    foreach (var meal in meals)
                    {
                        var matchItem = specialMealsMasterList.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.Code) && m.Code.Trim().Equals(meal.Code.Trim(), StringComparison.OrdinalIgnoreCase));

                        if (matchItem != null) // has meal reference
                        {
                            results.Add(meal);
                        }
                    }
                }
            }

            // Special Requests
            if (referenceData.SpecialRequests != null)
            {
                var specialRequestsMasterList = referenceData.SpecialRequests.Where(x => !string.IsNullOrWhiteSpace(x.Code) && !x.Code.ToUpper().Equals("OTHS"));
                var requests = specialNeeds.Where(x => !string.IsNullOrWhiteSpace(x.Type) && (x.Type.Trim().Equals(TravelSpecialNeedType.SpecialRequest.ToString().Trim()) || x.Type.Trim().Equals(TravelSpecialNeedType.ServiceAnimalType.ToString().Trim()))
                && !string.IsNullOrWhiteSpace(x.Code));
                if (requests != null && requests.Any() && specialRequestsMasterList != null && specialRequestsMasterList.Any())
                {
                    foreach (var request in requests) // OTHS is service animal
                    {
                        var mainFound = specialRequestsMasterList.FirstOrDefault(m => m.Code.Trim().Equals(request.Code.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (mainFound == null)
                            continue;

                        if (request.SubOptions == null || !request.SubOptions.Any())
                        {
                            if (mainFound.SubOptions == null || !mainFound.SubOptions.Any())
                                results.Add(mainFound.CloneDeep());
                        }
                        else if (mainFound.SubOptions != null && mainFound.SubOptions.Any())
                        {
                            foreach (var requestSub in request.SubOptions)
                            {
                                var subFound = mainFound.SubOptions.FirstOrDefault(subR => !string.IsNullOrWhiteSpace(subR.Code) && subR.Code.Trim().Equals(requestSub.Code.Trim(), StringComparison.OrdinalIgnoreCase));
                                if (subFound == null)
                                    continue;

                                mainFound = mainFound.CloneDeep();
                                mainFound.SubOptions.Clear();
                                mainFound.SubOptions.Add(subFound.CloneDeep());
                                results.Add(mainFound);
                            }
                        }
                    }
                    if (_configuration.GetValue<bool>("EnableExtraSeatsFeature") && (specialNeeds.Where(a => a.Code?.Contains("EXST") == true).Any() || specialNeeds.Where(a => a.Code?.Contains("CBBG") == true).Any()))
                        results.Add(specialNeeds.Where(a => a.Code?.Contains("EXST") == true || a.Code?.Contains("CBBG") == true)?.FirstOrDefault());
                    try
                    {
                        if (await _shoppingUtility.IsEnableWheelChairSizerChanges(appId, appVersion, catalogItems).ConfigureAwait(false)
                            && specialNeeds.Any(sn => sn.Code == _configuration.GetValue<string>("SSRWheelChairDescription")
                            && sn.SubOptions != null && sn.SubOptions.Any()
                            && sn.SubOptions[0].WheelChairDimensionInfo != null
                            && !string.IsNullOrEmpty(sn.SubOptions[0].WheelChairDimensionInfo.Dimensions)) && results != null && results.Any() && results.Count > 0)
                        {
                            foreach (TravelSpecialNeed need in results)
                            {
                                if (need != null && need?.Code == _configuration.GetValue<string>("SSRWheelChairDescription") && need.SubOptions != null && need.SubOptions.Any())
                                {
                                    need.SubOptions[0].WheelChairDimensionInfo = new MOBDimensions();
                                    need.SubOptions[0].WheelChairDimensionInfo.Dimensions = specialNeeds.FirstOrDefault(sn => sn.Code == _configuration.GetValue<string>("SSRWheelChairDescription"))?.SubOptions[0]?.WheelChairDimensionInfo?.Dimensions;
                                    need.SubOptions[0].WheelChairDimensionInfo.WcFitConfirmationMsg = _configuration.GetValue<string>("WheelChairSizeSuccessMsg");
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger1.LogError("Retaining WheelChair dimensions {@message} {@stackTrace}", ex.Message, ex.StackTrace);
                    }
                }
            }

            // service animal preferences
            if (_configuration.GetValue<bool>("EnableServiceAnimalEnhancements") == false && referenceData.ServiceAnimals != null)
            {
                var serviceAnimalMasterList = referenceData.ServiceAnimals.Where(x => !string.IsNullOrWhiteSpace(x.Value));
                var serviceAnimals = specialNeeds.Where(x => !string.IsNullOrWhiteSpace(x.Type) && x.Type.Equals(TravelSpecialNeedType.ServiceAnimalType.ToString())
                                    && (_configuration.GetValue<bool>("EnableServiceAnimalEnhancements") || !string.IsNullOrWhiteSpace(x.Value)));
                if (serviceAnimals != null && serviceAnimals.Any() && serviceAnimalMasterList != null && serviceAnimalMasterList.Any()) // has meal reference
                {
                    foreach (var sa in serviceAnimals) // OTHS is service animal
                    {
                        var mainFound = serviceAnimalMasterList.FirstOrDefault(m => m.Value.Trim().Equals(sa.Value.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (mainFound == null)
                            continue;

                        if (sa.SubOptions == null || !sa.SubOptions.Any())
                        {
                            if (mainFound.SubOptions == null || !mainFound.SubOptions.Any())
                                results.Add(mainFound.CloneDeep());
                        }
                        else if (mainFound.SubOptions != null && mainFound.SubOptions.Any())
                        {
                            foreach (var subSa in sa.SubOptions)
                            {
                                var subFound = mainFound.SubOptions.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.Value) && a.Value.Trim().Equals(subSa.Value.Trim(), StringComparison.OrdinalIgnoreCase));
                                if (subFound == null)
                                    continue;

                                mainFound = mainFound.CloneDeep();
                                mainFound.SubOptions.Clear();
                                mainFound.SubOptions.Add(subFound.CloneDeep());
                                results.Add(mainFound);
                            }
                        }
                    }
                }
            }

            return results;
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
        public InfoWarningMessages GetPriceChangeMessage()
        {
            InfoWarningMessages inhibitMessage = new InfoWarningMessages();

            inhibitMessage.Order = MOBINFOWARNINGMESSAGEORDER.PRICECHANGE.ToString();
            inhibitMessage.IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString();

            inhibitMessage.Messages = new List<string>();
            inhibitMessage.Messages.Add((_configuration.GetValue<string>("PriceChangeMessage") as string) ?? string.Empty);

            return inhibitMessage;
        }
        public void UpdateInhibitMessage(ref Reservation reservation)
        {
            if (reservation == null) return;

            if (reservation.ShopReservationInfo2 == null)
                reservation.ShopReservationInfo2 = new ReservationInfo2();

            if (reservation.ShopReservationInfo2.InfoWarningMessages == null)
                reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();


            if (!reservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.INHIBITBOOKING.ToString()))
            {
                if (_shoppingUtility.EnableBoeingDisclaimer(reservation.IsReshopChange))
                {
                    if (!_configuration.GetValue<bool>("TurnOffBookingCutoffMinsFromCSL"))
                    {
                        reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetInhibitMessage(reservation.ShopReservationInfo2.BookingCutOffMinutes));
                    }
                    else
                    {
                        reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetInhibitMessage(string.Empty));
                    }
                    reservation.ShopReservationInfo2.InfoWarningMessages = reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)System.Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                }
                else
                {
                    List<InfoWarningMessages> lst = reservation.ShopReservationInfo2.InfoWarningMessages.Clone();
                    reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();
                    //reservation.ShopReservationInfo2.InfoWarningMessages.Add(Utility.GetInhibitMessage());
                    if (!_configuration.GetValue<bool>("TurnOffBookingCutoffMinsFromCSL"))
                    {
                        reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetInhibitMessage(reservation.ShopReservationInfo2.BookingCutOffMinutes));

                    }
                    else
                    {
                        reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetInhibitMessage(string.Empty));
                    }
                    reservation.ShopReservationInfo2.InfoWarningMessages.AddRange(lst);
                }
            }

        }
        public void ValidateTravelersCSLForCubaReason(MOBSHOPReservation reservation)
        {
            if (reservation.IsCubaTravel)
            {
                var travelersCSL = reservation.TravelersCSL;
                ValidateTravelersCSLForCubaReason(travelersCSL);
                if (reservation.ShopReservationInfo2 != null && reservation.ShopReservationInfo2.AllEligibleTravelersCSL != null && reservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count > 0)
                {
                    ValidateTravelersForCubaReason(reservation.ShopReservationInfo2.AllEligibleTravelersCSL, reservation.IsCubaTravel);
                }
            }
        }
        private bool IsCubaTravelerHasReason(MOBCPTraveler traveler)
        {
            return (traveler.CubaTravelReason != null && !string.IsNullOrEmpty(traveler.CubaTravelReason.Vanity));
        }
        public bool EnableTPI(int appId, string appVersion, int path)
        {
            // path 1 means confirmation flow, path 2 means view reservation flow, path 3 means booking flow 
            if (path == 1)
            {
                // ==>> Venkat and Elise chagne code to offer TPI postbooking when inline TPI is off for new clients 2.1.36 and above
                // App Version 2.1.36 && ShowTripInsuranceSwitch = true
                bool offerTPIAtPostBooking = _configuration.GetValue<bool>("ShowTripInsuranceSwitch") &&
                    GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidTPIConfirmationVersion", "iPhoneTPIConfirmationVersion", "", "", true, _configuration);
                if (offerTPIAtPostBooking)
                {
                    offerTPIAtPostBooking = !GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidTPIBookingVersion", "iPhoneTPIBookingVersion", "", "", true, _configuration);                    // When the Flag is true, we offer for old versions, when the flag is off, we offer for all versions. 
                    if (!offerTPIAtPostBooking && _configuration.GetValue<bool>("ShowTPIatPostBooking_ForAppVer_2.1.36_UpperVersions"))
                    {
                        //"ShowTripInsuranceBookingSwitch" == false
                        //ShowTPIatPostBooking_ForAppVer_2.1.36_LowerVersions = true
                        //
                        offerTPIAtPostBooking = true;
                    }
                }
                return offerTPIAtPostBooking;
            }
            else if (path == 2)
            {
                return _configuration.GetValue<bool>("ShowTripInsuranceViewResSwitch") &&
                    GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidTPIViewResVersion", "iPhoneTPIViewResVersion", "", "", true, _configuration);
            }
            else if (path == 3)
            {
                return _configuration.GetValue<bool>("ShowTripInsuranceBookingSwitch") &&
                    GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidTPIBookingVersion", "iPhoneTPIBookingVersion", "", "", true, _configuration);
            }
            else
            {
                return false;
            }
        }
        public bool IsEnableNavigation(bool isReshop = false)
        {
            return _configuration.GetValue<bool>("EnableBookingNavigation") && !isReshop;
        }

        public bool IsFFCTravelerChanged(List<MOBCPTraveler> travelers, SerializableDictionary<string, MOBCPTraveler> travelerCSLBeforeRegister)
        {
            bool isFFCTravelerChanged = false;
            var ffcTravelers = travelerCSLBeforeRegister.Where(persistedTraveler => persistedTraveler.Value?.FutureFlightCredits?.Count() > 0);
            if (ffcTravelers?.Count() > 0)
            {
                foreach (var trv in ffcTravelers)
                {
                    var currentTravelrFromRequestedTravelers = travelers.FirstOrDefault(t => t.PaxID == trv.Value.PaxID);
                    if (currentTravelrFromRequestedTravelers != null && trv.Value.FutureFlightCredits?.Count() > 0)
                    {
                        isFFCTravelerChanged = ShopStaticUtility.IsValuesChangedForSameTraveler(currentTravelrFromRequestedTravelers, trv.Value);
                    }
                    else
                    {
                        isFFCTravelerChanged = true;
                    }

                    if (isFFCTravelerChanged)
                        break;
                }
            }

            return isFFCTravelerChanged;
        }
        public bool IsETCTravelerChanged(List<MOBCPTraveler> travelers, SerializableDictionary<string, MOBCPTraveler> travelerCSLBeforeRegister, List<MOBFOPCertificate> certificates)
        {
            bool isETCTravelerchanged = false;
            var allTravelerCertificate = certificates.Find(ct => ct.CertificateTraveler?.TravelerNameIndex == "0");

            foreach (var trv in travelerCSLBeforeRegister)
            {
                var certificateForPersistedTraveler = certificates.Find(c => c.CertificateTraveler?.PaxId == trv.Value.PaxID);
                if (certificateForPersistedTraveler != null || allTravelerCertificate != null)
                {
                    var currentTravelrFromRequestedTravelers = travelers.FirstOrDefault(t => t.PaxID == trv.Value.PaxID);
                    if (currentTravelrFromRequestedTravelers != null)
                    {
                        isETCTravelerchanged = ShopStaticUtility.IsValuesChangedForSameTraveler(currentTravelrFromRequestedTravelers, trv.Value);
                    }
                    else
                    {
                        isETCTravelerchanged = true;
                    }
                }
                if (isETCTravelerchanged)
                    break;
            }

            return isETCTravelerchanged;
        }
        public T ObjectToObjectCasting<T, R>(R request)
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
        private FormofPaymentOption UpliftAsFormOfPayment(MOBSHOPReservation reservation, MOBShoppingCart shoppingCart)
        {
            if (_configuration.GetValue<bool>("EnableUpliftPayment"))
            {
                if (_shoppingUtility.IsEligibileForUplift(reservation, shoppingCart))
                {
                    return new FormofPaymentOption
                    {
                        Category = "UPLIFT",
                        FoPDescription = "Pay Monthly",
                        Code = "UPLIFT",
                        FullName = "Pay Monthly",
                        DeleteOrder = shoppingCart?.FormofPaymentDetails?.FormOfPaymentType?.ToUpper() != MOBFormofPayment.Uplift.ToString().ToUpper()
                    };
                }
            }
            return null;
        }
        public async Task<ProfileResponse> GetCSLProfileResponseInSession(string sessionId)
        {
            ProfileResponse profile = new ProfileResponse();
            try
            {
                profile = await _sessionHelperService.GetSession<ProfileResponse>(sessionId, profile.ObjectName, new List<string> { sessionId, profile.ObjectName }).ConfigureAwait(false);
            }
            catch (System.Exception)
            {

            }
            return profile;
        }



        public string GetPriceAfterChaseCredit(decimal price)
        {
            int creditAmt = (_configuration.GetValue<int>("ChaseStatementCredit"));

            CultureInfo culture = new CultureInfo("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            return String.Format(culture, "{0:C}", price - creditAmt);

            //return (Convert.ToDecimal(price - creditAmt)).ToString("C2", CultureInfo.CurrentCulture);
        }
        public string GetPriceAfterChaseCredit(decimal price, string chaseCrediAmount)
        {
            int creditAmt = 0;

            int.TryParse(chaseCrediAmount, System.Globalization.NumberStyles.AllowCurrencySymbol | System.Globalization.NumberStyles.AllowDecimalPoint, null, out creditAmt);

            CultureInfo culture = new System.Globalization.CultureInfo("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            return String.Format(culture, "{0:C}", price - creditAmt);

            //return (Convert.ToDecimal(price - creditAmt)).ToString("C2", CultureInfo.CurrentCulture);
        }

        public void FormatChaseCreditStatemnet(MOBCCAdStatement chaseCreditStatement)
        {
            if (_configuration.GetValue<bool>("UpdateChaseColor16788"))
            {
                chaseCreditStatement.styledInitialDisplayPrice = string.IsNullOrWhiteSpace(chaseCreditStatement.initialDisplayPrice) ? "" : HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginning")) + chaseCreditStatement.initialDisplayPrice + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
                chaseCreditStatement.styledInitialDisplayText = HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginning")) + _configuration.GetValue<string>("InitialDisplayText") + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
                chaseCreditStatement.styledStatementCreditDisplayPrice = string.IsNullOrWhiteSpace(chaseCreditStatement.statementCreditDisplayPrice) ? "" : HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginningWithColor")) + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextStrongBeginning")) + GetPriceAfterChaseCredit(0, chaseCreditStatement.statementCreditDisplayPrice) + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextStrongEnding")) + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
                chaseCreditStatement.styledStatementCreditDisplayText = HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginningWithColor")) + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextStrongBeginning")) + _configuration.GetValue<string>("StatementCreditDisplayText") + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextStrongEnding")) + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
                chaseCreditStatement.styledFinalAfterStatementDisplayPrice = string.IsNullOrWhiteSpace(chaseCreditStatement.finalAfterStatementDisplayPrice) ? "" : HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginning")) + chaseCreditStatement.finalAfterStatementDisplayPrice + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
                chaseCreditStatement.styledFinalAfterStatementDisplayText = HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginning")) + _configuration.GetValue<string>("FinalAfterStatementDisplayText") + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
            }
        }
        public async Task<LoadReservationAndDisplayCartResponse> GetCartInformation(string sessionId, Mobile.Model.MOBApplication application, string device, string cartId, string token, WorkFlowType workFlowType = WorkFlowType.InitialBooking)
        {
            LoadReservationAndDisplayCartRequest loadReservationAndDisplayCartRequest = new LoadReservationAndDisplayCartRequest();
            LoadReservationAndDisplayCartResponse loadReservationAndDisplayResponse = new LoadReservationAndDisplayCartResponse();
            loadReservationAndDisplayCartRequest.CartId = cartId;
            loadReservationAndDisplayCartRequest.WorkFlowType = (Services.FlightShopping.Common.FlightReservation.WorkFlowType)workFlowType;
            string jsonRequest = JsonConvert.SerializeObject(loadReservationAndDisplayCartRequest);
            loadReservationAndDisplayResponse = await _shoppingCartService.GetCartInformation<LoadReservationAndDisplayCartResponse>(token, "LoadReservationAndDisplayCart", jsonRequest, _headers.ContextValues.SessionId).ConfigureAwait(false);
            return loadReservationAndDisplayResponse;
        }

        public void AddGrandTotalIfNotExistInPrices(List<MOBSHOPPrice> prices)
        {
            var grandTotalPrice = prices.FirstOrDefault(p => p.DisplayType.ToUpper().Equals("GRAND TOTAL"));
            if (grandTotalPrice == null)
            {
                var totalPrice = prices.FirstOrDefault(p => p.DisplayType.ToUpper().Equals("TOTAL"));
                grandTotalPrice = ShopStaticUtility.BuildGrandTotalPriceForReservation(totalPrice.Value);
                if (_configuration.GetValue<bool>("EnableAdvanceSearchCouponBooking"))
                {
                    grandTotalPrice.PromoDetails = totalPrice.PromoDetails;
                }
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
        public bool isAFSCouponApplied(DisplayCart displayCart)
        {
            if (displayCart != null && displayCart.SpecialPricingInfo != null && displayCart.SpecialPricingInfo.MerchOfferCoupon != null && !string.IsNullOrEmpty(displayCart.SpecialPricingInfo.MerchOfferCoupon.PromoCode) && displayCart.SpecialPricingInfo.MerchOfferCoupon.IsCouponEligible.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        public async Task<TPIInfo> GetTPIDetails(Service.Presentation.ProductResponseModel.ProductOffer productOffer, string sessionId, bool isShoppingCall, bool isBookingPath = false, int appid = -1, string appVersion = "")
        {
            if (productOffer?.Offers == null || !(productOffer?.Offers?.Any() ?? false))
            {
                return null;
            }
            TPIInfo tripInsuranceInfo = new TPIInfo();
            var product = productOffer.Offers.FirstOrDefault(a => a.ProductInformation.ProductDetails.Where(b => b.Product != null && b.Product.Code.ToUpper().Trim() == "TPI").ToList().Count > 0).
                ProductInformation.ProductDetails.FirstOrDefault(c => c.Product != null && c.Product.Code.ToUpper().Trim() == "TPI").Product;
            #region // sample code If AIG Dont Offer TPI, the contents and Prices should be null. 
            if (product.SubProducts[0].Prices == null || product.Presentation == null || product.Presentation.Contents == null)
            {
                tripInsuranceInfo = null;
                return tripInsuranceInfo;
            }
            #endregion

            #region Trip Insurance V2
            var session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);
            if (session != null &&
                await _featureToggles.IsEnabledTripInsuranceV2(appid, appVersion, session.CatalogItems).ConfigureAwait(false) &&
                isBookingPath)
            {
                if (product.Presentation.HTML == null)
                {
                    tripInsuranceInfo = null;
                    return tripInsuranceInfo;
                }
                tripInsuranceInfo = await GetTPIInfoFromContentV2(product.Presentation, tripInsuranceInfo, sessionId, isShoppingCall, isBookingPath).ConfigureAwait(false);
            }
            #endregion
            else
            {
                #region mapping content
                tripInsuranceInfo = await GetTPIInfoFromContent(product.Presentation.Contents, tripInsuranceInfo, sessionId, isShoppingCall, isBookingPath, appid).ConfigureAwait(false);
                #endregion
            }

            if (tripInsuranceInfo != null)
            {
                tripInsuranceInfo.ProductId = GetTPIQuoteId(product.Characteristics);
                // if quoteId is null, we should keep reponse null
                if (!string.IsNullOrEmpty(tripInsuranceInfo.ProductId))
                {
                    tripInsuranceInfo = GetTPIAmountAndFormattedAmount(tripInsuranceInfo, product.SubProducts);
                }
                tripInsuranceInfo.ProductCode = product.Code;
                tripInsuranceInfo.ProductName = product.DisplayName;
            }

            return tripInsuranceInfo;
        }
        private TPIInfo GetTPIAmountAndFormattedAmount(TPIInfo tripInsuranceInfo, System.Collections.ObjectModel.Collection<Service.Presentation.ProductModel.SubProduct> subProducts)
        {
            string currencyCode = string.Empty;
            var prices = subProducts.Where(a => a.Prices != null && a.Prices.Count > 0).FirstOrDefault().Prices;
            foreach (var price in prices)
            {
                if (price != null && price.PaymentOptions != null && price.PaymentOptions.Count > 0)
                {
                    foreach (var option in price.PaymentOptions)
                    {
                        if (option != null && option.Type != null && option.Type.ToUpper().Contains("TOTALPRICE"))
                        {
                            foreach (var PriceComponent in option.PriceComponents)
                            {
                                foreach (var total in PriceComponent.Price.Totals)
                                {
                                    tripInsuranceInfo.Amount = total.Amount;
                                    currencyCode = total.Currency.Code.ToUpper().Trim();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            // concate currency sign and round up amount //removed
            //tripInsuranceInfo.FormattedDisplayAmount = AttachCurrencySymbol(tripInsuranceInfo.Amount, currencyCode, true);
            // real amount concat with currency sign
            tripInsuranceInfo.DisplayAmount = AttachCurrencySymbol(tripInsuranceInfo.Amount, currencyCode, false);
            if (tripInsuranceInfo.Amount <= 0)
            {
                tripInsuranceInfo = null;
            }
            return tripInsuranceInfo;
        }

        public List<ShopBundleEplus> GetTravelOptionEplusAncillary(Services.FlightShopping.Common.DisplayCart.SubitemsCollection subitemsCollection, List<ShopBundleEplus> bundlecode)
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

        public void GetTravelOptionAncillaryDescription(Services.FlightShopping.Common.DisplayCart.SubitemsCollection subitemsCollection, Mobile.Model.Shopping.TravelOption travelOption, Services.FlightShopping.Common.DisplayCart.DisplayCart displayCart)
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
        private string AttachCurrencySymbol(double amount, string currencyCode, bool isRoundUp)
        {
            string formattedDisplayAmount = string.Empty;
            CultureInfo ci = TopHelper.GetCultureInfo(currencyCode);
            formattedDisplayAmount = TopHelper.FormatAmountForDisplay(string.Format("{0:#,0.00}", amount), ci, isRoundUp);
            return formattedDisplayAmount;
        }
        private string GetTPIQuoteId(System.Collections.ObjectModel.Collection<Service.Presentation.CommonModel.Characteristic> characteristics)
        {
            string productId = string.Empty;
            productId = characteristics.FirstOrDefault(a => !string.IsNullOrEmpty(a.Code) && a.Code.ToUpper().Trim() == "QUOTEPACKID").Value.Trim();
            return productId;
        }

        private async Task<TPIInfo> GetTPIInfoFromContent(System.Collections.ObjectModel.Collection<United.Service.Presentation.ProductModel.PresentationContent> contents, TPIInfo tripInsuranceInfo, string sessionId, bool isShoppingCall, bool isBookingPath = false, int appid = -1)
        {
            string tncPaymentText1 = string.Empty;
            string tncPaymentText2 = string.Empty;
            string tncPaymentText3 = string.Empty;
            string tncPaymentLinkMessage = string.Empty;
            string tncProductPageText1 = string.Empty;
            string tncProductPageText2 = string.Empty;
            string tncProductPageLinkMessage = string.Empty;
            string confirmationResponseDetailMessage1 = string.Empty;
            string confirmationResponseDetailMessage2 = string.Empty;
            string contentInBooking1 = string.Empty;
            string contentInBooking2 = string.Empty;
            string contentInBooking3 = string.Empty;
            string header1 = string.Empty;
            string header2 = string.Empty;
            string legalInfo = string.Empty;
            string legalInfoText = string.Empty;
            string bookingImg = string.Empty;
            string bookingTncContentMsg = string.Empty;
            string bookingTncLinkMsg = string.Empty;
            string bookingLegalInfoContentMsg = string.Empty;
            string mobTgiLimitationMessage = string.Empty;
            string mobTgiReadMessage = string.Empty;
            string mobTgiAndMessage = string.Empty;
            // Covid-19 Emergency WHO TPI content
            string mobTIMREmergencyMessage = string.Empty;
            string mobTIMREmergencyMessageUrltext = string.Empty;
            string mobTIMREmergencyMessagelinkUrl = string.Empty;
            string mobTIMBemergencyMessage = string.Empty;
            string mobTIMBemergencyMessageUrltext = string.Empty;
            string mobTIMBemergencyMessagelinkUrl = string.Empty;
            string mobTIMRWashingtonMessage = string.Empty;

            foreach (var content in contents)
            {
                switch (content.Header.ToUpper().Trim())
                {
                    case "MOBOFFERHEADERMESSAGE":
                        tripInsuranceInfo.Title1 = content.Body.Trim();
                        break;
                    case "MOBOFFERTITLEMESSAGE":
                        tripInsuranceInfo.QuoteTitle = content.Body.Trim();
                        break;
                    case "MOBTRIPCOVEREDPRICEMESSAGE":
                        tripInsuranceInfo.CoverCost = content.Body.Trim();
                        break;
                    case "MOBOFFERFROMTMESSAGE":
                        tripInsuranceInfo.Title2 = content.Body.Trim();
                        break;
                    case "MOBOFFERIMAGE":
                        tripInsuranceInfo.Image = content.Body.Trim();
                        break;
                    case "PREBOOKINGMOBOFFERHEADERMESSAGE":
                        tripInsuranceInfo.TileTitle1 = content.Body.Trim();
                        break;
                    case "PREBOOKINGMOBOFFERTITLEMESSAGE":
                        tripInsuranceInfo.TileQuoteTitle = content.Body.Trim();
                        break;
                    case "PREBOOKINGMOBOFFERFROMTMESSAGE":
                        tripInsuranceInfo.TileTitle2 = content.Body.Trim();
                        break;
                    case "PREBOOKINGMOBOFFERCTAMESSAGE":
                        tripInsuranceInfo.TileLinkText = content.Body.Trim();
                        break;
                    case "MOBTGIVIEWHEADERMESSAGE":
                        tripInsuranceInfo.Headline1 = content.Body.Trim();
                        break;
                    case "MOBTIDETAILSTOTALCOVERCOSTMESSAGE":
                        tripInsuranceInfo.LineItemText = content.Body.Trim();
                        break;
                    case "MOBPAYMENTTANDCHEADER1MESSAGE": //By clicking on purchase I acknowledge that I have read and understand the
                        tncPaymentText1 = content.Body.Trim();
                        break;
                    case "MOBPAYMENTTANDCURLHEADERMESSAGE": //Certificate of Insurance
                        tncPaymentText2 = content.Body.Trim();
                        break;
                    case "MOBPAYMENTTANDCURLHEADER2MESSAGE": //, and agree to the terms and conditions of the insurance coverage provided.
                        tncPaymentText3 = content.Body.Trim();
                        break;
                    case "MOBPAYMENTTANDCBODYURLMESSAGE":
                        tncPaymentLinkMessage = content.Body.Trim();
                        break;
                    case "MOBTIDETAILSTANDCHEADER1MESSAGE": // Coverage is offered by Travel Guard Group, Inc. and limitations will apply;
                        tncProductPageText1 = content.Body.Trim();
                        break;
                    case "MOBTIDETAILSTANDCURLHEADERMESSAGE": // view details.
                        tncProductPageText2 = content.Body.Trim();
                        break;
                    case "MOBTIDETAILSTANDCURLMESSAGE":
                        tncProductPageLinkMessage = content.Body.Trim();
                        break;
                    case "MOBTGIVIEWBODY1MESSAGE": // Are you prepared?
                        tripInsuranceInfo.Body1 = content.Body.Trim();
                        break;
                    case "MOBTGIVIEWBODY2MESSAGE": // For millions of travelers every year...
                        tripInsuranceInfo.Body2 = content.Body.Trim();
                        break;
                    // used in payment confirmation page 
                    case "MOBTICONFIRMATIONBODY1MESSAGE":
                        confirmationResponseDetailMessage1 = content.Body.Trim();
                        break;
                    case "MOBTICONFIRMATIONBODY2MESSAGE":
                        confirmationResponseDetailMessage2 = content.Body.Trim();
                        break;
                    // used in booking path 
                    case "PREBOOKINGMOBOFFERIMAGE":
                        bookingImg = content.Body.Trim();
                        tripInsuranceInfo.TileImage = bookingImg;
                        break;
                    case "PREBOOKINGMOBTIDETAILSTANDCURLHEADERMESSAGE":
                        bookingTncContentMsg = content.Body.Trim();
                        break;
                    case "PREBOOKINGMOBTIDETAILSTANDCURLMESSAGE":
                        bookingTncLinkMsg = content.Body.Trim();
                        break;
                    case "PREBOOKINGMOBPAYMENTTANDCHEADER1MESSAGE":
                        bookingLegalInfoContentMsg = content.Body.Trim();
                        break;
                    case "PREBOOKINGMOBBOOKINGPRODPAGEHEADLINE1":
                        header1 = content.Body.Trim();
                        break;
                    case "PREBOOKINGMOBBOOKINGPRODPAGEHEADLINE2":
                        header2 = content.Body.Trim();
                        break;
                    case "PREBOOKINGMOBBOOKINGPRODPAGECONTENT1":
                        contentInBooking1 = content.Body.Trim();
                        break;
                    case "PREBOOKINGMOBBOOKINGPRODPAGECONTENT2":
                        contentInBooking2 = content.Body.Trim();
                        break;
                    case "PREBOOKINGMOBBOOKINGPRODPAGECONTENT3":
                        contentInBooking3 = content.Body.Trim();
                        break;
                    case "PREBOOKINGMOBBOOKINGPRODPAGEBOTTOMLINE":
                        legalInfo = content.Body.Trim();
                        break;
                    case "MOBTGILIMITATIONMESSAGE":
                        mobTgiLimitationMessage = content.Body.Trim();
                        break;
                    case "MOBTGIREADMESSAGE":
                        mobTgiReadMessage = content.Body.Trim();
                        break;
                    case "MOBTGIANDMESSAGE":
                        mobTgiAndMessage = content.Body.Trim();
                        break;
                    // Covid-19 Emergency WHO TPI content
                    case "MOBTIMREMERGENCYMESSAGETEXT":
                        mobTIMREmergencyMessage = content != null ? content.Body.Trim() : string.Empty;
                        break;
                    case "MOBTIMREMERGENCYMESSAGELINKTEXT":
                        mobTIMREmergencyMessageUrltext = content != null ? content.Body.Trim() : string.Empty;
                        break;
                    case "MOBTIMREMERGENCYMESSAGELINKURL":
                        mobTIMREmergencyMessagelinkUrl = content != null ? content.Body.Trim() : string.Empty;
                        break;
                    case "MOBTIMBEMERGENCYMESSAGETEXT":
                        mobTIMBemergencyMessage = content != null ? content.Body.Trim() : string.Empty;
                        break;
                    case "MOBTIMBEMERGENCYMESSAGELINKTEXT":
                        mobTIMBemergencyMessageUrltext = content != null ? content.Body.Trim() : string.Empty;
                        break;
                    case "MOBTIMBEMERGENCYMESSAGELINKURL":
                        mobTIMBemergencyMessagelinkUrl = content != null ? content.Body.Trim() : string.Empty;
                        break;
                    case "MOBTIMRWASHINGTONMESSAGE":
                        mobTIMRWashingtonMessage = content != null && !string.IsNullOrEmpty(content.Body) ? content.Body.Trim() : string.Empty;
                        break;
                    default:
                        break;
                }
            }

            //Covid-19 Emergency WHO TPI content
            if (_configuration.GetValue<bool>("ToggleCovidEmergencytextTPI") == true)
            {
                if (tripInsuranceInfo != null &&
                    !string.IsNullOrEmpty(mobTIMREmergencyMessage) && !string.IsNullOrEmpty(mobTIMREmergencyMessageUrltext)
                    && !string.IsNullOrEmpty(mobTIMREmergencyMessagelinkUrl))
                {
                    MOBItem tpiContentMessage = new MOBItem();
                    tpiContentMessage.Id = "COVID19EmergencyAlertManageRes";
                    tpiContentMessage.CurrentValue = mobTIMREmergencyMessage +
                        " <a href =\"" + mobTIMREmergencyMessagelinkUrl + "\" target=\"_blank\">" + mobTIMREmergencyMessageUrltext + "</a> ";
                    tripInsuranceInfo.TPIAIGReturnedMessageContentList = new List<MOBItem>();
                    tripInsuranceInfo.TPIAIGReturnedMessageContentList.Add(tpiContentMessage);
                }

                if (tripInsuranceInfo != null &&
                    !string.IsNullOrEmpty(mobTIMBemergencyMessage) && !string.IsNullOrEmpty(mobTIMBemergencyMessageUrltext)
                    && !string.IsNullOrEmpty(mobTIMBemergencyMessagelinkUrl))
                {
                    MOBItem tpiContentMessage = new MOBItem();
                    tpiContentMessage.Id = "COVID19EmergencyAlert";
                    tpiContentMessage.CurrentValue = mobTIMBemergencyMessage +
                        " <a href =\"" + mobTIMBemergencyMessagelinkUrl + "\" target=\"_blank\">" + mobTIMBemergencyMessageUrltext + "</a> ";
                    tripInsuranceInfo.TPIAIGReturnedMessageContentList = new List<MOBItem>();
                    tripInsuranceInfo.TPIAIGReturnedMessageContentList.Add(tpiContentMessage);
                }
            }

            string isNewTPIMessageHTML = appid == 2 ? "<br/><br/>" : "<br/>";
            string specialCharacter = _configuration.GetValue<string>("TPIinfo-SpecialCharacter") ?? "";
            //string specialCharacter = "®";
            if (tripInsuranceInfo != null && !string.IsNullOrEmpty(tripInsuranceInfo.Image) && !string.IsNullOrEmpty(tncProductPageLinkMessage) &&
                !string.IsNullOrEmpty(tncPaymentLinkMessage) &&
                tripInsuranceInfo.QuoteTitle != null && tripInsuranceInfo.QuoteTitle.Contains("(R)") && !isBookingPath)
            {
                tripInsuranceInfo.Body3 = (mobTgiLimitationMessage.IsNullOrEmpty() && mobTgiReadMessage.IsNullOrEmpty()) ?
                                          (tncProductPageText1 + " <a href =\"" + tncProductPageLinkMessage + "\" target=\"_blank\">"
                                          + tncProductPageText2 + "</a>")
                                          : (tncProductPageText1 + " <a href =\"" + tncPaymentLinkMessage + "\" target=\"_blank\">"
                                          + mobTgiLimitationMessage + "</a> " + mobTgiReadMessage + " <a href =\""
                                          + tncProductPageLinkMessage + "\" target=\"_blank\">" + tncProductPageText2 + "</a>");
                tripInsuranceInfo.Body3 = !_configuration.GetValue<bool>("IsDisableTPIForWashington") &&
                    !string.IsNullOrEmpty(tripInsuranceInfo.Body3) && !string.IsNullOrEmpty(mobTIMRWashingtonMessage)
                    ? tripInsuranceInfo.Body3 + isNewTPIMessageHTML + mobTIMRWashingtonMessage : tripInsuranceInfo.Body3;
                tripInsuranceInfo.TNC = tncPaymentText1 + tncPaymentText3 + " <a href =\"" + tncPaymentLinkMessage + "\" target=\"_blank\">" + tncPaymentText2 + "</a> " + mobTgiAndMessage +
                    " <a href =\"" + tncProductPageLinkMessage + "\" target=\"_blank\">" + tncProductPageText2 + "</a> ";
                tripInsuranceInfo.QuoteTitle = @tripInsuranceInfo.QuoteTitle.Replace("(R)", specialCharacter);
                tripInsuranceInfo.PageTitle = _configuration.GetValue<string>("TPIinfo-PageTitle") ?? "";
                //tripInsuranceInfo.PageTitle = "Travel Guard® Insurance";
                tripInsuranceInfo.Headline2 = _configuration.GetValue<string>("TPIinfo-Headline2") ?? "";
                tripInsuranceInfo.PaymentContent = _configuration.GetValue<string>("TPIinfo-PaymentContent") ?? "";
                // confirmation page use
                if (isShoppingCall)
                {
                    Reservation bookingPathReservation = new Reservation();
                    bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionId, bookingPathReservation.ObjectName, new List<string> { sessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                    if (bookingPathReservation == null)
                    {
                        bookingPathReservation = new Reservation();
                    }
                    if (bookingPathReservation.TripInsuranceFile == null)
                    {
                        bookingPathReservation.TripInsuranceFile = new TripInsuranceFile() { };
                    }
                    bookingPathReservation.TripInsuranceFile.ConfirmationResponseDetailMessage1 = @confirmationResponseDetailMessage1.Replace("(R)", specialCharacter);
                    bookingPathReservation.TripInsuranceFile.ConfirmationResponseDetailMessage2 = confirmationResponseDetailMessage2;
                    await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                }
                else
                {
                    tripInsuranceInfo.Confirmation1 = @confirmationResponseDetailMessage1.Replace("(R)", specialCharacter);
                    tripInsuranceInfo.Confirmation2 = confirmationResponseDetailMessage2;
                }
            }
            else if (isShoppingCall && isBookingPath && !string.IsNullOrEmpty(contentInBooking1) && !string.IsNullOrEmpty(contentInBooking2) && !string.IsNullOrEmpty(contentInBooking3)
                        && !string.IsNullOrEmpty(header1) && !string.IsNullOrEmpty(header2) && !string.IsNullOrEmpty(tncProductPageText1)
                        && !string.IsNullOrEmpty(bookingTncLinkMsg) && !string.IsNullOrEmpty(bookingTncContentMsg) && !string.IsNullOrEmpty(bookingLegalInfoContentMsg)
                        && !string.IsNullOrEmpty(tncPaymentLinkMessage) && !string.IsNullOrEmpty(tncPaymentText2) && !string.IsNullOrEmpty(tncPaymentText3) && !string.IsNullOrEmpty(bookingImg)
                        && !string.IsNullOrEmpty(tripInsuranceInfo.CoverCost) && !string.IsNullOrEmpty(tncPaymentText1)
                        && !string.IsNullOrEmpty(confirmationResponseDetailMessage1)
                        && !string.IsNullOrEmpty(confirmationResponseDetailMessage2))
            {
                tripInsuranceInfo.Body3 = (mobTgiLimitationMessage.IsNullOrEmpty() && mobTgiReadMessage.IsNullOrEmpty()) ?
                                           tncProductPageText1 + " <a href =\"" + bookingTncLinkMsg + "\" target=\"_blank\">" + bookingTncContentMsg + "</a>"
                                         : (tncProductPageText1 + " <a href =\"" + tncPaymentLinkMessage + "\" target=\"_blank\">" + mobTgiLimitationMessage + "</a> " + mobTgiReadMessage + " <a href =\"" + bookingTncLinkMsg + "\" target=\"_blank\">" + bookingTncContentMsg + "</a>");
                tripInsuranceInfo.Body3 = !_configuration.GetValue<bool>("IsDisableTPIForWashington") && !string.IsNullOrEmpty(tripInsuranceInfo.Body3) && !string.IsNullOrEmpty(mobTIMRWashingtonMessage) ? tripInsuranceInfo.Body3 + isNewTPIMessageHTML + mobTIMRWashingtonMessage : tripInsuranceInfo.Body3;
                tripInsuranceInfo.TNC = (mobTgiLimitationMessage.IsNullOrEmpty() && mobTgiReadMessage.IsNullOrEmpty()) ? bookingLegalInfoContentMsg + " <a href =\"" + tncPaymentLinkMessage + "\" target=\"_blank\">" + tncPaymentText2 + "</a> " + tncPaymentText3
                                        : bookingLegalInfoContentMsg + " " + tncPaymentText3 + " <a href =\"" + tncPaymentLinkMessage + "\" target=\"_blank\">" + tncPaymentText2 + "</a> " + mobTgiAndMessage + " <a href =\"" + bookingTncLinkMsg + "\" target=\"_blank\">" + bookingTncContentMsg + "</a>";

                tripInsuranceInfo.PageTitle = _configuration.GetValue<string>("TPIinfo-PageTitle") ?? "";
                //tripInsuranceInfo.PageTitle = "Travel Guard® Insurance";
                tripInsuranceInfo.Image = bookingImg;
                Reservation bookingPathReservation = new Reservation();
                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionId, bookingPathReservation.ObjectName, new List<string> { sessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                if (bookingPathReservation == null)
                {
                    bookingPathReservation = new Reservation();
                }
                if (bookingPathReservation.TripInsuranceFile == null)
                {
                    bookingPathReservation.TripInsuranceFile = new TripInsuranceFile() { };
                }
                if (bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo == null)
                {
                    bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo = new TPIInfoInBookingPath() { };
                }
                List<string> contentInBooking = new List<string>();
                contentInBooking.Add(contentInBooking1);
                contentInBooking.Add(contentInBooking2);
                contentInBooking.Add(contentInBooking3);
                bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo.Content = contentInBooking;
                bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo.Header = header1 + " <b>" + header2 + "</b>";
                bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo.LegalInformation = legalInfo;
                bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo.TncSecondaryFOPPage = (mobTgiLimitationMessage.IsNullOrEmpty() && mobTgiReadMessage.IsNullOrEmpty()) ? tncPaymentText1 + " <a href =\"" + tncPaymentLinkMessage + "\" target=\"_blank\">" + tncPaymentText2 + "</a> " + tncPaymentText3
                       : tncPaymentText1 + " " + tncPaymentText3 + " <a href =\"" + tncPaymentLinkMessage + "\" target=\"_blank\">" + tncPaymentText2 + "</a> " + mobTgiAndMessage + " <a href =\"" + tncProductPageLinkMessage + "\" target=\"_blank\">" + tncProductPageText2 + "</a> ";
                bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo.ConfirmationMsg = @confirmationResponseDetailMessage1.Replace("(R)", specialCharacter) + "\n\n" + confirmationResponseDetailMessage2;
                await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
            }
            else
            {
                tripInsuranceInfo = null;
            }
            return tripInsuranceInfo;
        }

        private async Task<TPIInfo> GetTPIInfoFromContentV2(United.Service.Presentation.ProductModel.Presentation presentation, TPIInfo tripInsuranceInfo, string sessionId, bool isShoppingCall, bool isBookingPath = false)
        {
            string tncPaymentText1 = string.Empty;
            string tncPaymentText2 = string.Empty;
            string tncPaymentText3 = string.Empty;
            string tncPaymentLinkMessage = string.Empty;
            string tncProductPageText1 = string.Empty;
            string tncProductPageText2 = string.Empty;
            string tncProductPageLinkMessage = string.Empty;
            string confirmationResponseDetailMessage1 = string.Empty;
            string confirmationResponseDetailMessage2 = string.Empty;
            string legalInfoText = string.Empty;
            string bookingTncContentMsg = string.Empty;
            string bookingTncLinkMsg = string.Empty;
            string bookingLegalInfoContentMsg = string.Empty;
            string mobTgiLimitationMessage = string.Empty;
            string mobTgiReadMessage = string.Empty;
            string mobTgiAndMessage = string.Empty;
            // Covid-19 Emergency WHO TPI content
            string mobTIMBemergencyMessage = string.Empty;
            string mobTIMBemergencyMessageUrltext = string.Empty;
            string mobTIMBemergencyMessagelinkUrl = string.Empty;

            foreach (var content in presentation.Contents)
            {
                switch (content.Header.ToUpper().Trim())
                {
                    case "PREBOOKINGMOBOFFERTITLEMESSAGE":
                        tripInsuranceInfo.QuoteTitle = content.Body.Trim();
                        tripInsuranceInfo.TileQuoteTitle = content.Body.Trim();
                        break;
                    case "MOBPAYMENTTANDCHEADER1MESSAGE": //By clicking on purchase I acknowledge that I have read and understand the
                        tncPaymentText1 = content.Body.Trim();
                        break;
                    case "MOBPAYMENTTANDCURLHEADERMESSAGE": //Certificate of Insurance
                        tncPaymentText2 = content.Body.Trim();
                        break;
                    case "MOBPAYMENTTANDCURLHEADER2MESSAGE": //, and agree to the terms and conditions of the insurance coverage provided.
                        tncPaymentText3 = content.Body.Trim();
                        break;
                    case "MOBPAYMENTTANDCBODYURLMESSAGE":
                        tncPaymentLinkMessage = content.Body.Trim();
                        break;
                    case "MOBTIDETAILSTANDCHEADER1MESSAGE": // Coverage is offered by Travel Guard Group, Inc. and limitations will apply;
                        tncProductPageText1 = content.Body.Trim();
                        break;
                    case "MOBTIDETAILSTANDCURLHEADERMESSAGE": // view details.
                        tncProductPageText2 = content.Body.Trim();
                        break;
                    case "MOBTIDETAILSTANDCURLMESSAGE":
                        tncProductPageLinkMessage = content.Body.Trim();
                        break;
                    // used in payment confirmation page 
                    case "MOBTICONFIRMATIONBODY1MESSAGE":
                        confirmationResponseDetailMessage1 = content.Body.Trim();
                        break;
                    case "MOBTICONFIRMATIONBODY2MESSAGE":
                        confirmationResponseDetailMessage2 = content.Body.Trim();
                        break;
                    case "PREBOOKINGMOBTIDETAILSTANDCURLHEADERMESSAGE":
                        bookingTncContentMsg = content.Body.Trim();
                        break;
                    case "PREBOOKINGMOBTIDETAILSTANDCURLMESSAGE":
                        bookingTncLinkMsg = content.Body.Trim();
                        break;
                    case "PREBOOKINGMOBPAYMENTTANDCHEADER1MESSAGE":
                        bookingLegalInfoContentMsg = content.Body.Trim();
                        break;
                    case "MOBTGILIMITATIONMESSAGE":
                        mobTgiLimitationMessage = content.Body.Trim();
                        break;
                    case "MOBTGIREADMESSAGE":
                        mobTgiReadMessage = content.Body.Trim();
                        break;
                    case "MOBTGIANDMESSAGE":
                        mobTgiAndMessage = content.Body.Trim();
                        break;
                    case "MOBTIMBEMERGENCYMESSAGETEXT":
                        mobTIMBemergencyMessage = content != null ? content.Body.Trim() : string.Empty;
                        break;
                    case "MOBTIMBEMERGENCYMESSAGELINKTEXT":
                        mobTIMBemergencyMessageUrltext = content != null ? content.Body.Trim() : string.Empty;
                        break;
                    case "MOBTIMBEMERGENCYMESSAGELINKURL":
                        mobTIMBemergencyMessagelinkUrl = content != null ? content.Body.Trim() : string.Empty;
                        break;
                    default:
                        break;
                }
            }

            // Get the html content
            Regex remScript = new Regex(@"<script[^>]*>[\s\S]*?</script>");
            string output = remScript.Replace(presentation.HTML, "");
            tripInsuranceInfo.HtmlContentV2 = output;

            //Covid-19 Emergency WHO TPI content
            if (_configuration.GetValue<bool>("ToggleCovidEmergencytextTPI") == true)
            {
                if (tripInsuranceInfo != null &&
                    !string.IsNullOrEmpty(mobTIMBemergencyMessage) && !string.IsNullOrEmpty(mobTIMBemergencyMessageUrltext)
                    && !string.IsNullOrEmpty(mobTIMBemergencyMessagelinkUrl))
                {
                    MOBItem tpiContentMessage = new MOBItem();
                    tpiContentMessage.Id = "COVID19EmergencyAlert";
                    tpiContentMessage.CurrentValue = mobTIMBemergencyMessage +
                        " <a href =\"" + mobTIMBemergencyMessagelinkUrl + "\" target=\"_blank\">" + mobTIMBemergencyMessageUrltext + "</a> ";
                    tripInsuranceInfo.TPIAIGReturnedMessageContentList = new List<MOBItem>();
                    tripInsuranceInfo.TPIAIGReturnedMessageContentList.Add(tpiContentMessage);
                }
            }

            string specialCharacter = _configuration.GetValue<string>("TPIinfo-SpecialCharacter") ?? "";
            if (isShoppingCall && isBookingPath && !string.IsNullOrEmpty(tncProductPageText1) && !string.IsNullOrEmpty(tripInsuranceInfo.HtmlContentV2)
                        && !string.IsNullOrEmpty(bookingTncLinkMsg) && !string.IsNullOrEmpty(bookingTncContentMsg) && !string.IsNullOrEmpty(bookingLegalInfoContentMsg)
                        && !string.IsNullOrEmpty(tncPaymentLinkMessage) && !string.IsNullOrEmpty(tncPaymentText2) && !string.IsNullOrEmpty(tncPaymentText3)
                        && !string.IsNullOrEmpty(tncPaymentText1)
                        && !string.IsNullOrEmpty(confirmationResponseDetailMessage1)
                        && !string.IsNullOrEmpty(confirmationResponseDetailMessage2))
            {
                tripInsuranceInfo.TNC = (mobTgiLimitationMessage.IsNullOrEmpty() && mobTgiReadMessage.IsNullOrEmpty()) ? bookingLegalInfoContentMsg + " <a href =\"" + tncPaymentLinkMessage + "\" target=\"_blank\">" + tncPaymentText2 + "</a> " + tncPaymentText3
                                        : bookingLegalInfoContentMsg + " " + tncPaymentText3 + " <a href =\"" + tncPaymentLinkMessage + "\" target=\"_blank\">" + tncPaymentText2 + "</a> " + mobTgiAndMessage + " <a href =\"" + bookingTncLinkMsg + "\" target=\"_blank\">" + bookingTncContentMsg + "</a>";

                tripInsuranceInfo.PageTitle = _configuration.GetValue<string>("TPIinfo-PageTitle") ?? "";

                Reservation bookingPathReservation = new Reservation();
                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionId, bookingPathReservation.ObjectName, new List<string> { sessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                if (bookingPathReservation == null)
                {
                    bookingPathReservation = new Reservation();
                }
                if (bookingPathReservation.TripInsuranceFile == null)
                {
                    bookingPathReservation.TripInsuranceFile = new TripInsuranceFile() { };
                }
                if (bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo == null)
                {
                    bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo = new TPIInfoInBookingPath() { };
                }
                
                bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo.TncSecondaryFOPPage = (mobTgiLimitationMessage.IsNullOrEmpty() && mobTgiReadMessage.IsNullOrEmpty()) ? tncPaymentText1 + " <a href =\"" + tncPaymentLinkMessage + "\" target=\"_blank\">" + tncPaymentText2 + "</a> " + tncPaymentText3
                       : tncPaymentText1 + " " + tncPaymentText3 + " <a href =\"" + tncPaymentLinkMessage + "\" target=\"_blank\">" + tncPaymentText2 + "</a> " + mobTgiAndMessage + " <a href =\"" + tncProductPageLinkMessage + "\" target=\"_blank\">" + tncProductPageText2 + "</a> ";
                bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo.ConfirmationMsg = @confirmationResponseDetailMessage1.Replace("(R)", specialCharacter) + "\n\n" + confirmationResponseDetailMessage2;
                bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo.HtmlContentV2 = tripInsuranceInfo.HtmlContentV2;

                await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
            }
            else
            {
                tripInsuranceInfo = null;
            }
            return tripInsuranceInfo;
        }
        public bool IsEnableUKChildrenTaxReprice(bool isReShop, int appid, string appversion)
        {
            if (!isReShop && EnableUKChildrenTaxReprice(appid, appversion))
            {
                return true;
            }
            return false;
        }
        private bool EnableUKChildrenTaxReprice(int appId, string appVersion)
        {
            // return GetBooleanConfigValue("EnableForceEPlus");
            return _configuration.GetValue<bool>("EnableUKChildrenTaxReprice")
           && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidiPhoneUKChildrenTaxRepriceVersion", "AndroidiPhoneUKChildrenTaxRepriceVersion", "", "", true, _configuration);
        }
        public string GetTypeCodeByAge(int age, bool iSChildInLap = false)
        {
            if ((18 <= age) && (age <= 64))
            {
                return TravelerTypeCode.Adult;
            }
            else
            if ((2 <= age) && (age < 5))
            {
                return TravelerTypeCode.child2To4;
            }
            else
            if ((5 <= age) && (age <= 11))
            {
                return TravelerTypeCode.child5To11;
            }
            else
            if ((12 <= age) && (age <= 14))
            {
                return TravelerTypeCode.child12To14;
            }
            else
            if ((15 <= age) && (age <= 17))
            {
                return TravelerTypeCode.child15To17;
            }
            else
            if (65 <= age)
            {
                return TravelerTypeCode.Senior;
            }
            else
                if (age < 2 && iSChildInLap)
            {
                return TravelerTypeCode.InfantInLap;
            }
            else if (age < 2)
                return TravelerTypeCode.InfantWithSeat;

            return TravelerTypeCode.Adult;
        }
        private class TravelerTypeCode
        {
            public const string Adult = "ADT";
            public const string InfantWithSeat = "INS";
            public const string child2To4 = "C04";
            public const string child5To11 = "C11";
            public const string child12To14 = "C14";
            public const string child15To17 = "C17";
            public const string Senior = "SNR";
            public const string InfantInLap = "INF";
        }
        public string GetPaxDescriptionByDOB(string date, string deptDateFLOF)
        {
            int age = GetAgeByDOB(date, deptDateFLOF);
            if ((18 <= age) && (age <= 64))
            {
                return "Adult (18-64)";
            }
            else
            if ((2 <= age) && (age < 5))
            {
                return "Child (2-4)";
            }
            else
            if ((5 <= age) && (age <= 11))
            {
                return "Child (5-11)";
            }
            else
            //if((12 <= age) && (age <= 17))
            //{

            //}
            if ((12 <= age) && (age <= 14))
            {
                return "Child (12-14)";
            }
            else
            if ((15 <= age) && (age <= 17))
            {
                return "Child (15-17)";
            }
            else
            if (65 <= age)
            {
                return "Senior (65+)";
            }
            else if (age < 2)
                return "Infant (under 2)";

            return string.Empty;
        }
        public bool EnableServicePlacePassBooking(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("PlacePassServiceTurnOnToggle_Booking")
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidPlacePassVersion", "iPhonePlacePassVersion", "", "", true, _configuration);
        }
        public async Task<PlacePass> GetGenericPlacePass(string destinationAiportCode, string tripType, string sessionId, int appID, string appVersion, string deviceId, string logAction, string utm_campain)
        {
            #region Load Macthed Place Pass from the Persist Place Passes List
            PlacePass matchedPlacePass = new PlacePass();
            try
            {
                #region
                int flag = 4;
                string utm_source = "utm_source=United";
                string utm_medium = "utm_medium=Web";
                string utm_campain1 = utm_campain;

                List<PlacePass> placePassListFromPersist = await _sessionHelperService.GetSession<List<PlacePass>>(_configuration.GetValue<string>("GetAllEligiblePlacePassesAndSaveToPersistStaticGUID"), ObjectNames.AllEligiblePlacePassesFullName, new List<string> { _configuration.GetValue<string>("GetAllEligiblePlacePassesAndSaveToPersistStaticGUID"), ObjectNames.AllEligiblePlacePassesFullName }).ConfigureAwait(false);
                if (placePassListFromPersist == null)
                {
                    placePassListFromPersist = await GetAllEligiblePlacePasses(sessionId, appID, appVersion, deviceId, logAction, true);
                }
                //if (levelSwitch.TraceInfo)
                //{
                //   // logEntries.Add(LogEntry.GetLogEntry<List<MOBPlacePass>>(sessionId, logAction, "MOBPlacepassResponseFromPersist", appID, appVersion, deviceId, placePassListFromPersist, true, false));
                //}
                string utm = utm_source + "&" + utm_medium + "&" + utm_campain1;
                foreach (PlacePass placePass in placePassListFromPersist)
                {
                    if (placePass.Destination.Trim().ToLower() == "genericplacepassurl")
                    {
                        placePass.PlacePassUrl = placePass.PlacePassUrl + "?" + utm;
                        matchedPlacePass = placePass;
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                //if (levelSwitch.TraceInfo)
                //{
                //    MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                //   // logEntries.Add(LogEntry.GetLogEntry(sessionId, logAction, "Exception", appID, appVersion, deviceId, exceptionWrapper));
                //}
                _logger.LogError("GetGenericPlacePass {@Exception}", JsonConvert.SerializeObject(ex));
                matchedPlacePass = new PlacePass();
            }
            #endregion Load Mached Place Pass from the Persist Place Passes List
            return matchedPlacePass;
        }
        public bool EnablePlacePassBooking(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("PlacePassTurnOnToggle_Booking")
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidPlacePassVersion", "iPhonePlacePassVersion", "", "", true, _configuration);
        }
        public async Task<PlacePass> GetEligiblityPlacePass(string destinationAiportCode, string tripType, string sessionId, int appID, string appVersion, string deviceId, string logAction)
        {
            #region Load Macthed Place Pass from the Persist Place Passes List
            PlacePass matchedPlacePass = new PlacePass();
            try
            {
                #region
                int flag = 2;
                List<PlacePass> placePassListFromPersist = await _sessionHelperService.GetSession<List<PlacePass>>(_configuration.GetValue<string>("GetAllEligiblePlacePassesAndSaveToPersistStaticGUID"), ObjectNames.AllEligiblePlacePassesFullName, new List<string> { _configuration.GetValue<string>("GetAllEligiblePlacePassesAndSaveToPersistStaticGUID"), ObjectNames.AllEligiblePlacePassesFullName }).ConfigureAwait(false);

                if (placePassListFromPersist == null)
                {
                    placePassListFromPersist = await GetAllEligiblePlacePasses(sessionId, appID, appVersion, deviceId, logAction, true);
                }
                //if (levelSwitch.TraceInfo)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<List<MOBPlacePass>>(sessionId, logAction, "MOBPlacepassResponseFromPersist", appID, appVersion, deviceId, placePassListFromPersist, true, false));
                //}
                if (!string.IsNullOrEmpty(destinationAiportCode) && !string.IsNullOrEmpty(tripType))
                {
                    switch (tripType.ToLower())
                    {
                        case "ow":
                        case "one_way":
                            flag = 1;
                            break;
                        case "rt":
                        case "round_trip":
                            flag = 1;
                            break;
                        case "md":
                        case "multi_city":
                            flag = 2;
                            break;
                    }

                    foreach (PlacePass placePass in placePassListFromPersist)
                    {
                        if (placePass.PlacePassID == 1)
                        {
                            matchedPlacePass = placePass;
                        }
                        if (placePass.Destination.Trim().ToUpper() == destinationAiportCode.Trim().ToUpper() && flag == 1)
                        {
                            matchedPlacePass = placePass;
                            break;
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                //if (levelSwitch.TraceInfo)
                //{
                //    MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                //  //  logEntries.Add(LogEntry.GetLogEntry(sessionId, logAction, "Exception", appID, appVersion, deviceId, exceptionWrapper));
                //}
                _logger.LogError("GetEligiblityPlacePass {@Exception}", JsonConvert.SerializeObject(ex));
                matchedPlacePass = new PlacePass();
            }
            #endregion Load Mached Place Pass from the Persist Place Passes List
            return matchedPlacePass;
        }
        private async Task<List<PlacePass>> GetAllEligiblePlacePasses(string sessionId, int appID, string appVersion, string deviceId, string logAction, bool saveToPersist)
        {
            List<PlacePass> placepasses = new List<PlacePass>();
            logAction = logAction == null ? "GetAllEligiblePlacePasses" : logAction + "- GetAllEligiblePlacePasses";

            var manageresDynamoDB = new ManageResDynamoDB(_configuration, _dynamoDBService);
            string destinationCode = "ALL";
            int flag = 3;
            var eligibleplace = await manageresDynamoDB.GetAllEligiblePlacePasses<List<PlacePass>>(destinationCode, flag, sessionId).ConfigureAwait(false);

            try
            {
                //while (var eligibleplace)
                //{
                //    MOBPlacePass placepass = new MOBPlacePass();
                //    placepass.PlacePassID = Convert.ToInt32(eligibleplace[0]);
                //    placepass.Destination = dataReader["DestinationCode"].ToString().Trim();
                //    placepass.PlacePassImageSrc = dataReader["PlacePassImageSrc"].ToString().Trim();
                //    placepass.OfferDescription = dataReader["CityDescription"].ToString().Trim();
                //    placepass.PlacePassUrl = dataReader["PlacePassUrl"].ToString().Trim();
                //    placepass.TxtPoweredBy = "Powered by";
                //    placepass.TxtPlacepass = "PLACEPASS";
                //    placepasses.Add(placepass);
                //}
                foreach (var place in eligibleplace)
                {
                    PlacePass placepass = new PlacePass();
                    placepass.PlacePassID = Convert.ToInt32(place.PlacePassID);
                    placepass.Destination = place.Destination;
                    placepass.PlacePassImageSrc = place.PlacePassImageSrc;
                    placepass.OfferDescription = place.OfferDescription;
                    placepass.PlacePassUrl = place.PlacePassUrl;
                    placepass.TxtPoweredBy = "Powered by"; ;
                    placepass.PlacePassUrl = "PLACEPASS";
                    placepasses.Add(placepass);
                }

                if (saveToPersist && placepasses != null && placepasses.Count > 1)
                {
                    await _sessionHelperService.SaveSession<List<PlacePass>>(placepasses, _configuration.GetValue<string>("GetAllEligiblePlacePassesAndSaveToPersistStaticGUID"), new List<string> { _configuration.GetValue<string>("GetAllEligiblePlacePassesAndSaveToPersistStaticGUID"), ObjectNames.AllEligiblePlacePassesFullName }, ObjectNames.AllEligiblePlacePassesFullName).ConfigureAwait(false);
                }

            }
            catch (Exception ex)
            {
                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                _logger.LogError("GetAllEligiblePlacePasses {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));
                //logEntries.Add(LogEntry.GetLogEntry(sessionId, logAction, "Exception", appID, appVersion, deviceId, exceptionWrapper));
            }
            return placepasses;
        }
        public void RemoveelfMessagesForRTI(ref Reservation bookingPathReservation)
        {
            if (_configuration.GetValue<bool>("ByPassSetUpUpgradedFromELFMessages"))
            {
                if (bookingPathReservation.FareLock != null && bookingPathReservation.FareLock.FareLockProducts != null && bookingPathReservation.FareLock.FareLockProducts.Count > 0
                     && (bookingPathReservation?.ShopReservationInfo2?.InfoWarningMessages?.Exists(m => m.Messages != null && m.Messages.Count > 0 && m.Messages[0].Equals(_configuration.GetValue<string>("UpgradedFromElfText"))) ?? false))
                {
                    bookingPathReservation?.ShopReservationInfo2?.InfoWarningMessages?.RemoveAll(m => m.Messages[0].Equals(_configuration.GetValue<string>("UpgradedFromElfText")));
                }
            }
            else
            {
                if (bookingPathReservation.FareLock != null && bookingPathReservation.FareLock.FareLockProducts != null && bookingPathReservation.FareLock.FareLockProducts.Count > 0 &&
                    bookingPathReservation.ELFMessagesForRTI != null && bookingPathReservation.ELFMessagesForRTI.Exists(elf => elf.Id.Contains("UpgradedFromElf")))
                {
                    bookingPathReservation.ELFMessagesForRTI.RemoveAll(elf => elf.Id.Contains("UpgradedFromElf"));
                }
            }
        }
        public bool EnableConcurrCardPolicy(bool isReshop = false)
        {
            return _configuration.GetValue<bool>("EnableConcurrCardPolicy") && !isReshop;
        }
        public InfoWarningMessages GetConcurrCardPolicyMessage(bool isPayment = false)
        {
            string message;
            if (isPayment)
            {
                message = _configuration.GetValue<string>("ConcurrCardPolicyPaymentMessage") as string ?? "Please review/ensure that the credit card being used to purchase meets your corporate policy/guidelines";
            }
            else
            {
                message = _configuration.GetValue<string>("ConcurrCardPolicyMessage") as string ?? "Please review/ensure that the credit card being used to purchase meets your corporate policy/guidelines";
            }
            return BuildConcurrCardPolicyMsg(message);
        }
        private InfoWarningMessages BuildConcurrCardPolicyMsg(string message)
        {
            var infoWarningMessage = new InfoWarningMessages
            {
                Order = MOBINFOWARNINGMESSAGEORDER.CONCURRCARDPOLICY.ToString(),
                IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString(),
                Messages = new List<string>
                {
                    message
                }
            };
            return infoWarningMessage;
        }
        public MOBCCAdStatement BuildChasePromo(string adType)
        {
            MOBCCAdStatement CCAdStatement = null;

            if (!adType.ToUpper().Equals(CHASEADTYPE.NONE.ToString()))
            {
                try
                {
                    Dictionary<string, string> imagePaths = GetImageUrl();
                    if (imagePaths.Count > 0)
                    {
                        string chaseDomainURL = _configuration.GetValue<string>("ChaseDomainNameURL");
                        CCAdStatement = new MOBCCAdStatement();

                        CCAdStatement.ccImage = string.Format("{0}/{1}", chaseDomainURL, imagePaths["ccImage"]);

                        CCAdStatement.bannerImage = new Image();

                        if (adType.ToUpper() == CHASEADTYPE.PREMIER.ToString())
                        {
                            CCAdStatement.bannerImage.PhoneUrl = string.Format("{0}/{1}", chaseDomainURL, imagePaths["PrimePhoneUrl"]);
                            CCAdStatement.bannerImage.TabletUrl = string.Format("{0}/{1}", chaseDomainURL, imagePaths["PrimeTabUrl"]);
                        }
                        else
                        {
                            CCAdStatement.bannerImage.PhoneUrl = string.Format("{0}/{1}", chaseDomainURL, imagePaths["NPrimePhoneUrl"]);
                            CCAdStatement.bannerImage.TabletUrl = string.Format("{0}/{1}", chaseDomainURL, imagePaths["NPrimeTabUrl"]);
                        }

                        CCAdStatement.statementCreditDisplayPrice = (Convert.ToDecimal(_configuration.GetValue<string>("ChaseStatementCredit"))).ToString("C2", CultureInfo.CurrentCulture);
                    }
                }
                catch (Exception e) { throw e; }
            }

            return CCAdStatement;
        }
        private Dictionary<string, string> GetImageUrl()
        {
            Dictionary<string, string> dicImageUrl = new Dictionary<string, string>();
            string[] urls = (_configuration.GetValue<string>("ChaseImages") ?? "").Split('^');
            foreach (string url in urls)
            {
                string[] imagepath = url.Split('|');
                dicImageUrl.Add(imagepath[0], imagepath[1]);
            }

            return dicImageUrl;
        }
        private async Task<List<MOBMobileCMSContentMessages>> GetTermsAndConditions()
        {
            var cmsContentMessages = new List<MOBMobileCMSContentMessages>();
            var docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles("PCU_TnC", _headers.ContextValues.SessionId,true).ConfigureAwait(false);
            if (docs != null && docs.Any())
            {
                foreach (var doc in docs)
                {
                    var cmsContentMessage = new MOBMobileCMSContentMessages();
                    cmsContentMessage.ContentFull = doc.LegalDocument;
                    cmsContentMessage.Title = doc.Title;
                    cmsContentMessages.Add(cmsContentMessage);
                }
            }
            return cmsContentMessages;
        }
        private async Task<List<MOBMobileCMSContentMessages>> GetTermsAndConditions(bool hasPremierAccelerator)
        {

            var dbKey = _configuration.GetValue<bool>("EnablePPRChangesForAAPA") ? hasPremierAccelerator ? "PPR_AAPA_TERMS_AND_CONDITIONS_AA_PA_MP"
                                              : "PPR_AAPA_TERMS_AND_CONDITIONS_AA_MP" : hasPremierAccelerator ? "AAPA_TERMS_AND_CONDITIONS_AA_PA_MP"
                                              : "AAPA_TERMS_AND_CONDITIONS_AA_MP";

            var docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles( dbKey , _headers.ContextValues.SessionId,true).ConfigureAwait(false);
            if (docs == null || !docs.Any())
                return null;

            var tncs = new List<MOBMobileCMSContentMessages>();
            foreach (var doc in docs)
            {
                var tnc = new MOBMobileCMSContentMessages
                {
                    Title = "Terms and conditions",
                    ContentFull = doc.LegalDocument,
                    ContentShort = _configuration.GetValue<string>("PaymentTnCMessage"),
                    HeadLine = doc.Title
                };
                tncs.Add(tnc);
            }

            return tncs;
        }

        private List<MOBTypeOption> GetPATermsAndConditionsList()
        {
            List<MOBTypeOption> tAndCList = new List<MOBTypeOption>();
            if (_configuration.GetValue<string>("PremierAccessTermsAndConditionsList") != null)
            {
                string premierAccessTermsAndConditionsList = _configuration.GetValue<string>("PremierAccessTermsAndConditionsList");
                foreach (string eachItem in premierAccessTermsAndConditionsList.Split('~'))
                {
                    tAndCList.Add(new MOBTypeOption(eachItem.Split('|')[0].ToString(), eachItem.Split('|')[1].ToString()));
                }
            }
            else
            {
                #region
                tAndCList.Add(new MOBTypeOption("paTandC1", "This Premier Access offer is nonrefundable and non-transferable"));
                tAndCList.Add(new MOBTypeOption("paTandC2", "Voluntary changes to your itinerary may forfeit your Premier Access purchase and \n any associated fees."));
                tAndCList.Add(new MOBTypeOption("paTandC3", "In the event of a flight cancellation or involuntary schedule change, we will refund \n the fees paid for the unused Premier Access product upon request."));
                tAndCList.Add(new MOBTypeOption("paTandC4", "Premier Access is offered only on flights operated by United and United Express."));
                tAndCList.Add(new MOBTypeOption("paTandC5", "This Premier Access offer is processed based on availability at time of purchase."));
                tAndCList.Add(new MOBTypeOption("paTandC6", "Premier Access does not guarantee wait time in airport check-in, boarding, or security lines. Premier Access does not exempt passengers from check-in time limits."));
                tAndCList.Add(new MOBTypeOption("paTandC7", "Premier Access benefits apply only to the customer who purchased Premier Access \n unless purchased for all customers on a reservation. Each travel companion must purchase Premier Access in order to receive benefits."));
                tAndCList.Add(new MOBTypeOption("paTandC8", "“Premier Access” must be printed or displayed on your boarding pass in order to \n receive benefits."));
                tAndCList.Add(new MOBTypeOption("paTandC9", "This offer is made at United's discretion and is subject to change or termination \n at any time with or without notice to the customer."));
                tAndCList.Add(new MOBTypeOption("paTandC10", "By clicking “I agree - Continue to purchase” you agree to all terms and conditions."));
                #endregion
            }
            return tAndCList;
        }
        private List<MOBTypeOption> GetPBContentList(string configValue)
        {
            List<MOBTypeOption> contentList = new List<MOBTypeOption>();
            if (_configuration.GetValue<string>(configValue) != null)
            {
                string pBContentList = _configuration.GetValue<string>(configValue);
                foreach (string eachItem in pBContentList.Split('~'))
                {
                    contentList.Add(new MOBTypeOption(eachItem.Split('|')[0].ToString(), eachItem.Split('|')[1].ToString()));
                }
            }
            return contentList;
        }
        public async Task<List<MOBMobileCMSContentMessages>> GetProductBasedTermAndConditions(string sessionId, United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, bool isPost)
        {
            var productCodes = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart.Items.Where(x => x.Product.FirstOrDefault().Code != "RES").Select(x => x.Product.FirstOrDefault().Code).ToList() :
                                        flightReservationResponse.ShoppingCart.Items.Where(x => x.Product.FirstOrDefault().Code != "RES").Select(x => x.Product.FirstOrDefault().Code).ToList();

            if (productCodes == null || !productCodes.Any())
                return null;

            List<MOBMobileCMSContentMessages> tNClist = new List<MOBMobileCMSContentMessages>();
            MOBMobileCMSContentMessages tNC = null;
            List<MOBTypeOption> typeOption = null;
            productCodes = OrderPCUTnC(productCodes);

            foreach (var productCode in productCodes)
            {
                if (isPost == false)
                {
                    switch (productCode)
                    {
                        case "PCU":
                            tNC = new MOBMobileCMSContentMessages();
                            List<MOBMobileCMSContentMessages> tncPCU = await GetTermsAndConditions();
                            tNC.Title = "Terms and conditions";
                            tNC.ContentShort = _configuration.GetValue<string>("PaymentTnCMessage");
                            tNC.ContentFull = tncPCU[0].ContentFull;
                            tNC.HeadLine = tncPCU[0].Title;
                            tNClist.Add(tNC);
                            break;

                        case "PAS":
                            tNC = new MOBMobileCMSContentMessages();
                            typeOption = new List<MOBTypeOption>();
                            typeOption = GetPATermsAndConditionsList();

                            tNC.Title = "Terms and conditions";
                            tNC.ContentShort = _configuration.GetValue<string>("PaymentTnCMessage");
                            tNC.ContentFull = string.Join("<br><br>", typeOption.Select(x => x.Value));
                            tNC.HeadLine = "Premier Access";
                            tNClist.Add(tNC);
                            break;

                        case "PBS":
                            tNC = new MOBMobileCMSContentMessages();
                            typeOption = new List<MOBTypeOption>();
                            typeOption = GetPBContentList("PriorityBoardingTermsAndConditionsList");

                            tNC.Title = "Terms and conditions";
                            tNC.ContentShort = _configuration.GetValue<string>("PaymentTnCMessage");
                            tNC.ContentFull = "<ul><li>" + string.Join("<br></li><li>", typeOption.Select(x => x.Value)) + "</li></ul>";
                            tNC.HeadLine = "Priority Boarding";
                            tNClist.Add(tNC);
                            break;

                        case "TPI":
                            var productVendorOffer = new GetVendorOffers();
                            productVendorOffer = await _sessionHelperService.GetSession<GetVendorOffers>(sessionId, productVendorOffer.ObjectName, new List<string> { sessionId, productVendorOffer.ObjectName }).ConfigureAwait(false);
                            if (productVendorOffer == null)
                                break;

                            tNC = new MOBMobileCMSContentMessages();
                            var product = productVendorOffer.Offers.FirstOrDefault(a => a.ProductInformation.ProductDetails.Where(b => b.Product != null && b.Product.Code.ToUpper().Trim() == "TPI").ToList().Count > 0).
                                ProductInformation.ProductDetails.FirstOrDefault(c => c.Product != null && c.Product.Code.ToUpper().Trim() == "TPI").Product;

                            //string tncTPIMessage1 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCHeader1Message").Select(x => x.Body).FirstOrDefault().ToString();
                            //string tncTPIMessage2 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCBodyUrlMessage").Select(x => x.Body).FirstOrDefault().ToString();
                            //string tncTPIMessage3 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCUrlHeaderMessage").Select(x => x.Body).FirstOrDefault().ToString();
                            //string tncTPIMessage4 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCUrlHeader2Message").Select(x => x.Body).FirstOrDefault().ToString();
                            //string tncTPI = tncTPIMessage1 + " <a href =\"" + tncTPIMessage2 + "\" target=\"_blank\">" + tncTPIMessage3 + "</a> " + tncTPIMessage4;
                            string tncTPI = string.Empty;
                            string tncTPIMessage1 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCHeader1Message").Select(x => x.Body).FirstOrDefault().ToString();
                            string tncTPIMessage2 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCBodyUrlMessage").Select(x => x.Body).FirstOrDefault().ToString();
                            string tncTPIMessage3 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCUrlHeaderMessage").Select(x => x.Body).FirstOrDefault().ToString();
                            string tncTPIMessage4 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCUrlHeader2Message").Select(x => x.Body).FirstOrDefault().ToString();
                            string tncTPIMessage5 = product.Presentation.Contents.Any(x => x.Header == "MobTIDetailsTAndCUrlMessage") ? product.Presentation.Contents.Where(x => x.Header == "MobTIDetailsTAndCUrlMessage").Select(x => x.Body).FirstOrDefault().ToString() : string.Empty;
                            string tncTPIMessage6 = product.Presentation.Contents.Any(x => x.Header == "MobTIDetailsTAndCUrlHeaderMessage") ? product.Presentation.Contents.Where(x => x.Header == "MobTIDetailsTAndCUrlHeaderMessage").Select(x => x.Body).FirstOrDefault().ToString() : string.Empty;
                            string tncTPIMessage7 = product.Presentation.Contents.Any(x => x.Header == "MobTGIAndMessage") ? product.Presentation.Contents.Where(x => x.Header == "MobTGIAndMessage").Select(x => x.Body).FirstOrDefault().ToString() : string.Empty;
                            if (string.IsNullOrEmpty(tncTPIMessage5) || string.IsNullOrEmpty(tncTPIMessage6) || string.IsNullOrEmpty(tncTPIMessage7))
                                tncTPI = tncTPIMessage1 + " <a href =\"" + tncTPIMessage2 + "\" target=\"_blank\">" + tncTPIMessage3 + "</a> " + tncTPIMessage4;
                            else
                                tncTPI = tncTPIMessage1 + " " + tncTPIMessage4 + " <a href =\"" + tncTPIMessage2 + "\" target=\"_blank\">" + tncTPIMessage3 + "</a> " + tncTPIMessage7 + " <a href =\"" + tncTPIMessage5 + "\" target=\"_blank\">" + tncTPIMessage6 + "</a> ";
                            tNC.Title = "Terms and conditions";
                            tNC.ContentShort = _configuration.GetValue<string>("PaymentTnCMessage");
                            tNC.ContentFull = tncTPI;
                            tNC.HeadLine = "Terms and conditions";
                            tNClist.Add(tNC);
                            break;
                        case "AAC":
                            var acceleratorTnCs = await GetTermsAndConditions(flightReservationResponse.DisplayCart.TravelOptions.Any(d => d.Key == "PAC"));
                            if (acceleratorTnCs != null && acceleratorTnCs.Any())
                            {
                                tNClist.AddRange(acceleratorTnCs);
                            }
                            break;
                        case "POM":
                            break;
                        case "SEATASSIGNMENTS":
                            if (string.IsNullOrEmpty(_configuration.GetValue<string>("EnablePCUSelectedSeatPurchaseViewRes")))
                                break;
                            var seatTypes = flightReservationResponse.DisplayCart.DisplaySeats.Where(s => s.SeatPrice > 0).Select(s => ShopStaticUtility.GetCommonSeatCode(s.SeatPromotionCode)).ToList();
                            var seatsTnCs = new List<MOBItem>();
                            if (seatTypes.Any() && seatTypes.Contains("ASA"))
                            {
                                var asaTncs = await GetCaptions("CFOP_UnitedTravelOptions_ASA_TnC");
                                if (asaTncs != null && asaTncs.Any())
                                {
                                    seatsTnCs.AddRange(asaTncs);
                                }
                            }
                            if (seatTypes.Any() && (seatTypes.Contains("EPU") || seatTypes.Contains("PSL")))
                            {
                                var eplusTncs = await GetCaptions("CFOP_UnitedTravelOptions_EPU_TnC");
                                if (eplusTncs != null && eplusTncs.Any())
                                {
                                    seatsTnCs.AddRange(eplusTncs);
                                }
                            }
                            if (seatTypes.Any() && seatTypes.Contains("PZA"))
                            {
                                var pzaTncs = await GetCaptions("CFOP_UnitedTravelOptions_PZA_TnC");
                                if (pzaTncs != null && pzaTncs.Any())
                                {
                                    seatsTnCs.AddRange(pzaTncs);
                                }
                            }

                            if (seatsTnCs.Any())
                            {
                                tNC = new MOBMobileCMSContentMessages
                                {
                                    Title = "Terms and conditions",
                                    ContentShort = _configuration.GetValue<string>("PaymentTnCMessage"),
                                    ContentFull = string.Join("<br>", seatsTnCs.Select(a => a.CurrentValue)),
                                    HeadLine = seatsTnCs[0].Id
                                };
                                tNClist.Add(tNC);
                            }
                            break;
                        case "BEB":
                            tNC = new United.Common.Helper.Merchandize.BasicEconomyBuyOut(sessionId, _configuration, _sessionHelperService).GetTermsAndConditions();
                            if (tNC != null)
                            {
                                tNClist.Add(tNC);
                            }
                            break;
                    }
                }
                else if (isPost == true)
                {
                    switch (productCode)
                    {
                        case "TPI":
                            var productVendorOffer = new GetVendorOffers();
                            productVendorOffer = await _sessionHelperService.GetSession<GetVendorOffers>(sessionId, productVendorOffer.ObjectName, new List<string> { sessionId, productVendorOffer.ObjectName }).ConfigureAwait(false);
                            if (productVendorOffer == null)
                                break;

                            string specialCharacter = _configuration.GetValue<string>("TPIinfo-SpecialCharacter") ?? "";
                            tNC = new MOBMobileCMSContentMessages();
                            var product = productVendorOffer.Offers.FirstOrDefault(a => a.ProductInformation.ProductDetails.Where(b => b.Product != null && b.Product.Code.ToUpper().Trim() == "TPI").ToList().Count > 0).
                                ProductInformation.ProductDetails.FirstOrDefault(c => c.Product != null && c.Product.Code.ToUpper().Trim() == "TPI").Product;

                            string tncTPIMessage1 = product.Presentation.Contents.Where(x => x.Header == "MobTIConfirmationBody1Message").Select(x => x.Body).FirstOrDefault().ToString().Replace("(R)", specialCharacter);
                            string tncTPIMessage2 = product.Presentation.Contents.Where(x => x.Header == "MobTIConfirmationBody2Message").Select(x => x.Body).FirstOrDefault().ToString();

                            string tncTPI = tncTPIMessage1 + "\n\n" + tncTPIMessage2;

                            tNC.Title = _configuration.GetValue<string>("TPIPurchaseResposne-ConfirmationResponseMessage") ?? ""; ;
                            tNC.ContentShort = _configuration.GetValue<string>("TPIPurchaseResposne-ConfirmationResponseEmailMessage"); // + ((flightReservationResponse.Reservation.EmailAddress.Count() > 0) ? flightReservationResponse.Reservation.EmailAddress.Where(x => x.Address != null).Select(x => x.Address).FirstOrDefault().ToString() : null) ?? "";
                            tNC.ContentFull = tncTPI;
                            tNClist.Add(tNC);
                            break;
                    }
                }
            }

            if (!isPost && IsBundleProductSelected(flightReservationResponse))
            {
                tNC = new MOBMobileCMSContentMessages
                {
                    Title = "Terms and conditions",
                    ContentShort = _configuration.GetValue<string>("PaymentTnCMessage"),
                    HeadLine = "Travel Options bundle terms and conditions",
                    ContentFull = await GetBundleTermsandConditons("bundlesTermsandConditons")

                };
                string strterms = tNC.ContentFull;
                tNC.ContentFull = strterms.Replace('?', '℠');
                tNClist.Add(tNC);
            }

            return tNClist;
        }
        private async Task<string> GetBundleTermsandConditons(string databaseKeys)
        {
            
            var docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles("bundlesTermsandConditons", _headers.ContextValues.SessionId,true).ConfigureAwait(false);

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

        private bool IsBundleProductSelected(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse)
        {
            if (!_configuration.GetValue<bool>("EnableTravelOptionsBundleInViewRes"))
                return false;

            return flightReservationResponse?.ShoppingCart?.Items?.Where(x => x.Product?.FirstOrDefault()?.Code != "RES")?.Any(x => x.Product?.Any(p => p?.SubProducts?.Any(sp => sp?.GroupCode == "BE") ?? false) ?? false) ?? false;
        }
        private async Task<List<MOBItem>> GetCaptions(string key)
        {
            return !string.IsNullOrEmpty(key) ? (await GetCaptions(key, true)) : null;
        }

        private async Task<List<MOBItem>> GetCaptions(List<string> keyList, bool isTnC)
        {
            var docs = await _documentLibraryDynamoDB.GetNewLegalDocumentsForTitles(keyList, _headers.ContextValues.SessionId).ConfigureAwait(false);
            if (docs == null || !docs.Any()) return null;

            var captions = new List<MOBItem>();

            captions.AddRange(
                docs.Select(doc => new MOBItem
                {
                    Id = doc.Title,
                    CurrentValue = doc.LegalDocument
                }));
            return captions;
        }
        private async Task<List<MOBItem>> GetCaptions(string keyList, bool isTnC)
        {

            var docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(keyList, _headers.ContextValues.TransactionId, true).ConfigureAwait(false);
            if (docs == null || !docs.Any()) return null;

            var captions = new List<MOBItem>();

            captions.AddRange(
                docs.Select(doc => new MOBItem
                {
                    Id = doc.Title,
                    CurrentValue = doc.LegalDocument
                }));
            return captions;
        }
        private List<string> OrderPCUTnC(List<string> productCodes)
        {
            if (productCodes == null || !productCodes.Any())
                return productCodes;

            return productCodes.OrderBy(p => GetProductOrderTnC()[GetProductTnCtoOrder(p)]).ToList();
        }
        private Dictionary<string, int> GetProductOrderTnC()
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) {
                    { "SEATASSIGNMENTS", 0 },
                    { "PCU", 1 },
                    { string.Empty, 2 } };
        }
        private string GetProductTnCtoOrder(string productCode)
        {
            productCode = string.IsNullOrEmpty(productCode) ? string.Empty : productCode.ToUpper().Trim();

            if (productCode == "SEATASSIGNMENTS" || productCode == "PCU")
                return productCode;

            return string.Empty;
        }
        public string GetBookingPaymentTargetForRegisterFop(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse)
        {
            if (flightReservationResponse.ShoppingCart == null || !flightReservationResponse.ShoppingCart.Items.Any())
                return string.Empty;

            return string.Join(",", flightReservationResponse.ShoppingCart.Items.SelectMany(x => x.Product).Select(x => x.Code).Distinct());
        }
        public string GetPaymentTargetForRegisterFop(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, bool isCompleteFarelockPurchase = false)
        {
            if (string.IsNullOrEmpty(_configuration.GetValue<string>("EnablePCUSelectedSeatPurchaseViewRes")))
                return string.Empty;

            if (isCompleteFarelockPurchase)
                return "RES";

            if (flightReservationResponse == null || flightReservationResponse.ShoppingCart == null || flightReservationResponse.ShoppingCart.Items == null)
                return string.Empty;

            var productCodes = flightReservationResponse.ShoppingCart.Items.Where(x => x.Product.FirstOrDefault().Code != "RES").Select(x => x.Product.FirstOrDefault().Code).ToList();
            if (productCodes == null || !productCodes.Any())
                return string.Empty;

            return string.Join(",", productCodes.Distinct());
        }
        public bool EnableTravelerTypes(int appId, string appVersion, bool reshop = false)
        {
            if (!string.IsNullOrEmpty(appVersion) && appId != -1)
            {
                return _configuration.GetValue<bool>("EnableTravelerTypes") && !reshop
               && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidTravelerTypesVersion", "iPhoneTravelerTypesVersion", "", "", true, _configuration);
            }
            return false;
        }
        public bool EnableReshopCubaTravelReasonVersion(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "iPhoneEnableReshopCubaTravelReasonVersion", "AndroidEnableReshopCubaTravelReasonVersion", "", "", true, _configuration);
        }
        public bool IsETCEnabledforMultiTraveler(int applicationId, string appVersion)
        {
            if (_configuration.GetValue<bool>("MTETCToggle") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableETCForMultiTraveler_AppVersion"), _configuration.GetValue<string>("iPhone_EnableETCForMultiTraveler_AppVersion")))
            {
                return true;
            }
            return false;
        }
        public bool IsETCCombinabilityEnabled(int applicationId, string appVersion)
        {
            if (_configuration.GetValue<bool>("CombinebilityETCToggle") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableETCCombinability_AppVersion"), _configuration.GetValue<string>("iPhone_EnableETCCombinability_AppVersion")))
            {
                return true;
            }

            return false;
        }

        public bool IncludeMoneyPlusMiles(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableMilesPlusMoney")
                && GeneralHelper.IsApplicationVersionGreater
                (appId, appVersion, "AndroidMilesPlusMoneyVersion", "iPhoneMilesPlusMoneyVersion", "", "", true, _configuration);
        }
        public bool EnableSpecialNeeds(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableSpecialNeeds")
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidEnableSpecialNeedsVersion", "iPhoneEnableSpecialNeedsVersion", "", "", true, _configuration);
        }

        public bool IncludeFFCResidual(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableFFCResidual")
                && GeneralHelper.IsApplicationVersionGreater
                (appId, appVersion, "AndroidFFCResidualVersion", "iPhoneFFCResidualVersion", "", "", true, _configuration);
        }

        public string GetAccessCode(string inputCountryCode)
        {
            if (!string.IsNullOrEmpty(inputCountryCode))
            {
                if (!ValidateCountryCode(inputCountryCode))
                {
                    return inputCountryCode;
                }
                var ap = Countries.Where(a => a[0].Trim().ToUpper().Equals(inputCountryCode.Trim().ToUpper()))
                         .Select(a => new { value = a[2] }).FirstOrDefault().value;
                if (!string.IsNullOrEmpty(ap) && ap.Length > 1)
                {
                    return ap;
                }
            }
            return inputCountryCode;
        }

        private bool ValidateCountryCode(string inputCountryCode)
        {

            if (!string.IsNullOrEmpty(inputCountryCode))
            {
                var ap = Countries.Where(a => a[0].Trim().ToUpper().Equals(inputCountryCode.Trim().ToUpper()))
                         .Select(a => new { value = a[0] });
                if (ap.Count() == 1)
                {
                    return true;
                }
            }
            return false;
        }

        public List<string[]> LoadCountries()
        {
            PATH_COUNTRIES_XML = _configuration.GetValue<string>("GetCountriesXMlPath") != null ? _configuration.GetValue<string>("GetCountriesXMlPath") : "";

            if (File.Exists(PATH_COUNTRIES_XML))
            {
                XElement data = XElement.Load(PATH_COUNTRIES_XML);
                Countries = data.Elements("COUNTRY").Select(a =>
                                           new string[]
                                           {
                                                a.Attribute("CODE").Value,
                                                a.Attribute("NAME").Value,
                                                a.Attribute("ACCESSCODE").Value,
                                           }).ToList();
            }
            return Countries;
        }

        public async Task<(FOPResponse, Reservation bookingPathReservation)> LoadBasicFOPResponse(Session session, Reservation bookingPathReservation)
        {
            var response = new FOPResponse();
            bookingPathReservation = new Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(session.SessionId, new Reservation().ObjectName, new List<string> { session.SessionId, new Reservation().ObjectName }).ConfigureAwait(false);
            response.Reservation = new MOBSHOPReservation(_configuration, _cachingService);
            response.Reservation = await _shoppingUtility.MakeReservationFromPersistReservation(response.Reservation, bookingPathReservation, session);

            var persistShoppingCart = new MOBShoppingCart();
            persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(session.SessionId, persistShoppingCart.ObjectName, new List<string> { session.SessionId, persistShoppingCart.ObjectName }).ConfigureAwait(false);
            response.ShoppingCart = persistShoppingCart;
            response.Profiles = await LoadPersistedProfile(session.SessionId, response.ShoppingCart?.Flow);
            if (response?.ShoppingCart?.FormofPaymentDetails == null)
            {
                response.ShoppingCart.FormofPaymentDetails = new MOBFormofPaymentDetails();
            }

            return (response,bookingPathReservation);
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
                taxsAndFees.Add(taxNfee);
            }
            return taxsAndFees;
        }

        public async Task<List<CPProfile>> LoadPersistedProfile(string sessionId, string flow)
        {
            switch (flow)
            {
                case "BOOKING":   //profile Object load
                    ProfileResponse persistedProfileResponse = new ProfileResponse();
                    persistedProfileResponse = await _sessionHelperService.GetSession<ProfileResponse>(sessionId, persistedProfileResponse.ObjectName, new List<string> { sessionId, persistedProfileResponse.ObjectName }).ConfigureAwait(false);
                    return persistedProfileResponse != null ? persistedProfileResponse.Response.Profiles : null;
                case "VIEWRES":
                    ProfileFOPCreditCardResponse profilePersist = new ProfileFOPCreditCardResponse();
                    profilePersist = await _sessionHelperService.GetSession<ProfileFOPCreditCardResponse>(sessionId, profilePersist.ObjectName, new List<string> { sessionId, profilePersist.ObjectName }).ConfigureAwait(false);
                    return profilePersist != null ? profilePersist.Response.Profiles : null;
                default: return null;
            }
        }
        public bool IncludeTravelCredit(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableTravelCredit") &&
                   GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidTravelCreditVersion", "iPhoneTravelCreditVersion", "", "", true, _configuration);
        }

        public bool IncludeTravelBankFOP(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableTravelBankFOP")
                && GeneralHelper.IsApplicationVersionGreater
                (appId, appVersion, "AndroidTravelBankFOPVersion", "iPhoneTravelBankFOPVersion", "", "", true, _configuration);
        }

        public bool EnableEPlusAncillary(int appID, string appVersion, bool isReshop = false)
        {
            return _configuration.GetValue<bool>("EnableEPlusAncillaryChanges") && !isReshop && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, _configuration.GetValue<string>("EplusAncillaryAndroidversion"), _configuration.GetValue<string>("EplusAncillaryiOSversion"));
        }

        public List<MOBSHOPPrice> UpdatePricesForBundles(MOBSHOPReservation reservation, Mobile.Model.Shopping.RegisterOfferRequest request, int appID, string appVersion, bool isReshop, string productId = "")
        {
            List<MOBSHOPPrice> prices = reservation.Prices.Clone();

            if (reservation.TravelOptions != null && reservation.TravelOptions.Count > 0)
            {
                foreach (var travelOption in reservation.TravelOptions)
                {
                    //below if condition modified by prasad for bundle checking
                    //MOB-4676-Added condition to ignore the trip insurance which is added as traveloption - sandeep/saikiran
                    if (!travelOption.Key.Equals("PAS") && (!travelOption.Type.IsNullOrEmpty() && !travelOption.Type.ToUpper().Equals("TRIP INSURANCE"))
                        && (EnableEPlusAncillary(appID, appVersion, isReshop) ? !travelOption.Key.Trim().ToUpper().Equals("FARELOCK") : true)
                        && !(_configuration.GetValue<bool>("EnableEplusCodeRefactor") && !string.IsNullOrEmpty(productId) && productId.Trim().ToUpper() != travelOption.Key.Trim().ToUpper()))
                    {
                        List<MOBSHOPPrice> totalPrices = new List<MOBSHOPPrice>();
                        bool totalExist = false;
                        double flightTotal = 0;

                        CultureInfo ci = null;

                        for (int i = 0; i < prices.Count; ++i)
                        {
                            if (ci == null)
                                ci = TopHelper.GetCultureInfo(prices[i].CurrencyCode);

                            if (prices[i].DisplayType.ToUpper() == "GRAND TOTAL")
                            {
                                totalExist = true;
                                prices[i].DisplayValue = string.Format("{0:#,0.00}", (Convert.ToDouble(prices[i].DisplayValue) + travelOption.Amount));
                                prices[i].FormattedDisplayValue = TopHelper.FormatAmountForDisplay(prices[i].DisplayValue, ci, false); // string.Format("{0:c}", prices[i].DisplayValue);
                                double tempDouble1 = 0;
                                double.TryParse(prices[i].DisplayValue.ToString(), out tempDouble1);
                                prices[i].Value = Math.Round(tempDouble1, 2, MidpointRounding.AwayFromZero);
                            }
                            if (prices[i].DisplayType.ToUpper() == "TOTAL")
                            {
                                flightTotal = Convert.ToDouble(prices[i].DisplayValue);
                            }
                        }
                        MOBSHOPPrice travelOptionPrice = new MOBSHOPPrice();
                        travelOptionPrice.CurrencyCode = travelOption.CurrencyCode;
                        travelOptionPrice.DisplayType = "Travel Options";
                        travelOptionPrice.DisplayValue = string.Format("{0:#,0.00}", travelOption.Amount.ToString());
                        travelOptionPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(travelOptionPrice.DisplayValue, ci, false); //Convert.ToDouble(travelOptionPrice.DisplayValue).ToString("C2", CultureInfo.CurrentCulture);

                        if (_configuration.GetValue<bool>("EnableEplusCodeRefactor") && travelOption.Key?.Trim().ToUpper() == "EFS")
                        {
                            travelOptionPrice.PriceType = "EFS";
                        }
                        else
                        {
                            travelOptionPrice.PriceType = "Travel Options";
                        }

                        double tmpDouble1 = 0;
                        double.TryParse(travelOptionPrice.DisplayValue.ToString(), out tmpDouble1);
                        travelOptionPrice.Value = Math.Round(tmpDouble1, 2, MidpointRounding.AwayFromZero);

                        prices.Add(travelOptionPrice);

                        if (!totalExist)
                        {
                            MOBSHOPPrice totalPrice = new MOBSHOPPrice();
                            totalPrice.CurrencyCode = travelOption.CurrencyCode;
                            totalPrice.DisplayType = "Grand Total";
                            totalPrice.DisplayValue = (flightTotal + travelOption.Amount).ToString("N2", CultureInfo.InvariantCulture);
                            totalPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalPrice.DisplayValue, ci, false); //string.Format("${0:c}", totalPrice.DisplayValue);
                            double tempDouble1 = 0;
                            double.TryParse(totalPrice.DisplayValue.ToString(), out tempDouble1);
                            totalPrice.Value = Math.Round(tempDouble1, 2, MidpointRounding.AwayFromZero);
                            totalPrice.PriceType = "Grand Total";
                            prices.Add(totalPrice);
                        }
                    }
                }
            }
            if (request != null && request.ClubPassPurchaseRequest != null)
            {
                List<MOBSHOPPrice> totalPrices = new List<MOBSHOPPrice>();
                bool totalExist = false;
                double flightTotal = 0;

                CultureInfo ci = null;

                for (int i = 0; i < prices.Count; ++i)
                {
                    if (ci == null)
                        ci = TopHelper.GetCultureInfo(prices[i].CurrencyCode);

                    if (prices[i].DisplayType.ToUpper() == "GRAND TOTAL")
                    {
                        totalExist = true;
                        prices[i].DisplayValue = string.Format("{0:#,0.00}", Convert.ToDouble(prices[i].DisplayValue) + request.ClubPassPurchaseRequest.AmountPaid);
                        prices[i].FormattedDisplayValue = Convert.ToDouble(prices[i].DisplayValue).ToString("C2", CultureInfo.CurrentCulture);
                        double tempDouble1 = 0;
                        double.TryParse(prices[i].DisplayValue.ToString(), out tempDouble1);
                        prices[i].Value = Math.Round(tempDouble1, 2, MidpointRounding.AwayFromZero);
                    }
                    if (prices[i].DisplayType.ToUpper() == "TOTAL")
                    {
                        flightTotal = Convert.ToDouble(prices[i].DisplayValue);
                    }
                }
                MOBSHOPPrice otpPrice = new MOBSHOPPrice();
                otpPrice.CurrencyCode = prices[prices.Count - 1].CurrencyCode;
                otpPrice.DisplayType = "One-time Pass";
                otpPrice.DisplayValue = string.Format("{0:#,0.00}", request.ClubPassPurchaseRequest.AmountPaid);
                double tempDouble = 0;
                double.TryParse(otpPrice.DisplayValue.ToString(), out tempDouble);
                otpPrice.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);
                otpPrice.FormattedDisplayValue = request.ClubPassPurchaseRequest.AmountPaid.ToString("C2", CultureInfo.CurrentCulture);
                otpPrice.PriceType = "One-time Pass";
                if (totalExist)
                {
                    prices.Insert(prices.Count - 2, otpPrice);
                }
                else
                {
                    prices.Add(otpPrice);
                }

                if (!totalExist)
                {
                    MOBSHOPPrice totalPrice = new MOBSHOPPrice();
                    totalPrice.CurrencyCode = prices[prices.Count - 1].CurrencyCode;
                    totalPrice.DisplayType = "Grand Total";
                    totalPrice.DisplayValue = (flightTotal + request.ClubPassPurchaseRequest.AmountPaid).ToString("N2", CultureInfo.InvariantCulture);
                    //totalPrice.DisplayValue = string.Format("{0:#,0.00}", (flightTotal + request.ClubPassPurchaseRequest.AmountPaid).ToString("{0:#,0.00}", CultureInfo.InvariantCulture);
                    totalPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalPrice.DisplayValue, ci, false); //string.Format("${0:c}", totalPrice.DisplayValue);
                    double tempDouble1 = 0;
                    double.TryParse(totalPrice.DisplayValue.ToString(), out tempDouble1);
                    totalPrice.Value = Math.Round(tempDouble1, 2, MidpointRounding.AwayFromZero);
                    totalPrice.PriceType = "Grand Total";
                    prices.Add(totalPrice);
                }
            }
            return prices;
        }
        public async Task<double> GetTravelBankBalance(string sessionId)
        {
            MOBCPTraveler mobCPTraveler = await GetProfileOwnerTravelerCSL(sessionId);
            return mobCPTraveler?.MileagePlus?.TravelBankBalance > 0.00 ? mobCPTraveler.MileagePlus.TravelBankBalance : 0.00;
        }
        public async Task<MOBCPTraveler> GetProfileOwnerTravelerCSL(string sessionID)
        {
            ProfileResponse profilePersist = new ProfileResponse();
            profilePersist = await GetCSLProfileResponseInSession(sessionID);
            if (profilePersist != null && profilePersist.Response != null && profilePersist.Response.Profiles != null)
            {
                foreach (MOBCPTraveler mobCPTraveler in profilePersist.Response.Profiles[0].Travelers)
                {
                    if (mobCPTraveler.IsProfileOwner)
                    {
                        return mobCPTraveler;
                    }
                }
            }
            return null;
        }

        //TravelCreditUtility
        public void AddFFCandFFCR(List<MOBCPTraveler> travelers, List<MOBFOPTravelCredit> travelCredits, FFCRCertificates ffcr, List<MOBMobileCMSContentMessages> lookUpMessages, bool isOtfFFC, bool islookUp)
        {
            if (ffcr != null)
            {
                Collection<FFCRCertificate> certificates = ffcr.CertificateList;
                if (certificates != null)
                {
                    if (travelCredits == null)
                        travelCredits = new List<MOBFOPTravelCredit>();
                    foreach (var item in certificates)
                    {
                        foreach (var travelCredit in item.TravelCreditList)
                        {
                            var item1 = ConvertToFFC(travelCredit, travelers, lookUpMessages, isOtfFFC, item.FirstName, item.LastName, ffcr.Errors, islookUp);
                            if (item1 != null)
                            {
                                if (!travelCredits.Exists(tc => tc.PinCode == item1.PinCode && tc.RecordLocator == item1.RecordLocator))
                                    travelCredits.Add(item1);
                            }

                        }
                    }
                }
            }
        }

        public void AddETCToTC(List<MOBFOPTravelCredit> travelCredits, ETCCertificates etc, bool islookUp)
        {
            var isEnbleTCNonMem = _configuration.GetValue<bool>("EnablePreLoadForTCNonMember");
            //Load ETC
            if (etc?.CertificateList != null && etc.CertificateList.Any())
            {
                foreach (var item in etc.CertificateList)
                {
                    if (isEnbleTCNonMem)
                    {
                        travelCredits.Add(new MOBFOPTravelCredit()
                        {
                            IsLookupCredit = islookUp,
                            CreditAmount = $"${string.Format("{0:0.00}", Math.Round(Convert.ToDouble(item.CurrentValue), 2, MidpointRounding.AwayFromZero))}",
                            CurrentValue = Math.Round(Convert.ToDouble(item.CurrentValue), 2, MidpointRounding.AwayFromZero),
                            DisplayNewValueAfterRedeem = $"${string.Format("{0:0.00}", Math.Round(Convert.ToDouble(item.CurrentValue), 2, MidpointRounding.AwayFromZero))}",
                            DisplayRedeemAmount = "0.00",
                            ExpiryDate = Convert.ToDateTime(item.CertExpDate).ToString("MMMM dd, yyyy"),
                            InitialValue = Math.Round(Convert.ToDouble(item.InitialValue), 2, MidpointRounding.AwayFromZero),
                            IsRemove = false,
                            IsEligibleToRedeem = true,
                            NewValueAfterRedeem = Math.Round(Convert.ToDouble(item.CurrentValue), 2, MidpointRounding.AwayFromZero),
                            PinCode = item.CertPin,
                            PromoCode = item.PromoID,
                            RecordLocator = item.PNR,
                            RedeemAmount = 0,
                            TravelCreditType = (MOBFOPTravelCredit.MOBTravelCreditFOP)MOBTravelCreditFOP.ETC,
                            YearIssued = Convert.ToDateTime(item.OrigIssueDate).ToString("MMMM dd, yyyy"),
                            Recipient = $"{item.FirstName} {item.LastName}",
                            IsHideShowDetails = false,
                            LastName=_configuration.GetValue<bool>("EnableMFOP") ? item.LastName : string.Empty,
                            FirstName= _configuration.GetValue<bool>("EnableMFOP") ? item.FirstName : string.Empty
                        });
                    }
                    else
                    {
                        travelCredits.Add(new MOBFOPTravelCredit()
                        {
                            IsLookupCredit = islookUp,
                            CreditAmount = $"${string.Format("{0:0.00}", Math.Round(Convert.ToDouble(item.CurrentValue), 2, MidpointRounding.AwayFromZero))}",
                            CurrentValue = Math.Round(Convert.ToDouble(item.CurrentValue), 2, MidpointRounding.AwayFromZero),
                            DisplayNewValueAfterRedeem = $"${string.Format("{0:0.00}", Math.Round(Convert.ToDouble(item.CurrentValue), 2, MidpointRounding.AwayFromZero))}",
                            DisplayRedeemAmount = "0.00",
                            ExpiryDate = Convert.ToDateTime(item.CertExpDate).ToString("MMMM dd, yyyy"),
                            InitialValue = Math.Round(Convert.ToDouble(item.InitialValue), 2, MidpointRounding.AwayFromZero),
                            IsRemove = false,
                            IsEligibleToRedeem = true,
                            NewValueAfterRedeem = Math.Round(Convert.ToDouble(item.CurrentValue), 2, MidpointRounding.AwayFromZero),
                            PinCode = item.CertPin,
                            PromoCode = item.PromoID,
                            RecordLocator = item.PNR,
                            RedeemAmount = 0,
                            TravelCreditType = (MOBFOPTravelCredit.MOBTravelCreditFOP)MOBTravelCreditFOP.ETC,
                            YearIssued = Convert.ToDateTime(item.OrigIssueDate).ToString("MMMM dd, yyyy"),
                            Recipient = $"{item.FirstName} {item.LastName}",
                            LastName = _configuration.GetValue<bool>("EnableMFOP") ? item.LastName : string.Empty,
                            FirstName = _configuration.GetValue<bool>("EnableMFOP") ? item.FirstName : string.Empty
                        });
                    }

                }
            }//End ETC
        }
        public MOBFOPTravelCredit ConvertToFFC(Service.Presentation.PaymentModel.TravelCredit cslTravelCredit,
                                                      List<MOBCPTraveler> travelers,
                                                      List<MOBMobileCMSContentMessages> lookUpMessages,
                                                      bool isOtfFFC,
                                                      string firstName,
                                                      string lastName,
                                                      Collection<TCError> errors,
                                                      bool islookUp)
        {
            MOBFOPTravelCredit travelCredit = null;
            try
            {
                travelCredit = new MOBFOPTravelCredit();
                string initialValue = cslTravelCredit.InitialValue;
                string origIssueDate = cslTravelCredit.OrigIssueDate;
                if (string.IsNullOrEmpty(initialValue))
                {
                    initialValue = cslTravelCredit.CurrentValue;
                }
                initialValue = initialValue.Trim('$');
                if (string.IsNullOrEmpty(origIssueDate))
                {
                    origIssueDate = cslTravelCredit.PNRCreateDate;
                }
                travelCredit.IsLookupCredit = islookUp;
                travelCredit.CreditAmount = $"${string.Format("{0:0.00}", Math.Round(Convert.ToDouble(cslTravelCredit.CurrentValue), 2, MidpointRounding.AwayFromZero))}";
                travelCredit.InitialValue = Convert.ToDouble(initialValue);
                travelCredit.CurrentValue = Convert.ToDouble(cslTravelCredit.CurrentValue);
                travelCredit.DisplayNewValueAfterRedeem = $"${string.Format("{0:0.00}", Math.Round(Convert.ToDouble(cslTravelCredit.CurrentValue), 2, MidpointRounding.AwayFromZero))}";
                travelCredit.DisplayRedeemAmount = "0.00";
                travelCredit.ExpiryDate = Convert.ToDateTime(cslTravelCredit.CertExpDate).ToString("MMMM dd, yyyy");
                travelCredit.IsApplied = false;
                travelCredit.IsRemove = false;
                travelCredit.NewValueAfterRedeem = Convert.ToDouble(cslTravelCredit.CurrentValue);
                travelCredit.PinCode = isOtfFFC ? cslTravelCredit.OrigTicketNumber : cslTravelCredit.CertPin;
                travelCredit.PromoCode = cslTravelCredit.PromoID;
                travelCredit.RecordLocator = cslTravelCredit.OrigPNR;
                travelCredit.RedeemAmount = 0;
                travelCredit.TravelCreditType = (MOBFOPTravelCredit.MOBTravelCreditFOP)MOBTravelCreditFOP.FFC;
                travelCredit.YearIssued = Convert.ToDateTime(origIssueDate).ToString("MMMM dd, yyyy");
                travelCredit.Recipient = $"{firstName} {lastName}";
                travelCredit.FirstName = firstName;
                travelCredit.LastName = lastName;
                travelCredit.IsOTFFFC = isOtfFFC;
                travelCredit.IsNameMatch = cslTravelCredit.Travellers.Exists(tcTraveler => Convert.ToBoolean(tcTraveler.IsNameMatch));
                travelCredit.IsNameMatchWaiverApplied = cslTravelCredit.Travellers.Exists(tcTraveler => Convert.ToBoolean(Convert.ToBoolean(tcTraveler.IsNameMatchWaiverApplied))
                                                                                                        && !Convert.ToBoolean(tcTraveler.IsNameMatch));
                if (_configuration.GetSection("EnableU4BCorporateBookingFFC").GetValue<bool>("isEnable"))
                {
                    travelCredit.IsCorporateTravelCreditText = String.Equals(cslTravelCredit.IsCorporateBooking, "true", StringComparison.OrdinalIgnoreCase) ? _configuration.GetSection("EnableU4BCorporateBookingFFC").GetValue<string>("u4BCorporateText") : String.Empty;
                }
                if (_configuration.GetValue<bool>("EnableU4BCorporateName"))
                {
                    travelCredit.CorporateName = cslTravelCredit?.CorporateName;
                }
                travelCredit.IsTravelDateBeginsBeforeCertExpiry = cslTravelCredit.Travellers.Exists(tcTraveler => Convert.ToBoolean(tcTraveler.IsTravelDateBeginsBeforeCertExpiry));
                travelCredit.IsEligibleToRedeem = cslTravelCredit.Travellers.Exists(tcTraveler => Convert.ToBoolean(tcTraveler.IsEligibleToRedeem));

                travelCredit.Captions = TravelCreditErrorMapping
                    (errors, lookUpMessages, cslTravelCredit);

                if (!_configuration.GetValue<bool>("EnableAwardOTF"))
                {
                    //var _errorMSG = new string[] { "Mnr22", "Mnr23" }; // List of error messages for Award OTF
                    travelCredit.IsAwardOTFEligible = errors.IsNullOrEmpty() ? false :
                        !errors.Where(t => string.Equals(t.RecordLocator, cslTravelCredit.OrigPNR, StringComparison.OrdinalIgnoreCase) && //PNR Check
                         ((string.Equals(t.MajorCode, "Msg1", StringComparison.OrdinalIgnoreCase) && string.Equals(t.MinorCode, "Mnr22", StringComparison.OrdinalIgnoreCase)) ||
                         (string.Equals(t.MajorCode, "Err2", StringComparison.OrdinalIgnoreCase) && string.Equals(t.MinorCode, "Mnr23", StringComparison.OrdinalIgnoreCase)))).IsNullOrEmpty();
                }

                if (!travelCredit.IsEligibleToRedeem
                    && (travelCredit.Captions == null || !travelCredit.Captions.Any()))
                {
                    travelCredit.Captions = SetErrorMessage2(lookUpMessages);
                }

                travelCredit.IsEligibleToRedeem
                    = (travelCredit.IsEligibleToRedeem && (travelCredit.Captions == null || !travelCredit.Captions.Any()));

                var csltravelers = cslTravelCredit.Travellers.Where(t => Convert.ToBoolean(t.IsEligibleToRedeem) ||
                                                                  Convert.ToBoolean(t.IsNameMatchWaiverApplied));
                if (csltravelers != null)
                {
                    travelCredit.EligibleTravelerNameIndex = new List<string>();
                    travelCredit.EligibleTravelers = new List<string>();
                    foreach (var csltraveler in csltravelers)
                    {
                        var traveler = travelers.FirstOrDefault(t => t.TravelerNameIndex == csltraveler?.PaxIndex);
                        travelCredit.TravelerNameIndex += traveler.TravelerNameIndex + ",";
                        travelCredit.EligibleTravelerNameIndex.Add(traveler.TravelerNameIndex);
                        if (!Convert.ToBoolean(csltraveler.IsNameMatchWaiverApplied))
                            travelCredit.EligibleTravelers.Add(traveler.TravelerNameIndex);
                    }
                }
                if (_configuration.GetValue<bool>("EnablePreLoadForTCNonMember"))
                    travelCredit.IsHideShowDetails = cslTravelCredit.IsTravelerCustomDataUsedToLookupCert.ToBoolean();

                if (_configuration.GetValue<bool>("EnableMFOP"))
                {
                    travelCredit.CertificateNumber = cslTravelCredit.CertificateNumber;
                    travelCredit.CsltravelCreditType = cslTravelCredit.TravelCreditType;
                }
            }
            catch { }

            return travelCredit;
        }
        public static List<MOBTypeOption> TravelCreditErrorMapping
           (Collection<TCError> errors, List<MOBMobileCMSContentMessages> lookUpMessages,
            Service.Presentation.PaymentModel.TravelCredit cslTravelCredit)
        {
            List<MOBTypeOption> mobcaptions = null;

            if (errors == null || !errors.Any()) return null;

            var selectedError = errors.FirstOrDefault
                (x => string.Equals(x.RecordLocator, cslTravelCredit.OrigPNR, StringComparison.OrdinalIgnoreCase));

            if (selectedError == null || string.IsNullOrEmpty(selectedError.MajorCode)) return null;

            mobcaptions = (mobcaptions == null) ? new List<MOBTypeOption>() : mobcaptions;


            if (string.Equals(selectedError.MajorCode, _MSG1, StringComparison.OrdinalIgnoreCase))
            {
                var errordata = lookUpMessages?.FirstOrDefault
                    (x => string.Equals(x.Title, "RTI.TravelCertificate.LookUpTravelCredits.ErrorMessage1", StringComparison.OrdinalIgnoreCase));

                if (errordata != null)
                {
                    mobcaptions = new List<MOBTypeOption> {
                            new MOBTypeOption{ Key = "NAVIGATE", Value = "MOBILE"},
                            new MOBTypeOption{ Key = "BUTTONTXT", Value = errordata.ContentShort },
                            new MOBTypeOption{ Key = "HEADERTXT", Value =  errordata.ContentFull },
                };
                }
            }
            else if (string.Equals(selectedError.MajorCode, _ERR2, StringComparison.OrdinalIgnoreCase))
            {
                mobcaptions = SetErrorMessage2(lookUpMessages);
            }
            else if (string.Equals(selectedError.MajorCode, _ERR3, StringComparison.OrdinalIgnoreCase))
            {
                var errordata = lookUpMessages?.FirstOrDefault
                  (x => string.Equals(x.Title, "RTI.TravelCertificate.LookUpTravelCredits.ErrorMessage3", StringComparison.OrdinalIgnoreCase));

                if (errordata != null)
                {
                    mobcaptions = new List<MOBTypeOption> {
                            new MOBTypeOption{ Key = "NAVIGATE", Value = "NONE"},
                            new MOBTypeOption{ Key = "HEADERTXT", Value = string.Format(errordata.ContentFull,cslTravelCredit.CertExpDate) },
                };
                }
            }
            else if (string.Equals(selectedError.MajorCode, _ERR4, StringComparison.OrdinalIgnoreCase))
            {
                var errordata = lookUpMessages?.FirstOrDefault
                  (x => string.Equals(x.Title, "RTI.TravelCertificate.LookUpTravelCredits.ErrorMessage4", StringComparison.OrdinalIgnoreCase));

                if (errordata != null)
                {
                    mobcaptions = new List<MOBTypeOption> {
                            new MOBTypeOption{ Key = "NAVIGATE", Value = "NONE"},
                            new MOBTypeOption{ Key = "HEADERTXT", Value = errordata.ContentFull },
                };
                }
            }
            else if (string.Equals(selectedError.MajorCode, _ERR5, StringComparison.OrdinalIgnoreCase))
            {
                var errordata = lookUpMessages?.FirstOrDefault
                  (x => string.Equals(x.Title, "RTI.TravelCertificate.LookUpTravelCredits.ErrorMessage5", StringComparison.OrdinalIgnoreCase));

                if (errordata != null)
                {
                    mobcaptions = new List<MOBTypeOption> {
                            new MOBTypeOption{ Key = "NAVIGATE", Value = "WEB"},
                            new MOBTypeOption{ Key = "URL", Value = errordata.ContentShort},
                            new MOBTypeOption{ Key = "BUTTONTXT", Value = errordata.HeadLine},
                            new MOBTypeOption{ Key = "HEADERTXT", Value = errordata.ContentFull },
                };
                }
            }
            return (mobcaptions == null || !mobcaptions.Any()) ? null : mobcaptions;
        }

        public static List<MOBTypeOption> SetErrorMessage2(List<MOBMobileCMSContentMessages> lookUpMessages)
        {
            var errordata = lookUpMessages?.FirstOrDefault
                   (x => string.Equals(x.Title, "RTI.TravelCertificate.LookUpTravelCredits.ErrorMessage2", StringComparison.OrdinalIgnoreCase));

            if (errordata != null)
            {
                return new List<MOBTypeOption> {
                            new MOBTypeOption{ Key = "NAVIGATE", Value = "WEB"},
                            new MOBTypeOption{ Key = "URL", Value = errordata.ContentShort},
                            new MOBTypeOption{ Key = "BUTTONTXT", Value = errordata.HeadLine},
                            new MOBTypeOption{ Key = "HEADERTXT", Value = errordata.ContentFull  },
                };
            }
            return null;
        }
        public void UpdateAllEligibleTravelersList(Reservation bookingPathReservation)
        {
            if (bookingPathReservation.ShopReservationInfo2 == null || bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL == null || bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count() == 0)
            {
                if (bookingPathReservation.ShopReservationInfo2 == null)
                    bookingPathReservation.ShopReservationInfo2 = new ReservationInfo2();
                if (bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL == null)
                    bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL = new List<MOBCPTraveler>();

                if (bookingPathReservation.TravelersCSL != null)
                {
                    foreach (var travelerkey in bookingPathReservation.TravelersCSL)
                    {
                        travelerkey.Value.IsPaxSelected = true;
                        bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Add(travelerkey.Value);
                    }
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

        public bool EnableChaseOfferRTI(int appID, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableChaseOfferRTI") && (!_configuration.GetValue<bool>("EnableChaseOfferRTIVersionCheck") ||
                (_configuration.GetValue<bool>("EnableChaseOfferRTIVersionCheck") && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, _configuration.GetValue<string>("AndroidEnableChaseOfferRTIVersion"), _configuration.GetValue<string>("iPhoneEnableChaseOfferRTIVersion"))));
        }

        public async Task<TPIInfoInBookingPath> GetBookingPathTPIInfo(string sessionId, string languageCode, MOBApplication application, string deviceId, string cartId, string token, bool isRequote, bool isRegisterTraveler, bool isReshop)
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
            Reservation bookingPathReservation = new Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionId, bookingPathReservation.ObjectName, new List<string> { sessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
            // if it is concur account with non valid fop for ghost card, we dont show TPI to them
            if (IsValidForTPI(bookingPathReservation))
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

                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionId, bookingPathReservation.ObjectName, new
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
                        var travelOptions = RegisterOffer(sessionId, cartId, cartKey, languageCode, getTripInsuranceRequest.PointOfSale, productCode, productId, productIds, subProductCode, delete, application.Id, deviceId, application.Version.Major, isReshop);
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
                                var travelOptions = RegisterOffer(sessionId, cartId, cartKey, languageCode, getTripInsuranceRequest.PointOfSale, productCode, productId, productIds, subProductCode, delete, application.Id, deviceId, application.Version.Major, isReshop);
                                productId = tPIInfo.QuoteId;
                                delete = false;
                                travelOptions = RegisterOffer(sessionId, cartId, cartKey, languageCode, getTripInsuranceRequest.PointOfSale, productCode, productId, productIds, subProductCode, delete, application.Id, deviceId, application.Version.Major, isReshop);
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

            await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);

            return tPIInfo;
        }
        private async Task<TPIInfo> GetTripInsuranceInfo(ProductSearchRequest request, string token, bool isBookingPath = false)
        {
            TPIInfo tripInsuranceInfo = new TPIInfo();
            bool isPostBookingCall = (_configuration.GetValue<bool>("ShowTripInsuranceSwitch"));
            // ==>> Venkat and Elise change only one below line of code to offer TPI postbooking when inline TPI is off for new clients 2.1.36 and above
            isPostBookingCall = EnableTPI(request.Application.Id, request.Application.Version.Major, 1);
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

                    string jsonResponse = await _getProductsService.GetProducts(token, _headers.ContextValues.SessionId, jsonRequest).ConfigureAwait(false);
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
                        // add presist file 
                        Reservation bookingPathReservation = new Reservation();
                        bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                        if (bookingPathReservation.TripInsuranceFile == null)
                        {
                            bookingPathReservation.TripInsuranceFile = new TripInsuranceFile() { };
                        }
                        bookingPathReservation.TripInsuranceFile.TripInsuranceInfo = tripInsuranceInfo;
                        await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, request.SessionId, new List<string> { request.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                    }
                }
                catch (System.Net.WebException wex)
                {
                    //var errorResponse = new StreamReader(wex.Response.GetResponseStream()).ReadToEnd();
                    _logger.LogError("GetTripInsuranceInfo in {@LogAction} - WebException {@WebException}", logAction, JsonConvert.SerializeObject(wex));
                }
                catch (System.Exception ex)
                {
                    tripInsuranceInfo = null;
                    MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                    _logger.LogError("GetTripInsuranceInfo in {@LogAction} - WebException {@Exception}", logAction, JsonConvert.SerializeObject(exceptionWrapper));
                }
            }
            return tripInsuranceInfo;
        }
        private async Task<TPIInfo> GetTripInsuranceDetails(Service.Presentation.ProductResponseModel.ProductOffer offer, int applicationId, string applicationVersion, string deviceID, string sessionId, bool isBookingPath = false)
        {
            TPIInfo tripInsuranceInfo = new TPIInfo();
            try
            {
                tripInsuranceInfo = await GetTPIDetails(offer, sessionId, true, isBookingPath, applicationId, applicationVersion);
            }
            catch (System.Exception ex)
            {
                tripInsuranceInfo = null;
                _logger.LogError("GetTripInsuranceInfo - Exception {@Exception}", JsonConvert.SerializeObject(ex));
            }
            if (tripInsuranceInfo == null && (_configuration.GetValue<bool>("Log_TI_Offer_If_AIG_NotOffered_At_BookingPath")))
            {
                var ex = "AIG Not Offered Trip Insuracne in Booking Path";
                _logger.LogWarning("GetTripInsuranceInfo - UnitedException {@UnitedException}", ex);
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
                // Session
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
            tPIInfo.Content = tripInsuranceBookingInfo.Content;
            tPIInfo.Header = tripInsuranceBookingInfo.Header;
            tPIInfo.LegalInformation = tripInsuranceBookingInfo.LegalInformation;
            tPIInfo.LegalInformationText = tripInsuranceInfo.TNC;
            tPIInfo.TncSecondaryFOPPage = tripInsuranceBookingInfo.TncSecondaryFOPPage;
            tPIInfo.DisplayAmount = tripInsuranceInfo.DisplayAmount;
            tPIInfo.ConfirmationMsg = tripInsuranceBookingInfo.ConfirmationMsg;
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
        private bool IsValidForTPI(Reservation bookingPathReservation)
        {
            bool isValid = true;
            if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo != null && bookingPathReservation.ShopReservationInfo.IsCorporateBooking && !bookingPathReservation.ShopReservationInfo.IsGhostCardValidForTPIPurchase)
            {
                isValid = false;
            }
            return isValid;
        }
        private async Task<List<Mobile.Model.Shopping.TravelOption>> RegisterOffer(string sessionId, string cartId, string cartKey, string languageCode, string pointOfSale, string productCode, string productId, List<string> productIds, string subProductCode, bool delete, int applicationId, string deviceId, string appVersion, bool isReshopChange)
        {
            List<Mobile.Model.Shopping.TravelOption> travelOptions = null;

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
                string action = "RegisterOffer";
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******
                string jsonResponse = await _getProductsService.RegisterOffer(session.Token, action, jsonRequest, sessionId).ConfigureAwait(false);
                #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******

                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******            
                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    FlightReservationResponse response = JsonConvert.DeserializeObject<FlightReservationResponse>(jsonResponse);

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
        public List<MOBSHOPPrice> UpdatePriceForUnRegisterTPI(List<MOBSHOPPrice> persistedPrices)
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
                        price.FormattedDisplayValue = string.Format("${0:c}", price.DisplayValue);//GeneralHelper.FormatCurrency(price.DisplayValue);
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
        public List<Mobile.Model.Shopping.TravelOption> GetTravelOptions(DisplayCart displayCart, bool isReshop, int appID, string appVersion)
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
                    if (!anOption.Type.Equals("Premium Access") && !anOption.Key.Trim().ToUpper().Contains("FARELOCK") && !(addTripInsuranceInTravelOption && anOption.Key.Trim().ToUpper().Contains("TPI"))
                    && !(_shoppingUtility.EnableEPlusAncillary(appID, appVersion, isReshop) && anOption.Key.Trim().ToUpper().Contains("EFS")))
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

        public bool IsEnableFeature(string feature, int appId, string appVersion)
        {
            var enableFFC = _configuration.GetSection("EnableU4BCorporateBookingFFC").GetValue<bool>("isEnable");
            var android_AppVersion = _configuration.GetSection("EnableU4BCorporateBookingFFC").GetValue<string>("android_EnableU4BCorporateBookingFFC_AppVersion");
            var iPhone_AppVersion = _configuration.GetSection("EnableU4BCorporateBookingFFC").GetValue<string>("iPhone_EnableU4BCorporateBookingFFC_AppVersion");
            return _configuration.GetValue<bool>("EnableU4BCorporateBooking") && enableFFC && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, android_AppVersion, iPhone_AppVersion);
        }

        public bool IsExtraSeatFeatureEnabled(int appId, string appVersion, List<MOBItem> catalogItems)
        {
            if (catalogItems != null && catalogItems.Count > 0 &&
                           catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableExtraSeatFeature).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableExtraSeatFeature).ToString())?.CurrentValue == "1")
                return _configuration.GetValue<bool>("EnableExtraSeatsFeature")
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "Android_EnableExtraSeatsFeature_AppVersion", "IPhone_EnableExtraSeatsFeature_AppVersion", "", "", true, _configuration);
            else return false;
        }

        public bool IsExtraSeatEligibleForInfantOnLapAndInfantOnSeat(string travelerTypeCode, bool isExtraSeat)
        {
            if(isExtraSeat)
                return false;

            var inEligibleTravelTypeCodes = _configuration.GetValue<string>("InEligibleTravelTypeCodesForExtraSeat")?.Split('|').ToList();
            if (!string.IsNullOrEmpty(travelerTypeCode) && inEligibleTravelTypeCodes != null && inEligibleTravelTypeCodes.Count() > 0)
                return !inEligibleTravelTypeCodes.Contains(travelerTypeCode?.ToUpper());
            
            return true;
        }

        public async Task ExtraSeatHandling(MOBCPTraveler traveler, MOBRequest mobRequest, 
            Reservation bookingPathReservation, Session session, string mpNumber = "", bool isUpdateTraveler = false)
        {
            if (IsExtraSeatFeatureEnabled(mobRequest.Application.Id, mobRequest.Application.Version.Major, session.CatalogItems))
            {
                if (bookingPathReservation.ShopReservationInfo2 == null)
                    bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(session.SessionId, bookingPathReservation.ObjectName, new List<string> { session.SessionId, bookingPathReservation.ObjectName });

                if ((isUpdateTraveler && traveler != null) || (!isUpdateTraveler && traveler.PaxID > 0))
                {
                    var eligibleTravelersCSL = bookingPathReservation?.ShopReservationInfo2?.AllEligibleTravelersCSL?.FirstOrDefault(p => p != null && p.PaxID == traveler.PaxID);
                   
                    if (eligibleTravelersCSL != null && !eligibleTravelersCSL.IsExtraSeat && traveler.IsExtraSeat)
                    {
                            throw new MOBUnitedException("4590", _configuration.GetValue<string>("ExtraSeatMessageForEditTraveler"));
                    }

                    var count = bookingPathReservation?.ShopReservationInfo2?.AllEligibleTravelersCSL?.Where(a => a != null && a.ExtraSeatData?.SelectedPaxId == traveler?.PaxID)?.Count();

                    if (count > 0 && !traveler.IsExtraSeat 
                        && bookingPathReservation != null && bookingPathReservation.Trips != null 
                        && bookingPathReservation.Trips[0]?.FlattenedFlights != null 
                        && bookingPathReservation.Trips[0].FlattenedFlights[0]?.Flights != null)
                    {
                        int age = GetAgeByDOB(traveler.BirthDate.ToString(), bookingPathReservation?.Trips[0].FlattenedFlights[0].Flights[0]?.DepartureDateTime);
                        string travelerTypeCode = GetTypeCodeByAge(age);

                        var inEligibleTravelTypeCodes = _configuration.GetValue<string>("InEligibleTravelTypeCodesForExtraSeat")?.Split('|').ToList();
                        if (!string.IsNullOrEmpty(travelerTypeCode) && inEligibleTravelTypeCodes != null && inEligibleTravelTypeCodes.Count() > 0
                            && inEligibleTravelTypeCodes.Contains(travelerTypeCode?.ToUpper()))
                            throw new MOBUnitedException("4590", _configuration.GetValue<string>("UnaccompaniedDisclaimerMessage"));
                    }
                }

                var extraSeatSelectedForTravelerPaxId = bookingPathReservation?.ShopReservationInfo2?.AllEligibleTravelersCSL?.Where(a => a != null && a.IsExtraSeat == true && a.ExtraSeatData?.SelectedPaxId == traveler?.ExtraSeatData?.SelectedPaxId)?.ToList();
                int? extraSeatCount = extraSeatSelectedForTravelerPaxId?.Count();

                int extraSeatWithPersonalComfort = 0;
                int extraSeatWithCabinBaggage = 0;
                if (extraSeatSelectedForTravelerPaxId?.Count > 0)
                {
                    extraSeatWithPersonalComfort = extraSeatSelectedForTravelerPaxId.Where(x => x != null && x.ExtraSeatData?.Purpose?.ToLower() == ExtraSeatReason.PersonalComfort.GetDescription().ToLower()).Count();
                    extraSeatWithCabinBaggage = extraSeatSelectedForTravelerPaxId.Where(x => x != null && x.ExtraSeatData?.Purpose?.ToLower() == ExtraSeatReason.CabinSeatBaggage.GetDescription().ToLower()).Count();
                }

                var tempList = new List<MOBCPTraveler>(bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL);
                if (mpNumber == "")
                    tempList.Add(traveler);

                traveler.IsEligibleForExtraSeatSelection = bookingPathReservation?.ShopReservationInfo2?.displayTravelTypes?.Count > 1;

                if (traveler.IsExtraSeat)
                {
                    int extraSeatMaxAllowedCount = _configuration.GetValue<int>("ExtraSeatMaxAllowedLimit");
                    if (extraSeatCount >= extraSeatMaxAllowedCount && (isUpdateTraveler == false))
                    {
                        throw new MOBUnitedException("4590", _configuration.GetValue<string>("ExtraSeatMaxMessage"));
                    }
                    else
                    {
                        if (traveler.FirstName.Contains(_configuration.GetValue<string>("ExtraSeatName")) == false)
                        {
                            traveler.LastName = GetTravelerDisplayNameForExtraSeat(traveler.FirstName, traveler.MiddleName, traveler.LastName, traveler.Suffix);
                            if (traveler.ExtraSeatData?.Purpose?.ToLower() == ExtraSeatReason.PersonalComfort.GetDescription().ToLower())
                            {
                                SetFirstNamePersonalComfort(traveler, extraSeatWithPersonalComfort);
                            }
                            else if (traveler.ExtraSeatData?.Purpose.ToLower() == ExtraSeatReason.CabinSeatBaggage.GetDescription().ToLower())
                            {
                                SetFirstNameCabinBaggage(traveler, extraSeatWithCabinBaggage);
                            }

                            var paxSelected = bookingPathReservation?.ShopReservationInfo2?.AllEligibleTravelersCSL?.FirstOrDefault(a => a != null && a.PaxID == traveler?.ExtraSeatData?.SelectedPaxId);
                            if (paxSelected != null)
                            {
                                traveler.Nationality = paxSelected?.Nationality;
                                traveler.CountryOfResidence = paxSelected?.CountryOfResidence;
                            }
                        }
                    }
                    // ExtraSeat needed to be added as Passenger and SSR
                    TravelSpecialNeed extraSeatAsSSR = new TravelSpecialNeed();
                    if (traveler.ExtraSeatData?.Purpose == ExtraSeatReason.PersonalComfort.GetDescription())
                    {
                        extraSeatAsSSR.Code = System.Enum.GetName(typeof(EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT), _configuration.GetValue<bool>("SpecialServiceRequestforExtraSeat") ? _configuration.GetValue<int>("SpecialServiceRequestforExtraSeatValue") : extraSeatWithPersonalComfort);
                    }
                    else if (traveler.ExtraSeatData?.Purpose == ExtraSeatReason.CabinSeatBaggage.GetDescription())
                    {
                        extraSeatAsSSR.Code = System.Enum.GetName(typeof(EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE), _configuration.GetValue<bool>("SpecialServiceRequestforExtraSeat") ? _configuration.GetValue<int>("SpecialServiceRequestforExtraSeatValue") : extraSeatWithCabinBaggage);
                    }
                    extraSeatAsSSR.DisplayDescription = _configuration.GetValue<string>("ExtraSeatSSRRemark");
                    extraSeatAsSSR.Type = TravelSpecialNeedType.SpecialRequest.ToString();
                    if (traveler.SelectedSpecialNeeds == null) traveler.SelectedSpecialNeeds = new List<TravelSpecialNeed>();
                    traveler.SelectedSpecialNeeds.Add(extraSeatAsSSR);
                }

                traveler.IsEligibleForExtraSeatSelection = IsExtraSeatEligible(tempList, bookingPathReservation?.ShopReservationInfo2?.displayTravelTypes?.Count(), session, bookingPathReservation, mpNumber);
            
            }
        }

        public string GetTravelerDisplayNameForExtraSeat(string firstName, string middleName, string lastName, string suffix)
        {
            string travelerMiddleName = (!string.IsNullOrEmpty(middleName) ? " " + middleName.Substring(0, 1) : string.Empty);
            string travelerSuffix = (!string.IsNullOrEmpty(suffix) ? " " + suffix : string.Empty);
            return "(" + firstName + travelerMiddleName + " " + lastName + travelerSuffix + ")";
        }

        public string SpecialServiceRequestCode(string ssrCode)
        {
            if (!string.IsNullOrEmpty(ssrCode))
            {
                if (ssrCode.Contains("EXST"))
                    return System.Enum.GetName(typeof(EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT), _configuration.GetValue<int>("SpecialServiceRequestforExtraSeatValue"));
                else if (ssrCode.Contains("CBBG"))
                    return System.Enum.GetName(typeof(EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE), _configuration.GetValue<int>("SpecialServiceRequestforExtraSeatValue"));
            }
            return ssrCode;
        }

        public string GetExtraSeatReasonDescription(string ssrCode)
        {
            if (!string.IsNullOrEmpty(ssrCode))
            {
                if (ssrCode.Contains("EXST"))
                    return ExtraSeatReason.PersonalComfort.GetDescription();
                else if (ssrCode.Contains("CBBG"))
                    return ExtraSeatReason.CabinSeatBaggage.GetDescription();
            }
            return ssrCode;
        }

        private static void SetFirstNamePersonalComfort(MOBCPTraveler traveler, int extraSeatCount)
        {
            if (extraSeatCount == 0)
                traveler.FirstName = EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXST.GetDescription();
            else if (extraSeatCount == 1)
                traveler.FirstName = EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXSTTWO.GetDescription();
            else if (extraSeatCount == 2)
                traveler.FirstName = EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXSTTHREE.GetDescription();
            else if (extraSeatCount == 3)
                traveler.FirstName = EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXSTFOUR.GetDescription();
            else if (extraSeatCount == 4)
                traveler.FirstName = EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXSTFIVE.GetDescription();
            else if (extraSeatCount == 5)
                traveler.FirstName = EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXSTSIX.GetDescription();
        }

        private static void SetFirstNameCabinBaggage(MOBCPTraveler traveler, int extraSeatCount)
        {
            if (extraSeatCount == 0)
                traveler.FirstName = EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBG.GetDescription();
            else if (extraSeatCount == 1)
                traveler.FirstName = EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBGTWO.GetDescription();
            else if (extraSeatCount == 2)
                traveler.FirstName = EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBGTHREE.GetDescription();
            else if (extraSeatCount == 3)
                traveler.FirstName = EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBGFOUR.GetDescription();
            else if (extraSeatCount == 4)
                traveler.FirstName = EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBGFIVE.GetDescription();
            else if (extraSeatCount == 5)
                traveler.FirstName = EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBGSIX.GetDescription();
        }

        public string GivenNameForExtraSeat(string firstName, string middleName, string lastName, string suffix, bool isExtraSeatEnabled)
        {
            if (isExtraSeatEnabled)
            {
                string travelerMiddleInitial = !string.IsNullOrEmpty(middleName) ? " " + middleName.Substring(0, 1) : string.Empty;
                string travelerSuffix = !string.IsNullOrEmpty(suffix) ? " " + suffix : string.Empty;

                return _configuration.GetValue<string>("ExtraSeatName") + " (" + RemoveExtraSeatCodeFromGivenName(firstName) + travelerMiddleInitial + " " + firstName + travelerSuffix + ")";
            }
            else
                return firstName;
        }

        public string RemoveExtraSeatCodeFromGivenName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                if (name.Contains(EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXSTTWO.ToString()))
                    return name.Remove(0, EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXSTTWO.ToString().Length);
                else if (name.Contains(EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBGTWO.ToString()))
                    return name.Remove(0, EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBGTWO.ToString().Length);
                else if (name.Contains(EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXST.ToString()))
                    return name.Remove(0, EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXST.ToString().Length);
                else if (name.Contains(EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBG.ToString()))
                    return name.Remove(0, EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBG.ToString().Length);
            }

            return name;
        }

        private bool IsExtraSeatEligible(List<MOBCPTraveler> travelers, int? bookingMainCount, Session session, Reservation reservation, string mpNumber = "")
        {
            try
            {
                if (IsExtraSeatExcluded(session, reservation?.Trips, reservation?.ShopReservationInfo2?.displayTravelTypes, null, reservation?.ShopReservationInfo2?.IsUnfinihedBookingPath))
                    return false;
                var regularTravelers = travelers?.Where(a => a.IsExtraSeat == false)?.ToList();
                var allTravelerCount = travelers?.Count;
                foreach (var regularTraveler in regularTravelers)
                {
                    var extraSeatCountPerPax = travelers.Where(a => a?.ExtraSeatData?.SelectedPaxId == regularTraveler.PaxID && a.IsExtraSeat == true)?.Count();
                    if (string.IsNullOrEmpty(mpNumber) == false && extraSeatCountPerPax < Convert.ToInt32(_configuration.GetValue<int>("ExtraSeatMaxAllowedLimit")))
                        return true;
                    else if (string.IsNullOrEmpty(mpNumber) && allTravelerCount < bookingMainCount && extraSeatCountPerPax < Convert.ToInt32(_configuration.GetValue<int>("ExtraSeatMaxAllowedLimit")))
                        return true;
                }
            }
            catch { }
            return false;
        }


        public bool IsExtraSeatExcluded(Session session, List<MOBSHOPTrip> trips, List<DisplayTravelType> displayTravelTypes, MOBCPProfileResponse response = null, bool? isUnfinishedBooking = false)
        {
            return (displayTravelTypes?.Count() > 1) && !session.IsCorporateBooking
                && (isUnfinishedBooking == true || session.TravelType == TravelType.RA.ToString() || session.TravelType == TravelType.TPBooking.ToString())
                && IsUAFlight(trips)
                && IsExcludedOperatingCarrier(trips);
                
        }

        public bool IsUAFlight(List<MOBSHOPTrip> trips)
        {
            var unitedCarriers = _configuration.GetValue<string>("UnitedCarriers");
            return (trips?.SelectMany(a => a?.FlattenedFlights)?.SelectMany(b => b?.Flights)?
             .Any(c => !unitedCarriers.Contains(c?.OperatingCarrier)) == true) ? false : true;
        }

        public bool IsExcludedOperatingCarrier(List<MOBSHOPTrip> trips)
        {
            var execludedUnitedCarriers = _configuration.GetValue<string>("ExcludedOperatingCarriersForExtraSeat");
            return (trips?.SelectMany(a => a?.FlattenedFlights)?.SelectMany(b => b?.Flights)?
             .Any(c => execludedUnitedCarriers.Contains(c?.OperatingCarrier)) == true) ? false : true;
        }

        public void ExtraSeatHandlingForProfile(MOBRequest request, MOBCPProfileResponse response, Session session)
        {
            
                if (IsExtraSeatFeatureEnabled(request.Application.Id, request.Application.Version.Major, session.CatalogItems) && response.Reservation != null)
                {
                   response.Reservation.ShopReservationInfo2.AllowExtraSeatSelection = IsExtraSeatExcluded(session, response.Reservation?.Trips, response?.Reservation?.ShopReservationInfo2?.displayTravelTypes, response, response?.Reservation?.ShopReservationInfo2?.IsUnfinihedBookingPath);
                  
                }
                else
                {
                    if (response?.Reservation?.ShopReservationInfo2 != null)
                        response.Reservation.ShopReservationInfo2.AllowExtraSeatSelection = false;
                }
            
        }

        public List<string> GetTravelerNameIndexForExtraSeat(bool isExtraSeatEnabled, Collection<Service.Presentation.CommonModel.Service> services)
        {
            var extraSeatPassengerIndex = new List<string>();
            if (isExtraSeatEnabled)
            {
                var extraSeatCodes = _configuration.GetValue<string>("EligibleSSRCodesForExtraSeat")?.Split("|");
                services?.ForEach(x =>
                {
                    if (!string.IsNullOrEmpty(x?.Code) && extraSeatCodes.Contains(x.Code) && !string.IsNullOrEmpty(x.TravelerNameIndex) && !extraSeatPassengerIndex.Contains(x.TravelerNameIndex))
                    {
                        extraSeatPassengerIndex.Add(x.TravelerNameIndex);
                    }
                });
            }

            return extraSeatPassengerIndex;
        }

        public string GetExtraSeatReason(string travelerNameIndex, bool isExtraSeatEnabled, Collection<Service.Presentation.CommonModel.Service> services)
        {
            if (!string.IsNullOrEmpty(travelerNameIndex) && isExtraSeatEnabled && services != null)
            {
                string extraSeatCode = services?.FirstOrDefault(w => w?.TravelerNameIndex == travelerNameIndex).Code;

                if (!string.IsNullOrEmpty(extraSeatCode))
                    return GetExtraSeatReasonDescription(extraSeatCode);
            }
            
            return string.Empty;
        }

        // For express checkout flow, if this is the holder account skip review itinerary and bundles screens.
        public async Task SetNextViewNameForEliteCustomer(United.Mobile.Model.Common.ProfileResponse profilePersist, United.Mobile.Model.Shopping.Reservation bookingPathReservation)
        {
            if (profilePersist != null && profilePersist?.Response != null && profilePersist?.Response?.Profiles != null
                && profilePersist?.Response?.Profiles[0]?.Travelers != null
                && profilePersist?.Response?.Profiles[0]?.Travelers.Any() == true && profilePersist?.Response?.Profiles[0]?.Travelers[0] != null
                && profilePersist?.Response?.Profiles[0]?.Travelers[0]?.IsProfileOwner == true
                && bookingPathReservation != null && bookingPathReservation?.ShopReservationInfo2 != null
                )
            {
                if (profilePersist?.Response?.Profiles[0]?.Travelers[0]?.CurrentEliteLevel > 0)
                {
                    bookingPathReservation.ShopReservationInfo2.NextViewName = "Seats";
                }
                else
                {
                    bookingPathReservation.ShopReservationInfo2.IsExpressCheckoutPath = false;
                }
                await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
            }
        }

        public async Task DuplicateTravelerCheck(string sessionId, MOBCPTraveler traveler, bool isExtraSeatFeatureEnabled)
        {
            Reservation bookingReservation = new Reservation();
            bookingReservation = await _sessionHelperService.GetSession<Reservation>(sessionId, bookingReservation.ObjectName, new List<String> { sessionId, bookingReservation.ObjectName }).ConfigureAwait(false);

            if (bookingReservation != null && bookingReservation.ShopReservationInfo2 != null && bookingReservation.ShopReservationInfo2.AllEligibleTravelersCSL != null && bookingReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Exists(tr =>
                                                                               (isExtraSeatFeatureEnabled ? traveler.IsExtraSeat == false : true) &&
                                                                               (traveler.PaxID == 0 || tr.PaxID != traveler.PaxID) &&
                                                                               tr.FirstName.ToLower() == traveler.FirstName.ToLower() &&
                                                                               tr.MiddleName.ToLower() == traveler.MiddleName.ToLower() &&
                                                                               tr.LastName.ToLower() == traveler.LastName.ToLower() &&
                                                                               tr.Suffix.ToLower() == traveler.Suffix.ToLower()))
            {
                    throw new MOBUnitedException(_configuration.GetValue<string>("DuplicateTravelerMessage").ToString());
            }
        }

        public ReservationInfo2 corpMultiPaxInfo(MOBSHOPReservation mOBSHOPReservation, ReservationInfo2 reservationInfo2)
        {
            try
            {
                reservationInfo2.CorpMultiPaxInfo = new corpMultiPaxInfo();
                reservationInfo2.CorpMultiPaxInfo.ShowUAMileagePlusNumberField = true;
                reservationInfo2.IsShowAddNewTraveler = true;
                reservationInfo2.CorpMultiPaxInfo.RewardProgram = new RewardProgram();
                reservationInfo2.CorpMultiPaxInfo.RewardProgram = mOBSHOPReservation?.RewardPrograms?.Where(a => a.ProgramID == "7").FirstOrDefault();
                return reservationInfo2;
            }
            catch { }
            return reservationInfo2;
        }
        public corpMultiPaxInfo corpMultiPaxInfo(bool isCorpBooking, bool isArrangerBooking, bool isMultipaxAllowed, List<RewardProgram> rewardPrograms)
        {
            corpMultiPaxInfo corpMultiPaxInfo = new corpMultiPaxInfo();
            try
            {
                if (isCorpBooking && isMultipaxAllowed)
                {
                    corpMultiPaxInfo.ShowUAMileagePlusNumberField = isArrangerBooking ? false : true;
                    corpMultiPaxInfo.RewardProgram = new RewardProgram();
                    corpMultiPaxInfo.RewardProgram = rewardPrograms?.Where(a => a != null && !string.IsNullOrEmpty(a.ProgramID) && a.ProgramID == "7").FirstOrDefault();
                }
            }
            catch { }
            return corpMultiPaxInfo;
        }
    }
}
