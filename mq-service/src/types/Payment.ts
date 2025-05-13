export interface Payment {
    id?: string;  // Сделать id необязательным
    orderId?: string;
    amount?: number;
    paymentStatus?: string | null;
}