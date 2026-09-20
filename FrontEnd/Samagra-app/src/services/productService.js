// src/services/productService.js

import api from './api/axios.js'; //  

export const getAllProducts = async () => {
  const response = await api.get('/products/hybrid');
  return response.data;
};

// Product detail page ke liye — ek product
export const getProductById = async (id) => {
  const response = await api.get(`/products/${id}`);
  return response.data;
};