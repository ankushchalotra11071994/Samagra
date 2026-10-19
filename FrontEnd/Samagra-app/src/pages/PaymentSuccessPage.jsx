// src/pages/PaymentSuccessPage.jsx
import { useNavigate, useParams } from 'react-router-dom';

export default function PaymentSuccessPage() {
  const { id }   = useParams();
  const navigate = useNavigate();

  return (
    <div className="flex flex-col items-center justify-center">
      <div className="bg-surface rounded-control border border-line p-10 
                      max-w-md w-full text-center">

        {/* Success Icon */}
        <div className="w-16 h-16 bg-brand-100 rounded-full flex items-center 
                        justify-center mx-auto mb-4">
          <span className="text-3xl">✓</span>
        </div>

        <h1 className="text-2xl font-semibold text-ink mb-2">
          Payment Successful!
        </h1>
        <p className="text-ink-muted text-sm mb-2">
          Your order has been placed successfully.
        </p>
        <p className="text-xs text-ink-subtle mb-8 font-mono">
          Payment ID: {id}
        </p>

        <button
          onClick={() => navigate('/home')}
          className="w-full py-3 bg-brand-600 text-ink-inverse font-medium 
                     rounded-control hover:bg-brand-700"
        >
          Continue Shopping
        </button>

      </div>
    </div>
  );
}