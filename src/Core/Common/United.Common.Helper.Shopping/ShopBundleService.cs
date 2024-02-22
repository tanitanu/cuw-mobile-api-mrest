using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Definition;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.ShopBundles;
using United.Mobile.Model.Common;
using United.Mobile.Model.CSLModels;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Bundles;
using United.Service.Presentation.CommonModel;
using United.Service.Presentation.PersonalizationRequestModel;
using United.Service.Presentation.PersonalizationResponseModel;
using United.Service.Presentation.ProductModel;
using United.Services.FlightShopping.Common.Extensions;


namespace United.Common.Helper.Shopping
{
    public class ShopBundleService : IShopBundleService
    {
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IBundleOfferService _bundleOfferService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IHeaders _headers;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly IOmniCart _omniCart;
        private readonly IFeatureSettings _featureSettings;
        private readonly ICCEDynamicOfferDetailsService _cceDODService;

        public ShopBundleService(IConfiguration configuration, ISessionHelperService sessionHelperService, IBundleOfferService bundleOfferService,
            IShoppingUtility shoppingUtility, IHeaders headers, IDynamoDBService dynamoDBService, IOmniCart omniCart, IFeatureSettings featureSettings
            , ICCEDynamicOfferDetailsService cceDODService)
        {
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _bundleOfferService = bundleOfferService;
            _shoppingUtility = shoppingUtility;
            _headers = headers;
            _dynamoDBService = dynamoDBService;
            _omniCart = omniCart;
            _featureSettings = featureSettings;
            _cceDODService = cceDODService;
        }

        public async Task<BookingBundlesResponse> GetBundleOffer(BookingBundlesRequest bundleRequest, bool throwExceptionWhenSaveOmniCartFlow = false)

        {
            BookingBundlesResponse response = new BookingBundlesResponse(_configuration);
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(bundleRequest.SessionId, session.ObjectName, new List<string>() { bundleRequest.SessionId, session.ObjectName }).ConfigureAwait(false);
            string logAction = "dynamicOfferdetail";
            if (session == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }

            DynamicOfferDetailRequest dynamicOfferRequest = BuildDynamicOfferRequestForBundles(bundleRequest, session);
            string jsonRequest = JsonConvert.SerializeObject(dynamicOfferRequest);
           
            var jsonResponse = new DynamicOfferDetailResponse(); long s2 = 0;
            if (!await _featureSettings.GetFeatureSettingValue("EnableCCEDynamicOfferUrls").ConfigureAwait(false))
            {
                (jsonResponse, s2) = await _bundleOfferService.DynamicOfferdetail<DynamicOfferDetailResponse>(session.Token, session.SessionId, logAction, jsonRequest).ConfigureAwait(false);
            }
            else
            {
                var str = await _cceDODService.GetCCEDynamicOffersDetail(session.Token, jsonRequest);
                jsonResponse = JsonConvert.DeserializeObject<DynamicOfferDetailResponse>(str);
            }
            
            if (!jsonResponse.IsNullOrEmpty())
            {
                List<BundleProduct> objbundleproduct = new List<BundleProduct>();

                //_logger.LogInformation("GetBundles_CFOP {response} and {@sessionId}", jsonResponse, bundleRequest.SessionId);

                var productDetails = jsonResponse.Offers?.Where(offer => offer.ProductInformation != null)
                                                    .SelectMany(offer => offer.ProductInformation.ProductDetails);
                if (jsonResponse.ResponseData == null && throwExceptionWhenSaveOmniCartFlow)
                {
                    throw new Exception("CCEResponse data is empty or not loaded");
                }
                if (jsonResponse.ResponseData != null && productDetails.Any())
                {
                    var sdlResponseDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(jsonResponse.ResponseData);
                    SDLContentResponseData sdlResponseData = Newtonsoft.Json.JsonConvert.DeserializeObject<SDLContentResponseData>(sdlResponseDataJson);
                    var sdlBundleData = sdlResponseData?.Results;
                    // MOBILE-25395: SAF
                    var safCode = _configuration.GetValue<string>("SAFCode");

                    //TnC
                    var responseUndleTnc = PopulateBundleTnC(response, sdlResponseData.Body);

                    response.Products = new List<BundleProduct>();
                    for (var index = 0; index < sdlBundleData.Length; index++)
                    {
                        var bundleData = sdlBundleData.ElementAt(index);
                        var productDetail = productDetails.FirstOrDefault(x => x?.Product?.Code == bundleData.Code);
                        // MOBILE-25395: SAF
                        if (bundleData.Code != safCode)
                        {
                            var bundleProduct = new BundleProduct();
                            bundleProduct.ProductIndex = index;
                            bundleProduct.ProductCode = productDetail.Product?.Code;
                            bundleProduct.ProductID = productDetail.Product?.SubProducts?.FirstOrDefault(sp => sp.InEligibleReason == null)?.Prices?.FirstOrDefault()?.ID;
                            bundleProduct.Detail = new BundleDetail();
                            bundleProduct.Detail.OfferTitle = $"Bundle offer {index + 1} includes the following additions to your trip:";
                            bundleProduct.Detail.OfferTrips = new List<BundleOfferTrip>();
                            var bundlePrice = productDetail.Product?
                                                           .SubProducts?.FirstOrDefault(sp => sp.InEligibleReason == null)?
                                                           .Prices?.FirstOrDefault()?
                                                           .PaymentOptions?.FirstOrDefault()?
                                                           .PriceComponents?.FirstOrDefault()?
                                                           .Price?
                                                           .Totals?.FirstOrDefault()?
                                                           .Amount;
                            bundleProduct.Tile = new BundleTile();
                            bundleProduct.Tile.OfferTitle = $"Bundle offer {index + 1}";
                            bundleProduct.Tile.OfferPrice = string.Concat("$", bundlePrice);
                            bundleProduct.Tile.PriceText = string.Concat("+$", bundlePrice, "/person");
                            // if CCE returns Type?.Description then it is considered  
                            if (_shoppingUtility.IsEnableMostPopularBundle(bundleRequest.Application.Id, bundleRequest.Application.Version.Major) && productDetail?.Product?.Type?.Description?.ToUpper().Trim() == "PPLBNDL")
                                bundleProduct.Tile.BundleBadgeText = _configuration.GetValue<string>("MostPopularBundleText");

                            bundleProduct.Detail.OfferDetails = new List<BundleOfferDetail>();
                            bundleProduct.Tile.OfferDescription = new List<string>();
                            bundleProduct.BundleProductCodes = new List<string>();
                            bundleData.Products.ForEach(product =>
                            {
                                bundleProduct.Tile.OfferDescription.Add(product.Name);
                                bundleProduct.BundleProductCodes.Add(product.Code);
                            });


                            #region Build Segment Selection screen for bundles details (Trip Details)
                            var nonNullSubProducts = productDetail.Product != null && productDetail.Product.SubProducts?.Count > 0 ? productDetail.Product.SubProducts.Where(sp => sp != null) : new Collection<Service.Presentation.ProductModel.SubProduct>();
                            foreach (var subProduct in nonNullSubProducts)
                            {

                                BundleOfferTrip offerTrip = new BundleOfferTrip();
                                if (subProduct.Prices?.Count > 0)
                                {
                                    offerTrip.TripProductID = String.Join(",", subProduct.Prices.Select(p => p.ID).ToList()); //As we are getting multiple price items for multipax                     
                                    offerTrip.TripProductIDs = subProduct.Prices.Select(p => p.ID).ToList();
                                }
                                offerTrip.Price = Convert.ToInt32(subProduct
                                                                  .Prices?.FirstOrDefault()?
                                                                  .PaymentOptions?.FirstOrDefault()?
                                                                  .PriceComponents?.FirstOrDefault()?
                                                                  .Price?
                                                                  .Totals?.FirstOrDefault()?
                                                                  .Amount);
                                offerTrip.TripId = GetTripId(subProduct, jsonResponse.ODOptions);
                                offerTrip.OriginDestination = GetOriginDestinationDesc(subProduct, jsonResponse.ODOptions, offerTrip.Price);
                                bundleProduct.Detail.OfferTrips.Add(offerTrip);

                                PopulateBundleOfferDetails(subProduct, bundleProduct.Detail.OfferDetails, bundleData.Products);
                            }
                            #endregion
                            response.Products.Add(bundleProduct);
                        }
                        else if (ConfigUtility.IsEnableSAFFeature(session))
                        {
                            // MOBILE-25395: SAF
                            //TnC
                            PopulateBundleTnCForSAF(response, sdlResponseData.Body);

                            var bundleProduct = new BundleProduct();
                            bundleProduct.ProductIndex = index;
                            bundleProduct.ProductCode = productDetail.Product?.Code;
                            bundleProduct.ProductName = bundleData.Products?.FirstOrDefault()?.Name ??
                                                        productDetail.Product?.DisplayName;
                            bundleProduct.ProductID = productDetail.Product?.SubProducts?.FirstOrDefault(sp => sp.InEligibleReason == null)?.Prices?.FirstOrDefault()?.ID;
                            bundleProduct.Detail = new BundleDetail();
                            bundleProduct.Detail.OfferTitle = bundleData.Products?.FirstOrDefault()?.OfferTile ??
                                                              productDetail.Product?.DisplayName;
                            bundleProduct.Detail.OfferTrips = new List<BundleOfferTrip>();
                            var bundlePrice = productDetail.Product?
                                                           .SubProducts?.FirstOrDefault(sp => sp.InEligibleReason == null)?
                                                           .Prices?.FirstOrDefault()?
                                                           .PaymentOptions?.FirstOrDefault()?
                                                           .PriceComponents?.FirstOrDefault()?
                                                           .Price?
                                                           .Totals?.FirstOrDefault()?
                                                           .Amount;
                            bundleProduct.Tile = new BundleTile();
                            bundleProduct.Tile.BackGroundColor = MOBStyledColor.Green.GetDescription();
                            if (await _featureSettings.GetFeatureSettingValue("EnableNewBackGroundColor").ConfigureAwait(false) && Utility.Helper.GeneralHelper.IsApplicationVersionGreaterorEqual(bundleRequest.Application.Id, bundleRequest.Application.Version.Major.ToString(), _configuration.GetValue<string>("Android_Atmos2_New_BackGroundColor_AppVersion"), _configuration.GetValue<string>("iPhone_Atmos2_New_BackGroundColor_AppVersion")))
                            {
                                bundleProduct.Tile.BackGroundColor = MOBStyledColor.AlertGreen.GetDescription();
                            }
                            bundleProduct.Tile.OfferTitle = bundleData.Name ??
                                                            productDetail.Product?.DisplayName;
                            bundleProduct.Tile.OfferPrice = _configuration.GetValue<bool>("IsEnableTilePriceForSAF") ? string.Concat("$", bundlePrice) : _configuration.GetValue<string>("TravelOptionsPageSAFOfferPrice");
                            bundleProduct.Tile.PriceText = bundleProduct.Tile.OfferPrice;
                            bundleProduct.Tile.OfferDescription = new List<string>();
                            bundleProduct.Detail.OfferDetails = new List<BundleOfferDetail>
                            {
                                new BundleOfferDetail() {
                                    OfferDetailDescription = bundleData.Products?.FirstOrDefault()?.ConfigDetails ?? ""
                                }
                            };
                            bundleProduct.BundleProductCodes = new List<string>();
                            bundleData.Products.ForEach(product =>
                            {
                                bundleProduct.Tile.OfferDescription.Add(product.Description);
                                bundleProduct.BundleProductCodes.Add(product.Code);
                            });
                            if (!_configuration.GetValue<bool>("DisableSAFSliderChanges"))
                            {
                                bundleProduct.Detail.IncrementSliderValue = Convert.ToDouble(_configuration.GetValue<string>("SAFIncrementSliderValue"));
                                bundleProduct.Tile.FromText = _configuration.GetValue<string>("TravelOptionsPageFromText");
                            }


                            #region Build SAF Selection screen for bundles details (Trip Details)

                            var nonNullSubProducts = productDetail.Product != null && productDetail.Product.SubProducts?.Count > 0 ? productDetail.Product.SubProducts.Where(sp => sp != null) : new Collection<Service.Presentation.ProductModel.SubProduct>();
                            foreach (var subProduct in nonNullSubProducts)
                            {

                                BundleOfferTrip offerTrip = new BundleOfferTrip();
                                if (subProduct.Prices?.Count > 0)
                                {
                                    offerTrip.TripProductID = String.Join(",", subProduct.Prices.Select(p => p.ID).ToList()); //As we are getting multiple price items for multipax                     
                                    offerTrip.TripProductIDs = subProduct.Prices.Select(p => p.ID).ToList();
                                }
                                var price = subProduct.Prices?.FirstOrDefault()?
                                                                 .PaymentOptions?.FirstOrDefault()?
                                                                 .PriceComponents?.FirstOrDefault()?
                                                                 .Price?
                                                                 .Totals?.FirstOrDefault()?
                                                                 .Amount;
                                offerTrip.Price = Convert.ToInt32(price);
                                if (!_configuration.GetValue<bool>("DisableSAFSliderChanges"))
                                {
                                    offerTrip.Amount = Convert.ToDouble(price);
                                    if (bundleProduct.Detail.OfferTrips.Count() == 1)
                                        offerTrip.IsDefault = true;
                                }
                                offerTrip.TripId = "10" + subProduct.ID;
                                if (_configuration.GetValue<bool>("ShowDecimalValueForSAF"))
                                {
                                    offerTrip.OriginDestination = string.Concat("$", string.Format("{0:#.00}", offerTrip.Amount));

                                }
                                else
                                {
                                    offerTrip.OriginDestination = string.Concat("$", offerTrip.Price);
                                }
                                bundleProduct.Detail.OfferTrips.Add(offerTrip);

                            }

                            #endregion

                            response.Products.Add(bundleProduct);

                        }

                    }

                    //Persisting the offer as we need to send this while registering the offer
                    var productOffer = new GetOffers();
                    productOffer = ObjectToObjectCasting<GetOffers, DynamicOfferDetailResponse>(jsonResponse);
                    await PersistAncillaryProducts(bundleRequest.SessionId, productOffer, true, "Bundle").ConfigureAwait(true);
                }
            }
            else
            {
                if (throwExceptionWhenSaveOmniCartFlow)
                    throw new Exception("CCEResponse is not loaded");
            }
            await _sessionHelperService.SaveSession<BookingBundlesResponse>(response, bundleRequest.SessionId, new List<string>() { bundleRequest.SessionId, response.ObjectName }, response.ObjectName).ConfigureAwait(false);
            return response;

        }

        private static DynamicOfferDetailRequest BuildDynamicOfferRequestForBundles(BookingBundlesRequest bundleRequest, Session session)
        {
            var isAward = false;
            if (ConfigUtility.IsEnableSAFFeature(session))
            {
                isAward = session?.IsAward ?? false;
            }
            var characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>() {
                new United.Service.Presentation.CommonModel.Characteristic(){ Code ="TKT_PRICE" },
                new United.Service.Presentation.CommonModel.Characteristic(){ Code ="MILES_NEEDED", Value="N" },
                new United.Service.Presentation.CommonModel.Characteristic(){ Code ="CartId", Value=bundleRequest.CartId },
                new United.Service.Presentation.CommonModel.Characteristic(){ Code ="Context", Value="TO" },
                new United.Service.Presentation.CommonModel.Characteristic(){ Code ="New", Value="True" },
                new United.Service.Presentation.CommonModel.Characteristic(){ Code ="REVENUE", Value= isAward ? "False" : "True" },
                new United.Service.Presentation.CommonModel.Characteristic(){ Code ="IsEnabledThroughCCE", Value="True" }
            };

            var dynamicOfferRequest = new DynamicOfferDetailRequest()
            {
                Characteristics = characteristics,
                CountryCode = "US",
                CurrencyCode = "USD",
                IsAwardReservation = isAward ? "True" : "False",
                TicketingCountryCode = "US",
                Requester = new ServiceClient()
                {
                    Requestor = new Requestor()
                    {
                        ChannelName = "MBE",
                        LanguageCode = "en"
                    }
                }
            };
            return dynamicOfferRequest;
        }

        private async Task<string> PopulateBundleTnC(BookingBundlesResponse response, SDLBody[] sDLBodies)
        {
            var bundleTnCContent = sDLBodies.FirstOrDefault(body => string.Equals(body.name, "bookingbundle", StringComparison.OrdinalIgnoreCase))?
                                                .content.FirstOrDefault(content => string.Equals(content.name, "booking-tc", StringComparison.OrdinalIgnoreCase))?
                                                .content;
            MOBMobileCMSContentMessages objtermsandconditions = new MOBMobileCMSContentMessages();
            response.TermsAndCondition = response.TermsAndCondition ?? new MOBMobileCMSContentMessages();
            response.TermsAndCondition.Title = "Terms and Conditions";
            response.TermsAndCondition.LocationCode = response.TermsAndCondition.ContentShort = "";
            response.TermsAndCondition.HeadLine = "Travel Options bundle terms and conditions";
            if (!string.IsNullOrEmpty(bundleTnCContent?.body))
            {
                response.TermsAndCondition.ContentFull = bundleTnCContent.body;

            }
            else //fallback, if sdl not giving tnc then we're getting from document library
            {
                //TODO Vishal
                response.TermsAndCondition.ContentFull = await GetBundleTermsandConditons("bundlesTermsandConditons", _headers.ContextValues.SessionId);
            }
            return response.ToString();
        }

        private async Task<string> GetBundleTermsandConditons(string databaseKeys, string sessionId)
        {
            var documentLibrary = new DocumentLibraryDynamoDB(_configuration, _dynamoDBService);
            List<string> docTitles = new List<string>() { "bundlesTermsandConditons" };
            var docs = await documentLibrary.GetNewLegalDocumentsForTitles(docTitles, sessionId /* Headers.ContextValues.SessionId*/);

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

        private string GetTripId(SubProduct subProduct, Collection<Service.Presentation.ProductModel.ProductOriginDestinationOption> ODOptions)
        {
            var odOption = ODOptions.First(od => od.ID == subProduct.Association.ODMappings.First().RefID);
            return odOption.FlightSegments.First().TripIndicator;
        }

        private string GetOriginDestinationDesc(SubProduct subProduct, Collection<Service.Presentation.ProductModel.ProductOriginDestinationOption> ODOptions, int price)
        {
            var odOption = ODOptions.First(od => od.ID == subProduct.Association.ODMappings.First().RefID);
            string odDesc = string.Concat(odOption.FlightSegments.First().DepartureAirport.IATACode, " - ", odOption.FlightSegments.Last().ArrivalAirport.IATACode);
            if (subProduct.Prices?.Count > 0)
            {
                return string.Concat(odDesc, " | ", "$", price, "/person");
            }
            return String.Concat(odDesc, " | ", "Not available");

        }

        private void PopulateBundleOfferDetails(SubProduct subProduct, List<BundleOfferDetail> bundleOfferDetails, SDLProduct[] sDLProducts)
        {
            bundleOfferDetails = bundleOfferDetails ?? new List<BundleOfferDetail>();

            foreach (var product in sDLProducts)
            {
                var bundleOfferDetail = new BundleOfferDetail();
                bundleOfferDetail.OfferDetailDescription = product.Description.Replace("\n", String.Empty);
                bundleOfferDetail.OfferDetailHeader = product.Name;
                if (subProduct?.Extension?.Bundle?.Products.Count > 0)
                {
                    var notAvailable = new List<string>();
                    subProduct.Extension.Bundle.Products.ForEach(sp =>
                    {
                        var additionalExtensions = sp.SubProducts.Where(sub => sub.Extension != null
                                                  && sub.Code == product.Code
                                                  && sub.Extension.AdditionalExtensions != null).Select(sub => sub.Extension.AdditionalExtensions);

                        foreach (var additionalExtension in additionalExtensions)
                        {
                            additionalExtension.ForEach(ae =>
                            {
                                ae.Characteristics.Where(characteristic => string.Equals(characteristic.Value, "false", StringComparison.OrdinalIgnoreCase))
                                                   .ForEach(c => notAvailable.Add($"Not available at {c.Code}"));
                            });
                        }
                    });
                    bundleOfferDetail.OfferDetailWarningMessage = String.Join("\n", notAvailable.Distinct());
                }

                if (!bundleOfferDetails.Any(bod => string.Equals(bod.OfferDetailHeader, bundleOfferDetail.OfferDetailHeader, StringComparison.OrdinalIgnoreCase)))
                {
                    bundleOfferDetails.Add(bundleOfferDetail);
                }
                else
                {
                    var offerdetail = bundleOfferDetails.Find(bod => string.Equals(bod.OfferDetailHeader, bundleOfferDetail.OfferDetailHeader, StringComparison.OrdinalIgnoreCase));
                    offerdetail.OfferDetailWarningMessage = String.Join("\n", (new[] { offerdetail.OfferDetailWarningMessage?.Trim('\n')
                                                                                        , bundleOfferDetail.OfferDetailWarningMessage?.Trim('\n')
                                                                                }).Where(s => !string.IsNullOrEmpty(s)).Distinct());
                }
            }

        }

        private void PopulateBundleTnCForSAF(BookingBundlesResponse response, SDLBody[] sDLBodies)
        {
            var sdlBodyForSAF = sDLBodies.FirstOrDefault(body => string.Equals(body.name, "sfc", StringComparison.OrdinalIgnoreCase));
            var bundleTnCContent = sdlBodyForSAF?.content?.FirstOrDefault(content => string.Equals(content.name, "sfc-tnc", StringComparison.OrdinalIgnoreCase));
            MOBMobileCMSContentMessages objtermsandconditions = new MOBMobileCMSContentMessages();
            response.AdditionalTermsAndCondition = new MOBMobileCMSContentMessages();
            response.AdditionalTermsAndCondition.Title = "Terms and Conditions";
            response.AdditionalTermsAndCondition.LocationCode = response.AdditionalTermsAndCondition.ContentShort = "";
            response.AdditionalTermsAndCondition.HeadLine = sdlBodyForSAF?.metadata.nav_title;
            if (!string.IsNullOrEmpty(bundleTnCContent?.content?.body_text))
            {
                response.AdditionalTermsAndCondition.ContentFull = bundleTnCContent.content.body_text;

            }
        }

        private async System.Threading.Tasks.Task PersistAncillaryProducts(string sessionId, GetOffers productOffer, bool IsCCEDynamicOffer = false, String product = "")
        {

            await System.Threading.Tasks.Task.Run(async () =>
            {
                var persistedProductOffers = new GetOffers();
                persistedProductOffers = await _sessionHelperService.GetSession<GetOffers>(sessionId, persistedProductOffers.ObjectName, new List<string>() { sessionId, persistedProductOffers.ObjectName }).ConfigureAwait(false);
                if (_configuration.GetValue<bool>("EnableOmniCartReleaseCandidateTwoChanges_Bundles") && !string.IsNullOrEmpty(product))
                {
                    //Remove the Existing offer if we are making the dynamicOffer call multiple times with the same session
                    _omniCart.RemoveProductOfferIfAlreadyExists(persistedProductOffers, product);
                }

                if (persistedProductOffers != null && persistedProductOffers.Offers.Count > 0)
                {
                    if (!_configuration.GetValue<bool>("DisablePostBookingPurchaseFailureFix"))//Flightsegments need to be updated when ever we get an offer for the product.
                    {
                        persistedProductOffers.FlightSegments = productOffer.FlightSegments;
                        persistedProductOffers.Travelers = productOffer.Travelers;
                        persistedProductOffers.Solutions = productOffer.Solutions;
                        persistedProductOffers.Response = productOffer.Response;
                        persistedProductOffers.Requester = productOffer.Requester;
                    }

                    if (_configuration.GetValue<bool>("EnableOmniCartReleaseCandidateTwoChanges_Bundles") && !string.IsNullOrEmpty(product) && productOffer.Offers.FirstOrDefault().ProductInformation.ProductDetails != null && productOffer.Offers.FirstOrDefault().ProductInformation.ProductDetails.Count > 0)
                    {
                        foreach (var productDetails in productOffer.Offers.FirstOrDefault().ProductInformation.ProductDetails)
                        {
                            persistedProductOffers.Offers.FirstOrDefault().ProductInformation.ProductDetails.Add(productDetails);
                        }
                    }
                    else
                    {
                        persistedProductOffers.Offers.FirstOrDefault().ProductInformation.ProductDetails.Add(productOffer.Offers.FirstOrDefault().ProductInformation.ProductDetails.FirstOrDefault());
                    }
                }
                else
                {
                    persistedProductOffers = productOffer;
                }
                if (_configuration.GetValue<bool>("EnableOmniCartReleaseCandidateTwoChanges_Bundles"))
                {
                    if (IsCCEDynamicOffer)
                    {
                        if (productOffer.Characteristics == null)
                        {
                            productOffer.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                        }
                        if (persistedProductOffers != null && !persistedProductOffers.Characteristics.Any(characteristic => characteristic.Code == "IsEnabledThroughCCE"))
                        {
                            productOffer.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic { Code = "IsEnabledThroughCCE", Value = "True" });
                        }
                    }
                    else// Need to remove this characteristics when IsCCEDynamicOffer==false ,As this is the same method we use for saving the postbooking products (PBS and PCU) at that we shouldnt send this characteristis as we are going to flightshoppig.
                    {
                        if (persistedProductOffers != null && persistedProductOffers.Characteristics?.Any(characteristic => characteristic.Code == "IsEnabledThroughCCE") == true)
                        {
                            persistedProductOffers.Characteristics.Remove(persistedProductOffers.Characteristics.First(characteristic => characteristic.Code == "IsEnabledThroughCCE"));
                        }
                    }
                }
                await _sessionHelperService.SaveSession<GetOffers>(persistedProductOffers, sessionId, new List<string>() { sessionId, persistedProductOffers.ObjectName }, persistedProductOffers.ObjectName).ConfigureAwait(false);
            });
        }

        private T ObjectToObjectCasting<T, R>(R request)
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
    }
}
