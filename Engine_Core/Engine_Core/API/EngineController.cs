using Microsoft.AspNetCore.Mvc;

namespace Engine_Core.API;

[ApiController]
[System.Web.Mvc.Route("api/[controller]")]
public class EngineController : System.Web.Mvc.ControllerBase, IEngineServices
{

}