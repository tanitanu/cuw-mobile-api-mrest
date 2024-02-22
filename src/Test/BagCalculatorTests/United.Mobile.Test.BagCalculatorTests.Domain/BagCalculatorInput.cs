using MerchandizingServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using United.Definition;
using United.Mobile.Model.BagCalculator;
using United.Mobile.Model.Common;
using United.Services.FlightShopping.Common.Cart;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.Test.BagCalculatorTests.Domain
{
    public class BagCalculatorInput
    {
        public static string GetFileContent(string fileName)
        {
            var path = Path.IsPathRooted(fileName) ? fileName
                                                : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }
        public static IEnumerable<object[]> MobileCMSContent()
        {
            var contentRequestData = GetFileContent(@"SampleData\MobileCMSContentRequest.json");
            var contentRequestList = JsonConvert.DeserializeObject<List<MobileCMSContentRequest>>(contentRequestData);
            var contentResponseData = GetFileContent(@"SampleData\MobileCMSContentResponse.json");
            var contentResponseList = JsonConvert.DeserializeObject<List<MobileCMSContentResponse>>(contentResponseData);
            var shoppingSession = BagCalculatorInput.GetFileContent(@"SampleData\ShoppingSession.json");
            var sessionObj = JsonConvert.DeserializeObject<List<Session>>(shoppingSession);
            var shoppingReservation = BagCalculatorInput.GetFileContent(@"SampleData\ShoppingReservation.json");
            var reservationObj = JsonConvert.DeserializeObject<Reservation>(shoppingReservation);
            var sortedCMSContentData = GetFileContent(@"SampleData\SortedCMSContent.json");
            var sortedCMSContentList = JsonConvert.DeserializeObject<List<List<MobileCMSContentMessages>>>(sortedCMSContentData);


            yield return new object[] { sessionObj[0], reservationObj, contentRequestList[0], contentResponseList[0], sortedCMSContentList[0] };
            yield return new object[] { sessionObj[0], reservationObj, contentRequestList[1], contentResponseList[0], sortedCMSContentList[0] };
            yield return new object[] { sessionObj[1], reservationObj, contentRequestList[2], contentResponseList[0], sortedCMSContentList[0] };


        }

        public static IEnumerable<object[]> SampleDataRequest1()
        {
            var contentRequestData = GetFileContent(@"..\..\..\SampleData\MobileCMSContentRequest_V2.json");
            var contentRequestList = JsonConvert.DeserializeObject<List<MobileCMSContentRequest>>(contentRequestData);
            yield return contentRequestList.ToArray();
        }

        //-----------------------------------myadds

        public static IEnumerable<object[]> MobileCMSContent_TnC()
        {
            var contentRequestData = GetFileContent(@"SampleData\MobileCMSContentRequest.json");
            var contentRequestList = JsonConvert.DeserializeObject<List<MobileCMSContentRequest>>(contentRequestData);

            var contentResponseData = GetFileContent(@"SampleData\MobileCMSContentResponse.json");
            var contentResponseList = JsonConvert.DeserializeObject<List<MobileCMSContentResponse>>(contentResponseData);

            var shoppingSession = BagCalculatorInput.GetFileContent(@"SampleData\ShoppingSession.json");
            var sessionObj = JsonConvert.DeserializeObject<List<Session>>(shoppingSession);

            var sortedCMSContentData = GetFileContent(@"SampleData\SortedCMSContent.json");
            var sortedCMSContentList = JsonConvert.DeserializeObject<List<List<MobileCMSContentMessages>>>(sortedCMSContentData);

            var shoppingReservation = BagCalculatorInput.GetFileContent(@"SampleData\ShoppingReservation.json");
            var reservationObj = JsonConvert.DeserializeObject<Reservation>(shoppingReservation);
            yield return new object[] { sessionObj[0], reservationObj, contentRequestList[0], contentResponseList[0], sortedCMSContentList[0] };
            yield return new object[] { sessionObj[1], reservationObj, contentRequestList[0], contentResponseList[0], sortedCMSContentList[0] };
        }

        public static IEnumerable<object[]> GetMobileCMSContentsData_Testingflow()
        {
            var mobileCMSContentRequestJson = GetFileContent(@"ReshopFlow\MobileCMSContentRequest.json");
            var mobileCMSContentRequest = JsonConvert.DeserializeObject<List<MobileCMSContentRequest>>(mobileCMSContentRequestJson);

            var mobileCMSContentResponseJson = GetFileContent(@"ReshopFlow\MobileCMSContentResponse.json");
            var mobileCMSContentResponse = JsonConvert.DeserializeObject<List<MobileCMSContentResponse>>(mobileCMSContentResponseJson);

            var shoppingSession = BagCalculatorInput.GetFileContent(@"SampleData\ShoppingSession.json");
            var sessionObj = JsonConvert.DeserializeObject<List<Session>>(shoppingSession);

            var shoppingReservation = BagCalculatorInput.GetFileContent(@"SampleData\ShoppingReservation.json");
            var reservationObj = JsonConvert.DeserializeObject<List<Reservation>>(shoppingReservation);

            var mobileCMSContentMessagesJson = GetFileContent(@"SampleData\SortedCMSContent.json");
            var mobileCMSContentMessages = JsonConvert.DeserializeObject<List<List<MobileCMSContentMessages>>>(mobileCMSContentMessagesJson);

            yield return new object[] { mobileCMSContentRequest[1], mobileCMSContentResponse[0], sessionObj[0], reservationObj[1], mobileCMSContentMessages[0] };
            yield return new object[] { mobileCMSContentRequest[1], mobileCMSContentResponse[0], sessionObj[0], reservationObj[1], mobileCMSContentMessages[0] };
            yield return new object[] { mobileCMSContentRequest[2], mobileCMSContentResponse[0], sessionObj[1], reservationObj[1], mobileCMSContentMessages[0] };

        }
    }

}
