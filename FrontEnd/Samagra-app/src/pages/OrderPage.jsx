// src/pages/OrderPage.jsx

import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useCart } from '../context/CartContext';
import api from '../api/axios';

export default function OrderPage() {
  const { cartItems, totalAmount, clearCart } = useCart();
  const navigate                              = useNavigate();
  const [loading, setLoading]                 = useState(false);
  const [error, setError]                     = useState('');

  // Cart empty hai toh home pe bhejo
  if (cartItems.length === 0) {
    return (
      <div className="min-h-screen bg-gray-50 p-8">
        <div className="max-w-4xl mx-auto text-center mt-20">
          <h1 className="text-2xl font-semibold text-gray-900 mb-4">
            Cart is Empty
          </h1>
          <button
            onClick={() => navigate('/home')}
            className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
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
      // Step 1 — Order create karo
      const orderRes = await api.post('/orders', {
        userId: 'USR-001', // baad mein auth se lenge
        items: cartItems.map((item) => ({
          productId: item.id,
          quantity:  item.quantity
        }))
      });

      const orderId = orderRes.data.id;

      // Step 2 — Payment karo
      const paymentRes = await api.post('/payments', {
        orderId:       orderId,
        paymentMethod: 'UPI'
      });

      // Step 3 — Cart clear karo
      clearCart();

      // Step 4 — Payment page pe jao
      navigate(`/payment-success/${paymentRes.data.id}`);

    } catch (err) {
      setError(err.response?.data?.message || 'Order failed, try again');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 p-8">
      <div className="max-w-4xl mx-auto">

        {/* Header */}
        <h1 className="text-2xl font-semibold text-gray-900 mb-6">
          Order Summary
        </h1>

        <div className="flex flex-col lg:flex-row gap-6">

          {/* Left — Items */}
          <div className="flex-1 flex flex-col gap-4">
            {cartItems.map((item) => (
              <div
                key={item.id}
                className="bg-white rounded-lg border border-gray-200 p-4 
                           flex gap-4 items-center"
              >
                <img
                  src={item.imageUrl}
                  alt={item.name}
                  className="w-16 h-16 object-cover rounded-lg"
                />
                <div className="flex-1">
                  <h3 className="font-medium text-gray-900 text-sm">
                    {item.name}
                  </h3>
                  <p className="text-gray-500 text-sm mt-1">
                    Qty: {item.quantity}
                  </p>
                </div>
                <p className="text-sm font-semibold text-gray-900">
                  ₹{item.price * item.quantity}
                </p>
              </div>
            ))}
          </div>

          {/* Right — Summary */}
          <div className="w-full lg:w-72">
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">
                Payment Summary
              </h2>

              <div className="flex justify-between text-sm text-gray-600 mb-2">
                <span>Subtotal</span>
                <span>₹{totalAmount}</span>
              </div>
              <div className="flex justify-between text-sm text-gray-600 mb-4">
                <span>Delivery</span>
                <span className="text-green-600">Free</span>
              </div>

              <div className="border-t border-gray-200 pt-4 mb-6">
                <div className="flex justify-between font-semibold text-gray-900">
                  <span>Total</span>
                  <span>₹{totalAmount}</span>
                </div>
              </div>

              {error && (
                <p className="text-red-500 text-sm mb-4">{error}</p>
              )}

              <button
                onClick={handlePlaceOrder}
                disabled={loading}
                className="w-full py-3 bg-blue-600 text-white font-medium 
                           rounded-lg hover:bg-blue-700 disabled:opacity-50"
              >
                {loading ? 'Placing Order...' : 'Place Order & Pay'}
              </button>

              <button
                onClick={() => navigate('/cart')}
                className="w-full mt-3 py-3 border border-gray-300 text-gray-600 
                           font-medium rounded-lg hover:bg-gray-50"
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