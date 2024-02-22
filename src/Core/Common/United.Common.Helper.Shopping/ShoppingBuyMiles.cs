using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Utility.Helper;
using MOBSHOPPrice = United.Mobile.Model.Shopping.MOBSHOPPrice;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using Session = United.Mobile.Model.Common.Session;


namespace United.Common.Helper.Shopping
{
    public class ShoppingBuyMiles : IShoppingBuyMiles
    {
        private readonly IConfiguration _configuration;
        private readonly IFFCShoppingcs _fFCShoppingcs;

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IFeatureSettings _featureSettings;

        public ShoppingBuyMiles(IConfiguration configuration
           , IFFCShoppingcs fFCShoppingcs
           , IServiceScopeFactory serviceScopeFactory
           , IFeatureSettings featureSettings
           )
        {
            _configuration = configuration;
            _fFCShoppingcs = fFCShoppingcs;
            _serviceScopeFactory = serviceScopeFactory;
            _featureSettings = featureSettings;
        }


        public void AddBuyMilesFeature(ShopSelectRequest request, MOBSHOPAvailability availability, SelectTripRequest selectTripRequest, Session session,
            FlightReservationResponse shopBookingDetailsResponse, FlightReservationResponse response)
        {
            if (IsBuyMilesFeatureEnabled(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.CatalogItems) && response != null
                && session.IsAward)
            {
                DisplayBuyMiles(availability.Reservation, shopBookingDetailsResponse, session, selectTripRequest);
            }
        }

        private async Task DisplayBuyMiles(MOBSHOPReservation reservation, FlightReservationResponse response, Session session,
            SelectTripRequest selectTripRequest)
        {
            List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(selectTripRequest, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID").ConfigureAwait(false);
            if (response.IsMileagePurchaseRequired && response.IsPurchaseIneligible)
            {
                string webShareToken = string.Empty;
                //Scenario 1
                // MOBILE-20327 mApp | Insufficient Mileage of 50% or more 
                //threshold does not meet 50% mileage balance criteria
                webShareToken =await GetSSOToken(selectTripRequest.Application.Id,
                    selectTripRequest.DeviceId, selectTripRequest.Application.Version.Major,
                    selectTripRequest.TransactionId, null, selectTripRequest.SessionId, session.MileagPlusNumber);
                await AddActionsOnMileageBalanceNOTMeetsThreshhold(reservation, response, webShareToken, lstMessages);
            }
            else if (response.IsMileagePurchaseRequired)
            {
                // Scenario 2
                //MOBILE-20326	mApp | Insufficient Mileage under 50%                
                AddActionsOnMileageBalanceMeetsThreshhold(reservation, response, lstMessages);
                // Add the Grrand total with PRICE TYPE MPF                
            }
        }
        public async Task<string> GetSSOToken(int applicationId, string deviceId, string appVersion, string transactionId, String webConfigSession, string sessionID, string mileagPlusNumber)
        {
            string ssoToken = string.Empty;
            using var scope = _serviceScopeFactory.CreateScope();
            var dpSSOService = scope.ServiceProvider.GetRequiredService<IDPService>();
            try
            {
                var ssoTokenObject =await dpSSOService.GetSSOToken(applicationId, mileagPlusNumber, _configuration).ConfigureAwait(false);
                ssoToken = $"{ssoTokenObject.TokenType} {ssoTokenObject.AccessToken}";
                return ssoToken;
            }
            catch (Exception ex)
            {
                throw new MOBUnitedException(ex.Message.ToString());
            }
        }



        public void UpdatePricesForBuyMiles(List<MOBSHOPPrice> displayPrices, FlightReservationResponse shopBookingDetailsResponse, List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> displayFees = null)
        {
            if (displayFees?.Where(a => a.Type == "MPF") != null)
            {
                UpdateGrandTotalBuyMiles(displayPrices, displayFees);
                UpdateTaxPriceTypeDescriptionForBuyMiles(displayPrices);
                AddFarewayDescriptionForMultipaxForBuyMiles(displayPrices);
            }
        }

        private void AddFarewayDescriptionForMultipaxForBuyMiles(List<MOBSHOPPrice> displayPrices)
        {
            var miles = displayPrices.FirstOrDefault(a => a.DisplayType == "MILES");
            if (miles != null && displayPrices?.Count > 0)
            {
                MOBSHOPPrice travelrPriceMPF = new MOBSHOPPrice();
                travelrPriceMPF.DisplayType = "TRAVELERPRICE_MPF";
                travelrPriceMPF.CurrencyCode = miles.CurrencyCode;
                travelrPriceMPF.DisplayValue = miles.DisplayValue;
                travelrPriceMPF.Value = miles.Value;
                travelrPriceMPF.PaxTypeCode = miles.PaxTypeCode;
                travelrPriceMPF.PriceTypeDescription = "Fare";
                travelrPriceMPF.FormattedDisplayValue = miles.FormattedDisplayValue;
                travelrPriceMPF.PaxTypeDescription = miles.PaxTypeDescription;
                displayPrices.Add(travelrPriceMPF);
            }
        }

        private void UpdateTaxPriceTypeDescriptionForBuyMiles(List<MOBSHOPPrice> displayPrices)
        {
            var mpfIndex = displayPrices.FindIndex(a => a.DisplayType == "TAX");
            if (mpfIndex >= 0)
                displayPrices[mpfIndex].PriceTypeDescription = "Taxes and fees";
        }

        private void UpdateMPFPriceTypeDescriptionForBuyMiles(List<MOBSHOPPrice> displayPrices, FlightReservationResponse flightReservationResponse)
        {
            string additionalMiles = "Additional {0} miles";
            var mpfIndex = displayPrices.FindIndex(a => a.DisplayType == "MPF");
            if (mpfIndex >= 0)
            {
                string formattedMiles = String.Format("{0:n0}", flightReservationResponse.DisplayCart?.DisplayFees?.FirstOrDefault()?.SubItems?.Where(a => a.Key == "PurchaseMiles")?.FirstOrDefault()?.Value);
                displayPrices[mpfIndex].PriceTypeDescription = string.Format(additionalMiles, formattedMiles);
            }
        }

        private void UpdateGrandTotalBuyMiles(List<MOBSHOPPrice> displayPrices, List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> displayFees = null, bool isCommonMethod = false)
        {
            var grandTotalIndex = displayPrices.FindIndex(a => a.DisplayType == "GRAND TOTAL");
            if (grandTotalIndex >= 0)
            {
                double extraMilePurchaseAmount = (displayFees?.Where(a => a.Type == "MPF")?.FirstOrDefault()?.Amount != null) ?
                                         Convert.ToDouble(displayFees?.Where(a => a.Type == "MPF")?.FirstOrDefault()?.Amount) : 0;
                string priceTypeDescription = displayFees?.Where(a => a.Type == "MPF")?.FirstOrDefault()?.Description;
                if (extraMilePurchaseAmount > 0 && (priceTypeDescription == null || priceTypeDescription?.Contains("Additional") == false))
                {
                    displayPrices[grandTotalIndex].Value += extraMilePurchaseAmount;
                    CultureInfo ci = null;
                    ci = TopHelper.GetCultureInfo(displayPrices[grandTotalIndex].CurrencyCode);
                    displayPrices[grandTotalIndex].DisplayValue = displayPrices[grandTotalIndex].Value.ToString();
                    displayPrices[grandTotalIndex].FormattedDisplayValue = TopHelper.FormatAmountForDisplay(displayPrices[grandTotalIndex].Value.ToString(), ci, false);
                }
            }
        }

        public bool IsBuyMilesFeatureEnabled(int appId, string version, List<MOBItem> catalogItems = null, bool isNotSelectTripCall = false)
        {
            if (_configuration.GetValue<bool>("EnableBuyMilesFeature") == false) return false;
            if ((catalogItems != null && catalogItems.Count > 0 &&
                   catalogItems.FirstOrDefault(a => a.Id == _configuration.GetValue<string>("Android_EnableBuyMilesFeatureCatalogID") || a.Id == _configuration.GetValue<string>("iOS_EnableBuyMilesFeatureCatalogID"))?.CurrentValue == "1")
                   || isNotSelectTripCall)
                return GeneralHelper.IsApplicationVersionGreaterorEqual(appId, version, _configuration.GetValue<string>("Android_BuyMilesFeatureSupported_AppVersion"), _configuration.GetValue<string>("IPhone_BuyMilesFeatureSupported_AppVersion"));
            else
                return false;

        }

        public void UpdatePriceTypeDescForBuyMiles(int appId, string appVersion, List<MOBItem> catalogItems, FlightReservationResponse shopBookingDetailsResponse, MOBSHOPPrice bookingPrice)
        {
            // if BUY MILES flow and PRice type is MPF change the description for UI display
            if (IsBuyMilesFeatureEnabled(appId, appVersion, isNotSelectTripCall: true))
            {
                string additionalMiles = "Additional {0} miles";
                string formattedMiles = String.Format("{0:n0}", shopBookingDetailsResponse?.DisplayCart?.DisplayFees?.FirstOrDefault()?.SubItems?.Where(a => a.Key == "PurchaseMiles")?.FirstOrDefault()?.Value);
                if (bookingPrice?.DisplayType == "MPF")
                    bookingPrice.PriceTypeDescription = string.Format(additionalMiles, formattedMiles);
            }
        }

        public void ApplyPriceChangesForBuyMiles(FlightReservationResponse flightReservationResponse,
        Reservation reservation = null, Reservation bookingPathReservation = null)
        {

            if (reservation != null)
            {
                if (reservation.Prices != null && reservation.Prices.Count > 0)
                {
                    //UpdateGrandTotalBuyMiles(reservation);
                    //UpdateMPFPriceTypeDescriptionForBuyMiles(reservation, flightReservationResponse);
                    //UpdateTaxPriceTypeDescriptionForBuyMiles(reservation);
                    //AddFarewayDescriptionForMultipaxForBuyMiles(reservation);
                    //UpdateComplianceTaxesForBuyMiles(reservation);
                }
            }
            else if (bookingPathReservation != null)
            {
                UpdateGrandTotalForBookingReservation(bookingPathReservation);
                UpdateMPFPriceTypeDescriptionForBuyMilesForBookingReservation(bookingPathReservation, flightReservationResponse);
                UpdateTaxPriceTypeDescriptionForBuyMilesForBookingReservation(bookingPathReservation);
                AddFarewayDescriptionForMultipaxForBuyMilesForBookingReservation(bookingPathReservation);
                UpdateComplianceTaxesForBuyMilesForReservation(bookingPathReservation);
            }
        }

        private void UpdateComplianceTaxesForBuyMilesForReservation(Reservation reservation)
        {
            try
            {
                var complainceTaxes = reservation?.ShopReservationInfo2?.InfoNationalityAndResidence?.ComplianceTaxes;
                if (complainceTaxes != null)
                {
                    foreach (var taxes in complainceTaxes)
                    {
                        foreach (var tax in taxes)
                        {
                            if (tax.TaxCode == "MPF")
                            {
                                taxes.Remove(tax);
                                return;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void AddFarewayDescriptionForMultipaxForBuyMiles(MOBSHOPReservation reservation)
        {
            var miles = reservation.Prices.FirstOrDefault(a => a.DisplayType == "MILES");
            if (miles != null && reservation?.Prices?.Count > 0)
            {
                MOBSHOPPrice travelrPriceMPF = new MOBSHOPPrice();
                travelrPriceMPF.DisplayType = "TRAVELERPRICE_MPF";
                travelrPriceMPF.CurrencyCode = miles.CurrencyCode;
                travelrPriceMPF.DisplayValue = miles.DisplayValue;
                travelrPriceMPF.Value = miles.Value;
                travelrPriceMPF.PaxTypeCode = miles.PaxTypeCode;
                travelrPriceMPF.PriceTypeDescription = "Fare";
                travelrPriceMPF.FormattedDisplayValue = miles.FormattedDisplayValue;
                travelrPriceMPF.PaxTypeDescription = miles.PaxTypeDescription;
                reservation.Prices.Add(travelrPriceMPF);

            }
        }

        private void AddFarewayDescriptionForMultipaxForBuyMilesForBookingReservation(Reservation reservation)
        {
            var miles = reservation.Prices.FirstOrDefault(a => a.DisplayType == "MILES");
            if (miles != null && reservation?.Prices?.Count > 0)
            {
                MOBSHOPPrice travelrPriceMPF = new MOBSHOPPrice();
                travelrPriceMPF.DisplayType = "TRAVELERPRICE_MPF";
                travelrPriceMPF.CurrencyCode = miles.CurrencyCode;
                travelrPriceMPF.DisplayValue = miles.DisplayValue;
                travelrPriceMPF.Value = miles.Value;
                travelrPriceMPF.PaxTypeCode = miles.PaxTypeCode;
                travelrPriceMPF.PriceTypeDescription = "Fare";
                travelrPriceMPF.FormattedDisplayValue = miles.FormattedDisplayValue;
                travelrPriceMPF.PaxTypeDescription = miles.PaxTypeDescription;
                reservation.Prices.Add(travelrPriceMPF);

            }
        }

        private void UpdateMPFPriceTypeDescriptionForBuyMiles(MOBSHOPReservation reservation, FlightReservationResponse flightReservationResponse)
        {
            string additionalMiles = "Additional {0} miles";
            var mpfIndex = reservation.Prices.FindIndex(a => a.DisplayType == "MPF");
            if (mpfIndex >= 0)
            {
                string formattedMiles = String.Format("{0:n0}", flightReservationResponse.DisplayCart?.DisplayFees?.FirstOrDefault()?.SubItems?.Where(a => a.Key == "PurchaseMiles")?.FirstOrDefault()?.Value);
                reservation.Prices[mpfIndex].PriceTypeDescription = string.Format(additionalMiles, formattedMiles);
            }
        }

        //isCommonMethod = true : This is required for calling from Price update from common method to by pass the check for Additional priceTypeDescrirption
        public void UpdateGrandTotal(MOBSHOPReservation reservation, bool isCommonMethod = false)
        {
            if (reservation.Prices != null)
            {
                var grandTotalIndex = reservation.Prices.FindIndex(a => a.PriceType == "GRAND TOTAL");
                if (grandTotalIndex >= 0)
                {
                    double extraMilePurchaseAmount = (reservation?.Prices?.Where(a => a.DisplayType == "MPF")?.FirstOrDefault()?.Value != null) ?
                                             Convert.ToDouble(reservation?.Prices?.Where(a => a.DisplayType == "MPF")?.FirstOrDefault()?.Value) : 0;
                    string priceTypeDescription = reservation?.Prices?.Where(a => a.DisplayType == "MPF")?.FirstOrDefault()?.PriceTypeDescription;
                    if (extraMilePurchaseAmount > 0 && (priceTypeDescription == null || priceTypeDescription?.Contains("Additional") == false))
                    {
                        reservation.Prices[grandTotalIndex].Value += extraMilePurchaseAmount;
                        CultureInfo ci = null;
                        ci = TopHelper.GetCultureInfo(reservation?.Prices[grandTotalIndex].CurrencyCode);
                        reservation.Prices[grandTotalIndex].DisplayValue = reservation.Prices[grandTotalIndex].Value.ToString();
                        reservation.Prices[grandTotalIndex].FormattedDisplayValue = TopHelper.FormatAmountForDisplay(reservation?.Prices[grandTotalIndex].Value.ToString(), ci, false);
                    }
                }
            }
        }



        private void UpdateTaxPriceTypeDescriptionForBuyMilesForBookingReservation(Reservation reservation)
        {
            var mpfIndex = reservation.Prices.FindIndex(a => a.DisplayType == "TAX");
            if (mpfIndex >= 0)
                reservation.Prices[mpfIndex].PriceTypeDescription = "Taxes and fees";
        }

        private void UpdateMPFPriceTypeDescriptionForBuyMilesForBookingReservation(Reservation reservation, FlightReservationResponse flightReservationResponse)
        {
            string additionalMiles = "Additional {0} miles";
            var mpfIndex = reservation.Prices.FindIndex(a => a.DisplayType == "MPF");
            if (mpfIndex >= 0)
            {
                string formattedMiles = String.Format("{0:n0}", flightReservationResponse.DisplayCart?.DisplayFees?.FirstOrDefault()?.SubItems?.Where(a => a.Key == "PurchaseMiles")?.FirstOrDefault()?.Value);
                reservation.Prices[mpfIndex].PriceTypeDescription = string.Format(additionalMiles, formattedMiles);
            }
        }

        private void UpdateGrandTotalForBookingReservation(Reservation bookingPathReservation)
        {
            var grandTotalIndex = bookingPathReservation.Prices.FindIndex(a => a.PriceType == "GRAND TOTAL");
            if (grandTotalIndex >= 0)
            {
                string priceTypeDescription = bookingPathReservation?.Prices?.Where(a => a.DisplayType == "MPF")?.FirstOrDefault()?.PriceTypeDescription;
                double extraMilePurchaseAmount = (bookingPathReservation?.Prices?.Where(a => a.DisplayType == "MPF")?.FirstOrDefault()?.Value != null) ?
                                         Convert.ToDouble(bookingPathReservation?.Prices?.Where(a => a.DisplayType == "MPF")?.FirstOrDefault()?.Value) : 0;

                if (extraMilePurchaseAmount > 0 && bookingPathReservation.Prices[grandTotalIndex].Value < extraMilePurchaseAmount)
                {
                    bookingPathReservation.Prices[grandTotalIndex].Value += extraMilePurchaseAmount;
                    CultureInfo ci = null;
                    ci = TopHelper.GetCultureInfo(bookingPathReservation?.Prices[grandTotalIndex].CurrencyCode);
                    bookingPathReservation.Prices[grandTotalIndex].DisplayValue = bookingPathReservation.Prices[grandTotalIndex].Value.ToString();
                    bookingPathReservation.Prices[grandTotalIndex].FormattedDisplayValue = TopHelper.FormatAmountForDisplay(bookingPathReservation?.Prices[grandTotalIndex].Value.ToString(), ci, false);
                }
            }
        }

        private async Task AddActionsOnMileageBalanceNOTMeetsThreshhold(MOBSHOPReservation reservation, FlightReservationResponse response, string webShareToken, List<CMSContentMessage> lstMessages)
        {
            reservation.OnScreenAlert = new MOBOnScreenAlert();
            reservation.OnScreenAlert.Title = _configuration.GetValue<string>("BuyMilesHeaderAlertTitle");
            string alerMessage = (reservation?.FareLock?.FareLockProducts != null && reservation?.FareLock?.FareLockProducts?.Count > 0) ?
                    GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.NOTMeetsThreshhold.AlertMessage")
                    : GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.NOFareLockNOTMeetsThreshhold.AlertMessage");
            string fallBackMessage = (reservation?.FareLock?.FareLockProducts != null && reservation?.FareLock?.FareLockProducts?.Count > 0) ?
                            _configuration.GetValue<String>("SelectTrip.BuyMiles.NOTMeetsThreshhold.AlertMessage") : _configuration.GetValue<String>("SelectTrip.BuyMiles.NOFareLockNOTMeetsThreshhold.AlertMessage");

            if (string.IsNullOrEmpty(alerMessage) == false)
                reservation.OnScreenAlert.Message = string.Format(alerMessage, String.Format("{0:n0}", response.DisplayCart?.ActualMileageRequired));
            else
                reservation.OnScreenAlert.Message = string.Format(fallBackMessage, String.Format("{0:n0}", response.DisplayCart?.ActualMileageRequired));
            reservation.OnScreenAlert.Actions = new List<MOBOnScreenActions>();
            

            if (await _featureSettings.GetFeatureSettingValue("EnableRedirectURLUpdate").ConfigureAwait(false))
            {
                reservation.OnScreenAlert.Actions.Add(new MOBOnScreenActions
                {
                    ActionTitle = GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.MeetsThreshhold.PurchaseMilesText"),
                    ActionType = MOBOnScreenAlertActionType.NAVIGATE_TO_EXTERNAL,
                    ActionURL = $"{_configuration.GetValue<string>("NewDotcomSSOUrl")}?type=sso&token={webShareToken}&landingUrl={_configuration.GetValue<string>("BuyMilesExternalMilegePlusURL")}",
                    WebShareToken = string.Empty,
                    WebSessionShareUrl = string.Empty
                });
            }
            else
            {
                reservation.OnScreenAlert.Actions.Add(new MOBOnScreenActions
                {
                    ActionTitle = GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.MeetsThreshhold.PurchaseMilesText"),
                    ActionType = MOBOnScreenAlertActionType.NAVIGATE_TO_EXTERNAL,
                    ActionURL = _configuration.GetValue<string>("BuyMilesExternalMilegePlusURL"),
                    WebShareToken = webShareToken,
                    WebSessionShareUrl = _configuration.GetValue<string>("DotcomSSOUrl")
                });
            }
            AddFareLockLink(reservation, lstMessages);
            reservation.OnScreenAlert.Actions.Add(new MOBOnScreenActions
            {
                ActionTitle = GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.MeetsThreshhold.SelectDifferentFlightText"),
                ActionType = MOBOnScreenAlertActionType.DISMISS_ALERT
            });
        }

        private void AddFareLockLink(MOBSHOPReservation reservation, List<CMSContentMessage> lstMessages)
        {
            if (reservation?.FareLock?.FareLockProducts != null && reservation?.FareLock?.FareLockProducts?.Count > 0)
            {
                reservation.OnScreenAlert.Actions.Add(new MOBOnScreenActions
                {
                    ActionTitle = reservation?.FareLock?.FareLockTitleText,
                    ActionType = MOBOnScreenAlertActionType.NAVIGATE_TO_FARELOCK,

                });

                var findContinueWithFareLockOption = reservation.FareLock.FareLockProducts.FindIndex(a => a.FareLockProductTitle == _configuration.GetValue<string>("FareLockTextContinueWithOutFareLock"));
                if (findContinueWithFareLockOption > -1)
                    reservation.FareLock.FareLockProducts.RemoveAt(findContinueWithFareLockOption);
            }
        }

        private void AddActionsOnMileageBalanceMeetsThreshhold(MOBSHOPReservation reservation, FlightReservationResponse response, List<CMSContentMessage> lstMessages)
        {
            reservation.OnScreenAlert = new MOBOnScreenAlert();
            reservation.OnScreenAlert.Title = _configuration.GetValue<string>("BuyMilesHeaderAlertTitle");
            string alerMessage = GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.MeetsThreshhold.AlertMessage");
            if (string.IsNullOrEmpty(alerMessage) == false)
                reservation.OnScreenAlert.Message = string.Format(alerMessage, String.Format("{0:n0}", response.DisplayCart?.ActualMileageRequired));
            else
                reservation.OnScreenAlert.Message = string.Format(_configuration.GetValue<String>("SelectTrip.BuyMiles.MeetsThreshhold.AlertMessage"), String.Format("{0:n0}", response.DisplayCart?.ActualMileageRequired), String.Format("{0:n0}", response.DisplayCart?.DetectedUserBalance));
            reservation.OnScreenAlert.Actions = new List<MOBOnScreenActions>();
            reservation.OnScreenAlert.Actions.Add(new MOBOnScreenActions
            {
                ActionTitle = GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.MeetsThreshhold.AddMilesText"),
                ActionType = MOBOnScreenAlertActionType.ADD_MILES
            });
            reservation.OnScreenAlert.Actions.Add(new MOBOnScreenActions
            {
                ActionTitle = GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.MeetsThreshhold.SelectDifferentFlightText"),
                ActionType = MOBOnScreenAlertActionType.DISMISS_ALERT
            });


        }

        private string GetSDLStringMessageFromList(List<CMSContentMessage> list, string title)
        {
            return list?.Where(x => x.Title.Equals(title))?.FirstOrDefault()?.ContentFull?.Trim();
        }
    }
}
