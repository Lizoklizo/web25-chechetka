import React, { useState, useEffect } from "react";
import { getUsers, createUser, deleteUser } from "../services/UserService"; // Добавляем функцию удаления
import Table from "./Table";
import Notification from "./Notification";
import ServerStatusWrapper from "./ServerStatusWrapper";
import CustomProgressBar from "./ProgressBar";

const UserComponent: React.FC = () => {
    const [newUser, setNewUser] = useState<{ name: string; email: string }>({
        name: "",
        email: "",
    });
    const [showNotification, setShowNotification] = useState<string | null>(null);
    const [countdown, setCountdown] = useState<number>(10); // Таймер отсчета
    const [progress, setProgress] = useState<number>(100); // Прогресс таймера (ширина прогресс-бара)
    const [refreshTrigger, setRefreshTrigger] = useState<boolean>(false); // Стейт для триггера обновления данных
    const [users, setUsers] = useState<any[]>([]); // Данные пользователей

    // Обработчик создания нового пользователя
    const handleCreateUser = async () => {
        try {
            await createUser(newUser);
            setShowNotification("User successfully added!");
            setTimeout(() => setShowNotification(null), 3000);
            setNewUser({ name: "", email: "" }); // Очищаем поля
            setRefreshTrigger(prev => !prev); // Меняем состояние для перезапуска загрузки
            setCountdown(10); // Сброс таймера
            setProgress(100); // Сброс прогресса
        } catch (error) {
            setShowNotification("Error adding user!");
            setTimeout(() => setShowNotification(null), 3000);
            console.error("Error creating user:", error);
        }
    };

    // Обработчик удаления пользователя
    const handleDeleteUser = async (id: string) => {
        try {
            await deleteUser(id); // Удаляем пользователя по id
            setShowNotification("User successfully deleted!");
            setTimeout(() => setShowNotification(null), 3000);
            setRefreshTrigger(prev => !prev); // Перезапуск загрузки данных
        } catch (error) {
            setShowNotification("Error deleting user!");
            setTimeout(() => setShowNotification(null), 3000);
            console.error("Error deleting user:", error);
        }
    };

    useEffect(() => {
        const interval = setInterval(() => {
            setCountdown((prev) => {
                if (prev === 1) {
                    setProgress(100); // Сброс прогресс-бара на 100 после завершения таймера
                    setRefreshTrigger(prev => !prev); // Обновление данных по истечении таймера
                    return 10; // Сбрасываем таймер
                }

                setProgress(((prev - 1) * 10)); // Уменьшаем прогресс
                return prev - 1; // Уменьшаем таймер
            });
        }, 1000);

        return () => clearInterval(interval); // Очистка интервала при размонтировании компонента
    }, []); // useEffect будет вызван только один раз при монтировании компонента

    return (
        <div className="container mt-4">
            {showNotification && <Notification message={showNotification} type="success" />}
            <ServerStatusWrapper apiCall={getUsers} refreshTrigger={refreshTrigger}>
                {(users: any[]) => (
                    <>
                        <div className="mt-4">
                            <h3>Create New User</h3>
                            <input
                                type="text"
                                className="form-control mb-2"
                                value={newUser.name}
                                onChange={(e) => setNewUser({ ...newUser, name: e.target.value })}
                                placeholder="Enter name"
                            />
                            <input
                                type="email"
                                className="form-control mb-2"
                                value={newUser.email}
                                onChange={(e) => setNewUser({ ...newUser, email: e.target.value })}
                                placeholder="Enter email"
                            />
                            <button className="btn btn-success w-100" onClick={handleCreateUser}>
                                Create User
                            </button>
                        </div>

                        <Table
                            columns={["Name", "Email"]}
                            data={users}
                            key={users.map(user => user.id).join(",")}
                            onDelete={handleDeleteUser} // Передаем функцию удаления
                        />
                    </>
                )}
            </ServerStatusWrapper>
        </div>
    );
};

export default UserComponent;