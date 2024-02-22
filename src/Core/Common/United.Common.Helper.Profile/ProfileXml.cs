using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UAWSAccountProfile;
using United.Mobile.DataAccess.Profile;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Utility.Helper;

namespace United.Common.Helper.Profile
{
    public class ProfileXml: IProfileXML
    {
        private readonly IConfiguration _configuration;
        private readonly IProfileService _profileService;

        public ProfileXml(IConfiguration configuration,IProfileService profileService)
        {
            _configuration = configuration;
            _profileService = profileService;
        }

        public async Task<MOBProfile> GetOwnerProfileForMP2014(string mileagePlusAccountNumber)
        {
            MOBProfile profile = null;

            var response =await _profileService.GetOwnerProfileForMP2014(mileagePlusAccountNumber).ConfigureAwait(false);
           

            if (response.Errors.Length > 0)
            {
                throw new MOBUnitedException(response.Errors[0].Message);
            }

            if (response.Profiles != null)
            {
                profile = new MOBProfile();
                profile.MileagePlusNumber = response.Profiles.OnePassId;
                profile.CustomerId = response.Profiles.CustomerId.ToString();

                if (response.Profiles.ProfileSession != null)
                {
                    profile.OwnerName = new MOBName();
                    profile.OwnerName.Title = (string.IsNullOrEmpty(response.Profiles.ProfileSession.Title) || response.Profiles.ProfileSession.Title.Trim().ToUpper() == "<NULL>") ? string.Empty : response.Profiles.ProfileSession.Title;
                    profile.OwnerName.First = response.Profiles.ProfileSession.FirstName;
                    profile.OwnerName.Middle = response.Profiles.ProfileSession.MiddleName;
                    profile.OwnerName.Last = response.Profiles.ProfileSession.LastName;
                    profile.OwnerName.Suffix = response.Profiles.ProfileSession.Suffix;
                }

                if (response.Profiles.ProfileCollection != null && response.Profiles.ProfileCollection.Length > 0 && response.Profiles.ProfileCollection[0].Travelers != null && response.Profiles.ProfileCollection[0].Travelers.Length > 0)
                {
                    profile.Travelers = new List<MOBTraveler>();
                    MOBTraveler t = null;

                    foreach (UAWSProfileMP2014.Traveler traveler in response.Profiles.ProfileCollection[0].Travelers)
                    {
                        if (traveler.IsProfileOwner)
                        {
                            t = new MOBTraveler();
                            t.MileagePlusNumber = traveler.OnePassId;
                            t.CustomerId = traveler.CustomerId;
                            t.Name = new MOBName();
                            t.Name.Title = traveler.Title.Trim().ToUpper() == "<NULL>" ? string.Empty : traveler.Title;
                            t.Name.First = traveler.FirstName;
                            t.Name.Middle = traveler.MiddleName;
                            t.Name.Last = traveler.LastName;
                            t.Name.Suffix = traveler.Suffix;
                            t.IsProfileOwner = traveler.IsProfileOwner;
                            t.CurrentEliteLevel = traveler.ProfileSession.CurrentEliteLevel;

                            if (traveler.Addresses != null && traveler.Addresses.Length > 0)
                            {
                                t.Addresses = new List<MOBAddress>();

                                foreach (UAWSProfileMP2014.Address address in traveler.Addresses)
                                {
                                    if (address.EffectiveDate <= DateTime.UtcNow && address.DiscontinuedDate >= DateTime.UtcNow && address.IsPrimary)
                                    {
                                        var a = new MOBAddress();
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
                                        a.Line3 = address.AddressLine2;
                                        a.State = new State();
                                        a.State.Code = address.StateCode;
                                        a.State.Name = address.StateName;
                                        a.IsDefault = address.IsDefault;
                                        a.IsPrimary = address.IsPrimary;
                                        a.IsPrivate = address.IsPrivate;
                                        a.PostalCode = address.PostalCode;
                                        t.Addresses.Add(a);
                                    }
                                }
                            }

                            if (traveler.OnePass != null && traveler.OnePass.PaymentInfos != null && traveler.OnePass.PaymentInfos.Length > 0)
                            {
                                t.PaymentInfos = new List<MOBPaymentInfo>();
                                foreach (UAWSProfileMP2014.PaymentInfo paymentInfo in traveler.OnePass.PaymentInfos)
                                {
                                    if (!string.IsNullOrEmpty(paymentInfo.PartnerCode) && paymentInfo.PartnerCode.Equals("CH") && !string.IsNullOrEmpty(paymentInfo.PartnerCardType) && _configuration.GetValue<string>("ChaseCardTypes").IndexOf(paymentInfo.PartnerCardType) != -1)
                                    {
                                        MOBPaymentInfo pi = new MOBPaymentInfo();
                                        pi.Key = paymentInfo.Key;
                                        pi.CardType = paymentInfo.CardType;
                                        pi.CardTypeDescription = paymentInfo.CardTypeDescription;
                                        pi.ExpireMonth = paymentInfo.ExpMonth.ToString();
                                        pi.ExpireYear = paymentInfo.ExpYear.ToString();
                                        pi.IsPartnerCard = paymentInfo.IsPartnerCard;
                                        pi.Issuer = paymentInfo.Issuer;
                                        pi.MileagePlusNumber = paymentInfo.OnePassId;
                                        pi.PartnerCardType = paymentInfo.PartnerCardType;
                                        pi.PartnerCode = paymentInfo.PartnerCode;
                                        t.PaymentInfos.Add(pi);
                                    }
                                }
                            }

                            if (traveler.OnePass != null && traveler.OnePass.PartnerCards != null && traveler.OnePass.PartnerCards.Length > 0)
                            {
                                t.PartnerCards = new List<MOBPartnerCard>();
                                foreach (UAWSProfileMP2014.PartnerCard partnerCard in traveler.OnePass.PartnerCards)
                                {
                                    if (!string.IsNullOrEmpty(partnerCard.CardType) && _configuration.GetValue<string>("PresidentialPlusChaseCardTypes").IndexOf(partnerCard.CardType) != -1)
                                    {
                                        MOBPartnerCard pc = new MOBPartnerCard();
                                        pc.CardType = partnerCard.CardType;
                                        pc.CardTypeDescription = partnerCard.CardTypeDescription;
                                        pc.Key = partnerCard.Key;
                                        pc.MileagePlusnumber = partnerCard.OnePassId;
                                        pc.PartnerCode = partnerCard.PartnerCode;

                                        t.PartnerCards.Add(pc);
                                    }
                                }
                            }

                            profile.Travelers.Add(t);
                        }
                    }
                }
                else
                {
                    throw new MOBUnitedException(string.Format("No profile data found for MileagePlus Number {0}.", mileagePlusAccountNumber));
                }
            }
            else
            {
                throw new MOBUnitedException(string.Format("No profile data found for MileagePlus Number {0}.", mileagePlusAccountNumber));
            }

            return profile;
        }

        public async Task<MOBProfile> GetProfile(MOBProfileRequest request)
        {
            MOBProfile profile = null;
            List<MOBPhone> profileOwnerHomePhones = new List<MOBPhone>();

            //.AccountProfileSoapClient soapClient = new UAWSAccountProfile.AccountProfileSoapClient();
            //UAWSAccountProfile.wsAccountResponse response = soapClient.GetProfileWithPartnerCardOption(request.MileagePlusNumber, true, "iPhone", ConfigurationManager.AppSettings["AccessCode - AccountProfile"], string.Empty);

            var response= await _profileService.GetProfile(request.MileagePlusNumber).ConfigureAwait(false);

            if (response.Errors.Length > 0)
            {
                throw new MOBUnitedException(response.Errors[0].Message);
            }

            if (response.Profiles != null)
            {
                #region
                profile = new MOBProfile();
                profile.MileagePlusNumber = response.Profiles.OnePassId;
                profile.CustomerId = response.Profiles.CustomerId.ToString();

                if (response.Profiles.ProfileSession != null)
                {
                    profile.OwnerName = new MOBName();
                    profile.OwnerName.Title = (string.IsNullOrEmpty(response.Profiles.ProfileSession.Title) || response.Profiles.ProfileSession.Title.Trim().ToUpper() == "<NULL>") ? string.Empty : response.Profiles.ProfileSession.Title;
                    profile.OwnerName.First = response.Profiles.ProfileSession.FirstName;
                    profile.OwnerName.Middle = response.Profiles.ProfileSession.MiddleName;
                    profile.OwnerName.Last = response.Profiles.ProfileSession.LastName;
                    profile.OwnerName.Suffix = response.Profiles.ProfileSession.Suffix;
                }

                if (response.Profiles.ProfileCollection != null && response.Profiles.ProfileCollection.Length > 0 && response.Profiles.ProfileCollection[0].Travelers != null && response.Profiles.ProfileCollection[0].Travelers.Length > 0)
                {
                    profile.Travelers = new List<MOBTraveler>();

                    foreach (Traveler traveler in response.Profiles.ProfileCollection[0].Travelers)
                    {
                        #region
                        MOBTraveler t = new MOBTraveler();
                        t.MileagePlusNumber = traveler.OnePassId;
                        t.CustomerId = traveler.CustomerId;
                        t.Name = new MOBName();
                        t.Name.Title = traveler.Title.Trim().ToUpper() == "<NULL>" ? string.Empty : traveler.Title;
                        t.Name.First = traveler.FirstName;
                        t.Name.Middle = traveler.MiddleName;
                        t.Name.Last = traveler.LastName;
                        t.Name.Suffix = traveler.Suffix;
                        t.IsProfileOwner = traveler.IsProfileOwner;
                        if (!t.IsProfileOwner)
                        {
                            if (traveler.ProfileSession != null && traveler.ProfileSession.CurrentEliteLevel != null)
                            {
                                t.CurrentEliteLevel = traveler.ProfileSession.CurrentEliteLevel;
                            }
                        }
                        else
                        {
                            if (traveler.OnePass != null && traveler.OnePass.CurrentEliteLevel != null)
                            {
                                t.CurrentEliteLevel = traveler.OnePass.CurrentEliteLevel;
                            }
                            else
                            {
                                t.CurrentEliteLevel = GetCurrentEliteLevel(request.MileagePlusNumber);
                            }
                        }

                        if (traveler.AirPreferences != null && traveler.AirPreferences.Length > 0)
                        {
                            #region
                            t.AirPreferences = new List<MOBPrefAirPreference>();
                            foreach (UAWSAccountProfile.AirPreference ap in traveler.AirPreferences)
                            {
                                MOBPrefAirPreference airPreference = new MOBPrefAirPreference();
                                airPreference.AirportCode = ap.AirportCode;
                                airPreference.AirportNameLong = ap.AirportNameLong;
                                airPreference.AirportNameShort = ap.AirportNameShort;
                                airPreference.AirPreferenceId = ap.AirPreferenceId;
                                airPreference.ClassDescription = ap.ClassDescription;
                                airPreference.ClassId = ap.ClassId;
                                airPreference.CustomerId = ap.CustomerId;
                                airPreference.EquipmentCode = ap.EquipmentCode;
                                airPreference.EquipmentDesc = ap.EquipmentDesc;
                                airPreference.IsActive = ap.IsActive;
                                airPreference.IsNew = ap.IsNew;
                                airPreference.IsSelected = ap.IsSelected;
                                airPreference.Key = ap.Key;
                                airPreference.LanguageCode = ap.LanguageCode;
                                airPreference.MealCode = ap.MealCode;
                                airPreference.MealDescription = ap.MealDescription;
                                airPreference.MealId = ap.MealId;
                                airPreference.NumOfFlightsDisplay = ap.NumOfFlightsDisplay;
                                airPreference.ProfileId = ap.ProfileId;
                                airPreference.SearchPreferenceDescription = ap.SearchPreferenceDescription;
                                airPreference.SearchPreferenceId = ap.SearchPreferenceId;
                                airPreference.SeatFrontBack = ap.SeatFrontBack;
                                airPreference.SeatSide = ap.SeatSide;
                                airPreference.SeatSideDescription = ap.SeatSideDescription;
                                airPreference.VendorCode = ap.VendorCode;
                                airPreference.VendorDescription = ap.VendorDescription;
                                airPreference.VendorId = ap.VendorId;

                                if (ap.AirRewardPrograms != null && ap.AirRewardPrograms.Length > 0)
                                {
                                    airPreference.AirRewardPrograms = new List<MOBPrefRewardProgram>();
                                    foreach (UAWSAccountProfile.RewardProgram rp in ap.AirRewardPrograms)
                                    {
                                        MOBPrefRewardProgram rewardProgram = new MOBPrefRewardProgram();
                                        rewardProgram.CustomerId = rp.CustomerId;
                                        rewardProgram.IsNew = rp.IsNew;
                                        rewardProgram.IsSelected = rp.IsSelected;
                                        rewardProgram.CustomerId = rp.CustomerId;
                                        rewardProgram.IsValidNumber = rp.IsValidNumber;
                                        rewardProgram.Key = rp.Key;
                                        rewardProgram.PreferenceId = rp.PreferenceId;
                                        rewardProgram.ProfileId = rp.ProfileId;
                                        rewardProgram.ProgramCode = rp.ProgramCode;
                                        rewardProgram.ProgramDescription = rp.ProgramDescription;
                                        rewardProgram.ProgramId = rp.ProgramId;
                                        rewardProgram.ProgramMemberId = rp.ProgramMemberId;
                                        rewardProgram.ProgramType = rp.ProgramType;
                                        rewardProgram.SourceCode = rp.SourceCode;
                                        rewardProgram.SourceDescription = rp.SourceDescription;
                                        rewardProgram.VendorCode = rp.VendorCode;
                                        rewardProgram.VendorDescription = rp.VendorDescription;

                                        airPreference.AirRewardPrograms.Add(rewardProgram);
                                    }
                                }

                                if (ap.SpecialRequests != null && ap.SpecialRequests.Length > 0)
                                {
                                    airPreference.SpecialRequests = new List<MOBPrefSpecialRequest>();
                                    foreach (UAWSAccountProfile.SpecialRequest sr in ap.SpecialRequests)
                                    {
                                        MOBPrefSpecialRequest specialRequest = new MOBPrefSpecialRequest();
                                        specialRequest.AirPreferenceId = sr.AirPreferenceId;
                                        specialRequest.Description = sr.Description;
                                        specialRequest.IsNew = sr.IsNew;
                                        specialRequest.IsSelected = sr.IsSelected;
                                        specialRequest.Key = sr.Key;
                                        specialRequest.LanguageCode = sr.LanguageCode;
                                        specialRequest.Priority = sr.Priority;
                                        specialRequest.SpecialRequestCode = sr.SpecialRequestCode;
                                        specialRequest.SpecialRequestId = sr.SpecialRequestId;

                                        airPreference.SpecialRequests.Add(specialRequest);
                                    }
                                }
                                t.AirPreferences.Add(airPreference);
                            }
                            #endregion
                        }

                        if (traveler.Contacts != null && traveler.Contacts.Length > 0)
                        {
                            #region
                            t.Contacts = new List<MOBPrefContact>();
                            foreach (Contact c in traveler.Contacts)
                            {
                                MOBPrefContact contact = new MOBPrefContact();
                                contact.ContactId = c.ContactId;
                                contact.ContactMileagePlusId = c.ContactOnePassId;
                                contact.ContactSequenceNum = c.ContactSequenceNum;
                                contact.ContactTypeCode = c.ContactTypeCode;
                                contact.ContactTypeDescription = c.ContactTypeDescription;
                                contact.CustomerId = c.CustomerId;
                                contact.FirstName = c.FirstName;
                                contact.IsDeceased = c.IsDeceased;
                                contact.IsNew = c.IsNew;
                                contact.IsSelected = c.IsSelected;
                                contact.IsVictim = c.IsVictim;
                                contact.Key = c.Key;
                                contact.LanguageCode = c.LanguageCode;
                                contact.LastName = c.LastName;
                                contact.MiddleName = c.MiddleName;
                                contact.ProfileOwnerId = c.ProfileOwnerId;
                                contact.Suffix = c.Suffix;
                                contact.Title = c.Title;

                                if (c.Phones != null && c.Phones.Length > 0)
                                {
                                    contact.Phones = new List<MOBPrefPhone>();
                                    foreach (UAWSAccountProfile.Phone p in c.Phones)
                                    {
                                        MOBPrefPhone phone = new MOBPrefPhone();
                                        phone.AreaNumber = p.AreaNumber;

                                        phone.ChannelCode = p.ChannelCode;
                                        phone.ChannelCodeDescription = p.ChannelCodeDescription;

                                        switch (p.ChannelTypeCode)
                                        {
                                            case UAWSAccountProfile.ChannelType.Business:
                                                phone.ChannelTypeCode = "Business";
                                                break;
                                            case UAWSAccountProfile.ChannelType.Default:
                                                phone.ChannelTypeCode = "Default";
                                                break;
                                            case UAWSAccountProfile.ChannelType.Home:
                                                phone.ChannelTypeCode = "Home";
                                                break;
                                            case UAWSAccountProfile.ChannelType.Null:
                                                phone.ChannelTypeCode = "";
                                                break;
                                            case UAWSAccountProfile.ChannelType.Other:
                                                phone.ChannelTypeCode = "Other";
                                                break;
                                        }

                                        phone.ChannelTypeDescription = p.ChannelTypeDescription;
                                        phone.ChannelTypeSeqNum = p.ChannelTypeSeqNum;
                                        phone.CountryCode = p.CountryCode;
                                        phone.CountryName = p.CountryName;
                                        phone.CountryPhoneNumber = p.CountryPhoneNumber;
                                        phone.CustomerId = p.CustomerId;
                                        phone.Description = p.Description;
                                        phone.ExtensionNumber = p.ExtensionNumber;
                                        phone.IsDefault = p.IsDefault;
                                        phone.IsNew = p.IsNew;
                                        phone.IsPrivate = p.IsPrivate;
                                        phone.IsProfileOwner = p.IsProfileOwner;
                                        phone.IsSelected = p.IsSelected;
                                        phone.Key = p.Key;
                                        phone.LanguageCode = p.LanguageCode;
                                        phone.PhoneNumber = p.PagerPinNumber;

                                        contact.Phones.Add(phone);
                                    }
                                }

                                t.Contacts.Add(contact);
                            }
                            #endregion
                        }

                        if (traveler.AirPreferences != null && traveler.AirPreferences.Length > 0 && traveler.AirPreferences[0].AirRewardPrograms != null && traveler.AirPreferences[0].AirRewardPrograms.Length > 0)
                        {
                            #region
                            t.AirRewardPrograms = new List<MOBAirRewardProgram>();
                            foreach (UAWSAccountProfile.RewardProgram rewardProgram in traveler.AirPreferences[0].AirRewardPrograms)
                            {
                                MOBAirRewardProgram airRewardProgram = new MOBAirRewardProgram();
                                airRewardProgram.CustomerId = rewardProgram.CustomerId.ToString();
                                airRewardProgram.ProfileId = rewardProgram.ProfileId.ToString();
                                airRewardProgram.ProgramCode = rewardProgram.ProgramCode;
                                airRewardProgram.ProgramDescription = rewardProgram.ProgramDescription;
                                airRewardProgram.ProgramMemberId = rewardProgram.ProgramMemberId;
                                airRewardProgram.VendorCode = rewardProgram.VendorCode;
                                airRewardProgram.VendorDescription = rewardProgram.VendorDescription;

                                t.AirRewardPrograms.Add(airRewardProgram);
                            }
                            #endregion
                        }

                        if (request.IncludeAddresses)
                        {
                            #region
                            if (traveler.Addresses != null && traveler.Addresses.Length > 0)
                            {
                                t.Addresses = new List<MOBAddress>();
                                foreach (UAWSAccountProfile.Address address in traveler.Addresses)
                                {
                                    if (address.EffectiveDate <= DateTime.UtcNow && address.DiscontinuedDate >= DateTime.UtcNow)
                                    {
                                        MOBAddress a = new MOBAddress();
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
                                        a.Line3 = address.AddressLine2;
                                        a.State = new State();
                                        a.State.Code = address.StateCode;
                                        a.State.Name = address.StateName;
                                        a.IsDefault = address.IsDefault;
                                        a.IsPrimary = address.IsPrimary;
                                        a.IsPrivate = address.IsPrivate;
                                        a.PostalCode = address.PostalCode;
                                        t.Addresses.Add(a);
                                    }
                                }
                            }
                            #endregion
                        }

                        if (request.IncludePhones)
                        {
                            #region
                            bool hasHomePhone = false;
                            if (traveler.Phones != null && traveler.Phones.Length > 0)
                            {
                                t.Phones = new List<MOBPhone>();
                                foreach (UAWSAccountProfile.Phone phone in traveler.Phones)
                                {
                                    if (phone.EffectiveDate <= DateTime.UtcNow && phone.DiscontinuedDate >= DateTime.UtcNow)
                                    {
                                        MOBPhone p = new MOBPhone();
                                        p.Key = phone.Key;
                                        p.Channel = new SHOPChannel();
                                        p.Channel.ChannelCode = phone.ChannelCode;
                                        p.Channel.ChannelDescription = phone.ChannelCodeDescription;
                                        p.Channel.ChannelTypeCode = phone.ChannelTypeCode.ToString();
                                        p.Channel.ChannelTypeDescription = phone.ChannelTypeDescription;
                                        p.Country = new MOBCountry();
                                        p.Country.Code = phone.CountryCode;
                                        p.Country.Name = phone.CountryName;
                                        p.Country.PhoneCode = phone.CountryPhoneNumber;

                                        p.IsPrimary = phone.IsPrimary;
                                        p.IsPrivate = phone.IsPrivate;
                                        p.IsProfileOwner = phone.IsProfileOwner;
                                        p.PhoneNumber = phone.PhoneNumber;
                                        p.AreaNumber = phone.AreaNumber;
                                        p.ExtensionNumber = phone.ExtensionNumber;
                                        if (t.IsProfileOwner && phone.ChannelCode.Trim().ToUpper() == "P" && phone.ChannelTypeCode == UAWSAccountProfile.ChannelType.Home)
                                        {

                                            p.IsProfileOwner = true;

                                        }
                                        p.IsProfileOwner = false;
                                        t.Phones.Add(p);
                                        if (!hasHomePhone && phone.ChannelCode.Trim().ToUpper() == "P" && phone.ChannelTypeCode == UAWSAccountProfile.ChannelType.Home)
                                        {
                                            hasHomePhone = true;
                                        }
                                    }
                                }
                                // Enhancement to add the Profile Owner Home Phones if the saved travelers has not Home Phones but has other phones
                                if (!t.IsProfileOwner && !hasHomePhone)
                                {
                                    t.Phones.AddRange(profileOwnerHomePhones);
                                }
                            }
                            else if (!t.IsProfileOwner && profileOwnerHomePhones != null && profileOwnerHomePhones.Count > 0)
                            {
                                // Enhancement to add the Profile Owner Home Phones if the saved travelers does not have any phones (home,work etc..)
                                //t.Phones = new List<MOBPhone>();
                                //t.Phones = profileOwnerHomePhones;
                            }

                            #endregion
                        }

                        if (request.IncludeEmails)
                        {
                            #region
                            if (traveler.EmailAddresses != null && traveler.EmailAddresses.Length > 0)
                            {
                                t.Emails = new List<MOBEmail>();
                                foreach (UAWSAccountProfile.Email email in traveler.EmailAddresses)
                                {
                                    if (email.EffectiveDate <= DateTime.UtcNow && email.DiscontinuedDate >= DateTime.UtcNow)
                                    {
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

                                        t.Emails.Add(e);
                                    }
                                }
                            }
                            #endregion
                        }

                        if (request.IncludePaymentInfos)
                        {
                            #region
                            if (traveler.OnePass != null && traveler.OnePass.PaymentInfos != null && traveler.OnePass.PaymentInfos.Length > 0)
                            {
                                t.PaymentInfos = new List<MOBPaymentInfo>();
                                foreach (UAWSAccountProfile.PaymentInfo paymentInfo in traveler.OnePass.PaymentInfos)
                                {
                                    // Remove "Bill me latre" from saved card
                                    if (paymentInfo.CardType.Equals("BL"))
                                    {
                                        continue;
                                    }

                                    MOBPaymentInfo pi = new MOBPaymentInfo();
                                    pi.Key = paymentInfo.Key;
                                    pi.CardType = paymentInfo.CardType;
                                    pi.CardTypeDescription = paymentInfo.CardTypeDescription;
                                    pi.ExpireMonth = paymentInfo.ExpMonth.ToString();
                                    pi.ExpireYear = paymentInfo.ExpYear.ToString();
                                    pi.IsPartnerCard = paymentInfo.IsPartnerCard;
                                    pi.Issuer = paymentInfo.Issuer;
                                    pi.MileagePlusNumber = paymentInfo.OnePassId;
                                    pi.PartnerCardType = paymentInfo.PartnerCardType;
                                    pi.PartnerCode = paymentInfo.PartnerCode;
                                    t.PaymentInfos.Add(pi);
                                }
                            }

                            if (traveler.CreditCards != null)
                            {
                                #region
                                t.CreditCards = new List<MOBCreditCard>();
                                foreach (UAWSAccountProfile.CreditCard creditCard in traveler.CreditCards)
                                {
                                    // Remove "Bill me latre" from saved card
                                    if (creditCard.CCTypeCode.Equals("BL"))
                                    {
                                        continue;
                                    }

                                    if (!IsValidCreditCard(creditCard))
                                    {
                                        continue;
                                    }

                                    MOBCreditCard cc = new MOBCreditCard();
                                    cc.Key = creditCard.Key;
                                    cc.CardType = creditCard.CCTypeCode;
                                    cc.CardTypeDescription = creditCard.CCTypeDescription;
                                    cc.Description = creditCard.Description;
                                    cc.ExpireMonth = creditCard.ExpMonth.ToString();
                                    cc.ExpireYear = creditCard.ExpYear.ToString();
                                    cc.IsPrimary = creditCard.IsPrimary;
                                    if (creditCard.DecryptedCardNumber.Length == 15)
                                        cc.UnencryptedCardNumber = "XXXXXXXXXXX" + creditCard.DecryptedCardNumber.Substring(creditCard.DecryptedCardNumber.Length - 4, 4);
                                    else
                                        cc.UnencryptedCardNumber = "XXXXXXXXXXXX" + creditCard.DecryptedCardNumber.Substring(creditCard.DecryptedCardNumber.Length - 4, 4);
                                    //cc.UnencryptedCardNumber = creditCard.DecryptedCardNumber;
                                    cc.EncryptedCardNumber = creditCard.EncryptedCardNumber;
                                    cc.cIDCVV2 = creditCard.CIDCVV2;
                                    cc.cCName = creditCard.CCName;
                                    t.CreditCards.Add(cc);
                                }
                                #endregion
                            }

                            #endregion
                        }

                        if (request.IncludeSecureTravelers)
                        {

                            if (traveler.SecureTravelers != null && traveler.SecureTravelers.Length > 0)
                            {
                                #region
                                t.SecureTravelers = new List<MOBSecureTraveler>();
                                int secureTravelerCount = 0;
                                MOBSecureTraveler ver1SecureTraveler = new MOBSecureTraveler();
                                bool needVersion1ForMyAccount = false;
                                if (traveler.IsProfileOwner && request.TransactionId.Trim() == "FromAccountSummaryCall")
                                {
                                    #region
                                    needVersion1ForMyAccount = true;
                                    ver1SecureTraveler.Key = "";// secureTraveler.Key;
                                    ver1SecureTraveler.Name = new MOBName();
                                    ver1SecureTraveler.Name.Title = traveler.Title;
                                    ver1SecureTraveler.Name.First = traveler.FirstName;
                                    ver1SecureTraveler.Name.Middle = traveler.MiddleName;
                                    ver1SecureTraveler.Name.Last = traveler.LastName;
                                    ver1SecureTraveler.Name.Suffix = traveler.Suffix;
                                    ver1SecureTraveler.SequenceNumber = "0";
                                    ver1SecureTraveler.Gender = traveler.GenderCode.Trim();
                                    if (traveler.BirthDate != null)
                                    {
                                        ver1SecureTraveler.BirthDate = GeneralHelper.FormatDate(Convert.ToDateTime(traveler.BirthDate).ToString("yyyyMMdd"), request.LanguageCode);
                                        ver1SecureTraveler.DocumentType = "";// secureTraveler.DocumentType;
                                        ver1SecureTraveler.FlaggedType = "U";
                                    }
                                    #endregion
                                }

                                #region LINQ Code
                                List<UAWSAccountProfile.SecureTraveler> lastUpdatedSecureTravelers = (from st in traveler.SecureTravelers
                                                                                                      where st.DocumentType == "U"// U stands for flagged
                                                                                                      orderby st.UpdateDtmz descending
                                                                                                      select st).ToList();
                                #endregion
                                if (lastUpdatedSecureTravelers == null || (lastUpdatedSecureTravelers != null && lastUpdatedSecureTravelers.Count == 0))
                                {
                                    #region LINQ Code
                                    //List<Continental.DAL.UAWSAccountProfile.SecureTraveler> 
                                    lastUpdatedSecureTravelers = (from st in traveler.SecureTravelers
                                                                  where st.DocumentType == "C" //C for cleared
                                                                  orderby st.UpdateDtmz descending
                                                                  select st).ToList();
                                    #endregion
                                }
                                if (lastUpdatedSecureTravelers == null || (lastUpdatedSecureTravelers != null && lastUpdatedSecureTravelers.Count == 0))
                                {
                                    #region LINQ Code
                                    //List<Continental.DAL.UAWSAccountProfile.SecureTraveler> 
                                    lastUpdatedSecureTravelers = (from st in traveler.SecureTravelers
                                                                  where st.DocumentType != "X" //X stands for delete
                                                                  orderby st.UpdateDtmz descending
                                                                  select st).ToList();
                                    #endregion
                                }
                                //for (int i = traveler.SecureTravelers.Count()-1;i>=0;i--) // This is to get latest updated TSA records 
                                //foreach (UAWSAccountProfile.SecureTraveler secureTraveler in traveler.SecureTravelers)
                                foreach (UAWSAccountProfile.SecureTraveler secureTraveler in lastUpdatedSecureTravelers)
                                {
                                    //UAWSAccountProfile.SecureTraveler secureTraveler = traveler.SecureTravelers[i];
                                    if (secureTraveler.DocumentType.Trim().ToUpper() != "X" && secureTravelerCount < 3)
                                    {
                                        #region
                                        MOBSecureTraveler st = new MOBSecureTraveler();
                                        st.Key = secureTraveler.Key;
                                        st.Name = new MOBName();
                                        st.Name.Title = traveler.Title;
                                        st.Name.First = secureTraveler.FirstName;
                                        st.Name.Middle = secureTraveler.MiddleName;
                                        st.Name.Last = secureTraveler.LastName;
                                        st.Name.Suffix = secureTraveler.Suffix;
                                        if (st.Name.Suffix == null || st.Name.Suffix.Trim() == string.Empty)
                                        {
                                            st.Name.Suffix = traveler.Suffix;
                                        }
                                        st.SequenceNumber = secureTraveler.SequenceNumber.ToString();
                                        st.Gender = secureTraveler.Gender;
                                        if (secureTraveler.SupplementaryTravelInfos != null && secureTraveler.SupplementaryTravelInfos.Length > 0)
                                        {
                                            foreach (UAWSAccountProfile.SupplementaryTravelInfo supplementaryTravelInfo in secureTraveler.SupplementaryTravelInfos)
                                            {
                                                if (supplementaryTravelInfo.Type == UAWSAccountProfile.NumberType.KnownTraveler)
                                                {
                                                    st.KnownTravelerNumber = supplementaryTravelInfo.Number;
                                                }

                                                if (supplementaryTravelInfo.Type == UAWSAccountProfile.NumberType.Redress)
                                                {
                                                    st.RedressNumber = supplementaryTravelInfo.Number;
                                                }
                                            }
                                        }
                                        if (secureTraveler.BirthDate != null)
                                        {
                                            st.BirthDate = GeneralHelper.FormatDate(Convert.ToDateTime(secureTraveler.BirthDate).ToString("yyyyMMdd"), request.LanguageCode);
                                        }
                                        st.DocumentType = "";// secureTraveler.DocumentType;
                                        st.FlaggedType = secureTraveler.DocumentType;
                                        if (!t.IsTSAFlagON && secureTraveler.DocumentType.Trim().ToUpper() == "U")
                                        {
                                            t.IsTSAFlagON = true;
                                            if (t.IsProfileOwner)
                                            {
                                                profile.IsProfileOwnerTSAFlagON = true;
                                            }
                                        }
                                        if (secureTraveler.DocumentType.Trim().ToUpper() == "C") // This is to get only Customer Cleared Secure Traveler records
                                        {
                                            t.SecureTravelers = new List<MOBSecureTraveler>();
                                            t.SecureTravelers.Add(st);
                                            //i = -1;
                                            secureTravelerCount = 4;
                                        }
                                        else
                                        {
                                            if (needVersion1ForMyAccount && traveler.IsProfileOwner)
                                            {
                                                if (ver1SecureTraveler.Gender.Trim() == "")
                                                {
                                                    ver1SecureTraveler.Gender = st.Gender;
                                                }
                                                if (ver1SecureTraveler.BirthDate.Trim() == "")
                                                {
                                                    ver1SecureTraveler.BirthDate = st.BirthDate;
                                                }
                                                ver1SecureTraveler.KnownTravelerNumber = st.KnownTravelerNumber;
                                                ver1SecureTraveler.RedressNumber = st.RedressNumber;
                                                t.SecureTravelers.Add(ver1SecureTraveler);
                                                needVersion1ForMyAccount = false; // once added make this false so next time it won't add
                                                secureTravelerCount = secureTravelerCount + 1;
                                            }
                                            t.SecureTravelers.Add(st);
                                            secureTravelerCount = secureTravelerCount + 1;
                                        }
                                        #endregion
                                    }
                                    else if (secureTravelerCount > 3)
                                    {
                                        break;
                                    }
                                }
                                #endregion
                            }
                            else
                            {
                                #region
                                t.SecureTravelers = new List<MOBSecureTraveler>();

                                MOBSecureTraveler secureTraveler = new MOBSecureTraveler();
                                secureTraveler.Name = new MOBName();
                                secureTraveler.Name.Title = traveler.Title;
                                secureTraveler.Name.First = traveler.FirstName;
                                secureTraveler.Name.Middle = traveler.MiddleName;
                                secureTraveler.Name.Last = traveler.LastName;
                                secureTraveler.Name.Suffix = traveler.Suffix;
                                if (traveler.BirthDate != null)
                                {
                                    secureTraveler.BirthDate = Convert.ToDateTime(traveler.BirthDate.ToString()).ToShortDateString();
                                }
                                secureTraveler.Gender = traveler.GenderCode;
                                secureTraveler.SequenceNumber = "1";
                                t.SecureTravelers.Add(secureTraveler);
                                #endregion
                            }

                        }

                        #region this was deemed not correct
                        //if (request.IncludeSecureTravelers)
                        //{

                        //    if (traveler.SecureTravelers != null && traveler.SecureTravelers.Length > 0)
                        //    {
                        //        #region
                        //        t.SecureTravelers = new List<MOBSecureTraveler>();
                        //        int secureTravelerCount = 0;
                        //        MOBSecureTraveler ver1SecureTraveler = new MOBSecureTraveler();
                        //        bool needVersion1ForMyAccount = false;
                        //        if (traveler.IsProfileOwner && request.TransactionId.Trim() == "FromAccountSummaryCall")
                        //        {
                        //            #region
                        //            needVersion1ForMyAccount = true;
                        //            ver1SecureTraveler.Key = "";// secureTraveler.Key;
                        //            ver1SecureTraveler.Name = new MOBName();
                        //            ver1SecureTraveler.Name.Title = traveler.Title;
                        //            ver1SecureTraveler.Name.First = traveler.FirstName;
                        //            ver1SecureTraveler.Name.Middle = traveler.MiddleName;
                        //            ver1SecureTraveler.Name.Last = traveler.LastName;
                        //            ver1SecureTraveler.Name.Suffix = traveler.Suffix;
                        //            ver1SecureTraveler.SequenceNumber = "0";
                        //            ver1SecureTraveler.Gender = traveler.GenderCode.Trim();
                        //            if (traveler.BirthDate != null)
                        //            {
                        //                ver1SecureTraveler.BirthDate = Utility.FormatDate(Convert.ToDateTime(traveler.BirthDate).ToString("yyyyMMdd"), request.LanguageCode);
                        //                ver1SecureTraveler.DocumentType = "";// secureTraveler.DocumentType;
                        //                ver1SecureTraveler.FlaggedType = "U";
                        //            }
                        //            #endregion
                        //        }

                        //        #region LINQ Code
                        //        List<UAWSAccountProfile.SecureTraveler> lastUpdatedSecureTravelers = (from st in traveler.SecureTravelers
                        //                                                                              where st.DocumentType == "U"// U stands for flagged
                        //                                                                              orderby st.UpdateDtmz descending
                        //                                                                              select st).ToList();
                        //        #endregion
                        //        if (lastUpdatedSecureTravelers == null || (lastUpdatedSecureTravelers != null && lastUpdatedSecureTravelers.Count == 0))
                        //        {
                        //            #region LINQ Code
                        //            //List<Continental.DAL.UAWSAccountProfile.SecureTraveler> 
                        //            lastUpdatedSecureTravelers = (from st in traveler.SecureTravelers
                        //                                          where st.DocumentType == "C" //C for cleared
                        //                                          orderby st.UpdateDtmz descending
                        //                                          select st).ToList();
                        //            #endregion
                        //        }
                        //        if (lastUpdatedSecureTravelers == null || (lastUpdatedSecureTravelers != null && lastUpdatedSecureTravelers.Count == 0))
                        //        {
                        //            #region LINQ Code
                        //            //List<Continental.DAL.UAWSAccountProfile.SecureTraveler> 
                        //            lastUpdatedSecureTravelers = (from st in traveler.SecureTravelers
                        //                                          where st.DocumentType != "X" //X stands for delete
                        //                                          orderby st.UpdateDtmz descending
                        //                                          select st).ToList();
                        //            #endregion
                        //        }
                        //        //for (int i = traveler.SecureTravelers.Count()-1;i>=0;i--) // This is to get latest updated TSA records 
                        //        //foreach (UAWSAccountProfile.SecureTraveler secureTraveler in traveler.SecureTravelers)
                        //        foreach (UAWSAccountProfile.SecureTraveler secureTraveler in lastUpdatedSecureTravelers)
                        //        {
                        //            //UAWSAccountProfile.SecureTraveler secureTraveler = traveler.SecureTravelers[i];
                        //            if (secureTraveler.DocumentType.Trim().ToUpper() != "X" && secureTravelerCount < 3)
                        //            {
                        //                #region
                        //                MOBSecureTraveler st = new MOBSecureTraveler();
                        //                st.Key = secureTraveler.Key;
                        //                st.Name = new MOBName();
                        //                st.Name.Title = traveler.Title;
                        //                st.Name.First = secureTraveler.FirstName;
                        //                st.Name.Middle = secureTraveler.MiddleName;
                        //                st.Name.Last = secureTraveler.LastName;
                        //                st.Name.Suffix = secureTraveler.Suffix;
                        //                if (st.Name.Suffix == null || st.Name.Suffix.Trim() == string.Empty)
                        //                {
                        //                    st.Name.Suffix = traveler.Suffix;
                        //                }
                        //                st.SequenceNumber = secureTraveler.SequenceNumber.ToString();
                        //                st.Gender = secureTraveler.Gender;
                        //                if (secureTraveler.SupplementaryTravelInfos != null && secureTraveler.SupplementaryTravelInfos.Length > 0)
                        //                {
                        //                    foreach (UAWSAccountProfile.SupplementaryTravelInfo supplementaryTravelInfo in secureTraveler.SupplementaryTravelInfos)
                        //                    {
                        //                        if (supplementaryTravelInfo.Type == UAWSAccountProfile.NumberType.KnownTraveler)
                        //                        {
                        //                            st.KnownTravelerNumber = supplementaryTravelInfo.Number;
                        //                        }

                        //                        if (supplementaryTravelInfo.Type == UAWSAccountProfile.NumberType.Redress)
                        //                        {
                        //                            st.RedressNumber = supplementaryTravelInfo.Number;
                        //                        }
                        //                    }
                        //                }
                        //                if (secureTraveler.BirthDate != null)
                        //                {
                        //                    st.BirthDate = Utility.FormatDate(Convert.ToDateTime(secureTraveler.BirthDate).ToString("yyyyMMdd"), request.LanguageCode);
                        //                }
                        //                st.DocumentType = "";// secureTraveler.DocumentType;
                        //                st.FlaggedType = secureTraveler.DocumentType;
                        //                if (!t.IsTSAFlagON && secureTraveler.DocumentType.Trim().ToUpper() == "U")
                        //                {
                        //                    t.IsTSAFlagON = true;
                        //                    if (t.IsProfileOwner)
                        //                    {
                        //                        profile.IsProfileOwnerTSAFlagON = true;
                        //                    }
                        //                }
                        //                if (secureTraveler.DocumentType.Trim().ToUpper() == "C") // This is to get only Customer Cleared Secure Traveler records
                        //                {
                        //                    t.SecureTravelers = new List<MOBSecureTraveler>();
                        //                    t.SecureTravelers.Add(st);
                        //                    //i = -1;
                        //                    secureTravelerCount = 4;
                        //                }
                        //                else
                        //                {
                        //                    if (needVersion1ForMyAccount && traveler.IsProfileOwner)
                        //                    {
                        //                        if (ver1SecureTraveler.Gender.Trim() == "")
                        //                        {
                        //                            ver1SecureTraveler.Gender = st.Gender;
                        //                        }
                        //                        if (ver1SecureTraveler.BirthDate.Trim() == "")
                        //                        {
                        //                            ver1SecureTraveler.BirthDate = st.BirthDate;
                        //                        }
                        //                        ver1SecureTraveler.KnownTravelerNumber = st.KnownTravelerNumber;
                        //                        ver1SecureTraveler.RedressNumber = st.RedressNumber;
                        //                        t.SecureTravelers.Add(ver1SecureTraveler);
                        //                        needVersion1ForMyAccount = false; // once added make this false so next time it won't add
                        //                        secureTravelerCount = secureTravelerCount + 1;
                        //                    }
                        //                    t.SecureTravelers.Add(st);
                        //                    secureTravelerCount = secureTravelerCount + 1;
                        //                }
                        //                #endregion
                        //            }
                        //            else if (secureTravelerCount > 3)
                        //            {
                        //                break;
                        //            }
                        //        }
                        //        #endregion
                        //    }
                        //    else
                        //    {
                        //        #region
                        //        t.SecureTravelers = new List<MOBSecureTraveler>();

                        //        MOBSecureTraveler secureTraveler = new MOBSecureTraveler();
                        //        secureTraveler.Name = new MOBName();
                        //        secureTraveler.Name.Title = traveler.Title;
                        //        secureTraveler.Name.First = traveler.FirstName;
                        //        secureTraveler.Name.Middle = traveler.MiddleName;
                        //        secureTraveler.Name.Last = traveler.LastName;
                        //        secureTraveler.Name.Suffix = traveler.Suffix;
                        //        if (traveler.BirthDate != null)
                        //        {
                        //            secureTraveler.BirthDate = Convert.ToDateTime(traveler.BirthDate.ToString()).ToShortDateString();
                        //        }
                        //        secureTraveler.Gender = traveler.GenderCode;
                        //        secureTraveler.SequenceNumber = "1";
                        //        t.SecureTravelers.Add(secureTraveler);
                        //        #endregion
                        //    }

                        //}
                        #endregion


                        profile.Travelers.Add(t);
                        #endregion
                    }
                }
                else
                {
                    throw new MOBUnitedException(string.Format("No profile data found for MileagePlus Number {0}.", request.MileagePlusNumber));
                }
                #endregion
            }
            else
            {
                throw new MOBUnitedException(string.Format("No profile data found for MileagePlus Number {0}.", request.MileagePlusNumber));
            }
            return profile;
        }

        private int GetCurrentEliteLevel(string mileageAccountNumber)
        {
            int eliteLevel = 0;

            //OnePassDAL onepassDAL = new OnePassDAL();
            //OPAccountSummary opAccountSummary = onepassDAL.GetAccountSummary(mileageAccountNumber, "TO_GET_ELITE_STATUS_CODE_FROM_CACHE");
            //if (opAccountSummary != null && opAccountSummary.EliteStatus != null && opAccountSummary.EliteStatus.Code != null)
            //{
            //    eliteLevel = opAccountSummary.EliteStatus.Level;

            //}

            return eliteLevel;
        }

        private bool IsValidCreditCard(CreditCard creditCard)
        {
            bool ok = false;

            if (!string.IsNullOrEmpty(creditCard.AddressKey) || (_configuration.GetValue<string>("AllowCreditCardWithEmptyAddressForBooking") != null && _configuration.GetValue<bool>("AllowCreditCardWithEmptyAddressForBooking"))) // As per Wade and Ann get credit card without Address also for the web api 2.0 Apps as the client prepoluating the profile address and address required at payment screen
            {
                if (creditCard.ExpYear > DateTime.Today.Year)
                {
                    ok = true;
                }
                else if (creditCard.ExpYear == DateTime.Today.Year)
                {
                    if (creditCard.ExpMonth >= DateTime.Today.Month)
                    {
                        ok = true;
                    }
                }
            }

            return ok;
        }
    }
}
