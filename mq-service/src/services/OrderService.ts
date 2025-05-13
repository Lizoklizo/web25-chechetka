import axios, { AxiosResponse } from "axios";
import { Order } from "../types/Order";
import { API_URLS } from "../config"; // Импорт конфигурации с URL

const getOrders = (): Promise<AxiosResponse<Order[]>> => {
    console.log("Sending request to get orders...");  // Лог запроса
    return axios.get(API_URLS.ORDER_SERVICE)
        .then((response) => {
            console.log("Received response for getOrders:", response);  // Лог успешного ответа
            return response.data;
        })
        .catch((error) => {
            console.error("Error fetching orders:", error);  // Лог ошибки
            throw error;
        });
};

const getOrderById = (id: string): Promise<AxiosResponse<Order>> => {
    console.log(`Sending request to get order by ID: ${id}`);  // Лог запроса
    return axios.get(`${API_URLS.ORDER_SERVICE}/${id}`)
        .then((response) => {
            console.log(`Received response for getOrderById (ID: ${id}):`, response);  // Лог успешного ответа
            return response.data;
        })
        .catch((error) => {
            console.error(`Error fetching order by ID (${id}):`, error);  // Лог ошибки
            throw error;
        });
};

const createOrder = (orderData: Order): Promise<AxiosResponse<Order>> => {
    console.log("Sending request to create order:", orderData);  // Лог запроса
    return axios.post(API_URLS.ORDER_SERVICE, orderData)
        .then((response) => {
            console.log("Received response for createOrder:", response);  // Лог успешного ответа
            return response.data;
        })
        .catch((error) => {
            console.error("Error creating order:", error);  // Лог ошибки
            throw error;
        });
};

const deleteOrder = (id: string): Promise<AxiosResponse<void>> => {
    console.log(`Sending request to delete order with ID: ${id}`);
    return axios.delete(`${API_URLS.ORDER_SERVICE}/${id}`)
        .then((response) => {
            console.log(`Received response for deleteOrder (ID: ${id}):`, response);
            return response.data;
        })
        .catch((error) => {
            console.error(`Error deleting order with ID (${id}):`, error);
            throw error;
        });
};

export { getOrders, getOrderById, createOrder, deleteOrder };