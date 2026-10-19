// src/pages/admin/AdminProductsPage.jsx

import { useCallback, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  getProductsForManagement,
  deactivateProduct,
  restoreProduct,
} from '../../services/productService';
import { getCategories } from '../../services/categoryService';
import useAsyncData from '../../hooks/useAsyncData';
import {
  Card,
  EmptyState,
  ErrorBanner,
  Loading,
  PageHeader,
  Pagination,
} from '../../components/ui';
import { errorMessage, formatMoney } from '../../lib/format';

const PAGE_SIZE = 20;
const EMPTY_PAGE = { items: [], total: 0, page: 1, pageSize: PAGE_SIZE };

export default function AdminProductsPage() {
  const [filters, setFilters] = useState({ q: '', categoryId: '', includeInactive: true });
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [actionError, setActionError] = useState('');
  const [busyId, setBusyId] = useState(null);

  const fetchProducts = useCallback(
    () => getProductsForManagement({ ...filters, page, pageSize: PAGE_SIZE }),
    [filters, page]
  );

  const { data, loading, error, reload } = useAsyncData(fetchProducts, {
    initialData: EMPTY_PAGE,
    fallbackError: 'Could not load products',
  });

  const fetchCategories = useCallback(
    () => getCategories({ includeInactive: true }).catch(() => []),
    []
  );
  const { data: categories } = useAsyncData(fetchCategories, { initialData: [] });

  const applySearch = (e) => {
    e.preventDefault();
    setPage(1);
    setFilters((f) => ({ ...f, q: search.trim() }));
  };

  const toggleActive = async (product) => {
    setBusyId(product.id);
    setActionError('');
    try {
      if (product.isActive) await deactivateProduct(product.id);
      else await restoreProduct(product.id);
      reload();
    } catch (err) {
      setActionError(errorMessage(err, 'Could not update the product'));
    } finally {
      setBusyId(null);
    }
  };

  const field =
    'rounded-control border border-line px-3 py-2 text-sm text-ink focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-600/20';

  return (
    <div>
      <PageHeader title="Products" subtitle={`${data.total} product(s) in the catalog.`}>
        <Link
          to="/admin/products/new"
          className="rounded-control bg-brand-600 px-4 py-2 text-sm font-medium text-ink-inverse hover:bg-brand-700"
        >
          Add product
        </Link>
      </PageHeader>

      <Card className="mb-4 p-4">
        <form onSubmit={applySearch} className="flex flex-wrap items-center gap-3">
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by name…"
            aria-label="Search products by name"
            className={`${field} min-w-[14rem] flex-1`}
          />

          <select
            value={filters.categoryId}
            onChange={(e) => {
              setPage(1);
              setFilters((f) => ({ ...f, categoryId: e.target.value }));
            }}
            aria-label="Filter by category"
            className={field}
          >
            <option value="">All categories</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>

          <label className="flex cursor-pointer items-center gap-2 text-sm text-ink">
            <input
              type="checkbox"
              checked={filters.includeInactive}
              onChange={(e) => {
                setPage(1);
                setFilters((f) => ({ ...f, includeInactive: e.target.checked }));
              }}
              className="h-4 w-4 accent-brand-600"
            />
            Show deactivated
          </label>

          <button
            type="submit"
            className="rounded-control border border-line px-4 py-2 text-sm text-ink hover:bg-surface-muted"
          >
            Search
          </button>
        </form>
      </Card>

      <ErrorBanner message={error} onRetry={reload} />
      <ErrorBanner message={actionError} />

      {loading ? (
        <Loading label="Loading products…" />
      ) : data.items.length === 0 ? (
        <EmptyState
          title="No products match"
          hint="Try a different search, or clear the category filter."
        />
      ) : (
        <>
          <Card className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b border-line bg-surface-muted text-left text-xs uppercase tracking-wide text-ink-muted">
                <tr>
                  <th className="px-4 py-3 font-medium">Product</th>
                  <th className="px-4 py-3 font-medium">Category</th>
                  <th className="px-4 py-3 text-right font-medium">Price</th>
                  <th className="px-4 py-3 text-right font-medium">Stock</th>
                  <th className="px-4 py-3 font-medium">State</th>
                  <th className="px-4 py-3 text-right font-medium">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {data.items.map((p) => (
                  <tr key={p.id} className={p.isActive ? '' : 'bg-surface-muted/60'}>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <img
                          src={p.imageUrl || '/assets/1.webp'}
                          alt=""
                          className="h-10 w-10 shrink-0 rounded object-cover"
                          onError={(e) => {
                            e.target.src = '/assets/1.webp';
                          }}
                        />
                        <span className="font-medium text-ink">{p.name}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-ink-muted">{p.categoryName}</td>
                    <td className="px-4 py-3 text-right text-ink">{formatMoney(p.price)}</td>
                    <td
                      className={`px-4 py-3 text-right font-medium ${
                        p.stock === 0
                          ? 'text-danger-600'
                          : p.stock <= 10
                            ? 'text-accent-600'
                            : 'text-ink'
                      }`}
                    >
                      {p.stock}
                    </td>
                    <td className="px-4 py-3">
                      {p.isActive ? (
                        <span className="text-xs font-medium text-brand-700">Active</span>
                      ) : (
                        <span className="text-xs font-medium text-ink-muted">Deactivated</span>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-3">
                        <Link
                          to={`/admin/products/${p.id}/edit`}
                          className="font-medium text-brand-600 hover:text-brand-700"
                        >
                          Edit
                        </Link>
                        <button
                          onClick={() => toggleActive(p)}
                          disabled={busyId === p.id}
                          className="font-medium text-ink-muted hover:text-ink disabled:opacity-50"
                        >
                          {busyId === p.id ? '…' : p.isActive ? 'Deactivate' : 'Restore'}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>

          <Pagination
            page={data.page}
            pageSize={data.pageSize}
            total={data.total}
            onPage={setPage}
          />
        </>
      )}
    </div>
  );
}
