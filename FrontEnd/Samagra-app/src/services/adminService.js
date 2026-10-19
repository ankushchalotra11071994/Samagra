// src/services/adminService.js

import api from '../api/axios';

export const getDashboard = async () => {
  const response = await api.get('/admin/dashboard');
  return response.data;
};

export const getAiUsage = async (days = 7) => {
  const response = await api.get('/admin/ai-usage', { params: { days } });
  return response.data;
};
