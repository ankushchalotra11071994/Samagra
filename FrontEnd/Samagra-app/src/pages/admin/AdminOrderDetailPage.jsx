// src/pages/admin/AdminOrderDetailPage.jsx
// Buyer wale detail page se alag: yahan kisi bhi user ka order khulta hai
// aur status badla ja sakta hai.

import { useCallback, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { getOrderById, nextStatuses, updateOrderStatus } from '../../services/orderService';
import useAsyncData from '../../hooks/useAsyncData';
import { Card, ErrorBanner, Loading, PageHeader, StatusBadge } from '../../components/ui';
import { errorMessage, formatDate, formatMoney, shortId } from '../../lib/format';

export default function AdminOrderDetailPage() {
  const { id } = useParams();
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState('');

  const fetchOrder = useCallback(async () => {
    try {
      return await getOrderById(id);
    } catch (err) {
      if (err?.response?.status === 404)
        throw new Error('No order with that id.', { cause: err });
      throw err;
    }
  }, [id]);

  const {
    data: order,
    loading,
    error,
    reload,
  } = useAsyncData(fetchOrder, { fallbackError: 'Could not load this order' });

  const move = async (to) => {
    setBusy(true);
    setActionError('');
    try {
      await updateOrderStatus(id, to);
      reload();
    } catch (err) {
      setActionError(errorMessage(err, 'Could not change the status'));
    } finally {
      setBusy(false);
    }
  };

  if (loading) return <Loading label="Loading order…" />;

  if (!order) {
    return (
      <div>
        <ErrorBanner message={error} onRetry={reload} />
        <Link to="/admin/orders" className="text-sm font-medium text-brand-600 hover:text-brand-700">
          ← Back to orders
        </Link>
      </div>
    );
  }

  const options = nextStatuses(order.status);

  return (
    <div>
      <Link
        to="/admin/orders"
        className="mb-4 inline-block text-sm font-medium text-brand-600 hover:text-brand-700"
      >
        ← Back to orders
      </Link>

      <PageHeader
        title={`Order #${shortId(order.id)}`}
        subtitle={`Placed ${formatDate(order.createdAt)}`}
      >
        <StatusBadge status={order.status} />
      </PageHeader>

      <ErrorBanner message={actionError} />

      <div className="flex flex-col gap-6 lg:flex-row">
        <Card className="flex-1 overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b border-line bg-surface-muted text-left text-xs uppercase tracking-wide text-ink-muted">
              <tr>
                <th className="px-4 py-3 font-medium">Product</th>
                <th className="px-4 py-3 text-right font-medium">Unit price</th>
                <th className="px-4 py-3 text-right font-medium">Qty</th>
                <th className="px-4 py-3 text-right font-medium">Subtotal</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-line">
              {order.items.map((item) => (
                <tr key={item.productId}>
                  <td className="px-4 py-3">
                    <Link
                      to={`/admin/products/${item.productId}/edit`}
                      className="font-medium text-ink hover:text-brand-700"
                    >
                      {item.productName}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-right text-ink-muted">
                    {formatMoney(item.unitPrice)}
                  </td>
                  <td className="px-4 py-3 text-right text-ink-muted">{item.quantity}</td>
                  <td className="px-4 py-3 text-right font-medium text-ink">
                    {formatMoney(item.subTotal)}
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot className="border-t border-line bg-surface-muted">
              <tr>
                <td colSpan={3} className="px-4 py-3 text-right font-medium text-ink">
                  Order total
                </td>
                <td className="px-4 py-3 text-right font-semibold text-ink">
                  {formatMoney(order.totalAmount)}
                </td>
              </tr>
            </tfoot>
          </table>
        </Card>

        <div className="w-full lg:w-80">
          <Card className="p-6">
            <h2 className="text-sm font-medium text-ink">Buyer</h2>
            <p className="mt-1 break-all font-mono text-xs text-ink-muted">{order.userId}</p>

            <h2 className="mt-5 text-sm font-medium text-ink">Payment</h2>
            {order.payment ? (
              <dl className="mt-2 space-y-1.5 text-sm text-ink-muted">
                <div className="flex justify-between">
                  <dt>Status</dt>
                  <dd>
                    <StatusBadge status={order.payment.status} />
                  </dd>
                </div>
                <div className="flex justify-between">
                  <dt>Method</dt>
                  <dd>{order.payment.paymentMethod}</dd>
                </div>
                <div className="flex justify-between">
                  <dt>Amount</dt>
                  <dd>{formatMoney(order.payment.amount)}</dd>
                </div>
                <div className="flex justify-between">
                  <dt>Taken on</dt>
                  <dd>{formatDate(order.payment.createdAt)}</dd>
                </div>
              </dl>
            ) : (
              <p className="mt-2 text-sm text-ink-muted">Not paid yet.</p>
            )}

            <h2 className="mt-5 border-t border-line pt-5 text-sm font-medium text-ink">
              Move this order
            </h2>
            {options.length === 0 ? (
              <p className="mt-2 text-sm text-ink-muted">
                {order.status} is a final state — nothing left to do.
              </p>
            ) : (
              <div className="mt-3 flex flex-col gap-2">
                {options.map((to) => (
                  <button
                    key={to}
                    onClick={() => move(to)}
                    disabled={busy}
                    className={`rounded-control px-4 py-2.5 text-sm font-medium disabled:opacity-50 ${
                      to === 'CANCELLED'
                        ? 'border border-danger-200 text-danger-700 hover:bg-danger-50'
                        : 'bg-brand-600 text-white hover:bg-brand-700'
                    }`}
                  >
                    {busy ? 'Working…' : `Mark as ${to}`}
                  </button>
                ))}
              </div>
            )}
          </Card>
        </div>
      </div>
    </div>
  );
}
