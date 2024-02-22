using System;
using System.Collections.Generic;

namespace United.Mobile.Model.Shopping.Common.MoneyPlusMiles
{
    [Serializable]
    public class MOBMoneyPlusMilesOptionsResponse : MOBResponse
    {
        private List<MOBMoneyPlusMilesShopFlightDetails> flights;

        public List<MOBMoneyPlusMilesShopFlightDetails> Flights
        {
            get { return flights; }
            set { flights = value; }
        }

        private List<MOBOnScreenAlert> onScreenAlerts;

        public List<MOBOnScreenAlert> OnScreenAlerts
        {
            get { return onScreenAlerts; }
            set { onScreenAlerts = value; }
        }
    }

    [Serializable]
    public class MOBMoneyPlusMilesShopFlightDetails
    {
        private List<MOBMoneyPlusMilesShopProduct> products;

        public List<MOBMoneyPlusMilesShopProduct> Products
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
    public class MOBMoneyPlusMilesShopProduct
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

        private string milesDisplayValue;

        public string MilesDisplayValue
        {
            get { return milesDisplayValue; }
            set { milesDisplayValue = value; }
        }
        private string moneyPlusMilesOptionId;

        public string MoneyPlusMilesOptionId
        {
            get { return moneyPlusMilesOptionId; }
            set { moneyPlusMilesOptionId = value; }
        }
    }
}
