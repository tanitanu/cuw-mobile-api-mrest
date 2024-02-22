using System;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper.Shopping;
using United.Service.Presentation.ProductResponseModel;
using MOBOfferTile = United.Mobile.Model.Common.MOBOfferTile;

namespace United.Common.Helper.Merchandize
{
    public class MileageAndStatusOptions
    {
        private readonly ProductOffer _offerResponse;
        private readonly string _sessionId;
        public MOBOfferTile OfferTile;
        private readonly IProductInfoHelper _productInfoHelper;

        public MileageAndStatusOptions(ProductOffer offerResponse
            , string sessionId
            , IProductInfoHelper productInfoHelper)
        {
            _offerResponse = offerResponse;
            _sessionId = sessionId;
            _productInfoHelper = productInfoHelper;
        }

        public async Task<MileageAndStatusOptions> BuildOfferTile()
        {
            if (_offerResponse == null || _offerResponse.Offers == null || !_offerResponse.Offers.Any())
                return this;

            var productDetail = _offerResponse.Offers.FirstOrDefault().ProductInformation.ProductDetails.FirstOrDefault(p => p != null && p.Product != null && p.Product.Code == "AAC");
            if (productDetail != null)
            {
                var subproductWithPrices = productDetail.Product.SubProducts.Where(sp => sp.Prices != null &&
                                                                                         sp.Prices.Any() &&
                                                                                         sp.Prices.FirstOrDefault().PaymentOptions != null &&
                                                                                         sp.Prices.FirstOrDefault().PaymentOptions.Any() &&
                                                                                         sp.Prices.FirstOrDefault().PaymentOptions.FirstOrDefault().PriceComponents != null &&
                                                                                         sp.Prices.FirstOrDefault().PaymentOptions.FirstOrDefault().PriceComponents.Any() &&
                                                                                         sp.Prices.FirstOrDefault().PaymentOptions.FirstOrDefault().PriceComponents.FirstOrDefault().Price != null &&
                                                                                         sp.Prices.FirstOrDefault().PaymentOptions.FirstOrDefault().PriceComponents.FirstOrDefault().Price.Totals != null &&
                                                                                         sp.Prices.FirstOrDefault().PaymentOptions.FirstOrDefault().PriceComponents.FirstOrDefault().Price.Totals.FirstOrDefault() != null);
                if (subproductWithPrices != null && subproductWithPrices.Any())
                {
                    var price = subproductWithPrices.Min(sp => sp.Prices.FirstOrDefault().PaymentOptions.FirstOrDefault().PriceComponents.FirstOrDefault().Price.Totals.FirstOrDefault().Amount);
                    OfferTile = await BuildOfferTile(price, "AA_OfferTile");
                }
            }
            return this;
        }

        private async Task<MOBOfferTile> BuildOfferTile(double offerPrice, string captionKey, bool showUplift = false)
        {
            if (offerPrice <= 0 || string.IsNullOrWhiteSpace(captionKey))
                return null;

            var offerTileCaptions = await _productInfoHelper.GetCaptions(captionKey);
            var offerTile = new MOBOfferTile();
            offerTile.Price = (decimal)Math.Round(offerPrice);
            offerTile.ShowUpliftPerMonthPrice = showUplift;
            foreach (var caption in offerTileCaptions)
            {
                switch (caption.Id.ToUpper())
                {
                    case "TITLE":
                        offerTile.Title = caption.CurrentValue;
                        break;
                    case "TEXT1":
                        offerTile.Text1 = caption.CurrentValue;
                        break;
                    case "TEXT2":
                        offerTile.Text2 = caption.CurrentValue;
                        break;
                    case "TEXT3":
                        offerTile.Text3 = caption.CurrentValue;
                        break;
                    case "CURRENCYCODE":
                        offerTile.CurrencyCode = caption.CurrentValue;
                        break;
                }
            }

            return offerTile;
        }

    }

}
