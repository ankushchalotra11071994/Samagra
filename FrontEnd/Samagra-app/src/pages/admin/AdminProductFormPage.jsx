// src/pages/admin/AdminProductFormPage.jsx — add aur edit dono isi se

import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  createProduct,
  getProductById,
  updateProduct,
} from '../../services/productService';
import { getCategories } from '../../services/categoryService';
import { Card, ErrorBanner, Loading, PageHeader } from '../../components/ui';
import { errorMessage } from '../../lib/format';

const EMPTY = {
  name: '',
  description: '',
  price: '',
  stock: '',
  categoryId: '',
  imageUrl: '',
};

export default function AdminProductFormPage() {
  const { id } = useParams();
  const isEdit = Boolean(id);
  const navigate = useNavigate();

  const [form, setForm] = useState(EMPTY);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;

    (async () => {
      try {
        const [cats, product] = await Promise.all([
          getCategories({ includeInactive: true }),
          isEdit ? getProductById(id) : null,
        ]);
        if (!active) return;

        setCategories(cats);
        if (product) {
          setForm({
            name: product.name ?? '',
            description: product.description ?? '',
            price: String(product.price ?? ''),
            stock: String(product.stock ?? ''),
            categoryId: product.categoryId ?? '',
            imageUrl: product.imageUrl ?? '',
          });
        }
      } catch (err) {
        if (!active) return;
        setError(
          err?.response?.status === 404
            ? 'That product no longer exists, or it has been deactivated.'
            : errorMessage(err, 'Could not load the form')
        );
      } finally {
        if (active) setLoading(false);
      }
    })();

    return () => {
      active = false;
    };
  }, [id, isEdit]);

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    setError('');

    const payload = {
      name: form.name.trim(),
      description: form.description.trim() || null,
      price: Number(form.price),
      stock: Number(form.stock),
      categoryId: form.categoryId,
      imageUrl: form.imageUrl.trim() || null,
    };

    try {
      if (isEdit) await updateProduct(id, payload);
      else await createProduct(payload);
      navigate('/admin/products');
    } catch (err) {
      setError(errorMessage(err, 'Could not save the product'));
      setSaving(false);
    }
  };

  const input =
    'w-full rounded-control border border-line px-3 py-2.5 text-sm text-ink ' +
    'focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-600/20';

  if (loading) return <Loading label="Loading product…" />;

  return (
    <div className="max-w-2xl">
      <Link
        to="/admin/products"
        className="mb-4 inline-block text-sm font-medium text-brand-600 hover:text-brand-700"
      >
        ← Back to products
      </Link>

      <PageHeader
        title={isEdit ? 'Edit product' : 'Add product'}
        subtitle={isEdit ? 'Changes go live as soon as you save.' : 'It appears in the catalog immediately.'}
      />

      <ErrorBanner message={error} />

      <Card className="p-6">
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label htmlFor="name" className="mb-1.5 block text-sm font-medium text-ink">
              Name
            </label>
            <input
              id="name"
              name="name"
              value={form.name}
              onChange={handleChange}
              required
              maxLength={200}
              className={input}
            />
          </div>

          <div>
            <label
              htmlFor="description"
              className="mb-1.5 block text-sm font-medium text-ink"
            >
              Description
            </label>
            <textarea
              id="description"
              name="description"
              value={form.description}
              onChange={handleChange}
              rows={3}
              className={input}
            />
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div>
              <label htmlFor="price" className="mb-1.5 block text-sm font-medium text-ink">
                Price (₹)
              </label>
              <input
                id="price"
                name="price"
                type="number"
                min="0"
                step="0.01"
                value={form.price}
                onChange={handleChange}
                required
                className={input}
              />
            </div>

            <div>
              <label htmlFor="stock" className="mb-1.5 block text-sm font-medium text-ink">
                Stock
              </label>
              <input
                id="stock"
                name="stock"
                type="number"
                min="0"
                step="1"
                value={form.stock}
                onChange={handleChange}
                required
                className={input}
              />
            </div>
          </div>

          <div>
            <label htmlFor="categoryId" className="mb-1.5 block text-sm font-medium text-ink">
              Category
            </label>
            <select
              id="categoryId"
              name="categoryId"
              value={form.categoryId}
              onChange={handleChange}
              required
              className={input}
            >
              <option value="">Choose a category…</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                  {c.isActive ? '' : ' (deactivated)'}
                </option>
              ))}
            </select>
            {categories.length === 0 && (
              <p className="mt-1.5 text-xs text-accent-700">
                No categories yet —{' '}
                <Link to="/admin/categories" className="underline">
                  create one first
                </Link>
                .
              </p>
            )}
          </div>

          <div>
            <label htmlFor="imageUrl" className="mb-1.5 block text-sm font-medium text-ink">
              Image URL
            </label>
            <input
              id="imageUrl"
              name="imageUrl"
              type="url"
              value={form.imageUrl}
              onChange={handleChange}
              placeholder="https://…"
              className={input}
            />
            {form.imageUrl && (
              <img
                src={form.imageUrl}
                alt=""
                className="mt-3 h-24 w-24 rounded border border-line object-cover"
                onError={(e) => {
                  e.target.style.visibility = 'hidden';
                }}
              />
            )}
          </div>

          <div className="flex items-center gap-3 border-t border-line pt-4">
            <button
              type="submit"
              disabled={saving}
              className="rounded-control bg-brand-600 px-5 py-2.5 text-sm font-medium text-ink-inverse hover:bg-brand-700 disabled:opacity-50"
            >
              {saving ? 'Saving…' : isEdit ? 'Save changes' : 'Create product'}
            </button>
            <Link
              to="/admin/products"
              className="rounded-control border border-line px-5 py-2.5 text-sm font-medium text-ink hover:bg-surface-muted"
            >
              Cancel
            </Link>
          </div>
        </form>
      </Card>
    </div>
  );
}
