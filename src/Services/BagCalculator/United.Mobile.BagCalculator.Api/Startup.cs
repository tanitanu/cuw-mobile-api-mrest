using Autofac;
using Autofac.Features.AttributeFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Globalization;
using System.IO;
using United.Common.Helper;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Ebs.Logging.Enrichers;
using United.Mobile.BagCalculator.Domain;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.FlightStatus;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.MerchandizeService;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.DataAccess.ShopProducts;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.Model;
using United.Utility.Helper;
using United.Utility.Http;
using United.Utility.Middleware;
using System;
using System.Threading.Tasks;
namespace United.Mobile.BagCalculator.Api
{
    public class Startup
    {
        private Microsoft.Extensions.Hosting.IHostingEnvironment CurrentEnvironment { get; set; }

        public Startup(IConfiguration configuration, Microsoft.Extensions.Hosting.IHostingEnvironment env)
        {
            Configuration = configuration;
            CurrentEnvironment = env;

            if (Log.Logger == null)
            {
                Log.Logger = CreateSerilogLogger(configuration);
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddControllers();
                services.AddHttpContextAccessor();           
                services.AddTransient<IBagCalculatorBusiness, BagCalculatorBusiness>();            
                services.AddScoped<IHeaders, Headers>();
                services.AddTransient<ICMSContentHelper, CMSContentHelper>();
                services.AddScoped<ISessionHelperService, SessionHelperService>();
                services.AddScoped<IMerchOffersService, MerchOffersService>();
                services.AddScoped<IShoppingSessionHelper, ShoppingSessionHelper>();
                services.AddScoped<IDataPowerFactory, DataPowerFactory>();
                services.AddTransient<IShoppingUtility, ShoppingUtility>();          
                services.AddTransient<IMileagePlus, MileagePlus>();
                services.AddScoped<IShoppingBuyMiles, ShoppingBuyMiles>();
                services.AddTransient<IFFCShoppingcs, FFCShopping>();
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
                container.Register(c => new ResilientClient(Configuration.GetSection("FlightShoppingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightShoppingClientKey");
                container.RegisterType<FlightShoppingService>().As<IFlightShoppingService>().WithAttributeFiltering();
                container.RegisterType<LMXInfo>().As<ILMXInfo>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("PNRReservationService").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PNRRetrievalClientKey");
                container.RegisterType<PNRRetrievalService>().As<IPNRRetrievalService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("CMSContentClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CMSContentClientKey");
                container.RegisterType<CMSContentService>().As<ICMSContentService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("DynamoDBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("DynamoDBClientKey");
                container.RegisterType<DynamoDBService>().As<IDynamoDBService>().WithAttributeFiltering(); 
                container.Register(c => new ResilientClient(Configuration.GetSection("MerchandizingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MerchandizingClientKey");
                container.RegisterType<PurchaseMerchandizingService>().As<IPurchaseMerchandizingService>().WithAttributeFiltering(); ;
                container.Register(c => new ResilientClient(Configuration.GetSection("CarrierOnPremSqlClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CarrierOnPremSqlClientKey");
                container.RegisterType<AirlineCarrierService>().As<IAirlineCarrierService>().WithAttributeFiltering();
                if (!Configuration.GetValue<bool>("SwitchToDynamoDB"))
                {
                    container.Register(c => new ResilientClient(Configuration.GetSection("LegalDocumentsOnPremSqlClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LegalDocumentsOnPremSqlClientKey");
                    container.RegisterType<LegalDocumentsForTitlesService>().As<ILegalDocumentsForTitlesService>().WithAttributeFiltering();
                }
                container.Register(c => new ResilientClient(Configuration.GetSection("ShoppingCcePromoClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ShoppingCcePromoClientKey");
                container.RegisterType<ShoppingCcePromoService>().As<IShoppingCcePromoService>().WithAttributeFiltering();
               
                container.Register(c => new ResilientClient(Configuration.GetSection("CCEDynamicOffersClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CCEDynamicOffersClientKey");
                container.RegisterType<CCEDynamicOffersService>().As<ICCEDynamicOffersService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("CCEDynamicOffersDetailClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CCEDynamicOffersDetailClientKey");
                container.RegisterType<CCEDynamicOfferDetailsService>().As<ICCEDynamicOfferDetailsService>().WithAttributeFiltering();
                
                container.Register(c => new ResilientClient(Configuration.GetSection("CSLStatisticsOnPremSqlClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLStatisticsOnPremSqlClientKey");
                container.RegisterType<CSLStatisticsService>().As<ICSLStatisticsService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("ShoppingCartClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ShoppingCartClientKey");
                container.RegisterType<ShoppingCartService>().As<IShoppingCartService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("OnPremSQLServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ValidateHashPinOnPremSqlClientKey");
                container.RegisterType<ValidateHashPinService>().As<IValidateHashPinService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("OptimizelyServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("OptimizelyServiceClientKey");
                container.RegisterType<OptimizelyPersistService>().As<IOptimizelyPersistService>().WithAttributeFiltering();
                container.Register(c => new ResilientClient(Configuration.GetSection("dpTokenValidateConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("dpTokenValidateKey");
                container.RegisterType<DPTokenValidationService>().As<IDPTokenValidationService>().WithAttributeFiltering();
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "Unexpected error occurred while starting services");
            }
        }

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
                        await context.Response.WriteAsync("Welcome from Bag Calculator Microservice");
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
                    RequestPath = "/bagcalculatorservice/Images"
                });
                app.UseDirectoryBrowser(new DirectoryBrowserOptions()
                {
                    FileProvider = new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), "Images")),
                    RequestPath = new PathString("/bagcalculatorservice/Images")
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
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "Unexpected error occurred while starting services");
            }
        }

        private Serilog.ILogger CreateSerilogLogger(IConfiguration configuration)
        {
            return new LoggerConfiguration()
                .Enrich.WithProperty("ApplicationContext", "")
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }

        private async Task OnStart(IFeatureSettings featureSettings)
        {
            try
            {
                await featureSettings.LoadFeatureSettings(United.Mobile.Model.Common.ServiceNames.BAGCALCULATOR.ToString());
            }
            catch (Exception) { }

        }
        private async Task OnShutDown(IFeatureSettings featureSettings)
        {
            try
            {
                await featureSettings.DeleteContainerIPAdress(United.Mobile.Model.Common.ServiceNames.BAGCALCULATOR.ToString(), StaticDataLoader._ipAddress);
            }
            catch { }
        }
    }
}