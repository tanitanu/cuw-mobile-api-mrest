using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.FlightStatus;
using United.Mobile.Model.Common;
using United.Mobile.Model.FlightStatus;
using United.Mobile.Model.Internal.Common;
using United.Service.Presentation.FlightResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Utility.Helper;
using FlightStatusSegment = United.Mobile.Model.FlightStatus.FlightStatusSegment;
using Genre = United.Service.Presentation.CommonModel.Genre;
namespace United.Common.Helper.FlightStatus
{
    public class FlightStatusHelper : IFlightStatusHelper
    {
        private readonly ICacheLog<FlightStatusHelper> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDPService _tokenService;
        private readonly IFlightStatusService _flightStatusService;
        private readonly FlightStatusDynamoDB _flightStatusDynamoDB;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly IFlightRequestSqlSpOnPremService _flightRequestSqlSpOnPremService;
        private readonly IHeaders _headers;

        public FlightStatusHelper(ICacheLog<FlightStatusHelper> logger
             , IConfiguration configuration
             , IDPService tokenService
             , IFlightStatusService Service
             , IFlightRequestSqlSpOnPremService flightRequestSqlSpOnPremService
             , IDynamoDBService dynamoDBService
            , IHeaders headers
           
            )
        {
            _logger = logger;
            _configuration = configuration;
            _tokenService = tokenService;
            _flightStatusService = Service;
            _dynamoDBService = dynamoDBService;
            _flightRequestSqlSpOnPremService = flightRequestSqlSpOnPremService;
            _headers = headers;
            _flightStatusDynamoDB = new FlightStatusDynamoDB(configuration, _dynamoDBService);
        }

        public async Task<FlightStatusInfo> GetFlightStatusJson(int flightNumber, string flightDate, string origin)
        {
            string flifoAuthenticationToken = await _tokenService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration);

            var cslFlifoResponseOBJ = await GetCSLFlightStatusJson(flightNumber.ToString(), flightDate, origin, flifoAuthenticationToken, string.Empty);

            if (cslFlifoResponseOBJ == null && !string.IsNullOrEmpty(origin))
            {
                // This is to get the FLIFO if the Passed In Departure Airport Code does not match with the Actual Flight Segments Departure Airport codes so that it will return all the segments of the flight in the next call. - Venkat 08/06/2013
                cslFlifoResponseOBJ = await GetCSLFlightStatusJson(flightNumber.ToString(), flightDate, string.Empty, flifoAuthenticationToken, string.Empty);
            }
            if (cslFlifoResponseOBJ != null)
            {
                FlightStatusInfo objFlightStatusInfo = await GetFlightStatusJsonResponse(cslFlifoResponseOBJ, flightNumber, _headers.ContextValues.LangCode);

                objFlightStatusInfo.FlightNumber = flightNumber.ToString();
                objFlightStatusInfo.FlightDate = flightDate;
                objFlightStatusInfo.CarrierCode = "UA";

                _logger.LogInformation("Flight status response {flightStatusInfo}", objFlightStatusInfo);

                return objFlightStatusInfo;
            }
            return default;
        }

        public async Task<FlightInformation> GetCSLFlightStatusJson(string flightNumber, string flightDate, string origin, string flifoAuthenticationToken, string sessionId)
        {
            var jsonResponse = await _flightStatusService.GetFlightStatusDetails(flifoAuthenticationToken, sessionId, flightNumber, flightDate, origin);
            if (!string.IsNullOrEmpty(jsonResponse))
            {
                DataContractJsonSerializer dataContractJsonSerializer = new DataContractJsonSerializer(typeof(FlightInformation));
                MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonResponse));
                return (FlightInformation)dataContractJsonSerializer.ReadObject(memoryStream);
            }
            return null;
        }

        private async Task<FlightStatusInfo> GetFlightStatusJsonResponse(FlightInformation cslFlifoResponseOBJ, int flightNum, string langCode)
        {
            #region
            List<string> shipNumbers = new List<string>();
            List<string> wiFiShipNumbers = new List<string>();
            List<string> advisoryCheckAirports = new List<string>();
            FlightStatusInfo objFlightStatusInfo = new FlightStatusInfo();

            List<FlightStatusSegment> flightStatusSegments = new List<FlightStatusSegment>();
            string flightDate = string.Empty;

            foreach (var flightLeg in cslFlifoResponseOBJ.FlightLegs)
            {
                if (flightLeg.OperationalFlightSegments != null)
                {
                    foreach (var seg in flightLeg.OperationalFlightSegments)
                    {
                        bool isDisplaySegment = true;
                        foreach (var characteristic in seg.Characteristic)
                        {
                            if (characteristic.Code.ToUpper().Trim() == "DisplayInd".ToUpper().Trim() && characteristic.Value.ToUpper().Trim() == "0"
                                && (_configuration.GetValue<string>("CheckForIsDisplaySegmentCSL6.1") == null || Convert.ToBoolean(_configuration.GetValue<string>("CheckForIsDisplaySegmentCSL6.1").ToString())))
                            {
                                isDisplaySegment = false;
                                break;
                            }
                        }
                        if (isDisplaySegment)
                        {
                            var scheduledFlightLeg = flightLeg.ScheduledFlightSegments.Where(s => s.DepartureAirport.IATACode == seg.DepartureAirport.IATACode && s.ArrivalAirport.IATACode == seg.ArrivalAirport.IATACode).FirstOrDefault();

                            #region
                            if (string.IsNullOrEmpty(flightDate)) // && (!String.IsNullOrEmpty(seg.DepartureDateTime) || !String.IsNullOrEmpty(seg.OutTime)))
                            {
                                if (!string.IsNullOrEmpty(seg.DepartureDateTime))
                                {
                                    flightDate = DateTime.Parse(seg.DepartureDateTime).ToString("MM/dd/yyyy");
                                }
                                else if (!string.IsNullOrEmpty(seg.OutTime))
                                {
                                    flightDate = DateTime.Parse(seg.OutTime).ToString("MM/dd/yyyy");
                                }
                            }
                            var flightStatusSeg = new FlightStatusSegment
                            {
                                FlightNumber = flightNum.ToString()
                            };

                            if (seg.DepartureUTCDateTime != null && seg.DepartureUTCDateTime.Trim() != "")
                            {
                                flightStatusSeg.ScheduledDepartureTimeGMT = seg.DepartureUTCDateTime;
                            }

                            if (seg.ArrivalUTCDateTime != null && seg.ArrivalUTCDateTime.Trim() != "")
                            {
                                flightStatusSeg.ScheduledArrivalTimeGMT = seg.ArrivalUTCDateTime;
                            }
                            flightStatusSeg.ScheduledArrivalDateTime = TopHelper.FormatDatetime(seg.ArrivalDateTime, langCode);
                            flightStatusSeg.ScheduledDepartureDateTime = TopHelper.FormatDatetime(seg.DepartureDateTime, langCode);

                            flightStatusSeg.ActualArrivalDateTime = TopHelper.FormatDatetime(seg.InTime, langCode);
                            flightStatusSeg.ActualDepartureDateTime = TopHelper.FormatDatetime(seg.OutTime, langCode);

                            flightStatusSeg.EstimatedArrivalDateTime = TopHelper.FormatDatetime(seg.EstimatedArrivalTime, langCode);
                            flightStatusSeg.EstimatedDepartureDateTime = TopHelper.FormatDatetime(seg.EstimatedDepartureTime, langCode);

                            if (scheduledFlightLeg != null && scheduledFlightLeg.ScheduledFlightDuration != null)
                                flightStatusSeg.ScheduledFlightTime = GetFormattedFlightTravelTime(scheduledFlightLeg.ScheduledFlightDuration);

                            #region To fix the Null Schedule Dep and Arr Date Times at response for Diverted CSL FLIFO with FLT Number , Origin 
                            if (string.IsNullOrEmpty(flightStatusSeg.ScheduledDepartureDateTime))
                            {
                                flightStatusSeg.ScheduledDepartureDateTime = (!string.IsNullOrEmpty(flightStatusSeg.EstimatedDepartureDateTime) ? flightStatusSeg.EstimatedDepartureDateTime : (!string.IsNullOrEmpty(flightStatusSeg.ActualDepartureDateTime) ? flightStatusSeg.ActualDepartureDateTime : null));
                            }

                            if (string.IsNullOrEmpty(flightStatusSeg.ScheduledArrivalDateTime))
                            {
                                flightStatusSeg.ScheduledArrivalDateTime = (!string.IsNullOrEmpty(flightStatusSeg.EstimatedArrivalDateTime) ? flightStatusSeg.EstimatedArrivalDateTime : (!string.IsNullOrEmpty(flightStatusSeg.ActualArrivalDateTime) ? flightStatusSeg.ActualArrivalDateTime : null));
                            }
                            #endregion

                            flightStatusSeg.Departure = new MOBAirport
                            {
                                Code = seg.DepartureAirport.IATACode,
                                Name = seg.DepartureAirport.Name
                            };

                            var storedProcedureService = new StoredProcedureService();

                            var spResponse = await _flightStatusDynamoDB.GetAirport<Airport>(flightStatusSeg.Departure.Code, string.Empty);

                            flightStatusSeg.Departure.Name = spResponse?.CityName;
                            //User Story - 160153 - Added below DepartureAirport property to get the airportname from database
                            flightStatusSeg.DepartureAirport = spResponse?.AirportName;

                            if (Convert.ToBoolean(_configuration.GetValue<string>("TurnOnBagInfo")))
                            {
                                flightStatusSeg.Baggage = new Baggage();

                                if (!string.IsNullOrEmpty(seg.ArrivalBagClaimUnit))
                                {
                                    string tempCar = _configuration.GetValue<string>("Carousel").ToString();
                                    flightStatusSeg.Baggage.BagClaimUnit = tempCar + " " + seg.ArrivalBagClaimUnit;
                                    flightStatusSeg.Baggage.HasBagLocation = true;
                                }
                                else
                                {
                                    flightStatusSeg.Baggage.BagClaimUnit = _configuration.GetValue<string>("NoBagInfo");
                                    flightStatusSeg.Baggage.HasBagLocation = false;
                                }
                            }

                            flightStatusSeg.Arrival = new MOBAirport
                            {
                                Code = seg.ArrivalAirport.IATACode,
                                Name = seg.ArrivalAirport.Name
                            };

                            spResponse = await _flightStatusDynamoDB.GetAirport<Airport>(flightStatusSeg.Departure.Code, string.Empty);

                            flightStatusSeg.Arrival.Name = spResponse?.CityName;
                            //User Story - 160153 - Added below ArrivalAirport property to get the airportname from database
                            flightStatusSeg.ArrivalAirport = spResponse?.AirportName;

                            flightStatusSeg.ArrivalGate = seg.ArrivalGate;
                            flightStatusSeg.ArrivalTerminal = seg.ArrivalTermimal;

                            flightStatusSeg.CanPushNotification = false;

                            if ((seg.InTime == null || (seg.InTime != null && seg.InTime.Trim() == "")))
                            {
                                flightStatusSeg.CanPushNotification = true;
                            }

                            flightStatusSeg.DepartureGate = seg.DepartureGate;
                            flightStatusSeg.DepartureTerminal = seg.DepartureTerminal;

                            if (seg.InboundFlightSegment != null && !string.IsNullOrEmpty(seg.InboundFlightSegment.FlightNumber) && !string.IsNullOrEmpty(seg.InboundFlightSegment.DepartureDate))
                            {
                                #region
                                flightStatusSeg.GetInBoundSegment = true;
                                flightStatusSeg.InBoundSegment = new Mobile.Model.FlightStatus.FlightSegment
                                {
                                    ArrivalAirport = seg.InboundFlightSegment.ArrivalAirport,
                                    ArrivalAirportName = seg.InboundFlightSegment.ArrivalAirportName,
                                    DepartureAirport = seg.InboundFlightSegment.DepartureAirport,
                                    DepartureAirportName = seg.InboundFlightSegment.DepartureAirportName,
                                    CarrierCode = seg.InboundFlightSegment.CarrierCode,
                                    FlightNumber = seg.InboundFlightSegment.FlightNumber
                                };

                                DateTime inBoundSegmentDepartureDate = new DateTime();
                                string inBoundSegmentDepartureDateString = string.Empty;

                                if (DateTime.TryParseExact(seg.InboundFlightSegment.DepartureDate, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out inBoundSegmentDepartureDate))
                                {
                                    inBoundSegmentDepartureDateString = inBoundSegmentDepartureDate.ToString("yyyy-MM-dd");
                                }
                                else
                                {
                                    string loggingInfo = "Flight Number: " + seg.FlightNumber + " - Flight Date: " + seg.DepartureDateTime + " - InboundFlightSegment.DepartureDate: " + seg.InboundFlightSegment.DepartureDate;
                                    loggingInfo = loggingInfo.Replace("\r", "").Replace("\n", "").Trim();
                                    _logger.LogInformation("Parsing CSL 6.1 InboundFlightSegment DepartureDate to DateTime Error: {loggingInfo}", loggingInfo);
                                }

                                flightStatusSeg.InBoundSegment.DepartureDate = inBoundSegmentDepartureDateString;
                                #endregion
                            }
                            if (seg.FlightStatuses != null)
                            {
                                foreach (var flightSegmentStatus in seg.FlightStatuses)
                                {
                                    #region
                                    if (flightSegmentStatus.StatusType.ToLower().Trim() == "flightstatus")
                                    {
                                        flightStatusSeg.Status = flightSegmentStatus.Description;

                                        if (!string.IsNullOrEmpty(flightStatusSeg.Status) && string.IsNullOrEmpty(flightStatusSeg.ActualDepartureDateTime))
                                        {
                                            if (flightStatusSeg.Status.ToUpper().Contains("SCHEDULE"))
                                            {
                                                flightStatusSeg.StatusShort = "On time";
                                            }
                                            else if (flightStatusSeg.Status.ToUpper().Contains("DELAY") || flightStatusSeg.Status.ToUpper().Contains("LATE"))
                                            {
                                                flightStatusSeg.StatusShort = "Delayed";
                                            }
                                            else if (flightStatusSeg.Status.ToUpper().Contains("CANCEL"))
                                            {
                                                flightStatusSeg.StatusShort = "Canceled";
                                            }
                                        }

                                        if (!string.IsNullOrEmpty(flightStatusSeg.Status) && !string.IsNullOrEmpty(flightStatusSeg.ActualDepartureDateTime) && string.IsNullOrEmpty(flightStatusSeg.ActualArrivalDateTime))
                                        {
                                            if (flightStatusSeg.Status.ToUpper().Contains("IN FLIGHT"))
                                            {
                                                flightStatusSeg.StatusShort = "In flight";
                                            }
                                            else if (flightStatusSeg.Status.ToUpper().Contains("DEPARTED"))
                                            {
                                                flightStatusSeg.StatusShort = "Departed";
                                            }
                                            else if (flightStatusSeg.Status.ToUpper().Contains("LANDED"))
                                            {
                                                flightStatusSeg.StatusShort = "Landed";
                                            }
                                        }

                                        if (!string.IsNullOrEmpty(flightStatusSeg.Status) && !string.IsNullOrEmpty(flightStatusSeg.ActualDepartureDateTime) && !string.IsNullOrEmpty(flightStatusSeg.ActualArrivalDateTime))
                                        {
                                            flightStatusSeg.StatusShort = "Landed";
                                        }

                                        if (flightSegmentStatus.Code.ToLower().Trim() == "cancelled")
                                        {
                                            flightStatusSeg.IsSegmentCancelled = true;
                                        }
                                        break;
                                    }
                                    #endregion
                                }
                            }
                            if (_configuration.GetValue<string>("GetCancelDelayReasonMessage") != null && Convert.ToBoolean(_configuration.GetValue<string>("GetCancelDelayReasonMessage").ToString()) && !BypassGetCancelDelayReasonMessage(seg))
                            {
                                #region Cancel Delay Reason Changes
                                var flightStatusObj = new Service.Presentation.SegmentModel.FlightStatus();
                                Genre longPulicDescription = null;

                                foreach (var flightSegmentStatus in seg.FlightStatuses)
                                {
                                    if (!string.IsNullOrEmpty(flightSegmentStatus.StatusType) && flightSegmentStatus.StatusType.ToLower().Trim() == "ShortFlightStatus".ToLower().Trim() && !String.IsNullOrEmpty(flightSegmentStatus.Code) && !flightSegmentStatus.Code.ToLower().Trim().Equals("adminmessage"))
                                    {
                                        flightStatusObj = flightSegmentStatus;

                                        break;
                                    }
                                }
                                if (seg.ReasonStatuses != null)
                                {
                                    foreach (var reasonStatus in seg.ReasonStatuses)
                                    {
                                        #region
                                        if (!string.IsNullOrEmpty(reasonStatus.IsDelayEffective) && reasonStatus.IsDelayEffective.Trim().ToUpper() == "1" && reasonStatus.ReasonDescriptions != null)
                                        {
                                            foreach (var reasonDescription in reasonStatus.ReasonDescriptions)
                                            {
                                                if (!string.IsNullOrEmpty(reasonDescription.Key) && reasonDescription.Key.ToUpper().Trim() == "LongPubDesc".ToUpper().Trim())
                                                {
                                                    longPulicDescription = new Genre();
                                                    longPulicDescription = reasonDescription;
                                                    break;
                                                }
                                            }
                                            break;
                                        }
                                        #endregion
                                    }
                                }
                                if (longPulicDescription != null)
                                {
                                    if (!string.IsNullOrEmpty(flightStatusObj.Code) && flightStatusObj.Code.ToLower().Trim() == "cancelled")
                                    {
                                        flightStatusSeg.Status = "Canceled due to " + longPulicDescription.Description;
                                    }
                                    else
                                    {
                                        flightStatusSeg.Status = "Delayed due to " + longPulicDescription.Description + " (" + flightStatusObj.Description + ")";
                                    }
                                }
                                #endregion Cancel Delay Reason Changes
                            }

                            flightStatusSeg.OperatingCarrier = new Airline
                            {
                                Code = seg.OperatingAirline.IATACode,
                                Name = seg.OperatingAirline.Name,
                                FlightNumber = seg.FlightNumber
                            };
                            flightStatusSeg.MarketingCarrier = new Airline
                            {
                                Code = "UA",
                                Name = "United Airlines"
                            };
                            flightStatusSeg.Ship = new Equipment();
                            if (seg.Equipment != null)
                            {
                                flightStatusSeg.Ship.Aircraft = new Aircraft
                                {
                                    Code = seg.Equipment.Model.Genre != null ? seg.Equipment.Model.Genre : seg.Equipment.Model.Fleet,
                                    LongName = seg.Equipment.Model.Description,
                                    ShortName = seg.Equipment.Model.Name ?? seg.Equipment.ShipNumber
                                };

                                if (!string.IsNullOrEmpty(seg.Equipment.NoseNumber))
                                {
                                    flightStatusSeg.Ship.Id = seg.Equipment.NoseNumber;
                                }
                                else
                                {
                                    flightStatusSeg.Ship.Id = seg.Equipment.TailNumber;
                                }
                                if (!string.IsNullOrEmpty(seg.Equipment.OwnerAirlineCode)
                                    && (seg.Equipment.OwnerAirlineCode.Trim().ToUpper() == "CO"
                                        || seg.Equipment.OwnerAirlineCode.Trim().ToUpper() == "UA")
                                    && !string.IsNullOrEmpty(seg.Equipment.NoseNumber))
                                {
                                    shipNumbers.Add(seg.Equipment.NoseNumber.Trim());
                                }
                                if (_configuration.GetValue<bool>("EnableTravelReadiness"))
                                {
                                    flightStatusSeg.Ship.NoseNumber = seg.Equipment.NoseNumber?.Trim();
                                    flightStatusSeg.Ship.TailNumber = seg.Equipment.TailNumber?.Trim();
                                }
                            }

                            if (!Convert.ToBoolean(_configuration.GetValue<string>("segregateEnablingFlightStatusTabs"))) //Enable Flight Status Segment Tab's # [Nizam] - 01/29/2018 - Bug #237138, 237143
                            {
                                string gateInfoCarriers = _configuration.GetValue<string>("GateInfoCarriers");

                                if (gateInfoCarriers.Contains(seg.OperatingAirline.IATACode))
                                {
                                    flightStatusSeg.EnableSeatMap = true;
                                    flightStatusSeg.EnableStandbyList = true;

                                    if (seg.Equipment == null)
                                    {
                                        flightStatusSeg.EnableAmenity = false;
                                    }
                                    else
                                    {
                                        flightStatusSeg.EnableAmenity = true;
                                    }
                                }
                            }
                            else
                            {
                                #region # Enable Flight Status Segment Tab's # [Nizam] - 01/29/2018 - Bug #237138, 237143
                                //Enable Amentities Tab
                                string amenitiesCarriersCode = _configuration.GetValue<string>("AmenitiesListCarriers");
                                if (amenitiesCarriersCode.Contains(seg.OperatingAirline.IATACode))
                                {
                                    if (seg.Equipment == null)
                                        flightStatusSeg.EnableAmenity = false;
                                    else
                                        flightStatusSeg.EnableAmenity = true;
                                }

                                //Enable SeatMap Tab
                                string seatMapCarriersCode = _configuration.GetValue<string>("SeatMapListCarriers");
                                if (seatMapCarriersCode.Contains(seg.OperatingAirline.IATACode))
                                {
                                    flightStatusSeg.EnableSeatMap = true;
                                }

                                //Enable StandBy Tab
                                string standByCarriersCode = _configuration.GetValue<string>("StandByListCarriers");
                                if (standByCarriersCode.Contains(seg.OperatingAirline.IATACode))
                                {
                                    flightStatusSeg.EnableStandbyList = true;
                                }
                                #endregion
                            }

                            int daysDiff = 0;

                            try
                            {
                                DateTime.TryParseExact(flightStatusSeg.ScheduledDepartureDateTime, "M/d/yyyy h:m tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime scheduledFlightdate);
                                TimeSpan dateDiff = scheduledFlightdate.Date - DateTime.Now.Date;
                                daysDiff = dateDiff.Days;
                            }
                            catch
                            {
                                string loggingInfo = "Flight Number: " + seg.FlightNumber + " - Flight Date: " + seg.DepartureDateTime + " - flightStatusSeg.ScheduledDepartureDateTime: " + flightStatusSeg.ScheduledDepartureDateTime;
                                _logger.LogInformation("Parsing ScheduledDepartureDateTime to DateTime to get daysDiff: {loggingInfo}", loggingInfo);
                            }

                            if (daysDiff >= -1) // Checking if the flight date is not for past 2 days to enable standby and upgradelist
                            {
                                string uaExpressWith2CabinAircraftCarriers = _configuration.GetValue<string>("UAExpressWith2CabinAircraftCarriers");

                                if (flightStatusSeg.OperatingCarrier != null && !string.IsNullOrEmpty(flightStatusSeg.OperatingCarrier.Code) && uaExpressWith2CabinAircraftCarriers.IndexOf(flightStatusSeg.OperatingCarrier.Code) != -1)
                                {
                                    string uaExpress2CabinAircrafts = _configuration.GetValue<string>("UAExpress2CabinAircrafts");
                                    if (flightStatusSeg.Ship.Aircraft != null && !string.IsNullOrEmpty(flightStatusSeg.OperatingCarrier.Code) && uaExpress2CabinAircrafts.IndexOf(flightStatusSeg.Ship.Aircraft.Code) != -1)
                                    {
                                        flightStatusSeg.EnableUpgradeList = true;
                                    }
                                }
                                else
                                {
                                    string upgradeListCarriers = _configuration.GetValue<string>("UpgradeListCarriers");
                                    var listOfAirports = new List<string>() { flightStatusSeg.Ship.Aircraft.ShortName.Replace("-", "") };
                                    var listOfShipment = await _flightRequestSqlSpOnPremService.GetCabinCountByShipNumber(listOfAirports, _headers.ContextValues.SessionId);
                                    var cabinCount = listOfShipment?.Count;
                                    flightStatusSeg.EnableUpgradeList = upgradeListCarriers.Contains(flightStatusSeg.OperatingCarrier.Code) || (flightStatusSeg.Ship.Aircraft != null) && cabinCount > 1;
                                }
                            }
                            else
                            {
                                flightStatusSeg.EnableUpgradeList = false;
                                flightStatusSeg.EnableStandbyList = false;
                            }

                            #endregion

                            #region
                            bool addAirportSegment = true, addDepartureAdded = true, addArrivalAirport = true, addDepartureAllAirport = true, addAllAirportArrival = true;

                            foreach (string airportSegment in advisoryCheckAirports)
                            {
                                if (airportSegment.ToUpper().Trim() == (flightStatusSeg.Departure.Code.Trim() + "-" + flightStatusSeg.Arrival.Code.Trim()).ToUpper().Trim())
                                {
                                    addAirportSegment = false;
                                }
                                else if (airportSegment.ToUpper().Trim() == (flightStatusSeg.Departure.Code.Trim() + "-" + flightStatusSeg.Departure.Code.Trim()).ToUpper().Trim())
                                {
                                    addDepartureAdded = false;
                                }
                                else if (airportSegment.ToUpper().Trim() == (flightStatusSeg.Arrival.Code.Trim() + "-" + flightStatusSeg.Arrival.Code.Trim()).ToUpper().Trim())
                                {
                                    addArrivalAirport = false;
                                }
                                else if (airportSegment.ToUpper().Trim() == (flightStatusSeg.Departure.Code.Trim() + "-XXX"))
                                {
                                    addDepartureAllAirport = false;
                                }
                                else if (airportSegment.ToUpper().Trim() == ("XXX-" + flightStatusSeg.Arrival.Code.Trim()).ToUpper().Trim())
                                {
                                    addAllAirportArrival = false;
                                }

                                if (!addAirportSegment && !addDepartureAdded && !addArrivalAirport && !addDepartureAllAirport && !addAllAirportArrival)
                                {
                                    break;
                                }
                            }

                            if (addAirportSegment)
                            {
                                advisoryCheckAirports.Add((flightStatusSeg.Departure.Code.Trim() + "-" + flightStatusSeg.Arrival.Code.Trim()).ToUpper().Trim());
                            }

                            if (addDepartureAdded)
                            {
                                advisoryCheckAirports.Add((flightStatusSeg.Departure.Code.Trim() + "-" + flightStatusSeg.Departure.Code.Trim()).ToUpper().Trim());
                            }

                            if (addArrivalAirport)
                            {
                                advisoryCheckAirports.Add((flightStatusSeg.Arrival.Code.Trim() + "-" + flightStatusSeg.Arrival.Code.Trim()).ToUpper().Trim());
                            }

                            if (addDepartureAllAirport)
                            {
                                advisoryCheckAirports.Add((flightStatusSeg.Departure.Code.Trim() + "-XXX").ToUpper().Trim());
                            }

                            if (addAllAirportArrival)
                            {
                                advisoryCheckAirports.Add(("XXX-" + flightStatusSeg.Arrival.Code.Trim()).ToUpper().Trim());
                            }
                            #endregion

                            flightStatusSegments.Add(flightStatusSeg);
                        }
                    }
                }
            }

            wiFiShipNumbers = await _flightRequestSqlSpOnPremService.GetCabinCountByShipNumber(shipNumbers, _headers.ContextValues.SessionId);
            if (wiFiShipNumbers != null && wiFiShipNumbers?.Count != 0)
            {
                List<FlightStatusSegment> updatedFlightStatusSegments = new List<FlightStatusSegment>();
                foreach (FlightStatusSegment segment in flightStatusSegments)
                {
                    foreach (string ship in wiFiShipNumbers)
                    {
                        if (segment.Ship.Id.ToUpper().Trim() == ship.ToUpper().Trim())
                        {
                            segment.ISWiFiAvailable = true;
                            break;
                        }
                    }
                    updatedFlightStatusSegments.Add(segment);
                }
                objFlightStatusInfo.Segments = updatedFlightStatusSegments;
            }
            else
            {
                objFlightStatusInfo.Segments = flightStatusSegments;
            }

            objFlightStatusInfo.AirportAdvisoryMessage = await GetAirportAdvisoryMessages(advisoryCheckAirports, flightDate, string.Empty);

            return objFlightStatusInfo;
            #endregion
        }

        public string GetFormattedFlightTravelTime(TimeSpan flightTravelTime)
        {
            string segmentScheduledFlightDuration = string.Empty;

            if (flightTravelTime > TimeSpan.Zero)
            {
                var flightTimeHours = flightTravelTime.Hours == 0 ? "" : (string.Format("{0}h", flightTravelTime.Hours) + " ");

                var flightTimeMinutes = string.Format("{0}m", flightTravelTime.Minutes);

                segmentScheduledFlightDuration = flightTimeHours + flightTimeMinutes;
            }
            return segmentScheduledFlightDuration;
        }

        public async Task<AirportAdvisoryMessage> GetAirportAdvisoryMessages(List<string> advisoryCheckAirports, string flightDate, string transactionId)
        {
            var airportAdvisoryMessages = new AirportAdvisoryMessage();

            if (_configuration.GetValue<bool>("CheckAirportAdvisoryMessages"))
            {
                bool isFirstRow = false;

                foreach (var item in advisoryCheckAirports)
                {
                    var advisoryMessage = await _flightStatusDynamoDB.GetAirportAdvisoryMessages<AirportAdvisoryMsg>(item, flightDate, transactionId);
                    if (advisoryMessage != null)
                    {
                        if (!isFirstRow)
                        {
                            airportAdvisoryMessages.ButtonTitle = advisoryMessage.ButtonTitle.S;
                            airportAdvisoryMessages.HeaderTitle = advisoryMessage.HeaderTitle.S;
                            isFirstRow = true;
                        }
                        Mobile.Model.FlightStatus.TypeOption typeOption = new Mobile.Model.FlightStatus.TypeOption();
                        typeOption.Key = advisoryMessage.DepAirportCode.S.Trim().ToUpper() == "XXX" ? advisoryMessage.ArrAirportCode.S.Trim().ToUpper() : advisoryMessage.DepAirportCode.S.Trim().ToUpper();
                        
                        //**NOTE**: As discussed with Priya, The logic above is for Check In Work FLow service expecting a Airport instead of segment (like SFO - EWR or SFO - All airports or All Airport - SFO) just want a Valid Airport Code if its Departure is all airports (XXX) then return arrival airport code and Priya confirmed Clients (Iphone , Andriod etc.. ) do not use this propery (key) its only for Check In Service
                        typeOption.Value = advisoryMessage.Message.S;
                        airportAdvisoryMessages.AdvisoryMessages.Add(typeOption);
                    }
                }
            }

            return airportAdvisoryMessages;
        }

        public string GetAirportSegmentListParameter(List<string> advisoryCheckAirports)
        {
            string segmentListParamter = "(";
            foreach (string segment in advisoryCheckAirports)
            {
                segmentListParamter = segmentListParamter + "'" + segment + "',";
            }
            segmentListParamter = segmentListParamter.Trim(',') + ")";
            return segmentListParamter;
        }

        public bool BypassGetCancelDelayReasonMessage(OperationalFlightSegment seg)
        {
            bool result = false;

            if (seg != null && seg.FlightStatuses != null && seg.FlightStatuses.Count > 0)
            {
                foreach (var flightStatus in seg.FlightStatuses)
                {
                    if (!string.IsNullOrEmpty(flightStatus.StatusType) && flightStatus.StatusType.ToLower().Trim().Equals("flightstatus") && !string.IsNullOrEmpty(flightStatus.Code) && flightStatus.Code.ToLower().Trim().Equals("adminmessage"))
                    {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }

    }
}