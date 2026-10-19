// src/pages/ProductDetailPage.jsx

import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useCart } from '../context/CartContext';
import api from '../api/axios';

export default function ProductDetailPage() {
  const { id }                  = useParams();
  const navigate                = useNavigate();
  const { addToCart } = useCart();
  const [product, setProduct]   = useState(null);
  const [loading, setLoading]   = useState(true);
  const [error, setError]       = useState('');
  const [added, setAdded]       = useState(false);

  useEffect(() => {
    const fetchProduct = async () => {
      try {
        const res = await api.get(`/products/${id}`);
        setProduct(res.data);
      } catch {
        setError('Product not found');
      } finally {
        setLoading(false);
      }
    };

    fetchProduct();
  }, [id]);

  const handleAddToCart = () => {
    addToCart({
      id:       product.id,
      name:     product.name,
      price:    product.price,
      imageUrl: product.imageUrl
    });
    setAdded(true);
    setTimeout(() => setAdded(false), 2000);
  };

  if (loading) return <p className="p-8 text-ink-muted">Loading...</p>;
  if (error)   return <p className="p-8 text-danger-600">{error}</p>;

  return (
    <div>

      <button
        onClick={() => navigate('/home')}
        className="mb-4 text-sm font-medium text-brand-600 hover:text-brand-700"
      >
        ← Back to Products
      </button>

      {/* Main Content */}
      <div className="max-w-4xl">
        <div className="bg-white rounded-control border border-line p-6 
                        flex flex-col md:flex-row gap-8">

          {/* Left — Image */}
          <div className="w-full md:w-1/2">
            <img
              src={product.imageUrl}
              alt={product.name}
              className="w-full h-80 object-cover rounded-control"
              onError={(e) => { e.target.src = '/assets/1.webp'; }}
            />
          </div>

          {/* Right — Details */}
          <div className="w-full md:w-1/2 flex flex-col justify-between">
            <div>
              <p className="text-xs text-ink-muted mb-1">
                {product.categoryName}
              </p>
              <h1 className="text-2xl font-semibold text-ink mb-3">
                {product.name}
              </h1>
              <p className="text-ink-muted text-sm mb-4">
                {product.description || 'No description available'}
              </p>
              <p className="text-sm text-ink-muted mb-2">
                Stock:{' '}
                <span className="font-medium text-ink">
                  {product.stock}
                </span>
              </p>
              <p className="text-3xl font-bold text-ink mb-6">
                ₹{product.price}
              </p>
            </div>

            {/* Buttons */}
            <div className="flex flex-col gap-3">

              {/* Add to Cart */}
              <button
                onClick={handleAddToCart}
                className={`w-full py-3 font-medium rounded-control transition-colors
                  ${added
                    ? 'bg-brand-600 text-white'
                    : 'bg-brand-600 text-white hover:bg-brand-700'
                  }`}
              >
                {added ? '✓ Added to Cart' : 'Add to Cart'}
              </button>

              {/* Go to Cart */}
              <button
                onClick={() => navigate('/cart')}
                className="w-full py-3 border border-brand-600 text-brand-600 
                           font-medium rounded-control hover:bg-brand-50 transition-colors"
              >
                Go to Cart
              </button>

            </div>
          </div>
        </div>
      </div>

    </div>
  );
}