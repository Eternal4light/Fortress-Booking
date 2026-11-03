import { useState } from 'react'
import { register } from '../api'
import { saveSession } from '../auth'

const EMPTY_FORM = {
  firstName: '',
  secondName: '',
  lastName: '',
  phone: '',
  email: '',
  password: '',
  activity: '',
}

function RegisterPage({ onAuthenticated, onSwitchToLogin }) {
  const [form, setForm] = useState(EMPTY_FORM)
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const update = (field) => (event) => setForm((prev) => ({ ...prev, [field]: event.target.value }))

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      const { token, user } = await register(form)
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
      <h2 className="card__title">Регистрация</h2>
      <p className="page__subtitle">
        Для тех, кто ищет коворкинг — по указанной деятельности вам будут приходить приглашения от партнёров.
      </p>

      {error && <div className="message message--error">{error}</div>}

      <form className="form" onSubmit={handleSubmit}>
        <div className="form__row">
          <label htmlFor="reg-firstName">Имя</label>
          <input id="reg-firstName" value={form.firstName} onChange={update('firstName')} required />
        </div>

        <div className="form__row">
          <label htmlFor="reg-lastName">Фамилия</label>
          <input id="reg-lastName" value={form.lastName} onChange={update('lastName')} required />
        </div>

        <div className="form__row">
          <label htmlFor="reg-phone">Телефон</label>
          <input id="reg-phone" value={form.phone} onChange={update('phone')} />
        </div>

        <div className="form__row">
          <label htmlFor="reg-email">Email</label>
          <input id="reg-email" type="email" value={form.email} onChange={update('email')} required />
        </div>

        <div className="form__row">
          <label htmlFor="reg-password">Пароль</label>
          <input id="reg-password" type="password" value={form.password} onChange={update('password')} required />
        </div>

        <div className="form__row">
          <label htmlFor="reg-activity">Деятельность / профессия</label>
          <input
            id="reg-activity"
            value={form.activity}
            onChange={update('activity')}
            placeholder="Например: Красота"
            required
          />
        </div>

        <button className="form__submit" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Регистрируем…' : 'Зарегистрироваться'}
        </button>
      </form>

      <p className="auth-switch">
        Уже есть аккаунт?{' '}
        <button type="button" className="link-button" onClick={onSwitchToLogin}>
          Войти
        </button>
      </p>
    </section>
  )
}

export default RegisterPage
