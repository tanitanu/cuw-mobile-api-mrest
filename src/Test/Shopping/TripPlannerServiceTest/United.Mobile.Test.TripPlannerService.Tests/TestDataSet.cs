using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.TripPlannerGetService;
using United.Mobile.Model.TripPlannerService;

namespace United.Mobile.Test.TripPlannerService.Tests
{
  public  class TestDataSet
    {
        public Object[] set1()
        {

            var mOBTripPlanVoteRequestJson = TestDataGenerator.GetFileContent("MOBTripPlanVoteRequest.json");
            var mOBTripPlanVoteRequest = JsonConvert.DeserializeObject<MOBTripPlanVoteRequest>(mOBTripPlanVoteRequestJson);

            var sessionjson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

           

            return new object[] { mOBTripPlanVoteRequest, session[0]};
        }

        public Object[] set2()
        {

            var mOBTripPlanDeleteRequestJson = TestDataGenerator.GetFileContent("MOBTripPlanDeleteRequest.json");
            var mOBTripPlanDeleteRequest = JsonConvert.DeserializeObject<MOBTripPlanDeleteRequest>(mOBTripPlanDeleteRequestJson);

            var sessionjson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var tripPlanCCEResponseJson = TestDataGenerator.GetFileContent("TripPlanCCEResponse.json");
            var tripPlanCCEResponse = JsonConvert.DeserializeObject<TripPlanCCEResponse>(tripPlanCCEResponseJson);

            return new object[] { mOBTripPlanDeleteRequest, session[0], tripPlanCCEResponse };
        }

        public Object[] set3()
        {

            var mOBTripPlanVoteRequestJson = TestDataGenerator.GetFileContent("MOBTripPlanVoteRequest.json");
            var mOBTripPlanVoteRequest = JsonConvert.DeserializeObject<MOBTripPlanVoteRequest>(mOBTripPlanVoteRequestJson);

            var sessionjson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

           

            return new object[] { mOBTripPlanVoteRequest, session[0] };
        }
    }
}
