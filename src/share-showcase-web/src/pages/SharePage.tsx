import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { createPublicShareApi } from '../lib/api'

type PublicShareItem = {
  id: string
  originalFileName: string
  contentType: string
  sizeBytes: number
}

type PublicMetadata = {
  targetType: string
  requiresPassword: boolean
  items: PublicShareItem[] | null
}

export function SharePage() {
  const { token } = useParams<{ token: string }>()
  const qc = useQueryClient()
  const [password, setPassword] = useState('')
  const [unlockError, setUnlockError] = useState<string | null>(null)

  const client = useMemo(
    () => (token ? createPublicShareApi(token) : null),
    [token],
  )

  const metadataQ = useQuery({
    queryKey: ['publicShareMetadata', token],
    enabled: Boolean(token && client),
    queryFn: async () => {
      const { data } = await client!.get<PublicMetadata>(
        `/api/v1/public/shares/${encodeURIComponent(token!)}/metadata`,
      )
      return data
    },
  })

  const unlock = useMutation({
    mutationFn: async (pw: string) => {
      const { data } = await client!.post<{ viewerToken: string }>(
        `/api/v1/public/shares/${encodeURIComponent(token!)}/unlock`,
        { password: pw },
      )
      return data
    },
    onSuccess: (data) => {
      if (token) sessionStorage.setItem(`share_viewer_${token}`, data.viewerToken)
      setUnlockError(null)
      setPassword('')
      void qc.invalidateQueries({ queryKey: ['publicShareMetadata', token] })
    },
    onError: () => {
      setUnlockError('Incorrect password.')
    },
  })

  const download = async (file: PublicShareItem) => {
    if (!token || !client) return
    const isFolder = metadataQ.data?.targetType === 'folder'
    const res = await client.get(
      `/api/v1/public/shares/${encodeURIComponent(token)}/download`,
      {
        params: isFolder ? { fileId: file.id } : {},
        responseType: 'blob',
      },
    )
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = file.originalFileName
    a.click()
    URL.revokeObjectURL(url)
  }

  if (!token) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-indigo-950 to-slate-950 px-4 py-12 text-slate-100">
        <p className="text-center text-slate-400">Invalid link.</p>
      </div>
    )
  }

  if (metadataQ.isLoading) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-indigo-950 to-slate-950 px-4 py-12 text-slate-100">
        <p className="text-center text-slate-400">Loading…</p>
      </div>
    )
  }

  if (metadataQ.isError) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-indigo-950 to-slate-950 px-4 py-12 text-slate-100">
        <div className="mx-auto max-w-lg rounded-2xl border border-white/10 bg-white/5 p-8 text-center">
          <h1 className="text-xl font-semibold text-white">Link unavailable</h1>
          <p className="mt-2 text-slate-400">
            This share may have expired or been revoked.
          </p>
        </div>
      </div>
    )
  }

  const meta = metadataQ.data!
  const needsPasswordGate = meta.requiresPassword && meta.items == null

  return (
    <div className="min-h-screen bg-gradient-to-b from-indigo-950 to-slate-950 px-4 py-12 text-slate-100">
      <div className="mx-auto max-w-3xl rounded-2xl border border-white/10 bg-white/5 p-8 shadow-xl backdrop-blur">
        <p className="text-sm text-indigo-200">Shared content</p>
        <h1 className="mt-1 text-2xl font-semibold text-white">
          {meta.targetType === 'folder' ? 'Folder' : 'File'}
        </h1>

        {needsPasswordGate ? (
          <form
            className="mt-6 max-w-sm space-y-3"
            onSubmit={(e) => {
              e.preventDefault()
              unlock.mutate(password)
            }}
          >
            <p className="text-sm text-slate-400">
              This link is password-protected.
            </p>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Password"
              autoComplete="current-password"
              className="w-full rounded-md border border-slate-600 bg-slate-950/80 px-3 py-2 text-sm text-white placeholder:text-slate-500"
            />
            {unlockError && (
              <p className="text-sm text-amber-400">{unlockError}</p>
            )}
            <button
              type="submit"
              disabled={unlock.isPending || !password.trim()}
              className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-50"
            >
              {unlock.isPending ? 'Checking…' : 'Unlock'}
            </button>
          </form>
        ) : (
          <>
            <p className="mt-2 text-sm text-slate-400">
              {meta.items?.length === 1
                ? '1 item'
                : `${meta.items?.length ?? 0} items`}
            </p>
            <ul className="mt-6 space-y-2">
              {meta.items?.map((f) => (
                <li
                  key={f.id}
                  className="flex items-center justify-between gap-3 rounded-lg border border-white/10 px-3 py-3"
                >
                  <span className="min-w-0 truncate text-sm text-slate-200">
                    {f.originalFileName}
                  </span>
                  <button
                    type="button"
                    onClick={() => download(f)}
                    className="shrink-0 text-sm font-medium text-indigo-300 hover:text-indigo-200"
                  >
                    Download
                  </button>
                </li>
              ))}
            </ul>
            {!meta.items?.length && (
              <p className="mt-4 text-sm text-slate-500">No files in this share.</p>
            )}
          </>
        )}
      </div>
    </div>
  )
}
