// src/pages/admin/AdminAiUsagePage.jsx
// Pipeline ka CostTracking middleware har LLM call ka row likhta hai — ye uski report hai.

import { useCallback, useState } from 'react';
import { getAiUsage } from '../../services/adminService';
import useAsyncData from '../../hooks/useAsyncData';
import { Card, ErrorBanner, Loading, PageHeader, StatTile } from '../../components/ui';
import { formatCount, formatDay, formatUsd } from '../../lib/format';

const RANGES = [7, 14, 30, 90];

function Table({ title, head, rows, empty }) {
  return (
    <Card className="overflow-x-auto">
      <div className="border-b border-line px-5 py-4">
        <h2 className="font-semibold text-ink">{title}</h2>
      </div>
      {rows.length === 0 ? (
        <p className="px-5 py-8 text-center text-sm text-ink-muted">{empty}</p>
      ) : (
        <table className="w-full text-sm">
          <thead className="border-b border-line bg-surface-muted text-left text-xs uppercase tracking-wide text-ink-muted">
            <tr>
              {head.map((h, i) => (
                <th key={h} className={`px-5 py-3 font-medium ${i > 0 ? 'text-right' : ''}`}>
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-line">{rows}</tbody>
        </table>
      )}
    </Card>
  );
}

export default function AdminAiUsagePage() {
  const [days, setDays] = useState(7);

  const fetchUsage = useCallback(() => getAiUsage(days), [days]);
  const { data, loading, error, reload } = useAsyncData(fetchUsage, {
    fallbackError: 'Could not load the usage report',
  });

  return (
    <div>
      <PageHeader
        title="AI usage"
        subtitle="Tokens and cost recorded by the cost-tracking middleware."
      >
        <div className="flex gap-2">
          {RANGES.map((d) => (
            <button
              key={d}
              onClick={() => setDays(d)}
              className={`rounded-full border px-3 py-1 text-xs font-medium transition-colors ${
                days === d
                  ? 'border-brand-600 bg-brand-50 text-brand-700'
                  : 'border-line text-ink-muted hover:bg-surface-muted'
              }`}
            >
              {d}d
            </button>
          ))}
        </div>
      </PageHeader>

      <ErrorBanner message={error} onRetry={reload} />

      {loading ? (
        <Loading label="Loading usage…" />
      ) : !data ? null : (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatTile
              label="Total cost"
              value={formatUsd(data.totalCostUsd)}
              hint={`last ${days} days`}
            />
            <StatTile label="Calls" value={formatCount(data.totalCalls)} />
            <StatTile label="Input tokens" value={formatCount(data.inputTokens)} />
            <StatTile label="Output tokens" value={formatCount(data.outputTokens)} />
          </div>

          <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-2">
            <Table
              title="By feature"
              head={['Feature', 'Calls', 'Cost']}
              empty="Nothing recorded in this window."
              rows={data.byFeature.map((row) => (
                <tr key={row.feature}>
                  <td className="px-5 py-3 font-medium text-ink">{row.feature}</td>
                  <td className="px-5 py-3 text-right text-ink-muted">{formatCount(row.calls)}</td>
                  <td className="px-5 py-3 text-right text-ink">{formatUsd(row.costUsd)}</td>
                </tr>
              ))}
            />

            <Table
              title="By day"
              head={['Day', 'Calls', 'Cost']}
              empty="Nothing recorded in this window."
              rows={data.byDay.map((row) => (
                <tr key={row.day}>
                  <td className="px-5 py-3 font-medium text-ink">{formatDay(row.day)}</td>
                  <td className="px-5 py-3 text-right text-ink-muted">{formatCount(row.calls)}</td>
                  <td className="px-5 py-3 text-right text-ink">{formatUsd(row.costUsd)}</td>
                </tr>
              ))}
            />
          </div>

          <div className="mt-6">
            <Table
              title="Top users"
              head={['User id', 'Calls', 'Cost']}
              empty="Nothing recorded in this window."
              rows={data.topUsers.map((row) => (
                <tr key={row.userId ?? 'anonymous'}>
                  <td className="px-5 py-3 font-mono text-xs text-ink">
                    {row.userId ?? 'anonymous'}
                  </td>
                  <td className="px-5 py-3 text-right text-ink-muted">{formatCount(row.calls)}</td>
                  <td className="px-5 py-3 text-right text-ink">{formatUsd(row.costUsd)}</td>
                </tr>
              ))}
            />
          </div>
        </>
      )}
    </div>
  );
}
