import axios, { AxiosResponse } from "axios";
import { Payment } from "../types/Payment";
import { API_URLS } from "../config"; // Импорт конфигурации с URL

const getPayments = (): Promise<AxiosResponse<Payment[]>> => {
    console.log("Sending request to get payments...");  // Лог запроса
    return axios.get(API_URLS.PAYMENT_SERVICE)
        .then((response) => {
            console.log("Received response for getPayments:", response);  // Лог успешного ответа
            return response.data;
        })
        .catch((error) => {
            console.error("Error fetching payments:", error);  // Лог ошибки
            throw error;
        });
};

const getPaymentById = (id: string): Promise<AxiosResponse<Payment>> => {
    console.log(`Sending request to get payment by ID: ${id}`);  // Лог запроса
    return axios.get(`${API_URLS.PAYMENT_SERVICE}/${id}`)
        .then((response) => {
            console.log(`Received response for getPaymentById (ID: ${id}):`, response);  // Лог успешного ответа
            return response.data;
        })
        .catch((error) => {
            console.error(`Error fetching payment by ID (${id}):`, error);  // Лог ошибки
            throw error;
        });
};

const createPayment = (paymentData: Payment): Promise<AxiosResponse<Payment>> => {
    console.log("Sending request to create payment:", paymentData);  // Лог запроса
    return axios.post(API_URLS.PAYMENT_SERVICE, paymentData)
        .then((response) => {
            console.log("Received response for createPayment:", response);  // Лог успешного ответа
            return response.data;
        })
        .catch((error) => {
            console.error("Error creating payment:", error);  // Лог ошибки
            throw error;
        });
};

const deletePayment = (id: string): Promise<AxiosResponse<void>> => {
    console.log(`Sending request to delete payment with ID: ${id}`);
    return axios.delete(`${API_URLS.PAYMENT_SERVICE}/${id}`)
        .then((response) => {
            console.log(`Received response for deletePayment (ID: ${id}):`, response);
            return response.data;
        })
        .catch((error) => {
            console.error(`Error deleting payment with ID (${id}):`, error);
            throw error;
        });
};

export { getPayments, getPaymentById, createPayment, deletePayment };