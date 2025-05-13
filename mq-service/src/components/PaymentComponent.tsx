import React, { useState, useEffect } from "react";
import { getPayments, createPayment, deletePayment } from "../services/PaymentService"; // Добавляем функцию удаления
import Table from "./Table";
import Notification from "./Notification";
import ServerStatusWrapper from "./ServerStatusWrapper";
import CustomProgressBar from "./ProgressBar";
import { Payment } from "../types/Payment";

const PaymentComponent: React.FC = () => {
    const [newPayment, setNewPayment] = useState<Payment>({ orderId: "", amount: 0 });
    const [showNotification, setShowNotification] = useState<string | null>(null);
    const [progress, setProgress] = useState<number>(100); // Прогресс таймера
    const [refreshTrigger, setRefreshTrigger] = useState<boolean>(false); // Триггер для обновления данных

    // Обработчик создания нового платежа
    const handleCreatePayment = () => {
        createPayment(newPayment).then(() => {
            setShowNotification("Payment successfully added!");
            setTimeout(() => setShowNotification(null), 3000); // Скрытие уведомления через 3 секунды
            setNewPayment({ orderId: "", amount: 0 }); // Очищаем поля
            setRefreshTrigger(prev => !prev); // Обновление данных после добавления платежа
        });
    };

    // Обработчик удаления платежа
    const handleDeletePayment = (id: string) => {
        deletePayment(id).then(() => {
            setShowNotification("Payment successfully deleted!");
            setTimeout(() => setShowNotification(null), 3000);
            setRefreshTrigger(prev => !prev); // Перезапуск загрузки данных после удаления платежа
        }).catch((error) => {
            setShowNotification("Error deleting payment!");
            setTimeout(() => setShowNotification(null), 3000);
            console.error("Error deleting payment:", error);
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
            {/* Уведомление о добавлении нового платежа */}
            {showNotification && <Notification message={showNotification} type="success" />}

            {/* Используем ServerStatusWrapper для запросов */}
            <ServerStatusWrapper apiCall={getPayments} refreshTrigger={refreshTrigger}>
                {(payments: Payment[]) => (
                    <>

                        <Table
                            columns={["Order Id", "Amount"]}
                            data={payments}
                            onDelete={handleDeletePayment} // Передаем функцию удаления
                        />
                    </>
                )}
            </ServerStatusWrapper>
        </div>
    );
};

export default PaymentComponent;