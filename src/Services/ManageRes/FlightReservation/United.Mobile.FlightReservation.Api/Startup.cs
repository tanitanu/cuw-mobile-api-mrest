using Autofac;
using Autofac.Features.AttributeFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using United.Common.Helper;
using United.Common.Helper.ManageRes;
using United.Common.Helper.Shopping;
using United.Ebs.Logging.Enrichers;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.FlightReservation;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Common.HelperSeatEngine;

using United.Mobile.FlightReservation.Domain;
using United.Mobile.Model;
using United.Utility.Http;
using United.Utility.Middleware;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.Fitbit;
using United.Common.Helper.Profile;
using United.Mobile.DataAccess.FlightShopping;
using System.Text.Json.Serialization;
using United.Common.Helper.Merchandize;
using United.Common.Helper.EmployeeReservation;
using United.Mobile.DataAccess.ETC;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.MerchandizeService;
using United.Mobile.DataAccess.SeatEngine;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.ShopSeats;
using United.Mobile.DataAccess.ShopProducts;
using United.Mobile.DataAccess.MPAuthentication;
using United.Mobile.DataAccess.Loyalty;
using United.Mobile.DataAccess.MPRewards;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.DataAccess.UnitedClub;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.DynamoDB;

namespace United.Mobile.FlightReservation.Api
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
                //.AddJsonOptions(opts => opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            services.AddHttpContextAccessor();
            services.AddScoped<IHeaders, Headers>();
            services.AddTransient<IFlightReservationBusiness, FlightReservationBusiness>();
            services.AddTransient<ICachingService, CachingService>();
            services.AddTransient<IDPService, DPService>();
            services.AddScoped<ISessionHelperService, SessionHelperService>();
            services.AddTransient<IShoppingSessionHelper, ShoppingSessionHelper>();
            services.AddTransient<IFlightReservation, Common.Helper.ManageRes.FlightReservation>();
            services.AddTransient<IDynamoDBService, DynamoDBService>();
            //services.AddTransient<IRequestReceiptByEmailService, RequestReceiptByEmailService>();
            services.AddTransient<ILegalDocumentsForTitlesService, LegalDocumentsForTitlesService>();
            services.AddTransient<IShoppingUtility, ShoppingUtility>();
            services.AddTransient<IDataPowerFactory, DataPowerFactory>();
            services.AddTransient<ISeatMapCSL30, SeatMapCSL30>();
            services.AddTransient<IFlightSeapMapService, FlightSeapMapService>();
            services.AddTransient<IPNRRetrievalService, PNRRetrievalService>();
            services.AddTransient<IFlightReservationService, FlightReservationService>();
            services.AddTransient<IEmpProfile, EmpProfile>();
            services.AddTransient<ILMXInfo, LMXInfo>(); 
            services.AddHttpContextAccessor();
            //services.AddScoped<IHeaders, Headers>();
            services.AddScoped<ISessionHelperService, SessionHelperService>();
            //services.AddTransient<IShoppingUtility, ShoppingUtility>();
            services.AddTransient<ISeatEngine, SeatEngine>();
            services.AddTransient<IManageReservation, ManageReservation>();
            services.AddTransient<ISeatMapCSL30, SeatMapCSL30>();
            //services.AddTransient<ISeatMapBusiness, SeatMapBusiness>();
            //services.AddTransient<IPaymentUtility, PaymentUtility>();
            services.AddTransient<ISeatEnginePostService, SeatEnginePostService>();
            //services.AddTransient<IRegisterCFOP, RegisterCFOP>();
             //services.AddTransient<IFormsOfPayment, FormsOfPayment>();
            //services.AddTransient<IProductOffers, ProductOffers>();
           services.AddTransient<IFFCShoppingcs, FFCShopping>();
            services.AddTransient<IOmniCart, OmniCart>();
            services.AddTransient<IProductInfoHelper, ProductInfoHelper>();
            services.AddTransient<IShoppingSessionHelper, ShoppingSessionHelper>();
            services.AddTransient<ITravelerCSL, TravelerCSL>();
            services.AddTransient<IFlightReservation, Common.Helper.ManageRes.FlightReservation>();
            services.AddTransient<IEmpProfile, EmpProfile>();
            services.AddTransient<IEmployeeReservations, EmployeeReservations>();
            services.AddTransient<IMileagePlus, MileagePlus>();
            services.AddTransient<IETCService, ETCService>();
            services.AddTransient<IProfileService, ProfileService>();
            services.AddTransient<IFlightReservationService, FlightReservationService>();
            services.AddTransient<IMileagePlusReservationService, MileagePlusReservationService>();
            services.AddTransient<IPNRServiceEResService, PNRServiceEResService>();
            services.AddTransient<IMerchOffersService, MerchOffersService>();
            services.AddTransient<IBaggageInfo, InitialBaggageInfo>();
            services.AddTransient<IMerchandizingServices, Common.Helper.Merchandize.MerchandizingServices>();
            services.AddTransient<IProfileCreditCard, ProfileCreditCard>();
            services.AddTransient<IMPTraveler, MPTraveler>();
            services.AddTransient<IConfigUtility, ConfigUtility>();
            services.AddTransient<IDataPowerFactory, DataPowerFactory>();
            if (Configuration.GetValue<bool>("SwitchToDynamoDB"))
                services.AddTransient<ILegalDocumentsForTitlesService, LegalDocumentForTitleServiceDynamoDB>();

        }

        public void ConfigureContainer(ContainerBuilder container)
        {
            container.Register(c => new ResilientClient(Configuration.GetSection("cachingConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("cachingConfigKey");
            container.RegisterType<CachingService>().As<ICachingService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("dpTokenConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("dpTokenConfigKey");
            container.RegisterType<DPService>().As<IDPService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("SessionOnCloudConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("sessionOnCloudConfigKey");
            container.RegisterType<SessionOnCloudService>().As<ISessionOnCloudService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("DynamoDBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("DynamoDBClientKey");
            container.RegisterType<DynamoDBService>().As<IDynamoDBService>().WithAttributeFiltering();

            if (!Configuration.GetValue<bool>("SwitchToDynamoDB"))
            {
                container.Register(c => new ResilientClient(Configuration.GetSection("LegalDocumentsOnPremSqlClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LegalDocumentsOnPremSqlClientKey");
                container.RegisterType<LegalDocumentsForTitlesService>().As<ILegalDocumentsForTitlesService>().WithAttributeFiltering();
            }

            container.Register(c => new ResilientClient(Configuration.GetSection("SQLDBComplimentaryUpgradeClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("SQLDBComplimentaryUpgradeClientKey");
            container.RegisterType<ComplimentaryUpgradeService>().As<IComplimentaryUpgradeService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("UtilitiesServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("UtilitiesServiceClientKey");
            container.RegisterType<UtilitiesService>().As<IUtilitiesService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("LoyaltyWebClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LoyaltyWebClientKey");
            container.RegisterType<LoyaltyWebService>().As<ILoyaltyWebService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("SeatMapClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("SeatMapClientKey");
            container.RegisterType<SeatMapService>().As<ISeatMapService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("SeatMapCSL30Client").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("SeatMapCSL30ClientKey");
            container.RegisterType<SeatMapCSL30Service>().As<ISeatMapCSL30Service>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("ShoppingCcePromoClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ShoppingCcePromoClientKey");
            container.RegisterType<ShoppingCcePromoService>().As<IShoppingCcePromoService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("MerchandizingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MerchandizingClientKey");
            container.RegisterType<PurchaseMerchandizingService>().As<IPurchaseMerchandizingService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("ShoppingCartClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ShoppingCartClientKey");
            container.RegisterType<ShoppingCartService>().As<IShoppingCartService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("PaymentServiceClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PaymentServiceClientKey");
            container.RegisterType<PaymentService>().As<IPaymentService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("DataVaultTokenClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("DataVaultTokenClientKey");
            container.RegisterType<DataVaultService>().As<IDataVaultService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("PersistentTokenByAccountNumberTokenClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PersistentTokenByAccountNumberTokenClientKey");
            container.RegisterType<PersistentTokenByAccountNumberTokenService>().As<IPersistentTokenByAccountNumberTokenService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("CMSContentClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CMSContentClientKey");
            container.RegisterType<CMSContentService>().As<ICMSContentService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("PKDispenserClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PKDispenserClientKey");
            container.RegisterType<PKDispenserService>().As<IPKDispenserService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("SeatMapAvailabilityClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("SeatMapAvailabilityServiceKey");
            container.RegisterType<SeatMapAvailabilityService>().As<ISeatMapAvailabilityService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("PNRRetrievalClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PNRRetrievalClientKey");
            container.RegisterType<PNRRetrievalService>().As<IPNRRetrievalService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("ReferencedataClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ReferencedataClientKey");
            container.RegisterType<ReferencedataService>().As<IReferencedataService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("employeeProfileClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("employeeProfileClientKey");
            container.RegisterType<EmployeeProfileService>().As<IEmployeeProfileService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("CustomerDataClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CustomerDataClientKey");
            container.RegisterType<CustomerDataService>().As<ICustomerDataService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("CustomerPreferencesClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CustomerPreferencesClientKey");
            container.RegisterType<CustomerPreferencesService>().As<ICustomerPreferencesService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("LoyaltyAccountClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LoyaltyAccountClientKey");
            container.RegisterType<LoyaltyAccountService>().As<ILoyaltyAccountService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("LoyaltyUCBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LoyaltyUCBClientKey");
            container.RegisterType<LoyaltyUCBService>().As<ILoyaltyUCBService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("AccountPremierClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("AccountPremierClientKey");
            container.RegisterType<MyAccountPremierService>().As<IMyAccountPremierService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("EmployeeIdByMileageplusNumberClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("EmployeeIdByMileageplusNumberClientKey");
            container.RegisterType<EmployeeIdByMileageplusNumber>().As<IEmployeeIdByMileageplusNumber>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("MyAccountFutureFlightCreditClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MyAccountFutureFlightCreditClientKey");
            container.RegisterType<MPFutureFlightCredit>().As<IMPFutureFlightCredit>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("ReferencedataClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ReferencedataClientKey");
            container.RegisterType<ReferencedataService>().As<IReferencedataService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("FlightSeapMapClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightSeapMapClientKey");
            container.RegisterType<FlightSeapMapService>().As<IFlightSeapMapService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("FlightShoppingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightShoppingClientKey");
            container.RegisterType<LMXInfo>().As<ILMXInfo>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("ReservationClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("ReservationClientKey");
            container.RegisterType<ReservationService>().As<IReservationService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("FareLockClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FareLockClientKey");
            container.RegisterType<FareLockService>().As<IFareLockService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("SeatEngineClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("SeatEngineClientKey");
            container.RegisterType<SeatEngineService>().As<ISeatEngineService>().WithAttributeFiltering();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApplicationEnricher applicationEnricher)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            applicationEnricher.Add(Constants.ServiceNameText, Program.Namespace);
            applicationEnricher.Add(Constants.EnvironmentText, env.EnvironmentName);

            var cultureInfo = new CultureInfo("en-US");
            // cultureInfo.NumberFormat.CurrencySymbol = "€";
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            app.MapWhen(context => string.IsNullOrEmpty(context.Request.Path) || string.Equals("/", context.Request.Path), appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    await context.Response.WriteAsync("Welcome from FlightReservation Microservice");
                });
            });


            app.UseMiddleware<RequestResponseLoggingMiddleware>();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
