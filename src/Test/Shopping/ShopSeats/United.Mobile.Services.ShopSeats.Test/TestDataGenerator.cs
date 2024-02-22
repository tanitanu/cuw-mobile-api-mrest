using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using United.Utility.Helper;

namespace United.Mobile.Services.ShopSeats.Test
{
    class TestDataGenerator
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


        //public static IEnumerable<object[]> GetFlifoScheduleRequests()
        //{
        //    TestDataSet testDataSet = new TestDataSet();
        //    var return1 = testDataSet.set1();
        //    yield return return1;

        //}
        public static IEnumerable<object[]> SelectSeatsRequest()
        {
            TestDataSet testDataSet = new TestDataSet();
            var return1 = testDataSet.set1();
            yield return return1;
        }

        public static IEnumerable<object[]> SelectSeatsRequest1()
        {
            TestDataSet testDataSet = new TestDataSet();
            var return1 = testDataSet.set2();
            yield return return1;
        }

        public static IEnumerable<object[]> SelectSeatsRequest2()
        {
            TestDataSet testDataSet = new TestDataSet();
            var return1 = testDataSet.set3();
            yield return return1;
        }

        public static IEnumerable<object[]> SelectSeats_flow()
        {
            TestDataSet testDataSet = new TestDataSet();
            var return1 = testDataSet.set1_1();
            yield return return1;
        }
    }
}
