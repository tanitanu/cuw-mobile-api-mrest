using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using United.Mobile.Model.PostBooking;

namespace United.Mobile.Test.PostBooking.Tests

{
    class TestDataGenerator
    {
        public static string GetFileContent(string fileName)
        {
            fileName = string.Format("..\\..\\..\\TestData\\{0}", fileName);
            var path = Path.IsPathRooted(fileName) ? fileName : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }
        public static IEnumerable<object[]> GetOffers()
        {
            var shopGetoffersRequest = JsonConvert.DeserializeObject<List<MOBSHOPGetOffersRequest>>(GetFileContent("shopGetoffersRequest.json"));

            yield return new object[] { shopGetoffersRequest[0] };
        }
        public static IEnumerable<object[]> GetOffers1()
        {
            var shopGetoffersRequest = JsonConvert.DeserializeObject<List<MOBSHOPGetOffersRequest>>(GetFileContent("shopGetoffersRequest1.json"));

            yield return new object[] { shopGetoffersRequest[0] };
        }

    }
}
