// src/components/ui.jsx — chhote shared components, taaki har screen same dikhe.
// Colours sirf semantic tokens se aate hain (src/index.css ka @theme block).
// Formatting helpers ../lib/format mein hain.

const STATUS_STYLES = {
  PENDING: 'bg-accent-50 text-accent-700 border-accent-200',
  CONFIRMED: 'bg-info-50 text-info-700 border-info-200',
  SHIPPED: 'bg-brand-50 text-brand-700 border-brand-200',
  DELIVERED: 'bg-brand-600 text-ink-inverse border-brand-600',
  CANCELLED: 'bg-danger-50 text-danger-700 border-danger-200',
  SUCCESS: 'bg-brand-50 text-brand-700 border-brand-200',
  FAILED: 'bg-danger-50 text-danger-700 border-danger-200',
};

export function StatusBadge({ status }) {
  const tone = STATUS_STYLES[status] ?? 'bg-surface-muted text-ink-muted border-line';
  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-[11px] font-semibold uppercase tracking-wide ${tone}`}
    >
      {status ?? 'UNKNOWN'}
    </span>
  );
}

export function PageHeader({ title, subtitle, children }) {
  return (
    <div className="mb-6 flex flex-col gap-3 sm:mb-8 sm:flex-row sm:items-start sm:justify-between sm:gap-4">
      <div className="min-w-0">
        <h1 className="text-2xl font-bold tracking-tight text-ink sm:text-[28px]">{title}</h1>
        {subtitle && <p className="mt-1 text-sm text-ink-muted">{subtitle}</p>}
      </div>
      {children && <div className="flex shrink-0 flex-wrap items-center gap-3">{children}</div>}
    </div>
  );
}

export function Card({ className = '', children }) {
  return (
    <div className={`rounded-card border border-line bg-surface ${className}`}>{children}</div>
  );
}

export function Loading({ label = 'Loading…' }) {
  return (
    <div className="flex items-center justify-center gap-3 p-12 text-ink-muted">
      <svg className="h-4 w-4 animate-spin motion-reduce:animate-none" viewBox="0 0 24 24" fill="none">
        <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="3" opacity="0.2" />
        <path d="M22 12a10 10 0 00-10-10" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
      </svg>
      <span className="text-sm">{label}</span>
    </div>
  );
}

export function ErrorBanner({ message, onRetry }) {
  if (!message) return null;
  return (
    <div
      role="alert"
      className="mb-4 flex flex-col gap-2 rounded-control border border-danger-200 bg-danger-50 px-4 py-3 text-sm text-danger-700 sm:flex-row sm:items-center sm:justify-between sm:gap-3"
    >
      <span>{message}</span>
      {onRetry && (
        <button
          onClick={onRetry}
          className="self-start font-semibold underline hover:no-underline sm:self-auto"
        >
          Try again
        </button>
      )}
    </div>
  );
}

export function EmptyState({ title, hint, children }) {
  return (
    <Card className="px-6 py-12 text-center sm:py-16">
      <p className="text-base font-semibold text-ink">{title}</p>
      {hint && <p className="mx-auto mt-2 max-w-sm text-sm text-ink-muted">{hint}</p>}
      {children && <div className="mt-6 flex flex-wrap justify-center gap-3">{children}</div>}
    </Card>
  );
}

export function StatTile({ label, value, hint, tone = 'text-ink' }) {
  return (
    <Card className="p-5">
      <p className="text-xs font-semibold uppercase tracking-wide text-ink-subtle">{label}</p>
      <p className={`mt-2 text-2xl font-bold tracking-tight ${tone}`}>{value}</p>
      {hint && <p className="mt-1 text-xs text-ink-muted">{hint}</p>}
    </Card>
  );
}

export function Pagination({ page, pageSize, total, onPage }) {
  const pages = Math.max(1, Math.ceil(total / pageSize));
  if (total === 0) return null;

  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, total);

  const button =
    'rounded-control border border-line bg-surface px-3 py-1.5 font-medium text-ink transition-colors hover:bg-surface-muted disabled:opacity-40 disabled:hover:bg-surface';

  return (
    <div className="mt-4 flex flex-col items-center gap-3 text-sm text-ink-muted sm:flex-row sm:justify-between">
      <span>
        {from}–{to} of {total}
      </span>
      <div className="flex gap-2">
        <button onClick={() => onPage(page - 1)} disabled={page <= 1} className={button}>
          Previous
        </button>
        <button onClick={() => onPage(page + 1)} disabled={page >= pages} className={button}>
          Next
        </button>
      </div>
    </div>
  );
}
