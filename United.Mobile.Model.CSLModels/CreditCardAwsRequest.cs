using System;

namespace United.Mobile.Model.CSLModels
{
    public class CreditCardAwsRequest
    {
        public string TravelerKey { get; set; }

        public int CustomerId { get; set; }

        public string CreditCardType { get; set; }

        public string AccountNumberToken { get; set; }

        public int ExpirationMonth { get; set; }
        public int ExpirationYear { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string AddressKey { get; set; }

        public bool IsPrimary { get; set; }

        public string InsertId { get; set; }

        public string CreditCardKey { get; set; }
    }

}
