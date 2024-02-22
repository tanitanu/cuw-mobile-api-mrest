using Autofac;
using Autofac.Features.AttributeFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using United.Common.Helper;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Common.HelperSeatEngine;
using United.Ebs.Logging.Enrichers;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.FlightReservation;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.MerchandizeService;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.SeatEngine;
using United.Mobile.DataAccess.ShopBundles;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.DataAccess.ShopProducts;
using United.Mobile.DataAccess.ShopSeats;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.DataAccess.Travelers;
using United.Mobile.Model;
using United.Mobile.Services.ShopBundles.Domain;
using United.Utility.Helper;
using United.Utility.Http;
using United.Utility.Middleware;
using System;
using System.Threading.Tasks;

namespace United.Mobile.Services.ShopBundles.Api
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
            services.AddControllers();
            services.AddScoped<IHeaders, Headers>();
            services.AddScoped<ISessionHelperService, SessionHelperService>();
            services.AddScoped<IShopBundlesBusiness, ShopBundlesBusiness>();
            services.AddScoped<IShoppingUtility, ShoppingUtility>();
            services.AddTransient<IOmniCart, OmniCart>();
            services.AddTransient<ISeatMapCSL30, SeatMapCSL30>();
            services.AddTransient<ITravelerUtility, TravelerUtility>();
            services.AddTransient<IShoppingBuyMiles, ShoppingBuyMiles>();
            services.AddTransient<IFFCShoppingcs, FFCShopping>();
            services.AddTransient<ISeatEngine, SeatEngine>();
            services.AddTransient<ISeatEnginePostService, SeatEnginePostService>();
            services.AddTransient<IPNRServiceEResService, PNRServiceEResService>();
            services.AddTransient<IProductInfoHelper, ProductInfoHelper>();
            services.AddScoped<IShoppingSessionHelper, ShoppingSessionHelper>();
            services.AddScoped<IDataPowerFactory, DataPowerFactory>();
            services.AddTransient<IMerchandizingServices, United.Common.Helper.Merchandize.MerchandizingServices>();
            services.AddTransient<IMerchOffersService, MerchOffersService>();
            services.AddTransient<IBaggageInfo, InitialBaggageInfo>();
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

            container.Register(c => new ResilientClient(Configuration.GetSection("FlightShoppingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightShoppingClientKey");
            container.RegisterType<UnfinishedBookingService>().As<IUnfinishedBookingService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("FlightShoppingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightShoppingClientKey");
            container.RegisterType<FlightShoppingService>().As<IFlightShoppingService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("DynamoDBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("DynamoDBClientKey");
            container.RegisterType<DynamoDBService>().As<IDynamoDBService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("BundleOfferServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("BundleOfferServiceClientKey");
            container.RegisterType<BundleOfferService>().As<IBundleOfferService>().WithAttributeFiltering();

            if (!Configuration.GetValue<bool>("SwitchToDynamoDB"))
            {
                container.Register(c => new ResilientClient(Configuration.GetSection("LegalDocumentsOnPremSqlClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LegalDocumentsOnPremSqlClientKey");
                container.RegisterType<LegalDocumentsForTitlesService>().As<ILegalDocumentsForTitlesService>().WithAttributeFiltering();
            }

            container.Register(c => new ResilientClient(Configuration.GetSection("OnPremSQLServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ValidateHashPinOnPremSqlClientKey");
            container.RegisterType<ValidateHashPinService>().As<IValidateHashPinService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("OptimizelyServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("OptimizelyServiceClientKey");
            container.RegisterType<OptimizelyPersistService>().As<IOptimizelyPersistService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("CMSContentClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CMSContentClientKey");
            container.RegisterType<CMSContentService>().As<ICMSContentService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("SeatMapCSL30Client").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("SeatMapCSL30ClientKey");
            container.RegisterType<SeatMapCSL30Service>().As<ISeatMapCSL30Service>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("SeatMapClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("SeatMapClientKey");
            container.RegisterType<SeatMapService>().As<ISeatMapService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("SeatEngineClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("SeatEngineClientKey");
            container.RegisterType<SeatEngineService>().As<ISeatEngineService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("PNRRetrievalClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PNRRetrievalClientKey");
            container.RegisterType<PNRRetrievalService>().As<IPNRRetrievalService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("SQLDBComplimentaryUpgradeClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("SQLDBComplimentaryUpgradeClientKey");
            container.RegisterType<ComplimentaryUpgradeService>().As<IComplimentaryUpgradeService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("MerchandizingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MerchandizingClientKey");
            container.RegisterType<PurchaseMerchandizingService>().As<IPurchaseMerchandizingService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("ShoppingCcePromoClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ShoppingCcePromoClientKey");
            container.RegisterType<ShoppingCcePromoService>().As<IShoppingCcePromoService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("PKDispenserClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PKDispenserClientKey");
            container.RegisterType<PKDispenserService>().As<IPKDispenserService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("ShoppingCartClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ShoppingCartClientKey");
            container.RegisterType<ShoppingCartService>().As<IShoppingCartService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("FlightShoppingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightShoppingClientKey");
            container.RegisterType<FlightShoppingProductsService>().As<IFlightShoppingProductsService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("dpTokenValidateConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("dpTokenValidateKey");
            container.RegisterType<DPTokenValidationService>().As<IDPTokenValidationService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("CCEDynamicOffersDetailClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CCEDynamicOffersDetailClientKey");
            container.RegisterType<CCEDynamicOfferDetailsService>().As<ICCEDynamicOfferDetailsService>().WithAttributeFiltering();

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
                appBuilder.Run(async context => {
                    await context.Response.WriteAsync("Welcome from ShopBundles Microservice");
                });
            });

            app.UseMiddleware<RequestResponseLoggingMiddleware>();
            //app.UseSerilogRequestLogging(opts
            // => opts.EnrichDiagnosticContext = (diagnosticsContext, httpContext) => {
            //     var request = httpContext.Request;
            //     diagnosticsContext.Set("gzip", request.Headers["Content-Encoding"]);
            //     System.Net.ServicePointManager.ServerCertificateValidationCallback = (o, certificate, arg3, arg4) => { return true; };
            // });
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
                await featureSettings.LoadFeatureSettings(United.Mobile.Model.Common.ServiceNames.SHOPBUNDLES.ToString());
            }
            catch (Exception) { }

        }
        private async Task OnShutDown(IFeatureSettings featureSettings)
        {
            try
            {
                await featureSettings.DeleteContainerIPAdress(United.Mobile.Model.Common.ServiceNames.SHOPBUNDLES.ToString(), StaticDataLoader._ipAddress);
            }
            catch { }
        }
    }
}
