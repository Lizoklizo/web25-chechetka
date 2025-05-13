namespace MQ.PaymentService.Models;

// ������ ���������� ��������� ��� Outbox Pattern
public class OutboxMessage
{
    // ���������� ������������� ���������
    public Guid Id { get; set; }

    // ��� ������� 
    public string EventType { get; set; }

    // �������� ��������
    public string Payload { get; set; }

    // ����, �����������, ���� �� ��������� ��� ���������� � �������
    public bool Processed { get; set; }

    // ���� � ����� �������� �������
    public DateTime CreatedAt { get; set; }
}
