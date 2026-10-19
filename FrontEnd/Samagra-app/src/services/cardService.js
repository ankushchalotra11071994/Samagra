// src/services/cardService.js
//
// Card number sirf POST /api/cards tak jaata hai, jahan backend use validate
// karke brand + last4 rakh leta hai aur number phenk deta hai. Na yahan, na
// localStorage mein, na kahin aur card number rakha jaata hai.

import api from '../api/axios';

export const getCards = async () => {
  const response = await api.get('/cards');
  return response.data; // [{ id, brand, last4, display, expiryMonth, expiryYear, expired, isDefault }]
};

export const createCard = async (payload) => {
  const response = await api.post('/cards', payload);
  return response.data;
};

export const makeCardDefault = async (id) => {
  await api.post(`/cards/${id}/default`);
};

export const deleteCard = async (id) => {
  await api.delete(`/cards/${id}`);
};

// 4-4-4-4 grouping, sirf dikhane ke liye — bhejte waqt digits hi jaate hain
export const formatCardNumber = (value) =>
  (value || '')
    .replace(/\D/g, '')
    .slice(0, 19)
    .replace(/(.{4})/g, '$1 ')
    .trim();

export const digitsOnly = (value) => (value || '').replace(/\D/g, '');

// Brand guess sirf form par icon/label dikhane ke liye; asli faisla backend ka hai
export const guessBrand = (value) => {
  const d = digitsOnly(value);
  if (d.length < 2) return '';
  const two = Number(d.slice(0, 2));
  const three = Number(d.slice(0, 3));
  const four = Number(d.slice(0, 4));

  if (two === 34 || two === 37) return 'Amex';
  if (d[0] === '4') return 'Visa';
  if ((two >= 51 && two <= 55) || (four >= 2221 && four <= 2720)) return 'Mastercard';
  if (four === 6011) return 'Discover';
  if (four === 6521 || four === 6522) return 'RuPay';
  if (two === 60 || two === 81 || two === 82) return 'RuPay';
  if (two === 65 || (three >= 644 && three <= 649)) return 'Discover';
  return '';
};

export const expiryYears = (count = 12) => {
  const thisYear = new Date().getFullYear();
  return Array.from({ length: count }, (_, i) => thisYear + i);
};
