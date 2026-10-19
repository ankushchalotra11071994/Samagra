// src/services/categoryService.js

import api from '../api/axios';

export const getCategories = async ({ includeInactive = false } = {}) => {
  const response = await api.get('/categories', { params: { includeInactive } });
  return response.data; // [{ id, name, isActive, productCount }]
};

export const createCategory = async (payload) => {
  const response = await api.post('/categories', payload);
  return response.data;
};

export const updateCategory = async (id, payload) => {
  await api.put(`/categories/${id}`, payload);
};

// Soft delete — active products ho to backend mana kar dega
export const deactivateCategory = async (id) => {
  await api.delete(`/categories/${id}`);
};
