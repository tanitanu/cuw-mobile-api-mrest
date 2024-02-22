using System;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class SHOPSearchFilterItem
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DisplayValue { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public bool IsSelected { get; set; }

        //public string Key
        //{
        //    get
        //    {
        //        return this.key;
        //    }
        //    set
        //    {
        //        this.key = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string Value
        //{
        //    get
        //    {
        //        return this.value;
        //    }
        //    set
        //    {
        //        this.value = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string DisplayValue
        //{
        //    get
        //    {
        //        return this.displayValue;
        //    }
        //    set
        //    {
        //        this.displayValue = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string Amount
        //{
        //    get
        //    {
        //        return this.amount;
        //    }
        //    set
        //    {
        //        this.amount = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string Currency
        //{
        //    get
        //    {
        //        return this.currency;
        //    }
        //    set
        //    {
        //        this.currency = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public bool IsSelected
        //{
        //    get { return this.isSelected; }
        //    set { this.isSelected = value; }
        //}
    }
}
