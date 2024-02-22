using System;
using System.Collections.Generic;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class SHOPReward
    {
        public string TripId { get; set; }
        public string FlightId { get; set; }
        public string RewardId { get; set; }
        public bool Available { get; set; }
        public string Cabin { get; set; } = string.Empty;
        public decimal ChangeFee { get; set; }
        public decimal ChangeFeeTotal { get; set; }
        public string CrossCabinMessaging { get; set; } = string.Empty;
        public string FareBasis { get; set; } = string.Empty;
        public decimal Mileage { get; set; }
        public decimal MileageCollect { get; set; }
        public decimal MileageCollectTotal { get; set; }
        public decimal MileageTotal { get; set; }
        public bool Promotion { get; set; }
        public string RewardCode { get; set; } = string.Empty;
        public string RewardType { get; set; } = string.Empty;
        public bool Selected { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<SHOPTax> Taxes { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal TaxAndFeeTotal { get; set; }
        public List<string> Descriptions { get; set; }

        //public string TripId
        //{
        //    get
        //    {
        //        return this.tripId;
        //    }
        //    set
        //    {
        //        this.tripId = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string FlightId
        //{
        //    get
        //    {
        //        return this.flightId;
        //    }
        //    set
        //    {
        //        this.flightId = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string RewardId
        //{
        //    get
        //    {
        //        return this.rewardId;
        //    }
        //    set
        //    {
        //        this.rewardId = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public bool Available
        //{
        //    get
        //    {
        //        return this.available;
        //    }
        //    set
        //    {
        //        this.available = value;
        //    }
        //}

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

        //public decimal ChangeFee
        //{
        //    get
        //    {
        //        return this.changeFee;
        //    }
        //    set
        //    {
        //        this.changeFee = value;
        //    }
        //}

        //public decimal ChangeFeeTotal
        //{
        //    get
        //    {
        //        return this.changeFeeTotal;
        //    }
        //    set
        //    {
        //        this.changeFeeTotal = value;
        //    }
        //}

        //public string CrossCabinMessaging
        //{
        //    get
        //    {
        //        return this.crossCabinMessaging;
        //    }
        //    set
        //    {
        //        this.crossCabinMessaging = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string FareBasis
        //{
        //    get
        //    {
        //        return this.fareBasis;
        //    }
        //    set
        //    {
        //        this.fareBasis = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public decimal Mileage
        //{
        //    get
        //    {
        //        return this.mileage;
        //    }
        //    set
        //    {
        //        this.mileage = value;
        //    }
        //}

        //public decimal MileageCollect
        //{
        //    get
        //    {
        //        return this.mileageCollect;
        //    }
        //    set
        //    {
        //        this.mileageCollect = value;
        //    }
        //}

        //public decimal MileageCollectTotal
        //{
        //    get
        //    {
        //        return this.mileageCollectTotal;
        //    }
        //    set
        //    {
        //        this.mileageCollectTotal = value;
        //    }
        //}

        //public decimal MileageTotal
        //{
        //    get
        //    {
        //        return this.mileageTotal;
        //    }
        //    set
        //    {
        //        this.mileageTotal = value;
        //    }
        //}

        //public string RewardCode
        //{
        //    get
        //    {
        //        return this.rewardCode;
        //    }
        //    set
        //    {
        //        this.rewardCode = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
        //    }
        //}

        //public string RewardType
        //{
        //    get
        //    {
        //        return this.rewardType;
        //    }
        //    set
        //    {
        //        this.rewardType = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public bool Selected
        //{
        //    get
        //    {
        //        return this.selected;
        //    }
        //    set
        //    {
        //        this.selected = value;
        //    }
        //}

        //public string Status
        //{
        //    get
        //    {
        //        return this.status;
        //    }
        //    set
        //    {
        //        this.status = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public List<SHOPTax> Taxes
        //{
        //    get
        //    {
        //        return this.taxes;
        //    }
        //    set
        //    {
        //        this.taxes = value;
        //    }
        //}

        //public decimal TaxTotal
        //{
        //    get
        //    {
        //        return this.taxTotal;
        //    }
        //    set
        //    {
        //        this.taxTotal = value;
        //    }
        //}

        //public decimal TaxAndFeeTotal
        //{
        //    get
        //    {
        //        return this.taxAndFeeTotal;
        //    }
        //    set
        //    {
        //        this.taxAndFeeTotal = value;
        //    }
        //}

        //public List<string> Descriptions
        //{
        //    get
        //    {
        //        return this.descriptions;
        //    }
        //    set
        //    {
        //        this.descriptions = value;
        //    }
        //}
    }
}
