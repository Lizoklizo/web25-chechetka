namespace MQ.NotificationService.Models;

// ������ ��� �����������
public class Notification
{
    // ���������� ������������� �����������
    public Guid Id { get; set; }

    // ���� � ����� �������� �����������
    public DateTime CreatedAt { get; set; }

    // ������������� ������������, �������� ���������� �����������
    public Guid UserId { get; set; }

    // ���������, ������� ����� ���������� ������������
    public string Message { get; set; }
}
