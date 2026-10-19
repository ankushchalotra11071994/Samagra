// src/pages/OrderPage.jsx — checkout: kahan bhejna hai aur kaise pay karna hai

import { useCallback, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useCart } from '../context/CartContext';
import { placeOrder, payForOrder } from '../services/orderService';
import { getAddresses } from '../services/addressService';
import { getCards } from '../services/cardService';
import useAsyncData from '../hooks/useAsyncData';
import { Card, ErrorBanner, Loading, PageHeader } from '../components/ui';
import { errorMessage, formatMoney } from '../lib/format';

export default function OrderPage() {
  const { cartItems, totalAmount, clearCart } = useCart();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [addressId, setAddressId] = useState('');
  const [method, setMethod] = useState('UPI');
  const [cardId, setCardId] = useState('');

  const fetchAddresses = useCallback(() => getAddresses().catch(() => []), []);
  const { data: addresses, loading: loadingAddresses, reload: reloadAddresses } =
    useAsyncData(fetchAddresses, { initialData: [] });

  const fetchCards = useCallback(() => getCards().catch(() => []), []);
  const { data: cards, reload: reloadCards } = useAsyncData(fetchCards, { initialData: [] });

  const usableCards = useMemo(() => cards.filter((c) => !c.expired), [cards]);

  // Default selection derive karte hain, effect se set nahi karte — eslint
  // effect body mein synchronous setState allow nahi karta, aur derive karna
  // zyada seedha bhi hai: list aane par chunaav apne aap bhar jaata hai.
  const pickedAddressId = useMemo(() => {
    if (addressId) return addressId;
    const preferred = addresses.find((a) => a.isDefault) ?? addresses[0];
    return preferred?.id ?? '';
  }, [addressId, addresses]);

  const pickedCardId = useMemo(() => {
    if (cardId) return cardId;
    const preferred = usableCards.find((c) => c.isDefault) ?? usableCards[0];
    return preferred?.id ?? '';
  }, [cardId, usableCards]);

  // Cart empty hai toh home pe bhejo
  if (cartItems.length === 0) {
    return (
      <div>
        <div className="max-w-4xl text-center mt-20">
          <h1 className="text-2xl font-semibold text-ink mb-4">Cart is Empty</h1>
          <button
            onClick={() => navigate('/home')}
            className="px-6 py-2 bg-brand-600 text-ink-inverse rounded-control hover:bg-brand-700"
          >
            Continue Shopping
          </button>
        </div>
      </div>
    );
  }

  const canPlace =
    Boolean(pickedAddressId) && (method !== 'CARD' || Boolean(pickedCardId)) && !loading;

  const handlePlaceOrder = async () => {
    setLoading(true);
    setError('');

    try {
      // Step 1 — Order create karo. userId backend token se leta hai,
      // addressId se wo shipping snapshot bana leta hai.
      const order = await placeOrder(
        cartItems.map((item) => ({
          productId: item.id,
          quantity: item.quantity,
        })),
        pickedAddressId
      );

      // Step 2 — Payment karo
      const payment = await payForOrder(
        order.id,
        method,
        method === 'CARD' ? pickedCardId : null
      );

      // Step 3 — Cart clear karo
      clearCart();

      // Step 4 — Payment page pe jao
      navigate(`/payment-success/${payment.id}`);
    } catch (err) {
      setError(errorMessage(err, 'Order failed, try again'));
      setLoading(false);
    }
  };

  const optionClass = (selected) =>
    `flex cursor-pointer gap-3 rounded-control border p-4 text-left transition-colors ${
      selected
        ? 'border-brand-600 bg-brand-50'
        : 'border-line bg-surface hover:border-line-strong'
    }`;

  return (
    <div>
      <PageHeader title="Checkout" subtitle={`${cartItems.length} item(s) in this order.`} />

      <div className="flex flex-col gap-6 lg:flex-row">
        <div className="flex-1 space-y-6">
          {/* ── Ship to ─────────────────────────────── */}
          <Card className="p-5 sm:p-6">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h2 className="text-base font-bold text-ink">Ship to</h2>
              <Link
                to="/addresses"
                className="text-sm font-semibold text-brand-700 hover:text-brand-800"
              >
                Manage addresses
              </Link>
            </div>

            {loadingAddresses ? (
              <Loading label="Loading addresses…" />
            ) : addresses.length === 0 ? (
              <div className="mt-4 rounded-control border border-accent-200 bg-accent-50 px-4 py-3 text-sm text-accent-700">
                <p className="font-bold">You need a shipping address first.</p>
                <p className="mt-1">
                  We can’t place an order without somewhere to deliver it.{' '}
                  <Link to="/addresses" className="font-bold underline">
                    Add an address
                  </Link>{' '}
                  and come back — your cart is saved.
                </p>
                <button
                  onClick={reloadAddresses}
                  className="mt-2 font-bold underline hover:no-underline"
                >
                  I’ve added one — refresh
                </button>
              </div>
            ) : (
              <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-2">
                {addresses.map((a) => (
                  <label key={a.id} className={optionClass(pickedAddressId === a.id)}>
                    <input
                      type="radio"
                      name="address"
                      value={a.id}
                      checked={pickedAddressId === a.id}
                      onChange={() => setAddressId(a.id)}
                      className="mt-0.5 h-4 w-4 shrink-0 accent-brand-600"
                    />
                    <span className="min-w-0">
                      <span className="flex items-center gap-2">
                        <span className="font-bold text-ink">{a.label}</span>
                        {a.isDefault && (
                          <span className="text-[11px] font-bold uppercase tracking-wide text-brand-700">
                            default
                          </span>
                        )}
                      </span>
                      <span className="mt-1 block text-sm font-semibold text-ink">
                        {a.fullName}
                      </span>
                      <span className="mt-0.5 block text-sm leading-relaxed text-ink-muted">
                        {a.oneLine}
                      </span>
                      <span className="mt-0.5 block text-sm text-ink-muted">{a.phone}</span>
                    </span>
                  </label>
                ))}
              </div>
            )}
          </Card>

          {/* ── Payment ─────────────────────────────── */}
          <Card className="p-5 sm:p-6">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h2 className="text-base font-bold text-ink">Payment</h2>
              <Link to="/cards" className="text-sm font-semibold text-brand-700 hover:text-brand-800">
                Manage cards
              </Link>
            </div>

            <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-2">
              <label className={optionClass(method === 'UPI')}>
                <input
                  type="radio"
                  name="method"
                  value="UPI"
                  checked={method === 'UPI'}
                  onChange={() => setMethod('UPI')}
                  className="mt-0.5 h-4 w-4 shrink-0 accent-brand-600"
                />
                <span>
                  <span className="block font-bold text-ink">UPI</span>
                  <span className="mt-1 block text-sm text-ink-muted">
                    Nothing to save in advance.
                  </span>
                </span>
              </label>

              <label className={optionClass(method === 'CARD')}>
                <input
                  type="radio"
                  name="method"
                  value="CARD"
                  checked={method === 'CARD'}
                  onChange={() => setMethod('CARD')}
                  className="mt-0.5 h-4 w-4 shrink-0 accent-brand-600"
                />
                <span>
                  <span className="block font-bold text-ink">Card</span>
                  <span className="mt-1 block text-sm text-ink-muted">
                    {usableCards.length > 0
                      ? `${usableCards.length} saved`
                      : 'None saved yet'}
                  </span>
                </span>
              </label>
            </div>

            {method === 'CARD' && (
              <div className="mt-4 border-t border-line pt-4">
                {usableCards.length === 0 ? (
                  <div className="rounded-control border border-accent-200 bg-accent-50 px-4 py-3 text-sm text-accent-700">
                    <p className="font-bold">No usable card saved.</p>
                    <p className="mt-1">
                      <Link to="/cards" className="font-bold underline">
                        Add a card
                      </Link>{' '}
                      — only its brand and last 4 digits get stored — or pay by UPI instead.
                    </p>
                    <button
                      onClick={reloadCards}
                      className="mt-2 font-bold underline hover:no-underline"
                    >
                      I’ve added one — refresh
                    </button>
                  </div>
                ) : (
                  <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                    {usableCards.map((c) => (
                      <label key={c.id} className={optionClass(pickedCardId === c.id)}>
                        <input
                          type="radio"
                          name="card"
                          value={c.id}
                          checked={pickedCardId === c.id}
                          onChange={() => setCardId(c.id)}
                          className="mt-0.5 h-4 w-4 shrink-0 accent-brand-600"
                        />
                        <span>
                          <span className="block font-mono font-bold tracking-wider text-ink">
                            {c.brand} ···· {c.last4}
                          </span>
                          <span className="mt-1 block text-sm text-ink-muted">
                            {c.cardholderName} · {String(c.expiryMonth).padStart(2, '0')}/
                            {c.expiryYear}
                          </span>
                        </span>
                      </label>
                    ))}
                  </div>
                )}
              </div>
            )}
          </Card>

          {/* ── Items ───────────────────────────────── */}
          <Card className="divide-y divide-line">
            {cartItems.map((item) => (
              <div key={item.id} className="flex items-center gap-4 p-4">
                <img
                  src={item.imageUrl}
                  alt={item.name}
                  className="h-16 w-16 rounded-control object-cover"
                  onError={(e) => {
                    e.target.src = '/assets/1.webp';
                  }}
                />
                <div className="min-w-0 flex-1">
                  <h3 className="truncate text-sm font-semibold text-ink">{item.name}</h3>
                  <p className="mt-1 text-sm text-ink-muted">Qty: {item.quantity}</p>
                </div>
                <p className="text-sm font-bold text-ink">
                  {formatMoney(item.price * item.quantity)}
                </p>
              </div>
            ))}
          </Card>
        </div>

        {/* ── Summary ──────────────────────────────── */}
        <div className="w-full lg:w-80">
          <Card className="p-6 lg:sticky lg:top-24">
            <h2 className="text-lg font-bold text-ink">Payment Summary</h2>

            <div className="mt-4 flex justify-between text-sm text-ink-muted">
              <span>Subtotal</span>
              <span>{formatMoney(totalAmount)}</span>
            </div>
            <div className="mt-2 flex justify-between text-sm text-ink-muted">
              <span>Delivery</span>
              <span className="font-semibold text-brand-600">Free</span>
            </div>

            <div className="mt-4 flex justify-between border-t border-line pt-4 font-bold text-ink">
              <span>Total</span>
              <span>{formatMoney(totalAmount)}</span>
            </div>

            <ErrorBanner message={error} />

            <button
              onClick={handlePlaceOrder}
              disabled={!canPlace}
              className="mt-4 w-full rounded-control bg-brand-600 py-3 text-sm font-bold text-ink-inverse transition-colors hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {loading ? 'Placing Order…' : 'Place Order & Pay'}
            </button>

            {!pickedAddressId && !loadingAddresses && (
              <p className="mt-2 text-center text-xs text-ink-muted">
                Pick a shipping address to continue.
              </p>
            )}
            {method === 'CARD' && !pickedCardId && (
              <p className="mt-2 text-center text-xs text-ink-muted">
                Pick a card, or switch to UPI.
              </p>
            )}

            <button
              onClick={() => navigate('/cart')}
              className="mt-3 w-full rounded-control border border-line py-3 text-sm font-semibold text-ink transition-colors hover:bg-surface-muted"
            >
              Back to Cart
            </button>
          </Card>
        </div>
      </div>
    </div>
  );
}
