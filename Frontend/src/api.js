import { getToken } from './auth'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'

async function request(path, { method = 'GET', body, auth = false } = {}) {
  const headers = {}

  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  if (auth) {
    const token = getToken()
    if (token) {
      headers['Authorization'] = `Bearer ${token}`
    }
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (!response.ok) {
    let message = `Ошибка запроса: ${response.status}`

    try {
      const data = await response.json()
      if (typeof data === 'string' && data.trim()) {
        message = data
      } else if (data?.title) {
        message = data.title
      } else if (data?.errors) {
        message = Object.values(data.errors).flat().join('; ')
      }
    } catch {
      // тело ответа не JSON — оставляем сообщение по умолчанию
    }

    throw new Error(message)
  }

  if (response.status === 204) {
    return null
  }

  const text = await response.text()
  return text ? JSON.parse(text) : null
}

// Коворкинги (видят только менеджеры и администратор)
export const fetchPartners = () => request('/partners', { auth: true })
export const createPartner = ({ name, specialization, capacity }) =>
  request('/partners', { method: 'POST', body: { name, specialization, capacity }, auth: true })

// Деятельности (справочник для выдачи приглашений)
export const fetchActivities = () => request('/activities')

// Приглашения
export const issueInvite = (partnerId, { activityId, description }) =>
  request(`/partners/${partnerId}/invites`, {
    method: 'POST',
    body: { activityId, description },
    auth: true,
  })

// Аутентификация
export const login = (email, password) => request('/api/auth/login', { method: 'POST', body: { email, password } })

export const register = (payload) => request('/api/auth/register', { method: 'POST', body: payload })

// Текущий пользователь
export const fetchCurrentUser = () => request('/api/currentuser', { auth: true })

export const updateCurrentUser = (payload) =>
  request('/api/currentuser', { method: 'PUT', body: payload, auth: true })

export const fetchMyInvites = () => request('/api/currentuser/invites', { auth: true })

export const acceptInvite = (id) =>
  request(`/api/currentuser/invites/${id}/accept`, { method: 'POST', auth: true })

export const declineInvite = (id) =>
  request(`/api/currentuser/invites/${id}/decline`, { method: 'POST', auth: true })

// Сотрудники (только администратор)
export const fetchEmployees = () => request('/api/employees', { auth: true })

export const createEmployee = (payload) => request('/api/employees', { method: 'POST', body: payload, auth: true })

export const updateEmployee = (id, payload) =>
  request(`/api/employees/${id}`, { method: 'PUT', body: payload, auth: true })

export const deleteEmployee = (id) => request(`/api/employees/${id}`, { method: 'DELETE', auth: true })

// Клиентская база (видят только менеджеры и администратор)
export const fetchClients = (activity) =>
  request(`/api/users${activity ? `?activity=${encodeURIComponent(activity)}` : ''}`, { auth: true })

export const updateClient = (id, payload) =>
  request(`/api/users/${id}`, { method: 'PUT', body: payload, auth: true })

export const deleteClient = (id) => request(`/api/users/${id}`, { method: 'DELETE', auth: true })
