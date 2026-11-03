import { useEffect, useState } from 'react'
import { createPartner, fetchPartners } from '../api'

function PartnersPage() {
  const [partners, setPartners] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')

  const [name, setName] = useState('')
  const [specialization, setSpecialization] = useState('')
  const [capacity, setCapacity] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState('')
  const [submitSuccess, setSubmitSuccess] = useState('')

  const loadPartners = () => {
    setIsLoading(true)
    setLoadError('')

    fetchPartners()
      .then((data) => setPartners(data ?? []))
      .catch((error) => setLoadError(error.message))
      .finally(() => setIsLoading(false))
  }

  useEffect(() => {
    loadPartners()
  }, [])

  const handleSubmit = async (event) => {
    event.preventDefault()

    if (!name.trim()) {
      setSubmitError('Укажите название коворкинга')
      return
    }

    const capacityNumber = Number(capacity)
    if (!Number.isInteger(capacityNumber) || capacityNumber < 1) {
      setSubmitError('Вместимость — целое число не меньше 1')
      return
    }

    setIsSubmitting(true)
    setSubmitError('')
    setSubmitSuccess('')

    try {
      const created = await createPartner({
        name: name.trim(),
        specialization: specialization.trim() || null,
        capacity: capacityNumber,
      })

      setPartners((prev) => [...prev, created])
      setSubmitSuccess(`Коворкинг «${created.name}» добавлен`)
      setName('')
      setSpecialization('')
      setCapacity('')
    } catch (error) {
      setSubmitError(error.message)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <>
      <section className="card">
        <h2 className="card__title">Добавить коворкинг</h2>

        {submitError && <div className="message message--error">{submitError}</div>}
        {submitSuccess && <div className="message message--success">{submitSuccess}</div>}

        <form className="form" onSubmit={handleSubmit}>
          <div className="form__row">
            <label htmlFor="name">Название</label>
            <input
              id="name"
              type="text"
              value={name}
              onChange={(event) => setName(event.target.value)}
              placeholder="Например: Коворкинг на Ленина"
            />
          </div>

          <div className="form__row">
            <label htmlFor="specialization">Специализация</label>
            <input
              id="specialization"
              type="text"
              value={specialization}
              onChange={(event) => setSpecialization(event.target.value)}
              placeholder="Например: бьюти, офисный"
            />
          </div>

          <div className="form__row">
            <label htmlFor="capacity">Вместимость</label>
            <input
              id="capacity"
              type="number"
              min="1"
              step="1"
              value={capacity}
              onChange={(event) => setCapacity(event.target.value)}
              placeholder="Сколько клиентов можно пригласить, например: 20"
            />
          </div>

          <button className="form__submit" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Добавляем…' : 'Добавить'}
          </button>
        </form>
      </section>

      <section className="card">
        <h2 className="card__title">Список коворкингов</h2>

        {isLoading && <p className="empty">Загрузка…</p>}
        {loadError && <div className="message message--error">{loadError}</div>}

        {!isLoading && !loadError && partners.length === 0 && (
          <p className="empty">Коворкингов пока нет — добавьте первый выше</p>
        )}

        {!isLoading && !loadError && partners.length > 0 && (
          <ul className="list">
            {partners.map((partner) => (
              <li className="list__item" key={partner.id}>
                <div>
                  <div className="list__name">{partner.name}</div>
                  {partner.specialization && (
                    <div className="list__specialization">{partner.specialization}</div>
                  )}
                  <div className="list__meta">Вместимость: {partner.capacity}</div>
                </div>
                <span className={`badge ${partner.isActive ? 'badge--active' : ''}`}>
                  {partner.isActive ? 'активен' : 'неактивен'}
                </span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </>
  )
}

export default PartnersPage
