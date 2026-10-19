// src/pages/NotFoundPage.jsx

import { Link } from 'react-router-dom';

export default function NotFoundPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-surface-muted p-8">
      <div className="max-w-md rounded-control border border-line bg-surface p-10 text-center">
        <p className="text-sm font-medium text-ink-subtle">404</p>
        <h1 className="mt-1 text-xl font-semibold text-ink">Page not found</h1>
        <p className="mt-2 text-sm text-ink-muted">
          The link may be out of date, or the page may have moved.
        </p>
        <Link
          to="/home"
          className="mt-6 inline-block rounded-control bg-brand-600 px-5 py-2.5 text-sm font-medium text-ink-inverse hover:bg-brand-700"
        >
          Go to catalog
        </Link>
      </div>
    </div>
  );
}
