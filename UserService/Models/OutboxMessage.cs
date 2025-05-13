namespace MQ.UserService.Models;

// ������ ���������� ��������� (Outbox Pattern)
public class OutboxMessage
{
    // ���������� ������������� ��������� (���� � �������)
    public Guid Id { get; set; }

    // ��� ������� (��������: "UserCreated", "UserUpdated", � �.�.)
    public string EventType { get; set; }

    // ��������������� ���������� ��������� (������ � JSON)
    public string Payload { get; set; }

    // ����, �����������, ���� �� ��������� ��� ���������� � �������
    public bool Processed { get; set; }

    // ���� � ����� �������� ���������
    public DateTime CreatedAt { get; set; }
}
