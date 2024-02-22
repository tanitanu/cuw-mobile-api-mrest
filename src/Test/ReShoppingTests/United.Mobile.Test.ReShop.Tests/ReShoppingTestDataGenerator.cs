using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using United.Utility.Helper;

namespace United.Mobile.Test.ReShop.Tests
{
    public class ReShoppingTestDataGenerator
    {
        public static string GetFileContent(string fileName)
        {
            fileName = string.Format("..\\..\\..\\TestData\\{0}", fileName);
            var path = Path.IsPathRooted(fileName) ? fileName : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }

        public static T GetXmlData<T>(string filename)
        {
            var persistedReservation1Json = ReShoppingTestDataGenerator.GetFileContent(filename);
            return XmlSerializerHelper.Deserialize<T>(persistedReservation1Json);
        }

        public static T GetJsonData<T>(string filename)
        {
            var persistedReservation1Json = ReShoppingTestDataGenerator.GetFileContent(filename);
            return JsonConvert.DeserializeObject<T>(persistedReservation1Json);
        }


        public static IEnumerable<object[]> ReShop_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set1();
        }
        public static IEnumerable<object[]> ChangeEligibleCheck_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set2();
        }
        public static IEnumerable<object[]> ChangeEligibleCheckAndReshop_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set3();
        }
        public static IEnumerable<object[]> ReshopSaveEmail_CFOP_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set4();
        }
        public static IEnumerable<object[]> SelectTrip_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set5();
        }
        public static IEnumerable<object[]> GetProducts_CFOP_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set6();
        }
        public static IEnumerable<object[]> ConfirmScheduleChange_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set7();
        }
        public static IEnumerable<object[]> ChangeEligibleCheck1_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set8();
        }
        public static IEnumerable<object[]> ChangeEligibleCheckAndReshop1_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set9();
        }
        public static IEnumerable<object[]> ChangeEligibleCheck2_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set10();
        }
        public static IEnumerable<object[]> ChangeEligibleCheck_negative_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set11();
        }
        public static IEnumerable<object[]> ChangeEligibleCheck_negative1_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set12();
        }
        public static IEnumerable<object[]> ChangeEligibleCheck_negative2_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set13();
        }
        public static IEnumerable<object[]> ChangeEligibleCheck_negative3_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set14();
        }
        public static IEnumerable<object[]> ChangeEligibleCheckAndReshop_negative_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set15();
        }
        public static IEnumerable<object[]> ReshopSaveEmail_CFOP2_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set16();
        }
        public static IEnumerable<object[]> SelectTrip_negative_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set17();
        }
        public static IEnumerable<object[]> ChangeEligibleCheckAndReshop2_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set18();
        }
        public static IEnumerable<object[]> ChangeEligibleCheck3_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set19();
        }
        public static IEnumerable<object[]> ChangeEligibleCheckAndReshop3_Test()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set20();
        }

        public static IEnumerable<object[]> GetProducts_CFOP_Flow()
        {
            ReShoppingTestDataSet testDataSet = new ReShoppingTestDataSet();
            yield return testDataSet.set21();
        }
    }
}
