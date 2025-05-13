// Notification.tsx
import React from "react";

interface NotificationProps {
    message: string | null;
    type: "success" | "error";
}

const Notification: React.FC<NotificationProps> = ({ message, type }) => {
    if (!message) return null;

    return (
        <div
            style={{
                padding: "10px",
                backgroundColor: type === "success" ? "green" : "red",
                color: "white",
                borderRadius: "5px",
                marginBottom: "20px",
            }}
        >
            {message}
        </div>
    );
};

export default Notification;