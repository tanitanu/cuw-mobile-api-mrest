﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace United.Mobile.Model.Shopping
{
    [Serializable]
    public class DOTBaggageCalculatorResponse : MOBResponse
    {
        public DOTBaggageCalculatorResponse()
        {

        }
        public DOTBaggageCalculatorResponse(bool getDOTStaticInfoText)
        {
            if (getDOTStaticInfoText)
            {
                faqsCheckedBagsTitle = System.Configuration.ConfigurationManager.AppSettings["DOTBaggageFAQsTitle"].Split('|')[0].ToString();
                myCheckedBagServiceChargesDesc = System.Configuration.ConfigurationManager.AppSettings["MyCheckedBagServiceChargesDesc"].Split('|')[0].ToString();
            }
        }
        private DOTBaggageInfoRequest request;
        private List<DOTBaggageAdditionalDetails> checkedAndOtherBagFees;
        private string faqsCheckedBagsTitle;
        private List<DOTBaggageFAQ> baggageFAQs;
        private string myCheckedBagServiceChargesDesc = string.Empty;
        private bool isAnyFlightSearch;

        public DOTBaggageInfoRequest Request
        {
            get
            {
                return this.request;
            }
            set
            {
                this.request = value;
            }
        }
        public List<DOTBaggageAdditionalDetails> CheckedAndOtherBagFees
        {
            get
            {
                return this.checkedAndOtherBagFees;
            }
            set
            {
                this.checkedAndOtherBagFees = value;
            }
        }
        public string FAQsCheckedBagsTitle
        {
            get { return this.faqsCheckedBagsTitle; }
            set { this.faqsCheckedBagsTitle = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); }
        }
        public List<DOTBaggageFAQ> BaggageFAQs
        {
            get { return this.baggageFAQs; }
            set { this.baggageFAQs = value; }
        }
        public string MyCheckedBagServiceChargesDesc
        {
            get { return this.myCheckedBagServiceChargesDesc; }
            set { this.myCheckedBagServiceChargesDesc = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); }
        }

        public bool IsAnyFlightSearch
        {
            get { return isAnyFlightSearch; }
            set { isAnyFlightSearch = value; }
        }
    }
}
