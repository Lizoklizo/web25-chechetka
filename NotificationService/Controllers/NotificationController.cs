using Microsoft.AspNetCore.Mvc;
using MQ.NotificationService.Models;
using MQ.NotificationService.Services;

namespace MQ.NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController(INotificationService notificationService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Notification>> SendNotification([FromBody] Notification notification)
    {
        var sentNotification = await notificationService.SendNotificationAsync(notification);
        return Ok(sentNotification);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Notification>> GetNotification(Guid id)
    {
        var notification = await notificationService.GetNotificationByIdAsync(id);
        if (notification == null)
            return NotFound();

        return Ok(notification);
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Notification>>> GetAllNotifications()
    {
        var notifications = await notificationService.GetAllNotificationsAsync();

        return Ok(notifications);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        await notificationService.RemoveNotificatonByIdAsync(id);
        
        return Ok();
    }
}