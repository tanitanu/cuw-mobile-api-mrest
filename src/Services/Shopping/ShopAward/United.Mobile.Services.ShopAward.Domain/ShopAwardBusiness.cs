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
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.ShopAward;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.AwardCalendar;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.Boombox;
using United.Services.FlightShopping.Common.Extensions;
using United.Services.FlightShopping.Common.SpecialPricing;
using United.Utility.Helper;
using CSLShopRequest = United.Mobile.Model.Shopping.CSLShopRequest;
using Flight = United.Services.FlightShopping.Common.Flight;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using SearchType = United.Services.FlightShopping.Common.SearchType;
using ShoppingResponse = United.Mobile.Model.Shopping.ShoppingResponse;
using ShopResponse = United.Services.FlightShopping.Common.ShopResponse;
using Trip = United.Services.FlightShopping.Common.Trip;

namespace United.Mobile.Services.ShopAward.Domain
{
    public class ShopAwardBusiness : IShopAwardBusiness
    {
        private readonly ICacheLog<ShopAwardBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IMileagePlus _mileagePlus;

        private readonly IFlightShoppingService _flightShoppingService;
        private readonly IDPService _dPService;
        private readonly IAwardCalendarAzureService _awardCalendarAzureService;

        public ShopAwardBusiness(ICacheLog<ShopAwardBusiness> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility
            , IFlightShoppingService flightShoppingService
            , IDPService dPService
            , IAwardCalendarAzureService awardCalendarAzureService
            , IMileagePlus mileagePlus
            , IShoppingSessionHelper shoppingSessionHelper)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
            _flightShoppingService = flightShoppingService;
            _dPService = dPService;
            _awardCalendarAzureService = awardCalendarAzureService;
            _mileagePlus = mileagePlus;
            _shoppingSessionHelper = shoppingSessionHelper;
        }

        public async Task<AwardCalendarResponse> GetSelectTripAwardCalendar(SelectTripRequest shopRequest)
        {
            Session session = await _shoppingSessionHelper.GetShoppingSession(shopRequest.SessionId);
            AwardCalendarResponse response = new AwardCalendarResponse();
            try
            {
                if (!_configuration.GetValue<bool>("ReshopAwardCalendarFlag") && session.IsReshopChange) return response;
                string logAction = (session.IsReshopChange) ? "ReShop - GetShopAwardCalendar" : "GetShopAwardCalendar";

                response = await SelectTripAwardDynamicCalendar(shopRequest, session.Token);

                response.AwardCalendarRequest = shopRequest;
                response.TransactionId = shopRequest.TransactionId;
                response.LanguageCode = shopRequest.LanguageCode;
                response.CartId = session.CartId;
                if (response.AwardDynamicCalendar == null)
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("AwardCalendarMP2017GenericExceptionMessage"));
                }
            }
            catch(Exception ex)
            {
                _logger.LogError("GetSelectTripAwardCalendar Error {@Exception}", JsonConvert.SerializeObject(ex));
                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
                //from iOS revenue flow this method is called and thrown exception which is not expected. applying the fix for that
                if (!_configuration.GetValue<bool>("DisableAwardCalendarExceptionMessageForRevenue"))
                {                    
                    if (session != null && !session.IsReshopChange && !session.IsAward)
                    {
                        response.Exception = null;
                    }
                }
            }
            return await Task.FromResult(response);
        }

        public async Task<AwardCalendarResponse> GetShopAwardCalendar(MOBSHOPShopRequest shopRequest)
        {
            AwardCalendarResponse response = new AwardCalendarResponse();

            CheckTripsDepartDateAndAssignPreviousTripDateIfItLesser(shopRequest);

            Session session = await _shoppingSessionHelper.GetShoppingSession(shopRequest.SessionId);
            if (!_configuration.GetValue<bool>("ReshopAwardCalendarFlag") && session.IsReshopChange) return response;

            //Fix for call from Invalid MP No without HashPin (GetShopRequest - GetAccountSummary : JIRA : MOBILE-7272) - by Shashank
            if (shopRequest.AwardTravel == true && _configuration.GetValue<bool>("ForAwardTravelGetShopAwardCalendarRequestWithEmptyOrInvalidMpNo"))
            {
                if (!string.IsNullOrEmpty(shopRequest.MileagePlusAccountNumber) && !string.IsNullOrEmpty(shopRequest.HashPinCode) && await _shoppingUtility.ValidateHashPinAndGetAuthToken(shopRequest.MileagePlusAccountNumber, shopRequest.HashPinCode, shopRequest.Application.Id, shopRequest.DeviceId, shopRequest.Application.Version.Major))
                {
                    response = await ShopAwardDynamicCalendar(shopRequest, session.Token);
                }
                else
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("AwardCalendarMP2017GenericExceptionMessage"));
                }
            }
            else
            {
                response = await ShopAwardDynamicCalendar(shopRequest, session.Token);
            }

            response.TransactionId = shopRequest.TransactionId;
            response.LanguageCode = shopRequest.LanguageCode;
            response.CartId = session.CartId;
            if (response.AwardDynamicCalendar == null)
            {
                response.Exception = new MOBException("9999", _configuration.GetValue<string>("AwardCalendarMP2017GenericExceptionMessage"));
            }
            return await Task.FromResult(response);
        }

        public async Task<RevenueLowestPriceForAwardSearchResponse> RevenueLowestPriceForAwardSearch(MOBSHOPShopRequest shopRequest)
        {
            RevenueLowestPriceForAwardSearchResponse response = new RevenueLowestPriceForAwardSearchResponse();
            MOBSHOPShopRequest updateShopRequest = new MOBSHOPShopRequest();
            string logAction = "RevenueLowestPriceForAwardSearch";
            //shopping.LogEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBSHOPShopRequest>(request.SessionId, logAction, "Request", request.Application.Id, request.Application.Version.Major, request.DeviceId, request));

            var session = await _shoppingSessionHelper.CreateShoppingSession(shopRequest.Application.Id, shopRequest.DeviceId,
                      shopRequest.Application.Version.Major, shopRequest.TransactionId, shopRequest.MileagePlusAccountNumber,
                       shopRequest.EmployeeDiscountId, shopRequest.IsELFFareDisplayAtFSR,
                     shopRequest.IsReshopChange, shopRequest.AwardTravel);
            shopRequest.SessionId = session.SessionId;
            updateShopRequest = shopRequest.Clone();
            if (session == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }
            //Building the equivalent revenue request using the award request
            ConvetAwardToRevenueRequest(updateShopRequest);
            //Call Shop fare wheel to get the lowest price for the given itenary
            string lowestRevenuePrice = string.Empty;
            MOBSHOPShopRequest shopFareWheelRequest = new MOBSHOPShopRequest();
            shopFareWheelRequest = updateShopRequest.Clone();
            shopFareWheelRequest.SessionId = shopRequest.SessionId;
            lowestRevenuePrice = await GetRevenueLowestPriceForAwardSearch(shopFareWheelRequest, session);

            if (string.IsNullOrEmpty(lowestRevenuePrice))
            {
                throw new MOBUnitedException("Lowest revenue fare flight not found for given Search Criteria");
            }

            BuildRevenueLowestPriceForAwardSearchResponse(shopRequest, response, updateShopRequest, session.CartId, lowestRevenuePrice);

            return response;
        }

        public async Task<MOBException> GetAwardCalendarExceptionMessage(string code)
        {
            MOBException exception = new MOBException();
            string[] apologyErrorCode = { "400.1000", "400.1001", "400.1002", "400.1003" };
            string[] awardCalendarUnavailableErrorCodes = { "500.5000", "400.1004" };
            string exceptionMessage = code;
            if (apologyErrorCode.Contains(code) && (_configuration.GetValue<string>("AwardCalendarMP2017ApologyErrorCode") ?? "") == "400.100X")
            {
                exception.Message = _configuration.GetValue<string>("AwardCalendarMP2017ApologyMessage");
                exception.Code = _configuration.GetValue<string>("AwardCalendarMP2017ApologyErrorCode");
            }
            else if (awardCalendarUnavailableErrorCodes.Contains(code))
            {
                exception.Message = _configuration.GetValue<string>("AwardCalendarMP2017GenericExceptionMessage");
                exception.Code = code;
            }
            else
            {
                exception.Message = _configuration.GetValue<string>("AwardCalendarMP2017GenericExceptionMessage");
            }
            return await Task.FromResult(exception);
        }

        private async Task<string> GetRevenueLowestPriceForAwardSearch(MOBSHOPShopRequest shopRequest, Session session)
        {

            string lowestRevenueFare = string.Empty;
            string logAction = "GetRevenueLowestPriceForAwardSearch";
            FareWheelResponse response = new FareWheelResponse();
            shopRequest.GetFlightsWithStops = false;
            shopRequest.GetNonStopFlightsOnly = false;

            ShopRequest request = await GetShopRequest(shopRequest, false);

            request.FlexibleDaysAfter = _configuration.GetValue<int>("RevenueLowestPriceForAwardSearchFarewheelFlexibleDaysAfter");
            request.FlexibleDaysBefore = _configuration.GetValue<int>("RevenueLowestPriceForAwardSearchFarewheelFlexibleDaysBefore");
            request.DisableMostRestrictive = !shopRequest.IsELFFareDisplayAtFSR;
            //request.CartId = session.CartId;

            string shopFareWheelJsonRequest = JsonConvert.SerializeObject(request), shopCSLCallDurations = string.Empty;

            shopCSLCallDurations = string.Empty;

            var shopFareWheelInfoResponse = await _flightShoppingService.ShopFareWheelInfo<ShopResponse>(session.Token, session.SessionId, "ShopFareWheel", shopFareWheelJsonRequest);

            response = DeserialiseAndBuildFareWheelResponse(shopFareWheelInfoResponse);
            return GetLowestRevenuePrice(response.FareWheel, request.DepartDateTime);
        }

        private string GetLowestRevenuePrice(List<MOBSHOPFareWheelItem> FareWheel, string departDateTime)
        {
            foreach (var fareWheelItem in FareWheel)
            {
                if (fareWheelItem.Key == departDateTime)
                {
                    if (fareWheelItem.Value != "Not available")
                        return fareWheelItem.Value;
                }
            }
            return string.Empty;
        }

        private FareWheelResponse DeserialiseAndBuildFareWheelResponse(ShopResponse shopFareWheelResponse)
        {
            FareWheelResponse response = new FareWheelResponse();
            
                if (shopFareWheelResponse != null && shopFareWheelResponse.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success) && (shopFareWheelResponse.Errors == null || shopFareWheelResponse.Errors.Count == 0))
                {
                    response.FareWheel = PopulateFareWheel(shopFareWheelResponse.FareWheelGrid, shopFareWheelResponse.LastBBXSolutionSetId);

                }
                else
                {
                    BuildShopFareWheelErrorMessage(shopFareWheelResponse);
                }
            return response;
        }

        private void BuildShopFareWheelErrorMessage(ShopResponse shopFareWheelResponse)
        {
            #region 89882 - CSL: ShopFareWheel : FareWheelOnly is not allowed and  FAREWHEEEL NOT FOUND - Ravi/Issuf
            if (shopFareWheelResponse.Errors != null && shopFareWheelResponse.Errors.Count > 0)
            {
                string errorMessage = string.Empty;
                foreach (var error in shopFareWheelResponse.Errors)
                {
                    errorMessage = errorMessage + " " + error.Message;

                    if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10035"))
                    {
                        throw new MOBUnitedException(error.Message);
                    }
                    else if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10038"))
                    {

                        throw new MOBUnitedException(error.Message);

                    }
                    // Added by Ali as part of Task 264468:Booking Flow Exception analysis - GetFareWheelList 
                    else if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && (error.MinorCode.Trim().Equals("10051") || error.MinorCode.Trim().Equals("10052") || error.MinorCode.Trim().Equals("10056") || error.MinorCode.Trim().Equals("10069")))
                    {
                        throw new MOBUnitedException(error.Message);
                    }
                }
                throw new System.Exception(errorMessage);
            }
            #endregion
        }

        private List<MOBSHOPFareWheelItem> PopulateFareWheel(Grid fareWheelGrid, string tripId)
        {
            List<MOBSHOPFareWheelItem> fares = new List<MOBSHOPFareWheelItem>();

            if (fareWheelGrid != null && fareWheelGrid.Rows != null && fareWheelGrid.Rows.Count > 0)
            {
                CultureInfo ci = null;

                foreach (GridRow row in fareWheelGrid.Rows)
                {
                    if (row.RowItems != null && row.RowItems.Count > 0)
                    {
                        foreach (GridRowItem item in row.RowItems)
                        {
                            if (ci == null)
                            {
                                if (item.PricingItem != null)
                                {
                                    ci = TopHelper.GetCultureInfo(item.PricingItem.Currency);
                                }
                            }

                            if (item != null)
                            {
                                if (item.PricingItem != null)
                                {
                                    MOBSHOPFareWheelItem kvp = new MOBSHOPFareWheelItem();
                                    int year = DateTime.Today.Year;
                                    int.TryParse(item.Day.Substring(0, 4), out year);
                                    int month = DateTime.Today.Month;
                                    int.TryParse(item.Day.Substring(5, 2), out month);
                                    int day = DateTime.Today.Day;
                                    int.TryParse(item.Day.Substring(8, 2), out day);

                                    DateTime fareDate = new DateTime(year, month, day);
                                    kvp.Key = (month < 10 ? "0" + month.ToString() : month.ToString()) + "/" + (day < 10 ? "0" + day.ToString() : day.ToString()) + "/" + year.ToString();
                                    kvp.DisplayValue = fareDate.ToString("ddd MMM dd");
                                    if (!_configuration.GetValue<bool>("DisableGetFareWheelProductIdMissingFix"))
                                    {
                                        kvp.Value = "Not available";
                                        kvp.ProductId = "Not available";
                                        if (item.PricingItem.Amount > 0 && !string.IsNullOrWhiteSpace(item.ID))
                                        {
                                            kvp.Value = TopHelper.FormatAmountForDisplay(item.PricingItem.Amount, ci);
                                            kvp.ProductId = item.ID;
                                        }
                                    }
                                    else
                                    {
                                        if (item.PricingItem.Amount > 0)
                                        {
                                            kvp.Value = TopHelper.FormatAmountForDisplay(item.PricingItem.Amount, ci);
                                            kvp.ProductId = item.ID;
                                        }
                                        else
                                        {
                                            kvp.Value = "Not available";
                                            kvp.ProductId = "Not available";
                                        }
                                    }
                                    kvp.TripId = tripId;
                                    fares.Add(kvp);
                                }
                                else
                                {
                                    MOBSHOPFareWheelItem kvp = new MOBSHOPFareWheelItem();
                                    int year = DateTime.Today.Year;
                                    int.TryParse(item.Day.Substring(0, 4), out year);
                                    int month = DateTime.Today.Month;
                                    int.TryParse(item.Day.Substring(5, 2), out month);
                                    int day = DateTime.Today.Day;
                                    int.TryParse(item.Day.Substring(8, 2), out day);

                                    DateTime fareDate = new DateTime(year, month, day);
                                    kvp.Key = (month < 10 ? "0" + month.ToString() : month.ToString()) + "/" + (day < 10 ? "0" + day.ToString() : day.ToString()) + "/" + year.ToString();
                                    kvp.Value = "Not available";
                                    kvp.DisplayValue = fareDate.ToString("ddd MMM dd");
                                    kvp.ProductId = "Not available";
                                    kvp.TripId = tripId;
                                    fares.Add(kvp);
                                }
                            }
                        }
                    }
                }
            }


            return fares;
        }

        private void BuildRevenueLowestPriceForAwardSearchResponse(MOBSHOPShopRequest request, RevenueLowestPriceForAwardSearchResponse response, MOBSHOPShopRequest updateShopRequest, String cartId, string lowestRevenuePrice)
        {
            response.FSRRevenueLowestPriceAlertMessages = new List<MOBFSRAlertMessage>
            {
                BuildFSRAlertmessage_RevenueLowestPriceForAwardSearch(updateShopRequest, lowestRevenuePrice)
            };
            response.SessionId = request.SessionId;
            response.CartId = cartId;
            response.RevenueLowestPriceForAwardSearchRequest = request;
            response.TransactionId = request.TransactionId;
            response.LanguageCode = request.LanguageCode;
        }

        private MOBFSRAlertMessage BuildFSRAlertmessage_RevenueLowestPriceForAwardSearch(MOBSHOPShopRequest updateShopRequest, string lowestRevenuePrice)
        {

            MOBFSRAlertMessage alertMessage = new MOBFSRAlertMessage
            {
                BodyMessage = _configuration.GetValue<string>("FSRRevenueLowestPriceForAwardSearchMsgBody"),
                HeaderMessage = _configuration.GetValue<string>("FSRRevenueLowestPriceForAwardSearchMsgHeader"),
                MessageTypeDescription = FSRAlertMessageType.RevenueLowestPriceForAwardSearch,
                MessageType = 0
            };
            if (_shoppingUtility.EnableAirportDecodeToCityAllAirports(updateShopRequest.IsReshop))
            {
                alertMessage.AlertType = MOBFSRAlertMessageType.Information.ToString();
            }
            MOBFSRAlertMessageButton button = new MOBFSRAlertMessageButton
            {
                ButtonLabel = string.Format(_configuration.GetValue<string>("FSRRevenueLowestPriceForAwardSearchButtonLabel"), lowestRevenuePrice),
                UpdatedShopRequest = updateShopRequest
            };
            button.UpdatedShopRequest.IsRevShopCallFromAwardFSR1 = true;
            alertMessage.Buttons = new List<MOBFSRAlertMessageButton>();
            alertMessage.Buttons.Add(button);
            return alertMessage;
        }

        private void ConvetAwardToRevenueRequest(MOBSHOPShopRequest updateShopRequest)
        {
            updateShopRequest.AwardTravel = false;
            updateShopRequest.CustomerMetrics = null;
            updateShopRequest.GetNonStopFlightsOnly = true;
            updateShopRequest.SessionId = string.Empty;
            foreach (MOBSHOPTripBase trip in updateShopRequest.Trips)
            {
                trip.Cabin = GetRevenueCabin(trip.Cabin);
            }
        }

        private string GetRevenueCabin(string cabin)
        {

            switch (cabin.Trim().ToLower())
            {
                case "awardecon":
                    cabin = "econ";
                    break;
                case "awardbusinessfirst":
                    cabin = "businessFirst";
                    break;
                case "awardfirst":
                    cabin = "first";
                    break;
                default:
                    return cabin;
            }
            return cabin;
        }

        private async Task<ShopRequest> GetShopRequest(MOBSHOPShopRequest ShopRequest, bool isShopRequest)
        {
            ShopRequest shopRequest = new ShopRequest
            {
                RememberedLoyaltyId = ShopRequest.MileagePlusAccountNumber,
                LoyaltyId = ShopRequest.MileagePlusAccountNumber,
                ChannelType = _configuration.GetValue<string>("Shopping - ChannelType"),
                AccessCode = _configuration.GetValue<string>("AccessCode - CSLShopping")
            };
            var isStandardRevenueSearch = IsStandardRevenueSearch(ShopRequest.IsCorporateBooking, ShopRequest.IsYoungAdultBooking,
                                                                  ShopRequest.AwardTravel, ShopRequest.EmployeeDiscountId,
                                                                  ShopRequest.TravelType, ShopRequest.IsReshop || ShopRequest.IsReshopChange,
                                                                  ShopRequest.FareClass, ShopRequest.PromotionCode);
            if (!ShopRequest.IsReshopChange && !ShopRequest.AwardTravel)
            {
                shopRequest.DisablePricingBySlice = _shoppingUtility.EnableRoundTripPricing(ShopRequest.Application.Id, ShopRequest.Application.Version.Major);
            }
            if (_configuration.GetValue<bool>("EnableTripPlannerView") && ShopRequest.TravelType == MOBTripPlannerType.TPSearch.ToString() || ShopRequest.TravelType == MOBTripPlannerType.TPEdit.ToString())
            {
                if (shopRequest.Characteristics == null) shopRequest.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                shopRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "IsTripPlannerRequest", Value = "true" });
            }
            bool decodeOTP = false;
            bool.TryParse(_configuration.GetValue<string>("DecodesOnTimePerformance"), out decodeOTP);
            shopRequest.DecodesOnTimePerfRequested = decodeOTP;

            bool decodesRequested = false;
            bool.TryParse(_configuration.GetValue<string>("DecodesRequested"), out decodesRequested);
            shopRequest.DecodesRequested = decodesRequested;

            bool includeAmenities = false;
            if (!Convert.ToBoolean(_configuration.GetValue<string>("ByPassAmenities").ToString()))
            {
                bool.TryParse(_configuration.GetValue<string>("IncludeAmenities"), out includeAmenities);
            }
            shopRequest.IncludeAmenities = includeAmenities;

            var version1 = _configuration.GetValue<string>("FSRBasicEconomyToggleOnBookingMainAndroidversion");
            var version2 = _configuration.GetValue<string>("FSRBasicEconomyToggleOnBookingMainiOSversion");
            if (_configuration.GetValue<bool>("EnableFSRBasicEconomyToggleOnBookingMain") /*Master toggle to hide the be column */
                && GeneralHelper.IsApplicationVersionGreaterorEqual(ShopRequest.Application.Id, ShopRequest.Application.Version.Major, version1, version2) /*Version check for latest client changes which hardcoded IsELFFareDisplayAtFSR to true at Shop By Map*/
                && CheckFSRRedesignFromShopRequest(ShopRequest))//check for FSR resdesign experiment ON Builds )
            {
                shopRequest.DisableMostRestrictive = !ShopRequest.IsELFFareDisplayAtFSR;
            }

            shopRequest.SessionId = ShopRequest.SessionId;
            shopRequest.CountryCode = ShopRequest.CountryCode;
            shopRequest.SimpleSearch = true;

            // Refundable fares toggle feature
            if (IsEnableRefundableFaresToggle(ShopRequest.Application.Id, ShopRequest.Application.Version.Major) &&
                isStandardRevenueSearch &&
                (isShopRequest ||
                 (ShopRequest.Trips[0].SearchFiltersIn?.RefundableFaresToggle?.IsSelected ?? false) ||
                 (ShopRequest.Trips[0].SearchFiltersIn?.RefundableFaresToggle == null && ShopRequest.FareType == "urf")))
            {
                shopRequest.FareType = _configuration.GetValue<string>("RefundableFaresToggleFareType");

                if (shopRequest.Characteristics == null) shopRequest.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                shopRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "filterflightsbytoggle", Value = "true" });
            }

            // Mixed Cabin toggle feature
            if (IsMixedCabinFilerEnabled(ShopRequest.Application.Id, ShopRequest.Application.Version.Major) &&
                ShopRequest.AwardTravel && ShopRequest.IsReshopChange == false &&
                (isShopRequest ||
                 ShopRequest?.Trips[0]?.SearchFiltersIn == null || (ShopRequest?.Trips[0]?.SearchFiltersIn?.AdditionalToggles?.Where(a => a.Key == _configuration.GetValue<string>("MixedCabinToggleKey")) == null) ||
                 (ShopRequest?.Trips[0]?.SearchFiltersIn?.AdditionalToggles?.FirstOrDefault(a => a.Key == _configuration.GetValue<string>("MixedCabinToggleKey"))?.IsSelected ?? false)))
            {
                shopRequest.FareType = _configuration.GetValue<string>("MixedCabinToggle");

                if (shopRequest.Characteristics == null) shopRequest.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                shopRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "filterflightsbytoggle", Value = "true" });
            }

            //shopRequest.FareFamilies = true;
            if (isShopRequest)
            {
                shopRequest.FlexibleDaysBefore = _configuration.GetValue<string>("ShopFareWheelFlexibleDaysBefore") == null ? 0 : Convert.ToInt32(_configuration.GetValue<string>("ShopFareWheelFlexibleDaysBefore").ToString());  //getFlexibleDaysBefore();

                shopRequest.FlexibleDaysAfter = _configuration.GetValue<string>("ShopFareWheelFlexibleDaysAfter") == null ? 0 : Convert.ToInt32(_configuration.GetValue<string>("ShopFareWheelFlexibleDaysAfter").ToString()); //getFlexibleDaysAfter();
            }
            else
            {
                shopRequest.FlexibleDaysBefore = GetFlexibleDaysBefore();
                shopRequest.FlexibleDaysAfter = GetFlexibleDaysAfter();
            }
            shopRequest.FareCalendar = false;
            //to prep for REST filtering, hardcodeing max trips
            shopRequest.MaxTrips = GetShoppingSearchMaxTrips();
            shopRequest.PageIndex = 1;
            shopRequest.PageSize = _configuration.GetValue<int>("ShopAndSelectTripCSLRequestPageSize");
            ///172651 : mApp FSR Booking: Flight Results are disappearing  from the FSR screen when user try to Filter the Flights.
            ///184707 : Booking FSR mApp: Wrong flights count is displaying when we tap on Done button in the Airport filter screen
            ///Srini 11/22/2017
            if (_configuration.GetValue<bool>("BugFixToggleFor17M"))
            {
                if (ShopRequest.GetFlightsWithStops)
                {
                    MOBSHOPAvailability nonStopsAvailability = await GetLastTripAvailabilityFromPersist(1, ShopRequest.SessionId);
                    shopRequest.PageSize = shopRequest.PageSize - nonStopsAvailability.Trip.FlightCount;
                }
            }
            shopRequest.DepartDateTime = ShopRequest.Trips[0].DepartDate;
            shopRequest.RecentSearchKey = ShopRequest.Trips[0].Origin + ShopRequest.Trips[0].Destination + ShopRequest.Trips[0].DepartDate;

            shopRequest.Origin = ShopRequest.Trips[0].Origin;
            shopRequest.Destination = ShopRequest.Trips[0].Destination;

            shopRequest.PromoCode = ShopRequest.PromotionCode;
            shopRequest.BookingCodesSpecified = ShopRequest.FareClass;

            if (ShopRequest.IsReshopChange)
            {
                var tupleRresponse = await GetReshopTripsList(ShopRequest);
                shopRequest.Trips = tupleRresponse.tripsList;
                shopRequest.CabinPreferenceMain = GetCabinPreference(shopRequest.Trips[0].CabinType);

                if (_configuration.GetValue<bool>("EnableReshopOverride24HrFlex") && tupleRresponse.isOverride24HrFlex)
                {
                    shopRequest.Characteristics = (shopRequest.Characteristics == null)
                        ? new Collection<United.Service.Presentation.CommonModel.Characteristic>() : shopRequest.Characteristics;
                    shopRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic { Code = "Ignore24HrFlex", Value = "true" });
                }
            }
            else
            {
                shopRequest.Trips = new List<Trip>();
                Trip trip = GetTrip(ShopRequest.Trips[0].Origin, ShopRequest.Trips[0].Destination, ShopRequest.Trips[0].DepartDate, ShopRequest.Trips[0].Cabin, ShopRequest.Trips[0].UseFilters, ShopRequest.Trips[0].SearchFiltersIn, ShopRequest.Trips[0].SearchNearbyOriginAirports, ShopRequest.Trips[0].SearchNearbyDestinationAirports,
                                    ShopRequest.Application.Id, ShopRequest.Application.Version.Major, isStandardRevenueSearch, ShopRequest.IsELFFareDisplayAtFSR, ShopRequest.FareType,
                                    false, ShopRequest.Trips[0].OriginAllAirports, ShopRequest.Trips[0].DestinationAllAirports);
                if (trip == null)
                {
                    throw new MOBUnitedException("You must specify at least one trip.");
                }
                trip.TripIndex = 1;
                if (trip.SearchFiltersIn == null)
                {
                    trip.SearchFiltersIn = new SearchFilterInfo();
                }
                trip.SearchFiltersIn.FareFamily = GetFareFamily(ShopRequest.Trips[0].Cabin, ShopRequest.FareType);
                shopRequest.Trips.Add(trip);
                shopRequest.CabinPreferenceMain = GetCabinPreference(trip.CabinType);

                if (ShopRequest.Trips.Count > 1)
                {
                    trip = GetTrip(ShopRequest.Trips[1].Origin, ShopRequest.Trips[1].Destination, ShopRequest.Trips[1].DepartDate, ShopRequest.Trips[1].Cabin, ShopRequest.Trips[1].UseFilters, ShopRequest.Trips[1].SearchFiltersIn, ShopRequest.Trips[1].SearchNearbyOriginAirports, ShopRequest.Trips[1].SearchNearbyDestinationAirports,
                                    ShopRequest.Application.Id, ShopRequest.Application.Version.Major, isStandardRevenueSearch, ShopRequest.IsELFFareDisplayAtFSR, ShopRequest.FareType,
                                    false, ShopRequest.Trips[1].OriginAllAirports, ShopRequest.Trips[1].DestinationAllAirports);
                    if (trip != null)
                    {
                        trip.TripIndex = 2;
                        if (trip.SearchFiltersIn == null)
                        {
                            trip.SearchFiltersIn = new SearchFilterInfo();
                        }
                        trip.SearchFiltersIn.FareFamily = GetFareFamily(ShopRequest.Trips[1].Cabin, ShopRequest.FareType);
                        shopRequest.Trips.Add(trip);
                    }
                }

                if (ShopRequest.Trips.Count > 2)
                {
                    trip = GetTrip(ShopRequest.Trips[2].Origin, ShopRequest.Trips[2].Destination, ShopRequest.Trips[2].DepartDate, ShopRequest.Trips[2].Cabin, ShopRequest.Trips[2].UseFilters, ShopRequest.Trips[2].SearchFiltersIn, ShopRequest.Trips[2].SearchNearbyOriginAirports, ShopRequest.Trips[2].SearchNearbyDestinationAirports,
                                    ShopRequest.Application.Id, ShopRequest.Application.Version.Major, isStandardRevenueSearch, ShopRequest.IsELFFareDisplayAtFSR, ShopRequest.FareType,
                                    false, ShopRequest.Trips[2].OriginAllAirports, ShopRequest.Trips[2].DestinationAllAirports);
                    if (trip != null)
                    {
                        trip.TripIndex = 3;
                        if (trip.SearchFiltersIn == null)
                        {
                            trip.SearchFiltersIn = new SearchFilterInfo();
                        }
                        trip.SearchFiltersIn.FareFamily = GetFareFamily(ShopRequest.Trips[2].Cabin, ShopRequest.FareType);
                        shopRequest.Trips.Add(trip);
                    }
                }
                if (ShopRequest.Trips.Count > 3)
                {
                    trip = GetTrip(ShopRequest.Trips[3].Origin, ShopRequest.Trips[3].Destination, ShopRequest.Trips[3].DepartDate, ShopRequest.Trips[3].Cabin, ShopRequest.Trips[3].UseFilters, ShopRequest.Trips[3].SearchFiltersIn, ShopRequest.Trips[3].SearchNearbyOriginAirports, ShopRequest.Trips[3].SearchNearbyDestinationAirports,
                                    ShopRequest.Application.Id, ShopRequest.Application.Version.Major, isStandardRevenueSearch, ShopRequest.IsELFFareDisplayAtFSR, ShopRequest.FareType,
                                    false, ShopRequest.Trips[3].OriginAllAirports, ShopRequest.Trips[3].DestinationAllAirports);

                    if (trip != null)
                    {
                        trip.TripIndex = 4;
                        if (trip.SearchFiltersIn == null)
                        {
                            trip.SearchFiltersIn = new SearchFilterInfo();
                        }
                        trip.SearchFiltersIn.FareFamily = GetFareFamily(ShopRequest.Trips[3].Cabin, ShopRequest.FareType);
                        shopRequest.Trips.Add(trip);
                    }
                }
                if (ShopRequest.Trips.Count > 4)
                {
                    trip = GetTrip(ShopRequest.Trips[4].Origin, ShopRequest.Trips[4].Destination, ShopRequest.Trips[4].DepartDate, ShopRequest.Trips[4].Cabin, ShopRequest.Trips[4].UseFilters, ShopRequest.Trips[4].SearchFiltersIn, ShopRequest.Trips[4].SearchNearbyOriginAirports, ShopRequest.Trips[4].SearchNearbyDestinationAirports,
                                    ShopRequest.Application.Id, ShopRequest.Application.Version.Major, isStandardRevenueSearch, ShopRequest.IsELFFareDisplayAtFSR, ShopRequest.FareType,
                                    false, ShopRequest.Trips[4].OriginAllAirports, ShopRequest.Trips[4].DestinationAllAirports);
                    if (trip != null)
                    {
                        trip.TripIndex = 5;
                        if (trip.SearchFiltersIn == null)
                        {
                            trip.SearchFiltersIn = new SearchFilterInfo();
                        }
                        trip.SearchFiltersIn.FareFamily = GetFareFamily(ShopRequest.Trips[4].Cabin, ShopRequest.FareType);
                        shopRequest.Trips.Add(trip);
                    }
                }
                if (ShopRequest.Trips.Count > 5)
                {
                    trip = GetTrip(ShopRequest.Trips[5].Origin, ShopRequest.Trips[5].Destination, ShopRequest.Trips[5].DepartDate, ShopRequest.Trips[5].Cabin, ShopRequest.Trips[5].UseFilters, ShopRequest.Trips[5].SearchFiltersIn, ShopRequest.Trips[5].SearchNearbyOriginAirports, ShopRequest.Trips[5].SearchNearbyDestinationAirports,
                                    ShopRequest.Application.Id, ShopRequest.Application.Version.Major, isStandardRevenueSearch, ShopRequest.IsELFFareDisplayAtFSR, ShopRequest.FareType,
                                    false, ShopRequest.Trips[5].OriginAllAirports, ShopRequest.Trips[5].DestinationAllAirports);
                    if (trip != null)
                    {
                        trip.TripIndex = 6;
                        if (trip.SearchFiltersIn == null)
                        {
                            trip.SearchFiltersIn = new SearchFilterInfo();
                        }
                        trip.SearchFiltersIn.FareFamily = GetFareFamily(ShopRequest.Trips[5].Cabin, ShopRequest.FareType);
                        shopRequest.Trips.Add(trip);
                    }
                }
            }

            if ((!ShopRequest.IsReshopChange || ShopRequest.ReshopTravelers == null || ShopRequest.ReshopTravelers.Count == 0))
            {

                shopRequest.PaxInfoList = new List<PaxInfo>();

                PaxInfo paxInfo = null;
                if (_shoppingUtility.EnableYoungAdult(ShopRequest.IsReshop) && ShopRequest.IsYoungAdultBooking)
                {
                    shopRequest.PaxInfoList.Add(GetYAPaxInfo());
                }
                else
                {
                    if (_shoppingUtility.EnableTravelerTypes(ShopRequest.Application.Id, ShopRequest.Application.Version.Major) && ShopRequest.TravelerTypes != null && ShopRequest.TravelerTypes.Count > 0)
                    {
                        GetPaxInfo(ShopRequest, shopRequest);
                    }
                    else
                    {
                        if ((ShopRequest.NumberOfAdults > 0 || ShopRequest.NumberOfSeniors > 0 || ShopRequest.NumberOfChildren5To11 > 0 || ShopRequest.NumberOfChildren12To17 > 0))
                        {
                            for (int i = 0; i < ShopRequest.NumberOfAdults; i++)
                            {
                                paxInfo = new PaxInfo
                                {
                                    PaxType = PaxType.Adult,
                                    DateOfBirth = DateTime.Today.AddYears(-20).ToShortDateString()
                                };
                                AssignPTCValue(ShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < ShopRequest.NumberOfSeniors; i++)
                            {
                                paxInfo = new PaxInfo
                                {
                                    PaxType = PaxType.Senior,
                                    DateOfBirth = DateTime.Today.AddYears(-67).ToShortDateString()
                                };
                                AssignPTCValue(ShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < ShopRequest.NumberOfChildren2To4; i++)
                            {
                                paxInfo = new PaxInfo
                                {
                                    PaxType = PaxType.Child01,
                                    DateOfBirth = DateTime.Today.AddYears(-3).ToShortDateString()
                                };
                                AssignPTCValue(ShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < ShopRequest.NumberOfChildren5To11; i++)
                            {
                                paxInfo = new PaxInfo
                                {
                                    PaxType = PaxType.Child02,
                                    DateOfBirth = DateTime.Today.AddYears(-8).ToShortDateString()
                                };
                                AssignPTCValue(ShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < ShopRequest.NumberOfChildren12To17; i++)
                            {
                                paxInfo = new PaxInfo
                                {
                                    PaxType = PaxType.Child03,
                                    DateOfBirth = DateTime.Today.AddYears(-15).ToShortDateString()
                                };
                                AssignPTCValue(ShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < ShopRequest.NumberOfInfantOnLap; i++)
                            {
                                paxInfo = new PaxInfo
                                {
                                    PaxType = PaxType.InfantLap,
                                    DateOfBirth = DateTime.Today.AddYears(-1).ToShortDateString()
                                };
                                AssignPTCValue(ShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < ShopRequest.NumberOfInfantWithSeat; i++)
                            {
                                paxInfo = new PaxInfo
                                {
                                    PaxType = PaxType.InfantSeat,
                                    DateOfBirth = DateTime.Today.AddYears(-1).ToShortDateString()
                                };
                                AssignPTCValue(ShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);

                            }
                        }
                    }
                }
            }
            else if (ShopRequest.IsReshopChange)
            {
                var cslReservation = await _sessionHelperService.GetSession<ReservationDetail>(shopRequest.SessionId, new ReservationDetail().GetType().FullName, new List<string> { shopRequest.SessionId, new ReservationDetail().GetType().FullName }).ConfigureAwait(false);
                shopRequest.PaxInfoList = GetReshopPaxInfoList(ShopRequest, cslReservation);
            }
            else
            {
                throw new MOBUnitedException("You must specify at least one passenger.");
            }

            shopRequest.AwardTravel = ShopRequest.AwardTravel;
            shopRequest.SearchType = AvailabilitySearchType.ValueNotSet;
            shopRequest.SearchTypeSelection = GetSearchTypeSelection(ShopRequest.SearchType);
            shopRequest.ServiceType = United.Services.FlightShopping.Common.ServiceType.Boombox;
            shopRequest.Stops = ShopRequest.MaxNumberOfStops;
            shopRequest.StopsInclusive = true;
            shopRequest.ChannelType = "MOBILE";
            shopRequest.EliteLevel = ShopRequest.PremierStatusLevel;
            shopRequest.SortType = ShopRequest.ResultSortType;
            //get account summary for mileage info
            try
            {
                string getAccountSummaryTransactionID = ShopRequest.TransactionId;
                if ((ShopRequest.AwardTravel || EnableEPlusAncillary(ShopRequest.Application.Id, ShopRequest.Application.Version.Major, ShopRequest.IsReshopChange)) && !string.IsNullOrWhiteSpace(ShopRequest.MileagePlusAccountNumber))
                {
                    if (_configuration.GetValue<string>("AddingExtraLogggingForAwardShopAccountSummary_Toggle") != null
                        && (_configuration.GetValue<bool>("AddingExtraLogggingForAwardShopAccountSummary_Toggle")))
                    {
                        //    logEntries.Add(LogEntry.GetLogEntry<string>(ShopRequest.SessionId, "ShopRequest - TransactionId", "TransactionId", ShopRequest.Application.Id, ShopRequest.Application.Version.Major, ShopRequest.DeviceId, ShopRequest.TransactionId));

                        if (!string.IsNullOrWhiteSpace(ShopRequest.SessionId))
                        {
                            getAccountSummaryTransactionID = ShopRequest.SessionId;
                        }
                    }
                    var summary = await _mileagePlus.GetAccountSummary(getAccountSummaryTransactionID, ShopRequest.MileagePlusAccountNumber, "en-US", false, ShopRequest.SessionId).ConfigureAwait(false);
                    shopRequest.LoyaltyPerson = new Service.Presentation.PersonModel.LoyaltyPerson
                    {
                        LoyaltyProgramMemberID = summary.MileagePlusNumber,
                        LoyaltyProgramMemberTierLevel = (Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel)summary.EliteStatus.Level,
                        AccountBalances = new Collection<Service.Presentation.CommonModel.LoyaltyAccountBalance>()
                    };
                    Service.Presentation.CommonModel.LoyaltyAccountBalance balance = new Service.Presentation.CommonModel.LoyaltyAccountBalance();
                    int bal = 0;
                    int.TryParse(summary.Balance, out bal);
                    balance.Balance = bal;
                    balance.BalanceType = Service.Presentation.CommonEnumModel.LoyaltyAccountBalanceType.MilesBalance;
                    shopRequest.LoyaltyPerson.AccountBalances.Add(balance);
                }
                else if (ShopRequest.AwardTravel && string.IsNullOrWhiteSpace(ShopRequest.MileagePlusAccountNumber))
                {
                    if (_configuration.GetValue<string>("AddingExtraLogggingForAwardShopAccountSummary_Toggle") != null
                        && _configuration.GetValue<bool>("AddingExtraLogggingForAwardShopAccountSummary_Toggle"))
                    {

                        /*logEntries.Add(LogEntry.GetLogEntry<ShopRequest>(ShopRequest.SessionId, "GetShopRequest-AwardTravel_EmptyMpNumber", "Exception", ShopRequest.Application.Id, ShopRequest.Application.Version.Major, ShopRequest.DeviceId, ShopRequest, true, false))*/
                        ;

                    }
                }
            }
            catch (Exception)
            {
                if (_configuration.GetValue<string>("AddingExtraLogggingForAwardShopAccountSummary_Toggle") != null
                    && _configuration.GetValue<bool>("AddingExtraLogggingForAwardShopAccountSummary_Toggle"))
                {

                    //logEntries.Add(LogEntry.GetLogEntry<string>(ShopRequest.SessionId, "GetShopRequest - GetAccountSummary", "Exception", ShopRequest.Application.Id, ShopRequest.Application.Version.Major, ShopRequest.DeviceId, ex.Message));

                }
            };
            //if (_configuration.GetValue<string>("AddingExtraLogggingForAwardShopAccountSummary_Toggle") != null
            //&& (_configuration.GetValue<bool>("AddingExtraLogggingForAwardShopAccountSummary_Toggle")
            //&& mp != null && mp.LogEntries != null))
            //{
            //   // logEntries.AddRange(mp.LogEntries);
            //}


            if (!string.IsNullOrEmpty(ShopRequest.EmployeeDiscountId))
            {
                shopRequest.EmployeeDiscountId = ShopRequest.EmployeeDiscountId;
            }
            shopRequest.NGRP = _configuration.GetValue<string>("NGRPSwitchONOFFValue") != null ? _configuration.GetValue<bool>("NGRPSwitchONOFFValue") : false;
            #region
            AssignCalendarLengthOfStay(ShopRequest.LengthOfCalendar, shopRequest);
            #endregion
            //persis CSL shop request so we nave Loyalty info without making multiple summary calls
            CSLShopRequest cslShopRequest = new CSLShopRequest();
            if (ShopRequest.IsReshopChange)
            {
                shopRequest.ConfirmationID = ShopRequest.RecordLocator;
                shopRequest.DisableMostRestrictive = false;
                var cslReservation = await _sessionHelperService.GetSession<ReservationDetail>(ShopRequest.SessionId, new ReservationDetail().GetType().FullName, new List<string> { shopRequest.SessionId, new ReservationDetail().GetType().FullName }).ConfigureAwait(false);
                if (cslReservation != null && cslReservation.Detail != null)
                    shopRequest.reservation = cslReservation.Detail;

                string riskFreePolicy24Hr = ShopStaticUtility.GetCharacteristicDescription(cslReservation.Detail.Characteristic.ToList(), "24HrFlexibleBookingPolicy");
                if (!string.IsNullOrEmpty(riskFreePolicy24Hr))
                {
                    shopRequest.RiskFreePolicy = riskFreePolicy24Hr;
                }
            }
            cslShopRequest.ShopRequest = shopRequest;
           await _sessionHelperService.SaveSession<United.Mobile.Model.Shopping.CSLShopRequest>(cslShopRequest, ShopRequest.SessionId,  new List<string> { cslShopRequest.SessionId, cslShopRequest.ObjectName}, cslShopRequest.ObjectName).ConfigureAwait(false);
            #region Corporate Booking
            bool isCorporateBooking = _configuration.GetValue<bool>("CorporateConcurBooking");
            if ((isCorporateBooking && ShopRequest.IsCorporateBooking) || ShopRequest.TravelType == TravelType.CLB.ToString())
            {
                shopRequest.CorporateTravelProvider = ShopRequest.MOBCPCorporateDetails.CorporateTravelProvider;
                shopRequest.CorporationName = ShopRequest.MOBCPCorporateDetails.CorporateCompanyName;
                shopRequest.SpecialPricingInfo = new SpecialPricingInfo();
                //TODO:Review with rajesh ..faregroupID is required for corporate lesiure
                if (_configuration.GetValue<bool>("EnableCorporateLeisure"))
                {
                    if (!string.IsNullOrEmpty(ShopRequest.MOBCPCorporateDetails?.DiscountCode) && ShopRequest.TravelType != TravelType.CLB.ToString())
                    {
                        shopRequest.SpecialPricingInfo.AccountCode = ShopRequest.MOBCPCorporateDetails.DiscountCode;
                        shopRequest.SpecialPricingInfo.PromoType = PromoType.CorporateDiscount;
                    }
                    else if (!string.IsNullOrEmpty(ShopRequest.MOBCPCorporateDetails?.LeisureDiscountCode) && ShopRequest.TravelType == TravelType.CLB.ToString())
                    {
                        shopRequest.SpecialPricingInfo.AccountCode = ShopRequest.MOBCPCorporateDetails.LeisureDiscountCode;
                        shopRequest.SpecialPricingInfo.PromoType = PromoType.CorporateDiscountLeisure;
                    }
                }
                else
                {
                    shopRequest.SpecialPricingInfo.AccountCode = ShopRequest.MOBCPCorporateDetails.DiscountCode;
                    shopRequest.SpecialPricingInfo.PromoType = PromoType.CorporateDiscount;
                }
                shopRequest.SpecialPricingInfo.FareGroupID = ShopRequest.MOBCPCorporateDetails.FareGroupId;// Passed from the GetProfile  call; XBE - Xclude BE; Null means include BE
                                                                                                           // This Value is 10 to get CORPDISC "Corporate Discount Fare"
            }
            #endregion

            if (_configuration.GetValue<bool>("EnableNonStopFlight") && (ShopRequest.GetNonStopFlightsOnly || ShopRequest.GetFlightsWithStops))
            {
                await RequestForNonStopFlights(ShopRequest, shopRequest);
                if (ShopRequest.GetFlightsWithStops)
                {
                    Session session = new Session();
                    session = await _sessionHelperService.GetSession<Session>(shopRequest.SessionId, session.ObjectName, new List<string> { shopRequest.SessionId, session.ObjectName }).ConfigureAwait(false);
                    if (session == null)
                    {
                        throw new MOBUnitedException("Your session has expired. Please start a new search.");
                    }
                    shopRequest.CartId = session.CartId;
                }
            }
            return shopRequest;
        }

        private bool CheckFSRRedesignFromShopRequest(MOBSHOPShopRequest request)
        {
            return _shoppingUtility.CheckFSRRedesignFromShop(request);
        }

        private int GetFlexibleDaysBefore()
        {
            int flexibleFareDaysBefore = 0;
            int.TryParse(_configuration.GetValue<string>("AffinitySearchFlexibleDaysBefore"), out flexibleFareDaysBefore);

            return flexibleFareDaysBefore;
        }

        private int GetFlexibleDaysAfter()
        {
            int flexibleFareDaysAfter = 0;
            int.TryParse(_configuration.GetValue<string>("AffinitySearchFlexibleDaysAfter"), out flexibleFareDaysAfter);

            return flexibleFareDaysAfter;
        }

        private int GetShoppingSearchMaxTrips()
        {
            int maxTrips = 125;
            int.TryParse(_configuration.GetValue<string>("ShoppingSearchMaxTrips"), out maxTrips);

            return maxTrips;
        }

        private async Task<MOBSHOPAvailability> GetLastTripAvailabilityFromPersist(int lastTripIndexRequested, string sessionID)
        {
            #region
            MOBSHOPAvailability lastTripAvailability = null;
            LatestShopAvailabilityResponse persistAvailability = new LatestShopAvailabilityResponse();
            persistAvailability = await _sessionHelperService.GetSession<LatestShopAvailabilityResponse>(sessionID, persistAvailability.ObjectName, new List<string> { sessionID, persistAvailability.ObjectName }).ConfigureAwait(false);

            if (persistAvailability != null && persistAvailability.AvailabilityList != null && persistAvailability.AvailabilityList.Count > 0 && persistAvailability.AvailabilityList[lastTripIndexRequested.ToString()] != null)
            {
                lastTripAvailability = persistAvailability.AvailabilityList[lastTripIndexRequested.ToString()];
            }

            return lastTripAvailability;
            #endregion

        }

        private async Task<(List<Trip> tripsList,  bool isOverride24HrFlex)> GetReshopTripsList(MOBSHOPShopRequest mobShopShopRequest)
        {
            var cslReservation = await _sessionHelperService.GetSession<ReservationDetail>(mobShopShopRequest.SessionId, new ReservationDetail().GetType().FullName, new List<string> { mobShopShopRequest.SessionId, new ReservationDetail().GetType().FullName }).ConfigureAwait(false);
            var persistedReservation = await _sessionHelperService.GetSession<Reservation>(mobShopShopRequest.SessionId, (new Reservation()).ObjectName, new List<string> { mobShopShopRequest.SessionId, (new Reservation()).ObjectName }).ConfigureAwait(false);

            List<Trip> tripsList = new List<Trip>();
            bool isOverride24HrFlex;
            int segementNumber = 0;

            isOverride24HrFlex = (persistedReservation == null) ? false : persistedReservation.Override24HrFlex;

            if (persistedReservation.Reshop.IsUsedPNR)
            {
                List<MOBSHOPTripBase> usedTripBase = new List<MOBSHOPTripBase>();
                List<MOBSHOPTripBase> requestTripBase = mobShopShopRequest.Trips;
                foreach (var trip in persistedReservation.ReshopUsedTrips)
                {
                    MOBSHOPTripBase tripbase = ConvertReshopTripToTripBase(trip);
                    usedTripBase.Add(tripbase);
                }
                int tripRequestIndex = 0;
                foreach (var tripRequest in mobShopShopRequest.Trips)
                {
                    usedTripBase.Add(tripRequest);
                    var mOBSHOPReShopTrip = persistedReservation.ReshopTrips[tripRequestIndex];
                    mOBSHOPReShopTrip.IsReshopTrip = (tripRequest.ChangeType == 0);
                    mOBSHOPReShopTrip.ChangeTripTitle = (_configuration.GetValue<string>("ReshopChange-RTIFlightBlockTitle") ?? "");
                    tripRequestIndex++;
                }
                requestTripBase = usedTripBase;
                int requestTripIndex = Convert.ToInt32(cslReservation.Detail.FlightSegments.Min(p => p.TripNumber));

                foreach (var requestTrip in requestTripBase)
                {
                    tripsList.Add(GetTripForReshopChangeRequestUsingMobRequestAndCslReservationSegments(requestTrip, segementNumber, mobShopShopRequest.FareType, cslReservation.Detail.FlightSegments.ToList(), persistedReservation.Reshop.IsUsedPNR, requestTripIndex));
                    segementNumber++;
                    requestTripIndex++;
                }

               await _sessionHelperService.SaveSession<Reservation>(persistedReservation, mobShopShopRequest.SessionId, new List<string> { mobShopShopRequest.SessionId, persistedReservation.ObjectName }, persistedReservation.ObjectName).ConfigureAwait(false);
                return (tripsList,isOverride24HrFlex);
            }

            foreach (var requestTrip in mobShopShopRequest.Trips)
            {
                tripsList.Add(GetTripForReshopChangeRequestUsingMobRequestAndCslReservationSegments(requestTrip, segementNumber, mobShopShopRequest.FareType, cslReservation.Detail.FlightSegments.ToList()));
                segementNumber++;

                if (persistedReservation.ReshopTrips.Count >= tripsList.Count)
                {
                    var mOBSHOPReShopTrip = persistedReservation.ReshopTrips[tripsList.Count - 1];
                    mOBSHOPReShopTrip.IsReshopTrip = (tripsList[tripsList.Count - 1].Flights == null || tripsList[tripsList.Count - 1].Flights.Count == 0);
                    mOBSHOPReShopTrip.ChangeTripTitle = (_configuration.GetValue<string>("ReshopChange-RTIFlightBlockTitle") ?? "");
                }
            }


            await _sessionHelperService.SaveSession<Reservation>(persistedReservation, mobShopShopRequest.SessionId, new List<string> { mobShopShopRequest.SessionId, persistedReservation.ObjectName }, persistedReservation.ObjectName).ConfigureAwait(false);
            return (tripsList,isOverride24HrFlex);
        }

        private MOBSHOPTripBase ConvertReshopTripToTripBase(ReshopTrip reshopTrip)
        {
            MOBSHOPTripBase tripBase = new MOBSHOPTripBase();
            tripBase = reshopTrip.OriginalTrip;
            tripBase.DepartDate = reshopTrip.OriginalTrip.DepartDate;

            return tripBase;
        }

        private Trip GetTripForReshopChangeRequestUsingMobRequestAndCslReservationSegments(MOBSHOPTripBase mobSHOPTripBase, int segmentnumber, string fareType, List<ReservationFlightSegment> cslReservationFlightSegment, bool isused = false, int originalTripIndex = 0)
        {
            Trip trip = null;
            string segmentCabin;

            trip = GetTrip(mobSHOPTripBase.Origin, mobSHOPTripBase.Destination,
               mobSHOPTripBase.DepartDate, mobSHOPTripBase.Cabin,
               mobSHOPTripBase.UseFilters, mobSHOPTripBase.SearchFiltersIn,
               mobSHOPTripBase.SearchNearbyOriginAirports,
               mobSHOPTripBase.SearchNearbyDestinationAirports,
               -1, "", false, false, "", isused);

            trip.ChangeType = (int)mobSHOPTripBase.ChangeType;
            segmentCabin = mobSHOPTripBase.Cabin;
            trip.TripIndex = segmentnumber + 1;

            if (trip.SearchFiltersIn == null)
            {
                trip.SearchFiltersIn = new SearchFilterInfo();
            }
            trip.SearchFiltersIn.FareFamily = GetFareFamily(segmentCabin, fareType);

            if (trip.ChangeType == 3)
            {
                var tripUsedIndex = cslReservationFlightSegment.Where(p => p.TripNumber == trip.TripIndex.ToString());
                if (isused)
                {
                    int index = Convert.ToInt32(trip.TripIndex);
                    tripUsedIndex = cslReservationFlightSegment.Where(p => p.TripNumber == originalTripIndex.ToString());
                }
                foreach (var cslTripSegments in tripUsedIndex)
                {
                    Flight flight = new Flight
                    {
                        FlightNumber = cslTripSegments.FlightSegment.FlightNumber,
                        OperatingCarrier = cslTripSegments.FlightSegment.OperatingAirlineCode,
                        Origin = cslTripSegments.FlightSegment.DepartureAirport.IATACode,
                        Destination = cslTripSegments.FlightSegment.ArrivalAirport.IATACode,
                        DepartDateTime = cslTripSegments.FlightSegment.DepartureDateTime
                    };
                    trip.Flights.Add(flight);
                }
            }

            return trip;
        }

        private string GetCabinPreference(CabinType cabinType)
        {
            string ct = string.Empty;

            //add premium cabin string to align columns to shopping products
            if (cabinType == CabinType.Business ||
                cabinType == CabinType.BusinessFirst ||
                cabinType == CabinType.First)
            {
                ct = "premium";
            }

            return ct;
        }

        private Trip GetTrip(string origin, string destination, string departureDate, string cabin, bool useFilters, MOBSearchFilters filters, bool searchNearbyOrigins, bool searchNearbyDestinations,
                             int appId = -1, string appVersion = "", bool isStandardRevenueSearch = false, bool isELFFareDisplayAtFSR = false, string fareType = "",
                             bool isUsed = false, int originAllAirports = -1, int destinationAllAirports = -1)
        {
            Trip trip = null;

            if (!string.IsNullOrEmpty(origin) && !string.IsNullOrEmpty(destination) && origin.Length == 3 && destination.Length == 3 && IsValidDate(departureDate, true, isUsed))
            {
                trip = new Trip
                {
                    Origin = origin
                };

                // MB-2639 add all airports flag to csl shop call
                if (_configuration.GetValue<bool>("EnableAllAirportsFlightSearch") && originAllAirports != -1 && destinationAllAirports != -1)
                {
                    trip.OriginAllAirports = originAllAirports == 1 ? true : false;
                    trip.DestinationAllAirports = destinationAllAirports == 1 ? true : false;
                }
                //if (_configuration.GetValue<string>("CityCodeToReturnAllAirportsFlightSearch") != null && _configuration.GetValue<string>("CityCodeToReturnAllAirportsFlightSearch").Contains(origin))
                //{
                //    trip.OriginAllAirports = true;
                //}
                //if (_configuration.GetValue<string>("CityCodeToReturnAllAirportsFlightSearch") != null && _configuration.GetValue<string>("CityCodeToReturnAllAirportsFlightSearch").Contains(destination))
                //{
                //    trip.DestinationAllAirports = true;
                //}

                trip.Destination = destination;
                trip.DepartDate = departureDate;
                if (searchNearbyDestinations)
                    trip.SearchRadiusMilesDestination = GetSearchRadiusForNearbyAirports();
                if (searchNearbyOrigins)
                    trip.SearchRadiusMilesOrigin = GetSearchRadiusForNearbyAirports();
                trip.CabinType = GetCabinType(cabin);
                trip.SearchFiltersIn = GetSearchFilters(filters, appId, appVersion, isStandardRevenueSearch, isELFFareDisplayAtFSR, fareType);
                trip.UseFilters = useFilters;

            }

            return trip;
        }

        private CabinType GetCabinType(string cabin)
        {
            CabinType cabinType = new CabinType();

            if (string.IsNullOrEmpty(cabin))
            {
                cabinType = CabinType.ValueNotSet;
            }
            else
            {
                switch (cabin.ToUpper().Trim())
                {
                    case "ECON":
                    case "COACH":
                        cabinType = CabinType.Coach;
                        break;
                    case "FIRST":
                        cabinType = CabinType.First;
                        break;
                    case "BUSINESS":
                        cabinType = CabinType.Business;
                        break;
                    case "BUSINESSFIRST":
                        cabinType = CabinType.BusinessFirst;
                        break;
                    default:
                        cabinType = CabinType.ValueNotSet;
                        break;
                }
            }

            return cabinType;
        }

        private SearchFilterInfo GetSearchFilters(MOBSearchFilters filters, int appId, string appVersion, bool isStandardRevenueSearch, bool isELFFareDisplayAtFSR, string fareType)
        {
            SearchFilterInfo filter = new SearchFilterInfo();

            if (filters != null)
            {
                if (!String.IsNullOrEmpty(filters.AircraftTypes))
                {
                    filter.AircraftTypes = filters.AircraftTypes;
                }

                if (!String.IsNullOrEmpty(filters.AirportsDestination))
                {
                    filter.AirportsDestination = filters.AirportsDestination;
                }

                if (filters.AirportsDestinationList != null && filters.AirportsDestinationList.Count > 0)
                {
                    filter.AirportsDestinationList = new List<CodeDescPair>();
                    foreach (MOBSearchFilterItem kvp in filters.AirportsDestinationList)
                    {
                        CodeDescPair cdp = new CodeDescPair
                        {
                            Code = kvp.Key,
                            Description = kvp.Value,
                            Amount = kvp.Amount,
                            Currency = kvp.Currency
                        };

                        filter.AirportsDestinationList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.AirportsOrigin))
                {
                    filter.AirportsOrigin = filters.AirportsOrigin;
                }

                if (filters.AirportsOriginList != null && filters.AirportsOriginList.Count > 0)
                {
                    filter.AirportsOriginList = new List<CodeDescPair>();
                    foreach (MOBSearchFilterItem kvp in filters.AirportsOriginList)
                    {
                        CodeDescPair cdp = new CodeDescPair
                        {
                            Code = kvp.Key,
                            Description = kvp.Value,
                            Amount = kvp.Amount,
                            Currency = kvp.Currency
                        };

                        filter.AirportsOriginList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.AirportsStop))
                {
                    filter.AirportsStop = filters.AirportsStop;
                }

                if (filters.AirportsStopList != null && filters.AirportsStopList.Count > 0)
                {
                    filter.AirportsStopList = new List<CodeDescPair>();
                    foreach (MOBSearchFilterItem kvp in filters.AirportsStopList)
                    {
                        CodeDescPair cdp = new CodeDescPair
                        {
                            Code = kvp.Key,
                            Description = kvp.Value,
                            Amount = kvp.Amount,
                            Currency = kvp.Currency
                        };

                        filter.AirportsStopList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.AirportsStopToAvoid))
                {
                    filter.AirportsStopToAvoid = filters.AirportsStopToAvoid;
                }

                if (filters.AirportsStopToAvoidList != null && filters.AirportsStopToAvoidList.Count > 0)
                {
                    filter.AirportsStopToAvoidList = new List<CodeDescPair>();
                    foreach (MOBSearchFilterItem kvp in filters.AirportsStopToAvoidList)
                    {
                        CodeDescPair cdp = new CodeDescPair
                        {
                            Code = kvp.Key,
                            Description = kvp.Value,
                            Amount = kvp.Amount,
                            Currency = kvp.Currency
                        };

                        filter.AirportsStopToAvoidList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.BookingCodes))
                {
                    filter.BookingCodes = filters.BookingCodes;
                }

                if (!String.IsNullOrEmpty(filters.CarriersMarketing))
                {
                    filter.CarriersMarketing = filters.CarriersMarketing;
                }

                if (filters.CarriersMarketingList != null && filters.CarriersMarketingList.Count > 0)
                {
                    filter.CarriersMarketingList = new List<CodeDescPair>();
                    foreach (MOBSearchFilterItem kvp in filters.CarriersMarketingList)
                    {
                        CodeDescPair cdp = new CodeDescPair
                        {
                            Code = kvp.Key,
                            Description = kvp.Value,
                            Amount = kvp.Amount,
                            Currency = kvp.Currency
                        };

                        filter.CarriersMarketingList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.CarriersOperating))
                {
                    filter.CarriersOperating = filters.CarriersOperating;
                }

                if (filters.CarriersOperatingList != null && filters.CarriersOperatingList.Count > 0)
                {
                    filter.CarriersOperatingList = new List<CodeDescPair>();
                    foreach (MOBSearchFilterItem kvp in filters.CarriersOperatingList)
                    {
                        CodeDescPair cdp = new CodeDescPair
                        {
                            Code = kvp.Key,
                            Description = kvp.Value,
                            Amount = kvp.Amount,
                            Currency = kvp.Currency
                        };

                        filter.CarriersOperatingList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.EquipmentCodes))
                {
                    filter.EquipmentCodes = filters.EquipmentCodes;
                }

                if (filters.EquipmentList != null && filters.EquipmentList.Count > 0)
                {
                    filter.EquipmentList = new List<CodeDescPair>();
                    foreach (MOBSearchFilterItem kvp in filters.EquipmentList)
                    {
                        CodeDescPair cdp = new CodeDescPair
                        {
                            Code = kvp.Key,
                            Description = kvp.Value,
                            Amount = kvp.Amount,
                            Currency = kvp.Currency
                        };

                        filter.EquipmentList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.EquipmentTypes))
                {
                    filter.EquipmentTypes = filters.EquipmentTypes;
                }

                if (filters.FareFamilies != null && filters.FareFamilies.Count > 0)
                {
                    filter.FareFamilies = new sliceFareFamilies
                    {
                        fareFamily = new fareFamilyType[filters.FareFamilies.Count]
                    };
                    int cnt = 0;
                    foreach (MOBSHOPFareFamily ff in filters.FareFamilies)
                    {

                        fareFamilyType fft = new fareFamilyType
                        {
                            fareFamily = string.IsNullOrEmpty(ff.FareFamily) ? "" : ff.FareFamily,
                            maxMileage = ff.MaxMileage
                        };
                        if (!string.IsNullOrEmpty(ff.MaxPrice))
                        {
                            fft.maxPrice = new price
                            {
                                amount = Convert.ToDecimal(ff.MaxPrice)
                            };
                        }
                        fft.minMileage = ff.MinMileage;
                        if (!string.IsNullOrEmpty(ff.MinPrice))
                        {
                            fft.minPrice = new price
                            {
                                amount = Convert.ToDecimal(ff.MinPrice)
                            };
                        }
                        fft.minPriceInSummary = ff.MinPriceInSummary;
                        filter.FareFamilies.fareFamily[cnt] = fft;
                        cnt++;
                    }
                }

                if (!String.IsNullOrEmpty(filters.FareFamily))
                {
                    filter.FareFamily = filters.FareFamily;
                }

                if (!String.IsNullOrEmpty(filters.TimeArrivalMax))
                {
                    filter.TimeArrivalMax = filters.TimeArrivalMax;
                }

                if (!String.IsNullOrEmpty(filters.TimeArrivalMin))
                {
                    filter.TimeArrivalMin = filters.TimeArrivalMin;
                }

                if (!String.IsNullOrEmpty(filters.TimeDepartMax))
                {
                    filter.TimeDepartMax = filters.TimeDepartMax;
                }

                if (!String.IsNullOrEmpty(filters.TimeDepartMin))
                {
                    filter.TimeDepartMin = filters.TimeDepartMin;
                }

                if (filters.WarningsFilter != null && filters.WarningsFilter.Count > 0)
                {
                    foreach (MOBSearchFilterItem warningFilter in filters.WarningsFilter)
                    {
                        filter.Warnings.Add(warningFilter.Key);
                    }
                }

                filter.CabinCountMax = filters.CabinCountMax;
                filter.CabinCountMin = filters.CabinCountMin;
                filter.CarrierDefault = filters.CarrierDefault;
                filter.CarrierExpress = filters.CarrierExpress;
                filter.CarrierPartners = filters.CarrierPartners;
                filter.CarrierStar = filters.CarrierStar;
                filter.DurationMax = filters.DurationMax;
                filter.DurationMin = filters.DurationMin;
                filter.DurationStopMax = filters.DurationStopMax;
                filter.DurationStopMin = filters.DurationStopMin;
                filter.PriceMax = filters.PriceMax;
                filter.PriceMin = filters.PriceMin;
                filter.StopCountExcl = filters.StopCountExcl;
                filter.StopCountMax = filters.StopCountMax;
                filter.StopCountMin = filters.StopCountMin;
            }
            else //set default values
            {
                filter.FareFamily = "ECONOMY";
            }
            // Refundable fares toggle feature
            AddRefundableFaresToggleFilter(filter, filters, appId, appVersion, isStandardRevenueSearch, isELFFareDisplayAtFSR, fareType);
            // Mixed Cabin toggle feature
            AddMixedCabinToggleFilter(filter, filters, appId, appVersion, isStandardRevenueSearch, isELFFareDisplayAtFSR, fareType);


            return filter;
        }

        private int GetSearchRadiusForNearbyAirports()
        {
            int searchRadius = 150;
            int.TryParse(_configuration.GetValue<string>("SearchRadiusForNearbyAirports"), out searchRadius);

            return searchRadius;
        }

        private bool IsValidDate(string dateString, bool notPastDate, bool used = false)
        {
            bool result = false;

            if (used)
                return used;

            DateTime date = new DateTime(0);
            DateTime.TryParse(dateString, out date);

            if (date.Ticks != 0)
            {
                if (notPastDate)
                {
                    date = new DateTime(date.Year, date.Month, date.Day);
                    if (date >=
                      (_configuration.GetValue<bool>("InvalidFlightdateFix")
                      ? DateTime.Today.AddHours(-12).Date
                      : DateTime.Today))
                    {
                        result = true;
                    }
                    else
                    {
                        throw new MOBUnitedException(_configuration.GetValue<string>("PastBookingDateErrorMessage"));
                    }
                }
                else
                {
                    result = true;
                }
            }

            return result;
        }

        private string GetFareFamily(string cabinSearched, string fareType)
        {
            string cabin = "ECONOMY";

            switch (cabinSearched.Trim().ToLower())
            {
                case "econ":
                    cabin = "ECONOMY";
                    break;
                case "business":
                case "businessfirst":
                    cabin = "BUSINESS";
                    break;
                case "first":
                    cabin = "FIRST";
                    break;
                case "awardecon":
                    cabin = "AWARDECONOMY";
                    break;
                case "awardbusiness":
                case "awardbusinessfirst":
                    cabin = "AWARDBUSINESSFIRST";
                    break;
                case "awardfirst":
                    cabin = "AWARDFIRST";
                    break;
                default:
                    cabin = "ECONOMY";
                    break;
            }

            string FareType = "";

            switch (fareType.Trim().ToLower())
            {
                case "lf":
                    FareType = "";
                    break;
                case "ff":
                    FareType = "-FLEXIBLE";
                    break;
                case "urf":
                    FareType = "-UNRESTRICTED";
                    break;
                default:
                    FareType = "";
                    break;
            }

            return cabin + FareType;
        }

        private PaxInfo GetYAPaxInfo()
        {
            PaxInfo paxInfo = new PaxInfo
            {
                Characteristics = new List<United.Service.Presentation.CommonModel.Characteristic>()
            };
            paxInfo.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "YOUNGADULT", Value = "True" });
            paxInfo.DateOfBirth = DateTime.Today.AddYears(-20).ToShortDateString();
            paxInfo.PaxType = PaxType.Adult;
            return paxInfo;
        }

        private void GetPaxInfo(MOBSHOPShopRequest MOBShopShopRequest, ShopRequest shopRequest)
        {
            PaxInfo paxInfo = null;
            foreach (MOBTravelerType t in MOBShopShopRequest.TravelerTypes.Where(t => t.TravelerType != null && t.Count > 0))
            {
                int tType = (int)Enum.Parse(typeof(PAXTYPE), t.TravelerType);

                switch (tType)
                {
                    case (int)PAXTYPE.Adult:
                    default:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.Adult,
                                DateOfBirth = DateTime.Today.AddYears(-25).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Senior:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.Senior,
                                DateOfBirth = DateTime.Today.AddYears(-67).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Child2To4:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.Child01,
                                DateOfBirth = DateTime.Today.AddYears(-3).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Child5To11:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.Child02,
                                DateOfBirth = DateTime.Today.AddYears(-8).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Child12To17:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.Child03,
                                DateOfBirth = DateTime.Today.AddYears(-15).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Child12To14:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.Child04,
                                DateOfBirth = DateTime.Today.AddYears(-13).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Child15To17:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.Child05,
                                DateOfBirth = DateTime.Today.AddYears(-16).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.InfantSeat:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.InfantSeat,
                                DateOfBirth = DateTime.Today.AddYears(-1).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.InfantLap:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.InfantLap,
                                DateOfBirth = DateTime.Today.AddYears(-1).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;
                }
            }
        }

        private void AssignPTCValue(MOBSHOPShopRequest MOBShopShopRequest, PaxInfo paxInfo)
        {
            bool isAwardCalendarMP2017 = _configuration.GetValue<bool>("NGRPAwardCalendarMP2017Switch");
            if (MOBShopShopRequest.AwardTravel)
            {
                if (isAwardCalendarMP2017 && MOBShopShopRequest.CustomerMetrics != null) //No null Check for PTCCode because we can pass empty string and CSL defaults PTCCode to PPR
                {
                    string ptcCode = string.Empty;
                    paxInfo.PtcList = new List<string>();
                    ptcCode = MOBShopShopRequest.CustomerMetrics.PTCCode ?? string.Empty;

                    paxInfo.PtcList.Add(ptcCode);
                }
            }
        }

        private List<PaxInfo> GetReshopPaxInfoList(MOBSHOPShopRequest mobShopShopRequest, ReservationDetail cslReservation)
        {
            List<PaxInfo> paxInfoList = null;
            if (cslReservation != null && cslReservation.Detail != null && cslReservation.Detail.Travelers != null && cslReservation.Detail.Travelers.Count > 0)
            {
                paxInfoList = new List<PaxInfo>();
                foreach (var cslReservationTraveler in cslReservation.Detail.Travelers)
                {
                    bool isSelectedTraveler = false;
                    if (mobShopShopRequest != null && mobShopShopRequest.ReshopTravelers != null && mobShopShopRequest.ReshopTravelers.Count > 0)
                    {
                        isSelectedTraveler =
                            mobShopShopRequest.ReshopTravelers.Exists(
                                p => cslReservationTraveler.Person.Key == p.SHARESPosition);
                    }

                    if (isSelectedTraveler)
                    {
                        PaxInfo paxInfo = new PaxInfo
                        {
                            DateOfBirth = cslReservationTraveler.Person.DateOfBirth,
                            Key = cslReservationTraveler.Person.Key,
                            PaxTypeCode = cslReservationTraveler.Person.Type,
                            PaxType = GetRevenuePaxType(cslReservationTraveler.Person.Type)
                        };

                        paxInfoList.Add(paxInfo);
                    }
                }
            }
            return paxInfoList;
        }

        private PaxType GetRevenuePaxType(string pnrTravelerPersonType)
        {
            PaxType paxType = PaxType.Adult;
            if (pnrTravelerPersonType == "INS")
            {
                paxType = PaxType.InfantSeat;
            }
            else if (pnrTravelerPersonType == "C05")
            {
                paxType = PaxType.Child01;
            }
            else if (pnrTravelerPersonType == "C08")
            {
                paxType = PaxType.Child02;
            }
            else if (pnrTravelerPersonType == "C12")
            {
                paxType = PaxType.Child03;
            }
            else if (pnrTravelerPersonType == "C15")
            {
                paxType = PaxType.Child04;
            }
            else if (pnrTravelerPersonType == "C17")
            {
                paxType = PaxType.Child05;
            }
            else if (pnrTravelerPersonType == "SNR")
            {
                paxType = PaxType.Senior;
            }
            else if (pnrTravelerPersonType == "INF")
            {
                paxType = PaxType.InfantLap;
            }
            else if (pnrTravelerPersonType == "ADT")
            {
                paxType = PaxType.Adult;
            }
            return paxType;
        }

        private SearchType GetSearchTypeSelection(string searchTypeSelection)
        {
            SearchType searchType = new SearchType();

            if (string.IsNullOrEmpty(searchTypeSelection))
            {
                searchType = SearchType.ValueNotSet;
            }
            else
            {
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

            }

            return searchType;
        }

        private void AssignCalendarLengthOfStay(int lengthOfCalendar, ShopRequest shopRequest)
        {
            bool isAwardCalendarMP2017 = _configuration.GetValue<bool>("NGRPAwardCalendarMP2017Switch");
            if (shopRequest.AwardTravel)
            {
                if (isAwardCalendarMP2017 && lengthOfCalendar == 3)// 3 days calendar i.e. 3 days before and 3 days later from the search date
                {
                    shopRequest.FlexibleDaysAfter = lengthOfCalendar;
                    shopRequest.FlexibleDaysBefore = lengthOfCalendar;
                }
                else if (isAwardCalendarMP2017 && lengthOfCalendar == 6)// 7 days calendar i.e. starting from the search date
                {
                    shopRequest.FlexibleDaysAfter = lengthOfCalendar;
                    shopRequest.FlexibleDaysBefore = 0;
                }
            }
        }
        private async Task RequestForNonStopFlights(MOBSHOPShopRequest mobShopShopRequest, ShopRequest shopRequest)
        {
            if (_configuration.GetValue<bool>("EnableCodeRefactorForSavingSessionCalls") == false)
            {
                var session = await _sessionHelperService.GetSession<ShoppingResponse>(mobShopShopRequest.SessionId, (new ShoppingResponse()).ObjectName, new List<string> { mobShopShopRequest.SessionId, (new ShoppingResponse()).ObjectName }).ConfigureAwait(false);
                if (session != null && mobShopShopRequest.GetFlightsWithStops)
                    shopRequest.CartId = session.CartId;
            }

            foreach (var trip in shopRequest.Trips)
            {
                trip.UseFilters = true;
                SetStopCountsToGetNonStopFlights(mobShopShopRequest.GetNonStopFlightsOnly, mobShopShopRequest.GetFlightsWithStops, trip.SearchFiltersIn);
                break; //**nonstopchanges==>  To set use filters bool as true only for Out Bound Segment as we go live for 17G with shop call selectrip later when working on select trip then remove this break
            }
        }

        private void SetStopCountsToGetNonStopFlights(bool getNonStopFlights, bool getFlightsWithStops, SearchFilterInfo searchFiltersIn)
        {
            if ((!getNonStopFlights && !getFlightsWithStops) || (getNonStopFlights && getFlightsWithStops))
                return;

            if (searchFiltersIn == null)
                searchFiltersIn = new SearchFilterInfo();

            if (getNonStopFlights) // getNonStopFlights == true means First shop or First select trip call()
            {
                searchFiltersIn.StopCountMax = 0;
                searchFiltersIn.StopCountMin = -1;
            }
            else  // getFlightsWithStops == true means second shop or second select trip call()
            {
                searchFiltersIn.StopCountMax = -1;
                searchFiltersIn.StopCountMin = 1;
            }
        }

        private async Task<AwardCalendarResponse> SelectTripAwardDynamicCalendar(SelectTripRequest selectTripRequest, string token)
        {
            AwardCalendarResponse awardDynamicCalendarResponse = new AwardCalendarResponse();
            ShoppingResponse shopResponse = new ShoppingResponse();
            shopResponse = await _sessionHelperService.GetSession<ShoppingResponse>(selectTripRequest.SessionId, shopResponse.ObjectName, new List<string> { selectTripRequest.SessionId, shopResponse.ObjectName });
            ///shopResponse = await _sessionHelperService.GetSession<ShoppingResponse>(Headers.ContextValues, shopResponse.ObjectName);

            if (shopResponse == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }
            MOBSHOPShopRequest shopRequest = shopResponse.Request;
            //Get the trip index from request using selectTripRequest
            //Pass selectTripRequest
            //Retrieve data from persist which we saved during the RT/MT/selectTrip call 
            ShopRequest request = await GetShopRequestFromPersistUsingSelectTripRequest(selectTripRequest);
            var selectedTrip = request.Trips.FirstOrDefault(t => t.BBXCellIdSelected == selectTripRequest.ProductId);

            //if (DateTime.ParseExact(selectTripRequest.CalendarDateChange, "MM/dd/yyyy", CultureInfo.InvariantCulture) 
            //    <= DateTime.ParseExact(shopRequest.Trips[selectedTrip.TripIndex-1].DepartDate, "MM/dd/yyyy", CultureInfo.InvariantCulture))
            //{
            //    selectTripRequest.CalendarDateChange = DateTime.ParseExact(shopRequest.Trips[selectedTrip.TripIndex-1].DepartDate, "MM/dd/yyyy", CultureInfo.InvariantCulture).AddDays(1).ToString("MM/dd/yyyy");
            //}
            //request = GetShopRequestFromPersistUsingSelectTripRequest(selectTripRequest);
            var awardDynamicCalendar = await CSLAwardDynamicCalendar(selectTripRequest.SessionId, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, token, request);

            SetFilterOptionsAwardCalendar(shopRequest, awardDynamicCalendar, selectedTrip.TripIndex);

            if (shopRequest.IsReshopChange)
            {
                RemovePriceAndTaxforReshop(awardDynamicCalendar);
            }

            if (awardDynamicCalendar != null)
            {
                awardDynamicCalendarResponse.AwardDynamicCalendar = awardDynamicCalendar;
                awardDynamicCalendarResponse.AwardDynamicCalendar.IsLeftArrowDisabled = DisableLeftArrowAwardCalendar(request.DepartDateTime, selectTripRequest.LengthOfCalendar, selectedTrip.DepartDate);
                awardDynamicCalendarResponse.AwardDynamicCalendar.IsRightArrowDisabled = DisableRightArrowAwardCalendar(request.DepartDateTime, selectTripRequest.LengthOfCalendar);
            }
            awardDynamicCalendarResponse.CartId = request.CartId;
            awardDynamicCalendarResponse.SessionID = selectTripRequest.SessionId;
            awardDynamicCalendarResponse.AwardCalendarRequest = new SelectTripRequest();
            awardDynamicCalendarResponse.AwardCalendarRequest = selectTripRequest;
            awardDynamicCalendarResponse.LanguageCode = selectTripRequest.LanguageCode;
            awardDynamicCalendarResponse.TransactionId = selectTripRequest.TransactionId;
            if (shopRequest.AwardTravel && shopRequest.IsReshopChange == false)
                SetDateForwardForAward(awardDynamicCalendarResponse);

            return awardDynamicCalendarResponse;
        }

        private async Task<ShopRequest> GetShopRequestFromPersistUsingSelectTripRequest(SelectTripRequest selectTripRequest)
        {
            //Retrieve data from persist which we saved during the shop/selectTrip call 
            CSLShopRequest cslShopRequest = new CSLShopRequest();
            cslShopRequest =
                await _sessionHelperService.GetSession<CSLShopRequest>(selectTripRequest.SessionId, cslShopRequest.ObjectName, new List<string> { selectTripRequest.SessionId, cslShopRequest.ObjectName }).ConfigureAwait(false);
            if (cslShopRequest == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }
            ShopRequest shopRequest = cslShopRequest.ShopRequest;


            //Trip index to find departdate in that trip object and assign it
            var selectedTrip = shopRequest.Trips.FirstOrDefault(trip => trip.BBXCellIdSelected == selectTripRequest.ProductId);
            if (selectedTrip != null && shopRequest.Trips != null && shopRequest.Trips.Count > selectedTrip.TripIndex)
            {
                if (DateTime.ParseExact(selectTripRequest.CalendarDateChange, "MM/dd/yyyy", CultureInfo.InvariantCulture)
                <= DateTime.ParseExact(shopRequest.Trips[selectedTrip.TripIndex - 1].DepartDate, "MM/dd/yyyy", CultureInfo.InvariantCulture))
                {
                    selectTripRequest.CalendarDateChange = DateTime.ParseExact(shopRequest.Trips[selectedTrip.TripIndex - 1].DepartDate, "MM/dd/yyyy", CultureInfo.InvariantCulture).AddDays(1).ToString("MM/dd/yyyy");
                }
                shopRequest.Trips[selectedTrip.TripIndex].DepartDate = selectTripRequest.CalendarDateChange;
                shopRequest.DepartDateTime = selectTripRequest.CalendarDateChange;
                cslShopRequest.ShopRequest = shopRequest;
                await _sessionHelperService.SaveSession<CSLShopRequest>(cslShopRequest, selectTripRequest.SessionId, new List<string> { selectTripRequest.SessionId, cslShopRequest.ObjectName }, cslShopRequest.ObjectName,30000).ConfigureAwait(false);
            }

            /// 212006 - mApps:RT/MT: “Award calendar is not available” error is displayed when tap on right arrow of award  calendar.
            /// FIX: Assign previous trip depart date to current, if that is less than the previous depart date
            /// Srini - 12/082017
            if (_configuration.GetValue<bool>("BugFixToggleFor17M"))
            {
                _shoppingUtility.CheckTripsDepartDateAndAssignPreviousTripDateIfItLesser(shopRequest);
            }

            AssignCalendarLengthOfStay(selectTripRequest.LengthOfCalendar, shopRequest);
            return shopRequest;
        }

        private async Task<AwardDynamicCalendar> CSLAwardDynamicCalendar(string sessionId, int applicationId, string appVersion, string deviceId, string token, ShopRequest request)
        {
            AwardDynamicCalendar awardDynamicCalendar = null;

            string jsonRequest = JsonConvert.SerializeObject(request);

            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "CSLAwardDynamicCalendar - Request for AwardCalendar", "Request", applicationId, appVersion, deviceId, jsonRequest));

            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "CSLAwardDynamicCalendar - Request url for AwardCalendar", "URL", applicationId, appVersion, deviceId, url));

            string ocpApiSubscriptionKey = _configuration.GetValue<string>("CSLNGRPAwardCalendarMP2017AzureService - OCPAPIMSubscriptionKey");

            var jsonResponse = await _awardCalendarAzureService.AwardDynamicCalendar<ShopResponse>(token, sessionId, jsonRequest).ConfigureAwait(false);

            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "CSLAwardDynamicCalendar - Response for AwardCalendar", "Response", applicationId, appVersion, deviceId, jsonResponse));

            if (jsonResponse != null && (jsonResponse.Errors == null || jsonResponse.Errors.Count == 0))
            {
                string airportName = string.Empty;
                string originCityName = string.Empty;
                string destCityName = string.Empty;

                awardDynamicCalendar = GetAwardDynamicCalendarDetails(jsonResponse, sessionId);
                if (awardDynamicCalendar != null)
                {
                    var selectedTrip = request.Trips.FirstOrDefault(trip => trip.BBXCellIdSelected == null);
                    _shoppingUtility.GetAirportCityName(selectedTrip.Origin, ref airportName, ref originCityName);
                    _shoppingUtility.GetAirportCityName(selectedTrip.Destination, ref airportName, ref destCityName);
                    awardDynamicCalendar.OriginDestination = originCityName + " to " + destCityName;
                    awardDynamicCalendar.AwardSubTitleText = _configuration.GetValue<string>("AwardCalendarMP2017SubTitleText") ?? string.Empty;
                }
            }
            else
            {
                awardDynamicCalendar = null;
                HandleAwardCalendarResponseExceptions(jsonResponse.Errors);
            }

            return awardDynamicCalendar;
        }

        private void SetFilterOptionsAwardCalendar(MOBSHOPShopRequest shopRequest, AwardDynamicCalendar awardDynamicCalendar, int tripIndex)
        {
            if (shopRequest != null && awardDynamicCalendar != null && awardDynamicCalendar.Awardcalendars.Count > 0)
            {
                foreach (var awardCabinTypeCalendar in awardDynamicCalendar.Awardcalendars)
                {
                    if ((shopRequest.Trips[tripIndex].Cabin.ToLower().Trim() == "awardecon" || shopRequest.Trips[tripIndex].Cabin.ToLower().Trim() == "econ") && awardCabinTypeCalendar.CabinType.ToLower() == "economy")
                    {
                        awardCabinTypeCalendar.IsDefaultSelected = true;
                    }
                    if ((shopRequest.Trips[tripIndex].Cabin.ToLower().Trim() != "awardecon" || shopRequest.Trips[tripIndex].Cabin.ToLower().Trim() == "econ") && awardCabinTypeCalendar.CabinType.ToLower() == "business/first")
                    {
                        awardCabinTypeCalendar.IsDefaultSelected = true;
                    }
                }
            }
        }

        private void RemovePriceAndTaxforReshop(AwardDynamicCalendar awardCalendar)
        {
            if (awardCalendar == null || awardCalendar.Awardcalendars == null)
            {
                return;
            }
            foreach (var calendar in awardCalendar.Awardcalendars)
            {
                if (calendar.CalendarWeek != null)
                {
                    foreach (AwardCalendarDay day in calendar.CalendarWeek)

                        if (day != null && day.PriceMiles != null && day.PriceMiles.IndexOf('+') != -1)
                            day.PriceMiles = day.PriceMiles.Substring(0, day.PriceMiles.IndexOf('+')).Trim();
                }
            }
        }

        private bool DisableLeftArrowAwardCalendar(string departDateTime, int lengthOfCalendar, string previousTripDepartDateTime = null)
        {
            DateTime dt;
            DateTime ptdt;
            //if the depart date < 4days from previous trip -> returning 7 days from previous trip date and hide left arrow
            if (!string.IsNullOrEmpty(previousTripDepartDateTime))
            {
                DateTime.TryParse(previousTripDepartDateTime, out ptdt);
                return !string.IsNullOrEmpty(departDateTime) && DateTime.TryParse(departDateTime, out dt)
                    && (dt.Date == DateTime.Today || ptdt.Date == DateTime.Today || dt.Date == ptdt.Date || dt.Date.AddDays(1) == ptdt.Date || (lengthOfCalendar == 6 && dt.Date == ptdt.Date.AddDays(1)) || lengthOfCalendar == 3 && dt.Date < ptdt.Date.AddDays(4) || (lengthOfCalendar == 3 && dt.Date < DateTime.Today.AddDays(4))
                   || (lengthOfCalendar == 6 && dt.Date == DateTime.Today));
            }
            else
            {
                return !string.IsNullOrEmpty(departDateTime) && DateTime.TryParse(departDateTime, out dt) &&
                       (dt.Date == DateTime.Today || (lengthOfCalendar == 3 && dt.Date < DateTime.Today.AddDays(4)) || (lengthOfCalendar == 6 && dt.Date == DateTime.Today));
            }
        }

        private bool DisableRightArrowAwardCalendar(string departDateTime, int lengthOfCalendar)
        {
            DateTime dt;
            return !string.IsNullOrEmpty(departDateTime) && DateTime.TryParse(departDateTime, out dt) &&
                   (dt.Date > DateTime.Today.AddDays(333) || lengthOfCalendar == 6 && dt.Date == DateTime.Today.AddDays(331));
        }

        private AwardDynamicCalendar GetAwardDynamicCalendarDetails(ShopResponse response, string sessionID)
        {
            AwardDynamicCalendar awardDynamicCalendar = new AwardDynamicCalendar();

            try
            {
                if (response.Calendar != null)
                {
                    awardDynamicCalendar = GetAwardDynamicCalendar(response.Calendar, sessionID);

                }
            }
            catch (System.Exception ex)
            {
                awardDynamicCalendar = null;
                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                _logger.LogError("GetAwardDynamicCalendarDetails {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));
            }
            return awardDynamicCalendar;
        }

        private void HandleAwardCalendarResponseExceptions(List<United.Services.FlightShopping.Common.ErrorInfo> errors)
        {
            if (errors != null && errors.Count > 0)
            {
                string[] errorCode = { "400.1000", "400.1001", "400.1002", "400.1003",
                                                   "500.5000", "500.5001", "500.1001", "500.2001", "500.2002",
                                                   "400.1004",
                                                   "400.1005", "400.1006", "400.1007", "400.1008", "400.1009", "400.1010"
                                                 };
                var errorObject = errors.FirstOrDefault(error => errorCode.Contains(error.MajorCode));
                if (errorObject != null)
                {
                    throw new MOBUnitedException(errorObject.MajorCode);
                }
                else
                {
                    string errorMessage = string.Empty;
                    foreach (var error in errors)
                    {
                        errorMessage = errorMessage + " " + error.Message;
                    }
                    throw new System.Exception(errorMessage);
                }
            }
        }

        private AwardDynamicCalendar GetAwardDynamicCalendar(CalendarType calendar, string sessionID)
        {
            AwardDynamicCalendar awardDynamicCalendar = new AwardDynamicCalendar();
            List<AwardCalendarDay> economyAwardCalendarDays = new List<AwardCalendarDay>();
            List<AwardCalendarDay> businessAwardCalendarDays = new List<AwardCalendarDay>();
            List<AwardCalendarDay> firstAwardCalendarDays = new List<AwardCalendarDay>();
            List<AwardCalendarDay> businessFirstAwardCalendarDays = new List<AwardCalendarDay>();

            awardDynamicCalendar.DateRangeDisplay = string.Empty;

            if (calendar != null && calendar.Months != null && calendar.Months.Count > 0)
            {

                foreach (CalendarMonth month in calendar.Months)
                {
                    foreach (CalendarWeek week in month.Weeks)
                    {
                        foreach (CalendarDay day in week.Days)  // 7 days 
                        {
                            if (day.Solutions != null && day.Solutions.Count > 0)
                            {
                                CalendarDaySolution solution = new CalendarDaySolution();
                                CalendarDaySolution busSolution = new CalendarDaySolution(); ;
                                CalendarDaySolution firstSolution = new CalendarDaySolution(); ;
                                if (_configuration.GetValue<bool>("EnableAwardMixedCabinFiter"))
                                {
                                    solution = day.Solutions.FirstOrDefault(s => s.CabinType.ToUpper().Contains("ECONOMY"));
                                    economyAwardCalendarDays.Add(GetCalendarDaysByCabinType(solution, day.DateValue, sessionID, day.ErrorCode));
                                    busSolution = day.Solutions.FirstOrDefault(s => s.CabinType.ToUpper().Contains("BUSINESS"));
                                    businessAwardCalendarDays.Add(GetCalendarDaysByCabinType(busSolution, day.DateValue, sessionID, day.ErrorCode));
                                    firstSolution = day.Solutions.FirstOrDefault(s => s.CabinType.ToUpper().Contains("FIRST"));
                                    firstAwardCalendarDays.Add(GetCalendarDaysByCabinType(firstSolution, day.DateValue, sessionID, day.ErrorCode));

                                }
                                else
                                {
                                    solution = day.Solutions.FirstOrDefault(s => s.CabinType == "Economy");
                                    economyAwardCalendarDays.Add(GetCalendarDaysByCabinType(solution, day.DateValue, sessionID, day.ErrorCode));
                                    busSolution = day.Solutions.FirstOrDefault(s => s.CabinType == "Business");
                                    businessAwardCalendarDays.Add(GetCalendarDaysByCabinType(busSolution, day.DateValue, sessionID, day.ErrorCode));
                                    firstSolution = day.Solutions.FirstOrDefault(s => s.CabinType == "First");
                                    firstAwardCalendarDays.Add(GetCalendarDaysByCabinType(firstSolution, day.DateValue, sessionID, day.ErrorCode));
                                }

                                CalendarDaySolution busFirstSolution = GetBusinessFirstAwardCalendarDay(busSolution, firstSolution);
                                businessFirstAwardCalendarDays.Add(GetCalendarDaysByCabinType(busFirstSolution, day.DateValue, sessionID, day.ErrorCode));
                            }

                            else
                            {
                                economyAwardCalendarDays.Add(GetCalendarDaysByCabinType(null, day.DateValue, sessionID, day.ErrorCode));
                                businessAwardCalendarDays.Add(GetCalendarDaysByCabinType(null, day.DateValue, sessionID, day.ErrorCode));
                                firstAwardCalendarDays.Add(GetCalendarDaysByCabinType(null, day.DateValue, sessionID, day.ErrorCode));
                                businessFirstAwardCalendarDays.Add(GetCalendarDaysByCabinType(null, day.DateValue, sessionID, day.ErrorCode));
                                //TODO
                                //_logger.LogWarning("GetAwardDynamicCalendar Added this United Exception to log the Solutions empty");
                            }

                            //month day.DateValue of first day -  day.DateValue of last day, year
                            awardDynamicCalendar.DateRangeDisplay = DateTime.ParseExact(week.Days.First().DateValue, "MM/dd/yyyy", CultureInfo.InvariantCulture).ToString("MMMM") +
                                DateTime.ParseExact(week.Days.First().DateValue, "MM/dd/yyyy", CultureInfo.InvariantCulture).ToString(" d") + "-" +
                                DateTime.ParseExact(week.Days.Last().DateValue, "MM/dd/yyyy", CultureInfo.InvariantCulture).ToString("MMMM") +
                                DateTime.ParseExact(week.Days.Last().DateValue, "MM/dd/yyyy", CultureInfo.InvariantCulture).ToString(" d") + ", " +
                                DateTime.ParseExact(week.Days.Last().DateValue, "MM/dd/yyyy", CultureInfo.InvariantCulture).ToString("yyyy");

                        }
                    }
                }


                awardDynamicCalendar.Awardcalendars = new List<AwardCabinTypeCalendar>();

                AwardCabinTypeCalendar awardCalendarDayList = new AwardCabinTypeCalendar
                {
                    CalendarWeek = new List<AwardCalendarDay>()
                };
                awardCalendarDayList.CalendarWeek = economyAwardCalendarDays;
                awardCalendarDayList.CabinType = "Economy";
                awardCalendarDayList.IsDefaultSelected = false;
                awardDynamicCalendar.Awardcalendars.Add(awardCalendarDayList);

                awardCalendarDayList = new AwardCabinTypeCalendar
                {
                    CalendarWeek = new List<AwardCalendarDay>()
                };
                awardCalendarDayList.CalendarWeek = businessAwardCalendarDays;
                awardCalendarDayList.CabinType = "Business";
                awardCalendarDayList.IsDefaultSelected = false;
                awardDynamicCalendar.Awardcalendars.Add(awardCalendarDayList);

                awardCalendarDayList = new AwardCabinTypeCalendar
                {
                    CalendarWeek = new List<AwardCalendarDay>()
                };
                awardCalendarDayList.CalendarWeek = firstAwardCalendarDays;
                awardCalendarDayList.CabinType = "First";
                awardCalendarDayList.IsDefaultSelected = false;
                awardDynamicCalendar.Awardcalendars.Add(awardCalendarDayList);

                awardCalendarDayList = new AwardCabinTypeCalendar
                {
                    CalendarWeek = new List<AwardCalendarDay>()
                };
                awardCalendarDayList.CalendarWeek = businessFirstAwardCalendarDays;
                awardCalendarDayList.CabinType = "Business/First";
                awardCalendarDayList.IsDefaultSelected = false;
                awardDynamicCalendar.Awardcalendars.Add(awardCalendarDayList);


            }

            return awardDynamicCalendar;
        }
        private AwardCalendarDay GetCalendarDaysByCabinType(CalendarDaySolution solution, string strDateValue, string sessionID, string errorCode)
        {
            AwardCalendarDay acalendarDay = new AwardCalendarDay();
            string[] emptyErrorCode = { "500.5001", "500.1001", "500.2001", "500.2002" };
            string[] notAvailableErrorCode = { "200.1000", "200.1001" };
            if (solution != null)
            {
                DateTime dateValue = DateTime.ParseExact(strDateValue, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                acalendarDay.Week = dateValue.ToString("ddd, d");
                acalendarDay.OriginalDate = strDateValue;
                acalendarDay.IsLowest = solution.Cheapest;
                if (solution.Prices != null && solution.Prices.Count > 0 && !emptyErrorCode.Contains(errorCode) && !notAvailableErrorCode.Contains(errorCode))
                {
                    decimal closeInFeeTotal = 0;
                    string awardPriceTotal = string.Empty;
                    if (solution.Prices.Count > 2 && solution.Prices[2] != null)
                    {
                        closeInFeeTotal = solution.Prices[2].Amount;
                    }
                    if (solution.Prices.Count > 1 && solution.Prices[1] != null)
                    {
                        CultureInfo ci = TopHelper.GetCultureInfo(solution.Prices[1].Currency);
                        decimal taxesTotal = solution.Prices[1].Amount;
                        decimal taxCloseinfeeTotal = taxesTotal + closeInFeeTotal;
                        if (taxCloseinfeeTotal > 0)
                        {
                            string plusSymbol = !_configuration.GetValue<bool>("NGRPAwardCalendarMP2017NewUISwitch") ? " + " : " ";
                            awardPriceTotal = plusSymbol + TopHelper.FormatAmountForDisplay(taxCloseinfeeTotal.ToString(), ci, false, true);
                        }
                    }
                    acalendarDay.PriceMiles = !_configuration.GetValue<bool>("NGRPAwardCalendarMP2017NewUISwitch") ?
                         ShopStaticUtility.FormatAllCabinAwardAmountNoMiles(solution.Prices[0].Amount.ToString()) + awardPriceTotal :
                         ShopStaticUtility.FormatAwardAmountForDisplay(solution.Prices[0].Amount.ToString()) + awardPriceTotal;
                }
                else if (emptyErrorCode.Contains(errorCode))
                {
                    acalendarDay.PriceMiles = string.Empty;
                }
                else
                {
                    acalendarDay.PriceMiles = "Not Available";
                }
            }
            else
            {
                acalendarDay.NotAvailableText = "Not Available";
                DateTime dateValue = DateTime.ParseExact(strDateValue, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                acalendarDay.Week = dateValue.ToString("ddd, d");
                acalendarDay.OriginalDate = strDateValue;
                //TODO
               // _logger.LogWarning("GetAwardDynamicCalendar Added this United Exception to log the Solutions empty");
            }
            return acalendarDay;
        }

        private CalendarDaySolution GetBusinessFirstAwardCalendarDay(CalendarDaySolution business, CalendarDaySolution first)
        {
            CalendarDaySolution busFirstCalendarDaySolution = null;

            if (!business.IsNullOrEmpty() && !first.IsNullOrEmpty())
            {
                if (business.DateValue == first.DateValue
                    && !business.Prices.IsNullOrEmpty() && business.Prices.Count > 0
                    && !first.Prices.IsNullOrEmpty() && first.Prices.Count > 0)
                {
                    busFirstCalendarDaySolution = new CalendarDaySolution();
                    if (!business.Prices[0].Amount.IsNullOrEmpty() && !first.Prices[0].Amount.IsNullOrEmpty() && (business.Prices[0].Amount <= first.Prices[0].Amount))
                    {
                        busFirstCalendarDaySolution = business;
                    }
                    if (!business.Prices[0].Amount.IsNullOrEmpty() && first.Prices[0].Amount.IsNullOrEmpty())
                    {
                        busFirstCalendarDaySolution = business;
                    }
                    if (business.Prices[0].Amount.IsNullOrEmpty() && !first.Prices[0].Amount.IsNullOrEmpty())
                    {
                        busFirstCalendarDaySolution = first;
                    }

                }

            }
            if (!business.IsNullOrEmpty() && first.IsNullOrEmpty())
            {
                busFirstCalendarDaySolution = new CalendarDaySolution();
                busFirstCalendarDaySolution = business;
            }
            if (business.IsNullOrEmpty() && !first.IsNullOrEmpty())
            {
                busFirstCalendarDaySolution = new CalendarDaySolution();
                busFirstCalendarDaySolution = first;
            }


            return busFirstCalendarDaySolution;
        }

        private void CheckTripsDepartDateAndAssignPreviousTripDateIfItLesser(MOBSHOPShopRequest request)
        {
            if (_configuration.GetValue<bool>("BugFixToggleFor17M") && request != null && request.Trips != null && request.Trips.Count > 0)
            {
                for (int i = 0; i < request.Trips.Count; i++)
                {
                    if (i < request.Trips.Count - 1 && DateTime.Parse(request.Trips[i].DepartDate) > DateTime.Parse(request.Trips[i + 1].DepartDate))
                    {
                        request.Trips[i + 1].DepartDate = request.Trips[i].DepartDate;
                    }

                }
            }
        }

        private async Task<AwardCalendarResponse> ShopAwardDynamicCalendar(MOBSHOPShopRequest shopRequest, string token)
        {
            AwardCalendarResponse awardDynamicCalendarResponse = new AwardCalendarResponse();
            ShopRequest request = await GetShopRequest(shopRequest, true);

            awardDynamicCalendarResponse.AwardDynamicCalendar = await CSLAwardDynamicCalendar(shopRequest.SessionId, shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.DeviceId, token, request);
            SetFilterOptionsAwardCalendar(shopRequest, awardDynamicCalendarResponse.AwardDynamicCalendar, 0);

            if (shopRequest.IsReshopChange)
            {
                RemovePriceAndTaxforReshop(awardDynamicCalendarResponse.AwardDynamicCalendar);
            }
            if (shopRequest.AwardTravel && shopRequest.IsReshopChange == false)
                SetDateForwardForAward(awardDynamicCalendarResponse);

            if (shopRequest.AwardTravel
              && awardDynamicCalendarResponse.AwardDynamicCalendar?.Awardcalendars?.Where(a => a.IsDefaultSelected == true)?.FirstOrDefault()?.CalendarWeek?.All(b => b.PriceMiles == null) == true)
                throw new MOBUnitedException("", _configuration.GetValue<string>("AwardCalendarMP2017GenericExceptionMessage"));

            if (!awardDynamicCalendarResponse.AwardDynamicCalendar.IsNullOrEmpty())
            {
                awardDynamicCalendarResponse.AwardDynamicCalendar.IsLeftArrowDisabled = DisableLeftArrowAwardCalendar(request.DepartDateTime, shopRequest.LengthOfCalendar);
                awardDynamicCalendarResponse.AwardDynamicCalendar.IsRightArrowDisabled = DisableRightArrowAwardCalendar(request.DepartDateTime, shopRequest.LengthOfCalendar);
                awardDynamicCalendarResponse.CartId = request.CartId;
                awardDynamicCalendarResponse.SessionID = request.SessionId;
            }

            return awardDynamicCalendarResponse;
        }
        private bool IsStandardRevenueSearch(bool isCorporateBooking, bool isYoungAdultBooking, bool isAwardTravel,
                                                   string employeeDiscountId, string travelType, bool isReshop, string fareClass,
                                                   string promotionCode)
        {
            return !(isCorporateBooking || travelType == TravelType.CLB.ToString() || isYoungAdultBooking ||
                     isAwardTravel || !string.IsNullOrEmpty(employeeDiscountId) || isReshop ||
                     travelType == TravelType.TPSearch.ToString() || !string.IsNullOrEmpty(fareClass) ||
                     !string.IsNullOrEmpty(promotionCode));
        }

        private bool IsAwardFSRRedesignEnabled(int appId, string appVersion)
        {
            if (!_configuration.GetValue<bool>("EnableAwardFSRChanges")) return false;
            return GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("AndroidAwardFSRChangesVersion"), _configuration.GetValue<string>("iOSAwardFSRChangesVersion"));
        }
        private bool IsEnableRefundableFaresToggle(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableRefundableFaresToggle") &&
                   GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("AndroidRefundableFaresToggleVersion"), _configuration.GetValue<string>("iPhoneRefundableFaresToggleVersion"));
        }
        private bool IsMixedCabinFilerEnabled(int id, string version)
        {
            if (!_configuration.GetValue<bool>("EnableAwardMixedCabinFiter")) return false;
            return GeneralHelper.IsApplicationVersionGreaterorEqual(id, version, _configuration.GetValue<string>("Android_AwardMixedCabinFiterFeatureSupported_AppVersion"), _configuration.GetValue<string>("iPhone_AwardMixedCabinFiterFeatureSupported_AppVersion"));
        }
        private bool EnableEPlusAncillary(int appID, string appVersion, bool isReshop = false)
        {
            return _configuration.GetValue<bool>("EnableEPlusAncillaryChanges") && !isReshop && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, _configuration.GetValue<string>("EplusAncillaryAndroidversion"), _configuration.GetValue<string>("EplusAncillaryiOSversion"));
        }
        private  void SetDateForwardForAward(AwardCalendarResponse awardDynamicCalendarResponse)
        {
            if (_configuration.GetValue<bool>("DisableDateFormatChange") == false)
            {
                var currentCalendar = awardDynamicCalendarResponse.AwardDynamicCalendar?.Awardcalendars?.Where(a => a.IsDefaultSelected == true)?.FirstOrDefault()?.CalendarWeek;
                if (currentCalendar != null)
                {
                    foreach (var item in currentCalendar)
                    {
                        if (item != null && item?.OriginalDate != null)
                        {
                            DateTime dateValue = DateTime.ParseExact(item?.OriginalDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                            item.Week = dateValue.ToString("ddd MMM dd");
                        }
                    }
                }
            }
        }


        private void AddRefundableFaresToggleFilter(SearchFilterInfo shopfilter, MOBSearchFilters filters, int appId, string appVersion, bool isStandardRevenueSearch, bool isELFFareDisplayAtFSR, string fareType)
        {

            //Refundable fares toggle feature
            if (IsEnableRefundableFaresToggle(appId, appVersion) && isStandardRevenueSearch)
            {
                shopfilter.ShopIndicators = new ShopIndicators();

                shopfilter.ShopIndicators.IsBESelected = isELFFareDisplayAtFSR;

                if ((filters?.RefundableFaresToggle?.IsSelected ?? false) ||
                    (filters?.RefundableFaresToggle == null && fareType == "urf"))
                {
                    shopfilter.ShopIndicators.IsRefundableSelected = true;
                }
                else
                {
                    shopfilter.ShopIndicators.IsRefundableSelected = false;
                }
            }
        }
        private void AddMixedCabinToggleFilter(SearchFilterInfo shopfilter, MOBSearchFilters filters, int appId, string appVersion, bool isStandardRevenueSearch, bool isELFFareDisplayAtFSR, string fareType)
        {
            //MixedCabin toggle feature
            if (IsMixedCabinFilerEnabled(appId, appVersion) && (fareType == _configuration.GetValue<string>("MixedCabinToggle") || fareType == "lf"))
            {
                if (shopfilter.ShopIndicators == null)
                    shopfilter.ShopIndicators = new ShopIndicators();

                if (filters == null || (filters?.AdditionalToggles?.FirstOrDefault(a => a.Key == _configuration.GetValue<string>("MixedCabinToggleKey"))?.IsSelected ?? false))
                    shopfilter.ShopIndicators.IsMixedToggleSelected = true;
                else
                    shopfilter.ShopIndicators.IsMixedToggleSelected = false;
            }
        }
    }
}
