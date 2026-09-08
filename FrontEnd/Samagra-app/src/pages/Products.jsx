import { useState, useEffect } from 'react';
import api from '../api/axios';

export default function Products() {
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [elapsed, setElapsed] = useState(null);

  const fetchProducts = async () => {
    setLoading(true);
    setError('');
    const start = performance.now();

    try {
      const res = await api.get('/products');
      setProducts(res.data);
      setElapsed(Math.round(performance.now() - start));
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load products');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProducts();
  }, []);

  return (
    <div className="min-h-screen bg-gray-50 p-8">
      <div className="max-w-6xl mx-auto">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h1 className="text-2xl font-semibold text-gray-900">Products</h1>
            {elapsed !== null && (
              <p className="text-sm text-gray-500 mt-1">
                Loaded {products.length} products in{' '}
                <span className="font-mono font-semibold text-gray-900">{elapsed} ms</span>
              </p>
            )}
          </div>
          <button
            onClick={fetchProducts}
            disabled={loading}
            className="px-4 py-2 bg-blue-600 text-white rounded-md text-sm font-medium
                       hover:bg-blue-700 disabled:opacity-50"
          >
            {loading ? 'Loading...' : 'Reload'}
          </button>
        </div>

        {error && (
          <div className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-md px-4 py-3 mb-4">
            {error}
          </div>
        )}

        {loading ? (
          <p className="text-gray-500">Loading...</p>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            {products.map((p) => (
              <div
                key={p.id}
                className="bg-white rounded-lg border border-gray-200 p-4 hover:shadow-sm transition-shadow"
              >
                <div className="text-xs text-gray-500 mb-1">{p.categoryName}</div>
                <h3 className="font-medium text-gray-900 text-sm mb-2 line-clamp-2">
                  {p.name}
                </h3>
                <div className="text-lg font-semibold text-gray-900">
                  ₹{p.price}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}