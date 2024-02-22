using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using United.Mobile.DataAccess.MPAuthentication;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.MPSignIn;
using United.Utility.Helper;
using JsonSerializer = United.Utility.Helper.DataContextJsonSerializer;

namespace United.Common.Helper.EmployeeReservation
{
    public class EmployeeReservations:IEmployeeReservations
    {
        private readonly ICacheLog<EmployeeReservations> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmployeeProfileService _employeeProfileService;
        private readonly IEResEmployeeProfileService _eResEmployeeProfileService;
        private readonly IHeaders _headers;

        public EmployeeReservations(ICacheLog<EmployeeReservations> logger
            , IConfiguration configuration
            , IEmployeeProfileService employeeProfileService
            , IEResEmployeeProfileService eResEmployeeProfileService
            , IHeaders headers)
        {
            _logger = logger;
            _configuration = configuration;
            _employeeProfileService = employeeProfileService;
            _eResEmployeeProfileService = eResEmployeeProfileService;
            _headers = headers;
        }
        public MOBEmpTravelTypeAndJAProfileResponse GetTravelTypesAndJAProfile(MOBEmpTravelTypeAndJAProfileRequest request)
        {
            string strServerUpdateTimes = _configuration.GetValue<string>("empJAReloadTimes");
            int intWaitTime = _configuration.GetValue<int>("empJAReloadWaitMinutes");
            int intExpireCacheTime = _configuration.GetValue<int>("empJAReladExpireHours");
            MOBCacheDataResponse cacheData = new MOBCacheDataResponse();
            cacheData = GetCacheDataID(strServerUpdateTimes, intWaitTime, intExpireCacheTime, request.SessionId, request.DeviceId, request.MPNumber, request.Application.Id, request.Application.Version.Major, "MOBEmpTravelTypeAndJAProfileResponse");
            //Check if Data is current
            //Utility.InsertCacheData
            MOBEmpTravelTypeAndJAProfileResponse empTravelTypeAndJAProfileResponse = new MOBEmpTravelTypeAndJAProfileResponse();
            //empJAReladExpireHours
            if (!cacheData.BlnRefresh || (cacheData.CacheData != "" && cacheData.CacheData != null))
            {
                
                empTravelTypeAndJAProfileResponse = JsonConvert.DeserializeObject<MOBEmpTravelTypeAndJAProfileResponse>(cacheData.CacheData);
                //Hashing the employee ID for Stephen Copley's test account for his demo/video that he's making.
                //This account belongs to Gemma Egana and she is aware and agreed to let us use it for this purpose.
                if (!string.IsNullOrEmpty(_configuration.GetValue<string>("empHashEmployeeNumber")))
                {
                    bool blnHashEmployeeID = false;
                    if (_configuration.GetValue<bool>("empHashEmployeeNumber"))
                    {
                        blnHashEmployeeID = true;
                    }
                    if (blnHashEmployeeID)
                    {
                        if (!string.IsNullOrEmpty(_configuration.GetValue<string>("empEmployeeNumberToHash")))
                        {
                            string strEmployeeIDsToHash = _configuration.GetValue<string>("empEmployeeNumberToHash");
                            if (!string.IsNullOrEmpty(empTravelTypeAndJAProfileResponse.MOBEmpTravelTypeResponse.DisplayEmployeeId) && strEmployeeIDsToHash.Length > 0)
                            {
                                if (strEmployeeIDsToHash.Trim().ToUpper().Contains(request.MPNumber.Trim().ToUpper()))
                                {
                                    //empTravelTypeAndJAProfileResponse.DisplayEmployeeId = "U000000";
                                    empTravelTypeAndJAProfileResponse.MOBEmpTravelTypeResponse.DisplayEmployeeId = "U000000";
                                }
                            }
                        }
                    }
                }
            }
            if (cacheData.BlnRefresh)
            {
                try
                {
                    empTravelTypeAndJAProfileResponse.TransactionId = request.TransactionId;
                    MOBEmpTravelType empTravelType = new MOBEmpTravelType();
                    BookingTypesResponse bookingTypes = new BookingTypesResponse();

                    //to do
                    //Eres Wrapper Migration
                    //Starts Here
                    //string url = string.Empty;
                    //EmployeeRes.ClientProxy.EmployeeResProxyClient proxy;

                    //if (!Utility.EnableeRESMigration())
                    //{
                    //    url = ConfigurationManager.AppSettings["eResApiURL"].ToString().Trim();
                    //    proxy = new EmployeeRes.ClientProxy.EmployeeResProxyClient(url);
                    //    proxy.RequiredAudits = new employeeRes.Models.RequiredAudits
                    //    {
                    //        DeviceId = request.DeviceId,
                    //        MPNumber = request.MPNumber,
                    //        EmployeeId = request.EmployeeID
                    //    };
                    //}
                    //else
                    //{
                    //    proxy = SetURLDpTokenToEresProxy(request.DeviceId, request.MPNumber, request.EmployeeID, request.TokenId);
                    //}
                    //Ends Here
                    //Log Request;
                    //if (this.levelSwitch.TraceInfo)
                    //{
                    //    logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "eRes - GetEmployeeEligibleBookingTypes", "Request", string.Format("URL={0} | DeviceId={1} | MPNumber={2} | EmployeeId={3}", url, request.DeviceId, request.MPNumber, request.EmployeeID)));
                    //}

                    //bookingTypes = proxy.GetEmployeeEligibleBookingTypes(request.EmployeeID);
                    ////            bookingTypes = proxy.GetEligibleBookingTypes(employeeID);

                    //Log Response
                    //to do
                    //if (this.levelSwitch.TraceInfo)
                    //{
                    //    logEntries.Add(LogEntry.GetLogEntry<employeeRes.Models.BookingTypesResponse>(request.SessionId, "eRes - GetEmployeeEligibleBookingTypes", "Response", bookingTypes));
                    //    logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "eRes - GetEmployeeEligibleBookingTypes", "eResTransactionId", bookingTypes.TransactionId));
                    //    logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "eRes - GetEmployeeEligibleBookingTypes", "CallDuration", bookingTypes.CallDuration.ToString()));
                    //}

                    List<MOBEmpTravelTypeItem> empTravelTypeObjs = new List<MOBEmpTravelTypeItem>();
                    if (bookingTypes.BookingTypes != null && bookingTypes.BookingTypes.Count != 0)
                    {
                        foreach (var bookingType in bookingTypes.BookingTypes)
                        {

                            MOBEmpTravelTypeItem empTravelTypeObj = new MOBEmpTravelTypeItem();
                            empTravelTypeObj.Advisory = "";
                            empTravelTypeObj.IsAuthorizationRequired = false;
                            empTravelTypeObj.IsEligible = true;
                            empTravelTypeObj.NumberOfTravelers = 1;
                            if (bookingTypes.NumberOfPassengersInJA > 1)
                            {
                                empTravelTypeObj.NumberOfTravelers = bookingTypes.NumberOfPassengersInJA;
                            }

                            empTravelTypeObj.TravelType = bookingType.DisplayCode;

                            if (bookingType.DisplayCode == "RA")
                            {
                                bookingType.Display = _configuration.GetValue<string>("RevenueAwardUILabel");
                            }
                            else if (bookingType.DisplayCode == "E20")
                            {
                                bookingType.Display = _configuration.GetValue<string>("Employee20UILabel");
                            }
                            else if (bookingType.DisplayCode == "P")
                            {
                                bookingType.Display = _configuration.GetValue<string>("PersonalLeisureUILabel");
                            }
                            else if (bookingType.DisplayCode == "B")
                            {
                                if (bookingType.Display.ToLower().Trim().Contains("authorization"))
                                {
                                    empTravelTypeObj.IsAuthorizationRequired = true;
                                }
                                bookingType.Display = _configuration.GetValue<string>("BusinessUILabel");
                                if (empTravelTypeObj.IsAuthorizationRequired)
                                {
                                    bookingType.Display = _configuration.GetValue<string>("BusinessUILabelAuthRequired");
                                }
                                empTravelTypeObj.Advisory = bookingTypes.PositiveSpaceAlertMessage;
                            }
                            empTravelTypeObj.TravelTypeDescription = bookingType.Display;
                            empTravelTypeObjs.Add(empTravelTypeObj);
                        }
                        empTravelType.EmpTravelTypes = empTravelTypeObjs;

                        empTravelType.IsTermsAndConditionsAccepted = bookingTypes.IsTermsAndConditionsAccepted;
                        empTravelType.NumberOfPassengersInJA = bookingTypes.NumberOfPassengersInJA;

                        MOBEmpTravelTypeResponse mobEmpTravelTypeResponse = new MOBEmpTravelTypeResponse();
                        mobEmpTravelTypeResponse.EResTransactionId = bookingTypes.TransactionId;
                        empTravelTypeAndJAProfileResponse.MOBEmpTravelTypeResponse = mobEmpTravelTypeResponse;
                        empTravelTypeAndJAProfileResponse.MOBEmpTravelTypeResponse.EmpTravelType = empTravelType;
                    }
                    else
                    {
                        throw new MOBUnitedException("BookingTypes are Empty");
                    }

                    empTravelTypeAndJAProfileResponse.MOBEmpJAResponse = LoadEmployeeJA(bookingTypes);
                    //JD - Populating IsPayrollDeduct from TravelElligibilities instead of GetEmployeeProfile (moved from MileagePlusCSLController)
                    if (bookingTypes.EmployeeJA != null && bookingTypes.EmployeeJA.Airlines != null && bookingTypes.EmployeeJA.Airlines.Count > 0)
                    {
                        foreach (var airline in bookingTypes.EmployeeJA.Airlines)
                        {
                            if (airline.PaymentDetails != null)
                            {
                                if (!string.IsNullOrEmpty(airline.PaymentDetails.PayrollDeduct) && (airline.PaymentDetails.PayrollDeduct.Trim().ToUpper() == "Y" || airline.PaymentDetails.PayrollDeduct.Trim().ToUpper() == "YES"))
                                {
                                    empTravelTypeAndJAProfileResponse.MOBEmpTravelTypeResponse.IsPayrollDeduct = true;
                                }
                            }
                        }
                        /*
                        
                         */
                    }
                    //string serializejson = United.Json.Serialization.JsonSerializer.NewtonSoftSerializeToJson<United.Definition.Emp.MOBEmpTravelTypeAndJAProfileResponse>(empTravelTypeAndJAProfileResponse);
                    string commonRequestJSON = JsonConvert.SerializeObject(empTravelTypeAndJAProfileResponse);
                    MOBInsertCacheDataRequest mobRequest = new MOBInsertCacheDataRequest();
                    mobRequest.IntID = cacheData.Id;
                    mobRequest.StrGUID = request.SessionId;
                    mobRequest.StrDeviceID = request.DeviceId;
                    mobRequest.StrMPNumber = request.MPNumber;
                    mobRequest.IntAppId = request.Application.Id;
                    mobRequest.StrAppVersion = request.Application.Version.Major;
                    mobRequest.StrCacheData = commonRequestJSON;
                    mobRequest.StrDataDescription = "MOBEmpTravelTypeAndJAProfileResponse";
                    InsertCacheData(mobRequest);
                }
                catch (System.Exception ex)
                {
                    //Do something or just let it fall through?
                }
            }
            return empTravelTypeAndJAProfileResponse;
        }
        public static void InsertCacheData(MOBInsertCacheDataRequest request)
        {
            /*
[dbo].[uasp_Insert_Update_CacheData]
	@Id	bigint,
	@Guid		nvarchar(64),
	@DeviceId	nvarchar(256),
	@MPNumber	nvarchar(64),
	@AppId		int,
	@AppVersion	nvarchar(64),
	@CacheData	nvarchar(MAX),
	@DataDescription	nvarchar(64)
            */

            try
            {
                //to do
                //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
                //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("uasp_Insert_Update_CacheData");
                //database.AddInParameter(dbCommand, "@Id", DbType.String, request.IntID);
                //database.AddInParameter(dbCommand, "@Guid", DbType.String, request.StrGUID);
                //database.AddInParameter(dbCommand, "@DeviceId", DbType.String, request.StrDeviceID);
                //database.AddInParameter(dbCommand, "@MPNumber", DbType.String, request.StrMPNumber);
                //database.AddInParameter(dbCommand, "@AppId", DbType.String, request.IntAppId);
                //database.AddInParameter(dbCommand, "@AppVersion", DbType.String, request.StrAppVersion);
                //database.AddInParameter(dbCommand, "@CacheData", DbType.String, request.StrCacheData);
                //database.AddInParameter(dbCommand, "@DataDescription", DbType.String, request.StrDataDescription);

                //database.ExecuteNonQuery(dbCommand);
            }
            catch (System.Exception) { }

        }
        public MOBEmpJAResponse LoadEmployeeJA(BookingTypesResponse bookingTypes)
        {
            MOBEmpJA empJA = new MOBEmpJA();
            MOBEmpJAResponse empJAResponse = new MOBEmpJAResponse();
            MOBEmpPassRiderExtended empPassRiderExtended = new MOBEmpPassRiderExtended();
            MOBEmployeeProfileExtended empProfileExtended = new MOBEmployeeProfileExtended();

            List<MOBEmpBuddy> empBuddies = new List<MOBEmpBuddy>();
            List<MOBEmpPassRider> empPassRiders = new List<MOBEmpPassRider>();
            List<MOBEmpPassRider> empSuspendedPassRiders = new List<MOBEmpPassRider>();
            List<MOBEmpJAByAirline> empJAByAirlines = new List<MOBEmpJAByAirline>();

            List<MOBEmpBuddy> empLoggedInBuddies = new List<MOBEmpBuddy>();
            List<MOBEmpPassRider> empLoggedInPassRiders = new List<MOBEmpPassRider>();
            List<MOBEmpPassRider> empLoggedInSuspendedPassRiders = new List<MOBEmpPassRider>();
            List<MOBEmpJAByAirline> empLoggedInJAByAirlines = new List<MOBEmpJAByAirline>();

            //These got logged when the call was made in GetTravelTypesAndJAProfile
            //if (this.levelSwitch.TraceInfo)
            //{
            //    //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "eRes - GetEmployeeJA", "Request", string.Format("URL={0}-DeviceId={1}-MPNumber={2}-EmployeeId={3}", System.Configuration.ConfigurationManager.AppSettings["eResApiURL"], request.DeviceId, session.MPNumber, session.EmployeeId)));
            //}


            ////Log Response
            //if (this.levelSwitch.TraceInfo)
            //{
            //    //logEntries.Add(LogEntry.GetLogEntry<employeeRes.Models.EmployeeProfileResponse>(request.SessionId, "eRes - GetEmployeeProfile", "Response", employeeProfileResponse));
            //    //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "eRes - GetEmployeeProfile", "CallDuration", request.Application.Id, request.Application.Version.Major, request.DeviceId, employeeProfileResponse.CallDuration.ToString()));
            //    //                logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "eRes - GetEmployeeProfile", "CallDuration", employeeProfileResponse.CallDuration.ToString()));
            //}

            empJAResponse.EmpJA = empJA;

            if (bookingTypes != null)
            {
                #region Buddies
                if (bookingTypes != null && bookingTypes.EmployeeJA != null && bookingTypes.EmployeeJA.Buddies != null && bookingTypes.EmployeeJA.Buddies.Count != 0)
                {
                    foreach (var buddy in bookingTypes.EmployeeJA.Buddies)
                    {
                        MOBEmpBuddy empBuddy = new MOBEmpBuddy();
                        empBuddy.BirthDate = buddy.BirthDate;
                        empBuddy.Email = buddy.DayOfEmail;
                        empBuddy.Gender = buddy.Gender;
                        empBuddy.Name.First = buddy.FirstName;
                        empBuddy.Name.Last = buddy.LastName;
                        empBuddy.Name.Middle = buddy.MiddleName;
                        empBuddy.Name.Suffix = buddy.NameSuffix;
                        empBuddy.OwnerCarrier = buddy.OwnerCarrier;
                        empBuddy.Phone = buddy.DayOfPhone;
                        empBuddy.Redress = buddy.Redress;
                        empBuddy.EmpRelationship.Relationship = buddy.Relationship.Relationship;
                        empBuddy.EmpRelationship.RelationshipDescription = buddy.Relationship.RelationshipDescription;
                        empBuddy.EmpRelationship.RelationshipSubType = buddy.Relationship.RelationshipSubType;
                        empBuddy.EmpRelationship.RelationshipSubTypeDescription = buddy.Relationship.RelationshipSubTypeDescription;
                        empBuddies.Add(empBuddy);
                    }
                    empJAResponse.EmpJA.EmpBuddies = empBuddies;
                }
                #endregion

                #region PassRiders
                if (bookingTypes != null && bookingTypes.EmployeeJA != null && bookingTypes.EmployeeJA.PassRiders != null && bookingTypes.EmployeeJA.PassRiders.Count != 0)
                {
                    foreach (var passrider in bookingTypes.EmployeeJA.PassRiders)
                    {
                        MOBEmpPassRider empPassRider = new MOBEmpPassRider();
                        empPassRider.Age = passrider.Age;
                        empPassRider.BirthDate = passrider.BirthDate.ToString();
                        empPassRider.DependantID = passrider.DependantID;

                        empPassRider.FirstBookingBuckets = passrider.FirstBookingBuckets;
                        empPassRider.Gender = passrider.Gender;
                        empPassRider.MustUseCurrentYearPasses = passrider.MustUseCurrentYearPasses;

                        empPassRiders.Add(empPassRider);
                    }
                    empJAResponse.EmpJA.EmpPassRiders = empPassRiders;
                }
                #endregion

                #region Suspended PassRiders
                if (bookingTypes != null && bookingTypes.EmployeeJA != null && bookingTypes.EmployeeJA.SuspendedPassRiders != null && bookingTypes.EmployeeJA.SuspendedPassRiders.Count != 0)
                {
                    foreach (var spassrider in bookingTypes.EmployeeJA.SuspendedPassRiders)
                    {
                        MOBEmpPassRider empSuspendedPassRider = new MOBEmpPassRider();
                        empSuspendedPassRider.Age = spassrider.Age;
                        empSuspendedPassRider.BirthDate = spassrider.BirthDate.ToString();
                        empSuspendedPassRider.DependantID = spassrider.DependantID;
                        empSuspendedPassRider.FirstBookingBuckets = spassrider.FirstBookingBuckets;
                        empSuspendedPassRider.Gender = spassrider.Gender;
                        empSuspendedPassRider.MustUseCurrentYearPasses = spassrider.MustUseCurrentYearPasses;
                        empSuspendedPassRider.PrimaryFriend = spassrider.PrimaryFriend;
                        empSuspendedPassRider.UnaccompaniedFirst = spassrider.UnaccompaniedFirst;
                        empSuspendedPassRiders.Add(empSuspendedPassRider);
                    }
                    empJAResponse.EmpJA.EmpSuspendedPassRiders = empSuspendedPassRiders;
                }
                #endregion

                #region Airlines
                if (bookingTypes.EmployeeJA.Airlines.Count != 0)
                {
                    foreach (var airline in bookingTypes.EmployeeJA.Airlines)
                    {
                        MOBEmpJAByAirline empJAByAirline = new MOBEmpJAByAirline();
                        empJAByAirline.AirlineCode = airline.AirlineCode;
                        empJAByAirline.AirlineDescription = airline.AirlineDescription;
                        empJAByAirline.BoardDate = airline.BoardDate;
                        empJAByAirline.BuddyPassClass = airline.BuddyPassClass;
                        empJAByAirline.BusinessPassClass = airline.BusinessPassClass;
                        empJAByAirline.CanBookFirstOnBusiness = airline.CanBookFirstOnBusiness;
                        empJAByAirline.DeviationPassClass = airline.DeviationPassClass;
                        //to do empJAByAirline.Display = airline.Display;
                        empJAByAirline.ETicketIndicator = airline.ETicketIndicator;
                        empJAByAirline.ExtendedFamilyPassClass = airline.ExtendedFamilyPassClass;
                        empJAByAirline.FamilyPassClass = airline.FamilyPassClass;
                        empJAByAirline.FamilyVacationPassClass = airline.FamilyVacationPassClass;
                        //to do
                        //empJAByAirline.FeeWaivedCoach = airline.FeeWaivedCoach;
                        //empJAByAirline.FeeWaivedFirst = airline.FeeWaivedFirst;
                        empJAByAirline.JumpSeatPassClass = airline.JumpSeatPassClass;
                        empJAByAirline.PaymentIndicator = airline.PaymentIndicator;
                        empJAByAirline.PersonalPassClass = airline.PersonalPassClass;
                        empJAByAirline.ScheduleEngineCode = airline.ScheduleEngineCode;
                        empJAByAirline.Seniority = airline.Seniority;
                        empJAByAirline.SeniorityDate = airline.SeniorityDate;
                        empJAByAirline.SuspendEndDate = airline.SuspendEndDate;
                        empJAByAirline.SuspendStartDate = airline.SuspendStartDate;
                        empJAByAirline.TrainingPassClass = airline.TrainingPassClass;
                        empJAByAirline.VacationPassClass = airline.VacationPassClass;
                        empJAByAirlines.Add(empJAByAirline);

                    }
                    empJAResponse.EmpJA.EmpJAByAirlines = empJAByAirlines;
                }
                #endregion

                #region EmployeeProfileExtended
                if (bookingTypes.EmployeeProfile != null && bookingTypes.EmployeeProfile.ExtendedProfile != null)
                {

                    empProfileExtended.Email = bookingTypes.EmployeeProfile.ExtendedProfile.Email;
                    empProfileExtended.HomePhone = bookingTypes.EmployeeProfile.ExtendedProfile.HomePhone;
                    empProfileExtended.FaxNumber = bookingTypes.EmployeeProfile.ExtendedProfile.FaxNumber;
                    empProfileExtended.WorkPhone = bookingTypes.EmployeeProfile.ExtendedProfile.WorkPhone;

                    empJAResponse.EmpProfileExtended = empProfileExtended;
                }
                #endregion
                empJAResponse.AllowImpersonation = bookingTypes.AllowImpersonation;
                empJAResponse.ImpersonateType = bookingTypes.ImpersonateType;
                empJAResponse.LanguageCode = bookingTypes.LanguageCode;
                empJAResponse.MachineName = bookingTypes.MachineName;
                empJAResponse.TransactionId = bookingTypes.TransactionId;

            }
            else
            {
                //logEntries.Add(LogEntry.GetLogEntry<employeeRes.Models.Exception>(request.SessionId, "eRes - GetEmployeeJA", "Response", employeeProfileResponse.Exception));
            }

            return empJAResponse;
        }

        public static MOBCacheDataResponse GetCacheDataID(string strServerUpdateTimes, int intWaitTime, int intExpireTime, string strGUID, string strDeviceID, string strMPNumber, int intAppId, string strAppVersion, string strDataDescription)
        {
            MOBCacheDataResponse mobReturn = new MOBCacheDataResponse();
            mobReturn.Id = 0;
            mobReturn.BlnRefresh = true;
            /*
[dbo].[uasp_Get_CacheData_UpdateDate]
	@Guid		nvarchar(64),
	@DeviceId	nvarchar(256),
	@MPNumber	nvarchar(64),
	@AppId		int,
	@AppVersion	nvarchar(64),
	@DataDescription	nvarchar(64)
    
    //Returns: Id, LastRefreshDateTime
            */
            //Call Stored proc, return Last Updated Time Stamp and ID
            try
            {
                DateTime dteLastServerUpdate = System.DateTime.Now;

                //TODO..
                //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
                //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("uasp_Get_CacheData_UpdateDate");
                //database.AddInParameter(dbCommand, "@Guid", DbType.String, strGUID);
                //database.AddInParameter(dbCommand, "@DeviceId", DbType.String, strDeviceID);
                //database.AddInParameter(dbCommand, "@MPNumber", DbType.String, strMPNumber);
                //database.AddInParameter(dbCommand, "@AppId", DbType.Int32, intAppId);
                //database.AddInParameter(dbCommand, "@AppVersion", DbType.String, strAppVersion);
                //database.AddInParameter(dbCommand, "@DataDescription", DbType.String, strDataDescription);

                //using (IDataReader dataReader = database.ExecuteReader(dbCommand))
                //{
                //    while (dataReader.Read())
                //    {
                //        DateTime dteExpire = System.DateTime.Now.AddHours(-intExpireTime);
                //        mobReturn.Id = Convert.ToInt32(dataReader["Id"].ToString());
                //        mobReturn.LastUpdateDateTime = Convert.ToDateTime(dataReader["LastRefreshDateTime"].ToString());
                //        if (mobReturn.LastUpdateDateTime > dteExpire)
                //        {
                //            mobReturn.CacheData = dataReader["CacheData"].ToString();
                //        }
                //        else
                //        {
                //            mobReturn.CacheData = "";
                //        }
                //    }
                //}
                if (strServerUpdateTimes != null && strServerUpdateTimes.Length > 0)
                {
                    string[] arrStrings = strServerUpdateTimes.Split('|');
                    List<DateTime> lstDates = new List<DateTime>();
                    DateTime dteToday = System.DateTime.Now;
                    string strDate = "";
                    DateTime dteActive = dteToday;
                    if (arrStrings.Length > 0)
                    {
                        int intUpdateTimesCount = arrStrings.Length + 1;
                        for (int i = 0; i < intUpdateTimesCount; i++)
                        {
                            if (i < intUpdateTimesCount - 1)
                            {
                                strDate = dteActive.Month.ToString() + "/" + dteActive.Day.ToString() + "/" + dteActive.Year.ToString() + " " + arrStrings[i];
                                lstDates.Add(Convert.ToDateTime(strDate));
                            }
                            else
                            {
                                dteActive = dteToday.AddDays(-1);
                                strDate = dteActive.Month.ToString() + "/" + dteActive.Day.ToString() + "/" + dteActive.Year.ToString() + " " + arrStrings[0];
                                lstDates.Add(Convert.ToDateTime(strDate));
                            }
                        }
                        if (lstDates != null && lstDates.Count > 0)
                        {
                            for (int x = 0; x < lstDates.Count; x++)
                            {
                                if (System.DateTime.Now > lstDates[x])
                                {
                                    dteLastServerUpdate = lstDates[x];
                                    if (mobReturn.LastUpdateDateTime > dteLastServerUpdate.AddMinutes(intWaitTime))
                                    {
                                        mobReturn.BlnRefresh = false;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception) { }

            return mobReturn;
        }

        public async Task<United.Service.Presentation.PersonModel.EmployeeTravelProfile> GetEmployeeProfile(int applicationId, string applicationVersion, string deviceId, string employeeId, string token, string sessionId)
        {
            United.Service.Presentation.PersonModel.EmployeeTravelProfile empProfile = null;

            try
            {
                string path = string.Format("{0}", employeeId);

                //if (this.levelSwitch.TraceInfo)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "eRes - GetEmployeeProfile", "Request", jsonRequest));
                //}

                //string jsonResponse = HttpHelper.Get(jsonRequest, "application/json; charset=utf-8", token);
                string jsonResponse = await _employeeProfileService.GetEmployeeProfile(token, path, sessionId);
                //if (this.levelSwitch.TraceInfo)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "eRes - GetEmployeeProfile", "Response", jsonResponse));
                //}
                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    empProfile = DataContextJsonSerializer.NewtonSoftDeserialize<United.Service.Presentation.PersonModel.EmployeeTravelProfile>(jsonResponse);
                }
            }
            catch (System.Exception ex)
            {
                //if (this.levelSwitch.TraceInfo)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "eRes - GetEmployeeProfile", "Exception", ex.ToString()));
                //}
            }

            return empProfile;
        }

        public async Task<EmployeeJA> GetEResEmp20PassriderDetails(string employeeId, string token, string TransactionId, int ApplicationId, string AppVersion, string DeviceId)
        {
            var encryptedEmployeeId = new AesEncryptAndDecrypt().Encrypt(employeeId);
            string path = $"/Employee/Emp20PassriderDetails?employeeID={HttpUtility.UrlEncode(encryptedEmployeeId)}";

            EmployeeJA response;

            try
            {
                //string jsonResponse = HttpHelper.Get(url, "application/json; charset=utf-8", token);
                response = await _eResEmployeeProfileService.GetEResEmpProfile<EmployeeJA>(token, path, _headers.ContextValues.SessionId).ConfigureAwait(false);
            }
            catch (System.Net.WebException webException)
            {
                _logger.LogError("GetEmpProfileCSL_CFOP - eResEmp20PassriderDetails Error {@WebException}", JsonConvert.SerializeObject(webException));
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.LogError("GetEmpProfileCSL_CFOP - eResEmp20PassriderDetails Error {@Exception}", JsonConvert.SerializeObject(ex));
                throw;
            }

            return response;
        }
    }
}
