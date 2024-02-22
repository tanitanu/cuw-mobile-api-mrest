using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.Internal.Common.DynamoDB
{
    public class GetDataRequestForAllRecordsByKeys
    {
        public string TransactionId { get; set; }
        public string TableName { get; set; }
        public List<FilterObj> Filters { get; set; }
    }

    public class FilterObj
    {
        public string key { get; set; }
        public string value { get; set; }
    }
}