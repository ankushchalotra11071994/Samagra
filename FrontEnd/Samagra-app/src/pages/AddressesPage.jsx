// src/pages/AddressesPage.jsx

import { useCallback, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  EMPTY_ADDRESS,
  createAddress,
  deleteAddress,
  getAddresses,
  makeAddressDefault,
  updateAddress,
} from '../services/addressService';
import useAsyncData from '../hooks/useAsyncData';
import { Card, EmptyState, ErrorBanner, Loading, PageHeader } from '../components/ui';
import { errorMessage } from '../lib/format';

const field =
  'w-full rounded-control border border-line bg-surface px-3 py-2.5 text-[15px] text-ink ' +
  'placeholder:text-ink-subtle focus:border-brand-600 focus:outline-none';

export default function AddressesPage() {
  const [form, setForm] = useState(null); // null = form band; {id?} = khula
  const [saving, setSaving] = useState(false);
  const [busyId, setBusyId] = useState(null);
  const [actionError, setActionError] = useState('');

  const fetchAddresses = useCallback(() => getAddresses(), []);
  const {
    data: addresses,
    loading,
    error,
    reload,
  } = useAsyncData(fetchAddresses, {
    initialData: [],
    fallbackError: 'Could not load your addresses',
  });

  const openNew = () => {
    setActionError('');
    setForm({ ...EMPTY_ADDRESS, isDefault: addresses.length === 0 });
  };

  const openEdit = (a) => {
    setActionError('');
    setForm({
      id: a.id,
      label: a.label,
      fullName: a.fullName,
      phone: a.phone,
      line1: a.line1,
      line2: a.line2 ?? '',
      city: a.city,
      state: a.state,
      postalCode: a.postalCode,
      country: a.country,
      isDefault: a.isDefault,
    });
  };

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setForm((f) => ({ ...f, [name]: type === 'checkbox' ? checked : value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    setActionError('');

    const { id, ...payload } = form;
    payload.line2 = payload.line2.trim() || null;

    try {
      if (id) await updateAddress(id, payload);
      else await createAddress(payload);
      setForm(null);
      reload();
    } catch (err) {
      setActionError(errorMessage(err, 'Could not save the address'));
    } finally {
      setSaving(false);
    }
  };

  const run = async (id, action, fallback) => {
    setBusyId(id);
    setActionError('');
    try {
      await action();
      reload();
    } catch (err) {
      setActionError(errorMessage(err, fallback));
    } finally {
      setBusyId(null);
    }
  };

  if (loading) return <Loading label="Loading your addresses…" />;

  return (
    <div className="max-w-3xl">
      <PageHeader
        title="Shipping addresses"
        subtitle="Orders are delivered to the address you pick at checkout."
      >
        {!form && (
          <button
            onClick={openNew}
            className="rounded-control bg-brand-600 px-4 py-2 text-sm font-bold text-ink-inverse transition-colors hover:bg-brand-700"
          >
            Add address
          </button>
        )}
      </PageHeader>

      <ErrorBanner message={error} onRetry={reload} />
      <ErrorBanner message={actionError} />

      {form && (
        <Card className="mb-6 p-5 sm:p-6">
          <h2 className="text-base font-bold text-ink">
            {form.id ? 'Edit address' : 'New address'}
          </h2>

          <form onSubmit={handleSubmit} className="mt-4 space-y-4">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <label className="block">
                <span className="mb-1.5 block text-sm font-semibold text-ink">Label</span>
                <input
                  name="label"
                  value={form.label}
                  onChange={handleChange}
                  required
                  maxLength={50}
                  placeholder="Home, Warehouse…"
                  className={field}
                />
              </label>

              <label className="block">
                <span className="mb-1.5 block text-sm font-semibold text-ink">Full name</span>
                <input
                  name="fullName"
                  value={form.fullName}
                  onChange={handleChange}
                  required
                  maxLength={150}
                  autoComplete="name"
                  className={field}
                />
              </label>
            </div>

            <label className="block">
              <span className="mb-1.5 block text-sm font-semibold text-ink">Phone</span>
              <input
                name="phone"
                type="tel"
                value={form.phone}
                onChange={handleChange}
                required
                maxLength={20}
                autoComplete="tel"
                className={field}
              />
            </label>

            <label className="block">
              <span className="mb-1.5 block text-sm font-semibold text-ink">Address line 1</span>
              <input
                name="line1"
                value={form.line1}
                onChange={handleChange}
                required
                maxLength={200}
                autoComplete="address-line1"
                className={field}
              />
            </label>

            <label className="block">
              <span className="mb-1.5 block text-sm font-semibold text-ink">
                Address line 2 <span className="font-normal text-ink-subtle">(optional)</span>
              </span>
              <input
                name="line2"
                value={form.line2}
                onChange={handleChange}
                maxLength={200}
                autoComplete="address-line2"
                className={field}
              />
            </label>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
              <label className="block">
                <span className="mb-1.5 block text-sm font-semibold text-ink">City</span>
                <input
                  name="city"
                  value={form.city}
                  onChange={handleChange}
                  required
                  maxLength={100}
                  autoComplete="address-level2"
                  className={field}
                />
              </label>

              <label className="block">
                <span className="mb-1.5 block text-sm font-semibold text-ink">State</span>
                <input
                  name="state"
                  value={form.state}
                  onChange={handleChange}
                  required
                  maxLength={100}
                  autoComplete="address-level1"
                  className={field}
                />
              </label>

              <label className="block">
                <span className="mb-1.5 block text-sm font-semibold text-ink">PIN code</span>
                <input
                  name="postalCode"
                  value={form.postalCode}
                  onChange={handleChange}
                  required
                  maxLength={20}
                  autoComplete="postal-code"
                  className={field}
                />
              </label>
            </div>

            <label className="block">
              <span className="mb-1.5 block text-sm font-semibold text-ink">Country</span>
              <input
                name="country"
                value={form.country}
                onChange={handleChange}
                required
                maxLength={100}
                autoComplete="country-name"
                className={field}
              />
            </label>

            <label className="flex cursor-pointer items-center gap-2.5">
              <input
                type="checkbox"
                name="isDefault"
                checked={form.isDefault}
                onChange={handleChange}
                className="h-4 w-4 accent-brand-600"
              />
              <span className="text-sm text-ink">Use this as my default address</span>
            </label>

            <div className="flex flex-wrap gap-3 border-t border-line pt-4">
              <button
                type="submit"
                disabled={saving}
                className="rounded-control bg-brand-600 px-5 py-2.5 text-sm font-bold text-ink-inverse transition-colors hover:bg-brand-700 disabled:opacity-50"
              >
                {saving ? 'Saving…' : form.id ? 'Save changes' : 'Add address'}
              </button>
              <button
                type="button"
                onClick={() => setForm(null)}
                className="rounded-control border border-line px-5 py-2.5 text-sm font-semibold text-ink transition-colors hover:bg-surface-muted"
              >
                Cancel
              </button>
            </div>
          </form>
        </Card>
      )}

      {addresses.length === 0 && !form ? (
        <EmptyState
          title="No addresses yet"
          hint="Add one so your orders have somewhere to go — checkout needs it."
        >
          <button
            onClick={openNew}
            className="rounded-control bg-brand-600 px-5 py-2.5 text-sm font-bold text-ink-inverse hover:bg-brand-700"
          >
            Add address
          </button>
        </EmptyState>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {addresses.map((a) => (
            <Card key={a.id} className="flex flex-col p-5">
              <div className="flex items-start justify-between gap-3">
                <p className="font-bold text-ink">{a.label}</p>
                {a.isDefault && (
                  <span className="shrink-0 rounded-full border border-brand-200 bg-brand-50 px-2.5 py-0.5 text-[11px] font-bold uppercase tracking-wide text-brand-700">
                    Default
                  </span>
                )}
              </div>

              <p className="mt-2 text-sm font-semibold text-ink">{a.fullName}</p>
              <p className="mt-1 text-sm leading-relaxed text-ink-muted">{a.oneLine}</p>
              <p className="mt-1 text-sm text-ink-muted">{a.phone}</p>

              <div className="mt-4 flex flex-wrap items-center gap-4 border-t border-line pt-4 text-sm">
                <button
                  onClick={() => openEdit(a)}
                  className="font-semibold text-brand-700 hover:text-brand-800"
                >
                  Edit
                </button>
                {!a.isDefault && (
                  <button
                    onClick={() =>
                      run(a.id, () => makeAddressDefault(a.id), 'Could not set the default')
                    }
                    disabled={busyId === a.id}
                    className="font-semibold text-ink-muted hover:text-ink disabled:opacity-50"
                  >
                    Make default
                  </button>
                )}
                <button
                  onClick={() =>
                    run(a.id, () => deleteAddress(a.id), 'Could not remove the address')
                  }
                  disabled={busyId === a.id}
                  className="ml-auto font-semibold text-danger-600 hover:text-danger-700 disabled:opacity-50"
                >
                  {busyId === a.id ? '…' : 'Remove'}
                </button>
              </div>
            </Card>
          ))}
        </div>
      )}

      <p className="mt-6 text-sm text-ink-muted">
        Removing an address doesn’t change orders already placed — each order keeps its own copy
        of where it was shipped. <Link to="/orders" className="font-semibold text-brand-700 hover:text-brand-800">See your orders</Link>.
      </p>
    </div>
  );
}
