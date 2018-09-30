using Hyprsoft.IoT.AppUpdates.Web.Areas.AppUpdates.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Hyprsoft.IoT.AppUpdates.Web.Areas.AppUpdates.Controllers
{
    public class AccountController : BaseController
    {
        #region Fields

        private readonly IConfiguration _configuration;

        #endregion

        #region Constructors

        public AccountController(UpdateManager manager, IConfiguration configuration) : base(manager)
        {
            _configuration = configuration;
        }

        #endregion

        #region Methods

        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Login model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var credentialProvider = await new CredentialProviderHelper(_configuration).CreateProviderAsync();
                var username = await credentialProvider.GetUsernameAsync();
                if (String.Compare(model.Username, username, true) == 0 && model.Password == await credentialProvider.GetPasswordAsync())
                {
                    var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
                    var authenticationProperties = new AuthenticationProperties
                    {
                        AllowRefresh = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(AuthenticationHelper.CookieExpirationDays),
                        IsPersistent = false,
                        IssuedUtc = DateTime.UtcNow
                    };
                    await HttpContext.SignInAsync(AuthenticationHelper.CookieAuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationHelper.CookieAuthenticationScheme)), authenticationProperties);
                    if (Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    else
                        return RedirectToAction("List", "Apps", new { Area = "AppUpdates" });
                }
                else
                    TempData["Error"] = "Invalid login attempt.  Please try again.";
            }   // model state valid?
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(AuthenticationHelper.CookieAuthenticationScheme);
            return RedirectToAction("List", "Apps", new { Area = "AppUpdates" });
        }

        #endregion
    }
}
