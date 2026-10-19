// src/components/AdminLayout.jsx — admin screens sidebar shell.
// Buyer header se alag jaan boojh kar: yahan catalog manage hota hai, khareeda nahi jaata.

import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/auth-context';

const NAV = [
  { to: '/admin', label: 'Dashboard', end: true },
  { to: '/admin/products', label: 'Products' },
  { to: '/admin/categories', label: 'Categories' },
  { to: '/admin/orders', label: 'Orders' },
  { to: '/admin/bulk-import', label: 'Bulk import' },
  { to: '/admin/ai-usage', label: 'AI usage' },
];

const linkClass = ({ isActive }) =>
  `block rounded-control px-3 py-2 text-sm font-medium transition-colors ${
    isActive ? 'bg-brand-900 text-ink-inverse' : 'text-brand-100 hover:bg-brand-800 hover:text-ink-inverse'
  }`;

export default function AdminLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleSignOut = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  return (
    <div className="min-h-screen bg-canvas lg:flex">
      <aside className="bg-brand-900 lg:min-h-screen lg:w-60 lg:shrink-0">
        <div className="flex flex-wrap items-center justify-between gap-3 px-4 py-4 lg:block">
          <div>
            <Link to="/admin" className="text-lg font-semibold text-ink-inverse">
              Samagra
            </Link>
            <p className="text-xs uppercase tracking-wider text-ink-muted">Admin</p>
          </div>

          <nav className="flex flex-wrap gap-1 lg:mt-6 lg:flex-col">
            {NAV.map((item) => (
              <NavLink key={item.to} to={item.to} end={item.end} className={linkClass}>
                {item.label}
              </NavLink>
            ))}
          </nav>
        </div>

        <div className="hidden border-t border-brand-800 px-4 py-4 lg:block">
          <p className="truncate text-xs text-ink-subtle" title={user?.email}>
            {user?.fullName || user?.email}
          </p>
          <Link to="/home" className="mt-3 block text-sm text-brand-100 hover:text-ink-inverse">
            ← Back to shop
          </Link>
          <button
            onClick={handleSignOut}
            className="mt-2 text-sm text-ink-subtle hover:text-ink-inverse"
          >
            Sign out
          </button>
        </div>
      </aside>

      <main className="flex-1 px-4 py-8 sm:px-8">
        <div className="mx-auto max-w-6xl">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
