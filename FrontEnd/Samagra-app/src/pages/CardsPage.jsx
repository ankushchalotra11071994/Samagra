// src/pages/CardsPage.jsx

import { useCallback, useState } from 'react';
import {
  createCard,
  deleteCard,
  digitsOnly,
  expiryYears,
  formatCardNumber,
  getCards,
  guessBrand,
  makeCardDefault,
} from '../services/cardService';
import useAsyncData from '../hooks/useAsyncData';
import { Card, EmptyState, ErrorBanner, Loading, PageHeader } from '../components/ui';
import { errorMessage } from '../lib/format';

const MONTHS = Array.from({ length: 12 }, (_, i) => i + 1);

const field =
  'w-full rounded-control border border-line bg-surface px-3 py-2.5 text-[15px] text-ink ' +
  'placeholder:text-ink-subtle focus:border-brand-600 focus:outline-none';

const EMPTY = {
  number: '',
  cardholderName: '',
  expiryMonth: new Date().getMonth() + 1,
  expiryYear: new Date().getFullYear(),
  isDefault: false,
};

export default function CardsPage() {
  const [form, setForm] = useState(null);
  const [saving, setSaving] = useState(false);
  const [busyId, setBusyId] = useState(null);
  const [actionError, setActionError] = useState('');

  const fetchCards = useCallback(() => getCards(), []);
  const {
    data: cards,
    loading,
    error,
    reload,
  } = useAsyncData(fetchCards, {
    initialData: [],
    fallbackError: 'Could not load your cards',
  });

  const openNew = () => {
    setActionError('');
    setForm({ ...EMPTY, isDefault: cards.length === 0 });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    setActionError('');

    try {
      // Sirf digits jaate hain; response mein bhi number wapas nahi aata
      await createCard({
        number: digitsOnly(form.number),
        cardholderName: form.cardholderName,
        expiryMonth: Number(form.expiryMonth),
        expiryYear: Number(form.expiryYear),
        isDefault: form.isDefault,
      });
      setForm(null);
      reload();
    } catch (err) {
      setActionError(errorMessage(err, 'Could not save the card'));
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

  const brand = form ? guessBrand(form.number) : '';

  if (loading) return <Loading label="Loading your cards…" />;

  return (
    <div className="max-w-3xl">
      <PageHeader title="Payment cards" subtitle="Pick one of these at checkout.">
        {!form && (
          <button
            onClick={openNew}
            className="rounded-control bg-brand-600 px-4 py-2 text-sm font-bold text-ink-inverse transition-colors hover:bg-brand-700"
          >
            Add card
          </button>
        )}
      </PageHeader>

      <div className="mb-6 rounded-control border border-accent-200 bg-accent-50 px-4 py-3 text-sm text-accent-700">
        <strong className="font-bold">Only the last 4 digits are saved.</strong> The number you
        type is checked and then discarded — the full number is never written to the database, and
        there is no CVV field because nothing here can use one.
      </div>

      <ErrorBanner message={error} onRetry={reload} />
      <ErrorBanner message={actionError} />

      {form && (
        <Card className="mb-6 p-5 sm:p-6">
          <h2 className="text-base font-bold text-ink">New card</h2>

          <form onSubmit={handleSubmit} className="mt-4 space-y-4">
            <label className="block">
              <span className="mb-1.5 block text-sm font-semibold text-ink">Card number</span>
              <div className="relative">
                <input
                  value={formatCardNumber(form.number)}
                  onChange={(e) => setForm((f) => ({ ...f, number: digitsOnly(e.target.value) }))}
                  required
                  inputMode="numeric"
                  autoComplete="cc-number"
                  placeholder="4242 4242 4242 4242"
                  className={`${field} pr-24 font-mono tracking-wider`}
                />
                {brand && (
                  <span className="absolute inset-y-0 right-3 flex items-center text-sm font-bold text-brand-700">
                    {brand}
                  </span>
                )}
              </div>
            </label>

            <label className="block">
              <span className="mb-1.5 block text-sm font-semibold text-ink">Name on card</span>
              <input
                value={form.cardholderName}
                onChange={(e) => setForm((f) => ({ ...f, cardholderName: e.target.value }))}
                required
                maxLength={150}
                autoComplete="cc-name"
                className={field}
              />
            </label>

            <div className="grid grid-cols-2 gap-4">
              <label className="block">
                <span className="mb-1.5 block text-sm font-semibold text-ink">Expiry month</span>
                <select
                  value={form.expiryMonth}
                  onChange={(e) => setForm((f) => ({ ...f, expiryMonth: e.target.value }))}
                  autoComplete="cc-exp-month"
                  className={field}
                >
                  {MONTHS.map((m) => (
                    <option key={m} value={m}>
                      {String(m).padStart(2, '0')}
                    </option>
                  ))}
                </select>
              </label>

              <label className="block">
                <span className="mb-1.5 block text-sm font-semibold text-ink">Expiry year</span>
                <select
                  value={form.expiryYear}
                  onChange={(e) => setForm((f) => ({ ...f, expiryYear: e.target.value }))}
                  autoComplete="cc-exp-year"
                  className={field}
                >
                  {expiryYears().map((y) => (
                    <option key={y} value={y}>
                      {y}
                    </option>
                  ))}
                </select>
              </label>
            </div>

            <label className="flex cursor-pointer items-center gap-2.5">
              <input
                type="checkbox"
                checked={form.isDefault}
                onChange={(e) => setForm((f) => ({ ...f, isDefault: e.target.checked }))}
                className="h-4 w-4 accent-brand-600"
              />
              <span className="text-sm text-ink">Use this card by default</span>
            </label>

            <div className="flex flex-wrap gap-3 border-t border-line pt-4">
              <button
                type="submit"
                disabled={saving}
                className="rounded-control bg-brand-600 px-5 py-2.5 text-sm font-bold text-ink-inverse transition-colors hover:bg-brand-700 disabled:opacity-50"
              >
                {saving ? 'Saving…' : 'Add card'}
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

      {cards.length === 0 && !form ? (
        <EmptyState
          title="No cards saved"
          hint="Add one to pay by card at checkout. You can also pay by UPI without saving anything."
        >
          <button
            onClick={openNew}
            className="rounded-control bg-brand-600 px-5 py-2.5 text-sm font-bold text-ink-inverse hover:bg-brand-700"
          >
            Add card
          </button>
        </EmptyState>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {cards.map((c) => (
            <Card key={c.id} className={`p-5 ${c.expired ? 'opacity-70' : ''}`}>
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="font-mono text-base font-bold tracking-wider text-ink">
                    {c.brand} ···· {c.last4}
                  </p>
                  <p className="mt-1 text-sm text-ink-muted">{c.cardholderName}</p>
                  <p className="mt-0.5 text-sm text-ink-muted">
                    Expires {String(c.expiryMonth).padStart(2, '0')}/{c.expiryYear}
                    {c.expired && (
                      <span className="ml-2 font-bold text-danger-600">expired</span>
                    )}
                  </p>
                </div>
                {c.isDefault && (
                  <span className="shrink-0 rounded-full border border-brand-200 bg-brand-50 px-2.5 py-0.5 text-[11px] font-bold uppercase tracking-wide text-brand-700">
                    Default
                  </span>
                )}
              </div>

              <div className="mt-4 flex flex-wrap items-center gap-4 border-t border-line pt-4 text-sm">
                {!c.isDefault && !c.expired && (
                  <button
                    onClick={() =>
                      run(c.id, () => makeCardDefault(c.id), 'Could not set the default')
                    }
                    disabled={busyId === c.id}
                    className="font-semibold text-ink-muted hover:text-ink disabled:opacity-50"
                  >
                    Make default
                  </button>
                )}
                <button
                  onClick={() => run(c.id, () => deleteCard(c.id), 'Could not remove the card')}
                  disabled={busyId === c.id}
                  className="ml-auto font-semibold text-danger-600 hover:text-danger-700 disabled:opacity-50"
                >
                  {busyId === c.id ? '…' : 'Remove'}
                </button>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
