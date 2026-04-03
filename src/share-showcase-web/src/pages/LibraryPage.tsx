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
              <li key={f.id}>
                <button
                  type="button"
                  onClick={() => setSelectedFolderId(f.id)}
                  className={`w-full rounded-md px-2 py-1.5 text-left text-sm ${
                    selectedFolderId === f.id
                      ? 'bg-indigo-600 text-white'
                      : 'text-slate-300 hover:bg-slate-800'
                  }`}
                >
                  {f.parentFolderId ? f.name : `${f.name} (root)`}
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
                <button
                  type="button"
                  onClick={() => download(f)}
                  className="shrink-0 text-sm text-indigo-400 hover:underline"
                >
                  Download
                </button>
              </li>
            ))}
          </ul>
          {filesQ.data?.length === 0 && selectedFolderId && (
            <p className="mt-2 text-sm text-slate-500">No files yet.</p>
          )}
        </section>
      </div>
    </div>
  )
}
