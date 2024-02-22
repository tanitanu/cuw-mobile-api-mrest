using Autofac;
using Autofac.Features.AttributeFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Converters;
using Serilog;
using System;
using System.Globalization;
using System.IO;
using System.Text.Json.Serialization;
using United.Common.Helper;
using United.Common.Helper.EmployeeReservation;
using United.Common.Helper.FlightStatus;
using United.Common.Helper.FOP;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Ebs.Logging.Enrichers;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.ETC;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.FlightStatus;
using United.Mobile.DataAccess.Loyalty;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.MerchandizeService;
using United.Mobile.DataAccess.MPAuthentication;
using United.Mobile.DataAccess.MPRewards;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.DataAccess.ShopProducts;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.DataAccess.Travelers;
using United.Mobile.DataAccess.UnitedClub;
using United.Mobile.MemberProfile.Domain;
using United.Mobile.Model;
using United.Utility.Helper;
using United.Utility.Http;
using United.Utility.Middleware;
using System;
using System.Threading.Tasks;
using United.Mobile.DataAccess.ShopBundles;

namespace United.Mobile.MemberProfile.Api
{
    public class Startup
    {
        private Microsoft.Extensions.Hosting.IHostingEnvironment CurrentEnvironment { get; set; }
        public Startup(IConfiguration configuration, Microsoft.Extensions.Hosting.IHostingEnvironment env)
        {
            Configuration = configuration;
            CurrentEnvironment = env;
        }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddControllers();
                services.AddControllers()
                     .AddJsonOptions(opts => opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
                services.AddControllers().AddNewtonsoftJson(option =>
                 {
                     option.SerializerSettings.Converters.Add(new StringEnumConverter());
                 });                
                services.AddTransient<IMemberProfileBusiness, MemberProfileBusiness>();
                services.AddTransient<IMemberProfileUtility, MemberProfileUtility>(); 
                services.AddScoped<ISessionHelperService, SessionHelperService>(); 
                services.AddTransient<ICustomerProfile, CustomerProfile>(); 
                services.AddTransient<IProfileXML, ProfileXml>(); 
                services.AddTransient<IMerchandizingServices, United.Common.Helper.Merchandize.MerchandizingServices>(); 
                services.AddTransient<IMerchOffersService, MerchOffersService>(); 
                services.AddTransient<IShoppingUtility, ShoppingUtility>();
                services.AddTransient<IMileagePlus, MileagePlus>();
                services.AddTransient<IShoppingSessionHelper, ShoppingSessionHelper>();
                services.AddTransient<IOmniCart, OmniCart>();
                services.AddScoped<IDataPowerFactory, DataPowerFactory>();
                services.AddScoped<IHeaders, Headers>();
                services.AddTransient<IProfileService, ProfileService>();
                services.AddTransient<IETCService, ETCService>();
                services.AddTransient<IFlightStatusHelper, FlightStatusHelper>();
                services.AddTransient<IBaggageInfo, InitialBaggageInfo>();
                services.AddTransient<IEmployeeReservations, EmployeeReservations>();
                services.AddTransient<IMPTraveler, MPTraveler>(); 
                services.AddTransient<ICorporateProfile, CorporateProfile>();
                services.AddTransient<IProfileCreditCard, ProfileCreditCard>();
                services.AddTransient<IProductInfoHelper, ProductInfoHelper>();
                services.AddTransient<ITraveler, Traveler>();
                services.AddTransient<ITravelerUtility, TravelerUtility>();
                services.AddTransient<IFFCShoppingcs, FFCShopping>();
                services.AddTransient<IShoppingBuyMiles, ShoppingBuyMiles>();
                services.AddTransient<IEmpProfile, EmpProfile>();
                services.AddTransient<ILoyaltyPromotionsService, LoyaltyPromotionsService>();
                services.AddTransient<IShopBundleService, ShopBundleService>();
                services.AddScoped<IInsertOrUpdateTravelInfoService, InsertOrUpdateTravelInfoService>();
                services.AddScoped<IFormsOfPayment, FormsOfPayment>();
                services.AddScoped<IProductOffers, ProductOffers>();
                services.AddScoped<IPaymentUtility, PaymentUtility>();
                services.AddScoped<CacheLogWriter>();
                services.AddScoped(typeof(ICacheLog<>), typeof(CacheLog<>));
                if (Configuration.GetValue<bool>("SwitchToDynamoDB"))
                {
                    services.AddTransient<ILegalDocumentsForTitlesService, LegalDocumentForTitleServiceDynamoDB>();
                }
                services.AddSingleton<IFeatureSettings, FeatureSettings>();
                services.AddSingleton<IAuroraMySqlService, AuroraMySqlService>();
                services.AddSingleton<IAWSSecretManager, AWSSecretManager>();
                services.AddSingleton<IDataSecurity, DataSecurity>();
                services.AddSingleton<IFeatureToggles, FeatureToggles>();
                services.AddSingleton<ICCEDynamicOffersService, CCEDynamicOffersService>();
                services.AddSingleton<ICCEDynamicOfferDetailsService, CCEDynamicOfferDetailsService>();
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "Unexpected error occurred while starting services");
            }
        }

        public void ConfigureContainer(ContainerBuilder container)
        {
            try
            {
                container.Register(c => new ResilientClient(Configuration.GetSection("cachingConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("cachingConfigKey");
                container.RegisterType<CachingService>().As<ICachingService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("sessionConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("sessionConfigKey");
                container.RegisterType<SessionService>().As<ISessionService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("sessionConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("sessionOnCloudConfigKey");
                container.RegisterType<SessionOnCloudService>().As<ISessionOnCloudService>().WithAttributeFiltering();
                //container.RegisterType<PersistToken>().As<IPersistToken>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("dpTokenConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("dpTokenConfigKey");
                container.RegisterType<DPService>().As<IDPService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("DynamoDBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("DynamoDBClientKey");
                container.RegisterType<DynamoDBService>().As<IDynamoDBService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("LoyaltyUCBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LoyaltyUCBClientKey");
                container.RegisterType<LoyaltyUCBService>().As<ILoyaltyUCBService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CustomerPreferencesClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CustomerPreferencesClientKey");
                container.RegisterType<CustomerPreferencesService>().As<ICustomerPreferencesService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("UtilitiesServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("UtilitiesServiceClientKey");
                container.RegisterType<UtilitiesService>().As<IUtilitiesService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("MPSecurityQuestionsClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MPSecurityQuestionsClientKey");
                container.RegisterType<MPSecurityQuestionsService>().As<IMPSecurityQuestionsService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("ClubMembershipClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ClubMembershipClientKey");
                container.RegisterType<ClubMembershipService>().As<IClubMembershipService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("MPSecurityCheckDetailsClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MPSecurityCheckDetailsClientKey");
                container.RegisterType<MPSecurityCheckDetailsService>().As<IMPSecurityCheckDetailsService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CustomerDataClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CustomerDataClientKey");
                container.RegisterType<CustomerDataService>().As<ICustomerDataService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("MyAccountFutureFlightCreditClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MyAccountFutureFlightCreditClientKey");
                container.RegisterType<MPFutureFlightCredit>().As<IMPFutureFlightCredit>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("LoyaltyPromotionsClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LoyaltyPromotionsClientKey");
                container.RegisterType<LoyaltyPromotionsService>().As<ILoyaltyPromotionsService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("PKDispenserClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PKDispenserClientKey");
                container.RegisterType<PKDispenserService>().As<IPKDispenserService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("MerchandizingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MerchandizingClientKey");
                container.RegisterType<PurchaseMerchandizingService>().As<IPurchaseMerchandizingService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("ShoppingCcePromoClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ShoppingCcePromoClientKey");
                container.RegisterType<ShoppingCcePromoService>().As<IShoppingCcePromoService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CMSContentClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CMSContentClientKey");
                container.RegisterType<CMSContentService>().As<ICMSContentService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("EmployeeIdByMileageplusNumberClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("EmployeeIdByMileageplusNumberClientKey");
                container.RegisterType<EmployeeIdByMileageplusNumber>().As<IEmployeeIdByMileageplusNumber>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("LoyaltyAccountClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LoyaltyAccountClientKey");
                container.RegisterType<LoyaltyAccountService>().As<ILoyaltyAccountService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("LoyaltyWebClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LoyaltyWebClientKey");
                container.RegisterType<LoyaltyWebService>().As<ILoyaltyWebService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("AccountPremierClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("AccountPremierClientKey");
                container.RegisterType<MyAccountPremierService>().As<IMyAccountPremierService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("FlightStatusClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightStatusClientKey");
                container.RegisterType<FlightStatusService>().As<IFlightStatusService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("FLIFOTokenServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FLIFOTokenServiceClientKey");
                container.RegisterType<FLIFOTokenService>().As<IFLIFOTokenService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("sessionConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("sessionConfigKey");
                container.RegisterType<PersistToken>().As<IPersistToken>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("PNRRetrievalClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PNRRetrievalClientKey");
                container.RegisterType<PNRRetrievalService>().As<IPNRRetrievalService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("ReferencedataClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ReferencedataClientKey");
                container.RegisterType<ReferencedataService>().As<IReferencedataService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("DataVaultTokenClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("DataVaultTokenClientKey");
                container.RegisterType<DataVaultService>().As<IDataVaultService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("employeeProfileClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("employeeProfileClientKey");
                container.RegisterType<EmployeeProfileService>().As<IEmployeeProfileService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("BundleOfferServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("BundleOfferServiceClientKey");
                container.RegisterType<BundleOfferService>().As<IBundleOfferService>().WithAttributeFiltering();

                if (!Configuration.GetValue<bool>("SwitchToDynamoDB"))
                {
                    container.Register(c => new ResilientClient(Configuration.GetSection("LegalDocumentsOnPremSqlClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LegalDocumentsOnPremSqlClientKey");
                    container.RegisterType<LegalDocumentsForTitlesService>().As<ILegalDocumentsForTitlesService>().WithAttributeFiltering();
                }
                container.Register(c => new ResilientClient(Configuration.GetSection("ValidateAccountOnPremClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ValidateAccountOnPremClientKey");
                container.RegisterType<ValidateAccountFC>().As<IValidateAccountFC>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("PaymentServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PaymentServiceClientKey");
                container.RegisterType<PaymentService>().As<IPaymentService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CSLStatisticsOnPremSqlClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLStatisticsOnPremSqlClientKey");
                container.RegisterType<CSLStatisticsService>().As<ICSLStatisticsService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("MobileShoppingCartClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MobileShoppingCartClientKey");
                container.RegisterType<MobileShoppingCart>().As<IMobileShoppingCart>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("PlacePassClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PlacePassClientKey");
                container.RegisterType<PlacePassService>().As<IPlacePassService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("ShoppingCartClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ShoppingCartClientKey");
                container.RegisterType<ShoppingCartService>().As<IShoppingCartService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("TravelReadyClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("TravelReadyClientKey");
                container.RegisterType<TravelReadyService>().As<ITravelReadyService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("FlightShoppingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightShoppingClientKey");
                container.RegisterType<LMXInfo>().As<ILMXInfo>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("BaseEmployeeResClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("BaseEmployeeResClientKey");
                container.RegisterType<BaseEmployeeResService>().As<IBaseEmployeeResService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("OptimizelyServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("OptimizelyServiceClientKey");
                container.RegisterType<OptimizelyPersistService>().As<IOptimizelyPersistService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("OnPremSQLServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ValidateHashPinOnPremSqlClientKey");
                container.RegisterType<ValidateHashPinService>().As<IValidateHashPinService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("eResEmployeeProfileClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("eResEmployeeProfileClientKey");
                container.RegisterType<EResEmployeeProfileService>().As<IEResEmployeeProfileService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("EServiceCheckinClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("EServiceCheckinClientKey");
                container.RegisterType<EServiceCheckin>().As<IEServiceCheckin>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CustomerProfileContactpointsURL").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CustomerProfileContactpointsKey");
                container.RegisterType<InsertOrUpdateTravelInfoService>().As<IInsertOrUpdateTravelInfoService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("UnitedClubMembershipV2Client").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("UnitedClubMembershipV2ClientKey");
                container.RegisterType<UnitedClubMembershipV2Service>().As<IUnitedClubMembershipV2Service>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("UnitedClubMembershipService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("UnitedClubMembershipKey");
                container.RegisterType<UnitedClubMembershipService>().As<IUnitedClubMembershipService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("CSLGetProfileTravelerDetailsService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLGetProfileTravelerDetailsServiceKey");
                container.RegisterType<CustomerProfileTravelerService>().As<ICustomerProfileTravelerService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("CSLGetProfileTravelerService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLGetProfileTravelerServiceKey");
                container.RegisterType<CustomerTravelerService>().As<ICustomerTravelerService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("CSLGetProfileCreditCardsService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLGetProfileCreditCardsServiceKey");
                container.RegisterType<CustomerProfileCreditCardsService>().As<ICustomerProfileCreditCardsService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("CSLGetProfileOwnerService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLGetProfileOwnerServiceKey");
                container.RegisterType<CustomerProfileOwnerService>().As<ICustomerProfileOwnerService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("CSLCorporateGetService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLCorporateGetServiceKey");
                container.RegisterType<CustomerCorporateProfileService>().As<ICustomerCorporateProfileService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("FlightShoppingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightShoppingClientKey");
                container.RegisterType<FlightShoppingProductsService>().As<IFlightShoppingProductsService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("SSOTokenClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("SSOTokenClientKey");
                container.RegisterType<SSOTokenKeyService>().As<ISSOTokenKeyService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("dpTokenValidateConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("dpTokenValidateKey");
                container.RegisterType<DPTokenValidationService>().As<IDPTokenValidationService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("ProvisionService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ProvisionServiceKey");
                container.RegisterType<ProvisionService>().As<IProvisionService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CCEDynamicOffersClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CCEDynamicOffersClientKey");
                container.RegisterType<CCEDynamicOffersService>().As<ICCEDynamicOffersService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("CCEDynamicOffersDetailClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CCEDynamicOffersDetailClientKey");
                container.RegisterType<CCEDynamicOfferDetailsService>().As<ICCEDynamicOfferDetailsService>().WithAttributeFiltering();
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "Unexpected error occurred while starting services");
            }

        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApplicationEnricher applicationEnricher, IFeatureSettings featureSettings, IHostApplicationLifetime applicationLifetime)
        {
            try
            {

                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                app.MapWhen(context => string.IsNullOrEmpty(context.Request.Path) || string.Equals("/", context.Request.Path), appBuilder =>
                {
                    appBuilder.Run(async context =>
                    {
                        await context.Response.WriteAsync("Welcome from Member Profile Microservice");
                    });
                });

                applicationEnricher.Add(Constants.ServiceNameText, Program.Namespace);
                applicationEnricher.Add(Constants.EnvironmentText, env.EnvironmentName);

                if (Configuration.GetValue<bool>("Globalization"))
                {
                    var cultureInfo = new CultureInfo("en-US");
                    // cultureInfo.NumberFormat.CurrencySymbol = "€";
                    CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                    CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
                }

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Images")),
                    RequestPath = "/memberprofileservice/Images"
                });

                app.UseDirectoryBrowser(new DirectoryBrowserOptions()
                {
                    FileProvider = new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), "Images")),
                    RequestPath = new PathString("/memberprofileservice/Images")
                });

                app.UseMiddleware<RequestResponseLoggingMiddleware>();
               
                if (Configuration.GetValue<bool>("EnableFeatureSettingsChanges"))
                {
                    applicationLifetime.ApplicationStarted.Register(async () => await OnStart(featureSettings));
                    applicationLifetime.ApplicationStopping.Register(async () => await OnShutDown(featureSettings));
                }
                //app.UseMiddleware<SetCultureMiddleware>();
                app.UseHttpsRedirection();
                
                app.UseRouting();
                app.UseAuthorization();
                
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "Unexpected error occurred while starting services");
            }
        }
        private async Task OnStart(IFeatureSettings featureSettings)
        {
            try
            {
                await featureSettings.LoadFeatureSettings(United.Mobile.Model.Common.ServiceNames.MEMBERPROFILE.ToString());
            }
            catch (Exception) { }

        }
        private async Task OnShutDown(IFeatureSettings featureSettings)
        {
            try
            {
                await featureSettings.DeleteContainerIPAdress(United.Mobile.Model.Common.ServiceNames.MEMBERPROFILE.ToString(), StaticDataLoader._ipAddress);
            }
            catch { }
        }

    }
}
