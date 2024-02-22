using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Common;
using Session = United.Mobile.Model.Common.Session;

namespace United.Mobile.Test.SeatEngine.Api
{
    public class TestDataSet
    {
        public Object[] set1()
        {
            var mOBSeatMapRequestjson = TestDataGenerator.GetFileContent(@"ReshopFlow\MOBSeatMapRequest.json");
            var mOBSeatMapRequest = JsonConvert.DeserializeObject<List<MOBSeatMapRequest>>(mOBSeatMapRequestjson);

            var cslResponsejson = TestDataGenerator.GetFileContent("cslstrResponse.json");
            var cslResponse = JsonConvert.DeserializeObject<List<United.Definition.SeatCSL30.SeatMap>>(cslResponsejson);

            var onTimePerformanceInfoResponsejson = TestDataGenerator.GetFileContent("OnTimePerformanceInfoResponse.json");
            var onTimePerformanceInfoResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.OnTimePerformanceInfoResponse>>(onTimePerformanceInfoResponsejson);

            var cabinBrandsjson = TestDataGenerator.GetFileContent("cabinBrands.json");
            var cabinBrands = JsonConvert.DeserializeObject<List<List<CabinBrand>>>(cabinBrandsjson);

            var sessionjson = TestDataGenerator.GetFileContent("SessionData.json");
            var session = JsonConvert.DeserializeObject<Session>(sessionjson);


            return new object[] { mOBSeatMapRequest[0], cslResponse[12], onTimePerformanceInfoResponse[0], cabinBrands[0],session };
        }

        public Object[] set1_1()
        {
            var mOBSeatMapRequestjson = TestDataGenerator.GetFileContent(@"ReshopFlow\MOBSeatMapRequest.json");
            var mOBSeatMapRequest = JsonConvert.DeserializeObject<List<MOBSeatMapRequest>>(mOBSeatMapRequestjson);

            var cslResponsejson = TestDataGenerator.GetFileContent("cslstrResponse.json");
            var cslResponse = JsonConvert.DeserializeObject<List<United.Definition.SeatCSL30.SeatMap>>(cslResponsejson);

            var onTimePerformanceInfoResponsejson = TestDataGenerator.GetFileContent("OnTimePerformanceInfoResponse.json");
            var onTimePerformanceInfoResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.OnTimePerformanceInfoResponse>>(onTimePerformanceInfoResponsejson);

            var cabinBrandsjson = TestDataGenerator.GetFileContent("cabinBrands.json");
            var cabinBrands = JsonConvert.DeserializeObject<List<List<CabinBrand>>>(cabinBrandsjson);

            var sessionjson = TestDataGenerator.GetFileContent("SessionData.json");
            var session = JsonConvert.DeserializeObject<Session>(sessionjson);


            return new object[] { mOBSeatMapRequest[0], cslResponse[0], onTimePerformanceInfoResponse[0], cabinBrands[0], session };
        }
    }
}
