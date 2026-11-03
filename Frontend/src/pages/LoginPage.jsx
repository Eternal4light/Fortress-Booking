import { useState } from 'react'
import { login } from '../api'
import { saveSession } from '../auth'

function LoginPage({ onAuthenticated, onSwitchToRegister }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      const { token, user } = await login(email, password)
      saveSession(token, user)
      onAuthenticated(user)
    } catch (err) {
      setError(err.message)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <section className="card card--narrow">
      <h2 className="card__title">Вход</h2>

      {error && <div className="message message--error">{error}</div>}

      <form className="form" onSubmit={handleSubmit}>
        <div className="form__row">
          <label htmlFor="login-email">Email</label>
          <input
            id="login-email"
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
          />
        </div>

        <div className="form__row">
          <label htmlFor="login-password">Пароль</label>
          <input
            id="login-password"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
        </div>

        <button className="form__submit" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Входим…' : 'Войти'}
        </button>
      </form>

      <p className="auth-switch">
        Ищете коворкинг и ещё не зарегистрированы?{' '}
        <button type="button" className="link-button" onClick={onSwitchToRegister}>
          Зарегистрироваться
        </button>
      </p>
    </section>
  )
}

export default LoginPage
