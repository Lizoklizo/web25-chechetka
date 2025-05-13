export interface Notification {
    id?: string;  // Сделать id необязательным
    userId?: string;
    message?: string | null;
}