using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Common;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.Boombox;
using United.Utility.Helper;

namespace United.Common.Helper.Shopping
{
    public class FareWheelHelper : IFareWheelHelper
    {
        private readonly IConfiguration _configuration;
        private readonly ICacheLog _logger;
        private readonly IHeaders _headers;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        public FareWheelHelper(IConfiguration configuration
            , ICacheLog logger
            , IHeaders headers
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility)
        {
            _configuration = configuration;
            _logger = logger;
            _headers = headers;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
        }
        public async Task<United.Services.FlightShopping.Common.ShopSelectRequest> GetShopSelectRequest(SelectTripRequest selectRequest, bool isForSelectTrip = false, bool isEnableWheelChairFilterOnFSR = false)
        {
            United.Services.FlightShopping.Common.ShopSelectRequest shopSelectRequest = new United.Services.FlightShopping.Common.ShopSelectRequest();
            shopSelectRequest.ChannelType = _configuration.GetValue<string>("Shopping - ChannelType");
            shopSelectRequest.AccessCode = _configuration.GetValue<string>("AccessCode - CSLShopping");

            bool decodeOTP = false;
            bool.TryParse(_configuration.GetValue<string>("DecodesOnTimePerformance"), out decodeOTP);
            shopSelectRequest.DecodesOnTimePerfRequested = decodeOTP;

            bool decodesRequested = false;
            bool.TryParse(_configuration.GetValue<string>("DecodesRequested"), out decodesRequested);
            shopSelectRequest.DecodesRequested = decodesRequested;

            United.Mobile.Model.Shopping.ShoppingResponse shop = new United.Mobile.Model.Shopping.ShoppingResponse();
            shop = await _sessionHelperService.GetSession<ShoppingResponse>(selectRequest.SessionId, shop.ObjectName, new List<string> { selectRequest.SessionId, shop.ObjectName });

            if (shop != null && !string.IsNullOrEmpty(shop.CartId))
            {
                shopSelectRequest.CartId = shop.CartId;
            }
            else
            {
                throw new MOBUnitedException("The booking session could not be found.");
            }

            shopSelectRequest.BBXCellId = selectRequest.ProductId;
            shopSelectRequest.BBXSolutionSetId = selectRequest.TripId;
            if (shop.Request.ResultSortType.ToUpper() == "P")
            {
                shopSelectRequest.SortType = "price";
            }
            shopSelectRequest.SortTypeDescending = false;
            shopSelectRequest.CalendarOnly = false;

            shopSelectRequest.UseFilters = selectRequest.UseFilters;
            var isStandardRevenueSearch = IsStandardRevenueSearch(shop.Request.IsCorporateBooking, shop.Request.IsYoungAdultBooking,
                                                     shop.Request.AwardTravel, shop.Request.EmployeeDiscountId,
                                                     shop.Request.TravelType, shop.Request.IsReshop || shop.Request.IsReshopChange,
                                                     shop.Request.FareClass, shop.Request.PromotionCode);

            if (selectRequest.UseFilters && selectRequest.Filters != null)
            {
                shopSelectRequest.SearchFilters = GetSearchFilters(selectRequest.Filters, selectRequest.Application.Id, selectRequest.Application.Version.Major,
                                                                   isStandardRevenueSearch, shop.Request.IsELFFareDisplayAtFSR, shop.Request.FareType);
            }

            shopSelectRequest.ChannelType = "MOBILE";
            shopSelectRequest.CountryCode = shop.Request.CountryCode;

            shopSelectRequest.FlexibleDaysAfter = 0;
            shopSelectRequest.FlexibleDaysBefore = 0;

            shopSelectRequest.MaxTrips = getShoppingSearchMaxTrips();
            shopSelectRequest.PageIndex = 1;
            shopSelectRequest.PageSize = _configuration.GetValue<int>("ShopAndSelectTripCSLRequestPageSize");//getShoppingSearchMaxTrips();
            shopSelectRequest.CalendarDateChange = selectRequest.CalendarDateChange;

            shopSelectRequest.SortType = selectRequest.ResultSortType;

            bool includeAmenities = false;

            if (!_configuration.GetValue<bool>("ByPassAmenities"))
            {
                bool.TryParse(_configuration.GetValue<string>("IncludeAmenities"), out includeAmenities);
            }
            shopSelectRequest.IncludeAmenities = includeAmenities;

            try
            {
                if (shop.Request.AwardTravel)
                {
                    CSLShopRequest cslShopRequest = new CSLShopRequest();
                    cslShopRequest = await _sessionHelperService.GetSession<CSLShopRequest>(selectRequest.SessionId, cslShopRequest.ObjectName, new List<string> { selectRequest.SessionId, cslShopRequest.ObjectName });
                    shopSelectRequest.LoyaltyPerson = cslShopRequest?.ShopRequest?.LoyaltyPerson;
                }
            }
            catch { };
            if (_configuration.GetValue<bool>("EnableNonStopFlight") && (selectRequest.GetNonStopFlightsOnly || selectRequest.GetFlightsWithStops))
            {
                if (shopSelectRequest.SearchFilters == null)
                {
                    shopSelectRequest.SearchFilters = new SearchFilterInfo();
                }
                RequestForNonStopFlights(selectRequest, shopSelectRequest);
            }
            // Refundable fares toggle feature
            if (IsEnableRefundableFaresToggle(selectRequest.Application.Id, selectRequest.Application.Version.Major) &&
                isStandardRevenueSearch &&
                (isForSelectTrip || (selectRequest.Filters?.RefundableFaresToggle?.IsSelected ?? false)))
            {
                shopSelectRequest.FareType = _configuration.GetValue<string>("RefundableFaresToggleFareType");
                if (shopSelectRequest.Characteristics == null) shopSelectRequest.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                shopSelectRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "filterflightsbytoggle", Value = "true" });

                if (isForSelectTrip && (!selectRequest.UseFilters || selectRequest.Filters == null))
                {
                    if (shopSelectRequest.SearchFilters == null)
                    {
                        shopSelectRequest.SearchFilters = new SearchFilterInfo();
                    }
                    AddRefundableFaresToggleFilter(shopSelectRequest.SearchFilters, selectRequest.Filters, selectRequest.Application.Id, selectRequest.Application.Version.Major,
                                                   isStandardRevenueSearch, shop.Request.IsELFFareDisplayAtFSR, shop.Request.FareType);
                }
            }
            if (isEnableWheelChairFilterOnFSR && (selectRequest?.Filters?.RemoveWheelChairFilterApplied ?? false))
            {
                shopSelectRequest.Characteristics ??= new Collection<Service.Presentation.CommonModel.Characteristic>();
                shopSelectRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "WheelChairFilterRemove", Value = "true" });
            }
            // Mixed cabin fares toggle feature
            if (_shoppingUtility.IsMixedCabinFilerEnabled(selectRequest.Application.Id, selectRequest.Application.Version.Major)
                && selectRequest.Filters?.AdditionalToggles?.Any(a => a.Key == _configuration.GetValue<string>("MixedCabinToggleKey")) == true)
            {
                shopSelectRequest.FareType = _configuration.GetValue<string>("MixedCabinToggle");
                if (shopSelectRequest.Characteristics == null) shopSelectRequest.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                shopSelectRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "filterflightsbytoggle", Value = "true" });

                if (isForSelectTrip && (!selectRequest.UseFilters || selectRequest.Filters == null))
                {
                    if (shopSelectRequest.SearchFilters == null)
                    {
                        shopSelectRequest.SearchFilters = new SearchFilterInfo();
                    }
                    AddMixedCabinToggleFilter(shopSelectRequest.SearchFilters, selectRequest.Filters, selectRequest.Application.Id, selectRequest.Application.Version.Major,
                                                   isStandardRevenueSearch, shop.Request.IsELFFareDisplayAtFSR, shopSelectRequest.FareType);
                }
            }
            if(_configuration.GetValue<bool>("EnableFSRMoneyPlusMilesFeature")
                && ShopStaticUtility.ValidateMoneyPlusMilesFlow(shop?.Request)
                && selectRequest.IsMoneyPlusMiles && !string.IsNullOrEmpty(selectRequest.MoneyPlusMilesOptionId))
            {
                shopSelectRequest.MoneyAndMilesOptionId = selectRequest.MoneyPlusMilesOptionId;
                shopSelectRequest.MileagePlusNumber = selectRequest.MileagePlusAccountNumber;
                if (!string.IsNullOrEmpty(selectRequest.MileagePlusAccountNumber))
                {
                    if (shopSelectRequest.LoyaltyPerson == null)
                    {
                        shopSelectRequest.LoyaltyPerson = new Service.Presentation.PersonModel.LoyaltyPerson();
                        shopSelectRequest.LoyaltyPerson.LoyaltyProgramMemberID = selectRequest.MileagePlusAccountNumber;
                        if (selectRequest.PremierStatusLevel == -1)
                        {
                            selectRequest.PremierStatusLevel = 0;// General Member
                        }
                        shopSelectRequest.LoyaltyPerson.LoyaltyProgramMemberTierLevel = (Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel)selectRequest.PremierStatusLevel;
                        shopSelectRequest.LoyaltyPerson.AccountBalances = new Collection<Service.Presentation.CommonModel.LoyaltyAccountBalance>();
                        Service.Presentation.CommonModel.LoyaltyAccountBalance balance = new Service.Presentation.CommonModel.LoyaltyAccountBalance();
                        int.TryParse(selectRequest.MileageBalance, out int bal);
                        balance.Balance = bal;
                        balance.BalanceType = Service.Presentation.CommonEnumModel.LoyaltyAccountBalanceType.MilesBalance;
                        shopSelectRequest.LoyaltyPerson.AccountBalances.Add(balance);
                    }
                }
                //log info if moneyplusmiles option is selected and MileagePlusAccountNumber is null
            }

            if (await _shoppingUtility.EnableFSRETCTravelCreditsFeature()
            && shopSelectRequest.SearchFilters != null && shopSelectRequest.SearchFilters.ShopIndicators != null
             )
            {
                if (!shop.Request.IsReshop || !shop.Request.IsReshopChange || !shop.Request.AwardTravel)
                {
                    shopSelectRequest.SearchFilters.ShopIndicators.IsTravelCreditsApplied = selectRequest.PricingType == PricingType.ETC.ToString();
                }
            }
            return shopSelectRequest;
        }
        private void AddMixedCabinToggleFilter(SearchFilterInfo shopfilter, MOBSearchFilters filters, int appId, string appVersion, bool isStandardRevenueSearch, bool isELFFareDisplayAtFSR, string fareType)
        {
            // MixedCabin toggle feature
            if (_shoppingUtility.IsMixedCabinFilerEnabled(appId, appVersion) && (fareType == _configuration.GetValue<string>("MixedCabinToggle") || fareType == "lf"))
            {
                if (shopfilter.ShopIndicators == null)
                    shopfilter.ShopIndicators = new ShopIndicators();

                if (filters == null || (filters?.AdditionalToggles?.FirstOrDefault(a => a.Key == _configuration.GetValue<string>("MixedCabinToggleKey"))?.IsSelected ?? false))
                    shopfilter.ShopIndicators.IsMixedToggleSelected = true;
                else
                    shopfilter.ShopIndicators.IsMixedToggleSelected = false;
            }
        }

        private void AddRefundableFaresToggleFilter(SearchFilterInfo shopfilter, MOBSearchFilters filters, int appId, string appVersion, bool isStandardRevenueSearch, bool isELFFareDisplayAtFSR, string fareType)
        {
            // Refundable fares toggle feature
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

        private bool IsStandardRevenueSearch(bool isCorporateBooking, bool isYoungAdultBooking, bool isAwardTravel,
                                                  string employeeDiscountId, string travelType, bool isReshop, string fareClass,
                                                  string promotionCode)
        {
            return !(isCorporateBooking || travelType == TravelType.CLB.ToString() || isYoungAdultBooking ||
                     isAwardTravel || !string.IsNullOrEmpty(employeeDiscountId) || isReshop ||
                     travelType == TravelType.TPSearch.ToString() || !string.IsNullOrEmpty(fareClass) ||
                     !string.IsNullOrEmpty(promotionCode));
        }
        private bool IsEnableRefundableFaresToggle(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableRefundableFaresToggle") &&
                   GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("AndroidRefundableFaresToggleVersion"), _configuration.GetValue<string>("iPhoneRefundableFaresToggleVersion"));
        }

        private void RequestForNonStopFlights(SelectTripRequest selectRequest, United.Services.FlightShopping.Common.ShopSelectRequest shopSelectRequest)
        {
            shopSelectRequest.UseFilters = true;
            SetStopCountsToGetNonStopFlights(selectRequest.GetNonStopFlightsOnly, selectRequest.GetFlightsWithStops, shopSelectRequest.SearchFilters);
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
                    foreach (var kvp in filters.AirportsDestinationList)
                    {
                        CodeDescPair cdp = new CodeDescPair();
                        cdp.Code = kvp.Key;
                        cdp.Description = kvp.Value;
                        cdp.Amount = kvp.Amount;
                        cdp.Currency = kvp.Currency;

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
                    foreach (var kvp in filters.AirportsOriginList)
                    {
                        CodeDescPair cdp = new CodeDescPair();
                        cdp.Code = kvp.Key;
                        cdp.Description = kvp.Value;
                        cdp.Amount = kvp.Amount;
                        cdp.Currency = kvp.Currency;

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
                    foreach (var kvp in filters.AirportsStopList)
                    {
                        CodeDescPair cdp = new CodeDescPair();
                        cdp.Code = kvp.Key;
                        cdp.Description = kvp.Value;
                        cdp.Amount = kvp.Amount;
                        cdp.Currency = kvp.Currency;

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
                    foreach (var kvp in filters.AirportsStopToAvoidList)
                    {
                        CodeDescPair cdp = new CodeDescPair();
                        cdp.Code = kvp.Key;
                        cdp.Description = kvp.Value;
                        cdp.Amount = kvp.Amount;
                        cdp.Currency = kvp.Currency;

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
                    foreach (var kvp in filters.CarriersMarketingList)
                    {
                        CodeDescPair cdp = new CodeDescPair();
                        cdp.Code = kvp.Key;
                        cdp.Description = kvp.Value;
                        cdp.Amount = kvp.Amount;
                        cdp.Currency = kvp.Currency;

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
                    foreach (var kvp in filters.CarriersOperatingList)
                    {
                        CodeDescPair cdp = new CodeDescPair();
                        cdp.Code = kvp.Key;
                        cdp.Description = kvp.Value;
                        cdp.Amount = kvp.Amount;
                        cdp.Currency = kvp.Currency;

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
                    foreach (var kvp in filters.EquipmentList)
                    {
                        CodeDescPair cdp = new CodeDescPair();
                        cdp.Code = kvp.Key;
                        cdp.Description = kvp.Value;
                        cdp.Amount = kvp.Amount;
                        cdp.Currency = kvp.Currency;

                        filter.EquipmentList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.EquipmentTypes))
                {
                    filter.EquipmentTypes = filters.EquipmentTypes;
                }

                if (filters.FareFamilies != null && filters.FareFamilies.Count > 0)
                {
                    filter.FareFamilies = new sliceFareFamilies();
                    filter.FareFamilies.fareFamily = new fareFamilyType[filters.FareFamilies.Count];
                    int cnt = 0;
                    foreach (var ff in filters.FareFamilies)
                    {

                        fareFamilyType fft = new fareFamilyType();
                        fft.fareFamily = string.IsNullOrEmpty(ff.FareFamily) ? "" : ff.FareFamily;
                        fft.maxMileage = ff.MaxMileage;
                        if (!string.IsNullOrEmpty(ff.MaxPrice))
                        {
                            fft.maxPrice = new price();
                            fft.maxPrice.amount = Convert.ToDecimal(ff.MaxPrice);
                        }
                        fft.minMileage = ff.MinMileage;
                        if (!string.IsNullOrEmpty(ff.MinPrice))
                        {
                            fft.minPrice = new price();
                            fft.minPrice.amount = Convert.ToDecimal(ff.MinPrice);
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
                    foreach (var warningFilter in filters.WarningsFilter)
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
        private int getShoppingSearchMaxTrips()
        {
            int maxTrips = 125;
            int.TryParse(_configuration.GetValue<string>("ShoppingSearchMaxTrips"), out maxTrips);

            return maxTrips;
        }
        public United.Services.FlightShopping.Common.ShopSelectRequest GetShopSelectFareWheelRequest(United.Services.FlightShopping.Common.ShopSelectRequest selectRequest)
        {
            United.Services.FlightShopping.Common.ShopSelectRequest fareWheelRequest = selectRequest;
            ///224314 : mApp : Booking –Revenue- Multitrip- Fare wheel in FSR2 is not updating correctly after selecting alternate date and user unable to select alternate dates which are not in initial FSR2 farewheel
            ///Srini 11/22/2017 -- CB
            if (_configuration.GetValue<bool>("BugFixToggleFor17M"))
            {
                if (fareWheelRequest.CalendarDateChange == null) fareWheelRequest.CalendarDateChange = "";
            }
            else
            {
                fareWheelRequest.CalendarDateChange = "";
            }
            fareWheelRequest.CalendarOnly = false;
            fareWheelRequest.FareWheelOnly = true;
            fareWheelRequest.FlexibleDaysAfter = getFlexibleDaysAfter();
            fareWheelRequest.FlexibleDaysBefore = getFlexibleDaysBefore();
            fareWheelRequest.DecodesRequested = false;

            return fareWheelRequest;
        }
        private int getFlexibleDaysAfter()
        {
            int flexibleFareDaysAfter = 0;
            int.TryParse(_configuration.GetValue<string>("AffinitySearchFlexibleDaysAfter"), out flexibleFareDaysAfter);

            return flexibleFareDaysAfter;
        }

        private int getFlexibleDaysBefore()
        {
            int flexibleFareDaysBefore = 0;
            int.TryParse(_configuration.GetValue<string>("AffinitySearchFlexibleDaysBefore"), out flexibleFareDaysBefore);

            return flexibleFareDaysBefore;
        }
    }
}
