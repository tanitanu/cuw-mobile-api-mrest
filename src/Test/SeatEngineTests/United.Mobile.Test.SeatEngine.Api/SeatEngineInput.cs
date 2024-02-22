using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Common;
using United.Mobile.Model.SeatMapEngine;

namespace United.Mobile.Test.SeatEngine.Api
{
    public class SeatEngineInput
    {
        private static string GetFileContent(string fileName)
        {
            var path = Path.IsPathRooted(fileName) ? fileName
                                                : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }
        public static IEnumerable<object[]> InputPreviewSeatMap()
        {
            var list = JsonConvert.DeserializeObject<List<MOBSeatMapRequest>>(GetFileContent("MOBSeatMapRequest.json"));
            var cslResponse = JsonConvert.DeserializeObject<List<United.Definition.SeatCSL30.SeatMap>>(GetFileContent("cslstrResponse.json"));
            var data = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.OnTimePerformanceInfoResponse>>(GetFileContent("OnTimePerformanceInfoResponse.json"));

            var cabinBrandsjson = GetFileContent("cabinBrands.json");
            var cabinBrands = JsonConvert.DeserializeObject<List<List<CabinBrand>>>(cabinBrandsjson);

            //yield return new object[] { list[0], cslResponse[0], data[0] };
            //yield return new object[] { list[6], cslResponse[0], data[0] };
            //yield return new object[] { list[0], cslResponse[1], data[0] };
            //yield return new object[] { list[7], cslResponse[9], data[0] };
            //yield return new object[] { list[8], cslResponse[10], data[0] };
            yield return new object[] { list[9], cslResponse[10], data[0], cabinBrands[0] };
            //yield return new object[] { list[5], cslResponse[1], data[0] };
            //yield return new object[] { list[4], cslResponse[6], data[0] };
            //yield return new object[] { list[0], cslResponse[1], data[1] };
            //yield return new object[] { list[0], cslResponse[6], data[1] };
            //yield return new object[] { list[0], cslResponse[1], data[2] };
            //yield return new object[] { list[1], cslResponse[0], data[0] };
            //yield return new object[] { list[2], cslResponse[0], data[0] };
            //yield return new object[] { list[3], cslResponse[0], data[0] };
        }

        public static IEnumerable<object[]> InputGetSeatMap()
        {
            var list = JsonConvert.DeserializeObject<List<MOBSeatMapRequest>>(GetFileContent("MOBSeatMapRequest.json"));
            var cslResponse = JsonConvert.DeserializeObject<List<United.Definition.SeatCSL30.SeatMap>>(GetFileContent("cslstrResponse.json"));
            var data = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.OnTimePerformanceInfoResponse>>(GetFileContent("OnTimePerformanceInfoResponse.json"));
            yield return new object[] { data[0], cslResponse[0], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[1], 1, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[1], 1, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "1" };
            yield return new object[] { data[0], cslResponse[1], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "1" };
            yield return new object[] { data[0], cslResponse[7], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[0], 2, "", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[0], 2, "4.1.23", "", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[0], 2, "4.1.23", "ACCESSCODE", "", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[0], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[0], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[0], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[0], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[0], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[0], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[0], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[0], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "" };
            yield return new object[] { data[0], cslResponse[1], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[2], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[3], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[4], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[5], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[1], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[11], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[0], cslResponse[8], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[1], cslResponse[1], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
            yield return new object[] { data[2], cslResponse[1], 2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "UA", "2653", "20210730", "IAH", "ORD", "en-US", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15" };
        }

    }
}
