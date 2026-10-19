// src/pages/HomePage.jsx

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { getAllProducts } from '../services/productService';
import { getCategories } from '../services/categoryService';
import { useCart } from '../context/CartContext';
import useAsyncData from '../hooks/useAsyncData';
import { Card, EmptyState, ErrorBanner, Loading, PageHeader } from '../components/ui';
import { formatMoney } from '../lib/format';

export default function HomePage() {
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('');
  const [addedId, setAddedId] = useState(null);
  const addedTimer = useRef(null);
  const { addToCart } = useCart();

  const fetchProducts = useCallback(() => getAllProducts(), []);
  const {
    data: products,
    loading,
    error,
    reload,
  } = useAsyncData(fetchProducts, {
    initialData: [],
    fallbackError: 'Failed to load products',
  });

  // Filter optional hai — category list na aaye to page phir bhi chalta hai
  const fetchCategories = useCallback(() => getCategories().catch(() => []), []);
  const { data: categories } = useAsyncData(fetchCategories, { initialData: [] });

  useEffect(() => () => clearTimeout(addedTimer.current), []);

  // Catalog ek hi call mein aata hai (100 rows), to filter client par hi theek hai
  const visible = useMemo(() => {
    const term = search.trim().toLowerCase();
    return products.filter(
      (p) =>
        (!category || p.categoryName === category) &&
        (!term || p.name.toLowerCase().includes(term))
    );
  }, [products, search, category]);

  const handleAdd = (product) => {
    addToCart({
      id: product.id,
      name: product.name,
      price: product.price,
      imageUrl: product.imageUrl,
    });
    setAddedId(product.id);
    clearTimeout(addedTimer.current);
    addedTimer.current = setTimeout(() => setAddedId(null), 1600);
  };

  const filtering = Boolean(search.trim() || category);

  const chipClass = (active) =>
    `shrink-0 rounded-full border px-3.5 py-1.5 text-sm font-semibold transition-colors ${
      active
        ? 'border-brand-600 bg-brand-600 text-ink-inverse'
        : 'border-line bg-surface text-ink-muted hover:border-line-strong hover:text-ink'
    }`;

  if (loading) return <Loading label="Loading products…" />;

  return (
    <div>
      <PageHeader
        title="Our Products"
        subtitle={
          filtering
            ? `${visible.length} of ${products.length} products`
            : `${products.length} products available`
        }
      />

      {/* Filters */}
      <div className="mb-6 space-y-3">
        <div className="relative">
          <span className="pointer-events-none absolute inset-y-0 left-0 flex w-11 items-center justify-center text-ink-subtle">
            <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round">
              <circle cx="11" cy="11" r="7" />
              <path d="M20 20l-3.5-3.5" />
            </svg>
          </span>
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search products…"
            aria-label="Search products"
            className="w-full rounded-control border border-line bg-surface py-3 pl-11 pr-10 text-[15px] text-ink placeholder:text-ink-subtle focus:border-brand-600 focus:outline-none"
          />
          {search && (
            <button
              onClick={() => setSearch('')}
              aria-label="Clear search"
              className="absolute inset-y-0 right-0 flex w-10 items-center justify-center text-ink-subtle hover:text-ink"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round">
                <path d="M18 6L6 18M6 6l12 12" />
              </svg>
            </button>
          )}
        </div>

        {categories.length > 0 && (
          <div className="-mx-4 flex gap-2 overflow-x-auto px-4 pb-1 sm:mx-0 sm:flex-wrap sm:px-0 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
            <button onClick={() => setCategory('')} className={chipClass(!category)}>
              All
            </button>
            {categories.map((c) => (
              <button
                key={c.id}
                onClick={() => setCategory(c.name)}
                className={chipClass(category === c.name)}
              >
                {c.name}
              </button>
            ))}
          </div>
        )}
      </div>

      <ErrorBanner message={error} onRetry={reload} />

      {visible.length === 0 ? (
        <EmptyState
          title="Nothing matches that"
          hint="Try a different search term, or pick another category."
        >
          {filtering && (
            <button
              onClick={() => {
                setSearch('');
                setCategory('');
              }}
              className="rounded-control bg-brand-600 px-5 py-2.5 text-sm font-semibold text-ink-inverse transition-colors hover:bg-brand-700"
            >
              Clear filters
            </button>
          )}
        </EmptyState>
      ) : (
        <div className="grid grid-cols-2 gap-3 md:grid-cols-3 sm:gap-5 xl:grid-cols-4">
          {visible.map((p) => {
            const added = addedId === p.id;
            return (
              <Card
                key={p.id}
                className="group flex flex-col overflow-hidden transition-all hover:-translate-y-0.5 hover:border-line-strong hover:shadow-lg hover:shadow-ink/5 motion-reduce:hover:translate-y-0"
              >
                <Link
                  to={`/products/${p.id}`}
                  className="block aspect-square overflow-hidden bg-surface-sunken"
                >
                  <img
                    src={p.imageUrl}
                    alt={p.name}
                    loading="lazy"
                    className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105 motion-reduce:transition-none motion-reduce:group-hover:scale-100"
                    onError={(e) => {
                      e.target.src = '/assets/1.webp'; // fallback
                    }}
                  />
                </Link>

                <div className="flex flex-1 flex-col p-3 sm:p-4">
                  <p className="text-[11px] font-bold uppercase tracking-wider text-accent-600">
                    {p.categoryName}
                  </p>
                  <h3 className="mt-1 line-clamp-2 text-sm font-semibold leading-snug text-ink sm:text-[15px]">
                    <Link to={`/products/${p.id}`} className="hover:text-brand-700">
                      {p.name}
                    </Link>
                  </h3>
                  <p className="mt-2 text-lg font-bold tracking-tight text-ink sm:text-xl">
                    {formatMoney(p.price)}
                  </p>

                  <button
                    onClick={() => handleAdd(p)}
                    className={`mt-3 w-full rounded-control py-2.5 text-sm font-bold transition-colors sm:mt-4 ${
                      added
                        ? 'bg-brand-50 text-brand-700'
                        : 'bg-brand-600 text-ink-inverse hover:bg-brand-700'
                    }`}
                  >
                    {added ? '✓ Added' : 'Add to cart'}
                  </button>
                </div>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
