using System;
using System.Collections.Generic;
using System.Text;

namespace United.Utility.Enum
{
    public enum FlowType
    {
        ALL,
        BOOKING,
        POSTBOOKING,
        MANAGERES,
        BAGGAGECALCULATOR,
        INITIAL,
        EXCHANGE,
        VIEWRES,
        CHECKIN,
        ERES,
        RESHOP,
        VIEWRES_SEATMAP,
        VIEWRES_BUNDLES_SEATMAP,
        FARELOCK,
        UPGRADEMALL,
        FLIGHTSTATUS_SEATMAP,
        BOOKING_PREVIEW_SEATMAP,
        CHECKINSDC,
        SCHEDULECHANGE,
        SHOPBYMAP,
        MOBILECHECKOUT,
        OMNICARTDEEPLINK
    }

    public enum IOSCatalogEnum
    {
        EnableBuyMilesFeatureCatalogID = 11388,
        AwardStrikeThroughPricing = 11428,
        EnableOmnicartRC4CChanges = 11463,
        EnableTaskTrainedServiceAnimalFeature = 11587,
        EnableEditSearchOnFSRHeader = 11502,
        EnableWheelchairLinkUpdate = 11647,
        EnableNewPartnerAirlines = 11643,
        EnableU4BCorporateBooking = 11699,
        DisablePKDispenserKeyFromCSL = 11757,
        EnableSAFFeature = 11870,
        DisablePKDispenserKeyFromCSLForOTP = 11713,
        EnableExtraSeatFeature = 12125,
        EnableNoFlightsSeasonalityFeature = 11793,
        EnableCarbonEmissionsFeature = 11890,
        EnableFareLockAmoutDisplayPerPerson = 11815,
        EnableSearchByMapFirstForNoFlightsFound = 11867,
        EnableFareWheelFiltersFromSelectTripResponseRequest = 11873,
        EnableOmnicartRetargeting = 12003,
        EnableGetFSRDInfoFromCSL = 11878,
        EnableU4BTravelAddOnPolicy = 11936,
        EnableMilesFOPForPaidSeats = 12032,
        EnableWheelChairSizer = 12061,
        EnableExpressCheckout = 12044,
        EnableOfferCodeExpansionChanges= 12093,
        EnableGuatemalaTaxIdCollection = 12106,
        EnableIsEligibilityReasons = 12094,
        EnableMoneyMilesBooking = 12122,
        EnablePartnerProvision = 12121,
        EnableCancelbuttonOnscreenAlert = 12208,
        EnableTripInsuranceV2 = 12221,
        EnableProvisionInVIEWRES = 12219,
        EnableProvisionInCHECKIN = 12243,
        EnableTaxIdChangesForLATIDCountries = 12450,
        EnableCustomerFacingCartId = 12283,
        EnableFSROAFlashFeature = 12378,
        EnableETCCreditsInBooking = 12258,
        EnableWheelchairFilterOnFSR = 12319,
        EnableMultiCityEditSearch = 12309,
        EnableFSROAFlashFeatureInReShop = 12372,
        ENABLE_U4B_FOR_MULTI_PAX = 12272,
        EnableVerticalSeatMapInBooking = 12439,
        EnableReshopWheelchairFilterOnFSR = 12449
    }

    public enum AndroidCatalogEnum
    {
        EnableBuyMilesFeatureCatalogID = 21388,
        AwardStrikeThroughPricing = 21428,
        EnableOmnicartRC4CChanges = 21463,
        EnableTaskTrainedServiceAnimalFeature = 21587,
        EnableEditSearchOnFSRHeader = 21502,
        EnableWheelchairLinkUpdate = 21647,
        EnableNewPartnerAirlines = 21643,
        EnableU4BCorporateBooking = 21699,
        DisablePKDispenserKeyFromCSL = 21757,
        EnableSAFFeature = 21870,
        DisablePKDispenserKeyFromCSLForOTP = 21713,
        EnableExtraSeatFeature = 22125,
        EnableNoFlightsSeasonalityFeature = 21793,
        EnableCarbonEmissionsFeature = 21890,
        EnableFareLockAmoutDisplayPerPerson = 21815,
        EnableSearchByMapFirstForNoFlightsFound = 21867,
        EnableFareWheelFiltersFromSelectTripResponseRequest = 21873,
        EnableOmnicartRetargeting = 22003,
        EnableGetFSRDInfoFromCSL = 21878,
        EnableU4BTravelAddOnPolicy = 21936,
        EnableMilesFOPForPaidSeats = 22032,
        EnableWheelChairSizer = 22061,
        EnableExpressCheckout = 22044,
        EnableOfferCodeExpansionChanges = 22093,
        EnableGuatemalaTaxIdCollection = 22106,
        EnableIsEligibilityReasons = 22094,
        EnableMoneyMilesBooking = 22122,
        EnablePartnerProvision = 22121,
        EnableCancelbuttonOnscreenAlert = 22208,
        EnableTripInsuranceV2 = 22221,
        EnableProvisionInVIEWRES = 22219,
        EnableProvisionInCHECKIN = 22243,
        EnableTaxIdChangesForLATIDCountries = 22450,
        EnableCustomerFacingCartId = 22283,
        EnableFSROAFlashFeature = 22378,
        EnableETCCreditsInBooking = 22258,
        EnableWheelchairFilterOnFSR = 22319,
        EnableMultiCityEditSearch = 22309,
        EnableFSROAFlashFeatureInReShop = 22372,
        ENABLE_U4B_FOR_MULTI_PAX = 22272,
        EnableVerticalSeatMapInBooking = 22439,
        EnableReshopWheelchairFilterOnFSR = 22449
    }

    public enum FlightCombinationType
    {
        UAandUAXFlights,
        OAFlights,
        UAandOAFlights
    }

    public enum TaxIdType
    {
        NI,
        ID,
        PP
    }
}
