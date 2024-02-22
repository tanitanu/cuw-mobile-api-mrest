using System;
using System.Collections.Generic;

namespace United.Mobile.Model.Shopping.Common
{
    [Serializable]
    public class MOBFSRMileagePricingResponse : MOBResponse
    {

        private string pricingType;

        public string PricingType
        {
            get { return pricingType; }
            set { pricingType = value; }
        }

        private List<MOBFSRMileagePricingShopFlightDetails> flights;

        public List<MOBFSRMileagePricingShopFlightDetails> Flights
        {
            get { return flights; }
            set { flights = value; }
        }

        private List<MOBFSRAlertMessage> fsrAlertMessages;

        public List<MOBFSRAlertMessage> FsrAlertMessages
        {
            get { return fsrAlertMessages; }
            set { fsrAlertMessages = value; }
        }

        private List<MOBOnScreenAlert> onScreenAlerts;

        public List<MOBOnScreenAlert> OnScreenAlerts
        {
            get { return onScreenAlerts; }
            set { onScreenAlerts = value; }
        }
    }

    [Serializable]
    public class MOBFSRMileagePricingShopFlightDetails
    {
        private List<MOBFSRMileagePricingShopProduct> products;

        public List<MOBFSRMileagePricingShopProduct> Products
        {
            get { return products; }
            set { products = value; }
        }

        private string flightHash;

        public string FlightHash
        {
            get { return flightHash; }
            set { flightHash = value; }
        }
    }


    [Serializable]
    public class MOBFSRMileagePricingShopProduct
    {
        private string productId;

        public string ProductId
        {
            get { return productId; }
            set { productId = value; }
        }

        private string price;

        public string Price
        {
            get { return price; }
            set { price = value; }
        }

        private string displayValue;        

        public string DisplayValue
        {
            get { return displayValue; }
            set { displayValue = value; }
        }

        private string pricingTypeOptionId;

        public string PricingTypeOptionId
        {
            get { return pricingTypeOptionId; }
            set { pricingTypeOptionId = value; }
        }
    }
}
