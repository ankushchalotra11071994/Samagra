// src/services/orderService.js

import api from '../api/axios';

// userId body mein nahi bhejte — backend token se nikaalta hai
export const placeOrder = async (items) => {
  const response = await api.post('/orders', { items });
  return response.data; // { id, totalAmount, status, createdAt }
};

export const payForOrder = async (orderId, paymentMethod = 'UPI') => {
  const response = await api.post('/payments', { orderId, paymentMethod });
  return response.data;
};

export const getMyOrders = async () => {
  const response = await api.get('/orders/mine');
  return response.data;
};

export const getOrderById = async (id) => {
  const response = await api.get(`/orders/${id}`);
  return response.data;
};

// Admin — saare orders, status filter + paging ke saath
export const getAllOrders = async ({ status = '', page = 1, pageSize = 20 } = {}) => {
  const response = await api.get('/orders', {
    params: { status: status || undefined, page, pageSize },
  });
  return response.data; // { items, page, pageSize, total }
};

export const updateOrderStatus = async (id, status) => {
  const response = await api.patch(`/orders/${id}/status`, { status });
  return response.data;
};

export const ORDER_STATUSES = ['PENDING', 'CONFIRMED', 'SHIPPED', 'DELIVERED', 'CANCELLED'];

// Backend ke OrderStatuses.CanMove ka mirror — UI sirf allowed options dikhaye
const NEXT_STATUS = {
  PENDING: ['CONFIRMED', 'CANCELLED'],
  CONFIRMED: ['SHIPPED', 'CANCELLED'],
  SHIPPED: ['DELIVERED'],
  DELIVERED: [],
  CANCELLED: [],
};

export const nextStatuses = (current) => NEXT_STATUS[current] ?? [];
