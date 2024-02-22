using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using United.Definition;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.Test.MemberProfileTests.Api
{
    public class Input
    {
        private static string GetFileContent(string fileName)
        {
            var path = Path.IsPathRooted(fileName) ? fileName
                                                : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }
        public static IEnumerable<object[]> InputMemberProfile()
        {
            var filename = GetFileContent("Request.json");
            var List = JsonConvert.DeserializeObject<List<MOBCustomerPreferencesRequest>>(filename);

            yield return new object[] { List[0] };
            yield return new object[] {List[1] };
        }
        public static IEnumerable<object[]> InputMemberProfile1()
        {
            var filename = GetFileContent("GetAccountSummaryRequest.json");
            var List = JsonConvert.DeserializeObject<List<MPAccountValidationRequest>>(filename);
            var file = GetFileContent("GetAccountSummaryResponse.json");
            var Data= JsonConvert.DeserializeObject<List<MPAccountSummaryResponse>>(file);

            yield return new object[] { true,List[0], Data[0] };
            yield return new object[] { false,List[1], Data[1] };
            yield return new object[] { true, List[1], Data[1] };
            yield return new object[] { true, List[1], Data[2] };
        }
        public static IEnumerable<object[]> InputMemberProfile2()
        {
          
            var file = GetFileContent("MOBContactUsRequest.json");
            var Data = JsonConvert.DeserializeObject<List<MOBContactUsRequest>>(file);
            yield return new object[] { Data[0] };
            yield return new object[] { Data[1] };
            yield return new object[] { Data[2] };
            yield return new object[] { Data[3] };
            yield return new object[] { Data[4] };
            yield return new object[] { Data[5] };
        }
        public static IEnumerable<object[]> InputMemberProfile3()
        {
            var Data = GetFileContent("MOBCustomerProfileRequest.json");
            var requestPayload = JsonConvert.DeserializeObject<List<MOBCustomerProfileRequest>>(Data);
            yield return new object[] { true, true, requestPayload[0] };
            yield return new object[] { true, false, requestPayload[0] };
            yield return new object[] { false, false, requestPayload[0] };
            yield return new object[] { false, true, requestPayload[0] };
            yield return new object[] { true, true, requestPayload[1] };
            yield return new object[] { true, false, requestPayload[1] };
            yield return new object[] { false, false, requestPayload[1] };
            yield return new object[] { false, true, requestPayload[1] };
            yield return new object[] { true, true, requestPayload[2] };
            yield return new object[] { true, true, requestPayload[3] };

        }

       

        public static IEnumerable<object[]> InputMemberProfile4()
        {
            var Data = GetFileContent("MOBCustomerProfileRequest.json");
            var requestPayload = JsonConvert.DeserializeObject<List<MOBCustomerProfileRequest>>(Data);
            yield return new object[] { true, true, requestPayload[0] };
            yield return new object[] { true, false, requestPayload[0] };
            yield return new object[] { false, false, requestPayload[0] };
            yield return new object[] { false, true, requestPayload[0] };
            yield return new object[] { false, true, requestPayload[1] };
            //yield return new object[] { true, true, requestPayload[2] };
            yield return new object[] { true, true, requestPayload[3] };

        }


    }
}
