using System;
using System.IdentityModel.Metadata;
using Microsoft.Owin;
using Owin;
using Configuration;
using IdentityServer3.Core.Configuration;
using IdentityServer3.Core.Services;
using IdentityServer3.Core.Services.Default;
using Kentor.AuthServices;
using Kentor.AuthServices.Configuration;
using Kentor.AuthServices.Owin;
using Serilog;

[assembly: OwinStartup(typeof(WebHost.Startup))]
namespace WebHost
{
    public class Startup
    {

        public void Configuration(IAppBuilder app)
        {
            Log.Logger =  new LoggerConfiguration()
                //.WriteTo.EventLog("FederatedIdentityServer")
                .WriteTo.File(
                    path: AppDomain.CurrentDomain.BaseDirectory + "\\log.dat",
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] {ClassName}:{MethodName} {Message}{NewLine}{Exception}")
                .Enrich.With(new StackTraceEnricher())
                .MinimumLevel.Verbose()
                .CreateLogger();

            Log.Information("started");

            app.Map("/identity", idsrvApp =>
            {
                var factory = new IdentityServerServiceFactory();
                factory
                    .UseInMemoryClients(Clients.Get())
                    .UseInMemoryScopes(Scopes.Get());

                var userService = new ExternalRegistrationUserService();
                factory.UserService = new Registration<IUserService>(resolver => userService);
                factory.CorsPolicyService = new Registration<ICorsPolicyService>(new DefaultCorsPolicyService { AllowAll = true });

                idsrvApp.UseIdentityServer(new IdentityServerOptions
                {
                    SiteName = "Federated Identity Server 3",
                    SigningCertificate = WebHost.Configuration.Certificate.Load(),
                    Factory = factory,
                    AuthenticationOptions = new AuthenticationOptions
                    {
                        IdentityProviders = WireIdps
                    },

                    EventsOptions = new EventsOptions
                    {
                        RaiseSuccessEvents = true,
                        RaiseErrorEvents = true,
                        RaiseFailureEvents = true,
                        RaiseInformationEvents = true
                    }
                });
            });
        }

        public static void WireIdps(IAppBuilder app, string signInAsType)
        {
            var okta = new KentorAuthServicesAuthenticationOptions(false)
            {
                SPOptions = new SPOptions
                {
                    EntityId = new EntityId("https://samlauth-rr-71k0j0ps.cloudapp.net/identity/AuthServices"), // from (B) above
                    ReturnUrl = new Uri("http://localhost:54792/Idp_InitiatedRedirect.aspx?idp=okta")
                },
                SignInAsAuthenticationType = signInAsType,
                AuthenticationType = "okta", // this is the "idp" - identity provider - that you can refer to throughout identity server
                Caption = "Okta",  // this is the caption for the button or option that a user might see to prompt them for this login option             
            };

            okta.IdentityProviders.Add(new IdentityProvider(new EntityId("http://www.okta.com/exk5gl5fv25zcFaRX0h7"), okta.SPOptions)  // from (F) above
            {
                LoadMetadata = true,
                MetadataUrl = new Uri("https://dev-935143.oktapreview.com/app/exk5gl5fv25zcFaRX0h7/sso/saml/metadata"), // see Metadata note above
                AllowUnsolicitedAuthnResponse = true
            });

            app.UseKentorAuthServicesAuthentication(okta);
        }
    }
}