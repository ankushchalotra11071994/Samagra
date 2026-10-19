// src/context/AuthContext.jsx
//
// Token HttpOnly cookie mein hai, to frontend use padh nahi sakta.
// Session kaun hai ye /auth/me se poochna padta hai — har reload par ek baar.

import { useCallback, useEffect, useState } from 'react';
import api from '../api/axios';
import { AuthContext } from './auth-context';

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    try {
      const res = await api.get('/auth/me');
      setUser(res.data);
      return res.data;
    } catch {
      // 401 = koi signed in nahi hai, ye error nahi hai
      setUser(null);
      return null;
    }
  }, []);

  useEffect(() => {
    let active = true;

    (async () => {
      await refresh();
      if (active) setLoading(false);
    })();

    return () => {
      active = false;
    };
  }, [refresh]);

  const login = async (email, password, rememberMe) => {
    await api.post('/auth/login', { email, password, rememberMe });
    return refresh();
  };

  const logout = async () => {
    try {
      await api.post('/auth/logout');
    } finally {
      // Server cookie na hata paye tab bhi UI ko signed out dikhna chahiye
      setUser(null);
    }
  };

  const isAdmin = Boolean(user?.roles?.includes('Admin'));

  return (
    <AuthContext.Provider value={{ user, loading, isAdmin, login, logout, refresh }}>
      {children}
    </AuthContext.Provider>
  );
}
