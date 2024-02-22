using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using United.Mobile.Model.FlightReservation;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.MPRewards;

namespace United.Mobile.Test.FlightReservation.Tests

{
    class TestDataGenerator
    {
        public static string GetFileContent(string fileName)
        {
            fileName = string.Format("..\\..\\..\\TestData\\{0}", fileName);
            var path = Path.IsPathRooted(fileName) ? fileName : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }
        public static IEnumerable<object[]> GetPNRsByMileagePlusNumber()
        {
            var requestData = JsonConvert.DeserializeObject<List<MOBPNRByMileagePlusRequest>>(GetFileContent("MOBPNRByMileagePlusRequest.json"));
            yield return new object[] { requestData[0] };
        }
        public static IEnumerable<object[]> RequestReceiptByEmail()
        {
            var requestData = JsonConvert.DeserializeObject<List<MOBReceiptByEmailRequest>>(GetFileContent("MOBReceiptByEmailRequest.json"));
            yield return new object[] { requestData[0] };
        }

    }
}
