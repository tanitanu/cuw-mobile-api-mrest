using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.TripPlannerGetService;
using United.Service.Presentation.PersonalizationResponseModel;
using CSLShopRequest = United.Mobile.Model.TripPlannerGetService.CSLShopRequest;
using CSLShopResponse = United.Mobile.Model.TripPlannerGetService.CSLShopResponse;
using TripPlanCCEResponse = United.Mobile.Model.TripPlannerGetService.TripPlanCCEResponse;

namespace United.Mobile.Test.TripPlannerGetService.Tests
{
   public class TestDataSet
    {
        public Object[] set1()
        {

            var mOBTripPlanSummaryRequestJson = TestDataGenerator.GetFileContent("MOBTripPlanSummaryRequest.json");
            var mOBTripPlanSummaryRequest = JsonConvert.DeserializeObject<List<MOBTripPlanSummaryRequest>>(mOBTripPlanSummaryRequestJson);

            var sessionjson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var cSLShopResponseJson = TestDataGenerator.GetFileContent("CSLShopResponse.json");
            var cSLShopResponse = JsonConvert.DeserializeObject<List<CSLShopResponse>>(cSLShopResponseJson);

            var cSLShopRequestJson = TestDataGenerator.GetFileContent("CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestJson);

            var tripPlanCCEResponseJson = TestDataGenerator.GetFileContent("TripPlanCCEResponse.json");
            var tripPlanCCEResponse = JsonConvert.DeserializeObject<TripPlanCCEResponse>(tripPlanCCEResponseJson);

            //var contextualCommResponseJson = TestDataGenerator.GetFileContent("ContextualCommResponse.json");
            //var contextualCommResponse = JsonConvert.DeserializeObject<List<ContextualCommResponse>>(contextualCommResponseJson);


            return new object[] { mOBTripPlanSummaryRequest[0], session[0], cSLShopResponse[0], cSLShopRequest[0], tripPlanCCEResponse};
        }

        public Object[] set1_1()
        {

            var mOBTripPlanSummaryRequestJson = TestDataGenerator.GetFileContent("MOBTripPlanSummaryRequest.json");
            var mOBTripPlanSummaryRequest = JsonConvert.DeserializeObject<List<MOBTripPlanSummaryRequest>>(mOBTripPlanSummaryRequestJson);

            var sessionjson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var cSLShopResponseJson = TestDataGenerator.GetFileContent("CSLShopResponse.json");
            var cSLShopResponse = JsonConvert.DeserializeObject<List<CSLShopResponse>>(cSLShopResponseJson);

            var cSLShopRequestJson = TestDataGenerator.GetFileContent("CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestJson);

            var tripPlanCCEResponseJson = TestDataGenerator.GetFileContent("TripPlanCCEResponse.json");
            var tripPlanCCEResponse = JsonConvert.DeserializeObject<TripPlanCCEResponse>(tripPlanCCEResponseJson);

            //var contextualCommResponseJson = TestDataGenerator.GetFileContent("ContextualCommResponse.json");
            //var contextualCommResponse = JsonConvert.DeserializeObject<List<ContextualCommResponse>>(contextualCommResponseJson);


            return new object[] { mOBTripPlanSummaryRequest[1], session[0], cSLShopResponse[0], cSLShopRequest[0], tripPlanCCEResponse };
        }

        public Object[] set2()
        {

            var mOBSHOPSelectTripRequestJson = TestDataGenerator.GetFileContent("MOBSHOPSelectTripRequest.json");
            var mOBSHOPSelectTripRequest = JsonConvert.DeserializeObject<MOBSHOPSelectTripRequest>(mOBSHOPSelectTripRequestJson);

            var sessionjson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var shoppingResponseJson = TestDataGenerator.GetFileContent("ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<ShoppingResponse>(shoppingResponseJson);

            var cSLShopResponseJson = TestDataGenerator.GetFileContent("CSLShopResponse.json");
            var cSLShopResponse = JsonConvert.DeserializeObject<List<CSLShopResponse>>(cSLShopResponseJson);

            var cSLShopRequestJson = TestDataGenerator.GetFileContent("CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestJson);

            var cSLSelectTripjson = TestDataGenerator.GetFileContent("CSLSelectTrip.json");
            var cSLSelectTrip = JsonConvert.DeserializeObject<List<CSLSelectTrip>>(cSLSelectTripjson);



            return new object[] { mOBSHOPSelectTripRequest, session[0], shoppingResponse, cSLShopResponse[0], cSLShopRequest[0], cSLSelectTrip[0] };
        }

        public Object[] set3()
        {

            var mOBTripPlanBoardRequestJson = TestDataGenerator.GetFileContent("MOBTripPlanBoardRequest.json");
            var mOBTripPlanBoardRequest = JsonConvert.DeserializeObject<MOBTripPlanBoardRequest>(mOBTripPlanBoardRequestJson);

            var sessionjson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);



            return new object[] { mOBTripPlanBoardRequest, session[0] };
        }
    }
}
