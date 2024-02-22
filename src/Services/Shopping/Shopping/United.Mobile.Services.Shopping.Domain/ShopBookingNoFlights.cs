using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using United.Common.Helper;
using United.Common.Helper.FSRHandler;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Definition;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.Model.Booking;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Common;
using United.Mobile.Model.Shopping.Common.TripPlanner;
using United.Service.Presentation.LoyaltyModel;
using United.Service.Presentation.ReferenceDataModel;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.Boombox;
using United.Services.FlightShopping.Common.Extensions;
using United.Services.FlightShopping.Common.LMX;
using United.Services.FlightShopping.Common.OrganizeResults;
using United.Services.FlightShopping.Common.SpecialPricing;
using United.Utility.Helper;
using United.Utility.HttpService;
using CreditType = United.Mobile.Model.Shopping.CreditType;
using CreditTypeColor = United.Mobile.Model.Shopping.CreditTypeColor;
using ErrorInfo = United.Services.FlightShopping.Common.ErrorInfo;
using MOBSHOPSegmentInfoDisplay = United.Mobile.Model.SeatMapEngine.MOBSHOPSegmentInfoDisplay;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using SearchType = United.Services.FlightShopping.Common.SearchType;

namespace United.Mobile.Services.Shopping.Domain
{
    //This partial class is ONLY for handling NO FLIGHTS found scenarios
    public partial class ShopBooking
    {
        private async Task<MOBSHOPAvailability> HandleNoFlightsFoundOptions(string token, MOBSHOPShopRequest shopRequest, MOBSHOPAvailability availability, United.Services.FlightShopping.Common.ShopResponse response, List<MOBFSRAlertMessage> alertMessages, Session session)
        {
            // Offer the customer other search options due to no flights available.
            string ex = "Offer the customer other search options due to no flights available.";
            //_logger.LogWarning("GetAvailability - HandleFlightShoppingThatHasNoResults {@UnitedException}", ex);
            _logger.LogWarning("GetAvailability - HandleFlightShoppingThatHasNoResults {@UnitedException}", ex);
            List<CMSContentMessage> lstMessages = null;
            if (_configuration.GetValue<bool>("DisableSDLValuesForNoFlightsFound") == false)
                lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(shopRequest, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
            availability = new MOBSHOPAvailability
            {
                FSRAlertMessages = alertMessages,
                TravelerCount = (_shoppingUtility.EnableTravelerTypes(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop) && shopRequest.TravelerTypes != null && shopRequest.TravelerTypes.Count > 0) ? ShopStaticUtility.GetTravelerCount(shopRequest.TravelerTypes) : 0,
                Title = (_shoppingUtility.CheckFSRRedesignFromShop(shopRequest)) ? _configuration.GetValue<string>("FSRRedesignFSRTitleForNoResults") ?? "Select flights" : string.Empty,
                SubTitle = (_shoppingUtility.CheckFSRRedesignFromShop(shopRequest)) ? ShopStaticUtility.GetSubTitleForNoResults(shopRequest, ShopStaticUtility.GetTravelerCount(shopRequest.TravelerTypes)) : string.Empty
            };
            if (_configuration.GetValue<bool>("EnableSwatNoFlightFoundFeature"))
            {
                availability.ResponseType = MOBAvailabiltyResponseType.NoFlightFound.GetDescription();
                if (response.Trips != null && response.Trips[0] != null)
                {
                    availability.Title = (_shoppingUtility.CheckFSRRedesignFromShop(shopRequest)) ? response.Trips[0].OriginDecoded.Split(',')[0] + " to " + response.Trips[0].DestinationDecoded.Split(',')[0] : string.Empty;
                }
                //search By airports //_configuration.GetValue<string>("")
                var searchByAirportTitle = _configuration.GetValue<string>("NoFlightFoundSearchNearByAirportTitleText");
                var searchByAirportBody = _configuration.GetValue<string>("NoFlightFoundSearchNearByAirportBodyText");
                var searchByAirportImageAccessibilityText = _configuration.GetValue<string>("NoFlightFoundSearchNearByAirportImageAccessibilityText");
                var searchByAirportImageUrl = GetFormatedUrl(_httpContext.Request.Host.Value,
                                                         _httpContext.Request.Scheme, _configuration.GetValue<string>("NoFlightFoundSearchNearByAirportImageUrl"), true);
                var searchByAirportSeachButtonText = _configuration.GetValue<string>("NoFlightFoundSearchNearByAirportSearchButtonText");
                string otherRequestDataForSearchByAirport = alertMessages.FirstOrDefault(alert => alert != null && string.Equals(alert.AlertType, MOBFSRAlertMessageType.Information.ToString(), StringComparison.OrdinalIgnoreCase))?.Buttons?.FirstOrDefault()?.UpdatedShopRequest != null
                                                                            ? Newtonsoft.Json.JsonConvert.SerializeObject(alertMessages.FirstOrDefault()?.Buttons?.FirstOrDefault()?.UpdatedShopRequest
                                                                            , new Newtonsoft.Json.JsonSerializerSettings()
                                                                            {
                                                                                Converters = new List<Newtonsoft.Json.JsonConverter> {
                                                                                                new Newtonsoft.Json.Converters.StringEnumConverter() },
                                                                                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                                                                            }) : string.Empty;
                var noFlightOtherSearchOptions = new List<MOBNoFlightOtherSearchOption>() {
                                new MOBNoFlightOtherSearchOption () //search by airport
                                     {
                                         BodyText= searchByAirportBody,
                                         Title= searchByAirportTitle,
                                         ImageUrl = searchByAirportImageUrl,
                                         ImageAccessibilityText=searchByAirportImageAccessibilityText,
                                         Buttons = new List<MOBNoFlightButton> (){
                                             new MOBNoFlightButton (){
                                                 ButtonText= searchByAirportSeachButtonText,
                                                 ActionType = MOBSearchButtonActionType.SearchByRequest.GetDescription(),
                                                 OtherRequestData= otherRequestDataForSearchByAirport
                                             }
                                         },
                                         SortingKey = MOBSortNoFlightFoundTiles.SearchByRequest.GetDescription()
                                     }
                            };

                if (await IsAirportInAllowedRegion(shopRequest.Trips[0].Origin, _configuration.GetValue<string>("SearchByMapAllowedCountryList"), shopRequest.SessionId, token, shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.DeviceId) &&
                        !("MD".Equals(shopRequest.SearchType)))
                {
                    var searchByMapTitle = _configuration.GetValue<string>("NoFlightFoundSearchByMapTitleText");
                    var searchByMapBody = _configuration.GetValue<string>("NoFlightFoundSearchByMapBodyText");
                    var searchByMapImageUrl = GetFormatedUrl(_httpContext.Request.Host.Value,
                                                         _httpContext.Request.Scheme, _configuration.GetValue<string>("NoFlightFoundSearchByMapImageUrl"), true);
                    var searchByMapSeachButtonText = _configuration.GetValue<string>("NoFlightFoundSearchByMapSearchButtonText");
                    var searchByMapImageAccessibilityText = _configuration.GetValue<string>("NoFlightFoundSearchByMapImageAccessibilityText");

                    noFlightOtherSearchOptions.Add(
                        new MOBNoFlightOtherSearchOption() //search by map
                        {
                            BodyText = searchByMapBody,
                            Title = searchByMapTitle,
                            ImageUrl = searchByMapImageUrl,
                            ImageAccessibilityText = searchByMapImageAccessibilityText,
                            Buttons = new List<MOBNoFlightButton>(){
                                             new MOBNoFlightButton (){
                                                 ButtonText= searchByMapSeachButtonText,
                                                 ActionType =MOBSearchButtonActionType.SearchByMap.GetDescription()
                                             }
                             },
                            SortingKey = MOBSortNoFlightFoundTiles.SearchByMap.GetDescription()
                        }
                        );
                }
                if (_shoppingUtility.IsReverseSearchByMapOrderForNoFlightsFound(shopRequest.Application.Id, shopRequest.Application.Version.Major, session?.CatalogItems))
                    noFlightOtherSearchOptions.Sort(a => a.SortingKey);

                availability.NoFlightFoundMessage = SetNoFlightFoundMessage(shopRequest, response, session, lstMessages, noFlightOtherSearchOptions); ;
            }
            if (_configuration.GetValue<bool>("EnableNoFlightsSeasonalityFeature"))
                availability.FareWheel = PopulateFareWheelDates(shopRequest?.Trips, "SHOP-NOFLIGHTS-FOUND");
            availability.AwardTravel = shopRequest.AwardTravel;
            return availability;
        }

        private MOBNoFlightFoundMessage SetNoFlightFoundMessage(MOBSHOPShopRequest shopRequest, United.Services.FlightShopping.Common.ShopResponse response, Session session, List<CMSContentMessage> lstMessages, List<MOBNoFlightOtherSearchOption> noFlightOtherSearchOptions)
        {
            United.Mobile.Model.Shopping.MOBNoFlightFoundMessage noFlightFoundMessage = new United.Mobile.Model.Shopping.MOBNoFlightFoundMessage()
            {
                NoFlightOtherSearchOptions = noFlightOtherSearchOptions
            };
            if (_shoppingUtility.IsNoFlightsSeasonalityFeatureEnabled(shopRequest.Application.Id, shopRequest.Application.Version.Major, session.CatalogItems) &&
            response.Warnings?.Where(a => a.MinorCode == _configuration.GetValue<string>("Seasonality201ErrorCode"))?.Any() == true)
            {
                noFlightFoundMessage.NoFlightFoundHelpMessageText = "";
                noFlightFoundMessage.SectionDescription = GetSDLStringMessageFromList(lstMessages, "FSR_Seasonality_NoSeasonality_Description");
                noFlightFoundMessage.SectionMessage = GetSDLStringMessageFromList(lstMessages, "FSR_Seasonality_NoSeasonality_HeaderText");
            }
            else
            {
                noFlightFoundMessage.NoFlightFoundHelpMessageText = GetSDLStringMessageFromList(lstMessages, "FSR_NoFlightsFound_NoFlightFoundPageHelpText");
                noFlightFoundMessage.SectionDescription = GetSDLStringMessageFromList(lstMessages, "FSR_NoFlightsFound_NoFlightFoundSectionDescription");
                noFlightFoundMessage.SectionMessage = GetSDLStringMessageFromList(lstMessages, "FSR_Seasonality_NoFlightFoundSectionText");
            }

            return noFlightFoundMessage;
        }
    }
}
