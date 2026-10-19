// src/pages/ProfilePage.jsx

import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/auth-context';
import { Card, PageHeader } from '../components/ui';

export default function ProfilePage() {
  const { user, isAdmin, logout } = useAuth();
  const navigate = useNavigate();

  const handleSignOut = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  const rows = [
    { label: 'Full name', value: user?.fullName || '—' },
    { label: 'Email', value: user?.email },
    { label: 'Role', value: isAdmin ? 'Admin' : 'Buyer' },
    { label: 'Buyer id', value: user?.buyerId, mono: true },
    { label: 'User id', value: user?.userId, mono: true },
  ];

  return (
    <div className="max-w-2xl">
      <PageHeader title="Profile" subtitle="Details attached to your sign-in." />

      <Card className="divide-y divide-line">
        {rows.map((row) => (
          <div key={row.label} className="flex flex-wrap gap-2 px-6 py-4">
            <dt className="w-32 shrink-0 text-sm text-ink-muted">{row.label}</dt>
            <dd
              className={`text-sm text-ink ${row.mono ? 'break-all font-mono text-xs' : ''}`}
            >
              {row.value || '—'}
            </dd>
          </div>
        ))}
      </Card>

      <p className="mt-4 text-sm text-ink-muted">
        Editing these details isn’t supported yet — there is no profile update endpoint on the
        API.
      </p>

      <button
        onClick={handleSignOut}
        className="mt-6 rounded-control border border-line px-5 py-2.5 text-sm font-medium text-ink hover:bg-surface-muted"
      >
        Sign out
      </button>
    </div>
  );
}
