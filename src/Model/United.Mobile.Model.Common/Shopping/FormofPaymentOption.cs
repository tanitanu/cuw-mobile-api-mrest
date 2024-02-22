﻿using System;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace United.Mobile.Model.Common.Shopping
{
    [Serializable()]
    public class FormofPaymentOption
    {

        private string category = string.Empty;
        private string fullName = string.Empty;
        private string code = string.Empty;
        private string fopDescription = string.Empty;
        private bool deleteOrder;
        public string ObjectName { get; set; } = "United.Definition.FormofPaymentOption";

        public string Category
        {
            get { return category; }
            set { category = value; }
        }
        public string FullName
        {
            get { return fullName; }
            set { fullName = value; }
        }
        public string Code
        {
            get { return code; }
            set { code = value; }
        }

        public bool DeleteOrder
        {
            get { return deleteOrder; }
            set { deleteOrder = value; }
        }

        [JsonPropertyName("fopDescription")]
        [JsonProperty("fopDescription")]
        public string FoPDescription
        {
            get { return fopDescription; }
            set { fopDescription = value; }
        }

    }
}
