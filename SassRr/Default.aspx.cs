using System;
using System.Linq;
using IdentityModel.Client;

namespace SassRr
{
    // https://identityserver.github.io/Documentation/docs/endpoints/userinfo.html
    // https://samlauth-rr-71k0j0ps.cloudapp.net/identity/.well-known/openid-configuration

    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var token = Request["access_token"];
            if (token != null)
            {
                try
                {
                   /* var userInfoClient = new UserInfoClient(new Uri("https://samlauth-rr-71k0j0ps.cloudapp.net/identity/connect/userinfo"), token);

                    var userInfo = userInfoClient.GetAsync().Result;
                    var claims = userInfo.Claims.ToList();

                    claims.ForEach(ui => Response.Write(Server.HtmlEncode(ui.Item1 + " " + ui.Item2)));*/

                }
                catch (Exception er)
                {
                    var htmlEncode = Server.HtmlEncode(er.ToString());

                    Response.Write(htmlEncode);
                }
            }
        }
    }
}