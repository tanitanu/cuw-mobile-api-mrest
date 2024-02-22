using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.Model.Common;

namespace United.Common.Helper
{
    public interface IFeatureToggles
    {
        Task<bool> IsEnableConfirmationCTOTile(int applicationId, string appVersion);
        Task<bool> IsEnableTaxIdCollectionForLATIDCountries(int applicationId, string appVersion, List<MOBItem> catalogItems = null);
        Task<bool> IsEnabledTripInsuranceV2(int applicationId, string appVersion, List<MOBItem> catalogItems = null);
        Task<bool> IsEnableCustomerFacingCartId(int applicationId, string appVersion, List<MOBItem> catalogItems = null);
        Task<bool> IsEnableWheelchairFilterOnFSR(int applicationId, string appVersion, List<MOBItem> catalogItems = null);
        Task<bool> IsEnableMultiCityEditSearchOnFSRBooking(int applicationId, string appVersion, List<MOBItem> catalogItems);
        Task<bool> IsEnableU4BForMultipax(int applicationId, string appVersion, List<MOBItem> catalogItems);
        Task<bool> IsEnableVerticalSeatMapInBooking(int applicationId, string appVersion, List<MOBItem> catalogItems = null);
        Task<bool> IsEnableReshopWheelchairFilterOnFSR(int applicationId, string appVersion, List<MOBItem> catalogItems = null);
    }
}
