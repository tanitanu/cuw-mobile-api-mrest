using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Bundles;
using United.Mobile.Shopping.ShopFlightDetails.Tests;
using United.Services.FlightShopping.Common;
using United.Utility.Helper;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.Shopping.Shopping.Test
{
    public class ShoppingTestDataGenerator
    {
        public static string GetFileContent(string fileName)
        {
            fileName = string.Format("..\\..\\..\\TestData\\{0}", fileName);
            var path = Path.IsPathRooted(fileName) ? fileName : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }
        public static T GetXmlData<T>(string filename)
        {
            var persistedReservation1Json = ShoppingTestDataGenerator.GetFileContent(filename);
            return XmlSerializerHelper.Deserialize<T>(persistedReservation1Json);
        }

        public static T GetJsonData<T>(string filename)
        {
            var persistedReservation1Json = ShoppingTestDataGenerator.GetFileContent(filename);
            return JsonConvert.DeserializeObject<T>(persistedReservation1Json);
        }

        public static IEnumerable<object[]> GetShop_Request()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set1();
        }

        public static IEnumerable<object[]> GetShop_Request1()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set1_1();
        }

        public static IEnumerable<object[]> GetShop_Request2()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set1_1_1();
            yield return testDataSet.set1_1_2();
        }
        public static IEnumerable<object[]> GetShop_Request2_1()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set1_1_3();
        }

        public static IEnumerable<object[]> GetShop_Request2_2()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set1_1_4();
        }

        public static IEnumerable<object[]> GetShopRequest_Request()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set4();
        }
        public static IEnumerable<object[]> OrganizeShopResults_Request()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set2();
            yield return testDataSet.set2_1();

        }

        public static IEnumerable<object[]> OrganizeShopResults_Request1()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set2_2();

        }

        public static IEnumerable<object[]> OrganizeShopResults_Request1_1()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set2_3();

        }

        public static IEnumerable<object[]> OrganizeShopResults_flow()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set2_4();
        }

        public static IEnumerable<object[]> ShopCLBOptOut_Request()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set3();
        }

        public static IEnumerable<object[]> SelectTrip_Request()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set5();
            yield return testDataSet.set6();
            
        }
        //public static IEnumerable<object[]> SelectTrip_Test_Change()
        //{
        //    ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
        //    yield return testDataSet.set6();
        //}

        public static IEnumerable<object[]> SelectTrip_Request2()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set5();
        }

        public static IEnumerable<object[]> SelectTrip_Request3()
        {
           ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
           yield return testDataSet.set7();
        }

        public static IEnumerable<object[]> SelectTrip_Request4()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set8();
            // yield return testDataSet.set14();
        }

        public static IEnumerable<object[]> SelectTrip_Request4_1()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set9();
            // yield return testDataSet.set14();
        }

        public static IEnumerable<object[]> ChasePromoRedirectRequests()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            var return1 = testDataSet.set15();
            yield return return1;
           // yield return testDataSet.set15();

        }
        public static IEnumerable<object[]> GetProductInfoForFSRDRequests()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            var return2 = testDataSet.set16();
           // var return4 = testDataSet.set17();
            yield return return2;
           // yield return return4;
        }

        public static IEnumerable<object[]> GetShopTripPlan_Request()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set18();
        }

        public static IEnumerable<object[]> GetShopTripPlan_Request1()
        {
            ShoppingTestDataSet testDataSet = new ShoppingTestDataSet();
            yield return testDataSet.set18_1();
            yield return testDataSet.set18_2();
        }


    }
}
