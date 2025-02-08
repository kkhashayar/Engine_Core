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

    [HttpGet("status")]
    public async Task<ActionResult<string>> GetStatus()
    {
        var response = await _engineService.GetStatus();
       
        if (response == null) return NotFound();    

        return Ok(response);
    }


    [HttpGet("newgame")]
    public async Task<ActionResult<string>> GetNewGame()
    {
        var response = await _engineService.GetNewGame();
        
        if (response == null)  return NoContent();
        return Ok(response);
    }

    [HttpGet("stopgame")]
    public async Task<ActionResult<string>> StopGame()
    {
        var response = await _engineService.StopGame();
        
        if (response == null) return NotFound();
        
        
        return Ok(response);
    }

    [HttpPost("sendmove")]
    public async Task<ActionResult<string>> SendMove([FromBody] Move move)
    {
        var response = await _engineService.SendMove(move);
        
        if (response == null)  return NotFound();
       
        return Ok(response);
    }

}
