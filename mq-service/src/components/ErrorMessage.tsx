import React from "react";

interface ErrorMessageProps {
    message: string;
}

const ErrorMessage: React.FC<ErrorMessageProps> = ({ message }) => {
    return (
        <div className="alert alert-danger mt-4" role="alert">
            <strong>Error:</strong> {message}
        </div>
    );
};

export default ErrorMessage;