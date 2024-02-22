using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper.Shopping;
using United.Definition;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Misc;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.Extensions;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Utility.Helper;
using CouponDetails = United.Mobile.Model.Shopping.CouponDetails;
using FlowType = United.Utility.Enum.FlowType;
using MOBBKTraveler = United.Mobile.Model.Shopping.Booking.MOBBKTraveler;
using MOBItem = United.Mobile.Model.Common.MOBItem;
using MOBMobileCMSContentMessages = United.Mobile.Model.Common.MOBMobileCMSContentMessages;

namespace United.Common.Helper.Merchandize
{
    public class ProductInfoHelper : IProductInfoHelper
    {
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly DocumentLibraryDynamoDB _documentLibraryDynamoDB;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly IHeaders _headers;

        public ProductInfoHelper(IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , IDynamoDBService dynamoDBService
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , IHeaders headers
            )
        {
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _dynamoDBService = dynamoDBService;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _headers = headers;
            _documentLibraryDynamoDB = new DocumentLibraryDynamoDB(_configuration, _dynamoDBService);
        }

        public async Task<List<ProdDetail>> ConfirmationPageProductInfo(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, bool isPost, MOBApplication application, SeatChangeState state = null, string flow = "VIEWRES", string sessionId = "")
        {
            List<ProdDetail> prodDetails = new List<ProdDetail>();
            List<string> productCodes = new List<string>();

            bool isFareLockCompletePurchase = _configuration.GetValue<bool>("EnableFareLockPurchaseViewRes") && flightReservationResponse.Reservation.Characteristic.Any(o => (o.Code != null && o.Value != null && o.Code.Equals("FARELOCK") && o.Value.Equals("TRUE"))) &&
                flightReservationResponse.Reservation.Characteristic.Any(o => (o.Code != null && o.Code.Equals("FARELOCK_DATE")));

            if (isFareLockCompletePurchase)
            {
                var displayTotalPrice = flightReservationResponse.DisplayCart.DisplayPrices.FirstOrDefault(o => (o.Description != null && o.Description.Equals("Total", StringComparison.OrdinalIgnoreCase))).Amount;
                var grandTotal = ShopStaticUtility.GetGrandTotalPriceFareLockShoppingCart(flightReservationResponse);
                var prodDetail = new ProdDetail()
                {
                    Code = "FLK_VIEWRES",
                    ProdDescription = string.Empty,
                    ProdTotalPrice = String.Format("{0:0.00}", grandTotal),
                    ProdDisplayTotalPrice = grandTotal.ToString("c"),
                    Segments = new List<ProductSegmentDetail> {
                                     new ProductSegmentDetail {
                                                        SegmentInfo = "",
                                                        SubSegmentDetails = new List<ProductSubSegmentDetail>
                                                                            {
                                                                                new ProductSubSegmentDetail
                                                                                {
                                                                                    Price = String.Format("{0:0.00}", displayTotalPrice),
                                                                                    DisplayPrice = displayTotalPrice.ToString("c"),
                                                                                    Passenger = ShopStaticUtility.GetFareLockPassengerDescription(flightReservationResponse.Reservation),
                                                                                    SegmentDescription = ShopStaticUtility.GetFareLockSegmentDescription(flightReservationResponse.Reservation)
                                                                                }
                                                        }
                                     }
                    },
                };
                prodDetails.Add(prodDetail);
            }
            else
            {
                productCodes = ShopStaticUtility.GetProductCodes(flightReservationResponse, flow, isPost);
                productCodes = ShopStaticUtility.OrderProducts(productCodes);

                //Added this line to replace the ProductCode for FareLock
                int index = productCodes.FindIndex(ind => ind.Equals("FLK"));
                if (index != -1)
                    productCodes[index] = "FareLock";
                foreach (string productCode in productCodes)
                {
                    ProdDetail prodDetail;
                    United.Service.Presentation.InteractionModel.ShoppingCart flightReservationResponseShoppingCart;

                    switch (productCode?.ToUpper()?.Trim())
                    {
                        case "SEATASSIGNMENTS":
                            if (flow == FlowType.BOOKING.ToString())
                            {
                                prodDetail = BuildProdDetailsForSeats(flightReservationResponse, state, flow, application);
                            }
                            else
                            {
                                prodDetail = BuildProdDetailsForSeats(flightReservationResponse, state, isPost, flow);
                            }

                            if (prodDetail != null && ((!string.IsNullOrEmpty(prodDetail.ProdDisplayTotalPrice) || !string.IsNullOrEmpty(prodDetail.ProdDisplayOtherPrice)) || IsFreeSeatCouponApplied(prodDetail, flightReservationResponse)))
                            {
                                prodDetails.Add(prodDetail);
                            }
                            break;
                        case "RES":
                            flightReservationResponseShoppingCart = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart : flightReservationResponse.ShoppingCart;
                            prodDetail = new ProdDetail()
                            {
                                Code = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Code).FirstOrDefault().ToString(),
                                ProdDescription = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Description).FirstOrDefault().ToString(),
                                ProdTotalPrice = String.Format("{0:0.00}", ShopStaticUtility.GetTotalPriceForRESProduct(isPost, flightReservationResponseShoppingCart, flow)),
                                ProdDisplayTotalPrice = Decimal.Parse(ShopStaticUtility.GetTotalPriceForRESProduct(isPost, flightReservationResponseShoppingCart, flow).ToString()).ToString("c")
                            };
                            prodDetails.Add(prodDetail);
                            break;
                        case "RBF":
                            flightReservationResponseShoppingCart = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart : flightReservationResponse.ShoppingCart;
                            prodDetail = new ProdDetail()
                            {
                                Code = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Code).FirstOrDefault().ToString(),
                                ProdDescription = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Description).FirstOrDefault().ToString(),
                                ProdTotalPrice = String.Format("{0:0.00}", ShopStaticUtility.GetCloseBookingFee(isPost, flightReservationResponseShoppingCart, flow)),
                                ProdDisplayTotalPrice = Decimal.Parse(ShopStaticUtility.GetCloseBookingFee(isPost, flightReservationResponseShoppingCart, flow).ToString()).ToString("c")
                            };
                            prodDetails.Add(prodDetail);
                            break;
                        case "BAG":
                            if (_configuration.GetValue<bool>("EnableCouponMVP2Changes") && flow == FlowType.BOOKING.ToString())//SC is adding a bag product if free bag coupon is applied..Adding default values
                            {
                                prodDetail = new ProdDetail()
                                {
                                    Code = "BAG",
                                    ProdDescription = string.Empty,
                                    ProdTotalPrice = "0",
                                    ProdDisplayTotalPrice = "0"
                                };
                                prodDetails.Add(prodDetail);
                            }
                            break;
                        case "POM":
                            prodDetails = await BuildProductDetailsForInflightMeals(flightReservationResponse, productCode, sessionId, isPost);
                            break;
                        default:

                            List<string> refundedSegmentNums = null;
                            var travelOptions = ShopStaticUtility.GetTravelOptionItems(flightReservationResponse, productCode);
                            bool isBundleProduct = string.Equals(travelOptions?.FirstOrDefault(t => t.Key == productCode)?.Type, "BE", StringComparison.OrdinalIgnoreCase);
                            if (travelOptions == null || !travelOptions.Any())
                                continue;

                            //ModifyReservationFailed
                            if (flightReservationResponse?.Errors?.Any(e => e?.MinorCode == "90506") ?? false)
                            {
                                bool DisableFixForPCUPurchaseFailMsg_MOBILE15837 = _configuration.GetValue<bool>("DisableFixForPCUPurchaseFailMsg_MOBILE15837");
                                if (!ShopStaticUtility.IsRefundSuccess(flightReservationResponse.CheckoutResponse.ShoppingCartResponse.Items, out refundedSegmentNums, DisableFixForPCUPurchaseFailMsg_MOBILE15837))
                                {
                                    continue;
                                }
                            }

                            prodDetail = new ProdDetail()
                            {
                                Code = travelOptions.Where(d => d.Key == productCode).Select(d => d.Key).FirstOrDefault().ToString(),
                                ProdDescription = productCode == "TPI" && _configuration.GetValue<bool>("GetTPIProductName_HardCode") ? "Travel insurance" : travelOptions.Where(d => d.Key == productCode).Select(d => d.Type).FirstOrDefault().ToString(),
                                ProdTotalPrice = String.Format("{0:0.00}", travelOptions.Sum(d => d.Amount)),
                                ProdOriginalPrice = String.Format("{0:0.00}", travelOptions.Select(d => d.OriginalPrice).Sum()),
                                ProdDisplayTotalPrice = Decimal.Parse(travelOptions.Sum(d => d.Amount).ToString()).ToString("c"),
                                Segments = travelOptions
                                .Where(d => d.Key == productCode)
                                .SelectMany(x => x.SubItems)
                                .Where(x => ShopStaticUtility.ShouldIgnoreAmount(x) ? true : x.Amount != 0 || (!_configuration.GetValue<bool>("DisableFreeCouponFix") && x.OriginalPrice != 0))
                                .OrderBy(x => x.SegmentNumber)
                                .GroupBy(x => x.SegmentNumber).ToList()
                                .Select(x => new ProductSegmentDetail
                                {
                                    SegmentInfo = BuildSegmentInfo(productCode, flightReservationResponse.Reservation.FlightSegments, x),
                                    ProductId = string.Join(",", x.Select(u => u.Value).ToList()),
                                    TripId = string.Join(",", x.Select(u => u.TripIndex).ToList()),
                                    SegmentId = string.Join(",", x.Select(u => u.SegmentNumber).Distinct().ToList()),
                                    ProductIds = x.Select(u => u.Key).ToList(),
                                    SubSegmentDetails = x.GroupBy(f => f.SegmentNumber).Select(t => new ProductSubSegmentDetail
                                    {
                                        SegmentInfo = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) && productCode != "TPI" ? BuildSegmentInfo(productCode, flightReservationResponse.Reservation.FlightSegments, x) : string.Empty,
                                        Price = String.Format("{0:0.00}", t.Sum(i => i.Amount)),
                                        DisplayPrice = Decimal.Parse(t.Sum(i => i.Amount).ToString()).ToString("c"),
                                        OrginalPrice = EnablePromoCodeForAncillaryProductsManageRes() ? String.Format("{0:0.00}", t.Select(i => i.OriginalPrice).Sum()) : string.Empty,
                                        DisplayOriginalPrice = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? ShopStaticUtility.GetDisplayOriginalPrice(t.Select(i => i.Amount).Sum(), t.Select(i => i.OriginalPrice).Sum()) : string.Empty,
                                        Passenger = x.Count().ToString() + (x.Count() > 1 ? " Travelers" : " Traveler"),
                                        SegmentDescription = BuildProductDescription(travelOptions, t, productCode),
                                        IsPurchaseFailure = ShopStaticUtility.IsPurchaseFailed(productCode == "PCU", t.Select(sb => sb.SegmentNumber).FirstOrDefault(), refundedSegmentNums),
                                        ProdDetailDescription =  IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? (GetProductDetailDescrption(t, productCode, sessionId, isBundleProduct).Result) : null,
                                        ProductDescription = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? ShopStaticUtility.GetProductDescription(travelOptions, productCode) : string.Empty
                                    }).ToList()
                                }).ToList(),
                                ProdTotalMiles = 0,
                                ProdDisplayTotalMiles = string.Empty
                            };
                            // MOBILE-25395: SAF
                            // Fix the product descriptions and tripID for SAF bundle
                            var safCode = _configuration.GetValue<string>("SAFCode");
                            if (productCode?.ToUpper()?.Trim() == safCode)
                            {
                                prodDetail?.Segments?.ForEach(segment =>
                                {
                                    segment.SubSegmentDetails?.ForEach(subSegment => subSegment.ProductDescription = subSegment.ProdDetailDescription?.FirstOrDefault());
                                    var safProdDetail = flightReservationResponse.ShoppingCart?.Items?.FirstOrDefault(item => item.Product?.Any(product => string.Equals(product.Code, safCode, StringComparison.OrdinalIgnoreCase)) ?? false);
                                    if (safProdDetail != null)
                                    {
                                        segment.TripId = string.Join(",", safProdDetail.Product?.FirstOrDefault(product => string.Equals(product.Code, safCode, StringComparison.OrdinalIgnoreCase))?.SubProducts?.Select(sp => sp.ID)?.ToList());
                                    }
                                });
                            }
                            if (!ShopStaticUtility.IsCheckinFlow(flow))
                            {
                                if (productCode != "FareLock")
                                    ShopStaticUtility.UpdateRefundTotal(prodDetail);
                                else
                                    prodDetail.Code = "FLK";
                            }
                            if (prodDetail != null && (!string.IsNullOrEmpty(prodDetail.ProdDisplayTotalPrice) || !string.IsNullOrEmpty(prodDetail.ProdDisplayOtherPrice) || IsOriginalPriceExists(prodDetail)))
                            {
                                prodDetails.Add(prodDetail);
                            }
                            break;
                    }
                }
                if ((_configuration.GetValue<bool>("EnableCouponsforBooking") && flow == FlowType.BOOKING.ToString() && prodDetails != null)
                      || (_configuration.GetValue<bool>("EnableCouponsInPostBooking") && flow == FlowType.POSTBOOKING.ToString()) || (_configuration.GetValue<bool>("IsEnableManageResCoupon") && (flow == FlowType.VIEWRES.ToString() || flow == FlowType.VIEWRES_SEATMAP.ToString())))
                {
                    AddCouponDetails(prodDetails, flightReservationResponse, isPost, flow, application);
                }

            }
            return prodDetails;
        }

        public async Task<List<string>> GetProductDetailDescrption(IGrouping<String, SubItem> subItem, string productCode, String sessionId, bool isBundleProduct)
        {
            List<string> prodDetailDescription = new List<string>();
            // MOBILE-25395: SAF
            var safCode = _configuration.GetValue<string>("SAFCode");

            if (string.Equals(productCode, "EFS", StringComparison.OrdinalIgnoreCase))
            {
                prodDetailDescription.Add("Included with your fare");
            }

            // MOBILE-25395: SAF
            if ((isBundleProduct || string.Equals(productCode, safCode, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrEmpty(sessionId))
            {
                var bundleResponse = new MOBBookingBundlesResponse(_configuration);
                bundleResponse = await _sessionHelperService.GetSession<MOBBookingBundlesResponse>(sessionId, bundleResponse.ObjectName, new List<string> { sessionId, bundleResponse.ObjectName }).ConfigureAwait(false);
                if (bundleResponse != null)
                {
                    var selectedBundleResponse = bundleResponse.Products?.FirstOrDefault(p => string.Equals(p.ProductCode, productCode, StringComparison.OrdinalIgnoreCase));
                    if (selectedBundleResponse != null)
                    {
                        // MOBILE-25395: SAF
                        if (string.Equals(productCode, safCode, StringComparison.OrdinalIgnoreCase))
                        {
                            prodDetailDescription.Add(selectedBundleResponse.ProductName);
                        }
                        else
                        {
                            prodDetailDescription.AddRange(selectedBundleResponse.Tile.OfferDescription);
                        }
                    }
                }
            }
            return prodDetailDescription;
        }

        public bool EnablePromoCodeForAncillaryProductsManageRes()
        {
            return _configuration.GetValue<bool>("EnablePromoCodeForAncillaryOffersManageRes");
        }

        private bool IsFreeSeatCouponApplied(ProdDetail prodDetail, FlightReservationResponse flightReservationResponse)
        {
            return _configuration.GetValue<bool>("IsEnableManageResCoupon") && isAFSCouponApplied(flightReservationResponse?.DisplayCart) && prodDetail?.Segments != null && prodDetail.Segments.Any(x => x != null && IsCouponApplied(x));
        }

        private bool isAFSCouponApplied(DisplayCart displayCart)
        {
            if (displayCart != null && displayCart.SpecialPricingInfo != null && displayCart.SpecialPricingInfo.MerchOfferCoupon != null && !string.IsNullOrEmpty(displayCart.SpecialPricingInfo.MerchOfferCoupon.PromoCode) && displayCart.SpecialPricingInfo.MerchOfferCoupon.IsCouponEligible.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private bool IsCouponApplied(ProductSegmentDetail segmentDetail)
        {
            return segmentDetail?.SubSegmentDetails != null ? segmentDetail.SubSegmentDetails.Any(x => x != null && !string.IsNullOrEmpty(x.OrginalPrice) && Decimal.Parse(x.OrginalPrice) > 0) : false;
        }

        //public List<ProdDetail> MSCConfirmationPageProductInfo(FlightReservationResponse flightReservationResponse, bool isPost, bool isError, string flow, MOBApplication application, SeatChangeState state = null, bool isRefundSuccess = false, bool isPartialSuccess = false, List<Mobile.Model.Shopping.SCProductContext> unSuccessfulSegmentDetails = null, List<string> refundedSegmentNums = null, bool isGetCartInformation = false)
        //{
        //    List<ProdDetail> prodDetails = new List<ProdDetail>();
        //    List<string> productCodes = new List<string>();

        //    bool isFareLockCompletePurchase = _configuration.GetValue<bool>("EnableFareLockPurchaseViewRes") && flightReservationResponse.Reservation?.Characteristic != null && flightReservationResponse.Reservation.Characteristic.Any(o => (o.Code != null && o.Value != null && o.Code.Equals("FARELOCK") && o.Value.Equals("TRUE"))) &&
        //        flightReservationResponse.Reservation.Characteristic.Any(o => (o.Code != null && o.Code.Equals("FARELOCK_DATE")));

        //    if (isFareLockCompletePurchase)
        //    {
        //        var displayTotalPrice = flightReservationResponse.DisplayCart.DisplayPrices.FirstOrDefault(o => (o.Description != null && o.Description.Equals("Total", StringComparison.OrdinalIgnoreCase))).Amount;
        //        var grandTotal = ShopStaticUtility.GetGrandTotalPriceFareLockShoppingCart(flightReservationResponse);
        //        var prodDetail = new ProdDetail()
        //        {
        //            Code = "FLK_VIEWRES",
        //            ProdDescription = string.Empty,
        //            ProdTotalPrice = String.Format("{0:0.00}", grandTotal),
        //            ProdDisplayTotalPrice = grandTotal.ToString("c"),
        //            Segments = new List<ProductSegmentDetail> {
        //                         new ProductSegmentDetail {
        //                                            SegmentInfo = "",
        //                                            SubSegmentDetails = new List<ProductSubSegmentDetail>
        //                                            {
        //                                                                    new ProductSubSegmentDetail
        //                                                                    {
        //                                                                        Price = String.Format("{0:0.00}", displayTotalPrice),
        //                                                                        DisplayPrice = displayTotalPrice.ToString("c"),
        //                                                                        Passenger = ShopStaticUtility.GetFareLockPassengerDescription(flightReservationResponse.Reservation),
        //                                                                        SegmentDescription = ShopStaticUtility.GetFareLockSegmentDescription(flightReservationResponse.Reservation)
        //                                                                    }
        //                                            }
        //                         }
        //            },
        //        };
        //        prodDetails.Add(prodDetail);
        //    }
        //    else
        //    {
        //        //United.Service.Presentation.InteractionModel.ShoppingCart flightReservationResponseShoppingCart = isPost ? (United.Service.Presentation.InteractionModel.ShoppingCart)flightReservationResponse.CheckoutResponse.ShoppingCartResponse : flightReservationResponse.ShoppingCart;

        //        productCodes = GetProductCodes(flightReservationResponse, flow, isPost, isGetCartInformation);
        //        productCodes = ShopStaticUtility.OrderProducts(productCodes);
        //        //Added this line to replace the ProductCode for FareLock
        //        int index = productCodes.FindIndex(ind => ind.Equals("FLK"));
        //        if (index != -1)
        //            productCodes[index] = "FareLock";
        //        foreach (string productCode in productCodes)
        //        {

        //            if (productCode == "SEATASSIGNMENTS")
        //            {
        //                ProdDetail prodDetail = BuildProdDetailsForSeats(flightReservationResponse, state, flow, application);
        //                if (prodDetail != null && (!string.IsNullOrEmpty(prodDetail.ProdDisplayTotalPrice) || !string.IsNullOrEmpty(prodDetail.ProdDisplayOtherPrice)))
        //                {
        //                    prodDetails.Add(prodDetail);
        //                }
        //            }
        //            else if (productCode == "RES")
        //            {
        //                United.Service.Presentation.InteractionModel.ShoppingCart flightReservationResponseShoppingCart = new United.Service.Presentation.InteractionModel.ShoppingCart();
        //                flightReservationResponseShoppingCart = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart : flightReservationResponse.ShoppingCart;

        //                var prodDetail = new ProdDetail()
        //                {
        //                    Code = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Code).FirstOrDefault().ToString(),
        //                    ProdDescription = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Description).FirstOrDefault().ToString(),
        //                    ProdTotalPrice = String.Format("{0:0.00}", UtilityHelper.GetTotalPriceForRESProduct(isPost, flightReservationResponseShoppingCart, flow)),
        //                    ProdDisplayTotalPrice = Decimal.Parse(UtilityHelper.GetTotalPriceForRESProduct(isPost, flightReservationResponseShoppingCart, flow).ToString()).ToString("c")
        //                };
        //                prodDetails.Add(prodDetail);
        //            }
        //            else if (_configuration.GetValue<bool>("CFOP19HBugFixToggle") && productCode == "RBF")
        //            {
        //                United.Service.Presentation.InteractionModel.ShoppingCart flightReservationResponseShoppingCart = new United.Service.Presentation.InteractionModel.ShoppingCart();
        //                flightReservationResponseShoppingCart = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart : flightReservationResponse.ShoppingCart;

        //                var prodDetail = new ProdDetail()
        //                {
        //                    Code = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Code).FirstOrDefault().ToString(),
        //                    ProdDescription = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Description).FirstOrDefault().ToString(),
        //                    ProdTotalPrice = String.Format("{0:0.00}", UtilityHelper.GetCloseBookingFee(isPost, flightReservationResponseShoppingCart, flow)),
        //                    ProdDisplayTotalPrice = Decimal.Parse(UtilityHelper.GetCloseBookingFee(isPost, flightReservationResponseShoppingCart, flow).ToString()).ToString("c")
        //                };
        //                prodDetails.Add(prodDetail);
        //            }
        //            else if (_configuration.GetValue<bool>("EnablePassCHGProductInReshopFlowToggle") && productCode == "CHG")
        //            {
        //                United.Service.Presentation.InteractionModel.ShoppingCart flightReservationResponseShoppingCart = new United.Service.Presentation.InteractionModel.ShoppingCart();
        //                flightReservationResponseShoppingCart = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart : flightReservationResponse.ShoppingCart;

        //                var prodDetail = new ProdDetail()
        //                {
        //                    Code = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Code).FirstOrDefault().ToString(),
        //                    ProdDescription = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Description).FirstOrDefault().ToString(),
        //                    ProdTotalPrice = String.Format("{0:0.00}", GetProductFee(isPost, flightReservationResponseShoppingCart, flow, productCode)),
        //                    ProdDisplayTotalPrice = Decimal.Parse(GetProductFee(isPost, flightReservationResponseShoppingCart, flow, productCode).ToString()).ToString("c")
        //                };
        //                prodDetails.Add(prodDetail);
        //            }
        //            else if (_configuration.GetValue<bool>("EnableCouponMVP2Changes") && flow == FlowType.BOOKING.ToString() && productCode == "BAG")//SC is adding a bag product if free bag coupon is applies..Adding default values
        //            {
        //                var prodDetail = new ProdDetail()
        //                {
        //                    Code = "BAG",
        //                    ProdDescription = string.Empty,
        //                    ProdTotalPrice = "0",
        //                    ProdDisplayTotalPrice = "0"
        //                };
        //                prodDetails.Add(prodDetail);
        //            }
        //            else if (productCode == "PCU" && isError == true)
        //            {
        //                var chargedSegmentNums = flightReservationResponse.DisplayCart.TravelOptions.SelectMany(x => x.SubItems).Where(x => x.Amount != 0).Select(x => x.SegmentNumber).ToList().Except(refundedSegmentNums).ToList();

        //                PremiumCabinUpgrade premiumCabinUpgrade = new PremiumCabinUpgrade(_sessionHelperService, _configuration, null, _dynamoDBService, _documentLibraryDynamoDB, null, null, null, _legalDocumentsForTitlesService);
        //                var prodDetail = new ProdDetail()
        //                {
        //                    Code = flightReservationResponse.DisplayCart.TravelOptions.Where(d => d.Key == productCode).Select(d => d.Key).FirstOrDefault().ToString(),
        //                    ProdDescription = flightReservationResponse.DisplayCart.TravelOptions.Where(d => d.Key == productCode).Select(d => d.Type).FirstOrDefault().ToString(),
        //                    ProdTotalPrice = String.Format("{0:0.00}", flightReservationResponse.DisplayCart.TravelOptions.Where(d => d.Key == productCode).Select(d => d.Amount).Sum()),
        //                    ProdDisplayTotalPrice = Decimal.Parse(flightReservationResponse.DisplayCart.TravelOptions.Where(d => d.Key == productCode).Select(d => d.Amount).Sum().ToString()).ToString("c"),
        //                    Segments = (isPartialSuccess == true) ? flightReservationResponse.DisplayCart.TravelOptions.Where(d => d.Key == productCode).SelectMany(x => x.SubItems).Where(x => x.Amount != 0).OrderBy(x => x.SegmentNumber).GroupBy(x => x.SegmentNumber).ToList().Select(x => new ProductSegmentDetail
        //                    {
        //                        SegmentInfo = BuildSegmentInfo(productCode, flightReservationResponse.Reservation.FlightSegments, x),
        //                        ProductId = string.Join(",", x.Select(u => u.Key).ToList()),
        //                        TripId = string.Join(",", x.Select(u => u.TripIndex).ToList()),
        //                        SubSegmentDetails = x.GroupBy(f => f.SegmentNumber).Select(t => new ProductSubSegmentDetail
        //                        {
        //                            Price = String.Format("{0:0.00}", t.Select(i => i.Amount).Sum()),
        //                            DisplayPrice = Decimal.Parse(t.Select(i => i.Amount).Sum().ToString()).ToString("c"),
        //                            Passenger = x.Count().ToString() + (x.Count() > 1 ? " Travelers" : " Traveler"),
        //                            //asKK
        //                            SegmentDescription = premiumCabinUpgrade.GetFormattedCabinName(t.Select(u => u.Description).FirstOrDefault().ToString()),
        //                            //IsPurchaseFailure = unSuccessfulSegmentDetails.Select(q => q.Value).ToList().Select(q => q.Split(' ')).Any(q => q[1] == t.Select(u => u.SegmentNumber).FirstOrDefault() && q[4].Split(':')[1].Split('.')[0] == t.Select(u => u.TravelerRefID).FirstOrDefault())
        //                            IsPurchaseFailure = unSuccessfulSegmentDetails.Select(q => q.Value).ToList().Select(q => q.Split(' ')).Any(q => q[1] == t.Select(u => u.SegmentNumber).FirstOrDefault())
        //                        }).ToList()
        //                    }).ToList() : null,
        //                    //ProdOtherPrice = String.Format("{0:0.00}", (isPartialSuccess == true) ? (flightReservationResponse.DisplayCart.TravelOptions.SelectMany(x => x.SubItems).Where(x => x.Amount != 0 && chargedSegmentNums.Contains(x.SegmentNumber) && unSuccessfulSegmentDetails.Select(r => r.Value).ToList().Select(r => r.Split(' ')).Select(r => r[4]).Select(r => r.Split(':')).Select(r => r[1]).Select(r => r.Split('.')[0]).Contains(x.TravelerRefID)).Select(r => r.Amount).Sum()) : flightReservationResponse.DisplayCart.TravelOptions.SelectMany(x => x.SubItems).Where(x => x.Amount != 0).Select(r => r.Amount).Sum()),
        //                    //ProdDisplayOtherPrice = (isPartialSuccess == true) ? (flightReservationResponse.DisplayCart.TravelOptions.SelectMany(x => x.SubItems).Where(x => x.Amount != 0 && chargedSegmentNums.Contains(x.SegmentNumber) && unSuccessfulSegmentDetails.Select(r => r.Value).ToList().Select(r => r.Split(' ')).Select(r => r[4]).Select(r => r.Split(':')).Select(r => r[1]).Select(r => r.Split('.')[0]).Contains(x.TravelerRefID)).Select(r => r.Amount).Sum()).ToString("c") : flightReservationResponse.DisplayCart.TravelOptions.SelectMany(x => x.SubItems).Where(x => x.Amount != 0).Select(r => r.Amount).Sum().ToString("c"),
        //                    ProdOtherPrice = String.Format("{0:0.00}", (isPartialSuccess == true) ? (flightReservationResponse.DisplayCart.TravelOptions.Where(d => d.Key == productCode).SelectMany(x => x.SubItems).Where(x => x.Amount != 0 && chargedSegmentNums.Contains(x.SegmentNumber)).Select(r => r.Amount).Sum()) : flightReservationResponse.DisplayCart.TravelOptions.Where(d => d.Key == productCode).SelectMany(x => x.SubItems).Where(x => x.Amount != 0).Select(r => r.Amount).Sum()),
        //                    ProdDisplayOtherPrice = (isPartialSuccess == true) ? (flightReservationResponse.DisplayCart.TravelOptions.Where(d => d.Key == productCode).SelectMany(x => x.SubItems).Where(x => x.Amount != 0 && chargedSegmentNums.Contains(x.SegmentNumber)).Select(r => r.Amount).Sum()).ToString("c") : flightReservationResponse.DisplayCart.TravelOptions.Where(d => d.Key == productCode).SelectMany(x => x.SubItems).Where(x => x.Amount != 0).Select(r => r.Amount).Sum().ToString("c"),
        //                    ProdTotalMiles = 0,
        //                    ProdDisplayTotalMiles = string.Empty
        //                };
        //                prodDetails.Add(prodDetail);
        //            }
        //            else
        //            {
        //                var travelOptions = flightReservationResponse.DisplayCart.TravelOptions?.Where(d => d.Key == productCode).ToCollection().Clone();
        //                bool isBundleProduct = string.Equals(travelOptions?.FirstOrDefault(t => t.Key == productCode)?.Type, "BE", StringComparison.OrdinalIgnoreCase); //ensuring bundle product
        //                if (!string.IsNullOrEmpty(_configuration.GetValue<string>("EnablePCUSelectedSeatPurchaseViewRes")))
        //                {
        //                    if (flightReservationResponse.DisplayCart.DisplaySeats != null && flightReservationResponse.DisplayCart.DisplaySeats.Any(s => s != null && s.PCUSeat))
        //                    {
        //                        travelOptions.Where(t => t.Key == "PCU")
        //                                     .ForEach(t => t.SubItems.RemoveWhere(sb => sb.Amount == 0 || flightReservationResponse.DisplayCart.DisplaySeats.Any(s => s.PCUSeat && s.OriginalSegmentIndex.ToString() == sb.SegmentNumber && (s.TravelerIndex + 1).ToString() == sb.TravelerRefID)));
        //                        travelOptions.RemoveWhere(t => t.SubItems == null || !t.SubItems.Any());
        //                    }

        //                    if (travelOptions == null || !travelOptions.Any())
        //                        continue;

        //                    if (flightReservationResponse.Errors != null && flightReservationResponse.Errors.Any(e => e != null && e.MinorCode == "90506"))
        //                    {
        //                        if (!UtilityHelper.IsRefundSuccess(flightReservationResponse.CheckoutResponse.ShoppingCartResponse.Items, out refundedSegmentNums))
        //                        {
        //                            continue;
        //                        }
        //                    }
        //                }

        //                var prodDetail = new ProdDetail()
        //                {
        //                    Code = travelOptions.Where(d => d.Key == productCode).Select(d => d.Key).FirstOrDefault().ToString(),
        //                    ProdDescription = (productCode == "TPI" && (_configuration.GetValue<bool>("GetTPIProductName_HardCode"))) ? "Travel insurance" : travelOptions.Where(d => d.Key == productCode).Select(d => d.Type).FirstOrDefault().ToString(),
        //                    ProdTotalPrice = String.Format("{0:0.00}", travelOptions.Select(d => d.Amount).Sum()),
        //                    ProdDisplayTotalPrice = Decimal.Parse(travelOptions.Select(d => d.Amount).Sum().ToString()).ToString("c"),
        //                    ProdOriginalPrice = String.Format("{0:0.00}", travelOptions.Select(d => d.OriginalPrice).Sum()),
        //                    Segments = travelOptions
        //                    .Where(d => d.Key == productCode)
        //                    .SelectMany(x => x.SubItems)
        //                    .Where(x => (ShouldIgnoreAmount(x, flightReservationResponse, flow)) ? true : x.Amount != 0 || (!_configuration.GetValue<bool>("DisableFreeCouponFix") && x.OriginalPrice != 0))
        //                    .OrderBy(x => x.SegmentNumber).GroupBy(x => x.SegmentNumber)
        //                    .ToList()
        //                    .Select(x => new ProductSegmentDetail
        //                    {
        //                        SegmentInfo = BuildSegmentInfo(productCode, flightReservationResponse.Reservation.FlightSegments, x),
        //                        ProductId = string.Join(",", x.Select(u => u.Key).ToList()),
        //                        TripId = string.Join(",", x.Select(u => u.TripIndex).ToList()),
        //                        ProductIds = x.Select(u => u.Key).ToList(),
        //                        SubSegmentDetails = x.GroupBy(f => f.SegmentNumber).Select(t => new ProductSubSegmentDetail
        //                        {
        //                            SegmentInfo = ConfigUtility.IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major) && productCode != "TPI" ? BuildSegmentInfo(productCode, flightReservationResponse.Reservation.FlightSegments, x, isBundleProduct) : string.Empty,
        //                            Price = String.Format("{0:0.00}", t.Select(i => i.Amount).Sum()),
        //                            OrginalPrice = String.Format("{0:0.00}", t.Select(i => i.OriginalPrice).Sum()),
        //                            DisplayOriginalPrice = ConfigUtility.IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major) ? UtilityHelper.GetDisplayOriginalPrice(t.Select(i => i.Amount).Sum(), t.Select(i => i.OriginalPrice).Sum()) : string.Empty,
        //                            DisplayPrice = Decimal.Parse(t.Select(i => i.Amount).Sum().ToString()).ToString("c"),
        //                            Passenger = GetPassengerText(flow, productCode, x.Count(), flightReservationResponse.Reservation.Travelers.Count),
        //                            SegmentDescription = BuildProductDescription(travelOptions, t, productCode),
        //                            IsPurchaseFailure = IsPurchaseFailed(productCode == "PCU", t.Select(sb => sb.SegmentNumber).FirstOrDefault(), refundedSegmentNums),
        //                            ProdDetailDescription = ConfigUtility.IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major) ? UtilityHelper.GetProductDetailDescrption(t) : null,
        //                            ProductDescription = ConfigUtility.IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major) ? UtilityHelper.GetProductDescription(travelOptions, productCode) : string.Empty
        //                        }).ToList()
        //                    }).ToList(),
        //                    ProdTotalMiles = 0,
        //                    ProdDisplayTotalMiles = string.Empty
        //                };
        //                if (isGetCartInformation)
        //                {
        //                    List<MOBTypeOption> lineItems = new List<MOBTypeOption>();
        //                    MOBBag bagCounts = null;
        //                    Services.FlightShopping.Common.DisplayCart.TravelOption travelOption = flightReservationResponse.DisplayCart?.TravelOptions?.FirstOrDefault(x => x.Type == "Bags");

        //                    var product = travelOption?.SubItems?.Select(p => p.Product).Where(p => p != null);
        //                    if (travelOption != null && product != null)
        //                    {
        //                        bagCounts = new MOBBag()
        //                        {
        //                            TotalBags = travelOption.ItemCount,
        //                            OverWeightBags = product.SelectMany(subproducts => subproducts.SubProducts.Select(b => b.EddCode == "OWB")).Where(exist => exist == true).Count(),
        //                            OverWeight2Bags = product.SelectMany(subproducts => subproducts.SubProducts.Select(b => b.EddCode == "OWH")).Where(exist => exist == true).Count(),
        //                            OverSizeBags = product.SelectMany(subproducts => subproducts.SubProducts.Select(b => b.EddCode == "OSB")).Where(exist => exist == true).Count(),
        //                            ExceptionItemInfantSeat = product.SelectMany(subproducts => subproducts.SubProducts.Select(b => b.EddCode.Contains("EX"))).Where(exist => exist == true).Count()
        //                        };
        //                        prodDetail.LineItems = GetBagsLineItems(bagCounts);
        //                    }
        //                }
        //                if (prodDetail == null) { continue; }
        //                if (!United.Common.Helper.UtilityHelper.IsCheckinFlow(flow)) { United.Common.Helper.UtilityHelper.UpdateRefundTotal(prodDetail); }
        //                if (!string.IsNullOrEmpty(prodDetail.ProdDisplayTotalPrice) || !string.IsNullOrEmpty(prodDetail.ProdDisplayOtherPrice) || IsOriginalPriceExists(prodDetail))
        //                    prodDetails.Add(prodDetail);
        //            }
        //        }
        //        if ((_configuration.GetValue<bool>("EnableCouponsforBooking") && flow == FlowType.BOOKING.ToString() && prodDetails != null)
        //            || (_configuration.GetValue<bool>("EnableCouponsInPostBooking") && flow == FlowType.POSTBOOKING.ToString()) || (_configuration.GetValue<bool>("IsEnableManageResCoupon") && (flow == FlowType.VIEWRES.ToString() || flow == FlowType.VIEWRES_SEATMAP.ToString())))
        //        {
        //            AddCouponDetails(prodDetails, flightReservationResponse, isPost, flow, application);
        //        }

        //    }
        //    return prodDetails;
        //}

        private static bool ShouldIgnoreAmount(SubItem subItem, FlightReservationResponse flightReservationResponse, string flow)
        {
            if (string.IsNullOrEmpty(subItem.Reason))
                return false;

            return subItem.Reason.ToUpper().Equals("INF");
        }

        public static double GetProductFee(bool isPost, United.Service.Presentation.InteractionModel.ShoppingCart flightReservationResponseShoppingCart, string flow, string productCode)
        {
            return isPost ? flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).FirstOrDefault().Price.Totals.FirstOrDefault().Amount :
                                  flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).SelectMany(x => x.Price.Totals).FirstOrDefault().Amount;
        }

        public static List<string> GetProductCodes(FlightReservationResponse flightReservationResponse, string flow, bool isPost, bool isGetCartInfo)
        {
            List<string> productCodes = new List<string>();

            if (isPost ? flightReservationResponse.CheckoutResponse.ShoppingCartResponse.Items.Select(x => x.Item).Any(x => x.Product.FirstOrDefault().Code == "FLK")
                        : flightReservationResponse.ShoppingCart.Items.Any(x => x.Product.FirstOrDefault().Code == "FLK"))
                flow = FlowType.FARELOCK.ToString();

            switch (flow)
            {
                case "BOOKING":
                case "RESHOP":
                    productCodes = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart.Items.Where(x => !CheckFailedShoppingCartItem(flightReservationResponse, x)).Select(x => x.Product.FirstOrDefault().Code).Distinct().ToList()
                        : flightReservationResponse.ShoppingCart.Items.Select(x => x.Product.FirstOrDefault().Code).ToList();
                    //productCodes = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCartResponse.Items.Where(x => !CheckFailedShoppingCartItem(x)).Select(x => x.Item).Select(x => x.Product.FirstOrDefault().Code).Distinct().ToList()
                    //    : flightReservationResponse.ShoppingCart.Items.Select(x => x.Product.FirstOrDefault().Code).ToList();
                    break;

                case "POSTBOOKING":
                    productCodes = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart.Items.Where(x => !CheckFailedShoppingCartItem(flightReservationResponse, x)).SelectMany(x => x.Product).Where(x => x.Characteristics != null && (x.Characteristics.Any(y => y.Description == "PostPurchase" && Convert.ToBoolean(y.Value) == true))).Select(x => x.Code).Distinct().ToList()
                        : flightReservationResponse.ShoppingCart.Items.SelectMany(x => x.Product).Where(x => x.Characteristics != null && (x.Characteristics.Any(y => y.Description == "PostPurchase" && Convert.ToBoolean(y.Value) == true))).Select(x => x.Code).Distinct().ToList();
                    break;

                case "FARELOCK":
                    productCodes = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart.Items.Where(x => !CheckFailedShoppingCartItem(flightReservationResponse, x)).Where(x => x.Product.FirstOrDefault().Code != "RES").Select(x => x.Product.FirstOrDefault().Code).Distinct().ToList()
                        : flightReservationResponse.ShoppingCart.Items.Where(x => x.Product.FirstOrDefault().Code != "RES").Select(x => x.Product.FirstOrDefault().Code).Distinct().ToList();
                    break;

                case "VIEWRES":
                case "VIEWRES_SEATMAP":
                case "CHECKIN":
                case "MOBILECHECKOUT":
                    productCodes = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart.Items.Where(x => !CheckFailedShoppingCartItem(flightReservationResponse, x)).Where(x => x.Product.FirstOrDefault().Code != "RES").Select(x => x.Product.FirstOrDefault().Code).Distinct().ToList()
                        : flightReservationResponse.ShoppingCart.Items.Where(x => x.Product.FirstOrDefault().Code != "RES").Select(x => x.Product.FirstOrDefault().Code).Distinct().ToList();
                    break;

            }

            return productCodes;
        }

        private static bool CheckFailedShoppingCartItem(FlightReservationResponse flightReservationResponse, United.Service.Presentation.InteractionModel.ShoppingCartItem item)
        {
            string productCode = item.Product.Select(z => z.Code).FirstOrDefault().ToString();
            bool isFailed = false;

            switch (productCode)
            {
                case "TPI":
                case "EFS":
                case "SEATASSIGNMENTS":
                    isFailed = flightReservationResponse.CheckoutResponse.ShoppingCartResponse.Items.Select(x => x.Item).Any(x => x.Product.Select(y => y.Code).FirstOrDefault() == productCode.ToString() && (x.Status.Contains("FAILED")));
                    break;

                default:
                    isFailed = flightReservationResponse.CheckoutResponse.ShoppingCartResponse.Items.Any(x => x.Item.Product.Select(y => y.Code).FirstOrDefault() == productCode.ToString() && (x.Error != null && x.Error.Count > 0));
                    break;
            }

            return isFailed;
        }

        public async Task<List<MOBMobileCMSContentMessages>> GetProductBasedTermAndConditions(United.Service.Presentation.ProductResponseModel.ProductOffer productVendorOffer, FlightReservationResponse flightReservationResponse, bool isPost, bool isGetCartInfo = false)
        {
            List<MOBMobileCMSContentMessages> tNClist = new List<MOBMobileCMSContentMessages>();
            MOBMobileCMSContentMessages tNC = null;
            List<MOBTypeOption> typeOption = null;

            var productCodes = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart.Items.Where(x => x.Product.FirstOrDefault().Code != "RES").Select(x => x.Product.FirstOrDefault().Code).ToList() :
                                       flightReservationResponse.ShoppingCart.Items.Where(x => x.Product.FirstOrDefault().Code != "RES").Select(x => x.Product.FirstOrDefault().Code).ToList();

            if (productCodes == null || !productCodes.Any())
                return null;

            if (isPost == true)
            {
                bool isTPIFailed = flightReservationResponse.CheckoutResponse.ShoppingCartResponse.Items.Any(x => x.Item.Product[0].Code == "TPI") &&
                    flightReservationResponse.CheckoutResponse.ShoppingCartResponse.Items.Where(x => x.Item.Product[0].Code == "TPI").Select(x => x.Item).Any(y => y.Status.Contains("FAILED"));

                isPost = !isTPIFailed; //If TPI failed during checkout then set isPost to false.
            }

            foreach (var productCode in productCodes)
            {
                if (isPost == false)
                {
                    switch  (productCode)
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
                            //TODO Remove
                            ///MerchandizingServices merchandizingServices = new MerchandizingServices();
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
                            string tncTPIMessage1 = product.Presentation.Contents.Where(x => x.Header.ToUpper() == "MobPaymentTAndCHeader1Message".ToUpper()).Select(x => x.Body).FirstOrDefault().ToString();
                            string tncTPIMessage2 = product.Presentation.Contents.Where(x => x.Header.ToUpper() == "MobPaymentTAndCBodyUrlMessage".ToUpper()).Select(x => x.Body).FirstOrDefault().ToString();
                            string tncTPIMessage3 = product.Presentation.Contents.Where(x => x.Header.ToUpper() == "MobPaymentTAndCUrlHeaderMessage".ToUpper()).Select(x => x.Body).FirstOrDefault().ToString();
                            string tncTPIMessage4 = product.Presentation.Contents.Where(x => x.Header.ToUpper() == "MobPaymentTAndCUrlHeader2Message".ToUpper()).Select(x => x.Body).FirstOrDefault().ToString();
                            string tncTPIMessage5 = product.Presentation.Contents.Any(x => x.Header.ToUpper() == "MobTIDetailsTAndCUrlMessage".ToUpper()) ? product.Presentation.Contents.Where(x => x.Header.ToUpper() == "MobTIDetailsTAndCUrlMessage".ToUpper()).Select(x => x.Body).FirstOrDefault().ToString() : string.Empty;
                            string tncTPIMessage6 = product.Presentation.Contents.Any(x => x.Header.ToUpper() == "MobTIDetailsTAndCUrlHeaderMessage".ToUpper()) ? product.Presentation.Contents.Where(x => x.Header.ToUpper() == "MobTIDetailsTAndCUrlHeaderMessage".ToUpper()).Select(x => x.Body).FirstOrDefault().ToString() : string.Empty;
                            string tncTPIMessage7 = product.Presentation.Contents.Any(x => x.Header.ToUpper() == "MobTGIAndMessage".ToUpper()) ? product.Presentation.Contents.Where(x => x.Header.ToUpper() == "MobTGIAndMessage".ToUpper()).Select(x => x.Body).FirstOrDefault().ToString() : string.Empty;
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
                        case "SEATASSIGNMENTS":
                            if (!isGetCartInfo)
                                break;
                            if (string.IsNullOrEmpty(_configuration.GetValue<string>("EnablePCUSelectedSeatPurchaseViewRes")))
                                break;
                            var seatTypes = flightReservationResponse.DisplayCart.DisplaySeats.Where(s => s.SeatPrice > 0).Select(s => GetCommonSeatCode(s.SeatPromotionCode)).ToList();
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
                        case "PET":
                            if (!isGetCartInfo)
                                break;
                            tNC = new MOBMobileCMSContentMessages();
                            List<MOBMobileCMSContentMessages> tncPET = await GetTermsAndConditions("PETINCABIN_TNC");
                            tNC.Title = "Terms and conditions";
                            tNC.ContentShort = _configuration.GetValue<string>("PaymentTnCMessage").ToString();
                            tNC.ContentFull = tncPET[0].ContentFull;
                            tNC.HeadLine = tncPET[0].Title;
                            tNClist.Add(tNC);
                            break;
                        case "OTP":
                            if (!isGetCartInfo)
                                break;
                            tNC = new MOBMobileCMSContentMessages();
                            string termsAndConditionsConfigurationKey = "UnitedClubDayPassTermsAndConditions" + flightReservationResponse.DisplayCart.CountryCode;
                            typeOption = new List<MOBTypeOption>();
                            typeOption = GetTCList(termsAndConditionsConfigurationKey);

                            tNC.Title = "Terms and conditions";
                            tNC.ContentShort = _configuration.GetValue<string>("PaymentTnCMessage").ToString();
                            tNC.ContentFull = "<ul><li>" + string.Join("<br></li><li>", typeOption.Select(x => x.Value)) + "</li></ul>";
                            tNC.HeadLine = "United Club Pass";
                            tNClist.Add(tNC);
                            break;
                    }
                }
                else if (isPost == true)
                {
                    switch (productCode)
                    {
                        case "TPI":
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

            return tNClist;
        }

        public string GetCommonSeatCode(string seatCode)
        {
            if (string.IsNullOrEmpty(seatCode))
                return string.Empty;

            string commonSeatCode = string.Empty;

            switch (seatCode.ToUpper().Trim())
            {
                case "SXZ": //StandardPreferredExitPlus
                case "SZX": //StandardPreferredExit
                case "SBZ": //StandardPreferredBlukheadPlus
                case "SZB": //StandardPreferredBlukhead
                case "SPZ": //StandardPreferredZone
                case "PZA":
                    commonSeatCode = "PZA";
                    break;
                case "SXP": //StandardPrimeExitPlus
                case "SPX": //StandardPrimeExit
                case "SBP": //StandardPrimeBlukheadPlus
                case "SPB": //StandardPrimeBlukhead
                case "SPP": //StandardPrimePlus
                case "PPE": //StandardPrime
                case "BSA":
                case "ASA":
                    commonSeatCode = "ASA";
                    break;
                case "EPL": //EplusPrime
                case "EPU": //EplusPrimePlus
                case "BHS": //BulkheadPrime
                case "BHP": //BulkheadPrimePlus  
                case "PSF": //PrimePlus 
                    commonSeatCode = "EPU";
                    break;
                case "PSL": //Prime                           
                    commonSeatCode = "PSL";
                    break;
                default:
                    return seatCode;
            }
            return commonSeatCode;
        }

        public async Task<List<MOBMobileCMSContentMessages>> GetTermsAndConditions(string title)
        {
            var cmsContentMessages = new List<MOBMobileCMSContentMessages>();
            var docKeys = new List<string> { title };
            var docs = await _documentLibraryDynamoDB.GetNewLegalDocumentsForTitles(docKeys, _headers.ContextValues.SessionId).ConfigureAwait(false);
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

        public List<MOBTypeOption> GetTCList(string configValue)
        {
            List<MOBTypeOption> contentList = new List<MOBTypeOption>();
            if (_configuration.GetValue<string>(configValue) != null)
            {
                string tcContentList = _configuration.GetValue<string>(configValue);
                foreach (string eachItem in tcContentList.Split('|'))
                {
                    contentList.Add(new MOBTypeOption() { Value = eachItem });
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
                            var productVendorOffer = new Mobile.Model.ManageRes.GetVendorOffers();
                            productVendorOffer = await _sessionHelperService.GetSession<Mobile.Model.ManageRes.GetVendorOffers>(sessionId, productVendorOffer.ObjectName, new List<string> { sessionId, productVendorOffer.ObjectName }).ConfigureAwait(false);
                            if (productVendorOffer == null)
                                break;

                            tNC = new MOBMobileCMSContentMessages();
                            var product = productVendorOffer.Offers.FirstOrDefault(a => a.ProductInformation.ProductDetails.Where(b => b.Product != null && b.Product.Code.ToUpper().Trim() == "TPI").ToList().Count > 0).
                                ProductInformation.ProductDetails.FirstOrDefault(c => c.Product != null && c.Product.Code.ToUpper().Trim() == "TPI").Product;

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
                            var productVendorOffer = new Mobile.Model.ManageRes.GetVendorOffers();
                            productVendorOffer =await _sessionHelperService.GetSession<Mobile.Model.ManageRes.GetVendorOffers>(sessionId, productVendorOffer.ObjectName, new List<string> { sessionId, productVendorOffer.ObjectName }).ConfigureAwait(false);
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
                tNClist.Add(tNC);
            }

            return tNClist;
        }

        private ProdDetail BuildProdDetailsForSeats(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, SeatChangeState state, bool isPost, string flow)
        {
            if (flightReservationResponse.DisplayCart.DisplaySeats == null || !flightReservationResponse.DisplayCart.DisplaySeats.Any())
            {
                return null;
            }
            //check here.
            var fliterSeats = flightReservationResponse.DisplayCart.DisplaySeats.Where(d => d.PCUSeat || (CheckSeatAssignMessage(d.SeatAssignMessage, isPost) && d.Seat != "---")).ToList();
            if (_configuration.GetValue<bool>("EnablePCUFromSeatMapErrorCheckViewRes"))
            {
                fliterSeats = HandleCSLDefect(flightReservationResponse, fliterSeats, isPost);
            }
            if (!fliterSeats.Any())
            {
                return null;
            }

            var totalPrice = fliterSeats.Select(s => s.SeatPrice).ToList().Sum();
            var prod = new ProdDetail
            {
                Code = "SEATASSIGNMENTS",
                ProdDescription = string.Empty,
                ProdTotalPrice = String.Format("{0:0.00}", totalPrice),
                ProdDisplayTotalPrice = totalPrice > 0 ? Decimal.Parse(totalPrice.ToString()).ToString("c") : string.Empty,
                Segments = BuildProductSegmentsForSeats(flightReservationResponse, state?.Seats, state?.BookingTravelerInfo, isPost, flow)
            };
            if (prod.Segments != null && prod.Segments.Any())
            {
                if (IsMilesFOPEnabled())
                {
                    if (prod.Segments.SelectMany(s => s.SubSegmentDetails).ToList().Select(ss => ss.Miles == 0).ToList().Count == 0 && IsMilesFOPEnabled())
                    {
                        prod.ProdTotalMiles = _configuration.GetValue<int>("milesFOP");
                        prod.ProdDisplayTotalMiles = ShopStaticUtility.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false);
                    }
                    else
                    {
                        prod.ProdTotalMiles = 0;
                        prod.ProdDisplayTotalMiles = string.Empty;
                    }
                }
                if (_configuration.GetValue<bool>("IsEnableManageResCoupon") && isAFSCouponApplied(flightReservationResponse.DisplayCart))
                    prod.Segments.Select(x => x.SubSegmentDetails).ToList().ForEach(item => item.RemoveAll(k => Decimal.Parse(k.Price) == 0 && (string.IsNullOrEmpty(k.OrginalPrice) || Decimal.Parse(k.OrginalPrice) == 0)));
                else
                    prod.Segments.Select(x => x.SubSegmentDetails).ToList().ForEach(item => item.RemoveAll(k => Decimal.Parse(k.Price) == 0 && (k.StrikeOffPrice == string.Empty || Decimal.Parse(k.StrikeOffPrice) == 0)));

                prod.Segments.RemoveAll(k => k.SubSegmentDetails.Count == 0);
            }
            ShopStaticUtility.UpdateRefundTotal(prod);
            return prod;
        }


        private async Task<List<ProdDetail>> BuildProductDetailsForInflightMeals(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, string productCode, string sessionId, bool isPost)
        {
            List<MOBInFlightMealsRefreshmentsResponse> savedResponse =
            await _sessionHelperService.GetSession<List<MOBInFlightMealsRefreshmentsResponse>>(sessionId, new MOBInFlightMealsRefreshmentsResponse().ObjectName, new List<string> { sessionId, new MOBInFlightMealsRefreshmentsResponse().ObjectName }).ConfigureAwait(false); //change session
            United.Service.Presentation.InteractionModel.ShoppingCart flightReservationResponseShoppingCart;
            if (isPost)
                flightReservationResponseShoppingCart = flightReservationResponse.CheckoutResponse.ShoppingCart;
            else
                flightReservationResponseShoppingCart = flightReservationResponse.ShoppingCart;


            var displayTotalPrice = flightReservationResponse.DisplayCart.DisplayPrices.FirstOrDefault(o => (o.Description != null && o.Description.Equals("Total", StringComparison.OrdinalIgnoreCase))).Amount;
            var grandTotal = flightReservationResponseShoppingCart?.Items.SelectMany(p => p.Product).Where(d => d.Code == "POM")?.Select(p => p.Price?.Totals?.FirstOrDefault().Amount).FirstOrDefault();

            var travelOptions = ShopStaticUtility.GetTravelOptionItems(flightReservationResponse, productCode);
            // For RegisterOffer uppercabin when there is no price no need to build the product
            List<ProdDetail> response = new List<ProdDetail>();
            if (grandTotal > 0 && productCode == _configuration.GetValue<string>("InflightMealProductCode"))
            {
                var productDetail = new ProdDetail()
                {
                    Code = travelOptions.Where(d => d.Key == productCode).Select(d => d.Key).FirstOrDefault().ToString(),
                    ProdDescription = travelOptions.Where(d => d.Key == productCode).Select(d => d.Type).FirstOrDefault().ToString(),
                    ProdTotalPrice = String.Format("{0:0.00}", grandTotal),
                    ProdDisplayTotalPrice = grandTotal?.ToString("c"),
                    Segments = GetProductSegmentForInFlightMeals(flightReservationResponse, savedResponse, travelOptions, flightReservationResponseShoppingCart),
                };
                response.Add(productDetail);
                return response;
            }
            else return response;

        }

        private string BuildSegmentInfo(string productCode, Collection<ReservationFlightSegment> flightSegments, IGrouping<string, SubItem> x)
        {
            if (productCode == "AAC" || productCode == "PAC")
                return string.Empty;

            if (_configuration.GetValue<bool>("EnableBasicEconomyBuyOutInViewRes") && productCode == "BEB")
            {
                var tripNumber = flightSegments?.Where(y => y.FlightSegment.SegmentNumber == Convert.ToInt32(x.Select(u => u.SegmentNumber).FirstOrDefault())).FirstOrDefault().TripNumber;
                var tripFlightSegments = flightSegments?.Where(c => c != null && !string.IsNullOrEmpty(c.TripNumber) && c.TripNumber.Equals(tripNumber)).ToCollection();
                if (tripFlightSegments != null && tripFlightSegments.Count > 1)
                {
                    return tripFlightSegments?.FirstOrDefault()?.FlightSegment?.DepartureAirport?.IATACode + " - " + tripFlightSegments?.LastOrDefault()?.FlightSegment?.ArrivalAirport?.IATACode;
                }
                else
                {
                    return flightSegments.Where(y => y.FlightSegment.SegmentNumber == Convert.ToInt32(x.Select(u => u.SegmentNumber).FirstOrDefault())).Select(y => y.FlightSegment.DepartureAirport.IATACode + " - " + y.FlightSegment.ArrivalAirport.IATACode).FirstOrDefault().ToString();
                }
            }

            return flightSegments.Where(y => y.FlightSegment.SegmentNumber == Convert.ToInt32(x.Select(u => u.SegmentNumber).FirstOrDefault())).Select(y => y.FlightSegment.DepartureAirport.IATACode + " - " + y.FlightSegment.ArrivalAirport.IATACode).FirstOrDefault().ToString();
        }

        private ProdDetail BuildProdDetailsForSeats(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, SeatChangeState state, string flow, MOBApplication application)
        {
            if (ShopStaticUtility.IsCheckinFlow(flow) && flightReservationResponse.DisplayCart.TravelOptions != null && flightReservationResponse.DisplayCart.TravelOptions.Any(x => x.Type == "SEATASSIGNMENTS"))
            {
                ProdDetail prod = new ProdDetail();
                prod.Code = "SEATASSIGNMENTS";
                prod.ProdDescription = string.Empty;
                decimal totalPrice = flightReservationResponse.DisplayCart.TravelOptions.Sum(x => x.Type == "SEATASSIGNMENTS" ? x.Amount : 0);
                prod.ProdTotalPrice = String.Format("{0:0.00}", totalPrice);
                prod.ProdDisplayTotalPrice = $"${prod.ProdTotalPrice}";
                if (totalPrice > 0)
                {
                    var displaySeats = flightReservationResponse.DisplayCart.DisplaySeats.Where(x => x.OriginalPrice > 0).Select(x => { x.SeatType = ShopStaticUtility.GetCommonSeatCode(x.SeatType); return x; }).GroupBy(x => $"{x.DepartureAirportCode} - {x.ArrivalAirportCode}");
                    prod.Segments = BuildCheckinSegmentDetail(displaySeats);
                }
                return prod;
            }

            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("EnablePCUSelectedSeatPurchaseViewRes")) && !ShopStaticUtility.IsCheckinFlow(flow))
            {
                if (!_configuration.GetValue<bool>("DisableMinusPriceSeatsegmentPopulatingIssueFix"))
                {
                    var seatTotalPrice = flightReservationResponse.DisplayCart.DisplaySeats.Select(d => d.SeatPrice).ToList().Sum();
                    var prod = new ProdDetail
                    {
                        Code = "SEATASSIGNMENTS",
                        ProdDescription = string.Empty,
                        ProdTotalPrice = seatTotalPrice > 0 ? String.Format("{0:0.00}", seatTotalPrice) : string.Format("{0:0.00}", 0),
                        ProdDisplayTotalPrice = seatTotalPrice > 0 ? seatTotalPrice.ToString("c") : 0.ToString("c"),
                        Segments = seatTotalPrice > 0 ? BuildProductSegmentsForSeats(flightReservationResponse, state?.Seats, application, flow) : null
                    };
                    prod.Segments?.Select(x => x.SubSegmentDetails).ToList().ForEach(item => item.RemoveAll(k => Decimal.Parse(k.Price) == 0 && (k.StrikeOffPrice == string.Empty || Decimal.Parse(k.StrikeOffPrice) == 0)));
                    prod.Segments?.RemoveAll(k => k.SubSegmentDetails.Count == 0);
                    return prod;
                }
                else
                {
                    var prod = new ProdDetail
                    {
                        Code = "SEATASSIGNMENTS",
                        ProdDescription = string.Empty,
                        ProdTotalPrice = String.Format("{0:0.00}", flightReservationResponse.DisplayCart.DisplaySeats.Select(d => d.SeatPrice).ToList().Sum()),
                        ProdDisplayTotalPrice = Decimal.Parse(flightReservationResponse.DisplayCart.DisplaySeats.Select(d => d.SeatPrice).ToList().Sum().ToString()).ToString("c"),
                        Segments = BuildProductSegmentsForSeats(flightReservationResponse, state?.Seats, application, flow)
                    };
                    prod.Segments.Select(x => x.SubSegmentDetails).ToList().ForEach(item => item.RemoveAll(k => Decimal.Parse(k.Price) == 0 && (k.StrikeOffPrice == string.Empty || Decimal.Parse(k.StrikeOffPrice) == 0)));
                    prod.Segments.RemoveAll(k => k.SubSegmentDetails.Count == 0);
                    return prod;
                }
            }

            var prodDetail = new ProdDetail()
            {
                Code = "SEATASSIGNMENTS",
                ProdDescription = string.Empty,
                ProdTotalPrice = String.Format("{0:0.00}", flightReservationResponse.DisplayCart.DisplaySeats.Select(d => d.SeatPrice).ToList().Sum()),
                ProdDisplayTotalPrice = Decimal.Parse(flightReservationResponse.DisplayCart.DisplaySeats.Select(d => d.SeatPrice).ToList().Sum().ToString()).ToString("c"),
                //Mobile-1524: Include all the seats even if seat price is null when user has E+/Preferred subscriptions
                //                                                           |----Ignore the DisplaySeats ---------|------Updating the SeatPromotionCode to a common code for easy grouping ---------------|                                                                                                                            |--Ordering the DisplaySeats based---|--Group the resulted DisplaySeats objects based on OriginalSegmentIndex and LegIndex value and return the List of DisplaySeats -|   
                //       LEVEL 1                                             |----if SeatPromotionCode = null------|------and return the object using GetCommonCode method---------------------------------|----------Ignoring DisplaySeats child object if either SeatPrice = 0  ------------------------------------------------      |--on OriginalSegmentIndex-----------|--This is been done for COG, THRU flights. These flights even though are one segment at high level but have multiple segments---|
                Segments = flightReservationResponse.DisplayCart.DisplaySeats.Where(x => x.SeatPromotionCode != null).Select(x => { x.SeatPromotionCode = ShopStaticUtility.GetCommonSeatCode(x.SeatPromotionCode); return x; }).Where(d => (state != null ? (state.TotalEplusEligible > 0 ? true : d.SeatPrice != 0) : d.SeatPrice != 0)).OrderBy(d => d.OriginalSegmentIndex).GroupBy(d => new { d.OriginalSegmentIndex, d.LegIndex }).Select(d => new ProductSegmentDetail
                //Segments = flightReservationResponse.DisplayCart.DisplaySeats.Where(x => x.SeatPromotionCode != null).Select(x => { x.SeatPromotionCode = ShopStaticUtility.GetCommonSeatCode(x.SeatPromotionCode); return x; }).Where(d => (true? d.SeatPrice != 0 : d.SeatPrice >= 0)).OrderBy(d =>d.Orih]]]]]]]]]]]ginalSegmentIndex).GroupBy(d => new { d.OriginalSegmentIndex, d.LegIndex }).Select(d => new MOBProductSegmentDetail
                {
                    //                |--------------Get the individual Segment Origin and Destination detail based on OriginalSegmentIndex and LegIndex for the list of DisplaySeats from LEVEL 1---| 
                    SegmentInfo = ShopStaticUtility.GetSegmentInfo(flightReservationResponse, d.Select(u => u.OriginalSegmentIndex).FirstOrDefault(), Convert.ToInt32(d.Select(u => u.LegIndex).FirstOrDefault())),
                    ProductId = null,
                    //     LEVEL 2         |---- Further group the LEVEL 1 list of DisplaySeats based on OriginalSegmentIndex and SeatPromotionCode to get SubSegmentDetails--|
                    SubSegmentDetails = d.GroupBy(t => new { t.OriginalSegmentIndex, t.SeatPromotionCode }).Select(t => new ProductSubSegmentDetail
                    {
                        //              |--Getting the sum of SeatPrice from the list of DisplaySeats at LEVEL 2 --|
                        Price = String.Format("{0:0.00}", t.Select(s => s.SeatPrice).ToList().Sum()),
                        DisplayPrice = Decimal.Parse(t.Select(s => s.SeatPrice).ToList().Sum().ToString()).ToString("c"),
                        //                  | --Getting the count of list of DisplaySeats at LEVEL 2-- |
                        Passenger = t.Count().ToString() + (t.Count() > 1 ? " Travelers" : " Traveler"), // t.GroupBy(u => u.TravelerIndex).Count().ToString() + (t.GroupBy(u => u.TravelerIndex).Count() > 1 ? " Travelers" : " Traveler"),
                        SeatCode = t.Select(u => u.SeatPromotionCode).FirstOrDefault(),
                        FlightNumber = t.Select(x => x.FlightNumber).FirstOrDefault(),
                        // DepartureTime = flightReservationResponse.Reservation.FlightSegments.Where(s => s.FlightSegment == ),
                        //                          | --Getting the SeatDescription based on SeatPromotionCode from the list of DisplaySeats at LEVEL 2, TravelerIndex count for pluralizing the text. -- |
                        SegmentDescription = GetSeatTypeBasedonCode(t.Select(u => u.SeatPromotionCode).FirstOrDefault(), t.GroupBy(u => u.TravelerIndex).Count())
                        //             | -- Once Get the final list ordering them based on the order defined in GetSEatPriceOrder method comparing with list SegmentDescription -- |
                    }).ToList().OrderBy(p => ShopStaticUtility.GetSeatPriceOrder()[p.SegmentDescription]).ToList()
                }).ToList(),
            };

            if (state == null ? false : (state.TotalEplusEligible > 0))
            {
                foreach (var segmnt in prodDetail.Segments)
                {
                    var subSegments = segmnt.SubSegmentDetails;
                    foreach (var subSegment in subSegments)
                    {
                        if (state.Seats != null)
                        {
                            var sbSegments = state.Seats.Where(x => x.Origin == segmnt.SegmentInfo.Substring(0, 3) && x.Destination == segmnt.SegmentInfo.Substring(6, 3) && x.FlightNumber == subSegment.FlightNumber && (ShopStaticUtility.GetCommonSeatCode(x.ProgramCode) == subSegment.SeatCode)).ToList();
                            decimal totalPrice = sbSegments.Select(u => u.Price).ToList().Sum();
                            decimal discountPrice = sbSegments.Select(u => u.PriceAfterTravelerCompanionRules).ToList().Sum();
                            if (discountPrice < totalPrice)
                            {
                                subSegment.StrikeOffPrice = String.Format("{0:0.00}", sbSegments.Select(u => u.Price).ToList().Sum().ToString());
                                subSegment.DisplayStrikeOffPrice = Decimal.Parse(sbSegments.Select(u => u.Price).ToList().Sum().ToString()).ToString("c");
                            }
                            subSegment.Passenger = sbSegments.Count() + (sbSegments.Count() > 1 ? " Travelers" : " Traveler");
                            subSegment.Price = String.Format("{0:0.00}", sbSegments.Select(u => u.PriceAfterTravelerCompanionRules).ToList().Sum().ToString());
                            subSegment.DisplayPrice = Decimal.Parse(sbSegments.Select(u => u.PriceAfterTravelerCompanionRules).ToList().Sum().ToString()).ToString("c");
                        }
                    }
                }
            }
            //Mobile-1855: Remove segments with no seats to purchase
            prodDetail.Segments.Select(x => x.SubSegmentDetails).ToList().ForEach(item => item.RemoveAll(k => k.Price == "0" && (k.StrikeOffPrice == "0" || k.StrikeOffPrice == string.Empty)));
            prodDetail.Segments.RemoveAll(k => k.SubSegmentDetails.Count == 0);
            return prodDetail;
        }


        public bool IsEnableOmniCartMVP2Changes(int applicationId, string appVersion, bool isDisplayCart)
        {
            if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableOmniCartMVP2Changes_AppVersion"), _configuration.GetValue<string>("iPhone_EnableOmniCartMVP2Changes_AppVersion")))
            {
                return true;
            }
            return false;
        }

        private string BuildProductDescription(Collection<Services.FlightShopping.Common.DisplayCart.TravelOption> travelOptions, IGrouping<string, SubItem> t, string productCode)
        {
            if (string.IsNullOrEmpty(productCode))
                return string.Empty;

            productCode = productCode.ToUpper().Trim();

            if (productCode == "AAC")
                return "Award Accelerator®";

            if (productCode == "PAC")
                return "Premier Accelerator℠";

            if (productCode == "TPI" && _configuration.GetValue<bool>("GetTPIProductName_HardCode"))
                return "Travel insurance";
            if (productCode == "FARELOCK")
                return "FareLock";

            if (_configuration.GetValue<bool>("EnableBasicEconomyBuyOutInViewRes") && productCode == "BEB")
                return !_configuration.GetValue<bool>("EnableNewBEBContentChange") ? "Switch to Economy" : _configuration.GetValue<string>("BEBuyOutPaymentInformationMessage");

            if (productCode == "PCU")
                return GetFormattedCabinName(t.Select(u => u.Description).FirstOrDefault().ToString());


            return travelOptions.Where(d => d.Key == productCode).Select(d => d.Type).FirstOrDefault().ToString();
        }

        public void AddCouponDetails(List<ProdDetail> prodDetails, Services.FlightShopping.Common.FlightReservation.FlightReservationResponse cslFlightReservationResponse, bool isPost, string flow, MOBApplication application)
        {
            United.Service.Presentation.InteractionModel.ShoppingCart flightReservationResponseShoppingCart = new United.Service.Presentation.InteractionModel.ShoppingCart();
            flightReservationResponseShoppingCart = isPost ? cslFlightReservationResponse.CheckoutResponse.ShoppingCart : cslFlightReservationResponse.ShoppingCart;
            foreach (var prodDetail in prodDetails)
            {
                var product = flightReservationResponseShoppingCart.Items.SelectMany(I => I.Product).Where(p => p.Code == prodDetail.Code).FirstOrDefault();
                if (product != null && product.CouponDetails != null && product.CouponDetails.Any(c => c != null) && product.CouponDetails.Count() > 0)
                {
                    prodDetail.CouponDetails = new List<CouponDetails>();
                    foreach (var coupon in product.CouponDetails)
                    {
                        if (coupon != null)
                        {
                            prodDetail.CouponDetails.Add(new CouponDetails
                            {
                                PromoCode = coupon.PromoCode,
                                Product = coupon.Product,
                                IsCouponEligible = coupon.IsCouponEligible,
                                Description = coupon.Description,
                                DiscountType = coupon.DiscountType
                            });
                        }
                    }
                }
                if (flow == FlowType.POSTBOOKING.ToString() && prodDetail.CouponDetails != null && prodDetail.CouponDetails.Count > 0
                     || (flow == FlowType.BOOKING.ToString() && prodDetail.CouponDetails != null && prodDetail.CouponDetails.Count > 0 && IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true)) || (_configuration.GetValue<bool>("IsEnableManageResCoupon") && (flow == FlowType.VIEWRES.ToString() || flow == FlowType.VIEWRES_SEATMAP.ToString()) && prodDetail.CouponDetails != null && prodDetail.CouponDetails.Count > 0))
                {
                    AddPromoDetailsInSegments(prodDetail);
                }
            }
        }

        private bool IsOriginalPriceExists(ProdDetail prodDetail)
        {
            return !_configuration.GetValue<bool>("DisableFreeCouponFix")
                   && !string.IsNullOrEmpty(prodDetail.ProdOriginalPrice)
                   && Decimal.TryParse(prodDetail.ProdOriginalPrice, out decimal originalPrice)
                   && originalPrice > 0;
        }

        private List<SeatAssignment> HandleCSLDefect(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, List<SeatAssignment> fliterSeats, bool isPost)
        {
            if (fliterSeats == null || !fliterSeats.Any())
                return fliterSeats;

            fliterSeats = fliterSeats.Where(s => s != null && s.OriginalSegmentIndex != 0 && !string.IsNullOrEmpty(s.DepartureAirportCode) && !string.IsNullOrEmpty(s.ArrivalAirportCode)).ToList();

            if (fliterSeats == null || !fliterSeats.Any())
                return fliterSeats;

            if (flightReservationResponse.Errors != null &&
                flightReservationResponse.Errors.Any(e => e != null && e.MinorCode == "90584") &&
                flightReservationResponse.DisplayCart.DisplaySeats != null &&
                flightReservationResponse.DisplayCart.DisplaySeats.Any(s => s != null && s.PCUSeat) &&
                flightReservationResponse.DisplayCart.DisplaySeats.Any(s => s != null && !s.PCUSeat &&
                 CheckSeatAssignMessage(s.SeatAssignMessage, isPost)))
            {
                //take this from errors
                var item = flightReservationResponse.CheckoutResponse.ShoppingCartResponse.Items.Where(t => t.Item.Category == "Reservation.Reservation.SEATASSIGNMENTS").FirstOrDefault();
                if (item != null && item.Item != null && item.Item.Product != null && item.Item.Product.Any())
                {
                    var description = JsonConvert.DeserializeObject<Service.Presentation.FlightResponseModel.AssignTravelerSeat>(item.Item.Product.FirstOrDefault().Status.Description);
                    var unAssignedSeats = description.Travelers.SelectMany(t => t.Seats.Where(s => !string.IsNullOrEmpty(s.AssignMessage))).ToList();
                    if (unAssignedSeats != null && unAssignedSeats.Any())
                    {
                        return fliterSeats.Where(s => !ShopStaticUtility.IsFailedSeat(s, unAssignedSeats)).ToList();
                    }
                }
            }
            return fliterSeats;
        }

        public List<ProductSegmentDetail> BuildProductSegmentsForSeats(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, List<Seat> seats, MOBApplication application, string flow)
        {
            return flightReservationResponse.DisplayCart.DisplaySeats.OrderBy(d => d.OriginalSegmentIndex)
                                                        .GroupBy(d => new { d.OriginalSegmentIndex, d.LegIndex })
                                                        .Select(d => new ProductSegmentDetail
                                                        {
                                                            SegmentInfo = ShopStaticUtility.GetSegmentInfo(flightReservationResponse, d.Key.OriginalSegmentIndex, Convert.ToInt32(d.Key.LegIndex)),
                                                            SubSegmentDetails = d.GroupBy(s => ShopStaticUtility.GetSeatTypeForDisplay(s, flightReservationResponse.DisplayCart.TravelOptions))
                                                                                .Select(seatGroup => new ProductSubSegmentDetail
                                                                                {
                                                                                    SegmentInfo = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? ShopStaticUtility.GetSegmentInfo(flightReservationResponse, d.Key.OriginalSegmentIndex, Convert.ToInt32(d.Key.LegIndex)) : string.Empty,
                                                                                    OrginalPrice = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? String.Format("{0:0.00}", seatGroup.Select(s => s.OriginalPrice).ToList().Sum()) : string.Empty,
                                                                                    Price = String.Format("{0:0.00}", seatGroup.Select(s => s.SeatPrice).ToList().Sum()),
                                                                                    DisplayPrice = Decimal.Parse(seatGroup.Select(s => s.SeatPrice).ToList().Sum().ToString()).ToString("c"),
                                                                                    DisplayOriginalPrice = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? Decimal.Parse(seatGroup.Select(s => s.OriginalPrice).ToList().Sum().ToString()).ToString("c") : string.Empty,
                                                                                    StrikeOffPrice = ShopStaticUtility.GetOriginalTotalSeatPriceForStrikeOff(seatGroup.ToList(), seats),
                                                                                    DisplayStrikeOffPrice = ShopStaticUtility.GetFormatedDisplayPriceForSeats(ShopStaticUtility.GetOriginalTotalSeatPriceForStrikeOff(seatGroup.ToList(), seats)),
                                                                                    Passenger = seatGroup.Count().ToString() + (seatGroup.Count() > 1 ? " Travelers" : " Traveler"),
                                                                                    SeatCode = seatGroup.Key,
                                                                                    FlightNumber = seatGroup.Select(x => x.FlightNumber).FirstOrDefault(),
                                                                                    SegmentDescription = GetSeatTypeBasedonCode(seatGroup.Key, seatGroup.Count()),

                                                                                    PaxDetails = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? GetPaxDetails(seatGroup, flightReservationResponse, flow, application) : null,

                                                                                    ProductDescription = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? ShopStaticUtility.GetSeatDescription(seatGroup.Key) : string.Empty
                                                                                }).ToList().OrderBy(p => ShopStaticUtility.GetSeatPriceOrder()[p.SegmentDescription]).ToList()
                                                        }).ToList();
        }
        public List<MOBPaxDetails> GetPaxDetails(IGrouping<string, SeatAssignment> t, FlightReservationResponse response, string flow, MOBApplication application)
        {
            List<MOBPaxDetails> paxDetails = new List<MOBPaxDetails>();
            if (response?.Reservation?.Travelers != null)
            {
                bool isExtraSeatEnabled = application != null && IsExtraSeatFeatureEnabled(application.Id, application?.Version?.Major) && flow.ToString() == FlowType.BOOKING.ToString();
                var extraSeatPassengerIndex = GetTravelerNameIndexForExtraSeat(isExtraSeatEnabled, response.Reservation.Services);

                t.ForEach(seat =>
                {
                    var traveler = response.Reservation.Travelers.Where(passenger => passenger.Person != null && passenger.Person.Key == seat.PersonIndex).FirstOrDefault();
                    if (traveler != null && (seat.SeatPrice > 0 || seat.OriginalPrice > 0)) // Added OriginalPrice check as well to handle coupon applied sceanrios where seat price can be 0 but we have original price
                    {
                        paxDetails.Add(new MOBPaxDetails
                        {
                            FullName = PaxName(traveler, isExtraSeatEnabled, extraSeatPassengerIndex),
                            Key = seat.PersonIndex,
                            Seat = seat.Seat

                        });
                    }

                });
            }
            return paxDetails;
        }

        public string PaxName(United.Service.Presentation.ReservationModel.Traveler traveler, bool isExtraSeatEnabled, List<string> extraSeatPassengerIndex)
        {
            if (isExtraSeatEnabled && extraSeatPassengerIndex?.Count > 0 && !string.IsNullOrEmpty(traveler?.Person?.Key) && extraSeatPassengerIndex.Contains(traveler.Person.Key))
            {
                string travelerMiddleInitial = !string.IsNullOrEmpty(traveler.Person?.MiddleName) ? " " + traveler.Person.MiddleName.Substring(0, 1) : string.Empty;
                string travelerSuffix = !string.IsNullOrEmpty(traveler.Person?.Suffix) ? " " + traveler.Person.Suffix : string.Empty;

                return _configuration.GetValue<string>("ExtraSeatName") + " (" + GetGivenNameForExtraSeat(traveler.Person?.GivenName) + travelerMiddleInitial + " " + traveler.Person?.Surname + travelerSuffix + ")";
            }
            else
                return traveler.Person.GivenName + " " + traveler.Person.Surname;
        }
        public bool IsExtraSeatFeatureEnabled(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableExtraSeatsFeature")
                        && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "Android_EnableExtraSeatsFeature_AppVersion", "IPhone_EnableExtraSeatsFeature_AppVersion", "", "", true, _configuration);
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

        public string GetGivenNameForExtraSeat(string givenName)
        {
            if (!string.IsNullOrEmpty(givenName))
            {
                if (givenName.Contains(EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXSTTWO.ToString()))
                    return givenName.Remove(0, EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXSTTWO.ToString().Length);
                else if (givenName.Contains(EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBGTWO.ToString()))
                    return givenName.Remove(0, EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBGTWO.ToString().Length);
                else if (givenName.Contains(EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXST.ToString()))
                    return givenName.Remove(0, EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXST.ToString().Length);
                else if (givenName.Contains(EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBG.ToString()))
                    return givenName.Remove(0, EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBG.ToString().Length);
            }

            return givenName;
        }

        public static List<string> GetPassengerNamesRemove(IGrouping<string, SeatAssignment> t, FlightReservationResponse response)
        {
            List<string> passengerNames = new List<string>();
            if (response?.Reservation?.Travelers != null)
            {
                t.ForEach(seat =>
                {
                    var traveler = response.Reservation.Travelers.Where(passenger => passenger.Person != null && passenger.Person.Key == seat.PersonIndex).FirstOrDefault();
                    if (traveler != null)
                    {
                        passengerNames.Add(traveler.Person.GivenName + " " + traveler.Person.Surname);
                    }

                });
            }
            return passengerNames;
        }
        private bool CheckSeatAssignMessage(string seatAssignMessage, bool isPost)
        {
            if (_configuration.GetValue<bool>("EnableCSL30ManageResSelectSeatMap") && isPost)
            {
                return !string.IsNullOrEmpty(seatAssignMessage) && seatAssignMessage.Equals("SEATS ASSIGNED", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return string.IsNullOrEmpty(seatAssignMessage);
            }
        }

        private List<ProductSegmentDetail> BuildProductSegmentsForSeats(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, List<Seat> seats, List<MOBBKTraveler> BookingTravelerInfo, bool isPost, string flow)
        {
            if (flightReservationResponse.DisplayCart.DisplaySeats == null || !flightReservationResponse.DisplayCart.DisplaySeats.Any())
                return null;

            var displaySeats = flightReservationResponse.DisplayCart.DisplaySeats.Clone();
            List<string> refundedSegmentNums = null;
            if (flightReservationResponse.Errors != null && flightReservationResponse.Errors.Any(e => e != null && e.MinorCode == "90506"))
            {
                bool DisableFixForPCUPurchaseFailMsg_MOBILE15837 = _configuration.GetValue<bool>("DisableFixForPCUPurchaseFailMsg_MOBILE15837");
                var isRefundSuccess = ShopStaticUtility.IsRefundSuccess(flightReservationResponse.CheckoutResponse.ShoppingCartResponse.Items, out refundedSegmentNums, DisableFixForPCUPurchaseFailMsg_MOBILE15837);
                //Remove pcu seats if refund Failed
                if (!isRefundSuccess)
                {
                    displaySeats.RemoveAll(ds => ds.PCUSeat);
                }
                if (!displaySeats.Any())
                    return null;
            }

            //Remove all failed seats other than pcu seats.
            displaySeats.RemoveAll(ds => !ds.PCUSeat && !CheckSeatAssignMessage(ds.SeatAssignMessage, isPost)); // string.IsNullOrEmpty(ds.SeatAssignMessage)
            if (_configuration.GetValue<bool>("EnablePCUFromSeatMapErrorCheckViewRes"))
            {
                displaySeats = HandleCSLDefect(flightReservationResponse, displaySeats, isPost);
            }
            if (!displaySeats.Any())
                return null;

            return displaySeats.OrderBy(d => d.OriginalSegmentIndex)
                                .GroupBy(d => new { d.OriginalSegmentIndex, d.LegIndex })
                                .Select(d => new ProductSegmentDetail
                                {
                                    SegmentInfo = ShopStaticUtility.GetSegmentInfo(flightReservationResponse, d.Key.OriginalSegmentIndex, Convert.ToInt32(d.Key.LegIndex)),
                                    SubSegmentDetails = d.GroupBy(s => ShopStaticUtility.GetSeatTypeForDisplay(s, flightReservationResponse.DisplayCart.TravelOptions))
                                                        .Select(seatGroup => new ProductSubSegmentDetail
                                                        {
                                                            Price = String.Format("{0:0.00}", seatGroup.Select(s => s.SeatPrice).ToList().Sum()),
                                                            OrginalPrice = _configuration.GetValue<bool>("IsEnableManageResCoupon") ? String.Format("{0:0.00}", seatGroup.Select(s => s.OriginalPrice).ToList().Sum()) : string.Empty,
                                                            DisplayPrice = Decimal.Parse(seatGroup.Select(s => s.SeatPrice).ToList().Sum().ToString()).ToString("c"),
                                                            DisplayOriginalPrice = _configuration.GetValue<bool>("IsEnableManageResCoupon") ? Decimal.Parse(seatGroup.Select(s => s.OriginalPrice).ToList().Sum().ToString()).ToString("c") : string.Empty,
                                                            StrikeOffPrice = ShopStaticUtility.GetOriginalTotalSeatPriceForStrikeOff(seatGroup.ToList(), seats, BookingTravelerInfo),
                                                            DisplayStrikeOffPrice = ShopStaticUtility.GetFormatedDisplayPriceForSeats(ShopStaticUtility.GetOriginalTotalSeatPriceForStrikeOff(seatGroup.ToList(), seats, BookingTravelerInfo)),
                                                            Passenger = seatGroup.Count().ToString() + (seatGroup.Count() > 1 ? " Travelers" : " Traveler"),
                                                            SeatCode = seatGroup.Key,
                                                            FlightNumber = seatGroup.Select(x => x.FlightNumber).FirstOrDefault(),
                                                            SegmentDescription = GetSeatTypeBasedonCode(seatGroup.Key, seatGroup.Count()),
                                                            IsPurchaseFailure = ShopStaticUtility.IsPurchaseFailed(seatGroup.Any(s => s.PCUSeat), d.Key.OriginalSegmentIndex.ToString(), refundedSegmentNums),
                                                            Miles = IsMilesFOPEnabled() ? seatGroup.Any(s => s.PCUSeat == true) ? 0 : _configuration.GetValue<int>("milesFOP") : 0,
                                                            DisplayMiles = IsMilesFOPEnabled() ? seatGroup.Any(s => s.PCUSeat == true) ? string.Empty : ShopStaticUtility.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false) : string.Empty,
                                                            StrikeOffMiles = IsMilesFOPEnabled() ? seatGroup.Any(s => s.PCUSeat == true) ? 0 : Convert.ToInt32(_configuration.GetValue<string>("milesFOP")) : 0,
                                                            DisplayStrikeOffMiles = IsMilesFOPEnabled() ? seatGroup.Any(s => s.PCUSeat == true) ? string.Empty : ShopStaticUtility.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false) : string.Empty
                                                        }).ToList().OrderBy(p => ShopStaticUtility.GetSeatPriceOrder()[p.SegmentDescription]).ToList()
                                }).ToList();
        }

        private bool IsMilesFOPEnabled()
        {
            Boolean isMilesFOP;
            Boolean.TryParse(_configuration.GetValue<string>("EnableMilesAsPayment"), out isMilesFOP);
            return isMilesFOP;
        }

        private List<ProductSegmentDetail> GetProductSegmentForInFlightMeals(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse,
  List<MOBInFlightMealsRefreshmentsResponse> savedResponse, Collection<Services.FlightShopping.Common.DisplayCart.TravelOption> travelOptions, United.Service.Presentation.InteractionModel.ShoppingCart flightReservationResponseShoppingCart)
        {
            List<ProductSegmentDetail> response = new List<ProductSegmentDetail>();
            ProductSegmentDetail segmentDetail = new ProductSegmentDetail();
            List<ProductSubSegmentDetail> subSegmentDetails = new List<ProductSubSegmentDetail>();
            var traveler = flightReservationResponse?.Reservation?.Travelers;
            string productCode = _configuration.GetValue<string>("InflightMealProductCode");

            var subProducts = flightReservationResponseShoppingCart.Items
           ?.Where(a => a.Product != null)
           ?.SelectMany(b => b.Product)
           ?.Where(c => c.SubProducts != null && c.SubProducts.Any(d => d.Code == _configuration.GetValue<string>("InflightMealProductCode")))
           ?.SelectMany(d => d.SubProducts);

            var characterStics = flightReservationResponseShoppingCart.Items
           ?.Where(a => a.Product != null)
           ?.SelectMany(b => b.Product)
           ?.Where(c => c.Code == productCode)
           ?.SelectMany(d => d.Characteristics)
           ?.Where(e => e.Code == "SegTravProdSubGroupIDQtyPrice")
           ?.FirstOrDefault();

            string[] items = characterStics.Value.Split(',');
            List<Tuple<string, string, int, string>> tupleList = new List<Tuple<string, string, int, string>>();

            if (items != null && items.Length > 0)
            {
                string[] selectedItems = null;
                foreach (var item in items)
                {
                    //segmentID - TravelerID - ProductID - SubGroupID - Quantity - Price
                    if (item != "")
                        selectedItems = item.Split('|');
                    if (selectedItems != null && selectedItems.Length > 0)
                    {
                        //TravelerID - ProductID - SubGroupID - Quantity - Price
                        tupleList.Add(Tuple.Create(selectedItems[2], selectedItems[3], Convert.ToInt32(selectedItems[4]), selectedItems[5]));
                    }
                }
            }
            for (int i = 0; i < flightReservationResponse.Reservation.Travelers.Count; i++)
            {
                if (response.Count == 0)
                    segmentDetail.SegmentInfo = ShopStaticUtility.GetSegmentDescription(travelOptions);
                List<ProductSubSegmentDetail> snackDetails = new List<ProductSubSegmentDetail>();
                int travelerCouter = 0;
                int prodCounter = 0;
                foreach (var subProduct in subProducts)
                {
                    ProductSubSegmentDetail segDetail = new ProductSubSegmentDetail();
                    if (subProduct.Prices.Where(a => a.Association.TravelerRefIDs[0] == (i + 1).ToString()).Any())
                    {
                        if (subProduct != null && subProduct.Extension != null)
                        {
                            var priceInfo = subProduct.Prices.Where(a => a.Association.TravelerRefIDs[0] == (i + 1).ToString()).FirstOrDefault();
                            double price = priceInfo.PaymentOptions.FirstOrDefault().PriceComponents.FirstOrDefault().Price.Totals.FirstOrDefault().Amount;
                            var tupleSelectedItem = tupleList.Where(a => a.Item2 == subProduct.SubGroupCode && a.Item1 == priceInfo.ID).FirstOrDefault();

                            if (tupleSelectedItem != null)
                            {
                                if (_configuration.GetValue<bool>("EnableisEditablePOMFeature"))
                                {
                                    if (price > 0 && subProduct.Extension.MealCatalog?.MealShortDescription != null)
                                    {
                                        if (prodCounter == 0 && travelerCouter == 0)
                                        {
                                            segDetail.Passenger = traveler[i].Person.GivenName.ToLower().ToPascalCase() + " " + traveler[i].Person.Surname.ToLower().ToPascalCase();
                                            segDetail.Price = "0";
                                            snackDetails.Add(segDetail);
                                            segDetail = new ProductSubSegmentDetail();

                                            segDetail.SegmentDescription = subProduct.Extension.MealCatalog?.MealShortDescription + " x " + tupleSelectedItem.Item3;
                                            segDetail.DisplayPrice = "$" + String.Format("{0:0.00}", price * tupleSelectedItem.Item3);
                                            segDetail.Price = price.ToString();
                                        }
                                        else
                                        {
                                            segDetail.SegmentDescription = subProduct.Extension.MealCatalog?.MealShortDescription + " x " + tupleSelectedItem.Item3;
                                            segDetail.DisplayPrice = "$" + String.Format("{0:0.00}", price * tupleSelectedItem.Item3);
                                            segDetail.Price = price.ToString();
                                        }
                                        prodCounter++;
                                        snackDetails.Add(segDetail);
                                    }
                                }
                                else
                                {
                                    //  int quantity = GetQuantity(travelOptions, subProduct.SubGroupCode, subProduct.Prices.Where(a=>a.ID == (i+1).ToString()).Select(b=>b.ID).ToString());
                                    if (prodCounter == 0 && travelerCouter == 0)
                                    {
                                        segDetail.Passenger = traveler[i].Person.GivenName.ToLower().ToPascalCase() + " " + traveler[i].Person.Surname.ToLower().ToPascalCase();
                                        segDetail.Price = "0";
                                        snackDetails.Add(segDetail);
                                        segDetail = new ProductSubSegmentDetail();

                                        segDetail.SegmentDescription = subProduct.Extension.MealCatalog?.MealShortDescription + " x " + tupleSelectedItem.Item3;
                                        segDetail.DisplayPrice = "$" + String.Format("{0:0.00}", price * tupleSelectedItem.Item3);
                                        segDetail.Price = price.ToString();
                                    }
                                    else
                                    {
                                        segDetail.SegmentDescription = subProduct.Extension.MealCatalog?.MealShortDescription + " x " + tupleSelectedItem.Item3;
                                        segDetail.DisplayPrice = "$" + String.Format("{0:0.00}", price * tupleSelectedItem.Item3);
                                        segDetail.Price = price.ToString();
                                    }
                                    prodCounter++;
                                    snackDetails.Add(segDetail);
                                }
                            }

                        }
                    }

                }
                if (segmentDetail.SubSegmentDetails == null) segmentDetail.SubSegmentDetails = new List<ProductSubSegmentDetail>();
                if (snackDetails != null)
                    segmentDetail.SubSegmentDetails.AddRange(snackDetails);
                travelerCouter++;

            }
            if (segmentDetail != null && segmentDetail.SubSegmentDetails != null && !response.Contains(segmentDetail))
                response.Add(segmentDetail);
            return response;
        }

        private List<ProductSegmentDetail> BuildCheckinSegmentDetail(IEnumerable<IGrouping<string, SeatAssignment>> seatAssignmentGroup)
        {
            List<ProductSegmentDetail> segmentDetails = new List<ProductSegmentDetail>();
            seatAssignmentGroup.ForEach(seatSegment => segmentDetails.Add(new ProductSegmentDetail()
            {
                SegmentInfo = seatSegment.Key,
                SubSegmentDetails = BuildSubsegmentDetails(seatSegment.ToList()).OrderBy(p => ShopStaticUtility.GetSeatPriceOrder()[p.SegmentDescription]).ToList()
            }));
            return segmentDetails;
        }

        private string GetSeatTypeBasedonCode(string seatCode, int travelerCount, bool isCheckinPath = false)
        {
            string seatType = string.Empty;

            switch (seatCode.ToUpper().Trim())
            {
                case "SXZ": //StandardPreferredExitPlus
                case "SZX": //StandardPreferredExit
                case "SBZ": //StandardPreferredBlukheadPlus
                case "SZB": //StandardPreferredBlukhead
                case "SPZ": //StandardPreferredZone
                case "PZA":
                    seatType = (travelerCount > 1) ? "Preferred seats" : "Preferred seat";
                    break;
                case "SXP": //StandardPrimeExitPlus
                case "SPX": //StandardPrimeExit
                case "SBP": //StandardPrimeBlukheadPlus
                case "SPB": //StandardPrimeBlukhead
                case "SPP": //StandardPrimePlus
                case "PPE": //StandardPrime
                case "BSA":
                case "ASA":
                    if (isCheckinPath)
                        seatType = (travelerCount > 1) ? "Seat assignments" : "Seat assignment";
                    else
                        seatType = (travelerCount > 1) ? "Advance seat assignments" : "Advance seat assignment";
                    break;
                case "EPL": //EplusPrime
                case "EPU": //EplusPrimePlus
                case "BHS": //BulkheadPrime
                case "BHP": //BulkheadPrimePlus  
                case "PSF": //PrimePlus  
                    seatType = (travelerCount > 1) ? "Economy Plus Seats" : "Economy Plus Seat";
                    break;
                case "PSL": //Prime                            
                    seatType = (travelerCount > 1) ? "Economy Plus Seats (limited recline)" : "Economy Plus Seat (limited recline)";
                    break;
                default:
                    var pcuCabinName = GetFormattedCabinName(seatCode);
                    if (!string.IsNullOrEmpty(pcuCabinName))
                    {
                        return pcuCabinName + ((travelerCount > 1) ? " Seats" : " Seat");
                    }
                    return string.Empty;
            }
            return seatType;
        }

        private string GetFormattedCabinName(string cabinName)
        {
            if (!_configuration.GetValue<bool>("EnablePcuMultipleUpgradeOptions"))
            {
                return cabinName;
            }

            if (string.IsNullOrWhiteSpace(cabinName))
                return string.Empty;

            switch (cabinName.ToUpper().Trim())
            {
                case "UNITED FIRST":
                    return "United First®";
                case "UNITED BUSINESS":
                    return "United Business®";
                case "UNITED POLARIS FIRST":
                    return "United Polaris℠ first";
                case "UNITED POLARIS BUSINESS":
                    return "United Polaris℠ business";
                case "UNITED PREMIUM PLUS":
                    return "United® Premium Plus";
                default:
                    return string.Empty;
            }
        }

        private void AddPromoDetailsInSegments(ProdDetail prodDetail)
        {
            if (prodDetail?.Segments != null)
            {
                double promoValue;
                prodDetail?.Segments.ForEach(p =>
                {
                    p.SubSegmentDetails.ForEach(subSegment =>
                    {
                        if (!string.IsNullOrEmpty(subSegment.OrginalPrice) && !string.IsNullOrEmpty(subSegment.Price))
                        {
                            promoValue = Convert.ToDouble(subSegment.OrginalPrice) - Convert.ToDouble(subSegment.Price);
                            subSegment.Price = subSegment.OrginalPrice;
                            subSegment.DisplayPrice = Decimal.Parse(subSegment.Price).ToString("c");
                            if (promoValue > 0)
                            {
                                subSegment.PromoDetails = new Mobile.Model.Common.MOBPromoCode
                                {
                                    PriceTypeDescription = _configuration.GetValue<string>("PromoCodeAppliedText"),
                                    PromoValue = Math.Round(promoValue, 2, MidpointRounding.AwayFromZero),
                                    FormattedPromoDisplayValue = "-" + promoValue.ToString("C2", CultureInfo.CurrentCulture)
                                };
                            }
                        }
                    });

                });
            }
        }

        private List<ProductSubSegmentDetail> BuildSubsegmentDetails(List<SeatAssignment> seatAssignments)
        {
            List<ProductSubSegmentDetail> subSegmentDetails = new List<ProductSubSegmentDetail>();
            var groupedByTypeAndPrice = seatAssignments.GroupBy(s => s.SeatType, (key, grpSeats) => new { SeatType = key, OriginalPrice = grpSeats.Sum(x => x.OriginalPrice), SeatPrice = grpSeats.Sum(x => x.SeatPrice), Count = grpSeats.Count() });

            groupedByTypeAndPrice.ForEach(grpSeats =>
            {
                subSegmentDetails.Add(PopulateSubsegmentDetails(grpSeats.SeatType, grpSeats.OriginalPrice, grpSeats.SeatPrice, grpSeats.Count));
            });
            return subSegmentDetails;
        }

        private ProductSubSegmentDetail PopulateSubsegmentDetails(string seatType, decimal originalPrice, decimal seatPrice, int count)
        {
            ProductSubSegmentDetail subsegmentDetail = new ProductSubSegmentDetail();
            subsegmentDetail.Price = String.Format("{0:0.00}", seatPrice);
            subsegmentDetail.DisplayPrice = $"${subsegmentDetail.Price}";
            if (originalPrice > seatPrice)
            {
                subsegmentDetail.StrikeOffPrice = String.Format("{0:0.00}", originalPrice);
                subsegmentDetail.DisplayStrikeOffPrice = $"${subsegmentDetail.StrikeOffPrice}";
            }
            subsegmentDetail.Passenger = $"{count} Traveler{(count > 1 ? "s" : String.Empty)}";
            subsegmentDetail.SegmentDescription = GetSeatTypeBasedonCode(seatType, count, true);
            return subsegmentDetail;
        }

        private List<string> OrderPCUTnC(List<string> productCodes)
        {
            if (productCodes == null || !productCodes.Any())
                return productCodes;

            return productCodes.OrderBy(p => GetProductOrderTnC()[GetProductTnCtoOrder(p)]).ToList();
        }

        private async Task<List<MOBMobileCMSContentMessages>> GetTermsAndConditions()
        {
            var cmsContentMessages = new List<MOBMobileCMSContentMessages>();
            var docKeys = "PCU_TnC";
            var docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(docKeys, _headers.ContextValues.TransactionId, true).ConfigureAwait(false);
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

        private async Task<List<MOBMobileCMSContentMessages>> GetTermsAndConditions(bool hasPremierAccelerator)
        {

            var dbKey = _configuration.GetValue<bool>("EnablePPRChangesForAAPA") ? hasPremierAccelerator ? "PPR_AAPA_TERMS_AND_CONDITIONS_AA_PA_MP"
                                              : "PPR_AAPA_TERMS_AND_CONDITIONS_AA_MP" : hasPremierAccelerator ? "AAPA_TERMS_AND_CONDITIONS_AA_PA_MP"
                                              : "AAPA_TERMS_AND_CONDITIONS_AA_MP";

            var docs =await _documentLibraryDynamoDB.GetNewLegalDocumentsForTitles(new List<string> { dbKey }, _headers.ContextValues.SessionId).ConfigureAwait(false);
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
        public async Task<List<MOBItem>> GetCaptions(string key)
        {
            return !string.IsNullOrEmpty(key) ? await GetCaptions(key, true) : null;
        }
        private async Task<List<MOBItem>> GetCaptions(string keyList, bool isTnC)
        {
            var docs =await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(keyList, _headers.ContextValues.TransactionId, isTnC).ConfigureAwait(false);
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

        private bool IsBundleProductSelected(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse)
        {
            if (!_configuration.GetValue<bool>("EnableTravelOptionsBundleInViewRes"))
                return false;

            return flightReservationResponse?.ShoppingCart?.Items?.Where(x => x.Product?.FirstOrDefault()?.Code != "RES")?.Any(x => x.Product?.Any(p => p?.SubProducts?.Any(sp => sp?.GroupCode == "BE") ?? false) ?? false) ?? false;
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

        private async Task<string> GetBundleTermsandConditons(string databaseKeys)
        {
            List<string> docTitles = new List<string>() { "bundlesTermsandConditons" };
            var docs = await _documentLibraryDynamoDB.GetNewLegalDocumentsForTitles(docTitles, _headers.ContextValues.SessionId).ConfigureAwait(false);

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

        private bool IsPurchaseFailed(bool isPCUProduct, string originalSegmentIndex, List<string> refundedSegmentNums)
        {
            if (!isPCUProduct)
                return false;

            if (refundedSegmentNums == null || !refundedSegmentNums.Any() || string.IsNullOrEmpty(originalSegmentIndex))
                return false;

            return refundedSegmentNums.Any(s => s == originalSegmentIndex);
        }

        //public List<MOBTypeOption> GetBagsLineItems(MOBBag bagCount)
        //{
        //    List<MOBTypeOption> lineItems = new List<MOBTypeOption>();

        //    lineItems.Add(new MOBTypeOption() { Key = "Total bags:", Value = $"{bagCount.TotalBags} bag{(bagCount.TotalBags > 1 ? "s" : String.Empty)}" });

        //    if (bagCount.OverWeightBags > 0)
        //        lineItems.Add(new MOBTypeOption() { Key = "Bags over 50 lbs:", Value = $"{bagCount.OverWeightBags} bag{(bagCount.OverWeightBags > 1 ? "s" : String.Empty)}" });

        //    if (bagCount.OverWeight2Bags > 0)
        //        lineItems.Add(new MOBTypeOption() { Key = "Bags over 70 lbs:", Value = $"{bagCount.OverWeight2Bags} bag{(bagCount.OverWeight2Bags > 1 ? "s" : String.Empty)}" });

        //    if (bagCount.OverSizeBags > 0)
        //        lineItems.Add(new MOBTypeOption() { Key = "Bags over 60 in:", Value = $"{bagCount.OverSizeBags} bag{(bagCount.OverSizeBags > 1 ? "s" : String.Empty)}" });

        //    int totalExceptionBags = bagCount.ExceptionItemHockeySticks + bagCount.ExceptionItemInfantSeat + bagCount.ExceptionItemInfantStroller + bagCount.ExceptionItemSkiBoots + bagCount.ExceptionItemWheelChair;
        //    if (totalExceptionBags > 0)
        //        lineItems.Add(new MOBTypeOption() { Key = "Exception items:", Value = $"{totalExceptionBags} bag{(totalExceptionBags > 1 ? "s" : String.Empty)}" });

        //    return lineItems;
        //}

        private String GetPassengerText(String flow, string productCode, int travelerCount, int reservationTravelerCount)
        {
            //required for SDC since eservice and shopping cart do not return price per passenger so existing logic doesnt work for getting the traveler count
            int numTravelers = United.Common.Helper.UtilityHelper.IsCheckinFlow(flow) && productCode == "SDC" ? reservationTravelerCount : travelerCount;
            return $"{numTravelers} Traveler{(numTravelers > 1 ? "s" : String.Empty)}";
        }

        private string BuildSegmentInfo(string productCode, Collection<ReservationFlightSegment> flightSegments, IGrouping<string, SubItem> x, bool isBundleProduct = false)
        {
            if (productCode == "AAC" || productCode == "PAC" || productCode == "BAG" || productCode == "SDC")
                return string.Empty;

            if (isBundleProduct)
            {
                var subItem = x.FirstOrDefault();
                var segmentInfo = string.Join(" - ", flightSegments?.FirstOrDefault(f => string.Equals(f.TripNumber, subItem?.TripIndex))?.FlightSegment?.DepartureAirport?.IATACode
                , flightSegments?.LastOrDefault(l => string.Equals(l.TripNumber, subItem?.TripIndex))?.FlightSegment?.ArrivalAirport?.IATACode);
                return segmentInfo;
            }

            return flightSegments.Where(y => y.FlightSegment.SegmentNumber == Convert.ToInt32(x.Select(u => u.SegmentNumber).FirstOrDefault())).Select(y => y.FlightSegment.DepartureAirport.IATACode + " - " + y.FlightSegment.ArrivalAirport.IATACode).FirstOrDefault().ToString();
        }
    }
}
