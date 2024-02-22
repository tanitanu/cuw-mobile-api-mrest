using Autofac;
using Autofac.Features.AttributeFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using United.Common.Helper;
using United.Common.Helper.EmployeeReservation;
using United.Common.Helper.FOP;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Ebs.Logging.Enrichers;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.ETC;
using United.Mobile.DataAccess.Loyalty;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.MPAuthentication;
using United.Mobile.DataAccess.MPRewards;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.DataAccess.ShopProducts;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.Model;
using United.Mobile.UpdateMemberProfile.Domain;
using United.Utility.Http;
using United.Utility.Middleware;
using United.Mobile.DataAccess.UnitedClub;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;
using United.Utility.Helper;
using United.Mobile.DataAccess.DynamoDB;
using System;
using System.Threading.Tasks;

namespace United.Mobile.UpdateMemberProfile.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddControllers();
            services.AddControllers()
                   .AddJsonOptions(opts => opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            services.AddControllers().AddNewtonsoftJson(option =>
            {
                option.SerializerSettings.Converters.Add(new StringEnumConverter());
            });

            services.AddScoped<IHeaders, Headers>();
            services.AddTransient<IUpdateMemberProfileBusiness, UpdateMemberProfileBusiness>();
            services.AddTransient<IUpdateMemberProfileUtility, UpdateMemberProfileUtility>();
            services.AddScoped<ISessionHelperService, SessionHelperService>();
            services.AddTransient<IShoppingUtility, ShoppingUtility>();
            services.AddTransient<IFFCShoppingcs, FFCShopping>();
            services.AddTransient<IOmniCart, OmniCart>();
            services.AddTransient<IShoppingSessionHelper, ShoppingSessionHelper>();
            services.AddScoped<ICustomerProfile, CustomerProfile>();
            services.AddScoped<ICorporateProfile, CorporateProfile>();
            services.AddScoped<IMPTraveler, MPTraveler>();
            services.AddScoped<IProfileCreditCard, ProfileCreditCard>();
            services.AddScoped<IEmpProfile, EmpProfile>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IEmployeeReservations, EmployeeReservations>();
            services.AddScoped<IMileagePlus, MileagePlus>();
            services.AddScoped<IETCService, ETCService>();
            services.AddScoped<IEmployeeReservations, EmployeeReservations>();
            services.AddScoped<IDataPowerFactory, DataPowerFactory>();
            services.AddScoped<IFormsOfPayment, FormsOfPayment>();
            services.AddScoped<IProductInfoHelper, ProductInfoHelper>();
            services.AddScoped<IRegisterCFOP, RegisterCFOP>();
            services.AddScoped<IPaymentUtility, PaymentUtility>();
            services.AddScoped<IShoppingBuyMiles, ShoppingBuyMiles>();
            services.AddScoped<IProductOffers, ProductOffers>();
            services.AddScoped<ICustomerPreferencesService, CustomerPreferencesService>();
            services.AddScoped<IInsertOrUpdateTravelInfoService, InsertOrUpdateTravelInfoService>();
            services.AddScoped<CacheLogWriter>();
            services.AddScoped(typeof(ICacheLog<>), typeof(CacheLog<>));
            if (Configuration.GetValue<bool>("SwitchToDynamoDB"))
                services.AddTransient<ILegalDocumentsForTitlesService, LegalDocumentForTitleServiceDynamoDB>();
            services.AddSingleton<IFeatureSettings, FeatureSettings>();
            services.AddSingleton<IAuroraMySqlService, AuroraMySqlService>();
            services.AddSingleton<IAWSSecretManager, AWSSecretManager>();
            services.AddSingleton<IDataSecurity, DataSecurity>();
            services.AddSingleton<IFeatureToggles, FeatureToggles>();

        }

        public void ConfigureContainer(ContainerBuilder container)
        {
            try
            {
                container.Register(c => new ResilientClient(Configuration.GetSection("dpTokenConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("dpTokenConfigKey");
                container.RegisterType<DPService>().As<IDPService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CMSContentClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CMSContentClientKey");
                container.RegisterType<CMSContentService>().As<ICMSContentService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("sessionConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("sessionConfigKey");
                container.RegisterType<PersistToken>().As<IPersistToken>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("sessionConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("sessionOnCloudConfigKey");
                container.RegisterType<SessionOnCloudService>().As<ISessionOnCloudService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("sessionConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("sessionConfigKey");
                container.RegisterType<SessionService>().As<ISessionService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("cachingConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("cachingConfigKey");
                container.RegisterType<CachingService>().As<ICachingService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("DynamoDBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("DynamoDBClientKey");
                container.RegisterType<DynamoDBService>().As<IDynamoDBService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("UtilitiesServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("UtilitiesServiceClientKey");
                container.RegisterType<UtilitiesService>().As<IUtilitiesService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("ReferencedataClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ReferencedataClientKey");
                container.RegisterType<ReferencedataService>().As<IReferencedataService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("DataVaultTokenClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("DataVaultTokenClientKey");
                container.RegisterType<DataVaultService>().As<IDataVaultService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CustomerDataClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CustomerDataClientKey");
                container.RegisterType<CustomerDataService>().As<ICustomerDataService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("MPSecurityQuestionsClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MPSecurityQuestionsClientKey");
                container.RegisterType<MPSecurityQuestionsService>().As<IMPSecurityQuestionsService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("LoyaltyUCBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LoyaltyUCBClientKey");
                container.RegisterType<LoyaltyUCBService>().As<ILoyaltyUCBService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CustomerPreferencesClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CustomerPreferencesClientKey");
                container.RegisterType<CustomerPreferencesService>().As<ICustomerPreferencesService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("PNRRetrievalClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PNRRetrievalClientKey");
                container.RegisterType<PNRRetrievalService>().As<IPNRRetrievalService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("EmployeeIdByMileageplusNumberClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("EmployeeIdByMileageplusNumberClientKey");
                container.RegisterType<EmployeeIdByMileageplusNumber>().As<IEmployeeIdByMileageplusNumber>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("MyAccountFutureFlightCreditClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MyAccountFutureFlightCreditClientKey");
                container.RegisterType<MPFutureFlightCredit>().As<IMPFutureFlightCredit>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("AccountPremierClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("AccountPremierClientKey");
                container.RegisterType<MyAccountPremierService>().As<IMyAccountPremierService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("LoyaltyWebClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LoyaltyWebClientKey");
                container.RegisterType<LoyaltyWebService>().As<ILoyaltyWebService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("LoyaltyAccountClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LoyaltyAccountClientKey");
                container.RegisterType<LoyaltyAccountService>().As<ILoyaltyAccountService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("employeeProfileClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("employeeProfileClientKey");
                container.RegisterType<EmployeeProfileService>().As<IEmployeeProfileService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("HashPinCodeDBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("HashPinCodeDataAccessService");
                container.RegisterType<HashpinCodeValidationService>().As<IHashpinCodeValidationService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("OnPremSQLServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ValidateHashPinOnPremSqlClientKey");
                container.RegisterType<ValidateHashPinService>().As<IValidateHashPinService>().WithAttributeFiltering();
                if (!Configuration.GetValue<bool>("SwitchToDynamoDB"))
                {
                    container.Register(c => new ResilientClient(Configuration.GetSection("LegalDocumentsOnPremSqlClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LegalDocumentsOnPremSqlClientKey");
                    container.RegisterType<LegalDocumentsForTitlesService>().As<ILegalDocumentsForTitlesService>().WithAttributeFiltering();
                }
                container.Register(c => new ResilientClient(Configuration.GetSection("ShoppingCcePromoClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ShoppingCcePromoClientKey");
                container.RegisterType<ShoppingCcePromoService>().As<IShoppingCcePromoService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("OptimizelyServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("OptimizelyServiceClientKey");
                container.RegisterType<OptimizelyPersistService>().As<IOptimizelyPersistService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("MPSecurityCheckDetailsClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MPSecurityCheckDetailsClientKey");
                container.RegisterType<MPSecurityCheckDetailsService>().As<IMPSecurityCheckDetailsService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("BaseEmployeeResClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("BaseEmployeeResClientKey");
                container.RegisterType<BaseEmployeeResService>().As<IBaseEmployeeResService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("eResEmployeeProfileClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("eResEmployeeProfileClientKey");
                container.RegisterType<EResEmployeeProfileService>().As<IEResEmployeeProfileService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("PKDispenserClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PKDispenserClientKey");
                container.RegisterType<PKDispenserService>().As<IPKDispenserService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("ShoppingCartClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ShoppingCartClientKey");
                container.RegisterType<ShoppingCartService>().As<IShoppingCartService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("PaymentServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PaymentServiceClientKey");
                container.RegisterType<PaymentService>().As<IPaymentService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("EServiceCheckinClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("EServiceCheckinClientKey");
                container.RegisterType<EServiceCheckin>().As<IEServiceCheckin>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("CustomerProfileContactpointsURL").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CustomerProfileContactpointsKey");
                container.RegisterType<InsertOrUpdateTravelInfoService>().As<IInsertOrUpdateTravelInfoService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("UnitedClubMembershipService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("UnitedClubMembershipKey");
                container.RegisterType<UnitedClubMembershipService>().As<IUnitedClubMembershipService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("UnitedClubMembershipV2Client").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("UnitedClubMembershipV2ClientKey");
                container.RegisterType<UnitedClubMembershipV2Service>().As<IUnitedClubMembershipV2Service>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CSLGetProfileTravelerDetailsService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLGetProfileTravelerDetailsServiceKey");
                container.RegisterType<CustomerProfileTravelerService>().As<ICustomerProfileTravelerService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CSLGetProfileTravelerService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLGetProfileTravelerServiceKey");
                container.RegisterType<CustomerTravelerService>().As<ICustomerTravelerService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CSLGetProfileOwnerService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLGetProfileOwnerServiceKey");
                container.RegisterType<CustomerProfileOwnerService>().As<ICustomerProfileOwnerService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CSLGetProfileCreditCardsService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLGetProfileCreditCardsServiceKey");
                container.RegisterType<CustomerProfileCreditCardsService>().As<ICustomerProfileCreditCardsService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CSLCorporateGetService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLCorporateGetServiceKey");
                container.RegisterType<CustomerCorporateProfileService>().As<ICustomerCorporateProfileService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("dpTokenValidateConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("dpTokenValidateKey");
                container.RegisterType<DPTokenValidationService>().As<IDPTokenValidationService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("ProvisionService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ProvisionServiceKey");
                container.RegisterType<ProvisionService>().As<IProvisionService>().WithAttributeFiltering();
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "ConfigureContainer-Unexpected error occurred while starting services");
            }

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApplicationEnricher applicationEnricher, IFeatureSettings featureSettings, IHostApplicationLifetime applicationLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            applicationEnricher.Add(Constants.ServiceNameText, Program.Namespace);
            applicationEnricher.Add(Constants.EnvironmentText, env.EnvironmentName);

            app.MapWhen(context => string.IsNullOrEmpty(context.Request.Path) || string.Equals("/", context.Request.Path), appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    await context.Response.WriteAsync("Welcome from UpdateMemberProfile Microservice");
                });
            });

            app.UseMiddleware<RequestResponseLoggingMiddleware>();
           
            //app.UseMiddleware<SetCultureMiddleware>();
            if (Configuration.GetValue<bool>("EnableFeatureSettingsChanges"))
            {
                applicationLifetime.ApplicationStarted.Register(async () => await OnStart(featureSettings));
                applicationLifetime.ApplicationStopping.Register(async () => await OnShutDown(featureSettings));
            }
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
        private async Task OnStart(IFeatureSettings featureSettings)
        {
            try
            {
                await featureSettings.LoadFeatureSettings(United.Mobile.Model.Common.ServiceNames.UPDATEMEMBERPROFILE.ToString());
            }
            catch (Exception) { }

        }
        private async Task OnShutDown(IFeatureSettings featureSettings)
        {
            try
            {
                await featureSettings.DeleteContainerIPAdress(United.Mobile.Model.Common.ServiceNames.UPDATEMEMBERPROFILE.ToString(), StaticDataLoader._ipAddress);
            }
            catch { }
        }

    }
}
