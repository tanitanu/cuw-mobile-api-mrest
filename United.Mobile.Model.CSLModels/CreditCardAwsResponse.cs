using System;
using System.Collections.Generic;

namespace United.Mobile.Model.CSLModels
{
    public class CreditCardAwsResponse
    {
        public MOBCreditCardAwsData Data { get; set; }
        public List<MOBCustomerDataAwsResponseError> Errors { get; set; }
        public int Status { get; set; }
        public string ServerName { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }

    public class MOBCreditCardAwsData
    {
        public List<ReturnValue> ReturnValues { get; set; }
    }


    public class ReturnValue
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class MOBCustomerDataAwsResponseError
    {
        public string MajorCode { get; set; }
        public string MajorDescription { get; set; }
        public string MinorCode { get; set; }
        public string MinorDescription { get; set; }
        public string Message { get; set; }
        public string ErrorType { get; set; }
        public object UserFriendlyMessageType { get; set; }
        public object UserFriendlyMessageNumber { get; set; }
        public object UserFriendlyMessage { get; set; }
        public DateTime CallTime { get; set; }
    }

}
