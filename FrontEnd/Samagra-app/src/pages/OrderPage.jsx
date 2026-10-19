// src/pages/OrderPage.jsx

import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useCart } from '../context/CartContext';
import { placeOrder, payForOrder } from '../services/orderService';
import { errorMessage } from '../lib/format';

export default function OrderPage() {
  const { cartItems, totalAmount, clearCart } = useCart();
  const navigate                              = useNavigate();
  const [loading, setLoading]                 = useState(false);
  const [error, setError]                     = useState('');

  // Cart empty hai toh home pe bhejo
  if (cartItems.length === 0) {
    return (
      <div>
        <div className="max-w-4xl text-center mt-20">
          <h1 className="text-2xl font-semibold text-ink mb-4">
            Cart is Empty
          </h1>
          <button
            onClick={() => navigate('/home')}
            className="px-6 py-2 bg-brand-600 text-white rounded-control hover:bg-brand-700"
          >
            Continue Shopping
          </button>
        </div>
      </div>
    );
  }

  const handlePlaceOrder = async () => {
    setLoading(true);
    setError('');

    try {
      // Step 1 — Order create karo. userId backend token se leta hai.
      const order = await placeOrder(
        cartItems.map((item) => ({
          productId: item.id,
          quantity:  item.quantity
        }))
      );

      // Step 2 — Payment karo
      const payment = await payForOrder(order.id, 'UPI');

      // Step 3 — Cart clear karo
      clearCart();

      // Step 4 — Payment page pe jao
      navigate(`/payment-success/${payment.id}`);

    } catch (err) {
      setError(errorMessage(err, 'Order failed, try again'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <div className="max-w-4xl">

        {/* Header */}
        <h1 className="text-2xl font-semibold text-ink mb-6">
          Order Summary
        </h1>

        <div className="flex flex-col lg:flex-row gap-6">

          {/* Left — Items */}
          <div className="flex-1 flex flex-col gap-4">
            {cartItems.map((item) => (
              <div
                key={item.id}
                className="bg-white rounded-control border border-line p-4 
                           flex gap-4 items-center"
              >
                <img
                  src={item.imageUrl}
                  alt={item.name}
                  className="w-16 h-16 object-cover rounded-control"
                />
                <div className="flex-1">
                  <h3 className="font-medium text-ink text-sm">
                    {item.name}
                  </h3>
                  <p className="text-ink-muted text-sm mt-1">
                    Qty: {item.quantity}
                  </p>
                </div>
                <p className="text-sm font-semibold text-ink">
                  ₹{item.price * item.quantity}
                </p>
              </div>
            ))}
          </div>

          {/* Right — Summary */}
          <div className="w-full lg:w-72">
            <div className="bg-white rounded-control border border-line p-6">
              <h2 className="text-lg font-semibold text-ink mb-4">
                Payment Summary
              </h2>

              <div className="flex justify-between text-sm text-ink-muted mb-2">
                <span>Subtotal</span>
                <span>₹{totalAmount}</span>
              </div>
              <div className="flex justify-between text-sm text-ink-muted mb-4">
                <span>Delivery</span>
                <span className="text-brand-600">Free</span>
              </div>

              <div className="border-t border-line pt-4 mb-6">
                <div className="flex justify-between font-semibold text-ink">
                  <span>Total</span>
                  <span>₹{totalAmount}</span>
                </div>
              </div>

              {error && (
                <p className="text-danger-600 text-sm mb-4">{error}</p>
              )}

              <button
                onClick={handlePlaceOrder}
                disabled={loading}
                className="w-full py-3 bg-brand-600 text-white font-medium 
                           rounded-control hover:bg-brand-700 disabled:opacity-50"
              >
                {loading ? 'Placing Order...' : 'Place Order & Pay'}
              </button>

              <button
                onClick={() => navigate('/cart')}
                className="w-full mt-3 py-3 border border-line text-ink-muted 
                           font-medium rounded-control hover:bg-surface-muted"
              >
                Back to Cart
              </button>

            </div>
          </div>

        </div>
      </div>
    </div>
  );
}