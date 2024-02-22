using System;
using System.Collections.Generic;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable]
    public class SHOPRewardPriceSummary
    {
        public string Cabin { get; set; }
        public List<string> PriceSummaries { get; set; }

        //public string Cabin
        //{
        //    get
        //    {
        //        return this.cabin;
        //    }
        //    set
        //    {
        //        this.cabin = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public List<string> PriceSummaries
        //{
        //    get
        //    {
        //        return this.priceSummaries;
        //    }
        //    set
        //    {
        //        this.priceSummaries = value;
        //    }
        //}

        public SHOPRewardPriceSummary()
        {
            PriceSummaries = new List<string>();
        }
    }
}
