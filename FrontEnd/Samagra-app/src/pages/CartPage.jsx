// src/pages/CartPage.jsx

import { useNavigate } from 'react-router-dom';
import { useCart } from '../context/CartContext';

export default function CartPage() {
  const { cartItems, removeFromCart, updateQuantity, totalAmount } = useCart();
  const navigate = useNavigate();

  if (cartItems.length === 0) {
    return (
      <div className="min-h-screen bg-gray-50 p-8">
        <div className="max-w-4xl mx-auto text-center mt-20">
          <h1 className="text-2xl font-semibold text-gray-900 mb-4">
            Your Cart is Empty
          </h1>
          <button
            onClick={() => navigate('/')}
            className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          >
            Continue Shopping
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-8">
      <div className="max-w-4xl mx-auto">

        {/* Header */}
        <h1 className="text-2xl font-semibold text-gray-900 mb-6">
          Your Cart
        </h1>

        <div className="flex flex-col lg:flex-row gap-6">

          {/* Left — Cart Items */}
          <div className="flex-1 flex flex-col gap-4">
            {cartItems.map((item) => (
              <div
                key={item.id}
                className="bg-white rounded-lg border border-gray-200 p-4 
                           flex gap-4 items-center"
              >
                {/* Image */}
                <img
                  src={item.imageUrl}
                  alt={item.name}
                  className="w-20 h-20 object-cover rounded-lg"
                />

                {/* Details */}
                <div className="flex-1">
                  <h3 className="font-medium text-gray-900 text-sm">
                    {item.name}
                  </h3>
                  <p className="text-gray-500 text-sm mt-1">
                    ₹{item.price}
                  </p>
                </div>

                {/* Quantity */}
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => updateQuantity(item.id, item.quantity - 1)}
                    className="w-7 h-7 rounded-full border border-gray-300 
                               text-gray-600 hover:bg-gray-100"
                  >
                    −
                  </button>
                  <span className="text-sm font-medium w-5 text-center">
                    {item.quantity}
                  </span>
                  <button
                    onClick={() => updateQuantity(item.id, item.quantity + 1)}
                    className="w-7 h-7 rounded-full border border-gray-300 
                               text-gray-600 hover:bg-gray-100"
                  >
                    +
                  </button>
                </div>

                {/* Subtotal */}
                <p className="text-sm font-semibold text-gray-900 w-20 text-right">
                  ₹{item.price * item.quantity}
                </p>

                {/* Remove */}
                <button
                  onClick={() => removeFromCart(item.id)}
                  className="text-red-500 text-sm hover:underline"
                >
                  Remove
                </button>

              </div>
            ))}
          </div>

          {/* Right — Order Summary */}
          <div className="w-full lg:w-72">
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">
                Order Summary
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

              <button
                onClick={() => navigate('/order')}
                className="w-full py-3 bg-blue-600 text-white font-medium 
                           rounded-lg hover:bg-blue-700"
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