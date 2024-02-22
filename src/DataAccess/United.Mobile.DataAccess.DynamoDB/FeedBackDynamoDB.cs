using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using United.Mobile.DataAccess.Common;

namespace United.Mobile.DataAccess.DynamoDB
{
    public class FeedBackDynamoDB
    {
        private readonly IConfiguration _configuration;
        private readonly IDynamoDBService _dynamoDBService;

        public FeedBackDynamoDB(IConfiguration configuration
            , IDynamoDBService dynamoDBService)
        {
            _configuration = configuration;
            _dynamoDBService = dynamoDBService;
        }
        //public async Task<T> IsInBeta<T>(string mileagePlusNumber, int featureId, int applicationId, string applicationVersion, string deviceId, string sessionId)
        //{
        //    #region
        //    //DatabaseProviderFactory factory = new DatabaseProviderFactory();
        //    //Database database = factory.Create("ConnectionString - iPhone");
        //    //DbCommand dbCommand = database.GetStoredProcCommand("uasp_Select_BetaFeatureMPAccess");
        //    //database.AddInParameter(dbCommand, "@MileagePlusNumber", DbType.String, mileagePlusAccountNumber);
        //    //database.AddInParameter(dbCommand, "@FeatureId", DbType.Int32, featureId);
        //    //database.AddInParameter(dbCommand, "@ApplicationId", DbType.Int32, applicationId);
        //    //database.AddInParameter(dbCommand, "@ApplicationVersion", DbType.String, applicationVersion);
        //    //database.AddInParameter(dbCommand, "@DeviceId", DbType.String, string.Empty);
        //    //try
        //    //{
        //    //    using (IDataReader dataReader = database.ExecuteReader(dbCommand))
        //    //    {
        //    //        while (dataReader.Read())
        //    //        {
        //    //            isInBeta = dataReader["IsInBeta"].Equals(DBNull.Value) ? false : Convert.ToBoolean(dataReader["IsInBeta"]);
        //    //        }
        //    //    }
        //    //}
        //    //catch (Exception) { }

        //    #endregion

        //    #region StoredProcedure uasp_Select_BetaFeatureMPAccess
        //    //            USE[iPhone]
        //    //GO
        //    ///****** Object: StoredProcedure [dbo].[uasp_Select_BetaFeatureMPAccess] Script Date: 08-11-2021 23:39:18 ******/
        //    //SET ANSI_NULLS ON
        //    //GO
        //    //SET QUOTED_IDENTIFIER ON
        //    //GO



        //    //ALTER PROCEDURE[dbo].[uasp_Select_BetaFeatureMPAccess]
        //    //@MileagePlusNumber VARCHAR(8),
        //    //@FeatureId INT,
        //    //@ApplicationId INT,
        //    //@ApplicationVersion VARCHAR(16),
        //    //@DeviceId VARCHAR(64)
        //    //AS
        //    //BEGIN
        //    //SET NOCOUNT ON;



        //    //            DECLARE @IsInBeta BIT
        //    //            DECLARE @TesterId INT
        //    //DECLARE @OptOutEligible BIT
        //    //DECLARE @OptOutMileagePlusAccount BIT
        //    //DECLARE @OptOutDevice BIT



        //    //SET @IsInBeta = 0
        //    //SET @OptOutEligible = 1
        //    //SET @OptOutMileagePlusAccount = 0
        //    //SET @OptOutDevice = 0



        //    //SELECT @OptOutMileagePlusAccount = OptOutMileagePlusAccount, @OptOutDevice = OptOutDevice
        //    //FROM uatb_BetaFeature(NOLOCK)
        //    //WHERE
        //    //FeatureId = @FeatureId
        //    //AND Active = 1
        //    //AND BeginDateTimeUTC <= GETUTCDATE()
        //    //AND EndDateTimeUTC > GETUTCDATE()



        //    //IF EXISTS(
        //    //SELECT bt.TesterId
        //    //FROM uatb_BetaTester bt (NOLOCK)
        //    //INNER JOIN uatb_BetaFeature bf (NOLOCK)
        //    //ON bt.FeatureId = bf.FeatureId
        //    //WHERE
        //    //bt.MileagePlusNumber = 'ALL'
        //    //AND bt.FeatureId = @FeatureId
        //    //AND bt.ApplicationId = @ApplicationId
        //    //AND bt.AppVersion = @ApplicationVersion
        //    //AND bf.Active = 1
        //    //AND bf.BeginDateTimeUTC <= GETUTCDATE()
        //    //AND bf.EndDateTimeUTC > GETUTCDATE()
        //    //)
        //    //BEGIN
        //    //SET @IsInBeta = 1
        //    //SET @OptOutEligible = 0
        //    //END
        //    //ELSE
        //    //BEGIN
        //    //SELECT @TesterId = bt.TesterId
        //    //FROM uatb_BetaTester bt(NOLOCK)
        //    //INNER JOIN uatb_BetaFeature bf(NOLOCK)
        //    //ON bt.FeatureId = bf.FeatureId
        //    //WHERE
        //    //bt.MileagePlusNumber = @MileagePlusNumber
        //    //AND bt.FeatureId = @FeatureId
        //    //AND bt.ApplicationId = @ApplicationId
        //    //AND bt.AppVersion = @ApplicationVersion
        //    //AND bf.Active = 1
        //    //AND bf.BeginDateTimeUTC <= GETUTCDATE()
        //    //AND bf.EndDateTimeUTC > GETUTCDATE()
        //    //IF(@TesterId IS NOT NULL)
        //    //BEGIN
        //    //IF(@OptOutMileagePlusAccount = 1)
        //    //BEGIN
        //    //SET @IsInBeta = 1
        //    //END
        //    //ELSE
        //    //BEGIN
        //    //IF(@DeviceId IS NOT NULL)
        //    //BEGIN
        //    //IF NOT EXISTS(
        //    //SELECT TesterId
        //    //FROM uatb_BetaDevice (NOLOCK)
        //    //WHERE TesterId = @TesterId AND DeviceId = @DeviceId)
        //    //BEGIN
        //    //EXEC uasp_Insert_BetaDevice @TesterId, @DeviceId
        //    //SET @IsInBeta = 1
        //    //END
        //    //ELSE
        //    //BEGIN
        //    //IF EXISTS(
        //    //SELECT TesterId
        //    //FROM uatb_BetaDevice(NOLOCK)
        //    //WHERE TesterId = @TesterId AND DeviceId = @DeviceId AND OptOutDateTime IS NULL)
        //    //BEGIN
        //    //SET @IsInBeta = 1
        //    //END
        //    //END
        //    //END
        //    //END
        //    //END
        //    //END



        //    //SELECT @IsInBeta AS IsInBeta, @OptOutEligible AS OptOutEligible, @OptOutMileagePlusAccount AS OptOutMileagePlusAccount, @OptOutDevice AS OptOutDevice
        //    //END
        //    #endregion
        //    string key = mileagePlusNumber + "::" + deviceId;
        //    var tableName = _configuration?.GetSection("DynamoDBTable").GetValue<string>("uasp_Select_BetaFeatureMPAccess");
        //    return await _dynamoDBService.GetRecords<T>(tableName, "isInBeta01", key, sessionId);
        //}
        public async Task<bool> InsertFeedback<T>(T obj, string key, string sessionId)
        {
            #region
            //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
            //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("uasp_Insert_Feedback_OpinionLab");
            //database.AddInParameter(dbCommand, "@ApplicationId", DbType.Int32, applicationId);
            //database.AddInParameter(dbCommand, "@ApplicationVersion", DbType.String, applicationVersion);
            //database.AddInParameter(dbCommand, "@MileagePlusAccountNumber", DbType.String, mileagePlusAccountNumber);
            //database.AddInParameter(dbCommand, "@Text", DbType.String, Utility.profanityCheck(text));
            //database.AddInParameter(dbCommand, "@StarRating", DbType.String, starRating);
            //database.AddInParameter(dbCommand, "@Category", DbType.String, category);
            //database.AddInParameter(dbCommand, "@TaskAnswer", DbType.String, taskAnswer);
            //database.AddInParameter(dbCommand, "@DeviceModel", DbType.String, deviceModel);
            //database.AddInParameter(dbCommand, "@DeviceOSVersion", DbType.String, deviceOSVersion);
            //database.AddInParameter(dbCommand, "@Latitude", DbType.Double, latitude);
            //database.AddInParameter(dbCommand, "@Longitude", DbType.Double, longitude);
            //database.AddInParameter(dbCommand, "@Pnrs", DbType.String, pnrs);
            //database.AddInParameter(dbCommand, "@Answer1", DbType.String, answer1);
            //database.AddInParameter(dbCommand, "@Answer2", DbType.String, answer2);
            //database.AddInParameter(dbCommand, "@OpinionLabRequest", DbType.String, opinionLabRequest);
            //database.AddInParameter(dbCommand, "@OpinionLabResponse", DbType.String, opinionLabResponse);

            //            USE[iPhone]
            //GO
            ///****** Object: StoredProcedure [dbo].[uasp_Insert_Feedback_OpinionLab] Script Date: 08-11-2021 23:46:40 ******/
            //SET ANSI_NULLS ON
            //GO
            //SET QUOTED_IDENTIFIER ON
            //GO
            //ALTER PROCEDURE[dbo].[uasp_Insert_Feedback_OpinionLab]
            //@ApplicationId INT,
            //@ApplicationVersion NVARCHAR(16),
            //@MileagePlusAccountNumber NVARCHAR(32),
            //@Text NVARCHAR(1024),
            //@StarRating NVARCHAR(16),
            //@Category NVARCHAR(256),
            //@TaskAnswer NVARCHAR(256),
            //@DeviceModel NVARCHAR(128) = null,
            //@DeviceOSVersion NVARCHAR(128) = null,
            //@Latitude FLOAT = 0.0,
            //@longitude FLOAT = 0.0,
            //@Pnrs NVARCHAR(256) = null,
            //@Answer1 NVARCHAR(128) = null,
            //@Answer2 NVARCHAR(128) = null,
            //@OpinionLabRequest NVARCHAR(MAX) = null,
            //@OpinionLabResponse TEXT
            //AS



            //INSERT INTO uatb_Feedback(ApplicationId, ApplicationVersion, MileagePlusAccountNumber, Text, StarRating, Category, TaskAnswer, DeviceModel, DeviceOSVersion, Latitude, longitude, Pnrs, OpinionLabRequest, OpinionLabResponse, Answer1, Answer2, InsertDateTime)
            //VALUES(@ApplicationId, @ApplicationVersion, @MileagePlusAccountNumber, @Text, @StarRating, @Category, @TaskAnswer, @DeviceModel, @DeviceOSVersion, @Latitude, @longitude, @Pnrs, @OpinionLabRequest, @OpinionLabResponse, @Answer1, @Answer2, GETDATE())
            #endregion
            return await _dynamoDBService.SaveRecords<T>(_configuration?.GetValue<string>("DynamoDBTable:uatb_Feedback"), "insertFeedback01", key, obj, sessionId);
        }
    }
}
