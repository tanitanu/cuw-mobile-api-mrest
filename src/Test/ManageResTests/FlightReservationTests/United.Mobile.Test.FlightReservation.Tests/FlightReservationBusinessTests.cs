using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using United.Common.Helper;
using United.Common.Helper.ManageRes;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightReservation;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.FlightReservation.Domain;
using United.Mobile.Model;
using United.Mobile.Model.FlightReservation;
using United.Mobile.Model.ManageRes;
using United.Utility.Helper;
using Xunit;

namespace United.Mobile.Test.FlightReservation.Tests
{
    public class FlightReservationBusinessTests
    {
        private readonly Mock<ICacheLog<FlightReservationBusiness>> _logger;
        private readonly IConfiguration _configuration;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly Mock<IShoppingSessionHelper> _shoppingSessionHelper;
        private readonly Mock<IFlightReservation> _flightReservation;
        private readonly Mock<IDynamoDBService> _dynamoDBService;
        private readonly Mock<IRequestReceiptByEmailService> _requestReceiptByEmailService;
        private readonly Mock<ManageResUtility> _manageResUtility;
        private readonly FlightReservationBusiness _flightReservationBusiness;
        private readonly Mock<ILegalDocumentsForTitlesService> _legalDocumentsForTitlesService;
        private readonly Mock<IHeaders> _headers;
        public FlightReservationBusinessTests()
        {
            _logger = new Mock<ICacheLog<FlightReservationBusiness>>();
            _configuration = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appSettings.test.json", optional: false, reloadOnChange: true)
             .Build();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _shoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _flightReservation = new Mock<IFlightReservation>();
            _dynamoDBService = new Mock<IDynamoDBService>();
            _requestReceiptByEmailService = new Mock<IRequestReceiptByEmailService>();
            _manageResUtility = new Mock<ManageResUtility>();
            _legalDocumentsForTitlesService = new Mock<ILegalDocumentsForTitlesService>();
            _headers = new Mock<IHeaders>();
            _flightReservationBusiness = new FlightReservationBusiness(_logger.Object, _configuration, _sessionHelperService.Object, 
                _shoppingSessionHelper.Object, _flightReservation.Object, _dynamoDBService.Object, /*_requestReceiptByEmailService.Object, */
                _legalDocumentsForTitlesService.Object, _headers.Object );
        }
        private string GetFileContent(string fileName)
        {
            fileName = string.Format("..\\..\\..\\TestData\\{0}", fileName);
            var path = Path.IsPathRooted(fileName) ? fileName
                                                : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetPNRsByMileagePlusNumber), MemberType = typeof(TestDataGenerator))]
        public void GetPNRsByMileagePlusNumber_Test(MOBPNRByMileagePlusRequest request)
        {
            _manageResUtility.Setup(p => p.isValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(true);
            _manageResUtility.Setup(p => p.ValidateAccountFromCache(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            var result = _flightReservationBusiness.GetPNRsByMileagePlusNumber(request.Application.Id, request.Application.Version.ToString(),request.AccessCode,request.TransactionId,request.MileagePlusNumber,null,request.ReservationType.ToString(),request.LanguageCode);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.Exception == null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }
        [Theory]
        [MemberData(nameof(TestDataGenerator.RequestReceiptByEmail), MemberType = typeof(TestDataGenerator))]
        public void RequestReceiptByEmail_Test(MOBReceiptByEmailRequest request)
        {
            var CommonDef = GetFileContent("CommonDef.json");
            var CommonDefData = JsonConvert.DeserializeObject<Model.Common.Session>(CommonDef);
           // _sessionHelperService.Setup(p => p.GetSession<CommonDef>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(CommonDefData);
            var result = _flightReservationBusiness.RequestReceiptByEmail(request);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.Exception == null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }
    }
}
