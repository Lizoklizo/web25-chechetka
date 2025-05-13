import React, { useState, useEffect, JSX } from "react";

interface ServerStatusWrapperProps {
    apiCall: () => Promise<any>;
    children: (data: any) => JSX.Element;
    refreshTrigger: boolean; // Новый пропс для перезапуска запроса
}

const ServerStatusWrapper: React.FC<ServerStatusWrapperProps> = ({ apiCall, children, refreshTrigger }) => {
    const [data, setData] = useState<any>(null);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [isError, setIsError] = useState<boolean>(false);

    useEffect(() => {
        const fetchData = async () => {
            setIsLoading(true);
            setIsError(false);

            try {
                const result = await apiCall();
                setData(result);
            } catch (error) {
                setIsError(true);
                console.error("Error fetching data:", error);
            } finally {
                setIsLoading(false);
            }
        };

        fetchData();
    }, [apiCall, refreshTrigger]); // Зависимость от refreshTrigger

    if (isLoading) {
        return <div>Loading...</div>;
    }

    if (isError) {
        return <div>Error fetching data</div>;
    }

    return children(data); // Рендерим дочерний компонент с данными
};

export default ServerStatusWrapper;