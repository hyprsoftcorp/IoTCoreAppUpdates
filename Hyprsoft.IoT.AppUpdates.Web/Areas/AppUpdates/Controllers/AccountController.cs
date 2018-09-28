using Hyprsoft.IoT.AppUpdates.Web.Areas.AppUpdates.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Hyprsoft.IoT.AppUpdates.Web.Areas.AppUpdates.Controllers
{
    public class AccountController : BaseController
    {
        #region Constructors

        public AccountController(UpdateManager manager) : base(manager) { }

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
                if (String.Compare(model.Username, AuthenticationHelper.DefaultUsername, true) == 0 && model.Password == AuthenticationHelper.DefaultPassword)
                {
                    var claims = new List<Claim> { new Claim(ClaimTypes.Name, AuthenticationHelper.DefaultUsername) };
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
