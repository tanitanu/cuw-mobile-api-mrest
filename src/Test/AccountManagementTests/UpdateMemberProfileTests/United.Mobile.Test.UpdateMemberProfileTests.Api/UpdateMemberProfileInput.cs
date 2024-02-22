using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using United.Mobile.Model.Internal.Common;
using United.Mobile.Model.UpdateMemberProfile;

namespace United.Mobile.Test.UpdateMemberProfileTests.Api
{
    class UpdateMemberProfileInput
    {
        private static string GetFileContent(string fileName)
        {
            var path = Path.IsPathRooted(fileName) ? fileName
                                                : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }
        public static IEnumerable<object[]> InputUpdateProfileOwnerCardInfo()
        {
            var filename = GetFileContent("UpdateProfileOwnerCardInfoRequest.json");
            var List = JsonConvert.DeserializeObject<List<MOBUpdateProfileOwnerFOPRequest>>(filename);

            var sessionfile = GetFileContent("SessionData.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionfile);
            //var file = GetFileContent("UpdateProfileOwnerCardInfoResponse.json");
            //var Data = JsonConvert.DeserializeObject<List<MOBUpdateProfileOwnerFOPResponse>>(file);

            yield return new object[] { JsonConvert.SerializeObject(List[0]), JsonConvert.SerializeObject(session[0]) };
            yield return new object[] { JsonConvert.SerializeObject(List[0]), JsonConvert.SerializeObject(session[1]) };
            yield return new object[] { JsonConvert.SerializeObject(List[0]), JsonConvert.SerializeObject(session[2]) };
            yield return new object[] { JsonConvert.SerializeObject(List[0]), JsonConvert.SerializeObject(session[3]) };
            yield return new object[] { JsonConvert.SerializeObject(List[1]), JsonConvert.SerializeObject(session[0]) };
            yield return new object[] { JsonConvert.SerializeObject(List[1]), JsonConvert.SerializeObject(session[1]) };
            yield return new object[] { JsonConvert.SerializeObject(List[1]), JsonConvert.SerializeObject(session[2]) };
            yield return new object[] { JsonConvert.SerializeObject(List[1]), JsonConvert.SerializeObject(session[3]) };
        }
        }
}
