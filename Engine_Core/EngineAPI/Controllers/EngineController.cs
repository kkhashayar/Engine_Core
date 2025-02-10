using Engine_API.Authorization;
using Engine_API.Enumes;
using Engine_API.Interfaces;
using Engine_API.Models;
using Engine_API.Validators;
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


    // GET: Handle CECP commands that fetch information 
    [EngineApiKeyAuthorize("1ea567708d9d43f0acad7abfa8a192e8")]
    [HttpGet]
    public async Task<ActionResult<string>> HandleGetCommands([FromQuery]CECPCommands command)
    {
        if (!CECPValidator.GetCommands.Contains(command)) return BadRequest("Command is not available");
        string? response = command switch
        {
            
            CECPCommands.status => await _engineService.GetStatus(),
            CECPCommands.newGame => await _engineService.GetNewGame(),
            CECPCommands.stop => await _engineService.StopGame(),
            _ => null
        }; 

        if(response == null) return BadRequest(response);   
        return Ok(response);
    }


    // POST: Handle CECP commands that send data like moves.
    [EngineApiKeyAuthorize("1ea567708d9d43f0acad7abfa8a192e8")]
    [HttpPost]
    public async Task<ActionResult<string>> HandlePostCommands([FromQuery]CECPCommands command, Move? move)
    {
        if (!CECPValidator.PostCommands.Contains(command)) return BadRequest("Command is not available");
        string? response = command switch
        {
            CECPCommands.usermove => await _engineService.SendMove(move),
            _ => null
        };

        if (response == null) return BadRequest(); 
        return Ok(response);
    }


}
