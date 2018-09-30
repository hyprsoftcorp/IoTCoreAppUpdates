using Hyprsoft.IoT.AppUpdates.Web.Areas.AppUpdates.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Hyprsoft.IoT.AppUpdates.Web.Areas.AppUpdates.Controllers
{
    [Authorize(AuthenticationSchemes = AuthenticationHelper.CookieAuthenticationScheme)]
    [Route("apps/{applicationId}/[controller]/[action]/{id?}")]
    public class PackagesController : BaseController
    {
        #region Fields

        private readonly IHostingEnvironment _hostingEnv;

        #endregion

        #region Constructors

        public PackagesController(UpdateManager manager, IHostingEnvironment env) : base(manager)
        {
            _hostingEnv = env;
        }

        #endregion

        #region Methods

        public IActionResult Create(Guid applicationId)
        {
            var item = UpdateManager.Applications.FirstOrDefault(a => a.Id == applicationId);
            if (item != null)
            {
                return View(new Package
                {
                    Id = Guid.NewGuid(),
                    Application = item,
                    ReleaseDateUtc = DateTime.UtcNow
                });
            }   // item found?
            return NotFound();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid applicationId, Package model, IFormFile zipfile)
        {
            var item = UpdateManager.Applications.FirstOrDefault(a => a.Id == applicationId);
            if (item == null)
                return NotFound();

            model.Application = item;
            if (ModelState.IsValid && zipfile != null && zipfile.Length > 0)
            {
                var packagesFolder = Path.Combine(_hostingEnv.WebRootPath, "packages");
                var packageFilename = Path.Combine(packagesFolder, zipfile.FileName);
                if (!Directory.Exists(packagesFolder))
                    Directory.CreateDirectory(packagesFolder);
                using (var stream = new FileStream(packageFilename, FileMode.OpenOrCreate))
                    await zipfile.CopyToAsync(stream);

                model.SourceUri = new Uri($"{Request.Scheme}://{Request.Host}/packages/{zipfile.FileName}");

                try
                {
                    ValidatePackage(model, packageFilename);
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Package '{model.FileVersion}' validation failed.  Details: {ex.Message}";
                    return View(model);
                }

                model.Application = item;
                item.Packages.Add(model);
                await UpdateManager.Save();
                TempData["Feedback"] = $"Package '{model.FileVersion}' successfully added to app '{model.Application.Name}'.";
                return RedirectToAction(nameof(AppsController.List), "Apps", $"item-{item.Id}");
            }   // model state valid?
            return View(model);
        }

        public IActionResult Edit(Guid applicationId, Guid id)
        {
            var item = UpdateManager.Applications.SelectMany(a => a.Packages).FirstOrDefault(p => p.Id == id);
            if (item != null)
                return View(item);
            return NotFound();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid applicationId, Package model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var item = UpdateManager.Applications.SelectMany(a => a.Packages).FirstOrDefault(p => p.Id == model.Id);
                    if (item != null)
                    {
                        var app = item.Application;
                        model.Application = app;

                        try
                        {
                            var packageFilename = Path.Combine(Path.Combine(_hostingEnv.WebRootPath, "packages"), Path.GetFileName(model.SourceUri.ToString()));
                            ValidatePackage(model, packageFilename);
                        }
                        catch (Exception ex)
                        {
                            TempData["Error"] = $"Package '{model.FileVersion}' validation failed.  Details: {ex.Message}";
                            return View(model);
                        }

                        app.Packages.Remove(item);
                        app.Packages.Add(model);
                        await UpdateManager.Save();
                        TempData["Feedback"] = $"Successfully updated package '{model.FileVersion}' for app '{model.Application.Name}'.";
                        return RedirectToAction(nameof(AppsController.List), "Apps", $"item-{item.Id}");
                    }   // item null?
                    return NotFound();
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Unable to update package '{model.FileVersion}'.  Details: {ex.Message}";
                }
            }   // valid model state?
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid applicationId, Guid id)
        {
            var item = UpdateManager.Applications.SelectMany(a => a.Packages).FirstOrDefault(p => p.Id == id);
            if (item != null)
            {
                item.Application.Packages.Remove(item);
                await UpdateManager.Save();
                return Ok(new AjaxResponse { Message = $"Package '{item.FileVersion}' was successfully deleted." });
            }
            return NotFound();
        }

        private void ValidatePackage(Package package, string packageFilename)
        {
            if (!System.IO.File.Exists(packageFilename))
                throw new FileNotFoundException($"File '{packageFilename}' not found.");

            var checksum = UpdateManager.CalculateMD5Checksum(new Uri(packageFilename));
            if (package.Checksum != checksum)
                throw new InvalidOperationException($"The file checksums do not match.  Expected '{package.Checksum}' and got '{checksum}'.");

            using (var localZip = ZipFile.OpenRead(packageFilename))
            {
                if (!localZip.Entries.Any(e => e.Name.ToLower() == package.Application.ExeFilename.ToLower()))
                    throw new InvalidOperationException($"Package '{package.FileVersion}' does not contain the required file '{package.Application.ExeFilename}'.");

                var versionFilenameEntry = localZip.Entries.FirstOrDefault(e => e.Name.ToLower() == package.Application.VersionFilename.ToLower());
                if (versionFilenameEntry == null)
                    throw new InvalidOperationException($"Package '{package.FileVersion}' does not contain the required file '{package.Application.VersionFilename}'.");

                // Validate our file version.
                var tempFilename = Path.GetTempFileName();
                versionFilenameEntry.ExtractToFile(tempFilename, true);
                var fileVersion = FileVersionInfo.GetVersionInfo(tempFilename).FileVersion;
                System.IO.File.Delete(tempFilename);
                if (fileVersion != package.FileVersion)
                    throw new InvalidOperationException($"Package '{package.FileVersion}' does not contain the required file '{package.Application.VersionFilename}' with version '{package.FileVersion}'.");
            }   // using zip file
        }

        #endregion
    }
}
