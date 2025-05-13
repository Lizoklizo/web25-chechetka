using Microsoft.AspNetCore.Mvc;
using MQ.PaymentService.Models;
using MQ.PaymentService.Services;

namespace MQ.PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController(IPaymentService paymentService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment([FromBody] Payment payment)
    {
        var processedPayment = await paymentService.ProcessPaymentAsync(payment);
        return Ok(processedPayment);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Payment>> GetPayment(Guid id)
    {
        var payment = await paymentService.GetPaymentByIdAsync(id);
        if (payment == null)
            return NotFound();

        return Ok(payment);
    }
    
    [HttpGet]
    public async Task<ActionResult<Payment>> GetAllPayments()
    {
        var payments = await paymentService.GetAllPaymentsAsync();

        return Ok(payments);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePayment(Guid id)
    {
        await paymentService.RemovePaymentByIdAsync(id);
        
        return Ok();
    }
}