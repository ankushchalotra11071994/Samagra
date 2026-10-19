// src/pages/admin/AdminOrdersPage.jsx

import { useCallback, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  ORDER_STATUSES,
  getAllOrders,
  nextStatuses,
  updateOrderStatus,
} from '../../services/orderService';
import useAsyncData from '../../hooks/useAsyncData';
import {
  Card,
  EmptyState,
  ErrorBanner,
  Loading,
  PageHeader,
  Pagination,
  StatusBadge,
} from '../../components/ui';
import { errorMessage, formatDate, formatMoney, shortId } from '../../lib/format';

const PAGE_SIZE = 20;
const EMPTY_PAGE = { items: [], total: 0, page: 1, pageSize: PAGE_SIZE };

export default function AdminOrdersPage() {
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const [busyId, setBusyId] = useState(null);
  const [actionError, setActionError] = useState('');

  const fetchOrders = useCallback(
    () => getAllOrders({ status, page, pageSize: PAGE_SIZE }),
    [status, page]
  );

  const { data, loading, error, reload } = useAsyncData(fetchOrders, {
    initialData: EMPTY_PAGE,
    fallbackError: 'Could not load orders',
  });

  const move = async (order, to) => {
    setBusyId(order.id);
    setActionError('');
    try {
      await updateOrderStatus(order.id, to);
      reload();
    } catch (err) {
      setActionError(errorMessage(err, 'Could not change the status'));
    } finally {
      setBusyId(null);
    }
  };

  return (
    <div>
      <PageHeader title="Orders" subtitle={`${data.total} order(s) placed.`} />

      <Card className="mb-4 p-4">
        <div className="flex flex-wrap items-center gap-2">
          <span className="mr-1 text-sm text-ink-muted">Status</span>
          {['', ...ORDER_STATUSES].map((s) => (
            <button
              key={s || 'ALL'}
              onClick={() => {
                setPage(1);
                setStatus(s);
              }}
              className={`rounded-full border px-3 py-1 text-xs font-medium transition-colors ${
                status === s
                  ? 'border-brand-600 bg-brand-50 text-brand-700'
                  : 'border-line text-ink-muted hover:bg-surface-muted'
              }`}
            >
              {s || 'All'}
            </button>
          ))}
        </div>
      </Card>

      <ErrorBanner message={error} onRetry={reload} />
      <ErrorBanner message={actionError} />

      {loading ? (
        <Loading label="Loading orders…" />
      ) : data.items.length === 0 ? (
        <EmptyState
          title="No orders here"
          hint={status ? `Nothing is currently ${status}.` : 'No orders have been placed yet.'}
        />
      ) : (
        <>
          <Card className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b border-line bg-surface-muted text-left text-xs uppercase tracking-wide text-ink-muted">
                <tr>
                  <th className="px-4 py-3 font-medium">Order</th>
                  <th className="px-4 py-3 font-medium">Placed</th>
                  <th className="px-4 py-3 font-medium">Ship to</th>
                  <th className="px-4 py-3 text-right font-medium">Items</th>
                  <th className="px-4 py-3 text-right font-medium">Total</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  <th className="px-4 py-3 font-medium">Payment</th>
                  <th className="px-4 py-3 text-right font-medium">Move to</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {data.items.map((order) => {
                  const options = nextStatuses(order.status);
                  return (
                    <tr key={order.id}>
                      <td className="px-4 py-3">
                        <Link
                          to={`/admin/orders/${order.id}`}
                          className="font-mono font-medium text-ink hover:text-brand-700"
                        >
                          #{shortId(order.id)}
                        </Link>
                        <p className="max-w-[12rem] truncate font-mono text-xs text-ink-subtle">
                          {order.userId}
                        </p>
                      </td>
                      <td className="px-4 py-3 text-ink-muted">{formatDate(order.createdAt)}</td>
                      <td className="px-4 py-3 text-ink-muted">
                        {order.shipToName ? (
                          <>
                            <span className="block text-ink">{order.shipToName}</span>
                            <span className="text-xs">{order.shipToCity}</span>
                          </>
                        ) : (
                          <span className="text-xs text-ink-subtle">—</span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-right text-ink-muted">{order.itemCount}</td>
                      <td className="px-4 py-3 text-right font-medium text-ink">
                        {formatMoney(order.totalAmount)}
                      </td>
                      <td className="px-4 py-3">
                        <StatusBadge status={order.status} />
                      </td>
                      <td className="px-4 py-3">
                        {order.paymentStatus ? (
                          <StatusBadge status={order.paymentStatus} />
                        ) : (
                          <span className="text-xs text-ink-subtle">none</span>
                        )}
                      </td>
                      <td className="px-4 py-3">
                        {options.length === 0 ? (
                          <span className="block text-right text-xs text-ink-subtle">final</span>
                        ) : (
                          <div className="flex justify-end gap-2">
                            {options.map((to) => (
                              <button
                                key={to}
                                onClick={() => move(order, to)}
                                disabled={busyId === order.id}
                                className={`rounded-control border px-2.5 py-1 text-xs font-medium disabled:opacity-50 ${
                                  to === 'CANCELLED'
                                    ? 'border-danger-200 text-danger-700 hover:bg-danger-50'
                                    : 'border-line text-ink hover:bg-surface-muted'
                                }`}
                              >
                                {to}
                              </button>
                            ))}
                          </div>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </Card>

          <Pagination
            page={data.page}
            pageSize={data.pageSize}
            total={data.total}
            onPage={setPage}
          />
        </>
      )}
    </div>
  );
}
