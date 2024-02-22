using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using United.Mobile.Model.Common;

namespace United.Mobile.Test.BagCalculatorTests.Domain
{
  public  class TestDataSet
    {

        public Object[] set1()
        {

            var mobileCMSContentRequestJson = TestDataGenerator.GetFileContent(@"SampleData\MobileCMSContentRequest.json");
            var mobileCMSContentRequest = JsonConvert.DeserializeObject<List<MobileCMSContentRequest>>(mobileCMSContentRequestJson);

            var sessionjson = TestDataGenerator.GetFileContent(@"SampleData\session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponsejson = TestDataGenerator.GetFileContent(@"GetShop_TestData\ShoppingResponse.json");
            //var shoppingResponse = JsonConvert.DeserializeObject<List<ShoppingResponse>>(shoppingResponsejson);

            //var mOBOptimizelyQMDatajson = TestDataGenerator.GetFileContent(@"GetShop_TestData\MOBOptimizelyQMData.json");
            //var mOBOptimizelyQMData = JsonConvert.DeserializeObject<List<MOBOptimizelyQMData>>(mOBOptimizelyQMDatajson);

            //var mOBSHOPAvailabilityjson = TestDataGenerator.GetFileContent(@"GetShop_TestData\MOBSHOPAvailability.json");
            //var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<MOBSHOPAvailability>>(mOBSHOPAvailabilityjson);

            return new object[] { mobileCMSContentRequest[1], session[0]};
        }
    }
}
