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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.FlightStatus;
using United.Common.Helper.FOP;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
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
using United.Mobile.DataAccess.TripPlanGetService;
using United.Mobile.DataAccess.UnitedClub;
using United.Mobile.Model;
using United.Mobile.Model.Shopping;
using United.Mobile.Services.Shopping.Domain;
using United.Utility.Helper;
using United.Utility.Http;
using United.Utility.HttpService;
using United.Utility.Middleware;
using FeatureSettings = United.Common.Helper.FeatureSettings;
using MileagePlus = United.Common.Helper.Profile.MileagePlus;

namespace United.Mobile.Services.Shopping.Api
{
    public class Startup
    {
       
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            
        }

        public IConfiguration Configuration { get; }
    


        private const string StaticDataLoaderSection = "dpTokenRequest";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var shoppingOptions = Configuration.GetSection(ShoppingOptions.ConfigNode).Get<ShoppingOptions>();
            services.AddControllers();
            services.AddControllers().AddJsonOptions(opts => opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            services.AddControllers().AddNewtonsoftJson(option =>
            {
                option.SerializerSettings.Converters.Add(new StringEnumConverter());
            });
            services.AddScoped<IHeaders, Headers>();
            services.AddScoped<IShoppingBusiness, ShoppingBusiness>();
            services.AddScoped<IShopProductsBusiness, ShopProductsBusiness>();
            services.AddScoped<ISessionHelperService, SessionHelperService>();
            services.AddScoped<IShoppingUtility, ShoppingUtility>();
            services.AddScoped<IShopBooking, ShopBooking>();
            services.AddTransient<IShoppingSessionHelper, ShoppingSessionHelper>();
            services.AddScoped<IMileagePlus, MileagePlus>();
            services.AddTransient<ITravelerCSL, TravelerCSL>();
            services.AddTransient<IFlightStatusHelper, FlightStatusHelper>();
            services.AddTransient<IETCService, ETCService>(); //SOAP Service call 
            services.AddTransient<IDataPowerFactory, DataPowerFactory>();
            services.AddTransient<IUnfinishedBooking, UnfinishedBooking>();
            services.AddTransient<IFormsOfPayment, FormsOfPayment>();
            services.AddTransient<IOmniCart, OmniCart>();
            services.AddTransient<IFFCShoppingcs, FFCShopping>();
            services.AddTransient<IMerchandizingServices, Common.Helper.Merchandize.MerchandizingServices>();
            services.AddTransient<IProductInfoHelper, ProductInfoHelper>();
            services.AddTransient<IBaggageInfo, InitialBaggageInfo>();
            services.AddTransient<IMerchOffersService, MerchOffersService>();
            services.AddTransient<IShoppingBuyMiles, ShoppingBuyMiles>();
            services.AddHttpClient<IShoppingClientService, ShoppingClientService>().AddPolicyHandler(PollyHandler.WaitAndRetry(shoppingOptions.RetryCount, shoppingOptions.SleepTimeInMillSecs))
                                                                                    .AddPolicyHandler(PollyHandler.Timeout(shoppingOptions.TimeOut));
            services.AddTransient<ITripPlannerIDService, TripPlannerIDService>();
            services.AddTransient<IPaymentUtility, PaymentUtility>();
            services.AddTransient<IProductOffers, ProductOffers>();
            services.AddTransient<IProfileCreditCard, ProfileCreditCard>();
            services.Configure<ShoppingOptions>(Configuration.GetSection(ShoppingOptions.ConfigNode));
            services.AddScoped<CacheLogWriter>();
            services.AddScoped(typeof(ICacheLog<>), typeof(CacheLog<>));
            if (Configuration.GetValue<bool>("SwitchToDynamoDB"))
                services.AddTransient<ILegalDocumentsForTitlesService, LegalDocumentForTitleServiceDynamoDB>();
            services.AddSingleton<IFeatureSettings, FeatureSettings>();
            services.AddSingleton<IAuroraMySqlService, AuroraMySqlService>();
            services.AddSingleton<IAWSSecretManager, AWSSecretManager>();
            services.AddSingleton<IDataSecurity, DataSecurity>();

            services.AddTransient<IShopMileagePricingBusiness, ShopMileagePricingBusiness>();
            services.AddTransient<IMileagePricingService, MileagePricingService>();
            services.AddSingleton<IFeatureToggles, FeatureToggles>();
        }

        public void ConfigureContainer(ContainerBuilder container)
        {
            container.Register(c => new ResilientClient(Configuration.GetSection("dpTokenConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("dpTokenConfigKey");
            container.RegisterType<DPService>().As<IDPService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("sessionConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("sessionOnCloudConfigKey");
            container.RegisterType<SessionOnCloudService>().As<ISessionOnCloudService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("sessionConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("sessionConfigKey");
            container.RegisterType<SessionService>().As<ISessionService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("cachingConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("cachingConfigKey");
            container.RegisterType<CachingService>().As<ICachingService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("DynamoDBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("DynamoDBClientKey");
            container.RegisterType<DynamoDBService>().As<IDynamoDBService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("ReferencedataClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ReferencedataClientKey");
            container.RegisterType<ReferencedataService>().As<IReferencedataService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("FlightShoppingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightShoppingClientKey");
            container.RegisterType<FlightShoppingService>().As<IFlightShoppingService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("LoyaltyAccountClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LoyaltyAccountClientKey");
            container.RegisterType<LoyaltyAccountService>().As<ILoyaltyAccountService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("LoyaltyWebClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LoyaltyWebClientKey");
            container.RegisterType<LoyaltyWebService>().As<ILoyaltyWebService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("CustomerDataClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CustomerDataClientKey");
            container.RegisterType<CustomerDataService>().As<ICustomerDataService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("EmployeeIdByMileageplusNumberClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("EmployeeIdByMileageplusNumberClientKey");
            container.RegisterType<EmployeeIdByMileageplusNumber>().As<IEmployeeIdByMileageplusNumber>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("LoyaltyUCBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LoyaltyUCBClientKey");
            container.RegisterType<LoyaltyUCBService>().As<ILoyaltyUCBService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("AccountPremierClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("AccountPremierClientKey");
            container.RegisterType<MyAccountPremierService>().As<IMyAccountPremierService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("MyAccountFutureFlightCreditClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MyAccountFutureFlightCreditClientKey");
            container.RegisterType<MPFutureFlightCredit>().As<IMPFutureFlightCredit>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("CMSContentClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CMSContentClientKey");
            container.RegisterType<CMSContentService>().As<ICMSContentService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("FlightStatusClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightStatusClientKey");
            container.RegisterType<FlightStatusService>().As<IFlightStatusService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("FlightRequestSqlSpOnPremClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightRequestSqlSpOnPremClientKey");
            container.RegisterType<FlightRequestSqlSpOnPremService>().As<IFlightRequestSqlSpOnPremService>().WithAttributeFiltering();

            if (!Configuration.GetValue<bool>("SwitchToDynamoDB"))
            {
                container.Register(c => new ResilientClient(Configuration.GetSection("LegalDocumentsOnPremSqlClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LegalDocumentsOnPremSqlClientKey");
                container.RegisterType<LegalDocumentsForTitlesService>().As<ILegalDocumentsForTitlesService>().WithAttributeFiltering();
            }

            container.Register(c => new ResilientClient(Configuration.GetSection("CSLStatisticsOnPremSqlClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLStatisticsOnPremSqlClientKey");
            container.RegisterType<CSLStatisticsService>().As<ICSLStatisticsService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("FlightShoppingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightShoppingClientKey");
            container.RegisterType<LMXInfo>().As<ILMXInfo>().WithAttributeFiltering();
            
            container.Register(c => new ResilientClient(Configuration.GetSection("FlightShoppingBaseClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightShoppingBaseClientKey");
            container.RegisterType<FlightShoppingBaseClient>().As<IFlightShoppingBaseClient>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("OnPremSQLServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("OnPremSQLServiceClientKey");
            container.RegisterType<GMTConversionService>().As<IGMTConversionService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("PKDispenserClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PKDispenserClientKey");
            container.RegisterType<PKDispenserService>().As<IPKDispenserService>().WithAttributeFiltering(); ;

            container.Register(c => new ResilientClient(Configuration.GetSection("CustomerPreferencesClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CustomerPreferencesClientKey");
            container.RegisterType<CustomerPreferencesService>().As<ICustomerPreferencesService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("ShoppingCartClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ShoppingCartClientKey");
            container.RegisterType<ShoppingCartService>().As<IShoppingCartService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("MerchandizingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MerchandizingClientKey");
            container.RegisterType<PurchaseMerchandizingService>().As<IPurchaseMerchandizingService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("PNRRetrievalClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PNRRetrievalClientKey");
            container.RegisterType<PNRRetrievalService>().As<IPNRRetrievalService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("ShoppingCcePromoClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ShoppingCcePromoClientKey");
            container.RegisterType<ShoppingCcePromoService>().As<IShoppingCcePromoService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("OnPremSQLServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ValidateHashPinOnPremSqlClientKey");
            container.RegisterType<ValidateHashPinService>().As<IValidateHashPinService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("OptimizelyServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("OptimizelyServiceClientKey");
            container.RegisterType<OptimizelyPersistService>().As<IOptimizelyPersistService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("UnitedClubMembershipV2Client").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("UnitedClubMembershipV2ClientKey");
            container.RegisterType<UnitedClubMembershipV2Service>().As<IUnitedClubMembershipV2Service>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("UnitedClubMembershipService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("UnitedClubMembershipKey");
            container.RegisterType<UnitedClubMembershipService>().As<IUnitedClubMembershipService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("TripPlannerIDServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("TripPlannerIDServiceClientKey");
            container.RegisterType<TripPlannerIDService>().As<ITripPlannerIDService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("PaymentServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PaymentServiceClientKey");
            container.RegisterType<PaymentService>().As<IPaymentService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("DataVaultTokenClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("DataVaultTokenClientKey");
            container.RegisterType<DataVaultService>().As<IDataVaultService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("CSLGetProfileCreditCardsService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLGetProfileCreditCardsServiceKey");
            container.RegisterType<CustomerProfileCreditCardsService>().As<ICustomerProfileCreditCardsService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("CSLGetProfileOwnerService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLGetProfileOwnerServiceKey");
            container.RegisterType<CustomerProfileOwnerService>().As<ICustomerProfileOwnerService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("CSLCorporateGetService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLCorporateGetServiceKey");
            container.RegisterType<CustomerCorporateProfileService>().As<ICustomerCorporateProfileService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("dpTokenValidateConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("dpTokenValidateKey");
                container.RegisterType<DPTokenValidationService>().As<IDPTokenValidationService>().WithAttributeFiltering();
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
                    await context.Response.WriteAsync("Welcome from Shopping Microservice");
                });
            });

            if (!Configuration.GetValue<bool>("Globalization"))
            {
                var cultureInfo = new CultureInfo("en-US");
                // cultureInfo.NumberFormat.CurrencySymbol = "";
                CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            }
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "images")),
                RequestPath = "/shoppingservice/images"
            });
            app.UseDirectoryBrowser(new DirectoryBrowserOptions()
            {
                FileProvider = new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), "images")),
                RequestPath = new PathString("/shoppingservice/images")
            });
                   
            app.UseMiddleware<RequestResponseLoggingMiddleware>();
            //app.UseSerilogRequestLogging(opts
            //  => opts.EnrichDiagnosticContext = (diagnosticsContext, httpContext) => {
            //      var request = httpContext.Request;
            //      diagnosticsContext.Set("gzip", request.Headers["Content-Encoding"]);
            //      System.Net.ServicePointManager.ServerCertificateValidationCallback = (o, certificate, arg3, arg4) => { return true; };
            //  });
            if (Configuration.GetValue<bool>("EnableFeatureSettingsChanges"))
            {
                applicationLifetime.ApplicationStarted.Register(async () => await OnStart(featureSettings));
                applicationLifetime.ApplicationStopping.Register(async () => await OnShutDown(featureSettings));
            }
            //app.UseMiddleware<SetCultureMiddleware>();
            InitializeSettings();
           
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            
        }

        private void InitializeSettings()
        {
            LoadStaticData();
        }

        private void LoadStaticData()
        {
            try
            {
                try
                {
                    StaticDataLoader.LoadAirports(Configuration);
                }
                catch (Exception) { }            
                NationalityResidenceMsgs.LoadConfig(Configuration);

            }
            catch (Exception)
            {

            }
        }
        private async Task OnStart(IFeatureSettings featureSettings)
        {
            try
            {
                await featureSettings.LoadFeatureSettings(United.Mobile.Model.Common.ServiceNames.SHOPPING.ToString());
            }
            catch (Exception) { }

        }
        private async Task OnShutDown(IFeatureSettings featureSettings)
        {
            try
            {
                await featureSettings.DeleteContainerIPAdress(United.Mobile.Model.Common.ServiceNames.SHOPPING.ToString(), StaticDataLoader._ipAddress);
            }
            catch { }
        }


    }
}
