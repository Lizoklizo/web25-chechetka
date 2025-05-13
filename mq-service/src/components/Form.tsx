// Form.tsx
import React, { useState } from "react";

interface FormProps {
    onSubmit: (data: any) => void;
    fields: string[];
}

const Form: React.FC<FormProps> = ({ onSubmit, fields }) => {
    const [formData, setFormData] = useState<any>({});

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>, field: string) => {
        setFormData({ ...formData, [field]: e.target.value });
    };

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        onSubmit(formData);
    };

    return (
        <form onSubmit={handleSubmit} style={{ marginTop: "20px" }}>
            {fields.map((field) => (
                <div key={field} className="mb-3">
                    <label>{field}:</label>
                    <input
                        type="text"
                        className="form-control"
                        value={formData[field] ?? ""}
                        onChange={(e) => handleInputChange(e, field)}
                        placeholder={`Enter ${field}`}
                    />
                </div>
            ))}
            <button type="submit" className="btn btn-primary w-100">
                Submit
            </button>
        </form>
    );
};

export default Form;