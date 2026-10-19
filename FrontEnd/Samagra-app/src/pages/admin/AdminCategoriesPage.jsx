// src/pages/admin/AdminCategoriesPage.jsx

import { useCallback, useState } from 'react';
import {
  createCategory,
  deactivateCategory,
  getCategories,
  updateCategory,
} from '../../services/categoryService';
import useAsyncData from '../../hooks/useAsyncData';
import { Card, EmptyState, ErrorBanner, Loading, PageHeader } from '../../components/ui';
import { errorMessage, formatDate } from '../../lib/format';

export default function AdminCategoriesPage() {
  const [newName, setNewName] = useState('');
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState(null); // { id, name, isActive }
  const [busyId, setBusyId] = useState(null);
  const [actionError, setActionError] = useState('');

  const fetchCategories = useCallback(() => getCategories({ includeInactive: true }), []);
  const {
    data: categories,
    loading,
    error,
    reload,
  } = useAsyncData(fetchCategories, {
    initialData: [],
    fallbackError: 'Could not load categories',
  });

  const handleCreate = async (e) => {
    e.preventDefault();
    const name = newName.trim();
    if (!name) return;

    setCreating(true);
    setActionError('');
    try {
      await createCategory({ name, isActive: true });
      setNewName('');
      reload();
    } catch (err) {
      setActionError(errorMessage(err, 'Could not create the category'));
    } finally {
      setCreating(false);
    }
  };

  const handleRename = async (e) => {
    e.preventDefault();
    const name = editing.name.trim();
    if (!name) return;

    setBusyId(editing.id);
    setActionError('');
    try {
      await updateCategory(editing.id, { name, isActive: editing.isActive });
      setEditing(null);
      reload();
    } catch (err) {
      setActionError(errorMessage(err, 'Could not rename the category'));
    } finally {
      setBusyId(null);
    }
  };

  const toggleActive = async (category) => {
    setBusyId(category.id);
    setActionError('');
    try {
      if (category.isActive) {
        // Backend active products hone par mana kar dega
        await deactivateCategory(category.id);
      } else {
        await updateCategory(category.id, { name: category.name, isActive: true });
      }
      reload();
    } catch (err) {
      setActionError(errorMessage(err, 'Could not update the category'));
    } finally {
      setBusyId(null);
    }
  };

  const field =
    'rounded-control border border-line px-3 py-2 text-sm text-ink focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-600/20';

  return (
    <div className="max-w-3xl">
      <PageHeader title="Categories" subtitle="Every product belongs to exactly one category." />

      <Card className="mb-4 p-4">
        <form onSubmit={handleCreate} className="flex flex-wrap items-center gap-3">
          <input
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            placeholder="New category name…"
            aria-label="New category name"
            maxLength={100}
            className={`${field} flex-1`}
          />
          <button
            type="submit"
            disabled={creating || !newName.trim()}
            className="rounded-control bg-brand-600 px-4 py-2 text-sm font-medium text-white hover:bg-brand-700 disabled:opacity-50"
          >
            {creating ? 'Adding…' : 'Add category'}
          </button>
        </form>
      </Card>

      <ErrorBanner message={error} onRetry={reload} />
      <ErrorBanner message={actionError} />

      {loading ? (
        <Loading label="Loading categories…" />
      ) : categories.length === 0 ? (
        <EmptyState
          title="No categories yet"
          hint="Add one above — products cannot be created without a category."
        />
      ) : (
        <Card className="divide-y divide-line">
          {categories.map((c) => (
            <div key={c.id} className="flex flex-wrap items-center gap-3 px-5 py-4">
              {editing?.id === c.id ? (
                <form onSubmit={handleRename} className="flex flex-1 flex-wrap items-center gap-3">
                  <input
                    value={editing.name}
                    onChange={(e) => setEditing({ ...editing, name: e.target.value })}
                    aria-label={`Rename ${c.name}`}
                    maxLength={100}
                    autoFocus
                    className={`${field} flex-1`}
                  />
                  <button
                    type="submit"
                    disabled={busyId === c.id}
                    className="rounded-control bg-brand-600 px-4 py-2 text-sm font-medium text-white hover:bg-brand-700 disabled:opacity-50"
                  >
                    Save
                  </button>
                  <button
                    type="button"
                    onClick={() => setEditing(null)}
                    className="text-sm text-ink-muted hover:text-ink"
                  >
                    Cancel
                  </button>
                </form>
              ) : (
                <>
                  <div className="min-w-0 flex-1">
                    <p className="font-medium text-ink">
                      {c.name}
                      {!c.isActive && (
                        <span className="ml-2 text-xs font-normal text-ink-muted">
                          (deactivated)
                        </span>
                      )}
                    </p>
                    <p className="text-xs text-ink-muted">
                      {c.productCount} active product{c.productCount === 1 ? '' : 's'} · added{' '}
                      {formatDate(c.createdAt)}
                    </p>
                  </div>

                  <div className="flex items-center gap-4">
                    <button
                      onClick={() => setEditing({ id: c.id, name: c.name, isActive: c.isActive })}
                      className="text-sm font-medium text-brand-600 hover:text-brand-700"
                    >
                      Rename
                    </button>
                    <button
                      onClick={() => toggleActive(c)}
                      disabled={busyId === c.id}
                      className="text-sm font-medium text-ink-muted hover:text-ink disabled:opacity-50"
                    >
                      {busyId === c.id ? '…' : c.isActive ? 'Deactivate' : 'Reactivate'}
                    </button>
                  </div>
                </>
              )}
            </div>
          ))}
        </Card>
      )}
    </div>
  );
}
