using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.Shopping;
using United.Mobile.Model.Internal.AccountManagement;
using United.Mobile.Model.UpdateMemberProfile;
using United.Service.Presentation.SecurityResponseModel;

namespace United.Mobile.Test.UpdateMProfileTests.Api
{
    public class TestDataSet
    {
        public Object[] set1()
        {

            var mOBUpdateProfileOwnerFOPRequestJson = TestDataGenerator.GetFileContent(@"ManageResFlow\UpdateProfileOwnerCardInfoRequest.json");
            var mOBUpdateProfileOwnerFOPRequest = JsonConvert.DeserializeObject<List<MOBUpdateProfileOwnerFOPRequest>>(mOBUpdateProfileOwnerFOPRequestJson);

            var mOBUpdateTravelerRequestJson = TestDataGenerator.GetFileContent(@"ManageResFlow\MOBUpdateTravelerRequest.json");
            var mOBUpdateTravelerRequest = JsonConvert.DeserializeObject<List<MOBUpdateTravelerRequest>>(mOBUpdateTravelerRequestJson);

            var sessionjson = TestDataGenerator.GetFileContent(@"ManageResFlow\Session.json");
            var session = JsonConvert.DeserializeObject<Session>(sessionjson);

            var mOBItemsJson = TestDataGenerator.GetFileContent("MOBItem.json");
            var mOBItems = JsonConvert.DeserializeObject<List<MOBItem>>(mOBItemsJson);

            var pKDispenserResponseJson = TestDataGenerator.GetFileContent(@"ManageResFlow\PKDispenserResponse.json");
            var  pKDispenserResponse = JsonConvert.DeserializeObject<List<PKDispenserResponse>>(pKDispenserResponseJson);

            var formofPaymentOptionsJson = TestDataGenerator.GetFileContent(@"ManageResFlow\FormofPaymentOption.json");
            var formofPaymentOptions = JsonConvert.DeserializeObject<List<List<FormofPaymentOption>>>(formofPaymentOptionsJson);

            return new object[] { mOBUpdateProfileOwnerFOPRequest[0], mOBUpdateTravelerRequest[0], session, mOBItems, pKDispenserResponse[0], formofPaymentOptions[0] };
        }

        public Object[] set1_1()
        {

            var mOBUpdateProfileOwnerFOPRequestJson = TestDataGenerator.GetFileContent(@"ManageResFlow\UpdateProfileOwnerCardInfoRequest.json");
            var mOBUpdateProfileOwnerFOPRequest = JsonConvert.DeserializeObject<List<MOBUpdateProfileOwnerFOPRequest>>(mOBUpdateProfileOwnerFOPRequestJson);

            var mOBUpdateTravelerRequestJson = TestDataGenerator.GetFileContent(@"ManageResFlow\MOBUpdateTravelerRequest.json");
            var mOBUpdateTravelerRequest = JsonConvert.DeserializeObject<List<MOBUpdateTravelerRequest>>(mOBUpdateTravelerRequestJson);

            var sessionjson = TestDataGenerator.GetFileContent(@"ManageResFlow\Session.json");
            var session = JsonConvert.DeserializeObject<Session>(sessionjson);

            var mOBItemsJson = TestDataGenerator.GetFileContent("MOBItem.json");
            var mOBItems = JsonConvert.DeserializeObject<List<MOBItem>>(mOBItemsJson);

            var pKDispenserResponseJson = TestDataGenerator.GetFileContent(@"ManageResFlow\PKDispenserResponse.json");
            var pKDispenserResponse = JsonConvert.DeserializeObject<List<PKDispenserResponse>>(pKDispenserResponseJson);

            var formofPaymentOptionsJson = TestDataGenerator.GetFileContent(@"ManageResFlow\FormofPaymentOption.json");
            var formofPaymentOptions = JsonConvert.DeserializeObject<List<List<FormofPaymentOption>>>(formofPaymentOptionsJson);

            return new object[] { mOBUpdateProfileOwnerFOPRequest[1], mOBUpdateTravelerRequest[0], session, mOBItems, pKDispenserResponse[0], formofPaymentOptions[0] };
        }

        public Object[] set2()
        {

            var mOBUpdateCustomerFOPRequestJson = TestDataGenerator.GetFileContent(@"ManageResFlow\UpdateProfileOwnerCardInfoRequest.json");
            var mOBUpdateCustomerFOPRequest = JsonConvert.DeserializeObject<List<MOBUpdateCustomerFOPRequest>>(mOBUpdateCustomerFOPRequestJson);

            //var updateProfileOwnerCardInfoRequestJson = TestDataGenerator.GetFileContent(@"ManageResFlow\UpdateProfileOwnerCardInfoRequest.json");
            //var updateProfileOwnerCardInfoRequest = JsonConvert.DeserializeObject<List<MOBUpdateProfileOwnerFOPRequest>>(updateProfileOwnerCardInfoRequestJson);

            var sessionjson = TestDataGenerator.GetFileContent("SessionData.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var profileFOPCreditCardResponseJson = TestDataGenerator.GetFileContent(@"ManageResFlow\UpdateProfileOwnerCardInfoResponse.json");
            var profileFOPCreditCardResponse = JsonConvert.DeserializeObject<List < ProfileFOPCreditCardResponse >>(profileFOPCreditCardResponseJson);

            var hashPinValidateJson = TestDataGenerator.GetFileContent(@"ManageResFlow\HashPinValidate.json");
            var hashPinValidate = JsonConvert.DeserializeObject<HashPinValidate>(hashPinValidateJson);

            //var mOBUpdateTravelerInfoResponseJson = TestDataGenerator.GetFileContent("MOBUpdateTravelerInfoResponse.json");
            //var mOBUpdateTravelerInfoResponse = JsonConvert.DeserializeObject<MOBUpdateTravelerInfoResponse>(mOBUpdateTravelerInfoResponseJson);

            var mOBItemsJson = TestDataGenerator.GetFileContent("MOBItem.json");
            var mOBItems = JsonConvert.DeserializeObject<List<MOBItem>>(mOBItemsJson);

            return new object[] { mOBUpdateCustomerFOPRequest[2], session[2], profileFOPCreditCardResponse[0], hashPinValidate, mOBItems };
        }
    }
}
