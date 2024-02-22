using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Internal.Common;

namespace United.Mobile.DataAccess.DynamoDB
{
    public class CodeTableDynamoDB
    {
        private readonly IConfiguration _configuration;
        private readonly IDynamoDBService _dynamoDBService;

        public CodeTableDynamoDB(IConfiguration configuration
            , IDynamoDBService dynamoDBService)
        {
            _configuration = configuration;
            _dynamoDBService = dynamoDBService;
        }

        public async Task<T> GetAirportsForSyncUpdatedAirportsRAMP<T>(string availableFlag, string sessionId)
        {
            #region
            //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
            //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("usp_Select_Airports_RAMP");
            //database.AddInParameter(dbCommand, "@AvailableFlag", DbType.Int32, availableFlag);

            #endregion
            try
            {
                return await _dynamoDBService.GetRecords<T>(_configuration?.GetValue<string>("DynamoDBTable:uatb_AirportList"), "airportsForSyncUpdatedAirportsRAMP01", availableFlag, sessionId);
            }
            catch
            {

            }

            return default;
        }

        public async Task<T> GetCarriersDetailsFromInventory<T>(bool isStarFlag, int applicationId, string sessionId)
        {
            #region
            //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
            //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("uasp_Get_CarriersDetails");
            //database.AddInParameter(dbCommand, "@IsStarFlag", DbType.Byte, Convert.ToByte(request.CarriersDetailsRequest.IsStarFlag));
            //database.AddInParameter(dbCommand, "@AppID", DbType.Byte, Convert.ToByte(request.Application.Id));
            //using (IDataReader dataReader = database.ExecuteReader(dbCommand))
            //{
            //    while (dataReader.Read())
            //    {
            //        if (responseCarriersDetails == null)
            //        {
            //            responseCarriersDetails = new List<MOBCarriersDetails>();
            //        }

            //        responseCarriersDetails.Add(new MOBCarriersDetails()
            //        {
            //            CarrierCode = dataReader["CarrierCode"].ToString().Trim(),
            //            CarrierShortName = dataReader["CarrierShortName"].ToString().Trim(),
            //            CarrierFullName = (dataReader["CarrierCode"].ToString().Trim() != "STAR") ? (dataReader["CarrierFullName"].ToString().Trim() + " (" + dataReader["CarrierCode"].ToString().Trim() + ")") : (dataReader["CarrierFullName"].ToString().Trim()),
            //            CarrierImageSrc = dataReader["CarrierImageSrc"].ToString().Trim(),
            //            CarrierImageName = dataReader["ImageName"].ToString().Trim()
            //        });
            //    }
            //}

            #endregion
            try
            {
                string key = isStarFlag + "::" + applicationId;
                return await _dynamoDBService.GetRecords<T>(_configuration?.GetValue<string>("DynamoDBTable:uatb_CarriersDetails"), "carriersDetails01", key, sessionId);
            }
            catch
            {

            }

            return default;
        }

        public async Task<T> GetNearestCOAirport<T>(string latitude, string Longitude, int radius, bool coAirport, string sessionId)
        {
            #region
            //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
            //DbCommand dbCommand = null;
            //if (coAirport)
            //{
            //    dbCommand = (DbCommand)database.GetStoredProcCommand("usp_GetNearestContinentalAirportGeoWithinMilesByLatitudeLongitude");
            //}
            //else
            //{
            //    dbCommand = (DbCommand)database.GetStoredProcCommand("usp_GetNearestPointInsideAirportGeoWithinMilesByLatitudeLongitude");
            //}

            //database.AddInParameter(dbCommand, "@Latitude", DbType.String, latitude);
            //database.AddInParameter(dbCommand, "@Longitude", DbType.String, Longitude);
            //database.AddInParameter(dbCommand, "@Miles", DbType.Int32, radius);

            #endregion
            try
            {
                string key = latitude + "::" + Longitude;
                return await _dynamoDBService.GetRecords<T>(_configuration?.GetValue<string>("DynamoDBTable:utb_AirportGeoInfo"), "nearestCOAAirport01", key, sessionId);
            }
            catch
            {

            }

            return default;
        }

        public async Task<bool> InsertVerifiedPassportDetailsToDB(
                 string idExpiry,
                 string idType,
                 string idDob,
                 string idCheckSignature,
                 string idCheckDataPositions,
                 string idCheckHologram,
                 string idCheckMicroprint,
                 string idCheckDocumentValidation,
                 string idCountry,
                 string idScanSource,
                 string idFirstName,
                 string verificationStatus,
                 string jumioIdScanReference,
                 string personalNumber,
                 string merchantIdScanReference,
                 string idCheckSecurityFeatures,
                 string idCheckMRZcode,
                 string idScanImage,
                 string callBackType,
                 string clientIp,
                 string idLastName,
                 string idAddress,
                 string idScanStatus,
                 string idNumber,
                 string rejectReason,
                 int applicationId,
                 string appVersion,
                 string deviceID,
                 string idGender,
                 string checkInGUID,
                 string pnr,
                 string sessionId
                 )
        {
            #region
            //    Database database = DatabaseFactory.CreateDatabase("ConnectionString - OCR-Jumio-Calls");
            //    DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("uasp_Insert_third_party_verified_passport_details");
            //    database.AddInParameter(dbCommand, "@idNumber", DbType.String, idNumber);
            //    database.AddInParameter(dbCommand, "@idScanStatus", DbType.String, idScanStatus);
            //    database.AddInParameter(dbCommand, "@idType", DbType.String, idType);
            //    database.AddInParameter(dbCommand, "@idExpiry", DbType.String, idExpiry);
            //    database.AddInParameter(dbCommand, "@idDob", DbType.String, idDob);
            //    database.AddInParameter(dbCommand, "@idCheckSignature", DbType.String, idCheckSignature);
            //    database.AddInParameter(dbCommand, "@idCheckDataPositions", DbType.String, idCheckDataPositions);
            //    database.AddInParameter(dbCommand, "@idCheckHologram", DbType.String, idCheckHologram);
            //    database.AddInParameter(dbCommand, "@idCheckMicroprint", DbType.String, idCheckMicroprint);
            //    database.AddInParameter(dbCommand, "@idCheckDocumentValidation", DbType.String, idCheckDocumentValidation);
            //    database.AddInParameter(dbCommand, "@idCountry", DbType.String, idCountry);
            //    database.AddInParameter(dbCommand, "@idScanSource", DbType.String, idScanSource);
            //    database.AddInParameter(dbCommand, "@idFirstName", DbType.String, idFirstName);
            //    database.AddInParameter(dbCommand, "@verificationStatus", DbType.String, verificationStatus);
            //    database.AddInParameter(dbCommand, "@jumioIdScanReference", DbType.String, jumioIdScanReference);
            //    database.AddInParameter(dbCommand, "@personalNumber", DbType.String, personalNumber);
            //    database.AddInParameter(dbCommand, "@merchantIdScanReference", DbType.String, merchantIdScanReference);
            //    database.AddInParameter(dbCommand, "@idCheckSecurityFeatures", DbType.String, idCheckSecurityFeatures);
            //    database.AddInParameter(dbCommand, "@idCheckMRZcode", DbType.String, idCheckMRZcode);
            //    database.AddInParameter(dbCommand, "@idScanImage", DbType.String, idScanImage);
            //    database.AddInParameter(dbCommand, "@callBackType", DbType.String, callBackType);
            //    database.AddInParameter(dbCommand, "@clientIp", DbType.String, clientIp);
            //    database.AddInParameter(dbCommand, "@idLastName", DbType.String, idLastName);
            //    database.AddInParameter(dbCommand, "@idAddress", DbType.String, idAddress);
            //    database.AddInParameter(dbCommand, "@rejectReason", DbType.String, rejectReason);
            //    database.AddInParameter(dbCommand, "@applicationId", DbType.Int32, applicationId);
            //    database.AddInParameter(dbCommand, "@appVersion", DbType.String, appVersion);
            //    database.AddInParameter(dbCommand, "@deviceID", DbType.String, deviceID);
            //    database.AddInParameter(dbCommand, "@idGender", DbType.String, idGender);
            //    database.AddInParameter(dbCommand, "@checkInGUID", DbType.String, checkInGUID);
            //    database.AddInParameter(dbCommand, "@pnr", DbType.String, pnr);

            #endregion
            try
            {
                string key = idNumber + "::" + personalNumber;
                var verifiedPassportDetails = new VerifiedPassportDetails
                {
                    IDExpiry = idExpiry,
                    IDType = idType,
                    IDDob = idDob,
                    IDCheckSignature = idCheckSignature,
                    IDCheckDataPositions = idCheckDataPositions,
                    IDCheckHologram = idCheckHologram,
                    IDCheckMicroprint = idCheckMicroprint,
                    IDCheckDocumentValidation = idCheckDocumentValidation,
                    IDCountry = idCountry,
                    IDScanSource = idScanSource,
                    IDFirstName = idFirstName,
                    VerificationStatus = verificationStatus,
                    JumioIdScanReference = jumioIdScanReference,
                    PersonalNumber = personalNumber,
                    MerchantIdScanReference = merchantIdScanReference,
                    IDCheckSecurityFeatures = idCheckSecurityFeatures,
                    IDCheckMRZcode = idCheckMRZcode,
                    IDScanImage = idScanImage,
                    CallBackType = callBackType,
                    ClientIp = clientIp,
                    IDLastName = idLastName,
                    IDAddress = idAddress,
                    IDScanStatus = idScanStatus,
                    IDNumber = idNumber,
                    RejectReason = rejectReason,
                    ApplicationId = applicationId,
                    ApplicationVersion = appVersion,
                    DeviceId = deviceID,
                    IDGender = idGender,
                    CheckInGUID = checkInGUID,
                    PNR = pnr,
                    SessionId = sessionId
                };
                return await _dynamoDBService.SaveRecords<VerifiedPassportDetails>(_configuration?.GetValue<string>("DynamoDBTable:uatb_third_party_verified_passport_details"), "insertVerifiedPassportDetailsToDB01", key, verifiedPassportDetails, sessionId);
            }
            catch
            {

            }

            return default;
        }

        public async Task<T> GetVerifiedPassportDetailsFromDB<T>(int id, string jumioIdScanReference, bool recordUpdated, string sessionId)
        {
            #region
            //Database database = DatabaseFactory.CreateDatabase("ConnectionString - OCR-Jumio-Calls");
            //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("uasp_get_third_party_verified_passport_details");
            //database.AddInParameter(dbCommand, "@id", DbType.String, id);
            //database.AddInParameter(dbCommand, "@jumioIdScanReference", DbType.String, jumioIdScanReference);
            //using (IDataReader dataReader = database.ExecuteReader(dbCommand))
            //{
            //    while (dataReader.Read())
            //    {
            //        recordUpdated = true;
            //        passportDetails.ID = Convert.ToInt32(dataReader["ID"].ToString());
            //        passportDetails.IDNumber = dataReader["idNumber"].ToString();
            //        passportDetails.IDScanStatus = dataReader["idScanStatus"].ToString();
            //        passportDetails.IDType = dataReader["idType"].ToString();
            //        passportDetails.IDExpiry = dataReader["idExpiry"].ToString();
            //        passportDetails.IDDob = dataReader["idDob"].ToString();
            //        passportDetails.IDCheckSignature = dataReader["idCheckSignature"].ToString();
            //        passportDetails.IDCheckDataPositions = dataReader["idCheckDataPositions"].ToString();
            //        passportDetails.IDCheckHologram = dataReader["idCheckHologram"].ToString();
            //        passportDetails.IDCheckMicroprint = dataReader["idCheckMicroprint"].ToString();
            //        passportDetails.IDCheckDocumentValidation = dataReader["idCheckDocumentValidation"].ToString();
            //        passportDetails.IDCountry = dataReader["idCountry"].ToString();
            //        passportDetails.IDScanSource = dataReader["idScanSource"].ToString();
            //        passportDetails.IDFirstName = dataReader["idFirstName"].ToString();
            //        passportDetails.VerificationStatus = dataReader["verificationStatus"].ToString();
            //        passportDetails.JumioIdScanReference = dataReader["jumioIdScanReference"].ToString();
            //        passportDetails.PersonalNumber = dataReader["personalNumber"].ToString();
            //        passportDetails.MerchantIdScanReference = dataReader["merchantIdScanReference"].ToString();
            //        passportDetails.IDCheckSecurityFeatures = dataReader["idCheckSecurityFeatures"].ToString();
            //        passportDetails.IDCheckMRZcode = dataReader["idCheckMRZcode"].ToString();
            //        passportDetails.IDScanImage = dataReader["idScanImage"].ToString();
            //        passportDetails.CallBackType = dataReader["callBackType"].ToString();
            //        passportDetails.ClientIp = dataReader["clientIp"].ToString();
            //        passportDetails.IDLastName = dataReader["idLastName"].ToString();
            //        passportDetails.IDAddress = dataReader["idAddress"].ToString();
            //        passportDetails.RejectReason = dataReader["rejectReason"].ToString();
            //        passportDetails.IDGender = dataReader["idGender"].ToString();
            //        passportDetails.Gender = GetgenderDescription(dataReader["idGender"].ToString());
            //        if (!String.IsNullOrEmpty(dataReader["insertedDateTime"].ToString()))
            //        {
            //            passportDetails.InsertedDateTime = Convert.ToDateTime(dataReader["insertedDateTime"]).ToString("MM/dd/yyyy hh:mm tt"); //dataReader["insertedDateTime"].ToString();
            //        }
            //        if (!String.IsNullOrEmpty(dataReader["updatedDateTime"].ToString()))
            //        {
            //            passportDetails.UpdatedDateTime = Convert.ToDateTime(dataReader["updatedDateTime"]).ToString("MM/dd/yyyy hh:mm tt");  //dataReader["updatedDateTime"].ToString();

            //        }
            //        passportDetails.DurationOfStay = dataReader["durationOfStay"].ToString();
            //        passportDetails.VisaCategory = dataReader["visaCategory"].ToString();
            //        passportDetails.NumberOfEntries = dataReader["numberOfEntries"].ToString();
            //        passportDetails.PassportNumber = dataReader["passportNumber"].ToString();
            //        passportDetails.IssuingDate = dataReader["issuingDate"].ToString();
            //    }
            //}

            #endregion
            try
            {
                string key = id + "::" + jumioIdScanReference;
                return await _dynamoDBService.GetRecords<T>(_configuration?.GetValue<string>("DynamoDBTable:uasp_get_third_party_verified_passport_details"), "verifiedPassportDetailsFromDB01", key, sessionId);
            }
            catch
            {

            }

            return default;
        }

        public async Task<T> GetAllCountryPhoneList<T>(string sessionId)
        {
            #region
            //Database database = DatabaseFactory.CreateDatabase(connectionString);
            //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("uasp_Select_CountryList");
            //using (IDataReader dataReader = database.ExecuteReader(dbCommand))
            //{
            //    while (dataReader.Read())
            //    {
            //        if (countryPhoneList == null)
            //        {
            //            countryPhoneList = new List<MOBCountryPhone>();
            //        }

            //        MOBCountryPhone countryPhone = new MOBCountryPhone
            //        {
            //            CountryName = !string.IsNullOrEmpty(Convert.ToString(dataReader["CountryName"])) ? Convert.ToString(dataReader["CountryName"]).Trim() : "",
            //            CountryCode = !string.IsNullOrEmpty(Convert.ToString(dataReader["CountryCode"])) ? Convert.ToString(dataReader["CountryCode"]).Trim() : "",
            //            CountryPhoneCode = !string.IsNullOrEmpty(Convert.ToString(dataReader["CountryPhoneCode"])) ? Convert.ToString(dataReader["CountryPhoneCode"]).Trim() : "",
            //            IsActive = !string.IsNullOrEmpty(Convert.ToString(dataReader["IsActive"])) ? Convert.ToString(dataReader["IsActive"]) : "0",
            //            IsActiveForNationality = !string.IsNullOrEmpty(Convert.ToString(dataReader["IsActiveForNationality"])) ? Convert.ToString(dataReader["IsActiveForNationality"]) : "0",
            //            IsActiveForPhoneCode = !string.IsNullOrEmpty(Convert.ToString(dataReader["IsActiveForPhoneCode"])) ? Convert.ToString(dataReader["IsActiveForPhoneCode"]) : "0",
            //            IsActiveForCountryOfResidence = !string.IsNullOrEmpty(Convert.ToString(dataReader["IsActiveForCountryOfResidence"])) ? Convert.ToString(dataReader["IsActiveForCountryOfResidence"]) : "0",
            //            IsActiveForPointOfSale = !string.IsNullOrEmpty(Convert.ToString(dataReader["IsActiveForPointOfSale"])) ? Convert.ToString(dataReader["IsActiveForPointOfSale"]) : "0",
            //            PointOfSaleCountryName = !string.IsNullOrEmpty(Convert.ToString(dataReader["PointOfSaleCountryName"])) ? Convert.ToString(dataReader["PointOfSaleCountryName"]).Trim() : "",
            //            PointOfSaleCountryOrder = (dataReader["PointOfSaleCountryOrder"] != DBNull.Value) ? Convert.ToInt32(dataReader["PointOfSaleCountryOrder"]) : 0
            //        };

            //        countryPhoneList.Add(countryPhone);
            //    }
            //}

            #endregion
            try
            {
                return await _dynamoDBService.GetAllRecordsByKeys<T>(_configuration?.GetValue<string>("DynamoDBTable:uatb_CountryList"), "TID1");
            }
            catch
            {

            }

            return default;
        }
    }
}