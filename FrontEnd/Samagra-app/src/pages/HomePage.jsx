// src/pages/HomePage.jsx

import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api/axios';
import { useCart } from '../context/CartContext';
export default function HomePage() {
  const [products, setProducts] = useState([]);
  const [loading, setLoading]   = useState(true);
  const [error, setError]       = useState('');
  const navigate                = useNavigate();
const { totalItems } = useCart();
  useEffect(() => {
    const fetchProducts = async () => {
      try {
        const res = await api.get('/products/hybrid');
        setProducts(res.data);
      } catch (err) {
        setError(err.response?.data?.message || 'Failed to load products');
      } finally {
        setLoading(false);
      }
    };

    fetchProducts();
  }, []);

  if (loading) return <p className="p-8 text-gray-500">Loading...</p>;
  if (error)   return <p className="p-8 text-red-500">{error}</p>;

  return (
    <div className="min-h-screen bg-gray-50 p-8">
      <div className="max-w-6xl mx-auto">

        <div className="flex items-center justify-between mb-6">
  <h1 className="text-2xl font-semibold text-gray-900">
    Our Products
  </h1>
  <button
    onClick={() => navigate('/cart')}
    className="relative px-4 py-2 bg-blue-600 text-white 
               rounded-lg hover:bg-blue-700"
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

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {products.map((p) => (
            <div
              key={p.id}
              className="bg-white rounded-lg border border-gray-200 
                         hover:shadow-md transition-shadow"
            >
              {/* Image */}
              <img
                src={p.imageUrl}
                alt={p.name}
                className="w-full h-48 object-cover rounded-t-lg"
                onError={(e) => {
                  e.target.src = '/assets/1.webp'; // fallback
                }}
              />

              {/* Card Body */}
              <div className="p-4">
                <div className="text-xs text-gray-500 mb-1">
                  {p.categoryName}
                </div>
                <h3 className="font-medium text-gray-900 text-sm mb-2 line-clamp-2">
                  {p.name}
                </h3>
                <div className="text-lg font-semibold text-gray-900 mb-3">
                  ₹{p.price}
                </div>
                <button
                  onClick={() => navigate(`/products/${p.id}`)}
                  className="w-full px-3 py-1.5 bg-blue-600 text-white 
                             text-sm rounded-md hover:bg-blue-700"
                >
                  View Details
                </button>
              </div>

            </div>
          ))}
        </div>

      </div>
    </div>
  );
}