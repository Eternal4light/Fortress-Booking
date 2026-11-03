import { useEffect, useState } from 'react'
import { deleteClient, fetchClients, updateClient } from '../api'

function toEditForm(client) {
  return {
    firstName: client.firstName,
    secondName: client.secondName,
    lastName: client.lastName,
    phone: client.phone,
    activity: client.activity ?? '',
  }
}

function ClientsPage() {
  const [clients, setClients] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [activityFilter, setActivityFilter] = useState('')

  const [editingId, setEditingId] = useState(null)
  const [editForm, setEditForm] = useState(null)
  const [editError, setEditError] = useState('')
  const [isSavingEdit, setIsSavingEdit] = useState(false)

  const updateEdit = (field) => (event) => setEditForm((prev) => ({ ...prev, [field]: event.target.value }))

  const load = (activity) => {
    setIsLoading(true)
    setError('')

    fetchClients(activity)
      .then((data) => setClients(data ?? []))
      .catch((err) => setError(err.message))
      .finally(() => setIsLoading(false))
  }

  useEffect(() => {
    load()
  }, [])

  const handleFilterSubmit = (event) => {
    event.preventDefault()
    load(activityFilter.trim() || undefined)
  }

  const handleReset = () => {
    setActivityFilter('')
    load()
  }

  const startEdit = (client) => {
    setEditingId(client.id)
    setEditForm(toEditForm(client))
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
      const updated = await updateClient(editingId, {
        ...editForm,
        activity: editForm.activity.trim() || null,
      })
      setClients((prev) => prev.map((item) => (item.id === updated.id ? updated : item)))
      setEditingId(null)
      setEditForm(null)
    } catch (err) {
      setEditError(err.message)
    } finally {
      setIsSavingEdit(false)
    }
  }

  const handleDelete = async (client) => {
    const fullName = `${client.firstName} ${client.lastName}`.trim()
    if (!window.confirm(`Удалить клиента «${fullName}»? Это действие необратимо.`)) {
      return
    }

    setError('')

    try {
      await deleteClient(client.id)
      setClients((prev) => prev.filter((item) => item.id !== client.id))
      if (editingId === client.id) {
        cancelEdit()
      }
    } catch (err) {
      setError(err.message)
    }
  }

  return (
    <>
      <section className="card">
        <h2 className="card__title">Клиенты</h2>
        <p className="page__subtitle">База зарегистрированных клиентов сервиса</p>

        <form className="form" onSubmit={handleFilterSubmit}>
          <div className="form__row">
            <label htmlFor="client-activity-filter">Фильтр по деятельности</label>
            <input
              id="client-activity-filter"
              value={activityFilter}
              onChange={(event) => setActivityFilter(event.target.value)}
              placeholder="Например: Красота"
            />
          </div>

          <button className="form__submit" type="submit">
            Показать
          </button>
          {activityFilter && (
            <button type="button" className="link-button" onClick={handleReset}>
              сбросить фильтр
            </button>
          )}
        </form>
      </section>

      <section className="card">
        {isLoading && <p className="empty">Загрузка…</p>}
        {error && <div className="message message--error">{error}</div>}

        {!isLoading && !error && clients.length === 0 && <p className="empty">Клиенты не найдены</p>}

        {!isLoading && !error && clients.length > 0 && (
          <ul className="list">
            {clients.map((client) =>
              editingId === client.id ? (
                <li className="list__item list__item--column" key={client.id}>
                  <form className="form" onSubmit={saveEdit} style={{ width: '100%' }}>
                    {editError && <div className="message message--error">{editError}</div>}

                    <div className="form__row">
                      <label htmlFor="edit-client-firstName">Имя</label>
                      <input
                        id="edit-client-firstName"
                        value={editForm.firstName}
                        onChange={updateEdit('firstName')}
                        required
                      />
                    </div>

                    <div className="form__row">
                      <label htmlFor="edit-client-lastName">Фамилия</label>
                      <input
                        id="edit-client-lastName"
                        value={editForm.lastName}
                        onChange={updateEdit('lastName')}
                        required
                      />
                    </div>

                    <div className="form__row">
                      <label htmlFor="edit-client-phone">Телефон</label>
                      <input id="edit-client-phone" value={editForm.phone} onChange={updateEdit('phone')} />
                    </div>

                    <div className="form__row">
                      <label htmlFor="edit-client-activity">Деятельность</label>
                      <input
                        id="edit-client-activity"
                        value={editForm.activity}
                        onChange={updateEdit('activity')}
                        placeholder="Например: Красота"
                      />
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
                <li className="list__item list__item--column" key={client.id}>
                  <div className="list__name">
                    {client.firstName} {client.lastName}
                  </div>
                  <div className="list__specialization">
                    {client.email}
                    {client.phone ? ` · ${client.phone}` : ''}
                  </div>
                  {client.activity && <span className="badge">{client.activity}</span>}
                  <div className="list__actions">
                    <button type="button" className="link-button" onClick={() => startEdit(client)}>
                      редактировать
                    </button>
                    <button
                      type="button"
                      className="link-button link-button--danger"
                      onClick={() => handleDelete(client)}
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

export default ClientsPage
