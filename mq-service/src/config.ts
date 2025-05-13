// Конфигурация для базового URL и портов для каждого сервиса
const BASE_URL = "https://localhost";  // Общий адрес для всех сервисов

export const API_URLS = {
    USER_SERVICE: `${BASE_URL}:7049/api/User`,           // Порт для UserService
    ORDER_SERVICE: `${BASE_URL}:7057/api/Order`,         // Порт для OrderService
    PAYMENT_SERVICE: `${BASE_URL}:7092/api/Payment`,     // Порт для PaymentService
    NOTIFICATION_SERVICE: `${BASE_URL}:7031/api/Notification`, // Порт для NotificationService
};