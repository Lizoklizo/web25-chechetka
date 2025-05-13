import React, { useState, useEffect } from "react";
import { getNotifications, createNotification, deleteNotification } from "../services/NotificationService"; // Добавляем функцию удаления
import Table from "./Table"; // Переиспользуем Table
import NotificationWrapper from "./Notification"; // Переиспользуем Notification
import CustomProgressBar from "./ProgressBar"; // Переиспользуем прогресс-бар

const NotificationComponent: React.FC = () => {
    const [newNotification, setNewNotification] = useState<{ userId: string; message: string }>({
        userId: "",
        message: "",
    });
    const [showNotification, setShowNotification] = useState<string | null>(null);
    const [countdown, setCountdown] = useState<number>(10); // Таймер отсчета
    const [progress, setProgress] = useState<number>(100); // Прогресс таймера (ширина прогресс-бара)
    const [notifications, setNotifications] = useState<any[]>([]); // Храним уведомления

    // Функция для получения уведомлений
    const fetchNotifications = () => {
        getNotifications()
            .then((response) => {
                if (Array.isArray(response.data)) {
                    setNotifications(response.data); // Обновляем список уведомлений
                } else {
                    setNotifications([]); // Если данные не массив, передаем пустой массив
                }
            })
            .catch((error) => {
                console.error("Error fetching notifications:", error);
                setNotifications([]); // В случае ошибки устанавливаем пустой массив
            });
    };

    // Обработчик создания нового уведомления
    const handleCreateNotification = () => {
        createNotification(newNotification).then(() => {
            setShowNotification("Notification successfully added!");
            setTimeout(() => setShowNotification(null), 3000); // Скрытие уведомления через 3 секунды
            setNewNotification({ userId: "", message: "" }); // Очищаем поля
            fetchNotifications(); // Перезагружаем уведомления после добавления
        });
    };

    // Обработчик удаления уведомления
    const handleDeleteNotification = (id: string) => {
        deleteNotification(id).then(() => {
            setShowNotification("Notification successfully deleted!");
            setTimeout(() => setShowNotification(null), 3000);
            fetchNotifications(); // Перезагружаем уведомления после удаления
        }).catch((error) => {
            setShowNotification("Error deleting notification!");
            setTimeout(() => setShowNotification(null), 3000);
            console.error("Error deleting notification:", error);
        });
    };

    useEffect(() => {
        fetchNotifications(); // Загружаем данные при монтировании компонента

        const interval = setInterval(() => {
            setCountdown((prev) => {
                const newProgress = (prev - 1) * (100 / 10); // Плавное уменьшение прогресса
                setProgress(newProgress <= 0 ? 0 : newProgress); // Обновляем прогресс

                if (prev === 1) {
                    setProgress(100); // Сброс прогресс-бара на 100 после завершения таймера
                    fetchNotifications(); // Обновляем данные
                    return 10; // Сбрасываем таймер
                }

                return prev - 1; // Уменьшаем таймер
            });
        }, 1000);

        return () => clearInterval(interval); // Очистка интервала при размонтировании компонента
    }, []);

    return (
        <div className="container mt-4">
            {/* Уведомление о добавлении нового уведомления */}
            {showNotification && <NotificationWrapper message={showNotification} type="success" />}

            {/* Таблица для уведомлений */}
            {notifications.length === 0 ? (
                <div>No data available</div>
            ) : (
                <Table
                    columns={["Message", "User Id"]}
                    data={notifications}
                    onDelete={handleDeleteNotification} // Передаем функцию удаления
                />
            )}
        </div>
    );
};

export default NotificationComponent;