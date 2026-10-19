// src/pages/OrderDetailPage.jsx — buyer ka apna order

import { useCallback, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { getOrderById, payForOrder } from '../services/orderService';
import useAsyncData from '../hooks/useAsyncData';
import {
  Card,
  ErrorBanner,
  Loading,
  PageHeader,
  StatusBadge,
} from '../components/ui';
import { errorMessage, formatDate, formatMoney, shortId } from '../lib/format';

export default function OrderDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [paying, setPaying] = useState(false);
  const [payError, setPayError] = useState('');

  const fetchOrder = useCallback(async () => {
    try {
      return await getOrderById(id);
    } catch (err) {
      if (err?.response?.status === 404)
        throw new Error('That order does not exist, or it belongs to another account.', {
          cause: err,
        });
      throw err;
    }
  }, [id]);

  const {
    data: order,
    loading,
    error,
    reload,
  } = useAsyncData(fetchOrder, { fallbackError: 'Could not load this order' });

  // Checkout ke beech payment fail ho gaya ho to yahan se dobara try kar sakte hain
  const handlePay = async () => {
    setPaying(true);
    setPayError('');
    try {
      const payment = await payForOrder(order.id);
      navigate(`/payment-success/${payment.id}`);
    } catch (err) {
      setPayError(errorMessage(err, 'Payment failed'));
      setPaying(false);
    }
  };

  if (loading) return <Loading label="Loading order…" />;

  if (!order) {
    return (
      <div>
        <ErrorBanner message={error} onRetry={reload} />
        <Link to="/orders" className="text-sm font-medium text-brand-600 hover:text-brand-700">
          ← Back to my orders
        </Link>
      </div>
    );
  }

  const unpaid = order.status === 'PENDING';

  return (
    <div>
      <Link
        to="/orders"
        className="mb-4 inline-block text-sm font-medium text-brand-600 hover:text-brand-700"
      >
        ← Back to my orders
      </Link>

      <PageHeader
        title={`Order #${shortId(order.id)}`}
        subtitle={`Placed ${formatDate(order.createdAt)}`}
      >
        <StatusBadge status={order.status} />
      </PageHeader>

      <ErrorBanner message={payError} />

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
                    <div className="flex items-center gap-3">
                      {item.imageUrl && (
                        <img
                          src={item.imageUrl}
                          alt=""
                          className="h-10 w-10 rounded object-cover"
                          onError={(e) => {
                            e.target.style.visibility = 'hidden';
                          }}
                        />
                      )}
                      <Link
                        to={`/products/${item.productId}`}
                        className="font-medium text-ink hover:text-brand-700"
                      >
                        {item.productName}
                      </Link>
                    </div>
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
          </table>
        </Card>

        <div className="w-full lg:w-80">
          <Card className="p-6">
            <h2 className="text-lg font-semibold text-ink">Summary</h2>

            <div className="mt-4 flex justify-between text-sm text-ink-muted">
              <span>Items total</span>
              <span>{formatMoney(order.totalAmount)}</span>
            </div>
            <div className="mt-2 flex justify-between text-sm text-ink-muted">
              <span>Delivery</span>
              <span className="text-brand-600">Free</span>
            </div>

            <div className="mt-4 flex justify-between border-t border-line pt-4 font-semibold text-ink">
              <span>Total</span>
              <span>{formatMoney(order.totalAmount)}</span>
            </div>


            <div className="mt-6 border-t border-line pt-4">
              <h3 className="text-sm font-bold text-ink">Shipping to</h3>
              {order.shippingAddress ? (
                <address className="mt-2 text-sm not-italic leading-relaxed text-ink-muted">
                  <span className="block font-semibold text-ink">
                    {order.shippingAddress.name}
                  </span>
                  {order.shippingAddress.line1}
                  {order.shippingAddress.line2 ? `, ${order.shippingAddress.line2}` : ''}
                  <br />
                  {order.shippingAddress.city}, {order.shippingAddress.state}{' '}
                  {order.shippingAddress.postalCode}
                  <br />
                  {order.shippingAddress.country}
                  <br />
                  {order.shippingAddress.phone}
                </address>
              ) : (
                <p className="mt-2 text-sm text-ink-muted">
                  No address recorded — this order predates shipping addresses.
                </p>
              )}
            </div>

            <div className="mt-6 border-t border-line pt-4">
              <h3 className="text-sm font-bold text-ink">Payment</h3>
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
                    <dd>
                      {order.payment.paymentMethod}
                      {order.payment.cardLast4 && ` ···· ${order.payment.cardLast4}`}
                    </dd>
                  </div>
                  <div className="flex justify-between">
                    <dt>Paid on</dt>
                    <dd>{formatDate(order.payment.createdAt)}</dd>
                  </div>
                </dl>
              ) : (
                <p className="mt-2 text-sm text-ink-muted">No payment recorded yet.</p>
              )}
            </div>

            {unpaid && (
              <button
                onClick={handlePay}
                disabled={paying}
                className="mt-6 w-full rounded-control bg-brand-600 py-3 text-sm font-medium text-ink-inverse hover:bg-brand-700 disabled:opacity-50"
              >
                {paying ? 'Processing…' : 'Pay now'}
              </button>
            )}
          </Card>
        </div>
      </div>
    </div>
  );
}
