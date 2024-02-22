﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using United.Utility.Helper;

namespace United.Mobile.Shopping.UnfinishedBooking.Tests
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
        public static IEnumerable<object[]> GetUnfinishedBookings_Request()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set1();
        }
        public static IEnumerable<object[]> SelectUnfinishedBooking_Request()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set2();
            yield return testDataSet.set4();

        }
        public static IEnumerable<object[]> ClearUnfinishedBookings_Request()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set3();
        }
        public static IEnumerable<object[]> GetOmniCartSavedTrips_Test()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set7();
        }
        public static IEnumerable<object[]> SelectOmniCartSavedTrip_Request()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set5();
        }
        public static IEnumerable<object[]> RemoveOmniCartSavedTrip_Request()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set6();
        }

    }
}
