// src/services/productService.js

import api from '../api/axios';

// Shop catalog — HybridCache ke peeche, sirf active products
export const getAllProducts = async () => {
  const response = await api.get('/products/hybrid');
  return response.data;
};

// Product detail page ke liye — ek product
export const getProductById = async (id) => {
  const response = await api.get(`/products/${id}`);
  return response.data;
};

// Admin list — inactive bhi, stock ke saath, paged
export const getProductsForManagement = async ({
  q = '',
  categoryId = '',
  includeInactive = true,
  page = 1,
  pageSize = 20,
} = {}) => {
  const response = await api.get('/products/manage', {
    params: {
      q: q || undefined,
      categoryId: categoryId || undefined,
      includeInactive,
      page,
      pageSize,
    },
  });
  return response.data; // { items, page, pageSize, total }
};

export const createProduct = async (payload) => {
  const response = await api.post('/products', payload);
  return response.data;
};

export const updateProduct = async (id, payload) => {
  await api.put(`/products/${id}`, payload);
};

// Soft delete — product IsActive = false ho jaata hai
export const deactivateProduct = async (id) => {
  await api.delete(`/products/${id}`);
};

export const restoreProduct = async (id) => {
  await api.post(`/products/${id}/restore`);
};
