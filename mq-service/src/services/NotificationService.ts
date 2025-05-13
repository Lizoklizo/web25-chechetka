import axios, { AxiosResponse } from "axios";
import { Notification } from "../types/Notification";

const API_URL = "http://localhost:5132/api/Notification";

const getNotifications = (): Promise<AxiosResponse<Notification[]>> => axios.get(API_URL);
const getNotificationById = (id: string): Promise<AxiosResponse<Notification>> => axios.get(`${API_URL}/${id}`);
const createNotification = (notificationData: Notification): Promise<AxiosResponse<Notification>> => axios.post(API_URL, notificationData);
const deleteNotification = (id: string): Promise<AxiosResponse<void>> => axios.delete(`${API_URL}/${id}`);

export { getNotifications, getNotificationById, createNotification, deleteNotification };