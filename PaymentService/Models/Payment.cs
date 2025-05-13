namespace MQ.PaymentService.Models;

// ������ ������ ��� �������
public class Payment
{
    // ���������� ������������� �������
    public Guid Id { get; set; }

    // ���� � ����� �������� ������ � �������
    public DateTime CreatedAt { get; set; }

    // ������������� ������, � �������� ��������� �����
    public Guid OrderId { get; set; }

    // ����� �������
    public decimal Amount { get; set; }

    // ������ �������
    public string PaymentStatus { get; set; }
}
