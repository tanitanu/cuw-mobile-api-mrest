using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Shopping.ShopAward.Tests;
using United.Utility.Helper;

namespace United.Mobile.Shopping.ShopAward.Test
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
        public static IEnumerable<object[]> RevenueLowestPriceForAwardSearch_Request()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set1();
        }
        public static IEnumerable<object[]> GetSelectTripAwardCalendar_Request()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set2();
        }

        public static IEnumerable<object[]> GetSelectTripAwardCalendar_Request1()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set4();
        }

        public static IEnumerable<object[]> GetSelectTripAwardCalendar_flow()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set4_2();
        }

        public static IEnumerable<object[]> GetSelectTripAwardCalendar_Exception()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set4_1();
        }

        public static IEnumerable<object[]> GetShopAwardCalendar_Request()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set3();
        }

        public static IEnumerable<object[]> GetShopAwardCalendar_Request1()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set3_1();
        }

        public static IEnumerable<object[]> GetShopAwardCalendar_flow()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set3_2();
        }



    }
}
