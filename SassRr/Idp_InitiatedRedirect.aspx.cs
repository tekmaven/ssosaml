using System;
using Thinktecture.IdentityModel.Client;

namespace SassRr
{
    public partial class Idp_InitiatedRedirect : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var client = new OAuth2Client(new Uri("https://samlauth-rr-71k0j0ps.cloudapp.net/identity/connect/authorize"));
            var startUrl = client.CreateAuthorizeUrl(
                clientId: "sassrr",
                responseType: "id_token token",
                scope: "sassweb profile openid",
                redirectUri: "http://localhost:54792/Default.aspx",
                nonce: "random_nonce",
                responseMode: "form_post",
                acrValues: "idp:okta");

            Response.Redirect(startUrl, false);
        }
    }
}