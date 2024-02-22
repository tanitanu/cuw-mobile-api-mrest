using System;
using System.Collections.Generic;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class SHOPSearchFilters
    {
        public string AircraftTypes { get; set; } = string.Empty;
        public string AirportsDestination { get; set; } = string.Empty;
        public List<SHOPSearchFilterItem> AirportsDestinationList { get; set; }
        public string AirportsOrigin { get; set; } = string.Empty;
        public List<SHOPSearchFilterItem> AirportsOriginList { get; set; }
        public string AirportsStop { get; set; } = string.Empty;
        public List<SHOPSearchFilterItem> AirportsStopList { get; set; }
        public string AirportsStopToAvoid = string.Empty;
        public List<SHOPSearchFilterItem> AirportsStopToAvoidList { get; set; }
        public string BookingCodes { get; set; } = string.Empty;
        public int CabinCountMax { get; set; } = -1;
        public int CabinCountMin { get; set; } = -1;
        public bool CarrierDefault { get; set; } = true;
        public bool CarrierExpress { get; set; } = true;
        public bool CarrierPartners { get; set; } = true;
        public string CarriersMarketing { get; set; } = string.Empty;
        public List<SHOPSearchFilterItem> CarriersMarketingList { get; set; }
        public string CarriersOperating { get; set; } = string.Empty;
        public List<SHOPSearchFilterItem> CarriersOperatingList { get; set; }
        public bool CarrierStar { get; set; } = true;
        public int DurationMax { get; set; } = -1;
        public int DurationMin { get; set; } = -1;
        public int DurationStopMax { get; set; } = -1;
        public int DurationStopMin { get; set; } = -1;
        public string EquipmentCodes { get; set; } = string.Empty;
        public List<SHOPSearchFilterItem> EquipmentList { get; set; }
        public string EquipmentTypes { get; set; } = string.Empty;
        public List<SHOPFareFamily> FareFamilies { get; set; }
        public string FareFamily { get; set; } = "";
        public decimal PriceMax { get; set; } = new Decimal(-1.0);
        public decimal PriceMin { get; set; } = new Decimal(-1.0);
        public string PriceMaxDisplayValue { get; set; } = string.Empty;
        public string PriceMinDisplayValue { get; set; } = string.Empty;
        public int StopCountExcl { get; set; } = -1;
        public int StopCountMax { get; set; } = -1;
        public int StopCountMin { get; set; } = -1;
        public string TimeArrivalMax { get; set; } = string.Empty;
        public string TimeArrivalMin { get; set; } = string.Empty;
        public string TimeDepartMax { get; set; } = string.Empty;
        public string TimeDepartMin { get; set; } = string.Empty;
        public List<string> Warnings { get; set; }
        public List<SHOPSearchFilterItem> WarningsFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public string SortType1 { get; set; } = string.Empty;
        public List<SHOPSearchFilterItem> SortTypes { get; set; }
        public List<SHOPSearchFilterItem> NumberofStops { get; set; }
        public List<SHOPSearchFilterItem> AmenityTypes { get; set; }
        public List<SHOPSearchFilterItem> CarrierTypes { get; set; }
        public List<SHOPSearchFilterItem> AircraftCabinTypes { get; set; }
        public bool ShowPriceFilters { get; set; } = false;
        public bool ShowDepartureFilters { get; set; } = false;
        public bool ShowArrivalFilters { get; set; } = false;
        public bool ShowDurationFilters { get; set; } = false;
        public bool ShowLayOverFilters { get; set; } = false;
        public bool ShowSortingandFilters { get; set; } = false;
        public bool FilterSortPaging { get; set; } = false;

        //public string AircraftTypes { get { return this.aircraftTypes; } set { this.aircraftTypes = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public string AirportsDestination { get { return this.airportsDestination; } set { this.airportsDestination = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public List<SHOPSearchFilterItem> AirportsDestinationList { get { return this.airportsDestinationList; } set { this.airportsDestinationList = value; } }
        //public string AirportsOrigin { get { return this.airportsOrigin; } set { this.airportsOrigin = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public List<SHOPSearchFilterItem> AirportsOriginList { get { return this.airportsOriginList; } set { this.airportsOriginList = value; } }
        //public string AirportsStop { get { return this.airportsStop; } set { this.airportsStop = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public List<SHOPSearchFilterItem> AirportsStopList { get { return this.airportsStopList; } set { this.airportsStopList = value; } }
        //public string AirportsStopToAvoid { get { return this.airportsStopToAvoid; } set { this.airportsStopToAvoid = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public List<SHOPSearchFilterItem> AirportsStopToAvoidList { get { return this.airportsStopToAvoidList; } set { this.airportsStopToAvoidList = value; } }
        //public string BookingCodes { get { return this.bookingCodes; } set { this.bookingCodes = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public int CabinCountMax { get { return this.cabinCountMax; } set { this.cabinCountMax = value; } }
        //public int CabinCountMin { get { return this.cabinCountMin; } set { this.cabinCountMin = value; } }
        //public bool CarrierDefault { get { return this.carrierDefault; } set { this.carrierDefault = value; } }
        //public bool CarrierExpress { get { return this.carrierExpress; } set { this.carrierExpress = value; } }
        //public bool CarrierPartners { get { return this.carrierPartners; } set { this.carrierPartners = value; } }
        //public string CarriersMarketing { get { return this.carriersMarketing; } set { this.carriersMarketing = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public List<SHOPSearchFilterItem> CarriersMarketingList { get { return this.carriersMarketingList; } set { this.carriersMarketingList = value; } }
        //public string CarriersOperating { get { return this.carriersOperating; } set { this.carriersOperating = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public List<SHOPSearchFilterItem> CarriersOperatingList { get { return this.carriersOperatingList; } set { this.carriersOperatingList = value; } }
        //public bool CarrierStar { get { return this.carrierStar; } set { this.carrierStar = value; } }
        //public int DurationMax { get { return this.durationMax; } set { this.durationMax = value; } }
        //public int DurationMin { get { return this.durationMin; } set { this.durationMin = value; } }
        //public int DurationStopMax { get { return this.durationStopMax; } set { this.durationStopMax = value; } }
        //public int DurationStopMin { get { return this.durationStopMin; } set { this.durationStopMin = value; } }
        //public string EquipmentCodes { get { return this.equipmentCodes; } set { this.equipmentCodes = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public List<SHOPSearchFilterItem> EquipmentList { get { return this.equipmentList; } set { this.equipmentList = value; } }
        //public string EquipmentTypes { get { return this.equipmentTypes; } set { this.equipmentTypes = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public List<SHOPFareFamily> FareFamilies { get { return this.fareFamilies; } set { this.fareFamilies = value; } }
        //public string FareFamily { get { return this.fareFamily; } set { this.fareFamily = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public decimal PriceMax { get { return this.priceMax; } set { this.priceMax = value; } }
        //public decimal PriceMin { get { return this.priceMin; } set { this.priceMin = value; } }
        //public string PriceMaxDisplayValue { get { return this.priceMaxDisplayValue; } set { this.priceMaxDisplayValue = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public string PriceMinDisplayValue { get { return this.priceMinDisplayValue; } set { this.priceMinDisplayValue = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public int StopCountExcl { get { return this.stopCountExcl; } set { this.stopCountExcl = value; } }
        //public int StopCountMax { get { return this.stopCountMax; } set { this.stopCountMax = value; } }
        //public int StopCountMin { get { return this.stopCountMin; } set { this.stopCountMin = value; } }
        //public string TimeArrivalMax { get { return this.timeArrivalMax; } set { this.timeArrivalMax = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public string TimeArrivalMin { get { return this.timeArrivalMin; } set { this.timeArrivalMin = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public string TimeDepartMax { get { return this.timeDepartMax; } set { this.timeDepartMax = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public string TimeDepartMin { get { return this.timeDepartMin; } set { this.timeDepartMin = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public List<string> Warnings { get { return this.warnings; } set { this.warnings = value; } }
        //public List<SHOPSearchFilterItem> WarningsFilter { get { return this.warningsFilter; } set { this.warningsFilter = value; } }
        //public int PageNumber { get { return this.pageNumber; } set { this.pageNumber = value; } }
        //public string SortType1 { get { return this.sortType1; } set { this.sortType1 = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); } }
        //public List<SHOPSearchFilterItem> SortTypes { get { return this.sortTypes; } set { this.sortTypes = value; } }
        //public List<SHOPSearchFilterItem> NumberofStops { get { return this.numberofStops; } set { this.numberofStops = value; } }
        //public List<SHOPSearchFilterItem> AmenityTypes { get { return this.amenityTypes; } set { this.amenityTypes = value; } }
        //public List<SHOPSearchFilterItem> AircraftCabinTypes { get { return this.aircraftCabinTypes; } set { this.aircraftCabinTypes = value; } }
        //public List<SHOPSearchFilterItem> CarrierTypes { get { return this.carrierTypes; } set { this.carrierTypes = value; } }
        //public bool ShowPriceFilters { get { return this.showPriceFilters; } set { this.showPriceFilters = value; } }
        //public bool ShowDepartureFilters { get { return this.showDepartureFilters; } set { this.showDepartureFilters = value; } }
        //public bool ShowArrivalFilters { get { return this.showArrivalFilters; } set { this.showArrivalFilters = value; } }
        //public bool ShowDurationFilters { get { return this.showDurationFilters; } set { this.showDurationFilters = value; } }
        //public bool ShowLayOverFilters { get { return this.showLayOverFilters; } set { this.showLayOverFilters = value; } }
        //public bool ShowSortingandFilters { get { return this.showSortingandFilters; } set { this.showSortingandFilters = value; } }
        //public bool FilterSortPaging { get { return this.filterSortPaging; } set { this.filterSortPaging = value; } }

        public SHOPSearchFilters()
        {
            AirportsDestinationList = new List<SHOPSearchFilterItem>();
            AirportsOriginList = new List<SHOPSearchFilterItem>();
            SortTypes = new List<SHOPSearchFilterItem>();
            NumberofStops = new List<SHOPSearchFilterItem>();
            AmenityTypes = new List<SHOPSearchFilterItem>();
            CarrierTypes = new List<SHOPSearchFilterItem>();
            AircraftCabinTypes = new List<SHOPSearchFilterItem>();
            Warnings = new List<string>();
            EquipmentList = new List<SHOPSearchFilterItem>();
            FareFamilies = new List<SHOPFareFamily>();
            CarriersMarketingList = new List<SHOPSearchFilterItem>();
            CarriersOperatingList = new List<SHOPSearchFilterItem>();
            AirportsDestinationList = new List<SHOPSearchFilterItem>();
            AirportsOriginList = new List<SHOPSearchFilterItem>();
            AirportsStopList = new List<SHOPSearchFilterItem>();
            AirportsStopToAvoidList = new List<SHOPSearchFilterItem>();
        }

    }
}
