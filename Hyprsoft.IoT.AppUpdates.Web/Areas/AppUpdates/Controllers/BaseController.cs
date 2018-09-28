using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Hyprsoft.IoT.AppUpdates.Web.Areas.AppUpdates.Controllers
{
    [Area("AppUpdates")]
    public abstract class BaseController : Controller
    {
        #region Constructors

        public BaseController(UpdateManager manager)
        {
            UpdateManager = manager;
        }

        #endregion

        #region Methods

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!UpdateManager.IsLoaded)
                await UpdateManager.Load();
            await base.OnActionExecutionAsync(context, next);
        }

        #endregion

        #region Properties

        protected UpdateManager UpdateManager { get; private set; }
        
        #endregion
    }
}