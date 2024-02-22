using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Bundles;
using United.Services.FlightShopping.Common;
using United.Utility.Helper;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.Shopping.ShopFlightDetails.Tests
{
    public class TestDataGenerator
    {
        public static string GetFileContent(string fileName)
        {
            fileName = string.Format("..\\..\\..\\TestData\\{0}", fileName);
            var path = Path.IsPathRooted(fileName) ? fileName : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }
        public static T GetXmlData<T>(string filename)
        {

            var persistedReservation1Json = TestDataGenerator.GetFileContent(filename);
            return XmlSerializerHelper.Deserialize<T>(persistedReservation1Json);

        }

        public static T GetJsonData<T>(string filename)
        {

            var OnTimePerformanceRequestJson = TestDataGenerator.GetFileContent(filename);
            return JsonConvert.DeserializeObject<T>(OnTimePerformanceRequestJson);

        }


        public static IEnumerable<object[]> GetFlifoScheduleRequests()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set1();
            yield return testDataSet.set2();
            yield return testDataSet.set3();

        }
        public static IEnumerable<object[]> GetTeaserPage_Request()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set4();
        }

        public static IEnumerable<object[]> GetTeaserPage_Request1()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set5();
        }
    }
}
