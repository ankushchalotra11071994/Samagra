 import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/auth-context';

const REMEMBER_KEY = 'rememberedEmail';

// localStorage can throw (private mode, disabled storage) — never let that break login
const storage = {
  get: (k) => { try { return localStorage.getItem(k); } catch { return null; } },
  set: (k, v) => { try { localStorage.setItem(k, v); } catch { /* ignore */ } },
  remove: (k) => { try { localStorage.removeItem(k); } catch { /* ignore */ } },
};

export default function Login() {
  // Pre-fill email if the user chose "Remember me" last time — pehle render par
  // hi padh lete hain, effect se setState karne ki zaroorat nahi
  const [form, setForm] = useState(() => ({
    email: storage.get(REMEMBER_KEY) ?? '',
    password: '',
  }));
  const [rememberMe, setRememberMe] = useState(() => Boolean(storage.get(REMEMBER_KEY)));
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      // Backend decides cookie lifetime based on rememberMe.
      // login() cookie set hone ke baad /auth/me padhta hai, to context turant bhar jaata hai.
      const me = await login(form.email, form.password, rememberMe);

      // Save only the email — NEVER the password
      if (rememberMe) storage.set(REMEMBER_KEY, form.email);
      else storage.remove(REMEMBER_KEY);

      // Guard ne kahin se bheja tha to wahin wapas, warna role ke hisaab se landing
      const fallback = me?.roles?.includes('Admin') ? '/admin' : '/home';
      navigate(location.state?.from || fallback, { replace: true });
    } catch (err) {
      if (err.response?.status === 401) {
        setError('That email and password don’t match. Check both and try again.');
      } else if (err.response?.data?.message) {
        setError(err.response.data.message);
      } else {
        setError('Couldn’t reach the server. Check your connection and try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  const inputBase =
    'w-full rounded-control border bg-surface px-4 py-3 text-[15px] text-ink placeholder:text-ink-subtle ' +
    'transition focus:outline-none focus:ring-4 focus:ring-brand-600/15 focus:border-brand-600';
  const inputBorder = error ? 'border-danger-200' : 'border-line';

  return (
    <div
      className="grid min-h-screen bg-canvas lg:grid-cols-[1.1fr_1fr]"
    >
      {/* Left brand panel — hidden on small screens */}
      <aside className="relative hidden lg:flex flex-col justify-between overflow-hidden bg-brand-700 p-12 text-white">
        {/* Shelf-grid motif: reads as a product catalog */}
        <div className="pointer-events-none absolute inset-0 opacity-[0.07]" aria-hidden="true">
          <div className="grid h-full grid-cols-6 gap-4 p-8">
            {Array.from({ length: 36 }).map((_, i) => (
              <div key={i} className="rounded-lg border border-white" />
            ))}
          </div>
        </div>

        <div className="relative flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-control bg-accent-500 text-brand-900">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M21 8l-9-5-9 5 9 5 9-5z" />
              <path d="M3 8v8l9 5 9-5V8" />
              <path d="M12 13v8" />
            </svg>
          </div>
          <span className="text-lg font-extrabold tracking-tight">Samagra</span>
        </div>

        <div className="relative max-w-md">
          <h2 className="text-[42px] font-extrabold leading-[1.1] tracking-tight">
            Every product, stock level and price in one place.
          </h2>
          <p className="mt-5 text-[17px] leading-relaxed text-white/65">
            Sign in to manage your catalog and keep your stock up to date.
          </p>
        </div>

        <p className="relative text-sm text-white/40">© {new Date().getFullYear()} Samagra</p>
      </aside>

      {/* Form panel */}
      <main className="flex items-center justify-center px-5 py-12 sm:px-10">
        <div className="w-full max-w-[400px]">
          <h1 className="text-[32px] font-extrabold tracking-tight text-ink">Welcome back</h1>
          <p className="mt-2 text-[15px] text-ink-muted">Sign in with your work email.</p>

          <form onSubmit={handleSubmit} className="mt-8 space-y-5" noValidate={false}>
            <div>
              <label htmlFor="email" className="mb-2 block text-sm font-semibold text-ink">
                Email
              </label>
              <input
                id="email"
                name="email"
                type="email"
                value={form.email}
                onChange={handleChange}
                required
                autoComplete="email"
                placeholder="you@company.com"
                className={`${inputBase} ${inputBorder}`}
              />
            </div>

            <div>
              <div className="mb-2 flex items-center justify-between">
                <label htmlFor="password" className="text-sm font-semibold text-ink">
                  Password
                </label>
                <a
                  href="/forgot-password"
                  className="rounded text-sm font-semibold text-brand-600 hover:text-brand-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-600"
                >
                  Forgot password?
                </a>
              </div>
              <div className="relative">
                <input
                  id="password"
                  name="password"
                  type={showPassword ? 'text' : 'password'}
                  value={form.password}
                  onChange={handleChange}
                  required
                  autoComplete="current-password"
                  placeholder="Enter your password"
                  className={`${inputBase} ${inputBorder} pr-12`}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((s) => !s)}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                  className="absolute inset-y-0 right-0 flex w-12 items-center justify-center rounded-r-control text-ink-subtle hover:text-ink focus:outline-none focus-visible:text-brand-600"
                >
                  {showPassword ? (
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                      <path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94" />
                      <path d="M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19" />
                      <path d="M1 1l22 22" />
                    </svg>
                  ) : (
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                      <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                      <circle cx="12" cy="12" r="3" />
                    </svg>
                  )}
                </button>
              </div>
            </div>

            <label className="flex cursor-pointer select-none items-center gap-3">
              <input
                type="checkbox"
                checked={rememberMe}
                onChange={(e) => setRememberMe(e.target.checked)}
                className="h-[18px] w-[18px] cursor-pointer rounded accent-brand-600"
              />
              <span className="text-sm text-ink">Keep me signed in on this device</span>
            </label>

            {error && (
              <div
                role="alert"
                className="flex items-start gap-3 rounded-control border border-danger-200 bg-danger-50 px-4 py-3 text-sm text-danger-700"
              >
                <svg className="mt-0.5 shrink-0" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2">
                  <circle cx="12" cy="12" r="10" />
                  <path d="M12 8v4M12 16h.01" />
                </svg>
                <span>{error}</span>
              </div>
            )}

            <button
              type="submit"
              disabled={loading}
              className="flex w-full items-center justify-center gap-2 rounded-control bg-brand-700 py-3.5 text-[15px] font-bold text-white
                         transition hover:bg-brand-800 active:scale-[0.99]
                         focus:outline-none focus-visible:ring-4 focus-visible:ring-brand-700/25
                         disabled:cursor-not-allowed disabled:opacity-60"
            >
              {loading && (
                <svg className="h-4 w-4 animate-spin motion-reduce:animate-none" viewBox="0 0 24 24" fill="none">
                  <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="3" opacity="0.25" />
                  <path d="M22 12a10 10 0 00-10-10" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
                </svg>
              )}
              {loading ? 'Signing in…' : 'Sign in'}
            </button>
          </form>

          <p className="mt-8 text-center text-sm text-ink-muted">
            Don’t have an account?{' '}
            <a href="/register" className="font-semibold text-brand-600 hover:text-brand-700">
              Create one
            </a>
          </p>
        </div>
      </main>
    </div>
  );
}