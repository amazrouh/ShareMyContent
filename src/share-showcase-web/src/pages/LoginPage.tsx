import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Link, useNavigate, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { api } from '../lib/api'

const schema = z.object({
  email: z.string().email(),
  password: z.string().min(1, 'Required'),
})

type Form = z.infer<typeof schema>

export function LoginPage() {
  const { setToken } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? '/library'

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setError: setFormError,
  } = useForm<Form>({ resolver: zodResolver(schema) })

  const onSubmit = async (data: Form) => {
    try {
      const { data: res } = await api.post<{
        token: string
        email: string
        userId: string
      }>('/api/v1/auth/login', data)
      setToken(res.token)
      navigate(from, { replace: true })
    } catch {
      setFormError('root', { message: 'Invalid email or password.' })
    }
  }

  return (
    <div className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-4">
      <h1 className="text-2xl font-semibold text-white">Sign in</h1>
      <p className="mt-1 text-sm text-slate-400">
        New here?{' '}
        <Link className="text-indigo-400 hover:underline" to="/register">
          Create an account
        </Link>
      </p>

      <form
        onSubmit={handleSubmit(onSubmit)}
        className="mt-8 flex flex-col gap-4 rounded-xl border border-slate-800 bg-slate-900/60 p-6"
      >
        <div>
          <label className="text-sm text-slate-300" htmlFor="email">
            Email
          </label>
          <input
            id="email"
            type="email"
            autoComplete="email"
            className="mt-1 w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-white"
            {...register('email')}
          />
          {errors.email && (
            <p className="mt-1 text-sm text-amber-400">{errors.email.message}</p>
          )}
        </div>
        <div>
          <label className="text-sm text-slate-300" htmlFor="password">
            Password
          </label>
          <input
            id="password"
            type="password"
            autoComplete="current-password"
            className="mt-1 w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-white"
            {...register('password')}
          />
          {errors.password && (
            <p className="mt-1 text-sm text-amber-400">
              {errors.password.message}
            </p>
          )}
        </div>
        {errors.root && (
          <p className="text-sm text-amber-400">{errors.root.message}</p>
        )}
        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded-md bg-indigo-600 px-4 py-2 font-medium text-white hover:bg-indigo-500 disabled:opacity-50"
        >
          {isSubmitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </div>
  )
}
