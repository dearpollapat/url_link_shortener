import { useLinks } from './hooks/useLinks';
import { CreateLinkForm } from './components/CreateLinkForm';
import { LinkCard } from './components/LinkCard';

export default function App() {
  const { data: links, isLoading, isError } = useLinks();

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100">
      <header className="px-4 pt-10 pb-6 text-center sm:pt-14">
        <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">🔗 Shortly</h1>
        <p className="mt-2 text-slate-500 dark:text-slate-400">
          Turn long URLs into short, trackable links.
        </p>
      </header>

      <main className="mx-auto w-full max-w-2xl px-4 pb-20">
        <CreateLinkForm />

        <section className="mt-8">
          <h2 className="mb-4 flex items-center gap-2 text-lg font-semibold">
            Your links
            {links && links.length > 0 && (
              <span className="rounded-full bg-indigo-600 px-2 py-0.5 text-xs font-semibold text-white">
                {links.length}
              </span>
            )}
          </h2>

          {isLoading && <p className="text-slate-500 dark:text-slate-400">Loading…</p>}

          {isError && (
            <p className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-600 dark:bg-red-950/40 dark:text-red-400">
              Could not reach the API. Is the backend running on :5000?
            </p>
          )}

          {links && links.length === 0 && (
            <p className="rounded-xl border border-dashed border-slate-300 px-4 py-10 text-center text-slate-500 dark:border-slate-700 dark:text-slate-400">
              No links yet — create your first one above.
            </p>
          )}

          <div className="flex flex-col gap-4">
            {links?.map((link) => (
              <LinkCard key={link.shortCode} link={link} />
            ))}
          </div>
        </section>
      </main>

      <footer className="border-t border-slate-200 py-6 text-center text-xs text-slate-400 dark:border-slate-800">
        Full Stack Assignment · .NET + React
      </footer>
    </div>
  );
}
