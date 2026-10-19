// src/services/addressService.js

import api from '../api/axios';

// Backend sirf apne hi pate bhejta hai — userId token se aata hai
export const getAddresses = async () => {
  const response = await api.get('/addresses');
  return response.data;
};

export const createAddress = async (payload) => {
  const response = await api.post('/addresses', payload);
  return response.data;
};

export const updateAddress = async (id, payload) => {
  await api.put(`/addresses/${id}`, payload);
};

export const makeAddressDefault = async (id) => {
  await api.post(`/addresses/${id}/default`);
};

// Soft delete — purane orders apna snapshot rakhte hain
export const deleteAddress = async (id) => {
  await api.delete(`/addresses/${id}`);
};

export const EMPTY_ADDRESS = {
  label: '',
  fullName: '',
  phone: '',
  line1: '',
  line2: '',
  city: '',
  state: '',
  postalCode: '',
  country: 'India',
  isDefault: false,
};
