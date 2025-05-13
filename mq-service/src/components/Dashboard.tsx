import React, { useState } from "react";
import NotificationComponent from "./NotificationComponent";
import UserComponent from "./UserComponent";
import OrderComponent from "./OrderComponent";
import PaymentComponent from "./PaymentComponent";

const Dashboard: React.FC = () => {
    const [selectedComponent, setSelectedComponent] = useState<string>("user");

    const renderComponent = () => {
        switch (selectedComponent) {
            case "user":
                return <UserComponent />;
            case "order":
                return <OrderComponent />;
            case "payment":
                return <PaymentComponent />;
            case "notification":
                return <NotificationComponent />;
            default:
                return <UserComponent />;
        }
    };

    return (
        <div>
            {/* Минималистичное меню */}
            <header className="bg-dark text-white text-center py-3 fixed-top w-100">
                <h1 className="mb-0">Admin Dashboard</h1>
            </header>

            {/* Навигационное меню */}
            <div className="container my-5 pt-5">
                <div className="row">
                    <div className="col-md-3">
                        <div className="list-group">
                            <button
                                className={`list-group-item list-group-item-action ${selectedComponent === "user" ? "active" : ""}`}
                                onClick={() => setSelectedComponent("user")}
                            >
                                User List
                            </button>
                            <button
                                className={`list-group-item list-group-item-action ${selectedComponent === "order" ? "active" : ""}`}
                                onClick={() => setSelectedComponent("order")}
                            >
                                Order List
                            </button>
                            <button
                                className={`list-group-item list-group-item-action ${selectedComponent === "payment" ? "active" : ""}`}
                                onClick={() => setSelectedComponent("payment")}
                            >
                                Payment List
                            </button>
                            <button
                                className={`list-group-item list-group-item-action ${selectedComponent === "notification" ? "active" : ""}`}
                                onClick={() => setSelectedComponent("notification")}
                            >
                                Notification List
                            </button>
                        </div>
                    </div>

                    {/* Основной контент */}
                    <div className="col-md-9">
                        <div className="card border-secondary">
                            <div className="card-header bg-light text-dark">
                                {selectedComponent.charAt(0).toUpperCase() + selectedComponent.slice(1)} List
                            </div>
                            <div className="card-body" style={{ overflowY: "auto" }}>
                                {renderComponent()}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default Dashboard;