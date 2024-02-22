using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Utility.Enum;
using United.Utility.Helper;

namespace United.Common.Helper
{
    public class FeatureToggles:IFeatureToggles
    {
        private readonly IFeatureSettings _featureSettings;
        private readonly IConfiguration _configuration;
        public FeatureToggles(IConfiguration configuration, IFeatureSettings featureSettings)
        {
            _featureSettings = featureSettings;
            _configuration = configuration;
        }
        public async Task<bool> IsEnableConfirmationCTOTile(int applicationId, string appVersion)
        {
            return (await _featureSettings.GetFeatureSettingValue("EnableConfirmationCTOTile").ConfigureAwait(false) &&
                GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Andriod_EnableConfirmationCTOTile_AppVersion"), _configuration.GetValue<string>("Iphone_EnableConfirmationCTOTile_AppVersion")));
        }

        public async Task<bool> IsEnableTaxIdCollectionForLATIDCountries(int applicationId, string appVersion, List<MOBItem> catalogItems = null)
        {
            return (await _featureSettings.GetFeatureSettingValue("EnableTaxIdChangesForLATIDCountries").ConfigureAwait(false) && (catalogItems != null && catalogItems.Count > 0 &&
            catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableTaxIdChangesForLATIDCountries).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableTaxIdChangesForLATIDCountries).ToString())?.CurrentValue == "1")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableTaxIdChangesForLATIDCountries_AppVersion"), _configuration.GetValue<string>("IPhone_EnableTaxIdChangesForLATIDCountries_AppVersion")));
        }

        public async Task<bool> IsEnabledTripInsuranceV2(int applicationId, string appVersion, List<MOBItem> catalogItems = null)
        {
            return (await _featureSettings.GetFeatureSettingValue("EnableTripInsuranceV2").ConfigureAwait(false) && (catalogItems != null && catalogItems.Count > 0 &&
            catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableTripInsuranceV2).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableTripInsuranceV2).ToString())?.CurrentValue == "1")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableTripInsuranceV2_AppVersion"), _configuration.GetValue<string>("IPhone_EnableTripInsuranceV2_AppVersion")));
        }

        public async Task<bool> IsEnableCustomerFacingCartId(int applicationId, string appVersion, List<MOBItem> catalogItems = null)
        {
            return (await _featureSettings.GetFeatureSettingValue("EnableCustomerFacingCartId").ConfigureAwait(false) && (catalogItems != null && catalogItems.Count > 0 &&
            catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableCustomerFacingCartId).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableCustomerFacingCartId).ToString())?.CurrentValue == "1")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableCustomerFacingCartId_AppVersion"), _configuration.GetValue<string>("IPhone_EnableCustomerFacingCartId_AppVersion")));
        }        
        public async Task<bool> IsEnableWheelchairFilterOnFSR(int applicationId, string appVersion, List<MOBItem> catalogItems = null)
        {
            return (await _featureSettings.GetFeatureSettingValue("EnableWheelchairFilterOnFSR").ConfigureAwait(false) &&
                (catalogItems != null && catalogItems.Count > 0 &&
            catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableWheelchairFilterOnFSR).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableWheelchairFilterOnFSR).ToString())?.CurrentValue == "1")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableWheelchairFilterOnFSR_AppVersion"), _configuration.GetValue<string>("IPhone_EnableWheelchairFilterOnFSR_AppVersion"))); 
        }
        public async Task<bool> IsEnableReshopWheelchairFilterOnFSR(int applicationId, string appVersion, List<MOBItem> catalogItems = null)
        {
            return (await _featureSettings.GetFeatureSettingValue("EnableReshopWheelchairFilterOnFSR").ConfigureAwait(false) &&
                (catalogItems != null && catalogItems.Count > 0 &&
            catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableReshopWheelchairFilterOnFSR).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableReshopWheelchairFilterOnFSR).ToString())?.CurrentValue == "1")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableReshopWheelchairFilterOnFSR_AppVersion"), _configuration.GetValue<string>("IPhone_EnableReshopWheelchairFilterOnFSR_AppVersion")));
        }
       
        public async Task<bool> IsEnableMultiCityEditSearchOnFSRBooking(int applicationId, string appVersion, List<MOBItem> catalogItems)
        {
            return (await _featureSettings.GetFeatureSettingValue("EnableMultiCityEditSearchOnFSRBooking").ConfigureAwait(false) && 
                   (catalogItems != null && catalogItems.Count > 0 && catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableMultiCityEditSearch).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableMultiCityEditSearch).ToString())?.CurrentValue == "1") &&
                GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Andriod_EnableMultiCityEditSearchOnFSRBooking_AppVersion"), _configuration.GetValue<string>("Iphone_EnableMultiCityEditSearchOnFSRBooking_AppVersion")));
        }
        public async Task<bool> IsEnableU4BForMultipax(int applicationId, string appVersion, List<MOBItem> catalogItems)
        {
            if (catalogItems==null)
            {
                return (await _featureSettings.GetFeatureSettingValue("EnableU4BForMultipax").ConfigureAwait(false) &&
               GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableU4BForMultipax_AppVersion"), _configuration.GetValue<string>("IPhone_EnableU4BForMultipax_AppVersion")));
            }
                return (await _featureSettings.GetFeatureSettingValue("EnableU4BForMultipax").ConfigureAwait(false) &&
                (catalogItems != null && catalogItems.Count > 0 && catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.ENABLE_U4B_FOR_MULTI_PAX).ToString() || a.Id == ((int)AndroidCatalogEnum.ENABLE_U4B_FOR_MULTI_PAX).ToString())?.CurrentValue == "1") &&
               GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableU4BForMultipax_AppVersion"), _configuration.GetValue<string>("IPhone_EnableU4BForMultipax_AppVersion")));
           
        }
        public async Task<bool> IsEnableVerticalSeatMapInBooking(int applicationId, string appVersion, List<MOBItem> catalogItems = null)
        {
            return (await _featureSettings.GetFeatureSettingValue("EnableVerticalSeatMapInBooking").ConfigureAwait(false)
               //   && (catalogItems != null && catalogItems.Count > 0 &&
               //catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableVerticalSeatMapInBooking).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableVerticalSeatMapInBooking).ToString())?.CurrentValue == "1")
               && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableVerticalSeatMapInBooking_AppVersion"), _configuration.GetValue<string>("IPhone_EnableVerticalSeatMapInBooking_AppVersion")));
        }
    }
}
