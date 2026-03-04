using Microsoft.AspNetCore.Mvc;
using IframeFullServer.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace IframeFullServer.Controllers;

public class HomeController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HomeController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }
    public ActionResult Index()
    {
        ViewBag.IsAuthenticated = HttpContext.User.Identity.IsAuthenticated;
        return View();
    }

    public ActionResult About()
    {
        ViewBag.IsAuthenticated = HttpContext.User.Identity.IsAuthenticated;
        ViewBag.Message = "Your application description page.";

        return View();
    }

    public ActionResult Contact()
    {
        ViewBag.IsAuthenticated = HttpContext.User.Identity.IsAuthenticated;
        ViewBag.Message = "Your contact page.";

        return View();
    }

    public ActionResult Login()
    {
        var email = Request.Form["user"].ToString();
        var baseUrl = GetBaseUrl();
        string redirectUrl = $"{baseUrl}/Home/Embed";
        if (email != null)
        {
            var claims = new List<Claim>
                {
                new Claim(ClaimTypes.Name, email)
            };
            var identity = new ClaimsIdentity(claims, "Cookies");
            HttpContext.User = new ClaimsPrincipal(identity);
            HttpContext.SignInAsync("Cookies", HttpContext.User);
        }
        if (string.IsNullOrWhiteSpace(redirectUrl))
            return RedirectToAction("Index");
        else
            return RedirectToAction("Embed");
    }

    public ActionResult JWTLogin()
    {
        if (HttpContext.User.Identity.IsAuthenticated)
        {
            var email = HttpContext.User.Identity.Name;
            var token = new TokenHelper().GenerateJSONWebToken(new User { Email = email });

            /* GET request works till Release v12*/
            // var url = _configuration["jwt:boldbiserverurl"].TrimEnd('/') + "/sso/jwt/callback?jwt=" + token;
            // return Redirect(url);

            /* POST request works from Release v13*/
            var externalPostUrl = _configuration["jwt:boldbiserverurl"].TrimEnd('/') + "/sso/jwt/callback";
            var siteIdentifier = _configuration["jwt:siteidentifier"];
            var redirectTo = _configuration["jwt:redirectto"];
            var model = new JwtPostModel
            {
                Url = externalPostUrl,
                Jwt = token,
                SiteIdentifier = siteIdentifier,
                RedirectTo = redirectTo
            };

            return View("PostToExternalApp", model);
        }
        else
        {
            return RedirectToAction("Loginpage");
        }
    }
    
    public ActionResult Loginpage()
    {
        ViewBag.IsAuthenticated = HttpContext.User.Identity.IsAuthenticated;
        ViewBag.ReturnURL = Request.Query["returnURL"].ToString();
        return View();
    }

    public ActionResult Logout()
    {
        HttpContext.SignOutAsync("Cookies");
        var baseUrl = GetBaseUrl();
        var url = _configuration["jwt:boldbiserverurl"].TrimEnd('/') + $"/oauth/logout?redirect_uri={baseUrl}/Home/Loginpage";
        return Redirect(url);
    }

    public ActionResult JWTLogOut()
    {
        return RedirectToAction("Logout");
    }

    public ActionResult Embed()
    {
        ViewBag.IsAuthenticated = HttpContext.User.Identity.IsAuthenticated;
        return View();
    }

    /// <summary>
    /// Gets the base URL for this application.
    /// Tries configuration first (for explicit local/AKS settings), then falls back to auto-detection.
    /// </summary>
    private string GetBaseUrl()
    {
        // Try to get from configuration first (works for both local and AKS)
        var configuredBaseUrl = _configuration["AppBaseUrl"];
        if (!string.IsNullOrEmpty(configuredBaseUrl))
        {
            return configuredBaseUrl.TrimEnd('/');
        }

        // Fallback: auto-detect from current request
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request != null)
        {
            return $"{request.Scheme}://{request.Host}";
        }

        // Last resort fallback
        return "http://localhost:44383";
    }

}
