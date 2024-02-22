using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Travelers;
using United.Services.FlightShopping.Common.FlightReservation;

namespace United.Mobile.Test.Travelers.Tests
{
    class Input
    {
        public static string GetFileContent(string fileName)
        {
            var path = Path.IsPathRooted(fileName) ? fileName
                                                : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }
        public static IEnumerable<object[]> MobileCMSContent()
        {
            var contentRequestData = GetFileContent("MobileCMSContentRequest.json");
            var contentRequestList = JsonConvert.DeserializeObject<List<MobileCMSContentRequest>>(contentRequestData);
            var shoppingSession = GetFileContent("ShoppingSession.json");
            var sessionObj = JsonConvert.DeserializeObject<List<Session>>(shoppingSession);
            var shoppingReservation = GetFileContent("Reservation.json");
            var reservationObj = JsonConvert.DeserializeObject<List<Model.Shopping.Reservation>>(shoppingReservation);


            yield return new object[] { sessionObj[0], reservationObj[0], contentRequestList[0] };
            yield return new object[] { sessionObj[0], reservationObj[1], contentRequestList[0] };
            yield return new object[] { sessionObj[0], reservationObj[2], contentRequestList[0] };
            yield return new object[] { sessionObj[0], reservationObj[0], contentRequestList[1] };


        }

        public static IEnumerable<object[]> TravelerInput()
        {
            var shoppingSession = GetFileContent("ShoppingSession.json");
            var sessionObj = JsonConvert.DeserializeObject<List<Session>>(shoppingSession);
            var shoppingReservation = GetFileContent("ShoppingReservation.json");
            var reservationObj = JsonConvert.DeserializeObject<List<Model.Shopping.Reservation>>(shoppingReservation);
          //  var reservation = JsonConvert.DeserializeObject<List<Model.Shopping.Reservation>>(GetFileContent("Reservation.json"));
            var request = JsonConvert.DeserializeObject<List<MOBMPNameMissMatchRequest>>(GetFileContent("MOBMPNameMissMatchRequest.json"));
            var response = JsonConvert.DeserializeObject<List<MOBMPNameMissMatchResponse>>(GetFileContent("MOBMPNameMissMatchResponse.json"));
            var mOBShoppingCart = GetFileContent("MOBShoppingCart.json");
            var mOBShoppingCartObj = JsonConvert.DeserializeObject<List<MOBShoppingCart>>(mOBShoppingCart);
           yield return new object[] { sessionObj[3], request[0], response[0], reservationObj[0], true , mOBShoppingCartObj[0]};
           yield return new object[] { sessionObj[3], request[2], response[1], reservationObj[0], true , mOBShoppingCartObj[0] };
           yield return new object[] { sessionObj[3], request[2], response[2], reservationObj[0], true , mOBShoppingCartObj[0] };
           yield return new object[] { sessionObj[3], request[2], response[2], reservationObj[5], true , mOBShoppingCartObj[0] };
           yield return new object[] { sessionObj[3], request[1], response[0], reservationObj[3], true , mOBShoppingCartObj[0] };
            yield return new object[] { sessionObj[3], request[1], response[0], reservationObj[4], false , mOBShoppingCartObj[0] };
        }

        public static IEnumerable<object[]> TravelerInput1()
        {
            //var request = JsonConvert.DeserializeObject<List<MOBRegisterTravelersRequest>>(GetFileContent("MOBRegisterTravelersRequest.json"));

            var requestjson = GetFileContent("MOBRegisterTravelersRequest.json");
            var request = JsonConvert.DeserializeObject<List<MOBRegisterTravelersRequest>>(requestjson);


            var response = JsonConvert.DeserializeObject<List<MOBRegisterTravelersResponse>>(GetFileContent("MOBRegisterTravelersResponse.json"));
            var reservationObj = JsonConvert.DeserializeObject<List<Model.Shopping.Reservation>>(GetFileContent("Reservation.json"));
            var shoppingSession = GetFileContent("ShoppingSession.json");
            var sessionObj = JsonConvert.DeserializeObject<List<Session>>(shoppingSession);
            var shoppingCart = JsonConvert.DeserializeObject<List<MOBShoppingCart>>(GetFileContent("MOBShoppingCart.json"));
            var Information = JsonConvert.DeserializeObject<List<FlightReservationResponse>>(GetFileContent("FlightReservationResponse.json"));
            var registerTravelersRequestjson = GetFileContent("RegisterTravelersRequest.json");
            var registerTravelersRequest = JsonConvert.DeserializeObject<RegisterTravelersRequest>(registerTravelersRequestjson);

            var fOPResponsejson = GetFileContent("FOPResponse.json");
            var fOPResponse = JsonConvert.DeserializeObject<List<FOPResponse>>(fOPResponsejson);




            //yield return new object[] { request[0], response[0], reservationObj[0], sessionObj[0], true, false, Information[1], shoppingCart[0], "" , registerTravelersRequest ,fOPResponse[0]};
            yield return new object[] { request[0], response[0], reservationObj[0], sessionObj[0], true, false, Information[2], shoppingCart[0], "" , registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[7], response[0], reservationObj[0], sessionObj[0], true, false, Information[2], shoppingCart[0], "PSL" , registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[7], response[0], reservationObj[0], sessionObj[0], true, false, Information[2], shoppingCart[0], "PZA" , registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[7], response[0], reservationObj[0], sessionObj[0], true, false, Information[2], shoppingCart[0], "ASA" , registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[7], response[0], reservationObj[0], sessionObj[0], true, false, Information[2], shoppingCart[0], "EPU" , registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[1], response[0], reservationObj[0], sessionObj[0], true, false, Information[0], shoppingCart[0], "" , registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[1], response[0], reservationObj[0], sessionObj[0], true, false, Information[1], shoppingCart[0], "" , registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[3], response[0], reservationObj[7], sessionObj[0], false, false, Information[1], shoppingCart[0], "" , registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[1], response[0], reservationObj[8], sessionObj[0], false, false, Information[1], shoppingCart[0], "" , registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[1], response[0], reservationObj[6], sessionObj[0], true, false, Information[1], shoppingCart[0], "", registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[4], response[0], reservationObj[8], sessionObj[0], false, false, Information[1], shoppingCart[0], "" , registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[4], response[0], reservationObj[8], sessionObj[0], false, true, Information[1], shoppingCart[1], "" , registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[4], response[0], reservationObj[8], sessionObj[0], false, false, Information[1], shoppingCart[1], "" , registerTravelersRequest, fOPResponse[0] };
            //yield return new object[] { request[4], response[0], reservationObj[8], sessionObj[0], true, true, Information[1], shoppingCart[0], "" , registerTravelerRequest,fOPResponse[0] };
            yield return new object[] { request[0], response[0], reservationObj[3], sessionObj[0], true, false, Information[1], shoppingCart[0], "" , registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[1], response[0], reservationObj[3], sessionObj[0], true, false, Information[1], shoppingCart[0], "" , registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[2], response[0], reservationObj[3], sessionObj[0], true, false, Information[1], shoppingCart[0], "" , registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[2], response[0], reservationObj[4], sessionObj[0], true, false, Information[1], shoppingCart[0], "" , registerTravelersRequest, fOPResponse[0] };
            yield return new object[] { request[0], response[0], reservationObj[5], sessionObj[0], false, false, Information[1], shoppingCart[0], "" , registerTravelersRequest, fOPResponse[0] };

        }
    }
}
