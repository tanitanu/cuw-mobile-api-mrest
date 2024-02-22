using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.Shopping
{
    [Serializable()]
    [XmlRoot("MOBSHOPSelectTripRequest")]
    public class SelectTripRequest : MOBRequest
    {
        public string cartId { get; set; } = string.Empty;

        public int LengthOfCalendar { get; set; }

        public string SessionId { get; set; } = string.Empty;

        public string TripId { get; set; } = string.Empty;

        public string FlightId { get; set; } = string.Empty;

        public string ProductId { get; set; } = string.Empty;

        public string RewardId { get; set; } = string.Empty;

        public bool UseFilters { get; set; }

        public MOBSearchFilters Filters { get; set; }

        public string ResultSortType { get; set; } 
        

        public string CalendarDateChange { get; set; } 

        public bool BackButtonClick { get; set; }

        public bool ISProductSelected { get; set; }

        public bool GetNonStopFlightsOnly { get; set; }

        public bool GetFlightsWithStops { get; set; }
        private List<MOBItem> catalogItems;
        public List<MOBItem> CatalogItems
        {
            get { return catalogItems; }
            set { catalogItems = value; }
        }

        private bool isMoneyPlusMiles;

        public bool IsMoneyPlusMiles
        {
            get { return isMoneyPlusMiles; }
            set { isMoneyPlusMiles = value; }
        }
        private string mileagePlusAccountNumber;

        public string MileagePlusAccountNumber
        {
            get { return mileagePlusAccountNumber; }
            set { mileagePlusAccountNumber = value; }
        }

        private string hashPinCode;
        public string HashPinCode
        {
            get { return hashPinCode; }
            set { hashPinCode = value; }
        }

        private string moneyPlusMilesOptionId;

        public string MoneyPlusMilesOptionId
        {
            get { return moneyPlusMilesOptionId; }
            set { moneyPlusMilesOptionId = value; }
        }
        private int premierStatusLevel;

        public int PremierStatusLevel
        {
            get
            {
                return this.premierStatusLevel;
            }
            set
            {
                this.premierStatusLevel = value;
            }
        }
        private string mileageBalance;

        public string MileageBalance
        {
            get { return mileageBalance; }
            set { mileageBalance = value; }
        }
        private string pricingType;

        public string PricingType
        {
            get { return pricingType; }
            set { pricingType = value; }
        }

    }
}
