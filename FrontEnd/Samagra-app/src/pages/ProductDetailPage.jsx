// src/pages/ProductDetailPage.jsx

import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useCart } from '../context/CartContext';
import api from '../api/axios';

export default function ProductDetailPage() {
  const { id }                  = useParams();
  const navigate                = useNavigate();
  const { addToCart, totalItems } = useCart();
  const [product, setProduct]   = useState(null);
  const [loading, setLoading]   = useState(true);
  const [error, setError]       = useState('');
  const [added, setAdded]       = useState(false);

  useEffect(() => {
    const fetchProduct = async () => {
      try {
        const res = await api.get(`/products/${id}`);
        setProduct(res.data);
      } catch (err) {
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

  if (loading) return <p className="p-8 text-gray-500">Loading...</p>;
  if (error)   return <p className="p-8 text-red-500">{error}</p>;

  return (
    <div className="min-h-screen bg-gray-50">

      {/* Navbar */}
      <div className="bg-white border-b border-gray-200 px-8 py-4 
                      flex items-center justify-between">
        <button
          onClick={() => navigate('/home')}
          className="text-sm text-blue-600 hover:underline"
        >
          ← Back to Products
        </button>

        {/* Cart Icon */}
        <button
          onClick={() => navigate('/cart')}
          className="relative px-4 py-2 bg-blue-600 text-white 
                     rounded-lg hover:bg-blue-700 text-sm"
        >
          Cart
          {totalItems > 0 && (
            <span className="absolute -top-2 -right-2 bg-red-500 text-white 
                             text-xs rounded-full w-5 h-5 flex items-center 
                             justify-center">
              {totalItems}
            </span>
          )}
        </button>
      </div>

      {/* Main Content */}
      <div className="max-w-4xl mx-auto p-8">
        <div className="bg-white rounded-lg border border-gray-200 p-6 
                        flex flex-col md:flex-row gap-8">

          {/* Left — Image */}
          <div className="w-full md:w-1/2">
            <img
              src={product.imageUrl}
              alt={product.name}
              className="w-full h-80 object-cover rounded-lg"
              onError={(e) => { e.target.src = '/assets/1.webp'; }}
            />
          </div>

          {/* Right — Details */}
          <div className="w-full md:w-1/2 flex flex-col justify-between">
            <div>
              <p className="text-xs text-gray-500 mb-1">
                {product.categoryName}
              </p>
              <h1 className="text-2xl font-semibold text-gray-900 mb-3">
                {product.name}
              </h1>
              <p className="text-gray-600 text-sm mb-4">
                {product.description || 'No description available'}
              </p>
              <p className="text-sm text-gray-500 mb-2">
                Stock:{' '}
                <span className="font-medium text-gray-800">
                  {product.stock}
                </span>
              </p>
              <p className="text-3xl font-bold text-gray-900 mb-6">
                ₹{product.price}
              </p>
            </div>

            {/* Buttons */}
            <div className="flex flex-col gap-3">

              {/* Add to Cart */}
              <button
                onClick={handleAddToCart}
                className={`w-full py-3 font-medium rounded-lg transition-colors
                  ${added
                    ? 'bg-green-600 text-white'
                    : 'bg-blue-600 text-white hover:bg-blue-700'
                  }`}
              >
                {added ? '✓ Added to Cart' : 'Add to Cart'}
              </button>

              {/* Go to Cart */}
              <button
                onClick={() => navigate('/cart')}
                className="w-full py-3 border border-blue-600 text-blue-600 
                           font-medium rounded-lg hover:bg-blue-50 transition-colors"
              >
                Go to Cart ({totalItems})
              </button>

            </div>
          </div>
        </div>
      </div>

    </div>
  );
}