﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;
using United.Mobile.Model.Common;
//using United.Services.FlightShopping.Common;

namespace United.Mobile.Model.Shopping
{
    [Serializable()]
    public class CPCorporate
    {

        private string companyName = string.Empty;
        private string discountCode = string.Empty;
        private string leisureDiscountCode = string.Empty;
        private List<ErrorInfo> errors = null;
        private string fareGroupId = string.Empty;
        private bool isValid;
        private long vendorId;
        private string vendorName = string.Empty;
        private string corporateBookingType;
        private int noOfTravelers;


        public int NoOfTravelers
        {
            get { return noOfTravelers; }
            set { noOfTravelers = value; }
        }

        public string CorporateBookingType
        {
            get { return corporateBookingType; }
            set { corporateBookingType = value; }
        }

        public string CompanyName
        {
            get
            {
                return companyName;
            }
            set
            {
                companyName = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        public string DiscountCode
        {
            get
            {
                return discountCode;
            }
            set
            {
                discountCode = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string LeisureDiscountCode
        {
            get
            {
                return leisureDiscountCode;
            }
            set
            {
                leisureDiscountCode = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public List<ErrorInfo> Errors
        {
            get { return errors; }
            set { errors = value; }
        }

        public string FareGroupId
        {
            get
            {
                return fareGroupId;
            }
            set
            {
                fareGroupId = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public bool IsValid
        {
            get { return isValid; }
            set { isValid = value; }
        }

        public long VendorId
        {
            get { return vendorId; }
            set { vendorId = value; }
        }

        public string VendorName
        {
            get
            {
                return vendorName;
            }
            set
            {
                vendorName = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        private bool isMultiPaxAllowed;
        public bool IsMultiPaxAllowed
        {
            get { return isMultiPaxAllowed; }
            set { isMultiPaxAllowed = value; }
        }
    }
}
