import React, { useState }  from "react";

interface TableProps {
    columns: string[]; // Названия столбцов
    data: { [key: string]: any }[]; // Массив объектов с данными
    onDelete: (id: string) => void; // Функция для удаления строки, принимающая id строки
}

// Проверка на GUID
const isGuid = (value: string) => {
    const guidPattern = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
    return guidPattern.test(value);
};

// Функция для форматирования GUID (обрезаем до первых 8 символов)
const formatGuid = (guid: string) => {
    return guid.slice(0, 8) + '...';
};

const Table: React.FC<TableProps> = ({ columns, data, onDelete }) => {
    // Состояние для управления раскрытием GUID в ячейках
    const [expandedGuids, setExpandedGuids] = useState<Set<string>>(new Set());

    // Обработчик клика по ячейке с GUID
    const toggleGuid = (guid: string) => {
        const newExpandedGuids = new Set(expandedGuids);
        if (newExpandedGuids.has(guid)) {
            newExpandedGuids.delete(guid); // Если уже раскрыт, скрываем
        } else {
            newExpandedGuids.add(guid); // Если не раскрыт, показываем
        }
        setExpandedGuids(newExpandedGuids);
    };

    // Проверяем, что данные есть
    if (!Array.isArray(data) || data.length === 0) {
        return (
            <div style={{ textAlign: "center", padding: "20px" }}>
                <p>No data available</p>
            </div>
        );
    }

    return (
        <table className="table table-bordered">
            <thead>
            <tr>
                {/* Столбец для ID всегда на первом месте */}
                <th style={{ fontWeight: "bold", color: "blue" }}>ID</th>

                {/* Проверяем, передан ли столбец для UserId и показываем его */}
                {columns.includes("User Id") && <th style={{ fontWeight: "bold", color: "blue" }}>User Id</th>}

                {/* Проверяем, передан ли столбец для OrderId и показываем его */}
                {columns.includes("Order Id") && <th style={{ fontWeight: "bold", color: "blue" }}>Order Id</th>}

                {columns.map((column, index) => column !== "User Id" && column !== "Order Id" && (
                    <th key={index}>{column}</th> // Заголовки столбцов из переданных данных
                ))}
                <th>Actions</th> {/* Колонка для кнопки удаления */}
            </tr>
            </thead>
            <tbody>
            {data.map((row, rowIndex) => (
                <tr key={rowIndex}>
                    {/* Столбец для ID с уникальной стилизацией */}
                    <td
                        style={{ fontWeight: "bold", color: "blue", cursor: 'pointer' }}
                        onClick={() => toggleGuid(row.id)} // Клик по ID для раскрытия
                    >
                        {expandedGuids.has(row.id) ? row.id : formatGuid(row.id)} {/* Форматируем и отображаем GUID */}
                    </td>

                    {/* Проверяем, есть ли UserId в данных и отображаем его */}
                    {columns.includes("User Id") && row.userId && (
                        <td
                            style={{ fontWeight: "bold", color: "blue", cursor: 'pointer' }}
                            onClick={() => toggleGuid(row.userId)} // Клик по UserId для раскрытия
                        >
                            {expandedGuids.has(row.userId) ? row.userId : formatGuid(row.userId)} {/* Форматируем и отображаем GUID */}
                        </td>
                    )}

                    {/* Проверяем, есть ли OrderId в данных и отображаем его */}
                    {columns.includes("Order Id") && row.orderId && (
                        <td
                            style={{ fontWeight: "bold", color: "blue", cursor: 'pointer' }}
                            onClick={() => toggleGuid(row.orderId)} // Клик по OrderId для раскрытия
                        >
                            {expandedGuids.has(row.orderId) ? row.orderId : formatGuid(row.orderId)} {/* Форматируем и отображаем GUID */}
                        </td>
                    )}

                    {/* Остальные столбцы */}
                    {columns.map((column, colIndex) => {
                        const field = column.toLowerCase(); // Преобразуем строку в camelCase
                        const value = row[field];

                        // Если значение существует и есть в данных
                        if (value !== undefined && column !== "User Id" && column !== "Order Id") {
                            const isGuidValue = isGuid(value); // Проверяем, является ли значение GUID

                            return (
                                <td
                                    key={colIndex}
                                    style={{
                                        cursor: isGuidValue ? 'pointer' : 'default',
                                        color: isGuidValue ? 'blue' : 'inherit'
                                    }} // Выделяем ячейки с GUID
                                    onClick={isGuidValue ? () => toggleGuid(value) : undefined} // Добавляем обработчик клика только для GUID
                                >
                                    {isGuidValue && expandedGuids.has(value) ? value : isGuidValue ? formatGuid(value) : value}
                                    {/* Если это GUID, показываем полный или урезанный */}
                                </td>
                            );
                        }

                        return null; // Если данных для этого столбца нет, пропускаем
                    })}
                    <td>
                        {/* Кнопка удаления */}
                        <button
                            onClick={() => onDelete(row.id)} // Передаем id строки в функцию удаления
                            className="btn btn-danger"
                        >
                            Delete
                        </button>
                    </td>
                </tr>
            ))}
            </tbody>
        </table>
    );
};

export default Table;