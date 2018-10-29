using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebApplication1.ML;
using WebApplication1.TokenStorage;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (Request.IsAuthenticated)
            {
                var userName = ClaimsPrincipal.Current.FindFirst("name").Value;
                var userId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("SignOut");
                }
                var tokenCache = new SessionTokenCache(userId, HttpContext);
                if (!tokenCache.HasData())
                {
                    return RedirectToAction("SignOut");
                }

                ViewBag.UserName = userName;
            }
            return View();
        }

        public void SignIn()
        {
            if (!Request.IsAuthenticated)
            {
                HttpContext.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = "/" },
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
        }

        public void SignOut()
        {
            if (Request.IsAuthenticated)
            {
                var userId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    SessionTokenCache tokenCache = new SessionTokenCache(userId, HttpContext);
                    tokenCache.Clear();
                }
            }
            HttpContext.GetOwinContext().Authentication.SignOut(
                CookieAuthenticationDefaults.AuthenticationType);
            Response.Redirect("/");
        }
        public async Task<string> GetAccessToken()
        {
            string accessToken = null;
            string[] scopes = ConfigurationManager.AppSettings["ida:AppScopes"]
                .Replace(' ', ',').Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var userId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Get the user's token cache
                SessionTokenCache tokenCache = new SessionTokenCache(userId, HttpContext);
                ConfidentialClientApplication cca = new ConfidentialClientApplication(
                    ConfigurationManager.AppSettings["ida:AppId"], 
                    ConfigurationManager.AppSettings["ida:RedirectUri"], 
                    new ClientCredential(ConfigurationManager.AppSettings["ida:AppPassword"]), 
                    tokenCache.GetMsalCacheInstance(), null);
                var accounts = await cca.GetAccountsAsync();
                var firstAccount = accounts.FirstOrDefault();
                AuthenticationResult result = await cca.AcquireTokenSilentAsync(scopes, firstAccount);
                accessToken = result.AccessToken;
            }

            return accessToken;
        }

        public async Task<ActionResult> Inbox()
        {
            var token = await GetAccessToken();
            if (string.IsNullOrEmpty(token))
            {
                return Redirect("/");
            }

            var ana = new Analyze();
            var tess = ana.Analyse();
            var client = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", token);

                        return Task.FromResult(0);
                    }));

            try
            {
                var mailResults = await client.Me.MailFolders.Inbox.Messages.Request()
                    .OrderBy("receivedDateTime DESC")
                    .Select("subject,receivedDateTime,from,body,categories,bodyPreview,flag")
                    .Top(10)
                    .GetAsync();
                if (mailResults.CurrentPage.Select(x => x.Body.Content.Contains("Spara")).Any())
                {
                    var message = new Message
                    {
                        ToRecipients = new List<Recipient>
                        {
                            new Recipient
                            {
                                EmailAddress = new EmailAddress
                                {
                                    Address = "jakob.vesterberg1@gmail.com"
                                }
                            }
                        },
                        Subject = "DRAGONSEVRY WHERE two COOL boy",
                    };
                    var test = client.Me.SendMail(message, true);
                    await test.Request().PostAsync();
                }
                return View(mailResults.CurrentPage);
            }
            catch (ServiceException ex)
            {
                return RedirectToAction("Error", "Home", new { message = "ERROR retrieving messages", debug = ex.Message });
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public ActionResult Error(string message, string debug)
        {
            ViewBag.Message = message;
            ViewBag.Debug = debug;
            return View("Error");
        }
    }
}