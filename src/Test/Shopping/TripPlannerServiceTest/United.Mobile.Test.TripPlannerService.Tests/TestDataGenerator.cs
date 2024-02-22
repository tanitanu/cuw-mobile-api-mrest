using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using United.Utility.Helper;

namespace United.Mobile.Test.TripPlannerService.Tests
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
            var persistedReservation1Json = TestDataGenerator.GetFileContent(filename);
            return JsonConvert.DeserializeObject<T>(persistedReservation1Json);
        }

        public static IEnumerable<object[]> AddTripPlanVoting_Request()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set1();
        }

        public static IEnumerable<object[]> DeleteTripPlan_Request()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set2();
        }

        public static IEnumerable<object[]> DeleteTripPlanVoting_Request()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set3();
        }

    }
}
