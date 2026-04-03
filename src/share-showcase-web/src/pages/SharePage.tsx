import { useParams } from 'react-router-dom'

/**
 * Public recipient view (no auth). Token resolution and assets will be wired to the API later.
 */
export function SharePage() {
  const { token } = useParams<{ token: string }>()

  return (
    <div className="min-h-screen bg-gradient-to-b from-indigo-950 to-slate-950 px-4 py-12 text-slate-100">
      <div className="mx-auto max-w-3xl rounded-2xl border border-white/10 bg-white/5 p-8 shadow-xl backdrop-blur">
        <p className="text-sm text-indigo-200">Shared link</p>
        <h1 className="mt-1 text-2xl font-semibold text-white">
          Download page
        </h1>
        <p className="mt-2 text-slate-400">
          Token:{' '}
          <span className="font-mono text-sm text-slate-200">{token}</span>
        </p>
        <p className="mt-6 text-slate-500">
          This route will list shared files and downloads when the share API is
          implemented.
        </p>
      </div>
    </div>
  )
}
