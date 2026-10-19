// src/pages/CartPage.jsx

import { useNavigate } from 'react-router-dom';
import { useCart } from '../context/CartContext';

export default function CartPage() {
  const { cartItems, removeFromCart, updateQuantity, totalAmount } = useCart();
  const navigate = useNavigate();

  if (cartItems.length === 0) {
    return (
      <div>
        <div className="max-w-4xl text-center mt-20">
          <h1 className="text-2xl font-semibold text-ink mb-4">
            Your Cart is Empty
          </h1>
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

  return (
    <div>
      <div className="max-w-4xl">

        {/* Header */}
        <h1 className="text-2xl font-semibold text-ink mb-6">
          Your Cart
        </h1>

        <div className="flex flex-col lg:flex-row gap-6">

          {/* Left — Cart Items */}
          <div className="flex-1 flex flex-col gap-4">
            {cartItems.map((item) => (
              <div
                key={item.id}
                className="bg-surface rounded-control border border-line p-4 
                           flex gap-4 items-center"
              >
                {/* Image */}
                <img
                  src={item.imageUrl}
                  alt={item.name}
                  className="w-20 h-20 object-cover rounded-control"
                />

                {/* Details */}
                <div className="flex-1">
                  <h3 className="font-medium text-ink text-sm">
                    {item.name}
                  </h3>
                  <p className="text-ink-muted text-sm mt-1">
                    ₹{item.price}
                  </p>
                </div>

                {/* Quantity */}
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => updateQuantity(item.id, item.quantity - 1)}
                    className="w-7 h-7 rounded-full border border-line 
                               text-ink-muted hover:bg-surface-muted"
                  >
                    −
                  </button>
                  <span className="text-sm font-medium w-5 text-center">
                    {item.quantity}
                  </span>
                  <button
                    onClick={() => updateQuantity(item.id, item.quantity + 1)}
                    className="w-7 h-7 rounded-full border border-line 
                               text-ink-muted hover:bg-surface-muted"
                  >
                    +
                  </button>
                </div>

                {/* Subtotal */}
                <p className="text-sm font-semibold text-ink w-20 text-right">
                  ₹{item.price * item.quantity}
                </p>

                {/* Remove */}
                <button
                  onClick={() => removeFromCart(item.id)}
                  className="text-danger-600 text-sm hover:underline"
                >
                  Remove
                </button>

              </div>
            ))}
          </div>

          {/* Right — Order Summary */}
          <div className="w-full lg:w-72">
            <div className="bg-surface rounded-control border border-line p-6">
              <h2 className="text-lg font-semibold text-ink mb-4">
                Order Summary
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

              <button
                onClick={() => navigate('/order')}
                className="w-full py-3 bg-brand-600 text-ink-inverse font-medium 
                           rounded-control hover:bg-brand-700"
              >
                Proceed to Order
              </button>

            </div>
          </div>

        </div>
      </div>
    </div>
  );
}