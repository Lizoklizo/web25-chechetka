import React from "react";
import { ProgressBar } from "react-bootstrap";

interface ProgressBarProps {
    now: number;
}

const CustomProgressBar: React.FC<ProgressBarProps> = ({ now }) => {
    return (
        <div style={{ textAlign: "center", marginTop: "20px" }}>
            <ProgressBar
                now={now}
                style={{
                    height: "20px",
                    backgroundColor: "#f3f3f3", // Цвет фона прогресс-бара
                    borderRadius: "10px",
                    transition: "width 1s linear", // Плавное изменение ширины
                }}
            />
        </div>
    );
};

export default CustomProgressBar;