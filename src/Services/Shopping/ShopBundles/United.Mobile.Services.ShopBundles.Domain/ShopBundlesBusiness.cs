using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Common.HelperSeatEngine;
using United.Definition;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.ShopBundles;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Bundles;
using United.Service.Presentation.CommonModel;
using United.Service.Presentation.PersonalizationRequestModel;
using United.Service.Presentation.PersonalizationResponseModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.Cart;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.Extensions;
using United.Utility.Helper;
using Characteristic = United.Service.Presentation.CommonModel.Characteristic;
using Product = United.Services.FlightShopping.Common.Product;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using SubProduct = United.Service.Presentation.ProductModel.SubProduct;

namespace United.Mobile.Services.ShopBundles.Domain
{
    public class ShopBundlesBusiness : IShopBundlesBusiness
    {
        private readonly ICacheLog<ShopBundlesBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IFlightShoppingService _flightShoppingService;
        private readonly IUnfinishedBookingService _unfinishedBookingService;
        private readonly IDPService _dPService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly IOmniCart _omniCart;
        private readonly ISeatMapCSL30 _seatMapCSL30;
        private readonly ITravelerUtility _travelerUtility;
        private readonly IBundleOfferService _bundleOfferService;
        private readonly IShopBundleService _shopBundleService;
        private readonly IFeatureSettings _featureSettings;
        private Stopwatch stopwatch;

        public ShopBundlesBusiness(ICacheLog<ShopBundlesBusiness> logger
            , IConfiguration configuration
            , IHeaders headers
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility
            , IFlightShoppingService flightShoppingService
            , IUnfinishedBookingService unfinishedBookingService
            , IDPService dPService
            , IDynamoDBService dynamoDBService
            , IOmniCart omniCart
            , ISeatMapCSL30 seatMapCSL30
            , ITravelerUtility travelerUtility
            , IBundleOfferService bundleOfferService
            , IShopBundleService shopBundleService
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _headers = headers;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
            _flightShoppingService = flightShoppingService;
            _unfinishedBookingService = unfinishedBookingService;
            _dPService = dPService;
            _dynamoDBService = dynamoDBService;
            _omniCart = omniCart;
            _seatMapCSL30 = seatMapCSL30;
            _travelerUtility = travelerUtility;
            _bundleOfferService = bundleOfferService;
            _shopBundleService = shopBundleService;
            _featureSettings = featureSettings;
            stopwatch = new Stopwatch();
        }

        public async Task<BookingBundlesResponse> GetBundles_CFOP(BookingBundlesRequest bookingBundlesRequest)
        {
            stopwatch.Reset();
            stopwatch.Start();

            BookingBundlesResponse response = new BookingBundlesResponse(_configuration);

            #region Action to Map CSL Bundle response to REST Response

            var persistedReservation = await _sessionHelperService.GetSession<Reservation>(bookingBundlesRequest.SessionId, new Reservation().ObjectName, new List<string>() { bookingBundlesRequest.SessionId, new Reservation().ObjectName });
            var tupleRes = await _omniCart.IsOmniCartFlow_BundlesAlreadyLoaded(response, persistedReservation, bookingBundlesRequest, bookingBundlesRequest.SessionId);
            response = tupleRes.bundleResponse;
            if (!tupleRes.returnValue)
            {
                if (persistedReservation != null && persistedReservation?.ShopReservationInfo2?.NextViewName != "RTI" 
                    || (_omniCart.IsEnableOmniCartReleaseCandidateThreeChanges_Seats(bookingBundlesRequest.Application.Id, bookingBundlesRequest.Application.Version.Major) && persistedReservation?.ShopReservationInfo2?.IsOmniCartSavedTripFlow == true)//Added this condition as we need to call the Service to get the offer when customer directly redirected to final rti from omni flow (Sceanrio : Bundles not selected and seats selected for all segments )
                        )
                {
                    if (!(_omniCart.IsEnableOmniCartReleaseCandidateTwoChanges_Bundles(bookingBundlesRequest.Application.Id, bookingBundlesRequest.Application.Version.Major)))
                    {
                        if (_shoppingUtility.EnableUnfinishedBookings(bookingBundlesRequest) // toggle
                    && persistedReservation.ShopReservationInfo2.IsUnfinihedBookingPath && (!_omniCart.IsEnableOmniCartHomeScreenChanges(bookingBundlesRequest.Application.Id, bookingBundlesRequest.Application.Version.Major)))
                        {
                            Session session = new Session();

                            session = await _sessionHelperService.GetSession<Session>(bookingBundlesRequest.SessionId, new Session().ObjectName, new List<string>() { bookingBundlesRequest.SessionId, new Session().ObjectName });

                            var persistedShopPindownRequest = await _sessionHelperService.GetSession<ShopRequest>(bookingBundlesRequest.SessionId, new ShopRequest().GetType().FullName, new List<string>() { bookingBundlesRequest.SessionId, new ShopRequest().GetType().FullName });

                            if (persistedShopPindownRequest == null)
                            {
                                throw new MOBUnitedException("Could not find persisted shopppindown request.");
                            }

                            persistedShopPindownRequest.BundleProductCode = United.Services.FlightShopping.Common.Bundles.StaticModel.BundlesCode.SBE;
                            persistedShopPindownRequest.CartId = session.CartId;

                            response = await GetBundleDetails_ForUnfinishedBooking(persistedShopPindownRequest, session, bookingBundlesRequest.Application);

                            if (_configuration.GetValue<bool>("BundleCart"))
                            {

                                MOBShoppingCart persistShoppingCart = new MOBShoppingCart();
                                persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(session.SessionId, persistShoppingCart.ObjectName, new List<string>() { session.SessionId, persistShoppingCart.ObjectName});

                                if (persistShoppingCart != null)
                                {
                                    persistShoppingCart.BundleCartId = response.CartId;
                                    //persistedReservation.ShopReservationInfo2.BundleCartID = response.CartId;


                                    await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, session.SessionId, new List<string>() { session.SessionId, persistShoppingCart.ObjectName}, persistShoppingCart.ObjectName);
                                }
                            }

                        }
                        else
                        {
                            response = await GetBundleDetails_CFOP(bookingBundlesRequest);
                        }
                        await _sessionHelperService.SaveSession<BookingBundlesResponse>(response, bookingBundlesRequest.SessionId, new List<string>() { bookingBundlesRequest.SessionId, response.ObjectName }, response.ObjectName);

                    }
                    else
                    {
                        if (!_configuration.GetValue<bool>("DisableBundlesMethodsInCommonPlace"))
                        {
                            response = await _shopBundleService.GetBundleOffer(bookingBundlesRequest);
                        }
                        else 
                        {
                            response = await GetBundleOffer(bookingBundlesRequest);
                        }                 
                    }
                }
                else
                {
                    response = await _sessionHelperService.GetSession<BookingBundlesResponse>(bookingBundlesRequest.SessionId, response.ObjectName, new List<string>() { bookingBundlesRequest.SessionId, response.ObjectName });
                }
                if (_omniCart.IsEnableOmniCartMVP2Changes(bookingBundlesRequest.Application.Id, bookingBundlesRequest.Application.Version.Major, persistedReservation?.ShopReservationInfo2?.IsDisplayCart == true))
                {
                    if (!bookingBundlesRequest.IsBackNavigationFromRTI)
                    {
                        response.ClearOption = CartClearOption.ClearBundles.ToString().ToUpper();
                    }
                    else
                    {
                        response.ClearOption = String.Empty;
                    }                    
                    await _sessionHelperService.SaveSession<BookingBundlesResponse>(response, bookingBundlesRequest.SessionId, new List<string>() { bookingBundlesRequest.SessionId, response.ObjectName}, response.ObjectName);
                }
            }
            if (persistedReservation?.TravelOptions?.Count > 0 && response.Products?.Count > 0)
            {
                for (int i = 0; i < response.Products.Count; i++)
                {

                    for (int j = 0; j < persistedReservation.TravelOptions.Count; j++)
                    {
                        if (persistedReservation.TravelOptions[j].SubItems[0].ProductId == response.Products[i].ProductCode)
                        {
                            response.Products[i].Tile.IsSelected = true;

                            for (int k = 0; k < persistedReservation.TravelOptions[j].TripIds.Count; k++)
                            {
                                for (int l = 0; l < response.Products[i].Detail.OfferTrips.Count; l++)
                                {
                                    if (persistedReservation.TravelOptions[j].TripIds[k] == response.Products[i].Detail.OfferTrips[l].TripId)
                                    {
                                        response.Products[i].Detail.OfferTrips[l].IsChecked = true;
                                    }
                                }
                            }

                        }
                    }
                }
            }
            response.Products = response.Products?.Count > 0 ? response.Products : new List<BundleProduct>();

            PopulateEconomyPlusWarningMessage(persistedReservation, response);
            response.Flow = bookingBundlesRequest.Flow;
            response.SessionId = bookingBundlesRequest.SessionId;
            response.CartId = bookingBundlesRequest.CartId;
            #endregion

            // Feature TravelInsuranceOptimization : MOBILE-21191, MOBILE-21193, MOBILE-21195, MOBILE-21197
            if (_configuration.GetValue<bool>("EnableTravelInsuranceOptimization") &&
                persistedReservation?.TripInsuranceFile != null)
            {
                response.TripInsuranceInfoBookingPath = persistedReservation.TripInsuranceFile.TripInsuranceBookingInfo;
            }
            stopwatch.Stop();
            response.CallDuration = stopwatch.ElapsedMilliseconds;

            return response;
        }

        private async Task<BookingBundlesResponse> GetBundleOffer(BookingBundlesRequest bundleRequest, bool throwExceptionWhenSaveOmniCartFlow = false)

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
            var jsonResponse = await _bundleOfferService.DynamicOfferdetail<DynamicOfferDetailResponse>(session.Token, session.SessionId, logAction, jsonRequest).ConfigureAwait(false);

            if (!jsonResponse.IsNullOrEmpty())
            {
                List<BundleProduct> objbundleproduct = new List<BundleProduct>();

                //_logger.LogInformation("GetBundles_CFOP {response} and {@sessionId}", jsonResponse, bundleRequest.SessionId);

                var productDetails = jsonResponse.response.Offers?.Where(offer => offer.ProductInformation != null)
                                                    .SelectMany(offer => offer.ProductInformation.ProductDetails);
                if (jsonResponse.response.ResponseData == null && throwExceptionWhenSaveOmniCartFlow)
                {
                    throw new Exception("CCEResponse data is empty or not loaded");
                }
                if (jsonResponse.response.ResponseData != null && productDetails.Any())
                {
                    var sdlResponseDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(jsonResponse.response.ResponseData);
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
                                offerTrip.TripId = GetTripId(subProduct, jsonResponse.response.ODOptions);
                                offerTrip.OriginDestination = GetOriginDestinationDesc(subProduct, jsonResponse.response.ODOptions, offerTrip.Price);
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
                                    if(bundleProduct.Detail.OfferTrips.Count() == 1)
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
                    productOffer = _travelerUtility.ObjectToObjectCasting<GetOffers, DynamicOfferDetailResponse>(jsonResponse.response);
                    await PersistAncillaryProducts(bundleRequest.SessionId, productOffer, true, "Bundle").ConfigureAwait(true);
                }
            }
            else
            {
                if (throwExceptionWhenSaveOmniCartFlow)
                    throw new Exception("CCEResponse is not loaded");
            }
           await _sessionHelperService.SaveSession<BookingBundlesResponse>(response, bundleRequest.SessionId, new List<string>() { bundleRequest.SessionId, response.ObjectName}, response.ObjectName).ConfigureAwait(false);
            return response;

        }

        private async System.Threading.Tasks.Task PersistAncillaryProducts(string sessionId, GetOffers productOffer, bool IsCCEDynamicOffer = false, String product = "")
        {

            await System.Threading.Tasks.Task.Run(async() =>
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
                            productOffer.Characteristics = new Collection<Characteristic>();
                        }
                        if (persistedProductOffers != null && !persistedProductOffers.Characteristics.Any(characteristic => characteristic.Code == "IsEnabledThroughCCE"))
                        {
                            productOffer.Characteristics.Add(new Characteristic { Code = "IsEnabledThroughCCE", Value = "True" });
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

        private string GetTripId(SubProduct subProduct, Collection<Service.Presentation.ProductModel.ProductOriginDestinationOption> ODOptions)
        {
            var odOption = ODOptions.First(od => od.ID == subProduct.Association.ODMappings.First().RefID);
            return odOption.FlightSegments.First().TripIndicator;
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
                response.TermsAndCondition.ContentFull =await GetBundleTermsandConditons("bundlesTermsandConditons", _headers.ContextValues.SessionId);
            }
            return response.ToString();
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

        private static DynamicOfferDetailRequest BuildDynamicOfferRequestForBundles(BookingBundlesRequest bundleRequest, Session session)
        {
            var isAward = false;
            if (ConfigUtility.IsEnableSAFFeature(session))
            {
                isAward = session?.IsAward ?? false;
            }
            var characteristics = new Collection<Characteristic>() {
                new Characteristic(){ Code ="TKT_PRICE" },
                new Characteristic(){ Code ="MILES_NEEDED", Value="N" },
                new Characteristic(){ Code ="CartId", Value=bundleRequest.CartId },
                new Characteristic(){ Code ="Context", Value="TO" },
                new Characteristic(){ Code ="New", Value="True" },
                new Characteristic(){ Code ="REVENUE", Value= isAward ? "False" : "True" },
                new Characteristic(){ Code ="IsEnabledThroughCCE", Value="True" }
            };
            var dynamicOfferRequest = new DynamicOfferDetailRequest()
            {
                Characteristics = characteristics,
                CountryCode = "US",
                CurrencyCode = "USD",
                IsAwardReservation = isAward ? "True":"False",
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

        private async Task<BookingBundlesResponse> GetBundleDetails_ForUnfinishedBooking(United.Services.FlightShopping.Common.ShopRequest persistedShopPindownRequest, Session session, MOBApplication app)
        {
            BookingBundlesResponse response = new BookingBundlesResponse(_configuration);
            var bundleResponse = await _unfinishedBookingService.GetShopPinDown<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(session.Token, session.SessionId, persistedShopPindownRequest);
            if (_configuration.GetValue<bool>("BundleCart"))
            {
                response.CartId = bundleResponse.CartId;
            }

            var objtermsandconditions = new MOBMobileCMSContentMessages();
            List<BundleProduct> objbundleproduct = new List<BundleProduct>();
            BundleTile objTile;
            BundleDetail objDetail;
            List<BundleOfferDetail> objOfferDetails;
            List<BundleOfferTrip> offerTrips;

            List<string> offerDescription;

            #region Bundles

            var bundleproductsAll = bundleResponse.DisplayCart.DisplayTrips.Where(t => t != null && t.Flights != null)
                    .SelectMany(t => t.Flights).Where(f => f != null && f.Products != null).SelectMany(f => f.Products).
                    Where(p => p != null && p.IsBundleProduct == true && p.ProductId != "").ToList();
            var bundleproducts = new List<Product>();
            foreach (var prod in bundleproductsAll)
            {
                if (bundleproducts.Count < 4 && !bundleproducts.Exists(p => p.ProductType == prod.ProductType))
                {
                    bundleproducts.Add(prod);
                }
            }

            #region BundleBinding
            for (int i = 0; i < bundleproducts.Count; i++)
            {
                //MOBBundleOfferTrip objmoboffertrips = new MOBBundleOfferTrip();
                if (bundleproducts[i].IsBundleProduct == true)
                {
                    objTile = new BundleTile();
                    objDetail = new BundleDetail();

                    objbundleproduct.Add(new BundleProduct { ProductID = bundleproducts[i].ProductId, ProductIDs = bundleproducts[i].ProductId.Split(',').ToList(), ProductCode = bundleproducts[i].ProductType, ProductIndex = i });
                    offerTrips = new List<BundleOfferTrip>();
                    offerDescription = new List<string>();
                    objOfferDetails = new List<BundleOfferDetail>();
                    if (bundleproducts[i].SubProducts != null)
                    {
                        BundleOfferDetail objmobofferdetails = new BundleOfferDetail();
                        foreach (var v in bundleproducts[i].SubProducts)
                        {
                            objmobofferdetails = new BundleOfferDetail();
                            v.Description = MakeFirstlettercapital(v.Description.Replace("Club&#8480", "Club℠"));
                            objmobofferdetails.OfferDetailHeader = v.Description;
                            //v.AlternateDescription = v.AlternateDescription.Replace("Club&#8480", "Club℠");
                            string detailDescription = null;
                            if (_configuration.GetValue<bool>("BundlSubProductDeatilDesc_FromCSL"))
                            {
                                if (!string.IsNullOrWhiteSpace(v.SubDescription))
                                {
                                    detailDescription = PutFullStopOnEndOfString(MakeFirstlettercapital(v.SubDescription.Trim()));
                                }
                            }
                            else
                            {
                                detailDescription = BundleSubProductDeailDescription(v.Code);
                            }

                            objmobofferdetails.OfferDetailDescription = string.IsNullOrEmpty(detailDescription) ? PutFullStopOnEndOfString(MakeFirstlettercapital(v.AlternateDescription.Replace("Club&#8480", "Club℠"))) : detailDescription;
                            //if (v.ReferencedSegments != null)
                            //{
                            //    if (v.ReferencedSegments[0].IsEligible == false)
                            //    {
                            objmobofferdetails.OfferDetailWarningMessage = GetOfferDetailWarningMessage(bundleResponse.DisplayCart.DisplayTrips, bundleproducts[i].ProductType, objmobofferdetails, v.EddCode);

                            //        objmobofferdetails.OfferDetailWarningMessage = "Not available for" + v.ReferencedSegments[0].Origin + '-' + v.ReferencedSegments[0].Destination;
                            //    }
                            //}
                            offerDescription.Add(v.Description);
                            objOfferDetails.Add(objmobofferdetails);
                        }

                    }
                    objTile.IsSelected = false;
                    int j = i + 1;
                    objTile.OfferTitle = "Bundle offer " + j;
                    objTile.OfferDescription = offerDescription;
                    if (bundleproducts[i].Prices.Count >= 1)
                    {
                        objTile.PriceText = "+$" + bundleproducts[i].Prices[0].Amount.ToString() + "/person";
                        objTile.OfferPrice = "$" + bundleproducts[i].Prices[0].Amount.ToString();
                    }
                    objTile.TileIndex = i;

                    objDetail.OfferTitle = objTile.OfferTitle + " includes the following additions to your trip:";
                    //objDetail.OfferWarningMessage = "A Traveler has already selected an Economy Plus@ seat for one or more flights on this reservation.If you purchase this bundle,you 'll be able to select Economy Plus for all travelers on the selected flights below.";
                    objDetail.OfferWarningMessage = string.Empty;

                    //The below code need to modfied duplicate is there..need to modify.-- prasad.
                    if (bundleResponse.DisplayCart.DisplayTrips.Count > 1)
                    {
                        foreach (var v in bundleResponse.DisplayCart.DisplayTrips)
                        {
                            BundleOfferTrip objmoboffertrips = new BundleOfferTrip();
                            foreach (var f in v.Flights)
                            {
                                bool isProductEligible = false;
                                for (int k = 0; k < f.Products.Count; k++)
                                {
                                    if (f.Products[k].PromoDescription != null)
                                    {
                                        objmoboffertrips = new BundleOfferTrip();
                                        if (f.Products[k].IsBundleProduct == true)
                                        {
                                            if (bundleproducts[i].ProductType == f.Products[k].ProductType)
                                            {
                                                isProductEligible = true;
                                                // Below condition to make price item 0 when there is no price object ruturned for the trip and product type is NonExistingProductPlaceholder
                                                if (f.Products[k].Prices.Count <= 0 && f.Products[k].ProductSubtype == "NonExistingProductPlaceholder")
                                                {
                                                    objmoboffertrips.OriginDestination = f.Products[k].PromoDescription + " | " + "Not available";
                                                    objmoboffertrips.Price = 0;

                                                }
                                                else
                                                {
                                                    objmoboffertrips.OriginDestination = f.Products[k].PromoDescription + " | " + "$" + f.Products[k].Prices[0].Amount.ToString("N0") + '/' + "person";
                                                    objmoboffertrips.Price = Convert.ToInt32(f.Products[k].Prices[0].Amount);
                                                }
                                                objmoboffertrips.TripId = f.Products[k].TripIndex.ToString();
                                                objmoboffertrips.TripProductID = f.Products[k].ProductId;
                                                objmoboffertrips.TripProductIDs = f.Products[k].ProductId.Split(',').ToList();
                                                objmoboffertrips.IsChecked = false;
                                                offerTrips.Add(objmoboffertrips);
                                            }

                                        }

                                    }

                                }
                                if (!isProductEligible)
                                {
                                    objmoboffertrips.TripId = f.TripIndex.ToString();
                                    objmoboffertrips.TripProductID = bundleproducts[i].ProductId;
                                    objmoboffertrips.TripProductIDs = bundleproducts[i].ProductId.Split(',').ToList();
                                    objmoboffertrips.IsChecked = false;
                                    objmoboffertrips.OriginDestination = (v.Origin + " - " + v.Destination) + " | " + "Not available";
                                    objmoboffertrips.Price = 0;
                                    offerTrips.Add(objmoboffertrips);
                                }



                            }
                        }
                    }
                    else
                    {

                        foreach (var v in bundleResponse.DisplayCart.DisplayTrips)
                        {
                            BundleOfferTrip objmoboffertrips = new BundleOfferTrip();
                            foreach (var f in v.Flights)
                            {
                                foreach (var p in f.Products)
                                {
                                    if (p.IsBundleProduct == true)
                                    {
                                        if (p.Prices.Count >= 1)
                                        {
                                            objmoboffertrips.OriginDestination = p.PromoDescription + " | " + "$" + bundleproducts[i].Prices[0].Amount.ToString("N0") + '/' + "person";
                                            objmoboffertrips.Price = Convert.ToInt32(bundleproducts[i].Prices[0].Amount);
                                        }
                                        objmoboffertrips.TripId = p.TripIndex.ToString();
                                        // objmoboffertrips.TripProductID = f.Products[i].ProductId;
                                        objmoboffertrips.TripProductID = bundleproducts[i].ProductId;
                                        objmoboffertrips.TripProductIDs = bundleproducts[i].ProductId.Split(',').ToList();
                                        objmoboffertrips.IsChecked = false;
                                    }
                                }
                                offerTrips.Add(objmoboffertrips);
                            }

                        }
                    }
                    objbundleproduct[i].Tile = objTile;
                    objDetail.OfferDetails = objOfferDetails;
                    objbundleproduct[i].Detail = objDetail;
                    objbundleproduct[i].Detail.OfferTrips = offerTrips;
                }
                objtermsandconditions.Title = "Terms and Conditions";
                objtermsandconditions.LocationCode = "";
                objtermsandconditions.ContentShort = "";
                objtermsandconditions.HeadLine = "Travel Options bundle terms and conditions";
                objtermsandconditions.ContentFull = await GetBundleTermsandConditons("bundlesTermsandConditons", session.SessionId);
                string strterms = objtermsandconditions.ContentFull;
                objtermsandconditions.ContentFull = strterms.Replace('?', '℠');
                //}
                #endregion

                response.Products = objbundleproduct;
                response.TermsAndCondition = objtermsandconditions;
                #endregion
            }

            return response;
        }

        private async Task<BookingBundlesResponse> GetBundleDetails_CFOP(BookingBundlesRequest bundleRequest)
        {
            BookingBundlesResponse response = new BookingBundlesResponse(_configuration);

            var request = await _sessionHelperService.GetSession<ShopSelectRequest>(bundleRequest.SessionId, typeof(ShopSelectRequest).FullName, new List<string>() { bundleRequest.SessionId, typeof(ShopSelectRequest).FullName });
            //TODO Sathwika
            if (request != null)
            {
                request.ScreenSize = bundleRequest.ScreenSize;
                request.DeviceType = (bundleRequest.Application.Id == 1) ? "iOS" : "Android";
                request.MileagePlusNumber = bundleRequest.MPNumber;
                request.BundleProductCode = United.Services.FlightShopping.Common.Bundles.StaticModel.BundlesCode.SBE;
            }

            DisplayCartRequest displayCartRequest = new DisplayCartRequest();
            displayCartRequest.LangCode = bundleRequest.LanguageCode;
            displayCartRequest.CartId = bundleRequest.CartId;
            displayCartRequest.CountryCode = "US";
            displayCartRequest.CartKey = null;
            displayCartRequest.ProductCodes = new List<string> { United.Services.FlightShopping.Common.Bundles.StaticModel.BundlesCode.SBE.ToString() };

            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(bundleRequest.SessionId, session.ObjectName, new List<string>() { bundleRequest.SessionId, session.ObjectName });
            if (session == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }
            string logAction = session.IsReshopChange ? "ReShopGetBundleDetails" : "PopulateBundles";
            string jsonRequest = session.IsReshopChange ? JsonConvert.SerializeObject(request) : JsonConvert.SerializeObject(displayCartRequest);

            var responseBundle = await _flightShoppingService.GetBundles<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(session.Token, session.SessionId, logAction, jsonRequest);

            if (responseBundle != null)
            {
                List<BundleProduct> objbundleproduct = new List<BundleProduct>();
                var objtermsandconditions = new MOBMobileCMSContentMessages();
                BundleTile objTile;
                BundleDetail objDetail;
                List<BundleOfferDetail> objOfferDetails;
                List<BundleOfferTrip> offerTrips;
                List<string> offerDescription;
                List<string> bundleProductCodes;
                bool isAdvanceSearchCouponApplied = _seatMapCSL30.EnableAdvanceSearchCouponBooking(bundleRequest.Application.Id, bundleRequest.Application.Version.Major);


                #region Bundles

                var bundleproductsAll = responseBundle.DisplayCart.DisplayTrips.Where(t => t != null && t.Flights != null)
                    .SelectMany(t => t.Flights).Where(f => f != null && f.Products != null).SelectMany(f => f.Products).
                    Where(p => p != null && p.IsBundleProduct == true && p.ProductId != "").ToList();
                var bundleproducts = new List<Product>();
                foreach (var prod in bundleproductsAll)
                {
                    if (bundleproducts.Count < 4 && !bundleproducts.Exists(p => p.ProductType == prod.ProductType))
                    {
                        bundleproducts.Add(prod);
                    }
                }
                #region BundleBinding
                for (int i = 0; i < bundleproducts.Count; i++)
                {
                    //MOBBundleOfferTrip objmoboffertrips = new MOBBundleOfferTrip();
                    if (bundleproducts[i].IsBundleProduct == true)
                    {
                        objTile = new BundleTile();
                        objDetail = new BundleDetail();

                        objbundleproduct.Add(new BundleProduct { ProductID = bundleproducts[i].ProductId, ProductIDs = bundleproducts[i].ProductId.Split(',').ToList(), ProductCode = bundleproducts[i].ProductType, ProductIndex = i });
                        offerTrips = new List<BundleOfferTrip>();
                        offerDescription = new List<string>();
                        objOfferDetails = new List<BundleOfferDetail>();
                        bundleProductCodes = new List<string>();

                        if (bundleproducts[i].SubProducts != null)
                        {
                            BundleOfferDetail objmobofferdetails = new BundleOfferDetail();
                            foreach (var v in bundleproducts[i].SubProducts)
                            {
                                objmobofferdetails = new BundleOfferDetail();
                                v.Description = MakeFirstlettercapital(v.Description.Replace("Club&#8480", "Club℠"));
                                objmobofferdetails.OfferDetailHeader = v.Description;
                                //v.AlternateDescription = v.AlternateDescription.Replace("Club&#8480", "Club℠");
                                string detailDescription = null;
                                if (_configuration.GetValue<bool>("BundlSubProductDeatilDesc_FromCSL"))
                                {
                                    if (!string.IsNullOrWhiteSpace(v.SubDescription))
                                    {
                                        detailDescription = PutFullStopOnEndOfString(MakeFirstlettercapital(v.SubDescription.Trim()));
                                    }
                                }
                                else
                                {
                                    detailDescription = BundleSubProductDeailDescription(v.Code);
                                }

                                objmobofferdetails.OfferDetailDescription = string.IsNullOrEmpty(detailDescription) ? PutFullStopOnEndOfString(MakeFirstlettercapital(string.IsNullOrEmpty(v.AlternateDescription) ? "" : v.AlternateDescription.Replace("Club&#8480", "Club℠"))) : detailDescription;
                                //if (v.ReferencedSegments != null)
                                //{
                                //    if (v.ReferencedSegments[0].IsEligible == false)
                                //    {
                                objmobofferdetails.OfferDetailWarningMessage = GetOfferDetailWarningMessage(responseBundle.DisplayCart.DisplayTrips, bundleproducts[i].ProductType, objmobofferdetails, v.EddCode);

                                //        objmobofferdetails.OfferDetailWarningMessage = "Not available for" + v.ReferencedSegments[0].Origin + '-' + v.ReferencedSegments[0].Destination;
                                //    }
                                //}
                                offerDescription.Add(v.Description);
                                objOfferDetails.Add(objmobofferdetails);
                                if (isAdvanceSearchCouponApplied)
                                    bundleProductCodes.Add(v.Code);
                            }
                        }
                        objTile.IsSelected = false;
                        int j = i + 1;
                        objTile.OfferTitle = "Bundle offer " + j;
                        objTile.OfferDescription = offerDescription;
                        if (bundleproducts[i].Prices.Count >= 1)
                        {
                            objTile.PriceText = "+$" + bundleproducts[i].Prices[0].Amount.ToString() + "/person";
                            objTile.OfferPrice = "$" + bundleproducts[i].Prices[0].Amount.ToString();
                        }
                        objTile.TileIndex = i;

                        objDetail.OfferTitle = objTile.OfferTitle + " includes the following additions to your trip:";
                        //objDetail.OfferWarningMessage = "A Traveler has already selected an Economy Plus@ seat for one or more flights on this reservation.If you purchase this bundle,you 'll be able to select Economy Plus for all travelers on the selected flights below.";
                        objDetail.OfferWarningMessage = string.Empty;

                        //The below code need to modfied duplicate is there..need to modify.-- prasad.
                        if (responseBundle.DisplayCart.DisplayTrips.Count > 1)
                        {
                            foreach (var v in responseBundle.DisplayCart.DisplayTrips)
                            {
                                BundleOfferTrip objmoboffertrips = new BundleOfferTrip();
                                foreach (var f in v.Flights)
                                {
                                    bool isProductEligible = false;
                                    for (int k = 0; k < f.Products.Count; k++)
                                    {
                                        if (f.Products[k].PromoDescription != null)
                                        {
                                            objmoboffertrips = new BundleOfferTrip();
                                            if (f.Products[k].IsBundleProduct == true)
                                            {
                                                if (bundleproducts[i].ProductType == f.Products[k].ProductType)
                                                {
                                                    isProductEligible = true;
                                                    // Below condition to make price item 0 when there is no price object ruturned for the trip and product type is NonExistingProductPlaceholder
                                                    if (f.Products[k].Prices.Count <= 0 && f.Products[k].ProductSubtype == "NonExistingProductPlaceholder")
                                                    {
                                                        objmoboffertrips.OriginDestination = f.Products[k].PromoDescription + " | " + "Not available";
                                                        objmoboffertrips.Price = 0;
                                                    }
                                                    else
                                                    {
                                                        objmoboffertrips.OriginDestination = f.Products[k].PromoDescription + " | " + "$" + f.Products[k].Prices[0].Amount + '/' + "person";
                                                        objmoboffertrips.Price = Convert.ToInt32(f.Products[k].Prices[0].Amount);
                                                    }
                                                    objmoboffertrips.TripId = f.Products[k].TripIndex.ToString();
                                                    objmoboffertrips.TripProductID = f.Products[k].ProductId;
                                                    objmoboffertrips.TripProductIDs = f.Products[k].ProductId.Split(',').ToList();
                                                    objmoboffertrips.IsChecked = false;
                                                    offerTrips.Add(objmoboffertrips);
                                                }
                                            }
                                        }
                                    }
                                    if (!isProductEligible)
                                    {
                                        objmoboffertrips.TripId = f.TripIndex.ToString();
                                        objmoboffertrips.TripProductID = bundleproducts[i].ProductId;
                                        objmoboffertrips.TripProductIDs = bundleproducts[i].ProductId.Split(',').ToList();
                                        objmoboffertrips.IsChecked = false;
                                        objmoboffertrips.OriginDestination = (v.Origin + " - " + v.Destination) + " | " + "Not available";
                                        objmoboffertrips.Price = 0;
                                        offerTrips.Add(objmoboffertrips);
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var v in responseBundle.DisplayCart.DisplayTrips)
                            {
                                BundleOfferTrip objmoboffertrips = new BundleOfferTrip();
                                foreach (var f in v.Flights)
                                {
                                    foreach (var p in f.Products)
                                    {
                                        if (p.IsBundleProduct == true)
                                        {
                                            if (p.Prices.Count >= 1)
                                            {
                                                objmoboffertrips.OriginDestination = p.PromoDescription + " | " + "$" + bundleproducts[i].Prices[0].Amount + '/' + "person";
                                                objmoboffertrips.Price = Convert.ToInt32(bundleproducts[i].Prices[0].Amount);
                                            }
                                            objmoboffertrips.TripId = p.TripIndex.ToString();
                                            // objmoboffertrips.TripProductID = f.Products[i].ProductId;
                                            objmoboffertrips.TripProductID = bundleproducts[i].ProductId;
                                            objmoboffertrips.TripProductIDs = bundleproducts[i].ProductId.Split(',').ToList();
                                            objmoboffertrips.IsChecked = false;
                                        }
                                    }
                                    offerTrips.Add(objmoboffertrips);
                                }
                            }
                        }
                        objbundleproduct[i].Tile = objTile;
                        objDetail.OfferDetails = objOfferDetails;
                        objbundleproduct[i].Detail = objDetail;
                        objbundleproduct[i].Detail.OfferTrips = offerTrips;
                        objbundleproduct[i].BundleProductCodes = bundleProductCodes;
                    }
                    objtermsandconditions.Title = "Terms and Conditions";
                    objtermsandconditions.LocationCode = "";
                    objtermsandconditions.ContentShort = "";
                    objtermsandconditions.HeadLine = "Travel Options bundle terms and conditions";
                    objtermsandconditions.ContentFull = await GetBundleTermsandConditons("bundlesTermsandConditons", session.SessionId);
                    string strterms = objtermsandconditions.ContentFull;
                    objtermsandconditions.ContentFull = strterms.Replace('?', '℠');
                }
                #endregion

                response.Products = objbundleproduct;
                response.TermsAndCondition = objtermsandconditions;
                #endregion

            }
            return response;
        }

        private void PopulateEconomyPlusWarningMessage(Reservation persistedReservation, BookingBundlesResponse bundleResponse)
        {
            // Below code is for show the validation message if user select the E+ seat prior to E+ Bundle
            if (persistedReservation != null &&
              persistedReservation.TravelersCSL != null &&
              persistedReservation.TravelersCSL.Any() &&
              persistedReservation.TravelersCSL.Values.Any(t => t.Seats != null && t.Seats.Any(s => s.Price > 0 && !_shoppingUtility.IsEMinusSeat(s.ProgramCode))))
            {
                foreach (var bundleProduct in bundleResponse.Products)
                {
                    if (bundleProduct.Tile != null &&
                        bundleProduct.Tile.OfferDescription != null &&
                        bundleProduct.Tile.OfferDescription.Any(d => d != null && d.ToUpper().Contains("ECONOMY PLUS")))
                    {
                        bundleProduct.Detail.OfferWarningMessage = "A Traveler has already selected an Economy Plus® seat for one or more flights on this reservation. If you purchase this bundle, you'll be able to select Economy Plus for all travelers on the selected flights below.";
                    }
                }
            }
        }

        private string MakeFirstlettercapital(string Descriptionstr)
        {
            if (!string.IsNullOrEmpty(Descriptionstr))
            {
                Descriptionstr = Descriptionstr.Trim();
                Descriptionstr = char.ToUpper(Descriptionstr[0]) + Descriptionstr.Substring(1);
            }
            return Descriptionstr;
        }

        private string PutFullStopOnEndOfString(string Descriptionstr)
        {
            if (!string.IsNullOrEmpty(Descriptionstr))
            {
                Descriptionstr = Descriptionstr.Trim();
                if (Descriptionstr.LastIndexOf('.') == -1 || Descriptionstr.LastIndexOf('.') != Descriptionstr.Length - 1)
                {
                    Descriptionstr = Descriptionstr + '.';
                }
            }
            return Descriptionstr;
        }

        private string BundleSubProductDeailDescription(string BundleSubProductCode)
        {
            string BundleSubProductDeailDescriptionstr = null;
            if (!string.IsNullOrEmpty(BundleSubProductCode))
            {
                Dictionary<string, string> DicSubproductCodeDesc = new Dictionary<string, string>();
                DicSubproductCodeDesc.Add("EPU", "Choose any available Economy Plus seat.");
                DicSubproductCodeDesc.Add("EXB", "Travel with an extra standard checked bag.");
                DicSubproductCodeDesc.Add("PAS", "Enjoy dedicated check-in, exclusive security lanes (where available) and priority boarding.");
                DicSubproductCodeDesc.Add("CTP", "Access available club locations throughout your trip.");
                DicSubproductCodeDesc.Add("BMI", "Earn extra award miles for this trip.");
                DicSubproductCodeDesc.TryGetValue(BundleSubProductCode.ToUpper().Trim(), out BundleSubProductDeailDescriptionstr);
            }
            return BundleSubProductDeailDescriptionstr;
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

        private static string GetOfferDetailWarningMessage(List<DisplayTrip> displayTrips, string productType, BundleOfferDetail objmobofferdetails, string eddCode)
        {

            string detailWarningMessages = string.Empty;
            try
            {
                var objects = displayTrips.Where(t => t.Flights != null && t.Flights.Exists(f => f.Products != null && f.Products.Exists(p => p.ProductType == productType &&
                                                                                                      p.SubProducts != null && p.SubProducts.Exists(sp => sp.EddCode == eddCode)
                                                                                                      )));
                if (objects != null)
                {
                    List<string> airportCodes = new List<string>();
                    foreach (var notExistEddType in objects)
                    {
                        if (notExistEddType.Flights != null)
                        {
                            foreach (var fl in notExistEddType.Flights)
                            {
                                if (fl.Products != null)
                                {
                                    foreach (var pr in fl.Products.Where(ilProds => ilProds.ProductType == productType))
                                    {
                                        if (pr.SubProducts != null)
                                        {
                                            foreach (var sr in pr.SubProducts.Where(subProds => subProds.EddCode == eddCode))
                                            {

                                                if (sr.ReferencedSegments != null && sr.ReferencedSegments.Count > 0)
                                                {
                                                    foreach (var rs in sr.ReferencedSegments.Where(refseg => !refseg.IsEligible))
                                                        detailWarningMessages += "Not available for " + rs.Origin + '-' + rs.Destination + "\n";
                                                }

                                                if (sr.ReferencedAirports != null && sr.ReferencedAirports.Count > 0)
                                                {
                                                    foreach (var ra in sr.ReferencedAirports.Where(refseg => !refseg.IsEligible))
                                                    {
                                                        if (!airportCodes.Exists(p => p == ra.Airport))
                                                        {
                                                            detailWarningMessages += "Not available at " + ra.Airport + "\n";
                                                            airportCodes.Add(ra.Airport);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            if (!string.IsNullOrEmpty(detailWarningMessages))
            {
                //detailWarningMessages = detailWarningMessages.Trim(char[]{ '\',\n'});
                detailWarningMessages = detailWarningMessages.Substring(0, detailWarningMessages.Length - 1);
            }

            return detailWarningMessages;
        }

    }
}
