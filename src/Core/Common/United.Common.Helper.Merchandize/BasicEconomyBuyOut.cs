using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Common;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.MPRewards;
using United.Service.Presentation.PersonalizationResponseModel;
using United.Service.Presentation.SegmentModel;

namespace United.Common.Helper.Merchandize
{
    public class BasicEconomyBuyOut
    {
        private const string BE_BUYOUT_PRODUCT_CODE = "BEB";
        private readonly DynamicOfferDetailResponse _offerResponse;
        private readonly SDLProduct _sdlContent;
        private readonly int _noOfTravelers;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        public MOBBasicEconomyBuyOut BasicEconomyBuyOutOffer { get; private set; }

        public BasicEconomyBuyOut(string sessionId, IConfiguration configuration, ISessionHelperService sessionHelperService)
        {
            _configuration = configuration;
            var productOfferCce = new GetOffersCce();
            productOfferCce = sessionHelperService.GetSession<GetOffersCce>(sessionId, productOfferCce.ObjectName, new List<string> { sessionId, productOfferCce.ObjectName }).Result;

            this._offerResponse = string.IsNullOrEmpty(productOfferCce?.OfferResponseJson)
                                    ? null
                                    : JsonConvert.DeserializeObject<DynamicOfferDetailResponse>(productOfferCce?.OfferResponseJson);
            this._sdlContent = GetSdlContent(_offerResponse);
            this._noOfTravelers = _offerResponse?.Travelers?.Count ?? 0;
        }

        public BasicEconomyBuyOut(DynamicOfferDetailResponse offerResponse, IConfiguration configuration)
        {
            this._offerResponse = offerResponse;
            this._sdlContent = GetSdlContent(offerResponse);
            this._noOfTravelers = offerResponse?.Travelers?.Count ?? 0;
            _configuration = configuration;
        }

        private SDLProduct GetSdlContent(DynamicOfferDetailResponse offerResponse)
        {
            if (offerResponse?.ResponseData == null)
                return null;

            SDLContentResponseData sdlData = offerResponse.ResponseData.ToObject<SDLContentResponseData>();
            return sdlData?.Results
                          ?.FirstOrDefault(r => r?.Code == BE_BUYOUT_PRODUCT_CODE)
                          ?.Products
                          ?.FirstOrDefault();
        }

        internal BasicEconomyBuyOut BuildBuyOutOptions()
        {
            if (_offerResponse == null || _offerResponse.Offers == null || !_offerResponse.Offers.Any())
                return this;

            var offerPrice = GetOfferPrice();
            if (offerPrice <= 0)
                return this;

            if (!HasRequriedContent())
                return null;

            BasicEconomyBuyOutOffer = new MOBBasicEconomyBuyOut
            {
                OfferTile = BuildOfferTile(offerPrice),
                ElfRestrictionsBeBuyOutLink = ElfRestrictionsBeBuyOutLink(),
                ProductCode = BE_BUYOUT_PRODUCT_CODE,
                ProductIds = GetProductIds(),
                ProductDetail = GetProductDetail(),
                Captions = GetCaptions(),
                MobileCmsContentMessages = GetTermsAndConditions(),
                Faqs = GetFaqs()
            };

            return this;
        }

        private List<MOBItem> GetFaqs()
        {
            if (_offerResponse?.ResponseData == null)
                return null;

            SDLContentResponseData sdlData = _offerResponse.ResponseData.ToObject<SDLContentResponseData>();
            var faqs = sdlData?.Body
                              ?.FirstOrDefault(b => b?.name?.Equals(BE_BUYOUT_PRODUCT_CODE, StringComparison.OrdinalIgnoreCase) ?? false)
                              ?.content
                              ?.FirstOrDefault(c => c?.name?.Equals("beb-FAQ-List", StringComparison.OrdinalIgnoreCase) ?? false)
                              ?.content
                              ?.sections
                              ?.Select(s => new MOBItem { Id = s?.content?.title, CurrentValue = s?.content?.body })
                              ?.TakeWhile(faq => !string.IsNullOrEmpty(faq?.Id) || !string.IsNullOrEmpty(faq?.CurrentValue))
                              ?.ToList();

            if (!(faqs?.Any() ?? false))
                return null;

            return faqs;
        }

        /// <summary>
        /// This method extracts Terms And Conditions for BE-BuyOut from SDL Content response
        /// </summary>
        /// <returns>MOBMobileCMSContentMessages T&C's for BE-BuyOut</returns>
        public MOBMobileCMSContentMessages GetTermsAndConditions()
        {
            if (_offerResponse?.ResponseData == null)
                return null;

            SDLContentResponseData sdlData = _offerResponse.ResponseData.ToObject<SDLContentResponseData>();
            var termsAndConditions = sdlData?.Body
                                            ?.FirstOrDefault(b => b?.name?.Equals(BE_BUYOUT_PRODUCT_CODE, StringComparison.OrdinalIgnoreCase) ?? false)
                                            ?.content
                                            ?.FirstOrDefault(c => c?.name?.Equals("beb-Terms-And-Conditions", StringComparison.OrdinalIgnoreCase) ?? false)
                                            ?.content;

            if (termsAndConditions == null || string.IsNullOrEmpty(termsAndConditions?.body) || string.IsNullOrEmpty(termsAndConditions?.title) || string.IsNullOrEmpty(termsAndConditions?.subtitle))
                return null;

            return new MOBMobileCMSContentMessages
            {
                Title = $"{termsAndConditions?.title}",
                ContentShort = _configuration.GetValue<string>("PaymentTnCMessage"),
                HeadLine = $"{termsAndConditions?.subtitle}",
                ContentFull = $"{termsAndConditions?.body}"
            };
        }


        private List<MOBItem> GetCaptions()
        {
            var configurationPageText = _sdlContent?.ConfigDetails?.Split('|');
            if (configurationPageText != null && configurationPageText.Count() >= 4)
            {
                return new List<MOBItem>
            {
                new MOBItem { Id = "BeBuyOutPageTitle", CurrentValue = configurationPageText[0] },
                new MOBItem { Id = "BeBuyOutHeader", CurrentValue = configurationPageText[1]}, //"Change your fare to Economy" //_sdlContent?.Name
                new MOBItem { Id = "BeBuyOutHeaderInfo", CurrentValue =configurationPageText[2] },//,GetFormattedHtml(_sdlContent?.ConfigDetails)// "With an Economy fare, you’ll have the option to:<br/>- Bring one full-sized carry-on item onboard<br/>- Select your seat in Economy cabin<br/>- Purchase Economy Plus or Premium Cabin seats<br/>- Change your ticket"},
                new MOBItem { Id = "ContinueButton", CurrentValue = configurationPageText[3]}, //$"Add for ${GetTotalPrice()}"
                new MOBItem { Id = "FAQTitle", CurrentValue = "FAQs"}, //$"{_sdlContent?.Name} Frequently Asked Questions"//Change your fare to Economy Frequently Asked Questions
                new MOBItem { Id = "FAQToolTip", CurrentValue = "FAQs"}
            };
            }

            return null;
        }

        private List<MOBBeBuyOutSegment> GetProductDetail()
        {
            var offerprice = _configuration.GetValue<bool>("DisableDecimalFormat") ? GetBEBPrice() : GetBEBPriceV2();                
            if (offerprice > 0)
            {
                return _offerResponse?.Solutions?.FirstOrDefault()?.ODOptions?.Select(od => new MOBBeBuyOutSegment
                {
                    Segment = GetSegmentInfo(od.FlightSegments),
                    Warning = GetWarningMessage(od.FlightSegments), //"Flight occured in the past"
                    Message = GetProductDetailHeaderText(od.FlightSegments, offerprice)
                }).ToList();
            }
            return null;
        }

        private string GetProductDetailHeaderText(Collection<ProductFlightSegment> flightSegments, decimal price)
        {
            return GetWarningMessage(flightSegments) ? "Flight occured in the past" : $"${price} per person";
        }

        private bool GetWarningMessage(Collection<ProductFlightSegment> flightSegments)
        {
            return (flightSegments?.Any(f => f?.IsActive?.ToUpper().Trim() == "N") ?? false) ? true : false;
        }

        private string GetSegmentInfo(Collection<ProductFlightSegment> flightSegments)
        {
            if (!(flightSegments?.Any() ?? false))
                return string.Empty;

            return $"{GetLineOfFlight(flightSegments)}";
        }

        private string GetLineOfFlight(Collection<ProductFlightSegment> flightSegments)
        {
            return $"{flightSegments?.FirstOrDefault()?.DepartureAirport?.IATACode} - {flightSegments?.LastOrDefault()?.ArrivalAirport?.IATACode}";
        }

        private decimal GetBEBPrice()
        {
            if (_offerResponse == null || _offerResponse.Offers == null || !_offerResponse.Offers.Any())
                return 0;

            var subProducts = _offerResponse?.Offers
                                            ?.FirstOrDefault()
                                            ?.ProductInformation?.ProductDetails
                                            ?.Where(p => p?.Product?.Code == BE_BUYOUT_PRODUCT_CODE)
                                            ?.SelectMany(p => p?.Product?.SubProducts ?? null)
                                            ?.TakeWhile(sp => sp != null);

            var totalPerPaxForAllSegments = subProducts?.Select(sp => sp?.Prices?.FirstOrDefault())
                                                       ?.Select(p => p?.PaymentOptions?.FirstOrDefault())
                                                       ?.Select(po => po?.PriceComponents?.FirstOrDefault())
                                                       ?.Select(pc => pc?.Price?.Totals?.FirstOrDefault()?.Amount)
                                                       ?.TakeWhile(a => a > 0).FirstOrDefault();




            return (decimal)Math.Round(Convert.ToDouble(totalPerPaxForAllSegments));
        }


        private List<string> GetProductIds()
        {
            return _offerResponse?.Offers?.FirstOrDefault()
                                 ?.ProductInformation?.ProductDetails
                                 ?.FirstOrDefault(p => p.Product?.Code == BE_BUYOUT_PRODUCT_CODE)
                                 ?.Product
                                 ?.SubProducts
                                 ?.Where(sp => sp != null && sp.Prices != null && sp.Prices.Any())
                                 ?.SelectMany(sp => sp?.Prices)
                                 ?.TakeWhile(p => p?.PaymentOptions?.FirstOrDefault()?.PriceComponents?.FirstOrDefault()?.Price?.Totals.FirstOrDefault()?.Amount > 0)
                                 ?.Select(p => p?.ID)
                                 ?.ToList();

        }


        private MOBItem ElfRestrictionsBeBuyOutLink()
        {
            if (string.IsNullOrEmpty(_sdlContent?.ComponentTitle))
                return null;

            return new MOBItem { Id = "ELFRestrictionsBeBuyOutLink", CurrentValue = _sdlContent?.ComponentTitle };//Change your fare to Economy for less restrictions
        }

        private MOBBeBuyOutOffer BuildOfferTile(decimal offerPrice)
        {
            var offerTileText = _sdlContent?.OfferTile?.Split('|');

            if (offerPrice > 0 && offerTileText?.Count() >= 8)
            {
                return new MOBBeBuyOutOffer
                {
                    Title = offerTileText[0], // Want more flexibility
                    Header = offerTileText[1], //Switch your fare to Economy
                    ELFShopOptions = BuildShopOptions(offerTileText),
                    CurrencyCode = "$",
                    Price = offerPrice,
                    Text1 = "+ $" + offerPrice.ToString(),
                    Text2 = offerTileText[2], //per person
                    Button = offerTileText[3] //Switch to Economy
                };
            }

            return null;
        }

        private List<MOBSHOPOption> BuildShopOptions(string[] offerTileText)
        {
            if (offerTileText == null || offerTileText.Count() < 8)
                return null;

            List<MOBSHOPOption> elfOptions = new List<MOBSHOPOption>();
            for (int i = 5; i < offerTileText.Count(); i++)
            {
                if (offerTileText[i].Contains("~"))
                {
                    MOBSHOPOption elfOption = new MOBSHOPOption()
                    {
                        OptionDescription = offerTileText[i]?.Split('~')[1],
                        OptionIcon = offerTileText[i]?.Split('~')[0]
                    };
                    elfOptions.Add(elfOption);
                }
            }
            return elfOptions;
        }

        /// <summary>
        /// This method validates the SDL content to make sure we have minimum required content to show offer tile and product page
        /// </summary>
        /// <returns>boolean</returns>
        private bool HasRequriedContent()
        {
            return _sdlContent?.OfferTile?.Count() >= 8 &&
                   !string.IsNullOrEmpty(_sdlContent?.Name) && !string.IsNullOrEmpty(_sdlContent?.ConfigDetails);
        }

        private decimal GetOfferPrice()
        {
            if (_offerResponse == null || _offerResponse.Offers == null || !_offerResponse.Offers.Any())
                return 0;

            var subProducts = _offerResponse?.Offers
                                            ?.FirstOrDefault()
                                            ?.ProductInformation?.ProductDetails
                                            ?.Where(p => p?.Product?.Code == BE_BUYOUT_PRODUCT_CODE)
                                            ?.SelectMany(p => p?.Product?.SubProducts ?? null)
                                            ?.TakeWhile(sp => sp != null);

            var totalPerPaxForAllSegments = subProducts?.Select(sp => sp?.Prices?.FirstOrDefault())
                                                       ?.Select(p => p?.PaymentOptions?.FirstOrDefault())
                                                       ?.Select(po => po?.PriceComponents?.FirstOrDefault())
                                                       ?.Select(pc => _configuration.GetValue<bool>("DisableDecimalFormat") ?
                                                           Convert.ToDecimal(pc?.Price?.Totals?.FirstOrDefault()?.Amount) : Convert.ToDecimal(pc?.Price?.FareCalculation))                                                       
                                                       ?.TakeWhile(a => a > 0)
                                                       ?.Sum() ?? 0;

            return _configuration.GetValue<bool>("DisableDecimalFormat") ? (decimal)Math.Round(totalPerPaxForAllSegments) : totalPerPaxForAllSegments;
        }

        private decimal GetBEBPriceV2()
        {
            if (_offerResponse == null || _offerResponse.Offers == null || !_offerResponse.Offers.Any())
                return 0;

            var subProducts = _offerResponse?.Offers
                                            ?.FirstOrDefault()
                                            ?.ProductInformation?.ProductDetails
                                            ?.Where(p => p?.Product?.Code == BE_BUYOUT_PRODUCT_CODE)
                                            ?.SelectMany(p => p?.Product?.SubProducts ?? null)
                                            ?.TakeWhile(sp => sp != null);

            var totalPerPaxForAllSegments = subProducts?.Select(sp => sp?.Prices?.FirstOrDefault())
                                                       ?.Select(p => p?.PaymentOptions?.FirstOrDefault())
                                                       ?.Select(po => po?.PriceComponents?.FirstOrDefault())
                                                       ?.Select(pc => Convert.ToDecimal(pc?.Price?.FareCalculation))
                                                       ?.TakeWhile(a => a > 0).FirstOrDefault();




            return (decimal)totalPerPaxForAllSegments;
        }
    }
}
