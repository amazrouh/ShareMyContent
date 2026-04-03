import axios from 'axios'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? ''

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
