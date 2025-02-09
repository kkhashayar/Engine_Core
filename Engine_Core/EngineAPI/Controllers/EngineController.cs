using Engine_API.Enumes;
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


    // GET: Handle CECP commands that fetch information 
    [HttpGet("{command}")]
    public async Task<ActionResult<string>> HandleGetCommands(CECPCommands command)
    {
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
    [HttpPost("{command}")]
    public async Task<ActionResult<string>> HandlePostCommands(CECPCommands command, Move? move)
    {
        string? response = command switch
        {
            CECPCommands.usermove => await _engineService.SendMove(move),
            _ => null
        };

        if (response == null) return BadRequest(); 
        return Ok(response);
    }


}
