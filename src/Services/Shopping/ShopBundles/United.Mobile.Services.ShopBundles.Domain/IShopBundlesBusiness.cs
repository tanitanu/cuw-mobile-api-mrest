using System.Threading.Tasks;
using United.Mobile.Model.Shopping.Bundles;

namespace United.Mobile.Services.ShopBundles.Domain
{
    public interface IShopBundlesBusiness
    {
        Task<BookingBundlesResponse> GetBundles_CFOP(BookingBundlesRequest bookingBundlesRequest );
    }
}
