// src/components/ShopLayout.jsx — buyer screens ka common header.
// Mobile par nav apni alag scrollable row mein chala jaata hai, isliye
// chhoti screen par links kabhi overflow nahi karte.

import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/auth-context';
import { useCart } from '../context/CartContext';

const NAV = [
  { to: '/home', label: 'Catalog' },
  { to: '/orders', label: 'My Orders' },
  { to: '/ai', label: 'Assistant' },
];

const linkClass = ({ isActive }) =>
  `shrink-0 rounded-control px-3 py-2 text-sm font-semibold transition-colors ${
    isActive
      ? 'bg-brand-800 text-ink-inverse'
      : 'text-brand-100 hover:bg-brand-800/70 hover:text-ink-inverse'
  }`;

export default function ShopLayout() {
  const { user, isAdmin, logout } = useAuth();
  const { totalItems } = useCart();
  const navigate = useNavigate();

  const handleSignOut = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  const initial = (user?.fullName || user?.email || '?').trim().charAt(0).toUpperCase();

  return (
    <div className="min-h-screen bg-canvas">
      <header className="sticky top-0 z-20 bg-brand-700 shadow-sm">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="flex h-16 items-center justify-between gap-3">
            <Link to="/home" className="flex shrink-0 items-center gap-2.5">
              <span className="flex h-9 w-9 items-center justify-center rounded-control bg-accent-500 text-brand-900">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M21 8l-9-5-9 5 9 5 9-5z" />
                  <path d="M3 8v8l9 5 9-5V8" />
                </svg>
              </span>
              <span className="text-lg font-extrabold tracking-tight text-ink-inverse">
                Samagra
              </span>
            </Link>

            <nav className="hidden flex-1 items-center gap-1 md:flex">
              {NAV.map((item) => (
                <NavLink key={item.to} to={item.to} className={linkClass}>
                  {item.label}
                </NavLink>
              ))}
            </nav>

            <div className="flex shrink-0 items-center gap-2 sm:gap-3">
              {isAdmin && (
                <Link
                  to="/admin"
                  className="hidden rounded-control border border-brand-200/40 px-3 py-2 text-sm font-semibold text-brand-100 transition-colors hover:bg-brand-800 hover:text-ink-inverse sm:block"
                >
                  Admin
                </Link>
              )}

              <NavLink
                to="/cart"
                className="relative flex items-center gap-2 rounded-control bg-surface px-3 py-2 text-sm font-bold text-brand-700 transition-colors hover:bg-brand-50 sm:px-4"
              >
                <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                  <circle cx="9" cy="20" r="1.5" />
                  <circle cx="18" cy="20" r="1.5" />
                  <path d="M2 3h2.5l2.6 11.2a2 2 0 002 1.6h8.1a2 2 0 002-1.6L21 7H6" />
                </svg>
                <span className="hidden sm:inline">Cart</span>
                {totalItems > 0 && (
                  <span className="absolute -right-1.5 -top-1.5 flex h-5 min-w-5 items-center justify-center rounded-full bg-accent-500 px-1 text-[11px] font-bold text-brand-900">
                    {totalItems}
                  </span>
                )}
              </NavLink>

              <Link
                to="/profile"
                title={user?.email}
                className="flex h-9 w-9 items-center justify-center rounded-full bg-brand-800 text-sm font-bold text-ink-inverse transition-colors hover:bg-brand-900"
              >
                {initial}
              </Link>

              <button
                onClick={handleSignOut}
                aria-label="Sign out"
                className="flex h-9 w-9 items-center justify-center rounded-control text-brand-100 transition-colors hover:bg-brand-800 hover:text-ink-inverse"
              >
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4" />
                  <path d="M16 17l5-5-5-5M21 12H9" />
                </svg>
              </button>
            </div>
          </div>

          {/* Mobile nav — apni row, horizontally scrollable */}
          <nav className="-mx-4 flex gap-1 overflow-x-auto px-4 pb-2 md:hidden [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
            {NAV.map((item) => (
              <NavLink key={item.to} to={item.to} className={linkClass}>
                {item.label}
              </NavLink>
            ))}
            {isAdmin && (
              <NavLink to="/admin" className={linkClass}>
                Admin
              </NavLink>
            )}
          </nav>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-6 sm:px-6 sm:py-8 lg:px-8">
        <Outlet />
      </main>
    </div>
  );
}
