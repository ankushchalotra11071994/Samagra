// src/pages/admin/AdminDashboardPage.jsx

import { useCallback } from 'react';
import { Link } from 'react-router-dom';
import { getDashboard } from '../../services/adminService';
import useAsyncData from '../../hooks/useAsyncData';
import {
  Card,
  ErrorBanner,
  Loading,
  PageHeader,
  StatTile,
  StatusBadge,
} from '../../components/ui';
import { formatDate, formatMoney, shortId } from '../../lib/format';

export default function AdminDashboardPage() {
  const fetchDashboard = useCallback(() => getDashboard(), []);
  const { data, loading, error, reload } = useAsyncData(fetchDashboard, {
    fallbackError: 'Could not load the dashboard',
  });

  if (loading) return <Loading label="Loading dashboard…" />;

  if (!data) return <ErrorBanner message={error} onRetry={reload} />;

  return (
    <div>
      <PageHeader title="Dashboard" subtitle="Catalog and order health at a glance.">
        <button
          onClick={reload}
          className="rounded-control border border-line px-4 py-2 text-sm text-ink hover:bg-surface-muted"
        >
          Refresh
        </button>
      </PageHeader>

      <ErrorBanner message={error} onRetry={reload} />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatTile
          label="Revenue (all time)"
          value={formatMoney(data.revenue.allTime)}
          hint={`${formatMoney(data.revenue.lastSevenDays)} in the last 7 days`}
        />
        <StatTile
          label="Orders"
          value={data.orders.total}
          hint={`${data.categoryCount} categories`}
        />
        <StatTile
          label="Active products"
          value={data.products.active}
          hint={`${data.products.total} total incl. deactivated`}
        />
        <StatTile
          label="Low stock"
          value={data.products.lowStock}
          hint={`at or below ${data.lowStockThreshold} units · ${data.products.outOfStock} out of stock`}
          tone={data.products.lowStock > 0 ? 'text-accent-600' : 'text-ink'}
        />
      </div>

      <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-5">
        <Card className="lg:col-span-2">
          <div className="border-b border-line px-5 py-4">
            <h2 className="font-semibold text-ink">Orders by status</h2>
          </div>
          <ul className="divide-y divide-line">
            {data.orders.byStatus.map((row) => (
              <li key={row.status} className="flex items-center justify-between px-5 py-3">
                <StatusBadge status={row.status} />
                <span className="text-sm font-medium text-ink">{row.count}</span>
              </li>
            ))}
          </ul>
          <div className="border-t border-line px-5 py-3">
            <Link
              to="/admin/orders"
              className="text-sm font-medium text-brand-600 hover:text-brand-700"
            >
              Manage orders →
            </Link>
          </div>
        </Card>

        <Card className="lg:col-span-3">
          <div className="border-b border-line px-5 py-4">
            <h2 className="font-semibold text-ink">Recent orders</h2>
          </div>
          {data.recentOrders.length === 0 ? (
            <p className="px-5 py-8 text-center text-sm text-ink-muted">No orders yet.</p>
          ) : (
            <ul className="divide-y divide-line">
              {data.recentOrders.map((order) => (
                <li key={order.id} className="flex items-center justify-between gap-4 px-5 py-3">
                  <div className="min-w-0">
                    <Link
                      to={`/admin/orders/${order.id}`}
                      className="font-mono text-sm font-medium text-ink hover:text-brand-700"
                    >
                      #{shortId(order.id)}
                    </Link>
                    <p className="text-xs text-ink-muted">{formatDate(order.createdAt)}</p>
                  </div>
                  <div className="flex shrink-0 items-center gap-3">
                    <StatusBadge status={order.status} />
                    <span className="text-sm font-medium text-ink">
                      {formatMoney(order.totalAmount)}
                    </span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      <Card className="mt-6">
        <div className="flex items-center justify-between border-b border-line px-5 py-4">
          <h2 className="font-semibold text-ink">Needs restocking</h2>
          <Link
            to="/admin/products"
            className="text-sm font-medium text-brand-600 hover:text-brand-700"
          >
            All products →
          </Link>
        </div>
        {data.lowStockProducts.length === 0 ? (
          <p className="px-5 py-8 text-center text-sm text-ink-muted">
            Every active product is above the threshold.
          </p>
        ) : (
          <ul className="divide-y divide-line">
            {data.lowStockProducts.map((product) => (
              <li key={product.id} className="flex items-center justify-between gap-4 px-5 py-3">
                <div className="min-w-0">
                  <Link
                    to={`/admin/products/${product.id}/edit`}
                    className="truncate text-sm font-medium text-ink hover:text-brand-700"
                  >
                    {product.name}
                  </Link>
                  <p className="text-xs text-ink-muted">{product.categoryName}</p>
                </div>
                <span
                  className={`shrink-0 text-sm font-semibold ${
                    product.stock === 0 ? 'text-danger-600' : 'text-accent-600'
                  }`}
                >
                  {product.stock} left
                </span>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}
