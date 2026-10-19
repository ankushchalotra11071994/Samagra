// src/pages/MyOrdersPage.jsx

import { useCallback } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getMyOrders } from '../services/orderService';
import useAsyncData from '../hooks/useAsyncData';
import {
  Card,
  EmptyState,
  ErrorBanner,
  Loading,
  PageHeader,
  StatusBadge,
} from '../components/ui';
import { formatDate, formatMoney, shortId } from '../lib/format';

export default function MyOrdersPage() {
  const navigate = useNavigate();
  const fetchOrders = useCallback(() => getMyOrders(), []);
  const {
    data: orders,
    loading,
    error,
    reload,
  } = useAsyncData(fetchOrders, {
    initialData: [],
    fallbackError: 'Could not load your orders',
  });

  if (loading) return <Loading label="Loading your orders…" />;

  return (
    <div>
      <PageHeader title="My Orders" subtitle="Every order placed from this account." />

      <ErrorBanner message={error} onRetry={reload} />

      {!error && orders.length === 0 ? (
        <EmptyState
          title="No orders yet"
          hint="Once you place an order it will show up here with its payment and delivery status."
        >
          <button
            onClick={() => navigate('/home')}
            className="rounded-control bg-brand-600 px-5 py-2.5 text-sm font-medium text-ink-inverse hover:bg-brand-700"
          >
            Browse products
          </button>
        </EmptyState>
      ) : (
        <div className="flex flex-col gap-3">
          {orders.map((order) => (
            <Card key={order.id} className="p-4 transition-shadow hover:shadow-md">
              <div className="flex flex-wrap items-center justify-between gap-4">
                <div>
                  <div className="flex items-center gap-3">
                    <span className="font-mono text-sm font-medium text-ink">
                      #{shortId(order.id)}
                    </span>
                    <StatusBadge status={order.status} />
                    {order.paymentStatus && order.paymentStatus !== 'SUCCESS' && (
                      <StatusBadge status={order.paymentStatus} />
                    )}
                  </div>
                  <p className="mt-1 text-sm text-ink-muted">
                    {formatDate(order.createdAt)} · {order.itemCount}{' '}
                    {order.itemCount === 1 ? 'item' : 'items'}
                  </p>
                </div>

                <div className="flex items-center gap-6">
                  <span className="text-lg font-semibold text-ink">
                    {formatMoney(order.totalAmount)}
                  </span>
                  <Link
                    to={`/orders/${order.id}`}
                    className="rounded-control border border-line px-4 py-2 text-sm text-ink hover:bg-surface-muted"
                  >
                    View details
                  </Link>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
