using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.MemberProfile.Api.Controllers;
using United.Mobile.MemberProfile.Domain;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using Xunit;
using Constants = United.Mobile.Model.Constants;
using United.Utility.Helper;

namespace United.Mobile.Test.MemberProfileTests.Api
{
    public class MemberProfileControllerTests
    {
        private readonly Mock<ICacheLog<MemberProfileController>> _logger;
        private readonly Mock<IMemberProfileBusiness> _memberProfileBusiness;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly Mock<IHeaders> _moqHeader;
        private readonly MemberProfileController _memberProfileController;
        private readonly Mock<IFeatureSettings> _featureSetting;
        public MemberProfileControllerTests()
        {
            _configuration = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appSettings.test.json", optional: false, reloadOnChange: true)
             .Build();
            _moqHeader = new Mock<IHeaders>();
            _logger = new Mock<ICacheLog<MemberProfileController>>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _memberProfileBusiness = new Mock<IMemberProfileBusiness>();
            _memberProfileController = new MemberProfileController(_logger.Object, _configuration, _moqHeader.Object, _memberProfileBusiness.Object,_featureSetting.Object);
            SetupHttpContextAccessor();
        }
        private void SetupHttpContextAccessor()
        {
            var guid = Guid.NewGuid().ToString();
            var context = new DefaultHttpContext();
            context.Request.Headers[Constants.HeaderAppIdText] = "1";
            context.Request.Headers[Constants.HeaderAppMajorText] = "1";
            context.Request.Headers[Constants.HeaderAppMinorText] = "0";
            context.Request.Headers[Constants.HeaderDeviceIdText] = guid;
            _httpContextAccessor.Setup(_ => _.HttpContext).Returns(context);
        }
        [Fact]
        public void HealthCheck_Test()
        {
            string result = _memberProfileController.HealthCheck();
            Assert.True(result == "Healthy");
        }

        [Fact]
        public void GetContactUsDetails_Test()
        {
            var response = new MOBContactUsResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44" };
            var request = new Request<MOBContactUsRequest>()
            {
                Data = new MOBContactUsRequest()
                {
                    MileagePlusNumber = "",
                    HashValue = "",
                    Application = new MOBApplication()
                    {
                        Id = 2,
                        Version = new MOBVersion()
                        {
                            Major = "4.1.23"
                        }
                    },
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44"
                }
            };
            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";
            _memberProfileBusiness.Setup(p => p.GetContactUsDetails(request.Data)).Returns(Task.FromResult(response));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();

            var result = _memberProfileController.GetContactUsDetails(request.Data);
            Assert.True(result != null && result.Result.TransactionId == "EE64E779-7B46-4836-B261-62AE35498B44");
            Assert.True(result.Result.CallDuration > 0);
            Assert.True(result.Result.Exception == null);
        }

        [Fact]
        public void GetContactUsDetails_Exception_Test()
        {
            var response = new MOBContactUsResponse();
            var request = new Request<MOBContactUsRequest>()
            {
                Data = new MOBContactUsRequest()
                {
                    MileagePlusNumber = "",
                    HashValue = "",
                    Application = new MOBApplication()
                    {
                        Id = 2,
                        Version = new MOBVersion()
                        {
                            Major = "4.1.23"
                        }
                    },
                }
            };


            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";
            _memberProfileBusiness.Setup(p => p.GetContactUsDetails(request.Data)).ThrowsAsync(new System.Exception("United data services are not currently available."));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();

            var result = _memberProfileController.GetContactUsDetails(request.Data);
            Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            Assert.True(result.Result.CallDuration > 0);
        }

        [Fact]
        public void GetContactUsDetails_Exception_Test1()
        {
            var response = new MOBContactUsResponse();
            var request = new Request<MOBContactUsRequest>()
            {
                Data = new MOBContactUsRequest()
                {
                    MileagePlusNumber = "",
                    HashValue = "",
                    Application = new MOBApplication()
                    {
                        Id = 2,
                        Version = new MOBVersion()
                        {
                            Major = "4.1.23"
                        }
                    },
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44"
                }
            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";


            _memberProfileBusiness.Setup(p => p.GetContactUsDetails(request.Data)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();

            var result = _memberProfileController.GetContactUsDetails(request.Data);
            Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            Assert.True(result.Result.CallDuration > 0);
        }

        [Fact]
        public void RetrieveCustomerPreferences_Test()
        {
            var response = new MOBCustomerPreferencesResponse() { SessionId = "CD9ED989-7BD7-44E9-B4C4-1B5A1ACDEF15" };
            var request = new Request<MOBCustomerPreferencesRequest>()
            {
                Data = new MOBCustomerPreferencesRequest()
                {
                    HashValue = "",
                    MileagePlusNumber = "",
                    SessionId = "CD9ED989-7BD7-44E9-B4C4-1B5A1ACDEF15",
                    CustomerId = "164345762",
                    ProfileId = "",
                    Application = new MOBApplication()
                    {
                        Id = 1,
                        Version = new MOBVersion()
                        {
                            Major = "4.1.23"
                        }
                    },
                }
            };
            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.RetrieveCustomerPreferences(request.Data)).Returns(Task.FromResult(response));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();

            var result = _memberProfileController.RetrieveCustomerPreferences(request.Data);
            Assert.True(result.Result != null && result.Result.SessionId != null);
            Assert.True(result.Result.CallDuration > 0);
            Assert.True(result.Result.Exception == null);
        }

        [Fact]
        public void RetrieveCustomerPreferences_Exception_Test()
        {
            var response = new MOBCustomerPreferencesResponse();
            var request = new Request<MOBCustomerPreferencesRequest>()
            {
                Data = new MOBCustomerPreferencesRequest()
                {

                    HashValue = "",
                    MileagePlusNumber = "",
                    SessionId = "",
                    CustomerId = "",
                    ProfileId = "",
                    Application = new MOBApplication()
                    {
                        Id = 2,
                        Version = new MOBVersion()
                        {
                            Major = "4.1.23"
                        }
                    },
                }
            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.RetrieveCustomerPreferences(request.Data)).ThrowsAsync(new System.Exception("Sorry,something went wrong. Please try again."));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();

            var result = _memberProfileController.RetrieveCustomerPreferences(request.Data);
            Assert.True(result.Result != null && result.Result.Exception.Message.Equals("Sorry, something went wrong. Please try again."));
            Assert.True(result.Result.CallDuration > 0);
        }


        [Fact]
        public void RetrieveCustomerPreferences_Exception_Tes1()
        {
            var response = new MOBCustomerPreferencesResponse();
            var request = new Request<MOBCustomerPreferencesRequest>()
            {
                Data = new MOBCustomerPreferencesRequest()
                {
                    MileagePlusNumber = "",
                    HashValue = "",
                    Application = new MOBApplication()
                    {
                        Id = 2,
                        Version = new MOBVersion()
                        {
                            Major = "4.1.23"
                        }
                    },
                }
            };
            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.RetrieveCustomerPreferences(request.Data)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();

            var result = _memberProfileController.RetrieveCustomerPreferences(request.Data);
            Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            Assert.True(result.Result.CallDuration > 0);
        }

        [Fact]
        public void GetProfileOwner_Test()
        {
            var response = new MOBCustomerProfileResponse() { SessionId = "CD9ED989-7BD7-44E9-B4C4-1B5A1ACDEF15" };
            var request = new MOBCustomerProfileRequest()
            {
                AccessCode = "ACCESSCODE",
                CustomerId = 164345762,
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"
            };
            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.GetProfileOwner(request)).Returns(Task.FromResult(response));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();

            var result = _memberProfileController.GetProfileOwner(request);
            Assert.True(result.Result != null);
           // Assert.True(result.Result.CallDuration > 0);
            Assert.True(result.Result.Exception == null);
        }

        [Fact]
        public void GetProfileOwner_Exception_Test()
        {
            var response = new MOBCustomerPreferencesResponse();
            var request = new MOBCustomerProfileRequest()
            {
                AccessCode = "ACCESSCODE",
                CustomerId = 164345762,
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = ""
            };


            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.GetProfileOwner(request)).ThrowsAsync(new System.Exception("United data services are not currently available."));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();

            var result = _memberProfileController.GetProfileOwner(request);
            Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            //Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);
        }

        [Fact]
        public void GetProfileOwner_Exception_Test1()
        {
            var response = new MOBCustomerPreferencesResponse();
            var request = new MOBCustomerProfileRequest()
            {
                AccessCode = "ACCESSCODE",
                CustomerId = 164345762,
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = ""
            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.GetProfileOwner(request)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();

            var result = _memberProfileController.GetProfileOwner(request);
            Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            // Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);
        }

        [Fact]
        public void GetAccountSummaryWithMemberCardPremierActivity_Test()
        {
            var response = new MPAccountSummaryResponse();
            var request = new MPAccountValidationRequest()
            {
                AccessCode = "ACCESSCODE",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"
            };
            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.GetAccountSummaryWithMemberCardPremierActivity(request)).Returns(Task.FromResult(response));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();

            var result = _memberProfileController.GetAccountSummaryWithMemberCardPremierActivity(request);
            Assert.True(result.Result != null);
           Assert.True(result.Result.CallDuration > 0);
           // Assert.True(result.Result.Exception == null);
        }

        [Fact]
        public void GetAccountSummaryWithMemberCardPremierActivity_Exception_Test()
        {
            var response = new MPAccountSummaryResponse();
            var request = new MPAccountValidationRequest()
            {
                AccessCode = "ACCESSCODE",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"
            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.GetAccountSummaryWithMemberCardPremierActivity(request)).ThrowsAsync(new System.Exception("United data services are not currently available."));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();

            var result = _memberProfileController.GetAccountSummaryWithMemberCardPremierActivity(request);
            Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
             Assert.True(result.Result.CallDuration > 0);
        }

        [Fact]
        public void GetAccountSummaryWithMemberCardPremierActivity_Exception_Test1()
        {
            var response = new MPAccountSummaryResponse();
            var request = new MPAccountValidationRequest()
            {
                AccessCode = "ACCESSCODE",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"
            };
            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.GetAccountSummaryWithMemberCardPremierActivity(request)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();

            var result = _memberProfileController.GetAccountSummaryWithMemberCardPremierActivity(request);
            Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            Assert.True(result.Result.CallDuration > 0);
            //Assert.True(result != null);
        }

        [Fact]
        public void GetProfileCSL_CFOP_Test()
        {
            var response = new MOBCPProfileResponse() { };

            var request = new MOBCPProfileRequest()
            {
                AccessCode = "ACCESSCODE",
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"

            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.GetProfileCSL_CFOP(request)).Returns(Task.FromResult(response));


            // Act
            var result = _memberProfileController.GetProfileCSL_CFOP(request);
            // Assert
            Assert.True(result != null);

            Assert.True(result.Result.CallDuration > 0);
            Assert.True(result.Result.Exception == null);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);

        }

        [Fact]
        public void GetProfileCSL_CFOP_Exception()
        {
            var response = new MOBCPProfileResponse() { };

            var request = new MOBCPProfileRequest()
            {
                AccessCode = "ACCESSCODE",
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"
            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.GetProfileCSL_CFOP(request)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _memberProfileController.GetProfileCSL_CFOP(request);
            // Assert
            //Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            Assert.True(result != null);

            Assert.True(result.Result.CallDuration > 0);
            Assert.False(result.Result.Exception == null);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);


        }

        [Fact]
        public void GetProfileCSL_CFOP_MOBUnitedException()
        {
            var response = new MOBCPProfileResponse() { };

            var request = new MOBCPProfileRequest()
            {
                AccessCode = "ACCESSCODE",
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"
            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.GetProfileCSL_CFOP(request)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _memberProfileController.GetProfileCSL_CFOP(request);
            // Assert
            // Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            Assert.True(result != null);

            Assert.True(result.Result.CallDuration > 0);
            Assert.False(result.Result.Exception == null);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);


        }

        [Fact]
        public void GetEmpProfileCSL_CFOP_Test()
        {
            var response = new MOBCPProfileResponse() { };

            var request = new MOBCPProfileRequest()
            {
                AccessCode = "ACCESSCODE",
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"

            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.GetEmpProfileCSL_CFOP(request)).Returns(Task.FromResult(response));


            // Act
            var result = _memberProfileController.GetEmpProfileCSL_CFOP(request);
            // Assert
            Assert.True(result != null);
            //Assert.True(result.Result.CallDuration > 0);
            Assert.True(result.Result.Exception == null);
        }

        [Fact]
        public void GetEmpProfileCSL_CFOP_Exception()
        {
            var response = new MOBCPProfileResponse() { };

            var request = new MOBCPProfileRequest()
            {
                AccessCode = "ACCESSCODE",
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"
            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.GetEmpProfileCSL_CFOP(request)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _memberProfileController.GetEmpProfileCSL_CFOP(request);
            // Assert
            //Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            Assert.True(result != null);
            //Assert.True(result.Result.CallDuration > 0);
            Assert.False(result.Result.Exception == null);
        }

        [Fact]
        public void GetEmpProfileCSL_CFOP_MOBUnitedException()
        {
            var response = new MOBCPProfileResponse() { };

            var request = new MOBCPProfileRequest()
            {
                AccessCode = "ACCESSCODE",
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"
            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.GetEmpProfileCSL_CFOP(request)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _memberProfileController.GetEmpProfileCSL_CFOP(request);
            // Assert
            // Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            Assert.True(result != null);
            //Assert.True(result.Result.CallDuration > 0);
            Assert.False(result.Result.Exception == null);

        }

        [Fact]
        public void MPSignedInInsertUpdateTraveler_CFOP_Test()
        {
            var response = new MOBCPProfileResponse() { };

            var request = new MOBUpdateTravelerRequest()
            {
                AccessCode = "ACCESSCODE",
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"

            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.MPSignedInInsertUpdateTraveler_CFOP(request)).Returns(Task.FromResult(response));


            // Act
            var result = _memberProfileController.MPSignedInInsertUpdateTraveler_CFOP(request);
            // Assert
            Assert.True(result != null);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Fact]
        public void MPSignedInInsertUpdateTraveler_CFOP_Exception()
        {
            var response = new MOBCPProfileResponse() { };

            var request = new MOBUpdateTravelerRequest()
            {
                AccessCode = "ACCESSCODE",
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"
            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.MPSignedInInsertUpdateTraveler_CFOP(request)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _memberProfileController.MPSignedInInsertUpdateTraveler_CFOP(request);
            // Assert
            //Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            Assert.True(result != null);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Fact]
        public void MPSignedInInsertUpdateTraveler_CFOP_MOBUnitedException()
        {
            var response = new MOBCPProfileResponse() { };

            var request = new MOBUpdateTravelerRequest()
            {
                AccessCode = "ACCESSCODE",
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"
            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.MPSignedInInsertUpdateTraveler_CFOP(request)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _memberProfileController.MPSignedInInsertUpdateTraveler_CFOP(request);
            // Assert
            // Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            Assert.True(result != null);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);

        }

        [Theory]
       // [InlineData("applicationId", "appVersion", "accessCode", "transactionId", "languageCode")]
        [InlineData(1, "4.1.18", "ACCESSCODE", "605b2552-9609-407b-bf32-99246cb13343|2b6f0fef-5c9d-4175-a8b1-62859af35a94", "en-US")]
        public void GetLatestFrequentFlyerRewardProgramList_Test(int applicationId, string appVersion, string accessCode, string transactionId, string languageCode)
        {
            var response = new FrequentFlyerRewardProgramsResponse() { };

            _memberProfileBusiness.Setup(p => p.GetLatestFrequentFlyerRewardProgramList( applicationId,  appVersion,  accessCode,  transactionId,  languageCode)).Returns(Task.FromResult(response));

            var result = _memberProfileController.GetLatestFrequentFlyerRewardProgramList(applicationId, appVersion, accessCode, transactionId, languageCode);
            
            //Assert
            Assert.True(result.Result != null);
            
        }

        [Fact]
        public void GetLatestFrequentFlyerRewardProgramList_Exception()
        {
            var response = new FrequentFlyerRewardProgramsResponse() { };

            _memberProfileBusiness.Setup(p => p.GetLatestFrequentFlyerRewardProgramList(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("Error Message"));

            var result = _memberProfileController.GetLatestFrequentFlyerRewardProgramList(1, "4.1.18", "ACCESSCODE", "605b2552-9609-407b-bf32-99246cb13343|2b6f0fef-5c9d-4175-a8b1-62859af35a94", "en-US");

            //Assert
           // Assert.True(result.Result != null);
            Assert.True(result.Exception != null || result.Result != null);

        }

        [Fact]
        public void GetLatestFrequentFlyerRewardProgramList_MOBUnitedException()
        {
            var response = new FrequentFlyerRewardProgramsResponse() { };

            _memberProfileBusiness.Setup(p => p.GetLatestFrequentFlyerRewardProgramList(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new System.Exception("Error Message"));

            var result = _memberProfileController.GetLatestFrequentFlyerRewardProgramList(1, "4.1.18", "ACCESSCODE", "605b2552-9609-407b-bf32-99246cb13343|2b6f0fef-5c9d-4175-a8b1-62859af35a94", "en-US");

            //Assert
            // Assert.True(result.Result != null);
            Assert.True(result.Exception != null || result.Result != null);
            Assert.True(result.Result.Exception.Message == "United data services are not currently available.");
        }

        [Fact]
        public void UpdateTravelerCCPromo_CFOP_Test()
        {
            var response = new MOBCPProfileResponse() { };

            var request = new MOBUpdateTravelerRequest()
            {
                AccessCode = "ACCESSCODE",
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"

            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.UpdateTravelerCCPromo_CFOP(request)).Returns(Task.FromResult(response));


            // Act
            var result = _memberProfileController.UpdateTravelerCCPromo_CFOP(request);
            // Assert
            Assert.True(result != null);
        }

        [Fact]
        public void UpdateTravelerCCPromo_CFOP_Exception()
        {
            var response = new MOBCPProfileResponse() { };

            var request = new MOBUpdateTravelerRequest()
            {
                AccessCode = "ACCESSCODE",
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"
            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.UpdateTravelerCCPromo_CFOP(request)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _memberProfileController.UpdateTravelerCCPromo_CFOP(request);
            // Assert
            //Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            Assert.True(result != null);
        }

        [Fact]
        public void UpdateTravelerCCPromo_CFOP_MOBUnitedException()
        {
            var response = new MOBCPProfileResponse() { };

            var request = new MOBUpdateTravelerRequest()
            {
                AccessCode = "ACCESSCODE",
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US"
            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.UpdateTravelerCCPromo_CFOP(request)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _memberProfileController.UpdateTravelerCCPromo_CFOP(request);
            // Assert
            // Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            Assert.True(result != null);

        }

        [Fact]
        public void UpdateTravelersInformation_Test()
        {
            var response = new MOBUpdateTravelerInfoResponse() { };

            var request = new MOBUpdateTravelerInfoRequest()
            {
                Token = "dgCVACtd7Oc:APA91bGRw3rfnI39OHpXGsTgCNGKT_yaclvZ6L_MLG5yPpqkvYhg97GxTb_BfW785Aa7o-_woFCIYYC9GmPgkIRaZzdZKeylMibPMu1PZB5Va6fT4Vk4pjOYoiuHVZAY93IUKswcEtFD",
                RecordLocator = "abc",
                AccessCode = "ACCESSCODE",
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US",
                TravelersInfo = new List<MOBPNRPassenger>()
                {
                    new MOBPNRPassenger 
                    {
                         KnownTravelerNumber = "A12",
                         KTNDisplaySequence = "Sequence"
                    }
                },

            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.UpdateTravelersInformation(request)).Returns(Task.FromResult(response));


            // Act
            var result = _memberProfileController.UpdateTravelersInformation(request);
            // Assert
            Assert.True(result != null);
        }

        [Fact]
        public void UpdateTravelersInformation_Exception()
        {
            var response = new MOBUpdateTravelerInfoResponse() { };

            var request = new MOBUpdateTravelerInfoRequest()
            {
                Token = "dgCVACtd7Oc:APA91bGRw3rfnI39OHpXGsTgCNGKT_yaclvZ6L_MLG5yPpqkvYhg97GxTb_BfW785Aa7o-_woFCIYYC9GmPgkIRaZzdZKeylMibPMu1PZB5Va6fT4Vk4pjOYoiuHVZAY93IUKswcEtFD",
                RecordLocator = "abc",
                AccessCode = "ACCESSCODE",
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US",
                TravelersInfo = new List<MOBPNRPassenger>()
                {
                    new MOBPNRPassenger
                    {
                         KnownTravelerNumber = "A12",
                         KTNDisplaySequence = "Sequence"
                    }
                },
            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.UpdateTravelersInformation(request)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _memberProfileController.UpdateTravelersInformation(request);
            // Assert
            //Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            Assert.True(result != null);
        }

        [Fact]
        public void UpdateTravelersInformation_MOBUnitedException()
        {
            var response = new MOBUpdateTravelerInfoResponse() { };

            var request = new MOBUpdateTravelerInfoRequest()
            {
                Token = "dgCVACtd7Oc:APA91bGRw3rfnI39OHpXGsTgCNGKT_yaclvZ6L_MLG5yPpqkvYhg97GxTb_BfW785Aa7o-_woFCIYYC9GmPgkIRaZzdZKeylMibPMu1PZB5Va6fT4Vk4pjOYoiuHVZAY93IUKswcEtFD",
                RecordLocator = "abc",
                AccessCode = "ACCESSCODE",
                SessionId = "0A5D5398AB074B11B5B50A99A8D18BD8",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.18"
                    }
                },
                LanguageCode = "en-US",
                TravelersInfo = new List<MOBPNRPassenger>()
                {
                    new MOBPNRPassenger
                    {
                         KnownTravelerNumber = "A12",
                         KTNDisplaySequence = "Sequence"
                    }
                },
            };

            _memberProfileController.ControllerContext = new ControllerContext();
            _memberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _memberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _memberProfileBusiness.Setup(p => p.UpdateTravelersInformation(request)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _memberProfileController.UpdateTravelersInformation(request);
            // Assert
            // Assert.True(result.Result != null && result.Result.Exception.Message.Equals("United data services are not currently available."));
            Assert.True(result != null);

        }


    }

    }


