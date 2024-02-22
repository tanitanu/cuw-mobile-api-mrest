﻿using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.Shopping
{
    /// <summary>
    /// This CLASS can be used to when OUT paratemeters are required for any methods instead of additing multiple paratemeters in methods
    /// </summary>
    public class MOBAdditionalItems
    {
        private bool showOrgAndDestByNearByAirport;

        private bool strikeThroughPricing;

        private List<string> airlineCodes;

        private string strikeThroughDisplayType;

        public List<string> AirlineCodes
        {
            get { return airlineCodes; }
            set { airlineCodes = value; }
        }

        public bool StrikeThroughPricing
        {
            get { return strikeThroughPricing; }
            set { strikeThroughPricing = value; }
        }

        public bool ShowOrgAndDestByNearByAirport
        {
            get { return showOrgAndDestByNearByAirport; }
            set { showOrgAndDestByNearByAirport = value; }
        }

        public int AppId { get; set; }

        public string AppVersion { get; set; }

        private bool moneyPlusMilesPricing;

        public bool MoneyPlusMilesPricing
        {
            get { return moneyPlusMilesPricing; }
            set { moneyPlusMilesPricing = value; }
        }

        public string StrikeThroughDisplayType
        {
            get { return strikeThroughDisplayType; }
            set { strikeThroughDisplayType = value; }
        }
        public string StrikeThroughTripDisplayType { get; set; }

    }
}
