using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;

namespace Hyprsoft.IoT.AppUpdates.Web.Areas.AppUpdates.Controllers
{
    [Authorize(AuthenticationSchemes = AuthenticationSettings.AuthenticationScheme)]
    [Route("[area]/apps/{applicationId:guid}/packages/[controller]/{filename}")]
    public class DownloadController : BaseController
    {
        #region Fields

        private readonly IHostingEnvironment _hostingEnv;

        #endregion

        #region Constructors

        public DownloadController(UpdateManager manager, IHostingEnvironment env) : base(manager)
        {
            _hostingEnv = env;
        }

        #endregion

        #region Methods

        [HttpGet]
        public IActionResult Download(Guid applicationId, string filename)
        {
            var item = UpdateManager.Applications.FirstOrDefault(a => a.Id == applicationId);
            if (item == null)
                return NotFound();

            var package = item.Packages.FirstOrDefault(p => String.Compare(Path.GetFileName(p.SourceUri.ToString()), filename, true) == 0 && p.IsAvailable);
            if (package == null)
                return NotFound();

            var packageFilename = Path.Combine(Path.Combine(_hostingEnv.ContentRootPath, "packages"), filename);
            if (!System.IO.File.Exists(packageFilename))
                return NotFound();

            return PhysicalFile(packageFilename, "application/zip", filename);
        }

        #endregion
    }
}
