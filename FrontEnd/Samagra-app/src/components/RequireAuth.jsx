// src/components/RequireAuth.jsx
//
// Route guard. Buyer screens ke liye <RequireAuth />, admin ke liye
// <RequireAuth requireAdmin />. Server par bhi role check hai — ye sirf UI hai.

import { Navigate, Outlet, useLocation, Link } from 'react-router-dom';
import { useAuth } from '../context/auth-context';
import { Loading } from './ui';

export default function RequireAuth({ requireAdmin = false }) {
  const { user, loading, isAdmin } = useAuth();
  const location = useLocation();

  if (loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface-muted">
        <Loading label="Checking your session…" />
      </div>
    );
  }

  if (!user) {
    // Login ke baad wapas isi page par bhej sakein
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  if (requireAdmin && !isAdmin) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface-muted p-8">
        <div className="max-w-md rounded-control border border-line bg-surface p-10 text-center">
          <h1 className="text-xl font-semibold text-ink">Admins only</h1>
          <p className="mt-2 text-sm text-ink-muted">
            This area manages the catalog and orders. Your account is signed in as a buyer.
          </p>
          <Link
            to="/home"
            className="mt-6 inline-block rounded-control bg-brand-600 px-5 py-2.5 text-sm font-medium text-ink-inverse hover:bg-brand-700"
          >
            Back to shopping
          </Link>
        </div>
      </div>
    );
  }

  return <Outlet />;
}
