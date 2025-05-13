import axios, { AxiosResponse } from "axios";
import { User } from "../types/User";
import { API_URLS } from "../config"; // Импорт конфигурации с URL

const getUsers = (): Promise<AxiosResponse<User[]>> => {
    console.log("Sending request to get users...");  // Лог запроса
    return axios.get(API_URLS.USER_SERVICE)
        .then((response) => {
            console.log("Received response for getUsers:", response);  // Лог успешного ответа
            return response.data;
        })
        .catch((error) => {
            console.error("Error fetching users:", error);  // Лог ошибки
            throw error;
        });
};

const getUserById = (id: string): Promise<AxiosResponse<User>> => {
    console.log(`Sending request to get user by ID: ${id}`);  // Лог запроса
    return axios.get(`${API_URLS.USER_SERVICE}/${id}`)
        .then((response) => {
            console.log(`Received response for getUserById (ID: ${id}):`, response);  // Лог успешного ответа
            return response.data;
        })
        .catch((error) => {
            console.error(`Error fetching user by ID (${id}):`, error);  // Лог ошибки
            throw error;
        });
};

const createUser = (userData: User): Promise<AxiosResponse<User>> => {
    console.log("Sending request to create user:", userData);  // Лог запроса
    return axios.post(API_URLS.USER_SERVICE, userData)
        .then((response) => {
            console.log("Received response for createUser:", response);  // Лог успешного ответа
            return response.data;
        })
        .catch((error) => {
            console.error("Error creating user:", error);  // Лог ошибки
            throw error;
        });
};

const deleteUser = (id: string): Promise<AxiosResponse<void>> => {
    console.log(`Sending request to delete user with ID: ${id}`);
    return axios.delete(`${API_URLS.USER_SERVICE}/${id}`)
        .then((response) => {
            console.log(`Received response for deleteUser (ID: ${id}):`, response);
            return response.data;
        })
        .catch((error) => {
            console.error(`Error deleting user with ID (${id}):`, error);
            throw error;
        });
};

export { getUsers, getUserById, createUser, deleteUser };