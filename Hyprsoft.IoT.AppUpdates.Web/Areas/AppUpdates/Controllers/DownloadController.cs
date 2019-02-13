using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Hyprsoft.IoT.AppUpdates.Web.Areas.AppUpdates.Controllers
{
    [Authorize(AuthenticationSchemes = AuthenticationSettings.AuthenticationScheme)]
    [Route("[area]/apps/{applicationId}/packages/[controller]/{filename}")]
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
        public IActionResult Download(string filename)
        {
            return PhysicalFile(Path.Combine(Path.Combine(_hostingEnv.ContentRootPath, "packages"), filename), "application/zip", filename);
        }

        #endregion
    }
}
