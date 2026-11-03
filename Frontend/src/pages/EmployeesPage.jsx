import { useEffect, useState } from 'react'
import { createEmployee, deleteEmployee, fetchEmployees, updateEmployee } from '../api'

const EMPTY_FORM = {
  firstName: '',
  secondName: '',
  lastName: '',
  phone: '',
  email: '',
  password: '',
  role: 'Manager',
}

function toEditForm(employee) {
  return {
    firstName: employee.firstName,
    secondName: employee.secondName,
    lastName: employee.lastName,
    phone: employee.phone,
    role: employee.role,
    isActive: employee.isActive,
  }
}

function EmployeesPage() {
  const [employees, setEmployees] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')

  const [form, setForm] = useState(EMPTY_FORM)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState('')
  const [submitSuccess, setSubmitSuccess] = useState('')

  const [editingId, setEditingId] = useState(null)
  const [editForm, setEditForm] = useState(null)
  const [editError, setEditError] = useState('')
  const [isSavingEdit, setIsSavingEdit] = useState(false)

  const update = (field) => (event) => setForm((prev) => ({ ...prev, [field]: event.target.value }))
  const updateEdit = (field) => (event) => setEditForm((prev) => ({ ...prev, [field]: event.target.value }))

  const load = () => {
    setIsLoading(true)
    setLoadError('')

    fetchEmployees()
      .then((data) => setEmployees(data ?? []))
      .catch((err) => setLoadError(err.message))
      .finally(() => setIsLoading(false))
  }

  useEffect(() => {
    load()
  }, [])

  const handleSubmit = async (event) => {
    event.preventDefault()
    setIsSubmitting(true)
    setSubmitError('')
    setSubmitSuccess('')

    try {
      const created = await createEmployee(form)
      setEmployees((prev) => [...prev, created])
      setSubmitSuccess(`Сотрудник «${created.firstName} ${created.lastName}» добавлен`)
      setForm(EMPTY_FORM)
    } catch (err) {
      setSubmitError(err.message)
    } finally {
      setIsSubmitting(false)
    }
  }

  const toggleActive = async (employee) => {
    setLoadError('')

    try {
      const updated = await updateEmployee(employee.id, {
        firstName: employee.firstName,
        secondName: employee.secondName,
        lastName: employee.lastName,
        phone: employee.phone,
        role: employee.role,
        isActive: !employee.isActive,
      })
      setEmployees((prev) => prev.map((item) => (item.id === updated.id ? updated : item)))
    } catch (err) {
      setLoadError(err.message)
    }
  }

  const startEdit = (employee) => {
    setEditingId(employee.id)
    setEditForm(toEditForm(employee))
    setEditError('')
  }

  const cancelEdit = () => {
    setEditingId(null)
    setEditForm(null)
    setEditError('')
  }

  const saveEdit = async (event) => {
    event.preventDefault()
    setIsSavingEdit(true)
    setEditError('')

    try {
      const updated = await updateEmployee(editingId, {
        ...editForm,
        isActive: Boolean(editForm.isActive),
      })
      setEmployees((prev) => prev.map((item) => (item.id === updated.id ? updated : item)))
      setEditingId(null)
      setEditForm(null)
    } catch (err) {
      setEditError(err.message)
    } finally {
      setIsSavingEdit(false)
    }
  }

  const handleDelete = async (employee) => {
    const fullName = `${employee.firstName} ${employee.lastName}`.trim()
    if (!window.confirm(`Удалить сотрудника «${fullName}»? Это действие необратимо.`)) {
      return
    }

    setLoadError('')

    try {
      await deleteEmployee(employee.id)
      setEmployees((prev) => prev.filter((item) => item.id !== employee.id))
      if (editingId === employee.id) {
        cancelEdit()
      }
    } catch (err) {
      setLoadError(err.message)
    }
  }

  return (
    <>
      <section className="card">
        <h2 className="card__title">Добавить сотрудника</h2>

        {submitError && <div className="message message--error">{submitError}</div>}
        {submitSuccess && <div className="message message--success">{submitSuccess}</div>}

        <form className="form" onSubmit={handleSubmit}>
          <div className="form__row">
            <label htmlFor="emp-firstName">Имя</label>
            <input id="emp-firstName" value={form.firstName} onChange={update('firstName')} required />
          </div>

          <div className="form__row">
            <label htmlFor="emp-lastName">Фамилия</label>
            <input id="emp-lastName" value={form.lastName} onChange={update('lastName')} required />
          </div>

          <div className="form__row">
            <label htmlFor="emp-phone">Телефон</label>
            <input id="emp-phone" value={form.phone} onChange={update('phone')} />
          </div>

          <div className="form__row">
            <label htmlFor="emp-email">Email</label>
            <input id="emp-email" type="email" value={form.email} onChange={update('email')} required />
          </div>

          <div className="form__row">
            <label htmlFor="emp-password">Пароль</label>
            <input id="emp-password" type="password" value={form.password} onChange={update('password')} required />
          </div>

          <div className="form__row">
            <label htmlFor="emp-role">Роль</label>
            <select id="emp-role" value={form.role} onChange={update('role')}>
              <option value="Manager">Менеджер</option>
              <option value="Admin">Администратор</option>
            </select>
          </div>

          <button className="form__submit" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Добавляем…' : 'Добавить сотрудника'}
          </button>
        </form>
      </section>

      <section className="card">
        <h2 className="card__title">Сотрудники</h2>

        {isLoading && <p className="empty">Загрузка…</p>}
        {loadError && <div className="message message--error">{loadError}</div>}

        {!isLoading && !loadError && employees.length === 0 && <p className="empty">Сотрудников пока нет</p>}

        {!isLoading && !loadError && employees.length > 0 && (
          <ul className="list">
            {employees.map((employee) =>
              editingId === employee.id ? (
                <li className="list__item list__item--column" key={employee.id}>
                  <form className="form" onSubmit={saveEdit} style={{ width: '100%' }}>
                    {editError && <div className="message message--error">{editError}</div>}

                    <div className="form__row">
                      <label htmlFor="edit-emp-firstName">Имя</label>
                      <input
                        id="edit-emp-firstName"
                        value={editForm.firstName}
                        onChange={updateEdit('firstName')}
                        required
                      />
                    </div>

                    <div className="form__row">
                      <label htmlFor="edit-emp-lastName">Фамилия</label>
                      <input
                        id="edit-emp-lastName"
                        value={editForm.lastName}
                        onChange={updateEdit('lastName')}
                        required
                      />
                    </div>

                    <div className="form__row">
                      <label htmlFor="edit-emp-phone">Телефон</label>
                      <input id="edit-emp-phone" value={editForm.phone} onChange={updateEdit('phone')} />
                    </div>

                    <div className="form__row">
                      <label htmlFor="edit-emp-role">Роль</label>
                      <select id="edit-emp-role" value={editForm.role} onChange={updateEdit('role')}>
                        <option value="Manager">Менеджер</option>
                        <option value="Admin">Администратор</option>
                      </select>
                    </div>

                    <div className="list__actions">
                      <button className="form__submit" type="submit" disabled={isSavingEdit}>
                        {isSavingEdit ? 'Сохраняем…' : 'Сохранить'}
                      </button>
                      <button type="button" className="link-button" onClick={cancelEdit}>
                        отмена
                      </button>
                    </div>
                  </form>
                </li>
              ) : (
                <li className="list__item" key={employee.id}>
                  <div>
                    <div className="list__name">
                      {employee.firstName} {employee.lastName}
                    </div>
                    <div className="list__specialization">
                      {employee.email} · {employee.role === 'Admin' ? 'Администратор' : 'Менеджер'}
                    </div>
                  </div>
                  <div className="list__actions">
                    <span className={employee.isActive ? 'badge badge--active' : 'badge'}>
                      {employee.isActive ? 'активен' : 'не активен'}
                    </span>
                    <button type="button" className="link-button" onClick={() => toggleActive(employee)}>
                      {employee.isActive ? 'деактивировать' : 'активировать'}
                    </button>
                    <button type="button" className="link-button" onClick={() => startEdit(employee)}>
                      редактировать
                    </button>
                    <button
                      type="button"
                      className="link-button link-button--danger"
                      onClick={() => handleDelete(employee)}
                    >
                      удалить
                    </button>
                  </div>
                </li>
              )
            )}
          </ul>
        )}
      </section>
    </>
  )
}

export default EmployeesPage
