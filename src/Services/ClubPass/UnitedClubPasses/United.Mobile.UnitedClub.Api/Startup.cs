using Autofac;
using Autofac.Features.AttributeFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using United.Common.Helper;
using United.Ebs.Logging.Enrichers;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.UnitedClub;
using United.Mobile.Model;
using United.Mobile.UnitedClubPasses.Domain;
using United.Utility.Helper;
using United.Utility.Http;
using United.Utility.Middleware;
using System;
using System.Threading.Tasks;

namespace United.Mobile.UnitedClub.Api
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
            services.AddHttpContextAccessor();
            services.AddTransient<IUnitedClubBusiness, UnitedClubBusiness>();
            services.AddTransient<IPurchaseOTPPassesBusiness, PurchaseOTPPassesBusiness>();
            services.AddScoped<IHeaders, Headers>();
            services.AddScoped<CacheLogWriter>();
            services.AddScoped(typeof(ICacheLog<>), typeof(CacheLog<>));
            services.AddSingleton<IFeatureSettings, FeatureSettings>();
            services.AddSingleton<IAuroraMySqlService, AuroraMySqlService>();
            services.AddSingleton<IAWSSecretManager, AWSSecretManager>();
            services.AddSingleton<IDataSecurity, DataSecurity>();
        }

        public void ConfigureContainer(ContainerBuilder container)
        {
            container.Register(c => new ResilientClient(Configuration.GetSection("dpTokenConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("dpTokenConfigKey");
            container.RegisterType<DPService>().As<IDPService>().WithAttributeFiltering();
            container.RegisterType<PersistToken>().As<IPersistToken>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("cachingConfig").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("cachingConfigKey");
            container.RegisterType<CachingService>().As<ICachingService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("PKDispenserPublicKeyClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PKDispenserPublicKeyClientKey");
            container.RegisterType<PKDispenserPublicKeyServices>().As<IPKDispenserPublicKeyService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("DataVaultTokenClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("DataVaultTokenClientKey");
            container.RegisterType<DataVaultService>().As<IDataVaultService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("LoyaltyUnitedClubIssuePassClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("LoyaltyUnitedClubIssuePassClientKey");
            container.RegisterType<LoyaltyUnitedClubIssuePassService>().As<ILoyaltyUnitedClubIssuePassService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("MasterPassSessionDetailsClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MasterPassSessionDetailsClientKey");
            container.RegisterType<MasterPassSessionDetailsService>().As<IMasterPassSessionDetailsService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("FlightShoppingClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("FlightShoppingClientKey");
            container.RegisterType<PayPalCreditCardService>().As<IPayPalCreditCardService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("PersistentTokenByAccountNumberTokenClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PersistentTokenByAccountNumberTokenClientKey");
            container.RegisterType<PersistentTokenByAccountNumberTokenService>().As<IPersistentTokenByAccountNumberTokenService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("MECSLFullfillmentClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("MECSLFullfillmentClientKey");
            container.RegisterType<MECSLFullfillmentService>().As<IMECSLFullfillmentService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("UnitedClubMembershipV2Client").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("UnitedClubMembershipV2ClientKey");
            container.RegisterType<UnitedClubMembershipV2Service>().As<IUnitedClubMembershipV2Service>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("DynamoDBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("DynamoDBClientKey");
            container.RegisterType<DynamoDBService>().As<IDynamoDBService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("PaymentDBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("PaymentDataAccessClientKey");
            container.RegisterType<PaymentDataAccessService>().As<IPaymentDataAccessService>().WithAttributeFiltering();

            container.Register(c => new ResilientClient(Configuration.GetSection("OnPremSQLDBClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("UnitedClubSQLDBServiceClientKey");
            container.RegisterType<UnitedClubSQLDBService>().As<IUnitedClubSQLDBService>().WithAttributeFiltering();
            container.Register(c => new ResilientClient(Configuration.GetSection("CSLStatisticsOnPremSqlClient").Get<ResilientClientOpitons>())).Keyed<IResilientClient>("CSLStatisticsOnPremSqlClientKey");
            container.RegisterType<CSLStatisticsService>().As<ICSLStatisticsService>().WithAttributeFiltering();
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
                    await context.Response.WriteAsync("Welcome from united club passes Microservice");
                });
            });

            app.UseMiddleware<RequestResponseLoggingMiddleware>();
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            if (Configuration.GetValue<bool>("EnableFeatureSettingsChanges"))
            {
                applicationLifetime.ApplicationStarted.Register(async () => await OnStart(featureSettings));
                applicationLifetime.ApplicationStopping.Register(async () => await OnShutDown(featureSettings));
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
        private async Task OnStart(IFeatureSettings featureSettings)
        {
            try
            {
                await featureSettings.LoadFeatureSettings(United.Mobile.Model.Common.ServiceNames.UNITEDCLUBPASSES.ToString());
            }
            catch (Exception) { }

        }
        private async Task OnShutDown(IFeatureSettings featureSettings)
        {
            try
            {
                await featureSettings.DeleteContainerIPAdress(United.Mobile.Model.Common.ServiceNames.UNITEDCLUBPASSES.ToString(), StaticDataLoader._ipAddress);
            }
            catch { }
        }

    }
}
