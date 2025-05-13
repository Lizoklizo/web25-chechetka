import React, { useState, useEffect } from "react";
import { getOrders, createOrder, deleteOrder } from "../services/OrderService"; // Добавляем функцию удаления
import Table from "./Table";
import Notification from "./Notification";
import ServerStatusWrapper from "./ServerStatusWrapper";
import CustomProgressBar from "./ProgressBar";
import { Order } from "../types/Order";

const OrderComponent: React.FC = () => {
    const [newOrder, setNewOrder] = useState<Order>({ userId: "", product: "", quantity: 0 });
    const [showNotification, setShowNotification] = useState<string | null>(null);
    const [progress, setProgress] = useState<number>(100); // Прогресс таймера
    const [refreshTrigger, setRefreshTrigger] = useState<boolean>(false); // Триггер для обновления данных

    // Обработчик создания нового заказа
    const handleCreateOrder = () => {
        createOrder(newOrder).then(() => {
            setShowNotification("Order successfully added!");
            setTimeout(() => setShowNotification(null), 3000); // Скрытие уведомления через 3 секунды
            setNewOrder({ userId: "", product: "", quantity: 0 }); // Очищаем поля
            setRefreshTrigger(prev => !prev); // Обновление данных после добавления заказа
        });
    };

    // Обработчик удаления заказа
    const handleDeleteOrder = (id: string) => {
        deleteOrder(id).then(() => {
            setShowNotification("Order successfully deleted!");
            setTimeout(() => setShowNotification(null), 3000);
            setRefreshTrigger(prev => !prev); // Перезапуск загрузки данных после удаления заказа
        }).catch((error) => {
            setShowNotification("Error deleting order!");
            setTimeout(() => setShowNotification(null), 3000);
            console.error("Error deleting order:", error);
        });
    };

    // Таймер отсчета
    useEffect(() => {
        const interval = setInterval(() => {
            setProgress(prev => {
                const newProgress = prev - 10; // Плавное уменьшение прогресса
                if (newProgress <= 0) {
                    setRefreshTrigger(prev => !prev); // Обновление данных по истечении таймера
                    return 100; // Сброс прогресс-бара на 100 после завершения таймера
                }
                return newProgress; // Уменьшаем прогресс
            });
        }, 1000);

        return () => clearInterval(interval); // Очистка интервала при размонтировании компонента
    }, []);

    return (
        <div className="container mt-4">
            {/* Уведомление о добавлении нового заказа */}
            {showNotification && <Notification message={showNotification} type="success" />}

            {/* Используем ServerStatusWrapper для запросов */}
            <ServerStatusWrapper apiCall={getOrders} refreshTrigger={refreshTrigger}>
                {(orders: Order[]) => (
                    <>

                        <Table
                            columns={["User Id", "Product", "Quantity"]}
                            data={orders}
                            onDelete={handleDeleteOrder} // Передаем функцию удаления
                        />
                    </>
                )}
            </ServerStatusWrapper>
        </div>
    );
};

export default OrderComponent;