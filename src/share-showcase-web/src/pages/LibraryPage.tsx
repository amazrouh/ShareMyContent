import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useState } from 'react'
import { api } from '../lib/api'

type FolderDto = {
  id: string
  parentFolderId: string | null
  name: string
  createdAt: string
}

type MediaFileDto = {
  id: string
  folderId: string
  originalFileName: string
  contentType: string
  sizeBytes: number
  createdAt: string
}

type ShareSummaryDto = {
  id: string
  token: string
  targetType: string
  mediaAssetId: string | null
  folderId: string | null
  hasPassword: boolean
  expiresAt: string | null
  revokedAt: string | null
  createdAt: string
}

export function LibraryPage() {
  const qc = useQueryClient()
  const foldersQ = useQuery({
    queryKey: ['folders'],
    queryFn: async () => {
      const { data } = await api.get<FolderDto[]>('/api/v1/folders')
      return data
    },
  })

  const libraryFolder = useMemo(
    () => foldersQ.data?.find((f) => f.parentFolderId === null),
    [foldersQ.data],
  )

  const [selectedFolderId, setSelectedFolderId] = useState<string | null>(null)

  useEffect(() => {
    if (libraryFolder && !selectedFolderId) {
      setSelectedFolderId(libraryFolder.id)
    }
  }, [libraryFolder, selectedFolderId])

  const filesQ = useQuery({
    queryKey: ['files', selectedFolderId],
    enabled: Boolean(selectedFolderId),
    queryFn: async () => {
      const { data } = await api.get<MediaFileDto[]>(
        `/api/v1/folders/${selectedFolderId}/files`,
      )
      return data
    },
  })

  const createFolder = useMutation({
    mutationFn: async (name: string) => {
      if (!libraryFolder) throw new Error('No Library folder')
      await api.post('/api/v1/folders', {
        name,
        parentFolderId: libraryFolder.id,
      })
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['folders'] }),
  })

  const upload = useMutation({
    mutationFn: async (file: File) => {
      if (!selectedFolderId) throw new Error('No folder')
      const fd = new FormData()
      fd.append('file', file)
      await api.post(`/api/v1/folders/${selectedFolderId}/files`, fd, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
    },
    onSuccess: () =>
      qc.invalidateQueries({ queryKey: ['files', selectedFolderId] }),
  })

  const download = async (file: MediaFileDto) => {
    const res = await api.get(`/api/v1/files/${file.id}/download`, {
      responseType: 'blob',
    })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = file.originalFileName
    a.click()
    URL.revokeObjectURL(url)
  }

  const [shareModal, setShareModal] = useState<{
    kind: 'file' | 'folder'
    id: string
    label: string
  } | null>(null)
  const [sharePassword, setSharePassword] = useState('')
  const [shareExpires, setShareExpires] = useState('')
  const [createdLink, setCreatedLink] = useState<string | null>(null)

  const createShare = useMutation({
    mutationFn: async () => {
      if (!shareModal) throw new Error('No target')
      const body: Record<string, string> = {}
      if (shareModal.kind === 'file') body.mediaAssetId = shareModal.id
      else body.folderId = shareModal.id
      const pw = sharePassword.trim()
      if (pw) body.password = pw
      if (shareExpires) {
        const d = new Date(shareExpires)
        if (!Number.isNaN(d.getTime())) body.expiresAt = d.toISOString()
      }
      const { data } = await api.post<ShareSummaryDto>('/api/v1/shares', body)
      return data
    },
    onSuccess: (data) => {
      const url = `${window.location.origin}/s/${data.token}`
      setCreatedLink(url)
    },
  })

  const closeShareModal = () => {
    setShareModal(null)
    setSharePassword('')
    setShareExpires('')
    setCreatedLink(null)
    createShare.reset()
  }

  return (
    <div className="mx-auto max-w-4xl">
      <h1 className="text-2xl font-semibold text-white">Library</h1>

      <div className="mt-6 grid gap-6 md:grid-cols-2">
        <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
          <h2 className="text-sm font-medium text-slate-300">Folders</h2>
          {foldersQ.isLoading && (
            <p className="mt-2 text-sm text-slate-500">Loading…</p>
          )}
          {foldersQ.error && (
            <p className="mt-2 text-sm text-amber-400">Could not load folders.</p>
          )}
          <ul className="mt-2 space-y-1">
            {foldersQ.data?.map((f) => (
              <li
                key={f.id}
                className="flex items-center gap-1 rounded-md hover:bg-slate-800/80"
              >
                <button
                  type="button"
                  onClick={() => setSelectedFolderId(f.id)}
                  className={`min-w-0 flex-1 rounded-md px-2 py-1.5 text-left text-sm ${
                    selectedFolderId === f.id
                      ? 'bg-indigo-600 text-white'
                      : 'text-slate-300 hover:bg-slate-800'
                  }`}
                >
                  {f.parentFolderId ? f.name : `${f.name} (root)`}
                </button>
                <button
                  type="button"
                  title="Share this folder"
                  onClick={() =>
                    setShareModal({
                      kind: 'folder',
                      id: f.id,
                      label: f.parentFolderId ? f.name : `${f.name} (root)`,
                    })
                  }
                  className="shrink-0 rounded px-1.5 py-1 text-xs text-indigo-400 hover:bg-slate-700 hover:text-indigo-300"
                >
                  Share
                </button>
              </li>
            ))}
          </ul>
          <form
            className="mt-4 flex gap-2"
            onSubmit={(e) => {
              e.preventDefault()
              const fd = new FormData(e.currentTarget)
              const name = String(fd.get('name') ?? '').trim()
              if (!name) return
              createFolder.mutate(name)
              e.currentTarget.reset()
            }}
          >
            <input
              name="name"
              placeholder="New subfolder"
              className="flex-1 rounded-md border border-slate-700 bg-slate-950 px-2 py-1.5 text-sm text-white"
            />
            <button
              type="submit"
              disabled={createFolder.isPending || !libraryFolder}
              className="rounded-md bg-slate-700 px-3 py-1.5 text-sm text-white hover:bg-slate-600 disabled:opacity-50"
            >
              Add
            </button>
          </form>
        </section>

        <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
          <div className="flex items-center justify-between gap-2">
            <h2 className="text-sm font-medium text-slate-300">Files</h2>
            <label className="cursor-pointer rounded-md bg-indigo-600 px-3 py-1.5 text-sm text-white hover:bg-indigo-500">
              Upload
              <input
                type="file"
                className="hidden"
                onChange={(e) => {
                  const f = e.target.files?.[0]
                  if (f) upload.mutate(f)
                  e.target.value = ''
                }}
              />
            </label>
          </div>
          {!selectedFolderId && (
            <p className="mt-2 text-sm text-slate-500">Select a folder.</p>
          )}
          {filesQ.isLoading && (
            <p className="mt-2 text-sm text-slate-500">Loading…</p>
          )}
          <ul className="mt-2 space-y-2">
            {filesQ.data?.map((f) => (
              <li
                key={f.id}
                className="flex items-center justify-between gap-2 rounded-md border border-slate-800 px-2 py-2"
              >
                <span className="truncate text-sm text-slate-200">
                  {f.originalFileName}
                </span>
                <span className="flex shrink-0 items-center gap-2">
                  <button
                    type="button"
                    onClick={() =>
                      setShareModal({
                        kind: 'file',
                        id: f.id,
                        label: f.originalFileName,
                      })
                    }
                    className="text-sm text-indigo-400 hover:underline"
                  >
                    Share
                  </button>
                  <button
                    type="button"
                    onClick={() => download(f)}
                    className="text-sm text-indigo-400 hover:underline"
                  >
                    Download
                  </button>
                </span>
              </li>
            ))}
          </ul>
          {filesQ.data?.length === 0 && selectedFolderId && (
            <p className="mt-2 text-sm text-slate-500">No files yet.</p>
          )}
        </section>
      </div>

      {shareModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
          role="dialog"
          aria-modal="true"
          aria-labelledby="share-modal-title"
        >
          <div className="w-full max-w-md rounded-xl border border-slate-700 bg-slate-900 p-6 shadow-xl">
            <h2
              id="share-modal-title"
              className="text-lg font-semibold text-white"
            >
              Share {shareModal.kind === 'file' ? 'file' : 'folder'}
            </h2>
            <p className="mt-1 truncate text-sm text-slate-400">{shareModal.label}</p>

            {!createdLink ? (
              <form
                className="mt-4 space-y-3"
                onSubmit={(e) => {
                  e.preventDefault()
                  createShare.mutate()
                }}
              >
                <label className="block text-sm text-slate-300">
                  Optional password
                  <input
                    type="password"
                    value={sharePassword}
                    onChange={(e) => setSharePassword(e.target.value)}
                    autoComplete="new-password"
                    className="mt-1 w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
                    placeholder="Leave empty for public link"
                  />
                </label>
                <label className="block text-sm text-slate-300">
                  Optional expiry (local time)
                  <input
                    type="datetime-local"
                    value={shareExpires}
                    onChange={(e) => setShareExpires(e.target.value)}
                    className="mt-1 w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
                  />
                </label>
                {createShare.isError && (
                  <p className="text-sm text-amber-400">
                    Could not create share. Check inputs and try again.
                  </p>
                )}
                <div className="flex justify-end gap-2 pt-2">
                  <button
                    type="button"
                    onClick={closeShareModal}
                    className="rounded-md px-4 py-2 text-sm text-slate-300 hover:bg-slate-800"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={createShare.isPending}
                    className="rounded-md bg-indigo-600 px-4 py-2 text-sm text-white hover:bg-indigo-500 disabled:opacity-50"
                  >
                    {createShare.isPending ? 'Creating…' : 'Create link'}
                  </button>
                </div>
              </form>
            ) : (
              <div className="mt-4 space-y-3">
                <p className="text-sm text-slate-300">Copy this link:</p>
                <div className="flex gap-2">
                  <input
                    readOnly
                    value={createdLink}
                    className="min-w-0 flex-1 rounded-md border border-slate-700 bg-slate-950 px-3 py-2 font-mono text-xs text-slate-200"
                  />
                  <button
                    type="button"
                    onClick={() => {
                      void navigator.clipboard.writeText(createdLink)
                    }}
                    className="shrink-0 rounded-md bg-slate-700 px-3 py-2 text-sm text-white hover:bg-slate-600"
                  >
                    Copy
                  </button>
                </div>
                <button
                  type="button"
                  onClick={closeShareModal}
                  className="w-full rounded-md bg-indigo-600 py-2 text-sm text-white hover:bg-indigo-500"
                >
                  Done
                </button>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
