import { useState } from 'react';
import { QRCodeCanvas } from 'qrcode.react';
import { ApiError, type LinkResponse } from '../api';
import { useDeleteLink, useSetStatus } from '../hooks/useLinks';

interface Props {
  link: LinkResponse;
}

const PLATFORM_LABELS: Record<string, string> = {
  default: 'Default',
  android: 'Android',
  ios: 'iOS',
};

const actionBtn =
  'rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium ' +
  'transition hover:border-indigo-500 disabled:cursor-not-allowed disabled:opacity-60 ' +
  'dark:border-slate-700 dark:bg-slate-800';

export function LinkCard({ link }: Props) {
  const [showQr, setShowQr] = useState(false);
  const [copied, setCopied] = useState(false);

  const setStatus = useSetStatus();
  const deleteLink = useDeleteLink();

  const isActive = link.status === 'Active';
  const busy = setStatus.isPending || deleteLink.isPending;

  const error =
    setStatus.error instanceof ApiError
      ? setStatus.error.message
      : deleteLink.error instanceof ApiError
        ? deleteLink.error.message
        : null;

  async function copy() {
    await navigator.clipboard.writeText(link.shortUrl);
    setCopied(true);
    setTimeout(() => setCopied(false), 1500);
  }

  function toggleStatus() {
    setStatus.mutate({ shortCode: link.shortCode, status: isActive ? 'Disabled' : 'Active' });
  }

  function remove() {
    if (!confirm(`Delete ${link.shortCode}? This cannot be undone.`)) return;
    deleteLink.mutate(link.shortCode);
  }

  const platformDestinations = Object.entries(link.destinations).filter(([p]) => p !== 'default');

  return (
    <article
      className={`flex flex-col gap-3 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm transition dark:border-slate-800 dark:bg-slate-900 sm:p-5 ${
        isActive ? '' : 'opacity-60'
      }`}
    >
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <a
              href={link.shortUrl}
              target="_blank"
              rel="noreferrer"
              className="break-all text-base font-semibold text-indigo-600 hover:underline dark:text-indigo-400"
            >
              {link.shortUrl.replace(/^https?:\/\//, '')}
            </a>
            <span
              className={`rounded-full px-2 py-0.5 text-[0.65rem] font-semibold uppercase tracking-wide ${
                isActive
                  ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-400'
                  : 'bg-slate-200 text-slate-500 dark:bg-slate-800 dark:text-slate-400'
              }`}
            >
              {link.status}
            </span>
          </div>

          <div
            className="mt-1 truncate text-sm text-slate-500 dark:text-slate-400"
            title={link.destinations.default}
          >
            → {link.destinations.default}
          </div>

          {platformDestinations.length > 0 && (
            <ul className="mt-2 flex flex-col gap-1">
              {platformDestinations.map(([platform, dest]) => (
                <li key={platform} className="flex items-center gap-2 text-xs">
                  <span className="shrink-0 rounded-md border border-slate-200 bg-slate-50 px-1.5 py-0.5 font-semibold dark:border-slate-700 dark:bg-slate-800">
                    {PLATFORM_LABELS[platform] ?? platform}
                  </span>
                  <span className="truncate text-slate-500 dark:text-slate-400">{dest}</span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="flex shrink-0 items-baseline gap-2 rounded-xl bg-slate-50 px-3 py-2 dark:bg-slate-800 sm:flex-col sm:items-center sm:gap-0">
          <span className="text-2xl font-bold leading-none">{link.clickCount}</span>
          <span className="text-[0.65rem] uppercase tracking-wider text-slate-400">clicks</span>
        </div>
      </div>

      <dl className="flex gap-6 border-t border-slate-200 pt-3 text-xs dark:border-slate-800">
        <div>
          <dt className="text-slate-400">Created</dt>
          <dd className="mt-0.5">{new Date(link.createdAt).toLocaleString()}</dd>
        </div>
        <div>
          <dt className="text-slate-400">Last visited</dt>
          <dd className="mt-0.5">
            {link.lastAccessedAt ? new Date(link.lastAccessedAt).toLocaleString() : '—'}
          </dd>
        </div>
      </dl>

      {error && (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          {error}
        </p>
      )}

      <div className="flex flex-wrap gap-2">
        <button onClick={copy} className={actionBtn}>
          {copied ? 'Copied ✓' : 'Copy'}
        </button>
        <button onClick={() => setShowQr((v) => !v)} className={actionBtn}>
          {showQr ? 'Hide QR' : 'QR'}
        </button>
        <button onClick={toggleStatus} disabled={busy} className={actionBtn}>
          {isActive ? 'Disable' : 'Enable'}
        </button>
        <button
          onClick={remove}
          disabled={busy}
          className={`${actionBtn} text-red-600 hover:border-red-500 dark:text-red-400`}
        >
          Delete
        </button>
      </div>

      {showQr && (
        <div className="self-center rounded-xl bg-white p-3">
          <QRCodeCanvas value={link.shortUrl} size={140} />
        </div>
      )}
    </article>
  );
}
