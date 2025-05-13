namespace MQ.PaymentService.Models;

// Модель данных для платежа
public class Payment
{
    // Уникальный идентификатор платежа
    public Guid Id { get; set; }

    // Дата и время создания записи о платеже
    public DateTime CreatedAt { get; set; }

    // Идентификатор заказа, к которому относится платёж
    public Guid OrderId { get; set; }

    // Сумма платежа
    public decimal Amount { get; set; }

    // Статус платежа
    public string PaymentStatus { get; set; }
}
