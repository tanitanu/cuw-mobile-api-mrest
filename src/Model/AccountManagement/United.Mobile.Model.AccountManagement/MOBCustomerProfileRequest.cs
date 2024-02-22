using System;
using System.Collections.Generic;

namespace United.Mobile.Model.Common
{
    [Serializable]
    public class MOBCustomerProfileRequest:MOBCPProfileRequest
    {  
        private string transactionPath = string.Empty;
        private string redirectViewName = string.Empty;
        private bool isRequirePKDispenserPublicKey;
        private List<MOBItem> catalogItems;

        public bool IsRequirePKDispenserPublicKey
        {
            get { return isRequirePKDispenserPublicKey; }
            set { isRequirePKDispenserPublicKey = value; }
        }
        public string RedirectViewName
        {
            get { return redirectViewName; }
            set { redirectViewName = value; }
        }
        public string TransactionPath
        {
            get { return transactionPath; }
            set { transactionPath = value; }
        }
        public List<MOBItem> CatalogItems
        {
            get { return catalogItems; }
            set { catalogItems = value; }
        }
    }
}
