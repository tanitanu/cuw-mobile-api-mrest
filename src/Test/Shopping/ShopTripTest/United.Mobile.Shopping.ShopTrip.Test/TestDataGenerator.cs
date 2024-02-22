using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using United.Utility.Helper;

namespace United.Mobile.Shopping.ShopTrip.Test
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

        public static IEnumerable<object[]> SelectTrip_Request()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set1();
            yield return testDataSet.set2();
            yield return testDataSet.set3();
            yield return testDataSet.set5();
            yield return testDataSet.set6();
            yield return testDataSet.set8();
            yield return testDataSet.set9();
            yield return testDataSet.set10();
            yield return testDataSet.set11();
            yield return testDataSet.set12();




        }
        public static IEnumerable<object[]> SelectTrip_Request2()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set4();

        }

        public static IEnumerable<object[]> SelectTrip_Request3()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set7();

        }

        public static IEnumerable<object[]> SelectTrip_Request4()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set13();
            yield return testDataSet.set14();

        }

        public static IEnumerable<object[]> GetTripCompareFareTypesRequest()
        {
            TestDataSet testDataSet = new TestDataSet();
            var return1 = testDataSet.GetTripCompareFareTypesRequestSet1();
            yield return return1;
        }

        public static IEnumerable<object[]> MetaSelectTripRequests()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.metaSelectTripSet();
        }

        public static IEnumerable<object[]> MetaSelectTripRequests1()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.metaSelectTripSet1();
        }

        public static IEnumerable<object[]> MetaSelectTripRequests2()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.metaSelectTripSet1_1();
            yield return testDataSet.metaSelectTripSet1_2();
        }

        public static IEnumerable<object[]> GetFareRulesForSelectedTripsRequest()
        {
            TestDataSet testDataSet = new TestDataSet();
            var return1 = testDataSet.GetFareRulesForSelectedTripsRequestSet1();
            yield return return1;
        }

        public static IEnumerable<object[]> GetFareRulesForSelectedTrip_flow()
        {
            TestDataSet testDataSet = new TestDataSet();
            var return1 = testDataSet.GetFareRulesForSelectedTripsRequestSet2();
            yield return return1;
        }

        public static IEnumerable<object[]> GetShareTripRequest()
        {
            TestDataSet testDataSet = new TestDataSet();
            var return1 = testDataSet.GetShareTripRequestSet1();
            yield return return1;
        }

    }
}
