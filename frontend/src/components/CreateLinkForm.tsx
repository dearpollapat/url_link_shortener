import { useState, type FormEvent } from 'react';
import { ApiError, type CreateLinkRequest } from '../api';
import { useCreateLink } from '../hooks/useLinks';

const inputClass =
  'w-full rounded-lg border border-slate-300 bg-slate-50 px-3 py-2.5 text-base ' +
  'text-slate-900 outline-none transition focus:border-brand-green focus:ring-2 ' +
  'focus:ring-brand-green/30 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100';

const labelClass = 'flex flex-col gap-1.5 text-sm font-medium';

export function CreateLinkForm() {
  const [url, setUrl] = useState('');
  const [customAlias, setCustomAlias] = useState('');
  const [androidUrl, setAndroidUrl] = useState('');
  const [iosUrl, setIosUrl] = useState('');
  const [showPlatforms, setShowPlatforms] = useState(false);

  const createLink = useCreateLink();

  function handleSubmit(event: FormEvent) {
    event.preventDefault();

    const destinations: Record<string, string> = {};
    if (androidUrl.trim()) destinations.android = androidUrl.trim();
    if (iosUrl.trim()) destinations.ios = iosUrl.trim();

    const body: CreateLinkRequest = { url: url.trim() };
    if (customAlias.trim()) body.customAlias = customAlias.trim();
    if (Object.keys(destinations).length > 0) body.destinations = destinations;

    createLink.mutate(body, {
      onSuccess: () => {
        setUrl('');
        setCustomAlias('');
        setAndroidUrl('');
        setIosUrl('');
        setShowPlatforms(false);
      },
    });
  }

  const errorMessage =
    createLink.error instanceof ApiError
      ? createLink.error.message
      : createLink.isError
        ? 'Something went wrong.'
        : null;

  return (
    <form
      onSubmit={handleSubmit}
      className="flex flex-col gap-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900 sm:p-6"
    >
      <label className={labelClass}>
        <span>Long URL</span>
        <input
          type="url"
          required
          placeholder="https://www.example.com/very/long/path"
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          className={inputClass}
        />
      </label>

      <label className={labelClass}>
        <span>
          Custom alias <span className="font-normal text-slate-400">(optional)</span>
        </span>
        <input
          type="text"
          placeholder="my-link"
          value={customAlias}
          onChange={(e) => setCustomAlias(e.target.value)}
          className={inputClass}
        />
      </label>

      <button
        type="button"
        onClick={() => setShowPlatforms((v) => !v)}
        className="self-start text-sm font-medium text-brand-green hover:underline"
      >
        {showPlatforms ? '− Hide' : '+ Add'} platform-specific destinations
      </button>

      {showPlatforms && (
        <div className="flex flex-col gap-3 rounded-xl border border-dashed border-slate-300 p-4 dark:border-slate-700">
          <label className={labelClass}>
            <span>Android destination</span>
            <input
              type="url"
              placeholder="https://example.com/app.apk"
              value={androidUrl}
              onChange={(e) => setAndroidUrl(e.target.value)}
              className={inputClass}
            />
          </label>
          <label className={labelClass}>
            <span>iOS destination</span>
            <input
              type="url"
              placeholder="https://example.com/app.ipa"
              value={iosUrl}
              onChange={(e) => setIosUrl(e.target.value)}
              className={inputClass}
            />
          </label>
        </div>
      )}

      {errorMessage && (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          {errorMessage}
        </p>
      )}

      <button
        type="submit"
        disabled={createLink.isPending}
        className="rounded-lg bg-brand-green px-4 py-3 font-semibold text-white transition hover:bg-brand-green-dark disabled:cursor-not-allowed disabled:opacity-60"
      >
        {createLink.isPending ? 'Shortening…' : 'Shorten'}
      </button>
    </form>
  );
}
