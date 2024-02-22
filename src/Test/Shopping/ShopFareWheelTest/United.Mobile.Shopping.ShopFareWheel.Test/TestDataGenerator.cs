using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using United.Utility.Helper;

namespace United.Mobile.Shopping.ShopFareWheel.Test
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


        public static IEnumerable<object[]> GetFareWheelListResponse_Request()
        {
            TestDataSet testDataSet = new TestDataSet();
            //yield return testDataSet.set1();
            yield return testDataSet.set1_1();

        }

        public static IEnumerable<object[]> GetFareWheelListResponse_Request1()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set1_2();

        }

        public static IEnumerable<object[]> GetFareWheelListResponse_flow()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set1_3();

        }

        public static IEnumerable<object[]> GetShopFareWheelListResponse_Request()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set2();
            //yield return testDataSet.set3();
            //yield return testDataSet.set4();

        }

        public static IEnumerable<object[]> GetShopFareWheelListResponse_flow()
        {
            TestDataSet testDataSet = new TestDataSet();
            yield return testDataSet.set2_1();
            

        }



    }
}
