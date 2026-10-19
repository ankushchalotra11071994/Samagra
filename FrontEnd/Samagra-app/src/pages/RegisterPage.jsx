// src/pages/RegisterPage.jsx

import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api from '../api/axios';
import { useAuth } from '../context/auth-context';
import { errorMessage } from '../lib/format';

export default function RegisterPage() {
  const [form, setForm] = useState({
    fullName: '',
    businessName: '',
    email: '',
    password: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      await api.post('/auth/register', form);
      // Register cookie set nahi karta, to turant login kar ke andar bhej dete hain
      await login(form.email, form.password, false);
      navigate('/home', { replace: true });
    } catch (err) {
      setError(errorMessage(err, 'Could not create the account'));
    } finally {
      setLoading(false);
    }
  };

  const input =
    'w-full rounded-control border border-line px-3 py-2.5 text-sm text-ink ' +
    'focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-600/20';

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface-muted p-6">
      <div className="w-full max-w-md">
        <h1 className="text-2xl font-semibold text-ink">Create your account</h1>
        <p className="mt-1 text-sm text-ink-muted">
          You will be signed in as a buyer once the account is created.
        </p>

        <form
          onSubmit={handleSubmit}
          className="mt-6 space-y-4 rounded-control border border-line bg-white p-6"
        >
          <div>
            <label htmlFor="fullName" className="mb-1.5 block text-sm font-medium text-ink">
              Full name
            </label>
            <input
              id="fullName"
              name="fullName"
              value={form.fullName}
              onChange={handleChange}
              required
              autoComplete="name"
              className={input}
            />
          </div>

          <div>
            <label
              htmlFor="businessName"
              className="mb-1.5 block text-sm font-medium text-ink"
            >
              Business name
            </label>
            <input
              id="businessName"
              name="businessName"
              value={form.businessName}
              onChange={handleChange}
              required
              autoComplete="organization"
              className={input}
            />
            <p className="mt-1.5 text-xs text-ink-muted">
              Your orders and pricing tier are tracked against this business.
            </p>
          </div>

          <div>
            <label htmlFor="email" className="mb-1.5 block text-sm font-medium text-ink">
              Work email
            </label>
            <input
              id="email"
              name="email"
              type="email"
              value={form.email}
              onChange={handleChange}
              required
              autoComplete="email"
              className={input}
            />
          </div>

          <div>
            <label htmlFor="password" className="mb-1.5 block text-sm font-medium text-ink">
              Password
            </label>
            <input
              id="password"
              name="password"
              type="password"
              value={form.password}
              onChange={handleChange}
              required
              autoComplete="new-password"
              className={input}
            />
          </div>

          {error && (
            <p role="alert" className="rounded-control border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full rounded-control bg-brand-600 py-2.5 text-sm font-medium text-white hover:bg-brand-700 disabled:opacity-50"
          >
            {loading ? 'Creating account…' : 'Create account'}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-ink-muted">
          Already have an account?{' '}
          <Link to="/login" className="font-medium text-brand-600 hover:text-brand-700">
            Sign in
          </Link>
        </p>
      </div>
    </div>
  );
}
