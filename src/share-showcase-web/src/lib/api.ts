import axios from 'axios'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? ''

/** Must match `PublicSharesController.ShareViewerHeaderName` on the API. */
export const shareViewerHeaderName = 'X-Share-Viewer'

export const api = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
})

api.interceptors.request.use((config) => {
  const t = localStorage.getItem('shareshowcase_token')
  if (t) {
    config.headers.Authorization = `Bearer ${t}`
  }
  return config
})

/** Anonymous client for `/api/v1/public/shares/...`; sends viewer JWT from sessionStorage when set. */
export function createPublicShareApi(shareToken: string) {
  const instance = axios.create({
    baseURL,
    headers: {
      'Content-Type': 'application/json',
    },
  })
  instance.interceptors.request.use((config) => {
    const v = sessionStorage.getItem(`share_viewer_${shareToken}`)
    if (v) {
      config.headers[shareViewerHeaderName] = v
    }
    return config
  })
  return instance
}
