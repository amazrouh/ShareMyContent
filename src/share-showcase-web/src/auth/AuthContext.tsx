import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'

const STORAGE_KEY = 'shareshowcase_token'

type AuthContextValue = {
  token: string | null
  setToken: (token: string | null) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setTokenState] = useState<string | null>(null)

  useEffect(() => {
    const t = localStorage.getItem(STORAGE_KEY)
    if (t) setTokenState(t)
  }, [])

  const setToken = useCallback((t: string | null) => {
    setTokenState(t)
    if (t) localStorage.setItem(STORAGE_KEY, t)
    else localStorage.removeItem(STORAGE_KEY)
  }, [])

  const logout = useCallback(() => setToken(null), [setToken])

  const value = useMemo(
    () => ({ token, setToken, logout }),
    [token, setToken, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
