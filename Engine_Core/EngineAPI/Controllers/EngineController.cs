using Engine_API.Interfaces;
using Engine_API.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Engine_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EngineController : ControllerBase
{
    private readonly IEngineService _engineService;

    public EngineController(IEngineService engineService)
    {
        _engineService = engineService;
    }

    

}
