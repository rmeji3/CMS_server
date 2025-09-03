using Microsoft.AspNetCore.Mvc;

namespace CMS.Controllers.Base
{
    public abstract class TenantControllerBase : ControllerBase
    {
        protected string? TenantId => HttpContext.Items["TenantId"] as string;
    }
}
