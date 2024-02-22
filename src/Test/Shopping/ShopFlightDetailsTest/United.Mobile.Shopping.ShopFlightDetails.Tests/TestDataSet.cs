using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.TeaserPage;
using United.Services.FlightShopping.Common;

namespace United.Mobile.Shopping.ShopFlightDetails.Tests
{
    public class TestDataSet
    {
        public Object[] set1()
        {
            var OnTimePerformanceRequestJson = TestDataGenerator.GetFileContent("OnTimePerformanceRequest.json");
            var OnTimePerformanceRequest = JsonConvert.DeserializeObject<List<OnTimePerformanceRequest>>(OnTimePerformanceRequestJson);

            var OnTimePerformanceResponseJson = TestDataGenerator.GetFileContent("OnTimePerformanceResponse.json");
            var OnTimePerformanceResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.OnTimePerformanceInfoResponse>>(OnTimePerformanceResponseJson);

            var input1 = TestDataGenerator.GetXmlData<Session>(@"Set1\Session1.xml");
            return new object[] { input1, OnTimePerformanceRequest[0], OnTimePerformanceResponse[0] };

        }
        public Object[] set2()
        {
            var OnTimePerformanceRequestJson = TestDataGenerator.GetFileContent("OnTimePerformanceRequest.json");
            var OnTimePerformanceRequest = JsonConvert.DeserializeObject<List<OnTimePerformanceRequest>>(OnTimePerformanceRequestJson);

            var OnTimePerformanceResponseJson = TestDataGenerator.GetFileContent("OnTimePerformanceResponse.json");
            var OnTimePerformanceResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.OnTimePerformanceInfoResponse>>(OnTimePerformanceResponseJson);

            var input1 = TestDataGenerator.GetXmlData<Session>(@"Set1\Session1.xml");
            return new object[] { input1, OnTimePerformanceRequest[0], OnTimePerformanceResponse[1] };

        }
        public Object[] set3()
        {
            var OnTimePerformanceRequestJson = TestDataGenerator.GetFileContent("OnTimePerformanceRequest.json");
            var OnTimePerformanceRequest = JsonConvert.DeserializeObject<List<OnTimePerformanceRequest>>(OnTimePerformanceRequestJson);

            var OnTimePerformanceResponseJson = TestDataGenerator.GetFileContent("OnTimePerformanceResponse.json");
            var OnTimePerformanceResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.OnTimePerformanceInfoResponse>>(OnTimePerformanceResponseJson);

            var input1 = TestDataGenerator.GetXmlData<Session>(@"Set1\Session1.xml");
            return new object[] { input1, OnTimePerformanceRequest[0], OnTimePerformanceResponse[2] };

        }

        public Object[] set4()
        {
            var mOBSHOPShoppingTeaserPageRequestjson = TestDataGenerator.GetFileContent("MOBSHOPShoppingTeaserPageRequest.json");
            var mOBSHOPShoppingTeaserPageRequest = JsonConvert.DeserializeObject<List<MOBSHOPShoppingTeaserPageRequest>>(mOBSHOPShoppingTeaserPageRequestjson);

            // var session = TestDataGenerator.GetXmlData<Session>("Session.xml");

            var sessionJson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            var fareColumnContentInformationResponsejson = TestDataGenerator.GetFileContent("FareColumnContentInformationResponse.json");
            var fareColumnContentInformationResponse = JsonConvert.DeserializeObject<List<FareColumnContentInformationResponse>>(fareColumnContentInformationResponsejson);

            var cMSContentMessageJson = TestDataGenerator.GetFileContent("CMSContentMessage.json");
            var cMSContentMessage = JsonConvert.DeserializeObject<List<List<CMSContentMessage>>>(cMSContentMessageJson);

            var mOBSHOPShoppingTeaserPageResponseJson = TestDataGenerator.GetFileContent("MOBSHOPShoppingTeaserPageResponse.json");
            var mOBSHOPShoppingTeaserPageResponse = JsonConvert.DeserializeObject<MOBSHOPShoppingTeaserPageResponse>(mOBSHOPShoppingTeaserPageResponseJson);

            return new object[] { mOBSHOPShoppingTeaserPageRequest[0], session[0], fareColumnContentInformationResponse[0], cMSContentMessage[0], mOBSHOPShoppingTeaserPageResponse };
        }

        public Object[] set5()
        {
            var mOBSHOPShoppingTeaserPageRequestjson = TestDataGenerator.GetFileContent("MOBSHOPShoppingTeaserPageRequest.json");
            var mOBSHOPShoppingTeaserPageRequest = JsonConvert.DeserializeObject<List<MOBSHOPShoppingTeaserPageRequest>>(mOBSHOPShoppingTeaserPageRequestjson);

            // var session = TestDataGenerator.GetXmlData<Session>("Session.xml");

            var sessionJson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            var fareColumnContentInformationResponsejson = TestDataGenerator.GetFileContent("FareColumnContentInformationResponse.json");
            var fareColumnContentInformationResponse = JsonConvert.DeserializeObject<List<FareColumnContentInformationResponse>>(fareColumnContentInformationResponsejson);

            var cMSContentMessageJson = TestDataGenerator.GetFileContent("CMSContentMessage.json");
            var cMSContentMessage = JsonConvert.DeserializeObject<List<List<CMSContentMessage>>>(cMSContentMessageJson);

            var mOBSHOPShoppingTeaserPageResponseJson = TestDataGenerator.GetFileContent("MOBSHOPShoppingTeaserPageResponse.json");
            var mOBSHOPShoppingTeaserPageResponse = JsonConvert.DeserializeObject<MOBSHOPShoppingTeaserPageResponse>(mOBSHOPShoppingTeaserPageResponseJson);

            var mOBSHOPShoppingProductListjson = TestDataGenerator.GetFileContent("MOBSHOPShoppingProductList.json");
            var mOBSHOPShoppingProductList = JsonConvert.DeserializeObject<List<MOBSHOPShoppingProductList>>(mOBSHOPShoppingProductListjson);

            var shoppingResponseJson = TestDataGenerator.GetFileContent("ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<ShoppingResponse>(shoppingResponseJson);

            return new object[] { mOBSHOPShoppingTeaserPageRequest[0], session[1], fareColumnContentInformationResponse[0], cMSContentMessage[0], mOBSHOPShoppingTeaserPageResponse, mOBSHOPShoppingProductList[1], shoppingResponse };
        }

    }
}
