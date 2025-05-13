namespace MQ.PaymentService.Events;

public class OrderCreatedEvent
{
    public Guid Id { get; set; }
    public decimal TotalAmount { get; set; }
}