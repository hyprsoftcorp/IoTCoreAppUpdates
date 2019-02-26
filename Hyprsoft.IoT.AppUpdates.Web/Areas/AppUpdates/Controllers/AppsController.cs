using Hyprsoft.IoT.AppUpdates.Web.Areas.AppUpdates.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace Hyprsoft.IoT.AppUpdates.Web.Areas.AppUpdates.Controllers
{
    [Authorize(AuthenticationSchemes = AuthenticationSettings.CookieAuthenticationScheme)]
    public class AppsController : BaseController
    {
        #region Constructors

        public AppsController(UpdateManager manager) : base(manager)
        {
        }

        #endregion

        #region Methods

        [HttpGet]
        public IActionResult List()
        {
            return View(UpdateManager);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Application() { Id = Guid.NewGuid() });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(Application model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.UpdateManager = UpdateManager;
                    UpdateManager.Applications.Add(model);
                    UpdateManager.Save();
                    TempData["Feedback"] = $"Successfully added app '{model.Name}'.";
                    return RedirectToAction(nameof(List));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Unable to add the '{model.Name}' app.  Details: {ex.Message}";
                }
            }   // valid model state?
            return View(model);
        }

        [HttpGet]
        public IActionResult Edit(Guid id)
        {
            var item = UpdateManager.Applications.FirstOrDefault(a => a.Id == id);
            if (item != null)
                return View(item);
            else
                return NotFound();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Edit(Application model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var item = UpdateManager.Applications.FirstOrDefault(a => a.Id == model.Id);
                    UpdateManager.Applications.Remove(item);
                    model.Packages = item.Packages;
                    UpdateManager.Applications.Add(model);
                    UpdateManager.Save();
                    TempData["Feedback"] = $"Successfully updated app '{model.Name}'.";
                    return RedirectToAction(nameof(List));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Unable to update app '{model.Name}'.  Details: {ex.Message}";
                }
            }   // valid model state?
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Delete(Guid id)
        {
            var item = UpdateManager.Applications.FirstOrDefault(a => a.Id == id);
            if (item != null)
            {
                try
                {
                    UpdateManager.Applications.Remove(item);
                    UpdateManager.Save();
                    return Ok(new AjaxResponse { Message = $"App '{item.Name}' was successfully deleted." });
                }
                catch (Exception ex)
                {
                    return Ok(new AjaxResponse { IsError = true, Message = $"Unable to delete app '{item.Name}'.  Details: {ex.Message}" });
                }
            }
            return NotFound();
        }

        #endregion    
    }
}