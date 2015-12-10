using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer3.Core;
using IdentityServer3.Core.Extensions;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Newtonsoft.Json;
using Serilog;

namespace WebHost
{
    public class ExternalRegistrationUserService : IUserService
    {
        public class CustomUser
        {
            public string Subject { get; set; }
            public string Provider { get; set; }
            public string ProviderID { get; set; }
            public List<Claim> Claims { get; set; }
        }

        public static List<CustomUser> Users = new List<CustomUser>();

        private static string Pp(object context)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            
            return JsonConvert.SerializeObject(context, settings);
        }

        public Task AuthenticateExternalAsync(ExternalAuthenticationContext context)
        {
            var trace = Pp(context);

            Log.Information("RRRRRR AuthenticateExternalAsync called " + trace);

            var user = Users.SingleOrDefault(x => x.Provider == context.ExternalIdentity.Provider && x.ProviderID == context.ExternalIdentity.ProviderId);
            var name = "Unknown";

            if (user == null)
            {
                user = new CustomUser
                {
                    Subject = Guid.NewGuid().ToString(),
                    Provider = context.ExternalIdentity.Provider,
                    ProviderID = context.ExternalIdentity.ProviderId,
                    Claims = new List<Claim> { new Claim(Constants.ClaimTypes.Name, name) }
                };
                Users.Add(user);

                if (user.Claims.Any())
                    name = user.Claims.First(x => x.Type == Constants.ClaimTypes.Name).Value;
            }

            context.AuthenticateResult = new AuthenticateResult(user.Subject, name, identityProvider: user.Provider);

            return Task.FromResult(0);
        }

        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            Log.Information("RRRRRR GetProfileDataAsync called " + Pp(context));

            // issue the claims for the user
            var user = Users.SingleOrDefault(x => x.Subject == context.Subject.GetSubjectId());
            if (user != null)
            {
                context.IssuedClaims = user.Claims.Where(x => context.RequestedClaimTypes.Contains(x.Type));
            }

            return Task.FromResult(0);
        }

        public Task PreAuthenticateAsync(PreAuthenticationContext context)
        {
            Log.Information("RRRRRR PreAuthenticateAsync called" + Pp(context));
            return Task.FromResult(0);
        }

        public Task AuthenticateLocalAsync(LocalAuthenticationContext context)
        {
            Log.Information("RRRRRR AuthenticateLocalAsync called " + Pp(context));
            return Task.FromResult(0);
        }

        public Task PostAuthenticateAsync(PostAuthenticationContext context)
        {
            Log.Information("RRRRRR PostAuthenticateAsync " + Pp(context));
            return Task.FromResult(0);
        }

        public Task SignOutAsync(SignOutContext context)
        {
            Log.Information("RRRRRR SignOutAsync called " + Pp(context));
            return Task.FromResult(0);
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            Log.Information("RRRRRR IsActiveAsync called " + Pp(context));
            return Task.FromResult(0);
        }
    }
}