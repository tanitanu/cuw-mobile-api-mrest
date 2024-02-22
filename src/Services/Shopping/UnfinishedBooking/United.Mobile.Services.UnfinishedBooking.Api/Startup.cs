using Autofac;
using Autofac.Features.AttributeFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using Serilog;
using System;
using System.Globalization;
using System.Text.Json.Serialization;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Ebs.Logging.Enrichers;
using United.Foundations.Practices.Framework.Security.DataPower;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.Loyalty;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.MerchandizeService;
using United.Mobile.DataAccess.MPAuthentication;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.ShopBundles;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.DataAccess.ShopProducts;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.DataAccess.Travelers;
using United.Mobile.DataAccess.UnfinishedBooking;
using United.Mobile.Model;
using United.Mobile.Services.UnfinishedBooking.Domain;
using United.Utility.Helper;
using United.Utility.Http;
using United.Utility.Middleware;
using System;
using System.Threading.Tasks;

namespace United.Mobile.Services.UnfinishedBooking.Api
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
            try
            {
                services.AddControllers();
                services.AddControllers().AddJsonOptions(opts => opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
                services.AddControllers().AddNewtonsoftJson(option =>
                {
                    option.SerializerSettings.Converters.Add(new StringEnumConverter());
                });
                services.AddScoped<IHeaders, Headers>();
                services.AddTransient<IUnfinishedBookingBusiness, UnfinishedBookingBusiness>();
                services.AddScoped<ISessionHelperService, SessionHelperService>();
                services.AddTransient<IShoppingSessionHelper, ShoppingSessionHelper>();
                services.AddTransient<IDataPowerFactory, DataPowerFactory>();
                services.AddTransient<IShoppingUtility, ShoppingUtility>();
                services.AddTransient<IUnfinishedBooking, United.Common.Helper.Shopping.UnfinishedBooking>();
                services.AddTransient<IOmniCart, OmniCart>();
                services.AddTransient<IFFCShoppingcs, FFCShopping>();
                services.AddTransient<IFormsOfPayment, FormsOfPayment>();
                services.AddTransient<ITravelerCSL, TravelerCSL>();
                services.AddScoped<IShoppingBuyMiles, ShoppingBuyMiles>();
                services.AddTransient<ITravelerUtility, TravelerUtility>();
                services.AddTransient<ICustomerProfile, CustomerProfile>();
                services.AddTransient<IProductInfoHelper, ProductInfoHelper>();
                services.AddTransient<IProfileCreditCard, ProfileCreditCard>();
                services.AddTransient<IRegisterCFOP, RegisterCFOP>();
                services.AddTransient<IMPTraveler, MPTraveler>();
                services.AddTransient<ICorporateProfile, CorporateProfile>();
                services.AddTransient<IPaymentUtility, PaymentUtility>();
                services.AddTransient<IELFRitMetaShopMessages, Common.Helper.FOP.ELFRitMetaShopMessages>();
                services.AddTransient<ITraveler, Traveler>();
                services.AddTransient<IProfileService, ProfileService>();
                services.AddTransient<IMileagePlusTFACSL, MileagePlusTFACSL>();
                services.AddTransient<IShoppingCartService, ShoppingCartService>();
                services.AddTransient<IPaymentService, PaymentService>();
                services.AddTransient<IReferencedataService, ReferencedataService>();
                services.AddTransient<IDataVaultService, DataVaultService>();
                services.AddTransient<IPKDispenserService, PKDispenserService>();
                services.AddTransient<IDPService, DPService>();
                services.AddTransient<IProductOffers, ProductOffers>();
                services.AddTransient<ICachingService, CachingService>();
                services.AddTransient<IDynamoDBService, DynamoDBService>();
                services.AddTransient<IProductInfoHelper, ProductInfoHelper>();
                services.AddTransient<ILegalDocumentsForTitlesService, LegalDocumentsForTitlesService>();
                services.AddTransient<IShopBundleService, ShopBundleService>();
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
                container.Register(c => new ResilientClient(Configuration.GetSection("MPSecurityQuestionsClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MPSecurityQuestionsClientKey");
                container.RegisterType<MPSecurityQuestionsService>().As<IMPSecurityQuestionsService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("PaymentServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PaymentServiceClientKey");
                container.RegisterType<PaymentService>().As<IPaymentService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("LoyaltyUCBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LoyaltyUCBClientKey");
                container.RegisterType<LoyaltyUCBService>().As<ILoyaltyUCBService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CustomerProfileContactpointsURL").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CustomerProfileContactpointsKey");
                container.RegisterType<InsertOrUpdateTravelInfoService>().As<IInsertOrUpdateTravelInfoService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("cachingConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("cachingConfigKey");
                container.RegisterType<CachingService>().As<ICachingService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("dpTokenConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("dpTokenConfigKey");
                container.RegisterType<DPService>().As<IDPService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("sessionConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("sessionOnCloudConfigKey");
                container.RegisterType<SessionOnCloudService>().As<ISessionOnCloudService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("DynamoDBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("DynamoDBClientKey");
                container.RegisterType<DynamoDBService>().As<IDynamoDBService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("BundleOfferServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("BundleOfferServiceClientKey");
                container.RegisterType<BundleOfferService>().As<IBundleOfferService>().WithAttributeFiltering();

                if (!Configuration.GetValue<bool>("SwitchToDynamoDB"))
                {
                    container.Register(c => new ResilientClient(Configuration.GetSection("LegalDocumentsOnPremSqlClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LegalDocumentsOnPremSqlClientKey");
                    container.RegisterType<LegalDocumentsForTitlesService>().As<ILegalDocumentsForTitlesService>().WithAttributeFiltering();
                }
                container.Register(c => new ResilientClient(Configuration.GetSection("FlightShoppingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightShoppingClientKey");
                container.RegisterType<FlightShoppingService>().As<IFlightShoppingService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CustomerPreferencesClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CustomerPreferencesClientKey");
                container.RegisterType<CustomerPreferencesService>().As<ICustomerPreferencesService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("ShoppingCartClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ShoppingCartClientKey");
                container.RegisterType<ShoppingCartService>().As<IShoppingCartService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CMSContentClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CMSContentClientKey");
                container.RegisterType<CMSContentService>().As<ICMSContentService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("ReferencedataClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ReferencedataClientKey");
                container.RegisterType<ReferencedataService>().As<IReferencedataService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("MerchandizingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MerchandizingClientKey");
                container.RegisterType<PurchaseMerchandizingService>().As<IPurchaseMerchandizingService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("PNRRetrievalClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PNRRetrievalClientKey");
                container.RegisterType<PNRRetrievalService>().As<IPNRRetrievalService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("PKDispenserClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PKDispenserClientKey");
                container.RegisterType<PKDispenserService>().As<IPKDispenserService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("OmniChannelCartServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("OmniChannelCartServiceClientKey");
                container.RegisterType<OmniChannelCartService>().As<IOmniChannelCartService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("OnPremSQLServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("OnPremSQLServiceClientKey");
                container.RegisterType<GMTConversionService>().As<IGMTConversionService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("OnPremSQLServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ValidateHashPinOnPremSqlClientKey");
                container.RegisterType<ValidateHashPinService>().As<IValidateHashPinService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("OnPremSQLServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ValidateHashPinOnPremSqlClientKey");
                container.RegisterType<MileagePlusCSSTokenService>().As<IMileagePlusCSSTokenService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("OptimizelyServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("OptimizelyServiceClientKey");
                container.RegisterType<OptimizelyPersistService>().As<IOptimizelyPersistService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("TravelReadyClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("TravelReadyClientKey");
                container.RegisterType<TravelReadyService>().As<ITravelReadyService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CustomerDataClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CustomerDataClientKey");
                container.RegisterType<CustomerDataService>().As<ICustomerDataService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("PlacePassClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PlacePassClientKey");
                container.RegisterType<PlacePassService>().As<IPlacePassService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("UtilitiesServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("UtilitiesServiceClientKey");
                container.RegisterType<UtilitiesService>().As<IUtilitiesService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("DataVaultTokenClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("DataVaultTokenClientKey");
                container.RegisterType<DataVaultService>().As<IDataVaultService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("EServiceCheckinClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("EServiceCheckinClientKey");
                container.RegisterType<EServiceCheckin>().As<IEServiceCheckin>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("BaseEmployeeResClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("BaseEmployeeResClientKey");
                container.RegisterType<BaseEmployeeResService>().As<IBaseEmployeeResService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("MPSecurityCheckDetailsClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MPSecurityCheckDetailsClientKey");
                container.RegisterType<MPSecurityCheckDetailsService>().As<IMPSecurityCheckDetailsService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("ShoppingCcePromoClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ShoppingCcePromoClientKey");
                container.RegisterType<ShoppingCcePromoService>().As<IShoppingCcePromoService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("MobileShoppingCartClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MobileShoppingCartClientKey");
                container.RegisterType<MobileShoppingCart>().As<IMobileShoppingCart>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("FlightShoppingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightShoppingClientKey");
                container.RegisterType<LMXInfo>().As<ILMXInfo>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("FlightShoppingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightShoppingClientKey");
                container.RegisterType<FlightShoppingProductsService>().As<IFlightShoppingProductsService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CSLGetProfileCreditCardsService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLGetProfileCreditCardsServiceKey");
                container.RegisterType<CustomerProfileCreditCardsService>().As<ICustomerProfileCreditCardsService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CSLCorporateGetService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLCorporateGetServiceKey");
                container.RegisterType<CustomerCorporateProfileService>().As<ICustomerCorporateProfileService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CSLGetProfileTravelerDetailsService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLGetProfileTravelerDetailsServiceKey");
                container.RegisterType<CustomerProfileTravelerService>().As<ICustomerProfileTravelerService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CSLGetProfileOwnerService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLGetProfileOwnerServiceKey");
                container.RegisterType<CustomerProfileOwnerService>().As<ICustomerProfileOwnerService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CSLGetProfileTravelerService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLGetProfileTravelerServiceKey");
                container.RegisterType<CustomerTravelerService>().As<ICustomerTravelerService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("dpTokenValidateConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("dpTokenValidateKey");
                container.RegisterType<DPTokenValidationService>().As<IDPTokenValidationService>().WithAttributeFiltering();

                container.Register(c => new ResilientClient(Configuration.GetSection("CCEDynamicOffersDetailClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CCEDynamicOffersDetailClientKey");
                container.RegisterType<CCEDynamicOfferDetailsService>().As<ICCEDynamicOfferDetailsService>().WithAttributeFiltering();
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "Unexpected error occurred while starting services");
            }

        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IApplicationEnricher applicationEnricher, IFeatureSettings featureSettings, IHostApplicationLifetime applicationLifetime)
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
                        await context.Response.WriteAsync("Welcome from UnfinishedBooking Microservice");
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

                app.UseMiddleware<RequestResponseLoggingMiddleware>();
                //app.UseSerilogRequestLogging(opts
                //=> opts.EnrichDiagnosticContext = (diagnosticsContext, httpContext) =>
                //{
                //    var request = httpContext.Request;
                //    diagnosticsContext.Set("gzip", request.Headers["Content-Encoding"]);
                //    System.Net.ServicePointManager.ServerCertificateValidationCallback = (o, certificate, arg3, arg4) => { return true; };
                //});
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
                await featureSettings.LoadFeatureSettings(United.Mobile.Model.Common.ServiceNames.UNFINISHEDBOOKING.ToString());
            }
            catch (Exception) { }

        }
        private async Task OnShutDown(IFeatureSettings featureSettings)
        {
            try
            {
                await featureSettings.DeleteContainerIPAdress(United.Mobile.Model.Common.ServiceNames.UNFINISHEDBOOKING.ToString(), StaticDataLoader._ipAddress);
            }
            catch { }
        }
    }
}
