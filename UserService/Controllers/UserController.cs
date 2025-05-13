using Microsoft.AspNetCore.Mvc;
using MQ.UserService.Models;
using MQ.UserService.Services;

namespace MQ.UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser([FromBody] User user)
    {
        var createdUser = await userService.CreateUserAsync(user);
        return Ok(createdUser);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(Guid id)
    {
        var user = await userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();

        return Ok(user);
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
    {
        var users = await userService.GetAllUsersAsync();
        
        return Ok(users);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await userService.RemoveUserByIdAsync(id);
        
        return Ok();
    }
}