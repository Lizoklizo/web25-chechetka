export interface Order {
    id?: string;  // Сделать id необязательным
    userId?: string;
    product?: string;
    quantity?: number;
}