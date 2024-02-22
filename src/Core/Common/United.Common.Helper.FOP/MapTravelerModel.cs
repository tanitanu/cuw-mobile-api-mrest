using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Service.Presentation.CommonModel;
using United.Utility.Helper;

namespace United.Common.Helper.FOP
{
    public class MapTravelerModel
    {
        public static MOBCPTraveler MapCslPersonToMOBCPTravel(Service.Presentation.ReservationModel.Traveler cslTraveler, int paxIndex, Collection<Service.Presentation.SegmentModel.ReservationFlightSegment> flightSegments, List<MOBSHOPTripBase> trips, bool partiallyFlownToggle = false, ICacheLog _logger = null)
        {
            var mobCPTraveler = new MOBCPTraveler();
            Service.Presentation.PersonModel.Person person = cslTraveler.Person;
            mobCPTraveler.Key = person.Key;
            mobCPTraveler.PaxIndex = paxIndex;
            mobCPTraveler.BirthDate = Convert.ToDateTime(person.DateOfBirth).ToShortDateString();
            mobCPTraveler.GenderCode = person.Sex;
            mobCPTraveler.FirstName = person.GivenName;
            mobCPTraveler.MiddleName = person.MiddleName;
            mobCPTraveler.LastName = person.Surname;
            mobCPTraveler.KnownTravelerNumber = string.Empty;
            mobCPTraveler.Message = string.Empty;
            mobCPTraveler.MileagePlus = null;
            mobCPTraveler.TravelerNameIndex = person.Key;
            mobCPTraveler.CslReservationPaxTypeCode = person.PricingPaxType;
            mobCPTraveler.TravelerTypeCode = person.Type;
            mobCPTraveler.PNRCustomerID = person.CustomerID;
            if (trips != null && trips.Any())
            {
                string DeptDateOfFLOF = trips[0].DepartDate;

                if (!string.IsNullOrEmpty(cslTraveler.Person.Type)
                    && cslTraveler.Person.Type.ToUpper().Equals("INF")
                    && !string.IsNullOrEmpty(cslTraveler.Person.DateOfBirth)
                    && TopHelper.GetAgeByDOB(cslTraveler.Person.DateOfBirth, DeptDateOfFLOF) < 2)
                    mobCPTraveler.IsEligibleForSeatSelection = false;
                else
                    mobCPTraveler.IsEligibleForSeatSelection = true;
            }

            mobCPTraveler.Seats = new List<MOBSeat>();

            if (cslTraveler.LoyaltyProgramProfile != null && !string.IsNullOrEmpty(cslTraveler.LoyaltyProgramProfile.LoyaltyProgramMemberID))
            {
                mobCPTraveler.MileagePlus = new MOBCPMileagePlus();
                int currentLevel = 0;
                mobCPTraveler.MileagePlus.MileagePlusId = cslTraveler.LoyaltyProgramProfile.LoyaltyProgramMemberID;
                mobCPTraveler.MileagePlus.CurrentEliteLevelDescription = GetLoyalityStatus(Convert.ToString(cslTraveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription), out currentLevel);
                mobCPTraveler.MileagePlus.CurrentEliteLevel = currentLevel;

                mobCPTraveler.AirRewardPrograms = new List<MOBBKLoyaltyProgramProfile>();

                var airRewardProgram = new MOBBKLoyaltyProgramProfile();
                //airRewardProgram.ProgramId = rewardProgram.ProgramID.ToString();
                //airRewardProgram.ProgramName = rewardProgram.Description;
                //airRewardProgram.RewardProgramKey = rewardProgram.Key;
                airRewardProgram.MemberId = cslTraveler.LoyaltyProgramProfile.LoyaltyProgramMemberID;
                airRewardProgram.CarrierCode = cslTraveler.LoyaltyProgramProfile.LoyaltyProgramCarrierCode;
                if (airRewardProgram.CarrierCode.Trim().Equals("UA"))
                {
                    airRewardProgram.MPEliteLevel = currentLevel;
                }
                mobCPTraveler.AirRewardPrograms.Add(airRewardProgram);
            }

            foreach (var fs in flightSegments)
            {
                foreach (var cs in fs.CurrentSeats ?? Enumerable.Empty<United.Service.Presentation.SegmentModel.PersonSeat>())
                {
                    if (cs.ReservationNameIndex == person.Key)
                    {
                        int tripno = Convert.ToInt32(fs.TripNumber);
                        if (trips.Count() < tripno)
                            tripno--;
                        var mobseat = new MOBSeat();
                        if (partiallyFlownToggle ? (trips.Count >= tripno && trips[tripno - 1].ChangeType != MOBSHOPTripChangeType.ChangeFlight) :(trips[tripno - 1].ChangeType != MOBSHOPTripChangeType.ChangeFlight))
                        {
                            if (mobCPTraveler.IsEligibleForSeatSelection)
                            {
                                mobseat.SeatAssignment = cs.Seat.Identifier.Replace("*", "").Trim('0');
                                mobseat.SeatType = cs.Seat.SeatType;
                            }
                            mobseat.TravelerSharesIndex = person.Key;
                            mobseat.Destination = fs.FlightSegment.ArrivalAirport.IATACode;
                            mobseat.Origin = fs.FlightSegment.DepartureAirport.IATACode;
                            mobseat.ProgramCode = cs.ProgramCode;
                            mobseat.FlightNumber = fs.FlightSegment.FlightNumber;
                            mobCPTraveler.Seats.Add(mobseat);
                        }
                        else if (_logger!=null && trips.Count < tripno) {
                            _logger.LogInformation("Partially Flown Changes Logging {surname}", person.Surname);
                        }
                    }
                }
            }

            //if (person.Contact != null && person.Contact.PhoneNumbers != null)
            //{
            //    mobCPTraveler.Phones = GetMobCpPhones(person.Contact.PhoneNumbers);
            //    mobCPTraveler.ReservationPhones = mobCPTraveler.Phones;
            //}

            if (person.Contact != null && person.Contact.Emails != null && person.Contact.Emails.Count > 0)
            {
                mobCPTraveler.EmailAddresses = GetMobEmails(person.Contact.Emails);
                mobCPTraveler.ReservationEmailAddresses = mobCPTraveler.EmailAddresses;
            }

            return mobCPTraveler;
        }
        public static string GetLoyalityStatus(string memberType, out int level)
        {
            switch (memberType)
            {
                case "PremierGold":
                    level = 2;
                    return "Premier Gold";
                case "PremierSilver":
                    level = 1;
                    return "Premier Silver";
                case "PremierPlatinum":
                    level = 3;
                    return "Premier Platium";
                case "Premier1K":
                    level = 4;
                    return "Premier 1K";
                case "GlobalServices":
                    level = 5;
                    return "Global Services";
            }
            level = 0;
            return "General Member";
        }
        public static List<MOBEmail> GetMobEmails(Collection<EmailAddress> pnrEmailAddress)
        {
            var mobEmails = new List<MOBEmail>();
            if (pnrEmailAddress != null)
            {
                foreach (var pnremail in pnrEmailAddress)
                {
                    var mobEmail = new MOBEmail();
                    mobEmail.EmailAddress = pnremail.Address.ToLower();
                    mobEmails.Add(mobEmail);
                }
            }
            return mobEmails;
        }
        public static List<MOBCPPhone> GetMobCpPhones(Collection<Telephone> pnrTelephoneNumbers)
        {
            var mobCpPhones = new List<MOBCPPhone>();
            if (pnrTelephoneNumbers != null)
            {
                foreach (var pnrPhone in pnrTelephoneNumbers)
                {
                    var mobPhone = new MOBCPPhone();
                    mobPhone.AreaNumber = pnrPhone.AreaCityCode;
                    mobPhone.PhoneNumber = System.Text.RegularExpressions.Regex.Replace(pnrPhone.PhoneNumber, @"[^0-9]+", "");
                    mobPhone.CountryCode = "US";
                    mobPhone.ChannelCodeDescription = "US";
                    mobCpPhones.Add(mobPhone);
                }
            }
            return mobCpPhones;
        }
    }
}
