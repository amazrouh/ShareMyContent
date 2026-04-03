import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { api } from '../lib/api'

type HealthResponse = {
  status: string
  timeUtc: string
}

async function fetchHealth(): Promise<HealthResponse> {
  const { data } = await api.get<HealthResponse>('/api/v1/system/health')
  return data
}

export function HomePage() {
  const health = useQuery({
    queryKey: ['health'],
    queryFn: fetchHealth,
  })

  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="text-2xl font-semibold text-white">
        Simplified Share &amp; Showcase
      </h1>
      <p className="mt-2 text-slate-400">
        Sign in to manage folders and uploads, or{' '}
        <Link className="text-indigo-400 hover:underline" to="/register">
          create an account
        </Link>
        .
      </p>

      <div className="mt-8 rounded-lg border border-slate-800 bg-slate-900/50 p-4">
        <h2 className="text-sm font-medium text-slate-300">API health</h2>
        {health.isLoading && (
          <p className="mt-2 text-sm text-slate-500">Checking&hellip;</p>
        )}
        {health.isError && (
          <p className="mt-2 text-sm text-amber-400">
            Could not reach the API. Run{' '}
            <code className="rounded bg-slate-800 px-1">dotnet run --launch-profile https</code>{' '}
            in <code className="rounded bg-slate-800 px-1">src/ShareShowcase.Api</code>.
          </p>
        )}
        {health.data && (
          <dl className="mt-2 grid gap-1 text-sm text-slate-300">
            <div>
              <dt className="inline text-slate-500">status:</dt>{' '}
              <dd className="inline font-mono">{health.data.status}</dd>
            </div>
            <div>
              <dt className="inline text-slate-500">timeUtc:</dt>{' '}
              <dd className="inline font-mono">{health.data.timeUtc}</dd>
            </div>
          </dl>
        )}
      </div>
    </div>
  )
}
