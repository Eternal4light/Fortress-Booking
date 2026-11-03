import { useEffect, useState } from 'react'
import { fetchActivities, fetchPartners, issueInvite } from '../api'

function IssueInvitePage() {
  const [partners, setPartners] = useState([])
  const [activities, setActivities] = useState([])
  const [loadError, setLoadError] = useState('')
  const [isLoading, setIsLoading] = useState(true)

  const [partnerId, setPartnerId] = useState('')
  const [activityId, setActivityId] = useState('')
  const [description, setDescription] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState('')
  const [submitSuccess, setSubmitSuccess] = useState('')

  useEffect(() => {
    Promise.all([fetchPartners(), fetchActivities()])
      .then(([partnersData, activitiesData]) => {
        setPartners(partnersData ?? [])
        setActivities(activitiesData ?? [])
        if (partnersData?.length) {
          setPartnerId(partnersData[0].id)
        }
        if (activitiesData?.length) {
          setActivityId(activitiesData[0].id)
        }
      })
      .catch((err) => setLoadError(err.message))
      .finally(() => setIsLoading(false))
  }, [])

  const handleSubmit = async (event) => {
    event.preventDefault()

    if (!partnerId || !activityId) {
      setSubmitError('Выберите коворкинг и деятельность')
      return
    }

    setIsSubmitting(true)
    setSubmitError('')
    setSubmitSuccess('')

    try {
      const result = await issueInvite(partnerId, {
        activityId,
        description: description.trim() || null,
      })
      setSubmitSuccess(
        result.queuedForDelivery
          ? 'Приглашение выдано и поставлено в очередь на рассылку клиентам'
          : 'Приглашение сохранено, но постановка в очередь на рассылку не удалась — проверьте, что RabbitMQ запущен'
      )
      setDescription('')
    } catch (err) {
      setSubmitError(err.message)
    } finally {
      setIsSubmitting(false)
    }
  }

  if (isLoading) {
    return (
      <section className="card">
        <p className="empty">Загрузка…</p>
      </section>
    )
  }

  if (loadError) {
    return (
      <section className="card">
        <div className="message message--error">{loadError}</div>
      </section>
    )
  }

  return (
    <section className="card">
      <h2 className="card__title">Выдать приглашение</h2>
      <p className="page__subtitle">
        Приглашение уйдёт всем клиентам, у которых указана выбранная деятельность.
      </p>

      {submitError && <div className="message message--error">{submitError}</div>}
      {submitSuccess && <div className="message message--success">{submitSuccess}</div>}

      <form className="form" onSubmit={handleSubmit}>
        <div className="form__row">
          <label htmlFor="invite-partner">Коворкинг</label>
          <select id="invite-partner" value={partnerId} onChange={(event) => setPartnerId(event.target.value)}>
            {partners.map((partner) => (
              <option key={partner.id} value={partner.id}>
                {partner.name}
                {partner.specialization ? ` — ${partner.specialization}` : ''}
              </option>
            ))}
          </select>
        </div>

        <div className="form__row">
          <label htmlFor="invite-activity">Деятельность</label>
          <select id="invite-activity" value={activityId} onChange={(event) => setActivityId(event.target.value)}>
            {activities.map((activity) => (
              <option key={activity.id} value={activity.id}>
                {activity.name}
              </option>
            ))}
          </select>
        </div>

        <div className="form__row">
          <label htmlFor="invite-description">Текст приглашения</label>
          <textarea
            id="invite-description"
            rows={3}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            placeholder="Например: приглашаем на бесплатный день в коворкинге"
          />
        </div>

        <button className="form__submit" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Отправляем…' : 'Выдать приглашение'}
        </button>
      </form>
    </section>
  )
}

export default IssueInvitePage
