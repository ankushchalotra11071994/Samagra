// src/pages/PaymentSuccessPage.jsx
import AiChat from '../components/AiChat';
import { useNavigate, useParams } from 'react-router-dom';

export default function PaymentSuccessPage() {
  const { id }   = useParams();
  const navigate = useNavigate();

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center">
      <div className="bg-white rounded-lg border border-gray-200 p-10 
                      max-w-md w-full text-center">

        {/* Success Icon */}
        <div className="w-16 h-16 bg-green-100 rounded-full flex items-center 
                        justify-center mx-auto mb-4">
          <span className="text-3xl">✓</span>
        </div>

        <h1 className="text-2xl font-semibold text-gray-900 mb-2">
          Payment Successful!
        </h1>
        <p className="text-gray-500 text-sm mb-2">
          Your order has been placed successfully.
        </p>
        <p className="text-xs text-gray-400 mb-8 font-mono">
          Payment ID: {id}
        </p>

        <button
          onClick={() => navigate('/home')}
          className="w-full py-3 bg-blue-600 text-white font-medium 
                     rounded-lg hover:bg-blue-700"
        >
          Continue Shopping
        </button>

      </div>
          <AiChat />   
    </div>
  );
}