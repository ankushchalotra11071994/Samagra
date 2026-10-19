// src/lib/format.js — plain helpers. Components ke saath ek file mein nahi rakh sakte,
// warna eslint ka react-refresh rule fast refresh todne ki shikayat karta hai.

export const formatMoney = (amount) =>
  `₹${Number(amount ?? 0).toLocaleString('en-IN', { maximumFractionDigits: 2 })}`;

export const formatDate = (value) =>
  value
    ? new Date(value).toLocaleString('en-IN', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      })
    : '—';

// DateOnly ("2026-10-19") ko local date maan kar padho, UTC shift se bachne ke liye
export const formatDay = (day) =>
  day
    ? new Date(`${day}T00:00:00`).toLocaleDateString('en-IN', {
        day: 'numeric',
        month: 'short',
      })
    : '—';

export const formatUsd = (amount) => `$${Number(amount ?? 0).toFixed(4)}`;

export const formatCount = (value) => Number(value ?? 0).toLocaleString('en-IN');

export const shortId = (id) => (id ? String(id).slice(0, 8).toUpperCase() : '—');

// Axios error se ek padhne laayak line nikalta hai
export const errorMessage = (err, fallback = 'Something went wrong') =>
  err?.response?.data?.message ||
  err?.response?.data?.error ||
  (Array.isArray(err?.response?.data?.errors) ? err.response.data.errors[0] : null) ||
  err?.message ||
  fallback;
