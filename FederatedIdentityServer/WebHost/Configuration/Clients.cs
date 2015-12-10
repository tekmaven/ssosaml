using System.Collections.Generic;
using IdentityServer3.Core;
using IdentityServer3.Core.Models;

namespace Configuration
{
    public class Clients
    {
        public static List<Client> Get()
        {
            return new List<Client>
            {
                new Client
                {
                    ClientName = "sassrr",
                    ClientId = "sassrr",
                    Enabled = true,
                    AccessTokenType = AccessTokenType.Jwt,

                    Flow = Flows.Implicit,
                    RequireConsent = false,


                    RedirectUris = new List<string>
                    {
                        "http://localhost:54792/Default.aspx"
                    },

                    ClientSecrets = new List<Secret>
                    {
                        new Secret("F621F470-9731-4A25-80EF-67A6F7C5F4B8".Sha256())
                    },


                    AllowedScopes = new List<string>
                    {
                        Constants.StandardScopes.OpenId,
                        Constants.StandardScopes.Profile,
                        Constants.StandardScopes.Email,
                        Constants.StandardScopes.Roles,
                        Constants.StandardScopes.OfflineAccess,

                        "sassweb"
                    }
                }
            };
        }
    }
}