import React from "react";

const LoadingSpinner: React.FC = () => {
    return (
        <div className="text-center mt-4">
            <div className="spinner-border text-primary" role="status">
                <span className="visually-hidden">Loading...</span>
            </div>
        </div>
    );
};

export default LoadingSpinner;