import { useEffect, useState } from 'react'
import { acceptInvite, declineInvite, fetchMyInvites } from '../api'

const STATUS_LABELS = {
  Pending: 'ждёт ответа',
  Accepted: 'принято',
  Declined: 'отклонено',
  Expired: 'отклонено автоматически — срок истёк',
}

function formatTimeLeft(expiresAt) {
  const msLeft = new Date(expiresAt).getTime() - Date.now()
  if (msLeft <= 0) {
    return 'срок истёк'
  }

  const totalMinutes = Math.ceil(msLeft / 60000)
  const hours = Math.floor(totalMinutes / 60)
  const minutes = totalMinutes % 60

  return hours > 0 ? `осталось ${hours} ч ${minutes} мин` : `осталось ${minutes} мин`
}

function MyInvitesPage() {
  const [invites, setInvites] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [actionError, setActionError] = useState('')
  const [busyId, setBusyId] = useState(null)

  const loadInvites = () =>
    fetchMyInvites()
      .then((data) => setInvites(data ?? []))
      .catch((err) => setError(err.message))

  useEffect(() => {
    loadInvites().finally(() => setIsLoading(false))
  }, [])

  const respond = async (invite, action) => {
    setBusyId(invite.id)
    setActionError('')

    try {
      const updated = await action(invite.id)
      setInvites((prev) => prev.map((item) => (item.id === updated.id ? updated : item)))
    } catch (err) {
      setActionError(err.message)
      // например, срок истёк, пока страница была открыта, — подтягиваем актуальные статусы
      await loadInvites()
    } finally {
      setBusyId(null)
    }
  }

  return (
    <section className="card">
      <h2 className="card__title">Мои приглашения</h2>
      <p className="page__subtitle">
        Приглашение нужно принять или отклонить в течение 24 часов — иначе оно отклонится автоматически.
      </p>

      {isLoading && <p className="empty">Загрузка…</p>}
      {error && <div className="message message--error">{error}</div>}
      {actionError && <div className="message message--error">{actionError}</div>}

      {!isLoading && !error && invites.length === 0 && (
        <p className="empty">
          Пока приглашений нет — они появятся здесь, когда коворкинг пригласит вас по вашей деятельности
        </p>
      )}

      {!isLoading && !error && invites.length > 0 && (
        <ul className="list">
          {invites.map((invite) => (
            <li className="list__item list__item--column" key={invite.id}>
              <div className="list__name">{invite.partnerName}</div>
              {invite.specialization && <div className="list__specialization">{invite.specialization}</div>}
              {invite.description && <p className="invite-description">{invite.description}</p>}
              <div className="list__meta">{new Date(invite.issuedAt).toLocaleString('ru-RU')}</div>

              <div className="list__actions">
                <span className={`badge badge--${invite.status.toLowerCase()}`}>
                  {STATUS_LABELS[invite.status] ?? invite.status}
                </span>

                {invite.status === 'Pending' && (
                  <>
                    <span className="list__meta">{formatTimeLeft(invite.expiresAt)}</span>
                    <button
                      className="link-button"
                      type="button"
                      disabled={busyId === invite.id}
                      onClick={() => respond(invite, acceptInvite)}
                    >
                      Принять
                    </button>
                    <button
                      className="link-button link-button--danger"
                      type="button"
                      disabled={busyId === invite.id}
                      onClick={() => respond(invite, declineInvite)}
                    >
                      Отклонить
                    </button>
                  </>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

export default MyInvitesPage
