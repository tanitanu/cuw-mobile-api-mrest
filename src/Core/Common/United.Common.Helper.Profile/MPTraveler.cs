﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using United.Definition;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.Loyalty;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.MPAuthentication;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.CSLModels;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Service.Presentation.CommonModel;
using United.Services.Customer.Common;
using United.Services.FlightShopping.Common.Extensions;
using United.Utility.Enum;
using United.Utility.Helper;
using InsertTravelerRequest = United.Mobile.Model.Shopping.InsertTravelerRequest;
using JsonSerializer = United.Utility.Helper.DataContextJsonSerializer;
using MOBItem = United.Mobile.Model.Common.MOBItem;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using Session = United.Mobile.Model.Common.Session;
using Status = United.Service.Presentation.CommonModel.Status;
using Traveler = United.Services.Customer.Common.Traveler;
using TravelType = United.Common.Helper.Enum.TravelType;
using United.Common.Helper.Shopping;

namespace United.Common.Helper.Profile
{
    public class MPTraveler : IMPTraveler
    {
        private readonly IConfiguration _configuration;
        private readonly IReferencedataService _referencedataService;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly ICustomerDataService _customerDataService;
        private readonly IDataVaultService _dataVaultService;
        private readonly IUtilitiesService _utilitiesService;
        private readonly ICacheLog<MPTraveler> _logger;
        private readonly IPNRRetrievalService _pNRRetrievalService;
        private readonly IProfileCreditCard _profileCreditCard;
        private readonly IMPSecurityCheckDetailsService _mPSecurityCheckDetailsService;
        private readonly IBaseEmployeeResService _baseEmployeeRes;
        private readonly IEServiceCheckin _eServiceCheckin;
        private readonly IInsertOrUpdateTravelInfoService _insertOrUpdateTravelInfoService;
        private readonly ILoyaltyUCBService _loyaltyUCBService;
        private readonly ICustomerProfileOwnerService _customerProfileOwnerService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly ICustomerPreferencesService _customerPreferencesService;
        private readonly ICustomerProfileCreditCardsService _customerProfileCreditCardsService;
        private readonly ICustomerTravelerService _customerTravelerService;
        private readonly ICorporateProfile _corporateProfile;
        private readonly IFeatureToggles _featureToggles;
        private readonly IFFCShoppingcs _fFCShoppingcs;

        private string _deviceId = string.Empty;
        private bool IsCorpBookingPath = false;
        private bool IsArrangerBooking = false;
        private readonly IHeaders _headers;

        private MOBApplication _application = new MOBApplication() { Version = new MOBVersion() };

        public MPTraveler(IConfiguration configuration
            , IReferencedataService referencedataService
            , ISessionHelperService sessionHelperService
            , ICustomerDataService mPEnrollmentService
            , IDataVaultService dataVaultService
            , IUtilitiesService utilitiesService
            , ICacheLog<MPTraveler> logger
            , IPNRRetrievalService pNRRetrievalService
            , IProfileCreditCard profileCreditCard
            , IMPSecurityCheckDetailsService mPSecurityCheckDetailsService
            , IBaseEmployeeResService baseEmployeeRes
            , IEServiceCheckin eServiceCheckin
            , IInsertOrUpdateTravelInfoService insertOrUpdateTravelInfoService
            , ILoyaltyUCBService loyaltyUCBService
            , ICustomerProfileOwnerService customerProfileOwnerService
            , IHeaders headers
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , ICustomerPreferencesService customerPreferencesService
            , ICustomerProfileCreditCardsService customerProfileCreditCardsService
            , ICustomerTravelerService customerTravelerService
            , ICorporateProfile corporateProfile
            , IFeatureToggles featureToggles
            , IFFCShoppingcs fFCShoppingcs
            )
        {
            _configuration = configuration;
            _referencedataService = referencedataService;
            _sessionHelperService = sessionHelperService;
            _customerDataService = mPEnrollmentService;
            _dataVaultService = dataVaultService;
            _utilitiesService = utilitiesService;
            _logger = logger;
            _pNRRetrievalService = pNRRetrievalService;
            _profileCreditCard = profileCreditCard;
            _mPSecurityCheckDetailsService = mPSecurityCheckDetailsService;
            _baseEmployeeRes = baseEmployeeRes;
            _eServiceCheckin = eServiceCheckin;
            _insertOrUpdateTravelInfoService = insertOrUpdateTravelInfoService;
            _loyaltyUCBService = loyaltyUCBService;
            _customerProfileOwnerService = customerProfileOwnerService;
            _headers = headers;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _customerPreferencesService = customerPreferencesService;
            _customerProfileCreditCardsService = customerProfileCreditCardsService;
            _customerTravelerService = customerTravelerService;
            new ConfigUtility(_configuration);
            //_profileCreditCard = new ProfileCreditCard(null, _configuration, _sessionHelperService, _dataVaultService);
            _corporateProfile = corporateProfile;
            _featureToggles = featureToggles;
            _fFCShoppingcs = fFCShoppingcs;
        }

        #region Methods
        private bool IsCorporateLeisureFareSelected(List<MOBSHOPTrip> trips)
        {
            string corporateFareText = _configuration.GetValue<string>("FSRLabelForCorporateLeisure") ?? string.Empty;
            if (trips != null)
            {
                return trips.Any(
                   x =>
                       x.FlattenedFlights.Any(
                           f =>
                               f.Flights.Any(
                                   fl =>
                                       fl.CorporateFareIndicator ==
                                       corporateFareText.ToString())));
            }

            return false;
        }
        public async Task<(List<MOBCPTraveler> mobTravelersOwnerFirstInList, bool isProfileOwnerTSAFlagOn, List<MOBKVP> savedTravelersMPList)> PopulateTravelers(List<Traveler> travelers, string mileagePluNumber,  bool isProfileOwnerTSAFlagOn, bool isGetCreditCardDetailsCall, MOBCPProfileRequest request, string sessionid, bool getMPSecurityDetails = false, string path = "")
        {
            var savedTravelersMPList = new List<MOBKVP>();
            List<MOBCPTraveler> mobTravelers = null;
            List<MOBCPTraveler> mobTravelersOwnerFirstInList = null;
            MOBCPTraveler profileOwnerDetails = new MOBCPTraveler();
            if (travelers != null && travelers.Count > 0)
            {
                mobTravelers = new List<MOBCPTraveler>();
                int i = 0;
                var persistedReservation = await PersistedReservation(request);

                foreach (Traveler traveler in travelers)
                {
                    #region
                    MOBCPTraveler mobTraveler = new MOBCPTraveler();
                    mobTraveler.PaxIndex = i; i++;
                    mobTraveler.CustomerId = traveler.CustomerId;
                    if (_configuration.GetValue<bool>("NGRPAwardCalendarMP2017Switch"))
                    {
                        mobTraveler.CustomerMetrics = PopulateCustomerMetrics(traveler.CustomerMetrics);
                    }
                    if (traveler.BirthDate != null)
                    {
                        mobTraveler.BirthDate = GeneralHelper.FormatDateOfBirth(traveler.BirthDate.GetValueOrDefault());
                        
                    }
                    if (_configuration.GetValue<bool>("EnableNationalityAndCountryOfResidence"))
                    {
                        if (persistedReservation != null && persistedReservation.ShopReservationInfo2 != null && persistedReservation.ShopReservationInfo2.InfoNationalityAndResidence != null
                            && persistedReservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence)
                        {
                            if (string.IsNullOrEmpty(traveler.CountryOfResidence) || string.IsNullOrEmpty(traveler.Nationality))
                            {
                                mobTraveler.Message = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage");
                            }
                        }
                        mobTraveler.Nationality = traveler.Nationality;
                        mobTraveler.CountryOfResidence = traveler.CountryOfResidence;
                    }

                    mobTraveler.FirstName = traveler.FirstName;
                    mobTraveler.GenderCode = traveler.GenderCode;
                    mobTraveler.IsDeceased = traveler.IsDeceased;
                    mobTraveler.IsExecutive = traveler.IsExecutive;
                    mobTraveler.IsProfileOwner = traveler.IsProfileOwner;
                    mobTraveler.Key = traveler.Key;
                    mobTraveler.LastName = traveler.LastName;
                    mobTraveler.MiddleName = traveler.MiddleName;
                    mobTraveler.MileagePlus = PopulateMileagePlus(traveler.MileagePlus);
                    if (mobTraveler.MileagePlus != null)
                    {
                        mobTraveler.MileagePlus.MpCustomerId = traveler.CustomerId;

                        if (request != null && ConfigUtility.IncludeTravelBankFOP(request.Application.Id, request.Application.Version.Major))
                        {
                            Session session = new Session();
                            string cslLoyaltryBalanceServiceResponse = await _loyaltyUCBService.GetLoyaltyBalance(request.Token, request.MileagePlusNumber, request.SessionId);
                            if (!string.IsNullOrEmpty(cslLoyaltryBalanceServiceResponse))
                            {
                                United.TravelBank.Model.BalancesDataModel.BalanceResponse PlusPointResponse = JsonSerializer.NewtonSoftDeserialize<United.TravelBank.Model.BalancesDataModel.BalanceResponse>(cslLoyaltryBalanceServiceResponse);
                                United.TravelBank.Model.BalancesDataModel.Balance tbbalance = PlusPointResponse.Balances.FirstOrDefault(tb => tb.ProgramCurrencyType == United.TravelBank.Model.TravelBankConstants.ProgramCurrencyType.UBC);
                                if (tbbalance != null && tbbalance.TotalBalance > 0)
                                {
                                    mobTraveler.MileagePlus.TravelBankBalance = (double)tbbalance.TotalBalance;
                                }
                            }
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
                            request = new MOBCPProfileRequest();
                            request.SessionId = string.Empty;
                            request.DeviceId = string.Empty;
                            request.Application = new MOBApplication() { Id = 0 };
                        }
                        else if (request.Application == null)
                        {
                            request.Application = new MOBApplication() { Id = 0 };
                        }
                        mobTraveler.SecureTravelers = PopulatorSecureTravelers(traveler.SecureTravelers, ref isTSAFlagOn, i >= 2, request.SessionId, request.Application.Id, request.DeviceId);
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
                    mobTraveler.TravelerTypeCode = GetTravelerTypeCode(traveler.TravelerTypeCode);
                    mobTraveler.TravelerTypeDescription = traveler.TravelerTypeDescription;
                    //mobTraveler.PTCDescription = Utility.GetPaxDescription(traveler.TravelerTypeCode);
                    if (persistedReservation != null && persistedReservation.ShopReservationInfo2 != null && persistedReservation.ShopReservationInfo2.TravelerTypes != null
                        && persistedReservation.ShopReservationInfo2.TravelerTypes.Count > 0)
                    {
                        if (traveler.BirthDate != null)
                        {
                            if (EnableYADesc() && persistedReservation.ShopReservationInfo2.IsYATravel)
                            {
                                mobTraveler.PTCDescription = GetYAPaxDescByDOB();
                            }
                            else
                            {
                                mobTraveler.PTCDescription = GetPaxDescriptionByDOB(traveler.BirthDate.ToString(), persistedReservation.Trips[0].FlattenedFlights[0].Flights[0].DepartDate);
                            }
                        }
                    }
                    else
                    {
                        if (EnableYADesc() && persistedReservation != null && persistedReservation.ShopReservationInfo2 != null && persistedReservation.ShopReservationInfo2.IsYATravel)
                        {
                            mobTraveler.PTCDescription = GetYAPaxDescByDOB();
                        }
                    }
                    mobTraveler.TravelProgramMemberId = traveler.TravProgramMemberId;
                    if (traveler != null)
                    {
                        if (traveler.MileagePlus != null)
                        {
                            mobTraveler.CurrentEliteLevel = traveler.MileagePlus.CurrentEliteLevel;
                            //mobTraveler.AirRewardPrograms = GetTravelerLoyaltyProfile(traveler.AirPreferences, traveler.MileagePlus.CurrentEliteLevel);
                        }
                    }
                    else if (_configuration.GetValue<bool>("BugFixToggleFor17M") && request != null && !string.IsNullOrEmpty(request.SessionId))
                    {
                        //    mobTraveler.CurrentEliteLevel = GetCurrentEliteLevel(mileagePluNumber);//**// Need to work on this with a test scenario with a Saved Traveler added MP Account with a Elite Status. Try to Add a saved traveler(with MP WX664656) to MP Account VW344781
                        /// 195113 : Booking - Travel Options -mAPP: Booking: PA tile is displayed for purchase in Customize screen for Elite Premier member travelling and Login with General member
                        /// Srini - 12/04/2017
                        /// Calling getprofile for each traveler to get elite level for a traveler, who hav mp#
                        mobTraveler.MileagePlus = await GetCurrentEliteLevelFromAirPreferences(traveler.AirPreferences, request.SessionId);
                        if (mobTraveler != null)
                        {
                            if (mobTraveler.MileagePlus != null)
                            {
                                mobTraveler.CurrentEliteLevel = mobTraveler.MileagePlus.CurrentEliteLevel;
                            }
                        }
                    }
                    mobTraveler.AirRewardPrograms = GetTravelerLoyaltyProfile(traveler.AirPreferences, mobTraveler.CurrentEliteLevel);
                    mobTraveler.Phones = PopulatePhones(traveler.Phones, true);

                    if (mobTraveler.IsProfileOwner)
                    {
                        // These Phone and Email details for Makre Reseravation Phone and Email reason is mobTraveler.Phones = PopulatePhones(traveler.Phones,true) will get only day of travel contacts to register traveler & edit traveler.
                        mobTraveler.ReservationPhones = PopulatePhones(traveler.Phones, false);
                        mobTraveler.ReservationEmailAddresses = PopulateEmailAddresses(traveler.EmailAddresses, false);

                        // Added by Hasnan - #53484. 10/04/2017
                        // As per the Bug 53484:PINPWD: iOS and Android - Phone number is blank in RTI screen after booking from newly created account.
                        // If mobTraveler.Phones is empty. Then it newly created account. Thus returning mobTraveler.ReservationPhones as mobTraveler.Phones.
                        if (!_configuration.GetValue<bool>("EnableDayOfTravelEmail") || string.IsNullOrEmpty(path) || !path.ToUpper().Equals("BOOKING"))
                        {
                            if (mobTraveler.Phones.Count == 0)
                            {
                                mobTraveler.Phones = mobTraveler.ReservationPhones;
                            }
                        }
                        #region Corporate Leisure(ProfileOwner must travel)//Client will use the IsMustRideTraveler flag to auto select the travel and not allow to uncheck the profileowner on the SelectTraveler Screen.
                        if (_configuration.GetValue<bool>("EnableCorporateLeisure"))
                        {
                            if (persistedReservation?.ShopReservationInfo2 != null && persistedReservation.ShopReservationInfo2.TravelType == TravelType.CLB.ToString() && IsCorporateLeisureFareSelected(persistedReservation.Trips))
                            {
                                mobTraveler.IsMustRideTraveler = true;
                            }
                        }
                        #endregion Corporate Leisure
                    }
                    if (mobTraveler.IsProfileOwner && request == null) //**PINPWD//mobTraveler.IsProfileOwner && request == null Means GetProfile and Populate is for MP PIN PWD Path
                    {
                        mobTraveler.ReservationEmailAddresses = PopulateAllEmailAddresses(traveler.EmailAddresses);
                    }
                    mobTraveler.AirPreferences = PopulateAirPrefrences(traveler.AirPreferences);
                    if (request?.Application?.Version != null && string.IsNullOrEmpty(request?.Flow) && IsInternationalBillingAddress_CheckinFlowEnabled(request.Application))
                    {
                        try
                        {
                            MOBShoppingCart mobShopCart = new MOBShoppingCart();
                            mobShopCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, mobShopCart.ObjectName, new List<string> { request.SessionId, mobShopCart.ObjectName }).ConfigureAwait(false);
                            if (mobShopCart != null && !string.IsNullOrEmpty(mobShopCart.Flow) && mobShopCart.Flow == FlowType.CHECKIN.ToString())
                            {
                                request.Flow = mobShopCart.Flow;
                            }
                        }
                        catch { }
                    }
                    mobTraveler.Addresses = PopulateTravelerAddresses(traveler.Addresses, request?.Application, request?.Flow);

                    if (_configuration.GetValue<bool>("EnableDayOfTravelEmail") && !string.IsNullOrEmpty(path) && path.ToUpper().Equals("BOOKING"))
                    {
                        mobTraveler.EmailAddresses = PopulateEmailAddresses(traveler.EmailAddresses, true);
                    }
                    else
                    if (!getMPSecurityDetails)
                    {
                        mobTraveler.EmailAddresses = PopulateEmailAddresses(traveler.EmailAddresses, false);
                    }
                    else
                    {
                        mobTraveler.EmailAddresses = PopulateMPSecurityEmailAddresses(traveler.EmailAddresses);
                    }
                    mobTraveler.CreditCards = IsCorpBookingPath ?await _profileCreditCard.PopulateCorporateCreditCards(traveler.CreditCards, isGetCreditCardDetailsCall, mobTraveler.Addresses, persistedReservation, sessionid) : await _profileCreditCard.PopulateCreditCards(traveler.CreditCards, isGetCreditCardDetailsCall, mobTraveler.Addresses, sessionid);

                    //if ((mobTraveler.IsTSAFlagON && string.IsNullOrEmpty(mobTraveler.Title)) || string.IsNullOrEmpty(mobTraveler.FirstName) || string.IsNullOrEmpty(mobTraveler.LastName) || string.IsNullOrEmpty(mobTraveler.GenderCode) || string.IsNullOrEmpty(mobTraveler.BirthDate)) //|| mobTraveler.Phones == null || (mobTraveler.Phones != null && mobTraveler.Phones.Count == 0)
                    if (mobTraveler.IsTSAFlagON || string.IsNullOrEmpty(mobTraveler.FirstName) || string.IsNullOrEmpty(mobTraveler.LastName) || string.IsNullOrEmpty(mobTraveler.GenderCode) || string.IsNullOrEmpty(mobTraveler.BirthDate)) //|| mobTraveler.Phones == null || (mobTraveler.Phones != null && mobTraveler.Phones.Count == 0)
                    {
                        mobTraveler.Message = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage");
                    }
                    if (mobTraveler.IsProfileOwner)
                    {
                        profileOwnerDetails = mobTraveler;
                    }
                    else
                    {
                        #region
                        if (mobTraveler.AirRewardPrograms != null && mobTraveler.AirRewardPrograms.Count > 0)
                        {
                            var airRewardProgramList = (from program in mobTraveler.AirRewardPrograms
                                                        where program.CarrierCode.ToUpper().Trim() == "UA"
                                                        select program).ToList();

                            if (airRewardProgramList != null && airRewardProgramList.Count > 0)
                            {
                                savedTravelersMPList.Add(new MOBKVP() { Key = mobTraveler.CustomerId.ToString(), Value = airRewardProgramList[0].MemberId });
                            }
                        }
                        #endregion
                        mobTravelers.Add(mobTraveler);
                    }
                    #endregion
                }
            }
            mobTravelersOwnerFirstInList = new List<MOBCPTraveler>();
            mobTravelersOwnerFirstInList.Add(profileOwnerDetails);
            if (!IsCorpBookingPath || IsArrangerBooking)
            {
                mobTravelersOwnerFirstInList.AddRange(mobTravelers);
            }

            return (mobTravelersOwnerFirstInList,isGetCreditCardDetailsCall, savedTravelersMPList);
        }
        private string GetTravelerTypeCode(string travelerTypeCode)
        {
            if (!_configuration.GetValue<bool>("AllowAdditionalTravelers")) return travelerTypeCode;
            string travelerType = "";
            switch (travelerTypeCode)
            {
                case "INS":
                    travelerType = TravelerTypeCode.InfantWithSeat;
                    break;
                case "C04":
                case "C05":
                    travelerType = TravelerTypeCode.child2To4;
                    break;
                case "C08":
                case "C11":
                    travelerType = TravelerTypeCode.child5To11;
                    break;
                case "C12":
                    travelerType = "C12";
                    break;
                case "C14":
                case "C15":
                    travelerType = TravelerTypeCode.child12To14;
                    break;
                case "C17":
                    travelerType = TravelerTypeCode.child15To17;
                    break;
                case "SNR":
                    travelerType = TravelerTypeCode.Senior;
                    break;
                case "INF":
                    travelerType = TravelerTypeCode.InfantInLap;
                    break;
                case "ADT":
                    travelerType = TravelerTypeCode.Adult;
                    break;
            }
            return travelerType;
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
        private List<MOBEmail> PopulateMPSecurityEmailAddresses(List<Services.Customer.Common.Email> emailAddresses)
        {
            #region
            List<MOBEmail> mobEmailAddresses = new List<MOBEmail>();
            if (emailAddresses != null && emailAddresses.Count > 0)
            {
                MOBEmail primaryEmailAddress = null;
                int co = 0;
                foreach (Services.Customer.Common.Email email in emailAddresses)
                {
                    if (email.EffectiveDate <= DateTime.UtcNow && email.DiscontinuedDate >= DateTime.UtcNow)
                    {
                        #region As per Wade Change want to filter out to return only Primary email to client if not primary phone exist return the first one from the list.So lot of work at client side will save time. Sep 15th 2014
                        co = co + 1;
                        MOBEmail e = new MOBEmail();
                        e.Key = email.Key;
                        e.Channel = new SHOPChannel();
                        e.Channel.ChannelCode = email.ChannelCode;
                        e.Channel.ChannelDescription = email.ChannelCodeDescription;
                        e.Channel.ChannelTypeCode = email.ChannelTypeCode.ToString();
                        e.Channel.ChannelTypeDescription = email.ChannelTypeDescription;
                        e.EmailAddress = email.EmailAddress;
                        e.IsDefault = email.IsDefault;
                        e.IsPrimary = email.IsPrimary;
                        e.IsPrivate = email.IsPrivate;
                        e.IsDayOfTravel = email.IsDayOfTravel;
                        if (email.IsPrimary)
                        {
                            primaryEmailAddress = new MOBEmail();
                            primaryEmailAddress = e;
                            break;
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
        public List<Mobile.Model.Common.MOBAddress> PopulateTravelerAddresses(List<United.Services.Customer.Common.Address> addresses, MOBApplication application = null, string flow = null)
        {
            #region

            var mobAddresses = new List<Mobile.Model.Common.MOBAddress>();
            if (addresses != null && addresses.Count > 0)
            {
                bool isCorpAddressPresent = false;
                if (_configuration.GetValue<bool>("CorporateConcurBooking") && IsCorpBookingPath)
                {
                    //As per Business / DotCom Kalpen; we are removing the condition for checking the Effectivedate and Discontinued date
                    var corpIndex = addresses.FindIndex(x => x.ChannelTypeDescription != null && x.ChannelTypeDescription.ToLower() == "corporate" && x.AddressLine1 != null && x.AddressLine1.Trim() != "");
                    if (corpIndex >= 0)
                        isCorpAddressPresent = true;

                }
                foreach (United.Services.Customer.Common.Address address in addresses)
                {
                    if (_configuration.GetValue<bool>("CorporateConcurBooking"))
                    {
                        if (isCorpAddressPresent && address.ChannelTypeDescription.ToLower() == "corporate" &&
                            (_configuration.GetValue<bool>("USPOSCountryCodes_ByPass") || IsInternationalBilling(application, address.CountryCode, flow)))
                        {
                            var a = new Mobile.Model.Common.MOBAddress();
                            a.Key = address.Key;
                            a.Channel = new SHOPChannel();
                            a.Channel.ChannelCode = address.ChannelCode;
                            a.Channel.ChannelDescription = address.ChannelCodeDescription;
                            a.Channel.ChannelTypeCode = address.ChannelTypeCode.ToString();
                            a.Channel.ChannelTypeDescription = address.ChannelTypeDescription;
                            a.ApartmentNumber = address.AptNum;
                            a.Channel = new SHOPChannel();
                            a.Channel.ChannelCode = address.ChannelCode;
                            a.Channel.ChannelDescription = address.ChannelCodeDescription;
                            a.Channel.ChannelTypeCode = address.ChannelTypeCode.ToString();
                            a.Channel.ChannelTypeDescription = address.ChannelTypeDescription;
                            a.City = address.City;
                            a.CompanyName = address.CompanyName;
                            a.Country = new MOBCountry();
                            a.Country.Code = address.CountryCode;
                            a.Country.Name = address.CountryName;
                            a.JobTitle = address.JobTitle;
                            a.Line1 = address.AddressLine1;
                            a.Line2 = address.AddressLine2;
                            a.Line3 = address.AddressLine3;
                            a.State = new Mobile.Model.Common.State();
                            a.State.Code = address.StateCode;
                            a.IsDefault = address.IsDefault;
                            a.IsPrivate = address.IsPrivate;
                            a.PostalCode = address.PostalCode;
                            if (address.ChannelTypeDescription.ToLower().Trim() == "corporate")
                            {
                                a.IsPrimary = true;
                                a.IsCorporate = true; // MakeIsCorporate true inorder to disable the edit on client
                            }
                            // Make IsPrimary true inorder to select the corpaddress by default

                            if (_configuration.GetValue<bool>("ShowTripInsuranceBookingSwitch"))
                            {
                                a.IsValidForTPIPurchase = IsValidAddressForTPIpayment(address.CountryCode);

                                if (a.IsValidForTPIPurchase)
                                {
                                    a.IsValidForTPIPurchase = IsValidSateForTPIpayment(address.StateCode);
                                }
                            }
                            mobAddresses.Add(a);
                        }
                    }


                    if (address.EffectiveDate <= DateTime.UtcNow && address.DiscontinuedDate >= DateTime.UtcNow)
                    {
                        if (_configuration.GetValue<bool>("USPOSCountryCodes_ByPass") || IsInternationalBilling(application, address.CountryCode, flow)) //##Kirti - allow only US addresses 
                        {
                            var a = new Mobile.Model.Common.MOBAddress();
                            a.Key = address.Key;
                            a.Channel = new SHOPChannel();
                            a.Channel.ChannelCode = address.ChannelCode;
                            a.Channel.ChannelDescription = address.ChannelCodeDescription;
                            a.Channel.ChannelTypeCode = address.ChannelTypeCode.ToString();
                            a.Channel.ChannelTypeDescription = address.ChannelTypeDescription;
                            a.ApartmentNumber = address.AptNum;
                            a.Channel = new SHOPChannel();
                            a.Channel.ChannelCode = address.ChannelCode;
                            a.Channel.ChannelDescription = address.ChannelCodeDescription;
                            a.Channel.ChannelTypeCode = address.ChannelTypeCode.ToString();
                            a.Channel.ChannelTypeDescription = address.ChannelTypeDescription;
                            a.City = address.City;
                            a.CompanyName = address.CompanyName;
                            a.Country = new MOBCountry();
                            a.Country.Code = address.CountryCode;
                            a.Country.Name = address.CountryName;
                            a.JobTitle = address.JobTitle;
                            a.Line1 = address.AddressLine1;
                            a.Line2 = address.AddressLine2;
                            a.Line3 = address.AddressLine3;
                            a.State = new Mobile.Model.Common.State();
                            a.State.Code = address.StateCode;
                            //a.State.Name = address.StateName;
                            a.IsDefault = address.IsDefault;
                            a.IsPrimary = address.IsPrimary;
                            a.IsPrivate = address.IsPrivate;
                            a.PostalCode = address.PostalCode;
                            //Adding this check for corporate addresses to gray out the Edit button on the client
                            //if (address.ChannelTypeDescription.ToLower().Trim() == "corporate")
                            //{
                            //    a.IsCorporate = true;
                            //}
                            if (_configuration.GetValue<bool>("ShowTripInsuranceBookingSwitch"))
                            {
                                a.IsValidForTPIPurchase = IsValidAddressForTPIpayment(address.CountryCode);

                                if (a.IsValidForTPIPurchase)
                                {
                                    a.IsValidForTPIPurchase = IsValidSateForTPIpayment(address.StateCode);
                                }
                            }
                            mobAddresses.Add(a);
                        }
                    }
                }
            }
            return mobAddresses;
            #endregion
        }
        private bool IsInternationalBilling(MOBApplication application, string countryCode, string flow)
        {
            if (_configuration.GetValue<bool>("EnableUCBPhase1_MobilePhase3Changes") && String.IsNullOrEmpty(countryCode))
                return false;
            bool _isIntBilling = IsInternationalBillingAddress_CheckinFlowEnabled(application);
            if (_isIntBilling && flow?.ToLower() == FlowType.CHECKIN.ToString().ToLower()) // need to enable Int Billing address only in Checkin flow
            {
                //check for multiple countries
                return _isIntBilling;
            }
            else
            {
                //Normal Code as usual
                return _configuration.GetValue<string>("USPOSCountryCodes").Contains(countryCode);
            }
        }
        private bool IsValidSateForTPIpayment(string stateCode)
        {
            return !string.IsNullOrEmpty(stateCode) && !string.IsNullOrEmpty(_configuration.GetValue<string>("ExcludeUSStateCodesForTripInsurance")) && !_configuration.GetValue<string>("ExcludeUSStateCodesForTripInsurance").Contains(stateCode.ToUpper().Trim());
        }
        private bool IsValidAddressForTPIpayment(string countryCode)
        {
            return !string.IsNullOrEmpty(countryCode) && countryCode.ToUpper().Trim() == "US";
        }
        public List<MOBPrefAirPreference> PopulateAirPrefrences(List<United.Services.Customer.Common.AirPreference> airPreferences)
        {
            List<MOBPrefAirPreference> mobAirPrefs = new List<MOBPrefAirPreference>();
            if (airPreferences != null && airPreferences.Count > 0)
            {
                foreach (United.Services.Customer.Common.AirPreference pref in airPreferences)
                {
                    MOBPrefAirPreference mobAirPref = new MOBPrefAirPreference();
                    mobAirPref.AirportCode = pref.AirportCode;
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
                    MOBPrefRewardProgram mobAirRewardsProg = new MOBPrefRewardProgram();
                    mobAirRewardsProg.CustomerId = Convert.ToInt32(pref.CustomerId);
                    mobAirRewardsProg.ProfileId = Convert.ToInt32(pref.ProfileId);
                    //mobAirRewardsProg.ProgramCode = pref.ProgramCode;
                    //mobAirRewardsProg.ProgramDescription = pref.ProgramDescription;
                    mobAirRewardsProg.ProgramMemberId = pref.ProgramMemberId;
                    mobAirRewardsProg.VendorCode = pref.VendorCode;
                    mobAirRewardsProg.VendorDescription = pref.VendorDescription;
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
                    MOBPrefSpecialRequest mobSpecialRequest = new MOBPrefSpecialRequest();
                    mobSpecialRequest.AirPreferenceId = req.AirPreferenceId;
                    mobSpecialRequest.SpecialRequestId = req.SpecialRequestId;
                    mobSpecialRequest.SpecialRequestCode = req.SpecialRequestCode;
                    mobSpecialRequest.Key = req.Key;
                    mobSpecialRequest.LanguageCode = req.LanguageCode;
                    mobSpecialRequest.Description = req.Description;
                    mobSpecialRequest.Priority = req.Priority;
                    mobSpecialRequest.IsNew = req.IsNew;
                    mobSpecialRequest.IsSelected = req.IsSelected;
                    mobSpecialRequests.Add(mobSpecialRequest);
                }
            }
            return mobSpecialRequests;
        }
        private List<MOBPrefServiceAnimal> GetTravelerServiceAnimals(List<ServiceAnimal> serviceAnimals)
        {
            var results = new List<MOBPrefServiceAnimal>();

            if (serviceAnimals == null || !serviceAnimals.Any())
                return results;

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
                MOBEmail primaryEmailAddress = null;
                int co = 0;
                foreach (Services.Customer.Common.Email email in emailAddresses)
                {
                    if (email.EffectiveDate <= DateTime.UtcNow && email.DiscontinuedDate >= DateTime.UtcNow)
                    {
                        #region As per Wade Change want to filter out to return only Primary email to client if not primary phone exist return the first one from the list.So lot of work at client side will save time. Sep 15th 2014
                        co = co + 1;
                        MOBEmail e = new MOBEmail();
                        e.Key = email.Key;
                        e.Channel = new SHOPChannel();
                        e.Channel.ChannelCode = email.ChannelCode;
                        e.Channel.ChannelDescription = email.ChannelCodeDescription;
                        e.Channel.ChannelTypeCode = email.ChannelTypeCode.ToString();
                        e.Channel.ChannelTypeDescription = email.ChannelTypeDescription;
                        e.EmailAddress = email.EmailAddress;
                        e.IsDefault = email.IsDefault;
                        e.IsPrimary = email.IsPrimary;
                        e.IsPrivate = email.IsPrivate;
                        e.IsDayOfTravel = email.IsDayOfTravel;
                        mobEmailAddresses.Add(e);
                        #endregion
                    }
                }
            }
            return mobEmailAddresses;
            #endregion
        }
        public List<MOBEmail> PopulateEmailAddresses(List<Services.Customer.Common.Email> emailAddresses, bool onlyDayOfTravelContact)
        {
            #region
            List<MOBEmail> mobEmailAddresses = new List<MOBEmail>();
            bool isCorpEmailPresent = false;

            if (_configuration.GetValue<bool>("CorporateConcurBooking") && IsCorpBookingPath)
            {
                //As per Business / DotCom Kalpen; we are removing the condition for checking the Effectivedate and Discontinued date
                var corpIndex = emailAddresses.FindIndex(x => x.ChannelTypeDescription != null && x.ChannelTypeDescription.ToLower() == "corporate" && x.EmailAddress != null && x.EmailAddress.Trim() != "");
                if (corpIndex >= 0)
                    isCorpEmailPresent = true;

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
                        MOBEmail e = new MOBEmail();
                        e.Key = email.Key;
                        e.Channel = new SHOPChannel();
                        e.EmailAddress = email.EmailAddress;
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
        public List<MOBCPPhone> PopulatePhones(List<United.Services.Customer.Common.Phone> phones, bool onlyDayOfTravelContact)
        {
            List<MOBCPPhone> mobCPPhones = new List<MOBCPPhone>();
            bool isCorpPhonePresent = false;


            if (_configuration.GetValue<bool>("CorporateConcurBooking") && IsCorpBookingPath)
            {
                var corpIndex = phones.FindIndex(x => x.ChannelTypeDescription != null && x.ChannelTypeDescription.ToLower() == "corporate" && x.PhoneNumber != null && x.PhoneNumber != "");
                if (corpIndex >= 0)
                    isCorpPhonePresent = true;
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
                        MOBBKLoyaltyProgramProfile airRewardProgram = new MOBBKLoyaltyProgramProfile();
                        airRewardProgram.ProgramId = rewardProgram.ProgramID.ToString();
                        airRewardProgram.ProgramName = rewardProgram.Description;
                        airRewardProgram.MemberId = rewardProgram.ProgramMemberId;
                        airRewardProgram.CarrierCode = rewardProgram.VendorCode;
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
        public async Task<MOBCPMileagePlus> GetCurrentEliteLevelFromAirPreferences(List<AirPreference> airPreferences, string sessionid)
        {
            MOBCPMileagePlus mobCPMileagePlus = null;
            if (_configuration.GetValue<bool>("BugFixToggleFor17M") &&
                airPreferences != null &&
                airPreferences.Count > 0 &&
                airPreferences[0].AirRewardPrograms != null &&
                airPreferences[0].AirRewardPrograms.Count > 0)
            {
                mobCPMileagePlus = await GetCurrentEliteLevelFromAirRewardProgram(airPreferences, sessionid);
            }

            return mobCPMileagePlus;
        }
        private async Task<MOBCPMileagePlus> GetCurrentEliteLevelFromAirRewardProgram(List<AirPreference> airPreferences, string sessionid)
        {
            MOBCPMileagePlus mobCPMileagePlus = null;
            var airRewardProgram = airPreferences[0].AirRewardPrograms[0];
            if (!string.IsNullOrEmpty(airRewardProgram.ProgramMemberId))
            {
                Session session = new Session();
                session = await _sessionHelperService.GetSession<Session>(_headers.ContextValues.SessionId, session.ObjectName, new List<string>() { _headers.ContextValues.SessionId, session.ObjectName }).ConfigureAwait(false);

                MOBCPProfileRequest request = new MOBCPProfileRequest();
                request.CustomerId = 0;
                request.MileagePlusNumber = airRewardProgram.ProgramMemberId;
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
            var response = await _customerDataService.GetCustomerData<United.Services.Customer.Common.ProfileRequest>(token, sessionId, jsonRequest);
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

        private string GetPaxDescriptionByDOB(string date, string deptDateFLOF)
        {
            int age = TopHelper.GetAgeByDOB(date, deptDateFLOF);
            if ((18 <= age) && (age <= 64))
            {
                return "Adult (18-64)";
            }
            else
            if ((2 <= age) && (age < 5))
            {
                return "Child (2-4)";
            }
            else
            if ((5 <= age) && (age <= 11))
            {
                return "Child (5-11)";
            }
            else
            //if((12 <= age) && (age <= 17))
            //{

            //}
            if ((12 <= age) && (age <= 14))
            {
                return "Child (12-14)";
            }
            else
            if ((15 <= age) && (age <= 17))
            {
                return "Child (15-17)";
            }
            else
            if (65 <= age)
            {
                return "Senior (65+)";
            }
            else if (age < 2)
                return "Infant (under 2)";

            return string.Empty;
        }
        public string GetYAPaxDescByDOB()
        {
            return "Young adult (18-23)";
        }
        private bool EnableYADesc(bool isReshop = false)
        {
            return _configuration.GetValue<bool>("EnableYoungAdultBooking") && _configuration.GetValue<bool>("EnableYADesc") && !isReshop;
        }
        public List<MOBCPSecureTraveler> PopulatorSecureTravelers(List<United.Services.Customer.Common.SecureTraveler> secureTravelers, ref bool isTSAFlag, bool correctDate, string sessionID, int appID, string deviceID)
        {
            List<MOBCPSecureTraveler> mobSecureTravelers = null;
            try
            {
                if (secureTravelers != null && secureTravelers.Count > 0)
                {
                    mobSecureTravelers = new List<MOBCPSecureTraveler>();
                    int secureTravelerCount = 0;
                    foreach (var secureTraveler in secureTravelers)
                    {
                        if (secureTraveler.DocumentType != null && secureTraveler.DocumentType.Trim().ToUpper() != "X" && secureTravelerCount < 3)
                        {
                            #region
                            MOBCPSecureTraveler mobSecureTraveler = new MOBCPSecureTraveler();
                            if (correctDate)
                            {
                                DateTime tempBirthDate = secureTraveler.BirthDate.GetValueOrDefault().AddHours(1);
                                mobSecureTraveler.BirthDate = tempBirthDate.ToString("MM/dd/yyyy", CultureInfo.CurrentCulture);
                            }
                            else
                            {
                                mobSecureTraveler.BirthDate = secureTraveler.BirthDate.GetValueOrDefault().ToString("MM/dd/yyyy", CultureInfo.CurrentCulture);
                            }
                            mobSecureTraveler.CustomerId = secureTraveler.CustomerId;
                            mobSecureTraveler.DecumentType = secureTraveler.DocumentType;
                            mobSecureTraveler.Description = secureTraveler.Description;
                            mobSecureTraveler.FirstName = secureTraveler.FirstName;
                            mobSecureTraveler.Gender = secureTraveler.Gender;
                            mobSecureTraveler.Key = secureTraveler.Key;
                            mobSecureTraveler.LastName = secureTraveler.LastName;
                            mobSecureTraveler.MiddleName = secureTraveler.MiddleName;
                            mobSecureTraveler.SequenceNumber = secureTraveler.SequenceNumber;
                            mobSecureTraveler.Suffix = secureTraveler.Suffix;
                            if (secureTraveler.SupplementaryTravelInfos != null)
                            {
                                foreach (Services.Customer.Common.SupplementaryTravelInfo supplementaryTraveler in secureTraveler.SupplementaryTravelInfos)
                                {
                                    if (supplementaryTraveler.Type == Services.Customer.Common.Constants.SupplementaryTravelInfoNumberType.KnownTraveler)
                                    {
                                        mobSecureTraveler.KnownTravelerNumber = supplementaryTraveler.Number;
                                    }
                                    if (supplementaryTraveler.Type == Services.Customer.Common.Constants.SupplementaryTravelInfoNumberType.Redress)
                                    {
                                        mobSecureTraveler.RedressNumber = supplementaryTraveler.Number;
                                    }
                                }
                            }
                            if (!isTSAFlag && secureTraveler.DocumentType.Trim().ToUpper() == "U")
                            {
                                isTSAFlag = true;
                            }
                            if (secureTraveler.DocumentType.Trim().ToUpper() == "C" || secureTraveler.DocumentType.Trim() == "") // This is to get only Customer Cleared Secure Traveler records
                            {
                                mobSecureTravelers = new List<MOBCPSecureTraveler>();
                                mobSecureTravelers.Add(mobSecureTraveler);
                                secureTravelerCount = 4;
                            }
                            else
                            {
                                mobSecureTravelers.Add(mobSecureTraveler);
                                secureTravelerCount = secureTravelerCount + 1;
                            }
                            #endregion
                        }
                        else if (secureTravelerCount > 3)
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("PopulatorSecureTravelers {@Exception} for {@SecureTravelers}", JsonConvert.SerializeObject(ex), JsonConvert.SerializeObject(secureTravelers));
            }

            return mobSecureTravelers;
        }
        private MOBCPMileagePlus PopulateMileagePlus(United.Services.Customer.Common.MileagePlus onePass)
        {
            MOBCPMileagePlus mileagePlus = null;
            if (onePass != null)
            {
                mileagePlus = new MOBCPMileagePlus();
                mileagePlus.AccountBalance = onePass.AccountBalance;
                mileagePlus.ActiveStatusCode = onePass.ActiveStatusCode;
                mileagePlus.ActiveStatusDescription = onePass.ActiveStatusDescription;
                mileagePlus.AllianceEliteLevel = onePass.AllianceEliteLevel;
                mileagePlus.ClosedStatusCode = onePass.ClosedStatusCode;
                mileagePlus.ClosedStatusDescription = onePass.ClosedStatusDescription;
                mileagePlus.CurrentEliteLevel = onePass.CurrentEliteLevel;
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
        private MOBCPCustomerMetrics PopulateCustomerMetrics(United.Services.Customer.Common.CustomerMetrics customerMetrics)
        {
            MOBCPCustomerMetrics travelerCustomerMetrics = new MOBCPCustomerMetrics();
            if (customerMetrics != null && customerMetrics.PTCCode != null)
            {
                travelerCustomerMetrics.PTCCode = customerMetrics.PTCCode;
            }
            return travelerCustomerMetrics;
        }
        private async Task<Reservation> PersistedReservation(MOBCPProfileRequest request)
        {
            Reservation persistedReservation =
                new Reservation();
            if (request != null)
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
            return persistedReservation;
        }

        #region updated member profile start
        public async Task<(bool updateTravelerSuccess, List<MOBItem> insertUpdateItemKeys)> UpdateTraveler(MOBUpdateTravelerRequest request, List<MOBItem> insertUpdateItemKeys)
        {
            if (_configuration.GetValue<bool>("EnableUCBPhase1_MobilePhase2Changes"))
            {
                return await UpdateTravelerV2(request, insertUpdateItemKeys);
            }
            else
            {
                bool updateTravelerSuccess = false;
                if (string.IsNullOrEmpty(request.MileagePlusNumber))
                {
                    throw new MOBUnitedException("Profile Owner MileagePlus number is required.");
                }
                // Validate Phone even if request.UpdatePhoneInfo = false too as phone and email will be passed from payment page to update the reservation 
                if (request.Traveler.Phones != null && request.Traveler.Phones.Count > 0 && !String.IsNullOrEmpty(request.Traveler.Phones[0].PhoneNumber) && !String.IsNullOrEmpty(request.Traveler.Phones[0].CountryCode))
                {
                    string invalidPhoneNumberMessage = string.Empty;
                    var tupleRes = await ValidatePhoneWithCountryCode(request.Traveler.Phones[0], request.Application, request.SessionId, request.DeviceId, request.Token, invalidPhoneNumberMessage);
                    invalidPhoneNumberMessage = tupleRes.message;
                    if (!tupleRes.returnValue)
                    {
                        throw new MOBUnitedException(invalidPhoneNumberMessage);
                    }
                }
                if (request.UpdateAddressInfoAssociatedWithCC == true && request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0 && request.Traveler.Addresses[0].Country.Code.Trim() == "US")
                {
                    string stateCode = string.Empty;
                    var tupleValue = await GetAndValidateStateCode(request, stateCode);
                    stateCode = tupleValue.stateCode;
                    if (tupleValue.returnValue)
                    {
                        request.Traveler.Addresses[0].State.Code = stateCode;
                    }
                }
                if (request.UpdateTravelerBasiInfo == true && !String.IsNullOrEmpty(request.Traveler.Key))
                {
                    updateTravelerSuccess = await UpdateTravelerBase(request);

                    if (!request.UpdateCreditCardInfo && !request.UpdateAddressInfoAssociatedWithCC)
                    {
                        await UpdateViewName(request);
                    }
                }
                if (request.UpdatePhoneInfo == true && request.Traveler.Phones != null && request.Traveler.Phones.Count > 0 && !String.IsNullOrEmpty(request.Traveler.Phones[0].Key) && !String.IsNullOrEmpty(request.Traveler.Phones[0].PhoneNumber) && !String.IsNullOrEmpty(request.Traveler.Phones[0].CountryCode))
                {
                    if (await  isPhoneNumberChanged(request))
                    {
                        updateTravelerSuccess = await UpdateTravelerPhone(request);
                    }
                }
                else if (request.UpdatePhoneInfo == true && request.Traveler.Phones != null && request.Traveler.Phones.Count > 0 && String.IsNullOrEmpty(request.Traveler.Phones[0].Key) && !String.IsNullOrEmpty(request.Traveler.Phones[0].PhoneNumber) && !String.IsNullOrEmpty(request.Traveler.Phones[0].CountryCode))
                {
                    updateTravelerSuccess = await InsertTravelerPhone(request);
                }
                bool isEmailUpdated = default, isRewardProgramUpdated=default;
                var tupleResponse = await isRewardProgramChanged(request, isEmailUpdated, isRewardProgramUpdated);
                isEmailUpdated = tupleResponse.emailChanged;
                isRewardProgramUpdated = tupleResponse.rewardProgramUpdated;

                if (request.UpdateEmailInfo == true && request.Traveler.EmailAddresses != null && request.Traveler.EmailAddresses.Count > 0 && !String.IsNullOrEmpty(request.Traveler.EmailAddresses[0].Key) && !String.IsNullOrEmpty(request.Traveler.EmailAddresses[0].EmailAddress))
                {
                    if (isEmailUpdated)
                        updateTravelerSuccess = await UpdateTravelerEmail(request);
                }
                else if (request.UpdateEmailInfo == true && request.Traveler.EmailAddresses != null && request.Traveler.EmailAddresses.Count > 0 && String.IsNullOrEmpty(request.Traveler.EmailAddresses[0].Key) && !String.IsNullOrEmpty(request.Traveler.EmailAddresses[0].EmailAddress))
                {
                    updateTravelerSuccess = await InsertTravelerEmail(request);
                }
                if (request.UpdateRewardProgramInfo == true && request.Traveler.AirRewardPrograms != null && request.Traveler.AirRewardPrograms.Count > 0 && !String.IsNullOrEmpty(request.Traveler.AirRewardPrograms[0].RewardProgramKey))
                {
                    if (!request.Traveler.IsProfileOwner || (request.Traveler.IsProfileOwner && request.Traveler.AirRewardPrograms[0].CarrierCode.Trim().ToUpper() != "UA")) // to check not to update profile owner UA MP details. It will fail if trying to update the MP Account of the profile owner
                    {
                        if (isRewardProgramUpdated)
                        {
                            updateTravelerSuccess = await UpdateTravelerRewardProgram(request);
                        }
                    }
                }
                else if (request.UpdateRewardProgramInfo == true && request.Traveler.AirRewardPrograms != null && request.Traveler.AirRewardPrograms.Count > 0 && String.IsNullOrEmpty(request.Traveler.AirRewardPrograms[0].RewardProgramKey))
                {
                    updateTravelerSuccess = await InsertTravelerRewardProgram(request);
                }
                if (request.UpdateAddressInfoAssociatedWithCC || request.UpdateCreditCardInfo)
                {
                    insertUpdateItemKeys = new List<MOBItem>();
                    string addressKey = string.Empty, ccKey = string.Empty;
                    if (Convert.ToBoolean(_configuration.GetValue<string>("CorporateConcurBooking") ?? "false"))
                    {
                        #region
                        if (request.UpdateAddressInfoAssociatedWithCC == true && request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0 && !String.IsNullOrEmpty(request.Traveler.Addresses[0].Key) && !request.Traveler.Addresses[0].IsCorporate)
                        {
                            var tupleRes =await UpdateTravelerAddress(request, addressKey); // Because the Address Key is going to be a different udpated address key after Update ( as per Babu or seth the keys for addresss or credit card are going to be generated with the address values plus upate date time so the key going to be different after the upate)
                            updateTravelerSuccess = tupleRes.returnValue;
                            addressKey = tupleRes.updatedAddressKey;
                            MOBItem item = new MOBItem();
                            item.Id = "AddressKey";
                            item.CurrentValue = addressKey;
                            insertUpdateItemKeys.Add(item);
                        }
                        else if (request.UpdateAddressInfoAssociatedWithCC == true && request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0 && String.IsNullOrEmpty(request.Traveler.Addresses[0].Key) && !request.Traveler.Addresses[0].IsCorporate)
                        {
                            var tupleRes = await InsertTravelerAddress(request, addressKey);
                            updateTravelerSuccess = tupleRes.returnValue;
                            addressKey = tupleRes.insertedAddressKey;
                            MOBItem item = new MOBItem();
                            item.Id = "AddressKey";
                            item.CurrentValue = addressKey;
                            insertUpdateItemKeys.Add(item);
                        }
                        else if (request.UpdateAddressInfoAssociatedWithCC == true && request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0 && !String.IsNullOrEmpty(request.Traveler.Addresses[0].Key) && request.Traveler.Addresses[0].IsCorporate)
                        {
                            MOBItem item = new MOBItem();
                            item.Id = "AddressKey";
                            item.CurrentValue = request.Traveler.Addresses[0].Key;
                            insertUpdateItemKeys.Add(item);
                        }
                        if (_configuration.GetValue<string>("NotAllowUpdateAddressKeyForCorporateCC") != null && Convert.ToBoolean(_configuration.GetValue<string>("NotAllowUpdateAddressKeyForCorporateCC")))
                        {
                            #region
                            if (request.UpdateCreditCardInfo == true && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && !String.IsNullOrEmpty(request.Traveler.CreditCards[0].Key) && !request.Traveler.CreditCards[0].IsCorporate)
                            {
                                request.Traveler.CreditCards[0].AddressKey = addressKey;
                                var tupleRes = await UpdateTravelerCreditCard(request, ccKey); // Same here the CC key going to change after the update.
                                updateTravelerSuccess = tupleRes.returnValue;
                                ccKey = tupleRes.ccKey;
                                MOBItem item = new MOBItem();
                                item.Id = "CreditCardKey";
                                item.CurrentValue = ccKey;
                                insertUpdateItemKeys.Add(item);
                            }
                            else if (request.UpdateCreditCardInfo == true && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && String.IsNullOrEmpty(request.Traveler.CreditCards[0].Key) && !request.Traveler.CreditCards[0].IsCorporate)
                            {
                                request.Traveler.CreditCards[0].AddressKey = addressKey;
                                var tupleRes = await InsertTravelerCreditCard(request, ccKey);
                                updateTravelerSuccess = tupleRes.returnValue;
                                ccKey = tupleRes.ccKey;
                                MOBItem item = new MOBItem();
                                item.Id = "CreditCardKey";
                                item.CurrentValue = ccKey;
                                insertUpdateItemKeys.Add(item);
                            }
                            else if (request.UpdateCreditCardInfo == true && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && !String.IsNullOrEmpty(request.Traveler.CreditCards[0].Key) && request.Traveler.CreditCards[0].IsCorporate)
                            {
                                MOBItem item = new MOBItem();
                                item.Id = "CreditCardKey";
                                item.CurrentValue = request.Traveler.CreditCards[0].Key;
                                insertUpdateItemKeys.Add(item);
                            }
                            #endregion
                        }
                        else
                        {
                            #region
                            if (request.UpdateCreditCardInfo == true && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && !String.IsNullOrEmpty(request.Traveler.CreditCards[0].Key) && !(request.Traveler.CreditCards[0].IsCorporate && request.Traveler.Addresses[0].IsCorporate))
                            {
                                request.Traveler.CreditCards[0].AddressKey = addressKey;
                                var tupleRes = await UpdateTravelerCreditCard(request, ccKey); // Same here the CC key going to change after the update.
                                updateTravelerSuccess = tupleRes.returnValue;
                                ccKey = tupleRes.ccKey; 
                                MOBItem item = new MOBItem();
                                item.Id = "CreditCardKey";
                                item.CurrentValue = ccKey;
                                insertUpdateItemKeys.Add(item);
                            }
                            else if (request.UpdateCreditCardInfo == true && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && String.IsNullOrEmpty(request.Traveler.CreditCards[0].Key) && !request.Traveler.CreditCards[0].IsCorporate)
                            {
                                request.Traveler.CreditCards[0].AddressKey = addressKey;
                                var tupleRes = await InsertTravelerCreditCard(request, ccKey);
                                updateTravelerSuccess = tupleRes.returnValue;
                                ccKey = tupleRes.ccKey;
                                MOBItem item = new MOBItem();
                                item.Id = "CreditCardKey";
                                item.CurrentValue = ccKey;
                                insertUpdateItemKeys.Add(item);
                            }
                            else if (request.UpdateCreditCardInfo == true && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && !String.IsNullOrEmpty(request.Traveler.CreditCards[0].Key) && request.Traveler.CreditCards[0].IsCorporate)
                            {
                                MOBItem item = new MOBItem();
                                item.Id = "CreditCardKey";
                                item.CurrentValue = request.Traveler.CreditCards[0].Key;
                                insertUpdateItemKeys.Add(item);
                            }

                            #endregion
                        }
                        #endregion
                    }
                    else
                    {
                        #region
                        if (request.UpdateAddressInfoAssociatedWithCC == true && request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0 && !String.IsNullOrEmpty(request.Traveler.Addresses[0].Key))
                        {
                            var tupleRes = await UpdateTravelerAddress(request, addressKey); // Because the Address Key is going to be a different udpated address key after Update ( as per Babu or seth the keys for addresss or credit card are going to be generated with the address values plus upate date time so the key going to be different after the upate)
                            updateTravelerSuccess = tupleRes.returnValue;
                            addressKey = tupleRes.updatedAddressKey;
                            MOBItem item = new MOBItem();
                            item.Id = "AddressKey";
                            item.CurrentValue = addressKey;
                            insertUpdateItemKeys.Add(item);
                        }
                        else if (request.UpdateAddressInfoAssociatedWithCC == true && request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0 && String.IsNullOrEmpty(request.Traveler.Addresses[0].Key))
                        {
                            var tupleRes =  await InsertTravelerAddress(request, addressKey);
                            updateTravelerSuccess = tupleRes.returnValue;
                            addressKey = tupleRes.insertedAddressKey;
                            MOBItem item = new MOBItem();
                            item.Id = "AddressKey";
                            item.CurrentValue = addressKey;
                            insertUpdateItemKeys.Add(item);
                        }
                        if (request.UpdateCreditCardInfo == true && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && !String.IsNullOrEmpty(request.Traveler.CreditCards[0].Key))
                        {
                            request.Traveler.CreditCards[0].AddressKey = addressKey;
                            var tupleRes = await UpdateTravelerCreditCard(request, ccKey); // Same here the CC key going to change after the update.
                            updateTravelerSuccess = tupleRes.returnValue;
                            ccKey = tupleRes.ccKey;
                            MOBItem item = new MOBItem();
                            item.Id = "CreditCardKey";
                            item.CurrentValue = ccKey;
                            insertUpdateItemKeys.Add(item);
                        }
                        else if (request.UpdateCreditCardInfo == true && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && String.IsNullOrEmpty(request.Traveler.CreditCards[0].Key))
                        {
                            request.Traveler.CreditCards[0].AddressKey = addressKey;
                            var tupleRes =  await InsertTravelerCreditCard(request, ccKey);
                            updateTravelerSuccess = tupleRes.returnValue;
                            ccKey = tupleRes.ccKey;
                            MOBItem item = new MOBItem();
                            item.Id = "CreditCardKey";
                            item.CurrentValue = ccKey;
                            insertUpdateItemKeys.Add(item);
                        }
                        #endregion
                    }
                }

                if (EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
                {
                    if (request.UpdateSpecialRequests)
                    {
                        updateTravelerSuccess = UpdateSpecialRequests(request);
                    }

                    if (request.UpdateServiceAnimals)
                    {
                        updateTravelerSuccess = UpdateServiceAnimals(request);
                    }

                    if (request.UpdateMealPreference)
                    {
                        updateTravelerSuccess = UpdateMealPreference(request);
                    }
                }

                return (updateTravelerSuccess, insertUpdateItemKeys);
            }
        }
        private async Task<(bool returnValue, string stateCode)> GetAndValidateStateCode(MOBUpdateTravelerRequest request,  string stateCode)
        {
            bool validStateCode = false;
            #region
            string urlPath = string.Format("/StatesFilter?State={0}&CountryCode={1}&Language={2}", request.Traveler.Addresses[0].State.Code, request.Traveler.Addresses[0].Country.Code, request.LanguageCode);
            
            _logger.LogInformation("GetAndValidateStateCode - {url} and {sessionID}", urlPath, _headers.ContextValues.SessionId);
            string jsonResponse = await _referencedataService.GetAndValidateStateCode(request.Token, urlPath, _headers.ContextValues.SessionId);
            if (!string.IsNullOrEmpty(jsonResponse))
            {
                List<United.Service.Presentation.CommonModel.StateProvince> response = JsonSerializer.Deserialize<List<United.Service.Presentation.CommonModel.StateProvince>>(jsonResponse);

                if (response != null && response.Count == 1 && !string.IsNullOrEmpty(response[0].StateProvinceCode))
                {
                    stateCode = response[0].StateProvinceCode;
                    validStateCode = true;
                    _logger.LogInformation("GetAndValidateStateCode - {url} {response} and {sessionID}", urlPath, JsonConvert.SerializeObject(response), _headers.ContextValues.SessionId);
                }
                else
                {
                    string exceptionMessage = _configuration.GetValue<string>("UnableToGetAndValidateStateCode");
                    throw new MOBUnitedException(exceptionMessage);
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToGetAndValidateStateCode");
                if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting")))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  GetCommonUsedDataList()";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion
            return (validStateCode,stateCode);
        }
        public async Task<bool> UpdateTravelerBase(MOBUpdateTravelerRequest request)
        {
            if (_configuration.GetValue<bool>("CustomerDataTravelerAwsUCB"))
            {
                return UpdateTravelerBaseV2(request);
            }
            bool updateTravelerSuccess = false;
            #region
            UpdateTravelerBaseRequest updateTraveler = GetUpdateTravelerRequest(request);
            string jsonRequest = JsonSerializer.Serialize<UpdateTravelerBaseRequest>(updateTraveler);
            string urlPath = "/UpdateTravelerBase";          
            var response = await _customerDataService.GetProfile<SaveResponse>(request.Token, jsonRequest, _headers.ContextValues.SessionId, urlPath);
            if (response != null)
            {
                if (response != null && response.Status.Equals(United.Services.Customer.Common.Constants.StatusType.Success) && response.ReturnValues != null)
                {
                    updateTravelerSuccess = true;
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                        }
                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerBaseProfileErrorMessage");
                        if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && _configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL UpdateTravelerBase(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerBaseProfileErrorMessage");
                if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && _configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  UpdateTravelerBase(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion
            return updateTravelerSuccess;
        }
        public async System.Threading.Tasks.Task UpdateViewName(MOBUpdateTravelerRequest request)
        {
            var bookingPathReservation = await _sessionHelperService.GetSession<United.Mobile.Model.Shopping.Reservation>(request.SessionId, new Reservation().ObjectName, new List<string> { request.SessionId, new Reservation().ObjectName }).ConfigureAwait(false);
            if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null && !string.IsNullOrEmpty(bookingPathReservation.ShopReservationInfo2.NextViewName) &&
                !bookingPathReservation.ShopReservationInfo2.NextViewName.ToUpper().Equals("TRAVELOPTION"))
            {
                if (bookingPathReservation.TravelersCSL != null && bookingPathReservation.TravelersCSL.Count > 0)
                {
                    foreach (var t in bookingPathReservation.TravelersCSL)
                    {
                        if (t.Value.Key.Equals(request.Traveler.Key))
                        {
                            if (_configuration.GetValue<bool>("EnableTravelerNationalityChangeFix"))
                            {
                                if (!t.Value.FirstName.ToUpper().Equals(request.Traveler.FirstName.ToUpper()) || !t.Value.LastName.ToUpper().Equals(request.Traveler.LastName.ToUpper()) || !t.Value.BirthDate.Equals(request.Traveler.BirthDate)
                                || !string.Equals(t.Value.Nationality, request.Traveler.Nationality, StringComparison.OrdinalIgnoreCase))
                                {
                                    bookingPathReservation.ShopReservationInfo2.NextViewName = "TravelOption";
                                    await _sessionHelperService.SaveSession(bookingPathReservation, request.SessionId, new List<string> { request.SessionId, new Reservation().ObjectName }).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                if (!t.Value.FirstName.ToUpper().Equals(request.Traveler.FirstName.ToUpper()) || !t.Value.LastName.ToUpper().Equals(request.Traveler.LastName.ToUpper()) || !t.Value.BirthDate.Equals(request.Traveler.BirthDate))
                                {
                                    bookingPathReservation.ShopReservationInfo2.NextViewName = "TravelOption";
                                    await _sessionHelperService.SaveSession(bookingPathReservation, request.SessionId, new List<string> { request.SessionId, new Reservation().ObjectName }).ConfigureAwait(false);
                                }

                            }
                        }
                    }
                }
            }
        }
        private United.Services.Customer.Common.UpdateTravelerBaseRequest GetUpdateTravelerRequest(MOBUpdateTravelerRequest request)
        {
            #region Build insert Traverl Request
            United.Services.Customer.Common.UpdateTravelerBaseRequest travelUpdateRequest = new Services.Customer.Common.UpdateTravelerBaseRequest();
            if (request.UpdateTravelerBasiInfo)
            {
                #region
                travelUpdateRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices"); ;
                travelUpdateRequest.Title = request.Traveler.Title;
                travelUpdateRequest.FirstName = request.Traveler.FirstName;
                travelUpdateRequest.MiddleName = request.Traveler.MiddleName;
                travelUpdateRequest.LastName = request.Traveler.LastName;
                travelUpdateRequest.Suffix = request.Traveler.Suffix;
                travelUpdateRequest.DateOfBirth = new DateTime();
                int offSetHours = _configuration.GetValue<string>("UpdateTravelerDOBOffSetHours") == null ? 12 : _configuration.GetValue<int>("UpdateTravelerDOBOffSetHours");
                travelUpdateRequest.DateOfBirth = DateTime.ParseExact(request.Traveler.BirthDate, "M/d/yyyy", CultureInfo.InvariantCulture).AddHours(offSetHours);
                travelUpdateRequest.Gender = request.Traveler.GenderCode;
                travelUpdateRequest.KnownTravelerNumber = request.Traveler.KnownTravelerNumber;
                travelUpdateRequest.RedressNumber = request.Traveler.RedressNumber;
                travelUpdateRequest.UpdateId = request.Traveler.CustomerId.ToString();
                travelUpdateRequest.CustomerId = request.Traveler.CustomerId;
                travelUpdateRequest.ProfileId = request.Traveler.ProfileId;
                travelUpdateRequest.TravelerKey = request.Traveler.Key;
                //Need to be updated once Nuget get updated to 7.1
                if (IsEnabledNationalityAndResidence(false, request.Application.Id, request.Application.Version.Major))
                {
                    if (request.Traveler != null && !string.IsNullOrEmpty(request.Traveler.Nationality) && request.Traveler.Nationality.ToUpper().Equals("OTHER"))
                    {
                        travelUpdateRequest.Nationality = null;
                    }
                    else
                    {
                        travelUpdateRequest.Nationality = request.Traveler.Nationality;
                    }

                    if (request.Traveler != null && !string.IsNullOrEmpty(request.Traveler.CountryOfResidence) && request.Traveler.CountryOfResidence.ToUpper().Equals("OTHER"))
                    {
                        travelUpdateRequest.CountryOfResidence = null;
                    }
                    else
                    {
                        travelUpdateRequest.CountryOfResidence = request.Traveler.CountryOfResidence;
                    }
                }
                #endregion
            }

            #endregion
            return travelUpdateRequest;
        }
        private async Task<(bool returnValue, string message)> ValidatePhoneWithCountryCode(MOBCPPhone phone, MOBApplication application, string sessionID, string deviceID, string token, string message)
        {
            #region
            bool isValidPhone = false;
            United.Service.Presentation.CustomerRequestModel.PhoneNumberValidation phoneValidateRequest = new Service.Presentation.CustomerRequestModel.PhoneNumberValidation();
            phoneValidateRequest.CountryCode = phone.CountryCode;
            phoneValidateRequest.Number = new Service.Presentation.CommonModel.Telephone();
            phoneValidateRequest.Number.PhoneNumber = (phone.AreaNumber + "-" + phone.PhoneNumber).Trim().Trim('-');
            string jsonRequest = JsonConvert.SerializeObject(phoneValidateRequest);

            bool phoneValidationServiceMigrationEnabled = string.IsNullOrEmpty(_configuration.GetValue<string>("EnableAWSPhoneValidationService"))
                ? false
                : Convert.ToBoolean(_configuration.GetValue<string>("EnableAWSPhoneValidationService"));

            string path = phoneValidationServiceMigrationEnabled
                ? string.Format("/phonevalidation/ValidateAndFormat") : string.Format("/phone/ValidateAndFormat");
            var phoneValidationResponse = new United.Service.Presentation.CustomerResponseModel.PhoneNumberValidation();
            if (phoneValidationServiceMigrationEnabled)
            {
                phoneValidationResponse = await _eServiceCheckin.GetPhoneValidation<United.Service.Presentation.CustomerResponseModel.PhoneNumberValidation>(token, path, jsonRequest, sessionID);
            }
            else
            {
                phoneValidationResponse = await _utilitiesService.ValidatePhoneWithCountryCode<United.Service.Presentation.CustomerResponseModel.PhoneNumberValidation>(token, path, jsonRequest, sessionID);
            }
            _logger.LogInformation("ValidatePhoneWithCountryCode - {url} {request} and {sessionID}", path, JsonConvert.SerializeObject(jsonRequest), sessionID);

            if (phoneValidationResponse != null)
            {
                _logger.LogInformation("ValidatePhoneWithCountryCode - {url} {response} and {sessionID}", path, JsonConvert.SerializeObject(phoneValidationResponse), _headers.ContextValues.SessionId);

                isValidPhone = true;
                if (phoneValidationResponse.Error != null)
                {
                    isValidPhone = false;
                    if (_configuration.GetValue<string>("GenericInValidPhoneNumberMessage") == null)
                    {
                        foreach (Service.Presentation.CommonModel.ExceptionModel.Error error in phoneValidationResponse.Error)
                        {
                            message = message + " " + error.Text;
                        }
                    }
                    else
                    {
                        message = _configuration.GetValue<string>("GenericInValidPhoneNumberMessage");
                    }
                    message = message.Trim();
                }
            }
            return (isValidPhone,message);
            #endregion
        }
        private bool UpdateMealPreference(MOBUpdateTravelerRequest request)
        {
            var success = true;
            // Implement update UpdateMealPreference here
            var updateSpecialRequestsRequest = new UpdateMealPreferenceRequest();

            return success;
        }
        private bool UpdateServiceAnimals(MOBUpdateTravelerRequest request)
        {
            var success = true;
            // Implement update UpdateServiceAnimals here
            var updateSpecialRequestsRequest = new UpdateServiceAnimalRequest();

            return success;
        }
        private bool UpdateSpecialRequests(MOBUpdateTravelerRequest request)
        {
            var success = true;
            // Implement update special requests here
            var updateSpecialRequestsRequest = new UpdateSpecialReqsRequest();

            return success;
        }
        private bool EnableSpecialNeeds(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableSpecialNeeds")
                    && GeneralHelper.isApplicationVersionGreater(appId, appVersion, "AndroidEnableSpecialNeedsVersion", "iPhoneEnableSpecialNeedsVersion", "", "", true, _configuration);
        }
        private async Task<bool> isPhoneNumberChanged(MOBUpdateTravelerRequest request)
        {
            bool isChanged = true;
            try
            {
                if (_configuration.GetValue<bool>("BugFixToggleFor17M") && !string.IsNullOrEmpty(request.SessionId))
                {
                    Reservation persistedReservation =
                    new Reservation();
                    if (request != null)
                        persistedReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, new Reservation().ObjectName, new List<string> { request.SessionId, new Reservation().ObjectName }).ConfigureAwait(false);
                    if (persistedReservation != null &&
                        persistedReservation.ReservationPhone != null &&
                        request.Traveler != null &&
                        request.Traveler.Phones != null &&
                        request.Traveler.Phones.Count > 0 &&
                        request.Traveler.Phones[0].AreaNumber == persistedReservation.ReservationPhone.AreaNumber &&
                        request.Traveler.Phones[0].PhoneNumber == persistedReservation.ReservationPhone.PhoneNumber)
                    {
                        isChanged = false;
                    }
                }
            }
            catch { }
            return isChanged;
        }
        private async Task<(bool returnValue, string insertedAddressKey)> InsertTravelerAddress(MOBUpdateTravelerRequest request,  string insertedAddressKey)
        {
            bool insertTravelerAddress = false;
            #region
            United.Services.Customer.Common.InsertAddressRequest insertAddress = GetInsertAddressRequest(request);
            string jsonRequest = JsonSerializer.Serialize<United.Services.Customer.Common.InsertAddressRequest>(insertAddress);

            string urlPath = "/InsertAddress";
            var response = await _customerDataService.GetProfile<SaveResponse>(request.Token, jsonRequest, _headers.ContextValues.SessionId, urlPath);

            if (response != null)
            {
                if (response != null && response.Status.Equals(Services.Customer.Common.Constants.StatusType.Success) && response.ReturnValues != null)
                {
                    var obj = (from st in response.ReturnValues
                               where st.Key.ToUpper().Trim() == "AddressKey".ToUpper().Trim()
                               select st).ToList();
                    insertedAddressKey = obj[0].Value;
                    insertTravelerAddress = true;
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                        }

                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToInsertAddressToProfileErrorMessage");
                        if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && _configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL InsertTravelerAddress(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToInsertAddressToProfileErrorMessage");
                if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && _configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  InsertTravelerAddress(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion
            return (insertTravelerAddress, insertedAddressKey);
        }
        private United.Services.Customer.Common.InsertAddressRequest GetInsertAddressRequest(MOBUpdateTravelerRequest request)
        {
            #region Build insert Traverl Request
            United.Services.Customer.Common.InsertAddressRequest addressInsertRequest = new Services.Customer.Common.InsertAddressRequest();
            if (request.UpdateAddressInfoAssociatedWithCC && request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0)
            {
                addressInsertRequest.TravelerKey = request.Traveler.Key;
                addressInsertRequest.CustomerId = request.Traveler.CustomerId;
                addressInsertRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices");
                addressInsertRequest.AddressLine1 = request.Traveler.Addresses[0].Line1;
                addressInsertRequest.AddressLine2 = request.Traveler.Addresses[0].Line2;
                addressInsertRequest.AddressLine3 = request.Traveler.Addresses[0].Line3;
                addressInsertRequest.ChannelTypeCode = request.Traveler.Addresses[0].Channel != null ? request.Traveler.Addresses[0].Channel.ChannelTypeCode : "H";
                addressInsertRequest.City = request.Traveler.Addresses[0].City;
                addressInsertRequest.CountryCode = request.Traveler.Addresses[0].Country != null ? request.Traveler.Addresses[0].Country.Code : "";
                addressInsertRequest.PostalCode = request.Traveler.Addresses[0].PostalCode;
                addressInsertRequest.StateCode = request.Traveler.Addresses[0].State != null ? request.Traveler.Addresses[0].State.Code : "";
                addressInsertRequest.Description = ""; //--> Confirmed with Edwards Babu better to pass emtpy for description as if we pass for EX: Home two times even with different address then second time this service customerdata/api/InsertAddress 
                //will return error as "UserFriendlyMessage=The Address Description entered already exists in your Saved Addresses. Please revise the description." to avoid this error beter pass as empty.
                //request.Traveler.Addresses[0].Channel != null ? request.Traveler.Addresses[0].Channel.ChannelTypeDescription : "";  
                addressInsertRequest.InsertId = request.Traveler.CustomerId.ToString();
                addressInsertRequest.LangCode = request.LanguageCode;
            }
            #endregion
            return addressInsertRequest;
        }
        private async Task<(bool returnValue, string ccKey)> InsertTravelerCreditCard(MOBUpdateTravelerRequest request, string ccKey)
        {
            if (_configuration.GetValue<bool>("CustomerDataCreditCardAwsUCB"))
            {
                return await UpsertTravelerCreditCardV2(request);
            }
            bool insertTravelerCreditCard = false;

            #region
            United.Services.Customer.Common.InsertCreditCardRequest insertAddress = await GetInsertCreditCardRequest(request);
            string jsonRequest = JsonSerializer.Serialize<United.Services.Customer.Common.InsertCreditCardRequest>(insertAddress);
            string urlPath = "/InsertCreditCard";
           
            var response = await _customerDataService.GetProfile<SaveResponse>(request.Token, jsonRequest, _headers.ContextValues.SessionId, urlPath);

            if (response != null)
            {
                if (response != null && response.Status.Equals(United.Services.Customer.Common.Constants.StatusType.Success) && response.ReturnValues != null)
                {
                    var creditCardKey = (from t in response.ReturnValues
                                         where t.Key.ToUpper().Trim() == "CreditCardKey".ToUpper().Trim()
                                         select t).ToList();
                    insertTravelerCreditCard = true;
                    ccKey = creditCardKey[0].Value;

                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            //Alekhya - Null check Conditon for Message has been added - Aug 16,2016
                            if (!String.IsNullOrEmpty(error.Message) && error.Message.ToLower().Contains("ora-") && error.Message.ToLower().Contains("unique constraint") && error.Message.Contains("violated"))
                                errorMessage = errorMessage + " " + _configuration.GetValue<string>("CCAlreadyExistMessage");
                            errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                        }
                        errorMessage = string.IsNullOrWhiteSpace(errorMessage) ? _configuration.GetValue<string>("Booking2OGenericExceptionMessage") : errorMessage;
                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToInsertCreditCardToProfileErrorMessage");
                        if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && _configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL InsertTravelerCreditCard(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToInsertCreditCardToProfileErrorMessage");
                if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && _configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  InsertTravelerCreditCard(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion

            return (insertTravelerCreditCard,ccKey);
        }
        private async Task<(bool returnValue, string ccKey)> UpdateTravelerCreditCard(MOBUpdateTravelerRequest request, string ccKey)
        {

            if (_configuration.GetValue<bool>("CustomerDataCreditCardAwsUCB"))
            {
                return await UpsertTravelerCreditCardV2(request);
            }

            bool updateTravelerCreditCard = false;

            #region
            United.Services.Customer.Common.UpdateCreditCardRequest updateCreditCard = await GetUpdateCreditCardRequest(request);
            string jsonRequest = JsonSerializer.Serialize<United.Services.Customer.Common.UpdateCreditCardRequest>(updateCreditCard);

            string urlPath = string.Format("/UpdateCreditCard");
            var response = await _customerDataService.GetProfile<SaveResponse>(request.Token, jsonRequest, _headers.ContextValues.SessionId, urlPath);

            if (response != null)
            {
                if (response != null && response.Status.Equals(United.Services.Customer.Common.Constants.StatusType.Success) && response.ReturnValues != null)
                {
                    var creditCardKey = (from t in response.ReturnValues
                                         where t.Key.ToUpper().Trim() == "CreditCardKey".ToUpper().Trim()
                                         select t).ToList();
                    ccKey = creditCardKey[0].Value;
                    updateTravelerCreditCard = true;

                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                        }

                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateCreditCardToProfileErrorMessage");
                        if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && _configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL UpdateTravelerCreditCard(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateCreditCardToProfileErrorMessage");
                if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && _configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  UpdateTravelerCreditCard(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion

            return (updateTravelerCreditCard,ccKey);
        }
        private async Task<United.Services.Customer.Common.UpdateCreditCardRequest> GetUpdateCreditCardRequest(MOBUpdateTravelerRequest request)
        {
            #region Build insert Traverl Request
            United.Services.Customer.Common.UpdateCreditCardRequest creditCardUpdateRequest = new Services.Customer.Common.UpdateCreditCardRequest();
            string accountNumberToken = request.Traveler.CreditCards[0].AccountNumberToken;
            bool CCTokenDataVaultCheck = true;
            //if (!request.Traveler.CreditCards[0].UnencryptedCardNumber.Contains("XXXXXXXXXXXX"))
            if (!string.IsNullOrEmpty(request.Traveler.CreditCards[0].EncryptedCardNumber))
            {
                var tupleResponse = await _profileCreditCard.GenerateCCTokenWithDataVault(request.Traveler.CreditCards[0], request.SessionId, request.Token, request.Application, request.DeviceId, accountNumberToken);
                CCTokenDataVaultCheck = tupleResponse.Item1;
                accountNumberToken = tupleResponse.ccDataVaultToken;
            }
            if (request.UpdateCreditCardInfo && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && CCTokenDataVaultCheck)
            {
                creditCardUpdateRequest.CreditCardKey = request.Traveler.CreditCards[0].Key;
                //creditCardUpdateRequest.NewCreditCardNumber = request.Traveler.CreditCards[0].UnencryptedCardNumber;
                if (_configuration.GetValue<string>("17BHotFix_DoNotAllowEditExistingSavedCreditCardNumber") != null &&
                    Convert.ToBoolean(_configuration.GetValue<string>("17BHotFix_DoNotAllowEditExistingSavedCreditCardNumber")))
                {
                    creditCardUpdateRequest.AccountNumberToken = request.Traveler.CreditCards[0].AccountNumberToken;
                }
                else
                {
                    creditCardUpdateRequest.AccountNumberToken = accountNumberToken;
                }
                creditCardUpdateRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices");
                creditCardUpdateRequest.AddressKey = request.Traveler.CreditCards[0].AddressKey;
                creditCardUpdateRequest.CreditCardType = request.Traveler.CreditCards[0].CardType;
                creditCardUpdateRequest.ExpirationMonth = Convert.ToInt32(request.Traveler.CreditCards[0].ExpireMonth.Trim());
                creditCardUpdateRequest.ExpirationYear = Convert.ToInt32(request.Traveler.CreditCards[0].ExpireYear.Trim().Length == 2 ? "20" + request.Traveler.CreditCards[0].ExpireYear.Trim() : request.Traveler.CreditCards[0].ExpireYear.Trim());
                creditCardUpdateRequest.Name = request.Traveler.CreditCards[0].cCName;
                creditCardUpdateRequest.PhoneKey = request.Traveler.CreditCards[0].PhoneKey;
                creditCardUpdateRequest.Description = request.Traveler.CreditCards[0].Description;
                creditCardUpdateRequest.UpdateId = request.Traveler.CustomerId.ToString();
                creditCardUpdateRequest.LangCode = request.LanguageCode;
            }
            #endregion
            return creditCardUpdateRequest;
        }
        private async Task<bool> UpdateTravelerPhone(MOBUpdateTravelerRequest request)
        {
            bool updateTravelerPhone = false;
            #region
            United.Services.Customer.Common.UpdatePhoneRequest updatePhone = GetUpdatePhoneRequest(request);
            string jsonRequest = JsonSerializer.Serialize<United.Services.Customer.Common.UpdatePhoneRequest>(updatePhone);
            string urlPath = "/UpdatePhone ";
            var response = await _customerDataService.InsertMPEnrollment<SaveResponse>(request.Token, jsonRequest, request.SessionId, urlPath);

            if (response != null)
            {
                if (response != null && response.Status.Equals(United.Services.Customer.Common.Constants.StatusType.Success) && response.ReturnValues != null)
                {
                    ///104572 : New Phone and email address are not saved in the profile, when email address and phone are edited in Payment screen with save card to profile toggle ON
                    ///Srini : 12/18/2017
                    if (_configuration.GetValue<bool>("BugFixToggleFor17M") &&
                        response.ReturnValues != null &&
                        response.ReturnValues.Count > 0 &&
                        response.ReturnValues.Exists(p => p.Key == "PhoneKey"))
                    {
                        request.Traveler.Phones[0].Key = response.ReturnValues.First(p => p.Key == "PhoneKey").Value;
                    }
                    updateTravelerPhone = true;

                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                        }

                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerPhoneToProfileErrorMessage");
                        if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL UpdateTravelerPhone(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerPhoneToProfileErrorMessage");
                if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  UpdateTravelerPhone(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion
            return updateTravelerPhone;
        }
        private United.Services.Customer.Common.UpdatePhoneRequest GetUpdatePhoneRequest(MOBUpdateTravelerRequest request)
        {
            #region Build insert Traverl Request
            United.Services.Customer.Common.UpdatePhoneRequest phoneUpdateRequest = new Services.Customer.Common.UpdatePhoneRequest();
            if (request.UpdatePhoneInfo && request.Traveler.Phones != null && request.Traveler.Phones.Count > 0)
            {
                phoneUpdateRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices");
                phoneUpdateRequest.Number = (request.Traveler.Phones[0].AreaNumber + "-" + request.Traveler.Phones[0].PhoneNumber).Trim().Trim('-');
                phoneUpdateRequest.CountryCode = string.IsNullOrEmpty(request.Traveler.Phones[0].CountryCode) ? "US" : request.Traveler.Phones[0].CountryCode.Trim();
                //phoneUpdateRequest.CustomerId = request.Traveler.CustomerId;
                phoneUpdateRequest.UpdateId = request.Traveler.CustomerId.ToString();
                phoneUpdateRequest.PhoneKey = request.Traveler.Phones[0].Key;
                phoneUpdateRequest.DeviceType = string.IsNullOrEmpty(request.Traveler.Phones[0].DeviceTypeCode) ? "PH" : request.Traveler.Phones[0].DeviceTypeCode.Trim();
                phoneUpdateRequest.NewChannelTypeCode = string.IsNullOrEmpty(request.Traveler.Phones[0].ChannelTypeCode) ? "H" : request.Traveler.Phones[0].ChannelTypeCode.Trim();
            }
            #endregion
            return phoneUpdateRequest;
        }
        private async Task<(bool returnValue, string updatedAddressKey)> UpdateTravelerAddress(MOBUpdateTravelerRequest request, string updatedAddressKey)
        {
            bool updateTravelerAddress = false;
            #region
            United.Services.Customer.Common.UpdateAddressRequest updateAddress = GetUpdateAddressRequest(request);
            string jsonRequest = JsonSerializer.Serialize<United.Services.Customer.Common.UpdateAddressRequest>(updateAddress);

            string url = "/UpdateAddress";    
            var response = await _customerDataService.GetProfile<SaveResponse>(request.Token, jsonRequest, _headers.ContextValues.SessionId, url);
            if (response != null)
            {
                if (response != null && response.Status.Equals(United.Services.Customer.Common.Constants.StatusType.Success) && response.ReturnValues != null)
                {
                    var obj = (from st in response.ReturnValues
                               where st.Key.ToUpper().Trim() == "AddressKey".ToUpper().Trim()
                               select st).ToList();
                    updatedAddressKey = obj[0].Value;
                    updateTravelerAddress = true;
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                        }

                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateAddressToProfileErrorMessage");
                        if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL UpdateTravelerAddress(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateAddressToProfileErrorMessage");
                if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  UpdateTravelerAddress(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion
            return (updateTravelerAddress,updatedAddressKey);
        }
        private United.Services.Customer.Common.UpdateAddressRequest GetUpdateAddressRequest(MOBUpdateTravelerRequest request)
        {
            #region Build Update Address Request
            United.Services.Customer.Common.UpdateAddressRequest addressUpdateRequest = new Services.Customer.Common.UpdateAddressRequest();
            if (request.UpdateAddressInfoAssociatedWithCC && request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0)
            {
                addressUpdateRequest.AddressKey = request.Traveler.Addresses[0].Key;
                //addressUpdateRequest.CustomerId = request.Traveler.CustomerId;
                addressUpdateRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices");
                addressUpdateRequest.AddressLine1 = request.Traveler.Addresses[0].Line1;
                addressUpdateRequest.AddressLine2 = request.Traveler.Addresses[0].Line2;
                addressUpdateRequest.AddressLine3 = request.Traveler.Addresses[0].Line3;
                //addressUpdateRequest.OldChannelTypeCode = "";
                addressUpdateRequest.NewChannelTypeCode = request.Traveler.Addresses[0].Channel != null ? request.Traveler.Addresses[0].Channel.ChannelTypeCode : "";
                addressUpdateRequest.City = request.Traveler.Addresses[0].City;
                addressUpdateRequest.CountryCode = request.Traveler.Addresses[0].Country != null ? request.Traveler.Addresses[0].Country.Code : "";
                addressUpdateRequest.PostalCode = request.Traveler.Addresses[0].PostalCode;
                addressUpdateRequest.StateCode = request.Traveler.Addresses[0].State != null ? request.Traveler.Addresses[0].State.Code : "";
                //addressUpdateRequest.Description = request.Traveler.Addresses[0].Channel != null ? request.Traveler.Addresses[0].Channel.ChannelTypeDescription : "";
                addressUpdateRequest.UpdateId = request.Traveler.CustomerId.ToString();
                addressUpdateRequest.LangCode = request.LanguageCode;
            }
            #endregion
            return addressUpdateRequest;
        }
        private async Task<bool> InsertTravelerRewardProgram(MOBUpdateTravelerRequest request)
        {
            if (_configuration.GetValue<bool>("CustomerDataRewardProgramAwsUCB"))
            {
                return InsertTravelerRewardProgramV2(request);
            }

            bool insertTravelerRewardProgram = false;
            United.Services.Customer.Common.InsertRewardProgramRequest insertRewardProgram = GetInsertRewardProgramRequest(request);
            string jsonRequest = JsonSerializer.Serialize<United.Services.Customer.Common.InsertRewardProgramRequest>(insertRewardProgram);

            string url = "/InsertRewardProgram";
         
            var response = await _customerDataService.GetProfile<SaveResponse>(request.Token, jsonRequest, _headers.ContextValues.SessionId, url);

            if (response != null)
            {
                if (response != null && response.Status.Equals(United.Services.Customer.Common.Constants.StatusType.Success) && response.ReturnValues != null)
                {
                    insertTravelerRewardProgram = true;
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                        }

                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToInsertTravelerRewardProgramToProfileErrorMessage");
                        if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL InsertTravelerRewardProgram(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToInsertTravelerRewardProgramToProfileErrorMessage");
                if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  InsertTravelerRewardProgram(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            return insertTravelerRewardProgram;
        }
        private United.Services.Customer.Common.InsertRewardProgramRequest GetInsertRewardProgramRequest(MOBUpdateTravelerRequest request)
        {
            #region Build insert Traverl Request
            United.Services.Customer.Common.InsertRewardProgramRequest rewardInsertRequest = new Services.Customer.Common.InsertRewardProgramRequest();

            if (request.UpdateRewardProgramInfo && request.Traveler.AirRewardPrograms != null && request.Traveler.AirRewardPrograms.Count > 0)
            {
                rewardInsertRequest.TravelerKey = request.Traveler.Key;
                rewardInsertRequest.ProfileId = request.Traveler.ProfileId;
                rewardInsertRequest.CustomerId = request.Traveler.CustomerId;
                rewardInsertRequest.ProgramId = Convert.ToInt32(request.Traveler.AirRewardPrograms[0].ProgramId);
                rewardInsertRequest.ProgramMemberId = request.Traveler.AirRewardPrograms[0].MemberId;
                rewardInsertRequest.InsertId = request.Traveler.CustomerId.ToString();
                rewardInsertRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices");
                rewardInsertRequest.LangCode = request.LanguageCode;
            }
            #endregion
            return rewardInsertRequest;
        }
        private async Task<bool> InsertTravelerPhone(MOBUpdateTravelerRequest request)
        {
            bool updateTravelerPhone = false;
            #region
            InsertPhoneRequest insertPhone = GetInsertPhoneRequest(request);
            string jsonRequest = JsonSerializer.Serialize<InsertPhoneRequest>(insertPhone);

            string url = "/InsertPhone ";          
            var response = await _customerDataService.GetProfile<SaveResponse>(request.Token, jsonRequest, _headers.ContextValues.SessionId, url);

            if (response != null)
            {
                #region
                if (response != null && response.Status.Equals(United.Services.Customer.Common.Constants.StatusType.Success) && response.ReturnValues != null)
                {
                    updateTravelerPhone = true;
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                        }
                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToInsertPhoneToProfileErrorMessage");
                        if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL InsertTravelerPhone(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
                #endregion
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToInsertPhoneToProfileErrorMessage");
                if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  InsertTravelerPhone(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion
            return updateTravelerPhone;
        }
        private United.Services.Customer.Common.InsertPhoneRequest GetInsertPhoneRequest(MOBUpdateTravelerRequest request)
        {
            #region Build insert Traverl Request
            United.Services.Customer.Common.InsertPhoneRequest phoneInsertRequest = new Services.Customer.Common.InsertPhoneRequest();
            if (request.UpdatePhoneInfo && request.Traveler.Phones != null && request.Traveler.Phones.Count > 0)
            {
                phoneInsertRequest.TravelerKey = request.Traveler.Key;
                phoneInsertRequest.CustomerId = request.Traveler.CustomerId;
                phoneInsertRequest.ChannelTypeCode = string.IsNullOrEmpty(request.Traveler.Phones[0].ChannelTypeCode) ? "H" : request.Traveler.Phones[0].ChannelTypeCode.Trim();
                phoneInsertRequest.Number = (request.Traveler.Phones[0].AreaNumber + "-" + request.Traveler.Phones[0].PhoneNumber).Trim().Trim('-');
                phoneInsertRequest.CountryCode = string.IsNullOrEmpty(request.Traveler.Phones[0].CountryCode) ? "US" : request.Traveler.Phones[0].CountryCode.Trim();
                phoneInsertRequest.DeviceType = string.IsNullOrEmpty(request.Traveler.Phones[0].DeviceTypeCode) ? "PH" : request.Traveler.Phones[0].DeviceTypeCode.Trim();
                if (!request.UpdateCreditCardInfo)
                {
                    phoneInsertRequest.SetAsDayOfTravelContact = true;
                }
                phoneInsertRequest.Description = request.Traveler.Phones[0].Description;
                phoneInsertRequest.InsertId = request.Traveler.CustomerId.ToString();
                phoneInsertRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices");
                phoneInsertRequest.LangCode = request.LanguageCode;
            }
            #endregion
            return phoneInsertRequest;
        }
        private async Task<(bool emailChanged, bool rewardProgramUpdated)> isRewardProgramChanged(MOBUpdateTravelerRequest request, bool emailChanged, bool rewardProgramUpdated)
        {
            emailChanged = false; rewardProgramUpdated = false;
            try
            {
                if (!string.IsNullOrEmpty(request.SessionId))
                {
                    Reservation persistedReservation = new Reservation();
                    if (request != null)
                        persistedReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, persistedReservation.ObjectName, new List<string> { request.SessionId, persistedReservation.ObjectName }).ConfigureAwait(false);

                    if (persistedReservation != null &&
                        persistedReservation.ShopReservationInfo2 != null && persistedReservation.ShopReservationInfo2.AllEligibleTravelersCSL != null && persistedReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count > 0 &&
                        (_configuration.GetValue<bool>("EnableAirRewardProgramsNullCheck") ? (request.Traveler != null && request.Traveler.AirRewardPrograms != null && request.Traveler.AirRewardPrograms.Count > 0) : true))//Removing the null check for airreward program ..Since there is check for email updating logic which doesnt require reward program to be there in the request and before updating the reward program we have null check inside this block.
                    {
                        var traveler = persistedReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Where(t => !string.IsNullOrEmpty(t.Key) && t.Key.ToUpper().Equals(request.Traveler.Key.ToUpper())).FirstOrDefault();
                        if (traveler != null)
                        {
                            if (request.UpdateEmailInfo == true && request.Traveler.EmailAddresses != null && request.Traveler.EmailAddresses.Count > 0 && !String.IsNullOrEmpty(request.Traveler.EmailAddresses[0].Key) && !String.IsNullOrEmpty(request.Traveler.EmailAddresses[0].EmailAddress))
                            {
                                if (traveler.EmailAddresses != null && traveler.EmailAddresses.Count > 0)
                                {
                                    foreach (var email in traveler.EmailAddresses.Where(e => !string.IsNullOrEmpty(e.Key)))
                                    {
                                        if (email.Key.ToUpper().Equals(request.Traveler.EmailAddresses[0].Key.ToUpper()) && (!email.EmailAddress.ToUpper().Equals(request.Traveler.EmailAddresses[0].EmailAddress.ToUpper())))
                                        {
                                            emailChanged = true;
                                        }
                                    }
                                }
                            }
                            if (request.UpdateRewardProgramInfo == true && request.Traveler.AirRewardPrograms != null && request.Traveler.AirRewardPrograms.Count > 0 && !String.IsNullOrEmpty(request.Traveler.AirRewardPrograms[0].RewardProgramKey))
                            {
                                if (!request.Traveler.IsProfileOwner || (request.Traveler.IsProfileOwner && request.Traveler.AirRewardPrograms[0].CarrierCode.Trim().ToUpper() != "UA")) // to check not to update profile owner UA MP details. It will fail if trying to update the MP Account of the profile owner
                                {
                                    if (traveler.AirRewardPrograms != null && traveler.AirRewardPrograms.Count > 0)
                                    {
                                        foreach (var prog in traveler.AirRewardPrograms.Where(p => p != null && !string.IsNullOrEmpty(p.RewardProgramKey)))
                                        {
                                            var rewardInfo = request.Traveler.AirRewardPrograms.Where(arp => !string.IsNullOrEmpty(arp.RewardProgramKey) && arp.RewardProgramKey.Equals(prog.RewardProgramKey)).FirstOrDefault();
                                            if (rewardInfo != null && (!rewardInfo.MemberId.ToUpper().Equals(prog.MemberId.ToUpper()) || (!rewardInfo.ProgramName.ToUpper().Equals(prog.ProgramName.ToUpper()))))
                                            {
                                                rewardProgramUpdated = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return (emailChanged, rewardProgramUpdated);
        }
        private async Task<bool> UpdateTravelerRewardProgram(MOBUpdateTravelerRequest request)
        {
            if (_configuration.GetValue<bool>("CustomerDataRewardProgramAwsUCB"))
            {
                return UpdateTravelerRewardProgramV2(request);
            }

            bool updateTravelerRewardProgram = false;
            #region
            UpdateRewardProgramRequest updateRewardProgram = GetUpdateRewardProgramRequest(request);
            string jsonRequest = JsonSerializer.Serialize<UpdateRewardProgramRequest>(updateRewardProgram);

            string url = "/UpdateRewardProgram";
            var response = await _customerDataService.GetProfile<SaveResponse>(request.Token, jsonRequest, _headers.ContextValues.SessionId, url);

            if (response != null)
            {
                if (response != null && response.Status.Equals(Services.Customer.Common.Constants.StatusType.Success) && response.ReturnValues != null)
                {
                    updateTravelerRewardProgram = true;
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                        }

                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerRewardProgramToProfileErrorMessage");
                        if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL UpdateTravelerRewardProgram(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerRewardProgramToProfileErrorMessage");
                if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  UpdateTravelerRewardProgram(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion
            return updateTravelerRewardProgram;
        }
        private UpdateRewardProgramRequest GetUpdateRewardProgramRequest(MOBUpdateTravelerRequest request)
        {
            #region Build insert Traverl Request
            United.Services.Customer.Common.UpdateRewardProgramRequest rewardUpdateRequest = new Services.Customer.Common.UpdateRewardProgramRequest();

            if (request.UpdateRewardProgramInfo && request.Traveler.AirRewardPrograms != null && request.Traveler.AirRewardPrograms.Count > 0)
            {
                rewardUpdateRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices");
                rewardUpdateRequest.ProgramId = Convert.ToInt32(request.Traveler.AirRewardPrograms[0].ProgramId);
                rewardUpdateRequest.ProgramMemberId = request.Traveler.AirRewardPrograms[0].MemberId;
                rewardUpdateRequest.UpdateId = request.Traveler.CustomerId.ToString();
                rewardUpdateRequest.RewardProgramKey = request.Traveler.AirRewardPrograms[0].RewardProgramKey;
                rewardUpdateRequest.PreferenceId = Convert.ToInt32(request.Traveler.AirRewardPrograms[0].ProgramId);//**//--> Need to confirm whats this value could be or where this value would be at get profile resposne?
                rewardUpdateRequest.LangCode = request.LanguageCode;
            }
            #endregion
            return rewardUpdateRequest;
        }
        private async Task<bool> UpdateTravelerEmail(MOBUpdateTravelerRequest request)
        {
            bool updateTravelerEmail = false;
            #region
            United.Services.Customer.Common.UpdateEmailAddressRequest updateEmail = GetUpdateEmailAddressRequest(request);
            string jsonRequest = JsonSerializer.Serialize<United.Services.Customer.Common.UpdateEmailAddressRequest>(updateEmail);
            string url = string.Format("/UpdateEmailAddress");
           
            var response = await  _customerDataService.GetProfile<SaveResponse>(request.Token, jsonRequest, _headers.ContextValues.SessionId, url);

            if (response != null)
            {


                if (response != null && response.Status.Equals(United.Services.Customer.Common.Constants.StatusType.Success) && response.ReturnValues != null)
                {
                    updateTravelerEmail = true;
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                        }
                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerEmailToProfileErrorMessage");
                        if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL UpdateTravelerEmail(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerEmailToProfileErrorMessage");
                if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  UpdateTravelerEmail(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion
            return updateTravelerEmail;
        }
        private United.Services.Customer.Common.UpdateEmailAddressRequest GetUpdateEmailAddressRequest(MOBUpdateTravelerRequest request)
        {
            #region Build insert Traverl Request
            United.Services.Customer.Common.UpdateEmailAddressRequest emailUpdateRequest = new Services.Customer.Common.UpdateEmailAddressRequest();
            if (request.UpdateEmailInfo && request.Traveler.EmailAddresses != null && request.Traveler.EmailAddresses.Count > 0)
            {
                emailUpdateRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices");
                emailUpdateRequest.EmailAddress = request.Traveler.EmailAddresses[0].EmailAddress;
                emailUpdateRequest.CustomerId = request.Traveler.CustomerId;
                emailUpdateRequest.UpdateId = request.Traveler.CustomerId.ToString();
                emailUpdateRequest.EmailAddressKey = request.Traveler.EmailAddresses[0].Key;
            }
            #endregion
            return emailUpdateRequest;
        }
        private async Task<bool> InsertTravelerEmail(MOBUpdateTravelerRequest request)
        {
            bool insertTravelerEmail = false;
            #region
            InsertEmailAddressRequest updateEmail = GetInsertEmailAddressRequest(request);
            string jsonRequest = JsonSerializer.Serialize<InsertEmailAddressRequest>(updateEmail);
            string url = "/InsertEmailAddress";
           
            var response = await _customerDataService.GetProfile<SaveResponse>(request.Token, jsonRequest, _headers.ContextValues.SessionId, url);

            if (response != null)
            {
                if (response != null && response.Status.Equals(United.Services.Customer.Common.Constants.StatusType.Success) && response.ReturnValues != null)
                {
                    insertTravelerEmail = true;
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                        }
                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToInsertTravelerEmailToProfileErrorMessage");
                        if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL InsertTravelerEmail(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToInsertTravelerEmailToProfileErrorMessage");
                if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  InsertTravelerEmail(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion
            return insertTravelerEmail;
        }
        private InsertEmailAddressRequest GetInsertEmailAddressRequest(MOBUpdateTravelerRequest request)
        {
            #region Build insert Traverl Request
            United.Services.Customer.Common.InsertEmailAddressRequest emailInsertRequest = new Services.Customer.Common.InsertEmailAddressRequest();
            if (request.UpdateEmailInfo && request.Traveler.EmailAddresses != null && request.Traveler.EmailAddresses.Count > 0)
            {
                emailInsertRequest.TravelerKey = request.Traveler.Key;
                emailInsertRequest.CustomerId = request.Traveler.CustomerId;
                emailInsertRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices");
                emailInsertRequest.EmailAddress = request.Traveler.EmailAddresses[0].EmailAddress;
                emailInsertRequest.ChannelTypeCode = request.Traveler.EmailAddresses[0].Channel != null ? request.Traveler.EmailAddresses[0].Channel.ChannelCode : "O";
                //emailInsertRequest.Description = request.Traveler.EmailAddresses[0].Channel.ChannelDescription; 
                emailInsertRequest.InsertId = request.Traveler.CustomerId.ToString();
                emailInsertRequest.LangCode = request.LanguageCode;
                emailInsertRequest.SetAsPrimary = request.SetEmailAsPrimay;
                if (!request.UpdateCreditCardInfo)
                {
                    emailInsertRequest.SetAsDayOfTravelContact = true;
                }
            }
            #endregion
            return emailInsertRequest;
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
        private async Task<United.Services.Customer.Common.InsertCreditCardRequest> GetInsertCreditCardRequest(MOBUpdateTravelerRequest request)
        {
            #region Build insert Traverl Request
            United.Services.Customer.Common.InsertCreditCardRequest creditCardInsertRequest = new Services.Customer.Common.InsertCreditCardRequest();
            string accountNumberToken = string.Empty;
            var tupleResponse = await _profileCreditCard.GenerateCCTokenWithDataVault(request.Traveler.CreditCards[0], request.SessionId, request.Token, request.Application, request.DeviceId, accountNumberToken);
            accountNumberToken = tupleResponse.ccDataVaultToken;
            if (request.UpdateCreditCardInfo && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && !string.IsNullOrEmpty(request.Traveler.CreditCards[0].EncryptedCardNumber) && tupleResponse.Item1)
            {
                creditCardInsertRequest.TravelerKey = request.Traveler.Key;
                creditCardInsertRequest.CustomerId = request.Traveler.CustomerId;
                creditCardInsertRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices");
                creditCardInsertRequest.AddressKey = request.Traveler.CreditCards[0].AddressKey;
                //creditCardInsertRequest.CreditCardNumber = request.Traveler.CreditCards[0].UnencryptedCardNumber;
                creditCardInsertRequest.AccountNumberToken = accountNumberToken;
                creditCardInsertRequest.CreditCardType = request.Traveler.CreditCards[0].CardType;
                creditCardInsertRequest.ExpirationMonth = Convert.ToInt32(request.Traveler.CreditCards[0].ExpireMonth.Trim());
                //creditCardInsertRequest.ExpirationYear = Convert.ToInt32(request.Traveler.CreditCards[0].ExpireYear.Trim());
                creditCardInsertRequest.ExpirationYear = Convert.ToInt32(request.Traveler.CreditCards[0].ExpireYear.Trim().Length == 2 ? "20" + request.Traveler.CreditCards[0].ExpireYear.Trim() : request.Traveler.CreditCards[0].ExpireYear.Trim());
                creditCardInsertRequest.Name = request.Traveler.CreditCards[0].cCName;
                creditCardInsertRequest.PhoneKey = request.Traveler.CreditCards[0].PhoneKey;
                creditCardInsertRequest.Description = request.Traveler.CreditCards[0].Description;
                creditCardInsertRequest.InsertId = request.Traveler.CustomerId.ToString();
                creditCardInsertRequest.LangCode = request.LanguageCode;
            }
            #endregion
            return creditCardInsertRequest;
        }
        public async Task<(bool returnValue, List<MOBItem> insertItemKeys)> InsertTraveler(InsertTravelerRequest request, List<MOBItem> insertItemKeys)
        {
            if (_configuration.GetValue<bool>("CustomerDataTravelerAwsUCB"))
            {
                return await InsertTravelerV2(request);
            }

            bool insertTravelerSuccess = false;
            United.Services.Customer.Common.InsertTravelerRequest insertTravelRequest = await GetInsertTravelerRequest(request);
            string jsonRequest = JsonSerializer.Serialize<United.Services.Customer.Common.InsertTravelerRequest>(insertTravelRequest);
            var response = await _customerDataService.InsertTraveler<United.Services.Customer.Common.SaveResponse>(request.Token, jsonRequest, request.SessionId);

            if (response != null && response.Status.Equals(United.Services.Customer.Common.Constants.StatusType.Success) && response.ReturnValues != null)
            {
                insertTravelerSuccess = true;
                var obj = (from st in response.ReturnValues
                           where st.Key.ToUpper().Trim() == "TravelerKey".ToUpper().Trim()
                           select st).ToList();
                MOBItem item = new MOBItem();
                item.Id = "TravelerKey";
                item.CurrentValue = obj[0].Value;
                insertItemKeys.Add(item);
            }
            else
            {
                if (response.Errors != null && response.Errors.Count > 0)
                {
                    string errorMessage = string.Empty;
                    foreach (var error in response.Errors)
                    {
                        if (error.ErrorType.Trim().ToUpper() == "NameMismatch".ToUpper().Trim())
                        {
                            string insertTravelerMpNumber = request.Traveler.AirRewardPrograms != null ? request.Traveler.AirRewardPrograms[0].MemberId : string.Empty;
                            errorMessage = _configuration.GetValue<string>("ValidateMPNameMisMatchErrorMessage").ToString();
                        }
                        else
                        {
                            errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                        }
                    }

                    throw new MOBUnitedException(errorMessage);
                }
                else
                {
                    throw new MOBUnitedException("Unable to Insert Traveler.");
                }
            }

            return (insertTravelerSuccess,insertItemKeys);
        }

        private async Task<United.Services.Customer.Common.InsertTravelerRequest> GetInsertTravelerRequest(InsertTravelerRequest request)
        {
            #region Sample InsertTravelerRequest
            //var requestTest = new United.Services.Customer.Common.InsertTravelerRequest
            //{
            //    LoyaltyId = "WU907059", //edbtest2
            //    DataSetting = "Prod",
            //    Title = "MS",
            //    FirstName = "Charlie" + DateTime.Now.Minute.ToString() + "." + DateTime.Now.Second.ToString(),
            //    MiddleName = "P",
            //    LastName = "Deen",
            //    Suffix = "I",
            //    Gender = "M",
            //    DateOfBirth = DateTime.Now.AddYears(-30).AddMonths(3),
            //    LangCode = "en-US",
            //    PhoneCountryCode = "US",
            //    PhoneNumber = "713-635-5565",
            //    IsDayOfTravelPhone = true,
            //    IsDayOfTravelEmail = true,
            //    EmailAddress = "sp.rock@gmail.com",
            //    ProgramId = 22,     //Valid air programs 3,8,9,12-19,21-29
            //    ProgramMemberId = "INF627",
            //    //ProgramId = 7,      //MP
            //    //ProgramMemberId = "CH29",   //Should fail
            //    ServiceAnimalTypeId = 3,
            //    ServiceAnimalId = 8,
            //    MealId = 3,
            //    InsertId = "12345678",
            //};
            #endregion
            #region Build insert Traverl Request
            United.Services.Customer.Common.InsertTravelerRequest insertTravelRequest = new Services.Customer.Common.InsertTravelerRequest();
            insertTravelRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices").ToString();
            insertTravelRequest.LoyaltyId = request.MileagePlusNumber;
            insertTravelRequest.Title = request.Traveler.Title;
            insertTravelRequest.FirstName = request.Traveler.FirstName;
            insertTravelRequest.MiddleName = request.Traveler.MiddleName;
            insertTravelRequest.LastName = request.Traveler.LastName;
            insertTravelRequest.Suffix = request.Traveler.Suffix;
            int offSetHours = _configuration.GetValue<string>("UpdateTravelerDOBOffSetHours") == null ? 12 : Convert.ToInt32(_configuration.GetValue<string>("UpdateTravelerDOBOffSetHours").ToString());
            insertTravelRequest.DateOfBirth = new DateTime();
            insertTravelRequest.DateOfBirth = DateTime.ParseExact(request.Traveler.BirthDate, "MM/dd/yyyy", CultureInfo.InvariantCulture).AddHours(offSetHours);
            insertTravelRequest.Gender = request.Traveler.GenderCode;
            insertTravelRequest.KnownTravelerNumber = request.Traveler.KnownTravelerNumber;
            insertTravelRequest.RedressNumber = request.Traveler.RedressNumber;
            if (request.Traveler.Phones != null && request.Traveler.Phones.Count > 0)
            {
                if (!String.IsNullOrEmpty(request.Traveler.Phones[0].PhoneNumber) && !String.IsNullOrEmpty(request.Traveler.Phones[0].CountryCode))
                {
                    string invalidPhoneNumberMessage = string.Empty;
                    var tupleRes = await ValidatePhoneWithCountryCode(request.Traveler.Phones[0], request.Application, request.SessionId, request.DeviceId, request.Token, invalidPhoneNumberMessage);
                    invalidPhoneNumberMessage = tupleRes.message;
                    if (!tupleRes.returnValue)
                    {
                        throw new MOBUnitedException(invalidPhoneNumberMessage);
                    }
                }
                insertTravelRequest.PhoneNumber = (request.Traveler.Phones[0].AreaNumber + "-" + request.Traveler.Phones[0].PhoneNumber).Trim().Trim('-');
                insertTravelRequest.PhoneCountryCode = request.Traveler.Phones[0].CountryCode;
                insertTravelRequest.IsDayOfTravelPhone = true;
            }
            if (request.Traveler.EmailAddresses != null && request.Traveler.EmailAddresses.Count > 0)
            {
                insertTravelRequest.EmailAddress = request.Traveler.EmailAddresses[0].EmailAddress;
                insertTravelRequest.IsDayOfTravelEmail = true;
            }
            if (request.Traveler.AirRewardPrograms != null && request.Traveler.AirRewardPrograms.Count > 0)
            {
                insertTravelRequest.ProgramId = Convert.ToInt32(request.Traveler.AirRewardPrograms[0].ProgramId);
                insertTravelRequest.ProgramMemberId = request.Traveler.AirRewardPrograms[0].MemberId;
            }
            insertTravelRequest.InsertId = "123456";
            if (IsEnabledNationalityAndResidence(false, request.Application.Id, request.Application.Version.Major))
            {
                if (request.Traveler != null && !string.IsNullOrEmpty(request.Traveler.CountryOfResidence) && request.Traveler.CountryOfResidence.ToUpper().Equals("OTHER"))
                {
                    insertTravelRequest.CountryOfResidence = null;
                }
                else
                {
                    insertTravelRequest.CountryOfResidence = request.Traveler.CountryOfResidence;
                }

                if (request.Traveler != null && !string.IsNullOrEmpty(request.Traveler.Nationality) && request.Traveler.Nationality.ToUpper().Equals("OTHER"))
                {
                    insertTravelRequest.Nationality = null;
                }
                else
                {
                    insertTravelRequest.Nationality = request.Traveler.Nationality;
                }
            }

            ////SSR Testing
            //insertTravelRequest.MealId = 14;
            //insertTravelRequest.SpecialRequestIds = new List<int> { 6, 17, 20 };
            //insertTravelRequest.ServiceAnimalTypeId = 9; // AnimalTypeDesc=Balance-assistance dog
            //insertTravelRequest.ServiceAnimalId = 3; // AnimalDesc=Dog

            #endregion
            return insertTravelRequest;
        }
        #endregion updated member profile End

        public async Task<MOBUpdateTravelerInfoResponse> UpdateTravelerMPId(string deviceId, string accessCode, string recordLocator, string sessionId, string transactionId, string languageCode, int applicationId, string appVersion, string mileagePlusNumber, string firstName, string lastName, string sharesPosition, string token)
        {

            MOBUpdateTravelerInfoResponse response = new MOBUpdateTravelerInfoResponse();

            #region//****Get Call Duration Code*******

            Stopwatch cslStopWatch;
            cslStopWatch = new Stopwatch();
            cslStopWatch.Reset();
            cslStopWatch.Start();
            #endregion            

            try
            {
                MOBUpdateTravelerInfoRequest request = CreateTravelerInfoRequest(deviceId, accessCode, recordLocator, sessionId, transactionId, languageCode, applicationId, appVersion, mileagePlusNumber, firstName, languageCode, sharesPosition);

                if (request == null)
                    throw new MOBUnitedException("UpdateTravelerMPId request cannot be null.");

                MOBPNRPassenger travelerInfo = request.TravelersInfo[0]; //Fetching the first item from traveler Info

                List<United.Service.Presentation.ReservationModel.Traveler> travelerInfoMPKTNRequest = UpdateTravelerInfoMPRedressKTNRequest(request);
                string jsonRequest = JsonConvert.SerializeObject(travelerInfoMPKTNRequest);

                //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(sessionId, "UpdateTravelerMpId", "Request - JSON", applicationId, appVersion, deviceId, jsonRequest));

                //MPNumber CSL Call
                await UpdateTravlerMPorFFInfo(response, request, token, jsonRequest, travelerInfo, true);

                if (response.Exception != null)
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("AssociateMPNumberFailErrorMsg"));
                }
            }
            catch (System.Net.WebException wex)
            {
                if (wex.Response != null)
                {
                    var errorResponse = new StreamReader(wex.Response.GetResponseStream()).ReadToEnd();

                    //if (traceSwitch.TraceInfo)
                    //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(sessionId, "SSR - UpdateTravelerMPId  - Exception", "UpdateTravelerInfo", errorResponse));

                    throw new System.Exception(wex.Message);
                }
            }

            #region// 2 = cslStopWatch//****Get Call Duration Code*******

            if (cslStopWatch.IsRunning)
            {
                cslStopWatch.Stop();
            }
            string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

            #endregion//****Get Call Duration Code*******

            //if (traceSwitch.TraceError)
            //{
            //    logEntries.Add(LogEntry.GetLogEntry<MOBUpdateTravelerInfoResponse>(sessionId, "UpdateTravelerMPId - Client Response for Update Traveler Info", "to client Response", applicationId, appVersion, deviceId, response));
            //}
            return response;
        }

        private MOBUpdateTravelerInfoRequest CreateTravelerInfoRequest(string deviceId, string accessCode, string recordLocator, string sessionId, string transactionId, string languageCode, int appId, string appVersion, string mileagePlusNumber, string firstName, string lastName, string sharesPosition)
        {
            MOBUpdateTravelerInfoRequest request = new MOBUpdateTravelerInfoRequest();

            request.AccessCode = accessCode;
            request.DeviceId = deviceId;
            request.LanguageCode = languageCode;
            request.SessionId = sessionId;
            request.TransactionId = transactionId;
            request.RecordLocator = recordLocator;
            request.Application = new MOBApplication();
            request.Application.Id = appId;
            request.Application.Version = new MOBVersion();
            request.Application.Version.Major = appVersion;
            request.TravelersInfo = new List<MOBPNRPassenger>();
            request.TravelersInfo.Add(new MOBPNRPassenger()
            {
                MileagePlus = new MOBCPMileagePlus()
                {
                    MileagePlusId = mileagePlusNumber
                },
                PassengerName = new MOBName()
                {
                    First = firstName,
                    Last = lastName
                },
                SharesGivenName = firstName,
                SHARESPosition = sharesPosition,
            });

            return request;
        }

        private List<United.Service.Presentation.ReservationModel.Traveler> UpdateTravelerInfoMPRedressKTNRequest(MOBUpdateTravelerInfoRequest request)
        {
            List<United.Service.Presentation.ReservationModel.Traveler>
                _TravellerList = new List<United.Service.Presentation.ReservationModel.Traveler>();


            request.TravelersInfo.ForEach(traveler =>
            {
                Service.Presentation.PersonModel.Person person;
                LoyaltyProgramProfile loyaltyProgramProfile;

                if ((string.IsNullOrEmpty(traveler.KnownTravelerNumber) == false || string.IsNullOrEmpty(traveler.RedressNumber) == false))
                {
                    person = new Service.Presentation.PersonModel.Person
                    {
                        Documents = new Collection<Service.Presentation.PersonModel.Document>() {
                        new Service.Presentation.PersonModel.Document {
                            KnownTravelerNumber = traveler.KnownTravelerNumber,
                            RedressNumber = traveler.RedressNumber
                        }
                    },
                        GivenName = (!string.IsNullOrEmpty(traveler.SharesGivenName)
                        && !string.Equals(traveler.SharesGivenName, traveler.PassengerName.First, StringComparison.OrdinalIgnoreCase))
                        ? traveler.SharesGivenName
                        : traveler.PassengerName.First,

                        Surname = traveler.PassengerName.Last,
                        InfantIndicator = "FALSE",
                        Key = traveler.SHARESPosition
                    };
                }
                else
                {
                    if (traveler.PassengerName != null)
                    {
                        person = new Service.Presentation.PersonModel.Person
                        {
                            GivenName = (!string.IsNullOrEmpty(traveler.SharesGivenName)
                        && !string.Equals(traveler.SharesGivenName, traveler.PassengerName.First, StringComparison.OrdinalIgnoreCase))
                        ? traveler.SharesGivenName
                        : traveler.PassengerName.First,

                            Surname = traveler.PassengerName.Last,
                            InfantIndicator = "FALSE",
                            Key = traveler.SHARESPosition
                        };
                    }
                    else
                    {
                        person = null;
                    }
                }

                if (traveler.MileagePlus != null && string.IsNullOrEmpty(traveler.MileagePlus.MileagePlusId) == false)
                {
                    loyaltyProgramProfile = new LoyaltyProgramProfile
                    {
                        LoyaltyProgramCarrierCode = "UA",
                        LoyaltyProgramMemberID = traveler.MileagePlus.MileagePlusId
                    };
                }
                else if (traveler.OaRewardPrograms != null)
                {
                    loyaltyProgramProfile = new LoyaltyProgramProfile
                    {
                        LoyaltyProgramCarrierCode = traveler.OaRewardPrograms[0].VendorCode,
                        LoyaltyProgramMemberID = traveler.OaRewardPrograms[0].ProgramMemberId

                    };
                }
                else
                {
                    loyaltyProgramProfile = null;
                }

                _TravellerList.Add(new United.Service.Presentation.ReservationModel.Traveler
                {
                    Person = person,
                    LoyaltyProgramProfile = loyaltyProgramProfile
                });

            });

            return _TravellerList;
        }

        private async Task<MOBUpdateTravelerInfoResponse> UpdateTravlerMPorFFInfo
           (MOBUpdateTravelerInfoResponse response, MOBUpdateTravelerInfoRequest request, string Token, string cslJsonRequest, MOBPNRPassenger travelerInfo, bool associateMPidFlow = false)
        {
            var urlLoyaltyId = string.Empty;
            var jsonResponseLoyaltyId = string.Empty;

            bool hasMilagePlus = (travelerInfo.MileagePlus != null && !string.IsNullOrEmpty(travelerInfo.MileagePlus.MileagePlusId));
            bool hasRewardProgram = (travelerInfo.OaRewardPrograms != null && travelerInfo.OaRewardPrograms.Any() && !string.IsNullOrEmpty(travelerInfo.OaRewardPrograms[0].ProgramMemberId));

            try
            {
                if (hasMilagePlus || hasRewardProgram)
                {
                    urlLoyaltyId = string.Format("{0}/Passengers/Loyalty?RetrievePNR=true&EndTransaction=true", request.RecordLocator);
                    //_configuration.GetValue(("ManageRes_EditTraveler"), request.RecordLocator));

                    jsonResponseLoyaltyId = await _pNRRetrievalService.UpdateTravelerInfo(Token, cslJsonRequest, urlLoyaltyId, request.SessionId);
                }

                //If Response NOT-NULL Check for MP Number / FREQ Flyer Number 
                if (!string.IsNullOrEmpty(jsonResponseLoyaltyId))
                {
                    List<United.Service.Presentation.CommonModel.Message>
                       msgResponseLoyaltyId = JsonConvert.DeserializeObject<List<United.Service.Presentation.CommonModel.Message>>(jsonResponseLoyaltyId);

                    if (associateMPidFlow && msgResponseLoyaltyId != null && msgResponseLoyaltyId[0].Type.ToLower() == "failed")
                    {
                        response.Exception = new MOBException("9999", _configuration.GetValue<string>("AssociateMPNumberFailErrorMsg"));
                    }

                    if (msgResponseLoyaltyId != null && string.Equals
                        (msgResponseLoyaltyId[0].Status, "SHARE_RESPONSE", StringComparison.OrdinalIgnoreCase))
                    {
                        if ((hasRewardProgram && msgResponseLoyaltyId[0].Text.IndexOf(travelerInfo.OaRewardPrograms[0].ProgramMemberId.ToUpper()) == -1) ||
                            (hasMilagePlus && msgResponseLoyaltyId[0].Text.IndexOf(travelerInfo.MileagePlus.MileagePlusId.ToUpper()) == -1))
                        {
                            response.Exception = new MOBException("9999", _configuration.GetValue<string>("ValidateManageResMPNameMisMatchErrorMessage"));
                        }
                    }
                }
            }
            catch (System.Net.WebException wex)
            {
                if (wex.Response != null)
                {
                    var errorResponse = new StreamReader(wex.Response.GetResponseStream()).ReadToEnd();

                    //if (traceSwitch.TraceInfo)
                    //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(request.SessionId, "UpdateTravelerInfo-MP/FF", "Exception", errorResponse));
                    //response.Exception = new MOBException("9999", _configuration.GetValue<string>("ValidateManageResMPNameMisMatchErrorMessage"));
                }
            }
            return response;
        }

        #endregion

        public async Task<bool> UpdatePassRider(MOBUpdateTravelerRequest request, string employeeID)
        {
            bool updateTravelerSuccess = false;
            if (string.IsNullOrEmpty(employeeID))
            {
                throw new Exception("employeeID is required.");
            }
            // Validate Phone even if request.UpdatePhoneInfo = false too as phone and email will be passed from payment page to update the reservation 
            if (request.UpdatePhoneInfo)
            {
                if (request.Traveler.Phones != null && request.Traveler.Phones.Count > 0 && !String.IsNullOrEmpty(request.Traveler.Phones[0].PhoneNumber) && !String.IsNullOrEmpty(request.Traveler.Phones[0].CountryCode))
                {
                    string invalidPhoneNumberMessage = string.Empty;
                    var tupleRes = await ValidatePhoneWithCountryCode(request.Traveler.Phones[0], request.Application, request.SessionId, request.DeviceId, request.Token, invalidPhoneNumberMessage);
                    invalidPhoneNumberMessage = tupleRes.message;
                    if (!tupleRes.returnValue)
                    {
                        throw new MOBUnitedException(invalidPhoneNumberMessage);
                    }
                }
            }

            String empTransactionID = await _sessionHelperService.GetSession<string>(employeeID + request.DeviceId, ObjectNames.EresTransactionIDFullName, new List<string> { employeeID + request.DeviceId, ObjectNames.EresTransactionIDFullName }).ConfigureAwait(false);
            #region as we are getting response in xml format .Applying XMLSerialisation to get the correct value
            if (!_configuration.GetValue<bool>("DisableUA20TravelerUpdatebugfix") && !string.IsNullOrEmpty(empTransactionID) && empTransactionID.Contains(@"<?xml"))
                empTransactionID = XmlSerializerHelper.GetObjectFromXmlData<string>(empTransactionID);
            #endregion

            if (string.IsNullOrEmpty(empTransactionID))
                throw new Exception("Unable to fetch Eres TransactionID");

            if (request.UpdateTravelerBasiInfo == true && !String.IsNullOrEmpty(request.Traveler.Key) && await isPassRiderBaseInfoUpdated(request))
            {

                updateTravelerSuccess = UpdatePassRiderBaseInfo(request, employeeID, empTransactionID);
            }

            if (request.UpdatePhoneInfo == true || request.UpdateEmailInfo == true && await isPassRiderContactInfoUpdated(request))
            {
                updateTravelerSuccess = UpdatePassRiderContactInfo(request, employeeID, empTransactionID);
            }


            return updateTravelerSuccess;
        }
        private async Task<bool> isPassRiderContactInfoUpdated(MOBUpdateTravelerRequest request)
        {

            try
            {
                if (!string.IsNullOrEmpty(request.SessionId))
                {
                    Reservation persistedReservation = new Reservation();
                    if (request != null)
                        persistedReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, new Reservation().ObjectName, new List<string> { request.SessionId, new Reservation().ObjectName }).ConfigureAwait(false);

                    if (persistedReservation != null &&
                        persistedReservation.ShopReservationInfo2 != null && persistedReservation.ShopReservationInfo2.AllEligibleTravelersCSL != null && persistedReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count > 0)//Removing the null check for airreward program ..Since there is check for email updating logic which doesnt require reward program to be there in the request and before updating the reward program we have null check inside this block.
                    {
                        var traveler = persistedReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Where(t => !string.IsNullOrEmpty(t.Key) && t.Key.ToUpper().Equals(request.Traveler.Key.ToUpper())).FirstOrDefault();
                        if (traveler != null)
                        {
                            if (request.UpdateEmailInfo)
                            {
                                if ((request.Traveler.EmailAddresses?.Count ?? 0) != (traveler.EmailAddresses?.Count ?? 0))
                                {
                                    return true;
                                }

                                if ((request.Traveler.EmailAddresses?[0]?.EmailAddress?.ToUpper() ?? "") != (traveler.EmailAddresses?[0]?.EmailAddress?.ToUpper() ?? ""))
                                    return true;

                            }

                            if (request.UpdatePhoneInfo)
                            {
                                if ((request.Traveler.Phones?.Count ?? 0) != (traveler.Phones?.Count ?? 0))
                                    return true;

                                if ((request.Traveler.Phones?[0]?.AreaNumber ?? "" + request.Traveler.Phones?[0]?.PhoneNumber ?? "") != (traveler.Phones?[0]?.PhoneNumber ?? ""))
                                    return true;
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }
        private async Task<bool> isPassRiderBaseInfoUpdated(MOBUpdateTravelerRequest request)
        {
            try
            {
                if (!string.IsNullOrEmpty(request.SessionId))
                {
                    Reservation persistedReservation = new Reservation();
                    if (request != null)
                        persistedReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, new Reservation().ObjectName, new List<string> { request.SessionId, new Reservation().ObjectName });

                    if (persistedReservation != null &&
                        persistedReservation.ShopReservationInfo2 != null && persistedReservation.ShopReservationInfo2.AllEligibleTravelersCSL != null && persistedReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count > 0)//Removing the null check for airreward program ..Since there is check for email updating logic which doesnt require reward program to be there in the request and before updating the reward program we have null check inside this block.
                    {
                        var traveler = persistedReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Where(t => !string.IsNullOrEmpty(t.Key) && t.Key.ToUpper().Equals(request.Traveler.Key.ToUpper())).FirstOrDefault();
                        if (traveler != null)
                        {
                            if (string.IsNullOrEmpty(request.Traveler.FirstName) || string.IsNullOrEmpty(request.Traveler.LastName) || string.IsNullOrEmpty(request.Traveler.BirthDate) || string.IsNullOrEmpty(request.Traveler.GenderCode))
                                return false;

                            if ((request.Traveler.RedressNumber != traveler.RedressNumber) || (request.Traveler.KnownTravelerNumber != traveler.KnownTravelerNumber) || request.Traveler.Nationality != traveler.Nationality || request.Traveler.CountryOfResidence != traveler.CountryOfResidence)

                                return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public bool UpdatePassRiderBaseInfo(MOBUpdateTravelerRequest request, string employeeID, string empTransactionID)
        {
            bool updateTravelerSuccess = false;
            #region
            TravelDocumentRequest travelDocumentRequest = new TravelDocumentRequest();
            SSRInfo updatePassrider = GetUpdatePassriderRequest(request, employeeID);
            travelDocumentRequest.TransactionId = empTransactionID;

            travelDocumentRequest.SSRInfos = new List<SSRInfo>();
            travelDocumentRequest.SSRInfos.Add(updatePassrider);

            string jsonRequest = JsonConvert.SerializeObject(travelDocumentRequest);

            string path = "/Employee/TravelDocument/Save";
            //string url = string.Format("{0}/UpdateTravelerBase", _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLProfile"]);


            var response = _baseEmployeeRes.UpdateTravelerBase<TravelDocResponse>(request.Token, jsonRequest, request.SessionId, path);

            if (response.Result != null)
            {
                if (response != null && response.Result.IsSuccess)
                {
                    updateTravelerSuccess = true;
                }
                else
                {
                    if (!string.IsNullOrEmpty(response.Result?.Error?.ErrorDescription?.Trim() ?? ""))
                    {
                        throw new MOBUnitedException(response.Result.Error.ErrorDescription);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerBaseProfileErrorMessage").ToString();
                        if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting").ToString()))
                        {
                            exceptionMessage = exceptionMessage + " response.success is false - at DAL UpdatePassRiderBaseInfo(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerBaseProfileErrorMessage").ToString();
                if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting").ToString()))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  UpdatePassRiderBaseInfo(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion
            return updateTravelerSuccess;
        }
        private EmployeeDetailRequest GetUpdatePassriderContactInfoRequest(MOBUpdateTravelerRequest request, string employeeID, string eresTransactionID)
        {
            //var passRider = employeeJA?.PassRiders?.First(r => r?.DependantID?.Equals(request?.Traveler?.EmployeeId ?? "") ?? false);
            #region Build insert Traverl Request

            EmployeeDetailRequest employeeDetailRequest = new EmployeeDetailRequest();


            employeeDetailRequest.EmployeeId = new AesEncryptAndDecrypt().Encrypt(employeeID);
            employeeDetailRequest.DependantId = request.Traveler.Key;
            employeeDetailRequest.ContactInformations = new DayOfContactInformation();
            if ((request.Traveler?.Phones?.Count ?? 0) > 0)
            {
                employeeDetailRequest.ContactInformations.CountryCode = request.Traveler.Phones[0].CountryCode;
                employeeDetailRequest.ContactInformations.DialCode = request.Traveler.Phones[0].CountryPhoneNumber?.TrimStart('+');
                employeeDetailRequest.ContactInformations.PhoneNumber = request.Traveler.Phones[0].AreaNumber + request.Traveler.Phones[0].PhoneNumber;
            }
            if ((request.Traveler.EmailAddresses?.Count ?? 0) > 0)
            {
                employeeDetailRequest.ContactInformations.Email = request.Traveler.EmailAddresses[0]?.EmailAddress;
            }
            employeeDetailRequest.TransactionId = eresTransactionID;
            //TransactionId ?
            employeeDetailRequest.IsPassriderInfo = true;


            return employeeDetailRequest;


            #endregion
        }

        public bool UpdatePassRiderContactInfo(MOBUpdateTravelerRequest request, string employeeID, string eresTransactionID)
        {
            bool updateTravelerContactSuccess = false;
            #region

            EmployeeDetailRequest employeeDetailRequest = GetUpdatePassriderContactInfoRequest(request, employeeID, eresTransactionID);
            //travelDocumentRequest.TransactionID = employeeJA.


            string jsonRequest = JsonConvert.SerializeObject(employeeDetailRequest);

            string path = "/Employee/Profile/Save";
            //string url = string.Format("{0}/UpdateTravelerBase", _configuration.GetValue<string>["ServiceEndPointBaseUrl - CSLProfile"]);


            var response = _baseEmployeeRes.UpdateTravelerBase<EmployeeDetailResponse>(request.Token, jsonRequest, request.Token, path);


            if (response.Result != null)
            {
                if (response != null && response.Result.IsSuccess)
                {
                    updateTravelerContactSuccess = true;
                }
                else
                {
                    if (string.IsNullOrEmpty(response.Result.Error?.ErrorDescription?.Trim() ?? ""))
                    {
                        throw new MOBUnitedException(response.Result.Error.ErrorDescription);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerPhoneOrEmailToProfileErrorMessage").ToString();
                        if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting").ToString()))
                        {
                            exceptionMessage = exceptionMessage + " response.success is false - at DAL UpdatePassRiderContactInfo(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerPhoneOrEmailToProfileErrorMessage").ToString();
                if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting").ToString()))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  UpdatePassRiderContactInfo(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion
            return updateTravelerContactSuccess;
        }

        private SSRInfo GetUpdatePassriderRequest(MOBUpdateTravelerRequest request, string employeeID)
        {
            if (request.UpdateTravelerBasiInfo)
            {
                //var passRider = employeeJA?.PassRiders?.First(r => r?.DependantID?.Equals(request?.Traveler?.EmployeeId ?? "") ?? false);
                #region Build insert Traverl Request

                SSRInfo passRiderUpdateRequest = new SSRInfo()
                {

                    BirthDate = request.Traveler.BirthDate,
                    //DateCreated
                    //Description
                    //EmployeeID = new AesEncryptAndDecrypt().Encrypt(employeeJA?.Employee?.EmployeeID),
                    EmployeeID = employeeID,
                    FirstName = request.Traveler.FirstName,
                    LastName = request.Traveler.LastName,
                    Gender = request.Traveler.GenderCode,
                    //ID = passRider?.SSRs?[0]?.TravelDocument?.Id ?? 0,
                    //Description = passRider?.SSRs?[0]?.TravelDocument?.Description,
                    KnownTraveler = request.Traveler.KnownTravelerNumber,
                    MiddleName = request.Traveler.MiddleName,
                    NameSuffix = request.Traveler.Suffix,
                    PassportIssuedCountry = request.Traveler.Nationality,
                    Country = new MOBEmpCountry() { Code = request.Traveler.CountryOfResidence },
                    PaxID = request.Traveler.Key,
                    Redress = request.Traveler.RedressNumber,
                    //State=
                    //status ?
                    //TravelDocument = passRider?.SSRs?[0]?.TravelDocument,
                    //TransactionId =
                };
                return passRiderUpdateRequest;

            }

            #endregion
            return null;
        }


        public async Task<MOBUpdateTravelerInfoResponse> ManageTravelerInfo(MOBUpdateTravelerInfoRequest request, string token)
        {
            if (request == null)
                throw new MOBUnitedException("UpdateTravelerInfo request cannot be null.");

            MOBUpdateTravelerInfoResponse mobresponse = new MOBUpdateTravelerInfoResponse();

            var jsonTravelerInfoSSRRequest = string.Empty;

            MOBPNRPassenger travelerInfo = request.TravelersInfo[0];
            var travelerSession = await _sessionHelperService.GetSession<MOBPNR>(request.SessionId, new MOBPNR().ObjectName, new List<String> { request.SessionId, new MOBPNR().ObjectName }).ConfigureAwait(false);
            var deleteSSRItems = new List<string>();

            var selectedTravelerSession
                   = travelerSession.Passengers.FirstOrDefault
                   (tvlr => string.Equals(travelerInfo.SHARESPosition, tvlr.SHARESPosition, StringComparison.OrdinalIgnoreCase) == true);


            //Manage FF/MP Number
            List<United.Service.Presentation.ReservationModel.Traveler> travelerInfoMPRequest = CreateTravelerInfoMPFFRequest(request);
            string jsonRequest = JsonSerializer.Serialize<List<United.Service.Presentation.ReservationModel.Traveler>>(travelerInfoMPRequest);

            await UpdateTravlerMPorFFInfo(mobresponse, request, token, jsonRequest, travelerInfo);

            if (selectedTravelerSession.Contact != null)
            {
                if (selectedTravelerSession.Contact.PhoneNumbers != null && selectedTravelerSession.Contact.PhoneNumbers.Any())
                {
                    selectedTravelerSession.Contact.PhoneNumbers.ForEach(pitem =>
                    {
                        deleteSSRItems.Add(pitem.Key);
                    });
                }
                if (selectedTravelerSession.Contact.Emails != null && selectedTravelerSession.Contact.Emails.Any())
                {
                    selectedTravelerSession.Contact.Emails.ForEach(eitem =>
                    {
                        deleteSSRItems.Add(eitem.Key);
                    });
                }
            }

            if (!string.IsNullOrEmpty(selectedTravelerSession.KnownTravelerNumber))
                deleteSSRItems.Add(selectedTravelerSession.KTNDisplaySequence);

            if (!string.IsNullOrEmpty(selectedTravelerSession.REDRESSDisplaySequence))
                deleteSSRItems.Add(selectedTravelerSession.REDRESSDisplaySequence);

            if (!string.IsNullOrEmpty(selectedTravelerSession.CTNDisplaySequence))
                deleteSSRItems.Add(selectedTravelerSession.CTNDisplaySequence);

            if (!string.IsNullOrEmpty(selectedTravelerSession.SSRDisplaySequence))
            {
                selectedTravelerSession.SSRDisplaySequence.Split('|').EachFor(x =>
                {
                    deleteSSRItems.Add(x);
                });
            }

            if (deleteSSRItems.Any())
                await DeleteSelectedSSRItemNew(mobresponse, request, token, deleteSSRItems);

            //Manage SSR items
            List<United.Service.Presentation.CommonModel.Service> travelerInfoSSRRequest = CreateTravelerInfoSSRRequest(request, travelerSession.Segments);
            jsonTravelerInfoSSRRequest = JsonSerializer.Serialize<List<United.Service.Presentation.CommonModel.Service>>(travelerInfoSSRRequest);

            string jsonTravelerInfoSSRCSLResponse = string.Empty;

            if (travelerInfoSSRRequest.Any())
                jsonTravelerInfoSSRCSLResponse = await UpdateTravelerSSRInfo(mobresponse, request, token, jsonTravelerInfoSSRRequest);

            if (!string.IsNullOrEmpty(jsonTravelerInfoSSRCSLResponse))
            {
                United.Service.Presentation.ReservationModel.Reservation
                  jsonTravelerInfoSSRResponse = JsonSerializer.Deserialize<United.Service.Presentation.ReservationModel.Reservation>(jsonTravelerInfoSSRCSLResponse);

                if (jsonTravelerInfoSSRResponse != null && jsonTravelerInfoSSRResponse.Characteristic.Any())
                {
                    await SaveLatestSSRInformation(travelerSession, request, jsonTravelerInfoSSRResponse);
                    mobresponse.MealAccommodationAdvisory = travelerSession.MealAccommodationAdvisory;
                    mobresponse.MealAccommodationAdvisoryHeader = travelerSession.MealAccommodationAdvisoryHeader;
                }
                else
                    mobresponse.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            if (string.IsNullOrEmpty(mobresponse.MealAccommodationAdvisory) && string.IsNullOrEmpty(mobresponse.MealAccommodationAdvisoryHeader))
            {
                var otherPaxPreSelected = travelerSession.Passengers
                    .Where(x => !string.Equals(x.SHARESPosition, selectedTravelerSession.SHARESPosition, StringComparison.OrdinalIgnoreCase))
                    .Where(y => y.SelectedSpecialNeeds.Count > 0);

                if (otherPaxPreSelected != null && otherPaxPreSelected.Any())
                {
                    GetSpecialNeedsAdvisoryMessage(request.Application.Id, request.Application.Version.Major, ref travelerSession);
                    mobresponse.MealAccommodationAdvisory = travelerSession.MealAccommodationAdvisory;
                    mobresponse.MealAccommodationAdvisoryHeader = travelerSession.MealAccommodationAdvisoryHeader;
                }
            }

            return mobresponse;
        }

        public List<United.Service.Presentation.ReservationModel.Traveler> CreateTravelerInfoMPFFRequest(MOBUpdateTravelerInfoRequest request)
        {
            List<United.Service.Presentation.ReservationModel.Traveler>
                _travelerMPFFInfo = new List<United.Service.Presentation.ReservationModel.Traveler>();


            request.TravelersInfo.ForEach(traveler =>
            {
                Service.Presentation.PersonModel.Person person;
                LoyaltyProgramProfile loyaltyProgramProfile;

                if (traveler.PassengerName != null)
                {
                    person = new Service.Presentation.PersonModel.Person
                    {
                        GivenName = (!string.IsNullOrEmpty(traveler.SharesGivenName)
                    && !string.Equals(traveler.SharesGivenName, traveler.PassengerName.First, StringComparison.OrdinalIgnoreCase))
                    ? traveler.SharesGivenName
                    : traveler.PassengerName.First,

                        Surname = traveler.PassengerName.Last,
                        InfantIndicator = "FALSE",
                        Key = traveler.SHARESPosition
                    };
                }
                else
                {
                    person = null;
                }


                if (traveler.MileagePlus != null && string.IsNullOrEmpty(traveler.MileagePlus.MileagePlusId) == false)
                {
                    loyaltyProgramProfile = new LoyaltyProgramProfile
                    {
                        LoyaltyProgramCarrierCode = "UA",
                        LoyaltyProgramMemberID = traveler.MileagePlus.MileagePlusId
                    };
                }
                else if (traveler.OaRewardPrograms != null)
                {
                    loyaltyProgramProfile = new LoyaltyProgramProfile
                    {
                        LoyaltyProgramCarrierCode = traveler.OaRewardPrograms[0].VendorCode,
                        LoyaltyProgramMemberID = traveler.OaRewardPrograms[0].ProgramMemberId
                    };
                }
                else
                {
                    loyaltyProgramProfile = null;
                }

                _travelerMPFFInfo.Add(new United.Service.Presentation.ReservationModel.Traveler
                {
                    Person = person,
                    LoyaltyProgramProfile = loyaltyProgramProfile
                });

            });

            return _travelerMPFFInfo;
        }


        public async Task<MOBUpdateTravelerInfoResponse> DeleteSelectedSSRItemNew
         (MOBUpdateTravelerInfoResponse response, MOBUpdateTravelerInfoRequest request, string Token, List<string> deleteSSRItems)
        {
            var jsonDeleteSSRResponse = string.Empty;

            List<United.Service.Presentation.CommonModel.Service> travelerDeleteSSRRequest = DeleteTravelerSSRRequestNew(deleteSSRItems);
            string jsonDeleteSSRRequest = JsonSerializer.Serialize<List<United.Service.Presentation.CommonModel.Service>>(travelerDeleteSSRRequest);

            try
            {
                string path = string.Format("{0}/ManageSSRs", request.RecordLocator);

                jsonDeleteSSRResponse = await _pNRRetrievalService.PNRRetrieval(Token, jsonDeleteSSRRequest, request.SessionId, path);

                if (!string.IsNullOrEmpty(jsonDeleteSSRResponse))
                {

                    _logger.LogInformation("DeleteTravelerInfo-SSR {Response} and {SessionId}", jsonDeleteSSRResponse, request.SessionId);

                    United.Service.Presentation.ReservationModel.Reservation
                       msgDeleteSSRResponse = JsonSerializer.Deserialize<United.Service.Presentation.ReservationModel.Reservation>(jsonDeleteSSRResponse);

                    if (msgDeleteSSRResponse != null && !msgDeleteSSRResponse.Characteristic.Any())
                    {
                        response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                    }
                }
            }
            catch (System.Net.WebException wex)
            {
                if (wex.Response != null)
                {
                    var errorResponse = new StreamReader(wex.Response.GetResponseStream()).ReadToEnd();

                    _logger.LogError("DeleteTravelerInfo-SSR {Exception} and {SessionId}", errorResponse, request.SessionId);

                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }

            return response;
        }

        public bool IsEnableCanadianTravelNumber(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableCanadianTravelNumber") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_CanadianTravelNumber_AppVersion"), _configuration.GetValue<string>("IPhone_CanadianTravelNumber_AppVersion"));
        }

        public List<United.Service.Presentation.CommonModel.Service> DeleteTravelerSSRRequestNew(List<string> deleteSSRItems)
        {

            List<United.Service.Presentation.CommonModel.Service> _deleteSSRRequests = new List<Service.Presentation.CommonModel.Service>();
            United.Service.Presentation.CommonModel.Service _deleteSSRRequest = new United.Service.Presentation.CommonModel.Service();

            deleteSSRItems.ForEach(item =>
            {
                int opValue;
                bool hasValidDisplaySequence = Int32.TryParse(item, out opValue);
                if (hasValidDisplaySequence)
                {
                    _deleteSSRRequest = new United.Service.Presentation.CommonModel.Service
                    {
                        DisplaySequence = opValue,
                        Key = "SSR",
                        Status = new Status { Code = "DELETE" }
                    };
                    _deleteSSRRequests.Add(_deleteSSRRequest);
                }
            });
            return _deleteSSRRequests;
        }

        public List<United.Service.Presentation.CommonModel.Service> CreateTravelerInfoSSRRequest(MOBUpdateTravelerInfoRequest request, List<MOBPNRSegment> segments)
        {
            //Constants
            var emailReqCode = "CTCE";
            var phoneReqCode = "CTCM";
            var ktnCtnRedressReqCode = "DOCO";
            var ssrReqKey = "SSR";
            var ssrDOCOStatusDescription = "NN";
            var ssrStatusCode = "ADD";
            var ssrCarrierCode = (!_configuration.GetValue<bool>("EnableIgnoreSSRCarrierCode")) ? "UA" : string.Empty; ; // Remove the 'UA' carrier code according to MOBILE-28064. Now CSL returning by default CTCEYY
            var emailInputPattern = "/{0}//{1}";
            var phoneInputPattern = "/{0}";
            var ssrCTEStatusDescription
                = _configuration.GetValue<String>("CTEStatusDescription");

            List<United.Service.Presentation.CommonModel.Service> _ssrRequests = new List<Service.Presentation.CommonModel.Service>();
            United.Service.Presentation.CommonModel.Service _ssrRequest = new United.Service.Presentation.CommonModel.Service();

            List<Tuple<int, string, string>> eligibleSegmentMeals = new List<Tuple<int, string, string>>();
            Collection<int> specialAccomodation = new Collection<int>();
            segments.ForEach(f =>
            {

                if (!string.IsNullOrEmpty(f.SSRMeals))
                {
                    eligibleSegmentMeals.Add(Tuple.Create(f.SegmentNumber, f.SSRMeals, f.ActionCode));
                }
                specialAccomodation.Add(f.SegmentNumber);
            });


            request.TravelersInfo.ForEach(traveler =>
            {
                if (GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, "iPhonePNRTravelerContactVersion", "AndroidPNRTravelerContactVersion", "", "", true, _configuration))
                {
                    if (traveler.Contact != null)
                    {
                        if (traveler.Contact.Emails != null && traveler.Contact.Emails.Any())
                        {
                            var emailName = string.Empty;
                            var emailDomain = string.Empty;
                            GetEmailAddressFrmPNRContact(traveler.Contact.Emails, ref emailName, ref emailDomain);

                            if (!string.IsNullOrEmpty(emailName) && !string.IsNullOrEmpty(emailDomain))
                            {
                                _ssrRequests.Add(SetSSRMobileRequesttoCSLRequest(ssrCarrierCode, emailReqCode, ssrReqKey, traveler.SHARESPosition,
                                    ssrCTEStatusDescription, ssrStatusCode, string.Format(emailInputPattern, emailName, emailDomain)));
                            }
                        }

                        if (traveler.Contact.PhoneNumbers != null && traveler.Contact.PhoneNumbers.Any())
                        {
                            var countryphonenumber = string.Empty;
                            var phonenumber = string.Empty;
                            GetPhoneNumberFrmPNRContact(traveler.Contact.PhoneNumbers, ref countryphonenumber, ref phonenumber);

                            if (!string.IsNullOrEmpty(phonenumber))
                            {
                                if (!string.IsNullOrEmpty(countryphonenumber))
                                {
                                    phonenumber = string.Concat(countryphonenumber.Replace(" ", ""), phonenumber);
                                }
                                _ssrRequests.Add(SetSSRMobileRequesttoCSLRequest(ssrCarrierCode, phoneReqCode, ssrReqKey, traveler.SHARESPosition,
                                    ssrCTEStatusDescription, ssrStatusCode, string.Format(phoneInputPattern, phonenumber)));
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(traveler.KnownTravelerNumber))
                {
                    _ssrRequest = new United.Service.Presentation.CommonModel.Service
                    {
                        CarrierCode = new Service.Presentation.CommonModel.VendorModel.Vendor { Name = ssrCarrierCode },
                        Code = ktnCtnRedressReqCode,
                        Description = string.Format("//K/{0}///US", traveler.KnownTravelerNumber),
                        Key = ssrReqKey,
                        TravelerNameIndex = traveler.SHARESPosition,
                        NumberInParty = 1,
                        Status = new Status { Description = ssrDOCOStatusDescription, Code = ssrStatusCode }
                    };
                    _ssrRequests.Add(_ssrRequest);
                }

                if (!string.IsNullOrEmpty(traveler.RedressNumber))
                {
                    _ssrRequest = new United.Service.Presentation.CommonModel.Service
                    {
                        CarrierCode = new Service.Presentation.CommonModel.VendorModel.Vendor { Name = ssrCarrierCode },
                        Code = ktnCtnRedressReqCode,
                        Description = string.Format("//R/{0}///US", traveler.RedressNumber),
                        Key = ssrReqKey,
                        TravelerNameIndex = traveler.SHARESPosition,
                        NumberInParty = 1,
                        Status = new Status { Description = ssrDOCOStatusDescription, Code = ssrStatusCode }
                    };
                    _ssrRequests.Add(_ssrRequest);
                }

                //Birkan CTN
                if (IsEnableCanadianTravelNumber(request.Application.Id, request.Application.Version.Major))
                {

                    if (!string.IsNullOrEmpty(traveler.CanadianTravelNumber))
                    {
                        _ssrRequest = new United.Service.Presentation.CommonModel.Service
                        {
                            CarrierCode = new Service.Presentation.CommonModel.VendorModel.Vendor { Name = ssrCarrierCode },
                            Code = ktnCtnRedressReqCode,
                            Description = string.Format("//R/{0}///CA", traveler.CanadianTravelNumber),
                            Key = ssrReqKey,
                            TravelerNameIndex = traveler.SHARESPosition,
                            NumberInParty = 1,
                            Status = new Status { Description = ssrDOCOStatusDescription, Code = ssrStatusCode }
                        };
                        _ssrRequests.Add(_ssrRequest);
                    }

                }

                if (traveler.SelectedSpecialNeeds != null && traveler.SelectedSpecialNeeds.Any())
                {

                    traveler.SelectedSpecialNeeds.ForEach(item =>
                    {
                        string statusDesc = string.Empty;

                        Collection<int> selectedFlightSegment = new Collection<int>();
                        eligibleSegmentMeals.EachFor(sfs =>
                        {
                            if (sfs.Item2.IndexOf(item.Code) > -1)
                            {
                                selectedFlightSegment.Add(sfs.Item1);
                                statusDesc = sfs.Item3;
                            }
                        });
                    });



                    traveler.SelectedSpecialNeeds.ForEach(ssritem =>
                    {
                        if (ssritem.SubOptions != null && ssritem.SubOptions.Any())
                        {
                            ssritem.SubOptions.ForEach(subItem =>
                            {
                                _ssrRequest = new United.Service.Presentation.CommonModel.Service
                                {
                                    CarrierCode = new Service.Presentation.CommonModel.VendorModel.Vendor { Name = ssrCarrierCode },
                                    Code = subItem.Code,
                                    Description = ssritem.DisplayDescription,
                                    Key = ssrReqKey,
                                    TravelerNameIndex = traveler.SHARESPosition,
                                    SegmentNumber = specialAccomodation,
                                    NumberInParty = 1,
                                    Status = new Status { Description = ssrDOCOStatusDescription, Code = ssrStatusCode }
                                };
                                _ssrRequests.Add(_ssrRequest);
                            });
                        }
                        else
                        {
                            _ssrRequest = new United.Service.Presentation.CommonModel.Service
                            {
                                CarrierCode = new Service.Presentation.CommonModel.VendorModel.Vendor { Name = ssrCarrierCode },
                                Code = ssritem.Code,
                                //Description = ssritem.DisplayDescription,
                                Key = ssrReqKey,
                                TravelerNameIndex = traveler.SHARESPosition,
                                SegmentNumber = specialAccomodation,
                                NumberInParty = 1,
                                Status = new Status { Description = ssrDOCOStatusDescription, Code = ssrStatusCode }
                            };
                            _ssrRequests.Add(_ssrRequest);

                        }
                    });
                }
            });

            return _ssrRequests;
        }

        private static void GetPhoneNumberFrmPNRContact(List<MOBCPPhone> phonenumbers, ref string countryphonenumber, ref string phonenumber)
        {
            foreach (var item in phonenumbers)
            {
                if (item != null && !string.IsNullOrEmpty(item.PhoneNumber))
                {
                    countryphonenumber = item.CountryPhoneNumber;
                    phonenumber = item.PhoneNumber;
                    return;
                }
            }
        }


        private static void GetEmailAddressFrmPNRContact(List<MOBEmail> emails, ref string emailName, ref string emailDomain)
        {
            foreach (var item in emails)
            {
                if (item != null && !string.IsNullOrEmpty(item.EmailAddress))
                {
                    string[] emailSplit = item.EmailAddress.Split('@');
                    if (emailSplit.Length == 2 && !string.IsNullOrEmpty(emailSplit[0]))
                    {
                        emailName = emailSplit[0];
                        emailDomain = emailSplit[1];
                        return;
                    }
                }
            }
        }

        private static United.Service.Presentation.CommonModel.Service SetSSRMobileRequesttoCSLRequest
            (string ssrCarrierCode, string ssrReqCode, string ssrReqKey, string sharesPos,
            string ssrStatusDescription, string ssrStatusCode, string sharesInputPattern)
        {
            United.Service.Presentation.CommonModel.Service _ssrRequest = new United.Service.Presentation.CommonModel.Service
            {
                CarrierCode = new Service.Presentation.CommonModel.VendorModel.Vendor { Name = ssrCarrierCode },
                Code = ssrReqCode,
                Description = sharesInputPattern,
                Key = ssrReqKey,
                TravelerNameIndex = sharesPos,
                NumberInParty = 1,
                Status = new Status { Description = ssrStatusDescription, Code = ssrStatusCode }
            };

            return _ssrRequest;
        }

        private async Task<string> UpdateTravelerSSRInfo
           (MOBUpdateTravelerInfoResponse response, MOBUpdateTravelerInfoRequest request, string token, string jsonTravelerInfoSSRRequest)
        {
            var urlSSRRequest = string.Empty;
            var jsonTravelerInfoSSRResponse = string.Empty;

            string path = string.Format("{0}/ManageSSRs", request.RecordLocator);

            try
            {
                jsonTravelerInfoSSRResponse = await _pNRRetrievalService.PNRRetrieval(token, jsonTravelerInfoSSRRequest, request.SessionId, path);

                _logger.LogInformation("UpdateTravelerInfo-SSR {Response} and {SessionId}", jsonTravelerInfoSSRResponse, request.SessionId);

            }
            catch (System.Net.WebException wex)
            {
                if (wex.Response != null)
                {
                    var errorResponse = new StreamReader(wex.Response.GetResponseStream()).ReadToEnd();

                    _logger.LogError("UpdateTravelerInfo-SSR {Exception} and {SessionId}", errorResponse, request.SessionId);

                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }

            return jsonTravelerInfoSSRResponse;
        }

        private async System.Threading.Tasks.Task SaveLatestSSRInformation
            (MOBPNR travelerSession, MOBUpdateTravelerInfoRequest request, United.Service.Presentation.ReservationModel.Reservation cslResponse)
        {
            string ssrKTNumber = string.Empty;
            string ssrREDRESSNumber = string.Empty;
            string ssrCTNumber = string.Empty;
            string ssrPhoneNumber = string.Empty;
            string ssrEmailAddress = string.Empty;
            string sharesPosition = string.Empty;
            List<string> specialRequestsItem = new List<string>();
            List<string> specialReqSavedItem;
            Boolean isDisplayAdvisoryMessage = false;

            try
            {
                if (request.TravelersInfo != null & request.TravelersInfo.Any())
                {
                    request.TravelersInfo.ForEach(traveler =>
                    {
                        ssrKTNumber = traveler.KnownTravelerNumber;
                        ssrREDRESSNumber = traveler.RedressNumber;
                        ssrCTNumber = traveler.CanadianTravelNumber;
                        sharesPosition = traveler.SHARESPosition;

                        if (traveler.SelectedSpecialNeeds != null)
                        {
                            specialRequestsItem = traveler.SelectedSpecialNeeds.Where(x => (((!string.IsNullOrEmpty(x.Code)) && (x.SubOptions == null))))
                               .Select(y => Convert.ToString(y.Code)).ToList();

                            traveler.SelectedSpecialNeeds.ForEach(x =>
                            {
                                if (x.SubOptions != null && x.SubOptions.Any())
                                {
                                    x.SubOptions.ForEach(y =>
                                    {
                                        if (!string.IsNullOrEmpty(y.Code))
                                        {
                                            specialRequestsItem.Add(y.Code);
                                        }
                                    });
                                }
                            });
                        }

                        specialReqSavedItem = new List<string>();

                        if (cslResponse != null && cslResponse.Services != null && cslResponse.Services.Any())
                        {
                            specialReqSavedItem = cslResponse.Services.Where(x =>
                            (string.Equals(sharesPosition, x.TravelerNameIndex, StringComparison.OrdinalIgnoreCase)
                            && !string.IsNullOrEmpty(x.Code) && specialRequestsItem.Contains(x.Code)))
                                .Select(y => Convert.ToString(y.DisplaySequence)).ToList();

                            if (specialReqSavedItem != null && specialReqSavedItem.Any())
                                isDisplayAdvisoryMessage = true;

                            if (!string.IsNullOrEmpty(ssrKTNumber))
                            {
                                var ktnDispSeqNo = cslResponse.Services.Where(x =>
                                (string.Equals(sharesPosition, x.TravelerNameIndex, StringComparison.OrdinalIgnoreCase)
                                && !string.IsNullOrEmpty(x.Description) && x.Description.Contains(ssrKTNumber)))
                                .Select(y => Convert.ToString(y.DisplaySequence)).ToList();
                                specialReqSavedItem.AddRange(ktnDispSeqNo);
                            }

                            if (!string.IsNullOrEmpty(ssrREDRESSNumber))
                            {
                                var redrDispSeqNo = cslResponse.Services.Where(x =>
                                (string.Equals(sharesPosition, x.TravelerNameIndex, StringComparison.OrdinalIgnoreCase)
                                && !string.IsNullOrEmpty(x.Description) && x.Description.Contains(ssrREDRESSNumber)))
                             .Select(y => Convert.ToString(y.DisplaySequence)).ToList();
                                specialReqSavedItem.AddRange(redrDispSeqNo);
                            }

                            //Birkan CTN
                            if (IsEnableCanadianTravelNumber(request.Application.Id, request.Application.Version.Major))
                            {

                                if (!string.IsNullOrEmpty(ssrCTNumber))
                                {
                                    var ctnDispSeqNo = cslResponse.Services.Where(x =>
                                    (string.Equals(sharesPosition, x.TravelerNameIndex, StringComparison.OrdinalIgnoreCase)
                                    && !string.IsNullOrEmpty(x.Description) && x.Description.Contains(ssrCTNumber)))
                                 .Select(y => Convert.ToString(y.DisplaySequence)).ToList();
                                    specialReqSavedItem.AddRange(ctnDispSeqNo);
                                }
                            }

                            if (GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, "iPhonePNRTravelerContactVersion", "AndroidPNRTravelerContactVersion", "", "", true, _configuration))
                            {
                                var countryphonenumber = string.Empty;
                                var phonenumber = string.Empty;
                                GetPhoneNumberFrmPNRContact(traveler.Contact.PhoneNumbers, ref countryphonenumber, ref phonenumber);
                                if (!string.IsNullOrEmpty(phonenumber))
                                {
                                    var phoneDispSeqNo = cslResponse.Services.Where(x =>
                                    (string.Equals(sharesPosition, x.TravelerNameIndex, StringComparison.OrdinalIgnoreCase)
                                    && !string.IsNullOrEmpty(x.Description) && x.Description.Contains(phonenumber)))
                                 .Select(y => Convert.ToString(y.DisplaySequence)).ToList();
                                    specialReqSavedItem.AddRange(phoneDispSeqNo);
                                }

                                var emailName = string.Empty;
                                var emailDomain = string.Empty;
                                GetEmailAddressFrmPNRContact(traveler.Contact.Emails, ref emailName, ref emailDomain);
                                if (!string.IsNullOrEmpty(emailName))
                                {
                                    var emailDispSeqNo = cslResponse.Services.Where(x =>
                                    (string.Equals(sharesPosition, x.TravelerNameIndex, StringComparison.OrdinalIgnoreCase)
                                    && !string.IsNullOrEmpty(x.Description) && x.Description.Contains(emailName.ToUpper())))
                                  .Select(y => Convert.ToString(y.DisplaySequence)).ToList();
                                    specialReqSavedItem.AddRange(emailDispSeqNo);
                                }
                            }
                        }

                        if (specialReqSavedItem != null && specialReqSavedItem.Any())
                            travelerSession.Passengers.Where(x => string.Equals(x.SHARESPosition, sharesPosition, StringComparison.OrdinalIgnoreCase))
                                .FirstOrDefault().SSRDisplaySequence = string.Join("|", specialReqSavedItem);
                    });

                    if (isDisplayAdvisoryMessage)
                        GetSpecialNeedsAdvisoryMessage(request.Application.Id, request.Application.Version.Major, ref travelerSession);
                    else
                    {
                        travelerSession.MealAccommodationAdvisory = string.Empty;
                        travelerSession.MealAccommodationAdvisoryHeader = string.Empty;
                    }

                    await _sessionHelperService.SaveSession<MOBPNR>(travelerSession, request.SessionId, new List<String> { request.SessionId, new MOBPNR().ObjectName }, new MOBPNR().ObjectName).ConfigureAwait(false);
                }
            }
            catch { }
        }

        private void GetSpecialNeedsAdvisoryMessage(int applicationId, string appVersion, ref MOBPNR pnr)
        {
            if (GeneralHelper.IsApplicationVersionGreater
                            (applicationId, appVersion, "AndroidEnableMgnResUpdateSpecialNeeds", "iPhoneEnableMgnResUpdateSpecialNeeds", string.Empty, string.Empty, true, _configuration))
            {
                try
                {
                    if (pnr.Segments != null && pnr.Segments.Any())
                    {
                        var getAllCarrierNames = pnr.Segments.Where(x => (x.OperationoperatingCarrier != null))
                            .Where(n => ((!string.Equals(n.OperationoperatingCarrier.Code, "UA", StringComparison.OrdinalIgnoreCase))
                                && (n.OperationoperatingCarrier.Name.IndexOf("United Express", StringComparison.OrdinalIgnoreCase) == -1)))
                            .GroupBy(y => y.OperationoperatingCarrier.Code).Select(z => z.First())
                            .Select(l => l.OperationoperatingCarrier.Name).ToList();

                        string strCarrierNames = ConvertListToString(getAllCarrierNames);

                        if (!string.IsNullOrEmpty(strCarrierNames))
                        {
                            pnr.MealAccommodationAdvisory = string.Format(_configuration.GetValue<string>("SSR_OA_MessageNew"), strCarrierNames);
                            pnr.MealAccommodationAdvisoryHeader = "Action required";
                        }
                    }
                }
                catch (Exception ex) { }
            }
        }

        public string ConvertListToString(List<string> inputList)
        {
            try
            {
                if (inputList != null && inputList.Any())
                    inputList.RemoveAll(x => (string.IsNullOrWhiteSpace(x) || string.IsNullOrEmpty(x)));

                if (inputList != null && inputList.Any())
                {
                    if (inputList.Count == 1) return inputList[0];
                    else if (inputList.Count == 2)
                        return string.Join(", ", inputList.Take(inputList.Count() - 1)) + " and " + inputList.Last();
                    else
                        return string.Join(", ", inputList.Take(inputList.Count() - 1)) + ", and " + inputList.Last();
                }
                else
                    return string.Empty;
            }
            catch (Exception ex) { return string.Empty; }
        }
        public async Task<MOBUpdateTravelerInfoResponse> UpdateTravelerInfo(MOBUpdateTravelerInfoRequest request, string Token)
        {
            if (request == null)
                throw new MOBUnitedException("UpdateTravelerInfo request cannot be null.");

            MOBUpdateTravelerInfoResponse response = new MOBUpdateTravelerInfoResponse();

            #region//****Get Call Duration Code - Venkat 03/17/2015*******

            Stopwatch cslStopWatch;
            cslStopWatch = new Stopwatch();
            cslStopWatch.Reset();
            cslStopWatch.Start();
            #endregion            
            MOBPNRPassenger travelerInfo = request.TravelersInfo[0]; //Fetching the first item from traveler Info

            try
            {
                var travelerSession = await _sessionHelperService.GetSession<MOBPNR>(request.SessionId, new MOBPNR().ObjectName, new List<string> { request.SessionId, new MOBPNR().ObjectName });
                //var travelerSession = FilePersist.Load<United.Persist.Definition.Reshopping.EligibilityResponse>
                //(request.SessionId, "United.Persist.Definition.Reshopping.EligibilityResponse");

                var travelerInfoSession
                    = travelerSession.Passengers.FirstOrDefault
                    (tvlr => string.Equals(travelerInfo.SHARESPosition, tvlr.SHARESPosition, StringComparison.OrdinalIgnoreCase) == true);

                var deleteSSRItems = new List<string>();

                //travelerInfoSession.SelectedSpecialNeeds.ForEach(item => {
                //    deleteSSRItems.Add(item.DisplaySequence);
                //});                

                var reqKTN = travelerInfo.KnownTravelerNumber;
                var cacheKTN = travelerInfoSession.KnownTravelerNumber;

                var reqREDRESS = travelerInfo.RedressNumber;
                var cacheREDRESS = travelerInfoSession.RedressNumber;


                bool isDeleteKtn = (string.IsNullOrEmpty(reqKTN) && !string.IsNullOrEmpty(cacheKTN));

                bool isDeleteRedress = (string.IsNullOrEmpty(reqREDRESS) && !string.IsNullOrEmpty(cacheREDRESS));

                bool isUpdateKtn = (!string.IsNullOrEmpty(reqKTN) && !string.IsNullOrEmpty(cacheKTN)
                    && !string.Equals(reqKTN, cacheKTN, StringComparison.OrdinalIgnoreCase));

                bool isAddKtn = (string.IsNullOrEmpty(cacheKTN) && !string.IsNullOrEmpty(reqKTN));


                bool isUpdateRedress = (!string.IsNullOrEmpty(reqREDRESS) && !string.IsNullOrEmpty(cacheREDRESS)
                   && !string.Equals(reqREDRESS, cacheREDRESS, StringComparison.OrdinalIgnoreCase));

                bool isAddRedress = (string.IsNullOrEmpty(cacheREDRESS) && !string.IsNullOrEmpty(reqREDRESS));

                //Check if no change 
                if (!isUpdateKtn && !isAddKtn)
                    travelerInfo.KnownTravelerNumber = string.Empty;
                if (!isUpdateRedress && !isAddRedress)
                    travelerInfo.RedressNumber = string.Empty;
                //

                List<United.Service.Presentation.ReservationModel.Traveler> travelerInfoMPKTNRequest = UpdateTravelerInfoMPRedressKTNRequest(request);
                string jsonRequest = JsonSerializer.Serialize<List<United.Service.Presentation.ReservationModel.Traveler>>(travelerInfoMPKTNRequest);

                //MPNumber CSL Call
                await UpdateTravlerMPorFFInfo(response, request, Token, jsonRequest, travelerInfo);

                if (isDeleteKtn || isUpdateKtn)
                    deleteSSRItems.Add(travelerInfoSession.KTNDisplaySequence);

                if (isDeleteRedress || isUpdateRedress)
                    deleteSSRItems.Add(travelerInfoSession.REDRESSDisplaySequence);

                if (!string.IsNullOrEmpty(travelerInfoSession.SSRDisplaySequence))
                {
                    travelerInfoSession.SSRDisplaySequence.Split('|').EachFor(x =>
                    {
                        deleteSSRItems.Add(x);
                    });
                }

                if (deleteSSRItems.Any())
                    await DeleteSelectedSSRItem(response, request, Token, deleteSSRItems);

                //KTN & REDRESS CSL Call  
                await UpdateTravlerKTNRedress(response, request, Token, jsonRequest, travelerInfo);

                //Special Request Call
                await UpdateTravlerSSRInfo(response, request, Token, travelerInfo, travelerSession.Segments);
            }
            catch (System.Net.WebException wex)
            {
                if (wex.Response != null)
                {
                    var errorResponse = new StreamReader(wex.Response.GetResponseStream()).ReadToEnd();

                    _logger.LogError("SSR - UpdateTravelerInfo  - Exception {UpdateTravelerInfo} and {SessionId}", errorResponse, request.SessionId);

                    throw new System.Exception(wex.Message);
                }
            }

            #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******

            if (cslStopWatch.IsRunning)
            {
                cslStopWatch.Stop();
            }
            string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******


            _logger.LogInformation("UpdateTravelerInfo - Client Response for Update Traveler Info {to client Response} and {SessionId}", response, request.SessionId);
            return response;
        }

        public async Task<MOBUpdateTravelerInfoResponse> DeleteSelectedSSRItem
        (MOBUpdateTravelerInfoResponse response, MOBUpdateTravelerInfoRequest request, string Token, List<string> deleteSSRItems)
        {

            var jsonDeleteSSRResponse = string.Empty;

            List<United.Service.Presentation.CommonModel.Service> travelerDeleteSSRRequest = DeleteTravelerSSRRequest(deleteSSRItems);
            string jsonDeleteSSRRequest = JsonSerializer.Serialize<List<United.Service.Presentation.CommonModel.Service>>(travelerDeleteSSRRequest);

            try
            {
                string path = string.Format("{0}/Services/Delete?RetrievePNR=true&EndTransaction=true", request.RecordLocator);


                jsonDeleteSSRResponse = await _pNRRetrievalService.PNRRetrieval(Token, jsonDeleteSSRRequest, request.SessionId, path);

                if (!string.IsNullOrEmpty(jsonDeleteSSRResponse))
                {
                    United.Service.Presentation.CommonModel.Message
                       msgDeleteSSRResponse = JsonSerializer.Deserialize<United.Service.Presentation.CommonModel.Message>(jsonDeleteSSRResponse);

                    if (msgDeleteSSRResponse != null && string.Equals
                       (msgDeleteSSRResponse.Status, "SHARE_RESPONSE", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        if (msgDeleteSSRResponse.Text.IndexOf("INV") > -1)
                        {
                            response.Exception = new MOBException("9999", _configuration.GetValue<String>("Booking2OGenericExceptionMessage"));
                        }
                    }
                }
            }
            catch (System.Net.WebException wex)
            {
                if (wex.Response != null)
                {
                    var errorResponse = new StreamReader(wex.Response.GetResponseStream()).ReadToEnd();

                    _logger.LogError("Delete SSR - UpdateTravelerInfo  - Exception {DeleteSelectedSSRItem} and {SessionId}", errorResponse, request.SessionId);
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }

            return response;
        }
        public List<United.Service.Presentation.CommonModel.Service> DeleteTravelerSSRRequest(List<string> deleteSSRItems)
        {

            List<United.Service.Presentation.CommonModel.Service> _deleteSSRRequests = new List<Service.Presentation.CommonModel.Service>();
            United.Service.Presentation.CommonModel.Service _deleteSSRRequest = new United.Service.Presentation.CommonModel.Service();

            deleteSSRItems.ForEach(item =>
            {
                int opValue;
                bool hasValidDisplaySequence = Int32.TryParse(item, out opValue);
                if (hasValidDisplaySequence)
                {
                    _deleteSSRRequest = new United.Service.Presentation.CommonModel.Service
                    {
                        DisplaySequence = opValue,
                        Key = "SSR",
                    };
                    _deleteSSRRequests.Add(_deleteSSRRequest);
                }

            });
            return _deleteSSRRequests;
        }

        public async Task<MOBUpdateTravelerInfoResponse> UpdateTravlerKTNRedress
           (MOBUpdateTravelerInfoResponse response, MOBUpdateTravelerInfoRequest request, string Token, string cslJsonRequest, MOBPNRPassenger travelerInfo)
        {
            var jsonResponseKTNRedress = string.Empty;
            var urlKTNRedress = string.Empty;

            bool hasKTN = !string.IsNullOrEmpty(travelerInfo.KnownTravelerNumber);
            bool hasRedress = !string.IsNullOrEmpty(travelerInfo.RedressNumber);
            string ktnValue = string.IsNullOrEmpty(travelerInfo.KnownTravelerNumber) ? string.Empty : travelerInfo.KnownTravelerNumber;
            string redressValue = string.IsNullOrEmpty(travelerInfo.RedressNumber) ? string.Empty : travelerInfo.RedressNumber;

            try
            {
                if (hasKTN || hasRedress)
                {
                    string path = string.Format("{0}/Passengers/SecureFlights?RetrievePNR=true&EndTransaction=true", request.RecordLocator);

                    jsonResponseKTNRedress = await _pNRRetrievalService.PNRRetrieval(Token, cslJsonRequest, request.SessionId, path);
                }

                if (!string.IsNullOrEmpty(jsonResponseKTNRedress))
                {
                    List<United.Service.Presentation.CommonModel.Message>
                        msgResponseKTNRedress = JsonSerializer.Deserialize<List<United.Service.Presentation.CommonModel.Message>>(jsonResponseKTNRedress);

                    if (msgResponseKTNRedress != null && string.Equals
                       (msgResponseKTNRedress[0].Status, "SHARE_RESPONSE", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        if ((hasKTN || hasRedress) && (msgResponseKTNRedress[0].Text.IndexOf(ktnValue.ToUpper()) == -1 &&
                            msgResponseKTNRedress[0].Text.IndexOf(redressValue.ToUpper()) == -1))
                        {
                            response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                        }
                        else if (msgResponseKTNRedress[0].Text.IndexOf("INV") > -1)
                        {
                            response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                        }
                    }
                }
            }
            catch (System.Net.WebException wex)
            {
                if (wex.Response != null)
                {
                    var errorResponse = new StreamReader(wex.Response.GetResponseStream()).ReadToEnd();

                    _logger.LogError("KTN/REDRESS - UpdateTravelerInfo  - Exception {UpdateTravelerInfo} and {SessionId}", errorResponse, request.SessionId);
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }

            return response;
        }

        public async Task<MOBUpdateTravelerInfoResponse> UpdateTravlerSSRInfo
          (MOBUpdateTravelerInfoResponse response, MOBUpdateTravelerInfoRequest request, string Token, MOBPNRPassenger travelerInfo, List<MOBPNRSegment> segments)
        {

            string urlSpecialRequest = string.Empty;
            var jsonResponseSpecialRequest = string.Empty;

            bool hasSpecialRequest = (travelerInfo.SelectedSpecialNeeds != null && travelerInfo.SelectedSpecialNeeds.Any());

            if (GeneralHelper.IsApplicationVersionGreater
                    (request.Application.Id, request.Application.Version.Major, "AndroidEnableMgnResUpdateSpecialNeeds", "iPhoneEnableMgnResUpdateSpecialNeeds", string.Empty, string.Empty, true, _configuration))
            {
                if (hasSpecialRequest)
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("ValidateMsgMgnResUpdateSpecialNeeds"));
                return response;
            }

            try
            {
                if (hasSpecialRequest)
                {

                    List<United.Service.Presentation.CommonModel.Service> travelerInfoSpecialRequest = UpdateTravelerInfoSpecialRequest(request, segments);

                    string jsonRequestSpecialRequest = JsonSerializer.Serialize<List<United.Service.Presentation.CommonModel.Service>>(travelerInfoSpecialRequest);

                    var specialRequestCodes = travelerInfoSpecialRequest.Select(c => c.Code);

                    string path = string.Format("{0}/Services?RetrievePNR=true&EndTransaction=true", request.RecordLocator);

                    jsonResponseSpecialRequest = await _pNRRetrievalService.PNRRetrieval(Token, jsonRequestSpecialRequest, request.SessionId, path);
                }
                if (!string.IsNullOrEmpty(jsonResponseSpecialRequest))
                {
                    United.Service.Presentation.CommonModel.Message msgResponseSpecialRequest = JsonSerializer.Deserialize<United.Service.Presentation.CommonModel.Message>(jsonResponseSpecialRequest);

                    if (msgResponseSpecialRequest != null && string.Equals
                       (msgResponseSpecialRequest.Status, "SHARE_RESPONSE", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        if (msgResponseSpecialRequest.Text.IndexOf("INV") > -1)
                        {
                            response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                        }
                    }
                }
            }
            catch (System.Net.WebException wex)
            {
                if (wex.Response != null)
                {
                    var errorResponse = new StreamReader(wex.Response.GetResponseStream()).ReadToEnd();

                    _logger.LogInformation("SSR Meals/Accomodation - UpdateTravelerInfo  - Exception {UpdateTravelerInfo} and SessionId}", errorResponse, request.SessionId);
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }


            return response;
        }
        public List<United.Service.Presentation.CommonModel.Service> UpdateTravelerInfoSpecialRequest
            (MOBUpdateTravelerInfoRequest request, List<MOBPNRSegment> segments)
        {

            List<United.Service.Presentation.CommonModel.Service> _specialRequests = new List<Service.Presentation.CommonModel.Service>();
            United.Service.Presentation.CommonModel.Service _specialRequest = new United.Service.Presentation.CommonModel.Service();

            List<Tuple<int, string, string>> eligibleSegmentMeals = new List<Tuple<int, string, string>>();
            Collection<int> specialAccomodation = new Collection<int>();
            segments.ForEach(f =>
            {

                if (!string.IsNullOrEmpty(f.SSRMeals))
                {
                    eligibleSegmentMeals.Add(Tuple.Create(f.SegmentNumber, f.SSRMeals, f.ActionCode));
                }
                specialAccomodation.Add(f.SegmentNumber);
            });


            request.TravelersInfo.ForEach(traveler =>
            {
                if (traveler.SelectedSpecialNeeds != null && traveler.SelectedSpecialNeeds.Any())
                {
                    traveler.SelectedSpecialNeeds.ForEach(item =>
                    {
                        string statusDesc = string.Empty;

                        Collection<int> selectedFlightSegment = new Collection<int>();
                        eligibleSegmentMeals.EachFor(sfs =>
                        {
                            if (sfs.Item2.IndexOf(item.Code) > -1)
                            {
                                selectedFlightSegment.Add(sfs.Item1);
                                statusDesc = sfs.Item3;
                            }
                        });


                        if (item.SubOptions != null && item.SubOptions.Any())
                        {
                            item.SubOptions.ForEach(subItem =>
                            {
                                _specialRequest = new United.Service.Presentation.CommonModel.Service
                                {
                                    CarrierCode = new Service.Presentation.CommonModel.VendorModel.Vendor { Name = "UA" },//TODO : Need to check
                                    Code = subItem.Code,
                                    Description = item.DisplayDescription,
                                    Key = "SSR",
                                    TravelerNameIndex = traveler.SHARESPosition,
                                    NumberInParty = 1,
                                    SegmentNumber = specialAccomodation,
                                    Status = new Status { Description = "NN" }
                                };
                                _specialRequests.Add(_specialRequest);
                            });
                        }
                        else
                        {
                            _specialRequest = new United.Service.Presentation.CommonModel.Service
                            {
                                CarrierCode = new Service.Presentation.CommonModel.VendorModel.Vendor { Name = "UA" },
                                Code = item.Code,
                                Description = item.DisplayDescription,
                                Key = "SSR",
                                TravelerNameIndex = traveler.SHARESPosition,
                                NumberInParty = 1,
                                SegmentNumber = specialAccomodation,
                                Status = new Status { Description = "NN" },
                            };
                            _specialRequests.Add(_specialRequest);

                        }

                    });
                }
            });

            return _specialRequests;
        }
        public bool IsInternationalBillingAddress_CheckinFlowEnabled(MOBApplication application)
        {
            if (_configuration.GetValue<bool>("EnableInternationalBillingAddress_CheckinFlow"))
            {
                if (application != null && GeneralHelper.IsApplicationVersionGreater(application.Id, application.Version.Major, "IntBillingCheckinFlowAndroidversion", "IntBillingCheckinFlowiOSversion", "", "", true, _configuration))
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<(bool updateTravelerSuccess, List<MOBItem> insertUpdateItemKeys)> UpdateTravelerV2(MOBUpdateTravelerRequest request, List<MOBItem> insertUpdateItemKeys)
        {
            bool updateTravelerSuccess = false;
            if (string.IsNullOrEmpty(request.MileagePlusNumber))
            {
                throw new MOBUnitedException("Profile Owner MileagePlus number is required.");
            }

            if (request.UpdateAddressInfoAssociatedWithCC == true && request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0 && request.Traveler.Addresses[0].Country.Code.Trim() == "US")
            {
                string stateCode = string.Empty;
                var tupleResponse = await GetAndValidateStateCode(request, stateCode);
                stateCode = tupleResponse.stateCode;
                if (tupleResponse.returnValue)
                {
                    request.Traveler.Addresses[0].State.Code = stateCode;
                }
            }
            if (request.UpdateTravelerBasiInfo == true && !String.IsNullOrEmpty(request.Traveler.Key))
            {
                updateTravelerSuccess = await UpdateTravelerBase(request);

                if (!request.UpdateCreditCardInfo && !request.UpdateAddressInfoAssociatedWithCC)
                {
                    await UpdateViewName(request);
                }
            }

            bool isEmailUpdated = default, isRewardProgramUpdated=default;
            var tupleRes = await isRewardProgramChanged(request,  isEmailUpdated,  isRewardProgramUpdated);
            isEmailUpdated = tupleRes.emailChanged;
            isRewardProgramUpdated = tupleRes.rewardProgramUpdated;

            if ((request.UpdatePhoneInfo == true
                                            && request.Traveler.Phones != null
                                            && request.Traveler.Phones.Count > 0
                                            && !String.IsNullOrEmpty(request.Traveler.Phones[0].PhoneNumber)
                                            && !String.IsNullOrEmpty(request.Traveler.Phones[0].CountryCode)
                                            &&
                                            (
                                               String.IsNullOrEmpty(request.Traveler.Phones[0].Key)
                                               ||
                                               (!String.IsNullOrEmpty(request.Traveler.Phones[0].Key) && await isPhoneNumberChanged(request))
                                            ))
              ||
                (
                 request.UpdateEmailInfo == true
                                           && request.Traveler.EmailAddresses != null
                                           && request.Traveler.EmailAddresses.Count > 0
                                           && !String.IsNullOrEmpty(request.Traveler.EmailAddresses[0].EmailAddress)
                                           &&
                                           (
                                            String.IsNullOrEmpty(request.Traveler.EmailAddresses[0].Key)
                                            ||
                                            (!String.IsNullOrEmpty(request.Traveler.EmailAddresses[0].Key) && isEmailUpdated)
                                           )

                ))
            {

                updateTravelerSuccess = await InsertOrUpdateTravelerEmailPhone(request);
            }

            if (request.UpdateRewardProgramInfo == true && request.Traveler.AirRewardPrograms != null && request.Traveler.AirRewardPrograms.Count > 0 && !String.IsNullOrEmpty(request.Traveler.AirRewardPrograms[0].RewardProgramKey))
            {
                if (!request.Traveler.IsProfileOwner || (request.Traveler.IsProfileOwner && request.Traveler.AirRewardPrograms[0].CarrierCode.Trim().ToUpper() != "UA")) // to check not to update profile owner UA MP details. It will fail if trying to update the MP Account of the profile owner
                {
                    if (isRewardProgramUpdated)
                    {
                        updateTravelerSuccess = await UpdateTravelerRewardProgram(request);
                    }
                }
            }
            else if (request.UpdateRewardProgramInfo == true && request.Traveler.AirRewardPrograms != null && request.Traveler.AirRewardPrograms.Count > 0 && String.IsNullOrEmpty(request.Traveler.AirRewardPrograms[0].RewardProgramKey))
            {
                updateTravelerSuccess =await InsertTravelerRewardProgram(request);
            }
            if (request.UpdateAddressInfoAssociatedWithCC || request.UpdateCreditCardInfo)
            {
                insertUpdateItemKeys = new List<MOBItem>();
                string addressKey = string.Empty, ccKey = string.Empty;
                if (Convert.ToBoolean(_configuration.GetValue<string>("CorporateConcurBooking") ?? "false"))
                {
                    #region
                    if (request.UpdateAddressInfoAssociatedWithCC == true && request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0 && !request.Traveler.Addresses[0].IsCorporate)
                    {
                        var insertOrUpdateTravelerAddressResponse = await InsertOrUpdateTravelerAddress(request, addressKey); //After UCB migration service is same for updating or Inserting the address..So combined both into one service
                        updateTravelerSuccess = insertOrUpdateTravelerAddressResponse.isSuccess;
                        addressKey = insertOrUpdateTravelerAddressResponse.addressKey;
                        MOBItem item = new MOBItem();
                        item.Id = "AddressKey";
                        item.CurrentValue = addressKey;
                        insertUpdateItemKeys.Add(item);
                    }
                    else if (request.UpdateAddressInfoAssociatedWithCC == true && request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0 && !String.IsNullOrEmpty(request.Traveler.Addresses[0].Key) && request.Traveler.Addresses[0].IsCorporate)
                    {
                        MOBItem item = new MOBItem();
                        item.Id = "AddressKey";
                        item.CurrentValue = request.Traveler.Addresses[0].Key;
                        insertUpdateItemKeys.Add(item);
                    }
                    if (_configuration.GetValue<string>("NotAllowUpdateAddressKeyForCorporateCC") != null && Convert.ToBoolean(_configuration.GetValue<string>("NotAllowUpdateAddressKeyForCorporateCC").ToString()))
                    {
                        #region
                        if (request.UpdateCreditCardInfo == true && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && !String.IsNullOrEmpty(request.Traveler.CreditCards[0].Key) && !request.Traveler.CreditCards[0].IsCorporate)
                        {
                            request.Traveler.CreditCards[0].AddressKey = addressKey;
                            var tupleValue = await UpdateTravelerCreditCard(request,  ccKey); // Same here the CC key going to change after the update.
                            updateTravelerSuccess = tupleValue.returnValue;
                            ccKey = tupleValue.ccKey;
                            MOBItem item = new MOBItem();
                            item.Id = "CreditCardKey";
                            item.CurrentValue = ccKey;
                            insertUpdateItemKeys.Add(item);
                        }
                        else if (request.UpdateCreditCardInfo == true && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && String.IsNullOrEmpty(request.Traveler.CreditCards[0].Key) && !request.Traveler.CreditCards[0].IsCorporate)
                        {
                            request.Traveler.CreditCards[0].AddressKey = addressKey;
                            var tupleValue = await InsertTravelerCreditCard(request, ccKey);
                            updateTravelerSuccess = tupleValue.returnValue;
                            ccKey = tupleValue.ccKey;
                            MOBItem item = new MOBItem();
                            item.Id = "CreditCardKey";
                            item.CurrentValue = ccKey;
                            insertUpdateItemKeys.Add(item);
                        }
                        else if (request.UpdateCreditCardInfo == true && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && !String.IsNullOrEmpty(request.Traveler.CreditCards[0].Key) && request.Traveler.CreditCards[0].IsCorporate)
                        {
                            MOBItem item = new MOBItem();
                            item.Id = "CreditCardKey";
                            item.CurrentValue = request.Traveler.CreditCards[0].Key;
                            insertUpdateItemKeys.Add(item);
                        }
                        #endregion
                    }
                    else
                    {
                        #region
                        if (request.UpdateCreditCardInfo == true && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && !String.IsNullOrEmpty(request.Traveler.CreditCards[0].Key) && !(request.Traveler.CreditCards[0].IsCorporate && request.Traveler.Addresses[0].IsCorporate))
                        {
                            request.Traveler.CreditCards[0].AddressKey = addressKey;
                            var tupleValue = await UpdateTravelerCreditCard(request, ccKey); // Same here the CC key going to change after the update.
                            updateTravelerSuccess = tupleValue.returnValue;
                            ccKey = tupleValue.ccKey;
                            MOBItem item = new MOBItem();
                            item.Id = "CreditCardKey";
                            item.CurrentValue = ccKey;
                            insertUpdateItemKeys.Add(item);
                        }
                        else if (request.UpdateCreditCardInfo == true && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && String.IsNullOrEmpty(request.Traveler.CreditCards[0].Key) && !request.Traveler.CreditCards[0].IsCorporate)
                        {
                            request.Traveler.CreditCards[0].AddressKey = addressKey;
                            var tupleValue = await InsertTravelerCreditCard(request, ccKey);
                            updateTravelerSuccess = tupleValue.returnValue;
                            ccKey = tupleValue.ccKey;
                            MOBItem item = new MOBItem();
                            item.Id = "CreditCardKey";
                            item.CurrentValue = ccKey;
                            insertUpdateItemKeys.Add(item);
                        }
                        else if (request.UpdateCreditCardInfo == true && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && !String.IsNullOrEmpty(request.Traveler.CreditCards[0].Key) && request.Traveler.CreditCards[0].IsCorporate)
                        {
                            MOBItem item = new MOBItem();
                            item.Id = "CreditCardKey";
                            item.CurrentValue = request.Traveler.CreditCards[0].Key;
                            insertUpdateItemKeys.Add(item);
                        }

                        #endregion
                    }
                    #endregion
                }

            }

            if (EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
            {
                if (request.UpdateSpecialRequests)
                {
                    updateTravelerSuccess = UpdateSpecialRequests(request);
                }

                if (request.UpdateServiceAnimals)
                {
                    updateTravelerSuccess = UpdateServiceAnimals(request);
                }

                if (request.UpdateMealPreference)
                {
                    updateTravelerSuccess = UpdateMealPreference(request);
                }
            }

            return (updateTravelerSuccess, insertUpdateItemKeys);
        }


        public async Task<bool> InsertOrUpdateTravelerEmailPhone(MOBUpdateTravelerRequest request)
        {
            bool isSuccess = false;

            string jsonResponse = string.Empty;
            #region
            UpdateMemberContact insertOrUpdateRequest = GetInsertOrUpdateTravelerEmailPhone(request);

            string jsonRequest = JsonSerializer.Serialize<UpdateMemberContact>(insertOrUpdateRequest);
            _logger.LogInformation("InsertOrUpdateTravelerEmailPhone - Request {ApplicationId} {DeviceId} {JsonRequest} {ApplicationVersion}", request.Application.Id, request.DeviceId, jsonRequest, request.Application.Version.Major);

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cslStopWatch;
            cslStopWatch = new Stopwatch();
            cslStopWatch.Reset();
            cslStopWatch.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******     

            jsonResponse = await _insertOrUpdateTravelInfoService.InsertOrUpdateTravelerInfo(request.Traveler.CustomerId, jsonRequest, request.Token);


            #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cslStopWatch.IsRunning)
            {
                cslStopWatch.Stop();
            }
            string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

            _logger.LogInformation("InsertOrUpdateTravelerEmailPhone - Response {ApplicationId} {ApplicationVersion} {DeviceId} {cslCallTime}", request.Application.Id, request.Application.Version.Major, request.DeviceId, cslCallTime);

            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******    

            _logger.LogInformation("InsertOrUpdateTravelerEmailPhone - Response {ApplicationId} {ApplicationVersion} {DeviceId} {reponse}", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonResponse);


            if (!string.IsNullOrEmpty(jsonResponse))
            {
                var responseData = Newtonsoft.Json.JsonConvert.DeserializeObject<Mobile.Model.CSLModels.CslResponse<UpdateContactResponse>>(jsonResponse);

                if (responseData.Data != null && responseData.Errors == null)
                {
                    UpdateContactResponse response = new UpdateContactResponse();
                    response = responseData.Data;
                    if (!string.IsNullOrEmpty(response.PhoneKey) || !string.IsNullOrEmpty(response.EmailKey))
                    {
                        isSuccess = true;
                    }
                    _logger.LogInformation("InsertOrUpdateTravelerAddress - DeSerialized Response {ApplicationId} {DeviceId} {reponse}", request.Application.Id, request.Application.Version.Major, request.DeviceId, response);

                }
            }
            #endregion
            return isSuccess;
        }

        private UpdateMemberContact GetInsertOrUpdateTravelerEmailPhone(MOBUpdateTravelerRequest request)
        {
            UpdateMemberContact insertOrUpdateTravelerEmailPhone = new UpdateMemberContact();
            insertOrUpdateTravelerEmailPhone.CustomerId = request.Traveler.CustomerId.ToString();
            #region Build insert or Update Traveler Phone request

            if (request.UpdatePhoneInfo
              && request.Traveler.Phones != null
              && request.Traveler.Phones.Count > 0
              && (!String.IsNullOrEmpty(request.Traveler.Phones[0].Key) || (!String.IsNullOrEmpty(request.Traveler.Phones[0].AreaNumber) && !String.IsNullOrEmpty(request.Traveler.Phones[0].PhoneNumber)))
              )
            {
                //Rajesh K need to fix
                insertOrUpdateTravelerEmailPhone.Phone = new United.Mobile.Model.CSLModels.Phone();
                Mobile.Model.CSLModels.Constants.PhoneType phoneType;
                Mobile.Model.CSLModels.Constants.DeviceType deviceType;
                #region Convert channelTyCode in request to Enum
                if (!String.IsNullOrEmpty(request.Traveler.Phones[0].ChannelTypeCode))
                {
                    System.Enum.TryParse<Mobile.Model.CSLModels.Constants.PhoneType>(request.Traveler.Phones[0].ChannelTypeCode, out phoneType);
                }
                else
                {
                    phoneType = Mobile.Model.CSLModels.Constants.PhoneType.H;
                }
                #endregion
                #region Convert DeviceTypeCode in request to Enum
                if (!String.IsNullOrEmpty(request.Traveler.Phones[0].DeviceTypeCode))
                {
                    System.Enum.TryParse<Mobile.Model.CSLModels.Constants.DeviceType>(request.Traveler.Phones[0].DeviceTypeCode, out deviceType);
                }
                else
                {
                    deviceType = Mobile.Model.CSLModels.Constants.DeviceType.PH;
                }
                #endregion
                if ((!String.IsNullOrEmpty(request.Traveler.Phones[0].Key)))
                {
                    insertOrUpdateTravelerEmailPhone.Phone.Key = request.Traveler.Phones[0].Key;
                    insertOrUpdateTravelerEmailPhone.Phone.UpdateId = request.Traveler.CustomerId.ToString();
                }
                else
                {
                    insertOrUpdateTravelerEmailPhone.Phone.Type = phoneType;
                    insertOrUpdateTravelerEmailPhone.Phone.InsertId = request.Traveler.CustomerId.ToString();
                }
                insertOrUpdateTravelerEmailPhone.Phone.Number = (request.Traveler.Phones[0].AreaNumber + request.Traveler.Phones[0].PhoneNumber).Trim();
                insertOrUpdateTravelerEmailPhone.Phone.CountryCode = string.IsNullOrEmpty(request.Traveler.Phones[0].CountryCode) ? "US" : request.Traveler.Phones[0].CountryCode.Trim();
                insertOrUpdateTravelerEmailPhone.Phone.DeviceType = deviceType;
                if (!request.UpdateCreditCardInfo && String.IsNullOrEmpty(request.Traveler.Phones[0].Key))
                {
                    insertOrUpdateTravelerEmailPhone.Phone.DayOfTravelNotification = true;
                }

                insertOrUpdateTravelerEmailPhone.Phone.Remark = request.Traveler.Phones[0].Description;
                insertOrUpdateTravelerEmailPhone.Phone.LanguageCode = request.LanguageCode;
            }
            #endregion

            #region Build Insert or Update Traveler Email request
            if (request.UpdateEmailInfo
                        && request.Traveler.EmailAddresses != null
                        && request.Traveler.EmailAddresses.Count > 0
                        && (!String.IsNullOrEmpty(request.Traveler.EmailAddresses[0].Key) || !String.IsNullOrEmpty(request.Traveler.EmailAddresses[0].EmailAddress))
                        )
            {               
                Mobile.Model.CSLModels.Constants.EmailType emailType;
                #region Convert channelTyCode in request to Enum
                if (request.Traveler.EmailAddresses[0].Channel != null && !String.IsNullOrEmpty(request.Traveler.EmailAddresses[0].Channel.ChannelCode))
                {
                    System.Enum.TryParse<Mobile.Model.CSLModels.Constants.EmailType>(request.Traveler.EmailAddresses[0].Channel.ChannelCode, out emailType);
                }
                else
                {
                    emailType = Mobile.Model.CSLModels.Constants.EmailType.O;
                }
                #endregion
                //Rajesh K need to fix
                insertOrUpdateTravelerEmailPhone.Email = new United.Mobile.Model.CSLModels.Email();
                insertOrUpdateTravelerEmailPhone.Email.Address = request.Traveler.EmailAddresses[0].EmailAddress;
                insertOrUpdateTravelerEmailPhone.Email.Type = emailType;
                if (!String.IsNullOrEmpty(request.Traveler.EmailAddresses[0].Key))
                {
                    insertOrUpdateTravelerEmailPhone.Email.Key = request.Traveler.EmailAddresses[0].Key;
                    insertOrUpdateTravelerEmailPhone.Email.UpdateId = request.Traveler.CustomerId.ToString();
                }
                else
                {
                    insertOrUpdateTravelerEmailPhone.Email.InsertId = request.Traveler.CustomerId.ToString();
                }
                insertOrUpdateTravelerEmailPhone.Email.LanguageCode = request.LanguageCode;

                if (!request.UpdateCreditCardInfo && String.IsNullOrEmpty(request.Traveler.EmailAddresses[0].Key))
                {
                    insertOrUpdateTravelerEmailPhone.Email.DayOfTravelNotification = true;
                }
            }
            #endregion

            return insertOrUpdateTravelerEmailPhone;
        }

        public async Task<(bool isSuccess, string addressKey)> InsertOrUpdateTravelerAddress(MOBUpdateTravelerRequest request, string addressKey)
        {
            bool isSuccess = false;
            string jsonResponse = string.Empty;
            try
            {

                UpdateMemberContact insertOrUpdateAddress = GetInsertOrUpdateAddressRequest(request);
                string jsonRequest = JsonSerializer.Serialize<UpdateMemberContact>(insertOrUpdateAddress);

                _logger.LogInformation("InsertOrUpdateTravelerAddress - Request,  {SessionId} {ApplicationId} {ApplicationVersion} {DeviceId} {jsonRequest} ", request.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonRequest);

                #region//****Get Call Duration Code - Venkat 03/17/2015*******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

                jsonResponse = await _insertOrUpdateTravelInfoService.InsertOrUpdateTravelerInfo(request.Traveler.CustomerId, jsonRequest, request.Token, true);

                #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    var responseData = Newtonsoft.Json.JsonConvert.DeserializeObject<Mobile.Model.CSLModels.CslResponse<UpdateContactResponse>>(jsonResponse);

                    if (responseData.Data != null && responseData.Errors == null)
                    {
                        UpdateContactResponse response = new UpdateContactResponse();
                        response = responseData.Data;

                        addressKey = response.AddressKey;
                        isSuccess = true;

                        _logger.LogInformation("InsertOrUpdateTravelerAddress - DeSerialized Response,  {SessionId} {ApplicationId} {ApplicationVersion} {DeviceId} {jsonRequest} ", request.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, response);

                    }

                }

                #endregion
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
            return (isSuccess, addressKey);
        }

        private UpdateMemberContact GetInsertOrUpdateAddressRequest(MOBUpdateTravelerRequest request)
        {
            #region Build insert Traverl Request
            UpdateMemberContact addressInsertOrUpdateRequest = new UpdateMemberContact();
            if (request.UpdateAddressInfoAssociatedWithCC && request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0)
            {
                addressInsertOrUpdateRequest.Address = new United.Mobile.Model.CSLModels.Address();
                if (!string.IsNullOrEmpty(request.Traveler.Addresses[0].Key))
                {
                    addressInsertOrUpdateRequest.Address.Key = request.Traveler.Addresses[0].Key;
                    addressInsertOrUpdateRequest.Address.UpdateId = request.Traveler.CustomerId.ToString();
                }
                else
                {
                    addressInsertOrUpdateRequest.Address.InsertId = request.Traveler.CustomerId.ToString();
                }
                addressInsertOrUpdateRequest.CustomerId = request.Traveler.CustomerId.ToString();
                addressInsertOrUpdateRequest.Address.Line1 = request.Traveler.Addresses[0].Line1;
                addressInsertOrUpdateRequest.Address.Line2 = request.Traveler.Addresses[0].Line2;
                addressInsertOrUpdateRequest.Address.Line3 = request.Traveler.Addresses[0].Line3;
                addressInsertOrUpdateRequest.Address.City = request.Traveler.Addresses[0].City;
                addressInsertOrUpdateRequest.Address.CountryCode = request.Traveler.Addresses[0].Country != null ? request.Traveler.Addresses[0].Country.Code : "";
                addressInsertOrUpdateRequest.Address.PostalCode = request.Traveler.Addresses[0].PostalCode;
                addressInsertOrUpdateRequest.Address.StateCode = request.Traveler.Addresses[0].State != null ? request.Traveler.Addresses[0].State.Code : "";
                addressInsertOrUpdateRequest.Address.LanguageCode = request.LanguageCode;
            }
            #endregion
            return addressInsertOrUpdateRequest;
        }

        #region UCB Migration MobilePhase3
        private async Task<(bool returnValue, string ccKey)> UpsertTravelerCreditCardV2(MOBUpdateTravelerRequest request)
        {
            bool success = false;
            var ccKey = string.Empty;
            string jsonResponse = string.Empty;
            try
            {
                #region
                var ccRequest = GetCreditCardAwsRequest(request);
                string jsonRequest = JsonSerializer.Serialize(ccRequest);

                var response = await _customerProfileCreditCardsService.UpsertCreditCard<CreditCardAwsResponse>(request.Token, request.SessionId, request.MileagePlusNumber, jsonRequest);

                if (response != null)
                {
                    if (response.Data.ReturnValues != null)
                    {
                        var creditCardKey = response.Data.ReturnValues.Where(t => t.Key.ToUpper().Trim() == "CreditCardKey".ToUpper().Trim()).ToList();
                        success = true;
                        ccKey = creditCardKey[0].Value;
                    }
                    else
                    {
                        if (response.Errors != null && response.Errors.Count > 0)
                        {
                            string errorMessage = string.Empty;
                            foreach (var error in response.Errors)
                            {
                                if (!string.IsNullOrEmpty(error.Message) && error.Message.ToLower().Contains("ora-") && error.Message.ToLower().Contains("unique constraint") && error.Message.Contains("violated"))
                                    errorMessage = errorMessage + " " + _configuration.GetValue<string>("CCAlreadyExistMessage").ToString();
                                errorMessage = errorMessage + " " + error.UserFriendlyMessage;
                            }

                            throw new MOBUnitedException(errorMessage);
                        }
                        else
                        {
                            string exceptionMessage = _configuration.GetValue<string>("UnableToInsertTravelerRewardProgramToProfileErrorMessage");
                            if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                            {
                                exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL InsertTravelerRewardProgram(MOBUpdateTravelerRequest request)";
                            }
                            throw new MOBUnitedException(exceptionMessage);
                        }
                    }
                }
                else
                {
                    string exceptionMessage = _configuration.GetValue<string>("UnableToInsertCreditCardToProfileErrorMessage");
                    if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                    {
                        exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  InsertTravelerCreditCardV2(MOBUpdateTravelerRequest request)";
                    }
                    throw new MOBUnitedException(exceptionMessage);
                }
                #endregion
            }
            catch (Exception ex)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
            }
            return (success, ccKey);
        }

        private bool InsertTravelerRewardProgramV2(MOBUpdateTravelerRequest request)
        {
            bool success = false;
            #region
            var insertRewardProgram = GetInsertRewardProgramAwsRequest(request);
            string jsonRequest = JsonSerializer.Serialize(insertRewardProgram);

            var response = _customerPreferencesService.InsertRewardPrograms<RewardProgramAwsResponse>(request.Token, request.SessionId, request.Traveler.CustomerId, request.Traveler.ProfileId, jsonRequest).Result;

            if (response != null)
            {
                if (response.Status.Equals((int)Services.Customer.Common.Constants.StatusType.Success))
                {
                    success = true;
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            if (error.UserFriendlyMessage != null && error.UserFriendlyMessage.ToString().Contains("ProgramMemberId") && error.UserFriendlyMessage.ToString().Contains("not valid"))
                            {
                                errorMessage += " " + _configuration.GetValue<string>("ValidateMPNameMisMatchErrorMessage");
                            }
                            else
                            {
                                errorMessage += " " + error.UserFriendlyMessage;
                            }
                        }

                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToInsertTravelerRewardProgramToProfileErrorMessage");
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToInsertTravelerRewardProgramToProfileErrorMessage");
                if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  InsertTravelerRewardProgram(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion
            return success;
        }

        private bool UpdateTravelerRewardProgramV2(MOBUpdateTravelerRequest request)
        {
            bool success = false;
            #region
            var updateRewardProgram = GetUpdateRewardProgramAwsRequest(request);
            string jsonRequest = JsonSerializer.Serialize(updateRewardProgram);

            var response = _customerPreferencesService.UpdateRewardPrograms<RewardProgramAwsResponse>(request.Token, request.SessionId, request.Traveler.CustomerId, request.Traveler.ProfileId, jsonRequest).Result;

            if (response != null)
            {
                if (response.Status.Equals((int)Services.Customer.Common.Constants.StatusType.Success))
                {
                    success = true;
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            if (error.UserFriendlyMessage != null && error.UserFriendlyMessage.ToString().Contains("ProgramMemberId") && error.UserFriendlyMessage.ToString().Contains("not valid"))
                            {
                                errorMessage += " " + _configuration.GetValue<string>("ValidateMPNameMisMatchErrorMessage");
                            }
                            else
                            {
                                errorMessage += " " + error.UserFriendlyMessage;
                            }
                        }

                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerRewardProgramToProfileErrorMessage");
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerRewardProgramToProfileErrorMessage");
                if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  UpdateTravelerRewardProgram(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion
            return success;
        }

        private UpdateRewardProgramAwsRequest GetUpdateRewardProgramAwsRequest(MOBUpdateTravelerRequest request)
        {
            var rewardProgramRequest = new UpdateRewardProgramAwsRequest()
            {
                RewardPrograms = new List<RewardProgramAws>()
                {
                   new RewardProgramAws
                   {
                        ProgramId = Convert.ToInt32(request.Traveler.AirRewardPrograms[0].ProgramId),
                        ProgramMemberId = request.Traveler.AirRewardPrograms[0].MemberId
                   }
                },
                UpdateId = request.Traveler.CustomerId.ToString()
            };

            return rewardProgramRequest;

        }

        private InsertRewardProgramAwsRequest GetInsertRewardProgramAwsRequest(MOBUpdateTravelerRequest request)
        {
            var rewardProgramRequest = new InsertRewardProgramAwsRequest()
            {
                RewardPrograms = new List<RewardProgramAws>()
                {
                   new RewardProgramAws
                   {
                        ProgramId = Convert.ToInt32(request.Traveler.AirRewardPrograms[0].ProgramId),
                        ProgramMemberId = request.Traveler.AirRewardPrograms[0].MemberId
                   }
                },
                InsertId = request.Traveler.CustomerId.ToString()
            };

            return rewardProgramRequest;
        }

        private CreditCardAwsRequest GetCreditCardAwsRequest(MOBUpdateTravelerRequest request)
        {
            var creditCardRequest = new CreditCardAwsRequest();
            string accountNumberToken = request.Traveler.CreditCards[0].AccountNumberToken;
            bool CCTokenDataVaultCheck = true;

            if (!string.IsNullOrEmpty(request.Traveler.CreditCards[0].EncryptedCardNumber))
            {
                var tupleResponse = _profileCreditCard.GenerateCCTokenWithDataVault(request.Traveler.CreditCards[0], request.SessionId, request.Token, request.Application, request.DeviceId, accountNumberToken).Result;
                accountNumberToken = tupleResponse.ccDataVaultToken;
            }

            if (request.UpdateCreditCardInfo && request.Traveler.CreditCards != null && request.Traveler.CreditCards.Count > 0 && CCTokenDataVaultCheck)
            {
                creditCardRequest.CreditCardKey = request.Traveler.CreditCards[0].Key;
                creditCardRequest.AccountNumberToken = accountNumberToken;
                creditCardRequest.AddressKey = request.Traveler.CreditCards[0].AddressKey;
                creditCardRequest.CreditCardType = request.Traveler.CreditCards[0].CardType;
                creditCardRequest.CustomerId = request.Traveler.CustomerId;
                creditCardRequest.Description = request.Traveler.CreditCards[0].Description;
                creditCardRequest.ExpirationMonth = Convert.ToInt32(request.Traveler.CreditCards[0].ExpireMonth.Trim());
                creditCardRequest.ExpirationYear = Convert.ToInt32(request.Traveler.CreditCards[0].ExpireYear.Trim().Length == 2 ? "20" + request.Traveler.CreditCards[0].ExpireYear.Trim() : request.Traveler.CreditCards[0].ExpireYear.Trim());
                creditCardRequest.Name = request.Traveler.CreditCards[0].cCName;
                creditCardRequest.InsertId = request.Traveler.CustomerId.ToString();
                creditCardRequest.TravelerKey = request.Traveler.Key;
            }

            return creditCardRequest;
        }

        private async Task<(bool returnValue, List<MOBItem> insertItemKeys)> InsertTravelerV2(InsertTravelerRequest request)
        {
            bool success = false;
            var insertItemKeys = new List<MOBItem>();
            var travelRequest = GetInsertTravelerRequestAws(request);
            string jsonRequest = JsonSerializer.Serialize(travelRequest);
            var response = await _customerTravelerService.InsertTraveler<Mobile.Model.CSLModels.CslResponse<TravelerAwsResponse>>(request.Token, request.SessionId, request.MileagePlusNumber, jsonRequest);

            if (response != null)
            {
                if (response.Data != null)
                {
                    success = true;
                    MOBItem item = new MOBItem();
                    item.Id = "TravelerKey";
                    item.CurrentValue = response.Data.TravelerKey;
                    insertItemKeys.Add(item);
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count() > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage += " " + error.Description;
                        }

                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        throw new MOBUnitedException("Unable to Insert Traveler.");
                    }
                }
            }
            else
            {
                throw new MOBUnitedException("Unable to Insert Traveler.");
            }
            
            return (success, insertItemKeys);
        }

        public bool UpdateTravelerBaseV2(MOBUpdateTravelerRequest request)
        {
            var success = false;
            #region
            var updateTraveler = GetUpdateTravelerRequestAws(request);
            string jsonRequest = JsonSerializer.Serialize(updateTraveler);

            var response = _customerTravelerService.UpdateTravelerBase<Mobile.Model.CSLModels.CslResponse<TravelerAwsResponse>>(request.Token, request.SessionId, request.Traveler.CustomerId.ToString(), jsonRequest).Result;

            if (response != null)
            {
                if (response.Data != null)
                {
                    success = true;
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count() > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage += " " + error.Description;
                        }

                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerBaseProfileErrorMessage");
                        if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL UpdateTravelerBaseV2(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToUpdateTravelerBaseProfileErrorMessage");
                if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  UpdateTravelerBaseV2(MOBUpdateTravelerRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            #endregion
            return success;
        }
        private TravelerAwsRequest GetInsertTravelerRequestAws(InsertTravelerRequest request)
        {
            #region Build insert Traverl Request
            var requestData = new TravelerAwsRequest();
            requestData.TravelerData = new TravelerData()
            {
                BirthDate = DateTime.Parse(request.Traveler.BirthDate).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"),
                FirstName = request.Traveler.FirstName,
                MiddleName = request.Traveler.MiddleName,
                LastName = request.Traveler.LastName,
                Gender = request.Traveler.GenderCode,
                Suffix = request.Traveler.Suffix,
                Title = request.Traveler.Title,
            };

            requestData.TravelerData.InsertId = "123456";

            if (IsEnabledNationalityAndResidence(false, request.Application.Id, request.Application.Version.Major))
            {
                if (request.Traveler != null && !string.IsNullOrEmpty(request.Traveler.CountryOfResidence) && request.Traveler.CountryOfResidence.ToUpper().Equals("OTHER"))
                {
                    requestData.TravelerData.CountryOfResidence = null;
                }
                else
                {
                    requestData.TravelerData.CountryOfResidence = request.Traveler.CountryOfResidence;
                }

                if (request.Traveler != null && !string.IsNullOrEmpty(request.Traveler.Nationality) && request.Traveler.Nationality.ToUpper().Equals("OTHER"))
                {
                    requestData.TravelerData.Nationality = null;
                }
                else
                {
                    requestData.TravelerData.Nationality = request.Traveler.Nationality;
                }
            }

            requestData.TravelerData.SecureTraveler = new List<TravelerSecure>();
            if (!string.IsNullOrEmpty(request.Traveler.KnownTravelerNumber))
            {
                requestData.TravelerData.SecureTraveler.Add(
                    new TravelerSecure()
                    {
                        Number = request.Traveler.KnownTravelerNumber,
                        Type = "K"
                    });
            }

            if (!string.IsNullOrEmpty(request.Traveler.RedressNumber))
            {
                requestData.TravelerData.SecureTraveler.Add(
                    new TravelerSecure()
                    {
                        Number = request.Traveler.RedressNumber,
                        Type = "R"
                    });
            }

            requestData.ContactData = new ContactData();

            if (request.Traveler.Phones != null && request.Traveler.Phones.Count > 0)
            {
                if (!string.IsNullOrEmpty(request.Traveler.Phones[0].PhoneNumber) && !string.IsNullOrEmpty(request.Traveler.Phones[0].CountryCode))
                {
                    string invalidPhoneNumberMessage = string.Empty;
                    var tupleRes = ValidatePhoneWithCountryCode(request.Traveler.Phones[0], request.Application, request.SessionId, request.DeviceId, request.Token, invalidPhoneNumberMessage).Result;
                    invalidPhoneNumberMessage = tupleRes.message;
                    if (!tupleRes.returnValue)
                    {
                        throw new MOBUnitedException(invalidPhoneNumberMessage);
                    }

                    requestData.ContactData.AddPhone = new List<AddPhone>()
                    {
                        new AddPhone()
                        {
                            CountryCode =  request.Traveler.Phones[0].CountryCode,
                            CountryPhoneNumber =  request.Traveler.Phones[0].CountryPhoneNumber.Replace("+",""),
                            DeviceType = String.IsNullOrEmpty(request.Traveler.Phones[0].DeviceTypeCode) ? "PH" : request.Traveler.Phones[0].DeviceTypeCode,
                            Number = (request.Traveler.Phones[0].AreaNumber + request.Traveler.Phones[0].PhoneNumber).Trim().Trim('-'),
                            Type = String.IsNullOrEmpty(request.Traveler.Phones[0].ChannelTypeCode) ? "H" : request.Traveler.Phones[0].ChannelTypeCode
                         }
                    };
                }
                
            }

            if (request.Traveler.EmailAddresses != null && request.Traveler.EmailAddresses.Count > 0)
            {
                requestData.ContactData.AddEmail = new List<AddEmail>()
                {
                    new AddEmail()
                    {
                        Address = request.Traveler.EmailAddresses[0].EmailAddress,
                        Type = String.IsNullOrEmpty(request.Traveler.EmailAddresses[0].Channel?.ChannelCode) ? "H" : request.Traveler.EmailAddresses[0].Channel?.ChannelCode
                    }
                };
            }

            if (request.Traveler.AirRewardPrograms != null && request.Traveler.AirRewardPrograms.Count > 0)
            {
                requestData.RewardProgramData = new List<TravelerRewardProgram>()
                {
                    new TravelerRewardProgram()
                    {
                        ProgramId = Convert.ToInt32(request.Traveler.AirRewardPrograms[0].ProgramId),
                        ProgramMemberId = request.Traveler.AirRewardPrograms[0].MemberId
                    }
                };
            }

            return requestData;

            #endregion
        }

        private TravelerAwsRequest GetUpdateTravelerRequestAws(MOBUpdateTravelerRequest request)
        {
            var requestData = new TravelerAwsRequest();
            requestData.TravelerData = new TravelerData()
            {
                BirthDate = DateTime.Parse(request.Traveler.BirthDate).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"),
                FirstName = request.Traveler.FirstName,
                MiddleName = request.Traveler.MiddleName,
                LastName = request.Traveler.LastName,
                Gender = request.Traveler.GenderCode,
                Suffix = request.Traveler.Suffix,
                Title = request.Traveler.Title,
            };

            requestData.TravelerData.TravelerKey = request.Traveler.Key;
            requestData.TravelerData.UpdateId = request.Traveler.CustomerId.ToString();

            if (IsEnabledNationalityAndResidence(false, request.Application.Id, request.Application.Version.Major))
            {
                if (request.Traveler != null && !string.IsNullOrEmpty(request.Traveler.CountryOfResidence) && request.Traveler.CountryOfResidence.ToUpper().Equals("OTHER"))
                {
                    requestData.TravelerData.CountryOfResidence = null;
                }
                else
                {
                    requestData.TravelerData.CountryOfResidence = request.Traveler.CountryOfResidence;
                }

                if (request.Traveler != null && !string.IsNullOrEmpty(request.Traveler.Nationality) && request.Traveler.Nationality.ToUpper().Equals("OTHER"))
                {
                    requestData.TravelerData.Nationality = null;
                }
                else
                {
                    requestData.TravelerData.Nationality = request.Traveler.Nationality;
                }
            }

            requestData.TravelerData.SecureTraveler = new List<TravelerSecure>();
            if (!string.IsNullOrEmpty(request.Traveler.KnownTravelerNumber))
            {
                requestData.TravelerData.SecureTraveler.Add(
                    new TravelerSecure()
                    {
                        Number = request.Traveler.KnownTravelerNumber,
                        Type = "K"
                    });
            }

            if (!string.IsNullOrEmpty(request.Traveler.RedressNumber))
            {
                requestData.TravelerData.SecureTraveler.Add(
                    new TravelerSecure()
                    {
                        Number = request.Traveler.RedressNumber,
                        Type = "R"
                    });
            }

            return requestData;
        }

        public async Task<(List<MOBCPTraveler> mobTravelersOwnerFirstInList, bool isProfileOwnerTSAFlagOn, List<MOBKVP> savedTravelersMPList)> PopulateTravelersV2(List<TravelerProfileResponse> travelers, string mileagePluNumber, bool isProfileOwnerTSAFlagOn, bool isGetCreditCardDetailsCall, MOBCPProfileRequest request, string sessionid, bool getMPSecurityDetails = false, string path = "")
        {
            var savedTravelersMPList = new List<MOBKVP>();
            List<MOBCPTraveler> mobTravelers = null;
            List<MOBCPTraveler> mobTravelersOwnerFirstInList = null;
            MOBCPTraveler profileOwnerDetails = new MOBCPTraveler();
            OwnerResponseModel profileOwnerResponse = new OwnerResponseModel();
            United.CorporateDirect.Models.CustomerProfile.CorpProfileResponse corpProfileResponse = new United.CorporateDirect.Models.CustomerProfile.CorpProfileResponse();
            if (travelers != null && travelers.Count > 0)
            {
                mobTravelers = new List<MOBCPTraveler>();
                int i = 0;
                var persistedReservation = !getMPSecurityDetails ? await PersistedReservation(request) : new Reservation();

                foreach (TravelerProfileResponse traveler in travelers)
                {
                    #region
                    MOBCPTraveler mobTraveler = new MOBCPTraveler();
                    mobTraveler.PaxIndex = i; i++;
                    mobTraveler.CustomerId = Convert.ToInt32(traveler.Profile?.CustomerId);
                    if (traveler.Profile?.ProfileOwnerIndicator == true)
                    {
           
                        profileOwnerResponse = await _sessionHelperService.GetSession<OwnerResponseModel>(request.SessionId, ObjectNames.CSLGetProfileOwnerResponse, new List<string> { request.SessionId, ObjectNames.CSLGetProfileOwnerResponse }).ConfigureAwait(false); 
                        mobTraveler.CustomerMetrics = PopulateCustomerMetrics(profileOwnerResponse);
                        mobTraveler.MileagePlus = PopulateMileagePlusV2(profileOwnerResponse, request.MileagePlusNumber);
                        mobTraveler.IsDeceased = profileOwnerResponse?.MileagePlus?.Data?.IsDeceased == true;
                        mobTraveler._employeeId = traveler.Profile?.EmployeeId;

                    }
                    if (traveler.Profile?.BirthDate != null)
                    {
                        mobTraveler.BirthDate = GeneralHelper.FormatDateOfBirth(traveler.Profile.BirthDate);
                        if (mobTraveler.BirthDate == "01/01/1")
                            mobTraveler.BirthDate = null;
                    }
                    if (_configuration.GetValue<bool>("EnableNationalityAndCountryOfResidence"))
                    {
                        if (persistedReservation != null && persistedReservation.ShopReservationInfo2 != null && persistedReservation.ShopReservationInfo2.InfoNationalityAndResidence != null
                            && persistedReservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence)
                        {
                            if (string.IsNullOrEmpty(traveler.CustomerAttributes?.CountryofResidence) || string.IsNullOrEmpty(traveler.CustomerAttributes?.Nationality))
                            {
                                mobTraveler.Message = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage");
                            }
                        }
                        mobTraveler.Nationality = traveler.CustomerAttributes?.Nationality;
                        mobTraveler.CountryOfResidence = traveler.CustomerAttributes?.CountryofResidence;
                    }

                    mobTraveler.FirstName = traveler.Profile.FirstName;
                    mobTraveler.GenderCode = traveler.Profile?.Gender.ToString() == "Undefined" ? "" : traveler.Profile.Gender.ToString();
                    mobTraveler.IsProfileOwner = traveler.Profile.ProfileOwnerIndicator;
                    mobTraveler.Key = traveler.Profile.TravelerKey;
                    mobTraveler.LastName = traveler.Profile.LastName;
                    mobTraveler.MiddleName = traveler.Profile.MiddleName;
                    if (mobTraveler.MileagePlus != null)
                    {
                        mobTraveler.MileagePlus.MpCustomerId = Convert.ToInt32(traveler.Profile.CustomerId);

                        if (request != null && ConfigUtility.IncludeTravelBankFOP(request.Application.Id, request.Application.Version.Major))
                        {
                            Session session = new Session();
                            string cslLoyaltryBalanceServiceResponse = await _loyaltyUCBService.GetLoyaltyBalance(request.Token, request.MileagePlusNumber, request.SessionId);
                            if (!string.IsNullOrEmpty(cslLoyaltryBalanceServiceResponse))
                            {
                                United.TravelBank.Model.BalancesDataModel.BalanceResponse PlusPointResponse = JsonSerializer.NewtonSoftDeserialize<United.TravelBank.Model.BalancesDataModel.BalanceResponse>(cslLoyaltryBalanceServiceResponse);
                                United.TravelBank.Model.BalancesDataModel.Balance tbbalance = PlusPointResponse.Balances.FirstOrDefault(tb => tb.ProgramCurrencyType == United.TravelBank.Model.TravelBankConstants.ProgramCurrencyType.UBC);
                                if (tbbalance != null && tbbalance.TotalBalance > 0)
                                {
                                    mobTraveler.MileagePlus.TravelBankBalance = (double)tbbalance.TotalBalance;
                                }
                            }
                        }
                    }

                    mobTraveler.ProfileId = Convert.ToInt32(traveler.Profile.ProfileId);
                    mobTraveler.ProfileOwnerId = Convert.ToInt32(traveler.Profile.ProfileOwnerId);
                    bool isTSAFlagOn = false;
                    if (traveler.SecureTravelers != null)
                    {
                        if (request == null)
                        {
                            request = new MOBCPProfileRequest();
                            request.SessionId = string.Empty;
                            request.DeviceId = string.Empty;
                            request.Application = new MOBApplication() { Id = 0 };
                        }
                        else if (request.Application == null)
                        {
                            request.Application = new MOBApplication() { Id = 0 };
                        }
                        mobTraveler.SecureTravelers = PopulatorSecureTravelersV2(traveler.SecureTravelers, ref isTSAFlagOn, i >= 2, request.SessionId, request.Application.Id, request.DeviceId);
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
                    mobTraveler.Suffix = traveler.Profile.Suffix;
                    mobTraveler.Title = traveler.Profile.Title;
                    mobTraveler.TravelerTypeCode = GetTravelerTypeCode(traveler.Profile?.TravelerTypeCode);
                    mobTraveler.TravelerTypeDescription = traveler.Profile?.TravelerTypeDescription;

                    mobTraveler.IsEligibleForExtraSeatSelection = IsExtraSeatFeatureEnabled(request.Application.Id, request.Application.Version.Major, traveler.Profile?.TravelerTypeCode);

                    if (persistedReservation != null && persistedReservation.ShopReservationInfo2 != null && persistedReservation.ShopReservationInfo2.TravelerTypes != null
                        && persistedReservation.ShopReservationInfo2.TravelerTypes.Count > 0)
                    {
                        if (traveler.Profile?.BirthDate != null)
                        {
                            if (EnableYADesc() && persistedReservation.ShopReservationInfo2.IsYATravel)
                            {
                                mobTraveler.PTCDescription = GetYAPaxDescByDOB();
                            }
                            else
                            {
                                mobTraveler.PTCDescription = GetPaxDescriptionByDOB(traveler.Profile.BirthDate.ToString(), persistedReservation.Trips[0].FlattenedFlights[0].Flights[0].DepartDate);
                            }
                        }
                    }
                    else
                    {
                        if (EnableYADesc() && persistedReservation != null && persistedReservation.ShopReservationInfo2 != null && persistedReservation.ShopReservationInfo2.IsYATravel)
                        {
                            mobTraveler.PTCDescription = GetYAPaxDescByDOB();
                        }
                    }                 
                    if (traveler != null)
                    {
                        if (mobTraveler.MileagePlus != null)
                        {
                            mobTraveler.CurrentEliteLevel = mobTraveler.MileagePlus.CurrentEliteLevel;
                            //mobTraveler.AirRewardPrograms = GetTravelerLoyaltyProfile(traveler.AirPreferences, traveler.MileagePlus.CurrentEliteLevel);
                        }
                    }

                    mobTraveler.AirRewardPrograms = GetTravelerRewardPgrograms(traveler.RewardPrograms, mobTraveler.CurrentEliteLevel);
                    mobTraveler.Phones = PopulatePhonesV2(traveler, true);
                    if (mobTraveler.IsProfileOwner)
                    {
                        // These Phone and Email details for Makre Reseravation Phone and Email reason is mobTraveler.Phones = PopulatePhones(traveler.Phones,true) will get only day of travel contacts to register traveler & edit traveler.
                        mobTraveler.ReservationPhones = PopulatePhonesV2(traveler, false);
                        mobTraveler.ReservationEmailAddresses = PopulateEmailAddressesV2(traveler.Emails, false);

                        // Added by Hasnan - #53484. 10/04/2017
                        // As per the Bug 53484:PINPWD: iOS and Android - Phone number is blank in RTI screen after booking from newly created account.
                        // If mobTraveler.Phones is empty. Then it newly created account. Thus returning mobTraveler.ReservationPhones as mobTraveler.Phones.
                        if (_configuration.GetValue<bool>("EnableDayOfTravelEmail") || string.IsNullOrEmpty(path) || !path.ToUpper().Equals("BOOKING"))
                        {
                            if (mobTraveler.Phones.Count == 0)
                            {
                                mobTraveler.Phones = mobTraveler.ReservationPhones;
                            }
                        }
                        #region Corporate Leisure(ProfileOwner must travel)//Client will use the IsMustRideTraveler flag to auto select the travel and not allow to uncheck the profileowner on the SelectTraveler Screen.
                        if (_configuration.GetValue<bool>("EnableCorporateLeisure"))
                        {
                            if (persistedReservation?.ShopReservationInfo2 != null && persistedReservation.ShopReservationInfo2.TravelType == TravelType.CLB.ToString() && IsCorporateLeisureFareSelected(persistedReservation.Trips))
                            {
                                mobTraveler.IsMustRideTraveler = true;
                            }
                        }
                        #endregion Corporate Leisure
                    }
                    if (mobTraveler.IsProfileOwner && getMPSecurityDetails) //**PINPWD//mobTraveler.IsProfileOwner && request == null Means GetProfile and Populate is for MP PIN PWD Path
                    {
                        mobTraveler.ReservationEmailAddresses = PopulateAllEmailAddressesV2(traveler.Emails);
                    }
                    mobTraveler.AirPreferences = PopulateAirPrefrencesV2(traveler);
                    if (!getMPSecurityDetails && request?.Application?.Version != null && string.IsNullOrEmpty(request?.Flow) && IsInternationalBillingAddress_CheckinFlowEnabled(request.Application))
                    {
                        try
                        {
                            MOBShoppingCart mobShopCart = new MOBShoppingCart();
                            mobShopCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, mobShopCart.ObjectName, new List<string> { request.SessionId, mobShopCart.ObjectName });
                            if (mobShopCart != null && !string.IsNullOrEmpty(mobShopCart.Flow) && mobShopCart.Flow == FlowType.CHECKIN.ToString())
                            {
                                request.Flow = mobShopCart.Flow;
                            }
                        }
                        catch { }
                    }
                    mobTraveler.Addresses = PopulateTravelerAddressesV2(traveler.Addresses, request?.Application, request?.Flow);

                    if (_configuration.GetValue<bool>("EnableDayOfTravelEmail") && !string.IsNullOrEmpty(path) && path.ToUpper().Equals("BOOKING"))
                    {
                        mobTraveler.EmailAddresses = PopulateEmailAddressesV2(traveler.Emails, true);
                    }
                    else
                    if (!getMPSecurityDetails)
                    {
                        mobTraveler.EmailAddresses = PopulateEmailAddressesV2(traveler.Emails, false);
                    }
                    else
                    {
                        mobTraveler.EmailAddresses = PopulateMPSecurityEmailAddressesV2(traveler.Emails);
                    }
                    if (mobTraveler.IsProfileOwner == true)
                    {                     
                        if (!getMPSecurityDetails)
                        {
                            if (mobTraveler.CreditCards == null)
                            {
                                mobTraveler.CreditCards = new List<MOBCreditCard>();
                            }
                            var corpCreditCards = new List<MOBCreditCard>();
                            var isCardMandatory = false;
                            if (IsCorpBookingPath)
                            {
                                bool isEnableU4BCorporateBooking = request != null && request.Application != null ? IsEnableU4BCorporateBooking(request.Application.Id, request.Application.Version?.Major) : false;
                                string sessionId = isEnableU4BCorporateBooking ? request.DeviceId + request.MileagePlusNumber : request.SessionId;
                                corpProfileResponse = await _sessionHelperService.GetSession<United.CorporateDirect.Models.CustomerProfile.CorpProfileResponse>(sessionId, ObjectNames.CSLCorpProfileResponse, new List<string> { sessionId, ObjectNames.CSLCorpProfileResponse }).ConfigureAwait(false);
                                corpCreditCards = await _profileCreditCard.PopulateCorporateCreditCards(isGetCreditCardDetailsCall, mobTraveler.Addresses, persistedReservation, request);

                                if (corpCreditCards != null && corpCreditCards.Any(s => s.IsMandatory == true))
                                {
                                    isCardMandatory = true;
                                    mobTraveler.CreditCards = corpCreditCards;
                                }
                            }
                            if (!isCardMandatory)
                            {
                                mobTraveler.CreditCards = await _profileCreditCard.PopulateCreditCards(isGetCreditCardDetailsCall, mobTraveler.Addresses, request);
                                if (corpCreditCards != null && corpCreditCards.Count > 0)
                                {
                                    mobTraveler.CreditCards.AddRange(corpCreditCards);
                                }
                            }
                        }
                        if (IsCorpBookingPath && corpProfileResponse?.Profiles != null && corpProfileResponse.Profiles.Count() > 0)
                        {
                            var corporateTraveler = corpProfileResponse?.Profiles[0].Travelers.FirstOrDefault();
                            if (corporateTraveler != null)
                            {
                                if (corporateTraveler.Addresses != null)
                                {
                                    var corporateAddress = PopulateCorporateTravelerAddresses(corporateTraveler.Addresses, request.Application, request.Flow);
                                    if (mobTraveler.Addresses == null)
                                        mobTraveler.Addresses = new List<MOBAddress>();
                                    mobTraveler.Addresses.AddRange(corporateAddress);
                                }
                                if (corporateTraveler.EmailAddresses != null)
                                {
                                    var corporateEmailAddresses = PopulateCorporateEmailAddresses(corporateTraveler.EmailAddresses, false);
                                    mobTraveler.ReservationEmailAddresses = new List<MOBEmail>();
                                    mobTraveler.ReservationEmailAddresses.AddRange(corporateEmailAddresses);
                                }
                                if (corporateTraveler.Phones != null)
                                {
                                    var corporatePhones = PopulateCorporatePhones(corporateTraveler.Phones, false);
                                    mobTraveler.ReservationPhones = new List<MOBCPPhone>();
                                    mobTraveler.ReservationPhones.AddRange(corporatePhones);
                                }
                                if (corporateTraveler.AirPreferences != null)
                                {
                                    var corporateAirpreferences = PopulateCorporateAirPrefrences(corporateTraveler.AirPreferences);
                                    if (mobTraveler.AirPreferences == null)
                                        mobTraveler.AirPreferences = new List<MOBPrefAirPreference>();
                                    mobTraveler.AirPreferences.AddRange(corporateAirpreferences);
                                }
                            }

                        }
                    }
                    if (mobTraveler.IsTSAFlagON || string.IsNullOrEmpty(mobTraveler.FirstName) || string.IsNullOrEmpty(mobTraveler.LastName) || string.IsNullOrEmpty(mobTraveler.GenderCode) || string.IsNullOrEmpty(mobTraveler.BirthDate)) //|| mobTraveler.Phones == null || (mobTraveler.Phones != null && mobTraveler.Phones.Count == 0)
                    {
                        mobTraveler.Message = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage");
                    }
                    if (mobTraveler.IsProfileOwner)
                    {
                        profileOwnerDetails = mobTraveler;
                    }
                    else
                    {
                        #region
                        if (mobTraveler.AirRewardPrograms != null && mobTraveler.AirRewardPrograms.Count > 0)
                        {
                            var airRewardProgramList = (from program in mobTraveler.AirRewardPrograms
                                                        where program.CarrierCode.ToUpper().Trim() == "UA"
                                                        select program).ToList();

                            if (airRewardProgramList != null && airRewardProgramList.Count > 0)
                            {
                                savedTravelersMPList.Add(new MOBKVP() { Key = mobTraveler.CustomerId.ToString(), Value = airRewardProgramList[0].MemberId });
                            }
                        }
                        #endregion
                        mobTravelers.Add(mobTraveler);
                    }
                    #endregion
                }
            }
            mobTravelersOwnerFirstInList = new List<MOBCPTraveler>();
            mobTravelersOwnerFirstInList.Add(profileOwnerDetails);
            if (!IsCorpBookingPath || IsArrangerBooking)
            {
                mobTravelersOwnerFirstInList.AddRange(mobTravelers);
            }

            if (await _featureToggles.IsEnableU4BForMultipax(request.Application.Id, request.Application.Version?.Major, null))
            {
                if(!IsArrangerBooking && IsCorpBookingPath)
                   mobTravelersOwnerFirstInList?.Where(a => a?.IsProfileOwner == true).ToList()?.FirstOrDefault(x => x.IsMustRideTraveler = true);
                bool isMultiPaxAllowed = corpProfileResponse?.Profiles?.Select(a => a?.CorporateData?.IsMultiPaxAllowed)?.FirstOrDefault() ?? false;
                if (IsCorpBookingPath && !IsArrangerBooking && isMultiPaxAllowed)
                {
                    MOBSHOPReservation persistedMOBSHOPReservation = new MOBSHOPReservation();
                    try
                    {
                        var UCSID = corpProfileResponse?.Profiles?.Select(a => a.CorporateData?.UCSID)?.FirstOrDefault() ?? 0;
                        List<string> profileLoyaltyId = null;
                        profileLoyaltyId = travelers?.Where(a => !string.IsNullOrEmpty(a?.Profile?.LoyaltyId)).Select(b => b?.Profile?.LoyaltyId.ToString()).ToList();
                        if (UCSID != 0 && profileLoyaltyId != null)
                        {
                            var _corpMpNumberValidationResponse = await _corporateProfile.CorpMpNumberValidation(request.Token, request.SessionId, profileLoyaltyId, UCSID);
                            if (_corpMpNumberValidationResponse != null && (_corpMpNumberValidationResponse.Errors == null || _corpMpNumberValidationResponse.Errors.Count == 0))
                            {
                                var corpMpNumber = new List<string>();
                                corpMpNumber = _corpMpNumberValidationResponse.Data?.Where(a => a?.IsVerified == true).Select(a => a?.MpNumber).ToList();
                                if (corpMpNumber?.Count > 0)
                                {
                                    var travelersCustIDlist = travelers?.Where(a => corpMpNumber.Contains(a?.Profile?.LoyaltyId)).Select(a => a?.Profile?.CustomerId).ToList();
                                    if (travelersCustIDlist?.Count > 0)
                                    {
                                        var mobTravelerMulpaxList = mobTravelers?.Where(a => travelersCustIDlist.Contains(a.CustomerId)).ToList();
                                        if (mobTravelerMulpaxList?.Count > 0)
                                            mobTravelersOwnerFirstInList.AddRange(mobTravelerMulpaxList);
                                    }
                                }
                                await UpdateCorporateMultipaxDataInReservationSession(sessionid, false, isMultiPaxAllowed, request, request.Token).ConfigureAwait(false); ;
                            }
                            else
                            {
                                mobTravelersOwnerFirstInList.AddRange(mobTravelers);
                                await UpdateCorporateMultipaxDataInReservationSession(sessionid, true, isMultiPaxAllowed, request, request.Token).ConfigureAwait(false);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        mobTravelersOwnerFirstInList.AddRange(mobTravelers);
                        await UpdateCorporateMultipaxDataInReservationSession(sessionid, true, isMultiPaxAllowed, request, request.Token).ConfigureAwait(false);
                    }
                }
            }

            return (mobTravelersOwnerFirstInList,isProfileOwnerTSAFlagOn,savedTravelersMPList);
        }
        private async System.Threading.Tasks.Task UpdateCorporateMultipaxDataInReservationSession(string sessionid, bool IsCorpMpNumberValidationFailed, bool isMultiPaxAllowedFromCorpProfileResponse, MOBRequest request, string token)
        {
            var bookingReservation = new Reservation();
            bookingReservation = await _sessionHelperService.GetSession<Reservation>(sessionid, bookingReservation.ObjectName, new List<string> { sessionid, bookingReservation.ObjectName }).ConfigureAwait(false);
            if (bookingReservation?.ShopReservationInfo2 != null)
            {
                if (IsCorpMpNumberValidationFailed)
                {
                    bookingReservation.ShopReservationInfo2.IsCorpMpNumberValidationFailed = IsCorpMpNumberValidationFailed;
                    List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, sessionid, token, _configuration.GetValue<string>("U4BCorporateContentMessageGroupName"), "U4BCorporateContentMessageCache");
                    InfoWarningMessages warningMessage = _corporateProfile.CorpMultiPaxinfoWarningMessages(lstMessages);
                    if (bookingReservation.ShopReservationInfo2.InfoWarningMessages == null)
                        bookingReservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();
                    if (!bookingReservation.ShopReservationInfo2.InfoWarningMessages?.Any(a => a?.Order == MOBINFOWARNINGMESSAGEORDER.CORPORATEUNENROLLEDTRAVELER.ToString()) ?? false)
                    {
                        bookingReservation.ShopReservationInfo2.InfoWarningMessages.Add(warningMessage);
                        bookingReservation.ShopReservationInfo2.InfoWarningMessages = bookingReservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)System.Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                    }
                }
                bookingReservation.ShopReservationInfo2.IsMultiPaxAllowed = isMultiPaxAllowedFromCorpProfileResponse;

            }
            await _sessionHelperService.SaveSession<Reservation>(bookingReservation, sessionid, new List<string> { sessionid, bookingReservation.ObjectName }, bookingReservation.ObjectName).ConfigureAwait(false);
        }
        private bool IsEnableU4BCorporateBooking(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableU4BCorporateBooking") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableU4BCorporateBooking_AppVersion"), _configuration.GetValue<string>("IPhone_EnableU4BCorporateBooking_AppVersion"));
        }

        private bool IsExtraSeatFeatureEnabled(int appId, string appVersion, string travelerTypeCode)
        {
            if(_configuration.GetValue<bool>("EnableExtraSeatsFeature")
                && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "Android_EnableExtraSeatsFeature_AppVersion", "IPhone_EnableExtraSeatsFeature_AppVersion", "", "", true, _configuration))
            {
                var inEligibleTravelTypeCodes = _configuration.GetValue<string>("InEligibleTravelTypeCodesForExtraSeat")?.Split('|').ToList();
                if (!string.IsNullOrEmpty(travelerTypeCode) && inEligibleTravelTypeCodes != null && inEligibleTravelTypeCodes.Count() > 0)
                    return !inEligibleTravelTypeCodes.Contains(travelerTypeCode?.ToUpper());
            }
            return true;
        }

        public MOBCPMileagePlus PopulateMileagePlusV2(OwnerResponseModel profileOwnerResponse, string mileageplusId)
        {
            if (profileOwnerResponse?.MileagePlus?.Data != null)
            {
                MOBCPMileagePlus mileagePlus = null;
                var mileagePlusData = profileOwnerResponse.MileagePlus.Data;

                mileagePlus = new MOBCPMileagePlus();
                var balance = profileOwnerResponse.MileagePlus.Data.Balances?.FirstOrDefault(balnc => (int)balnc.Currency == 5);
                mileagePlus.AccountBalance = Convert.ToInt32(balance.Amount);
                mileagePlus.ActiveStatusCode = mileagePlusData.AccountStatus;
                mileagePlus.ActiveStatusDescription = mileagePlusData.AccountStatusDescription;
                mileagePlus.AllianceEliteLevel = mileagePlusData.StarAllianceTierLevel;
                mileagePlus.ClosedStatusCode = mileagePlusData.OpenClosedStatusCode;
                mileagePlus.ClosedStatusDescription = mileagePlusData.OpenClosedStatusDescription;
                mileagePlus.CurrentEliteLevel = mileagePlusData.MPTierLevel;
                if (mileagePlus.CurrentEliteLevelDescription != null)
                {
                    mileagePlus.CurrentEliteLevelDescription = mileagePlusData.MPTierLevelDescription.ToString().ToUpper() == "NON-ELITE" ? "General member" : mileagePlusData.MPTierLevelDescription;
                }
                mileagePlus.CurrentYearMoneySpent = mileagePlusData.CurrentYearMoneySpent;
                mileagePlus.EliteMileageBalance = Convert.ToInt32(mileagePlusData.EliteMileageBalance);         
                mileagePlus.EnrollDate = mileagePlusData.EnrollDate.GetValueOrDefault().ToString("MM/dd/yyyy");
                mileagePlus.EnrollSourceCode = mileagePlusData.EnrollSourceCode;
                mileagePlus.EnrollSourceDescription = mileagePlusData.EnrollSourceDescription;
                mileagePlus.FutureEliteDescription = mileagePlusData.NextStatusLevelDescription;
                mileagePlus.FutureEliteLevel = mileagePlusData.NextStatusLevel;
                mileagePlus.InstantEliteExpirationDate = mileagePlusData.NextStatusLevelDescription;
                mileagePlus.IsCEO = mileagePlusData.CEO;
                mileagePlus.IsClosedPermanently = mileagePlusData.IsClosedPermanently;
                mileagePlus.IsClosedTemporarily = mileagePlusData.IsClosedTemporarily;         
                mileagePlus.IsLockedOut = mileagePlusData.IsLockedOut;
                mileagePlus.IsUnitedClubMember = mileagePlusData.IsPClubMember;
                mileagePlus.LastActivityDate = mileagePlusData.LastActivityDate.GetValueOrDefault().ToString("MM/dd/yyyy");
                mileagePlus.LastFlightDate = mileagePlusData.LastFlightDate.GetValueOrDefault().ToString("MM/dd/yyyy");
                mileagePlus.LastStatementDate = mileagePlusData.LastStatementDate.GetValueOrDefault().ToString("MM/dd/yyyy");
                mileagePlus.LifetimeEliteMileageBalance = Convert.ToInt32(mileagePlusData.LifetimeMiles);
                mileagePlus.MileagePlusId = mileageplusId;             
                return mileagePlus;
            }
            else
            {
                throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

        }
        public async Task<OwnerResponseModel> GetProfileOwnerInfo(String token, string sessionId, string mileagePlusNumber)
        {
            var response = await _customerProfileOwnerService.GetProfileOwnerInfo<OwnerResponseModel>(token, sessionId, mileagePlusNumber);
            if (response != null && response.MileagePlus != null)
            {
                return response;
            }
            else
            {
                throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
        }
        public List<MOBCPSecureTraveler> PopulatorSecureTravelersV2(SecureTravelerResponseData secureTravelerResponseData, ref bool isTSAFlag, bool correctDate, string sessionID, int appID, string deviceID)
        {
            List<MOBCPSecureTraveler> mobSecureTravelers = null;
            try
            {
                if (secureTravelerResponseData?.SecureTraveler != null)
                {
                    mobSecureTravelers = new List<MOBCPSecureTraveler>();
                    var secureTraveler = secureTravelerResponseData.SecureTraveler;
                    if (!_configuration.GetValue<bool>("DisableUCBKTNFix") && secureTraveler.DocumentType == null) //MOBILE-26294 : Before UCB Migration documentype used to be empty .But after UCB Migration we are getting it as Null Due to that we are not building the KTN number.Looks for the bug number for more details.
                    {
                        secureTraveler.DocumentType = "";
                    }
                    if (secureTraveler.DocumentType != null && secureTraveler.DocumentType.Trim().ToUpper() != "X")
                    {
                        #region
                        MOBCPSecureTraveler mobSecureTraveler = new MOBCPSecureTraveler();
                        if (correctDate)
                        {
                            DateTime tempBirthDate = secureTraveler.BirthDate.GetValueOrDefault().AddHours(1);
                            mobSecureTraveler.BirthDate = tempBirthDate.ToString("MM/dd/yyyy", CultureInfo.CurrentCulture);
                        }
                        else
                        {
                            mobSecureTraveler.BirthDate = secureTraveler.BirthDate.GetValueOrDefault().ToString("MM/dd/yyyy", CultureInfo.CurrentCulture);
                        }
                        mobSecureTraveler.CustomerId = Convert.ToInt32(secureTraveler.CustomerId);
                        mobSecureTraveler.DecumentType = secureTraveler.DocumentType;
                        mobSecureTraveler.Description = secureTraveler.Description;
                        mobSecureTraveler.FirstName = secureTraveler.FirstName;
                        mobSecureTraveler.Gender = secureTraveler.Gender;
                        // mobSecureTraveler.Key = secureTraveler.Key;No longer needed confirmed from service
                        mobSecureTraveler.LastName = secureTraveler.LastName;
                        mobSecureTraveler.MiddleName = secureTraveler.MiddleName;
                        mobSecureTraveler.SequenceNumber = (int)secureTraveler.SequenceNumber;
                        mobSecureTraveler.Suffix = secureTraveler.Suffix;
                        if (secureTravelerResponseData.SupplementaryTravelInfos != null)
                        {
                            foreach (SupplementaryTravelDocsDataMembers supplementaryTraveler in secureTravelerResponseData.SupplementaryTravelInfos)
                            {
                                if (supplementaryTraveler.Type == "K")
                                {
                                    mobSecureTraveler.KnownTravelerNumber = supplementaryTraveler.Number;
                                }
                                if (supplementaryTraveler.Type == "R")
                                {
                                    mobSecureTraveler.RedressNumber = supplementaryTraveler.Number;
                                }
                            }
                        }
                        if (!isTSAFlag && secureTraveler.DocumentType.Trim().ToUpper() == "U")
                        {
                            isTSAFlag = true;
                        }
                        if (secureTraveler.DocumentType.Trim().ToUpper() == "C" || secureTraveler.DocumentType.Trim() == "") // This is to get only Customer Cleared Secure Traveler records
                        {
                            mobSecureTravelers = new List<MOBCPSecureTraveler>();
                            mobSecureTravelers.Add(mobSecureTraveler);
                        }
                        else
                        {
                            mobSecureTravelers.Add(mobSecureTraveler);
                        }
                        #endregion
                    }

                }

            }
            catch (Exception ex)
            {
                try
                {
                    _logger.LogError("PopulatorSecureTravelers {@Exception} for {@SecureTravelerResponseData}", JsonConvert.SerializeObject(ex), JsonConvert.SerializeObject(secureTravelerResponseData));
                }
                catch { }
            }

            return mobSecureTravelers;
        }

        public List<MOBCPPhone> PopulatePhonesV2(TravelerProfileResponse traveler, bool onlyDayOfTravelContact)
        {
            List<MOBCPPhone> mobCPPhones = new List<MOBCPPhone>();
            bool isCorpPhonePresent = false;
            var phones = traveler.Phones;
            if (phones != null && phones.Count > 0)
            {
                MOBCPPhone primaryMobCPPhone = null;
                CultureInfo ci = GeneralHelper.EnableUSCultureInfo();
                int co = 0;
                foreach (United.Mobile.Model.CSLModels.Phone phone in phones)
                {
                    #region As per Wade Change want to filter out to return only Primary Phone to client if not primary phone exist return the first one from the list.So lot of work at client side will save time. Sep 15th 2014
                    MOBCPPhone mobCPPhone = new MOBCPPhone();
                    co = co + 1;
                    mobCPPhone.AreaNumber = phone.AreaCode;
                    mobCPPhone.PhoneNumber = phone.Number;
                    //mobCPPhone.Attention = phone.Attention;No longer needed confirmed from service
                    mobCPPhone.ChannelCode = "P";
                    mobCPPhone.ChannelCodeDescription = "Phone";
                    mobCPPhone.ChannelTypeCode = phone.Type.ToString();
                    mobCPPhone.ChannelTypeDescription = phone.TypeDescription;
                    mobCPPhone.ChannelTypeSeqNumber = phone.SequenceNumber;
                    mobCPPhone.CountryCode = phone.CountryCode;
                    //mobCPPhone.CountryCode = GetAccessCode(phone.CountryCode);
                    mobCPPhone.CountryPhoneNumber = phone.CountryPhoneNumber;
                    mobCPPhone.CustomerId = Convert.ToInt32(traveler.Profile.CustomerId);
                    mobCPPhone.Description = phone.Remark;
                    mobCPPhone.DiscontinuedDate = Convert.ToString(phone.DiscontinuedDate);
                    mobCPPhone.EffectiveDate = Convert.ToString(phone.EffectiveDate);
                    mobCPPhone.ExtensionNumber = phone.ExtensionNumber;
                    mobCPPhone.IsPrimary = phone.PrimaryIndicator;
                    mobCPPhone.IsPrivate = phone.IsPrivate;
                    mobCPPhone.IsProfileOwner = traveler.Profile.ProfileOwnerIndicator;
                    mobCPPhone.Key = phone.Key;
                    mobCPPhone.LanguageCode = phone.LanguageCode;
                    // mobCPPhone.PagerPinNumber = phone.PagerPinNumber;
                    // mobCPPhone.SharesCountryCode = phone.SharesCountryCode;
                    mobCPPhone.WrongPhoneDate = Convert.ToString(phone.WrongPhoneDate);
                    mobCPPhone.DeviceTypeCode = phone.DeviceType.ToString();
                    mobCPPhone.DeviceTypeDescription = phone.TypeDescription;

                    mobCPPhone.IsDayOfTravel = phone.DayOfTravelNotification;

                    if (phone.DayOfTravelNotification)
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
                        if (phone.DayOfTravelNotification)
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
        public List<MOBEmail> PopulateEmailAddressesV2(List<United.Mobile.Model.CSLModels.Email> emailAddresses, bool onlyDayOfTravelContact)
        {
            #region
            List<MOBEmail> mobEmailAddresses = new List<MOBEmail>();
            bool isCorpEmailPresent = false;

            if (emailAddresses != null && emailAddresses.Count > 0)
            {
                if (IsCorpBookingPath)
                {
                    //As per Business / DotCom Kalpen; we are removing the condition for checking the Effectivedate and Discontinued date
                    var corpIndex = emailAddresses.FindIndex(x => x.TypeDescription != null && x.TypeDescription.ToLower() == "corporate" && x.Address != null && x.Address.Trim() != "");
                    if (corpIndex >= 0)
                        isCorpEmailPresent = true;
                }

                MOBEmail primaryEmailAddress = null;
                int co = 0;
                foreach (United.Mobile.Model.CSLModels.Email email in emailAddresses)
                {
                    if (isCorpEmailPresent && !onlyDayOfTravelContact && email.TypeDescription.ToLower() == "corporate")
                    {
                        primaryEmailAddress = new MOBEmail();
                        email.PrimaryIndicator = true;
                        primaryEmailAddress.Key = email.Key;
                        primaryEmailAddress.Channel = new SHOPChannel();
                        primaryEmailAddress.EmailAddress = email.Address;
                        primaryEmailAddress.Channel.ChannelCode = "E";
                        primaryEmailAddress.Channel.ChannelDescription = "Email";
                        primaryEmailAddress.Channel.ChannelTypeCode = email.Type.ToString();
                        primaryEmailAddress.Channel.ChannelTypeDescription = email.TypeDescription;
                        primaryEmailAddress.IsDefault = email.PrimaryIndicator;
                        primaryEmailAddress.IsPrimary = email.PrimaryIndicator;
                        primaryEmailAddress.IsPrivate = email.IsPrivate;
                        primaryEmailAddress.IsDayOfTravel = email.DayOfTravelNotification;
                        if (!email.DayOfTravelNotification)
                        {
                            break;
                        }

                    }
                    else if (isCorpEmailPresent && !onlyDayOfTravelContact && email.TypeDescription.ToLower() != "corporate")
                    {
                        continue;
                    }
                    
                    //Fix for CheckOut ArgNull Exception - Empty EmailAddress with null EffectiveDate & DiscontinuedDate for Corp Account Revenue Booking (MOBILE-9873) - Shashank : Added OR condition to allow CorporateAccount ProfileOwner.
                    if ((email.EffectiveDate <= DateTime.UtcNow && email.DiscontinuedDate >= DateTime.UtcNow) ||
                            (email.TypeDescription.ToLower() == "corporate"
                            && email.PrimaryIndicator == true && primaryEmailAddress.IsNullOrEmpty()))
                    {
                        #region As per Wade Change want to filter out to return only Primary email to client if not primary phone exist return the first one from the list.So lot of work at client side will save time. Sep 15th 2014
                        co = co + 1;
                        MOBEmail e = new MOBEmail();
                        e.Key = email.Key;
                        e.Channel = new SHOPChannel();
                        e.EmailAddress = email.Address;
                        e.Channel.ChannelCode = "E";
                        e.Channel.ChannelDescription = "Email";
                        e.Channel.ChannelTypeCode = email.Type.ToString();
                        e.Channel.ChannelTypeDescription = email.TypeDescription;
                        e.IsDefault = email.PrimaryIndicator;
                        e.IsPrimary = email.PrimaryIndicator;
                        e.IsPrivate = email.IsPrivate;
                        e.IsDayOfTravel = email.DayOfTravelNotification;
                        if (email.DayOfTravelNotification)
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
                            if (email.PrimaryIndicator)
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
        public List<MOBEmail> PopulateAllEmailAddressesV2(List<United.Mobile.Model.CSLModels.Email> emailAddresses)
        {
            #region
            List<MOBEmail> mobEmailAddresses = new List<MOBEmail>();
            if (emailAddresses != null && emailAddresses.Count > 0)
            {
                int co = 0;
                foreach (United.Mobile.Model.CSLModels.Email email in emailAddresses)
                {
                    if (email.EffectiveDate <= DateTime.UtcNow && email.DiscontinuedDate >= DateTime.UtcNow)
                    {
                        #region As per Wade Change want to filter out to return only Primary email to client if not primary phone exist return the first one from the list.So lot of work at client side will save time. Sep 15th 2014
                        co = co + 1;
                        MOBEmail e = new MOBEmail();
                        e.Key = email.Key;
                        e.Channel = new SHOPChannel();
                        e.Channel.ChannelCode = "E";
                        e.Channel.ChannelDescription = "Email";
                        e.Channel.ChannelTypeCode = email.Type.ToString();
                        e.Channel.ChannelTypeDescription = email.TypeDescription;
                        e.EmailAddress = email.Address;
                        e.IsDefault = email.PrimaryIndicator;
                        e.IsPrimary = email.PrimaryIndicator;
                        e.IsPrivate = email.IsPrivate;
                        e.IsDayOfTravel = email.DayOfTravelNotification;
                        mobEmailAddresses.Add(e);
                        #endregion
                    }
                }
            }
            return mobEmailAddresses;
            #endregion
        }
        public List<MOBPrefAirPreference> PopulateAirPrefrencesV2(TravelerProfileResponse traveler)
        {
            var airPreferences = traveler.AirPreferences;
            List<MOBPrefAirPreference> mobAirPrefs = new List<MOBPrefAirPreference>();
            if (airPreferences != null && airPreferences.Count > 0)
            {
                foreach (AirPreferenceDataModel pref in airPreferences)
                {
                    MOBPrefAirPreference mobAirPref = new MOBPrefAirPreference();
                    mobAirPref.AirportCode = pref.AirportCode;
                    mobAirPref.AirportCode = pref.AirportNameLong;
                    mobAirPref.AirportNameShort = pref.AirportNameShort;
                    mobAirPref.AirPreferenceId = pref.AirPreferenceId;
                    mobAirPref.ClassDescription = pref.ClassDescription;
                    mobAirPref.ClassId = pref.ClassID;
                    mobAirPref.CustomerId = traveler.Profile.CustomerId;
                    mobAirPref.EquipmentCode = pref.EquipmentCode;
                    mobAirPref.EquipmentDesc = pref.EquipmentDescription;
                    mobAirPref.EquipmentId = pref.EquipmentID;
                    mobAirPref.IsActive = true;//By default if it is returned it is active confirmed with service team
                    mobAirPref.IsSelected = true;// By default if it is returned it is active confirmed with service team
                    mobAirPref.IsNew = false;// By default if it is returned it is false confirmed with service team
                    mobAirPref.Key = pref.Key;
                    //mobAirPref.LanguageCode = pref.LanguageCode;No longer sent from service confirmed with them
                    mobAirPref.MealCode = pref.MealCode;
                    mobAirPref.MealDescription = pref.MealDescription;
                    mobAirPref.MealId = pref.MealId;
                    // mobAirPref.NumOfFlightsDisplay = pref.NumOfFlightsDisplay;No longer sent from service confirmed with them
                    mobAirPref.ProfileId = traveler.Profile.ProfileId;
                    mobAirPref.SearchPreferenceDescription = pref.SearchPreferenceDescription;
                    mobAirPref.SearchPreferenceId = pref.SearchPreferenceID;
                    //mobAirPref.SeatFrontBack = pref.SeatFrontBack;No longer sent from service confirmed with them
                    mobAirPref.SeatSide = pref.SeatSide;
                    mobAirPref.SeatSideDescription = pref.SeatSideDescription;
                    mobAirPref.VendorCode = pref.VendorCode;//Service confirmed we can hard code this as we dont have any other vendor it is always United airlines
                    mobAirPref.VendorDescription = pref.VendorDescription;//Service confirmed we can hard code this as we dont have any other vendor it is always United airlines
                    mobAirPref.VendorId = pref.VendorId;
                    mobAirPref.AirRewardPrograms = GetAirRewardPrograms(traveler);
                    // mobAirPref.SpecialRequests = GetTravelerSpecialRequests(pref.SpecialRequests);Client is not using this even we send this ..
                    // mobAirPref.ServiceAnimals = GetTravelerServiceAnimals(pref.ServiceAnimals);Client is not using this even we send this ..
                    mobAirPrefs.Add(mobAirPref);
                }
            }
            return mobAirPrefs;
        }
        public List<Mobile.Model.Common.MOBAddress> PopulateTravelerAddressesV2(List<United.Mobile.Model.CSLModels.Address> addresses, MOBApplication application = null, string flow = null)
        {
            #region

            var mobAddresses = new List<Mobile.Model.Common.MOBAddress>();
            if (addresses != null && addresses.Count > 0)
            {
                bool isCorpAddressPresent = false;
                    //As per Business / DotCom Kalpen; we are removing the condition for checking the Effectivedate and Discontinued date
                    var corpIndex = addresses.FindIndex(x => x.TypeDescription != null && x.TypeDescription.ToLower() == "corporate" && x.Line1 != null && x.Line1.Trim() != "");
                    if (corpIndex >= 0)
                        isCorpAddressPresent = true;

                foreach (United.Mobile.Model.CSLModels.Address address in addresses)
                {
                        if (isCorpAddressPresent && address.TypeDescription.ToLower() == "corporate" &&
                            (_configuration.GetValue<bool>("USPOSCountryCodes_ByPass") || IsInternationalBilling(application, address.CountryCode, flow)))
                        {
                            var a = new Mobile.Model.Common.MOBAddress();
                            a.Key = address.Key;
                            a.Channel = new SHOPChannel();
                            a.Channel.ChannelCode = "A";
                            a.Channel.ChannelDescription = "Address";
                            a.Channel.ChannelTypeCode = address.Type.ToString();
                            a.Channel.ChannelTypeDescription = address.TypeDescription;
                            //a.ApartmentNumber = address.AptNum; No longer needed confirmed from service
                            a.City = address.City;
                            // a.CompanyName = address.CompanyName;No longer needed confirmed from service
                            a.Country = new MOBCountry();
                            a.Country.Code = address.CountryCode;
                            a.Country.Name = address.CountryName;
                            // a.JobTitle = address.JobTitle;No longer needed confirmed from service
                            a.Line1 = address.Line1;
                            a.Line2 = address.Line2;
                            a.Line3 = address.Line3;
                            a.State = new Mobile.Model.Common.State();
                            a.State.Code = address.StateCode;
                            a.IsDefault = address.PrimaryIndicator;
                            a.IsPrivate = address.IsPrivate;
                            a.PostalCode = address.PostalCode;
                            if (address.TypeDescription.ToLower().Trim() == "corporate")
                            {
                                a.IsPrimary = true;
                                a.IsCorporate = true; // MakeIsCorporate true inorder to disable the edit on client
                            }
                            // Make IsPrimary true inorder to select the corpaddress by default

                            if (_configuration.GetValue<bool>("ShowTripInsuranceBookingSwitch"))
                            {
                                a.IsValidForTPIPurchase = IsValidAddressForTPIpayment(address.CountryCode);

                                if (a.IsValidForTPIPurchase)
                                {
                                    a.IsValidForTPIPurchase = IsValidSateForTPIpayment(address.StateCode);
                                }
                            }
                            mobAddresses.Add(a);
                        }

                    if (address.EffectiveDate <= DateTime.UtcNow && address.DiscontinuedDate >= DateTime.UtcNow)
                    {
                        if (_configuration.GetValue<bool>("USPOSCountryCodes_ByPass") || IsInternationalBilling(application, address.CountryCode, flow)) //##Kirti - allow only US addresses 
                        {
                            var a = new Mobile.Model.Common.MOBAddress();
                            a.Key = address.Key;
                            a.Channel = new SHOPChannel();
                            a.Channel.ChannelCode = "A";
                            a.Channel.ChannelDescription = "Address";
                            a.Channel.ChannelTypeCode = address.Type.ToString();
                            a.Channel.ChannelTypeDescription = address.TypeDescription;
                            a.City = address.City;
                            a.Country = new MOBCountry();
                            a.Country.Code = address.CountryCode;
                            a.Country.Name = address.CountryName;
                            a.Line1 = address.Line1;
                            a.Line2 = address.Line2;
                            a.Line3 = address.Line3;
                            a.State = new Mobile.Model.Common.State();
                            a.State.Code = address.StateCode;
                            //a.State.Name = address.StateName;
                            a.IsDefault = address.PrimaryIndicator;
                            a.IsPrimary = address.PrimaryIndicator;
                            a.IsPrivate = address.IsPrivate;
                            a.PostalCode = address.PostalCode;
                            //Adding this check for corporate addresses to gray out the Edit button on the client
                            //if (address.ChannelTypeDescription.ToLower().Trim() == "corporate")
                            //{
                            //    a.IsCorporate = true;
                            //}
                            if (_configuration.GetValue<bool>("ShowTripInsuranceBookingSwitch"))
                            {
                                a.IsValidForTPIPurchase = IsValidAddressForTPIpayment(address.CountryCode);

                                if (a.IsValidForTPIPurchase)
                                {
                                    a.IsValidForTPIPurchase = IsValidSateForTPIpayment(address.StateCode);
                                }
                            }
                            mobAddresses.Add(a);
                        }
                    }
                }
            }
            return mobAddresses;
            #endregion
        }
        private List<MOBEmail> PopulateMPSecurityEmailAddressesV2(List<United.Mobile.Model.CSLModels.Email> emailAddresses)
        {
            #region
            List<MOBEmail> mobEmailAddresses = new List<MOBEmail>();
            if (emailAddresses != null && emailAddresses.Count > 0)
            {
                MOBEmail primaryEmailAddress = null;
                int co = 0;
                foreach (United.Mobile.Model.CSLModels.Email email in emailAddresses)
                {
                    if (email.EffectiveDate <= DateTime.UtcNow && email.DiscontinuedDate >= DateTime.UtcNow)
                    {
                        #region As per Wade Change want to filter out to return only Primary email to client if not primary phone exist return the first one from the list.So lot of work at client side will save time. Sep 15th 2014
                        co = co + 1;
                        MOBEmail e = new MOBEmail();
                        e.Key = email.Key;
                        e.Channel = new SHOPChannel();
                        e.Channel.ChannelCode = "E";
                        e.Channel.ChannelDescription = "Email";
                        e.Channel.ChannelTypeCode = email.Type.ToString();
                        e.Channel.ChannelTypeDescription = email.TypeDescription;
                        e.EmailAddress = email.Address;
                        e.IsDefault = email.PrimaryIndicator;
                        e.IsPrimary = email.PrimaryIndicator;
                        e.IsPrivate = email.IsPrivate;
                        e.IsDayOfTravel = email.DayOfTravelNotification;
                        if (email.PrimaryIndicator)
                        {
                            primaryEmailAddress = new MOBEmail();
                            primaryEmailAddress = e;
                            break;
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
        private MOBCPCustomerMetrics PopulateCustomerMetrics(OwnerResponseModel profileOwnerResponse)
        {
            if (profileOwnerResponse?.CustomerMetrics?.Data != null)
            {
                MOBCPCustomerMetrics travelerCustomerMetrics = new MOBCPCustomerMetrics();
                if (!String.IsNullOrEmpty(profileOwnerResponse.CustomerMetrics.Data.PTCCode))
                {
                    travelerCustomerMetrics.PTCCode = profileOwnerResponse.CustomerMetrics.Data.PTCCode;
                }
                return travelerCustomerMetrics;
            }
            return null;
        }
        private List<MOBPrefRewardProgram> GetAirRewardPrograms(TravelerProfileResponse traveler)
        {
            List<MOBPrefRewardProgram> mobAirRewardsProgs = new List<MOBPrefRewardProgram>();
            if (traveler?.RewardPrograms != null && traveler?.RewardPrograms.Count > 0)
            {
                foreach (United.Mobile.Model.CSLModels.RewardProgramData pref in traveler?.RewardPrograms)
                {
                    MOBPrefRewardProgram mobAirRewardsProg = new MOBPrefRewardProgram();
                    if (traveler?.Profile != null)
                    {
                        mobAirRewardsProg.CustomerId = traveler.Profile.CustomerId;
                        mobAirRewardsProg.ProfileId = traveler.Profile.ProfileId;
                    }
                    mobAirRewardsProg.ProgramMemberId = pref.ProgramMemberId;
                    mobAirRewardsProg.VendorCode = pref.VendorCode;
                    mobAirRewardsProg.VendorDescription = pref.VendorDescription;
                    mobAirRewardsProgs.Add(mobAirRewardsProg);
                }
            }
            return mobAirRewardsProgs;
        }

        public List<MOBBKLoyaltyProgramProfile> GetTravelerRewardPgrograms(List<RewardProgramData> rewardPrograms, int currentEliteLevel)
        {
            List<MOBBKLoyaltyProgramProfile> programs = new List<MOBBKLoyaltyProgramProfile>();

            if (rewardPrograms != null && rewardPrograms.Count > 0)
            {
                foreach (RewardProgramData rewardProgram in rewardPrograms)
                {
                    MOBBKLoyaltyProgramProfile airRewardProgram = new MOBBKLoyaltyProgramProfile();
                    airRewardProgram.ProgramId = rewardProgram.ProgramId.ToString();
                    airRewardProgram.ProgramName = rewardProgram.Description;
                    airRewardProgram.MemberId = rewardProgram.ProgramMemberId;
                    airRewardProgram.CarrierCode = rewardProgram.VendorCode;
                    if (airRewardProgram.CarrierCode.Trim().Equals("UA"))
                    {
                        airRewardProgram.MPEliteLevel = currentEliteLevel;
                    }
                    airRewardProgram.RewardProgramKey = rewardProgram.Key;
                    programs.Add(airRewardProgram);
                }
            }
            return programs;
        }
        public async Task<List<MOBTypeOption>> GetProfileDisclaimerList()
        {

            List<MOBLegalDocument> profileDisclaimerList = await GetLegalDocumentsForTitles("ProfileDisclamerList");
            List<MOBTypeOption> disclaimerList = new List<MOBTypeOption>();
            List<MOBTypeOption> travelerDisclaimerTextList = new List<MOBTypeOption>();

            List<string> mappingTextList = _configuration.GetValue<string>("Booking20TravelerDisclaimerMapping").Split('~').ToList();
            foreach (string mappingText in mappingTextList)
            {
                string disclaimerTextTitle = mappingText.Split('=')[0].ToString().Trim();
                List<string> travelerTextTitleList = mappingText.Split('=')[1].ToString().Split('|').ToList();
                int co = 0;
                foreach (string travelerTextTile in travelerTextTitleList)
                {
                    if (profileDisclaimerList != null)
                    {
                        foreach (MOBLegalDocument legalDocument in profileDisclaimerList)
                        {
                            if (legalDocument.Title.ToUpper().Trim() == travelerTextTile.ToUpper().Trim())
                            {
                                MOBTypeOption typeOption = new MOBTypeOption();
                                co++;
                                typeOption.Key = disclaimerTextTitle + co.ToString();
                                typeOption.Value = legalDocument.LegalDocument;
                                travelerDisclaimerTextList.Add(typeOption);
                            }
                        }
                    }
                }
            }
            return travelerDisclaimerTextList;
        }

        private async Task<List<MOBLegalDocument>> GetLegalDocumentsForTitles(string titles)
        {
            var legalDocuments = new List<MOBLegalDocument>();
            legalDocuments = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(titles, _headers.ContextValues.TransactionId, true);

            if (legalDocuments == null)
            {
                legalDocuments = new List<United.Definition.MOBLegalDocument>();
            }
            return legalDocuments;
        }
        public async System.Threading.Tasks.Task MakeProfileOwnerServiceCall(MOBCPProfileRequest request)
        {
            var ownerProfileResponse = await _customerProfileOwnerService.GetProfileOwnerInfo<OwnerResponseModel>(request.Token, request.SessionId, request.MileagePlusNumber);
            await _sessionHelperService.SaveSession<OwnerResponseModel>(ownerProfileResponse, request.SessionId, new List<string> { request.SessionId, ObjectNames.CSLGetProfileOwnerResponse }, ObjectNames.CSLGetProfileOwnerResponse);
        }
        public List<MOBAddress> PopulateCorporateTravelerAddresses(List<United.CorporateDirect.Models.CustomerProfile.Address> addresses, MOBApplication application = null, string flow = null)
        {
            #region
            List<MOBAddress> mobAddresses = new List<MOBAddress>();
            if (addresses != null && addresses.Count > 0)
            {

                foreach (United.CorporateDirect.Models.CustomerProfile.Address address in addresses)
                {
                    if ((_configuration.GetValue<bool>("USPOSCountryCodes_ByPass") || IsInternationalBilling(application, address.CountryCode, flow)))
                    {
                        MOBAddress a = new MOBAddress();
                        a.Key = address.Key;
                        a.Channel = new SHOPChannel();
                        a.Channel.ChannelCode = address.ChannelCode;
                        a.Channel.ChannelDescription = address.ChannelCodeDescription;
                        a.Channel.ChannelTypeCode = address.ChannelTypeCode.ToString();
                        a.Channel.ChannelTypeDescription = address.ChannelTypeDescription;
                        a.City = address.City;
                        a.Country = new MOBCountry();
                        a.Country.Code = address.CountryCode;
                        a.Line1 = address.AddressLine1;
                        a.State = new State();
                        a.State.Code = address.StateCode;
                        a.PostalCode = address.PostalCode;
                        a.IsPrimary = true;
                        a.IsCorporate = true;
                        if (_configuration.GetValue<bool>("ShowTripInsuranceBookingSwitch"))
                        {
                            a.IsValidForTPIPurchase = IsValidAddressForTPIpayment(address.CountryCode);

                            if (a.IsValidForTPIPurchase)
                            {
                                a.IsValidForTPIPurchase = IsValidSateForTPIpayment(address.StateCode);
                            }
                        }
                        mobAddresses.Add(a);
                    }
                }
            }
            return mobAddresses;
            #endregion
        }
        private List<MOBEmail> PopulateCorporateEmailAddresses(List<United.CorporateDirect.Models.CustomerProfile.Email> emailAddresses, bool onlyDayOfTravelContact)
        {
            #region
            List<MOBEmail> mobEmailAddresses = new List<MOBEmail>();
            bool isCorpEmailPresent = false;

            if (_configuration.GetValue<bool>("CorporateConcurBooking") && IsCorpBookingPath)
            {
                //As per Business / DotCom Kalpen; we are removing the condition for checking the Effectivedate and Discontinued date
                var corpIndex = emailAddresses.FindIndex(x => x.ChannelTypeDescription != null && x.ChannelTypeDescription.ToLower() == "corporate" && x.EmailAddress != null && x.EmailAddress.Trim() != "");
                if (corpIndex >= 0)
                    isCorpEmailPresent = true;

            }

            if (emailAddresses != null && emailAddresses.Count > 0)
            {
                MOBEmail primaryEmailAddress = null;
                int co = 0;
                foreach (United.CorporateDirect.Models.CustomerProfile.Email email in emailAddresses)
                {
                    if (_configuration.GetValue<bool>("CorporateConcurBooking"))
                    {
                        if (isCorpEmailPresent && !onlyDayOfTravelContact && email.ChannelTypeDescription.ToLower() == "corporate")
                        {
                            primaryEmailAddress = new MOBEmail();
                            primaryEmailAddress.Channel = new SHOPChannel();
                            primaryEmailAddress.EmailAddress = email.EmailAddress;
                            primaryEmailAddress.Channel.ChannelCode = email.ChannelCode;
                            primaryEmailAddress.Channel.ChannelDescription = email.ChannelCodeDescription;
                            primaryEmailAddress.Channel.ChannelTypeCode = email.ChannelTypeCode.ToString();
                            primaryEmailAddress.Channel.ChannelTypeDescription = email.ChannelTypeDescription;
                            primaryEmailAddress.IsPrimary = true;
                            break;
                        }
                        else if (isCorpEmailPresent && !onlyDayOfTravelContact && email.ChannelTypeDescription.ToLower() != "corporate")
                        {
                            continue;
                        }
                    }
                    //Fix for CheckOut ArgNull Exception - Empty EmailAddress with null EffectiveDate & DiscontinuedDate for Corp Account Revenue Booking (MOBILE-9873) - Shashank : Added OR condition to allow CorporateAccount ProfileOwner.
                    if ((!_configuration.GetValue<bool>("DisableCheckforCorpAccEmail")
                            && email.IsProfileOwner == true && primaryEmailAddress.IsNullOrEmpty()))
                    {
                        #region As per Wade Change want to filter out to return only Primary email to client if not primary phone exist return the first one from the list.So lot of work at client side will save time. Sep 15th 2014
                        co = co + 1;
                        MOBEmail e = new MOBEmail();
                        e.Channel = new SHOPChannel();
                        e.EmailAddress = email.EmailAddress;
                        e.Channel.ChannelCode = email.ChannelCode;
                        e.Channel.ChannelDescription = email.ChannelCodeDescription;
                        e.Channel.ChannelTypeCode = email.ChannelTypeCode.ToString();
                        e.Channel.ChannelTypeDescription = email.ChannelTypeDescription;

                        if (!onlyDayOfTravelContact)
                        {
                            if (co == 1)
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

        private List<MOBCPPhone> PopulateCorporatePhones(List<United.CorporateDirect.Models.CustomerProfile.Phone> phones, bool onlyDayOfTravelContact)
        {
            List<MOBCPPhone> mobCPPhones = new List<MOBCPPhone>();
            bool isCorpPhonePresent = false;


            if (_configuration.GetValue<bool>("CorporateConcurBooking") && IsCorpBookingPath)
            {
                var corpIndex = phones.FindIndex(x => x.ChannelTypeDescription != null && x.ChannelTypeDescription.ToLower() == "corporate" && x.PhoneNumber != null && x.PhoneNumber != "");
                if (corpIndex >= 0)
                    isCorpPhonePresent = true;
            }


            if (phones != null && phones.Count > 0)
            {
                MOBCPPhone primaryMobCPPhone = null;
                int co = 0;
                foreach (United.CorporateDirect.Models.CustomerProfile.Phone phone in phones)
                {
                    #region As per Wade Change want to filter out to return only Primary Phone to client if not primary phone exist return the first one from the list.So lot of work at client side will save time. Sep 15th 2014
                    MOBCPPhone mobCPPhone = new MOBCPPhone();
                    co = co + 1;
                    mobCPPhone.PhoneNumber = phone.PhoneNumber;
                    mobCPPhone.ChannelCode = phone.ChannelCode;
                    mobCPPhone.ChannelCodeDescription = phone.ChannelCodeDescription;
                    mobCPPhone.ChannelTypeCode = Convert.ToString(phone.ChannelTypeCode);
                    mobCPPhone.ChannelTypeDescription = phone.ChannelTypeDescription;
                    mobCPPhone.ChannelTypeDescription = phone.ChannelTypeDescription;
                    mobCPPhone.ChannelTypeSeqNumber = 0;
                    mobCPPhone.CountryCode = phone.CountryCode;
                    mobCPPhone.IsProfileOwner = phone.IsProfileOwner;
                    if (phone.PhoneDevices != null && phone.PhoneDevices.Count > 0)
                    {
                        mobCPPhone.DeviceTypeCode = phone.PhoneDevices[0].CommDeviceTypeCode;
                    }

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
                    

                    if (!onlyDayOfTravelContact)
                    {
                        if (co == 1)
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
            }
            return mobCPPhones;
        }
        private List<MOBPrefAirPreference> PopulateCorporateAirPrefrences(List<United.CorporateDirect.Models.CustomerProfile.AirPreference> airPreferences)
        {
            List<MOBPrefAirPreference> mobAirPrefs = new List<MOBPrefAirPreference>();
            if (airPreferences != null && airPreferences.Count > 0)
            {
                foreach (United.CorporateDirect.Models.CustomerProfile.AirPreference pref in airPreferences)
                {
                    MOBPrefAirPreference mobAirPref = new MOBPrefAirPreference();
                    mobAirPref.MealCode = pref.MealCode;
                    mobAirPrefs.Add(mobAirPref);
                }
            }
            return mobAirPrefs;
        }
        #endregion

    }
}
