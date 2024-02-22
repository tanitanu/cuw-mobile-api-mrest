using System;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class SHOPTax
    {
        public decimal Amount { get; set; }
        public string DisplayAmount { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal NewAmount { get; set; }
        public string DisplayNewAmount { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string TaxCodeDescription { get; set; } = string.Empty;

        //public decimal Amount
        //{
        //    get
        //    {
        //        return this.amount;
        //    }
        //    set
        //    {
        //        this.amount = value;
        //    }
        //}

        //public string DisplayAmount
        //{
        //    get
        //    {
        //        return this.displayAmount;
        //    }
        //    set
        //    {
        //        this.displayAmount = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
        //    }
        //}

        //public string CurrencyCode
        //{
        //    get
        //    {
        //        return this.currencyCode;
        //    }
        //    set
        //    {
        //        this.currencyCode = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
        //    }
        //}

        //public decimal NewAmount
        //{
        //    get
        //    {
        //        return this.newAmount;
        //    }
        //    set
        //    {
        //        this.newAmount = value;
        //    }
        //}

        //public string DisplayNewAmount
        //{
        //    get { return this.displayNewAmount; }
        //    set { this.displayNewAmount = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper(); }
        //}

        //public string TaxCode
        //{
        //    get { return this.taxCode; }
        //    set { this.taxCode = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); }
        //}

        //public string TaxCodeDescription
        //{
        //    get { return this.taxCodeDescription; }
        //    set { this.taxCodeDescription = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); }
        //}
    }
}
