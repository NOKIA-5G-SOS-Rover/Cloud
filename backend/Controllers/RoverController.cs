using backend.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("rover")]
public class RoverController : ControllerBase
{
    [HttpPost("command")]
    public IActionResult SendCommand([FromBody] RoverCommandDto command)
    {
        // Aici va veni ulterior logica de comunicare cu rover-ul fizic 
        // (ex: trimiterea payload-ului prin MQTT, gRPC, sau WebSockets peste 5G)
        
        // Momentan facem doar un log în consolă pentru a valida conexiunea dinspre React
        var activeDirections = command.Directions.Any() ? string.Join(", ", command.Directions) : "None";
        Console.WriteLine($"[ROVER COMMAND] Speed: {command.Speed} | Directions: {activeDirections}");

        return Ok(new 
        { 
            message = "Command received and routed to rover.",
            speed = command.Speed,
            directions = command.Directions
        });
    }
}