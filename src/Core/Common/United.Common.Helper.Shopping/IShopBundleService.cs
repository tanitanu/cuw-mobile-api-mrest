using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.Model.Shopping.Bundles;

namespace United.Common.Helper.Shopping
{
    public interface IShopBundleService
    {
        Task<BookingBundlesResponse> GetBundleOffer(BookingBundlesRequest bundleRequest, bool throwExceptionWhenSaveOmniCartFlow = false);
    }
}
