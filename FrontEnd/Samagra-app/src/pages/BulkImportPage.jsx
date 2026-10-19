// src/pages/BulkImportPage.jsx

import { useState, useEffect, useRef } from 'react';
import {
  createUploadUrl,
  uploadToBlob,
  startImport,
  getImportJob,
  getRecentImports,
  getImportErrors,
} from '../services/bulkImportService';

const TEMPLATE_CSV =
  'Name,Description,Price,Stock,CategoryName,ImageUrl\n' +
  'Sample Product,Short description,499.00,25,Electronics,https://example.com/img.png\n';

const FINAL_STATUSES = ['Completed', 'Failed'];
const POLL_MS = 3000;

const STATUS_STYLES = {
  Pending:    'bg-surface-muted text-ink',
  Queued:     'bg-yellow-100 text-yellow-800',
  Processing: 'bg-brand-100 text-info-700',
  Completed:  'bg-brand-100 text-brand-800',
  Failed:     'bg-danger-50 text-danger-700',
};

function StatusBadge({ status }) {
  return (
    <span className={`px-2 py-0.5 rounded text-xs font-medium ${STATUS_STYLES[status] || ''}`}>
      {status}
    </span>
  );
}

export default function BulkImportPage() {
  const [file, setFile]           = useState(null);
  const [step, setStep]           = useState('idle'); // idle | uploading | starting
  const [progress, setProgress]   = useState(0);
  const [job, setJob]             = useState(null);
  const [rowErrors, setRowErrors] = useState([]);
  const [recent, setRecent]       = useState([]);
  const [error, setError]         = useState('');
  const inputRef                  = useRef(null);

  // Pehli baar recent imports load karo
  useEffect(() => {
    getRecentImports().then(setRecent).catch(() => {});
  }, []);

  // Job final status tak poll karo
  useEffect(() => {
    if (!job || FINAL_STATUSES.includes(job.status)) return;

    const timer = setTimeout(async () => {
      try {
        const next = await getImportJob(job.id);
        setJob(next);

        if (FINAL_STATUSES.includes(next.status)) {
          getRecentImports().then(setRecent).catch(() => {});
          if (next.failedRows > 0) {
            getImportErrors(next.id).then(setRowErrors).catch(() => {});
          }
        }
      } catch (err) {
        setError(err.response?.data?.message || 'Failed to fetch import status');
      }
    }, POLL_MS);

    return () => clearTimeout(timer);
  }, [job]);

  const handleUpload = async () => {
    if (!file) return;

    setError('');
    setJob(null);
    setRowErrors([]);
    setProgress(0);

    try {
      // Step 1 — SAS URL
      setStep('uploading');
      const { jobId, uploadUrl } = await createUploadUrl(file);

      // Step 2 — Blob pe upload
      await uploadToBlob(uploadUrl, file, setProgress);

      // Step 3 — Service Bus trigger
      setStep('starting');
      const started = await startImport(jobId);

      setStep('idle');
      setJob(started);
      setFile(null);
      if (inputRef.current) inputRef.current.value = '';
    } catch (err) {
      setStep('idle');
      setError(err.response?.data?.message || err.message || 'Upload failed, try again');
    }
  };

  const downloadTemplate = () => {
    const blob = new Blob([TEMPLATE_CSV], { type: 'text/csv' });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href     = url;
    a.download = 'products-template.csv';
    a.click();
    URL.revokeObjectURL(url);
  };

  const busy = step === 'uploading' || step === 'starting';

  return (
    <div>
      <div className="max-w-4xl">

        <div className="flex items-center justify-between mb-6">
          <h1 className="text-2xl font-semibold text-ink">
            Bulk Product Import
          </h1>
          <button
            onClick={downloadTemplate}
            className="text-sm text-brand-600 hover:underline"
          >
            Download CSV template
          </button>
        </div>

        {/* Upload card */}
        <div className="bg-white rounded-control border border-line p-6 mb-6">
          <p className="text-sm text-ink-muted mb-4">
            Columns: <code>Name, Description, Price, Stock, CategoryName, ImageUrl</code>.
            Category must already exist. Max 100 MB.
          </p>

          <div className="flex flex-col sm:flex-row gap-4 sm:items-center">
            <input
              ref={inputRef}
              type="file"
              accept=".csv,text/csv"
              disabled={busy}
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
              className="flex-1 text-sm text-ink file:mr-4 file:py-2 file:px-4
                         file:rounded-control file:border-0 file:bg-surface-muted file:text-ink
                         hover:file:bg-surface-sunken"
            />
            <button
              onClick={handleUpload}
              disabled={!file || busy}
              className="px-6 py-2 bg-brand-600 text-white font-medium rounded-control
                         hover:bg-brand-700 disabled:opacity-50"
            >
              {step === 'uploading' ? 'Uploading...' : step === 'starting' ? 'Starting...' : 'Upload & Import'}
            </button>
          </div>

          {step === 'uploading' && (
            <div className="mt-4">
              <div className="h-2 bg-surface-muted rounded-full overflow-hidden">
                <div
                  className="h-full bg-brand-600 transition-all"
                  style={{ width: `${progress}%` }}
                />
              </div>
              <p className="text-xs text-ink-muted mt-1">{progress}% uploaded</p>
            </div>
          )}

          {error && <p className="text-danger-600 text-sm mt-4">{error}</p>}
        </div>

        {/* Current job */}
        {job && (
          <div className="bg-white rounded-control border border-line p-6 mb-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-ink">{job.fileName}</h2>
              <StatusBadge status={job.status} />
            </div>

            {!FINAL_STATUSES.includes(job.status) && (
              <p className="text-sm text-ink-muted">
                Processing in background — you can leave this page.
              </p>
            )}

            {job.status === 'Completed' && (
              <div className="grid grid-cols-3 gap-4 text-center">
                <div>
                  <p className="text-2xl font-semibold text-ink">{job.totalRows}</p>
                  <p className="text-xs text-ink-muted">Total rows</p>
                </div>
                <div>
                  <p className="text-2xl font-semibold text-brand-600">{job.succeededRows}</p>
                  <p className="text-xs text-ink-muted">Imported</p>
                </div>
                <div>
                  <p className="text-2xl font-semibold text-danger-600">{job.failedRows}</p>
                  <p className="text-xs text-ink-muted">Failed</p>
                </div>
              </div>
            )}

            {job.errorMessage && (
              <p className="text-danger-600 text-sm mt-4">{job.errorMessage}</p>
            )}

            {rowErrors.length > 0 && (
              <div className="mt-6">
                <h3 className="text-sm font-medium text-ink mb-2">
                  Failed rows {job.failedRows > rowErrors.length && `(showing first ${rowErrors.length})`}
                </h3>
                <div className="max-h-64 overflow-y-auto border border-line rounded-control">
                  <table className="w-full text-sm">
                    <thead className="bg-surface-muted sticky top-0">
                      <tr>
                        <th className="text-left px-3 py-2 text-ink-muted font-medium w-20">Row</th>
                        <th className="text-left px-3 py-2 text-ink-muted font-medium">Error</th>
                      </tr>
                    </thead>
                    <tbody>
                      {rowErrors.map((e) => (
                        <tr key={e.rowNumber} className="border-t border-line">
                          <td className="px-3 py-2 text-ink">{e.rowNumber}</td>
                          <td className="px-3 py-2 text-ink">{e.message}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </div>
        )}

        {/* Recent imports */}
        {recent.length > 0 && (
          <div className="bg-white rounded-control border border-line p-6">
            <h2 className="text-lg font-semibold text-ink mb-4">Recent imports</h2>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-ink-muted">
                    <th className="py-2 font-medium">File</th>
                    <th className="py-2 font-medium">Status</th>
                    <th className="py-2 font-medium text-right">Imported</th>
                    <th className="py-2 font-medium text-right">Failed</th>
                    <th className="py-2 font-medium text-right">Date</th>
                  </tr>
                </thead>
                <tbody>
                  {recent.map((r) => (
                    <tr key={r.id} className="border-t border-line">
                      <td className="py-2 text-ink">{r.fileName}</td>
                      <td className="py-2"><StatusBadge status={r.status} /></td>
                      <td className="py-2 text-right text-ink">{r.succeededRows}</td>
                      <td className="py-2 text-right text-ink">{r.failedRows}</td>
                      <td className="py-2 text-right text-ink-muted">
                        {new Date(r.createdAtUtc + 'Z').toLocaleString()}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

      </div>
    </div>
  );
}
