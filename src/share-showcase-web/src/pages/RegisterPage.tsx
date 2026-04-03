import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import axios from 'axios'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { api } from '../lib/api'

const schema = z.object({
  email: z.string().email(),
  password: z.string().min(8, 'At least 8 characters'),
})

type Form = z.infer<typeof schema>

export function RegisterPage() {
  const { setToken } = useAuth()
  const navigate = useNavigate()

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
      }>('/api/v1/auth/register', data)
      setToken(res.token)
      navigate('/library', { replace: true })
    } catch (e: unknown) {
      if (axios.isAxiosError(e)) {
        const errs = e.response?.data as { errors?: string[] } | undefined
        if (errs?.errors && Array.isArray(errs.errors)) {
          setFormError('root', { message: errs.errors.join(' ') })
          return
        }
      }
      setFormError('root', { message: 'Registration failed.' })
    }
  }

  return (
    <div className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-4">
      <h1 className="text-2xl font-semibold text-white">Create account</h1>
      <p className="mt-1 text-sm text-slate-400">
        Already have one?{' '}
        <Link className="text-indigo-400 hover:underline" to="/login">
          Sign in
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
            autoComplete="new-password"
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
          {isSubmitting ? 'Creating…' : 'Register'}
        </button>
      </form>
    </div>
  )
}
