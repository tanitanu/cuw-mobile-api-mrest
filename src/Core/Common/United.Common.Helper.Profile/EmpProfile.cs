using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper.EmployeeReservation;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.Loyalty;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.Profile;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.CSLModels;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Internal.HomePageContent;
using United.Service.Presentation.PersonModel;
using United.Services.Customer.Common;
using United.Services.FlightShopping.Common.Extensions;
using United.Utility.Helper;
using CslDataVaultResponse = United.Service.Presentation.PaymentResponseModel.DataVault<United.Service.Presentation.PaymentModel.Payment>;
using JsonSerializer = United.Utility.Helper.DataContextJsonSerializer;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using Session = United.Mobile.Model.Common.Session;

namespace United.Common.Helper.Profile
{
    public class EmpProfile : IEmpProfile
    {
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IEmployeeReservations _employeeReservations;
        private readonly ICustomerDataService _customerDataService;
        private readonly IDataVaultService _dataVaultService;
        private readonly ICustomerPreferencesService _customerPreferencesService;
        private readonly ICacheLog<EmpProfile> _logger;
        private readonly IMileagePlus _mileagePlus;
        private readonly IProfileService _profileService;
        private readonly IDPService _dPService;
        private readonly IReferencedataService _referencedataService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ILoyaltyUCBService _loyaltyUCBService;
        private readonly IMPTraveler _mPTraveler;
        private readonly IProfileCreditCard _profileCreditCard;
        private readonly ICustomerProfile _customerProfile;
        private bool IsCorpBookingPath = false;
        private bool IsArrangerBooking = false;

        private string _deviceId = string.Empty;
        private readonly DocumentLibraryDynamoDB _documentLibraryDynamoDB;
        private readonly ICustomerProfileTravelerService _customerProfileTravelerService;

        private MOBApplication _application = new MOBApplication() { Version = new MOBVersion() };
        private readonly IHeaders _headers;
        private readonly IFeatureSettings _featureSettings;

        public EmpProfile(IConfiguration configuration
            , IReferencedataService referencedataService
            , IDynamoDBService dynamoDBService
            , ISessionHelperService sessionHelperService
            , ICustomerDataService mPEnrollmentService
            , IDataVaultService dataVaultService
            , IEmployeeReservations employeeReservations
            , ICacheLog<EmpProfile> logger
            , IMileagePlus mileagePlus
            , IProfileService profileService
            , IDPService dPService
            , ICustomerPreferencesService customerPreferencesService
            , ILoyaltyUCBService loyaltyUCBService
            , IMPTraveler mPTraveler
            , ICustomerProfileTravelerService customerProfileTravelerService
            , IProfileCreditCard profileCreditCard
            , IHeaders headers
            ,ICustomerProfile customerProfile
            , IFeatureSettings featureSettings
            )
        {
            _configuration = configuration;
            _referencedataService = referencedataService;
            _dynamoDBService = dynamoDBService;
            _sessionHelperService = sessionHelperService;
            _customerDataService = mPEnrollmentService;
            _dataVaultService = dataVaultService;
            _employeeReservations = employeeReservations;
            _logger = logger;
            _mileagePlus = mileagePlus;
            _profileService = profileService;
            _dPService = dPService;
            _customerPreferencesService = customerPreferencesService;
            _documentLibraryDynamoDB = new DocumentLibraryDynamoDB(_configuration, _dynamoDBService);
            _loyaltyUCBService = loyaltyUCBService;
            _mPTraveler = mPTraveler;
            _customerProfileTravelerService = customerProfileTravelerService;
            _profileCreditCard = profileCreditCard;
            _headers = headers;
            _customerProfile = customerProfile;
            _featureSettings = featureSettings;
        }

        #region Methods

        public async Task<List<MOBCPProfile>> GetEmpProfile(MOBCPProfileRequest request, bool getEmployeeIdFromCSLCustomerData = false)
        {
            if (_configuration.GetValue<bool>("EnableUCBPhase1_MobilePhase3Changes"))
            {
                return await GetEmpProfileV2(request, getEmployeeIdFromCSLCustomerData);
            }
            if (request == null)
            {
                throw new MOBUnitedException("Profile request cannot be null.");
            }
            List<MOBCPProfile> profiles = null;

            United.Services.Customer.Common.ProfileRequest profileRequest = (new ProfileRequest(_configuration, IsCorpBookingPath)).GetProfileRequest(request, getEmployeeIdFromCSLCustomerData);
            string jsonRequest = JsonSerializer.Serialize<United.Services.Customer.Common.ProfileRequest>(profileRequest);

            string urlPath = string.Format("/GetProfile");

            if (_configuration.GetValue<string>("ForTestingGetttingXMLGetProfileTime") != null && _configuration.GetValue<bool>("ForTestingGetttingXMLGetProfileTime"))
            {
                await GetXMLGetProfileTimeForTesting(request);
            }
            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cslStopWatch;
            cslStopWatch = new Stopwatch();
            cslStopWatch.Reset();
            cslStopWatch.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******
            if (_configuration.GetValue<string>("ForTestingGetttingSeperateTOken") != null)
            {
                await _sessionHelperService.GetSession<Session>(request.SessionId, new Session().ObjectName, new List<string> { request.SessionId, new Session().ObjectName }).ConfigureAwait(false);
                request.Token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration);
            }

            var response = await _customerDataService.GetProfile<United.Services.Customer.Common.ProfileResponse>(request.Token, jsonRequest, _headers.ContextValues.SessionId, urlPath).ConfigureAwait(false);

            #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cslStopWatch.IsRunning)
            {
                cslStopWatch.Stop();
            }
            string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

            if (_configuration.GetValue<string>("ForTestingGetttingXMLGetProfileTime") != null && _configuration.GetValue<bool>("ForTestingGetttingXMLGetProfileTime"))
            {
                _logger.LogInformation("GetProfile - CSS / CSL - CallDuration", request.Application.Id, request.Application.Version.Major, request.DeviceId + "_" + request.MileagePlusNumber, "CSLGetProfile = " + cslCallTime, request.SessionId);
            }

            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            if (response != null)
            {
                if (response != null && response.Status.Equals(United.Services.Customer.Common.Constants.StatusType.Success) && response.Profiles != null)
                {
                    profiles = await PopulateEmpProfiles(request.SessionId, request.MileagePlusNumber, request.CustomerId, response.Profiles, request, getEmployeeIdFromCSLCustomerData);
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            if (!string.IsNullOrEmpty(error.UserFriendlyMessage))
                            {
                                errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(error.Message) && error.Message.ToUpper().Trim().Contains("INVALID"))
                                {
                                    errorMessage = errorMessage + " " + "Invalid MileagePlusId " + request.MileagePlusNumber;
                                }
                                else
                                {
                                    errorMessage = errorMessage + " " + (error.MinorDescription != null ? error.MinorDescription : string.Empty);
                                }
                            }
                        }

                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        throw new MOBUnitedException("Unable to get profile.");
                    }
                }
            }
            else
            {
                throw new MOBUnitedException("Unable to get profile.");
            }

            return profiles;
        }

        private async Task<List<MOBCPProfile>> PopulateEmpProfiles(string sessionId, string mileagePlusNumber, int customerId, List<United.Services.Customer.Common.Profile> profiles, MOBCPProfileRequest request, bool getEmployeeIdFromCSLCustomerData = false)
        {
            List<MOBCPProfile> mobProfiles = null;
            if (profiles != null && profiles.Count > 0)
            {
                mobProfiles = new List<MOBCPProfile>();
                if (!string.IsNullOrEmpty(sessionId))
                {
                    CSLProfile persistedCSLProfile = new CSLProfile();
                    persistedCSLProfile = await _sessionHelperService.GetSession<CSLProfile>(sessionId, persistedCSLProfile.ObjectName, new List<String> { sessionId, persistedCSLProfile.ObjectName }).ConfigureAwait(false);
                    if (persistedCSLProfile == null)
                    {
                        persistedCSLProfile = new CSLProfile
                        {
                            SessionId = sessionId,
                            MileagePlusNumber = mileagePlusNumber,
                            CustomerId = customerId
                        };
                    }
                    if (persistedCSLProfile.Profiles == null)
                    {

                        persistedCSLProfile.Profiles = mobProfiles;
                    }
                    else
                    {
                        mobProfiles = persistedCSLProfile.Profiles;
                    }
                }
                foreach (var profile in profiles)
                {
                    if (profile.Travelers != null && profile.Travelers.Count > 0)
                    {
                        MOBCPProfile mobProfile = new MOBCPProfile
                        {
                            AirportCode = profile.AirportCode,
                            AirportNameLong = profile.AirportNameLong,
                            AirportNameShort = profile.AirportNameShort,
                            Description = profile.Description,
                            Key = profile.Key,
                            LanguageCode = profile.LanguageCode,
                            ProfileId = profile.ProfileId,
                            ProfileMembers = PopulateProfileMembers(profile.ProfileMembers),
                            ProfileOwnerId = profile.ProfileOwnerId,
                            ProfileOwnerKey = profile.ProfileOwnerKey,

                            //Kirti - code breaking due to DLL update so commented 
                            //mobProfile.QuickCreditCardKey = profile.QuickCreditCardKey;
                            //mobProfile.QuickCreditCardNumber = profile.QuickCreditCardNum;

                            QuickCustomerId = profile.QuickCustomerId,
                            QuickCustomerKey = profile.QuickCustomerKey
                        };
                        bool isProfileOwnerTSAFlagOn = false;
                        List<MOBKVP> mpList = new List<MOBKVP>();
                        if (_configuration.GetValue<bool>("GetEmp20PassRidersFromEResService"))
                        {
                            var tupleResponse = await PopulateEmpTravelers(profile.Travelers, mileagePlusNumber, isProfileOwnerTSAFlagOn, false, request, sessionId, getEmployeeIdFromCSLCustomerData);
                            mobProfile.Travelers = tupleResponse.Item1;
                            isProfileOwnerTSAFlagOn = tupleResponse.isProfileOwnerTSAFlagOn;
                            mpList = tupleResponse.savedTravelersMPList;
                        }
                        mobProfile.SavedTravelersMPList = mpList;
                        mobProfile.IsProfileOwnerTSAFlagON = isProfileOwnerTSAFlagOn;
                        if (mobProfile != null)
                        {
                            mobProfile.DisclaimerList = await _mPTraveler.GetProfileDisclaimerList();
                        }
                        mobProfiles.Add(mobProfile);
                    }
                }
            }

            return mobProfiles;
        }

        private async Task<List<MOBCreditCard>> PopulateCreditCards(List<Services.Customer.Common.CreditCard> creditCards, bool isGetCreditCardDetailsCall, List<Mobile.Model.Common.MOBAddress> addresses)
        {
            #region

            List<MOBCreditCard> mobCreditCards = new List<MOBCreditCard>();
            if (creditCards != null && creditCards.Count > 0)
            {
                #region
                foreach (Services.Customer.Common.CreditCard creditCard in creditCards)
                {
                    //if(!IsValidCreditCard(creditCard))
                    //{
                    //    continue;
                    //}
                    if (creditCard.IsCorporate)
                    {
                        continue;
                    }

                    MOBCreditCard cc = new MOBCreditCard
                    {
                        Message = IsValidCreditCardMessage(creditCard),
                        AddressKey = creditCard.AddressKey,
                        Key = creditCard.Key
                    };
                    if (_configuration.GetValue<bool>("WorkAround4DataVaultUntilClientChange"))
                    {
                        cc.Key = creditCard.Key + "~" + creditCard.AccountNumberToken;
                    }
                    cc.CardType = creditCard.Code;
                    //switch (creditCard.CCTypeDescription.ToLower())
                    //{
                    //    case "diners club":
                    //        cc.CardTypeDescription = "Diners Club Card";
                    //        break;
                    //    case "uatp (formerly air travel card)":
                    //        cc.CardTypeDescription = "UATP";
                    //        break;
                    //    default:
                    //        cc.CardTypeDescription = creditCard.CCTypeDescription;
                    //        break;
                    //}
                    cc.CardTypeDescription = creditCard.CCTypeDescription;
                    cc.Description = creditCard.CustomDescription;
                    cc.ExpireMonth = creditCard.ExpMonth.ToString();
                    cc.ExpireYear = creditCard.ExpYear.ToString();
                    cc.IsPrimary = creditCard.IsPrimary;
                    //if (creditCard.AccountNumber.Length == 15)
                    //    cc.UnencryptedCardNumber = "XXXXXXXXXXX" + creditCard.AccountNumber.Substring(creditCard.AccountNumber.Length - 4, 4);
                    //else
                    //cc.UnencryptedCardNumber = "XXXXXXXXXXXX" + creditCard.AccountNumber.Substring(creditCard.AccountNumber.Length - 4, 4);
                    //updated due to CSL no longer providing the account number.
                    //Wade 11/03/2014
                    cc.UnencryptedCardNumber = "XXXXXXXXXXXX" + creditCard.AccountNumberLastFourDigits;
                    //**NOTE**: IF "XXXXXXXXXXXX" is updated to any other format need to fix this same at GetUpdateCreditCardRequest() as we trying to check if CC updated by checking "XXXXXXXXXXXX" exists in the UnencryptedCardNumber if exists CC Number not updated.

                    cc.DisplayCardNumber = cc.UnencryptedCardNumber;
                    cc.EncryptedCardNumber = creditCard.AccountNumberEncrypted;
                    cc.cIDCVV2 = creditCard.SecurityCode;
                    //cc.CCName = creditCard.Name;
                    if (creditCard.Payor != null)
                    {
                        cc.cCName = creditCard.Payor.GivenName;
                    }
                    if (isGetCreditCardDetailsCall)
                    {
                        cc.UnencryptedCardNumber = creditCard.AccountNumber;
                    }
                    cc.AccountNumberToken = creditCard.AccountNumberToken;
                    MOBVormetricKeys vormetricKeys = await AssignPersistentTokenToCC(creditCard.AccountNumberToken, creditCard.PersistentToken, creditCard.SecurityCodeToken, creditCard.Code, "", "PopulateCreditCards", 0, "");
                    if (!string.IsNullOrEmpty(vormetricKeys.PersistentToken))
                    {
                        cc.PersistentToken = vormetricKeys.PersistentToken;
                        //cc.SecurityCodeToken = vormetricKeys.SecurityCodeToken;
                    }

                    if (!string.IsNullOrEmpty(vormetricKeys.CardType) && string.IsNullOrEmpty(cc.CardType))
                    {
                        cc.CardType = vormetricKeys.CardType;
                    }

                    if (_configuration.GetValue<bool>("CFOPViewRes_ExcludeCorporateCard"))
                    {
                        cc.IsCorporate = creditCard.IsCorporate;
                    }

                    if (_configuration.GetValue<bool>("ShowTripInsuranceBookingSwitch"))
                    {
                        cc.IsValidForTPIPurchase = IsValidFOPForTPIpayment(creditCard.Code);
                    }
                    if (addresses != null)
                    {
                        foreach (var address in addresses)
                        {
                            if (address.Key.ToUpper().Trim() == cc.AddressKey.ToUpper().Trim())
                            {
                                mobCreditCards.Add(cc);
                            }
                        }
                    }
                }
                #endregion
            }
            #endregion

            return mobCreditCards;
        }

        private string IsValidCreditCardMessage(Services.Customer.Common.CreditCard creditCard)
        {
            string message = string.Empty;
            if (string.IsNullOrEmpty(creditCard.AddressKey))
            {
                message = _configuration.GetValue<string>("NoAddressAssociatedWithTheSavedCreditCardMessage");
            }
            if (creditCard.ExpYear < DateTime.Today.Year)
            {
                message = message + _configuration.GetValue<string>("CreditCardDateExpiredMessage");
            }
            else if (creditCard.ExpYear == DateTime.Today.Year)
            {
                if (creditCard.ExpMonth < DateTime.Today.Month)
                {
                    message = message + _configuration.GetValue<string>("CreditCardDateExpiredMessage");
                }
            }
            return message;
        }

        private bool IsValidFOPForTPIpayment(string cardType)
        {
            return !string.IsNullOrEmpty(cardType) &&
                (cardType.ToUpper().Trim() == "VI" || cardType.ToUpper().Trim() == "MC" || cardType.ToUpper().Trim() == "AX" || cardType.ToUpper().Trim() == "DS");
        }


        private bool IsValidSateForTPIpayment(string stateCode)
        {
            return !string.IsNullOrEmpty(stateCode) && !string.IsNullOrEmpty(_configuration.GetValue<string>("ExcludeUSStateCodesForTripInsurance")) && !_configuration.GetValue<string>("ExcludeUSStateCodesForTripInsurance").Contains(stateCode.ToUpper().Trim());
        }

        private bool IsValidAddressForTPIpayment(string countryCode)
        {
            return !string.IsNullOrEmpty(countryCode) && countryCode.ToUpper().Trim() == "US";
        }

        private List<MOBPrefAirPreference> PopulateAirPrefrences(List<United.Services.Customer.Common.AirPreference> airPreferences)
        {
            List<MOBPrefAirPreference> mobAirPrefs = new List<MOBPrefAirPreference>();
            if (airPreferences != null && airPreferences.Count > 0)
            {
                foreach (United.Services.Customer.Common.AirPreference pref in airPreferences)
                {
                    MOBPrefAirPreference mobAirPref = new MOBPrefAirPreference
                    {
                        AirportCode = pref.AirportCode
                    };
                    mobAirPref.AirportCode = pref.AirportNameLong;
                    mobAirPref.AirportNameShort = pref.AirportNameShort;
                    mobAirPref.AirPreferenceId = pref.AirPreferenceId;
                    mobAirPref.ClassDescription = pref.ClassDescription;
                    mobAirPref.ClassId = pref.ClassId;
                    mobAirPref.CustomerId = pref.CustomerId;
                    mobAirPref.EquipmentCode = pref.EquipmentCode;
                    mobAirPref.EquipmentDesc = pref.EquipmentDesc;
                    mobAirPref.EquipmentId = pref.EquipmentId;
                    mobAirPref.IsActive = pref.IsActive;
                    mobAirPref.IsSelected = pref.IsSelected;
                    mobAirPref.IsNew = pref.IsNew;
                    mobAirPref.Key = pref.Key;
                    mobAirPref.LanguageCode = pref.LanguageCode;
                    mobAirPref.MealCode = pref.MealCode;
                    mobAirPref.MealDescription = pref.MealDescription;
                    mobAirPref.MealId = pref.MealId;
                    mobAirPref.NumOfFlightsDisplay = pref.NumOfFlightsDisplay;
                    mobAirPref.ProfileId = pref.ProfileId;
                    mobAirPref.SearchPreferenceDescription = pref.SearchPreferenceDescription;
                    mobAirPref.SearchPreferenceId = pref.SearchPreferenceId;
                    mobAirPref.SeatFrontBack = pref.SeatFrontBack;
                    mobAirPref.SeatSide = pref.SeatSide;
                    mobAirPref.SeatSideDescription = pref.SeatSideDescription;
                    mobAirPref.VendorCode = pref.VendorCode;
                    mobAirPref.VendorDescription = pref.VendorDescription;
                    mobAirPref.VendorId = pref.VendorId;
                    mobAirPref.AirRewardPrograms = GetAirRewardPrograms(pref.AirRewardPrograms);
                    mobAirPref.SpecialRequests = GetTravelerSpecialRequests(pref.SpecialRequests);
                    mobAirPref.ServiceAnimals = GetTravelerServiceAnimals(pref.ServiceAnimals);

                    mobAirPrefs.Add(mobAirPref);
                }
            }
            return mobAirPrefs;
        }

        private List<MOBPrefRewardProgram> GetAirRewardPrograms(List<United.Services.Customer.Common.RewardProgram> programs)
        {
            List<MOBPrefRewardProgram> mobAirRewardsProgs = new List<MOBPrefRewardProgram>();
            if (programs != null && programs.Count > 0)
            {
                foreach (United.Services.Customer.Common.RewardProgram pref in programs)
                {
                    MOBPrefRewardProgram mobAirRewardsProg = new MOBPrefRewardProgram
                    {
                        CustomerId = Convert.ToInt32(pref.CustomerId),
                        ProfileId = Convert.ToInt32(pref.ProfileId),
                        //mobAirRewardsProg.ProgramCode = pref.ProgramCode;
                        //mobAirRewardsProg.ProgramDescription = pref.ProgramDescription;
                        ProgramMemberId = pref.ProgramMemberId,
                        VendorCode = pref.VendorCode,
                        VendorDescription = pref.VendorDescription
                    };
                    mobAirRewardsProgs.Add(mobAirRewardsProg);
                }
            }
            return mobAirRewardsProgs;
        }

        private List<MOBPrefSpecialRequest> GetTravelerSpecialRequests(List<United.Services.Customer.Common.SpecialRequest> specialRequests)
        {
            List<MOBPrefSpecialRequest> mobSpecialRequests = new List<MOBPrefSpecialRequest>();
            if (specialRequests != null && specialRequests.Count > 0)
            {
                foreach (United.Services.Customer.Common.SpecialRequest req in specialRequests)
                {
                    MOBPrefSpecialRequest mobSpecialRequest = new MOBPrefSpecialRequest
                    {
                        AirPreferenceId = req.AirPreferenceId,
                        SpecialRequestId = req.SpecialRequestId,
                        SpecialRequestCode = req.SpecialRequestCode,
                        Key = req.Key,
                        LanguageCode = req.LanguageCode,
                        Description = req.Description,
                        Priority = req.Priority,
                        IsNew = req.IsNew,
                        IsSelected = req.IsSelected
                    };
                    mobSpecialRequests.Add(mobSpecialRequest);
                }
            }
            return mobSpecialRequests;
        }
        private List<MOBPrefServiceAnimal> GetTravelerServiceAnimals(List<ServiceAnimal> serviceAnimals)
        {
            var results = new List<MOBPrefServiceAnimal>();

            if (serviceAnimals == null || !serviceAnimals.Any())
            {
                return results;
            }

            results = serviceAnimals.Select(x => new MOBPrefServiceAnimal
            {
                AirPreferenceId = x.AirPreferenceId,
                ServiceAnimalId = x.ServiceAnimalId,
                ServiceAnimalIdDesc = x.ServiceAnimalDesc,
                ServiceAnimalTypeId = x.ServiceAnimalTypeId,
                ServiceAnimalTypeIdDesc = x.ServiceAnimalTypeDesc,
                Key = x.Key,
                Priority = x.Priority,
                IsNew = x.IsNew,
                IsSelected = x.IsSelected
            }).ToList();

            return results;
        }

        private List<MOBEmail> PopulateAllEmailAddresses(List<Services.Customer.Common.Email> emailAddresses)
        {
            #region
            List<MOBEmail> mobEmailAddresses = new List<MOBEmail>();
            if (emailAddresses != null && emailAddresses.Count > 0)
            {
                int co = 0;
                foreach (Services.Customer.Common.Email email in emailAddresses)
                {
                    if (email.EffectiveDate <= DateTime.UtcNow && email.DiscontinuedDate >= DateTime.UtcNow)
                    {
                        #region As per Wade Change want to filter out to return only Primary email to client if not primary phone exist return the first one from the list.So lot of work at client side will save time. Sep 15th 2014
                        co = co + 1;
                        MOBEmail e = new MOBEmail
                        {
                            Key = email.Key,
                            Channel = new SHOPChannel
                            {
                                ChannelCode = email.ChannelCode,
                                ChannelDescription = email.ChannelCodeDescription,
                                ChannelTypeCode = email.ChannelTypeCode.ToString(),
                                ChannelTypeDescription = email.ChannelTypeDescription
                            },
                            EmailAddress = email.EmailAddress,
                            IsDefault = email.IsDefault,
                            IsPrimary = email.IsPrimary,
                            IsPrivate = email.IsPrivate,
                            IsDayOfTravel = email.IsDayOfTravel
                        };
                        mobEmailAddresses.Add(e);
                        #endregion
                    }
                }
            }
            return mobEmailAddresses;
            #endregion
        }
        private List<MOBEmail> PopulateEmailAddresses(List<Services.Customer.Common.Email> emailAddresses, bool onlyDayOfTravelContact)
        {
            #region
            List<MOBEmail> mobEmailAddresses = new List<MOBEmail>();
            bool isCorpEmailPresent = false;

            if (_configuration.GetValue<bool>("CorporateConcurBooking") && IsCorpBookingPath)
            {
                //As per Business / DotCom Kalpen; we are removing the condition for checking the Effectivedate and Discontinued date
                var corpIndex = emailAddresses.FindIndex(x => x.ChannelTypeDescription != null && x.ChannelTypeDescription.ToLower() == "corporate" && x.EmailAddress != null && x.EmailAddress.Trim() != "");
                if (corpIndex >= 0)
                {
                    isCorpEmailPresent = true;
                }
            }

            if (emailAddresses != null && emailAddresses.Count > 0)
            {
                MOBEmail primaryEmailAddress = null;
                int co = 0;
                foreach (Services.Customer.Common.Email email in emailAddresses)
                {
                    if (_configuration.GetValue<bool>("CorporateConcurBooking"))
                    {
                        if (isCorpEmailPresent && !onlyDayOfTravelContact && email.ChannelTypeDescription.ToLower() == "corporate")
                        {
                            primaryEmailAddress = new MOBEmail();
                            email.IsPrimary = true;
                            primaryEmailAddress.Key = email.Key;
                            primaryEmailAddress.Channel = new SHOPChannel();
                            primaryEmailAddress.EmailAddress = email.EmailAddress;
                            primaryEmailAddress.Channel.ChannelCode = email.ChannelCode;
                            primaryEmailAddress.Channel.ChannelDescription = email.ChannelCodeDescription;
                            primaryEmailAddress.Channel.ChannelTypeCode = email.ChannelTypeCode.ToString();
                            primaryEmailAddress.Channel.ChannelTypeDescription = email.ChannelTypeDescription;
                            primaryEmailAddress.IsDefault = email.IsDefault;
                            primaryEmailAddress.IsPrimary = email.IsPrimary;
                            primaryEmailAddress.IsPrivate = email.IsPrivate;
                            primaryEmailAddress.IsDayOfTravel = email.IsDayOfTravel;
                            if (!email.IsDayOfTravel)
                            {
                                break;
                            }

                        }
                        else if (isCorpEmailPresent && !onlyDayOfTravelContact && email.ChannelTypeDescription.ToLower() != "corporate")
                        {
                            continue;
                        }
                    }
                    //Fix for CheckOut ArgNull Exception - Empty EmailAddress with null EffectiveDate & DiscontinuedDate for Corp Account Revenue Booking (MOBILE-9873) - Shashank : Added OR condition to allow CorporateAccount ProfileOwner.
                    if ((email.EffectiveDate <= DateTime.UtcNow && email.DiscontinuedDate >= DateTime.UtcNow) ||
                            (!_configuration.GetValue<bool>("DisableCheckforCorpAccEmail") && email.ChannelTypeDescription.ToLower() == "corporate"
                            && email.IsProfileOwner == true && primaryEmailAddress.IsNullOrEmpty()))
                    {
                        #region As per Wade Change want to filter out to return only Primary email to client if not primary phone exist return the first one from the list.So lot of work at client side will save time. Sep 15th 2014
                        co = co + 1;
                        MOBEmail e = new MOBEmail
                        {
                            Key = email.Key,
                            Channel = new SHOPChannel(),
                            EmailAddress = email.EmailAddress
                        };
                        e.Channel.ChannelCode = email.ChannelCode;
                        e.Channel.ChannelDescription = email.ChannelCodeDescription;
                        e.Channel.ChannelTypeCode = email.ChannelTypeCode.ToString();
                        e.Channel.ChannelTypeDescription = email.ChannelTypeDescription;
                        e.IsDefault = email.IsDefault;
                        e.IsPrimary = email.IsPrimary;
                        e.IsPrivate = email.IsPrivate;
                        e.IsDayOfTravel = email.IsDayOfTravel;
                        if (email.IsDayOfTravel)
                        {
                            primaryEmailAddress = new MOBEmail();
                            primaryEmailAddress = e;
                            if (onlyDayOfTravelContact)
                            {
                                break;
                            }
                        }
                        if (!onlyDayOfTravelContact)
                        {
                            if (email.IsPrimary)
                            {
                                primaryEmailAddress = new MOBEmail();
                                primaryEmailAddress = e;
                                break;
                            }
                            else if (co == 1)
                            {
                                primaryEmailAddress = new MOBEmail();
                                primaryEmailAddress = e;
                            }
                        }
                        #endregion
                    }
                }
                if (primaryEmailAddress != null)
                {
                    mobEmailAddresses.Add(primaryEmailAddress);
                }
            }
            return mobEmailAddresses;
            #endregion
        }
        private List<MOBCPPhone> PopulatePhones(List<United.Services.Customer.Common.Phone> phones, bool onlyDayOfTravelContact)
        {
            List<MOBCPPhone> mobCPPhones = new List<MOBCPPhone>();
            bool isCorpPhonePresent = false;


            if (_configuration.GetValue<bool>("CorporateConcurBooking") && IsCorpBookingPath)
            {
                var corpIndex = phones.FindIndex(x => x.ChannelTypeDescription != null && x.ChannelTypeDescription.ToLower() == "corporate" && x.PhoneNumber != null && x.PhoneNumber != "");
                if (corpIndex >= 0)
                {
                    isCorpPhonePresent = true;
                }
            }


            if (phones != null && phones.Count > 0)
            {
                MOBCPPhone primaryMobCPPhone = null;
                CultureInfo ci = GeneralHelper.EnableUSCultureInfo();
                int co = 0;
                foreach (United.Services.Customer.Common.Phone phone in phones)
                {
                    #region As per Wade Change want to filter out to return only Primary Phone to client if not primary phone exist return the first one from the list.So lot of work at client side will save time. Sep 15th 2014
                    MOBCPPhone mobCPPhone = new MOBCPPhone();
                    co = co + 1;

                    mobCPPhone.AreaNumber = phone.AreaNumber;
                    mobCPPhone.PhoneNumber = phone.PhoneNumber;

                    mobCPPhone.Attention = phone.Attention;
                    mobCPPhone.ChannelCode = phone.ChannelCode;
                    mobCPPhone.ChannelCodeDescription = phone.ChannelCodeDescription;
                    mobCPPhone.ChannelTypeCode = Convert.ToString(phone.ChannelTypeCode);
                    mobCPPhone.ChannelTypeDescription = phone.ChannelTypeDescription;
                    mobCPPhone.ChannelTypeDescription = phone.ChannelTypeDescription;
                    mobCPPhone.ChannelTypeSeqNumber = phone.ChannelTypeSeqNum;
                    mobCPPhone.CountryCode = phone.CountryCode;
                    //mobCPPhone.CountryCode = GetAccessCode(phone.CountryCode);
                    mobCPPhone.CountryPhoneNumber = phone.CountryPhoneNumber;
                    mobCPPhone.CustomerId = phone.CustomerId;
                    mobCPPhone.Description = phone.Description;
                    mobCPPhone.DiscontinuedDate = Convert.ToString(phone.DiscontinuedDate);
                    mobCPPhone.EffectiveDate = Convert.ToString(phone.EffectiveDate);
                    mobCPPhone.ExtensionNumber = phone.ExtensionNumber;
                    mobCPPhone.IsPrimary = phone.IsPrimary;
                    mobCPPhone.IsPrivate = phone.IsPrivate;
                    mobCPPhone.IsProfileOwner = phone.IsProfileOwner;
                    mobCPPhone.Key = phone.Key;
                    mobCPPhone.LanguageCode = phone.LanguageCode;
                    mobCPPhone.PagerPinNumber = phone.PagerPinNumber;
                    mobCPPhone.SharesCountryCode = phone.SharesCountryCode;
                    mobCPPhone.WrongPhoneDate = Convert.ToString(phone.WrongPhoneDate);
                    if (phone.PhoneDevices != null && phone.PhoneDevices.Count > 0)
                    {
                        mobCPPhone.DeviceTypeCode = phone.PhoneDevices[0].CommDeviceTypeCode;
                        mobCPPhone.DeviceTypeDescription = phone.PhoneDevices[0].CommDeviceTypeDescription;
                    }
                    mobCPPhone.IsDayOfTravel = phone.IsDayOfTravel;

                    if (_configuration.GetValue<bool>("CorporateConcurBooking"))
                    {
                        #region
                        if (IsCorpBookingPath && isCorpPhonePresent && !onlyDayOfTravelContact && phone.ChannelTypeDescription.ToLower() == "corporate")
                        {
                            //return the corporate phone number
                            primaryMobCPPhone = new MOBCPPhone();
                            mobCPPhone.IsPrimary = true;
                            primaryMobCPPhone = mobCPPhone;
                            break;

                        }
                        if (IsCorpBookingPath && isCorpPhonePresent && !onlyDayOfTravelContact && phone.ChannelTypeDescription.ToLower() != "corporate")
                        {
                            //There is corporate phone number present, continue till corporate phone number is found
                            continue;
                        }
                        #endregion
                    }

                    if (phone.IsDayOfTravel)
                    {
                        primaryMobCPPhone = new MOBCPPhone();
                        primaryMobCPPhone = mobCPPhone;// Only day of travel contact should be returned to use at Edit Traveler
                        if (onlyDayOfTravelContact)
                        {
                            break;
                        }
                    }
                    if (!onlyDayOfTravelContact)
                    {
                        if (phone.IsPrimary)
                        {
                            primaryMobCPPhone = new MOBCPPhone();
                            primaryMobCPPhone = mobCPPhone;
                            break;
                        }
                        else if (co == 1)
                        {
                            primaryMobCPPhone = new MOBCPPhone();
                            primaryMobCPPhone = mobCPPhone;
                        }
                    }
                    #endregion
                }
                if (primaryMobCPPhone != null)
                {
                    mobCPPhones.Add(primaryMobCPPhone);
                }
                GeneralHelper.DisableUSCultureInfo(ci);
            }
            return mobCPPhones;
        }
        private List<MOBBKLoyaltyProgramProfile> GetTravelerLoyaltyProfile(List<United.Services.Customer.Common.AirPreference> airPreferences, int currentEliteLevel)
        {
            List<MOBBKLoyaltyProgramProfile> programs = new List<MOBBKLoyaltyProgramProfile>();
            //if(airPreferences != null && airPreferences.Count > 0 && airPreferences[0].AirRewardPrograms != null && airPreferences[0].AirRewardPrograms.Count > 0) 
            if (airPreferences != null && airPreferences.Count > 0)
            {
                #region
                List<United.Services.Customer.Common.AirPreference> airPreferencesList = new List<Services.Customer.Common.AirPreference>();
                airPreferencesList = (from item in airPreferences
                                      where item.AirRewardPrograms != null && item.AirRewardPrograms.Count > 0
                                      select item).ToList();
                //foreach(United.Services.Customer.Common.RewardProgram rewardProgram in airPreferences[0].AirRewardPrograms) 
                if (airPreferencesList != null && airPreferencesList.Count > 0)
                {
                    foreach (United.Services.Customer.Common.RewardProgram rewardProgram in airPreferencesList[0].AirRewardPrograms)
                    {
                        MOBBKLoyaltyProgramProfile airRewardProgram = new MOBBKLoyaltyProgramProfile
                        {
                            ProgramId = rewardProgram.ProgramID.ToString(),
                            ProgramName = rewardProgram.Description,
                            MemberId = rewardProgram.ProgramMemberId,
                            CarrierCode = rewardProgram.VendorCode
                        };
                        if (airRewardProgram.CarrierCode.Trim().Equals("UA"))
                        {
                            airRewardProgram.MPEliteLevel = currentEliteLevel;
                        }
                        airRewardProgram.RewardProgramKey = rewardProgram.Key;
                        programs.Add(airRewardProgram);
                    }
                }
                #endregion
            }
            return programs;
        }

        private async Task<MOBCPMileagePlus> GetCurrentEliteLevelFromAirRewardProgram(List<AirPreference> airPreferences, string sessionid)
        {
            MOBCPMileagePlus mobCPMileagePlus = null;
            var airRewardProgram = airPreferences[0].AirRewardPrograms[0];
            if (!string.IsNullOrEmpty(airRewardProgram.ProgramMemberId))
            {
                Session session = new Session();
                session = await _sessionHelperService.GetSession<Session>(sessionid, session.ObjectName, new List<string> { sessionid, session.ObjectName }).ConfigureAwait(false);

                MOBCPProfileRequest request = new MOBCPProfileRequest
                {
                    CustomerId = 0,
                    MileagePlusNumber = airRewardProgram.ProgramMemberId
                };
                United.Services.Customer.Common.ProfileRequest profileRequest = (new ProfileRequest(_configuration, IsCorpBookingPath)).GetProfileRequest(request);
                string jsonRequest = JsonSerializer.Serialize<United.Services.Customer.Common.ProfileRequest>(profileRequest);
                string url = string.Format("/GetProfile");

                //Utility utility = new Utility();
                var jsonresponse = await MakeHTTPPostAndLogIt(session.SessionId, session.DeviceID, "GetProfileForTravelerToGetEliteLevel", session.AppID, string.Empty, session.Token, url, jsonRequest);
                mobCPMileagePlus = GetOwnerEliteLevelFromCslResponse(jsonresponse);
            }
            return mobCPMileagePlus;
        }

        private MOBCPMileagePlus GetOwnerEliteLevelFromCslResponse(string jsonresponse)
        {
            MOBCPMileagePlus mobCPMileagePlus = null;
            if (!string.IsNullOrEmpty(jsonresponse))
            {
                United.Services.Customer.Common.ProfileResponse response = JsonSerializer.Deserialize<United.Services.Customer.Common.ProfileResponse>(jsonresponse);
                if (response != null && response.Status.Equals(United.Services.Customer.Common.Constants.StatusType.Success) &&
                    response.Profiles != null &&
                    response.Profiles.Count > 0 &&
                    response.Profiles[0].Travelers != null &&
                    response.Profiles[0].Travelers.Exists(p => p.IsProfileOwner))
                {
                    var owner = response.Profiles[0].Travelers.First(p => p.IsProfileOwner);
                    if (owner != null & owner.MileagePlus != null)
                    {
                        mobCPMileagePlus = PopulateMileagePlus(owner.MileagePlus);
                    }
                }
            }

            return mobCPMileagePlus;
        }

        private async Task<string> MakeHTTPPostAndLogIt(string sessionId, string deviceId, string action, int applicationId, string appVersion, string token, string url, string jsonRequest, bool isXMLRequest = false)
        {
            ////logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, action, "URL", applicationId, appVersion, deviceId, url, true, true));
            ////logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, action, "Request", applicationId, appVersion, deviceId, jsonRequest, true, true));
            string jsonResponse = string.Empty;

            string paypalCSLCallDurations = string.Empty;
            string callTime4Tuning = string.Empty;

            #region//****Get Call Duration Code*******
            Stopwatch cslCallDurationstopwatch1;
            cslCallDurationstopwatch1 = new Stopwatch();
            cslCallDurationstopwatch1.Reset();
            cslCallDurationstopwatch1.Start();
            #endregion

            string applicationRequestType = isXMLRequest ? "xml" : "json";
            //jsonResponse = HttpHelper.Post(url, "application/" + applicationRequestType + "; charset=utf-8", token, jsonRequest, httpPostTimeOut, httpPostNumberOfRetry);
            var response = await _customerDataService.GetCustomerData<United.Services.Customer.Common.ProfileRequest>(token, sessionId, jsonRequest).ConfigureAwait(false);
            #region
            if (cslCallDurationstopwatch1.IsRunning)
            {
                cslCallDurationstopwatch1.Stop();
            }
            paypalCSLCallDurations = paypalCSLCallDurations + "|2=" + cslCallDurationstopwatch1.ElapsedMilliseconds.ToString() + "|"; // 2 = shopCSLCallDurationstopwatch1
            callTime4Tuning = "|CSL =" + (cslCallDurationstopwatch1.ElapsedMilliseconds / (double)1000).ToString();
            #endregion
            //check
            return response.ToString();
        }



        private MOBCPMileagePlus PopulateMileagePlus(United.Services.Customer.Common.MileagePlus onePass)
        {
            MOBCPMileagePlus mileagePlus = null;
            if (onePass != null)
            {
                mileagePlus = new MOBCPMileagePlus
                {
                    AccountBalance = onePass.AccountBalance,
                    ActiveStatusCode = onePass.ActiveStatusCode,
                    ActiveStatusDescription = onePass.ActiveStatusDescription,
                    AllianceEliteLevel = onePass.AllianceEliteLevel,
                    ClosedStatusCode = onePass.ClosedStatusCode,
                    ClosedStatusDescription = onePass.ClosedStatusDescription,
                    CurrentEliteLevel = onePass.CurrentEliteLevel
                };
                if (onePass.CurrentEliteLevelDescription != null)
                {
                    mileagePlus.CurrentEliteLevelDescription = onePass.CurrentEliteLevelDescription.ToString().ToUpper() == "NON-ELITE" ? "General member" : onePass.CurrentEliteLevelDescription;
                }
                mileagePlus.CurrentYearMoneySpent = onePass.CurrentYearMoneySpent;
                mileagePlus.EliteMileageBalance = onePass.EliteMileageBalance;
                mileagePlus.EliteSegmentBalance = Convert.ToInt32(onePass.EliteSegmentBalance);
                //mileagePlus.EliteSegmentDecimalPlaceValue = onePass.elite;
                mileagePlus.EncryptedPin = onePass.EncryptedPin;
                mileagePlus.EnrollDate = onePass.EnrollDate.GetValueOrDefault().ToString("MM/dd/yyyy");
                mileagePlus.EnrollSourceCode = onePass.EnrollSourceCode;
                mileagePlus.EnrollSourceDescription = onePass.EnrollSourceDescription;
                mileagePlus.FlexPqmBalance = onePass.FlexPQMBalance;
                mileagePlus.FutureEliteDescription = onePass.FutureEliteLevelDescription;
                mileagePlus.FutureEliteLevel = onePass.FutureEliteLevel;
                mileagePlus.InstantEliteExpirationDate = onePass.FutureEliteLevelDescription;
                mileagePlus.IsCEO = onePass.IsCEO;
                mileagePlus.IsClosedPermanently = onePass.IsClosedPermanently;
                mileagePlus.IsClosedTemporarily = onePass.IsClosedTemporarily;
                mileagePlus.IsCurrentTrialEliteMember = onePass.IsCurrentTrialEliteMember;
                mileagePlus.IsFlexPqm = onePass.IsFlexPQM;
                mileagePlus.IsInfiniteElite = onePass.IsInfiniteElite;
                mileagePlus.IsLifetimeCompanion = onePass.IsLifetimeCompanion;
                mileagePlus.IsLockedOut = onePass.IsLockedOut;
                mileagePlus.IsPresidentialPlus = onePass.IsPresidentialPlus;
                mileagePlus.IsUnitedClubMember = onePass.IsPClubMember;
                mileagePlus.Key = onePass.Key;
                mileagePlus.LastActivityDate = onePass.LastActivityDate;
                mileagePlus.LastExpiredMile = onePass.LastExpiredMile;
                mileagePlus.LastFlightDate = onePass.LastFlightDate;
                mileagePlus.LastStatementBalance = onePass.LastStatementBalance;
                mileagePlus.LastStatementDate = onePass.LastStatementDate.GetValueOrDefault().ToString("MM/dd/yyyy");
                mileagePlus.LifetimeEliteMileageBalance = onePass.LifetimeEliteMileageBalance;
                mileagePlus.MileageExpirationDate = onePass.MileageExpirationDate;
                mileagePlus.MileagePlusId = onePass.MileagePlusId;
                mileagePlus.MileagePlusPin = onePass.MileagePlusPIN;
                mileagePlus.NextYearEliteLevel = onePass.NextYearEliteLevel;
                mileagePlus.NextYearEliteLevelDescription = onePass.NextYearEliteLevelDescription;
                mileagePlus.PriorUnitedAccountNumber = onePass.PriorUnitedAccountNumber;
                mileagePlus.StarAllianceEliteLevel = onePass.SkyTeamEliteLevelCode;
                if (!_configuration.GetValue<bool>("Keep_MREST_MP_EliteLevel_Expiration_Logic"))
                {
                    mileagePlus.PremierLevelExpirationDate = onePass.PremierLevelExpirationDate;
                    if (onePass.CurrentYearInstantElite != null)
                    {
                        mileagePlus.InstantElite = new MOBInstantElite()
                        {
                            ConsolidatedCode = onePass.CurrentYearInstantElite.ConsolidatedCode,
                            EffectiveDate = onePass.CurrentYearInstantElite.EffectiveDate != null ? onePass.CurrentYearInstantElite.EffectiveDate.ToString("MM/dd/yyyy") : string.Empty,
                            EliteLevel = onePass.CurrentYearInstantElite.EliteLevel,
                            EliteYear = onePass.CurrentYearInstantElite.EliteYear,
                            ExpirationDate = onePass.CurrentYearInstantElite.ExpirationDate != null ? onePass.CurrentYearInstantElite.ExpirationDate.ToString("MM/dd/yyyy") : string.Empty,
                            PromotionCode = onePass.CurrentYearInstantElite.PromotionCode
                        };
                    }
                }
            }

            return mileagePlus;
        }
        private async Task<Reservation> PersistedReservation(MOBCPProfileRequest request)
        {
            Reservation persistedReservation =
                new Reservation();
            if (request != null)
            {
                persistedReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, persistedReservation.ObjectName, new List<string> { request.SessionId, persistedReservation.ObjectName }).ConfigureAwait(false);

                if (_configuration.GetValue<bool>("CorporateConcurBooking"))
                {
                    if (persistedReservation != null && persistedReservation.ShopReservationInfo != null &&
                        persistedReservation.ShopReservationInfo.IsCorporateBooking)
                    {
                        this.IsCorpBookingPath = true;
                    }

                    if (persistedReservation != null && persistedReservation.ShopReservationInfo2 != null &&
                        persistedReservation.ShopReservationInfo2.IsArrangerBooking)
                    {
                        this.IsArrangerBooking = true;
                    }
                }
            }

            return persistedReservation;
        }
        private List<MOBCPProfileMember> PopulateProfileMembers(List<United.Services.Customer.Common.ProfileMember> profileMembers)
        {
            List<MOBCPProfileMember> mobProfileMembers = null;

            if (profileMembers != null && profileMembers.Count > 0)
            {
                mobProfileMembers = new List<MOBCPProfileMember>();
                foreach (var profileMember in profileMembers)
                {
                    MOBCPProfileMember mobProfileMember = new MOBCPProfileMember
                    {
                        CustomerId = profileMember.CustomerId,
                        Key = profileMember.Key,
                        LanguageCode = profileMember.LanguageCode,
                        ProfileId = profileMember.ProfileId
                    };

                    mobProfileMembers.Add(mobProfileMember);
                }
            }

            return mobProfileMembers;
        }

        #region updated member profile start

        private async Task GetXMLGetProfileTimeForTesting(MOBCPProfileRequest request)
        {
            #region
            Stopwatch cslStopWatch;
            cslStopWatch = new Stopwatch();
            cslStopWatch.Reset();
            cslStopWatch.Start();
            //response = soapClient.GetProfileWithPartnerCardOption(request.MileagePlusNumber, true, "iPhone", ConfigurationManager.AppSettings["AccessCode - AccountProfile"], string.Empty);
            var response = await _profileService.GetProfile(request.MileagePlusNumber).ConfigureAwait(false);
            if (cslStopWatch.IsRunning)
            {
                cslStopWatch.Stop();
            }
            string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

            //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, soapClient.Endpoint.Address.ToString(), "CSS/CSL-CallDuration", request.Application.Id, request.Application.Version.Major, request.DeviceId + "_" + request.MileagePlusNumber, "XMLGetProfile=" + cslCallTime));
            _logger.LogInformation("GetXMLGetProfileTimeForTesting: CSS/CSL-CallDuration {response}", JsonConvert.SerializeObject(request), "XMLGetProfile=" + cslCallTime);
            #endregion
        }
        private bool IsEnabledNationalityAndResidence(bool isReShop, int appid, string appversion)
        {
            if (!isReShop && EnableNationalityResidence(appid, appversion))
            {
                return true;
            }
            return false;
        }
        private bool EnableNationalityResidence(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableNationalityAndCountryOfResidence")
           && GeneralHelper.isApplicationVersionGreater(appId, appVersion, "AndroidiPhonePriceChangeVersion", "AndroidiPhonePriceChangeVersion", "", "", true, _configuration);
        }

        private async Task<MOBVormetricKeys> AssignPersistentTokenToCC(string accountNumberToken, string persistentToken, string securityCodeToken, string cardType, string sessionId, string action, int appId, string deviceID)
        {
            MOBVormetricKeys vormetricKeys = new MOBVormetricKeys();
            if (_configuration.GetValue<bool>("VormetricTokenMigration"))
            {
                if ((string.IsNullOrEmpty(persistentToken) || string.IsNullOrEmpty(cardType)) && !string.IsNullOrEmpty(accountNumberToken) && !string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(accountNumberToken))
                {
                    vormetricKeys = await GetPersistentTokenUsingAccountNumberToken(accountNumberToken, sessionId, accountNumberToken);
                    persistentToken = vormetricKeys.PersistentToken;
                }

                if (!string.IsNullOrEmpty(persistentToken))
                {
                    vormetricKeys.PersistentToken = persistentToken;
                    vormetricKeys.SecurityCodeToken = securityCodeToken;
                    vormetricKeys.CardType = cardType;
                }
                else
                {
                    LogNoPersistentTokenInCSLResponseForVormetricPayment(sessionId);
                }
            }
            else
            {
                persistentToken = string.Empty;
            }

            return vormetricKeys;
        }

        private void LogNoPersistentTokenInCSLResponseForVormetricPayment(string sessionId, string Message = "Unable to retieve PersistentToken")
        {
            //if (string.IsNullOrEmpty(sessionId) && _logEntries != null && _logEntries.Count() > 0)
            //{
            //    sessionId = _logEntries[0].Guid;
            //}

            //_logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, _action, "PERSISTENTTOKENNOTFOUND", _application.Id, _application.Version.Major, _deviceId, Message, false, true));
            //No need to block the flow as we are calling DataVault for Persistent Token during the final payment
            //throw new System.Exception(ConfigurationManager.AppSettings["VormetricExceptionMessage"]);
        }

        private async Task<MOBVormetricKeys> GetPersistentTokenUsingAccountNumberToken(string accountNumberToke, string sessionId, string token)
        {
            string url = string.Format("/{0}/RSA", accountNumberToke);

            var cslResponse = await MakeHTTPCallAndLogIt(sessionId, _deviceId, "CSL-ChangeEligibleCheck", _application, token, url, string.Empty, true, false);

            return GetPersistentTokenFromCSLDatavaultResponse(cslResponse, sessionId);
        }

        private MOBVormetricKeys GetPersistentTokenFromCSLDatavaultResponse(string jsonResponseFromCSL, string sessionID)
        {
            MOBVormetricKeys vormetricKeys = new MOBVormetricKeys();
            if (!string.IsNullOrEmpty(jsonResponseFromCSL))
            {
                CslDataVaultResponse response = DataContextJsonSerializer.DeserializeJsonDataContract<CslDataVaultResponse>(jsonResponseFromCSL);
                if (response != null && response.Responses != null && response.Responses[0].Error == null && response.Responses[0].Message != null && response.Responses[0].Message.Count > 0 && response.Responses[0].Message[0].Code.Trim() == "0")
                {
                    var creditCard = response.Items[0] as Service.Presentation.PaymentModel.CreditCard;
                    vormetricKeys.PersistentToken = creditCard.PersistentToken;
                    vormetricKeys.SecurityCodeToken = creditCard.SecurityCodeToken;
                    vormetricKeys.CardType = creditCard.Code;
                }
                else
                {
                    if (response.Responses[0].Error != null && response.Responses[0].Error.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Responses[0].Error)
                        {
                            errorMessage = errorMessage + " " + error.Text;
                        }
                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToInsertCreditCardToProfileErrorMessage").ToString();
                        if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && _configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL InsertTravelerCreditCard(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }

            return vormetricKeys;
        }

        private async Task<string> MakeHTTPCallAndLogIt(string sessionId, string deviceId, string action, MOBApplication application, string token, string url, string jsonRequest, bool isGetCall, bool isXMLRequest = false)
        {
            string jsonResponse = string.Empty;

            string paypalCSLCallDurations = string.Empty;
            string callTime4Tuning = string.Empty;

            #region//****Get Call Duration Code*******
            Stopwatch cslCallDurationstopwatch1;
            cslCallDurationstopwatch1 = new Stopwatch();
            cslCallDurationstopwatch1.Reset();
            cslCallDurationstopwatch1.Start();
            #endregion

            string applicationRequestType = isXMLRequest ? "xml" : "json";
            if (isGetCall)
            {
                //jsonResponse = HttpHelper.Get(url, "Application/json", token);
                jsonResponse = await _dataVaultService.GetPersistentToken(token, jsonRequest, url, sessionId).ConfigureAwait(false);

            }
            else
            {
                //jsonResponse = HttpHelper.Post(url, "application/" + applicationRequestType + "; charset=utf-8", token, jsonRequest, httpPostTimeOut, httpPostNumberOfRetry);
                jsonResponse = await _dataVaultService.PersistentToken(token, jsonRequest, url, sessionId).ConfigureAwait(false);

            }

            #region
            if (cslCallDurationstopwatch1.IsRunning)
            {
                cslCallDurationstopwatch1.Stop();
            }
            paypalCSLCallDurations = paypalCSLCallDurations + "|2=" + cslCallDurationstopwatch1.ElapsedMilliseconds.ToString() + "|"; // 2 = shopCSLCallDurationstopwatch1
            callTime4Tuning = "|CSL =" + (cslCallDurationstopwatch1.ElapsedMilliseconds / (double)1000).ToString();
            #endregion

            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, action, "Response", application.Id, application.Version.Major, deviceId, jsonResponse, false, false));
            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, action, "CSS/CSL-CallDuration", application.Id, application.Version.Major, deviceId, "CSLResponse=" + (cslCallDurationstopwatch1.ElapsedMilliseconds / (double)1000).ToString(), false, false));
            return jsonResponse;
        }


        private async Task<(string employeeId, string displayEmployeeId)> GetEmployeeId(string transactionId, string mileagePlusNumber, string displayEmployeeId)
        {
            string employeeId = string.Empty;

            if (!string.IsNullOrEmpty(transactionId) && !string.IsNullOrEmpty(mileagePlusNumber))
            {
                var tupleRes = await _mileagePlus.GetEmployeeIdy(transactionId, mileagePlusNumber, _headers.ContextValues.SessionId, displayEmployeeId);
                string eId = tupleRes.employeeId;
                displayEmployeeId = tupleRes.displayEmployeeId;
                if (eId != null)
                {
                    employeeId = eId;
                }
            }

            return (employeeId, displayEmployeeId);
        }

        private async Task<(List<MOBCPTraveler>, bool isProfileOwnerTSAFlagOn, List<MOBKVP> savedTravelersMPList)> PopulateEmpTravelers(List<United.Services.Customer.Common.Traveler> travelers, string mileagePluNumber, bool isProfileOwnerTSAFlagOn, bool isGetCreditCardDetailsCall, MOBCPProfileRequest request, string sessionId, bool getEmployeeIdFromCSLCustomerData = false)
        {
            string displayEmployeeId = string.Empty;
            string employeeId = string.Empty;

            if (getEmployeeIdFromCSLCustomerData && travelers != null && travelers.Count > 0)
            {
                employeeId = travelers[0].EmployeeId;
            }
            else
            {
                var tupleRes = await GetEmployeeId(request.TransactionId, mileagePluNumber, displayEmployeeId);
                employeeId = tupleRes.employeeId;
                displayEmployeeId = tupleRes.displayEmployeeId;
            }


            if (string.IsNullOrEmpty(employeeId))
            {
                throw new MOBUnitedException("Unable to get employee profile.");
            }

            List<MOBKVP> savedTravelersMPList = new List<MOBKVP>();
            MOBCPTraveler profileOwnerDetails = new MOBCPTraveler();
            List<MOBCPTraveler> mobTravelers = new List<MOBCPTraveler>();
            var persistedReservation = await PersistedReservation(request);
            var isRequireNationalityAndResidence = IsEnabledNationalityAndResidence(false, request.Application.Id, request.Application.Version.Major)
                                                    && (persistedReservation?.ShopReservationInfo2?.InfoNationalityAndResidence?.IsRequireNationalityAndResidence ?? false);

            if (travelers != null && travelers.Count > 0)
            {
                int i = 0;
                foreach (var traveler in travelers)
                {
                    if (traveler.IsProfileOwner)
                    {
                        MOBCPTraveler mobTraveler = new MOBCPTraveler
                        {
                            PaxIndex = i
                        };
                        i++;
                        mobTraveler.CustomerId = traveler.CustomerId;
                        if (traveler.BirthDate != null)
                        {
                            mobTraveler.BirthDate = traveler.BirthDate.GetValueOrDefault().ToString("MM/dd/yyyy");
                        }
                        mobTraveler.FirstName = traveler.FirstName;
                        mobTraveler.GenderCode = traveler.GenderCode;
                        mobTraveler.IsDeceased = traveler.IsDeceased;
                        mobTraveler.IsExecutive = traveler.IsExecutive;
                        mobTraveler.IsProfileOwner = traveler.IsProfileOwner;
                        mobTraveler._employeeId = traveler.EmployeeId;
                        mobTraveler.Key = traveler.Key;
                        //mobTraveler.Key = mobTraveler.PaxIndex.ToString();
                        mobTraveler.LastName = traveler.LastName;
                        mobTraveler.MiddleName = traveler.MiddleName;
                        mobTraveler.MileagePlus = PopulateMileagePlus(traveler.MileagePlus);
                        if (mobTraveler.MileagePlus != null)
                        {
                            mobTraveler.MileagePlus.MpCustomerId = traveler.CustomerId;
                            if (request != null && ConfigUtility.IncludeTravelBankFOP(request.Application.Id, request.Application.Version.Major))
                            {
                                mobTraveler.MileagePlus.TravelBankBalance = await GetTravelBankBalance(request, mobTraveler.MileagePlus.MileagePlusId);
                            }
                        }
                        mobTraveler.OwnerFirstName = traveler.OwnerFirstName;
                        mobTraveler.OwnerLastName = traveler.OwnerLastName;
                        mobTraveler.OwnerMiddleName = traveler.OwnerMiddleName;
                        mobTraveler.OwnerSuffix = traveler.OwnerSuffix;
                        mobTraveler.OwnerTitle = traveler.OwnerTitle;
                        mobTraveler.ProfileId = traveler.ProfileId;
                        mobTraveler.ProfileOwnerId = traveler.ProfileOwnerId;
                        bool isTSAFlagOn = false;
                        if (traveler.SecureTravelers != null && traveler.SecureTravelers.Count > 0)
                        {
                            if (request == null)
                            {
                                request = new MOBCPProfileRequest
                                {
                                    SessionId = string.Empty,
                                    DeviceId = string.Empty,
                                    Application = new MOBApplication() { Id = 0 }
                                };
                            }
                            mobTraveler.SecureTravelers = _mPTraveler.PopulatorSecureTravelers(traveler.SecureTravelers, ref isTSAFlagOn, false, request.SessionId, request.Application.Id, request.DeviceId);
                            if (mobTraveler.SecureTravelers != null && mobTraveler.SecureTravelers.Count > 0)
                            {
                                mobTraveler.RedressNumber = mobTraveler.SecureTravelers[0].RedressNumber;
                                mobTraveler.KnownTravelerNumber = mobTraveler.SecureTravelers[0].KnownTravelerNumber;
                            }
                        }
                        mobTraveler.IsTSAFlagON = isTSAFlagOn;
                        if (mobTraveler.IsProfileOwner)
                        {
                            isProfileOwnerTSAFlagOn = isTSAFlagOn;
                        }
                        mobTraveler.Suffix = traveler.Suffix;
                        mobTraveler.Title = traveler.Title;
                        mobTraveler.TravelerTypeCode = traveler.TravelerTypeCode;
                        mobTraveler.TravelerTypeDescription = traveler.TravelerTypeDescription;
                        mobTraveler.TravelProgramMemberId = traveler.TravProgramMemberId;

                        if (traveler != null)
                        {
                            if (traveler.MileagePlus != null)
                            {
                                mobTraveler.CurrentEliteLevel = traveler.MileagePlus.CurrentEliteLevel;
                                //mobTraveler.AirRewardPrograms = GetTravelerLoyaltyProfile(traveler.AirPreferences, traveler.MileagePlus.CurrentEliteLevel);
                            }
                        }
                        else if (_configuration.GetValue<bool>("BugFixToggleFor17M"))
                        {
                            //mobTraveler.CurrentEliteLevel = GetCurrentEliteLevel(mileagePluNumber);//**// Need to work on this with a test scenario with a Saved Traveler added MP Account with a Elite Status. Try to Add a saved traveler(with MP WX664656) to MP Account VW344781
                            /// 195113 : Booking - Travel Options -mAPP: Booking: PA tile is displayed for purchase in Customize screen for Elite Premier member travelling and Login with General member
                            /// /// 228346 : mAPP: Booking-Sponser vs Traveler: MP status wrongly displayed in seatmap for login with General member and selected/provided travler as 1K MP member
                            /// Srini - 12/04/2017
                            /// Calling getprofile for each traveler to get elite level for a traveler, who hav mp#
                            mobTraveler.MileagePlus = await _mPTraveler.GetCurrentEliteLevelFromAirPreferences(traveler.AirPreferences, request.SessionId);
                            if (mobTraveler != null)
                            {
                                if (mobTraveler.MileagePlus != null)
                                {
                                    mobTraveler.CurrentEliteLevel = mobTraveler.MileagePlus.CurrentEliteLevel;
                                }
                            }
                        }
                        mobTraveler.AirRewardPrograms = GetTravelerLoyaltyProfile(traveler.AirPreferences, mobTraveler.CurrentEliteLevel);
                        mobTraveler.Phones = _mPTraveler.PopulatePhones(traveler.Phones, true);

                        if (mobTraveler.IsProfileOwner)
                        {
                            // These Phone and Email details for Makre Reseravation Phone and Email reason is mobTraveler.Phones = PopulatePhones(traveler.Phones,true) will get only day of travel contacts to register traveler & edit traveler.
                            mobTraveler.ReservationPhones = _mPTraveler.PopulatePhones(traveler.Phones, false);
                            mobTraveler.ReservationEmailAddresses = _mPTraveler.PopulateEmailAddresses(traveler.EmailAddresses, false);
                        }
                        if (mobTraveler.IsProfileOwner && request == null) //**PINPWD//mobTraveler.IsProfileOwner && request == null Means GetProfile and Populate is for MP PIN PWD Path
                        {
                            mobTraveler.ReservationEmailAddresses = PopulateAllEmailAddresses(traveler.EmailAddresses);
                        }
                        mobTraveler.AirPreferences = _mPTraveler.PopulateAirPrefrences(traveler.AirPreferences);
                        mobTraveler.Addresses = _mPTraveler.PopulateTravelerAddresses(traveler.Addresses, request?.Application, request?.Flow);
                        mobTraveler.EmailAddresses = _mPTraveler.PopulateEmailAddresses(traveler.EmailAddresses, true);
                        mobTraveler.CreditCards = await PopulateCreditCards(traveler.CreditCards, isGetCreditCardDetailsCall, mobTraveler.Addresses);

                        //if ((mobTraveler.IsTSAFlagON && string.IsNullOrEmpty(mobTraveler.Title)) || string.IsNullOrEmpty(mobTraveler.FirstName) || string.IsNullOrEmpty(mobTraveler.LastName) || string.IsNullOrEmpty(mobTraveler.GenderCode) || string.IsNullOrEmpty(mobTraveler.BirthDate)) //|| mobTraveler.Phones == null || (mobTraveler.Phones != null && mobTraveler.Phones.Count == 0)
                        if (mobTraveler.IsTSAFlagON || string.IsNullOrEmpty(mobTraveler.FirstName) || string.IsNullOrEmpty(mobTraveler.LastName) || string.IsNullOrEmpty(mobTraveler.GenderCode) || string.IsNullOrEmpty(mobTraveler.BirthDate)) //|| mobTraveler.Phones == null || (mobTraveler.Phones != null && mobTraveler.Phones.Count == 0)
                        {
                            mobTraveler.Message = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage");
                        }

                        if (isRequireNationalityAndResidence)
                        {
                            if (string.IsNullOrEmpty(traveler.CountryOfResidence) || string.IsNullOrEmpty(traveler.Nationality))
                            {
                                mobTraveler.Message = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage");
                            }
                        }
                        mobTraveler.Nationality = traveler.Nationality;
                        mobTraveler.CountryOfResidence = traveler.CountryOfResidence;
                        mobTravelers.Add(mobTraveler);
                        break;
                    }
                }
            }

            //IEmployeeReservations employeeReservations = new EmployeeReservations(_employeeReservations );
            //var employeeJA = employeeReservations.GetEResEmp20PassriderDetails(employeeId, request.Token, request.TransactionId, request.Application.Id, request.Application.Version.Major, request.DeviceId);
            var employeeJA = await _employeeReservations.GetEResEmp20PassriderDetails(employeeId, request.Token, request.TransactionId, request.Application.Id, request.Application.Version.Major, request.DeviceId);
            if (employeeJA?.PassRiders?.Any() ?? false)
            {
                //Populate people on JA other than employee
                int paxIndex = 1;
                foreach (var passRider in employeeJA?.PassRiders)
                {
                    var isMoreInfoRequired = false;

                    if (string.IsNullOrEmpty(passRider.FirstName)
                        || string.IsNullOrEmpty(passRider.LastName)
                        || string.IsNullOrEmpty(passRider.Gender)
                        || string.IsNullOrEmpty(passRider.BirthDate.ToString("MM/dd/yyyy")))
                    {
                        isMoreInfoRequired = true;
                    }
                    if (isRequireNationalityAndResidence)
                    {
                        if (string.IsNullOrEmpty(passRider.Residence) || string.IsNullOrEmpty(passRider.Citizenship))
                        {
                            isMoreInfoRequired = true;
                        }
                    }
                    MOBCPTraveler mobTraveler = new MOBCPTraveler
                    {
                        PaxIndex = paxIndex,
                        Key = passRider.DependantID,
                        FirstName = passRider.FirstName,
                        MiddleName = passRider.MiddleName,
                        LastName = passRider.LastName,
                        BirthDate = passRider.BirthDate.ToString("MM/dd/yyyy"),
                        GenderCode = passRider.Gender,
                        KnownTravelerNumber = passRider.SSRs.FirstOrDefault(s => s.Description.Equals("Known Traveler Number", StringComparison.InvariantCultureIgnoreCase))?.KnownTraveler,
                        RedressNumber = passRider.SSRs.FirstOrDefault(s => s.Description.Equals("Known Traveler Number", StringComparison.InvariantCultureIgnoreCase))?.Redress,
                        CountryOfResidence = passRider.Residence,
                        Nationality = passRider.Citizenship,
                        Message = isMoreInfoRequired ? _configuration.GetValue<string>("SavedTravelerInformationNeededMessage") : string.Empty,
                        _employeeId = passRider.DependantID,
                        Phones = await GetPassriderPhoneList(passRider.DayOfContactInformation),
                        EmailAddresses = GetPassriderEmail(passRider.DayOfContactInformation)
                    };
                    mobTravelers.Add(mobTraveler);
                    paxIndex++;
                }
            }

            return (mobTravelers, isProfileOwnerTSAFlagOn, savedTravelersMPList);
        }
        public async Task<double> GetTravelBankBalance(MOBCPProfileRequest request, string mileagePlusId)
        {
            double tbBalance = 0.00;
            string cslLoyaltryBalanceServiceResponse = await _loyaltyUCBService.GetLoyaltyBalance(request.Token, request.MileagePlusNumber, request.SessionId).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(cslLoyaltryBalanceServiceResponse))
            {
                United.TravelBank.Model.BalancesDataModel.BalanceResponse PlusPointResponse = JsonSerializer.NewtonSoftDeserialize<United.TravelBank.Model.BalancesDataModel.BalanceResponse>(cslLoyaltryBalanceServiceResponse);
                United.TravelBank.Model.BalancesDataModel.Balance tbbalance = PlusPointResponse.Balances.FirstOrDefault(tb => tb.ProgramCurrencyType == United.TravelBank.Model.TravelBankConstants.ProgramCurrencyType.UBC);
                if (tbbalance != null && tbbalance.TotalBalance > 0)
                {
                    tbBalance = (double)tbbalance.TotalBalance;
                }
            }
            return tbBalance;
        }

        public async Task<List<MOBCPPhone>> GetPassriderPhoneList(Mobile.Model.Common.DayOfContactInformation contactInfo)
        {
            if (!string.IsNullOrEmpty(contactInfo?.PhoneNumber?.Trim()) && !string.IsNullOrEmpty(contactInfo.CountryCode?.Trim()) && !string.IsNullOrEmpty(contactInfo.DialCode?.Trim()))
            {
                bool enableEmp20PassRiderFixforPhnNoAreaCode = await _featureSettings.GetFeatureSettingValue("EnableEmp20PassRiderFixforPhnNoAreaCode").ConfigureAwait(false);
                return new List<MOBCPPhone>()
                        {
                           new MOBCPPhone()
                           {
                               AreaNumber = enableEmp20PassRiderFixforPhnNoAreaCode ? contactInfo.PhoneNumber.Substring(0,3) : string.Empty,
                               PhoneNumber = enableEmp20PassRiderFixforPhnNoAreaCode ? contactInfo.PhoneNumber.Substring(3) : contactInfo.PhoneNumber,
                               CountryCode = contactInfo.CountryCode,
                               CountryPhoneNumber = contactInfo.DialCode
                           }
                        };
            }

            return null;
        }

        private static List<MOBEmail> GetPassriderEmail(Mobile.Model.Common.DayOfContactInformation contactInfo)
        {
            if (!string.IsNullOrEmpty(contactInfo?.Email))
            {
                return new List<MOBEmail>()
                        {
                            new MOBEmail()
                            {
                                EmailAddress=contactInfo.Email
                            }
                        };
            }

            return null;
        }
        #endregion updated member profile End

        public async Task<string> GetOnlyEmpIDForWalletCall(MOBCPProfileRequest request, bool getEmployeeIdFromCSLCustomerData = false)
        {
            string employeeId = string.Empty;

            if (request == null)
            {
                throw new MOBUnitedException("Profile request cannot be null.");
            }

            United.Services.Customer.Common.ProfileRequest profileRequest = GetEmpIDCSLRequest(request, getEmployeeIdFromCSLCustomerData);
            string jsonRequest = JsonSerializer.Serialize<United.Services.Customer.Common.ProfileRequest>(profileRequest);
            var response = await _customerDataService.GetCustomerData<United.Services.Customer.Common.ProfileResponse>(request.Token, request.SessionId, jsonRequest).ConfigureAwait(false);
            if (response.response != null)
            {
                if (response.response != null && response.response.Status.Equals(United.Services.Customer.Common.Constants.StatusType.Success)
                    && response.response.Profiles != null && response.response.Profiles.Count > 0 && response.response.Profiles[0].Travelers != null &&
                    response.response.Profiles[0].Travelers.Count > 0 && response.response.Profiles[0].Travelers[0].EmployeeId != null)
                {
                    employeeId = response.response.Profiles[0].Travelers[0].EmployeeId;
                }
                else
                {
                    return string.Empty;
                }
            }

            return employeeId;
        }
        private United.Services.Customer.Common.ProfileRequest GetEmpIDCSLRequest(MOBCPProfileRequest mobCPProfileRequest, bool getEmployeeIdFromCSLCustomerData = false)
        {
            #region
            United.Services.Customer.Common.ProfileRequest request = new United.Services.Customer.Common.ProfileRequest
            {
                DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices")
            };
            List<string> requestStringList = new List<string>
            {
                "ProfileOwnerOnly"
            };
            if (mobCPProfileRequest.CustomerId != 0)
            {
                request.CustomerId = mobCPProfileRequest.CustomerId;
            }
            else
            {
                if (!string.IsNullOrEmpty(mobCPProfileRequest.MileagePlusNumber))
                {
                    request.LoyaltyId = mobCPProfileRequest.MileagePlusNumber;
                }
                else
                {
                    throw new MOBUnitedException("Profile Owner MileagePlus number is required.");
                }
            }
            if (getEmployeeIdFromCSLCustomerData)
            {
                requestStringList.Add("EmployeeLinkage");
            }
            request.DataToLoad = requestStringList;
            #endregion
            return request;
        }


        #endregion
        #region UCB Migration Mobile Phase 3 Changes
        public async Task<List<MOBCPProfile>> GetEmpProfileV2(MOBCPProfileRequest request, bool getEmployeeIdFromCSLCustomerData = false)
        {

            if (request == null)
            {
                throw new MOBUnitedException("Profile request cannot be null.");
            }
            List<MOBCPProfile> profiles = null;


            var response = await _customerProfile.GetProfileDetails(request);
           

            if (response != null && response.Data != null)
            {
                profiles = await PopulateEmpProfilesV2(request.SessionId, request.MileagePlusNumber, request.CustomerId, response.Data.Travelers, request, getEmployeeIdFromCSLCustomerData);
            }
            else
            {
                throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            return profiles;
        }
        private async Task<List<MOBCPProfile>> PopulateEmpProfilesV2(string sessionId, string mileagePlusNumber, int customerId, List<TravelerProfileResponse> profilesTravelers, MOBCPProfileRequest request, bool getEmployeeIdFromCSLCustomerData = false)
        {
            List<MOBCPProfile> mobProfiles = null;
            if (profilesTravelers != null && profilesTravelers.Count > 0)
            {
                mobProfiles = new List<MOBCPProfile>();
                if (!string.IsNullOrEmpty(sessionId))
                {
                    CSLProfile persistedCSLProfile = new CSLProfile();
                    persistedCSLProfile = await _sessionHelperService.GetSession<CSLProfile>(sessionId, "United.Persist.Definition.Profile.CSLProfile", new List<String> { sessionId, "United.Persist.Definition.Profile.CSLProfile" }).ConfigureAwait(false);
                    if (persistedCSLProfile == null)
                    {
                        persistedCSLProfile = new CSLProfile
                        {
                            SessionId = sessionId,
                            MileagePlusNumber = mileagePlusNumber,
                            CustomerId = customerId
                        };
                    }
                    if (persistedCSLProfile.Profiles == null)
                    {

                        persistedCSLProfile.Profiles = mobProfiles;
                    }
                    else
                    {
                        mobProfiles = persistedCSLProfile.Profiles;
                    }
                }
                TravelerProfileResponse owner = profilesTravelers.First(t => t.Profile?.ProfileOwnerIndicator == true);
                if (owner != null && owner.AirPreferences != null && owner.Profile != null)
                {
                    MOBCPProfile mobProfile = new MOBCPProfile
                    {
                        AirportCode = owner.AirPreferences[0].AirportCode,
                        AirportNameLong = owner.AirPreferences[0].AirportNameLong,
                        AirportNameShort = owner.AirPreferences[0].AirportNameShort,
                        // Description = profile.Description,No longer sent by new service
                        // Key = profile.Key,No longer sent by new service
                        // LanguageCode = profile.LanguageCode,No longer sent by new service
                        ProfileId = Convert.ToInt32(owner.Profile.ProfileId),
                        //ProfileMembers = PopulateProfileMembers(profile.ProfileMembers),No longer sent by new service itis just duplicate info 
                        ProfileOwnerId = Convert.ToInt32(owner.Profile.ProfileOwnerId),
                        ProfileOwnerKey = owner.Profile.TravelerKey
                        // QuickCustomerId = profile.QuickCustomerId,No longer sent by new service
                        // QuickCustomerKey = profile.QuickCustomerKey No longer sent by new service
                    };
                    bool isProfileOwnerTSAFlagOn = false;
                    List<MOBKVP> mpList = new List<MOBKVP>();
                    if (_configuration.GetValue<bool>("GetEmp20PassRidersFromEResService"))
                    {
                        var tupleResponse = await PopulateEmpTravelersV2(profilesTravelers, mileagePlusNumber, isProfileOwnerTSAFlagOn, false, request, sessionId, getEmployeeIdFromCSLCustomerData);
                        mobProfile.Travelers = tupleResponse.Item1;
                        isProfileOwnerTSAFlagOn = tupleResponse.isProfileOwnerTSAFlagOn;
                        mpList = tupleResponse.savedTravelersMPList;
                    }

                    mobProfile.SavedTravelersMPList = mpList;
                    mobProfile.IsProfileOwnerTSAFlagON = isProfileOwnerTSAFlagOn;
                    if (mobProfile != null)
                    {
                        mobProfile.DisclaimerList = await _mPTraveler.GetProfileDisclaimerList();
                    }
                    mobProfiles.Add(mobProfile);
                }

            }

            return mobProfiles;
        }
        private async Task<(List<MOBCPTraveler>, bool isProfileOwnerTSAFlagOn, List<MOBKVP> savedTravelersMPList)> PopulateEmpTravelersV2(List<TravelerProfileResponse> travelers, string mileagePluNumber, bool isProfileOwnerTSAFlagOn, bool isGetCreditCardDetailsCall, MOBCPProfileRequest request, string sessionId, bool getEmployeeIdFromCSLCustomerData = false)
        {
            string displayEmployeeId = string.Empty;
            string employeeId = string.Empty;
            OwnerResponseModel profileOwnerResponse = new OwnerResponseModel();
            if (getEmployeeIdFromCSLCustomerData && travelers != null && travelers.Count > 0)
            {
                var owner = travelers.FirstOrDefault(traveler => traveler.Profile?.ProfileOwnerIndicator == true);
                if (owner != null)
                {
                    employeeId = travelers[0].Profile?.EmployeeId;
                }
            }
            else
            {
                var tupleRes = await GetEmployeeId(request.TransactionId, mileagePluNumber, displayEmployeeId);
                employeeId = tupleRes.employeeId;
                displayEmployeeId = tupleRes.displayEmployeeId;
            }


            if (string.IsNullOrEmpty(employeeId))
            {
                throw new MOBUnitedException("Unable to get employee profile.");
            }

            List<MOBKVP> savedTravelersMPList = new List<MOBKVP>();
            MOBCPTraveler profileOwnerDetails = new MOBCPTraveler();
            List<MOBCPTraveler> mobTravelers = new List<MOBCPTraveler>();
            var persistedReservation = await PersistedReservation(request);
            var isRequireNationalityAndResidence = IsEnabledNationalityAndResidence(false, request.Application.Id, request.Application.Version.Major)
                                                    && (persistedReservation?.ShopReservationInfo2?.InfoNationalityAndResidence?.IsRequireNationalityAndResidence ?? false);

            if (travelers != null && travelers.Count > 0)
            {
                int i = 0;
                var traveler = travelers.FirstOrDefault(traveler => traveler.Profile?.ProfileOwnerIndicator == true);
                if (traveler != null && traveler.Profile != null)
                {
                    profileOwnerResponse = await _mPTraveler.GetProfileOwnerInfo(request.Token, request.SessionId, request.MileagePlusNumber);
                    MOBCPTraveler mobTraveler = new MOBCPTraveler
                    {
                        PaxIndex = i
                    };
                    i++;
                    mobTraveler.CustomerId = Convert.ToInt32(traveler.Profile?.CustomerId);
                    if (traveler.Profile?.BirthDate != null)
                    {
                        mobTraveler.BirthDate = GeneralHelper.FormatDateOfBirth(traveler.Profile.BirthDate);
                        if (mobTraveler.BirthDate == "01/01/1")
                            mobTraveler.BirthDate = null;
                    }
                    mobTraveler.FirstName = traveler.Profile?.FirstName;
                    mobTraveler.GenderCode = traveler.Profile?.Gender.ToString() == "Undefined" ? "" : traveler.Profile.Gender.ToString();
                    mobTraveler.IsDeceased = mobTraveler.IsDeceased = profileOwnerResponse?.MileagePlus?.Data?.IsDeceased == true;
                    //mobTraveler.IsExecutive = traveler.IsExecutive;
                    mobTraveler.IsProfileOwner = traveler.Profile?.ProfileOwnerIndicator == true;
                    mobTraveler._employeeId = traveler.Profile?.EmployeeId;
                    mobTraveler.Key = traveler.Profile?.TravelerKey;
                    //mobTraveler.Key = mobTraveler.PaxIndex.ToString();
                    mobTraveler.LastName = traveler.Profile?.LastName;
                    mobTraveler.MiddleName = traveler.Profile?.MiddleName;
                    mobTraveler.MileagePlus = _mPTraveler.PopulateMileagePlusV2(profileOwnerResponse, request.MileagePlusNumber);
                    if (mobTraveler.MileagePlus != null)
                    {
                        mobTraveler.MileagePlus.MpCustomerId = Convert.ToInt32(traveler.Profile?.CustomerId);
                        if (request != null && ConfigUtility.IncludeTravelBankFOP(request.Application.Id, request.Application.Version.Major))
                        {
                            mobTraveler.MileagePlus.TravelBankBalance = await GetTravelBankBalance(request, mobTraveler.MileagePlus.MileagePlusId);
                        }
                    }
                    //all the below commented properties no longer needed confirmed with service team
                    //mobTraveler.OwnerFirstName = traveler.OwnerFirstName;
                    //mobTraveler.OwnerLastName = traveler.OwnerLastName;
                    //mobTraveler.OwnerMiddleName = traveler.OwnerMiddleName;
                    //mobTraveler.OwnerSuffix = traveler.OwnerSuffix;
                    //mobTraveler.OwnerTitle = traveler.OwnerTitle;
                    mobTraveler.ProfileId = Convert.ToInt32(traveler.Profile.ProfileId);
                    mobTraveler.ProfileOwnerId = traveler.Profile.ProfileOwnerId;
                    bool isTSAFlagOn = false;
                    if (traveler.SecureTravelers != null)
                    {
                        if (request == null)
                        {
                            request = new MOBCPProfileRequest
                            {
                                SessionId = string.Empty,
                                DeviceId = string.Empty,
                                Application = new MOBApplication() { Id = 0 }
                            };
                        }
                        mobTraveler.SecureTravelers = _mPTraveler.PopulatorSecureTravelersV2(traveler.SecureTravelers, ref isTSAFlagOn, false, request.SessionId, request.Application.Id, request.DeviceId);
                        if (mobTraveler.SecureTravelers != null && mobTraveler.SecureTravelers.Count > 0)
                        {
                            mobTraveler.RedressNumber = mobTraveler.SecureTravelers[0].RedressNumber;
                            mobTraveler.KnownTravelerNumber = mobTraveler.SecureTravelers[0].KnownTravelerNumber;
                        }
                    }
                    mobTraveler.IsTSAFlagON = isTSAFlagOn;
                    if (mobTraveler.IsProfileOwner)
                    {
                        isProfileOwnerTSAFlagOn = isTSAFlagOn;
                    }
                    mobTraveler.Suffix = traveler.Profile?.Suffix;
                    mobTraveler.Title = traveler.Profile?.Title;
                    mobTraveler.TravelerTypeCode = traveler.Profile?.TravelerTypeCode;
                    mobTraveler.TravelerTypeDescription = traveler.Profile?.TravelerTypeDescription;
                    //mobTraveler.TravelProgramMemberId = traveler.TravProgramMemberId; No longer needed by Service team  

                    if (traveler != null)
                    {
                        if (mobTraveler.MileagePlus != null)
                        {
                            mobTraveler.CurrentEliteLevel = mobTraveler.MileagePlus.CurrentEliteLevel;
                            //mobTraveler.AirRewardPrograms = GetTravelerLoyaltyProfile(traveler.AirPreferences, traveler.MileagePlus.CurrentEliteLevel);
                        }
                    }

                    mobTraveler.AirRewardPrograms = _mPTraveler.GetTravelerRewardPgrograms(traveler.RewardPrograms, mobTraveler.CurrentEliteLevel);
                    mobTraveler.Phones = _mPTraveler.PopulatePhonesV2(traveler, true);

                    if (mobTraveler.IsProfileOwner)
                    {
                        // These Phone and Email details for Makre Reseravation Phone and Email reason is mobTraveler.Phones = PopulatePhones(traveler.Phones,true) will get only day of travel contacts to register traveler & edit traveler.
                        mobTraveler.ReservationPhones = _mPTraveler.PopulatePhonesV2(traveler, false);
                        mobTraveler.ReservationEmailAddresses = _mPTraveler.PopulateEmailAddressesV2(traveler.Emails, false);
                    }
                    if (mobTraveler.IsProfileOwner && request == null) //**PINPWD//mobTraveler.IsProfileOwner && request == null Means GetProfile and Populate is for MP PIN PWD Path
                    {
                        mobTraveler.ReservationEmailAddresses = _mPTraveler.PopulateAllEmailAddressesV2(traveler.Emails);
                    }
                    mobTraveler.AirPreferences = _mPTraveler.PopulateAirPrefrencesV2(traveler);
                    mobTraveler.Addresses = _mPTraveler.PopulateTravelerAddressesV2(traveler.Addresses, request?.Application, request?.Flow);
                    mobTraveler.EmailAddresses = _mPTraveler.PopulateEmailAddressesV2(traveler.Emails, true);
                    mobTraveler.CreditCards = await _profileCreditCard.PopulateCreditCards(isGetCreditCardDetailsCall, mobTraveler.Addresses, request);

                    //if ((mobTraveler.IsTSAFlagON && string.IsNullOrEmpty(mobTraveler.Title)) || string.IsNullOrEmpty(mobTraveler.FirstName) || string.IsNullOrEmpty(mobTraveler.LastName) || string.IsNullOrEmpty(mobTraveler.GenderCode) || string.IsNullOrEmpty(mobTraveler.BirthDate)) //|| mobTraveler.Phones == null || (mobTraveler.Phones != null && mobTraveler.Phones.Count == 0)
                    if (mobTraveler.IsTSAFlagON || string.IsNullOrEmpty(mobTraveler.FirstName) || string.IsNullOrEmpty(mobTraveler.LastName) || string.IsNullOrEmpty(mobTraveler.GenderCode) || string.IsNullOrEmpty(mobTraveler.BirthDate)) //|| mobTraveler.Phones == null || (mobTraveler.Phones != null && mobTraveler.Phones.Count == 0)
                    {
                        mobTraveler.Message = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage");
                    }

                    if (isRequireNationalityAndResidence)
                    {
                        if (string.IsNullOrEmpty(traveler.CustomerAttributes?.CountryofResidence) || string.IsNullOrEmpty(traveler.CustomerAttributes?.Nationality))
                        {
                            mobTraveler.Message = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage");
                        }
                    }
                    mobTraveler.Nationality = traveler.CustomerAttributes?.Nationality;
                    mobTraveler.CountryOfResidence = traveler.CustomerAttributes?.CountryofResidence;
                    mobTravelers.Add(mobTraveler);
                }
            }


            //IEmployeeReservations employeeReservations = new EmployeeReservations(_employeeReservations );
            //var employeeJA = employeeReservations.GetEResEmp20PassriderDetails(employeeId, request.Token, request.TransactionId, request.Application.Id, request.Application.Version.Major, request.DeviceId);
            var employeeJA = await _employeeReservations.GetEResEmp20PassriderDetails(employeeId, request.Token, request.TransactionId, request.Application.Id, request.Application.Version.Major, request.DeviceId);
            if (employeeJA?.PassRiders?.Any() ?? false)
            {
                //Populate people on JA other than employee
                int paxIndex = 1;
                foreach (var passRider in employeeJA?.PassRiders)
                {
                    var isMoreInfoRequired = false;

                    if (string.IsNullOrEmpty(passRider.FirstName)
                        || string.IsNullOrEmpty(passRider.LastName)
                        || string.IsNullOrEmpty(passRider.Gender)
                        || string.IsNullOrEmpty(passRider.BirthDate.ToString("MM/dd/yyyy")))
                    {
                        isMoreInfoRequired = true;
                    }
                    if (isRequireNationalityAndResidence)
                    {
                        if (string.IsNullOrEmpty(passRider.Residence) || string.IsNullOrEmpty(passRider.Citizenship))
                        {
                            isMoreInfoRequired = true;
                        }
                    }
                    MOBCPTraveler mobTraveler = new MOBCPTraveler
                    {
                        PaxIndex = paxIndex,
                        Key = passRider.DependantID,
                        FirstName = passRider.FirstName,
                        MiddleName = passRider.MiddleName,
                        LastName = passRider.LastName,
                        BirthDate = passRider.BirthDate.ToString("MM/dd/yyyy"),
                        GenderCode = passRider.Gender,
                        KnownTravelerNumber = passRider.SSRs.FirstOrDefault(s => s.Description.Equals("Known Traveler Number", StringComparison.InvariantCultureIgnoreCase))?.KnownTraveler,
                        RedressNumber = passRider.SSRs.FirstOrDefault(s => s.Description.Equals("Known Traveler Number", StringComparison.InvariantCultureIgnoreCase))?.Redress,
                        CountryOfResidence = passRider.Residence,
                        Nationality = passRider.Citizenship,
                        Message = isMoreInfoRequired ? _configuration.GetValue<string>("SavedTravelerInformationNeededMessage") : string.Empty,
                        _employeeId = passRider.DependantID,
                        Phones = await GetPassriderPhoneList(passRider.DayOfContactInformation),
                        EmailAddresses = GetPassriderEmail(passRider.DayOfContactInformation),
                        Suffix = passRider.NameSuffix,
                        TravelerTypeCode = await _featureSettings.GetFeatureSettingValue("EnableAddTravelers").ConfigureAwait(false) ? GetTypeCodeByAge(passRider.Age) : string.Empty
                    };
                    mobTravelers.Add(mobTraveler);
                    paxIndex++;
                }
            }

            return (mobTravelers, isProfileOwnerTSAFlagOn, savedTravelersMPList);
        }
        public string GetTypeCodeByAge(int age, bool iSChildInLap = false)
        {
            if ((18 <= age) && (age <= 64))
            {
                return TravelerTypeCode.Adult;
            }
            else
            if ((2 <= age) && (age < 5))
            {
                return TravelerTypeCode.child2To4;
            }
            else
            if ((5 <= age) && (age <= 11))
            {
                return TravelerTypeCode.child5To11;
            }
            else
            if ((12 <= age) && (age <= 14))
            {
                return TravelerTypeCode.child12To14;
            }
            else
            if ((15 <= age) && (age <= 17))
            {
                return TravelerTypeCode.child15To17;
            }
            else
            if (65 <= age)
            {
                return TravelerTypeCode.Senior;
            }
            else
                if (age < 2 && iSChildInLap)
            {
                return TravelerTypeCode.InfantInLap;
            }
            else if (age < 2)
                return TravelerTypeCode.InfantWithSeat;

            return TravelerTypeCode.Adult;
        }
        private class TravelerTypeCode
        {
            public const string Adult = "ADT";
            public const string InfantWithSeat = "INS";
            public const string child2To4 = "C04";
            public const string child5To11 = "C11";
            public const string child12To14 = "C14";
            public const string child15To17 = "C17";
            public const string Senior = "SNR";
            public const string InfantInLap = "INF";
        }
        #endregion 
    }
}
