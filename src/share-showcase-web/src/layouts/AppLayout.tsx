import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

const linkClass = ({ isActive }: { isActive: boolean }) =>
  [
    'rounded-md px-3 py-2 text-sm font-medium transition-colors',
    isActive
      ? 'bg-indigo-600 text-white'
      : 'text-slate-300 hover:bg-slate-800 hover:text-white',
  ].join(' ')

export function AppLayout() {
  const { token, logout } = useAuth()

  return (
    <div className="flex min-h-screen flex-col bg-slate-950 text-slate-100 md:flex-row">
      <aside className="border-b border-slate-800 p-4 md:w-56 md:border-b-0 md:border-r">
        <div className="mb-6 text-lg font-semibold tracking-tight text-indigo-400">
          Share Showcase
        </div>
        <nav className="flex flex-col gap-1">
          <NavLink to="/" end className={linkClass}>
            Home
          </NavLink>
          <NavLink to="/library" className={linkClass}>
            Library
          </NavLink>
        </nav>
        <div className="mt-8 border-t border-slate-800 pt-4 text-sm">
          {token ? (
            <button
              type="button"
              onClick={() => logout()}
              className="text-slate-400 hover:text-white"
            >
              Sign out
            </button>
          ) : (
            <div className="flex flex-col gap-2">
              <NavLink
                to="/login"
                className="text-indigo-400 hover:underline"
              >
                Sign in
              </NavLink>
              <NavLink
                to="/register"
                className="text-slate-400 hover:text-white"
              >
                Register
              </NavLink>
            </div>
          )}
        </div>
      </aside>
      <main className="flex-1 p-6 md:p-8">
        <Outlet />
      </main>
    </div>
  )
}
