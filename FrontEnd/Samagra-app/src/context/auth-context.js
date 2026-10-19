// src/context/auth-context.js — context object + hook.
// Provider component AuthContext.jsx mein hai (ek file mein dono rakhne par
// eslint ka react-refresh rule shikayat karta hai).

import { createContext, useContext } from 'react';

export const AuthContext = createContext(null);

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
};
